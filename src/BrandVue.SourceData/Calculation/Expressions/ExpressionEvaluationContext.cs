using IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// Represents a profile, and 0 or 1 arguments that are currently in scope (e.g. due to list comprehension, or lambda)
    /// </summary>
    public class ExpressionEvaluationContext
    {
        private readonly ManagedMemoryPool<Numeric> _managedMemory = new();
        public IProfileResponseEntity Profile { get; private set; }
        internal IManagedMemoryPool<Numeric> ManagedMemory => _managedMemory;

        internal Numeric Arg0 { get; set; }

        public void Reset(IProfileResponseEntity profileResponseEntity)
        {
            Profile = profileResponseEntity;
            Arg0 = null;
            _managedMemory.FreeAll();
        }
    }
}