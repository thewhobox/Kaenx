using METS.Knx.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace METS.Classes.Bus.Actions
{
    public class ProgApplication : IBusAction, INotifyPropertyChanged
    {
        private ProgAppType _type;
        private int _state = 0;
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private BusConnection _conn;
        private byte _sequence = 0x00;
        private CancellationToken _token;

        public string Type { get; } = "Geräte Info";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public event EventHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgApplication(ProgAppType type)
        {
            _conn = BusConnection.Instance;
            _conn.OnTunnelResponse += _conn_OnTunnelResponse;
            _type = type;
        }

        private void _conn_OnTunnelResponse(TunnelResponse response)
        {

        }

        public void Run(CancellationToken token)
        {
            _token = token;
            _state = 0;
            TodoText = "Lese Informationen...";
            //StorageFile x = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/img.jpg"));
        }







        private void Changed(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch
            {
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }


        public enum ProgAppType
        {
            Komplett, // 0
            Partiell, // 1
            Minimal // 2
        }
    }
}
