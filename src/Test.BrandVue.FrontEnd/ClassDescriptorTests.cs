using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.PublicApi.Models;
using BrandVue.PublicApi.Services;
using BrandVue.SourceData.Entity;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd
{
    public class ClassDescriptorTests
    {
        [Test]
        public void ClassDescriptorRepositoryShouldJustReturnBrandWhenNotTreatingSubsetsAsProduct()
        {
            //Arrange
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            responseEntityTypeRepository.TryAdd(EntityType.Profile, EntityType.ProfileType);
            responseEntityTypeRepository.TryAdd(EntityType.Product, new EntityType(EntityType.Product, "Product", "Products"));
            var classDescriptorRepository = new ClassDescriptorRepository(responseEntityTypeRepository);

            //Act
            var classDescriptors = classDescriptorRepository.ValidClassDescriptors();

            //Assert
            var expected = new List<ClassDescriptor>
            {
                new(TestEntityTypeRepository.Brand, Array.Empty<string>()),
                new(TestEntityTypeRepository.Product, Array.Empty<string>()),
            };
            Assert.That(classDescriptors, Is.EqualTo(expected));
        }
    }
}
