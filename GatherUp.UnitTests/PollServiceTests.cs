using GatherUp.BL;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using GatherUp.Core;
using System.Collections.Generic;

namespace GatherUp.UnitTests
{
    public class PollServiceTests
    {
        [Fact]
        public async Task CreatePoll_NotifiesParticipantsWithAllUpdates_AndLinksPollToEvent()
        {
            // Arrange
            var pollRepo = new MemoryRepository<Poll>();
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notifications);

            var participant = new Participant(0, "P1", "p1@example.com") { MailingPreferences = MailingPreference.AllUpdates };
            await participantRepo.AddAsync(participant);

            var ev = new Event(0, "E1", "desc");
            await eventRepo.AddAsync(ev);
            ev.ParticipantIds.Add(participant.Id);
            await eventRepo.UpdateAsync(ev);

            // Act
            var poll = await svc.CreatePollAsync(ev.Id, "Poll1", "desc", new[] { ("Q?", (IEnumerable<string>)new[] { "A", "B" }) });

            // Assert
            var storedPoll = (await pollRepo.GetAllAsync()).FirstOrDefault(p => p.Id == poll.Id);
            Assert.NotNull(storedPoll);

            var evAfter = await eventRepo.GetByIdAsync(ev.Id);
            Assert.Contains(poll.Id, evAfter.PollIds);

            // mail sent to participant
            Assert.Contains(mailService.Sent, s => s.To == participant.Email && s.Subject.Contains("New poll"));
        }

        [Fact]
        public async Task SubmitVote_PreventsDuplicateAndNotifiesManager()
        {
            // Arrange
            var pollRepo = new MemoryRepository<Poll>();
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notifications);

            var manager = new EventManager(0, "M", "m@example.com");
            await managerRepo.AddAsync(manager);

            var participant = new Participant(0, "P2", "p2@example.com") { MailingPreferences = MailingPreference.AllUpdates };
            await participantRepo.AddAsync(participant);

            var ev = new Event(0, "E2", "desc");
            await eventRepo.AddAsync(ev);
            ev.EventManagerId = manager.Id;
            ev.ParticipantIds.Add(participant.Id);
            await eventRepo.UpdateAsync(ev);

            var poll = await svc.CreatePollAsync(ev.Id, "Poll2", "desc", new[] { ("Where?", (IEnumerable<string>)new[] { "X", "Y" }) });

            // Act - first vote
            await svc.SubmitVoteAsync(poll.Id, 1, participant.Id, "X");
            // second vote (update)
            await svc.SubmitVoteAsync(poll.Id, 1, participant.Id, "Y");

            // Assert participant choice only single and is 'Y'
            var stored = await pollRepo.GetByIdAsync(poll.Id);
            var q = stored.Questions.First(qt => qt.Id == 1);
            Assert.Equal(1, q.ParticipantChoices.Count(c => c.ParticipantId == participant.Id));
            Assert.Equal("Y", q.ParticipantChoices.First(c => c.ParticipantId == participant.Id).Choice);

            // manager notified by mail
            Assert.Contains(mailService.Sent, s => s.To == manager.Email && s.Subject.Contains("New vote"));
        }

        [Fact]
        public async Task GetPollResults_ReturnsCorrectCountsAndPercentages()
        {
            // Arrange
            var pollRepo = new MemoryRepository<Poll>();
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var svc = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notifications);

            var p1 = new Participant(0, "A", "a@example.com");
            var p2 = new Participant(0, "B", "b@example.com");
            await participantRepo.AddAsync(p1); await participantRepo.AddAsync(p2);

            var ev = new Event(0, "E3", "d");
            await eventRepo.AddAsync(ev);
            ev.ParticipantIds.Add(p1.Id); ev.ParticipantIds.Add(p2.Id);
            await eventRepo.UpdateAsync(ev);

            var poll = await svc.CreatePollAsync(ev.Id, "Poll3", "desc", new[] { ("Select", (IEnumerable<string>)new[] { "Opt1", "Opt2" }) });

            await svc.SubmitVoteAsync(poll.Id, 1, p1.Id, "Opt1");
            await svc.SubmitVoteAsync(poll.Id, 1, p2.Id, "Opt1");

            // Act
            var results = await svc.GetPollResultsAsync(poll.Id);
            var qres = results.QuestionResults.First();
            var opt1 = qres.OptionBreakdown.First(ob => ob.Choice == "Opt1");
            var opt2 = qres.OptionBreakdown.First(ob => ob.Choice == "Opt2");

            // Assert
            Assert.Equal(2, opt1.Count);
            Assert.Equal(0, opt2.Count);
            Assert.Equal(100.0, opt1.Percentage);
            Assert.Equal(0.0, opt2.Percentage);
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
