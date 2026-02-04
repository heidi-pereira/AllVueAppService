namespace BrandVue.EntityFramework.MetaData
{
    public enum AutoGenerationType : byte
    {
        Original = 0, //user created metric
        CreatedFromField = 1, //default auto generated
        CreatedFromNumeric = 2 //numeric auto generated
    }
}