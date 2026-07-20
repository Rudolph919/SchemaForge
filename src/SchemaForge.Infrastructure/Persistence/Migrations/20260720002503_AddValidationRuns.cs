using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "validation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    executed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    errors = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validation_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_validation_runs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validation_runs_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validation_runs_schema_versions_schema_version_id",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validation_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validation_runs_users_executed_by_user_id",
                        column: x => x.executed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_validation_runs_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_created_by_user_id",
                table: "validation_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_executed_by_user_id",
                table: "validation_runs",
                column: "executed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_organization_id",
                table: "validation_runs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_project_id",
                table: "validation_runs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_schema_version_id_executed_at",
                table: "validation_runs",
                columns: new[] { "schema_version_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_validation_runs_updated_by_user_id",
                table: "validation_runs",
                column: "updated_by_user_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE validation_runs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE validation_runs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON validation_runs
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "validation_runs");
        }
    }
}
