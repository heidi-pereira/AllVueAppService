
Print 'Starting migration script'
SET NOCOUNT Off;

DECLARE @pageName NVARCHAR(450)
SET @pageName = 'Brand Analysis'

DECLARE @partName NVARCHAR(450)
SET @partName = 'BrandAnalysis_1'

DECLARE @BrandAnalysisPageIndex int

Print 'Delete existing Pages, Panes & Parts'
-- Deletions

DELETE [dbo].[Panes] WHERE PageName = @pageName
DELETE [dbo].[Parts] WHERE PaneId = @partName
DELETE [dbo].[Pages] WHERE [Name] = @pageName

DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandAdvocacy%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandBuzz%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandLove%'
DELETE [dbo].[Parts] WHERE PaneId LIKE 'BrandUsage%'

DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandAdvocacy%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandBuzz%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandLove%'
DELETE [dbo].[Panes] WHERE PaneId LIKE 'BrandUsage%'

DELETE [dbo].[Pages] WHERE [Name] IN ('Brand Analysis Advocacy', 'Brand Analysis Buzz', 'Brand Analysis Love', 'Brand Analysis Usage')

DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'retail'      and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'drinks'      and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'finance'     and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'charities'   and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Region (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'brandvue'    and Name = N'Affinity (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Segment (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Age (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Gender (Analysis)';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Affinity (Analysis)';

DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) FR';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) ES';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) DE';
DELETE FROM [dbo].[MetricConfigurations] where ProductShortCode = N'eatingout'   and Name = N'Region (Grouped) US';

DELETE MetricConfigurations WHERE ProductShortCode = 'brandvue' AND Name IN ('Promoters')
DELETE MetricConfigurations WHERE ProductShortCode = 'finance' AND Name = 'Promotors Advocacy'
-------------------------------------------------------
Print 'Creating metrics'

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'retail', N'Affinity (Analysis)', null, N'Brand_affinity', null, null, N'yn', N'1|2|3|4|5|6|7', null, N'Consumer_segment', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 0, null, null, null, null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);


INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'retail', N'Segment (Analysis)', null, N'Consumer_segment_entity', null, null, N'yn', N'1>6', null, N'Consumer_segment_base', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 1, null, null, N'1,2:None|3:Lapsed|4:L12M|5,6:L3M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 0, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'retail', N'Region (Analysis)', null, N'Region', null, null, N'yn', N'0>12', null, N'Region', N'0>12', null, null, null, N'About you: Where do you live?', N'0%', null, null, null, null, N'1-2:NI & Scotland|3-5:North|6-8:Midlands|9-11:South|12:London|0:Other', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Region (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'retail', N'Gender (Analysis)', null, N'Gender', null, null, N'yn', N'0|1', null, null, N'0|1', null, null, null, null, N'0', 0, 0, null, null, N'0:Female|1:Male', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Gender (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'retail', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 0, null, null, N'16-34:16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'drinks', N'Segment (Analysis)', null, N'Consumer_segment_entity', null, null, N'yn', N'1>7', null, N'Consumer_segment_base', N'1|2|3|4|5|6|7', null, null, null, null, N'0', 0, 1, null, null, N'1,2:None|3:Lapsed|4:L12M|5,6,7:L3M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 0, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'drinks', N'Region (Analysis)', null, N'Region', null, null, N'yn', N'0>12', null, N'Region', N'0>12', null, null, null, null, N'0%', null, null, null, null, N'1-2:NI & Scotland|3-5:North|6-8:Midlands|9-11:South|12:London|0:Other', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Region (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'drinks', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 0, null, null, N'16-34:16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'drinks', N'Gender (Analysis)', null, N'Gender', null, null, N'yn', N'0|1', null, null, N'0|1', null, null, null, null, N'0', 0, 0, null, null, N'0:Female|1:Male', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Gender (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'drinks', N'Affinity (Analysis)', null, N'Brand_affinity', null, null, N'yn', N'1|2|3|4|5|6|7', null, N'Consumer_segment', N'1|2|3|4|5|6|7', null, null, null, null, N'0', 0, 0, null, null, null, null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'finance', N'Gender (Analysis)', null, N'Gender', null, null, N'yn', N'0|1', null, null, N'0|1', null, null, null, null, N'0', 0, 0, null, null, N'0:Female|1:Male', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Gender (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'finance', N'Affinity (Analysis)', null, N'Brand_affinity', null, null, N'yn', N'1|2|3|4|5|6|7', null, N'Consumer_segment', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 0, null, null, null, null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'finance', N'Segment (Analysis)', null, N'Consumer_segment_entity', null, null, N'yn', N'1>6', null, N'Consumer_segment_base', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 1, null, null, N'1,2:None|3:Lapsed|4:L12M|5,6:L3M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 0, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'finance', N'Region (Analysis)', null, N'Region', null, null, N'yn', N'0>12', null, N'Region', N'0>12', null, null, null, N'About you: Where do you live?', N'0%', null, null, null, null, N'1-2:NI & Scotland|3-5:North|6-8:Midlands|9-11:South|12:London|0:Other', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Region (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'finance', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 1, null, null, N'16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'charities', N'Segment (Analysis)', null, N'Consumer_segment_entity', null, null, N'yn', N'1>6', null, N'Consumer_segment_base', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 1, null, null, N'1,2:None|3:Lapsed|4:L12M|5,6:L3M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 0, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'charities', N'Affinity (Analysis)', null, N'Brand_affinity', null, null, N'yn', N'1|2|3|4|5|6|7', null, N'Consumer_segment', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 0, null, null, null, null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'charities', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 0, null, null, N'16-34:16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId)
VALUES (N'charities', N'Gender (Analysis)', null, N'Gender', null, null, N'yn', N'0|1', null, null, N'0|1', null, null, null, null, N'0', 0, 0, null, null, N'0:Female|1:Male', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Gender (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'brandvue', N'Segment (Analysis)', null, N'Consumer_segment', null, null, N'yn', N'1>4', null, N'Consumer_segment_asked', N'1>4', null, null, N'When, if ever, have you used (or brought) products or services from the following?', N'When, if ever, have you used (or brought) products or services from the following?', N'0%', null, null, null, null, N'2:None|3:Lapsed|4:L12M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'brandvue', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 0, null, null, N'16-34:16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'brandvue', N'Region (Analysis)', null, N'Regions', null, null, N'yn', N'0>12', null, N'Regions', N'0>12', null, null, null, N'About you: Where do you live?', N'0%', null, null, null, null, N'1-2:NI & Scotland|3-5:North|6-8:Midlands|9-11:South|12:London|0:Other', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Region (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'brandvue', N'Affinity (Analysis)', N'len(response.Brand_affinity(brand = result.brand, Affinity = [1,2,3,4,5,6,7]))', N'Brand_affinity', null, null, N'yn', N'1>100', N'len(response.Consumer_segment(brand = result.brand, ShopperSegment = [1,2,3,4])) and len(response.Brand_affinity(brand = result.brand, Affinity = [1,2,3,4,5,6,7,100]))', N'Brand_affinity_asked', N'1>100', null, null, null, N'Company opinion: How would you describe your opinion of the following?', N'0%', null, null, null, null, N'1:Love|2:Like a lot|3:Like a little|4:Indifferent|5:Dislike a little|100:Do not know much about them|6:Dislike a lot|7:Hate', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Segment (Analysis)', null, N'Consumer_segment_entity', null, null, N'yn', N'1>6', null, N'Consumer_segment_base', N'1|2|3|4|5|6', null, null, null, null, N'0', 0, 1, null, null, N'1,2:None|3:Lapsed|4:L12M|5,6:L3M', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 0, 0, N'Segment (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Age (Analysis)', null, N'Age', null, null, N'yn', N'16>74', null, null, N'16>74', null, null, null, null, N'0', 0, 0, null, null, N'16-34:16-34|35-54:35-54|55-74:55+', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Age (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Gender (Analysis)', null, N'Gender', null, null, N'yn', N'0|1', null, null, N'0|1', null, null, null, null, N'0', 0, 0, null, null, N'0:Female|1:Male', null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Gender (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Affinity (Analysis)', null, N'Brand_affinity', null, null, N'yn', N'1|2|3|4|5|6|7', null, null, N'1|2|3|4|5|6|7|100', null, null, null, null, N'0', 0, 0, null, null, null, null, 0, null, null, null, 0, 1, null, 0, null, null, 0, null, null, null, null, 1, 0, N'Affinity (Analysis)', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Region (Grouped) ES', null, N'Region', null, null, N'yn', N'1', null, null, N'201>219', null, null, null, null, N'0', 0, 1, null, null, N'202,204,211,214,216,217,218:North|210:Madrid|205,206,213:Centre|207,212,215:East|201,203,208,209,219:South', null, 0, null, null, N'ES', 1, 0, null, 0, null, null, 0, null, null, null, null, 0, 1, N'Region (Grouped) ES', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Region (Grouped) DE', null, N'Region', null, null, N'yn', N'1', null, null, N'301>316', null, null, null, null, N'0', 0, 1, null, null, N'305,306,309,310,315:North West|301,307,311,312:South West|302:Bavaria|303,304,308,313,314,316:North East', null, 0, null, null, N'DE', 1, 0, null, 0, null, null, 0, null, null, null, null, 0, 1, N'Region (Grouped) DE', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Region (Grouped) US', null, N'Region', null, null, N'yn', N'1', null, null, N'1>51', null, null, null, null, N'0', 0, 1, null, null, N'2,3,5,6,12,13,27,29,32,38,45,48,51:West|1,4,8,9,10,11,18,19,21,25,34,37,41,43,44,47,49:South|14,15,16,17,23,24,26,28,35,36,42,50:Midwest|7,20,22,30,31,33,39,40,46:Northeast', null, 0, null, null, N'US', 1, 0, null, 0, null, null, 0, null, null, null, null, 0, 1, N'Region (Grouped) US', null, null, null);

INSERT INTO dbo.MetricConfigurations (ProductShortCode, Name, FieldExpression, Field, Field2, FieldOp, CalcType, TrueVals, BaseExpression, BaseField, BaseVals, MarketAverageBaseMeasure, KeyImage, Measure, HelpText, NumFormat, Min, Max, ExcludeWaves, StartDate, FilterValueMapping, FilterGroup, FilterMulti, PreNormalisationMinimum, PreNormalisationMaximum, Subset, DisableMeasure, DisableFilter, ExcludeList, EligibleForMetricComparison, Category, SubCategory, DownIsGood, ScaleFactor, SubProductId, VariableConfigurationId, DefaultSplitByEntityType, IsAutoGenerated, EligibleForCrosstabOrAllVue, VarCode, OriginalMetricName, BaseExpression_OLD, BaseVariableConfigurationId) 
VALUES (N'eatingout', N'Region (Grouped) FR', null, N'Region', null, null, N'yn', N'1', null, null, N'101>120', null, null, null, null, N'0', 0, 1, null, null, N'110:Île De France|101,105,107,108,109,113,115,117:North-East|104,106,116,118:North-West|102,112,114:South West|103,111,119,120:South East', null, 0, null, null, N'FR', 1, 0, null, 0, null, null, 0, null, null, null, null, 0, 1, N'Region (Grouped) FR', null, null, null);

-------------------------------------------------------
Print 'brandvue: Creating metrics'

insert into MetricConfigurations
values ('brandvue', 'Promoters', null, 'Recommendation_all', null, null, 'yn', '9|10', null, null, '0>10', null, null, null, null, '0',0 ,0, null, null, null, null, 0, null, null, null, 1, 1, null, 1, null, null, 0, null, null, null, null, 1, 1, 'Promoters', null, null, null)

-------------------------------------------------------
Print 'finance: Creating metrics'

insert into MetricConfigurations
values ('finance', 'Promotors Advocacy', 'sum(response.Recommendation_All(brand=result.brand,product=[1,2,4,8,52,21,59,17,20,12,13,15,16,14,19,18,111,121,72,5,131,9,92,141,150]))/len(response.Recommendation_All(brand=result.brand,product=[1,2,4,8,52,21,59,17,20,12,13,15,16,14,19,18,111,121,72,5,131,9,92,141,150])) > 8', null, null, null, 'yn', '9|10', 'len(response.Recommendation_All(brand=result.brand,product=[1,2,4,8,52,21,59,17,20,12,13,15,16,14,19,18,111,121,72,5,131,9,92,141,150]))', null, '0>10', null, null, null, null, '0',0 ,0, null, null, null, null, 0, null, null, null, 1, 1, null, 1, null, null, 0, null, null, null, null, 1, 1, 'Promotors Advocacy', null, null, null)


Print 'Running migration script for "eatingout"...'
Print 'eatingout: Ordering existing pages'
-- Page order
UPDATE [Pages] 
SET PageDisplayIndex = 50 WHERE id = 370 -- Audience (UK only)
UPDATE [Pages] SET PageDisplayIndex = 100 WHERE id = 377 -- Market Summary
UPDATE [Pages] SET PageDisplayIndex = 200 WHERE id = 380 -- Topline Summary
UPDATE [Pages] SET PageDisplayIndex = 300 WHERE id = 381 -- Brand Attention
UPDATE [Pages] SET PageDisplayIndex = 400 WHERE id = 390 -- Brand Health
UPDATE [Pages] SET PageDisplayIndex = 500 WHERE id = 398 -- Demand & Usage
UPDATE [Pages] SET PageDisplayIndex = 600 WHERE id = 410 -- Image & Association
UPDATE [Pages] SET PageDisplayIndex = 700 WHERE id = 419 -- Customer Experience
UPDATE [Pages] SET PageDisplayIndex = 800 WHERE id = 432 -- Choosing Where to Go
UPDATE [Pages] SET PageDisplayIndex = 900 WHERE id = 440 -- Experience Profiling
UPDATE [Pages] SET PageDisplayIndex = 950 WHERE id = 448 -- Delivery And Takeaway (UK only)
UPDATE [Pages] SET PageDisplayIndex = 1000 WHERE id = 449 -- Reporting
UPDATE [Pages] SET PageDisplayIndex = 1100 WHERE id = 450 -- Compare metrics
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 451 -- About - US
UPDATE [Pages] SET PageDisplayIndex = 1200 WHERE id = 1213 -- Documents  (UK only)
UPDATE [Pages] SET PageDisplayIndex = 1300 WHERE id = 1347 -- Crosstabbing  (UK only)
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 1457 -- About - UK
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 1462 -- About - DE
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 1468 -- About - ES
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 1473 -- About - FR

Print 'eatingout: Creating pages'
-- Top page
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('eatingout', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 10)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'eatingout' and Name = @pageName)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('eatingout', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'AnalysisScorecard','Promoters','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'AnalysisScorecard','Positive buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'AnalysisScorecard','Penetration (L12M)','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout',@partName,'AnalysisScorecard','Brand Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('eatingout','Brand Analysis Advocacy','Brand Advocacy', null, 'SubPage', null, 100, 0, null, 'Brand Advocacy', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('eatingout','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('eatingout','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('eatingout','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset) 
VALUES ('eatingout', 'BrandAdvocacyUK', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6, 'UK')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View], Subset)
VALUES ('eatingout', 'BrandBuzzUK', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6, 'UK')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandLoveUK', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6, 'UK')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandUsageUK', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6, 'UK')


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset) 
VALUES ('eatingout', 'BrandAdvocacyDE', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6, 'DE')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View], Subset)
VALUES ('eatingout', 'BrandBuzzDE', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6, 'DE')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandLoveDE', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6, 'DE')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandUsageDE', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6, 'DE')


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset) 
VALUES ('eatingout', 'BrandAdvocacyFR', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6, 'FR')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View], Subset)
VALUES ('eatingout', 'BrandBuzzFR', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6, 'FR')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandLoveFR', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6, 'FR')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandUsageFR', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6, 'FR')


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset) 
VALUES ('eatingout', 'BrandAdvocacyUS', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6, 'US')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View], Subset)
VALUES ('eatingout', 'BrandBuzzUS', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6, 'US')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandLoveUS', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6, 'US')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandUsageUS', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6, 'US')


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset) 
VALUES ('eatingout', 'BrandAdvocacyES', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6, 'ES')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View], Subset)
VALUES ('eatingout', 'BrandBuzzES', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6, 'ES')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandLoveES', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6, 'ES')

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View], Subset)
VALUES ('eatingout', 'BrandUsageES', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6, 'ES')


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUK',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisScorecard',		'Positive buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisBasedOn',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisScore',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisPotentialScore',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisScoreOverTime',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUK',		'BrandAnalysisWhereNext',		'Positive buzz',	'Buzz',	'{"metrics":[{"key":"Positive buzz","metricName":"Positive buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Net buzz","metricName":"Net buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUK',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics":[{"key":"Image:Affordable","metricName":"Image:Affordable","requestType":"scorecardPerformance"},{"key":"Image:Authentic","metricName":"Image:Authentic","requestType":"scorecardPerformance"},{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Delicious","metricName":"Image:Delicious","requestType":"scorecardPerformance"},{"key":"Image:Different","metricName":"Image:Different","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Everyday","metricName":"Image:Everyday","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Family","metricName":"Image:Family","requestType":"scorecardPerformance"},{"key":"Image:Fashionable","metricName":"Image:Fashionable","requestType":"scorecardPerformance"},{"key":"Image:Fresh","metricName":"Image:Fresh","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Fun","metricName":"Image:Fun","requestType":"scorecardPerformance"},{"key":"Image:Generous","metricName":"Image:Generous","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Has character","metricName":"Image:Has character","requestType":"scorecardPerformance"},{"key":"Image:Healthy","metricName":"Image:Healthy","requestType":"scorecardPerformance"},{"key":"Image:Homely","metricName":"Image:Homely","requestType":"scorecardPerformance"},{"key":"Image:Lively","metricName":"Image:Lively","requestType":"scorecardPerformance"},{"key":"Image:Local","metricName":"Image:Local","requestType":"scorecardPerformance"},{"key":"Image:New","metricName":"Image:New","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quick","metricName":"Image:Quick","requestType":"scorecardPerformance"},{"key":"Image:Quirky","metricName":"Image:Quirky","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trustworthy","metricName":"Image:Trustworthy","requestType":"scorecardPerformance"},{"key":"Image:Guilty pleasure","metricName":"Image:Guilty pleasure","requestType":"scorecardPerformance"},{"key":"Image:Socially responsible","metricName":"Image:Socially responsible","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUK',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyDE',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisScorecard',		'Positive buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisBasedOn',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisScore',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisPotentialScore',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisScoreOverTime',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzDE',		'BrandAnalysisWhereNext',		'Positive buzz',	'Buzz',	'{"metrics":[{"key":"Positive buzz","metricName":"Positive buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Net buzz","metricName":"Net buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveDE',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics":[{"key":"Image:Affordable","metricName":"Image:Affordable","requestType":"scorecardPerformance"},{"key":"Image:Authentic","metricName":"Image:Authentic","requestType":"scorecardPerformance"},{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Delicious","metricName":"Image:Delicious","requestType":"scorecardPerformance"},{"key":"Image:Different","metricName":"Image:Different","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Everyday","metricName":"Image:Everyday","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Family","metricName":"Image:Family","requestType":"scorecardPerformance"},{"key":"Image:Fashionable","metricName":"Image:Fashionable","requestType":"scorecardPerformance"},{"key":"Image:Fresh","metricName":"Image:Fresh","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Fun","metricName":"Image:Fun","requestType":"scorecardPerformance"},{"key":"Image:Generous","metricName":"Image:Generous","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Has character","metricName":"Image:Has character","requestType":"scorecardPerformance"},{"key":"Image:Healthy","metricName":"Image:Healthy","requestType":"scorecardPerformance"},{"key":"Image:Homely","metricName":"Image:Homely","requestType":"scorecardPerformance"},{"key":"Image:Lively","metricName":"Image:Lively","requestType":"scorecardPerformance"},{"key":"Image:Local","metricName":"Image:Local","requestType":"scorecardPerformance"},{"key":"Image:New","metricName":"Image:New","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quick","metricName":"Image:Quick","requestType":"scorecardPerformance"},{"key":"Image:Quirky","metricName":"Image:Quirky","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trustworthy","metricName":"Image:Trustworthy","requestType":"scorecardPerformance"},{"key":"Image:Guilty pleasure","metricName":"Image:Guilty pleasure","requestType":"scorecardPerformance"},{"key":"Image:Socially responsible","metricName":"Image:Socially responsible","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped) DE","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageDE',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyES',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisScorecard',		'Positive buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisBasedOn',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisScore',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisPotentialScore',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisScoreOverTime',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzES',		'BrandAnalysisWhereNext',		'Positive buzz',	'Buzz',	'{"metrics":[{"key":"Positive buzz","metricName":"Positive buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Net buzz","metricName":"Net buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveES',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics":[{"key":"Image:Affordable","metricName":"Image:Affordable","requestType":"scorecardPerformance"},{"key":"Image:Authentic","metricName":"Image:Authentic","requestType":"scorecardPerformance"},{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Delicious","metricName":"Image:Delicious","requestType":"scorecardPerformance"},{"key":"Image:Different","metricName":"Image:Different","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Everyday","metricName":"Image:Everyday","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Family","metricName":"Image:Family","requestType":"scorecardPerformance"},{"key":"Image:Fashionable","metricName":"Image:Fashionable","requestType":"scorecardPerformance"},{"key":"Image:Fresh","metricName":"Image:Fresh","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Fun","metricName":"Image:Fun","requestType":"scorecardPerformance"},{"key":"Image:Generous","metricName":"Image:Generous","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Has character","metricName":"Image:Has character","requestType":"scorecardPerformance"},{"key":"Image:Healthy","metricName":"Image:Healthy","requestType":"scorecardPerformance"},{"key":"Image:Homely","metricName":"Image:Homely","requestType":"scorecardPerformance"},{"key":"Image:Lively","metricName":"Image:Lively","requestType":"scorecardPerformance"},{"key":"Image:Local","metricName":"Image:Local","requestType":"scorecardPerformance"},{"key":"Image:New","metricName":"Image:New","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quick","metricName":"Image:Quick","requestType":"scorecardPerformance"},{"key":"Image:Quirky","metricName":"Image:Quirky","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trustworthy","metricName":"Image:Trustworthy","requestType":"scorecardPerformance"},{"key":"Image:Guilty pleasure","metricName":"Image:Guilty pleasure","requestType":"scorecardPerformance"},{"key":"Image:Socially responsible","metricName":"Image:Socially responsible","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped) ES","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageES',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyUS',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisScorecard',		'Positive buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisBasedOn',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisScore',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisPotentialScore',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisScoreOverTime',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzUS',		'BrandAnalysisWhereNext',		'Positive buzz',	'Buzz',	'{"metrics":[{"key":"Positive buzz","metricName":"Positive buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Net buzz","metricName":"Net buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveUS',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics":[{"key":"Image:Affordable","metricName":"Image:Affordable","requestType":"scorecardPerformance"},{"key":"Image:Authentic","metricName":"Image:Authentic","requestType":"scorecardPerformance"},{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Delicious","metricName":"Image:Delicious","requestType":"scorecardPerformance"},{"key":"Image:Different","metricName":"Image:Different","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Everyday","metricName":"Image:Everyday","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Family","metricName":"Image:Family","requestType":"scorecardPerformance"},{"key":"Image:Fashionable","metricName":"Image:Fashionable","requestType":"scorecardPerformance"},{"key":"Image:Fresh","metricName":"Image:Fresh","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Fun","metricName":"Image:Fun","requestType":"scorecardPerformance"},{"key":"Image:Generous","metricName":"Image:Generous","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Has character","metricName":"Image:Has character","requestType":"scorecardPerformance"},{"key":"Image:Healthy","metricName":"Image:Healthy","requestType":"scorecardPerformance"},{"key":"Image:Homely","metricName":"Image:Homely","requestType":"scorecardPerformance"},{"key":"Image:Lively","metricName":"Image:Lively","requestType":"scorecardPerformance"},{"key":"Image:Local","metricName":"Image:Local","requestType":"scorecardPerformance"},{"key":"Image:New","metricName":"Image:New","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quick","metricName":"Image:Quick","requestType":"scorecardPerformance"},{"key":"Image:Quirky","metricName":"Image:Quirky","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trustworthy","metricName":"Image:Trustworthy","requestType":"scorecardPerformance"},{"key":"Image:Guilty pleasure","metricName":"Image:Guilty pleasure","requestType":"scorecardPerformance"},{"key":"Image:Socially responsible","metricName":"Image:Socially responsible","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped) US","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageUS',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandAdvocacyFR',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisScorecard',		'Positive buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisBasedOn',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisScore',			'Positive buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisPotentialScore',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisScoreOverTime',	'Positive buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandBuzzFR',		'BrandAnalysisWhereNext',		'Positive buzz',	'Buzz',	'{"metrics":[{"key":"Positive buzz","metricName":"Positive buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Net buzz","metricName":"Net buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandLoveFR',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics":[{"key":"Image:Affordable","metricName":"Image:Affordable","requestType":"scorecardPerformance"},{"key":"Image:Authentic","metricName":"Image:Authentic","requestType":"scorecardPerformance"},{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Delicious","metricName":"Image:Delicious","requestType":"scorecardPerformance"},{"key":"Image:Different","metricName":"Image:Different","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Everyday","metricName":"Image:Everyday","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Family","metricName":"Image:Family","requestType":"scorecardPerformance"},{"key":"Image:Fashionable","metricName":"Image:Fashionable","requestType":"scorecardPerformance"},{"key":"Image:Fresh","metricName":"Image:Fresh","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Fun","metricName":"Image:Fun","requestType":"scorecardPerformance"},{"key":"Image:Generous","metricName":"Image:Generous","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Has character","metricName":"Image:Has character","requestType":"scorecardPerformance"},{"key":"Image:Healthy","metricName":"Image:Healthy","requestType":"scorecardPerformance"},{"key":"Image:Homely","metricName":"Image:Homely","requestType":"scorecardPerformance"},{"key":"Image:Lively","metricName":"Image:Lively","requestType":"scorecardPerformance"},{"key":"Image:Local","metricName":"Image:Local","requestType":"scorecardPerformance"},{"key":"Image:New","metricName":"Image:New","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quick","metricName":"Image:Quick","requestType":"scorecardPerformance"},{"key":"Image:Quirky","metricName":"Image:Quirky","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trustworthy","metricName":"Image:Trustworthy","requestType":"scorecardPerformance"},{"key":"Image:Guilty pleasure","metricName":"Image:Guilty pleasure","requestType":"scorecardPerformance"},{"key":"Image:Socially responsible","metricName":"Image:Socially responsible","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('eatingout', 'BrandUsageFR',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')

Print 'Running migration script for "eatingout"... DONE'

-------------------------------

Print 'Running migration script for "brandvue"...'


Print 'brandvue: Creating pages'
-- Top page

INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('brandvue', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 10)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'brandvue' and Name = @pageName)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('brandvue', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'AnalysisScorecard','Promoters','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'AnalysisScorecard','Positive Buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'AnalysisScorecard','Penetration (L12M)','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue',@partName,'AnalysisScorecard','Brand Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('brandvue','Brand Analysis Advocacy','Brand Advocacy', null, 'SubPage', null, 100, 0, null, 'Brand Advocacy', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('brandvue','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('brandvue','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('brandvue','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View]) 
VALUES ('brandvue', 'BrandAdvocacy', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View])
VALUES ('brandvue', 'BrandBuzz', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('brandvue', 'BrandLove', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('brandvue', 'BrandUsage', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandAdvocacy',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisScorecard',		'Positive Buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisBasedOn',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisScore',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisPotentialScore',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisScoreOverTime',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandBuzz',		'BrandAnalysisWhereNext',		'Positive Buzz',	'Buzz',	'{"metrics":[{"key":"Positive Buzz","metricName":"Positive Buzz","requestType":"scorecardPerformance"},{"key":"Negative buzz","metricName":"Negative buzz","requestType":"scorecardPerformance"},{"key":"Advertising awareness","metricName":"Advertising awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandLove',		'BrandAnalysisWhereNext',		'Brand Love',	'Love',	'{"metrics": []}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('brandvue', 'BrandUsage',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"}]}')

Print 'Running migration script for "brandvue"... DONE'

----------------------

Print 'Running migration script for "charities"...'
Print 'charities: Ordering existing pages'
-- Page order
UPDATE [Pages] SET PageDisplayIndex = 100 WHERE id = 220 -- Audience
UPDATE [Pages] SET PageDisplayIndex = 200 WHERE id = 227 -- Topline Summary
UPDATE [Pages] SET PageDisplayIndex = 300 WHERE id = 228 -- Brand Attention
UPDATE [Pages] SET PageDisplayIndex = 400 WHERE id = 232 -- Brand Health
UPDATE [Pages] SET PageDisplayIndex = 500 WHERE id = 244 -- Support & Engagement
UPDATE [Pages] SET PageDisplayIndex = 600 WHERE id = 251 -- Image & Association
UPDATE [Pages] SET PageDisplayIndex = 700 WHERE id = 255 -- Fundraising & Donations
UPDATE [Pages] SET PageDisplayIndex = 800 WHERE id = 256 -- Supporter Experience
UPDATE [Pages] SET PageDisplayIndex = 900 WHERE id = 269 -- Reporting
UPDATE [Pages] SET PageDisplayIndex = 1000 WHERE id = 270 -- Compare metrics
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 271 -- About


Print 'charities: Creating pages'
-- Top page

INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('charities', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 10)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'charities' and Name = @pageName)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('charities', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'AnalysisScorecard','Promoters','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'AnalysisScorecard','Positive Buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'AnalysisScorecard','Support Status:Support','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities',@partName,'AnalysisScorecard','Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('charities','Brand Analysis Advocacy','Brand Advocacy', null, 'SubPage', null, 100, 0, null, 'Brand Advocacy', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('charities','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('charities','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('charities','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View]) 
VALUES ('charities', 'BrandAdvocacy', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View])
VALUES ('charities', 'BrandBuzz', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('charities', 'BrandLove', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('charities', 'BrandUsage', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6)


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandAdvocacy',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisScorecard',		'Positive Buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisBasedOn',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisScore',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisPotentialScore',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisScoreOverTime',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandBuzz',		'BrandAnalysisWhereNext',		'Positive Buzz',	'Buzz',	'{"metrics":[{"key":"Positive Buzz","metricName":"Positive Buzz","requestType":"scorecardPerformance"},{"key":"Negative Buzz","metricName":"Negative Buzz","requestType":"scorecardPerformance"},{"key":"Net Buzz","metricName":"Net Buzz","requestType":"scorecardPerformance"},{"key":"Advertising Awareness","metricName":"Advertising Awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities', 'BrandLove',		'BrandAnalysisScorecard',		'Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandLove',		'BrandAnalysisBasedOn',			'Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandLove',		'BrandAnalysisScore',			'Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandLove',		'BrandAnalysisPotentialScore',	'Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandLove',		'BrandAnalysisScoreOverTime',	'Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandLove',		'BrandAnalysisWhereNext',		'Love',	'Love',	'{"metrics":[{"key":"Image:Authoritative","metricName":"Image:Authoritative","requestType":"scorecardPerformance"},{"key":"Image:Bold","metricName":"Image:Bold","requestType":"scorecardPerformance"},{"key":"Image:Caring","metricName":"Image:Caring","requestType":"scorecardPerformance"},{"key":"Image:Committed","metricName":"Image:Committed","requestType":"scorecardPerformance"},{"key":"Image:Confident","metricName":"Image:Confident","requestType":"scorecardPerformance"},{"key":"Image:Credible","metricName":"Image:Credible","requestType":"scorecardPerformance"},{"key":"Image:Empathetic","metricName":"Image:Empathetic","requestType":"scorecardPerformance"},{"key":"Image:Established","metricName":"Image:Established","requestType":"scorecardPerformance"},{"key":"Image:Expert","metricName":"Image:Expert","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Honest","metricName":"Image:Honest","requestType":"scorecardPerformance"},{"key":"Image:Inspiring","metricName":"Image:Inspiring","requestType":"scorecardPerformance"},{"key":"Image:Modern","metricName":"Image:Modern","requestType":"scorecardPerformance"},{"key":"Image:Optimistic","metricName":"Image:Optimistic","requestType":"scorecardPerformance"},{"key":"Image:Passionate","metricName":"Image:Passionate","requestType":"scorecardPerformance"},{"key":"Image:Plain speaking","metricName":"Image:Plain speaking","requestType":"scorecardPerformance"},{"key":"Image:Trusted","metricName":"Image:Trusted","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisScorecard',		'Support Status:Support',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisBasedOn',			'Support Status:Support',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisScore',			'Support Status:Support',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Grouped)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisPotentialScore',	'Support Status:Support',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisScoreOverTime',	'Support Status:Support',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('charities', 'BrandUsage',		'BrandAnalysisWhereNext',		'Support Status:Support',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"}]}')

Print 'Running migration script for "charities"... DONE'

----------------------

Print 'Running migration script for "finance"...'
Print 'finance: Ordering existing pages'
-- Page order
UPDATE [Pages] SET PageDisplayIndex = 100 WHERE id = 97 -- Audience
UPDATE [Pages] SET PageDisplayIndex = 200 WHERE id = 104 -- Topline Summary
UPDATE [Pages] SET PageDisplayIndex = 300 WHERE id = 105 -- Brand Attention
UPDATE [Pages] SET PageDisplayIndex = 400 WHERE id = 111 -- Brand Health
UPDATE [Pages] SET PageDisplayIndex = 500 WHERE id = 126 -- Demand & Usage
UPDATE [Pages] SET PageDisplayIndex = 600 WHERE id = 134 -- Image & Association
UPDATE [Pages] SET PageDisplayIndex = 700 WHERE id = 140 -- Product Usage
UPDATE [Pages] SET PageDisplayIndex = 800 WHERE id = 141 -- Customer Experience
UPDATE [Pages] SET PageDisplayIndex = 900 WHERE id = 147 -- Reporting
UPDATE [Pages] SET PageDisplayIndex = 1000 WHERE id = 148 -- Compare metrics
UPDATE [Pages] SET PageDisplayIndex = 2000 WHERE id = 150 -- About

Print 'finance: Creating pages'
-- Top page

INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('finance', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 10)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'finance' and Name = @pageName)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('finance', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'AnalysisScorecard','Promotors Advocacy','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'AnalysisScorecard','Positive Buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'AnalysisScorecard','Penetration (L12M)','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance',@partName,'AnalysisScorecard','Brand Affinity:Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('finance','Brand Analysis Advocacy','Brand Advocacy', null, 'SubPage', null, 100, 0, null, 'Brand Advocacy', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('finance','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('finance','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('finance','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View]) 
VALUES ('finance', 'BrandAdvocacy', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View])
VALUES ('finance', 'BrandBuzz', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('finance', 'BrandLove', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('finance', 'BrandUsage', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisScorecard',		'Promotors Advocacy',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisBasedOn',			'Promotors Advocacy',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisScore',			'Promotors Advocacy',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisPotentialScore',	'Promotors Advocacy',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisScoreOverTime',	'Promotors Advocacy',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandAdvocacy',	'BrandAnalysisWhereNext',		'Promotors Advocacy',	'Advocacy',	'{"metrics":[{"key":"Promotors","metricName":"Promotors","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisScorecard',		'Positive Buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisBasedOn',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisScore',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisPotentialScore',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisScoreOverTime',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandBuzz',		'BrandAnalysisWhereNext',		'Positive Buzz',	'Buzz',	'{"metrics":[{"key":"Positive Buzz","metricName":"Positive Buzz","requestType":"scorecardPerformance"},{"key":"Negative Buzz","metricName":"Negative Buzz","requestType":"scorecardPerformance"},{"key":"Net Buzz","metricName":"Net Buzz","requestType":"scorecardPerformance"},{"key":"Advertising Awareness","metricName":"Advertising Awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance', 'BrandLove',		'BrandAnalysisScorecard',		'Brand Affinity:Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandLove',		'BrandAnalysisBasedOn',			'Brand Affinity:Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandLove',		'BrandAnalysisScore',			'Brand Affinity:Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandLove',		'BrandAnalysisPotentialScore',	'Brand Affinity:Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandLove',		'BrandAnalysisScoreOverTime',	'Brand Affinity:Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandLove',		'BrandAnalysisWhereNext',		'Brand Affinity:Love',	'Love',	'{"metrics":[{"key":"Image: A Market Leader","metricName":"Image: A Market Leader","requestType":"scorecardPerformance"},  {"key":"Image: A Market Leader","metricName":"Image:Promoters","requestType":"scorecardPerformance"},  {"key":"Image: Accurate","metricName":"Image: Accurate","requestType":"scorecardPerformance"},  {"key":"Image: Approachable","metricName":"Image: Approachable","requestType":"scorecardPerformance"},  {"key":"Image: Caring","metricName":"Image: Caring","requestType":"scorecardPerformance"},  {"key":"Image: Dependable","metricName":"Image: Dependable","requestType":"scorecardPerformance"},  {"key":"Image: Expert","metricName":"Image: Expert","requestType":"scorecardPerformance"},  {"key":"Image: For People Like Me","metricName":"Image: For People Like Me","requestType":"scorecardPerformance"},  {"key":"Image: Friendly","metricName":"Image: Friendly","requestType":"scorecardPerformance"},  {"key":"Image: Fun / Entertaining","metricName":"Image: Fun / Entertaining","requestType":"scorecardPerformance"},  {"key":"Image: Gets Me a Good Deal","metricName":"Image: Gets Me a Good Deal","requestType":"scorecardPerformance"},  {"key":"Image: Helpful","metricName":"Image: Helpful","requestType":"scorecardPerformance"},  {"key":"Image: Innovative","metricName":"Image: Innovative","requestType":"scorecardPerformance"},  {"key":"Image: Inspires Confidence","metricName":"Image: Inspires Confidence","requestType":"scorecardPerformance"},  {"key":"Image: Knowledgeable","metricName":"Image: Knowledgeable","requestType":"scorecardPerformance"},  {"key":"Image: None of These","metricName":"Image: None of These","requestType":"scorecardPerformance"},  {"key":"Image: On My Side","metricName":"Image: On My Side","requestType":"scorecardPerformance"},  {"key":"Image: Socially Responsible","metricName":"Image: Socially Responsible","requestType":"scorecardPerformance"},  {"key":"Image: Straight-Forward","metricName":"Image: Straight-Forward","requestType":"scorecardPerformance"},  {"key":"Image: Trustworthy","metricName":"Image: Trustworthy","requestType":"scorecardPerformance"},  {"key":"Image: Upmarket","metricName":"Image: Upmarket","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('finance', 'BrandUsage',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"}]}')


    Print 'Running migration script for "finance"... DONE'

----------------------

Print 'Running migration script for "retail"...'
Print 'retail: Creating pages'
-- Top page

INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('retail', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 110)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'retail' and Name = @pageName)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('retail', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'AnalysisScorecard','Promoters','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'AnalysisScorecard','Positive Buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'AnalysisScorecard','Penetration (L12M)','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail',@partName,'AnalysisScorecard','Brand Affinity:Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('retail','Brand Analysis Advocacy','Brand Advocacy', null, 'SubPage', null, 100, 0, null, 'Brand Advocacy', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('retail','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('retail','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('retail','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View]) 
VALUES ('retail', 'BrandAdvocacy', 'Brand Analysis Advocacy', 500, 'BrandAdvocacy', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View])
VALUES ('retail', 'BrandBuzz', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('retail', 'BrandLove', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('retail', 'BrandUsage', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6)



INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisScorecard',		'Promoters',	'Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisBasedOn',			'Promoters',	'Advocacy', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisScore',			'Promoters',	'Advocacy', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisPotentialScore',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisScoreOverTime',	'Promoters',	'Advocacy', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandAdvocacy',	'BrandAnalysisWhereNext',		'Promoters',	'Advocacy',	'{"metrics":[{"key":"Promoters","metricName":"Promoters","requestType":"scorecardPerformance"},{"key":"Neutrals","metricName":"Neutrals","requestType":"scorecardPerformance"},{"key":"Detractors","metricName":"Detractors","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisScorecard',		'Positive Buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisBasedOn',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisScore',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisPotentialScore',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisScoreOverTime',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandBuzz',		'BrandAnalysisWhereNext',		'Positive Buzz',	'Buzz',	'{"metrics":[{"key":"Positive Buzz","metricName":"Positive Buzz","requestType":"scorecardPerformance"},{"key":"Negative Buzz","metricName":"Negative Buzz","requestType":"scorecardPerformance"},{"key":"Net Buzz","metricName":"Net Buzz","requestType":"scorecardPerformance"},{"key":"Advertising Awareness","metricName":"Advertising Awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail', 'BrandLove',		'BrandAnalysisScorecard',		'Brand Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandLove',		'BrandAnalysisBasedOn',			'Brand Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandLove',		'BrandAnalysisScore',			'Brand Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandLove',		'BrandAnalysisPotentialScore',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandLove',		'BrandAnalysisScoreOverTime',	'Brand Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandLove',		'BrandAnalysisWhereNext',		'Brand Affinity:Love',	'Love',	'{"metrics":[{"key":"Image:Convenient","metricName":"Image:Convenient","requestType":"scorecardPerformance"},{"key":"Image:Easy to find what you want","metricName":"Image:Easy to find what you want","requestType":"scorecardPerformance"},{"key":"Image:Environmentally friendly","metricName":"Image:Environmentally friendly","requestType":"scorecardPerformance"},{"key":"Image:Ethical","metricName":"Image:Ethical","requestType":"scorecardPerformance"},{"key":"Image:Exciting","metricName":"Image:Exciting","requestType":"scorecardPerformance"},{"key":"Image:For people like you","metricName":"Image:For people like you","requestType":"scorecardPerformance"},{"key":"Image:Friendly","metricName":"Image:Friendly","requestType":"scorecardPerformance"},{"key":"Image:Good service","metricName":"Image:Good service","requestType":"scorecardPerformance"},{"key":"Image:Good value","metricName":"Image:Good value","requestType":"scorecardPerformance"},{"key":"Image:Premium","metricName":"Image:Premium","requestType":"scorecardPerformance"},{"key":"Image:Quality","metricName":"Image:Quality","requestType":"scorecardPerformance"},{"key":"Image:Stylish","metricName":"Image:Stylish","requestType":"scorecardPerformance"},{"key":"Image:Trusted","metricName":"Image:Trusted","requestType":"scorecardPerformance"},{"key":"Image:Wide range of product","metricName":"Image:Wide range of product","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('retail', 'BrandUsage',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')


    Print 'Running migration script for "retail"... DONE'

----------------------

Print 'Running migration script for "drinks"...'
Print 'drinks: Creating pages'
-- Top page

INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[PageDisplayIndex])
VALUES ('drinks', @pageName,'Brand Analysis', NULL, 'Standard', null, 100, 0, 'cols2rows3','Brand Analysis', 110)

SET @BrandAnalysisPageIndex = (select id from [Pages] where ProductShortCode = 'drinks' and Name = @pageName)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View] )
VALUES ('drinks', @partName, @pageName, 500, 'AnalysisScorecard', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'AnalysisScorecard','Promoters','Advocacy')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'AnalysisScorecard','Positive Buzz','Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'Text','BIGGER','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'Text','MORE LOVED','text-description')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'AnalysisScorecard','Penetration (L12M)','Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks',@partName,'AnalysisScorecard','Love','Love')


INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('drinks','Brand Analysis Buzz','Brand Buzz', null, 'SubPage', null, 100, 0, null, 'Brand Buzz', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('drinks','Brand Analysis Love','Brand Love', null, 'SubPage', null, 100, 0, null, 'Brand Love', @BrandAnalysisPageIndex)
INSERT INTO [dbo].[Pages] ([ProductShortCode],[Name],[DisplayName],[MenuIcon],[PageType],[HelpText],[MinUserLevel],[StartPage],[Layout],[PageTitle],[ParentId])
VALUES ('drinks','Brand Analysis Usage','Brand Usage', null, 'SubPage', null, 100, 0, null, 'Brand Usage', @BrandAnalysisPageIndex)


INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec,[View])
VALUES ('drinks', 'BrandBuzz', 'Brand Analysis Buzz', 500, 'BrandBuzz', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('drinks', 'BrandLove', 'Brand Analysis Love', 500, 'BrandLove', 'BiggerAndMoreLoved', 6)

INSERT INTO [dbo].[Panes] ([ProductShortCode], [PaneId], [PageName], Height, PaneType, Spec, [View])
VALUES ('drinks', 'BrandUsage', 'Brand Analysis Usage', 500, 'BrandUsage', 'BiggerAndMoreLoved', 6)


INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisScorecard',		'Positive Buzz',	'Buzz')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisBasedOn',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisScore',			'Positive Buzz',	'Buzz', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisPotentialScore',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisScoreOverTime',	'Positive Buzz',	'Buzz', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandBuzz',		'BrandAnalysisWhereNext',		'Positive Buzz',	'Buzz',	'{"metrics":[{"key":"Positive Buzz","metricName":"Positive Buzz","requestType":"scorecardPerformance"},{"key":"Negative Buzz","metricName":"Negative Buzz","requestType":"scorecardPerformance"},{"key":"Net Buzz","metricName":"Net Buzz","requestType":"scorecardPerformance"},{"key":"Advertising Awareness","metricName":"Advertising Awareness","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisScorecard',		'Love',	'Love')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisBasedOn',			'Love',	'Love', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisScore',			'Love',	'Love', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisPotentialScore',	'Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisScoreOverTime',	'Love',	'Love', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandLove',		'BrandAnalysisWhereNext',		'Love',	'Love',	'{"metrics":[{"key":"Image:Boring","metricName":"Image:Boring","requestType":"scorecardPerformance"},{"key":"Image:Brand for me","metricName":"Image:Brand for me","requestType":"scorecardPerformance"},{"key":"Image:Cheap and cheerful","metricName":"Image:Cheap and cheerful","requestType":"scorecardPerformance"},{"key":"Image:Classic","metricName":"Image:Classic","requestType":"scorecardPerformance"},{"key":"Image:Cool","metricName":"Image:Cool","requestType":"scorecardPerformance"},{"key":"Image:Crafted","metricName":"Image:Crafted","requestType":"scorecardPerformance"},{"key":"Image:Easy drinking","metricName":"Image:Easy drinking","requestType":"scorecardPerformance"},{"key":"Image:For old people","metricName":"Image:For old people","requestType":"scorecardPerformance"},{"key":"Image:For young people","metricName":"Image:For young people","requestType":"scorecardPerformance"},{"key":"Image:Full of taste and flavour","metricName":"Image:Full of taste and flavour","requestType":"scorecardPerformance"},{"key":"Image:Great with food","metricName":"Image:Great with food","requestType":"scorecardPerformance"},{"key":"Image:High quality","metricName":"Image:High quality","requestType":"scorecardPerformance"},{"key":"Image:Interesting","metricName":"Image:Interesting","requestType":"scorecardPerformance"},{"key":"Image:Market leader","metricName":"Image:Market leader","requestType":"scorecardPerformance"},{"key":"Image:Modern","metricName":"Image:Modern","requestType":"scorecardPerformance"},{"key":"Image:Old fashioned","metricName":"Image:Old fashioned","requestType":"scorecardPerformance"},{"key":"Image:Popular","metricName":"Image:Popular","requestType":"scorecardPerformance"},{"key":"Image:Proud to share with friends","metricName":"Image:Proud to share with friends","requestType":"scorecardPerformance"},{"key":"Image:Refreshing","metricName":"Image:Refreshing","requestType":"scorecardPerformance"},{"key":"Image:Something different","metricName":"Image:Something different","requestType":"scorecardPerformance"},{"key":"Image:Worth paying more for","metricName":"Image:Worth paying more for","requestType":"scorecardPerformance"}]}')

INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisScorecard',		'Penetration (L12M)',	'Usage')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisBasedOn',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"gender","metricName":"Gender (Analysis)","requestType":"crossbreak"},{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"segment","metricName":"Segment (Analysis)","requestType":"competition"},{"key":"affinity","metricName":"Affinity (Analysis)", "includePrimaryMetricFilter":false ,"requestType":"competition"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisScore',			'Penetration (L12M)',	'Usage', '{"metrics":[{"key":"region","metricName":"Region (Analysis)","requestType":"crossbreak"},{"key":"age","metricName":"Age (Analysis)","requestType":"crossbreak"}]}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisPotentialScore',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisScoreOverTime',	'Penetration (L12M)',	'Usage', '{"metrics": []}')
INSERT INTO [dbo].[Parts] ([ProductShortCode],[PaneId],[PartType],[Spec1],[Spec2],[Spec3]) VALUES ('drinks', 'BrandUsage',		'BrandAnalysisWhereNext',		'Penetration (L12M)',	'Usage',	'{"metrics":[{"key":"Awareness","metricName":"Awareness","requestType":"scorecardPerformance"},{"key":"Familiarity","metricName":"Familiarity","requestType":"scorecardPerformance"},{"key":"Consideration","metricName":"Consideration","requestType":"scorecardPerformance"},{"key":"Penetration (L12M)","metricName":"Penetration (L12M)","requestType":"scorecardPerformance"},{"key":"Penetration (L3M)","metricName":"Penetration (L3M)","requestType":"scorecardPerformance"}]}')

Print 'Running migration script for "drinks"... DONE'
