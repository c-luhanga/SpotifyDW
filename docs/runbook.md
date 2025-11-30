# SpotifyDW Runbook

## Prerequisites

### Required Software
- **SQL Server** (2019 or later recommended)
  - SQL Server Express is sufficient for development
  - Ensure Windows Authentication is enabled
- **.NET SDK** (10.0 or later)
  - Download from: https://dotnet.microsoft.com/download
- **Git** (for cloning the repository)
- **sqlcmd** (SQL Server command-line tools)

### System Requirements
- Windows 10/11 or Windows Server
- Minimum 4GB RAM
- 500MB disk space for database

## Setup Instructions

### Step 1: Clone the Repository

```powershell
git clone https://github.com/c-luhanga/SpotifyDW.git
cd SpotifyDW
```

### Step 2: Prepare Data Files

Place your CSV data files in the `data/` directory:

```powershell
# Ensure you have these files:
data/spotify_data_clean.csv
data/track_data_final.csv
```

**Expected CSV Format:**

**spotify_data_clean.csv:**
- Columns: track_id, track_name, track_number, track_popularity, explicit, artist_name, artist_popularity, artist_followers, artist_genres, album_id, album_name, album_release_date, album_total_tracks, album_type, track_duration_min

**track_data_final.csv:**
- Columns: track_id, track_name, track_number, track_popularity, track_duration_ms, explicit, artist_name, artist_popularity, artist_followers, artist_genres, album_id, album_name, album_release_date, album_total_tracks, album_type

### Step 3: Create Database

Run the database creation script:

```powershell
# Navigate to the db directory
cd db

# Execute the schema script
sqlcmd -S localhost -E -i schema.sql
```

This will:
1. Create the `SpotifyDW` database
2. Create all dimension tables (DimArtist, DimAlbum, DimTrack, DimDate)
3. Create the fact table (FactTrack)
4. Create indexes for query performance

**Verify database creation:**

```powershell
sqlcmd -S localhost -E -d SpotifyDW -Q "SELECT name FROM sys.tables ORDER BY name"
```

Expected output:
```
DimAlbum
DimArtist
DimDate
DimTrack
FactTrack
```

### Step 4: Configure Connection String (Optional)

If you need to modify the database connection:

Edit `src/SpotifyDW.ETL/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SpotifyDW": "Server=localhost;Database=SpotifyDW;Integrated Security=true;TrustServerCertificate=true;"
  },
  "DataSources": {
    "SpotifyDataCleanPath": "../../../../../data/spotify_data clean.csv",
    "TrackDataFinalPath": "../../../../../data/track_data_final.csv"
  }
}
```

**Connection String Options:**
- `Server=localhost` - Change to your SQL Server instance name
- `Integrated Security=true` - Use Windows Authentication
- For SQL Authentication: `User ID=username;Password=password;`

### Step 5: Build the ETL Application

```powershell
# Navigate to the ETL project
cd src/SpotifyDW.ETL

# Restore NuGet packages and build
dotnet build
```

Expected output: `Build succeeded`

### Step 6: Run the ETL Process

```powershell
# Run the ETL application
dotnet run
```

**Expected Output:**

```
=== SpotifyDW ETL Application ===

Configuration loaded successfully:
  Connection String: Server=localhost;Database=SpotifyDW;...
  Spotify Data CSV: ../../../../../data/spotify_data clean.csv
  Track Data CSV: ../../../../../data/track_data_final.csv

Validating data files:
  C:\...\data\spotify_data clean.csv: ✓ Found
  C:\...\data\track_data_final.csv: ✓ Found

=== EXTRACT PHASE ===
Loaded 8582 tracks from contemporary data (spotify_data_clean.csv)
Loaded 8778 tracks from historic data (track_data_final.csv)
Total tracks loaded: 17360

✓ Extract phase completed successfully!

=== TRANSFORM PHASE ===
Created 2551 unique artists
Created 2385 unique dates
Created 5317 unique albums
Created 8778 unique tracks
Created 16956 fact records

✓ Transform phase completed successfully!

=== TRANSFORMATION SUMMARY ===
  Artists: 2551
  Albums: 5317
  Tracks: 8778
  Dates: 2385
  Fact Records: 16956

=== LOAD PHASE ===
Loaded 2551 artists into DimArtist
Loaded 2385 dates into DimDate
Loaded 5317 albums into DimAlbum
Loaded 8778 tracks into DimTrack
Loaded 16956 fact records into FactTrack

✓ Load phase completed successfully!

ETL process completed.
Press any key to exit...
```

## Verification

### Verify Data Load

Check row counts:

```powershell
sqlcmd -S localhost -E -d SpotifyDW -Q "SELECT 'DimArtist' AS [Table], COUNT(*) AS [Rows] FROM DimArtist; SELECT 'DimAlbum', COUNT(*) FROM DimAlbum; SELECT 'DimTrack', COUNT(*) FROM DimTrack; SELECT 'DimDate', COUNT(*) FROM DimDate; SELECT 'FactTrack', COUNT(*) FROM FactTrack;"
```

### Sample Queries

**Top 10 most popular tracks:**

```sql
SELECT TOP 10 
  a.ArtistName,
  t.TrackName,
  f.TrackPopularity
FROM FactTrack f
JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
JOIN DimTrack t ON f.TrackKey = t.TrackKey
ORDER BY f.TrackPopularity DESC;
```

**Tracks by year:**

```sql
SELECT 
  d.Year,
  COUNT(*) AS TrackCount,
  AVG(f.TrackPopularity) AS AvgPopularity
FROM FactTrack f
JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
GROUP BY d.Year
ORDER BY d.Year DESC;
```

**Most prolific artists:**

```sql
SELECT TOP 10
  a.ArtistName,
  COUNT(DISTINCT f.TrackKey) AS TrackCount,
  COUNT(DISTINCT f.AlbumKey) AS AlbumCount,
  MAX(a.ArtistFollowers) AS Followers
FROM FactTrack f
JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
GROUP BY a.ArtistName
ORDER BY TrackCount DESC;
```
## Web UI Usage

- The web application provides autocomplete for artist and album search fields, prioritizing exact, prefix, and contains matches, ordered by popularity.
- All report queries use the same match prioritization for consistent, relevant results.

## Maintenance

### Re-running the ETL

If you need to reload data (full refresh):

```powershell
# 1. Clear existing data
sqlcmd -S localhost -E -d SpotifyDW -Q "DELETE FROM FactTrack; DELETE FROM DimTrack; DELETE FROM DimAlbum; DELETE FROM DimArtist; DELETE FROM DimDate;"

# 2. Run ETL again
cd src/SpotifyDW.ETL
dotnet run
```

### Backing Up the Database

```powershell
sqlcmd -S localhost -E -Q "BACKUP DATABASE SpotifyDW TO DISK='C:\Backups\SpotifyDW.bak' WITH INIT;"
```

### Restoring from Backup

```powershell
sqlcmd -S localhost -E -Q "RESTORE DATABASE SpotifyDW FROM DISK='C:\Backups\SpotifyDW.bak' WITH REPLACE;"
```

## Troubleshooting


### Error: "Invalid column name 'Popularity'"

**Solution:**
- Ensure all queries use the correct column name: `TrackPopularity` (not `Popularity`).
- Update any custom SQL or code to reference the correct column.

### Error: "Cannot connect to database"

**Solution:**
- Verify SQL Server is running: `sqlcmd -S localhost -E -Q "SELECT @@VERSION"`
- Check connection string in `appsettings.json`
- Ensure Windows Authentication is enabled

### Error: "CSV file not found"

**Solution:**
- Verify CSV files are in the `data/` directory
- Check file names match exactly (including spaces)
- Ensure paths in `appsettings.json` are correct

### Error: "Duplicate key violation"

**Solution:**
- Clear existing data before re-running ETL
- Run: `DELETE FROM FactTrack; DELETE FROM DimTrack; DELETE FROM DimAlbum; DELETE FROM DimArtist; DELETE FROM DimDate;`

### Performance Issues

**Symptoms:** ETL takes longer than 2-3 minutes

**Solutions:**
- Check SQL Server resource usage
- Ensure indexes are created (verify with schema.sql)
- Consider running on a dedicated SQL Server instance

## Command Reference

### Quick Commands

```powershell
# Create database
sqlcmd -S localhost -E -i db\schema.sql

# Build ETL
cd src\SpotifyDW.ETL; dotnet build

# Run ETL
dotnet run

# Clear data
sqlcmd -S localhost -E -d SpotifyDW -Q "DELETE FROM FactTrack; DELETE FROM DimTrack; DELETE FROM DimAlbum; DELETE FROM DimArtist; DELETE FROM DimDate;"

# Row counts
sqlcmd -S localhost -E -d SpotifyDW -Q "SELECT COUNT(*) FROM DimArtist; SELECT COUNT(*) FROM FactTrack;"
```

## Future Work

### Incremental Load Strategy

**Current Limitation:** Full refresh only (truncate and reload all data)

**Proposed Enhancement:**
- Implement change data capture (CDC) to identify new/updated tracks
- Add `SourceLastModified` column to track CSV file timestamps
- Compare existing data with source to determine delta
- Load only new or changed records

**Benefits:**
- Faster ETL runs for large datasets
- Reduced database load
- Ability to run more frequently (hourly vs daily)

### Slowly Changing Dimensions (SCD)

**Current Limitation:** Type 1 SCD (overwrites) for all dimensions

**Proposed Enhancement:**

**Type 2 SCD for DimArtist:**
- Track historical changes in artist popularity and followers
- Add columns: `EffectiveDate`, `EndDate`, `IsCurrent`
- Maintain history of artist metrics over time

Example schema:
```sql
ALTER TABLE DimArtist ADD EffectiveDate DATE;
ALTER TABLE DimArtist ADD EndDate DATE;
ALTER TABLE DimArtist ADD IsCurrent BIT DEFAULT 1;
```

**Benefits:**
- Analyze artist growth trends
- Compare current vs historical popularity
- Time-series analysis of artist evolution

### Additional Enhancements

1. **Error Logging & Monitoring**
   - Log ETL errors to database table
   - Email notifications on failures
   - Track ETL run history and duration

2. **Data Quality Checks**
   - Pre-load validation rules
   - Reject records with missing critical fields
   - Data quality dashboard

3. **Audio Features Integration**
   - Connect to Spotify API to fetch audio features (energy, danceability, etc.)
   - Populate audio feature columns in FactTrack
   - Enable advanced music analysis

4. **Partitioning**
   - Partition FactTrack by Year (ReleaseDateKey)
   - Improve query performance for time-based analysis
   - Easier archival of old data

5. **OLAP Cube**
   - Build SQL Server Analysis Services (SSAS) cube
   - Pre-aggregate common metrics
   - Enable faster business intelligence queries

6. **Power BI Integration**
   - Create Power BI semantic model
   - Build pre-configured dashboards
   - Enable self-service analytics

7. **Automation**
   - Schedule ETL with SQL Server Agent or Windows Task Scheduler
   - Parameterize for different environments (Dev, Test, Prod)
   - CI/CD pipeline integration

## Support

For issues or questions:
- Review logs in console output
- Check SQL Server error logs
- Verify prerequisites are met
- Consult `architecture.md` for design details

## Version History

- **v1.0** (November 2025) - Initial release
  - Full refresh ETL
  - Star schema with 4 dimensions + 1 fact
  - Support for 2 CSV sources
