using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    public class Event : IEntity
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlElement]
        public string Description { get; set; } = string.Empty;

        [XmlArray("ParticipantIds")]
        [XmlArrayItem("ParticipantId")]
        public List<int> ParticipantIds { get; set; } = new List<int>();

        [XmlAttribute]
        public int EventManagerId { get; set; }

        [XmlAttribute]
        public int EventHostId { get; set; }

        [XmlArray("PollIds")]
        [XmlArrayItem("PollId")]
        public List<int> PollIds { get; set; } = new List<int>();

        [XmlArray("Vendors")]
        [XmlArrayItem("Vendor")]
        public List<VendorAllocation> Vendors { get; set; } = new List<VendorAllocation>();

        // Optional event details
        [XmlElement("Date", IsNullable = false)]
        public string? DateString
        {
            get => Date.HasValue ? Date.Value.ToString("o") : null;
            set => Date = string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
        }
        [XmlIgnore]
        public DateTime? Date { get; set; }

        [XmlAttribute]
        public string Location { get; set; } = string.Empty;

        [XmlElement("Price", IsNullable = false)]
        public string? PriceString
        {
            get => PricePerParticipant.HasValue ? PricePerParticipant.Value.ToString() : null;
            set => PricePerParticipant = string.IsNullOrEmpty(value) ? null : decimal.Parse(value);
        }
        [XmlIgnore]
        public decimal? PricePerParticipant { get; set; }

        [XmlElement]
        public string PaymentMethods { get; set; } = string.Empty;

        public Event()
        {
        }

        [SetsRequiredMembers]
        public Event(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}