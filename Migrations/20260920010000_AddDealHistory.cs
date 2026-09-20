using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContractorHub.Migrations
{
    [DbContext(typeof(ContractorHub.Data.AppDbContext))]
    [Migration("20260920010000_AddDealHistory")]
    public partial class AddDealHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DealHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealHistories", x => x.Id);
                    table.ForeignKey("FK_DealHistories_Deals_DealId", x => x.DealId, "Deals", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_DealHistories_Users_ChangedByUserId", x => x.ChangedByUserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                });
            migrationBuilder.CreateIndex("IX_DealHistories_DealId", "DealHistories", "DealId");
            migrationBuilder.CreateIndex("IX_DealHistories_ChangedByUserId", "DealHistories", "ChangedByUserId");
        }
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "DealHistories");
    }
}
