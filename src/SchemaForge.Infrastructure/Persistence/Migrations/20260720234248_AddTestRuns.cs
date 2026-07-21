using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_suite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    executed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    executed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    results = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_runs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_runs_schema_versions_schema_version_id",
                        column: x => x.schema_version_id,
                        principalTable: "schema_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_runs_test_suites_test_suite_id",
                        column: x => x.test_suite_id,
                        principalTable: "test_suites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_runs_users_executed_by_user_id",
                        column: x => x.executed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_runs_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_created_by_user_id",
                table: "test_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_executed_by_user_id",
                table: "test_runs",
                column: "executed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_organization_id",
                table: "test_runs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_schema_version_id",
                table: "test_runs",
                column: "schema_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_test_suite_id_executed_at",
                table: "test_runs",
                columns: new[] { "test_suite_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_test_runs_updated_by_user_id",
                table: "test_runs",
                column: "updated_by_user_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE test_runs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE test_runs FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON test_runs
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_runs");
        }
    }
}
