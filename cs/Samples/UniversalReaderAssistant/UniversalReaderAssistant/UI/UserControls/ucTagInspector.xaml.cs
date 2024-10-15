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
using ThingMagic;
using ThingMagic.URA2.BL;
using AEITagDecoder;
using ThingMagic.URA2.Models;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for ucTagInspectorxaml.xaml
    /// </summary>
    public partial class ucTagInspector : UserControl
    {
        Reader objReader;
        uint startAddress = 0;
        int antenna = 0;
        Gen2.Bank selectMemBank;
        TagReadRecord tempSelectedTag;

        // Constant XPC_1 offset 
        const int XPC1OFFSET = 42;

        // Constant XPC_2 offset
        const int XPC2OFFSET = 44;
        #region Protocols
        private CheckBox cbxGen2;

        public CheckBox CbxGen2
        {
            get { return cbxGen2; }
            set { cbxGen2 = value; }
        }

        private CheckBox cbxATA;

        public CheckBox CbxATA
        {
            get { return cbxATA; }
            set { cbxATA = value; }
        }

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
        string model = string.Empty;

        public ucTagInspector()
        {
            InitializeComponent();
        }

        public void LoadTagInspector(Reader reader, string readerModel)
        {
            objReader = reader;
            model = readerModel;
            InitializeRadioButton();
            InitializeTagView();
        }

        public void LoadTagInspector(Reader reader, uint address, Gen2.Bank selectedBank, TagReadRecord selectedTagRed, string readerModel)
        {
            try
            {
                InitializeRadioButton();
                InitializeTagView();
                objReader = reader;
                startAddress = address;
                model = readerModel;
                rbFirstTagIns.IsEnabled = true;
                rbSelectedTagIns.IsChecked = true;
                rbSelectedTagIns.IsEnabled = true;
                rbEPCAscii.IsEnabled = true;
                rbEPCBase36.IsEnabled = true;
                btnRead.Content = "Refresh";
                antenna = selectedTagRed.Antenna;
                tempSelectedTag = selectedTagRed;
                selectMemBank = selectedBank;
                //txtEPCData.Text = selectedTagRed.EPC;
                string[] stringData = selectedTagRed.Data.Split(' ');
                txtEpc.Text = selectedTagRed.EPC;
                currentEPC = txtEpc.Text;
                txtData.Text = string.Join("", stringData);
                Window mainWindow = App.Current.MainWindow;
                ucTagResults tagResults = (ucTagResults)mainWindow.FindName("TagResults");
                if (selectedTagRed.Protocol == TagProtocol.GEN2)
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
                    protocol = TagProtocol.GEN2;
                    PopulateData();
                }
                else if (selectedTagRed.Protocol == TagProtocol.ATA)
                {
                    protocol = TagProtocol.ATA;
                    DecodeATATag(selectedTagRed.EPC);
                }
                else if (selectedTagRed.Protocol == TagProtocol.ISO14443A)
                {
                    Iso14443a.TagType tag;
                    Enum.TryParse(selectedTagRed.TagType.ToString().Replace(' ', '_'), out tag);
                    tag14443A = tag;
                    protocol = TagProtocol.ISO14443A;
                    Decode14443A(selectedTagRed.EPC, tag);
                }
                else if (selectedTagRed.Protocol == TagProtocol.ISO15693)
                {
                    Iso15693.TagType tag;
                    Enum.TryParse(selectedTagRed.TagType.ToString().Replace(' ', '_'), out tag);
                    tag15693 = tag;
                    protocol = TagProtocol.ISO15693;
                    Decode15693Tag(selectedTagRed.EPC, tag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error : Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTagView()
        {
            spTagInspector_M3e.Visibility = Visibility.Collapsed;
            spTagInspector_GEN2.Visibility = Visibility.Collapsed;
            spTagInspector_ATA.Visibility = Visibility.Collapsed;
            if (model.Equals("M3e"))
            {
                spTagInspector_M3e.Visibility = Visibility.Visible;
            }
            else
            {
                if ((bool)cbxGen2.IsChecked && !(bool)cbxATA.IsChecked)
                {
                    spTagInspector_GEN2.Visibility = Visibility.Visible;
                }
                else if (!(bool)cbxGen2.IsChecked && (bool)cbxATA.IsChecked)
                {
                    spTagInspector_ATA.Visibility = Visibility.Visible;
                }
            }
        }

        private void InitializeRadioButton()
        {
            CbxGen2 = (CheckBox)App.Current.MainWindow.FindName("gen2CheckBox");
            CbxATA = (CheckBox)App.Current.MainWindow.FindName("ataCheckBox");
            CbxISO14443A = (CheckBox)App.Current.MainWindow.FindName("iso14443aCheckBox");
            CbxISO15693 = (CheckBox)App.Current.MainWindow.FindName("iso15693CheckBox");
            if (model.Equals("M3e"))
            {
                lblNote.Visibility = Visibility.Collapsed;
                if (cbxISO14443A.Visibility == Visibility.Visible && cbxISO15693.Visibility == Visibility.Visible)
                {
                    lblNote.Content = "Note : Tag Inspector supports only ISO14443A and ISO15693 protocol tags.";
                }
                else
                {
                    if (cbxISO14443A.Visibility == Visibility.Visible)
                    {
                        lblNote.Content = "Note : Tag Inspector supports only " + cbxISO14443A.Content.ToString() + "protocol tags.";
                    }
                    else if (cbxISO15693.Visibility == Visibility.Visible)
                    {
                        lblNote.Content = "Note : Tag Inspector supports only " + cbxISO15693.Content.ToString() + "protocol tags.";
                    }
                }
            }
            else
            {
                lblNote.Visibility = Visibility.Visible;
                if (cbxATA.Visibility != Visibility.Visible)
                {
                    lblNote.Content = "Note : Tag Inspector supports only GEN2 protocol tags.";
                }
            }

        }

        string currentEPC = string.Empty;
        Iso14443a.TagType tag14443A = Iso14443a.TagType.AUTO_DETECT;
        Iso15693.TagType tag15693 = Iso15693.TagType.AUTO_DETECT;
        TagProtocol protocol = TagProtocol.NONE;

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Wait);
            InitializeRadioButton();
            Window mainWindow = App.Current.MainWindow;
            lbltagnotfound.Visibility = Visibility.Collapsed;
            try
            {
                TagReadData[] tagReads = null;
                if (btnRead.Content.Equals("Read"))
                {
                    #region Detect Tag

                    if (model.Equals("M3e"))
                    {
                        #region Protocol Region
                        if (cbxISO14443A.IsChecked == false && cbxISO15693.IsChecked == false)
                        {
                            MessageBox.Show("Please select Protocol", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        #endregion

                        if ((bool)cbxISO14443A.IsChecked && !((bool)cbxISO15693.IsChecked))
                        {
                            objReader.ParamSet("/reader/read/plan", (new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443A, null, null, 1000)));
                        }
                        else if (!((bool)cbxISO14443A.IsChecked) && (bool)cbxISO15693.IsChecked)
                        {
                            objReader.ParamSet("/reader/read/plan", (new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO15693, null, null, 1000)));
                        }
                        else
                        {
                            List<ReadPlan> readPlans = new List<ReadPlan>();
                            readPlans.Add(new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO14443A, null, 0));
                            readPlans.Add(new SimpleReadPlan(new int[] { 1 }, TagProtocol.ISO15693, null, 0));
                            MultiReadPlan multiReadPlan = new MultiReadPlan(readPlans);
                            objReader.ParamSet("/reader/read/plan", multiReadPlan);
                        }
                    }
                    else
                    {
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

                        #region Protocol Region
                        if (cbxGen2.IsChecked == false && cbxATA.IsChecked == false)
                        {
                            MessageBox.Show("Please select Protocol", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        #endregion

                        if ((bool)cbxGen2.IsChecked && !((bool)cbxATA.IsChecked))
                        {
                            protocol = TagProtocol.GEN2;
                            objReader.ParamSet("/reader/read/plan", new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), protocol, null, 0));
                        }
                        else if (!((bool)cbxGen2.IsChecked) && (bool)cbxATA.IsChecked)
                        {
                            protocol = TagProtocol.ATA;
                            objReader.ParamSet("/reader/read/plan", new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), protocol, null, 0));
                        }
                        else
                        {
                            List<ReadPlan> readPlans = new List<ReadPlan>();
                            readPlans.Add(new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), TagProtocol.GEN2, null, 0));
                            readPlans.Add(new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), TagProtocol.ATA, null, 0));
                            MultiReadPlan multiReadPlan = new MultiReadPlan(readPlans);
                            objReader.ParamSet("/reader/read/plan", multiReadPlan);
                        }
                    }

                    lblSelectFilter.Content = "";

                    if (model.Equals("M3e"))
                    {
                        SerialReader serialReader = objReader as SerialReader;
                        serialReader.IsProtocolDynamicSwitching = false;
                    }

                    tagReads = objReader.Read(750);

                    if ((tagReads != null) && (tagReads.Length != 0) && (tagReads[0].EpcString != ""))
                    {
                        if (tagReads.Length > 1)
                        {
                            lblTagInspectorError.Content = "Warning: More than one tag responded";
                            lblTagInspectorError.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            lblTagInspectorError.Visibility = System.Windows.Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        txtEpc.Text = "";
                        lblTagInspectorError.Content = "";
                        lblTagInspectorError.Visibility = System.Windows.Visibility.Collapsed;
                        currentEPC = string.Empty;
                        MessageBox.Show("No tags found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    #endregion
                }
                //M3e tag inspector coding
                if (model.Equals("M3e"))
                {
                    #region HF-LF Region
                    try
                    {
                        rbEPCAscii.IsEnabled = true;
                        if (btnRead.Content.Equals("Read"))
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
                        else
                        {
                            rbSelectedTagIns.IsChecked = true;
                            if (protocol == TagProtocol.ISO14443A)
                            {
                                Decode14443A(currentEPC, tag14443A);
                            }
                            if (protocol == TagProtocol.ISO15693)
                            {
                                Decode15693Tag(currentEPC, tag15693);
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    #region UTF region

                    try
                    {
                        rbEPCAscii.IsEnabled = true;
                        rbEPCBase36.IsEnabled = true;
                        if (btnRead.Content.Equals("Read"))
                        {
                            if (tagReads[0].Tag.Protocol == TagProtocol.GEN2)
                            {
                                currentEPC = tagReads[0].EpcString;
                                if ((bool)rbEPCAscii.IsChecked)
                                {
                                    txtEpc.Text = Utilities.HexStringToAsciiString(tagReads[0].EpcString);
                                }
                                else if ((bool)rbEPCBase36.IsChecked)
                                {
                                    txtEpc.Text = Utilities.ConvertHexToBase36(tagReads[0].EpcString);
                                }
                                else
                                {
                                    txtEpc.Text = tagReads[0].EpcString;
                                }
                                lblSelectFilter.Content = "EPC ID : " + tagReads[0].EpcString;
                                protocol = TagProtocol.GEN2;
                            }
                            else if (tagReads[0].Tag.Protocol == TagProtocol.ATA)
                            {
                                DecodeATATag(tagReads[0].EpcString);
                                protocol = TagProtocol.ATA;
                            }
                            else
                            {
                                MessageBox.Show("Please select a protocol.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                        {
                            rbSelectedTagIns.IsChecked = true;
                        }
                        if (protocol == TagProtocol.GEN2)
                            PopulateData();
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
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

        #region ISO14443A
        private void Decode14443A(string tagRead, Iso14443a.TagType tagType)
        {
            currentEPC = tagRead;
            lblSelectFilter.Content = "UID : " + currentEPC;
            spTagInspector_M3e.Visibility = Visibility.Visible;
            spTagInspector_GEN2.Visibility = spTagInspector_ATA.Visibility = Visibility.Collapsed;
            Populate14443AData(tagType, tagRead);
        }

        private void Populate14443AData(Iso14443a.TagType tag, string UID)
        {
            objReader.ParamSet("/reader/tagop/protocol", TagProtocol.ISO14443A);

            #region Reset the Tag Data UI element
            ResetTagData();
            #endregion

            #region Getting the tag memory
            string[] tagMemory = TagMemoryArchitecture.Get14443AMemoryLayout(tag);
            uint blockCount = 0;
            byte[] data = TagMemoryArchitecture.Get14443AMemory(objReader, tag, ref blockCount, UID).ToArray();
            tagMemory[1] = blockCount.ToString();
            #endregion

            Label[] labelcollection = new Label[] { lblM3e00, lblM3e02, lblM3e10, lblM3e12, lblM3e20, lblM3e22, lblM3e30, lblM3e32, lblM3e40, lblM3e42 };
            TextBox[] textboxCollection = new TextBox[] { txtM3e01, txtM3e03, txtM3e11, txtM3e13, txtM3e21, txtM3e23, txtM3e31, txtM3e33, txtM3e41, txtM3e43 };
            int postion = 0;
            int i = 0;

            {
                labelcollection[postion].Content = "Tag Type";
                textboxCollection[postion].Text = tag.ToString().Replace('_', ' ');
                textboxCollection[postion].TextAlignment = TextAlignment.Left;
                textboxCollection[postion].VerticalContentAlignment = VerticalAlignment.Center;
                textboxCollection[postion].IsReadOnly = true;
                labelcollection[postion].Visibility = textboxCollection[postion].Visibility = Visibility.Visible;
                postion++;
            }
            foreach (string item in tagMemory)
            {
                if (i == 1)
                {
                    labelcollection[postion].Content = "Block Count";
                    textboxCollection[postion].Text = item;
                }
                if (i == 0)
                {
                    labelcollection[postion].Content = "Block Size";
                    textboxCollection[postion].Text = item;
                }
                textboxCollection[postion].TextAlignment = TextAlignment.Left;
                textboxCollection[postion].VerticalContentAlignment = VerticalAlignment.Center;
                textboxCollection[postion].IsReadOnly = true;
                labelcollection[postion].Visibility = textboxCollection[postion].Visibility = Visibility.Visible;
                postion++;
                i++;
            }

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

            #region Get User Memory of the tag
            RemoveGeneratedControl();
            GenerateControls(data, Convert.ToInt32(tagMemory[0]), Convert.ToInt32(tagMemory[1]), lockStatus);
            #endregion

        }
        #endregion

        private void ResetTagData()
        {
            lblUID.Visibility = txtUID_M3e.Visibility = lblM3e00.Visibility = txtM3e01.Visibility = lblM3e02.Visibility = txtM3e03.Visibility = lblM3e10.Visibility = txtM3e11.Visibility = lblM3e12.Visibility = txtM3e13.Visibility = lblM3e20.Visibility = txtM3e21.Visibility = lblM3e22.Visibility = txtM3e23.Visibility = lblM3e30.Visibility = txtM3e31.Visibility = lblM3e32.Visibility = txtM3e33.Visibility = lblM3e40.Visibility = txtM3e41.Visibility = lblM3e42.Visibility = txtM3e43.Visibility = Visibility.Collapsed;
        }

        #region ISO15693

        private void Decode15693Tag(string tagRead, Iso15693.TagType tagType)
        {
            currentEPC = tagRead;
            lblSelectFilter.Content = "UID : " + currentEPC;
            spTagInspector_M3e.Visibility = Visibility.Visible;
            spTagInspector_GEN2.Visibility = spTagInspector_ATA.Visibility = Visibility.Collapsed;
            Populate15693Data(tagType, tagRead);
        }

        /// <summary>
        /// Populate ISO15693 tags
        /// </summary>
        /// <param name="tag">Selected tag</param>
        private void Populate15693Data(Iso15693.TagType tag, string UID)
        {
            objReader.ParamSet("/reader/tagop/protocol", TagProtocol.ISO15693);

            #region Reset the Tag Data UI element
            ResetTagData();
            #endregion

            #region Getting the tag memory organisation
            Dictionary<string, uint> tagMemory = TagMemoryArchitecture.Get15693AMemoryLayout(objReader, tag, UID);
            #endregion

            txtUID_M3e.Text = UID;
            txtUID_M3e.Visibility = Visibility.Collapsed;
            lblUID.Visibility = Visibility.Collapsed;

            #region Get UID break Down
            Dictionary<string, string> uidBreakDown = TagMemoryArchitecture.UIDBreakdown(UID);
            #endregion

            Label[] labelcollection = new Label[] { lblM3e00, lblM3e02, lblM3e10, lblM3e12, lblM3e20, lblM3e22, lblM3e30, lblM3e32, lblM3e40, lblM3e42 };
            TextBox[] textboxCollection = new TextBox[] { txtM3e01, txtM3e03, txtM3e11, txtM3e13, txtM3e21, txtM3e23, txtM3e31, txtM3e33, txtM3e41, txtM3e43 };
            int postion = 0;
            int i = 0;
            foreach (KeyValuePair<string, string> item in uidBreakDown)
            {
                if (i == 1 || i == 2)
                {
                    labelcollection[postion].Content = (i == 2) ? "Tag Type" : "Manufacturer";
                    textboxCollection[postion].Text = (i == 2) ? tag.ToString().Replace('_', ' ') : item.Value;
                    textboxCollection[postion].TextAlignment = TextAlignment.Left;
                    textboxCollection[postion].VerticalContentAlignment = VerticalAlignment.Center;
                    textboxCollection[postion].IsReadOnly = true;
                    labelcollection[postion].Visibility = textboxCollection[postion].Visibility = Visibility.Visible;
                    postion++;
                }
                i++;
            }
            foreach (KeyValuePair<string, uint> item in tagMemory)
            {
                if (item.Key != "Block Size" || item.Key != "Block Count")
                {
                    labelcollection[postion].Content = item.Key;
                    textboxCollection[postion].Text = item.Value.ToString();
                    textboxCollection[postion].TextAlignment = TextAlignment.Left;
                    textboxCollection[postion].VerticalContentAlignment = VerticalAlignment.Center;
                    textboxCollection[postion].IsReadOnly = true;
                    labelcollection[postion].Visibility = textboxCollection[postion].Visibility = Visibility.Visible;
                    postion++;
                }
            }

            #region Get block Lock Status
            List<byte> lockStatus = new List<byte>();
            if ((tag != Iso15693.TagType.ICODE_SLI_L) && (tag != Iso15693.TagType.ICODE_SLIX_L)
                 && (tag != Iso15693.TagType.PICOPASS) && (tag != Iso15693.TagType.HID_ICLASS_SE))
            {
                lockStatus = TagMemoryArchitecture.MemoryLockStatus(objReader, (byte)tagMemory["Block Count"], TagProtocol.ISO15693, UID);
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

            #region Get User Memory of the tag
            RemoveGeneratedControl();
            GenerateControls(data.ToArray(), (byte)tagMemory["Block Size"], (byte)tagMemory["Block Count"], lockStatus);
            #endregion
            //txtM3e01
        }

        #endregion

        #region ATA tag
        private void DecodeATATag(string epcdata)
        {
            try
            {
                spTagInspector_ATA.Visibility = Visibility.Visible;
                spTagInspector_M3e.Visibility = spTagInspector_GEN2.Visibility = Visibility.Collapsed;
                lblSelectFilter.Content = "EPC ID : " + epcdata;
                AeiTag decodedTag = new AeiTag(epcdata);
                txtATAEPCData.Text = epcdata;
                ResetATATextControl();
                if ((AeiTag.DataFormat)decodedTag.getDataFormat == (AeiTag.DataFormat.SIX_BIT_ASCII) && !decodedTag.IsHalfFrameTag)
                {
                    tblASCIIBinaryFormat.Visibility = lblASCIIBinaryFormat.Visibility = tblASCIIFormat.Visibility = lblASCIIFormat.Visibility = Visibility.Visible;
                    lblEquipmentGroup.Visibility = txtEquipmentGroup.Visibility = lblEquipmentInitial.Visibility = txtEquipmentInitial.Visibility = txtTagType.Visibility = lblTagType.Visibility = Visibility.Collapsed;
                    txtDataFormat.Foreground = Brushes.Gray;
                    txtDataFormat.Text = ((AeiTag.DataFormat)decodedTag.getDataFormat).ToString();
                    string binstrvalue = BinaryStringToHexString(epcdata);
                    StringBuilder asciiValue = new StringBuilder();
                    tblASCIIBinaryFormat.Text = binstrvalue;
                    List<int> finalList = AeiTag.FromString(binstrvalue);
                    foreach (int temp in finalList)
                        asciiValue.Append(AeiTag.convertDecToSixBitAscii(temp).ToString());
                    tblASCIIFormat.Text = asciiValue.ToString();
                }
                else
                {
                    tblASCIIBinaryFormat.Visibility = lblASCIIBinaryFormat.Visibility = tblASCIIFormat.Visibility = lblASCIIFormat.Visibility = Visibility.Collapsed;
                    lblEquipmentGroup.Visibility = txtEquipmentGroup.Visibility = lblEquipmentInitial.Visibility = txtEquipmentInitial.Visibility = txtTagType.Visibility = lblTagType.Visibility = Visibility.Visible;
                    if (decodedTag.IsFieldValid[AeiTag.TagField.EQUIPMENT_GROUP])
                    {

                        txtEquipmentGroup.Foreground = txtCarNumber.Foreground = txtSideIndicator.Foreground = txtLength.Foreground = txtNumberofAxles.Foreground = txtBearingType.Foreground = txtPlatformID.Foreground = Brushes.Gray;
                        txtEquipmentGroup.Text = ((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup).ToString();

                        if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.RAILCAR)
                        {
                            gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;
                            lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Visible;
                            lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Collapsed;
                            lblTypeDetails.Visibility = txtTypeDetails.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = Visibility.Collapsed;
                            else
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = Visibility.Visible;

                            txtCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtLength.Text = decodedTag.getLengthInDecimeters.ToString();
                            txtNumberofAxles.Text = decodedTag.getNumberOfAxles.ToString();
                            txtBearingType.Text = ((AeiTag.BearingType)decodedTag.getBearing).ToString();
                            txtPlatformID.Text = ((AeiTag.PlatformId)decodedTag.getPlatformId).ToString();
                            txtSpare.Text = decodedTag.getSpare.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.END_OF_TRAIN_DEVICE)
                        {
                            gridEndofTrain.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtEOTSpare.Visibility = lblEOTSpare.Visibility = Visibility.Collapsed;
                            else
                                txtEOTSpare.Visibility = lblEOTSpare.Visibility = Visibility.Visible;

                            txtEOTCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtEOTType.Text = decodedTag.getEOTType.ToString();
                            txtEOTSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtEOTSpare.Text = decodedTag.getEOTType.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.LOCOMOTIVE)
                        {
                            gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Collapsed;
                            lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Collapsed;
                            lblTypeDetails.Visibility = txtTypeDetails.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = lblSpare.Visibility = txtSpare.Visibility = Visibility.Collapsed;
                            else
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = lblSpare.Visibility = txtSpare.Visibility = Visibility.Visible;

                            txtCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtLength.Text = decodedTag.getLengthInDecimeters.ToString();
                            txtNumberofAxles.Text = decodedTag.getNumberOfAxles.ToString();
                            txtBearingType.Text = ((AeiTag.BearingType)decodedTag.getBearing).ToString();
                            txtSpare.Text = decodedTag.getSpare.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.INTERMODAL_CONTAINER)
                        {
                            gridIntermodal.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridEndofTrain.Visibility = gridChassis.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtIntermodalHeight.Visibility = lblIntermodalHeight.Visibility = txtIntermodalWidth.Visibility = lblIntermodalWidth.Visibility = txtIntermodalContainerType.Visibility = lblIntermodalContainerType.Visibility = txtIntermodalMaxGrossWeight.Visibility = lblIntermodalMaxGrossWeight.Visibility = txtIntermodalTareWeight.Visibility = lblIntermodalTareWeight.Visibility = txtIntermodalSpare.Visibility = lblIntermodalSpare.Visibility = Visibility.Collapsed;
                            else
                                txtIntermodalHeight.Visibility = lblIntermodalHeight.Visibility = txtIntermodalWidth.Visibility = lblIntermodalWidth.Visibility = txtIntermodalContainerType.Visibility = lblIntermodalContainerType.Visibility = txtIntermodalMaxGrossWeight.Visibility = lblIntermodalMaxGrossWeight.Visibility = txtIntermodalTareWeight.Visibility = lblIntermodalTareWeight.Visibility = txtIntermodalSpare.Visibility = lblIntermodalSpare.Visibility = Visibility.Visible;

                            txtEquipmentGroup.Text = "CONTAINER";
                            txtIntermodalCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtIntermodalCheckDigit.Text = decodedTag.getCheckDigit.ToString();
                            txtIntermodalLengthCM.Text = decodedTag.getLengthCM.ToString();
                            txtIntermodalHeight.Text = decodedTag.getHeight.ToString();
                            txtIntermodalWidth.Text = decodedTag.getWidth.ToString();
                            txtIntermodalContainerType.Text = decodedTag.getContainerType.ToString();
                            txtIntermodalMaxGrossWeight.Text = decodedTag.getMaxGrossWeight.ToString();
                            txtIntermodalTareWeight.Text = decodedTag.getTareWeight.ToString();
                            txtIntermodalSpare.Text = decodedTag.getSpare.ToString();

                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.TRAILER)
                        {
                            gridTrailer.Visibility = Visibility.Visible;
                            gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;
                            if (decodedTag.IsHalfFrameTag)
                                txtTrailerLengthCM.Visibility = lblTrailerLengthCM.Visibility = txtTrailerWidth.Visibility = lblTrailerWidth.Visibility = txtTrailerTandemWidth.Visibility = lblTrailerTandemWidth.Visibility = txtTypeDetails.Visibility = lblTypeDetails.Visibility = txtTrailerForwardExtension.Visibility = lblTrailerForwardExtension.Visibility = txtTrailerTareWeight.Visibility = lblTrailerTareWeight.Visibility = txtTrailerHeight.Visibility = lblTrailerHeight.Visibility = Visibility.Collapsed;
                            else
                                txtTrailerLengthCM.Visibility = lblTrailerLengthCM.Visibility = txtTrailerWidth.Visibility = lblTrailerWidth.Visibility = txtTrailerTandemWidth.Visibility = lblTrailerTandemWidth.Visibility = txtTypeDetails.Visibility = lblTypeDetails.Visibility = txtTrailerForwardExtension.Visibility = lblTrailerForwardExtension.Visibility = txtTrailerTareWeight.Visibility = lblTrailerTareWeight.Visibility = txtTrailerHeight.Visibility = lblTrailerHeight.Visibility = Visibility.Collapsed;

                            txtTrailerNumber.Text = decodedTag.getTrailerNumer.ToString();
                            txtTrailerNumberString.Text = decodedTag.getTrailerNumerString;
                            txtTrailerLengthCM.Text = decodedTag.getLengthCM.ToString();
                            txtTrailerWidth.Text = decodedTag.getWidth.ToString();
                            txtTrailerTandemWidth.Text = decodedTag.getTandemWidth.ToString();
                            txtTypeDetails.Text = decodedTag.getTypeDetail.ToString();
                            txtTrailerForwardExtension.Text = decodedTag.getForwardExtension.ToString();
                            txtTrailerTareWeight.Text = decodedTag.getTareWeight.ToString();
                            txtTrailerHeight.Text = decodedTag.getHeight.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.CHASSIS)
                        {
                            gridChassis.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridEndofTrain.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtChassisTandemWidth.Visibility = lblChassisTandemWidth.Visibility = txtChassisForwardExtension.Visibility = lblChassisForwardExtension.Visibility = txtChassisKingPinSettin.Visibility = lblChassisKingPinSettin.Visibility = txtChassisAxleSpacing.Visibility = lblChassisAxleSpacing.Visibility = txtChassisRunningGearLoc.Visibility = lblChassisRunningGearLoc.Visibility = txtChassisNumLength.Visibility = lblChassisNumLength.Visibility = txtChassisMinLength.Visibility = lblChassisMinLength.Visibility = txtChassisSpare.Visibility = lblChassisSpare.Visibility = txtChassisMaxLength.Visibility = lblChassisMaxLength.Visibility = Visibility.Collapsed;
                            else
                                txtChassisTandemWidth.Visibility = lblChassisTandemWidth.Visibility = txtChassisForwardExtension.Visibility = lblChassisForwardExtension.Visibility = txtChassisKingPinSettin.Visibility = lblChassisKingPinSettin.Visibility = txtChassisAxleSpacing.Visibility = lblChassisAxleSpacing.Visibility = txtChassisRunningGearLoc.Visibility = lblChassisRunningGearLoc.Visibility = txtChassisNumLength.Visibility = lblChassisNumLength.Visibility = txtChassisMinLength.Visibility = lblChassisMinLength.Visibility = txtChassisSpare.Visibility = lblChassisSpare.Visibility = txtChassisMaxLength.Visibility = lblChassisMaxLength.Visibility = Visibility.Collapsed;

                            txtChassisCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtChassisTypeDetail.Text = decodedTag.getTypeDetail.ToString();
                            txtChassisTareWeight.Text = decodedTag.getTareWeight.ToString();
                            txtChassisHeight.Text = decodedTag.getHeight.ToString();
                            txtChassisTandemWidth.Text = decodedTag.getTandemWidth.ToString();
                            txtChassisForwardExtension.Text = decodedTag.getForwardExtension.ToString();
                            txtChassisKingPinSettin.Text = decodedTag.getKingPinSetting.ToString();
                            txtChassisAxleSpacing.Text = decodedTag.getAxleSpacing.ToString();
                            txtChassisRunningGearLoc.Text = decodedTag.getRunningGearLoc.ToString();
                            txtChassisNumLength.Text = decodedTag.getNumLengths.ToString();
                            txtChassisMinLength.Text = decodedTag.getMinLength.ToString();
                            txtChassisSpare.Text = decodedTag.getSpare.ToString();
                            txtChassisMaxLength.Text = decodedTag.getMaxLength.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.RAILCAR_COVER)
                        {
                            gridRailcarCover.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridIntermodal.Visibility = gridEndofTrain.Visibility = gridChassis.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;
                            if (decodedTag.IsHalfFrameTag)
                                txtInsulation.Visibility = lblInsulation.Visibility = txtfitting.Visibility = lblfitting.Visibility = txtAssocRailCarInitial.Visibility = lblAssocRailCarInitial.Visibility = txtAssocRailCarInitialString.Visibility = lblAssocRailCarInitialString.Visibility = txtAssocRailCarNumber.Visibility = lblAssocRailCarNumber.Visibility = Visibility.Collapsed;
                            else
                                txtInsulation.Visibility = lblInsulation.Visibility = txtfitting.Visibility = lblfitting.Visibility = txtAssocRailCarInitial.Visibility = lblAssocRailCarInitial.Visibility = txtAssocRailCarInitialString.Visibility = lblAssocRailCarInitialString.Visibility = txtAssocRailCarNumber.Visibility = lblAssocRailCarNumber.Visibility = Visibility.Visible;

                            txtRailcarCoverCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtRailcarCoverSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtRailcarCoverLength.Text = decodedTag.getLengthInDecimeters.ToString();
                            txtCoverType.Text = decodedTag.getCoverType.ToString();
                            txtDateBuilt.Text = decodedTag.getDateBuilt.ToString();
                            txtInsulation.Text = decodedTag.getInsulation.ToString();
                            txtfitting.Text = decodedTag.getFitting.ToString();
                            txtAssocRailCarInitial.Text = decodedTag.getAssocRailcarInitial.ToString();
                            txtAssocRailCarInitialString.Text = decodedTag.getAssocRailcarInitialString.ToString();
                            txtAssocRailCarNumber.Text = decodedTag.getAssocRailcarInitialNumber.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.PASSIVE_ALARM_TAG)
                        {
                            gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Visible;
                            lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Visible;
                            lblTypeDetails.Visibility = txtTypeDetails.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Collapsed;
                            else
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Visible;

                            txtCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtLength.Text = decodedTag.getLengthInDecimeters.ToString();
                            txtNumberofAxles.Text = decodedTag.getNumberOfAxles.ToString();
                            txtBearingType.Text = ((AeiTag.BearingType)decodedTag.getBearing).ToString();
                            txtSpare.Text = decodedTag.getSpare.ToString();
                            txtPlatformID.Text = ((AeiTag.PlatformId)decodedTag.getPlatformId).ToString();
                            txtAlarm.Text = decodedTag.getAlarm.ToString();
                            txtTypeDetails.Text = decodedTag.getTypeDetail.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.GENERATOR_SET)
                        {
                            gridGeneratorSet.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            if (decodedTag.IsHalfFrameTag)
                                txtGeneratorSetTareWeight.Visibility = lblGeneratorSetTareWeight.Visibility = txtFuelCapacity.Visibility = lblFuelCapacity.Visibility = txtVoltage.Visibility = lblVoltage.Visibility = txtGeneratorSerSpare.Visibility = lblGeneratorSerSpare.Visibility = Visibility.Collapsed;
                            else
                                txtGeneratorSetTareWeight.Visibility = lblGeneratorSetTareWeight.Visibility = txtFuelCapacity.Visibility = lblFuelCapacity.Visibility = txtVoltage.Visibility = lblVoltage.Visibility = txtGeneratorSerSpare.Visibility = lblGeneratorSerSpare.Visibility = Visibility.Visible;

                            txtGeneratorSetNumber.Text = decodedTag.getGenSetNumber.ToString();
                            txtGeneratorSetNumberString.Text = decodedTag.getGenSetNumberString.ToString();
                            txtMounting.Text = decodedTag.getMouting.ToString();
                            txtGeneratorSetTareWeight.Text = decodedTag.getTareWeight.ToString();
                            txtFuelCapacity.Text = decodedTag.getFuelCapacity.ToString();
                            txtVoltage.Text = decodedTag.getVoltage.ToString();
                            txtGeneratorSerSpare.Text = decodedTag.getSpare.ToString();
                        }
                        else if (((AeiTag.EquipmentGroup)decodedTag.getEquipmentGroup) == AeiTag.EquipmentGroup.MULTIMODAL_EQUIPMENT)
                        {
                            gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = Visibility.Visible;
                            gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridChassis.Visibility = gridEndofTrain.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;

                            lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Visible;
                            lblAlarm.Visibility = txtAlarm.Visibility = Visibility.Collapsed;
                            lblTypeDetails.Visibility = txtTypeDetails.Visibility = Visibility.Visible;

                            if (decodedTag.IsHalfFrameTag)
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = txtTypeDetails.Visibility = lblTypeDetails.Visibility = Visibility.Collapsed;
                            else
                                txtNumberofAxles.Visibility = lblNumberofAxles.Visibility = txtBearingType.Visibility = lblBearingType.Visibility = txtPlatformID.Visibility = lblPlatformID.Visibility = lblSpare.Visibility = txtSpare.Visibility = txtTypeDetails.Visibility = lblTypeDetails.Visibility = Visibility.Visible;

                            txtCarNumber.Text = decodedTag.getCarNumber.ToString();
                            txtSideIndicator.Text = ((AeiTag.SideIndicator)decodedTag.getSide).ToString();
                            txtLength.Text = decodedTag.getLengthInDecimeters.ToString();
                            txtNumberofAxles.Text = decodedTag.getNumberOfAxles.ToString();
                            txtBearingType.Text = ((AeiTag.BearingType)decodedTag.getBearing).ToString();
                            txtSpare.Text = decodedTag.getSpare.ToString();
                            txtPlatformID.Text = ((AeiTag.PlatformId)decodedTag.getPlatformId).ToString();
                            txtTypeDetails.Text = decodedTag.getTypeDetail.ToString();
                        }
                    }
                    else
                    {
                        gridErrorMessage.Visibility = Visibility.Visible;
                        tblErrorMessage.Text += "- Unknown/unidentified tag type, can not proceed with tag parsing\nand displaying the raw ASCII data.\n";
                        txtEquipmentGroup.Text = Convert.ToString(decodedTag.getEquipmentGroup, 2).PadLeft(5, '0');
                        tblBinaryFormat.Text = BinaryStringToHexString(epcdata);
                    }

                    txtEquipmentInitial.Text = decodedTag.getEquipmentInitialString;

                    if (decodedTag.IsFieldValid[AeiTag.TagField.TAG_TYPE])
                    {
                        txtTagType.Foreground = Brushes.Gray;
                        txtTagType.Text = ((AeiTag.TagType)decodedTag.getTagType).ToString();
                    }
                    else
                    {
                        txtTagType.Foreground = Brushes.Red;
                        txtTagType.Text = Convert.ToString(decodedTag.getTagType, 2).PadLeft(2, '0');
                        tblErrorMessage.Text += "- Invalid Tag Type.\n";
                    }

                    if (decodedTag.IsFieldValid[AeiTag.TagField.DATA_FORMAT])
                    {
                        txtDataFormat.Foreground = Brushes.Gray;
                        txtDataFormat.Text = ((AeiTag.DataFormat)decodedTag.getDataFormat).ToString();
                    }
                    else
                    {
                        txtDataFormat.Foreground = Brushes.Red;
                        txtDataFormat.Text = Convert.ToString(decodedTag.getDataFormat, 2).PadLeft(6, '0');
                        tblErrorMessage.Text += "- Invalid Data Format.\n";
                    }

                    if (decodedTag.IsHalfFrameTag)
                        txtDataFormat.Visibility = lblDataFormat.Visibility = Visibility.Collapsed;
                    else
                        txtDataFormat.Visibility = lblDataFormat.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                gridErrorMessage.Visibility = Visibility.Visible;
                tblErrorMessage.Text += "- Unsupported AEI Tag.\n" + "- " + ex.Message;
                tblBinaryFormat.Text = BinaryStringToHexString(epcdata);
                throw new Exception("Unsupported AEI Tag. " + ex.Message);
            }
            finally
            {
                tblErrorMessage.Text = tblErrorMessage.Text.TrimEnd();
            }
        }

        private void ResetATATextControl()
        {
            txtCarNumber.Text = "";
            txtSideIndicator.Text = "";
            txtLength.Text = "";
            txtNumberofAxles.Text = "";
            txtBearingType.Text = "";
            txtSpare.Text = "";
            txtPlatformID.Text = "";
            txtAlarm.Text = "";
            txtTypeDetails.Text = "";

            txtTrailerNumber.Text = "";
            txtTrailerNumberString.Text = "";
            txtTrailerLengthCM.Text = "";
            txtTrailerWidth.Text = "";
            txtTrailerTandemWidth.Text = "";
            txtTypeDetails.Text = "";
            txtTrailerForwardExtension.Text = "";
            txtTrailerTareWeight.Text = "";
            txtTrailerHeight.Text = "";

            txtChassisCarNumber.Text = "";
            txtChassisTypeDetail.Text = "";
            txtChassisTareWeight.Text = "";
            txtChassisHeight.Text = "";
            txtChassisTandemWidth.Text = "";
            txtChassisForwardExtension.Text = "";
            txtChassisKingPinSettin.Text = "";
            txtChassisAxleSpacing.Text = "";
            txtChassisRunningGearLoc.Text = "";
            txtChassisNumLength.Text = "";
            txtChassisMinLength.Text = "";
            txtChassisSpare.Text = "";
            txtChassisMaxLength.Text = "";

            txtEOTCarNumber.Text = "";
            txtEOTType.Text = "";
            txtEOTSideIndicator.Text = "";
            txtEOTSpare.Text = "";

            txtIntermodalCarNumber.Text = "";
            txtIntermodalCheckDigit.Text = "";
            txtIntermodalLengthCM.Text = "";
            txtIntermodalHeight.Text = "";
            txtIntermodalWidth.Text = "";
            txtIntermodalContainerType.Text = "";
            txtIntermodalMaxGrossWeight.Text = "";
            txtIntermodalTareWeight.Text = "";
            txtIntermodalSpare.Text = "";

            txtRailcarCoverCarNumber.Text = "";
            txtRailcarCoverSideIndicator.Text = "";
            txtRailcarCoverLength.Text = "";
            txtCoverType.Text = "";
            txtDateBuilt.Text = "";
            txtInsulation.Text = "";
            txtfitting.Text = "";
            txtAssocRailCarInitial.Text = "";
            txtAssocRailCarInitialString.Text = "";
            txtAssocRailCarNumber.Text = "";

            txtGeneratorSetNumber.Text = "";
            txtGeneratorSetNumberString.Text = "";
            txtMounting.Text = "";
            txtGeneratorSetTareWeight.Text = "";
            txtFuelCapacity.Text = "";
            txtVoltage.Text = "";
            txtGeneratorSerSpare.Text = "";

            tblErrorMessage.Text = "";
            tblBinaryFormat.Text = "";
        }

        private void InitializeControlForEOT()
        {
            lblCarNumber.Text = "EOT Number : ";
            lblLength.Text = "EOT Type : ";
            lblSideIndicator.Visibility = txtSideIndicator.Visibility = Visibility.Visible;
            lblBearingType.Visibility = txtBearingType.Visibility = lblNumberofAxles.Visibility = txtNumberofAxles.Visibility = lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Collapsed;
            //lblCheckDigit.Visibility = txtCheckDigit.Visibility = lblContainerLength.Visibility = txtContainerLength.Visibility = lblContainerHeight.Visibility = txtContainerHeight.Visibility = lblContainerGrossWeight.Visibility = txtContainerGrossWeight.Visibility = lblContainerTareWeight.Visibility = txtContainerTareWeight.Visibility = lblContainerType.Visibility = txtContainerType.Visibility = lblConainterWidth.Visibility = txtConainterWidth.Visibility = Visibility.Collapsed;
        }

        private void InitializeControlForLocomotive()
        {
            lblCarNumber.Text = "Car Number : ";
            lblLength.Text = "Length (dm) : ";
            lblBearingType.Visibility = txtBearingType.Visibility = lblNumberofAxles.Visibility = txtNumberofAxles.Visibility = Visibility.Visible;
            lblSideIndicator.Visibility = txtSideIndicator.Visibility = Visibility.Visible;
            lblPlatformID.Visibility = txtPlatformID.Visibility = Visibility.Collapsed;
            //lblCheckDigit.Visibility = txtCheckDigit.Visibility = lblContainerLength.Visibility = txtContainerLength.Visibility = lblContainerHeight.Visibility = txtContainerHeight.Visibility = lblContainerGrossWeight.Visibility = txtContainerGrossWeight.Visibility = lblContainerTareWeight.Visibility = txtContainerTareWeight.Visibility = lblContainerType.Visibility = txtContainerType.Visibility = lblConainterWidth.Visibility = txtConainterWidth.Visibility = Visibility.Collapsed;
        }

        private void InitializeControlForContainer()
        {
            lblBearingType.Visibility = txtBearingType.Visibility = lblNumberofAxles.Visibility = txtNumberofAxles.Visibility = lblPlatformID.Visibility = txtPlatformID.Visibility = lblLength.Visibility = txtLength.Visibility = lblSideIndicator.Visibility = txtSideIndicator.Visibility = Visibility.Collapsed;
            //lblCheckDigit.Visibility = txtCheckDigit.Visibility = lblContainerLength.Visibility = txtContainerLength.Visibility = lblContainerHeight.Visibility = txtContainerHeight.Visibility = lblContainerGrossWeight.Visibility = txtContainerGrossWeight.Visibility = lblContainerTareWeight.Visibility = txtContainerTareWeight.Visibility = lblContainerType.Visibility = txtContainerType.Visibility = lblConainterWidth.Visibility = txtConainterWidth.Visibility = Visibility.Visible;

        }
        #endregion

        #region Gen2 Tag
        string userData = string.Empty;
        string unUsedEpcData = string.Empty;
        string unadditionalMemData = string.Empty;
        private void PopulateData()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                spTagInspector_GEN2.Visibility = Visibility.Visible;
                spTagInspector_M3e.Visibility = spTagInspector_ATA.Visibility = Visibility.Collapsed;

                // Create the object to read tag memory
                ReadTagMemory readTagMem = new ReadTagMemory(objReader, model);

                if ((bool)rbFirstTagIns.IsChecked)
                {
                    antenna = GetSelectedAntennaList()[0];
                }

                objReader.ParamSet("/reader/tagop/antenna", antenna);
                TagFilter searchSelect = null;

                if ((bool)rbSelectedTagIns.IsChecked)
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

                #region ReadReservedMemory

                //Read Reserved memory bank data
                ushort[] reservedData = null;

                txtKillPassword.Text = "";
                txtAcessPassword.Text = "";
                txtReservedMemUnusedValue.Text = "";
                // Hide additional memory textboxes
                lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Collapsed;
                txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Collapsed;
                lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Collapsed;

                try
                {
                    readTagMem.ReadTagMemoryData(Gen2.Bank.RESERVED, searchSelect, ref reservedData);
                    // Parse the response to get access pwd, kill pwd and if additional memory exists
                    ParseReservedMemData(reservedData);
                }
                catch (Exception ex)
                {
                    // Hide additional memory textboxes
                    lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Collapsed;
                    txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Collapsed;
                    lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Collapsed;
                    txtReservedMemUnusedValue.Text = "";
                    // If either of the memory is locked get in or else throw the exception.
                    if ((ex is FAULT_PROTOCOL_BIT_DECODING_FAILED_Exception) || (ex is FAULT_GEN2_PROTOCOL_MEMORY_LOCKED_Exception))
                    {
                        try
                        {
                            ReadReservedMemData(Gen2.Bank.RESERVED, searchSelect);
                        }
                        catch (Exception e)
                        {
                            if (e.Message.ToLower().Contains("no tags found"))
                            {
                                txtKillPassword.Text = "Unable to read Kill Password";
                                txtAcessPassword.Text = "Unable to read Access Password";
                                lbltagnotfound.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                txtKillPassword.Text = txtAcessPassword.Text = e.Message;
                            }
                        }
                    }
                    else if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                    {
                        txtKillPassword.Text = "Read Error";
                        txtAcessPassword.Text = "Read Error";
                    }
                    else
                    {
                        if (ex.Message.ToLower().Contains("no tags found"))
                        {
                            txtKillPassword.Text = "Unable to read Kill Password";
                            txtAcessPassword.Text = "Unable to read Access Password";
                            lbltagnotfound.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtKillPassword.Text = txtAcessPassword.Text = ex.Message;
                        }
                    }
                }
                #endregion ReadReservedMemory

                #region ReadEPCMemory
                //Read EPC bank data
                ushort[] epcData = null;

                txtPC.Text = "";
                txtCRC.Text = "";
                txtEPCData.Text = "";
                txtEPCValue.Text = "";
                txtEPCUnused.Text = "";
                txtEPCUnusedValue.Text = "";
                txtadditionalMemValue.Text = "";

                // Hide additional memory
                spUnused.Visibility = System.Windows.Visibility.Collapsed;
                spXPC.Visibility = System.Windows.Visibility.Collapsed;
                spXPC2.Visibility = System.Windows.Visibility.Collapsed;
                spAddMemory.Visibility = System.Windows.Visibility.Collapsed;

                try
                {
                    readTagMem.ReadTagMemoryData(Gen2.Bank.EPC, searchSelect, ref epcData);
                    ParseEPCMemData(epcData, searchSelect);
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("no tags found"))
                    {
                        txtEPCData.Text = "Unable to read EPC Memory Bank.";
                        lbltagnotfound.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        txtEPCData.Text = ex.Message;
                    }
                    rbEPCAscii.IsEnabled = false;
                    rbEPCBase36.IsEnabled = false;
                }
                #endregion ReadEPCMemory

                #region ReadTIDMemory
                //Read TID bank data
                ushort[] tidData = null;

                txtClsID.Text = "";
                txtVendorID.Text = "";
                txtVendorValue.Text = "";
                txtModelID.Text = "";
                txtModeldIDValue.Text = "";
                txtUniqueIDValue.Text = "";

                try
                {
                    readTagMem.ReadTagMemoryData(Gen2.Bank.TID, searchSelect, ref tidData);
                    ParseTIDMemData(tidData);
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                    {
                        txtUniqueIDValue.Text = "Read Error";
                    }
                    else
                    {
                        if (ex.Message.ToLower().Contains("no tags found"))
                        {
                            txtUniqueIDValue.Text = "Unable to read TID Memory Bank.";
                            lbltagnotfound.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtUniqueIDValue.Text = ex.Message;
                        }
                    }
                }
                #endregion ReadTIDMemory

                #region ReadUserMemory
                //Read USER bank data
                ushort[] userMemData = null;

                txtUserDataValue.Text = "";
                txtUserMemData.Text = "";

                try
                {
                    readTagMem.ReadTagMemoryData(Gen2.Bank.USER, searchSelect, ref userMemData);
                    ParseUserMemData(userMemData);
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                    {
                        txtUserMemData.Text = "Read Error";
                    }
                    else
                    {
                        if (ex.Message.ToLower().Contains("no tags found"))
                        {
                            txtUserMemData.Text = "Unable to read User Memory Bank.";
                            lbltagnotfound.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            txtUserMemData.Text = ex.Message;
                        }
                    }
                }
                #endregion ReadUserMemory
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

        /// <summary>
        /// Parse reserved memory data and populate the reserved mem textboxes
        /// </summary>
        /// <param name="reservedData">accepts read reserved memory data</param>
        private void ParseReservedMemData(ushort[] reservedData)
        {
            string reservedMemData = string.Empty;
            if (null != reservedData)
                reservedMemData = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");

            if (reservedMemData.Length > 0)
            {
                if (reservedMemData.Length > 11)
                {
                    // Extract kill pwd
                    txtKillPassword.Text = reservedMemData.Substring(0, 11).TrimStart(' ');
                    string tempData = reservedMemData.Substring(12).TrimStart(' ');
                    // Check if reserved memory has additional data
                    if (tempData.Length > 11)
                    {
                        // Extract access pwd
                        txtAcessPassword.Text = reservedMemData.Substring(12, 11).TrimStart(' ');
                        // Extract additional reserved memory
                        txtReservedMemUnusedValue.Text = reservedMemData.Substring(24).TrimStart(' ');

                        // Visible additional memory textboxes
                        lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Visible;
                        txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Visible;
                        lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        // Extract access pwd
                        txtAcessPassword.Text = reservedMemData.Substring(12).TrimStart(' ');

                        // Hide additional memory textboxes
                        lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Collapsed;
                        txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Collapsed;
                        lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
                else
                {
                    txtKillPassword.Text = reservedMemData.Substring(0, reservedData.Length).TrimStart(' ');
                }
            }
        }

        /// <summary>
        /// Parse epc memory data and populate the epc mem textboxes
        /// </summary>
        /// <param name="epcData">accepts read epc memory data</param>
        private void ParseEPCMemData(ushort[] epcData, TagFilter filter)
        {
            unUsedEpcData = string.Empty;
            byte[] epcBankData = null;
            if (null != epcData)
            {
                epcBankData = ByteConv.ConvertFromUshortArray(epcData);
                int readOffset = 0;
                byte[] epc, crc, pc, unusedEpc = null, additionalMemData = null;
                int lengthCounter = 2;
                crc = SubArray(epcBankData, ref readOffset, lengthCounter);
                pc = SubArray(epcBankData, ref readOffset, lengthCounter);
                lengthCounter += 2;

                // Extract the epc length from pc word
                int epclength = Convert.ToInt32(((pc[0] & 0xf8) >> 3)) * 2;

                epc = SubArray(epcBankData, ref readOffset, epclength);

                List<byte> xpc = new List<byte>();

                /* Add support for XPC bits
                    * XPC_W1 is present, when the 6th most significant bit of PC word is set
                    */
                if ((pc[0] & 0x02) == 0x02)
                {
                    /* When this bit is set, the XPC_W1 word will follow the PC word
                        * Our TMR_Gen2_TagData::pc has enough space, so copying to the same.
                        */
                    try
                    {
                        ushort[] xpcW1 = (ushort[])objReader.ExecuteTagOp(new Gen2.ReadData(Gen2.Bank.EPC, 0x21, 1), filter);
                        spXPC.Visibility = System.Windows.Visibility.Visible;
                        lblXPC1MemAddress.Content = "33";
                        xpc.AddRange(ByteConv.ConvertFromUshortArray(xpcW1));
                        lengthCounter += 2;
                        txtXPC1.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(xpcW1), "", " ");
                        lblXPC1.Content = "XPC";
                    }
                    catch (Exception ex)
                    {
                        spXPC.Visibility = System.Windows.Visibility.Visible;
                        txtXPC1.Text = ex.Message;
                        lblXPC1.Content = "XPC";
                    }
                    /* If the most siginificant bit of XPC_W1 is set, then there exists
                    * XPC_W2. A total of 6  (PC + XPC_W1 + XPC_W2 bytes)
                    */
                    if ((xpc[0] & 0x80) == 0x80)
                    {
                        try
                        {
                            ushort[] xpcW2 = (ushort[])objReader.ExecuteTagOp(new Gen2.ReadData(Gen2.Bank.EPC, 0x22, 1), filter);
                            spXPC2.Visibility = System.Windows.Visibility.Visible;
                            lblXPC2MemAddress.Content = "34";
                            lengthCounter += 2;
                            txtXPC2.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(xpcW2), "", " ");
                            // Change the name of XPC to XPC1
                            lblXPC1.Content = "XPC1";
                        }
                        catch (Exception ex)
                        {
                            spXPC2.Visibility = System.Windows.Visibility.Visible;
                            txtXPC2.Text = ex.Message;
                            // Change the name of XPC to XPC1
                            lblXPC1.Content = "XPC1";
                        }
                    }
                }
                // Read extended epc memory
                if (epcBankData.Length > (lengthCounter + epclength))
                {
                    lblExtdEPCMemAddress.Content = Convert.ToString(readOffset / 2);
                    bool isExtendedEPCMemover = true;
                    uint startExtdEPCMemAddress = (uint)readOffset / 2;
                    List<ushort> data = new List<ushort>();
                    try
                    {
                        while (isExtendedEPCMemover)
                        {
                            // Make sure reading of memory word by word doesn't override XPC1 data
                            if (startExtdEPCMemAddress < 33)
                            {
                                data.AddRange((ushort[])objReader.ExecuteTagOp(new Gen2.ReadData(Gen2.Bank.EPC, startExtdEPCMemAddress, 1), filter));
                                startExtdEPCMemAddress += 1;
                            }
                            else
                            {
                                // Read of memory should not exceed XPC bytes
                                isExtendedEPCMemover = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // If more then once the below exceptions are recieved then come out of the loop.
                        if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception || (-1 != ex.Message.IndexOf("LLRP reader unknown error")) || (-1 != ex.Message.IndexOf("General Tag Error")) || (-1 != ex.Message.IndexOf("Tag data access failed")))
                        {
                            if (data.Count > 0)
                            {
                                // Just skip the exception and move on. So as not to lose the already read data.
                                isExtendedEPCMemover = false;
                            }
                        }
                    }

                    if (data.Count > 0)
                    {
                        unusedEpc = ByteConv.ConvertFromUshortArray(data.ToArray());
                    }
                }

                // Read additional memory
                if (epcBankData.Length > (lengthCounter + epclength))
                {
                    lblAddMemAddress.Content = "35";
                    bool isAdditionalMemover = true;
                    uint startAdditionalMemAddress = 0x23;
                    List<ushort> dataAdditionalMem = new List<ushort>();
                    try
                    {
                        while (isAdditionalMemover)
                        {
                            dataAdditionalMem.AddRange((ushort[])objReader.ExecuteTagOp(new Gen2.ReadData(Gen2.Bank.EPC, startAdditionalMemAddress, 1), filter));
                            startAdditionalMemAddress += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If more then once the below exceptions are recieved then come out of the loop.
                        if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception || (-1 != ex.Message.IndexOf("LLRP reader unknown error")) || (-1 != ex.Message.IndexOf("General Tag Error")) || (-1 != ex.Message.IndexOf("Tag data access failed")))
                        {
                            if (dataAdditionalMem.Count > 0)
                            {
                                // Just skip the exception and move on. So as not to lose the already read data.
                                isAdditionalMemover = false;
                            }
                        }
                    }
                    if (dataAdditionalMem.Count > 0)
                    {
                        additionalMemData = ByteConv.ConvertFromUshortArray(dataAdditionalMem.ToArray());
                    }
                }

                if (txtXPC1.Text != "")
                {
                    spXPC.Visibility = System.Windows.Visibility.Visible;
                }
                if (txtXPC2.Text != "")
                {
                    spXPC2.Visibility = System.Windows.Visibility.Visible;
                }

                txtCRC.Text = ByteFormat.ToHex(crc, "", " ");
                txtPC.Text = ByteFormat.ToHex(pc, "", " ");
                if (epc.Length == epclength)
                {
                    txtEPCData.Text = ByteFormat.ToHex(epc, "", " ");
                }
                else
                {
                    txtEPCData.Text = currentEPC;
                }
                if (null != unusedEpc)
                {
                    txtEPCUnused.Text = ByteFormat.ToHex(unusedEpc, "", " ");
                    unUsedEpcData = ByteFormat.ToHex(unusedEpc, "", "");
                    // Visible additional memory
                    spUnused.Visibility = System.Windows.Visibility.Visible;
                }

                if (null != additionalMemData)
                {
                    txtAdditionalMem.Text = ByteFormat.ToHex(additionalMemData, "", " ");
                    unadditionalMemData = ByteFormat.ToHex(additionalMemData, "", "");
                    // Visible additional memory
                    spAddMemory.Visibility = System.Windows.Visibility.Visible;
                }

                if ((bool)rbEPCAscii.IsChecked)
                {
                    txtEPCValue.Text = Utilities.HexStringToAsciiString(currentEPC);
                    txtEPCUnusedValue.Text = Utilities.HexStringToAsciiString(unUsedEpcData);
                    txtadditionalMemValue.Text = Utilities.HexStringToAsciiString(unadditionalMemData);
                }
                else if ((bool)rbEPCBase36.IsChecked)
                {
                    txtEPCValue.Text = Utilities.ConvertHexToBase36(currentEPC);
                    txtEPCUnusedValue.Text = Utilities.ConvertHexToBase36(unUsedEpcData);
                    txtadditionalMemValue.Text = Utilities.ConvertHexToBase36(unadditionalMemData);
                }
            }
        }

        /// <summary>
        /// Parse tid memory data and populate the tid mem textboxes
        /// </summary>
        /// <param name="tidData">accepts read tid memory data</param>
        private void ParseTIDMemData(ushort[] tidData)
        {
            string tidBankData = string.Empty;
            if (null != tidData)
                tidBankData = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(tidData), "", " ");

            if (tidBankData.Length > 0)
            {
                txtClsID.Text = tidBankData.Substring(0, 2).TrimStart(' ');
                txtVendorID.Text = tidBankData.Substring(3, 4).Replace(" ", string.Empty);
                string tagModel = string.Empty;
                txtVendorValue.Text = GetVendor(txtVendorID.Text, tidBankData.Substring(7, 4).Replace(" ", string.Empty), out tagModel);
                txtModelID.Text = tidBankData.Substring(7, 4).Replace(" ", string.Empty);
                txtModeldIDValue.Text = tagModel;
                if (tidBankData.Length >= 12)
                    txtUniqueIDValue.Text = tidBankData.Substring(12).TrimStart(' ');
            }
        }

        /// <summary>
        /// Parse user memory data and populate the user mem textboxes
        /// </summary>
        /// <param name="userData">accepts read user memory data</param>
        private void ParseUserMemData(ushort[] userData)
        {
            string userMemData = string.Empty;
            if (null != userData)
            {
                userMemData = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(userData), "", "");
                txtUserDataValue.Text = ReplaceSpecialCharInAsciiData(Utilities.HexStringToAsciiString(userMemData).ToCharArray());
                txtUserMemData.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(userData), "", " ");
            }
        }

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

        private void ReadReservedMemData(Gen2.Bank bank, TagFilter filter)
        {
            ushort[] reservedData;
            TagOp op;
            try
            {
                try
                {
                    // Read kill password
                    op = new Gen2.ReadData(Gen2.Bank.RESERVED, 0, 2);
                    reservedData = (ushort[])objReader.ExecuteTagOp(op, filter);
                    if (null != reservedData)
                    {
                        txtKillPassword.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");
                    }
                    else
                    {
                        txtKillPassword.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                    {
                        txtKillPassword.Text = "Read Error";
                    }
                    else
                    {
                        if (ex.Message.ToLower().Contains("no tag found"))
                        {
                            txtKillPassword.Text = "Unable to read Kill Password";
                        }
                        else
                        {
                            txtKillPassword.Text = ex.Message;
                        }
                    }
                }

                try
                {
                    // Read access password
                    reservedData = null;
                    op = new Gen2.ReadData(Gen2.Bank.RESERVED, 2, 2);
                    reservedData = (ushort[])objReader.ExecuteTagOp(op, filter);
                    if (null != reservedData)
                    {
                        txtAcessPassword.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");
                    }
                    else
                    {
                        txtAcessPassword.Text = "";
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                    {
                        txtAcessPassword.Text = "Read Error";
                    }
                    else
                    {
                        if (ex.Message.ToLower().Contains("no tag found"))
                        {
                            txtAcessPassword.Text = "Unable to read Access Password";
                        }
                        else
                        {
                            txtAcessPassword.Text = ex.Message;
                        }
                    }
                }

                // Read additional memory password
                try
                {
                    reservedData = null;
                    if (model.Equals("Astra"))
                    {
                        ReadAdditionalReservedMemDataM5eVariants(Gen2.Bank.RESERVED, 4, filter, out reservedData);
                    }
                    else
                    {
                        op = new Gen2.ReadData(Gen2.Bank.RESERVED, 4, 0);
                        reservedData = (ushort[])objReader.ExecuteTagOp(op, filter);
                    }

                    if (null != reservedData)
                    {
                        txtReservedMemUnusedValue.Text = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");
                        // Visible additional memory textboxes
                        lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Visible;
                        txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Visible;
                        lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        txtReservedMemUnusedValue.Text = "";
                    }
                }
                catch
                {
                    // catch the exception and move on. Only some tags has aditional memory 
                    txtReservedMemUnusedValue.Text = "";
                    // Hide additional memory textboxes
                    lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Collapsed;
                    txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Collapsed;
                    lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Read additional reserved memory for m5e variants
        /// </summary>
        /// <param name="bank"></param>
        /// <param name="startAddress"></param>
        /// <param name="filter"></param>
        /// <param name="data"></param>
        private void ReadAdditionalReservedMemDataM5eVariants(Gen2.Bank bank, uint startAddress, TagFilter filter, out ushort[] data)
        {
            data = null;
            int words = 1;
            TagOp op;
            while (true)
            {
                try
                {
                    op = new Gen2.ReadData(bank, startAddress, Convert.ToByte(words));
                    data = (ushort[])objReader.ExecuteTagOp(op, filter);
                    words++;
                }
                catch (Exception)
                {
                    throw;
                }
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

        private string GetVendor(string vendorId, string tagId, out string model)
        {
            string vendor = string.Empty;
            model = string.Empty;

            switch (vendorId)
            {
                case Utilities.Impinj: vendor = "Impinj";
                    switch (tagId)
                    {
                        case Utilities.ImpinjOld: model = "Old"; break;
                        case Utilities.ImpinjAnchor: model = "Anchor"; break;
                        case Utilities.ImpinjMonaco: model = "Monaco"; break;
                        case Utilities.ImpinjMonza: model = "Monza"; break;
                        case Utilities.ImpinjMonza2: model = "Monza 2"; break;
                        case Utilities.ImpinjMonza2A: model = "Monza 2A"; break;
                        case Utilities.ImpinjMonzaID_3: model = "Monza ID/3"; break;
                        case Utilities.ImpinjMonzaID_3A: model = "Monza ID/3A"; break;
                        case Utilities.ImpinjMonzaID_3B: model = "Monza ID/3B"; break;
                        case Utilities.ImpinjMonzaID_3C: model = "Monza ID/3C"; break;
                        case Utilities.ImpinjMonza3: model = "Monza 3"; break;
                        case Utilities.ImpinjMonza_3A: model = "Monza 3A"; break;
                        case Utilities.ImpinjMonza_3B: model = "Monza 3B"; break;
                        case Utilities.ImpinjMonza_3C: model = "Monza 3C"; break;
                        case Utilities.ImpinjMonza_3D: model = "Monza 3D"; break;
                        default: break;
                    }
                    break;
                case Utilities.ImpinjXTID: vendor = "Impinj";
                    switch (tagId)
                    {
                        case Utilities.ImpinjMonza_4D: model = "Monza 4D"; break;
                        case Utilities.ImpinjMonza_4E: model = "Monza 4E"; break;
                        case Utilities.ImpinjMonza_4I: model = "Monza 4I"; break;
                        case Utilities.ImpinjMonza_4U: model = "Monza 4U"; break;
                        case Utilities.ImpinjMonza_4QT: model = "Monza 4QT"; break;
                        case Utilities.ImpinjMonza_5: model = "Monza 5"; break;
                        case Utilities.ImpinjMonza_X2K: model = "Monza X2K"; break;
                        case Utilities.ImpinjMonza_R6: model = "Monza R6"; break;
                        case Utilities.ImpinjMonza_R6_P: model = "Monza R6-P"; break;
                        case Utilities.ImpinjMonza_S6_C: model = "Monza S6-C"; break;
                        default: break;
                    }
                    break;
                case Utilities.TI: vendor = "TI"; break;
                case Utilities.Alien: vendor = "Alien";
                    switch (tagId)
                    {
                        case Utilities.AlienHiggs2: model = "Higgs 2"; break;
                        case Utilities.AlienHiggs3: model = "Higgs 3"; break;
                        case Utilities.AlienHiggs4: model = "Higgs 4"; break;
                        case Utilities.AlienHiggsEC: model = "Higgs EC"; break;
                        default: break;
                    }
                    break;
                case Utilities.Phillips: vendor = "Phillips"; break;
                case Utilities.NXP: vendor = "NXP";
                    switch (tagId)
                    {
                        case Utilities.NXP_G2: model = "G2"; break;
                        case Utilities.NXP_G2_XM: model = "G2 XM"; break;
                        case Utilities.NXP_G2_XL: model = "G2 XL"; break;
                        case Utilities.NXP_G2_iL_V0: model = "G2 iL V0"; break;
                        case Utilities.NXP_G2_iL_V2: model = "G2 iL V2"; break;
                        case Utilities.NXP_G2_iL_V6: model = "G2 iL V6"; break;
                        case Utilities.NXP_G2_iL_Plus_V0: model = "G2 iL+ V0"; break;
                        case Utilities.NXP_G2_iL_Plus_V2: model = "G2 iL+ V2"; break;
                        case Utilities.NXP_G2_iL_Plus_V6: model = "G2 iL+ V6"; break;
                        case Utilities.NXP_G2_IM: model = "G2 iM"; break;
                        case Utilities.NXP_G2_IM_Plus: model = "G2 iM+"; break;
                        case Utilities.NXPI2C: model = "I2C"; break;
                        case Utilities.NXP_UCODE_8: model = "UCODE8"; break;
                        case Utilities.NXP_UCODE_8m: model = "UCODE 8m"; break;
                        default: break;
                    }
                    break;
                case Utilities.NXPXTID: vendor = "NXPXTID";
                    switch (tagId)
                    {
                        case Utilities.NXP_UCODE_7: model = "UCODE7"; break;
                        case Utilities.NXP_UCODE_7m: model = "UCODE 7m"; break;
                        case Utilities.NXPI2C4011: model = "I2C 4011"; break;
                        case Utilities.NXPI2C4021: model = "I2C 4011"; break;
                        default: break;
                    }
                    break;
                case Utilities.NXPv2: vendor = "NXP";
                    switch (tagId)
                    {
                        case Utilities.NXP_UCODE_7xm: model = "UCODE 7xm"; break;
                        case Utilities.NXP_UCODE_7xm_Plus: model = "UCODE 7xm+"; break;
                        case Utilities.NXP_UCODE_DNA: model = "UCODE DNA"; break;
                    }
                    break;
                case Utilities.STMicro: vendor = "STMicro";
                    switch (tagId)
                    {
                        case Utilities.STMicro_XRAG2: model = "XRAG2"; break;
                        default: break;
                    }
                    break;
                case Utilities.EMMICRO: vendor = "EM Micro";
                    switch (tagId)
                    {
                        case Utilities.EMMICRO_EM4126_V0: model = "EM4126 V0"; break;
                        case Utilities.EMMICRO_EM4126_V1: model = "EM4126 V1"; break;
                        case Utilities.EMMICRO_EM4126_V2: model = "EM4126 V2"; break;
                        case Utilities.EMMICRO_EM4126_V3: model = "EM4126 V3"; break;
                        case Utilities.EMMICRO_EM4126_V4: model = "EM4126 V4"; break;
                        case Utilities.EMMICRO_EM4126_V5: model = "EM4126 V5"; break;
                        case Utilities.EMMICRO_EM4126_V6: model = "EM4126 V6"; break;
                        case Utilities.EMMICRO_EM4126_V7: model = "EM4126 V7"; break;
                        case Utilities.EMMICRO_EM4124: model = "EM4124"; break;
                        case Utilities.EMMICRO_EM4124_D: model = "EM4124 D"; break;
                        case Utilities.PowerIDEmMicroWithTamper: model = "With tamper alaram"; break;
                        case Utilities.PowerIDEmMicroWithOutTamper: model = "With out tamper alaram"; break;
                        default: break;
                    }
                    break;
                case Utilities.EMMICROXTID: vendor = "EM Micro";
                    switch (tagId)
                    {
                        case Utilities.EMMICRO_EM4325_V11: model = "EM4325 V11"; break;
                        case Utilities.EMMICRO_EM4325_V21: model = "EM4325 V21"; break;
                        case Utilities.EMMICRO_EM4325_V31: model = "EM4325 V31"; break;
                        case Utilities.EMMICRO_EM4325_V41: model = "EM4325 V41"; break;
                        case Utilities.EMMICRO_EM4423_S: model = "EM4423 S"; break;
                        case Utilities.EMMICRO_EM4423_L: model = "EM4423 L"; break;
                        case Utilities.EMMICRO_EM4423_S_PLUS: model = "EM4423 S+"; break;
                        case Utilities.EMMICRO_EM4423_L_PLUS: model = "EM4423 L+"; break;
                        case Utilities.EMMICRO_EM4423T_S: model = "EM4423T S"; break;
                        case Utilities.EMMICRO_EM4423T_L: model = "EM4423T L"; break;
                        case Utilities.EMMICRO_EM4423T_S_PLUS: model = "EM4423T S+"; break;
                        case Utilities.EMMICRO_EM4423T_L_PLUS: model = "EM4423T L+"; break;
                        default: break;
                    }
                    break;
                case Utilities.Hitchi: vendor = "Hitchi"; break;
                case Utilities.Quanray: vendor = "Quanray";
                    switch (tagId)
                    {
                        case Utilities.QuanrayQstar5: model = "Qstar5"; break;
                        default: break;
                    }
                    break;
                case Utilities.FUJITSU: vendor = "Fujitsu";
                    switch (tagId)
                    {
                        case Utilities.FUJUTSU_64K: model = "64K"; break;
                        default: break;
                    }
                    break;
                case Utilities.IDS: vendor = "IDS";
                    switch (tagId)
                    {
                        case Utilities.IDSSL900A: model = "SL900A"; break;
                        default: break;
                    }
                    break;
                case Utilities.RAMTRON: vendor = "RAMTRON";
                    switch (tagId)
                    {
                        case Utilities.RAMTRON_WM72016: model = "WM72016"; break;
                        default: break;
                    }
                    break;
                case Utilities.Tego: vendor = "Tego";
                    switch (tagId)
                    {
                        case Utilities.Tego4K: model = "TegoChip 4K"; break;
                        case Utilities.Tego8K: model = "TegoChip 8K"; break;
                        case Utilities.Tego24K: model = "TegoChip 24K"; break;
                        default: break;
                    }
                    break;
                case Utilities.TegoXTID: vendor = "Tego";
                    switch (tagId)
                    {
                        case Utilities.Tego4K: model = "TegoChip 4K"; break;
                        case Utilities.Tego8K: model = "TegoChip 8K"; break;
                        case Utilities.Tego24K: model = "TegoChip 24K"; break;
                        default: break;
                    }
                    break;
                case Utilities.RFMicron: vendor = "RF Micron";
                    switch (tagId)
                    {
                        case Utilities.RFMicronMagnus: model = "Magnus"; break;
                        default: break;
                    }
                    break;
                case Utilities.WvonBraun: vendor = "W von Braun";
                    switch (tagId)
                    {
                        case Utilities.NMV2D: model = "NMV2D"; break;
                        default: break;
                    }
                    break;
                case Utilities.Fudan: vendor = "Fudan";
                    switch(tagId)
                    {
                        case Utilities.FudanDT160: model = "DT160"; break;
                    }
                    break;
            }
            return vendor;
        }

        private void rbEPCAscii_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                if (!(txtEpc.Text.Equals("")))
                {
                    txtEPCValue.Text = Utilities.HexStringToAsciiString(currentEPC);
                    txtEPCUnusedValue.Text = Utilities.HexStringToAsciiString(unUsedEpcData);
                    txtadditionalMemValue.Text = Utilities.HexStringToAsciiString(unadditionalMemData);
                }
            }
        }

        private void rbEPCBase36_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                if (!(txtEpc.Text.Equals("")))
                {
                    txtEPCValue.Text = Utilities.ConvertHexToBase36(currentEPC);
                    txtEPCUnusedValue.Text = Utilities.ConvertHexToBase36(unUsedEpcData);
                    txtadditionalMemValue.Text = Utilities.ConvertHexToBase36(unadditionalMemData);
                }
            }
        }

        private void rbUserAscii_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != objReader)
                {
                    if (!(txtUserDataValue.Text.Equals(string.Empty)))
                    {
                        txtUserDataValue.Text = Utilities.HexStringToAsciiString(userData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void rbUserBase36_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (null != objReader)
                {
                    if (!(txtUserDataValue.Text.Equals(string.Empty)))
                    {
                        txtUserDataValue.Text = Utilities.ConvertHexToBase36(userData);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtUserMemData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Don't accept any characters in the textbox
            e.Handled = true;
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private void txtEPCData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Back) || (e.Key == Key.Delete))
            {
                e.Handled = true;
            }
        }

        private void rbFirstTagIns_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                ResetTagInspectorTab();
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

        /// <summary>
        /// Reset taginspector tab to default values
        /// </summary>
        public void ResetTagInspectorTab()
        {
            if (null != objReader)
            {
                //set the default values for TagInspector tab
                lblSelectFilter.Content = "";
                txtAcessPassword.Text = "";
                txtKillPassword.Text = "";
                txtEpc.Text = "";
                txtCRC.Text = "";
                txtPC.Text = "";
                lblTagInspectorError.Content = "";
                lblTagInspectorError.Visibility = System.Windows.Visibility.Collapsed;
                txtEPCData.Text = "";
                txtEPCValue.Text = "";

                // Hide XPC bytes textbox
                spXPC.Visibility = System.Windows.Visibility.Collapsed;
                spXPC2.Visibility = System.Windows.Visibility.Collapsed;
                lblXPC1.Content = "XPC";
                txtXPC1.Text = "";
                txtXPC2.Text = "";

                //Hide additional memory
                spAddMemory.Visibility = System.Windows.Visibility.Collapsed;
                txtAdditionalMem.Text = "";
                txtadditionalMemValue.Text = "";

                // Hide reserved additional memory textboxes
                lblAdditionalReservedMemAdd.Visibility = System.Windows.Visibility.Collapsed;
                lblAdditionalReservedMem.Visibility = System.Windows.Visibility.Collapsed;
                txtReservedMemUnusedValue.Visibility = System.Windows.Visibility.Collapsed;
                txtReservedMemUnusedValue.Text = "";

                // Hide epc additional memory textboxes
                spUnused.Visibility = System.Windows.Visibility.Collapsed;
                txtEPCUnused.Text = "";
                txtEPCUnusedValue.Text = "";

                txtClsID.Text = "";
                txtVendorID.Text = "";
                txtVendorValue.Text = "";
                txtModelID.Text = "";
                txtModeldIDValue.Text = "";
                txtUniqueIDValue.Text = "";

                txtUserMemData.Text = "";
                txtUserDataValue.Text = "";
                btnRead.Content = "Read";
                rbSelectedTagIns.IsEnabled = false;
                rbFirstTagIns.IsChecked = true;
                rbFirstTagIns.IsEnabled = true;
                rbEPCAscii.IsChecked = true;
                rbEPCAscii.IsEnabled = true;
                rbEPCBase36.IsEnabled = true;

                if (model.Equals("M3e"))
                {
                    #region Reset Tag data UI element
                    ResetTagData();
                    #endregion
                    RemoveGeneratedControl();
                }

                lblSelectFilter.Content = "";

                gridChassis.Visibility = gridTrailer.Visibility = gridRailcarCover.Visibility = gridIntermodal.Visibility = gridEndofTrain.Visibility = gridRailcarLocomotiveMultiModalPassiveAlarm.Visibility = gridGeneratorSet.Visibility = gridErrorMessage.Visibility = Visibility.Collapsed;
                txtATAEPCData.Text = "";
                txtTagType.Text = "";
                txtDataFormat.Text = "";
                txtEquipmentGroup.Text = "";
                txtEquipmentInitial.Text = "";
                tblASCIIFormat.Text = "";
                tblASCIIBinaryFormat.Text = "";
            }
        }

        private string BinaryStringToHexString(string binary)
        {
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);
            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                // pad to length multiple of 8
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 1)
            {
                string eightBits = binary.Substring(i, 1);
                result.Append(Convert.ToString(Convert.ToInt64(eightBits, 16), 2).PadLeft(4, '0'));
            }

            return result.ToString();
        }
        #endregion


        /// <summary>
        /// Generate textboxes for hex representation dynamically based on the tag data length
        /// </summary>
        /// <param name="data">Tag Data received from the module</param>
        private void GenerateControls(byte[] data, int blockSizeByte, int blockCountByte, List<byte> lockStatus)
        {
            try
            {
                bool isLastTagBlockAdded = false;
                //TextBox[] txtBlockCellList = new TextBox[data.Length];
                List<TextBox> txtBlockCellList = new List<TextBox>();

                // Create new label the represent byte's in next address eg: Byte 1, Byte 2 etc.
                Label lblEmptyGridCell = new Label();
                lblEmptyGridCell.Height = 30;
                lblEmptyGridCell.Width = 57;
                lblEmptyGridCell.VerticalAlignment = VerticalAlignment.Top;
                lblEmptyGridCell.HorizontalAlignment = HorizontalAlignment.Left;
                lblEmptyGridCell.Content = "";
                stkpnlByteAddress.Children.Add(lblEmptyGridCell);
                // Create a namescope for the yte Address panel
                NameScope.SetNameScope(stkpnlByteAddress, new NameScope());
                int byteAddressCount = blockSizeByte + 1;
                for (int i = 0; i < byteAddressCount; i++)//here we will Add the total byte address count
                {
                    // Create a new label to represent Byte Address eg: Byte 1, Byte 2 etc.
                    Label lblAddressGridCell = new Label();
                    lblAddressGridCell.Height = 50;
                    lblAddressGridCell.Width = 80;
                    lblAddressGridCell.VerticalAlignment = VerticalAlignment.Top;
                    lblAddressGridCell.HorizontalAlignment = HorizontalAlignment.Left;
                    lblAddressGridCell.HorizontalContentAlignment = HorizontalAlignment.Center;
                    lblAddressGridCell.Content = ((i) != (byteAddressCount - 1)) ? ("Byte " + i.ToString()) : ("Lock Status");
                    if (lblAddressGridCell.Content == "Lock Status")
                    {
                        lblAddressGridCell.Width = 90;
                    }
                    stkpnlByteAddress.Children.Add(lblAddressGridCell);
                }
                int byteCount = blockSizeByte;//here we will add the byte count of the tag.
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
                        txtCellAddress.IsReadOnly = true;
                        txtCellAddress.Name = "txtCellNumber" + i.ToString() + j.ToString();
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
                        #region TextBox Command Binding

                        #endregion
                        int cellNumber = Convert.ToInt32((i).ToString() + (j).ToString());
                        stkPanel.Children.Add(txtCellAddress);
                        //NameScope.SetNameScope(txtCellAddress.Name,txtCellAddress); enable when the Binding is done
                        //txtBlockCellList[cellNumber] = txtCellAddress;
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
                    txtLockStatus.IsReadOnly = true;
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
                    txtLockStatus.VerticalAlignment = VerticalAlignment.Top;
                    txtLockStatus.HorizontalAlignment = HorizontalAlignment.Left;
                    txtLockStatus.Visibility = Visibility.Visible;
                    txtLockStatus.VerticalContentAlignment = VerticalAlignment.Center;
                    #region Click Binding Lock Textbox;

                    #endregion
                    stkPanel.Children.Add(txtLockStatus);
                    stkpnlBlockAddress.Children.Add(stkPanel);

                }

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
            catch (Exception ex)
            {
                throw new Exception("No data could be read from a tag");
            }
        }


        private void RemoveGeneratedControl()
        {
            try
            {
                StackPanel stkpnlByteAdd = (StackPanel)this.FindName("stkpnlBlockAddress");

                // Loop through the parent stackpanel and get its child elements
                foreach (StackPanel stkpnlChild in stkpnlByteAdd.Children)
                {


                    // Unregister the control so that this exception can be resolved "Cannot register duplicate Name 'stkPnl' in this scope."
                    // Because textbox controls are created dynamically for each read.
                    //stkpnlByteAdd.UnregisterName(stkpnlChild.Name.ToString());
                    // Removes all the elements present in the each child stackpanel element
                    stkpnlChild.Children.Clear();
                }

                StackPanel stkpnlHexClmnAddRep = (StackPanel)this.FindName("stkpnlByteAddress");
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
    }
}
