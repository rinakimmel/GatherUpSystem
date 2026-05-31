public record ReceiptDetails
{
    public required string ReceiptNumber { get; init; }
    public required decimal Amount { get; init; }
    public required DateTime IssuedDate { get; init; }
}