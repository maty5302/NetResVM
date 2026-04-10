-- Vytvoření DB, pokud neexistuje
IF DB_ID('DB_NetResVM') IS NULL
BEGIN
    CREATE DATABASE DB_NetResVM;
END
GO

USE DB_NetResVM;
GO

-- Vytvoření tabulky Server, pokud neexistuje
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Server]') AND type in (N'U'))
BEGIN
    CREATE TABLE Server (
        ServerID INT PRIMARY KEY IDENTITY NOT NULL,
        ServerType VARCHAR(50) NOT NULL,
        [Name] VARCHAR(100) NOT NULL,
        IpAddress VARCHAR(15) NOT NULL,
        Username VARCHAR(50) NOT NULL,
        [Password] VARCHAR(50) NOT NULL
    );
END
GO

-- Vytvoření tabulky User, pokud neexistuje
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND type in (N'U'))
BEGIN
    CREATE TABLE [User] (
        UserID INT PRIMARY KEY IDENTITY NOT NULL,
        Username VARCHAR(50) NOT NULL,
        [Password] VARCHAR(50), --password could be null after adding connection type
        [Role] VARCHAR(20) NOT NULL,
        AuthorizationType VARCHAR(20) NOT NULL,
        Active INT NOT NULL
    );
END
GO

-- Vložení výchozího uživatele admin, POKUD TAM JEŠTĚ NENÍ
IF NOT EXISTS (SELECT * FROM [dbo].[User] WHERE Username = 'admin')
BEGIN
    INSERT INTO [dbo].[User] (Username, Password, Role, AuthorizationType, Active)
    VALUES ('admin', 'Password123', 'Admin', 'localhost', 1);
END
GO

-- Vytvoření tabulky Reservation, pokud neexistuje
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Reservation]') AND type in (N'U'))
BEGIN
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
END
GO

-- Vytvoření tabulky UserLabOwnership, pokud neexistuje
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserLabOwnership]') AND type in (N'U'))
BEGIN
    CREATE TABLE UserLabOwnership (
        LabID VARCHAR(255) NOT NULL,
        ServerID INT NOT NULL,
        UserID INT NOT NULL,
        CONSTRAINT PK_OwnedLab PRIMARY KEY (LabID, ServerID),
        CONSTRAINT FK_OwnedLab_Server FOREIGN KEY (ServerID) REFERENCES Server(ServerID),
        CONSTRAINT FK_OwnedLab_User FOREIGN KEY (UserID) REFERENCES [User](UserID)
    );
END
GO
