using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;

namespace TestCommon
{
    public class TestFieldExpressionParser
    {
        public static IFieldExpressionParser PrePopulateForFields(IResponseFieldManager responseFieldManager, IEntityRepository entityRepository, IResponseEntityTypeRepository responseEntityTypeRepository)
        {
            var parser = new FieldExpressionParser(responseFieldManager, entityRepository, responseEntityTypeRepository);
            parser.DeclareDummyQuestionVariables(responseFieldManager.GetAllFields());
            return parser;
        }
    }
}