namespace BrandVue.EntityFramework.Answers
{
    public sealed class DbLocation : IEquatable<DbLocation>
    {
        public static readonly DbLocation SectionEntity = new DbLocation("SectionChoiceId");
        public static readonly DbLocation PageEntity = new DbLocation("PageChoiceId");
        public static readonly DbLocation QuestionEntity = new DbLocation("QuestionChoiceId");
        public static readonly DbLocation AnswerEntity = new DbLocation("AnswerChoiceId");
        public static readonly DbLocation AnswerShort = new DbLocation("AnswerValue");
        public static readonly DbLocation AnswerText = new DbLocation("AnswerText");
        /// <summary>
        /// Returns the constant 1 rather than accessing a column. This is a hack because old field definitions relied on pulling a 1 through from optValue sometimes
        /// </summary>
        public static readonly DbLocation ConstantOne = new DbLocation("1");

        private readonly string _safeSqlColumnName;

        public string SafeSqlReference => _safeSqlColumnName == "1" ? "1" : $"[{_safeSqlColumnName}]";
        public string UnquotedColumnName => _safeSqlColumnName;

        public DbLocation(string unquotedColumnName)
        {
            _safeSqlColumnName = unquotedColumnName == "1" ? "1" : unquotedColumnName?.AssertSafeQuotedSqlId() ?? throw new ArgumentNullException(nameof(unquotedColumnName));
        }

        public bool Equals(DbLocation other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _safeSqlColumnName == other._safeSqlColumnName;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is DbLocation other && Equals(other);

        public override int GetHashCode() => (_safeSqlColumnName != null ? _safeSqlColumnName.GetHashCode() : 0);

        public static bool operator ==(DbLocation left, DbLocation right) => Equals(left, right);

        public static bool operator !=(DbLocation left, DbLocation right) => !Equals(left, right);

        public override string ToString() => $"{nameof(_safeSqlColumnName)}: {_safeSqlColumnName}";
    }
}