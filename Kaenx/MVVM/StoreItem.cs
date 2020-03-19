using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Kaenx.MVVM
{
    public class StoreItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string Price { get; set; }
        public bool IsInUserCollection { get; set; }

        public StoreItem(StoreProduct product)
        {
            Id = product.StoreId;
            Name = product.Title;
            Description = product.Description;
            Price = product.Price.FormattedPrice;
            IsInUserCollection = product.IsInUserCollection;
            JObject x = JObject.Parse(product.ExtendedJsonData);
            JToken token = x["LocalizedProperties"].SingleOrDefault(t => t["Language"].ToString() == product.Language );
            Image = "https:" + token["Images"].ElementAt(0)["Uri"].ToString();
        }
    }
}
