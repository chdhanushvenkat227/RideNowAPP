using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideNowAPP.Migrations
{
    /// <inheritdoc />
    public partial class AddedDriverToUserFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackType",
                table: "Feedbacks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackType",
                table: "Feedbacks");
        }
    }
}
