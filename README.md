# TodoSystem

个人待办管理系统。清单、待办、标签、子任务，数据按用户隔离。

后端 ASP.NET Core（.NET 10）+ PostgreSQL，前端 React 19，
登录支持邮箱密码与 Google 账号，两套会话（浏览器 Cookie / 客户端 JWT + Refresh Token）。

---

## 技术栈

| | |
|---|---|
| 后端 | .NET 10、ASP.NET Core Controllers、EF Core 10、PostgreSQL |
| 认证 | Cookie（v1）/ JWT + Refresh Token（v2）、Google Identity Services |
| 前端 | Vite + React 19 + TypeScript + Tailwind 4 + React Router |
| 其他 | Serilog、Asp.Versioning、Scalar（开发期 API 文档）、Docker Compose |

---

## 服务结构

### 后端：六个项目

| 项目 | 职责 |
|---|---|
| `TodoSystem.Domain` | 实体、基类、枚举常量。**无任何依赖** |
| `TodoSystem.Infrastructure` | JWT 签发、Google 校验、邮件发送、异常类型 |
| `TodoSystem.EntityFrameworkCore` | `DbContext`、实体配置、迁移 |
| `TodoSystem.Application` | 业务服务与 DTO |
| `TodoSystem.Web` | 宿主：控制器、路由、DI、认证、启动期配置校验 |
| `TodoSystem.HealthDashboard` | 独立的健康看板进程，**不引用其他项目** |

依赖单向向下：

```
Web ──▶ Application ──▶ EntityFrameworkCore ──▶ Domain
                 └────▶ Infrastructure ─────────┘
```

### 一次请求

```
浏览器 ──▶ Vite 代理 / nginx ──▶ Controller ──▶ Service ──▶ DbContext ──▶ PostgreSQL
                                   └─ ApiResultFilter 包统一响应体
```

### 前端

四个路由：`/login`、`/register`、`/verify-email`、`/`（待办主界面，需登录）。
`src/` 分 `apis` / `hooks` / `components` / `layouts` / `pages` / `stores` / `router` / `types` / `utils`。

开发时 Vite 把 `/api` 代理到后端，浏览器视角同源 —— 因此不需要 CORS，Cookie 也不受 `SameSite` 影响。

### 容器：四个服务

| 服务 | 内容 | 端口 | 对外 |
|---|---|---|---|
| `web` | nginx + 前端静态文件 | `8080` | **是** |
| `api` | `TodoSystem.Web` | `127.0.0.1:15241` | 否 |
| `db` | `postgres:17-alpine` | `127.0.0.1:5432` | 否 |
| `dashboard` | `TodoSystem.HealthDashboard` | `127.0.0.1:15242` | 否 |

容器里前后端是两个 IP，nginx 把 `/api` 反代到 api 容器，把两个源合成一个。

---

## 项目结构

```
TodoSystem/
├── TodoSystem.Domain/
├── TodoSystem.Application/
├── TodoSystem.Infrastructure/
├── TodoSystem.EntityFrameworkCore/
├── TodoSystem.Web/                 # 含 Dockerfile
├── TodoSystem.HealthDashboard/     # 含 Dockerfile
├── TodoSystem.WebClient/           # 前端，含 Dockerfile + nginx.conf
├── docker-compose.yml
├── .env.example
└── TodoSystem.slnx
```

---

## 领域模型

八张表，全部走软删除（`deleted_at`）+ 全局查询过滤器。

```
users ──┬── todo_lists
        ├── todo_items ──┬── parent_id ──▶ todo_items（子任务，最多两层）
        │                └── todo_item_tags ──▶ tags
        ├── tags
        ├── external_logins
        ├── refresh_tokens
        └── email_verification_tokens
```

- **待办**：`todo_lists` / `todo_items` / `tags` / `todo_item_tags`
- **认证**：`users` / `external_logins` / `refresh_tokens` / `email_verification_tokens`

几条关键约束：

- `todo_items` 用**复合外键** `(user_id, list_id) → todo_lists(user_id, id)`，
  让数据库保证「待办只能挂到自己名下的清单」，服务层漏校验也插不进去。
- 唯一索引一律带 `WHERE deleted_at IS NULL`，否则软删除后同名记录再也建不出来。
- `refresh_tokens` / `email_verification_tokens` **只存哈希**，明文只在签发时出现一次。
- `status` / `priority` 用 CHECK 约束兜住取值；`status = 'DONE'` 与 `completed_at` 必须同进同退。

---

## 接口

路径前缀 `/api`，版本写在路径里（`/api/v1/...`、`/api/v2/...`）。统一响应体：

```json
{ "data": {}, "statusCode": 200, "code": 0, "message": "" }
```

`code === 0` 成功；失败时 `code` 等于 HTTP 状态码，`message` 可直接展示。

| 资源 | 路径 |
|---|---|
| 清单 | `/api/v1/todo-lists` |
| 待办 | `/api/v1/todo-items`（含 `/status`、`/tags` 子资源；支持按清单 / 状态 / 标签 / 关键字等筛选） |
| 标签 | `/api/v1/tags` |
| 认证 | `/api/v1/auth`（Cookie）、`/api/v2/auth`（JWT + Refresh） |
| 账号 | `/api/v1/account`（`me`、绑定 Google） |
| 邮箱验证 | `/api/v1/email-verification`（`verify`、`resend`） |
| 公开配置 | `/api/v1/common/config` |
| 健康检查 | `/healthz`（匿名） |

开发环境有 Scalar 文档：`http://localhost:15241/scalar/v1`。

访问别人的资源一律返回 **404 而不是 403** —— 403 等于承认「这个 id 存在，只是不是你的」。

---

## 认证

- **v1 Cookie**：同站浏览器，`SameSite=Lax`。
- **v2 JWT + Refresh Token**：Access Token 30 分钟，Refresh Token 7 天且**轮换**，
  检测到已吊销令牌被再次使用即视为重放，整条链作废。
- **Google 登录**：前端拿 `id_token` 传给后端验签。按 `(google, sub)` 找绑定 →
  邮箱匹配到**已验证**的本地账号则合并 → 匹配到未验证账号则拒绝（409）→ 都没有则新建。
- **邮箱验证**：注册后发验证邮件，链接指向前端 `/verify-email?token=...`。
  验证接口幂等，重发接口不泄漏账号是否存在。未验证不阻断登录。

---

## 快速开始

需要 .NET 10 SDK、PostgreSQL、Node.js。

```bash
# 后端 → http://localhost:15241
dotnet run --project TodoSystem.Web --launch-profile http

# 前端 → http://localhost:5173
cd TodoSystem.WebClient && npm install && npm run dev

# 健康看板（可选）→ http://localhost:15242/dashboard
dotnet run --project TodoSystem.HealthDashboard --launch-profile http
```

数据库连接串在 `TodoSystem.Web/appsettings.Development.json`，
应用启动时自动 `MigrateAsync()`，不需要手工建表。
首次启动会写入种子账号 `ADMIN` / `USER`（密码 `123`），**只在开发环境**。

### 配置

优先级：`appsettings.json` → `appsettings.{Environment}.json` → 环境变量
（层级用**双下划线**，如 `Email__Smtp__Password`）。

`appsettings.json` 会跟着镜像打进生产，所以只放「不是密钥、且每个实例都一样」的值；
连接串、密钥、SMTP 密码都在 `appsettings.Development.json` 或环境变量里。

启动时会做一次配置体检：必填项缺失、取值非法、组合不自洽都一次性报完。

本机开发默认 `Email:Provider=File`，邮件落盘到 `TodoSystem.Web/emails/`
并把验证链接打进日志 —— 不配 SMTP 也能跑通整条验证链路。

---

## 容器部署

```bash
cp .env.example .env      # 改掉 POSTGRES_PASSWORD、JWT_SECRET_KEY
docker compose up -d --build
```

打开 `http://localhost:8080`。

模板默认 `EMAIL_PROVIDER=File`：邮件写成 `.html` 落进数据卷，验证链接同时打进日志，
不配 SMTP 也能把「注册 → 收信 → 验证」跑通。要看链接：

```bash
docker compose logs -f api | grep 邮件链接
```

要真发信，在 `.env` 里把 `EMAIL_PROVIDER` 改成 `Smtp`，并把 `EMAIL_FROM_ADDRESS` /
`EMAIL_SMTP_HOST` / `EMAIL_SMTP_USERNAME` / `EMAIL_SMTP_PASSWORD` 四项一起填上
（163 用 25 端口，密码填「授权码」不是登录密码）。只填一半会在启动时被拦下。

- compose 占用的 `5432` / `15241` / `15242` 与本地 `dotnet run` 同端口，两边不能同时跑。
- compose 里的 `${VAR:-}` 是「覆盖」不是「留空」：给 base 里已有非空值的键写默认值时，
  要和 base 保持一致，否则变量没设就把值抹成空串。
- 非 Development 环境用 `File` 投递时，启动会打一条警告 —— 邮件只落盘、不会真的寄出。
