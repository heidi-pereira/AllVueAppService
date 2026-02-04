using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using UserManagement.BackEnd.Models;
using Product = UserManagement.BackEnd.Models.Product;

namespace UserManagement.BackEnd.Services
{
    public class ProductsService : IProductsService
    {
        public const string AuthProductIdFor_SurveyVueEditor = "surveyeditor-editing";
        public const string AuthProductIdFor_SurveyVueFeedback = "surveyeditor";
        private IList<SurveyGroup>? _brandVueSurveyGroups;

        private readonly ISurveyGroupService _surveyGroupService;

        public ProductsService(ISurveyGroupService surveyGroupService)
        {
            _surveyGroupService = surveyGroupService ?? throw new ArgumentNullException(nameof(surveyGroupService));
        }

        public ICollection<string> GetAuthProductIds(List<ProductIdentifier> products, bool surveyVueEditingAvailable,
            bool surveyVueFeedbackAvailable)
        {
            var productIds = new List<string>();
            if (surveyVueEditingAvailable)
            {
                productIds.Add(AuthProductIdFor_SurveyVueEditor);
            }
            if (surveyVueFeedbackAvailable)
            {
                productIds.Add(AuthProductIdFor_SurveyVueFeedback);
            }

            var brandVues = GetBrandVueSurveyGroupsLazy();
            foreach (var productIdentifier in products)
            {
                var brandVue = brandVues.FirstOrDefault(x => x.SurveyGroupId == productIdentifier.Id);
                if (brandVue != null)
                {
                    productIds.Add(brandVue.Name);
                }
            }
            return productIds;
        }
        private IList<SurveyGroup> GetBrandVueSurveyGroupsLazy()
        {
            if (_brandVueSurveyGroups == null)
            {
                _brandVueSurveyGroups = _surveyGroupService.GetBrandVueSurveyGroups();
            }
            return _brandVueSurveyGroups;
        }
        public List<Product> GetProducts()
        {
            return GetBrandVueSurveyGroupsLazy().Select(x =>
                new Product(
                    new ProductIdentifier(ProjectType.BrandVue, x.SurveyGroupId),
                    ToPossiblyUpperFirstLetter(x.Name), x.UrlSafeName)).ToList();
        }

        private static string ToPossiblyUpperFirstLetter(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            return char.ToUpper(name[0]) + name[1..];
        }

        public List<ProductIdentifier> ToProducts(List<string> productCodes)
        {
            return ToProducts(productCodes, GetBrandVueSurveyGroupsLazy());
        }
        private List<ProductIdentifier> ToProducts(List<string> productCodes, IList<SurveyGroup> groups)
        {
            var products = new List<ProductIdentifier>();
            foreach (string productCode in productCodes)
            {
                var surveyGroup = groups.FirstOrDefault(x => x.Name == productCode);
                if (surveyGroup != null)
                {
                    products.Add(new ProductIdentifier(ProjectType.BrandVue, surveyGroup.SurveyGroupId));
                }
            }
            return products;
        }

        private ProductIdentifier? AuthProductToProductIdentifier(AuthServer.GeneratedAuthApi.UserProductModel product)
        {
            var brandVues = _surveyGroupService.GetBrandVueSurveyGroups();
            var brandVue = brandVues.SingleOrDefault(x => x.Name == product.ShortCode);
            if (brandVue != null)
            {
                return new ProductIdentifier(ProjectType.BrandVue, brandVue.SurveyGroupId);
            }
            return null;
        }

        public List<ProductIdentifier> ToProducts(ICollection<UserProductModel>? products)
        {
            var result = new List<ProductIdentifier>();
            if (products != null)
            {
                foreach (var product in products)
                {
                    var productIdentifier = AuthProductToProductIdentifier(product);
                    if (productIdentifier != null)
                    {
                        result.Add(productIdentifier);
                    }
                }
            }
            return result;
        }


    }
}
