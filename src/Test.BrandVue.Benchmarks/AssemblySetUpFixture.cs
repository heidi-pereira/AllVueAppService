using DiffEngine;
using NUnit.Framework;
using VerifyTests;

// ReSharper disable once CheckNamespace
namespace Test.BrandVue;

[SetUpFixture]
public class AssemblySetUpFixture
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        VerifierSettings.SortPropertiesAlphabetically();
        VerifierSettings.DontIgnoreEmptyCollections();
        VerifierSettings.AlwaysIncludeMembersWithType(typeof(object));
        DiffTools.UseOrder(DiffTool.WinMerge);
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
        // ...
    }
}