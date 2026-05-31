public class Event
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<int> ParticipantIds { get; set; } = new();
    public int EventManagerId { get; set; }
    public int EventHostId { get; set; }
    public List<VendorAllocation> Vendors { get; set; } = new();
    public List<Poll> Polls { get; set; } = new();
}