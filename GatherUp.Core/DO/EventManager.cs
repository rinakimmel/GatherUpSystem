using System.Diagnostics.CodeAnalysis;

namespace GatherUp.Core.DO
{
    public class EventManager : Person
    {
        public EventManager() { }

        [SetsRequiredMembers]
        public EventManager(int id, string name, string email)
            : base(id, name, email) { }
    }
}