public class VendorAllocation
{
    public required string VendorName { get; init; }
    public decimal AmountOwed { get; set; }
    public bool ReceiptsReceived { get; set; }
    public List<ReceiptDetails> Receipts { get; set; } = new();
}