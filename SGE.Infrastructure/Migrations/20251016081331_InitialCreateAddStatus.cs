using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateAddStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "Attendances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "Attendances");
        }
    }
}
