# HappyDayBackEnd Setup for Alpine Linux

This document explains how to set up and run the HappyDayBackEnd application on Alpine Linux.

## Prerequisites

- Alpine Linux OS
- Internet connection

## Setup Instructions

### Option 1: Using the setup script (recommended)

1. Make the setup script executable:
   ```bash
   chmod +x setup-alpine.sh
   ```

2. Run the setup script:
   ```bash
   ./setup-alpine.sh
   ```

This will:
- Install all required dependencies
- Restore .NET packages
- Build the solution

### Option 2: Manual setup

1. Install required packages:
   ```bash
   apk update
   apk add --no-cache dotnet10-sdk postgresql16-client curl wget git bash
   ```

2. Restore .NET dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

## Database Setup

The application uses PostgreSQL as its database. You need to set up PostgreSQL before running the application:

### Option 1: Using Docker (Recommended for development)

1. Start PostgreSQL (container `happyday-postgres`, matches `appsettings.Development.json`):
   ```bash
   docker compose -f docker-compose.dev.yml up -d
   ```

2. Apply the migrations (see [Database Migrations](#database-migrations) below). The `SeedTurkeyLocations` migration loads the 81 provinces (plate codes as ids, İstanbul = 34) and 973 districts. If the `Cities` table already holds provinces numbered differently (for example alphabetically), that migration stops with an error and changes nothing, so existing companies and venues are never re-linked to the wrong province.

3. The venue categories come with the `SeedCategories` migration. (`seed.sql` does the same for databases created before it; running it again changes nothing.)

4. Optional, development only: load sample companies and venues (5 fictional companies, 13 venues with photos and packages). Never run it against production. It is safe to re-run; it replaces only its own `seed-*@davetly.local` rows.
   ```bash
   docker exec -i happyday-postgres psql -U postgres -d HappyDayDb -v ON_ERROR_STOP=1 < seed-demo.sql
   ```
   The sample companies can sign in to the company panel as `seed-<name>@davetly.local` (`bogazici`, `polonez`, `ege`, `baskent`, `bodrum`) with password `Davetly2026!`. It also creates an admin user, `seed-admin@davetly.local` with the same password, which signs in through `POST /api/auth/login` (there is no admin UI yet; use Swagger). The password hash includes the pepper from your local user secrets, so this only works with the same `Security:Pepper` value.

### Option 2: Local PostgreSQL installation

1. Run the PostgreSQL setup script:
   ```bash
   ./setup-postgres.sh
   ```

### Option 3: Manual PostgreSQL setup

1. Install PostgreSQL:
   ```bash
   apk update
   apk add --no-cache postgresql16 postgresql16-dev
   ```

2. Initialize and start PostgreSQL:
   ```bash
   adduser -D -H postgres
   mkdir -p /var/lib/postgresql/data
   chown -R postgres:postgres /var/lib/postgresql
   su - postgres -c "initdb -D /var/lib/postgresql/data"
   mkdir -p /run/postgresql
   chown -R postgres:postgres /run/postgresql
   echo "unix_socket_directories = '/run/postgresql'" >> /var/lib/postgresql/data/postgresql.conf
   su - postgres -c "pg_ctl -D /var/lib/postgresql/data -l /var/lib/postgresql/data/logfile start"
   ```

3. Create database and user:
   ```bash
   su - postgres -c "psql -c \"CREATE DATABASE happydaydb;\""
   su - postgres -c "psql -c \"ALTER USER postgres WITH PASSWORD 'postgres';\""
   su - postgres -c "psql -c \"GRANT ALL PRIVILEGES ON DATABASE happydaydb TO postgres;\""
   ```

## Database Migrations

The EF Core tools are pinned in `dotnet-tools.json` (dotnet-ef 10.0.14). Restore them once, then apply the migrations from the repository root:

```bash
dotnet tool restore
dotnet ef database update -p Infrastructure/HappyDay.Persistance -s Presentation/HappyDay.Api
```

The `AccountSecurity` migration adds a case-insensitive unique index on the e-mail of active users and companies. If the database already holds two active accounts with the same e-mail (ignoring case), it stops with an error that lists them and changes nothing; deactivate the extra accounts and run it again. It also turns `Users.IdentityNo` into a nullable text column (old `0` values become `NULL`).

The `AuthSessions` migration adds the session table used by the login cookies (see [Sessions](#sessions)).

The `VenueLocation` migration gives every venue its own city, district, address and coordinates, and copies each existing venue's values from its company.

## Sessions

Browser sessions use httpOnly cookies instead of a token in `localStorage`: a 15-minute access token (`JwtSettings:ExpirationInMinutes`) and a refresh token that changes on every use (`Auth:RefreshTokenDays`, 14 by default). Every access token belongs to a row in `AuthSessions`, which is checked on each request, so logging out, changing or resetting the password, or deactivating a user ends the session at once. Tokens issued before this change are rejected; users sign in once more.

Requests that change data and authenticate with the cookie must send the `X-Davetly-Client` header (CSRF protection). Clients that send `Authorization: Bearer` (Swagger) do not need it.

Cookies default to `SameSite=Strict`, which works when the frontend and the API share a site (for example `davetly.com` and `api.davetly.com`). If they are on different sites, set `Auth__CookieSameSite=None` (HTTPS only) and list the frontend in `Cors:AllowedOrigins`. When the frontend and the API are on different subdomains (`mutlugunum.com` and `api.mutlugunum.com`), set `Auth__SiteDomain=mutlugunum.com`: the non-secret `davetly.session` marker is then written for the whole site so the frontend can see it; the token cookies stay on the API's own host.

## E-mail

Verification and password-reset e-mails contain links to the frontend, built from `Frontend:BaseUrl` (`http://localhost:5173` in development). Set it to the frontend's address on the server, for example `Frontend__BaseUrl=https://davetly.com`.

Companies must confirm their e-mail before they can sign in. All e-mails (verification, password reset, and the inquiry notice sent to a company) use one SMTP setting under `Smtp` (`Host`, `Port`, `UseStartTls`, `Username`, `Password`, `FromEmail`, `FromName`). Keep the password in user secrets or `Smtp__Password`, never in `appsettings.json`. `Smtp:CheckCertificateRevocation` is `false` in `appsettings.Development.json`: on macOS, .NET cannot finish the revocation check for certificates without an OCSP address (Gmail's, for example) and the connection is refused. The certificate chain and host name are still checked; on the server the setting stays `true`. In development, when `Smtp:Host` is not set, e-mails are not sent; the API writes them to the console instead, so you can copy the verification or reset link from there. Outside development a missing SMTP setting means sending fails: registration still succeeds and the error is logged.

## Secrets

The JWT secret and the password pepper are not stored in `appsettings.json`. The API refuses to start without them.

For local development, use .NET user secrets:

```bash
cd Presentation/HappyDay.Api
dotnet user-secrets set "JwtSettings:Secret" "<at least 32 characters>"
dotnet user-secrets set "Security:Pepper" "<pepper>"
```

On the server, set these environment variables:

- `ConnectionStrings__DefaultConnection`: the PostgreSQL connection string. The API refuses to start without it. (Until now `HappyDayContext` had a hard-coded `localhost` connection that silently overrode this setting, so a server must have it set explicitly.)
- `JwtSettings__Secret`: at least 32 characters
- `Security__Pepper`: must stay the same value the existing passwords were hashed with; changing it makes every existing password fail to verify

## Running the Application

After setting up the database, you can run the application with:

```bash
cd Presentation/HappyDay.Api
dotnet run
```

The API will be available at `http://localhost:5050`, with Swagger at `http://localhost:5050/swagger`.

Endpoints follow REST naming (`api/organizations/{id}` and so on). For the old → new route mapping, see [docs/api-endpoint-degisiklikleri.md](docs/api-endpoint-degisiklikleri.md).

## Tests

`Tests/HappyDay.Tests` holds unit tests and integration tests that run the whole API in memory against a real PostgreSQL. Each run creates its own throwaway database (`happyday_test_<guid>`), applies the migrations, and drops it at the end, so the development database is never touched. Uploaded files go to a temporary folder, e-mails are captured instead of sent, and rate limiting is turned off.

```bash
docker compose -f docker-compose.dev.yml up -d   # the same Postgres used for development
dotnet test
```

The tests connect to `Host=localhost;Port=5432;Username=postgres;Password=postgres` by default; set `HAPPYDAY_TEST_DB` to use another server. They cover login and lockout, session cookies, refresh-token rotation and reuse, session revocation (logout, password change, password reset, deactivation), CSRF header checks, authorization between companies, admin endpoints, and the upload checks.

## CI

`.github/workflows/ci.yml` runs on every push and pull request:

- builds the solution and runs the tests against a PostgreSQL service container; the test results are kept as an artifact
- fails when an entity was changed without a migration (`dotnet ef migrations has-pending-model-changes`)
- builds the Docker image from `Dockerfile` without publishing it

## Deployment

Production runs on Coolify: the API from the root `Dockerfile`, the frontend from the `Dockerfile` in the [happydayfinly](https://github.com/yasinerenkovalik/happydayfinly) repo, and PostgreSQL as a Coolify database. The step-by-step guide (in Turkish) is [docs/yayin-coolify.md](docs/yayin-coolify.md); the environment variables to paste into Coolify are in [deploy/coolify-api.env.example](deploy/coolify-api.env.example).

On startup the API applies pending migrations when `Database__MigrateOnStartup=true` (provinces, districts and venue categories come with the migrations), creates the first admin from `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` if there is none yet, and answers `GET /health` (checks the database connection).

## Using Docker (Alternative)

If you prefer to run the API in Docker, build the image from the repository root and pass the secrets as environment variables:

```bash
docker build -f Presentation/HappyDay.Api/Dockerfile -t happyday-api .
```

The container listens on port `8080`.