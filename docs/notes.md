# SpotifyDW - Notes

## Project Overview
Data warehouse project for Spotify data analysis.

## Data Sources

### 1. spotify_data_clean.csv
**What one row represents:** A single Spotify track with associated artist and album information (without audio features).

**Total rows:** 8,584 tracks

#### Column Structure:

**Identity Columns (Dimensions):**
- `track_id` (string) - Unique Spotify track identifier
- `track_name` (string) - Name of the track
- `artist_name` (string) - Primary artist name
- `album_id` (string) - Unique Spotify album identifier
- `album_name` (string) - Name of the album
- `album_release_date` (string/date) - Release date (YYYY-MM-DD format)


**Metrics (Measures):**
- `track_number` (int) - Track position on album
- `track_popularity` (int) - Popularity score (0-100)
- `track_duration_min` (float) - Track length in minutes (decimal format)
- `artist_popularity` (int) - Artist popularity score (0-100)
- `artist_followers` (int) - Number of artist followers on Spotify
- **Note:** In the data warehouse, the correct column for popularity is `TrackPopularity` (not `Popularity`).

**Categories (Attributes):**
- `explicit` (bool) - Whether track has explicit content (TRUE/FALSE)
- `artist_genres` (string) - Comma-separated genre tags or "N/A" if missing
- `album_total_tracks` (int) - Total number of tracks on album
- `album_type` (string) - Type of album (album, single)

**Key Observations:**
- Duration is in **minutes** (not milliseconds like track_data_final.csv)
- artist_genres uses comma-separated values or "N/A" (not JSON array format)
- Some tracks have very recent release dates (2025)
- Explicit field uses TRUE/FALSE (not True/False)
- No audio features (energy, danceability, etc.) in this dataset

### 2. track_data_final.csv
**What one row represents:** A single Spotify track with associated artist, album, and audio feature metadata.

**Total rows:** 8,780 tracks

#### Column Structure:

**Identity Columns (Dimensions):**
- `track_id` (string) - Unique Spotify track identifier
- `track_name` (string) - Name of the track
- `artist_name` (string) - Primary artist name
- `album_id` (string) - Unique Spotify album identifier
- `album_name` (string) - Name of the album
- `album_release_date` (string/date) - Release date (YYYY-MM-DD format)

**Metrics (Measures):**
- `track_number` (int) - Track position on album
- `track_popularity` (int) - Popularity score (0-100)
- `track_duration_ms` (int) - Track length in milliseconds
- `artist_popularity` (float) - Artist popularity score (0-100)
- `artist_followers` (float) - Number of artist followers on Spotify

**Categories (Attributes):**
- `explicit` (bool) - Whether track has explicit content (True/False)
- `artist_genres` (string/array) - JSON-like array of genre tags (e.g., ['pop'], ['hip hop', 'west coast hip hop'])
- `album_total_tracks` (int) - Total number of tracks on album
- `album_type` (string) - Type of album (album, single, compilation)

**Key Observations:**
- Some fields have missing values (e.g., artist_popularity, artist_followers shown as floats with nulls possible)
- artist_genres is stored as a string representation of a list
- Multiple tracks can share the same artist_id, album_id (one-to-many relationships)
- Dates are in ISO format (YYYY-MM-DD)

## Star Schema Design (Logical Model)

### Dimension Tables

#### DimArtist
Stores unique artist information.
- `ArtistKey` (int, PK) - Surrogate key
- `ArtistName` (nvarchar) - Artist name
- `ArtistPopularity` (int) - Current popularity score (0-100)
- `ArtistFollowers` (bigint) - Number of followers
- `ArtistGenres` (nvarchar) - Comma-separated or JSON list of genres

**Business Key:** ArtistName (no artist_id in source data)

---

#### DimAlbum
Stores unique album information.
- `AlbumKey` (int, PK) - Surrogate key
- `SpotifyAlbumId` (nvarchar) - Spotify album identifier
- `AlbumName` (nvarchar) - Album name
- `ArtistKey` (int, FK) - Reference to DimArtist
- `AlbumType` (nvarchar) - Type: album, single, compilation
- `AlbumTotalTracks` (int) - Total tracks on album
- `ReleaseDateKey` (int, FK) - Reference to DimDate

**Business Key:** SpotifyAlbumId

---

#### DimTrack
Stores unique track information (attributes that don't change).
- `TrackKey` (int, PK) - Surrogate key
- `SpotifyTrackId` (nvarchar) - Spotify track identifier
- `TrackName` (nvarchar) - Track name
- `TrackNumber` (int) - Position on album
- `TrackDurationMs` (int) - Duration in milliseconds
- `Explicit` (bit) - Explicit content flag

**Business Key:** SpotifyTrackId

---

#### DimDate
Standard date dimension for time-based analysis.
- `DateKey` (int, PK) - Format: YYYYMMDD
- `Date` (date) - Full date
- `Year` (int) - Year
- `Month` (int) - Month (1-12)
- `MonthName` (nvarchar) - Month name
- `Quarter` (int) - Quarter (1-4)
- `DayOfWeek` (int) - Day of week (1-7)
- `DayName` (nvarchar) - Day name

**Business Key:** Date

---

### Fact Table

#### FactTrack
Central fact table storing track metrics and audio features.
- `FactTrackKey` (bigint, PK) - Surrogate key
- `TrackKey` (int, FK) - Reference to DimTrack
- `ArtistKey` (int, FK) - Reference to DimArtist
- `AlbumKey` (int, FK) - Reference to DimAlbum
- `ReleaseDateKey` (int, FK) - Reference to DimDate


**Measures (Metrics):**
- `TrackPopularity` (int) - Popularity score at time of load (0-100)
- `Energy` (float) - Audio feature (0.0-1.0) *[if available in source]*
- `Danceability` (float) - Audio feature (0.0-1.0) *[if available]*
- `Valence` (float) - Audio feature (0.0-1.0) *[if available]*
- `Loudness` (float) - Audio feature in dB *[if available]*
- `Tempo` (float) - BPM *[if available]*
- `Acousticness` (float) - Audio feature (0.0-1.0) *[if available]*
- `Instrumentalness` (float) - Audio feature (0.0-1.0) *[if available]*
- `Liveness` (float) - Audio feature (0.0-1.0) *[if available]*
- `Speechiness` (float) - Audio feature (0.0-1.0) *[if available]*
- **Note:** Audio features are included in the schema for future enrichment; not all source CSVs provide them.
## Search & Query Logic

- All artist and album search fields, as well as report queries, now prioritize exact, prefix, and contains matches, ordered by popularity for relevance.

**Grain:** One row per unique track (SpotifyTrackId)

**Note:** Audio features (energy, danceability, etc.) are not present in current CSVs. These columns are included in the design for future enhancement if audio feature data becomes available.

---

### Relationships
```
FactTrack -----> DimTrack (TrackKey)
FactTrack -----> DimArtist (ArtistKey)
FactTrack -----> DimAlbum (AlbumKey)
FactTrack -----> DimDate (ReleaseDateKey)
DimAlbum  -----> DimArtist (ArtistKey)
DimAlbum  -----> DimDate (ReleaseDateKey)
```

---

## Next Steps
1. ✅ Inspect track_data_final.csv structure
2. ✅ Inspect spotify_data_clean.csv structure
3. ✅ Design star schema (logical model)
4. Implement schema.sql with DDL statements
5. Create C# ETL console application
6. Implement data loading process

