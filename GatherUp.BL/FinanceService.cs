using GatherUp.Core;
using GatherUp.Core.DO;

namespace GatherUp.BL
{
    public record FinancialSummary(
        IEnumerable<(string Name, decimal Amount)> PaidParticipants,
        decimal TotalIncome,
        IEnumerable<(string VendorName, decimal AmountOwed)> Vendors,
        decimal TotalExpenses,
        decimal Balance);

    public class FinanceService
    {
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly IMailService _mailService;
        private readonly IEventNotifications _notifications;

        public FinanceService(
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
            _notifications.OnPaymentReceived += HandlePaymentReceived;
        }

        public void RegisterPayment(int eventId, int participantId, decimal amount)
        {
            var participant = _participantRepo.GetById(participantId)
                ?? throw new KeyNotFoundException($"Participant {participantId} not found.");

            participant.HasPaid = true;
            participant.AmountContributed += amount;
            _participantRepo.Update(participant);

            _notifications.OnPaymentReceived?.Invoke(eventId, participantId, amount);
        }

        public void AddVendorDebt(int eventId, string vendorName, decimal amount)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            var vendor = ev.Vendors.FirstOrDefault(v => v.VendorName == vendorName);
            if (vendor != null)
                vendor.AmountOwed += amount;
            else
                ev.Vendors.Add(new VendorAllocation(vendorName) { AmountOwed = amount });

            _eventRepo.Update(ev);
        }

        public void SendPaymentReminders(int eventId, string bankDetails)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && !p.HasPaid)
                .ToList()
                .ForEach(p => _mailService.Send(
                    p!.Email,
                    $"Payment reminder for {ev.Name}",
                    $"Hi {p.Name}, please transfer your payment to: {bankDetails}"));
        }

        public FinancialSummary GetFinancialSummary(int eventId)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            var paidParticipants = GetPaidParticipants(ev);
            var totalIncome = paidParticipants.Sum(x => x.Amount);
            var vendors = ev.Vendors.Select(v => (v.VendorName, v.AmountOwed));
            var totalExpenses = ev.Vendors.Sum(v => v.AmountOwed);

            return new FinancialSummary(paidParticipants, totalIncome, vendors, totalExpenses, totalIncome - totalExpenses);
        }

        // חישוב תקציב דינמי בשרשור LINQ – סעיף 4.1a
        public decimal GetCurrentBalance(int eventId)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            return ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && p.IsAttending == true && p.HasPaid)
                .Sum(p => p!.AmountContributed)
                - ev.Vendors.Sum(v => v.AmountOwed);
        }

        // שיטוח קבלות עם SelectMany – סעיף 4.1b
        public IEnumerable<(string ReceiptNumber, decimal Amount)> GetAllReceiptsSorted(int eventId)
        {
            var ev = _eventRepo.GetById(eventId) ?? throw new KeyNotFoundException($"Event {eventId} not found.");

            return ev.Vendors
                .SelectMany(v => v.Receipts)
                .OrderByDescending(r => r.IssuedDate)
                .Select(r => (r.ReceiptNumber, r.Amount));
        }

        private IEnumerable<(string Name, decimal Amount)> GetPaidParticipants(Event ev) =>
            ev.ParticipantIds
                .Select(id => _participantRepo.GetById(id))
                .Where(p => p != null && p.HasPaid)
                .Select(p => (p!.Name, p.AmountContributed));

        // טיפול באירוע: מי ביקש לקבל מייל על תשלום — המנהל
        private void HandlePaymentReceived(int eventId, int participantId, decimal amount)
        {
            var ev = _eventRepo.GetById(eventId);
            if (ev == null) return;
            var manager = _managerRepo.GetById(ev.EventManagerId);
            var participant = _participantRepo.GetById(participantId);
            if (manager == null || participant == null) return;

            _mailService.Send(manager.Email,
                $"Payment received – {ev.Name}",
                $"{participant.Name} paid {amount:C}.");
        }
    }
}
