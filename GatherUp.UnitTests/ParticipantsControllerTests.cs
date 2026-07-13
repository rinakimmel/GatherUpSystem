using GatherUp.API.Controllers;
using GatherUp.BL;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using Xunit;
using System.Threading.Tasks;
using GatherUp.Core;

namespace GatherUp.UnitTests
{
    public class ParticipantsControllerTests
    {
        [Fact]
        public async Task AddParticipant_ReturnsCreated()
        {
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new ParticipantService(participantRepo, managerRepo, eventRepo, mailService, notifications);
            var controller = new ParticipantsController(svc, participantRepo, eventRepo);

            var ev = new Event(0, "E", "d");
            await eventRepo.AddAsync(ev);

            var p = new Participant(0, "Alice", "a@example.com");

            var result = await controller.AddParticipant(ev.Id, p) as Microsoft.AspNetCore.Mvc.CreatedAtActionResult;
            Assert.NotNull(result);
            var returned = result.Value as Participant;
            Assert.NotNull(returned);
            Assert.Equal(p.Name, returned.Name);
        }

        [Fact]
        public async Task ConfirmAttendance_NoContentAndUpdatesParticipant()
        {
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new ParticipantService(participantRepo, managerRepo, eventRepo, mailService, notifications);
            var controller = new ParticipantsController(svc, participantRepo, eventRepo);

            var p = new Participant(0, "Bob", "b@example.com");
            await participantRepo.AddAsync(p);

            var res = await controller.ConfirmAttendance(1, p.Id, true) as Microsoft.AspNetCore.Mvc.NoContentResult;
            Assert.NotNull(res);

            var stored = await participantRepo.GetByIdAsync(p.Id);
            Assert.True(stored.IsAttending == true);
        }

        // small in-test mail service
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
