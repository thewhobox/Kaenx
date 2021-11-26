using Kaenx.Classes.Project;
using Kaenx.DataContext.Migrations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Classes.Save
{
    public interface ISaveHelper
    {
        public void Init(Kaenx.Classes.Project.Project project);
        public void SaveLine();
    }
}
