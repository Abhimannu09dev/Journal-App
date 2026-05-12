# 📓 Journal App

[![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/en-us/apps/maui)
[![Platform](https://img.shields.io/badge/platforms-Android%20%7C%20iOS%20%7C%20macOS%20%7C%20Windows-blue)](https://dotnet.microsoft.com/en-us/apps/maui)
[![SQLite](https://img.shields.io/badge/database-SQLite-003B57?logo=sqlite)](https://www.sqlite.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

---

## 📖 Project Description

**Journal App** is a cross-platform personal journaling application built with **.NET MAUI Blazor Hybrid**. It runs natively on Android, iOS, macOS, and Windows — all from a single codebase.

### What it does

The app gives you a private, local-first space to write daily journal entries. Each entry supports Markdown formatting, mood tagging (primary and secondary), and custom tag labels. A built-in PIN lock keeps your entries private. The app also tracks your writing streaks, visualises your journaling history on a calendar, and lets you export entries as formatted PDFs.

### Problem it solves

Most journaling tools are cloud-based, meaning your personal thoughts live on someone else's server. Journal App is **100% offline** — all data is stored in a local SQLite database on your device. No account, no internet connection, no data leaving your phone or computer.

---

## ✨ Features

- **Daily Journal Entries** — One entry per day enforced by the database. Write in Markdown or rich text with a word count tracked automatically.
- **Mood Tracking** — Tag each entry with a primary mood and optional secondary moods. 12 built-in moods with emoji (😊 Happy, 😢 Sad, 😰 Anxious, 💪 Motivated, and more).
- **Custom Tags** — Label entries with tags like Work, Health, Gratitude, Travel. Manage your tag library from the app.
- **Streak Tracking** — View your current and longest writing streaks, missed days, first entry date, and last entry date.
- **Dashboard** — At-a-glance summary: total entries, current streak, longest streak, recent entries, and your most-used primary mood.
- **Calendar View** — Month-view calendar that highlights days that have journal entries for easy navigation.
- **PDF Export** — Export a single entry or a date range to a formatted PDF. Configurable export options: include/exclude title, moods, tags, content. Includes a **Privacy-Safe Mode** that strips all personal content and exports only metadata.
- **Export History** — The app keeps a log of all previous exports.
- **PIN Authentication** — 4-digit PIN gate on app launch. PIN is hashed with PBKDF2-SHA256 at 100,000 iterations with a cryptographically random salt — never stored in plaintext.
- **Light / Dark / System Theme** — Follows the device theme or can be forced to light or dark mode.
- **User Settings** — Customise your display name, theme preference, and accent colour.
- **Offline-First** — No internet connection required. Ever.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | .NET MAUI 10 (Blazor Hybrid) |
| **Language** | C# (.NET 10) |
| **UI** | Blazor Components (Razor) + HTML/CSS |
| **Database** | SQLite (local, on-device) |
| **ORM** | Entity Framework Core 10 (Microsoft.EntityFrameworkCore.Sqlite) |
| **PDF Export** | PdfSharpCore |
| **Markdown Parsing** | Markdig |
| **Image Processing** | SixLabors.ImageSharp |
| **MAUI Utilities** | CommunityToolkit.Maui.Core |
| **Security** | PBKDF2-SHA256 (via `Rfc2898DeriveBytes`) + CSPRNG salt |

---

## 📱 Supported Platforms

| Platform | Minimum Version |
|---|---|
| Android | 7.0 (API 24) |
| iOS | 15.0 |
| macOS (Catalyst) | 15.0 |
| Windows | 10 (build 17763+) |

---

## 🏗️ Architecture Overview

Journal App is a **single-project .NET MAUI Blazor Hybrid** application. MAUI provides the native shell and platform bootstrapping; Blazor renders the UI inside a `BlazorWebView`. EF Core with SQLite handles all local data persistence — no external server or network is involved.

```
┌─────────────────────────────────────────┐
│         Native MAUI Shell               │  ← Platform entry point (Android/iOS/macOS/Windows)
│                                         │
│  ┌───────────────────────────────────┐  │
│  │     BlazorWebView (Razor Pages)   │  │  ← UI: Components/Pages/*.razor
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │     Services (Business Logic)     │  │  ← JournalService, DashboardService, etc.
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │   EF Core + SQLite (Local DB)     │  │  ← AppDbContext → journal.db (on device)
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## 📁 Project Structure

```
Journal App/
├── Components/
│   ├── Layout/
│   │   ├── LoginLayout.razor       # Layout used exclusively on the PIN login screen
│   │   ├── MainLayout.razor        # Main app shell with navigation sidebar
│   │   └── NavMenu.razor           # Navigation menu component
│   └── Pages/
│       ├── Dashboard.razor         # Overview: streaks, recent entries, top mood
│       ├── TodayJournal.razor      # Write or edit an entry for a specific date
│       ├── Journal.razor           # List of all journal entries
│       ├── JournalEntryPreview.razor # Read-only preview of a single entry
│       ├── CalendarView.razor      # Monthly calendar highlighting entry dates
│       ├── Streaks.razor           # Streak statistics (current, longest, missed days)
│       ├── Export.razor            # PDF export (single entry or date range)
│       ├── ManageTags.razor        # Create, view, and deactivate tags
│       ├── Settings.razor          # Theme, username, accent colour
│       ├── Login.razor             # PIN entry screen
│       └── TagPicker.razor         # Reusable tag selection component
│
├── Data/
│   └── AppDbContext.cs             # EF Core DbContext (tables, relationships, SaveChanges hook)
│
├── Entities/
│   ├── JournalEntry.cs             # Core journal entry (date key, title, content, word count)
│   ├── Mood.cs                     # Mood definition (name + emoji)
│   ├── EntryMood.cs                # Entry ↔ Mood (primary / secondary role)
│   ├── Tag.cs                      # Tag definition
│   ├── EntryTag.cs                 # Entry ↔ Tag (many-to-many join)
│   ├── AuthSecret.cs               # Stored PIN hash + salt
│   ├── UserSettings.cs             # Theme, username, accent colour (single-row table)
│   └── ExportHistory.cs            # Log of past exports
│
├── Services/
│   ├── IJournalService.cs          # Journal operations contract
│   ├── JournalService.cs           # Journal CRUD, mood/tag assignment, streak calc
│   ├── DashboardService.cs         # Dashboard aggregation queries
│   ├── AuthStateService.cs         # In-memory login/logout state (singleton)
│   ├── UserSettingsService.cs      # Read/write user settings; PIN provisioning
│   ├── ThemeService.cs             # Applies MAUI AppTheme globally
│   ├── IExportService.cs           # Export contract
│   ├── PdfExportService.cs         # PDF generation via PdfSharpCore
│   ├── ExportOptions.cs            # Export configuration (include/exclude fields)
│   └── StreakStatsDto.cs           # Streak data transfer object
│
├── Security/
│   └── PinHasher.cs                # PBKDF2-SHA256 PIN hashing and verification
│
├── Platforms/                      # Platform-specific entry points and manifests
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
│
├── wwwroot/
│   ├── app.css                     # Global styles
│   ├── index.html                  # Blazor host page
│   └── js/theme.js                 # JS interop for theme application
│
├── MauiProgram.cs                  # App startup, DI registration, DB seed
└── App.xaml / MainPage.xaml        # MAUI app root and Blazor host page
```

---

## ⚙️ Installation Guide

### Prerequisites

Make sure you have the following installed before you begin:

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) with MAUI workload
- [Visual Studio 2022 (v17.12+)](https://visualstudio.microsoft.com/) with the **.NET Multi-platform App UI development** workload
  — *or* VS Code with the .NET MAUI extension
- For Android builds: Android SDK (installed via Visual Studio or Android Studio)
- For iOS/macOS builds: A Mac with Xcode 16+

---

### 1. Install the MAUI workload

If you haven't already, install the MAUI workload via the .NET CLI:

```bash
dotnet workload install maui
```

---

### 2. Clone the repository

```bash
git clone https://github.com/your-username/Journal-App.git
cd Journal-App
```

---

### 3. Install dependencies

```bash
dotnet restore
```

This pulls all NuGet packages including EF Core, PdfSharpCore, Markdig, and MAUI libraries.

---

### 4. Database setup

> **No manual setup required.** The app uses a local SQLite database (`journal.db`) stored in the device's application data folder. The database is created automatically the first time the app launches via `db.Database.EnsureCreated()`.

Default tags (Work, Study, Health, Family, etc.) and default moods (😊 Happy, 😢 Sad, 💪 Motivated, etc.) are also seeded automatically on first run.

---

## ▶️ How to Run the Project Locally

### Run on Windows

```bash
dotnet build -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0
```

Or simply press **F5** in Visual Studio with the `Windows Machine` target selected.

### Run on Android

Connect an Android device or start an emulator, then:

```bash
dotnet build -f net10.0-android
dotnet run -f net10.0-android
```

Or select your Android device/emulator in Visual Studio and press **F5**.

### Run on iOS / macOS

> **Requires a Mac with Xcode.**

```bash
dotnet build -f net10.0-ios
dotnet run -f net10.0-ios

# or for macOS
dotnet build -f net10.0-maccatalyst
dotnet run -f net10.0-maccatalyst
```

> **💡 Tip:** For the fastest development loop, use the **Windows** target. Hot Reload is fully supported.

> **⚠️ Note:** The first launch may take longer than usual — EF Core is creating the SQLite database and seeding default data in the background.

---

## 🔐 Security — PIN Authentication

Journal App protects your entries with a 4-digit PIN gate. Here's how it works:

- On first setup, a default PIN hint is stored in user settings: `"The PIN is 1234"`.
- The PIN itself is **never stored in plaintext**. It is hashed using **PBKDF2 with SHA-256** at **100,000 iterations** with a **16-byte cryptographically random salt** (via `RandomNumberGenerator`).
- On login, the entered PIN is re-derived and compared using `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.
- `AuthStateService` (singleton) tracks whether the user is currently authenticated in-memory. The app redirects to the login screen if `IsLoggedIn` is `false`.

> **💡 Tip:** You can update your PIN and PIN hint from the **Settings** page.

---

## 🗺️ App Pages & Navigation

| Page | Route | Description |
|---|---|---|
| Login | `/login` | PIN entry screen (shown on launch if locked) |
| Dashboard | `/dashboard` | Summary: totals, streaks, latest entries, top mood |
| Journal List | `/journal` | Chronological list of all entries |
| Write / Edit Entry | `/journal/entry/{dateKey}` | Create or edit the entry for a specific date |
| Entry Preview | `/journal/preview/{dateKey}` | Read-only view of a single entry |
| Calendar | `/calendar` | Month view with highlighted entry dates |
| Streaks | `/streaks` | Current streak, longest streak, missed days |
| Export | `/export` | Export single entry or date range to PDF |
| Manage Tags | `/tags` | Create and manage your tag library |
| Settings | `/settings` | Theme, username, accent colour, PIN |

---

## 🧩 How the System Works

### App Startup Flow

```
MauiProgram.CreateMauiApp()
    │
    ├── Register services (DI container)
    │   ├── AppDbContext → SQLite (journal.db in LocalApplicationData)
    │   ├── JournalService, DashboardService, ExportService (Scoped)
    │   ├── UserSettingsService (Scoped)
    │   ├── ThemeService, AuthStateService (Singleton)
    │
    ├── db.Database.EnsureCreated() → creates tables if they don't exist
    ├── Seed default Tags (if missing)
    ├── Seed default Moods (if missing)
    ├── EnsurePinSecretExistsAsync() → provisions default PIN hash if missing
    │
    └── App launches → Redirects to /login
```

### Writing a Journal Entry

1. User navigates to a date (from Dashboard, Journal list, or Calendar).
2. `TodayJournal.razor` calls `JournalService.GetEntryByDateAsync(date)`.
3. If no entry exists, the form renders in **create** mode; otherwise in **edit** mode.
4. On save, `CreateEntryWithMoodsAsync` or `UpdateEntryWithMoodsAsync` is called.
5. Word count is computed and stored automatically.
6. Tags are saved via `SetEntryTagsAsync` — new tag names are created on-the-fly; existing ones are reused.
7. Moods are saved via `SetEntryMoodsAsync` with a primary/secondary role distinction.

### PDF Export

1. User picks a date range and toggles export options on the Export page.
2. `PdfExportService.ExportEntriesPdfAsync` loads entries with their moods and tags.
3. If **Privacy-Safe Mode** is on, all content/mood/tag/title fields are stripped — only the date and word count are included.
4. A PDF is built using `PdfSharpCore`, written to a byte array, and saved to the device via `CommunityToolkit.Maui.Storage.FileSaver`.
5. The export is logged to the `ExportHistory` table.

### Theme System

`ThemeService` (singleton) exposes an `Apply(string mode)` method. Accepted values: `"light"`, `"dark"`, `"system"`. It sets `Application.Current.UserAppTheme` globally. The current preference is persisted in `UserSettings` via `UserSettingsService` and re-applied on each app launch.

---

## 📸 Screenshots / Demo

> Screenshots will be added once the UI is finalised. A demo video link will appear here.

| Screen | Description |
|---|---|
| `[Dashboard Screenshot]` | Streak summary and recent entries |
| `[Journal Entry Screenshot]` | Markdown editor with mood and tag pickers |
| `[Calendar Screenshot]` | Monthly view with highlighted entry days |
| `[Export Screenshot]` | PDF export options and privacy mode toggle |
| `[Login Screenshot]` | 4-digit PIN login screen |

---

## 🚀 Future Improvements

- [ ] Biometric authentication (fingerprint / Face ID) as a PIN alternative
- [ ] Cloud backup option (iCloud, Google Drive, or OneDrive)
- [ ] Rich text editor (WYSIWYG) as an alternative to Markdown
- [ ] Entry search (by keyword, tag, or mood)
- [ ] Mood trend charts (weekly/monthly breakdown)
- [ ] Reminder notifications to prompt daily journaling
- [ ] Multiple export formats (CSV, plain text, HTML)
- [ ] Entry templates for structured journaling (e.g. Gratitude, Daily Review)
- [ ] Widget support (Android/iOS home screen streak widget)
- [ ] Unit tests for service layer (xUnit)

---

## 🤝 Contributing

Contributions are welcome! To get started:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Ensure the project builds: `dotnet build`
5. Commit with a clear message: `git commit -m "feat: add your feature"`
6. Push your branch: `git push origin feature/your-feature-name`
7. Open a Pull Request

Please keep one feature or fix per PR and test on at least one platform before submitting.

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](./LICENSE) file for details.

---

## 👨‍💻 Author

**Abhimannu**
BSc Computing — Islington College (London Metropolitan University)
Backend Developer

> *Built with .NET MAUI Blazor Hybrid as a personal project — fully offline, fully cross-platform.*
