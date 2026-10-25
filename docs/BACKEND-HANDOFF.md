# Backend 交接說明

更新日期：2026-10-20（Asia/Kuala_Lumpur）。這次交付是**Web Forms .aspx/.aspx.cs 檔案結構＋既有 JavaScript 功能**，不是已完成的 backend。

## Lecturer 要求的檔案格式
本次確認只要求 .aspx + .aspx.cs 結構，既有 JavaScript 可以保留。每個主要畫面都有一組入口，例如：
- Frontend/Login.aspx + Login.aspx.cs
- Frontend/Dashboard.aspx + Dashboard.aspx.cs
- Frontend/Course.aspx?id=c1 + Course.aspx.cs
- Frontend/Activity.aspx?course=c1&lesson=l1 + Activity.aspx.cs

畫面共用 SkillUp.Master；C# Page_Load 設定該頁的初始路由。**頁內導覽和表單操作仍由 JavaScript / SkillAPI 處理，不是 asp:Button postback 改寫。**
各 .aspx.cs 已有真正可編譯的 Page_Load，後端同事可接 service；尚無資料庫和正式登入服務。
Master 的 designer.cs 是 Web Forms 控制項宣告輔助檔，不是第三套業務邏輯。
.NET 頁面必須經 IIS/Visual Studio 執行，不能直接雙擊 .aspx。index.html 只保留為不需 .NET 的獨立前端預覽。
同資料夾放置這些頁面是為了保留現有相對素材路徑與 hash 導覽，沒有用 iframe。
完整入口清單見 [WEBFORMS-PAGES.md](WEBFORMS-PAGES.md)。

## 先看這些檔案
1. `Frontend/js/runtime-config.js`：mock / HTTP、API base URL。
2. `Frontend/js/api.js`：所有頁面共用 `SkillAPI.request(action, payload)`。
3. [API 契約](../Frontend/API-CONTRACT.md)：DTO、指令 payload、權限和交易規則。
4. [模組對照](MODULE-MAP.md)、[路由清單](ROUTES.json)、[指令清單](COMMANDS.json)。
5. `Frontend/js/seed.js`、`*-data.js`：只作 mock DTO 參考，不是資料庫或正式帳號。

## 要實作的 HTTP 邊界
- `GET /api/workspace` → 直接回傳目前身分有權看到的 workspace JSON。
- `POST /api/commands/{action}` → `{ data: resultOrNull, state: updatedWorkspace }`。
- 失敗使用正確 HTTP status，JSON 可含 `detail` 或 `message`。目前 adapter 不會把 HTTP 失敗當成功。
- 送出 request 使用 `credentials: include`。請在服務端實作 cookie/session 驗證與授權，並在這一個 adapter 加團隊的 anti-forgery header。
- 訪客的 state 也需存在，例如 `currentUser: null` 與可公開課程清單。
- 日期用 ISO 格式；只含日期用 YYYY-MM-DD；活動時間請回傳 offset，排程時區明確使用 Asia/Kuala_Lumpur。

## 建議接線順序
1. DTO / database schema、登入、登出、註冊、忘記密碼及角色授權。
2. role-scoped workspace、課程、enrolment、lesson completion、quiz attempt。
3. Instructor 編輯與送審、Program Leader 審批／刪除申請，保留審批事件與歷史。
4. 訊息、inquiry CRUD、檔案儲存、課程公告。
5. Admin 帳號、統計與 audit logs，最後再接真正的 AI 和通話服務。

## 目前角色範圍
| 角色 | 頁面與主要流程 |
|---|---|
| Visitor | 首頁、課程介紹、登入／註冊、FAQ、建立 inquiry 與 access-code portal |
| Learner | Dashboard、My courses、Explore courses、活動與測驗、Leaderboard、訊息、個人資料 |
| Instructor | 自己的課程／活動／測驗、requests、學員表現、資源、訊息 |
| Program Leader | 管理的 programs、課程順序與指派、送審／刪除申請決策、學習分析 |
| Admin | 帳號與驗證、平台資料、所有角色的 user activity logs、support inquiry |

## 本次 UI 現況
- 所有角色鈴鐺、獨立 notification/announcements 頁、notification 設定分頁已移除。
- 舊 `#/announcements`、`#/notifications` 回到 dashboard；guest 回 home。
- 課程公告資料仍保留。Dashboard 內公告只是摘要，不再連到被移除的頁面。
- `#/paths` 已移除；staff programs 的課程排序並未刪除。
- Learner dashboard 是紫黑背景、黑色指標卡和 Active Courses 清單。
- Inquiry 底部是簡潔 Send enquiry 按鈕，送出成功會顯示 ID/access code。
- Navbar 的 Test video call / Receive call 目前仍是使用者要求保留的臨時預覽按鈕。

## 需要接 backend 的地方
- Mock DB 是 localStorage，不是多人共享資料、也不是正式 session。
- Seed 中含示範密碼、lab expected answer 和 quiz answer；正式發佈必須移除/分離這些 fixture scripts，不能只把 mode 改成 http 就上線。api.js 的 mock 初始化也需隔離。
- UI 會過濾部分資料，但資料的真正可見性、 ownership、排序與權限都必須由服務端重新檢查。
- Quiz 評分與 lab 通過判斷需由服務端執行，不信任前端計算。AI 目前是規則式範例回覆。
- 通話只模擬來電／本機裝置畫面，未實作 signaling、遠端媒體、會議房間或即時在線狀態。上線前關閉 `js/workspace-nav.js` 的 `SHOW_INCOMING_CALL_TEST` 並接正式服務。
- 資源／頭像目前可用 data URL。正式檔案需要儲存服務、檔案類型／大小限制及存取權限。
- Inquiry 的 ID/access code 是存取憑證；lookup/update/delete 都要在服務端驗證 token。保留既有 browser 只列出本機保存憑證的 UI 行為，不可公開列出所有詢問。
- leaderboard-demo-data.js、my-courses-data.js、xp-history-data.js、request-history-data.js 是示範／回填 fixture，不能寫進正式使用者的真實歷史。
- 重置密碼、email、註冊審核通知、AI、通話、正式檔案儲存都尚未串接。
- 檔案中的設計來源紀錄不構成第三方素材授權保證；正式發佈時需由團隊確認素材權利。

## 程式結構與改動方式
入口集中載入頁面腳本與樣式。為保持目前畫面，交接保留原載入順序及 CSS 檔名，不在搬移過程做大規模 bundle/refactor。
新增 runtime-config 把環境切換獨立出來。API adapter 是唯一 transport boundary，請避免在每個頁面各自加入 fetch。
目前採 snapshot/command 架構；若團隊改 REST controller，可在 adapter 內轉換，無需重寫整套 UI。

## 開發與測試
- Visual Studio F5：從 Default.aspx 轉到 Frontend/Home.aspx。
- 靜態模式：在 Frontend 執行 `node server.cjs`，只提供靜態檔案，沒有 C# API。
- `npm test`：相對路徑檢查、JS 語法、domain suites、HTTP adapter 契約測試。
- `npm run test:ui`：安裝開發相依套件和 Playwright Chromium 後，檢查各角色、已刪除通知頁、inquiry 與電腦版排版。
- 原 UI 截圖測試保留在 tests，但部分屬於較早設計，可能需更新 selector；不要把它們全數標為已通過。
- `scripts/` 中依 Figma 匯出來源重建素材的工具是開發參考；執行前確認對應 design input 存在，避免覆蓋目前客製 UI。
- 通用 browser 工具已移除原電腦的 Playwright/Sharp module path，改用 package.json 開發依賴；可用 BROWSER_EXECUTABLE_PATH 指定已安裝瀏覽器。

## 原專案保護與交付
既有 C#、NuGet 與 Web Forms 範本保留。新增每頁 .aspx/.aspx.cs 與共用 Master Page；修改 Default.aspx.cs 啟動轉址、.csproj content/compile items，以及本機啟始頁設定。
原整合檔備份放在 App_Data/HandoffBackups（不提供 Web 存取）；舊的前端視覺歷史文件在 docs/history。
不要把 .vs、bin、obj、node_modules、測試輸出或本機備份當作 backend source 交付。

## 電腦端支援範圍
目前只提供桌面瀏覽器版本，建議寬度 1280px 以上。窄視窗保留至少 1024px 的桌面畫布。手機專用母版、切換控制項、頁面模板、預覽圖片與手機版 CSS 已移除；一般電腦視窗調整、列印和減少動態效果仍保留。

## Admin 頁面移除（2026-10-22）

Platform reports、Pending verifications、Roles & permissions 已移除，相關 Admin 導覽及報表連結已清理。Roles.aspx／Verification.aspx 與其 code-behind 已刪除，入口清單剩 44 組。使用者帳戶的角色及啟用操作仍在 User accounts 中；Instructor／Program Leader 的 Learning analytics 使用共用 Analytics.aspx。舊 Admin hash 連結會轉回有效頁面。

## Admin 活躍使用者與稽核頁面移除（2026-10-22）

Active users 與 Activity & audit 獨立頁面已刪除，包括 ActiveUsers.aspx、ActivityLogs.aspx 及各自 code-behind、專案編譯項目、路由、導覽和失效連結。Frontend 現有 42 組頁面入口。舊的 #/active-users 與 #/activity 會導回 Overview；未登入者導向 Log in。Admin overview 內的活動紀錄及 CSV 匯出保留，原活動連結改為定位到該區。Activity.aspx 是學員課程活動入口，並非被刪除的 Admin 稽核頁，繼續保留。

## Admin 討論審核與意見頁面移除（2026-10-22）

Discussion moderation 和 Enquiries & feedback 獨立 Admin 頁面已刪除，包括 Moderation.aspx、Support.aspx、各自 code-behind、專案編譯項目及導覽。舊 #/moderation、#/support 轉回 Overview；訪客轉到 Log in。Frontend 現有 40 組頁面入口。一般課程討論與講師的討論操作、訪客提交 enquiry、個人 enquiry portal CRUD 及既有 feedback 資料仍保留；此次只移除指定的 Admin 頁面。

## Virtual Lab 終端機（2026-10-22）

活動頁的終端機由 js/answer-page.js、answer-skill.css 及 js/app.js 共同實作。題目直接使用活動 title／body；command、output、history 保留原本依使用者與活動區分的 sessionStorage 草稿。執行狀態由 .lab-shell 的 data-execution 及 aria-busy 控制，分段 loading 為不顯示假百分比的執行中動畫。

目前 Execute 使用本機 JavaScript Web Worker，可停止、逾時或切換頁面終止，並不提供遠端系統指令、MongoDB 連線或 C# 編譯。題目交答案與評分仍透過原 SkillAPI 流程。後端接 isolated runner 時應保留成功／錯誤／停止生命週期，並由伺服器獨立驗證作答與權限。Frontend/tests/terminal-lab.cjs 與 tests/answer-page.cjs 可供回歸驗證。

## 帳號編輯介面更新（2026-10-22）

User Management 的編輯與帳號紀錄頁改用課程編輯頁的摘要／表單配置，建立帳號保留行式表單。共用 js/user-editor.js 與 user-editor.css；原 userSave／userDelete API、角色／狀態選項及草稿 key 維持原契約。左側摘要與日期、最近活動由真實帳號資料及 audit 紀錄取得，未有資料時顯示 Not recorded／No activity recorded。

## 全域側欄及獨立功能頁移除（2026-10-22）

已移除 Assistant／Discussions／LiveSessions／Settings 的四組 Web Forms 頁面及獨立 hash renderer，現有頁面共 36 組。舊 hash 對已登入帳號導回 dashboard、訪客導向 login。全域 sidebar 與 drawer 不再產生；ui-refinement.js 的 SkillNavigationItems 直接提供頁面資料給 workspace-nav.js，剩餘入口、Profile／Help／Sign out 改由頂部 More 選單操作。

活動中的 AI companion、Profile 編輯與密碼功能、Messages／課程公告及訊息通話介面保留。既有 AI／討論／session／preferences 資料及 API 契約未刪除；本次移除的是指定獨立頁面與其導覽。

## Sidebar 範圍更正：功能與 navbar 保留（2026-10-22）

使用者確認只刪除全域 sidebar。Assistant、Discussions、LiveSessions、Settings 的四組 Web Forms 頁面、hash renderer 及頂部導覽入口已恢復，最新頁面數為 40；此更正取代前一節的獨立頁面移除。navbar 主入口與 More 選單提供原有功能，sidebar／drawer 不再產生。課程內 AI、Profile／Settings、訊息、Live sessions 與討論繼續沿用既有 API；Activity Logs 的左側資料卡移除維持。
