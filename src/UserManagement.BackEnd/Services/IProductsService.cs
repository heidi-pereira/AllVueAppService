using AuthServer.GeneratedAuthApi;
using UserManagement.BackEnd.Models;
using Product = UserManagement.BackEnd.Models.Product;

namespace UserManagement.BackEnd.Services
{
    public interface IProductsService
    {
        public List<Product> GetProducts();
        ICollection<string> GetAuthProductIds(List<ProductIdentifier> products, bool surveyVueEditingAvailable, bool surveyVueFeedbackAvailable);
        public List<ProductIdentifier> ToProducts(List<string> productCodes);
        public List<ProductIdentifier> ToProducts(ICollection<UserProductModel>? products);
    }
}