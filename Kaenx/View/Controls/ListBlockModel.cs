﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.View.Controls
{
    public class ListBlockModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public StackPanel Panel { get; set; }
        public Visibility Visible { get; set; }
    }
}