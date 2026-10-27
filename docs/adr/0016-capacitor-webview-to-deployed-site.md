# 手機殼開啟已部署的網站

植栽的畫面與資料在網站與 SQL 上。Study 的手機把靜態頁烤進 APK、不連伺服器；植物若照做，就要改掉資料庫與全部畫面。

我們決定用 Capacitor Android 殼，以 `npm run app` 產出 APK，WebView 打開已部署的植栽網站。相機走殼的原生拍照。網站一更新，手機裡的內容跟著變。沒有網路就打不開植栽。

## Considered Options

- 把前端烤進 APK、不連網站（Study 手機的做法；與現有網站不合）
- 做電腦版 Electron 自更新（Study 有，但這次要的是手機 APK）

## Consequences

殼只負責原生能力與安裝包；照護資料仍在網站。內容更新不靠重裝 APK。
