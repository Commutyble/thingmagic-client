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
using System.Globalization;
using ThingMagic.URA2.BL;
using ThingMagic.URA2.Models;
using System.Text.RegularExpressions;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for UserMemory.xaml
    /// </summary>
    public partial class UserMemory : UserControl
    {
        Reader objReader;
        uint startAddress = 0;
        int antenna = 0;
        int dataLength = 0;
        string model = string.Empty;
        Gen2.Bank selectMemBank;
        TagReadRecord selectedTagReadRecord;
        TextBox[] txtbxHexRepList = null;

        // Cache the ascii data received from module for future modifications
        private string editedAsciiNewString = string.Empty;
        private string originalAsciiNewString = string.Empty;

        #region M3e variables
        private IList<byte> cacheData = null;
        private IList<byte> cacheEditedData = null;
        private uint cacheBlockSize = 0;
        private uint cacheBlockCount = 0;
        private IList<Byte> cacheLockStatus = null;
        private IList<int> editBlockNumber = null;
        private string originalM3eAsciiString = string.Empty;
        private string editedM3eAsciiString = string.Empty;
        TextBox[] txtHexList = null;
        #endregion

        #region Protocols
        private CheckBox cbxISO14443A;

        public CheckBox CbxISO14443A
        {
            get { return cbxISO14443A; }
            set { cbxISO14443A = value; }
        }

        private CheckBox cbxISO15693;

        public CheckBox CbxISO15693
        {
            get { return cbxISO15693; }
            set { cbxISO15693 = value; }
        }

        #endregion

        public UserMemory()
        {
            InitializeComponent();
        }

        public void LoadUserMemory(Reader reader, string readerModel)
        {
            objReader = reader;
            model = readerModel;
            InitializeTagView();
        }

        public void txtblk0ByteAddressClmn1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                int hexNumber;
                if (null != objReader)
                {
                    e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
                    TextBox txt = (TextBox)sender;
                    string temp = txt.Text;
                    if (model.Equals("M3e"))
                    {
                        btnM3eWriteHex.IsEnabled = true;
                        string num = txt.Name.Split(new string[] { "txtCellNumber" }, StringSplitOptions.None)[1].ToString();
                        string[] cellDetail = num.Split(new string[] { "_" }, StringSplitOptions.None);
                        string blockNumber = cellDetail[0];
                        int blockcount = Convert.ToInt32(blockNumber);
                        int blockcount1 = ((blockcount * (byte)cacheBlockSize));
                        int editedCell = (((blockcount1 < 0) ? 0 : blockcount1) + Convert.ToInt32(cellDetail[1]));
                        byte[] originalData = cacheData.ToArray();
                        if (originalData[editedCell].ToString() != (txt.Text + e.Text))
                        {
                            if (editBlockNumber == null)
                            {

                                editBlockNumber = new List<int>() { blockcount };
                            }
                            else
                            {
                                if (!(editBlockNumber.Contains(blockcount)))
                                {
                                    editBlockNumber.Add(blockcount);
                                }
                            }
                            txt.Foreground = Brushes.Red;
                        }
                        else
                        {
                            txt.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        btnWrite.IsEnabled = true;
                        if (CacheUserMem.Count > 0)
                        {
                            if (CacheUserMem[txt.Name.ToString()].ToString() != txt.Text)
                            {
                                txt.Foreground = Brushes.Red;
                                string num = txt.Name.Split(new string[] { "txtNumber" }, StringSplitOptions.None)[1].ToString();
                                editedDataCellLocation.Add(Convert.ToUInt32(num) - 1);
                                btnWriteAscii.IsEnabled = true;
                            }
                            else
                            {
                                txt.Foreground = Brushes.Black;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void LoadUserMemory(Reader reader, uint address, Gen2.Bank selectedBank, TagReadRecord selectedTagRed, string readerModel)
        {
            try
            {
                InitializeTagView();
                objReader = reader;
                startAddress = address;
                model = readerModel;
                spUserMemory.IsEnabled = true;
                rbFirstTagUserMemTb.IsEnabled = true;
                rbSelectedTagUserMemTb.IsChecked = true;
                rbSelectedTagUserMemTb.IsEnabled = true;

                lblUserMemoryError.Content = "";
                lblErrorAddAsciiEdtr.Content = "";
                lblErrorAddHexEdtr.Content = "";

                btnRead.Content = "Refresh";
                selectedTagReadRecord = selectedTagRed;
                antenna = selectedTagRed.Antenna;
                selectMemBank = selectedBank;
                //txtEPCData.Text = selectedTagRed.EPC;
                string[] stringData = selectedTagRed.Data.Split(' ');
                txtEpc.Text = selectedTagRed.EPC;
                currentEPC = txtEpc.Text;
                txtData.Text = string.Join("", stringData);
                Window mainWindow = App.Current.MainWindow;
                ucTagResults tagResults = (ucTagResults)mainWindow.FindName("TagResults");
                if (model.Equals("M3e"))
                {
                    if (selectedTagRed.Protocol == TagProtocol.ISO14443A)
                    {
                        Iso14443a.TagType tag;
                        Enum.TryParse(selectedTagRed.TagType.ToString().Replace(' ', '_'), out tag);
                        tag14443A = tag;
                        Decode14443A(selectedTagRed.EPC, tag);
                    }
                    else if (selectedTagRed.Protocol == TagProtocol.ISO15693)
                    {
                        Iso15693.TagType tag;
                        Enum.TryParse(selectedTagRed.TagType.ToString().Replace(' ', '_'), out tag);
                        tag15693 = tag;
                        Decode15693Tag(selectedTagRed.EPC, tag);
                    }

                }
                else
                {
                    switch (selectedBank)
                    {
                        case Gen2.Bank.EPC:
                            if (tagResults.txtSelectedCell.Text == "Data")
                            {
                                lblSelectFilter.Content = "EPC data at decimal address " + address.ToString() + "  : " + txtData.Text;
                            }
                            else
                            {
                                lblSelectFilter.Content = "EPC ID : " + selectedTagRed.EPC;
                            }
                            break;
                        case Gen2.Bank.TID:
                            if (tagResults.txtSelectedCell.Text == "Data")
                            {
                                lblSelectFilter.Content = "TID data at decimal address " + address.ToString() + " : " + txtData.Text;
                            }
                            else
                            {
                                lblSelectFilter.Content = "EPC ID : " + selectedTagRed.EPC;
                            }
                            break;
                        case Gen2.Bank.USER:
                            if (tagResults.txtSelectedCell.Text == "Data")
                            {
                                lblSelectFilter.Content = "User data at decimal address " + address.ToString() + " : " + txtData.Text;
                            }
                            else
                            {
                                lblSelectFilter.Content = "EPC ID : " + selectedTagRed.EPC;
                            }
                            break;
                    }
                    PopulateUserData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        string currentEPC = string.Empty;
        Iso14443a.TagType tag14443A = Iso14443a.TagType.AUTO_DETECT;
        Iso15693.TagType tag15693 = Iso15693.TagType.AUTO_DETECT;

        /// <summary>
        /// Clicked command binding for Read or Refresh button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Wait);
            if (!(model.Equals("M3e")))
            {
                Window mainWindow = App.Current.MainWindow;
                ComboBox CheckRegionCombobx = (ComboBox)mainWindow.FindName("regioncombo");
                if (CheckRegionCombobx.SelectedValue.ToString() == "Select")
                {
                    MessageBox.Show("Please select region", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (GetSelectedAntennaList() == null)
                {
                    MessageBox.Show("Please select at least one antenna", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(Main))
                        {
                            if (!(window as Main).expdrReadOptions.IsExpanded)
                            {
                                (window as Main).expdrReadOptions.IsExpanded = true;
                                (window as Main).expdrReadOptions.Focus();
                            }
                        }
                    }
                    return;
                }
            }
            else
            {
                if (CbxISO14443A.IsChecked == false && CbxISO15693.IsChecked == false)
                {
                    MessageBox.Show("Please select Protocol", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            TagReadData[] tagReads = null;
            try
            {
                if (model.Equals("M3e"))
                {
                    #region M3e implementation
                    if (btnRead.Content.Equals("Read"))
                    {
                        #region Protocol region 
                        if ((bool)CbxISO14443A.IsChecked==false&&(bool)CbxISO15693.IsChecked==false)
                        {
                            MessageBox.Show("Please select Protocol", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        #endregion

                        if ((bool)CbxISO14443A.IsChecked == true && (bool)CbxISO15693.IsChecked == false)
                        {
                            SimpleReadPlan plan = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443A, null, null, 1000);
                            objReader.ParamSet("/reader/read/plan", plan);
                        }
                        else if ((bool)CbxISO14443A.IsChecked == false && (bool)CbxISO15693.IsChecked == true)
                        {
                            SimpleReadPlan srp = new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO15693, null, null, 0);
                            objReader.ParamSet("/reader/read/plan", srp);
                        }
                        else
                        {
                            List<ReadPlan> readPlans = new List<ReadPlan>();
                            readPlans.Add(new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443A, null, 0));
                            readPlans.Add(new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO15693, null, 0));
                            MultiReadPlan multiReadPlan = new MultiReadPlan(readPlans);
                            objReader.ParamSet("/reader/read/plan", multiReadPlan);
                        }

                        {
                            SerialReader serialReader = objReader as SerialReader;
                            serialReader.IsProtocolDynamicSwitching = false;
                        }

                        tagReads = objReader.Read(750);

                        if ((tagReads != null) && (tagReads.Length != 0) && (tagReads[0].EpcString != ""))
                        {
                            if (tagReads.Length > 1)
                            {
                                lblUserMemoryError.Content = "Warning: More than one tag responded";
                                lblUserMemoryError.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                            {
                                lblUserMemoryError.Visibility = System.Windows.Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            txtEpc.Text = "";
                            currentEPC = string.Empty;
                            MessageBox.Show("No tags found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        {
                            objReader.ParamSet("/reader/tagop/protocol", TagProtocol.ISO14443A);
                            if (tagReads[0].Tag.Protocol == TagProtocol.ISO14443A)
                            {
                                Decode14443A(tagReads[0].EpcString, (Iso14443a.TagType)tagReads[0].TagType);
                            }
                            else if (tagReads[0].Tag.Protocol == TagProtocol.ISO15693)
                            {
                                Decode15693Tag(tagReads[0].EpcString, (Iso15693.TagType)tagReads[0].TagType);
                            }
                        }
                    }
                    else
                    {
                        rbSelectedTagUserMemTb.IsChecked = true;
                        if(selectedTagReadRecord.Protocol == TagProtocol.ISO14443A)
                        {
                            Decode14443A(currentEPC, tag14443A);
                        }
                        if (selectedTagReadRecord.Protocol == TagProtocol.ISO15693)
                        {
                            Decode15693Tag(currentEPC, tag15693);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region UHF
                    if (btnRead.Content.Equals("Read"))
                    {
                        SimpleReadPlan srp = new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), TagProtocol.GEN2, null, 0);
                        objReader.ParamSet("/reader/read/plan", srp);
                        tagReads = objReader.Read(500);
                        if ((null != tagReads) && (tagReads.Length > 0))
                        {
                            currentEPC = tagReads[0].EpcString;
                            txtEpc.Text = tagReads[0].EpcString;
                            lblSelectFilter.Content = "EPC ID : " + tagReads[0].EpcString;
                            if (tagReads.Length > 1)
                            {
                                lblUserMemoryError.Content = "Warning: More than one tag responded";
                                lblUserMemoryError.Visibility = System.Windows.Visibility.Visible;
                            }
                            else
                            {
                                lblUserMemoryError.Visibility = System.Windows.Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            txtEpc.Text = "";
                            currentEPC = string.Empty;
                            MessageBox.Show("No tags found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            txtblkSpMAXSizeHexByteSp64BytesAvailable.Text = "00";
                            txtblkSpMAXSizeCharsAvailable.Text = "00";
                            txtblkRemainingCharsAvailableCount.Text = "00 characters remaining";
                            txtASCIIData.IsEnabled = false;
                            RemoveGeneratedControl();
                            return;
                        }
                    }
                    else
                    {
                        rbSelectedTagUserMemTb.IsChecked = true;
                    }
                    //Display user bank data
                    PopulateUserData();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        #region ISO14443A section
        private void Decode14443A(string tagRead, Iso14443a.TagType tagType)
        {
            currentEPC = tagRead;
            lblSelectFilter.Content = "UID : " + currentEPC;
            Populate14443AData(tagType, tagRead);
        }

        private void Populate14443AData(Iso14443a.TagType tag, string UID)
        {
            objReader.ParamSet("/reader/tagop/protocol", TagProtocol.ISO14443A);

            #region Getting the tag memory
            string[] tagMemory = TagMemoryArchitecture.Get14443AMemoryLayout(tag);
            uint blockCount = 0;
            byte[] data = TagMemoryArchitecture.Get14443AMemory(objReader, tag, ref blockCount, UID).ToArray();
            tagMemory[1] = blockCount.ToString();
            #endregion

            #region Get block Lock Status
            List<byte> lockStatus = new List<byte>();//= TagMemoryArchitecture.MemoryLockStatus(objReader, tagMemory["Block Count"], TagProtocol.ISO15693, UID);
            lockStatus.AddRange(TagMemoryArchitecture.Lockstatus.ToArray());
            #region Lock status logic
            if (lockStatus.Count != blockCount)
            {
                for (int j = lockStatus.Count; j < Convert.ToInt32(tagMemory[1]); j++)
                {
                    lockStatus.Add(00);
                }
            }
            #endregion

            #endregion

            #region Cache Data for future Use
            cacheData = data;
            cacheEditedData = data;
            cacheBlockSize = (byte)(Convert.ToInt32(tagMemory[0]));
            cacheBlockCount = (byte)(Convert.ToInt32(tagMemory[1]));
            cacheLockStatus = lockStatus;
            #endregion

            #region Get User Memory of the tag

            GenerateASCIIControls(data.ToArray());
            RemoveGeneratedControlM3e();
            GenerateControls(data, (Convert.ToInt32(tagMemory[0])), (Convert.ToInt32(tagMemory[1])), lockStatus);

            if ((bool)rbHexRep.IsChecked)
            {
                currentEpcRep = "Hex";
            }
            else
            {
                currentEpcRep = "Ascii";
            }

            #endregion
        }
        #endregion

        #region ISO15693 Section
        /// <summary>
        /// Function Starts populating ISo15693 Tag data.
        /// </summary>
        /// <param name="tagRead">Tag UID</param>
        /// <param name="tagType">Tag Type</param>
        private void Decode15693Tag(string tagRead, Iso15693.TagType tagType)
        {
            currentEPC = tagRead;
            lblSelectFilter.Content = "UID : " + currentEPC;
            Populate15693Data(tagType, tagRead);
        }
        /// <summary>
        /// Populate ISO15693 tags
        /// </summary>
        /// <param name="tag">Selected tag type</param>
        /// <param name="UID">Selected tag UID</param>
        private void Populate15693Data(Iso15693.TagType tag, string UID)
        {
            objReader.ParamSet("/reader/tagop/protocol", TagProtocol.ISO15693);
            #region Getting the tag memory organisation
            Dictionary<string, uint> tagMemory = TagMemoryArchitecture.Get15693AMemoryLayout(objReader, tag, UID);
            #endregion

            #region Get block Lock Status
            List<byte> lockStatus = new List<byte>();
            if ((tag != Iso15693.TagType.ICODE_SLI_L) && (tag != Iso15693.TagType.ICODE_SLIX_L)
                 && (tag != Iso15693.TagType.PICOPASS) && (tag != Iso15693.TagType.HID_ICLASS_SE))
            {
                lockStatus = TagMemoryArchitecture.MemoryLockStatus(objReader, tagMemory["Block Count"], TagProtocol.ISO15693, UID);
            }
            else
            {
                for (int j = 0; j < tagMemory["Block Count"]; j++)
                {
                    lockStatus.Add(00);
                }
            }
            #endregion

            #region Get the Tag Memory
            uint blockLength = tagMemory["Block Count"];//there we will add the data which we get from system info.

            List<byte> data = TagMemoryArchitecture.Get15693Memory(objReader, tag, blockLength, tagMemory["Block Size"], UID);

            #endregion

            #region Cache Data for future Use
            cacheData = data;
            cacheEditedData = data;
            cacheBlockSize = tagMemory["Block Size"];
            cacheBlockCount = tagMemory["Block Count"];
            cacheLockStatus = lockStatus;
            #endregion

            #region Get User Memory of the tag

            GenerateASCIIControls(data.ToArray());
            RemoveGeneratedControlM3e();
            GenerateControls(data.ToArray(), (byte)tagMemory["Block Size"], (byte)tagMemory["Block Count"], lockStatus);

            if ((bool)rbHexRep.IsChecked)
            {
                currentEpcRep = "Hex";
            }
            else
            {
                currentEpcRep = "Ascii";
            }

            #endregion
        }
        #endregion

        #region To generate ASCII design in M3e
        private void GenerateASCIIControls(byte[] data)
        {
            string asciiData = Utilities.HexStringToAsciiString(ByteFormat.ToHex(data, "", ""));
            string ReplacedASCIIData = ReplaceSpecialCharInAsciiData(asciiData.ToCharArray());
            txtM3eDataASCII.Text = "";
            txtM3eDataASCII.Text = ReplacedASCIIData;
            txtM3eDataASCII.IsEnabled = true;
            txtblkTotalCount.Text = cacheData.Count.ToString();
            originalM3eAsciiString = asciiData;
            editedM3eAsciiString = asciiData;
            txtM3eDataASCII.Foreground = Brushes.Black;
        }
        #endregion

        #region To generate UI design for Hex in M3e

        /// <summary>
        /// Removes the dynamically created textbox and stack panel controls in parent stack panel of Hex representation
        /// </summary>
        private void RemoveGeneratedControlM3e()
        {
            try
            {
                StackPanel stkpnlByteAdd = (StackPanel)this.FindName("stkpnlBlockAddressHex");

                // Loop through the parent stackpanel and get its child elements
                foreach (StackPanel stkpnlChild in stkpnlByteAdd.Children)
                {


                    // Unregister the control so that this exception can be resolved "Cannot register duplicate Name 'stkPnl' in this scope."
                    // Because textbox controls are created dynamically for each read.
                    stkpnlByteAdd.UnregisterName(stkpnlChild.Name.ToString());
                    // Removes all the elements present in the each child stackpanel element
                    stkpnlChild.Children.Clear();
                }

                StackPanel stkpnlHexClmnAddRep = (StackPanel)this.FindName("stkpnlM3eByteAddressHex");
                stkpnlHexClmnAddRep.Children.Clear();

                // Removes all the child stack panels in the parent stack panel
                stkpnlByteAdd.Children.Clear();
                stkpnlByteAdd.InvalidateVisual();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Test   " + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Creats UI elements and fills the data Data Editor in hex format.
        /// </summary>
        /// <param name="data">Data in Hex</param>
        /// <param name="blockSizeByte">Size of the block</param>
        /// <param name="blockCountByte">Number of block</param>
        /// <param name="lockStatus">Lock Status of each block</param>
        private void GenerateControls(byte[] data, int blockSizeByte, int blockCountByte, List<byte> lockStatus)
        {
            try
            {


                if (data.Length != cacheData.Count)
                {
                    List<byte> tempData = new List<byte>();
                    for (int i = 0; i < cacheData.Count; i++)
                    {
                        if (i < data.Length)
                        {
                            tempData.Add(data[i]);
                        }
                        else
                        {
                            tempData.Add(0x00);
                        }
                    }
                    data = tempData.ToArray();
                }
                bool isLastTagBlockAdded = false;
                //TextBox[] txtBlockCellList = new TextBox[data.Length];
                List<TextBox> txtBlockCellList = new List<TextBox>();
                txtHexList = new TextBox[data.Length];
                int count = 0;

                // Create new label the represent byte's in next address eg: Byte 1, Byte 2 etc.
                Label lblEmptyGridCell = new Label();
                lblEmptyGridCell.Height = 30;
                lblEmptyGridCell.Width = 57;
                lblEmptyGridCell.VerticalAlignment = VerticalAlignment.Top;
                lblEmptyGridCell.HorizontalAlignment = HorizontalAlignment.Left;
                lblEmptyGridCell.Content = "";
                stkpnlM3eByteAddressHex.Children.Add(lblEmptyGridCell);
                // Create a namescope for the yte Address panel
                NameScope.SetNameScope(stkpnlM3eByteAddressHex, new NameScope());
                int byteAddressCount = Convert.ToInt32(blockSizeByte) + 1;
                for (int i = 0; i < byteAddressCount; i++)//here we will Add the total byte address count
                {
                    // Create a new label to represent Byte Address eg: Byte 1, Byte 2 etc.
                    Label lblAddressGridCell = new Label();
                    lblAddressGridCell.Height = 50;
                    lblAddressGridCell.Width = 65;
                    lblAddressGridCell.VerticalAlignment = VerticalAlignment.Top;
                    lblAddressGridCell.HorizontalAlignment = HorizontalAlignment.Left;
                    lblAddressGridCell.HorizontalContentAlignment = HorizontalAlignment.Center;
                    lblAddressGridCell.Content = ((i) != (byteAddressCount - 1)) ? ("Byte " + i.ToString()) : ("Lock Status");
                    if (lblAddressGridCell.Content == "Lock Status")
                    {
                        lblAddressGridCell.Width = 90;
                    }
                    stkpnlM3eByteAddressHex.Children.Add(lblAddressGridCell);
                }
                int byteCount = Convert.ToInt32(blockSizeByte);//here we will add the byte count of the tag.
                int blockCount = Convert.ToInt32(blockCountByte);
                for (int i = 0; i < blockCount; i++)//here we will add the Block count
                {
                    StackPanel stkPanel = new StackPanel();
                    stkPanel.Orientation = Orientation.Horizontal;
                    stkPanel.Name = "stkpnlMemoryAddress" + i.ToString();
                    stkPanel.VerticalAlignment = VerticalAlignment.Top;
                    stkPanel.HorizontalAlignment = HorizontalAlignment.Left;

                    // Create a name scope for the stackpanel. 
                    NameScope.SetNameScope(stkPanel, new NameScope());

                    // Create a new label to represent block Address eg: Block 1, Block 2 etc.
                    Label txtBlockAddress = new Label();
                    txtBlockAddress.Content = "Block " + i.ToString();
                    txtBlockAddress.Height = 30;
                    txtBlockAddress.Width = 80;
                    txtBlockAddress.VerticalAlignment = VerticalAlignment.Top;
                    txtBlockAddress.HorizontalAlignment = HorizontalAlignment.Left;
                    txtBlockAddress.Visibility = Visibility.Visible;
                    stkPanel.Children.Add(txtBlockAddress);

                    for (int j = 0; j < byteCount; j++)
                    {
                        isLastTagBlockAdded = true;
                        // Create Textbox to represent tag data.
                        TextBox txtCellAddress = new TextBox();
                        //txtCellAddress.IsReadOnly = false;
                        txtCellAddress.Name = "txtCellNumber" + i.ToString() + "_" + j.ToString();
                        txtCellAddress.Text = "00";
                        txtCellAddress.CaretBrush = Brushes.Black;
                        txtCellAddress.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#0A000000");
                        txtCellAddress.TextAlignment = TextAlignment.Center;
                        txtCellAddress.Height = 30;
                        txtCellAddress.Width = 65;
                        txtCellAddress.VerticalAlignment = VerticalAlignment.Top;
                        txtCellAddress.HorizontalAlignment = HorizontalAlignment.Left;
                        txtCellAddress.TextAlignment = TextAlignment.Center;
                        txtCellAddress.VerticalContentAlignment = VerticalAlignment.Center;
                        txtCellAddress.MaxLength = 2;
                        txtCellAddress.Visibility = Visibility.Visible;
                        if (lockStatus.Count == 0)
                        {
                            txtCellAddress.IsReadOnly = false;
                        }
                        else
                        {
                            txtCellAddress.IsReadOnly = ((lockStatus[i] == 0) ? false : true);
                        }
                        #region TextBox Command Binding
                        txtCellAddress.PreviewKeyDown += new KeyEventHandler(this.txtblk0ByteAddressClmn1_PreviewKeyDown);
                        txtCellAddress.PreviewTextInput += new TextCompositionEventHandler(this.txtblk0ByteAddressClmn1_PreviewTextInput);
                        #endregion
                        int cellNumber = Convert.ToInt32((i).ToString() + (j).ToString());
                        stkPanel.Children.Add(txtCellAddress);
                        stkPanel.RegisterName(txtCellAddress.Name, txtCellAddress);
                        //NameScope.SetNameScope(txtCellAddress.Name,txtCellAddress); enable when the Binding is done
                        //txtBlockCellList[cellNumber] = txtCellAddress;
                        txtHexList[count++] = txtCellAddress;
                        txtBlockCellList.Add(txtCellAddress);
                    }

                    //Image imgLockStatus = new Image();
                    //imgLockStatus.Height = 30;
                    //imgLockStatus.Width = 130;
                    //imgLockStatus.VerticalAlignment = VerticalAlignment.Center;
                    //imgLockStatus.HorizontalAlignment = HorizontalAlignment.Center;
                    ////imgLockStatus.Visibility = Visibility = Visibility.Visible;
                    ////imgLockStatus.
                    TextBox txtLockStatus = new TextBox();
                    txtLockStatus.IsReadOnly = false;
                    txtLockStatus.Name = "txtBlockLockStatus" + i.ToString();
                    if (lockStatus.Count == 0)
                    {
                        txtLockStatus.Text = "Unlocked";
                    }
                    else
                    {
                        txtLockStatus.Text = ((lockStatus[i] == 0) ? "Unlocked" : "Locked");
                    }
                    txtLockStatus.CaretBrush = Brushes.Black;
                    txtLockStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#0A000000");
                    txtLockStatus.TextAlignment = TextAlignment.Center;
                    txtLockStatus.Height = 30;
                    txtLockStatus.Width = 90;
                    txtLockStatus.IsReadOnly = true;
                    txtLockStatus.VerticalAlignment = VerticalAlignment.Top;
                    txtLockStatus.HorizontalAlignment = HorizontalAlignment.Left;
                    txtLockStatus.Visibility = Visibility.Visible;
                    txtLockStatus.VerticalContentAlignment = VerticalAlignment.Center;
                    #region Click Binding Lock Textbox;

                    #endregion
                    stkPanel.Children.Add(txtLockStatus);
                    stkpnlBlockAddressHex.Children.Add(stkPanel);
                    stkpnlBlockAddressHex.RegisterName(stkPanel.Name, stkPanel);

                }

                if (cacheData != null)
                {
                    int cellBlock = 0;
                    int lockFlag = 0;
                    byte[] orignalHexData = cacheData.ToArray();
                    if (orignalHexData.Length == data.Length)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (orignalHexData[i] == data[i])
                            {
                                txtBlockCellList[i].Foreground = Brushes.Black;
                            }
                            else
                            {
                                txtBlockCellList[i].Foreground = Brushes.Red;
                            }
                            if (lockStatus[lockFlag] == 0)
                            {
                                txtBlockCellList[i].Text = data[i].ToString("X2");
                            }
                            else
                            {
                                txtBlockCellList[i].Text = "";
                            }
                            cellBlock++;
                            if (cellBlock == blockSizeByte)
                            {
                                if (lockStatus.Count != (lockFlag + 1))
                                {
                                    if (cellBlock == blockSizeByte)
                                    {
                                        cellBlock = 0;
                                        lockFlag++;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < orignalHexData.Length; i++)
                        {
                            if (orignalHexData[i] == data[i])
                            {
                                txtBlockCellList[i].Foreground = Brushes.Black;
                            }
                            else
                            {
                                txtBlockCellList[i].Foreground = Brushes.Red;
                            }
                            if (lockStatus[lockFlag] == 0)
                            {
                                txtBlockCellList[i].Text = data[i].ToString("X2");
                            }
                            else
                            {
                                txtBlockCellList[i].Text = "";
                            }
                            cellBlock++;
                            if (cellBlock == blockSizeByte)
                            {
                                if (lockStatus.Count != (lockFlag + 1))
                                {
                                    if (cellBlock == blockSizeByte)
                                    {
                                        cellBlock = 0;
                                        lockFlag++;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    int cellBlock = 0;
                    int lockFlag = 0;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (lockStatus[lockFlag] == 0)
                        {
                            txtBlockCellList[i].Text = data[i].ToString("X2");
                        }
                        else
                        {
                            txtBlockCellList[i].Text = "";
                        }
                        cellBlock++;
                        if (cellBlock == blockSizeByte)
                        {
                            if (lockStatus.Count != (lockFlag + 1))
                            {
                                if (cellBlock == blockSizeByte)
                                {
                                    cellBlock = 0;
                                    lockFlag++;
                                }
                            }
                        }

                    }
                }

                // Generate word address mapping
                // word address = (byte address of cell 1, byte address of cell 2)
                // 0x0000 = (0x0000, 0x0001)
                uint temp = 0;
                WordAddressMapping.Clear();
                for (int i = 0; i < data.Length / 2; i++)
                {
                    WordAddressMapping.Add((uint)i, new uint[] { (uint)temp, (uint)++temp });
                    ++temp;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("No data could be read from a tag");
            }
        }



        private void WriteHexM3eData()
        {
            string failBlockCount = string.Empty;
            if (editBlockNumber == null)
            {
                throw new Exception("Trying to write invalid block data.");
            }
            for (int i = 0; i < editBlockNumber.Count; i++)
            {
                int blockNumber = editBlockNumber[i];
                string txtBlock = blockNumber.ToString();
                byte[] writeData = new byte[cacheBlockSize];
                for (int j = 0; j < cacheBlockSize; j++)
                {
                    StackPanel stkpanel = (StackPanel)stkpnlBlockAddressHex.FindName("stkpnlMemoryAddress" + txtBlock);//(TextBox)App.Current.MainWindow.FindName("txtCellNumber" + txtBlock + j.ToString());
                    TextBox txt = (TextBox)stkpanel.FindName("txtCellNumber" + txtBlock + "_" + j.ToString());
                    writeData[j] = Utilities.HexToByte(txt.Text.ToString());
                }

                if (TagMemoryArchitecture.WriteTagMemory((uint)(blockNumber), writeData, currentEPC, objReader))
                {
                    for (int j = 0; j < cacheBlockSize; j++)
                    {
                        StackPanel stkpanel = (StackPanel)stkpnlBlockAddressHex.FindName("stkpnlMemoryAddress" + txtBlock);
                        TextBox txt = (TextBox)stkpanel.FindName("txtCellNumber" + txtBlock + "_" + j.ToString());
                        txt.Foreground = Brushes.Black;
                    }
                }
                else
                {

                    if (failBlockCount == string.Empty)
                    {
                        failBlockCount += txtBlock;
                    }
                    else
                    {
                        failBlockCount += "," + txtBlock;
                    }

                    {
                        byte[] data = TagMemoryArchitecture.ReadTagMemory((uint)blockNumber, currentEPC, objReader);
                        for (int j = 0; j < cacheBlockSize; j++)
                        {
                            StackPanel stkpanel = (StackPanel)stkpnlBlockAddressHex.FindName("stkpnlMemoryAddress" + txtBlock);
                            TextBox txt = (TextBox)stkpanel.FindName("txtCellNumber" + txtBlock + "_" + j.ToString());
                            
                            txt.Text = data[j].ToString("X2");
                        }
                    }
                }
            }
            if (failBlockCount != string.Empty)
            {
                if (failBlockCount.Contains(","))
                {
                    List<int> index = new List<int>();
                    index.AddRange(failBlockCount.Split(',').Select(Int32.Parse).ToList());
                    EditASCIIOrignalCache(index);
                }
                else
                {
                    int index = Convert.ToInt32(failBlockCount);
                    EditASCIIOrignalCache(new List<int>() { index });
                }
                throw new Exception("Write memory failed for block " + failBlockCount);
            }
            else
            {
                btnM3eWriteASCII.IsEnabled = false;
                btnM3eWriteHex.IsEnabled = false;
                txtM3eDataASCII.Foreground = Brushes.Black;
                if (txtHexList != null)
                {
                    string hexData = string.Empty;
                    for (int i = 0; i < txtHexList.Length; i++)
                    {
                        if (txtHexList[i].Text != string.Empty)
                        {
                            if (txtHexList[i].Text.Length == 1)
                            {
                                txtHexList[i].Text = "0" + txtHexList[i].Text;
                            }
                            else
                            {
                                hexData += txtHexList[i].Text;
                            }
                        }
                        else
                        {
                            hexData += "00";
                        }
                    }
                    editedM3eAsciiString = (Utilities.HexStringToAsciiString(hexData));
                }
                originalM3eAsciiString = editedM3eAsciiString;
                MessageBox.Show("Write memory block successful.", "Universal reader assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region SubArray
        /// <summary>
        /// Extract subarray
        /// </summary>
        /// <param name="src">Source array</param>
        /// <param name="offset">Start index in source array</param>
        /// <param name="length">Number of source elements to extract</param>
        /// <returns>New array containing specified slice of source array</returns>
        private static byte[] SubArray(byte[] src, int offset, int length)
        {
            return SubArray(src, ref offset, length);
        }

        /// <summary>
        /// Extract subarray, automatically incrementing source offset
        /// </summary>
        /// <param name="src">Source array</param>
        /// <param name="offset">Start index in source array.  Automatically increments value by copied length.</param>
        /// <param name="length">Number of source elements to extract</param>
        /// <returns>New array containing specified slice of source array</returns>
        private static byte[] SubArray(byte[] src, ref int offset, int length)
        {
            byte[] dst = new byte[length];
            try
            {
                Array.Copy(src, offset, dst, 0, length);
                offset += length;
            }
            catch
            {
            }
            return dst;
        }

        #endregion

        #region Gen2 Section
        /// <summary>
        /// Populate the User memory editors with user data
        /// </summary>
        private void PopulateUserData()
        {
            try
            {
                ReadTagMemory readUserMemory = new ReadTagMemory(objReader, model);
                // Disable write buttons in ascii and hex representation grpbx
                btnWriteAscii.IsEnabled = false;
                btnWrite.IsEnabled = false;

                // Clear the cached previous ascii data
                editedAsciiNewString = string.Empty;
                originalAsciiNewString = string.Empty;

                lblErrorAddAsciiEdtr.Content = "";
                lblErrorAddHexEdtr.Content = "";

                if ((bool)rbFirstTagUserMemTb.IsChecked)
                {
                    antenna = GetSelectedAntennaList()[0];
                }

                objReader.ParamSet("/reader/tagop/antenna", antenna);
                TagFilter searchSelect = null;

                if ((bool)rbSelectedTagUserMemTb.IsChecked)
                {
                    if (lblSelectFilter.Content.ToString().Contains("EPC ID"))
                    {
                        searchSelect = new TagData(currentEPC);
                    }
                    else
                    {
                        int dataLength = 0;
                        byte[] SearchSelectData = ByteFormat.FromHex(txtData.Text);
                        if (null != SearchSelectData)
                        {
                            dataLength = SearchSelectData.Length;
                        }

                        searchSelect = new Gen2.Select(false, selectMemBank, Convert.ToUInt16(startAddress * 16), Convert.ToUInt16(dataLength * 8), SearchSelectData);
                    }
                }
                else
                {
                    searchSelect = new TagData(currentEPC);
                }

                //Read USER bank data
                ushort[] userMemData = null;
                readUserMemory.ReadTagMemoryData(Gen2.Bank.USER, searchSelect, ref userMemData);
                byte[] userByteData = null;
                if (null != userMemData)
                {
                    userByteData = ByteConv.ConvertFromUshortArray(userMemData);
                    string asciiData = Utilities.HexStringToAsciiString(ByteFormat.ToHex(userByteData, "", ""));

                    // Cache the original ascii string received from the module for further modifications
                    originalAsciiNewString = asciiData;
                    editedAsciiNewString = asciiData;

                    string replacedAsciiData = ReplaceSpecialCharInAsciiData(asciiData.ToCharArray());
                    txtASCIIData.Text = replacedAsciiData;

                    // Max characters available to edit in ascii editior
                    txtblkSpMAXSizeCharsAvailable.Text = userByteData.Length.ToString();
                    AsciiDataChanged();
                    txtASCIIData.ScrollToVerticalOffset(0);

                    // Max bytes available to edit in hex editior
                    txtblkSpMAXSizeHexByteSp64BytesAvailable.Text = userByteData.Length.ToString();
                    txtASCIIData.IsEnabled = true;
                    txtASCIIData.Foreground = Brushes.Black;
                    txtASCIIData.MaxLength = userByteData.Length;
                    UpdateHexUserMemoryRepresentation(userByteData);
                }
                lblErrorAddAsciiEdtr.Content = "";
                lblErrorAddHexEdtr.Content = "";
            }
            catch (Exception ex)
            {
                if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                {
                    lblErrorAddAsciiEdtr.Content = "Error: Read Error";
                    lblErrorAddHexEdtr.Content = "Error: Read Error";
                }
                else
                {
                    lblErrorAddAsciiEdtr.Content = "Error: " + ex.Message;
                    lblErrorAddHexEdtr.Content = "Error: " + ex.Message;
                }

                lblUserMemoryError.Content = "";
                txtblkSpMAXSizeHexByteSp64BytesAvailable.Text = "00";
                txtblkSpMAXSizeCharsAvailable.Text = "00";
                txtblkRemainingCharsAvailableCount.Text = "00 characters remaining";
                txtASCIIData.IsEnabled = false;
                RemoveGeneratedControl();
            }
            finally
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }
        #endregion
        /// <summary>
        /// Append at the end of ascii string with zero's if the length of the edited ascii string is less then the original ascii data received. 
        /// If length is same then return the edited ascii string.
        /// </summary>
        /// <param name="ascii"></param>
        /// <returns></returns>
        private string EditAsciiString(string ascii)
        {
            try
            {
                char[] editedAsciiString = ascii.ToCharArray();
                char[] originalAsciiString = (model.Equals("M3e")) ? originalM3eAsciiString.ToCharArray() : originalAsciiNewString.ToCharArray();
                string asciiEditedData = string.Empty;
                if (editedAsciiString.Length != originalAsciiString.Length)
                {
                    for (int charIndex = 0; charIndex < originalAsciiString.Length; charIndex++)
                    {
                        if (charIndex < editedAsciiString.Length)
                        {
                            asciiEditedData += editedAsciiString[charIndex];
                        }
                        else
                        {
                            asciiEditedData += (Char)0;
                        }
                    }
                    return asciiEditedData;
                }
                else
                {
                    return ascii;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Replace all the special characters in the ascii string with .[DOT]
        /// </summary>
        /// <param name="asciiData"></param>
        /// <returns></returns>
        private string ReplaceSpecialCharInAsciiData(char[] asciiData)
        {
            string replacedData = string.Empty;
            foreach (char character in asciiData)
            {
                if (char.IsControl(character))
                {
                    replacedData += ".";
                }
                else
                {
                    replacedData += character;
                }
            }
            return replacedData;
        }

        Dictionary<string, string> CacheUserMem = new Dictionary<string, string>();
        // Word address mapping
        // Ex: 0x0000 = 2 byte cells i.e 0x0000 = 0x0000 and 0x0001 byte cell address
        Dictionary<uint, uint[]> WordAddressMapping = new Dictionary<uint, uint[]>();
        // Cache edited byte cell location 
        List<uint> editedDataCellLocation = new List<uint>();

        /// <summary>
        /// Update hex editor with the new user mem data
        /// </summary>
        /// <param name="userByteData"></param>
        private void UpdateHexUserMemoryRepresentation(byte[] userByteData)
        {
            // Clear all the previous generated controls in hex editor
            RemoveGeneratedControl();
            // Generate new textbox controls in hex editor
            GenerateControls(userByteData);
            CacheUserMem.Clear();
            editedDataCellLocation.Clear();
            WordAddressMapping.Clear();
            // Update the user mem data in the hex editor
            for (int count = 0; count < userByteData.Length; count++)
            {
                txtbxHexRepList[count].Text = userByteData[count].ToString("X2");
                txtbxHexRepList[count].Foreground = Brushes.Black;
                txtbxHexRepList[count].Visibility = System.Windows.Visibility.Visible;
                CacheUserMem.Add(txtbxHexRepList[count].Name.ToString(), txtbxHexRepList[count].Text);
            }

            // Generate word address mapping
            // word address = (byte address of cell 1, byte address of cell 2)
            // 0x0000 = (0x0000, 0x0001)
            uint temp = 0;
            for (int count = 0; count < userByteData.Length / 2; count++)
            {
                WordAddressMapping.Add((uint)count, new uint[] { (uint)temp, (uint)++temp });
                ++temp;
            }
        }

        /// <summary>
        /// Get selected antenna list
        /// </summary>
        /// <returns></returns>
        private List<int> GetSelectedAntennaList()
        {
            Window mainWindow = App.Current.MainWindow;
            CheckBox Ant1CheckBox = (CheckBox)mainWindow.FindName("Ant1CheckBox");
            CheckBox Ant2CheckBox = (CheckBox)mainWindow.FindName("Ant2CheckBox");
            CheckBox Ant3CheckBox = (CheckBox)mainWindow.FindName("Ant3CheckBox");
            CheckBox Ant4CheckBox = (CheckBox)mainWindow.FindName("Ant4CheckBox");
            CheckBox[] antennaBoxes = { Ant1CheckBox, Ant2CheckBox, Ant3CheckBox, Ant4CheckBox };
            List<int> ant = new List<int>();

            for (int antIdx = 0; antIdx < antennaBoxes.Length; antIdx++)
            {
                CheckBox antBox = antennaBoxes[antIdx];

                if ((bool)antBox.IsChecked)
                {
                    int antNum = antIdx + 1;
                    ant.Add(antNum);
                }
            }
            if (ant.Count > 0)
                return ant;
            else
                return null;
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);

                if (model.Equals("M3e"))
                {
                    if ((bool)rbHexRep.IsChecked)
                    {
                        WriteHexM3eData();
                    }
                    if ((bool)rbASCII.IsChecked)
                    {
                        WriteASCIIM3eData();
                    }
                }
                else
                {
                    if ((bool)rbFirstTagUserMemTb.IsChecked)
                    {
                        antenna = ((null != GetSelectedAntennaList()) ? (GetSelectedAntennaList()[0]) : antenna);
                    }

                    objReader.ParamSet("/reader/tagop/antenna", antenna);

                    if ((bool)rbSelectedTagUserMemTb.IsChecked)
                    {
                        TagFilter searchSelect = null;


                        if (selectMemBank == Gen2.Bank.EPC)
                        {
                            searchSelect = new TagData(txtEpc.Text);
                        }
                        else
                        {
                            byte[] SearchSelectData = ByteFormat.FromHex(txtData.Text);
                            if (null == SearchSelectData)
                            {
                                dataLength = 0;
                            }
                            else
                            {
                                dataLength = SearchSelectData.Length;
                            }

                            searchSelect = new Gen2.Select(false, selectMemBank, Convert.ToUInt16(startAddress * 16), Convert.ToUInt16(dataLength * 8), SearchSelectData);
                        }
                        WriteUserMem(searchSelect);
                    }
                    else
                    {
                        WriteUserMem(null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                editBlockNumber = null;
                btnM3eWriteHex.IsEnabled = false;
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        /// <summary>
        /// Write the data to user memoru
        /// </summary>
        /// <param name="filter"> data to be written on the specified tag which is used as filter</param>
        private void WriteUserMem(TagFilter filter)
        {
            string userMem = string.Empty;
            try
            {
                objReader.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);
                if ((bool)rbHexRep.IsChecked)
                {
                    for (int count = 0; count < txtbxHexRepList.Length; count++)
                    {
                        if (txtbxHexRepList[count].Text != string.Empty)
                        {
                            userMem += txtbxHexRepList[count].Text;
                        }
                        else
                        {
                            userMem += "00";
                        }
                    }
                    ExecuteUsermemoryTagOp(ByteConv.ToU16s(ByteFormat.FromHex(userMem)), filter);

                    UpdateHexUserMemoryRepresentation(ByteFormat.FromHex(userMem));
                    txtASCIIData.IsEnabled = true;
                    originalAsciiNewString = Utilities.HexStringToAsciiString(userMem);
                    btnWrite.IsEnabled = false;
                    btnWriteAscii.IsEnabled = false;
                }
                else if ((bool)rbASCII.IsChecked)
                {
                    if (txtASCIIData.Text.Length > 0)
                    {
                        string editedAsciiData = EditAsciiString(editedAsciiNewString);
                        UpdateHexEditor(editedAsciiData);
                        ExecuteUsermemoryTagOp(ByteConv.ToU16s(ByteFormat.FromHex(Utilities.AsciiStringToHexString(editedAsciiData))), filter);

                        txtASCIIData.Foreground = Brushes.Black;
                        originalAsciiNewString = editedAsciiData;
                        btnWrite.IsEnabled = false;
                        btnWriteAscii.IsEnabled = false;
                    }
                    else
                    {
                        return;
                    }
                }
                MessageBox.Show("Write user memory is successful", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reads the data edit in the ASCII format to be written on the tag.
        /// </summary>
        private void WriteASCIIM3eData()
        {
            if (txtM3eDataASCII.Text.Length > 0)
            {
                string editedASCIIData = EditAsciiString(editedM3eAsciiString);
                UpdateHexEditor(editedASCIIData);
                ExecuteUsermemoryTagOp((ByteFormat.FromHex(Utilities.AsciiStringToHexString(editedASCIIData))));
            }
        }

        /// <summary>
        /// Write memeory to M3e tags.
        /// </summary>
        /// <param name="data">Edited data</param>
        private void ExecuteUsermemoryTagOp(byte[] data)
        {
            List<int> editedBlockCount = new List<int>();
            string failBlockCount = string.Empty;
            uint userMemStartAddress = (uint)startAddress;
            uint[] wrdAddressList = null;
            wrdAddressList = GetWordAddressList();
            int editedDataCellLocLength = 0;
            while (editedDataCellLocLength < wrdAddressList.Length)
            {
                userMemStartAddress = ((wrdAddressList[editedDataCellLocLength]) * 2);

                int byteNumber = 0;
                int wordMapAddress = unchecked((int)userMemStartAddress);//this data is neg then raise exception.
                int blockNumber = Math.DivRem(wordMapAddress, (int)cacheBlockSize, out byteNumber);

                if (!(editedBlockCount.Contains(blockNumber)))
                {
                    editedBlockCount.Add(blockNumber);
                }
                editedDataCellLocLength++;
            }

            if (editedBlockCount == null)
            {
                throw new Exception("Trying to write invalid block data.");
            }

            foreach (int item in editedBlockCount)
            {
                if (cacheLockStatus[item] == 0)
                {
                    byte[] writeData = new byte[cacheBlockSize];
                    for (int j = 0; j < writeData.Length; j++)
                    {
                        StackPanel stkpanel = (StackPanel)stkpnlBlockAddressHex.FindName("stkpnlMemoryAddress" + item);
                        TextBox txt = (TextBox)stkpanel.FindName("txtCellNumber" + item + "_" + j.ToString());
                        if (txt.Text != "")
                        {
                            writeData[j] = Utilities.HexToByte(txt.Text.ToString());
                        }
                        else
                        {
                            continue;
                        }

                    }

                    if (TagMemoryArchitecture.WriteTagMemory((uint)item, writeData, currentEPC, objReader))
                    {
                        for (int j = 0; j < cacheBlockSize; j++)
                        {
                            StackPanel stkpanel = (StackPanel)stkpnlBlockAddressHex.FindName("stkpnlMemoryAddress" + item);
                            TextBox txt = (TextBox)stkpanel.FindName("txtCellNumber" + item + "_" + j.ToString());
                            txt.Foreground = Brushes.Black;
                        }
                    }
                    else
                    {

                        if (failBlockCount == string.Empty)
                        {
                            failBlockCount += item.ToString();
                        }
                        else
                        {
                            failBlockCount += "," + item.ToString();
                        }
                    }
                }
            }

            if (failBlockCount != string.Empty)
            {
                if (failBlockCount.Contains(","))
                {
                    List<int> index = new List<int>();
                    index.AddRange(failBlockCount.Split(',').Select(Int32.Parse).ToList());
                    EditASCIIOrignalCache(index);
                }
                else
                {
                    int index = Convert.ToInt32(failBlockCount);
                    EditASCIIOrignalCache(new List<int>() { index });
                }
                throw new Exception("Write Memory failed for block " + failBlockCount);
            }
            else
            {
                btnM3eWriteASCII.IsEnabled = false;
                btnM3eWriteHex.IsEnabled = false;
                txtM3eDataASCII.Foreground = Brushes.Black;
                originalM3eAsciiString = editedM3eAsciiString;
                MessageBox.Show("Write memory block successful.", "Universal reader assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        /// <summary>
        /// Edit the Orignal ASCII data for the successfull written data.
        /// </summary>
        /// <param name="index">Failed byte address</param>
        private void EditASCIIOrignalCache(List<int> index)
        {
            string editOrignalString = string.Empty;
            for (int i = 0; i < editedM3eAsciiString.Length; i++)
            {
                if (index.Contains(i))
                {
                    editOrignalString += originalM3eAsciiString[i];
                }
                else
                {
                    editOrignalString += editedM3eAsciiString[i];
                }
            }
            originalM3eAsciiString = editOrignalString;
        }

        /// <summary>
        /// Write data to user memory
        /// </summary>
        /// <param name="userMemData"></param>
        /// <param name="filter"></param>
        private void ExecuteUsermemoryTagOp(ushort[] userMemData, TagFilter filter)
        {
            uint userMemStartAddress = (uint)startAddress;
            uint[] wrdAddressList = null;
            wrdAddressList = GetWordAddressList();
            int editedDataCellLocLength = 0;
            while (editedDataCellLocLength < wrdAddressList.Length)
            {
                userMemStartAddress = wrdAddressList[editedDataCellLocLength];
                ushort[] dataToBeWritten = new ushort[1];
                Array.Copy(userMemData, userMemStartAddress, dataToBeWritten, 0, 1);
                objReader.ExecuteTagOp(new Gen2.WriteData(Gen2.Bank.USER, userMemStartAddress, dataToBeWritten), filter);
                editedDataCellLocLength++;
            }
        }

        /// <summary>
        /// Get word address for the specified byte cell
        /// </summary>
        /// <param name="cellLocation">Cell location</param>
        /// <returns>Word address of the specifeid byte cell</returns>
        private uint GetWordAddress(uint cellLocation)
        {
            uint edtdCellLocWordAddress = 0;
            uint index = 0;
            // loop through the Word address mapping
            while (index < WordAddressMapping.Count)
            {
                uint[] cellLoc = WordAddressMapping[index];
                bool isWordAddressFound = false;
                for (int locIndex = 0; locIndex < cellLoc.Length; locIndex++)
                {
                    // If the specified location is present in the map, copy the index 
                    // which will be the word address
                    if (cellLocation == cellLoc[locIndex])
                    {
                        edtdCellLocWordAddress = index;
                        isWordAddressFound = true;
                        break;
                    }
                }
                if (isWordAddressFound)
                {
                    break;
                }
                index++;
            }
            return edtdCellLocWordAddress;
        }

        /// <summary>
        /// Gets word address list of edited data
        /// </summary>
        /// <returns>unique array of word address</returns>
        private uint[] GetWordAddressList()
        {
            List<uint> wordAddressList = new List<uint>();
            foreach (uint wordadd in editedDataCellLocation)
            {
                uint tempAdd = GetWordAddress(wordadd);
                // Don't add the address if already present in the list. 
                // To avoid writing data on the same location twice
                if (!wordAddressList.Contains(tempAdd))
                {
                    wordAddressList.Add(tempAdd);
                }
            }
            return wordAddressList.ToArray();
        }

        string currentEpcRep = "Hex";
        private void rbHexRep_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                gbHexRep.Focus();
                try
                {
                    if (currentEPC.Length > 0)
                    {
                        if (currentEpcRep != "Hex")
                        {
                            if (model.Equals("M3e"))
                            {
                                if (cacheData != null)
                                {
                                    RemoveGeneratedControlM3e();
                                    GenerateControls(cacheData.ToArray(), (byte)cacheBlockSize, (byte)cacheBlockCount, cacheLockStatus.ToList());
                                    if (currentEpcRep == "Ascii")
                                    {
                                        string editedAsciiData = EditAsciiString(editedM3eAsciiString);
                                        UpdateHexEditor(editedAsciiData);
                                    }
                                }
                            }
                            else
                            {
                                if (currentEpcRep == "Ascii")
                                {
                                    // Get the ascii data from ascii text editor
                                    string editedAsciiData = EditAsciiString(editedAsciiNewString);
                                    UpdateHexEditor(editedAsciiData);
                                }
                            }
                        }
                        currentEpcRep = "Hex";
                    }
                }
                catch (Exception ex)
                {
                    rbHexRep.IsChecked = true;
                    MessageBox.Show(ex.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Update hex editor with the edited data, when ascii editor is edited
        /// </summary>
        /// <param name="editedAsciiData"> edited ascii string</param>
        private void UpdateHexEditor(string editedAsciiData)
        {
            if (model.Equals("M3e"))
            {
                byte[] orignalBytes = ByteFormat.FromHex(Utilities.AsciiStringToHexString(originalM3eAsciiString));
                //Updating Hex UI Grid
                RemoveGeneratedControlM3e();
                GenerateControls((ByteFormat.FromHex(Utilities.AsciiStringToHexString(editedAsciiData))), (byte)cacheBlockSize, (byte)cacheBlockCount, cacheLockStatus.ToList());
                for (int i = 0; i < txtHexList.Length; i++)
                {
                    if (i < orignalBytes.Length)
                    {
                        if (txtHexList[i].Text != orignalBytes[i].ToString("X2"))
                        {
                            if (!(editedDataCellLocation.Contains((uint)i)))
                            {
                                editedDataCellLocation.Add((uint)i);
                            }
                            txtHexList[i].Foreground = Brushes.Red;
                        }
                        else
                        {
                            txtHexList[i].Foreground = Brushes.Black;
                        }
                    }
                    else
                    {
                        txtHexList[i].Foreground = Brushes.Black;
                    }
                }
            }
            else
            {
                byte[] orignalBytes = ByteConv.ConvertFromUshortArray(ByteConv.ToU16s(ByteFormat.FromHex(Utilities.AsciiStringToHexString(originalAsciiNewString))));



                // Update the data to hex editor
                UpdateHexUserMemoryRepresentation(ByteConv.ConvertFromUshortArray(ByteConv.ToU16s(ByteFormat.FromHex(Utilities.AsciiStringToHexString(editedAsciiData)))));
                for (int index = 0; index < txtbxHexRepList.Length; index++)
                {
                    if (txtbxHexRepList[index].Text != orignalBytes[index].ToString("X2"))
                    {
                        txtbxHexRepList[index].Foreground = Brushes.Red;
                        editedDataCellLocation.Add((uint)index);
                    }
                    else
                    {
                        txtbxHexRepList[index].Foreground = Brushes.Black;
                    }
                }
            }
        }

        private void rbASCII_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                try
                {
                    if (currentEPC.Length > 0)
                    {
                        if (currentEpcRep == "Hex")
                        {
                            string hexData = string.Empty;
                            if (model.Equals("M3e"))
                            {
                                if (cacheData != null)
                                {
                                    if (txtHexList != null)
                                    {
                                        for (int i = 0; i < txtHexList.Length; i++)
                                        {
                                            if (txtHexList[i].Text != string.Empty)
                                            {
                                                hexData += txtHexList[i].Text;
                                            }
                                            else
                                            {
                                                hexData += "00";
                                            }
                                        }
                                        string asciiData = (Utilities.HexStringToAsciiString(hexData));
                                        string replacedAsciiData = ReplaceSpecialCharInAsciiData(asciiData.ToCharArray());
                                        if (hexData.Length > 0)
                                        {
                                            if (Convert.ToBoolean(string.Compare(asciiData, originalM3eAsciiString)))
                                            {
                                                txtM3eDataASCII.Foreground = Brushes.Red;
                                            }
                                            else
                                            {
                                                txtM3eDataASCII.Foreground = Brushes.Black;
                                            }
                                        }
                                        txtM3eDataASCII.Text = replacedAsciiData;
                                        editedM3eAsciiString = asciiData;
                                    }
                                    else
                                    {
                                        GenerateASCIIControls(cacheData.ToArray());
                                    }
                                }
                            }
                            else
                            {

                                // Get all the hex data from hex editor
                                if (null != txtbxHexRepList)
                                {
                                    for (int count = 0; count < txtbxHexRepList.Length; count++)
                                    {
                                        if (txtbxHexRepList[count].Text != string.Empty)
                                        {
                                            hexData += txtbxHexRepList[count].Text;
                                        }
                                        else
                                        {
                                            hexData += "00";
                                        }
                                    }
                                    string asciiData = Utilities.HexStringToAsciiString(hexData);

                                    string replacedAsciiData = ReplaceSpecialCharInAsciiData(asciiData.ToCharArray());

                                    // Change the text colour of ascii text editor if the data has been edited in hex editor
                                    if (hexData.Length > 0)
                                    {
                                        if (Convert.ToBoolean(string.Compare(asciiData, originalAsciiNewString)))
                                        {
                                            txtASCIIData.Foreground = Brushes.Red;
                                        }
                                        else
                                        {
                                            txtASCIIData.Foreground = Brushes.Black;
                                        }
                                    }
                                    txtASCIIData.Text = replacedAsciiData;
                                    editedAsciiNewString = asciiData;
                                }
                            }
                        }
                        currentEpcRep = "Ascii";
                    }
                }
                catch (Exception ex)
                {
                    if (currentEpcRep == "Hex")
                        rbHexRep.IsChecked = true;
                    MessageBox.Show(ex.Message.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void txtM3eDataASCII_TextChanged(object sender, TextChangedEventArgs e)
        {
            AsciiDataChanged();
            if (txtM3eDataASCII.Text.Length == 0)
            {
                // Clear the string when ascii text editor is empty totally 
                editedM3eAsciiString = string.Empty;
            }
        }

        private void txtASCIIData_TextChanged(object sender, TextChangedEventArgs e)
        {
            AsciiDataChanged();
            if (txtASCIIData.Text.Length == 0)
            {
                // Clear the string when ascii text editor is empty totally                
                editedAsciiNewString = string.Empty;
            }
        }

        /// <summary>
        /// Calculate the remaining characters available to enter in ascii editor 
        /// </summary>
        private void AsciiDataChanged()
        {
            if (null != objReader)
            {
                if (model.Equals("M3e"))
                {
                    if (txtblkTotalCount.Text != "00")
                    {
                        txtblkCharAvailable.Text = ((Convert.ToInt64(txtblkTotalCount.Text) - (txtM3eDataASCII.Text.Length)).ToString() + " characters remaining");
                    }
                }
                else
                {
                    if (txtblkSpMAXSizeCharsAvailable.Text != "00")
                    {
                        txtblkRemainingCharsAvailableCount.Text = (Convert.ToInt64(txtblkSpMAXSizeCharsAvailable.Text) - txtASCIIData.Text.Length).ToString() + " characters remaining";
                    }
                }
            }
        }

        private void txtM3eDataASCII_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            btnM3eWriteASCII.IsEnabled = true;
            btnM3eWriteHex.IsEnabled = true;
            txtM3eDataASCII.Foreground = Brushes.Red;
            if ((Convert.ToInt64(txtblkTotalCount.Text) - (txtM3eDataASCII.Text.Length) == 0) && (txtM3eDataASCII.SelectionLength == 0))
            {
                throw new Exception("Invalid input");
            }
            else
            {
                if (txtM3eDataASCII.SelectionLength == txtM3eDataASCII.Text.Length)
                {
                    editedM3eAsciiString = e.Text;
                }
                else
                {
                    if (txtM3eDataASCII.SelectionLength == 0)
                    {
                        // Inseret the text at the selected position
                        editedM3eAsciiString = editedM3eAsciiString.Insert(txtM3eDataASCII.SelectionStart, e.Text);
                    }
                    else
                    {
                        editedM3eAsciiString = editedM3eAsciiString.Remove(txtM3eDataASCII.SelectionStart, txtM3eDataASCII.SelectionLength);
                        editedM3eAsciiString = editedM3eAsciiString.Insert(txtM3eDataASCII.SelectionStart, e.Text);
                    }
                }
            }
        }

        private void txtASCIIData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            btnWriteAscii.IsEnabled = true;
            txtASCIIData.Foreground = Brushes.Red;
            btnWrite.IsEnabled = true;

            if (((Convert.ToInt64(txtblkSpMAXSizeCharsAvailable.Text) - txtASCIIData.Text.Length) == 0) && (txtASCIIData.SelectionLength == 0))
            {
                return;
            }
            else
            {
                if (txtASCIIData.SelectionLength == txtASCIIData.Text.Length)
                {
                    editedAsciiNewString = e.Text;
                }
                else
                {
                    if (txtASCIIData.SelectionLength == 0)
                    {
                        // Inseret the text at the selected position
                        editedAsciiNewString = editedAsciiNewString.Insert(txtASCIIData.SelectionStart, e.Text);
                    }
                    else
                    {
                        editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength);
                        editedAsciiNewString = editedAsciiNewString.Insert(txtASCIIData.SelectionStart, e.Text);
                    }
                }
            }
        }

        public void txtblk0ByteAddressClmn1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            if ((e.Key == Key.Back) || (e.Key == Key.Delete))
            {
                if (model.Equals("M3e"))
                {
                    if (editBlockNumber == null)
                    {
                        btnM3eWriteASCII.IsEnabled = false;
                        btnM3eWriteHex.IsEnabled = false;
                    }
                    else
                    {
                        btnM3eWriteASCII.IsEnabled = true;
                        btnM3eWriteHex.IsEnabled = true;
                    }
                    txt.Foreground = Brushes.Red;
                }
                else
                {
                    btnWrite.IsEnabled = true;
                    btnWriteAscii.IsEnabled = true;
                    txt.Foreground = Brushes.Red;
                }
            }
        }

        private void txtM3eDataASCII_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Back) || (e.Key == Key.Delete))
            {
                btnM3eWriteASCII.IsEnabled = true;
                btnM3eWriteHex.IsEnabled = true;
                txtM3eDataASCII.Foreground = Brushes.Red;
                if ((txtM3eDataASCII.SelectionLength > 0) && (txtM3eDataASCII.SelectionLength != txtM3eDataASCII.MaxLength))
                {
                    // if more then one character is selected to delete
                    editedM3eAsciiString = editedM3eAsciiString.Remove(txtM3eDataASCII.SelectionStart, txtM3eDataASCII.SelectionLength);
                }
                else if (txtM3eDataASCII.SelectionLength == 0)
                {
                    // if more then one character is selected to delete
                    editedM3eAsciiString = editedM3eAsciiString.Remove((txtM3eDataASCII.SelectionStart - 1), 1);
                }
            }
            else if ((e.Key == Key.Space) && (txtM3eDataASCII.MaxLength != txtM3eDataASCII.Text.Length))
            {
                editedM3eAsciiString = editedM3eAsciiString.Insert(txtM3eDataASCII.SelectionStart, " ");
            }
        }

        private void txtASCIIData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Back) || (e.Key == Key.Delete))
            {
                btnWriteAscii.IsEnabled = true;
                btnWrite.IsEnabled = true;
                txtASCIIData.Foreground = Brushes.Red;
                if ((txtASCIIData.SelectionLength > 0) && (txtASCIIData.SelectionLength != txtASCIIData.MaxLength))
                {
                    if (txtASCIIData.SelectionStart == 0)
                    {
                        // if more then one character is selected to delete
                        editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength);
                    }
                    else
                    {
                        // if more then one character is selected to delete
                        editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength);
                    }
                }
                else if (txtASCIIData.SelectionLength == 0)
                {
                    // If only 1 character is deleted
                    if (txtASCIIData.SelectionStart > 0)
                    {
                        editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart - 1, 1);
                    }
                }
            }
            else if ((e.Key == Key.Space) && (txtASCIIData.MaxLength != txtASCIIData.Text.Length))
            {
                editedAsciiNewString = editedAsciiNewString.Insert(txtASCIIData.SelectionStart, " ");
            }
        }

        /// <summary>
        /// Removes the dynamically created textbox and stack panel controls in parent stack panel of Hex representation
        /// </summary>
        public void RemoveGeneratedControl()
        {
            try
            {
                // Clear the cached textbox controls
                txtbxHexRepList = null;

                StackPanel stkpnlByteAdd = (StackPanel)this.FindName("stkpnlByteAddress");
                // Loop through the parent stackpanel and get its child elements
                foreach (StackPanel stkpnlChild in stkpnlByteAdd.Children)
                {
                    // Removes all the elements present in the each child stackpanel element
                    stkpnlChild.Children.Clear();

                    // Unregister the control so that this exception can be resolved "Cannot register duplicate Name 'stkPnl' in this scope."
                    // Because textbox controls are created dynamically for each read.
                    stkpnlByteAdd.UnregisterName(stkpnlChild.Name.ToString());
                }

                StackPanel stkpnlHexClmnAddRep = (StackPanel)this.FindName("stkpnlHexClmnAddress");
                stkpnlHexClmnAddRep.Children.Clear();

                // Removes all the child stack panels in the parent stack panel
                stkpnlByteAdd.Children.Clear();
                stkpnlByteAdd.InvalidateVisual();
            }
            catch { }
        }

        /// <summary>
        /// Generate textboxes for hex representation dynamically based on the user data length
        /// </summary>
        /// <param name="data"> Maximum user memory data received from the module</param>
        private void GenerateControls(byte[] data)
        {
            bool islastHexBytesAddedToHexEdtr = false;

            int datalength = data.Length;

            // Create a new label to represent next address eg: 0x0001, 0x0002 etc
            Label lblHexAddressRepEmptySpace = new Label();
            lblHexAddressRepEmptySpace.Height = 30;
            lblHexAddressRepEmptySpace.Width = 57;
            lblHexAddressRepEmptySpace.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            lblHexAddressRepEmptySpace.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            lblHexAddressRepEmptySpace.Content = "";
            stkpnlHexClmnAddress.Children.Add(lblHexAddressRepEmptySpace);
            // Create a name scope for the hex address stackpanel. 
            NameScope.SetNameScope(stkpnlHexClmnAddress, new NameScope());
            for (int i = 0; i < 16; i++)
            {
                // Create a new label to represent next address eg: 0x0001, 0x0002 etc
                Label lblHexAddressRep = new Label();
                lblHexAddressRep.Height = 30;
                lblHexAddressRep.Width = 57;
                lblHexAddressRep.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                lblHexAddressRep.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                lblHexAddressRep.Content = "0x" + (i).ToString("x4"); ;
                stkpnlHexClmnAddress.Children.Add(lblHexAddressRep);
            }

            StackPanel stkPannel = new StackPanel();
            stkPannel.Orientation = Orientation.Horizontal;
            stkPannel.Name = "stkpnlHexAddress0";
            stkPannel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            stkPannel.VerticalAlignment = System.Windows.VerticalAlignment.Top;

            // Cache textboxes which is to be used for editing and writing back to the tag
            txtbxHexRepList = new TextBox[data.Length];

            // Create a name scope for the stackpanel. 
            NameScope.SetNameScope(stkPannel, new NameScope());

            // Create a label Hex word address eg: 0x0000 and add it to the stack panel
            Label lblHexAddress = new Label();
            lblHexAddress.Content = "0x0000";
            lblHexAddress.Height = 30;
            lblHexAddress.Width = 57;
            lblHexAddress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            lblHexAddress.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            stkPannel.Children.Add(lblHexAddress);

            for (int i = 1; i <= data.Length; i++)
            {
                islastHexBytesAddedToHexEdtr = true;
                // Create the textbox to hold the user byte data
                TextBox txtbxhexAddressed = new TextBox();

                CommandBinding txtbxhexAddressedCutCmdBnd = new CommandBinding(ApplicationCommands.Cut);
                txtbxhexAddressedCutCmdBnd.CanExecute += new CanExecuteRoutedEventHandler(txtbxhexAddressedCutCmdBnd_CanExecute);
                txtbxhexAddressedCutCmdBnd.Executed += new ExecutedRoutedEventHandler(txtbxhexAddressedCutCmdBnd_Executed);

                CommandBinding txtbxhexAddressedPasteCmdBnd = new CommandBinding(ApplicationCommands.Paste);
                txtbxhexAddressedPasteCmdBnd.CanExecute += new CanExecuteRoutedEventHandler(txtbxhexAddressedPasteCmdBnd_CanExecute);
                txtbxhexAddressedPasteCmdBnd.Executed += new ExecutedRoutedEventHandler(txtbxhexAddressedPasteCmdBnd_Executed);
                txtbxhexAddressed.CommandBindings.Add(txtbxhexAddressedCutCmdBnd);
                txtbxhexAddressed.CommandBindings.Add(txtbxhexAddressedPasteCmdBnd);

                // register the event
                txtbxhexAddressed.PreviewTextInput += new TextCompositionEventHandler(this.txtblk0ByteAddressClmn1_PreviewTextInput);
                txtbxhexAddressed.PreviewKeyDown += new KeyEventHandler(this.txtblk0ByteAddressClmn1_PreviewKeyDown);
                txtbxhexAddressed.IsUndoEnabled = false;
                txtbxhexAddressed.Name = "txtNumber" + i.ToString();
                txtbxhexAddressed.Text = "00";
                txtbxhexAddressed.CaretBrush = Brushes.Black;
                txtbxhexAddressed.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#0A000000");
                txtbxhexAddressed.TextAlignment = TextAlignment.Center;
                txtbxhexAddressed.Height = 30;
                txtbxhexAddressed.Width = 57;
                txtbxhexAddressed.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                txtbxhexAddressed.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                txtbxhexAddressed.MaxLength = 2;
                txtbxHexRepList[i - 1] = txtbxhexAddressed;

                if (i % 16 == 0)
                {
                    // 16 bytes on each row

                    // Add 16th byte in the stack panel
                    stkPannel.Children.Add(txtbxhexAddressed);
                    // To get original event handler to work by registering the name and 
                    // This will then allow us to call FindName on the StackPanel and find the TextBox.
                    stkPannel.RegisterName(txtbxhexAddressed.Name, txtbxhexAddressed);

                    // Add the stackpanel with maximum 16 bytes to the parent stackpanel
                    stkpnlByteAddress.Children.Add(stkPannel);

                    // To get original event handler to work by registering the name and 
                    // This will then allow us to call FindName on the parent stackPanel and find the child stackpanel.
                    stkpnlByteAddress.RegisterName(stkPannel.Name, stkPannel);

                    // Create a new stackpanel to hold next 16 bytes
                    stkPannel = new StackPanel();
                    stkPannel.Orientation = Orientation.Horizontal;
                    stkPannel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    stkPannel.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    stkPannel.Name = "stkpnlHexAddress" + i.ToString();

                    // Create a new label to represent next address eg: 0x0001, 0x0002 etc
                    lblHexAddress = new Label();
                    lblHexAddress.Height = 30;
                    lblHexAddress.Width = 57;
                    lblHexAddress.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    lblHexAddress.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    lblHexAddress.Content = "0x" + (i).ToString("x4"); ;
                    stkPannel.Children.Add(lblHexAddress);

                    // Create a name scope for the stackpanel. 
                    NameScope.SetNameScope(stkPannel, new NameScope());
                }
                else
                {
                    stkPannel.Children.Add(txtbxhexAddressed);
                    stkPannel.RegisterName(txtbxhexAddressed.Name, txtbxhexAddressed);
                    islastHexBytesAddedToHexEdtr = false;
                }
            }

            // Will be false when the last set of bytes are not divisible by 16. 
            // Hence add the remaining bytes to the parent stack panel for ex: if the length of the data is 40 bytes and 64 bytes.
            if (!islastHexBytesAddedToHexEdtr)
            {
                // Add the stackpanel with maximum 16 bytes to the parent stackpanel
                stkpnlByteAddress.Children.Add(stkPannel);

                // To get original event handler to work by registering the name and 
                // This will then allow us to call FindName on the parent stackPanel and find the child stackpanel.
                stkpnlByteAddress.RegisterName(stkPannel.Name, stkPannel);
            }
            hexAddressEdtrScrollViewer.ScrollToTop();
        }

        void txtbxhexAddressedPasteCmdBnd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string text = Clipboard.GetText();
            TextBox txt = (TextBox)sender;
            string copiedString = string.Empty;
            // Return if the lenth of text exceeds the maximumm limit of text editor
            if (text.Length > txt.MaxLength)
            {
                return;
            }
            else
            {

                if (txt.SelectionLength == txt.Text.Length)
                {
                    copiedString = text;
                }
                if (txt.SelectionStart >= txt.MaxLength)
                {
                    return;
                }
                else
                {
                    if (txt.SelectionLength == 0)
                    {
                        // Inseret the text at the selected position
                        copiedString = copiedString.Insert(txt.SelectionStart, text);
                    }
                    else
                    {
                        copiedString = copiedString.Remove(txt.SelectionStart, txt.SelectionLength);
                        copiedString = copiedString.Insert(txt.SelectionStart, text);
                    }
                    if (copiedString.Length > txt.MaxLength)
                    {
                        copiedString = copiedString.Remove(txt.MaxLength);
                    }
                }
                int hexNumber;
                e.Handled = !int.TryParse(copiedString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
                string temp = txt.Text;
                if (CacheUserMem.Count > 0)
                {
                    if (CacheUserMem[txt.Name.ToString()].ToString() != copiedString)
                    {
                        txt.Foreground = Brushes.Red;
                    }
                    else
                    {
                        txt.Foreground = Brushes.Black;
                    }
                }
                txt.Text = copiedString;
                txt.SelectionStart = copiedString.Length;
                txt.Foreground = Brushes.Red;

                // Enable write ascii and write hex buttons 
                btnWriteAscii.IsEnabled = true;
                btnWrite.IsEnabled = true;
                //txtASCIIData.Foreground = Brushes.Red;                
            }
        }

        void txtbxhexAddressedPasteCmdBnd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        void txtbxhexAddressedCutCmdBnd_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            int selectionPos = 0;
            TextBox txt = (TextBox)sender;
            selectionPos = txt.SelectionStart;
            // Copy the selected text            
            try
            {
                Clipboard.SetText(txt.SelectedText);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Clipboard calls can fail without warning. The MSDN documentation does not state this, but they can all throw exceptions of type COMException. 
                // The most common one that I've seen has the error "CLIPBRD_E_CANT_OPEN" which means that the application was unable to open the clipboard. 
                // This is generally because another application currently has the clipboard open. The sad thing about this is that generally the best thing to
                // do is to just try again in a moment, because it will probably work.
                System.Threading.Thread.Sleep(10);
                try
                {
                    Clipboard.SetText(txt.SelectedText);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    MessageBox.Show("Can't Access Clipboard", "Universal Reader Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            // Remove the selected text from the editor
            string hexStr = txt.Text.Remove(txt.SelectionStart, txt.SelectionLength);
            txt.Text = hexStr;
            txt.SelectionStart = selectionPos;
            txt.Foreground = Brushes.Red;


            // Enable write ascii and write hex buttons 
            btnWriteAscii.IsEnabled = true;
            btnWrite.IsEnabled = true;
            txtASCIIData.Foreground = Brushes.Red;
            e.Handled = true;
        }

        void txtbxhexAddressedCutCmdBnd_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        /// <summary>
        /// Controls whether ctrl x and ctrl v can execute on ascii text editor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        /// <summary>
        /// Ctrl v command implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string text = Clipboard.GetText();
            // Return if the lenth of text exceeds the maximumm limit of text editor
            if (text.Length > Convert.ToInt64(txtblkSpMAXSizeCharsAvailable.Text))
            {
                return;
            }
            if (((Convert.ToInt64(txtblkSpMAXSizeCharsAvailable.Text) - txtASCIIData.Text.Length) == 0) && (txtASCIIData.SelectionLength == 0))
            {
                return;
            }
            else
            {
                txtASCIIData.Foreground = Brushes.Red;
                btnWrite.IsEnabled = true;
                btnWriteAscii.IsEnabled = true;
                if (txtASCIIData.SelectionLength == txtASCIIData.Text.Length)
                {
                    editedAsciiNewString = text;
                }
                else
                {
                    if (txtASCIIData.SelectionLength == 0)
                    {
                        // Inseret the text at the selected position
                        editedAsciiNewString = editedAsciiNewString.Insert(txtASCIIData.SelectionStart, text);
                    }
                    else
                    {
                        editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength);
                        editedAsciiNewString = editedAsciiNewString.Insert(txtASCIIData.SelectionStart, text);
                    }
                    if (editedAsciiNewString.Length > Convert.ToInt32(txtblkSpMAXSizeCharsAvailable.Text))
                    {
                        editedAsciiNewString = editedAsciiNewString.Remove(Convert.ToInt32(txtblkSpMAXSizeCharsAvailable.Text));
                    }
                }
                txtASCIIData.Text = editedAsciiNewString;
                txtASCIIData.SelectionStart = editedAsciiNewString.Length;
                txtASCIIData.Foreground = Brushes.Red;

                // Enable write ascii and write hex buttons 
                btnWriteAscii.IsEnabled = true;
                btnWrite.IsEnabled = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Ctrl x command implementation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            int selectionPos = 0;
            selectionPos = txtASCIIData.SelectionStart;
            // Copy the selected text            
            try
            {
                Clipboard.SetText(txtASCIIData.Text.Substring(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength));
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Clipboard calls can fail without warning. The MSDN documentation does not state this, but they can all throw exceptions of type COMException. 
                // The most common one that I've seen has the error "CLIPBRD_E_CANT_OPEN" which means that the application was unable to open the clipboard. 
                // This is generally because another application currently has the clipboard open. The sad thing about this is that generally the best thing to
                // do is to just try again in a moment, because it will probably work.
                System.Threading.Thread.Sleep(10);
                try
                {
                    Clipboard.SetText(txtASCIIData.Text.Substring(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength));
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    MessageBox.Show("Can't Access Clipboard", "Universal Reader Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            // Remove the selected text from the editor
            editedAsciiNewString = editedAsciiNewString.Remove(txtASCIIData.SelectionStart, txtASCIIData.SelectionLength);
            txtASCIIData.Text = editedAsciiNewString;
            txtASCIIData.SelectionStart = selectionPos;
            txtASCIIData.Foreground = Brushes.Red;

            // Enable write ascii and write hex buttons 
            btnWriteAscii.IsEnabled = true;
            btnWrite.IsEnabled = true;
            e.Handled = true;
        }


        private void InitializeTagView()
        {
            stkM3eUserMemory.Visibility = Visibility.Collapsed;
            stkGen2UserMemory.Visibility = Visibility.Collapsed;
            CbxISO14443A = (CheckBox)Application.Current.MainWindow.FindName("iso14443aCheckBox");
            CbxISO15693 = (CheckBox)Application.Current.MainWindow.FindName("iso15693CheckBox");
            if (model.Equals("M3e"))
            {
                stkM3eUserMemory.Visibility = Visibility.Visible;
            }
            else
            {
                stkGen2UserMemory.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Reset usermemory tab to default values
        /// </summary>
        public void ResetUserMemoryTab()
        {
            if (null != objReader)
            {
                lblSelectFilter.Content = "";
                btnRead.Content = "Read";
                rbSelectedTagUserMemTb.IsEnabled = false;
                rbFirstTagUserMemTb.IsChecked = true;
                rbFirstTagUserMemTb.IsEnabled = true;

                rbHexRep.IsChecked = true;
                txtASCIIData.Text = "";
                txtASCIIData.Foreground = Brushes.Black;
                txtblkSpMAXSizeHexByteSp64BytesAvailable.Text = "00";
                txtblkSpMAXSizeCharsAvailable.Text = "00";
                txtblkRemainingCharsAvailableCount.Text = "00 characters remaining";
                RemoveGeneratedControl();

                lblUserMemoryError.Content = "";
                lblUserMemoryError.Visibility = System.Windows.Visibility.Collapsed;
                lblErrorAddAsciiEdtr.Content = "";
                lblErrorAddHexEdtr.Content = "";
                currentEPC = string.Empty;
                btnWrite.IsEnabled = false;
                btnWriteAscii.IsEnabled = false;
                txtASCIIData.IsEnabled = false;
                if (model.Equals("M3e"))
                {
                    editBlockNumber = null;
                    cacheLockStatus = null;
                    cacheBlockSize = 0;
                    cacheBlockCount = 0;
                    cacheData = null;
                    RemoveGeneratedControlM3e();
                    txtM3eDataASCII.Text = "";
                    txtM3eDataASCII.IsEnabled = false;
                    InitializeTagView();
                }
            }
        }

        private void rbFirstTagUserMemTb_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                ResetUserMemoryTab();
            }
        }
    }
}