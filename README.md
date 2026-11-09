## Part 3 status

**Completed:** API additions for the front end (event filters, organiser "my events" endpoint with enrolment counts, finisher totals on results), Azure Blob Storage upload code with banner and profile picture columns and migration, the `RaceDay.Web` MVC project with session and API client setup, and a Dockerfile.

**Not completed:** the MVC pages (login, dashboards, enrolment) and the end-to-end Blob upload demo, because of time. Known issue: some unit tests fail on this branch after the Blob storage commit; Part 2 on `master` is green.

**Run with Docker:**
`docker build -t raceday-web . && docker run -p 8080:8080 raceday-web`

**Video:** TO BE ADDED

**AI disclosure:** I used an AI assistant (Claude) to help plan and draft code, tests and documentation, and I ran and committed it myself.