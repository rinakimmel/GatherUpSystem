# GatherUp

<div align="center">

![C#](https://img.shields.io/badge/C%23-.NET%208-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-512BD4)
![xUnit](https://img.shields.io/badge/Testing-xUnit-5C2D91)
![License](https://img.shields.io/badge/License-Project%20Use-blue)

</div>

A modern event coordination platform for managing group gatherings, participant engagement, financial activity, and community decision-making in one place.

> Built with C# and ASP.NET Core to demonstrate clean architecture, domain-driven design, and practical business workflows.

---

## Overview

GatherUp is designed to streamline event planning and execution for activities such as family celebrations, corporate meetings, social gatherings, and community events. It brings together the main operational needs of organizers into one system: attendance tracking, polling, payment management, notifications, and event coordination.

The platform helps teams and managers:

- organize event details and lifecycle management
- track participants and attendance status
- collect decisions through interactive polls
- manage costs, deposits, and vendor allocations
- send reminders and updates automatically
- maintain a clear and structured backend architecture

---

## Why It Matters

Traditional event planning often involves scattered tools and manual coordination. GatherUp centralizes those responsibilities into a unified workflow, reducing errors, improving visibility, and giving organizers a reliable view of event progress and financial health.

---

## Key Features

### Event Management
- Create and manage events with title, description, date, location, and pricing details
- Link a manager and event host to each event
- Keep all event-related data organized in one place

### Participant Coordination
- Register participants in a controlled flow
- Track confirmation and attendance status
- Send invitation and reminder actions
- Store communication preferences per participant

### Polling and Decision Support
- Create multiple-question polls with multiple-choice answers
- Allow participants to vote and update their choice
- Present results in a clear, visual form

### Financial Tracking
- Record participant payments and outstanding balances
- Track vendor liabilities and allocation details
- Store receipts in a structured way
- Review income, expenses, and net balance summaries

### Notification System
- Trigger updates based on attendance and payment changes
- Respect participant communication preferences
- Send event-related messages through a decoupled notification flow

---

## Architecture

The solution follows a layered architecture that separates the domain model, business rules, infrastructure, and API concerns.

```text
┌────────────────────────────────────────────┐
│ GatherUp.API                              │
│ - Controllers                             │
│ - Minimal API endpoints                   │
│ - Embedded frontend                      │
├────────────────────────────────────────────┤
│ GatherUp.BL                               │
│ - Application services                   │
│ - Core business logic                    │
│ - Notification orchestration             │
├────────────────────────────────────────────┤
│ GatherUp.Infrastructure                   │
│ - XML repositories                       │
│ - Mail service                           │
│ - Receipt handling                       │
├────────────────────────────────────────────┤
│ GatherUp.Core                             │
│ - Domain entities                        │
│ - Interfaces                             │
│ - Exceptions                            │
│ - Shared contracts                       │
└────────────────────────────────────────────┘
```

### Design Principles

- Dependency inversion through abstractions in the core layer
- Repository pattern for clean persistence abstraction
- Event-driven notifications for decoupled communication
- Immutable financial records for data integrity
- Asynchronous processing for communication and I/O workflows

---

## Core Domain Model

```text
Person (abstract)
├── EventManager
├── EventHost
└── Participant

Event
VendorAllocation
ReceiptDetails
Poll
PollQuestion
```

### Mailing Preference Enum

```csharp
None                 = 0
ImportantUpdatesOnly = 1
AllUpdates           = 2
DirectMessages       = 4
Everything           = 7
```

---

## Technology Stack

| Technology | Role |
|---|---|
| C# / .NET 8 | Application runtime and primary language |
| ASP.NET Core | API, routing, middleware, static hosting |
| XML Serialization | Lightweight local persistence |
| Bootstrap | Interface styling and layout |
| Chart.js | Poll and financial visualization |
| xUnit | Automated testing |

---

## Project Structure

```text
GatherUpSystem/
├── GatherUp.API/
│   ├── Controllers/
│   ├── Services/
│   ├── wwwroot/
│   ├── Program.cs
│   └── GlobalExceptionMiddleware.cs
├── GatherUp.BL/
│   ├── ParticipantService.cs
│   ├── FinanceService.cs
│   ├── PollService.cs
│   └── EventNotificationBus.cs
├── GatherUp.Core/
│   ├── DO/
│   ├── Exceptions/
│   ├── IEntity.cs
│   ├── IRepository.cs
│   ├── IReceiptRepository.cs
│   └── IMailService.cs
├── GatherUp.Infrastructure/
│   ├── XMLRepository.cs
│   ├── MemoryRepository.cs
│   ├── FileMailService.cs
│   ├── ReceiptRepository.cs
│   └── XML/
├── GatherUp.UnitTests/
├── GatherUp.Tests/
├── GatherUpSysTem.sln
├── README.md
└── .gitignore
```

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Visual Studio or VS Code

### Run the application

```bash
cd GatherUpSystem
dotnet restore
dotnet run --project GatherUp.API
```

Then open the local URL printed in the terminal, usually:

```text
http://localhost:5000
```

### Default login

| Role | Email | Password |
|---|---|---|
| Manager | admin@example.com | admin |

> Demo data is seeded automatically in development mode for easier testing.

---

## API Overview

### Events

| Method | Endpoint | Purpose |
|---|---|---|
| GET | /api/events | List all events |
| GET | /api/events/{id} | Get event details |
| POST | /api/events | Create a new event |
| PUT | /api/events/{id} | Update an event |
| DELETE | /api/events/{id} | Delete an event |

### Participants

| Method | Endpoint | Purpose |
|---|---|---|
| GET | /api/participants | List participants |
| GET | /api/participants/{id} | Fetch participant details |
| POST | /api/participants/{eventId} | Add participant |
| POST | /api/participants/{eventId}/confirm/{participantId} | Confirm or reject attendance |

### Polls

| Method | Endpoint | Purpose |
|---|---|---|
| POST | /api/polls/{eventId} | Create a poll |
| GET | /api/polls/{id} | Get poll data |
| GET | /api/polls/{pollId}/results | View poll results |
| POST | /api/polls/{pollId}/vote | Cast a vote |

### Finance

| Method | Endpoint | Purpose |
|---|---|---|
| GET | /api/finance/{eventId}/summary | View financial summary |
| POST | /api/finance/{eventId}/payment/{participantId} | Register a payment |
| POST | /api/finance/{eventId}/vendor-debt | Add vendor debt |
| POST | /api/finance/{eventId}/payment-reminders | Send payment reminders |

### Authentication

| Method | Endpoint | Purpose |
|---|---|---|
| POST | /auth/login | Sign in |
| POST | /auth/logout | Sign out |
| GET | /auth/me | Get authenticated user |
| POST | /auth/register/participant | Register a participant |

---

## User Experience

The project includes a lightweight embedded SPA that lets users manage the main workflow directly from the application interface without switching tools.

### Dashboard
- Event cards with summary information
- Detail panels for participants, polls, finance, and host data
- Responsive layout for practical use

### Finance Panel
- Revenue, expenses, balance, and payer indicators
- Payment status tables
- Vendor tracking and receipt handling

### Poll Panel
- Event-based polling workflow
- Voting interactions with clear visual feedback

---

## Business Value

GatherUp is more than a demo project. It represents a realistic operational system that helps organizations coordinate events more effectively while improving transparency, reducing manual effort, and keeping communication consistent.

---

## Testing

```bash
dotnet test
```

This project includes:
- unit tests for core business logic and controllers
- integration tests for end-to-end behavior validation

---

## Notes

- Data is stored locally in XML files under the application’s data folder
- Email notifications are logged to a local file instead of being sent externally
- Authentication uses cookie-based session management with role awareness
- Supported roles include `Manager` and `Participant`

---

## License

This project is intended for learning, portfolio development, and academic demonstration purposes.

---

## Main Project Areas

- [GatherUp.Core](GatherUp.Core)
- [GatherUp.BL](GatherUp.BL)
- [GatherUp.Infrastructure](GatherUp.Infrastructure)
- [GatherUp.API](GatherUp.API)
- [GatherUp.UnitTests](GatherUp.UnitTests)

