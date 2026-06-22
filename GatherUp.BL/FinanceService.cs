using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Core.Exceptions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace GatherUp.BL
{
    public record FinancialSummary(
        IEnumerable<(string Name, decimal Amount)> PaidParticipants,
        decimal TotalIncome,
        IEnumerable<(string VendorName, decimal AmountOwed)> Vendors,
        decimal TotalExpenses,
        decimal Balance);

    public partial class FinanceService
    {
        private readonly IRepository<Participant> _participantRepo;
        private readonly IRepository<EventManager> _managerRepo;
        private readonly IRepository<Event> _eventRepo;
        private readonly IMailService _mailService;
        private readonly IEventNotifications _notifications;
        private readonly IReceiptRepository _receiptRepo;

        public FinanceService(
            IRepository<Participant> participantRepo,
            IRepository<EventManager> managerRepo,
            IRepository<Event> eventRepo,
            IReceiptRepository receiptRepo,
            IMailService mailService,
            IEventNotifications notifications)
        {
            _participantRepo = participantRepo;
            _managerRepo = managerRepo;
            _eventRepo = eventRepo;
            _receiptRepo = receiptRepo;
            _mailService = mailService;
            _notifications = notifications;

            // register async handler to avoid blocking
            _notifications.OnPaymentReceived += (eventId, participantId, amount) => _ = HandlePaymentReceivedAsync(eventId, participantId, amount);
        }

        public async Task RegisterPaymentAsync(int eventId, int participantId, decimal amount)
        {
            var participant = await _participantRepo.GetByIdAsync(participantId)
                ?? throw new NotFoundException($"Participant {participantId} not found.");

            participant.HasPaid = true;
            participant.AmountContributed += amount;
            await _participantRepo.UpdateAsync(participant);

            _notifications.RaisePaymentReceived(eventId, participantId, amount);
        }

        public async Task AddVendorDebtAsync(int eventId, string vendorName, decimal amount)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            var vendor = ev.Vendors.FirstOrDefault(v => v.VendorName == vendorName);
            if (vendor != null)
                vendor.AmountOwed += amount;
            else
                ev.Vendors.Add(new VendorAllocation(vendorName) { AmountOwed = amount });

            await _eventRepo.UpdateAsync(ev);
        }

        public async Task SendPaymentRemindersAsync(int eventId, string bankDetails)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            var participants = await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id)));
            var pending = participants.Where(p => p != null && !p.HasPaid).Cast<Participant>();

            foreach (var p in pending)
            {
                await _mailService.SendAsync(p.Email, $"Payment reminder for {ev.Name}", $"Hi {p.Name}, please transfer your payment to: {bankDetails}");
            }
        }

        public async Task<FinancialSummary> GetFinancialSummaryAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");

            var participantObjs = (await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id))))
                .Where(p => p != null && p.HasPaid)
                .Select(p => p!);

            var paidParticipants = participantObjs.Select(p => (Name: p.Name, Amount: p.AmountContributed));
            var totalIncome = paidParticipants.Sum(x => x.Amount);
            var vendors = ev.Vendors.Select(v => (v.VendorName, v.AmountOwed));
            var totalExpenses = ev.Vendors.Sum(v => v.AmountOwed);

            return new FinancialSummary(paidParticipants, totalIncome, vendors, totalExpenses, totalIncome - totalExpenses);
        }

        public async Task<decimal> GetCurrentBalanceAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");
            var participants = await Task.WhenAll(ev.ParticipantIds.Select(id => _participantRepo.GetByIdAsync(id)));
            var income = participants.Where(p => p != null && p.IsAttending == true && p.HasPaid).Sum(p => p!.AmountContributed);
            var expenses = ev.Vendors.Sum(v => v.AmountOwed);
            return income - expenses;
        }

        public async Task<IEnumerable<(string ReceiptNumber, decimal Amount)>> GetAllReceiptsSortedAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");
            return ev.Vendors.SelectMany(v => v.Receipts).OrderByDescending(r => r.IssuedDate).Select(r => (r.ReceiptNumber, r.Amount));
        }

        // async event handler
        private async Task HandlePaymentReceivedAsync(int eventId, int participantId, decimal amount)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return;
            var manager = await _managerRepo.GetByIdAsync(ev.EventManagerId);
            var participant = await _participantRepo.GetByIdAsync(participantId);
            if (manager == null || participant == null) return;

            await _mailService.SendAsync(manager.Email, $"Payment received – {ev.Name}", $"{participant.Name} paid {amount:C}.");
        }

        public async Task AddReceiptToVendorAsync(int eventId, string vendorName, ReceiptDetails receipt)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId) ?? throw new NotFoundException($"Event {eventId} not found.");
            var vendor = ev.Vendors.FirstOrDefault(v => v.VendorName == vendorName) ?? throw new NotFoundException($"Vendor {vendorName} not found.");

            vendor.Receipts.Add(receipt);
            vendor.ReceiptsReceived = true;

            await _eventRepo.UpdateAsync(ev);

            // persist receipt xml entry and file copying should be done by infrastructure-specific repository
            await _receiptRepo.AddAsync(receipt);
        }
    }

    public partial class FinanceService
    {
        // synchronous wrappers
        public void RegisterPayment(int eventId, int participantId, decimal amount) =>
            RegisterPaymentAsync(eventId, participantId, amount).GetAwaiter().GetResult();

        public void AddVendorDebt(int eventId, string vendorName, decimal amount) =>
            AddVendorDebtAsync(eventId, vendorName, amount).GetAwaiter().GetResult();

        public void SendPaymentReminders(int eventId, string bankDetails) =>
            SendPaymentRemindersAsync(eventId, bankDetails).GetAwaiter().GetResult();

        public FinancialSummary GetFinancialSummary(int eventId) =>
            GetFinancialSummaryAsync(eventId).GetAwaiter().GetResult();

        public decimal GetCurrentBalance(int eventId) =>
            GetCurrentBalanceAsync(eventId).GetAwaiter().GetResult();

        public IEnumerable<(string ReceiptNumber, decimal Amount)> GetAllReceiptsSorted(int eventId) =>
            GetAllReceiptsSortedAsync(eventId).GetAwaiter().GetResult();
    }
}
