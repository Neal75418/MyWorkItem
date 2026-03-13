# My Work Item — 後端 API

.NET 10 Web API，提供「My Work Item」應用的後端服務。使用者可檢視並管理個人工作項目狀態，管理員可進行 CRUD 操作。

## 技術棧

| 元件 | 技術 | 版本 |
|------|------|------|
| 執行環境 | ASP.NET Core Web API | .NET 10 |
| 資料庫 | PostgreSQL + EF Core (Npgsql) | PostgreSQL 18 |
| 認證 | JWT Bearer Token | — |
| 密碼雜湊 | BCrypt | — |
| 架構 | Clean Architecture（四層） | — |

## 架構分層

```
MyWorkItem.Api              → Controllers、Program.cs、Middleware
MyWorkItem.Application      → Services、DTOs、Interfaces
MyWorkItem.Domain           → Entities、Enums（零依賴）
MyWorkItem.Infrastructure   → EF Core DbContext、JWT/BCrypt Services
```

依賴方向：`Api → Application ← Infrastructure → Domain`

## 前置需求

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL（本地，Port 5432）

## 快速啟動

```bash
# 1. 進入專案目錄
cd MyWorkItem

# 2. 啟動 API（自動建立資料庫 + 資料表 + 種子資料）
cd src/MyWorkItem.Api
dotnet run
```

API 啟動於 **http://localhost:5045**。

資料庫 `my_work_item` 會在首次啟動時透過 EF Core Migration **自動建立**，無需手動執行 SQL。

## 示範帳號

| 帳號 | 密碼 | 角色 | 顯示名稱 |
|------|------|------|----------|
| admin | admin123 | Admin | Admin User |
| user1 | user123 | User | Alice Wang |
| user2 | user123 | User | Bob Chen |

## API 端點

### 認證

| 方法 | 路徑 | 說明 | 認證 |
|------|------|------|------|
| POST | `/api/auth/login` | 登入，回傳 JWT | 否 |
| GET | `/api/auth/me` | 取得目前使用者資訊 | 需要 |

### 工作項目（前台使用者）

| 方法 | 路徑 | 說明 | 認證 |
|------|------|------|------|
| GET | `/api/work-items` | 列表（含個人狀態） | 需要 |
| GET | `/api/work-items/{id}` | 詳情（含個人狀態） | 需要 |
| POST | `/api/work-items/confirm` | 批次確認 | 需要 |
| PATCH | `/api/work-items/{id}/unconfirm` | 撤銷單項確認 | 需要 |

**查詢參數**（GET `/api/work-items`）：
- `sortBy` — `createdAt`（預設）或 `title`
- `sortDir` — `desc`（預設）或 `asc`

### 管理（需 Admin 角色）

| 方法 | 路徑 | 說明 | 認證 |
|------|------|------|------|
| GET | `/api/admin/work-items` | 管理列表 | Admin |
| POST | `/api/admin/work-items` | 新增 | Admin |
| PUT | `/api/admin/work-items/{id}` | 修改 | Admin |
| DELETE | `/api/admin/work-items/{id}` | 刪除 | Admin |

### OpenAPI

執行時可存取：`http://localhost:5045/openapi/v1.json`

## 組態設定

連線字串與 JWT 設定位於 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=my_work_item;Username=nealchen"
  },
  "Jwt": {
    "Key": "ThisIsASecretKeyForDevelopmentOnlyAtLeast32Characters!",
    "Issuer": "MyWorkItem",
    "Audience": "MyWorkItemApp"
  }
}
```

## 關鍵設計決策

| 決策 | 選擇 | 理由 |
|------|------|------|
| 個人化狀態 | 延遲建立（無紀錄 = Pending） | 簡單，不需背景作業 |
| 密碼雜湊 | BCrypt | 業界標準，安全可靠 |
| 認證 | JWT（24 小時過期） | 無狀態，適合 SPA 架構 |
| 刪除策略 | 硬刪除 | 面試範圍，保持簡潔 |
| 架構 | Clean Architecture 四層 | 展示關注點分離 |

## 前端專案

React SPA 前端：請參閱 [my-work-item-web](../WebstormProjects/my-work-item-web/) 專案。
