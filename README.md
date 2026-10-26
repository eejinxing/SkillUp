# Skill UP

Skill UP is a role-based online learning platform built for the WAPP group assignment (Group 24). Visitors browse courses, learners enrol and work through activities, labs and quizzes, instructors author courses, program leaders approve them, and admins manage accounts.

The app is an **ASP.NET Web Forms** project (.NET Framework 4.8.1). Each screen has an `.aspx` / `.aspx.cs` entry page, and a shared JavaScript frontend renders the UI inside them.

> **Status:** The Web Forms site now runs on a C# backend (`Backend/`) and the SQL Server database in `App_Data/SkillUp.mdf`. Sign-in, permissions, grading and every save happen on the server; nothing is stored in `localStorage` except UI preferences and enquiry access codes. Email, real AI, video calls and file storage services are not connected yet. [docs/BACKEND.md](docs/BACKEND.md) explains how the backend works.

## Features

| Role | What they can do |
|---|---|
| Visitor | Home page, course catalogue and details, FAQ, register / log in / reset password, send an enquiry and manage it with an access code |
| Learner | Dashboard, My courses, enrolment, sequential activities, virtual lab terminal, timed quizzes, leaderboard, certificates, messages, profile |
| Instructor | Create and edit courses, activities and quizzes; upload resources; submit or withdraw review requests; track learner performance |
| Program Leader | Manage programs and learning-path order; approve, reject or request changes on course requests; learning analytics |
| Admin | Platform overview with activity log and CSV export; create, edit, approve, deactivate and delete user accounts |

Every signed-in role also has messages, discussions, live sessions, an AI companion (sample replies) and profile settings in the top navigation.

## Getting started

### Option 1: Visual Studio (Web Forms)

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

### Option 2: Frontend only (no .NET)

Requires Node.js 22 or later. No `npm install` is needed.

```bash
cd Frontend
node server.cjs
```

Open <http://127.0.0.1:4185>. This serves `Frontend/index.html`, which is the same app without the Web Forms wrapper. There is no C# API here, so this preview still uses the browser mock data in `localStorage`.

### Demo accounts

Choose **Log in** and pick a demo role, or sign in with one of these accounts:

| Role | Username | Password |
|---|---|---|
| Learner | `alex` | `Demo1234!` |
| Instructor | `john` | `Demo1234!` |
| Program Leader | `sarah` | `Demo1234!` |
| Admin | `admin` | `Demo1234!` |

All accounts and records are fictional. In Visual Studio they live in your `App_Data\SkillUp.mdf`, loaded from `Database/demo-seed.json` the first time the app starts with an empty database. To start fresh, stop the site, delete `App_Data\SkillUp.mdf` and `SkillUp_log.ldf`, and run `Database\setup-db.ps1` again. The Node preview keeps its own separate data in the browser.

The demo role buttons (no password), the demo data and the on-screen password reset link are development switches in `Web.config` (`SkillUp:*` app settings). A Release publish turns all three off.

## Project structure

```text
Skill-Up/
├── SKILL UP.slnx / SKILL UP.csproj   Visual Studio solution and Web Forms project
├── Default.aspx(.cs)                  Start page; redirects to Frontend/Home.aspx
├── Global.asax, Web.config, …         ASP.NET application setup
├── App_Start/ Content/ Scripts/       Visual Studio template: routing, bundling, Bootstrap, jQuery
├── About.aspx, Contact.aspx,          Original template pages, kept but not used by Skill UP
│   Site.Master
│
├── Frontend/                          The Skill UP application
│   ├── *.aspx + *.aspx.cs             40 Web Forms entry pages, one per screen
│   ├── SkillUp.Master(.cs)            Shared master page; loads every stylesheet and script
│   ├── index.html                     Standalone entry for the Node preview server
│   ├── css/                           62 stylesheets, loaded in a fixed order
│   ├── js/                            Router, page modules, mock API and seed data
│   ├── assets/                        Images, icons and fonts
│   ├── design/                        Figma exports used by the design build scripts
│   ├── scripts/                       Static checks, test runner, design build tools
│   ├── tests/                         Domain-logic and Playwright browser tests
│   ├── server.cjs                     Static file server for local preview
│   └── package.json                   npm scripts
│
├── Backend/                           C# API: /api/workspace and /api/commands/{action}
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
    ├── MODULE-MAP.md                  Which JS and CSS files own each feature
    ├── WEBFORMS-PAGES.md / .json      Each .aspx page and the route it opens
    ├── ROUTES.json                    All hash routes
    ├── COMMANDS.json                  All API command names
    ├── LOAD-ORDER.json                Stylesheet and script load order
    ├── INTEGRATION-CHANGES.md         What changed when the frontend joined the Web Forms project
    ├── verification/                  Dated verification log and result files
    ├── proposal/                      Assignment proposal and architecture diagram (WAPP.drawio)
    └── history/                       Earlier frontend documentation (archive)
```

## How it works

- **Pages:** Each `.aspx.cs` calls `SetInitialRoute(...)` in `Page_Load`. `SkillUp.Master` writes that route into the URL hash, loads the scripts, and `js/app.js` renders the screen into `<div id="app">`. Navigation after that uses hash routes such as `#/dashboard` and `#/course/c1`. Forms are handled in JavaScript, not by server postbacks.
- **Data:** Every read and write goes through `SkillAPI.request(action, payload)` in `Frontend/js/api.js`. `Frontend/js/runtime-config.js` selects the transport:
  - `mode: 'http'` (the Web Forms pages; `SkillUp.Master` selects it): `GET /api/workspace` and `POST /api/commands/{action}`, answered by `Backend/ApiHandler.cs` from the database.
  - `mode: 'mock'` (`index.html` only): data lives in `localStorage`, seeded from `js/seed.js` and the `js/*-data.js` fixtures.
- **Styles:** Stylesheets in `Frontend/css/` override each other in load order, so order matters. To add or reorder one, update `Frontend/index.html` and `Frontend/SkillUp.Master` in the same way, then run `npm run check`. Asset URLs inside the CSS are relative to `css/`, for example `url('../assets/…')`.

[Frontend/README.md](Frontend/README.md) has notes for working on the frontend.

## Testing

From `Frontend/`:

```bash
npm test
```

This needs no installed packages. It runs:

- `scripts/check-handoff.cjs`: every linked stylesheet and script exists, every CSS asset URL resolves, all JavaScript parses, and every Web Forms page is wired to the master page.
- `scripts/test-domain.cjs`: 9 business-rule suites covering progress, scoring, requests, accounts and similar logic.
- `tests/handoff-contract.cjs`: checks the HTTP adapter against the API contract.

Browser tests use Playwright:

```bash
npm install
npx playwright install chromium
npm run test:ui
```

To test against IIS instead of `index.html`, set `SKILLUP_BASE_URL`. To use an installed browser such as Edge, set `BROWSER_EXECUTABLE_PATH`. Some older tests in `tests/` target earlier layouts and may need selector updates.

The backend has an end-to-end API test for every role (`tests/backend-api.cjs`, about 150 checks). It deletes and deactivates demo accounts, so run it only on a fresh demo database, with the site running:

```bash
NODE_TLS_REJECT_UNAUTHORIZED=0 node tests/backend-api.cjs https://localhost:44353 --fresh-db
```

`NODE_TLS_REJECT_UNAUTHORIZED=0` lets Node accept the IIS Express development certificate; use it only for this local test.

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

The browser still shows each viewer their own clock. The backend sends times to the page as UTC, and the page converts them for display. Times you type into forms, such as a live session's start time, are stored exactly as entered.

Rows created before 2026-10-26 were saved in UTC, so in Malaysia they appear 8 hours earlier than the PC clock. To fix a database that has such rows, run [`Database/shift-existing-times-to-local.sql`](Database/shift-existing-times-to-local.sql) **once**, before using the updated site: stop the site, open a **New Query** on your `SkillUp.mdf` connection in Server Explorer, paste the file and press **Ctrl+Shift+E**. A marker in the database stops a second run from shifting the times again. A database created after 2026-10-26 does not need it.

**Things to know:**

- `USER` is a reserved word in SQL Server, so write the table as `[USER]` in every query.
- `[USER].password` stores a salted PBKDF2-SHA256 hash, never the plain password.
- A row that other rows still reference cannot be deleted. The backend deletes or reassigns dependent rows in the right order, for example when an admin deletes an account or a Program Leader approves a course deletion.

## Known limitations

- No email service: the password reset link is shown on screen (development setting).
- AI replies are templates. The lab terminal runs JavaScript in a local Web Worker. Calls show a local camera preview only. Messages update on refresh, not in real time.
- Course files are stored in the database as data URLs (2 MB limit), not in file storage.
- `Frontend/js/seed.js` and the `*-data.js` fixtures still contain demo passwords and answers for the Node preview. The Web Forms site never sends answers to learners, but remove those files before any real deployment.
- The layout targets desktop browsers 1280 px wide or more. Narrow windows keep a 1024 px minimum canvas.
