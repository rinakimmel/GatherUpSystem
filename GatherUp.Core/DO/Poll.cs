public class Poll
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<PollQuestion> Questions { get; set; } = new();
}