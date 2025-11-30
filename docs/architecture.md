# SpotifyDW Architecture

## Overview
SpotifyDW is a dimensional data warehouse built using the Star Schema design pattern for analyzing Spotify track data. The warehouse consolidates data from multiple CSV sources and enables efficient querying for business intelligence and analytics.

## Star Schema Design

### ASCII Diagram
```
                    ┌─────────────┐
                    │   DimDate   │
                    ├─────────────┤
                    │ DateKey (PK)│
                    │ Date        │
                    │ Year        │
                    │ Month       │
                    │ Quarter     │
                    └──────┬──────┘
                           │
                           │
    ┌─────────────┐        │        ┌─────────────┐
    │  DimArtist  │        │        │  DimAlbum   │
    ├─────────────┤        │        ├─────────────┤
    │ArtistKey(PK)│        │        │ AlbumKey(PK)│
    │ ArtistName  │        │        │ AlbumName   │
    │ Popularity  │        │        │ ArtistKey   │◄──┐
    │ Followers   │        │        │ AlbumType   │   │
    │ Genres      │        │        │ ReleaseDate │   │
    └──────┬──────┘        │        └──────┬──────┘   │
           │               │               │          │
           │               │               │          │
           │        ┌──────▼───────┐       │          │
           │        │  FactTrack   │       │          │
           │        ├──────────────┤       │          │
           └───────►│ ArtistKey(FK)│       │          │
                    │ AlbumKey (FK)│◄──────┘          │
                    │ TrackKey (FK)│◄──────┐          │
                    │ DateKey  (FK)│       │          │
                    ├──────────────┤       │          │
                    │ Popularity   │       │          │
                    │ Energy       │       │          │
                    │ Danceability │       │          │
                    │ Valence      │       │          │
                    │ Tempo        │       │          │
                    │ (other audio)│       │          │
                    └──────────────┘       │          │
                                           │          │
                                    ┌──────┴──────┐   │
                                    │  DimTrack   │   │
                                    ├─────────────┤   │
                                    │ TrackKey(PK)│   │
                                    │ TrackName   │   │
                                    │ Duration    │   │
                                    │ Explicit    │   │
                                    └─────────────┘   │
                                                      │
                                    Relationship ─────┘
                                    (via ArtistKey)
```

## Dimension Tables

### DimArtist
Stores unique artist information.
- **Business Key:** ArtistName
- **Attributes:** Popularity, Followers, Genres
- **Type:** Type 1 SCD (overwrites)

### DimAlbum
Stores unique album information with artist relationships.
- **Business Key:** SpotifyAlbumId
- **Attributes:** AlbumName, AlbumType, TotalTracks, ReleaseDate
- **Foreign Keys:** ArtistKey → DimArtist, ReleaseDateKey → DimDate

### DimTrack
Stores unique track information (static attributes).
- **Business Key:** SpotifyTrackId
- **Attributes:** TrackName, TrackNumber, Duration, Explicit flag
- **Type:** Type 1 SCD

### DimDate
Standard date dimension for time-based analysis.
- **Business Key:** DateKey (YYYYMMDD format)
- **Attributes:** Year, Month, Quarter, DayOfWeek, IsWeekend
- **Grain:** Daily

## Fact Table


### FactTrack
Central fact table storing track metrics and audio features.
- **Grain:** One row per unique track (SpotifyTrackId)
- **Foreign Keys:** TrackKey, ArtistKey, AlbumKey, ReleaseDateKey
- **Measures:**
  - **TrackPopularity** (0-100) *(correct column name; not 'Popularity')*
  - Audio features: Energy, Danceability, Valence, Loudness, Tempo, etc.
    - *Note: Audio features are included for future enrichment; not all source CSVs provide them.*
- **Type:** Snapshot fact table (single point in time)
## Web UI & Query Logic

- The web application provides autocomplete for artist and album search, prioritizing exact, prefix, and contains matches, ordered by popularity.
- All report queries use the same match prioritization for consistent, relevant results.

## ETL Flow

### 1. Extract Phase
**Purpose:** Read and combine data from multiple CSV sources.

```
CSV Sources:
├── spotify_data_clean.csv (8,582 tracks)
│   └── Contemporary data with duration in minutes
└── track_data_final.csv (8,778 tracks)
    └── Historic data with duration in milliseconds

Combined Output: 17,360 raw track records
```

**Key Operations:**
- Read CSV files using CsvHelper
- Handle different formats (TRUE/FALSE vs True/False for explicit flag)
- Convert duration units (minutes → milliseconds)
- Parse genre strings (comma-separated vs JSON arrays)
- Combine into unified `RawTrack` objects

### 2. Transform Phase
**Purpose:** Clean, normalize, and structure data into dimension and fact models.

```
Raw Tracks (17,360)
    │
    ├─► Normalize Data
    │   ├── Trim whitespace
    │   ├── Title case names
    │   └── Clean genre strings
    │
    ├─► Build Dimensions
    │   ├── DimArtist (2,551 unique)
    │   │   └── Group by normalized artist name
    │   ├── DimDate (2,385 unique)
    │   │   └── Extract from release dates
    │   ├── DimAlbum (5,317 unique)
    │   │   └── Group by Spotify Album ID
    │   └── DimTrack (8,778 unique)
    │       └── Group by Spotify Track ID
    │
    └─► Build Facts
        └── FactTrack (16,956 records)
            └── Map dimension keys via in-memory dictionaries
```

**Normalization Rules:**
- Artist/Album/Track names: Trim + Title Case
- Genres: Remove brackets/quotes, handle "N/A"
- Dates: Parse to DateTime, convert to DateKey (YYYYMMDD)
- Deduplication: Group by business keys (Spotify IDs)

**Key Mapping Strategy:**
1. Create temporary surrogate keys during dimension build
2. Build lookup dictionaries: `businessKey → tempKey`
3. Use dictionaries to populate fact table foreign keys
4. Replace with real DB keys during load phase

### 3. Load Phase
**Purpose:** Persist data to SQL Server with proper referential integrity.

```
Transaction Begin
    │
    ├─► Insert DimArtist (2,551 rows)
    │   └── Capture real ArtistKey using OUTPUT INSERTED
    │
    ├─► Insert DimDate (2,385 rows)
    │   └── DateKey is business key (no generation needed)
    │
    ├─► Insert DimAlbum (5,317 rows)
    │   └── Map temp ArtistKey → real ArtistKey
    │       Capture real AlbumKey
    │
    ├─► Insert DimTrack (8,778 rows)
    │   └── Capture real TrackKey
    │
    └─► Insert FactTrack (16,956 rows)
        └── Map all temp keys → real keys
            Insert with proper foreign keys
Transaction Commit
```

**Load Strategy:**
- **Transactional:** All-or-nothing using SQL transactions
- **Order:** Load dimensions first (parent → child), then facts
- **Key Mapping:** Use `OUTPUT INSERTED` clause to capture identity values
- **Bulk Operations:** Dapper for efficient batch inserts

**Error Handling:**
- Rollback entire transaction on failure
- Constraint violations (unique keys) cause rollback
- Foreign key violations prevented by load order

## Data Flow Summary

```
┌──────────────────┐
│   CSV Files      │
│  (Data Lake)     │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  EXTRACT         │
│  - CsvHelper     │
│  - Combine data  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  TRANSFORM       │
│  - Normalize     │
│  - Build dims    │
│  - Build facts   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  LOAD            │
│  - SQL Server    │
│  - Transactions  │
│  - Key mapping   │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  SpotifyDW       │
│  (Star Schema)   │
└──────────────────┘
```

## Technology Stack

- **Language:** C# (.NET 10.0)
- **Database:** SQL Server
- **ETL Framework:** Custom console application
- **Libraries:**
  - CsvHelper - CSV parsing
  - Dapper - Data access
  - Microsoft.Data.SqlClient - SQL connectivity
  - Microsoft.Extensions.Configuration - App settings

## Performance Considerations

- **Batch Processing:** All records loaded in memory before insert
- **Indexes:** Non-clustered indexes on foreign keys and common query columns
- **Transaction Scope:** Single transaction per table for rollback safety
- **Normalization:** In-memory dictionaries for O(1) key lookups

## Data Quality

- **Uniqueness:** Enforced via unique constraints on business keys
- **Referential Integrity:** Foreign key constraints between dimensions and facts
- **Null Handling:** Nullable columns for optional attributes
- **Audit Columns:** CreatedDate, ModifiedDate, LoadDate for tracking

## Scalability Notes

Current implementation is suitable for datasets up to ~100K records. For larger datasets, consider:
- Bulk insert operations (SqlBulkCopy)
- Parallel processing of dimension loads
- Incremental loads instead of full refresh
- Partitioning of fact tables by date
