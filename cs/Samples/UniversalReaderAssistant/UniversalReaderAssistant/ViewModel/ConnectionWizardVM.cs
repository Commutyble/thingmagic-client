using GalaSoft.MvvmLight;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Windows.Documents;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System;
using System.Management;
using Bonjour;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using ThingMagic;
using ThingMagic.URA2;
using ThingMagic.URA2.Models;
using System.Linq;
using Microsoft.Win32;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Text;
using ThingMagic.URA2.BL;

namespace ThingMagic.URA2.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ConnectionWizardVM : ViewModelBase
    {
        #region Constants

        private const string READERTYPENOTSELECTED = "Please Select a Reader Type.";
        private const string NOSERIALREADERDETECTED = "No Serial Reader Detected. Please make sure that the device is connected to this Computer.";
        private const string NONETWORKREADERDETECTED = "No Network Reader Detected. Please make sure that the device is connected to the network.";
        private const string BONJOURERROR = "Unable to detect network reader. Please make sure Bonjour service is available and running.";
        private const string ADDSERIALREADERMANUALINFO = "Please type in the Serial Reader COM port.\nEx. COM1.";
        private const string ADDNETWORKREADERMANUALINFO = "Please type in  the Network Reader name.\nOR\nPlease enter the IP address where the Network Reader is connected. \nIP Format : xxx.xxx.xxx.xxx";
        private const string ADDCUSTOMREADERMANUALINFO = "Please type in custom reader in given format.";
        private const string NOREADERSELECTED = "No Reader has been Selected. Please select a reader from the list or type in reader port/IP.";
        private const int ANTENNACOUNT = 4;
        private const int PROTOCOLCOUNT = 5;

        #endregion

        #region Properties

        private static int UserControlIndex = 0;
        public static bool IsConfigurationAvailable = false;
        // public static string ConfigFilesdirPath = System.IO.Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ConfigFiles");
        public static string ConfigFilesdirPath = System.IO.Path.Combine(@"C:\URALogs", "ConfigFiles");
        private bool isReadConnect = true;
        private Dictionary<string, string> ReaderListAutoConnect = null;
        private Dictionary<string, string> ReaderListAutoConnectModified = null;
        private List<string> portNames = new List<string>();

        // Bonjour initialization fields

        BonjourService bonjour = null;

        BackgroundWorker bgw = null;
        BackgroundWorker bgwConnect = null;
        bool IsFirmwareUpdateSuccess = false;

        private string firmwareUpdatePath;
        public string FirmwareUpdatePath
        {
            get { return firmwareUpdatePath; }
            set { firmwareUpdatePath = value; RaisePropertyChanged("FirmwareUpdatePath"); }
        }

        private Visibility firmwareUpdateVisibility;
        public Visibility FirmwareUpdateVisibility
        {
            get { return firmwareUpdateVisibility; }
            set
            {
                firmwareUpdateVisibility = value;
                RaisePropertyChanged("FirmwareUpdateVisibility");
                if (value == Visibility.Visible)
                    ConnectionWizardButtonVisibility = Visibility.Collapsed;
                else
                    ConnectionWizardButtonVisibility = Visibility.Visible;
            }
        }

        private string _firmwareUpdateReaderName;
        public string FirmwareUpdateReaderName
        {
            get { return _firmwareUpdateReaderName; }
            set { _firmwareUpdateReaderName = value; RaisePropertyChanged("FirmwareUpdateVisibility"); }
        }


        private Visibility _lastConnectedVisibility;
        public Visibility LastConnectedVisibility
        {
            get { return _lastConnectedVisibility; }
            set
            {
                _lastConnectedVisibility = value;
                RaisePropertyChanged("LastConnectedVisibility");
                if (value == Visibility.Visible)
                    ReaderDetailContent = "Previous Settings";
                else
                    ReaderDetailContent = "Reader Details";
            }
        }

        private Visibility connectionWizardButtonVisibility;
        public Visibility ConnectionWizardButtonVisibility
        {
            get { return connectionWizardButtonVisibility; }
            set { connectionWizardButtonVisibility = value; RaisePropertyChanged("ConnectionWizardButtonVisibility"); }
        }

        #region UHF Protocol
        private bool _gen2ProtocolIsChecked;
        public bool Gen2ProtocolIsChecked
        {
            get { return _gen2ProtocolIsChecked; }
            set { _gen2ProtocolIsChecked = value; RaisePropertyChanged("Gen2ProtocolIsChecked"); NextButtonVisibilitySelectReaderPage(); }
        }

        private Visibility _gen2ProtocolVisbility;
        public Visibility Gen2ProtocolVisbility
        {
            get { return _gen2ProtocolVisbility; }
            set { _gen2ProtocolVisbility = value; RaisePropertyChanged("Gen2ProtocolVisbility"); }
        }

        private bool _iso18000_6bIsChecked;
        public bool ISO18000_6BIsChecked
        {
            get { return _iso18000_6bIsChecked; }
            set { _iso18000_6bIsChecked = value; RaisePropertyChanged("ISO18000_6BIsChecked"); NextButtonVisibilitySelectReaderPage(); }
        }

        private Visibility _iso18000_6bVisbility;
        public Visibility ISO18000_6BVisbility
        {
            get { return _iso18000_6bVisbility; }
            set { _iso18000_6bVisbility = value; RaisePropertyChanged("ISO18000_6BVisbility"); }
        }

        private bool _ipx64IsChecked;
        public bool IPX64IsChecked
        {
            get { return _ipx64IsChecked; }
            set { _ipx64IsChecked = value; RaisePropertyChanged("IPX64IsChecked"); NextButtonVisibilitySelectReaderPage(); }
        }

        private Visibility _ipx64Visbility;
        public Visibility IPX64Visbility
        {
            get { return _ipx64Visbility; }
            set { _ipx64Visbility = value; RaisePropertyChanged("IPX64Visbility"); }
        }

        private bool _ipx256IsChecked;
        public bool IPX256IsChecked
        {
            get { return _ipx256IsChecked; }
            set { _ipx256IsChecked = value; RaisePropertyChanged("IPX256IsChecked"); NextButtonVisibilitySelectReaderPage(); }
        }

        private Visibility _ipx256Visbility;
        public Visibility IPX256Visbility
        {
            get { return _ipx256Visbility; }
            set { _ipx256Visbility = value; RaisePropertyChanged("IPX256Visbility"); }
        }

        private bool _ataIsChecked;
        public bool ATAIsChecked
        {
            get { return _ataIsChecked; }
            set { _ataIsChecked = value; RaisePropertyChanged("ATAIsChecked"); NextButtonVisibilitySelectReaderPage(); }
        }

        private Visibility _ataVisbility;
        public Visibility ATAVisbility
        {
            get { return _ataVisbility; }
            set { _ataVisbility = value; RaisePropertyChanged("ATAVisbility"); }
        }
        #endregion

        #region HF Protocol
        private bool _iso14443aIsChecked;
        public bool ISO14443AIsChecked
        {
            get { return _iso14443aIsChecked; }
            set
            {
                _iso14443aIsChecked = value;
                RaisePropertyChanged("ISO14443AIsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _iso14443aVisbility;
        public Visibility ISO14443AVisbility
        {
            get { return _iso14443aVisbility; }
            set { _iso14443aVisbility = value; RaisePropertyChanged("ISO14443AVisbility"); }
        }

        private bool _iso1444bIsChecked;
        public bool ISO14443BIsChecked
        {
            get { return _iso1444bIsChecked; }
            set
            {
                _iso1444bIsChecked = value;
                RaisePropertyChanged("ISO14443BIsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _iso14443bVisbility;
        public Visibility ISO14443BVisbility
        {
            get { return _iso14443bVisbility; }
            set { _iso14443bVisbility = value; RaisePropertyChanged("ISO14443BVisbility"); }
        }

        private bool _iso15693IsChecked;
        public bool ISO15693IsChecked
        {
            get { return _iso15693IsChecked; }
            set
            {
                _iso15693IsChecked = value;
                RaisePropertyChanged("ISO15693IsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _iso15693Visbility;
        public Visibility ISO15693Visbility
        {
            get { return _iso15693Visbility; }
            set { _iso15693Visbility = value; RaisePropertyChanged("ISO15693Visbility"); }
        }

        private bool _iso18092IsChecked;
        public bool ISO18092IsChecked
        {
            get { return _iso18092IsChecked; }
            set
            {
                _iso18092IsChecked = value;
                RaisePropertyChanged("ISO18092IsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _iso18092Visbility;
        public Visibility ISO18092Visbility
        {
            get { return _iso18092Visbility; }
            set { _iso18092Visbility = value; RaisePropertyChanged("ISO18092Visbility"); }
        }

        private bool _felicaIsChecked;
        public bool FELICAIsChecked
        {
            get { return _felicaIsChecked; }
            set
            {
                _felicaIsChecked = value;
                RaisePropertyChanged("FELICAIsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _felicaVisbility;
        public Visibility FELICAVisbility
        {
            get { return _felicaVisbility; }
            set { _felicaVisbility = value; RaisePropertyChanged("FELICAVisbility"); }
        }

        private bool _iso180003Mode3IsChecked;
        public bool ISO180003Mode3IsChecked
        {
            get { return _iso180003Mode3IsChecked; }
            set
            {
                _iso180003Mode3IsChecked = value;
                RaisePropertyChanged("ISO180003Mode3IsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _iso180003Mode3Visbility;
        public Visibility ISO180003Mode3Visbility
        {
            get { return _iso180003Mode3Visbility; }
            set { _iso180003Mode3Visbility = value; RaisePropertyChanged("ISO180003Mode3Visbility"); }
        }
        #endregion

        #region LF Protocol
        private bool _lf125KHZIsChecked;
        public bool LF125KHZIsChecked
        {
            get { return _lf125KHZIsChecked; }
            set
            {
                _lf125KHZIsChecked = value;
                RaisePropertyChanged("LF125KHZIsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _lf125KHZVisbility;
        public Visibility LF125KHZVisbility
        {
            get { return _lf125KHZVisbility; }
            set { _lf125KHZVisbility = value; RaisePropertyChanged("LF125KHZVisbility"); }
        }

        private bool _lf134KHZIsChecked;
        public bool LF134KHZIsChecked
        {
            get { return _lf134KHZIsChecked; }
            set
            {
                _lf134KHZIsChecked = value;
                RaisePropertyChanged("LF134KHZIsChecked"); NextButtonVisibilitySelectReaderPage();
            }
        }

        private Visibility _lf134KHZVisbility;
        public Visibility LF134KHZVisbility
        {
            get { return _lf134KHZVisbility; }
            set { _lf134KHZVisbility = value; RaisePropertyChanged("LF134KHZVisbility"); }
        }
        #endregion

        private bool _antennaIsChecked1;
        public bool AntennaIsChecked1
        {
            get { return _antennaIsChecked1; }
            set
            {
                _antennaIsChecked1 = value; RaisePropertyChanged("AntennaIsChecked1");
                //if (model.Equals("M3e")&&value==false)
                //{
                //    ISO14443AIsChecked = ISO14443BIsChecked = ISO15693IsChecked = ISO18092IsChecked = FELICAIsChecked = ISO180003Mode3IsChecked = false;
                //}
                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
        }

        private Visibility _antennaVisibility1;
        public Visibility AntennaVisibility1
        {
            get { return _antennaVisibility1; }
            set { _antennaVisibility1 = value; RaisePropertyChanged("AntennaVisibility1"); }
        }

        private bool _antennaIsChecked2;
        public bool AntennaIsChecked2
        {
            get { return _antennaIsChecked2; }
            set
            {
                _antennaIsChecked2 = value; RaisePropertyChanged("AntennaIsChecked2");
                //if (model.Equals("M3e")&&value==false)
                //{
                //    LF125KHZIsChecked = LF134KHZIsChecked = false;
                //}
                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
        }

        private Visibility _antennaVisibility2;
        public Visibility AntennaVisibility2
        {
            get { return _antennaVisibility2; }
            set { _antennaVisibility2 = value; RaisePropertyChanged("AntennaVisibility2"); }
        }

        private bool _antennaIsChecked3;
        public bool AntennaIsChecked3
        {
            get { return _antennaIsChecked3; }
            set
            {
                _antennaIsChecked3 = value; RaisePropertyChanged("AntennaIsChecked3");
                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
        }

        private Visibility _antennaVisibility3;
        public Visibility AntennaVisibility3
        {
            get { return _antennaVisibility3; }
            set { _antennaVisibility3 = value; RaisePropertyChanged("AntennaVisibility3"); }
        }

        private bool _antennaIsChecked4;
        public bool AntennaIsChecked4
        {
            get { return _antennaIsChecked4; }
            set
            {
                _antennaIsChecked4 = value; RaisePropertyChanged("AntennaIsChecked4");

                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
        }

        private Visibility _antennaVisibility4;
        public Visibility AntennaVisibility4
        {
            get { return _antennaVisibility4; }
            set { _antennaVisibility4 = value; RaisePropertyChanged("AntennaVisibility4"); }
        }

        private bool _AntennaDetectionIsEnabled;
        public bool AntennaDetectionIsEnabled
        {
            get { return _AntennaDetectionIsEnabled; }
            set { _AntennaDetectionIsEnabled = value; RaisePropertyChanged("AntennaDetectionIsEnabled"); }
        }

        private bool _AntennaDetectionIsChecked;
        public bool AntennaDetectionIsChecked
        {
            get { return _AntennaDetectionIsChecked; }
            set { _AntennaDetectionIsChecked = value; RaisePropertyChanged("AntennaDetectionIsChecked"); }
        }

        private bool isInvalidAppExceptionRaised;

        public bool IsInvalidAppExceptionRaised
        {
            get { return isInvalidAppExceptionRaised; }
            set { isInvalidAppExceptionRaised = value; RaisePropertyChanged("IsInvalidAppExceptionRaised"); }
        }

        private bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set { isBusy = value; RaisePropertyChanged("IsBusy"); }
        }

        private string busyContent;
        public string BusyContent
        {
            get { return busyContent; }
            set { busyContent = value; RaisePropertyChanged("BusyContent"); }
        }

        private string detectedreadername;
        public string DetectedReaderName
        {
            get { return detectedreadername; }
            set { detectedreadername = value; RaisePropertyChanged("DetectedReaderName"); }
        }

        private string detectedReaderLastConnected;
        public string DetectedReaderLastConnected
        {
            get { return detectedReaderLastConnected; }
            set { detectedReaderLastConnected = value; RaisePropertyChanged("DetectedReaderLastConnected"); }
        }

        private string detectedReaderType;
        public string DetectedReaderType
        {
            get { return detectedReaderType; }
            set { detectedReaderType = value; RaisePropertyChanged("DetectedReaderType"); }
        }

        private string detectedReaderModel;
        public string DetectedReaderModel
        {
            get { return detectedReaderModel; }
            set { detectedReaderModel = value; RaisePropertyChanged("DetectedReaderModel"); }
        }

        private string detectedReaderRegion;
        public string DetectedReaderRegion
        {
            get { return detectedReaderRegion; }
            set { detectedReaderRegion = value; RaisePropertyChanged("DetectedReaderRegion"); }
        }

        private string detectedSelectedAntenna;
        public string DetectedSelectedAntenna
        {
            get { return detectedSelectedAntenna; }
            set { detectedSelectedAntenna = value; RaisePropertyChanged("DetectedSelectedAntenna"); }
        }

        private string detectedReaderProtocol;
        public string DetectedReaderProtocol
        {
            get { return detectedReaderProtocol; }
            set { detectedReaderProtocol = value; RaisePropertyChanged("DetectedReaderProtocol"); }
        }

        private FrameworkElement _contentControlView;
        public FrameworkElement ContentControlView
        {
            get { return _contentControlView; }
            set { _contentControlView = value; RaisePropertyChanged("ContentControlView"); }
        }

        private Visibility _backButtonVisibility;
        public Visibility BackButtonVisibility
        {
            get { return _backButtonVisibility; }
            set { _backButtonVisibility = value; RaisePropertyChanged("BackButtonVisibility"); }
        }

        private Visibility _nextButtonVisibility;
        public Visibility NextButtonVisibility
        {
            get { return _nextButtonVisibility; }
            set { _nextButtonVisibility = value; RaisePropertyChanged("NextButtonVisibility"); }
        }

        private Visibility _connectReadButtonVisibility;
        public Visibility ConnectReadButtonVisibility
        {
            get { return _connectReadButtonVisibility; }
            set { _connectReadButtonVisibility = value; RaisePropertyChanged("ConnectReadButtonVisibility"); }
        }

        private bool _isNextButtonEnabled;
        public bool IsNextButtonEnabled
        {
            get { return _isNextButtonEnabled; }
            set { _isNextButtonEnabled = value; RaisePropertyChanged("IsNextButtonEnabled"); }
        }

        private bool _isConnectionSettingButtonEnabled;
        public bool IsConnectionSettingButtonEnabled
        {
            get { return _isConnectionSettingButtonEnabled; }
            set { _isConnectionSettingButtonEnabled = value; RaisePropertyChanged("IsConnectionSettingButtonEnabled"); }
        }

        private bool _isAdvancedSettingButtonEnabled;
        public bool IsAdvancedSettingButtonEnabled
        {
            get { return _isAdvancedSettingButtonEnabled; }
            set { _isAdvancedSettingButtonEnabled = value; RaisePropertyChanged("IsAdvancedSettingButtonEnabled"); }
        }

        private bool _isSelectReaderButtonEnabled;
        public bool IsSelectReaderButtonEnabled
        {
            get { return _isSelectReaderButtonEnabled; }
            set { _isSelectReaderButtonEnabled = value; RaisePropertyChanged("IsSelectReaderButtonEnabled"); }
        }

        private string _nextConnectButtonContent;
        public string NextConnectButtonContent
        {
            get { return _nextConnectButtonContent; }
            set { _nextConnectButtonContent = value; RaisePropertyChanged("NextConnectButtonContent"); }
        }

        private string _backChangeReaderButtonContent;
        public string BackChangeReaderButtonContent
        {
            get { return _backChangeReaderButtonContent; }
            set { _backChangeReaderButtonContent = value; RaisePropertyChanged("BackChangeReaderButtonContent"); }
        }

        private string _hostAddress;
        public string HostAddress
        {
            get { return _hostAddress; }
            set
            {
                _hostAddress = value; RaisePropertyChanged("NextConnectButtonContent");
                if (IsAddCustomReader)
                    IsNextButtonEnabled = (string.IsNullOrWhiteSpace(HostAddress)) ? false : true;
            }
        }

        private bool _isAddCustomReader;
        public bool IsAddCustomReader
        {
            get { return _isAddCustomReader; }
            set { _isAddCustomReader = value; RaisePropertyChanged("IsAddCustomReader"); IsNextButtonEnabled = (string.IsNullOrWhiteSpace(HostAddress)) ? false : true; }
        }

        private bool _isSerialReader;
        public bool IsSerialReader
        {
            get { return _isSerialReader; }
            set { _isSerialReader = value; RaisePropertyChanged("IsSerialReader"); }
        }

        private bool _isNetworkReader;
        public bool IsNetworkReader
        {
            get { return _isNetworkReader; }
            set { _isNetworkReader = value; RaisePropertyChanged("IsNetworkReader"); }
        }

        private bool _isAddManualChecked;
        public bool IsAddManualChecked
        {
            get { return _isAddManualChecked; }
            set { _isAddManualChecked = value; RaisePropertyChanged("IsAddManualChecked"); }
        }

        private string _statusWarningText;
        public string StatusWarningText
        {
            get { return _statusWarningText; }
            set { _statusWarningText = value; RaisePropertyChanged("StatusWarningText"); }
        }

        private Brush _statusWarningColor;
        public Brush StatusWarningColor
        {
            get { return _statusWarningColor; }
            set { _statusWarningColor = value; RaisePropertyChanged("StatusWarningColor"); }
        }

        private ObservableCollection<string> _readerList;
        public ObservableCollection<string> ReaderList
        {
            get { return _readerList; }
            set { _readerList = value; RaisePropertyChanged("ReaderList"); }
        }

        private string _readerListSelectedItem;
        public string ReaderListSelectedItem
        {
            get { return _readerListSelectedItem; }
            set
            {
                _readerListSelectedItem = value;
                RaisePropertyChanged("ReaderListSelectedItem");
                if ((IsSerialReader || IsNetworkReader) && !IsAddManualChecked)
                {
                    StatusWarningText = "";
                    IsNextButtonEnabled = (string.IsNullOrWhiteSpace(ReaderURI())) ? false : true;
                }
                else
                    IsNextButtonEnabled = (string.IsNullOrWhiteSpace(HostAddress)) ? false : true;
            }
        }

        private string _readerListText;
        public string ReaderListText
        {
            get { return _readerListText; }
            set
            {
                _readerListText = value;
                RaisePropertyChanged("ReaderListText");
                IsNextButtonEnabled = string.IsNullOrWhiteSpace(ReaderListText) ? false : true;
            }
        }


        private string _regionListSelectedItem;
        public string RegionListSelectedItem
        {
            get { return _regionListSelectedItem; }
            set
            {
                _regionListSelectedItem = value;
                if (objReader != null && IsSerialReader && value.ToLower() != "select" && !(model.Equals("M3e")))
                    objReader.ParamSet("/reader/region/id", Enum.Parse(typeof(Reader.Region), value));
                //else if (RegionListSelectedItem.ToLower() == "select")
                //objReader.ParamSet("/reader/region/id", Reader.Region.UNSPEC);
                if (value != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
                RaisePropertyChanged("RegionListSelectedItem");
            }
        }

        private Visibility _regionLabel;

        public Visibility RegionLabel
        {
            get { return _regionLabel; }
            set { _regionLabel = value; RaisePropertyChanged("RegionLabel"); }
        }

        private Visibility _regionDropBox;

        public Visibility RegionDropBox
        {
            get { return _regionDropBox; }
            set { _regionDropBox = value; RaisePropertyChanged("RegionDropBox"); }
        }

        private ObservableCollection<string> _regionList;
        public ObservableCollection<string> RegionList
        {
            get { return _regionList; }
            set { _regionList = value; RaisePropertyChanged("RegionList"); }
        }

        private ObservableCollection<string> _BaudRateComboBoxSource;
        public ObservableCollection<string> BaudRateComboBoxSource
        {
            get { return _BaudRateComboBoxSource; }
            set { _BaudRateComboBoxSource = value; RaisePropertyChanged("BaudRateComboBoxSource"); }
        }

        private string _baudRateSelectedItem;
        public string BaudRateSelectedItem
        {
            get { return _baudRateSelectedItem; }
            set
            {
                _baudRateSelectedItem = value;
                if (objReader != null && IsSerialReader && BaudRateSelectedItem != null)
                    objReader.ParamSet("/reader/baudRate", Int32.Parse(BaudRateSelectedItem));
                RaisePropertyChanged("BaudRateSelectedItem");
            }
        }

        private Visibility _baudRateVisibility;
        public Visibility BaudRateVisibility
        {
            get { return _baudRateVisibility; }
            set { _baudRateVisibility = value; RaisePropertyChanged("BaudRateVisibility"); }
        }

        private string _selectedReaderName;
        public string SelectedReaderName
        {
            get { return _selectedReaderName; }
            set { _selectedReaderName = value; RaisePropertyChanged("SelectedReaderName"); }
        }

        private string _selectedReaderType;
        public string SelectedReaderType
        {
            get { return _selectedReaderType; }
            set { _selectedReaderType = value; RaisePropertyChanged("SelectedReaderType"); }
        }

        private string _readerDetailContent;
        public string ReaderDetailContent
        {
            get { return _readerDetailContent; }
            set { _readerDetailContent = value; RaisePropertyChanged("ReaderDetailContent"); }
        }

        private List<FrameworkElement> _userControlList = null;


        //Reader Properties

        string uri = string.Empty;
        /// <summary>
        /// Define a reader variable
        /// </summary>
        Reader objReader = null;
        string model = null;


        #endregion

        #region Static variable
        public static ObservableCollection<string> readerList = new ObservableCollection<string>();
        #endregion

        #region CommandProperties

        public ICommand CancelCommand { get; private set; }
        public ICommand NextConnectCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand WizardButtonCommand { get; private set; }
        public ICommand ReaderTypeCheckedCommand { get; private set; }
        public ICommand ConnectReadCommand { get; private set; }
        public ICommand OpenDialogCommand { get; private set; }
        public ICommand UpdateFirmwareCommand { get; private set; }
        public ICommand TestCommand { get; private set; }
        public ICommand ClosingCommand { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public ConnectionWizardVM()
        {
            try
            {
                // Bonjour Related
                bonjour = new BonjourService();
            }
            catch (Exception)
            {
                bonjour.IsBonjourServicesInstalled = false;
            }

            try
            {
                Thread th = new Thread(new ThreadStart(A));
                th.Start();
                // HostAddress = "10.2.0.104:5000";
                bgw = new BackgroundWorker();
                bgw.WorkerSupportsCancellation = true;
                bgw.DoWork += new DoWorkEventHandler(bgw_DoWork);
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgw_RunWorkerCompleted);

                bgwConnect = new BackgroundWorker();
                bgwConnect.DoWork += new DoWorkEventHandler(bgwConnect_DoWork);
                bgwConnect.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwConnect_RunWorkerCompleted);

                PopulateNetworkReader();
                IsBusy = false;

                _userControlList = new List<FrameworkElement>();
                _userControlList.Add(new ucWizardSelectReader());
                _userControlList.Add(new ucWizardReaderSetting());
                _userControlList.Add(new ucWizardConnectRead());

                // Creating BaudRate ItemSource
                BaudRateComboBoxSource = new ObservableCollection<string>();
                string[] baudrate = new string[] { "9600", "19200", "38400", "115200", "230400", "460800", "921600" };
                foreach (string temp in baudrate)
                    BaudRateComboBoxSource.Add(temp);

                ReaderList = new ObservableCollection<string>();
                ContentControlView = _userControlList[UserControlIndex];
                ButtonVisibility();
                LastConnectedVisibility = Visibility.Collapsed;

                // Button Command Handler Region
                CancelCommand = new RelayCommand<object>(OnCancel);
                NextConnectCommand = new RelayCommand<object>(OnNextConnect);
                BackCommand = new RelayCommand(OnBack);
                WizardButtonCommand = new RelayCommand<string>(OnWizardButton);
                ReaderTypeCheckedCommand = new RelayCommand(OnRefreshClick);
                ConnectReadCommand = new RelayCommand<object>(OnConnectRead);
                OpenDialogCommand = new RelayCommand(OnOpenDialog);
                UpdateFirmwareCommand = new RelayCommand(OnUpdateFirmware);
                TestCommand = new RelayCommand(OnTest);

                ReaderListAutoConnect = new Dictionary<string, string>();
                ReaderListAutoConnectModified = new Dictionary<string, string>();
                portNames.AddRange(GetComPortNames());
                foreach (string temp in portNames)
                {
                    MatchCollection mc = Regex.Matches(temp, @"(?<=\().+?(?=\))");
                    foreach (Match m in mc)
                    {
                        ReaderListAutoConnectModified.Add(m.ToString().ToUpper(), "serial");
                        ReaderListAutoConnect.Add(m.ToString().ToUpper(), temp);
                    }
                }

                //foreach (string temp in HostNameIpAddress.Values)
                //    ReaderListAutoConnect.Add(temp, "network");

                if (Directory.Exists(ConfigFilesdirPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(ConfigFilesdirPath);
                    FileInfo[] files = dir.GetFiles().Where(p => ((DateTime.Now.ToLocalTime() - p.LastWriteTime.ToLocalTime()) <= TimeSpan.FromDays(3))).OrderByDescending(p => p.LastWriteTime).ToArray();
                    string[] configurationfiles = files.Select(p => Path.GetFileNameWithoutExtension(p.FullName)).ToArray();
                    foreach (string temp in configurationfiles)
                    {
                        string comport = temp.Remove(temp.LastIndexOf('_'));
                        if (comport.ToLower().Contains("com"))
                        {
                            if (ReaderListAutoConnectModified.Keys.Contains(comport.ToUpper()))
                            {
                                if (ReaderListAutoConnectModified[comport.ToUpper()] == "serial")
                                {
                                    IsSerialReader = true; IsNetworkReader = false;
                                }
                                else if (ReaderListAutoConnectModified[comport.ToUpper()] == "network")
                                {
                                    IsNetworkReader = true; IsSerialReader = false;
                                }
                                IsSerialReader = true; IsNetworkReader = false;
                                ReaderConnectionDetail.ReaderName = ReaderListAutoConnect[comport.ToUpper()];
                                if (IsSerialReader)
                                {
                                    if (!IsReaderConnected())
                                    {
                                        GetReaderDetailsFromFile(comport.ToUpper());
                                        if (DetectedReaderModel == model && !(string.IsNullOrWhiteSpace(DetectedReaderRegion) || DetectedReaderRegion.Contains("Select")))
                                        {
                                            IsConfigurationAvailable = true;
                                            AutoConnectToURA();
                                            LastConnectedVisibility = Visibility.Visible;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            IsNetworkReader = true; IsSerialReader = false;
                            //Ping ping = new Ping();
                            //try
                            //{
                            //    PingReply reply = ping.Send(filename.ToUpper());
                            //    if (reply.Status == IPStatus.Success)
                            //    {
                            //        ReaderConnectionDetail.ReaderName = filename.ToUpper();
                            //        if (!IsReaderConnected())
                            //        {
                            //            GetReaderDetailsFromFile(filename.ToUpper());
                            //            if (DetectedReaderModel == model && !(string.IsNullOrWhiteSpace(DetectedReaderRegion) || DetectedReaderRegion.Contains("Select")))
                            //            {
                            //                IsConfigurationAvaiable = true;
                            //                AutoConnectToURA();
                            //                LastConnectedVisibility = Visibility.Visible;
                            //            }
                            //            break;
                            //        }
                            //    }
                            //}
                            //catch (Exception)
                            //{ }
                        }
                    }
                }
                if (!IsConfigurationAvailable)
                {
                    SelectReaderType();
                }
                StatusWarningText = "";
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        public void A()
        {
            IsNetworkReader = true; IsSerialReader = false;
            Dispatcher dispatchObject = Application.Current.Dispatcher;
            dispatchObject.BeginInvoke(new ThreadStart(delegate()
            {
                if (bonjour.IsBonjourServicesInstalled)
                {
                    bonjour.BackgroundNotifierCallbackCount = 0;
                    if (bonjour.Browser != null)
                    {
                        bonjour.Browser.Stop();
                        bonjour.ServicesList.Clear();
                    }

                    bonjour.HostNameIpAddress.Clear();
                    string[] serviceTypes = { "_llrp._tcp", "_m4api._udp." };//,
                    foreach (string serviceType in serviceTypes)
                    {
                        bonjour.Browser = bonjour.Service.Browse(0, 0, serviceType, null, bonjour.EventManager);
                    }
                    Thread.Sleep(500);
                    while (0 < bonjour.BackgroundNotifierCallbackCount)
                    {
                        Thread.Sleep(100);
                    }
                }
            }));
        }

        #endregion

        #region WPF Control Command Handler

        /// <summary>
        /// To be deleted before release
        /// </summary>
        private void OnTest()
        {
            //try
            //{
            //    NextButtonVisibility = Visibility.Collapsed;
            //    ContentControlView = new ucWizardFirmwareUpadate();
            //    FirmwareUpdateVisibility = Visibility.Visible;
            //}
            //catch (Exception ex)
            //{
            //    ShowErrorMessage(ex.Message);
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void OnConnectRead(object obj)
        {
            try
            {
                ValidateReadConnectPage(obj);
            }
            catch (Exception ex)
            {
                if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                {
                    ShowErrorMessage(ex);
                }
                else
                {
                    ShowErrorMessage(ex);
                    if (App.Current.MainWindow == null)
                    {
                        Window win = new ConnectionWizard();
                        win.Show();
                    }
                    else
                    {
                        Window win = (Window)App.Current.MainWindow;
                        win.Show();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnBack()
        {
            try
            {
                StatusWarningText = "";
                LastConnectedVisibility = Visibility.Collapsed;
                if (BackChangeReaderButtonContent == "Change Reader")
                {
                    UserControlIndex = 0;
                    SelectReaderType();
                    IsConfigurationAvailable = false;
                }
                else
                {
                    if (ContentControlView is ThingMagic.URA2.ucWizardFirmwareUpdate)
                        UserControlIndex = 0;
                    else
                        UserControlIndex--;
                }
                if (ContentControlView is ThingMagic.URA2.ucWizardReaderSetting)
                {
                    Gen2ProtocolIsChecked = ISO18000_6BIsChecked = IPX64IsChecked = IPX256IsChecked = ATAIsChecked = ISO14443AIsChecked = ISO14443BIsChecked = ISO15693IsChecked = ISO18092IsChecked = FELICAIsChecked = ISO180003Mode3IsChecked = LF125KHZIsChecked = LF134KHZIsChecked = false;
                }
                ContentControlView = _userControlList[UserControlIndex];
                ButtonVisibility();
                if (UserControlIndex == 0)
                {
                    if (objReader != null)
                    {
                        objReader.Destroy();
                        objReader = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
                if (NextConnectButtonContent.ToLower() == "next")
                {
                    ContentControlView = _userControlList[--UserControlIndex];
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNextConnect(object obj)
        {
            try
            {
                StatusWarningText = "";
                if (NextConnectButtonContent.ToLower() == "next")
                {
                    if (UserControlIndex == 0)
                    {
                        if (ValidateSelectReaderPage())
                        {
                            ReaderSettingsPageIntialize();
                        }
                    }
                    else if (UserControlIndex == 1)
                    {
                        if (ValidateConnectReaderPage())
                        {
                            ContentControlView = _userControlList[++UserControlIndex];
                            DetectedReaderName = ReaderURI();
                            DetectedReaderRegion = RegionListSelectedItem;
                            if (IsSerialReader)
                                DetectedReaderType = "Serial Reader";
                            else if (IsNetworkReader)
                                DetectedReaderType = "Network Reader";
                            else if (IsAddCustomReader)
                                DetectedReaderType = "Custom Transport Reader";
                            else
                                DetectedReaderType = "";

                            DetectedSelectedAntenna = "";
                            bool[] AntennaCheckedBool = { AntennaIsChecked1, AntennaIsChecked2, AntennaIsChecked3, AntennaIsChecked4 };
                            int tempint = 0;
                            foreach (bool temp in AntennaCheckedBool)
                            {
                                if (temp)
                                {
                                    DetectedSelectedAntenna = DetectedSelectedAntenna + ((tempint + 1).ToString()) + ",";
                                }
                                tempint++;
                            }
                            DetectedSelectedAntenna = DetectedSelectedAntenna.TrimEnd(',');

                            DetectedReaderProtocol = "";
                            bool[] ProtocolList = { Gen2ProtocolIsChecked, ISO18000_6BIsChecked, IPX64IsChecked, IPX256IsChecked, ATAIsChecked, ISO14443AIsChecked, ISO14443BIsChecked, ISO15693IsChecked, ISO18092IsChecked, FELICAIsChecked, ISO180003Mode3IsChecked, LF125KHZIsChecked, LF134KHZIsChecked };
                            tempint = 0;
                            foreach (bool temp in ProtocolList)
                            {
                                if (temp)
                                {
                                    switch (tempint)
                                    {
                                        case 0:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "Gen2" + " ,";
                                            break;
                                        case 1:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "ISO18000-6B" + " ,";
                                            break;
                                        case 2:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "IPX64" + " ,";
                                            break;
                                        case 3:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "IPX256" + " ,";
                                            break;
                                        case 4:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "ATA" + " ,";
                                            break;
                                        case 5:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "HF14443A" + " ,";
                                            break;
                                        case 6:
                                            detectedReaderProtocol = detectedReaderProtocol + "HF14443B" + " ,";
                                            break;
                                        case 7:
                                            detectedReaderProtocol = detectedReaderProtocol + "HF15693" + " ,";
                                            break;
                                        case 8:
                                            detectedReaderProtocol = detectedReaderProtocol + "HF18092" + " ,";
                                            break;
                                        case 9:
                                            detectedReaderProtocol = detectedReaderProtocol + "HFFELICA" + " ,";
                                            break;
                                        case 10:
                                            detectedReaderProtocol = detectedReaderProtocol + "HF180003M3" + " ,";
                                            break;
                                        case 11:
                                            detectedReaderProtocol = detectedReaderProtocol + "LF125KHZ" + " ,";
                                            break;
                                        case 12:
                                            detectedReaderProtocol = detectedReaderProtocol + "LF134KHZ" + " ,";
                                            break;
                                        default:
                                            DetectedReaderProtocol = DetectedReaderProtocol + "";
                                            break;
                                    }
                                }
                                tempint++;
                            }
                            DetectedReaderProtocol = DetectedReaderProtocol.TrimEnd(',');
                            DetectedReaderLastConnected = "";
                        }
                    }
                    ButtonVisibility();
                }
                else if (NextConnectButtonContent.ToLower() == "connect")
                {
                    isReadConnect = false;
                    OnConnectRead(obj);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void OnCancel(object obj)
        {
            try
            {
                if (objReader != null)
                {
                    objReader.Destroy();
                    objReader = null;
                }
                Window win = (Window)obj;
                Window win1 = new Main();
                win1.Show();
                win.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// On Refresh button click
        /// </summary>
        private void OnRefreshClick()
        {
            List<string> comPort = new List<string>();
            try
            {
                comPort.AddRange(GetComPortNames());
                for (int i = 0; i < portNames.Count; i++)
                {
                    string a = portNames[i];
                    if (!(comPort.Contains(portNames[i])))
                    {
                        portNames.Remove(portNames[i]);
                    }
                }
                for (int i = 0; i < comPort.Count; i++)
                {
                    string a = comPort[i];
                    if (!(portNames.Contains(comPort[i])))
                    {
                        portNames.Add(comPort[i]);
                    }
                }
                OnReaderTypeChecked();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radioButtonContent"></param>
        private void OnReaderTypeChecked()
        {
            try
            {
                StatusWarningText = "";
                SelectReaderPageIntialize();
                if (IsSerialReader)
                {
                    if (IsAddManualChecked)
                    {
                        SetStatusWarningMessage(ADDSERIALREADERMANUALINFO, Brushes.Blue);
                    }
                    else
                    {
                        if (IsReaderListNull())
                        {
                            SetStatusWarningMessage(NOSERIALREADERDETECTED, Brushes.Red);
                        }
                        else
                        {
                            ReaderListText = ReaderList[0];
                            ReaderListSelectedItem = ReaderList[0];
                        }
                    }
                }
                else if (IsNetworkReader)
                {
                    if (IsAddManualChecked)
                    {
                        SetStatusWarningMessage(ADDNETWORKREADERMANUALINFO, Brushes.Blue);
                    }
                    else
                    {
                        ReaderList = new ObservableCollection<string>();
                        foreach (string item in readerList)
                        {
                            ReaderList.Add(item);
                        }

                        if (IsReaderListNull())
                        {
                            if (bonjour.IsBonjourServicesInstalled)
                            {
                                SetStatusWarningMessage(NONETWORKREADERDETECTED, Brushes.Red);
                            }
                            else
                            {
                                SetStatusWarningMessage(BONJOURERROR, Brushes.Red);
                            }
                        }
                        else
                        {
                            ReaderListText = ReaderList[0];
                            ReaderListSelectedItem = ReaderList[0];
                        }
                    }
                }
                else if (IsAddCustomReader)
                {
                    SetStatusWarningMessage(ADDCUSTOMREADERMANUALINFO, Brushes.Blue);
                }
                else
                {
                    ShowErrorMessage(READERTYPENOTSELECTED);
                }

                // Not to be commented out
                //IsNextButtonEnabled = (ReaderListSelectedItem == null) ? false : true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        private void SelectReaderType()
        {
            try
            {
                IsNetworkReader = true; IsSerialReader = false;
                OnReaderTypeChecked();
                if (ReaderList != null)
                {
                    if (ReaderList.Count == 0)
                    {
                        IsSerialReader = true; IsNetworkReader = false;
                        OnReaderTypeChecked();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnOpenDialog()
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.Filter = "Firmware File (.*sim; *.deb)|*sim; *.deb|" + "ThingMagic Firmware (*.tmfw)|*.tmfw";
                openDialog.Title = "Select Firmware File";
                openDialog.ShowDialog();
                FirmwareUpdatePath = openDialog.FileName.ToString();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnUpdateFirmware()
        {
            try
            {
                if (IsSerialReader || IsAddCustomReader)
                {
                    if (!FirmwareUpdatePath.Contains(".sim"))
                    {
                        ShowErrorMessage("Invalid File Extension. Please select a .sim extension file");
                        return;
                    }
                }
                else if (IsNetworkReader)
                {
                    if (!(FirmwareUpdatePath.Contains(".tmfw") || FirmwareUpdatePath.Contains(".deb")))
                    {
                        ShowErrorMessage("Invalid File Extension. Please select a .tmfw (for M6,Astra etc.) or .deb (for Sargas Reader) extension file");
                        return;
                    }
                }
                else
                {
                    ContentControlView = _userControlList[UserControlIndex = 0];
                }

                IsBusy = true;
                BusyContent = "\n" + FirmwareUpdateReaderName + " : Firmware Update In Progress... \n";
                IsFirmwareUpdateSuccess = false;
                if (!bgw.IsBusy)
                    bgw.RunWorkerAsync();
                else
                {
                    RestartApplication();
                }

            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                System.IO.FileStream firmware = new System.IO.FileStream(FirmwareUpdatePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                if (objReader != null)
                {
                    objReader.FirmwareLoad(firmware);
                    IsFirmwareUpdateSuccess = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("successful"))
                {
                    IsFirmwareUpdateSuccess = true;
                    MessageBox.Show(ex.Message, "URA: Firmware Update Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    IsFirmwareUpdateSuccess = false;
                    ShowErrorMessage(ex.Message);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            if (IsFirmwareUpdateSuccess)
            {
                ContentControlView = _userControlList[UserControlIndex = 0];
                NextButtonVisibility = Visibility.Visible;
                StatusWarningText = "";
                FirmwareUpdateVisibility = Visibility.Collapsed;
                ButtonVisibility();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buttonContent"></param>
        private void OnWizardButton(string buttonContent)
        {

        }

        #endregion

        #region Command Handler

        /// <summary>
        /// 
        /// </summary>
        private void AutoConnectToURA()
        {
            try
            {
                UserControlIndex = 2;
                ContentControlView = _userControlList[UserControlIndex];
                ButtonVisibility();
                //IsConfigurationAvaiable = false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CheckForLastSaveSettings()
        {
            if (!(Directory.Exists(ConfigFilesdirPath)))
            {
                return false;
            }
            else
            {
                string fileName = ReaderURI() + ".urac";
                if (Directory.Exists(ConfigFilesdirPath))
                {
                    string[] configurationFiles = null;
                    configurationFiles = Directory.GetFiles(ConfigFilesdirPath, "*.urac").Select(p => Path.GetFileName(p)).ToArray();

                    foreach (string t in configurationFiles)
                    {
                        if (t.ToLower().Contains(fileName.ToLower()))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void GetReaderDetailsFromFile(string fname)
        {
            try
            {
                string fileFullPath = System.IO.Path.Combine(ConnectionWizardVM.ConfigFilesdirPath, fname + "_config" + ".txt");
                FileInfo fileinfo = new FileInfo(fileFullPath);
                //DetectedReaderLastConnected = fileinfo.LastWriteTimeUtc.ToLocalTime().ToString();
                DetectedReaderName = ReaderConnectionDetail.ReaderName;
                bool isMuxEnabled = false;
                if (File.Exists(fileFullPath))
                {
                    string[] lines = System.IO.File.ReadAllLines(fileFullPath);
                    var builder = new StringBuilder();
                    foreach (string line in lines)
                    {
                        if (line.Contains("/reader/region/id"))
                        {
                            DetectedReaderRegion = (line.Split('='))[1];
                        }
                        else if (line.Contains("/application/readwriteOption/portswitchgposenabled"))
                        {
                            string muxValue = (line.Split('='))[1];
                            if (muxValue.Contains("True"))
                            {
                                isMuxEnabled = true;
                            }
                            else
                            {
                                isMuxEnabled = false;
                            }
                        }
                        else if (line.Contains("/application/readwriteOption/Antennas"))
                        {
                            DetectedSelectedAntenna = (line.Split('='))[1];
                            string[] antennaArray = DetectedSelectedAntenna.Split(',');
                            if (isMuxEnabled && !(DetectedReaderModel.Equals("M3e")))//M3e doesnt support multiplexing 
                            {

                            }
                            else
                            {
                                int[] antList = (int[])objReader.ParamGet("/reader/antenna/PortList");
                                foreach (string r in antennaArray)
                                {
                                    int antenna = Convert.ToInt32(r);
                                    foreach(int ant in antList)
                                    {
                                        if(antenna.Equals(ant))
                                        {
                                            builder.Append(antenna);
                                            builder.Append(',');
                                        }
                                    }
                                }
                                try
                                {
                                    builder.Remove(builder.Length - 1, 1);
                                }
                                catch (Exception)
                                {
                                }
                                DetectedSelectedAntenna = builder.ToString();
                            }
                        }
                        else if (line.Contains("/application/readwriteOption/Protocols"))
                        {
                            DetectedReaderProtocol = (line.Split('='))[1];
                        }
                        else if (line.Contains("/application/connect/readerType"))
                        {
                            DetectedReaderType = (line.Split('='))[1];
                        }
                        else if (line.Contains("/reader/version/model"))
                        {
                            DetectedReaderModel = (line.Split('='))[1];
                            if (DetectedReaderModel.Contains("M3e"))//this will be optimized once the main form is ready
                            {
                                SelectedReaderType = "HF-LF";
                            }
                            else
                            {
                                SelectedReaderType = "UHF";
                            }
                        }
                        else if (line.Contains("lastconnected"))
                        {
                            DetectedReaderLastConnected = Convert.ToDateTime((line.Split('='))[1]).ToLocalTime().ToString();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void PopulateNetworkReader()
        {
            try
            {
                if (bonjour.IsBonjourServicesInstalled)
                {
                    bonjour.BackgroundNotifierCallbackCount = 0;
                    if (bonjour.Browser != null)
                    {
                        bonjour.Browser.Stop();
                        bonjour.ServicesList.Clear();
                    }

                    bonjour.HostNameIpAddress.Clear();
                    string[] serviceTypes = { "_llrp._tcp", "_m4api._udp." };

                    foreach (string serviceType in serviceTypes)
                    {
                        bonjour.Browser = bonjour.Service.Browse(0, 0, serviceType, null, bonjour.EventManager);
                    }
                    Thread.Sleep(500);
                    while (0 < bonjour.BackgroundNotifierCallbackCount)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsReaderConnected()
        {
            try
            {
                if (objReader != null)
                {
                    objReader.Destroy();
                    objReader = null;
                }
                objReader = CreateReaderObject(ReaderConnectionDetail.ReaderName);
                if (objReader != null)
                {
                    connectToReader(objReader);
                    model = objReader.ParamGet("/reader/version/model").ToString();
                }

                return false;
            }
            catch (Exception)
            {
                if (objReader != null)
                {
                    objReader.Destroy();
                    objReader = null;
                }
                return true;
            }
        }

        /// <summary>
        /// Configure antennas
        /// </summary>
        public void ConfigureAntennaBoxes()
        {
            // Cast int[] return values to IList<int> instead of int[] to get Contains method
            IList<int> existingAntennas = null;
            IList<int> detectedAntennas = null;
            IList<int> validAntennas = null;

            if (null == objReader)
            {
                int[] empty = new int[0];
                existingAntennas = detectedAntennas = validAntennas = empty;
            }
            else
            {
                bool checkPort = false;
                string model1 = objReader.ParamGet("/reader/version/model").ToString();
                switch (objReader.ParamGet("/reader/version/model").ToString())
                {
                    case "Astra":
                        checkPort = true;
                        break;
                    case "M3e":
                        checkPort = false;
                        break;
                    default:
                        try
                        {
                            checkPort = (bool)objReader.ParamGet("/reader/antenna/checkPort");
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Parameter not found"))
                            {
                                // Parameter not found error means antenna detection is not supported on the module.
                                checkPort = false;
                            }
                        }
                        break;
                }
                existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");
                detectedAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/connectedPortList");
                validAntennas = checkPort ? detectedAntennas : existingAntennas;
                Visibility[] AntennaVisibility = { AntennaVisibility1, AntennaVisibility2, AntennaVisibility3, AntennaVisibility4 };
                bool[] AntennaCheckedBool = { AntennaIsChecked1, AntennaIsChecked2, AntennaIsChecked3, AntennaIsChecked4 };

                for (int i = 0; i < ANTENNACOUNT; i++)
                {
                    switch (i)
                    {
                        case 0:
                            AntennaVisibility1 = (existingAntennas.Contains(i + 1)) ? Visibility.Visible : Visibility.Collapsed;
                            AntennaIsChecked1 = (detectedAntennas.Contains(i + 1)) ? true : false;
                            break;
                        case 1:
                            AntennaVisibility2 = (existingAntennas.Contains(i + 1)) ? Visibility.Visible : Visibility.Collapsed;
                            AntennaIsChecked2 = (detectedAntennas.Contains(i + 1)) ? true : false;
                            break;
                        case 2:
                            AntennaVisibility3 = (existingAntennas.Contains(i + 1)) ? Visibility.Visible : Visibility.Collapsed;
                            AntennaIsChecked3 = (detectedAntennas.Contains(i + 1)) ? true : false;
                            break;
                        case 3:
                            AntennaVisibility4 = (existingAntennas.Contains(i + 1)) ? Visibility.Visible : Visibility.Collapsed;
                            AntennaIsChecked4 = (detectedAntennas.Contains(i + 1)) ? true : false;
                            break;
                    }
                }
            }
        }

        private void ConfigureProtocols()
        {
            TagProtocol[] supportedProtocols = null;
            supportedProtocols = (TagProtocol[])objReader.ParamGet("/reader/version/supportedProtocols");
            Gen2ProtocolVisbility = ISO18000_6BVisbility = IPX256Visbility = IPX64Visbility = ATAVisbility = ISO14443AVisbility = ISO14443BVisbility = ISO15693Visbility = ISO18092Visbility = FELICAVisbility = ISO180003Mode3Visbility = LF125KHZVisbility = LF134KHZVisbility = Visibility.Collapsed;

            if (null != supportedProtocols)
            {
                foreach (TagProtocol proto in supportedProtocols)
                {
                    switch (proto)
                    {
                        case TagProtocol.GEN2:
                            Gen2ProtocolVisbility = Visibility.Visible;
                            Gen2ProtocolIsChecked = true;
                            break;
                        case TagProtocol.ISO180006B:
                            ISO18000_6BVisbility = Visibility.Visible;
                            break;
                        case TagProtocol.IPX64:
                            IPX64Visbility = Visibility.Visible;
                            break;
                        case TagProtocol.IPX256:
                            IPX256Visbility = Visibility.Visible;
                            break;
                        case TagProtocol.ATA:
                            ATAVisbility = Visibility.Visible;
                            break;
                        case TagProtocol.ISO14443A:
                            ISO14443AVisbility = Visibility.Visible;
                            ISO14443AIsChecked = true;
                            break;
                        case TagProtocol.ISO14443B:
                            ISO14443BVisbility = Visibility.Visible;
                            ISO14443BIsChecked = true;
                            break;
                        case TagProtocol.ISO15693:
                            ISO15693Visbility = Visibility.Visible;
                            ISO15693IsChecked = true;
                            break;
                        case TagProtocol.ISO18092:
                            ISO18092Visbility = Visibility.Visible;
                            ISO18092IsChecked = true;
                            break;
                        case TagProtocol.FELICA:
                            FELICAVisbility = Visibility.Visible;
                            FELICAIsChecked = true;
                            break;
                        case TagProtocol.ISO180003M3:
                            ISO180003Mode3Visbility = Visibility.Visible;
                            ISO180003Mode3IsChecked = true;
                            break;
                        case TagProtocol.LF125KHZ:
                            LF125KHZVisbility = Visibility.Visible;
                            LF125KHZIsChecked = true;
                            break;
                        case TagProtocol.LF134KHZ:
                            LF134KHZVisbility = Visibility.Visible;
                            LF134KHZIsChecked = true;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ButtonVisibility()
        {
            BackChangeReaderButtonContent = IsConfigurationAvailable ? "Change Reader" : "Back";
            FirmwareUpdateVisibility = Visibility.Collapsed;
            if (UserControlIndex == 0)
            {
                BackButtonVisibility = Visibility.Hidden;
                NextButtonVisibility = Visibility.Visible;
                ConnectReadButtonVisibility = Visibility.Collapsed;
                NextConnectButtonContent = "Next";
                IsAdvancedSettingButtonEnabled = true;
                IsConnectionSettingButtonEnabled = true;
                IsSelectReaderButtonEnabled = false;
                if (IsAddCustomReader)
                {
                    IsNextButtonEnabled = (string.IsNullOrWhiteSpace(HostAddress) ? false : true);
                }
                else
                {
                    IsNextButtonEnabled = string.IsNullOrWhiteSpace(ReaderURI()) ? false : true;
                }
            }
            else if (UserControlIndex == 2)
            {
                BackButtonVisibility = Visibility.Visible;
                NextButtonVisibility = Visibility.Visible;
                if (!(string.IsNullOrWhiteSpace(DetectedSelectedAntenna) || (string.IsNullOrWhiteSpace(DetectedReaderProtocol))))
                    ConnectReadButtonVisibility = Visibility.Visible;
                else
                    ConnectReadButtonVisibility = Visibility.Collapsed;
                NextConnectButtonContent = "Connect";
                IsAdvancedSettingButtonEnabled = false;
                IsConnectionSettingButtonEnabled = true;
                IsSelectReaderButtonEnabled = true;
                IsNextButtonEnabled = true;
            }
            else if (UserControlIndex == 1)
            {
                BackButtonVisibility = Visibility.Visible;
                NextButtonVisibility = Visibility.Visible;
                ConnectReadButtonVisibility = Visibility.Collapsed;
                NextConnectButtonContent = "Next";
                IsAdvancedSettingButtonEnabled = true;
                IsConnectionSettingButtonEnabled = false;
                IsSelectReaderButtonEnabled = true;
                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
        }

        private bool IsProtocolSelected()
        {
            bool[] ProtocolList = { Gen2ProtocolIsChecked, ISO18000_6BIsChecked, IPX64IsChecked, IPX256IsChecked, ATAIsChecked, ISO14443AIsChecked, ISO14443BIsChecked, ISO15693IsChecked, ISO18092IsChecked, FELICAIsChecked, ISO180003Mode3IsChecked, LF125KHZIsChecked, LF134KHZIsChecked };
            foreach (bool temp in ProtocolList)
            {
                if (temp)
                    return true;
            }
            return false;
        }

        private bool IsAntennaSelected()
        {
            bool[] AntennaCheckedBool = { AntennaIsChecked1, AntennaIsChecked2, AntennaIsChecked3, AntennaIsChecked4 };
            foreach (bool temp in AntennaCheckedBool)
            {
                if (temp)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void SelectReaderPageIntialize()
        {
            try
            {
                ReaderListText = "";
                ReaderList = null;
                ReaderList = new ObservableCollection<string>();
                if (IsSerialReader)
                {
                    foreach (string temp in portNames)
                        ReaderList.Add(temp);
                }
                else if (IsNetworkReader)
                {
                    if (bonjour.IsBonjourServicesInstalled)
                    {
                        bonjour.BackgroundNotifierCallbackCount = 0;
                        if (bonjour.Browser != null)
                        {
                            bonjour.Browser.Stop();
                            bonjour.ServicesList.Clear();
                        }

                        bonjour.HostNameIpAddress.Clear();

                        string[] serviceTypes = { "_llrp._tcp", "_m4api._udp." };//,
                        foreach (string serviceType in serviceTypes)
                        {
                            bonjour.Browser = bonjour.Service.Browse(0, 0, serviceType, null, bonjour.EventManager);
                        }
                        Thread.Sleep(500);
                        while (0 < bonjour.BackgroundNotifierCallbackCount)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
                else
                {
                    SetStatusWarningMessage(READERTYPENOTSELECTED, Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ReaderSettingsPageIntialize()
        {
            if (objReader != null)
            {
                objReader.Destroy();
                objReader = null;
            }
            try
            {
                BaudRateVisibility = Visibility.Collapsed;
                IsBusy = true;
                if (!string.IsNullOrWhiteSpace(ReaderListSelectedItem))
                {
                    BusyContent = "Checking connection to " + (IsAddCustomReader ? HostAddress : ReaderListSelectedItem);
                }
                else
                {
                    BusyContent = "Checking connection to " + (IsAddCustomReader ? HostAddress : ReaderListText);
                }
                if (!bgwConnect.IsBusy)
                    bgwConnect.RunWorkerAsync();
                else
                {
                    RestartApplication();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        void bgwConnect_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (IsSerialReader || IsNetworkReader)
                {
                    objReader = CreateReaderObject(ReaderURI());
                }
                else if (IsAddCustomReader)
                {
                    objReader = CreateReaderObject(HostAddress);
                }
                if (objReader != null)
                {
                    if (objReader is SerialReader)
                    {
                        ResetReader();
                    }
                    //create reader object for custom transport only since it is destroyed in ResetReader() in connectToReader().
                    if (IsAddCustomReader)
                    {
                        Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);
                        //Creates a Reader Object for operations on the Reader.
                        objReader = Reader.Create(string.Concat("tcp://", uri));
                    }
                    connectToReader(objReader);
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                if (objReader != null)
                {
                    objReader.Destroy();
                }
                if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                {
                    isInvalidAppExceptionRaised = true;
                    MessageBox.Show("Error connecting to reader: " + ex.Message + ". Please update the module firmware.", "Reader Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Reader connect using probing technique for serial readers. Others use connect directly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void connectToReader(Reader r)
        {
            if (r is SerialReader) // For serial readers, connect after probing
            {
                //Try probing through the baudrate list
                // to retrieve the module baudrate
                int currentBaudRate = 115200;
                try
                {
                    ((SerialReader)r).probeBaudRate(ref currentBaudRate);

                    // Serial port shut down happens inside probeBaudRate function which closes the "serverStream" and "clientSocket", so need to create a reader object again to initialize them again.
                    if (IsAddCustomReader)
                    {
                        Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);

                        //Creates a Reader Object for operations on the Reader.
                        r = Reader.Create(string.Concat("tcp://", uri));
                    }
                }
                catch (ReaderException rex)
                {
                    if (rex.Message.Contains("Connect Successful...Streaming tags"))
                    {
                        //stop the streaming
                        ((SerialReader)r).stopStreaming();
                    }
                    else { throw rex; }
                }
                //Set the current baudrate so that next connect will use this baudrate.
                r.ParamSet("/reader/baudRate", currentBaudRate);
                // Now connect with current baudrate
                r.Connect();
            }
            else // For other readers, call connect directly.
            {
                r.Connect();
            }
            if (IsAddCustomReader)
            {
                objReader = r;
            }
        }

        private void ResetReader()
        {
            try { 
            Reader temp;
            using (temp = CreateReaderObject(ReaderURI()))
            {
                SerialReader reader = temp as SerialReader;
                connectToReader(reader);
                if (IsAddCustomReader)
                {
                    reader = (SerialReader)objReader;
                }
                reader.CmdBootBootloader();
                if ((reader.ParamGet("/reader/version/model").ToString()).Contains("M3e"))
                {
                    Thread.Sleep(200);
                }
                reader.CmdBootFirmware();
                bool resetCompleted = false;
                int retryCount = 0;
                do
                {
                    try
                    {
                        byte program = reader.CmdGetCurrentProgram();
                        if ((program&0x3)==1)//Module is still in boot
                        {
                            resetCompleted = false;
                        }
                        else if ((program & 0x3) == 2)//Reader successfully jumped to App
                        {
                            resetCompleted = true;
                        }
                        else //unknown error
                        {
                            throw new ReaderException(String.Format("Unknown current program 0x{0:X}", program));
                        }
                    }
                    catch (Exception ex)
                    {
                        // In case of autonomous read enabled on the reader, send stopread.
                        if (-1 != ex.Message.IndexOf("Autonomous mode is enabled on reader."))
                        {
                            reader.stopReadIfAutoreadEnabled();
                        }
                        if (retryCount > 0)
                        {
                            reader.Destroy();
                            throw new Exception("No response");
                        }
                    }
                    finally
                    {
                        retryCount++;
                    }
                } while (!resetCompleted);
                reader.Destroy();
            }
            }catch(Exception){ }
        }

        void bgwConnect_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsBusy)
            {
                IsBusy = false;
                ContentControlView = _userControlList[++UserControlIndex];
                ButtonVisibility();
                SelectedReaderName = ReaderURI();
                model = (string)objReader.ParamGet("/reader/version/model");
                if (model.Contains("M3e"))
                {
                    SelectedReaderType = "HF-LF";
                }
                else
                {
                    SelectedReaderType = "UHF";
                }
                if (model.Equals("M3e"))
                {
                    RegionLabel = Visibility.Visible;
                    RegionDropBox = Visibility.Collapsed;
                }
                else
                {
                    RegionLabel = Visibility.Collapsed;
                    RegionDropBox = Visibility.Visible;
                    RegionList = GetSupportedRegion();
                }
                RegionList = GetSupportedRegion();
                if (((Reader.Region)objReader.ParamGet("/reader/region/id")) != Reader.Region.UNSPEC)
                {
                    //set the region on module
                    RegionListSelectedItem = ((Reader.Region)objReader.ParamGet("/reader/region/id")).ToString();
                }
                else
                {
                    if (model.Equals("M3e"))
                    {
                        RegionListSelectedItem = "UNIVERSAL";
                    }
                    else
                    {
                        RegionListSelectedItem = "Select";
                    }
                }

                ConfigureAntennaBoxes();

                ConfigureProtocols();

                // Set BaudRate
                if (IsSerialReader)
                {
                    BaudRateVisibility = Visibility.Visible;
                    BaudRateSelectedItem = this.objReader.ParamGet("/reader/baudRate").ToString();
                }

                DetectedReaderModel = this.objReader.ParamGet("/reader/version/model").ToString();

            }
            else
            {
                //if (StatusWarningText.Contains("Firmware is broken"))
                //{
                //    ContentControlView = new ucWizardFirmwareUpadate();
                //    FirmwareUpdateVisibility = Visibility.Visible;
                //    NextButtonVisibility = Visibility.Collapsed;
                //}
                //else
                //{
                ContentControlView = _userControlList[UserControlIndex = 0];
                ButtonVisibility();
                //}
                if (!IsInvalidAppExceptionRaised)
                {
                    ShowErrorMessage("Unable to connect to " + ReaderURI() + ".\nPlease check if the device is properly connected or Device might be in use.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ReaderName"></param>
        /// <returns></returns>
        private Reader CreateReaderObject(string ReaderName)
        {
            string readerUri = null;
            if (IsSerialReader)
            {
                if (!ValidatePortNumber(ReaderName) || ReaderName == "")
                {
                    throw new IOException();
                }

                // Creates a Reader Object for operations on the Reader.
                readerUri = ReaderName;
                //Regular Expression to get the com port number from comport name .
                //for Ex: If The Comport name is "USB Serial Port (COM19)" by using this 
                // regular expression will get com port number as "COM19".
                MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                        readerUri = m.ToString();
                }
                uri = readerUri;
                return Reader.Create(string.Concat("tmr:///", readerUri));
            }
            else if (IsNetworkReader)
            {
                string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(ReaderName)).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(key) || key == null)
                    readerUri = ReaderName;
                else
                    readerUri = bonjour.HostNameIpAddress[key];
                MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                        readerUri = m.ToString();
                }
                uri = readerUri;
                //Creates a Reader Object for operations on the Reader.
                return Reader.Create(string.Concat("tmr://", readerUri));
            }
            else if (IsAddCustomReader)
            {
                Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);
                readerUri = HostAddress;
                uri = readerUri;
                //Creates a Reader Object for operations on the Reader.
                return Reader.Create(string.Concat("tcp://", readerUri));
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<string> GetSupportedRegion()
        {
            Reader.Region[] regions;
            ObservableCollection<string> tempRegionList = new ObservableCollection<string>();
            if (objReader is LlrpReader || objReader is RqlReader)
            {
                regions = new Reader.Region[] { (Reader.Region)objReader.ParamGet("/reader/region/id") };
            }
            else
            {
                regions = (Reader.Region[])objReader.ParamGet("/reader/region/supportedRegions");
            }
            foreach (var region in regions)
            {
                tempRegionList.Add(region.ToString());
            }

            if (!(model.Equals("M3e")))
            {
                tempRegionList.Add("Select");
            }
            return tempRegionList;
        }

        /// <summary>
        /// Returns the COM port names as list
        /// </summary>
        /// <returns></returns>
        private List<string> GetComPortNames()
        {
            List<string> portNames = new List<string>();
            using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if ((queryObj != null) && (queryObj["Name"] != null))
                    {
                        try
                        {
                            if (queryObj["Name"].ToString().Contains("(COM"))
                            {
                                if (queryObj["Name"].ToString().Contains("Serial Port"))
                                {

                                    string portNumber = Regex.Match((queryObj["Name"].ToString()), @"(?<=\().+?(?=\))").Value.ToString();
                                    using (Reader r = Reader.Create("tmr:///" + portNumber))
                                    {
                                        //log.Info("Detected device Has Generic Name so conneting to retrive the Model name and Serail number for Port: " + queryObj["Name"].ToString());
                                        SerialReader serialReader = r as SerialReader;
                                        int baud = 115200;
                                        #region Reduce the timeout to quickly complete the search for non TM Devices
                                        r.ParamSet("/reader/transportTimeout", 100);
                                        r.ParamSet("/reader/commandTimeout", 100);
                                        #endregion
                                        try
                                        {
                                            // Open the serial port
                                            serialReader.OpenSerialPort(portNumber, ref baud);
                                        }
                                        catch (ReaderCommException ex)
                                        {
                                            // Baudrate mismatch leads to timeout error
                                            if (ex.Message.Contains("The operation has timed out."))
                                            {
                                                int currentBaudRate = baud;
                                                // Probe through the baudrate list and get the current baudrate of the reader
                                                try
                                                {
                                                    ProbeBaudratesAndOpenSerialPort(r, portNumber, ref currentBaudRate);
                                                }
                                                catch (ReaderException rex)
                                                {
                                                    if (rex.Message.Contains("Connect Successful...Streaming tags"))
                                                    {
                                                        HandleConnectStreamingResponse(r, currentBaudRate, portNumber, false);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // for any other kind of error, don't add the port to list. Go ahead and repeat the process for other ports detected.
                                                continue;
                                            }
                                        }
                                        catch (ReaderException re)
                                        {
                                            if (re.Message.Contains("Connect Successful...Streaming tags"))
                                            {
                                                HandleConnectStreamingResponse(r, baud, portNumber, true);
                                            }
                                        }
                                        string strSerialNumber, strModel = serialReader.model;
                                        try
                                        {
                                            strSerialNumber = serialReader.CmdGetSerialNumber();
                                        }
                                        catch (Exception)
                                        {
                                            strSerialNumber = "";
                                        }
                                        string serialNumber = strSerialNumber;
                                        if (serialNumber == "" || serialNumber == null)
                                        {
                                            portNames.Add(strModel + " (" + portNumber + ")");
                                        }
                                        else
                                        {
                                            portNames.Add(strModel + "-" + serialNumber + " (" + portNumber + ")");
                                        }
                                    }
                                }
                                else
                                {
                                    string portNumber = Regex.Match((queryObj["Name"].ToString()), @"(?<=\().+?(?=\))").Value.ToString();
                                    using (Reader r = Reader.Create("tmr:///" + portNumber))
                                    {
                                        //log.Info("Detected device Has Generic Name so conneting to retrive the Serial number for Port: " + queryObj["Name"].ToString());
                                        SerialReader serialReader = r as SerialReader;
                                        int baud = 115200;
                                        #region Reduce the timeout to quickly complete the search for non TM Devices
                                        r.ParamSet("/reader/transportTimeout", 100);
                                        r.ParamSet("/reader/commandTimeout", 100);
                                        #endregion
                                        try
                                        {
                                            serialReader.OpenSerialPort(portNumber, ref baud);
                                        }
                                        catch (ReaderCommException ex)
                                        {
                                            // Baudrate mismatch leads to timeout error
                                            if (ex.Message.Contains("The operation has timed out."))
                                            {
                                                int currentBaudRate = baud;
                                                // Probe through the baudrate list and get the current baudrate of the reader
                                                try
                                                {
                                                    ProbeBaudratesAndOpenSerialPort(r, portNumber, ref currentBaudRate);
                                                }
                                                catch (ReaderException rex)
                                                {
                                                    if (rex.Message.Contains("Connect Successful...Streaming tags"))
                                                    {
                                                        HandleConnectStreamingResponse(r, currentBaudRate, portNumber, false);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // for any other kind of error, don't add the port to list. Go ahead and repeat the process for other ports detected.
                                                continue;
                                            }
                                        }
                                        catch(ReaderException re)
                                        {
                                            if (re.Message.Contains("Connect Successful...Streaming tags"))
                                            {
                                                HandleConnectStreamingResponse(r, baud, portNumber, true);
                                            }
                                        }
                                        string strSerialNumber, strModel = serialReader.model;
                                        try
                                        {
                                            strSerialNumber = serialReader.CmdGetSerialNumber();
                                        }
                                        catch (Exception)
                                        {
                                            strSerialNumber = "";
                                        }
                                        string serialNumber = strSerialNumber;
                                        if (serialNumber == "" || serialNumber == null)
                                        {
                                            portNames.Add(queryObj["Name"].ToString());
                                        }
                                        else
                                        {
                                            portNames.Add(queryObj["Description"].ToString() + "-" + serialNumber + " (" + portNumber + ")");
                                        }
                                    }

                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Reader is throwing error for connect so we are not going add in the Reader's list.
                        }
                    }
                }
            }

            return portNames;
        }

        /// <summary>
        /// Probes through the baudrate list and gets the current baudrate of the reader.
        /// Sets the api baudrate value to module baudrate and opens the serial port with portNumber
        /// </summary>
        private void ProbeBaudratesAndOpenSerialPort(Reader r, String portNumber, ref int currentBaudRate)
        {
            // Probe through the baudrate list and get the current baudrate of the reader
            ((SerialReader)r).probeBaudRate(ref currentBaudRate);

            //Set the current baudrate so that next connect will use this baudrate.
            r.ParamSet("/reader/baudRate", currentBaudRate);

            // Open the serial port
            ((SerialReader)r).OpenSerialPort(portNumber, ref currentBaudRate);
        }

        /// <summary>
        /// In case of streaming enabled on the reader, it outputs tags for connect version command.
        /// In order for the application to get all the requested information of module, stop read has to be sent. Send stop read
        /// and set the baudrate if it is non-115200 and open the port with the same.
        /// </summary>
        private void HandleConnectStreamingResponse(Reader r, int currentBaudRate, String portNumber, bool isDefaultBaudRate)
        {
            //stop the streaming
            ((SerialReader)r).stopStreaming();

            if (!isDefaultBaudRate)
            {
                //Set the current baudrate so that next connect will use this baudrate.
                r.ParamSet("/reader/baudRate", currentBaudRate);
            }
            // Open the serial port
            ((SerialReader)r).OpenSerialPort(portNumber, ref currentBaudRate);
        }

        /// <summary>
        /// Check for valid port numbers
        /// </summary>
        /// <param name="portNumber"></param>
        /// <returns></returns>
        private bool ValidatePortNumber(string readerName)
        {
            List<string> portValues = new List<string>();
            //converting comport number from small letter to capital letter.Eg:com18 to COM18.
            string portNumber = Regex.Replace(readerName, @"[^a-zA-Z0-9_\\]", "").ToUpperInvariant();
            // getting the list of comports value and name which device manager shows
            for (int i = 0; i < portNames.Count; i++)
            {
                MatchCollection mc = Regex.Matches(portNames[i], @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    portValues.Add(m.ToString());
                }
            }
            if ((portNames.Contains(readerName)) || (portValues.Contains(portNumber)))
            {
                //Specified port number exist
                return true;
            }
            else
            {
                //Specified port number doesn't exist
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        private void ShowErrorMessage(string msg)
        {
            MessageBox.Show(msg, "Connection Wizard : Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatusWarningMessage(msg, Brushes.Red);
        }

        private void ShowErrorMessage(Exception ex)
        {

            if (ex is UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message + "\n" + "Please check if another program is accessing the Reader.", "Connection Wizard : Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatusWarningMessage(ex.Message + "\n" + "Please check if another program is accessing the Reader.", Brushes.Red);
            }
            else if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
            {
                MessageBox.Show("Firmware is broken. " + ex.Message + ".\nPlease Upgrade the firmware.\nClick on Cancel Button to go to URA and update the firmware.", "Firmware Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatusWarningMessage("Firmware is broken. " + ex.Message + ".\nPlease Upgrade the firmware.\nClick on Cancel Button to go to URA and update the firmware.", Brushes.Red);
                //FirmwareUpdateReaderName = ReaderURI();
                //FirmwareUpdateVisibility = Visibility.Visible;
                //NextButtonVisibility = Visibility.Collapsed;
                //BackButtonVisibility = Visibility.Visible;
                //ContentControlView = new ucWizardFirmwareUpadate();
            }
            else
            {
                MessageBox.Show(ex.Message, "Connection Wizard : Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatusWarningMessage(ex.Message, Brushes.Red);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        private void SetStatusWarningMessage(string message, Brush color)
        {
            StatusWarningText = message;
            StatusWarningColor = color;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsReaderListNull()
        {
            if (ReaderList.Count > 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsReaderListSelectedItemNull()
        {
            if (ReaderListSelectedItem != null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool IsReaderTypeSelected()
        {
            if (IsSerialReader || IsNetworkReader || IsAddCustomReader)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Validates tje Select Reader Page. Returns True : If all validations are successfull.
        /// </summary>
        /// <returns></returns>
        private bool ValidateSelectReaderPage()
        {
            if (IsReaderTypeSelected())
            {
                if (IsSerialReader || IsNetworkReader)
                {
                    if (IsReaderListNull() && string.IsNullOrWhiteSpace(ReaderListText))
                    {
                        ShowErrorMessage(IsSerialReader ? NOSERIALREADERDETECTED : (IsNetworkReader ? NONETWORKREADERDETECTED : "Error"));
                        return false;
                    }
                    else
                    {
                        if (IsAddManualChecked)
                        {
                            if (string.IsNullOrWhiteSpace(HostAddress))
                                return false;
                            else
                                return true;
                        }
                        if (IsReaderListSelectedItemNull() && string.IsNullOrWhiteSpace(ReaderListText))
                        {
                            ShowErrorMessage(NOREADERSELECTED);
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (!(String.IsNullOrWhiteSpace(HostAddress)))
                    {
                        Regex ip = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,4}\b");
                        if (ip.IsMatch(HostAddress))
                            return true;
                        else
                        {
                            ShowErrorMessage("Incorrect URI Format.\n" + ADDCUSTOMREADERMANUALINFO);
                            return false;
                        }
                    }
                    else
                    {
                        ShowErrorMessage("Host Address Cannout be left blank");
                        return false;
                    }
                }
            }
            else
            {
                ShowErrorMessage(READERTYPENOTSELECTED);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool ValidateConnectReaderPage()
        {
            if (RegionListSelectedItem == null || (IsSerialReader && BaudRateSelectedItem == null))
                return false;

            else
                return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void ValidateReadConnectPage(object obj)
        {
            ReaderConnectionDetail.ReaderName = DetectedReaderName;
            if (IsSerialReader)
            {
                ReaderConnectionDetail.BaudRate = "115200";
            }
            else if (IsNetworkReader)
            {
                //ReaderConnectionDetail.ReaderName = HostNameIpAddress.ContainsKey(ReaderConnectionDetail.ReaderName) ? HostNameIpAddress[ReaderConnectionDetail.ReaderName] : ReaderConnectionDetail.ReaderName;
            }
            ReaderConnectionDetail.ReaderType = DetectedReaderType;
            ReaderConnectionDetail.Region = DetectedReaderRegion;
            ReaderConnectionDetail.Antenna = DetectedSelectedAntenna;
            ReaderConnectionDetail.Protocol = DetectedReaderProtocol;
            ReaderConnectionDetail.ReaderModel = DetectedReaderModel;

            if (objReader != null)
            {
                objReader.Destroy();
                objReader = null;
            }
            IsBusy = true;
            BusyContent = "Opening Universal Assistant Reader - " + ReaderConnectionDetail.ReaderName;

            SetStatusWarningMessage(BusyContent, Brushes.DarkGreen);

            Main window = new Main();
            window.Show();
            window.LoadURAnFromWizardFlow(isReadConnect);

            if (objReader != null)
            {
                objReader.Destroy();
                objReader = null;
            }

            Window win = (Window)obj;
            win.Close();
        }

        private void NextButtonVisibilitySelectReaderPage()
        {
            try
            {
                if (RegionListSelectedItem != "Select" && IsProtocolSelected() && IsAntennaSelected())
                    IsNextButtonEnabled = true;
                else
                    IsNextButtonEnabled = false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private string ReaderURI()
        {
            if (!IsAddCustomReader)
            {
                if (IsNetworkReader)
                {
                    string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(string.IsNullOrWhiteSpace(ReaderListSelectedItem) ? ReaderListText : ReaderListSelectedItem)).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(string.IsNullOrWhiteSpace(ReaderListSelectedItem) ? ReaderListText : ReaderListSelectedItem) || key == null)
                        return string.IsNullOrWhiteSpace(ReaderListSelectedItem) ? ReaderListText : ReaderListSelectedItem;
                    else
                        return key;
                }
                else
                {
                    return (IsAddManualChecked ? HostAddress : (string.IsNullOrWhiteSpace(ReaderListSelectedItem) ? ReaderListText : ReaderListSelectedItem));
                }
            }
            else
                return HostAddress;
        }

        private void RestartApplication()
        {
            Window mw = Application.Current.MainWindow;
            Window cw = new ConnectionWizard();
            Application.Current.MainWindow = cw;
            MessageBox.Show("Application Encountered a fatal exception. Restarting the application", "Universal Reader Assitant : Restarting...");
            cw.Show();
            mw.Close();
        }

        #endregion
    }
}