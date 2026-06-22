using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Core.Exceptions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace GatherUp.BL
{
    public record PollResults(
        Poll Poll,
        IEnumerable<QuestionResult> QuestionResults);

    public record QuestionResult(
        string Question,
        IEnumerable<(string Choice, int Count, double Percentage)> OptionBreakdown);

    public partial class PollService
    {
        private readonly IRepository<Poll> _pollRepo;
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly IMailService _mailService;
        private readonly IEventNotifications _notifications;

        public PollService(
            IRepository<Poll> pollRepo,
            IRepository<Participant> participantRepo,
            IRepository<EventManager> managerRepo,
            IRepository<Event> eventRepo,
            IMailService mailService,
            IEventNotifications notifications)
        {
            _pollRepo = pollRepo;
            _participantRepo = participantRepo;
            _managerRepo = managerRepo;
            _eventRepo = eventRepo;
            _mailService = mailService;
            _notifications = notifications;

            // הרשמה לאירועים הרלוונטיים למחלקה זו
            _notifications.OnPollCreated += (pollId, eventId) => _ = HandlePollCreatedAsync(pollId, eventId);
            _notifications.OnPollVoteCast += (pollId, participantId) => _ = HandlePollVoteCastAsync(pollId, participantId);
            _notifications.OnEventDetailsChanged += id => _ = HandleEventDetailsChangedAsync(id);
        }

        public async Task<Poll> CreatePollAsync(int eventId, string name, string description,
            IEnumerable<(string Question, IEnumerable<string> Options)> questions)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            var poll = new Poll(0, name, description);
            int qId = 1;
            foreach (var (question, options) in questions)
            {
                var pq = new PollQuestion(qId++, question);
                pq.ChoiceOptions.AddRange(options);
                poll.Questions.Add(pq);
            }

            await _pollRepo.AddAsync(poll);
            ev.PollIds.Add(poll.Id);
            await _eventRepo.UpdateAsync(ev);

            _notifications.RaisePollCreated(poll.Id, eventId);
            return poll;
        }

        public async Task SubmitVoteAsync(int pollId, int questionId, int participantId, string choice)
        {
            var poll = await _pollRepo.GetByIdAsync(pollId) ?? throw new NotFoundException($"Poll {pollId} not found.");

            var question = poll.Questions.FirstOrDefault(q => q.Id == questionId)
                ?? throw new NotFoundException($"Question {questionId} not found.");

            // מניעת כפילות – הסרת הצבעה קודמת אם קיימת
            question.ParticipantChoices.RemoveAll(c => c.ParticipantId == participantId);
            question.ParticipantChoices.Add(new ParticipantChoice(participantId, choice));

            await _pollRepo.UpdateAsync(poll);
            _notifications.RaisePollVoteCast(pollId, participantId);
        }

        public async Task<PollResults> GetPollResultsAsync(int pollId)
        {
            var poll = await _pollRepo.GetByIdAsync(pollId) ?? throw new NotFoundException($"Poll {pollId} not found.");

            var results = poll.Questions.Select(q =>
            {
                int total = q.ParticipantChoices.Count;
                var breakdown = q.ChoiceOptions.Select(opt =>
                {
                    int count = q.ParticipantChoices.Count(c => c.Choice == opt);
                    double pct = total == 0 ? 0 : Math.Round((double)count / total * 100, 1);
                    return (opt, count, pct);
                });
                return new QuestionResult(q.QuestionContent, breakdown);
            });

            return new PollResults(poll, results);
        }

        // בדיקת קיום – סעיף 4.1c
        public async Task<bool> IsPollOpenAsync(int pollId) =>
            (await _pollRepo.GetAllAsync()).Any(p => p.Id == pollId);

        // async handlers
        private async Task HandlePollCreatedAsync(int pollId, int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return;

            var participants = (await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id))))
                .Where(p => p != null && p.MailingPreferences.HasFlag(MailingPreference.AllUpdates)).Cast<Participant>();

            foreach (var p in participants)
            {
                await _mailService.SendAsync(p.Email,
                    $"New poll – {ev.Name}",
                    $"A new poll (#{pollId}) has been created. Log in to vote.");
            }
        }

        private async Task HandlePollVoteCastAsync(int pollId, int participantId)
        {
            var ev = (await _eventRepo.GetAllAsync()).FirstOrDefault(e => e.PollIds.Contains(pollId));
            if (ev == null) return;
            var manager = await _managerRepo.GetByIdAsync(ev.EventManagerId);
            var participant = await _participantRepo.GetByIdAsync(participantId);
            if (manager == null || participant == null) return;

            await _mailService.SendAsync(manager.Email,
                $"New vote in poll #{pollId} – {ev.Name}",
                $"{participant.Name} submitted a vote.");
        }

        private async Task HandleEventDetailsChangedAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return;

            var participants = (await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id))))
                .Where(p => p != null && p.MailingPreferences.HasFlag(MailingPreference.ImportantUpdatesOnly)).Cast<Participant>();

            foreach (var p in participants)
            {
                await _mailService.SendAsync(p.Email,
                    $"Event updated – {ev.Name}",
                    $"The details of {ev.Name} have been updated.");
            }
        }

        public Poll CreatePoll(int eventId, string name, string description, IEnumerable<(string Question, IEnumerable<string> Options)> questions)
            => CreatePollAsync(eventId, name, description, questions).GetAwaiter().GetResult();

        public void SubmitVote(int pollId, int questionId, int participantId, string choice)
            => SubmitVoteAsync(pollId, questionId, participantId, choice).GetAwaiter().GetResult();

        public PollResults GetPollResults(int pollId)
            => GetPollResultsAsync(pollId).GetAwaiter().GetResult();

        public bool IsPollOpen(int pollId) => IsPollOpenAsync(pollId).GetAwaiter().GetResult();
    }
}
