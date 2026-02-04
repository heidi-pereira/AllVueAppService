using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using static BrandVue.EntityFramework.MetaData.VariableDefinitions.InstanceVariableComponentOperator;

namespace Test.BrandVue.SourceData.FieldExpressionParsing;

/// <summary>
/// These also cover CreateForSingleEntityIdAnswer for Variable
/// </summary>
class InstanceListVariableTests : ExpressionTestBase
{
    [TestCase(new[]{0}, 0, true, ExpectedResult = 1)]
    [TestCase(new[]{0}, 1, true, ExpectedResult = null)]
    [TestCase(new[]{1}, 0, true, ExpectedResult = null)]
    [TestCase(new[]{1}, 1, true, ExpectedResult = 1)]
    [TestCase(new[]{0,1}, 0, true, ExpectedResult = 1)]
    [TestCase(new[]{0,1}, 1,true,  ExpectedResult = 1)]
    // Check that the answers are the same when based on a nominally multi choice question (that just has one answer)
    [TestCase(new[]{0}, 0, false, ExpectedResult = 1)]
    [TestCase(new[]{0}, 1, false, ExpectedResult = null)]
    [TestCase(new[]{1}, 0, false, ExpectedResult = null)]
    [TestCase(new[]{1}, 1, false, ExpectedResult = 1)]
    [TestCase(new[]{0,1}, 0, false, ExpectedResult = 1)]
    [TestCase(new[]{0,1}, 1,false,  ExpectedResult = 1)]
    public int? SingleField_WithOredInstances_ReturnsSingleEntityId(int[] id1BrandIndices, int answerBrandIndex, bool isBasedOnSingleChoice) =>
        TestSingleFieldSingleGroupVariable(id1BrandIndices, answerBrandIndex, isBasedOnSingleChoice, Or);

    [TestCase(new[]{0}, 0, true, ExpectedResult = 1)]
    [TestCase(new[]{0}, 1, true, ExpectedResult = null)]
    [TestCase(new[]{1}, 0, true, ExpectedResult = null)]
    [TestCase(new[]{1}, 1, true, ExpectedResult = 1)]
    // See test below for seemingly missing cases here
    // Check that the answers are the same when based on a nominally multi choice question (that just has one answer)
    [TestCase(new[]{0}, 0, false, ExpectedResult = 1)]
    [TestCase(new[]{0}, 1, false, ExpectedResult = null)]
    [TestCase(new[]{1}, 0, false, ExpectedResult = null)]
    [TestCase(new[]{1}, 1, false, ExpectedResult = 1)]
    public int? SingleField_WithOneAndedInstance_ReturnsSingleEntityId(int[] id1BrandIndices, int answerBrandIndex, bool isBasedOnSingleChoice) =>
        TestSingleFieldSingleGroupVariable(id1BrandIndices, answerBrandIndex, isBasedOnSingleChoice, And);

    // Requiring 2 answers of a single answer question doesn't make sense...let's stick with the old path
    [TestCase(new[]{0,1}, 0, false, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 1,false,  ExpectedResult = null)]
    // Old slow path uses for complex case requiring multiple instances from the same variable
    [TestCase(new[]{0,1}, 0, true, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 1,true,  ExpectedResult = null)]
    public int? SingleField_WithMultipleAndedInstances_UsesOldPath(int[] id1BrandIndices, int answerBrandIndex, bool isBasedOnSingleChoice) =>
        TestSingleFieldSingleGroupVariable(id1BrandIndices, answerBrandIndex, isBasedOnSingleChoice, And, shouldUseInstanceListVariable: false);

    [TestCase(new[]{0}, 0, true, ExpectedResult = null)]
    [TestCase(new[]{0}, 1, true, ExpectedResult = 1)]
    [TestCase(new[]{1}, 0, true, ExpectedResult = 1)]
    [TestCase(new[]{1}, 1, true, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 0, true, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 1,true,  ExpectedResult = null)]
    // Sanity check that the answers are the same when not using an InstanceListVariable
    [TestCase(new[]{0}, 0, false, ExpectedResult = null)]
    [TestCase(new[]{0}, 1, false, ExpectedResult = 1)]
    [TestCase(new[]{1}, 0, false, ExpectedResult = 1)]
    [TestCase(new[]{1}, 1, false, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 0, false, ExpectedResult = null)]
    [TestCase(new[]{0,1}, 1,false,  ExpectedResult = null)]
    public int? SingleField_WithNottedInstances_ReturnsSingleEntityId(int[] id1BrandIndices, int answerBrandIndex, bool isBasedOnSingleChoice) =>
        TestSingleFieldSingleGroupVariable(id1BrandIndices, answerBrandIndex, isBasedOnSingleChoice, Not);

    private int? TestSingleFieldSingleGroupVariable(int[] id1BrandIndices, int answerBrandIndex, bool isBasedOnSingleChoice, InstanceVariableComponentOperator instanceOperator, bool shouldUseInstanceListVariable = true)
    {
        var answerBrand = Brand0AndBrand1[answerBrandIndex];
        var instanceIds = id1BrandIndices.Select(i => Brand0AndBrand1[i].Id).ToList();
        string fieldIdentifier = "fave_brand";
        var fieldEntity = TestEntityTypeRepository.Brand;

        _responseFieldManager.Add(fieldIdentifier, Subset.Id, isBasedOnSingleChoice, types: new[] { fieldEntity });

        var variable = DeclareVariable(new VariableComponent[]
        {
            new InstanceListVariableComponent()
            {
                FromVariableIdentifier = fieldIdentifier,
                FromEntityTypeName = fieldEntity.Identifier,
                InstanceIds = instanceIds,
                Operator = instanceOperator
            },
        });

        var response = CreateProfile((answerBrand,
            new (string FieldName, int Value)[] { (fieldIdentifier, answerBrand.Id) }));

        if (shouldUseInstanceListVariable) Assert.That(variable.WrappedVariable, Is.TypeOf(typeof(InstanceListVariable)), "Sanity check that InstanceListVariable is actually being tested by this code");
        var getter = variable.CreateForSingleEntity(_ => true);
        var memory = getter(response);
        var singleEntityId = memory.ToArray().Select(x => (int?)x).SingleOrDefault();
        return singleEntityId;
    }

    /// <summary>
    /// The test cases are in the same order as the variable component that satisfies them
    /// </summary>
    [TestCase(0, 0, 1, ExpectedResult = 1)]
    [TestCase(0, 1, 0, ExpectedResult = 2)]
    [TestCase(0, 1, 1, ExpectedResult = 3)]
    [TestCase(1, 0, 0, ExpectedResult = 4)]
    [TestCase(1, 0, 1, ExpectedResult = 5)]
    [TestCase(1, 1, 0, ExpectedResult = 6)]
    [TestCase(1, 1, 1, ExpectedResult = 7)]
    [TestCase(0, 0, 0, ExpectedResult = 8)]
    [TestCase(null, null, null, ExpectedResult = 9)]
    [TestCase(null, null, 0, ExpectedResult = null)]
    [TestCase(null, null, 1, ExpectedResult = null)]
    [TestCase(null, 0, 0, ExpectedResult = null)]
    [TestCase(null, 0, 1, ExpectedResult = null)]
    [TestCase(null, 1, 0, ExpectedResult = null)]
    [TestCase(null, 1, 1, ExpectedResult = null)]
    [TestCase(0, null, 0, ExpectedResult = null)]
    [TestCase(0, null, 1, ExpectedResult = null)]
    [TestCase(1, null, 0, ExpectedResult = null)]
    [TestCase(1, null, 1, ExpectedResult = null)]
    public int? TestTwoFieldTwoGroupVariable(int? faveIndexAnswer, int? coolIndexAnswer, int? biggestIndexAnswer)
    {
        var faveAnswer = faveIndexAnswer is null ? null : Brand0AndBrand1[faveIndexAnswer.Value];
        var coolAnswer = coolIndexAnswer is null ? null : Brand0AndBrand1[coolIndexAnswer.Value];
        var bigAnswer = biggestIndexAnswer is null ? null : Brand0AndBrand1[biggestIndexAnswer.Value];
        string faveField = "fave_brand";
        string coolField = "cool_brand";
        string bigField = "big_brand";
        var fieldEntity = TestEntityTypeRepository.Brand;

        _responseFieldManager.Add(faveField, Subset.Id, true, types: new[] { fieldEntity });
        _responseFieldManager.Add(coolField, Subset.Id, true, types: new[] { fieldEntity });
        _responseFieldManager.Add(bigField, Subset.Id, true, types: new[] { fieldEntity });

        int brand0Id = Brand0AndBrand1[0].Id;
        int brand1Id = Brand0AndBrand1[1].Id;

        // The And and Or passed to InstanceList are arbitrary when there's a single element - just trying to check both work in a few random cases
        var variable = DeclareVariable(new VariableComponent[]
        {
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand0Id),
                InstanceList(coolField, Or, brand0Id), InstanceList(bigField, Or, brand1Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand0Id),
                InstanceList(coolField, Or, brand1Id), InstanceList(bigField, Or, brand0Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand0Id),
                InstanceList(coolField, Or, brand1Id), InstanceList(bigField, Or, brand1Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand1Id),
                InstanceList(coolField, Or, brand0Id), InstanceList(bigField, Or, brand0Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, And, brand1Id),
                InstanceList(coolField, Or, brand0Id), InstanceList(bigField, Or, brand1Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, And, brand1Id),
                InstanceList(coolField, Or, brand1Id), InstanceList(bigField, Or, brand0Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand1Id),
                InstanceList(coolField, And, brand1Id), InstanceList(bigField, Or, brand1Id)),

            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand0Id),
                InstanceList(coolField, Or, brand0Id), InstanceList(bigField, Or, brand0Id)),
            //Should not be satisfied unless all fields are null - just here to check it doesn't interfere with the others
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Not, brand0Id, brand1Id),
                InstanceList(coolField, Not, brand0Id, brand1Id), InstanceList(bigField, Not, brand0Id, brand1Id)),
        });
        
        var response = CreateProfileFromSingleChoices((faveField, faveAnswer), (coolField, coolAnswer), (bigField, bigAnswer));


        Assert.That(variable.WrappedVariable, Is.InstanceOf<InstanceListVariable>(),
            "Sanity check that the InstanceListVariable is actually being tested and the performance optimization is activated");
        
        var getEntityIds = variable.CreateForSingleEntity(_ => true);
        var entityIds = getEntityIds(response).ToArray().Select<int, int?>(x=> x).ToArray();
        return entityIds.SingleOrDefault();
    }

    /// <summary>
    /// The test cases are in the same order as the variable component that satisfies them
    /// </summary>
    [TestCase(0, 0, 0, ExpectedResult = 1)]
    [TestCase(0, 0, 1, ExpectedResult = 1)]
    [TestCase(0, 1, 0, ExpectedResult = 1)]
    [TestCase(0, 1, 1, ExpectedResult = 1)]
    [TestCase(1, 0, 0, ExpectedResult = 1)]
    [TestCase(1, 0, 1, ExpectedResult = 1)]
    [TestCase(1, 1, 0, ExpectedResult = 1)]
    [TestCase(1, 1, 1, ExpectedResult = 2)]
    public int? TestTwoFieldTwoGroupOredVariable(int? faveIndexAnswer, int? coolIndexAnswer, int? biggestIndexAnswer)
    {
        var faveAnswer = faveIndexAnswer is null ? null : Brand0AndBrand1[faveIndexAnswer.Value];
        var coolAnswer = coolIndexAnswer is null ? null : Brand0AndBrand1[coolIndexAnswer.Value];
        var bigAnswer = biggestIndexAnswer is null ? null : Brand0AndBrand1[biggestIndexAnswer.Value];
        string faveField = "fave_brand";
        string coolField = "cool_brand";
        string bigField = "big_brand";
        var fieldEntity = TestEntityTypeRepository.Brand;

        _responseFieldManager.Add(faveField, Subset.Id, true, types: new[] { fieldEntity });
        _responseFieldManager.Add(coolField, Subset.Id, true, types: new[] { fieldEntity });
        _responseFieldManager.Add(bigField, Subset.Id, true, types: new[] { fieldEntity });

        int brand0Id = Brand0AndBrand1[0].Id;
        int brand1Id = Brand0AndBrand1[1].Id;


        var variable = DeclareVariable(new VariableComponent[]
        {
            CompositeInstances(CompositeVariableSeparator.Or, InstanceList(faveField, Not, brand1Id),
                InstanceList(coolField, Not, brand1Id), InstanceList(bigField, Not, brand1Id)),
            CompositeInstances(CompositeVariableSeparator.And, InstanceList(faveField, Or, brand1Id),
                InstanceList(coolField, Or, brand1Id), InstanceList(bigField, Or, brand1Id)),
        });
        var response = CreateProfileFromSingleChoices((faveField, faveAnswer), (coolField, coolAnswer), (bigField, bigAnswer));

        Assert.That(variable.WrappedVariable, Is.InstanceOf<InstanceListVariable>(),
            "Sanity check that the InstanceListVariable is actually being tested and the performance optimization is activated");

        Assert.That(variable.CanCreateForSingleEntity(), Is.True, "Sanity check we're allowed to call CreateForSingleEntityIdAnswer");

        var getEntityIds = variable.CreateForSingleEntity(_ => true);
        var entityIds = getEntityIds(response).ToArray().Select<int, int?>(x=> x).ToArray();
        return entityIds.SingleOrDefault();
    }

    [Test]
    public void TestMultipleChoiceNotCondition()
    {
        // InstanceListVariable has an issue with multiple choice + NOT condition - it inverts the NOT which doesnt work when choices are not exclusive
        // This checks that it is not using the optimized InstanceListVariable path in this scenario

        string fieldIdentifier = "brand_awareness";
        var fieldEntity = TestEntityTypeRepository.Brand;

        _responseFieldManager.Add(fieldIdentifier, Subset.Id, "CHECKBOX", types: new[] { fieldEntity });

        var variable = DeclareVariable(new VariableComponent[]
        {
            new InstanceListVariableComponent()
            {
                FromVariableIdentifier = fieldIdentifier,
                FromEntityTypeName = fieldEntity.Identifier,
                InstanceIds = [Brand0AndBrand1[1].Id],
                Operator = InstanceVariableComponentOperator.Not
            },
        });

        var response = CreateProfile(
            (Brand0AndBrand1[0], new (string FieldName, int Value)[] { (fieldIdentifier, Brand0AndBrand1[0].Id) }),
            (Brand0AndBrand1[1], new (string FieldName, int Value)[] { (fieldIdentifier, Brand0AndBrand1[1].Id) }));

        var getter = variable.CreateForSingleEntity(_ => true);
        var memory = getter(response);
        var singleEntityId = memory.ToArray().Select(x => (int?)x).SingleOrDefault();

        Assert.That(singleEntityId, Is.Null);
        Assert.That(variable.WrappedVariable, Is.Not.TypeOf(typeof(InstanceListVariable)), "Sanity check that InstanceListVariable is not being used by this code");
    }

    private InstanceListVariableComponent InstanceList(string fieldName, InstanceVariableComponentOperator op, params int[] instanceIds) =>
        InstanceList(fieldName, op, _responseFieldManager.Get(fieldName).EntityCombination.Single(), instanceIds);

    private InstanceListVariableComponent InstanceList(string fieldName, InstanceVariableComponentOperator op, EntityType fromEntityType, params int[] instanceIds)
    {
        return new InstanceListVariableComponent
        {
            FromVariableIdentifier = fieldName,
            FromEntityTypeName = fromEntityType.Identifier,
            InstanceIds = instanceIds.ToList(),
            Operator = op
        };
    }

    private static CompositeVariableComponent CompositeInstances<T>(CompositeVariableSeparator compositeVariableSeparator, params T[] variableComponents) where T: VariableComponent
    {
        return new CompositeVariableComponent()
        {
            CompositeVariableSeparator = compositeVariableSeparator,
            CompositeVariableComponents = variableComponents.Select<T, VariableComponent>(x => x).ToList(),
        };
    }

    private IntegerVariable DeclareVariable(VariableComponent[] orderedGroupComponents, string variableIdentifier = "VariableUnderTest")
    {
        var instanceListVariable =
            VariableConfigurationGenerator.CreateInstanceListVariable(variableIdentifier, orderedGroupComponents);
        var variableEntityLoader = new VariableEntityLoader(_entityTypeRepository, _entityInstanceRepository,
            Substitute.For<ILoadableEntitySetRepository>());
        variableEntityLoader.CreateOrUpdateEntityForVariable(instanceListVariable);

        var fieldExpressionParser =
            TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepository,
                _entityTypeRepository);
        fieldExpressionParser.DeclareOrUpdateVariable(instanceListVariable);
        var variable = (IntegerVariable)fieldExpressionParser.GetDeclaredVariableOrNull(variableIdentifier);
        return variable;
    }
}