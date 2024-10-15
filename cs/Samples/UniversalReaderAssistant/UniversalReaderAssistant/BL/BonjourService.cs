using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;
using Bonjour;
using ThingMagic.URA2.ViewModel;

namespace ThingMagic.URA2.BL
{
    public class BonjourService
    {

        private DNSSDEventManager eventManager;

        public DNSSDEventManager EventManager
        {
            get { return eventManager; }
            set { eventManager = value; }
        }

        private DNSSDService service;

        public DNSSDService Service
        {
            get { return service; }
            set { service = value; }
        }

        private DNSSDService browser;

        public DNSSDService Browser
        {
            get { return browser; }
            set { browser = value; }
        }

        private DNSSDService resolver;

        public DNSSDService Resolver
        {
            get { return resolver; }
            set { resolver = value; }
        }

        private Dictionary<string, string> hostNameIpAddress;

        public Dictionary<string, string> HostNameIpAddress
        {
            get { return hostNameIpAddress; }
            set { hostNameIpAddress = value; }
        }

        private int backgroundNotifierCallbackCount;

        public int BackgroundNotifierCallbackCount
        {
            get { return backgroundNotifierCallbackCount; }
            set { backgroundNotifierCallbackCount = value; }
        }

        private object _backgroundNotifierLock = new object();

        private List<string> servicesList;

        public List<string> ServicesList
        {
            get { return servicesList; }
            set { servicesList = value; }
        }

        private bool isBonjourServicesInstalled;

        public bool IsBonjourServicesInstalled
        {
            get { return isBonjourServicesInstalled; }
            set { isBonjourServicesInstalled = value; }
        }


        public BonjourService()
        {
            try
            {
                eventManager = new DNSSDEventManager();
                hostNameIpAddress = new Dictionary<string, string>();
                servicesList = new List<string>();
                eventManager.ServiceFound += new _IDNSSDEvents_ServiceFoundEventHandler(this.ServiceFound);
                eventManager.ServiceResolved += new _IDNSSDEvents_ServiceResolvedEventHandler(this.ServiceResolved);
                eventManager.ServiceLost += new _IDNSSDEvents_ServiceLostEventHandler(this.ServiceLost);
                service = new DNSSDService();
                isBonjourServicesInstalled = true;
            }
            catch (Exception ex)
            {
                isBonjourServicesInstalled = false;
            }
        }

        public void RebindServices()
        {
            eventManager.ServiceFound += new _IDNSSDEvents_ServiceFoundEventHandler(this.ServiceFound);
            eventManager.ServiceResolved += new _IDNSSDEvents_ServiceResolvedEventHandler(this.ServiceResolved);
        }

        public void UnbindServices()
        {
            eventManager.ServiceFound -= new _IDNSSDEvents_ServiceFoundEventHandler(this.ServiceFound);
            eventManager.ServiceResolved -= new _IDNSSDEvents_ServiceResolvedEventHandler(this.ServiceResolved);
        }

        /// <summary>
        /// ServiceLost
        /// </summary>
        public void ServiceLost(DNSSDService browser, DNSSDFlags flags, uint ifIndex, string serviceName, string regtype, string domain)
        {
            try
            {
                ComboBox cmbFixedReaderAddr = (ComboBox)App.Current.MainWindow.FindName("cmbFixedReaderAddr");
                if (cmbFixedReaderAddr != null)
                {
                    cmbFixedReaderAddr.Items.Clear();
                    cmbFixedReaderAddr.InvalidateVisual();
                }

                servicesList.Clear();

                HostNameIpAddress.Clear();
            }
            catch (Exception)
            {
                isBonjourServicesInstalled = false;
            }
        }

        // ServiceFound
        /// <summary>
        /// This call is invoked by the DNSService core.  We create
        /// a BrowseData object and invoked the appropriate method
        /// in the GUI thread so we can update the UI
        /// </summary>
        /// <param name="sref"></param>
        /// <param name="flags"></param>
        /// <param name="ifIndex"></param>
        /// <param name="serviceName"></param>
        /// <param name="regType"></param>
        /// <param name="domain"></param>
        public void ServiceFound(DNSSDService sref, DNSSDFlags flags, uint ifIndex, String serviceName, String regType, String domain)
        {
            try
            {
                int index = servicesList.IndexOf(serviceName);

                //
                // Check to see if we've seen this service before. If the machine has multiple
                // interfaces, we could potentially get called back multiple times for the
                // same service. Implementing a simple reference counting scheme will address
                // the problem of the same service showing up more than once in the browse list.
                //
                if (index == -1)
                {
                    lock (_backgroundNotifierLock)
                        backgroundNotifierCallbackCount++;
                    BrowseData data = new BrowseData();

                    data.InterfaceIndex = ifIndex;
                    data.Name = serviceName;
                    data.Type = regType;
                    data.Domain = domain;
                    data.Refs = 1;
                    servicesList.Add(serviceName);
                    resolver = service.Resolve(0, data.InterfaceIndex, data.Name, data.Type, data.Domain, eventManager);
                }
                else
                {
                    BrowseData data = new BrowseData();
                    data.InterfaceIndex = ifIndex;
                    data.Name = servicesList[index];
                    data.Name = serviceName;
                    data.Type = regType;
                    data.Domain = domain;
                    resolver = service.Resolve(0, data.InterfaceIndex, data.Name, data.Type, data.Domain, eventManager);
                    data.Refs++;
                }

                TextBlock lblWarning = (TextBlock)App.Current.MainWindow.FindName("lblWarning");
                if (lblWarning != null)
                {
                    lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                    {
                        lblWarning.Background = System.Windows.Media.Brushes.Transparent;
                        lblWarning.Text = "";
                    }));
                }
            }
            catch (Exception)
            {
                isBonjourServicesInstalled = false;
            }
        }

        // BrowseData
        /// <summary>
        /// This class is used to store data associated
        /// with a DNSService.Browse() operation 
        /// </summary>
        public class BrowseData
        {
            public uint InterfaceIndex;
            public String Name;
            public String Type;
            public String Domain;
            public int Refs;

            public override String
            ToString()
            {
                return Name;
            }

            public override bool
            Equals(object other)
            {
                bool result = false;

                if (other != null)
                {
                    result = (this.Name == other.ToString());
                }

                return result;
            }

            public override int
            GetHashCode()
            {
                return Name.GetHashCode();
            }
        };

        // ResolveData
        /// <summary>
        /// This class is used to store data associated
        /// with a DNSService.Resolve() operation
        /// </summary>
        public class ResolveData
        {
            public uint InterfaceIndex;
            public String FullName;
            public String HostName;
            public int Port;
            public TXTRecord TxtRecord;

            public override String
                ToString()
            {
                return FullName;
            }
        };

        /// <summary>
        /// Populate the comports or ip addresses in the combo-box when resolved
        /// </summary>
        /// <param name="sref"></param>
        /// <param name="flags"></param>
        /// <param name="ifIndex"></param>
        /// <param name="fullName"></param>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        /// <param name="txtRecord"></param>
        public void ServiceResolved(DNSSDService sref, DNSSDFlags flags, uint ifIndex, String fullName, String hostName, ushort port, TXTRecord txtRecord)
        {
            try
            {
                //cmbReaderAddr.Items.Add(hostName);
                ComboBox cmbFixedReaderAddr = (ComboBox)App.Current.MainWindow.FindName("cmbFixedReaderAddr");
                ResolveData data = new ResolveData();

                data.InterfaceIndex = ifIndex;
                data.FullName = fullName;
                data.HostName = hostName;
                data.Port = port;
                data.TxtRecord = txtRecord;
                string address = string.Empty;
                uint bits;

                if (txtRecord.ContainsKey("LanIP"))
                {
                    object ip = txtRecord.GetValueForKey("LanIP");
                    bits = BitConverter.ToUInt32((Byte[])ip, 0);
                    address = new System.Net.IPAddress(bits).ToString();
                }
                if ((address == "0.0.0.0") && txtRecord.ContainsKey("WLanIP"))
                {
                    object ip = txtRecord.GetValueForKey("WLanIP");
                    bits = BitConverter.ToUInt32((Byte[])ip, 0);
                    address = new System.Net.IPAddress(bits).ToString();
                }

                //Adding host name
                string[] hostnameArray = hostName.Split('.');

                if (hostnameArray.Length > 0)
                {
                    if (cmbFixedReaderAddr != null)
                    {


                        if (!(HostNameIpAddress.ContainsKey(hostnameArray[0])))
                        {
                            if (!cmbFixedReaderAddr.Items.Contains(hostnameArray[0] + " (" + address + ")"))
                            {
                                cmbFixedReaderAddr.Items.Add(hostnameArray[0] + " (" + address + ")");
                                HostNameIpAddress.Add(hostnameArray[0] + " (" + address + ")", address);

                            }
                        }
                        cmbFixedReaderAddr.SelectedIndex = 0;
                    }
                    else
                    {
                        if (!(HostNameIpAddress.ContainsKey(hostnameArray[0] + " (" + address + ")")))
                        {
                            if (!(ConnectionWizardVM.readerList.Contains(hostnameArray[0] + " (" + address + ")")))
                            {
                                ConnectionWizardVM.readerList.Add(hostnameArray[0] + " (" + address + ")");
                                HostNameIpAddress.Add(hostnameArray[0] + " (" + address + ")", address);
                            }
                        }
                    }
                }

                // Don't forget to stop the resolver. This eases the burden on the network
                //
                if (null != resolver)
                {
                    resolver.Stop();
                    resolver = null;
                }

                lock (_backgroundNotifierLock)
                    backgroundNotifierCallbackCount--;
            }
            catch (Exception)
            {
                isBonjourServicesInstalled = false;
            }
        }
    }
}
