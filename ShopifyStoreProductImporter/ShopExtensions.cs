using Newtonsoft.Json;
using ShopifySharp;
using ShopifySharp.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShopifyStoreProductImporter
{
    public class Shop2 : Shop
    {
        /// <summary>
        /// The shop's primary location id.
        /// </summary>
        [JsonProperty("primary_location_id")]
        public long? PrimaryLocationId { get; set; }
    }

    public class ShopService2 : ShopService
    {
        /// <summary>
        /// Creates a new instance of <see cref="ShopService" />.
        /// </summary>
        /// <param name="myShopifyUrl">The shop's *.myshopify.com URL.</param>
        /// <param name="shopAccessToken">An API access token for the shop.</param>
        public ShopService2(string myShopifyUrl, string shopAccessToken) : base(myShopifyUrl, shopAccessToken) { }

        /// <summary>
        /// Gets the shop's data.
        /// </summary>
        public new virtual async Task<Shop2> GetAsync()
        {
            var request = PrepareRequest("shop.json");

            return await ExecuteRequestAsync<Shop2>(request, HttpMethod.Get, rootElement: "shop");
        }

        /// <summary>
        /// Forces the shop to uninstall your Shopify app. Uninstalling an application is an irreversible operation. Be entirely sure that you no longer need to make API calls for the shop in which the application has been installed.
        /// </summary>
        public override async Task UninstallAppAsync()
        {
            var request = PrepareRequest("api_permissions/current.json");

            await ExecuteRequestAsync(request, HttpMethod.Delete);
        }
    }
}
