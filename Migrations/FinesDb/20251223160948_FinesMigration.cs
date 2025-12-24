using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementBE.Migrations.FinesDb
{
    /// <inheritdoc />
    public partial class FinesMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "Account",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        userName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
            //        email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            //        password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            //        phoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            //        role = table.Column<int>(type: "int", nullable: false),
            //        isActive = table.Column<bool>(type: "bit", nullable: false),
            //        createdAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Account", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Books",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        title = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        description = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        author = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        imageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
            //        genre = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        totalCopies = table.Column<int>(type: "int", nullable: false),
            //        availableCopies = table.Column<int>(type: "int", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Books", x => x.Id);
            //    });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    fineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    paymentStatus = table.Column<bool>(type: "bit", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fines_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fines_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fines_BookId_ReturnDate",
                table: "Fines",
                columns: new[] { "BookId", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Fines_UserId_ReturnDate",
                table: "Fines",
                columns: new[] { "UserId", "ReturnDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fines");

            //migrationBuilder.DropTable(
            //    name: "Account");

            //migrationBuilder.DropTable(
            //    name: "Books");
        }
    }
}
