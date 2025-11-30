using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services.Reports;

namespace SpotifyDW.Web.Pages.Reports;

public class CompareTwoArtistsModel : PageModel
{
    private readonly CompareTwoArtistsService _service;

    public CompareTwoArtistsModel(CompareTwoArtistsService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public string? Artist1 { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Artist2 { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MinYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxYear { get; set; }

    public CompareTwoArtistsService.ComparisonResult? ComparisonResult { get; set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Artist1) && !string.IsNullOrWhiteSpace(Artist2))
        {
            ComparisonResult = await _service.CompareAsync(Artist1, Artist2, MinYear, MaxYear);
        }
    }
}
