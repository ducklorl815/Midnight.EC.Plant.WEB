# Banner 主視覺：站點上傳與植栽圖引用

Banner 換圖要在拼圖編輯裡完成：可上傳到站點／拼圖專用媒體（不屬任何一盆），或從所有植栽的全部照片依盆分組選取。選盆圖時持久化 `plantId`＋`imageId`，首頁與預覽 Build 時再解析公開路徑；引用失效則回退占位並在編輯器提示「原圖已不在」。外連網址降為進階。

## Considered Options

- 上傳檔硬塞進某一盆相簿：污染單盆媒體、刪盆影響 Banner
- 選盆圖只存 URL：刪圖後難偵測，語意不像「引用」
- 禁止刪除被 Banner 引用的照片：耦合過重

## Consequences

`IImageStorageService.SaveSiteMediaAsync` 寫入 `wwwroot/uploads/page-composer/…`；`PageComposerHomeBuilder` 負責解析與照片目錄；設定頁 `UploadPageComposerBanner` 供編輯器 AJAX 上傳。
