using System;

namespace GatherUp.Core.DO
{
    [Flags]
    public enum MailingPreference
    {
        None = 0,
        ImportantUpdatesOnly = 1,
        AllUpdates = 2,
        DirectMessages = 4,
        Everything = ImportantUpdatesOnly | AllUpdates | DirectMessages
    }
}