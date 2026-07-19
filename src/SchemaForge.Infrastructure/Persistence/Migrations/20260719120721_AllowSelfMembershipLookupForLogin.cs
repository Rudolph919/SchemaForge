using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowSelfMembershipLookupForLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Login needs to discover which org a user belongs to before any tenant context
            // exists - the original policy's USING clause only matched the ambient tenant, which
            // blocks that lookup entirely (see OrganizationMembershipRepository.
            // GetFirstByUserIdAsync for the full reasoning). Adds a second, narrower USING
            // branch: a row is also visible if app.current_user_id matches its user_id. Safe
            // because that session variable is only ever set after LoginHandler has already
            // verified the password - never from caller-supplied input. WITH CHECK (governing
            // INSERT/UPDATE) is deliberately left untouched: writes must always go through a
            // real tenant-context-establishing flow, never this self-lookup exception.
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS tenant_isolation ON organization_memberships;

                CREATE POLICY tenant_isolation ON organization_memberships
                    USING (
                        organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                        OR user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid
                    )
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP POLICY IF EXISTS tenant_isolation ON organization_memberships;

                CREATE POLICY tenant_isolation ON organization_memberships
                    USING (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
                    WITH CHECK (organization_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
                """);
        }
    }
}
