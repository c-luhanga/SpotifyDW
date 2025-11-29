-- =============================================
-- SpotifyDW Database Schema
-- Star Schema for Spotify Track Analysis
-- =============================================

-- Create database
USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SpotifyDW')
BEGIN
    CREATE DATABASE SpotifyDW;
END
GO

USE SpotifyDW;
GO

-- =============================================
-- Drop existing tables (for clean re-runs)
-- =============================================
IF OBJECT_ID('dbo.FactTrack', 'U') IS NOT NULL DROP TABLE dbo.FactTrack;
IF OBJECT_ID('dbo.DimTrack', 'U') IS NOT NULL DROP TABLE dbo.DimTrack;
IF OBJECT_ID('dbo.DimAlbum', 'U') IS NOT NULL DROP TABLE dbo.DimAlbum;
IF OBJECT_ID('dbo.DimArtist', 'U') IS NOT NULL DROP TABLE dbo.DimArtist;
IF OBJECT_ID('dbo.DimDate', 'U') IS NOT NULL DROP TABLE dbo.DimDate;
GO

-- =============================================
-- Dimension Tables
-- =============================================

-- DimArtist: Stores unique artist information
CREATE TABLE dbo.DimArtist (
    ArtistKey INT IDENTITY(1,1) PRIMARY KEY,
    ArtistName NVARCHAR(255) NOT NULL,
    ArtistPopularity INT NULL,
    ArtistFollowers BIGINT NULL,
    ArtistGenres NVARCHAR(500) NULL,
    -- Audit columns
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ModifiedDate DATETIME2 DEFAULT GETDATE(),
    -- Business key constraint
    CONSTRAINT UQ_Artist_Name UNIQUE (ArtistName)
);
GO

-- DimAlbum: Stores unique album information
CREATE TABLE dbo.DimAlbum (
    AlbumKey INT IDENTITY(1,1) PRIMARY KEY,
    SpotifyAlbumId NVARCHAR(50) NOT NULL,
    AlbumName NVARCHAR(500) NOT NULL,
    ArtistKey INT NOT NULL,
    AlbumType NVARCHAR(50) NULL,
    AlbumTotalTracks INT NULL,
    ReleaseDateKey INT NULL,
    -- Audit columns
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ModifiedDate DATETIME2 DEFAULT GETDATE(),
    -- Business key constraint
    CONSTRAINT UQ_Album_SpotifyId UNIQUE (SpotifyAlbumId),
    -- Foreign key
    CONSTRAINT FK_Album_Artist FOREIGN KEY (ArtistKey) 
        REFERENCES dbo.DimArtist(ArtistKey)
);
GO

-- DimTrack: Stores unique track information
CREATE TABLE dbo.DimTrack (
    TrackKey INT IDENTITY(1,1) PRIMARY KEY,
    SpotifyTrackId NVARCHAR(50) NOT NULL,
    TrackName NVARCHAR(500) NOT NULL,
    TrackNumber INT NULL,
    TrackDurationMs INT NULL,
    Explicit BIT NULL,
    -- Audit columns
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    ModifiedDate DATETIME2 DEFAULT GETDATE(),
    -- Business key constraint
    CONSTRAINT UQ_Track_SpotifyId UNIQUE (SpotifyTrackId)
);
GO

-- DimDate: Standard date dimension
CREATE TABLE dbo.DimDate (
    DateKey INT PRIMARY KEY,
    Date DATE NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    MonthName NVARCHAR(20) NOT NULL,
    Quarter INT NOT NULL,
    DayOfWeek INT NOT NULL,
    DayName NVARCHAR(20) NOT NULL,
    -- Useful flags
    IsWeekend BIT NOT NULL DEFAULT 0,
    -- Audit columns
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT UQ_Date UNIQUE (Date)
);
GO

-- =============================================
-- Fact Table
-- =============================================

-- FactTrack: Central fact table storing track metrics
CREATE TABLE dbo.FactTrack (
    FactTrackKey BIGINT IDENTITY(1,1) PRIMARY KEY,
    TrackKey INT NOT NULL,
    ArtistKey INT NOT NULL,
    AlbumKey INT NOT NULL,
    ReleaseDateKey INT NULL,
    -- Measures
    TrackPopularity INT NULL,
    -- Audio features (placeholders for future enhancement)
    Energy FLOAT NULL,
    Danceability FLOAT NULL,
    Valence FLOAT NULL,
    Loudness FLOAT NULL,
    Tempo FLOAT NULL,
    Acousticness FLOAT NULL,
    Instrumentalness FLOAT NULL,
    Liveness FLOAT NULL,
    Speechiness FLOAT NULL,
    -- Audit columns
    LoadDate DATETIME2 DEFAULT GETDATE(),
    -- Foreign keys
    CONSTRAINT FK_Fact_Track FOREIGN KEY (TrackKey) 
        REFERENCES dbo.DimTrack(TrackKey),
    CONSTRAINT FK_Fact_Artist FOREIGN KEY (ArtistKey) 
        REFERENCES dbo.DimArtist(ArtistKey),
    CONSTRAINT FK_Fact_Album FOREIGN KEY (AlbumKey) 
        REFERENCES dbo.DimAlbum(AlbumKey),
    CONSTRAINT FK_Fact_ReleaseDate FOREIGN KEY (ReleaseDateKey) 
        REFERENCES dbo.DimDate(DateKey)
);
GO

-- =============================================
-- Indexes for Performance
-- =============================================

-- DimArtist indexes
CREATE NONCLUSTERED INDEX IX_DimArtist_Name 
    ON dbo.DimArtist(ArtistName);
GO

-- DimAlbum indexes
CREATE NONCLUSTERED INDEX IX_DimAlbum_ArtistKey 
    ON dbo.DimAlbum(ArtistKey);
CREATE NONCLUSTERED INDEX IX_DimAlbum_ReleaseDateKey 
    ON dbo.DimAlbum(ReleaseDateKey);
GO

-- DimTrack indexes
CREATE NONCLUSTERED INDEX IX_DimTrack_Name 
    ON dbo.DimTrack(TrackName);
GO

-- DimDate indexes
CREATE NONCLUSTERED INDEX IX_DimDate_Year 
    ON dbo.DimDate(Year);
CREATE NONCLUSTERED INDEX IX_DimDate_YearMonth 
    ON dbo.DimDate(Year, Month);
GO

-- FactTrack indexes for common queries
CREATE NONCLUSTERED INDEX IX_FactTrack_TrackKey 
    ON dbo.FactTrack(TrackKey);
CREATE NONCLUSTERED INDEX IX_FactTrack_ArtistKey 
    ON dbo.FactTrack(ArtistKey);
CREATE NONCLUSTERED INDEX IX_FactTrack_AlbumKey 
    ON dbo.FactTrack(AlbumKey);
CREATE NONCLUSTERED INDEX IX_FactTrack_ReleaseDateKey 
    ON dbo.FactTrack(ReleaseDateKey);
GO

-- =============================================
-- Verification: List all tables
-- =============================================
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

PRINT 'SpotifyDW schema created successfully!'
GO

