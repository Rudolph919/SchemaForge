using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schema_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_major = table.Column<int>(type: "integer", nullable: false),
                    version_minor = table.Column<int>(type: "integer", nullable: false),
                    version_patch = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    change_summary = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    root_node = table.Column<string>(type: "jsonb", nullable: false),
                    local_definitions = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schema_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_schema_versions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schema_versions_schema_definitions_schema_definition_id",
                        column: x => x.schema_definition_id,
                        principalTable: "schema_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schema_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_schema_versions_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_schema_versions_created_by_user_id",
                table: "schema_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_schema_versions_organization_id",
                table: "schema_versions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_schema_versions_updated_by_user_id",
                table: "schema_versions",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_schema_versions_one_draft",
                table: "schema_versions",
                column: "schema_definition_id",
                unique: true,
                filter: "status = 'Draft'");

            migrationBuilder.Sql(
                """
                ALTER TABLE schema_versions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE schema_versions FORCE ROW LEVEL SECURITY;
                CREATE POLICY tenant_isolation ON schema_versions
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schema_versions");
        }
    }
}
