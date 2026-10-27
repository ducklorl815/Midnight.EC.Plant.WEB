# 照護週期 module 與 CareGuide 正名

狀態：照護週期催辦已由 ADR-0017 取代。CareGuide 正名仍有效。

## Context

澆水／施肥的倒數與逾期曾散落在植栽清單、通知提醒、Dashboard、細節牆與 Controller，四份 Infer 語意不同，且 Dashboard 用 `elapsed ≥ interval` 會早一天催。同時 `Models.External`／`External*` 類型在刪掉外部植物 API（ADR-0014）後仍暗示假 seam。

## Decision

1. 新增 in-process **照護週期**（`CareCadence`）：`Compute(today, last?, interval?)` 與 `InferWateringIntervalDays`。逾期釘 CONTEXT：`(today − last) − interval > 0`。無上次日期或無效週期 → 不算逾期。Infer 推不出 → null（不默認 7）。施肥無 profile／盆用肥料間隔 → null（不默認 30）。`SyncReminders` 仍屬 Reminder module，due 改走照護週期；無 last／interval 則撤銷該提醒。
2. 類型正名：`Models.CareGuide`；`CareKnowledgeSnapshot`／`CareKnowledgePartial`／`SpeciesCandidate`；`CareGuideHtmlBuilder`。刪零 caller 舊形狀與 `ExternalPlantApiProvider`；Sync log `Provider` 改 `string`（見 `docs/sql/platform/migrations/PlantKnowledgeSyncLog_Provider_nvarchar.sql`）。DB 欄位 `PlantKnowledge.ExternalCareGuide` 暫留，snapshot 用 `CareGuideHtml` 對應。

不重開 ADR-0013：通知提醒第一版仍只展示單一施肥逾期切片。

## Consequences

清單／通知／Sync／Dashboard 共用同一公式，測試釘在 `CareCadence` interface。讀碼不再撞到 External 假 seam；既有 Sync log int 列需跑遷移。
