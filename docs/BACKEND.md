# Backend

The Skill UP backend is plain C# in the Web Forms project (`Backend/`), with no extra NuGet packages. The Web Forms pages call its commands directly; the same commands are also available as the JSON API in [API-CONTRACT.md](API-CONTRACT.md). Everything is read from and written to the SQL Server database.

## Request flow

```text
Frontend page (.aspx)  ──first load──►  SkillPage.OnPreInit ─► Model.Load ─► page renders the data it may show
                       ──postback───►  button handler ─► SkillPage.TryRun ─► Commands.Run (one SQL transaction)
                                                       ─► commit ─► redirect with a notice
JSON API (optional)    ──GET  /api/workspace───────────► ApiHandler ─► Model.Load ─► Workspace.Build(role) ─► JSON
                       ──POST /api/commands/{action}───► ApiHandler ─► Commands.Run (one SQL transaction)
```

- Pages inherit `Frontend/SkillPage.cs`. It loads the account from the sign-in cookie on every request and checks the page's roles before any data is read.
- `App_Start/RouteConfig.cs` sends every `/api/...` URL to `Backend/ApiHandler.cs`. No page uses the API any more; it stays for other clients.
- Each command runs inside one transaction. Any error rolls back everything that command wrote.
- Commands run one at a time (a lock in `SkillPage` and in `ApiHandler`). That is enough for a single IIS Express process. Several servers would need database-level concurrency checks instead.
- A refused command (bad input, no permission, conflict) shows its message on the page and keeps what was typed. The API returns the right HTTP status (400, 401, 403, 404, 409) with `{ "detail": "..." }`. Unexpected errors show the error page and are written to the trace log.

## Security

| Concern | What the backend does |
|---|---|
| Passwords | PBKDF2-SHA256 with a random salt and 100,000 iterations, stored in `[USER].password`. Plain passwords are never stored or returned. |
| Sign-in | An encrypted, HttpOnly, SameSite=Lax forms-authentication ticket in the `SkillUp.Auth` cookie (12 hours). The account is reloaded on every request, so a deactivated account loses access at once. |
| CSRF | The sign-in cookie is SameSite=Lax, so a post from another site arrives signed out. Each page's view state is tied to the signed-in account (`ViewStateUserKey`), and ASP.NET checks postbacks against what was rendered. API commands must also send the `X-SkillUp-Request: 1` header. |
| Authorisation | The role and user ID always come from the cookie, never from the request body. Instructors are limited to courses they are assigned to (`COURSE_INSTRUCTOR`), Program Leaders to their programs, and Admin-only commands check the role. |
| Data scoping | `Workspace.Build` sends each role only what it may see. Learners and visitors never get lab answers or quiz answer keys. Visitors get no emails, messages or discussions. Enquiries are reachable only from the browser that sent them (`ENQUIRY_ACCESS`, keyed by the hashed `SkillUp.Visitor` cookie) and for the signed-in account that sent them. |
| Grading | Lab answers and quiz scores are graded on the server. The quiz answer key is revealed only in the `quizSubmit` result. |
| Tokens | Visitor keys and password reset tokens are stored only as SHA-256 hashes. Reset links expire after 15 minutes and work once. |
| SQL | Every statement is parameterised (`Backend/Db.cs`). |
| Browser storage | The site saves nothing in `localStorage`. Unsaved quiz and learning-path edits are kept in the server session. Program Leader read / archived alerts are in `USER.preferences` (`leaderInbox`). |

## Where each part of the app is stored

The ERD's tables hold the core records; the web app added a few columns to them and 7 tables of its own. `Database/create-tables.sql` creates all of them (the additions are marked), and `Database/backend-extensions.sql` adds them to a database made with an older create-tables.sql; ERD columns and constraints are never removed. The only constraint it replaces is `CK_DISCUSSION_POST_status`, which gains `Flagged`.

| Workspace data | Tables |
|---|---|
| users | `[USER]` + `ROLE`. The display name is `first_name` + `last_name`. Added: `location`, `theme`, `preferences` (JSON); `profile_picture` widened to MAX for uploaded images. Pending instructors also get an `INSTRUCTOR_PROFILE`, approved when an admin activates them. |
| programs | `PROGRAM`. `courseIds` is the program's `LEARNING_PATH` with its ordered `LEARNING_PATH_COURSE` rows (one path per program; the first course is the starting course). |
| courses | `COURSE`. The status is `approval_status`, or Hidden when `visibility_status` is Hidden. The instructor comes from `COURSE_INSTRUCTOR`. Added: `start_date`, `deadline`, `duration_weeks`, `color`, `icon`. |
| course.lessons | `LAB`: each row is one ordered activity. Added: `lesson_type` (article, lab or quiz) and `minutes`. A lab activity's expected answer is its `LAB_TASK.correct_answer`. |
| course.quiz | `QUIZ` (one per course), `QUIZ_QUESTION` (added: `topic`) and `QUESTION_OPTION` (`is_correct` marks the answer). |
| enrollments | `ENROLLMENT` (added: `last_active`). An activity is complete when the learner has a Passed `LAB_SUBMISSION` for it. `progress_percent`, `status` and `completed_at` are kept up to date. |
| attempts | Quiz: `QUIZ_ATTEMPT` + `ATTEMPT_ANSWER`. Lab: `LAB_SUBMISSION` (Passed or Failed) + `TASK_ANSWER`. |
| reviews / feedback | `WEBSITE_FEEDBACK`: rows with a `course_id` are course reviews, rows without one are platform feedback. Added table `FEEDBACK_REPLY` for replies to reviews. |
| posts / globalMessages | `DISCUSSION_POST`: course posts, replies (`parent_post_id`) and community messages (no course). Hashtags are saved in `TAG` / `POST_TAG`. Moderation reasons go to `MODERATION_LOG`. |
| announcements | `ANNOUNCEMENT`. Added table `ANNOUNCEMENT_COMMENT`. |
| messages | `CHAT_MESSAGE`. |
| sessions | `VOICE_CHANNEL` (added: `starts_at`) and `CHANNEL_PARTICIPANT` (an attendee is a row with no `left_at`). |
| ai | `AI_CHAT_SESSION` + `AI_CHAT_MESSAGE` (a User row followed by an AI row). |
| resources | Added table `COURSE_RESOURCE` (file as a data URL, up to 2 MB). |
| courseRequests / approvals | Added tables `COURSE_REQUEST` and `COURSE_REQUEST_EVENT` (Submitted, Review started, Approved, ...). `approvals` is derived from the Create request events. Program Leader decisions are also written to `CONTENT_REVIEW`. The history has no foreign key to the course, so it survives an approved deletion. |
| enquiries | `ENQUIRY`; `ENQUIRY_ACCESS` (added) links a browser to the enquiries it sent. |
| audit | `ACTIVITY_LOG` (added: `detail`). |

Dropped because nothing used them (they are still in the proposal ERD): the `BADGE`, `USER_BADGE`, `NOTIFICATION` and `FAQ` tables, and the columns `[USER].total_points` and `current_streak`, `INSTRUCTOR_PROFILE.expertise`, `qualification`, `teaching_experience` and `certification`, `LAB_TASK.starter_code` and `expected_output` (a lab keeps its code in `LAB`), `TASK_ANSWER.code_output` and `QUIZ.is_ai_generated`. `backend-extensions.sql` drops them from older databases.

## Database setup

`Backend/DatabaseSetup.cs` runs once per app start, on the first API request:

1. If `App_Data\SkillUp.mdf` is missing or has no tables, the API answers 503 with a message to run `Database\setup-db.ps1`.
2. It runs `Database/backend-extensions.sql`. Every step checks first, so this is harmless on a database that already has the extensions.
3. If `SkillUp:SeedDemoData` is true and `[USER]` is empty, it loads `Database/demo-seed.json`. That file is generated from the frontend fixtures with `node scripts/export-demo-seed.cjs` (run from `Frontend/`).

## Web.config switches

| App setting | Debug | Release | Purpose |
|---|---|---|---|
| `SkillUp:DemoLogin` | true | false | The demo role buttons on the login page sign in without a password. |
| `SkillUp:SeedDemoData` | true | false | Load the demo accounts and courses into an empty database. |
| `SkillUp:ShowResetLink` | true | false | No email service yet: `forgot` returns the reset token so the page can show the link. |

`Web.Release.config` sets all three to false when you publish in Release.

## Adding a command

1. Add a `case` in `Commands.Run` (`Backend/Commands.cs`) and a method in the matching `Commands*.cs` file.
2. Validate with `Must(...)`, `CourseFor(id, manage: true)` or `Enrolled(...)`, then write with `db.Exec` / `db.Insert`.
3. Return `null`, a `JObject`, or a `Deferred` when the result needs the reloaded data, for example a new record's DTO.
4. If a page needs new data, add it to `Model.Load` (and to `Workspace.Build` for the API), and add any column to `backend-extensions.sql` with an `IF COL_LENGTH(...) IS NULL` guard.
5. Call it from a page with `TryRun("action", new { ... })` in a button handler, then redirect with `ReloadPage` or `Go`.

## Not done yet

- Email (password reset, registration notices), a real AI model, voice/video calls and SignalR real-time updates.
- File storage: course files live in the database as data URLs.
- Paging: the workspace is one snapshot per request, which is fine for a class-sized dataset.
- `xpLog` in the API workspace is always empty. It was demo-only activity history; the pages use attempts and enrolments.
