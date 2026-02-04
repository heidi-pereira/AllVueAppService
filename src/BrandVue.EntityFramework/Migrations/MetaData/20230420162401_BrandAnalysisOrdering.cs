#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BrandAnalysisOrdering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
update r set Ordering = z.rn 
from parts r join 
(select id, ROW_NUMBER() over (partition by productshortcode order by 
  CASE 
    WHEN Spec2 = 'Advocacy' THEN 1
    WHEN Spec2 = 'Buzz' THEN 2
    WHEN Spec1 = 'MORE LOVED' THEN 3
    WHEN Spec1 = 'BIGGER' THEN 4
    WHEN Spec2 = 'Love' THEN 5
    WHEN Spec2 = 'Usage' THEN 6
  END
) as rn from parts
where PaneId = 'BrandAnalysis_1'
) as z on z.id = r.id
";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
