using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingIndexesFromHardeningAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_organization_id_actor_user_id_occurred_at",
                table: "audit_log_entries",
                columns: new[] { "organization_id", "actor_user_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_log_entries_organization_id_actor_user_id_occurred_at",
                table: "audit_log_entries");
        }
    }
}
