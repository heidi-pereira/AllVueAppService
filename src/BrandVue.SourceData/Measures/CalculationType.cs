namespace BrandVue.SourceData.Measures
{
    public enum CalculationType
    {
        YesNo,
        Average,
        NetPromoterScore,
        Special_ShouldNotBeUsed,
        Text,
        //These are magic EO market metrics, added as a proof of concept for market metrics logic.
        //Before you add any new magic calculation types, consider extending BV with combining metrics feature.
        EoTotalSpendPerTimeOfDay,
        EoTotalSpendPerLocation
    }
}
