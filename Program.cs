using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Foundation;

namespace NetworkMonitoring
{
    public class Device
    {
        public string ip;
        public string mac;
        public string name;
    }
    class Program
    {
        List<Device> newDevices = new List<Device>();
        public string[] device = new string[3];
        static string NetworkGateway()
        {
            string defaultGateway = "";

            foreach (NetworkInterface network in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (GatewayIPAddressInformation gatewayInfo in network.GetIPProperties().GatewayAddresses)
                {
                    defaultGateway = gatewayInfo.Address.ToString();
                }
            }
            Console.WriteLine(defaultGateway);
            return defaultGateway;
            
        }



        static void Main(string[] args)
        {
            string defaultGateway = NetworkGateway();
            string[] temp = defaultGateway.Split(".");
            var pingAddress = new Program();
            
            for (int i = 2; i < 255; i++)
            {
                string address = temp[0] + "." + temp[1] + "." + temp[2] + "." + i;
                pingAddress.pingAddress(address, 4, 4000);
                
            }
            foreach (Device device in pingAddress.newDevices)
                Console.WriteLine(device.ip);

        }

        public void pingAddress(string host, int attempts, int timeout)
        {
            for (int i = 0; i < attempts; i++)
            {
                new Thread(delegate ()
                {
                    try
                    {
                        System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                        ping.PingCompleted += new PingCompletedEventHandler(Finished);
                        ping.SendAsync(host, timeout, host);
                    }
                    catch
                    {
                    }
                }).Start();
               
            }
            if (checkNew(device[2]) && device[2] != null)
            {
                if (!newDevices.Contains(new Device() { ip = device[0], mac = device[2], name = device[1] }))
                {
                    newDevices.Add(new Device() { ip = device[0], mac = device[2], name = device[1] });
                    StreamWriter writer = new StreamWriter("logs.txt", append: true);
                    writer.WriteLine(device[2]);
                    writer.Close();
                    Console.WriteLine("New Device Connected: " + device[0] + " " + device[1] + " " + device[2]);
                    notificationManager();
                    
                }
            }
            newDevices.TrimExcess();
        }

        private void Finished(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            string hostname = recieveName(ip);
            string macAddress = recieveMAC(ip);

            device[0] = ip;
            device[1] = hostname;
            device[2] = macAddress;
        }

        public string recieveName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (SocketException)
            {
            }

            return null;
        }

        public string recieveMAC(string ip)
        {
            string macAddress = "";
            System.Diagnostics.Process Process = new System.Diagnostics.Process();
            Process.StartInfo.FileName = "arp";
            Process.StartInfo.Arguments = "-a " + ip;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.CreateNoWindow = true;
            Process.Start();
            string[] temp = Process.StandardOutput.ReadToEnd().Split("-");
            if (temp.Length >= 8)
            {
                macAddress = temp[3].Substring(Math.Max(0, temp[3].Length - 2))
                         + "-" + temp[4] + "-" + temp[5] + "-" + temp[6]
                         + "-" + temp[7] + "-"
                         + temp[8].Substring(0, 2);
                return macAddress;
            }

            else
            {
                return "OWN Machine";
            }
        }

        public bool checkNew(string mac)
        {
            bool isOld = false;
            StreamReader reader = new StreamReader("logs.txt");
            while (!reader.EndOfStream && !isOld)
            {
                if(mac == reader.ReadLine())
                {
                    isOld = true;
                }
            }
            reader.Close();
            if (isOld)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void notificationManager()
        {
            string title = "New Device Connected to the Network";
            string content = device[0] + " " + device[1] + " " + device[2];

            string xmlString =
                 $@"<toast><visual>
                <binding template='ToastGeneric'>
                <text>{title}</text>
                <text>{content}</text>
                </binding>
                </visual></toast>";

            XmlDocument toastXml = new XmlDocument();
            toastXml.LoadXml(xmlString);

            ToastNotification toast = new ToastNotification(toastXml);

            ToastNotificationManager.CreateToastNotifier("NewNetworkDeviceManager").Show(toast);
        }

    }
}
