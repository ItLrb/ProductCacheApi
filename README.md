# Product Cache API

REST API built with **.NET 8** focused on backend best practices: distributed caching,
structured logging and a consistent HTTP contract. Product management with a Redis
cache in front of MySQL for read performance.

## 🚀 Tech stack

- **.NET 8** (ASP.NET Core)
- **Entity Framework Core 8** + **Pomelo** (MySQL provider)
- **MySQL** (database)
- **Redis** (distributed cache, optional)
- **Docker & Docker Compose**
- **Serilog** (structured logging)
- **Swagger / OpenAPI**
- **xUnit** (unit + integration tests)

## 📌 Features

- ✅ Product CRUD with **paged** listing
- ✅ Redis caching with automatic invalidation on writes
- ✅ **Graceful cache degradation** — the API runs with an in-memory cache when Redis is
  not configured, and fails fast (no ~5s hangs) when a configured Redis is temporarily down
- ✅ Input validation with Data Annotations
- ✅ Consistent error responses via **ProblemDetails** (RFC 9457)
- ✅ Structured logs (console + daily rolling file)
- ✅ `/health` endpoint — checks the database; reports **Degraded** (still `200`) when the
  cache is down, since the cache is optional
- ✅ Swagger UI (Development)

## 🧱 Architecture

The project uses a **feature-based (vertical slice) layout**:

```
ProductCacheApi/
├── Features/
│   ├── Products/                  # Product slice
│   │   ├── ProductController.cs   # HTTP endpoints
│   │   ├── ProductService.cs      # Business logic + caching
│   │   ├── Product.cs             # Domain entity
│   │   ├── Result.cs              # Result pattern for write operations
│   │   ├── CacheResult.cs         # Wraps a value + cache-hit flag
│   │   └── DTOs/                  # Request/response contracts
│   └── Cache/
│       ├── ICacheService.cs
│       └── RedisCacheService.cs
├── Config/
│   ├── AppDbContext.cs            # EF Core context
│   ├── GlobalExceptionHandler.cs # IExceptionHandler -> ProblemDetails
│   └── Migrations/
├── Program.cs                    # Composition root
├── compose.yaml                  # MySQL + Redis + API
└── tests/
    └── ProductCacheApi.Tests/    # xUnit unit + integration tests
```

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (recommended) **or** a local MySQL 8 (MariaDB works too) + Redis

```bash
git clone https://github.com/MiguelLopesDel/Product-Cache-Api.git
cd Product-Cache-Api
```

## 🐳 Running with Docker (recommended)

`compose.yaml` brings up **MySQL, Redis and the API** together, wired via healthchecks so
the API only starts once the database is ready. **Migrations are applied automatically on
startup** (`ApplyMigrationsAtStartup=true`), so the database is ready with no manual step.

```bash
cp .env.example .env      # adjust MYSQL_ROOT_PASSWORD etc.
docker compose up -d --build
```

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger` (Development environment)
- Health: `http://localhost:8080/health`

That's it — `curl http://localhost:8080/api/Product` returns `[]` on a fresh database.

## 🛠️ Running locally (without Docker)

You need a running MySQL/MariaDB and (optionally) Redis. Configuration is read from
user-secrets / environment variables — **no secrets are stored in the repository**. The
database connection string is required; Redis is optional (an in-memory cache is used when
it is absent).

```bash
# 1. Create the database (and optionally a dedicated user)
mysql -u root -p -e "CREATE DATABASE productcachedb;"

# 2. Install the EF Core CLI tool (once per machine)
dotnet tool install --global dotnet-ef

# 3. Configure the connection (stored outside the repo via user-secrets)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "server=localhost;port=3306;database=productcachedb;user=root;password=your-password"

# 4. (Optional) enable Redis
dotnet user-secrets set "Redis:Connection" "localhost:6379"

# 5. Apply migrations, then run
dotnet ef database update
dotnet run
```

The app is served at `http://localhost:5149` (see `Properties/launchSettings.json`);
Swagger at `http://localhost:5149/swagger`.

> If `ConnectionStrings:DefaultConnection` is missing, the app fails fast at startup with a
> clear message instead of returning confusing per-request errors.

## 📚 API

Base URL: `http://localhost:8080/api/Product` (or `:5149` when running locally).

| Method   | Endpoint                            | Description             | Success |
|----------|-------------------------------------|-------------------------|---------|
| `GET`    | `/api/Product?page=1&pageSize=20`   | List products (paged)   | `200`   |
| `GET`    | `/api/Product/{id}`                 | Get product by id       | `200`   |
| `POST`   | `/api/Product`                      | Create a product        | `201`   |
| `PUT`    | `/api/Product/{id}`                 | Update a product        | `200`   |
| `DELETE` | `/api/Product/{id}`                 | Delete a product        | `204`   |

### Response contract

Single resources are returned **directly** with the correct HTTP status code; the list
endpoint returns a small **paged envelope**. Timestamps are always serialized as UTC
(ISO-8601 with a trailing `Z`).

The cache origin of `GET` responses is exposed via the **`X-Cache`** response header
(`HIT` when served from cache, `MISS` when loaded from the database) instead of leaking a
field into the body.

**`GET /api/Product?page=1&pageSize=20`** → `200 OK`, header `X-Cache: MISS`
`page` defaults to `1`, `pageSize` to `20` (clamped to a maximum of `100`).
```json
{
  "items": [
    { "id": 1, "name": "Sample Product", "price": 99.99, "stock": 50, "createdAt": "2026-01-04T00:00:00Z" }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

**`POST /api/Product`** → `201 Created` (with `Location` header)
```json
{ "id": 2, "name": "New Product", "price": 149.99, "stock": 100, "createdAt": "2026-01-04T00:00:00Z" }
```

**`PUT /api/Product/1`** → `200 OK` (returns the updated product)
**`DELETE /api/Product/1`** → `204 No Content` (empty body)

### Errors

All errors use the standard **ProblemDetails** shape (`application/problem+json`).

Validation error (`400`):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Price": ["The field Price must be between 0.01 and ..."] }
}
```

Not found (`404`):
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Product with ID 1 not found"
}
```

## 📊 Caching

- List cache key: `products:all` (TTL 5 min)
- Per-item cache key: `product:{id}` (TTL 5 min)
- Cache is invalidated on Create / Update / Delete
- Cache is best-effort: a Redis outage is logged and the request continues against the database

## 📝 Logging

Serilog writes to the console and to `Logs/log-YYYYMMDD.txt` (daily rolling).

## 🧪 Tests

```bash
dotnet test
```

The `tests/ProductCacheApi.Tests` project contains:
- **Unit tests** for `ProductService` (EF Core InMemory + a fake cache) covering CRUD and
  cache invalidation.
- **Integration tests** driving the real HTTP pipeline via `WebApplicationFactory`
  (status codes, `X-Cache` header, ProblemDetails) with the database swapped for InMemory.

## 🔄 CI

GitHub Actions (`.github/workflows/ci.yml`) restores, builds and tests on every push and
pull request to `main`.

## 🔒 Security note

- No secrets are committed. Local configuration uses **user-secrets**; Docker uses the
  git-ignored `.env` file (see `.env.example`).
- ⚠️ **Historical exposure:** earlier commits in this repository contained a database
  password (`.env`, `appsettings*.json`). That credential must be considered compromised
  and **rotated**. The value still exists in the git history; purging it would require a
  history rewrite (`git filter-repo`) and a force-push, which is intentionally left as an
  explicit decision for the repository owner.

### Recommended for production

- [ ] Authentication / authorization
- [ ] HTTPS + HSTS
- [ ] Rate limiting
- [ ] CORS policy
- [ ] Readiness vs. liveness health checks

## 📄 License

MIT — see `LICENSE`.

## 👤 Author

**Italo** — [@ItLrb](https://github.com/ItLrb)
