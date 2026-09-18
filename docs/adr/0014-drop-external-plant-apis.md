# 刪掉未使用的外部植物 API

ADR-0008 曾保留 Trefle／iNaturalist／GBIF 呼叫但不走產品主路徑。實作上該 adapter 已無產品 caller，只是一個假 seam。我們刪掉外部植物 adapter 及其 expander／merger；AI 補足永遠整份重寫，不再經過 Sync／merge。若日後真的要接回外部源，再加第二個 adapter。
