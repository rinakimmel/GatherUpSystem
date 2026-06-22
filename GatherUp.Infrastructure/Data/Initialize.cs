using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GatherUp.Core.DO; // ודאי שמרחב השמות מיובא

namespace GatherUp.Infrastructure.Data
{
    public static class Initialize
    {
        // גרסה אסינכרונית לאיתחול נתונים
        public static async Task SeedAllAsync(
            IRepository<EventManager> managerRepo,
            IRepository<EventHost> hostRepo,
            IRepository<Participant> participantRepo,
            IRepository<Event> eventRepo,
            IRepository<Poll> pollRepo)
        {
            var manager = new EventManager(0, "Emily Brown", "emily.brown@example.com");
            var host = new EventHost(0, "Michael Green", "michael.green@example.com");

            var participant1 = new Participant(0, "Sara Cohen", "sara.cohen@example.com")
            {
                IsAttending = true,
                HasPaid = true,
                AmountContributed = 75.00m,
                MailingPreferences = MailingPreference.AllUpdates
            };

            var participant2 = new Participant(0, "David Levi", "david.levi@example.com")
            {
                IsAttending = false,
                HasPaid = false,
                AmountContributed = 0.00m,
                MailingPreferences = MailingPreference.ImportantUpdatesOnly
            };

            await managerRepo.AddAsync(manager);
            await hostRepo.AddAsync(host);
            await participantRepo.AddAsync(participant1);
            await participantRepo.AddAsync(participant2);

            var poll1 = new Poll(0, "Food Preferences", "Choose your preferred cuisine");
            poll1.Questions.Add(new PollQuestion(0, "Which cuisine do you prefer?")
            {
                ChoiceOptions = new List<string> { "Italian", "Mediterranean", "Vegan" },
                ParticipantChoices = new List<ParticipantChoice>
                {
                    new ParticipantChoice(participant1.Id, "Italian")
                }
            });

            var poll2 = new Poll(0, "Workshops", "Pick one workshop");
            poll2.Questions.Add(new PollQuestion(0, "Which workshop interests you most?")
            {
                ChoiceOptions = new List<string> { "Product Design", "Cloud Basics", "Testing" },
                ParticipantChoices = new List<ParticipantChoice>
                {
                    new ParticipantChoice(participant2.Id, "Testing")
                }
            });

            await pollRepo.AddAsync(poll1);
            await pollRepo.AddAsync(poll2);

            var vendor = new VendorAllocation("Sunrise Catering")
            {
                AmountOwed = 1200.00m,
                ReceiptsReceived = false
            };

            var mainEvent = new Event(0, "Summer Community Meetup", "Annual meetup for the local community.")
            {
                EventManagerId = manager.Id,
                EventHostId = host.Id,
                ParticipantIds = new List<int> { participant1.Id, participant2.Id },
                PollIds = new List<int> { poll1.Id, poll2.Id },
                Vendors = new List<VendorAllocation> { vendor }
            };

            await eventRepo.AddAsync(mainEvent);
        }
    }
}