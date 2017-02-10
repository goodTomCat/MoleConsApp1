using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MoleConsApp1.Migrations
{
    public partial class PublicKeyMogration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Ip = table.Column<string>(nullable: true),
                    Login = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    PortClientToClient1 = table.Column<int>(nullable: false),
                    PortClientToClient2 = table.Column<int>(nullable: false),
                    PortClientToClient3 = table.Column<int>(nullable: false),
                    PortServerToClient = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicKeys",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CryptoAlg = table.Column<string>(nullable: true),
                    CryptoProvider = table.Column<string>(nullable: true),
                    Hash = table.Column<byte[]>(nullable: true),
                    HashAlg = table.Column<string>(nullable: true),
                    Key = table.Column<byte[]>(nullable: true),
                    Sign = table.Column<byte[]>(nullable: true),
                    UserFormId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicKeys_Users_UserFormId",
                        column: x => x.UserFormId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccesForms",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ConstUsers = table.Column<string>(nullable: true),
                    IsPublicProfile = table.Column<bool>(nullable: false),
                    MaxConstUsers = table.Column<int>(nullable: false),
                    MaxTempUsers = table.Column<int>(nullable: false),
                    TempUsers = table.Column<string>(nullable: true),
                    UserFormId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccesForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccesForms_Users_UserFormId",
                        column: x => x.UserFormId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthForms",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AuthenticationMethod = table.Column<byte>(nullable: false),
                    CryptoProvider = table.Column<string>(nullable: true),
                    Login = table.Column<string>(nullable: true),
                    UserFormId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthForms_Users_UserFormId",
                        column: x => x.UserFormId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicKeys_UserFormId",
                table: "PublicKeys",
                column: "UserFormId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccesForms_UserFormId",
                table: "AccesForms",
                column: "UserFormId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthForms_UserFormId",
                table: "AuthForms",
                column: "UserFormId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicKeys");

            migrationBuilder.DropTable(
                name: "AccesForms");

            migrationBuilder.DropTable(
                name: "AuthForms");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
