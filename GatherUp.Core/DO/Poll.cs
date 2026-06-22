using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace GatherUp.Core.DO
{
    public class Poll : IEntity
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlElement]
        public string Description { get; set; } = string.Empty;

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