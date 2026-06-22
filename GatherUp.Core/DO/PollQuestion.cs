using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
namespace GatherUp.Core.DO
{
    public class PollQuestion
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlElement]
        public string QuestionContent { get; set; } = string.Empty;

        [XmlArray("ChoiceOptions")]
        [XmlArrayItem("Choice")]
        public List<string> ChoiceOptions { get; set; } = new List<string>();

        [XmlArray("ParticipantChoices")]
        [XmlArrayItem("Choice")]
        public List<ParticipantChoice> ParticipantChoices { get; set; } = new List<ParticipantChoice>();

        public PollQuestion() { }

        [SetsRequiredMembers]
        public PollQuestion(int id, string questionContent)
        {
            Id = id;
            QuestionContent = questionContent;
        }
    }

    [XmlType("Choice")]
    public class ParticipantChoice
    {
        [XmlAttribute]
        public int ParticipantId { get; set; }

        [XmlAttribute]
        public string Choice { get; set; } = string.Empty;

        public ParticipantChoice() { }

        public ParticipantChoice(int participantId, string choice)
        {
            ParticipantId = participantId;
            Choice = choice;
        }
    }
}