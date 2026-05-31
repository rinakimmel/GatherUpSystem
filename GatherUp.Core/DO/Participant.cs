public class Participant : Person
{
    public bool IsAttending { get; set; }
    public bool HasPaid { get; set; }
    public decimal AmountContributed { get; set; }
    public string? MailingPreferences { get; set; }
}