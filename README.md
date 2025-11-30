# SpotifyDW

A dimensional data warehouse for Spotify track analysis, built with SQL Server and C#. Implements a star schema design with Extract-Transform-Load (ETL) pipeline for processing and analyzing music data from multiple CSV sources.

## 🎵 Features

- **Star Schema Design** - Optimized for analytical queries with 4 dimension tables and 1 fact table
- **Multi-Source ETL** - Combines data from multiple CSV files (17,360+ tracks)
- **Data Quality** - Normalization, deduplication, and referential integrity enforcement
- **Transactional Loading** - Ensures data consistency with rollback capability
- **Scalable Architecture** - Clean separation of Extract, Transform, and Load phases

## 📊 Data Warehouse Stats

- **2,551** unique artists
- **5,317** albums
- **8,778** tracks
- **2,385** date records
- **16,956** fact records

## 🏗️ Project Structure

```
SpotifyDW/
├── data/                          # Source CSV files
│   ├── spotify_data_clean.csv     # Contemporary track data
│   └── track_data_final.csv       # Historic track data
├── db/
│   └── schema.sql                 # Database creation script
├── src/
│   └── SpotifyDW.ETL/             # C# ETL console application
│       ├── Models/                # Data models (Raw, Dim, Fact)
│       ├── Services/              # ETL service classes
│       └── appsettings.json       # Configuration
└── docs/
    ├── architecture.md            # Star schema design & ETL flow
    ├── runbook.md                 # Setup & operation guide
    └── notes.md                   # CSV analysis & requirements
```

## 🚀 Quick Start

### Prerequisites
- SQL Server (2019+)
- .NET SDK (10.0+)
- sqlcmd (SQL Server command-line tools)

### Setup (5 minutes)

1. **Clone the repository**
   ```powershell
   git clone https://github.com/c-luhanga/SpotifyDW.git
   cd SpotifyDW
   ```

2. **Create the database**
   ```powershell
   sqlcmd -S localhost -E -i db\schema.sql
   ```

3. **Add your CSV files**
   ```powershell
   # Place these files in the data/ folder:
   # - spotify_data_clean.csv
   # - track_data_final.csv
   ```

4. **Run the ETL**
   ```powershell
   cd src\SpotifyDW.ETL
   dotnet run
   ```

**That's it!** Your data warehouse is now populated and ready for analysis.

## 📖 Documentation

- **[Architecture Guide](docs/architecture.md)** - Star schema design, ETL flow, and technical details
- **[Runbook](docs/runbook.md)** - Complete setup instructions, troubleshooting, and sample queries
- **[Design Notes](docs/notes.md)** - CSV analysis and dimensional modeling decisions

## 🔍 Sample Queries

**Most popular tracks:**
```sql
SELECT TOP 10 a.ArtistName, t.TrackName, f.TrackPopularity
FROM FactTrack f
JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
JOIN DimTrack t ON f.TrackKey = t.TrackKey
ORDER BY f.TrackPopularity DESC;
```

**Tracks by release year:**
```sql
SELECT d.Year, COUNT(*) AS TrackCount, AVG(f.TrackPopularity) AS AvgPopularity
FROM FactTrack f
JOIN DimDate d ON f.ReleaseDateKey = d.DateKey
GROUP BY d.Year
ORDER BY d.Year DESC;
```

**Most prolific artists:**
```sql
SELECT TOP 10 a.ArtistName, 
    COUNT(DISTINCT f.TrackKey) AS TrackCount,
    MAX(a.ArtistFollowers) AS Followers
FROM FactTrack f
JOIN DimArtist a ON f.ArtistKey = a.ArtistKey
GROUP BY a.ArtistName
ORDER BY TrackCount DESC;
```

## 🛠️ Technology Stack

- **Database:** SQL Server
- **Language:** C# (.NET 10.0)
- **Libraries:**
  - CsvHelper - CSV parsing
  - Dapper - Data access
  - Microsoft.Data.SqlClient - Database connectivity
  - Microsoft.Extensions.Configuration - Configuration management

## 🔮 Future Enhancements

- **Incremental Loads** - Load only new/changed records instead of full refresh
- **Type 2 SCD** - Track historical changes in artist popularity over time
- **Audio Features** - Integrate Spotify API for advanced music analysis
- **Power BI Dashboards** - Pre-built visualizations for business insights
- **Automated Scheduling** - Daily ETL runs with error notifications

See [Runbook - Future Work](docs/runbook.md#future-work) for detailed roadmap.

## 📝 License

This project is licensed under the MIT License.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📧 Contact

For questions or issues, please open an issue on GitHub.

