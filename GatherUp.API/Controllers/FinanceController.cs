using GatherUp.BL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using GatherUp.Core.DO;
using System.Threading.Tasks;
using System.IO;
using System;
using GatherUp.Core;

namespace GatherUp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinanceController : ControllerBase
    {
        private readonly FinanceService _financeService;
        private readonly IReceiptRepository _receiptRepo;

        public FinanceController(FinanceService financeService, IReceiptRepository receiptRepo)
        {
            _financeService = financeService;
            _receiptRepo = receiptRepo;
        }

        [HttpPost("{eventId}/payment/{participantId}")]
        public async Task<IActionResult> RegisterPayment(int eventId, int participantId, [FromQuery] decimal amount)
        {
            await _financeService.RegisterPaymentAsync(eventId, participantId, amount);
            return NoContent();
        }

        [HttpPost("{eventId}/vendor-debt")]
        public async Task<IActionResult> AddVendorDebt(int eventId, [FromQuery] string vendorName, [FromQuery] decimal amount)
        {
            await _financeService.AddVendorDebtAsync(eventId, vendorName, amount);
            return NoContent();
        }

        [HttpGet("{eventId}/summary")]
        public async Task<IActionResult> GetSummary(int eventId)
        {
            var s = await _financeService.GetFinancialSummaryAsync(eventId);
            return Ok(s);
        }

        // Upload a receipt file and attach to vendor for the event
        [HttpPost("{eventId}/vendors/{vendorName}/receipts")]
        public async Task<IActionResult> UploadReceipt(int eventId, string vendorName, [FromForm] IFormFile file, [FromForm] string receiptNumber, [FromForm] decimal amount, [FromForm] DateTime? issuedDate)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "File is required." });

            // check duplicate
            var existing = await _receiptRepo.GetByReceiptNumberAsync(receiptNumber);
            if (existing != null)
                return Conflict(new { error = "Receipt with this number already exists." });

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
            await using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }

            var receipt = new ReceiptDetails(receiptNumber, amount, issuedDate ?? DateTime.UtcNow, tempPath);
            await _financeService.AddReceiptToVendorAsync(eventId, vendorName, receipt);

            // delete temp file after repository copied it (best-effort)
            try { System.IO.File.Delete(tempPath); } catch { }

            return Ok(new { receiptNumber = receiptNumber });
        }

        // Download stored receipt file by receipt number
        [HttpGet("receipts/{receiptNumber}/file")]
        public async Task<IActionResult> DownloadReceiptFile(string receiptNumber)
        {
            var receipt = await _receiptRepo.GetByReceiptNumberAsync(receiptNumber);
            if (receipt == null || string.IsNullOrEmpty(receipt.FilePath))
                return NotFound();

            if (!System.IO.File.Exists(receipt.FilePath))
                return NotFound();

            var contentType = "application/octet-stream";
            var fileName = Path.GetFileName(receipt.FilePath);
            var fs = System.IO.File.OpenRead(receipt.FilePath);
            return File(fs, contentType, fileName);
        }
    }
}
