using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [Route("api/products")]
    [ApiController]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsService _productService;

        public ProductsController(IProductsService productService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        }

        [HttpGet("getproducts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return _productService.GetProducts();
        }
    }
}