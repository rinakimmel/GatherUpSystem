using System;
using System.Collections.Generic;

public static class Initialize
{
    public static void SeedAll(
        MemoryRepository<EventManager> managerRepo,
        MemoryRepository<EventHost> hostRepo,
        MemoryRepository<Participant> participantRepo,
        MemoryRepository<Event> eventRepo,
        MemoryRepository<VendorAllocation> vendorRepo)
    {
        if (managerRepo == null) throw new ArgumentNullException(nameof(managerRepo));
        if (hostRepo == null) throw new ArgumentNullException(nameof(hostRepo));
        if (participantRepo == null) throw new ArgumentNullException(nameof(participantRepo));
        if (eventRepo == null) throw new ArgumentNullException(nameof(eventRepo));
        if (vendorRepo == null) throw new ArgumentNullException(nameof(vendorRepo));

        // Participants
        var participant1 = new Participant
        {
            Id = 1,
            Name = "Sara Cohen",
            Email = "sara.cohen@example.com",
            IsAttending = true,
            HasPaid = true,
            AmountContributed = 75.00m,
            MailingPreferences = "Email"
        };

        var participant2 = new Participant
        {
            Id = 2,
            Name = "David Levi",
            Email = "david.levi@example.com",
            IsAttending = false,
            HasPaid = false,
            AmountContributed = 0.00m,
            MailingPreferences = "Postal"
        };

        // Event manager and host (real-looking emails)
        var manager = new EventManager
        {
            Id = 3,
            Name = "Emily Brown",
            Email = "emily.brown@example.com"
        };

        var host = new EventHost
        {
            Id = 4,
            Name = "Michael Green",
            Email = "michael.green@example.com"
        };

        // Polls and questions
        var poll1 = new Poll
        {
            Id = 10,
            Name = "Food Preferences",
            Description = "Choose your preferred cuisine",
            Questions = new List<PollQuestion>
            {
                new PollQuestion
                {
                    Id = 100,
                    QuestionContent = "Which cuisine do you prefer?",
                    ChoiceOptions = new List<string> { "Italian", "Mediterranean", "Vegan" },
                    ParticipantChoices = new Dictionary<int, string> { { 1, "Italian" } }
                }
            }
        };

        var poll2 = new Poll
        {
            Id = 11,
            Name = "Workshops",
            Description = "Pick one workshop you'd like to attend",
            Questions = new List<PollQuestion>
            {
                new PollQuestion
                {
                    Id = 110,
                    QuestionContent = "Which workshop interests you most?",
                    ChoiceOptions = new List<string> { "Product Design", "Cloud Basics", "Testing" },
                    ParticipantChoices = new Dictionary<int, string> { { 2, "Testing" } }
                }
            }
        };

        // Vendor with debt
        var vendor = new VendorAllocation
        {
            VendorName = "Sunrise Catering",
            AmountOwed = 1200.00m,
            ReceiptsReceived = false,
            Receipts = new List<ReceiptDetails>()
        };

        // Event that ties everything together
        var @event = new Event
        {
            Id = 1000,
            Name = "Summer Community Meetup",
            Description = "Annual meetup for the local community.",
            ParticipantIds = new List<int> { participant1.Id, participant2.Id },
            EventManagerId = manager.Id,
            EventHostId = host.Id,
            Vendors = new List<VendorAllocation> { vendor },
            Polls = new List<Poll> { poll1, poll2 }
        };

        // Persist into provided in-memory repositories
        participantRepo.Add(participant1);
        participantRepo.Add(participant2);
        managerRepo.Add(manager);
        hostRepo.Add(host);
        vendorRepo.Add(vendor);
        eventRepo.Add(@event);
    }
}