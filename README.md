# Skill UP

Skill UP is a role-based online learning platform built for the WAPP group assignment (Group 24). Visitors browse courses, learners enrol and work through activities, labs and quizzes, instructors author courses, program leaders approve them, and admins manage accounts.

It is an **ASP.NET Core MVC** app (.NET 10, Razor views) that stores everything in **PostgreSQL**. The old Web Forms / LocalDB version has been replaced; page URLs (`/Home`, `/Courses`, `/Dashboard`, ...) are unchanged.

## Features

| Role | What they can do |
|---|---|
| Visitor | Home page, course catalogue and details, FAQ, register / log in / reset password, send an enquiry |
| Learner | Dashboard, My courses, enrolment, sequential activities, virtual lab terminal, timed quizzes, leaderboard, certificates, messages, profile |
| Instructor | Create and edit courses, activities and quizzes; upload resources; submit or withdraw review requests; track learner performance |
| Program Leader | Manage programs and learning-path order; approve, reject or request changes on course requests; learning analytics |
| Admin | Platform overview with activity log and CSV export; create, edit, approve, deactivate and delete user accounts |

## Getting started

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a running PostgreSQL server (tested with 18).

1. Give the app your PostgreSQL password. It is kept in user-secrets, never in the repository:

   ```bash
   dotnet user-secrets set "ConnectionStrings:SkillUp" "Host=localhost;Port=5432;Database=skillup;Username=postgres;Password=YOUR_PASSWORD"
   ```

2. Run it:

   ```bash
   dotnet run
   ```

   The site is at <http://localhost:5080>. On start-up the app creates the `skillup` database if it is missing, runs [`Database/schema.sql`](Database/schema.sql) (all 40 tables, idempotent) and loads the demo data from [`Database/demo-seed.json`](Database/demo-seed.json) into an empty database.

`Start-LAN.cmd` runs the `lan` profile so other devices on the network can open the site (needed for calls between two devices).

### Demo accounts

| Role | Username | Password |
|---|---|---|
| Learner | `alex` | `Demo1234!` |
| Instructor | `john`, `mark`, `lena` | `Demo1234!` |
| Program Leader | `sarah` | `Demo1234!` |
| Admin | `admin` | `Demo1234!` |

All accounts and records are fictional. To start again from the demo data, drop the `skillup` database (`DROP DATABASE skillup;`) and restart the app.

### AI companion (optional)

The AI Assistant and the lesson companion answer with Google Gemini when a key is configured; otherwise they give sample replies:

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY"
```

## Project structure

| Folder | Contents |
|---|---|
| `Controllers/` | One controller per area (Public, Account, Dashboard, Learning, Teaching, Management, Admin, Files) plus `ApiController` for the background JSON calls made by `wwwroot/js/site.js` |
| `Models/` | View models: the logic of each page (what to show for the signed-in user and query string) |
| `Views/` | Razor views and shared layouts/partials; forms post to the controllers with antiforgery tokens |
| `Services/` | Database access (`Db`, Npgsql), `Snapshot`, the business rules (`Commands*.cs`), sign-in cookie, set-up and demo seed |
| `Database/` | `schema.sql` and `demo-seed.json` |
| `wwwroot/` | CSS, JavaScript, fonts, images, uploaded files (`uploads/`, git-ignored) |

Every save runs through `CommandRunner`: one transaction, one command at a time, with all validation and permission checks on the server. Passwords are stored as salted PBKDF2 hashes. Timestamps are stored in UTC and shown in local time.

## Database

PostgreSQL, 40 tables defined in [`Database/schema.sql`](Database/schema.sql). The user table is named `app_user` (`user` is reserved). The four roles (Learner, Instructor, Program Leader, Admin) are inserted by the schema. Change a table by editing `schema.sql` with an idempotent statement (`CREATE TABLE IF NOT EXISTS`, `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`) so existing databases are brought up to date on the next start.

## Known limitations

- No email service: the password reset link is shown on screen in development.
- Calls work only between two users on the same network (no STUN/TURN relay).
- Course files are stored in the database as data URLs (2 MB limit).
- `docs/` still contains the original Web Forms design notes; they describe behaviour, not the current code layout.
