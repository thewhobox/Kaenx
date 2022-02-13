using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.MVVM
{
    internal class ChangeAppModel
    {
        public ApplicationViewModel Application;
        public string Name
        {
            get
            {
                return Application.Name + " " + Application.VersionString + Environment.NewLine + Application.Id;
            }
        }

        public ChangeAppModel(ApplicationViewModel model)
        {
            Application = model;
        }
    }
}
