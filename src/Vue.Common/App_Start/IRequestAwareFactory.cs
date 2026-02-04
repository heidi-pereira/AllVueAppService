namespace Vue.Common.App_Start;

/// <summary>
/// Depend on this in constructor when you need to be able to *create* a class that's bound in IoCConfig for the current request's context
/// </summary>
public interface IRequestAwareFactory<out T>
{
    T Create();
}