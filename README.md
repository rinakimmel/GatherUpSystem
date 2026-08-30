# GatherUp

A full-stack event coordination and finance platform built with C# and ASP.NET Core for managing group activities, participant engagement, polls, billing, and automated communication.

> Built as a final-year project focused on backend architecture, business rules, modular design, and practical workflow automation.

---

## Overview

GatherUp is a domain-driven event management system designed to simplify the complexity of organizing group experiences such as family events, corporate gatherings, community meetups, and celebrations.

The platform gives organizers a single place to:

- manage event details and lifecycle
- coordinate participants and attendance status
- collect decisions via interactive polls
- track payments, vendors, and receipts
- send reminders and notifications based on preferences
- maintain a clean, layered architecture with reusable business services

---

## Core Capabilities

### Event Management
- Create and update events with title, description, date, location, and cost details
- Attach a manager and event host to each event
- View all events in a unified dashboard
- Maintain event-specific participant and finance context

### Participant Management
- Register participants under a manager-controlled flow
- Track attendance confirmations and rejections
- Send invitations and reminders electronically
- Store individual communication preferences and status

### Polling and Decision Support
- Create polls with multiple questions and multiple-choice answers
- Allow participants to vote and change selections
- Display aggregated results with visual charts
- Support event-driven decision making in a transparent, collaborative way

### Finance Tracking
- Record participant payments and outstanding balances
- Track vendor allocations and debts
- Store invoice or receipt files securely
- Calculate summary values such as income, expenses, and net balance
- Trigger reminder flows for pending payments

### Notification Engine
- Push updates when participant attendance changes
- Send event updates based on each participant’s mailing preference
- Notify managers of important actions such as payments and confirmations
- Support asynchronous communication patterns using a notification bus

---

## Architecture

The solution uses a layered architecture, separating business logic, infrastructure, and domain responsibilities.

```text
┌─────────────────────────────────────────────┐
│ GatherUp.API                                │
│ - Controllers                               │
│ - Minimal API endpoints                     │
│ - Static SPA frontend                       │
├─────────────────────────────────────────────┤
│ GatherUp.BL                                 │
│ - Business services                         │
│ - Application logic                         │
│ - Event notification orchestration          │
├─────────────────────────────────────────────┤
│ GatherUp.Infrastructure                     │
│ - XML repository implementations            │
│ - Mail service                              │
│ - Receipt storage                           │
├─────────────────────────────────────────────┤
│ GatherUp.Core                               │
│ - Entities                                  │
│ - Interfaces                                │
│ - Business exceptions                       │
│ - Shared domain contracts                   │
└─────────────────────────────────────────────┘
```

### Design Principles Applied

| Principle | Implementation |
|---|---|
| Dependency Inversion | Core defines contracts, higher layers depend on abstractions |
| Repository Pattern | Generic repository interface used with XML and in-memory implementations |
| Event-Driven Notifications | Event bus decouples business actions from notification dispatch |
| Immutable Data | Receipt details are stored as immutable records |
| Async-first I/O | Communication and persistence flows use asynchronous operations |

---

## Domain Model

```text
Person (abstract)
├── EventManager     - manages the dashboard and event lifecycle
├── EventHost        - owner or coordinator of the event
└── Participant      - registered attendee with status, payment, and preferences

Event               - central entity for a gathering or meeting
VendorAllocation    - supplier and cost breakdown information
ReceiptDetails      - immutable financial receipt metadata
Poll                - collection of questions and choices
PollQuestion        - question and participant voting options
```

### Mailing Preference Enum

```csharp
None                 = 0
ImportantUpdatesOnly = 1   // time/location changes
AllUpdates           = 2   // general notifications
DirectMessages       = 4   // manager direct messages
Everything           = 7   // all communication
```

---

## Tech Stack

| Technology | Purpose |
|---|---|
| C# / .NET 8 | Main application language and runtime |
| ASP.NET Core | API framework, routing, middleware, static assets |
| XML Serialization | Persistent local data storage |
| Bootstrap 5 | UI styling and responsive layout |
| Bootstrap Icons | Visual iconography |
| Chart.js | Financial and poll result visualization |
| xUnit | Unit and integration testing |

---

## Solution Structure

```text
GatherUpSystem/
├── GatherUp.API/                 # Web API and frontend assets
│   ├── Controllers/
│   ├── Services/
│   ├── wwwroot/
│   └── Program.cs
├── GatherUp.BL/                  # Business logic and domain services
├── GatherUp.Core/                # Core entities, abstractions, exceptions
├── GatherUp.Infrastructure/      # Data access and mail/receipt services
├── GatherUp.UnitTests/           # Automated tests
├── GatherUp.Tests/               # Additional testing project
├── GatherUpSysTem.sln            # Visual Studio solution file
├── README.md
└── .gitignore
```

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- An IDE such as Visual Studio or VS Code

### Run the application

```bash
cd GatherUpSystem
dotnet restore
dotnet run --project GatherUp.API
```

Then open the URL shown in the terminal, typically:

```text
http://localhost:5000
```

### Default login

| Role | Email | Password |
|---|---|---|
| Manager | admin@example.com | admin |

> The application seeds demo data in development mode to make testing and UI exploration easier.

---

## API Overview

### Events

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/events | Get all events |
| GET | /api/events/{id} | Get specific event details |
| POST | /api/events | Create a new event |
| PUT | /api/events/{id} | Update an existing event |
| DELETE | /api/events/{id} | Delete an event |
| GET | /api/events/{id}/participants | Get event participants |
| GET | /api/events/{id}/polls | Get event polls |
| POST | /api/events/{id}/host | Set an event host |

### Participants

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/participants | Get all participants |
| GET | /api/participants/{id} | Get participant details |
| POST | /api/participants/{eventId} | Add participant to event |
| POST | /api/participants/{eventId}/confirm/{participantId} | Confirm or reject attendance |
| POST | /api/participants/{eventId}/invitations | Send invitations |
| POST | /api/participants/{eventId}/reminders | Send reminders |

### Polls

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/polls/{eventId} | Create a poll |
| GET | /api/polls/{id} | Get poll details |
| GET | /api/polls/{pollId}/results | View poll results |
| POST | /api/polls/{pollId}/vote | Cast a vote |

### Finance

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/finance/{eventId}/summary | View financial summary |
| POST | /api/finance/{eventId}/payment/{participantId} | Register payment |
| POST | /api/finance/{eventId}/vendor-debt | Add vendor debt |
| POST | /api/finance/{eventId}/payment-reminders | Trigger payment reminder flow |
| POST | /api/finance/{eventId}/vendors/{vendorName}/receipts | Upload receipt |
| GET | /api/finance/receipts/{receiptNumber}/file | Download receipt file |

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| POST | /auth/login | Sign in |
| POST | /auth/logout | Sign out |
| GET | /auth/me | Get current authenticated user |
| POST | /auth/register/participant | Register participant account |

---

## User Interface

The project includes a lightweight SPA embedded in the API, allowing users to manage events without leaving the application context.

### Dashboard Experience
- Event cards with quick summary details
- Detail panel with tabs for participants, polls, finance, and host info
- Responsive layout designed for workstations and tablet usage

### Participant Panel
- Full participant list with filtering by event
- Add participant actions
- Invitation and reminder workflow

### Poll Panel
- Select an event and view its poll set
- Vote interactively with visual progress indicators
- Review current selection status clearly

### Finance Panel
- KPI cards for revenue, expenses, balance, and payer count
- Participant payment status table
- Vendor tracking table
- Doughnut chart for financial distribution

---

## Business Value

GatherUp bridges the gap between event planning and execution by combining operational coordination with financial transparency and communication automation. It turns a traditionally fragmented planning workflow into one manageable system that helps organizers:

- keep every participant informed
- collect decisions faster
- reduce payment uncertainty
- minimize manual administrative work
- maintain an auditable digital trail for event-related actions

---

## Project Highlights

- Clean separation between domain, application, and infrastructure layers
- XML-backed persistence for lightweight local storage
- Notification bus for decoupled communication logic
- Event-specific finance and polling views
- Authentication support with role-based access patterns
- Ready for extension with databases, external notification providers, or richer front-end integrations

---

## Notes for Development

This project is intentionally designed as a practical business application rather than a pure academic demo. The current implementation stores records in XML files under the application data folder and uses a file-based mail service for notification simulation.

That makes it ideal for:

- learning layered architecture in .NET
- exploring repository and service patterns
- practicing business logic design with domain entities
- understanding how a real-world event platform can be structured in a manageable codebase

---

## License

This project is intended for learning, portfolio, and academic demonstration purposes.

---

## Contact / Project Context

This repository represents a complete event-management solution developed with a strong emphasis on clean architecture, modular services, and practical business workflows.

If you are reviewing the codebase, the most important folders to inspect first are:

- [GatherUp.Core](GatherUp.Core)
- [GatherUp.BL](GatherUp.BL)
- [GatherUp.Infrastructure](GatherUp.Infrastructure)
- [GatherUp.API](GatherUp.API)
- [GatherUp.UnitTests](GatherUp.UnitTests)


---

## Project Structure

```text
GatherUpSystem/
│
├── GatherUp.Core/                  # Core domain layer
│   ├── DO/                         # Domain objects
│   │   ├── Person.cs               # Abstract base class
│   │   ├── EventManager.cs
│   │   ├── EventHost.cs
│   │   ├── Participant.cs
│   │   ├── Event.cs
│   │   ├── Poll.cs
│   │   ├── PollQuestion.cs
│   │   ├── VendorAllocation.cs
│   │   ├── ReceiptDetails.cs       # Immutable record
│   │   └── MailingPreference.cs    # Flags enum
│   ├── IRepository.cs              # Generic storage contract
│   ├── IReceiptRepository.cs
│   ├── IMailService.cs
│   └── IEventNotifications.cs      # Event bus contract
│
├── GatherUp.BL/                    # Business logic layer
│   ├── ParticipantService.cs
│   ├── FinanceService.cs
│   ├── PollService.cs
│   └── EventNotificationBus.cs     # Event bus implementation
│
├── GatherUp.Infrastructure/        # Infrastructure and persistence layer
│   ├── XMLRepository.cs            # Generic XML repository
│   ├── MemoryRepository.cs         # In-memory repository for testing
│   ├── FileMailService.cs          # File-based mail logging
│   ├── ReceiptRepository.cs        # Receipt storage and retrieval
│   ├── XML/
│   │   ├── XMLSerializer.cs
│   │   └── XMLDocManager.cs
│   └── Data/
│       └── Initialize.cs           # Demo data seeding
│
├── GatherUp.API/                   # API layer
│   ├── Controllers/
│   │   ├── EventsController.cs
│   │   ├── ParticipantsController.cs
│   │   ├── PollsController.cs
│   │   └── FinanceController.cs
│   ├── Services/
│   │   └── CredentialService.cs    # Authentication and role management
│   ├── Program.cs                  # Dependency injection and middleware setup
│   ├── GlobalExceptionMiddleware.cs
│   └── wwwroot/                    # Frontend assets
│       ├── index.html
│       ├── styles.css
│       └── app.js
│
├── GatherUp.UnitTests/             # Unit tests
└── GatherUp.Tests/                 # Integration tests
```

---

## Layer Diagram

```text
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

## Test Execution

```bash
dotnet test
```

The project includes:
- Unit tests in `GatherUp.UnitTests` for services and controllers
- Integration tests in `GatherUp.Tests` using WebApplicationFactory and end-to-end flow validation

---

## Notes

- Data storage: all records are stored in XML files under the `Data/` folder next to the executable
- Mail handling: emails are logged to `mail_log.txt` rather than being sent externally
- Authentication: cookie-based authentication with `HttpOnly` protection against basic XSS exposure
- Roles: two access levels are supported, `Manager` and `Participant`

