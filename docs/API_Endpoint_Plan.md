# RaceDay – API Endpoint Plan

All routes start with `/api/`. Authentication uses a session cookie issued by login.
Role Required: **None** = public, **Any** = any logged-in user, or a specific role.
An Organiser can only change events, categories and results that belong to their own events.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates an account as Organiser or Participant. Organisers must supply the invite code. | None | { fullName, email, password, role, organiserCode (Organiser only) } | 201 Created – user (no password). 400 – invalid data or role. 403 – wrong or missing organiser code. 409 – email already registered. |
| POST | /api/auth/login | Logs in and starts a session cookie carrying the user's role. | None | { email, password } | 200 OK – { userId, fullName, email, role }. 401 – invalid email or password. |
| POST | /api/auth/logout | Ends the current session. | Any | None | 204 No Content. 401 – not logged in. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own profile. | Any | None | 200 OK – profile. 401 – not logged in. |
| PUT | /api/users/me | Updates the logged-in user's name and email. | Any | { fullName, email } | 200 OK – updated profile. 400 – invalid data. 401 – not logged in. 409 – email used by another account. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists events soonest first, with their categories. Optional ?upcomingOnly=true. | None | None | 200 OK – array of events. |
| GET | /api/events/{id} | Gets one event with its categories. | None | None | 200 OK – event. 404 – not found. |
| POST | /api/events | Creates an event owned by the logged-in Organiser. | Organiser | { name, description, eventDate, location, distanceKm, eventType (Run/Walk/Cycle), routeInfo } | 201 Created – event. 400 – invalid data. 401 – not logged in. 403 – not an Organiser. |
| PUT | /api/events/{id} | Updates an event. | Organiser (owner) | { name, description, eventDate, location, distanceKm, eventType, routeInfo } | 200 OK – event. 400. 401. 403 – not an Organiser or not the owner. 404 – not found. |
| DELETE | /api/events/{id} | Deletes an event and its categories. | Organiser (owner) | None | 204 No Content. 401. 403 – not an Organiser or not the owner. 404. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists the categories of an event. | None | None | 200 OK – array of categories. 404 – event not found. |
| POST | /api/events/{eventId}/categories | Adds a category (for example 10km or Under 20) to an event. | Organiser (owner) | { name, distanceKm, maxParticipants } | 201 Created – category. 400. 401. 403. 404 – event not found. 409 – name already used in this event. |
| PUT | /api/categories/{id} | Updates a category. | Organiser (owner) | { name, distanceKm, maxParticipants } | 200 OK – category. 400. 401. 403. 404. 409 – name already used. |
| DELETE | /api/categories/{id} | Deletes a category that has no enrolments. | Organiser (owner) | None | 204 No Content. 401. 403. 404. 409 – category already has enrolments. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enrols the logged-in Participant in an event by choosing a category. | Participant | None | 201 Created – enrolment. 400 – event has already taken place. 401. 403 – not a Participant. 404 – category not found. 409 – already enrolled in this event, or category full. |
| GET | /api/enrolments/me | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK – array of enrolments. 401. 403. |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event. | Organiser (owner) | None | 200 OK – array of enrolments. 401. 403 – not the owner. 404 – event not found. |
| DELETE | /api/enrolments/{id} | Withdraws the logged-in Participant from an event. | Participant (owner of the enrolment) | None | 204 No Content. 401. 403 – not your enrolment. 404. 409 – a result already exists. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/result | Captures a finish time and position for an enrolment, once the event has taken place. | Organiser (owner) | { finishTime (hh:mm:ss), position, status (Finished/DNF/DNS) } | 201 Created – result. 400 – event not held yet, or time/position missing. 401. 403. 404 – enrolment not found. 409 – result already exists, or position taken in this category. |
| PUT | /api/results/{id} | Corrects a captured result. | Organiser (owner) | { finishTime, position, status } | 200 OK – result. 400. 401. 403. 404. 409 – position taken. |
| GET | /api/results/me | Lists the logged-in Participant's own results. | Participant | None | 200 OK – array of results. 401. 403. |
| GET | /api/events/{eventId}/results | Lists the results of an event by category and position. | None | None | 200 OK – array of results. 404 – event not found. |
