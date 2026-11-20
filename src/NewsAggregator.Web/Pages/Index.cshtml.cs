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
    private readonly IHttpClientFactory _httpClientFactory;

    // Dependency Injection: ASP.NET Core automatically injects AppDbContext here
    public IndexModel(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    // Properties bound to the HTML view (@Model.Articles)
    public List<Article> Articles { get; set; } = [];
    public List<string> AvailableSources { get; set; } = [];

    // Reads query parameters from the URL (e.g., /?SearchTerm=dotnet&SelectedSource=Heise)
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    // Controls whether multiple comma-separated search terms use "OR" or "AND" logic
    [BindProperty(SupportsGet = true)]
    public string FilterMode { get; set; } = "OR";

    // Sources
    [BindProperty(SupportsGet = true)]
    public string? SelectedSource { get; set; }

    // Bookmarks
    [BindProperty(SupportsGet = true)]
    public bool OnlyBookmarked { get; set; }

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
        // Split comma-separated keywords/categories into clean, lowercase terms
        // Example: "Apple, KI, Linux" -> ["apple", "ki", "linux"]
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var terms = SearchTerm
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLower())
                .Distinct()
                .ToArray();

            if (terms.Length > 0)
            {
                if (FilterMode.Equals("AND", StringComparison.OrdinalIgnoreCase))
                {
                    // AND logic: Chaining .Where() requires the article to match EVERY term
                    foreach (var term in terms)
                    {
                        query = query.Where(a =>
                            a.Title.ToLower().Contains(term) ||
                            (a.Summary != null && a.Summary.ToLower().Contains(term)));
                    }
                }
                else
                {
                    // OR logic: .Any() checks if the article matches AT LEAST ONE term
                    // Note: EF Core translates array.Any(t => col.Contains(t)) cleanly into SQL OR statements
                    query = query.Where(a =>
                        terms.Any(term =>
                            a.Title.ToLower().Contains(term) ||
                            (a.Summary != null && a.Summary.ToLower().Contains(term))));
                }
            }
        }

        // Apply source filter if a specific source was selected in the dropdown
        if (!string.IsNullOrWhiteSpace(SelectedSource))
        {
            query = query.Where(a => a.Source == SelectedSource);
        }

        // Apply bookmark filter if the "OnlyBookmarked" checkbox/toggle is active
        if (OnlyBookmarked)
        {
            query = query.Where(a => a.IsBookmarked);
        }

        // Execute the final SQL query ordered by newest articles first
        Articles = await query
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // Calls PATCH /api/articles/{id}/bookmark instead of modifying _db directly
    public async Task<IActionResult> OnPostToggleBookmarkAsync(int id)
    {
        var client = CreateApiClient();

        // Send an HTTP PATCH request to our Minimal API endpoint
        var response = await client.PatchAsync($"/api/articles/{id}/bookmark", content: null);

        // Optional: You could check !response.IsSuccessStatusCode here to show an error message
        return RedirectToPage(new { SearchTerm, SelectedSource, OnlyBookmarked });
    }

    // Calls DELETE /api/articles/{id} instead of modifying _db directly
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var client = CreateApiClient();

        // Send an HTTP DELETE request to our Minimal API endpoint
        var response = await client.DeleteAsync($"/api/articles/{id}");

        return RedirectToPage(new { SearchTerm, SelectedSource, OnlyBookmarked });
    }

    // Helper method to configure an HttpClient pointing to this application's own base URL
    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient();
        // Builds e.g. "http://localhost:5223" dynamically from the current incoming request
        client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
        return client;
    }
    
}