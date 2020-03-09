using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kaenx.Classes.Bus.Actions
{
    public interface IBusAction
    {
        delegate void ActionFinishedHandler(IBusAction action, object data);
        event ActionFinishedHandler Finished;

        Kaenx.Konnect.Connection Connection { get; set; }
        int ProgressValue { get; set; }
        bool ProgressIsIndeterminate { get; set; }
        string TodoText { get; set; }
        string Type { get; }
        LineDevice Device { get; set; }

        void Run(CancellationToken token);
    }
}
