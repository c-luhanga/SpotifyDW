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

## Next Steps
1. ✅ Inspect track_data_final.csv structure
2. Inspect spotify_data_clean.csv (when available)
3. Design dimensional model (schema.sql)
4. Create C# ETL console application
5. Implement data loading process

