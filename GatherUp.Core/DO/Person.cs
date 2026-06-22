using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace GatherUp.Core.DO
{
    [XmlInclude(typeof(EventManager))]
    [XmlInclude(typeof(EventHost))]
    [XmlInclude(typeof(Participant))]
    public abstract class Person : IEntity
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute]
        public string Email { get; set; } = string.Empty;

        public Person()
        {
        }

        [SetsRequiredMembers]
        protected Person(int id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;
        }
    }
}