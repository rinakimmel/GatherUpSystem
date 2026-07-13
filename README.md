# GatherUp 🎉

> **מנוע Backend מלא לניהול, תיאום וגביית תקציב עבור מפגשים קבוצתיים**  
> פרויקט גמר — קורס C# מתקדם

---

## 📖 תוכן עניינים

- [רעיון כללי](#-רעיון-כללי)
- [תכונות עיקריות](#-תכונות-עיקריות)
- [ארכיטקטורה](#-ארכיטקטורה)
- [ישויות המערכת](#-ישויות-המערכת)
- [טכנולוגיות](#-טכנולוגיות)
- [הפעלה מהירה](#-הפעלה-מהירה)
- [נקודות קצה API](#-נקודות-קצה-api)
- [ממשק משתמש](#-ממשק-משתמש)
- [מבנה הפרויקט](#-מבנה-הפרויקט)
- [תרשים שכבות](#-תרשים-שכבות)

---

## 💡 רעיון כללי

**GatherUp** היא מערכת Web API מלאה שנועדה לפתור את הבלגן הארגוני הכרוך בתיאום אירועים קבוצתיים — שבתות גיבוש, מסיבות משפחתיות, מפגשי חברות ועוד.

המערכת מאפשרת:
- **תיאום לוחות זמנים** — קביעת תאריך, שעה ומיקום לאירוע
- **ניהול משתתפים** — הזמנה, אישור הגעה ומעקב
- **קבלת החלטות קבוצתית** — סקרים אינטראקטיביים
- **מעקב פיננסי** — גביית תשלומים, ניהול ספקים וקבלות דיגיטליות
- **מנוע התראות אסינכרוני** — עדכונים אוטומטיים למשתתפים לפי העדפותיהם

---

## ✨ תכונות עיקריות

### 🗓️ ניהול אירועים
- יצירה ועריכה של אירועים עם כל הפרטים (שם, תאריך, מיקום, מחיר לאדם, אמצעי תשלום)
- קישור מנהל ובעל האירוע (Host) לכל אירוע
- סקירה כוללת של כל האירועים בדשבורד

### 👥 ניהול משתתפים
- רישום משתתפים על-ידי מנהל (קבוצה סגורה)
- שליחת הזמנות ותזכורות בדואר אלקטרוני
- אישור / דחיית הגעה
- העדפות דיוור אישיות לכל משתתף

### 🗳️ סקרים (**Polls**)
- יצירת סקרים עם שאלות מרובות ואפשרויות בחירה
- הצבעה עם אפשרות לשינוי בחירה
- תצוגת תוצאות בזמן אמת עם גרף

### 💰 ניהול פיננסי
- רישום תשלומים ממשתתפים
- הוספת ספקים וסכומים חייבים
- העלאת קבלות דיגיטליות (נעילה immutable לאחר שמירה)
- סיכום פיננסי: הכנסות, הוצאות, יתרה
- שליחת תזכורות תשלום אוטומטיות

### 🔔 מנוע התראות אסינכרוני
- התראות למנהל על כל אישור הגעה ותשלום
- שליחת עדכונים למשתתפים לפי העדפות הדיוור שלהם
- מיילים אוטומטיים בעת שינוי פרטי האירוע

---

## 🏗️ ארכיטקטורה

המערכת בנויה על עקרון **הפרדת שכבות** (Clean Architecture) מלאה:

```
┌─────────────────────────────────────────┐
│           GatherUp.API                  │  ← Controllers, Minimal API, UI
├─────────────────────────────────────────┤
│           GatherUp.BL                   │  ← Business Logic Services
├─────────────────────────────────────────┤
│        GatherUp.Infrastructure          │  ← XML Repositories, Mail
├─────────────────────────────────────────┤
│           GatherUp.Core                 │  ← Entities, Interfaces, Exceptions
└─────────────────────────────────────────┘
```

### עקרונות עיצוב מיושמים

| עיקרון | יישום |
|--------|-------|
| **Dependency Inversion** | כל השכבות תלויות ב-Core, לא אחת בשנייה |
| **Repository Pattern** | `IRepository<T>` גנרי עם מימוש XML ו-Memory |
| **Observer / Event Bus** | `IEventNotifications` + `EventNotificationBus` |
| **Immutable Record** | `ReceiptDetails` — record שלא ניתן לשנות |
| **Async/Await** | כל פעולות ה-I/O אסינכרוניות במלואן |

---

## 🧩 ישויות המערכת

```
Person (abstract)
├── EventManager     — מנהל האירוע
├── EventHost        — בעל האירוע (כלה, יום הולדת...)
└── Participant      — משתתף עם מצב הגעה, תשלום והעדפות דיוור

Event                — האירוע המרכזי, מקשר בין כולם
VendorAllocation     — ספק + סכום חייב + קבלות
ReceiptDetails       — record קפוא (immutable) של קבלה פיננסית
Poll                 — סקר עם שאלות ואפשרויות
PollQuestion         — שאלה + אפשרויות + בחירות משתתפים
```

### MailingPreference (Flags Enum)
```csharp
None                 = 0
ImportantUpdatesOnly = 1   // שינויי מיקום / שעה
AllUpdates           = 2   // כל עדכון
DirectMessages       = 4   // הודעות מנהל
Everything           = 7   // הכל
```

---

## 🛠️ טכנולוגיות

| טכנולוגיה | שימוש |
|-----------|-------|
| **C# 12 / .NET 8** | שפת הפיתוח הראשית |
| **ASP.NET Core** | Web API + Minimal API + Static Files |
| **XML Serialization** | שמירה ושליפה מקובצי XML מקומיים |
| **Bootstrap 5.3** | עיצוב ממשק המשתמש |
| **Bootstrap Icons** | אייקונים |
| **Chart.js 4** | גרפים פיננסיים וסקרים |
| **xUnit** | בדיקות יחידה ואינטגרציה |

---

## 🚀 הפעלה מהירה

### דרישות מקדימות
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### הפעלה

```bash
# שכפול / פתיחת הפרויקט
cd GatherUpSystem

# הרצה
dotnet run --project GatherUp.API
```

ניווט לכתובת שמוצגת בטרמינל (לרוב `http://localhost:5000`).

### פרטי כניסה ברירת מחדל

| תפקיד | אימייל | סיסמה |
|--------|--------|-------|
| **מנהל** | `admin@example.com` | `admin` |

> **הוספת משתתפים:** היכנס כמנהל ← Participants ← Add Participant

---

## 📡 נקודות קצה API

### 🗓️ Events — `/api/events`

| Method | Endpoint | תיאור |
|--------|----------|-------|
| `GET` | `/api/events` | כל האירועים |
| `GET` | `/api/events/{id}` | אירוע מלא עם כל הפרטים |
| `POST` | `/api/events` | יצירת אירוע חדש |
| `PUT` | `/api/events/{id}` | עדכון אירוע |
| `DELETE` | `/api/events/{id}` | מחיקת אירוע |
| `GET` | `/api/events/{id}/participants` | משתתפי האירוע |
| `GET` | `/api/events/{id}/polls` | סקרי האירוע |
| `POST` | `/api/events/{id}/host` | הגדרת בעל האירוע |

### 👥 Participants — `/api/participants`

| Method | Endpoint | תיאור |
|--------|----------|-------|
| `GET` | `/api/participants` | כל המשתתפים |
| `GET` | `/api/participants/{id}` | משתתף ספציפי |
| `POST` | `/api/participants/{eventId}` | הוספת משתתף לאירוע |
| `POST` | `/api/participants/{eventId}/confirm/{participantId}` | אישור / דחיית הגעה |
| `POST` | `/api/participants/{eventId}/invitations` | שליחת הזמנות |
| `POST` | `/api/participants/{eventId}/reminders` | שליחת תזכורות |

### 🗳️ Polls — `/api/polls`

| Method | Endpoint | תיאור |
|--------|----------|-------|
| `POST` | `/api/polls/{eventId}` | יצירת סקר |
| `GET` | `/api/polls/{id}` | פרטי סקר |
| `GET` | `/api/polls/{pollId}/results` | תוצאות סקר |
| `POST` | `/api/polls/{pollId}/vote` | הצבעה |

### 💰 Finance — `/api/finance`

| Method | Endpoint | תיאור |
|--------|----------|-------|
| `GET` | `/api/finance/{eventId}/summary` | סיכום פיננסי |
| `POST` | `/api/finance/{eventId}/payment/{participantId}` | רישום תשלום |
| `POST` | `/api/finance/{eventId}/vendor-debt` | הוספת חוב לספק |
| `POST` | `/api/finance/{eventId}/payment-reminders` | תזכורות תשלום |
| `POST` | `/api/finance/{eventId}/vendors/{vendorName}/receipts` | העלאת קבלה |
| `GET` | `/api/finance/receipts/{receiptNumber}/file` | הורדת קובץ קבלה |

### 🔐 Auth — `/auth`

| Method | Endpoint | תיאור |
|--------|----------|-------|
| `POST` | `/auth/login` | התחברות |
| `POST` | `/auth/logout` | התנתקות |
| `GET` | `/auth/me` | פרטי המשתמש המחובר |
| `POST` | `/auth/register/participant` | רישום משתתף (מנהל בלבד) |

---

## 🖥️ ממשק משתמש

ממשק SPA (Single Page Application) מובנה בתוך ה-API עצמו:

### מסך התחברות
![Login](https://via.placeholder.com/600x300/5c6ef5/ffffff?text=Login+Page)

### פאנל Events
- רשימת אירועים כ-Cards עם מידע מקוצר
- לחיצה על כרטיס פותחת **פאנל פרטים** עם 4 טאבים:
  - **Participants** — טבלת משתתפים + שליחת הזמנות
  - **Polls** — סקרים + יצירת סקר חדש
  - **Finance** — סיכום כספי + רישום תשלומים + ספקים + קבלות
  - **Host** — פרטי בעל האירוע

### פאנל Participants
- טבלה כוללת של כל המשתתפים
- סינון לפי אירוע
- רישום משתתף חדש (מנהל בלבד)

### פאנל Polls
- בחירת אירוע וצפייה בסקרים
- הצבעה אינטראקטיבית עם progress bar
- ✅ מסומן על הבחירה הנוכחית של המשתמש

### פאנל Finance
- 4 KPI cards: הכנסות, הוצאות, יתרה, מספר משלמים
- טבלת סטטוס תשלומים
- טבלת ספקים
- גרף Doughnut של תקציב

---

## 📁 מבנה הפרויקט

```
GatherUpSystem/
│
├── GatherUp.Core/                  # ❤️ ליבת המערכת
│   ├── DO/                         # Domain Objects (ישויות)
│   │   ├── Person.cs               # מחלקת בסיס מופשטת
│   │   ├── EventManager.cs
│   │   ├── EventHost.cs
│   │   ├── Participant.cs
│   │   ├── Event.cs
│   │   ├── Poll.cs
│   │   ├── PollQuestion.cs
│   │   ├── VendorAllocation.cs
│   │   ├── ReceiptDetails.cs       # Immutable record
│   │   └── MailingPreference.cs    # Flags enum
│   ├── IRepository.cs              # ממשק גנרי לאחסון
│   ├── IReceiptRepository.cs
│   ├── IMailService.cs
│   └── IEventNotifications.cs      # ממשק Event Bus
│
├── GatherUp.BL/                    # 🧠 לוגיקה עסקית
│   ├── ParticipantService.cs
│   ├── FinanceService.cs
│   ├── PollService.cs
│   └── EventNotificationBus.cs     # מימוש Event Bus
│
├── GatherUp.Infrastructure/        # 🗄️ תשתית ואחסון
│   ├── XMLRepository.cs            # מימוש XML גנרי
│   ├── MemoryRepository.cs         # מימוש זיכרון (לבדיקות)
│   ├── FileMailService.cs          # שמירת מיילים לקובץ
│   ├── ReceiptRepository.cs        # אחסון קבלות + קבצים
│   ├── XML/
│   │   ├── XMLSerializer.cs
│   │   └── XMLDocManager.cs
│   └── Data/
│       └── Initialize.cs           # Seed נתוני דמו
│
├── GatherUp.API/                   # 🌐 שכבת API
│   ├── Controllers/
│   │   ├── EventsController.cs
│   │   ├── ParticipantsController.cs
│   │   ├── PollsController.cs
│   │   └── FinanceController.cs
│   ├── Services/
│   │   └── CredentialService.cs    # אותנטיקציה + הרשאות
│   ├── Program.cs                  # DI + Middleware + Minimal API
│   ├── GlobalExceptionMiddleware.cs
│   └── wwwroot/                    # 🎨 ממשק משתמש
│       ├── index.html
│       ├── styles.css
│       └── app.js
│
├── GatherUp.UnitTests/             # 🧪 בדיקות יחידה
└── GatherUp.Tests/                 # 🧪 בדיקות אינטגרציה
```

---

## 🔄 תרשים שכבות

```
         ┌──────────────────────────────────┐
         │         Browser / Client         │
         │    (HTML + CSS + JS - SPA)       │
         └─────────────┬────────────────────┘
                       │ HTTP
         ┌─────────────▼────────────────────┐
         │          GatherUp.API            │
         │   Controllers + Minimal API      │
         │   Auth Middleware + Exception     │
         └──────┬──────────────┬────────────┘
                │              │
    ┌───────────▼──┐    ┌──────▼──────────┐
    │  GatherUp.BL │    │  CredentialSvc  │
    │  Services +  │    │  (Auth/Roles)   │
    │  EventBus    │    └─────────────────┘
    └───────┬──────┘
            │
    ┌───────▼──────────────────────────────┐
    │        GatherUp.Infrastructure       │
    │   XMLRepository + FileMailService    │
    │   ReceiptRepository + XMLDocManager  │
    └───────┬──────────────────────────────┘
            │
    ┌───────▼──────────────────────────────┐
    │           GatherUp.Core              │
    │   Entities + Interfaces + Exceptions │
    └──────────────────────────────────────┘
            │
    ┌───────▼──────────────────────────────┐
    │         XML Files (Local)            │
    │  Events.xml, Participants.xml, ...   │
    └──────────────────────────────────────┘
```

---

## 🧪 הרצת בדיקות

```bash
dotnet test
```

הפרויקט כולל:
- **בדיקות יחידה** — `GatherUp.UnitTests` (Services + Controllers)
- **בדיקות אינטגרציה** — `GatherUp.Tests` (WebApplicationFactory end-to-end)

---

## 📝 הערות

- **אחסון נתונים:** כל הנתונים נשמרים בקובצי XML בתיקיית `Data/` ליד ה-executable
- **מיילים:** כל המיילים נכתבים לקובץ `mail_log.txt` (לא נשלחים אמיתית)
- **אימות:** Cookie-based authentication — `HttpOnly`, מוגן מ-XSS
- **הרשאות:** שתי רמות — `Manager` ו-`Participant`

---

<div align="center">

**נבנה במסגרת קורס C# מתקדם** 🎓

</div>
