# Program Leader: Function-by-Function Explanation Script

- Scope: **Program Leader only**. Every function used by the leader pages and the leader-facing backend commands is listed.
- Format: **Say** = what you read out loud. `File › function` = where it lives, so you can switch to the code when asked.
- Not covered on purpose: Instructor-only functions in `Learners.aspx.cs` (roster cards, enrol form) and the Learner/Admin pages. Messages was not reviewed.
- Pattern to mention once at the start: every page is an ASP.NET WebForms page that inherits `SkillPage`. It declares `Roles => { "Program Leader" }`, so other roles are blocked. Button clicks call `TryRun("<command>", data)`, which runs a backend command in `Commands`. The backend calls `Must("Program Leader")` again, so the rule is enforced on the server and not only in the UI.

---

## 0. Opening

**Say:** "I'll explain the Program Leader role by walking through the code, page by page. The leader has six areas: the Overview dashboard, Programs, Instructor assignment, Course approvals, Analytics and Learners. For each page I'll cover the functions that run when it loads, then the functions behind each button, then the backend command it calls."

---

## 1. Overview Dashboard

`Frontend/Controls/LeaderDashboard.ascx.cs`

**Say:** "The dashboard is a user control. Everything on it is scoped to the programs where `LeaderId` equals the signed-in user."

| Function | Say |
|---|---|
| `Page_Load` | "This is the main function. It finds my programs, then their courses, enrolments and quiz attempts. From those it calculates four metrics: managed programs, program courses, active learners over the last 30 days compared with the 30 days before, and completion rate. It also builds the list of pending approvals. Then it calls the three builders below." |
| `User(id)` | "A safe lookup. If a user was deleted it returns a placeholder named 'Former user' so the page never crashes." |
| `BuildChart` | "Builds the weekly activity chart. For each day it counts graded attempts plus new enrolments, and stacks them on the same number of days before. The window is 1, 7, 14 or 24 days. It also works out the total and the change against the prior period." |
| `Pct` | "Small helper that turns a part and a whole into a percentage for bar heights." |
| `BuildReach` | "Builds the course performance panel. It takes the course chosen in the dropdown, which is remembered in the session, and shows average progress, status, program, instructor, activities and due date. It also shows the enrolled, in-progress and completed breakdown, the course completion rate, and the overall completion ring." |
| `BuildNotes` | "Builds the alerts inbox. It collects pending approvals, request status changes, learner enrolments and completions, messages sent to me, and announcements in my courses. Each alert goes into Unread, Read or Archived. The read and archived lists come from my saved preferences." |
| `Ago` | "Turns a timestamp into text such as 'Just now', '5 min ago' or '2 days ago'." |
| `Period_Command` | "Runs when I click 24D, 14D, 7D or Today. It saves the choice in the session, only allowing 1, 7, 14 or 24, and reloads the page." |
| `Course_Changed` | "Runs when I change the course dropdown. It saves the course id in the session and reloads." |
| `Tab_Command` | "Runs when I switch between Unread, Read and Archived. It saves the tab and reloads." |
| `Note_Command` | "Runs when I click Mark as read, Archive or Restore on one alert. It updates the read and archived sets and calls the `preferences` command with section `leaderInbox`, so the state is saved to my account." |
| `TabCount` | "Returns how many alerts are in a tab, for the counter on each tab button." |
| `Native` and `MonitorSvg` | "Small rendering helpers. They output the SVG artwork and icons used by the cards and buttons." |
| `LeaderArt.Icon` (`Frontend/LeaderArt.cs`) | "Draws the illustration inside each of the four metric cards. The artwork was converted from the Figma design and `Icon(i)` scales it to the card with an SVG viewBox." |

**Backend behind it** (`Backend/Commands.cs › Preferences`): "For `leaderInbox` it keeps only string ids, cuts each to 80 characters, removes duplicates and caps the list at 500 per list. Then it saves them in the user's preferences JSON."

---

## 2. Programs

`Frontend/Programs.aspx.cs`

**Say:** "This page has two modes. With no id it shows my program library. With `?id=` it shows one program's learning path editor."

### 2.1 Page load and library

| Function | Say |
|---|---|
| `Page_Load` | "Loads only programs where I am the leader, with their courses, ordered path and instructors. If `?id=` names a program that is not mine, it shows 'Denied'. That is the access control." |
| `ShowLibrary` | "Applies the search term from `?q=`. Names that start with the typed text are ranked first." |
| `Card` | "Builds one program card: initials, up to three course chips, course count, Published or Draft state, and the instructors." |
| `HomeUrl` and `DraftKey` | "`HomeUrl` is where the page returns after an action. `DraftKey` is the session key for unsaved path changes, one per leader and program." |
| `Search_Click` | "Puts the search text into `?q=` and reloads." |
| `Open_Command` and `EditorOpen_Command` | "Navigation helpers. They open a link, or add a form flag such as `edit` or `move` to the current program URL." |

### 2.2 Learning path editor

| Function | Say |
|---|---|
| `ShowEditor` | "Loads one program. The path shown is the unsaved session draft if one exists, otherwise the saved path. It also works out which courses are available to add and how many enrolments the program has." |
| `ChangePath` | "The core of the editor. It takes a change, applies it to a copy of the path, stores the result in the session and redirects. So nothing is written to the database until I save." |
| `Arg` | "Reads the course id from the button's command argument." |
| `Up_Command` and `Down_Command` | "Swap a course with its neighbour to reorder the path." |
| `Remove_Command` | "Takes a course out of the path. The course stays assigned to the program." |
| `Add_Command` | "Adds an unlisted course to the end of the path, if it is not already there." |
| `Discard_Click` | "Deletes the session draft, which restores the saved order." |
| `SavePath_Click` | "Sends the draft to the backend `pathSave` command. On success it clears the draft." |

### 2.3 Forms: create, edit, delete, move

| Function | Say |
|---|---|
| `OpenForms` | "Decides which panel to show from the query string: edit, action menu, delete confirmation, help, add existing course, or move course. For add and move it builds the dropdown choices from my other programs. It blocks the move if there are unsaved path changes." |
| `SaveProgram_Click` | "Calls `programSave` with name, description and the Published switch. A new program redirects to its editor." |
| `DeleteProgram_Click` | "Calls `programDelete`." |
| `MoveCourse_Click` | "Calls `programCourseMove`. The same function handles 'Add existing course', where the target is the current program, and 'Move course'. It refuses if either program has an unsaved draft." |

### 2.4 Backend commands (`Backend/CommandsCourses.cs`)

| Command | Say |
|---|---|
| `OwnedProgram` | "Helper that returns a program only if I am its leader. Otherwise it fails with not found." |
| `ProgramSave` | "Needs the Program Leader role and checks ownership. Name is limited to 150 characters and description to 1000. Published becomes status Published, otherwise Draft. A new program also creates its `LEARNING_PATH` row." |
| `ProgramDelete` | "Refuses if the program still has courses. Otherwise it deletes the learning path, the announcements and the program." |
| `PathSave` | "Validates the id list: unique, and every course belongs to this program. Then `SetPath` writes it." |
| `ProgramCourseMove` | "Target program must be mine and different from the current one. The course must not be pending review. It removes the course from old paths, changes `program_id`, and optionally appends it to the new path. Lessons, enrolments and progress are untouched." |
| `PathId` | "Gets the program's learning path id, and creates the path if it is missing." |
| `SetPath` | "Rewrites the path rows in order, with sequence numbers, and marks the first as the starting course." |
| `RemoveFromPaths` and `AppendToPath` | "`RemoveFromPaths` deletes a course from every path. `AppendToPath` adds it to the end of one program's path." |
| `ProgramFor` | "Finds a program by id text, or returns null." |

---

## 3. Instructors (Assignments)

`Frontend/Assignments.aspx.cs`

**Say:** "This is where I decide who teaches each course. `OwnCourses` limits the list to my programs."

| Function | Say |
|---|---|
| `Page_Load` | "Reads `?q=` and `?assign=`. If a course is being assigned it fills the dropdown with 'Unassigned' plus all active instructors. It builds the summary of courses, instructors and unassigned count, then filters the cards by course, program, instructor name or email." |
| `Card` | "Builds one course card. It shows the instructor, email, learners, average completion, activities, due date and status. It also writes a 'Next step' hint that depends on whether there is an instructor and on the status. Footer buttons are View, Message and Change assignment." |
| `Row`, `Chip`, `Initials`, `Svg` | "Rendering helpers for the label-value rows, the pill chips, the avatar initials and the inline icons." |
| `Search_Click` | "Reloads with the search text in the URL. `ListUrl` builds that URL." |
| `Assign_Click` | "Calls the backend `assign` command, then returns to the list with 'Instructor assignment saved'." |

**Backend** (`CommandsCourses.cs › Assign`): "Needs the Program Leader role and a course in one of my programs. It deletes the current `COURSE_INSTRUCTOR` row. If I chose an instructor, it checks that the person is an active Instructor and inserts a new row with me as `assigned_by`. An empty choice means unassigned."

---

## 4. Course approvals

### 4.1 Board: `Frontend/Approvals.aspx.cs`

**Say:** "Every Create or Delete request in my programs appears here. I can view it as a board or a table."

| Function | Say |
|---|---|
| `Page_Load` | "Loads the requests of my programs, newest first. If `?id=` is present it shows one application, otherwise the board." |
| `ShowBoard` | "Restores my saved filters from the session: search, type, program, day and view. It builds the day strip and filters the requests." |
| `Board` and `Table` | "Two views of the same list. The board has three lanes: New (pending review), Rejected and Approved. Changes requested and withdrawn go into an 'Other outcomes' fold." |
| `Card` | "One request card with program badge, request type, reference number, submission time in MYT and requester. Pending requests get quick Approve and Reject links that preselect the decision." |
| `Badge` and `ProgramName` | "The coloured program label, and the program name lookup that falls back to 'Former program'." |
| `MalaysiaTime`, `InMyt`, `DayKey`, `ShortDate`, `ShortTime` | "Time handling. The database stores the server's local time. These convert it to Malaysia time, and `DayKey` is what the date filter compares." |
| `Capture` | "Copies the search box and the two dropdowns into the filter object." |
| `Apply` | "Saves the filters in the session and redirects, so a refresh does not resubmit." |
| `Filter_Changed`, `View_Command`, `AllDates_Click`, `Day_Command`, `Shift_Command` | "Each button first calls `Capture`, changes one filter, then calls `Apply`. They are: type or program or search changed, Board/Table switch, clear the date filter, pick a day, and move the day strip back or forward." |

### 4.2 One application

| Function | Say |
|---|---|
| `ShowApplication` | "Builds the detail view. It shows request type, requester, time, program, reason, and the current course content: outcomes, activities, quiz questions with the correct answer, and downloadable files. For a delete request it shows how many learners are enrolled and warns that deletion cannot be approved while learners exist. It also builds the timeline. If the request is not in my programs it shows 'Request access denied'." |
| `StartReview_Click` | "Calls `courseRequestSeen`. This writes a 'Review started' event so the instructor can see someone has opened it." |
| `SubmitDecision_Click` | "A Delete request calls `courseRequestReview`. A Create or update request calls `review`. Feedback is required." |

### 4.3 Separate review pages

| Page › function | Say |
|---|---|
| `CourseReview › Page_Load` | "Opens one submitted course by `?id=`, checked against `OwnCourses`. It shows outcomes, structure, quiz answers, files, and the review history. The decision form appears only while the course is Pending review." |
| `CourseReview › StartReview_Click` | "Same as above: calls `courseRequestSeen` for the pending Create request." |
| `CourseReview › SubmitDecision_Click` | "Calls `review` with Approved, Changes requested or Rejected." |
| `RequestReview › Page_Load` | "Opens one deletion request, but only if it belongs to one of my programs. It shows requester, course, learner count and history." |
| `RequestReview › StartReview_Click` | "Calls `courseRequestSeen`." |
| `RequestReview › SubmitDecision_Click` | "Calls `courseRequestReview` with Approved or Rejected." |

### 4.4 Backend (`CommandsCourses.cs`)

| Command | Say |
|---|---|
| `CourseRequestSeen` | "Program Leader only. The course must be in my program and the request still pending. It adds a 'Review started' event once, and does not change the status." |
| `Review` | "Handles Create requests. The course must be Pending review. The decision must be Approved, Changes requested or Rejected. Feedback is mandatory, up to 2000 characters. It updates the course: Approved becomes Published, the others keep their name. It inserts a `CONTENT_REVIEW` record and publishes the labs when approved. It also appends the event to the request timeline. If the request id no longer matches the pending one it returns a 409 conflict." |
| `CourseRequestReview` | "Handles Delete requests. Only Approved or Rejected. Approved calls `RemoveCourse`, and the request history is kept." |
| `RemoveCourse` | "Refuses if there are enrolments. Otherwise it deletes resources, announcements, discussion, quiz, labs, path rows and the course itself." |
| `AppendEvent` | "Adds a row to `COURSE_REQUEST_EVENT` and sets the request status to the action, unless the action is 'Review started'." |
| `PendingRequest`, `FindRequest`, `RequestResult` | "Helpers that find the pending request of a course, find a request by id or fail with 404, and return the refreshed request as the response." |
| `CourseAction` with `publish` | "The leader does not need approval. This publishes directly, but only if nothing is pending, and `RequireReady` has checked that the course has an activity and a published quiz where needed. `submit` is blocked for leaders with the message 'Program Leaders publish courses directly'." |
| `RequireReady` | "Guard used before submitting or publishing. It needs at least one activity, and a published quiz with questions when there is a quiz activity." |
| `CourseFor` (`Commands.cs`) | "The ownership check used by most of the commands above. For a Program Leader it fails with 403 if the course's program is not mine. It also blocks editing while a deletion request is pending, unless the call is a deletion review." |

---

## 5. Program analytics

`Frontend/Analytics.aspx.cs` (shared with Instructor, scoped by `OwnCourses`)

| Function | Say |
|---|---|
| `Page_Load` | "Loads enrolments and quiz attempts for my courses. It builds four stat cards: enrolments, average completion, quiz average, and participation. Participation counts enrolments with at least one completed activity. It then builds the programs table and the courses table. The All, Active and Empty filter comes from `?filter=`, and only the first six rows show until `?more=1`." |
| `Initials` | "Makes the short badge text. A label of up to three characters is kept as it is, such as 'C#'. Otherwise it uses the first letters of the first two words." |
| `Filter_Command` | "Switches the All, Active and Empty filter by reloading with `?filter=`." |
| `More_Click` | "Reloads with `?more=1` to show every course." |
| `Export_Click` | "Sends `skillup-analytics.csv` with course, enrolments, completion percent and average quiz score." |

---

## 6. Learners (Program Leader view)

`Frontend/Learners.aspx.cs` and `Frontend/Controls/LearnerTable.ascx.cs`

| Function | Say |
|---|---|
| `Page_Load` | "Loads the enrolments in my courses. With `?learner=` it opens one learner. For an Instructor it shows the roster cards. For me it calls `ShowTable`." |
| `ShowTable` | "The leader's learners table. It builds the stat cards: learners, average completion, completed and in progress. It filters by course, status (in progress, done, idle) and search text, and hands the result to the `LearnerTable` control." |
| `TableUrl` | "Builds the Learners URL from the current status, course and search, so filters survive navigation." |
| `Status_Command` | "Applies the All, In progress, Done or Idle filter." |
| `TableFilter_Click` | "Applies the search box and the course dropdown." |
| `More_Click` | "Shows every row with `?more=1`." |
| `ShowLearner` | "Opens one learner's record in one course: completion, quiz average, learning time, attempts, each activity's state, weak topics from wrong answers, activity counts by type, and attempt history. If the learner is not in my scope it shows an empty state." |
| `Back`, `BackUrl`, `BackLabel` | "Work out the back link. It returns to the Learners list with its filters if that is where I came from, otherwise to the previous page." |
| `LearnerTable › Page_Load` | "Binds the rows with `LearnerLines`. It binds only on the first load, because on a postback the rows come back from view state." |
| `LearnerTable › CanRemove` and `Remove_Command` | "`CanRemove` is true only for Instructors, so the Remove button is **not shown to a Program Leader**. For an instructor, `Remove_Command` calls `enrollmentSave` with `remove = true`." |

**Backend** (`CommandsCourses.cs › EnrollmentSave`): "Checks that the course is in my scope and the learner is active. With `remove` it deletes the enrolment. Otherwise it creates one, rejecting duplicates with 409, and recalculates progress with `RecalculateProgress`. Note that the backend would accept a leader calling this, but the UI does not expose it to leaders."

---

## 7. Related safeguards leaders trigger indirectly

**Say:** "Two rules in `CommandsLearning.cs` protect the leader role."

- "A user who still leads programs cannot be deleted, or have their role or status changed, until the programs are transferred."
- "An admin transfers programs with the Responsibilities command. The target must be an active Program Leader."

---

## 8. Closing

**Say:** "To summarise, the leader's pages are thin. They load only data that belongs to my programs, show it, and call one backend command per action. The backend repeats the role and ownership check, writes an audit event on every decision, and never lets a leader act outside their own programs."

---

## Appendix A: Coverage checklist

| File | Leader functions covered |
|---|---|
| `Controls/LeaderDashboard.ascx.cs` | Page_Load, User, BuildChart, Pct, BuildReach, BuildNotes, Ago, Period_Command, Course_Changed, Tab_Command, Note_Command, TabCount, Native, MonitorSvg |
| `LeaderArt.cs` | Icon |
| `Programs.aspx.cs` | Page_Load, ShowLibrary, Card, ShowEditor, ChangePath, Arg, Up/Down/Remove/Add_Command, Discard_Click, SavePath_Click, OpenForms, Open_Command, EditorOpen_Command, Search_Click, SaveProgram_Click, DeleteProgram_Click, MoveCourse_Click, HomeUrl, DraftKey |
| `Assignments.aspx.cs` | Page_Load, Card, Row, Chip, Initials, Svg, Search_Click, Assign_Click, ListUrl |
| `Approvals.aspx.cs` | Page_Load, ShowBoard, Board, Table, Card, Badge, ProgramName, MalaysiaTime, InMyt, DayKey, ShortDate, ShortTime, Capture, Apply, Filter_Changed, View_Command, AllDates_Click, Day_Command, Shift_Command, ShowApplication, StartReview_Click, SubmitDecision_Click |
| `CourseReview.aspx.cs` | Page_Load, StartReview_Click, SubmitDecision_Click |
| `RequestReview.aspx.cs` | Page_Load, StartReview_Click, SubmitDecision_Click |
| `Analytics.aspx.cs` | Page_Load, Initials, Filter_Command, More_Click, Export_Click |
| `Learners.aspx.cs` | Page_Load, ShowLearner, ShowTable, TableUrl, Status_Command, TableFilter_Click, More_Click, Back, BackUrl, BackLabel |
| `Controls/LearnerTable.ascx.cs` | Page_Load, CanRemove, Remove_Command |
| `Backend/CommandsCourses.cs` | ProgramSave, ProgramDelete, PathSave, ProgramCourseMove, Assign, EnrollmentSave, Review, CourseRequestSeen, CourseRequestReview, CourseAction (publish), RequireReady, RemoveCourse, AppendEvent, PendingRequest, FindRequest, RequestResult, OwnedProgram, ProgramFor, PathId, SetPath, RemoveFromPaths, AppendToPath, RecalculateProgress |
| `Backend/Commands.cs` | Preferences (leaderInbox), CourseFor |

## Appendix B: Likely questions

- **Why does the leader have no Submit button?** `CourseAction` rejects `submit` for leaders. They publish directly with `publish`.
- **Why can a program not be deleted?** `ProgramDelete` fails while any course still has that `ProgramId`.
- **Why did Move fail?** The course is pending review, the target program is not mine, or there are unsaved path changes.
- **Why can I not delete an approved course?** `RemoveCourse` refuses when the course has enrolments. The instructor should hide it instead.
- **Where is the 'read' state of alerts stored?** In `USER.preferences` JSON under `leaderInbox`.
