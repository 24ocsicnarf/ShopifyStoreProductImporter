# ShopifyStoreProductImporter
This is one of the entries for one of the contests in freelancer.com

Since the rate of using the Shopify API is 2 calls/second, I'd come up with this solution on how to import the products in the fastest way possible:
1. Get the shop info to get the primary location Id. (Used 1 API call)
2. Select all distinct categories from the database.
3. Get all smart collections on the Shopify store. (Used 1 API call for every 250 items)
4. Split all non-existing categories from the Shopify store and use these categories for building rules for a new smart collection and automation of assigning products to their corresponding collection (I assume that the product tag must be equal to all of the split categories). Then, add that new smart collection to the Shopify store. (used 1 API call for every smart collection added)
5. From the database, select all non-existing products in the Shopify store.
6. For each item in the retrieved products:<br />
	6a. Assign its product variant.<br />
	6b. Assign its product image.<br />
	6c. Split its product category.<br />
	6d. Pass its product variant, product image and the split category to the new product. Then, assign all required info to that product, and add that product to the Shopify store. (Used 1 API call for every product added)<br />
	6e. Set an inventory item by passing the quantity, primary location id, and inventory item id (from its product variant). (Used 1 API call for every inventory item set)<br />
7. Repeat 6 until all products added. All product are automatically assigned to their corresponding smart collection.<br /><br />

So, with 1000 new products with 20 new categories (assume perfect internet connection), the number of API calls will be:<br />
Getting the shop info - 1 API call<br />
Getting all smart collections  - 1 API call<br />
Adding new smart collections - 20 API calls<br />
Adding new products - 1,000 API call<br />
Setting all inventory items - 1,000* API call<br />
<br /><br />
Total calls: 2,022 API calls<br />
Duration (2 API calls/sec): 1,011 seconds<br />
All 1000 products will be imported roughly within 17 minutes.<br />

* "After August 1st, apps will no longer be able to set inventory using `inventory_quantity` or `inventory_quantity_adjustment`." (See https://help.shopify.com/en/api/reference/products/product_variant. Retrieved 2018-07-12)
