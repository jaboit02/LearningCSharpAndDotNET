using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Web.Models;

public class Article
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    public string? Summary { get; set; }

    [Required]
    public string Source { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsBookmarked { get; set; } = false;
}
