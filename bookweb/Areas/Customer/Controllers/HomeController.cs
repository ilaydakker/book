using System.Diagnostics;
using BookWeb.Business.Services.IServices;
using BookWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace bookweb.Areas.Customer.Controllers
{ 
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;

        public HomeController(IProductService productService)
        {
            _productService = productService;
        }

        
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync(includeCategory:true);
            return View(products);
        }
        public async Task<IActionResult> Details(int productid)
        {
            var product = await _productService.GetProductByIdAsync(productid, includeCategory: true);
            return View(product);
        }

        public IActionResult Privacy()
        {
            return View(); 
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
