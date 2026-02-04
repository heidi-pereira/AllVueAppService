#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddMetricAboutTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE [dbo].[MetricAbout](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductShortCode] [nvarchar](20) NOT NULL,
	[UrlSafeName] [nvarchar](200) NOT NULL,
	[AboutTitle] [nvarchar](200) NOT NULL,
	[AboutContent] [nvarchar](1000) NOT NULL,
	[Editable] [bit] NULL,
	[User] [nvarchar](200) NULL,
	[SysStartTime] [datetime2](0) GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
	[SysEndTime] [datetime2](0) GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
CONSTRAINT [PK_MetricAbout] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
	PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime])
) ON [PRIMARY]
WITH
(
SYSTEM_VERSIONING = ON ( HISTORY_TABLE = [dbo].[MetricAbout_History] )
)
GO

CREATE NONCLUSTERED INDEX [IX_MetricAbout_ProductShortCode_UrlSafeName] ON [dbo].[MetricAbout]
(
	[ProductShortCode] ASC,
	[UrlSafeName] ASC
);

ALTER TABLE [dbo].[MetricAbout] ADD  CONSTRAINT [DF_MetricAbout_SysStart]  DEFAULT (sysutcdatetime()) FOR [SysStartTime]

ALTER TABLE [dbo].[MetricAbout] ADD  CONSTRAINT [DF_MetricAbout_SysEnd]  DEFAULT (CONVERT([datetime2](0),'9999-12-31 23:59:59')) FOR [SysEndTime]

ALTER TABLE [dbo].[MetricAbout] ADD  CONSTRAINT [DF_MetricAbout_Editable]  DEFAULT (1) FOR [Editable]
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
