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
| Discussions | #/discussions | — | — |
| Messages | #/messages | id | — |
| Assistant | #/assistant | — | — |
| LiveSessions | #/sessions | — | — |
| Users | #/users | — | — |
| UserEditor | #/users | id | #/users/new |
| Verification | #/verification | — | — |
| ActivityLogs | #/activity | — | — |
| Moderation | #/moderation | — | — |
| Profile | #/profile | — | — |
| Settings | #/settings | — | — |
| Roles | #/roles | — | — |
| ActiveUsers | #/active-users | — | — |
| Support | #/support | — | — |
| Help | #/help | — | — |
| EnquiryPortal | #/enquiries | — | — |
| About | #/about | — | — |
| Contact | #/contact | — | — |
| Instructors | #/instructors | — | — |
| FAQ | #/faq | — | — |
| Terms | #/terms | — | — |
| Privacy | #/privacy | — | — |
