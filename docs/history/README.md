# Skill UP — complete frontend workspace

Pure **HTML + CSS + JavaScript**. No React, Tailwind, npm install or backend is required to explore the demo.

17 October UI polish: the original public-page composition is retained with centred copy, local DM Sans, sharp computer-topic SVGs, clearer testimonials/FAQ/footer, and a shared left-edge navigation for all roles. See [UI-REFINEMENT.md](UI-REFINEMENT.md) for files, verification, and Live Server instructions.

## Open

Program Leader Program Management (`#/programs`) now follows Figma `250:4767`: native monochrome document cards and the New Program illustration. Open a card to manage its assigned courses and ordered learning path. Add/remove/reorder locally, restore or discard tab drafts, and Save path to persist. Create/edit programs, create a course within a program, or move an existing course between your managed programs while retaining materials, publication state and student progress. Assigned-course membership and learning-path membership are distinct. See `tests/program-management-data.cjs` and `tests/program-management.cjs`.

Program Leader Requests (`#/approvals`) now follows Figma `250:3682`: grey navigation rail, date strip, three request lanes and purple base glow. Search/filter by program, application type or submission date; switch Board/Table. Open `#/approvals/{requestId}` to inspect the course, activities, assessment, files and submission timeline, then approve, reject or request changes with required instructor feedback. Creation approval publishes the course; deletion approval removes only an unenrolled course and retains request history. Failed saves retain feedback drafts. Dashboard pending cards link to this page. All operations remain local mock data, ready for the C# adapter described in `API-CONTRACT.md`. See `tests/leader-applications-data.cjs` and `tests/leader-applications.cjs`.

Program Leader dashboard (`#/dashboard`, signed in as sarah) now follows Figma Frame 172 (`250:5041`): grey navigation rail, four metric cards, segmented activity/completion charts and Pending Approval / Messages / AI Companion / Announcements panels. Counts are scoped to managed programs; chart periods support 24/14/7 days and Today. Pending creation and deletion requests open the new application detail; legacy course-only records retain the original review entry. Search contacts, open conversations, ask the local sample AI and publish announcements to a managed published course. AI/announcement drafts survive refresh and failed saves. Current learning records drive the charts; no source placeholder totals or historical bars are fabricated. See `tests/leader-dashboard-data.cjs` and `tests/leader-dashboard.cjs`.

Admin account creation and editing now share the full-page Figma `250:1980` form. Open a user card or Add User from `#/users`: edit name, email, username, profile notes, account status and role; save with the top-right button. New accounts receive a generated ID and creation timestamp and require a temporary password. Existing accounts support confirmation-based deactivate/reactivate/approve/delete actions, with self-admin and assigned-staff protections. Activity Logs includes actions by the user and administrative changes to that account; Learning activity remains accessible from the sidebar. Tab drafts survive reloads without storing the temporary password. See `tests/user-editor.cjs` and `tests/user-editor-data.cjs`.

Admin User Management (`#/users`, signed in as admin) follows Figma `250:1288`: original blue glow materials, a six-column capsule directory, Search, Add User and Load More. Search name/email/username/ID, filter by role/status, and open any card for the existing account information and access editor. Each card's actions menu supports editing, approval and guarded deletion. The directory shows real mock records, 60 at a time; no repeated placeholder accounts are added to fill the design. Phone layouts use two columns. Changes persist in the browser through the existing mock API. Verified by `tests/admin-users.cjs`.

The public homepage now follows the full Unregister User Figma layout, with Skill UP learning copy, original local illustrations and working navigation/FAQ. See `DESIGN-SOURCES.md` for the source node and copy mapping.

The Learner main page also follows Figma `250:466`: blue glass cards, cyan foil course cards, Global community chat and resume tiles. Sign in with **alex / Demo1234!** to see it. Progress, learning links, AI sample replies, session joining and persistent Global messages are connected to the shared mock API.

Click any course to open the Figma-based Enroll page (`262:6504`): original purple banner and chapter surfaces, enrol/continue action, expandable activities, course information and white review threads. Progress and rating summaries are calculated from saved demo records.

Course activities now use the answer-page design (`250:6405`): original AI orb, lime MARATHON banner, dark task cards and silver Submit / blue Hint buttons. Articles, answer labs and timed quizzes share this layout. Answers and scratchpad code survive reloads within the current browser tab; the existing mock API grades submissions and updates course progress.

My courses (`#/learning`) now follows Frame 197 (`296:448`). Scroll the cyan program cards independently, then select a program to show its published courses on the right. Purple course artwork, large chapter numbers and live completion rings retain the source treatment. Course artwork opens its overview; the arrow resumes an enrolled course's next activity, or opens the overview for enrolment. Available courses are clearly marked Not enrolled. Two additional fictional programs make switching testable without replacing existing records.

Chats (`#/messages`) now uses Frame 198 (`310:561`): searchable contacts, the original green glass bubble layers and background illustration, a rounded composer, and separate incoming/outgoing messages. Per-contact drafts survive navigation/reload in the current tab. Enter sends; Shift + Enter adds a line. Read status changes when the recipient chooses Mark as read. Phone layouts show contacts first and open a dedicated conversation with a back link.

Leaderboard (`#/leaderboard`) now follows `314:915`: red canvas, white leading card, muted red ranking rows, blue avatars, personal score and weekly standings. The left black rail switches all-time/current-week rankings. Scores come from saved records, ties share a rank, and zero points remain unranked. The timestamp is labelled LOCAL because this is a local snapshot; Refresh reloads the records.

Instructor dashboard (`#/dashboard`, signed in as john) now follows `283:838`: green enrolment chart, light completion/quiz cards, courses grouped by program, a white navigation rail and the original right companion panel. Counts and course progress use assigned-course records. Course cards open editing and filtered learner rosters; New Course preselects its program. The companion uses saved sample replies and a per-instructor draft.

Instructor question editing (`#/quiz-editor/c1`) now follows `250:1557`: original AI orb, MARATHON and silver Submit / blue Hint keycaps with editable task cards. Add/remove questions and 2–6 options, choose the correct answer, set a topic and time limit, then save. Each silver Submit saves the entire quiz; Hint explains authoring. Unsaved questions/settings and AI prompts survive reload within this tab. Saving returns the course to Draft; pending review locks the editor until withdrawal.

Course information create/edit (`#/course-edit/new` or `#/course-edit/c1`) now follows `250:3989`: dark course summary, Information / Learners / Learning Resources tabs, original HTML5 icon and cyan/blue field artwork. Description, outcomes, program, difficulty, duration, start date and deadline are editable. Status and visibility reflect the approval workflow. Saved courses retain activity/quiz editing, resource management and review history. Per-account/course tab drafts survive reload; a pending-review course is locked until withdrawal. `node tests/course-editor.cjs` verifies these flows against the local server.

Instructor student performance (`#/learners`) now follows Frame 190 (`250:1979`): charcoal canvas, white navigation rail, three-column black learner cards and original segmented progress assets. Each card represents one enrolment and opens its existing completion types, weak topics and attempt history. Search, course/progress filters, sorting, enrolment and removal are connected to shared mock records. Missing quiz attempts show a dash instead of a zero score. Course links open a filtered roster (`#/learners/course/c1`).

Instructor My requests (`#/requests`) now follows Frame 201 (`294:18`): grey rail and request rows, search/type/status filters, pending overview and the source tracking timeline assets. Select a submitted Create/update or Delete request to see actual submission, review and decision events. The initial history is derived from existing submitted approval records; unsent drafts are not presented as submitted. New request and Withdraw are functional. Instructor deletion now requests Program Leader approval, preserving the course until approved; enrolled courses must be hidden instead. Program Leaders see a deletion queue on Course approvals. History survives approved deletion. Pending deletion locks further course edits until withdrawal. Use `node tests/course-requests-domain.cjs` and `node tests/course-requests.cjs` to verify the flow.

Admin overview (`#/dashboard`, signed in as admin) now follows `250:845`: the original wide blue banner, material layers and five summary spheres, activity/completion cards, a cyan attention panel and a dark course table. Counts come from the workspace snapshot. Active learners are unique active learner accounts with recorded learning activity in the chosen 7/30/90-day window; the chart uses saved activity records. Course completions count completed enrolments, while the bars show average progress per course. Search/status filters, course inspection, account/report links and CSV export work. Pending verification updates after approval. See `tests/admin-dashboard-data.cjs` and `tests/admin-dashboard.cjs` for verification.

Run `node tests/instructor-editor-performance.cjs` with the local server on port 4185 to verify the editor and performance screens in an isolated browser.

See [HANDOFF.md](HANDOFF.md) for the full handoff notes and test accounts. The new role navigation, user details, approval board, chat and account preferences all use the same demo data.

Double-click `index.html`. Or, from the parent workspace, run `node server.cjs` and visit `http://127.0.0.1:4173/skillup-frontend/`.

For the standalone package, run `node server.cjs` **inside this folder** and visit `http://127.0.0.1:4185`.

Choose **Log in → a demo role**. Use **Switch demo role** to move between workspaces while keeping the same shared data. Changes are saved in this browser's localStorage. Data is separate between file:// and http:// origins, and between browsers.

| Role | Username | Password |
|---|---|---|
| Learner | alex | Demo1234! |
| Instructor | john | Demo1234! |
| Program Leader | sarah | Demo1234! |
| Admin | admin | Demo1234! |

All account details, learner records, scores and dates are fictional sample data. Use only demo passwords in this prototype.

## What works

- Public: course catalogue and search, course detail/outcomes/curriculum/reviews, read-only activity preview, registration, login, password recovery, FAQ, enquiry create/read/edit/delete with an access code.
- Learner: dashboard, Figma-based My Courses, enrolment, sequential activity unlocking, article completion, input-answer labs, local JavaScript scratchpad, timed multiple-choice quizzes, scores and explanations, progress, leaderboard, printable completion certificates, course reviews and replies.
- Instructor: course create/edit/hide/delete, learning activity CRUD, quiz editor and sample AI quiz drafts, upload/download/delete course files, submit/withdraw review, feedback history, enrol/remove learners, individual completion and weak-topic analysis, course analytics.
- Program Leader: program CRUD, add/remove/reorder learning-path courses, assign/remove instructors, inspect course content and resources, approve/request changes/reject with feedback and history, learner progress and analytics.
- Admin: user CRUD, roles and status, instructor approval, protected self-administration, platform statistics and CSV reports, audit search, forum moderation with reasons, visitor enquiries and feedback.
- Shared: scoped announcements, threaded discussions, local account-to-account messages with read status, session scheduling and join/leave, optional local camera preview, sample AI chat with persisted history, profile/avatar/password, light/dark theme, error/empty/loading states, responsive layout and keyboard-accessible forms/dialogs.

## Try the connected workflow

1. Instructor → My courses → Create course. Save a draft; add an article or lab. For quizzes, add questions and publish the quiz.
2. Submit for approval. Switch to Program Leader → Course approvals → Inspect.
3. Add feedback and approve. The course becomes Published.
4. Switch to Learner → Explore courses → enrol. Complete activities in order. A quiz score of at least 70% completes its activity.
5. Instructor → Learners shows the changed progress and incorrect topics; Program Leader analytics update from the same records.
6. Admin → Activity & audit shows recorded changes.

Editing published content returns it to Draft; submit it again to publish. Courses with enrolled learners cannot be deleted; use Hide. Programs with courses cannot be deleted. Reassign staff before deleting their accounts.

## Calculations

- Completion: completed **current** activities / total current activities, rounded to a whole percentage. Duplicate completion does not add learning time again.
- Overall progress: unweighted average of enrolment completion percentages, including 0% enrolments.
- Course participation: enrolments with at least one completed activity / enrolments.
- Quiz score: correct answers / questions. Average quiz score includes all submitted quiz attempts.
- Leaderboard: 100 points for each fully completed course plus the best quiz score per course. Failed/repeated attempts do not inflate quiz points.
- Weekly leaderboard: the best quiz score per course submitted since Monday, summed once per course. It uses the current browser's local week in this prototype. Course completion bonuses are included in all-time scores only.
- Learning time: estimated minutes of completed activities; it is not real-time attendance.
- Weak topics: count of incorrect quiz answers and unsuccessful lab attempts by topic.

## Delivery files

- `index.html`: standalone entry point.
- `styles.css`: common layouts, controls, responsive rules and themes.
- `design-alignment.css`: Figma-based course workspace, glass progress capsule and role styling.
- `role-completion.css`: role navigation, public landing page, user-detail tabs, chat, leaderboard and approval board.
- `js/answer-page.js` and `answer-page.css`: Figma activity layout, answer drafts, AI panel and virtual-lab scratchpad.
- `js/my-courses.js` and `my-courses.css`: scrollable program selector, matching course rows, completion and continue links.
- `js/my-courses-data.js`: one-time additive demo program/course fixtures; existing records and enrolments are preserved.
- `js/chat-page.js`, `js/chat-art.js` and `chat-page.css`: contacts, original bubble artwork, message composer and responsive conversations.
- `js/leaderboard.js` and `leaderboard.css`: all-time/weekly scoring, shared ranks, period selection and Figma ranking cards.
- `js/instructor-dashboard.js`, `js/instructor-art.js` and `instructor-dashboard.css`: Instructor metrics, grouped course cards, original artwork and teaching companion.
- `js/seed.js`: sample dataset and shared calculation helpers.
- `js/api.js`: mock service, validation, role checks, persistence and HTTP adapter.
- `js/app.js`: routing, accessible HTML rendering, forms and interactions.
- `js/pages.js`: additional role views using the same service and shared rendering helpers.
- `assets/`: locally downloaded Figma artwork; no temporary asset URLs at runtime.
- `API-CONTRACT.md`: C# integration contract and command payloads.
- `DESIGN-SOURCES.md`: Figma frame mapping and adaptation notes.
- `tests/`: domain and browser workflow checks.
- `previews/`: desktop/mobile screenshots.

## Backend handoff

The UI calls `SkillAPI.request(action, payload)` exclusively for application data changes. To connect a compatible ASP.NET API, change `config.mode` in `js/api.js` to `http` and set `baseUrl`. Implement the contract in `API-CONTRACT.md`; changing the flag alone does not create a backend.

**Simulation boundaries:** authentication and permissions are only frontend demonstrations; sample passwords and correct answers exist in browser data. AI responses are templates; email is a displayed local reset link; messages and sessions share local data only; camera is a local preview, not conferencing. The scratchpad executes JavaScript in a terminable worker, not C#. Real accounts, secure grading, email, AI, file storage and realtime transport belong to the C# service.

## Verification

Run `node tests/domain.cjs` for dependency-free business-rule tests. Browser tests use Playwright; replace the local runtime path and Edge executable in `tests/*.cjs` with your team's installed Playwright/browser path, then run `node tests/smoke.cjs`, `node tests/workflows.cjs` and `node tests/role-completion.cjs`.

`node tests/answer-page.cjs` checks the activity artwork, lab validation, hints, AI replies, draft restoration, JavaScript scratchpad, quiz timing/expiry/retry/grading, progress and responsive layout.

`node tests/my-courses.cjs` checks independent scrolling, program switching, remembered selection, keyboard navigation, course progress, enrolment/completion, empty programs, asset loading, mobile layouts and fixture migration.

`node tests/chat-page.cjs` checks bubble assets, contact search, unread/read status, per-contact drafts, failed sends, escaped text, keyboard sending, two-account replies, persistence, scroll-to-latest and mobile navigation.

`node tests/leaderboard-data.cjs` verifies scoring, deduplication, ties, active accounts and week boundaries. `node tests/leaderboard-page.cjs` checks period selection, completion updates, retries, personal rank and responsive layouts.

`node tests/instructor-dashboard.cjs` checks assigned-course metrics, original assets, grouped course/learner links, program preselection, AI drafts and errors, completion updates, responsive layouts and an unassigned Instructor's empty state.

To restore sample data: Admin → Profile & settings → Reset demo data. This only resets the demo workspace in the current browser/origin.
