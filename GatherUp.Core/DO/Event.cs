using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    public class Event : IEntity
    {
        [XmlAttribute]
        public int Id { get; internal set; }

        [XmlAttribute]
        public required string Name { get; set; }

        [XmlElement]
        public required string Description { get; set; }

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