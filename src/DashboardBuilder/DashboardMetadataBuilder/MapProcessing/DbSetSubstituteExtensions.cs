using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace DashboardMetadataBuilder.MapProcessing
{
    internal static class DbSetSubstituteExtensions
    {
        public static TContext ReturnsItems<TContext, T>(this TContext context, Func<TContext, DbSet<T>> getSet, params T[] elements) where TContext : DbContext where T : class
        {
            var subsituteSet = SubstituteDbSet(elements);
            getSet(context).Returns(_ => subsituteSet);
            return context;
        }

        private static DbSet<T> SubstituteDbSet<T>(T[] elements) where T : class
        {
            var subsetConfigurationData = new List<T>(elements).AsQueryable();
            var setSubstitute = Substitute.For<DbSet<T>, IQueryable<T>>();
            ((IQueryable<T>)setSubstitute).Provider.Returns(subsetConfigurationData.Provider);
            ((IQueryable<T>)setSubstitute).Expression.Returns(subsetConfigurationData.Expression);
            ((IQueryable<T>)setSubstitute).ElementType.Returns(subsetConfigurationData.ElementType);
            ((IQueryable<T>)setSubstitute).GetEnumerator()
                .Returns(subsetConfigurationData.GetEnumerator());
            return setSubstitute;
        }
    }
}