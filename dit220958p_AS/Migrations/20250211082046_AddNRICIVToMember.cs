using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dit220958p_AS.Migrations
{
    /// <inheritdoc />
    public partial class AddNRICIVToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NRIC_IV",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NRIC_IV",
                table: "Members");
        }
    }
}
