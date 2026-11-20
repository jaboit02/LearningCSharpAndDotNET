using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsAggregator.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBookmarkedToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBookmarked",
                table: "Articles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBookmarked",
                table: "Articles");
        }
    }
}
