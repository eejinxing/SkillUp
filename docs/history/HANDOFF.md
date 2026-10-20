# Skill UP 前端交接

## Program Management 與申請審批（最新更新）

Program Leader 的 `#/programs` 對照 Figma `250:4767`：三欄黑灰課程文件卡、New Program bento 插圖及完整灰色導覽。程式為 `js/program-management.js`／`program-management.css`；八個原始素材在 `assets/program-management/`，context／清單在 `design/program-management-*`。`scripts/build-program-art.cjs` 從 source 的 `250:5017` 保留原生方塊、陰影、旋轉和＋號，輸出 `js/program-management-art.js`。目前顯示三個實際 managed programs，不重複假卡填滿六格。

點 Program 開 `#/programs/{id}`，可編輯名稱／說明／發布狀態、新建所屬 Course、增減及上下調整 learning path 的課程，再 Save path。`course.programId` 是課程所屬；`program.courseIds` 是路徑中的排序子集。從 path 移除不會刪除課程或學生紀錄；All assigned courses 仍能編輯或 Move program。Add existing course 會從另一個自己管理的 program 移入。移動保留教材、題目、instructor、publication、enrolments、attempts／進度，從原 path 清除，按 checkbox 決定是否加入新 path。Pending review 課程及刪除審批中的課程須先撤回。任一相關 path 有未存草稿時先 Save／Discard；非空 program 不可刪除。路徑草稿為 `skillup.program.path.{leaderId}.{programId}`，保存成功或 Discard 才清除。

`#/approvals` 對照 Figma `250:3682`：黑灰 Requests 看板、原粉色 program badge、灰色 rail、真實日期選擇條與底部紫色光暈。runtime 為 `js/leader-applications.js`／`leader-applications.css`；source 是 `design/leader-approvals-source.jsx.txt`，原 SVG 是 `assets/leader-approvals/imgVector.svg`。素材保留 intrinsic dimensions，wrapper transform 縮放；未把 screenshot 作為背景。New／Rejected／Approved 三欄以 managed programs 的 courseRequests 實際狀態分類，不把所有 Published 課程當作已獲批申請。Changes requested／Withdrawn 在 Other outcomes，Table 可查全部狀態。搜尋／program／type／日期篩選和 Board/Table 狀態依 Leader 存 sessionStorage。日期依 Asia/Kuala_Lumpur，正確產生跨月日曆，不沿用來源圖不存在的 September 31；原 Urgent／Event 假標籤換成真實 request type／requester。

`#/approvals/{requestId}` 顯示申請原因、目前 Course 資訊、活動、題目答案、檔案、申請進度／回饋。Approve／Reject 卡片按鈕預選決定，再由詳情表單填回饋後提交；不會直接略過查看內容。Create 可 Approved／Rejected／Changes requested，Delete 可 Approved／Rejected。Create 獲批發布，Delete 獲批重查 enrolments 後刪除課程與素材，保留申請 events；拒絕刪除保留課程。已完成／撤回申請唯讀，即使同課程後續再申請也不重開舊決定。Create review 帶 requestId，服務須原子驗證當前 pending 申請一致，防止舊分頁錯批新申請。未存 feedback 草稿為 `skillup.approvals.decision.{leaderId}.{requestId}`，錯誤／refresh 保留，成功清除。Dashboard pending 連新詳情；舊 `review/{courseId}`、`request-review/{id}` 保留相容。

驗證：Program Management 43 個 data checks 與完整 browser flow；Applications data/browser tests 包含實際建立／拒絕／再提交／批准、刪除批准／拒絕、Instructor 回饋同步、錯誤重試／還原、權限、歷史、MYT 日期及 1440／960／390 寬度。既有 domain 41 assertions、course request domain/UI、Leader dashboard 與 60 routes 角色檢查通過。新增預覽在 `previews/program-management-*`、`program-course-editor-*`、`leader-applications-*`、`leader-application-detail*`；verification JSON 記錄結果。測試在隔離瀏覽器，不寫入使用者目前的 demo 資料。仍是 HTML/CSS/JavaScript 前端及 mock API，未連接真實 C# 服務。

## Program Leader Dashboard（本次更新）

Program Leader 登入後的 `#/dashboard` 按 Figma Frame 172（`250:5041`，page `126:20`）重建：灰色導覽、四個 KPI、分段活動圖／完成率圖、Pending Approval、Messages、AI Companion、Announcements。首頁、Program Management 及 Requests 採完整寬度與選單抽屜；其餘 Program Leader 頁沿用原本導覽。使用 `js/leader-dashboard.js`、`leader-dashboard.css`。原始畫面過大，分區讀取完整 context；31 份 source 存在 `design/leader-*.jsx.txt`，87 個原始素材與清單在 `assets/leader-dashboard/`／`design/leader-dashboard-assets.json`。`scripts/build-leader-dashboard-art.cjs` 保留四組原圖迷你柱圖及兩種公告玻璃氣泡的圖層／光效／旋轉與 blend mode，輸出 `js/leader-dashboard-art.js`。SVG intrinsic dimensions 不改，由 wrapper transform 縮放，並限制素材 overflow。

範圍：先按 `program.leaderId` 取 managed programs，再按 programId 取課程、enrolments、attempts、announcements。KPI 為 program 數、course 數、過去 30 個日曆日有紀錄且目前 Active 的唯一 Learner 人數、enrolment 平均活動完成率。Active 比較為前一個 30 日區間唯一人數差，並非來源圖的固定增長百分比。完成率下方另列已完成 enrolments／總 enrolments。無記錄、無課程或無 enrolments 時不捏造數值。

活動圖以選取 24／14／7／1 天的 `attempts.createdAt` 統計，與前一個等長期間按日對齊，白色為選取期間、灰色為前一期間，數字比較為總 attempt 數差。今天選項沿用原圖 now。排除無效與未來時間；Y 軸依目前／前期合計自動調整，最少 4，使 count 刻度為整數。單位是 learning attempts，並非登入次數、即時在線或人數。右圖依當前 enrolments 顯示各 course 平均完成率，未選日期區間篩選；畫面明標 Current average progress，無 enrolment 顯示基線並以 aria-label 說明。活動與 Active 趨勢只能使用現有 snapshot 的最新紀錄，正式服務如需完整歷史分析須提供聚合事件資料。圖柱隨實際假數據更新，不用固定原圖柱高填滿。

Pending Approval 合併 managed programs 內 Pending review 的 Create／Delete courseRequests，並兼容沒有 request 的舊 Pending review course；同課程的 Create fallback 不重複。有 courseRequest 的新增／刪除均連 `#/approvals/{requestId}`；舊 course fallback 仍連 `#/review/{courseId}`，由現有審批命令決定。最多顯示 8 筆並提供 View all。Messages 顯示管理範圍內的講師／學生及已有對話的聯絡人，按最近對話排序、列實際 unread；沒有對話時顯示 Start a conversation，搜尋後開啟現有聊天頁，不捏造訊息。

AI 使用既有 `ai` 命令與該 Leader 個人歷史，明標 Sample replies；＋可填入 program context。公告 feed 為管理課程的實際 announcements，最多顯示 4 筆，可捲動；下方選擇已 Published 的課程、填標題與內容，使用現有 announcementSave。沒有 Published 課程時停用發布。兩種 composer 的 draft 分別存在 sessionStorage `skillup.leader.ai.{id}`／`skillup.leader.announcement.{id}`，儲存成功才清除，錯誤保留。沒有新增 API 命令。

驗證：`tests/leader-dashboard-data.cjs`（program scope、活動區間／唯一人數、完成率、Create／Delete queue、空資料）、`tests/leader-dashboard.cjs`（原始素材、日期切換／還原、訊息搜尋／導覽／unread、AI、公告失敗重試與還原、審批／刪除連線、學習完成後指標更新、手機及未分配 Leader），並執行既有角色導覽檢查。預覽為 `previews/leader-dashboard-desktop.png`／`leader-dashboard-populated.png`／`leader-dashboard-mobile.png`；報告 `leader-dashboard-verification.json`。測試使用隔離資料，不修改使用者瀏覽器中的實際工作區。

## Admin 用戶詳情／新增帳號（本次更新）

`#/users/{id}` 與 `#/users/new` 共用 Figma `250:1980` 全頁表單，原本的 Edit／Add User modal 已改為此頁。左邊顯示個人照片、姓名、email、唯讀 ID、Joined、Last active；右邊 Profile & access 提供姓名／email／username／bio／status／role，同一個 Save 一次提交。新增時補 Temporary password（至少 8 字元），ID 由 mock API 產生，不接受表單修改。`#/users/{id}/access`、`/information` 舊連結仍開啟相同整合表單；`/audit` 開啟 Activity Logs；`/learning` 保留 enrolments 與 assigned courses。Admin 用戶頁現皆使用選單抽屜。

`js/user-editor.js`、`user-editor.css` 使用原圖側欄、欄位、頁籤、圖示、鎖及角色選單。27 個原始素材存在 `assets/user-editor/`，`scripts/build-user-editor-art.cjs` 將角色選單的原始光效／內層／箭頭寫入 `js/user-editor-art.js`，保留 source 的 15 個圖層與額外箭頭。SVG intrinsic dimensions 不改，由 wrapper transform 縮放。原圖頭像只作缺少個人 avatar 的預設；不將 sample John Doe／MongoDB 文案當真實資料。

帳號日期：新建帳號寫入真實 `createdAt`；既有資料沒有日期時顯示 Not recorded，不捏造加入日。Last active 取帳號記錄及 enrolments／attempts／messages／audit 中由該帳號實際產生的最新有效非未來時間，不以管理員修改他人資料當作對方活動。Activity Logs 按 actorId 或新增的 `targetUserId` 取得紀錄，兼容舊 `User account saved` 的 email matching。`userSave` 回傳 `{id}`；新增／更新都寫入帶 `targetUserId` 的帳號儲存紀錄，現有 frontend 亦兼容回傳缺少 id 時用提交 id 或更新 snapshot email 尋找帳號。查無指定 id 不會誤建為新帳號。

停用／重啟／批准走既有 `userSave`，先確認後使用已保存帳號欄位變更狀態，不順便提交未儲存表單。刪除沿用 `userDelete`、明確確認及 reassignment 保護，成功返回用戶列表。自己的停用／刪除按鈕停用，直接提交降權或停用仍由 API 擋下。姓名／email／username 儲存前 trim，email 轉小寫，唯一性驗證忽略帳號大小寫。未儲存欄位依 admin／target ID 存 `skillup.user.editor.{adminId}.{id}`，密碼不寫入分頁草稿；儲存／Discard／狀態動作成功後清除對應草稿，錯誤保留表單與可重試狀態。

驗證：`tests/user-editor.cjs`、`tests/user-editor-data.cjs`、`tests/admin-users.cjs`（目錄至新表單連線）、`tests/role-completion.cjs`（60 routes）與原本 domain 41 assertions。預覽 `admin-user-edit-{desktop,mobile}.png`、`admin-user-create-desktop.png`、`admin-user-activity.png`；報告 `previews/user-editor-verification.json`。後端仍須在 C# 服務實作相同授權、驗證及時間／audit 欄位。

## Admin User Management（本次更新）

`#/users` 按 Figma `250:1288` 重建：六個原始藍色光效圖層、六欄膠囊卡片、搜尋／Add User／Load More。窄螢幕依序四／三／兩欄。`js/admin-users.js` 和 `admin-users.css` 為獨立頁面；首頁及用戶列表使用導覽抽屜，帳號詳情及其他 Admin 功能沿用現有導覽。原圖角色頭像只作無 avatar 時的預設，實際 avatar 仍由用戶資料提供。每張卡片顯示姓名、角色和代表狀態的圓點，完整狀態也放在連結的無障礙名稱及 title。

按姓名／email／username／ID 搜尋，加角色及狀態篩選；篩選後重新顯示前 60 筆，Load More 再顯示 60 筆，已載入全部時隱藏按鈕。只列實際假數據，不以重複用戶填滿 60 個位置。篩選／顯示筆數保存在當前分頁的 `skillup.admin.users.{adminId}` sessionStorage。大資料量正式服務需改為後端搜尋和分頁；目前是已授權 snapshot 的前端切片。

點卡片進入既有 `#/users/{id}`，含 User information／Role & access／Learning activity／Activity logs。卡片 ⋯ 選單接上既有新增／編輯／審核／刪除命令。API `userSave` 保留 email／username 唯一性檢查及不可移除自己的 Admin 身份／啟用狀態；`userDelete` 保留自身帳號及已分配 course／program 保護。刪除須明確確認。沒有新增 API 命令或對 Figma 寫入。測試採隔離瀏覽器資料，額外測試用戶不會加入使用者的實際工作區。

驗證：`tests/admin-users.cjs`，涵蓋原圖素材／幾何、搜尋／篩選／空狀態／重新載入、帳號詳情與權限、CRUD／審核、重複與自身權限保護、65 筆資料的 60＋5 分頁、鍵盤焦點、手機導覽及非 Admin 存取拒絕。預覽：`previews/admin-users-desktop.png`（實際種子 7 人）、`admin-users-full-directory.png`（隔離測試 65 人）、`admin-users-mobile.png`。

## Admin Dashboard（本次更新）

Admin 登入後的 `#/dashboard` 已改為 Figma `250:845`：寬版藍色橫幅、Summary 彩色圓球、Active learners、Course completions、藍青色待辦區及黑色課程表格。原始素材保存在 `assets/admin-dashboard/`，source／inventory 在 `design/admin-dashboard-*`。`js/admin-dashboard-art.js` 由 `scripts/build-admin-dashboard-art.cjs` 保留原圖形及光效的相對位置；執行碼為 `js/admin-dashboard.js`、`admin-dashboard.css`。Admin 首頁用選單抽屜，其餘 Admin 頁保留原本導覽。

摘要：Users 為全部帳號、Courses 為全部課程、Programs 為全部 program、Quizzes 為有題目的 course.quiz 數、Assessments 為 lab／quiz 活動數。圓球和圖例數字同源。Active learners 為所選 7／30／90 天內，在 enrolment.lastActive 或 attempts.createdAt 有紀錄、且目前仍 Active 的 Learner 帳號唯一人數；排除其他角色、停用帳號、無效／未來時間。比較值是與前一個等長區間的唯一人數差。日趨勢亦按帳號去重，並非即時在線人數或完整登入分析；現有 snapshot 只有最新 enrolment 活動與 attempt 紀錄，正式服務需提供完整事件或聚合報表。

Course completions 為 `SkillData.progress === 100` 的 enrolment 數，同時標示全部 enrolment 分母。柱圖依課程顯示平均完成率（0–100%），無 enrolment 顯示基線，表格則顯示「—」；原圖的固定曲線、柱高及增長百分比已由實際示範紀錄取代。活動區間選擇只存在 sessionStorage `skillup.admin.activityWindow`。

課程表可按名稱／講師搜尋、按狀態篩選，點課名查看資料並開啟講師帳號。Export CSV 沿用現有 analytics 匯出；帳號、待審核、報表、Activity、Support 入口已接線。帳號審核返回首頁後會重新計算待辦數。待審課程與刪除申請的決定仍由 Program Leader 處理，Admin 待辦卡只連到平台報表。測試：`tests/admin-dashboard-data.cjs`、`tests/admin-dashboard.cjs`；預覽：`previews/admin-dashboard-{desktop,mobile}.png`。

## Instructor Creating／Deleting 申請列表（本次更新）

`#/requests`、`#/requests/{requestId}` 使用 Figma Frame 201（`294:18`）：灰色左欄／申請列、右邊 Request overview 與 Track Progress。支援課程／原因搜尋、建立／刪除類型、狀態篩選、選取申請及原生可收合時間線。入口已放進 Instructor 導覽、Dashboard、學生表現頁快選列及課程編輯頁。

新增 snapshot 集合 `courseRequests`，保存 requesterId、kind（Create／Delete）、courseId、courseTitle snapshot、programId、status、reason、createdAt、events。現有 approvals 中有 Submitted 且課程仍存在的紀錄會轉成歷史申請，每個 submissionId 只產生一次；重新送審是新一筆申請，先前結果保留。未送出的 Draft 不會列為已提交申請。原本的 courseAction submit／withdraw 及 review 決定會更新同一份申請時間線。Start review 是 Program Leader 的實際操作，不會自動捏造「已讀」或批准事件。

Instructor 原本 Delete course 改成 Request deletion，需填原因；刪除申請不立即移除課程。Program Leader 在 Course approvals 下方 Course deletion requests 開啟 `#/request-review/{requestId}`，可 Start review、Approve 或 Reject。Approve 時再次檢查無 enrolments，才移除課程、program 路徑引用及該課程教材／討論／公告／session／review／attempt records；申請快照和時間線保留。已有學生的課程須 Hide course。Pending deletion 鎖定課程管理修改，Instructor 可從 My requests 撤回後再編輯；建立申請撤回則讓課程回到 Draft。

刪除申請、撤回、review commands 均使用模擬 API 並保存 localStorage；沒有建立 C# 後端。正式服務需按 requesterId 與 program ownership 限制讀寫，使用 transaction 保證審批／刪除原子性及重複提交保護。篩選只存於目前分頁 sessionStorage `skillup.requests.filters.{userId}`。預覽在 `previews/instructor-course-requests-{desktop,mobile}.png`；測試為 `tests/course-requests-domain.cjs`、`tests/course-requests.cjs`。

## Course information 建立／編輯（本次更新）

`#/course-edit/new`、`#/course-edit/new/{programId}` 和 `#/course-edit/{courseId}` 共用 Figma `250:3989` 版型。原圖的 HTML5 圖案、側欄底圖、分頁底圖與藍青色欄位素材保存在 `assets/course-editor/`；`js/course-editor-art.js` 由 source layer placements 產生，沒有以整頁截圖代替可編輯表單。執行碼是 `js/course-editor.js` 和 `course-editor.css`，樣式以 `ci-` 隔離，避免影響學員 Enroll 頁。

可保存 title、programId、difficulty、deadline、duration、description、outcomes、新增的 startDate；deadline／startDate 可留空，兩者有填時會驗證開始日不晚於截止日。Assigned Instructor 是現有指派結果；Program Leader 可透過側欄 Change assignment 更改。Course Status 與 Visibility 為唯讀審批結果，不會由表單直接發布。保存既有 Published 課程仍回到 Draft，Pending review 禁止更改，需先 Withdraw submission。

Learners 分頁顯示這門課的學生並連到 Student performance；Learning Resources 沿用 resourceSave／resourceDelete／下載功能。新建課程先保存才啟用兩個分頁。活動 CRUD、測驗編輯、送審與 review history 保留在 Information 下方。修改尚未保存時，送審會提示先保存。

草稿 key：`skillup.course.editor.{userId}.{courseId}`；新建為 `skillup.course.editor.{userId}.new.{programId或空字串}`。草稿只存在目前分頁的 sessionStorage；保存／確認 Discard 後清除，模擬失敗保留輸入。`tests/course-editor.cjs` 驗證建立、編輯、日期、草稿、分頁、審批鎖定／撤回、未授權存取及手機版；預覽為 `previews/course-information-{edit,create}-desktop.png` 與 `previews/course-information-edit-mobile.png`。

## Instructor 題目編輯與學生表現（本次更新）

`#/quiz-editor/{courseId}` 已對照 Figma `250:1557`：左側原版 AI 球體、MARATHON、可編輯題卡和 Submit／Hint 玻璃按鈕。銀色 Submit 與 Save quiz 都會保存整份測驗；藍色 Hint 提供出題指引。可增刪題目及 2–6 個選項、選正確答案、填 topic、設定時間及啟用測驗。`published` 表示測驗啟用，課程仍需審批；保存會令課程回到 Draft，Pending review 則禁止編輯。刪除測驗需確認。

編輯草稿放在目前分頁的 sessionStorage：`skillup.quiz.editor.{userId}.{courseId}`；AI 輸入草稿在同 key 加 `.ai`。重新載入、回到頁面及發送 AI 不會覆蓋尚未保存的問題；保存或確認 Discard draft 後清除編輯草稿。成功刪除測驗亦清除草稿。失敗時保留輸入及顯示錯誤。AI 仍是本機樣板回覆，沒有接外部模型。

`#/learners` 在 Instructor 身分使用 Figma Frame 190（`250:1979`）卡片排列；`#/learners/course/{courseId}` 預選課程。每張卡是一筆有權查看的 enrolment，顯示學生姓名、email、課程、完成率、已完成活動／總活動、平均 quiz 分數和最近活動。無 quiz attempt 顯示「—」，不是 0%；不足 70% 才用紅色。搜尋、課程／進度篩選、排序、Enrol、Remove 和點入個人詳細紀錄均已接線。個人頁保留完成類型、錯誤主題及嘗試紀錄。

資料計算依現有 snapshot，沒有新增後端 command。正式 ASP.NET 必須在服務端按 instructor assignment／program ownership 篩選資料並驗證授權；平均分母只計已提交 quiz，不混入 lab。最近活動為 enrolment 的 lastActive，並非即時線上狀態。預覽：`previews/instructor-question-editor-{desktop,mobile}.png`、`previews/instructor-student-performance-{desktop,mobile}.png`。測試：`node tests/instructor-editor-performance.cjs`。

本交付使用 HTML、CSS、JavaScript；包含訪客、學員、Instructor、Program Leader、Admin、登入／註冊／個人資料流程。無需安裝前端框架。

## 開啟

直接打開 `index.html`。建議用 `node server.cjs` 啟動，開啟 `http://127.0.0.1:4185`，方便往後連接 ASP.NET。這個 Node 檔案只提供靜態檔案，不是業務後端。

登入頁可按示範角色，或使用以下帳號；密碼均為 `Demo1234!`：

| 角色 | 帳號 |
|---|---|
| Learner / Register User | alex |
| Instructor | john |
| Program Leader | sarah |
| Admin | admin |

Unregister User 是未登入的訪客狀態，不需要另一個帳號。

## Register User 主頁更新

學員的 `#/dashboard` 已按 Figma `250:466` 重做，保留藍色玻璃小卡、青藍鋁箔課程卡、Global 及底部 Continue 卡片。左上角選單可開啟完整功能導覽。完成率按已完成活動／全部已報讀且公開課程活動計算；姓名、課程、公告和 Session 使用同一份假資料。Continue 會前往下一個未完成活動。

Global 訊息可發送、重新整理後保留，History 可查看完整記錄；AI 使用原有的假回覆；Join Now 接入原有 Session 示範。右側 Global 可關閉及重開。手機版會改成直向排列。原設計的每週曲線仍是裝飾圖片，並非活動統計圖。

新增 `globalMessages` 資料及 `globalMessage` command，細節見 `API-CONTRACT.md`。後端接手時可沿用目前的 `SkillAPI.request()` 邊界。畫面主程式是 `js/learner-dashboard.js`，原圖層模板是 `js/learner-template.js`，樣式是 `learner-dashboard.css`。無需執行建置指令即可使用。

## 主要演示流程

課程詳情／Enroll 頁已按 Figma `262:6504` 更新。從首頁課程卡或課程目錄點擊，即會開啟對應的 `#/course/{id}`。未報讀可 Enrol；報讀後 Continue 會進入下一個未完成活動。章節列可展開，未解鎖活動會顯示原因；公開文章可預覽。評價區會按目前課程的假資料計算平均分及星級比例，也可切換至所有課程，並使用原有評價／回覆操作。未登入訪客從 Enrol 入口登入後會返回選中的課程。

這次未新增後端 command。主程式是 `js/enroll-page.js`，原圖素材模板為 `js/enroll-art.js`，樣式是 `enroll-page.css`。圖片在 `assets/enroll/`，其他角色頁面和共用假資料仍沿用原本的 API adapter。

1. Instructor 建課，新增文章、實作題、測驗或課程檔案，提交審批。
2. Program Leader 在審批看板查看課程，批准、要求修改或拒絕。課程批准後才會出現在學員課程目錄。
3. Learner 報讀、依次完成活動；實作題會驗答案，測驗會計分。完成全部活動後可查看／列印證書。
4. Instructor 查看學生完成率、答錯的主題和嘗試紀錄；Program Leader 的完成率與報表同步更新。
5. Admin 查看使用者詳情，修改角色／啟用狀態，批准 Instructor、查看稽核和管理討論內容。

## 已補齊的導覽及細節

- 桌面左側導覽可收合；手機選單、遮罩及 Escape 關閉。
- 訪客首頁、課程、About、Instructor、FAQ、Contact、登入及註冊均有目的頁。
- Admin 使用者資料／角色權限／學習紀錄／稽核分頁；另有角色說明及活躍使用者頁。
- Program Leader 審批看板與表格、搜尋、內容審批及意見；Program overview 從實際假資料計算。
- 聊天聯絡人搜尋、未讀數、傳送、標為已讀；可切換示範角色回覆。
- 帳號偏好、通知選項、地址、個人資料及外觀會保存；免費課程的 Learning access 顯示 $0。
- 程式處理載入、空狀態、驗證、失敗、重新嘗試及權限限制。

## C# 後端接入

### Instructor Dashboard 更新

John 的 `#/dashboard` 已按 Figma `283:838` 更新：左側白色導覽、綠色招生圖、淺色完成率／平均成績卡、按 Program 分組的 Course，以及右側助理。統計只計算該 Instructor 被分派的課程。學生數是去重人數；完成率是各報讀紀錄完成率的平均；成績是全部測驗嘗試的平均。沒有嘗試時顯示「—」。綠色柱圖按最近 30 天的報讀日期計算；兩條原始曲線和右側 182 保留為裝飾素材，並非歷史走勢或實際數字。

課程名稱／右下角可開編輯頁；學生頭像可開該課程的學生名單，搜尋也維持在該課程內，再點 View 可看個別完成率、錯誤主題及完成類型。Program 內的 New Course 會在建立表單預選該 Program；課程限定學生名單中的 Enrol learner 會預選同一門課。資料仍經過原有 mock API，未新增寫入 command。

主程式是 `js/instructor-dashboard.js`、`instructor-dashboard.css`，素材在 `assets/instructor/`，原圖層效果在 `js/instructor-art.js`。右側助理使用假回覆，支援課程提示、草稿保存及錯誤後重試；草稿 key 為 `skillup.instructor.ai.{userId}`。手機會重排指標、課程與助理，未分派課程的 Instructor 有獨立空狀態。`tests/instructor-dashboard.cjs` 已檢查這些操作、學生完成後的數字更新及手機版。

### Leaderboard 更新

`#/leaderboard` 已按 Figma `314:915` 改成紅黑版，保留第一名白卡、藍色頭像、個人名次及每週排行榜。左側黑色欄可切換總排名／本週排名；手機會把右側資料移到排名列表下方。姓名、日期和分數來自假資料，不會使用圖中的重複 134 分。總分仍是每門完成課程 100 分，再加每門課的最高測驗分數；本週榜只算週一以來每門課的最高測驗分數。重試不疊加積分，同分共用名次，零分顯示尚未上榜。

程式是 `js/leaderboard.js`、`leaderboard.css`，沒有新增後端寫入 command。選擇的榜單保存在 `skillup.leaderboard.period.{userId}`。目前是本機快照，所以標示 LOCAL，Refresh 會重讀資料。正式名次、週期時區及分數應由 C# 服務提供；本週資料的邊界與計算細節見 `API-CONTRACT.md`。

`tests/leaderboard-data.cjs` 有 20 個計分斷言；`tests/leaderboard-page.cjs` 驗證切換、保存、完成課程後更新、重試不重複加分、同分及手機版。五人預覽是在測試瀏覽器透過註冊及測驗 commands 建立，不會新增到你目前的帳號資料中。

### Chats 更新

`#/messages/{userId}` 已按 Frame 198（`310:561`）更新，保留原本綠色玻璃泡泡、背景、搜尋及送出圖示。左邊是可搜尋／捲動的聯絡人；右邊收到的訊息靠左，自己送出的靠右。手機版先顯示聯絡人，點入後開啟對話，可用返回箭頭回到列表。

程式是 `js/chat-page.js`、`chat-page.css`；`js/chat-art.js` 保存原始光暈圖層，可由 `scripts/build-chat-art.cjs` 重建。素材全在 `assets/chat/`。背景的 182 只是參考圖插畫，並非學生進度。名稱、頭像、內容、時間和未讀數使用同一份假資料。

傳送及已讀沿用 `message`、`readMessages` commands。收件者按 Mark as read 才更新已讀；訊息為最多 4000 字的純文字。Enter 傳送、Shift + Enter 換行。草稿按寄件者／收件者存於 sessionStorage，切換聊天及重新整理會保留，傳送成功後清除；失敗時會保留並顯示錯誤。`tests/chat-page.cjs` 已驗證雙帳號回覆、未讀已讀、搜尋、草稿、錯誤處理、安全文字顯示、保存及手機操作。現在仍是本機示範，跨裝置／即時聊天需後端及 SignalR。

### My courses／Program 切換

學員的 `#/learning` 已按 Frame 197（`296:448`）更新：左側青藍卡是 Program，可獨立上下捲動；點選後右側顯示該 Program 的公開 Course。右側卡片保留紫色素材、大編號、綠色進度圈及箭頭。進度使用目前學員的完成活動計算；未報讀課程顯示 Not enrolled，不會自動報讀。圖片可開啟課程詳情，箭頭可繼續下一個未完成活動。手機仍保留左右兩欄。

程式在 `js/my-courses.js`、`my-courses.css`。Program 順序使用 workspace，Course 先按 `courseIds`，再附加同 Program 其他已公開課程。空 Program 有提示，選擇記在 `skillup.learning.program.{userId}`，也支援鍵盤上下／Home／End。左上角選單可開啟其他系統頁面。

`js/my-courses-data.js` 以一次性遷移加入 Computer Systems、AI & Data 兩個假資料 Program 和四門短課程，方便測試切換及報讀；不會覆蓋原本資料或替學員報讀。後端接入後 workspace 直接使用服務回應，可移除這份假資料 helper。新頁面沒有新增後端 command，詳見 `API-CONTRACT.md`。`tests/my-courses.cjs` 已驗證獨立捲動、切換、選擇保存、進度更新、報讀完成、鍵盤、空狀態及手機版。

### 課程回答頁更新

`#/lesson/{courseId}/{lessonId}` 已使用 Figma `250:6405` 的 AI 球體、MARATHON、深色題卡及原有 Submit／Hint 玻璃按鈕。畫面為 `js/answer-page.js`、`answer-page.css`，11 個原圖素材在 `assets/answer/`。文章、文字實作題和選擇題均沿用原有 `complete`／`quizSubmit` commands；完成後課程、學生資料及分析會使用同一份紀錄更新。

作答草稿及練習程式暫存在目前分頁的 sessionStorage，key 為 `skillup.answer.draft.{userId}.{courseId}.{lessonId}`。測驗開始時間另存為 `skillup.quiz.start.{userId}.{courseId}.{lessonId}`，重新整理不會重設倒數；到期需要 Restart quiz，重試會清除該活動的作答草稿。這些是前端示範狀態，正式計時及嘗試次數必須由後端驗證。

左側 AI 是現有假回覆；Virtual Lab 是本機 JavaScript scratchpad，可在可終止的 Worker 執行，並非遠端電腦或 C# 編譯器。`tests/answer-page.cjs` 已驗證實作題錯誤／正確答案、Hint、AI、草稿、程式輸出、文章完成、測驗計時／到期／重試／計分及平板手機排版。

所有資料變動經過 `SkillAPI.request(action, payload)`，集中在 `js/api.js`。頁面不直接存取資料庫。

1. 實作 `API-CONTRACT.md` 的 workspace 回應與 commands。
2. 將 `js/api.js` 的 `config.mode` 改成 `http`，設定 `baseUrl`。
3. 由 C# 服務提供登入 Session、每個角色可讀的資料、驗證、計分、審批及檔案儲存。
4. 取代本機 AI 範本、重設密碼連結與聊天／課堂模擬。

現在的資料存在同一個瀏覽器的 localStorage。不同瀏覽器、不同來源／連接埠不共用資料。現有瀏覽器中的舊假資料會保留；可用 Admin → Profile → Reset demo data 還原。

通知偏好在示範中會保存；實際郵件／推播需後端接入。登入、授權、預期答案都屬於前端模擬，不能直接用於正式帳號。課堂功能是加入／離開與本機攝影機預覽，未提供多人視訊傳輸。

## 設計對照

六個頁面均已讀取 Figma 結構；指定畫面取得設計內容與截圖。新建前端是響應式功能實作，補齊原本空白的導覽和功能，沒有宣稱整個檔案逐像素一致。請見 `DESIGN-SOURCES.md` 的實際對照。

## 驗證

- `node tests/domain.cjs`：41 個業務斷言。
- `node tests/smoke.cjs`：51 個角色路由、公開頁及手機溢位檢查。
- `node tests/workflows.cjs`：建課到證書的完整跨角色流程。
- `node tests/role-completion.cjs`：60 次新功能路由訪問，導覽、偏好保存、聊天、權限、審批、檔案及手機操作。

瀏覽器測試的 Playwright／Edge 路徑為本機環境；交接後可改為團隊自己的安裝路徑。應用本身不依賴 Playwright。
