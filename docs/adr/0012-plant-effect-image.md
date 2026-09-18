# 植物照片效果圖生成

在「設定 → 首頁拼圖 → 植栽細節牆」對封面生成效果圖，確認後「置換」寫入該盆 `slideFrames.effectImageUrl`，儲存拼圖後首頁左圖改顯示效果圖；可「還原原圖」。

## Decision

- 入口在拼圖編輯器，不在首頁輪播／詳情頁自動露出生成鈕。
- 置換只影響細節牆顯示，不覆蓋 `PlantImage` 相簿原檔。`CoverImagePath` 永遠是相簿封面。
- 左圖只認已置換 URL：無置換則封面。已生成但未置換、或已還原原圖，都不得用 latest 資產預填首頁。
- 同步呼叫 OpenAI Images Edits；僅使用者按「生成效果圖」才計費。生成失敗不寫成功資產，左圖保持封面或既有置換。
- 效果圖即 OpenAI 底圖存檔。不做 overlay／compose、不烙知識卡進 PNG。
- 不做雙層取景（已廢止 `zoom`／`focus*`／`sourceZoom`／`sourceFocus*`）。
- 版面固定左圖右文；輪播導覽為左圖欄底部圓點。首頁與拼圖預覽同一套。
- 儲存須保留 `slideFrames`；顯示解析由效果圖 module 的 `ResolveLeftImage` 執行。

## Consequences

需已執行 `docs/sql/PlantEffectImage.sql`。本 ADR 覆蓋先前「雙層取景」與「合成成品」段落。
