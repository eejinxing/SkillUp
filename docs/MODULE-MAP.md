# Frontend module map

Paths below are relative to Frontend/. Runtime script/style order is in index.html.

| Area | Main files | Responsibility |
|---|---|---|
|Entrypoint / routing|js/app.js, js/pages.js|Authentication, route permission gates, forms and page composition|
|Transport / fixtures|js/api.js, js/runtime-config.js, js/seed.js, *-data.js|Mock now; replace transport and fixture dependencies before production|
|Public home / enquiry|js/home-refinement.js, js/home-reference.js, js/enquiry-portal.js|FAQ, inquiry submit, access-code CRUD|
|Shared navigation / calls|js/ui-refinement.js, js/workspace-nav.js, js/profile-page.js|All-role navigation; calls are device/mock preview|
|Learner dashboard|js/learning-console.js, css/learner-velo.css|Purple header; black metrics; course progress list|
|Courses / learning|js/enroll-page.js, js/my-courses.js, js/answer-page.js|Course overview, enrolment, activities, labs and quiz UI|
|Leaderboard|js/leaderboard.js, css/leaderboard-timing.css|F1-style learning standings; scores from workspace|
|Messages|js/chat-page.js|Contacts, drafts, read state and messages|
|Instructor|js/instructor-dashboard.js, js/course-editor.js, js/quiz-editor.js, js/student-performance.js|Course authoring and student progress|
|Course requests / review|js/course-requests.js, js/leader-applications.js|Create/delete request history and decisions|
|Program Leader|js/leader-dashboard.js, js/program-management.js|Managed programs, ordering, transfer, assignments|
|Admin|js/admin-dashboard.js, js/admin-users.js, js/user-editor.js|Accounts, verification, activity and reports|
