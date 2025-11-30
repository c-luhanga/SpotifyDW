using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotifyDW.Web.Pages.Reports;

public class IndexModel : PageModel
{
    public IReadOnlyList<ReportLink> Reports { get; private set; } = Array.Empty<ReportLink>();

    public void OnGet()
    {
        Reports = new List<ReportLink>
        {
            new ReportLink
            {
                Title = "Top Artists by Year",
                Description = "View the top artists for a specific year based on average track popularity.",
                PageName = "/Reports/TopArtistsByYear"
            },
            new ReportLink
            {
                Title = "Top Tracks for an Artist",
                Description = "Search for an artist and view their most popular tracks.",
                PageName = "/Reports/TopTracksForArtist"
            },
            new ReportLink
            {
                Title = "Popularity Trend by Year",
                Description = "Analyze average track popularity trends over time.",
                PageName = "/Reports/PopularityTrendByYear"
            },
            new ReportLink
            {
                Title = "Compare Two Artists",
                Description = "Compare statistics and audio features between two artists.",
                PageName = "/Reports/CompareTwoArtists"
            },
            new ReportLink
            {
                Title = "Audio Profile of an Artist",
                Description = "Explore the audio characteristics and features of an artist's music.",
                PageName = "/Reports/AudioProfile"
            },
            new ReportLink
            {
                Title = "Custom Report Builder",
                Description = "Build your own custom report by selecting a measure, grouping, and filters.",
                PageName = "/Reports/CustomReportBuilder"
            }
        };
    }

    public class ReportLink
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string PageName { get; set; } = "";
    }
}
