# Skill UP

Skill UP is a role-based online learning platform built for the WAPP group assignment (Group 24). Visitors browse courses, learners enrol and work through activities, labs and quizzes, instructors author courses, program leaders approve them, and admins manage accounts.

The app is an **ASP.NET Web Forms** project (.NET Framework 4.8.1). Each screen is a Web Forms page: `.aspx` markup with server controls and an `.aspx.cs` code-behind, rendered on the server and saved through postbacks.

> **Status:** The Web Forms site now runs on a C# backend (`Backend/`) and the SQL Server database in `App_Data/SkillUp.mdf`. Sign-in, permissions, grading and every save happen on the server; nothing is stored in `localStorage`. The only thing kept in the browser is two HttpOnly cookies: the sign-in cookie and a random key that links the browser to its enquiries (`ENQUIRY_ACCESS`). Voice and video calls work between two signed-in users on the same network (see [Testing calls between two devices](#testing-calls-between-two-devices)). Email and file storage services are not connected yet; the AI companion uses Google Gemini once you add an API key (see [AI companion](#ai-companion-optional)). [docs/BACKEND.md](docs/BACKEND.md) explains how the backend works.

## Features

| Role | What they can do |
|---|---|
| Visitor | Home page, course catalogue and details, FAQ, register / log in / reset password, send an enquiry and manage it from the same browser |
| Learner | Dashboard, My courses, enrolment, sequential activities, virtual lab terminal, timed quizzes, leaderboard, certificates, messages, profile |
| Instructor | Create and edit courses, activities and quizzes; upload resources; submit or withdraw review requests; track learner performance |
| Program Leader | Manage programs and learning-path order; approve, reject or request changes on course requests; learning analytics |
| Admin | Platform overview with activity log and CSV export; create, edit, approve, deactivate and delete user accounts |

Every signed-in account opens its profile from the avatar in the top navigation. Learners, instructors and program leaders also have Messages, and learners an AI Assistant.

## Getting started

### Visual Studio

1. Open `SKILL UP.slnx` (or `SKILL UP.csproj`) in Visual Studio.
2. Build once to restore the NuGet packages. The `packages/` folder is not committed.
3. Create the local database once. From the project folder:

   ```powershell
   powershell -File Database\setup-db.ps1
   ```

   This creates `App_Data\SkillUp.mdf` on LocalDB and runs `Database/create-tables.sql` (all 40 tables) and `Database/backend-extensions.sql`. It does nothing if the file already exists.
4. Press **F5**. IIS Express serves `https://localhost:44353/`, and `Default.aspx` redirects to `Frontend/Home.aspx`. On the first page load the app finishes the database setup itself: it runs `Database/backend-extensions.sql` (which brings an older database up to date) and loads the demo accounts and courses into an empty database.

`.aspx` pages need IIS or IIS Express. Opening them directly from disk does not work.

If the page shows `Could not find a part of the path '...\bin\roslyn\csc.exe'`, your copy of `SKILL UP.csproj` is older than the fix that copies the compiler on the first build. Pull the latest code, or right-click the solution and choose **Restore NuGet Packages**, then **Build → Rebuild Solution**.

### Demo accounts

Choose **Log in** and sign in with one of these accounts:

| Role | Username | Password |
|---|---|---|
| Learner | `alex` | `Demo1234!` |
| Instructor | `john` | `Demo1234!` |
| Instructor | `mark` | `Demo1234!` |
| Instructor | `lena` | `Demo1234!` |
| Program Leader | `sarah` | `Demo1234!` |
| Admin | `admin` | `Demo1234!` |

All accounts and records are fictional. In Visual Studio they live in your `App_Data\SkillUp.mdf`, loaded from `Database/demo-seed.json` the first time the app starts with an empty database. To start fresh, stop the site, delete `App_Data\SkillUp.mdf` and `SkillUp_log.ldf`, and run `Database\setup-db.ps1` again.

The demo data and the on-screen password reset link are development switches in `Web.config` (`SkillUp:*` app settings). A Release publish turns both off.

### AI companion (optional)

The AI Assistant and the companion on each activity answer with Google Gemini when the site has an API key; without one they give sample replies. Copy `secrets.config.example` to `secrets.config` (next to `Web.config`), paste a free key from [Google AI Studio](https://aistudio.google.com/apikey), and restart the site. `secrets.config` is in `.gitignore`, so the key is never committed; the call to Gemini runs on the server, so the key never reaches the browser.

### Testing calls between two devices

Voice and video calls go straight from one browser to the other (WebRTC). The site only passes the small set-up messages (`Backend/CallSignals.cs`), keeps them in memory for up to 90 seconds, and stores nothing in the database or logs. There is no STUN or TURN server, so **both devices must be on the same network**.

Only one device runs the site; the other just opens it in a browser.

1. On the host PC, build the project in Visual Studio (**Ctrl+Shift+B**), then stop any run Visual Studio started (**F5**).
2. Double-click `Start-LAN.cmd` and accept the administrator prompt. It opens the firewall for ports 8080 and 44390 (only to devices on your own network, and only while the rule exists), starts IIS Express on your network address, and prints the links to use, for example `https://192.168.1.20:44390/`. You do not need to set or type an IP address anywhere.
3. On the other device, open the printed **https** link and continue past the certificate warning once. The camera and microphone only work on a secure page; on the `http://` link that device can still hear and see the other person but cannot send its own camera or microphone.
4. Log in as a **different user** on each device (for example `alex` and `john`), open **Messages**, choose the other person and press the voice or video button. The other device shows an Accept / Decline card on any page.
5. The launcher window lists each device when it joins, with the name it shares with the network (for example `Hongs-iPhone`) or just its address when it keeps its name private. Names depend on your router; they are a best guess.

Testing on one PC needs none of this: use a normal window and a private window on `https://localhost:44353/`.

**If a call does not connect:** check both devices are on the same Wi-Fi (guest networks often block device-to-device traffic; a phone hotspot is a good alternative) and that you are on the `https://` link.

**Security notes**

- Use the `https://` link. On `http://` the sign-in cookie and call set-up messages can be read by others on the Wi-Fi. The call's audio and video are encrypted either way.
- While the launcher runs, anyone on the same network who knows the address can open the site and sign in with the demo accounts, whose password is in this README. Use a network you control.
- The launcher adds two Windows Firewall rules that stay after it closes. Remove them when you finish:

  ```powershell
  netsh advfirewall firewall delete rule name="Skill UP test 8080"
  netsh advfirewall firewall delete rule name="Skill UP test 44390"
  ```
- Any signed-in user can ring any active user and there is no rate limit. This is fine for a demo; a real product needs contact rules and limits.
- Each device sees the other's local network address inside the call set-up data. No IP address is stored in the code or the database.

## Project structure

```text
Skill-Up/
├── SKILL UP.slnx / SKILL UP.csproj   Visual Studio solution and Web Forms project
├── Default.aspx(.cs)                  Start page; redirects to Frontend/Home.aspx
├── Global.asax, Web.config, …         ASP.NET application setup
├── secrets.config.example             Copy to secrets.config for the Gemini API key (secrets.config is not in git)
├── Start-LAN.cmd / Start-LAN.ps1      Runs the site on the local network for two-device call tests
├── App_Start/ Scripts/WebForms/       Routing (friendly URLs, /api); the Microsoft Ajax scripts the ScriptManager loads
│
├── Frontend/                          The Skill UP application
│   ├── *.aspx + *.aspx.cs             37 Web Forms pages, one per screen, rendered on the server
│   ├── SkillPage.cs                   Base page: signed-in account, role check, saves through Backend commands
│   ├── SkillUp.Master(.cs)            Shared master page: stylesheets, navigation, page frame, notices
│   ├── Controls/                      Shared user controls (course card, files, learner table, lab, dashboards)
│   ├── Ui.cs, Icons.cs, Charts.cs, …  Display helpers, icons, charts, Figma artwork kept as markup
│   ├── css/                           Stylesheets, loaded in a fixed order
│   ├── js/                            site.js, activity.js, pack-opening.js and call-preview.js (small browser-only extras)
│   ├── assets/                        Images, icons and fonts
│   └── uploads/                       Course covers uploaded in the course editor; not in git
│
├── Backend/                           C# commands used by the pages, plus the JSON API /api/workspace and /api/commands
│   ├── ApiHandler.cs                  HTTP endpoint, sign-in cookie, CSRF header, error responses
│   ├── CallSignals.cs                 In-memory mailbox that passes call set-up messages between two users (/api/call)
│   ├── Companion.cs                   The learning companion's replies from Google Gemini (sample replies without a key)
│   ├── Commands*.cs                   Every command: rules, permissions, grading, SQL writes
│   ├── Workspace.cs                   Builds the role-scoped workspace JSON for each user
│   ├── Model.cs, Db.cs                Loads the tables; parameterised ADO.NET helper
│   ├── Security.cs                    Password hashing, tokens, auth cookie, Web.config switches
│   └── DatabaseSetup.cs               Applies the extension script and loads the demo data
│
├── App_Data/SkillUp.mdf               SQL Server database (33 ERD tables + 7 added by the web app); not in git
├── Database/
│   ├── create-tables.sql              Creates all 40 tables in a new SkillUp.mdf
│   ├── backend-extensions.sql         Brings a database made with an older create-tables.sql up to date (idempotent)
│   ├── reset-demo-data.sql            Clears all data; the app then loads the demo data again
│   ├── setup-db.ps1                   Creates App_Data\SkillUp.mdf and runs both scripts
│   └── demo-seed.json                 Demo accounts and courses (from the frontend fixtures)
│
└── docs/
    ├── BACKEND.md                     How the C# backend and database fit together
    ├── BACKEND-HANDOFF.md             Backend integration guide
    ├── API-CONTRACT.md                Workspace and command API contract for the C# service
    ├── WEBFORMS-PAGES.md              Every page: who can open it, its query parameters and shared controls
    ├── COMMANDS.json                  All command names
    └── proposal/                      Assignment proposal and architecture diagram (WAPP.drawio)
```

## How it works

- **Pages:** Every page inherits `Frontend/SkillPage.cs`. It loads the database snapshot (`Backend/Model.cs`) and the signed-in account, checks the role, and the page renders its markup on the server. Links are page names without `.aspx`, such as `Course?id=3`; filters and tabs are query parameters.
- **Saving:** Buttons are `skill:SubmitButton` controls that post back. The handler calls `TryRun(action, payload)`, which runs `Backend/Commands*.cs` in one transaction, the same commands the JSON API uses, so every rule and permission check is shared. After a save the page redirects with a short notice, so a refresh never repeats it.
- **Scripts:** Pages work without JavaScript. `js/site.js` adds small conveniences, `js/activity.js` runs the quiz timer and the lab terminal, `js/pack-opening.js` tears program packs open on My courses (without it, the pack's Enroll button still works), and `js/call-preview.js` voice and video calls (WebRTC, with the set-up messages polled from `/api/call`), plus a device-only preview for live sessions.
- **Styles:** Stylesheets in `Frontend/css/` override each other in load order, so order matters; they are linked in `Frontend/SkillUp.Master`. Asset URLs inside the CSS are relative to `css/`, for example `url('../assets/…')`.

[Frontend/README.md](Frontend/README.md) has notes for working on the pages, and [docs/WEBFORMS-PAGES.md](docs/WEBFORMS-PAGES.md) lists every page.

## Testing

Build in Visual Studio: the pages compile with the project, and markup errors show when a page is opened. There are no automated tests for the Web Forms pages yet.

## Database

The database is `App_Data/SkillUp.mdf`, a SQL Server LocalDB file built from the ERD in `docs/proposal/WAPP.drawio`. It has the ERD's tables, with the columns the web app added to them, and 7 tables the web app added. Four ERD tables (`BADGE`, `USER_BADGE`, `NOTIFICATION`, `FAQ`) and ten ERD columns were dropped because nothing used them. [`Database/create-tables.sql`](Database/create-tables.sql) creates all of them and marks what was added. [docs/BACKEND.md](docs/BACKEND.md) lists which table stores each part of the app.

**View or edit the tables.** In Solution Explorer, double-click `App_Data/SkillUp.mdf`. It opens in **Server Explorer** under **Data Connections**. Expand **Tables**, then right-click a table:

- **Open Table Definition** opens the table designer. After changing a table, click **Update** to apply it.
- **Show Table Data** shows and edits the rows.

**Setting up your copy.** Git ignores `.mdf` and `.ldf` files, so each teammate creates their own database once. The quickest way is `powershell -File Database\setup-db.ps1`. To do it by hand in Visual Studio instead:

1. Go to **View → Server Explorer**. Right-click **Data Connections** and choose **Add Connection…**.
2. Set **Data source** to **Microsoft SQL Server Database File**. For **Database file name**, browse to the project's `App_Data` folder and type `SkillUp.mdf`. Click **OK**, then **Yes** to create the file.
3. Right-click the new **SkillUp.mdf** connection and choose **New Query**.
4. Paste the contents of [`Database/create-tables.sql`](Database/create-tables.sql) and press **Ctrl+Shift+E**. It ends with `Done: 40 tables created.`
5. Right-click **Tables** and choose **Refresh**.

You do not need to run `backend-extensions.sql` by hand: the app runs it on every start, and it brings a database made with an older `create-tables.sql` up to date. Until the file exists, Solution Explorer shows `SkillUp.mdf` with a missing-file warning. If the tables change, update `Database/create-tables.sql` (for new databases) and add the same change to `Database/backend-extensions.sql` with an `IF COL_LENGTH(...) IS NULL` or `IF OBJECT_ID(...) IS NULL` check (for existing ones), so everyone ends up with the same database.

**Start again from the demo data.** [`Database/reset-demo-data.sql`](Database/reset-demo-data.sql) deletes every row (accounts, courses, enquiries, messages, anything typed while testing) except the four roles and restarts the ID counters; on its next start the site loads the demo data again. Stop the site first and keep a copy of `App_Data` if you may want the old data back. The steps are at the top of the file.

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

**Things to know:**

- `USER` is a reserved word in SQL Server, so write the table as `[USER]` in every query.
- `[USER].password` stores a salted PBKDF2-SHA256 hash, never the plain password.
- A row that other rows still reference cannot be deleted. The backend deletes or reassigns dependent rows in the right order, for example when an admin deletes an account or a Program Leader approves a course deletion.

## Known limitations

- No email service: the password reset link is shown on screen (development setting).
- The AI companion gives sample replies until a Gemini API key is set, and with a key every account gets a small share of the free tier's quota. The lab terminal runs JavaScript in a local Web Worker. Calls work only between two users on the same network (no STUN/TURN relay), and live sessions still open a local camera preview only. In-call chat goes to the other person through the same relay and is not saved. The Messages page checks for new messages every 3 seconds while it is open (it is not instant, and a hidden tab pauses).
- Course files are stored in the database as data URLs (2 MB limit), not in file storage.
- Every page adapts to desktop, tablet and phone widths. The small-screen rules live in `Frontend/css/responsive.css`, which loads last.
