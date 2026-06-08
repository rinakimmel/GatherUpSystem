using System.Diagnostics.CodeAnalysis;

namespace GatherUp.Core.DO
{
    public class EventHost : Person
    {
        public EventHost() { }

        [SetsRequiredMembers]
        public EventHost(int id, string name, string email)
            : base(id, name, email) { }
    }
}