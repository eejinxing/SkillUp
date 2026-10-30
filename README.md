# Skill UP

Skill UP is a role-based online learning platform built for the WAPP group assignment (Group 24). Visitors browse courses, learners enrol and work through activities, labs and quizzes, instructors author courses, program leaders approve them, and admins manage accounts.

The app is an **ASP.NET Web Forms** project (.NET Framework 4.8.1). Each screen is a Web Forms page: `.aspx` markup with server controls and an `.aspx.cs` code-behind, rendered on the server and saved through postbacks.

> **Status:** The Web Forms site now runs on a C# backend (`Backend/`) and the SQL Server database in `App_Data/SkillUp.mdf`. Sign-in, permissions, grading and every save happen on the server; nothing is stored in `localStorage`. The only thing kept in the browser is two HttpOnly cookies: the sign-in cookie and a random key that links the browser to its enquiries (`ENQUIRY_ACCESS`). Email, real AI, video calls and file storage services are not connected yet. [docs/BACKEND.md](docs/BACKEND.md) explains how the backend works.

## Features

| Role | What they can do |
|---|---|
| Visitor | Home page, course catalogue and details, FAQ, register / log in / reset password, send an enquiry and manage it from the same browser |
| Learner | Dashboard, My courses, enrolment, sequential activities, virtual lab terminal, timed quizzes, leaderboard, certificates, messages, profile |
| Instructor | Create and edit courses, activities and quizzes; upload resources; submit or withdraw review requests; track learner performance |
| Program Leader | Manage programs and learning-path order; approve, reject or request changes on course requests; learning analytics |
| Admin | Platform overview with activity log and CSV export; create, edit, approve, deactivate and delete user accounts |

Every signed-in role also has messages, discussions, live sessions, an AI companion (sample replies) and profile settings in the top navigation.

## Getting started

### Visual Studio

1. Open `SKILL UP.slnx` (or `SKILL UP.csproj`) in Visual Studio.
2. Build once to restore the NuGet packages. The `packages/` folder is not committed.
3. Create the local database once. From the project folder:

   ```powershell
   powershell -File Database\setup-db.ps1
   ```

   This creates `App_Data\SkillUp.mdf` on LocalDB and runs `Database/create-tables.sql` (37 tables) and `Database/backend-extensions.sql`. It does nothing if the file already exists.
4. Press **F5**. IIS Express serves `https://localhost:44353/`, and `Default.aspx` redirects to `Frontend/Home.aspx`. On the first page load the app finishes the database setup itself: it applies `Database/backend-extensions.sql` if needed and loads the demo accounts and courses into an empty database.

`.aspx` pages need IIS or IIS Express. Opening them directly from disk does not work.

If the page shows `Could not find a part of the path '...\bin\roslyn\csc.exe'`, your copy of `SKILL UP.csproj` is older than the fix that copies the compiler on the first build. Pull the latest code, or right-click the solution and choose **Restore NuGet Packages**, then **Build → Rebuild Solution**.

### Demo accounts

Choose **Log in** and pick a demo role, or sign in with one of these accounts:

| Role | Username | Password |
|---|---|---|
| Learner | `alex` | `Demo1234!` |
| Instructor | `john` | `Demo1234!` |
| Program Leader | `sarah` | `Demo1234!` |
| Admin | `admin` | `Demo1234!` |

All accounts and records are fictional. In Visual Studio they live in your `App_Data\SkillUp.mdf`, loaded from `Database/demo-seed.json` the first time the app starts with an empty database. To start fresh, stop the site, delete `App_Data\SkillUp.mdf` and `SkillUp_log.ldf`, and run `Database\setup-db.ps1` again.

The demo role buttons (no password), the demo data and the on-screen password reset link are development switches in `Web.config` (`SkillUp:*` app settings). A Release publish turns all three off.

## Project structure

```text
Skill-Up/
├── SKILL UP.slnx / SKILL UP.csproj   Visual Studio solution and Web Forms project
├── Default.aspx(.cs)                  Start page; redirects to Frontend/Home.aspx
├── Global.asax, Web.config, …         ASP.NET application setup
├── App_Start/ Content/ Scripts/       Visual Studio template: routing, bundling, Bootstrap, jQuery
│
├── Frontend/                          The Skill UP application
│   ├── *.aspx + *.aspx.cs             39 Web Forms pages, one per screen, rendered on the server
│   ├── SkillPage.cs                   Base page: signed-in account, role check, saves through Backend commands
│   ├── SkillUp.Master(.cs)            Shared master page: stylesheets, navigation, page frame, notices
│   ├── Controls/                      Shared user controls (course card, files, learner table, lab, dashboards)
│   ├── Ui.cs, Icons.cs, Charts.cs, …  Display helpers, icons, charts, Figma artwork kept as markup
│   ├── css/                           Stylesheets, loaded in a fixed order
│   ├── js/                            site.js, activity.js, pack-opening.js and call-preview.js (small browser-only extras)
│   ├── assets/                        Images, icons and fonts
│   └── design/                        Figma exports and reference images
│
├── Backend/                           C# commands used by the pages, plus the JSON API /api/workspace and /api/commands
│   ├── ApiHandler.cs                  HTTP endpoint, sign-in cookie, CSRF header, error responses
│   ├── Commands*.cs                   Every command: rules, permissions, grading, SQL writes
│   ├── Workspace.cs                   Builds the role-scoped workspace JSON for each user
│   ├── Model.cs, Db.cs                Loads the tables; parameterised ADO.NET helper
│   ├── Security.cs                    Password hashing, tokens, auth cookie, Web.config switches
│   └── DatabaseSetup.cs               Applies the extension script and loads the demo data
│
├── App_Data/SkillUp.mdf               SQL Server database (37 ERD tables + extensions); not in git
├── Database/
│   ├── create-tables.sql              Creates the 37 ERD tables in a new SkillUp.mdf
│   ├── backend-extensions.sql         Extra columns and tables the web app needs (idempotent)
│   ├── setup-db.ps1                   Creates App_Data\SkillUp.mdf and runs both scripts
│   └── demo-seed.json                 Demo accounts and courses (from the frontend fixtures)
│
└── docs/
    ├── BACKEND.md                     How the C# backend and database fit together
    ├── BACKEND-HANDOFF.md             Backend integration guide
    ├── API-CONTRACT.md                Workspace and command API contract for the C# service
    ├── WEBFORMS-PAGES.md              Every page: who can open it, its query parameters and shared controls
    ├── COMMANDS.json                  All command names
    ├── INTEGRATION-CHANGES.md         What changed when the frontend joined the Web Forms project
    ├── verification/                  Dated verification log and result files
    ├── proposal/                      Assignment proposal and architecture diagram (WAPP.drawio)
    └── history/                       Earlier frontend documentation (archive)
```

## How it works

- **Pages:** Every page inherits `Frontend/SkillPage.cs`. It loads the database snapshot (`Backend/Model.cs`) and the signed-in account, checks the role, and the page renders its markup on the server. Links are page names without `.aspx`, such as `Course?id=3`; filters and tabs are query parameters.
- **Saving:** Buttons are `skill:SubmitButton` controls that post back. The handler calls `TryRun(action, payload)`, which runs `Backend/Commands*.cs` in one transaction, the same commands the JSON API uses, so every rule and permission check is shared. After a save the page redirects with a short notice, so a refresh never repeats it.
- **Scripts:** Pages work without JavaScript. `js/site.js` adds small conveniences, `js/activity.js` runs the quiz timer and the lab terminal, `js/pack-opening.js` tears program packs open on My courses (without it, the pack's Enroll button still works), and `js/call-preview.js` the device-only call preview.
- **Styles:** Stylesheets in `Frontend/css/` override each other in load order, so order matters; they are linked in `Frontend/SkillUp.Master`. Asset URLs inside the CSS are relative to `css/`, for example `url('../assets/…')`.

[Frontend/README.md](Frontend/README.md) has notes for working on the pages, and [docs/WEBFORMS-PAGES.md](docs/WEBFORMS-PAGES.md) lists every page.

## Testing

Build in Visual Studio: the pages compile with the project, and markup errors show when a page is opened. There are no automated tests for the Web Forms pages yet. The earlier JavaScript frontend's npm scripts and tests (`Frontend/tests/`, `Frontend/scripts/`) no longer apply to the pages and are listed in `.gitignore`.

## Database

The database is `App_Data/SkillUp.mdf`, a SQL Server LocalDB file built from the ERD in `docs/proposal/WAPP.drawio`. It has the ERD's 37 tables, plus the columns and five tables in `Database/backend-extensions.sql` that the web app needs. [docs/BACKEND.md](docs/BACKEND.md) lists which table stores each part of the app.

**View or edit the tables.** In Solution Explorer, double-click `App_Data/SkillUp.mdf`. It opens in **Server Explorer** under **Data Connections**. Expand **Tables**, then right-click a table:

- **Open Table Definition** opens the table designer. After changing a table, click **Update** to apply it.
- **Show Table Data** shows and edits the rows.

**Setting up your copy.** Git ignores `.mdf` and `.ldf` files, so each teammate creates their own database once. The quickest way is `powershell -File Database\setup-db.ps1`. To do it by hand in Visual Studio instead:

1. Go to **View → Server Explorer**. Right-click **Data Connections** and choose **Add Connection…**.
2. Set **Data source** to **Microsoft SQL Server Database File**. For **Database file name**, browse to the project's `App_Data` folder and type `SkillUp.mdf`. Click **OK**, then **Yes** to create the file.
3. Right-click the new **SkillUp.mdf** connection and choose **New Query**.
4. Paste the contents of [`Database/create-tables.sql`](Database/create-tables.sql) and press **Ctrl+Shift+E**. It ends with `Done: 37 tables created.`
5. Right-click **Tables** and choose **Refresh**.

You do not need to run `backend-extensions.sql` by hand: the app applies it on first start. Until the file exists, Solution Explorer shows `SkillUp.mdf` with a missing-file warning. If the tables change, update `Database/create-tables.sql` or `Database/backend-extensions.sql` so everyone can rebuild the same database.

**Connect from C#.** `Web.config` has a connection string named `SkillUp`:

```csharp
var connectionString = ConfigurationManager.ConnectionStrings["SkillUp"].ConnectionString;
using (var connection = new SqlConnection(connectionString)) { /* ... */ }
```

**Roles.** `[USER].role_id` points to the `ROLE` table. The numbers are fixed because `Database/create-tables.sql` inserts the four roles in this order:

| `role_id` | Role | What they do |
|---|---|---|
| 1 | Learner | Enrols in courses and completes activities and quizzes |
| 2 | Instructor | Creates and teaches courses, tracks learner performance |
| 3 | Program Leader | Manages programs and approves course content |
| 4 | Admin | Manages user accounts and the platform |

To see them in the database, run `SELECT role_id, role_name FROM ROLE;`. To list users with their role names:

```sql
SELECT u.user_id, u.username, r.role_name
FROM [USER] u JOIN ROLE r ON r.role_id = u.role_id;
```

**Timestamps.** Every `created_at`, `last_login` and similar column holds the **current time of the computer running the site**, with no time zone stored. If your PC clock says 18:04 when you register, the row shows `18:04`. Two places set it:

- The SQL column defaults use `SYSDATETIME()`, the PC's local time.
- The backend (`Backend/Commands.cs`) uses `DateTime.Now`, also the PC's local time.

Pages show these times as stored, in the site PC's local time (the Program Leader request board shows Malaysia time, MYT). The JSON API sends them as UTC. Times you type into forms, such as a live session's start time, are stored exactly as entered.

Rows created before 2026-10-26 were saved in UTC, so in Malaysia they appear 8 hours earlier than the PC clock. To fix a database that has such rows, run [`Database/shift-existing-times-to-local.sql`](Database/shift-existing-times-to-local.sql) **once**, before using the updated site: stop the site, open a **New Query** on your `SkillUp.mdf` connection in Server Explorer, paste the file and press **Ctrl+Shift+E**. A marker in the database stops a second run from shifting the times again. A database created after 2026-10-26 does not need it.

**Things to know:**

- `USER` is a reserved word in SQL Server, so write the table as `[USER]` in every query.
- `[USER].password` stores a salted PBKDF2-SHA256 hash, never the plain password.
- A row that other rows still reference cannot be deleted. The backend deletes or reassigns dependent rows in the right order, for example when an admin deletes an account or a Program Leader approves a course deletion.

## Known limitations

- No email service: the password reset link is shown on screen (development setting).
- AI replies are templates. The lab terminal runs JavaScript in a local Web Worker. Calls show a local camera preview only. Messages update on refresh, not in real time.
- Course files are stored in the database as data URLs (2 MB limit), not in file storage.
- The old JavaScript files in `Frontend/js/` (for example `seed.js` and the `*-data.js` fixtures) contain demo passwords and answers. No page loads them, but remove them before any real deployment.
- Every page adapts to desktop, tablet and phone widths. The small-screen rules live in `Frontend/css/responsive.css`, which loads last.
