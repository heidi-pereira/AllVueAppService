using DashboardMetadataBuilder.MapProcessing.SupportFiles;
using System;

namespace BrandVueBuilder
{
    internal class TextValueConstraint : IFieldConstraint
    {
        private readonly string _column;
        private readonly string _value;

        public TextValueConstraint(string column, string value)
        {
            _column = column;
            _value = value;
        }

        public JsonFilterColumn GenerateFilterColumnOrNull()
        {
            if (_column =="varCode") return null; //Can't tell which bit is the varcode base because we don't have the question model. BrandVue will handle this when looking up the question
            if (int.TryParse(_value, out var intVal)) return new JsonFilterColumn(_column, intVal);
            throw new NotImplementedException("Non-numeric text constraints not supported");
        }
    }
}