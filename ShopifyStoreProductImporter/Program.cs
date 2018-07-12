using Microsoft.Win32;
using Newtonsoft.Json;
using ShopifySharp;
using ShopifySharp.Enums;
using ShopifySharp.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyStoreProductImporter
{
    class Program
    {
        private static string _myShopifyUrl;
        private static string _shopAccessToken;
        
        private static string _connectionString;
        
        private static DateTime _startedAt;
        private static DateTime _endedAt;

        static void Main(string[] args)
        {
            ShopifyService.SetGlobalExecutionPolicy(new SmartRetryExecutionPolicy());

            Authenticate();
            ImportProducts();
            
            Console.ReadKey();
        } 

        private static void Authenticate()
        {
            while (true)
            {
                _myShopifyUrl = string.Empty;
                _shopAccessToken = string.Empty;

                Console.Write("Shopify Store Url:\t\thttps://{0}", _myShopifyUrl);
                _myShopifyUrl = Console.ReadLine();
                _myShopifyUrl = "https://" + _myShopifyUrl;

                Console.Write("Enter shopify access token:\t");
                _shopAccessToken = Console.ReadLine();

                var shopService = new ShopService(_myShopifyUrl, _shopAccessToken);
                try
                {
                    Console.Write("Connnecting to {0}...", _myShopifyUrl);
                    shopService.GetAsync().Wait();
                    Console.WriteLine(" CONNECTED!");
                    break;
                }
                catch (AggregateException ex)
                {
                    Console.WriteLine(" FAILED!");
                    FailAndRetry(ex);
                }
            }
            
            while (true)
            {
                string dbServer, dbName, dbUsername, dbPassword = string.Empty;

                try
                {
                    Console.WriteLine();
                    Console.Write("Enter server address [(local)]:\t");
                    dbServer = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(dbServer) || string.IsNullOrEmpty(dbServer))
                    {
                        dbServer = "(local)";
                    }

                    Console.Write("Enter database name:\t\t");
                    dbName = Console.ReadLine();

                    Console.Write("Enter user name:\t\t");
                    dbUsername = Console.ReadLine();

                    Console.Write("Enter password:\t\t\t");
                    ConsoleKeyInfo key;

                    do
                    {
                        key = Console.ReadKey(true);
                        if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                        {
                            dbPassword += key.KeyChar;
                            Console.Write("*");
                        }
                        else
                        {
                            if (key.Key == ConsoleKey.Backspace && dbPassword.Length > 0)
                            {
                                dbPassword = dbPassword.Substring(0, (dbPassword.Length - 1));
                                Console.Write("\b \b");
                            }
                        }
                    }
                    while (key.Key != ConsoleKey.Enter);

                    Console.WriteLine();
                    Console.Write("Connnecting to the database...");
                    _connectionString = GetConnectionString(dbServer, dbName, dbUsername, dbPassword);
                    using (var c = new ServerConnection(_connectionString))
                    {
                        Console.WriteLine(" CONNECTED!");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" FAILED!");
                    Console.WriteLine("xxxxxxx An error occured:");
                    Console.WriteLine("- {0}", ex.GetBaseException().Message);
                    Console.WriteLine("xxxxxxx Please try again.");
                    Console.WriteLine();
                }
            }

            Console.WriteLine();
        }

        private static void ImportProducts()
        {
            _startedAt = DateTime.Now;

            string willPublishProductsInput = null;
            bool willPublishProduct = false ;

            while (true)
            {
                try
                {
                    var shopService = new ShopService2(_myShopifyUrl, _shopAccessToken);
                    WriteInfo("Getting shop info...");
                    // HACK: API CALL
                    var shop = shopService.GetAsync().Result;
                    WriteInfoWithTime("DONE Getting shop info");

                    var products = GetProductsAsync().Result;
                    var productsCount = products.Count();

                    if (willPublishProductsInput == null && productsCount > 0)
                    {
                        Console.WriteLine();
                        Console.Write("Publish all {0} product(s)? (y/n) [n]: ", productsCount);
                        willPublishProductsInput = Console.In.ReadLine();
                        willPublishProduct = (willPublishProductsInput ?? "").ToLower() == "y";

                        WriteInfoWithTime("All {0} product(s) will{1} be published", 
                            productsCount, willPublishProduct ? "" : " NOT");
                    }

                    Console.WriteLine();

                    ImportNewCategoriesAsync().Wait();
                    ImportNewProductsAsync(shop, products, willPublishProduct).Wait();
                    
                    break;
                }
                catch (AggregateException ex)
                {
                    FailAndRetry(ex, 15);
                }
            }

            _endedAt = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("*************** FINISHED ***************");
            Console.WriteLine("Started at: {0}", _startedAt);
            Console.WriteLine("Ended at: {0}", _endedAt);
            Console.WriteLine("Total duration: {0}", _endedAt - _startedAt);
            Console.WriteLine("****************************************");
            Console.Write("Press any key to exit...");
        }

        private async static Task ImportNewCategoriesAsync()
        {
            var collectionDictionary = new Dictionary<string, long>();
            var service = new SmartCollectionService(_myShopifyUrl, _shopAccessToken);

            WriteInfo("Getting smart collections from Shopify store...");

            var collectionsCount = await service.CountAsync();
            var batchCount = Math.Ceiling(collectionsCount / 250d);
            WriteInfoWithTime("with {0} batch(es)", batchCount);
            for (int i = 0; i < batchCount; i++)
            {
                // HACK: API CALL
                var collections = await service.ListAsync(new SmartCollectionFilter { Limit = 250, Page = i + 1, Fields = "id,title" });
                foreach (var collection in collections)
                {
                    collectionDictionary.Add(collection.Title, collection.Id.Value);
                }

                WriteInfoWithTime("- Got {0} collection(s) in batch {1}", collections.Count(), i + 1);
            }

            WriteInfoWithTime("DONE Getting smart collections from Shopify store");

            Console.WriteLine();
            WriteInfo("Inserting new smart collections...");

            using (var c = new ServerConnection(_connectionString))
            {
                var distinctCommand = c.SetStatement(@"SELECT DISTINCT [Category] FROM Products p
                                            LEFT OUTER JOIN SeparatedTable st ON p.Id = st.Id
                                            WHERE st.Id IS NULL;");
                
                var dataReader = await distinctCommand.ExecuteReaderAsync();
                int newCount = 0;
                while (await dataReader.ReadAsync())
                {
                    var category = dataReader.GetValueOrDefault<string>("Category");
                    if (!collectionDictionary.ContainsKey(category))
                    {
                        newCount++;

                        var rules = category.Split('/').Select(cat => new SmartCollectionRules
                        {
                            Column = "tag",
                            Relation = "equals",
                            Condition = cat
                        });
                        // HACK: API CALL
                        await service.CreateAsync(new SmartCollection
                        {
                            Title = category,
                            Disjunctive = false,
                            Rules = rules
                        });

                        WriteInfoWithTime("✓ {0}. Added smart collection: '{1}'", newCount, category);
                    }
                }

                WriteInfoWithTime("DONE Inserting new collections");
            }
        }

        private static async Task ImportNewProductsAsync(Shop2 shop, List<Models.Product> products, bool willPublishProducts)
        {
            Console.WriteLine();
            WriteInfo("Importing products to {0}...", _myShopifyUrl);

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var productVariant = new ProductVariant
                {
                    SKU = product.Sku,
                    Title = product.Title,
                    Price = product.Price,
                    InventoryManagement = "shopify",
                    CompareAtPrice = product.PriceBeforeSale
                };

                Console.WriteLine("{0}/{1}\t\t{2} ({3})", i + 1, products.Count, product.Title, product.Sku);

                Product existingProduct = null;
                var productService = new ProductService(_myShopifyUrl, _shopAccessToken);

                if (i == 0)
                {
                    // HACK: API CALL 
                    var existingProducts = await productService.ListAsync(new ProductFilter
                    {
                        Title = product.Title,
                        Limit = 1
                    });
                    existingProduct = existingProducts.FirstOrDefault();
                }

                if (existingProduct == null)
                {
                    var productImages = new List<ProductImage>
                    {
                        new ProductImage
                        {
                            Attachment = product.Image
                        }
                    };

                    existingProduct = new Product
                    {
                        Title = product.Title,
                        BodyHtml = string.Format("<strong>{0}</strong>", product.Description),
                        ProductType = "",
                        Images = productImages,
                        Tags = product.Category.Replace("/", ","),
                        Variants = new List<ProductVariant>
                        {
                            productVariant
                        }
                    };
                    // HACK: API CALL
                    existingProduct = await productService
                        .CreateAsync(existingProduct, new ProductCreateOptions { Published = willPublishProducts });

                    WriteInfoWithTime("✓ Product added");
                }
                else
                {
                    WriteInfoWithTime("- Product ALREADY added");
                }

                productVariant = existingProduct.Variants.First();

                var inventoryLevelService = new InventoryLevelService(_myShopifyUrl, _shopAccessToken);
                var inventoryLevel = new InventoryLevel
                {
                    InventoryItemId = productVariant.InventoryItemId,
                    LocationId = shop.PrimaryLocationId,
                    Available = product.Quantity,
                };
                // HACK: API CALL
                await inventoryLevelService.SetAsync(inventoryLevel);
                WriteInfoWithTime("✓ Inventory label has been set");

                using (var c = new ServerConnection(_connectionString))
                {
                    var command = c.SetStatement(@"INSERT INTO [SeparatedTable](Id) VALUES (@id)");

                    command.AddParameter("@id", System.Data.SqlDbType.Int, product.Id);

                    await command.ExecuteNonQueryAsync();
                    WriteInfoWithTime("✓ Added to database");
                }
                Console.WriteLine();
            }

            WriteInfoWithTime("DONE Importing products to {0}", _myShopifyUrl);
        }

        private static async Task<List<Models.Product>> GetProductsAsync()
        {
            Console.WriteLine();
            WriteInfo("Getting products from database...");

            var products = new List<Models.Product>();
            using (var c = new ServerConnection(_connectionString))
            {
                var command = c.SetStatement(@"SELECT * FROM Products p
                                LEFT OUTER JOIN SeparatedTable st ON p.Id = st.Id
                                WHERE st.Id IS NULL
                                ORDER BY p.Id;");

                var dataReader = await command.ExecuteReaderAsync();
                while (await dataReader.ReadAsync())
                {
                    var product = new Models.Product
                    {
                        Id = dataReader.GetValueOrDefault<int>("Id"),
                        Title = dataReader.GetValueOrDefault<string>("Title"),
                        Description = dataReader.GetValueOrDefault<string>("Description"),
                        Sku = dataReader.GetValueOrDefault<string>("SKU"),
                        Quantity = dataReader.GetValueOrDefault<int>("Qty"),
                        Price = dataReader.GetValueOrDefault<decimal>("Price"),
                        Image = dataReader.GetValueOrDefault<string>("Image"),
                        PriceBeforeSale = dataReader.GetValueOrDefault<decimal>("PriceBeforeSale"),
                        Category = dataReader.GetValueOrDefault<string>("Category"),
                    };

                    products.Add(product);
                }
            }

            WriteInfoWithTime("DONE Getting products from database");

            return products;
        }

        private static void WriteInfo(string message, params object[] args)
        {
            Console.WriteLine(message.PadLeft(6, ' ').PadLeft(10, '*'), args);
        }

        private static void WriteInfoWithTime(string message, params object[] args)
        {
            Console.Write(string.Format("[{0}]", GetCurrentDurationText()).PadRight(16));
            Console.WriteLine(message, args);
        }

        private static TimeSpan GetCurrentDuration()
        {
            return DateTime.Now - _startedAt;
        }

        private static string GetCurrentDurationText()
        {
            return RoundTime(DateTime.Now - _startedAt);
        }

        private static string GetConnectionString(string dbServer, string dbName, string dbUsername = "", string dbPassword = "")
        {
            if (string.IsNullOrEmpty(dbUsername) && string.IsNullOrEmpty(dbPassword))
            {
                return string.Format("server={0};database={1};Persist Security Info=False;Trusted_Connection=True;",
                    dbServer, dbName);
            }
            else
            {
                return string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};",
                    dbServer, dbName, dbUsername, dbPassword);
            }
        }

        private static string RoundTime(TimeSpan timeSpan)
        {
            return string.Format("{0:00}:{1:00}:{2:00}.{3:000}",
              timeSpan.TotalHours, timeSpan.Minutes,
              timeSpan.Seconds, timeSpan.Milliseconds);
        }

        private static void FailAndRetry(AggregateException exception, uint retryDelaySec = 0)
        {
            Console.WriteLine();
            Console.WriteLine("xxxxxxx One or more errors occured:");
            foreach (var ex in exception.Flatten().InnerExceptions)
            {
                Console.WriteLine("--- {0}", ex.GetBaseException().Message);
            }

            if (retryDelaySec == 0)
            {
                Console.WriteLine("xxxxxxx Please try again.");
            }
            else
            {
                Console.Write("xxxxxxx Retrying in {0} seconds", retryDelaySec);

                for (int i = 0; i < retryDelaySec; i++)
                {
                    Task.Delay(1000).Wait();
                    if (i < 9)
                    {
                        Console.Write(".");
                    }
                    else
                    {
                        Console.Write(15 - 1 - i);
                    }
                }
            }
            Console.WriteLine("\n");
        }
    }
}