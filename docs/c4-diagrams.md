# C4 Architecture Diagrams

## Level 1 — System Context

```mermaid
graph TB
    User["User\n(Browser)"]
    Admin["Admin\n(Browser)"]
    System["MyWorkItem System\nView, confirm work items\nPersonalized status"]
    DB[("PostgreSQL\nDatabase")]

    User -->|"View & confirm\nwork items"| System
    Admin -->|"Manage work items\nCRUD operations"| System
    System -->|"Read/write\nusers, items, statuses"| DB

    style User fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style Admin fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style System fill:#2d7dd2,stroke:#1a5fa0,color:#fff
    style DB fill:#2d7dd2,stroke:#1a5fa0,color:#fff

    linkStyle default stroke:#8ab4f8,stroke-width:2px
```

## Level 2 — Container Diagram

```mermaid
graph TB
    subgraph browser["Browser"]
        SPA["React SPA\nTypeScript · MUI · React Query\n\nWork item list, detail\nand management UI"]
    end

    subgraph server["Application Server"]
        Nginx["Nginx\nReverse Proxy\n\nServes static files\nProxies /api/* to API"]
        API["ASP.NET Core Web API\n.NET 10 · C#\n\nAuth, business logic\nand data access"]
    end

    subgraph datastore["Data Store"]
        PG[("PostgreSQL 18\n\nUsers, work items\nand per-user statuses")]
    end

    SPA -->|"HTTP/JSON\nJWT Bearer"| Nginx
    Nginx -->|"/api/*\nReverse proxy"| API
    API -->|"EF Core\nNpgsql"| PG

    style browser fill:transparent,stroke:#555,color:#999
    style server fill:transparent,stroke:#555,color:#999
    style datastore fill:transparent,stroke:#555,color:#999
    style SPA fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style Nginx fill:#e8913a,stroke:#c47a2f,color:#fff
    style API fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style PG fill:#4a9eff,stroke:#2d7dd2,color:#fff

    linkStyle default stroke:#8ab4f8,stroke-width:2px
```

## Level 3 — Component Diagram (Backend)

```mermaid
graph TB
    subgraph api["API Layer — MyWorkItem.Api"]
        AuthCtrl["AuthController\nLogin, current user"]
        WICtrl["WorkItemsController\nList, detail, confirm, undo"]
        AdminCtrl["AdminWorkItemsController\nCRUD operations"]
        JWTMiddleware["JWT Auth Middleware"]
    end

    subgraph app["Application Layer — MyWorkItem.Application"]
        AuthSvc["AuthService\nLogin validation, user lookup"]
        WISvc["WorkItemService\nList with personal status\nConfirm / undo logic"]
        AdminSvc["AdminWorkItemService\nCreate, update, delete"]
        IAppDb["IAppDbContext\nInterface"]
    end

    subgraph infra["Infrastructure Layer — MyWorkItem.Infrastructure"]
        DbCtx["AppDbContext\nEF Core DbContext"]
        TokenSvc["TokenService\nJWT generation"]
        PwdHash["BcryptPasswordHasher\nPassword hashing"]
        SeedData["SeedData\nDemo accounts & items"]
    end

    subgraph domain["Domain Layer — MyWorkItem.Domain"]
        UserEntity["User Entity"]
        WorkItemEntity["WorkItem Entity"]
        StatusEntity["UserWorkItemStatus Entity"]
        RoleEnum["UserRole Enum"]
    end

    AuthCtrl --> AuthSvc
    WICtrl --> WISvc
    AdminCtrl --> AdminSvc

    AuthSvc --> IAppDb
    AuthSvc --> TokenSvc
    AuthSvc --> PwdHash
    WISvc --> IAppDb
    AdminSvc --> IAppDb

    DbCtx -.->|"implements"| IAppDb
    DbCtx --> UserEntity
    DbCtx --> WorkItemEntity
    DbCtx --> StatusEntity

    style api fill:transparent,stroke:#555,color:#999
    style app fill:transparent,stroke:#555,color:#999
    style infra fill:transparent,stroke:#555,color:#999
    style domain fill:transparent,stroke:#555,color:#999

    style AuthCtrl fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style WICtrl fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style AdminCtrl fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style JWTMiddleware fill:#4a9eff,stroke:#2d7dd2,color:#fff

    style AuthSvc fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style WISvc fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style AdminSvc fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style IAppDb fill:#6cb4ee,stroke:#4a9eff,color:#fff

    style DbCtx fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style TokenSvc fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style PwdHash fill:#6cb4ee,stroke:#4a9eff,color:#fff
    style SeedData fill:#6cb4ee,stroke:#4a9eff,color:#fff

    style UserEntity fill:#5b8c5a,stroke:#3d6b3d,color:#fff
    style WorkItemEntity fill:#5b8c5a,stroke:#3d6b3d,color:#fff
    style StatusEntity fill:#5b8c5a,stroke:#3d6b3d,color:#fff
    style RoleEnum fill:#5b8c5a,stroke:#3d6b3d,color:#fff

    linkStyle default stroke:#8ab4f8,stroke-width:1.5px
```

## Data Flow — Confirm Work Items

```mermaid
sequenceDiagram
    actor User as User
    participant SPA as React SPA
    participant API as WorkItemsController
    participant Svc as WorkItemService
    participant DB as PostgreSQL

    User->>SPA: Select items, click "Confirm"
    SPA->>API: POST /api/work-items/confirm<br/>{workItemIds: [...]}
    API->>API: Extract userId from JWT
    API->>Svc: ConfirmWorkItemsAsync(userId, ids)
    Svc->>DB: SELECT existing statuses<br/>WHERE user_id = ? AND work_item_id IN (...)
    DB-->>Svc: Existing statuses
    Svc->>DB: UPDATE existing → isConfirmed=true<br/>INSERT missing → new status records
    DB-->>Svc: OK
    Svc-->>API: confirmedCount
    API-->>SPA: 200 {confirmedCount: N}
    SPA->>SPA: Invalidate query cache
    SPA->>API: GET /api/work-items (refetch)
    API->>Svc: GetWorkItemsAsync(userId, ...)
    Svc->>DB: SELECT work_items<br/>LEFT JOIN statuses WHERE user_id = ?
    DB-->>Svc: Items with personal status
    Svc-->>API: WorkItemListResponse
    API-->>SPA: Updated list
    SPA-->>User: Status shows "Confirmed"
```

## Deployment — Docker Compose

```mermaid
graph TB
    Browser["Browser\n:3080"]

    subgraph docker["Docker Compose"]
        direction TB
        Web["web — Nginx\nStatic files · Reverse proxy\n:80"]
        Api["api — .NET 10\nASP.NET Core Web API\n:8080"]
        DB[("db — PostgreSQL 18\nPersistent volume\n:5432")]
    end

    Browser -->|"http://localhost:3080"| Web
    Web -->|"/ → static files"| Web
    Web -->|"/api/* → proxy_pass"| Api
    Api -->|"EF Core · Npgsql"| DB

    style Browser fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style docker fill:transparent,stroke:#555,color:#999
    style Web fill:#e8913a,stroke:#c47a2f,color:#fff
    style Api fill:#4a9eff,stroke:#2d7dd2,color:#fff
    style DB fill:#5b8c5a,stroke:#3d6b3d,color:#fff

    linkStyle default stroke:#8ab4f8,stroke-width:2px
```
