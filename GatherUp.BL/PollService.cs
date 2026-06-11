using GatherUp.Core;
using GatherUp.Core.DO;

namespace GatherUp.BL
{
    public record PollResults(
        Poll Poll,
        IEnumerable<QuestionResult> QuestionResults);

    public record QuestionResult(
        string Question,
        IEnumerable<(string Choice, int Count, double Percentage)> OptionBreakdown);

    public class PollService
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
            _notifications.OnPollCreated += HandlePollCreated;
            _notifications.OnPollVoteCast += HandlePollVoteCast;
            _notifications.OnEventDetailsChanged += HandleEventDetailsChanged;
        }

        public Poll CreatePoll(int eventId, string name, string description,
            IEnumerable<(string Question, IEnumerable<string> Options)> questions)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            var poll = new Poll(0, name, description);
            int qId = 1;
            foreach (var (question, options) in questions)
            {
                var pq = new PollQuestion(qId++, question);
                pq.ChoiceOptions.AddRange(options);
                poll.Questions.Add(pq);
            }

            _pollRepo.Add(poll);
            ev.PollIds.Add(poll.Id);
            _eventRepo.Update(ev);

            _notifications.OnPollCreated?.Invoke(poll.Id, eventId);
            return poll;
        }

        public void SubmitVote(int pollId, int questionId, int participantId, string choice)
        {
            var poll = _pollRepo.GetById(pollId) ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

            var question = poll.Questions.FirstOrDefault(q => q.Id == questionId)
                ?? throw new KeyNotFoundException($"Question {questionId} not found.");

            // מניעת כפילות – הסרת הצבעה קודמת אם קיימת
            question.ParticipantChoices.RemoveAll(c => c.ParticipantId == participantId);
            question.ParticipantChoices.Add(new ParticipantChoice(participantId, choice));

            _pollRepo.Update(poll);
            _notifications.OnPollVoteCast?.Invoke(pollId, participantId);
        }

        public PollResults GetPollResults(int pollId)
        {
            var poll = _pollRepo.GetById(pollId) ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

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
        public bool IsPollOpen(int pollId) =>
            _pollRepo.GetAll().Any(p => p.Id == pollId);

        // טיפול באירוע: מי ביקש מייל על סקר חדש — משתתפים עם AllUpdates
        private void HandlePollCreated(int pollId, int eventId)
        {
            var ev = _eventRepo.GetById(eventId);
            if (ev == null) return;

            ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && p.MailingPreferences.HasFlag(MailingPreference.AllUpdates))
                .ToList()
                .ForEach(p => _mailService.Send(p!.Email,
                    $"New poll – {ev.Name}",
                    $"A new poll (#{pollId}) has been created. Log in to vote."));
        }

        // טיפול באירוע: מי ביקש מייל על הצבעה חדשה — המנהל
        private void HandlePollVoteCast(int pollId, int participantId)
        {
            var ev = _eventRepo.GetAll().FirstOrDefault(e => e.PollIds.Contains(pollId));
            if (ev == null) return;
            var manager = _managerRepo.GetById(ev.EventManagerId);
            var participant = _participantRepo.GetById(participantId);
            if (manager == null || participant == null) return;

            _mailService.Send(manager.Email,
                $"New vote in poll #{pollId} – {ev.Name}",
                $"{participant.Name} submitted a vote.");
        }

        // טיפול באירוע: מי ביקש מייל על שינוי פרטי אירוע — משתתפים עם ImportantUpdatesOnly
        private void HandleEventDetailsChanged(int eventId)
        {
            var ev = _eventRepo.GetById(eventId);
            if (ev == null) return;

            ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && p.MailingPreferences.HasFlag(MailingPreference.ImportantUpdatesOnly))
                .ToList()
                .ForEach(p => _mailService.Send(p!.Email,
                    $"Event updated – {ev.Name}",
                    $"The details of {ev.Name} have been updated."));
        }
    }
}
