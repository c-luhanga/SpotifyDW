using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services.Reports;

namespace SpotifyDW.Web.Pages.Reports;

public class TopTracksForArtistModel : PageModel
{
    private readonly TopTracksForArtistService _service;

    public TopTracksForArtistModel(TopTracksForArtistService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public string? ArtistPattern { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Limit { get; set; } = 20;

    public IReadOnlyList<TopTracksForArtistService.TopTrackResult> Results { get; set; } = Array.Empty<TopTracksForArtistService.TopTrackResult>();

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ArtistPattern))
        {
            var results = await _service.GetTopTracksAsync(ArtistPattern, Limit);
            Results = results.ToList();
        }
    }
}
