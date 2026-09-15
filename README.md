# IsoTreatment — Monolith to Microservices Migration

This repository accompanies a master's thesis on migrating an existing ASP.NET Core
monolith to microservices. It is not a greenfield project: the starting point is a working
application, and every step is meant to be reproducible and verifiable rather than merely
described.

## The application being migrated

`IsoTreatmentProcessSupportAPI` is an ASP.NET Core Web API (.NET 8) supporting patients
who follow a long-term medication regimen. It manages user accounts along with their
pharmacological parameters, diary entries, medication reminders, and treatment-process
calculations. Today it runs as a single process backed by a single SQL Server database,
with controllers organised around entities rather than around business capabilities.

Its full commit history is preserved here. The application also continues to live in its
own repository at https://github.com/kowalczykp01/IsoTreatmentProcessSupportAPI, wired
here as the read-only `upstream` remote.

## Target architecture

The monolith is to be decomposed into two business services:

- **Identity** — authentication, identity, and token issuing.
- **Treatment** — the treatment domain: treatment profiles, diary entries, and reminders.

## Current scope

This repository demonstrates **Branch by Abstraction** on one complete slice of
functionality: extracting **reminders** from the monolith into the **Treatment** service.

It is the companion of
https://github.com/kowalczykp01/IsoTreatment-microservices-migration-strangler-fig-pattern,
which migrates the same slice, from the same starting commit, with the Strangler Fig
Pattern. The two are meant to be read side by side, so wherever the technique does not force
a difference, this repository follows the same structure and conventions.

The service is named Treatment from its first commit even though reminders are, for now,
the only thing it serves. Later waves move diary entries and treatment profiles into the
same service, and renaming a running service is exactly the cost this naming avoids.

The moving parts:

- the monolith, which stays the only address the frontend talks to — the decision "who
  serves this request" is made inside its code, not at the system boundary,
- `IReminderGateway`, the abstraction the monolith's reminder logic depends on, with two
  implementations: one backed by the monolith's own database, one calling the Treatment
  service over HTTP,
- a configuration flag, `Features:UseTreatmentServiceForReminders`, choosing between them
  at dependency-injection time,
- the Treatment service, laid out as Domain, Application, Infrastructure and Api, with a
  **database of its own**,
- **Jaeger** for distributed tracing, so that the switch is observable rather than asserted,
- **Docker Compose** tying the pieces together.

There is no reverse proxy. Its absence is the most visible difference from the Strangler
Fig repository, and it is a property of the technique rather than an omission.

## How this differs from the Strangler Fig migration

| | Strangler Fig | Branch by Abstraction (here) |
| --- | --- | --- |
| Where the switch lives | YARP route at the system boundary | DI registration inside the monolith |
| What the switch costs | editing gateway configuration | changing a flag and restarting the monolith |
| Monolith code before cleanup | untouched | refactored behind `IReminderGateway` |
| Treatment service database | the monolith's database, shared as a bridge | a separate database, populated by copying |
| How the token reaches Treatment | HttpOnly cookie, forwarded by the proxy | `Authorization: Bearer` header, set by the monolith |
| Verification | contract tests comparing two running services | equivalence tests running one set of assertions against both gateway implementations |

The database strategy differs on purpose. Neither YARP nor DI forces one choice or the
other; splitting the variants between the two repositories shows both of them — a shared
database as a bridge, and a separate database from the start — at the cost of one new
database rather than two.

## Migration plan

### Phase 0 — characterization tests around the reminder API

Before anything is refactored, the current behaviour of `/api/reminder` is pinned down by
HTTP-level tests running the real monolith against a throwaway SQL Server container: status
codes, exact response bodies, time formatting, error messages, content types, and the
rejection of missing or foreign tokens. These are the safety net for Phase 3, where the
monolith's own code changes.

Making the monolith testable requires a few seams, the same ones the Strangler Fig migration
introduced: the connection string moves from `IsoSupportDbContext` into configuration, and
`Program` becomes visible to `WebApplicationFactory`.

### Phase 1 — containerize the monolith as it is

A Dockerfile for the monolith and a Compose file running it next to SQL Server, with secrets
supplied through `.env`. No behaviour changes.

### Phase 2 — OpenTelemetry instrumentation exported to Jaeger

The monolith is instrumented before the Treatment service exists, so that a reminder request
served entirely in-process is on record and can be compared with the same request after the
switch, when it crosses a network boundary.

### Phase 3 — introduce the abstraction

`IReminderGateway` is extracted, and its first implementation, `EfReminderGateway`, wraps
the monolith's existing Entity Framework code. `ReminderService` depends on the interface
instead of on `IsoSupportDbContext` for reminders. The reminder service and controller
become asynchronous, following the Treatment service in the Strangler Fig repository.

Nothing observable changes. The characterization tests from Phase 0 pass unmodified, and
that is the whole acceptance criterion of this phase.

**The abstraction covers reminder storage only.** Checking that the user exists stays in
the monolith's `ReminderService`, which still owns the `Users` table. The Treatment service
has no `Users` table and never learns whether a user exists — it trusts the user id carried
by the token the monolith has already validated. This keeps the `User not found` response
byte-for-byte identical on both paths, and it is the opposite of the Strangler Fig
repository, where the Treatment service answers requests directly and so has to check users
itself.

### Phase 4 — the Treatment service with its own database

A new service, structured like its Strangler Fig counterpart, mapping only the `Reminders`
table in a physically separate SQL Server instance with its own migrations. It validates
tokens with the same symmetric HMAC key as the monolith — a temporary bridge until Identity
issues RS256 tokens published through JWKS — and reads them from the `Authorization` header
only, because its sole caller is the monolith. Instrumented and containerized from the start.
No traffic reaches it yet.

### Phase 5 — one-off copy of the reminder data

Existing reminders are copied from the monolith's database into the Treatment service's
database, preserving their ids, with the identity value set to the monolith's own rather than
to the highest copied id, so that no id the monolith ever issued is issued again. The copy is
verified by comparing row counts, row contents and identity values on both sides.

### Phase 6 — the second implementation, behind a flag

`TreatmentServiceReminderGateway` implements `IReminderGateway` by calling the Treatment
service with `HttpClient`, forwarding the incoming token as a bearer header. Not-found
responses are translated back into the monolith's own exceptions, so that clients see the
same status codes and messages as before.

`Features:UseTreatmentServiceForReminders` selects the implementation when services are
registered. It defaults to `false`: the code ships, but every request still goes to the
monolith's database.

### Phase 7 — equivalence tests

Because both implementations sit behind the same interface, one set of assertions runs
against both of them: the Entity Framework gateway against a test database, and the HTTP
gateway against a test instance of the Treatment service, each seeded with identical data.
A difference between the two fails the same test that passes for the other.

### Phase 8 — switch reminders to the Treatment service

**The data copy from Phase 5 is repeated immediately before the flag is turned on.**
Reminders created or changed in the monolith's database between the first copy and the
switch would otherwise be lost. The first copy proves the procedure; the second is the one
the switch relies on.

The flag is then enabled first in the characterization tests — the same HTTP requests to the
same monolith, now served through the new path — and after that in the Compose environment.
Rolling back is turning the flag off again, with one caveat that the Strangler Fig migration
did not have: reminders written to the Treatment service after the switch exist only in its
database.

### Phase 9 — remove the old path

`EfReminderGateway`, the `Reminder` entity and its mapping in `IsoSupportDbContext` are
removed from the monolith, together with the flag. Unlike in the Strangler Fig migration, the
physical `Reminders` table in the monolith's database no longer holds the data anyone uses,
so it can be dropped rather than left in place.

## Known consequences

Splitting the database removes the foreign key from `Reminders` to `Users` and its cascading
delete. The API exposes no way to delete a user today, but a user removed from the monolith's
database by any means would leave their reminders behind in the Treatment service's database.
This is accepted and left unsolved here; it is the kind of consistency the Identity
extraction will have to address with events rather than constraints.

## Out of scope

- Extracting Identity as a service of its own, hence the shared HMAC key.
- The registration seam and splitting `User` into a treatment profile.
- Removing the duplicated `ITokenService.GetUserIdFromToken` calls in the services that
  remain in the monolith.
- Migrating diary entries and treatment profiles.

## Progress

- [x] **Phase 0** — characterization tests around the reminder API
- [x] **Phase 1** — containerize the monolith as it is
- [x] **Phase 2** — OpenTelemetry instrumentation exported to Jaeger
- [x] **Phase 3** — introduce `IReminderGateway` with the Entity Framework implementation
- [x] **Phase 4** — the Treatment service with its own database
- [x] **Phase 5** — one-off copy of the reminder data
- [ ] **Phase 6** — the HTTP implementation, behind a flag
- [ ] **Phase 7** — equivalence tests
- [ ] **Phase 8** — repeat the data copy and switch reminders to the Treatment service
- [ ] **Phase 9** — remove the old path from the monolith

## Running the application

Docker is the only prerequisite — the monolith, the Treatment service and both SQL Server
instances run in containers.

Copy `.env.example` to `.env` and fill it in — it documents every variable Compose
expects and why. Only the SMTP entries are optional; without them registration and
password reset return 500, and nothing else is affected.

```
cp .env.example .env
docker compose up -d --build
```

Compose waits for each SQL Server to report healthy before it starts the service that uses
it, so the first run takes about a minute. On Apple Silicon the databases run under
emulation; the Compose file pins them to `linux/amd64` because SQL Server has no arm64 image.

| Address | What |
| --- | --- |
| `localhost:8080` | the monolith — the address the frontend uses, before and after the migration |
| `localhost:8082` | the Treatment service directly; in normal operation only the monolith calls it |
| `localhost:16686` | Jaeger UI |
| `localhost:14330` | the monolith's SQL Server |
| `localhost:14331` | the Treatment service's SQL Server — a separate instance, not a second database on the first |

The ports match the Strangler Fig repository, where `8080` is the gateway, so the two stacks
cannot run at the same time. Stop one with `docker compose stop` before starting the other.

### Applying the database schema

The schema is not created automatically. Apply the migrations from the host, against the
port Compose publishes:

```
set -a; . ./.env; set +a
ConnectionStrings__IsoSupportDb="Server=localhost,14330;Database=IsoTreatmentProcessSupport;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=true;TrustServerCertificate=true;" \
  dotnet ef database update --project IsoTreatmentProcessSupportAPI
```

The Treatment service has migrations of its own, applied the same way against its own
instance:

```
set -a; . ./.env; set +a
ConnectionStrings__TreatmentDb="Server=localhost,14331;Database=Treatment;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=true;TrustServerCertificate=true;" \
  dotnet ef database update --project TreatmentService/TreatmentService.Infrastructure \
  --startup-project TreatmentService/TreatmentService.Api
```

Its schema holds a single `Reminders` table, shaped like the monolith's but without the
foreign key to `Users`, which does not exist on that side.

This needs the EF Core tools (`dotnet tool install --global dotnet-ef`) and has to be repeated
whenever the `mssql-data` or `treatment-mssql-data` volume is removed.

### Checking that it works

| Request | Expected |
| --- | --- |
| `GET localhost:8080/swagger/index.html` | 200 — the application started |
| `GET localhost:8080/api/reminder` | 401 — routing and authentication are wired |
| `POST localhost:8080/api/user/login` with unknown credentials | 400 — the application reached the database |

A 500 on the last one means the database is unreachable or the schema was never applied.

| Request | Expected |
| --- | --- |
| `GET localhost:8082/swagger/index.html` | 200 — the Treatment service started |
| `GET localhost:8082/api/reminder` with the token in the `Authorization` header | 200 — its database is reachable |
| `GET localhost:8082/api/reminder` with the token in the `token` cookie | 401 — the service reads the header only |

## Copying the reminder data

`tools/ReminderDataCopy` copies the `Reminders` table from the monolith's database into the
Treatment service's. It references neither service and talks to both databases in plain SQL,
so it copies rows, not whatever either model thinks a reminder is. Both schemas have to be
applied first.

```
set -a; . ./.env; set +a
export ConnectionStrings__IsoSupportDb="Server=localhost,14330;Database=IsoTreatmentProcessSupport;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=true;TrustServerCertificate=true;"
export ConnectionStrings__TreatmentDb="Server=localhost,14331;Database=Treatment;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=true;TrustServerCertificate=true;"
dotnet run --project tools/ReminderDataCopy
```

```
Source    6 reminders, ids 3..1006, 3 users, identity 1006
Target    6 reminders, ids 3..1006, 3 users, identity 1006
Copied and verified 6 reminders.
```

What it does, in order:

- reads every reminder and the table's identity value from the monolith in one serializable
  transaction, so both describe the same moment,
- in a single transaction on the Treatment side, deletes existing rows, bulk-inserts the
  snapshot with its original ids, and reseeds the identity to the monolith's value,
- compares row count, row contents and identity value with the snapshot, and commits only if
  all three match — otherwise it rolls back and exits non-zero, leaving the target untouched.

Ids are preserved because clients already hold them. The identity is copied rather than
derived from the highest id because the two can differ considerably: SQL Server caches
identity values and skips ahead by up to 1000 after an instance restart, and deleted
reminders leave gaps. Reseeding to `MAX(Id)` would let the Treatment service hand out ids the
monolith had already issued once.

If the target already holds reminders, the tool refuses to run unless given `--replace`. The
first copy is made into an empty table; the second, immediately before the switch in Phase 8,
has to overwrite it. After the switch the flag is dangerous — the Treatment database is then
the only copy of any reminder written since, and `--replace` would silently discard it.

## Distributed tracing

The monolith is instrumented with OpenTelemetry and exports over OTLP to Jaeger at
`localhost:16686`. The service name and the exporter endpoint come from environment
variables in the Compose file — the OpenTelemetry SDK reads `OTEL_SERVICE_NAME` and
`OTEL_EXPORTER_OTLP_ENDPOINT` by itself, so neither appears in application code.

Instrumentation went in before any reminder code changed, so that a reminder request served
entirely in-process is on record:

```
monolith  GET api/reminder
monolith  SELECT [u].[Id] ...
```

One request, one SQL query: `ReminderService` loads reminders through
`Users.Include(u => u.Reminders)`, a join that only works while reminders and users live in
the same database.

Introducing the abstraction in Phase 3 changed that, before any request left the process:

```
monolith  GET api/reminder
monolith  SELECT [Users]
monolith  SELECT [Reminders]
```

The client sent the same request and got the same response; the characterization tests pass
unmodified. But checking the user and reading reminders are now two separate steps on two
sides of `IReminderGateway`, and the join cannot survive that. The extra round trip is the
price of the seam itself, paid while everything still runs against one database — the
Strangler Fig migration shows the same two queries, but only once the Treatment service
took over.

Tracing is not on the critical path. Stopping the Jaeger container leaves every endpoint
working; exports fail silently in the background.

## Running the tests

The characterization tests start a real SQL Server in a throwaway container, so Docker has
to be running:

```
dotnet test tests/IsoTreatmentProcessSupportAPI.CharacterizationTests
```

Fuller technical documentation follows as the implementation progresses.
