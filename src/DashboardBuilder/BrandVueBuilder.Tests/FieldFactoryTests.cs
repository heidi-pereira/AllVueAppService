using NUnit.Framework;

namespace BrandVueBuilder.Tests
{
    public class FieldFactoryTests
    {
        [TestCase("MissingCurlyBrackets", false)]
        [TestCase("{EntityInstance_Underscored}", false)]
        [TestCase("{EntityInstance}", true, "EntityInstance")]
        [TestCase("{EntityInstanceWithNumbers123456790}", true, "EntityInstanceWithNumbers123456790")]
        [TestCase("prefix{EntityInstance}", true, "EntityInstance", "prefix")]
        [TestCase("{EntityInstance(1)}", true, "EntityInstance", "", "1")]
        [TestCase("{EntityInstance(5)}", false)]
        [TestCase("{EntityInstance:modify}", true, "EntityInstance", "", "", "modify")]
        [TestCase("prefix{EntityInstance(2):modify}", true, "EntityInstance", "prefix", "2", "modify")]
        public void EntityInstancesAreCorrectlyParsedFromRegex(string input, bool match, string expectedEntityName = "",
            string expectedVarCodePrefix = "", string expectedEntityIndex = "", string expectedModifier = "")
        {
            var regexMatch = FieldFactory.EntityRegex.Match(input);
            Assert.Multiple(() =>
            {
                Assert.That(regexMatch.Success, Is.EqualTo(match));
                if (regexMatch.Success)
                {
                    Assert.That(regexMatch.Groups[FieldFactory.EntityNameGroup].Value, Is.EqualTo(expectedEntityName));
                    Assert.That(regexMatch.Groups[FieldFactory.VarCodePrefixGroup].Value, Is.EqualTo(expectedVarCodePrefix));
                    Assert.That(regexMatch.Groups[FieldFactory.EntityIndexGroup].Value, Is.EqualTo(expectedEntityIndex));
                    Assert.That(regexMatch.Groups[FieldFactory.ModifierGroup].Value, Is.EqualTo(expectedModifier));
                }
            });
        }

        [TestCase("MissingCurlyBrackets", false)]
        [TestCase("{NotValue:something}", false)]
        [TestCase("{NotValue:something}", false)]
        [TestCase("{<ValueKeywordPlaceholder>}", true)]
        [TestCase("{<ValueKeywordPlaceholder>:modifier}", true, "modifier")]
        [TestCase("{<ValueKeywordPlaceholder>:modifierWithNumbers1234}", true, "modifierWithNumbers1234")]
        [TestCase("{<ValueKeywordPlaceholder>:modifier_WithUnderscores}", true, "modifier_WithUnderscores")]
        public void ValuesAreCorrectlyParsedFromRegex(string input, bool match, string expectedModifierGroup = "")
        {
            input = input.Replace("<ValueKeywordPlaceholder>", FieldFactory.ValueKeyword);
            var regexMatch = FieldFactory.ValueRegex.Match(input);
            Assert.Multiple(() =>
            {
                Assert.That(regexMatch.Success, Is.EqualTo(match));
                if (regexMatch.Success)
                {
                    Assert.That(regexMatch.Groups[FieldFactory.ModifierGroup].Value, Is.EqualTo(expectedModifierGroup));
                }
            });
        }
    }
}
