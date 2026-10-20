# Skill UP 前端

這裡是 .aspx/.aspx.cs Web Forms 畫面入口，以及保留現有 JavaScript 的完整前端。正式交接入口是 [../README.md](../README.md)，串接細節見 [../docs/BACKEND-HANDOFF.md](../docs/BACKEND-HANDOFF.md)。

## Web Forms 啟動
使用 Visual Studio / IIS 開啟 Home.aspx。每個主要頁面都有配對的 .aspx.cs，完整表見 ../docs/WEBFORMS-PAGES.md。資料操作仍使用既有 JavaScript，尚未改為 C# postback。

## 靜態預覽（index.html）
```powershell
cd Frontend
node server.cjs
```
開啟 http://127.0.0.1:4185 。可直接開啟 index.html，但建議 HTTP；視訊裝置預覽需要 localhost 或 HTTPS。

## 示範
登入頁提供 Learner / Instructor / Program Leader / Admin 示範角色；這只是 browser-local fixture，並非正式驗證。
資料保存在目前 origin 的 localStorage；不同 port / IIS 網址與 file:// 不共用資料。
UI 依目前 snapshot 計算數字，所以新 browser 不一定與設計截圖的數字相同。

## API 設定
修改 `js/runtime-config.js`：
- `mode: 'mock'`：目前交付預設，不需要資料庫。
- `mode: 'http'`：由 `js/api.js` 呼叫真實 API，不會在 HTTP 失敗時自動退回 mock。
- `baseUrl` 預設從 Frontend/index.html 算出上一層應用程式的 /api，支援 IIS virtual application。

API 尚未存在，請先完成 [API-CONTRACT.md](API-CONTRACT.md) 再切 HTTP。上線前必須移除 seed/demo fixtures、示範切角色和測試來電按鈕；詳見交接說明。

## 驗證
```powershell
npm test
# 瀏覽器測試才需要開發相依套件：
npm install
npx playwright install chromium
npm run test:ui
```
`npm test` 只使用 Node 內建模組；一般看畫面不需要 npm install。
若已安裝 Edge，也可設定 `BROWSER_EXECUTABLE_PATH` 為其完整路徑再跑瀏覽器測試。

`tests/handoff-ui.cjs` 是本次交接的現況檢查；其他 UI 測試含舊設計的 selector，不保證適用現在版面。業務資料測試以 `scripts/test-domain.cjs` 清單為準。
