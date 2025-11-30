using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services.Reports;

namespace SpotifyDW.Web.Pages.Reports;

public class TopArtistsByYearModel : PageModel
{
    private readonly TopArtistsByYearService _service;

    public TopArtistsByYearModel(TopArtistsByYearService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Limit { get; set; } = 10;

    public IReadOnlyList<TopArtistsByYearService.TopArtistResult> Results { get; set; } = Array.Empty<TopArtistsByYearService.TopArtistResult>();

    public async Task OnGetAsync()
    {
        if (Year.HasValue)
        {
            var results = await _service.GetTopArtistsAsync(Year.Value, Limit);
            Results = results.ToList();
        }
    }
}
