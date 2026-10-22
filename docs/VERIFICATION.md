# 交接版本驗證

2026-10-21，Asia/Kuala_Lumpur。

| 檢查 | 結果 |
|---|---|
| Visual Studio MSBuild，.NET Framework 4.8.1，Debug/AnyCPU | 通過，0 errors、0 warnings，已產生 SKILL UP.dll |
| 46 組 .aspx / .aspx.cs 與 Master Page | 已納入 csproj 並編譯 |
| IIS 實際請求全部 46 個 .aspx | 全部 HTTP 200，包含 server 產生的初始路由 |
| Default.aspx 開啟首頁 | 正常轉到 Frontend/Home.aspx；Friendly URLs 會再轉成無副檔名 URL |
| 47 支 runtime JS 語法、47 支入口 scripts、62 個 stylesheet | 通過 |
| CSS 使用的 29 個本機資源及入口引用 | 無缺檔 |
| 9 組 domain/data suites | 通過 |
| HTTP API adapter 契約 | 設定、GET、POST、credentials 與錯誤傳遞通過 |
| Learner / Instructor / Program Leader / Admin | 經 IIS 開啟與切換正常 |
| 已刪除的鈴鐺／通知頁 | 四種角色都已確認 |
| Learner 電腦版與課程篩選 | 通過；目前只支援桌面瀏覽器 |
| 詢問表單提交與 access-code portal | 通過；只在獨立測試 browser 儲存 fixture |
| Browser page errors | 0 |

本機 IIS 只是驗證用，測試結束會停止。由交接人重新用 Visual Studio F5 開啟。

這些結果不代表已完成 backend：目前保留 JavaScript 與 mock，資料庫、正式驗證、AI、通話及檔案服務仍待串接。
其他舊視覺 selector 測試保留供參考，未全部重跑；不要視為全測試套件通過。

## 電腦版支援範圍

2026-10-21 已重新驗證。

- 已移除手機 Master Page、ViewSwitcher、手機專用模板、樣式與預覽圖。
- 建議視窗寬度至少 1280px；小視窗保留至少 1024px 的電腦版畫布。
- 訪客首頁與四種角色在 1280px、1440px 的共 10 組版面比較通過；主要元素位置保持一致。
- 窄視窗及手機 User-Agent 均保留電腦版，不再切換手機 Master Page。
- 目前維護的 handoff-ui 測試已改為電腦版；歷史視覺測試仍只供參考。

## 最後重驗（2026-10-21）

- 全部 49 個入口回傳 HTTP 200（46 個 Frontend 頁面及 3 個根目錄頁面）；12 組查詢參數路由的編碼與初始路由正確。
- 使用 Visual Studio 實際 IIS 設定，在 HTTPS 下完成四種角色共 93 次路由巡覽；JavaScript、Console、HTTP 資源錯誤和 1280px 橫向溢出均為 0。
- 建立課程 → 加入活動 → 送審 → 批准 → 報名 → 完成 → 證書，以及 lab 答案修正、quiz 評分、個人資料持久化、失敗儲存回復和 Instructor 帳號批准通過。
- 9 組 domain suites、HTTP adapter 契約、desktop handoff UI 與 10 組桌面版面比較通過。額外解析全部 63 份前端 CSS、106 份 JavaScript／工具／測試檔及 6 份入口／交接 JSON，沒有語法錯誤。
- 修正本機啟動連接埠：原 HTTP 58654 落入 Windows 保留範圍，專案與本機 IIS 設定改為 51872；HTTPS 44353 保留。HTTP／HTTPS 的 Default.aspx 都能正常轉到 Frontend/Home。

測試使用獨立 browser context，不會修改日常瀏覽器中的 demo 資料。結果只涵蓋現有 Web Forms、JavaScript 及 mock 功能，不能視為未串接 backend 的正式整合測試。

## Admin 頁面移除重驗（2026-10-22）

- 已刪除 Admin Platform reports、Pending verifications 和 Roles & permissions，清理主導覽、更多選單、快捷入口和相關連結。
- Roles.aspx／Verification.aspx 及各自 code-behind 已刪除；檔案入口及無副檔名入口均回傳 404。舊 hash 路由會導回有效頁面。
- 最新 Web Forms 入口數為 44 組，全部實際請求 HTTP 200；前面的 46／49 個入口數屬於刪除前的重驗紀錄。
- 重新編譯通過：0 errors、0 warnings。9 組 domain suites、HTTP adapter 與四種角色 handoff UI 通過。
- Instructor／Program Leader 的 Learning analytics 正常；User accounts 中的帳號批准和每個帳號的角色選項正常。
- Browser、Console 和非預期 HTTP 錯誤為 0。測試使用獨立 browser context。

詳細結果：[admin-page-removal-verification.json](admin-page-removal-verification.json)。

## Active users／Activity & audit 頁面移除重驗（2026-10-22）

- 已刪除 ActiveUsers.aspx、ActivityLogs.aspx 及 code-behind，清理導覽、路由與失效連結。兩組實體網址（含無副檔名）均為 HTTP 404。
- 最新 Frontend Web Forms 頁面為 42 組，全部請求 HTTP 200；舊 hash 對四種已登入角色導回 Overview，訪客導向 Log in。
- Visual Studio MSBuild 通過，0 errors、0 warnings；9 組 domain suites、HTTP adapter 契約、四角色 desktop handoff UI 通過。
- Admin overview 中的活動紀錄、搜尋與 CSV 匯出、User accounts 導覽，以及學員 Activity.aspx?course=c1&lesson=l1 入口正常。
- Browser 與非預期 HTTP 錯誤為 0；測試使用獨立瀏覽器資料。

詳細結果：[admin-activity-removal-verification.json](admin-activity-removal-verification.json)。

## Discussion moderation／Enquiries & feedback 移除重驗（2026-10-22）

- Moderation.aspx、Support.aspx 及 code-behind 已刪除，移除 Admin 導覽、更多選單及 Dashboard 相關入口。實體與無副檔名網址均回傳 HTTP 404。
- 最新 Frontend 頁面入口為 40 組，全部請求 HTTP 200。舊 #/moderation、#/support 對四種已登入角色轉回 Overview；訪客轉到 Log in。
- Visual Studio MSBuild 通過（0 errors、0 warnings）；9 組 domain suites、HTTP adapter 契約和四角色 desktop handoff UI 通過。
- 訪客 enquiry 提交及個人 enquiry portal、一般課程討論和講師原有的討論操作、User accounts 保留且正常。
- Browser 與非預期 HTTP 資源錯誤為 0；測試使用獨立瀏覽器資料。

詳細結果：[admin-moderation-support-removal-verification.json](admin-moderation-support-removal-verification.json)。

## Virtual Lab 終端機介面（2026-10-22）

- 使用單一深色終端機：上方顯示實際活動題目，中間為 command 輸入，下方為結果與分段 execution loading 條。
- loading 依 Worker 的實際執行生命週期顯示；支援成功、語法／執行錯誤、2 秒逾時、手動停止及切換頁面中止。
- command／output／history 分頁、鍵盤執行、重設、清除、8 筆歷史及分活動草稿正常；訪客維持唯讀預覽。
- 1280／1440 電腦版排版、等寬字體、減少動態效果通過；保留既有 Submit／Hint 圖案及交答案流程。
- MSBuild、9 組 domain suites、HTTP adapter、四角色 handoff UI、answer-page 既有回歸與新的 terminal-lab 測試通過；瀏覽器程式錯誤為 0。

詳細結果：[terminal-lab-verification.json](terminal-lab-verification.json)。新的可重跑測試為 Frontend/tests/terminal-lab.cjs；本機 IIS URL 可由 SKILLUP_BASE_URL 指定。

## User Management 課程編輯版型（2026-10-22）

- 編輯帳號採課程編輯頁的配置：左側緊湊摘要卡、右側膠囊分頁及三欄表單，頂部 Save，表單下方 Save changes／Discard changes。
- 角色及帳號狀態改為一般選單；帳號存取操作、最近帳號活動與刪除操作沿用現有資料與命令。建立帳號維持先前的 Information／Account 行式表單。
- 帳號資料、唯讀 ID、草稿／還原／失敗儲存、重複資料驗證、建立／批准／停用／恢復／刪除、講師指派及自身 Admin 保護、Activity Logs／Learning activity 與權限路由測試通過。
- 1280／1440 電腦排版、account domain、靜態檢查、MSBuild 及四角色 handoff UI 通過，瀏覽器程式錯誤為 0。

詳細結果：[account-management-reference-verification.json](account-management-reference-verification.json)。可重跑 Frontend/tests/user-editor.cjs；本機網址以 SKILLUP_BASE_URL 指定。

## 全域側邊選單與獨立頁面移除（2026-10-22）

- 已移除全域 sidebar／drawer 與 Open all pages 按鈕；四種角色的有效入口改由頂部主導覽與 More 選單提供，保留 Profile、Help、Sign out 與切換 demo role。課程摘要、Profile 及 Messages 的頁內欄位維持原有功能。
- 已刪除 Assistant.aspx、Discussions.aspx、LiveSessions.aspx、Settings.aspx 與各自 code-behind，清理專案編譯項目、hash renderer、權限與導覽／個人頁連結。實體與無副檔名網址均為 HTTP 404；舊 hash（含 Settings 子路徑）導回 Overview，未登入者導到 Log in。
- 目前 36 組 Frontend Web Forms 頁面全部 HTTP 200。Profile 的資料編輯／密碼操作、活動內 AI companion／terminal、Messages 及原訊息通話按鈕保留且正常。
- MSBuild、9 組 domain suites、HTTP adapter、四角色 handoff UI、account editor 與新增 sidebar-page-removal 瀏覽器測試通過；1280／1440 無橫向溢出，瀏覽器程式錯誤為 0。

詳細結果：[sidebar-pages-removal-verification.json](sidebar-pages-removal-verification.json)。重跑測試為 Frontend/tests/sidebar-page-removal.cjs，網址以 SKILLUP_BASE_URL 指定。

## 更正：只移除 sidebar，保留 navbar 與原功能（2026-10-22）

- 已恢復 Assistant、Discussions、LiveSessions、Settings 四組 .aspx／.aspx.cs、renderer 及原 hash 路由，現有 40 組頁面入口。這項更正取代上一節四個獨立頁面的移除決定。
- 全域 sidebar／drawer 維持移除。主導覽保留原位置，其餘功能在 navbar 的 More 選單可存取，包括 Discussions／Live sessions／AI companion／Account preferences。
- 四種角色的導覽、獨立 AI 回覆、討論建立、Settings 密碼與地址分頁、課程內 AI／command、1280／1440 電腦排版均通過。Activity Logs 左側摘要卡維持移除。
- 四組實體及無副檔名網址全部 HTTP 200；MSBuild、靜態資源檢查與四角色 handoff UI 通過，瀏覽器程式錯誤為 0。

最新結果：[navbar-functions-verification.json](navbar-functions-verification.json)。重跑 Frontend/tests/navbar-functions.cjs；舊 sidebar-page-removal.cjs 也會轉而執行這組保留功能的檢查。
