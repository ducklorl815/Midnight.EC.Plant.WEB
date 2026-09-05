# Dapper 具體 Respo＋標準六欄表重建

資料存取改為 ERP.Web 風格：Controller → 具體 Service → 具體 Respo（Dapper），拿掉一般 Repository／Service 介面（僅保留 AI／Parser／Storage 等可替換點），並移除 EF Core。所有業務表統一 `ID(Guid)`、`Seq(IDENTITY)`、`CreateDate`、`ModifyDate`、`Enabled`、`Deleted`；以新表匯入後刪舊表完成切換。選這條路是為了可直接追蹤行為，並與既有 ERP 習慣對齊；代價是一次結構性遷移與放棄 EF 遷移鏈。
