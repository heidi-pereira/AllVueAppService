using System.Text.RegularExpressions;

namespace BrandVue.EntityFramework
{
    public static class SqlIdentifierExtensions
    {
        /// <remarks>
        /// Any letter or underscore, followed by up to 127 letters, numbers or one of these @$#_
        /// https://stackoverflow.com/a/30152027
        /// </remarks>
        private static readonly Regex SqlIdentifierRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$");
        /// <summary>
        /// Whitelisted some things I'm pretty sure are ok. I'd much rather we move to the stricter version above though.
        /// The key is to avoid square brackets and control characters like backspace
        /// </summary>
        private static readonly Regex QuotedSqlIdentifierRegex = new Regex(@"^[\p{L}\p{N}!_'@""\&\.\(\)\-\:\,\/\ 0-9]*$");

        public static string AssertSafeQuotedSqlId(this string toValidate) => 
            QuotedSqlIdentifierRegex.IsMatch(toValidate) ? toValidate
                : throw new ArgumentOutOfRangeException(nameof(toValidate), toValidate, $"`{toValidate}` is not allowed as a SQL identifier");

        public static string AssertSafeSqlId(this string toValidate) => 
            SqlIdentifierRegex.IsMatch(toValidate) ? toValidate
                : throw new ArgumentOutOfRangeException(nameof(toValidate), toValidate, $"`{toValidate}` is not allowed as a SQL identifier");
    }
}