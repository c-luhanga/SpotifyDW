using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services.Reports;

namespace SpotifyDW.Web.Pages.Reports;

public class AudioProfileModel : PageModel
{
    private readonly AudioProfileService _service;

    public AudioProfileModel(AudioProfileService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public string? ArtistPattern { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxYear { get; set; }

    public IReadOnlyList<AudioProfileService.ArtistProfileResult> Results { get; set; } = Array.Empty<AudioProfileService.ArtistProfileResult>();

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(ArtistPattern))
        {
            var results = await _service.GetProfileAsync(ArtistPattern, MinYear, MaxYear);
            Results = results.ToList();
        }
    }
}
