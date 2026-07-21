using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchemaForge.Infrastructure.Persistence.Migrations
{
    // Hand-edited: EF's generator doesn't know "xmin" is already a Postgres system column on
    // every table, not a real one to create - the auto-generated Up/Down here tried
    // ALTER TABLE ... ADD COLUMN xmin, which Postgres rejects outright ("column name "xmin"
    // conflicts with a system column name"). This migration exists purely to update EF's model
    // snapshot/history bookkeeping so it stops proposing a "pending model change" - there's no
    // actual DDL to run, since RowVersion (Step 6 §1.5) maps to a column that has always existed.
    /// <inheritdoc />
    public partial class AddRowVersionToMutableResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
