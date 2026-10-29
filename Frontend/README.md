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
| `css/` | Stylesheets. Later files override earlier ones; `webforms.css` holds the rules only the server-rendered pages need, and `responsive.css` loads last |
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
- **Add a stylesheet:** put it in `css/` and add the `<link>` to `SkillUp.Master`, before `webforms.css` and `responsive.css`. Asset URLs inside it start with `../assets/`.

## Scripts

| Script | What it does |
|---|---|
| `js/site.js` | Confirms deletes, fades the notice, Enter to send a message, print buttons, figures counting up, the request table's tick boxes |
| `js/activity.js` | Activity page: quiz timer and the lab terminal (runs code in a Web Worker) |
| `js/call-preview.js` | Device-only call preview. `SkillUp.Master` writes `#call-context` (your name, a demo caller, a course) for it |

## Notes

- The site stores data only in the database. Unsaved quiz and learning-path edits are kept in the session, not in the browser.
- Small-screen layouts live in `css/responsive.css`. Check a new screen at 390 px wide as well as on desktop: nothing should scroll sideways.
- The enquiry forms (`Controls/EnquiryForm.ascx`, `EnquiryPortal.aspx`) sit in UpdatePanels, so sending, saving and deleting redraw only that part of the page. `SkillUp.Master` holds the ScriptManager. A button inside an UpdatePanel must not have its icon or label take the click (see the pointer-events rule in `webforms.css`), or ASP.NET AJAX cannot tell which button was pressed.
- Camera preview needs `localhost` or HTTPS.
- The Test video call / Receive call buttons in the navbar are temporary. Turn them off with `SHOW_INCOMING_CALL_TEST` in `js/call-preview.js` (and remove the two buttons from `SkillUp.Master`) before release.
