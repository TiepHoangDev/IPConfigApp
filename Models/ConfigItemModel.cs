using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IPConfigApp.Models
{
    public class ConfigItemModel
    {
        public string Name { get; set; }
        public bool IsDhcpEnabled { get; set; }
        public ObservableCollection<Ip4AndSubnetmaskModel> IPAdressList { get; set; }
        public ObservableCollection<Wraper<string>> GatewayList { get; set; }
        public int Type { get; set; }
        public string PhysicalAddress { get; set; }
        public bool IsConnected { get; set; }


        public XCommand ApplyCommand => new XCommand(() => OnExcuteWaiting(Apply));
        public XCommand AddIPAdressCommand => new XCommand(AddIPAdress);
        public XCommand DeleteIPCommand => new XCommand(DeleteIP);
        public XCommand DeleteGatewayCommand => new XCommand(DeleteGateway);
        public XCommand GetByCmdCommand => new XCommand(GetByCmd);
        public XCommand AddGatewayCommand => new XCommand(AddGateway);
        public XCommand EnableCommand => new XCommand(x => OnExcuteWaiting(() => Enable(x)));

        /// <summary>
        /// Wrap method, loading process and reload data, to callback on server
        /// </summary>
        public Action<Action> OnExcuteWaiting { get; set; }

        string nameCmd => $"\"{Name}\"";

        public void Apply()
        {
            if (IsDhcpEnabled)
            {
                var cmd = $"netsh interface ipv4 set address name={nameCmd} source=dhcp";
                ExecuteCommand(cmd, false);
                cmd = $"netsh interface ipv4 set dns name={nameCmd} source=dhcp";
                ExecuteCommand(cmd, false);
                return;
            }


            bool first = true;
            foreach (var item in IPAdressList)
            {
                //- IP4: 
                //netsh interface ipv4 set address name=”Wi-Fi” address=192.168.1.64 mask=255.255.255.0 gateway=192.168.1.1
                //- DNS: 
                //netsh interface ipv4 set dns name=”Wi-Fi” static 8.8.8.8
                //                                          dhcp 
                //netsh int set int name="ethernet" admin=disabled
                //netsh interface set interface name="Wi-fi" admin=ENABLED
                //netsh interface ip set dns name="Wi-fi" static 1.1.1.1

                //set/add IP
                if (first)
                {
                    var gateway = GatewayList.FirstOrDefault()?.Value ?? "none";
                    var cmd = $"netsh interface ipv4 set address name={nameCmd} source=static address={item.IP4} mask={item.SubnetMask} gateway={gateway} gwmetric=0";
                    ExecuteCommand(cmd, false);
                }
                else
                {
                    var cmd = $"netsh interface ipv4 add address name={nameCmd} address={item.IP4} mask={item.SubnetMask}";
                    ExecuteCommand(cmd, false);
                }

                if (first)
                {
                    var cmd = $"netsh interface ip set dns name={nameCmd} source=static address=1.1.1.1";
                    ExecuteCommand(cmd, false);
                    cmd = $"netsh interface ip add dns name={nameCmd} 1.0.0.1 index=2";
                    ExecuteCommand(cmd, false);
                }

                first = false;
            }

            //add gateway
            foreach (var gw in GatewayList.Skip(1))
            {
                var cmd = $"netsh interface ipv4 add address name={nameCmd} gateway={gw.Value} gwmetric=0";
                ExecuteCommand(cmd, false);
            }
        }

        public void Enable(object isEnable)
        {
            bool enable = Convert.ToBoolean(isEnable);
            var value = enable ? "ENABLED" : "DISABLED";
            string command = $"netsh interface set interface name={nameCmd} admin={value}";
            ExecuteCommand(command, false);
        }

        private void ExecuteCommand(string command, bool keepCmd)
        {
            Debug.WriteLine(command);
            string flag = keepCmd ? "/k" : "/C";
            var cmd = Process.Start(new ProcessStartInfo
            {
                CreateNoWindow = !keepCmd,
                Arguments = $"{flag} {command}",
                UseShellExecute = false,
                FileName = "cmd",
            });
            if (!keepCmd)
            {
                cmd.WaitForExit();
            }
        }

        public void GetByCmd()
        {
            string command = $"netsh interface ipv4 show config name={nameCmd}";
            ExecuteCommand(command, true);
        }

        public void AddIPAdress()
        {
            this.IPAdressList.Add(new Ip4AndSubnetmaskModel()
            {
                IP4 = "192.168.1.1",
                SubnetMask = "255.255.255.0"
            });
        }

        public void DeleteIP(object index)
        {
            this.IPAdressList.Remove(index as Ip4AndSubnetmaskModel);
        }

        public void DeleteGateway(object index)
        {
            this.GatewayList.Remove(index as Wraper<string>);
        }

        public void AddGateway()
        {
            this.GatewayList.Add(Wraper<string>.From("192.168.1.1"));
        }
    }

    public class Ip4AndSubnetmaskModel
    {
        public string IP4 { get; set; }
        public string SubnetMask { get; set; }
    }

    public class Wraper<T>
    {
        public T Value { get; set; }

        public Wraper(T value)
        {
            Value = value;
        }

        public static Wraper<T> From(T value) => new Wraper<T>(value);

        public static implicit operator Wraper<T>(T value) => new Wraper<T>(value);
    }
}
