# Pulse API

Backend scaffold for **Pulse**. ASP.NET Core (.NET 10) MVC API, PostgreSQL via Supabase, orchestrated locally with .NET Aspire.

This is the bare scaffold: Supabase authentication, user provisioning, push-notification plumbing, and device registration. The domain (controllers, services, data) is intentionally minimal and will grow with the project.

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
  Controllers/      Devices
  Services/         DeviceService
  Domain/           User, UserDevice, enums
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

All enums are stored as strings, all names are snake_case (`EFCore.NamingConventions`), GUIDs are v7 for index locality.

### Auth (Supabase)

GoTrue issues HS256 JWTs; the API validates them locally against the project JWT secret (no network hop). Locally the AppHost injects `Supabase__JwtSecret` from the container stack; in production set it from *Dashboard → Settings → API*. `MapInboundClaims` is off, so `sub`/`email`/`role` arrive unmangled, and `ICurrentUser` reads display name / avatar from `user_metadata`. Since Supabase owns sign-up, there's no register endpoint — `UserProvisioningMiddleware` creates the profile row on a user's first authenticated request (memory-cached afterwards). Tombstoned accounts are rejected here while their JWT is still valid.

### Push notifications (Firebase)

The MAUI app calls `PUT /api/devices` on launch with the FCM token + device details, and `DELETE /api/devices/{token}` on sign-out. `IPushNotificationSender` has an FCM implementation (FirebaseAdmin, multicast, dead-token pruning) and a logging no-op used when no Firebase credentials are configured — so local dev needs no Firebase project. Enable it by setting `Firebase:CredentialsFile` (service-account JSON path) or `GOOGLE_APPLICATION_CREDENTIALS`.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| PUT | `/api/devices` | Register/refresh FCM token |
| DELETE | `/api/devices/{token}` | Unregister device |

A `UsersController` (`GET/PUT/DELETE /api/users/me`) is not yet implemented — the `User` entity, `ICurrentUser`, avatar storage, and `UserProvisioningMiddleware` are in place for it. The mobile client already expects these routes (`Services/Api/PulseApiClient.cs`).

## Migrations

```bash
cd src/Pulse.Api.ApiService
dotnet ef migrations add <Name> --output-dir Data/Migrations
dotnet ef migrations script -o Data/Migrations/<Name>.sql   # plain SQL for prod deploys
```

Applied automatically on startup in Development; in production apply during deployment (`dotnet ef database update` or the SQL script).

## Production deployment notes

- Point `ConnectionStrings__pulsedb` at the Supabase **connection pooler** (Settings → Database), set `Supabase__JwtSecret` and optionally `Supabase__Issuer` (`https://<ref>.supabase.co/auth/v1`) to enable issuer validation.
- The API talks to Postgres directly with the `postgres` role, so Row Level Security is not in play — keep the Supabase anon/REST surface for tables out of scope, or enable RLS with deny-all policies on these tables as defence in depth.
