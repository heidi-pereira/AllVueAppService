using Microsoft.EntityFrameworkCore.Metadata;

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddPageConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductShortCode = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    DisplayName = table.Column<string>(nullable: true),
                    MenuIcon = table.Column<string>(nullable: true),
                    PageType = table.Column<string>(nullable: true),
                    HelpText = table.Column<string>(nullable: true),
                    MinUserLevel = table.Column<int>(nullable: false),
                    StartPage = table.Column<bool>(nullable: false),
                    Layout = table.Column<string>(nullable: true),
                    PageTitle = table.Column<string>(nullable: true),
                    AverageGroup = table.Column<string>(nullable: true),
                    Subset = table.Column<string>(nullable: true),
                    ChildPages = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });
            migrationBuilder.AddTemporalTableSupport("Pages", "Pages_History");

            migrationBuilder.CreateTable(
                name: "Panes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductShortCode = table.Column<string>(nullable: true),
                    PaneId = table.Column<string>(nullable: true),
                    PageName = table.Column<string>(nullable: true),
                    Height = table.Column<int>(nullable: false),
                    PaneType = table.Column<string>(nullable: true),
                    Spec = table.Column<string>(nullable: true),
                    Spec2 = table.Column<string>(nullable: true),
                    View = table.Column<int>(nullable: false),
                    Subset = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Panes", x => x.Id);
                });
            migrationBuilder.AddTemporalTableSupport("Panes", "Panes_History");

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductShortCode = table.Column<string>(nullable: true),
                    PaneId = table.Column<string>(nullable: true),
                    PartType = table.Column<string>(nullable: true),
                    Spec1 = table.Column<string>(nullable: true),
                    Spec2 = table.Column<string>(nullable: true),
                    Spec3 = table.Column<string>(nullable: true),
                    DefaultSplitBy = table.Column<string>(nullable: true),
                    HelpText = table.Column<string>(nullable: true),
                    Disabled = table.Column<bool>(nullable: true),
                    AutoMetrics = table.Column<string>(nullable: true),
                    AutoPanes = table.Column<string>(nullable: true),
                    Ordering = table.Column<string>(nullable: true),
                    OrderingDirection = table.Column<string>(nullable: true),
                    Colours = table.Column<string>(nullable: true),
                    Filters = table.Column<string>(nullable: true),
                    XRange = table.Column<string>(nullable: true),
                    YRange = table.Column<string>(nullable: true),
                    Sections = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                });
            migrationBuilder.AddTemporalTableSupport("Parts", "Parts_History");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport("Pages", "Pages_History");
            migrationBuilder.DropTable(name: "Pages");

            migrationBuilder.RemoveTemporalTableSupport("Panes", "Panes_History");
            migrationBuilder.DropTable(name: "Panes");

            migrationBuilder.RemoveTemporalTableSupport("Parts", "Parts_History");
            migrationBuilder.DropTable(name: "Parts");
        }
    }
}
