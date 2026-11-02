# Backend handoff

> **2026-10-26: the backend now exists.** `Backend/` implements `GET /api/workspace` and every `POST /api/commands/{action}` below against `App_Data/SkillUp.mdf`, and the Web Forms pages use `http` mode. See [BACKEND.md](BACKEND.md). The sections below describe the original frontend handoff; their "no database / no sign-in" notes are historical.

Last updated: 2026-10-20 (Asia/Kuala_Lumpur). This delivery is the **Web Forms .aspx/.aspx.cs file structure plus the existing JavaScript features**. It is not a finished backend.

## File format required by the lecturer
The confirmed requirement is only the .aspx + .aspx.cs structure. The existing JavaScript may stay. Every main screen has its own entry pair, for example:
- Frontend/Login.aspx + Login.aspx.cs
- Frontend/Dashboard.aspx + Dashboard.aspx.cs
- Frontend/Course.aspx?id=c1 + Course.aspx.cs
- Frontend/Activity.aspx?course=c1&lesson=l1 + Activity.aspx.cs

All screens share SkillUp.Master. The C# Page_Load sets each page's initial route. **In-page navigation and form actions are still handled by JavaScript / SkillAPI; they were not rewritten as asp:Button postbacks.**
Every .aspx.cs has a real, compilable Page_Load, so backend teammates can plug services in there. There is no database or real sign-in service yet.
The master page's designer.cs only declares Web Forms controls. It is not a third layer of business logic.
.NET pages must run through IIS / Visual Studio; double-clicking an .aspx does not work. index.html is kept only as a standalone frontend preview that does not need .NET.
The pages sit in the same folder so the existing relative asset paths and hash navigation keep working. No iframes are used.
The full list of entry pages is in [WEBFORMS-PAGES.md](WEBFORMS-PAGES.md).

## Read these files first
1. `Frontend/js/runtime-config.js`: mock / HTTP mode and the API base URL.
2. `Frontend/js/api.js`: `SkillAPI.request(action, payload)`, shared by every page.
3. [API contract](API-CONTRACT.md): DTOs, command payloads, permissions and transaction rules.
4. [Command list](COMMANDS.json).
5. `Frontend/js/seed.js` and `*-data.js`: mock DTO references only. They are not a database or real accounts.

## HTTP boundary to implement
- `GET /api/workspace` returns the workspace JSON that the current identity is allowed to see.
- `POST /api/commands/{action}` returns `{ data: resultOrNull, state: updatedWorkspace }`.
- Failures use the correct HTTP status; the JSON may include `detail` or `message`. The adapter never treats an HTTP failure as success.
- Requests are sent with `credentials: include`. Implement cookie/session authentication and authorization on the server, and add the team's anti-forgery header in this one adapter.
- Visitors also need a state, for example `currentUser: null` plus the list of public courses.
- Use ISO dates. Date-only values use YYYY-MM-DD. Activity timestamps should include an offset; schedules explicitly use Asia/Kuala_Lumpur.

## Suggested integration order
1. DTO / database schema, log in, log out, register, forgot password and role authorization.
2. Role-scoped workspace, courses, enrolment, lesson completion, quiz attempts.
3. Instructor editing and review submission; Program Leader approval and deletion requests, keeping approval events and history.
4. Messages, enquiry CRUD, file storage, course announcements.
5. Admin accounts, statistics and audit logs; real AI and calling services last.

## Current role scope
| Role | Pages and main flows |
|---|---|
| Visitor | Home, course details, log in / register, FAQ, create an enquiry and manage it from the browser that sent it |
| Learner | Dashboard, My courses, Explore courses, activities and quizzes, Leaderboard, messages, profile |
| Instructor | Own courses / activities / quizzes, requests, learner performance, resources, messages |
| Program Leader | Managed programs, course order and assignment, review and deletion-request decisions, learning analytics |
| Admin | Accounts and verification, platform data, user activity logs for all roles, support enquiries |

## Current UI state
- The notification bell for all roles, the standalone notifications/announcements pages and the notification settings tab have been removed.
- Old `#/announcements` and `#/notifications` links go to the dashboard; guests go to home.
- Course announcement data is kept. Announcements on the dashboard are only a summary and no longer link to the removed pages.
- `#/paths` has been removed; course ordering within staff programs has not.
- The Learner dashboard has a purple and black background, black metric cards and an Active Courses list.
- The enquiry form ends with a simple Send enquiry button; a successful send shows a confirmation and lists the enquiry under My enquiries.
- The navbar's Test video call / Receive call buttons are temporary previews that the user asked to keep for now.

## What still needs the backend
- The mock database is localStorage. It is not shared between users and is not a real session.
- The seed data contains demo passwords, lab expected answers and quiz answers. Before release these fixture scripts must be removed or separated; switching the mode to http alone is not enough to go live. The mock initialisation in api.js also needs to be isolated.
- The UI filters some data, but visibility, ownership, ordering and permissions must all be re-checked by the server.
- Quiz scoring and lab pass/fail must run on the server; do not trust the frontend's calculation. AI currently returns rule-based sample replies.
- Calls only simulate an incoming call and a local device preview. There is no signalling, remote media, meeting room or live presence. Before release, turn off `SHOW_INCOMING_CALL_TEST` in `js/workspace-nav.js` and connect a real service.
- Resources and avatars can currently be data URLs. Real files need a storage service, file type and size limits, and access control.
- An enquiry can be listed, updated or deleted only from the browser (ENQUIRY_ACCESS, hashed visitor cookie) or signed-in account that sent it; the server checks this on every command. Never publicly list all enquiries.
- leaderboard-demo-data.js, my-courses-data.js, xp-history-data.js and request-history-data.js are demo / backfill fixtures. They must not be written into real users' history.
- Password reset, email, registration approval notifications, AI, calls and real file storage are not connected yet.
- Design source notes in these files are not a guarantee of third-party asset licences. The team must confirm asset rights before a real release.

## Code structure and how to change it
One entry point loads all page scripts and styles. To keep the current UI, the handoff kept the original load order and CSS file names, and did not do a large bundle/refactor during the move.
runtime-config was added to keep environment switching in one place. The API adapter is the only transport boundary; avoid adding fetch calls in individual pages.
The current design is snapshot/command. If the team prefers REST controllers, translate inside the adapter; the UI does not need to be rewritten.

## Development and testing
- Visual Studio F5: Default.aspx redirects to Frontend/Home.aspx.
- Static mode: run `node server.cjs` in Frontend. It only serves static files; there is no C# API.
- `npm test`: relative path checks, JS syntax, domain suites and the HTTP adapter contract test.
- `npm run test:ui`: after installing the dev dependencies and Playwright Chromium, checks every role, the removed notification pages, enquiries and the desktop layout.
- The original UI screenshot tests are kept in tests, but some target earlier designs and may need selector updates. Do not report all of them as passing.
- The tools in `scripts/` that rebuild assets from Figma exports are a development reference. Before running one, make sure its design input exists, so it does not overwrite the current customised UI.
- The general browser tools no longer use the original machine's Playwright/Sharp module paths. They use the package.json dev dependencies instead, and `BROWSER_EXECUTABLE_PATH` can point to an installed browser.

## Protecting the original project
The Web Forms template setup is kept where the site uses it (App_Start for routing, Scripts/WebForms for the ScriptManager, Global.asax, Web.config); its Bootstrap, jQuery, Modernizr and bundling files were removed on 2026-11-02 because no page loaded them. The template's own pages (About.aspx, Contact.aspx, Site.Master) were removed on 2026-10-29 because nothing used them; Default.aspx is now only the start-up redirect. Added: one .aspx/.aspx.cs pair per page and a shared master page. Changed: the Default.aspx.cs startup redirect, the .csproj content/compile items, and the local start page setting.
Backups of the original integration files are in App_Data/HandoffBackups (not web-accessible).
Do not hand over .vs, bin, obj, node_modules, test output or local backups as backend source.

## Desktop support
The same pages serve desktop, tablet and phone widths; there is no separate mobile master page or view switcher. `Frontend/css/responsive.css` (loaded last) holds the small-screen layouts. Printing and reduced motion are still supported.

## Admin pages removed (2026-10-22)

Platform reports, Pending verifications and Roles & permissions have been removed, along with their Admin navigation and report links. Roles.aspx / Verification.aspx and their code-behind were deleted, leaving 44 entry pairs. Role and activation actions remain in User accounts. Instructor and Program Leader learning analytics use the shared Analytics.aspx. Old Admin hash links redirect to valid pages.

## Admin Active users and audit pages removed (2026-10-22)

The standalone Active users and Activity & audit pages have been deleted: ActiveUsers.aspx, ActivityLogs.aspx, their code-behind, project compile items, routes, navigation and dead links. Frontend now has 42 entry pairs. Old #/active-users and #/activity links go to Overview; signed-out visitors go to Log in. The activity log and CSV export on the Admin overview are kept, and the old activity link now jumps to that section. Activity.aspx is the learner's course activity page, not the deleted Admin audit page, and remains.

## Admin discussion moderation and feedback pages removed (2026-10-22)

The standalone Admin Discussion moderation and Enquiries & feedback pages have been deleted: Moderation.aspx, Support.aspx, their code-behind, project compile items and navigation. Old #/moderation and #/support links go to Overview; visitors go to Log in. Frontend now has 40 entry pairs. Normal course discussions and instructor discussion actions, visitor enquiry submission, the personal enquiry portal CRUD and existing feedback data are all kept; only the named Admin pages were removed.

## Virtual Lab terminal (2026-10-22)

The terminal on the activity page is implemented by js/answer-page.js, css/answer-skill.css and js/app.js together. The task comes straight from the activity title / body. Command, output and history keep the existing sessionStorage drafts per user and activity. Execution state is driven by `data-execution` and `aria-busy` on `.lab-shell`; the segmented loading bar is an in-progress animation that does not show a fake percentage.

Execute currently runs in a local JavaScript Web Worker that can be stopped, times out, and is terminated when leaving the page. It does not provide remote system commands, a MongoDB connection or C# compilation. Answer submission and grading still go through the existing SkillAPI flow. When the backend connects an isolated runner, keep the success / error / stopped lifecycle and have the server verify answers and permissions independently. Frontend/tests/terminal-lab.cjs and tests/answer-page.cjs can be used for regression checks.

## Account editor update (2026-10-22)

The User Management edit and account history pages now use the summary/form layout of the course editor; account creation keeps its row-style form. Both share js/user-editor.js and css/user-editor.css. The existing userSave / userDelete API, role and status options, and draft keys keep their original contract. The summary on the left, the dates and recent activity come from real account data and audit records; when there is none, the page shows Not recorded / No activity recorded.

## Global sidebar and standalone pages removed (2026-10-22)

The Assistant, Discussions, LiveSessions and Settings Web Forms pages and their standalone hash renderers were removed, leaving 36 pairs. Old hashes send signed-in accounts to the dashboard and visitors to login. The global sidebar and drawer are no longer generated. `SkillNavigationItems` in ui-refinement.js passes page data directly to workspace-nav.js, and the remaining entries plus Profile / Help / Sign out moved to the top More menu.

The AI companion inside activities, profile editing and password features, Messages / course announcements and the message call UI are kept. Existing AI / discussion / session / preferences data and API contracts were not deleted; only the named standalone pages and their navigation were removed.

## Sidebar scope correction: features and navbar kept (2026-10-22)

The user confirmed that only the global sidebar should be removed. The four Web Forms pages for Assistant, Discussions, LiveSessions and Settings, their hash renderers and their top navigation entries have been restored, bringing the page count back to 40. This correction replaces the previous section's page removal. The main navbar and the More menu provide the original features; the sidebar / drawer is no longer generated. In-course AI, Profile / Settings, messages, live sessions and discussions continue to use the existing API. The removal of the left information card on Activity Logs stays.

## Project structure cleanup (2026-10-25)

- The 62 stylesheets moved from the Frontend/ root to Frontend/css/ with the same file names and load order. `assets/` paths inside the CSS were changed to `../assets/`. index.html, SkillUp.Master and the .csproj were updated to match. The CSS parsed by the browser is identical to before the move.
- learner-fusion.css, which no page loaded, was deleted.
- API-CONTRACT.md moved to docs/; the verification records and the old handoff-manifest.json moved to docs/verification/ and docs/history/ (both deleted on 2026-11-02).
- The one-off integration scripts scripts/activate-*.cjs (already applied), two tests that only called other tests, and inspect-visitor.cjs (whose output path no longer existed) were deleted.
- For the project overview and folder layout, see the root [README.md](../README.md).

## Web Forms pages (2026-10-28)

- Every page is now a real Web Forms page: the `.aspx` markup uses server controls, the code-behind reads the data and handles the buttons, and saves are postbacks. The JavaScript router (`js/app.js`), the hash routes and `SkillAPI.request` are no longer used.
- Pages inherit `Frontend/SkillPage.cs`, which loads `Backend/Model`, checks the page's roles and saves through `Backend/Commands` (`Run` / `TryRun`), the same commands as the JSON API. After a save the page redirects with a notice.
- The only scripts the pages load are `js/site.js` (small enhancements), `js/activity.js` (quiz timer and lab terminal), `js/pack-opening.js` (tearing program packs open) and `js/call-preview.js` (device-only call preview).
- The look is unchanged: the same stylesheets and class names. Rules needed only by the server-rendered pages are in `css/webforms.css`.
- The rest of `Frontend/js/`, `index.html`, `server.cjs`, `package.json`, `tests/` and `scripts/` belong to the JavaScript version, are no longer loaded and are listed in `.gitignore`. The docs that described that version (load order, route list, module map) were deleted on 2026-11-02. The sections above that mention hash routes, `SkillAPI` or `npm test` describe it too.
- [WEBFORMS-PAGES.md](WEBFORMS-PAGES.md) lists every page, who can open it and its query parameters.
