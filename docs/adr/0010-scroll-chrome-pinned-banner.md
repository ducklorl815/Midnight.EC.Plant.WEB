# 首頁滾動態殼層與釘死 Banner

首頁（與拼圖預覽）要像參考站：頂欄疊在 Banner 主視覺上透明淺色字，滾過 Banner 底緣後改白底深色字並固定。Banner 是殼層下的首屏主視覺，不可關閉、不可拖到其他模組之前；編輯以換圖為主，文案／CTA 屬進階。底緣大弧切邊在**右下角**。頁腳白底細線。預設去米色：紙色白、CTA 白底深绿字；透明頂欄用純白字並加強 Banner 頂緣深色遮罩。內頁可保留極淺綠灰溝槽。

## Considered Options

- 把 Banner 圖設成 Header 的 `background-image`：語意錯、難與模組拼圖共存
- Banner 可自由重排／開關：滾動態殼層失去錨點，透明頂欄行為難定義
- 僅首頁有滾動態、預覽不模擬：編輯時看不到真實效果
- 全站改純白含拿掉極淺綠灰溝槽：對比過平；定案保留溝槽、只滅米色

## Consequences

`PageComposerService.Normalize` 強制唯一 Banner 於 order 0 且 `Enabled=true`；拼圖編輯器隱藏 Banner 開關／刪除／拖移。殼層頂欄一進頁即 fixed（預覽為 absolute＋scroll 同步），過 Banner 後只改白底樣式。首頁 Header／Banner 背景與主視覺全寬穿過殼層左右溝槽；其餘模組仍限殼層寬。內容模組間距由 `.pc-shell` 的 `gap: var(--pc-module-gap)`（約 4–6rem）統一負責，避免單一模組 `margin: 0` 覆寫失效；Header 為 out-of-flow 不參與 gap。色票以白紙＋品牌綠為主，避免奶油／米色裝飾底。內容模組段底 CSS 一律白（theme 名可留資料）；無圖占位淺灰，不用彩色漸層。
