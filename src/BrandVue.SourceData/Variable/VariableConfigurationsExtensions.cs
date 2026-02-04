using System.Runtime.CompilerServices;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Variable;

public static class VariableConfigurationsExtensions
{
    internal static IQueryable<VariableConfiguration> For(
        this DbSet<VariableConfiguration> contextVariableConfigurations, IProductContext productContext, bool tracking)
    {
        IQueryable<VariableConfiguration> dbContextVariableConfigurations = contextVariableConfigurations
            .Include(v => v.VariableDependencies)
            .ThenInclude(d => d.DependentUponVariable)
            .Include(v => v.VariablesDependingOnThis)
            .ThenInclude(d => d.Variable);
        if (!tracking)
        {
            dbContextVariableConfigurations = dbContextVariableConfigurations.AsNoTrackingWithIdentityResolution();
        }

        return dbContextVariableConfigurations.Where(v =>
            v.ProductShortCode == productContext.ShortCode && v.SubProductId == productContext.SubProductId);
    }

    private const int MaxDependencies = 100;
    /// <returns>null for cyclic dependencies (which it logs)</returns>
    internal static IEnumerable<VariableConfiguration> GetTransitiveDependencies(this VariableConfiguration variable, ILogger logger)
    {
        int recursionGuard = 0;
        var transitiveDependencies = variable.FollowMany(v =>
        {
            if (recursionGuard++ > MaxDependencies)
            {
                logger.LogError($"Recursive definition in variable `{variable.Identifier}`");
                return [];
            }

            return v.VariableDependencies.Select(static x => x.DependentUponVariable);
        }).ToArray();
        return recursionGuard > MaxDependencies ? null : transitiveDependencies;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBaseVariable(this VariableConfiguration v) => v.Definition.IsBaseVariable();

    public static bool IsBaseVariable(this VariableDefinition vd) =>
        vd is BaseFieldExpressionVariableDefinition or BaseGroupedVariableDefinition;
}