using GatherUp.Core;
using GatherUp.Core.DO;

namespace GatherUp.BL
{
    public class ParticipantService
    {
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly IMailService _mailService;
        private readonly IEventNotifications _notifications;

        public ParticipantService(
            IRepository<Participant> participantRepo,
            IRepository<EventManager> managerRepo,
            IRepository<Event> eventRepo,
            IMailService mailService,
            IEventNotifications notifications)
        {
            _participantRepo = participantRepo;
            _managerRepo = managerRepo;
            _eventRepo = eventRepo;
            _mailService = mailService;
            _notifications = notifications;

            // הרשמה לאירועים הרלוונטיים למחלקה זו
            _notifications.OnAttendanceConfirmed += HandleAttendanceConfirmed;
        }

        public void AddParticipantToEvent(int eventId, Participant participant)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");
            _participantRepo.Add(participant);
            ev.ParticipantIds.Add(participant.Id);
            _eventRepo.Update(ev);
        }

        public void ConfirmAttendance(int eventId, int participantId, bool isAttending)
        {
            var participant = _participantRepo.GetById(participantId)
                ?? throw new KeyNotFoundException($"Participant {participantId} not found.");

            participant.IsAttending = isAttending;
            _participantRepo.Update(participant);

            if (isAttending)
                _notifications.OnAttendanceConfirmed?.Invoke(eventId, participantId);
        }

        public void SendInvitationReminders(int eventId)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && p.IsAttending == null)
                .ToList()
                .ForEach(p => _mailService.Send(
                    p!.Email,
                    $"Reminder: Please confirm your attendance for {ev.Name}",
                    $"Hi {p.Name}, please confirm your attendance at: https://gatherup.app/confirm/{p.Id}/{eventId}"));
        }

        public IEnumerable<Participant> GetEventParticipants(int eventId)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");
            return ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null)
                .Cast<Participant>();
        }

        // טיפול באירוע: מי ביקש לקבל מייל על אישור הגעה — המנהל
        private void HandleAttendanceConfirmed(int eventId, int participantId)
        {
            var ev = _eventRepo.GetById(eventId);
            if (ev == null) return;
            var manager = _managerRepo.GetById(ev.EventManagerId);
            var participant = _participantRepo.GetById(participantId);
            if (manager == null || participant == null) return;

            _mailService.Send(manager.Email,
                $"Attendance confirmed – {ev.Name}",
                $"{participant.Name} has confirmed attendance.");
        }
    }
}
