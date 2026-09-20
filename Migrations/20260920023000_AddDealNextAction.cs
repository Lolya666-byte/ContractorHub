using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractorHub.Migrations
{
    [DbContext(typeof(ContractorHub.Data.AppDbContext))]
    [Migration("20260920023000_AddDealNextAction")]
    public partial class AddDealNextAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "NextAction", table: "Deals", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "NextActionAt", table: "Deals", type: "timestamp with time zone", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NextAction", table: "Deals");
            migrationBuilder.DropColumn(name: "NextActionAt", table: "Deals");
        }
    }
}
