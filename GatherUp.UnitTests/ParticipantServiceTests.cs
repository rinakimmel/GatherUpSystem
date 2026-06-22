using GatherUp.BL;
using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace GatherUp.UnitTests
{
    public class ParticipantServiceTests
    {
        [Fact]
        public async Task AddParticipantToEvent_AddsParticipantAndLinksToEvent()
        {
            // Arrange
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new ParticipantService(participantRepo, managerRepo, eventRepo, mailService, notifications);

            var ev = new Event(0, "E1", "desc");
            await eventRepo.AddAsync(ev);

            var p = new Participant(0, "Test", "t@example.com");

            // Act
            await svc.AddParticipantToEventAsync(ev.Id, p);

            // Assert
            var stored = await participantRepo.GetByIdAsync(p.Id);
            Assert.NotNull(stored);
            var ev2 = await eventRepo.GetByIdAsync(ev.Id);
            Assert.Contains(p.Id, ev2.ParticipantIds);
        }

        [Fact]
        public async Task ConfirmAttendance_UpdatesParticipantAndRaisesNotification()
        {
            // Arrange
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new ParticipantService(participantRepo, managerRepo, eventRepo, mailService, notifications);

            var p = new Participant(0, "T2", "t2@example.com");
            await participantRepo.AddAsync(p);

            bool notified = false;
            notifications.OnAttendanceConfirmed += (eventId, participantId) => notified = true;

            // Act
            await svc.ConfirmAttendanceAsync(1, p.Id, true);

            // Assert
            var stored = await participantRepo.GetByIdAsync(p.Id);
            Assert.True(stored.IsAttending == true);
            Assert.True(notified);
        }

        // simple in-memory mail service for tests
        private class TestMailService : IMailService
        {
            public readonly System.Collections.Generic.List<(string To, string Subject, string Body)> Sent = new();
            public void Send(string toEmail, string subject, string body) => Sent.Add((toEmail, subject, body));
            public Task SendAsync(string toEmail, string subject, string body)
            {
                Send(toEmail, subject, body);
                return Task.CompletedTask;
            }
        }
    }
}
