namespace BrandVue.EntityFramework.MetaData.Reports
{
    [Flags]
    public enum DisplaySignificanceDifferences
    {
        None = 0,
        ShowDown = 0x01,
        ShowUp = 0x02,
        ShowBoth = ShowDown | ShowUp
    }
}
