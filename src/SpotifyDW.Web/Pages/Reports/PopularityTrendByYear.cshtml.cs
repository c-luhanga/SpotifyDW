using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services.Reports;

namespace SpotifyDW.Web.Pages.Reports;

public class PopularityTrendByYearModel : PageModel
{
    private readonly PopularityTrendByYearService _service;

    public PopularityTrendByYearModel(PopularityTrendByYearService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [BindProperty(SupportsGet = true)]
    public int? MinYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MaxYear { get; set; }

    public IReadOnlyList<PopularityTrendByYearService.YearTrendResult> Results { get; set; } = Array.Empty<PopularityTrendByYearService.YearTrendResult>();

    public bool HasSearched { get; set; }

    public async Task OnGetAsync()
    {
        // Always query if the page has been submitted (even if both are null)
        if (Request.Query.Count > 0)
        {
            HasSearched = true;
            var results = await _service.GetTrendAsync(MinYear, MaxYear);
            Results = results.ToList();
        }
    }
}
