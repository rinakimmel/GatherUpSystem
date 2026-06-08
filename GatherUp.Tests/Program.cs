using System;
using GatherUp.Core.DO;
using GatherUp.Infrastructure.Data;

namespace GatherUp.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== GatherUp System Initialization ===\n");

            // 1. יצירת אובייקטים מסוג ה-MemoryRepository (במקום XML בינתיים)
            IRepository<EventManager> managerRepo = new MemoryRepository<EventManager>();
            IRepository<EventHost> hostRepo = new MemoryRepository<EventHost>();
            IRepository<Participant> participantRepo = new MemoryRepository<Participant>();
            IRepository<Event> eventRepo = new MemoryRepository<Event>();
            IRepository<Poll> pollRepo = new MemoryRepository<Poll>();

            // 2. קריאה לפונקציית האיתחול עם המחלקות שיצרנו
            Console.WriteLine("Seeding initial data...");
            Initialize.SeedAll(managerRepo, hostRepo, participantRepo, eventRepo, pollRepo);
            Console.WriteLine("✓ Initial data seeded successfully!\n");


            // 3. הוספת 3 משתתפים חדשים למערכת תוך שימוש בבנאים המוגנים (SetsRequiredMembers)
            Console.WriteLine("Adding new participants...");

            var p3 = new Participant(0, "Anna Katz", "anna.katz@example.com")
            {
                IsAttending = true,
                HasPaid = false,
                AmountContributed = 0m,
                MailingPreferences = MailingPreference.Everything
            };

            var p4 = new Participant(0, "Oren Bar", "oren.bar@example.com")
            {
                IsAttending = true,
                HasPaid = true,
                AmountContributed = 50m,
                MailingPreferences = MailingPreference.ImportantUpdatesOnly
            };

            var p5 = new Participant(0, "Leah Mizrahi", "leah.mizrahi@example.com")
            {
                IsAttending = false,
                HasPaid = false,
                AmountContributed = 0m,
                MailingPreferences = MailingPreference.None
            };

            participantRepo.Add(p3);
            participantRepo.Add(p4);
            participantRepo.Add(p5);
            Console.WriteLine("✓ Added 3 new participants successfully!\n");


            // 4. שליפת אחד המשתתפים לפי ה-Id שלו (נשלוף את אנה קץ שקיבלה Id 3)
            Console.WriteLine("--- Retrieving Participant by ID ---");
            var found = participantRepo.GetById(3);
            if (found != null)
            {
                Console.WriteLine($"Found: {found.Name} (Email: {found.Email})");
            }
            else
            {
                Console.WriteLine("Participant not found.");
            }
            Console.WriteLine();


            // 5. הדפסת רשימת כל המשתתפים למסך
            Console.WriteLine("--- All Participants in System ---");
            foreach (var p in participantRepo.GetAll())
            {
                // הוספת הצגת סטטוס ההגעה בצורה ברורה
                string attendingStatus = p.IsAttending.HasValue
                    ? (p.IsAttending.Value ? "Yes" : "No")
                    : "Not Responded";

                Console.WriteLine($"[ID: {p.Id}] {p.Name} | Email: {p.Email} | Attending: {attendingStatus} | Paid: {p.AmountContributed:C}");
            }

            Console.WriteLine("\n=== Tests Completed Successfully ===");
            Console.ReadLine(); // משאיר את החלון פתוח כדי שתוכלי לקרוא את התוצאות
        }
    }
}