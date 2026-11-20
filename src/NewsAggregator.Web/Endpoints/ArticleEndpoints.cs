using Microsoft.EntityFrameworkCore;
using NewsAggregator.Web.Data;
using NewsAggregator.Web.Models;

namespace NewsAggregator.Web.Endpoints;

// Static class required to define C# Extension Methods
public static class ArticleEndpoints
{
    // The 'this IEndpointRouteBuilder app' parameter turns this into an Extension Method.
    // It allows to call 'app.MapArticleEndpoints()' directly inside Program.cs
    public static void MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        // MapGroup groups all routes under "/api/articles" and tags them cleanly for Scalar/OpenAPI
        var group = app.MapGroup("/api/articles")
                       .WithTags("Articles");

        // POST /api/articles -> Receives a new article from n8n and saves it to SQLite
        group.MapPost("/", async (Article article, AppDbContext db) =>
        /* Spring Boot Equivalent:
        @PostMapping("/")
        public ResponseEntity<?> createArticle(@RequestBody Article article) {
            // ... Logik ...
        }*/
        {
            // Check if an article with the exact same URL already exists (to prevent duplicates)
            bool exists = await db.Articles.AnyAsync(a => a.Url == article.Url);
            /* Spring Boot Equivalent: 
            bool exists = false;
            foreach (Article a in articles) // <--- Variable
            {
                if (a.Url == article.Url)   // <--- Check
                {
                    exists = true;
                    break;
                }
            }
            */
            if (exists)
            {
                // HTTP 409 Conflict tells n8n that this article was already imported
                return Results.Conflict(new { message = "Article with this URL already exists." });
            }

            // CreatedAt is set to current UTC time
            article.CreatedAt = DateTime.UtcNow;

            // Add to EF Core change tracker and write to SQLite
            db.Articles.Add(article);
            await db.SaveChangesAsync();

            // HTTP 201 Created returns the saved article including its newly generated database Id
            return Results.Created($"/api/articles/{article.Id}", article);
        })
        .WithName("CreateArticle")
        .WithSummary("Creates a new tech article in the database")
        .WithDescription("Receives an article payload (e.g., from n8n), checks for duplicate URLs, and persists it to SQLite.");
    }
}
