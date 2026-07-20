using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestSuites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_suites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_test_suites", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_suites_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_suites_schema_definitions_schema_definition_id",
                        column: x => x.schema_definition_id,
                        principalTable: "schema_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_suites_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_suites_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    input_json = table.Column<string>(type: "jsonb", nullable: false),
                    expectation = table.Column<string>(type: "jsonb", nullable: false),
                    test_suite_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_cases", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_cases_test_suites_test_suite_id",
                        column: x => x.test_suite_id,
                        principalTable: "test_suites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_test_cases_test_suite_id_name",
                table: "test_cases",
                columns: new[] { "test_suite_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_created_by_user_id",
                table: "test_suites",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_organization_id",
                table: "test_suites",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_schema_definition_id_name",
                table: "test_suites",
                columns: new[] { "schema_definition_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_suites_updated_by_user_id",
                table: "test_suites",
                column: "updated_by_user_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE test_suites ENABLE ROW LEVEL SECURITY;
                ALTER TABLE test_suites FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON test_suites
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_cases");

            migrationBuilder.DropTable(
                name: "test_suites");
        }
    }
}
