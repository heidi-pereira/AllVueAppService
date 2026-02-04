using BrandVue.EntityFramework.Answers;
using BrandVue.SourceData.AnswersMetadata;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using BrandVue.EntityFramework.Answers.Model;

namespace TestCommon.Mocks;

public class AnswersMetaDataHelper
{
    public static IAnswerDbContextFactory CreateMockAnswersDbContext(DbSet<Question> mockQuestions = null, DbSet<Answer> mockAnswers = null)
    {
        var answersDbContext = Substitute.For<AnswersDbContext>();
        if (mockQuestions != null)
        {
            answersDbContext.Questions.Returns(mockQuestions);

        }
        else
        {
            var emptyList = CreateMockDbSet(new List<Question>());
            answersDbContext.Questions.Returns(emptyList);
        }

        if (mockAnswers != null)
        {
            answersDbContext.Answers.Returns(mockAnswers);
        }
        else
        {
            var emptyList = CreateMockDbSet(new List<Answer>());
            answersDbContext.Answers.Returns(emptyList);
        }

        var answersDbContextFactory = Substitute.For<IAnswerDbContextFactory>();
        answersDbContextFactory.CreateDbContext().Returns(answersDbContext);
        return answersDbContextFactory;
    }

    public static DbSet<T> CreateMockDbSet<T>(List<T> elements) where T : class
    {
        var queryable = elements.AsQueryable();
        var dbSet = Substitute.For<DbSet<T>, IQueryable<T>>();

        ((IQueryable<T>)dbSet).Provider.Returns(queryable.Provider);
        ((IQueryable<T>)dbSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)dbSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)dbSet).GetEnumerator().Returns(queryable.GetEnumerator());

        return dbSet;
    }
}
