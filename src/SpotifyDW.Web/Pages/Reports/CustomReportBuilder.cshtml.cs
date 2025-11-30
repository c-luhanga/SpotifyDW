using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpotifyDW.Web.Services.Reports;
using static SpotifyDW.Web.Services.Reports.CustomReportBuilderService;

namespace SpotifyDW.Web.Pages.Reports;

public class CustomReportBuilderModel : PageModel
{
    private readonly CustomReportBuilderService _service;

    public CustomReportBuilderModel(CustomReportBuilderService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public MeasureType Measure { get; set; } = MeasureType.Popularity;

    [BindProperty(SupportsGet = true)]
    public GroupingType Grouping { get; set; } = GroupingType.Artist;

    [BindProperty(SupportsGet = true)]
    public int? MinYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinPopularity { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ArtistPattern { get; set; }

    public IReadOnlyList<CustomReportRow> Results { get; set; } = Array.Empty<CustomReportRow>();

    public bool HasSearched { get; set; }

    public List<SelectListItem> MeasureOptions { get; } = new()
    {
        new SelectListItem { Value = "0", Text = "Popularity" },
        new SelectListItem { Value = "1", Text = "Energy" },
        new SelectListItem { Value = "2", Text = "Danceability" },
        new SelectListItem { Value = "3", Text = "Valence" },
        new SelectListItem { Value = "4", Text = "Tempo" }
    };

    public List<SelectListItem> GroupingOptions { get; } = new()
    {
        new SelectListItem { Value = "0", Text = "By Artist" },
        new SelectListItem { Value = "1", Text = "By Year" },
        new SelectListItem { Value = "2", Text = "By Album" },
        new SelectListItem { Value = "3", Text = "By Artist + Year" }
    };

    public async Task OnGetAsync()
    {
        if (Request.Query.Count > 0)
        {
            HasSearched = true;
            var results = await _service.ExecuteCustomReportAsync(
                Measure,
                Grouping,
                MinYear,
                MaxYear,
                MinPopularity,
                ArtistPattern);
            Results = results.ToList();
        }
    }

    public string GetMeasureName()
    {
        return Measure switch
        {
            MeasureType.Energy => "Energy",
            MeasureType.Danceability => "Danceability",
            MeasureType.Valence => "Valence",
            MeasureType.Tempo => "Tempo",
            _ => "Popularity"
        };
    }

    public string GetGroupingName()
    {
        return Grouping switch
        {
            GroupingType.Year => "Year",
            GroupingType.Album => "Album",
            GroupingType.ArtistYear => "Artist + Year",
            _ => "Artist"
        };
    }
}
