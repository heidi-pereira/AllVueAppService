using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TestCommon;

namespace Test.BrandVue.FrontEnd;

class ArchitectureTests
{
    [Test]
    public void TestAssemblyDoesNotReferenceOtherTestAssemblies()
    {
        CommonAssert.DoesNotReferenceOtherTestProjects();
    }
}
