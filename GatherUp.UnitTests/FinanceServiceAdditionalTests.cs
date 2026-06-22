using GatherUp.BL;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using GatherUp.Infrastructure.Data;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using GatherUp.Core;
using System;

namespace GatherUp.UnitTests
{
    public class FinanceServiceAdditionalTests
    {
        [Fact]
        public async Task RegisterPayment_SendsEmailToManager()
        {
            // Arrange
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            string tmp = Path.Combine(AppContext.BaseDirectory, "FinanceTestData1");
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            Directory.CreateDirectory(tmp);
            var receiptRepo = new ReceiptRepository(tmp);
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var finance = new FinanceService(participantRepo, managerRepo, eventRepo, receiptRepo, mailService, notifications);

            var manager = new EventManager(0, "Mgr", "mgr@example.com");
            await managerRepo.AddAsync(manager);

            var participant = new Participant(0, "P", "p@example.com");
            await participantRepo.AddAsync(participant);

            var ev = new Event(0, "E", "d");
            ev.EventManagerId = manager.Id;
            ev.ParticipantIds.Add(participant.Id);
            await eventRepo.AddAsync(ev);

            // Act
            await finance.RegisterPaymentAsync(ev.Id, participant.Id, 50m);

            // Assert
            Assert.Contains(mailService.Sent, s => s.To == manager.Email && s.Subject.Contains("Payment received"));

            // cleanup
            Directory.Delete(tmp, true);
        }

        [Fact]
        public async Task AddVendorDebt_UpdatesExistingAndAddsNewVendor()
        {
            // Arrange
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            string tmp = Path.Combine(AppContext.BaseDirectory, "FinanceTestData2");
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            Directory.CreateDirectory(tmp);
            var receiptRepo = new ReceiptRepository(tmp);
            var mailService = new TestMailService();
            var notifications = new BL.EventNotificationBus();

            var finance = new FinanceService(participantRepo, managerRepo, eventRepo, receiptRepo, mailService, notifications);

            var ev = new Event(0, "E2", "d");
            ev.Vendors.Add(new VendorAllocation("VendorA") { AmountOwed = 100m });
            await eventRepo.AddAsync(ev);

            // Act: add debt to existing vendor
            await finance.AddVendorDebtAsync(ev.Id, "VendorA", 50m);
            var evAfter = await eventRepo.GetByIdAsync(ev.Id);
            var v = evAfter.Vendors.Find(x => x.VendorName == "VendorA");
            Assert.NotNull(v);
            Assert.Equal(150m, v.AmountOwed);

            // Act: add new vendor
            await finance.AddVendorDebtAsync(ev.Id, "VendorB", 200m);
            evAfter = await eventRepo.GetByIdAsync(ev.Id);
            var vb = evAfter.Vendors.Find(x => x.VendorName == "VendorB");
            Assert.NotNull(vb);
            Assert.Equal(200m, vb.AmountOwed);

            // cleanup
            Directory.Delete(tmp, true);
        }

        // simple in-memory mail service for tests
        private class TestMailService : IMailService
        {
            public readonly System.Collections.Generic.List<(string To, string Subject, string Body)> Sent = new();
            public void Send(string toEmail, string subject, string body) => Sent.Add((toEmail, subject, body));
            public Task SendAsync(string toEmail, string subject, string body)
            {
                Send(toEmail, subject, body);
                return Task.CompletedTask;
            }
        }
    }
}
