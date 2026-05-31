public class PollQuestion
{
    public required int Id { get; init; }
    public required string QuestionContent { get; init; }
    public List<string> ChoiceOptions { get; set; } = new();
    public Dictionary<int, string> ParticipantChoices { get; set; } = new();
}