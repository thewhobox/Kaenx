using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Kaenx.Classes.Bus.Actions.IBusAction;

namespace Kaenx.Classes.Bus.Actions
{
    public class ProgPhysicalAddress : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private List<string> progDevices = new List<string>();
        private CancellationToken _token;
        private BusCommon bus;

        public string Type { get; } = "Physikalische Adresse";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgPhysicalAddress()
        {
        }

        List<TunnelResponse> devices = new List<TunnelResponse>();

        private void _conn_OnTunnelResponse(TunnelResponse response)
        {
            devices.Add(response);

            if(response.APCI == Kaenx.Konnect.Parser.ApciTypes.IndividualAddressResponse && response.SourceAddress.Area != 0 && !progDevices.Contains(response.SourceAddress.ToString()))
            {
                progDevices.Add(response.SourceAddress.ToString());
            }
        }

        public void Run(CancellationToken token)
        {
            Connection.OnTunnelResponse += _conn_OnTunnelResponse;
            _token = token;
            bus = new BusCommon(Connection);

            CheckProgMode();
        }

        private async void CheckProgMode()
        {
            TodoText = "Bitte Programmierknopf drücken...";

            ProgressIsIndeterminate = true;
            progDevices.Clear();

            int threeshold = 0;


            //TODO check if PA already taken


            while(!_token.IsCancellationRequested)
            {
                if (progDevices.Count == 1)
                {
                    if(progDevices[0] == Device.LineName)
                    {
                        TodoText = "Gerät hat bereits die Adresse";
                        _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                        {
                            Device.LoadedPA = true;
                        });
                        await Task.Delay(1000);
                        await RestartCommands();
                        TodoText = "Erfolgreich abgeschlossen";
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    else
                    {
                        SetAddress();
                    }
                    break;
                } else if(progDevices.Count > 1)
                {
                    TodoText = "Es befinden sich mehrere Geräte im Programmiermodus";
                    if(threeshold > 5)
                    {
                        await Task.Delay(3000);
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    threeshold++;
                    continue;
                }

                progDevices.Clear();
                bus.IndividualAddressRead();
                await Task.Delay(2000);
            }
        }

        private async void SetAddress()
        {
            TodoText = "Adresse wird programmiert";
            ProgressIsIndeterminate = false;
            ProgressValue = 33;

            bus.IndividualAddressWrite(UnicastAddress.FromString(Device.LineName));
            await Task.Delay(500);

            CheckNewAddr();
        }

        private async void CheckNewAddr()
        {
            TodoText = "Adresse wird geprüft";
            ProgressValue = 66;
            progDevices.Clear();

            while (!_token.IsCancellationRequested)
            {
                if (progDevices.Count == 1)
                {
                    if(progDevices[0] == Device.LineName)
                    {
                        RestartDevice();
                    } else
                    {
                        TodoText = "Adresse konnte nicht programmiert werden";
                        await Task.Delay(3000);
                        Finished?.Invoke(this, new EventArgs());
                        break;
                    }
                    break;
                }
                else if (progDevices.Count > 1)
                {
                    TodoText = "Es befinden sich mehrere Geräte im Programmiermodus"; 
                    await Task.Delay(3000);
                    Finished?.Invoke(this, new EventArgs());
                    break;
                }

                progDevices.Clear();
                bus.IndividualAddressRead();
                await Task.Delay(500);
            }
        }

        private async void RestartDevice()
        {
            ProgressValue = 95;


            await RestartCommands();


            TodoText = "Erfolgreich abgeschlossen";

            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedPA = true;
            });

            Finished?.Invoke(this, null);
        }

        private async Task RestartCommands()
        {
            TodoText = "Gerät wird neu gestartet";
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            dev.Connect();
            await Task.Delay(100);
            string mask = await dev.DeviceDescriptorRead();

            byte[] serial = null;
            try
            {
                serial = await dev.PropertyRead("MV-" + mask, "DeviceSerialNumber");
            } catch {
                try
                {
                    serial = await dev.PropertyRead(0, 11, 6);
                }
                catch { }
            }

            if(serial != null)
            {
                Device.Serial = serial;
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    SaveHelper.UpdateDevice(Device);
                });
            }

            dev.Restart();



            ProgressValue = 100;
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
    }
}
