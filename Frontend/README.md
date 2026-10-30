# Skill UP frontend

Notes for working on the pages. For setup, demo accounts and the overall project layout, see the [root README](../README.md). [docs/WEBFORMS-PAGES.md](../docs/WEBFORMS-PAGES.md) lists every page, who can open it and its query parameters.

## Layout

| Path | Contents |
|---|---|
| `*.aspx` + `*.aspx.cs` | One Web Forms page per screen. The markup uses server controls; the code-behind reads the data and handles the buttons |
| `SkillPage.cs` | Base class of every page: signed-in account, role check, `Run` / `TryRun` for saves, redirect with a notice |
| `SkillUp.Master` | Shared master page: stylesheets, capsule navigation, page frame, notice, scripts |
| `Controls/` | User controls shared by several pages (course card, course files, learner table, lab terminal, the visitor footer, the four role dashboards) and `SubmitButton` |
| `Ui.cs`, `Icons.cs`, `Charts.cs`, `Insights.cs`, `*Art.cs` | Display helpers, inline icons, chart drawing, learning figures, Figma artwork kept as markup |
| `css/` | Stylesheets, one per screen or area (see below) |
| `js/site.js`, `js/activity.js`, `js/call-preview.js` | The only scripts the pages load (see below) |
| `assets/` | Images, icons and fonts used at runtime |
| `design/` | Figma exports and reference images |

The other files in `js/`, and `index.html`, `server.cjs`, `package.json`, `tests/` and `scripts/`, belong to the earlier JavaScript version of the frontend. No page loads them any more, and `.gitignore` keeps them out of the repository.

## How a page works

1. `SkillPage.OnPreInit` loads the data and the account. A page that needs an account sends visitors to `Login`.
2. `Page_Load` reads the query string, fills the controls on the first load and binds the repeaters.
3. A button posts back to its click handler, which calls `TryRun("command", payload)`. The command runs in `Backend/Commands*.cs` inside one transaction.
4. On success the handler redirects (`ReloadPage` or `Go`) with a notice. On failure the page shows the command's message and keeps what was typed.

Text is printed with `<%: %>`, `<%#: %>` or `Ui.H(...)`, so it is always HTML-encoded. Request validation is switched off for these pages so text such as code samples can be typed.

## Common tasks

- **Add a page:** copy an existing pair, inherit `SkillPage`, set `Route` (the navigation item to highlight) and `Roles`, and add the page to `SKILL UP.csproj`. Visual Studio creates the designer file when you save the markup.
- **Add a button that saves:** use `<skill:SubmitButton runat="server" OnClick="…" />` and call `TryRun` in the handler. For deletes and withdrawals add `data-confirm="…"`.
- **Change a screen's look:** edit its stylesheet (below). For a new screen, add a file to `css/` and its `<link>` to `SkillUp.Master`, before `webforms.css` and `responsive.css`. Asset URLs inside it start with `../assets/`.

## Stylesheets

`SkillUp.Master` loads them in this order, and the order matters: when two rules are equally specific, the later file wins. Keep the order when you add rules, and put small-screen rules in `responsive.css`.

| File | Styles |
|---|---|
| `base.css` | Reset, colours, type, buttons, forms, tables, the workspace shell, focus ring and page width |
| `secondary-pages.css` | The smaller pages (Discussions, Live sessions, Certificates, …) |
| `my-courses.css` | My courses |
| `admin-dashboard.css` | Admin overview |
| `course.css` | Course page |
| `course-requests.css` | Course requests (Instructor) |
| `learners.css` | Student performance cards (Instructor's Learners page) |
| `users.css` | User accounts (Admin) |
| `leader-dashboard.css` | Program Leader overview |
| `profile.css` | Profile |
| `header.css` | Capsule navigation and the call preview |
| `catalog.css` | Explore courses |
| `activity.css` | Activity page: article, quiz, practice terminal, learning companion |
| `quiz-editor.css` | Quiz editor (reuses the activity task cards, so it loads after `activity.css`) |
| `home.css` | Landing page, visitor footer, enquiry form and My Enquiries |
| `auth.css` | Log in / Sign up pop-ups, Forgot and Reset password |
| `leaderboard.css` | Leaderboard |
| `course-editor.css` | Course editor |
| `instructor-dashboard.css` | Instructor overview |
| `programs.css` | Programs and the learning path editor |
| `assignments.css` | Instructor assignments |
| `approvals.css` | Course approvals board and request pages |
| `user-editor.css` | Create / manage an account |
| `messages.css` | Messages and announcements |
| `analytics.css` | Learning analytics, and the Program Leader's learners table |
| `learner-dashboard.css` | Learner overview |
| `webforms.css` | Pieces only the server-rendered pages have: pop-ups, validator messages, UpdatePanel buttons, the back-to-top button |
| `responsive.css` | Tablet and phone layouts for every page (loads last) |

Each file is formatted the same way: one selector per line, one declaration per line, four-space indents, a comment above each section.

## Scripts

| Script | What it does |
|---|---|
| `js/site.js` | Grouped by where it runs. Every page: confirm before deletes, the notice fading, show / hide password, Escape closes menus and pop-ups, the back-to-top button. Landing page: section reveal, instructor carousel, the tab highlight, a refresh returns to the top. Workspace: Enter to send a message, print buttons, figures counting up, the request table's tick boxes |
| `js/activity.js` | Activity page: quiz timer and the lab terminal (runs code in a Web Worker) |
| `js/call-preview.js` | Device-only call preview. `SkillUp.Master` writes `#call-context` (your name, a demo caller, a course) for it |

## Notes

- The site stores data only in the database. Unsaved quiz and learning-path edits are kept in the session, not in the browser.
- Small-screen layouts live in `css/responsive.css`. Check a new screen at 390 px wide as well as on desktop: nothing should scroll sideways.
- The enquiry forms (`Controls/EnquiryForm.ascx`, `EnquiryPortal.aspx`) sit in UpdatePanels, so sending, saving and deleting redraw only that part of the page. `SkillUp.Master` holds the ScriptManager. A button inside an UpdatePanel must not have its icon or label take the click (see the pointer-events rule in `webforms.css`), or ASP.NET AJAX cannot tell which button was pressed.
- Camera preview needs `localhost` or HTTPS.
- The Test video call / Receive call buttons in the navbar are temporary. Turn them off with `SHOW_INCOMING_CALL_TEST` in `js/call-preview.js` (and remove the two buttons from `SkillUp.Master`) before release.
