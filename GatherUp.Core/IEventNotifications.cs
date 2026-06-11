namespace GatherUp.Core
{
    public interface IEventNotifications
    {
        event Action<int, int>? OnAttendanceConfirmed;     // eventId, participantId
        event Action<int, int, decimal>? OnPaymentReceived; // eventId, participantId, amount
        event Action<int, int>? OnPollVoteCast;             // pollId, participantId
        event Action<int, int>? OnPollCreated;              // pollId, eventId
        event Action<int>? OnEventDetailsChanged;           // eventId
    }
}
