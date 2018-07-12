# Shopify Store Product Importer
This is **one of the entries** for a **contest in freelancer.com**.

[ShopifySharp](https://github.com/nozzlegear/ShopifySharp)</b> was used to create this console application.

Since the rate of using the Shopify API for *free account* is **2 calls/second**, I'd come up with this solution on how to import the products in the fastest way possible:
1. Get the shop info to get the primary location id. (*used __1 API call__*)
2. Select all distinct categories from the database.
3. Get all smart collections on the Shopify store. (*used __1 API call for every 250 items__*)
4. Split all categories NOT yet imported to the Shopify store, and use these categories for building rules for the new smart collection -- this will automatically assign the upcoming products according to their tags (I assume that the product tag must be equal to all of the split categories). Then, add the new smart collection to the Shopify store. (*used __1 API call for every smart collection added__*)
5. From the database, select all products NOT yet imported to the Shopify store.  
6. For each product item in the retrieved products:  
 a. Assume lost Internet connection during the last import, check the first item if it is already added (if it has the same product title). If it is already added, proceed to sub-step g. (*used __1 API call__*)  
 b. Assign its product variant info to the new product variant.  
 c. Assign its product image info to the new product image.  
 d. Split its product category, and assign them to the tag of the new product.  
 e. Assign the new product image and the new product variant to the new product.  
 f. Add the new product to the Shopify store (*used __1 API call for every product added__*).  
 g. Set the new inventory item and assign the product item quantity, shop primary location id, and product variant inventory item id. (*used __1 API call for every inventory item set__*).  
 h. Insert the added product to the separated table of the database. (I assume this process means that the product is already imported to the Shopify store.)
7. Repeat Step 6 until all products added. All product are automatically assigned to their corresponding smart collection.

***NOT considering internet connection speed***, with *1,000 new products* with *20 new categories*, the number of API calls will be:

Procedure | No. of API Calls
--- | ---:
Getting the shop info | 1
Getting all smart collections | 1
Adding new smart collections | 20
Adding new products | 1,000
Setting all inventory items* | 1,000
**Total** | **2,022**

**Duration: 1,011 seconds**  
All 1,000 products will be imported within roughly *17 minutes* with **2 API calls/sec**.

> \*After August 1st, apps will no longer be able to set inventory using `inventory_quantity` or `inventory_quantity_adjustment`.
> --<cite>[Product Variant](https://help.shopify.com/en/api/reference/products/product_variant). Retrieved 2018-07-12.</cite>  
> \*After August 1, 2018, apps will no longer be able to use the ProductVariant API to adjust inventory. Additionally, fulfillments and refunds with restocks will require a location_id at creation.
> --<cite>[Migrating to multi-location inventory](https://help.shopify.com/en/api/guides/inventory-migration-guide). Retrieved 2018-07-12.</cite>
