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
