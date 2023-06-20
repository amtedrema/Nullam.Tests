using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Nullam.Data;
using Nullam.Models;
using Nullam.Services;

namespace Nullam.Tests.Services
{
    public class EventServiceTests
    {
        private static NullamDbContext GetDbContextWithData()
        {
            var options = new DbContextOptionsBuilder<NullamDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            var dbContext = new NullamDbContext(options);

            dbContext.Event.AddRange(GetSampleEvents());
            dbContext.SaveChanges();

            return dbContext;
        }

        private static List<Event> GetSampleEvents()
        {
            return new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event 1",
                    OccurrenceTime = DateTime.UtcNow.AddDays(2),
                    Place = "Place 1",
                    Info = "Info 1"
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event 2",
                    OccurrenceTime = DateTime.UtcNow.AddDays(-10),
                    Place = "Place 2",
                    Info = "Info 2"
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Name = "Event 3",
                    OccurrenceTime = DateTime.UtcNow.AddDays(1),
                    Place = "Place 3",
                    Info = "Info 3"
                }
            };
        }

        [Fact]
        public void GetEventsByDate_ReturnsEventsOrderedByOccurrenceTime()
        {

            // Arrange
            var contextMock = new Mock<NullamDbContext>();
            var eventSetMock = new Mock<DbSet<Event>>();

            var events = GetSampleEvents();

            eventSetMock.As<IQueryable<Event>>().Setup(e => e.Provider).Returns(events.AsQueryable().Provider);
            eventSetMock.As<IQueryable<Event>>().Setup(e => e.Expression).Returns(events.AsQueryable().Expression);
            eventSetMock.As<IQueryable<Event>>().Setup(e => e.ElementType).Returns(events.AsQueryable().ElementType);
            eventSetMock.As<IQueryable<Event>>().Setup(e => e.GetEnumerator()).Returns(events.GetEnumerator());

            contextMock.Setup(c => c.Event).Returns(eventSetMock.Object);
            var eventService = new EventService(contextMock.Object);

            // Act
            var result = eventService.GetEventsByDate();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PastEvents.Count);
            Assert.Equal(2, result.FutureEvents.Count);
            Assert.Equal("Event 3", result.FutureEvents[0].Name);
            Assert.Equal("Event 1", result.FutureEvents[1].Name);
        }

        [Fact]
        public void GetEventDetails_WithValidId_ReturnsEventDetailViewModel()
        {
            // Arrange
            Guid eventId;
            var dbContext = GetDbContextWithData();

            dbContext.Event.Add(
                new Event
                {
                    Id = eventId = Guid.NewGuid(),
                    Name = "Event 4",
                    OccurrenceTime = DateTime.UtcNow.AddDays(3),
                    Place = "Place 4",
                    Info = "Info 4"
                });
            dbContext.SaveChanges();

            var eventService = new EventService(dbContext);

            // Act
            var result = eventService.GetEventDetails(eventId.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(eventId.ToString(), result.Event.Id.ToString());
            Assert.Equal("Event 4", result.Event.Name);
            Assert.Equal(0, result.Participants.Count);
            Assert.NotNull(result.NewParticipant);
            Assert.Equal(eventId.ToString(), result.NewParticipant.EventId);
        }

        [Fact]
        public void GetEventDetails_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var dbContext = GetDbContextWithData();
            var eventService = new EventService(dbContext);

            // Act
            var result = eventService.GetEventDetails(Guid.NewGuid().ToString());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateEvent_AddsEventToDbContext()
        {
            // Arrange
            var events = new List<Event> { 
                new Event 
                {
                    Id = Guid.NewGuid(),
                    Name = "New Event Name",
                    OccurrenceTime = DateTime.UtcNow.AddDays(1),
                    Place = "New Place",
                    Info = "New Info"
                } 
            };

            var dbContext = new Mock<NullamDbContext>();
            dbContext.Setup(db => db.Event).ReturnsDbSet(events);
            var eventService = new EventService(dbContext.Object);

            // Act
            eventService.CreateEvent(events.First());

            // Assert
            dbContext.Verify(db => db.Event.Add(events.First()), Times.Once);
            dbContext.Verify(db => db.SaveChanges(), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithValidId_RemovesEventFromDbContext()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var events = new List<Event> { new Event {Id = eventId, Name = "Event Name", Place = "Event Place" } };

            var dbContextMock = new Mock<NullamDbContext>();
            dbContextMock.Setup(db => db.Event).ReturnsDbSet(events);

            var eventService = new EventService(dbContextMock.Object);

            // Act
            var result = eventService.DeleteEvent(eventId);

            // Assert
            Assert.True(result);
            dbContextMock.Verify(db => db.SaveChanges(), Times.Once);
            dbContextMock.Verify(db => db.Event.Remove(It.IsAny<Event>()), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            IList<Event> events = new List<Event> { new Event { Id = eventId, Name = "Event Name", Place = "Event Place" } };

            var dbContextMock = new Mock<NullamDbContext>();
            dbContextMock.Setup(db => db.Event).ReturnsDbSet(events);

            var eventService = new EventService(dbContextMock.Object);

            // Act

            var result = eventService.DeleteEvent(Guid.NewGuid()); // Invalid ID

            // Assert
            Assert.False(result);
            dbContextMock.Verify(db => db.SaveChanges(), Times.Never);
            dbContextMock.Verify(db => db.Event.Remove(It.IsAny<Event>()), Times.Never);
        }
    }
}
