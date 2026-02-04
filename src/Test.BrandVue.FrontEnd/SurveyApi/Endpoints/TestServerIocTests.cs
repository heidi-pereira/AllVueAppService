using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using BrandVue.Services;
using BrandVue.Services.Interfaces;
using BrandVue.Settings;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.Reflection;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.FeatureFlags;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    /// <summary>
    /// Along with the IOC tests ensuring we don't bind extra stuff in test, these give us reasonable confidence IOC will work for live
    /// </summary>
    public class TestServerIocTests
    {
        private static readonly ILifetimeScope LifetimeScope = new BrandVueTestServer(iocConfig: new ConstructionOnlyIocConfig(
            new AppSettings(),
            NullLoggerFactory.Instance,
            Substitute.For<IOptions<MixPanelSettings>>(),
            Substitute.For<IOptions<ProductSettings>>())).Container;

        private static HashSet<string> _fullyQualifiedNames;

        [Test]
        public void ResolveEveryRegisteredService()
        {
            var registeredTypes = LifetimeScope.ComponentRegistry.Registrations
                .SelectMany(r => r.Services.OfType<IServiceWithType>(), (_, s) => s.ServiceType)
                .ToList();

            var failures = new List<(Type ServiceType, string ErrorMessage)>();

            foreach (var serviceType in registeredTypes)
            {
                try
                {
                    var resolved = Resolve(serviceType);
                    if (resolved == null)
                    {
                        failures.Add((serviceType, "Resolved to null"));
                    }
                }
                catch (Exception ex)
                {
                    failures.Add((serviceType, ex.Message));
                }
            }

            if (failures.Any())
            {
                foreach (var failure in failures)
                {
                    Console.WriteLine($"Failed to resolve {failure.ServiceType.FullName}: {failure.ErrorMessage}");
                }
            }

            Assert.That(failures, Is.Empty, "Some services failed to resolve. Check the console output for details.");
        }

        /// Stateful types have a dictionary that needs to be shared between requests (stateful cache)
        [TestCaseSource(nameof(GetStatefulTypesToCheckBinding))]
        public void TypesWithStateShouldBeCached(StatefulType statefulType)
        {
            // Arrange & Act
            object resolved1 = ResolveFromNewScope(LifetimeScope, statefulType.ServiceType);
            object resolved2 = ResolveFromNewScope(LifetimeScope, statefulType.ServiceType);

            // If impl type isn't the expected implementation type, look in its fields in case it's a decorator
            resolved1 = GetContainedInstanceOfType(resolved1, statefulType.ImplementationType);
            resolved2 = GetContainedInstanceOfType(resolved2, statefulType.ImplementationType);

            // Assert
            Assert.That(resolved1, Is.SameAs(resolved2),
                $"""
                 {statefulType} of different lifetime scopes should be reference equal.
                 Field names: {string.Join(", ", statefulType.FieldNames)}
                 This type appears to be stateful because its fields can be changed. 
                 Either make it not stateful or bind it so it can be shared across multiple requests.
                 """
            );
        }

        public static IEnumerable<StatefulType> GetStatefulTypesToCheckBinding() =>
            GetStatefulTypes()
                // Exclude BrandVueDataLoader which is a special IoC root (for now)
                .Where(x => x.ServiceType.Name != nameof(BrandVueDataLoader));

        /// <summary>
        /// Assumption: If there's a field implementing IEnumerable then that class is stateful unless IoC container is already managing that state
        /// </summary>
        /// <returns>
        /// All stateful types that are registered in the container
        /// </returns>
        private static IEnumerable<StatefulType> GetStatefulTypes()
        {
            var registeredTypes = LifetimeScope.ComponentRegistry.Registrations
                .SelectMany(r => r.Services.OfType<IServiceWithType>())
                .SelectMany(s => s.ServiceType.GetTypesWithInterface(), (s, t) => (s.ServiceType, ImplementationType: t))
                .Where(t => !t.ImplementationType.Name.StartsWith("Test"))
                .ToArray();

            _fullyQualifiedNames = registeredTypes.Select(r => r.ServiceType.FullName).ToHashSet();

            foreach (var type in registeredTypes)
            {
                var fieldInfos = type.ImplementationType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var statefulFieldInfos = fieldInfos.Where(f => IsKnownMutableType(f.FieldType) || !f.IsInitOnly).ToArray();
                bool hasState = statefulFieldInfos.Any();

                if (hasState)
                {
                    yield return new StatefulType(type.ServiceType, type.ImplementationType, statefulFieldInfos.Select(f => f.Name).ToArray());
                }
            }
        }
        /// <summary>Recurse looking for something of the right type in fields, exiting if seeing the same type</summary>
        private object GetContainedInstanceOfType(object parentInstance, Type requiredType, HashSet<Type> typesSeen = null)
        {
            typesSeen ??= [];
            if (parentInstance == null || !typesSeen.Add(parentInstance.GetType())) return null;
            if (requiredType.IsAssignableFrom(parentInstance.GetType())) return parentInstance;

            return parentInstance.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(f => GetContainedInstanceOfType(f.GetValue(parentInstance), requiredType, typesSeen))
                .FirstOrDefault(instance => instance != null);
        }

        private static object Resolve(Type type)
        {
            try
            {
                return LifetimeScope.Resolve(type);
            }
            catch (DependencyResolutionException e)
            {
                for (Exception c = e; c is not null; c = c.InnerException)
                {
                    Console.WriteLine(c is DependencyResolutionException ? c.Message : c);
                }
                Console.WriteLine(Environment.NewLine);
                return null;
            }
        }

        public record StatefulType(Type ServiceType, Type ImplementationType, string[] FieldNames)
        {
            public override string ToString() => $"{ImplementationType.Name} ({ServiceType.Name})";
        }

        private static bool IsKnownMutableType(Type type)
        {
            return type.IsAssignableTo<IEnumerable>()
                   && type != typeof(string)
                   && !type.Name.StartsWith("IReadOnly") && !type.Name.StartsWith("IImmutable")
                   && !_fullyQualifiedNames.Contains(type.FullName);
        }

        private static object ResolveFromNewScope(ILifetimeScope rootScope, Type type)
        {
            using var scope = rootScope.BeginLifetimeScope();
            return scope.Resolve(type);
        }

        public class ConstructionOnlyIocConfig : IoCConfig
        {
            private readonly IAnswerDbContextFactory _contextFactory;
            public ConstructionOnlyIocConfig(
                AppSettings appSettings,
                ILoggerFactory loggerFactory,
                IOptions<MixPanelSettings> mixPanelSettings,
                IOptions<ProductSettings> productSettings,
                IAnswerDbContextFactory contextFactory = null)
                : base(appSettings, loggerFactory, mixPanelSettings, productSettings, new ConfigurationManager())
            {
                _contextFactory = contextFactory ?? new TestChoiceSetReaderFactory();
            }

            protected override void RegisterAppDependencies(ContainerBuilder builder)
            {
                base.RegisterAppDependencies(builder);
                var requestScopeAccessor = Substitute.For<IRequestScopeAccessor>();
                requestScopeAccessor.RequestScope.Returns(new RequestScope("barometer", null, "savanta", RequestResource.InternalApi));
                builder.RegisterInstance(requestScopeAccessor).As<IRequestScopeAccessor>();
                builder.RegisterInstance(Substitute.For<IUiBrandVueDataLoader, IBrandVueDataLoader>()).As<IUiBrandVueDataLoader, IBrandVueDataLoader>().SingleInstance();
                var brandVueDataLoaderSettings = Substitute.For<IBrandVueDataLoaderSettings>();
                brandVueDataLoaderSettings.MaxConcurrentDataLoaders.Returns(100);
                builder.RegisterInstance(brandVueDataLoaderSettings).As<IBrandVueDataLoaderSettings>();
                builder.RegisterInstance(Substitute.For<IEagerlyLoadable<IBrandVueDataLoader>>()).As<IEagerlyLoadable<IBrandVueDataLoader>>().SingleInstance();
                
                builder.RegisterInstance(Substitute.For<IFeatureToggleService>()).As<IFeatureToggleService>();
                var permissionService = Substitute.For<IPermissionService>();
                permissionService.GetAllUserFeaturePermissionsAsync(Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Task.FromResult<IReadOnlyCollection<IPermissionFeatureOptionWithCode>>(new List<IPermissionFeatureOptionWithCode>()));
                builder.RegisterInstance(permissionService).As<IPermissionService>();

                builder.Register(context =>
                {
                    var requestScope = context.Resolve<RequestScope>();
                    var provider = new ProductContextProvider(AppSettings, _contextFactory, _loggerFactory.CreateLogger<ProductContextProvider>());
                    return provider.ProvideProductContext(requestScope);
                }).As<IProductContext>().InstancePerLifetimeScope();
            }
        }
    }
}