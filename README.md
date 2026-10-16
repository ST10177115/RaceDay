\# RaceDay API (POE Part 2)



RaceDay is a web-based event management system for South African road running, walking and cycling events. This part is the RESTful API built with ASP.NET Core Web API (C#), Entity Framework Core (code-first) and SQL Server.



\*\*Video demo (unlisted YouTube):https://youtu.be/KqDnsz4Z8EY





\## Roles



| Role | Can do |

|---|---|

| \*\*Organiser\*\* | Create, edit and delete their own events; manage categories for their events; view all enrolments for their events; capture and correct results. |

| \*\*Participant\*\* | Register, browse events and categories, enrol in an event by choosing a category, view and withdraw their own enrolments, view their own results. |



Anyone (no login) can view events, categories and an event's results. Role-based access is enforced at the API level: a wrong role gets \*\*403\*\*, and a request with no login gets \*\*401\*\*.



\## Setup and run



\*\*Requirements:\*\* .NET 8 SDK, SQL Server (Express is fine), Visual Studio 2022 or the `dotnet` CLI.



1\. Clone the repo and open `RaceDay.sln`.

2\. In `RaceDay.Api/appsettings.json`, set `ConnectionStrings:DefaultConnection` to your own SQL Server instance. It is currently set for `LabVM2049826\\SQLEXPRESS` and must be changed on any other machine.

3\. Create the database by running the migrations:

&#x20;  - Visual Studio: Package Manager Console, then `Update-Database`

&#x20;  - CLI: `dotnet ef database update --project RaceDay.Api`

4\. Run the API (F5, or `dotnet run --project RaceDay.Api`) and open `https://localhost:7265/swagger`.

5\. Log in with `POST /api/auth/login` in Swagger. The session cookie is then sent automatically with later requests.



\*\*Registering an Organiser\*\* needs the invite code from `appsettings.json` (`OrganiserInviteCode`, currently `RACEDAY-ORG-2026`). Participants register without a code.



\## Run the tests



```

dotnet test

```



53 unit tests run against an in-memory database. They cover registration and login, event management, role-based rejection, and enrolments, with both success and failure cases.



\## CI/CD



GitHub Actions (`.github/workflows/ci.yml`) restores, builds and runs all tests on every push.



!\[Green build](docs/images/ci-green-build.png)



!\[Tests passed](docs/images/ci-tests-passed.png)



\## Part 1 planning documents



The `docs` folder contains the ERD (`ERD.png`), the API endpoint plan (`API\_Endpoint\_Plan.md`) and the SQL script (`RaceDay\_Schema.sql`).



\## Design decisions and differences from the first Part 1 plan



\- \*\*Session authentication:\*\* login uses ASP.NET Core cookie authentication, with the user's ID and role stored in the cookie. Passwords are hashed with `PasswordHasher` and never stored in plain text.

\- \*\*Organiser registration:\*\* to stop users giving themselves organiser powers, registering as an Organiser requires an invite code.

\- \*\*Events\*\* also store `DistanceKm` and `EventType` (Run, Walk or Cycle), as required by the Part 2 brief. These two columns were missing from my first ERD, so I added them to the ERD, SQL script and endpoint plan in `/docs` so that everything matches the API.

\- \*\*Ownership checks:\*\* an Organiser can only edit or delete events, categories and results belonging to their own events.

\- \*\*One enrolment per event:\*\* a participant cannot enrol in two categories of the same event.

\- \*\*Business rules:\*\* enrolment is blocked when the category is full or the event has passed; results can only be captured after the event date and a position cannot repeat within a category; a category with enrolments cannot be deleted; and a participant cannot withdraw once a result exists.



\## AI disclosure



I used an AI assistant (Claude) to help plan and draft the ERD, endpoint plan, SQL script, API code, unit tests, GitHub Actions workflow and this README. I ran, tested and committed everything myself, and I can explain how it works in my video.

