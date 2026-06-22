using GatherUp.Core;

namespace GatherUp.BL
{
    public class EventNotificationBus : IEventNotifications
    {
        public event Action<int, int>? OnAttendanceConfirmed;
        public event Action<int, int, decimal>? OnPaymentReceived;
        public event Action<int, int>? OnPollVoteCast;
        public event Action<int, int>? OnPollCreated;
        public event Action<int>? OnEventDetailsChanged;

        public void RaiseAttendanceConfirmed(int eventId, int participantId) =>
            OnAttendanceConfirmed?.Invoke(eventId, participantId);

        public void RaisePaymentReceived(int eventId, int participantId, decimal amount) =>
            OnPaymentReceived?.Invoke(eventId, participantId, amount);

        public void RaisePollVoteCast(int pollId, int participantId) =>
            OnPollVoteCast?.Invoke(pollId, participantId);

        public void RaisePollCreated(int pollId, int eventId) =>
            OnPollCreated?.Invoke(pollId, eventId);

        public void RaiseEventDetailsChanged(int eventId) =>
            OnEventDetailsChanged?.Invoke(eventId);
    }
}
