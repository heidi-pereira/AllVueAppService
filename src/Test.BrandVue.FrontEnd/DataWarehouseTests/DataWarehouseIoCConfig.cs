using System.Security.Claims;
using Autofac;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using BrandVue.PublicApi.Controllers;
using BrandVue.Settings;
using BrandVue.SourceData.CalculationLogging;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vue.Common.App_Start;
using Vue.Common.Constants.Constants;
using Vue.Common.Auth.Permissions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public class DataWarehouseIoCConfig : IoCConfig
    {
        private readonly ProductToTest _productToTest;
        private readonly string _organisation;

        public DataWarehouseIoCConfig(
            AppSettings appSettings,
            ILoggerFactory loggerFactory,
            ProductToTest productToTest,
            string organisation,
            IOptions<MixPanelSettings> mixPanelSettings,
            IOptions<ProductSettings> productSettings) 
            : base(appSettings, loggerFactory, mixPanelSettings, productSettings, new ConfigurationManager())
        {
            _productToTest = productToTest;
            _organisation = organisation;
            
        }

        protected override void RegisterAppDependencies(ContainerBuilder builder)
        {
            base.RegisterAppDependencies(builder);
            var requestScope = new RequestScope(_productToTest.ProductName, _productToTest.SubProduct, _organisation, RequestResource.PublicApi);
            var requestScopeAccessor = Substitute.For<IRequestScopeAccessor>();
            requestScopeAccessor.RequestScope.Returns(requestScope);
            builder.RegisterInstance(requestScopeAccessor).As<IRequestScopeAccessor>();
            builder.RegisterInstance(Substitute.For<IEagerlyLoadable<IBrandVueDataLoader>>()).As<IEagerlyLoadable<IBrandVueDataLoader>>();
            builder.Register(c => requestScope).As<RequestScope>().SingleInstance();
            builder.RegisterType<SurveysetsApiController>().AsSelf();
            builder.RegisterType<ClassesApiController>().AsSelf();
            builder.RegisterType<MetricsApiController>().AsSelf();
            builder.RegisterType<MetricResultsApiController>().AsSelf();
            builder.RegisterGeneric(typeof(RequestUnawareFactoryForTests<>)).As(typeof(IRequestAwareFactory<>));
            builder.RegisterInstance(Substitute.For<ICalculationLogger>()).As<ICalculationLogger>();

            //Mock IUserDataPermissionsOrchestrator to avoid HttpContext issues in tests
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();
            userDataPermissionsOrchestrator.GetDataPermission().Returns((DataPermissionDto?)null);
            builder.RegisterInstance(userDataPermissionsOrchestrator).As<IUserDataPermissionsOrchestrator>();

            //Mock IHttpContextAccessor with claims for the product
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new(RequiredClaims.Products, System.Text.Json.JsonSerializer.Serialize(new[] { _productToTest.ProductName })),
                new(RequiredClaims.Subsets, System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string[]>
                {
                    { _productToTest.ProductName, new[] { Vue.AuthMiddleware.Constants.AllSubsetsForProduct } }
                })),
                new(RequiredClaims.UserId, "test-user"),
                new(RequiredClaims.Role, "Administrator")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            httpContext.User = new ClaimsPrincipal(identity);
            httpContextAccessor.HttpContext.Returns(httpContext);
            builder.RegisterInstance(httpContextAccessor).As<IHttpContextAccessor>();
        }
    }

    public class RequestUnawareFactoryForTests<T>(IComponentContext componentContext) : IRequestAwareFactory<T>
    {
        public T Create() => (T) componentContext.Resolve<T>();
    }
}