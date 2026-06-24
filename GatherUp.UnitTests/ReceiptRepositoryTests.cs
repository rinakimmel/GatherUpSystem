using GatherUp.Infrastructure.Data;
using GatherUp.Core.DO;
using System.Threading.Tasks;
using System.IO;
using Xunit;

namespace GatherUp.UnitTests
{
    public class ReceiptRepositoryTests
    {
        [Fact]
        public async Task AddAsync_CopiesFileAndStoresMetadata()
        {
            string testDir = Path.Combine(System.AppContext.BaseDirectory, "ReceiptTestData");
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
            Directory.CreateDirectory(testDir);

            // create dummy file
            string sourceFile = Path.Combine(testDir, "dummy.txt");
            await File.WriteAllTextAsync(sourceFile, "hello");

            var repo = new ReceiptRepository(testDir);
            var receipt = new ReceiptDetails("RN-1", 42m, System.DateTime.UtcNow, sourceFile);

            await repo.AddAsync(receipt);

            var stored = await repo.GetByReceiptNumberAsync("RN-1");
            Assert.NotNull(stored);
            Assert.Equal(42m, stored.Amount);
            Assert.False(string.IsNullOrEmpty(stored.FilePath));
            Assert.True(File.Exists(stored.FilePath));

            // cleanup
            Directory.Delete(testDir, true);
        }
    }
}
