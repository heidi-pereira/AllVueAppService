using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace BrandVue.AuthMiddleware.FeatureToggle;

public class FeatureToggleAttribute : Attribute
{
    public readonly FeatureCode FeatureCode;

    public FeatureToggleAttribute(FeatureCode featureCode)
    {
        FeatureCode = featureCode;
    }
}

