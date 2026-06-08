using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    public class Participant : Person
    {
        public bool? IsAttending { get; set; }
        public bool HasPaid { get; set; }
        public decimal AmountContributed { get; set; }

        [XmlElement]
        public MailingPreference MailingPreferences { get; set; }

        public Participant() { }

        [SetsRequiredMembers]
        public Participant(int id, string name, string email)
            : base(id, name, email)
        {
            MailingPreferences = MailingPreference.ImportantUpdatesOnly;
        }
    }
}