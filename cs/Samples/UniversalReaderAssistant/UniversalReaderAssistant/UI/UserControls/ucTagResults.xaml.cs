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
using ThingMagic.URA2.BL;
using System.Windows.Controls.Primitives;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for ucTagResults.xaml
    /// </summary>
    public partial class ucTagResults : UserControl
    {
        TagDatabase tagdb = new TagDatabase();
        public bool chkEnableTagAging = false;
        public bool enableTagAgingOnRead = false;

        TagReadRecordBindingList _tagList = new TagReadRecordBindingList();

        public TagReadRecordBindingList TagList
        {
            get { return _tagList; }
            set { _tagList = value; }
        }

        //TagDatabase tagdb = new TagDatabase();
        public ucTagResults()
        {
            InitializeComponent();
            GenerateColmnsForDataGrid();
            //dgTagResults.ItemsSource = tagdb.TagList;
            this.DataContext = TagList;
            dgTagResults.MouseRightButtonDown += new MouseButtonEventHandler(dgTagResults_MouseRightButtonDown);
        }

        /// <summary>
        /// Generate columns for datagrid
        /// </summary>
        public void GenerateColmnsForDataGrid()
        {
            dgTagResults.AutoGenerateColumns = false;
            serialNoColumn.Binding = new Binding("SerialNumber");
            serialNoColumn.Header = "#";
            serialNoColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            epcColumn.Binding = new Binding("EPC");
            epcColumn.Header = "EPC";
            epcColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            timeStampColumn.Binding = new Binding("TimeStamp");
            timeStampColumn.Binding.StringFormat = "{0:hh:mm:ss.fff tt}";
            timeStampColumn.Header = "Time Stamp (ms)";
            timeStampColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            rssiColumn.Binding = new Binding("RSSI");
            rssiColumn.Header = "RSSI (dBm)";
            rssiColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            readCountColumn.Binding = new Binding("ReadCount");
            readCountColumn.Header = "Read Count";
            readCountColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            antennaColumn.Binding = new Binding("Antenna");
            antennaColumn.Header = "Antenna";
            antennaColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            protocolColumn.Binding = new Binding("Protocol");
            protocolColumn.Header = "Protocol";
            protocolColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            frequencyColumn.Binding = new Binding("Frequency");
            frequencyColumn.Header = "Frequency (kHz)";
            frequencyColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            phaseColumn.Binding = new Binding("Phase");
            phaseColumn.Header = "Phase";
            phaseColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataColumn.Binding = new Binding("Data");
            dataColumn.Header = "Data";
            dataColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataSecureColumn.Binding = new Binding("Data");
            dataSecureColumn.Header = "Secure ID";
            dataSecureColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            epcColumnInAscii.Binding = new Binding("EPCInASCII");
            epcColumnInAscii.Header = "EPC (ASCII)";
            epcColumnInAscii.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            epcColumnInReverseBase36.Binding = new Binding("EPCInReverseBase36");
            epcColumnInReverseBase36.Header = "EPC (ReverseBase36)";
            epcColumnInReverseBase36.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dataColumnInAscii.Binding = new Binding("DataInASCII");
            dataColumnInAscii.Header = "Data (ASCII)";
            dataColumnInAscii.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            GPIOColumn.Binding = new Binding("GPIO");
            GPIOColumn.Header = "GPIO Status";
            GPIOColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            tagTypeColumn.Binding = new Binding("TagType");
            tagTypeColumn.Header = "Tag Type";
            tagTypeColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            //BrandIDColumn.Binding = new Binding("BrandID");
            //BrandIDColumn.Header = "BrandID";
            //BrandIDColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dgTagResults.ItemsSource = TagList;
        }

        #region EventHandler

        #region DataGridHeaderChkBox
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HeadCheck(sender, e, true);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            HeadCheck(sender, e, false);
        }

        private void HeadCheck(object sender, RoutedEventArgs e, bool IsChecked)
        {
            foreach (TagReadRecord mf in dgTagResults.Items)
            {
                mf.Checked = IsChecked;
            }
            dgTagResults.Items.Refresh();
        }
        #endregion

        /// <summary>
        /// Change the ToolTip content based on the state of header checkbox in datagrid
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
        /// Retain aged tag cell colour
        /// </summary>
        public Dictionary<string, Brush> tagagingColourCache = new Dictionary<string, Brush>();
        private void dgTagResults_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            try
            {
                if (chkEnableTagAging)
                {
                    var data = (TagReadRecord)e.Row.DataContext;
                    TimeSpan difftimeInSeconds = (DateTime.UtcNow - data.TimeStamp.ToUniversalTime());
                    BrushConverter brush = new BrushConverter();
                    if (enableTagAgingOnRead)
                    {
                        if (difftimeInSeconds.TotalSeconds < 12)
                        {
                            switch (Math.Round(difftimeInSeconds.TotalSeconds).ToString())
                            {
                                case "5":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFEEEEEE");
                                    break;
                                case "6":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFD3D3D3");
                                    break;
                                case "7":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFCCCCCC");
                                    break;
                                case "8":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFC3C3C3");
                                    break;
                                case "9":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFBBBBBB");
                                    break;
                                case "10":
                                    e.Row.Background = (Brush)brush.ConvertFrom("#FFA1A1A1");
                                    break;
                                case "11":
                                    e.Row.Background = new SolidColorBrush(Colors.Gray);
                                    break;
                            }
                            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate() { RetainAgingOnStopRead(data.SerialNumber.ToString(), e.Row.Background); }));
                        }
                        else
                        {
                            e.Row.Background = (Brush)brush.ConvertFrom("#FF888888");
                            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate() { RetainAgingOnStopRead(data.SerialNumber.ToString(), e.Row.Background); }));
                        }
                    }
                    else
                    {
                        if (tagagingColourCache.ContainsKey(data.SerialNumber.ToString()))
                        {
                            e.Row.Background = tagagingColourCache[data.SerialNumber.ToString()];
                        }
                        else
                        {
                            e.Row.Background = Brushes.White;
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// To retain colour of aged tag after stop reading
        /// </summary>
        /// <param name="slno"></param>
        /// <param name="row"></param>
        private void RetainAgingOnStopRead(string slno, Brush row)
        {
            if (!tagagingColourCache.ContainsKey(slno))
            {
                tagagingColourCache.Add(slno, row);
            }
            else
            {
                tagagingColourCache.Remove(slno);
                tagagingColourCache.Add(slno, row);
            }
        }

        #endregion EventHandler

        private void dgTagResults_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu ctMenu = (ContextMenu)App.Current.MainWindow.FindName("ctMenu");
            Button btnConnect = (Button)App.Current.MainWindow.FindName("btnConnect");
            try
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    // iteratively traverse the visual tree
                    DependencyObject dep = (DependencyObject)e.OriginalSource;
                    while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }

                    if (btnConnect.Content != null)
                    {
                        if (btnConnect.Content.ToString() == "Connect" || btnConnect.IsEnabled == false)
                        {
                            ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                            return;
                        }
                    }
                    else if (dep == null)
                    {
                        ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                        return;
                    }

                    if (dep is DataGridCell)
                    {
                        DataGridCell cell = dep as DataGridCell;

                        // navigate further up the tree
                        while ((dep != null) && !(dep is DataGridRow))
                        {
                            dep = VisualTreeHelper.GetParent(dep);
                        }

                        DataGridRow row = dep as DataGridRow;
                        DataGrid dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;

                        TagReadRecord tr = (TagReadRecord)row.Item;


                        if ((null != dataGrid.CurrentCell) && (tr.Protocol == TagProtocol.GEN2))
                        {
                            foreach (MenuItem item in ctMenu.Items)
                            {
                                item.IsEnabled = true;
                            }

                            if (dataGrid.CurrentCell.Column.Header.Equals("EPC") || (dataGrid.CurrentCell.Column.Header.Equals("Data"))
                                || dataGrid.CurrentCell.Column.Header.Equals("EPC(ASCII)") || (dataGrid.CurrentCell.Column.Header.Equals("Data(ASCII)"))
                                || dataGrid.CurrentCell.Column.Header.Equals("EPC(ReverseBase36)"))
                            {
                                Window mainWindow = App.Current.MainWindow;
                                if (dataGrid.CurrentCell.Column.Header.Equals("EPC") || dataGrid.CurrentCell.Column.Header.Equals("EPC(ASCII)")
                                    || dataGrid.CurrentCell.Column.Header.Equals("EPC(ReverseBase36)"))
                                    txtSelectedCell.Text = "EPC";
                                else
                                    txtSelectedCell.Text = "Data";

                                ctMenu.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                            {
                                ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                                //Console.WriteLine("Else case Current cell" + dataGrid.CurrentCell.Column.Header.ToString());
                            }
                            int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
                        }
                        else if ((null != dataGrid.CurrentCell) && (tr.Protocol == TagProtocol.ATA))
                        {
                            foreach (MenuItem item in ctMenu.Items)
                            {
                                if (item.Header.ToString() != "Inspect Tag")
                                {
                                    item.IsEnabled = false;
                                }
                            }
                            ctMenu.Visibility = System.Windows.Visibility.Visible;
                        }
                        else if ((null != dataGrid.CurrentCell) && ((tr.Protocol == TagProtocol.ISO15693) || (tr.Protocol == TagProtocol.ISO14443A)))
                        {
                            foreach (MenuItem item in ctMenu.Items)
                            {
                                if (item.Header.ToString() == "Unsupported Operation.")
                                {
                                    item.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    item.Visibility = Visibility.Visible;
                                }
                                item.IsEnabled = true;
                            }
                            ctMenu.Visibility = System.Windows.Visibility.Visible;
                        }
                        else if ((null != dataGrid.CurrentCell) && (tr.Protocol == TagProtocol.ISO14443B) || (tr.Protocol == TagProtocol.ISO180003M3) || (tr.Protocol == TagProtocol.FELICA) || (tr.Protocol == (TagProtocol.ISO18092) || (tr.Protocol == TagProtocol.LF125KHZ) || (tr.Protocol == TagProtocol.LF134KHZ)))
                        {
                            foreach (MenuItem item in ctMenu.Items)
                            {
                                if (item.Header.ToString() == "Unsupported Operation.")
                                {
                                    item.Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    item.Visibility = Visibility.Collapsed;
                                }
                                item.IsEnabled = false;
                            }
                            ctMenu.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                            //Console.WriteLine("Current Cell is not there" + dataGrid.CurrentCell.Column.Header.ToString());
                        }
                    }
                    else
                    {
                        ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                        //Console.WriteLine("not on a data grid cell" );
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                {
                    ctMenu.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show(ex.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void dgTagResults_LostFocus(object sender, RoutedEventArgs e)
        {
            dgTagResults.UnselectAll();
            ContextMenu ctMenu = (ContextMenu)App.Current.MainWindow.FindName("ctMenu");
            ctMenu.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
