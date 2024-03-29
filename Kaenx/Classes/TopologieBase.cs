﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes
{
    public interface TopologieBase
    {
        int Id { get; set; }
        string Name { get; set; }
        string LineName { get; }
        Symbol Icon { get; set; }
        TopologieType Type { get; set; }

    }

    public enum TopologieType
    {
        Line,
        LineMiddle,
        Device
    }
}
