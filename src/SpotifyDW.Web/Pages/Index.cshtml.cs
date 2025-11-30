using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyDW.Web.Services;

namespace SpotifyDW.Web.Pages;

public class IndexModel : PageModel
{
    private readonly HomeStatsService _statsService;

    public IndexModel(HomeStatsService statsService)
    {
        _statsService = statsService;
    }

    public HomeStatsService.HomeStats? Stats { get; set; }

    public async Task OnGetAsync()
    {
        Stats = await _statsService.GetStatsAsync();
    }
}
