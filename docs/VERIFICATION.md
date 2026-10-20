# 交接版本驗證

2026-10-20，Asia/Kuala_Lumpur。

| 檢查 | 結果 |
|---|---|
| Visual Studio MSBuild，.NET Framework 4.8.1，Debug/AnyCPU | 通過，已產生 SKILL UP.dll |
| 46 組 .aspx / .aspx.cs 與 Master Page | 已納入 csproj 並編譯 |
| IIS 實際請求全部 46 個 .aspx | 全部 HTTP 200，包含 server 產生的初始路由 |
| Default.aspx 開啟首頁 | 正常轉到 Frontend/Home.aspx；Friendly URLs 會再轉成無副檔名 URL |
| 47 支 runtime JS 語法、47 支入口 scripts、61 個 stylesheet | 通過 |
| CSS 使用的 29 個本機資源及入口引用 | 無缺檔 |
| 9 組 domain/data suites | 通過 |
| HTTP API adapter 契約 | 設定、GET、POST、credentials 與錯誤傳遞通過 |
| Learner / Instructor / Program Leader / Admin | 經 IIS 開啟與切換正常 |
| 已刪除的鈴鐺／通知頁 | 四種角色都已確認 |
| Learner 手機版與課程篩選 | 通過 |
| 詢問表單提交與 access-code portal | 通過；只在獨立測試 browser 儲存 fixture |
| Browser page errors | 0 |

本機 IIS 只是驗證用，測試結束會停止。由交接人重新用 Visual Studio F5 開啟。

這些結果不代表已完成 backend：目前保留 JavaScript 與 mock，資料庫、正式驗證、AI、通話及檔案服務仍待串接。
其他舊視覺 selector 測試保留供參考，未全部重跑；不要視為全測試套件通過。
