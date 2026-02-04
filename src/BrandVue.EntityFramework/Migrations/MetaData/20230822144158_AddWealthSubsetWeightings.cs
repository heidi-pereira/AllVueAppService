#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWealthSubsetWeightings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
INSERT INTO [SubsetConfigurations](
	[Identifier]
      ,[DisplayName]
      ,[DisplayNameShort]
      ,[Iso2LetterCountryCode]
      ,[Description]
      ,[Order]
      ,[Disabled]
      ,[SurveyIdToAllowedSegmentNames]
      ,[EnableRawDataApiAccess]
      ,[ProductShortCode]
      ,[SubProductId]
      ,[Alias]
  )
  VALUES(
  'All'
  , 'All'
  , 'All'
  , 'gb'
  , 'All'
  ,0
  ,0
  ,'{""23317"":[""Main""]}'
  ,1
  ,'wealth'
  ,NULL
  ,'All'
  )
GO

declare @productShortCode nvarchar(20),
		@regionTargetNum int,
		@genderTargetNum int,
		@ageTargetNum int,
		@segTargetNum int,
		@regionTargetCounter int,
		@genderTargetCounter int,
		@ageTargetCounter int,
		@segTargetCounter int

set @productShortCode = 'wealth'
set @regionTargetNum = 4
set @genderTargetNum = 2
set @ageTargetNum = 5
set @segTargetNum = 2


declare @regionPlanId int
insert into [WeightingPlans]([VariableIdentifier],[ParentWeightingTargetId],[IsWeightingGroupRoot],[ProductShortCode],[SubProductId],[SubsetId])
values('RegionWeighting',NULL,0,@productShortCode,NULL,'All')
set @regionPlanId = SCOPE_IDENTITY()

set @regionTargetCounter = 1

WHILE ( @regionTargetCounter <= @regionTargetNum)
BEGIN
	declare @regionTargetId int
	insert into [WeightingTargets]([EntityInstanceId],[Target],[ParentWeightingPlanId],[ProductShortCode],[SubProductId],[SubsetId])
	values(@regionTargetCounter,NULL,@regionPlanId,@productShortCode,NULL,'All')
	set @regionTargetId = SCOPE_IDENTITY()

	declare @genderPlanId int
	insert into [WeightingPlans]([VariableIdentifier],[ParentWeightingTargetId],[IsWeightingGroupRoot],[ProductShortCode],[SubProductId],[SubsetId])
	values('GenderWeighting',@regionTargetId,0,@productShortCode,NULL,'All')
	set @genderPlanId = SCOPE_IDENTITY()

	set @genderTargetCounter = 1

	WHILE ( @genderTargetCounter <= @genderTargetNum)
	BEGIN
		declare @genderTargetId int
		insert into [WeightingTargets]([EntityInstanceId],[Target],[ParentWeightingPlanId],[ProductShortCode],[SubProductId],[SubsetId])
		values(@genderTargetCounter,NULL,@genderPlanId,@productShortCode,NULL,'All')
		set @genderTargetId = SCOPE_IDENTITY()

		declare @agePlanId int
		insert into [WeightingPlans]([VariableIdentifier],[ParentWeightingTargetId],[IsWeightingGroupRoot],[ProductShortCode],[SubProductId],[SubsetId])
		values('AgeWeighting',@genderTargetId,0,@productShortCode,NULL,'All')
		set @agePlanId = SCOPE_IDENTITY()

		set @ageTargetCounter = 1

		WHILE ( @ageTargetCounter <= @ageTargetNum)
		BEGIN
			declare @ageTargetId int
			insert into [WeightingTargets]([EntityInstanceId],[Target],[ParentWeightingPlanId],[ProductShortCode],[SubProductId],[SubsetId])
			values(@ageTargetCounter,NULL,@agePlanId,@productShortCode,NULL,'All')
			set @ageTargetId = SCOPE_IDENTITY()

			declare @segPlanId int
			insert into [WeightingPlans]([VariableIdentifier],[ParentWeightingTargetId],[IsWeightingGroupRoot],[ProductShortCode],[SubProductId],[SubsetId])
			values('SegWeighting',@ageTargetId,0,@productShortCode,NULL,'All')
			set @segPlanId = SCOPE_IDENTITY()

			set @segTargetCounter = 1

			WHILE ( @segTargetCounter <= @segTargetNum)
				BEGIN
					insert into [WeightingTargets]([EntityInstanceId],[Target],[ParentWeightingPlanId],[ProductShortCode],[SubProductId],[SubsetId])
					values(@segTargetCounter,1,@segPlanId,@productShortCode,NULL,'All')

					SET @segTargetCounter = @segTargetCounter + 1
				END

			SET @ageTargetCounter = @ageTargetCounter + 1
		END

		SET @genderTargetCounter = @genderTargetCounter + 1
	END

	SET @regionTargetCounter = @regionTargetCounter + 1
END
";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
delete from SubsetConfigurations
where ProductShortCode = 'wealth'

delete from WeightingPlans
where ProductShortCode = 'wealth'

delete from WeightingTargets
where ProductShortCode = 'wealth'";

            migrationBuilder.Sql(sql);
        }
    }
}
