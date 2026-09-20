using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ContractorHub.Migrations
{
    [DbContext(typeof(ContractorHub.Data.AppDbContext))]
    [Migration("20260920000000_AddDeals")]
    public partial class AddDeals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    ResponsibleUserId = table.Column<int>(type: "integer", nullable: true),
                    CommercialOfferId = table.Column<int>(type: "integer", nullable: true),
                    ContractId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                    table.ForeignKey("FK_Deals_Clients_ClientId", x => x.ClientId, "Clients", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_Deals_Users_ResponsibleUserId", x => x.ResponsibleUserId, "Users", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_Deals_CommercialOffers_CommercialOfferId", x => x.CommercialOfferId, "CommercialOffers", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_Deals_Contracts_ContractId", x => x.ContractId, "Contracts", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_Deals_ClientId", table: "Deals", column: "ClientId");
            migrationBuilder.CreateIndex(name: "IX_Deals_ResponsibleUserId", table: "Deals", column: "ResponsibleUserId");
            migrationBuilder.CreateIndex(name: "IX_Deals_CommercialOfferId", table: "Deals", column: "CommercialOfferId");
            migrationBuilder.CreateIndex(name: "IX_Deals_ContractId", table: "Deals", column: "ContractId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Deals");
    }
}
