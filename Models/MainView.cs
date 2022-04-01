using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IPConfigApp.Models
{
    public class MainViewModel : XNotifyPropertyChanged
    {
        private bool isWaiting;

        public ObservableCollection<ConfigItemModel> ListConfigs { get; set; } = new ObservableCollection<ConfigItemModel>();

        public XCommand ReloadCommand => new XCommand(Reload);
        public XCommand OpenControlPanelCommand => new XCommand(OpenControlPanel);
        public XCommand InfoCommand => new XCommand(Info);


        public async Task Reload()
        {
            IsWaiting = true;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            var newdata = adapters.Select(q =>
             {
                 var IPProperties = q.GetIPProperties();

                 var IPAdressList = IPProperties.UnicastAddresses
                 .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                 .Select(ip => new Ip4AndSubnetmaskModel
                 {
                     IP4 = ip.Address.ToString(),
                     SubnetMask = ip.IPv4Mask.ToString(),
                 }).ToList();
                 var GatewayList = IPProperties.GatewayAddresses
                 .Where(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                 .Select(g => new Wraper<string>(g.Address.ToString())).ToList();
                 var IsDhcpEnabled = false;
                 try
                 {
                     IsDhcpEnabled = IPProperties.GetIPv4Properties().IsDhcpEnabled;
                 }
                 catch (Exception)
                 { }
                 return new ConfigItemModel
                 {
                     Name = q.Name,
                     GatewayList = new ObservableCollection<Wraper<string>>(GatewayList),
                     IPAdressList = new ObservableCollection<Ip4AndSubnetmaskModel>(IPAdressList),
                     IsDhcpEnabled = IsDhcpEnabled,
                     PhysicalAddress = q.GetPhysicalAddress().ToString(),
                     IsConnected = q.OperationalStatus == OperationalStatus.Up,
                     Type = 0,
                     OnExcuteWaiting = OnExcuteWaiting
                 };
             }).OrderByDescending(q => q.IsConnected);

            ListConfigs.Clear();
            foreach (var item in newdata)
            {
                ListConfigs.Add(item);
            }

            await Task.Delay(1000);
            IsWaiting = false;
        }

        async void OnExcuteWaiting(Action process)
        {
            try
            {
                IsWaiting = true;
                process.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            await Reload();
            IsWaiting = false;
        }

        public void OpenControlPanel()
        {
            Process.Start("control", "netconnections");
        }

        public bool IsWaiting
        {
            get => isWaiting;
            set => _set(ref isWaiting, value);
        }

        public void Info()
        {
            MessageBox.Show("Contact me at: tiephoang.dev@gmail.com. Copyright 2022.");
        }
    }

    public class XCommand : ICommand
    {
        private Action<object> _action;

        public XCommand(Action<object> action)
        {
            _action = action;
        }

        public XCommand(Func<Task> actionAsync)
        {
            _action = async (x) =>
            {
                await actionAsync?.Invoke();
            };
        }


        public XCommand(Action action)
        {
            _action = x => action();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _action?.Invoke(parameter);

        public static implicit operator XCommand(Action x) => new XCommand(d => x());
    }

    public class XNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void _set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            Debug.WriteLine(propertyName);
            if (field.Equals(value) == false)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
