# Current handoff notes — 20 October 2026

These notes override older UI descriptions below:
- Standalone announcements/notifications pages and all notification bells were removed. Legacy hashes redirect to dashboard (signed in) or home (guest).
- Notification settings UI was removed; existing preference fields may remain for data compatibility.
- Learning paths page was removed. Program course ordering remains a staff feature.
- Course announcements remain course data / inline dashboard content; removal of the standalone page does not delete the records.
- Learner overview retains the current section positions, purple background, black metrics and full-width Active Courses list.
- Public enquiry form is connected to enquirySave and a private portal where a browser (or signed-in account) lists, edits and deletes the enquiries it sent.
- Runtime transport configuration lives in js/runtime-config.js; js/api.js is the transport boundary.
- Demo calls, local scratchpad execution and sample AI responses are not server integrations.
- Browser seed/demo files must be removed/isolated for production; do not distribute demo passwords or quiz answer keys as public production assets.

The original domain contract follows. Path/notification UI references in older sections are historical.

---

# C# / ASP.NET integration contract

## Transport boundary

Program Management uses `programSave`, `programDelete`, `pathSave` and the new `programCourseMove` command. A course's `programId` is its owning program; a program's ordered `courseIds` is a subset used in its learning path. Removing a path entry leaves course ownership, content, enrolments and progress intact. `pathSave` requires an array with unique, existing members of that program; an empty path is valid. Learner paths show only published courses in saved order. New programs return `{id}` and start with an empty path; program updates preserve membership. Reject unknown update IDs rather than silently creating records.

`programCourseMove` receives `{courseId,programId,includeInPath:boolean}`. Within one authorised transaction, require a Program Leader who owns both source and target, reject the same source/target and pending creation/deletion reviews, remove the course ID from the source path, update course.programId, then optionally append it once to the target path. Preserve lessons, quiz, instructorId, status, files, enrolments, attempts and related learning records. Deletion of a nonempty program is forbidden. The view prevents transfers while either path has an unsaved client draft; that draft is not server data or authorisation.

The new Requests board (`#/approvals`) reads courseRequests, rather than inferring application outcomes from course.status. Return scoped Create/Delete requests with IDs, courseTitle, historical programId, requesterId, status, createdAt, reason and events, including approved deletions whose course no longer exists. New = Pending review; Rejected and Approved have their own lanes; Changes requested and Withdrawn remain distinguishable. Search, date/program/type filters and Board/Table are local presentation state. Displayed submission dates use Asia/Kuala_Lumpur. Counts reflect filtered records, not placeholder card totals.

Creation detail submits existing `review` with `{courseId,requestId,decision,comment}`; requestId is optional only for older clients. Validate a supplied requestId against the exact currently pending Create request in the same transaction as the course status update. A completed/replaced application must return 409 even if the course has another pending submission. Feedback must be nonblank. Approval publishes; rejection/change requests retain their corresponding course/request status. Deletion detail uses existing `courseRequestReview`; recheck enrolments before removal and keep the request timeline. `courseRequestSeen` records a review-start event once. Feedback drafts are tab-only and cleared only on a successful decision. The Instructor history consumes the same request/events. Existing `/review/{courseId}` and `/request-review/{requestId}` routes remain compatible.

Program Leader overview uses the existing role-scoped workspace and commands: `ai`, `announcementSave`, the existing message/read workflow and course-review/deletion-review flows. No new command is introduced. The frontend scopes by managed program IDs and computes program/course totals, average enrolment progress, unique active learners from recorded learning activity, per-day attempt totals and pending requests. Activity periods are 1/7/14/24 calendar days; current/prior periods are compared by count, while the completion panel is the current snapshot. Server-provided aggregates may replace these calculations. Production reports need complete historical events for historical progress or sign-in trends; current enrolment.lastActive snapshots cannot reconstruct full history. Announcements require a managed courseId and validated title/body, with authority enforced again by the C# service.

The unified Admin create/edit form (`#/users/new`, `#/users/{id}`) posts existing `userSave` fields `{id?,name,email,username,bio,role,status,password?}`. `password` is required only for creation; IDs and creation dates are assigned by the service. Return `{data:{id},state:<updated workspace>}` for both create and update. The mock now normalises whitespace before uniqueness validation, assigns `createdAt` on creation, rejects an unknown non-empty id and records `{actorId,targetUserId,action:"User account saved",detail:email,createdAt}`. Existing timestamps are preserved on update. `users[].createdAt` / `lastActiveAt` and `audit[].targetUserId` are optional for older snapshots; missing dates render as Not recorded. Deactivate/reactivate/approve use the same save command with existing saved values and the new status. Delete remains `userDelete`. All commands require Admin authorisation, including the prohibition on self-deactivation/demotion/deletion and deletion of assigned staff. Account-list search/paging is unchanged.

Admin directory (`#/users`) reuses the authorised `users` snapshot and existing `userSave` / `userDelete` commands. Approval is `userSave` with the existing account values and `status: "Active"`. No new command is introduced. Search/filter and 60-record Load More are currently client-side, with visible counts derived from the filtered records. For production-scale account lists, replace the snapshot slice with an Admin-only paginated search endpoint returning records and total count; retain server enforcement of account uniqueness, self-admin protection, and reassignment before deleting assigned staff. Frontend role checks are only a UI boundary.

UI entry: `SkillAPI.request(action, payload)` in `js/api.js`.

- `GET /api/workspace` → a **role-scoped** workspace object (shape below).
- `POST /api/commands/{action}` with JSON → `{ "data": <result or null>, "state": <updated workspace> }`.
- Errors: an HTTP 4xx/5xx response with `{ "detail": "User-readable explanation" }` (ASP.NET ProblemDetails-compatible). The adapter also accepts `message`.
- The current adapter uses `credentials: 'include'`. Use the team's cookie/session authentication, HTTPS, CSRF protection and server-side authorisation. Add your anti-forgery header in this one adapter; do not expose credentials in view code.
- IDs are opaque strings. Seed IDs such as `c1` are **not** GUIDs; production IDs may be GUID strings.
- Date-only values: `YYYY-MM-DD`. Event timestamps: ISO8601. Session scheduling currently sends local `YYYY-MM-DDTHH:mm`; the backend should accept a configured timezone (demo assumes Asia/Kuala_Lumpur) and return an explicit offset.
- CSV export is client-side and uses the loaded authorised snapshot. For large datasets, replace it with a paginated server report/export endpoint.

This command transport is an intentionally simple frontend boundary. The C# team may map each action to their conventional REST controllers inside `SkillAPI.request`, without changing page code.

## Workspace shape

```json
{
  "version": 1,
  "currentUser": {"id":"u1","name":"Alex Morgan","email":"alex@skillup.demo","username":"alex","role":"Learner","status":"Active","theme":"dark"},
  "users": [], "programs": [], "courses": [], "enrollments": [],
  "attempts": [], "reviews": [], "posts": [], "announcements": [],
  "messages": [], "globalMessages": [], "sessions": [], "ai": [], "resources": [],
  "feedback": [], "enquiries": [], "approvals": [], "courseRequests": [], "audit": []
}
```

Collections are arrays even when empty. A non-authenticated request has `currentUser: null`. See `js/seed.js` for complete example DTOs.

| Entity | Fields used by views |
|---|---|
| User | id, name, email, username, role, status, bio, location, avatar (image URL/data URL), theme, preferences (below) |
| Program | id, name, description, leaderId, courseIds (ordered), published |
| Course | id, title, description, outcomes (newline-separated), programId, instructorId, difficulty, status, deadline, duration (weeks), color, icon, lessons, quiz |
| Lesson | id, title, type (`article/lab/quiz`), body, minutes, preview; expected answer is a staff-only field |
| Quiz | published, timeLimit (minutes), questions[{id,text,options,topic}]; correct answer index is staff-only |
| Enrollment | id, userId, courseId, completed (lesson ID array), minutes, joinedAt, lastActive |
| Attempt | id, userId, courseId, lessonId (lab), type, score, answers (quiz indices), wrongTopics, createdAt |
| Review | id, courseId, userId, rating, text, createdAt, replies |
| Post | id, courseId, userId, text, status, reason, createdAt, replies |
| Reply | id, userId, text, createdAt |
| Announcement | id, courseId, userId, title, body, createdAt |
| Message | id, from, to, text, createdAt, read |
| Session | id, courseId, hostId, title, startsAt, attendees (user IDs) |
| AI entry | id, userId, prompt, response, createdAt |
| Resource | id, courseId, name, size, dataUrl, createdAt |
| Approval | id, courseId, actorId, decision, comment, createdAt |
| Audit | id, actorId, action, detail, createdAt |

Production must filter DTOs on the server: never return passwords, recovery tokens, other learners' private data, lab expected answers or quiz answer keys to a learner/public visitor. The in-browser mock includes global demo records so role switching works; it is not a security model.

## Commands

Unless noted, `data` is null and state contains the fresh snapshot.

| Action | Payload | Result / policy |
|---|---|---|
| login | email (or username), password | authenticated state; active account required |
| logout | {} | currentUser null |
| register | name, username, email, role, password, confirm | `{pending:boolean}`; only Learner or Instructor; Instructor pending verification |
| forgot | email | mock `{token}`; replace with generic email acknowledgement in production |
| resetPassword | token, password, confirm | consume expiring one-use token |
| profile | name, username, email, location, bio, theme, avatar? | current user only |
| password | current, password, confirm | validate current password |
| enquirySave | id?, token?, name, email, subject, message | enquiry with id/access token (create or owner update) |
| enquiryDelete | id, token | authorised deletion |
| enrol | courseId | learner, published course, no duplicate |
| complete | courseId, lessonId, answer? | `{correct:boolean}`; wrong lab answer records attempt but not completion |
| quizSubmit | courseId, lessonId, answers: number[] | attempt `{id,score,answers,wrongTopics,createdAt}` plus `feedback` recommendation below |
| courseSave | id?, title, description, outcomes, programId, difficulty, deadline, duration | saved course; Draft status |
| courseAction | id, action (`submit/hide/withdraw/delete`) | submit requires activities and published questions for quiz activities |
| lessonSave | courseId, id?, title, type, body, answer, minutes, preview | content edits return course to Draft |
| lessonDelete | courseId, id | remove activity; recalculate progress |
| quizSave | courseId, questions, published, timeLimit | valid options + answer required; Draft course |
| resourceSave | courseId, name, size, dataUrl | mock upload up to 2MB; course returns to Draft |
| resourceDelete | id | content modification; Draft course |
| review | courseId, requestId?, decision, comment | Program Leader scope, pending only; match supplied requestId to the current pending Create request; Approved → Published |
| programSave | id?, name, description, published | leader-owned program; return `{id}`; unknown nonempty id is 404 |
| programCourseMove | courseId, programId, includeInPath: boolean | Program Leader owns source and target; preserve course content/status and enrolments; no pending reviews |
| programDelete | id | no courses assigned |
| pathSave | id (program), courseIds: string[] | unique ordered members of program |
| assign | courseId, instructorId (or empty to unassign) | active instructor |
| enrollmentSave | courseId, userId, remove? | authorised staff; active learner; no duplicate |
| announcementSave | id?, courseId, title, body | staff scope |
| announcementDelete | id | staff scope |
| post | courseId, text | enrolled learner or authorised staff |
| reply | id, kind (`post/review`), text | course access required |
| moderate | id, status, reason | instructor course scope / admin; audit reason |
| courseReview | courseId, rating, text | enrolled learner; updates their existing review |
| feedback | rating, text | authenticated user |
| message | to, text | authenticated sender; active recipient |
| readMessages | from | marks current recipient's messages read |
| sessionSave | courseId, title, startsAt | assigned instructor |
| sessionJoin | id, leave? | authorised course participant |
| ai | text | `{response}` and appended AI history |
| userSave | id?, name, username, email, role, status, bio, password? | admin; unique email/username; cannot revoke own admin access |
| userDelete | id | admin; no self-delete; reassign staff first |

`demo` and `resetDemo` are development-only actions. Remove the demo buttons and do not implement these endpoints in production.

## Server responsibilities / integration changes

1. **Authentication:** issue secure cookies; hash passwords; verify instructor applications. Replace demo recovery-link modal and social demo controls with your chosen real authentication flow. Implement rate limiting, CSRF, and an agreed session-expiry UX.
2. **Grading:** validate enrolment, current publication status and prerequisites on every submission. Make completion idempotent and update score/progress atomically. Return feedback after submission instead of sending answer keys in the workspace. `quizSubmit` should return `feedback: [{text, selectedText, correctText, correct}]`; the UI supports this field. For enforced timing, add a server-issued attempt ID/start endpoint and expiry to the adapter and quiz controller; the current timer is presentation-only.
3. **Scopes:** derive actor ID/role from the authenticated session. Never trust role, user ID, correct answer or computed progress supplied by the browser. Limit instructors to assigned courses and leaders to their programs. Validate that replies target a visible permitted thread.
4. **Concurrency:** use transactions/row versions. A second review of a completed submission should return 409. Return 401/403/404 consistently. Add server pagination for users, messages, audit and course lists when needed.
5. **Resources:** replace data URLs with multipart uploads and authorised download URLs; validate file type/content/size on the server. Change upload/download branches in the adapter/UI accordingly; localStorage is only demo storage.
6. **Realtime:** use SignalR for messages, unread notifications and presence. Voice/video requires a conferencing provider or WebRTC signalling; camera preview currently sends nothing. AI needs a server-held model credential; quiz drafts must still be reviewed by instructors.
7. **Code execution:** JavaScript scratchpad runs locally with a 2-second worker timeout. Do not execute learner C# submissions inside the API process; provide an isolated runner service. Wire results to the activity UI through the adapter.
8. **Large state:** the snapshot API is convenient for this prototype. Split it into resource endpoints and cache/paginate in the adapter for a production application.

## ASP.NET DTO convention example (handoff reference only)

```csharp
public sealed record CommandResponse<T>(T? Data, WorkspaceDto State);
public sealed record CompleteActivityRequest(string CourseId, string LessonId, string? Answer);
public sealed record QuizSubmissionRequest(string CourseId, string LessonId, int[] Answers);
public sealed record ReviewCourseRequest(string CourseId, string Decision, string Comment);
// Controllers resolve the current user from HttpContext.User, then call services.
// Return ProblemDetails with Status and Detail for validation/permission failures.
```

No C# server is included in this delivery, as requested.

## Instructor dashboard — 17 October

The redesigned Instructor dashboard adds no command. It consumes only courses where `instructorId` equals the signed-in Instructor, plus their enrolments and quiz attempts:

- Enrolled learners: distinct enrolment user IDs across assigned courses. Enrolment count is displayed separately because one learner may take multiple courses.
- Course completion: rounded unweighted mean of progress for those enrolments. The completed count is enrolments at 100%, not a unique-learner count.
- Average quiz score: rounded mean of all submitted quiz-attempt scores for assigned courses. No attempts displays no score (`—`).
- Daily enrolments: counts by `joinedAt` date for each of the last 30 local calendar days. Production should supply dates/buckets in the agreed service timezone.
- Course card progress: rounded mean of that course's enrolment progress; no enrolments is 0%. Activities, quiz questions, deadline, status and learner avatars use current records.

`#/course-edit/new/{programId}` is UI program preselection only; `courseSave` still validates the program. `#/learners/course/{courseId}` filters the authorised enrolment snapshot and keeps search within that course. Enrol-a-learner in that view preselects the same course; it still uses `enrollmentSave`. Learner details retain their existing route and commands. The C# service must enforce assignment/ownership for every read/write independently of these filters.

The companion reuses `ai` with `{ "text": "Question" }` and the shared history collection, scoped by user. Its UI-only draft key is `skillup.instructor.ai.{userId}` in sessionStorage. Failed requests keep the draft, successful submissions clear it. The context button only prefills text and does not send automatically. AI replies are still local templates; decorative source curves/182 artwork are not API metrics.

## Leaderboard / 02 Live — 17 October

The new view derives rankings from the workspace; it adds no write command. A production service should return an authoritative leaderboard read model rather than trusting client-calculated awards.

- Participants: active accounts with role Learner.
- All-time points: 100 times the number of distinct fully completed current courses, plus the best quiz score per existing course. Completion uses the existing current-activity progress rule. Repeated enrolment/attempt records do not duplicate points.
- Weekly points: sum of each existing course's best quiz score submitted in the current week, from Monday 00:00 inclusive to the next Monday exclusive; future-dated attempts are excluded. Lab attempts are excluded from quiz points. Completion bonuses are not weekly awards, since the current enrolment shape has no per-activity completion timestamp.
- Ranking: descending points, then display name and user ID for stable display; equal positive scores share a competition rank, e.g. 1, 1, 3. Zero points have no rank. A missing participant shows no rank and zero points.

The mock week uses the browser's local timezone. Agree on a service timezone (for example Asia/Kuala_Lumpur) and have the C# service supply window boundaries, update time and rank/points for both periods. Quiz scores must be validated/graded on the server. The UI-only selected period is `skillup.leaderboard.period.{userId}` in sessionStorage; it is revalidated to `all` or `week`. The header says LOCAL because the prototype updates through Refresh rather than realtime transport. Existing profile avatars are reused.

## Chats / Frame 198 — 17 October

The chat redesign reuses `message` and `readMessages`. `message` takes `{ "to": "userId", "text": "Message body" }`; trim and validate 1–4000 characters, require an active recipient other than the sender, and derive `from`, ID, timestamp and initial `read: false` on the server. The mock adapter now enforces the same length limit as the composer. Message content is displayed as escaped plain text.

`readMessages` takes `{ "from": "userId" }` and marks only messages from that sender to the authenticated user as read. The recipient explicitly chooses Mark as read. Sender receipt labels are derived from the shared `read` field, not fabricated online/typing status. Unread counts use incoming unread records. Contacts are other active accounts; the workspace must provide only users and conversations permitted by the real application's messaging policy.

Drafts use UI-only sessionStorage key `skillup.chat.draft.{senderId}.{recipientId}` and are never submitted as workspace fields. Successful send clears that draft; a failed request keeps it. The frontend lists messages chronologically and scrolls to the latest loaded message. The mock is single-browser local persistence; realtime updates, paging and delivery events remain C# / SignalR integration work. No attachment or voice-transfer endpoint was added.

## Program/course switcher — 17 October

`#/learning` uses the workspace's `programs`, `courses`, `enrollments` and current user. It lists published programs, with published courses matched by `course.programId`. Courses in `program.courseIds` appear in that order; remaining assigned published courses follow in workspace order. Empty published programs are allowed. The view includes available courses marked Not enrolled; selecting a program does not enrol a user. Existing `enrol` and activity commands remain unchanged.

Completion is the existing current-activity formula for each course and the current user's enrolment; an unenrolled course is 0%. The course image opens the overview. An enrolled course's arrow links to its first unfinished activity, while completed/unenrolled courses open the overview. The selected program ID is UI-only sessionStorage at `skillup.learning.program.{userId}`, validated against the currently published list at render time. A removed or unpublished selection falls back to a program containing an enrolled course, then the first published program. Selection requires no write endpoint.

`js/my-courses-data.js` adds demo programs/courses once using `migrations.programSwitcherV1` in local demo storage. The HTTP adapter continues to use the server's workspace response directly; these fixtures are not merged into backend responses. Remove the fixture helper when the C# service owns the dataset. An optional `course.artwork` string can supply a course-specific image; otherwise the three original local course illustrations cycle by display order.

## Activity answer view — 17 October

The new answer view reuses `complete`, `quizSubmit` and `ai`; it adds no server command. Quiz radio values are zero-based option indexes, assembled into the existing `answers` array for the whole assessment. Text labs send the existing `answer` field. Course completion and attempt records continue to come from the returned workspace, not DOM state.

UI-only sessionStorage keys are `skillup.answer.draft.{userId}.{courseId}.{lessonId}` for answer/code drafts and `skillup.quiz.start.{userId}.{courseId}.{lessonId}` for the demo countdown. A reload restores the draft and remaining time within that tab. Explicit retry starts a new countdown and clears the activity draft; passing a quiz or a correct lab submission clears its answer draft. These keys are not authoritative attempt records. The C# service must issue/validate attempt IDs, start/deadline timestamps, time limits, prerequisites, ownership and grades independently of client storage. The current JavaScript scratchpad and template AI replies remain frontend simulations.

## Course enrolment view — 17 October

The redesigned `#/course/{id}` page uses existing `enrol`, `complete`, `quizSubmit`, `courseReview`, `reply` and resource commands; no new API endpoint is required. Curriculum-row completion is 100% for a completed activity and 0% otherwise. Overall course completion uses the existing activity-count formula. Review averages and star distributions use loaded records for the selected course (or all courses when selected); empty sets display no rating.

The prototype saves a pending course ID in sessionStorage when a visitor follows the enrol sign-in/register link. After a successful Learner login it returns to that course if it remains Published. This is navigation state, not enrolment: the user still explicitly confirms Enrol. The backend must continue to validate publication, enrolment and activity prerequisites independently of UI locks.

## Registered-user main page — 17 October

`globalMessages` is an array of `{id, userId, text, createdAt}`. The dashboard shows the last four community messages, a separate available session card, and a History dialog containing all messages. It must not be combined with private direct messages.

`POST /api/commands/globalMessage` takes `{"text":"My learning question"}`. Require an active authenticated account, trim and validate 1–500 characters, derive `userId` from the session and assign an ID/timestamp on the server. Return the updated workspace using the existing command envelope. The prototype persists messages in localStorage and renders message bodies as escaped text. Add SignalR/pagination to this boundary when the backend is implemented. Existing locally saved workspaces receive the new sample collection without resetting user changes.

The main page reuses `ai`, `sessionJoin`, lesson completion and existing course routes. Overall activity completion = completed activities in enrolled Published courses / all activities in those courses, rounded to a percentage; it is zero with no enrolled activities. Displayed learning level = `floor(completedActivities / 5) + 1` (demo rule). Continue links open the next incomplete activity; completed courses open their overview. Empty course positions can suggest a published course instead. The `Your week` curves are preserved Figma illustration assets, not a measured activity chart; progress numbers and course/announcement/session content are data-driven.

## Added account preferences and view endpoints — 17 October

`POST /api/commands/preferences` requires a signed-in account and updates only that account.

Notification payload:

```json
{"section":"notifications","courseUpdates":true,"deadlines":false,"messages":true,"reviewUpdates":true}
```

Address payload:

```json
{"section":"address","address":"Demo street","city":"Demo City","country":"Malaysia","postcode":"00000"}
```

Return the updated `currentUser.preferences` with these fields. Missing notification flags default to enabled in the form. The current prototype saves preferences; email/push dispatch belongs to the backend.

`POST /api/commands/quizDelete` takes `{"courseId":"c1"}`. It requires the same staff ownership as quizSave, refuses a pending-review course, empties the questions and returns the course to Draft.

The Figma question editor continues to call `quizSave` with `{courseId, timeLimit, published, questions:[{id?,text,options,answer,topic}]}`. `answer` is a zero-based option index. Existing IDs survive edits, while new questions receive IDs on save. The authoring UI supports 2–6 non-empty options, one selected correct answer and a 1–180 minute time limit; server validation must enforce equivalent constraints. `published` alone must not bypass course approval. Tab drafts are client-only and are never treated as saved assessments. Each Submit key saves the entire quiz.

`courseSave` now also accepts optional `startDate` (`YYYY-MM-DD` or empty). Persist it on the Course DTO and return it in the workspace snapshot. An omitted startDate preserves the existing value for older callers; an empty string clears it. Reject invalid dates and a startDate after a supplied deadline. The form continues to submit `{id,title,description,outcomes,programId,difficulty,deadline,duration,startDate}`; it does not submit status, visibility or instructorId. Assignment uses the existing `assign` command. Editing still returns the course to Draft and pending review must be withdrawn before edits. UI-only tabs and tab drafts require no endpoints.

### Course request history and deletion approval

Return `courseRequests` in the authorised workspace snapshot. Each record is `{id,submissionId?,kind,courseId,courseTitle,programId,requesterId,status,reason,createdAt,events:[{id,actorId,action,comment,createdAt}]}`. `kind` is Create or Delete; statuses are Pending review, Approved, Changes requested (Create), Rejected, Withdrawn. A Review started event does not resolve the request. Course title/program snapshots and events must remain available after course removal. Scope instructor history to requesterId and leader review queues to owned programs; do not send other users' histories to the client.

- `POST /api/commands/courseRequest`: `{kind:"Create"|"Delete",courseId,reason}`. Instructor-only, assigned course only. Create delegates to the existing course submission validation and sets Pending review. Delete requires no enrolments and leaves the course present. Reject a second pending request for the same course. Returns the created request in `data` and the updated snapshot.
- `POST /api/commands/courseRequestWithdraw`: `{id}`. Only the submitting instructor may withdraw a pending request. Creation withdrawal returns the course to Draft; deletion withdrawal leaves the course unchanged. Retain a Withdrawn event.
- `POST /api/commands/courseRequestSeen`: `{id}`. Program Leader for the assigned program only; append Review started once while pending. The UI exposes Start review on creation and deletion review screens.
- `POST /api/commands/courseRequestReview`: `{id,decision:"Approved"|"Rejected",comment}`. Program Leader-only deletion decision. On approval, atomically recheck that the course exists and has no enrolments, remove it and related material records, and retain the request/events. Repeated decisions must fail. Rejection leaves the course present.

Existing `courseAction submit`, `courseAction withdraw`, and `review` must update the matching creation request cycle. Instructor `courseAction delete` now creates a Delete request and requires `comment` as its reason; it does not immediately delete. Existing Program Leader direct deletion remains a staff management command, subject to pending-request/enrolment restrictions. Management writes to courses with pending deletion are blocked, except authorised review/withdrawal. Implement server transactions, concurrency checks and idempotency for production; client disabling is not an authorisation boundary.

### Admin overview calculations

The Admin dashboard adds no write command. It reads the authorised Admin workspace snapshot and uses existing account, verification, activity and report routes. Summary totals derive from users/courses/programs, quizzes with questions, and lab/quiz activities. Active learners are distinct active Learner accounts with an enrolment.lastActive or attempt timestamp in a selected 7/30/90-day interval. The prior interval has equal length; the UI shows the count difference, avoiding a fabricated percentage when the previous count is zero. The daily line uses unique learner IDs per day. This snapshot-based trend is not complete historical DAU or presence; production should provide aggregate activity events and apply server timezone rules.

Completed-course count is the number of enrolments at 100% progress. Course table and bars use mean progress per course, with a null table value when there are no enrolments. The inspector is read-only and its instructor-account link is Admin-only. CSV uses the existing analytics export. Account approval still uses `userSave`; returning to dashboard reloads counts. Preserve server enforcement of Admin snapshot/account/report access; frontend calculations do not replace authorisation.

Instructor performance cards use authorised `enrollments`, related `users` and `courses`, and `attempts` of type `quiz`. Calculate quiz means per user/course; no attempts yields null in the presentation. Progress uses the shared course progress calculation. A card opens `/learners/{userId}/{courseId}`, preserving that course context. Search/filter/sort are client-side view state. Enrol/remove continue to use `enrollmentSave`. A production workspace response must scope these records to staff ownership, including direct learner-detail access.

The added user-detail, role-permission, active-user, approval-board and public content views use the workspace snapshot. No additional write endpoints are needed for view switching or search. User detail is Admin-only in the frontend and must also be Admin-only in server responses. Active-user counts are demo counts of audit actions, attempts and sent messages, not realtime presence.
