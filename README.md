# Pulse API

Backend for **Pulse** — a private app for two people to exchange tiny signals (moods, needs, thoughts) throughout the day. ASP.NET Core (.NET 10) MVC API, PostgreSQL via Supabase, orchestrated locally with .NET Aspire.

This is the MVP backend: Supabase authentication + user provisioning, partner **connections** (invite-code pairing), and **pulses** (Mood / Need / Thought) with a timeline. PulseTouch (drawing), realtime, widgets, and push are deferred.

## Running locally

```bash
aspire start   # or: dotnet run --project src/Pulse.Api.AppHost
```

Requires Docker. The AppHost boots a full local Supabase stack ([Nextended.Aspire.Hosting.Supabase](https://www.nuget.org/packages/Nextended.Aspire.Hosting.Supabase)) — Postgres (54322), GoTrue auth, Kong gateway (8000), Studio (54323) — plus the API. Three confirmed dev users are pre-registered (`dom@pulse.dev` / `mike@pulse.dev` / `sarah@pulse.dev`, password `Pulse123!`). EF Core migrations are applied automatically on API startup in Development.

Get a token for manual API calls:

```bash
curl -s http://localhost:8000/auth/v1/token?grant_type=password \
  -H "apikey: <anon key from Aspire dashboard>" \
  -H "Content-Type: application/json" \
  -d '{"email":"dom@pulse.dev","password":"Pulse123!"}'
```

## Architecture

**MVC, layered**: `Controllers → Services → EF Core`. Controllers are thin (auth + routing + DTO shaping); domain behaviour lives in `Services/`. Requests are validated by FluentValidation via a global `ValidationFilter`, and domain errors (`NotFoundException`, `ForbiddenException`, `ConflictException`, `DomainRuleException`) are translated to RFC 9457 problem details by `ApiExceptionHandler`.

```
src/Pulse.Api.ApiService/
  Controllers/      Connections, Pulses, Devices
  Services/         ConnectionService, PulseService, DeviceService
  Domain/           User, UserDevice, Connection, Pulse (+ PulseMood/Need/Thought), enums
  Data/             PulseDbContext, entity configurations, migrations
  Auth/             Supabase JWT validation, ICurrentUser, user provisioning,
                    avatar storage (Supabase Storage)
  Notifications/    FCM push pipeline (FirebaseAdmin)
  Contracts/        Request/response DTOs
  Validation/       FluentValidation global filter
```

### Domain model

| Table | Notes |
|---|---|
| `users` | Id **is** the Supabase Auth user id (`sub` claim) — no separate mapping table. Account deletion tombstones the row (`DisplayName` → "Deleted user", `deleted_at` set). |
| `user_devices` | FCM tokens + device details for push. Token is unique and re-homes on account switch. |
| `connections` | The private link between two people. `UserA` is the inviter; `UserB` is null until the invite is accepted. `Pending → Active`, or `Cancelled`. A user has at most one non-cancelled connection. `invite_code` is uniquely indexed while outstanding (filtered), cleared on accept. |
| `pulses` | A single signal from one partner to the other. `Type` (Mood/Need/Thought/Touch) selects which **detail table** carries the payload — no generic JSON. Append-only; the timeline. Indexed `(connection_id, created_at)`. |
| `pulse_moods` / `pulse_needs` / `pulse_thoughts` | 1:1 detail rows keyed by `pulse_id`. Mood/Need are fixed enums; Thought is free text ≤ 50 chars. (`pulse_touches` deferred.) |

All enums are stored as strings, all names are snake_case (`EFCore.NamingConventions`), GUIDs are v7 for index locality. **Row Level Security is not yet applied** — the API connects as the `postgres` role (bypassing RLS); add deny-all RLS policies (own profile / own connection / own connection's pulses) as defence in depth before exposing the Supabase REST surface.

### Auth (Supabase)

GoTrue issues HS256 JWTs; the API validates them locally against the project JWT secret (no network hop). Locally the AppHost injects `Supabase__JwtSecret` from the container stack; in production set it from *Dashboard → Settings → API*. `MapInboundClaims` is off, so `sub`/`email`/`role` arrive unmangled, and `ICurrentUser` reads display name / avatar from `user_metadata`. Since Supabase owns sign-up, there's no register endpoint — `UserProvisioningMiddleware` creates the profile row on a user's first authenticated request (memory-cached afterwards). Tombstoned accounts are rejected here while their JWT is still valid.

### Push notifications (Firebase)

The MAUI app calls `PUT /api/devices` on launch with the FCM token + device details, and `DELETE /api/devices/{token}` on sign-out. `IPushNotificationSender` has an FCM implementation (FirebaseAdmin, multicast, dead-token pruning) and a logging no-op used when no Firebase credentials are configured — so local dev needs no Firebase project. Enable it by setting `Firebase:CredentialsFile` (service-account JSON path) or `GOOGLE_APPLICATION_CREDENTIALS`.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/connection` | Current connection (pending/active), or 204 |
| POST | `/api/connection/invite` | Create a pending connection; returns the invite code to share |
| POST | `/api/connection/accept` | Join a partner's connection by code (`{"inviteCode":"ABC123"}`) |
| DELETE | `/api/connection` | Disconnect (cancel current connection) |
| GET | `/api/pulses?before=&limit=` | Timeline, newest first (cursor paged) |
| GET | `/api/pulses/latest` | Most recent pulse (Home), or 204 |
| POST | `/api/pulses/mood` | Send a mood (`{"moodType":"Loved"}`) |
| POST | `/api/pulses/need` | Send a need (`{"needType":"Hug"}`) |
| POST | `/api/pulses/thought` | Send a thought (`{"message":"Thinking of you ❤️"}`, ≤ 50 chars) |
| PUT | `/api/devices` | Register/refresh FCM token |
| DELETE | `/api/devices/{token}` | Unregister device |

A `UsersController` (`GET/PUT/DELETE /api/users/me`) is not yet implemented — the `User` entity, `ICurrentUser`, avatar storage, and `UserProvisioningMiddleware` are in place for it. The mobile client already expects these routes (`Services/Api/PulseApiClient.cs`). PulseTouch (`POST /api/pulses/touch`) is deferred.

## Migrations

```bash
cd src/Pulse.Api.ApiService
dotnet ef migrations add <Name> --output-dir Data/Migrations
dotnet ef migrations script -o Data/Migrations/<Name>.sql   # plain SQL for prod deploys
```

Applied automatically on startup in Development and on the homeserver compose deployment (`Database__MigrateOnStartup=true`); for a real production target apply during deployment instead (`dotnet ef database update` or the SQL script).

## Homeserver deployment (Docker Compose)

The AppHost publishes a self-contained Compose bundle. Container names are namespaced `pulse-*` and host ports are `7088` (dbgate) / `7089` (Supabase gateway) / `7090` (API) so the stack coexists with the Tally deployment on the same Docker host. Data persists under `/opt/pulse/{db,storage,dbgate}`.

```bash
# From Pulse.Api/. Publishes to src/Pulse.Api.AppHost/aspire-output and fills its .env.
PULSE_HOMESERVER_HOST=192.168.1.50 deploy/publish.sh

# Copy the output dir to the homeserver, then:
cd aspire-output && docker compose up -d --build
```

`deploy/publish.sh` runs `aspire publish` and pre-populates the generated `.env` (which Aspire otherwise emits empty): the `/opt/pulse/*` bind mounts, the API image tag/port, and `PULSE_HOMESERVER_HOST`. The host is **required** — it's the LAN address (IP or hostname) the phone uses to reach the box, baked into client-facing avatar URLs — so the script refuses to run without it rather than guess.

**Avatar / public URLs.** The API uploads to Supabase Storage over the internal Docker network (`ConnectionStrings__supabase__Url = http://supabase-kong:8000`), but the URLs it *persists* must be reachable from a phone — a `supabase-kong` hostname is not. So `Supabase__PublicUrl` is set to `http://<PULSE_HOMESERVER_HOST>:7089`, and `SupabaseStorageClient` builds public avatar URLs from that rather than from its internal request base. Locally (`aspire start`) `PublicUrl` is unset and it falls back to the internal base, which resolves at `localhost`.

## Production deployment notes

- Point `ConnectionStrings__pulsedb` at the Supabase **connection pooler** (Settings → Database), set `Supabase__JwtSecret` and optionally `Supabase__Issuer` (`https://<ref>.supabase.co/auth/v1`) to enable issuer validation. Set `Supabase__PublicUrl` to the project's public API URL (`https://<ref>.supabase.co`).
- The API talks to Postgres directly with the `postgres` role, so Row Level Security is not in play — keep the Supabase anon/REST surface for tables out of scope, or enable RLS with deny-all policies on these tables as defence in depth.
