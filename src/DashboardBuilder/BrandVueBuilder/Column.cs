namespace BrandVueBuilder 
{
    internal class Column
    {
        public string Name { get; }
        public string Alias { get; }
        public string TypeOverride { get; }

        protected Column(string name, string alias, string typeOverride)
        {
            Name = name;
            Alias = alias;
            TypeOverride = typeOverride;
        }

        public static Column Simple(string name, string alias)
        {
            return new Column(name, alias, null);
        }

        public static Column WithScaleFactor(string name, string alias, string scaleFactor)
        {
            var typeOverride = string.IsNullOrEmpty(scaleFactor) ? null : "SMALLINT";
            return new Column(name, alias, typeOverride);
        }
    }
}