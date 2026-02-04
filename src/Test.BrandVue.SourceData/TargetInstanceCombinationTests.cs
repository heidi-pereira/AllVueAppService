using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    public class TargetInstanceCombinationTests
    {
        [Test]
        public void ShouldReturnSingleEmptySetWhenNoTargetInstances()
        {
            var targetInstances = Array.Empty<TargetInstances>();

            var values = targetInstances.GetEntityValueCombination();

            var expectedEmpty = new[]
            {
                new HashSet<EntityValue>(),
            };

            Assert.That(values, Is.EquivalentTo(expectedEmpty));
        }

        [Test]
        public void ShouldThrowWhenCalledWithProfileTargetInstances()
        {
            var targetInstances = new []
            {
                new TargetInstances(TestEntityTypeRepository.Profile, Array.Empty<EntityInstance>()),
                new TargetInstances(TestEntityTypeRepository.Brand, EntityInstances(new int[] {1, 2, 3}))
            };

            Assert.Throws(typeof(InvalidOperationException), () => targetInstances.GetEntityValueCombination());
        }

        [Test]
        public void ShouldReturnListOfSingleItemHashSetsWhenOnlyOneTargetInstances()
        {
            var targetInstances = new []
            {
                new TargetInstances(TestEntityTypeRepository.Brand, EntityInstances(new int[] {1, 2, 3}))
            };

            var expectedValue = new[]
            {
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                }
            };

            var values = targetInstances.GetEntityValueCombination().ToList();

            Assert.That(values, Is.EquivalentTo(expectedValue));
        }

        [Test]
        public void ShouldReturnCartesianProductOf2TargetInstances()
        {
            var targetInstances = new []
            {
                new TargetInstances(TestEntityTypeRepository.Brand, EntityInstances(new int[] {1, 2, 3})),
                new TargetInstances(TestEntityTypeRepository.Product, EntityInstances(new int[] {4, 5}))
            };

            var expectedValue = new[]
            {
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                }
            };

            var values = targetInstances.GetEntityValueCombination().ToList();

            Assert.That(values, Is.EquivalentTo(expectedValue));
        }

        [Test]
        public void ShouldReturnCartesianProductOf3TargetInstances()
        {
            var targetInstances = new []
            {
                new TargetInstances(TestEntityTypeRepository.Brand, EntityInstances(new int[] {1, 2, 3})),
                new TargetInstances(TestEntityTypeRepository.Product, EntityInstances(new int[] {4, 5})),
                new TargetInstances(TestEntityTypeRepository.Product, EntityInstances(new int[] {6, 7}))
            };

            var expectedValue = new[]
            {
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 4),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 1),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 2),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 6),
                },
                new[]
                {
                    new EntityValue(TestEntityTypeRepository.Brand, 3),
                    new EntityValue(TestEntityTypeRepository.Product, 5),
                    new EntityValue(TestEntityTypeRepository.Product, 7),
                }
            };

            var values = targetInstances.GetEntityValueCombination().ToList();

            Assert.That(values, Is.EquivalentTo(expectedValue));
        }

        private static EntityInstance[] EntityInstances(IEnumerable<int> ids)
        {
            return ids.Select(id => new EntityInstance{Id = id}).ToArray();
        }
    }
}
