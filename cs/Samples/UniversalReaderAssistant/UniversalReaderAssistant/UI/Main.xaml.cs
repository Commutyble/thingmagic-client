#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using System.Xml;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using System.Collections;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ThingMagic;
using ThingMagic.URA2.ViewModel;
using ThingMagic.URA2.BL;
using System.Globalization;
using Bonjour;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Management;
using System.Text.RegularExpressions;
using log4net;
using ThingMagic.URA2.Models;
using ThingMagic.URA2.UI;
using static ThingMagic.Reader;

//using Excel = Microsoft.Office.Interop.Excel;
#endregion Using

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        #region Fields
        /// <summary>
        /// This flag is used to synchronize all "reader disconnection" exception messages
        /// </summary>
        static bool isReaderConnected = true;
        // Log4net
        private static readonly ILog log = LogManager.GetLogger((System.Reflection.MethodBase.GetCurrentMethod().DeclaringType));
        // Cache async read progress state
        private bool isAsyncReadGoingOn = false;
        // Cache sync read progress state
        private bool isSyncReadGoingOn = false;
        /// <summary>
        /// Firmware Update failed flag
        /// </summary>
        public bool isFirmwareUpdateFailed = false;
        /// <summary>
        /// Iso18k6bCheckbox checked state
        /// </summary>
        public bool iso6bCheckBoxChecked = false;
        /// <summary>
        ///  Read time gets accumulated across all Start and Stop Reads,
        ///  until Clear Reads is pressed.
        /// </summary>
        public double continuousreadElapsedTime = 0.0;
        /// <summary>
        /// Read once time
        /// </summary>
        public double readonceElapsedTime = 0.0;
        // Stream for logging
        //StreamWriter log;

        /// <summary>
        /// Reader model
        /// </summary>
        public string model = string.Empty;

        /// <summary>
        /// TMR log file created time
        /// </summary>
        public string tmrLogStartTime = string.Empty;

        /// <summary>
        /// Read rate tag count per second
        /// </summary>
        public long readRatePerSecondCount = 0;

        // Supported protocols by the connected reader
        private TagProtocol[] supportedProtocols = null;
        // Load save configuration object
        private LoadSaveConfiguration loadSaveConfig = null;

        /// <summary>
        /// Check bonjour services are installed on the host machine or no
        /// </summary>

        private DateTime previousTemperatureErrorTime;

        /// <summary>
        /// Connection lost count
        /// </summary>
        static int connectionLostCount = 0;

        //To remember start async read time
        DateTime startAsyncReadTime;

        // Dummy columns for layers 0 and 1:
        ColumnDefinition column1CloneForLayer0 = null;
        ColumnDefinition column2CloneForLayer0 = null;
        ColumnDefinition column2CloneForLayer1 = null;

        // To re read all the tags from the database and clear the memory
        DispatcherTimer dispatchtimer = null;

        //To display tag read rate per second
        DispatcherTimer readRatePerSec = null;

        //To check if the module has stopped reading
        DispatcherTimer isReadStopped = null;
        // To revert the CW and PBRS button status to defaults
        DispatcherTimer cwTimer = null;
        DispatcherTimer prbsTimer = null;

        //TO read all tags from database and show them on web
        System.Timers.Timer HttpPostDispatchTimer = null;

        /// <summary>
        /// Save the target in temporary variable before changing it to target AB, when fast search is enabled 
        /// and Change to the original target, when fast search is disabled.. 
        /// </summary>
        private string tempTarget = string.Empty;

        /// <summary>
        /// Define a reader variable
        /// </summary>
        Reader objReader = null;

        /// <summary>
        /// Define a region variable
        /// </summary>
        Reader.Region regionToSet = new Reader.Region();

        ///// <summary>
        ///// Define a user mode variable
        ///// </summary>
        SerialReader.UserMode usrModeToSet = new SerialReader.UserMode();

        /// <summary>
        /// Define a list of simple read plans for Multiple Read Plan
        /// </summary>
        List<ReadPlan> simpleReadPlans = new List<ReadPlan>();

        /// <summary>
        /// Define a protocol variable for access commands
        /// </summary>
        List<TagProtocol> tagOpProto = new List<TagProtocol>();

        /// <summary>
        /// Define a variable for the transport listener log
        /// </summary>
        TextWriter transportLogFile = null;
        // Cache warning text status
        string warningText;
        System.Timers.Timer myTimer = new System.Timers.Timer();

        // Flag to check if RF Mode is supported for M7e FW version
        private bool isRFModeSupp = false;
        /// <summary>
        /// Define a variable for selection criteria
        /// </summary>
        TagFilter selectionOnEPC = null;

        // Whether the settings need to be loaded from the profile or 
        // from the connected reader
        private bool initialReaderSettingsLoaded = true;

        /// <summary>
        /// Delegates 
        /// </summary>
        delegate void del();
        private delegate void EmptyDelegate();

        /// <summary>
        /// If a flurry of read exceptions occurs, only want to show one MessageBox
        /// </summary>
        private bool showingAsyncReadExceptionMessageBox;
        private Object showingAsyncReadExceptionMessageBoxLock = new Object();

        // Tag database object
        TagDatabase tagdb = new TagDatabase();

        public ObservableCollection<string> Positions { get; set; }

        public ObservableCollection<ColumnSelectionForTagResult> selectedColumnList { get; set; }
        public ObservableCollection<TagFamily> Families { get; set; }

        // Optimal tag reads settings
        Dictionary<string, string> OptimalReaderSettings = null;
        Dictionary<string, bool> Gen2SettingChanged = new Dictionary<string, bool>();

        // List of configuration settings to be saved
        Dictionary<string, string> SaveConfigurations = null;

        // Bonjour initialization fields
        string uri = string.Empty;
        BonjourService bonjour = null;

        /// <summary>
        /// URA custom message box object
        /// </summary>
        URACustomMessageBoxWindow CustomizedMessageBox = null;

        /// <summary>
        /// URA channel occupied Message box object
        /// </summary>
        URAChannelOccupiedMessageBoxWindow channelOccupiedMessageBox = null;

        //Socket for tcp streaming
        List<Socket> tagStreamSock = new List<Socket>();
        int tagStreamSockCount = 0;
        // listener to open Tcp connection
        private TcpListener listener;
        // Tcp Server thread 
        private Thread serverThread;
        // Array to store tagread objects
        TagReadData[] trd;
        // Boolean variable to check Tcp Client connect or not
        bool clientConnected = false;
        // Bollean variable to check wait for client connection or not
        public bool waitforclient = true;
        // ManualResetEvent class for Thread synchronization 
        private ManualResetEvent suspendChangedEvent = new ManualResetEvent(false);
        //boolean variable to check Http post is enabled or not
        bool isHttpPostServiceEnabled = false;
        // variables to store Http Post service fields
        string HttpPostServiceReaderName = string.Empty;
        string HttpPostServiceUrl = string.Empty;
        private double HttpPostInterval;
        private string MacAddress = string.Empty;
        // Added flag to exit SetOptimalReaderSettings method
        bool isOptimalReaderSettingsFailed = false;
        // Added to group all M6e family readers
        public static readonly List<string> M6eFamilyList = new List<string>();
        // Added to group all M7e family readers
        public static readonly List<string> M7eFamilyList = new List<string>();
        // To cache transport timeout while performing PBRS
        public int tempTransportTimeOut;
        // Added this to send PBRS command
        public Thread pbrsThread;
        //private bool IsReconnectReader = false;
        // Added to check if Reader Fails to reconnect back
        private bool isReconnectFailed = false;
        // Added to check if reader is connected 
        private bool isURAConnectInProgress = false;

        // License Upgrade Fields
        BackgroundWorker bgwLicenseUpgrade = new BackgroundWorker();
        string LicenseStatus = "";
        Dictionary<string, string> licensetoupdate = null;
        string licenseUpgradeModuleSerialNumber = "";
        string moduleName = "";
        string licensePathTemp = "";
        List<string> licenseUpgradesSuccessfull = null;
        List<string> licenseUpgradeFailed = null;
        List<int> antMux = null;
        List<GpioPin> gps = new List<GpioPin>();
        List<int> outputList = null;
        List<int> inputList = null;
        List<int> triggerGPI = null;

        BackgroundWorker bgwApplyGen2Settings = new BackgroundWorker();

        // Combobox for the Tag Type
        KeyValuePair<TagProtocol, ColumnSelectionForTagtype> keyValueTag;
        public List<KeyValuePair<TagProtocol, ColumnSelectionForTagtype>> selectedTagColumnList = new List<KeyValuePair<TagProtocol, ColumnSelectionForTagtype>>();

        #region Tag family List
        bool allFamilyTag = false;
        bool TagAddControl = false;
        bool TagRemoveControl = false;
        ObservableCollection<Tag> hidTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> mifareTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> icodeTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> legicTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> emTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> hiTagFamily = new ObservableCollection<Tag>();
        ObservableCollection<Tag> otherTagFamily = new ObservableCollection<Tag>();
        #endregion

        // Selected tags list 
        public List<ColumnSelectionForTagButton> selectedTagForList = new List<ColumnSelectionForTagButton>();
        //MACROS
        public int MAXVALUE = 65535;
        public int DEFUALTTIME = 500;
        public int TIMEFORCONDITION = 1000;

        public StopOnTagCount sotc = new StopOnTagCount();


        List<int> selectedAtenna = new List<int>();

        //Load/Save config boolean variable for HTTP settings
        bool httpenabled = false;
        /// <summary>
        /// Stores Detected comport
        /// </summary>
        List<string> masterPortList = new List<string>();
        #region Variable for Storing Reader settings
        /// <summary>
        /// If reconnect of the variable has started
        /// </summary>
        bool restoreReadSetting = false;
        #region Variable storing read settings
        List<int> antList = new List<int>();
        List<TagProtocol> protocolList = new List<TagProtocol>();
        bool storeFastSearch = false;
        bool enableEmbedded = false;
        uint startAddress = 0;
        int readLength = 0;
        Gen2.Bank memBank = Gen2.Bank.EPC;
        bool enableUnique = false;
        bool enableFailData = false;
        bool storeApplyFilter1 = false;
        ucFilters storeFilter1 = null;
        string storeFilterData1 = "";
        bool storeApplyFilter2 = false;
        ucFilters StoreFilter2 = null;
        string storeFilterData2 = "";
        bool storeApplyFilter3 = false;
        ucFilters storeFilter3 = null;
        string storeFilterData3 = "";
        #endregion
        #endregion

        #endregion

        static Main()
        {
            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            log4net.Config.XmlConfigurator.Configure(new FileInfo(string.Format("{0}.config", location)));
            //string folderpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "URA");
            //bool isExists = System.IO.Directory.Exists(folderpath);
            //// If folder doesn't exist. Create the new folder with name URA 
            //if (!isExists)
            //{
            //    System.IO.Directory.CreateDirectory(folderpath);
            //}
            //string tmrLogStartTime1 = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            //string filename = System.IO.Path.Combine(folderpath, "tmrlog_" + tmrLogStartTime1 + ".txt");
            //log = (ILog)new StreamWriter(filename, true);
        }

        public Main()
        {
            try
            {
                string[] titleVer = (this.GetType().Assembly.GetName().Version.ToString()).Split('.').ToArray();
                Title = "Universal Reader Assistant " + titleVer[0] + "." + titleVer[1];
                App.Current.MainWindow = this;
                // Ending the session when Application abruptly shuts down implicitly or explicitly
                SystemEvents.SessionEnding += (o, e) =>
                {
                    // releasing the reader resource.
                    if (null != objReader)
                    {
                        objReader.Destroy();
                        objReader = null;
                    }

                    // Closing application programatically
                    Environment.Exit(1);
                };
                //string folderpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "URA");
                //bool isExists = System.IO.Directory.Exists(folderpath);
                //// If folder doesn't exist. Create the new folder with name URA 
                //if (!isExists)
                //{
                //    System.IO.Directory.CreateDirectory(folderpath);
                //}
                //tmrLogStartTime = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                //string filename = System.IO.Path.Combine(folderpath, "tmrlog_" + tmrLogStartTime + ".txt");
                //log = new StreamWriter(filename, true);

                // WPF draws the screen at a continuous pace of 60 frames per second rate by default. 
                // So if we are using lots of graphics and images, application will eventually take a 
                // lots CPU utilization because of these frame rates. Hence reducing the frame-rates to 
                // 10 frames per second
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 10 });

                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                    CurrentDomain_UnhandledException);
                Application.Current.Dispatcher.UnhandledException += new DispatcherUnhandledExceptionEventHandler(
                    Dispatcher_UnhandledException);
                InitializeComponent();
                InitializeDummyClmnsforDocking();
                cbxBaudRate.ItemsSource = cbxDisplayBaudRateItem(string.Empty);
                AddGPIPort(0);
                dispatchtimer = new DispatcherTimer();
                readRatePerSec = new DispatcherTimer();
                isReadStopped = new DispatcherTimer();
                HttpPostDispatchTimer = new System.Timers.Timer();
                readRatePerSec.Interval = TimeSpan.FromMilliseconds(900);
                isReadStopped.Interval = TimeSpan.FromMilliseconds(10);
                dispatchtimer.Interval = TimeSpan.FromMilliseconds(50);
                readRatePerSec.Tick += new EventHandler(readRatePerSec_Tick);
                isReadStopped.Tick += new EventHandler(isReadStopped_Tick);
                dispatchtimer.Tick += new EventHandler(dispatchtimer_Tick);
                HttpPostDispatchTimer.Elapsed += new ElapsedEventHandler(sendtags_Tick);
                // PBRS Timer
                prbsTimer = new DispatcherTimer();
                prbsTimer.Tick += new EventHandler(prbsTimer_Tick);
                // CW Timer
                cwTimer = new DispatcherTimer();
                cwTimer.Tick += new EventHandler(cwTimer_Tick);

                //GenerateColmnsForDataGrid();  
                regioncombo.ItemsSource = null;
                ConfigureAntennaBoxes(null);
                ConfigureLogicalAntennaBoxes(null);
                ConfigureProtocols(null);
                loadSaveConfig = new LoadSaveConfiguration();
                SaveConfigurations = new Dictionary<string, string>();
                rdbtnglobal.IsChecked = true;
                InitializeColumnSelectionCbx();
                txtTagsNum.Text = _numValue.ToString();
                //Display URA version and Api version, when application gets initialized
                InitializeRdrDiagnostics();
                btnRefreshReadersList.Visibility = System.Windows.Visibility.Visible;
                M6eFamilyList.InsertRange(M6eFamilyList.Count, new string[] { "M6e", "M6e Micro", "M6e Micro USB", "M6e PRC", "M6e Nano", "M6e Micro USBPro", "M6e JIC" });
                M7eFamilyList.InsertRange(M7eFamilyList.Count, new string[] { "M7e Pico", "M7e Deka", "M7e Hecto", "M7e Mega", "M7e Tera" });
                chkEnableTagAging.IsChecked = true;
                previousTemperatureErrorTime = DateTime.Now;

                bonjour = new BonjourService();

                bgwLicenseUpgrade.DoWork += new DoWorkEventHandler(bgwLicenseUpgrade_DoWork);
                bgwLicenseUpgrade.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwLicenseUpgrade_RunWorkerCompleted);

                bgwApplyGen2Settings.DoWork += new DoWorkEventHandler(bgwApplyGen2Settings_DoWork);
                bgwApplyGen2Settings.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgwApplyGen2Settings_RunWorkerCompleted);
                xctkbiBusyIndicator.IsBusy = false;

                xctkTagUIBusyIndicator.IsBusy = false;

            }
            catch (Exception bonjEX)
            {
                Mouse.SetCursor(Cursors.Arrow);
                if (-1 != bonjEX.Message.IndexOf("80040154 Class not registered"))
                {
                    //Several factors such as corrupted files, security program conflict, and third-party application issues can raise this exception.
                    bonjour.IsBonjourServicesInstalled = false;
                    if (rdbtnNetworkConnection.IsChecked == true)
                        btnRefreshReadersList.Visibility = System.Windows.Visibility.Collapsed;
                    //do nothing
                }
                else
                {
                    if (bonjEX.Message.Contains("0x80004005"))
                    {
                        //When Bonjour service is unavailable or not is running state.
                        bonjour.IsBonjourServicesInstalled = false;
                        if (rdbtnNetworkConnection.IsChecked == true)
                            btnRefreshReadersList.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        MessageBox.Show(bonjEX.Message);
                    }
                }
            }
            finally
            {
                btnConnectExpander_Click(null, null);
            }
        }

        #region ExceptionHandling
        /// <summary>
        /// Logging the error messages into the file
        /// </summary>
        /// <param name="message"></param>
        void Onlog(string message)
        {
            if (!message.Contains("Index was outside the bounds of the array") && !message.Contains("packet size is too big"))
            {
                if (!string.IsNullOrWhiteSpace(uri))
                    log.Error("[" + uri.ToString() + "]: " + message.Trim());
                else
                    log.Error(message.Trim());
            }
        }

        void Onlog(Exception ex)
        {
            try
            {
                bool disconnectReader = false;
                if (!string.IsNullOrWhiteSpace(ex.Message))
                {
                    if (!(ex is NullReferenceException || ex is IndexOutOfRangeException) && !ex.Message.Contains("ItemsControl"))
                    {
                        Onlog(ex.Message);
                        Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            if (ex.Message.ToLower().Contains("the operation has timed out.") || ex.Message.ToLower().Contains("timeout") || ex.Message.ToLower().Contains("the device is not connected"))
                            {
                                MessageBox.Show("Connection to the reader is lost. Disconnecting the reader.", "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                                disconnectReader = true;
                            }
                            else if (ex.Message.ToLower().Contains("device was reset externally") || ex is ThingMagic.FAULT_TM_ASSERT_FAILED_Exception)
                            {
                                MessageBox.Show(ex.Message + ". Disconnecting the reader.", "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                                disconnectReader = true;
                            }
                            if (!btnConnect.Content.ToString().Equals("Connect") && disconnectReader)
                            {
                                DisconnectReader();
                            }
                        }));
                    }
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Application.DispatcherUnhandledException will handle exceptions thrown on the main UI thread in a WPF application
        /// </summary>
        void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is ReaderCommException)
            {
                Onlog(e.Exception);
                MessageBox.Show("Unable to communicate through COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception is InvalidOperationException)
            {
                Onlog(e.Exception);
                //MessageBox.Show("This operation is not supported");
            }
            else if (e.Exception is IOException)
            {
                Onlog(e.Exception);
                //MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        /// <summary>
        /// AppDomain.UnhandledException will handle exceptions thrown on any thread and never caught
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            //Onlog(ex.Message + ". Exceptions thrown on the main UI thread in a URA2 application");
            //Onlog(ex);
            if (ex.Message.Contains("The device does not recognize the command."))
            {
                MessageBox.Show(ex.Message + "Due to unavoidable error, restarting the application",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Environment.Exit(0);
            }
            else
            {
                while (e.IsTerminating)
                {
                    Thread.Sleep(10000);
                }
            }
        }

        /// <summary>
        /// This exception is thrown when antenna for tag operation is not selected
        /// </summary>
        public class TagOpAntennaException : ReaderException
        {
            public TagOpAntennaException(string message) : base(message) { }
        }

        /// <summary>
        /// This exception is thrown when protocol for tag operation is not selected
        /// </summary>
        public class TagOpProtocolException : ReaderException
        {
            public TagOpProtocolException(string message) : base(message) { }
        }

        /// <summary>
        /// Synchronous Reads Exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ReadException(Object sender, ReaderExceptionEventArgs e)
        {
            if (e.ReaderException is ReaderCodeException)
            {
                if ((((ReaderCodeException)e.ReaderException).Code == 0x504))
                {
                    DisplayMessageOnStatusBar("Over Temperature", Brushes.Red);
                    TimeSpan ts = DateTime.Now - previousTemperatureErrorTime;
                    if (ts.TotalSeconds < 45)
                    {
                        return;
                    }
                    previousTemperatureErrorTime = DateTime.Now;
                }
                if ((((ReaderCodeException)e.ReaderException).Code == 0x501))
                {
                    DisplayMessageOnStatusBar("Channel Occupied", Brushes.Red);
                }
            }
            else
            {
                // Clear previous shown error message on status bar
                ClearMessageOnStatusBar();
            }

            if (e.ReaderException.Message.Equals("Other Gen2 error"))
            {
                // Display error message on the status bar
                DisplayMessageOnStatusBar("Other Gen2 error", Brushes.Red);
                return;
            }

            // Log the exception
            Onlog(e.ReaderException);

            if (e.ReaderException.Message.Equals("Operation not supported. M5e does not support zero-length read."))
            {
                // Display error message on the status bar
                DisplayMessageOnStatusBar(e.ReaderException.Message, Brushes.Red);
                return;
            }

            if (e.ReaderException is FAULT_TAG_ID_BUFFER_FULL_Exception)
            {
                // Display error message on the status bar
                DisplayMessageOnStatusBar("Tag ID Buffer Full", Brushes.Red);
            }
            else if ((e.ReaderException.Message.Contains("The operation has timed out.")))
            {
                // Display error message on the status bar
                DisplayMessageOnStatusBar(e.ReaderException.Message, Brushes.Red);
            }
            else if (e.ReaderException.Message.Contains("The port '" + uri + "' does not exist."))
            {
                // Display error message on the status bar
                DisplayMessageOnStatusBar(e.ReaderException.Message, Brushes.Red);
            }
            else if ((e.ReaderException is ReaderCodeException)
                && ((((ReaderCodeException)e.ReaderException).Code == 0x504)
                || (((ReaderCodeException)e.ReaderException).Code == 0x501)
                || (((ReaderCodeException)e.ReaderException).Code == 0x505)))
            {
                switch (((ReaderCodeException)e.ReaderException).Code)
                {
                    case 0x504:
                        warningText = "Over Temperature";
                        break;
                    case 0x505:
                        warningText = "High Return Loss";
                        break;
                    case 0x501:
                        warningText = "Channel Occupied";
                        break;
                    default:
                        warningText = "warning";
                        break;

                }
                // Display custom message box with "Don't ask me again" check-box for all modules
                // its not apparent enough to show the error only in status bar and some 
                // customers simply think it slow performance.
                    if (warningText == "Over Temperature")
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            // If 'don't ask me again' is checked. Don't display the message just
                            // log the message and show the error in status bar
                            if (!CustomizedMessageBox.DoNotAskMeAgainChecked)
                            {
                                if (this != CustomizedMessageBox.Owner)
                                {
                                    CustomizedMessageBox.Owner = this;
                                }
                                if (!CustomizedMessageBox.MessageBoxOpened)
                                {
                                    CustomizedMessageBox.Show();
                                }
                            }
                            //else
                            //{
                            //GUIshowWarning();
                            //}
                        }));

                    }
                    if ((warningText == "Channel Occupied"))
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            // If 'don't ask me again' is checked. Don't display the message just
                            // log the message and show the error in status bar
                            if (!channelOccupiedMessageBox.DoNotAskMeAgainChecked)
                            {
                                if (this != channelOccupiedMessageBox.Owner)
                                {
                                    channelOccupiedMessageBox.Owner = this;
                                }
                                if (!channelOccupiedMessageBox.MessageBoxOpened)
                                {
                                    channelOccupiedMessageBox.Show();
                                }
                            }
                            //else
                            //{
                            //GUIshowWarning();
                            //}
                        }));
                    }

            }
            else if ((-1 != (e.ReaderException.Message.IndexOf("The I/O operation has been aborted"
                       + " because of either a thread exit or an application request."))))
            {
                MessageBox.Show(e.ReaderException.Message + "The port '" + uri + "' does not exist.",
                                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Disconnect the reader when exception is received
                DisconnectReader();
            }
            else if ((-1 != e.ReaderException.Message.IndexOf("Specified port does not exist")))
            {
                if (isReaderConnected)
                {
                    isReaderConnected = false;
                    MessageBox.Show("Connection to the reader is lost. Disconnecting the reader.", "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Disconnect the reader when exception is received
                DisconnectReader();
            }
            else if (-1 != e.ReaderException.Message.IndexOf("The port is closed."))
            {
                MessageBox.Show(e.ReaderException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if ((e.ReaderException.Message.Contains("Connection Lost"))
                    || (e.ReaderException.Message.Contains("Request timed out")))
            {
                if (!isAsyncReadGoingOn)
                {
                    btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                    {
                        btnConnect.Content = "Disconnect";
                        EnableDisableSyncReadOptions(true);
                        btnConnect_Click(sender, new RoutedEventArgs());
                    }));
                    // Display error message on the status bar
                    DisplayMessageOnStatusBar("Connection lost", Brushes.Red);
                }
                else if (isAsyncReadGoingOn)
                {
                    HandleAsyncReadException(e.ReaderException);
                    if (isStopReadingBtnPressed)
                    {
                        btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            Mouse.SetCursor(Cursors.AppStarting);
                            btnConnect.Content = "Disconnect";
                            btnConnect_Click(sender, new RoutedEventArgs());
                            // Remove any older status messages if exists
                            DisplayMessageOnStatusBar(e.ReaderException.Message, Brushes.Red);
                        }));
                    }
                    else
                    {
                        ++connectionLostCount;
                        if (connectionLostCount == 1)
                        {
                            // Consider the elapsed time till the first occurrence of the 
                            // connection lost exception and not till we receive the 5th 
                            // connection lost exception
                            Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                continuousreadElapsedTime = CalculateElapsedTime();
                            }));
                        }
                        // Disable the read/stop-reading button when connection is lost
                        btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            btnRead.IsEnabled = false;
                            cmbFixedReaderAddr.Text = uri;
                        }));
                        // Wait for 5 connection lost exception. If 5 exceptions, 
                        // assume reader has been restarted. hence restart the URA
                        if (connectionLostCount >= 5)
                        {
                            if (null != objReader)
                            {
                                objReader.Destroy();
                                DisconnectReader();
                            }
                            objReader = null;
                            //Reconnect the reader
                            ReconnectReader(sender, new RoutedEventArgs());
                            // Reset connection lost exception counter
                            connectionLostCount = 0;
                        }
                    }
                }
                else
                {
                    try
                    {
                        stopReading();
                    }
                    finally
                    {
                        ShutdownStartReads(e.ReaderException);
                    }
                }
            }
        }

        /// <summary>
        /// Displays error message or message on the status bar
        /// </summary>
        /// <param name="errorMessage"> error message or message to be displayed</param>
        /// <param name="colour">color to indicate the severity of the error</param>
        private void DisplayMessageOnStatusBar(string errorMessage, SolidColorBrush colour)
        {
            warningText = errorMessage;
            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                GUIshowWarning(colour);
            }));
        }

        /// <summary>
        /// Clear previously shown error message or message on the status bar
        /// </summary>
        private void ClearMessageOnStatusBar()
        {
            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                GUIturnoffWarning();
            }));
        }

        /// <summary>
        /// Disconnect the reader when I/O operation has been aborted 
        /// or the operation has timed out exception is occurred.
        /// </summary>
        private void DisconnectReader()
        {

            // Stop read rate per sec calculation
            readRatePerSec.Stop();
            isReadStopped.Stop();
            btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                if (!lblshowStatus.Content.ToString().Contains("Disconnected"))
                {
                    isAsyncReadGoingOn = false;
                    btnConnect.Content = "Disconnect";
                    btnRead.Content = "Read";
                    btnRead.ToolTip = "Start Async Read";
                    rdBtnReadOnce.IsEnabled = true;
                    AntennasGroupBox.IsEnabled = true;
                    grpGPIOBehaviour.IsEnabled = true;
                    ProtocolsGroupBox.IsEnabled = true;
                    ReadDataGroupBox.IsEnabled = true;
                    FilterGroupBox.IsEnabled = true;
                    // Disconnect the reader
                    btnConnect_Click(this, new RoutedEventArgs());
                    objReader = null;
                }
            }));
        }

        /// <summary>
        /// It stores the Embedded readdata settings
        /// </summary>
        /// <param name="readUpdate">Set true to store setting and false to get the stored data</param>
        private void CacheEmbeddedDataSettings(bool readUpdate)
        {
            if (readUpdate)
            {
                #region Reset value
                enableEmbedded = false;
                startAddress = 0;
                readLength = 0;
                memBank = Gen2.Bank.TID;
                enableUnique = false;
                enableFailData = false;
                #endregion
                //region stores data
                enableEmbedded = (bool)chkEmbeddedReadData.IsChecked;
                if (enableEmbedded)
                {
                    startAddress = Convert.ToUInt32(Utilities.CheckHexOrDecimal(txtembReadStartAddr.Text));
                    readLength = Convert.ToInt32(Utilities.CheckHexOrDecimal(txtembReadLength.Text));
                    switch (cbxReadDataBank.Text)
                    {
                        case "EPC":
                            memBank = Gen2.Bank.EPC;
                            break;
                        case "TID":
                            memBank = Gen2.Bank.TID;
                            break;
                        case "User":
                            memBank = Gen2.Bank.USER;
                            break;
                        case "Reserved":
                            memBank = Gen2.Bank.RESERVED;
                            break;
                    }
                    enableUnique = (bool)chkUniqueByData.IsChecked;
                    if (enableFailData)
                    {
                        enableFailData = (bool)chkShowFailedDataReads.IsChecked;
                    }
                }
            }
            else
            {
                if (enableEmbedded)
                {
                    chkEmbeddedReadData.IsChecked = enableEmbedded;
                    txtembReadStartAddr.Text = startAddress.ToString();
                    txtembReadLength.Text = readLength.ToString();
                    switch (memBank)
                    {
                        case Gen2.Bank.EPC:
                            cbxReadDataBank.SelectedIndex = 2;
                            break;
                        case Gen2.Bank.RESERVED:
                            cbxReadDataBank.SelectedIndex = 1;
                            break;
                        case Gen2.Bank.TID:
                            cbxReadDataBank.SelectedIndex = 0;
                            break;
                        case Gen2.Bank.USER:
                            cbxReadDataBank.SelectedIndex = 3;
                            break;
                    }
                    chkUniqueByData.IsChecked = enableUnique;
                    if (enableUnique)
                    {
                        chkShowFailedDataReads.IsChecked = enableFailData;
                    }
                }
            }
        }

        /// <summary>
        /// Main function store or updates the read settings
        /// </summary>
        /// <param name="readUpdate">Set true to store setting and false to get the stored data</param>
        /// <param name="ant">Store selected antenna list</param>
        /// <param name="protocol"> Store selected protocol</param>
        /// <param name="fastSearch">Store Fast search status</param>
        private void StoreReaderSettings(bool readUpdate, List<int> ant, List<TagProtocol> protocol, bool fastSearch)
        {
            #region Variables
            CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox, Ant5CheckBox, Ant6CheckBox, Ant7CheckBox, Ant8CheckBox,
                                            Ant9CheckBox, Ant10CheckBox, Ant11CheckBox, Ant12CheckBox, Ant13CheckBox, Ant14CheckBox, Ant15CheckBox, Ant16CheckBox,
                                            Ant17CheckBox, Ant18CheckBox, Ant19CheckBox, Ant20CheckBox, Ant21CheckBox, Ant22CheckBox, Ant23CheckBox, Ant24CheckBox,
                                            Ant25CheckBox, Ant26CheckBox, Ant27CheckBox, Ant28CheckBox, Ant29CheckBox, Ant30CheckBox, Ant31CheckBox, Ant32CheckBox,
                                            Ant33CheckBox, Ant34CheckBox, Ant35CheckBox, Ant36CheckBox, Ant37CheckBox, Ant38CheckBox, Ant39CheckBox, Ant40CheckBox,
                                            Ant41CheckBox, Ant42CheckBox, Ant43CheckBox, Ant44CheckBox, Ant45CheckBox, Ant46CheckBox, Ant47CheckBox, Ant48CheckBox,
                                            Ant49CheckBox, Ant50CheckBox, Ant51CheckBox, Ant52CheckBox, Ant53CheckBox, Ant54CheckBox, Ant55CheckBox, Ant56CheckBox,
                                            Ant57CheckBox, Ant58CheckBox, Ant59CheckBox, Ant60CheckBox, Ant61CheckBox, Ant62CheckBox, Ant63CheckBox, Ant64CheckBox};

            CheckBox[] protocolBoxes = { gen2CheckBox, iso6bCheckBox, isoUcodeCheckbox, ipx64CheckBox, ipx256CheckBox, ataCheckBox, iso14443aCheckBox, iso14443bCheckBox, iso15693CheckBox, iso18092CheckBox, felicaCheckBox, iso180003mode3CheckBox, lf125KHZCheckBox, lf134KHZCheckBox };
            #endregion
            #region This region stores the UI elements data.
            if (readUpdate)
            {
                //Reset the lists
                antList.Clear();
                protocolList.Clear();
                storeFastSearch = false;
                storeApplyFilter1 = storeApplyFilter2 = storeApplyFilter3 = false;
                storeFilter1 = StoreFilter2 = storeFilter3 = null;
                storeFilterData1 = storeFilterData2 = storeFilterData3 = "";
                #region Store antennas
                foreach (int item in ant)
                {
                    antList.Add(item);
                }
                #endregion
                #region Store Protocols
                foreach (TagProtocol item in protocol)
                {
                    protocolList.Add(item);
                }
                #endregion
                #region Store Fast search
                storeFastSearch = fastSearch;
                #endregion
                #region Store embedded read
                CacheEmbeddedDataSettings(true);
                #endregion
                #region Filter region
                storeApplyFilter1 = (bool)chkApplyFilter1.IsChecked;
                if (storeApplyFilter1)
                {
                    storeFilter1 = MultiFilters1;
                    storeFilterData1 = MultiFilters1.txtFilterData.Text;
                    storeApplyFilter2 = (bool)chkApplyFilter2.IsChecked;
                    if (storeApplyFilter2)
                    {
                        StoreFilter2 = MultiFilters2;
                        storeFilterData2 = MultiFilters2.txtFilterData.Text;
                        storeApplyFilter3 = (bool)chkApplyFilter3.IsChecked;
                        if (storeApplyFilter3)
                        {
                            storeFilter3 = MultiFilters3;
                            storeFilterData3 = MultiFilters3.txtFilterData.Text;
                        }
                    }

                }
                #endregion

            }
            #endregion
            #region This region update the UI elements.
            else
            {
                #region Antenna
                for (int i = 0; i < 64; i++)
                {
                    if (antList.Contains(i + 1))
                    {
                        antennaBoxes[i].IsChecked = true;
                        antennaBoxes[i].IsEnabled = true;
                    }
                    else
                    {
                        antennaBoxes[i].IsChecked = false;
                    }
                }
                #endregion
                #region Protocol
                foreach (TagProtocol item in protocolList)
                {
                    switch (item)
                    {
                        case TagProtocol.GEN2:
                            gen2CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO180006B:
                            iso6bCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.IPX64:
                            ipx64CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.IPX256:
                            ipx256CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ATA:
                            ataCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO14443A:
                            iso14443aCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO14443B:
                            iso14443bCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO15693:
                            iso15693CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO18092:
                            iso18092CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.FELICA:
                            felicaCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO180003M3:
                            iso180003mode3CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.LF125KHZ:
                            lf125KHZCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.LF134KHZ:
                            lf134KHZCheckBox.IsChecked = true;
                            break;
                    }
                }
                #endregion
                #region Fast search
                if (storeFastSearch)
                {
                    chkEnableFastSearch.IsChecked = true;
                }
                else
                {
                    chkEnableFastSearch.IsChecked = false;
                }
                #endregion
                #region Update Embedded read
                CacheEmbeddedDataSettings(false);
                #endregion
                #region Update Filter
                if (storeApplyFilter1)
                {
                    chkApplyFilter1.IsChecked = storeApplyFilter1;
                    MultiFilters1 = storeFilter1;
                    MultiFilters1.txtFilterData.Text = storeFilterData1;
                    if (storeApplyFilter2)
                    {
                        chkApplyFilter2.IsChecked = storeApplyFilter2;
                        MultiFilters2 = StoreFilter2;
                        MultiFilters2.txtFilterData.Text = storeFilterData2;
                        if (storeApplyFilter3)
                        {
                            chkApplyFilter3.IsChecked = storeApplyFilter3;
                            MultiFilters3 = storeFilter3;
                            MultiFilters3.txtFilterData.Text = storeFilterData3;
                        }
                    }
                }
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// Attempt to reconnect to the reader when connection is lost
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReconnectReader(Object sender, RoutedEventArgs e)
        {
            lock (new Object())
            {
                int retryCount;
                for (retryCount = 1; retryCount < 6; retryCount++)
                {
                    try
                    {
                        restoreReadSetting = true;
                        objReader = Reader.Create(string.Concat("tmr://", uri));
                        Onlog("Attempting to reconnect - " + (retryCount) + ", after connection lost");
                        // Display error message on the status bar
                        DisplayMessageOnStatusBar("Attempting to reconnect - " + (retryCount), Brushes.Red);
                        #region Just pinging the reader to now if the reader is ready to connect or not. Because the URA connect will call all the UI element function which increases the connect time and we dont want reconfigure UI each time we try to connect the reader.
                        objReader.Connect();
                        objReader.Destroy();
                        #endregion
                        objReader = null;
                        warningText = "";
                        lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            cmbFixedReaderAddr.Text = uri;
                            // Enable the read/stop-reading button when URA is able to connect to the reader.
                            btnRead.IsEnabled = true;
                            GUIturnoffWarning();
                            btnRead.Content = "Read";
                            AntennasGroupBox.IsEnabled = true;
                            grpGPIOBehaviour.IsEnabled = true;
                            ProtocolsGroupBox.IsEnabled = true;
                            ReadDataGroupBox.IsEnabled = true;
                            FilterGroupBox.IsEnabled = true;
                            if ((bool)rdBtnReadContinuously.IsChecked)
                            {
                                btnConnect_Click(this, new RoutedEventArgs());// this function should be called after all the Flags have be reseted.
                                OnStartRead("Reconnecting");
                            }
                        }));
                        break;
                    }
                    catch (Exception ex)
                    {
                        Onlog(ex);
                        continue;
                    }
                }

                if (retryCount >= 5)
                {
                    btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                    {
                        isReconnectFailed = true;
                        btnRead.Content = "Stop Reading";
                        btnConnect.Content = "Disconnect";
                        btnConnect_Click(sender, e);
                        if (objReader != null)
                        {
                            objReader.Destroy();
                            DisconnectReader();
                            objReader = null;
                        }

                    }));
                }
                else
                {
                    log.Info("[" + uri.ToString() + "]: " + "Reader Connected Successfully.");
                }
            }
        }

        /// <summary>
        /// Handle connection lost exception.
        /// </summary>
        /// <param name="ex"></param>
        private void HandleAsyncReadException(Exception ex)
        {
            bool alreadyShowing;
            lock (showingAsyncReadExceptionMessageBoxLock)
            {
                alreadyShowing = showingAsyncReadExceptionMessageBox;
                showingAsyncReadExceptionMessageBox = true;
            }
            if (!alreadyShowing)
            {
                this.Dispatcher.Invoke(new ThreadStart(delegate()
                {
                    Mouse.SetCursor(Cursors.Arrow);
                    readRatePerSec.Stop();
                    isReadStopped.Stop();

                    if (ex.Message.Contains("Connection Lost"))
                    {
                        // Display error message on the status bar
                        DisplayMessageOnStatusBar(ex.Message + "!. Trying to reconnect...", Brushes.Red);
                    }
                    else
                    {
                        // Display error message on the status bar
                        DisplayMessageOnStatusBar(ex.Message, Brushes.Red);
                    }

                }));
                lock (showingAsyncReadExceptionMessageBoxLock)
                {
                    showingAsyncReadExceptionMessageBox = false;
                }
            }
        }

        /// <summary>
        /// Shows dialog box when Reader Comm exception is found
        /// </summary>
        /// <param name="exp">Reader Comm Exception</param>
        /// <param name="e"></param>
        public void handleReaderCommException(ReaderCommException exp, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Arrow);
            switch (MessageBox.Show(exp.Message.ToString() + "\nDo you want to reset the reader now?",
                "Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error))
            {
                case MessageBoxResult.Yes:
                    {
                        // disconnectReader_Click(this, e);
                        btnConnect_Click(this, e);
                        break;
                    }
                case MessageBoxResult.No:
                    {
                        break;
                    }
                case MessageBoxResult.Cancel:
                    {
                        exitURA_Click(this, e);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        #endregion ExceptionHandling

        #region Initialize

        /// <summary>
        /// Populate Column selection combo-box in display options 
        /// </summary>
        private void InitializeColumnSelectionCbx()
        {
            selectedColumnList = new ObservableCollection<ColumnSelectionForTagResult>();
            selectedColumnList.Add(new ColumnSelectionForTagResult(false, "Protocol"));
            selectedColumnList.Add(new ColumnSelectionForTagResult(false, "Antenna"));
            if (!(model.Equals("M3e")))
            {
                selectedColumnList.Add(new ColumnSelectionForTagResult(false, "Frequency"));
                selectedColumnList.Add(new ColumnSelectionForTagResult(false, "Phase"));
                if (rdbtnNetworkConnection.IsChecked == false)
                {
                    selectedColumnList.Add(new ColumnSelectionForTagResult(false, "GPIO"));
                }
            }
            //selectedColumnList.Add(new ColumnSelectionForTagResult(false, "BrandID"));
            cbxcolumnSelection.ItemsSource = selectedColumnList;
        }

        /// <summary>
        /// // Initialize the dummy columns used when docking
        /// </summary>
        private void InitializeDummyClmnsforDocking()
        {
            column1CloneForLayer0 = new ColumnDefinition();
            column1CloneForLayer0.SharedSizeGroup = "column1";
            column2CloneForLayer0 = new ColumnDefinition();
            column2CloneForLayer0.SharedSizeGroup = "column2";
            column2CloneForLayer1 = new ColumnDefinition();
            column2CloneForLayer1.SharedSizeGroup = "column2";
        }

        /// <summary>
        /// Populate reader uri box with the port numbers
        /// </summary>
        private void InitializeReaderUriBox()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                if ((bool)rdbtnLocalConnection.IsChecked)
                {
                    List<string> portNames = GetComPortNames();
                    cmbReaderAddr.ItemsSource = "";
                    if (portNames.Count > 0)
                    {
                        cmbReaderAddr.ItemsSource = portNames;
                        cmbReaderAddr.Text = portNames[0];
                    }
                }
                else if (bonjour.IsBonjourServicesInstalled)
                {
                    bonjour.BackgroundNotifierCallbackCount = 0;
                    if (bonjour.Browser != null)
                    {
                        bonjour.Browser.Stop();
                        bonjour.ServicesList.Clear();
                    }

                    bonjour.HostNameIpAddress.Clear();
                    cmbFixedReaderAddr.Items.Clear();
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
                Mouse.SetCursor(Cursors.Arrow);
            }
            catch (Exception bonjEX)
            {
                Mouse.SetCursor(Cursors.Arrow);
                throw bonjEX;
            }
        }

        /// <summary>
        /// Returns the COM port names as list
        /// </summary>
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
                                log.Info("Device detected on Port: " + queryObj["Name"].ToString());
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
                            //Reader is throwing error for connect so we are not going show in the reader name list..
                            log.Info("Detected device" + queryObj["Name"].ToString() + " has Generic Name and Failed to connect.");
                        }
                    }
                }
            }

            #region Store to main list
            for (int i = 0; i < masterPortList.Count; i++)
            {
                string a = masterPortList[i];
                if (!(portNames.Contains(masterPortList[i])))
                {
                    masterPortList.Remove(masterPortList[i]);
                }
            }
            for (int i = 0; i < portNames.Count; i++)
            {
                string a = portNames[i];
                if (!(masterPortList.Contains(portNames[i])))
                {
                    masterPortList.Add(portNames[i]);
                }
            }
            #endregion

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
        /// Populate optimal settings based on reader
        /// </summary>
        private void InitializeOptimalSettings()
        {
            OptimalReaderSettings = new Dictionary<string, string>();
            Gen2SettingChanged = new Dictionary<string, bool>();
            if (!(model.Contains("M3e")))
            {
                Gen2Settings_InitialLoad();
            }
            Gen2SettingChanged.Clear();
            if (!M7eFamilyList.Contains(model))
            {
                Gen2SettingChanged.Add("BLF", true);
                Gen2SettingChanged.Add("TARI", true);
                Gen2SettingChanged.Add("TAGENCODING", true);
            }
            Gen2SettingChanged.Add("SESSION", true);
            Gen2SettingChanged.Add("TARGET", true);
            Gen2SettingChanged.Add("Q", true);
            Gen2SettingChanged.Add("initQ", true);
            if (M7eFamilyList.Contains(model) && isRFModeSupp)
            {
                Gen2SettingChanged.Add("RFMODE", true);
            }
        }

        /// <summary>
        /// Get all the current gen2 settings and set the radio buttons accordingly
        /// </summary>
        private void Gen2Settings_InitialLoad()
        {
            //Get all the current settings and set the radio buttons accordingly
            if (!M7eFamilyList.Contains(model))
            {
                try
                {
                    Gen2.LinkFrequency blf = (Gen2.LinkFrequency)objReader.ParamGet("/reader/gen2/BLF");
                    switch (blf)
                    {
                        case Gen2.LinkFrequency.LINK250KHZ:
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ"; break;
                        case Gen2.LinkFrequency.LINK320KHZ:
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK320KHZ"; break;
                        case Gen2.LinkFrequency.LINK640KHZ:
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK640KHZ"; ; break;
                        default:
                            OptimalReaderSettings.Add("/reader/gen2/BLF", ""); break;
                    }
                }
                catch (ArgumentException ex)
                {
                    if (ex.Message.Contains("Unknown Link Frequency"))
                    {
                        MessageBox.Show("Unknown Link Frequency found, Reverting to defaults.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OptimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ";
                    }
                    else
                    {
                        OptimalReaderSettings.Add("/reader/gen2/BLF", "");
                    }
                }

                try
                {

                    Gen2.Tari tariVal = (Gen2.Tari)objReader.ParamGet("/reader/gen2/Tari");
                    switch (tariVal)
                    {
                        case Gen2.Tari.TARI_6_25US:
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_6_25US"; break;
                        case Gen2.Tari.TARI_12_5US:
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_12_5US"; break;
                        case Gen2.Tari.TARI_25US:
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_25US"; break;
                        default:
                            OptimalReaderSettings.Add("/reader/gen2/tari", ""); break;
                    }
                }
                catch (ArgumentException)
                {
                    OptimalReaderSettings.Add("/reader/gen2/tari", "");
                }
                catch (ReaderCodeException)
                {
                }

                try
                {
                    Gen2.TagEncoding tagencoding = (Gen2.TagEncoding)objReader.ParamGet("/reader/gen2/tagEncoding");
                    switch (tagencoding)
                    {
                        case Gen2.TagEncoding.FM0:
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "FM0"; break;
                        case Gen2.TagEncoding.M2:
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M2"; break;
                        case Gen2.TagEncoding.M4:
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M4"; break;
                        case Gen2.TagEncoding.M8:
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M8"; break;
                        default:
                            OptimalReaderSettings.Add("/reader/gen2/tagEncoding", ""); break;
                    }
                }
                catch (ArgumentException)
                {
                    OptimalReaderSettings.Add("/reader/gen2/tagEncoding", "");
                }
            }
            try
            {
                Gen2.Session session = (Gen2.Session)objReader.ParamGet("/reader/gen2/session");
                switch (session)
                {
                    case Gen2.Session.S0:
                        OptimalReaderSettings["/reader/gen2/session"] = "S0"; break;
                    case Gen2.Session.S1:
                        OptimalReaderSettings["/reader/gen2/session"] = "S1"; break;
                    case Gen2.Session.S2:
                        OptimalReaderSettings["/reader/gen2/session"] = "S2"; break;
                    case Gen2.Session.S3:
                        OptimalReaderSettings["/reader/gen2/session"] = "S3"; break;
                    default:
                        OptimalReaderSettings.Add("/reader/gen2/session", ""); break;
                }
            }
            catch (ArgumentException)
            {
                OptimalReaderSettings.Add("/reader/gen2/session", "");
            }
            try
            {
                Gen2.Target target = (Gen2.Target)objReader.ParamGet("/reader/gen2/Target");
                switch (target)
                {
                    case Gen2.Target.A:
                        OptimalReaderSettings["/reader/gen2/target"] = "A"; break;
                    case Gen2.Target.B:
                        OptimalReaderSettings["/reader/gen2/target"] = "B"; break;
                    case Gen2.Target.AB:
                        OptimalReaderSettings["/reader/gen2/target"] = "AB"; break;
                    case Gen2.Target.BA:
                        OptimalReaderSettings["/reader/gen2/target"] = "BA"; break;
                    default: OptimalReaderSettings.Add("Target", ""); break;
                }
            }
            catch (FeatureNotSupportedException)
            {
                OptimalReaderSettings.Add("Target", "");
            }
            try
            {
                Gen2.Q qval = (Gen2.Q)objReader.ParamGet("/reader/gen2/q");

                if (qval.GetType() == typeof(Gen2.DynamicQ))
                {
                    OptimalReaderSettings["/reader/gen2/q"] = "DynamicQ";
                }
                else if (qval.GetType() == typeof(Gen2.StaticQ))
                {
                    Gen2.StaticQ stqval = (Gen2.StaticQ)qval;
                    OptimalReaderSettings["/reader/gen2/q"] = "StaticQ";
                    int countQ = Convert.ToInt32(((Gen2.StaticQ)qval).InitialQ);
                    OptimalReaderSettings["/application/performanceTuning/staticQValue"] = countQ.ToString();
                }
                else
                {
                    OptimalReaderSettings.Add("/reader/gen2/q", "");
                }
            }
            catch (FeatureNotSupportedException)
            {
                OptimalReaderSettings.Add("/reader/gen2/q", "");
            }

            if (!(model.Equals("Mercury6")))
            {
                try
                {

                    Gen2.InitQ qval = (Gen2.InitQ)objReader.ParamGet("/reader/gen2/initQ");
                    if (qval != null)
                    {
                        OptimalReaderSettings["/reader/gen2/initQ"] = qval.qEnable.ToString();
                        OptimalReaderSettings["/application/performanceTuning/initQValue"] = qval.initialQ.ToString();
                    }

                }
                catch (FeatureNotSupportedException)
                {
                    OptimalReaderSettings.Add("/reader/gen2/initQ", "");
                }
            }

            if (M7eFamilyList.Contains(model) && isRFModeSupp)
            {
                try
                {
                    Gen2.RFMode rfMode = (Gen2.RFMode)objReader.ParamGet("/reader/gen2/rfMode");
                    OptimalReaderSettings["/reader/gen2/rfMode"] = rfMode.ToString();
                }
                catch (Exception ex)
                {
                    OptimalReaderSettings.Add("/reader/gen2/rfMode", "");
                }
            }

        }

        /// <summary>
        /// Set maximum read power and initialize max and min read power to the read power slider.
        /// </summary>
        private void InitializeRdPwrSldrMaxNMinValue()
        {
            try
            {
                if (model.Contains("M3e"))
                {
                    // Getting Read and write power in M3e is not supported.
                }
                else
                {
                    sldrReadPwr.Maximum = sldrWritePwr.Maximum = (Convert.ToDouble(objReader.ParamGet("/reader/radio/powerMax").ToString()) / 100);
                    sldrReadPwr.Minimum = sldrWritePwr.Minimum = (Convert.ToDouble(objReader.ParamGet("/reader/radio/powerMin").ToString()) / 100);
                    if (regioncombo.SelectedItem.ToString().Equals("NA") && (objReader.ParamGet("/reader/version/model").ToString().Equals("Astra-EX")))
                    {
                        if (sldrReadPwr.Maximum > 30.0 || sldrWritePwr.Maximum > 30.0)
                        {
                            // Since Astra-EX reader with NA region don't support maximum read power
                            // i.e 31.5 dBm on 1st antenna. Hence setting read power to 30 dBm for 
                            // all the antenna. 
                            sldrReadPwr.Value = 30.0;
                            sldrWritePwr.Value = 30.0;
                        }
                    }
                    else if (model.Equals("M6e Micro USBPro"))
                    {
                        sldrReadPwr.Value = ValidatePowerValue(20.0, sldrReadPwr.Minimum, sldrReadPwr.Value);
                        sldrWritePwr.Value = ValidatePowerValue(20.0, sldrWritePwr.Minimum, sldrWritePwr.Value);
                    }
                    else
                    {
                        sldrReadPwr.Value = ValidatePowerValue(sldrReadPwr.Maximum, sldrReadPwr.Minimum, sldrReadPwr.Value);
                        sldrWritePwr.Value = ValidatePowerValue(sldrWritePwr.Maximum, sldrWritePwr.Minimum, sldrWritePwr.Value);
                    }
                    
                }
            }
            catch (ReaderException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private double ValidatePowerValue(double maxPower, double minPower, double previousValue)
        {
            if (previousValue <= maxPower && previousValue >= minPower && !(isURAConnectInProgress))
            {
                return previousValue;
            }
            else
            {
                if (previousValue < minPower && !(isURAConnectInProgress))
                {
                    return minPower;
                }
                else
                {
                    return maxPower;
                }
            }
        }

        #endregion Initialize

        #region EventHandler

        /// <summary>
        /// Open Help File on Help file toolbar button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string location = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                location = System.IO.Path.Combine(location, "URAHelp.chm");
                System.Diagnostics.Process.Start(location);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Opening Help File.\n" + ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                //Onlog("Error Opening Help File.\n" + ex.Message);
            }
        }

        /// <summary>
        /// Open Help File on press of "F1"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                string location = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                location = System.IO.Path.Combine(location, "URAHelp.chm");
                System.Diagnostics.Process.Start(location);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Opening Help File.\n" + ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                //Onlog("Error Opening Help File.\n" + ex.Message);
            }
        }

        /// <summary>
        /// Refresh the data grid for every dispatcher interval set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void dispatchtimer_Tick(Object sender, EventArgs args)
        {
            try
            {
                // Causes a control bound to the BindingSource to reread all the items 
                // in the list and refresh their displayed values.
                TagResults.tagagingColourCache.Clear();
                tagdb.Repaint();
                // Forces an immediate garbage collection from generation zero through
                // a specified generation.            
                // GC.Collect(1);
                // Retrieves the number of bytes currently thought to be allocated. If 
                // the forceFullCollection parameter is true, this method waits a short
                // interval before returning while the system collects garbage and finalizes
                // objects.            
                //long totalmem1 = GC.GetTotalMemory(true);
            }
            catch { }
        }

        /// <summary>
        /// Exit URA application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitURA_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfigurationForWizardFlow();
                tmrLogStopReader();
                // Don't go for tear down if the URA is not connected to any reader/module
                if ((null != objReader) && (lblshowStatus.Content.ToString() == "Connected"))
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/baudRate", 115200);
                    }
                    btnConnect_Click(sender, e);
                }
                else if (isSyncReadGoingOn)
                {
                    //Do nothing exit the application
                }
                else
                {
                    if (null != objReader)
                    {
                        if (objReader is SerialReader)
                        {
                            objReader.ParamSet("/reader/baudRate", 115200);
                        }
                        objReader.Destroy();
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
                if (objReader != null)
                {
                    objReader.Destroy();
                    objReader = null;
                }
            }
        }

        /// <summary>
        /// Copy feature implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyCtrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                string selectedText = "";
                Clipboard.SetText(selectedText);
            }
            catch { }
        }

        /// <summary>
        /// Paste feature implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pasteCtrl_Click(object sender, RoutedEventArgs e)
        {
            if (e.GetType().Equals(typeof(TextBox)))
            {
                TextBox tb = (TextBox)sender;
                tb.Paste();
            }
        }

        /// <summary>
        /// About info of the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void demoApplication_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                            String.Join("\n", new string[] {
                    String.Format("Universal Reader Assistant {0}", 
                    this.GetType().Assembly.GetName().Version.ToString()),
                    String.Format("Mercury API Version {0}", 
                    Assembly.GetAssembly(typeof(ThingMagic.Reader)).GetName().Version.ToString()),
                }),
                            "About Demo Application...",
                            MessageBoxButton.OK);
        }

        private void thingMagicReader_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string software = (string)objReader.ParamGet("/reader/version/software");
                MessageBox.Show(string.Concat("Hardware: ", objReader.ParamGet("/reader/version/model").ToString(),
                    "  ", "Software: ", software), "About ThingMagic Reader...", MessageBoxButton.OK);
            }
            catch
            {
                MessageBox.Show("Connection to ThingMagic Reader not established",
                    "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Method for updating Read Tag ID text box
        /// </summary>
        /// <param name="data">String Data</param>
        private void OutputUpdateReadTagID(TagReadData[] tags)
        {
            if (tags != null && tags.Length != 0)
            {
                foreach (TagReadData read in tags)
                {
                    tagdb.Add(read);
                }
            }
        }

        /// <summary>
        /// Validates EPC String and formats valid strings into EPC
        /// </summary>
        /// <param name="epcString">Valid/Invalid EPC String</param>
        /// <returns>Valid TagData used for selection</returns>
        public TagData validateEpcStringAndReturnTagData(string epcString)
        {
            TagData validTagData = null;
            if (epcString.Contains(" "))
            {
                validTagData = null;
            }
            else
            {
                if (epcString.Length == 0)
                {
                    validTagData = null;
                }
                else
                {
                    if (epcString.Contains("0x"))
                    {
                        validTagData = new TagData(epcString.Remove(0, 2));
                    }
                    validTagData = new TagData(epcString);
                }
            }
            return validTagData;
        }

        /// <summary>
        /// Remove error status messages after specified interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimeEvent(object sender, ElapsedEventArgs e)
        {
            myTimer.Stop();
            lblWarning.Dispatcher.Invoke(new del(GUIturnoffWarning));
        }

        /// <summary>
        /// Show warning messages on to the status bar at the bottom
        /// of the application
        /// </summary>
        void GUIshowWarning()
        {
            lblWarning.Background = Brushes.Red;
            lblWarning.Text = warningText;
        }

        /// <summary>
        /// Show warning messages on to the status bar at the bottom
        /// of the application
        /// </summary>
        /// <param name="colour">Color to indicate the severity of the error</param>
        void GUIshowWarning(SolidColorBrush colour)
        {
            lblWarning.Background = colour;
            lblWarning.Text = warningText;
        }

        /// <summary>
        /// Clear warning messages on to the status bar at the bottom
        /// of the application
        /// </summary>
        void GUIturnoffWarning()
        {
            lblWarning.Background = Brushes.Transparent;
            lblWarning.Text = "";
        }

        void updateGUIatStop()
        {
            GUIshowWarning();
            btnRead.ToolTip = "Start Async Read";
            //readTag.IsEnabled = true;
            cbxBaudRate.IsEnabled = true;
            //thingMagicReader.IsEnabled = true;
        }

        /// <summary>
        /// Show error only once when async read is going on
        /// </summary>
        /// <param name="ex"></param>
        private void ShutdownStartReads(Exception ex)
        {
            this.btnRead.Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                OnStopReadsClick();
            }));

            bool alreadyShowing;
            lock (showingAsyncReadExceptionMessageBoxLock)
            {
                alreadyShowing = showingAsyncReadExceptionMessageBox;
                showingAsyncReadExceptionMessageBox = true;
            }
            if (!alreadyShowing)
            {
                this.Dispatcher.Invoke(new ThreadStart(delegate()
                {
                    MessageBox.Show(ex.Message, "Reader Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
                lock (showingAsyncReadExceptionMessageBoxLock)
                {
                    showingAsyncReadExceptionMessageBox = false;
                }
            }
        }

        /// <summary>
        /// Check control visibility i.e if control property is set to visible return 
        /// true, and if property is set to hidden or collapsed return false
        /// </summary>
        /// <param name="obj">control</param>
        /// <returns>bool</returns>
        private bool checkControlVisibility(Control obj)
        {
            if (obj.Visibility.Equals(Visibility.Visible))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clear all the ui controls and database related to tag reads 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearReads_Click(object sender, RoutedEventArgs e)
        {
            ClearReads();
        }

        /// <summary>
        /// Clear all the ui controls and database related to tag reads 
        /// </summary>
        private void ClearReads()
        {
            Thread st = new Thread(delegate()
            {
                this.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    lock (tagdb)
                    {
                        tagdb.Clear();
                        tagdb.Repaint();
                    }
                }
                ));
                Dispatcher.Invoke(new del(delegate()
                {
                    try
                    {
                        startAsyncReadTime = DateTime.Now;
                        GUIturnoffWarning();
                        CheckBox headerCheckBox = (CheckBox)GetChildControl(TagResults.dgTagResults, "headerCheckBox");
                        headerCheckBox.IsChecked = false;
                        txtTotalTagReads.Content = "0";
                        totalUniqueTagsReadTextBox.Content = "0";
                        totalTimeElapsedTextBox.Content = "0";
                        if (objReader == null)
                        {
                            lblReaderTemperature.Content = "0" + "°C";
                        }
                        readRatePerSecondCount = 0;
                        readonceElapsedTime = 0.0;
                        txtbxReadRatePerSec.Content = "0";
                        continuousreadElapsedTime = 0.0;
                        TagResults.tagagingColourCache.Clear();
                    }
                    catch { }
                }));

            });
            st.Start();
        }

        /// <summary>
        /// Open the thingmagic support link when clicked on image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToolTip forWebsite = new ToolTip();
            forWebsite.Content = "www.jadaktech.com";
            System.Diagnostics.Process.Start("https://www.jadaktech.com");
        }

        /// <summary>
        /// Detect readers connected to each comports and suffix the name to the comports populated in the combo-box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void probeReadNames_Click(object sender, RoutedEventArgs e)
        {
            // hourglass cursor
            probeReadNames.IsEnabled = false;
            Mouse.SetCursor(System.Windows.Input.Cursors.Wait);

            try
            {
                cmbReaderAddr.ItemsSource = "";
                InitializeReaderUriBox();
                List<string> readersList = new List<string>();
                for (int count = 0; count < cmbReaderAddr.Items.Count; count++)
                {
                    if (cmbReaderAddr.Items[count].ToString().Contains("COM") || cmbReaderAddr.Items[count].ToString().Contains("com"))
                    {
                        ///Creates a Reader Object for operations on the Reader.
                        objReader = Reader.Create(string.Concat("tmr:///", cmbReaderAddr.Items[count].ToString()));

                    }
                    else
                    {
                        //Creates a Reader Object for operations on the Reader.
                        objReader = Reader.Create(string.Concat("tmr://", cmbReaderAddr.Items[count].ToString()));

                    }

                    ///Assign  reader to reader
                    //reader = reader; //(Reader)reader;
                    try
                    {
                        connectToReader(objReader);
                    }
                    catch
                    {
                        continue;
                    }

                    UInt16 id = (UInt16)objReader.ParamGet("/reader/version/productGroupID");
                    string model = (string)objReader.ParamGet("/reader/version/model");
                    if (model.Equals("M6e"))
                    {
                        readersList.Add(cmbReaderAddr.Items[count].ToString() + " " + "M6e Module");
                    }
                    else
                    {
                        readersList.Add(cmbReaderAddr.Items[count].ToString());
                    }
                    objReader.Destroy();
                }
                if (readersList.Count > 0)
                {
                    cmbReaderAddr.Items.Clear();
                    foreach (string uri in readersList)
                    {
                        cmbReaderAddr.Items.Add(uri);
                    }
                    cmbReaderAddr.SelectedIndex = 0;
                }

                //string[] services = { "_llrp._tcp", "_m4api._udp.", "_telnet._tcp." };
                //foreach (string service in services)
                //{
                //    nsBrowser.SearchForService(service, "");
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                probeReadNames.IsEnabled = true;
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        #region Docking of tool panel

        /// <summary>
        /// Toggle between docked and undocked states (Pane 1) 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void pane1Pin_Click(object sender, RoutedEventArgs e)
        {
            if (pane1Button.Visibility == Visibility.Collapsed)
                UndockPane(1);
            else
                DockPane(1);
        }

        /// <summary>
        /// Show Pane 1 when hovering over its button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void pane1Button_MouseEnter(object sender, RoutedEventArgs e)
        {
            layer1.Visibility = Visibility.Visible;
            // Adjust Z order to ensure the pane is on top:
            Grid.SetZIndex(layer1, 1);
            //Grid.SetZIndex(layer2, 0);
            //// Ensure the other pane is hidden if it is undocked
            //if (pane2Button.Visibility == Visibility.Visible)
            //    layer2.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hide any undocked panes when the mouse enters Layer 0 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void layer0_MouseEnter(object sender, RoutedEventArgs e)
        {
            if (pane1Button.Visibility == Visibility.Visible)
                layer1.Visibility = Visibility.Collapsed;
            //if (pane2Button.Visibility == Visibility.Visible)
            //    layer2.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hide the other pane if undocked when the mouse enters Pane 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void pane2_MouseEnter(object sender, RoutedEventArgs e)
        {
            // Ensure the other pane is hidden if it is undocked
            if (pane1Button.Visibility == Visibility.Visible)
                layer1.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Docks a pane, which hides the corresponding pane button 
        /// </summary>
        /// <param name="paneNumber"></param>
        public void DockPane(int paneNumber)
        {
            if (paneNumber == 1)
            {
                pane1Button.Visibility = Visibility.Collapsed;
                pane1PinImage.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/pin.gif",
                    UriKind.RelativeOrAbsolute));
                // Add the cloned column to layer 0:
                layer0.ColumnDefinitions.Add(column1CloneForLayer0);
                // Add the cloned column to layer 1, but only if pane 2 is docked:
                //if (pane2Button.Visibility == Visibility.Collapsed)
                //layer1.ColumnDefinitions.Add(column2CloneForLayer1);
            }

        }

        /// <summary>
        /// Undocks a pane, which reveals the corresponding pane button 
        /// </summary>
        /// <param name="paneNumber"></param>
        public void UndockPane(int paneNumber)
        {
            if (paneNumber == 1)
            {
                layer1.Visibility = Visibility.Visible;
                pane1Button.Visibility = Visibility.Visible;
                pane1PinImage.Source = new BitmapImage
                (new Uri("pack://application:,,,/Icons/pinHorizontal.gif", UriKind.RelativeOrAbsolute));
                // Remove the cloned columns from layers 0 and 1:
                layer0.ColumnDefinitions.Remove(column1CloneForLayer0);
                // This won’t always be present, but Remove silently ignores bad columns:
                layer1.ColumnDefinitions.Remove(column2CloneForLayer1);
            }

        }
        #endregion

        /// <summary>
        /// Get the child control placed inside the control template 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        private object GetChildControl(DependencyObject parent, string controlName)
        {
            Object tempObj = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int counter = 0; counter < count; counter++)
            {
                //Get The Child Control based on Index
                tempObj = VisualTreeHelper.GetChild(parent, counter);

                //If Control's name Property matches with the argument control
                //name supplied then Return Control
                if ((tempObj as DependencyObject).GetValue(NameProperty).ToString() == controlName)
                    return tempObj;
                else //Else Search Recursively
                {
                    tempObj = GetChildControl(tempObj as DependencyObject, controlName);
                    if (tempObj != null)
                        return tempObj;
                }
            }
            return null;
        }

        /// <summary>
        /// Change the ToolTip content based on the state of header check-box in data grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void headerCheckBox_MouseEnter(object sender, MouseEventArgs e)
        {
            CheckBox ch = (CheckBox)sender;
            if ((bool)ch.IsChecked)
            {
                ch.ToolTip = "DeSelectALL";
            }
            else
            {
                ch.ToolTip = "SelectALL";
            }
        }

        /// <summary>
        /// Open connect expander
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnectExpander_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnectExpander.Content.ToString().Contains("Disconnect"))
            {
                if (btnRead.Content.ToString().Contains("Stop Reading"))
                {
                    string msg = "Read is in progress , Do you want to stop the reading and disconnect the reader?";
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.OK:
                            {
                                btnRead_Click(sender, e);
                                btnConnect_Click(sender, e);
                                break;
                            }
                        case MessageBoxResult.Cancel:
                            {
                                break;
                            }
                    }
                }
                else if (!((bool)btnTimedOn.IsEnabled) || (btncwOn.Content.Equals("Stop")))
                {
                    string msg = "Regulatory Test is in progress , Do you want to " + ((btncwOn.Content.Equals("Stop")) ? "stop the test and disconnect the reader?" : "disconnect the reader?");
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Cancel:
                            {
                                break;
                            }
                        case MessageBoxResult.OK:
                            {
                                if (btncwOn.Content.Equals("Stop"))
                                {
                                    btnContinousOnOff_Click(sender, e);
                                    btnConnect_Click(sender, e);
                                    break;
                                }
                                else
                                {
                                    btnConnect_Click(sender, e);
                                    break;
                                }
                            }
                    }
                }
                else if (btnFrequencyOn.Content.Equals("Stop"))
                {
                    string msg = "Regulatory Test is in progress , Do you want to disconnect the reader?";
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Cancel:
                            break;
                        case MessageBoxResult.OK:
                            btnFrequencyOn_Click(sender, e);
                            btnConnect_Click(sender, e);
                            break;
                    }
                }
                else
                {
                    btnConnect_Click(sender, e);
                }
            }
            else
            {
                expdrConnect.IsExpanded = true;
                expdrConnect.Focus();
                //Dock the settings/status panel if not docked, to display connect options
                if (pane1Button.Visibility == System.Windows.Visibility.Visible)
                {
                    pane1Button_MouseEnter(sender, e);
                    pane1Pin_Click(sender, e);
                }
                //else
                //{
                //    pane1Pin_Click(sender, e);
                //    layer1.Visibility = Visibility.Collapsed;
                //}
                if (expdrConnect.IsExpanded)
                {
                    if (btnConnect.Content.ToString().Equals("Connect"))
                    {
                        btnConnectExpander.Content = "Connect...";
                    }
                    else
                    {
                        btnConnectExpander.Content = btnConnect.Content.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Check if module have stoped reading for Read 'N' tags.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void isReadStopped_Tick(object sender, EventArgs e)
        {
            if (objReader.isReadStopped())
            {
                startRead_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Calculate tags read rate per second 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void readRatePerSec_Tick(object sender, EventArgs e)
        {
            if (txtTotalTagReads.Content.ToString() != "")
            {
                //Divide Total tag count at every 1 sec instant per difference value of
                //current time and start async read time
                UpdateReadRate(CalculateElapsedTime());
            }
        }

        /// <summary>
        /// Calculate total elapsed time between present time and start read command
        /// initiated
        /// </summary>
        /// <returns>Returns total time elapsed </returns>
        private double CalculateElapsedTime()
        {
            TimeSpan elapsed = (DateTime.Now - startAsyncReadTime);
            // elapsed time + previous cached async read time
            double totalseconds = continuousreadElapsedTime + elapsed.TotalSeconds;
            totalTimeElapsedTextBox.Content = Math.Round(totalseconds, 2).ToString();
            return totalseconds;
        }

        /// <summary>
        /// Display read rate per sec
        /// </summary>
        /// <param name="totalElapsedSeconds"> total elapsed time</param>
        private void UpdateReadRate(double totalElapsedSeconds)
        {
            long temp = Convert.ToInt64(txtTotalTagReads.Content.ToString());
            txtbxReadRatePerSec.Content = (Math.Round((temp / totalElapsedSeconds), 2)).ToString();
        }

        /// <summary>
        /// Warns the user when trying to close the app, when async read is going on 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // If URA is still reading, notify user
                if (isAsyncReadGoingOn || isSyncReadGoingOn)
                {
                    string msg = "Read is in progress , Do you want to close the application?";
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.OK:
                            {
                                btnRead_Click(null, null);
                                exitURA_Click(sender, new RoutedEventArgs());
                                Environment.Exit(1);
                                break;
                            }
                        case MessageBoxResult.Cancel:
                            {
                                e.Cancel = true;
                                break;
                            }
                    }
                }
                else if ((!((bool)btnTimedOn.IsEnabled) || (btncwOn.Content.Equals("Stop"))) && !(btnConnect.Content.ToString().Equals("Connect")))
                {
                    string msg = "Regulatory Test is in progress , Do you want to " + ((btncwOn.Content.Equals("Stop")) ? "stop the test and disconnect the reader?" : "disconnect the reader?");
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Cancel:
                            {
                                e.Cancel = true;
                                break;
                            }
                        case MessageBoxResult.OK:
                            {
                                if (btncwOn.Content.Equals("Stop"))
                                {
                                    btnContinousOnOff_Click(sender, new RoutedEventArgs());
                                    exitURA_Click(sender, new RoutedEventArgs());
                                    break;
                                }
                                else
                                {
                                    exitURA_Click(sender, new RoutedEventArgs());
                                    break;
                                }
                            }
                    }
                }
                else if (btnFrequencyOn.Content.Equals("Stop") && !(btnConnect.Content.ToString().Equals("Connect")))
                {
                    string msg = "Regulatory Test is in progress , Do you want to close the application?";
                    switch (MessageBox.Show(msg, "Universal Reader Assistant Message", MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Cancel:
                            e.Cancel = true;
                            break;
                        case MessageBoxResult.OK:
                            btnFrequencyOn_Click(sender, new RoutedEventArgs());
                            btnConnect_Click(sender, new RoutedEventArgs());
                            break;
                    }
                }
                else if (!btnUpdate.IsEnabled)
                {
                    string msg = "Firmware update is in progress. Please wait";
                    MessageBoxResult result = MessageBox.Show(msg, "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                }
                else
                {
                    exitURA_Click(sender, new RoutedEventArgs());
                    Environment.Exit(1);
                }
            }
            catch (Exception ex) { Onlog(ex); };
        }

        #endregion EventHandler

        #region SaveTagResults
        ///<summary>
        ///Save the datagrid data to text file
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>        
        private void saveData_Click(object sender, RoutedEventArgs e)
        {
            string strDestinationFile = string.Empty;
            try
            {
                if (null != tcTagResults.SelectedItem)
                {
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
                    string tabHeader = ((TextBlock)((TabItem)tcTagResults.SelectedItem).Header).Text;
                    if (tabHeader.Equals("Tag Results"))
                    {
                        strDestinationFile = "UniversalReader_tagResults"
                            + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + @".csv";
                        saveFileDialog1.FileName = strDestinationFile;
                        if ((bool)saveFileDialog1.ShowDialog())
                        {
                            strDestinationFile = saveFileDialog1.FileName;
                            TagReadRecord rda;
                            // True, if any row is selected and only selected row is saved else 
                            // false and entire data grid is saved
                            bool flagSelectiveDataSave = false;
                            for (int rowCount = 0; rowCount <= TagResults.dgTagResults.Items.Count - 1; rowCount++)
                            {
                                rda = (TagReadRecord)TagResults.dgTagResults.Items.GetItemAt(rowCount);
                                if (rda.Checked)
                                {
                                    flagSelectiveDataSave = true;
                                    break;
                                }
                            }
                            TextWriter tw = new StreamWriter(strDestinationFile);
                            StringBuilder sb = new StringBuilder();
                            //writing the header
                            int columnCount = TagResults.dgTagResults.Columns.Count;

                            for (int count = 1; count < columnCount; count++)
                            {
                                string colHeader = TagResults.dgTagResults.Columns[count].Header.ToString();
                                if ((colHeader == "EPC(ASCII)") || (colHeader == "EPC(ReverseBase36)"))
                                {
                                    //Adding column header based on selection of Display options section
                                    if (-1 != colHeader.IndexOf(cbxDisplayEPCAs.Text))
                                    {
                                        sb.Append(colHeader + ", ");
                                    }
                                }
                                else if (colHeader == "Data(ASCII)")// || (colHeader == "Data(ReverseBase36)"))
                                {
                                    //Adding column header based on selection of Display options section
                                    if (-1 != colHeader.IndexOf(cbxDisplayEmbRdDataAs.Text))
                                    {
                                        sb.Append(colHeader + ", ");
                                    }
                                }
                                else
                                {
                                    if (count == columnCount - 1)
                                    {
                                        sb.Append(colHeader);
                                    }
                                    else
                                    {

                                        sb.Append(colHeader + ", ");
                                    }
                                }
                            }
                            tw.WriteLine(sb.ToString());
                            if (flagSelectiveDataSave)
                            {
                                //writing the data
                                rda = null;
                                for (int rowCount = 0; rowCount <= TagResults.dgTagResults.Items.Count - 1; rowCount++)
                                {
                                    rda = (TagReadRecord)TagResults.dgTagResults.Items.GetItemAt(rowCount);
                                    if (rda.Checked)
                                    {
                                        textWrite(tw, rda, rowCount + 1);
                                    }
                                }
                            }
                            else
                            {
                                //writing the data
                                rda = null;
                                for (int rowCount = 0; rowCount <= TagResults.dgTagResults.Items.Count - 1; rowCount++)
                                {
                                    rda = (TagReadRecord)TagResults.dgTagResults.Items.GetItemAt(rowCount);
                                    textWrite(tw, rda, rowCount + 1);
                                }
                            }
                            tw.Close();
                        }
                    }
                    else if (tabHeader.Equals("Tag Inspector"))
                    {
                        saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
                        strDestinationFile = "UniversalReader_TagInspection_Results"
                            + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + @".csv";
                        saveFileDialog1.FileName = strDestinationFile;
                        if ((bool)saveFileDialog1.ShowDialog())
                        {
                            strDestinationFile = saveFileDialog1.FileName;
                            using (StreamWriter sw = File.CreateText(strDestinationFile))
                            {

                                //sw.WriteLine("Tag inspected on EPC is: " + TagInspector.txtEpc.Text);
                                //sw.WriteLine("Reserved Memory Bank(0)");
                                //sw.WriteLine("-----------------------");
                                sw.WriteLine("Reserved, " + TagInspector.txtKillPassword.Text
                                    + TagInspector.txtKillPassword.Text);
                                //sw.WriteLine("Access password: " + ;
                                //sw.WriteLine("       ");
                                //sw.WriteLine("EPC Memory Bank(1)");
                                //sw.WriteLine("------------------");
                                //sw.WriteLine("CRC: " + TagInspector.txtCRC.Text);
                                //sw.WriteLine("PC: " + TagInspector.txtPC.Text);
                                sw.WriteLine("EPC, " + TagInspector.txtEPCData.Text);
                                //sw.WriteLine("Unused portion?: " + TagInspector.txtEPCUnused.Text);
                                //sw.WriteLine("       ");
                                //sw.WriteLine("TID Memory Bank(2)");
                                //sw.WriteLine("------------------");
                                //sw.WriteLine("Cls ID: " + TagInspector.txtClsID.Text);
                                //sw.WriteLine("Vendor ID: " + TagInspector.txtVendorID.Text);
                                //sw.WriteLine("Model ID: " + TagInspector.txtModelID.Text);
                                //sw.WriteLine("Unique ID: " + TagInspector.txtUniqueIDValue.Text);
                                sw.WriteLine("TID, " + TagInspector.txtClsID.Text + TagInspector.txtVendorID.Text
                                    + TagInspector.txtModelID.Text + TagInspector.txtUniqueIDValue.Text);
                                sw.WriteLine("Tag type: " + TagInspector.txtVendorValue.Text + " "
                                    + TagInspector.txtModeldIDValue.Text);
                                //sw.WriteLine("       ");
                                //sw.WriteLine("User Memory Bank(3)");
                                //sw.WriteLine("------------------");
                                //sw.WriteLine("Value: " + TagInspector.txtUserMemData.Text);
                                sw.WriteLine("User, " + TagInspector.txtUserMemData.Text);
                                sw.Flush();
                                sw.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// For readability sake in the text file.
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="rda"></param>
        private void textWrite(TextWriter tw, TagReadRecord rda, int rowNumber)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(rowNumber + ", ");
            sb.Append(rda.EPC + ", ");

            switch (cbxDisplayEPCAs.Text)
            {
                case "Select":
                case "Hex": sb.Append(", " + ", "); break;
                case "ASCII": sb.Append(rda.EPCInASCII + ", "); break;
                case "ReverseBase36": sb.Append(", " + rda.EPCInReverseBase36 + ", "); break;
            }
            if (TagResults.dataSecureColumn.Visibility == Visibility.Collapsed)
            {
                sb.Append(rda.Data + ", ");
            }
            else
            {
                sb.Append(", ");
            }

            switch (cbxDisplayEmbRdDataAs.Text)
            {
                case "Select":
                case "Hex": sb.Append(", "); break;
                case "ASCII": sb.Append(rda.DataInASCII + ", "); break;
                //case "ReverseBase36": sb.Append(rda.DataInReverseBase36 + ","); break;
            }

            if (TagResults.dataSecureColumn.Visibility == Visibility.Visible)
            {
                sb.Append(rda.Data + ", ");
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(rda.TimeStamp.ToString("MM-dd-yyyy HH:mm:ss:fff") + ", ");

            sb.Append(rda.RSSI + ", ");

            sb.Append(rda.ReadCount + ", ");

            if (TagResults.tagTypeColumn.Visibility == Visibility.Visible)
            {
                sb.Append(rda.TagType + ", ");
            }
            else
            {
                sb.Append(", ");
            }

            sb.Append(rda.Antenna + ", ");

            sb.Append(rda.Protocol + ", ");

            sb.Append(rda.Frequency + ", ");

            sb.Append(rda.Phase + ", ");

            if (TagResults.GPIOColumn.Visibility == Visibility.Visible)
            {
                sb.Append(rda.GPIO);
            }

            tw.Write(sb.ToString());
            tw.WriteLine();
        }
        #endregion SaveTagResults

        #region Connect
        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btnConnect.Content.ToString() == "Connect")
                {
                    if ((bool)rdbtnNetworkConnection.IsChecked)
                    {
                        if ("" == cmbFixedReaderAddr.Text)
                        {
                            MessageBox.Show("Please input valid reader URI", "Error");
                            return;
                        }
                    }
                    if ((bool)rdbtnCustomTransportConnection.IsChecked)
                    {
                        if (string.IsNullOrWhiteSpace(txtCustomTransport.Text))
                        {
                            MessageBox.Show("Please input valid reader URI", "Error : Incorrect URI", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        Regex ip = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,4}\b");
                        if (!ip.IsMatch(txtCustomTransport.Text))
                        {
                            MessageBox.Show("Please type in custom reader in given format.\nFormat :- xxx.xxx.xxx.xxx:xxxx (readerIP:portname).\nEx. 172.16.16.2:5000", "Error : Incorrect URI", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    App.Current.MainWindow.InvalidateVisual();
                    ProtocolsGroupBox.InvalidateVisual();
                    AntennasGroupBox.InvalidateVisual();
                    grpGPIOBehaviour.InvalidateVisual();
                    btnConnect.InvalidateVisual();

                    isReconnectFailed = false;
                    isReaderConnected = true;
                    isURAConnectInProgress = true;
                    try
                    {
                        Mouse.SetCursor(Cursors.AppStarting);
                        tagdb.Clear();
                        // Clear warning on status bar if any
                        ClearMessageOnStatusBar();
                        btnConnect_Click_Body(sender, e);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Inner Exception :" + ex.InnerException.ToString() + "Exception : "
                            + ex.Message + "Source :" + ex.Source, "ReaderException", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        try
                        {
                            //btnConnect.ToolTip = "Disconnect";
                            if (objReader != null)
                            {
                                MultiFilters1.cbxFilterMemBank.ItemsSource = null;
                                MultiFilters1.cbxFilterMemBank.Items.Clear();
                                MultiFilters1.cbxFilterMemBank.ItemsSource = cbxDisplayFilterMemBankItemsList(objReader);

                                MultiFilters2.cbxFilterMemBank.ItemsSource = null;
                                MultiFilters2.cbxFilterMemBank.Items.Clear();
                                MultiFilters2.cbxFilterMemBank.ItemsSource = cbxDisplayFilterMemBankItemsList(objReader);

                                MultiFilters3.cbxFilterMemBank.ItemsSource = null;
                                MultiFilters3.cbxFilterMemBank.Items.Clear();
                                MultiFilters3.cbxFilterMemBank.ItemsSource = cbxDisplayFilterMemBankItemsList(objReader);
                            }
                            if (chkApplyFilter3.IsChecked == true) chkApplyFilter3.IsChecked = false;
                            if (chkApplyFilter2.IsChecked == true) chkApplyFilter2.IsChecked = false;
                            if (chkApplyFilter1.IsChecked == true) chkApplyFilter1.IsChecked = false;

                            MultiFilters1.cbxFilterMemBank.SelectedIndex = 0;
                            MultiFilters2.cbxFilterMemBank.SelectedIndex = 0;
                            MultiFilters3.cbxFilterMemBank.SelectedIndex = 0;
                        }
                        catch(Exception exMsg)
                        {
                            Console.WriteLine(exMsg.Message);
                        }

                        tiTagResults.Focus();
                        btnClearTagReads.IsEnabled = true;
                        saveData.IsEnabled = true;
                        DeleteConfigFile();
                        // Enables the controls required after Connect.
                        gridPerformanceTuning.IsEnabled = true;
                        grpbxPerformanceTuning.IsEnabled = true;
                        rdbtnperantenna.IsEnabled = rdbtnglobal.IsEnabled = true;
                        txtReadPowerAnt1.IsEnabled = txtReadPowerAnt2.IsEnabled = txtReadPowerAnt3.IsEnabled = txtReadPowerAnt4.IsEnabled = true;
                        txtWritePowerAnt1.IsEnabled = txtWritePowerAnt2.IsEnabled = txtWritePowerAnt3.IsEnabled = txtWritePowerAnt4.IsEnabled = true;
                        txtfontSize.IsEnabled = true;
                        txtRFOffTimeout.IsEnabled = true;
                        txtRFOnTimeout.IsEnabled = true;
                        stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = true;
                        cbxAntennamux.IsEnabled = chbxFour.IsEnabled = chbxOne.IsEnabled = chbxTwo.IsEnabled = chbxThree.IsEnabled = true;
                        chbxGpo1.IsEnabled = chbxGpo2.IsEnabled = chbxGpo3.IsEnabled = chbxGpo4.IsEnabled = true;
                        cbxAntennamux.IsChecked = chbxFour.IsChecked = chbxOne.IsChecked = chbxTwo.IsChecked = chbxThree.IsChecked = false;
                        {
                            try
                            {
                                if (objReader is SerialReader)
                                {
                                    GetGPOS();
                                }

                                // In case of network readers, Get gpo is not supported. However, binding checkboxes to the triggers is mandatory since set is supported.
                                #region Binding the CheckBox to the Triggers
                                chbxOne.Checked += new RoutedEventHandler(chbxOne_Checked);
                                chbxOne.Unchecked += new RoutedEventHandler(chbxOne_Checked);
                                chbxTwo.Checked += new RoutedEventHandler(chbxTwo_Checked);
                                chbxTwo.Unchecked += new RoutedEventHandler(chbxTwo_Checked);
                                chbxThree.Checked += new RoutedEventHandler(chbxThree_Checked);
                                chbxThree.Unchecked += new RoutedEventHandler(chbxThree_Checked);
                                chbxFour.Checked += new RoutedEventHandler(chbxFour_Checked);
                                chbxFour.Unchecked += new RoutedEventHandler(chbxFour_Checked);
                                #endregion

                                displayLogicalAntennas();
                            }catch(Exception ex)
                            {
                                if(ex.Message.IndexOf("/reader/antenna/portSwitchGpos not supported") != -1)
                                {
                                    //Just ignore error as if module doesnot support. Disable antenna multiplexing
                                    cbxAntennamux.IsEnabled = chbxFour.IsEnabled = chbxOne.IsEnabled = chbxTwo.IsEnabled = chbxThree.IsEnabled = false;
                                    chbxGpo1.IsEnabled = chbxGpo2.IsEnabled = chbxGpo3.IsEnabled = chbxGpo4.IsEnabled = false;
                                    cbxAntennamux.IsChecked = chbxFour.IsChecked = chbxOne.IsChecked = chbxTwo.IsChecked = chbxThree.IsChecked = false;
                                }
                            }
                            if (btncwOn.Content.ToString() == "Stop")
                            {
                                btncwOn.Content = "Start";
                                grpbxtimed.IsEnabled = grpbxContinous.IsEnabled = txtbxCONTimeout.IsEnabled = txtbxCOFFTimeout.IsEnabled = txtbxTimedONTimeout.IsEnabled = btncwOn.IsEnabled = tbtimed.IsEnabled = expdrReadOptions.IsEnabled = expdrPerformanceTuning.IsEnabled = stkpnlHopTable.IsEnabled = true;
                            }
                            if (btnTimedOn.IsEnabled == false)
                            {
                                btnTimedOn.IsEnabled = true;
                            }
                        }
                        //cbxAntennamux.Visibility = Visibility.Visible;
                        txbGPI.Visibility = Visibility.Visible;
                        gridDataExtensions.IsEnabled = true;
                        grpbxDataExtensions.IsEnabled = true;
                        chkDataExtensions.IsEnabled = true;
                        configureGPIOS();
                        isURAConnectInProgress = false;
                    }
                }
                else
                {
                    try
                    {
                        expdrOpenRegConfig.IsExpanded = false;
                        expdrOpenRegConfig.IsEnabled = false;
                        expdrOpenRegConfig.Visibility = Visibility.Hidden;
                        txtbxHopTable_Cnot.Text = string.Empty;
                        //btnDefaultHopTable.Visibility = expdrOpenRegConfig.Visibility = Visibility.Hidden;
                        tbconnect.IsSelected = true;
                        expdrOpenUserMode.IsExpanded = false;
                        SaveConfigurationForWizardFlow();
                        tmrLogStopReader();
                        isRFModeSupp = false;
                        if (btnRead.Content.ToString() == "Stop Reading")
                        {
                            startRead_Click(sender, e);
                        }
                        regionToSet = Reader.Region.UNSPEC;
                        //lblReaderUri.Content = "Reader URI";
                        lblReaderUri.Content = "";
                        btnConnectExpander.ToolTip = "Connect";
                        regioncombo.ItemsSource = null;
                        btnConnect.IsEnabled = true;
                        chkDataExtensions.IsChecked = false;
                        if ((objReader is SerialReader))
                        {
                            cbxBaudRate.IsEnabled = true;
                        }

                        btnRead.IsEnabled = false;

                        cmbReaderAddr.IsEnabled = true;
                        cmbFixedReaderAddr.IsEnabled = true;
                        txtCustomTransport.IsEnabled = true;
                        objReader.StatsListener -= PrintTemperature;
                        #region Reset the UI Element
                        ClearReads();
                        chbxLLRPGPI1.IsChecked = chbxLLRPGPI2.IsChecked = chbxLLRPGPI3.IsChecked = chbxLLRPGPI4.IsChecked = false;
                        chbxLLRPGPO1.IsChecked = chbxLLRPGPO2.IsChecked = chbxLLRPGPO3.IsChecked = chbxLLRPGPO4.IsChecked = false;
                        expdrRegulatoryTesting.IsExpanded = false;
                        expdrRegulatoryTesting.IsEnabled = true;
                        rdbtnLicenceUpgrade.IsEnabled = true;
                        txtM3eUID.Text = String.Empty;
                        txtM3eTagFind.Text = string.Empty;
                        lblM3eRegion.Content = "";
                        cbxBaudRate.ItemsSource = null;
                        cbxBaudRate.ItemsSource = cbxDisplayBaudRateItem(string.Empty);
                        cbxGPIPort.ItemsSource = cbxDisplayGPIItem(0);
                        cbxBaudRate.SelectedIndex = 0;
                        lblReaderTemperature.Content = "0" + "°C";
                        chkBlockRead.IsChecked = false;
                        chkSecureRead.IsChecked = false;
                        txtBlockAddress.Text = "0";
                        txtReadLength.Text = "0";
                        #endregion
                        #region This will reset the tag combo box and Tag list .
                        treeView.ItemsSource = null;
                        treeView.Items.Clear();
                        lvTagType1.ItemsSource = null;
                        lvTagType1.Items.Clear();
                        lbTagType.ItemsSource = null;
                        lbTagType.Items.Clear();
                        TagAddControl = false;
                        #endregion
                        if (null != objReader)
                        {
                            objReader.Destroy();
                            objReader = null;
                        }
                        restoreReadSetting = false;
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Onlog(ex);
                    }
                    catch (Exception ex)
                    {
                        Onlog(ex);
                    }
                    finally
                    {
                        // Stop timer to render data on the grid
                        dispatchtimer.Stop();
                        readRatePerSec.Stop();
                        isReadStopped.Stop();
                        //btnConnect.ToolTip = "Connect";
                        btnConnect.Content = "Connect";
                        ConfigureAntennaBoxes(null);
                        ConfigureLogicalAntennaBoxes(null);
                        ConfigureProtocols(null);
                        gps.Clear();

                        // close the transport listener log re-enable the menu option but uncheck it                
                        chkEnableTransportLogging.IsChecked = false;
                        chkEnableTransportLogging.IsEnabled = true;

                        // Enable Probe read names after disconnect
                        probeReadNames.IsEnabled = true;

                        // Disable  read-once, readasyncread buttons                    
                        btnRead.IsEnabled = false;
                        btnRead.Visibility = System.Windows.Visibility.Hidden;
                        btnRefreshReadersList.IsEnabled = true;
                        // Register bonjour subscribed events
                        if (null != bonjour.EventManager)
                        {
                            bonjour.RebindServices();
                        }

                        // Enable fast search
                        if (!(model.Equals("M3e")))
                        {
                            chkEnableFastSearch.IsEnabled = true;
                        }
                        lblshowStatus.Content = "Disconnected";
                        InitializeRdrDiagnostics();

                        // Enable firmware Update panel
                        stackPanelFirmwareUpdate.IsEnabled = true;

                        imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedRed.png",
                            UriKind.RelativeOrAbsolute));

                        rdbtnLocalConnection.IsEnabled = true;
                        rdbtnNetworkConnection.IsEnabled = true;
                        rdbtnCustomTransportConnection.IsEnabled = true;
                        grpbxPerformanceTuning.IsEnabled = false;
                        chkCustomizeGen2Settings.IsChecked = false;
                        ReadDataGroupBox.IsEnabled = false;
                        gridPerformanceTuning.IsEnabled = false;
                        gridReadOptions.IsEnabled = false;
                        gridDataExtensions.IsEnabled = false;
                        gridRegulatoryTesting.IsEnabled = false;
                        regioncombo.IsEnabled = false;
                        gridDisplayOptions.IsEnabled = false;
                        btnConnectExpander.Content = "Connect...";
                        cbxAntennamux.IsChecked = chbxFour.IsChecked = chbxOne.IsChecked = chbxTwo.IsChecked = chbxThree.IsChecked = false;
                        cbxAntennamux.Visibility = Visibility.Collapsed;
                        configuringDuringConnect(false);

                        // Disable all the tabs except tag results tab
                        tiWriteEPC.IsEnabled = false;
                        tiTagInspector.IsEnabled = false;
                        tiUserMemory.IsEnabled = false;
                        tiLockTag.IsEnabled = false;
                        tiUntraceable.IsEnabled = false;
                        tiAuthenticate.IsEnabled = false;
                        tiTagResults.Focus();

                        // Reset protocols
                        iso6bCheckBox.IsChecked = false;
                        ipx256CheckBox.IsChecked = false;
                        ipx64CheckBox.IsChecked = false;
                        ataCheckBox.IsChecked = false;
                        isoUcodeCheckbox.IsChecked = false;
                        iso14443aCheckBox.IsChecked = false;
                        iso14443bCheckBox.IsChecked = false;
                        iso15693CheckBox.IsChecked = false;
                        iso18092CheckBox.IsChecked = false;
                        felicaCheckBox.IsChecked = false;
                        iso180003mode3CheckBox.IsChecked = false;
                        lf125KHZCheckBox.IsChecked = false;
                        lf134KHZCheckBox.IsChecked = false;

                        // Reset embedded read settings
                        chkEmbeddedReadData.IsChecked = false;
                        cbxReadDataBank.SelectedIndex = 0;
                        txtembReadStartAddr.Text = "0";
                        txtembReadLength.Text = "2";
                        chkUniqueByData.IsChecked = false;

                        // Reset filter settings
                        /*if (chkApplyFilter3.IsChecked == true)*/
                        chkApplyFilter3.IsChecked = false;
                        /*if (chkApplyFilter2.IsChecked == true)*/
                        chkApplyFilter2.IsChecked = false;
                        /*if (chkApplyFilter1.IsChecked == true)*/
                        chkApplyFilter1.IsChecked = false;

                        MultiFilters1.cbxFilterMemBank.SelectedIndex = 0;
                        MultiFilters1.txtFilterStartAddr.Text = "32";
                        MultiFilters1.txtFilterData.Text = "0";
                        MultiFilters1.chkFilterInvert.IsChecked = false;
                        MultiFilters2.cbxFilterMemBank.SelectedIndex = 0;
                        MultiFilters2.txtFilterStartAddr.Text = "32";
                        MultiFilters2.txtFilterData.Text = "0";
                        MultiFilters2.chkFilterInvert.IsChecked = false;
                        MultiFilters3.cbxFilterMemBank.SelectedIndex = 0;
                        MultiFilters3.txtFilterStartAddr.Text = "32";
                        MultiFilters3.txtFilterData.Text = "0";
                        MultiFilters3.chkFilterInvert.IsChecked = false;

                        // Re-initialized the read power slider control value to 0,
                        // when Disconnect Button on URA is pressed (Note : Only 
                        // re-initializing the slider control value and this doesn't
                        // set zero read power on the module while disconnecting)
                        sldrReadPwr.Value = 0;
                        sldrWritePwr.Value = 0;

                        if (null != transportLogFile)
                        {
                            transportLogFile.Close();
                        }

                        // Disable all the tabs except tag results tab
                        WriteEpc.spWriteEPC.IsEnabled = false;
                        TagInspector.spTagInspector.IsEnabled = false;
                        UserMemory.spUserMemory.IsEnabled = false;
                        LockTag.spLockTag.IsEnabled = false;

                        isAsyncReadGoingOn = false;
                        isSyncReadGoingOn = false;
                        if (OptimalReaderSettings != null)
                        {
                            OptimalReaderSettings.Clear();
                        }
                        // Turn off warning messages once after disconnect
                        GUIturnoffWarning();

                        CustomizedMessageBox = null;
                        channelOccupiedMessageBox = null;

                        isLoadSavedConfigurations = false;

                        // Disable save configuration button
                        btnSaveConfig.IsEnabled = false;

                        // Enable load configuration button
                        btnLoadConfig.IsEnabled = true;

                        chkEnableFastSearch.IsChecked = false;
                        Mouse.SetCursor(Cursors.Arrow);

                        // Reset connection lost exception counter
                        connectionLostCount = 0;
                        // print this line in TMR log file for mentioning closing the port
                        //log.Info("*****************************************************************");
                        //log.Flush();

                        //chkLBTEnable.Visibility = Visibility.Hidden;
                        //chkEnableDwellTime.Visibility = Visibility.Hidden;
                        //txtEnableLBTThreshold.Visibility = Visibility.Hidden;
                        //txtDwelltime.Visibility = Visibility.Hidden;
                        //btnRegionConfig.Visibility = Visibility.Hidden;
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        /// <summary>
        /// Reader connect using probing technique if the reader type is serial , otherwise just connects.
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

                    Dispatcher.Invoke((Action)(() =>
                    {
                        // Serial port shut down happens inside probeBaudRate function which closes the "serverStream" and "clientSocket", so need to create a reader object again to initialize them again.
                        if ((bool)rdbtnCustomTransportConnection.IsChecked)
                        {
                            Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);

                            //Creates a Reader Object for operations on the Reader.
                            r = Reader.Create(string.Concat("tcp://", uri));
                        }
                    }));
                }
                catch (ReaderException rex)
                {
                    if (rex.Message.Contains("Connect Successful...Streaming tags"))
                    {
                        //stop the streaming
                        ((SerialReader)r).stopStreaming();
                    }
                    else
                    {
                        throw rex;
                    }
                }
                //Set the current baudrate so that next connect will use this baudrate.
                r.ParamSet("/reader/baudRate", currentBaudRate);
                // Now connect with current baudrate
                r.Connect();
                Dispatcher.Invoke((Action)(() =>
                {
                    //Set user selected baudrate if it doesn't match module ones only when user has manually selected some baudrate.
                    if (cbxBaudRate.SelectedItem != null && (((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString() != "Select"))
                    {
                        if (currentBaudRate != Convert.ToInt32(((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString()))
                        {
                            r.ParamSet("/reader/baudRate", Convert.ToInt32(((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString()));
                            cbxBaudRate.SelectedItem = Convert.ToInt32(((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString());
                        }
                    }
                }));
            }
            else // For other readers, call connect directly.
            {
                r.Connect();
            }
            Dispatcher.Invoke((Action)(() =>
            {
                //load the 'r' reference in objReader if it is custom transport
                if ((bool)rdbtnCustomTransportConnection.IsChecked)
                {
                    objReader = r;
                }
            }));
        }

        /// <summary>
        /// Initializes the reader connected to the reader address field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click_Body(object sender, EventArgs e)
        {
            try
            {
                //Starts the reader from default state
                if (objReader != null)
                {
                    objReader.Destroy();
                    ConfigureAntennaBoxes(null);
                    ConfigureLogicalAntennaBoxes(null);
                    ConfigureProtocols(null);
                }
            }
            catch (Exception ex) { Onlog(ex); }
            try
            {
                if ((bool)rdbtnLocalConnection.IsChecked)
                {
                    if (!ValidatePortNumber(cmbReaderAddr.Text))
                    {
                        throw new IOException();
                    }
                    if (cmbReaderAddr.Text == "")
                    {
                        throw new IOException();
                    }
                    // Creates a Reader Object for operations on the Reader.
                    string readerUri = cmbReaderAddr.Text;
                    //Regular Expression to get the com port number from comport name .
                    //for Ex: If The Comport name is "USB Serial Port (COM19)" by using this 
                    // regular expression will get com port number as "COM19".
                    MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                    foreach (Match m in mc)
                    {
                        if (!string.IsNullOrWhiteSpace(m.ToString()))
                            readerUri = m.ToString();
                    }
                    objReader = Reader.Create(string.Concat("tmr:///", readerUri));
                    uri = readerUri;
                    // Create TMR log Header
                    tmrLogHeader(uri);
                }
                else if ((bool)rdbtnNetworkConnection.IsChecked)
                {
                    string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(cmbFixedReaderAddr.Text)).FirstOrDefault();
                    string readerUri;
                    if (string.IsNullOrWhiteSpace(key) || key == null)
                        readerUri = cmbFixedReaderAddr.Text;
                    else
                        readerUri = bonjour.HostNameIpAddress[key];
                    MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                    foreach (Match m in mc)
                    {
                        readerUri = m.ToString();
                    }
                    //Creates a Reader Object for operations on the Reader.
                    objReader = Reader.Create(string.Concat("tmr://", readerUri));
                    uri = readerUri;
                    // Create TMR log Header
                    tmrLogHeader(uri);
                }
                else if ((bool)rdbtnCustomTransportConnection.IsChecked)
                {
                    string readerUri = txtCustomTransport.Text.Trim();
                    Reader.SetSerialTransport("tcp", SerialTransportTCP.CreateSerialReader);
                    //Creates a Reader Object for operations on the Reader.
                    objReader = Reader.Create(string.Concat("tcp://", readerUri));
                    uri = readerUri;
                    // Create TMR log Header
                    tmrLogHeader(uri);
                }

                ///Assign  reader to reader
                //reader = reader; //(Reader)reader;

                // If Option selected add the serial-reader-specific message logger
                // before connecting, so we can see the initialization.
                if ((bool)chkEnableTransportLogging.IsChecked)
                {
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                    saveFileDialog1.Filter = "Text Files (.txt)|*.txt";
                    saveFileDialog1.Title = "Select a File to save transport layer logging";
                    string strDestinationFile = "UniversalReader_transportLog"
                        + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + @".txt";
                    saveFileDialog1.FileName = strDestinationFile;
                    // Show the Dialog.
                    // If the user clicked OK in the dialog and
                    // a .txt file was selected, open it.
                    if (saveFileDialog1.ShowDialog() == true)
                    {
                        StreamWriter writer = new StreamWriter(saveFileDialog1.FileName);
                        writer.AutoFlush = true;
                        transportLogFile = writer;
                        if (objReader is SerialReader)
                            objReader.Transport += SerialListener;
                        if (objReader is RqlReader)
                            objReader.Transport += RQLListener;
                        if (objReader is LlrpReader)
                            objReader.Transport += LLrpListener;
                    }
                    else
                    {
                        chkEnableTransportLogging.IsChecked = false;
                    }
                }
                chkEnableTransportLogging.IsEnabled = false;
                if (objReader is SerialReader)
                {
                    // Set the selected baud rate, so that api try's connecting to the 
                    // module with the selected baud rate first
                    SetBaudRate();
                }
                //Show the status
                lblshowStatus.Content = "Connecting..";
                Mouse.SetCursor(Cursors.AppStarting);
                connectToReader(objReader);
                // Enable all the expander controls
                EnableDisableExpanderControl(true);
                // Enable save config button
                btnSaveConfig.IsEnabled = true;
                //
                TagResults.dgTagResults.CancelEdit();
                TagResults.dgTagResults.ItemsSource = null;
                TagResults.dgTagResults.ItemsSource = tagdb.TagList;

                //Enable all the disabled tabs
                tiWriteEPC.IsEnabled = true;
                tcTagResults.IsEnabled = true;
                tiTagInspector.IsEnabled = true;
                tiUserMemory.IsEnabled = true;
                tiLockTag.IsEnabled = true;
                tiUntraceable.IsEnabled = true;
                tiAuthenticate.IsEnabled = true;

                GUIturnoffWarning();

                gridReadOptions.IsEnabled = true;
                regioncombo.IsEnabled = true;
                gridDisplayOptions.IsEnabled = true;
                gridDataExtensions.IsEnabled = true;
                gridPerformanceTuning.IsEnabled = true;

                cbxcolumnSelection.IsEnabled = true;

                //Setting to global power on connect
                rdbtnglobal.IsChecked = true;

                //Clear tag counts and database
                btnClearReads_Click(sender, (RoutedEventArgs)e);

                // Enable and Register temperature listener
                try
                {
                    if (rdbtnLocalConnection.IsChecked == true || rdbtnCustomTransportConnection.IsChecked == true)
                    {
                        if (objReader is SerialReader)
                        {
                            SerialReader reader = objReader as SerialReader;
                            if (reader.model.Equals("M3e"))
                            {
                                objReader.ParamSet("/reader/stats/enable", (Reader.Stat.StatsFlag.TEMPERATURE));
                                objReader.StatsListener += PrintTemperature;
                                //Get serial number
                                {
                                    try
                                    {
                                        SerialReader sr = objReader as SerialReader;
                                        string serialNumber = sr.CmdGetSerialNumber();
                                        cmbReaderAddr.Text = reader.model + "-" + serialNumber + " (" + uri + ")";
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                            else
                            {
                                objReader.ParamSet("/reader/stats/enable", Reader.Stat.StatsFlag.TEMPERATURE);
                                objReader.StatsListener += PrintTemperature;
                            }
                            rdBtnStopNRead.Visibility = Visibility.Visible;
                        }
                    }
                    else if (objReader is LlrpReader)
                    {
                        objReader.ParamSet("/reader/stats/enable", 0
                            | Reader.Stat.StatsFlag.TEMPERATURE
                            | Reader.Stat.StatsFlag.ANTENNAPORTS
                            );
                        objReader.StatsListener += PrintTemperature;
                        rdBtnStopNRead.Visibility = Visibility.Collapsed;
                    }
                }
                catch (FeatureNotSupportedException)
                {
                    // Ignore enabling reader stats on module firmware's which doesn't has support for it
                }

                try
                {
                    if (objReader is LlrpReader)
                    {
                        lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                    }
                    else
                    {
                        SerialReader reader = objReader as SerialReader;
                        if (reader.model.Equals("M3e"))
                        {
                            Reader.Stat.Values objRdrStats = (Reader.Stat.Values)objReader.ParamGet("/reader/stats");
                            lblReaderTemperature.Content = objRdrStats.TEMPERATURE.ToString() + "°C";
                        }
                        else
                        {
                            lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                        }
                    }
                }
                catch (Exception)
                {
                }

                //Get connect reader model
                model = objReader.ParamGet("/reader/version/model").ToString();

                txtbxReaderName.Text = model;

                //Set the embedded read data length for connected reader
                txtembReadLength.Text = "0";

                //Show the status
                lblshowStatus.Content = "Connected";

                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png", UriKind.RelativeOrAbsolute));
                if ((bool)rdbtnLocalConnection.IsChecked)
                {
                    lblReaderUri.Content = model.ToString() + " (" + uri + ")";//uri;
                }
                else if ((bool)rdbtnCustomTransportConnection.IsChecked)
                {
                    lblReaderUri.Content = model.ToString() + " (" + uri + ")";//uri;
                }
                else
                {
                    lblReaderUri.Content = model.ToString() + " (" + uri + ")";// uri;
                }
                Mouse.SetCursor(Cursors.Arrow);
                btnConnectExpander.ToolTip = "Disconnect";
                grpbxPerformanceTuning.IsEnabled = true;
                grpbxRdrPwrSettngs.IsEnabled = rdbtnperantenna.IsEnabled = rdbtnglobal.IsEnabled = true;
                txtReadPowerAnt1.IsEnabled = txtReadPowerAnt2.IsEnabled = txtReadPowerAnt3.IsEnabled = txtReadPowerAnt4.IsEnabled = true;
                txtWritePowerAnt1.IsEnabled = txtWritePowerAnt2.IsEnabled = txtWritePowerAnt3.IsEnabled = txtWritePowerAnt4.IsEnabled = true;
                ReadDataGroupBox.IsEnabled = true;
                if (M7eFamilyList.Contains(model))
                {
                    //Get version information
                    string fwVer = (string)objReader.ParamGet("/reader/version/software");
                    isRFModeSupp = isRFModeSupportedFW(fwVer.Substring(1, 10));
                }
                //Disable connected device / network reader radio button based on reader object
                if (objReader is SerialReader)
                {
                    if (rdbtnCustomTransportConnection.IsChecked == true)
                    {
                        rdbtnNetworkConnection.IsEnabled = false;
                        rdbtnLocalConnection.IsEnabled = false;
                    }
                    else
                    {
                        rdbtnNetworkConnection.IsEnabled = false;
                        rdbtnCustomTransportConnection.IsEnabled = false;
                    }

                }
                else
                {
                    rdbtnCustomTransportConnection.IsEnabled = false;
                    rdbtnLocalConnection.IsEnabled = false;
                }
                if (rdbtnNetworkConnection.IsChecked == true && TagResults.GPIOColumn.Visibility == Visibility.Visible)
                {
                    TagResults.GPIOColumn.Visibility = System.Windows.Visibility.Collapsed;
                }

                //Disable the refresh button
                btnRefreshReadersList.IsEnabled = false;
                // Release bonjour subscribed events
                if (null != bonjour.EventManager)
                {
                    bonjour.UnbindServices();
                }

                //Enable launch web api
                if (!(objReader is SerialReader))
                    btnWebUI.Visibility = System.Windows.Visibility.Visible;

                btnRead.Visibility = System.Windows.Visibility.Visible;
                //READER URI drop down should be disabled after URA is connected to a reader
                cmbReaderAddr.IsEnabled = false;
                cmbFixedReaderAddr.IsEnabled = false;
                txtCustomTransport.IsEnabled = false;

                //Disable Probe read names after connect
                probeReadNames.IsEnabled = false;

                //readerStatus.IsEnabled = true;
                regionToSet = (Reader.Region)objReader.ParamGet("/reader/region/id");
                Reader.Region[] regions;
                regioncombo.Items.Clear();
                if (objReader is LlrpReader || objReader is RqlReader)
                {
                    regions = new Reader.Region[] { regionToSet };
                }
                else
                {
                    if (!(model.Equals("M3e")))
                    {
                        regioncombo.Items.Add("Select");
                        regioncombo.Visibility = Visibility.Visible;
                        lblM3eRegion.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        regioncombo.Visibility = Visibility.Collapsed;
                        lblM3eRegion.Visibility = Visibility.Visible;
                        lblM3eRegion.Content = "UNIVERSAL";
                    }
                    regions = (Reader.Region[])objReader.ParamGet("/reader/region/supportedRegions");
                }
                for (int i = 0; i < regions.Length; i++)
                {
                    regioncombo.Items.Add(regions[i]);
                }

                if (!(model.Equals("M3e")))
                {
                    if (regionToSet != Reader.Region.UNSPEC)
                    {
                        //set the region on module
                        regioncombo.SelectedItem = regioncombo.Items.GetItemAt(regioncombo.Items.IndexOf(regionToSet));
                    }
                    else
                    {
                        regioncombo.SelectedItem = regioncombo.Items.GetItemAt(regioncombo.Items.IndexOf("Select"));
                    }
                }
                else
                {
                    regioncombo.SelectedItem = regioncombo.Items[0];
                }

                //Initialize max and min read power for read power slider
                InitializeRdPwrSldrMaxNMinValue();

                // Get the power mode on the reader and set in the UI. 
                if (objReader is SerialReader)
                {
                    stackPowerMode.Visibility = Visibility.Visible;
                    try
                    {
                        PowerMode getPwrMode = (PowerMode)objReader.ParamGet("/reader/powerMode");
                        switch (getPwrMode)
                        {
                            case PowerMode.FULL:
                                pwrModeComboList.SelectedValue = "FULL";
                                break;
                            case PowerMode.MINSAVE:
                                pwrModeComboList.SelectedValue = "MINSAVE";
                                break;
                            case PowerMode.SLEEP:
                                pwrModeComboList.SelectedValue = "SLEEP";
                                break;
                            default:
                                String dispStr = "The Power Mode on the reader is " + getPwrMode.ToString() + ". Reverting to FULL Power Mode.";
                                MessageBox.Show(dispStr, "Reader Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                objReader.ParamSet("/reader/powerMode", PowerMode.FULL);
                                pwrModeComboList.SelectedValue = "FULL";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Reader Message",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    stackPowerMode.Visibility = Visibility.Collapsed;
                }

                //Initialize settings received o
                OptimalReaderSettings = null;

                //Make these UI elements visible  when userMode implementation is needed.
                lblUserMode.Visibility = Visibility.Collapsed;
                userModeComboList.Visibility = Visibility.Collapsed;
                expdrOpenUserMode.Visibility = Visibility.Collapsed;
                if ((!model.Contains("M3e")) && (objReader is SerialReader))
                {
                    if (M7eFamilyList.Contains(model))
                    {
                        try
                        {
                            usrModeToSet = (SerialReader.UserMode)objReader.ParamGet("/reader/userMode");
                            if (usrModeToSet == SerialReader.UserMode.MEDICAL_CABINET)
                            {
                                userModeComboList.SelectedValue = "MEDICAL CABINET";
                            }
                            else
                            {
                                userModeComboList.SelectedValue = usrModeToSet;
                            }
                            expdrOpenUserMode.IsExpanded = true;
                            if (usrModeToSet == SerialReader.UserMode.OPEN)
                            {
                                enableGen2Settings();
                            }
                            userModeComboList.IsEnabled = true;
                            expdrOpenUserMode.IsEnabled = true;

                            xgrpbxSelectBLF.Visibility = Visibility.Collapsed;
                            xgrpbxtTari.Visibility = Visibility.Collapsed;
                            xgrpbxtagEncoding.Visibility = Visibility.Collapsed;
                            if (isRFModeSupp)
                            {
                                xgrpbxrfMode.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                xgrpbxrfMode.Visibility = Visibility.Collapsed;
                            }
                        }
                        catch (Exception ex)
                        {
                            Onlog(ex);
                        }
                    }
                    else
                    {
                        //disable user mode and its expander, rfMode comboBox and set their selected index for non-M7e modules. 
                        userModeComboList.IsEnabled = false;
                        userModeComboList.SelectedIndex = -1;
                        expdrOpenUserMode.IsEnabled = false;

                        xgrpbxSelectBLF.Visibility = Visibility.Visible;
                        xgrpbxtTari.Visibility = Visibility.Visible;
                        xgrpbxtagEncoding.Visibility = Visibility.Visible;
                        xgrpbxrfMode.Visibility = Visibility.Collapsed;
                    }

                    //Get the Gen2 individual params
                    InitializeOptimalSettings();
                }

                if(objReader is LlrpReader)
                {
                    InitializeOptimalSettings();
                    //disable user mode and its expander, rfMode comboBox and set their selected index for non-M7e modules. 
                    userModeComboList.IsEnabled = false;
                    userModeComboList.SelectedIndex = -1;
                    expdrOpenUserMode.IsEnabled = false;
                    xgrpbxSelectBLF.Visibility = Visibility.Visible;
                    xgrpbxtTari.Visibility = Visibility.Visible;
                    xgrpbxtagEncoding.Visibility = Visibility.Visible;
                    xgrpbxrfMode.Visibility = Visibility.Collapsed;
                }

                objReader.ParamSet("/reader/transportTimeout", int.Parse(txtRFOnTimeout.Text) + 5000);
                //try setting a unique ID
                //reader.ParamSet("/reader/hostname", "this reader");
                if (objReader is SerialReader)
                {
                    cbxBaudRate.IsEnabled = true;
                }

                // Load Gen2 Settings 
                initialReaderSettingsLoaded = false;
                if (!model.Contains("M3e"))
                {
                    LoadGen2Settings();
                }
                initialReaderSettingsLoaded = true;

                //Enable fast search on for mercury6, astra-ex, m6e. 
                if (model.Equals("Astra") ||  model.Equals("M3e"))
                {
                    chkEnableFastSearch.IsEnabled = false;
                }

                // Disable performance tuning and embedded read data options for model astra.
                if (!(model.Equals("M3e")))
                {
                    if (model.Equals("Astra"))
                    {
                        // // Disable performance tuning options
                        //// grpbxPerformanceTuning.IsEnabled = false;
                        // rdbtnAutoAdjstAsPoplChngs.IsEnabled = false;
                        // rdbtnOptmzExtmdNoTagsInField.IsEnabled = false;
                        // stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
                        // stkpnlRdDistVsrdRate.IsEnabled = false;
                        // rdBtnSlBstChforPoplSize.IsEnabled = false;
                        // rdBtnTagsRespondOption.IsEnabled = true;
                        // rdBtnTagsRespondOption.IsChecked = true;
                        // // Disable embedded read data options
                        // ReadDataGroupBox.IsEnabled = false;
                        // chkEmbeddedReadData.IsChecked = false;
                        DisableControlsForAstra();
                    }
                    else
                    {
                        // Disable performance tuning options
                        // grpbxPerformanceTuning.IsEnabled = false;
                        rdbtnAutoAdjstAsPoplChngs.IsEnabled = true;
                        rdbtnOptmzExtmdNoTagsInField.IsEnabled = true;
                        if (rdbtnOptmzExtmdNoTagsInField.IsChecked == true)
                        {
                            stkpnlOptmzExtmdNoTagsInField.IsEnabled = true;
                        }
                        else
                        {
                            stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
                        }
                        stkpnlRdDistVsrdRate.IsEnabled = true;
                        if (M7eFamilyList.Contains(model))
                        {
                            rdBtnSlBstChforPoplSize.IsEnabled = false;
                        }
                        else
                        {
                            rdBtnSlBstChforPoplSize.IsEnabled = true;
                        }
                        rdBtnTagsRespondOption.IsEnabled = true;
                        rdBtnTagsRespondOption.IsChecked = true;
                        // Disable embedded read data options
                        ReadDataGroupBox.IsEnabled = true;
                    }
                }
                //thingMagicReader.IsEnabled = true;
                //startRead.IsEnabled = true;
                btnRead.IsEnabled = true;
                //advanceReaderSettings.IsEnabled = true;
                ReaderTypeUIControl();
                if (objReader is SerialReader)
                {
                    this.cbxBaudRate.ItemsSource = null;
                    this.cbxBaudRate.ItemsSource = cbxDisplayBaudRateItem(model);
                    initializeBaudRate();
                }
                ConfigureAntennaBoxes(objReader);
                ConfigureLogicalAntennaBoxes(objReader);
                if (objReader is SerialReader)
                {
                    SerialReader reader = objReader as SerialReader;
                }
                supportedProtocols = (TagProtocol[])objReader.ParamGet("/reader/version/supportedProtocols");

                ConfigureProtocols(supportedProtocols);
                //btnConnect.ToolTip = "Disconnect";
                btnConnect.Content = "Disconnect";
                btnConnectExpander.Content = btnConnect.Content;

                //Enable save data, btnClearTagReads, read-once, readasyncread buttons
                saveData.IsEnabled = true;
                btnClearTagReads.IsEnabled = true;
                btnRead.IsEnabled = true;

                //startRead.IsEnabled = true;
                InitializeRdrDiagnostics();
                //Enable Read/Write option panel
                PerformAsyncReadInitialActions(true);
                RegulatoryOperation();
                //Enable UI controls based on model.
                if (model.Contains("M3e"))
                {
                    rdBtnEqualSwitching.IsEnabled = false;
                    rdBtnEqualSwitching.IsChecked = false;
                    rdBtnAutoSwitching.IsChecked = false;
                    rdBtnAutoSwitching.IsEnabled = false;
                    chkbxAntennaDetection.IsEnabled = false;
                    chkbxAntennaDetection.IsChecked = false;
                    cbxAntennamux.IsEnabled = false;
                    cbxAntennamux.IsChecked = false;
                }
                else
                {
                    rdBtnEqualSwitching.IsEnabled = true;
                    rdBtnEqualSwitching.IsChecked = false;
                    rdBtnAutoSwitching.IsEnabled = true;
                    rdBtnAutoSwitching.IsChecked = true;
                    chkbxAntennaDetection.IsEnabled = true;
                    cbxAntennamux.IsEnabled = true;
                }
                Mouse.SetCursor(Cursors.Arrow);
                CustomizedMessageBox = new URACustomMessageBoxWindow();
                channelOccupiedMessageBox = new URAChannelOccupiedMessageBoxWindow();

                // Clear firmware Update open file dialog status
                txtFirmwarePath.Text = "";
            }
            catch (Exception ex)
            {
                string error = "Connect failed to saved reader URI [" + uri + "]. Please make sure reader "
                    + " is connected and try again. Or, connect to desired reader first then load any saved "
                    + "configuration to it";
                Mouse.SetCursor(Cursors.Arrow);
                btnConnect.IsEnabled = true;
                btnRead.Visibility = Visibility.Hidden;
                initialReaderSettingsLoaded = true;
                lblshowStatus.Content = "Disconnected";
                rdbtnLocalConnection.IsEnabled = rdbtnNetworkConnection.IsEnabled = rdbtnCustomTransportConnection.IsEnabled = true;
                cmbFixedReaderAddr.IsEnabled = cmbReaderAddr.IsEnabled = txtCustomTransport.IsEnabled = true;
                chkEnableTransportLogging.IsEnabled = true;
                gridReadOptions.IsEnabled = false;
                regioncombo.IsEnabled = false;
                gridDisplayOptions.IsEnabled = false;
                if (ex is IOException)
                {
                    if (!(cmbReaderAddr.Text.Contains("COM") || cmbReaderAddr.Text.Contains("com")))
                    {
                        MessageBox.Show("Application needs a valid Reader Address of type COMx",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        if (isLoadSavedConfigurations)
                        {
                            MessageBox.Show("Connect failed to saved reader URI [" + cmbReaderAddr.Text + "]. "
                                + "Please make sure reader is connected and try again. Or, connect to desired reader first then "
                                + "load any saved configuration to it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Reader not connected on " + cmbReaderAddr.Text, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    cmbReaderAddr.IsEnabled = true;
                    cmbFixedReaderAddr.IsEnabled = true;
                    txtCustomTransport.IsEnabled = true;
                }
                else if (ex is ReaderException)
                {
                    if (-1 != ex.Message.IndexOf("target machine actively refused"))
                    {
                        if (isLoadSavedConfigurations)
                        {
                            MessageBox.Show("Error connecting to reader: " + error, "Reader Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                                "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message + ". Please update the module firmware.", "Reader Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        expdrFirmwareUpdate.IsExpanded = true;
                        expdrFirmwareUpdate.Focus();
                        //Dock the settings/status panel if not docked, to display Firmware update options
                        if (pane1Button.Visibility == System.Windows.Visibility.Visible)
                        {
                            pane1Button.RaiseEvent(new RoutedEventArgs(ButtonBase.MouseEnterEvent));
                            pane1Pin.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        }
                        if (expdrConnect.IsExpanded)
                        {
                            expdrConnect.IsExpanded = true;
                        }
                    }
                    else
                    {
                        if (isLoadSavedConfigurations)
                        {
                            MessageBox.Show("Error connecting to reader: " + ex.Message + " " + error,
                                "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else if (ex is UnauthorizedAccessException)
                {
                    MessageBox.Show("Access to " + cmbReaderAddr.Text + " denied. Please check if another "
                        + "program is accessing this port", "Error!", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    if (-1 != ex.Message.IndexOf("target machine actively refused"))
                    {
                        MessageBox.Show("Error connecting to reader: " + "Connection attempt failed...",
                            "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Error connecting to reader: " + ex.Message, "Reader Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                objReader.Destroy();
                ConfigureAntennaBoxes(null);
                ConfigureLogicalAntennaBoxes(null);
                ConfigureProtocols(null);
            }
        }

        /// <summary>
        /// Disable some of the UI controls when the connected reader is astra
        /// </summary>
        private void DisableControlsForAstra()
        {
            rdbtnAutoAdjstAsPoplChngs.IsEnabled = false;
            rdbtnOptmzExtmdNoTagsInField.IsEnabled = false;
            stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
            stkpnlRdDistVsrdRate.IsEnabled = false;
            rdBtnSlBstChforPoplSize.IsEnabled = false;
            rdBtnTagsRespondOption.IsEnabled = true;
            rdBtnTagsRespondOption.IsChecked = true;
            // Disable embedded read data options
            ReadDataGroupBox.IsEnabled = false;
            chkEmbeddedReadData.IsChecked = false;
        }

        /// <summary>
        /// Check for valid port numbers
        /// </summary>
        /// <param name="portNumber"></param>
        /// <returns></returns>
        private bool ValidatePortNumber(string portNumber)
        {
            List<string> portNames = new List<string>();
            List<string> portValues = new List<string>();
            //converting comport number from small letter to capital letter.Eg:com18 to COM18.
            MatchCollection mc1 = Regex.Matches(portNumber, @"(?<=\().+?(?=\))");
            foreach (Match m1 in mc1)
            {
                if (m1.ToString().ToUpperInvariant().Contains("COM"))
                {
                    portNumber = m1.ToString();
                }
            }
            // getting the list of comports value and name which device manager shows
            portNames = masterPortList;
            for (int i = 0; i < portNames.Count; i++)
            {
                MatchCollection mc = Regex.Matches(portNames[i], @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    portValues.Add(m.ToString());
                }
            }
            if ((portNames.Contains(cmbReaderAddr.Text)) || (portValues.Contains(portNumber)))
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
        /// Serial transport listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialListener(Object sender, TransportListenerEventArgs e)
        {
            transportLogFile.Write(String.Format("{0} {1}",
                DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), e.Tx ? "Sending" : "Received"));

            for (int i = 0; i < e.Data.Length; i++)
            {
                if ((i & 15) == 0)
                {
                    transportLogFile.WriteLine();
                    transportLogFile.Write("  ");
                }
                transportLogFile.Write("  " + e.Data[i].ToString("X2"));
            }
            transportLogFile.WriteLine();
            transportLogFile.Flush();
        }

        /// <summary>
        /// Rql transport listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RQLListener(Object sender, TransportListenerEventArgs e)
        {
            transportLogFile.Write(String.Format("{0} {1}",
                DateTime.Now.ToString("hh:mm:ss.fff tt"),
                e.Tx ? "Sending" : "Received"));

            // Create an ASCII encoding.
            Encoding ascii = Encoding.ASCII;
            String decodedString = "  " + ascii.GetString(e.Data);
            transportLogFile.Write(decodedString);
            transportLogFile.WriteLine();
            transportLogFile.Flush();
        }

        /// <summary>
        /// Llrp transport listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LLrpListener(Object sender, TransportListenerEventArgs e)
        {
            transportLogFile.Write(String.Format("{0} {1}",
                DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), e.Tx ? "Sending" : "Received"));

            // Create an ASCII encoding.
            Encoding ascii = Encoding.ASCII;
            String decodedString = "  " + ascii.GetString(e.Data);
            transportLogFile.Write(decodedString);
            transportLogFile.WriteLine();
            transportLogFile.Flush();
        }

        /// <summary>
        /// Select the reader set baud rate for the baud-rate combo-box
        /// </summary>
        private void initializeBaudRate()
        {
            int setBaudRate = 0;
            int cbxbaudrateindex = 0;
            setBaudRate = (int)this.objReader.ParamGet("/reader/baudRate");
            switch (setBaudRate)
            {
                case 9600:
                    cbxbaudrateindex = 1;
                    break;
                case 19200:
                    cbxbaudrateindex = 2;
                    break;
                case 38400:
                    cbxbaudrateindex = 3;
                    break;
                case 57600:
                    cbxbaudrateindex = 4;
                    break;
                case 115200:
                    cbxbaudrateindex = 5;
                    break;
                case 230400:
                    cbxbaudrateindex = 6;
                    break;
                case 460800:
                    cbxbaudrateindex = 7;
                    break;
                case 921600:
                    cbxbaudrateindex = 8;
                    break;
                default:
                    cbxbaudrateindex = 0;
                    break;
            }
            cbxBaudRate.SelectedItem = cbxBaudRate.Items.GetItemAt(cbxbaudrateindex);
        }

        /// <summary>
        /// Fixed reader connection initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdbtnNetworkConnection_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != lblbaudRate)
                {
                    lblbaudRate.Visibility = System.Windows.Visibility.Collapsed;
                    cbxBaudRate.Visibility = System.Windows.Visibility.Collapsed;
                    cmbReaderAddr.ItemsSource = "";
                    if (bonjour.IsBonjourServicesInstalled)
                    {
                        btnRefreshReadersList.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        btnRefreshReadersList.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    ToolTip networkRdrToolTip = new ToolTip();
                    networkRdrToolTip.Content = "Enter reader IPaddress/Hostname";
                    cmbReaderAddr.ToolTip = networkRdrToolTip;
                    btnRefreshReadersList.ToolTip = "Refresh host name list";
                    InitializeReaderUriBox();
                    cmbReaderAddr.Visibility = System.Windows.Visibility.Collapsed;
                    txtCustomTransport.Visibility = Visibility.Collapsed;
                    cmbFixedReaderAddr.Visibility = System.Windows.Visibility.Visible;
                    InitializeColumnSelectionCbx();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Serial reader connection initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdbtnLocalConnection_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lblbaudRate != null)
                {
                    if (rdbtnLocalConnection.IsChecked == true)
                    {
                        cmbFixedReaderAddr.Visibility = Visibility.Collapsed;
                        cmbReaderAddr.Visibility = System.Windows.Visibility.Visible;
                        txtCustomTransport.Visibility = Visibility.Collapsed;
                        btnRefreshReadersList.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        cmbReaderAddr.Visibility = System.Windows.Visibility.Collapsed;
                        txtCustomTransport.Visibility = Visibility.Visible;
                        cmbFixedReaderAddr.Visibility = Visibility.Collapsed;
                        btnRefreshReadersList.Visibility = Visibility.Collapsed;
                    }
                    cmbFixedReaderAddr.Visibility = System.Windows.Visibility.Hidden;
                    lblbaudRate.Visibility = System.Windows.Visibility.Visible;
                    cbxBaudRate.Visibility = System.Windows.Visibility.Visible;
                    ToolTip serialRdrToolTip = new ToolTip();
                    serialRdrToolTip.Content = "Enter COM port or press Refresh to refresh comport list";
                    cmbReaderAddr.ToolTip = serialRdrToolTip;
                    btnRefreshReadersList.ToolTip = "Refresh Comport list";
                    InitializeReaderUriBox();
                    btnWebUI.Visibility = System.Windows.Visibility.Collapsed;
                    cbxBaudRate.IsEnabled = true;
                    InitializeColumnSelectionCbx();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Set selected region to the connected reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void regioncombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (regioncombo.Items.Count > 0)
            {
                if (regioncombo.SelectedItem != null)
                {
                    if ("" != regioncombo.SelectedItem.ToString())
                    {
                        if (objReader is SerialReader)
                        {
                            objReader.ParamSet("/reader/region/id",
                                Enum.Parse(typeof(Reader.Region), regioncombo.SelectedItem.ToString()));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the baud rate selected to the serial port 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != objReader)
            {
                if (cbxBaudRate.Items.Count > 0)
                {
                    if (cbxBaudRate.SelectedItem != null)
                    {
                        if (objReader is SerialReader)
                        {
                            if (((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString() != "Select")
                            {
                                objReader.ParamSet("/reader/baudRate",
                                    Convert.ToInt32(((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString()));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set selected region to the connected reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void regioncombo_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (regioncombo.Items.Count > 0)
                {
                    if (regioncombo.SelectedItem != null)
                    {
                        if ("" != regioncombo.SelectedItem.ToString())
                        {
                            if (objReader is SerialReader)
                            {
                                expdrOpenRegConfig.IsEnabled = false;
                                expdrOpenRegConfig.Visibility = Visibility.Hidden;
                                expdrOpenRegConfig.IsExpanded = false;
                                txtbxHopTable_Cnot.Text = string.Empty;
                                if (regioncombo.SelectedItem.ToString() != "Select")
                                {
                                    Reader.Region getRegion = regionToSet;
                                    if (regioncombo.SelectedItem.ToString() == "OPEN")
                                    {
                                        //If region is already OPEN on both module and in URA, no need to send command again to module.
                                        if (!(Enum.Parse(typeof(Reader.Region), regioncombo.SelectedItem.ToString()).Equals((object)getRegion)))
                                        {
                                            objReader.ParamSet("/reader/region/id", Reader.Region.OPEN);
                                        }
                                        expdrOpenRegConfig.IsEnabled = true;
                                        expdrOpenRegConfig.Visibility = Visibility.Visible;
                                        expdrOpenRegConfig.IsExpanded = true;
                                        // update the regionToSet value 
                                        regionToSet = Reader.Region.OPEN;
                                    }
                                    else if (!(Enum.Parse(typeof(Reader.Region), regioncombo.SelectedItem.ToString()).Equals((object)getRegion)))
                                    {
                                        objReader.ParamSet("/reader/region/id",
                                            Enum.Parse(typeof(Reader.Region), regioncombo.SelectedItem.ToString()));
                                        // update the regionToSet value 
                                        regionToSet = (Reader.Region)(Enum.Parse(typeof(Reader.Region), regioncombo.SelectedItem.ToString()));
                                    }
                                    else
                                    {
                                        expdrOpenRegConfig.IsEnabled = false;
                                        expdrOpenRegConfig.Visibility = Visibility.Hidden;
                                    }
                                    //try
                                    //{
                                    //    lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                                    //}
                                    //catch (Exception)
                                    //{
                                    //}

                                    // Reinitialize max and min read power for read power slider, when 
                                    // region is set on the reader
                                    InitializeRdPwrSldrMaxNMinValue();
                                    if (!(model.Equals("M3e")))
                                    {
                                        InitializeHopTable();
                                    }
                                }
                                else
                                {
                                    txtbxHopTable.Text = string.Empty;
                                    txtbxHopTable_Cnot.Text = string.Empty;
                                }
                            }
                            else
                            {
                                //this area is for the network reader
                                InitializeHopTable();// region is set for network reader
                                //btnDefaultHopTable.Visibility = Visibility.Visible;
                            }

                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); Onlog(ex); }
        }
        /// <summary>
        /// Initialize hop table frequencies in Reader Diagnostics
        /// </summary>
        public void InitializeHopTable()
        {
            int[] hopTableList = (int[])objReader.ParamGet("/reader/region/hopTable");
            txtbxHopTable.Text = string.Empty;
            txtbxHopTable.Text = String.Join(",", hopTableList.Select(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Set the baud rate selected to the serial port 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxBaudRate_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (null != objReader)
            {
                SetBaudRate();
            }
        }

        /// <summary>
        /// Set the selected baud rate
        /// </summary>
        private void SetBaudRate()
        {
            if (cbxBaudRate.Items.Count > 0)
            {
                if (cbxBaudRate.SelectedItem != null)
                {
                    if (objReader is SerialReader)
                    {
                        if (((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString() != "Select")
                        {
                            objReader.ParamSet("/reader/baudRate",
                                Convert.ToInt32(((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString()));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Open the web page of the connected reader if fixed reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWebUI_Click(object sender, RoutedEventArgs e)
        {
            if (cmbFixedReaderAddr.Text != "")
            {
                string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(cmbFixedReaderAddr.Text)).FirstOrDefault();
                string readerUri;
                if (string.IsNullOrWhiteSpace(key) || key == null)
                    readerUri = cmbFixedReaderAddr.Text;
                else
                    readerUri = bonjour.HostNameIpAddress[key];
                MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                foreach (Match m in mc)
                {
                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                        readerUri = m.ToString();
                }
                System.Diagnostics.Process.Start("http://" + readerUri);
            }
        }

        #endregion Connect

        #region ReadOptions

        /// <summary>
        /// Based on the protocol being picked to perform a select operation accordingly.
        /// For example if Gen2, then it should change select screen to word address. on
        /// the other hand if it is ISO18-6b, then it be byte address.
        /// </summary>
        private void OnProtocolSelect()
        {
            try
            {
                if (iso6bCheckBox != null)
                {
                    if ((bool)iso6bCheckBox.IsChecked)
                    {
                        iso6bCheckBoxChecked = true;
                    }
                    else if (((bool)gen2CheckBox.IsChecked)
                        || ((bool)ipx64CheckBox.IsChecked)
                        || ((bool)ipx256CheckBox.IsChecked)
                        || ((bool)ataCheckBox.IsChecked)
                        || (bool)isoUcodeCheckbox.IsChecked)
                    {
                        iso6bCheckBoxChecked = false;
                    }
                }

                List<CheckBox> protocolCheckBox = new List<CheckBox>();
                protocolCheckBox.Add(iso6bCheckBox);
                protocolCheckBox.Add(gen2CheckBox);
                protocolCheckBox.Add(ipx64CheckBox);
                protocolCheckBox.Add(ipx256CheckBox);
                protocolCheckBox.Add(ataCheckBox);
                protocolCheckBox.Add(isoUcodeCheckbox);
                int count = 0;


                if (cbxcolumnSelection != null)
                {
                    for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
                    {
                        ColumnSelectionForTagResult rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);
                        if (rda.SelectedColumn == "Protocol")
                        {
                            if (rda.IsColumnChecked)
                            {
                                TagResults.protocolColumn.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                            {
                                if (btnConnect.Content.ToString() != "Connect")
                                {
                                    foreach (CheckBox cbx in protocolCheckBox)
                                    {
                                        if (cbx.Visibility == Visibility.Visible && cbx.IsChecked == true)
                                            count++;
                                        if (count > 1)
                                        {
                                            rda.IsColumnChecked = true;
                                            TagResults.protocolColumn.Visibility = System.Windows.Visibility.Visible;
                                            break;
                                        }
                                        else
                                        {
                                            rda.IsColumnChecked = false;
                                            TagResults.protocolColumn.Visibility = System.Windows.Visibility.Collapsed;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Onlog(ex); }
        }

        /// <summary>
        /// Initialize read plans to perform read
        /// </summary>
        private void SetReadPlans()
        {
            try
            {
                List<int> ant = GetSelectedAntennaList();
                bool isFastSearchEnabled = (bool)chkEnableFastSearch.IsChecked;


                //Setup embedded tagOp settings if option checked
                TagOp embTagOp = null;
                Gen2.Bank opMemBank = Gen2.Bank.TID;

                if (model.Equals("M3e"))
                {
                    if ((bool)chkBlockRead.IsChecked)
                    {
                        TagResults.dataColumn.Visibility = Visibility.Visible;
                        TagResults.dataSecureColumn.Visibility = Visibility.Collapsed;
                        TagResults.dataColumn.Header = "Tag Data";
                        embTagOp = new ReadMemory(MemoryType.TAG_MEMORY, Convert.ToUInt32(Utilities.CheckHexOrDecimal(txtBlockAddress.Text)), Convert.ToByte(Utilities.CheckHexOrDecimal(txtReadLength.Text)));
                    }
                    else if ((bool)chkSecureRead.IsChecked)
                    {
                        TagResults.dataColumn.Visibility = Visibility.Collapsed;
                        TagResults.dataSecureColumn.Visibility = Visibility.Visible;
                        TagResults.dataSecureColumn.Header = "Secure ID";
                        embTagOp = new ReadMemory(MemoryType.SECURE_ID, 0, 0);
                    }
                    else
                    {
                        TagResults.dataColumn.Visibility = Visibility.Collapsed;
                        TagResults.dataSecureColumn.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if ((bool)chkEmbeddedReadData.IsChecked)
                    {
                        //automatically enable the Data column if embedded tagOp is enabled
                        TagResults.dataColumn.Visibility = System.Windows.Visibility.Visible;
                        TagResults.dataSecureColumn.Visibility = Visibility.Collapsed;
                        TagResults.dataColumn.Header = "Data";
                        switch (cbxReadDataBank.Text)
                        {
                            case "Reserved":
                                opMemBank = Gen2.Bank.RESERVED;
                                break;
                            case "EPC":
                                opMemBank = Gen2.Bank.EPC;
                                break;
                            case "TID":
                                opMemBank = Gen2.Bank.TID;
                                break;
                            case "User":
                                opMemBank = Gen2.Bank.USER;
                                break;
                        };

                        embTagOp = new Gen2.ReadData(opMemBank, Convert.ToUInt32(Utilities.CheckHexOrDecimal(
                            txtembReadStartAddr.Text)), Convert.ToByte(Utilities.CheckHexOrDecimal(txtembReadLength.Text)));
                        objReader.ParamSet("/reader/tagreaddata/uniquebydata", (bool)chkUniqueByData.IsChecked);
                    }
                    else
                    {
                        TagResults.dataColumn.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
                //END Setup embedded tagOp settings if option checked

                //Setup Select Filter settings if option checked
                Gen2.Select searchSelect = null;
                Gen2.Select searchSelect2 = null;
                Gen2.Select searchSelect3 = null;
                MultiFilter multifilter = null;

                Gen2.Bank selectMemBank = Gen2.Bank.EPC;
                Gen2.Bank selectMemBank2 = Gen2.Bank.EPC;
                Gen2.Bank selectMemBank3 = Gen2.Bank.EPC;

                if ((bool)chkApplyFilter1.IsChecked)
                {

                    switch (MultiFilters1.cbxFilterMemBank.Text)
                    {
                        case "EPC ID":
                            selectMemBank = Gen2.Bank.EPC;
                            break;
                        case "TID":
                            selectMemBank = Gen2.Bank.TID;
                            break;
                        case "User":
                            selectMemBank = Gen2.Bank.USER;
                            break;
                        case "EPC Truncate":
                            selectMemBank = Gen2.Bank.GEN2EPCTRUNCATE;
                            break;
                        case "EPC Length":
                            selectMemBank = Gen2.Bank.GEN2EPCLENGTHFILTER;
                            break;
                    };

                    int discard;
                    byte[] SearchSelectData = Utilities.GetBytes(
                        Utilities.RemoveHexstringPrefix(MultiFilters1.txtFilterData.Text), out discard);
                    // Enter inside if condition, only if filter length is in odd nibbles(hex characters)
                    if (discard == 1 && SearchSelectData.Length == 0)
                    {
                        // If only one hex character(one nibble) is specified as a filter.
                        // For ex: 0xa, convert into byte array
                        byte objByte = (byte)(Utilities.HexToByte(
                            Utilities.RemoveHexstringPrefix(MultiFilters1.txtFilterData.Text).TrimEnd()) << 4);
                        SearchSelectData = new byte[] { objByte };
                    }
                    else if (discard == 1)
                    {
                        // If filter length is in odd nibbles. For ex: 0xabc , 0xabcde, 0xabcdefg
                        // after converting to byte array we get 0xab, 0xc0 if the specified filter is 0xabc
                        //Adding omitted character to byte array
                        Array.Resize(ref SearchSelectData, SearchSelectData.Length + 1);
                        byte objByte = (byte)(((Utilities.HexToByte(
                            Utilities.RemoveHexstringPrefix(MultiFilters1.txtFilterData.Text).Substring(
                            Utilities.RemoveHexstringPrefix(
                            MultiFilters1.txtFilterData.Text).Length - 1).TrimEnd())) << 4));
                        Array.Copy(new object[] { objByte }, 0, SearchSelectData, SearchSelectData.Length - 1, 1);
                    }

                    UInt16 dataLength;
                    if (MultiFilters1.txtFilterData.Text != "")
                        dataLength = Convert.ToUInt16(Utilities.RemoveHexstringPrefix(MultiFilters1.txtFilterData.Text).Length * 4);//calculate the length in the form of nibbles
                    else
                        dataLength = 0;

                    if (MultiFilters1.cbxFilterMemBank.Text == "EPC Length")
                    {
                        dataLength = Convert.ToUInt16(Utilities.CheckHexOrDecimal(MultiFilters1.txtFilterEPCLength.Text));
                        searchSelect = new Gen2.Select(false, Gen2.Bank.GEN2EPCLENGTHFILTER, 16, dataLength, new byte[] { 0x30, 0x00 });
                    }
                    else
                    {
                        searchSelect = new Gen2.Select((bool)MultiFilters1.chkFilterInvert.IsChecked, selectMemBank, Convert.ToUInt32(Utilities.CheckHexOrDecimal(MultiFilters1.txtFilterStartAddr.Text)), dataLength, SearchSelectData);
                    }
                    switch (MultiFilters1.cbxFilterTarget.Text)
                    {
                        case "INV S0":
                            searchSelect.target = Gen2.Select.Target.Inventoried_S0;
                            break;
                        case "INV S1":
                            searchSelect.target = Gen2.Select.Target.Inventoried_S1;
                            break;
                        case "INV S2":
                            searchSelect.target = Gen2.Select.Target.Inventoried_S2;
                            break;
                        case "INV S3":
                            searchSelect.target = Gen2.Select.Target.Inventoried_S3;
                            break;
                        case "SELECT":
                            searchSelect.target = Gen2.Select.Target.Select;
                            break;
                    };

                    switch (MultiFilters1.cbxFilterAction.Text)
                    {
                        case "0":
                            searchSelect.action = Gen2.Select.Action.ON_N_OFF;
                            break;
                        case "1":
                            searchSelect.action = Gen2.Select.Action.ON_N_NOP;
                            break;
                        case "2":
                            searchSelect.action = Gen2.Select.Action.NOP_N_OFF;
                            break;
                        case "3":
                            searchSelect.action = Gen2.Select.Action.NEG_N_NOP;
                            break;
                        case "4":
                            searchSelect.action = Gen2.Select.Action.OFF_N_ON;
                            break;
                        case "5":
                            searchSelect.action = Gen2.Select.Action.OFF_N_NOP;
                            break;
                        case "6":
                            searchSelect.action = Gen2.Select.Action.NOP_N_ON;
                            break;
                        case "7":
                            searchSelect.action = Gen2.Select.Action.NOP_N_NEG;
                            break;
                    };

                    if ((bool)chkApplyFilter2.IsChecked)
                    {
                        switch (MultiFilters2.cbxFilterMemBank.Text)
                        {
                            case "EPC ID":
                                selectMemBank2 = Gen2.Bank.EPC;
                                break;
                            case "TID":
                                selectMemBank2 = Gen2.Bank.TID;
                                break;
                            case "User":
                                selectMemBank2 = Gen2.Bank.USER;
                                break;
                            case "EPC Truncate":
                                selectMemBank2 = Gen2.Bank.GEN2EPCTRUNCATE;
                                break;
                            case "EPC Length":
                                selectMemBank2 = Gen2.Bank.GEN2EPCLENGTHFILTER;
                                break;
                        };

                        int discard2 = 0;
                        byte[] SearchSelectData2 = Utilities.GetBytes(
                            Utilities.RemoveHexstringPrefix(MultiFilters2.txtFilterData.Text), out discard2);
                        // Enter inside if condition, only if filter length is in odd nibbles(hex characters)
                        if (discard2 == 1 && SearchSelectData2.Length == 0)
                        {
                            // If only one hex character(one nibble) is specified as a filter.
                            // For ex: 0xa, convert into byte array
                            byte objByte = (byte)(Utilities.HexToByte(
                                Utilities.RemoveHexstringPrefix(MultiFilters2.txtFilterData.Text).TrimEnd()) << 4);
                            SearchSelectData2 = new byte[] { objByte };
                        }
                        else if (discard2 == 1)
                        {
                            // If filter length is in odd nibbles. For ex: 0xabc , 0xabcde, 0xabcdefg
                            // after converting to byte array we get 0xab, 0xc0 if the specified filter is 0xabc
                            //Adding omitted character to byte array
                            Array.Resize(ref SearchSelectData2, SearchSelectData2.Length + 1);
                            byte objByte = (byte)(((Utilities.HexToByte(
                                Utilities.RemoveHexstringPrefix(MultiFilters2.txtFilterData.Text).Substring(
                                Utilities.RemoveHexstringPrefix(
                                MultiFilters2.txtFilterData.Text).Length - 1).TrimEnd())) << 4));
                            Array.Copy(new object[] { objByte }, 0, SearchSelectData2, SearchSelectData2.Length - 1, 1);
                        }

                        UInt16 dataLength2;
                        if (MultiFilters2.txtFilterData.Text != "")
                            dataLength2 = Convert.ToUInt16(Utilities.RemoveHexstringPrefix(MultiFilters2.txtFilterData.Text).Length * 4);//calculate the length in the form of nibbles
                        else
                            dataLength2 = 0;

                        if (MultiFilters2.cbxFilterMemBank.Text == "EPC Length")
                        {
                            dataLength2 = Convert.ToUInt16(Utilities.CheckHexOrDecimal(MultiFilters2.txtFilterEPCLength.Text));
                            searchSelect2 = new Gen2.Select(false, Gen2.Bank.GEN2EPCLENGTHFILTER, 16, dataLength2, new byte[] { 0x30, 0x00 });
                        }
                        else
                        {
                            searchSelect2 = new Gen2.Select((bool)MultiFilters2.chkFilterInvert.IsChecked, selectMemBank2, Convert.ToUInt32(Utilities.CheckHexOrDecimal(MultiFilters2.txtFilterStartAddr.Text)), dataLength, SearchSelectData2);
                        }
                        switch (MultiFilters2.cbxFilterTarget.Text)
                        {
                            case "INV S0":
                                searchSelect2.target = Gen2.Select.Target.Inventoried_S0;
                                break;
                            case "INV S1":
                                searchSelect2.target = Gen2.Select.Target.Inventoried_S1;
                                break;
                            case "INV S2":
                                searchSelect2.target = Gen2.Select.Target.Inventoried_S2;
                                break;
                            case "INV S3":
                                searchSelect2.target = Gen2.Select.Target.Inventoried_S3;
                                break;
                            case "SELECT":
                                searchSelect2.target = Gen2.Select.Target.Select;
                                break;
                        };

                        switch (MultiFilters2.cbxFilterAction.Text)
                        {
                            case "0":
                                searchSelect2.action = Gen2.Select.Action.ON_N_OFF;
                                break;
                            case "1":
                                searchSelect2.action = Gen2.Select.Action.ON_N_NOP;
                                break;
                            case "2":
                                searchSelect2.action = Gen2.Select.Action.NOP_N_OFF;
                                break;
                            case "3":
                                searchSelect2.action = Gen2.Select.Action.NEG_N_NOP;
                                break;
                            case "4":
                                searchSelect2.action = Gen2.Select.Action.OFF_N_ON;
                                break;
                            case "5":
                                searchSelect2.action = Gen2.Select.Action.OFF_N_NOP;
                                break;
                            case "6":
                                searchSelect2.action = Gen2.Select.Action.NOP_N_ON;
                                break;
                            case "7":
                                searchSelect2.action = Gen2.Select.Action.NOP_N_NEG;
                                break;
                        };
                        if ((bool)chkApplyFilter3.IsChecked)
                        {
                            //Gen2.Select searchSelect3 = null;
                            //Gen2.Bank selectMemBank = Gen2.Bank.EPC;
                            switch (MultiFilters3.cbxFilterMemBank.Text)
                            {
                                case "EPC ID":
                                    selectMemBank3 = Gen2.Bank.EPC;
                                    break;
                                case "TID":
                                    selectMemBank3 = Gen2.Bank.TID;
                                    break;
                                case "User":
                                    selectMemBank3 = Gen2.Bank.USER;
                                    break;
                                case "EPC Truncate":
                                    selectMemBank3 = Gen2.Bank.GEN2EPCTRUNCATE;
                                    break;
                                case "EPC Length":
                                    selectMemBank3 = Gen2.Bank.GEN2EPCLENGTHFILTER;
                                    break;
                            };

                            int discard3 = 0;
                            byte[] SearchSelectData3 = Utilities.GetBytes(
                                Utilities.RemoveHexstringPrefix(MultiFilters3.txtFilterData.Text), out discard3);
                            // Enter inside if condition, only if filter length is in odd nibbles(hex characters)
                            if (discard3 == 1 && SearchSelectData3.Length == 0)
                            {
                                // If only one hex character(one nibble) is specified as a filter.
                                // For ex: 0xa, convert into byte array
                                byte objByte = (byte)(Utilities.HexToByte(
                                    Utilities.RemoveHexstringPrefix(MultiFilters3.txtFilterData.Text).TrimEnd()) << 4);
                                SearchSelectData3 = new byte[] { objByte };
                            }
                            else if (discard3 == 1)
                            {
                                // If filter length is in odd nibbles. For ex: 0xabc , 0xabcde, 0xabcdefg
                                // after converting to byte array we get 0xab, 0xc0 if the specified filter is 0xabc
                                //Adding omitted character to byte array
                                Array.Resize(ref SearchSelectData3, SearchSelectData3.Length + 1);
                                byte objByte = (byte)(((Utilities.HexToByte(
                                    Utilities.RemoveHexstringPrefix(MultiFilters3.txtFilterData.Text).Substring(
                                    Utilities.RemoveHexstringPrefix(
                                    MultiFilters3.txtFilterData.Text).Length - 1).TrimEnd())) << 4));
                                Array.Copy(new object[] { objByte }, 0, SearchSelectData3, SearchSelectData3.Length - 1, 1);
                            }

                            UInt16 dataLength3;
                            if (MultiFilters3.txtFilterData.Text != "")
                                dataLength3 = Convert.ToUInt16(Utilities.RemoveHexstringPrefix(MultiFilters3.txtFilterData.Text).Length * 4);//calculate the length in the form of nibbles
                            else
                                dataLength3 = 0;

                            if (MultiFilters3.cbxFilterMemBank.Text == "EPC Length")
                            {
                                dataLength3 = Convert.ToUInt16(Utilities.CheckHexOrDecimal(MultiFilters3.txtFilterEPCLength.Text));
                                searchSelect3 = new Gen2.Select(false, Gen2.Bank.GEN2EPCLENGTHFILTER, 16, dataLength3, new byte[] { 0x30, 0x00 });
                            }
                            else
                            {
                                searchSelect3 = new Gen2.Select((bool)MultiFilters3.chkFilterInvert.IsChecked, selectMemBank3, Convert.ToUInt32(Utilities.CheckHexOrDecimal(MultiFilters3.txtFilterStartAddr.Text)), dataLength, SearchSelectData3);
                            }
                            switch (MultiFilters3.cbxFilterTarget.Text)
                            {
                                case "INV S0":
                                    searchSelect3.target = Gen2.Select.Target.Inventoried_S0;
                                    break;
                                case "INV S1":
                                    searchSelect3.target = Gen2.Select.Target.Inventoried_S1;
                                    break;
                                case "INV S2":
                                    searchSelect3.target = Gen2.Select.Target.Inventoried_S2;
                                    break;
                                case "INV S3":
                                    searchSelect3.target = Gen2.Select.Target.Inventoried_S3;
                                    break;
                                case "SELECT":
                                    searchSelect3.target = Gen2.Select.Target.Select;
                                    break;
                            };

                            switch (MultiFilters3.cbxFilterAction.Text)
                            {
                                case "0":
                                    searchSelect3.action = Gen2.Select.Action.ON_N_OFF;
                                    break;
                                case "1":
                                    searchSelect3.action = Gen2.Select.Action.ON_N_NOP;
                                    break;
                                case "2":
                                    searchSelect3.action = Gen2.Select.Action.NOP_N_OFF;
                                    break;
                                case "3":
                                    searchSelect3.action = Gen2.Select.Action.NEG_N_NOP;
                                    break;
                                case "4":
                                    searchSelect3.action = Gen2.Select.Action.OFF_N_ON;
                                    break;
                                case "5":
                                    searchSelect3.action = Gen2.Select.Action.OFF_N_NOP;
                                    break;
                                case "6":
                                    searchSelect3.action = Gen2.Select.Action.NOP_N_ON;
                                    break;
                                case "7":
                                    searchSelect3.action = Gen2.Select.Action.NOP_N_NEG;
                                    break;
                            };
                            //three filters applied
                            multifilter = new MultiFilter(new TagFilter[] { searchSelect, searchSelect2, searchSelect3 });
                        }
                        else
                        {//Two filters applied
                            multifilter = new MultiFilter(new TagFilter[] { searchSelect, searchSelect2 });
                        }
                    }
                    else
                    {//only one filter
                        //searchSelect = new Gen2.Select((bool)MultiFilters1.chkFilterInvert.IsChecked, selectMemBank, Convert.ToUInt32(Utilities.CheckHexOrDecimal(MultiFilters1.txtFilterStartAddr.Text)), dataLength, SearchSelectData);
                    }
                }
                else
                {
                    searchSelect = null;
                }
                //END Setup Select Filter settings if option checked

                if (0 == ant.Count)
                {
                    throw new Exception("Please select at least one antenna");
                }

                simpleReadPlans.Clear();
                List<TagProtocol> protlist = new List<TagProtocol>();

                if (model.Equals("M3e"))//For M3e only
                {
                    if (!((bool)rdBtnAutoProtocolSwitching.IsChecked))//Equal protocol switching
                    {
                        if ((bool)iso14443aCheckBox.IsChecked)
                        {
                            CreateReadPlan(TagProtocol.ISO14443A, null, embTagOp, false);
                        }
                        if ((bool)iso15693CheckBox.IsChecked)
                        {
                            CreateReadPlan(TagProtocol.ISO15693, null, embTagOp, false);
                        }
                        if ((bool)lf125KHZCheckBox.IsChecked)
                        {
                            CreateReadPlan(TagProtocol.LF125KHZ, null, embTagOp, false);
                        }
                    }
                    else//Dynamic protocol switching
                    {
                        List<TagProtocol> protocolList = new List<TagProtocol>();
                        if ((bool)iso14443aCheckBox.IsChecked)
                        {
                            protocolList.Add(TagProtocol.ISO14443A);
                        }
                        if ((bool)iso15693CheckBox.IsChecked)
                        {
                            protocolList.Add(TagProtocol.ISO15693);
                        }
                        if ((bool)lf125KHZCheckBox.IsChecked)
                        {
                            protocolList.Add(TagProtocol.LF125KHZ);
                        }

                        if (protocolList.Count > 0)
                        {
                            objReader.ParamSet("/reader/protocolList", protocolList.ToArray());
                            CreateReadPlan(protocolList[0], null, embTagOp, false);
                        }
                    }
                }
                else//For UHF readers only
                {
                    if ((bool)gen2CheckBox.IsChecked && !(bool)chkApplyFilter1.IsChecked)
                    {//no filter selected
                        CreateReadPlan(TagProtocol.GEN2, searchSelect, embTagOp, isFastSearchEnabled);
                        protlist.Add(TagProtocol.GEN2);
                    }
                    else if ((bool)gen2CheckBox.IsChecked && (bool)chkApplyFilter1.IsChecked && !(bool)chkApplyFilter2.IsChecked)
                    {//Single filter selected
                        CreateReadPlan(TagProtocol.GEN2, searchSelect, embTagOp, isFastSearchEnabled);
                        protlist.Add(TagProtocol.GEN2);
                    }
                    else if ((bool)gen2CheckBox.IsChecked && (bool)chkApplyFilter1.IsChecked && (bool)chkApplyFilter2.IsChecked)
                    {//multi select plan has to be set
                        CreateReadPlan(TagProtocol.GEN2, multifilter, embTagOp, isFastSearchEnabled);
                        protlist.Add(TagProtocol.GEN2);
                    }
                    if ((bool)iso6bCheckBox.IsChecked)
                    {
                        CreateReadPlan(TagProtocol.ISO180006B, null, null, isFastSearchEnabled);
                        protlist.Add(TagProtocol.ISO180006B);
                    }
                    if ((bool)ipx64CheckBox.IsChecked)
                    {
                        CreateReadPlan(TagProtocol.IPX64, null, null, isFastSearchEnabled);
                        protlist.Add(TagProtocol.IPX64);
                    }
                    if ((bool)ipx256CheckBox.IsChecked)
                    {
                        CreateReadPlan(TagProtocol.IPX256, null, null, isFastSearchEnabled);
                        protlist.Add(TagProtocol.IPX256);
                    }
                    if ((bool)ataCheckBox.IsChecked)
                    {
                        CreateReadPlan(TagProtocol.ATA, null, null, isFastSearchEnabled);
                        protlist.Add(TagProtocol.ATA);
                    }
                    if ((bool)isoUcodeCheckbox.IsChecked)
                    {
                        CreateReadPlan(TagProtocol.ISO180006B_UCODE, null, null, isFastSearchEnabled);
                        protlist.Add(TagProtocol.ISO180006B_UCODE);
                    }
                }

                if (simpleReadPlans.ToArray().Length == 0)
                {
                    throw new Exception("Please select at least one protocol");
                    //objReader.ParamSet("/reader/read/plan", new SimpleReadPlan(ant.ToArray(), TagProtocol.GEN2, searchSelect, embTagOp, isFastSearchEnabled));
                }
                else
                {
                    StoreReaderSettings(true, ant, protlist, isFastSearchEnabled);
                    ReadPlan plan;
                    if (simpleReadPlans.Count == 1)
                    {
                        plan = (SimpleReadPlan)simpleReadPlans[0];
                    }
                    else
                    {
                        plan = new MultiReadPlan(simpleReadPlans);
                    }
                    objReader.ParamSet("/reader/read/plan", plan);
                }
                simpleReadPlans.Clear();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // <summary>
        /// Create read plans to perform read
        /// </summary>
        private void CreateReadPlan(TagProtocol protocol, TagFilter searchSelect, TagOp embTagOp, bool isFastSearchEnabled)
        {
            SerialReader.TagMetadataFlag tg = new SerialReader.TagMetadataFlag();
            TagType tagType = null;
            TagType secureTagType = null;

            #region M3e Filter Region
            //byte[] uid = null;
            ICollection<byte> uid;
            List<TagFilter> filterList = new List<TagFilter>();
            #endregion
            #region HF-LF filters
            if (model.Equals("M3e"))
            {
                if (true)
                {
                    if (!(String.IsNullOrWhiteSpace(txtM3eUID.Text)))//(!(txtM3eUID.Text == null && txtM3eUID.Text == ""))
                    {
                        try
                        {
                            string tmp = txtM3eUID.Text;
                            int tmpLength = 0;
                            if ((tmp.Length % 2) != 0)
                            {
                                tmp = tmp.Insert((tmp.Length), "0");
                                tmpLength = tmp.Length - 1;
                            }
                            else
                            {
                                tmpLength = tmp.Length;
                            }
                            uid = ByteFormat.FromHex(tmp);
                            TagFilter uidFilter = new Select_UID((byte)((tmpLength) * 4), uid);
                            filterList.Add(uidFilter);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Invalid UID Length.\r\nUID length: Should be more than 8 letters and in multiple of 4.");
                        }
                    }
                    if ((bool)rdBtnAutoProtocolSwitching.IsChecked)
                    {
                        if (!(lbTagType.Items.Count == 0))
                        {
                            Iso14443a.TagType iso14443aTag = new Iso14443a.TagType();
                            Iso15693.TagType iso15693Tag = new Iso15693.TagType();
                            Lf125khz.TagType lf125Tag = new Lf125khz.TagType();
                            foreach (ColumnSelectionForTagButton item in selectedTagForList)
                            {
                                switch (item.ButtonNameTag)
                                {
                                    case "MIFARE PLUS":
                                        iso14443aTag |= Iso14443a.TagType.MIFARE_PLUS;
                                        break;
                                    case "MIFARE ULTRALIGHT":
                                        iso14443aTag |= Iso14443a.TagType.MIFARE_ULTRALIGHT;
                                        break;
                                    case "MIFARE CLASSIC":
                                        iso14443aTag |= Iso14443a.TagType.MIFARE_CLASSIC;
                                        break;
                                    case "NTAG":
                                        iso14443aTag |= Iso14443a.TagType.NTAG;
                                        break;
                                    case "MIFARE DESFIRE":
                                        iso14443aTag |= Iso14443a.TagType.MIFARE_DESFIRE;
                                        break;
                                    case "MIFARE MINI":
                                        iso14443aTag |= Iso14443a.TagType.MIFARE_MINI;
                                        break;
                                    case "ICODE DNA":
                                        iso15693Tag |= Iso15693.TagType.ICODE_DNA;
                                        break;
                                    case "ICODE SLIX":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX;
                                        break;
                                    case "ICODE SLIX2":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_2;
                                        break;
                                    case "ICODE SLIX L":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_L;
                                        break;
                                    case "ICODE SLIX S":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_S;
                                        break;
                                    case "iCLASS SE":
                                        iso15693Tag |= Iso15693.TagType.HID_ICLASS_SE;
                                        break;
                                    case "ICODE SLI":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI;
                                        break;
                                    case "VIGO":
                                        iso15693Tag |= Iso15693.TagType.VIGO;
                                        break;
                                    case "TAGIT":
                                        iso15693Tag |= Iso15693.TagType.TAGIT;
                                        break;
                                    case "ICODE SLI L":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI_L;
                                        break;
                                    case "ICODE SLI S":
                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI_S;
                                        break;
                                    case "PICOPASS":
                                        iso15693Tag |= Iso15693.TagType.PICOPASS;
                                        break;
                                    case "AWID":
                                        lf125Tag |= Lf125khz.TagType.AWID;
                                        break;
                                    case "EM 4100":
                                        lf125Tag |= Lf125khz.TagType.EM_4100;
                                        break;
                                    case "PROX":
                                        lf125Tag |= Lf125khz.TagType.HID_PROX;
                                        break;
                                    case "HITAG 1":
                                        lf125Tag |= Lf125khz.TagType.HITAG_1;
                                        break;
                                    case "HITAG 2":
                                        lf125Tag |= Lf125khz.TagType.HITAG_2;
                                        break;
                                    case "KERI":
                                        lf125Tag |= Lf125khz.TagType.KERI;
                                        break;
                                    case "INDALA":
                                        lf125Tag |= Lf125khz.TagType.INDALA;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            UInt64 masterTag = 0;
                            masterTag |= (UInt64)iso14443aTag;
                            masterTag |= (UInt64)iso15693Tag;
                            masterTag |= (UInt64)lf125Tag;
                            if (masterTag != 0)
                            {
                                TagFilter tagFilter = new Select_TagType(masterTag);
                                filterList.Add(tagFilter);
                            }

                        }
                    }
                    else
                    {
                        if (!(lbTagType.Items.Count == 0))
                        {
                            if (selectedTagForList.Count != 0)
                            {
                                switch (protocol)
                                {
                                    case TagProtocol.ISO14443A:
                                        {
                                            Iso14443a.TagType tag = new Iso14443a.TagType();
                                            TagFilter tagFilter = null;
                                            foreach (ColumnSelectionForTagButton item in selectedTagForList)
                                            {
                                                switch (item.ButtonNameTag)
                                                {
                                                    case "ISO14443A AUTO DETECT":
                                                        //tagFilter=new Select_TagType((UInt32))
                                                        tag = tag | Iso14443a.TagType.AUTO_DETECT;
                                                        break;
                                                    case "MIFARE PLUS":
                                                        tag |= Iso14443a.TagType.MIFARE_PLUS;
                                                        break;
                                                    case "MIFARE ULTRALIGHT":
                                                        tag |= Iso14443a.TagType.MIFARE_ULTRALIGHT;
                                                        break;
                                                    case "MIFARE CLASSIC":
                                                        tag |= Iso14443a.TagType.MIFARE_CLASSIC;
                                                        break;
                                                    case "NTAG":
                                                        tag |= Iso14443a.TagType.NTAG;
                                                        break;
                                                    case "MIFARE DESFIRE":
                                                        tag |= Iso14443a.TagType.MIFARE_DESFIRE;
                                                        break;
                                                    case "MIFARE MINI":
                                                        tag |= Iso14443a.TagType.MIFARE_MINI;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            if (tag != 0)
                                            {
                                                tagFilter = new Select_TagType((UInt64)tag);
                                                filterList.Add(tagFilter);
                                            }
                                        }
                                        break;
                                    case TagProtocol.ISO14443B:
                                        break;
                                    case TagProtocol.ISO15693:
                                        {
                                            Iso15693.TagType iso15693Tag = new Iso15693.TagType();
                                            TagFilter tagFilter = null;
                                            foreach (ColumnSelectionForTagButton item in selectedTagForList)
                                            {
                                                switch (item.ButtonNameTag)
                                                {
                                                    case "ISO15693 AUTO DETECT":
                                                        iso15693Tag = iso15693Tag | Iso15693.TagType.AUTO_DETECT;
                                                        break;
                                                    //case "EM4133":
                                                    //    tagFilter = Iso15693.TagType.EM4133);
                                                    //    break;
                                                    //case "EM4233":
                                                    //    tagFilter = Iso15693.TagType.EM4233);
                                                    //    break;
                                                    //case "EM4237":
                                                    //    tagFilter = Iso15693.TagType.EM4237);
                                                    //    break;
                                                    case "ICODE DNA":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_DNA;
                                                        break;
                                                    case "ICODE SLIX":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX;
                                                        break;
                                                    case "ICODE SLIX2":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_2;
                                                        break;
                                                    case "ICODE SLIX L":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_L;
                                                        break;
                                                    case "ICODE SLIX S":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLIX_S;
                                                        break;
                                                    case "iCLASS SE":
                                                        iso15693Tag |= Iso15693.TagType.HID_ICLASS_SE;
                                                        break;
                                                    case "VIGO":
                                                        iso15693Tag |= Iso15693.TagType.VIGO;
                                                        break;
                                                    case "TAGIT":
                                                        iso15693Tag |= Iso15693.TagType.TAGIT;
                                                        break;
                                                    case "ICODE SLI":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI;
                                                        break;
                                                    case "ICODE SLI L":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI_L;
                                                        break;
                                                    case "ICODE SLI S":
                                                        iso15693Tag |= Iso15693.TagType.ICODE_SLI_S;
                                                        break;
                                                    case "PICOPASS":
                                                        iso15693Tag |= Iso15693.TagType.PICOPASS;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            if (iso15693Tag != 0)
                                            {
                                                tagFilter = new Select_TagType((UInt64)iso15693Tag);
                                                filterList.Add(tagFilter);
                                            }
                                        }

                                        break;
                                    case TagProtocol.ISO180003M3:
                                        break;
                                    case TagProtocol.ISO180006B:
                                        break;
                                    case TagProtocol.ISO180006B_UCODE:
                                        break;
                                    case TagProtocol.ISO18092:
                                        break;
                                    case TagProtocol.LF125KHZ:
                                        Lf125khz.TagType lf125Tag = new Lf125khz.TagType();
                                        {
                                            TagFilter tagFilter = null;
                                            foreach (ColumnSelectionForTagButton item in selectedTagForList)
                                            {
                                                switch (item.ButtonNameTag)
                                                {
                                                    case "LF125KHZ AUTO DETECT":
                                                        lf125Tag = lf125Tag | Lf125khz.TagType.AUTO_DETECT;
                                                        break;
                                                    case "AWID":
                                                        lf125Tag |= Lf125khz.TagType.AWID;
                                                        break;
                                                    case "EM 4100":
                                                        lf125Tag |= Lf125khz.TagType.EM_4100;
                                                        break;
                                                    case "PROX":
                                                        lf125Tag |= Lf125khz.TagType.HID_PROX;
                                                        break;
                                                    case "HITAG 1":
                                                        lf125Tag |= Lf125khz.TagType.HITAG_1;
                                                        break;
                                                    case "HITAG 2":
                                                        lf125Tag |= Lf125khz.TagType.HITAG_2;
                                                        break;
                                                    case "KERI":
                                                        lf125Tag |= Lf125khz.TagType.KERI;
                                                        break;
                                                    case "INDALA":
                                                        lf125Tag |= Lf125khz.TagType.INDALA;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            if (lf125Tag != 0)
                                            {
                                                tagFilter = new Select_TagType((UInt64)lf125Tag);
                                                filterList.Add(tagFilter);
                                            }
                                        }
                                        break;
                                    case TagProtocol.LF134KHZ:
                                        Lf134khz.TagType lf134Tag = new Lf134khz.TagType();
                                        {
                                            TagFilter tagFilter = null;
                                            foreach (ColumnSelectionForTagButton item in selectedTagForList)
                                            {
                                                switch (item.ButtonNameTag)
                                                {
                                                    case "LF134KHZ AUTO DETECT":
                                                        lf134Tag |= Lf134khz.TagType.AUTO_DETECT;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                            if (lf134Tag != 0)
                                            {
                                                tagFilter = new Select_TagType((UInt64)lf134Tag);
                                                filterList.Add(tagFilter);
                                            }
                                        }
                                        //tagType = new Lf134khz.SetTagType(lf134Tag);
                                        break;
                                    case TagProtocol.NONE:
                                        break;
                                    //default:
                                    //    break;
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            if (model.Equals("M3e"))
            {
                if ((bool)chkBlockRead.IsChecked || (bool)chkSecureRead.IsChecked)
                {
                    tg |= SerialReader.TagMetadataFlag.DATA;
                }
                tg |= SerialReader.TagMetadataFlag.PROTOCOL | SerialReader.TagMetadataFlag.READCOUNT | SerialReader.TagMetadataFlag.TIMESTAMP | SerialReader.TagMetadataFlag.TAGTYPE;
            }
            else
            {
                tg = SerialReader.TagMetadataFlag.PROTOCOL | SerialReader.TagMetadataFlag.TIMESTAMP | SerialReader.TagMetadataFlag.RSSI | SerialReader.TagMetadataFlag.READCOUNT | SerialReader.TagMetadataFlag.ANTENNAID;
            }
            //objReader.ParamSet("/reader/metadata", tg);
            ColumnSelectionForTagResult rda;//= (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(5);
            for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
            {
                rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);
                //re order column width based on column selection for readability
                if (rda.IsColumnChecked)
                {
                    switch (rda.SelectedColumn)
                    {
                        case "Antenna":
                            tg = tg | SerialReader.TagMetadataFlag.ANTENNAID;
                            break;
                        case "Frequency":
                            tg = tg | SerialReader.TagMetadataFlag.FREQUENCY;
                            break;
                        case "Phase":
                            tg = tg | SerialReader.TagMetadataFlag.PHASE;
                            break;
                        case "Protocol":
                            tg = tg | SerialReader.TagMetadataFlag.PROTOCOL;
                            break;
                        case "GPIO":
                            tg = tg | SerialReader.TagMetadataFlag.GPIO;
                            break;
                        //case "BrandID":
                        //    tg = tg | SerialReader.TagMetadataFlag.BRAND_IDENTIFIER;
                        //    break;
                        default:
                            tg = SerialReader.TagMetadataFlag.PROTOCOL | SerialReader.TagMetadataFlag.TIMESTAMP | SerialReader.TagMetadataFlag.RSSI | SerialReader.TagMetadataFlag.READCOUNT | SerialReader.TagMetadataFlag.ANTENNAID;
                            break;
                    }
                }
            }
            if ((bool)chkEmbeddedReadData.IsChecked)
            {
                tg = tg | SerialReader.TagMetadataFlag.DATA;
            }
            if ((bool)cbxAntennamux.IsChecked)
            {
                if (!(model.Equals("M3e")))
                    tg = tg | SerialReader.TagMetadataFlag.GPIO;
            }
            //if (objReader is SerialReader)
            {
                //SerialReader.TagMetadataFlag flagSet = SerialReader.TagMetadataFlag.ALL | SerialReader.TagMetadataFlag.BRAND_IDENTIFIER;
                if (!(model.Equals("Mercury6")))
                {
                    objReader.ParamSet("/reader/metadata", tg);
                }
            }
            List<int> ant = GetSelectedAntennaList();

            if (model.Equals("M3e"))
            {
                //Disabling Dynamic protocol switching when equal protocol switching is enabled.
                if ((bool)rdBtnEqualprotocolSwitching.IsChecked)
                {
                    SerialReader sr = objReader as SerialReader;
                    sr.IsProtocolDynamicSwitching = false;
                }

                if ((bool)rdBtnStopNRead.IsChecked)
                {
                    StopTriggerReadPlan srp = null;
                    sotc.N = Convert.ToUInt32(txtStopNReadTagCount.Text);

                    srp = new StopTriggerReadPlan(sotc, ant, protocol, searchSelect, embTagOp, 1000);
                    simpleReadPlans.Add(srp);
                }
                else
                {
                    SimpleReadPlan srp = null;

                    if (filterList.Count >= 1)
                    {
                        MultiFilter multiFilter = new MultiFilter(filterList.ToArray());
                        srp = new SimpleReadPlan(ant, protocol, multiFilter, embTagOp, 1000);
                    }
                    else if (selectedTagForList.Count == 0)
                    {
                        srp = new SimpleReadPlan(ant, protocol, searchSelect, embTagOp, 1000);
                    }
                    else
                    {
                        srp = new SimpleReadPlan(ant, protocol, searchSelect, embTagOp, 1000);
                    }

                    if ((bool)rbtnGPITrigger.IsChecked)
                    {
                        simpleReadPlans.Add(ReadOnGPI(srp));
                    }
                    else
                    {
                        simpleReadPlans.Add(srp);
                    }
                }
            }
            else
            {
                if (!(bool)rdBtnStopNRead.IsChecked)
                {
                    SimpleReadPlan srp = null;
                    if ((bool)rdBtnEqualSwitching.IsChecked)
                    {

                        foreach (int a in ant)
                        {
                            srp = new SimpleReadPlan(new int[] { a }, protocol, searchSelect, embTagOp, isFastSearchEnabled, 100);
                            AddToReadPlanList(srp, simpleReadPlans);
                        }
                    }
                    else
                    {
                        srp = new SimpleReadPlan(ant.ToArray(), protocol, searchSelect, embTagOp, isFastSearchEnabled);
                        AddToReadPlanList(srp, simpleReadPlans);
                    }
                }
                else
                {
                    StopTriggerReadPlan srp = null;
                    sotc.N = Convert.ToUInt32(txtStopNReadTagCount.Text);
                    if ((bool)rdBtnEqualSwitching.IsChecked)
                    {
                        foreach (int a in ant)
                        {
                            srp = new StopTriggerReadPlan(sotc, new int[] { a }, protocol, searchSelect, 1000);
                            simpleReadPlans.Add(srp);
                        }
                    }
                    else
                    {
                        srp = new StopTriggerReadPlan(sotc, ant.ToArray(), protocol, searchSelect, 1000);
                        simpleReadPlans.Add(srp);
                    }
                }
            }
        }

        private void AddToReadPlanList(SimpleReadPlan srp, List<ReadPlan> simpleReadPlans)
        {
            if ((bool)rbtnGPITrigger.IsChecked)
            {
                simpleReadPlans.Add(ReadOnGPI(srp));
            }
            else
            {
                simpleReadPlans.Add(srp);
            }
        }
        private SimpleReadPlan ReadOnGPI(SimpleReadPlan srp)
        {
            try
            {
                GpiPinTrigger gpiTrigger = null;
                if (cbxGPIPort.SelectedItem.ToString() != "Select")
                {
                    int trigTypeNum = Convert.ToInt32(((ComboBoxItem)(cbxGPIPort.SelectedItem)).Content.ToString());
                    gpiTrigger = new GpiPinTrigger();
                    //Gpi trigger option not there for M6e Micro USB
                    if (!("M6e Micro USB".Equals(model)))
                    {
                        gpiTrigger.enable = true;
                        //set the gpi pin to read on
                        objReader.ParamSet("/reader/read/trigger/gpi", new int[] { trigTypeNum });
                        srp.ReadTrigger = gpiTrigger;
                    }
                }
                return srp;
            }
            catch (Exception)
            {
                throw new Exception("Invalid GPI Selected");
            }
        }

        /// <summary>
        /// Set read settings received from the connected read
        /// </summary>
        private void SetOptimalReaderSettings()
        {
            if (model.Equals("M3e"))
            {
                return;
            }
            try
            {
                //To read maximum tags
                if (!model.Equals("Astra"))
                {
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/tagEncoding"))
                    {
                        switch (OptimalReaderSettings["/reader/gen2/tagEncoding"])
                        {
                            case "FM0": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.FM0); break;
                            case "M2": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M2); break;
                            case "M4": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M4); break;
                            case "M8": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M8); break;
                            default: break;
                        }
                    }
                    if (OptimalReaderSettings["/reader/gen2/q"] == "DynamicQ")
                    {
                        objReader.ParamSet("/reader/gen2/q", new Gen2.DynamicQ());
                    }
                    else
                    {
                        byte qValue = Convert.ToByte(
                            OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                        objReader.ParamSet("/reader/gen2/q", new Gen2.StaticQ(qValue));
                    }
                    if (!(model.Equals("Mercury6")))
                    {
                        List<string> result = OptimalReaderSettings["/reader/gen2/initQ"].Split(',').ToList();
                        Gen2.InitQ iq = new Gen2.InitQ();
                        if (result[0].Contains("True"))
                        {
                            iq.qEnable = true;
                            iq.initialQ = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                            objReader.ParamSet("/reader/gen2/initQ", iq);
                        }
                        else
                        {
                            iq.qEnable = false;
                            iq.initialQ = 2;// Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                            objReader.ParamSet("/reader/gen2/initQ", iq);
                        }
                    }
                }
                {
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/BLF"))
                    {
                        if (OptimalReaderSettings["/reader/gen2/BLF"].Equals("LINK640KHZ"))
                        {
                            objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK640KHZ);
                        }
                        else if (OptimalReaderSettings["/reader/gen2/BLF"].Equals("LINK320KHZ"))
                        {
                            objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK320KHZ);
                        }
                        else
                        {
                            objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
                        }
                    }
                    //Set tari
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/tari"))
                    { 
                        switch (OptimalReaderSettings["/reader/gen2/tari"])
                        {
                            case "TARI_25US": objReader.ParamSet("/reader/gen2/tari",
                                Gen2.Tari.TARI_25US); break;
                            case "TARI_12_5US": objReader.ParamSet("/reader/gen2/tari",
                                Gen2.Tari.TARI_12_5US); break;
                            case "TARI_6_25US": objReader.ParamSet("/reader/gen2/tari",
                                Gen2.Tari.TARI_6_25US); break;
                            default: break;
                        }
                    }
                }
                switch (OptimalReaderSettings["/reader/gen2/session"])
                {
                    case "S0": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S0); break;
                    case "S1": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S1); break;
                    case "S2": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S2); break;
                    case "S3": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S3); break;
                    default: break;
                }
                switch (OptimalReaderSettings["/reader/gen2/target"])
                {
                    case "A": objReader.ParamSet("/reader/gen2/target", Gen2.Target.A); break;
                    case "AB": objReader.ParamSet("/reader/gen2/target", Gen2.Target.AB); break;
                    case "B": objReader.ParamSet("/reader/gen2/target", Gen2.Target.B); break;
                    case "BA": objReader.ParamSet("/reader/gen2/target", Gen2.Target.BA); break;
                    default: break;
                }
            }
            catch (Exception ex)
            {
                if ((-1 != ex.Message.IndexOf("Specified RFMode is not supported"))
                    || (ex is FAULT_MSG_INVALID_PARAMETER_VALUE_Exception))
                {
                    {
                        if (OptimalReaderSettings.ContainsKey("/reader/gen2/BLF"))
                        {
                            if (OptimalReaderSettings["/reader/gen2/BLF"].Equals("LINK640KHZ"))
                            {
                                objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK640KHZ);
                            }
                            else
                            {
                                objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
                            }
                        }
                        //Set tari
                        if (OptimalReaderSettings.ContainsKey("/reader/gen2/tari"))
                        { 
                            switch (OptimalReaderSettings["/reader/gen2/tari"])
                            {
                                case "TARI_25US": objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_25US); break;
                                case "TARI_12_5US": objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_12_5US); break;
                                case "TARI_6_25US": objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_6_25US); break;
                                default: break;
                            }
                        }
                    }
                    if (!isOptimalReaderSettingsFailed)
                    {
                        isOptimalReaderSettingsFailed = true;
                        SetOptimalReaderSettings();
                    }
                    else
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                isOptimalReaderSettingsFailed = false;
            }
        }

        /// <summary>
        /// Initiates a Read Tag ID Single for the timeout given in the timeout Control Box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void readTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear warning on status bar if any
                ClearMessageOnStatusBar();

                // Set selected font size to tag-results
                SetFontSize();

                // Tag aging
                TagResults.enableTagAgingOnRead = true;
                TagResults.tagagingColourCache.Clear();

                // Set read plans 
                SetReadPlans();

                // Register exception handler
                objReader.ReadException += ReadException;


                if (!(model.Equals("M3e")))
                {
                    // Load gen2 settings
                    LoadGen2Settings();
                    // Set optimal reader settings
                    SetOptimalReaderSettings();
                }

                // Disable read options when read is in progress
                EnableDisableSyncReadOptions(false);
                TagResultsViewModel trViewModel = new TagResultsViewModel();
                if (Convert.ToInt32(txtbxreadOnceTimeout.Text) < 0)
                {
                    MessageBox.Show("Timeout value should be greater then 0", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string timeoutRdOnce = txtbxreadOnceTimeout.Text;
                Thread readSyncOnce = new Thread(new ThreadStart(delegate()
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                isSyncReadGoingOn = true;
                                lblshowStatus.Content = "Reading";
                                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                                    UriKind.RelativeOrAbsolute));
                                if (clientConnected || isHttpPostServiceEnabled)
                                {
                                    broadcastON();
                                }
                            }));
                            if (timeoutRdOnce != "")
                            {
                                trViewModel.ReadTags(ref tagdb, objReader, int.Parse(timeoutRdOnce), out trd);
                            }
                            else
                            {
                                timeoutRdOnce = "500";
                                trViewModel.ReadTags(ref tagdb, objReader, int.Parse(timeoutRdOnce), out trd);
                            }
                            if (clientConnected)
                            {
                                foreach (Socket tempSocket in tagStreamSock)
                                    if (tempSocket.Connected)
                                        sendTagsToTcp(trd, tempSocket);
                            }
                            if (isHttpPostServiceEnabled)
                            {
                                sendTagsToWeb(tagdb);
                            }
                            Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    tagdb.Repaint();
                                    isSyncReadGoingOn = false;
                                    btnRead.IsEnabled = true;
                                    readonceElapsedTime += Convert.ToDouble(txtbxreadOnceTimeout.Text);
                                    totalTimeElapsedTextBox.Content = Math.Round(((double)(readonceElapsedTime / 1000)),
                                        2).ToString();

                                    totalUniqueTagsReadTextBox.Content = tagdb.UniqueTagCount.ToString();

                                    txtTotalTagReads.Content = tagdb.TotalTagCount.ToString();
                                    txtbxReadRatePerSec.Content = (Math.Round(
                                        ((Convert.ToDouble(tagdb.TotalTagCount.ToString())) / ((readonceElapsedTime) / 1000)),
                                        2)).ToString();

                                    //Show the status
                                    lblshowStatus.Content = "Connected";
                                    imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                                        UriKind.RelativeOrAbsolute));
                                    broadcastOFF();

                                    TagResults.enableTagAgingOnRead = false;
                                    // Causes a control bound to the BindingSource to reread all the items 
                                    // in the list and refresh their displayed values.
                                    tagdb.Repaint();
                                    objReader.ReadException -= ReadException;
                                    CacheReadDataSettings();
                                    // Enable read options when read is in progress
                                    EnableDisableSyncReadOptions(true);
                                    lblTemperature.Visibility = lblReaderTemperature.Visibility = Visibility.Visible;
                                    if (model.Equals("M3e"))
                                    {
                                        Reader.Stat.Values objRdrStats = (Reader.Stat.Values)objReader.ParamGet("/reader/stats");
                                        lblReaderTemperature.Content = objRdrStats.TEMPERATURE.ToString() + "°C";
                                    }
                                    else
                                    {
                                        lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                                    }
                                }));
                        }
                        catch (Exception ex)
                        {
                            btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                btnRead.IsEnabled = true;
                                isSyncReadGoingOn = false;
                                TagResults.enableTagAgingOnRead = false;
                                //Show the status
                                lblshowStatus.Content = "Connected";
                                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                                    UriKind.RelativeOrAbsolute));

                                // Enable read options when read is in progress
                                EnableDisableSyncReadOptions(true);
                            }));
                            if (ex is ReaderCodeException)
                            {
                                if ((((ReaderCodeException)ex).Code == 0x504)
                                    || (((ReaderCodeException)ex).Code == 0x505))
                                {
                                    switch (((ReaderCodeException)ex).Code)
                                    {
                                        case 0x504:
                                            warningText = "Over Temperature";
                                            break;
                                        case 0x505:
                                            warningText = "High Return Loss";
                                            break;
                                        default:
                                            warningText = "warning";
                                            break;
                                    }
                                    myTimer.Elapsed += new ElapsedEventHandler(TimeEvent);
                                    myTimer.Interval = 2000;
                                    myTimer.Start();
                                    lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                    {
                                        GUIshowWarning();
                                    }));
                                }
                                else
                                {
                                    MessageBox.Show(ex.Message, "Reader Message",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else if (ex is ReaderCommException)
                            {
                                MessageBox.Show(ex.Message, "Reader Message",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                // Disconnect the reader when exception is received
                                DisconnectReader();
                                return;
                            }
                            else if (ex is ReaderException)
                            {
                                if (-1 != ex.Message.IndexOf("Specified port does not exist"))
                                {
                                    Onlog(ex);
                                    MessageBox.Show(ex.Message,
                                                     "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    // Disconnect the reader when exception is received
                                    DisconnectReader();
                                    return;
                                }
                                else
                                {
                                    DisplayMessageOnStatusBar(ex.Message, Brushes.Red);
                                    return;
                                }
                            }
                            else if (ex is IOException)
                            {
                                MessageBox.Show("The I/O operation has been aborted, Disconnecting from reader",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                btnConnect.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    btnConnect.Content = "Disconnect";
                                    btnConnect_Click(sender, new RoutedEventArgs());
                                }));
                            }
                            else
                            {
                                MessageBox.Show(ex.Message, "Reader Message",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                    }));
                readSyncOnce.Start();
                btnRead.IsEnabled = false;
            }
            catch (Exception ex)
            {
                // Enable read options when read is in progress
                EnableDisableSyncReadOptions(true);

                Onlog(ex);
                btnRead.IsEnabled = true;
                isSyncReadGoingOn = false;
                TagResults.enableTagAgingOnRead = false;
                //Show the status
                lblshowStatus.Content = "Connected";
                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png", UriKind.RelativeOrAbsolute));
                MessageBox.Show(ex.Message, "Reader Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enable or disable options when sync read is in progress
        /// </summary>
        /// <param name="option"></param>
        private void EnableDisableSyncReadOptions(bool option)
        {
            // Load config btn
            btnLoadConfig.IsEnabled = option;
            // Read continuous btn
            rdBtnReadContinuously.IsEnabled = option;
            // Baud rate combo-box
            cbxBaudRate.IsEnabled = option;
            // Fast search
            chkEnableFastSearch.IsEnabled = option;
            //if (model.Equals("M3e"))
            //{
            //    //chkEnableFastSearch.IsEnabled = false;
            //}
            //else
            //{
            //    chkEnableFastSearch.IsEnabled = option;
            //}
            // Region combo-box
            regioncombo.IsEnabled = option;
            // Select all check box in tag-results
            Dispatcher.Invoke(new del(delegate()
            {
                CheckBox headerCheckBox = (CheckBox)GetChildControl(TagResults.dgTagResults, "headerCheckBox");
                headerCheckBox.IsEnabled = option;
            }));
            // Connect btn in expander
            btnConnect.IsEnabled = option;
            // Comport combo-box
            cmbReaderAddr.IsEnabled = option;
            // IP address combo-box
            cmbFixedReaderAddr.IsEnabled = option;
            // Custom Trasport Text box
            txtCustomTransport.IsEnabled = option;
            // Font size
            txtfontSize.IsEnabled = option;
            // Rf off time
            txtRFOffTimeout.IsEnabled = option;
            // Rf on time
            txtRFOnTimeout.IsEnabled = option;
            // Disable all the buttons in settings, when async read is going on
            grpbxPerformanceTuning.IsEnabled = option;
            grpbxRdrPwrSettngs.IsEnabled = option;

            // Disable load config button when async read
            btnLoadConfig.IsEnabled = option;
            // Protocols
            ProtocolsGroupBox.IsEnabled = option;
            // Tag Type
            M3eFilterGroupBox.IsEnabled = option;
            // Antennas
            AntennasGroupBox.IsEnabled = option;
            // GPIO
            grpGPIOBehaviour.IsEnabled = option;
            gridInputOutput.IsEnabled = option;
            // Embedded read data
            ReadDataGroupBox.IsEnabled = option;
            // Filter
            FilterGroupBox.IsEnabled = option;
            // Disable firmware Update when async read
            stackPanelFirmwareUpdate.IsEnabled = option;
            // Read once timeout
            txtbxreadOnceTimeout.IsEnabled = option;
            // tag aging
            chkEnableTagAging.IsEnabled = option;
            // Refresh rate
            txtRefreshRate.IsEnabled = option;
            // WriteEPC tab
            tiWriteEPC.IsEnabled = option;
            // TagInspector tab
            tiTagInspector.IsEnabled = option;
            // UserMemory tab
            tiUserMemory.IsEnabled = option;
            // Lock tab
            tiLockTag.IsEnabled = option;
            // Disable all the controls inside data extensions
            grpbxDataExtensions.IsEnabled = option;
            // Disable Protocol switching
            stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = option;

            //chkLBTEnable.IsEnabled = option;
            //chkEnableDwellTime.IsEnabled = option;
            //txtEnableLBTThreshold.IsEnabled = option;
            //txtDwelltime.IsEnabled = option;
            //btnRegionConfig.IsEnabled = option;
        }

        /// <summary>
        /// Get selected antenna list
        /// </summary>
        /// <returns></returns>
        private List<int> GetSelectedAntennaList()
        {
            CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox, Ant5CheckBox, Ant6CheckBox, Ant7CheckBox, Ant8CheckBox,
                                            Ant9CheckBox, Ant10CheckBox, Ant11CheckBox, Ant12CheckBox, Ant13CheckBox, Ant14CheckBox, Ant15CheckBox, Ant16CheckBox,
                                            Ant17CheckBox, Ant18CheckBox, Ant19CheckBox, Ant20CheckBox, Ant21CheckBox, Ant22CheckBox, Ant23CheckBox, Ant24CheckBox,
                                            Ant25CheckBox, Ant26CheckBox, Ant27CheckBox, Ant28CheckBox, Ant29CheckBox, Ant30CheckBox, Ant31CheckBox, Ant32CheckBox,
                                            Ant33CheckBox, Ant34CheckBox, Ant35CheckBox, Ant36CheckBox, Ant37CheckBox, Ant38CheckBox, Ant39CheckBox, Ant40CheckBox,
                                            Ant41CheckBox, Ant42CheckBox, Ant43CheckBox, Ant44CheckBox, Ant45CheckBox, Ant46CheckBox, Ant47CheckBox, Ant48CheckBox,
                                            Ant49CheckBox, Ant50CheckBox, Ant51CheckBox, Ant52CheckBox, Ant53CheckBox, Ant54CheckBox, Ant55CheckBox, Ant56CheckBox,
                                            Ant57CheckBox, Ant58CheckBox, Ant59CheckBox, Ant60CheckBox, Ant61CheckBox, Ant62CheckBox, Ant63CheckBox, Ant64CheckBox};
            List<int> ant = new List<int>();
            bool flagAntBoxVisibility;
            for (int antIdx = 0; antIdx < antennaBoxes.Length; antIdx++)
            {
                CheckBox antBox = antennaBoxes[antIdx];
                bool check = (bool)antBox.IsEnabled;
                Visibility antBoxVisibility = antBox.Visibility;
                if (antBoxVisibility.Equals(Visibility.Visible))
                {
                    flagAntBoxVisibility = true;
                }
                else
                {
                    flagAntBoxVisibility = false;
                }
                if (flagAntBoxVisibility && (bool)antBox.IsEnabled && (bool)antBox.IsChecked)
                {
                    int antNum = antIdx + 1;
                    ant.Add(antNum);
                }
                else if (flagAntBoxVisibility && restoreReadSetting && (bool)antBox.IsChecked)
                {
                    int antNum = antIdx + 1;
                    ant.Add(antNum);
                }
            }
            return ant;
        }

        /// <summary>
        /// Configure protocols 
        /// </summary>
        /// <param name="supportedProtocols"></param>
        void ConfigureProtocols(TagProtocol[] supportedProtocols)
        {
            CheckBox[] protocolBoxes = { gen2CheckBox, iso6bCheckBox, isoUcodeCheckbox, ipx64CheckBox, ipx256CheckBox, ataCheckBox, iso14443aCheckBox, iso14443bCheckBox, iso15693CheckBox, iso18092CheckBox, felicaCheckBox, iso180003mode3CheckBox, lf125KHZCheckBox, lf134KHZCheckBox };
            foreach (CheckBox cb in protocolBoxes)
            {
                cb.IsChecked = false;
                cb.Visibility = Visibility.Collapsed;
            }
            if (null != supportedProtocols)
            {
                foreach (TagProtocol proto in supportedProtocols)
                {
                    switch (proto)
                    {
                        case TagProtocol.GEN2:
                            gen2CheckBox.Visibility = Visibility.Visible;
                            if (!(restoreReadSetting))
                            {
                                gen2CheckBox.IsChecked = true;   
                            }
                            break;
                        case TagProtocol.ISO180006B:
                            iso6bCheckBox.Visibility = Visibility.Visible;
                            break;
                        case TagProtocol.IPX64:
                            ipx64CheckBox.Visibility = Visibility.Visible;
                            break;
                        case TagProtocol.IPX256:
                            ipx256CheckBox.Visibility = Visibility.Visible;
                            break;
                        case TagProtocol.ATA:
                            ataCheckBox.Visibility = Visibility.Visible;
                            break;
                        case TagProtocol.ISO14443A:
                            iso14443aCheckBox.Visibility = Visibility.Visible;
                            iso14443aCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO14443B:
                            iso14443bCheckBox.Visibility = Visibility.Visible;
                            iso14443bCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO15693:
                            iso15693CheckBox.Visibility = Visibility.Visible;
                            iso15693CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO18092:
                            iso18092CheckBox.Visibility = Visibility.Visible;
                            iso18092CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.FELICA:
                            felicaCheckBox.Visibility = Visibility.Visible;
                            felicaCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.ISO180003M3:
                            iso180003mode3CheckBox.Visibility = Visibility.Visible;
                            iso180003mode3CheckBox.IsChecked = true;
                            break;
                        case TagProtocol.LF125KHZ:
                            lf125KHZCheckBox.Visibility = Visibility.Visible;
                            lf125KHZCheckBox.IsChecked = true;
                            break;
                        case TagProtocol.LF134KHZ:
                            lf134KHZCheckBox.Visibility = Visibility.Visible;
                            lf134KHZCheckBox.IsChecked = true;
                            break;

                        // Uncomment code to enable ISO180006B_UCODE Protocol Implementation.
                        //case TagProtocol.ISO180006B_UCODE :
                        //    isoUcodeCheckbox.Visibility = Visibility.Visible;
                        //    break;
                    }
                }
                //At the start of application Based on the protocol being picked, 
                //change the label on select screen accordingly.
                //For example if Gen2, then change select screen to word address. on
                //the other hand if it is ISO18-6b, then change it to byte address.
                OnProtocolSelect();
            }
        }

        /// <summary>
        /// Configure antennas
        /// </summary>
        /// <param name="objReader"></param>
        public void ConfigureAntennaBoxes(Reader objReader)
        {
            // Cast int[] return values to IList<int> instead of int[] to get Contains method
            IList<int> existingAntennas = null;
            IList<int> detectedAntennas = null;
            IList<int> validAntennas = null;
            bool checkPort = false;
            if (null == objReader)
            {
                int[] empty = new int[0];
                existingAntennas = detectedAntennas = validAntennas = empty;
            }
            else
            {
                switch (model)
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
                            if (-1 != ex.Message.IndexOf("Parameter not found:"))
                            {
                                // Parameter not found error means antenna detection is not supported on the module.
                                checkPort = false;
                                chkbxAntennaDetection.IsEnabled = false;
                                chkbxAntennaDetection.IsChecked = false;
                            }
                        }
                        break;
                }
                existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");
                detectedAntennas = model.Equals("M3e") ? existingAntennas : (IList<int>)objReader.ParamGet("/reader/antenna/connectedPortList");
                validAntennas = checkPort ? detectedAntennas : existingAntennas;
            }
            chkbxAntennaDetection.IsChecked = checkPort;
            CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox };
            TextBox[] readPowerTextBoxes = { txtReadPowerAnt1, txtReadPowerAnt2, txtReadPowerAnt3, txtReadPowerAnt4 };
            TextBox[] writePowerTextBoxes = { txtWritePowerAnt1, txtWritePowerAnt2, txtWritePowerAnt3, txtWritePowerAnt4 };
            Label[] powerLabel = { lblPowerAnt1, lblPowerAnt2, lblPowerAnt3, lblPowerAnt4 };
            int antNum = 1;
            foreach (CheckBox cb in antennaBoxes)
            {
                if (existingAntennas.Contains(antNum))
                {
                    cb.Visibility = Visibility.Visible;
                    readPowerTextBoxes[antNum - 1].Visibility = writePowerTextBoxes[antNum - 1].Visibility = powerLabel[antNum - 1].Visibility = Visibility.Visible;
                }
                else
                {
                    cb.Visibility = Visibility.Collapsed;
                    readPowerTextBoxes[antNum - 1].Visibility = writePowerTextBoxes[antNum - 1].Visibility = powerLabel[antNum - 1].Visibility = Visibility.Collapsed;
                }
                if (validAntennas.Contains(antNum))
                {
                    cb.IsEnabled = true;
                    readPowerTextBoxes[antNum - 1].IsEnabled = writePowerTextBoxes[antNum - 1].IsEnabled = powerLabel[antNum - 1].IsEnabled = true;
                }
                else
                {
                    cb.IsEnabled = false;
                    readPowerTextBoxes[antNum - 1].IsEnabled = writePowerTextBoxes[antNum - 1].IsEnabled = powerLabel[antNum - 1].IsEnabled = false;
                }
                if (detectedAntennas.Contains(antNum))
                {
                    cb.IsChecked = true;
                }
                else
                {
                    cb.IsChecked = false;
                }
                antNum++;
            }
        }

        /// <summary>
        /// Configure Logical antennas
        /// </summary>
        /// <param name="objReader"></param>
        public void ConfigureLogicalAntennaBoxes(Reader objReader)
        {
            // Cast int[] return values to IList<int> instead of int[] to get Contains method
            IList<int> existingAntennas = null;
            IList<int> detectedAntennas = null;
            IList<int> validAntennas = null;
            bool checkPort = false;
            if (null == objReader)
            {
                int[] empty = new int[0];
                existingAntennas = detectedAntennas = validAntennas = empty;
            }
            else
            {

                switch (model)
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
                            if (-1 != ex.Message.IndexOf("Parameter not found:"))
                            {
                                // Parameter not found error means antenna detection is not supported on the module.
                                checkPort = false;
                                chkbxAntennaDetection.IsEnabled = false;
                                chkbxAntennaDetection.IsChecked = false;
                            }
                        }
                        break;
                }
                existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");
                detectedAntennas = (model.Equals("M3e")) ? existingAntennas : (IList<int>)objReader.ParamGet("/reader/antenna/connectedPortList");
                validAntennas = checkPort ? detectedAntennas : existingAntennas;
            }
            chkbxAntennaDetection.IsChecked = checkPort;
            CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox, Ant5CheckBox, Ant6CheckBox, Ant7CheckBox, Ant8CheckBox,
                                            Ant9CheckBox, Ant10CheckBox, Ant11CheckBox, Ant12CheckBox, Ant13CheckBox, Ant14CheckBox, Ant15CheckBox, Ant16CheckBox,
                                            Ant17CheckBox, Ant18CheckBox, Ant19CheckBox, Ant20CheckBox, Ant21CheckBox, Ant22CheckBox, Ant23CheckBox, Ant24CheckBox,
                                            Ant25CheckBox, Ant26CheckBox, Ant27CheckBox, Ant28CheckBox, Ant29CheckBox, Ant30CheckBox, Ant31CheckBox, Ant32CheckBox,
                                            Ant33CheckBox, Ant34CheckBox, Ant35CheckBox, Ant36CheckBox, Ant37CheckBox, Ant38CheckBox, Ant39CheckBox, Ant40CheckBox,
                                            Ant41CheckBox, Ant42CheckBox, Ant43CheckBox, Ant44CheckBox, Ant45CheckBox, Ant46CheckBox, Ant47CheckBox, Ant48CheckBox,
                                            Ant49CheckBox, Ant50CheckBox, Ant51CheckBox, Ant52CheckBox, Ant53CheckBox, Ant54CheckBox, Ant55CheckBox, Ant56CheckBox,
                                            Ant57CheckBox, Ant58CheckBox, Ant59CheckBox, Ant60CheckBox, Ant61CheckBox, Ant62CheckBox, Ant63CheckBox, Ant64CheckBox};
            int antNum = 1;
            foreach (CheckBox cb in antennaBoxes)
            {
                if (existingAntennas.Contains(antNum))
                {
                    cb.Visibility = Visibility.Visible;
                }
                else
                {
                    cb.Visibility = Visibility.Collapsed;
                }
                if (validAntennas.Contains(antNum))
                {
                    cb.IsEnabled = true;
                }
                else
                {
                    cb.IsEnabled = false;
                }
                if (detectedAntennas.Contains(antNum))
                {
                    cb.IsChecked = true;
                }
                else
                {
                    cb.IsChecked = false;
                }
                antNum++;
            }
        }

        /// <summary>
        /// Updates module temperature asynchronously
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PrintTemperature(object sender, StatsReportEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                lblReaderTemperature.Content = e.StatsReport.STATS.TEMPERATURE.ToString() + "°C";
            }
          ));
        }

        /// <summary>
        /// Function that processes the Tag Data produced by StartReading();
        /// </summary>
        /// <param name="read"></param>
        void PrintTagRead(Object sender, TagReadDataEventArgs e)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
           {
               if (clientConnected)
               {
                   foreach (Socket tempSocket in tagStreamSock)
                   {
                       if (tempSocket.Connected)
                           PrintTagReads(e, tempSocket);
                   }
               }
               else if (!isHttpPostServiceEnabled)
               {
                   broadcastOFF();
               }
           }));
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                // Enable the read/stop-reading button when URA is able to connect 
                // to the reader or URA is able to get the tags.
                btnRead.IsEnabled = true;
                tagdb.Add(e.TagReadData);
                //If warning is there, remove it
                if (null != lblWarning.Text)
                {
                    string temperature = lblReaderTemperature.Content.ToString().TrimEnd('C', '°');
                    if (lblWarning.Text.ToString() != "")
                    {
                        if (int.Parse(temperature) < 85)
                        {
                            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                GUIturnoffWarning();
                            }));
                        }
                    }
                }
            }));
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                txtTotalTagReads.Content = tagdb.TotalTagCount.ToString();
                totalUniqueTagsReadTextBox.Content = tagdb.UniqueTagCount.ToString();
            }
            ));
        }

        /// <summary>
        /// Initiate continuous read
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startRead_Click(object sender, RoutedEventArgs e)
        {
            OnStartRead("");
        }

        /// <summary>
        /// Set selected font size to TagResults
        /// </summary>
        private void SetFontSize()
        {
            if (txtfontSize.Text == "")
            {
                txtfontSize.Text = "14";
                TagResults.dgTagResults.FontSize = Convert.ToDouble(txtfontSize.Text);
            }
            else if ((Convert.ToInt32(txtfontSize.Text) > 0) && (Convert.ToInt32(txtfontSize.Text) <= 20))
            {
                TagResults.dgTagResults.FontSize = Convert.ToDouble(txtfontSize.Text);
            }
        }

        /// <summary>
        /// Perform async read
        /// </summary>
        /// <param name="readStatus">Purpose of read async. "" if new read & "Reconnecting" 
        /// in case the ura lost its connection </param>
        private void OnStartRead(string readStatus)
        {
            bool triedStart = false;  // Did we go into the "Start Reads" clause?

            try
            {
                // Set the selected font size for content on tag results tab
                SetFontSize();

                Mouse.SetCursor(Cursors.Wait);
                if (btnRead.Content.ToString() == "Read")
                {
                    btnRead.IsEnabled = false;
                    // Reset connection lost exception counter
                    connectionLostCount = 0;

                    // Variables used for synchronizations
                    isStopReadingBtnPressed = false;
                    isAsyncReadGoingOn = true;

                    // Display message on the status bar
                    DisplayMessageOnStatusBar("Applying initial settings", Brushes.LightBlue);

                    // Clear and set read plans based on the antenna configuration
                    simpleReadPlans.Clear();
                    if (restoreReadSetting)
                    {
                        ConfigureAntennaBoxes(objReader);
                        ConfigureLogicalAntennaBoxes(objReader);
                        StoreReaderSettings(false, new List<int> { }, new List<TagProtocol> { }, false);
                    }
                    SetReadPlans();

                    TagResults.enableTagAgingOnRead = true;
                    TagResults.tagagingColourCache.Clear();

                    triedStart = true;

                    // Disable all the enabled buttons which are not based on reader type
                    PerformAsyncReadInitialActions(false);
                    if (rdBtnReadContinuously.IsChecked == true)
                    {
                        gridPerformanceTuning.IsEnabled = grpbxRdrPwrSettngs.IsEnabled = false;
                        rdbtnperantenna.IsEnabled = rdbtnglobal.IsEnabled = false;
                        txtReadPowerAnt1.IsEnabled = txtReadPowerAnt2.IsEnabled = txtReadPowerAnt3.IsEnabled = txtReadPowerAnt4.IsEnabled = false;
                        txtWritePowerAnt1.IsEnabled = txtWritePowerAnt2.IsEnabled = txtWritePowerAnt3.IsEnabled = txtWritePowerAnt4.IsEnabled = false;
                        grpGPIOBehaviour.IsEnabled = false;
                    }
                    else
                        gridPerformanceTuning.IsEnabled = false;
                    ReadDataGroupBox.IsEnabled = false;

                    if ("0" == txtRFOffTimeout.Text)
                    {
                        btnConnect.IsEnabled = false;
                        cmbReaderAddr.IsEnabled = false;
                        cmbFixedReaderAddr.IsEnabled = false;
                        txtCustomTransport.IsEnabled = false;
                        txtfontSize.IsEnabled = false;
                        txtRFOffTimeout.IsEnabled = false;
                        txtRFOnTimeout.IsEnabled = false;
                        stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = false;
                    }

                    btnRead.ToolTip = "Stop Async Read";
                    btnRead.Content = "Stop Reading";

                    // Set RF off and on time 
                    objReader.ParamSet("/reader/read/asyncOnTime", int.Parse(txtRFOnTimeout.Text));
                    try
                    {
                        objReader.ParamSet("/reader/read/asyncOffTime", int.Parse(txtRFOffTimeout.Text));
                    }
                    catch (Exception ex)
                    {
                        if (-1 != ex.Message.IndexOf("M_UnsupportedParameter"))
                        {
                            MessageBox.Show("asyncOffTime unsupported parameter", "Info",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            Onlog("AsyncOffTime unsupported parameter");
                            Onlog(ex);
                        }
                    }

                    // To read maximum tags by default
                    LoadGen2Settings();

                    // Don't set the optimal reader settings when the ura is attempting
                    // to reconnect to the reader, when connection lost
                    if (readStatus.Length == 0)
                    {
                        SetOptimalReaderSettings();
                    }

                    // Register read exception and tag read listeners
                    objReader.ReadException += ReadException;
                    objReader.TagRead += PrintTagRead;

                    // Clear tag results
                    if (selectionOnEPC != null)
                    {
                        ClearReads();
                    }

                    // Cache current time
                    startAsyncReadTime = DateTime.Now;

                    // Display reading status
                    lblshowStatus.Content = "Reading";
                    imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                        UriKind.RelativeOrAbsolute));
                    if (clientConnected)
                        broadcastON();

                    // Start timer to render data on the grid and calculate read rate
                    dispatchtimer.Start();
                    readRatePerSec.Start();
                    if ((bool)rdBtnStopNRead.IsChecked)
                    {
                        isReadStopped.Start();
                    }
                    if (isHttpPostServiceEnabled)
                        HttpPostDispatchTimer.Start();

                    Mouse.SetCursor(Cursors.Arrow);
                    // Call start async read
                    objReader.StartReading();

                    // Clear previous messages on status bar
                    ClearMessageOnStatusBar();
                    btnRead.IsEnabled = true;
                    cbxcolumnSelection.IsEnabled = false;
                    restoreReadSetting = false;
                }

                    // Call when stop reading button is pressed
                else if (btnRead.Content.ToString() == "Stop Reading")
                {
                    btnRead.IsEnabled = false;
                    OnStopReadsClick();
                    btnRead.IsEnabled = true;
                    cbxcolumnSelection.IsEnabled = true;
                }
            }
            catch (Exception exp)
            {
                btnRead.IsEnabled = true;
                if (exp.Message.Contains("Please select at least one antenna") || exp.Message.Contains("Antenna is not connected to reader") || exp.Message.Contains("Please select at least one protocol") || exp.Message.Contains("Invalid UID length"))
                {
                    MessageBox.Show(exp.Message, "Universal Reader Assistant Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    isAsyncReadGoingOn = false;
                    lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                    {
                        GUIturnoffWarning();
                        expdrReadOptions.IsExpanded = true;
                        expdrReadOptions.Focus();
                        //Dock the settings/status panel if not docked, to display Firmware update options
                        if (pane1Button.Visibility == System.Windows.Visibility.Visible)
                        {
                            pane1Button_MouseEnter(null, null);
                            pane1Pin_Click(null, null);
                        }
                        if (expdrConnect.IsExpanded)
                        {
                            expdrConnect.IsExpanded = true;
                        }
                    }));
                    return;
                }
                if (exp is ReaderCodeException)
                {
                    switch (MessageBox.Show(exp.Message.ToString() + "\nStart/Stop Tag Reads Failed. Try Again",
                        "Error", MessageBoxButton.YesNo, MessageBoxImage.Error))
                    {
                        case MessageBoxResult.Yes:
                            {
                                OnStartRead("");
                                btnRead.ToolTip = "Start Async Read (EPC Only)";
                                btnRead.Content = "Read";
                                rdBtnReadOnce.IsEnabled = true;
                                // Enable fast search
                                //if (!(model.Equals("M3e")))
                                {
                                    chkEnableFastSearch.IsEnabled = true;
                                }
                                isAsyncReadGoingOn = false;
                                TagResults.enableTagAgingOnRead = false;
                                // Enable firmware Update when async read is OFF
                                stackPanelFirmwareUpdate.IsEnabled = true;
                                OnStartRead("");
                                break;
                            }
                        default:
                            break;
                    }

                }
                if (exp is ReaderCommException)
                {
                    handleReaderCommException((ReaderCommException)exp, new RoutedEventArgs());
                }
                else
                {
                    if (triedStart)
                    {
                        ShutdownStartReads(exp);
                    }
                }
            }
        }

        /// <summary>
        /// Remember stop reading button press state, when connection lost 
        /// is occurred while performing async read 
        /// </summary>
        bool isStopReadingBtnPressed = false;

        /// <summary>
        /// Stop continuous read
        /// </summary>
        private void OnStopReadsClick()
        {
            // Variables used for synchronizations
            isStopReadingBtnPressed = true;

            btnRead.InvalidateVisual();

            // Stop timer to render data on the grid
            dispatchtimer.Stop();
            readRatePerSec.Stop();
            isReadStopped.Stop();
            if (isHttpPostServiceEnabled)
                HttpPostDispatchTimer.Stop();

            if (!isReconnectFailed)
                continuousreadElapsedTime = CalculateElapsedTime();
            try
            {
                Mouse.SetCursor(Cursors.AppStarting);

                // Call stop reading to stop async read
                if (null != objReader)
                {
                    objReader.StopReading();
                }
                // Causes a control bound to the BindingSource to reread all
                // the items in the list and refresh their displayed values.
                if (!dispatchtimer.IsEnabled)
                {
                    Dispatcher.Invoke(new del(delegate()
                    {
                        tagdb.Repaint();
                    }));
                }
            }
            finally
            {
                // Enable performance tuning controls
                gridPerformanceTuning.IsEnabled = true;
                grpbxPerformanceTuning.IsEnabled = true;
                rdbtnperantenna.IsEnabled = rdbtnglobal.IsEnabled = true;
                txtReadPowerAnt1.IsEnabled = txtReadPowerAnt2.IsEnabled = txtReadPowerAnt3.IsEnabled = txtReadPowerAnt4.IsEnabled = true;
                txtWritePowerAnt1.IsEnabled = txtWritePowerAnt2.IsEnabled = txtWritePowerAnt3.IsEnabled = txtWritePowerAnt4.IsEnabled = true;
                grpGPIOBehaviour.IsEnabled = true;

                // If connected reader is Astra, don't enable performance tunning
                // and read data grp bx. Since these parameters are not supported
                // by Astra
                if (!model.Equals("Astra"))
                {
                    ReadDataGroupBox.IsEnabled = true; ;
                }
                else
                {
                    DisableControlsForAstra();
                }

                // Variables used for synchronizations 
                isAsyncReadGoingOn = false;
                TagResults.enableTagAgingOnRead = false;

                Mouse.SetCursor(Cursors.Arrow);

                lblshowStatus.Content = "Stopped";
                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                    UriKind.RelativeOrAbsolute));
                broadcastOFF();
                btnRead.ToolTip = "Start Async Read";
                btnRead.Content = "Read";

                // De register read exception and tag read and temperature listeners
                objReader.TagRead -= PrintTagRead;
                objReader.ReadException -= ReadException;

                txtfontSize.IsEnabled = true;
                txtRFOffTimeout.IsEnabled = true;
                txtRFOnTimeout.IsEnabled = true;
                stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = true;

                // Calculate read rate with time elapsed which is calculated before
                // calling StopReading() api method. So that we don't miss the 
                // remaining tags obtained in the tag database after calling 
                // stop reading() api method)
                if (!isReconnectFailed)
                    UpdateReadRate(continuousreadElapsedTime);

                tiTagResults.Focus();

                // Cache reader settings such as start address, read length, gen2 
                // mem bank
                CacheReadDataSettings();
                // Clear previous messages on status bar
                ClearMessageOnStatusBar();

                // Enable all the disabled buttons which not based on reader
                // type
                PerformAsyncReadInitialActions(true);
                //Revert the enableReadFilter to defaults.
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/tagReadData/enableReadFilter", true);
                }
                try
                {
                    lblTemperature.Visibility = lblReaderTemperature.Visibility = Visibility.Visible;
                    lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Performs async read initial actions. Ex. disable some buttons when async 
        /// read is in progress and enable when stopped 
        /// </summary>
        /// <param name="value">true or false</param>
        private void PerformAsyncReadInitialActions(bool value)
        {
            cbxBaudRate.IsEnabled = value;
            //Disable fast search
            //if ((model.Equals("M3e")))
            {
                // chkEnableFastSearch.IsEnabled = false;
            }
            //else
            {
                chkEnableFastSearch.IsEnabled = value;
            }
            //thingMagicReader.IsEnabled = false;
            regioncombo.IsEnabled = value;
            //Disable Select all check box
            Dispatcher.Invoke(new del(delegate()
            {
                CheckBox headerCheckBox = (CheckBox)GetChildControl(TagResults.dgTagResults, "headerCheckBox");
                headerCheckBox.IsEnabled = value;
            }));

            // All the buttons in the settings panel when async read is turned off
            grpbxRdrPwrSettngs.IsEnabled = value;

            // Disable load config button when async read
            btnLoadConfig.IsEnabled = value;

            M3eFilterGroupBox.IsEnabled = value;
            ProtocolsGroupBox.IsEnabled = value;
            AntennasGroupBox.IsEnabled = value;
            FilterGroupBox.IsEnabled = value;
            rdBtnReadOnce.IsEnabled = value;
            rdBtnReadContinuously.IsEnabled = value;
            rdBtnStopNRead.IsEnabled = value;
            rbtnGPITrigger.IsEnabled = value;
            btnConnect.IsEnabled = value;
            //Disable firmware Update when async read
            stackPanelFirmwareUpdate.IsEnabled = value;
            // Diable all the controls in data extensions 
            grpbxDataExtensions.IsEnabled = value;
            gridRegulatoryTesting.IsEnabled = value;

            //chkLBTEnable.IsEnabled = value;
            //chkEnableDwellTime.IsEnabled = value;
            //txtEnableLBTThreshold.IsEnabled = value;
            //txtDwelltime.IsEnabled = value;
            //btnRegionConfig.IsEnabled = value;
        }

        /// <summary>
        /// Validate rf off time and throw exception if the value 
        /// is greater then the max limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void readDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtRFOffTimeout.Text != "")
                {
                    if (Convert.ToInt32(txtRFOffTimeout.Text) > 65535)
                    {
                        MessageBox.Show("Please input rf off timeout less then 65535",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtRFOffTimeout.Text = "0";
                        return;
                    }

                    if (Convert.ToInt32(txtRFOffTimeout.Text) < 0)
                    {
                        txtRFOffTimeout.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtRFOffTimeout.Foreground = Brushes.Black;
                    }
                    if (objReader != null)
                        objReader.ParamSet("/reader/transportTimeout", int.Parse(txtRFOffTimeout.Text) + 5000);
                }
            }
            catch (Exception ex) { Onlog(ex); }
        }

        /// <summary>
        /// Gen2 protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gen2CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// ISO18-6b protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void iso6bCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// IPX64 protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ipx64CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// IPX256 protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ipx256CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// ATA protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ataCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// ISO18000_Ucode protocol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void isoUcodeCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            //For example if Gen2, then change select screen to word address. on
            //the other hand if it is ISO18-6b, then change it to byte address.
            OnProtocolSelect();
        }

        /// <summary>
        /// Sets up the tag operation protocol and antenna based on  Option Menu Bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void tagOpProtocolAntenna(object sender, EventArgs e)
        {
            {
                List<int> ant;
                try
                {
                    ant = GetSelectedAntennaList();
                    if ((ant.ToArray().Length == 0) || (ant.ToArray().Length > 1))
                    {
                        throw new ArgumentException();
                    }
                }
                catch (ArgumentException)
                {
                    throw new TagOpAntennaException("Please select a single antenna port for this operation");
                }

                try
                {
                    objReader.ParamSet("/reader/tagop/antenna", ant.ToArray()[0]);
                }
                catch (Exception)
                {
                    throw new TagOpAntennaException("Please select a single antenna port for this operation"); ;
                }
            }
            {
                tagOpProto.Clear();
                if ((bool)gen2CheckBox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.GEN2);
                }
                if ((bool)iso6bCheckBox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.ISO180006B);
                }
                if ((bool)ipx64CheckBox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.IPX64);
                }
                if ((bool)ipx256CheckBox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.IPX256);
                }
                if ((bool)ataCheckBox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.ATA);
                }
                if ((bool)isoUcodeCheckbox.IsChecked)
                {
                    tagOpProto.Add(TagProtocol.ISO180006B_UCODE);
                }
                if ((tagOpProto.ToArray().Length == 0) || (tagOpProto.ToArray().Length > 1))
                {
                    throw new TagOpProtocolException("Please select a single protocol for this operation");
                }
                else
                {
                    objReader.ParamSet("/reader/tagop/protocol", tagOpProto.ToArray()[0]);
                }
            }
        }

        /// <summary>
        /// Validate rf on time and throw exception if the timeout value is 
        /// greater then the max limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtRFOnTimeout.Text != "")
                {
                    if (Convert.ToInt32(txtRFOnTimeout.Text) > 65535)
                    {
                        MessageBox.Show("Please input rf on timeout less then 65535",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtRFOnTimeout.Text = "1000";
                        return;
                    }
                    if (Convert.ToInt32(txtRFOnTimeout.Text) < 0)
                    {
                        txtRFOnTimeout.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtRFOnTimeout.Foreground = Brushes.Black;
                    }
                    if (objReader != null)
                        objReader.ParamSet("/reader/transportTimeout", int.Parse(txtRFOnTimeout.Text) + 5000);
                }
            }
            catch (Exception ex) { Onlog(ex); }
        }

        /// <summary>
        /// Perform read for specified timeout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnReadOnce_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)rdBtnReadOnce.IsChecked)
                {
                    if (btnConnect.Content.ToString() != "Connect")
                    {
                        //Show the status
                        lblshowStatus.Content = "Connected";
                        btnClearReads_Click(sender, e);
                        lblReadOnce.Visibility = System.Windows.Visibility.Visible;
                        txtbxreadOnceTimeout.Visibility = System.Windows.Visibility.Visible;
                        lblRfOn.Visibility = System.Windows.Visibility.Collapsed;
                        lblRfOff.Visibility = System.Windows.Visibility.Collapsed;
                        txtRFOffTimeout.Visibility = System.Windows.Visibility.Collapsed;
                        txtRFOnTimeout.Visibility = System.Windows.Visibility.Collapsed;
                        lblStopNReadTagCount.Visibility = txtStopNReadTagCount.Visibility = Visibility.Collapsed;
                        if (objReader is SerialReader)
                        {
                            rdBtnStopNRead.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            rdBtnStopNRead.Visibility = Visibility.Collapsed;
                        }
                        if (model.Equals("M3e"))
                        {
                            rdBtnAutoProtocolSwitching.IsChecked = false;
                            rdBtnAutoProtocolSwitching.IsEnabled = false;
                            rdBtnEqualprotocolSwitching.IsChecked = true;
                        }
                        cbxGPIPort.SelectedItem = 0;
                        lblGPIPort.Visibility = cbxGPIPort.Visibility = Visibility.Collapsed;
                        btnRead.IsEnabled = true;
                        //un-comment the following code to make readstop trigger visible and enable.
                        //chkEnableReadStopTrigger.Visibility = Visibility.Visible;
                        //chkEnableReadStopTrigger.IsChecked = false;
                        tiTagResults.Focus();
                    }
                }
            }
            catch { };
        }

        /// <summary>
        /// Perform Read stop trigger until we receive specified Tag Count
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnStopNRead_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)rdBtnStopNRead.IsChecked)
                {
                    if (btnConnect.Content.ToString()!="Connect")
                    {
                        btnClearReads_Click(sender, e);
                        
                        lblRfOn.Visibility = txtRFOnTimeout.Visibility = lblRfOff.Visibility = txtRFOffTimeout.Visibility =lblReadOnce.Visibility = txtbxreadOnceTimeout.Visibility = Visibility.Collapsed;
                        cbxGPIPort.SelectedItem = 0;
                        lblStopNReadTagCount.Visibility = txtStopNReadTagCount.Visibility = Visibility.Visible;
                        if (model.Equals("M3e"))
                        {
                            rdBtnAutoProtocolSwitching.IsChecked = true;
                            rdBtnAutoProtocolSwitching.IsEnabled = true;
                            rdBtnEqualprotocolSwitching.IsChecked = false;
                        }
                        tiTagResults.Focus();
                    }
                }
            }
            catch (Exception)
            { };
        }

        /// <summary>
        /// Perform GPI Trigger read 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnGPITrigger_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)rbtnGPITrigger.IsChecked)
                {
                    if (btnConnect.Content.ToString() != "Connect")
                    {
                        btnClearReads_Click(sender, e);

                        lblReadOnce.Visibility = System.Windows.Visibility.Collapsed;
                        txtbxreadOnceTimeout.Visibility = System.Windows.Visibility.Collapsed;
                        lblStopNReadTagCount.Visibility = txtStopNReadTagCount.Visibility = Visibility.Collapsed;
                        //rdBtnStopNRead.IsChecked = false;
                        //rdBtnStopNRead.Visibility = Visibility.Collapsed;
                        lblRfOn.Visibility = lblRfOff.Visibility = txtRFOffTimeout.Visibility = txtRFOnTimeout.Visibility = System.Windows.Visibility.Collapsed;
                        if (model.Equals("M3e"))
                        {
                            rdBtnAutoProtocolSwitching.IsChecked = true;
                            rdBtnAutoProtocolSwitching.IsEnabled = true;
                            rdBtnEqualprotocolSwitching.IsChecked = false;
                        }
                        lblGPIPort.Visibility = cbxGPIPort.Visibility = Visibility.Visible;
                        //btnRead.IsEnabled = false;
                        cbxGPIPort.SelectedIndex = 0;
                        tiTagResults.Focus();
                    }
                }
                else
                {
                    cbxGPIPort.SelectedIndex = 0;
                }
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        /// <summary>
        /// Perform continuous read until user press stop read
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnReadContinuously_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)rdBtnReadContinuously.IsChecked)
                {
                    if (btnConnect.Content.ToString() != "Connect")
                    {
                        btnClearReads_Click(sender, e);
                        lblReadOnce.Visibility = System.Windows.Visibility.Collapsed;
                        txtbxreadOnceTimeout.Visibility = System.Windows.Visibility.Collapsed;
                        if (objReader is SerialReader)
                        {
                            rdBtnStopNRead.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            rdBtnStopNRead.Visibility = Visibility.Collapsed;
                        }
                        lblStopNReadTagCount.Visibility = txtStopNReadTagCount.Visibility = Visibility.Collapsed;
                        //chkEnableReadStopTrigger.Visibility = Visibility.Collapsed;
                        //chkEnableReadStopTrigger.IsChecked = false;
                        //lblStopTriggerCount.Visibility = Visibility.Collapsed;
                        //txtbxStopTrigger.Visibility = Visibility.Collapsed;
                        lblRfOn.Visibility = System.Windows.Visibility.Visible;
                        lblRfOff.Visibility = System.Windows.Visibility.Visible;
                        txtRFOffTimeout.Visibility = System.Windows.Visibility.Visible;
                        txtRFOnTimeout.Visibility = System.Windows.Visibility.Visible;
                        if (model.Equals("M3e"))
                        {
                            rdBtnAutoProtocolSwitching.IsChecked = true;
                            rdBtnAutoProtocolSwitching.IsEnabled = true;
                            rdBtnEqualprotocolSwitching.IsChecked = false;
                        }
                        cbxGPIPort.SelectedItem = 0;
                        lblGPIPort.Visibility = cbxGPIPort.Visibility = Visibility.Collapsed;
                        btnRead.IsEnabled = true;
                        tiTagResults.Focus();
                    }
                }
            }
            catch { };
        }

        /// <summary>
        /// Read timeout event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxreadOnceTimeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtbxreadOnceTimeout.Text != "")
                {
                    if (Convert.ToInt32(txtbxreadOnceTimeout.Text) > 65535)
                    {
                        MessageBox.Show("Please input timeout less then 65535",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtbxreadOnceTimeout.Text = "500";
                        return;
                    }
                    if (Convert.ToInt32(txtbxreadOnceTimeout.Text) < 0)
                    {
                        txtbxreadOnceTimeout.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtbxreadOnceTimeout.Foreground = Brushes.Black;
                    }
                    if (objReader != null)
                        objReader.ParamSet("/reader/transportTimeout", int.Parse(txtbxreadOnceTimeout.Text) + 5000);
                }
            }
            catch (Exception ex) { Onlog(ex); }
        }

        private void txtStopNReadTagCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtStopNReadTagCount.Text != "")
                {
                    if (Convert.ToInt32(txtStopNReadTagCount.Text) > 65535)
                    {
                        MessageBox.Show("Please input tags less then 65535",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtStopNReadTagCount.Text = "1";
                    }
                    if (Convert.ToInt32(txtStopNReadTagCount.Text) < 0)
                    {
                        txtStopNReadTagCount.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtStopNReadTagCount.Foreground = Brushes.Black;
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        /// <summary>
        /// Read button implementation. Performs read once or continuous read based
        /// on the read behavior 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            if (btnRead.Content.ToString() == "Read")
            {
                try
                {
                    if (regioncombo.SelectedItem.ToString() == "Select")
                    {
                        MessageBox.Show("Please select a Region",
                        "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if ((objReader is SerialReader) && (((ComboBoxItem)cbxBaudRate.SelectedItem).Content.ToString() == "Select"))
                    {
                        MessageBox.Show("Please select a valid BaudRate",
                        "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if ((rbtnGPITrigger.IsChecked == true) && ((ComboBoxItem)cbxGPIPort.SelectedItem).Content.ToString() == "Select")
                    {
                        MessageBox.Show("Please select a GPI to read on",
                        "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    ValidateRefreshRate();
                    // Validating TID filter for Higgs3 tag
                    bool status = validateTidFilterData();
                    if (!status)
                    {
                        return;
                    }
                    if (isHttpPostServiceEnabled)
                    {
                        if (!SaveHttpPostServiceSettings())
                        {
                            return;
                        }
                    }
                    if (true)
                    {
                        //Implement the Check for the Tag type selected.
                    }
                    if (model.ToLower().Contains("Sargas") || model.ToLower().Contains("Izar"))
                    {
                        lblTemperature.Visibility = lblReaderTemperature.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        lblTemperature.Visibility = lblReaderTemperature.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                    return;
                }
            }
            if (clientConnected || isHttpPostServiceEnabled)
            {
                broadcastON();
            }
            if ((bool)rdBtnReadOnce.IsChecked)
            {
                //call read once
                btnRead.ToolTip = "Perform read based on the read behavior defined "
                    + " by the settings in the Read Options sidebar.";
                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                    UriKind.RelativeOrAbsolute));
                // Enabling deduplication in API in case of synchronous read.
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/tagReadData/enableReadFilter", true);
                }
                readTag_Click(sender, e);
                imgReaderStatus.Source = new BitmapImage(new Uri(@"..\Icons\LedGreen.png",
                    UriKind.RelativeOrAbsolute));
            }
            if ((bool)rdBtnReadContinuously.IsChecked || (bool)rdBtnStopNRead.IsChecked || (bool)rbtnGPITrigger.IsChecked)
            {
                //call read continuous
                btnRead.ToolTip = "Start Async Read";
                // If user selects multiple antennas with equal time switching API is not returning 
                // single tag count per record. To achieve this disabling the deduplication in API
                // in case of asynchronous read
                if (btnRead.Content.ToString() == "Read" && (objReader is SerialReader))
                {
                    objReader.ParamSet("/reader/tagReadData/enableReadFilter", false);
                }
                startRead_Click(sender, e);
            }
        }

        /// <summary>
        /// Stop continuous read if going on 
        /// </summary>
        void stopReading()
        {
            if (null != objReader)
            {
                objReader.StopReading();
                objReader.TagRead -= PrintTagRead;
                objReader.ReadException -= ReadException;
            }
        }

        /// <summary>
        /// When "Fast Search" is selected, the target should be changed to "AB" (required
        /// per the Fast Search HLD). (User should be able to manually change Target to
        /// something else if they wish.)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkEnableFastSearch_Checked(object sender, RoutedEventArgs e)
        {
            if (null != chkEnableFastSearch)
            {
                if ((bool)chkEnableFastSearch.IsChecked)
                {
                    // Save the target in temporary variable before changing it to target AB,
                    // when fast search is enabled.
                    tempTarget = OptimalReaderSettings["/reader/gen2/target"];
                    OptimalReaderSettings["/reader/gen2/target"] = "AB";
                }
                else
                {
                    //Change to the original target, when fast search is disabled.
                    OptimalReaderSettings["/reader/gen2/target"] = tempTarget;
                }
            }
        }

        /// Enable/Disable Antenna detection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkbxAntennaDetection_Click(object sender, RoutedEventArgs e)
        {
            var ckkbxStatus = sender as CheckBox;
            SetCheckPort(sender, e, ckkbxStatus.IsChecked.Value);
        }

        /// <summary>
        /// Antenna detection feature. Detect connected antennas on the reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="value"></param>
        private void SetCheckPort(Object sender, EventArgs e, bool value)
        {
            if (null != objReader)
            {
                try
                {
                    if (regioncombo.SelectedItem.ToString() != "Select")
                    {
                        objReader.ParamSet("/reader/antenna/checkPort", value);
                        ConfigureLogicalAntennaBoxes(objReader);
                    }
                    else
                    {
                        MessageBox.Show("Please select region", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                        chkbxAntennaDetection.IsChecked = false;
                    }
                }
                catch (ReaderCodeException ex)
                {
                    MessageBox.Show(ex.Message
                        + " Check supported reader configurations in Reader's Hardware Guide.",
                        "Unsupported Reader Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                    chkbxAntennaDetection.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// Condition to check textbox to have only number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtRFOnTimeout_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }
        /// <summary>
        /// Condition to check textbox to have only number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtStopNReadTagCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// Validate read once timeout and throw exception when the timeout
        /// is less then the specified min limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxreadOnceTimeout_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtbxreadOnceTimeout.Text != "")
            {
                if ((bool)rdbtnNetworkConnection.IsChecked)
                {
                    if (Convert.ToInt32(txtbxreadOnceTimeout.Text) < 30)
                    {
                        MessageBox.Show("Please input read once timeout greater then 30 ms",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtbxreadOnceTimeout.Text = "500";
                        return;
                    }
                }
            }
            else
            {
                MessageBox.Show("Timeout (ms) can't be empty", "Universal Reader Assistant Message",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtbxreadOnceTimeout.Text = "500";
            }
        }

        private void txtStopNReadTagCount_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtStopNReadTagCount.Text=="")
            {
                MessageBox.Show("Number of tags can't be empty", "Universal Reader Assistant Message",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtStopNReadTagCount.Text = "1";
            }
        }

        #endregion ReadOptions

        #region DisplayOptions

        /// <summary>
        /// Enable bigNumUniqueTagCounts option and disable the remaining big num options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expAdvRdOpRdBtnUniqueTagCountBigNum_Checked(object sender, RoutedEventArgs e)
        {
            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Visible;
            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Enable bigNumTotalTagCounts option and disable the remaining big num options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expAdvRdOpRdBtnTotalTagCountBigNum_Checked(object sender, RoutedEventArgs e)
        {
            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Visible;
            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Enable bigNumSummary of read option and disable the remaining big num options
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expAdvRdOpRdBtnCountsBigNum_Checked(object sender, RoutedEventArgs e)
        {
            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            gridCountsBigNum.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Disable big num feature
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expAdvRdOpRdBtnClearBigNumSelection_Checked(object sender, RoutedEventArgs e)
        {
            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Column with check-box implementation
        /// </summary>
        public class ColumnSelectionForTagResult : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            protected bool isColumnChecked = false;
            public Boolean IsColumnChecked
            {
                get { return isColumnChecked; }
                set
                {
                    isColumnChecked = value;
                    NotifyPropertyChanged("IsColumnChecked");
                }
            }

            public String SelectedColumn { get; set; }

            public ColumnSelectionForTagResult(bool columnchecked, string columnName)
            {
                IsColumnChecked = columnchecked;
                SelectedColumn = columnName;
            }
        }

        /// <summary>
        /// Enable or disable the selected columns on tag results tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddColumnsToGrid(object sender, RoutedEventArgs e)
        {
            ColumnSelectionForTagResult rda;
            for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
            {
                rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);

                //re order column width based on column selection for readability
                if (rda.IsColumnChecked)
                {
                    //Adjust columns based on the content of the column header
                    TagResults.epcColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    TagResults.timeStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    TagResults.rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    TagResults.readCountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    if (model.Equals("M3e"))
                    {
                        TagResults.tagTypeColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    }
                }
                else
                {
                    //Adjust columns in equal proportion of available space
                    TagResults.epcColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    TagResults.timeStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    TagResults.rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    TagResults.readCountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    if (model.Equals("M3e"))
                    {
                        TagResults.tagTypeColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    }
                }

                switch (rda.SelectedColumn)
                {
                    case "Antenna":
                        if (rda.IsColumnChecked)
                        {
                            tagdb.chkbxUniqueByAntenna = true;
                            TagResults.antennaColumn.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            tagdb.chkbxUniqueByAntenna = false;
                            TagResults.antennaColumn.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        break;
                    case "Phase":
                        if (rda.IsColumnChecked)
                        {
                            TagResults.phaseColumn.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            TagResults.phaseColumn.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        break;
                    case "Frequency":
                        if (rda.IsColumnChecked)
                        {
                            tagdb.chkbxUniqueByFrequency = true;
                            TagResults.frequencyColumn.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            tagdb.chkbxUniqueByFrequency = false;
                            TagResults.frequencyColumn.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        break;
                    case "Protocol":
                        if (rda.IsColumnChecked)
                        {
                            TagResults.protocolColumn.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            TagResults.protocolColumn.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        break;
                    case "GPIO":
                        if (rda.IsColumnChecked && rdbtnNetworkConnection.IsChecked == false)
                        {
                            TagResults.GPIOColumn.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            TagResults.GPIOColumn.Visibility = Visibility.Collapsed;
                        }
                        break;
                    //case "BrandID":
                    //    if (rda.IsColumnChecked && btnRead.Content.ToString() == "Read")
                    //    {
                    //        //As there is defualt select for Brand ID 
                    //        //So all the filters will be disabled and defualt filter will be applied 
                    //        chkApplyFilter1.IsChecked = false;
                    //        TagResults.BrandIDColumn.Visibility = System.Windows.Visibility.Visible;
                    //    }
                    //    else
                    //    {
                    //        TagResults.BrandIDColumn.Visibility = System.Windows.Visibility.Collapsed;
                    //        if (btnRead.Content.ToString() != "Read")
                    //        {
                    //            rda.IsColumnChecked = false;
                    //        }
                    //    }
                    //    break;
                }
            }
            TagResults.dgTagResults.InvalidateVisual();
        }

        /// <summary>
        /// Refresh fixed reader ip address
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefreshReadersList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                InitializeReaderUriBox();
                Mouse.SetCursor(Cursors.Arrow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// To enable tag aging. Same event handler will be called for both checked and unchecked event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkEnableTagAging_Checked(object sender, RoutedEventArgs e)
        {
            TagResults.chkEnableTagAging = (bool)chkEnableTagAging.IsChecked;
        }

        /// <summary>
        /// Enable or disable the selected big num option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxBigNum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != cbxBigNum)
            {
                try
                {
                    string text = ((ComboBoxItem)cbxBigNum.SelectedItem).Content.ToString();
                    switch (text)
                    {
                        case "Select":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Unique Tag Count":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Visible;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Total Tag Count":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Visible;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Summary of Tag Result":
                            bigNumUniqueTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            bigNumTotalTagCounts.Visibility = System.Windows.Visibility.Collapsed;
                            gridCountsBigNum.Visibility = System.Windows.Visibility.Visible;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    cbxBigNum.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Font size of the tag results tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtfontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if ((Convert.ToInt32(txtfontSize.Text) <= 0) || (Convert.ToInt32(txtfontSize.Text) > 20))
                {
                    txtfontSize.Foreground = Brushes.Red;
                }
                else
                {
                    txtfontSize.Foreground = Brushes.Black;
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        ///<summary>
        /// Change tagresults time stamp format based on the selected format in Time stamp combo-box
        ///</summary>
        ///<param name="sender"></param>
        ///<param name="e"></param>
        private void cbxTimestampFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagResults.timeStampColumn.Binding != null)
            {
                try
                {
                    string text = ((ComboBoxItem)cbxTimestampFormat.SelectedItem).Content.ToString();
                    switch (text)
                    {
                        case "DD/MM/YYYY HH:MM:Sec.MillSec":
                            TagResults.timeStampColumn.Binding = new Binding("TimeStamp")
                            {
                                StringFormat = "{0:dd/MM/yyyy hh:mm:ss:fff tt}"
                            };
                            break;
                        case "MM/DD/YYYY HH:MM:Sec.MillSec":
                            TagResults.timeStampColumn.Binding = new Binding("TimeStamp")
                            {
                                StringFormat = "{0:MM/dd/yyyy hh:mm:ss.fff tt}"
                            };
                            break;
                        case "YYYY/DD/MM HH:MM:Sec.MillSec":
                            TagResults.timeStampColumn.Binding = new Binding("TimeStamp")
                            {
                                StringFormat = "{0:yyyy/dd/MM hh:mm:ss.fff tt}"
                            };
                            break;
                        case "Select":
                            TagResults.timeStampColumn.Binding = new Binding("TimeStamp")
                            {
                                StringFormat = "{0:hh:mm:ss.fff tt}"
                            };
                            break;
                    }
                }
                catch
                {
                    MessageBox.Show("To set this, please clear the tag results",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbxTimestampFormat.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Refresh rate for tag result grid 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtRefreshRate_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                ValidateRefreshRate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtRefreshRate.Text = "100";
                return;
            }
        }

        /// <summary>
        /// Validate refresh rate of tag results grid
        /// </summary>
        private void ValidateRefreshRate()
        {

            if (txtRefreshRate.Text != "")
            {
                int refreshrate = 0;
                try
                {
                    refreshrate = Convert.ToInt32(txtRefreshRate.Text.TrimEnd());
                }
                catch { throw new Exception("Please input the refresh rate between 100 and 999"); }
                if ((refreshrate < 100) || (refreshrate > 999))
                {
                    throw new Exception("Please input the refresh rate between 100 and 999");
                }
                else
                {
                    try
                    {
                        if (null != dispatchtimer)
                        {
                            dispatchtimer.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(txtRefreshRate.Text));
                        }
                    }
                    catch (Exception ex)
                    {
                        Onlog(ex);
                    }
                }
            }
            else
            {
                throw new Exception("Please input the refresh rate between 100 and 999");
            }
        }

        #endregion DisplayOptions

        #region Gen2PerformanceTuning

        // Up down ui control implementation
        #region NumberUpDown

        private int _numValue = 1;

        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                txtTagsNum.Text = value.ToString();
            }
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            NumValue++;
            if (_numValue > 1)
            {
                cmdDown.IsEnabled = true;
            }
            if (_numValue > 99999)
            {
                NumValue--;
                cmdUp.IsEnabled = false;
            }
            UpDownCounterTextChange(sender);
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            NumValue--;
            if (_numValue <= 1)
            {
                _numValue = 1;
                cmdDown.IsEnabled = false;
            }
            if (_numValue < 99999)
            {
                cmdUp.IsEnabled = true;
            }
            UpDownCounterTextChange(sender);
        }
        private void txtTagsNum_PreviewTextChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            UpDownCounterTextChange(sender);
        }

        private void UpDownCounterTextChange(object sender)
        {
            if (null != txtTagsNum)
            {
                if (!int.TryParse(txtTagsNum.Text, out _numValue))
                {
                    txtTagsNum.Text = _numValue.ToString();
                }
                if (Convert.ToInt32(txtTagsNum.Text) <= 0)
                {
                    MessageBox.Show("Number of tags should be greater then zero",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtTagsNum.Text = "1";
                    _numValue = 1;
                    cmdDown.IsEnabled = false;
                    rdbtnOptmzExtmdNoTagsInField_Checked(sender, new RoutedEventArgs());
                    return;
                }
                if (null != cmdDown)
                {
                    if (_numValue <= 1)
                    {
                        cmdDown.IsEnabled = false;
                        cmdUp.IsEnabled = true;
                    }
                    if (_numValue > 1)
                    {
                        cmdDown.IsEnabled = true;
                    }
                }
                Gen2SettingChanged["Q"] = true;
                rdbtnOptmzExtmdNoTagsInField_Checked(sender, new RoutedEventArgs());
            }
        }
        #endregion NumberUpDown

        /// <summary>
        /// To set tag population size. Automatically adjust as population changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdbtnAutoAdjstAsPoplChngs_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != objReader)
                {
                    if ((bool)rdbtnAutoAdjstAsPoplChngs.IsChecked)
                    {
                        stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
                        // If tag population is selected as "auto", the Q setting should
                        // remain as "auto" i.e dynamic Q
                        objReader.ParamSet("/reader/gen2/q", new Gen2.DynamicQ());
                        OptimalReaderSettings["/reader/gen2/q"] = "DynamicQ";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }

            finally
            {
                if (null != objReader)
                {
                    LoadGen2Settings();
                }
            }
        }

        /// <summary>
        /// Read distance vs read rate 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sldrRdDistVsrdRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (null != objReader)
                {
                    string blf = OptimalReaderSettings["/reader/gen2/BLF"];
                    string tari = OptimalReaderSettings["/reader/gen2/tari"];
                    string tagencoding = OptimalReaderSettings["/reader/gen2/tagEncoding"];
                    switch (((Int32)sldrRdDistVsrdRate.Value))
                    {
                        case 0:
                            //Maximize tag read distance

                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ";

                            if (!model.Equals("Astra"))
                            {
                                if (model.Equals("M7e Pico"))
                                {
                                    OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M4";
                                }
                                else
                                {
                                    OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M8";
                                }
                            }
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_25US";
                            break;
                        case 1:
                            break;
                        case 2:
                            //Maximize tag read rate
                            if (!model.Equals("Astra"))
                            {
                                if(model.Equals("M7e Pico"))
                                    OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M2";
                                else
                                    OptimalReaderSettings["/reader/gen2/tagEncoding"] = "FM0";
                            }
                            if (model.Equals("M6e") || model.Equals("Mercury6") || model.Equals("Astra-EX")
                                || model.Equals("Sargas") || model.Equals("Izar") || model.Equals("M6e Micro") || model.Equals("M6e Micro USB")
                                || model.Equals("M6e Micro USBPro") || model.Equals("M6e PRC") || model.Equals("M6e JIC") || model.Equals("M6e Nano") || model.Equals("M7e Pico"))
                            {
                                if (model.Equals("M7e Pico"))
                                    OptimalReaderSettings["/reader/gen2/BLF"] = "LINK320KHZ";
                                else
                                    OptimalReaderSettings["/reader/gen2/BLF"] = "LINK640KHZ";
                            }
                            if (model.Equals("M7e Pico"))
                                OptimalReaderSettings["/reader/gen2/tari"] = "TARI_12_5US";
                            else
                                OptimalReaderSettings["/reader/gen2/tari"] = "TARI_6_25US";
                            break;
                    }
                    if (!OptimalReaderSettings["/reader/gen2/BLF"].Equals(blf))
                        Gen2SettingChanged["BLF"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/tari"].Equals(tari))
                        Gen2SettingChanged["TARI"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/tagEncoding"].Equals(tagencoding))
                        Gen2SettingChanged["TAGENCODING"] = true;

                    SetGen2ReaderSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (null != objReader)
                {
                    LoadGen2Settings();
                }
            }
        }

        /// <summary>
        /// Control tag response rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sldrTagrspLessVsTagrspMore_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SldrTagrspLessVsTagrspMoreValueChanged();
        }

        /// <summary>
        /// Set session and target based on the values set by the tag response less or more slider
        /// </summary>
        private void SldrTagrspLessVsTagrspMoreValueChanged()
        {
            try
            {
                if (null != objReader)
                {
                    string session = OptimalReaderSettings["/reader/gen2/session"];
                    string target = OptimalReaderSettings["/reader/gen2/target"];
                    switch (((Int32)sldrTagrspLessVsTagrspMore.Value))
                    {
                        case 0:
                            OptimalReaderSettings["/reader/gen2/session"] = "S2";
                            OptimalReaderSettings["/reader/gen2/target"] = "A";
                            break;
                        case 1:
                            OptimalReaderSettings["/reader/gen2/session"] = "S2";
                            OptimalReaderSettings["/reader/gen2/target"] = "AB";
                            break;
                        case 2:
                            OptimalReaderSettings["/reader/gen2/session"] = "S1";
                            OptimalReaderSettings["/reader/gen2/target"] = "A";
                            break;
                        case 3:
                            OptimalReaderSettings["/reader/gen2/session"] = "S1";
                            OptimalReaderSettings["/reader/gen2/target"] = "AB";
                            break;
                        case 4:
                            OptimalReaderSettings["/reader/gen2/session"] = "S0";
                            OptimalReaderSettings["/reader/gen2/target"] = "A";
                            break;
                    }
                    if (!OptimalReaderSettings["/reader/gen2/session"].Equals(session))
                        Gen2SettingChanged["SESSION"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/target"].Equals(target))
                        Gen2SettingChanged["TARGET"] = true;

                    SetGen2ReaderSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (null != objReader)
                {
                    LoadGen2Settings();
                }
            }
        }

        private void rdBtnTagsRespondOption_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdBtnTagsRespondOption.IsChecked)
            {
                stkPanelTagsRespondOption.Visibility = System.Windows.Visibility.Visible;
                txtblkTagrspLess.Visibility = System.Windows.Visibility.Visible;
                txtblkTagrspMore.Visibility = System.Windows.Visibility.Visible;
                sldrTagrspLessVsTagrspMore.Visibility = System.Windows.Visibility.Visible;
                SldrTagrspLessVsTagrspMoreValueChanged();
            }
            else if ((bool)(!rdBtnTagsRespondOption.IsChecked))
            {
                stkPanelTagsRespondOption.Visibility = System.Windows.Visibility.Collapsed;
                txtblkTagrspLess.Visibility = System.Windows.Visibility.Collapsed;
                txtblkTagrspMore.Visibility = System.Windows.Visibility.Collapsed;
                sldrTagrspLessVsTagrspMore.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void rdBtnSlBstChforPoplSize_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != objReader)
                {
                    string session = OptimalReaderSettings["/reader/gen2/session"];
                    if ((bool)rdBtnSlBstChforPoplSize.IsChecked)
                    {
                        if ((bool)rdbtnOptmzExtmdNoTagsInField.IsChecked)
                        {
                            int tagPopulationCount = Convert.ToInt32(txtTagsNum.Text);
                            switch (GetBLFTagEncodingEncodedValue())
                            {
                                case 1:
                                    if ((tagPopulationCount >= 1) && (tagPopulationCount <= 250))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S0";
                                    }
                                    else if ((tagPopulationCount >= 251) && (tagPopulationCount <= 1000))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S1";
                                    }
                                    else if (tagPopulationCount > 1000)
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S2";
                                    }
                                    break;
                                case 2:
                                    if ((tagPopulationCount >= 1) && (tagPopulationCount <= 125))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S0";
                                    }
                                    else if ((tagPopulationCount >= 126) && (tagPopulationCount <= 500))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S1";
                                    }
                                    else if (tagPopulationCount > 500)
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S2";
                                    }
                                    break;
                                case 3:
                                    if ((tagPopulationCount >= 1) && (tagPopulationCount <= 100))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S0";
                                    }
                                    else if ((tagPopulationCount >= 101) && (tagPopulationCount <= 400))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S1";
                                    }
                                    else if (tagPopulationCount > 400)
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S2";
                                    }
                                    break;
                                case 4:
                                    if ((tagPopulationCount >= 1) && (tagPopulationCount <= 50))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S0";
                                    }
                                    else if ((tagPopulationCount >= 51) && (tagPopulationCount <= 200))
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S1";
                                    }
                                    else if (tagPopulationCount > 200)
                                    {
                                        OptimalReaderSettings["/reader/gen2/session"] = "S2";
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            //Select session 1 if no tag population size has been declared
                            OptimalReaderSettings["/reader/gen2/session"] = "S1";
                        }
                        if (!session.Equals(OptimalReaderSettings["/reader/gen2/session"]))
                            Gen2SettingChanged["session"] = true;

                        SetGen2ReaderSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (null != objReader)
                {
                    LoadGen2Settings();
                }
            }
        }

        /// <summary>
        /// Apply Gen2 Settings while Read is in Progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyGen2Settings_Click(object sender, RoutedEventArgs e)
        {
            LoadGen2Settings();
        }

        /// <summary>
        /// Get blf tag encoding encoded value
        /// </summary>
        /// <returns></returns>
        private int GetBLFTagEncodingEncodedValue()
        {
            int retValue = 0;
            try
            {
                Gen2.LinkFrequency linkFrequency = (Gen2.LinkFrequency)objReader.ParamGet("/reader/gen2/BLF");
                Gen2.TagEncoding tagEncoding = (Gen2.TagEncoding)objReader.ParamGet("/reader/gen2/tagEncoding");
                if (((Gen2.LinkFrequency.LINK640KHZ == linkFrequency)
                    || (Gen2.LinkFrequency.LINK250KHZ == linkFrequency))
                    && (Gen2.TagEncoding.FM0 == tagEncoding))
                {
                    retValue = 1;
                }
                if (Gen2.LinkFrequency.LINK250KHZ == linkFrequency)
                {
                    if (Gen2.TagEncoding.M2 == tagEncoding)
                    {
                        retValue = 2;
                    }
                    else if (Gen2.TagEncoding.M4 == tagEncoding)
                    {
                        retValue = 3;
                    }
                    else if (Gen2.TagEncoding.M8 == tagEncoding)
                    {
                        retValue = 4;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            };
            return retValue;
        }

        #region Custom Gen2 Settings

        /// <summary>
        /// Get all the current gen2 settings and set the radio buttons accordingly
        /// </summary>
        private void LoadGen2Settings()
        {
            if (model.Equals("M3e"))
            {
                return;
            }
            //Get all the current settings and set the radio buttons accordingly
            // Astra doesn't support blf parameter
            if (!(model.Equals("Astra")))
            {
                xLINK250KHZ.IsEnabled = true;
                xLINK320KHZ.IsEnabled = true;
                xLINK640KHZ.IsEnabled = true;
            }
            else
            {
                xLINK250KHZ.IsEnabled = false;
            }
            xLINK320KHZ.IsEnabled = false;
            xLINK640KHZ.IsEnabled = false;
            try
            {
                switch (model)
                {
                    case "Mercury6":
                    case "Astra-EX":
                    case "Sargas":
                    case "Izar":
                    case "M6e":
                    case "M6e Micro":
                    case "M6e Micro USB":
                    case "M6e Micro USBPro":
                    case "M6e PRC":
                    case "M6e JIC":
                    case "M7e Mega":
                    case "M7e Tera":
                        xLINK250KHZ.IsEnabled = true;
                        xLINK640KHZ.IsEnabled = true;
                        xLINK320KHZ.IsEnabled = true;
                        break;
                    case "M6e Nano":
                        xLINK250KHZ.IsEnabled = true;
                        xLINK640KHZ.IsEnabled = false;
                        break;
                    case "M7e Pico":
                        xLINK250KHZ.IsEnabled = true;
                        xLINK320KHZ.IsEnabled = true;
                        xLINK640KHZ.IsEnabled = false;
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                Onlog("ArgumentException : " + ex.Message);
                Onlog(ex);
            }

            try
            {
                // Astra doesn't support blf parameter
                if (!(model.Equals("Astra")))
                {
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/BLF"))
                    {
                        switch (OptimalReaderSettings["/reader/gen2/BLF"])
                        {
                            case "LINK250KHZ":
                                xLINK250KHZ.IsChecked = true;
                                LINK250KHZ.IsChecked = true;
                                break;
                            case "LINK320KHZ":
                                xLINK320KHZ.IsChecked = true;
                                LINK320KHZ.IsChecked = true;
                                break;
                            case "LINK640KHZ":
                                xLINK640KHZ.IsChecked = true;
                            LINK320KHZ.IsChecked = true; 
                                break;
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Onlog("ArgumentException : " + ex.Message);
                Onlog(ex);
            }

            xgrpbxtTari.IsEnabled = true;
            xtari625.IsEnabled = true;
            xtari125.IsEnabled = true;
            xtari25.IsEnabled = true;
            try
            {
                // Astra doesn't support tari parameter
                if (!(model.Equals("Astra")))
                {
                    grpbxtTari.IsEnabled = true;
                    switch (model)
                    {
                        case "Mercury6":
                        case "Astra-EX":
                        case "Sargas":
                        case "Izar":
                        case "M6e":
                        case "M6e Micro":
                        case "M6e Micro USB":
                        case "M6e Micro USBPro":
                        case "M6e PRC":
                        case "M6e JIC":
                            xtari625.IsEnabled = true;
                            if (xLINK320KHZ.IsChecked == true || xLINK640KHZ.IsChecked == true)
                            {
                                xtari125.IsEnabled = false;
                                xtari25.IsEnabled = false;
                            }
                            else
                            {
                                xtari125.IsEnabled = true;
                                xtari25.IsEnabled = true;
                            }
                            break;
                        case "M6e Nano":
                            xtari625.IsEnabled = false;
                            xtari125.IsEnabled = false;
                            xtari25.IsEnabled = true;
                            break;
                        case "M7e Pico":
                            xtari625.IsEnabled = false;
                            xtari25.IsEnabled = true;
                            if (xLINK250KHZ.IsChecked == true)
                                xtari125.IsEnabled = false;
                            else if (xLINK320KHZ.IsChecked == true)
                                xtari125.IsEnabled = true;
                            break;
                        case "M7e Mega":
                        case "M7e Tera":
                            xtari625.IsEnabled = false;
                            if (xLINK640KHZ.IsChecked == true)
                            {
                                xtari625.IsEnabled = true;
                            }
                            else if (xLINK320KHZ.IsChecked == true)
                            {
                                xtari125.IsEnabled = true;
                                xtari25.IsEnabled = true;
                            }
                            else //250KHZ is checked
                            {
                                xtari25.IsEnabled = true;
                                xtari125.IsEnabled = false;
                            }
                            break;
                    }
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/tari"))
                    {
                        switch (OptimalReaderSettings["/reader/gen2/tari"])
                        {
                            case "TARI_6_25US":
                                xtari625.IsChecked = true;
                                tari625.IsChecked = true;
                                break;
                            case "TARI_12_5US":
                                xtari125.IsChecked = true;
                                tari125.IsChecked = true;
                                break;
                            case "TARI_25US":
                                xtari25.IsChecked = true;
                                tari25.IsChecked = true;
                                break;
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Onlog("ArgumentException : " + ex.Message);
                Onlog(ex);
            }
            catch (ReaderCodeException ex)
            {
                Onlog("ReaderCodeException : " + ex.Message);
                Onlog(ex);
            }

            try
            {
                // Astra doesn't support tagencoding parameter
                if (!(model.Equals("Astra")))
                {
                    if ((model.Equals("Mercury6")) || (model.Equals("Astra-EX")) || (model.Equals("Sargas")) || (model.Equals("Izar"))
                        || (model.Equals("M6e")) || (model.Equals("M6e Micro")) || (model.Equals("M6e Micro USB"))
                        || (model.Equals("M6e Micro USBPro")) || (model.Equals("M6e PRC")) || (model.Equals("M6e JIC")))
                    {
                        xFM0.IsEnabled = true;
                    }
                    else
                    {
                        xFM0.IsEnabled = false;
                    }

                    if (model.Equals("M7e Pico"))
                    {
                        xM8.IsEnabled = false;
                        if (xLINK250KHZ.IsChecked == true)
                        {
                            xM2.IsEnabled = false;
                            xM4.IsEnabled = true;
                        }
                        else if (xLINK320KHZ.IsChecked == true)
                        {
                            xM2.IsEnabled = true;
                            if (xtari25.IsChecked == true)
                            {
                                xM4.IsEnabled = true;
                                xM2.IsEnabled = true;
                            }
                            else if (xtari125.IsChecked == true)
                            {
                                xM4.IsEnabled = false;
                                xM4.IsChecked = false;
                                xM2.IsEnabled = true;
                                xM2.IsChecked = true;
                            }
                        }
                    }
                    else if (model.Equals("M7e Mega") || model.Equals("M7e Tera"))
                    {
                        xFM0.IsEnabled = false;
                        xM4.IsEnabled = true;
                        xM8.IsEnabled = false;
                        if (xLINK250KHZ.IsChecked == true)
                        {
                            xM2.IsEnabled = false;
                        }
                        else if (xLINK320KHZ.IsChecked == true)
                        {
                            if (xtari25.IsChecked == true)
                            {
                                xM2.IsEnabled = true;
                            }
                            else if (xtari125.IsChecked == true)
                            {
                                xM2.IsEnabled = true;
                                xM4.IsEnabled = false;
                            }
                        }// 640KHZ is checked
                        else
                        {
                            if (model.Equals("M7e Tera"))
                            {
                                xFM0.IsEnabled = true;
                            }
                            xM2.IsEnabled = true;
                        }
                    }
                    else
                    {
                        if ((xLINK320KHZ.IsChecked == true) || (xLINK640KHZ.IsChecked == true))
                        {
                            xM2.IsEnabled = false;
                            xM4.IsEnabled = false;
                            xM8.IsEnabled = false;
                        }
                        else
                        {
                            xM2.IsEnabled = true;
                            xM4.IsEnabled = true;
                            xM8.IsEnabled = true;
                        }
                    }

                    xFM0.IsEnabled = true;
                    xM2.IsEnabled = true;
                    xM4.IsEnabled = true;
                    xM8.IsEnabled = true;
                    if (OptimalReaderSettings.ContainsKey("/reader/gen2/tagEncoding"))
                    {
                        switch (OptimalReaderSettings["/reader/gen2/tagEncoding"])
                        {
                            case "FM0":
                                xFM0.IsChecked = true;
                                FM0.IsChecked = true;
                                break;
                            case "M2":
                                xM2.IsChecked = true;
                                M2.IsChecked = true;
                                break;
                            case "M4":
                                xM4.IsChecked = true;
                                M4.IsChecked = true;
                                break;
                            case "M8":
                                xM8.IsChecked = true;
                                M8.IsChecked = true;
                                break;
                        }
                    }
                }
                else
                {
                    xFM0.IsEnabled = false;
                    xM2.IsEnabled = false;
                    xM4.IsEnabled = false;
                    xM8.IsEnabled = false;
                }
            }
            catch (ArgumentException)
            {
                xFM0.IsEnabled = false;
                xM2.IsEnabled = false;
                xM4.IsEnabled = false;
                xM8.IsEnabled = false;
            }

            // Disable all blf, tari and tagencoding settings here, so that user can't select.
            // These are disabled because Impinj chip internally uses modes concept instead of individual gen2 settings and it internally sets some other settings. 
            // Remove them once Impinj modes implementation is done. 
            if (usrModeToSet != SerialReader.UserMode.OPEN)
            {
                LINK250KHZ.IsEnabled = false;
                LINK320KHZ.IsEnabled = false;
                LINK640KHZ.IsEnabled = false;
                tari625.IsEnabled = false;
                tari125.IsEnabled = false;
                tari25.IsEnabled = false;
                FM0.IsEnabled = false;
                M2.IsEnabled = false;
                M4.IsEnabled = false;
                M8.IsEnabled = false;
            }

            try
            {
                if (OptimalReaderSettings.ContainsKey("/reader/gen2/rfMode") && OptimalReaderSettings["/reader/gen2/rfMode"] != null)
                {
                    xrfMode.SelectedValue = OptimalReaderSettings["/reader/gen2/rfMode"].ToString();
                }
            }
            catch (ArgumentException)
            {
                xrfMode.SelectedIndex = -1;
            }
            try
            {
                switch (OptimalReaderSettings["/reader/gen2/session"])
                {
                    case "S0":
                        xS0.IsChecked = true;
                        S0.IsChecked = true;
                        break;
                    case "S1":
                        xS1.IsChecked = true;
                        S1.IsChecked = true;
                        break;
                    case "S2":
                        xS2.IsChecked = true;
                        S2.IsChecked = true;
                        break;
                    case "S3":
                        xS3.IsChecked = true;
                        S3.IsChecked = true;
                        break;
                }
            }
            catch (ArgumentException)
            {
                xS0.IsEnabled = false;
                xS1.IsEnabled = false;
                xS2.IsEnabled = false;
                xS3.IsEnabled = false;
            }
            try
            {
                switch (OptimalReaderSettings["/reader/gen2/target"])
                {
                    case "A":
                        xA.IsChecked = true;
                        A.IsChecked = true;
                        break;
                    case "B":
                        xB.IsChecked = true;
                        B.IsChecked = true;
                        break;
                    case "AB":
                        xAB.IsChecked = true;
                        AB.IsChecked = true;
                        break;
                    case "BA":
                        xBA.IsChecked = true;
                        BA.IsChecked = true;
                        break;
                }
            }
            catch (FeatureNotSupportedException)
            {
                xA.IsEnabled = false;
                xB.IsEnabled = false;
                xAB.IsEnabled = false;
                xBA.IsEnabled = false;
            }
            try
            {
                // Astra doesn't support tagencoding parameter
                if (!(model.Equals("Astra")))
                {
                    xDynamicQ.IsEnabled = true;
                    xStaticQ.IsEnabled = true;
                    if (OptimalReaderSettings["/reader/gen2/q"] == "DynamicQ")
                    {
                        xDynamicQ.IsChecked = true;
                        xQvalue.SelectedIndex = -1;
                        //DynamicQ.IsChecked = true;
                        //Qvalue.SelectedIndex = -1;
                    }
                    else if (OptimalReaderSettings["/reader/gen2/q"] == "StaticQ")
                    {
                        xStaticQ.IsChecked = true;
                        xQvalue.IsEnabled = true;
                        xQvalue.SelectedIndex = Convert.ToInt32(
                            OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                        //StaticQ.IsChecked = true;
                        //Qvalue.IsEnabled = true;
                        //Qvalue.SelectedIndex = Convert.ToInt32(
                        //    OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                    }
                }
                else
                {
                    xStaticQ.IsEnabled = false;
                    xDynamicQ.IsEnabled = false;
                    xQvalue.IsEnabled = false;
                }
            }
            catch (FeatureNotSupportedException)
            {
                xStaticQ.IsEnabled = false;
                xDynamicQ.IsEnabled = false;
            }

            try
            {
                // Astra doesn't support tagencoding parameter
                if (!(model.Equals("Astra")))
                {
                    yDynamic_InitQ.IsEnabled = true;
                    Gen2.InitQ iq = new Gen2.InitQ();
                    //iq.qEnable = Convert.ToBoolean( OptimalReaderSettings["/reader/gen2/initQ"]);
                    if (OptimalReaderSettings.ContainsKey("/application/performanceTuning/initQValue"))
                    {
                        iq.initialQ = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);

                        List<string> result = OptimalReaderSettings["/reader/gen2/initQ"].Split(',').ToList();
                        if (result[0].Contains("True"))
                        {
                            yDynamic_InitQ.IsChecked = true;
                            xDInitQValue1.Text = iq.initialQ.ToString();
                            //Dynamic_InitQ.IsChecked = true;
                            //DInitQValue.Text = iq.initialQ.ToString();
                        }
                        else
                        {
                            yDynamic_InitQ.IsChecked = false;
                            xDInitQValue1.Text = "2";
                            //Dynamic_InitQ.IsChecked = false;
                            //DInitQValue.Text = "2";
                        }
                    }
                    //else if (OptimalReaderSettings["/reader/gen2/q"] == "StaticQ")
                    //{
                    //    StaticQ.IsChecked = true;
                    //    Qvalue.IsEnabled = true;
                    //    Qvalue.SelectedIndex = Convert.ToInt32(
                    //        OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                    //}
                }
                else
                {
                    yDynamic_InitQ.IsEnabled = false;
                    xDInitQValue1.IsEnabled = false;
                }
            }
            catch (FeatureNotSupportedException)
            {
                yDynamic_InitQ.IsEnabled = false;
                xDInitQValue1.IsEnabled = false;
            }

            if (rdBtnReadContinuously.IsChecked != true || Convert.ToInt64(txtRFOffTimeout.Text) != 0)
            {
                try
                {
                    sldrReadPwr.Value = (double)(Convert.ToDouble(objReader.ParamGet("/reader/radio/readPower").ToString()) / 100);
                    sldrWritePwr.Value = (double)(Convert.ToDouble(objReader.ParamGet("/reader/radio/writePower").ToString()) / 100);
                }
                catch (Exception ex)
                {
                    Onlog("Read power exception : " + ex.Message);
                }

                try
                {
                    chkbxAntennaDetection.IsChecked = (bool)objReader.ParamGet("/reader/antenna/checkPort");
                    chkbxAntennaDetection.IsEnabled = true;
                }
                catch (FeatureNotSupportedException)
                {
                    chkbxAntennaDetection.IsEnabled = false;
                }
                catch (ArgumentException)
                {
                    chkbxAntennaDetection.IsEnabled = false;
                }
            }
            if (usrModeToSet != SerialReader.UserMode.OPEN)
            {
                //disable all gen2 UI elements so that user cannot select.
                S0.IsEnabled = false;
                S1.IsEnabled = false;
                S2.IsEnabled = false;
                S3.IsEnabled = false;

                A.IsEnabled = false;
                B.IsEnabled = false;
                AB.IsEnabled = false;
                BA.IsEnabled = false;

                StaticQ.IsEnabled = false;
                DynamicQ.IsEnabled = false;
                Dynamic_InitQ.IsEnabled = false;
                DInitQValue.IsEnabled = false;
            }
        }

        /// <summary>
        /// Enables all the gen2 UI elements for the open user mode
        /// </summary>
        private void enableGen2Settings()
        {
            LINK250KHZ.IsEnabled = true;
            LINK320KHZ.IsEnabled = true;
            LINK640KHZ.IsEnabled = true;

            tari625.IsEnabled = true;
            tari125.IsEnabled = true;
            tari25.IsEnabled = true;
            
            FM0.IsEnabled = true;
            M2.IsEnabled = true;
            M4.IsEnabled = true;
            M8.IsEnabled = true;

            //enable session, target and Q settings
            S0.IsEnabled = true;
            S1.IsEnabled = true;
            S2.IsEnabled = true;
            S3.IsEnabled = true;

            A.IsEnabled = true;
            B.IsEnabled = true;
            AB.IsEnabled = true;
            BA.IsEnabled = true;

            StaticQ.IsEnabled = true;
            DynamicQ.IsEnabled = true;
            Dynamic_InitQ.IsEnabled = true;
            DInitQValue.IsEnabled = true;

            //reset changed values here
            Gen2SettingChanged["BLF"] = false;
            Gen2SettingChanged["TARI"] = false;
            Gen2SettingChanged["TAGENCODING"] = false;
            Gen2SettingChanged["TARGET"] = false;
            Gen2SettingChanged["SESSION"] = false;
            Gen2SettingChanged["Q"] = false;
            Gen2SettingChanged["initQ"] = false;
        }

        /// <summary>
        /// Disables all the gen2 UI elements for the open user mode
        /// </summary>
        private void disableGen2Settings()
        {
            LINK250KHZ.IsEnabled = false;
            LINK320KHZ.IsEnabled = false;
            LINK640KHZ.IsEnabled = false;

            tari625.IsEnabled = false;
            tari125.IsEnabled = false;
            tari25.IsEnabled = false;

            FM0.IsEnabled = false;
            M2.IsEnabled = false;
            M4.IsEnabled = false;
            M8.IsEnabled = false;

            //enable session, target and Q settings
            S0.IsEnabled = false;
            S1.IsEnabled = false;
            S2.IsEnabled = false;
            S3.IsEnabled = false;

            A.IsEnabled = false;
            B.IsEnabled = false;
            AB.IsEnabled = false;
            BA.IsEnabled = false;

            StaticQ.IsEnabled = false;
            DynamicQ.IsEnabled = false;
            Dynamic_InitQ.IsEnabled = false;
            DInitQValue.IsEnabled = false;
        }

        /// <summary>
        /// Save the set gen2 settings. So that these can be set on the
        /// connected reader when read is performed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveGen2Settings_Click(object sender, EventArgs e)
        {
            try
            {
                //if (initialReaderSettingsLoaded && ((bool)chkCustomizeGen2Settings.IsChecked))
                if ((initialReaderSettingsLoaded) && ((usrModeToSet == SerialReader.UserMode.OPEN) || ((bool)chkCustomizeGen2Settings.IsChecked)))
                {
                    string blf = OptimalReaderSettings.ContainsKey("/reader/gen2/BLF") ? OptimalReaderSettings["/reader/gen2/BLF"] : null;
                    string tari = OptimalReaderSettings.ContainsKey("/reader/gen2/tari") ? OptimalReaderSettings["/reader/gen2/tari"]: null;
                    string tagencoding = OptimalReaderSettings.ContainsKey("/reader/gen2/tagEncoding") ? OptimalReaderSettings["/reader/gen2/tagEncoding"] : null;
                    string session = OptimalReaderSettings["/reader/gen2/session"];
                    string target = OptimalReaderSettings["/reader/gen2/target"];
                    string gen2q = OptimalReaderSettings["/reader/gen2/q"];
                    string gen2Initq = OptimalReaderSettings["/reader/gen2/initQ"];
                    string qcount = "0";
                    if (OptimalReaderSettings.ContainsKey("/application/performanceTuning/staticQValue"))
                        qcount = OptimalReaderSettings["/application/performanceTuning/staticQValue"];
                    string initqDefualt = "2";
                    if (OptimalReaderSettings.ContainsKey("/application/performanceTuning/initQValue"))
                        initqDefualt = OptimalReaderSettings["/application/performanceTuning/initQValue"];
                    string rfMode = OptimalReaderSettings.ContainsKey("/reader/gen2/rfMode") ? OptimalReaderSettings["/reader/gen2/rfMode"] : null;
                    Mouse.SetCursor(Cursors.Wait);
                    //LINK250KHZ_CheckedChanged
                    if ((bool)xLINK250KHZ.IsChecked)
                    {
                        switch (model)
                        {
                            case "Mercury6":
                            case "Astra-EX":
                            case "Sargas":
                            case "Izar":
                            case "M6e":
                            case "M6e PRC":
                            case "M6e JIC":
                            case "M6e Micro":
                            case "M6e Micro USB":
                            case "M6e Micro USBPro":
                                xFM0.IsEnabled = true;
                                xtari625.IsEnabled = true;
                                xtari125.IsEnabled = true;
                                xtari25.IsEnabled = true;
                                break;
                            case "M6e Nano":
                                xFM0.IsEnabled = false;
                                xtari625.IsEnabled = false;
                                xtari125.IsEnabled = false;
                                xtari25.IsEnabled = true;
                                break;
                            case "M7e Pico":
                            case "M7e Mega":
                            case "M7e Tera":
                                xM4.IsEnabled = true;
                                xM4.IsChecked = true;
                                xtari25.IsEnabled = true;
                                xtari25.IsChecked = true;
                                xtari125.IsEnabled = false;
                                xM2.IsEnabled = false;
                                xM8.IsEnabled = false;
                                break;
                            default:
                                xFM0.IsEnabled = false;
                                break;
                        }
                        //Work around for bug #2063 for Paine release
                        if (!model.Equals("M7e Pico"))
                        {
                            xM2.IsEnabled = true;
                            xM4.IsEnabled = true;
                            xM8.IsEnabled = true;
                        }
                        if (!M7eFamilyList.Contains(model))
                        {
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ";
                        }
                    }
                    //LINK640KHZ_CheckedChanged
                    if ((bool)xLINK640KHZ.IsChecked)
                    {
                        if ((bool)xgrpbxtTari.IsEnabled)
                        {
                            xtari625.IsEnabled = true;
                            xtari125.IsEnabled = false;
                            xtari25.IsEnabled = false;
                            xtari625.IsChecked = true;
                        }
                        switch (model)
                        {
                            case "M7e Mega":
                                xFM0.IsEnabled = false;
                                xM2.IsEnabled = true;
                                xM4.IsEnabled = true;
                                xM8.IsEnabled = false;
                                break;
                            case "M7e Tera":
                                xFM0.IsEnabled = true;
                                xM2.IsEnabled = true;
                                xM4.IsEnabled = true;
                                xM8.IsEnabled = false;
                                break;
                            default:
                                //Work around for bug #2063 for Paine release
                                xFM0.IsEnabled = true;
                                xM2.IsEnabled = false;
                                xM4.IsEnabled = false;
                                xM8.IsEnabled = false;
                                break;
                        }
                        if (!M7eFamilyList.Contains(model))
                        {
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK640KHZ";
                        }
                    }
                    //LINK320KHZ_CheckedChanged
                    if ((bool)xLINK320KHZ.IsChecked)
                    {
                        switch (model)
                        {
                            case "M7e Pico":
                            case "M7e Mega":
                            case "M7e Tera":
                                xtari125.IsEnabled = true;
                                xtari25.IsEnabled = true;
                                if (xtari25.IsChecked == true)
                                {
                                    xM2.IsEnabled = true;
                                    xM4.IsEnabled = true;
                                }
                                else if (xtari125.IsChecked == true)
                                {
                                    xM4.IsEnabled = false;
                                    xM4.IsChecked = false;
                                    xM2.IsEnabled = true;
                                    xM2.IsChecked = true;
                                }
                                break;
                            case "Astra-EX":
                            case "Sargas":
                            case "Izar":
                            case "M6e":
                            case "M6e PRC":
                            case "M6e JIC":
                            case "M6e Micro":
                            case "M6e Micro USB":
                            case "M6e Micro USBPro":
                                if ((bool)xgrpbxtTari.IsEnabled)
                                {
                                    xtari625.IsEnabled = true;
                                    xtari125.IsEnabled = false;
                                    xtari25.IsEnabled = false;
                                    xtari625.IsChecked = true;
                                }
                                xFM0.IsEnabled = true;
                                xFM0.IsChecked = true;
                                xM2.IsEnabled = false;
                                xM4.IsEnabled = false;
                                xM8.IsEnabled = false;
                                break;
                            default:
                                break;
                        }
                        if (!M7eFamilyList.Contains(model))
                        {
                            OptimalReaderSettings["/reader/gen2/BLF"] = "LINK320KHZ";
                        }
                    }

                    if (!M7eFamilyList.Contains(model))
                    {
                        //tari625_CheckedChanged
                        if ((bool)xtari625.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_6_25US";
                        }

                        //tari125_CheckedChanged
                        if ((bool)xtari125.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_12_5US";
                        }

                        //tari25_CheckedChanged
                        if ((bool)xtari25.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tari"] = "TARI_25US";
                        }

                        //FM0_CheckedChanged
                        if ((bool)xFM0.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "FM0";
                        }

                        //M2_CheckedChanged
                        if ((bool)xM2.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M2";
                        }

                        //M4_CheckedChanged
                        if ((bool)xM4.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M4";
                        }

                        //M8_CheckedChanged
                        if ((bool)xM8.IsChecked)
                        {
                            OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M8";
                        }
                    }

                    //S0_CheckedChanged
                    if ((bool)xS0.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/session"] = "S0";
                    }

                    //S1_CheckedChanged
                    if ((bool)xS1.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/session"] = "S1";
                    }

                    //S2_CheckedChanged
                    if ((bool)xS2.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/session"] = "S2";
                    }

                    //S3_CheckedChanged
                    if ((bool)xS3.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/session"] = "S3";
                    }

                    //A_CheckedChanged
                    if ((bool)xA.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/target"] = "A";
                    }

                    //B_CheckedChanged
                    if ((bool)xB.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/target"] = "B";
                    }

                    //AB_CheckedChanged
                    if ((bool)xAB.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/target"] = "AB";
                    }

                    //BA_CheckedChanged
                    if ((bool)xBA.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/target"] = "BA";
                    }

                    //DynamicQ_CheckedChanged
                    if ((bool)xDynamicQ.IsChecked)
                    {
                        OptimalReaderSettings["/reader/gen2/q"] = "DynamicQ";
                        xQvalue.IsEnabled = false;
                        yDynamic_InitQ.IsEnabled = true;
                    }

                    if (((bool)xStaticQ.IsChecked == true))
                    {
                        //Enable Q value combo-box
                        xQvalue.IsEnabled = true;
                        if (xQvalue.SelectedIndex == -1)
                            xQvalue.SelectedIndex = 0;
                        yDynamic_InitQ.IsEnabled = false;
                        yDynamic_InitQ.IsChecked = false;
                        //Qvalue_SelectedIndexChanged
                        OptimalReaderSettings["/reader/gen2/q"] = "StaticQ";
                        OptimalReaderSettings["/application/performanceTuning/staticQValue"] = xQvalue.SelectedIndex.ToString();
                    }

                    if (((bool)yDynamic_InitQ.IsChecked == true))
                    {
                        //Enable Q value combo-box
                        xDInitQValue1.IsEnabled = true;
                        //if (DInitQValue.SelectedIndex == -1)
                        //    DInitQValue.SelectedIndex = 0;

                        //Qvalue_SelectedIndexChanged
                        Gen2.InitQ gitQ = new Gen2.InitQ();
                        gitQ.qEnable = true;
                        gitQ.initialQ = Convert.ToUInt16(xDInitQValue1.Text);
                        OptimalReaderSettings["/reader/gen2/initQ"] = gitQ.ToString();
                        OptimalReaderSettings["/application/performanceTuning/initQValue"] = gitQ.initialQ.ToString();
                    }
                    else
                    {
                        //List<string> result = OptimalReaderSettings["/reader/gen2/initQ"].Split(',').ToList();
                        Gen2.InitQ iq = new Gen2.InitQ();
                        //if (result[0].Contains("True"))
                        //{
                        //    iq.qEnable = true;
                        //    iq.initialQ = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                        //    objReader.ParamSet("/reader/gen2/initQ", iq);
                        //}
                        //else
                        //{
                        iq.qEnable = false;
                        iq.initialQ = 2;// Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                        objReader.ParamSet("/reader/gen2/initQ", iq);
                        //}
                        xDInitQValue1.IsEnabled = false;
                        OptimalReaderSettings["/reader/gen2/initQ"] = "false";
                        OptimalReaderSettings["/application/performanceTuning/initQValue"] = "2";
                    }
                    ////WORD_ONLY_CheckedChanged
                    //if ((bool)WORD_ONLY.IsChecked)
                    //{
                    //    reader.ParamSet("/reader/gen2/writeMode", Gen2.WriteMode.WORD_ONLY);
                    //}

                    ////BLOCK_ONLY_CheckedChanged
                    //if ((bool)BLOCK_ONLY.IsChecked)
                    //{
                    //    reader.ParamSet("/reader/gen2/writeMode", Gen2.WriteMode.BLOCK_ONLY);
                    //}

                    ////BLOCK_FALLBACK_CheckedChanged
                    //if ((bool)BLOCK_FALLBACK.IsChecked)
                    //{
                    //    reader.ParamSet("/reader/gen2/writeMode", Gen2.WriteMode.BLOCK_FALLBACK);
                    //}

                    if(xrfMode.SelectedIndex != -1)
                    {
                        OptimalReaderSettings["/reader/gen2/rfMode"] = ((ComboBoxItem)(xrfMode.SelectedItem)).Content.ToString();
                    }
                    if (!M7eFamilyList.Contains(model))
                    {
                        if (!OptimalReaderSettings["/reader/gen2/BLF"].Equals(blf))
                            Gen2SettingChanged["BLF"] = true;
                        if (!OptimalReaderSettings["/reader/gen2/tari"].Equals(tari))
                            Gen2SettingChanged["TARI"] = true;
                        if (!OptimalReaderSettings["/reader/gen2/tagEncoding"].Equals(tagencoding))
                            Gen2SettingChanged["TAGENCODING"] = true;
                    }
                    if (!OptimalReaderSettings["/reader/gen2/session"].Equals(session))
                        Gen2SettingChanged["SESSION"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/target"].Equals(target))
                        Gen2SettingChanged["TARGET"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/q"].Equals(gen2q) || !qcount.Equals(xQvalue.SelectedIndex.ToString()))
                        Gen2SettingChanged["Q"] = true;
                    if (!OptimalReaderSettings["/reader/gen2/initQ"].Equals(gen2Initq) || !initqDefualt.Equals(xDInitQValue1.SelectedIndex.ToString()))
                        Gen2SettingChanged["initQ"] = true;
                    if (M7eFamilyList.Contains(model) && isRFModeSupp)
                    {
                        if (!OptimalReaderSettings["/reader/gen2/rfMode"].Equals(rfMode))
                            Gen2SettingChanged["RFMODE"] = true;
                    }

                    SetGen2ReaderSettings();

                    // Disable all blf, tari and tagencoding settings here, so that user can't select.
                    // These are disabled because Impinj chip internally uses modes concept instead of individual gen2 settings and it internally sets some other settings. 
                    // Allow user to change them only if user mode is OPEN.
                    if (usrModeToSet != SerialReader.UserMode.OPEN)
                    {
                        LINK250KHZ.IsEnabled = false;
                        LINK320KHZ.IsEnabled = false;
                        LINK640KHZ.IsEnabled = false;

                        tari625.IsEnabled = false;
                        tari125.IsEnabled = false;
                        tari25.IsEnabled = false;

                        FM0.IsEnabled = false;
                        M2.IsEnabled = false;
                        M4.IsEnabled = false;
                        M8.IsEnabled = false;

                        S0.IsEnabled = false;
                        S1.IsEnabled = false;
                        S2.IsEnabled = false;
                        S3.IsEnabled = false;

                        A.IsEnabled = false;
                        B.IsEnabled = false;
                        AB.IsEnabled = false;
                        BA.IsEnabled = false;

                        StaticQ.IsEnabled = false;
                        DynamicQ.IsEnabled = false;
                        Dynamic_InitQ.IsEnabled = false;
                        DInitQValue.IsEnabled = false;
                    }

                    Mouse.SetCursor(Cursors.Arrow);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message
                    + " Check supported protocol configurations in Reader's Hardware Guide.",
                    "Unsupported Reader Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.SetCursor(Cursors.Arrow);
                LoadGen2Settings();
            }
        }
        //need to work on this function.
        private void SetGen2ReaderSettings()
        {
            try
            {
                if (objReader != null)
                {
                    xctkbiBusyIndicator.IsBusy = true;
                    if (!bgwApplyGen2Settings.IsBusy)
                        bgwApplyGen2Settings.RunWorkerAsync();
                }
            }
            catch (Exception)
            {

            }
        }

        void bgwApplyGen2Settings_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                xctkbiBusyIndicator.IsBusy = false;
            }
            catch (Exception)
            {

            }
        }

        void bgwApplyGen2Settings_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!M7eFamilyList.Contains(model))
                {
                    if (Gen2SettingChanged["BLF"])
                    {
                        try
                        {
                            if (OptimalReaderSettings["/reader/gen2/BLF"].Equals("LINK640KHZ"))
                            {
                                objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK640KHZ);
                            }
                            else if (OptimalReaderSettings["/reader/gen2/BLF"].Equals("LINK320KHZ"))
                            {
                                objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK320KHZ);
                            }
                            else
                            {
                                objReader.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
                            }
                        }
                        catch (Exception)
                        {
                            // make the busy indicator false, so that it doesn't show up in the UI.
                            Dispatcher.Invoke((Action)(() =>
                            {
                                xctkbiBusyIndicator.IsBusy = false;
                            }));

                            Gen2.LinkFrequency OldBlf = (Gen2.LinkFrequency)objReader.ParamGet("/reader/gen2/BLF");
                            String newBlf = OptimalReaderSettings["/reader/gen2/BLF"].Substring(4, 3);
                            String errorMsg = "You are trying to change BLF from " + OldBlf + " to " + OptimalReaderSettings["/reader/gen2/BLF"];
                            if (Convert.ToUInt32(newBlf) == 640 && (model.Equals("M7e Pico") || model.Equals("M7e Deka") || model.Equals("M7e Hecto")))
                            {
                                errorMsg += " , which is not supported for the current reader.";
                            }
                            else
                            {
                                if (Convert.ToUInt32(newBlf) > gen2BLFObjectToInt(OldBlf))
                                {
                                    errorMsg += " for better read rate. Try changing the TagEncoding and Tari values to match the BLF.";
                                }
                                else
                                {
                                    errorMsg += " for better sensitivity. Try changing the TagEncoding and Tari values to match the BLF.";
                                }
                            }
                            // populate exception back to user in message box
                            MessageBox.Show(errorMsg, "Reader Message",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke((Action)(() =>
                            {
                                switch (OldBlf)
                                {
                                    case Gen2.LinkFrequency.LINK250KHZ:
                                        OptimalReaderSettings["/reader/gen2/BLF"] = "LINK250KHZ";
                                        LINK250KHZ.IsChecked = true;
                                        break;
                                    case Gen2.LinkFrequency.LINK320KHZ:
                                        OptimalReaderSettings["/reader/gen2/BLF"] = "LINK320KHZ";
                                        LINK320KHZ.IsChecked = true;
                                        break;
                                    case Gen2.LinkFrequency.LINK640KHZ:
                                        OptimalReaderSettings["/reader/gen2/BLF"] = "LINK640KHZ";
                                        LINK250KHZ.IsChecked = true;
                                        break;
                                }
                            }));
                        }
                    }

                    if (Gen2SettingChanged["TARI"])
                    {
                        try
                        {
                            switch (OptimalReaderSettings["/reader/gen2/tari"])
                            {
                                case "TARI_25US":
                                    objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_25US); break;
                                case "TARI_12_5US":
                                    objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_12_5US); break;
                                case "TARI_6_25US":
                                    objReader.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_6_25US); break;
                                default: break;
                            }
                        }
                        catch (Exception)
                        {
                            // make the busy indicator false, so that it doesn't show up in the UI.
                            Dispatcher.Invoke((Action)(() =>
                            {
                                xctkbiBusyIndicator.IsBusy = false;
                            }));

                            Gen2.Tari oldTari = (Gen2.Tari)objReader.ParamGet("/reader/gen2/tari");
                            String newTari = OptimalReaderSettings["/reader/gen2/tari"];
                            String errorMsg = "You are trying to change Tari from " + oldTari + " to " + OptimalReaderSettings["/reader/gen2/tari"];
                            if (gen2TariObjectToInt(newTari) > gen2TariObjectToInt(oldTari.ToString()))
                            {
                                errorMsg += " for better sensitivity. Try changing the TagEncoding and BLF values to match the Tari.";
                            }
                            else
                            {
                                errorMsg += " for better read rate. Try changing the TagEncoding and BLF values to match the Tari.";
                            }
                            // populate exception back to user in message box
                            MessageBox.Show(errorMsg, "Reader Message",
                                                MessageBoxButton.OK, MessageBoxImage.Error);

                            Dispatcher.Invoke((Action)(() =>
                            {
                                switch (oldTari)
                                {
                                    case Gen2.Tari.TARI_25US:
                                        OptimalReaderSettings["/reader/gen2/tari"] = "TARI_25US";
                                        tari25.IsChecked = true;
                                        break;
                                    case Gen2.Tari.TARI_12_5US:
                                        OptimalReaderSettings["/reader/gen2/tari"] = "TARI_12_5US";
                                        tari125.IsChecked = true;
                                        break;
                                    case Gen2.Tari.TARI_6_25US:
                                        OptimalReaderSettings["/reader/gen2/tari"] = "TARI_6_25US";
                                        tari625.IsChecked = true;
                                        break;
                                    default: break;
                                }
                            }));
                        }
                    }

                    if (Gen2SettingChanged["TAGENCODING"])
                    {
                        try
                        {
                            switch (OptimalReaderSettings["/reader/gen2/tagEncoding"])
                            {
                                case "FM0": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.FM0); break;
                                case "M2": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M2); break;
                                case "M4": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M4); break;
                                case "M8": objReader.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M8); break;
                                default: break;
                            }
                        }
                        catch (Exception)
                        {
                            // make the busy indicator false, so that it doesn't show up in the UI.
                            Dispatcher.Invoke((Action)(() =>
                            {
                                xctkbiBusyIndicator.IsBusy = false;
                            }));

                            Gen2.TagEncoding oldEncoding = (Gen2.TagEncoding)objReader.ParamGet("/reader/gen2/tagEncoding");
                            String newEncoding = OptimalReaderSettings["/reader/gen2/tagEncoding"];
                            String errorMsg = "You are trying to change TagEncoding from " + oldEncoding + " to " + OptimalReaderSettings["/reader/gen2/tagEncoding"];
                            if (gen2TagEncodingObjectToInt(newEncoding) > gen2TagEncodingObjectToInt(oldEncoding.ToString()))
                            {
                                errorMsg += " for better sensitivity. Try changing the BLF and Tari values to match the TagEncoding.";
                            }
                            else
                            {
                                errorMsg += " for better read rate. Try changing the BLF and Tari values to match the TagEncoding.";
                            }
                            // populate exception back to user in message box
                            MessageBox.Show(errorMsg, "Reader Message",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                            Dispatcher.Invoke((Action)(() =>
                            {
                                switch (oldEncoding)
                                {
                                    case Gen2.TagEncoding.FM0:
                                        OptimalReaderSettings["/reader/gen2/tagEncoding"] = "FM0";
                                        FM0.IsChecked = true;
                                        break;
                                    case Gen2.TagEncoding.M2:
                                        OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M2";
                                        M2.IsChecked = true;
                                        break;
                                    case Gen2.TagEncoding.M4:
                                        OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M4";
                                        M4.IsChecked = true;
                                        break;
                                    case Gen2.TagEncoding.M8:
                                        OptimalReaderSettings["/reader/gen2/tagEncoding"] = "M8";
                                        M8.IsChecked = true;
                                        break;
                                    default: break;
                                }
                            }));
                        }
                    }
                }

                try
                {
                    if (Gen2SettingChanged["Q"])
                    {
                        if (OptimalReaderSettings["/reader/gen2/q"] == "DynamicQ")
                        {
                            objReader.ParamSet("/reader/gen2/q", new Gen2.DynamicQ());
                        }
                        else
                        {
                            byte qValue = Convert.ToByte(
                                OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                            objReader.ParamSet("/reader/gen2/q", new Gen2.StaticQ(qValue));
                        }
                    }

                    if (Gen2SettingChanged["initQ"])
                    {
                        List<string> result = OptimalReaderSettings["/reader/gen2/initQ"].Split(',').ToList();
                        Gen2.InitQ iq = new Gen2.InitQ();
                        if (result[0].Contains("True"))
                        {
                            iq.qEnable = true;
                            iq.initialQ = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                            objReader.ParamSet("/reader/gen2/initQ", iq);
                        }
                        else
                        {
                            iq.qEnable = false;
                            iq.initialQ = 2;// Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                            objReader.ParamSet("/reader/gen2/initQ", iq);
                        }
                    }
                }catch(Exception ex)
                {
                    // make the busy indicator false, so that it doesn't show up in the UI.
                    Dispatcher.Invoke((Action)(() =>
                    {
                        xctkbiBusyIndicator.IsBusy = false;
                    }));

                    // populate exception back to user in message box
                    MessageBox.Show(ex.Message, "Reader Message",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Gen2.InitQ initQ = (Gen2.InitQ)objReader.ParamGet("/reader/gen2/initQ");
                    Dispatcher.Invoke((Action)(() =>
                    {
                        if (initQ != null)
                        {
                            OptimalReaderSettings["/reader/gen2/initQ"] = initQ.qEnable.ToString();
                            OptimalReaderSettings["/application/performanceTuning/initQValue"] = initQ.initialQ.ToString();
                            yDynamic_InitQ.IsChecked = initQ.qEnable;
                            xDInitQValue1.SelectedIndex = initQ.initialQ;
                        }
                    }));
                }

                if (Gen2SettingChanged["SESSION"])
                {
                    switch (OptimalReaderSettings["/reader/gen2/session"])
                    {
                        case "S0": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S0); break;
                        case "S1": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S1); break;
                        case "S2": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S2); break;
                        case "S3": objReader.ParamSet("/reader/gen2/session", Gen2.Session.S3); break;
                        default: break;
                    }
                }

                if (Gen2SettingChanged["TARGET"])
                {
                    switch (OptimalReaderSettings["/reader/gen2/target"])
                    {
                        case "A": objReader.ParamSet("/reader/gen2/target", Gen2.Target.A); break;
                        case "AB": objReader.ParamSet("/reader/gen2/target", Gen2.Target.AB); break;
                        case "B": objReader.ParamSet("/reader/gen2/target", Gen2.Target.B); break;
                        case "BA": objReader.ParamSet("/reader/gen2/target", Gen2.Target.BA); break;
                        default: break;
                    }
                }

                if (Gen2SettingChanged.ContainsKey("RFMODE") && Gen2SettingChanged["RFMODE"])
                {
                    try
                    {
                        switch(OptimalReaderSettings["/reader/gen2/rfMode"])
                        {
                            case "RFMODE_160_M8_20": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_160_M8_20); break;
                            case "RFMODE_250_M4_20": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_250_M4_20); break;
                            case "RFMODE_320_M2_15": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_320_M2_15); break;
                            case "RFMODE_320_M2_20": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_320_M2_20); break;
                            case "RFMODE_320_M4_20": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_320_M4_20); break;
                            case "RFMODE_640_FM0_7_5": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_640_FM0_7_5); break;
                            case "RFMODE_640_M2_7_5": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_640_M2_7_5); break;
                            case "RFMODE_640_M4_7_5": objReader.ParamSet("/reader/gen2/rfMode", Gen2.RFMode.RFMODE_640_M4_7_5); break;
                            default:break;
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Exception in setting rf Mode: " + ex.Message.ToString());
                        // make the busy indicator false, so that it doesn't show up in the UI.
                        Dispatcher.Invoke((Action)(() =>
                        {
                            xctkbiBusyIndicator.IsBusy = false;
                        }));

                        Gen2.RFMode oldrfMode = (Gen2.RFMode)objReader.ParamGet("/reader/gen2/rfMode");
                        // populate exception back to user in message box
                        MessageBox.Show(ex.Message, "Reader Message",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Dispatcher.Invoke((Action)(() =>
                        {
                            switch (oldrfMode)
                            {
                                case Gen2.RFMode.RFMODE_160_M8_20:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_160_M8_20";
                                    xrfMode.SelectedIndex = 0;
                                    break;
                                case Gen2.RFMode.RFMODE_250_M4_20:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_250_M4_20";
                                    xrfMode.SelectedIndex = 1;
                                    break;
                                case Gen2.RFMode.RFMODE_320_M2_15:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_320_M2_15";
                                    xrfMode.SelectedIndex = 2;
                                    break;
                                case Gen2.RFMode.RFMODE_320_M2_20:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_320_M2_20";
                                    xrfMode.SelectedIndex = 3;
                                    break;
                                case Gen2.RFMode.RFMODE_320_M4_20:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_320_M4_20";
                                    xrfMode.SelectedIndex = 4;
                                    break;
                                case Gen2.RFMode.RFMODE_640_FM0_7_5:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_640_FM0_7_5";
                                    xrfMode.SelectedIndex = 5;
                                    break;
                                case Gen2.RFMode.RFMODE_640_M2_7_5:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_640_M2_7_5";
                                    xrfMode.SelectedIndex = 6;
                                    break;
                                case Gen2.RFMode.RFMODE_640_M4_7_5:
                                    OptimalReaderSettings["/reader/gen2/rfMode"] = "RFMODE_640_M4_7_5";
                                    xrfMode.SelectedIndex = 7;
                                    break;
                                default: break;
                            }
                        }));
                    }
                    
                }

                Gen2SettingChanged["BLF"] = false;
                Gen2SettingChanged["TARI"] = false;
                Gen2SettingChanged["TAGENCODING"] = false;
                Gen2SettingChanged["TARGET"] = false;
                Gen2SettingChanged["SESSION"] = false;
                Gen2SettingChanged["Q"] = false;
                Gen2SettingChanged["initQ"] = false;
                if (M7eFamilyList.Contains(model))
                {
                    Gen2SettingChanged["RFMODE"] = false;
                }
            }
            catch (Exception exMsg)
            {
                // populate exception back to user in message box
                MessageBox.Show(exMsg.Message, "Reader Message",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        /// <summary>
        /// Function which returns only the blf value in integer form from Gen2.LinkFrequency
        /// </summary>
        /// <param name="val">Link frequency</param>
        /// <returns>integer value</returns>
        private int gen2BLFObjectToInt(Gen2.LinkFrequency val)
        {
            switch (val)
            {
                case Gen2.LinkFrequency.LINK250KHZ:
                {
                    return 250;
                }
                case Gen2.LinkFrequency.LINK320KHZ:
                {
                    return 320;
                }
                case Gen2.LinkFrequency.LINK640KHZ:
                {
                    return 640;
                }
                default:
                {
                    throw new ArgumentException("Unsupported tag BLF.");
                }
            }
        }

        /// <summary>
        /// Function to return Tari value from String
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private decimal gen2TariObjectToInt(String val)
        {
            int startIndex = val.IndexOf("_"); //TARI_ first occurrence of _
            int length = val.IndexOf("US") - startIndex;
            val = val.Substring(startIndex+1, length-1).Replace('_', '.');
            return Convert.ToDecimal(val);
        }

        /// <summary>
        /// Function to return the number after tagencoding like 0 in FM0, 2 in M2, 4 in M4, 8 in M8.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private int gen2TagEncodingObjectToInt(String val)
        {
            int startIndex = val.IndexOf("M");
            val = val.Substring(startIndex + 1);
            return Convert.ToInt32(val);
        }

        /// <summary>
        /// Set reader read power slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sldrReadPwr_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (null != objReader)
                {
                    Mouse.SetCursor(Cursors.Wait);

                    Slider slider = (Slider)sender;
                    //MessageBox.Show( objReader.ParamGet("/reader/region/id").ToString());
                    if (Reader.Region.NA == regionToSet && model.Contains("Astra200"))
                    {
                        if (slider.Value > 30)
                        {
                            slider.Value = (double)30;
                        }
                    }
                    else if (Reader.Region.EU == regionToSet && model.Contains("Astra200"))//convert this both where astra is added
                    {
                        if (slider.Value > 29)
                        {
                            slider.Value = (double)29;
                        }
                    }
                    if (model.Equals("M3e"))
                    {
                        //objReader.ParamSet("/reader/radio/readPower", Convert.ToInt32(100 * slider.Value));
                    }
                    else if (slider.Name.ToLower().Contains("read"))
                    {
                        //MessageBox.Show(objReader.ParamGet("/reader/radio/writePower").ToString() + " " + slider.Value);
                        objReader.ParamSet("/reader/radio/readPower", Convert.ToInt32(100 * slider.Value));
                    }
                    else
                    {
                        objReader.ParamSet("/reader/radio/writePower", Convert.ToInt32(100 * slider.Value));
                        //MessageBox.Show(objReader.ParamGet("/reader/radio/writePower").ToString() + " " + slider.Value);
                    }


                    Mouse.SetCursor(Cursors.Arrow);
                    if ((model.Equals("M6e Micro USBPro")))
                    {
                        if (sldrWritePwr.Value > 20 || sldrReadPwr.Value > 20)
                        {
                            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                warningText = "Please make sure to provide additional DC power source to the reader";
                                DisplayMessageOnStatusBar(warningText, (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff9400")));
                            }));
                        }
                        else
                        {
                            lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                            {
                                GUIturnoffWarning();
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message
                    + " Check supported protocol configurations in Reader's Hardware Guide.",
                    "Unsupported Reader Configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.SetCursor(Cursors.Arrow);
                LoadGen2Settings();
            }
        }

        /// <summary>
        /// Populate the custom gen2 settings, save the settings set and disable the auto settings
        /// in performance tunning section
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkCustomizeGen2Settings_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chkCustomizeGen2Settings.IsChecked)
            {
                gridPanleCustomizeGen2Settings.Visibility = System.Windows.Visibility.Visible;
                xstackPanelCustomizeGen2settings.IsEnabled = true;
                gridGen2PerformanceTuning1.IsEnabled = false;
                LoadGen2Settings();
                settingsScrollviewer.ScrollToVerticalOffset(2000);
            }
            else
            {
                gridPanleCustomizeGen2Settings.Visibility = System.Windows.Visibility.Collapsed;
                xstackPanelCustomizeGen2settings.IsEnabled = false;
                gridGen2PerformanceTuning1.IsEnabled = true;
            }
        }

        /// <summary>
        /// Populate the custom gen2 settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tglBtnDisplayGen2Settings_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)tglBtnDisplayGen2Settings.IsChecked)
            {
                gridPanleCustomizeGen2Settings.Visibility = System.Windows.Visibility.Visible;
                LoadGen2Settings();
                settingsScrollviewer.ScrollToVerticalOffset(1500);
            }
            else
            {
                gridPanleCustomizeGen2Settings.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        #endregion Gen2PerformanceTuning

        #region ReaderDiagnostics
        /// <summary>
        /// Initialize reader diagnostics
        /// </summary>
        private void InitializeRdrDiagnostics()
        {
            try
            {
                // Create a FlowDocument to contain content for the RichTextBox.
                FlowDocument myFlowDoc = new FlowDocument();
                if (lblshowStatus.Content.ToString() == "Disconnected")
                {
                    lblURAVersionContent.Content = this.GetType().Assembly.GetName().Version.ToString();
                    lblMercuryApiVersionContent.Content = Assembly.GetAssembly(typeof(ThingMagic.Reader)).GetName().Version.ToString();
                    lblRFIDEngine.Visibility = lblRFIDEngineContent.Visibility = lblFirmwareVersion.Visibility = lblFirmwareVersionContent.Visibility = lblHardwareVersion.Visibility = lblHardwareVersionContent.Visibility = lblSerialNumber.Visibility = lblSerialNumberContent.Visibility = lblRFIDIPCom.Visibility = lblRFIDIPComContent.Visibility = Visibility.Collapsed;
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("URA Version : " + lblURAVersionContent.Content.ToString())));
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("Mercury API Version : " + lblMercuryApiVersionContent.Content.ToString())));
                }
                else
                {
                    lblRFIDEngineContent.Content = model.ToString();
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("RFID Engine : " + model.ToString())));
                    MatchCollection mc = null;
                    lblRFIDIPComContent.Content = "";
                    if (rdbtnNetworkConnection.IsChecked == true)
                    {
                        lblRFIDIPComContent.Content = cmbFixedReaderAddr.Text.ToUpper();
                        mc = Regex.Matches(cmbFixedReaderAddr.Text, @"(?<=\().+?(?=\))");
                    }
                    else if (rdbtnLocalConnection.IsChecked == true)
                    {
                        lblRFIDIPComContent.Content = cmbReaderAddr.Text.ToUpper();
                        mc = Regex.Matches(cmbReaderAddr.Text, @"(?<=\().+?(?=\))");
                    }
                    else
                    {
                        lblRFIDIPComContent.Content = txtCustomTransport.Text;
                    }
                    if (mc != null)
                    {
                        foreach (Match m in mc)
                        {
                            lblRFIDIPComContent.Content = m.ToString();
                        }
                    }
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("COM/IP : " + lblRFIDIPComContent.Content.ToString())));

                    //Get version information
                    string fwver = (string)objReader.ParamGet("/reader/version/software");
                    if (objReader is RqlReader)
                    {
                        int index = ((string)fwver).LastIndexOf("build");
                        lblFirmwareVersionContent.Content = fwver.Substring(0, index - 1);
                    }
                    else if (objReader is SerialReader)
                    {
                        lblFirmwareVersionContent.Content = fwver.Substring(1, 10);
                    }
                    else if (objReader is LlrpReader)
                    {
                        lblFirmwareVersionContent.Content = fwver;
                    }
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("Firmware Version : " + lblFirmwareVersionContent.Content)));

                    string[] hVersion;
                    string[] serialnumber;
                    if (!(model.Equals("Astra")))
                    {
                        string hardwareVersion = ((string)objReader.ParamGet("/reader/version/hardware"));
                        string SerialNumber = ((string)objReader.ParamGet("/reader/version/serial"));
                        if (hardwareVersion != null)
                        {
                            hVersion = hardwareVersion.Split('-');
                            lblHardwareVersionContent.Content = hVersion[0].ToString();
                        }
                        if (SerialNumber != null)
                        {
                            serialnumber = SerialNumber.Split('-');
                            lblSerialNumberContent.Content = serialnumber[0];
                        }
                        else
                        {
                            lblSerialNumberContent.Content = "";
                        }
                    }
                    else
                    {
                        lblHardwareVersionContent.Content = "-";
                        lblSerialNumberContent.Content = "-";
                    }
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("Hardware Version : " + lblHardwareVersionContent.Content.ToString())));
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("Serial Number : " + lblSerialNumberContent.Content.ToString())));
                    lblURAVersionContent.Content = this.GetType().Assembly.GetName().Version.ToString();
                    lblMercuryApiVersionContent.Content = Assembly.GetAssembly(typeof(ThingMagic.Reader)).GetName().Version.ToString();
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("URA Version : " + lblURAVersionContent.Content.ToString())));
                    myFlowDoc.Blocks.Add(new Paragraph(new Run("Mercury API Version : " + lblMercuryApiVersionContent.Content.ToString())));
                    lblRFIDEngine.Visibility = lblRFIDEngineContent.Visibility = lblFirmwareVersion.Visibility = lblFirmwareVersionContent.Visibility = lblHardwareVersion.Visibility = lblHardwareVersionContent.Visibility = lblSerialNumber.Visibility = lblSerialNumberContent.Visibility = lblRFIDIPCom.Visibility = lblRFIDIPComContent.Visibility = Visibility.Visible;
                }

                richTxtBxRdrDiagnostics.Document = myFlowDoc;
            }
            catch (NullReferenceException ex)
            {
                Onlog(ex);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void btnCopyReaderDiagnostic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Reader Diagnostic :\n");
                sb.Append("RFID Engine : " + lblRFIDEngineContent.Content.ToString() + "\n");
                sb.Append("COM/IP : " + lblRFIDIPComContent.Content.ToString() + "\n");
                sb.Append("Firmware Version : " + lblFirmwareVersionContent.Content.ToString() + "\n");
                sb.Append("Hardware Version : " + lblHardwareVersionContent.Content.ToString() + "\n");
                sb.Append("Serial Number : " + lblSerialNumberContent.Content.ToString() + "\n");
                sb.Append("URA Version : " + lblURAVersionContent.Content.ToString() + "\n");
                sb.Append("Mercury API Version : " + lblMercuryApiVersionContent.Content.ToString() + "\n");
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        #endregion ReaderDiagnostics

        #region FirmwareUpdate

        //progress bar start flag
        bool prgStart = false;
        //progress bar stop flag
        bool prgStop = false;
        // Cache exception if any while upgrading the firmware for future use
        Exception exceptionCrc = null;
        private Thread progressStatus = null;
        // Open file dialog
        OpenFileDialog openFile = new OpenFileDialog();

        /// <summary>
        /// Open file dialog to select firmware for the connected reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnChooseFirmware_Click(object sender, RoutedEventArgs e)
        {
            if (rdbtnFirmwareUpgrade.IsChecked == true)
            {
                openFile.ShowDialog();
                txtFirmwarePath.Text = openFile.FileName.ToString();
            }
            else if (rdbtnLicenceUpgrade.IsChecked == true)
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                //openDialog.Filter = "Excel files (*.xls, *xlsx)|*.xls;*xlsx|All files (*.*)|*.*";
                openDialog.Filter = "csv files (*.csv)|*.csv";
                openDialog.Title = "Select License File";
                openDialog.ShowDialog();
                txtLicencePath.Text = openDialog.FileName.ToString();
            }
        }

        private void DisconnectDisable(bool trig)/// This is for disabling the disconnect button
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                btnConnectExpander.IsEnabled = trig;
                btnConnect.IsEnabled = trig;
            }));

        }
        /// <summary>
        /// Update the firmware/licence on the connected reader
        /// </summary>
        /// <param name="sender"></param>\
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (rdbtnFirmwareUpgrade.IsChecked == true)
                {
                    try
                    {
                        // Condition check for firmware file to be non-null
                        if (txtFirmwarePath.Text == "")
                        {
                            MessageBox.Show("Please select a firmware to load.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        if ((bool)rdbtnLocalConnection.IsChecked || (bool)rdbtnCustomTransportConnection.IsChecked)
                        {
                            if (!txtFirmwarePath.Text.Contains(".sim"))
                            {
                                MessageBox.Show("Invalid File Extension", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                txtFirmwarePath.Text = "";
                                return;
                            }
                        }
                        if ((bool)rdbtnNetworkConnection.IsChecked)
                        {
                            if (!(txtFirmwarePath.Text.Contains(".tmfw") || (txtFirmwarePath.Text.Contains(".deb"))))
                            {
                                MessageBox.Show("Invalid File Extension", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                txtFirmwarePath.Text = "";
                                return;
                            }
                        }

                        //// Clearing the object of the reader needs to happened only if firmware file with correct extension is provided to URA.
                        //if (null != objReader)
                        //{
                        //    objReader.Destroy();
                        //    objReader = null;
                        //}

                        // Set progress bar style to indeterminate 
                        progressBar.IsIndeterminate = true;
                        // Based on the connection type, try connecting to the reader automatically
                        // if user has not connected initially
                        if (null == objReader)
                        {
                            if ((bool)rdbtnLocalConnection.IsChecked)
                            {
                                if (!ValidatePortNumber(cmbReaderAddr.Text))
                                {
                                    throw new IOException();
                                }
                                if (cmbReaderAddr.Text == "")
                                {
                                    throw new IOException();
                                }
                                //Creates a Reader Object for operations on the Reader.
                                string readerUri = cmbReaderAddr.Text;
                                //Regular Expression to get the com port number from comport name .
                                //for Ex: If The Comport name is "USB Serial Port (COM19)" by using this 
                                // regular expression will get com port number as "COM19".
                                MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                                foreach (Match m in mc)
                                {
                                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                                    {
                                        readerUri = m.ToString();
                                    }
                                }
                                objReader = Reader.Create(string.Concat("tmr:///", readerUri));
                            }
                            else if ((bool)rdbtnCustomTransportConnection.IsChecked)
                            {
                                if (!string.IsNullOrWhiteSpace(txtCustomTransport.Text))
                                {
                                    Regex ip = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,4}\b");
                                    if (!ip.IsMatch(txtCustomTransport.Text))
                                    {
                                        throw new IOException();
                                    }
                                }
                                else
                                {
                                    throw new IOException();
                                }
                                string readerUri = txtCustomTransport.Text;
                                objReader = Reader.Create(string.Concat("tcp://", readerUri));
                            }
                            else
                            {
                                string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(cmbFixedReaderAddr.Text)).FirstOrDefault();
                                string readerUri;
                                if (string.IsNullOrWhiteSpace(key) || key == null)
                                    readerUri = cmbFixedReaderAddr.Text;
                                else
                                    readerUri = bonjour.HostNameIpAddress[key];
                                MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                                foreach (Match m in mc)
                                {
                                    if (!string.IsNullOrWhiteSpace(m.ToString()))
                                    {
                                        readerUri = m.ToString();
                                    }
                                }
                                //Creates a Reader Object for operations on the Reader.
                                objReader = Reader.Create(string.Concat("tmr://", readerUri));
                            }
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        if (!cmbReaderAddr.Text.Contains("COM"))
                        {
                            MessageBox.Show("Application needs a valid Reader Address of type COMx",
                                "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (rdbtnCustomTransportConnection.IsChecked == true)
                        {
                            MessageBox.Show("Please type in custom reader in given format.\nFormat :- xxx.xxx.xxx.xxx:xxxx (readerIP:portname).\nEx. 172.16.16.2:5000", "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Reader not connected on " + cmbReaderAddr.Text,
                                "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    try
                    {
                        // connect to the reader
                        //objReader.Connect();
                    }
                    catch (FAULT_BL_INVALID_IMAGE_CRC_Exception ex)
                    {
                        // Ignore INVALID_IMAGE here so we can reach FirmwareLoad --
                        // will be checked later in SendInitCommands, anyway
                        Onlog("FAULT_BL_INVALID_IMAGE_CRC_Exception : " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        if (rdbtnCustomTransportConnection.IsChecked == true)
                        {
                            Onlog(ex);
                        }
                        else
                        {
                            Onlog(ex);
                            MessageBox.Show(ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    if (null != objReader)
                    {
                        // Disable the UI controls when ura is loading the firmware
                        btnUpdate.IsEnabled = false;
                        btnChooseFirmware.IsEnabled = false;
                        tcTagResults.IsEnabled = false;
                        prgStart = false;
                        prgStop = false;
                        DisconnectDisable(false);//for deactivating disconnect
                        // Cache the exception when thrown for future use
                        exceptionCrc = null;
                        // Set progress bar to initial stage
                        progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });
                        System.IO.FileStream firmware = new System.IO.FileStream(txtFirmwarePath.Text,
                            System.IO.FileMode.Open, System.IO.FileAccess.Read);

                        Thread updateProgress = new Thread(delegate()
                        {
                            //Wait till updateTrd thread starts
                            while (!prgStart)
                            {
                                Thread.Sleep(10);
                            }
                            //After updateTrd thread starts, prgStart will be true
                            while (prgStart)
                            {
                                startProgress();
                            }
                            //If no exception received call stopProgress method
                            if (null == exceptionCrc)
                            {
                                stopProgress();
                            }
                        });

                        Thread updateTrd = new Thread(delegate()
                        {
                            try
                            {
                                prgStart = true;
                                Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                    {
                                        stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Visible;
                                        //disable all the expanders when firmware Update is in progress
                                        //gridConnect.IsEnabled = false;
                                        //gridReadOptions.IsEnabled = false;
                                        //gridPerformanceMetrics.IsEnabled = false;
                                        //gridPerformanceTuning.IsEnabled = false;
                                        //gridDisplayOptions.IsEnabled = false;
                                        //gridRdrDiagnostics.IsEnabled = false;
                                        EnableDisableExpanderControl(false);
                                        btnRead.IsEnabled = false;
                                    }));
                                try
                                {
                                    // Load the firmware on to the connected reader
                                    objReader.FirmwareLoad(firmware);
                                }catch(ReaderCommException ex)
                                {
                                    // Baudrate mismatch leads to timeout error
                                    if (ex.Message.Contains("The operation has timed out."))
                                    {
                                        // Probe through the baudrate list and get the current baudrate of the reader
                                        int currentBaudRate = ((SerialReader)objReader).probeBaudRate();

                                        //Set the current baudrate so that next connect will use this baudrate.
                                        objReader.ParamSet("/reader/baudRate", currentBaudRate);

                                        // Load the firmware on to the connected reader
                                        objReader.FirmwareLoad(firmware);
                                    }
                                }

                                prgStart = false;
                                // Update the firmware Update status
                                Dispatcher.Invoke(new del2(updateStatus));
                            }
                            catch (Exception ex)
                            {
                                //Freeze the progress-bar status when an exception is caught 
                                prgStart = false;
                                // Cache the exception thrown
                                exceptionCrc = ex;
                                DisconnectDisable(true);//for activating it
                                prgStop = true;
                                start = false;
                                progressStatus = null;
                                if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception)
                                {
                                    //MessageBox.Show("Firmware Update failed : " + exceptionCrc.Message.ToString(), "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                                    if (isFirmwareUpdateFailed)
                                    {
                                        //Firmware Update failed, disconnect the reader.
                                        Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                        {
                                            btnConnect_Click(this, new RoutedEventArgs());
                                        }));
                                        stopProgress();
                                    }
                                    this.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                    {
                                        progressBar.IsIndeterminate = false;
                                        progressBar.InvalidateVisual();
                                    }));
                                }
                                else
                                {
                                    this.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                    {
                                        stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Collapsed;
                                    }));
                                    if (!exceptionCrc.Message.Contains("Autonomous mode is already enabled on reader"))
                                    {
                                        MessageBox.Show(exceptionCrc.Message.ToString(), "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                    else
                                    {
                                        MessageBox.Show(exceptionCrc.Message.ToString(), "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    }
                                    stopProgress();
                                }
                                updateFailedStatus();
                            }
                            finally
                            {
                                if (isFirmwareUpdateFailed && !exceptionCrc.Message.Contains("Autonomous mode is already enabled on reader"))
                                {
                                    MessageBox.Show("Firmware Update failed : " + exceptionCrc.Message.ToString(), "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                isFirmwareUpdateFailed = false;
                            }
                        });
                        updateProgress.Start();
                        updateTrd.Start();
                    }
                }
                else if (rdbtnLicenceUpgrade.IsChecked == true)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(txtLicencePath.Text))
                        {
                            MessageBox.Show("License Key field cannot be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            //MessageBox.Show("No File selected. Please select a license file.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        if (null != objReader)
                        {
                            objReader.Destroy();
                            objReader = null;
                        }

                        //if (txtLicencePath.Text.Contains(".xls") || txtLicencePath.Text.Contains(".xlsx"))
                        if (rdbtnLicenseSingle.IsChecked != true)
                        {
                            if (!txtLicencePath.Text.Contains(".csv"))
                            {
                                MessageBox.Show("Invalid File Extension. File should be in .csv format.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        progressBar.IsIndeterminate = true;
                        if ((bool)rdbtnLocalConnection.IsChecked)
                        {
                            if (!ValidatePortNumber(cmbReaderAddr.Text))
                                throw new IOException();
                            if (cmbReaderAddr.Text == "")
                                throw new IOException();
                            string readerUri = cmbReaderAddr.Text;
                            MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                            foreach (Match m in mc)
                            {
                                if (!string.IsNullOrWhiteSpace(m.ToString()))
                                    readerUri = m.ToString();
                            }
                            objReader = Reader.Create(string.Concat("tmr:///", readerUri));
                        }
                        else if ((bool)rdbtnCustomTransportConnection.IsChecked)
                        {
                            if (!string.IsNullOrWhiteSpace(txtCustomTransport.Text))
                            {
                                Regex ip = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,4}\b");
                                if (!ip.IsMatch(txtCustomTransport.Text))
                                    throw new IOException();
                            }
                            else
                                throw new IOException();
                            string readerUri = txtCustomTransport.Text;
                            objReader = Reader.Create(string.Concat("tcp://", readerUri));
                        }
                        else
                        {
                            string key = bonjour.HostNameIpAddress.Keys.Where(x => x.Contains(cmbFixedReaderAddr.Text)).FirstOrDefault();
                            string readerUri;
                            if (string.IsNullOrWhiteSpace(key) || key == null)
                                readerUri = cmbFixedReaderAddr.Text;
                            else
                                readerUri = bonjour.HostNameIpAddress[key];
                            MatchCollection mc = Regex.Matches(readerUri, @"(?<=\().+?(?=\))");
                            foreach (Match m in mc)
                            {
                                if (!string.IsNullOrWhiteSpace(m.ToString()))
                                    readerUri = m.ToString();
                            }
                            objReader = Reader.Create(string.Concat("tmr://", readerUri));
                        }

                        if (null != objReader)
                        {
                            DisconnectDisable(false);///for closing the disconnect button
                            btnUpdate.IsEnabled = false;
                            btnChooseFirmware.IsEnabled = false;
                            tcTagResults.IsEnabled = false;
                            prgStart = false;
                            prgStop = false;
                            exceptionCrc = null;
                            progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });

                            prgStart = true;
                            stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Visible;
                            EnableDisableExpanderControl(false);
                            btnRead.IsEnabled = false;
                            licensePathTemp = txtLicencePath.Text;
                            bgwLicenseUpgrade.RunWorkerAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        DisconnectDisable(true);///activating the disconnect on exception.
                        MessageBox.Show(ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                btnUpdate.IsEnabled = true;
                btnChooseFirmware.IsEnabled = true;
                tcTagResults.IsEnabled = true;
                DisconnectDisable(true);///activate disconnnect.
            }
            catch (ReaderCodeException ex)
            {
                MessageBox.Show("Firmware update failed:" + "Error connecting to reader," + ex.Message.ToString(), "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });
                btnUpdate.IsEnabled = true;
                btnChooseFirmware.IsEnabled = true;
                tcTagResults.IsEnabled = true;
                DisconnectDisable(true);///activate disconnnect.
            }
            catch (System.UnauthorizedAccessException)
            {
                MessageBox.Show("Access denied. Please check if another program is accessing this port", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                btnUpdate.IsEnabled = true;
                btnChooseFirmware.IsEnabled = true;
                tcTagResults.IsEnabled = true;
                DisconnectDisable(true);///activate  disconect.
            }
        }

        private void rdbtnFirmwareUpgrade_Checked(object sender, RoutedEventArgs e)
        {
            if (lblSelectedFilePath != null)
            {
                if (rdbtnFirmwareUpgrade.IsChecked == true)
                {
                    lblSelectedFilePath.Visibility = Visibility.Visible;
                    txtFirmwarePath.Visibility = Visibility.Visible;
                    tbllicensetype.Visibility = Visibility.Collapsed;
                    txtLicencePath.Visibility = Visibility.Collapsed;
                    btnChooseFirmware.Visibility = Visibility.Visible;
                }
                else
                {
                    lblSelectedFilePath.Visibility = Visibility.Collapsed;
                    txtFirmwarePath.Visibility = Visibility.Collapsed;
                    tbllicensetype.Visibility = Visibility.Visible;
                    txtLicencePath.Visibility = Visibility.Visible;
                    btnChooseFirmware.Visibility = Visibility.Collapsed;
                }
            }
        }

        bool start;
        /// <summary>
        /// Start the progress bar
        /// </summary>
        private void startProgress()
        {
            start = true;
            if (progressStatus == null)
            {
                this.progressStatus = new Thread(ProgressBarWork);
                this.progressStatus.IsBackground = true;
                this.progressStatus.Start();
            }
        }

        /// <summary>
        /// Stop the progress bar
        /// </summary>
        private void stopProgress()
        {
            start = false;
            if (progressStatus != null)
            {
                progressStatus.Join(100);
                progressStatus = null;
            }
        }

        /// <summary>
        /// Delegate with parameter
        /// </summary>
        /// <param name="x"></param>
        delegate void del1(int x);

        /// <summary>
        /// Delegate without parameter
        /// </summary>
        delegate void del2();

        /// <summary>
        /// Update progress bar
        /// </summary>
        /// <param name="y">value</param>
        void updateProgressBar(int y)
        {
            progressBar.Value = y;
        }

        /// <summary>
        /// Update the firmware Update status if successful
        /// </summary>
        void updateStatus()
        {
            //Update the status to Update successful
            isFirmwareUpdateFailed = false;
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Collapsed;
                    //enable all the expanders when firmware Update is completed
                    //gridConnect.IsEnabled = true;
                    //gridReadOptions.IsEnabled = true;
                    //gridPerformanceMetrics.IsEnabled = true;
                    //gridPerformanceTuning.IsEnabled = true;
                    //gridDisplayOptions.IsEnabled = true;
                    //gridRdrDiagnostics.IsEnabled = true;
                    EnableDisableExpanderControl(true);
                    btnRead.IsEnabled = true;
                }));
            progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });
            MessageBox.Show("Firmware Update Successful:" + "Please Restart Reader and Reconnect to Universal Reader Assistant ",
               "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
            //txtFirmwarePath.Text = "";
            btnUpdate.IsEnabled = true;
            btnChooseFirmware.IsEnabled = true;
            tcTagResults.IsEnabled = true;
            //Firmware Update failed, disconnect the reader.
            this.Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                btnConnect.Content = "Disconnect";
                DisconnectDisable(true);//to activate disconnect button
                btnConnect_Click(this, new RoutedEventArgs());
                objReader = null;
            }));
        }

        /// <summary>
        /// Enable or disable expander ui controls
        /// </summary>
        /// <param name="flag"></param>
        private void EnableDisableExpanderControl(bool flag)
        {
            gridConnect.IsEnabled = flag;
            gridReadOptions.IsEnabled = flag;
            gridPerformanceMetrics.IsEnabled = flag;
            gridPerformanceTuning.IsEnabled = flag;
            gridDisplayOptions.IsEnabled = flag;
            cbxcolumnSelection.IsEnabled = flag;
            gridRdrDiagnostics.IsEnabled = flag;
            gridRegulatoryTesting.IsEnabled = flag;
        }

        /// <summary>
        /// Update the firmware Update status if failed
        /// </summary>
        void updateFailedStatus()
        {
            Dispatcher.Invoke(new ThreadStart(delegate()
            {
                //Update the status to Update failed
                progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });
                Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Collapsed;
                    //enable connect expander when firmware Update is failed. So that user can 
                    // connect to another reader
                    gridConnect.IsEnabled = true;
                }));
                isFirmwareUpdateFailed = true;
                btnUpdate.IsEnabled = true;
                btnChooseFirmware.IsEnabled = true;
                tcTagResults.IsEnabled = true;
                //if (null != objReader)
                //{
                //    objReader.Destroy();
                //    objReader = null;
                //}
                //Firmware Update failed, disconnect the reader.
                this.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    btnConnect.Content = "Disconnect";
                    DisconnectDisable(true);//to activate the disconnect button
                    btnConnect_Click(this, new RoutedEventArgs());
                    objReader = null;
                }));
            }));
        }

        /// <summary>
        /// Progress bar implementation
        /// </summary>
        private void ProgressBarWork()
        {
            Thread.Sleep(1000);
            int i = 0;
            while (i < 101)
            {
                progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { i });
                //start = true, when firmware Update started.
                if (start)
                {
                    //If no exception caught, prgStop = false
                    if (!prgStop)
                    {
                        i++;
                    }
                }
                else
                {
                    if (prgStop)
                    {
                        //If exception caught, prgStop = true, come out of the while loop,
                        //firmware Update failed
                        break;
                    }
                    else
                    {
                        //start = false and prgStop = false, when firmware Update done
                        //successfully.
                        i = 102;
                        double maxvalue = 0;
                        progressBar.Dispatcher.Invoke(new ThreadStart(delegate()
                        {
                            maxvalue = progressBar.Maximum;
                        }));
                        progressBar.Dispatcher.Invoke(new del1(updateProgressBar),
                            new object[] { Convert.ToInt32(maxvalue) });
                    }
                }
                Thread.Sleep(350);
            }
        }

        #endregion

        #region License Upgrade

        void bgwLicenseUpgrade_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                licensetoupdate = new Dictionary<string, string>();
                licenseUpgradesSuccessfull = new List<string>();
                licenseUpgradeFailed = new List<string>();

                connectToReader(objReader);
                if (((string)objReader.ParamGet("/reader/version/serial")) != null)
                {
                    string[] serialnumber;
                    serialnumber = ((string)objReader.ParamGet("/reader/version/serial")).Split('-');
                    licenseUpgradeModuleSerialNumber = serialnumber[0];
                }
                moduleName = objReader.ParamGet("/reader/version/model").ToString();

                bool? isrdbtnLicenseSingleChecked = true;
                rdbtnLicenseSingle.Dispatcher.Invoke(new Action(
                    () => isrdbtnLicenseSingleChecked = rdbtnLicenseSingle.IsChecked));

                string licensekey = "";
                txtLicencePath.Dispatcher.Invoke(new Action(() => licensekey = txtLicencePath.Text));

                if (isrdbtnLicenseSingleChecked == true)
                {
                    try
                    {
                        List<string> licenseList = new List<string>();
                        List<byte> licensebyteList = new List<byte>();

                        licenseList.AddRange(licensekey.Trim().Split(' '));
                        foreach (string licensebit in licenseList)
                        {
                            licensebyteList.Add(Convert.ToByte(licensebit, 16));
                        }

                        //Set the license key
                        objReader.ParamSet("/reader/licensekey", licensebyteList.ToArray());
                        licenseUpgradesSuccessfull.Add("License key Upgrade successful for \nModule : " + moduleName + "\nSerial Number : " + licenseUpgradeModuleSerialNumber.ToUpper().ToString());
                        licenseUpgradeFailed.Clear();
                        // here is the roboot 
                        objReader.Reboot();
                        objReader.Destroy();
                        DisconnectDisable(true);
                    }
                    catch //(Exception ex)
                    {
                        licenseUpgradesSuccessfull.Clear();
                        licenseUpgradeFailed.Add("License key Upgrade failed for \nModule : " + moduleName + "\nSerial Number : " + licenseUpgradeModuleSerialNumber.ToUpper().ToString() + "\nError : " + "Incorrect license key format.");
                    }
                }
                else
                {
                    try
                    {
                        DataTable dt = new DataTable();
                        int rowCount = 0;
                        string[] columnNames = null;
                        string[] streamDataValues = null;
                        Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
                        StreamReader reader = new StreamReader(licensePathTemp);

                        while (!reader.EndOfStream)
                        {
                            string readData = reader.ReadLine().Trim();

                            if (readData.Length > 0)
                            {
                                streamDataValues = CSVParser.Split(readData);
                                if (rowCount == 0)
                                {
                                    rowCount = 1;
                                    columnNames = streamDataValues;
                                    if (columnNames.Length != 5)
                                        throw new Exception("Invalid File content");

                                    if ((columnNames[0].ToUpper() != "CUSTOMER ID") || (columnNames[1].ToUpper() != "PRODUCT") || (columnNames[2].ToUpper() != "SERIAL NUMBER") || (columnNames[3].ToUpper() != "FEATURES") || (columnNames[4].ToUpper() != "KEY"))
                                        throw new Exception("Invalid File content");


                                    foreach (string csvHeader in columnNames)
                                    {
                                        DataColumn dc = new DataColumn(csvHeader.ToUpper(), typeof(string));
                                        dc.DefaultValue = string.Empty;
                                        dt.Columns.Add(dc);
                                    }
                                }
                                else
                                {
                                    DataRow dr = dt.NewRow();
                                    for (int i = 0; i < columnNames.Length; i++)
                                    {
                                        dr[columnNames[i]] = streamDataValues[i] == null ? string.Empty : streamDataValues[i].ToString();
                                    }
                                    dt.Rows.Add(dr);
                                }
                            }
                        }

                        reader.Close();
                        reader.Dispose();

                        foreach (DataRow dr in dt.Rows)
                        {
                            if (licenseUpgradeModuleSerialNumber.ToUpper().ToString().Equals(dr[2].ToString().ToUpper()))
                            {
                                //moduleName = dr[1].ToString();
                                if (!string.IsNullOrWhiteSpace(dr[4].ToString().Trim()))
                                    licensetoupdate.Add(dr[4].ToString(), dr[3].ToString());
                                else
                                    licenseUpgradeFailed.Add(dr[3].ToString() + " - " + "License key not present for this feature in uploaded file.");
                            }
                        }

                        if (licensetoupdate.Count > 0)
                        {
                            foreach (string license in licensetoupdate.Keys)
                            {
                                try
                                {
                                    List<string> licenseList = new List<string>();
                                    List<byte> licensebyteList = new List<byte>();

                                    licenseList.AddRange(license.Trim().Split(' '));
                                    foreach (string licensebit in licenseList)
                                    {
                                        licensebyteList.Add(Convert.ToByte(licensebit, 16));
                                    }

                                    //Set the license key
                                    objReader.ParamSet("/reader/licensekey", licensebyteList.ToArray());
                                    licenseUpgradesSuccessfull.Add(licensetoupdate[license].ToString());
                                }
                                catch (Exception ex)
                                {
                                    licenseUpgradeFailed.Add(licensetoupdate[license].ToString() + " - " + ex.Message);
                                }
                                break;
                            }
                        }
                        else
                        {
                            licenseUpgradeFailed.Clear();
                            MessageBox.Show("No license key found for \nModule : " + moduleName + "\nSerial Number : " + licenseUpgradeModuleSerialNumber.ToUpper().ToString(), "License Upgrade Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        licenseUpgradeFailed.Clear();
                        MessageBox.Show(ex.Message + "\nPlease check if the file content is in correct format.", "License Upgrade Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nUnable to Connect to module. Please try the license upgrade again.", "License Upgrade Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void bgwLicenseUpgrade_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stackPanelFrmwrUpdatePrgss.Visibility = System.Windows.Visibility.Collapsed;
            EnableDisableExpanderControl(true);
            btnRead.IsEnabled = true;
            progressBar.Dispatcher.Invoke(new del1(updateProgressBar), new object[] { 0 });
            btnUpdate.IsEnabled = true;
            btnChooseFirmware.IsEnabled = true;
            tcTagResults.IsEnabled = true;
            btnConnect.Content = "Disconnect";
            btnConnect_Click(this, new RoutedEventArgs());
            objReader = null;
            prgStart = false;
            prgStop = true;
            start = false;
            progressStatus = null;
            stopProgress();

            if (rdbtnLicenseSingle.IsChecked == true)
            {
                if (licenseUpgradesSuccessfull != null && licenseUpgradesSuccessfull.Count > 0)
                {
                    txtLicencePath.Text = "";
                    MessageBox.Show(licenseUpgradesSuccessfull[0], "License Upgrade Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                else if (licenseUpgradeFailed != null && licenseUpgradeFailed.Count > 0)
                    MessageBox.Show(licenseUpgradeFailed[0], "License Upgrade Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("License Upgrade Failed. Please try license upgrade once again with a valid key", "License Upgrade Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (licenseUpgradesSuccessfull != null && licenseUpgradesSuccessfull.Count > 0)
                {
                    foreach (string str in licenseUpgradesSuccessfull)
                    {
                        if (!string.IsNullOrWhiteSpace(str))
                            LicenseStatus += str + ", ";
                    }
                    if (!string.IsNullOrWhiteSpace(LicenseStatus))
                    {
                        LicenseStatus = LicenseStatus.TrimEnd().Substring(0, LicenseStatus.LastIndexOf(','));
                        LicenseStatus += " have been added to " + moduleName + " (Serial Number : " + licenseUpgradeModuleSerialNumber + ").";
                    }
                    LicenseStatus = "License Upgrade successful.\n" + LicenseStatus;
                    txtLicencePath.Text = "";
                    MessageBox.Show(LicenseStatus, "License Update Status", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (licenseUpgradeFailed != null && licenseUpgradeFailed.Count > 0)
                {
                    LicenseStatus = "License Upgrade failed for " + moduleName + " (Serial Number : " + licenseUpgradeModuleSerialNumber + ").\n";
                    foreach (string str in licenseUpgradeFailed)
                    {
                        if (str.Contains("invalid License key"))
                            LicenseStatus += "Please provide a valid license key.";
                        else
                            LicenseStatus += str;
                    }
                    MessageBox.Show(LicenseStatus, "License Upgrade Status", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                LicenseStatus = "";
            }

            licensetoupdate = null;
            licenseUpgradesSuccessfull = null;
            licenseUpgradeFailed = null;
        }

        #endregion

        /// <summary>
        /// Delegate for updating Read Tag ID text box
        /// </summary>
        /// <param name="data">String Data</param>
        delegate void OutputUpdateDelegate(TagReadData[] griddata);

        /// <summary>
        /// Class to sort com port serially
        /// </summary>
        class OrderNumeric : IComparer<string>
        {
            public int Compare(string strA, string strB)
            {
                int idA = int.Parse(strA.Substring(3)),
                idB = int.Parse(strB.Substring(3));

                return idA.CompareTo(idB);
            }
        }
        public uint startAddressToUC = 0;
        public int readLengthToUC = 0;
        public Gen2.Bank memBankToUC = Gen2.Bank.EPC;

        /// <summary>
        /// Cache reader settings such as start address, read length, gen2 memory bank
        /// </summary>
        private void CacheReadDataSettings()
        {

            if (!((bool)chkEmbeddedReadData.IsChecked))
            {
                startAddressToUC = 0;
                readLengthToUC = 0;
                memBankToUC = Gen2.Bank.EPC;
            }
            else
            {
                startAddressToUC = Convert.ToUInt32(Utilities.CheckHexOrDecimal(txtembReadStartAddr.Text));
                readLengthToUC = Convert.ToInt32(Utilities.CheckHexOrDecimal(txtembReadLength.Text));
                switch (cbxReadDataBank.Text)
                {
                    case "EPC":
                        memBankToUC = Gen2.Bank.EPC;
                        break;
                    case "TID":
                        memBankToUC = Gen2.Bank.TID;
                        break;
                    case "User":
                        memBankToUC = Gen2.Bank.USER;
                        break;
                    case "Reserved":
                        break;
                };
            }
        }

        /// <summary>
        /// Context menu options i.e list of tabs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TagReadRecord tagRead = (TagReadRecord)TagResults.dgTagResults.SelectedItem;
            ctMenu.Visibility = System.Windows.Visibility.Collapsed;
            //expdrReadOptions.IsEnabled = false;
            btnRead.IsEnabled = false;
            string menuItem = ((System.Windows.Controls.MenuItem)sender).Header.ToString();
            switch (menuItem)
            {
                case "Write EPC":
                    tiWriteEPC.Focus();
                    tiWriteEPC.Visibility = System.Windows.Visibility.Visible;
                    WriteEpc.Load(objReader, startAddressToUC, readLengthToUC, memBankToUC, tagRead);
                    break;

                case "Inspect Tag":
                    tiTagInspector.Focus();
                    tiTagInspector.Visibility = System.Windows.Visibility.Visible;
                    TagInspector.LoadTagInspector(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;

                case "Edit User Memory":
                    tiUserMemory.Focus();
                    tiUserMemory.Visibility = System.Windows.Visibility.Visible;
                    UserMemory.LoadUserMemory(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;

                case "Edit Tag Memory":
                    tiUserMemory.Focus();
                    tiUserMemory.Visibility = System.Windows.Visibility.Visible;
                    UserMemory.LoadUserMemory(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;

                case "Lock Tag":
                    tiLockTag.Focus();
                    tiLockTag.Visibility = System.Windows.Visibility.Visible;
                    LockTag.LoadReservedMemory(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;
                case "Untraceable":
                    tiUntraceable.Focus();
                    tiUntraceable.Visibility = Visibility.Visible;
                    Untraceable.LoadUntraceableMemory(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;
                case "Authenticate":
                    tiAuthenticate.Focus();
                    tiUntraceable.Visibility = Visibility.Visible;
                    Authenticate.LoadAuthenticateMemory(objReader, startAddressToUC, memBankToUC, tagRead, model);
                    break;
            }
        }

        /// <summary>
        /// Tag results tab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiTagResults_GotFocus(object sender, RoutedEventArgs e)
        {
            expdrReadOptions.IsEnabled = true;
            // Don't enable the read button, when sync read or async read is in progress
            if (!(isSyncReadGoingOn || isAsyncReadGoingOn))
            {
                btnRead.IsEnabled = true;
            }
            //Disable clear tag results button on tool-bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = true;
            saveData.IsEnabled = true;
            gen2CheckBox.IsEnabled = true;
            iso6bCheckBox.IsEnabled = true;
            ipx64CheckBox.IsEnabled = true;
            ipx256CheckBox.IsEnabled = true;
            ataCheckBox.IsEnabled = true;
            isoUcodeCheckbox.IsEnabled = true;

            //Set the default values of WriteEPC tab
            WriteEpc.ResetWriteEPCTab();

            //Set the default values of TagInspector tab
            TagInspector.ResetTagInspectorTab();

            //set the default values for UserMemory tab
            UserMemory.ResetUserMemoryTab();

            //set the default values for LockTag tab
            LockTag.ResetLockTagTab();

            //set default values for Untraceable tab
            Untraceable.ResetUntraceableTab();

            //set default values for Authenticate tab
            Authenticate.ResetAuthenticateTab();
        }

        /// <summary>
        /// Write EPC tab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiWriteEPC_GotFocus(object sender, RoutedEventArgs e)
        {
            WriteEpc.LoadEPC(objReader);
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                return;
            }
            if (!IsGen2ProtocolChecked(false))
            {
                return;
            }
            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                WriteEpc.spWriteEPC.IsEnabled = true;
            }
            btnRead.IsEnabled = false;

            //Disable clear tag results button and save on tool bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = false;
            gen2CheckBox.IsEnabled = false;
            iso6bCheckBox.IsEnabled = false;
            ipx64CheckBox.IsEnabled = false;
            ipx256CheckBox.IsEnabled = false;
            ataCheckBox.IsEnabled = false;
            isoUcodeCheckbox.IsEnabled = false;
            TagResults.dgTagResults.UnselectAll();
        }

        /// <summary>
        /// Initialize tag inspector tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiTagInspector_GotFocus(object sender, RoutedEventArgs e)
        {
            var triggerToSkip = e.Source.GetType();
            if (triggerToSkip.Name == "ScrollViewer")
            {
                e.Handled = true;
                return;
            }
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                e.Handled = true;
                return;
            }
            if (model.Equals("M3e"))
            {
                if (!IsM3eProtocolChecked(true))
                {
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                if (!IsGen2ProtocolChecked(true))
                {
                    e.Handled = true;
                    return;
                }
            }
            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                TagInspector.spTagInspector.IsEnabled = true;
            }
            btnRead.IsEnabled = false;
            //Disable clear tag results button on toolbar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = true;
            TagResults.dgTagResults.UnselectAll();
            TagInspector.LoadTagInspector(objReader, model);
            e.Handled = true;
        }

        /// <summary>
        /// Initialize user memory tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiUserMemory_GotFocus(object sender, RoutedEventArgs e)
        {
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                return;
            }
            if (model.Equals("M3e"))
            {
                UserMemory.stkGen2UserMemory.Visibility = Visibility.Collapsed;
                UserMemory.stkM3eUserMemory.Visibility = Visibility.Visible;
                UserMemory.lblUHFNote.Visibility = Visibility.Collapsed;
                UserMemory.rbASCII.IsEnabled = true;

            }
            else
            {
                UserMemory.stkGen2UserMemory.Visibility = Visibility.Visible;
                UserMemory.stkM3eUserMemory.Visibility = Visibility.Collapsed;
                UserMemory.lblUHFNote.Visibility = Visibility.Visible;
                UserMemory.rbASCII.IsEnabled = true;
                if (!IsGen2ProtocolChecked(false))
                {
                    return;
                }
            }
            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                UserMemory.spUserMemory.IsEnabled = true;
            }
            btnRead.IsEnabled = false;
            //Disable clear tag results button on tool-bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = false;
            gen2CheckBox.IsEnabled = false;
            iso6bCheckBox.IsEnabled = false;
            ipx64CheckBox.IsEnabled = false;
            ipx256CheckBox.IsEnabled = false;
            ataCheckBox.IsEnabled = false;
            isoUcodeCheckbox.IsEnabled = false;
            TagResults.dgTagResults.UnselectAll();
            UserMemory.LoadUserMemory(objReader, model);
        }

        /// <summary>
        /// Lock tag tab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiLockTag_GotFocus(object sender, RoutedEventArgs e)
        {
            LockTag.LoadReservedMemory(objReader, model);
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                return;
            }
            if (!IsGen2ProtocolChecked(false))
            {
                return;
            }
            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                LockTag.spLockTag.IsEnabled = true;
            }
            btnRead.IsEnabled = false;
            //Disable clear tag results button on tool-bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = false;
            gen2CheckBox.IsEnabled = false;
            iso6bCheckBox.IsEnabled = false;
            ipx64CheckBox.IsEnabled = false;
            ipx256CheckBox.IsEnabled = false;
            ataCheckBox.IsEnabled = false;
            isoUcodeCheckbox.IsEnabled = false;
            TagResults.dgTagResults.UnselectAll();
        }

        /// <summary>
        /// Untraceable tab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiUntraceable_GotFocus(object sender, RoutedEventArgs e)
        {
            Untraceable.LoadUntraceableMemory(objReader, model);
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                return;
            }
            if (!IsGen2ProtocolChecked(false))
            {
                return;
            }
            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                Untraceable.spUntraceable.IsEnabled = true;
            }
            btnRead.IsEnabled = false;
            //Disable clear tag results button on tool-bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = false;
            gen2CheckBox.IsEnabled = false;
            iso6bCheckBox.IsEnabled = false;
            ipx64CheckBox.IsEnabled = false;
            ipx256CheckBox.IsEnabled = false;
            ataCheckBox.IsEnabled = false;
            isoUcodeCheckbox.IsEnabled = false;
            TagResults.dgTagResults.UnselectAll();
        }

        /// <summary>
        /// Authenticate tab initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tiAuthenticate_GotFocus(object sender, RoutedEventArgs e)
        {
            Authenticate.LoadAuthenticateMemory(objReader, model);
            if (lblshowStatus.Content.ToString() == "Reading")
            {
                tiTagResults.Focus();
                return;
            }
            if (!IsGen2ProtocolChecked(false))
            {
                return;
            }

            if (btnRead.Visibility == System.Windows.Visibility.Visible)
            {
                Authenticate.spAuthenticate.IsEnabled = true;
            }
            btnRead.IsEnabled = false;
            //Disable clear tag results button on tool-bar when tabs other then Tag Results is clicked
            btnClearTagReads.IsEnabled = false;
            saveData.IsEnabled = false;
            gen2CheckBox.IsEnabled = false;
            iso6bCheckBox.IsEnabled = false;
            ipx64CheckBox.IsEnabled = false;
            ipx256CheckBox.IsEnabled = false;
            ataCheckBox.IsEnabled = false;
            isoUcodeCheckbox.IsEnabled = false;
            TagResults.dgTagResults.UnselectAll();
        }

        /// <summary>
        /// Check if supported M3e Protocol is checked or not.
        /// </summary>
        /// <param name="IsTagInspect">Is control comming from Tag inspector</param>
        /// <returns></returns>
        private bool IsM3eProtocolChecked(bool IsTagInspect)//This function needs to be updated as we add protocols to Tag Inspector.
        {
            if (iso14443aCheckBox.IsChecked==false&&iso15693CheckBox.IsChecked==false)
            {
                tiTagResults.Focus();
                if ((iso14443aCheckBox.Visibility==Visibility.Visible||iso15693CheckBox.Visibility==Visibility.Visible)&&IsTagInspect)
                {
                    if ((bool)iso14443bCheckBox.IsChecked||(bool)iso18092CheckBox.IsChecked||(bool)felicaCheckBox.IsChecked||(bool)iso180003mode3CheckBox.IsChecked)
                    {
                        MessageBox.Show("Please select HF14443A/HF15693 protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Please select Protocol.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    if (objReader == null)
                    {
                        MessageBox.Show("Please connect reader.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                        expdrConnect.IsExpanded = true;
                        expdrConnect.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Please select HF14443A/HF15693 protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                        expdrReadOptions.IsExpanded = true;
                        expdrReadOptions.Focus();
                        if (expdrConnect.IsExpanded)
                        {
                            expdrConnect.IsExpanded = false;
                        }
                    }
                }
                return false;
            }
            if ((iso14443aCheckBox.IsChecked == false || iso15693CheckBox.IsChecked == false) && !IsTagInspect)
            {
                tiTagResults.Focus();
                expdrReadOptions.IsExpanded = true;
                expdrReadOptions.Focus();
                if (expdrConnect.IsExpanded)
                {
                    expdrConnect.IsExpanded = false;
                }
                MessageBox.Show("Please select HF14443A/HF15693 protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if GEN2 Tag Protocol is checked or not
        /// </summary>
        /// <returns></returns>
        private bool IsGen2ProtocolChecked(bool IsTagInspect)
        {
            if (gen2CheckBox.IsChecked == false && ataCheckBox.IsChecked == false)
            {
                tiTagResults.Focus();
                if (ataCheckBox.Visibility == Visibility.Visible && IsTagInspect)
                {
                    if ((bool)iso6bCheckBox.IsChecked || (bool)isoUcodeCheckbox.IsChecked || (bool)ipx64CheckBox.IsChecked || (bool)ipx256CheckBox.IsChecked)
                    {
                        MessageBox.Show("Please select GEN2/ATA protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show("Please select Protocol.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    if (objReader == null)
                    {
                        MessageBox.Show("Please connect reader.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                        expdrConnect.IsExpanded = true;
                        expdrConnect.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Please select GEN2 protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                        expdrReadOptions.IsExpanded = true;
                        expdrReadOptions.Focus();
                        if (expdrConnect.IsExpanded)
                        {
                            expdrConnect.IsExpanded = false;
                        }
                    }
                }
                return false;
            }

            if (gen2CheckBox.IsChecked != true && !IsTagInspect)
            {
                tiTagResults.Focus();
                expdrReadOptions.IsExpanded = true;
                expdrReadOptions.Focus();
                if (expdrConnect.IsExpanded)
                {
                    expdrConnect.IsExpanded = false;
                }
                MessageBox.Show("Please select GEN2 protocol. Other protocols are not supported.", "Error : Universal Reader Assitant", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Open connect expander
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expdrConnect_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btnConnect.Content.ToString().Equals("Connect"))
                {
                    InitializeReaderUriBox();
                    btnConnectExpander.Content = "Connect...";
                }
                else
                {
                    btnConnectExpander.Content = btnConnect.Content.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Optimize estimated number of tags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdbtnOptmzExtmdNoTagsInField_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != objReader)
                {
                    stkpnlOptmzExtmdNoTagsInField.IsEnabled = true;
                    //set q value
                    if ((bool)rdbtnOptmzExtmdNoTagsInField.IsChecked)
                    {
                        int tag_pop_adjust_num_tags = Convert.ToInt32(txtTagsNum.Text.ToString());
                        double num_tags_float = tag_pop_adjust_num_tags * 1.5;
                        int count = 0;
                        double num = 1.0;
                        for (; ; )
                        {
                            num = num * 2.0;
                            if (num > num_tags_float)
                                break;
                            count++;
                        }
                        if (num_tags_float > 32768)
                        {
                            count = 15;
                        }
                        if (OptimalReaderSettings["/reader/gen2/q"].Equals("StaticQ"))
                            Gen2SettingChanged["Q"] = true;
                        OptimalReaderSettings["/reader/gen2/q"] = "StaticQ";
                        xStaticQ.IsChecked = true;
                        OptimalReaderSettings["/application/performanceTuning/staticQValue"] = count.ToString();
                        xQvalue.SelectedIndex = count;
                        rdBtnSlBstChforPoplSize_Checked(sender, e);
                        SetGen2ReaderSettings();
                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (null != objReader)
                {
                    LoadGen2Settings();
                }
            }
        }

        /// <summary>
        /// Initialize unique by data ui controls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkUniqueByData_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                tagdb.chkbxUniqueByData = (bool)chkUniqueByData.IsChecked;
                tagdb.chkbxShowFailedDataReads = (bool)chkShowFailedDataReads.IsChecked;
                if (!((bool)chkUniqueByData.IsChecked))
                {
                    chkShowFailedDataReads.IsChecked = chkUniqueByData.IsChecked;
                }
            }
        }

        /// <summary>
        /// Allow only hex string in the textbox
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
        }


        /// <summary>
        /// Sets the scroll bar in setting section when specific expander is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expdrPerformanceTuning_Expanded(object sender, RoutedEventArgs e)
        {
            // Return the offset vector for the TextBlock object.
            Vector vector = VisualTreeHelper.GetOffset(((UIElement)sender));

            // Convert the vector to a point value.
            Point currentPoint = new Point(vector.X, vector.Y);
            settingsScrollviewer.ScrollToVerticalOffset(vector.Y);
        }

        /// <summary>
        /// Enable or disable epc format column in tag results tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxDisplayEPCAs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != cbxDisplayEPCAs)
            {
                try
                {
                    if (cbxDisplayEPCAs.SelectedItem == null)
                    {
                        this.cbxDisplayEPCAs.SelectedIndex = 0;
                    }
                    string text = ((ComboBoxItem)cbxDisplayEPCAs.SelectedItem).Content.ToString();
                    switch (text)
                    {
                        case "ASCII":
                            TagResults.epcColumnInAscii.Visibility = System.Windows.Visibility.Visible;
                            TagResults.epcColumnInReverseBase36.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Select":
                            TagResults.epcColumnInAscii.Visibility = System.Windows.Visibility.Collapsed;
                            TagResults.epcColumnInReverseBase36.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "ReverseBase36":
                            TagResults.epcColumnInAscii.Visibility = System.Windows.Visibility.Collapsed;
                            TagResults.epcColumnInReverseBase36.Visibility = System.Windows.Visibility.Visible;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbxDisplayEPCAs.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Enable or disable embedded read data column in tag results tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxDisplayEmbRdDataAs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (null != cbxDisplayEmbRdDataAs)
            {
                try
                {
                    string text = ((ComboBoxItem)cbxDisplayEmbRdDataAs.SelectedItem).Content.ToString();
                    switch (text)
                    {
                        case "ASCII":
                            TagResults.dataColumnInAscii.Visibility = System.Windows.Visibility.Visible;
                            //TagResults.dataColumnReverseBase36.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case "Select":
                        case "Hex":
                            TagResults.dataColumnInAscii.Visibility = System.Windows.Visibility.Collapsed;
                            //TagResults.dataColumnReverseBase36.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        //case "ReverseBase36":
                        //    TagResults.dataColumnInAscii.Visibility = System.Windows.Visibility.Collapsed;
                        //    TagResults.dataColumnReverseBase36.Visibility = System.Windows.Visibility.Visible;
                        //    break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbxDisplayEmbRdDataAs.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Validate rf on time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtRFOnTimeout_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtRFOnTimeout.Text == "")
            {
                MessageBox.Show("RF On (ms) can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtRFOnTimeout.Text = "1000";
            }
        }

        /// <summary>
        /// Validate rf off time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtRFOffTimeout_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtRFOffTimeout.Text == "")
            {
                MessageBox.Show("RF Off (ms) can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtRFOffTimeout.Text = "0";
            }
        }



        /// <summary>
        /// Uncheck all check boxes present inside enable embedded read data group box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkEmbeddedReadData_Unchecked(object sender, RoutedEventArgs e)
        {
            chkUniqueByData.IsChecked = chkEmbeddedReadData.IsChecked;

            // remove data column when enable embedded read data is unchecked
            cbxDisplayEmbRdDataAs.SelectedIndex = 0;
        }


        /// <summary>
        /// Validate starting address in embedded read data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtembReadStartAddr_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (model.Equals("M3e"))
            {
                if (txtBlockAddress.Text == "")
                {
                    MessageBox.Show("Embedded Read Data: Starting Block Address to read from can't be empty.",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtBlockAddress.Text = "0";
                }
                try
                {
                    uint valueHex = Utilities.CheckHexOrDecimal(txtBlockAddress.Text);
                    if ((objReader is SerialReader) && (valueHex > 0xFFFF))
                    {
                        MessageBox.Show("Embedded Read Data: Starting Block Address can't be more then 0xFFFF",
                            "Universal Reader Assistant Message",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtBlockAddress.Text = "0";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Embedded Read Data: Starting Block Address, " + ex.Message + " If hex, prefix with 0x",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtBlockAddress.Text = "0";
                }
            }
            else
            {
                if (txtembReadStartAddr.Text == "")
                {
                    MessageBox.Show("Embedded Read Data: Starting Word Address to read from can't be empty.",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtembReadStartAddr.Text = "0";
                }
                try
                {
                    uint valueHex = Utilities.CheckHexOrDecimal(txtembReadStartAddr.Text);
                    if ((objReader is SerialReader) && (valueHex > 0xFFFF))
                    {
                        MessageBox.Show("Embedded Read Data: Starting Word Address can't be more then 0xFFFF",
                            "Universal Reader Assistant Message",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtembReadStartAddr.Text = "0";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Embedded Read Data: Starting Word Address, " + ex.Message + " If hex, prefix with 0x",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtembReadStartAddr.Text = "0";
                }
            }
        }

        /// <summary>
        /// Validate number of words to read in embedded read data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtembReadLength_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (model.Equals("M3e"))
            {
                if (txtReadLength.Text == "")
                {
                    MessageBox.Show("Embedded Read Data: Number of Blocks to read can't be empty.",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtReadLength.Text = "0";
                }
                try
                {
                    uint valueHex = Utilities.CheckHexOrDecimal(txtReadLength.Text);
                    if (valueHex > 100)
                    {
                        MessageBox.Show("Embedded Read Data: Number of Blocks can't be more then 0xffff words,it can be "
                            + "read by incrementing start address and length of target read data",
                            "Universal Reader Assistant Message",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtReadLength.Text = "0";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Embedded Read Data: Number of Blocks, " + ex.Message + " If hex, prefix with 0x",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtReadLength.Text = "0";
                }
            }
            else
            {
                if (txtembReadLength.Text == "")
                {
                    MessageBox.Show("Embedded Read Data: Number of Words to read can't be empty.",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtembReadLength.Text = "0";
                }
                try
                {
                    uint valueHex = Utilities.CheckHexOrDecimal(txtembReadLength.Text);
                    if (valueHex > 100)
                    {
                        MessageBox.Show("Embedded Read Data: Number of words can't be more then 0x64 words,it can be "
                            + "read by incrementing start address and length of target read data",
                            "Universal Reader Assistant Message",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        txtembReadLength.Text = "0";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Embedded Read Data: Number of Words, " + ex.Message + " If hex, prefix with 0x",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtembReadLength.Text = "0";
                }
            }
        }

        /// <summary>
        /// Condition to check textbox to have only double values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxValuedBm_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.DoubleCharChecker(e.Text);
            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// Validate read power and throw exception if not a valid read power
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxValuedBm_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                TextBox txt = (TextBox)sender;
                if (txt.Text != "")
                {
                    if (model.Equals("M3e"))
                    {
                        // Power slider is not supported for M3e.
                    }
                    else if ((Convert.ToDouble(txt.Text) < sldrReadPwr.Minimum) || (Convert.ToDouble(txt.Text) > sldrReadPwr.Maximum))
                    {
                        MessageBox.Show("Please enter power within " + sldrReadPwr.Minimum
                            + " and " + sldrReadPwr.Maximum + " dBm", "Universal Reader Assistant Message",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        if (((TextBox)sender).Name.Contains("txtbxValuedBm"))
                        {
                            txtbxValuedBm.Text = sldrReadPwr.Maximum.ToString();
                        }
                        else if (((TextBox)sender).Name.Contains("txtbxHFLFValuedBm"))
                        {
                            txtbxHFLFValuedBm.Text = sldrHFLFPwr.Maximum.ToString();
                        }
                        else
                        {
                            txtbxWriteValuedBm.Text = sldrWritePwr.Maximum.ToString();
                        }
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Power (dBm) can't be empty", "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    if (((TextBox)sender).Name.Contains("txtbxValuedBm"))
                    {
                        txtbxValuedBm.Text = sldrReadPwr.Maximum.ToString();
                    }
                    else if (((TextBox)sender).Name.Contains("txtbxHFLFValuedBm"))
                    {
                        txtbxHFLFValuedBm.Text = sldrHFLFPwr.Maximum.ToString();
                    }
                    else
                    {
                        txtbxWriteValuedBm.Text = sldrWritePwr.Maximum.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant Message",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                if (((TextBox)sender).Name.Contains("txtbxValuedBm"))
                {
                    txtbxValuedBm.Text = sldrReadPwr.Maximum.ToString();
                }
                else if (((TextBox)sender).Name.Contains("txtbxHFLFValuedBm"))
                {
                    txtbxHFLFValuedBm.Text = sldrHFLFPwr.Maximum.ToString();
                }
                else
                {
                    txtbxWriteValuedBm.Text = sldrWritePwr.Maximum.ToString();
                }
            }
        }

        /// <summary>
        /// Validate hex string
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtFilterData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.HexStringChecker(e.Text);
            base.OnPreviewTextInput(e);
        }

        #region Load/Save Profile

        // Remember if saved configurations is loaded by the user
        bool isLoadSavedConfigurations = false;

        /// <summary>
        /// Event handler to handle the load configurations when user presses Load button in
        /// Load/Save Profile section Used to load the configuration parameters from the user
        /// specified file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openLoadSaveConfigFileDialog = new OpenFileDialog();
                openLoadSaveConfigFileDialog.Filter = "URA Configuration Files (.urac)|*.urac";
                openLoadSaveConfigFileDialog.Title = "Select a configuration file to load reader and UI configuration parameters";
                openLoadSaveConfigFileDialog.InitialDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
                openLoadSaveConfigFileDialog.RestoreDirectory = true;
                if (true == openLoadSaveConfigFileDialog.ShowDialog())
                {
                    Mouse.SetCursor(Cursors.AppStarting);
                    isLoadSavedConfigurations = true;
                    loadSaveConfig.LoadConfigurations(openLoadSaveConfigFileDialog.FileName);
                    LoadConfigurations();
                    isLoadSavedConfigurations = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load Profile: " + ex.Message, "Universal Reader Assistant Message",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Mouse.SetCursor(Cursors.Arrow);
                isLoadSavedConfigurations = false;
            }
        }

        /// <summary>
        /// Displays message and logs the error message in the error log file 
        /// if the user enters value other then true or false
        /// </summary>
        /// <param name="parametername"></param>
        private void NotifyInvalidLoadConfigOption(string parametername)
        {
            MessageBox.Show("Saved parameter [" + parametername + "] parameter has invalid value [" +
                        loadSaveConfig.Properties[parametername] + "]. This parameter accepts only true or false."
                            + " URA sets this parameter to previous set value ", "Universal Reader Assistant",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            Onlog("Saved parameter [" + parametername + "] parameter has invalid value [" +
                        loadSaveConfig.Properties[parametername] + "]. This parameter accepts only true or false."
                            + " URA sets this parameter to previous set value ");
        }

        /// <summary>
        /// Load the configurations from the user specified configuration file
        /// </summary>
        private void LoadConfigurations()
        {
            if (lblshowStatus.Content.ToString() == "Disconnected")
            {
                if (loadSaveConfig.Properties["/application/connect/readerType"].Equals("SerialReader"))
                {
                    rdbtnLocalConnection.IsChecked = true;
                    cmbReaderAddr.Text = loadSaveConfig.Properties["/application/connect/readerURI"].ToUpper();
                    if (loadSaveConfig.Properties.ContainsKey("/reader/baudRate"))
                    {
                        if (loadSaveConfig.Properties["/reader/baudRate"] != "")
                        {
                            cbxBaudRate.SelectedIndex = GetIndexOf(cbxBaudRate,
                                loadSaveConfig.Properties["/reader/baudRate"], "Baudrate");
                        }
                        else
                        {
                            cbxBaudRate.SelectedIndex = 0;
                        }
                    }
                }
                else if (loadSaveConfig.Properties["/application/connect/readerType"].Equals("FixedReader"))
                {
                    rdbtnNetworkConnection.IsChecked = true;
                    cmbFixedReaderAddr.Text = loadSaveConfig.Properties["/application/connect/readerURI"];
                }
                else if (loadSaveConfig.Properties["/application/connect/readerType"].Equals("CustomTransport"))
                {
                    rdbtnCustomTransportConnection.IsChecked = true;
                    txtCustomTransport.Text = loadSaveConfig.Properties["/application/connect/readerURI"];
                }
                else
                {
                    // Notify to the user if options are not valid.
                    throw new Exception("Saved reader type [/application/connect/readerType] parameter has invalid value [" +
                        loadSaveConfig.Properties["/application/connect/readerType"]
                        + "]. URA will stop loading the remaining parameters." +
                        " Please provide valid reader type in configuration file.");
                    //NotifyLoadSaveConfigErrorMessage();
                    //rdbtnNetworkConnection.IsChecked = true;
                    //cmbFixedReaderAddr.Text = loadSaveConfig.Properties["/application/connect/readerURI"];
                }

                // Enable transport logging
                if (loadSaveConfig.Properties["/application/connect/enableTransportLogging"].ToLower().Equals("true"))
                {
                    chkEnableTransportLogging.IsChecked = true;
                }
                else if (loadSaveConfig.Properties["/application/connect/enableTransportLogging"].ToLower().Equals("false"))
                {
                    chkEnableTransportLogging.IsChecked = false;
                }
                else
                {
                    // Notify the error message to the user
                    NotifyInvalidLoadConfigOption("/application/connect/enableTransportLogging");
                }
                btnConnect_Click_Body(this, new RoutedEventArgs());
                if (lblshowStatus.Content.ToString() == "Connected")
                {
                    LoadAfterConnectConfigurations();
                    Mouse.SetCursor(Cursors.Arrow);
                    MessageBox.Show("Loaded reader and UI configuration parameters successfully",
                        "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                LoadAfterConnectConfigurations();
                Mouse.SetCursor(Cursors.Arrow);
                MessageBox.Show("Loaded reader and UI configuration parameters successfully",
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Display the error message to the user and log the message in error log file
        /// </summary>
        /// <param name="message">message to be displayed to the user</param>
        private void NotifyLoadSaveConfigErrorMessage(string message)
        {
            // display the message to the user
            MessageBox.Show(message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
            // log the error message
            Onlog(message);
        }

        /// <summary>
        /// Load the after connect parameters on to the UI and reader, if the reader is already connected
        /// </summary>
        private void LoadAfterConnectConfigurations()
        {
            if (loadSaveConfig.Properties.ContainsKey("/application/connect/enableTransportLogging"))
            {
                if (loadSaveConfig.Properties["/application/connect/enableTransportLogging"].ToLower().Equals("true"))
                {
                    if (lblshowStatus.Content.ToString() == "Connected")
                    {
                        if ((bool)chkEnableTransportLogging.IsChecked)
                        {
                            // if already checked don't do anything
                        }
                        else
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved enable transport logging "
                                + "[/application/connect/enableTransportLogging] value ["
                               + loadSaveConfig.Properties["/application/connect/enableTransportLogging"]
                               + "] failed. Transport logging can be enabled only at the time of connecting "
                               + "to the reader and not when reader is connected. URA  skips this setting"
                                + " or change to the supported value and reload the configuration");
                        }
                    }
                }
            }
            // Set read behavior
            if (loadSaveConfig.Properties["/application/readwriteOption/ReadBehaviour"].Equals("ReadOnce"))
            {
                rdBtnReadOnce.IsChecked = true;
                string tempValueReadOnce = txtbxreadOnceTimeout.Text;
                if (Utilities.AreAllValidNumericChars(
                    loadSaveConfig.Properties["/application/readwriteOption/readOnceTimeout"]))
                {
                    try
                    {
                        txtbxreadOnceTimeout.Text = loadSaveConfig.Properties["/application/readwriteOption/readOnceTimeout"];
                        if ((Convert.ToInt32(txtbxreadOnceTimeout.Text) < 30)
                            || ((Convert.ToInt32(txtbxreadOnceTimeout.Text) > 65535)))
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved Timeout (ms) "
                                + "[/application/readwriteOption/readOnceTimeout] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/readOnceTimeout"] + "] failed. "
                           + "Please enter only positive number within 30 and 65535."
                           + "URA sets Timeout (ms) to previous set value [" + tempValueReadOnce + "] or change to "
                           + "the supported value and reload the configuration");
                            txtbxreadOnceTimeout.Text = tempValueReadOnce;
                        }
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved Timeout (ms)[/application/readwriteOption/readOnceTimeout] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/readOnceTimeout"] + "] failed. "
                           + "Please enter only positive number within 30 and 65535. URA sets Timeout (ms) to previous set value ["
                           + tempValueReadOnce + "] or change to the supported value and reload the configuration");
                        txtbxreadOnceTimeout.Text = tempValueReadOnce;
                    }
                }
                else
                {
                    NotifyLoadSaveConfigErrorMessage("Saved Timeout (ms)[/application/readwriteOption/readOnceTimeout] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/readOnceTimeout"] + "] failed. Please enter "
                           + " only positive number within 30 and 65535. URA sets Timeout (ms) to previous set value ["
                           + tempValueReadOnce + "] or change to the supported value and reload the configuration");
                    txtbxreadOnceTimeout.Text = tempValueReadOnce;
                }
            }
            else if (loadSaveConfig.Properties["/application/readwriteOption/ReadBehaviour"].Equals("ReadContinuously"))
            {
                rdBtnReadContinuously.IsChecked = true;
                string tempValueRfOn = txtRFOnTimeout.Text;
                if (Utilities.AreAllValidNumericChars(loadSaveConfig.Properties["/reader/read/asyncOnTime"]))
                {
                    try
                    {
                        txtRFOnTimeout.Text = loadSaveConfig.Properties["/reader/read/asyncOnTime"];
                        if (Convert.ToInt32(txtRFOnTimeout.Text) < 0)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved RF On (ms) [/reader/read/asyncOnTime] value ["
                            + loadSaveConfig.Properties["/reader/read/asyncOnTime"] + "] failed. Please enter only "
                            + " positive number not more then 65535. URA sets RF On (ms) to previous set value  ["
                            + tempValueRfOn + "] or change to the supported value and reload the configuration");
                            txtRFOnTimeout.Text = tempValueRfOn;
                        }
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved RF On (ms) [/reader/read/asyncOnTime] value ["
                           + loadSaveConfig.Properties["/reader/read/asyncOnTime"] + "] failed. Please enter only "
                           + "positive number not more then 65535. URA sets RF On (ms) to previous set value  ["
                           + tempValueRfOn + "] or change to the supported value and reload the configuration");
                        txtRFOnTimeout.Text = tempValueRfOn;
                    }
                }
                else
                {

                    NotifyLoadSaveConfigErrorMessage("Saved RF On (ms) [/reader/read/asyncOnTime] value ["
                           + loadSaveConfig.Properties["/reader/read/asyncOnTime"] + "] failed. Please enter only "
                           + "positive number not more then 65535. URA sets RF On (ms) to previous set value  ["
                           + tempValueRfOn + "] or change to the supported value and reload the configuration");
                    txtRFOnTimeout.Text = tempValueRfOn;
                }
                string tempValueRfOff = txtRFOffTimeout.Text;
                if (Utilities.AreAllValidNumericChars(loadSaveConfig.Properties["/reader/read/asyncOffTime"]))
                {
                    try
                    {
                        txtRFOffTimeout.Text = loadSaveConfig.Properties["/reader/read/asyncOffTime"];
                        if (Convert.ToInt32(txtRFOffTimeout.Text) < 0)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved RF Off (ms)[/reader/read/asyncOffTime] value ["
                           + loadSaveConfig.Properties["/reader/read/asyncOffTime"] + "] failed. Please enter only "
                           + "positive number not more then 65535. URA sets RF Off (ms) to previous set value ["
                           + tempValueRfOff + "] or change to the supported value and reload the configuration");
                            txtRFOffTimeout.Text = tempValueRfOff;
                        }
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved RF Off (ms)[/reader/read/asyncOffTime] value ["
                           + loadSaveConfig.Properties["/reader/read/asyncOffTime"] + "] failed. Please enter only "
                           + " number not more then 65535. URA sets RF Off (ms) to previous set value ["
                           + tempValueRfOff + "] or change to the supported value and reload the configuration");
                        txtRFOffTimeout.Text = tempValueRfOff;
                    }
                }
                else
                {
                    NotifyLoadSaveConfigErrorMessage("Saved RF Off (ms)[/reader/read/asyncOffTime] value ["
                           + loadSaveConfig.Properties["/reader/read/asyncOffTime"] + "] failed. Please enter only "
                           + "positive number not more then 65535. URA sets RF Off (ms) to previous set value ["
                           + tempValueRfOff + "] or change to the supported value and reload the configuration");
                    txtRFOffTimeout.Text = tempValueRfOff;
                }
            }
            else
            {
                NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/readwriteOption/ReadBehaviour ] has "
                    + " invalid option [" + loadSaveConfig.Properties["/application/readwriteOption/ReadBehaviour"]
                    + "]. URA skips the read behavior and timeout parameters given in the configuration file"
                    + ".URA sets to previous chosen read behavior and its relevant timeouts parameters.");
            }

            // Set region
            if (loadSaveConfig.Properties.ContainsKey("/reader/region/id"))
            {
                bool regionStatus = false;
                regioncombo.SelectedIndex = GetRegionIndexOf(regioncombo,
                    loadSaveConfig.Properties["/reader/region/id"], out regionStatus);
                if (!regionStatus)
                {
                    if (objReader is SerialReader)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved region[/reader/region/id] value ["
                               + loadSaveConfig.Properties["/reader/region/id"] + "] failed. Please enter a "
                               + "supported region. URA gets and applies region ["
                               + regionToSet.ToString() + "] to URA or change to the supported value and reload the configuration");
                    }
                    else
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved region[/reader/region/id] value ["
                               + loadSaveConfig.Properties["/reader/region/id"] + "] failed. URA won't support to set this parameter."
                               + " URA gets the region from reader ["
                               + regionToSet.ToString() + "] and displays the value in UI.");
                    }
                    //set the region on module
                    regioncombo.SelectedItem = regioncombo.Items.GetItemAt(regioncombo.Items.IndexOf(regionToSet));
                }
            }

            // Set baud rate
            if (loadSaveConfig.Properties.ContainsKey("/reader/baudRate"))
            {
                int tempValueBaudrate = cbxBaudRate.SelectedIndex;
                if (loadSaveConfig.Properties["/reader/baudRate"] != "")
                {
                    if (cbxBaudRate.Visibility == System.Windows.Visibility.Visible)
                    {
                        cbxBaudRate.SelectedIndex = GetIndexOf(cbxBaudRate,
                            loadSaveConfig.Properties["/reader/baudRate"], "Baudrate");
                    }
                    else
                    {
                        // Fast search is not supported by connected reader
                        NotifyLoadSaveConfigErrorMessage("Saved baudrate parameter [/reader/baudRate] "
                         + " is not supported by connected reader. URA skips this setting");
                    }
                }
                else
                {
                    if (cbxBaudRate.Visibility == System.Windows.Visibility.Visible)
                    {
                        cbxBaudRate.SelectedIndex = tempValueBaudrate;
                        NotifyLoadSaveConfigErrorMessage("Saved baudrate[/reader/baudRate] value ["
                              + loadSaveConfig.Properties["/reader/baudRate"] + "] failed. Please enter a supported baudrate."
                              + " URA gets and sets baudrate ["
                              + cbxBaudRate.Text + "] or change to the supported value and reload the configuration");
                    }
                }
            }

            // Set fast search
            if (loadSaveConfig.Properties["/application/readwriteOption/enableFastSearch"].ToLower().Equals("true"))
            {
                if (chkEnableFastSearch.IsEnabled)
                {
                    chkEnableFastSearch.IsChecked = true;
                }
                else
                {
                    // Fast search is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved fast search parameter [/application/readwriteOption/enableFastSearch] "
                        + " is not supported by connected reader. URA skips this setting");
                    chkEnableFastSearch.IsChecked = false;
                }
            }
            else if (loadSaveConfig.Properties["/application/readwriteOption/enableFastSearch"].ToLower().Equals("false"))
            {
                chkEnableFastSearch.IsChecked = false;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/readwriteOption/enableFastSearch");
            }

            // Antenna switching
            string temp = "/application/readwriteOption/switchingMethod";
            if (loadSaveConfig.Properties[temp].Equals("Equal"))
            {
                rdBtnEqualSwitching.IsChecked = true;
            }
            else if (loadSaveConfig.Properties[temp].Equals("Dynamic"))
            {
                rdBtnAutoSwitching.IsChecked = true;
            }
            else
            {
                NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/readwriteOption/switchingMethod ]"
                    + " has invalid option ["
                    + loadSaveConfig.Properties["/application/readwriteOption/switchingMethod"] + "]"
                    + ".URA sets to previous chosen switching method.");
            }

            // Save protocol configurations
            if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/Protocols"))
            {
                string[] protocolToSet = loadSaveConfig.Properties["/application/readwriteOption/Protocols"].Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> protocollist = protocolToSet.ToList<string>();
                List<string> protocolCheck = new List<string>() { "GEN2", "ATA", "IPX64", "IPX256", "ISO18000-6B", "ISO18000-6B-UCODE" };
                int protcolIndex = 0;
                Dictionary<string, CheckBox> protocolCheckBox = new Dictionary<string, CheckBox>();
                foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackpanel6))
                {
                    protocolCheckBox.Add(child.Content.ToString().ToUpper(), child);
                    child.IsChecked = false;
                }
                foreach (string proto in protocollist)
                {
                    if (protocolCheckBox.Keys.Contains(proto.ToUpper()))
                    {
                        if (protocolCheckBox[proto.ToUpper()].Visibility == System.Windows.Visibility.Visible)
                        {
                            protocolCheckBox[proto.ToUpper()].IsChecked = true;
                        }
                        else
                        {
                            if (protcolIndex < protocollist.Count)
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/Protocols] protocol ["
                                    + proto + "] " + "is not supported for connected reader. URA skips this protocol");
                            }
                            protocolCheckBox[proto.ToUpper()].IsChecked = false;
                        }
                    }
                    else
                    {
                        if (protcolIndex < protocollist.Count)
                        {
                            if (!protocolCheck.Contains(proto.ToUpper()))
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/Protocols] protocol ["
                                    + proto + "] " + "is not valid protocol option for connected reader. URA skips this protocol");

                            }
                        }
                    }
                    protcolIndex++;
                }

            }

            // Antenna detection
            if (loadSaveConfig.Properties["/reader/antenna/checkPort"].ToLower().Equals("true"))
            {
                if (chkbxAntennaDetection.IsEnabled)
                {
                    chkbxAntennaDetection.IsChecked = true;
                    SetCheckPort(this, new EventArgs(), true);
                }
                else
                {
                    // Antenna detection is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved antenna detection parameter [/reader/antenna/checkPort] "
                        + " is not supported by connected reader. URA skips this setting");
                    chkbxAntennaDetection.IsChecked = false;
                    SetCheckPort(this, new EventArgs(), false);
                }
            }
            else if (loadSaveConfig.Properties["/reader/antenna/checkPort"].ToLower().Equals("false"))
            {
                chkbxAntennaDetection.IsChecked = false;
                SetCheckPort(this, new EventArgs(), false);
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/reader/antenna/checkPort");
            }

            // Save antenna configurations
            if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/Antennas"))
            {
                if (!((bool)chkbxAntennaDetection.IsChecked))
                {
                    string[] antennasToSet = loadSaveConfig.Properties["/application/readwriteOption/Antennas"].Split(
                        new char[] { ',', ' ' });
                    // List of antennas to set on the connected reader
                    List<string> antennalist = antennasToSet.ToList<string>();
                    // List of available antenna on the connected reader
                    List<string> availableAntenna = new List<string>();
                    // Generate the list of available antenna ports on the 
                    // connected reader and set them to false.
                    foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackPanel8))
                    {
                        availableAntenna.Add(child.Content.ToString());
                        child.IsChecked = false;
                    }
                    foreach (string antennaToConnect in antennasToSet)
                    {
                        // Check if the antenna to set is present in the list of available antennas
                        if (availableAntenna.Contains(antennaToConnect))
                        {
                            // Loop through all the available antenna
                            foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackPanel8))
                            {
                                if (child.Content.ToString().Equals(antennaToConnect))
                                {
                                    if ((child.Visibility == System.Windows.Visibility.Visible)
                                        && (child.IsEnabled == true))
                                    {
                                        child.IsChecked = true;
                                    }
                                    else
                                    {
                                        NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/Antennas] antenna ["
                                            + child.Content.ToString() + "] "
                                            + "is not supported. URA skips this antenna");
                                        child.IsChecked = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Not a valid antenna option
                            NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/Antennas] antenna ["
                                + antennaToConnect + "] "
                                + "is not valid antenna option.");
                        }
                    }
                }
                else
                {
                    // Antenna detection is enabled
                    NotifyLoadSaveConfigErrorMessage("Saved antenna detection parameter [/reader/antenna/checkPort] "
                        + " is enabled. URA skips manual antenna settings if provided any and gets the antenna connected from the reader.");
                }
            }

            // save Portswitchgpos
            if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/portswitchgpos"))
            {
                string[] portswitchgposToSet = loadSaveConfig.Properties["/application/readwriteOption/portswitchgpos"].Split(new char[] { ',', ' ' });
                List<string> availableAntmux = new List<string>();
                if (portswitchgposToSet.Length == 0 || portswitchgposToSet[0].Equals(""))
                {
                    cbxAntennamux.IsChecked = false;
                }
                else
                {
                    cbxAntennamux.IsChecked = true;
                    //Get total gpios using both input and output list. Each gpio can be used as either input or output.
                    //Hence combine the result of inputList and outputList.
                    int[] inputList = (int[])(objReader.ParamGet("/reader/gpio/inputList"));
                    int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
                    var aMuxList = new SortedSet<String>();
                    foreach (int i in inputList)
                        aMuxList.Add(i.ToString());
                    foreach (int j in outputList)
                        aMuxList.Add(j.ToString());
                    availableAntmux.AddRange(aMuxList);
                    if (!chbxOne.IsEnabled)
                    {
                        if (availableAntmux.Contains("1"))
                        {
                            availableAntmux.Remove("1");
                        }
                    }
                    if (!chbxTwo.IsEnabled)
                    {
                        if (availableAntmux.Contains("2"))
                        {
                            availableAntmux.Remove("2");
                        }
                    }
                    if (!chbxThree.IsEnabled)
                    {
                        if (availableAntmux.Contains("3"))
                        {
                            availableAntmux.Remove("3");
                        }
                    }
                    if (!chbxFour.IsEnabled)
                    {
                        if (availableAntmux.Contains("4"))
                        {
                            availableAntmux.Remove("4");
                        }
                    }
                    foreach (string str in portswitchgposToSet)
                    {
                        if (availableAntmux.Contains(str))
                        {
                            if (str.Equals("1"))
                            {
                                chbxOne.IsChecked = true;
                                displayLogicalAntennas();
                            }
                            if (str.Equals("2"))
                            {
                                chbxTwo.IsChecked = true;
                                displayLogicalAntennas();
                            }
                            if (str.Equals("3"))
                            {
                                chbxThree.IsChecked = true;
                                displayLogicalAntennas();
                            }
                            if (str.Equals("4"))
                            {
                                chbxFour.IsChecked = true;
                                displayLogicalAntennas();
                            }
                        }
                        else
                        {
                            // Not a valid antenna option
                            NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/portswitchgpos] antmux ["
                                + str + "] "
                                + "is not valid antmux option. URA skips this antmux Option");
                        }
                    }
                }
            }

            // input list
            if (!(objReader is LlrpReader))
            {
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/inputList"))
                {
                    string[] inputListToSet = loadSaveConfig.Properties["/application/readwriteOption/inputList"].Split(new char[] { ',', ' ' });
                    inputListToSet = inputListToSet.Distinct().ToArray();
                    List<string> availableinputList = new List<string>();
                    List<int> updatedInputList = new List<int>();
                    if ((bool)cbxAntennamux.IsChecked)
                    {
                        if (!(bool)chbxOne.IsChecked)
                        {
                            availableinputList.Add("1");
                        }
                        if (!(bool)chbxTwo.IsChecked)
                        {
                            availableinputList.Add("2");
                        }
                        if (!(bool)chbxThree.IsChecked)
                        {
                            availableinputList.Add("3");
                        }
                        if (!(bool)chbxFour.IsChecked)
                        {
                            availableinputList.Add("4");
                        }
                    }
                    else
                    {
                        //Get total gpios using both input and output list. Each gpio can be used as either input or output.
                        //Hence combine the result of inputList and outputList.
                        int[] inputList = (int[])(objReader.ParamGet("/reader/gpio/inputList"));
                        int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
                        var gpiList = new SortedSet<String>();
                        foreach (int i in inputList)
                            gpiList.Add(i.ToString());
                        foreach (int j in outputList)
                            gpiList.Add(j.ToString());
                        availableinputList.AddRange(gpiList);
                    }
                    int[] triggerGPI = (int[])objReader.ParamGet("/reader/read/trigger/gpi");


                    foreach (int i in triggerGPI)
                    {
                        if (i == 1)
                        {
                            if (availableinputList.Contains("1"))
                            {
                                availableinputList.Remove("1");
                            }
                        }
                        if (i == 2)
                        {
                            if (availableinputList.Contains("2"))
                            {
                                availableinputList.Remove("2");
                            }
                        }
                        if (i == 3)
                        {
                            if (availableinputList.Contains("3"))
                            {
                                availableinputList.Remove("3");
                            }
                        }
                        if (i == 4)
                        {
                            if (availableinputList.Contains("4"))
                            {
                                availableinputList.Remove("4");
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(inputListToSet[0]))//.Contains(""))
                    {
                        foreach (string str in inputListToSet)
                        {
                            if (availableinputList.Contains(str))
                            {
                                if (str.Equals("1"))
                                {
                                    if (!updatedInputList.Contains(1))
                                    {
                                        updatedInputList.Add(1);
                                    }
                                    chbxGpo1.Checked -= chbxGpo1_Checked;
                                    chbxGpo1.IsChecked = true;
                                    chbxGpo1.Checked += chbxGpo1_Checked;
                                    stkpnlOneDirection.Visibility = Visibility.Visible;
                                    stkpnlOneValue.Visibility = Visibility.Visible;
                                    chbxGpo1.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("2"))
                                {
                                    if (!updatedInputList.Contains(2))
                                    {
                                        updatedInputList.Add(2);
                                    }
                                    chbxGpo2.Checked -= chbxGpo2_Checked;
                                    chbxGpo2.IsChecked = true;
                                    chbxGpo2.Checked += chbxGpo2_Checked;
                                    stkpnlTwoDirection.Visibility = Visibility.Visible;
                                    stkpnlTwoValue.Visibility = Visibility.Visible;
                                    chbxGpo2.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("3"))
                                {
                                    if (!updatedInputList.Contains(3))
                                    {
                                        updatedInputList.Add(3);
                                    }
                                    chbxGpo3.Checked -= chbxGpo3_Checked;
                                    chbxGpo3.IsChecked = true;
                                    chbxGpo3.Checked += chbxGpo3_Checked;
                                    stkpnlThreeDirection.Visibility = Visibility.Visible;
                                    stkpnlThreeValue.Visibility = Visibility.Visible;
                                    chbxGpo3.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("4"))
                                {
                                    if (!updatedInputList.Contains(4))
                                    {
                                        updatedInputList.Add(4);
                                    }
                                    chbxGpo4.Checked -= chbxGpo4_Checked;
                                    chbxGpo4.IsChecked = true;
                                    chbxGpo4.Checked += chbxGpo4_Checked;
                                    stkpnlFourDirection.Visibility = Visibility.Visible;
                                    stkpnlFourValue.Visibility = Visibility.Visible;
                                    chbxGpo4.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                if (availableinputList.Count > 0)
                                {
                                    // Not a valid input option
                                    NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/inputList] inputList ["
                                        + str + "] "
                                        + "is not valid input option. URA skips this input Option");
                                }
                            }
                        }
                    }
                    objReader.ParamSet("/reader/gpio/inputList", updatedInputList.ToArray());
                    validateUIcontrols();
                    configureInputDirection(updatedInputList.ToArray());
                }

                // output list
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/outputList"))
                {
                    string[] outputListToSet = loadSaveConfig.Properties["/application/readwriteOption/outputList"].Split(new char[] { ',', ' ' });
                    outputListToSet = outputListToSet.Distinct().ToArray();
                    List<string> availableOutputList = new List<string>();
                    List<int> updatedOutputList = new List<int>();
                    if ((bool)cbxAntennamux.IsChecked)
                    {
                        if (!(bool)chbxOne.IsChecked)
                        {
                            availableOutputList.Add("1");
                        }
                        if (!(bool)chbxTwo.IsChecked)
                        {
                            availableOutputList.Add("2");
                        }
                        if (!(bool)chbxThree.IsChecked)
                        {
                            availableOutputList.Add("3");
                        }
                        if (!(bool)chbxFour.IsChecked)
                        {
                            availableOutputList.Add("4");
                        }
                    }
                    else
                    {
                        //Get total gpios using both input and output list. Each gpio can be used as either input or output.
                        //Hence combine the result of inputList and outputList.
                        int[] inputList = (int[])(objReader.ParamGet("/reader/gpio/inputList"));
                        int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
                        var gpoList = new SortedSet<String>();
                        foreach (int i in inputList)
                            gpoList.Add(i.ToString());
                        foreach (int j in outputList)
                            gpoList.Add(j.ToString());
                        availableOutputList.AddRange(gpoList);
                    }
                    int[] triggerGPI = (int[])objReader.ParamGet("/reader/read/trigger/gpi");
                    foreach (int i in triggerGPI)
                    {
                        if (i == 1)
                        {
                            if (availableOutputList.Contains("1"))
                            {
                                availableOutputList.Remove("1");
                            }
                        }
                        if (i == 2)
                        {
                            if (availableOutputList.Contains("2"))
                            {
                                availableOutputList.Remove("2");
                            }
                        }
                        if (i == 3)
                        {
                            if (availableOutputList.Contains("3"))
                            {
                                availableOutputList.Remove("3");
                            }
                        }
                        if (i == 4)
                        {
                            if (availableOutputList.Contains("4"))
                            {
                                availableOutputList.Remove("4");
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(outputListToSet[0]))//.Contains(""))
                    {
                        foreach (string str in outputListToSet)
                        {
                            if (availableOutputList.Contains(str))
                            {
                                if (str.Equals("1"))
                                {
                                    if (!updatedOutputList.Contains(1))
                                    {
                                        updatedOutputList.Add(1);
                                    }
                                    chbxGpo1.Checked -= chbxGpo1_Checked;
                                    chbxGpo1.IsChecked = true;
                                    chbxGpo1.Checked += chbxGpo1_Checked;
                                    stkpnlOneDirection.Visibility = Visibility.Visible;
                                    stkpnlOneValue.Visibility = Visibility.Visible;
                                    chbxGpo1.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("2"))
                                {
                                    if (!updatedOutputList.Contains(2))
                                    {
                                        updatedOutputList.Add(2);
                                    }
                                    chbxGpo2.Checked -= chbxGpo2_Checked;
                                    chbxGpo2.IsChecked = true;
                                    chbxGpo2.Checked += chbxGpo2_Checked;
                                    stkpnlTwoDirection.Visibility = Visibility.Visible;
                                    stkpnlTwoValue.Visibility = Visibility.Visible;
                                    chbxGpo2.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("3"))
                                {
                                    if (!updatedOutputList.Contains(3))
                                    {
                                        updatedOutputList.Add(3);
                                    }
                                    chbxGpo3.Checked -= chbxGpo3_Checked;
                                    chbxGpo3.IsChecked = true;
                                    chbxGpo3.Checked += chbxGpo3_Checked;
                                    stkpnlThreeDirection.Visibility = Visibility.Visible;
                                    stkpnlThreeValue.Visibility = Visibility.Visible;
                                    chbxGpo3.Visibility = Visibility.Visible;
                                }
                                if (str.Equals("4"))
                                {
                                    if (!updatedOutputList.Contains(4))
                                    {
                                        updatedOutputList.Add(4);
                                    }
                                    chbxGpo4.Checked -= chbxGpo4_Checked;
                                    chbxGpo4.IsChecked = true;
                                    chbxGpo4.Checked += chbxGpo4_Checked;
                                    stkpnlFourDirection.Visibility = Visibility.Visible;
                                    stkpnlFourValue.Visibility = Visibility.Visible;
                                    chbxGpo4.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                if (availableOutputList.Count > 0)
                                {
                                    // Not a valid output option
                                    NotifyLoadSaveConfigErrorMessage("Saved [/application/readwriteOption/outputList] outputList ["
                                        + str + "] "
                                        + "is not valid output option. URA skips this output Option");
                                }
                            }
                        }
                    }
                    objReader.ParamSet("/reader/gpio/outputList", updatedOutputList.ToArray());
                    validateUIcontrols();
                    configureOutputDirection(updatedOutputList.ToArray());

                }
            }
            else
            {
                configureGPIOS();
            }
            // Embedded read data
            if (loadSaveConfig.Properties["/application/readwriteOption/enableEmbeddedReadData"].ToLower().Equals("true"))
            {
                if (ReadDataGroupBox.IsEnabled)
                {
                    chkEmbeddedReadData.IsChecked = true;
                    if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/enableEmbeddedReadData/MemBank"))
                    {
                        cbxReadDataBank.SelectedIndex = GetIndexOf(cbxReadDataBank,
                            loadSaveConfig.Properties["/application/readwriteOption/enableEmbeddedReadData/MemBank"],
                            "EmbeddedReadData MemBank");
                    }
                    if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/enableEmbeddedReadData/StartAddress"))
                    {
                        string tempValueEmbeddStartAdd = txtembReadStartAddr.Text;
                        try
                        {
                            Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
                                "/application/readwriteOption/enableEmbeddedReadData/StartAddress"]);
                            txtembReadStartAddr.Text = loadSaveConfig.Properties[
                                "/application/readwriteOption/enableEmbeddedReadData/StartAddress"];
                            txtembReadStartAddr.Focus();
                        }
                        catch (Exception)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved embedded read start address "
                                + " [/application/readwriteOption/enableEmbeddedReadData/StartAddress] value ["
                                       + loadSaveConfig.Properties["/application/readwriteOption/enableEmbeddedReadData/StartAddress"] + "]"
                                       + " invalid. Please enter valid dec or hex with prefix as 0x. URA sets embedded read start address to "
                                       + "previous set value [" + tempValueEmbeddStartAdd + "] or change to the supported value and reload "
                                       + "the configuration");
                            txtembReadStartAddr.Text = tempValueEmbeddStartAdd;
                        }
                    }
                    if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead"))
                    {
                        string tempValueEmbeddedNoOfWrds = txtembReadLength.Text;
                        try
                        {
                            Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
                                "/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead"]);
                            txtembReadLength.Text = loadSaveConfig.Properties[
                                "/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead"];
                            txtembReadLength.Focus();
                        }
                        catch (Exception)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved embedded read, no of words to read "
                                + " [/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead] value ["
                                      + loadSaveConfig.Properties["/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead"] + "] "
                                      + "invalid. Please enter valid dec or hex with prefix as 0x. URA sets embedded read, no of words to read "
                                      + "to previous set value [" + tempValueEmbeddedNoOfWrds + "] or change to the supported value and reload"
                                      + " the configuration");
                            txtembReadLength.Text = tempValueEmbeddedNoOfWrds;
                        }
                    }
                }
                else
                {
                    // Embedded read data is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved enable embedded read data parameter "
                        + "[/application/readwriteOption/enableEmbeddedReadData] "
                        + " is not supported by connected reader. URA skips this setting and embedded read "
                        + "data relevant settings.");
                    chkEnableFastSearch.IsChecked = false;
                    ReadDataGroupBox.IsEnabled = false;
                }
            }
            else if (loadSaveConfig.Properties["/application/readwriteOption/enableEmbeddedReadData"].ToLower().Equals("false"))
            {
                chkEmbeddedReadData.IsChecked = false;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/readwriteOption/enableEmbeddedReadData");
            }

            // UniqueByData
            if (loadSaveConfig.Properties["/reader/tagReadData/uniqueByData"].ToLower().Equals("true"))
            {
                chkUniqueByData.IsChecked = true;
            }
            else if (loadSaveConfig.Properties["/reader/tagReadData/uniqueByData"].ToLower().Equals("false"))
            {
                chkUniqueByData.IsChecked = false;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/reader/tagReadData/uniqueByData");
            }

            // Show failed read data
            if (loadSaveConfig.Properties[
                "/application/readwriteOption/enableEmbeddedReadData/uniqueByData/ShowFailedDataRead"].ToLower().Equals("true"))
            {
                chkShowFailedDataReads.IsChecked = true;
            }
            else if (loadSaveConfig.Properties[
                "/application/readwriteOption/enableEmbeddedReadData/uniqueByData/ShowFailedDataRead"].ToLower().Equals("false"))
            {
                chkShowFailedDataReads.IsChecked = false;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption(
                    "/application/readwriteOption/enableEmbeddedReadData/uniqueByData/ShowFailedDataRead");
            }

            //// Apply filter
            if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter1"].ToLower().Equals("true"))
            {
                chkApplyFilter1.IsChecked = true;
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter1/FilterMemBank"))
                {
                    MultiFilters1.cbxFilterMemBank.SelectedIndex = GetIndexOf(MultiFilters1.cbxFilterMemBank,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/FilterMemBank"], "Filter MemBank");
                }
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter1/FilterStartAddress"))
                {
                    string tempValueFilterStartAdd = MultiFilters1.txtFilterStartAddr.Text;
                    try
                    {
                        Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter1/FilterStartAddress"]);
                        MultiFilters1.txtFilterStartAddr.Text = loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter1/FilterStartAddress"];
                        MultiFilters1.txtFilterStartAddr.Focus();
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved filter start address "
                            + "[/application/readwriteOption/applyFilter1/FilterStartAddress]"
                            + " value [" + loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/FilterStartAddress"] + "] "
                            + "invalid. Please enter valid dec or hex with prefix as 0x. URA sets filter start address to previous set value "
                            + " [" + tempValueFilterStartAdd + "] or change to the supported value and reload the configuration");
                        MultiFilters1.txtFilterStartAddr.Text = tempValueFilterStartAdd;
                    }
                }
                if (ValidateFilterDataFromConfig(loadSaveConfig.Properties[
                    "/application/readwriteOption/applyFilter1/FilterData"]))
                {
                    MultiFilters1.txtFilterData.Text = loadSaveConfig.Properties[
                        "/application/readwriteOption/applyFilter1/FilterData"];
                    MultiFilters1.txtFilterData.Focus();
                }
                else
                {
                    string tempValueFilterData = MultiFilters1.txtFilterData.Text;
                    NotifyLoadSaveConfigErrorMessage("Saved Filter data [/application/readwriteOption/applyFilter1/FilterData] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/FilterData"] + "] invalid. Please enter valid"
                           + " hex number. URA sets filter data to previous set value [" + tempValueFilterData + "] or change to the "
                           + " supported value and reload the configuration");
                    MultiFilters1.txtFilterData.Text = tempValueFilterData;
                }

                // Invert filter
                if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/InvertFilter"].ToLower().Equals("true"))
                {
                    MultiFilters1.chkFilterInvert.IsChecked = true;
                }
                else if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/InvertFilter"].ToLower().Equals("false"))
                {
                    MultiFilters1.chkFilterInvert.IsChecked = false;
                }
                else
                {
                    // Notify the error message to the user
                    NotifyInvalidLoadConfigOption("/application/readwriteOption/applyFilter1/InvertFilter");
                }

                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter1/Target"))
                {
                    MultiFilters1.cbxFilterTarget.SelectedIndex = GetIndexOf(MultiFilters1.cbxFilterTarget,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/Target"], "Target");
                }

                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter1/Action"))
                {
                    MultiFilters1.cbxFilterAction.SelectedIndex = GetIndexOf(MultiFilters1.cbxFilterAction,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter1/Action"], "Action");
                }
            }


            if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter2"].ToLower().Equals("true"))
            {
                chkApplyFilter2.IsChecked = true;
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter2/FilterMemBank"))
                {
                    MultiFilters2.cbxFilterMemBank.SelectedIndex = GetIndexOf(MultiFilters2.cbxFilterMemBank,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/FilterMemBank"], "Filter MemBank");
                }
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter2/FilterStartAddress"))
                {
                    string tempValueFilterStartAdd = MultiFilters2.txtFilterStartAddr.Text;
                    try
                    {
                        Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter2/FilterStartAddress"]);
                        MultiFilters2.txtFilterStartAddr.Text = loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter2/FilterStartAddress"];
                        MultiFilters2.txtFilterStartAddr.Focus();
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved filter start address "
                            + "[/application/readwriteOption/applyFilter2/FilterStartAddress]"
                            + " value [" + loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/FilterStartAddress"] + "] "
                            + "invalid. Please enter valid dec or hex with prefix as 0x. URA sets filter start address to previous set value "
                            + " [" + tempValueFilterStartAdd + "] or change to the supported value and reload the configuration");
                        MultiFilters2.txtFilterStartAddr.Text = tempValueFilterStartAdd;
                    }
                }
                if (ValidateFilterDataFromConfig(loadSaveConfig.Properties[
                    "/application/readwriteOption/applyFilter2/FilterData"]))
                {
                    MultiFilters2.txtFilterData.Text = loadSaveConfig.Properties[
                        "/application/readwriteOption/applyFilter2/FilterData"];
                    MultiFilters2.txtFilterData.Focus();
                }
                else
                {
                    string tempValueFilterData = MultiFilters2.txtFilterData.Text;
                    NotifyLoadSaveConfigErrorMessage("Saved Filter data [/application/readwriteOption/applyFilter2/FilterData] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/FilterData"] + "] invalid. Please enter valid"
                           + " hex number. URA sets filter data to previous set value [" + tempValueFilterData + "] or change to the "
                           + " supported value and reload the configuration");
                    MultiFilters2.txtFilterData.Text = tempValueFilterData;
                }
                // Invert filter
                if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/InvertFilter"].ToLower().Equals("true"))
                {
                    MultiFilters2.chkFilterInvert.IsChecked = true;
                }
                else if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/InvertFilter"].ToLower().Equals("false"))
                {
                    MultiFilters2.chkFilterInvert.IsChecked = false;
                }
                else
                {
                    // Notify the error message to the user
                    NotifyInvalidLoadConfigOption("/application/readwriteOption/applyFilter2/InvertFilter");
                }

                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter2/Target"))
                {
                    MultiFilters2.cbxFilterTarget.SelectedIndex = GetIndexOf(MultiFilters2.cbxFilterTarget,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/Target"], "Target");
                }

                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter2/Action"))
                {
                    MultiFilters2.cbxFilterAction.SelectedIndex = GetIndexOf(MultiFilters2.cbxFilterAction,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter2/Action"], "Action");
                }
            }


            if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter3"].ToLower().Equals("true"))
            {
                chkApplyFilter3.IsChecked = true;
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter3/FilterMemBank"))
                {
                    MultiFilters3.cbxFilterMemBank.SelectedIndex = GetIndexOf(MultiFilters3.cbxFilterMemBank,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/FilterMemBank"], "Filter MemBank");
                }
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter2/FilterStartAddress"))
                {
                    string tempValueFilterStartAdd = MultiFilters3.txtFilterStartAddr.Text;
                    try
                    {
                        Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter3/FilterStartAddress"]);
                        MultiFilters3.txtFilterStartAddr.Text = loadSaveConfig.Properties[
                            "/application/readwriteOption/applyFilter3/FilterStartAddress"];
                        MultiFilters3.txtFilterStartAddr.Focus();
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved filter start address "
                            + "[/application/readwriteOption/applyFilter3/FilterStartAddress]"
                            + " value [" + loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/FilterStartAddress"] + "] "
                            + "invalid. Please enter valid dec or hex with prefix as 0x. URA sets filter start address to previous set value "
                            + " [" + tempValueFilterStartAdd + "] or change to the supported value and reload the configuration");
                        MultiFilters3.txtFilterStartAddr.Text = tempValueFilterStartAdd;
                    }
                }
                if (ValidateFilterDataFromConfig(loadSaveConfig.Properties[
                    "/application/readwriteOption/applyFilter3/FilterData"]))
                {
                    MultiFilters3.txtFilterData.Text = loadSaveConfig.Properties[
                        "/application/readwriteOption/applyFilter3/FilterData"];
                    MultiFilters3.txtFilterData.Focus();
                }
                else
                {
                    string tempValueFilterData = MultiFilters1.txtFilterData.Text;
                    NotifyLoadSaveConfigErrorMessage("Saved Filter data [/application/readwriteOption/applyFilter3/FilterData] value ["
                           + loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/FilterData"] + "] invalid. Please enter valid"
                           + " hex number. URA sets filter data to previous set value [" + tempValueFilterData + "] or change to the "
                           + " supported value and reload the configuration");
                    MultiFilters3.txtFilterData.Text = tempValueFilterData;
                }

                // Invert filter
                if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/InvertFilter"].ToLower().Equals("true"))
                {
                    MultiFilters3.chkFilterInvert.IsChecked = true;
                }
                else if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/InvertFilter"].ToLower().Equals("false"))
                {
                    MultiFilters3.chkFilterInvert.IsChecked = false;
                }
                else
                {
                    // Notify the error message to the user
                    NotifyInvalidLoadConfigOption("/application/readwriteOption/applyFilter3/InvertFilter");
                }
                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter3/Target"))
                {
                    MultiFilters3.cbxFilterTarget.SelectedIndex = GetIndexOf(MultiFilters3.cbxFilterTarget,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/Target"], "Target");
                }

                if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter3/Action"))
                {
                    MultiFilters3.cbxFilterAction.SelectedIndex = GetIndexOf(MultiFilters3.cbxFilterAction,
                        loadSaveConfig.Properties["/application/readwriteOption/applyFilter3/Action"], "Action");
                }
            }

            // Performance tuning
            // Save Performance tuning settings

            // Set read power
            if (loadSaveConfig.Properties["/application/performanceTuning/rfPowerSettingGlobal"].ToLower().Equals("true"))
            {
                rdbtnglobal.IsChecked = true;
                if (loadSaveConfig.Properties.ContainsKey("/reader/radio/readPower"))
                {
                    if (loadSaveConfig.Properties["/reader/radio/readPower"] != "")
                    {
                        double tempValue = 0;
                        double newValue = 0;
                        try
                        {
                            tempValue = Convert.ToDouble(txtbxValuedBm.Text);
                            newValue = Convert.ToDouble(loadSaveConfig.Properties["/reader/radio/readPower"]) / 100;
                            if ((newValue >= sldrReadPwr.Minimum) && (newValue <= sldrReadPwr.Maximum))
                            {
                                txtbxValuedBm.Text = newValue.ToString();
                                sldrReadPwr.Value = newValue;
                            }
                            else
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/readPower] value ["
                                    + loadSaveConfig.Properties["/reader/radio/readPower"] + "] failed. Please enter within "
                                    + sldrReadPwr.Minimum * 100 +
                                    " and " + sldrReadPwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                                txtbxValuedBm.Text = tempValue.ToString();
                                sldrReadPwr.Value = tempValue;
                            }
                        }
                        catch (FormatException)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/readPower] value ["
                                    + loadSaveConfig.Properties["/reader/radio/readPower"] + "] failed. Entered value not in correct "
                                    + " format. Please enter within " + sldrReadPwr.Minimum * 100 +
                                    " and " + sldrReadPwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                            txtbxValuedBm.Text = tempValue.ToString();
                            sldrReadPwr.Value = tempValue;
                        }
                    }
                    else
                    {
                        // If read power is empty then keep the previous power. Don't set to max
                        sldrReadPwr.Value = Convert.ToDouble(txtbxValuedBm.Text);
                    }
                }

                // Set write power
                if (loadSaveConfig.Properties.ContainsKey("/reader/radio/writePower"))
                {
                    if (loadSaveConfig.Properties["/reader/radio/writePower"] != "")
                    {
                        double tempValue = 0;
                        double newValue = 0;
                        try
                        {
                            tempValue = Convert.ToDouble(txtbxWriteValuedBm.Text);
                            newValue = Convert.ToDouble(loadSaveConfig.Properties["/reader/radio/writePower"]) / 100;
                            if ((newValue >= sldrWritePwr.Minimum) && (newValue <= sldrWritePwr.Maximum))
                            {
                                txtbxWriteValuedBm.Text = newValue.ToString();
                                sldrWritePwr.Value = newValue;
                            }
                            else
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved write power[/reader/radio/writePower] value ["
                                    + loadSaveConfig.Properties["/reader/radio/writePower"] + "] failed. Please enter within "
                                    + sldrWritePwr.Minimum * 100 +
                                    " and " + sldrWritePwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets writePower power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                                txtbxWriteValuedBm.Text = tempValue.ToString();
                                sldrWritePwr.Value = tempValue;
                            }
                        }
                        catch (FormatException)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved writePower power[/reader/radio/writePower] value ["
                                    + loadSaveConfig.Properties["/reader/radio/writePower"] + "] failed. Entered value not in correct "
                                    + " format. Please enter within " + sldrWritePwr.Minimum * 100 +
                                    " and " + sldrWritePwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets writePower power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                            txtbxWriteValuedBm.Text = tempValue.ToString();
                            sldrWritePwr.Value = tempValue;
                        }
                    }
                    else
                    {
                        // If read power is empty then keep the previous power. Don't set to max
                        sldrWritePwr.Value = Convert.ToDouble(txtbxWriteValuedBm.Text);
                    }
                }
            }
            else
            {
                rdbtnperantenna.IsChecked = true;
                TextBox[] perPortReadText = { txtReadPowerAnt1, txtReadPowerAnt2, txtReadPowerAnt3, txtReadPowerAnt4 };
                TextBox[] perPortWriteText = { txtWritePowerAnt1, txtWritePowerAnt2, txtWritePowerAnt3, txtWritePowerAnt4 };
                double tempValue = 0;
                double newValue = 0;

                if (loadSaveConfig.Properties.ContainsKey("/reader/radio/portReadPowerList"))
                {
                    if (loadSaveConfig.Properties["/reader/radio/portReadPowerList"] != "")
                    {
                        tempValue = Convert.ToDouble(txtbxValuedBm.Text);
                        string[] splitreadresult = loadSaveConfig.Properties["/reader/radio/portReadPowerList"].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                        List<string> splitreadresult_final = new List<string>();
                        foreach (string str in splitreadresult)
                        {
                            splitreadresult_final.AddRange(str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                        try
                        {
                            for (int i = 0; i < splitreadresult_final.Count; i = i + 2)
                            {
                                if (splitreadresult_final.Count % 2 == 0 && !string.IsNullOrWhiteSpace(splitreadresult_final[i]) && Convert.ToInt32(splitreadresult_final[i]) <= 4)
                                {
                                    if (perPortReadText[Convert.ToInt32(splitreadresult_final[i]) - 1].Visibility == Visibility.Visible)
                                    {
                                        newValue = Convert.ToDouble(splitreadresult_final[i + 1]) / 100;
                                        if ((newValue >= sldrReadPwr.Minimum) && (newValue <= sldrReadPwr.Maximum))
                                        {
                                            perPortReadText[Convert.ToInt32(splitreadresult_final[i]) - 1].Text = (Convert.ToDouble(splitreadresult_final[i + 1]) / 100).ToString();
                                        }
                                        else
                                        {
                                            NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portReadPowerList] value ["
                                        + loadSaveConfig.Properties["/reader/radio/portReadPowerList"] + "] failed. Entered value not in correct "
                                        + " format. Please enter within " + sldrReadPwr.Minimum * 100 +
                                        " and " + sldrReadPwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                        + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                                            perPortReadText[Convert.ToInt32(splitreadresult_final[i]) - 1].Text = tempValue.ToString();
                                        }
                                    }
                                    else
                                    {
                                        NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portReadPowerList] value ["
                                        + loadSaveConfig.Properties["/reader/radio/portReadPowerList"] + "] failed. Entered antenna port[" + i.ToString() + "] is not available. ");
                                    }
                                }
                                else
                                    throw new FormatException();
                            }
                        }
                        catch (FormatException)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portReadPowerList] value ["
                                    + loadSaveConfig.Properties["/reader/radio/portReadPowerList"] + "] failed. Entered value not in correct "
                                    + " format. Please enter within " + sldrReadPwr.Minimum * 100 +
                                    " and " + sldrReadPwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                            for (int i = 0; i < 4 / 2; i++)
                            {
                                if (perPortReadText[i].Visibility == Visibility.Visible)
                                    perPortReadText[i].Text = "20";
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 4 / 2; i++)
                        {
                            if (perPortReadText[i].Visibility == Visibility.Visible)
                                perPortReadText[i].Text = "20";
                        }
                    }
                }

                if (loadSaveConfig.Properties.ContainsKey("/reader/radio/portWritePowerList"))
                {
                    if (loadSaveConfig.Properties["/reader/radio/portWritePowerList"] != "")
                    {
                        string[] splitwriteresult = loadSaveConfig.Properties["/reader/radio/portWritePowerList"].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                        List<string> splitwriteresult_final = new List<string>();
                        foreach (string str in splitwriteresult)
                        {
                            splitwriteresult_final.AddRange(str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                        tempValue = Convert.ToDouble(txtbxWriteValuedBm.Text);
                        try
                        {
                            for (int i = 0; i < splitwriteresult_final.Count; i = i + 2)
                            {
                                if (splitwriteresult_final.Count % 2 == 0 && !string.IsNullOrWhiteSpace(splitwriteresult_final[i]) && Convert.ToInt32(splitwriteresult_final[i]) <= 4)
                                {
                                    if (perPortReadText[Convert.ToInt32(splitwriteresult_final[i]) - 1].Visibility == Visibility.Visible)
                                    {
                                        newValue = Convert.ToDouble(splitwriteresult_final[i + 1]) / 100;
                                        if ((newValue >= sldrReadPwr.Minimum) && (newValue <= sldrReadPwr.Maximum))
                                        {
                                            perPortWriteText[Convert.ToInt32(splitwriteresult_final[i]) - 1].Text = (Convert.ToDouble(splitwriteresult_final[i + 1]) / 100).ToString();
                                        }
                                        else
                                        {
                                            NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portWritePowerList] value ["
                                        + loadSaveConfig.Properties["/reader/radio/portWritePowerList"] + "] failed. Entered value not in correct "
                                        + " format. Please enter within " + sldrWritePwr.Minimum * 100 +
                                        " and " + sldrWritePwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                        + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                                            perPortWriteText[Convert.ToInt32(splitwriteresult_final[i]) - 1].Text = tempValue.ToString();
                                        }
                                    }
                                    else
                                    {
                                        NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portWritePowerList] value ["
                                        + loadSaveConfig.Properties["/reader/radio/portWritePowerList"] + "] failed. Entered antenna port[" + i.ToString() + "] is not available. ");
                                    }
                                }
                                else
                                    throw new FormatException();
                            }
                        }
                        catch (FormatException)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved read power[/reader/radio/portWritePowerList] value ["
                                    + loadSaveConfig.Properties["/reader/radio/portWritePowerList"] + "] failed. Entered value not in correct "
                                    + " format. Please enter within " + sldrWritePwr.Minimum * 100 +
                                    " and " + sldrWritePwr.Maximum * 100 + " cdBm in Load/Save profile. URA gets and sets read power ["
                                    + (tempValue * 100).ToString() + "] cdBm or change to the supported value and reload the configuration");
                            for (int i = 0; i < 4 / 2; i++)
                            {
                                if (perPortWriteText[i].Visibility == Visibility.Visible)
                                    perPortWriteText[i].Text = "20";
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 4 / 2; i++)
                        {
                            if (perPortWriteText[i].Visibility == Visibility.Visible)
                                perPortWriteText[i].Text = "20";
                        }
                    }
                }
            }
            //if (loadSaveConfig.Properties["/application/performanceTuning/Enable"].Equals("true"))
            //{
            if (grpbxPerformanceTuning.IsEnabled)
            {
                if (loadSaveConfig.Properties["/application/performanceTuning/configureGen2SettingsType"].Equals("Manual"))
                {
                    LoadManualGen2ConfigurationFromProfile();
                }
                else if (loadSaveConfig.Properties["/application/performanceTuning/configureGen2SettingsType"].Equals("Auto"))
                {
                    LoadAutoGen2ConfigurationFromProfile();
                }
                else
                {
                    // Notify the error message to the user
                    NotifyLoadSaveConfigErrorMessage("Saved [/application/performanceTuning/configureGen2SettingsType] value ["
                    + loadSaveConfig.Properties["/application/performanceTuning/configureGen2SettingsType"] + "] failed. "
                    + "Invalid option. Accepts only Auto or Manual. URA skips the performance tuning settings given in the "
                    + "configuration and sets to the previous set values ");
                }
            }
            else
            {
                // Performance tuning is not supported by connected reader
                NotifyLoadSaveConfigErrorMessage("Saved performance tuning parameter [/application/performanceTuning/Enable] "
                    + " is not supported by connected reader. URA skips this setting including configure Gen2 Settings");
            }
            //}
            //else if (loadSaveConfig.Properties["/application/performanceTuning/Enable"].Equals("false"))
            //{
            //    if (grpbxPerformanceTuning.IsEnabled)
            //    {
            //        // Performance tuning is not supported by connected reader
            //        NotifyLoadSaveConfigErrorMessage("Saved performance tuning parameter [/application/performanceTuning/Enable] value is set to ["
            //               + loadSaveConfig.Properties["/application/performanceTuning/Enable"] + "]. "
            //               + "URA skips this [/application/performanceTuning/configureGen2SettingsType] "
            //               + "and related parameters and sets performance tuning parameters to previous set values or change to the supported "
            //               + "value and reload the configuration");
            //    }
            //}
            //else
            //{
            //    // Notify the error message to the user
            //    NotifyInvalidLoadConfigOption("/application/performanceTuning/Enable");
            //}

            // Save display options
            // Font size
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/fontSize"))
            {
                if (Utilities.AreAllValidNumericChars(loadSaveConfig.Properties["/application/displayOption/fontSize"]))
                {
                    txtfontSize.Text = loadSaveConfig.Properties["/application/displayOption/fontSize"];
                    txtfontSize.Focus();
                }
                else
                {
                    string tempValueFontSize = txtfontSize.Text;
                    NotifyLoadSaveConfigErrorMessage("Saved Font [/application/displayOption/fontSize] value ["
                           + loadSaveConfig.Properties["/application/displayOption/fontSize"] + "] failed. Please enter only number."
                           + "URA sets Font to previous set value [" + tempValueFontSize + "] or change to the supported value and "
                           + " reload the configuration");
                    txtfontSize.Text = tempValueFontSize;
                }
            }
            else
            {
                txtfontSize.Text = "14";
            }

            // chkEnableTagAging
            if (loadSaveConfig.Properties["/application/displayOption/enableTagAging"].ToLower().Equals("true"))
            {
                chkEnableTagAging.IsChecked = true;
            }
            else if (loadSaveConfig.Properties["/application/displayOption/enableTagAging"].ToLower().Equals("false"))
            {
                chkEnableTagAging.IsChecked = false;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/displayOption/enableTagAging");
            }

            // Refresh [TagResults] for every refresh rate interval set
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/refreshRate"))
            {
                if (Utilities.AreAllValidNumericChars(loadSaveConfig.Properties["/application/displayOption/refreshRate"]))
                {
                    txtRefreshRate.Text = loadSaveConfig.Properties["/application/displayOption/refreshRate"];
                    txtRefreshRate.Focus();
                }
                else
                {
                    string tempValueRefreshRate = txtRefreshRate.Text;
                    NotifyLoadSaveConfigErrorMessage("Saved Refresh rate [/application/displayOption/refreshRate] value ["
                           + loadSaveConfig.Properties["/application/displayOption/refreshRate"] + "] failed. Please enter only number."
                           + "URA sets refresh rate to previous set value [" + tempValueRefreshRate + "] or change to the supported "
                           + "value and reload the configuration");
                    txtRefreshRate.Text = tempValueRefreshRate;
                }
            }
            else
            {
                txtRefreshRate.Text = "100";
            }

            // Select columns to be displayed on Tag Results
            {
                int columnInd = 0;
                foreach (ColumnSelectionForTagResult item in cbxcolumnSelection.Items)
                {
                    if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/tagResultColumnSelection/enable"
                        + item.SelectedColumn))
                    {
                        if (loadSaveConfig.Properties["/application/displayOption/tagResultColumnSelection/enable"
                            + item.SelectedColumn].ToLower().Equals("true"))
                        {
                            selectedColumnList[columnInd].IsColumnChecked = true;
                        }
                        else if (loadSaveConfig.Properties["/application/displayOption/tagResultColumnSelection/enable"
                            + item.SelectedColumn].ToLower().Equals("false"))
                        {
                            selectedColumnList[columnInd].IsColumnChecked = false;
                        }
                        else
                        {
                            // Notify the error message to the user
                            MessageBox.Show("Saved parameter [/application/displayOption/tagResultColumnSelection/enable"
                                + item.SelectedColumn + "] parameter has invalid value [" +
                        loadSaveConfig.Properties["/application/displayOption/tagResultColumnSelection/enable"
                        + item.SelectedColumn] + "]. This parameter accepts only true or false."
                            + " URA sets this parameter to previous set value ", "Universal Reader Assistant",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                            Onlog("Saved parameter [/application/displayOption/tagResultColumnSelection/enable"
                                + item.SelectedColumn + "]  parameter has invalid value [" +
                                        loadSaveConfig.Properties["/application/displayOption/tagResultColumnSelection/enable"
                                        + item.SelectedColumn] + "]. This parameter accepts only true or false."
                                            + " URA sets this parameter to previous set value ");
                        }
                    }
                    else
                    {
                        selectedColumnList[columnInd].IsColumnChecked = false;
                    }
                    ++columnInd;
                }
                cbxcolumnSelection.ItemsSource = null;
                cbxcolumnSelection.ItemsSource = selectedColumnList;
                AddColumnsToGrid(this, new RoutedEventArgs());
                cbxcolumnSelection.InvalidateVisual();
            }

            // Time stamp format
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/tagResultColumn/timeStampFormat"))
            {
                if (loadSaveConfig.Properties["/application/displayOption/tagResultColumn/timeStampFormat"].Equals(""))
                {
                    cbxTimestampFormat.SelectedIndex = 0;
                }
                else
                {
                    cbxTimestampFormat.SelectedIndex = GetIndexOf(cbxTimestampFormat,
                        loadSaveConfig.Properties["/application/displayOption/tagResultColumn/timeStampFormat"],
                        "TimeStamp Format");
                }
            }

            // BigNum Selection
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/tagResult/bigNumSelection"))
            {
                if (loadSaveConfig.Properties["/application/displayOption/tagResult/bigNumSelection"].Equals(""))
                {
                    cbxBigNum.SelectedIndex = 0;
                }
                else
                {
                    cbxBigNum.SelectedIndex = GetIndexOf(cbxBigNum,
                        loadSaveConfig.Properties["/application/displayOption/tagResult/bigNumSelection"],
                        "BigNum Selection");
                }
            }

            // Displays EPC in selected format
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/tagResultColumn/displayEPCAs"))
            {
                if (loadSaveConfig.Properties["/application/displayOption/tagResultColumn/displayEPCAs"].Equals(""))
                {
                    cbxDisplayEPCAs.SelectedIndex = 0;
                }
                else
                {
                    cbxDisplayEPCAs.SelectedIndex = GetIndexOf(cbxDisplayEPCAs,
                        loadSaveConfig.Properties["/application/displayOption/tagResultColumn/displayEPCAs"],
                        "Displays EPC in selected format");
                }
            }

            // Display embedded read data column format
            if ((bool)chkEmbeddedReadData.IsChecked)
            {
                if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/tagResultColumn/displayEmbeddedReadDataAs"))
                {
                    if (loadSaveConfig.Properties["/application/displayOption/tagResultColumn/displayEmbeddedReadDataAs"].Equals(""))
                    {
                        cbxDisplayEmbRdDataAs.SelectedIndex = 0;
                    }
                    else
                    {
                        cbxDisplayEmbRdDataAs.SelectedIndex = GetIndexOf(cbxDisplayEmbRdDataAs,
                            loadSaveConfig.Properties["/application/displayOption/tagResultColumn/displayEmbeddedReadDataAs"],
                            "Displays embedded read data in selected format");
                    }
                }
            }

            // Display HTTP URL & update interval
            if (loadSaveConfig.Properties.ContainsKey("/application/displayOption/saveHttpURL"))
            {
                if (loadSaveConfig.Properties["/application/displayOption/saveHttpURL"].Equals(""))
                {
                    chkDataExtensions.IsChecked = false;
                    txtbxWebUrl.Text = "";
                    txtbxHttpPostIntrvl.Text = "1";
                }
                else
                {
                    httpenabled = true;
                    chkDataExtensions.IsChecked = true;
                    rdBtnHttpPost.IsChecked = true;
                    txtbxWebUrl.Text = loadSaveConfig.Properties["/application/displayOption/saveHttpURL"];
                    txtbxHttpPostIntrvl.Text = loadSaveConfig.Properties["/application/displayOption/saveHttpUpdateInterval"];
                }
            }
        }

        /// <summary>
        /// Load manual gen2 configurations from the profile and set the custom
        /// gen2 settings control in performance tunning sections
        /// </summary>
        private void LoadManualGen2ConfigurationFromProfile()
        {
            // log the parameter
            Onlog("/application/performanceTuning/configureGen2SettingsType" + "Value: " + "Manual");
            chkCustomizeGen2Settings.IsChecked = true;
            try
            {
                if ((model.Equals("Mercury6")) || (model.Equals("Astra-EX")) || (model.Equals("Sargas")) || (model.Equals("Izar"))
                    || (model.Equals("M6e")) || (model.Equals("M6e Micro")) || (model.Equals("M6e Micro USB"))
                    || (model.Equals("M6e Micro USBPro")) || (model.Equals("M6e PRC")) || (model.Equals("M6e JIC")) || (model.Equals("M6e Nano"))
                    || model.Equals("M7e Pico"))
                {
                    switch (loadSaveConfig.Properties["/reader/gen2/BLF"])
                    {
                        case "LINK250KHZ":
                            LINK250KHZ.IsChecked = true; break;
                        case "LINK640KHZ":
                            LINK640KHZ.IsChecked = true; break;
                        default:
                            NotifyLoadSaveConfigErrorMessage("Saved BLF [/reader/gen2/BLF] has invalid BLF value ["
                            + loadSaveConfig.Properties["/reader/gen2/BLF"] + "]"
                            + " getting and setting the BLF value [" + OptimalReaderSettings["/reader/gen2/BLF"]
                            + "] from the reader or change to the supported value and reload the configuration");
                            LINK250KHZ.IsChecked = true;
                            switch (OptimalReaderSettings["/reader/gen2/BLF"])
                            {
                                case "LINK250KHZ":
                                    LINK250KHZ.IsChecked = true; break;
                                case "LINK640KHZ":
                                    LINK640KHZ.IsChecked = true; break;
                            }
                            break;
                    }
                }
                else
                {
                    if (model.Equals("Astra"))
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved BLF [/reader/gen2/BLF] is not supported for connected reader. "
                               + "Connected reader doesn't support BLF. URA skips this settings");
                    }
                    else if (loadSaveConfig.Properties["/reader/gen2/BLF"].Equals("LINK250KHZ"))
                    {
                        LINK250KHZ.IsChecked = true;
                    }
                    else if (loadSaveConfig.Properties["/reader/gen2/BLF"].Equals("LINK640KHZ"))
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved BLF [/reader/gen2/BLF] has invalid BLF value ["
                            + loadSaveConfig.Properties["/reader/gen2/BLF"] + "] for connected reader."
                            + " Getting and setting the BLF value [LINK250KHZ] from the reader or change to the supported value"
                            + " and reload the configuration");
                        LINK250KHZ.IsChecked = true;
                    }
                    else
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved BLF [/reader/gen2/BLF] has invalid BLF value ["
                            + loadSaveConfig.Properties["/reader/gen2/BLF"] + "] for connected reader."
                            + " Getting and setting the BLF value [LINK250KHZ] from the reader or change to the supported value"
                            + " and reload the configuration");
                        LINK250KHZ.IsChecked = true;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Onlog("ArgumentException : " + ex.Message);
                Onlog(ex);
            }
            try
            {
                if ((model.Equals("Mercury6")) || (model.Equals("Astra-EX")) || (model.Equals("Sargas")) || (model.Equals("Izar"))
                   || (model.Equals("M6e")) || (model.Equals("M6e Micro")) || (model.Equals("M6e Micro USB"))
                   || model.Equals("M6e Micro USBPro") || (model.Equals("M6e PRC")) || (model.Equals("M6e JIC")) || (model.Equals("M6e Nano"))
                   || model.Equals("M7e Pico"))
                {
                    switch (loadSaveConfig.Properties["/reader/gen2/tari"])
                    {
                        case "TARI_6_25US":
                            tari625.IsChecked = true; break;
                        case "TARI_12_5US":
                            tari125.IsChecked = true; break;
                        case "TARI_25US":
                            tari25.IsChecked = true; break;
                        default:
                            NotifyLoadSaveConfigErrorMessage("Saved Tari [/reader/gen2/tari] has invalid tari value ["
                            + loadSaveConfig.Properties["/reader/gen2/tari"] + "]."
                            + " Getting and setting the tari value [" + OptimalReaderSettings["/reader/gen2/tari"]
                            + "] from the reader or change to the supported value and reload the configuration");
                            switch (OptimalReaderSettings["/reader/gen2/tari"])
                            {
                                case "TARI_6_25US":
                                    tari625.IsChecked = true; break;
                                case "TARI_12_5US":
                                    tari125.IsChecked = true; break;
                                case "TARI_25US":
                                    tari25.IsChecked = true; break;
                            }
                            break;
                    }
                }
                else
                {
                    if (model.Equals("Astra"))
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved tari [/reader/gen2/tari] is not supported for connected reader. "
                                + "Connected reader doesn't support tari. URA skips this settings");
                    }
                    else if (loadSaveConfig.Properties["/reader/gen2/tari"] != "")
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved tari [/reader/gen2/tari] is not supported for connected reader. "
                            + "Connected reader doesn't support tari. URA skips this settings");
                    }
                    grpbxtTari.IsEnabled = false;
                }
            }
            catch (ArgumentException ex)
            {
                Onlog("ArgumentException : " + ex.Message);
                Onlog(ex);
            }
            catch (ReaderCodeException ex)
            {
                Onlog("ReaderCodeException : " + ex.Message);
                Onlog(ex);
            }

            try
            {
                if (model.Equals("Astra"))
                {
                    NotifyLoadSaveConfigErrorMessage("Saved tagEncoding [/reader/gen2/tagEncoding] is not supported for connected reader. "
                            + "Connected reader doesn't support tagEncoding. URA skips this settings");
                    FM0.IsEnabled = false;
                    M2.IsEnabled = false;
                    M4.IsEnabled = false;
                    M8.IsEnabled = false;
                }
                else
                {
                    switch (loadSaveConfig.Properties["/reader/gen2/tagEncoding"])
                    {
                        case "FM0":
                            if ((model.Equals("Mercury6")) || (model.Equals("Astra-EX")) || (model.Equals("Sargas")) || (model.Equals("Izar"))
                                || (model.Equals("M6e")) || (model.Equals("M6e Micro")) || (model.Equals("M6e Micro USB"))
                                || (model.Equals("M6e Micro USBPro")) || (model.Equals("M6e PRC")) || (model.Equals("M6e JIC")) || (model.Equals("M6e Nano"))
                                || (model.Equals("M7e Pico")))
                            {
                                FM0.IsChecked = true;
                            }
                            else
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved tagEncoding [/reader/gen2/tagEncoding] has invalid tag encoding value ["
                                   + loadSaveConfig.Properties["/reader/gen2/tagEncoding"] + "] for connected reader."
                                   + " Getting and setting to the tag encoding value [" + OptimalReaderSettings["/reader/gen2/tagEncoding"]
                                   + "] or change to the supported value"
                                   + " and reload the configuration");
                                SetTagEncodingBasedOnReader();
                                FM0.IsEnabled = false;
                            }
                            break;
                        case "M2":
                            M2.IsChecked = true; break;
                        case "M4":
                            M4.IsChecked = true; break;
                        case "M8":
                            M8.IsChecked = true; break;
                        default:
                            NotifyLoadSaveConfigErrorMessage("Saved tagEncoding [/reader/gen2/tagEncoding] has invalid tag encoding value ["
                            + loadSaveConfig.Properties["/reader/gen2/tagEncoding"] + "]."
                            + " Getting and setting to the tag encoding value [" + OptimalReaderSettings["/reader/gen2/tagEncoding"]
                            + "] or change to the supported value"
                            + " and reload the configuration");
                            SetTagEncodingBasedOnReader();
                            break;
                    }
                }
            }
            catch (ArgumentException)
            {
                FM0.IsEnabled = false;
                M2.IsEnabled = false;
                M4.IsEnabled = false;
                M8.IsEnabled = false;
            }
            try
            {
                switch (loadSaveConfig.Properties["/reader/gen2/session"])
                {
                    case "S0":
                        S0.IsChecked = true; break;
                    case "S1":
                        S1.IsChecked = true; break;
                    case "S2":
                        S2.IsChecked = true; break;
                    case "S3":
                        S3.IsChecked = true; break;
                    default:
                        NotifyLoadSaveConfigErrorMessage("Saved session [/reader/gen2/session] has invalid session value ["
                            + loadSaveConfig.Properties["/reader/gen2/session"] + "]."
                            + " Getting and setting the session value [" + OptimalReaderSettings["/reader/gen2/session"] + "] from the reader"
                            + " or change to the supported value"
                            + " and reload the configuration");
                        switch (OptimalReaderSettings["/reader/gen2/session"])
                        {
                            case "S0":
                                S0.IsChecked = true; break;
                            case "S1":
                                S1.IsChecked = true; break;
                            case "S2":
                                S2.IsChecked = true; break;
                            case "S3":
                                S3.IsChecked = true; break;
                        }
                        break;
                }
            }
            catch (ArgumentException)
            {
                S0.IsEnabled = false;
                S1.IsEnabled = false;
                S2.IsEnabled = false;
                S3.IsEnabled = false;
            }
            try
            {
                switch (loadSaveConfig.Properties["/reader/gen2/target"])
                {
                    case "A":
                        A.IsChecked = true; break;
                    case "B":
                        B.IsChecked = true; break;
                    case "AB":
                        AB.IsChecked = true; break;
                    case "BA":
                        BA.IsChecked = true; break;
                    default:
                        NotifyLoadSaveConfigErrorMessage("Saved target [/reader/gen2/target] has invalid session value ["
                            + loadSaveConfig.Properties["/reader/gen2/target"] + "]."
                            + " Getting and setting the session value [" + OptimalReaderSettings["/reader/gen2/target"] + "] from the reader"
                            + " or change to the supported value"
                            + " and reload the configuration");
                        switch (OptimalReaderSettings["/reader/gen2/target"])
                        {
                            case "A":
                                A.IsChecked = true; break;
                            case "B":
                                B.IsChecked = true; break;
                            case "AB":
                                AB.IsChecked = true; break;
                            case "BA":
                                BA.IsChecked = true; break;
                        }
                        break;
                }
            }
            catch (FeatureNotSupportedException)
            {
                A.IsEnabled = false;
                B.IsEnabled = false;
                AB.IsEnabled = false;
                BA.IsEnabled = false;
            }
            try
            {
                if (model.Equals("Astra"))
                {
                    NotifyLoadSaveConfigErrorMessage("Saved q [/reader/gen2/q] is not supported for connected reader. "
                            + "Connected reader doesn't support q. URA skips this settings");
                    xStaticQ.IsEnabled = false;
                    xDynamicQ.IsEnabled = false;
                }
                else if (loadSaveConfig.Properties["/reader/gen2/q"] == "DynamicQ")
                {
                    xDynamicQ.IsChecked = true;
                    xQvalue.SelectedIndex = -1;
                }
                else if (loadSaveConfig.Properties["/reader/gen2/q"] == "StaticQ")
                {
                    xStaticQ.IsChecked = true;
                    xQvalue.IsEnabled = true;
                    try
                    {
                        if (Convert.ToInt32(loadSaveConfig.Properties["/application/performanceTuning/staticQValue"])
                            <= xQvalue.Items.Count)
                        {
                            xQvalue.SelectedIndex = Convert.ToInt32(loadSaveConfig.Properties[
                                "/application/performanceTuning/staticQValue"]);
                        }
                        else
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved static q [/application/performanceTuning/staticQValue] has invalid StaticQ value ["
                            + loadSaveConfig.Properties["/application/performanceTuning/staticQValue"] + "]. Enter Q value within 0 to 15"
                            + " Getting and setting the q value [" + OptimalReaderSettings["/application/performanceTuning/staticQValue"]
                            + "] from the reader or change to the supported value and reload the configuration");
                            xQvalue.SelectedIndex = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                        }
                    }
                    catch (Exception)
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved static q [/application/performanceTuning/staticQValue] has invalid StaticQ value ["
                            + loadSaveConfig.Properties["/application/performanceTuning/staticQValue"] + "]."
                            + " Getting and setting the q value [" + OptimalReaderSettings["/application/performanceTuning/staticQValue"]
                            + "] from the reader or change to the supported value and reload the configuration");
                        xQvalue.SelectedIndex = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                    }
                }
                else
                {
                    if (OptimalReaderSettings["/reader/gen2/q"] == "DynamicQ")
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved q [/reader/gen2/q] has invalid q value ["
                            + loadSaveConfig.Properties["/reader/gen2/q"] + "]."
                            + " Getting and setting the q value [" + OptimalReaderSettings["/reader/gen2/q"] + "] from the reader"
                            + " or change to the supported value"
                            + " and reload the configuration");
                        xDynamicQ.IsChecked = true;
                        xQvalue.SelectedIndex = -1;
                    }
                    else if (OptimalReaderSettings["/reader/gen2/q"] == "StaticQ")
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved q [/reader/gen2/q] has invalid q value ["
                            + loadSaveConfig.Properties["/reader/gen2/q"] + "]."
                            + " Getting and setting the q value [" + OptimalReaderSettings["/reader/gen2/q"] + "] from the reader"
                            + " or change to the supported value"
                            + " and reload the configuration");
                        xStaticQ.IsChecked = true;
                        xQvalue.IsEnabled = true;
                        xQvalue.SelectedIndex = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                    }
                }
            }
            catch (FeatureNotSupportedException)
            {
                xStaticQ.IsEnabled = false;
                xDynamicQ.IsEnabled = false;
            }

            try
            {
                if ((model.Equals("Mercury6")) || (model.Equals("Astra-EX")) || (model.Equals("Sargas")) || (model.Equals("Izar"))
                  || (model.Equals("M6e")) || (model.Equals("M6e Micro")) || (model.Equals("M6e Micro USB"))
                  || model.Equals("M6e Micro USBPro") || (model.Equals("M6e PRC")) || (model.Equals("M6e JIC")) || (model.Equals("M6e Nano"))
                  || model.Equals("M7e Pico"))
                {

                    //NotifyLoadSaveConfigErrorMessage("Saved InitQ [/reader/gen2/initQ] has invalid q value ["
                    //       + loadSaveConfig.Properties["/reader/gen2/initQ"] + "]."
                    //       + " Getting and setting the q value [" + OptimalReaderSettings["/reader/gen2/q"] + "] from the reader"
                    //       + " or change to the supported value"
                    //       + " and reload the configuration");
                    yDynamic_InitQ.IsChecked = true;
                    xDInitQValue1.IsEnabled = true;
                    xDInitQValue1.Text = Convert.ToString(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
                }
                else
                {
                    NotifyLoadSaveConfigErrorMessage("Saved initQ [/reader/gen2/initQ] has invalid value ["
                           + loadSaveConfig.Properties["/reader/gen2/initQ"] + "]."
                           + " Getting and setting the init q value [" + OptimalReaderSettings["/reader/gen2/initQ"] + "] from the reader"
                           + " or change to the supported value"
                           + " and reload the configuration");
                    yDynamic_InitQ.IsChecked = false;
                    xDInitQValue1.IsEnabled = false;
                    //Qvalue.SelectedIndex = Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/staticQValue"]);
                }

            }
            catch (FeatureNotSupportedException)
            {
                yDynamic_InitQ.IsChecked = false;
                xDInitQValue1.IsEnabled = false;
            }
        }

        /// <summary>
        /// Load auto gen2 configurations from the profile and set the
        /// controls in performance tunning section
        /// </summary>
        private void LoadAutoGen2ConfigurationFromProfile()
        {
            chkCustomizeGen2Settings.IsChecked = false;
            // Automatically adjust as population changes
            if (loadSaveConfig.Properties["/application/performanceTuning/automaticallyAdjustAsPopulationChanges"].ToLower().Equals("true"))
            {
                if (rdbtnAutoAdjstAsPoplChngs.IsEnabled)
                {
                    rdbtnAutoAdjstAsPoplChngs.IsChecked = true;
                    rdbtnOptmzExtmdNoTagsInField.IsChecked = false;
                }
                else
                {
                    // Embedded read data is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/performanceTuning/automaticallyAdjustAsPopulationChanges] "
                        + " is not supported by connected reader. URA skips this setting.");
                    rdbtnAutoAdjstAsPoplChngs.IsEnabled = false;
                }
            }
            else if (loadSaveConfig.Properties["/application/performanceTuning/automaticallyAdjustAsPopulationChanges"].ToLower().Equals("false"))
            {
                rdbtnAutoAdjstAsPoplChngs.IsChecked = false;
                rdbtnOptmzExtmdNoTagsInField.IsChecked = true;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/performanceTuning/automaticallyAdjustAsPopulationChanges");
            }

            // Optimize for estimated number of tags in field

            if (loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField"].ToLower().Equals("true"))
            {
                if (rdbtnOptmzExtmdNoTagsInField.IsEnabled)
                {
                    rdbtnOptmzExtmdNoTagsInField.IsChecked = true;
                    rdbtnAutoAdjstAsPoplChngs.IsChecked = false;
                    string tempValue = txtTagsNum.Text;
                    if (Utilities.AreAllValidNumericChars(
                        loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"]))
                    {
                        try
                        {
                            int tagcount = Convert.ToInt32(
                                loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"]);
                            if ((tagcount >= 1) && (tagcount <= 99999))
                            {
                                txtTagsNum.Text = loadSaveConfig.Properties[
                                    "/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"];
                            }
                            else
                            {
                                NotifyLoadSaveConfigErrorMessage("Saved estimated number of tags "
                                + "in field [/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField] value ["
                                + loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"]
                                + "] failed. Please enter number within 1 to 99999. URA sets estimated number of tags in field to previous "
                                + "set value [" + tempValue + "] or change to the supported value and reload the configuration");
                                txtTagsNum.Text = tempValue;
                            }
                        }
                        catch (Exception)
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved estimated number of tags "
                                + "in field [/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField] value ["
                                + loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"]
                                + "] failed. Please enter number within 1 to 99999. URA sets estimated number of tags in field to previous "
                                + "set value [" + tempValue + "] or change to the supported value and reload the configuration");
                            txtTagsNum.Text = tempValue;
                        }
                    }
                    else
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved estimated number of tags "
                            + "in field [/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField] value ["
                               + loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField"]
                               + "] failed. Please enter only positive number. URA sets estimated number of tags in field to previous "
                               + "set value [" + tempValue + "] or change to the supported value and reload the configuration");
                        txtTagsNum.Text = tempValue;
                    }
                    // update static q count
                    UpDownCounterTextChange(this);
                }
                else
                {
                    // Embedded read data is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField] "
                        + " is not supported by connected reader. URA skips this setting.");
                    rdbtnOptmzExtmdNoTagsInField.IsEnabled = false;
                    stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
                    //stkpnlRdDistVsrdRate.IsEnabled = false;
                    //rdBtnSlBstChforPoplSize.IsEnabled = false;
                }
            }
            else if (loadSaveConfig.Properties["/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField"].ToLower().Equals("false"))
            {
                rdbtnOptmzExtmdNoTagsInField.IsChecked = false;
                if (!(model.Equals("Astra")))
                {
                    rdbtnAutoAdjstAsPoplChngs.IsChecked = true;
                }
                txtTagsNum.Text = "1";
                // update static q count
                UpDownCounterTextChange(this);
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField");
            }

            // Read Distance vs. Read Rate

            if (loadSaveConfig.Properties.ContainsKey("/application/performanceTuning/readDistancevsReadRate"))
            {
                if (stkpnlRdDistVsrdRate.IsEnabled)
                {
                    try
                    {
                        double readDistValue = 0.0;
                        readDistValue = Convert.ToDouble(loadSaveConfig.Properties["/application/performanceTuning/readDistancevsReadRate"]);
                        if ((readDistValue >= sldrRdDistVsrdRate.Minimum)
                            && (readDistValue <= sldrRdDistVsrdRate.Maximum))
                        {
                            sldrRdDistVsrdRate.Value = Convert.ToDouble(
                                loadSaveConfig.Properties["/application/performanceTuning/readDistancevsReadRate"]);
                        }
                        else
                        {
                            NotifyLoadSaveConfigErrorMessage("Saved read distance vs. read rate "
                            + "[/application/performanceTuning/readDistancevsReadRate] value ["
                               + loadSaveConfig.Properties["/application/performanceTuning/readDistancevsReadRate"]
                               + "] failed. Please enter only positive number within " + sldrRdDistVsrdRate.Minimum
                               + " and " + sldrRdDistVsrdRate.Maximum + "."
                               + " URA sets to previous set value or change to the supported value and reload the configuration");
                        }
                    }
                    catch (Exception ex)
                    {
                        NotifyLoadSaveConfigErrorMessage("Load parameter [/application/performanceTuning/readDistancevsReadRate]: "
                               + ex.Message + "Please enter only positive number within " + sldrRdDistVsrdRate.Minimum + " and "
                               + sldrRdDistVsrdRate.Maximum + "."
                               + " URA sets to previous set value or change to the supported value and reload the configuration");
                    }
                }
                else
                {
                    // Embedded read data is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/performanceTuning/readDistancevsReadRate] "
                        + " is not supported by connected reader. URA skips this setting.");
                    stkpnlRdDistVsrdRate.IsEnabled = false;
                    sldrRdDistVsrdRate.Value = 0;
                }
            }
            else
            {
                sldrRdDistVsrdRate.Value = 0;
            }


            // Select best choice for population size

            if (loadSaveConfig.Properties[
                "/application/performanceTuning/selectBestChoiceForPopulationSize"].ToLower().Equals("true"))
            {
                if (rdBtnSlBstChforPoplSize.IsEnabled)
                {
                    rdBtnSlBstChforPoplSize.IsChecked = true;
                    rdBtnTagsRespondOption.IsChecked = false;
                }
                else
                {
                    // Embedded read data is not supported by connected reader
                    NotifyLoadSaveConfigErrorMessage("Saved parameter [/application/performanceTuning/selectBestChoiceForPopulationSize] "
                        + " is not supported by connected reader. URA skips this setting.");
                    ////rdbtnOptmzExtmdNoTagsInField.IsEnabled = false;
                    ////stkpnlOptmzExtmdNoTagsInField.IsEnabled = false;
                    //stkpnlRdDistVsrdRate.IsEnabled = false;
                    rdBtnSlBstChforPoplSize.IsEnabled = false;
                }
            }
            else if (loadSaveConfig.Properties["/application/performanceTuning/selectBestChoiceForPopulationSize"].ToLower().Equals("false"))
            {
                rdBtnSlBstChforPoplSize.IsChecked = false;
                rdBtnTagsRespondOption.IsChecked = true;
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/performanceTuning/selectBestChoiceForPopulationSize");
            }

            // Customize tag response rate
            if (loadSaveConfig.Properties["/application/performanceTuning/customizeTagResponseRate"].ToLower().Equals("true"))
            {
                rdBtnTagsRespondOption.IsChecked = true;
                rdBtnSlBstChforPoplSize.IsChecked = false;
                try
                {
                    double tagResponseRate = 0.0;
                    tagResponseRate = Convert.ToDouble(loadSaveConfig.Properties[
                        "/application/performanceTuning/customizeTagResponseRate/TagResponseRate"]);
                    if ((tagResponseRate >= sldrTagrspLessVsTagrspMore.Minimum)
                        && (tagResponseRate <= sldrTagrspLessVsTagrspMore.Maximum))
                    {
                        sldrTagrspLessVsTagrspMore.Value = Convert.ToDouble(
                            loadSaveConfig.Properties["/application/performanceTuning/customizeTagResponseRate/TagResponseRate"]);
                    }
                    else
                    {
                        NotifyLoadSaveConfigErrorMessage("Saved Customize tag response rate "
                        + "[/application/performanceTuning/customizeTagResponseRate/TagResponseRate] value ["
                           + loadSaveConfig.Properties["/application/performanceTuning/customizeTagResponseRate/TagResponseRate"]
                           + "] failed. Please enter only positive number within " + sldrTagrspLessVsTagrspMore.Minimum + " and "
                           + sldrTagrspLessVsTagrspMore.Maximum + "."
                           + " URA sets to previous set value or change to the supported value and reload the configuration");
                    }
                }
                catch (Exception ex)
                {
                    NotifyLoadSaveConfigErrorMessage("Load parameter [/application/performanceTuning/customizeTagResponseRate]: "
                        + ex.Message + " Please enter only positive number within " + sldrTagrspLessVsTagrspMore.Minimum + " and "
                           + sldrTagrspLessVsTagrspMore.Maximum + "."
                           + " URA sets to previous set value or change to the supported value and reload the configuration");
                }
            }
            else if (loadSaveConfig.Properties["/application/performanceTuning/customizeTagResponseRate"].ToLower().Equals("false"))
            {
                if (model.Equals("Astra"))
                {
                    // If connected reader is astra and if /application/performanceTuning/selectBestChoiceForPopulationSize parameter
                    // is set to false when loading configurations of other reader. Set  /application/performanceTuning/customizeTagResponseRate
                    // parameter as true.
                    rdBtnTagsRespondOption.IsChecked = true;
                    sldrTagrspLessVsTagrspMore.Value = 0;
                }
                else
                {
                    rdBtnTagsRespondOption.IsChecked = false;
                    rdBtnSlBstChforPoplSize.IsChecked = true;
                    sldrTagrspLessVsTagrspMore.Value = 0;
                }
            }
            else
            {
                // Notify the error message to the user
                NotifyInvalidLoadConfigOption("/application/performanceTuning/customizeTagResponseRate");
            }
        }

        /// <summary>
        /// Method to set tag encoding based on reader
        /// </summary>
        private void SetTagEncodingBasedOnReader()
        {
            switch (OptimalReaderSettings["/reader/gen2/tagEncoding"])
            {
                case "FM0":
                    FM0.IsChecked = true;
                    break;
                case "M2":
                    M2.IsChecked = true; break;
                case "M4":
                    M4.IsChecked = true; break;
                case "M8":
                    M8.IsChecked = true; break;
            }
        }

        /// <summary>
        /// Get the index of specified element in the combo-box
        /// </summary>
        /// <param name="element">Combo-box control</param>
        /// <param name="FindIndexOf">string to be searched in the list of combo-box elements</param>
        /// <returns>index of the specified string</returns>
        private int GetIndexOf(Control element, string FindIndexOf, string cbxOptionName)
        {
            int index = -1;
            ComboBox comboBox = (ComboBox)element;
            bool iselementFound = false;
            foreach (var cmbItem in comboBox.Items)
            {
                ComboBoxItem cbItem = cmbItem as ComboBoxItem;
                index++;
                if (cbItem.Content.ToString() == FindIndexOf)
                {
                    iselementFound = true;
                    break;
                }
            }
            if (!iselementFound)
            {
                // if element was not found return index of previous state
                index = comboBox.SelectedIndex;
                NotifyLoadSaveConfigErrorMessage("Saved option [" + FindIndexOf + "] not found in [" + cbxOptionName + "]."
                    + " Invalid option, URA sets to previous Chosen [" + comboBox.Text + "] as option for [" + cbxOptionName + "].");
                //MessageBox.Show("Saved option [" + FindIndexOf + "] not found in [" + cbxOptionName + "]."
                //    +" Invalid option, URA sets to previous Chosen [" + comboBox.Text + "] as option for [" + cbxOptionName + "].",
                //    "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //Onlog("Saved option [" + FindIndexOf + "] not found in [" + cbxOptionName + "]."
                //    +" Invalid option, URA sets to previous Chosen [" + comboBox.Text + "] as option for [" + cbxOptionName + "].");
            }
            return index;
        }

        /// <summary>
        /// Get the index of the specified region element
        /// </summary>
        /// <param name="element">Combo-box control</param>
        /// <param name="FindIndexOf">region to be searched in combo-box</param>
        /// <returns>index of the specified region</returns>
        private int GetRegionIndexOf(Control element, string FindIndexOf, out bool status)
        {
            int index = -1;
            status = false;
            ComboBox comboBox = (ComboBox)element;
            bool isRegionFound = false;
            foreach (var cmbItem in comboBox.Items)
            {
                index++;
                if (cmbItem.ToString() == FindIndexOf)
                {
                    isRegionFound = true;
                    status = true;
                    break;
                }
            }
            if (!isRegionFound)
            {
                // If no region found then return the index of the first element in the combo-box
                index = 0;
                status = false;
            }
            return index;
        }

        /// <summary>
        /// Get the parameters to be saved in the configuration file
        /// </summary>
        /// <returns>List of parameters to be saved int the configuration file</returns>
        private Dictionary<string, string> GetParametersToSave()
        {
            Dictionary<string, string> saveConfigurationList = new Dictionary<string, string>();

            // Save reader uri
            if ((bool)rdbtnLocalConnection.IsChecked)
            {
                saveConfigurationList.Add("/application/connect/readerType", "SerialReader");
                saveConfigurationList.Add("/application/connect/readerURI", uri);
            }
            else if ((bool)rdbtnCustomTransportConnection.IsChecked)
            {
                saveConfigurationList.Add("/application/connect/readerType", "CustomTransport");
                saveConfigurationList.Add("/application/connect/readerURI", uri);
            }
            else
            {
                saveConfigurationList.Add("/application/connect/readerType", "FixedReader");
                saveConfigurationList.Add("/application/connect/readerURI", uri);
            }

            // Baud rate
            if (cbxBaudRate.Visibility == System.Windows.Visibility.Visible)
            {
                if (cbxBaudRate.SelectedValue.ToString() != "Select")
                {
                    saveConfigurationList.Add("/reader/baudRate", cbxBaudRate.SelectedValue.ToString());
                }
                else
                {
                    saveConfigurationList.Add("/reader/baudRate", "");
                }
            }
            else
            {
                saveConfigurationList.Add("/reader/baudRate", "");
            }

            // Enable transport logging
            if ((bool)chkEnableTransportLogging.IsChecked)
            {
                saveConfigurationList.Add("/application/connect/enableTransportLogging",
                    ((bool)chkEnableTransportLogging.IsChecked).ToString().ToLower());
            }
            else
            {
                saveConfigurationList.Add("/application/connect/enableTransportLogging",
                    ((bool)chkEnableTransportLogging.IsChecked).ToString().ToLower());
            }

            // Set region
            if (regioncombo.IsEnabled)
            {
                if (regioncombo.SelectedValue.ToString() != "Select")
                {
                    saveConfigurationList.Add("/reader/region/id", regioncombo.SelectedValue.ToString());
                }
                else
                {
                    saveConfigurationList.Add("/reader/region/id", "");
                }
            }
            else
            {
                saveConfigurationList.Add("/reader/region/id", regioncombo.SelectedValue.ToString());
            }

            // Set read behavior
            if ((bool)rdBtnReadOnce.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/ReadBehaviour", "ReadOnce");
                saveConfigurationList.Add("/application/readwriteOption/readOnceTimeout", txtbxreadOnceTimeout.Text);
                saveConfigurationList.Add("/reader/read/asyncOnTime", txtRFOnTimeout.Text);
                saveConfigurationList.Add("/reader/read/asyncOffTime", txtRFOffTimeout.Text);
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/ReadBehaviour", "ReadContinuously");
                saveConfigurationList.Add("/reader/read/asyncOnTime", txtRFOnTimeout.Text);
                saveConfigurationList.Add("/reader/read/asyncOffTime", txtRFOffTimeout.Text);
                saveConfigurationList.Add("/application/readwriteOption/readOnceTimeout", txtbxreadOnceTimeout.Text);
            }

            // Set fast search
            if ((bool)chkEnableFastSearch.IsEnabled)
            {
                if ((bool)chkEnableFastSearch.IsChecked)
                {
                    saveConfigurationList.Add("/application/readwriteOption/enableFastSearch", "true");
                }
                else
                {
                    saveConfigurationList.Add("/application/readwriteOption/enableFastSearch", "false");
                }
            }
            else
            {
                // If fast search is not supported by connected reader, treat this as false
                saveConfigurationList.Add("/application/readwriteOption/enableFastSearch", "false");
            }

            // Antenna switching
            if ((bool)rdBtnEqualSwitching.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/switchingMethod", "Equal");
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/switchingMethod", "Dynamic");
            }

            // Save protocol configurations
            StringBuilder protocols = new StringBuilder();
            foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackpanel6))
            {
                if ((bool)child.IsChecked)
                {
                    protocols.Append(child.Content.ToString());
                    protocols.Append(" ,");
                }
            }
            saveConfigurationList.Add("/application/readwriteOption/Protocols", protocols.ToString().TrimEnd(','));

            // Save antenna configurations
            StringBuilder antennas = new StringBuilder();
            foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackPanel8))
            {
                if ((bool)child.IsChecked)
                {
                    // Stack panel has antenna detection check-box as a child. Don't include this in antenna list.
                    if (!(child.Content.Equals("Antenna Detection")))
                    {
                        antennas.Append(child.Content.ToString());
                        antennas.Append(",");
                    }
                }
            }
            saveConfigurationList.Add("/application/readwriteOption/Antennas", antennas.ToString().TrimEnd(','));

            // Antenna detection
            if (chkbxAntennaDetection.IsEnabled)
            {
                if ((bool)chkbxAntennaDetection.IsChecked)
                {
                    saveConfigurationList.Add("/reader/antenna/checkPort", "true");
                }
                else
                {
                    saveConfigurationList.Add("/reader/antenna/checkPort", "false");
                }
            }
            else
            {
                // If the connected reader doesn't support antenna detection
                saveConfigurationList.Add("/reader/antenna/checkPort", "false");
            }

            // Save antenna multiplexing configurations
            StringBuilder antMux = new StringBuilder();
            if (chbxOne.IsEnabled)
            {
                if ((bool)chbxOne.IsChecked)
                {
                    antMux.Append("1");
                    antMux.Append(",");
                }
            }
            if (chbxTwo.IsEnabled)
            {
                if ((bool)chbxTwo.IsChecked)
                {
                    antMux.Append("2");
                    antMux.Append(",");
                }
            }
            if (chbxThree.IsEnabled)
            {
                if ((bool)chbxThree.IsChecked)
                {
                    antMux.Append("3");
                    antMux.Append(",");
                }
            }
            if (chbxFour.IsEnabled)
            {
                if ((bool)chbxFour.IsChecked)
                {
                    antMux.Append("4");
                    antMux.Append(",");
                }
            }
            saveConfigurationList.Add("/application/readwriteOption/portswitchgpos", antMux.ToString().TrimEnd(','));


            //input list
            StringBuilder iList = new StringBuilder();
            int[] inputList = (int[])(objReader.ParamGet("/reader/gpio/inputList"));
            foreach (int num in inputList)
            {
                iList.Append(num.ToString());
                iList.Append(",");
            }
            saveConfigurationList.Add("/application/readwriteOption/inputList", iList.ToString().TrimEnd(','));

            //output list
            StringBuilder oList = new StringBuilder();
            int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
            foreach (int num in outputList)
            {
                oList.Append(num.ToString());
                oList.Append(",");
            }
            saveConfigurationList.Add("/application/readwriteOption/outputList", oList.ToString().TrimEnd(','));

            // Embedded read data
            if (ReadDataGroupBox.IsEnabled)
            {
                if ((bool)chkEmbeddedReadData.IsChecked)
                {
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData", "true");

                    // MemBank
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/MemBank",
                        cbxReadDataBank.Text);

                    // Start address
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/StartAddress",
                        txtembReadStartAddr.Text);

                    // Number of words to read
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead",
                        txtembReadLength.Text);
                }
                else
                {
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData", "false");

                    // MemBank
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/MemBank",
                        ((cbxReadDataBank.Text != "") ? cbxReadDataBank.Text : "TID"));

                    // Start address
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/StartAddress",
                        txtembReadStartAddr.Text);

                    // Number of words to read
                    saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead",
                        txtembReadLength.Text);
                }
            }
            else
            {
                // If connected reader doesn't support embedded read data
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData", "false");

                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/MemBank",
                    cbxReadDataBank.Text);

                // Start address
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/StartAddress",
                    txtembReadStartAddr.Text);

                // Number of words to read
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead",
                    txtembReadLength.Text);
            }

            // UniqueByData
            if ((bool)chkUniqueByData.IsChecked)
            {
                saveConfigurationList.Add("/reader/tagReadData/uniqueByData", "true");
            }
            else
            {
                saveConfigurationList.Add("/reader/tagReadData/uniqueByData", "false");
            }

            // Show failed read data
            if ((bool)chkShowFailedDataReads.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/uniqueByData/ShowFailedDataRead",
                    "true");
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/enableEmbeddedReadData/uniqueByData/ShowFailedDataRead",
                    "false");
            }

            // Apply filter
            if ((bool)chkApplyFilter1.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1", "true");

                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterMemBank",
                   MultiFilters1.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterStartAddress",
                   MultiFilters1.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterData",
                   MultiFilters1.txtFilterData.Text);
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1", "false");
                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterMemBank",
                   MultiFilters1.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterStartAddress",
                   MultiFilters1.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/FilterData",
                   MultiFilters1.txtFilterData.Text);
            }
            if ((bool)MultiFilters1.chkFilterInvert.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/InvertFilter", "true");
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter1/InvertFilter", "false");
            }

            saveConfigurationList.Add("/application/readwriteOption/applyFilter1/Target",
                   MultiFilters1.cbxFilterTarget.Text);
            saveConfigurationList.Add("/application/readwriteOption/applyFilter1/Action",
                   MultiFilters1.cbxFilterAction.Text);

            if ((bool)chkApplyFilter2.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2", "true");

                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterMemBank",
                   MultiFilters2.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterStartAddress",
                   MultiFilters2.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterData",
                   MultiFilters2.txtFilterData.Text);
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2", "false");
                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterMemBank",
                   MultiFilters2.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterStartAddress",
                   MultiFilters2.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/FilterData",
                   MultiFilters2.txtFilterData.Text);
            }

            if ((bool)MultiFilters2.chkFilterInvert.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/InvertFilter", "true");
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter2/InvertFilter", "false");
            }

            saveConfigurationList.Add("/application/readwriteOption/applyFilter2/Target",
                    MultiFilters2.cbxFilterTarget.Text);
            saveConfigurationList.Add("/application/readwriteOption/applyFilter2/Action",
                    MultiFilters2.cbxFilterAction.Text);

            if ((bool)chkApplyFilter3.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3", "true");

                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterMemBank",
                   MultiFilters3.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterStartAddress",
                   MultiFilters3.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterData",
                   MultiFilters3.txtFilterData.Text);
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3", "false");
                // MemBank
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterMemBank",
                   MultiFilters3.cbxFilterMemBank.Text);

                // Filter start address
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterStartAddress",
                   MultiFilters3.txtFilterStartAddr.Text);

                // Filter data
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/FilterData",
                   MultiFilters3.txtFilterData.Text);
            }

            if ((bool)MultiFilters3.chkFilterInvert.IsChecked)
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/InvertFilter", "true");
            }
            else
            {
                saveConfigurationList.Add("/application/readwriteOption/applyFilter3/InvertFilter", "false");
            }
            saveConfigurationList.Add("/application/readwriteOption/applyFilter3/Target",
                    MultiFilters3.cbxFilterTarget.Text);
            saveConfigurationList.Add("/application/readwriteOption/applyFilter3/Action",
                    MultiFilters3.cbxFilterAction.Text);
            // Save Performance tuning settings

            if (rdbtnglobal.IsChecked == true)
                saveConfigurationList.Add("/application/performanceTuning/rfPowerSettingGlobal", "true");
            else
                saveConfigurationList.Add("/application/performanceTuning/rfPowerSettingGlobal", "false");

            saveConfigurationList.Add("/reader/radio/readPower", (Convert.ToDouble(txtbxValuedBm.Text) * 100).ToString());
            saveConfigurationList.Add("/reader/radio/writePower", (Convert.ToDouble(txtbxWriteValuedBm.Text) * 100).ToString());

            TextBox[] perPortReadText = { txtReadPowerAnt1, txtReadPowerAnt2, txtReadPowerAnt3, txtReadPowerAnt4 };
            TextBox[] perPortWriteText = { txtWritePowerAnt1, txtWritePowerAnt2, txtWritePowerAnt3, txtWritePowerAnt4 };
            string tempstr = "";

            for (int k = 1; k <= 4; k++)
            {
                if (perPortReadText[k - 1].Visibility == Visibility.Visible)
                {
                    if (!string.IsNullOrWhiteSpace(perPortReadText[k - 1].Text))
                        tempstr = tempstr + "[" + k.ToString() + "," + (Convert.ToDouble(perPortReadText[k - 1].Text) * 100).ToString() + "],";
                    else
                        tempstr = tempstr + "[" + k.ToString() + "," + (Convert.ToDouble(txtbxValuedBm.Text) * 100).ToString() + "],";
                }
            }
            tempstr = tempstr.Remove(tempstr.LastIndexOf(','));
            saveConfigurationList.Add("/reader/radio/portReadPowerList", tempstr);

            tempstr = "";
            for (int k = 1; k <= 4; k++)
            {
                if (perPortWriteText[k - 1].Visibility == Visibility.Visible)
                {
                    if (!string.IsNullOrWhiteSpace(perPortReadText[k - 1].Text))
                        tempstr = tempstr + "[" + k.ToString() + "," + (Convert.ToDouble(perPortWriteText[k - 1].Text) * 100).ToString() + "],";
                    else
                        tempstr = tempstr + "[" + k.ToString() + "," + (Convert.ToDouble(txtbxWriteValuedBm.Text) * 100).ToString() + "],";
                }
            }
            tempstr = tempstr.Remove(tempstr.LastIndexOf(','));
            saveConfigurationList.Add("/reader/radio/portWritePowerList", tempstr);

            if (grpbxPerformanceTuning.IsEnabled)
            {
                saveConfigurationList.Add("/application/performanceTuning/Enable", "true");
                if ((bool)chkCustomizeGen2Settings.IsChecked)
                {
                    saveConfigurationList.Add("/application/performanceTuning/configureGen2SettingsType",
                        "Manual");
                    foreach (KeyValuePair<string, string> item in OptimalReaderSettings)
                    {
                        //Skip adding gen2 blf, tari and tag encoding to save configuration list
                        if ((item.Key != "/reader/gen2/BLF") && (item.Key != "/reader/gen2/tari") && (item.Key != "/reader/gen2/tagEncoding"))
                            saveConfigurationList.Add(item.Key, item.Value);
                    }
                    if (model.Equals("Astra"))
                    {
                        // If connected model is astra, add default values for saving.
                        saveConfigurationList["/reader/gen2/BLF"] = "LINK250KHZ";
                        saveConfigurationList["/reader/gen2/tari"] = "TARI_6_25US";
                        saveConfigurationList["/reader/gen2/tagEncoding"] = "FM0";
                        saveConfigurationList["/reader/gen2/q"] = "DynamicQ";

                        saveConfigurationList["/reader/gen2/initQ"] = "false";
                    }
                    if (saveConfigurationList["/reader/gen2/q"] == "DynamicQ")
                    {
                        // when ["/reader/gen2/q"] == "DynamicQ" then no need to save "/application/performanceTuning/staticQValue"
                        // in saveconfigurationList  
                        if (saveConfigurationList.ContainsKey("/application/performanceTuning/staticQValue"))
                        {
                            saveConfigurationList.Remove("/application/performanceTuning/staticQValue");
                        }
                    }
                    if (!(saveConfigurationList.ContainsKey("/application/performanceTuning/staticQValue")))
                    {
                        // Add [/application/performanceTuning/staticQValue] to saveconfigurationList
                        // only when ["/reader/gen2/q"] is StaticQ
                        if (saveConfigurationList.ContainsKey("/reader/gen2/q").Equals("StaticQ"))
                            saveConfigurationList.Add("/application/performanceTuning/staticQValue", "");
                    }

                    //Adding information for InitQ
                    if (saveConfigurationList.ContainsKey("/reader/gen2/initQ"))
                    {
                        if (saveConfigurationList.ContainsKey("/application/performanceTuning/initQValue"))
                        {
                            saveConfigurationList.Remove("/application/performanceTuning/initQValue");
                        }
                        else
                        {
                            // Add [/application/performanceTuning/staticQValue] to saveconfigurationList
                            // only when ["/reader/gen2/q"] is StaticQ
                            if (saveConfigurationList.ContainsKey("/reader/gen2/initQ").Equals("true"))
                                saveConfigurationList.Add("/application/performanceTuning/initQValue", "");
                        }
                    }

                    // Automatically adjust as population changes
                    saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges",
                        "false");
                    // Optimize for estimated number of tags in field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField",
                        "false");
                    // Tags in the field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                        txtTagsNum.Text);
                    // Read Distance vs. Read Rate
                    saveConfigurationList.Add("/application/performanceTuning/readDistancevsReadRate",
                        sldrRdDistVsrdRate.Value.ToString());

                    // Select best choice for population size
                    saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "false");
                    // Customize tag response rate
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate", "false");
                    // Tag response rate
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate/TagResponseRate",
                        sldrTagrspLessVsTagrspMore.Value.ToString());
                }
                else
                {
                    saveConfigurationList.Add("/application/performanceTuning/configureGen2SettingsType", "Auto");
                    foreach (KeyValuePair<string, string> item in OptimalReaderSettings)
                    {
                        //Skip adding gen2 blf, tari and tag encoding to save configuration list
                        if((item.Key != "/reader/gen2/BLF") && (item.Key != "/reader/gen2/tari") && (item.Key != "/reader/gen2/tagEncoding"))
                            saveConfigurationList.Add(item.Key, item.Value);
                    }
                    if (model.Equals("Astra"))
                    {
                        // If connected model is astra, add default values for saving.
                        /*saveConfigurationList["/reader/gen2/BLF"] = "LINK250KHZ";
                        saveConfigurationList["/reader/gen2/tari"] = "TARI_6_25US";
                        saveConfigurationList["/reader/gen2/tagEncoding"] = "FM0";*/
                        saveConfigurationList["/reader/gen2/q"] = "DynamicQ";

                        saveConfigurationList["/reader/gen2/initQ"] = "false";
                    }
                    if (!(saveConfigurationList.ContainsKey("/application/performanceTuning/staticQValue")))
                    {
                        // Add [/application/performanceTuning/staticQValue] to saveconfigurationList
                        // only when ["/reader/gen2/q"] is StaticQ
                        if (saveConfigurationList.ContainsKey("/reader/gen2/q").Equals("StaticQ"))
                            saveConfigurationList.Add("/application/performanceTuning/staticQValue", "");
                    }
                    // Automatically adjust as population changes
                    if (rdbtnAutoAdjstAsPoplChngs.IsEnabled)
                    {
                        if ((bool)rdbtnAutoAdjstAsPoplChngs.IsChecked)
                        {
                            saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges",
                                "true");
                        }
                        else
                        {
                            saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges",
                                "false");
                        }
                    }
                    else
                    {
                        // If the connected reader doesn't support this parameter
                        saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges",
                            "false");
                    }

                    // Optimize for estimated number of tags in field
                    if (rdbtnOptmzExtmdNoTagsInField.IsEnabled)
                    {
                        if ((bool)rdbtnOptmzExtmdNoTagsInField.IsChecked)
                        {
                            // Optimize for estimated number of tags in field
                            saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField",
                                "true");
                            // Tags in the field
                            saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                                txtTagsNum.Text);
                        }
                        else
                        {
                            // Optimize for estimated number of tags in field
                            saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField", "false");
                            // Tags in the field
                            saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                                txtTagsNum.Text);
                        }
                    }
                    else
                    {
                        // If the connected reader doesn't support this parameter
                        // Optimize for estimated number of tags in field
                        saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField", "false");
                        // Tags in the field
                        saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                            txtTagsNum.Text);
                    }

                    // Read Distance vs. Read Rate
                    saveConfigurationList.Add("/application/performanceTuning/readDistancevsReadRate",
                        sldrRdDistVsrdRate.Value.ToString());

                    // Select best choice for population size
                    if (rdBtnSlBstChforPoplSize.IsEnabled)
                    {
                        if ((bool)rdBtnSlBstChforPoplSize.IsChecked)
                        {
                            saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "true");
                        }
                        else
                        {
                            saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "false");
                        }
                    }
                    else
                    {
                        // If the connected reader doesn't support this parameter
                        saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "false");
                    }

                    // Customize tag response rate
                    if ((bool)rdBtnTagsRespondOption.IsChecked)
                    {
                        saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate", "true");
                        saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate/TagResponseRate",
                            sldrTagrspLessVsTagrspMore.Value.ToString());
                    }
                    else
                    {
                        saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate", "false");
                        saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate/TagResponseRate",
                            sldrTagrspLessVsTagrspMore.Value.ToString());
                    }
                }
            }
            else
            {
                saveConfigurationList.Add("/application/performanceTuning/Enable", "false");
                // Save the present state of performance tuning in URA
                saveConfigurationList.Add("/application/performanceTuning/configureGen2SettingsType", "Auto");
                foreach (KeyValuePair<string, string> item in OptimalReaderSettings)
                {
                    saveConfigurationList.Add(item.Key, item.Value);
                }
                if (model.Equals("Astra"))
                {
                    // If connected model is astra, add default values for saving.
                    /*saveConfigurationList["/reader/gen2/BLF"] = "LINK250KHZ";
                    saveConfigurationList["/reader/gen2/tari"] = "TARI_6_25US";
                    saveConfigurationList["/reader/gen2/tagEncoding"] = "FM0";*/
                    saveConfigurationList["/reader/gen2/q"] = "DynamicQ";

                    saveConfigurationList["/reader/gen2/initQ"] = "false";
                }
                if (!(saveConfigurationList.ContainsKey("/application/performanceTuning/staticQValue")))
                {
                    // Add [/application/performanceTuning/staticQValue] to saveconfigurationList
                    // only when ["/reader/gen2/q"] is StaticQ
                    if (saveConfigurationList.ContainsKey("/reader/gen2/q").Equals("StaticQ"))
                        saveConfigurationList.Add("/application/performanceTuning/staticQValue", "");
                }
                // Automatically adjust as population changes
                if ((bool)rdbtnAutoAdjstAsPoplChngs.IsChecked)
                {
                    saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges", "true");
                }
                else
                {
                    saveConfigurationList.Add("/application/performanceTuning/automaticallyAdjustAsPopulationChanges", "false");
                }

                // Optimize for estimated number of tags in field
                if ((bool)rdbtnOptmzExtmdNoTagsInField.IsChecked)
                {
                    // Optimize for estimated number of tags in field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField", "true");
                    // Tags in the field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                        txtTagsNum.Text);
                }
                else
                {
                    // Optimize for estimated number of tags in field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField", "false");
                    // Tags in the field
                    saveConfigurationList.Add("/application/performanceTuning/optimizeForEstimatedNumberOfTagsInField/tagsInTheField",
                        txtTagsNum.Text);
                }

                // Read Distance vs. Read Rate
                saveConfigurationList.Add("/application/performanceTuning/readDistancevsReadRate",
                    sldrRdDistVsrdRate.Value.ToString());

                // Select best choice for population size
                if ((bool)rdBtnSlBstChforPoplSize.IsChecked)
                {
                    saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "true");
                }
                else
                {
                    saveConfigurationList.Add("/application/performanceTuning/selectBestChoiceForPopulationSize", "false");
                }

                // Customize tag response rate
                if ((bool)rdBtnTagsRespondOption.IsChecked)
                {
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate", "true");
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate/TagResponseRate",
                        sldrTagrspLessVsTagrspMore.Value.ToString());
                }
                else
                {
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate", "false");
                    saveConfigurationList.Add("/application/performanceTuning/customizeTagResponseRate/TagResponseRate",
                        sldrTagrspLessVsTagrspMore.Value.ToString());
                }
            }
            // Save display options
            // Font size
            saveConfigurationList.Add("/application/displayOption/fontSize", txtfontSize.Text);

            // chkEnableTagAging
            if ((bool)chkEnableTagAging.IsChecked)
            {
                saveConfigurationList.Add("/application/displayOption/enableTagAging", "true");
            }
            else
            {
                saveConfigurationList.Add("/application/displayOption/enableTagAging", "false");
            }

            // Refresh [TagResults] for every refresh rate interval set
            saveConfigurationList.Add("/application/displayOption/refreshRate", txtRefreshRate.Text);

            // Select columns to be displayed on Tag Results
            foreach (ColumnSelectionForTagResult item in cbxcolumnSelection.Items)
            {
                if (item.IsColumnChecked)
                {
                    saveConfigurationList.Add("/application/displayOption/tagResultColumnSelection/enable"
                        + item.SelectedColumn, "true");
                }
                else
                {
                    saveConfigurationList.Add("/application/displayOption/tagResultColumnSelection/enable"
                        + item.SelectedColumn, "false");
                }
            }

            // Time stamp format
            // if the selected item is Select, keep it as null in config file
            if (cbxTimestampFormat.Text.Equals("Select"))
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/timeStampFormat", "");
            }
            else
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/timeStampFormat",
                    cbxTimestampFormat.Text);
            }

            // BigNum Selection
            // if the selected item is Select, keep it as null in config file
            if (cbxBigNum.Text.Equals("Select"))
            {
                saveConfigurationList.Add("/application/displayOption/tagResult/bigNumSelection", "");
            }
            else
            {
                saveConfigurationList.Add("/application/displayOption/tagResult/bigNumSelection",
                    cbxBigNum.Text);
            }

            // Displays EPC in selected format
            // if the selected item is Select, keep it as null in config file
            if (cbxDisplayEPCAs.Text.Equals("Select"))
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/displayEPCAs", "");
            }
            else
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/displayEPCAs",
                    cbxDisplayEPCAs.Text);
            }

            // BigNum Selection
            // if the selected item is Select, keep it as null in config file
            if (cbxDisplayEmbRdDataAs.Text.Equals("Select"))
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/displayEmbeddedReadDataAs", "");
            }
            else
            {
                saveConfigurationList.Add("/application/displayOption/tagResultColumn/displayEmbeddedReadDataAs",
                    cbxDisplayEmbRdDataAs.Text);
            }

            //Save HTTP post URL & update interval
            if (chkDataExtensions.IsEnabled)
            {
                if ((bool)rdBtnHttpPost.IsChecked)
                {
                    saveConfigurationList.Add("/application/displayOption/saveHttpURL", txtbxWebUrl.Text);
                    saveConfigurationList.Add("/application/displayOption/saveHttpUpdateInterval", txtbxHttpPostIntrvl.Text);
                }
                else
                {
                    saveConfigurationList.Add("/application/displayOption/saveHttpURL", "");
                    saveConfigurationList.Add("/application/displayOption/saveHttpUpdateInterval", "");
                }
            }

            return saveConfigurationList;
        }

        /// <summary>
        /// Event handler to handle when the user press the Save button in Load/Save Profile.
        /// Used to save the configuration parameters in the .urac file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveLoadSaveConfigFileDialog = new SaveFileDialog();
                // Files to be displayed when save file dialog window is opened
                saveLoadSaveConfigFileDialog.Filter = "URA Configuration Files (.urac)|*.urac";
                // Title to be appeared on the save file dialog
                saveLoadSaveConfigFileDialog.Title = "Select a configuration file to save reader "
                    + "and UI configuration parameters";

                // Filename field will be populated with [readername].urac. The reader name should be:
                // for Network readers the current hostname, such as: “m6-21071f.urac”
                // for Serial readers: [module type-COM#], such as: “M6e-COM4.urac”
                string strDestinationFile = lblReaderUri.Content.ToString().Replace(':', '_') + "_" + lblRFIDIPComContent.Content.ToString() + @".urac";
                saveLoadSaveConfigFileDialog.FileName = strDestinationFile;
                // Open the default directory to save the configuration file in the application
                // installed directory on the host machine
                saveLoadSaveConfigFileDialog.InitialDirectory = Directory.GetParent(
                    System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
                saveLoadSaveConfigFileDialog.RestoreDirectory = true;
                // Show the Dialog.
                if (saveLoadSaveConfigFileDialog.ShowDialog() == true)
                {
                    Mouse.SetCursor(Cursors.AppStarting);
                    loadSaveConfig.SaveConfigurations(saveLoadSaveConfigFileDialog.FileName,
                        GetParametersToSave());
                    Mouse.SetCursor(Cursors.Arrow);
                    // Set modify permission for configuration file. Since Windows OS doesn't 
                    // allow modifying any file present in ProgramFiles folder
                    Utilities.SetEditablePermissionOnConfigFile(saveLoadSaveConfigFileDialog.FileName);
                    MessageBox.Show("Saved reader and UI configuration parameters successfully",
                        "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Profile: " + ex.Message,
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        /// <summary>
        /// Validate load config filter data, strike out the 0x from the hex string.
        /// and checks whether the string is valid hex string
        /// </summary>
        /// <param name="hex">filter data</param>
        /// <returns>returns true, if valid hex string is found</returns>
        private bool ValidateFilterDataFromConfig(string hex)
        {
            string hexstring = string.Empty;
            int prelen = 0;
            // If string is prefixed with 0x
            if (hex.StartsWith("0x") || hex.StartsWith("0X"))
            {
                prelen = 2;
                // Strikeout the 0x and extract the remaining string
                hexstring = hex.Substring(prelen);
            }
            else
            {
                prelen = 0;
                hexstring = hex;
            }
            // Check whether the string is valid hex number
            //if (UInt32.TryParse(hexstring, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out decValue))
            if (System.Text.RegularExpressions.Regex.IsMatch(hexstring, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion Load/Save Profile

        /// <summary>
        /// Validate font size. Throw exception if the font size exceeds max limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtfontSize_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                if ((Convert.ToInt32(txtfontSize.Text) < 1) || (Convert.ToInt32(txtfontSize.Text) > 20))
                {
                    MessageBox.Show("Please input the font size between 1 and 20",
                        "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtfontSize.Text = "14";
                }
                else
                {
                    txtfontSize.Foreground = Brushes.Black;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Please input the font size between 1 and 20",
                        "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                txtfontSize.Text = "14";
                Onlog(ex);
            }
        }

        /// <summary>
        /// TMR log header - Add Start Time, reader URI and original file name to tmrlog file
        /// </summary>
        private void tmrLogHeader(string readerUri)
        {
            //log.Info("*****************************************************************");
            //log.Info("Start Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
            if (objReader is SerialReader)
            {
                // print reader uri in this format - Ex:tmr:///com31
                if (rdbtnCustomTransportConnection.IsChecked == true)
                    log.Info("Reader Uri: tcp://" + readerUri + ". " + "Reader Start Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
                else
                    log.Info("Reader Uri: tmr:///" + readerUri + ". " + "Reader Start Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
            }
            else
            {
                // print reader uri in this format - Ex:tmr://172.16.16.111
                log.Info("Reader Uri: tmr://" + readerUri + ". " + "Reader Start Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
            }
            //log.Info("Original Filename: " + "tmrlog_" + tmrLogStartTime + ".txt");
            //log.Info("*****************************************************************");
            //log.Flush();
        }

        private void tmrLogStopReader()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(lblRFIDIPComContent.Content.ToString()) && objReader != null)
                {
                    if (objReader is SerialReader)
                    {
                        if (rdbtnCustomTransportConnection.IsChecked == true)
                            log.Info("Reader Uri: tcp://" + lblRFIDIPComContent.Content.ToString() + ". " + "Reader Stop  Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
                        else
                            log.Info("Reader Uri: tmr:///" + lblRFIDIPComContent.Content.ToString() + ". " + "Reader Stop  Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
                    }
                    else
                    {
                        log.Info("Reader Uri: tmr://" + lblRFIDIPComContent.Content.ToString() + ". " + "Reader Stop  Time: " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss:fff"));
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        /// <summary>
        /// Checks whether Data extensions is Expanded or not 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expdrDataExtentions_Expanded(object sender, RoutedEventArgs e)
        {
            // Return the offset vector for the TextBlock object.
            Vector vector = VisualTreeHelper.GetOffset(((UIElement)sender));

            // Convert the vector to a point value.
            Point currentPoint = new Point(vector.X, vector.Y);
            settingsScrollviewer.ScrollToVerticalOffset(vector.Y);
        }

        /// <summary>
        /// Checks the checkbox EnableDataExtensions status
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkDataExtensions_Checked(object sender, RoutedEventArgs e)
        {
            if (httpenabled)
            {
                rdBtnStreamToTcpPort.IsEnabled = true;
                rdBtnHttpPost.IsEnabled = true;
                rdBtnHttpPost.IsChecked = true;
                httpenabled = false;
            }
            else
            {
                rdBtnStreamToTcpPort.IsEnabled = true;
                rdBtnHttpPost.IsEnabled = true;
                rdBtnStreamToTcpPort.IsChecked = true;
            }
        }

        /// <summary>
        /// Enables streaming to TCP when StreamtoTcpPort Radio button is checked 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnStreamToTcpPort_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdBtnStreamToTcpPort.IsChecked)
            {
                IPAddress ip = IPAddress.Any;
                int port = int.Parse(txtTcpPort.Text);
                waitforclient = true;
                serverThread = new Thread(() => connectToTCPClient(ip, port));
                serverThread.Start();
            }
            else
            {
                waitforclient = false;
                broadcastOFF();
                suspendChangedEvent.Set();
                listener.Stop();
                serverThread.Abort();
                clientConnected = false;
                suspendChangedEvent.Reset();
            }
        }

        /// <summary>
        /// Method to Creates Tcp connection and waits for clients to connect 
        /// </summary>
        /// <param name="ip">server ip</param>
        /// <param name="port">server port</param>
        public void connectToTCPClient(IPAddress ip, int port)
        {
            try
            {
                ASCIIEncoding asen = new ASCIIEncoding();
                listener = new TcpListener(ip, port);
                // Start Listening at the specified port
                listener.Start();
                while (waitforclient)
                {
                    tagStreamSock.Add(listener.AcceptSocket());
                    tagStreamSock[tagStreamSockCount].Blocking = false;
                    tagStreamSock[tagStreamSockCount].Send(asen.GetBytes("Automatic message: Connection Accepted!\n"));
                    if (isAsyncReadGoingOn)
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            broadcastON();
                        }));
                    }
                    clientConnected = true;
                    tagStreamSockCount++;
                }
                suspendChangedEvent.WaitOne();
                foreach (Socket tempSocket in tagStreamSock)
                    tempSocket.Close();
                listener.Stop();
            }
            catch { };
        }

        /// Sending tags to TCP server Through Async read
        /// </summary>
        /// <param name="e"></param>
        void PrintTagReads(TagReadDataEventArgs e, Socket tempSocket)
        {
            string data = StreamDataFormat(e.TagReadData);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate()
            {
                byte[] ba = Encoding.ASCII.GetBytes(data);
                var SentBytesLen = 0;
                while (SentBytesLen < ba.Length)
                {
                    try
                    {
                        SentBytesLen += tempSocket.Send(ba.Skip(SentBytesLen).ToArray());
                    }
                    catch (SocketException ex)
                    {
                        SentBytesLen = ba.Length;
                        DisplayMessageOnStatusBar(ex.Message, Brushes.Red);
                    }
                }
            }));
        }
        /// <summary>
        /// Get the time stamp format for streaming
        /// </summary>
        /// <returns></returns>
        private string GetTimeFormat()
        {
            string text = ((ComboBoxItem)cbxTimestampFormat.SelectedItem).Content.ToString();
            string StringFormat = string.Empty;
            switch (text)
            {
                case "DD/MM/YYYY HH:MM:Sec.MillSec":
                    StringFormat = "dd/MM/yyyy hh:mm:ss:fff tt";
                    break;
                case "MM/DD/YYYY HH:MM:Sec.MillSec":
                    StringFormat = "MM/dd/yyyy hh:mm:ss.fff tt";
                    break;
                case "YYYY/DD/MM HH:MM:Sec.MillSec":
                    StringFormat = "yyyy/dd/MM hh:mm:ss.fff tt";
                    break;
                case "HH:MM:Sec.MillSec":
                    StringFormat = "hh:mm:ss.fff tt";
                    break;
                default:
                    StringFormat = "hh:mm:ss.fff tt";
                    break;
            }
            return StringFormat;
        }
        public string StreamDataFormat(TagReadData e)
        {
            string data = string.Empty;
            data += e.EpcString;
            if (chkEmbeddedReadData.IsChecked == true)
            {
                data += "\t" + ByteFormat.ToHex(e.Data).Remove(0, 2);
            }
            data += "\t" + e.Time.ToString(GetTimeFormat()) + " \t" + e.Rssi.ToString() + "\t" + e.ReadCount.ToString();
            ColumnSelectionForTagResult rda;
            for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
            {
                rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);
                if (rda.IsColumnChecked)
                {
                    string newData = "";
                    switch (rda.SelectedColumn)
                    {
                        case "Antenna": newData = e.Antenna.ToString(); break;
                        case "Phase": newData = e.Phase.ToString(); break;
                        case "Frequency": newData = e.Frequency.ToString(); break;
                        case "Protocol": newData = e.Tag.Protocol.ToString(); break;
                        case "GPIO": newData = e.GPIO.ToString(); break;
                    }
                    if (!String.IsNullOrEmpty(newData))
                    {
                        data += "\t" + newData;
                    }
                }
            }
            data += "\r\n";
            return data;
        }
        /// Sending tags to TCP server Through Sync read
        /// </summary>
        /// <param name="data"></param>
        public void sendTagsToTcp(TagReadData[] data, Socket tempSocket)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
           {
               for (int i = 0; i < data.Length; i++)
               {
                   string data1 = StreamDataFormat(data[i]);
                   byte[] ba = Encoding.ASCII.GetBytes(data1);
                   tempSocket.Send(ba);
               }
           }));
        }

        /// <summary>
        /// txtTcpPort textbox only accepts numericals 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTcpPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// Validating txtTcpPort, If value greater than 65535 show message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTcpPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtTcpPort.Text != "")
                {
                    if (Convert.ToInt32(txtTcpPort.Text) > 65535)
                    {
                        MessageBox.Show("Please input TCP port value less then 65535",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtTcpPort.Text = "9055";
                        return;
                    }
                    if (Convert.ToInt32(txtRFOnTimeout.Text) < 0)
                    {
                        txtTcpPort.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txtTcpPort.Foreground = Brushes.Black;
                    }
                }
            }
            catch { }

        }

        /// <summary>
        /// Validating textbox txtTcpPort on keyboard lost focus. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTcpPort_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtTcpPort.Text == "")
            {
                MessageBox.Show("Tcp port can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtTcpPort.Text = "9055";
            }
        }

        /// <summary>
        ///  Http Post streaming option is enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnHttpPost_Checked(object sender, RoutedEventArgs e)
        {
            isHttpPostServiceEnabled = true;
        }

        /// <summary>
        /// Posts data to web server based on Update Interval value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void sendtags_Tick(Object sender, EventArgs args)
        {
            sendTagsToWeb(tagdb);
        }

        /// <summary>
        /// Posts data to web server based on Update Interval value.
        /// </summary>
        public void sendTagsToWeb(TagDatabase tagdb)
        {
            DateTime now = DateTime.Now;
            string tagData = string.Empty;
            Dispatcher.Invoke(new ThreadStart(delegate()
            {
                lock (tagdb)
                {
                    tagData = tagData + "reader_Name=" + HttpPostServiceReaderName + "&mac_address=" + GetMACAddress() + "&line_ending=\n" + "&field_delim=,"
                             + "&field_names=" + GetFieldNames() + "&field_values=";
                    for (int i = 0; i < tagdb.UniqueTagCount; i++)
                    {
                        tagData = tagData + GetFieldValues(tagdb.TagList[i]);
                    }
                }
            }));
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HttpPostServiceUrl);
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                byte[] byteArray = Encoding.UTF8.GetBytes(tagData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                request.Proxy = null;
                //By making Expect100Continue as false client will not expect 100-continue response from server.
                //By doing so we can send large amounts of data over the network.
                System.Net.ServicePointManager.Expect100Continue = false;
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Flush();
                    dataStream.Close();
                }
                var response = request.GetResponse() as HttpWebResponse;
                response.Close();
                request.Abort();
                request = null;
                DateTime end = DateTime.Now;
                if (Convert.ToDouble(end.Subtract(now).TotalMilliseconds) < HttpPostInterval)
                    HttpPostDispatchTimer.Interval = HttpPostInterval - Convert.ToDouble(end.Subtract(now).TotalMilliseconds);
                else
                    HttpPostDispatchTimer.Interval = 1;
                HttpPostDispatchTimer.Enabled = true;
            }
            catch (Exception ex)
            {
                HttpPostDispatchTimer.Enabled = false;
                HttpPostDispatchTimer.Close();
                MessageBox.Show("HttpPost:" + ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                broadcastOFF();
            }
        }
        /// <summary>
        /// Method to get http post field names.
        /// </summary>
        public string GetFieldNames()
        {
            string fieldNames = string.Empty;
            fieldNames += "EPC";
            chkEmbeddedReadData.Dispatcher.Invoke((Action)(() =>
            {
                if (chkEmbeddedReadData.IsChecked == true)
                {
                    fieldNames += "," + "Data";
                }
            }));
            fieldNames += "," + "TimeStamp" + "," + "Peak RSSI" + "," + "Read Count";
            ColumnSelectionForTagResult rda;
            for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
            {
                rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);
                if (rda.IsColumnChecked)
                {
                    string newData = "";
                    switch (rda.SelectedColumn)
                    {
                        case "Antenna": newData = "Antenna"; break;
                        case "Phase": newData = "Phase"; break;
                        case "Frequency": newData = "Frequency"; break;
                        case "Protocol": newData = "Protocol"; break;
                        case "GPIO": newData = "GPIO"; break;
                    }
                    if (!String.IsNullOrEmpty(newData))
                    {
                        fieldNames += "," + newData;
                    }
                }
            }
            return fieldNames;
        }

        /// <summary>
        /// Method to get http post field values
        /// </summary>
        /// <param name="e"></param>
        public string GetFieldValues(TagReadRecord e)
        {
            string data = string.Empty;
            data += e.EPC;
            chkEmbeddedReadData.Dispatcher.Invoke((Action)(() =>
            {
                if (chkEmbeddedReadData.IsChecked == true)
                {
                    data += "," + e.Data.Replace(" ", string.Empty);
                }
            }));
            data += "," + e.TimeStamp.ToString() + "," + e.RSSI.ToString() + "," + e.ReadCount.ToString();
            ColumnSelectionForTagResult rda;
            for (int rowCount = 0; rowCount <= cbxcolumnSelection.Items.Count - 1; rowCount++)
            {
                rda = (ColumnSelectionForTagResult)cbxcolumnSelection.Items.GetItemAt(rowCount);
                if (rda.IsColumnChecked)
                {
                    string newData = "";
                    switch (rda.SelectedColumn)
                    {
                        case "Antenna": newData = e.Antenna.ToString(); break;
                        case "Phase": newData = e.Phase.ToString(); break;
                        case "Frequency": newData = e.Frequency.ToString(); break;
                        case "Protocol": newData = e.Protocol.ToString(); break;
                    }
                    if (!String.IsNullOrEmpty(newData))
                    {
                        data += "," + newData;
                    }
                }
            }
            data += "\r\n";
            return data;
        }

        /// <summary>
        /// Validating HIGGS3 TID filter data length and start address 
        /// </summary>
        private bool validateTidFilterData()
        {
            bool isTidDataLength = true;
            if (chkApplyFilter1.IsChecked == true)
            {
                if (null != MultiFilters1.cbxFilterMemBank.SelectedItem)
                {
                    string selectedItemtext = ((ComboBoxItem)MultiFilters1.cbxFilterMemBank.SelectedItem).Content.ToString();
                    if (selectedItemtext.ToString() == "TID")
                    {
                        string txtModelID = string.Empty;
                        string txtVendorID = string.Empty;
                        string tidData = string.Empty;
                        tidData = Utilities.RemoveHexstringPrefix(MultiFilters1.txtFilterData.Text);
                        tidData = tidData.Replace(" ", string.Empty);
                        string higgsTidLengthWarning = "filter data length entered is greater than 12 bytes" +
                            " which falls into device configuration area of TID memory which is not fixed and filter may not be matched. Do you want to proceed?";
                        if (MultiFilters1.txtFilterStartAddr.Text != "0")
                        {
                            MessageBoxResult result = MessageBox.Show("Apply Filter: Start address is not zero so we cannot determine tag type." +
                            " In case of Higgs3 and Higgs4 if " + higgsTidLengthWarning,
                                   "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Error);
                            if (result.Equals(MessageBoxResult.No))
                            {
                                MultiFilters1.txtFilterStartAddr.Focus();
                                isTidDataLength = false;
                            }
                        }
                        else
                        {
                            if (tidData.Length > 24)
                            {
                                txtVendorID = tidData.Substring(2, 3);
                                txtModelID = tidData.Substring(5, 3);
                                string tagtype = string.Empty;
                                switch (txtVendorID)
                                {
                                    case Utilities.Alien:
                                        switch (txtModelID)
                                        {
                                            case Utilities.AlienHiggs3: tagtype = "Higgs3"; break;
                                            case Utilities.AlienHiggs4: tagtype = "Higgs4"; break;
                                            default: break;
                                        }
                                        break;
                                }
                                if (tagtype != string.Empty)
                                {
                                    MessageBoxResult result = MessageBox.Show("Apply Filter: Tag  type identified is " + tagtype + " and" + higgsTidLengthWarning,
                                           "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Error);
                                    if (result.Equals(MessageBoxResult.No))
                                    {
                                        MultiFilters1.txtFilterData.Focus();
                                        isTidDataLength = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return isTidDataLength;
        }
        /// <summary>
        /// When Enable Data extensions button unchecked disable all the streaming options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkDataExtensions_UnChecked(object sender, RoutedEventArgs e)
        {
            rdBtnStreamToTcpPort.IsChecked = false;
            rdBtnHttpPost.IsChecked = false;
            rdBtnStreamToTcpPort.IsEnabled = false;
            rdBtnHttpPost.IsEnabled = false;
        }
        /// <summary>
        /// When Http Post streaming option is unchecked stops the web streaming.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnHttpPost_UnChecked(object sender, RoutedEventArgs e)
        {
            isHttpPostServiceEnabled = false;
            HttpPostDispatchTimer.Enabled = false;
            HttpPostDispatchTimer.Close();
        }
        /// <summary>
        /// Method to save http post setings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool SaveHttpPostServiceSettings()
        {
            if (txtbxWebUrl.Text == string.Empty)
            {
                MessageBox.Show("HttpPost:URL can't be empty", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            HttpPostServiceReaderName = txtbxReaderName.Text;
            HttpPostServiceUrl = txtbxWebUrl.Text;
            Uri tempValue;
            if (!Uri.TryCreate(HttpPostServiceUrl, UriKind.Absolute, out tempValue) || null == tempValue)
            {
                MessageBox.Show("HttpPost: Please input valid URL", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                tempValue = null;
                //Invalid URL
                return false;
            }
            HttpPostInterval = Convert.ToDouble(txtbxHttpPostIntrvl.Text) * 1000;
            HttpPostDispatchTimer.Interval = HttpPostInterval;
            HttpPostDispatchTimer.AutoReset = false;
            MacAddress = GetMACAddress();
            return true;
        }

        private string GetMACAddress()
        {
            return string.Empty;
        }

        /// <summary>
        /// Update Interval(secs) text box only accepts numerical 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxHttpPostIntrvl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }
        /// <summary>
        /// Validating text box Update Interval(secs) on keyboard lost focus. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxHttpPostIntrvl_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtbxHttpPostIntrvl.Text == "")
            {
                MessageBox.Show("Http post time interval can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtbxHttpPostIntrvl.Text = "1";
            }
        }

        /// <summary>
        /// Enables the broadcast status. 
        /// </summary
        private void broadcastON()
        {
            imgTcpStreamStatus.Source = new BitmapImage(new Uri(@"..\Icons\broadcast-icon-on.png",
                    UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Disables the broadcast status. 
        /// </summary
        private void broadcastOFF()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate()
            {
                imgTcpStreamStatus.Source = new BitmapImage(new Uri(@"..\Icons\broadcast-icon-off.png",
                                            UriKind.RelativeOrAbsolute));
            }));
        }
        /// <summary>
        /// Http post Validating txtbxReaderName text box status on keyboard lost focus. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxReaderName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtbxReaderName.Text == string.Empty)
            {
                MessageBox.Show("HttpPost:URL can't be empty", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtbxReaderName.Text = model;
            }
        }

        private void txtbxHttpPostIntrvl_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtbxHttpPostIntrvl.Text != "")
                {
                    if (Convert.ToInt32(txtbxHttpPostIntrvl.Text) > 2073600)
                    {
                        MessageBox.Show("The input interval value is exceeding the limit. please enter a value between 1 to 2073600(24 days)",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtbxHttpPostIntrvl.Text = "1";
                        return;
                    }
                    if (Convert.ToInt32(txtbxHttpPostIntrvl.Text) <= 0)
                    {
                        MessageBox.Show("Zero is not a valid interval. Please enter a value between 1 and 2073600(24 days)",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtbxHttpPostIntrvl.Text = "1";
                        return;
                    }
                }
            }
            catch { }

        }

        private void RegulatoryUIControl()
        {
            if (btncwOn.Content.ToString().Equals("Start"))
            {
                grpbxtimed.IsEnabled = false;
                grpbxContinous.IsEnabled = false;
                txtbxCONTimeout.IsEnabled = false;
                txtbxCOFFTimeout.IsEnabled = false;
                tbtimed.IsEnabled = false;
                stkpnlHopTable.IsEnabled = false;
                expdrReadOptions.IsEnabled = false;
                expdrPerformanceTuning.IsEnabled = false;
                btnConnect.IsEnabled = false;
            }
            else
            {
                grpbxtimed.IsEnabled = grpbxContinous.IsEnabled = txtbxCONTimeout.IsEnabled = txtbxCOFFTimeout.IsEnabled = txtbxTimedONTimeout.IsEnabled = btncwOn.IsEnabled = tbtimed.IsEnabled = expdrReadOptions.IsEnabled = stkpnlHopTable.IsEnabled = expdrPerformanceTuning.IsEnabled = btnConnect.IsEnabled = true;
            }
        }

        private void btnTimedOn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool option = btnTimedOn.IsEnabled;
            if (objReader != null)
            {
                grpbxtimed.IsEnabled = grpbxContinous.IsEnabled = txtbxCONTimeout.IsEnabled = txtbxCOFFTimeout.IsEnabled = txtbxTimedONTimeout.IsEnabled = btncwOn.IsEnabled = stkpnlHopTable.IsEnabled = expdrReadOptions.IsEnabled = expdrPerformanceTuning.IsEnabled = btnConnect.IsEnabled = option;
            }
        }

        /// <summary>
        /// To Set hop table frequencies
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyHopTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hoptable = txtbxHopTable.Text;
                int[] hopTableFreqList = hoptable.Split(',').Select(s => int.Parse(s)).ToArray();
                objReader.ParamSet("/reader/region/hopTable", hopTableFreqList);
                MessageBox.Show("HopTable updated successfully", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enable or Disable the transmit continuous wave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTimedStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objReader != null)// && objReader is SerialReader
                {
                    if (!string.IsNullOrWhiteSpace(txtbxHopTable.Text) && !string.IsNullOrWhiteSpace((txtbxHopTable.Text.Split(','))[0]))
                    {
                        if (regioncombo.SelectedItem.ToString() == "Select")
                        {
                            MessageBox.Show("Please select region.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        List<int> ant = GetSelectedAntennaList();
                        if (ant.Count == 0)
                        {
                            MessageBox.Show("Please select at least one antenna.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                            if(!expdrReadOptions.IsExpanded)
                            {
                                expdrReadOptions.IsExpanded = true;
                                expdrReadOptions.Focus();
                            }
                            return;
                        }
                        else
                        {
                            objReader.ParamSet("/reader/tagop/antenna", ant[0]);
                            if (objReader is SerialReader)
                            {
                                SerialReader sRdr = (SerialReader)objReader;
                                sRdr.PrepForTagop();
                                sRdr.CmdTestSetFrequency(Convert.ToUInt32((txtbxHopTable.Text.Split(','))[0]));
                            }
                        }
                        int cwTimeout = Convert.ToUInt16(txtbxTimedONTimeout.Text);
                        //there is no off time for TIMED
                        Reader.RegulatoryMode rmode = Reader.RegulatoryMode.TIMED;
                        Reader.RegulatoryModulation RegModul = Reader.RegulatoryModulation.CW;
                        if (rdbtnTCW.IsChecked == true)
                        {
                            RegModul = Reader.RegulatoryModulation.CW;
                        }
                        else
                        {
                            RegModul = Reader.RegulatoryModulation.PRBS;
                        }

                        btnTimedOn.IsEnabled = false;
                        if (objReader is SerialReader)
                        {
                            SerialReader sRdr = (SerialReader)objReader;
                            //sRdr.PrepForTagop();

                            sRdr.cmdTestSendRegulatoryTest(rmode, RegModul, cwTimeout, 0, true);
                        }
                        else if (objReader is LlrpReader)
                        {
                            objReader.ParamSet("/reader/regulatory/mode", rmode);
                            objReader.ParamSet("/reader/regulatory/modulation", RegModul);
                            objReader.ParamSet("/reader/regulatory/onTime", cwTimeout);
                            objReader.ParamSet("/reader/regulatory/offTime", 0);
                            objReader.ParamSet("/reader/regulatory/enable", true);
                        }

                        cwTimer.Interval = new TimeSpan(0, 0, 0, 0, cwTimeout);
                        cwTimer.Start();
                        if (cwTimeout > TIMEFORCONDITION)
                        {
                            prbsTimer.Interval = TimeSpan.FromMilliseconds(1000);
                            prbsTimer.Start();
                        }
                        btnRead.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Hop Table cannnot be left blank", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                        tbHopTable.IsSelected = true;
                    }
                }
                else
                {
                    MessageBox.Show("Reader is not Serial/LLRP Reader", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("A message was received by the reader to set the frequency outside the supported range"))
                {
                    MessageBox.Show(ex.Message + "\n" + "Please select a appropriate frequecy in the hop table (first value).", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    tbHopTable.IsSelected = true;
                }
                else
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnTimedOn.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Time out for Timed Regulatory testing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cwTimer_Tick(object sender, EventArgs e)
        {
            prbsTimer.Stop();
            btnTimedOn.IsEnabled = true;
            btnRead.IsEnabled = true;
            (sender as DispatcherTimer).Stop();
        }

        /// <summary>
        /// Condition to check textbox to have only numbers and comma delimiter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxHopTable_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.Equals(","))
            {
                e.Handled = !Utilities.AreAllValidNumericChars(e.Text);
            }
            base.OnPreviewTextInput(e);
        }

        /// <summary>
        /// Timer for updating the Temperature on UI when CONTINOUS is ON in Regulatory testing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void prbsTimer_Tick(object sender, EventArgs e)
        {
            // Update the temperature parameter while CONTINOUS mode is ON
            try
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    lblReaderTemperature.Content = objReader.ParamGet("/reader/radio/temperature").ToString() + "°C";
                }
                ));
            }
            catch (Exception)
            {
                // Do Nothing
            }
        }

        /// <summary>
        /// Regulatory Testing CONTINUOUS Testing ON and OFF 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnContinousOnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btncwOn.Content.ToString() == "Start")
                {
                    if (objReader != null)//&& objReader is SerialReader)
                    {
                        if (regioncombo.SelectedItem.ToString() == "Select")
                        {
                            MessageBox.Show("Please select region.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        List<int> ant = GetSelectedAntennaList();
                        if (ant.Count == 0)
                        {
                            MessageBox.Show("Please select at least one antenna.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                            if (!expdrReadOptions.IsExpanded)
                            {
                                expdrReadOptions.IsExpanded = true;
                                expdrReadOptions.Focus();
                            }
                            return;
                        }
                        else
                        {
                            objReader.ParamSet("/reader/tagop/antenna", ant[0]);
                            if (objReader is SerialReader)
                            {
                                SerialReader serRdr = (SerialReader)objReader;
                                serRdr.PrepForTagop();
                                serRdr.CmdTestSetFrequency(Convert.ToUInt32((txtbxHopTable.Text.Split(','))[0]));
                            }
                        }
                        //disable Read button 
                        btnRead.IsEnabled = false;
                        //get ON and OFF timing
                        int cwTimeON = Convert.ToUInt16(txtbxCONTimeout.Text);
                        int cwTimeOff = Convert.ToUInt16(txtbxCOFFTimeout.Text);
                        //set regulatory mode and modulation and update the parameters based on the choice
                        Reader.RegulatoryMode rmode = Reader.RegulatoryMode.CONTINUOUS;
                        Reader.RegulatoryModulation RegModul = Reader.RegulatoryModulation.CW;
                        if (rdbtnCW.IsChecked == true)
                        {
                            RegModul = Reader.RegulatoryModulation.CW;
                        }
                        else
                        {
                            RegModul = Reader.RegulatoryModulation.PRBS;
                        }

                        //timeout updating
                        int PrbsTimeout = tempTransportTimeOut + cwTimeON + cwTimeOff;
                        objReader.ParamSet("/reader/transportTimeout", PrbsTimeout);
                        if (objReader is SerialReader)
                        {
                            SerialReader serRdr = (SerialReader)objReader;
                            //serRdr.PrepForTagop();
                            //serRdr.CmdTestSetFrequency(Convert.ToUInt32((txtbxHopTable.Text.Split(','))[0]));
                            serRdr.cmdTestSendRegulatoryTest(rmode, RegModul, cwTimeON, cwTimeOff, true);
                        }
                        else if (objReader is LlrpReader)
                        {
                            objReader.ParamSet("/reader/regulatory/mode", rmode);
                            objReader.ParamSet("/reader/regulatory/modulation", RegModul);
                            objReader.ParamSet("/reader/regulatory/onTime", cwTimeON);
                            objReader.ParamSet("/reader/regulatory/offTime", cwTimeOff);
                            objReader.ParamSet("/reader/regulatory/enable", true);
                        }
                        //if (cwTimeON >= TIMEFORCONDITION)
                        {
                            prbsTimer.Interval = TimeSpan.FromMilliseconds(TIMEFORCONDITION);
                            prbsTimer.Start();
                        }
                        RegulatoryUIControl();
                        btncwOn.Content = "Stop";
                    }
                    else
                    {
                        MessageBox.Show("Reader is not Serial/LLRP Reader", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    prbsTimer.Stop();
                    if (objReader is SerialReader)
                    {
                        SerialReader serRdr = (SerialReader)objReader;
                        serRdr.CmdTestSendCw(false);
                    }
                    else if (objReader is LlrpReader)
                    {
                        objReader.ParamSet("/reader/regulatory/enable", false);
                    }
                    btnRead.IsEnabled = true;
                    RegulatoryUIControl();
                    btncwOn.Content = "Start";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// PRBS timeout event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxTimeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (((TextBox)sender).Text != "")
                {
                    if (Convert.ToInt32(((TextBox)sender).Text) > MAXVALUE)
                    {
                        MessageBox.Show("Please input timeout less then " + MAXVALUE.ToString(),
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ((TextBox)sender).Text = DEFUALTTIME.ToString();
                        return;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxTimeout_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (((TextBox)sender).Text == "")
            {
                MessageBox.Show("Time (ms) can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                ((TextBox)sender).Text = DEFUALTTIME.ToString();
            }
        }

        //Implements Validation for Stop Trigger count input.
        private void txtbxStopTrigger_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void rdbtnglobal_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox[] perPortReadText = { txtReadPowerAnt1, txtReadPowerAnt2, txtReadPowerAnt3, txtReadPowerAnt4 };
                TextBox[] perPortWriteText = { txtWritePowerAnt1, txtWritePowerAnt2, txtWritePowerAnt3, txtWritePowerAnt4 };
                List<int[]> prpList = null;

                if (objReader != null)
                {
                    if (rdbtnglobal.IsChecked == false)
                    {
                        IList<int> existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");
                        int[][] perPortReadPower = (int[][])objReader.ParamGet("/reader/radio/portReadPowerList");
                        int[][] perPortWritePower = (int[][])objReader.ParamGet("/reader/radio/portWritePowerList");
                        prpList = new List<int[]>();

                        for (int i = 0; i < existingAntennas.Count; i++)
                        {
                            try
                            {
                                perPortReadText[i].Text = sldrReadPwr.Value.ToString();
                                if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortReadText[i].Text) > 30)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                        perPortReadText[i].Text = "30";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                    }

                                }
                                else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortReadText[i].Text) > 29)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 29) });
                                        perPortReadText[i].Text = "29";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                    }
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                }
                                //prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                            }catch(Exception ex)
                            {
                                // Came here means you might have received "Index was outside the bounds of the array."
                                // Max perPortReadText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                                // it results in trying to set perPortReadText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                                // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                                Onlog(ex);
                            }
                        }
                        objReader.ParamSet("/reader/radio/portReadPowerList", prpList.ToArray());

                        prpList = new List<int[]>();
                        for (int i = 0; i < existingAntennas.Count; i++)
                        {
                            try
                            {
                                perPortWriteText[i].Text = sldrWritePwr.Value.ToString();
                                if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortWriteText[i].Text) > 30)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                        perPortWriteText[i].Text = "30";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                    }
                                }
                                else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortWriteText[i].Text) > 29)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 29) });
                                        perPortWriteText[i].Text = "29";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                    }
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                }
                                //prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                            }catch(Exception ex)
                            {
                                // Came here means you might have received "Index was outside the bounds of the array."
                                // Max perPortWriteText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                                // it results in trying to set perPortWriteText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                                // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                                Onlog(ex);
                            }
                        }
                        objReader.ParamSet("/reader/radio/portWritePowerList", prpList.ToArray());

                        if ((model.Equals("M6e Micro USBPro")))
                        {
                            if (Double.Parse(perPortWriteText[0].Text) > 20 || Double.Parse(perPortWriteText[1].Text) > 20 || Double.Parse(perPortReadText[0].Text) > 20 || Double.Parse(perPortReadText[1].Text) > 20)
                            {
                                lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    warningText = "Please make sure to provide additional DC power source to the reader";
                                    DisplayMessageOnStatusBar(warningText, (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff9400")));
                                }));
                            }
                            else
                            {
                                lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    GUIturnoffWarning();
                                }));
                            }
                        }
                    }
                    else
                    {
                        IList<int> existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");
                        sldrReadPwr.Value = (Convert.ToDouble(objReader.ParamGet("/reader/radio/readPower").ToString()) / 100);
                        sldrWritePwr.Value = (Convert.ToDouble(objReader.ParamGet("/reader/radio/writePower").ToString()) / 100);

                        prpList = new List<int[]>();
                        for (int i = 0; i < existingAntennas.Count; i++)
                        {
                            try
                            {
                                perPortWriteText[i].Text = sldrWritePwr.Value.ToString();
                                if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortWriteText[i].Text) > 30)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                        perPortWriteText[i].Text = "30";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                    }
                                }
                                else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortWriteText[i].Text) > 29)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 29) });
                                        perPortWriteText[i].Text = "29";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                    }
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                }
                                //prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                            }catch(Exception ex)
                            {
                                // Came here means you might have received "Index was outside the bounds of the array."
                                // Max perPortWriteText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                                // it results in trying to set perPortWriteText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                                // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                                Onlog(ex);
                            }
                        }
                        objReader.ParamSet("/reader/radio/portWritePowerList", prpList.ToArray());

                        prpList = new List<int[]>();
                        for (int i = 0; i < existingAntennas.Count; i++)
                        {
                            try
                            {
                                perPortReadText[i].Text = sldrReadPwr.Value.ToString();
                                if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortReadText[i].Text) > 30)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                        perPortReadText[i].Text = "30";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                    }

                                }
                                else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                                {
                                    if (Convert.ToDouble(perPortReadText[i].Text) > 29)
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                        perPortReadText[i].Text = "29";
                                    }
                                    else
                                    {
                                        prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                    }
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                }
                                //prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                            }catch(Exception ex)
                            {
                                // Came here means you might have received "Index was outside the bounds of the array."
                                // Max perPortReadText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                                // it results in trying to set perPortReadText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                                // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                                Onlog(ex);
                            }
                        }
                        objReader.ParamSet("/reader/radio/portReadPowerList", prpList.ToArray());

                        if ((model.Equals("M6e Micro USBPro")))
                        {
                            if (sldrReadPwr.Value > 20 || sldrWritePwr.Value > 20)
                            {
                                lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    warningText = "Please make sure to provide additional DC power source to the reader";
                                    DisplayMessageOnStatusBar(warningText, (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff9400")));
                                }));
                            }
                            else
                            {
                                lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                                {
                                    GUIturnoffWarning();
                                }));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        private void txtPerPortPowerAnt_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            TextBox[] perPortReadText = { txtReadPowerAnt1, txtReadPowerAnt2, txtReadPowerAnt3, txtReadPowerAnt4 };
            TextBox[] perPortWriteText = { txtWritePowerAnt1, txtWritePowerAnt2, txtWritePowerAnt3, txtWritePowerAnt4 };
            IList<int> existingAntennas = (IList<int>)objReader.ParamGet("/reader/antenna/PortList");

            try
            {
                if (txt.Text != "")
                {
                    if ((Convert.ToDouble(txt.Text) < sldrReadPwr.Minimum) || (Convert.ToDouble(txt.Text) > sldrReadPwr.Maximum))
                    {
                        MessageBox.Show("Please enter power within " + sldrReadPwr.Minimum + " and " + sldrReadPwr.Maximum + " dBm", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                        txt.Text = sldrReadPwr.Maximum.ToString();
                    }
                }
                else
                {
                    MessageBox.Show("Power (dBm) can't be empty", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txt.Text = sldrReadPwr.Maximum.ToString();
                }

                if (txt.Name.ToLower().Contains("read"))
                {
                    List<int[]> prpList = new List<int[]>();
                    for (int i = 0; i < existingAntennas.Count; i++)
                    {
                        try
                        {
                            if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                            {
                                if (Convert.ToDouble(perPortReadText[i].Text) > 30)
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                    perPortReadText[i].Text = "30";
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                }

                            }
                            else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                            {
                                if (Convert.ToDouble(perPortReadText[i].Text) > 29)
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                    perPortReadText[i].Text = "29";
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                                }
                            }
                            else
                            {
                                prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                            }
                            //prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortReadText[i].Text)) });
                        }catch(Exception ex)
                        {
                            // Came here means you might have received "Index was outside the bounds of the array."
                            // Max perPortReadText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                            // it results in trying to set perPortReadText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                            // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                            Onlog(ex);
                        }
                    }
                    objReader.ParamSet("/reader/radio/portReadPowerList", prpList.ToArray());
                }
                else
                {
                    List<int[]> prpList = new List<int[]>();
                    for (int i = 0; i < existingAntennas.Count; i++)
                    {
                        try
                        {
                            if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.NA == regionToSet) && i == 0)
                            {
                                if (Convert.ToDouble(perPortWriteText[i].Text) > 30)
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 30) });
                                    perPortWriteText[i].Text = "30";
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                }
                            }
                            else if (!(objReader is SerialReader) && (model.Contains("Astra200")) && (Reader.Region.EU == regionToSet) && i == 0)
                            {
                                if (Convert.ToDouble(perPortWriteText[i].Text) > 29)
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * 29) });
                                    perPortWriteText[i].Text = "29";
                                }
                                else
                                {
                                    prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                                }
                            }
                            else
                            {
                                prpList.Add(new int[] { i + 1, Convert.ToInt32(100 * Convert.ToDouble(perPortWriteText[i].Text)) });
                            }
                        }catch(Exception ex)
                        {
                            // Came here means you might have received "Index was outside the bounds of the array."
                            // Max perPortWriteText boxes which can be set is 4(max physical ports). If logical antennas are enabled through mux,
                            // it results in trying to set perPortWriteText[y] box value where y is not 1 - 4 which is not available resulting in the above error.
                            // Hence log the error and continue with next operations as it is expected (we are not supporting logical antennas per port power).
                            Onlog(ex);
                        }

                    }
                    objReader.ParamSet("/reader/radio/portWritePowerList", prpList.ToArray());
                }

                if ((model.Equals("M6e Micro USBPro")))
                {
                    if (Double.Parse(perPortWriteText[0].Text) > 20 || Double.Parse(perPortWriteText[1].Text) > 20 || Double.Parse(perPortReadText[0].Text) > 20 || Double.Parse(perPortReadText[1].Text) > 20)
                    {
                        lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            warningText = "Please make sure to provide additional DC power source to the reader";
                            DisplayMessageOnStatusBar(warningText, (SolidColorBrush)(new BrushConverter().ConvertFrom("#ff9400")));
                        }));
                    }
                    else
                    {
                        lblWarning.Dispatcher.BeginInvoke(new ThreadStart(delegate()
                        {
                            GUIturnoffWarning();
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Onlog(ex);
                txt.Text = sldrReadPwr.Maximum.ToString();
            }
        }

        #region WizardFlow Method Handlers

        public void LoadURAnFromWizardFlow(bool IsRead)
        {
            try
            {
                if (ReaderConnectionDetail.ReaderType.ToLower().Contains("serial"))
                {
                    rdbtnLocalConnection.IsChecked = true;
                    rdbtnLocalConnection_Checked(null, null);
                    cmbReaderAddr.Text = ReaderConnectionDetail.ReaderName;
                }
                else if (ReaderConnectionDetail.ReaderType.ToLower().Contains("network"))
                {
                    rdbtnNetworkConnection.IsChecked = true;
                    rdbtnNetworkConnection_Checked(null, null);
                    cmbFixedReaderAddr.Text = ReaderConnectionDetail.ReaderName;
                }
                else if (ReaderConnectionDetail.ReaderType.ToLower().Contains("custom transport"))
                {
                    rdbtnCustomTransportConnection.IsChecked = true;
                    rdbtnLocalConnection_Checked(null, null);
                    txtCustomTransport.Text = ReaderConnectionDetail.ReaderName;
                }
                string[] tempantenna = ReaderConnectionDetail.Antenna.Split(',');
                string[] tempprotocol = ReaderConnectionDetail.Protocol.Split(new string[] { " ,", " " }, StringSplitOptions.RemoveEmptyEntries);
                CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox, Ant5CheckBox, Ant6CheckBox, Ant7CheckBox, Ant8CheckBox,
                                            Ant9CheckBox, Ant10CheckBox, Ant11CheckBox, Ant12CheckBox, Ant13CheckBox, Ant14CheckBox, Ant15CheckBox, Ant16CheckBox,
                                            Ant17CheckBox, Ant18CheckBox, Ant19CheckBox, Ant20CheckBox, Ant21CheckBox, Ant22CheckBox, Ant23CheckBox, Ant24CheckBox,
                                            Ant25CheckBox, Ant26CheckBox, Ant27CheckBox, Ant28CheckBox, Ant29CheckBox, Ant30CheckBox, Ant31CheckBox, Ant32CheckBox,
                                            Ant33CheckBox, Ant34CheckBox, Ant35CheckBox, Ant36CheckBox, Ant37CheckBox, Ant38CheckBox, Ant39CheckBox, Ant40CheckBox,
                                            Ant41CheckBox, Ant42CheckBox, Ant43CheckBox, Ant44CheckBox, Ant45CheckBox, Ant46CheckBox, Ant47CheckBox, Ant48CheckBox,
                                            Ant49CheckBox, Ant50CheckBox, Ant51CheckBox, Ant52CheckBox, Ant53CheckBox, Ant54CheckBox, Ant55CheckBox, Ant56CheckBox,
                                            Ant57CheckBox, Ant58CheckBox, Ant59CheckBox, Ant60CheckBox, Ant61CheckBox, Ant62CheckBox, Ant63CheckBox, Ant64CheckBox};
                CheckBox[] protocolBoxes = { gen2CheckBox, iso6bCheckBox, ipx64CheckBox, ipx256CheckBox, ataCheckBox, isoUcodeCheckbox, iso14443aCheckBox, iso14443bCheckBox, iso15693CheckBox, iso18092CheckBox, felicaCheckBox, iso180003mode3CheckBox, lf125KHZCheckBox, lf134KHZCheckBox };
                btnConnect_Click(null, null);
                //regioncombo.SelectedValue = ReaderConnectionDetail.Region;
                chkbxAntennaDetection.IsEnabled = Ant1CheckBox.IsEnabled = Ant2CheckBox.IsEnabled = Ant3CheckBox.IsEnabled = Ant4CheckBox.IsEnabled = true;
                chkbxAntennaDetection.IsChecked = Ant1CheckBox.IsChecked = Ant2CheckBox.IsChecked = Ant3CheckBox.IsChecked = Ant4CheckBox.IsChecked = false;
                regioncombo.SelectedItem = regioncombo.Items.GetItemAt(regioncombo.Items.IndexOf(Enum.Parse(typeof(Reader.Region), ReaderConnectionDetail.Region)));
                gen2CheckBox.IsChecked = iso6bCheckBox.IsChecked = ipx64CheckBox.IsChecked = ipx256CheckBox.IsChecked = ataCheckBox.IsChecked = isoUcodeCheckbox.IsChecked = iso14443aCheckBox.IsChecked = iso14443bCheckBox.IsChecked = iso15693CheckBox.IsChecked = iso18092CheckBox.IsChecked = felicaCheckBox.IsChecked = iso180003mode3CheckBox.IsChecked = lf125KHZCheckBox.IsChecked = lf134KHZCheckBox.IsChecked = false;

                // checks the checkport and then Antenna Detection checkbox is enabled.
                bool anchor = false;
                if (!(model.Contains("M3e")))
                {
                    try
                    {
                        anchor = (bool)objReader.ParamGet("/reader/antenna/checkPort");
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("Parameter not found"))
                        {
                            // Parameter not found error means antenna detection is not supported on the module.
                            anchor = false;
                            chkbxAntennaDetection.IsEnabled = false;
                            chkbxAntennaDetection.IsChecked = false;
                        }
                    }
                }
                chkbxAntennaDetection.IsChecked = anchor;
                if (anchor)
                {
                    ConfigureAntennaBoxes(objReader);
                }
                foreach (string tempant in tempantenna)
                {
                    antennaBoxes[Int32.Parse(tempant) - 1].IsChecked = true;
                }
                configureGPIOS();

                foreach (string tempprot in tempprotocol)
                {
                    switch (tempprot)
                    {
                        case "Gen2":
                            gen2CheckBox.Visibility = Visibility.Visible;
                            gen2CheckBox.IsChecked = true;
                            break;
                        case "ISO18000-6B":
                            iso6bCheckBox.Visibility = Visibility.Visible;
                            iso6bCheckBox.IsChecked = true;
                            break;
                        case "IPX64":
                            ipx64CheckBox.Visibility = Visibility.Visible;
                            ipx64CheckBox.IsChecked = true;
                            break;
                        case "IPX256":
                            ipx256CheckBox.Visibility = Visibility.Visible;
                            ipx256CheckBox.IsChecked = true;
                            break;
                        case "ATA":
                            ataCheckBox.Visibility = Visibility.Visible;
                            ataCheckBox.IsChecked = true;
                            break;
                        case "ISO18000-6B-UCODE":
                            isoUcodeCheckbox.Visibility = Visibility.Visible;
                            isoUcodeCheckbox.IsChecked = true;
                            break;
                        case "HF14443A":
                            iso14443aCheckBox.Visibility = Visibility.Visible;
                            iso14443aCheckBox.IsChecked = true;
                            break;
                        case "HF14443B":
                            iso14443bCheckBox.Visibility = Visibility.Visible;
                            iso14443bCheckBox.IsChecked = true;
                            break;
                        case "HF15693":
                            iso15693CheckBox.Visibility = Visibility.Visible;
                            iso15693CheckBox.IsChecked = true;
                            break;
                        case "HF18092":
                            iso18092CheckBox.Visibility = Visibility.Visible;
                            iso18092CheckBox.IsChecked = true;
                            break;
                        case "HFFELICA":
                            felicaCheckBox.Visibility = Visibility.Visible;
                            felicaCheckBox.IsChecked = true;
                            break;
                        case "HF180003M3":
                            iso180003mode3CheckBox.Visibility = Visibility.Visible;
                            iso180003mode3CheckBox.IsChecked = true;
                            break;
                        case "LF125KHZ":
                            lf125KHZCheckBox.Visibility = Visibility.Visible;
                            lf125KHZCheckBox.IsChecked = true;
                            break;
                        case "LF134KHZ":
                            lf134KHZCheckBox.Visibility = Visibility.Visible;
                            lf134KHZCheckBox.IsChecked = true;
                            break;
                    }
                }

                OnProtocolSelect();

                if (IsRead == true)
                {
                    btnRead_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                if (ex is FAULT_BL_INVALID_IMAGE_CRC_Exception || ex is FAULT_BL_INVALID_APP_END_ADDR_Exception)
                {
                    throw ex;
                }
                else
                {
                    Onlog(ex);
                }

            }
        }

        private void DeleteConfigFile()
        {
            if (!string.IsNullOrWhiteSpace(lblRFIDIPComContent.Content.ToString()))
            {
                string filename = lblRFIDIPComContent.Content.ToString().Replace(':', '_') + "_config" + @".txt";
                string fileFullPath = System.IO.Path.Combine(ConnectionWizardVM.ConfigFilesdirPath, filename);
                FileInfo fInfo = new FileInfo(fileFullPath);
                if (!(Directory.Exists(ConnectionWizardVM.ConfigFilesdirPath)))
                {
                    Directory.CreateDirectory(ConnectionWizardVM.ConfigFilesdirPath);
                }

                if (File.Exists(fileFullPath))
                {
                    fInfo.IsReadOnly = false;
                    File.Delete(fileFullPath);
                }
            }
        }

        private void SaveConfigurationForWizardFlow()
        {
            try
            {
                if (btnSaveConfig.IsEnabled == true)
                {
                    string fileName = null;
                    fileName = lblRFIDIPComContent.Content.ToString().Replace(':', '_') + "_config" + @".txt";
                    string fileFullPath = System.IO.Path.Combine(ConnectionWizardVM.ConfigFilesdirPath, fileName);
                    FileInfo fInfo = new FileInfo(fileFullPath);
                    if (!(Directory.Exists(ConnectionWizardVM.ConfigFilesdirPath)))
                    {
                        Directory.CreateDirectory(ConnectionWizardVM.ConfigFilesdirPath);
                    }

                    if (File.Exists(fileFullPath))
                    {
                        fInfo.IsReadOnly = false;
                        File.Delete(fileFullPath);
                    }

                    FileStream fs = new FileStream(fileFullPath, FileMode.OpenOrCreate, FileAccess.Write);
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        if (rdbtnLocalConnection.IsChecked == true)
                        {
                            sw.WriteLine("/application/connect/readerType=SerialReader");
                        }
                        else if (rdbtnNetworkConnection.IsChecked == true)
                        {
                            sw.WriteLine("/application/connect/readerType=NetworkReader");
                        }
                        else if (rdbtnCustomTransportConnection.IsChecked == true)
                        {
                            sw.WriteLine("/application/connect/readerType=CustomTransport");
                        }
                        else
                        {
                            sw.WriteLine("/application/connect/readerType=Unknown");
                        }

                        if (!string.IsNullOrWhiteSpace(regioncombo.SelectedItem.ToString()) && !regioncombo.SelectedItem.ToString().Contains("Select"))
                            sw.WriteLine("/reader/region/id=" + regioncombo.SelectedItem.ToString());
                        else
                        {
                            if (model.ToString().Equals(ReaderConnectionDetail.ReaderModel))
                                sw.WriteLine("/reader/region/id=" + ReaderConnectionDetail.Region);
                            else
                                sw.WriteLine("/reader/region/id=" + regioncombo.SelectedItem.ToString());
                        }
                        sw.WriteLine("/reader/version/model=" + model.ToString());

                        string tempMuxString = "/application/readwriteOption/portswitchgposenabled=";
                        //save Antenna Multiplexing status
                        if ((bool)(chbxOne.IsChecked) || (bool)(chbxTwo.IsChecked) || (bool)(chbxThree.IsChecked) || (bool)(chbxFour.IsChecked))
                        {
                            sw.WriteLine(tempMuxString + "True");
                        }
                        else
                        {
                            sw.WriteLine(tempMuxString + "False");
                        }

                        string tempantstring = "/application/readwriteOption/Antennas=";
                        foreach (int tempant in GetSelectedAntennaList())
                        {
                            tempantstring = tempantstring + tempant.ToString() + ",";
                        }

                        if (tempantstring.Contains(','))
                            sw.WriteLine(tempantstring.TrimEnd(','));
                        else
                        {
                            if (model.ToString().Equals(ReaderConnectionDetail.ReaderModel))
                                sw.WriteLine(tempantstring + ReaderConnectionDetail.Antenna);
                            else
                                sw.WriteLine(tempantstring + "1");
                        }

                        string tempprotocolstring = "/application/readwriteOption/Protocols=";
                        // Save protocol configurations
                        StringBuilder protocols = new StringBuilder();
                        foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackpanel6))
                        {
                            if ((bool)child.IsChecked)
                            {
                                protocols.Append(child.Content.ToString());
                                protocols.Append(" ,");
                            }
                        }
                        foreach (CheckBox child in LogicalTreeHelper.GetChildren(stackpanel7))
                        {
                            if ((bool)child.IsChecked)
                            {
                                protocols.Append(child.Content.ToString());
                                protocols.Append(" ,");
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(protocols.ToString()))
                            sw.WriteLine(tempprotocolstring + protocols.ToString().TrimEnd(','));
                        else
                        {
                            if (model.ToString().Equals(ReaderConnectionDetail.ReaderModel))
                                sw.WriteLine(tempprotocolstring + ReaderConnectionDetail.Protocol);
                            else
                                sw.WriteLine(tempprotocolstring + "Gen2");
                        }

                        sw.WriteLine("lastconnected=" + DateTime.Now.ToString());
                        sw.WriteLine("IsConnected=" + "false");
                    }
                    fInfo.IsReadOnly = true;
                }

            }
            catch (Exception ex)
            {
                Onlog(ex);
            }
        }

        #endregion

        #region Feature Supported
        /// <summary>
        /// Return's if the feature is supported for this version
        /// </summary>
        /// <param name="featureName"></param>
        /// <returns></returns>
        private bool IsFeatureSupported(string featureName)
        {
            try
            {
                Version readerVersion = null;
                Version checkVersion = null;

                switch (lblRFIDEngineContent.Content.ToString())
                {
                    case "M6e":
                    case "M6e PRC":
                    case "M6e JIC":
                        checkVersion = new Version("1.33.1.25"); //Converted to Decimal Version. Actual Version is 1.21.1.7.
                        break;
                    case "M6e Micro":
                    case "M6e Micro USB":
                    case "M6e Micro USBPro":
                        checkVersion = new Version("1.9.0.20");
                        break;
                    case "M6e Nano":
                        checkVersion = new Version("1.7.0.14");
                        break;
                    case "Sargas":
                        checkVersion = new Version("5.7.0.50");
                        break;
                    case "Izar":
                        checkVersion = new Version("5.7.0.50");
                        break;
                    default:
                        return false;
                }


                if (!string.IsNullOrWhiteSpace(lblFirmwareVersionContent.Content.ToString()))
                {
                    string tempreaderVersion = lblFirmwareVersionContent.Content.ToString();
                    string[] versionSplit = tempreaderVersion.Split('.');
                    for (int i = 0; i < versionSplit.Length; i++)
                    {
                        versionSplit[i] = int.Parse(versionSplit[i], System.Globalization.NumberStyles.HexNumber).ToString();
                    }
                    readerVersion = new Version(string.Join(".", versionSplit));

                    if (checkVersion != null && readerVersion != null)
                    {
                        if (readerVersion.CompareTo(checkVersion) >= 0)
                            return true;
                    }
                }
                return false;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        private void cbxAntennamux_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)cbxAntennamux.IsChecked)
            {
                if (model.Equals("M6e Nano") || M7eFamilyList.Contains(model))
                {
                    chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Visible;
                    rdBtnEqualSwitching.IsEnabled = true;
                }
                else if (model.Contains("M3e"))
                {
                    chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Visible;
                }
                else if (model.Equals("M6e"))
                {
                    chbxOne.Visibility = chbxThree.Visibility = chbxTwo.Visibility = chbxFour.Visibility = Visibility.Visible;
                }
                else if (model.Equals("M6e Micro") || model.Equals("M6e Micro USBPro"))
                {
                    chbxOne.Visibility = chbxTwo.Visibility = Visibility.Visible;
                }
                else if (model.Equals("Sargas") || model.Equals("Izar"))
                {
                    if (IsFeatureSupported("AntennaMux"))
                    {
                        if (model.Equals("Izar"))
                        {
                            chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Visible;
                            chbxThree.IsEnabled = chbxFour.IsEnabled = chbxTwo.IsEnabled = false;
                            chbxOne.IsEnabled = true;
                        }
                        else
                        {
                            chbxOne.Visibility = chbxTwo.Visibility = Visibility.Visible;
                            chbxOne.IsEnabled = chbxTwo.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Collapsed;
                }
                if (model.Equals("Sargas") || model.Equals("Izar"))
                {
                    //Need to add trigger GPO value.
                    triggerGPI = (new int[] { }).ToList();
                }
                else
                {
                    int[] triggerGPIArray = (int[])objReader.ParamGet("/reader/read/trigger/gpi");
                    triggerGPI = triggerGPIArray.ToList();
                }
                
            }
            else
            {
                chbxOne.IsChecked = chbxTwo.IsChecked = chbxThree.IsChecked = chbxThree.IsChecked = chbxFour.IsChecked = false;
                chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Collapsed;
                displayLogicalAntennas();
            }

        }

        private void AddGPIPort(int pin)
        {
            cbxGPIPort.ItemsSource = cbxDisplayGPIItem(pin);
            cbxGPIPort.SelectedIndex = 0;
        }

        private void configureGPIOS()
        {
            if (model.Equals("M6e Nano") || model.Equals("M6e") || model.Equals("M3e") || M7eFamilyList.Contains(model))
            {
                grpGPIOBehaviour.Visibility = Visibility.Visible;
                stkpnlSerialGPIO.IsEnabled = true;
                stkpnlSerialGPIO.Visibility = Visibility.Visible;
                stkpnlLLRPGPIO.IsEnabled = false;
                stkpnlLLRPGPIO.Visibility = Visibility.Collapsed;
                chbxGpo1.Visibility = chbxGpo2.Visibility = chbxGpo3.Visibility = chbxGpo4.Visibility = Visibility.Visible;
                AddGPIPort(4);

            }
            else if (model.Equals("M6e Micro") || model.Equals("M6e Micro USBPro"))
            {
                grpGPIOBehaviour.Visibility = Visibility.Visible;
                stkpnlSerialGPIO.IsEnabled = true;
                stkpnlSerialGPIO.Visibility = Visibility.Visible;
                stkpnlLLRPGPIO.IsEnabled = false;
                stkpnlLLRPGPIO.Visibility = Visibility.Collapsed;
                chbxGpo1.Visibility = chbxGpo2.Visibility = Visibility.Visible;
                chbxGpo3.Visibility = chbxGpo4.Visibility = Visibility.Collapsed;
                AddGPIPort(2);
            }
            else if (model.Equals("Sargas") || model.Equals("Izar"))
            {
                grpGPIOBehaviour.Visibility = Visibility.Visible;
                stkpnlSerialGPIO.IsEnabled = false;
                stkpnlSerialGPIO.Visibility = Visibility.Collapsed;
                stkpnlLLRPGPIO.IsEnabled = true;
                stkpnlLLRPGPIO.Visibility = Visibility.Visible;
                if (IsFeatureSupported("AntennaMUX"))
                {
                    // Adding the condition to stop from going to else part.
                    cbxAntennamux.Visibility = Visibility.Visible;
                }
                else
                {
                    cbxAntennamux.Visibility = Visibility.Collapsed;
                }
                // uncomment to enable GPIO configuration 
                if (model.Equals("Sargas"))
                {
                    chbxLLRPGPI1.IsEnabled = chbxLLRPGPI2.IsEnabled = true;
                    chbxLLRPGPI1.Visibility = chbxLLRPGPI2.Visibility = Visibility.Visible;
                    chbxLLRPGPO1.IsEnabled = chbxLLRPGPO2.IsEnabled = true;
                    chbxLLRPGPO1.Visibility = chbxLLRPGPO2.Visibility = Visibility.Visible;
                    chbxLLRPGPI3.Visibility = chbxLLRPGPI4.Visibility = Visibility.Collapsed;
                    chbxLLRPGPO3.Visibility = chbxLLRPGPO4.Visibility = Visibility.Collapsed;
                    AddGPIPort(2);
                }
                else if (model.Equals("Izar"))
                {
                    chbxLLRPGPI1.IsEnabled = chbxLLRPGPI2.IsEnabled = chbxLLRPGPI3.IsEnabled = chbxLLRPGPI4.IsEnabled = true;
                    chbxLLRPGPI1.Visibility = chbxLLRPGPI2.Visibility = chbxLLRPGPI3.Visibility = chbxLLRPGPI4.Visibility = Visibility.Visible;
                    chbxLLRPGPO1.IsEnabled = chbxLLRPGPO2.IsEnabled = chbxLLRPGPO3.IsEnabled = chbxLLRPGPO4.IsEnabled = true;
                    chbxLLRPGPO1.Visibility = chbxLLRPGPO2.Visibility = chbxLLRPGPO3.Visibility = chbxLLRPGPO4.Visibility = Visibility.Visible;
                    AddGPIPort(4);
                }
                //grpGPIOBehaviour.Visibility = Visibility.Visible;
                //chbxGpo1.Visibility = chbxGpo2.Visibility = chbxGpo3.Visibility = chbxGpo4.Visibility = Visibility.Visible;
            }
            else
            {
                cbxAntennamux.Visibility = Visibility.Collapsed;
                chbxOne.Visibility = chbxThree.Visibility = chbxFour.Visibility = chbxTwo.Visibility = Visibility.Collapsed;
                grpGPIOBehaviour.Visibility = Visibility.Collapsed;
                AddGPIPort(0);
            }
        }


        private void GetGPOS()
        {
            List<int> gpo = ((int[])(objReader.ParamGet("/reader/antenna/portSwitchGpos"))).ToList<int>();
            foreach (int item in gpo)
            {
                switch (item)
                {
                    case 1:
                        cbxAntennamux.IsChecked = true;
                        chbxOne.IsChecked = true;
                        break;
                    case 2:
                        cbxAntennamux.IsChecked = true;
                        chbxTwo.IsChecked = true;
                        break;
                    case 3:
                        cbxAntennamux.IsChecked = true;
                        chbxThree.IsChecked = true;
                        break;
                    case 4:
                        cbxAntennamux.IsChecked = true;
                        chbxFour.IsChecked = true;
                        break;
                    default:
                        cbxAntennamux.IsChecked = false;
                        break;
                }
            }
        }

        private List<int> checkedGpos()
        {
            antMux = new List<int>();
            if ((bool)chbxOne.IsChecked)
            {
                if (!antMux.Contains(1))
                {
                    antMux.Add(1);
                }
            }
            if ((bool)chbxTwo.IsChecked)
            {
                if (!antMux.Contains(2))
                {
                    antMux.Add(2);
                }
            }
            if ((bool)chbxThree.IsChecked)
            {
                if (!antMux.Contains(3))
                {
                    antMux.Add(3);
                }
            }
            if ((bool)chbxFour.IsChecked)
            {
                if (!antMux.Contains(4))
                {
                    antMux.Add(4);
                }
            }
            if (!(bool)chbxOne.IsChecked)
            {
                if (antMux.Contains(1))
                {
                    antMux.Remove(1);
                }
            }
            if (!(bool)chbxTwo.IsChecked)
            {
                if (antMux.Contains(2))
                {
                    antMux.Remove(2);
                }
            }
            if (!(bool)chbxThree.IsChecked)
            {
                if (antMux.Contains(3))
                {
                    antMux.Remove(3);
                }
            }
            if (!(bool)chbxFour.IsChecked)
            {
                if (antMux.Contains(4))
                {
                    antMux.Remove(4);
                }
            }
            return antMux;
        }

        private void displayLogicalAntennas()
        {
            List<int> tempGpos = null;
            try
            {
                tempGpos = checkedGpos();
                if (objReader != null)
                {
                    if (tempGpos.Count != 0)
                    {
                        objReader.ParamSet("/reader/antenna/portSwitchGpos", tempGpos.ToArray());
                    }
                    else
                    {
                        tempGpos.Clear();
                        objReader.ParamSet("/reader/antenna/portSwitchGpos", new int[] { });
                    }
                    ConfigureLogicalAntennaBoxes(objReader);
                }
                else
                {
                    #region Unbinding the CheckBox to the triggers
                    chbxOne.Checked -= new RoutedEventHandler(chbxOne_Checked);
                    chbxOne.Unchecked -= new RoutedEventHandler(chbxOne_Checked);
                    chbxTwo.Checked -= new RoutedEventHandler(chbxTwo_Checked);
                    chbxTwo.Unchecked -= new RoutedEventHandler(chbxTwo_Checked);
                    chbxThree.Checked -= new RoutedEventHandler(chbxThree_Checked);
                    chbxThree.Unchecked -= new RoutedEventHandler(chbxThree_Checked);
                    chbxFour.Checked -= new RoutedEventHandler(chbxFour_Checked);
                    chbxFour.Unchecked -= new RoutedEventHandler(chbxFour_Checked);
                    #endregion
                }
            }
            catch { }

        }
        private void configureInputDirection(int[] list)
        {
            RadioButton[] inputBoxes = { rdbOneInput, rdbTwoInput, rdbThreeInput, rdbFourInput };
            int inputNum = 1;
            foreach (RadioButton rb in inputBoxes)
            {
                if (list.Contains(inputNum))
                {
                    if (inputNum == 1)
                    {
                        rdbOneInput.Checked -= rdbOneInput_Checked;
                        rdbOneInput.IsChecked = true;
                        rdbOneInput.Checked += rdbOneInput_Checked;
                    }
                    if (inputNum == 2)
                    {
                        rdbTwoInput.Checked -= rdbOneInput_Checked;
                        rdbTwoInput.IsChecked = true;
                        rdbTwoInput.Checked += rdbOneInput_Checked;
                    }
                    if (inputNum == 3)
                    {
                        rdbThreeInput.Checked -= rdbOneInput_Checked;
                        rdbThreeInput.IsChecked = true;
                        rdbThreeInput.Checked += rdbOneInput_Checked;
                    }
                    if (inputNum == 4)
                    {
                        rdbFourInput.Checked -= rdbOneInput_Checked;
                        rdbFourInput.IsChecked = true;
                        rdbFourInput.Checked += rdbOneInput_Checked;
                    }

                }
                else
                {
                    rb.IsChecked = false;
                }
                inputNum++;
            }
            char returnValue;
            if ((bool)rdbOneInput.IsChecked)
            {
                returnValue = returnGPIOValue(1);
                if (returnValue == 'L')
                {
                    rdbOneLow.IsChecked = true;
                    rdbOneHigh.IsEnabled = false;
                }
                else
                {
                    rdbOneHigh.IsChecked = true;
                    rdbOneLow.IsEnabled = false;
                }
            }
            if ((bool)rdbFourInput.IsChecked)
            {
                returnValue = returnGPIOValue(4);
                if (returnValue == 'L')
                {
                    rdbFourLow.IsChecked = true;
                    rdbFourHigh.IsEnabled = false;
                }
                else
                {
                    rdbFourHigh.IsChecked = true;
                    rdbFourLow.IsEnabled = false;
                }
            }
            if ((bool)rdbTwoInput.IsChecked)
            {
                returnValue = returnGPIOValue(2);
                if (returnValue == 'L')
                {
                    rdbTwoLow.IsChecked = true;
                    rdbTwoHigh.IsEnabled = false;
                }
                else
                {
                    rdbTwoHigh.IsChecked = true;
                    rdbTwoLow.IsEnabled = false;
                }
            }
            if ((bool)rdbThreeInput.IsChecked)
            {
                returnValue = returnGPIOValue(3);
                if (returnValue == 'L')
                {
                    rdbThreeLow.IsChecked = true;
                    rdbThreeHigh.IsEnabled = false;
                }
                else
                {
                    rdbThreeHigh.IsChecked = true;
                    rdbThreeLow.IsEnabled = false;
                }
            }
        }
        private void configureOutputDirection(int[] list)
        {
            RadioButton[] outputBoxes = { rdbOneOutput, rdbTwoOutput, rdbThreeOutput, rdbFourOutput };
            int outputNum = 1;
            foreach (RadioButton rb in outputBoxes)
            {
                if (list.Contains(outputNum))
                {
                    if (outputNum == 1)
                    {
                        rdbOneOutput.Checked -= rdbOneOutput_Checked;
                        rdbOneOutput.IsChecked = true;
                        rdbOneOutput.Checked += rdbOneOutput_Checked;
                        rdbOneLow.IsEnabled = true;
                        rdbOneHigh.IsEnabled = true;
                        rdbOneHigh.IsChecked = false;
                        rdbOneLow.IsChecked = true;
                    }
                    if (outputNum == 2)
                    {
                        rdbTwoOutput.Checked -= rdbTwoOutput_Checked;
                        rdbTwoOutput.IsChecked = true;
                        rdbTwoOutput.Checked += rdbTwoOutput_Checked;
                        rdbTwoHigh.IsEnabled = true;
                        rdbTwoLow.IsEnabled = true;
                        rdbTwoLow.IsChecked = true;
                        rdbTwoHigh.IsChecked = false;
                    }
                    if (outputNum == 3)
                    {
                        rdbThreeOutput.Checked -= rdbThreeOutput_Checked;
                        rdbThreeOutput.IsChecked = true;
                        rdbThreeOutput.Checked += rdbThreeOutput_Checked;
                        rdbThreeHigh.IsEnabled = true;
                        rdbThreeLow.IsEnabled = true;
                        rdbThreeLow.IsChecked = true;
                        rdbThreeHigh.IsChecked = false;
                    }
                    if (outputNum == 4)
                    {
                        rdbFourOutput.Checked -= rdbFourOutput_Checked;
                        rdbFourOutput.IsChecked = true;
                        rdbFourOutput.Checked += rdbFourOutput_Checked;
                        rdbFourHigh.IsEnabled = true;
                        rdbFourLow.IsEnabled = true;
                        rdbFourLow.IsChecked = true;
                        rdbFourHigh.IsChecked = false;
                    }
                }
                else
                {
                    rb.IsChecked = false;
                }
                outputNum++;
            }
        }
        private void configureGPIODirection()
        {
            int[] inputList = (int[])(objReader.ParamGet("/reader/gpio/inputList"));
            int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
            List<int> portSwitchList = null;
            int[] portswitchArray = null;
            if (!(model.Equals("M3e")))
            {
                portswitchArray = (int[])(objReader.ParamGet("/reader/antenna/portSwitchGpos"));
                portSwitchList = portswitchArray.ToList();
            }
            RadioButton[] inputBoxes = { rdbOneInput, rdbTwoInput, rdbThreeInput, rdbFourInput };
            RadioButton[] outputBoxes = { rdbOneOutput, rdbTwoOutput, rdbThreeOutput, rdbFourOutput };
            int inputNum = 1;
            foreach (RadioButton rb in inputBoxes)
            {
                if (inputList.Contains(inputNum))
                {
                    rb.IsChecked = true;
                }
                else
                {
                    rb.IsChecked = false;
                }
                inputNum++;
            }
            int outputNum = 1;
            if (model.Equals("M3e"))
            {
                foreach (RadioButton rb in outputBoxes)
                {
                    if (outputList.Contains(outputNum))
                    {
                        rb.IsChecked = true;
                    }
                    else
                    {
                        rb.IsChecked = false;
                    }
                    outputNum++;
                }
            }
            else
            {
                foreach (RadioButton rb in outputBoxes)
                {
                    if (outputList.Contains(outputNum) && (!portSwitchList.Contains(outputNum)))
                    {
                        rb.IsChecked = true;
                    }
                    else
                    {
                        rb.IsChecked = false;
                    }
                    outputNum++;
                }
            }
            char returnValue;
            if ((bool)rdbOneInput.IsChecked)
            {
                returnValue = returnGPIOValue(1);
                if (returnValue == 'L')
                {
                    rdbOneLow.IsChecked = true;
                    rdbOneHigh.IsEnabled = false;
                }
                else
                {
                    rdbOneHigh.IsChecked = true;
                    rdbOneLow.IsEnabled = false;
                }
            }
            if ((bool)rdbFourInput.IsChecked)
            {
                returnValue = returnGPIOValue(4);
                if (returnValue == 'L')
                {
                    rdbFourLow.IsChecked = true;
                    rdbFourHigh.IsEnabled = false;
                }
                else
                {
                    rdbFourHigh.IsChecked = true;
                    rdbFourLow.IsEnabled = false;
                }
            }
            if ((bool)rdbTwoInput.IsChecked)
            {
                returnValue = returnGPIOValue(2);
                if (returnValue == 'L')
                {
                    rdbTwoLow.IsChecked = true;
                    rdbTwoHigh.IsEnabled = false;
                }
                else
                {
                    rdbTwoHigh.IsChecked = true;
                    rdbTwoLow.IsEnabled = false;
                }
            }
            if ((bool)rdbThreeInput.IsChecked)
            {
                returnValue = returnGPIOValue(3);
                if (returnValue == 'L')
                {
                    rdbThreeLow.IsChecked = true;
                    rdbThreeHigh.IsEnabled = false;
                }
                else
                {
                    rdbThreeHigh.IsChecked = true;
                    rdbThreeLow.IsEnabled = false;
                }
            }
        }
        private char returnGPIOValue(int gpio)
        {
            char value = ' ';
            string iValue;
            string flag = gpio.ToString();
            GpioPin[] gpilist = objReader.GpiGet();
            Array arr = (Array)gpilist;
            string[] valstrings = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                valstrings[i] = arr.GetValue(i).ToString();
                iValue = valstrings[i];
                if (iValue.StartsWith(flag))
                {
                    value = iValue[1];
                }
            }
            return value;
        }

        private void setOutputConfiguration()
        {
            outputList = new List<int>();
            if ((bool)rdbOneOutput.IsChecked)
            {
                if (!outputList.Contains(1))
                {
                    outputList.Add(1);
                }
            }
            if ((bool)rdbTwoOutput.IsChecked)
            {
                if (!outputList.Contains(2))
                {
                    outputList.Add(2);
                }
            }
            if ((bool)rdbThreeOutput.IsChecked)
            {
                if (!outputList.Contains(3))
                {
                    outputList.Add(3);
                }
            }
            if ((bool)rdbFourOutput.IsChecked)
            {
                if (!outputList.Contains(4))
                {
                    outputList.Add(4);
                }
            }
            if (!(bool)rdbOneOutput.IsChecked)
            {
                if (outputList.Contains(1))
                {
                    outputList.Remove(1);
                }
            }
            if (!(bool)rdbTwoOutput.IsChecked)
            {
                if (outputList.Contains(2))
                {
                    outputList.Remove(2);
                }
            }
            if (!(bool)rdbThreeOutput.IsChecked)
            {
                if (outputList.Contains(3))
                {
                    outputList.Remove(3);
                }
            }
            if (!(bool)rdbFourOutput.IsChecked)
            {
                if (outputList.Contains(4))
                {
                    outputList.Remove(4);
                }
            }
            objReader.ParamSet("/reader/gpio/outputList", outputList.ToArray());
        }
        private void setInputConfiguration()
        {
            inputList = new List<int>();
            if ((bool)rdbOneInput.IsChecked)
            {
                if (!inputList.Contains(1))
                {
                    inputList.Add(1);
                }
            }
            if ((bool)rdbTwoInput.IsChecked)
            {
                if (!inputList.Contains(2))
                {
                    inputList.Add(2);
                }
            }
            if ((bool)rdbThreeInput.IsChecked)
            {
                if (!inputList.Contains(3))
                {
                    inputList.Add(3);
                }
            }
            if ((bool)rdbFourInput.IsChecked)
            {
                if (!inputList.Contains(4))
                {
                    inputList.Add(4);
                }
            }
            if (!(bool)rdbOneInput.IsChecked)
            {
                if (inputList.Contains(1))
                {
                    inputList.Remove(1);
                }
            }
            if (!(bool)rdbTwoInput.IsChecked)
            {
                if (inputList.Contains(2))
                {
                    inputList.Remove(2);
                }
            }
            if (!(bool)rdbThreeInput.IsChecked)
            {
                if (inputList.Contains(3))
                {
                    inputList.Remove(3);
                }
            }
            if (!(bool)rdbFourInput.IsChecked)
            {
                if (inputList.Contains(4))
                {
                    inputList.Remove(4);
                }
            }
            objReader.ParamSet("/reader/gpio/inputList", inputList.ToArray());
        }
        private void validateUIcontrols()
        {
            if (((bool)chbxGpo1.IsChecked) || ((bool)chbxGpo2.IsChecked) || ((bool)chbxGpo3.IsChecked) || ((bool)chbxGpo4.IsChecked))
            {
                txbGPIDirection.Visibility = Visibility.Visible;
                txbGPIOValue.Visibility = Visibility.Visible;
            }
            else
            {
                txbGPIDirection.Visibility = Visibility.Collapsed;
                txbGPIOValue.Visibility = Visibility.Collapsed;
            }
        }

        private void configuringDuringConnect(bool flag)
        {
            chbxGpo1.IsChecked = chbxGpo2.IsChecked = chbxGpo3.IsChecked = chbxGpo4.IsChecked = flag;
            txbGPIDirection.Visibility = txbGPIOValue.Visibility = stkpnlOneDirection.Visibility = stkpnlOneValue.Visibility =
            stkpnlTwoDirection.Visibility = stkpnlTwoValue.Visibility = stkpnlThreeValue.Visibility = stkpnlThreeDirection.Visibility = stkpnlFourDirection.Visibility = stkpnlFourValue.Visibility = Visibility.Collapsed;
        }

        private void chbxOne_Checked(object sender, RoutedEventArgs e)
        {

            if (triggerGPI.Contains(1))
            {
                if ((bool)chbxOne.IsChecked)//"if" logic will be needed to modifiy if LLRP reader support triggers
                {
                    MessageBoxResult result = MessageBox.Show("Trigger GPI is configured on this pin. To disable triggering and enable multiplexing, Select YES. Otherwise Select NO", "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result.Equals(MessageBoxResult.Yes))
                    {
                        triggerGPI.Remove(1);
                        objReader.ParamSet("/reader/read/trigger/gpi", triggerGPI.ToArray());
                        bool flag = (!(bool)chbxOne.IsChecked);
                        chbxGpo1.IsEnabled = flag;
                        displayLogicalAntennas();
                        if (!flag)
                        {
                            chbxGpo1.IsChecked = false;
                            stkpnlOneDirection.Visibility = Visibility.Collapsed;
                            stkpnlOneValue.Visibility = Visibility.Collapsed;
                            validateUIcontrols();
                        }
                    }
                    else
                    {
                        chbxOne.IsChecked = false;
                        chbxOne.IsEnabled = false;
                        chbxGpo1.IsEnabled = false;
                    }
                }
            }
            else
            {
                bool flag = (!(bool)chbxOne.IsChecked);
                chbxGpo1.IsEnabled = flag;
                chbxLLRPGPO1.IsEnabled = flag;
                displayLogicalAntennas();
                if (!flag)
                {
                    chbxGpo1.IsChecked = false;
                    chbxLLRPGPO1.IsChecked = false;
                    stkpnlOneDirection.Visibility = Visibility.Collapsed;
                    stkpnlOneValue.Visibility = Visibility.Collapsed;
                    validateUIcontrols();
                }
            }
        }

        private void chbxTwo_Checked(object sender, RoutedEventArgs e)
        {
            if (triggerGPI.Contains(2))
            {
                if ((bool)chbxTwo.IsChecked)
                {
                    MessageBoxResult result = MessageBox.Show("Trigger GPI is configured on this pin. To disable triggering and enable multiplexing, Select YES. Otherwise Select NO", "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result.Equals(MessageBoxResult.Yes))
                    {
                        triggerGPI.Remove(2);
                        objReader.ParamSet("/reader/read/trigger/gpi", triggerGPI.ToArray());
                        bool flag = (!(bool)chbxTwo.IsChecked);
                        chbxGpo2.IsEnabled = flag;
                        displayLogicalAntennas();
                        if (!flag)
                        {
                            chbxGpo2.IsChecked = false;
                            stkpnlTwoDirection.Visibility = Visibility.Collapsed;
                            stkpnlTwoValue.Visibility = Visibility.Collapsed;
                            validateUIcontrols();
                        }
                    }
                    else
                    {
                        chbxTwo.IsChecked = false;
                        chbxTwo.IsEnabled = false;
                        chbxGpo2.IsEnabled = false;
                    }
                }
            }
            else
            {
                bool flag = (!(bool)chbxTwo.IsChecked);
                chbxGpo2.IsEnabled = flag;
                chbxLLRPGPO2.IsEnabled = flag;
                displayLogicalAntennas();
                if (!flag)
                {
                    chbxLLRPGPO2.IsChecked = false;
                    chbxGpo2.IsChecked = false;
                    stkpnlTwoDirection.Visibility = Visibility.Collapsed;
                    stkpnlTwoValue.Visibility = Visibility.Collapsed;
                    validateUIcontrols();
                }
            }
        }

        private void chbxThree_Checked(object sender, RoutedEventArgs e)
        {
            if (triggerGPI.Contains(3))
            {
                if ((bool)chbxThree.IsChecked)
                {
                    MessageBoxResult result = MessageBox.Show("Trigger GPI is configured on this pin. To disable triggering and enable multiplexing, Select YES. Otherwise Select NO", "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result.Equals(MessageBoxResult.Yes))
                    {
                        triggerGPI.Remove(3);
                        objReader.ParamSet("/reader/read/trigger/gpi", triggerGPI.ToArray());
                        bool flag = (!(bool)chbxThree.IsChecked);
                        chbxGpo3.IsEnabled = flag;
                        displayLogicalAntennas();
                        if (!flag)
                        {
                            chbxGpo3.IsChecked = false;
                            stkpnlThreeDirection.Visibility = Visibility.Collapsed;
                            stkpnlThreeValue.Visibility = Visibility.Collapsed;
                            validateUIcontrols();
                        }
                    }
                    else
                    {
                        chbxThree.IsChecked = false;
                        chbxThree.IsEnabled = false;
                        chbxGpo3.IsEnabled = false;
                    }
                }
            }
            else
            {
                bool flag = (!(bool)chbxThree.IsChecked);
                chbxGpo3.IsEnabled = flag;
                chbxLLRPGPO3.IsEnabled = flag;
                displayLogicalAntennas();
                if (!flag)
                {
                    chbxGpo3.IsChecked = false;
                    chbxLLRPGPO3.IsChecked = false;
                    stkpnlThreeDirection.Visibility = Visibility.Collapsed;
                    stkpnlThreeValue.Visibility = Visibility.Collapsed;
                    validateUIcontrols();
                }
            }
        }

        private void chbxFour_Checked(object sender, RoutedEventArgs e)
        {
            if (triggerGPI.Contains(4))
            {
                if ((bool)chbxFour.IsChecked)
                {
                    MessageBoxResult result = MessageBox.Show("Trigger GPI is configured on this pin. To disable triggering and enable multiplexing, Select YES. Otherwise Select NO", "Universal Reader Assistant Message", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result.Equals(MessageBoxResult.Yes))
                    {
                        triggerGPI.Remove(4);
                        objReader.ParamSet("/reader/read/trigger/gpi", triggerGPI.ToArray());
                        bool flag = (!(bool)chbxFour.IsChecked);
                        chbxGpo4.IsEnabled = flag;
                        displayLogicalAntennas();
                        if (!flag)
                        {
                            chbxGpo4.IsChecked = false;
                            stkpnlFourDirection.Visibility = Visibility.Collapsed;
                            stkpnlFourValue.Visibility = Visibility.Collapsed;
                            validateUIcontrols();
                        }
                    }
                    else
                    {
                        chbxFour.IsChecked = false;
                        chbxFour.IsEnabled = false;
                        chbxGpo4.IsEnabled = false;
                    }
                }
            }
            else
            {
                bool flag = (!(bool)chbxFour.IsChecked);
                chbxGpo4.IsEnabled = flag;
                chbxLLRPGPO4.IsEnabled = flag;
                displayLogicalAntennas();
                if (!flag)
                {
                    chbxGpo4.IsChecked = false;
                    chbxLLRPGPO4.IsChecked = false;
                    stkpnlFourDirection.Visibility = Visibility.Collapsed;
                    stkpnlFourValue.Visibility = Visibility.Collapsed;
                    validateUIcontrols();
                }
            }
        }

        private void chbxGpo1_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxGpo1.IsChecked)
            {
                stkpnlOneDirection.Visibility = Visibility.Visible;
                validateUIcontrols();
                stkpnlOneValue.Visibility = Visibility.Visible;
                rdbOneInput.IsChecked = true;
                setInputConfiguration();
                configureGPIODirection();
            }
            else
            {
                stkpnlOneDirection.Visibility = Visibility.Collapsed;
                validateUIcontrols();
                stkpnlOneValue.Visibility = Visibility.Collapsed;
            }
        }

        private void chbxGpo2_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxGpo2.IsChecked)
            {
                stkpnlTwoDirection.Visibility = Visibility.Visible;
                validateUIcontrols();
                stkpnlTwoValue.Visibility = Visibility.Visible;
                rdbTwoInput.IsChecked = true;
                setInputConfiguration();
                configureGPIODirection();
            }
            else
            {
                stkpnlTwoDirection.Visibility = Visibility.Collapsed;
                validateUIcontrols();
                stkpnlTwoValue.Visibility = Visibility.Collapsed;
            }
        }

        private void chbxGpo3_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxGpo3.IsChecked)
            {
                stkpnlThreeDirection.Visibility = Visibility.Visible;
                validateUIcontrols();
                stkpnlThreeValue.Visibility = Visibility.Visible;
                rdbThreeInput.IsChecked = true;
                setInputConfiguration();
                configureGPIODirection();
            }
            else
            {
                stkpnlThreeDirection.Visibility = Visibility.Collapsed;
                validateUIcontrols();
                stkpnlThreeValue.Visibility = Visibility.Collapsed;
            }
        }

        private void chbxGpo4_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxGpo4.IsChecked)
            {
                stkpnlFourDirection.Visibility = Visibility.Visible;
                validateUIcontrols();
                stkpnlFourValue.Visibility = Visibility.Visible;
                rdbFourInput.IsChecked = true;
                setInputConfiguration();
                configureGPIODirection();
            }
            else
            {
                stkpnlFourDirection.Visibility = Visibility.Collapsed;
                validateUIcontrols();
                stkpnlFourValue.Visibility = Visibility.Collapsed;
            }
        }

        private void rdbOneOutput_Checked(object sender, RoutedEventArgs e)
        {
            setOutputConfiguration();
            rdbOneLow.IsEnabled = true;
            rdbOneHigh.IsEnabled = true;
            rdbOneHigh.IsChecked = false;
            rdbOneLow.IsChecked = true;
        }

        private void rdbTwoOutput_Checked(object sender, RoutedEventArgs e)
        {
            setOutputConfiguration();
            rdbTwoHigh.IsEnabled = true;
            rdbTwoLow.IsEnabled = true;
            rdbTwoLow.IsChecked = true;
            rdbTwoHigh.IsChecked = false;
        }

        private void rdbThreeOutput_Checked(object sender, RoutedEventArgs e)
        {
            setOutputConfiguration();
            rdbThreeHigh.IsEnabled = true;
            rdbThreeLow.IsEnabled = true;
            rdbThreeLow.IsChecked = true;
            rdbThreeHigh.IsChecked = false;
        }

        private void rdbFourOutput_Checked(object sender, RoutedEventArgs e)
        {
            setOutputConfiguration();
            rdbFourHigh.IsEnabled = true;
            rdbFourLow.IsEnabled = true;
            rdbFourLow.IsChecked = true;
            rdbFourHigh.IsChecked = false;
        }

        private void rdbOneInput_Checked(object sender, RoutedEventArgs e)
        {
            setInputConfiguration();
            configureGPIODirection();
        }

        private void rdbTwoLow_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbTwoOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(2, false);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 2);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }

        }

        private void rdbTwoHigh_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbTwoOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(2, true);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 2);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }


        }

        private void rdbOneLow_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbOneOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(1, false);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 1);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }

        }

        private void rdbOneHigh_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbOneOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(1, true);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 1);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }
        }

        private void rdbThreeLow_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbThreeOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(3, false);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 3);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }
        }

        private void rdbThreeHigh_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbThreeOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(3, true);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 3);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }
        }

        private void rdbFourLow_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbFourOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(4, false);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 4);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }
        }

        private void rdbFourHigh_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)rdbFourOutput.IsChecked)
            {
                try
                {
                    GpioPin gp = new GpioPin(4, true);
                    if (!gps.Contains(gp))
                    {
                        gps.RemoveAll(p => p.Id == 4);
                        gps.Add(gp);
                    }
                    objReader.GpoSet(gps);
                }
                catch { }
            }
        }
        private void StaticQ_Unchecked(object sender, RoutedEventArgs e)
        {
            xDInitQValue1.IsEnabled = false;
            Gen2.InitQ iq = new Gen2.InitQ();

            iq.qEnable = false;
            iq.initialQ = 2;// Convert.ToInt32(OptimalReaderSettings["/application/performanceTuning/initQValue"]);
            objReader.ParamSet("/reader/gen2/initQ", iq);
            xDInitQValue1.IsEnabled = false;
            OptimalReaderSettings["/reader/gen2/initQ"] = "false";
            OptimalReaderSettings["/application/performanceTuning/initQValue"] = "2";
        }

        private void chkApplyFilter1_Unchecked(object sender, RoutedEventArgs e)
        {
            chkApplyFilter3.IsChecked = false;
            chkApplyFilter2.IsChecked = false;
        }

        private void chkApplyFilter2_Unchecked(object sender, RoutedEventArgs e)
        {
            chkApplyFilter3.IsChecked = false;
        }

        private void txtbxStopTrigger_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {

        }

        private void chkLBTEnable_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/region/lbt/enable", true);
                    txtEnableLBTThreshold.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkEnableDwellTime_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/region/dwellTime/enable", true);
                    txtDwelltime.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void chkLBTEnable_Unchecked(object sender, RoutedEventArgs e)
        {
            txtEnableLBTThreshold.IsEnabled = false;
            try
            {
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/region/lbt/enable", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void chkEnableDwellTime_Unchecked(object sender, RoutedEventArgs e)
        {
            txtDwelltime.IsEnabled = false;
            try
            {
                if (objReader is SerialReader)
                {
                    objReader.ParamSet("/reader/region/dwellTime/enable", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void txtEnableLBTThreshold_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtEnableLBTThreshold.Text == "")
            {
                // display popup box  
                MessageBox.Show("Please fill in the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtEnableLBTThreshold.Focus(); // set focus to lastNameTextBox  
                return;
            } // end if   
            // if input format invalid show message  
            if (!Regex.Match(txtEnableLBTThreshold.Text, "^[0-9a-zA-Z/ -]+$").Success)
            {
                // input was incorrect  
                MessageBox.Show("Invalid inputs", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtEnableLBTThreshold.Focus();
                return;
            } // end if 
            Int16 checc = Convert.ToSByte(txtEnableLBTThreshold.Text);
            if (checc < -129 || checc > 0)
            {
                // input was not in range
                MessageBox.Show("Range of input is between -128 ~ -1", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtEnableLBTThreshold.Focus();
                return;
            }
            if (txtEnableLBTThreshold.Text != "0" && chkLBTEnable.IsChecked == true)
            {
                try
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/region/lbtThreshold", checc);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    //get the default value on the module if set fails and show it in UI.
                    txtEnableLBTThreshold.Text = objReader.ParamGet("/reader/region/lbtThreshold").ToString();
                }
            }
        }

        private void txtDwelltime_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtDwelltime.Text == "")
            {
                // display popup box  
                MessageBox.Show("Please fill in the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtDwelltime.Focus(); // set focus to lastNameTextBox  
                return;
            } // end if   
            // if input format invalid show message  
            if (!Regex.Match(txtDwelltime.Text, "^[0-9a-zA-Z]+$").Success)
            {
                // input was incorrect  
                MessageBox.Show("Invalid inputs", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtDwelltime.Focus();
                return;
            } // end if 
            UInt16 checc = Convert.ToUInt16(txtDwelltime.Text);
            if (checc < 0 || checc > 65535)
            {
                // input was not in range
                MessageBox.Show("Range of input is between 0 ~ 65535", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtDwelltime.Focus();
                return;
            }
            if (txtDwelltime.Text != "0" && chkEnableDwellTime.IsChecked == true)
            {
                try
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/region/dwellTime", checc);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtMinFreq_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtMinFreq.Text == "")
            {
                // display popup box  
                MessageBox.Show("Please fill in the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtMinFreq.Focus(); // set focus to lastNameTextBox  
                return;
            } // end if   
            // if input format invalid show message  
            if (!Regex.Match(txtMinFreq.Text, "^[0-9a-zA-Z/ -]+$").Success)
            {
                // input was incorrect  
                MessageBox.Show("Invalid inputs", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtMinFreq.Focus();
                return;
            } // end if 
            Int32 checc = Convert.ToInt32(txtMinFreq.Text);
            //if (checc < 0 || checc > 65535)
            //{ range is unknown
            //    // input was not in range
            //    MessageBox.Show("Range of input is between 0 ~ 65535", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            //    //txtMinFreq.Focus();
            //    return;
            //}
            if (txtMinFreq.Text != "0")
            {
                try
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/region/minimumFrequency", checc);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtQuantizationStep_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtQuantizationStep.Text == "")
            {
                // display popup box  
                MessageBox.Show("Please fill in the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtQuantizationStep.Focus(); // set focus to lastNameTextBox  
                return;
            } // end if   
            // if input format invalid show message  
            if (!Regex.Match(txtQuantizationStep.Text, "^[0-9a-zA-Z/ -]+$").Success)
            {
                // input was incorrect  
                MessageBox.Show("Invalid inputs", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtQuantizationStep.Focus();
                return;
            } // end if 
            Int32 checc = Convert.ToInt32(txtQuantizationStep.Text);
            //if (checc < 0 || checc > 65535)
            //{
            //    // input was not in range
            //    MessageBox.Show("Range of input is between 0 ~ 65535", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            //    //txtQuantizationStep.Focus();
            //    return;
            //}
            if (txtQuantizationStep.Text != "0")
            {
                try
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/region/quantizationStep", checc);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtHopTime_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtHopTime.Text == "")
            {
                // display popup box  
                MessageBox.Show("Please fill in the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtHopTime.Focus(); // set focus to lastNameTextBox  
                return;
            } // end if   
            // if input format invalid show message  
            if (!Regex.Match(txtHopTime.Text, "^[0-9a-zA-Z/ -]+$").Success)
            {
                // input was incorrect  
                MessageBox.Show("Invalid inputs", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtHopTime.Focus();
                return;
            } // end if 
            Int32 checc = Convert.ToInt32(txtHopTime.Text);
            if (checc < 0 || checc > 65535)
            {
                // input was not in range
                MessageBox.Show("Range of input is between 0 ~ 65535", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                //txtHopTime.Focus();
                return;
            }
            if (txtHopTime.Text != "0")
            {
                try
                {
                    if (objReader is SerialReader)
                    {
                        objReader.ParamSet("/reader/region/hopTime", checc);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        bool HoptableCheck = true;
        private void txtbxHopTable_Cnot_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                string hoptable = txtbxHopTable_Cnot.Text;
                int[] hopTableFreqList = hoptable.Split(',').Select(s => int.Parse(s)).ToArray();
                objReader.ParamSet("/reader/region/hopTable", hopTableFreqList);
                //MessageBox.Show("HopTable updated successfully", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                //update regulatory tab hoptable as well based on above changed value.
                txtbxHopTable.Text = txtbxHopTable_Cnot.Text;
            }
            catch (Exception ex)
            {
                if (HoptableCheck == true)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    HoptableCheck = false;
                }
            }
        }
        /// <summary>
        /// This function is used for the Populate the components for the HF/LF
        /// </summary>
        private void ReaderTypeUIControl()
        {
            M3eFilterGroupBox.Visibility = Visibility.Collapsed;

            if (model.Contains("M3e"))
            {
                stkpnlUHFEmbeddedReadData1.Visibility = stkpnlUHFEmbeddedReadData0.Visibility = Visibility.Collapsed;
                stkpnlM3eSecureRead0.Visibility = stkpnlM3eBlockRead0.Visibility = stkpnlM3eBlockRead1.Visibility = Visibility.Visible;
                stackpanelRFPowerSetting.Visibility = Visibility.Collapsed;
                stackReadPower.Visibility = Visibility.Collapsed;
                stackWritePower.Visibility = Visibility.Collapsed;
                stackHFLFPower.Visibility = Visibility.Visible;
                tiTagInspector.Visibility = Visibility.Visible;
                tiWriteEPC.Visibility = Visibility.Collapsed;
                tiUserMemory.Visibility = Visibility.Visible;
                tiUserMemoryHeader.Text = "Write Tag Memory";
                tiUserMemoryHeader2.Text = "Write Tag Memory (TabItem)";
                tiUserMemoryContent.Text = "The Write Tag Memory Editor tag operation tab allows the contents of\n Memory of specific tags to be viewed and updated.";
                tiLockTag.Visibility = Visibility.Collapsed;
                tiUntraceable.Visibility = Visibility.Collapsed;
                tiAuthenticate.Visibility = Visibility.Collapsed;
                grpbxPerformanceTuning.Visibility = Visibility.Collapsed;
                chkbxAntennaDetection.Visibility = Visibility.Collapsed;
                TagResults.protocolColumn.Visibility = Visibility.Collapsed;
                TagResults.frequencyColumn.Visibility = Visibility.Collapsed;
                TagResults.antennaColumn.Visibility = Visibility.Collapsed;
                TagResults.phaseColumn.Visibility = Visibility.Collapsed;
                TagResults.GPIOColumn.Visibility = Visibility.Collapsed;
                TagResults.tagTypeColumn.Visibility = Visibility.Visible;
                TagResults.epcColumn.Header = "UID";
                TagResults.rssiColumn.Visibility = Visibility.Collapsed;
                stackPanel8.Margin = new Thickness(0, 2, 2, 2);
                chkApplyFilter1.IsEnabled = false;
                InitializeColumnSelectionCbx();
                expdrRegulatoryTesting.IsExpanded = true;
                expdrRegulatoryTesting.IsEnabled = true;
                tbcontinous.Visibility = Visibility.Collapsed;
                tbtimed.Visibility = Visibility.Collapsed;
                tbHopTable.Visibility = Visibility.Collapsed;
                tbM3eRegTest.Visibility = Visibility.Visible;
                tbRegulatoryTesting.SelectedIndex = 3;
                grpbxLoadSaveConfig.IsEnabled = false;
                sldrHFLFPwr.IsEnabled = false;
                txtbxHFLFValuedBm.IsReadOnly = true;
                M3eFilterGroupBox.Visibility = Visibility.Visible;
                chkEnableM3eFillter.IsChecked = false;
                FilterGroupBox.Visibility = Visibility.Collapsed;
                cbxAntennamux.Visibility = Visibility.Visible;
                expdrPerformanceTuning.Visibility = Visibility.Collapsed;
                TagResults.epcColumnInAscii.Header = "UID (ASCII)";
                stackPanel20.Visibility = Visibility.Visible;
                grpbxLoadSaveConfig.Visibility = Visibility.Collapsed;
                M3eFilterGroupBox.IsEnabled = true;
                stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = true;
                stkpnlProtocol0.Visibility = stkpnlProtocol1.Visibility = Visibility.Visible;
                stkpnlAntenna0.IsEnabled = stkpnlAntenna1.IsEnabled = true;
                stkpnlAntenna0.Visibility = stkpnlAntenna1.Visibility = Visibility.Collapsed;
                if ((bool)rdBtnReadOnce.IsChecked)
                {
                    rdBtnAutoProtocolSwitching.IsChecked = false;
                    rdBtnAutoProtocolSwitching.IsEnabled = false;
                    rdBtnEqualprotocolSwitching.IsChecked = true;
                }
            }
            else
            {
                stkpnlUHFEmbeddedReadData1.Visibility = stkpnlUHFEmbeddedReadData0.Visibility = Visibility.Visible;
                stkpnlM3eSecureRead0.Visibility = stkpnlM3eBlockRead0.Visibility = stkpnlM3eBlockRead1.Visibility = Visibility.Collapsed;
                stackpanelRFPowerSetting.Visibility = Visibility.Visible;
                stackReadPower.Visibility = Visibility.Visible;
                stackWritePower.Visibility = Visibility.Visible;
                stackHFLFPower.Visibility = Visibility.Collapsed;
                tiTagInspector.Visibility = Visibility.Visible;
                tiWriteEPC.Visibility = Visibility.Visible;
                tiUserMemory.Visibility = Visibility.Visible;
                tiUserMemoryHeader.Text = "User Memory";
                tiUserMemoryHeader2.Text = "User Memory (TabItem)";
                tiUserMemoryContent.Text = "The User Memory Editor tag operation tab allows the contents of\n User Memory of specific tags to be viewed and updated.";
                tiLockTag.Visibility = Visibility.Visible;
                tiUntraceable.Visibility = Visibility.Visible;
                tiAuthenticate.Visibility = Visibility.Visible;
                grpbxPerformanceTuning.Visibility = Visibility.Visible;
                chkbxAntennaDetection.Visibility = Visibility.Visible;
                TagResults.protocolColumn.Visibility = Visibility.Collapsed;
                TagResults.frequencyColumn.Visibility = Visibility.Collapsed;
                TagResults.antennaColumn.Visibility = Visibility.Collapsed;
                TagResults.phaseColumn.Visibility = Visibility.Collapsed;
                TagResults.GPIOColumn.Visibility = Visibility.Collapsed;
                TagResults.tagTypeColumn.Visibility = Visibility.Collapsed;
                TagResults.epcColumn.Header = "EPC";
                TagResults.rssiColumn.Visibility = Visibility.Visible;
                stackPanel8.Margin = new Thickness(25, 2, 2, 2);
                chkApplyFilter1.IsEnabled = true;
                InitializeColumnSelectionCbx();
                expdrRegulatoryTesting.IsExpanded = false;
                expdrRegulatoryTesting.IsEnabled = true;
                tbcontinous.Visibility = Visibility.Visible;
                tbtimed.Visibility = Visibility.Visible;
                tbHopTable.Visibility = Visibility.Visible;
                tbM3eRegTest.Visibility = Visibility.Collapsed;
                tbRegulatoryTesting.SelectedIndex = 0;
                grpbxLoadSaveConfig.IsEnabled = true;
                M3eFilterGroupBox.Visibility = Visibility.Collapsed;
                FilterGroupBox.Visibility = Visibility.Visible;
                cbxAntennamux.Visibility = Visibility.Visible;
                expdrPerformanceTuning.Visibility = Visibility.Visible;
                TagResults.epcColumnInAscii.Header = "EPC (ASCII)";
                stackPanel20.Visibility = Visibility.Visible;
                grpbxLoadSaveConfig.Visibility = Visibility.Visible;
                M3eFilterGroupBox.IsEnabled = false;
                stkpnlProtocol0.IsEnabled = stkpnlProtocol1.IsEnabled = true;
                stkpnlProtocol0.Visibility = stkpnlProtocol1.Visibility = Visibility.Collapsed;
                stkpnlAntenna0.IsEnabled = stkpnlAntenna1.IsEnabled = true;
                stkpnlAntenna0.Visibility = stkpnlAntenna1.Visibility = Visibility.Visible;
                cbxReadDataBank.ItemsSource = this.cbxDisplayReadDataBank();
            }
            expdrRegulatoryTesting.IsExpanded = false;
            this.ctMenu.ItemsSource = this.GetMenuItems(model);
            this.cbxDisplayEPCAs.ItemsSource = this.cbxDisplayEPCAsItems(model);
            #region If feature gets implemented
            //Fast search will be added above after it gets implemented in FW bug 7549
            chkEnableFastSearch.Visibility = Visibility.Visible;

            #endregion
        }

        /// <summary>
        /// Enable and Disable regulatory fields
        /// </summary>
        private void RegulatoryOperation()
        {
            if (lf134KHZCheckBox.Visibility == Visibility.Visible)
            {
                rdbLF134Frequency.IsEnabled = true;
            }
            else
            {
                rdbLF134Frequency.IsEnabled = false;
            }
        }

        private ObservableCollection<ComboBoxItem> cbxDisplayGPIItem(int GPICount)
        {
            ObservableCollection<ComboBoxItem> item = new ObservableCollection<ComboBoxItem>();

            ComboBoxItem GPISelect = new ComboBoxItem();
            ComboBoxItem GPI1 = new ComboBoxItem();
            ComboBoxItem GPI2 = new ComboBoxItem();
            ComboBoxItem GPI3 = new ComboBoxItem();
            ComboBoxItem GPI4 = new ComboBoxItem();

            GPISelect.Content = "Select";
            GPI1.Content = "1";
            GPI2.Content = "2";
            GPI3.Content = "3";
            GPI4.Content = "4";

            item.Add(GPISelect);
            if (GPICount == 1)
            {
                item.Add(GPI1);
            }
            else if (GPICount == 2)
            {
                item.Add(GPI1);
                item.Add(GPI2);
            }
            else if (GPICount == 3)
            {
                item.Add(GPI1);
                item.Add(GPI2);
                item.Add(GPI3);
            }
            else if (GPICount == 4)
            {
                item.Add(GPI1);
                item.Add(GPI2);
                item.Add(GPI3);
                item.Add(GPI4);
            }

            return item;
        }

        /// <summary>
        /// Removes and adds when ANtenna Mux and GPIO confiration is performed.
        /// </summary>
        /// <param name="gpi"></param>
        /// <returns></returns>
        private ObservableCollection<ComboBoxItem> CheckGPIPin(int gpi)
        {
            return new ObservableCollection<ComboBoxItem>();
        }

        /// <summary>
        /// Gets the updated Baudrate list.
        /// </summary>
        /// <param name="model">connected Model</param>
        /// <returns>Comboboxitem list of baudrate</returns>
        private ObservableCollection<ComboBoxItem> cbxDisplayBaudRateItem(string model)
        {
            ObservableCollection<ComboBoxItem> item = new ObservableCollection<ComboBoxItem>();

            ComboBoxItem baud1 = new ComboBoxItem();
            ComboBoxItem baud2 = new ComboBoxItem();
            ComboBoxItem baud3 = new ComboBoxItem();
            ComboBoxItem baud4 = new ComboBoxItem();
            ComboBoxItem baud5 = new ComboBoxItem();
            ComboBoxItem baud6 = new ComboBoxItem();
            ComboBoxItem baud7 = new ComboBoxItem();
            ComboBoxItem baud8 = new ComboBoxItem();
            ComboBoxItem baud9 = new ComboBoxItem();

            baud1.Content = "Select";
            baud2.Content = "9600";
            baud3.Content = "19200";
            baud4.Content = "38400";
            baud5.Content = "57600";
            baud6.Content = "115200";
            baud7.Content = "230400";
            baud8.Content = "460800";
            baud9.Content = "921600";


            item.Add(baud1);
            item.Add(baud2);
            item.Add(baud3);
            item.Add(baud4);
            item.Add(baud5);
            item.Add(baud6);
            item.Add(baud7);
            item.Add(baud8);
            if (!(model.Equals("M3e")))
            {
                item.Add(baud9);
            }

            return item;
        }

        /// <summary>
        /// Gets the updated Filter Memory Bank list.
        /// </summary>
        /// <param name="rdr">Reader type - Serial Reader or LLRPReader </param>
        /// <returns>Comboboxitem list of FilterMemBank</returns>
        private ObservableCollection<ComboBoxItem> cbxDisplayFilterMemBankItemsList(Reader rdr)
        {
            ObservableCollection<ComboBoxItem> item = new ObservableCollection<ComboBoxItem>();

            ComboBoxItem memBank1 = new ComboBoxItem();
            ComboBoxItem memBank2 = new ComboBoxItem();
            ComboBoxItem memBank3 = new ComboBoxItem();
            ComboBoxItem memBank4 = new ComboBoxItem();
            ComboBoxItem memBank5 = new ComboBoxItem();

            memBank1.Content = "EPC ID";
            memBank2.Content = "TID";
            memBank3.Content = "User";
            memBank4.Content = "EPC Truncate";
            memBank5.Content = "EPC Length";

            item.Add(memBank1);
            item.Add(memBank2);
            item.Add(memBank3);
            if (rdr is SerialReader)
            {
                item.Add(memBank4);
                item.Add(memBank5);
            }
            return item;
        }

        /// <summary>
        /// Gets combox item for Data format
        /// </summary>
        /// <param name="model">Model name</param>
        /// <returns>Return modified combox item</returns>
        private ObservableCollection<ComboBoxItem> cbxDisplayEPCAsItems(string model)
        {
            ObservableCollection<ComboBoxItem> item = new ObservableCollection<ComboBoxItem>();

            ComboBoxItem select = new ComboBoxItem();
            ComboBoxItem ascii = new ComboBoxItem();
            ComboBoxItem reverseBase36 = new ComboBoxItem();

            select.Content = "Select";
            ascii.Content = "ASCII";
            reverseBase36.Content = "ReverseBase36";

            item.Add(select);
            item.Add(ascii);
            if (!(model.Equals("M3e")))
            {
                item.Add(reverseBase36);
            }

            return item;
        }

        private ObservableCollection<ComboBoxItem> cbxDisplayReadDataBank()
        {
            ObservableCollection<ComboBoxItem> item = new ObservableCollection<ComboBoxItem>();

            ComboBoxItem tID = new ComboBoxItem();
            ComboBoxItem reserved = new ComboBoxItem();
            ComboBoxItem ePC = new ComboBoxItem();
            ComboBoxItem user = new ComboBoxItem();

            tID.Content = "TID";
            reserved.Content = "Reserved";
            ePC.Content = "EPC";
            user.Content = "User";

            item.Add(tID);
            item.Add(reserved);
            item.Add(ePC);
            item.Add(user);

            return item;
        }

        /// <summary>
        /// Get the model specific Menu Content 
        /// </summary>
        /// <param name="model">Model Name</param>
        /// <returns>Returns modified Menu Item Content</returns>
        private ObservableCollection<MenuItem> GetMenuItems(string model)
        {
            RoutedEventHandler click = new RoutedEventHandler(MenuItem_Click);
            ObservableCollection<MenuItem> menuItem = new ObservableCollection<MenuItem>();
            MenuItem tagInspector = new MenuItem();
            MenuItem writeEPC = new MenuItem();
            MenuItem writeMemory = new MenuItem();
            MenuItem lockTag = new MenuItem();
            MenuItem untraceable = new MenuItem();
            MenuItem authenticate = new MenuItem();
            MenuItem unsupported = new MenuItem();

            tagInspector.Click += click;
            writeEPC.Click += click;
            writeMemory.Click += click;
            lockTag.Click += click;
            untraceable.Click += click;
            authenticate.Click += click;

            tagInspector.Header = "Inspect Tag";
            writeEPC.Header = "Write EPC";
            lockTag.Header = "Lock Tag";
            untraceable.Header = "Untraceable";
            authenticate.Header = "Authenticate";
            unsupported.Header = "Unsupported Operation.";
            #region Changing the name of the Menuitem
            if (model.Equals("M3e"))
            {
                writeMemory.Header = "Edit Tag Memory";
            }
            else
            {
                writeMemory.Header = "Edit User Memory";
            }
            #endregion
            #region Adding to collection
            menuItem.Add(tagInspector);
            if (model.Equals("M3e"))
            {
                menuItem.Add(writeMemory);
                menuItem.Add(unsupported);
            }
            else
            {
                menuItem.Add(writeEPC);
                menuItem.Add(writeMemory);
                menuItem.Add(lockTag);
                menuItem.Add(untraceable);
                menuItem.Add(authenticate);
            }
            #endregion
            return menuItem;
        }

        private void txtbxHopTable_Cnot_TextChanged(object sender, TextChangedEventArgs e)
        {
            HoptableCheck = true;
        }
        /// <summary>
        /// This Function reset the default hop table for the network reader by rebooting it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDefaultHopTable_Click(object sender, RoutedEventArgs e)
        {
            //objReader.TMR_resetHoptable();
            //InitializeHopTable();
        }

        /// <summary>
        /// Get supported status tag feature
        /// </summary>
        private void TagFeature()
        {
            bool flag = false;
            try
            {
                if (iso15693CheckBox.Visibility == Visibility.Visible && (bool)iso15693CheckBox.IsChecked)
                {
                    SupportedTagFeatures tagFeature = (SupportedTagFeatures)objReader.ParamGet("/reader/iso15693/supportedTagFeatures");
                    if (tagFeature == SupportedTagFeatures.NONE)
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                if (lf125KHZCheckBox.Visibility == Visibility.Visible && (bool)lf125KHZCheckBox.IsChecked && flag == false)
                {
                    SupportedTagFeatures tagFeature = (SupportedTagFeatures)objReader.ParamGet("/reader/lf125khz/supportedTagFeatures");
                    if (tagFeature == SupportedTagFeatures.NONE)
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                if (iso14443aCheckBox.Visibility == Visibility.Visible && (bool)iso14443aCheckBox.IsChecked && flag == false)
                {
                    SupportedTagFeatures tagFeature = (SupportedTagFeatures)objReader.ParamGet("/reader/iso14443a/supportedTagFeatures");
                    if (tagFeature == SupportedTagFeatures.NONE)
                    {
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                chkSecureRead.IsEnabled = flag;
                if (flag == false)
                {
                    chkSecureRead.IsChecked = false;
                }
            }
            catch (Exception)
            {

            }
        }


        private void iso14443aCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            xctkTagUIBusyIndicator.IsBusy = true;
            if ((bool)iso14443aCheckBox.IsChecked)
            {
                //paramget we got all the tag type
                SerialReader reader = objReader as SerialReader;
                Iso14443a.TagType suppTagType;
                suppTagType = (Iso14443a.TagType)reader.ParamGet("/reader/iso14443a/supportedTagTypes");
                TagProtocol protocol = TagProtocol.ISO14443A;
                Iso14443a.TagType secureTagType = new Iso14443a.TagType();
                Int32 suppTagTypeInt = Convert.ToInt32(suppTagType);

                if (0 != (suppTagType & Iso14443a.TagType.MIFARE_PLUS))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("MIFARE PLUS", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso14443a.TagType.MIFARE_ULTRALIGHT))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("MIFARE ULTRALIGHT", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso14443a.TagType.MIFARE_CLASSIC))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("MIFARE CLASSIC", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso14443a.TagType.NTAG))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("NTAG", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso14443a.TagType.MIFARE_DESFIRE))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("MIFARE DESFIRE", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso14443a.TagType.MIFARE_MINI))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("MIFARE MINI", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
            }
            else
            {

                for (int i = 0; i < selectedTagColumnList.Count; )
                {
                    if (selectedTagColumnList[i].Key == TagProtocol.ISO14443A)
                    {
                        ResetAllTagTypeList(selectedTagColumnList[i].Value.TagName);
                        RemoveItemForFamilyList(selectedTagColumnList[i].Value.TagName);
                        selectedTagColumnList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
            xctkTagUIBusyIndicator.IsBusy = false;
            M3eFilterGroupBox.Visibility = Visibility.Visible;
            TagFeature();
        }

        private void iso14443bCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            M3eFilterGroupBox.Visibility = Visibility.Visible;
        }

        private void iso15693CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            xctkTagUIBusyIndicator.IsBusy = false;
            if ((bool)iso15693CheckBox.IsChecked)
            {
                SerialReader reader = objReader as SerialReader;
                Iso15693.TagType suppTagType;
                suppTagType = (Iso15693.TagType)reader.ParamGet("/reader/iso15693/supportedTagTypes");
                TagProtocol protocol = TagProtocol.ISO15693;
                Iso15693.TagType secureTagType = new Iso15693.TagType();
                Int32 suppTagTypeInt = Convert.ToInt32(suppTagType);


                //if (0 != (suppTagType & Iso15693.TagType.EM4133))
                //{
                //    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("EM4133", false));
                //    selectedTagColumnList.Add(keyValueTag);
                //}
                //if (0 != (suppTagType & Iso15693.TagType.EM4233))
                //{
                //    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("EM4233", false));
                //    selectedTagColumnList.Add(keyValueTag);
                //}
                //if (0 != (suppTagType & Iso15693.TagType.EM4237))
                //{
                //    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("EM4237", false));
                //    selectedTagColumnList.Add(keyValueTag);
                //}
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLI))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLI", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLI_L))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLI L", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLI_S))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLI S", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_DNA))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE DNA", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_DNA))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE DNA", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLIX))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLIX", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLIX_L))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLIX L", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLIX_S))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLIX S", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.ICODE_SLIX_2))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("ICODE SLIX2", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.HID_ICLASS_SE))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("iCLASS SE", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.VIGO))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("VIGO", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.TAGIT))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("TAGIT", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Iso15693.TagType.PICOPASS))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("PICOPASS", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
            }
            else
            {
                for (int i = 0; i < selectedTagColumnList.Count; )
                {
                    if (selectedTagColumnList[i].Key == TagProtocol.ISO15693)
                    {
                        ResetAllTagTypeList(selectedTagColumnList[i].Value.TagName);
                        RemoveItemForFamilyList(selectedTagColumnList[i].Value.TagName);
                        selectedTagColumnList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
            xctkTagUIBusyIndicator.IsBusy = false;
            M3eFilterGroupBox.Visibility = Visibility.Visible;
            TagFeature();
        }

        private void iso18092CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            M3eFilterGroupBox.Visibility = Visibility.Visible;
        }

        private void felicaCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            M3eFilterGroupBox.Visibility = Visibility.Visible;
        }

        private void iso180003mode3CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            M3eFilterGroupBox.Visibility = Visibility.Visible;
        }

        private void lf125KHZCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            if ((bool)lf125KHZCheckBox.IsChecked)
            {
                SerialReader reader = objReader as SerialReader;
                Lf125khz.TagType suppTagType;
                suppTagType = (Lf125khz.TagType)reader.ParamGet("/reader/lf125khz/supportedTagTypes");
                TagProtocol protocol = TagProtocol.LF125KHZ;

                Lf125khz.TagType secureTagType = new Lf125khz.TagType();
                Int32 suppTagTypeInt = Convert.ToInt32(suppTagType);

                if (0 != (suppTagType & Lf125khz.TagType.AWID))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("AWID", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.EM_4100))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("EM 4100", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.HID_PROX))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("PROX", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.HITAG_1))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("HITAG 1", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.HITAG_2))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("HITAG 2", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.KERI))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("KERI", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
                if (0 != (suppTagType & Lf125khz.TagType.INDALA))
                {
                    keyValueTag = new KeyValuePair<TagProtocol, ColumnSelectionForTagtype>(protocol, new ColumnSelectionForTagtype("INDALA", false));
                    selectedTagColumnList.Add(keyValueTag);
                }
            }
            else
            {
                for (int i = 0; i < selectedTagColumnList.Count; )
                {
                    if (selectedTagColumnList[i].Key == TagProtocol.LF125KHZ)
                    {
                        ResetAllTagTypeList(selectedTagColumnList[i].Value.TagName);
                        RemoveItemForFamilyList(selectedTagColumnList[i].Value.TagName);
                        selectedTagColumnList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
            M3eFilterGroupBox.Visibility = Visibility.Visible;
            TagFeature();
        }

        private void lf134KHZCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //Based on the protocol being picked, change the label on select screen accordingly.
            OnProtocolSelect();
            if ((bool)lf134KHZCheckBox.IsChecked)
            {
                SerialReader reader = objReader as SerialReader;
                Lf134khz.TagType suppTagType;
                suppTagType = (Lf134khz.TagType)reader.ParamGet("/reader/lf134khz/supportedTagTypes");
                TagProtocol protocol = TagProtocol.LF134KHZ;

                Lf134khz.TagType secureTagType = new Lf134khz.TagType();
                Int32 suppTagTypeInt = Convert.ToInt32(suppTagType);
            }
            else
            {
                for (int i = 0; i < selectedTagColumnList.Count; )
                {
                    if (selectedTagColumnList[i].Key == TagProtocol.LF134KHZ)
                    {
                        ResetAllTagTypeList(selectedTagColumnList[i].Value.TagName);
                        RemoveItemForFamilyList(selectedTagColumnList[i].Value.TagName);
                        selectedTagColumnList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
            M3eFilterGroupBox.Visibility = Visibility.Visible;
        }

        private void InitializeTagSelectionCbx(List<KeyValuePair<TagProtocol, ColumnSelectionForTagtype>> tagList)
        {
            ObservableCollection<ColumnSelectionForTagtype> columnSelectionForTagtype = new ObservableCollection<ColumnSelectionForTagtype>();
            foreach (KeyValuePair<TagProtocol, ColumnSelectionForTagtype> item in tagList)
            {
                if (!(columnSelectionForTagtype.Contains(item.Value)))
                {
                    columnSelectionForTagtype.Add(item.Value);
                }
            }
            if (!(TagAddControl))
            {


                InitializeFilterTagList(columnSelectionForTagtype);
            }
            lvTagType1.ItemsSource = columnSelectionForTagtype;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvTagType1.ItemsSource);
            view.Filter = TagFilter;
        }

        /// <summary>
        /// Find Tag in the Tag Family list
        /// </summary>
        /// <param name="list">Tag Family list</param>
        /// <param name="tagName">Tag to Find</param>
        /// <returns>Boolean</returns>
        private bool FindItemInList(ObservableCollection<Tag> list, string tagName)
        {
            foreach (Tag item in list)
            {
                if (item.Name.TagName == tagName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remove the Tag Combobox from the tag family list
        /// </summary>
        /// <param name="tagName">Tag name</param>
        private void RemoveItemForFamilyList(string tagName)
        {
            switch (TagFamilyDatabase.GetTagFamilyName(tagName))
            {
                case "HID Tag Family":
                    Tag item = hidTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    hidTagFamily.Remove(item);
                    break;
                case "MIFARE Tag Family":
                    Tag item1 = mifareTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    mifareTagFamily.Remove(item1);
                    break;
                case "EM Tag Family":
                    Tag item2 = emTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    emTagFamily.Remove(item2);
                    break;
                case "ICODE Tag Family":
                    Tag item3 = icodeTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    icodeTagFamily.Remove(item3);
                    break;
                case "Other Tags":
                    Tag item4 = otherTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    otherTagFamily.Remove(item4);
                    break;
                case "HITAG Tag Family":
                    Tag item5 = hiTagFamily.Where(x => x.Name.TagName == tagName).FirstOrDefault();
                    hiTagFamily.Remove(item5);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Add Tag to Tag Family
        /// </summary>
        /// <param name="columnSelectionForTagtype"></param>
        private void InitializeFilterTagList(ObservableCollection<ColumnSelectionForTagtype> columnSelectionForTagtype)
        {
            ObservableCollection<TagTypeComboBox> TagFamilyList = new ObservableCollection<TagTypeComboBox>();
            ObservableCollection<TagFamily> tagFamily = new ObservableCollection<TagFamily>();

            foreach (ColumnSelectionForTagtype item in columnSelectionForTagtype)
            {
                #region Adding Tag to list
                switch (TagFamilyDatabase.GetTagFamilyName(item.TagName))
                {
                    case "HID Tag Family":
                        if (!(FindItemInList(hidTagFamily, item.TagName)))
                        {
                            hidTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    case "MIFARE Tag Family":
                        if (!(FindItemInList(mifareTagFamily, item.TagName)))
                        {
                            mifareTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    case "EM Tag Family":
                        if (!(FindItemInList(emTagFamily, item.TagName)))
                        {
                            emTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    case "ICODE Tag Family":
                        if (!(FindItemInList(icodeTagFamily, item.TagName)))
                        {
                            icodeTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    case "Other Tags":
                        if (!(FindItemInList(otherTagFamily, item.TagName)))
                        {
                            otherTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    case "HITAG Tag Family":
                        if (!(FindItemInList(hiTagFamily, item.TagName)))
                        {
                            hiTagFamily.Add(new Tag() { Name = item });
                        }
                        break;
                    default:
                        break;
                }
                #endregion
            }

            #region Adding Tag Family Combobox to the ListView
            if (hidTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "iCLASS Tags", Members = hidTagFamily });
            }
            if (mifareTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "MIFARE Tags", Members = mifareTagFamily });
            }
            if (emTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "EM Tags", Members = emTagFamily });
            }
            if (icodeTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "ICODE Tags", Members = icodeTagFamily });
            }
            if (hiTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "HITAG Tags", Members = hiTagFamily });
            }
            if (otherTagFamily.Count > 0)
            {
                tagFamily.Add(new TagFamily() { Name = "Other Tags", Members = otherTagFamily });
            }

            #endregion

            this.Families = new ObservableCollection<TagFamily>();

            treeView.ItemsSource = tagFamily;
            foreach (TagFamily item in tagFamily)
            {
                foreach (Tag tag in item.Members)
                {
                    tag.SetValue(ItemHelper.ParentProperty, item);
                }
            }

            foreach (TagFamily family in tagFamily)
            {
                foreach (Tag tag in family.Members)
                {
                    ItemHelper.SetIsChecked(tag, tag.Name.IsTagChecked);
                    int count = family.Members.Count;
                    if (tag.Name.TagName == family.Members[count - 1].Name.TagName)
                    {
                        ItemHelper.CheckParent(tag);
                    }
                }
            }
        }

        private void AddTagToGrid(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = e.Source as CheckBox;
            if (checkBox.Content == null)
            {
                return;
            }
            else
            {
                if (checkBox.Content.ToString().Contains("Item"))
                {

                    return;
                }
            }
            string tagName = checkBox.Content.ToString();
            TagAddControl = true;
            if ((bool)checkBox.IsChecked)
            {
                AddAllTagTypeList(checkBox.Content.ToString());
                if (tagName.Contains("Tags"))
                {
                    allFamilyTag = true;
                    TagFamilyOperation(tagName, true);
                    allFamilyTag = false;
                }
                bool allTagsSelectedinFamily = false;
                if (!(tagName.Contains("Tags")))
                {
                    switch (TagFamilyDatabase.GetTagFamilyName(tagName))
                    {
                        case "HID Tag Family":
                            for (int i = 1; i < hidTagFamily.Count; i++)
                            {
                                if (hidTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        case "MIFARE Tag Family":
                            for (int i = 1; i < mifareTagFamily.Count; i++)
                            {
                                if (mifareTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        case "EM Tag Family":
                            for (int i = 1; i < emTagFamily.Count; i++)
                            {
                                if (emTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        case "ICODE Tag Family":
                            for (int i = 1; i < icodeTagFamily.Count; i++)
                            {
                                if (icodeTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        case "Other Tags":
                            for (int i = 1; i < otherTagFamily.Count; i++)
                            {
                                if (otherTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        case "HITAG Tag Family":
                            for (int i = 1; i < hiTagFamily.Count; i++)
                            {
                                if (hiTagFamily[i].Name.IsTagChecked)
                                {
                                    allTagsSelectedinFamily = true;
                                }
                                else
                                {
                                    allTagsSelectedinFamily = false;
                                    break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                ResetAllTagTypeList(checkBox.Content.ToString());
                if (tagName.Contains("Tags") && (!(TagRemoveControl)))
                {
                    TagFamilyOperation(tagName, false);
                }
                else if (!(tagName.Contains("Tags")))
                {
                    TagRemoveControl = true;
                    switch (TagFamilyDatabase.GetTagFamilyName(tagName))
                    {
                        case "HID Tag Family":
                            hidTagFamily[0].Name.IsTagChecked = false;
                            break;
                        case "MIFARE Tag Family":
                            mifareTagFamily[0].Name.IsTagChecked = false;
                            break;
                        case "EM Tag Family":
                            emTagFamily[0].Name.IsTagChecked = false;
                            break;
                        case "ICODE Tag Family":
                            icodeTagFamily[0].Name.IsTagChecked = false;
                            break;
                        case "Other Tags":
                            otherTagFamily[0].Name.IsTagChecked = false;
                            break;
                        case "HITAG Tag Family":
                            hiTagFamily[0].Name.IsTagChecked = false;
                            break;
                        default:
                            break;
                    }
                    TagRemoveControl = false;
                }
            }
            TagAddControl = false;
        }

        /// <summary>
        /// Adding and Removing selected tag.
        /// </summary>
        /// <param name="tagName">Tag Name</param>
        /// <param name="check">Set true and false in Tag Checkbox</param>
        private void TagFamilyOperation(string tagName, bool check)
        {
            if (tagName.Equals("iCLASS Tags"))
            {
                foreach (Tag item in hidTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
            if (tagName.Equals("MIFARE Tags"))
            {
                foreach (Tag item in mifareTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
            if (tagName.Equals("EM Tags"))
            {
                foreach (Tag item in emTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
            if (tagName.Equals("ICODE Tags"))
            {
                foreach (Tag item in icodeTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
            if (tagName.Equals("Other Tags"))
            {
                foreach (Tag item in otherTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
            if (tagName.Equals("HITAG Tags"))
            {
                foreach (Tag item in hiTagFamily)
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = item.Name.TagName;
                    if (check == (bool)chk.IsChecked)
                    {
                        chk.IsChecked = !(check);
                    }
                    chk.Checked += AddTagToGrid;
                    chk.Unchecked += AddTagToGrid;
                    chk.IsChecked = check;
                    item.Name.IsTagChecked = check;
                }
            }
        }

        private void RemoveTagFromList(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ColumnSelectionForTagButton tag = button.DataContext as ColumnSelectionForTagButton;
            if (button.Content.ToString().Contains("Item"))
            {
                return;
            }
            ResetAllTagTypeList(tag.ButtonNameTag);
        }

        private void AddAllTagTypeList(string tag)
        {
            if (tag.Contains("Tags"))
            {
                return;
            }
            ColumnSelectionForTagButton tagButton = new ColumnSelectionForTagButton(tag);
            bool flag = false;
            for (int i = 0; i < selectedTagForList.Count; i++)
            {
                if (selectedTagForList[i].ButtonNameTag == tagButton.ButtonNameTag)
                {
                    flag = true;
                }
            }
            if (!(flag))
            {
                selectedTagForList.Add(tagButton);
            }
            AddRemoveTagFromList(selectedTagForList);
            foreach (KeyValuePair<TagProtocol, ColumnSelectionForTagtype> item in selectedTagColumnList)
            {
                if (item.Value.TagName.Equals(tagButton.ButtonNameTag.ToString()))
                {
                    item.Value.IsTagChecked = true;
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
        }

        private void ResetAllTagTypeList(string tag)
        {
            ColumnSelectionForTagButton tagButton = new ColumnSelectionForTagButton(tag);
            foreach (ColumnSelectionForTagButton item in selectedTagForList)
            {
                if ((item.ButtonNameTag.Equals(tagButton.ButtonNameTag)))
                {
                    selectedTagForList.Remove(item);
                    break;
                }
            }
            AddRemoveTagFromList(selectedTagForList);
            foreach (KeyValuePair<TagProtocol, ColumnSelectionForTagtype> item in selectedTagColumnList)
            {
                if (item.Value.TagName.Equals(tagButton.ButtonNameTag.ToString()))
                {
                    item.Value.IsTagChecked = false;
                }
            }
            InitializeTagSelectionCbx(selectedTagColumnList);
        }

        private void AddRemoveTagFromList(List<ColumnSelectionForTagButton> collection)
        {
            lbTagType.ItemsSource = null;
            lbTagType.Items.Clear();
            lbTagType.ItemsSource = collection;
        }


        private bool TagFilter(object item)
        {
            if (String.IsNullOrEmpty(txtM3eTagFind.Text))
            {
                return true;
            }
            else
            {
                return ((item as ColumnSelectionForTagtype).TagName.IndexOf(txtM3eTagFind.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void txtM3eTagFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtM3eTagFind.Text == "" || txtM3eTagFind.Text == " ")
            {
                treeView.Visibility = Visibility.Visible;
                lvTagType1.Visibility = Visibility.Collapsed;
            }
            else
            {
                treeView.Visibility = Visibility.Collapsed;
                lvTagType1.Visibility = Visibility.Visible;
                CollectionViewSource.GetDefaultView(lvTagType1.ItemsSource).Refresh();
            }
        }


        /// <summary>
        /// Turns On and Off the regulatory Test for M3e modules
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFrequencyOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (objReader is SerialReader)
                {
                    SerialReader reader = objReader as SerialReader;
                    if (btnFrequencyOn.Content.ToString() == "Start")
                    {
                        btnFrequencyOn.IsEnabled = false;
                        if ((bool)rdbHFFrequency.IsChecked)
                        {
                            if (iso14443aCheckBox.IsVisible)
                            {
                                SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443A, null, null, 1000);
                                reader.ParamSet("/reader/read/plan", plan);
                                reader.StartReading();
                            }
                            else if (iso14443bCheckBox.IsVisible)
                            {
                                SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443B, null, null, 1000);
                                reader.ParamSet("/reader/read/plan", plan);
                                reader.StartReading();
                            }
                            else if (iso15693CheckBox.IsVisible)
                            {
                                SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO15693, null, null, 1000);
                                reader.ParamSet("/reader/read/plan", plan);
                                reader.StartReading();
                            }
                            else if (iso180003mode3CheckBox.IsVisible)
                            {
                                SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO180003M3, null, null, 1000);
                                reader.ParamSet("/reader/read/plan", plan);
                                reader.StartReading();
                            }

                        }
                        else if ((bool)rdbLF125Frequency.IsChecked)
                        {
                            SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.LF125KHZ, null, null, 1000);
                            reader.ParamSet("/reader/read/plan", plan);
                            reader.StartReading();
                        }
                        else if ((bool)rdbLF134Frequency.IsChecked)
                        {
                            SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.LF134KHZ, null, null, 1000);
                            reader.ParamSet("/reader/read/plan", plan);
                            reader.StartReading();
                        }
                        m3eRegulatoryUIControl(false);
                        btnFrequencyOn.IsEnabled = true;
                        btnFrequencyOn.Content = "Stop";
                    }
                    else
                    {
                        btnFrequencyOn.IsEnabled = false;
                        reader.StopReading();
                        m3eRegulatoryUIControl(true);
                        btnFrequencyOn.IsEnabled = true;
                        btnFrequencyOn.Content = "Start";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                m3eRegulatoryUIControl(true);
                btnFrequencyOn.Content = "Start";
                btnFrequencyOn.IsEnabled = true;
                return;
            }
        }

        private void m3eRegulatoryUIControl(bool flag)
        {
            expdrReadOptions.IsEnabled = flag;
            expdrPerformanceTuning.IsEnabled = flag;
            btnConnect.IsEnabled = flag;
            btnRead.IsEnabled = flag;
            stkpnlFrequency.IsEnabled = flag;
        }


        private void txtM3eUID_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtM3eUID.Text = IsUIDValid(txtM3eUID.Text);
        }

        private void txtM3eUID_PreviewKeyDown(object sender, TextCompositionEventArgs e)
        {

            string[] allowedChar = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };
            string text = e.Text.ToString().ToUpper();
            if (allowedChar.Contains(text))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }

        }


        private string IsUIDValid(string text)
        {
            string[] allowedChar = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F" };
            string tmp = text;
            foreach (char item in tmp.ToCharArray())
            {
                if (!(allowedChar.Contains(item.ToString())))
                {
                    tmp = tmp.Replace(item.ToString(), "");
                }
            }
            return tmp;
        }

        private bool IsNumberKey(Key inKey)
        {
            if (inKey < Key.D0 || inKey > Key.D9)
            {
                if (inKey < Key.NumPad0 || inKey > Key.NumPad9)
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsActionKey(Key inKey)
        {
            return inKey == Key.Delete || inKey == Key.Back || inKey == Key.Tab || inKey == Key.Return || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
        }


        private void EmbeddedReadControl(object sender, RoutedEventArgs e)
        {
            CheckBox chk = e.Source as CheckBox;
            if ((bool)chk.IsChecked)
            {
                if (chk.Name == "chkBlockRead")
                {
                    chkSecureRead.IsChecked = false;
                }
                else if (chk.Name == "chkSecureRead")
                {
                    chkBlockRead.IsChecked = false;
                }
            }
        }

        

        /// <summary>
        /// Makes LLRP GPIO UI visiable and collapse
        /// </summary>
        private void ValidateLLRPGPIOControl()
        {
            if (((bool)chbxLLRPGPI1.IsChecked) || ((bool)chbxLLRPGPI2.IsChecked) || ((bool)chbxLLRPGPI3.IsChecked) || ((bool)chbxLLRPGPI4.IsChecked))
            {
                txbLLRPGPIValue.Visibility = Visibility.Visible;
            }
            else
            {
                txbLLRPGPIValue.Visibility = Visibility.Collapsed;
            }
            if (((bool)chbxLLRPGPO1.IsChecked) || ((bool)chbxLLRPGPO2.IsChecked) || ((bool)chbxLLRPGPO3.IsChecked) || ((bool)chbxLLRPGPO4.IsChecked))
            {
                txbLLRPGPOValue.Visibility = Visibility.Visible;
            }
            else
            {
                txbLLRPGPOValue.Visibility = Visibility.Collapsed;
            }
        }

        private void configureGPIDirection()
        {
            int[] inputList = (int[])objReader.ParamGet("/reader/gpio/inputList");
            char returnValue;
            if ((bool)chbxLLRPGPI1.IsChecked)
            {
                returnValue = returnGPIOValue(inputList[0]);
                if (returnValue == 'L')
                {
                    rbdGPI1Low.IsEnabled = true;
                    rbdGPI1Low.IsChecked = true;
                    rbdGPI1High.IsChecked = false;
                    rbdGPI1High.IsEnabled = false;
                }
                else
                {
                    rbdGPI1Low.IsChecked = false;
                    rbdGPI1Low.IsEnabled = false;
                    rbdGPI1High.IsEnabled = true;
                    rbdGPI1High.IsChecked = true;
                }
            }
            if ((bool)chbxLLRPGPI2.IsChecked)
            {
                returnValue = returnGPIOValue(inputList[1]);
                if (returnValue == 'L')
                {
                    rbdGPI2Low.IsEnabled = true;
                    rbdGPI2Low.IsChecked = true;
                    rbdGPI2High.IsChecked = false;
                    rbdGPI2High.IsEnabled = false;
                }
                else
                {
                    rbdGPI2Low.IsChecked = false;
                    rbdGPI2Low.IsEnabled = false;
                    rbdGPI2High.IsEnabled = true;
                    rbdGPI2High.IsChecked = true;
                }
            }
            if ((bool)chbxLLRPGPI3.IsChecked)
            {
                returnValue = returnGPIOValue(inputList[2]);
                if (returnValue == 'L')
                {
                    rbdGPI3Low.IsEnabled = true;
                    rbdGPI3Low.IsChecked = true;
                    rbdGPI3High.IsChecked = false;
                    rbdGPI3High.IsEnabled = false;
                }
                else
                {
                    rbdGPI3Low.IsChecked = false;
                    rbdGPI3Low.IsEnabled = false;
                    rbdGPI3High.IsEnabled = true;
                    rbdGPI3High.IsChecked = true;
                }
            }
            if ((bool)chbxLLRPGPI4.IsChecked)
            {
                returnValue = returnGPIOValue(inputList[3]);
                if (returnValue == 'L')
                {
                    rbdGPI4Low.IsEnabled = true;
                    rbdGPI4Low.IsChecked = true;
                    rbdGPI4High.IsChecked = false;
                    rbdGPI4High.IsEnabled = false;
                }
                else
                {
                    rbdGPI4Low.IsChecked = false;
                    rbdGPI4Low.IsEnabled = false;
                    rbdGPI4High.IsEnabled = true;
                    rbdGPI4High.IsChecked = true;
                }
            }
        }


        private void chbxLLRPGPI1_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPI1.IsChecked)
            {
                stkpnlGPI1Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                configureGPIDirection();
            }
            else
            {
                stkpnlGPI1Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
        }

        private void chbxLLRPGPI2_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPI2.IsChecked)
            {
                stkpnlGPI2Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                configureGPIDirection();
            }
            else
            {
                stkpnlGPI2Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
        }

        private void chbxLLRPGPI3_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPI3.IsChecked)
            {
                stkpnlGPI3Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                configureGPIDirection();
            }
            else
            {
                stkpnlGPI3Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
        }

        private void chbxLLRPGPI4_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPI4.IsChecked)
            {
                stkpnlGPI4Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                configureGPIDirection();
            }
            else
            {
                stkpnlGPI4Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
        }

        private void configureGPODirection(int pin, bool high)
        {
            int[] outputList = (int[])(objReader.ParamGet("/reader/gpio/outputList"));
            try
            {
                GpioPin gp = new GpioPin(outputList[pin - 1], high);
                if (!gps.Contains(gp))
                {
                    gps.RemoveAll(p => p.Id == outputList[pin - 1]);
                    gps.Add(gp);
                }
                objReader.GpoSet(gps);
            }
            catch { }
        }

        private void chbxLLRPGPO1_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPO1.IsChecked)
            {
                stkpnlGPO1Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                rbdGPO1Low.IsChecked = true;
            }
            else
            {
                stkpnlGPO1Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
            configureGPODirection(1, false);
        }

        private void chbxLLRPGPO2_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPO2.IsChecked)
            {
                stkpnlGPO2Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                rbdGPO2Low.IsChecked = true;
            }
            else
            {
                stkpnlGPO2Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
            configureGPODirection(2, false);
        }

        private void chbxLLRPGPO3_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPO3.IsChecked)
            {
                stkpnlGPO3Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                rbdGPO3Low.IsChecked = true;
            }
            else
            {
                stkpnlGPO3Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
            configureGPODirection(3, false);
        }

        private void chbxLLRPGPO4_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)chbxLLRPGPO4.IsChecked)
            {
                stkpnlGPO4Value.Visibility = Visibility.Visible;
                ValidateLLRPGPIOControl();
                rbdGPO4Low.IsChecked = true;
            }
            else
            {
                stkpnlGPO4Value.Visibility = Visibility.Collapsed;
                ValidateLLRPGPIOControl();
            }
            configureGPODirection(4, false);
        }

        private void rbdGPO1Low_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(1, false);
        }

        private void rbdGPO1High_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(1, true);
        }

        private void rbdGPO2Low_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(2, false);
        }

        private void rbdGPO2High_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(2, true);
        }

        private void rbdGPO3Low_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(3, false);
        }

        private void rbdGPO3High_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(3, true);
        }

        private void rbdGPO4Low_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(4, false);
        }

        private void rbdGPO4High_Checked(object sender, RoutedEventArgs e)
        {
            configureGPODirection(4, true);
        }


        private void cbxGPIPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                if (cbxGPIPort.SelectedItem != null)
                {
                    string select = ((ComboBoxItem)cbxGPIPort.SelectedItem).Content.ToString();
                    if (select != "Select")
                    {
                        if (select == "1")
                        {
                            if ((bool)chbxGpo1.IsChecked)
                            {
                                chbxGpo1.IsChecked = false;
                            }
                            else if ((bool)chbxOne.IsChecked)
                            {
                                chbxOne.IsChecked = false;
                            }
                            chbxOne.IsEnabled = false;
                            chbxGpo1.IsEnabled = false;
                        }
                        else
                        {
                            chbxOne.IsEnabled = true;
                            chbxGpo1.IsEnabled = true;
                        }
                        if (select == "2")
                        {
                            if ((bool)chbxGpo2.IsChecked)
                            {
                                chbxGpo2.IsChecked = false;
                            }
                            else if ((bool)chbxTwo.IsChecked)
                            {
                                chbxTwo.IsChecked = false;
                            }
                            chbxTwo.IsEnabled = false;
                            chbxGpo2.IsEnabled = false;
                        }
                        else
                        {
                            chbxTwo.IsEnabled = true;
                            chbxGpo2.IsEnabled = true;
                        }
                        if (select == "3")
                        {
                            if ((bool)chbxGpo3.IsChecked)
                            {
                                chbxGpo3.IsChecked = false;
                            }
                            else if ((bool)chbxThree.IsChecked)
                            {
                                chbxThree.IsChecked = false;
                            }
                            chbxThree.IsEnabled = false;
                            chbxGpo3.IsEnabled = false;
                        }
                        else
                        {
                            chbxThree.IsEnabled = true;
                            chbxGpo3.IsEnabled = true;
                        }
                        if (select == "4")
                        {
                            if ((bool)chbxGpo4.IsChecked)
                            {
                                chbxGpo4.IsChecked = false;
                            }
                            else if ((bool)chbxFour.IsChecked)
                            {
                                chbxFour.IsChecked = false;
                            }
                            chbxFour.IsEnabled = false;
                            chbxGpo4.IsEnabled = false;
                        }
                        else
                        {
                            chbxFour.IsEnabled = true;
                            chbxGpo4.IsEnabled = true;
                        }
                    }
                    else
                    {
                        //If antenna multiplexing is selected and any of the GPOs are consumed for it, then disable the respective GPIO in GPIO Configuration.
                        //Otherwise, enable them so that users can select.
                        if ((bool)cbxAntennamux.IsChecked)
                        {
                            if ((bool)chbxOne.IsChecked)
                            {
                                chbxGpo1.IsEnabled = false;
                            }
                            if ((bool)chbxTwo.IsChecked)
                            {
                                chbxGpo2.IsEnabled = false;
                            }
                            if ((bool)chbxThree.IsChecked)
                            {
                                chbxGpo3.IsEnabled = false;
                            }
                            if ((bool)chbxFour.IsChecked)
                            {
                                chbxGpo4.IsEnabled = false;
                            }
                        }
                        else
                        {
                            chbxOne.IsEnabled = true;
                            chbxGpo1.IsEnabled = true;
                            chbxTwo.IsEnabled = true;
                            chbxGpo2.IsEnabled = true;
                            chbxThree.IsEnabled = true;
                            chbxGpo3.IsEnabled = true;
                            chbxFour.IsEnabled = true;
                            chbxGpo4.IsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void cbxUserMode_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            try {
                if (userModeComboList.Items.Count > 0)
                {
                    if (userModeComboList.SelectedItem != null)
                    {
                        if (userModeComboList.SelectedValue != null)
                        {
                            SerialReader.UserMode getUsrMode = usrModeToSet;
                            if (userModeComboList.SelectedValue.ToString() == "OPEN")
                            {
                                objReader.ParamSet("/reader/userMode", SerialReader.UserMode.OPEN);
                                // update the userModeToset value 
                                usrModeToSet = (SerialReader.UserMode)(Enum.Parse(typeof(SerialReader.UserMode), userModeComboList.SelectedValue.ToString()));
                                expdrOpenUserMode.IsExpanded = true;

                                //Enable all gen2 UI elements
                                enableGen2Settings();
                            }
                            else if(userModeComboList.SelectedValue.ToString() == "MEDICAL CABINET")
                            {
                                objReader.ParamSet("/reader/userMode", SerialReader.UserMode.MEDICAL_CABINET);
                                usrModeToSet = SerialReader.UserMode.MEDICAL_CABINET;
                                expdrOpenUserMode.IsExpanded = true;

                                //Get the gen2 settings 
                                InitializeOptimalSettings();

                                //Load the gen2 settings
                                LoadGen2Settings();
                            }
                            else if (!(Enum.Parse(typeof(SerialReader.UserMode), userModeComboList.SelectedValue.ToString()).Equals((object)getUsrMode)))
                            {
                                objReader.ParamSet("/reader/userMode", Enum.Parse(typeof(SerialReader.UserMode), userModeComboList.SelectedValue.ToString()));

                                // update the userModeToset value 
                                usrModeToSet = (SerialReader.UserMode)(Enum.Parse(typeof(SerialReader.UserMode), userModeComboList.SelectedValue.ToString()));
                                expdrOpenUserMode.IsExpanded = true;

                                //Get the gen2 settings 
                                InitializeOptimalSettings();

                                //Load the gen2 settings
                                LoadGen2Settings();
                            }
                            else
                            {
                                if (expdrOpenUserMode != null)
                                {
                                    expdrOpenUserMode.IsExpanded = false;
                                }
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Message",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Expander handler event to trigger when expanded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void expdrOpenRegion_Expanded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (regioncombo.SelectedItem.ToString() == "OPEN")
                {
                    expdrOpenRegConfig.IsEnabled = true;
                    expdrOpenRegConfig.Visibility = Visibility.Visible;

                    chkLBTEnable.IsChecked = (bool)objReader.ParamGet("/reader/region/lbt/enable");
                    chkEnableDwellTime.IsChecked = (bool)objReader.ParamGet("/reader/region/dwellTime/enable");
                    txtEnableLBTThreshold.Text = objReader.ParamGet("/reader/region/lbtThreshold").ToString();
                    txtDwelltime.Text = objReader.ParamGet("/reader/region/dwellTime").ToString();
                    txtHopTime.Text = objReader.ParamGet("/reader/region/hopTime").ToString();
                    txtQuantizationStep.Text = objReader.ParamGet("/reader/region/quantizationStep").ToString();
                    txtMinFreq.Text = objReader.ParamGet("/reader/region/minimumFrequency").ToString();
                    int[] hopTableList = (int[])objReader.ParamGet("/reader/region/hopTable");
                    txtbxHopTable_Cnot.Text = String.Join(",", hopTableList.Select(x => x.ToString()).ToArray());
                }
                e.Handled = true;
            }
            catch(ReaderException re)
            {
                Onlog(re);
            }
        }
        /// <summary>
        /// Function to check whether RF Mode is supported for the reader fw version
        /// </summary>
        /// <param name="version">firmware version</param>
        /// <returns>true or false indicating the status of feature support</returns>
        private bool isRFModeSupportedFW(String version)
        {
            try
            {
                Version readerVersion = null;
                Version checkVersion = new Version("2.1.3.28");//Converted to Decimal Version. Actual Version is 2.1.3.1C.;
                
                string tempreaderVersion = version;
                string[] versionSplit = version.Split('.');
                for (int i = 0; i < versionSplit.Length; i++)
                {
                    versionSplit[i] = int.Parse(versionSplit[i], System.Globalization.NumberStyles.HexNumber).ToString();
                }
                readerVersion = new Version(string.Join(".", versionSplit));

                if (checkVersion != null && readerVersion != null)
                {
                    if (readerVersion.CompareTo(checkVersion) >= 0)
                        return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Power mode selection changed listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxPowerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (pwrModeComboList.Items.Count > 0)
                {
                    if (pwrModeComboList.SelectedItem != null && pwrModeComboList.SelectedValue != null)
                    {
                        String pwrModeStr = ((ComboBoxItem)pwrModeComboList.SelectedItem).Content.ToString();
                        PowerMode usrSelectedPwrMode = PowerMode.INVALID;
                        switch (pwrModeStr)
                        {
                            case "FULL":
                                usrSelectedPwrMode = PowerMode.FULL;
                                break;
                            case "MINSAVE":
                                usrSelectedPwrMode = PowerMode.MINSAVE;
                                break;
                            case "SLEEP":
                                usrSelectedPwrMode = PowerMode.SLEEP;
                                break;
                            default:
                                usrSelectedPwrMode = PowerMode.INVALID;
                                break;
                        }
                        objReader.ParamSet("/reader/powerMode", usrSelectedPwrMode);
                        pwrModeComboList.SelectedItem = ((ComboBoxItem)pwrModeComboList.SelectedItem).Content.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reader Message",
                                     MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
