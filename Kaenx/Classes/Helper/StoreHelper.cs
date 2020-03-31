using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Kaenx.Classes.Helper
{
    public class StoreHelper
    {
        public bool DeviceDeActivate { get; set; }
        public bool LoadDeviceConfig { get; set; }

        private StoreContext c = StoreContext.GetDefault();


        public async Task Load()
        {
            
            string[] productKinds = { "Durable" };
            List<String> filterList = new List<string>(productKinds);
            StoreProductQueryResult queryResult = await c.GetAssociatedStoreProductsAsync(filterList);

            foreach (KeyValuePair<string, StoreProduct> item in queryResult.Products)
            {
                StoreProduct product = item.Value;

                //StorePurchaseResult result = await c.RequestPurchaseAsync(product.StoreId);
                switch (product.StoreId)
                {
                    case "9PB1ZQ9903WT":
                        LoadDeviceConfig = product.IsInUserCollection;
                        break;

                    case "9PJ8W7H9QSR3":
                        DeviceDeActivate = product.IsInUserCollection;
                        break;
                }
            }
        }
    }
}
