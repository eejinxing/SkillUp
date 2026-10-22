# Web Forms page pairs

All paths are relative to Frontend/. Every .aspx has a matching .aspx.cs class with a Page_Load event. Shared layout: SkillUp.Master + SkillUp.Master.cs.

Navigation inside the app still uses the existing hash router; these are direct ASP.NET entry pages, not a rewrite of every action as server postbacks. Initial query parameters are safely encoded by code-behind. Page names alone do not grant role permissions.

| .aspx + .aspx.cs | Initial hash route | Query parameters | Missing required parameter fallback |
|---|---|---|---|
| Home | #/home | — | — |
| Login | #/login | — | — |
| Register | #/register | — | — |
| ForgotPassword | #/forgot | — | — |
| ResetPassword | #/reset | token | #/forgot |
| Dashboard | #/dashboard | — | — |
| MyCourses | #/learning | — | — |
| Catalog | #/catalog | — | — |
| Course | #/course | id | #/catalog |
| Activity | #/lesson | course,lesson | #/learning |
| Leaderboard | #/leaderboard | — | — |
| Certificates | #/certificates | id | — |
| Courses | #/courses | — | — |
| CourseEditor | #/course-edit | id | — |
| QuizEditor | #/quiz-editor | id | #/courses |
| CourseRequests | #/requests | id | — |
| Approvals | #/approvals | id | — |
| CourseReview | #/review | id | #/approvals |
| RequestReview | #/request-review | id | #/approvals |
| Programs | #/programs | — | — |
| Assignments | #/assignments | — | — |
| Learners | #/learners | — | — |
| Analytics | #/analytics | — | — |
| Resources | #/resources | — | — |
| Messages | #/messages | id | — |
| Users | #/users | — | — |
| UserEditor | #/users | id | #/users/new |
| Profile | #/profile | — | — |
| Help | #/help | — | — |
| EnquiryPortal | #/enquiries | — | — |
| About | #/about | — | — |
| Contact | #/contact | — | — |
| Instructors | #/instructors | — | — |
| FAQ | #/faq | — | — |
| Terms | #/terms | — | — |
| Privacy | #/privacy | — | — |
| Discussions | #/discussions | — | — |
| Assistant | #/assistant | — | — |
| LiveSessions | #/sessions | — | — |
| Settings | #/settings | — | — |

Admin page removals (2026-10-22): Roles and Verification page pairs were removed. Their old hash URLs redirect to User accounts for Admin, or the appropriate workspace/sign-in screen for other visitors. Platform reports is removed for Admin. Analytics.aspx remains the shared entry for Instructor and Program Leader learning analytics; Admin opens Overview instead.

Admin Active users and Activity & audit were removed on 2026-10-22. ActiveUsers.aspx / ActivityLogs.aspx and their code-behind were deleted; 42 Frontend page pairs remain. Old hash routes redirect to Overview for signed-in users and Log in for guests. Activity.aspx is the learner lesson entry and remains available. The Admin overview activity log widget is retained.

Admin Discussion moderation and Enquiries & feedback were removed on 2026-10-22. Moderation.aspx / Support.aspx and their code-behind were deleted; 40 Frontend page pairs remain. Old hashes redirect to Overview for signed-in users and Log in for guests. Course discussions, visitor enquiry submission and the personal enquiry portal remain available.

Standalone Assistant, Discussions, LiveSessions and Settings pages were removed on 2026-10-22. There are 36 Frontend Web Forms pairs. Old hashes redirect to Overview for signed-in users or Log in for guests. Profile account controls, activity companion and Messages remain.

Correction (2026-10-22): Only the global sidebar is removed. Assistant, Discussions, LiveSessions and Settings page pairs and routes are restored, bringing the current Frontend inventory back to 40 pairs. All retained workspace functions are accessible from the top navigation and its More menu.
