using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Web.Data;
using NewsAggregator.Web.Models;

namespace NewsAggregator.Web.Pages;

// PageModel handles the logic and data fetching (similar to an Angular component class)
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    // Dependency Injection: ASP.NET Core automatically injects AppDbContext here
    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    // Properties bound to the HTML view (@Model.Articles)
    public List<Article> Articles { get; set; } = [];
    public List<string> AvailableSources { get; set; } = [];

    // Reads query parameters from the URL (e.g., /?SearchTerm=dotnet&SelectedSource=Heise)
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedSource { get; set; }

    // Executed automatically on HTTP GET requests to "/"
    public async Task OnGetAsync()
    {
        // Load distinct sources for the filter dropdown
        AvailableSources = await _db.Articles
            .Select(a => a.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        // Start building a dynamic SQL query (not executed against SQLite yet)
        IQueryable<Article> query = _db.Articles.AsQueryable();

        // Apply keyword filter if the user typed something into the search box
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                (a.Summary != null && a.Summary.ToLower().Contains(term)));
        }

        // Apply source filter if a specific source was selected in the dropdown
        if (!string.IsNullOrWhiteSpace(SelectedSource))
        {
            query = query.Where(a => a.Source == SelectedSource);
        }

        // Execute the final SQL query ordered by newest articles first
        Articles = await query
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // Executed when a user submits a form with asp-page-handler="Delete"
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article != null)
        {
            _db.Articles.Remove(article);
            await _db.SaveChangesAsync();
        }

        // Post-Redirect-Get pattern: redirects back to GET "/" preserving active filters
        return RedirectToPage(new { SearchTerm, SelectedSource });
    }
}