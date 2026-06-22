namespace GatherUp.Core
{
    public interface IEventNotifications
    {
        void RaiseAttendanceConfirmed(int eventId, int participantId);
        void RaisePaymentReceived(int eventId, int participantId, decimal amount);
        void RaisePollVoteCast(int pollId, int participantId);
        void RaisePollCreated(int pollId, int eventId);
        void RaiseEventDetailsChanged(int eventId);

        event Action<int, int>? OnAttendanceConfirmed;     // eventId, participantId
        event Action<int, int, decimal>? OnPaymentReceived; // eventId, participantId, amount
        event Action<int, int>? OnPollVoteCast;             // pollId, participantId
        event Action<int, int>? OnPollCreated;              // pollId, eventId
        event Action<int>? OnEventDetailsChanged;           // eventId
    }
}
