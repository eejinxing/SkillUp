# Web Forms pages

Every screen is a Web Forms page in `Frontend/`: `Name.aspx` holds the markup and server controls, `Name.aspx.cs` the code-behind, and `Name.aspx.designer.cs` the control fields Visual Studio generates. The page renders on the server and saves through postbacks; links use the page name without `.aspx` (FriendlyUrls), for example `Course?id=3`.

All pages inherit `SkillPage` (`Frontend/SkillPage.cs`), which:

- loads the database snapshot (`Backend/Model`) and the signed-in account,
- sends a visitor to `Login?return=…` when the page needs an account, and shows "Access restricted" when the role may not open it,
- saves with `Run` / `TryRun`, which call `Backend/Commands` in one transaction, so every rule and permission check is the same one the JSON API uses,
- redirects after a save (`ReloadPage`, `Go`) with a one-line notice, so a refresh never repeats it.

`SkillUp.Master` draws the capsule navigation for the role, the page frame and the notice.

## Pages

Roles: **V** visitor, **L** Learner, **I** Instructor, **P** Program Leader, **A** Admin. "Any" means anyone; "Signed in" means any role.

| Page | Who | Query parameters | What it shows |
|---|---|---|---|
| Home | Any | | Landing page. The visitor tabs scroll to its sections: About `#top`, Our Courses `#discover`, Our Instructors `#visitor-instructors`, Feedback `#visitor-stories`, FAQ `#visitor-faq`, and Help (like the footer's social icons) the enquiry form `#home-enquiry`. My Enquiries opens EnquiryPortal |
| Catalog | Any | `q`, `level` | Course catalogue |
| Course | Any | `id` | Course details, enrolment, reviews, files, discussion |
| Activity | Any | `course`, `lesson` | One activity: article, lab (with the practice terminal) or timed quiz, and the learning companion. Visitors see public previews |
| About, Terms, Privacy, FAQ, Instructors | Any | | Information pages |
| Help | Any | | The enquiry form for signed-in accounts. Visitors are sent to the one on Home (`Home#home-enquiry`) |
| EnquiryPortal | Any | `id` | The enquiries this browser (or the signed-in account) sent: list, edit, delete |
| Login | V | `return` | The Log in pop-up shown open (sign-in redirects come here). Elsewhere it opens over the page from links to `#login` |
| Register | V | `email` | The Sign up pop-up shown open. Elsewhere it opens over the page from links to `#register` |
| ForgotPassword, ResetPassword | V | `token` (reset) | Password reset |
| Dashboard | Signed in | Admin: `q`, `role`, `source` | Loads `Controls/<Role>Dashboard.ascx`: LearnerDashboard, InstructorDashboard, LeaderDashboard or AdminDashboard |
| Messages | Signed in | `id`, `channel`, `announcement`, `compose`, `q` | Direct messages, course channels and announcements |
| Discussions, LiveSessions, Assistant, Profile, Settings | Signed in | Settings: `tab` | Course discussions, live sessions, the companion, profile and account settings |
| MyCourses | L | `program` | Enrolled courses and learning paths |
| Leaderboard | L | `period`, `q` | Weekly and all-time ranking |
| Certificates | L | `id` | Certificates of completed courses |
| Courses | I, P | `q`, `status` | Courses the account manages |
| CourseEditor | I, P | `id` (none = new), `program`, `tab`, `lesson` | Course information, activities, review submission, learners and files |
| QuizEditor | I, P | `id` | The course quiz; unsaved edits are kept for the session |
| Learners | I, P | `course`, `learner`, `q`, `state`, `sort`, `status`, `more` | Instructor: student performance cards. Leader: learners table. With `learner`: one learner's record |
| Analytics | I, P | `filter`, `more` | Enrolment, completion and quiz figures; CSV export |
| Resources | I | | Activities and files of the instructor's courses |
| CourseRequests | I | `id`, `q`, `kind`, `status`, `sort`, `page`, `size`, `tab`, `export` | The instructor's Create / Delete requests; CSV export |
| Approvals | P | `id`, `decision` | Request board (filters kept for the session) and one application with its decision form |
| CourseReview | P | `id` | Review one submitted course |
| RequestReview | P | `id` | Review one deletion request |
| Programs | P | `id`, `q`, `edit`, `menu`, `delete`, `help`, `add`, `move` | Programs and the learning path editor (path changes kept for the session until saved) |
| Assignments | P | `q`, `assign` | Instructor assignment per course |
| Users | A | `q`, `role`, `status`, `sort`, `view`, `limit`, `actions` | Account directory |
| UserEditor | A | `id` (none or `new` = create), `tab` | Create or manage an account, its activity log and learning |

## Shared pieces

| File | Used by |
|---|---|
| `Controls/SubmitButton.cs` | Every page. A `Button` that renders `<button>`, so it can hold icons and keeps the button styles |
| `Controls/CourseCard.ascx`, `CourseHero.ascx`, `CourseFiles.ascx` | Catalog, MyCourses, Courses, Course, CourseEditor, CourseReview, Resources |
| `Controls/TerminalLab.ascx` | Activity (lab activities) |
| `Controls/LearnerTable.ascx` | CourseEditor (Learners tab), Learners (Program Leader) |
| `Controls/EnquiryForm.ascx`, `AuthArt.ascx` | Home and Help; the sign-in pop-ups and ForgotPassword, ResetPassword |
| `Controls/LoginDialog.ascx`, `RegisterDialog.ascx` | Log in and Sign up pop-ups on every visitor page, drawn by `SkillUp.Master`. A link to `#login` / `#register` opens one over the page (CSS `:target`, no script); a refused form comes back with it open. Signing in from a Course or Activity page returns there (`SkillPage.StayAfterSignIn`) |
| `Controls/SiteFooter.ascx` | Footer of every visitor page (Home, EnquiryPortal, Catalog, About, …): sign-up, contact links, logo, legal links. `SkillUp.Master` draws it; signed-in accounts have no footer |
| `Ui.cs`, `Icons.cs`, `Charts.cs`, `Insights.cs` | Encoding, dates and tags; inline icons; chart drawing; attempts and the leaderboard |
| `*Art.cs` | Figma artwork kept as markup (course editor, program and leader dashboard illustrations) |

## Scripts

Pages work without JavaScript, except the parts that need the browser:

| Script | Purpose |
|---|---|
| `js/site.js` | Small enhancements on every page: confirm before deletes, the notice fading, Enter-to-send, print buttons, figures counting up, the request table's tick boxes |
| `js/activity.js` | Activity page: the quiz timer and the lab terminal, which runs code in a Web Worker |
| `js/call-preview.js` | The device-only call preview (camera and microphone stay on the computer) |

ASP.NET's own scripts handle validators, Enter in a form (`DefaultButton`) and drop-downs that apply at once (`AutoPostBack`).
