using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace METS.Classes.Helper
{
    public class SettingsHelper
    {
        public static async Task<T> GetSetting<T>(string name)
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("settings_" + name + ".json", CreationCollisionOption.OpenIfExists);
            if (file == null) return default(T);

            string fileText = await Windows.Storage.FileIO.ReadTextAsync(file);
            T output = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(fileText);
            return output;
        }

        public static async Task SetSetting(string name, object value)
        {
            string fileText = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("settings_" + name + ".json", CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(file, fileText);
        }
    }
}
