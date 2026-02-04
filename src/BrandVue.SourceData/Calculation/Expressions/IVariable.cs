using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// The most general form of the variable concept in Vue.
    /// This is independent of how the user specified the variable (VariableConfiguration/VariableDefinition) and of how we calculate the value (implementations of this interface specify that).
    /// </summary>
    public interface IVariable
    {
        IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }
        IReadOnlyCollection<EntityType> UserEntityCombination { get; }
        /// <summary>
        /// If this is true, you can use CreateForSingleEntity to get all answers for a respondent
        /// </summary>
        bool OnlyDimensionIsEntityType();
        int? ConstantValue => null;
        IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset);
    }

    /// <summary>
    /// At some point should probably make this public and implement from all IVariables (potentially merge interfaces) with appropriate testing.
    /// For now, only need it in one place, so keep it internal.
    /// </summary>
    internal interface IVariableWithDependencies : IVariable
    {
        IReadOnlyCollection<string> VariableDependencyIdentifiers { get; }
    }

    public static class VariableExtensions
    {
        public static bool CanCreateForSingleEntity(this IVariable variable) => variable.OnlyDimensionIsEntityType() || variable.ConstantValue.HasValue;
    }

    /// <typeparam name="TOut">The type the variable evaluates too: e.g. int? is used for choiceids and raw values, bool is used for the base variable of a metric</typeparam>
    public interface IVariable<out TOut> : IVariable
    {
        Func<IProfileResponseEntity, TOut> CreateForEntityValues(EntityValueCombination entityValues);

        /// <summary>
        /// Resulting function is NOT THREAD SAFE (see InstanceListVariable.CreateForSingleEntityIdAnswer impl)
        /// Only valid for variables with exactly one entity.
        /// If variable has value dimension as well as the entity, you must pass a value predicate to get back the entity ids that match it
        /// See <see cref="IVariable.OnlyDimensionIsEntityType"/>, if that returns true, there is no need for a predicate.
        /// Multi-choice questions are already filtered to only those that have value 1
        /// </summary>
        /// <param name="valuePredicate"></param>
        Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<TOut, bool> valuePredicate);
    }
}
