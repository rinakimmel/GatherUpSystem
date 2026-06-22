using GatherUp.BL;
using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using GatherUp.Infrastructure.Data;
using System.Threading.Tasks;
using System.Linq;

namespace GatherUp.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== GatherUp System – Full BL Simulation ===\n");

            // --- הגדרת נתיבי XML ---
            string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            string mailLog = Path.Combine(AppContext.BaseDirectory, "mail_log.txt");

            // --- הקמת ה-Infrastructure (XML Repositories) ---
            IRepository<EventManager> managerRepo  = new XMLRepository<EventManager>(dataDir, "EventManagers");
            IRepository<EventHost>    hostRepo      = new XMLRepository<EventHost>(dataDir, "EventHosts");
            IRepository<Participant>  participantRepo = new XMLRepository<Participant>(dataDir, "Participants");
            IRepository<Event>        eventRepo     = new XMLRepository<Event>(dataDir, "Events");
            IRepository<Poll>         pollRepo      = new XMLRepository<Poll>(dataDir, "Polls");
            IReceiptRepository        receiptRepo   = new ReceiptRepository(dataDir);

            // --- שירות המיילים מ-Infrastructure ---
            IMailService mailService = new FileMailService(mailLog);

            // --- אתחול נתוני ברירת מחדל אם אין עדיין נתונים ---
            var events = await eventRepo.GetAllAsync();
            if (!events.Any())
            {
                Console.WriteLine("Seeding initial data...");
                await Initialize.SeedAllAsync(managerRepo, hostRepo, participantRepo, eventRepo, pollRepo);
                Console.WriteLine("✓ Data seeded.\n");
            }

            // --- הזרקת תלויות: אוטובוס האירועים (מחזיק את ה-events בלבד) ---
            var notificationBus = new EventNotificationBus();

            // --- הזרקת תלויות: הקמת שירותי ה-BL ---
            // כל שירות נרשם בבנאי שלו לאירועים הרלוונטיים לו
            var participantService = new ParticipantService(participantRepo, managerRepo, eventRepo, mailService, notificationBus);
            var financeService     = new FinanceService(participantRepo, managerRepo, eventRepo, receiptRepo, mailService, notificationBus);
            var pollService        = new PollService(pollRepo, participantRepo, managerRepo, eventRepo, mailService, notificationBus);

            int eventId = (await eventRepo.GetAllAsync()).First().Id;

            // ===================================================================
            // מסך: ניהול אירוע – לחיצה על "הוסף משתתף"
            // ===================================================================
            Console.WriteLine(">> [מסך ניהול אירוע] לחיצה על 'הוסף משתתף'");
            var newParticipant = new Participant(0, "Yael Shapira", "yael.shapira@example.com")
            {
                MailingPreferences = MailingPreference.AllUpdates
            };
            await participantService.AddParticipantToEventAsync(eventId, newParticipant);
            Console.WriteLine($"   ✓ נוסף: {newParticipant.Name} (ID: {newParticipant.Id})\n");

            // ===================================================================
            // מסך: כרטיס משתתף – לחיצה על "אשר הגעה"
            // (יופעל OnAttendanceConfirmed → מייל למנהל)
            // ===================================================================
            Console.WriteLine(">> [מסך משתתף] לחיצה על 'אשר הגעה'");
            await participantService.ConfirmAttendanceAsync(eventId, newParticipant.Id, true);
            Console.WriteLine("   ✓ הגעה אושרה. מייל נשלח למנהל (ראה mail_log.txt)\n");

            // ===================================================================
            // מסך: ניהול אירוע – לחיצה על "שלח תזכורות לאישור הגעה"
            // ===================================================================
            Console.WriteLine(">> [מסך ניהול אירוע] לחיצה על 'שלח תזכורות'");
            await participantService.SendInvitationRemindersAsync(eventId);
            Console.WriteLine("   ✓ תזכורות נשלחו למשתתפים שלא השיבו.\n");

            // ===================================================================
            // מסך: ניהול פיננסי – לחיצה על "רשום תשלום"
            // (יופעל OnPaymentReceived → מייל למנהל)
            // ===================================================================
            Console.WriteLine(">> [מסך ניהול פיננסי] לחיצה על 'רשום תשלום'");
            await financeService.RegisterPaymentAsync(eventId, newParticipant.Id, 120m);
            Console.WriteLine("   ✓ תשלום 120₪ נרשם. מייל נשלח למנהל.\n");

            // ===================================================================
            // מסך: ניהול ספקים – לחיצה על "הוסף חוב לספק"
            // ===================================================================
            Console.WriteLine(">> [מסך ספקים] לחיצה על 'הוסף חוב לספק'");
            await financeService.AddVendorDebtAsync(eventId, "Sunrise Catering", 300m);
            Console.WriteLine("   ✓ חוב 300₪ נוסף לספק Sunrise Catering.\n");

            // ===================================================================
            // מסך: ניהול פיננסי – לחיצה על "הצג סיכום תקציב"
            // ===================================================================
            Console.WriteLine(">> [מסך ניהול פיננסי] לחיצה על 'סיכום תקציב'");
            var summary = await financeService.GetFinancialSummaryAsync(eventId);
            Console.WriteLine($"   הכנסות: {summary.TotalIncome:C}  |  הוצאות: {summary.TotalExpenses:C}  |  מאזן: {summary.Balance:C}");
            Console.WriteLine($"   יתרה נוכחית (LINQ): {await financeService.GetCurrentBalanceAsync(eventId):C}\n");

            // ===================================================================
            // מסך: ניהול אירוע – לחיצה על "יצור סקר חדש"
            // (יופעל OnPollCreated → מייל למשתתפים שביקשו AllUpdates)
            // ===================================================================
            Console.WriteLine(">> [מסך ניהול אירוע] לחיצה על 'יצור סקר חדש'");
            var newPoll = await pollService.CreatePollAsync(
                eventId,
                "Venue Poll",
                "Where should we hold the event?",
                new[]
                {
                    ("Where should we meet?", (IEnumerable<string>)new[] { "Tel Aviv", "Jerusalem", "Haifa" })
                });
            Console.WriteLine($"   ✓ סקר '{newPoll.Name}' נוצר (ID: {newPoll.Id}). מיילים נשלחו לנרשמים.\n");

            // ===================================================================
            // מסך: משתתף – לחיצה על "הצבע בסקר"
            // (יופעל OnPollVoteCast → מייל למנהל)
            // ===================================================================
            Console.WriteLine(">> [מסך משתתף] לחיצה על 'הצבע' בסקר");
            await pollService.SubmitVoteAsync(newPoll.Id, 1, newParticipant.Id, "Tel Aviv");
            Console.WriteLine("   ✓ הצבעה נרשמה. מייל נשלח למנהל.\n");

            // הצבעה חוזרת – בדיקת מניעת כפילות
            await pollService.SubmitVoteAsync(newPoll.Id, 1, newParticipant.Id, "Haifa");
            Console.WriteLine("   ✓ הצבעה עודכנה (כפילות נמנעה אוטומטית).\n");

            // ===================================================================
            // מסך: תוצאות סקר – לחיצה על "הצג תוצאות"
            // ===================================================================
            Console.WriteLine(">> [מסך תוצאות] לחיצה על 'הצג תוצאות סקר'");
            var results = await pollService.GetPollResultsAsync(newPoll.Id);
            foreach (var qResult in results.QuestionResults)
            {
                Console.WriteLine($"   שאלה: {qResult.Question}");
                foreach (var (choice, count, pct) in qResult.OptionBreakdown)
                    Console.WriteLine($"     {choice}: {count} הצבעות ({pct}%)");
            }

            Console.WriteLine("\n=== סימולציה הסתיימה בהצלחה! ===");
            Console.WriteLine($"בדוק קבצי XML ב: {dataDir}");
            Console.WriteLine($"בדוק לוג מיילים ב: {mailLog}");
            Console.ReadLine();
        }
    }
}
