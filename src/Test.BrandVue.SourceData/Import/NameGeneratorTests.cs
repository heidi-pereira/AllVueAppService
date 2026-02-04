using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.Import;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Import
{
    class NameGeneratorTests
    {
        [Test]
        public void ThrowsForCaseInsensitiveComparer()
        {
            var ng = new NameGenerator(Enumerable.Empty<Question>());
            var usedNames = new HashSet<string> { "varcode", "varCode_2" };
            Assert.That(() => ng.GenerateMeasureName(new Question() { VarCode = "varcode" }, usedNames), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CreatesUniqueNames()
        {
            var ng = new NameGenerator(Enumerable.Empty<Question>());
            var usedMeasureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase){ "Var Code", "Var Code_3" };
            var firstGenerated = ng.GenerateMeasureName(new Question() { VarCode = "varCode" }, usedMeasureNames);
            var secondGenerated = ng.GenerateMeasureName(new Question() { VarCode = "Var code" }, usedMeasureNames);
            Assert.Multiple(() =>
            {
                Assert.That(firstGenerated, Is.EqualTo("Var code_2"));
                Assert.That(secondGenerated, Is.EqualTo("Var code_4"));
            });
        }
    }
}
