using System;
using Microsoft.EntityFrameworkCore.Migrations;

// ReSharper disable RedundantArgumentDefaultValue
namespace StockManager.Infrastructure.Data.SQLite.Migrations
{
    public partial class TradingData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogActions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Moment = table.Column<DateTime>(nullable: false),
                    LogActionType = table.Column<int>(nullable: false),
                    ExtendedOptionsEncoded = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderHistory",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExtId = table.Column<long>(nullable: false),
                    ClientId = table.Column<Guid>(nullable: false),
                    ParentClientId = table.Column<Guid>(nullable: false),
                    CurrencyPair = table.Column<string>(nullable: false),
                    Role = table.Column<int>(nullable: false),
                    OrderSide = table.Column<int>(nullable: false),
                    OrderType = table.Column<int>(nullable: false),
                    OrderStateType = table.Column<int>(nullable: false),
                    TimeInForce = table.Column<int>(nullable: false),
                    Quantity = table.Column<decimal>(nullable: false),
                    Price = table.Column<decimal>(nullable: false),
                    StopPrice = table.Column<decimal>(nullable: true),
                    AnalysisInfoEncoded = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Updated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExtId = table.Column<long>(nullable: false),
                    ClientId = table.Column<Guid>(nullable: false),
                    ParentClientId = table.Column<Guid>(nullable: false),
                    CurrencyPair = table.Column<string>(nullable: false),
                    Role = table.Column<int>(nullable: false),
                    OrderSide = table.Column<int>(nullable: false),
                    OrderType = table.Column<int>(nullable: false),
                    OrderStateType = table.Column<int>(nullable: false),
                    TimeInForce = table.Column<int>(nullable: false),
                    Quantity = table.Column<decimal>(nullable: false),
                    Price = table.Column<decimal>(nullable: false),
                    StopPrice = table.Column<decimal>(nullable: true),
                    AnalysisInfoEncoded = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Updated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TradingBallance",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyId = table.Column<string>(nullable: false),
                    Available = table.Column<decimal>(nullable: false),
                    Reserved = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingBallance", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogActions_Moment",
                table: "LogActions",
                column: "Moment");

            migrationBuilder.CreateIndex(
                name: "UniqueLogAction",
                table: "LogActions",
                columns: new[] { "Moment", "LogActionType" });

            migrationBuilder.CreateIndex(
                name: "ByHistoryClientId",
                table: "OrderHistory",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "HistoryCurrencyPair",
                table: "OrderHistory",
                column: "CurrencyPair");

            migrationBuilder.CreateIndex(
                name: "ByOrderClientId",
                table: "Orders",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "OrderCurrencyPair",
                table: "Orders",
                column: "CurrencyPair");

            migrationBuilder.CreateIndex(
                name: "TradingBallanceCurrency",
                table: "TradingBallance",
                column: "CurrencyId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogActions");

            migrationBuilder.DropTable(
                name: "OrderHistory");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "TradingBallance");
        }
    }
}
