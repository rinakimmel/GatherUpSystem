using GatherUp.Core;

namespace GatherUp.BL
{
    // אובייקט זה מחזיק את ה-events ומעביר אותם בין מחלקות ה-BL.
    // כל מחלקת BL נרשמת בבנאי שלה לאירועים הרלוונטיים לה ומטפלת בהם בעצמה.
    public class EventNotificationBus : IEventNotifications
    {
        public event Action<int, int>? OnAttendanceConfirmed;
        public event Action<int, int, decimal>? OnPaymentReceived;
        public event Action<int, int>? OnPollVoteCast;
        public event Action<int, int>? OnPollCreated;
        public event Action<int>? OnEventDetailsChanged;
    }
}
