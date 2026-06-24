using GatherUp.BL;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using GatherUp.Infrastructure.Data;
using System.Threading.Tasks;
using Xunit;

namespace GatherUp.UnitTests
{
    public class FinanceServiceTests
    {
        [Fact]
        public async Task AddReceiptToVendor_PersistsReceipt()
        {
            // Arrange
            string dataDir = System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData");
            var participantRepo = new MemoryRepository<Participant>();
            var managerRepo = new MemoryRepository<EventManager>();
            var eventRepo = new MemoryRepository<Event>();
            var receiptRepo = new ReceiptRepository(dataDir);
            var mailService = new FileMailService(System.IO.Path.Combine(dataDir, "mail.txt"));
            var notifications = new BL.EventNotificationBus();

            var finance = new FinanceService(participantRepo, managerRepo, eventRepo, receiptRepo, mailService, notifications);

            // seed an event and vendor
            var ev = new Event(0, "Test Event", "desc");
            await eventRepo.AddAsync(ev);
            ev.Vendors.Add(new VendorAllocation("Vendor A") { AmountOwed = 100 });
            await eventRepo.UpdateAsync(ev);

            var receipt = new ReceiptDetails("R-100", 100m, System.DateTime.UtcNow, null);

            // Act
            await finance.AddReceiptToVendorAsync(ev.Id, "Vendor A", receipt);

            // Assert
            var stored = await receiptRepo.GetByReceiptNumberAsync("R-100");
            Assert.NotNull(stored);
            Assert.Equal(100m, stored.Amount);
        }
    }
}
