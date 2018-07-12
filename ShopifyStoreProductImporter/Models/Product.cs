using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyStoreProductImporter.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal PriceBeforeSale { get; set; }
        public string Category { get; set; }
    }
}
