using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace StockManager.Infrastructure.Data.SQLite.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClosePrice = table.Column<decimal>(nullable: false),
                    CurrencyPair = table.Column<string>(nullable: false),
                    MaxPrice = table.Column<decimal>(nullable: false),
                    MinPrice = table.Column<decimal>(nullable: false),
                    Moment = table.Column<DateTime>(nullable: false),
                    OpenPrice = table.Column<decimal>(nullable: false),
                    Period = table.Column<int>(nullable: false),
                    VolumeInBaseCurrency = table.Column<decimal>(nullable: false),
                    VolumeInQuoteCurrency = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Moment",
                table: "Candles",
                column: "Moment");

            migrationBuilder.CreateIndex(
                name: "UniqueCandle",
                table: "Candles",
                columns: new[] { "CurrencyPair", "Period", "Moment" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candles");
        }
    }
}
