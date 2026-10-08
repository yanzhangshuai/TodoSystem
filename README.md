# TodoSystem

.NET 10 + PostgreSQL 的待办后端，配套 Vite + React 19 前端。
认证支持**邮箱密码**和 **Google 登录（ID Token 直传）**。

## 项目结构

```
TodoSystem/
├── TodoSystem.Domain/              # 实体、常量、基类（无外部依赖）
├── TodoSystem.Application/         # 业务服务、DTO
├── TodoSystem.Infrastructure/      # JWT 签发、Google Token 校验、异常
├── TodoSystem.EntityFrameworkCore/ # DbContext、实体配置、迁移
├── TodoSystem.Web/                 # ASP.NET Core 宿主、控制器
├── TodoSystem.HealthDashboard/     # 健康检查看板（独立进程，只轮询 /healthz）
├── TodoSystem.WebClient/           # 前端（Vite + React 19 + Tailwind 4）
└── TodoSystem.slnx
```

---

## 领域模型

八张表：**四张认证表**（`users` / `external_logins` / `refresh_tokens` / `email_verification_tokens`）+
**四张待办表**（`todo_lists` / `todo_items` / `tags` / `todo_item_tags`）。

```
users ──┬── todo_lists ──┐
        │                │ (user_id, list_id) 复合外键
        ├── todo_items ──┘
        │      │  │
        │      │  └── parent_id ──▶ todo_items   (自引用，子任务)
        │      │
        │      └── todo_item_tags ──▶ tags
        │             (复合主键)
        ├── tags
        ├── external_logins
        ├── refresh_tokens
        └── email_verification_tokens
```

### `todo_lists` — 待办清单

| 列 | 类型 | 说明 |
|---|---|---|
| `id` | int identity | 主键 |
| `user_id` | int | 归属用户，FK → `users.id` |
| `name` | varchar(100) | 清单名 |
| `description` | varchar(500) | 说明 |
| `color` | varchar(20) | 展示色，后端不解释语义 |
| `sort_order` | int | 手动排序 |
| `is_archived` | bool | 归档后默认不展示，数据保留 |
| `created_at` / `updated_at` / `deleted_at` | timestamptz | 审计 + 软删除 |

索引：`ix_todo_lists_user_id_name`（部分唯一，`deleted_at IS NULL`）、
`ix_todo_lists_user_id_sort_order`、唯一约束 `ak_todo_lists_user_id_id`（供复合外键引用）。

### `todo_items` — 待办项

| 列 | 类型 | 说明 |
|---|---|---|
| `id` | int identity | 主键 |
| `user_id` | int | 归属用户 |
| `list_id` | int? | 所属清单，null = 未归类 |
| `parent_id` | int? | 父待办，null = 顶层 |
| `title` | varchar(200) | 标题 |
| `description` | varchar(2000) | 详细说明 |
| `status` | varchar(20) | `TODO` / `IN_PROGRESS` / `DONE` |
| `priority` | varchar(20) | `LOW` / `NORMAL` / `HIGH` / `URGENT` |
| `due_at` | timestamptz? | 截止时间 |
| `completed_at` | timestamptz? | 完成时间 |
| `sort_order` | int | 手动排序 |

外键：

| 外键 | 列 | 目标 | 删除行为 |
|---|---|---|---|
| `fk_todo_items_users_user_id` | `user_id` | `users(id)` | Cascade |
| `fk_todo_items_todo_lists_user_id_list_id` | **`(user_id, list_id)`** | `todo_lists(user_id, id)` | Restrict |
| `fk_todo_items_todo_items_parent_id` | `parent_id` | `todo_items(id)` | Restrict |

CHECK 约束（都在数据库层兜底，不依赖应用层自律）：

- `ck_todo_items_status` / `ck_todo_items_priority` — 枚举取值
- `ck_todo_items_parent_not_self` — `parent_id <> id`
- `ck_todo_items_completed_at_consistent` — `status = 'DONE'` 与 `completed_at IS NOT NULL` 必须同真同假

索引：`(user_id, status)`、`(user_id, due_at)`、`(list_id, sort_order)`、`(parent_id)`。

### `tags` — 标签（用户私有）

`user_id` / `name`(50) / `color`(20) + 审计列。
索引：`ix_tags_user_id_name`（部分唯一，`deleted_at IS NULL`）。

### `todo_item_tags` — 待办 ↔ 标签（多对多）

只有 `todo_item_id` + `tag_id` 两列，**复合主键**，两个外键都是 Cascade。

---

## 设计要点

### 1. 隔离靠「归属」，不靠「角色」

每一张待办表都有 `user_id`，服务层每个查询都必须带上它。
接口上没有任何角色限制 —— 任何登录用户都能操作自己的数据，也只能操作自己的数据。

`ITodoListService` 这类接口把 `userId` 做成**必填的第一个参数**，
编译器会逼着每个调用点交出这个值，从形状上杜绝「忘了过滤用户」。

访问别人的资源一律返回 **404 而不是 403**：403 等于承认「这个 id 存在，只是不是你的」，
把别人数据的存在性泄漏了出去。

### 2. 软删除 + 部分唯一索引

所有业务表都走软删除（`Remove` 被 `TodoSystemDbContext` 的审计逻辑改写成 `UPDATE deleted_at`），
并有全局查询过滤器自动隐藏已删除行。

唯一索引因此必须带 `WHERE deleted_at IS NULL`：

```sql
CREATE UNIQUE INDEX ix_todo_lists_user_id_name ON todo_lists (user_id, name)
WHERE deleted_at IS NULL;
```

没有这个 filter，删掉一个清单之后同名清单就再也建不出来 ——
查询过滤器看不见软删除的行，唯一索引却照样拦插入，而且报的是 500 不是 409。

### 3. 复合外键：让数据库保证「待办只能挂到自己名下的清单」

`todo_items` 指向 `todo_lists` 用的是 **复合外键 `(user_id, list_id) → (user_id, id)`**，
而不是单列 `list_id → id`。

单列外键只保证「清单存在」，不保证「清单是你的」。一旦服务层漏了一处归属校验，
A 的待办就能挂到 B 的清单下，B 打开清单时看到 A 的待办标题 —— 静默越权。
复合外键把这条规则交给数据库，应用层写错也插不进去。

代价（都已在代码里处理并注释）：

| 代价 | 处理 |
|---|---|
| 无法用 `ON DELETE SET NULL`（会把 `user_id` 也置空） | 删清单时服务层显式把待办退回收件箱 |
| EF Core 翻译不了这个外键的**集合**导航 | `TodoListEntity` 上不放 `Items` 属性，计数用显式子查询 |
| 需要一个额外唯一约束当 principal key | `ak_todo_lists_user_id_id` |

> 注意 `list_id` 为 NULL 时整条约束不生效（PostgreSQL 默认 `MATCH SIMPLE`）——
> 这正是「未归类待办」能存在的原因。

### 4. 关联表不继承 `AppBaseEntity`

`todo_item_tags` 是本模型里唯一的例外。关联表没有独立生命周期：
「挂标签」是插入一行，「摘标签」是删除一行，不存在「修改关联」，
所以 `UpdatedAt` / `DeletedAt` 永远没有意义。
复合主键 `(todo_item_id, tag_id)` 顺带保证了同一标签不会被重复挂到同一条待办上。

同时给它配了与两端配对的查询过滤器：

```csharp
builder.HasQueryFilter(x => x.Tag!.DeletedAt == null && x.TodoItem!.DeletedAt == null);
```

不配的话，EF 会警告「required end 被过滤」，而且 `db.TodoItemTags` 会返回
「标签已删除」的关联行，`Include(x => x.Tag)` 拿到 null 而导航声明是非空的。

### 5. 枚举值用字符串常量 + CHECK 约束

`TodoStatuses` / `TodoPriorities` 是常量类而不是 `enum`，理由与既有的 `UserRoles` 一致：
这些值同时出现在数据库、API 响应和前端联合类型里。
用 enum 需要配两套独立转换（`HasConversion` 落库、`JsonStringEnumConverter` 出参），
两处命名规则一旦不一致就会出现「库里是 `TODO`、接口返回 `Todo`」这种只能靠联调发现的错位。
类型安全由「常量类 + CHECK 约束」共同保证。

### 6. 子任务最多两层

`parent_id` 是自引用外键，但外键管不到**深度**。
「父任务本身不能是子任务」由服务层 `EnsureParentUsableAsync` 校验。
不限制的话会无限嵌套，展示时要么递归爆栈、要么只能砍掉。

### 7. `completed_at` 不是 `status` 的副产品

`completed_at` 单独存一列，而不是「由 `status = 'DONE'` 推导」——
「什么时候完成的」本身就是要展示和统计的信息，推导会把它丢掉。
两者的一致性由服务层 `ApplyStatus` 维护（置 DONE 写时间、退回清空），
并由 `ck_todo_items_completed_at_consistent` 在数据库层兜底。

---

## 接口一览

统一响应结构：

```json
{ "data": {}, "statusCode": 200, "code": 0, "message": "" }
```

`code === 0` 表示成功；失败时 `code` 等于 HTTP 状态码，`message` 是可直接展示的文案。

### 待办（需要登录，v1）

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/v1/todo-lists` | 清单列表，`?includeArchived=false`，带 `itemCount` / `completedCount` |
| GET | `/api/v1/todo-lists/{id}` | 单个清单 |
| POST | `/api/v1/todo-lists` | 新建清单 |
| PUT | `/api/v1/todo-lists/{id}` | 整体替换 |
| DELETE | `/api/v1/todo-lists/{id}` | 删除清单（其中的待办退回收件箱，不删） |
| GET | `/api/v1/todo-items` | 待办列表，支持筛选（见下） |
| GET | `/api/v1/todo-items/{id}` | 待办详情，含子任务 |
| POST | `/api/v1/todo-items` | 新建待办 |
| PUT | `/api/v1/todo-items/{id}` | 整体替换（不含 `parentId`） |
| POST | `/api/v1/todo-items/{id}/status` | 改状态，后端同步 `completedAt` |
| DELETE | `/api/v1/todo-items/{id}` | 删除待办（子任务一并删除） |
| POST | `/api/v1/todo-items/{id}/tags` | 挂标签，幂等 |
| DELETE | `/api/v1/todo-items/{id}/tags/{tagId}` | 摘标签，幂等 |
| GET | `/api/v1/tags` | 标签列表 |
| POST | `/api/v1/tags` | 新建标签 |
| PUT | `/api/v1/tags/{id}` | 整体替换 |
| DELETE | `/api/v1/tags/{id}` | 删除标签（关联行一并清理，待办不受影响） |

**待办筛选参数**（都是可选的）：

```
GET /api/v1/todo-items?listId=3&statuses=TODO&statuses=IN_PROGRESS&priority=HIGH&keyword=文档
```

| 参数 | 说明 |
|---|---|
| `listId` | 某个清单 |
| `unassigned=true` | 只看未归类（`list_id IS NULL`） |
| `parentId` | 某个父任务下的子任务 |
| `tagId` | 带某个标签 |
| `statuses` | 状态白名单，重复参数表达集合 |
| `priority` | 优先级 |
| `dueBefore` | 截止时间早于等于该时刻 |
| `keyword` | 标题关键字，不区分大小写，`%` 和 `_` 按字面处理 |

> `statuses` 用集合而不是单值，是为了能表达「未完成的」这个最常见的诉求。
> 单值参数做不到，只能再加一个 `excludeCompleted` 开关，两个参数语义还互相重叠。
>
> `unassigned` 也不能靠 `listId` 传 null 表达 —— 那个 null 的含义是「不按清单过滤」，
> 两个语义挤在一个可空字段里，前端就没法请求收件箱了。

### 认证

| 控制器 | 路由 | 凭证方案 | 面向 |
|---|---|---|---|
| `CookieAuthController` | `/api/v1/auth/*` | Cookie | 同站浏览器 |
| `TokenAuthController` | `/api/v2/auth/*` | JWT + Refresh Token | SPA / API 客户端 |

| 方法 | 路径 | 认证 | 说明 |
|---|---|---|---|
| POST | `/api/v1/auth/register` | 匿名 | 注册并建立 Cookie 会话 |
| POST | `/api/v1/auth/login` | 匿名 | Cookie 登录 |
| POST | `/api/v1/auth/logout` | Cookie | 清除认证 Cookie |
| POST | `/api/v1/auth/google` | 匿名 | Google 登录，建立 Cookie 会话 |
| POST | `/api/v2/auth/register` | 匿名 | 注册并签发 Access + Refresh Token |
| POST | `/api/v2/auth/login` | 匿名 | JWT 登录 |
| POST | `/api/v2/auth/refresh` | 匿名 | Refresh Token 轮换 |
| POST | `/api/v2/auth/logout` | JWT | 吊销 Refresh Token |
| POST | `/api/v2/auth/google` | 匿名 | Google 登录，签发令牌 |
| GET | `/api/v1/account/me` | 需登录 | 当前用户资料 |
| POST | `/api/v1/account/google` | 需登录 | 绑定 Google 账号 |
| POST | `/api/v1/email-verification/verify` | 匿名 | 消费邮件里的令牌，回填邮箱已验证 |
| POST | `/api/v1/email-verification/resend` | 匿名 | 重发验证邮件（邮箱不存在也返回成功） |
| GET | `/api/v1/common/config` | 匿名 | 公开配置（Google ClientId） |

### 账号合并规则

Google 登录按以下顺序匹配，避免同一个人出现两个账号：

1. `external_logins` 里按 `(google, sub)` 找绑定 → 命中即登录（`sub` 不可变，是唯一可靠的身份依据）
2. 未命中绑定，但邮箱能匹配到**已验证**邮箱的本地账号 → 合并，补写一条绑定关系
3. 邮箱匹配到的本地账号邮箱**未验证** → 拒绝自动合并并返回 409
4. 都没有 → 新建用户（无本地密码，只能走 Google 登录）

第 3 条是刻意的：否则任何人抢先用别人的邮箱注册本地账号，
就能在对方用 Google 登录时把对方的 Google 账号劫持进自己创建的账号里。

这个 409 的正当出路就是**邮箱验证**：验证过之后 `email_verified_at` 有值，
第 2 条自动生效，用户不需要再手动走绑定接口。

### 邮箱验证

注册后自动发一封验证邮件，链接指向前端 `/verify-email?token=...`。
用户点进去，页面拿 token 换 `POST /api/v1/email-verification/verify`，成功即回填 `email_verified_at`。

三个刻意的决定：

- **令牌只存哈希**（`Base64(SHA-256)`），和 Refresh Token 同一套做法。明文只在邮件里出现一次。
- **验证接口幂等**。同一个链接重复验证返回成功，不报错 —— 浏览器会预取链接，
  React 的 StrictMode 也会让 effect 跑两遍。不这么做，用户会看到「链接已失效」而实际上什么都没做错。
- **重发接口不泄漏账号是否存在**。邮箱没注册、已验证、或还在冷却期内，一律返回成功；
  真发信了也返回成功。匿名可调的端点上，响应里的任何差异都是账号探测接口。

未验证不阻断登录 —— 只是登录后顶部会挂一条横幅提示可以重发。
真实产品里也有「未验证就不让用」的做法，那样会让「注册即登录」变成
「注册 → 退出 → 验证 → 再登录」，对练习项目没必要。

> 这套流程目前**没有自动化测试覆盖** —— 下面那套 39 项端到端测试是在它之前写的。

### 健康检查

`GET /healthz`（**匿名**），返回 UI 格式的 JSON，带每个检查项的名字、状态和描述：

| 检查项 | 验的是什么 |
|---|---|
| `test_health_check` | 管道自检（**随机**返回健康/不健康，见下） |
| `api_health_check` | 外网可达性（`https://baidu.com`） |
| `database_health_check` | EF Core 能不能真的连上 PostgreSQL |

> `test_health_check` 是**随机**返回健康/不健康（`Random.Shared.Next(2)`），
> 早期用来验证看板能不能正确变色。接上看板之后，看板每 30 秒轮询一次、
> 每次都是一次抛硬币，状态会一直闪。想安静看真实指标的话，把
> `CustomHealthCheckExtensions.cs` 里那个 `.AddCheck<TestCustomHealthCheck>(...)` 删掉即可。

**刻意不加 `RequireAuthorization()`。** 探针拿不到凭据 —— 负载均衡、容器编排、
以及下面那个健康看板，都只能裸调这个地址；加了授权它们一律拿 401，
然后全部报不健康。代价是响应里带各检查项的名字和描述，等于对外承认「哪个依赖挂了」。
生产环境要么把它限制在内网，要么改成带密钥的路径再放开。

---

## 快速开始

### 1. 数据库

需要一个 PostgreSQL，默认连接串在 `TodoSystem.Web/appsettings.json`：

```
server=127.0.0.1;port=5432;database=todo_system;userid=plotweaver;password=plotweaver;
```

### 2. 迁移

应用启动时会自动 `MigrateAsync()`。模型改动后重新生成：

```bash
cd /home/mwjz/code/net/TodoSystem

dotnet ef migrations add <名字> \
  --project TodoSystem.EntityFrameworkCore \
  --startup-project TodoSystem.Web
```

> **邮箱验证引入的 `email_verification_tokens` 表还没有迁移文件**，
> 第一次跑之前要先补上，否则注册时写令牌会报表不存在：
>
> ```bash
> dotnet ef migrations add AddEmailVerificationTokens \
>   --project TodoSystem.EntityFrameworkCore \
>   --startup-project TodoSystem.Web
> ```

> 想确认「模型和迁移是否一致」：
> `dotnet ef migrations has-pending-model-changes --project TodoSystem.EntityFrameworkCore --startup-project TodoSystem.Web`

### 3. 配置 Google 凭证

`ClientId` 是公开值，写在 `appsettings.json` 里（前端也要用）。
`ClientSecret` 不要写进配置文件，用用户机密：

```bash
cd TodoSystem.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "<你的 secret>"
```

> 注意：当前 `appsettings.json` 里仍然留着一个明文的 `ClientSecret`，需要清掉。
> 另外 ID Token 直传流程其实不需要 ClientSecret，只有将来接服务端授权码流程才用得上。

### 4. 配置邮件（邮箱验证）

`appsettings.json` 里 `Provider = File`：邮件不真发，而是写成 `.html` 落到
`TodoSystem.Web/emails/`，同时把链接打进控制台日志。所以什么都不配也能把整条
验证链路跑通 —— 注册完看一眼后端控制台，点那个链接就行。

`appsettings.Development.json` 已经切到真实 SMTP（163），开发环境默认走真发信。
**只需要补一个授权码**：

```bash
cd TodoSystem.Web
dotnet user-secrets set "Email:Smtp:Password" "<163 授权码>"
```

`Provider` 只认 `File` 和 `Smtp` 两个值，写错会在启动时直接抛异常。
以前是静默回落到 `File`，像 `FileSmtp` 这种手滑会表现成「配了但没生效」，
排查起来很费时间。

#### 163 的端口只能用 25，不能用 465

这是最容易踩的一脚。`System.Net.Mail.SmtpClient` 的 `EnableSsl` 实现的是
RFC 3207 的 **STARTTLS** —— 明文连上去，再用 `STARTTLS` 命令升级成加密。
而 163 的 465 / 994 是**隐式 TLS**：TCP 一连上就要求 TLS 握手。

两者对不上，表现是**干等到超时**，报错里看不出跟端口有关。所以：

| 端口 | 加密方式 | 能否用 SmtpClient |
| --- | --- | --- |
| 25 | STARTTLS | 可以，配 `UseStartTls: true` |
| 465 / 994 | 隐式 TLS | 不行，代码里已直接拦掉并给出提示 |
| 587 | — | 163 没开这个端口，连上不返回 banner |

一定要走 465 就只能换 MailKit（会多两个 NuGet 包）。现在这套零新依赖，够用。

> 出站 25 端口在国内家宽上通，但**阿里云 / 腾讯云等云主机默认封 25 出站**，
> 要单独提工单解封。跑到云上如果发不出去，先怀疑这个。

#### 授权码怎么拿

1. 登录 <https://mail.163.com>，右上角「设置」→「POP3/SMTP/IMAP」
2. 开启「SMTP 服务」，按提示用手机短信验证
3. 弹出的那串 16 位字符就是授权码

> 密码字段要填**授权码**，不是邮箱登录密码。`FromAddress` 也必须和
> `UserName` 是同一个地址，否则 163 会以 `553 Mail from must equal authorized user` 拒收。

### 5. 启动后端

```bash
cd /home/mwjz/code/net/TodoSystem
dotnet run --project TodoSystem.Web --launch-profile http
```

监听 `http://localhost:15241`。开发环境可访问 Scalar 文档：`http://localhost:15241/scalar/v1`。

### 6. 启动前端

```bash
cd TodoSystem.WebClient
npm install
npm run dev
```

打开 `http://localhost:5173`。Vite 已把 `/api` 代理到 `http://localhost:15241`，
浏览器视角是同源请求，因此不需要处理 CORS，Cookie 也不受 SameSite 影响。
后端也就没有 CORS 中间件 —— `app.UseCors()` 配不到 `AddCors` 会直接让进程起不来，
所以这里是刻意不装，而不是漏装。

> 这个设计有个前提：**生产环境前端也必须和后端同源**。
> `utils/http.ts` 里的接口路径全是相对路径（`/api/...`），
> 部署时得靠反向代理（nginx 等）把 `/api` 指到后端。
> 如果将来把前端放到另一个域名，光加 CORS 不够，还得先给 `http.ts` 加一个可配的 base URL。

### 7. 启动健康检查看板（可选）

`TodoSystem.HealthDashboard` 是一个**独立进程**，只做一件事：定时轮询后端 `/healthz`，
把结果画成网页。它不引用解决方案里的任何其他项目，后端挂了它照样能起来 ——
这正是「看板」该有的性质，不然它就是跟自己一起倒的那个东西。

```bash
cd /home/mwjz/code/net/TodoSystem
dotnet run --project TodoSystem.HealthDashboard --launch-profile http
```

打开 <http://localhost:15242/dashboard#/healthchecks>。后端没起来时页面会一直显示不健康，
属于预期现象。

要监控的后端地址写在 `TodoSystem.HealthDashboard/appsettings.json` 的
`HealthCheckEndpoints` 里（键名就是看板上显示的名字，可以配多个）：

```json
"HealthCheckEndpoints": {
  "TodoSystem": "http://127.0.0.1:15241/healthz"
}
```

> 写进配置而不是硬编码，是因为**本机跑和容器里跑，API 的地址不一样**：
> 容器里得写服务名（`http://todosystem-web:8080/healthz`），
> 或者用环境变量 `HealthCheckEndpoints__TodoSystem=...` 覆盖。

轮询间隔 30 秒；失败通知最短 60 秒一条，避免一个持续故障把通知刷爆。
状态只存**内存**（`AddInMemoryStorage`），进程重启后历史清空 —— 练习项目不需要持久化，
真要留痕就换成 `AddSqlServerStorage` / `AddPostgreSqlStorage`。

> 这个项目**没有配 HTTPS 重定向**。看板只绑本机/内网，没配 HTTPS 端口时
> `UseHttpsRedirection()` 会每次请求都打一条「无法确定 https 端口」的警告。
> 真要对外暴露，TLS 应该终结在反向代理上。

---

## 联调种子账号

首次启动且 `users` 表为空时写入：

| 用户名 | 密码 | 角色 |
|---|---|---|
| ADMIN | 123 | ADMIN |
| USER | 123 | USER |

**默认只在开发环境写。** 生产库首次启动如果照写，等于凭空建两个弱口令管理员账号，
所以 `Program.cs` 里按 `app.Environment.IsDevelopment()` 判断。

要在别的环境也写（比如演示用的 staging），用环境变量显式打开：

```bash
Seed__Enabled=true
```

> 这个键只认 `true` / `false`。写别的值（比如 `yes`）会在启动时直接抛异常，
> 刻意不静默回落 —— 一个拼错的开关悄悄失效，比启动失败难查得多。

---

## 生产环境要覆盖的配置

`appsettings.json` 是基础配置，会被提交进仓库。
生产环境的敏感项和差异项一律走环境变量（**双下划线**表示配置层级），
不要写进任何会进仓库的文件。

| 环境变量 | 说明 | 不设的后果 |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 默认就是它，显式写上更清楚 |
| `ConnectionStrings__PgSql` | 数据库连接串 | 会用仓库里的本地连接串 |
| `Jwt__SecretKey` | 签名密钥，≥ 32 字节随机串 | 用仓库里硬编码的开发密钥，**任何人都能伪造 token** |
| `Authentication__Google__ClientSecret` | Google OAuth 密钥 | 用仓库里那个已泄漏的值 |
| `Email__Smtp__Password` | 163 授权码 | 认证失败 —— **user-secrets 在生产环境不加载** |
| `Email__WebClientBaseUrl` | 前端地址，必须 `https` | 验证邮件里的链接指向 `localhost` |
| `Seed__Enabled` | 是否写种子账号 | 生产默认不写（这是对的，一般不用设） |

> `Email__Smtp:Password` 为什么不能靠 user-secrets：那是开发专用的配置源，
> 只在 `ASPNETCORE_ENVIRONMENT=Development` 时才挂载。

健康看板是独立进程，配置也独立：生产环境把 `appsettings.json` 里
`HealthCheckEndpoints:TodoSystem` 指回内网真实地址即可（或用
`HealthCheckEndpoints__TodoSystem` 覆盖）。它**不含任何密钥**，
所以没有必须走环境变量的项。

---

## 本次重构：从「考生管理」演示到真正的待办领域

重构前项目名叫 TodoSystem，但**没有任何待办表** ——
它实际上是一个认证骨架（`users` / `external_logins` / `refresh_tokens`）
外加一张只用来演示授权的 `students` 表。

### 被替换掉的 `students` 表

| 问题 | 说明 |
|---|---|
| **没有归属列** | 表里只有 `id` / `name`，没有 `user_id`。所有用户看到的是同一份全局数据，任何登录用户都能读到全部行。这是最根本的结构问题 —— 待办应用的核心实体必须归属于用户。 |
| **零索引零约束** | 除了主键什么都没有：没有业务键、没有外键、没有 CHECK。 |
| **演示代码是破坏性的** | `StudentService.GenerateAsync` 每次调用都软删除**全部**考生再重新生成；而全局查询过滤器会隐藏这些行，于是表里堆着一批批看不见的软删除数据，只增不减。 |
| **授权模型错位** | 靠 `ADMIN` / `USER` 角色区分读写，而待办领域的权限边界应该是「归属」而不是「角色」。 |

`students` 表、`StudentEntity`、`IStudentService` / `StudentService`、
`StudentController`，以及前端的 `apis/student.ts` / `hooks/use-students.ts` 已全部移除。

### 顺带修掉的既有缺陷

| 位置 | 问题 | 处理 |
|---|---|---|
| `TodoSystem.EntityFrameworkCore/Interceptor/AuditSaveChangesInterceptor.cs` | **死代码 + 重复实现**：这个拦截器从未在 DI 里注册过，而 `TodoSystemDbContext.ApplyAuditChanges()` 在做一模一样的事（写 `CreatedAt` / `UpdatedAt` / `DeletedAt`）。一旦有人把拦截器注册上，审计时间会被写两遍。文件还忘了写 namespace。 | 删除拦截器，保留 `DbContext` 里的实现（已经接好线，改动面最小） |
| `AuthService` | `IsUniqueViolation` 是私有方法，新服务要用就得复制一遍 | 提到 `Application/Common/DbErrors`，`AuthService` 一并改用 |
| `TodoListService` / `TodoItemService` | `OrderBy` 写在投影之后，EF 会把排序折进 `Select` 变成对 DTO 属性排序，运行时 `could not be translated` | 排序一律放在投影之前（已由端到端测试覆盖） |
| `TodoItemTagEntity` | 关联行不在软删除体系里，会返回「标签已删除」的关联行，`Include(x => x.Tag)` 拿到 null 而导航声明非空 | 补上配对查询过滤器，消除 EF 启动警告并修正语义 |

### 验证

重构后跑了两层验证：

- **端到端接口测试**（39 项）：认证、清单 / 待办 / 标签 CRUD、子任务层级限制、
  幂等挂标签、状态流转与 `completedAt` 同步、各类筛选（含 `%` 通配符转义）、
  **跨用户数据隔离**（读 / 写 / 挂载全部拒绝）、删除语义（软删除、删清单退回收件箱、删标签保留待办）。
- **数据库约束测试**（13 项）：绕过 API 直接写库，确认 CHECK 约束、复合外键、
  部分唯一索引确实在数据库层生效 —— 例如把 A 的待办改挂到 B 的清单上会被
  `fk_todo_items_todo_lists_user_id_list_id` 拒绝。

### 前端

`DashboardPage` 从「考生列表」改成真正的待办界面。
`http` 工具补上了 `put` / `delete`（原来只有 `get` / `post`）。

**组件与 Hook 分工**

| 文件 | 职责 |
|---|---|
| `hooks/use-todo-items.ts` | 待办的全部数据逻辑。`TodoScope`（范围：清单 / 未归类）+ `TodoFilters`（关键字 / 优先级 / 标签）两个独立入参 |
| `hooks/use-todo-lists.ts` | 清单加载、新建、改名 / 归档、删除；`includeArchived` 是服务端参数 |
| `hooks/use-tags.ts` | 标签加载、新建（带颜色）、改名 / 改色、删除 |
| `hooks/use-debounced-value.ts` | 搜索框防抖，避免逐字打出多个请求、以及乱序响应覆盖 |
| `components/TodoListSidebar.tsx` | 清单导航：双击改名、归档 / 恢复、「显示已归档」开关、删除 |
| `components/TodoItemRow.tsx` | 单行待办：勾选、状态切换、标签、截止日期、子任务、编辑表单、删除 |
| `components/TagManager.tsx` | 标签管理：固定调色板取色、行内改名改色、删除 |
| `hooks/use-verify-email.ts` | 验证页逻辑：进入即用 URL 里的 token 换结果 |
| `components/EmailVerificationBanner.tsx` | 邮箱未验证时的顶部提示条 + 重发入口，挂在 `AppLayout` 上 |

**几个刻意的取舍**

- **范围与筛选拆成两个入参**，不是一个「大对象」。两者正交：切清单不该重置筛选，
  反之亦然。混在一个对象里，「切清单时关键字被清空」这类 bug 几乎必然出现。
- **查询条件 memo 在基本类型上**（`listId` / `unassigned` / `keyword` / `priority` / `tagId`），
  而不是 memo 在 `scope` / `filters` 对象上。页面每次渲染都会新建这两个对象，
  直接进依赖数组会让 `reload` 每帧都变、`useEffect` 跟着重跑，请求无限循环。
- **关键字单独存一份未防抖的输入值**，真正发给后端的条件在渲染时合并，
  而不是用 `useEffect` 把防抖值「同步」回 state —— 后者多一次渲染，
  而且中间态（state 已更新、请求还没发）很容易写出 bug。
- **每次写操作后整体 reload**，不在本地拼状态。状态的真相在后端
  （`completedAt` 由后端维护、`isOverdue` 由后端算、删父任务会牵连子任务），
  本地维护副本迟早不一致。
- **子任务在前端按 `parentId` 分组**，不再发第二轮 `parentId` 查询：一次请求拿全量，
  也避免「父任务还没回来、子任务先到」的拼装顺序问题。若命中的子任务其父任务
  没命中（搜索时常见），把它当顶层渲染，保证「搜到什么就能看到什么」。
- **统计卡片只算顶层待办**。把子任务也算进「未完成」会让数字虚高 ——
  一个拆成五步的任务变成五个，数字越大越不可信。
- **验证页不做「只请求一次」的前端防抖**。StrictMode 下 effect 本来就会跑两遍，
  与其在前端用 ref 挡住（还得额外处理 token 变化时 ref 失效），
  不如让后端把重复验证做成幂等的 —— 一行 `if` 换掉一类前端时序 bug。
- **状态切换只给两态按钮**（待开始 ↔ 进行中），三态循环不做：
  从「进行中」误点一下变成「已完成」是误操作，而「已完成」已经有复选框这个更直觉的入口。
- **行内动作按「动作集合」传一个对象**（`TodoItemActions`），不逐个透传：
  九个回调加上数据字段要接十四个 props，真正的数据字段会被淹没在回调里。
- **`toUpdateInput()` 铺开当前值再盖补丁**：后端是 PUT 整体替换语义，
  只传 `name` 会把 `isArchived` 重置成 false —— 改名顺手把清单「取消归档」了。

### 前端验证

`tsc --noEmit` 与 `vite build` 均通过（88 modules transformed，CSS 21.6 KB / JS 305 KB）。
