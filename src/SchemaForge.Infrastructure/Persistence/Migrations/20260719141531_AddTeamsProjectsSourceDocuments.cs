using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsProjectsSourceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.CheckConstraint("ck_projects_status", "status IN ('Active', 'Archived')");
                    table.ForeignKey(
                        name: "FK_projects_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_projects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_projects_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_teams_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_source_documents_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_source_documents_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_source_documents_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_source_documents_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "team_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_memberships_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projects_created_by_user_id",
                table: "projects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_organization_id_name",
                table: "projects",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_updated_by_user_id",
                table: "projects",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_documents_created_by_user_id",
                table: "source_documents",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_documents_organization_id",
                table: "source_documents",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_documents_project_id",
                table: "source_documents",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_documents_updated_by_user_id",
                table: "source_documents",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_team_id_user_id",
                table: "team_memberships",
                columns: new[] { "team_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_memberships_user_id",
                table: "team_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_created_by_user_id",
                table: "teams",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_organization_id_name",
                table: "teams",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_updated_by_user_id",
                table: "teams",
                column: "updated_by_user_id");

            // RLS for the three new tenant-scoped tables. No self-lookup exception like
            // organization_memberships needed here - unlike login (which must discover a user's
            // org before any tenant context exists), teams/projects/source_documents are only
            // ever accessed by already-authenticated callers who already have one.
            // team_memberships doesn't need its own policy: it's a child table of teams, never
            // queried independently, so RLS on teams alone protects it transitively.
            migrationBuilder.Sql(
                """
                ALTER TABLE teams ENABLE ROW LEVEL SECURITY;
                ALTER TABLE teams FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON teams
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);

                ALTER TABLE projects ENABLE ROW LEVEL SECURITY;
                ALTER TABLE projects FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON projects
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);

                ALTER TABLE source_documents ENABLE ROW LEVEL SECURITY;
                ALTER TABLE source_documents FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON source_documents
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS tenant_isolation ON source_documents;
                ALTER TABLE source_documents DISABLE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS tenant_isolation ON projects;
                ALTER TABLE projects DISABLE ROW LEVEL SECURITY;
                DROP POLICY IF EXISTS tenant_isolation ON teams;
                ALTER TABLE teams DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.DropTable(
                name: "source_documents");

            migrationBuilder.DropTable(
                name: "team_memberships");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "teams");
        }
    }
}
