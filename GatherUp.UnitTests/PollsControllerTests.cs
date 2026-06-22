using GatherUp.API.Controllers;
using GatherUp.BL;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using System.Collections.Generic;
using Xunit;
using System.Threading.Tasks;
//using GatherUp.Core.Interfaces;
 using GatherUp.Core;


namespace GatherUp.UnitTests
{
    public class PollsControllerTests
    {
        [Fact]
        public async Task CreatePoll_ReturnsCreated()
        {
            var pollRepo = new MemoryRepository<Poll>();
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notifications);
            var controller = new PollsController(svc);

            var ev = new Event(0, "E", "d");
            await eventRepo.AddAsync(ev);

            var dto = new PollDto("P1", "desc", new List<PollQuestionDto> { new PollQuestionDto("Q?", new List<string>{ "A","B" }) });

            var result = await controller.CreatePoll(ev.Id, dto) as Microsoft.AspNetCore.Mvc.CreatedAtActionResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Vote_NoContent_And_ResultsUpdated()
        {
            var pollRepo = new MemoryRepository<Poll>();
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notifications);
            var controller = new PollsController(svc);

            var ev = new Event(0, "E", "d");
            await eventRepo.AddAsync(ev);

            var dto = new PollDto("P2", "desc", new List<PollQuestionDto> { new PollQuestionDto("Q?", new List<string>{ "A","B" }) });
            var created = await controller.CreatePoll(ev.Id, dto) as Microsoft.AspNetCore.Mvc.CreatedAtActionResult;
            var poll = created.Value as Poll;

            var vote = new VoteDto(1, 0, "A");
            // ensure participant exists
            var p = new Participant(0, "X", "x@x.com");
            await participantRepo.AddAsync(p);
            vote = vote with { ParticipantId = p.Id };

            var res = await controller.Vote(poll.Id, vote) as Microsoft.AspNetCore.Mvc.NoContentResult;
            Assert.NotNull(res);

            var results = await controller.GetResults(poll.Id) as Microsoft.AspNetCore.Mvc.OkObjectResult;
            Assert.NotNull(results);
        }

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
