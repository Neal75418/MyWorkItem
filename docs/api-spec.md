# API 規格

Base URL：`http://localhost:5045/api`

OpenAPI JSON（執行時）：`http://localhost:5045/openapi/v1.json`

## 認證

除 `POST /auth/login` 外，所有端點皆需在 `Authorization` 標頭附帶 JWT：

```
Authorization: Bearer <token>
```

---

## POST /auth/login

登入並取得 JWT。

**請求：**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**回應 200：**
```json
{
  "token": "eyJhbGciOi...",
  "user": {
    "id": "79a8052c-f67e-4581-9b19-f4430d2057eb",
    "username": "admin",
    "displayName": "Admin User",
    "role": "Admin"
  }
}
```

**回應 401：**
```json
{
  "message": "Invalid username or password"
}
```

---

## GET /auth/me

取得目前已認證使用者的資訊。

**回應 200：**
```json
{
  "id": "79a8052c-f67e-4581-9b19-f4430d2057eb",
  "username": "admin",
  "displayName": "Admin User",
  "role": "Admin"
}
```

---

## GET /work-items

取得工作項目列表，含當前使用者的個人化狀態。

**查詢參數：**

| 參數 | 型別 | 預設值 | 可選值 |
|------|------|--------|--------|
| sortBy | string | `createdAt` | `createdAt`, `title` |
| sortDir | string | `desc` | `asc`, `desc` |

**回應 200：**
```json
{
  "items": [
    {
      "id": "b98f46f1-be4c-4bf2-aa03-4302dbb8fa84",
      "title": "Review Q1 Financial Report",
      "description": "Review and verify all Q1 financial statements.",
      "status": "Pending",
      "isConfirmed": false,
      "confirmedAt": null,
      "createdAt": "2026-03-13T05:38:27Z",
      "updatedAt": "2026-03-13T05:38:27Z"
    },
    {
      "id": "6a6bf7a1-7bc4-4fe7-95e0-00f5874ce891",
      "title": "Complete Safety Training",
      "description": "Annual workplace safety training module.",
      "status": "Confirmed",
      "isConfirmed": true,
      "confirmedAt": "2026-03-13T06:00:00Z",
      "createdAt": "2026-03-13T05:38:27Z",
      "updatedAt": "2026-03-13T05:38:27Z"
    }
  ],
  "totalCount": 6
}
```

---

## GET /work-items/{id}

取得單一工作項目詳情，含當前使用者狀態。

**回應 200：**
```json
{
  "id": "b98f46f1-be4c-4bf2-aa03-4302dbb8fa84",
  "title": "Review Q1 Financial Report",
  "description": "Review and verify all Q1 financial statements before the deadline.",
  "status": "Pending",
  "isConfirmed": false,
  "confirmedAt": null,
  "createdByName": "Admin User",
  "createdAt": "2026-03-13T05:38:27Z",
  "updatedAt": "2026-03-13T05:38:27Z"
}
```

**回應 404：** 工作項目不存在。

---

## POST /work-items/confirm

為當前使用者批次確認工作項目。

**請求：**
```json
{
  "workItemIds": [
    "b98f46f1-be4c-4bf2-aa03-4302dbb8fa84",
    "6a6bf7a1-7bc4-4fe7-95e0-00f5874ce891"
  ]
}
```

**回應 200：**
```json
{
  "confirmedCount": 2
}
```

---

## PATCH /work-items/{id}/unconfirm

撤銷單一工作項目的確認（僅限當前使用者）。

**回應 200：**
```json
{
  "message": "Unconfirmed successfully"
}
```

**回應 404：** 狀態紀錄不存在或尚未確認。

---

## GET /admin/work-items

列出所有工作項目（管理員視圖）。**需 Admin 角色。**

**回應 200：**
```json
[
  {
    "id": "b98f46f1-be4c-4bf2-aa03-4302dbb8fa84",
    "title": "Review Q1 Financial Report",
    "description": "Review and verify all Q1 financial statements.",
    "createdByName": "Admin User",
    "createdAt": "2026-03-13T05:38:27Z",
    "updatedAt": "2026-03-13T05:38:27Z"
  }
]
```

---

## POST /admin/work-items

新增工作項目。**需 Admin 角色。**

**請求：**
```json
{
  "title": "New Task",
  "description": "Optional description"
}
```

**驗證：** `title` 必填（非空，最長 500 字元）。

**回應 201：**
```json
{
  "id": "new-uuid",
  "title": "New Task",
  "description": "Optional description",
  "createdByName": "Admin User",
  "createdAt": "2026-03-13T07:00:00Z",
  "updatedAt": "2026-03-13T07:00:00Z"
}
```

---

## PUT /admin/work-items/{id}

修改既有工作項目。**需 Admin 角色。**

**請求：**
```json
{
  "title": "Updated Title",
  "description": "Updated description"
}
```

**回應 200：** 更新後的工作項目 DTO（格式同新增）。

**回應 404：** 工作項目不存在。

---

## DELETE /admin/work-items/{id}

刪除工作項目。**需 Admin 角色。**

**回應 204：** 無內容（成功）。

**回應 404：** 工作項目不存在。

---

## 錯誤回應格式

採用 ASP.NET Core `ProblemDetails`：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-abc..."
}
```
