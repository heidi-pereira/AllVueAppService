using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NSubstitute;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Microsoft.EntityFrameworkCore.Query;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Test.BrandVue.SourceData.FeaturesRepository
{
    public class FeatureRepositoryHelper
    {
        public static IDbContextFactory<MetaDataContext> CreateMetaDbContextForUserFeatures(
            List<Feature> features = null, 
            List<UserFeature> userFeatures = null)
        {
            return CreateMetaDbContext(
                features,
                userFeatures,
                (context, specificSet) => context.UserFeatures = specificSet);
        }

        public static IDbContextFactory<MetaDataContext> CreateMetaDbContextForOrgFeatures(
            List<Feature> features = null, 
            List<OrganisationFeature> orgFeatures = null)
        {
            return CreateMetaDbContext(
                features,
                orgFeatures,
                (context, specificSet) => context.OrganisationFeatures = specificSet);
        }

        private static IDbContextFactory<MetaDataContext> CreateMetaDbContext<T>(
            List<Feature> features,
            List<T> specificFeatures,
            Action<MetaDataContext, DbSet<T>> setSpecificFeaturesAction)
            where T : class
        {
            var dbContext = Substitute.For<MetaDataContext>();
            
            var featuresDbSet = CreateMockDbSet(features ?? new List<Feature>());
            dbContext.Features.Returns(featuresDbSet);
            
            var specificFeaturesDbSet = CreateMockDbSet(specificFeatures ?? new List<T>());
            setSpecificFeaturesAction(dbContext, specificFeaturesDbSet);

            var dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
            dbContextFactory.CreateDbContext().Returns(dbContext);
            dbContextFactory.CreateDbContextAsync().Returns(dbContext);
            
            return dbContextFactory;
        }

        internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;
            internal TestAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }
            public IQueryable CreateQuery(Expression expression)
            {
                return new TestAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TEntity> CreateQuery<TEntity>(Expression expression)
            {
                return new TestAsyncEnumerable<TEntity>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            {
                return Execute<TResult>(expression);
            }
        }

        internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
        {
            public TestAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            { }

            public TestAsyncEnumerable(Expression expression)
                : base(expression)
            { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IQueryProvider IQueryable.Provider
            {
                get { return new TestAsyncQueryProvider<T>(this); }
            }
        }

        internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public T Current => _inner.Current;

            T IAsyncEnumerator<T>.Current => _inner.Current;

            ValueTask IAsyncDisposable.DisposeAsync()
            {
                _inner.Dispose();
                return ValueTask.CompletedTask;
            }

            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
            {
                return new ValueTask<bool>(_inner.MoveNext());
            }
        }

        public static DbSet<T> CreateMockDbSet<T>(List<T> elements) where T : class
        {
            var data = elements.AsQueryable();

            var dbSet = Substitute.For<DbSet<T>, IAsyncEnumerable<T>, IQueryable<T>>();


            var asyncEnumerator = new TestAsyncEnumerator<T>(data.GetEnumerator());
            ((IAsyncEnumerable<T>)dbSet).GetAsyncEnumerator()
                .Returns(asyncEnumerator);

            var provider = new TestAsyncQueryProvider<T>(data.Provider);
            ((IQueryable<T>)dbSet).Provider.Returns(provider);

            var expression = data.Expression;
            ((IQueryable<T>)dbSet).Expression.Returns(expression);

            var elementType = data.ElementType;
            ((IQueryable<T>)dbSet).ElementType.Returns(elementType);

            var enumerator = data.GetEnumerator();
            ((IQueryable<T>)dbSet).GetEnumerator().Returns(enumerator);

            return dbSet;
        }
    }
}
