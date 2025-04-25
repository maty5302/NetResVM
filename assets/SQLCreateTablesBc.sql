--CREATE DB for application
CREATE DATABASE DB_NetResVM
GO

USE DB_NetResVM
GO
--create table for servers
CREATE TABLE Server (
    ServerID INT PRIMARY KEY IDENTITY NOT NULL,
    ServerType VARCHAR(50) NOT NULL,
    [Name] VARCHAR(100) NOT NULL,
    IpAddress VARCHAR(15) NOT NULL,
    Username VARCHAR(50) NOT NULL,
    [Password] VARCHAR(50) NOT NULL
);
GO
--create table for users
CREATE TABLE [User] (
    UserID INT PRIMARY KEY IDENTITY NOT NULL,
    Username VARCHAR(50) NOT NULL,
    [Password] VARCHAR(50), --password could be null after adding connection type
    [Role] VARCHAR(20) NOT NULL,
    AuthorizationType VARCHAR(20) NOT NULL,
    Active INT NOT NULL
);
GO
--push default user - admin with local connectionType
INSERT INTO [User] (Username, [Password], [Role],AuthorizationType,Active) VALUES ('admin', 'Password123', 'Admin', 'localhost', 1);
GO
--create table for reservations
CREATE TABLE Reservation (
    ReservationID INT PRIMARY KEY IDENTITY NOT NULL,
    UserID INT NOT NULL,
    ServerID INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    LabID VARCHAR(255) NOT NULL,
    CONSTRAINT FK_Reservation_User FOREIGN KEY (UserID) REFERENCES [User](UserID),
    CONSTRAINT FK_Reservation_Server FOREIGN KEY (ServerID) REFERENCES Server(ServerID)
);
GO
--create table for owned labs
CREATE TABLE UserLabOwnership (
    LabID VARCHAR(255) NOT NULL,
    ServerID INT NOT NULL,
    UserID INT NOT NULL,
    CONSTRAINT PK_OwnedLab PRIMARY KEY (LabID, ServerID),
    CONSTRAINT FK_OwnedLab_Server FOREIGN KEY (ServerID) REFERENCES Server(ServerID),
    CONSTRAINT FK_OwnedLab_User FOREIGN KEY (UserID) REFERENCES [User](UserID)
);
GO