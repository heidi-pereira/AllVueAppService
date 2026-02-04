using System;
using System.Linq;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using Microsoft.Data.SqlClient.DataClassification;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd;

[TestFixture]
public class SampleSizeMetadataTests
{
    private IEntityRepository _entityRepository;

    [Test]
    public void ProvidesSampleSizeMetaDataForBrandResultsProviderParameters()
    {
        var entityRepository = PipelineResultsProviderTests.CreateEntityRepository();
        var rpp = new ResultsProviderParameters();
        rpp.FocusEntityInstanceId = PipelineResultsProviderTests.Brand1.Id;
        rpp.RequestedInstances = new TargetInstances(PipelineResultsProviderTests.BrandEntityType, new [] { PipelineResultsProviderTests.Brand1 });
        (string Label, WeightedDailyResult Result)[][] results =
        [
            new [] {  (PipelineResultsProviderTests.Brand1.Name, new WeightedDailyResult(new DateTime(2000, 01, 01)) { WeightedSampleSize = 100, UnweightedSampleSize = 50}) }
        ];
        var orderedResults = results.Select(x=>x.Select(y=>y.Result).ToArray()).ToArray();
        
        var sampleSizeMetadata = rpp.GetSampleSizeMetadata(results, orderedResults, entityRepository);
        
        Assert.That(sampleSizeMetadata, Is.Not.Null);
        Assert.That(50, Is.EqualTo(sampleSizeMetadata.SampleSize.Unweighted));
    }
    
    [Test]
    public void ProvidesSampleSizeMetaDataForImageResultsProviderParameters()
    {
        var entityRepository = PipelineResultsProviderTests.CreateEntityRepository();
        var rpp = new ResultsProviderParameters();
        rpp.FocusEntityInstanceId = PipelineResultsProviderTests.Image1.Id;
        rpp.RequestedInstances = new TargetInstances(PipelineResultsProviderTests.ImageEntityType, new [] { PipelineResultsProviderTests.Image1 });
        (string Label, WeightedDailyResult Result)[][] results =
        [
            new [] {  (PipelineResultsProviderTests.Brand1.Name, new WeightedDailyResult(new DateTime(2000, 01, 01)) { WeightedSampleSize = 100, UnweightedSampleSize = 50}) }
        ];
        var orderedResults = results.Select(x=>x.Select(y=>y.Result).ToArray()).ToArray();
        
        var sampleSizeMetadata = rpp.GetSampleSizeMetadata(results, orderedResults, entityRepository);
        
        Assert.That(sampleSizeMetadata, Is.Not.Null);
        Assert.That(50, Is.EqualTo(sampleSizeMetadata.SampleSize.Unweighted));
    }

    [Test]
    public void FailsForResultsProviderParametersWhenEntityIsIncorrect()
    {
        var entityRepository = PipelineResultsProviderTests.CreateEntityRepository();
        var rpp = new ResultsProviderParameters();
        rpp.FocusEntityInstanceId = PipelineResultsProviderTests.Image1.Id;
        rpp.RequestedInstances = new TargetInstances(PipelineResultsProviderTests.BrandEntityType, new [] { PipelineResultsProviderTests.Brand1 });
        (string Label, WeightedDailyResult Result)[][] results =
        [
            new [] {  (PipelineResultsProviderTests.Brand1.Name, new WeightedDailyResult(new DateTime(2000, 01, 01)) { WeightedSampleSize = 100, UnweightedSampleSize = 50}) }
        ];
        var orderedResults = results.Select(x=>x.Select(y=>y.Result).ToArray()).ToArray();

        Assert.Throws<ArgumentException>(() => rpp.GetSampleSizeMetadata(results, orderedResults, entityRepository));
    }
}