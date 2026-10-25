# Skill UP frontend handoff

## Program Management and request approvals (latest update)

Program Leader `#/programs` follows Figma `250:4767`: three columns of black-and-grey course document cards, the New Program bento illustration and the full grey navigation. The code is `js/program-management.js` / `program-management.css`; the eight original assets are in `assets/program-management/`, and the context / inventory is in `design/program-management-*`. `scripts/build-program-art.cjs` keeps the native blocks, shadows, rotation and + sign from source node `250:5017` and outputs `js/program-management-art.js`. The page shows the three real managed programs and does not repeat fake cards to fill six slots.

Clicking a program opens `#/programs/{id}`. There you can edit the name / description / publication state, create a course in the program, add, remove and reorder courses in the learning path, then Save path. `course.programId` is the course's owner; `program.courseIds` is the ordered subset in the path. Removing a course from the path does not delete the course or student records, and All assigned courses can still edit it or Move program. Add existing course moves a course in from another program you manage. A move keeps materials, questions, instructor, publication, enrolments, attempts and progress, clears it from the old path, and a checkbox decides whether to add it to the new path. Courses pending review or pending deletion approval must be withdrawn first. If either path has an unsaved draft, Save or Discard it first. A non-empty program cannot be deleted. The path draft key is `skillup.program.path.{leaderId}.{programId}` and is cleared only on a successful save or Discard.

`#/approvals` follows Figma `250:3682`: the black-and-grey Requests board, the original pink program badge, a grey rail, a real date picker strip and a purple glow at the bottom. The runtime is `js/leader-applications.js` / `leader-applications.css`; the source is `design/leader-approvals-source.jsx.txt` and the original SVG is `assets/leader-approvals/imgVector.svg`. Assets keep their intrinsic dimensions and are scaled by a wrapper transform; no screenshot is used as a background. The New / Rejected / Approved columns are grouped by the actual status of the managed programs' courseRequests, rather than treating every Published course as an approved request. Changes requested / Withdrawn appear under Other outcomes, and Table shows every status. Search, program / type / date filters and the Board/Table choice are saved per Leader in sessionStorage. Dates use Asia/Kuala_Lumpur and the calendar crosses months correctly instead of copying the source image's non-existent September 31. The source's fake Urgent / Event tags were replaced by the real request type and requester.

`#/approvals/{requestId}` shows the request reason, current course information, activities, quiz answers, files, and request progress / feedback. The Approve / Reject card buttons preselect a decision, which is then submitted from the detail form with feedback, so the content is never skipped. Create requests can be Approved / Rejected / Changes requested; Delete requests can be Approved / Rejected. An approved Create publishes the course. An approved Delete rechecks enrolments, then deletes the course and its materials while keeping the request events; a rejected Delete keeps the course. Completed or withdrawn requests are read-only and are not reopened even if the same course is submitted again later. Create reviews carry the requestId, and the service must atomically verify it matches the current pending request, so an old tab cannot approve a newer request by mistake. The unsaved feedback draft key is `skillup.approvals.decision.{leaderId}.{requestId}`; it survives errors and refreshes and is cleared on success. Dashboard pending items link to the new detail page; the old `review/{courseId}` and `request-review/{id}` routes stay compatible.

Verification: 43 Program Management data checks and the full browser flow. The Applications data/browser tests cover actually creating / rejecting / resubmitting / approving, approving and rejecting deletions, syncing Instructor feedback, error retry / restore, permissions, history, MYT dates and widths of 1440 / 960 / 390. The existing 41 domain assertions, course request domain/UI, Leader dashboard and the 60-route role check pass. New previews are `previews/program-management-*`, `program-course-editor-*`, `leader-applications-*` and `leader-application-detail*`; verification JSON records the results. Tests run in an isolated browser and do not write to the user's current demo data. This is still an HTML/CSS/JavaScript frontend with a mock API, not connected to a real C# service.

## Program Leader Dashboard (this update)

The Program Leader's `#/dashboard` was rebuilt from Figma Frame 172 (`250:5041`, page `126:20`): grey navigation, four KPIs, segmented activity and completion charts, Pending Approval, Messages, AI Companion and Announcements. The home page, Program Management and Requests use the full width with a menu drawer; other Program Leader pages keep the original navigation. It uses `js/leader-dashboard.js` and `leader-dashboard.css`. The original frame was too large, so its full context was read in sections; 31 sources are stored in `design/leader-*.jsx.txt`, and 87 original assets and their inventory are in `assets/leader-dashboard/` / `design/leader-dashboard-assets.json`. `scripts/build-leader-dashboard-art.cjs` keeps the layers, glow, rotation and blend modes of the four original mini bar charts and the two announcement glass bubbles, and outputs `js/leader-dashboard-art.js`. SVG intrinsic dimensions are unchanged; they are scaled by a wrapper transform with asset overflow clipped.

Scope: managed programs are selected by `program.leaderId`, then courses, enrolments, attempts and announcements by programId. The KPIs are the number of programs, the number of courses, the number of unique currently Active Learners with a record in the past 30 calendar days, and the average activity completion across enrolments. The Active comparison is the difference in unique learners against the previous 30-day period, not the source image's fixed growth percentage. Below the completion rate, completed enrolments / total enrolments is also listed. No values are invented when there are no records, courses or enrolments.

The activity chart counts `attempts.createdAt` over the selected 24 / 14 / 7 / 1 days and aligns it day by day with the previous period of the same length: white is the selected period, grey the previous one, and the comparison figure is the difference in total attempts. The Today option uses the source image's "now". Invalid and future timestamps are excluded. The Y axis adapts to the current and previous totals, with a minimum of 4 so count ticks are integers. The unit is learning attempts, not sign-ins, live presence or people. The right-hand chart shows each course's average completion from current enrolments and is not filtered by the date range; the screen labels it Current average progress, and courses without enrolments show a baseline explained by an aria-label. The activity and Active trends can only use the latest records in the current snapshot; a real service needs aggregated event data for full historical analysis. Bars follow the actual sample data instead of copying the source image's fixed bar heights.

Pending Approval merges Pending review Create / Delete courseRequests within managed programs, and stays compatible with older Pending review courses that have no request; a Create fallback for the same course is not duplicated. Create and Delete items with a courseRequest link to `#/approvals/{requestId}`; the old course fallback still links to `#/review/{courseId}` and is decided by the existing review commands. Up to 8 items are shown, with View all. Messages lists instructors / learners within scope plus contacts with existing conversations, sorted by most recent conversation with real unread counts. With no conversations it shows Start a conversation; searching opens the existing chat page. No messages are invented.

The AI uses the existing `ai` command and that Leader's own history, labelled Sample replies; the + button can insert program context. The announcement feed is the real announcements of managed courses, up to 4 shown and scrollable. Below it, choose a Published course and enter a title and body to publish with the existing announcementSave; publishing is disabled when there is no Published course. The two composer drafts are stored in sessionStorage as `skillup.leader.ai.{id}` / `skillup.leader.announcement.{id}`, cleared only after a successful save and kept on error. No new API commands were added.

Verification: `tests/leader-dashboard-data.cjs` (program scope, activity periods / unique learners, completion, Create / Delete queue, empty data) and `tests/leader-dashboard.cjs` (original assets, period switch / restore, message search / navigation / unread, AI, announcement failure retry and restore, review / deletion links, metrics updating after learning completion, mobile and an unassigned Leader), plus the existing role navigation check. Previews are `previews/leader-dashboard-desktop.png` / `leader-dashboard-populated.png` / `leader-dashboard-mobile.png`; the report is `leader-dashboard-verification.json`. Tests use isolated data and do not change the real workspace in the user's browser.

## Admin user details / new account (this update)

`#/users/{id}` and `#/users/new` share the full-page Figma `250:1980` form, replacing the old Edit / Add User modal. The left side shows the profile photo, name, email, read-only ID, Joined and Last active. On the right, Profile & access provides name / email / username / bio / status / role, all submitted with one Save. New accounts also need a Temporary password (at least 8 characters); the ID is generated by the mock API and cannot be edited in the form. Old `#/users/{id}/access` and `/information` links open the same combined form; `/audit` opens Activity Logs; `/learning` keeps enrolments and assigned courses. All Admin user pages now use the menu drawer.

`js/user-editor.js` and `user-editor.css` use the original sidebar, fields, tabs, icons, lock and role menu. 27 original assets are in `assets/user-editor/`. `scripts/build-user-editor-art.cjs` writes the role menu's original glow / inner layers / arrow into `js/user-editor-art.js`, keeping the source's 15 layers plus the extra arrow. SVG intrinsic dimensions are unchanged and scaled by a wrapper transform. The source avatar is only a default when a user has no avatar; the sample John Doe / MongoDB copy is not treated as real data.

Account dates: new accounts store a real `createdAt`; existing records without a date show Not recorded rather than an invented join date. Last active is the latest valid, non-future time actually produced by that account across the account record and enrolments / attempts / messages / audit; an admin editing someone else's data does not count as that person's activity. Activity Logs are selected by actorId or the new `targetUserId`, and stay compatible with the old `User account saved` email matching. `userSave` returns `{id}`; both create and update write an account-save record with `targetUserId`. The current frontend also copes with a response missing the id by using the submitted id or finding the account by email in the updated snapshot. An unknown id is never silently created as a new account.

Deactivate / reactivate / approve go through the existing `userSave`: after confirmation they change the status using the saved account fields, without also submitting the unsaved form. Delete uses `userDelete` with explicit confirmation and reassignment protection, and returns to the user list on success. The deactivate / delete buttons are disabled for your own account, and a direct demotion or deactivation is still blocked by the API. Name / email / username are trimmed before saving, email is lowercased, and the uniqueness check ignores account case. Unsaved fields are stored per admin / target ID as `skillup.user.editor.{adminId}.{id}`; the password is never written to the tab draft. The matching draft is cleared after a successful save / Discard / status action, and on error the form and retry state are kept.

Verification: `tests/user-editor.cjs`, `tests/user-editor-data.cjs`, `tests/admin-users.cjs` (directory-to-form link), `tests/role-completion.cjs` (60 routes) and the original 41 domain assertions. Previews: `admin-user-edit-{desktop,mobile}.png`, `admin-user-create-desktop.png`, `admin-user-activity.png`; report `previews/user-editor-verification.json`. The C# service still has to implement the same authorization, validation and time / audit fields.

## Admin User Management (this update)

`#/users` was rebuilt from Figma `250:1288`: six original blue glow layers, six columns of capsule cards, Search / Add User / Load More. Narrower screens drop to four / three / two columns. `js/admin-users.js` and `admin-users.css` form a standalone page; the home page and user list use the navigation drawer, while account details and other Admin features keep the existing navigation. The source's role avatars are only defaults for users without an avatar; real avatars still come from user data. Each card shows the name, role and a dot for status, and the full status is also in the link's accessible name and title.

Search by name / email / username / ID, plus role and status filters. After filtering, the first 60 results are shown again; Load More shows another 60 and is hidden once everything is loaded. Only real sample data is listed; duplicate users are not added to fill 60 slots. Filters and the visible count are saved in the current tab's sessionStorage as `skillup.admin.users.{adminId}`. A production service with large data needs server-side search and paging; this is a frontend slice of the authorised snapshot.

Clicking a card opens the existing `#/users/{id}` with User information / Role & access / Learning activity / Activity logs. Each card's ⋯ menu connects to the existing create / edit / approve / delete commands. The `userSave` API keeps the email / username uniqueness check and prevents removing your own Admin role or active status; `userDelete` keeps the protection for your own account and for assigned courses / programs. Deletion requires explicit confirmation. No new API commands were added and nothing was written to Figma. Tests use isolated browser data, so extra test users are not added to the user's real workspace.

Verification: `tests/admin-users.cjs`, covering original assets / geometry, search / filters / empty state / reload, account details and permissions, CRUD / approval, duplicate and self-permission protection, 60 + 5 paging with 65 records, keyboard focus, mobile navigation and access denial for non-Admins. Previews: `previews/admin-users-desktop.png` (the 7 real seed users), `admin-users-full-directory.png` (65 users in an isolated test), `admin-users-mobile.png`.

## Admin Dashboard (this update)

The Admin's `#/dashboard` now follows Figma `250:845`: a wide blue banner, coloured Summary spheres, Active learners, Course completions, a blue-cyan to-do panel and a black course table. Original assets are in `assets/admin-dashboard/` and the source / inventory in `design/admin-dashboard-*`. `js/admin-dashboard-art.js` is produced by `scripts/build-admin-dashboard-art.cjs` and keeps the relative positions of the original shapes and glow; the runtime code is `js/admin-dashboard.js` and `admin-dashboard.css`. The Admin home uses the menu drawer; other Admin pages keep the original navigation.

Summary: Users is all accounts, Courses all courses, Programs all programs, Quizzes the number of course.quiz entries that have questions, and Assessments the number of lab / quiz activities. The spheres and legend use the same numbers. Active learners is the number of unique currently Active Learner accounts with a record in enrolment.lastActive or attempts.createdAt within the selected 7 / 30 / 90 days, excluding other roles, deactivated accounts and invalid / future times. The comparison is the difference in unique learners against the previous period of the same length. The daily trend is also de-duplicated by account; it is not live presence or full sign-in analytics. The current snapshot only has the latest enrolment activity and attempt records, so a real service needs full events or aggregated reports.

Course completions is the number of enrolments with `SkillData.progress === 100`, shown with the total enrolment denominator. The bar chart shows each course's average completion (0–100%); courses without enrolments show a baseline, and the table shows "—". The source's fixed curve, bar heights and growth percentage were replaced by real demo records. The activity period choice is stored only in sessionStorage as `skillup.admin.activityWindow`.

The course table can be searched by name / instructor and filtered by status; clicking a course name shows its data and opens the instructor's account. Export CSV uses the existing analytics export. The account, pending approval, report, Activity and Support entries are connected. Returning to the home page after approving an account recalculates the to-do count. Decisions on pending courses and deletion requests are still made by the Program Leader; the Admin to-do card only links to platform reports. Tests: `tests/admin-dashboard-data.cjs`, `tests/admin-dashboard.cjs`; previews: `previews/admin-dashboard-{desktop,mobile}.png`.

## Instructor Creating / Deleting request list (this update)

`#/requests` and `#/requests/{requestId}` use Figma Frame 201 (`294:18`): a grey left column / request rows, with Request overview and Track Progress on the right. It supports searching by course / reason, Create / Delete type and status filters, selecting a request, and a native collapsible timeline. Entry points were added to the Instructor navigation, the Dashboard, the quick-select bar on the student performance page and the course editor.

A new snapshot collection `courseRequests` stores requesterId, kind (Create / Delete), courseId, a courseTitle snapshot, programId, status, reason, createdAt and events. Existing approvals with Submitted records whose course still exists are converted into historical requests, once per submissionId. A resubmission is a new request and earlier results are kept. Unsent Drafts are not listed as submitted requests. The original courseAction submit / withdraw and review decisions update the same request timeline. Start review is a real Program Leader action; "read" or approval events are never invented automatically.

The Instructor's former Delete course is now Request deletion and needs a reason; a deletion request does not remove the course immediately. The Program Leader opens `#/request-review/{requestId}` from Course deletion requests below Course approvals and can Start review, Approve or Reject. On Approve, the absence of enrolments is checked again before removing the course, its program path references and its materials / discussions / announcements / sessions / reviews / attempt records; the request snapshot and timeline are kept. Courses that already have students must use Hide course instead. Pending deletion locks course management changes; the Instructor can withdraw it from My requests and then edit. Withdrawing a Create request returns the course to Draft.

Deletion requests, withdrawals and review commands use the mock API and are stored in localStorage; no C# backend was built. A real service must restrict reads and writes by requesterId and program ownership, and use transactions for atomic approval / deletion and duplicate-submission protection. Filters are stored only in the current tab's sessionStorage as `skillup.requests.filters.{userId}`. Previews are `previews/instructor-course-requests-{desktop,mobile}.png`; tests are `tests/course-requests-domain.cjs` and `tests/course-requests.cjs`.

## Course information create / edit (this update)

`#/course-edit/new`, `#/course-edit/new/{programId}` and `#/course-edit/{courseId}` share the Figma `250:3989` layout. The source's HTML5 artwork, sidebar background, tab background and blue-cyan field assets are in `assets/course-editor/`. `js/course-editor-art.js` is generated from the source layer placements; the editable form is not replaced by a full-page screenshot. The runtime code is `js/course-editor.js` and `course-editor.css`, with styles isolated under the `ci-` prefix so the learner Enroll page is not affected.

Saved fields are title, programId, difficulty, deadline, duration, description, outcomes and the new startDate. deadline / startDate may be empty; when both are filled, the start date must not be after the deadline. Assigned Instructor is the current assignment, and a Program Leader can change it with Change assignment in the sidebar. Course Status and Visibility are read-only review results and cannot be published from the form. Saving an existing Published course still returns it to Draft; a course pending review cannot be changed until the submission is withdrawn.

The Learners tab lists this course's students and links to Student performance; Learning Resources keeps resourceSave / resourceDelete / download. Both tabs are enabled only after a new course is saved. Activity CRUD, quiz editing, review submission and review history stay below Information. If there are unsaved changes, submitting for review prompts you to save first.

Draft keys: `skillup.course.editor.{userId}.{courseId}`; for new courses, `skillup.course.editor.{userId}.new.{programId or empty string}`. Drafts exist only in the current tab's sessionStorage, are cleared after saving or confirming Discard, and simulated failures keep the input. `tests/course-editor.cjs` verifies create, edit, dates, drafts, tabs, review lock / withdrawal, unauthorised access and mobile; previews are `previews/course-information-{edit,create}-desktop.png` and `previews/course-information-edit-mobile.png`.

## Instructor question editor and student performance (this update)

`#/quiz-editor/{courseId}` follows Figma `250:1557`: the original AI sphere on the left, MARATHON, editable question cards and the Submit / Hint glass buttons. The silver Submit and Save quiz both save the whole quiz; the blue Hint gives authoring guidance. You can add and remove questions and 2–6 options, choose the correct answer, enter a topic, set the time limit and enable the quiz. `published` means the quiz is enabled, but the course still needs review. Saving returns the course to Draft, and editing is blocked while pending review. Deleting the quiz requires confirmation.

The editing draft is stored in the current tab's sessionStorage as `skillup.quiz.editor.{userId}.{courseId}`; the AI input draft uses the same key plus `.ai`. Reloading, returning to the page and sending to the AI do not overwrite unsaved questions. The editing draft is cleared after saving or confirming Discard draft, and also after a successful quiz deletion. On failure, the input is kept and an error is shown. The AI is still a local template reply, not connected to an external model.

For Instructors, `#/learners` uses the Figma Frame 190 (`250:1979`) card layout; `#/learners/course/{courseId}` preselects a course. Each card is one enrolment the Instructor may view, showing the learner's name, email, course, completion, completed / total activities, average quiz score and most recent activity. With no quiz attempt it shows "—", not 0%; red is used only below 70%. Search, course / progress filters, sorting, Enrol, Remove and opening the individual record are all connected. The individual page keeps completion types, incorrect topics and attempt history.

Data is calculated from the current snapshot; no backend commands were added. The real ASP.NET service must filter data by instructor assignment / program ownership on the server and verify authorization. The average only counts submitted quizzes and does not mix in labs. Most recent activity is the enrolment's lastActive, not live online status. Previews: `previews/instructor-question-editor-{desktop,mobile}.png`, `previews/instructor-student-performance-{desktop,mobile}.png`. Test: `node tests/instructor-editor-performance.cjs`.

This delivery uses HTML, CSS and JavaScript and covers visitors, learners, Instructors, Program Leaders and Admins, including log in / register / profile flows. No frontend framework needs to be installed.

## Opening

Open `index.html` directly. Running `node server.cjs` and opening `http://127.0.0.1:4185` is recommended, to make connecting ASP.NET easier later. This Node file only serves static files; it is not the business backend.

The login page has demo role buttons, or use the accounts below; the password is `Demo1234!` for all:

| Role | Username |
|---|---|
| Learner / Register User | alex |
| Instructor | john |
| Program Leader | sarah |
| Admin | admin |

Unregister User is the signed-out visitor state and does not need a separate account.

## Register User home page update

The learner's `#/dashboard` was redone from Figma `250:466`, keeping the small blue glass cards, cyan foil course cards, Global, and the Continue cards at the bottom. The top-left menu opens the full feature navigation. Completion is calculated as completed activities / all activities in enrolled, published courses. Names, courses, announcements and sessions use the same sample data. Continue goes to the next unfinished activity.

Global messages can be sent, survive a refresh, and History shows the full record. The AI uses the existing sample replies, and Join Now connects to the existing session demo. The Global panel on the right can be closed and reopened. On mobile the layout stacks vertically. The original design's weekly curve is still a decorative image, not an activity chart.

Added `globalMessages` data and the `globalMessage` command; details are in `API-CONTRACT.md`. The backend can keep the current `SkillAPI.request()` boundary. The screen's main code is `js/learner-dashboard.js`, the original layer template is `js/learner-template.js`, and the styles are in `learner-dashboard.css`. No build step is needed.

## Main demo flow

The course detail / Enroll page was updated from Figma `262:6504`. Clicking a course card on the home page or in the catalogue opens the matching `#/course/{id}`. If not enrolled you can Enrol; once enrolled, Continue goes to the next unfinished activity. Chapter rows expand, locked activities explain why, and public articles can be previewed. The review section calculates the average score and star breakdown from the current course's sample data, can switch to all courses, and uses the existing review / reply actions. A signed-out visitor who logs in from the Enrol entry returns to the selected course.

No backend commands were added this time. The main code is `js/enroll-page.js`, the original asset template is `js/enroll-art.js`, and the styles are in `enroll-page.css`. Images are in `assets/enroll/`; other role pages and the shared sample data still use the original API adapter.

1. The Instructor creates a course, adds articles, practical tasks, quizzes or course files, and submits it for review.
2. The Program Leader views the course on the approval board and approves, requests changes or rejects it. Only approved courses appear in the learner catalogue.
3. The Learner enrols and completes activities in order; practical tasks check answers and quizzes are scored. After completing all activities the learner can view / print a certificate.
4. The Instructor sees student completion, incorrectly answered topics and attempt history; the Program Leader's completion figures and reports update too.
5. The Admin views user details, changes roles / active status, approves Instructors, views audits and moderates discussions.

## Navigation and details completed

- The desktop left navigation can collapse; the mobile menu and overlay close with Escape.
- The visitor home page, courses, About, Instructors, FAQ, Contact, login and registration all have destination pages.
- Admin has user data / role permissions / learning records / audit tabs, plus role descriptions and an active users page.
- Program Leader has an approval board and table, search, content review and comments; the Program overview is calculated from real sample data.
- Chat has contact search, unread counts, sending and mark as read; you can switch demo role to reply.
- Account preferences, notification options, address, profile and appearance are saved; Learning access shows $0 for free courses.
- The code handles loading, empty states, validation, failures, retries and permission limits.

## Connecting the C# backend

### Instructor Dashboard update

John's `#/dashboard` was updated from Figma `283:838`: white left navigation, a green enrolment chart, light completion / average score cards, courses grouped by Program, and an assistant on the right. Statistics count only the courses assigned to that Instructor. Student count is unique people; completion is the average completion of the enrolment records; score is the average of all quiz attempts. With no attempts it shows "—". The green bar chart counts enrolment dates over the last 30 days. The two original curves and the 182 on the right are kept as decorative assets; they are not historical trends or real figures.

The course name or bottom-right corner opens the editor. Student avatars open that course's roster, and search stays within the course; View then shows individual completion, incorrect topics and completion types. New Course inside a Program preselects that Program in the creation form, and Enrol learner in a course-specific roster preselects the same course. Data still goes through the original mock API; no write commands were added.

The main code is `js/instructor-dashboard.js` and `instructor-dashboard.css`; assets are in `assets/instructor/`, and the original layer effects are in `js/instructor-art.js`. The assistant on the right uses sample replies and supports course hints, draft saving and retry after errors; the draft key is `skillup.instructor.ai.{userId}`. On mobile the metrics, courses and assistant are rearranged, and an Instructor with no assigned courses has a dedicated empty state. `tests/instructor-dashboard.cjs` checks these actions, the numbers updating after a learner completes, and mobile.

### Leaderboard update

`#/leaderboard` was changed to the red-and-black design from Figma `314:915`, keeping the white first-place card, blue avatars, personal rank and the weekly leaderboard. The black left column switches between overall and this week's ranking; on mobile the right-hand data moves below the ranking list. Names, dates and scores come from sample data rather than the repeated 134 points in the image. The overall score is still 100 points per completed course plus the best quiz score per course; the weekly board counts only the best quiz score per course since Monday. Retries do not add points, ties share a rank, and zero scores show as not yet ranked.

The code is `js/leaderboard.js` and `leaderboard.css`; no backend write commands were added. The selected board is saved in `skillup.leaderboard.period.{userId}`. Because this is a local snapshot it is labelled LOCAL, and Refresh rereads the data. Real rankings, the period time zone and scores should come from the C# service; weekly boundaries and calculation details are in `API-CONTRACT.md`.

`tests/leaderboard-data.cjs` has 20 scoring assertions; `tests/leaderboard-page.cjs` verifies switching, saving, updating after course completion, retries not adding points twice, ties and mobile. The five-person preview was created in the test browser through the register and quiz commands and is not added to your current account data.

### Chats update

`#/messages/{userId}` was updated from Frame 198 (`310:561`), keeping the original green glass bubbles, background, search and send icons. The left side is a searchable, scrollable contact list; on the right, received messages align left and your own align right. On mobile the contact list shows first, tapping opens the conversation, and the back arrow returns to the list.

The code is `js/chat-page.js` and `chat-page.css`; `js/chat-art.js` stores the original glow layers and can be rebuilt with `scripts/build-chat-art.cjs`. All assets are in `assets/chat/`. The 182 in the background is only a reference illustration, not student progress. Names, avatars, content, times and unread counts use the same sample data.

Sending and read status use the `message` and `readMessages` commands. Read status updates only when the recipient presses Mark as read; messages are plain text of up to 4000 characters. Enter sends and Shift + Enter adds a new line. Drafts are stored per sender / recipient in sessionStorage, kept when switching chats or refreshing, and cleared after a successful send; on failure they are kept and an error is shown. `tests/chat-page.cjs` verifies two-account replies, unread / read, search, drafts, error handling, safe text display, persistence and mobile use. This is still a local demo; cross-device / real-time chat needs a backend and SignalR.

### My courses / Program switching

The learner's `#/learning` was updated from Frame 197 (`296:448`): the cyan cards on the left are Programs and scroll independently; selecting one shows that Program's published courses on the right. The right-hand cards keep the purple assets, large numbers, green progress rings and arrows. Progress is calculated from the current learner's completed activities; courses not enrolled in show Not enrolled and are never enrolled automatically. The image opens the course details, and the arrow continues to the next unfinished activity. Mobile still keeps two columns.

The code is in `js/my-courses.js` and `my-courses.css`. Program order follows the workspace; courses follow `courseIds` first, then other published courses in the same Program. Empty Programs show a hint, the selection is remembered in `skillup.learning.program.{userId}`, and keyboard Up / Down / Home / End is supported. The top-left menu opens other system pages.

`js/my-courses-data.js` uses a one-time migration to add two sample Programs, Computer Systems and AI & Data, with four short courses, to make switching and enrolment easy to test. It does not overwrite existing data or enrol learners. Once the backend is connected, the workspace uses the service response directly and this sample data helper can be removed. The new page added no backend commands; see `API-CONTRACT.md`. `tests/my-courses.cjs` verifies independent scrolling, switching, remembered selection, progress updates, enrolment completion, keyboard, empty states and mobile.

### Course answer page update

`#/lesson/{courseId}/{lessonId}` now uses the AI sphere, MARATHON, dark task cards and original Submit / Hint glass buttons from Figma `250:6405`. The screen is `js/answer-page.js` and `answer-page.css`, and the 11 original assets are in `assets/answer/`. Articles, text practical tasks and multiple-choice questions all use the existing `complete` / `quizSubmit` commands; after completion, the course, student data and analytics update from the same records.

Answer drafts and practice code are kept in the current tab's sessionStorage under `skillup.answer.draft.{userId}.{courseId}.{lessonId}`. The quiz start time is stored separately as `skillup.quiz.start.{userId}.{courseId}.{lessonId}`, so a refresh does not reset the countdown. When time runs out, Restart quiz is required, and a retry clears that activity's answer draft. These are frontend demo states; real timing and attempt counts must be verified by the backend.

The AI on the left uses the existing sample replies. The Virtual Lab is a local JavaScript scratchpad that runs in a terminable Worker; it is not a remote computer or a C# compiler. `tests/answer-page.cjs` verifies wrong / correct practical answers, Hint, AI, drafts, program output, article completion, quiz timing / expiry / retry / scoring, and tablet and mobile layouts.

All data changes go through `SkillAPI.request(action, payload)`, centralised in `js/api.js`. Pages never access a database directly.

1. Implement the workspace response and commands in `API-CONTRACT.md`.
2. Change `config.mode` in `js/api.js` to `http` and set `baseUrl`.
3. Have the C# service provide sign-in sessions, the data each role can read, validation, scoring, approvals and file storage.
4. Replace the local AI templates, the password reset link and the chat / class simulations.

Data is currently stored in the same browser's localStorage. Different browsers and different origins / ports do not share data. Old sample data already in a browser is kept; use Admin → Profile → Reset demo data to restore it.

Notification preferences are saved in the demo; real email / push needs the backend. Sign-in, authorization and expected answers are frontend simulations and must not be used for real accounts. The class feature is join / leave plus a local camera preview; multi-party video is not provided.

## Design mapping

The Figma structure of all six pages was read, and the specified frames were fetched with design content and screenshots. The new frontend is a responsive functional implementation that fills in navigation and features missing from the original; it does not claim pixel-perfect fidelity to the whole file. See `DESIGN-SOURCES.md` for the actual mapping.

## Verification

- `node tests/domain.cjs`: 41 business assertions.
- `node tests/smoke.cjs`: 51 role routes, public pages and mobile overflow checks.
- `node tests/workflows.cjs`: the full cross-role flow from course creation to certificate.
- `node tests/role-completion.cjs`: 60 route visits for the new features, covering navigation, saved preferences, chat, permissions, approvals, files and mobile actions.

The Playwright / Edge paths in the browser tests were for the original machine; after handoff, change them to the team's own install paths. The app itself does not depend on Playwright.
