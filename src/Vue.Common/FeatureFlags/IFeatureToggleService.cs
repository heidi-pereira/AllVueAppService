namespace Vue.Common.FeatureFlags;

/// <summary>
/// Composite service that provides both feature querying and management capabilities.
/// This interface combines IFeatureQueryService and IFeatureManagementService for convenience.
/// </summary>
public interface IFeatureToggleService : IFeatureQueryService, IFeatureManagementService
{
}
