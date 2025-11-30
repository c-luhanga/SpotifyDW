namespace SpotifyDW.ETL.Models.Dim;

/// <summary>
/// Standard date dimension table
/// </summary>
public class DimDate
{
    public int DateKey { get; set; }
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Quarter { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsWeekend { get; set; }
    public DateTime CreatedDate { get; set; }
}
