using System.Net.Http.Headers;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using GatherUp.API;
using System.Net;
using System;

namespace GatherUp.UnitTests.IntegrationTests
{
    public class FinanceControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public FinanceControllerIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

        [Fact]
        public async Task UploadReceipt_And_Download_Works()
        {
            var client = _factory.CreateClient();

            // create a temp file
            var tmp = Path.Combine(Path.GetTempPath(), "test_receipt.txt");
            await File.WriteAllTextAsync(tmp, "receipt content");

            var receiptNumber = "RN-INT-" + Guid.NewGuid().ToString("N");

            using (var fs = File.OpenRead(tmp))
            {
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(fs);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", "test_receipt.txt");
                content.Add(new StringContent(receiptNumber), "receiptNumber");
                content.Add(new StringContent("42"), "amount");

                var response = await client.PostAsync($"/api/finance/1/vendors/TestVendor/receipts", content);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // download
            var dl = await client.GetAsync($"/api/finance/receipts/{receiptNumber}/file");
            Assert.Equal(HttpStatusCode.OK, dl.StatusCode);

            // cleanup
            File.Delete(tmp);
        }
    }
}
