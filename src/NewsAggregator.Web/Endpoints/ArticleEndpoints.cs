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
        group.MapPost("", async (Article article, AppDbContext db) =>
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
    
        // PATCH /api/articles/{id}/bookmark -> Toggles the bookmark status of an article
        group.MapPatch("/{id:int}/bookmark", async (int id, AppDbContext db) =>
        {
            // FindAsync searches the database by primary key (Id)
            var article = await db.Articles.FindAsync(id);
            if (article is null)
            {
                // Return HTTP 404 if no article with that Id exists
                return Results.NotFound(new { message = $"Article with ID {id} not found." });
            }

            // Flip the boolean value (true -> false, or false -> true)
            article.IsBookmarked = !article.IsBookmarked;

            // Persist the change to SQLite
            await db.SaveChangesAsync();

            // Return HTTP 200 OK with the updated status
            return Results.Ok(new { article.Id, article.IsBookmarked });
        })
        .WithName("ToggleBookmark")
        .WithSummary("Toggles the bookmark status of an article");

        // GET /api/articles/bookmarks -> Retrieves all bookmarked articles
        group.MapGet("/bookmarks", async (AppDbContext db) =>
        {
            var bookmarkedArticles = await db.Articles
                .Where(a => a.IsBookmarked) // Filter only rows where IsBookmarked == true
                .OrderByDescending(a => a.PublishedAt) // Show newest articles first
                .ToListAsync();

            return Results.Ok(bookmarkedArticles);
        })
        .WithName("GetBookmarkedArticles")
        .WithSummary("Retrieves all bookmarked articles");

        // DELETE /api/articles/{id} -> Deletes an article from SQLite by its ID
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        /* Spring Boot Equivalent:
        @DeleteMapping("/{id}")
        public ResponseEntity<?> deleteArticle(@PathVariable int id) { ... }
        */
        {
            // Look up the article by primary key
            var article = await db.Articles.FindAsync(id);
            if (article is null)
            {
                // Return HTTP 404 Not Found if the ID does not exist
                return Results.NotFound(new { message = $"Article with ID {id} not found." });
            }

            // Mark the entity for deletion in EF Core's Change Tracker
            db.Articles.Remove(article);

            // Execute the SQL DELETE statement against SQLite
            await db.SaveChangesAsync();

            // Return HTTP 204 No Content (standard REST response for successful deletion)
            return Results.NoContent();
        })
        .WithName("DeleteArticle")
        .WithSummary("Deletes an article by ID");
    }
}
