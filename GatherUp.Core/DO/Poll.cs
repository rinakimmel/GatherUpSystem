using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace GatherUp.Core.DO
{
    public class Poll : IEntity
    {
        [XmlAttribute]
        public int Id { get; internal set; }

        [XmlAttribute]
        public required string Name { get; set; }

        [XmlElement]
        public required string Description { get; set; }

        [XmlArray("Questions")]
        [XmlArrayItem("Question")]
        public List<PollQuestion> Questions { get; set; } = new List<PollQuestion>();

        public Poll() { }

        [SetsRequiredMembers]
        public Poll(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}