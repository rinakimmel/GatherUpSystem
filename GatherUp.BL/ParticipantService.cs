using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Core.Exceptions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace GatherUp.BL
{
    public partial class ParticipantService
    {
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly IRepository<EventManager> _managerRepo;
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
            _eventRepo = eventRepo;
            _managerRepo = managerRepo;
            _mailService = mailService;
            _notifications = notifications;

            // הרשמה לאירועים הרלוונטיים למחלקה זו
            _notifications.OnEventDetailsChanged += id => _ = HandleEventDetailsChangedAsync(id);
        }

        public async Task<Participant> AddParticipantAsync(int eventId, Participant participant)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            await _participantRepo.AddAsync(participant);
            ev.ParticipantIds.Add(participant.Id);
            await _eventRepo.UpdateAsync(ev);

            // optionally notify host/manager
            return participant;
        }

        // compatibility method expected by tests
        public Task AddParticipantToEventAsync(int eventId, Participant participant) =>
            AddParticipantAsync(eventId, participant).ContinueWith(_ => { });

        public async Task ConfirmAttendanceAsync(int eventId, int participantId, bool isAttending)
        {
            var participant = await _participantRepo.GetByIdAsync(participantId) ?? throw new NotFoundException($"Participant {participantId} not found.");
            participant.IsAttending = isAttending;
            await _participantRepo.UpdateAsync(participant);

            _notifications.RaiseAttendanceConfirmed(eventId, participantId);
        }

        public async Task SendInvitationsAsync(int eventId, string invitationUrlBase)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            var participants = await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id)));
            var pending = participants.Where(p => p != null && p.IsAttending == null).Cast<Participant>();

            foreach (var p in pending)
            {
                var link = invitationUrlBase?.TrimEnd('/') + "/?participantId=" + p.Id + "&eventId=" + eventId;
                await _mailService.SendAsync(p.Email, $"Invitation to {ev.Name}", $"Hi {p.Name}, please respond here: {link}");
            }
        }

        // compatibility method expected by tests
        public async Task SendInvitationRemindersAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");
            var participants = await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id)));
            var pending = participants.Where(p => p != null && p.IsAttending == null).Cast<Participant>();

            foreach (var p in pending)
            {
                await _mailService.SendAsync(p.Email, $"Reminder: please respond for {ev.Name}", $"Hi {p.Name}, please confirm your attendance for {ev.Name}.");
            }
        }

        public async Task RegisterParticipantPaymentAsync(int eventId, int participantId, decimal amount)
        {
            var participant = await _participantRepo.GetByIdAsync(participantId) ?? throw new NotFoundException($"Participant {participantId} not found.");
            participant.HasPaid = true;
            participant.AmountContributed += amount;
            await _participantRepo.UpdateAsync(participant);

            _notifications.RaisePaymentReceived(eventId, participantId, amount);
        }

        // event handler: notify participants who want important updates when event changes
        private async Task HandleEventDetailsChangedAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return;

            var participants = (await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id))))
                .Where(p => p != null && p.MailingPreferences.HasFlag(MailingPreference.ImportantUpdatesOnly)).Cast<Participant>();

            foreach (var p in participants)
            {
                await _mailService.SendAsync(p.Email,
                    $"Event updated - {ev.Name}",
                    $"The details of {ev.Name} have been updated.");
            }
        }
    }

    public partial class ParticipantService
    {
        // synchronous wrappers for compatibility
        public Participant AddParticipant(int eventId, Participant participant) =>
            AddParticipantAsync(eventId, participant).GetAwaiter().GetResult();

        public void ConfirmAttendance(int eventId, int participantId, bool isAttending) =>
            ConfirmAttendanceAsync(eventId, participantId, isAttending).GetAwaiter().GetResult();

        public void SendInvitations(int eventId, string invitationUrlBase) =>
            SendInvitationsAsync(eventId, invitationUrlBase).GetAwaiter().GetResult();

        public void RegisterParticipantPayment(int eventId, int participantId, decimal amount) =>
            RegisterParticipantPaymentAsync(eventId, participantId, amount).GetAwaiter().GetResult();

        // synchronous wrappers for new compatibility methods
        public void AddParticipantToEvent(int eventId, Participant participant) =>
            AddParticipantToEventAsync(eventId, participant).GetAwaiter().GetResult();

        public void SendInvitationReminders(int eventId) =>
            SendInvitationRemindersAsync(eventId).GetAwaiter().GetResult();
    }
}
