using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Knowledge.Server.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialKnowledgeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => new { x.WorkspaceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Memberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Memberships_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeNodes", x => x.Id);
                    table.UniqueConstraint("AK_KnowledgeNodes_WorkspaceId_Id", x => new { x.WorkspaceId, x.Id });
                    table.CheckConstraint("CK_KnowledgeNodes_ParentIsNotSelf", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\"");
                    table.ForeignKey(
                        name: "FK_KnowledgeNodes_KnowledgeNodes_WorkspaceId_ParentId",
                        columns: x => new { x.WorkspaceId, x.ParentId },
                        principalTable: "KnowledgeNodes",
                        principalColumns: new[] { "WorkspaceId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNodes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeNodes_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRevisions", x => x.Id);
                    table.UniqueConstraint("AK_KnowledgeRevisions_WorkspaceId_NodeId_Id", x => new { x.WorkspaceId, x.NodeId, x.Id });
                    table.CheckConstraint("CK_KnowledgeRevisions_VersionPositive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisions_KnowledgeNodes_WorkspaceId_NodeId",
                        columns: x => new { x.WorkspaceId, x.NodeId },
                        principalTable: "KnowledgeNodes",
                        principalColumns: new[] { "WorkspaceId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeRevisions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNodes_CreatedBy",
                table: "KnowledgeNodes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNodes_WorkspaceId_Id_CurrentRevisionId",
                table: "KnowledgeNodes",
                columns: new[] { "WorkspaceId", "Id", "CurrentRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNodes_WorkspaceId_ParentId",
                table: "KnowledgeNodes",
                columns: new[] { "WorkspaceId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeNodes_WorkspaceId_Type_Status",
                table: "KnowledgeNodes",
                columns: new[] { "WorkspaceId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisions_CreatedBy",
                table: "KnowledgeRevisions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRevisions_NodeId_Version",
                table: "KnowledgeRevisions",
                columns: new[] { "NodeId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_CreatedBy",
                table: "Workspaces",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeNodes_KnowledgeRevisions_WorkspaceId_Id_CurrentRev~",
                table: "KnowledgeNodes",
                columns: new[] { "WorkspaceId", "Id", "CurrentRevisionId" },
                principalTable: "KnowledgeRevisions",
                principalColumns: new[] { "WorkspaceId", "NodeId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeNodes_KnowledgeRevisions_WorkspaceId_Id_CurrentRev~",
                table: "KnowledgeNodes");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "KnowledgeRevisions");

            migrationBuilder.DropTable(
                name: "KnowledgeNodes");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
