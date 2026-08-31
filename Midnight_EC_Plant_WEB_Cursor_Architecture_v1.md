# Midnight.EC.Plant.WEB
# 植栽日記 + 植物知識 + AI Agent 分析系統
# Cursor 開發指令與架構規格 v1.0

> 文件用途：請將本文件整體提供給 Cursor，作為本專案第一階段的系統分析、架構設計與實作規範。
> 核心原則：先建立可長期擴充的 Domain / Service / Data 架構，再逐步加入外部植物資料、文章/影片解析、圖片日記與 AI Agent。

---

## 0. 專案目標

我要建立一個「個人植栽日記與 AI 植栽分析平台」。

使用者可以：

1. 建立自己的植栽。
2. 輸入植物名稱、別名或直接描述植物。
3. 系統第一次建立植物時，透過外部 API / 網路資料取得植物基礎知識。
4. 將外部資料整理、標準化後寫入自己的 DB。
5. 日後再次查看同一植物時，優先使用自己的 DB，不要每次重新呼叫外部 API。
6. 每天或不定期上傳植物照片，形成「成長日記」。
7. 上傳照片本身只代表日常紀錄，不應自動觸發 AI。
8. 使用者主動按下「AI 分析」按鈕後，才啟動 AI Agent。
9. AI 分析時可同時參考：
   - 植物基本資料
   - 植物適合環境
   - 使用者歷史照片
   - 歷史 AI 分析
   - 使用者日記文字
   - 已解析的外部文章 / 影片資料
10. AI 分析結果要保存到 DB。
11. 前端顯示「AI 分析紀錄」，使用 Collapse / Accordion 展開歷次分析。
12. 後台可以輸入文章 URL / YouTube 等影片 URL，系統解析內容後保存，避免 AI 每次重新抓取。
13. 系統未來可以擴充：
   - 澆水紀錄
   - 施肥紀錄
   - 換盆紀錄
   - 光照紀錄
   - 溫濕度
   - 病蟲害
   - 成長趨勢
   - AI 預警
   - 多植物比較

---

# 1. 現有專案架構

目前已建立：

Midnight.EC.Plant.WEB
    啟動專案 / MVC / Controller / View

Midnight.EC.Plant.WEB.Services
    類別庫
    Web 參考 Services
    商業邏輯層

Midnight.EC.Plant.WEB.Models
    類別庫
    Services 參考 Models
    DB / Entity / Repository / DTO 層

Midnight.EC.Plant.WEB.Utility
    類別庫
    Models 參考 Utility
    共用元件層

目前依賴方向：

WEB
 ↓
Services
 ↓
Models
 ↓
Utility

請不要破壞既有架構。

---

# 2. 建議責任分工

## Midnight.EC.Plant.WEB

只負責：

- MVC Controller
- View
- ViewModel
- User interaction
- File upload 接收
- API Endpoint / MVC Action
- Authentication / Authorization
- UI 顯示

不要在 Controller 寫：

- DB 查詢
- 外部 API 呼叫
- AI Prompt
- 網頁解析
- 植物分析邏輯

Controller 應保持薄。

例如：

POST /Plant/Create
    -> PlantService.CreateAsync()

POST /Plant/{id}/Diary
    -> PlantDiaryService.CreateAsync()

POST /Plant/{id}/Analyze
    -> PlantAnalysisService.AnalyzeAsync()

---

# 3. Midnight.EC.Plant.WEB.Services

這裡是整個系統核心。

建議：

Services/
├── Interfaces/
│   ├── IPlantService.cs
│   ├── IPlantKnowledgeService.cs
│   ├── IPlantDiaryService.cs
│   ├── IPlantAnalysisService.cs
│   ├── IPlantSourceService.cs
│   ├── IPlantContentParserService.cs
│   ├── IImageStorageService.cs
│   ├── IExternalPlantApiService.cs
│   ├── IAIAgentService.cs
│   └── IPlantCacheService.cs
│
├── Plant/
├── PlantKnowledge/
├── PlantDiary/
├── PlantAnalysis/
├── External/
├── ContentParser/
├── AI/
└── Background/

原則：

Controller
    ↓
Application Service
    ↓
Domain / Repository / External Adapter
    ↓
DB / API / AI

---

# 4. Midnight.EC.Plant.WEB.Models

Models 負責：

- Entity
- DbContext
- Repository
- DB DTO
- 外部 API DTO
- AI Result DTO
- enum
- database mapping

建議：

Models/
├── Entities/
│   ├── Plant.cs
│   ├── PlantSpecies.cs
│   ├── PlantKnowledge.cs
│   ├── PlantDiary.cs
│   ├── PlantImage.cs
│   ├── PlantAnalysis.cs
│   ├── PlantSource.cs
│   ├── PlantSourceContent.cs
│   └── PlantCareRecord.cs
│
├── DTOs/
│   ├── PlantDto.cs
│   ├── PlantKnowledgeDto.cs
│   ├── PlantDiaryDto.cs
│   ├── PlantAnalysisDto.cs
│   └── PlantSourceDto.cs
│
├── External/
│   ├── Trefle/
│   ├── iNaturalist/
│   └── GBIF/
│
├── AI/
│   └── PlantAnalysisResultDto.cs
│
├── Data/
│   └── PlantDbContext.cs
│
└── Repositories/

---

# 5. Utility

Utility 不要變成「什麼都丟進去的垃圾桶」。

只放真正共用的元件：

Utility/
├── Http/
├── Json/
├── Storage/
├── Hash/
├── Url/
├── Text/
├── DateTime/
└── Extensions/

例如：

- UrlNormalizer
- HashHelper
- JsonHelper
- FileExtensionHelper
- ImageMetadataHelper

---

# 6. 核心 Domain

系統不是單純「植物資料庫」。

核心概念是：

Plant
    ↓
Plant Knowledge
    ↓
Plant Diary
    ↓
Plant Images
    ↓
AI Analysis
    ↓
Plant Sources

---

# 7. Plant

代表「使用者實際養的這一盆植物」。

重要：

Plant 不等於 Species。

例如：

Species：
    玉露

Plant：
    我的玉露 A
    2026/08/01 開始養
    放在陽台
    使用者自己的備註

未來同一個 Species 可以有多盆 Plant。

建議欄位：

Plant
- Id
- Name
- SpeciesId
- NickName
- Description
- Location
- EnvironmentNote
- PurchaseDate
- StartDate
- IsActive
- CreatedAt
- UpdatedAt

---

# 8. PlantSpecies

代表植物物種 / 品種的標準化資料。

例如：

Echeveria ...
Haworthia cooperi
Ficus microcarpa

欄位：

PlantSpecies
- Id
- ScientificName
- CommonName
- ChineseName
- Genus
- Family
- TaxonId
- ImageUrl
- SourceType
- SourceId
- CreatedAt
- UpdatedAt

---

# 9. PlantKnowledge

這是「整理後的植物知識」。

不要把外部 API JSON 直接當作前端資料。

需要轉換成自己的標準格式：

PlantKnowledge
- Id
- SpeciesId
- LightRequirement
- WaterRequirement
- HumidityRequirement
- TemperatureMin
- TemperatureMax
- SoilRequirement
- FertilizerRequirement
- Dormancy
- GrowthSeason
- RepottingAdvice
- CommonProblems
- PestProblems
- DiseaseProblems
- CareSummary
- SourceUpdatedAt
- DataVersion
- UpdatedAt

未來可以再加入：

- TaiwanEnvironmentAdvice
- BalconyAdvice
- IndoorAdvice
- SummerAdvice
- WinterAdvice

---

# 10. PlantDiary

這是使用者真正的日常紀錄。

欄位：

PlantDiary
- Id
- PlantId
- DiaryDate
- Title
- Note
- WeatherNote
- EnvironmentNote
- WateringNote
- FertilizerNote
- CreatedAt
- UpdatedAt

原則：

「紀錄」和「AI 分析」必須分離。

上傳照片：
    不等於 AI 分析。

---

# 11. PlantImage

一筆 Diary 可以有多張照片。

PlantImage
- Id
- DiaryId
- FileName
- StoragePath
- ThumbnailPath
- OriginalFileName
- ContentType
- Width
- Height
- FileSize
- Sha256
- CreatedAt

不要直接把大型圖片 Binary 存在 DB。

第一階段建議：

DB：
    存 metadata + path

File Storage：
    存圖片

未來可以換：

- Local
- NAS
- S3
- Azure Blob
- MinIO

因此建立 IImageStorageService。

---

# 12. PlantAnalysis

這是 AI 分析結果。

欄位：

PlantAnalysis
- Id
- PlantId
- DiaryId
- AnalysisType
- ModelName
- PromptVersion
- InputSnapshot
- ResultJson
- Summary
- HealthScore
- Confidence
- CreatedAt

AnalysisType：

- General
- Health
- Growth
- Watering
- Disease
- Pest
- Environment

重要：

AI 分析必須保存「當時使用的資料快照」。

因為未來 PlantKnowledge 會更新。

如果只存結果，未來無法知道 AI 當初是根據什麼資料判斷。

---

# 13. PlantAnalysis 的 AI 結果格式

不要只存一大段文字。

AI 優先輸出 JSON：

{
  "summary": "",
  "healthScore": 0,
  "observations": [],
  "possibleIssues": [],
  "environmentAssessment": {
    "light": "",
    "water": "",
    "humidity": "",
    "temperature": ""
  },
  "recommendations": [],
  "warning": [],
  "confidence": 0,
  "needsHumanReview": false
}

前端再把 JSON Render 成漂亮 UI。

---

# 14. PlantSource

代表外部資料來源。

例如：

- Trefle
- iNaturalist
- GBIF
- 某個植物網站
- 部落格
- YouTube
- 官方植物園
- 農業單位

欄位：

PlantSource
- Id
- SpeciesId
- SourceType
- Title
- Url
- Domain
- Author
- PublishedAt
- Language
- ContentHash
- IsActive
- CreatedAt
- UpdatedAt

SourceType：

- PlantApi
- OfficialWebsite
- Article
- Video
- Blog
- YouTube
- Forum
- SocialMedia
- Other

---

# 15. PlantSourceContent

這是「解析後的內容」。

不要每次 AI 都重新抓 URL。

欄位：

PlantSourceContent
- Id
- SourceId
- RawText
- CleanText
- Summary
- Keywords
- ParsedJson
- ParserType
- ParserVersion
- ContentHash
- ParsedAt
- UpdatedAt

流程：

URL
 ↓
下載
 ↓
解析
 ↓
清理 HTML
 ↓
抽取正文
 ↓
產生 CleanText
 ↓
Summary
 ↓
Keywords
 ↓
寫入 DB
 ↓
未來 AI 直接使用 DB

---

# 16. 外部植物 API

第一階段評估：

## Trefle

Trefle 是植物資料 REST API，可取得植物基本資訊，包含植物分類與部分栽培資訊。

文件：
https://docs.trefle.io/

可作為：
「植物基本資料來源」。

注意：
不要把 Trefle 當成唯一真實來源。

---

## iNaturalist

iNaturalist API 適合：

- 植物辨識相關資料
- Taxon
- Observations
- Photos
- 地理觀察資料

官方 API：
https://www.inaturalist.org/api

新 API：
https://api.inaturalist.org/

應遵守官方 rate limit。

---

## GBIF

GBIF 適合：

- Taxonomy
- Biodiversity
- Occurrence
- Species
- 分類資料

API：
https://techdocs.gbif.org/en/openapi/

定位：

GBIF = 標準化生物多樣性資料

Trefle = 植物園藝 / 植物資訊

iNaturalist = 實際觀察 / 照片 / 辨識

三者不要混成一張外部 API Model。

---

# 17. External API Adapter

不要：

PlantService -> Trefle API

應該：

PlantService
    ↓
IExternalPlantApiService
    ↓
TreflePlantApiService

未來：

IExternalPlantApiService
    ├── TreflePlantApiService
    ├── INaturalistPlantApiService
    └── GbifPlantApiService

建立：

ExternalPlantApiProvider

- Trefle
- iNaturalist
- GBIF

未來可以加入其他 API 而不影響 PlantService。

---

# 18. 外部 API Cache

這是本系統的重要原則：

不要：

使用者開頁面
    ↓
每次呼叫外部 API

應該：

第一次：
API
 ↓
Normalize
 ↓
DB

之後：

DB
 ↓
顯示

只有：

- 資料不存在
- 手動更新
- Cache 過期
- Admin 強制同步

才呼叫外部 API。

建議：

PlantKnowledgeSyncLog
- Id
- SpeciesId
- Provider
- RequestUrl
- Status
- ResponseHash
- StartedAt
- CompletedAt
- ErrorMessage

---

# 19. 外部文章 / 影片解析

後台功能：

/Admin/PlantSources/Create

輸入：

URL
標題（可選）
來源類型（可選）

按：

「解析」

系統：

URL
 ↓
UrlNormalizer
 ↓
SourceTypeDetector
 ↓
ContentFetcher
 ↓
ContentParser
 ↓
CleanText
 ↓
ContentHash
 ↓
DB

---

# 20. Content Parser 架構

不要只做一個巨大 Parser。

建立：

IPlantContentParser

實作：

WebArticleParser
YouTubeParser
GenericHtmlParser

未來：

OfficialWebsiteParser
BlogParser

流程：

IPlantContentParserFactory
    ↓
判斷 URL
    ↓
選擇 Parser
    ↓
Parser 執行
    ↓
PlantSourceContent

---

# 21. URL 去重

任何 URL 儲存之前：

Normalize URL

例如：

https://example.com/article?id=123
https://example.com/article/?id=123

需要標準化。

再產生：

SHA256(normalizedUrl)

另外內容解析完成後：

SHA256(cleanText)

避免同一篇文章重複解析。

---

# 22. YouTube

YouTube 影片不能簡單假設可以任意下載字幕。

架構上：

YouTube URL
 ↓
YouTubeParser
 ↓
取得可合法使用的 metadata / transcript
 ↓
CleanText
 ↓
DB

如果無法取得 transcript：

SourceContent.Status = Failed
SourceContent.ErrorMessage = ...

不要讓整個系統失敗。

---

# 23. AI Agent

AI 不應該在：

Upload Image

時執行。

正確：

使用者上傳圖片
    ↓
保存 Diary
    ↓
保存 Image
    ↓
完成

使用者按：

「AI 分析」

才：

PlantAnalysisService
    ↓
建立 Analysis Job
    ↓
取得 Plant
    ↓
取得 PlantKnowledge
    ↓
取得 Diary
    ↓
取得圖片
    ↓
取得歷史 Analysis
    ↓
取得相關 PlantSourceContent
    ↓
建立 AI Context
    ↓
AI Agent
    ↓
JSON Result
    ↓
Validate
    ↓
DB
    ↓
前端顯示

---

# 24. AI Agent Context

AI 不要只看到圖片。

Context：

[Plant]
+
[PlantKnowledge]
+
[Recent Diaries]
+
[Recent Images]
+
[Previous AI Analysis]
+
[Relevant External Sources]

例如：

植物：
玉露

環境：
陽台

最近紀錄：
8/20 澆水
8/23 葉片變透明
8/28 發現中心葉片變化

圖片：
8/20
8/23
8/28

知識：
玉露需要明亮散射光
避免長時間高溫悶濕

外部文章：
文章 A
文章 B

AI 才能做「趨勢分析」。

---

# 25. AI 不可以假裝確定

Prompt 必須要求：

- 只能根據提供資料判斷
- 不確定時必須說不確定
- 不可直接宣稱植物得病
- 必須區分：
  Observed
  Possible
  Recommended
- 若需要人工確認，標記 needsHumanReview = true

AI 的結果是「建議」，不是醫療 / 農業專業診斷。

---

# 26. AI Agent Service

建立：

IAIAgentService

例如：

Task<PlantAnalysisResultDto> AnalyzePlantAsync(
    PlantAnalysisContext context
)

再由：

OpenAIPlantAgentService

實作。

未來可以：

OpenAIPlantAgentService
GeminiPlantAgentService
LocalPlantAgentService

不讓 Controller 知道是哪一個 AI。

OpenAI Responses API 支援文字與圖片輸入，也可搭配工具，因此很適合本專案的「圖片 + 植物資料 + Agent」模式。

---

# 27. AI 分析不要同步卡死 HTTP

第一階段可以：

POST /Plant/{id}/Analyze

建立：

PlantAnalysisJob

狀態：

Pending
Processing
Completed
Failed

Controller 回傳：

202 Accepted

前端：

顯示：

「AI 分析中...」

完成後：

Polling
或未來 SignalR

取得結果。

---

# 28. PlantAnalysisJob

PlantAnalysisJob
- Id
- PlantId
- DiaryId
- AnalysisId
- Status
- RetryCount
- StartedAt
- CompletedAt
- ErrorMessage
- CreatedAt

---

# 29. AI 分析流程

POST Analyze

↓

建立 Job

↓

Background Worker

↓

載入 Context

↓

呼叫 AI

↓

Validate JSON

↓

保存 Analysis

↓

Job Completed

---

# 30. 前端 UX

Plant Detail：

--------------------------------
我的玉露
--------------------------------

植物基本資料

☀ 光照
💧 澆水
🌡 溫度
💦 濕度

--------------------------------

我的植栽日記

[ + 新增日記 ]

2026/08/28
照片照片照片
今天葉子看起來比較透明

2026/08/23
照片照片

--------------------------------

[ 🤖 AI 分析 ]

按鈕只有使用者按下才執行。

--------------------------------

AI 分析紀錄

▼ 2026/08/28 21:00
   健康度：82
   摘要：目前整體狀況正常...

   展開後：
   觀察
   問題
   建議
   環境
   信心度

▼ 2026/08/23 20:10
   ...

--------------------------------

---

# 31. Collapse 設計

AIAnalysis List

每一筆：

Header：

日期
AnalysisType
HealthScore
Summary

Body：

- AI觀察
- 可能問題
- 建議
- 環境
- Warning
- Confidence

預設：

只展開最新一次。

---

# 32. 日記與 AI 分析關係

不要：

Diary = AI Analysis

正確：

Plant
 ├── Diary
 │    ├── Image
 │    └── Image
 │
 ├── Diary
 │    └── Image
 │
 └── Analysis
      ├── 使用 Diary A
      ├── 使用 Diary B
      └── 使用 Knowledge

未來可以讓一次 AI Analysis 分析一段時間。

---

# 33. 建議增加 AnalysisScope

AnalysisScope：

- SingleDiary
- Recent7Days
- Recent30Days
- AllHistory
- CustomRange

例如：

「分析最近 30 天」

AI Context 就取：

最近 30 天 Diary + Image。

---

# 34. DB 建議總覽

Plant
    ↓
PlantSpecies
    ↓
PlantKnowledge

Plant
    ↓
PlantDiary
    ↓
PlantImage

Plant
    ↓
PlantAnalysis

PlantSpecies
    ↓
PlantSource
    ↓
PlantSourceContent

Plant
    ↓
PlantAnalysisJob

---

# 35. 建議 Index

PlantSpecies
- ScientificName
- CommonName
- ChineseName
- TaxonId

Plant
- SpeciesId
- IsActive

PlantDiary
- PlantId + DiaryDate

PlantImage
- DiaryId

PlantAnalysis
- PlantId + CreatedAt

PlantSource
- SpeciesId
- ContentHash
- Url

PlantSourceContent
- SourceId
- ContentHash

---

# 36. 外部資料的資料優先級

建議：

Level 1
官方植物資料 / 植物學資料

Level 2
Trefle / GBIF / iNaturalist

Level 3
植物園 / 大學 / 農業單位

Level 4
專業植物網站

Level 5
部落格

Level 6
一般論壇 / 社群

AI Context 中必須保留 Source。

不要讓 AI 不知道資料來自哪裡。

---

# 37. Source Reliability

PlantSource 增加：

ReliabilityLevel

例如：

1 = 官方
2 = 學術 / 植物資料庫
3 = 專業網站
4 = 個人部落格
5 = 社群

AI Prompt 要求：

優先參考高可靠度來源。

---

# 38. Source Citation

AI 結果不要只：

「建議增加散射光。」

應該可以：

「建議增加明亮散射光。」

來源：

- Trefle
- 官方植物園文章
- 使用者提供文章

未來前端：

[查看來源]

---

# 39. Data Snapshot

AI 分析一定保存：

InputSnapshot

例如：

{
  "plant": {...},
  "knowledge": {...},
  "diaries": [...],
  "sources": [...],
  "analysisScope": "Recent30Days"
}

目的：

未來即使 DB 資料更新，也可以重現：

「AI 當時為什麼這樣判斷？」

---

# 40. AI Prompt Version

每次 AI 分析保存：

PromptVersion

例如：

plant-analysis-v1
plant-analysis-v2

未來修改 Prompt 不會影響舊紀錄。

---

# 41. Cache Strategy

Cache 分三層：

L1：
MemoryCache

L2：
Database

L3：
External API

流程：

Memory
 ↓ miss
DB
 ↓ miss / expired
External API
 ↓
DB
 ↓
Memory

---

# 42. 外部資料更新

不要每次進頁面更新。

可以：

Admin：

[同步植物資料]

或：

Background Job

例如：

每 7 天檢查一次。

但第一階段先不要做複雜排程。

---

# 43. 後台

建立：

/Admin

功能：

1. Plant Species
2. Plant Knowledge
3. External Sources
4. Source Content
5. AI Analysis
6. AI Jobs
7. API Sync Logs

---

# 44. 後台 Source 管理

畫面：

新增來源

URL：
[____________________________]

來源類型：
[自動判斷]

植物：
[玉露]

[解析]

解析完成：

Title
Author
PublishedAt
CleanText
Summary
Keywords
ContentHash

[儲存]

---

# 45. 後台 AI Context Preview

非常重要。

在執行 AI 前，可以提供：

「本次 AI 使用資料」

Plant
Knowledge
Diary
Images
Sources

方便 Debug。

未來可以：

[查看 AI Context]

---

# 46. API / Controller 建議

PlantController：

GET /Plant
GET /Plant/Create
POST /Plant/Create
GET /Plant/{id}
POST /Plant/{id}/Diary
POST /Plant/{id}/Image
POST /Plant/{id}/Analyze
GET /Plant/{id}/Analysis
GET /Plant/{id}/Analysis/{analysisId}

AdminPlantSourceController：

GET /Admin/PlantSources
GET /Admin/PlantSources/Create
POST /Admin/PlantSources/Create
POST /Admin/PlantSources/Parse
POST /Admin/PlantSources/{id}/Reparse

AdminPlantController：

GET /Admin/Plants
GET /Admin/Species
GET /Admin/Knowledge

---

# 47. API Response 不要直接回 Entity

Controller：

Entity
 ↓
DTO
 ↓
View / JSON

避免：

DB Entity
直接暴露。

---

# 48. Error Handling

外部 API：

Timeout
429
401
404
500

必須處理。

Article Parser：

404
403
Timeout
Unsupported
EmptyContent

AI：

Timeout
429
Invalid JSON
Token Limit
Provider Error

全部轉成系統自己的 Error Model。

---

# 49. Logging

每個外部操作要記：

Provider
Request
Status
Elapsed
Error

AI：

Model
PromptVersion
Token
Elapsed
Status
AnalysisId

但不要 Log：

API Key
完整敏感資料

---

# 50. Configuration

appsettings.json：

{
  "ExternalPlantApi": {
    "Trefle": {
      "BaseUrl": "https://trefle.io/api/v1/",
      "ApiKey": ""
    },
    "INaturalist": {
      "BaseUrl": "https://api.inaturalist.org/"
    },
    "GBIF": {
      "BaseUrl": "https://api.gbif.org/"
    }
  },
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "BaseUrl": "https://api.openai.com/",
      "ApiKey": "",
      "Model": ""
    }
  },
  "Storage": {
    "Provider": "Local",
    "RootPath": ""
  }
}

API Key 不可以寫死在程式。

Development：

appsettings.Development.json
User Secrets
Environment Variables

Production：

Environment Variables / Secret Manager

---

# 51. 第一階段不要做太多

MVP 只做：

Phase 1

[植物]

- 建立植物
- 植物 Species
- 植物基本資料
- Trefle / iNaturalist / GBIF adapter

[日記]

- 建立日記
- 上傳照片
- 照片列表

[知識]

- API 資料同步
- DB Cache

[AI]

- 手動 AI 分析
- AI 分析圖片
- AI 分析最近日記
- 保存 JSON
- Collapse 顯示歷史

---

# 52. Phase 2

[外部內容]

- URL Parser
- Article Parser
- YouTube Parser
- CleanText
- Summary
- Keyword
- Source

[AI]

- 加入 Source Context
- Citation
- Analysis Scope
- 歷史趨勢

---

# 53. Phase 3

[環境]

- 光照
- 溫度
- 濕度
- 澆水
- 施肥

[AI]

- 成長趨勢
- 澆水建議
- 病蟲害風險
- 異常預警

---

# 54. Phase 4

[智慧植栽]

- 自動提醒
- 趨勢圖
- 植物健康分數
- AI Timeline
- 多植物比較
- 個人化植物模型

---

# 55. 第一階段開發順序

Cursor 必須依照以下順序執行。

Step 1
檢查目前 solution。

Step 2
確認：
- .NET Version
- EF Core Version
- DB Provider
- Program.cs
- Startup / DI
- appsettings

Step 3
不要直接大量建立檔案。

先輸出：

「目前架構檢查結果」

Step 4
建立 Entities。

Step 5
建立 DbContext。

Step 6
建立 Migration。

Step 7
建立 Repository。

Step 8
建立 PlantService。

Step 9
建立 External API Adapter。

Step 10
建立 PlantKnowledge Cache。

Step 11
建立 Diary。

Step 12
建立 Image Storage。

Step 13
建立 AI Service interface。

Step 14
建立 AI Analysis Job。

Step 15
建立 MVC UI。

---

# 56. EF Migration 指令

請依目前專案 .NET / EF Core 版本調整。

如果 DbContext 在 Models：

dotnet ef migrations add InitialPlantSchema \
    --project Midnight.EC.Plant.WEB.Models \
    --startup-project Midnight.EC.Plant.WEB

更新：

dotnet ef database update \
    --project Midnight.EC.Plant.WEB.Models \
    --startup-project Midnight.EC.Plant.WEB

如果目前專案版本需要指定 DbContext：

dotnet ef migrations add InitialPlantSchema \
    --context PlantDbContext \
    --project Midnight.EC.Plant.WEB.Models \
    --startup-project Midnight.EC.Plant.WEB

---

# 57. NuGet 檢查

先不要直接亂裝。

先執行：

dotnet list package

確認：

- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer / 目前 DB Provider
- Microsoft.EntityFrameworkCore.Design
- Microsoft.Extensions.Http

再依實際 .NET 版本安裝對應版本。

如果 AI 使用官方 SDK，也要依目前專案 Target Framework 選擇相容版本。

---

# 58. 建議 DI

Program.cs / Startup：

AddHttpClient();

AddDbContext<PlantDbContext>();

AddScoped<IPlantService, PlantService>();
AddScoped<IPlantKnowledgeService, PlantKnowledgeService>();
AddScoped<IPlantDiaryService, PlantDiaryService>();
AddScoped<IPlantAnalysisService, PlantAnalysisService>();

AddScoped<IExternalPlantApiService, ...>();

AddScoped<IAIAgentService, ...>();

AddScoped<IImageStorageService, ...>();

AddScoped<IPlantContentParserService, ...>();

---

# 59. Repository 原則

不要建立過度 Generic Repository。

優先：

IPlantRepository
IPlantDiaryRepository
IPlantAnalysisRepository
IPlantSourceRepository

只抽象真正有商業意義的查詢。

---

# 60. Service 原則

Service 必須：

- async
- cancellation token
- logging
- exception handling
- transaction where necessary

不要：

Controller -> DbContext

應該：

Controller
 -> Service
 -> Repository
 -> DbContext

---

# 61. AI Context 建議

建立：

PlantAnalysisContext

包含：

PlantDto Plant
PlantKnowledgeDto Knowledge
List<PlantDiaryDto> Diaries
List<PlantImageDto> Images
List<PlantAnalysisDto> PreviousAnalyses
List<PlantSourceContentDto> Sources
AnalysisScope Scope

這是 AI 的唯一輸入模型。

---

# 62. AI Agent 不直接碰 DB

錯誤：

AI Service
 ↓
DbContext

正確：

PlantAnalysisService
 ↓
Repository
 ↓
PlantAnalysisContext
 ↓
IAIAgentService
 ↓
Result
 ↓
Repository

AI Agent 只處理 Context。

---

# 63. 外部 Parser 不直接碰 DB

錯誤：

Parser
 ↓
DbContext

正確：

AdminSourceService
 ↓
Parser
 ↓
ParsedContent
 ↓
Repository
 ↓
DB

---

# 64. 第一個可運作版本

完成後必須可以完整走：

使用者

↓

建立「我的玉露」

↓

系統建立 Species

↓

查外部植物 API

↓

Normalize

↓

PlantKnowledge 寫入 DB

↓

使用者建立日記

↓

上傳：

2026-08-31 玉露照片

↓

DB 保存 Diary + Image

↓

不執行 AI

↓

使用者按：

[AI 分析]

↓

建立 Analysis Job

↓

AI 取得：

Plant
Knowledge
Diary
Image

↓

AI 分析

↓

保存：

PlantAnalysis

↓

前端：

AI 分析紀錄

▼ 2026-08-31

健康度：85

摘要：
目前葉片狀況正常...

觀察：
...

建議：
...

---

# 65. Cursor 執行規則

你現在不是單純幫我寫 Demo。

你是在建立一個可以長期維護的正式專案。

請遵守：

1. 不破壞既有專案架構。
2. 不任意修改 namespace。
3. 不任意升級 .NET。
4. 不任意升級 EF Core。
5. 修改前先檢查現有程式。
6. 不把商業邏輯放 Controller。
7. 不把 API Key 寫死。
8. 不讓前端直接呼叫第三方 API。
9. 不讓每次開頁面都重新查外部 API。
10. AI 只有使用者按下分析才執行。
11. 上傳照片不觸發 AI。
12. AI 分析必須保存歷史。
13. AI 必須保存 InputSnapshot。
14. 外部文章 / 影片解析結果必須保存。
15. URL / Content 必須去重。
16. 外部 API 必須有 timeout / retry / rate limit handling。
17. 不可因為外部 API 失敗導致整個網站崩潰。
18. 所有外部資料都必須保存來源。
19. 所有 AI 結果都必須有 PromptVersion。
20. 先做 MVP，再擴充。

---

# 66. Cursor 第一個指令

請先不要寫程式。

先檢查整個 Midnight.EC.Plant.WEB Solution。

請列出：

1. Solution 結構
2. 每個 Project 的 Target Framework
3. Project Reference
4. NuGet Package
5. Program.cs / Startup.cs
6. appsettings
7. DB 連線設定
8. EF Core 設定
9. 現有 Controller
10. 現有 Models
11. 現有 Services
12. 現有 Utility
13. 是否已有 Repository Pattern
14. 是否已有 Dependency Injection
15. 是否已有 Authentication
16. 是否已有 File Upload
17. 是否已有 BackgroundService

然後對照本文件。

輸出：

A. 現況
B. 缺少
C. 建議修改
D. 不應修改
E. Phase 1 實作順序

確認後再開始寫程式。

---

# 67. Cursor 第二階段指令

在完成架構檢查後：

「請先只建立 Plant / PlantSpecies / PlantKnowledge / PlantDiary / PlantImage / PlantAnalysis / PlantSource / PlantSourceContent / PlantAnalysisJob Entity 與 DbContext。

不要先建立 Controller。

先確認 Entity Relationship。

完成後輸出：

- ER 關係
- Migration 預覽
- Index
- Foreign Key
- DeleteBehavior
- 欄位用途

確認沒有問題後再進下一步。」

---

# 68. Cursor 第三階段指令

「建立 Repository + Service。

要求：

Controller 不可以直接存取 DbContext。

所有 DB 操作由 Service 處理。

External API 與 AI Service 必須使用 Interface。

建立 DI。

完成後先 Build。

如果 Build Error，先修正 Error，不要繼續產生下一階段。」

---

# 69. Cursor 第四階段指令

「建立 External Plant API Adapter。

第一階段：

Trefle
iNaturalist
GBIF

不要讓 PlantService 依賴特定 Provider。

建立 Provider abstraction。

實作：

SearchSpecies
GetSpecies
GetPlantKnowledge

所有結果 Normalize 成自己的 Model。

加入：

Timeout
Retry
Logging
Rate Limit Handling
Cache

第一次查詢後寫入 DB。

再次查詢優先使用 DB。」

---

# 70. Cursor 第五階段指令

「建立 Plant Diary。

功能：

新增日記
上傳圖片
圖片列表
日記列表
刪除圖片
刪除日記

注意：

Upload Image 不可以呼叫 AI。

圖片必須透過 IImageStorageService。

DB 只存 metadata/path。」

---

# 71. Cursor 第六階段指令

「建立 AI Analysis。

使用者 POST /Plant/{id}/Analyze 時才執行。

不要在：

Create Plant
Create Diary
Upload Image

自動執行 AI。

建立：

PlantAnalysisJob

支援：

Pending
Processing
Completed
Failed

AI Input 必須建立 PlantAnalysisContext。

AI Output 必須 Validate JSON。

保存：

InputSnapshot
ResultJson
ModelName
PromptVersion
CreatedAt」

---

# 72. Cursor 第七階段指令

「建立 Plant Detail UI。

畫面包含：

1. 植物基本資料
2. 植物知識
3. 日記 Timeline
4. 圖片
5. AI 分析按鈕
6. AI 分析狀態
7. AI 歷史 Collapse

最新 AI 分析預設展開。

歷史分析預設收合。

UI 不可以自行呼叫外部 API。」

---

# 73. Cursor 第八階段指令

「建立 Admin Plant Source。

功能：

新增 URL
解析 URL
顯示 Parser 結果
儲存 Source
儲存 SourceContent
重新解析
查看 CleanText
查看 Summary
查看 Keywords

加入：

URL Hash
Content Hash

避免重複解析。」

---

# 74. Cursor 最終驗收

完成後執行：

dotnet build

確認：

0 Error

然後檢查：

[ ] Plant CRUD
[ ] Species
[ ] Knowledge
[ ] External API
[ ] DB Cache
[ ] Diary
[ ] Image Upload
[ ] AI Button
[ ] AI Job
[ ] AI Result
[ ] AI History
[ ] Collapse
[ ] Source URL
[ ] Article Parser
[ ] Content Storage
[ ] Logging
[ ] Error Handling
[ ] DI
[ ] Configuration
[ ] Migration

最後輸出：

1. 已完成
2. 尚未完成
3. 已知限制
4. 下一階段建議
5. API List
6. DB Table List
7. Project File Tree

---

# 75. 最重要的系統概念

本專案不是：

「拍照 -> AI 看植物」

而是：

                 ┌──────────────┐
                 │ PlantSpecies │
                 └──────┬───────┘
                        ↓
                ┌───────────────┐
                │ PlantKnowledge│
                └───────┬───────┘
                        ↓
┌──────────┐      ┌──────────────┐
│ Plant    │─────>│ Plant Diary  │
└────┬─────┘      └──────┬───────┘
     │                    ↓
     │              ┌────────────┐
     │              │ PlantImage │
     │              └──────┬─────┘
     │                     │
     └──────────────┬──────┘
                    ↓
             ┌──────────────┐
             │ AI Analysis  │
             └──────┬───────┘
                    ↑
                    │
        ┌───────────┴───────────┐
        │                       │
┌───────┴────────┐     ┌────────┴─────────┐
│ PlantSource    │────>│ SourceContent    │
│ Article/Video  │     │ CleanText/Parsed │
└────────────────┘     └──────────────────┘

最後形成：

「植物知識」
+
「我的環境」
+
「我的日記」
+
「我的照片」
+
「外部可信資料」
+
「歷史 AI」

=
「我的個人化植栽知識庫」

這才是本系統真正的核心。

---

# 76. 第一階段成功標準

當我第一次建立：

「我的小豆樹」

系統可以：

1. 找到標準植物。
2. 取得植物資料。
3. 寫入 DB。
4. 建立個人 Plant。
5. 上傳照片。
6. 建立日記。
7. 不自動 AI。
8. 按下 AI 分析。
9. AI 讀取照片。
10. AI 讀取植物知識。
11. AI 讀取日記。
12. 產生分析。
13. 保存分析。
14. 下次開啟頁面直接看到歷史。
15. 不重複查詢外部 API。
16. 後台可以加入文章 / 影片。
17. 解析結果保存。
18. 後續 AI 可以使用解析後資料。

完成以上後，再進行環境感測、趨勢分析、智慧提醒等功能。

