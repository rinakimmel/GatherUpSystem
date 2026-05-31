using System;

class Program
{
    static void Main()
    {
        // Create in-memory repositories
        var managerRepo = new MemoryRepository<EventManager>();
        var hostRepo = new MemoryRepository<EventHost>();
        var participantRepo = new MemoryRepository<Participant>();
        var eventRepo = new MemoryRepository<Event>();
        var vendorRepo = new MemoryRepository<VendorAllocation>();

        // Seed initial mock data (calls your Initialize.SeedAll)
        Initialize.SeedAll(managerRepo, hostRepo, participantRepo, eventRepo, vendorRepo);

        // Add 3 new participants
        var p3 = new Participant
        {
            Id = 5,
            Name = "Anna Katz",
            Email = "anna.katz@example.com",
            IsAttending = true,
            HasPaid = false,
            AmountContributed = 0.00m,
            MailingPreferences = "Email"
        };

        var p4 = new Participant
        {
            Id = 6,
            Name = "Oren Bar",
            Email = "oren.bar@example.com",
            IsAttending = true,
            HasPaid = true,
            AmountContributed = 50.00m,
            MailingPreferences = "Email"
        };

        var p5 = new Participant
        {
            Id = 7,
            Name = "Leah Mizrahi",
            Email = "leah.mizrahi@example.com",
            IsAttending = false,
            HasPaid = false,
            AmountContributed = 0.00m,
            MailingPreferences = "Postal"
        };

        participantRepo.Add(p3);
        participantRepo.Add(p4);
        participantRepo.Add(p5);

        // Retrieve one participant by ID and print
        var found = participantRepo.GetById(5);
        Console.WriteLine("Retrieved participant (Id=5):");
        if (found != null)
        {
            Console.WriteLine($"{found.Id}: {found.Name} <{found.Email}> Attending:{found.IsAttending} Paid:{found.HasPaid} Amount:{found.AmountContributed:C}");
        }
        else
        {
            Console.WriteLine("Participant not found.");
        }

        // Print all participants to verify
        Console.WriteLine();
        Console.WriteLine("All participants:");
        foreach (var p in participantRepo.GetAll())
        {
            Console.WriteLine($"{p.Id}: {p.Name} <{p.Email}> Attending:{p.IsAttending} Paid:{p.HasPaid} Amount:{p.AmountContributed:C}");
        }
    }
}