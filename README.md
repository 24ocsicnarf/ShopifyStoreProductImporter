# Shopify Store Product Importer
This is one of the entries for a contest in freelancer.com.

[ShopifySharp](https://github.com/nozzlegear/ShopifySharp)</b> was used to create this console application.

Since the rate of using the Shopify API is 2 calls/second (for free account), I'd come up with this solution on how to import the products in the fastest way possible:
1. Get the shop info to get the primary location Id. (*used 1 API call*)
2. Select all distinct categories from the database.
3. Get all smart collections on the Shopify store. (*used 1 API call for every 250 items*)
4. Split all non-existing categories from the Shopify store and use these categories for building rules for a new smart collection and for automation of assigning products to their corresponding collection (I assume that the product tag must be equal to all of the split categories). Then, add that new smart collection to the Shopify store. (used 1 API call for every smart collection added)
5. From the database, select all non-existing products in the Shopify store.
6. For each item in the retrieved products:  
 a. Assign its product variant.  
 b. Assign its product image.  
 c. Split its product category.  
 d. Pass its product variant, product image and the split category to the new product. Then, assign all required info to that product, and add that product to the Shopify store (*used 1 API call for every product added*).  
 e. Set an inventory item by passing the quantity, primary location id, and inventory item id (came from its product variant) (*used 1 API call for every inventory item set*).  
7. Repeat Step 6 until all products added. All product are automatically assigned to their corresponding smart collection.

So, with 1,000 new products with 20 new categories (assume perfect internet connection), the number of API calls will be:

Procedure | No. of API Calls
--- | ---:
Getting the shop info | 1
Getting all smart collections | 1
Adding new smart collections | 20
Adding new products | 1,000
Setting all inventory item* | 1,000
**Total** | **2,022**

**Duration: 1,011 seconds**  
All 1,000 products will be imported within roughly *17 minutes* with **2 API calls/sec**.

> \*After August 1st, apps will no longer be able to set inventory using `inventory_quantity` or `inventory_quantity_adjustment`.
> --<cite>[Product Variant](https://help.shopify.com/en/api/reference/products/product_variant). Retrieved 2018-07-12.</cite>  
> \*After August 1, 2018, apps will no longer be able to use the ProductVariant API to adjust inventory. Additionally, fulfillments and refunds with restocks will require a location_id at creation.
> --<cite>[Migrating to multi-location inventory](https://help.shopify.com/en/api/guides/inventory-migration-guide). Retrieved 2018-07-12.</cite>
