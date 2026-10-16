/* =========================================================
   RaceDay Database Schema (SQL Server / SSMS)
   Matches the ERD: 6 tables
   ========================================================= */

IF DB_ID('RaceDayDb') IS NULL
BEGIN
    CREATE DATABASE RaceDayDb;
END
GO

USE RaceDayDb;
GO

-- Drop tables if re-running (children first)
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(20) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT NOT NULL,
    Name            NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    EventDate       DATE NOT NULL,
    Location        NVARCHAR(150) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    EventType       NVARCHAR(10) NOT NULL
        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    RouteInfo       NVARCHAR(1000) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES dbo.Users(UserId)
);
GO

CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 500,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT NOT NULL,
    CategoryId      INT NOT NULL,
    EnrolmentDate   DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId) ON DELETE CASCADE,
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

CREATE TABLE dbo.Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId) ON DELETE CASCADE
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

INSERT INTO dbo.Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

INSERT INTO dbo.Users (RoleId, FullName, Email, PasswordHash)
VALUES
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'), 'Thandiwe Nkosi', 'thandiwe@raceday.co.za', 'HASHED_PASSWORD_1'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Organiser'), 'Pieter van Wyk', 'pieter@raceday.co.za', 'HASHED_PASSWORD_2'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Lindiwe Dube', 'lindiwe@example.com', 'HASHED_PASSWORD_3'),
    ((SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Participant'), 'Sipho Mokoena', 'sipho@example.com', 'HASHED_PASSWORD_4');
GO

INSERT INTO dbo.Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType, RouteInfo)
VALUES
    ((SELECT UserId FROM dbo.Users WHERE Email = 'thandiwe@raceday.co.za'),
        'Pietermaritzburg Community 10K', 'Annual charity road run through the city centre.', '2026-09-12', 'Pietermaritzburg, KZN', 10.00, 'Run', 'Flat out-and-back route starting at City Hall.'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'thandiwe@raceday.co.za'),
        'Durban Beachfront Fun Walk', 'Family-friendly walk along the promenade.', '2026-10-03', 'Durban, KZN', 3.00, 'Walk', 'Along the Golden Mile promenade.'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'pieter@raceday.co.za'),
        'Cape Winelands Cycle Challenge', 'Scenic cycling event through the Winelands.', '2026-11-08', 'Stellenbosch, Western Cape', 60.00, 'Cycle', 'Rolling hills route via Franschhoek Pass.');
GO

INSERT INTO dbo.Categories (EventId, Name, DistanceKm, MaxParticipants)
VALUES
    ((SELECT EventId FROM dbo.Events WHERE Name = 'Pietermaritzburg Community 10K'), '10km Run', 10.00, 300),
    ((SELECT EventId FROM dbo.Events WHERE Name = 'Pietermaritzburg Community 10K'), '5km Fun Run', 5.00, 200),
    ((SELECT EventId FROM dbo.Events WHERE Name = 'Durban Beachfront Fun Walk'), '3km Walk', 3.00, 400),
    ((SELECT EventId FROM dbo.Events WHERE Name = 'Cape Winelands Cycle Challenge'), '60km Road Cycle', 60.00, 150),
    ((SELECT EventId FROM dbo.Events WHERE Name = 'Cape Winelands Cycle Challenge'), '30km Road Cycle', 30.00, 150);
GO

INSERT INTO dbo.Enrolments (ParticipantId, CategoryId, Status)
VALUES
    ((SELECT UserId FROM dbo.Users WHERE Email = 'lindiwe@example.com'),
        (SELECT CategoryId FROM dbo.Categories WHERE Name = '10km Run'), 'Confirmed'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'sipho@example.com'),
        (SELECT CategoryId FROM dbo.Categories WHERE Name = '5km Fun Run'), 'Confirmed'),
    ((SELECT UserId FROM dbo.Users WHERE Email = 'lindiwe@example.com'),
        (SELECT CategoryId FROM dbo.Categories WHERE Name = '60km Road Cycle'), 'Confirmed');
GO

INSERT INTO dbo.Results (EnrolmentId, FinishTime, Position, Status)
VALUES
    ((SELECT e.EnrolmentId FROM dbo.Enrolments e
        JOIN dbo.Users u ON e.ParticipantId = u.UserId
        JOIN dbo.Categories c ON e.CategoryId = c.CategoryId
        WHERE u.Email = 'lindiwe@example.com' AND c.Name = '10km Run'),
        '00:52:30', 14, 'Finished');
GO
