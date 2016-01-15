using System.Collections.Generic;
using System.Web.Http;
using DSTBuilder.Models;

namespace DSTBuilder.Controllers
{
    public class ProductController : ApiController
    {
        static readonly IProductRepository productRepository = new ProductRepository();

        
    }
}