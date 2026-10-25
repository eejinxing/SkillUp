# Skill UP

Skill UP is a role-based online learning platform built for the WAPP group assignment (Group 24). Visitors browse courses, learners enrol and work through activities, labs and quizzes, instructors author courses, program leaders approve them, and admins manage accounts.

The app is an **ASP.NET Web Forms** project (.NET Framework 4.8.1). Each screen has an `.aspx` / `.aspx.cs` entry page, and a shared JavaScript frontend renders the UI inside them.

> **Status:** The frontend is complete and runs on mock data stored in the browser's `localStorage`. The backend does not exist yet: there is no database, real sign-in, email, AI or video-call service. [docs/BACKEND-HANDOFF.md](docs/BACKEND-HANDOFF.md) describes what the C# service needs to implement.

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
3. Press **F5**. IIS Express serves `https://localhost:44353/`, and `Default.aspx` redirects to `Frontend/Home.aspx`.

`.aspx` pages need IIS or IIS Express. Opening them directly from disk does not work.

### Option 2: Frontend only (no .NET)

Requires Node.js 22 or later. No `npm install` is needed.

```bash
cd Frontend
node server.cjs
```

Open <http://127.0.0.1:4185>. This serves `Frontend/index.html`, which is the same app without the Web Forms wrapper.

### Demo accounts

Choose **Log in** and pick a demo role, or sign in with one of these accounts:

| Role | Username | Password |
|---|---|---|
| Learner | `alex` | `Demo1234!` |
| Instructor | `john` | `Demo1234!` |
| Program Leader | `sarah` | `Demo1234!` |
| Admin | `admin` | `Demo1234!` |

All accounts and records are fictional. Data is saved in the current browser and is separate for each origin: IIS Express, `node server.cjs` and `file://` do not share data. To start fresh, clear the site's local storage.

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
├── App_Data/SkillUp.mdf               SQL Server database (37 tables, from the ERD); not in git
├── Database/create-tables.sql         Query that creates those 37 tables in a new SkillUp.mdf
│
└── docs/
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
  - `mode: 'mock'` (default): data lives in `localStorage`, seeded from `js/seed.js` and the `js/*-data.js` fixtures.
  - `mode: 'http'`: `GET {baseUrl}/workspace` and `POST {baseUrl}/commands/{action}`. Use this once the C# API in [docs/API-CONTRACT.md](docs/API-CONTRACT.md) exists.
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

## Database

The database is `App_Data/SkillUp.mdf`, a SQL Server LocalDB file built from the ERD in `docs/proposal/WAPP.drawio`. It has the ERD's 37 tables. Only `ROLE` contains rows (Learner, Instructor, Program Leader, Admin); every other table starts empty.

**View or edit the tables.** In Solution Explorer, double-click `App_Data/SkillUp.mdf`. It opens in **Server Explorer** under **Data Connections**. Expand **Tables**, then right-click a table:

- **Open Table Definition** opens the table designer. After changing a table, click **Update** to apply it.
- **Show Table Data** shows and edits the rows.

**Setting up your copy.** Git ignores `.mdf` and `.ldf` files, so each teammate creates their own database once:

1. Go to **View → Server Explorer**. Right-click **Data Connections** and choose **Add Connection…**.
2. Set **Data source** to **Microsoft SQL Server Database File**. For **Database file name**, browse to the project's `App_Data` folder and type `SkillUp.mdf`. Click **OK**, then **Yes** to create the file.
3. Right-click the new **SkillUp.mdf** connection and choose **New Query**.
4. Paste the contents of [`Database/create-tables.sql`](Database/create-tables.sql) and press **Ctrl+Shift+E**. It ends with `Done: 37 tables created.`
5. Right-click **Tables** and choose **Refresh**.

Until you do this, Solution Explorer shows `SkillUp.mdf` with a missing-file warning. If the tables change, update `Database/create-tables.sql` so everyone can rebuild the same database.

**Connect from C#.** `Web.config` has a connection string named `SkillUp`:

```csharp
var connectionString = ConfigurationManager.ConnectionStrings["SkillUp"].ConnectionString;
using (var connection = new SqlConnection(connectionString)) { /* ... */ }
```

**Things to know:**

- `USER` is a reserved word in SQL Server, so write the table as `[USER]` in every query.
- `[USER].password` is `nvarchar(255)`, long enough to store a hashed password instead of plain text.
- A row that other rows still reference cannot be deleted. For example, a user who created a course cannot be deleted until that course is reassigned. Child rows that only belong to their parent, such as quiz options and lab tasks, are deleted with the parent.
- The JavaScript frontend still uses its browser mock data. Pages read and write this database only once C# code is written for them.

## Known limitations

- Sign-in and permissions are only simulated in the browser. The server must enforce them.
- Seed files contain demo passwords and quiz and lab answers. Remove them before any real deployment; switching to `http` mode alone is not enough.
- AI replies are templates. The lab terminal runs JavaScript in a local Web Worker. Calls show a local camera preview only.
- The layout targets desktop browsers 1280 px wide or more. Narrow windows keep a 1024 px minimum canvas.
