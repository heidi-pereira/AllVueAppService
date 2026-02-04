using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData
{
    class ArchitectureTests
    {
        [Test]
        public void TestAssemblyDoesNotReferenceOtherTestAssemblies()
        {
            CommonAssert.DoesNotReferenceOtherTestProjects();
        }
    }
}
