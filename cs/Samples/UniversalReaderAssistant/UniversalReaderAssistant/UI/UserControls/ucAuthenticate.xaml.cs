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
using System.Globalization;
using AesLib;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for ucAuthenticate.xaml
    /// </summary>
    public partial class ucAuthenticate : UserControl
    {
        Reader objReader;
        int antenna = 0;
        uint startAddress = 0;
        string model = string.Empty;
        TagReadRecord selectedTagReadRecord;
        Gen2.Bank selectMemBank;
        TagFilter searchSelect = null;
        string currentEPC = string.Empty;
        Gen2.NXP.AES.Profile MemoryProfile;


        public ucAuthenticate()
        {
            InitializeComponent();
            btnRefresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        /// <summary>
        /// LoadAuthenticateMemory
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="readerModel"></param>
        public void LoadAuthenticateMemory(Reader reader, string readerModel)
        {
            objReader = reader;
            model = readerModel;
        }

        /// <summary>
        /// Load Authentication memory to insert and activate keys
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="address"></param>
        /// <param name="selectedBank"></param>
        /// <param name="selectedTagRed"></param>
        /// <param name="readerModel"></param>
        public void LoadAuthenticateMemory(Reader reader, uint address, Gen2.Bank selectedBank, TagReadRecord selectedTagRed, string readerModel)
        {
            objReader = reader;
            startAddress = address;
            model = readerModel;

            spAuthenticate.IsEnabled = true;
            rbFirstTagAuthenticateTb.IsEnabled = true;
            rbSelectedTagAuthenticateTb.IsChecked = true;
            rbSelectedTagAuthenticateTb.IsEnabled = true;

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
            switch (selectedBank)
            {
                case Gen2.Bank.EPC:
                    if (tagResults.txtSelectedCell.Text == "Data")
                    {
                        lblSelectFilter.Content = "Showing tag: EPC data at decimal address " + address.ToString() + "  = " + txtData.Text;
                    }
                    else
                    {
                        lblSelectFilter.Content = "Showing tag: EPC ID = " + selectedTagRed.EPC;
                    }
                    break;
                case Gen2.Bank.TID:
                    if (tagResults.txtSelectedCell.Text == "Data")
                    {
                        lblSelectFilter.Content = "Showing tag: TID data at decimal address " + address.ToString() + " = " + txtData.Text;
                    }
                    else
                    {
                        lblSelectFilter.Content = "Showing tag: EPC ID = " + selectedTagRed.EPC;
                    }
                    break;
                case Gen2.Bank.USER:
                    if (tagResults.txtSelectedCell.Text == "Data")
                    {
                        lblSelectFilter.Content = "Showing tag: User data at decimal address " + address.ToString() + " = " + txtData.Text;
                    }
                    else
                    {
                        lblSelectFilter.Content = "Showing tag: EPC ID = " + selectedTagRed.EPC;
                    }
                    break;
            }
            PopulateAuthenticateData();
        }

        /// <summary>
        /// Populate keys of the tag
        /// </summary>
        private void PopulateAuthenticateData()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                ResetUntraceableFields();
                if ((bool)rbFirstTagAuthenticateTb.IsChecked)
                {
                    antenna = GetSelectedAntennaList()[0];
                }
                objReader.ParamSet("/reader/tagop/antenna", antenna);

                if ((bool)rbSelectedTagAuthenticateTb.IsChecked)
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


                TagOp op;
                try
                {
                    ushort[] Key0 = null;
                    string CurrentKeyZero = string.Empty;
                    op = new Gen2.ReadData(Gen2.Bank.USER, 0xC0, 8);
                    Key0 = (ushort[])objReader.ExecuteTagOp(op, searchSelect);

                    if (null != Key0)
                        CurrentKeyZero = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(Key0), "", " ");

                    crntKey0Value.Content = CurrentKeyZero;
                    txtbxKeyZero.IsEnabled = true;
                    txtbxKeyZero.Text = CurrentKeyZero;
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception || ex.Message.Equals("Gen2 memory overrun - bad PC"))
                    {
                        crntKey0Value.Content = "Key0 is inserted and activated";
                        txtbxKeyZero.IsEnabled = false;
                    }
                }
                try
                {
                    ushort[] Key1 = null;
                    string CurrentKeyOne = string.Empty;
                    op = new Gen2.ReadData(Gen2.Bank.USER, 0xD0, 8);
                    Key1 = (ushort[])objReader.ExecuteTagOp(op, searchSelect);

                    if (null != Key1)
                        CurrentKeyOne = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(Key1), "", " ");

                    crntKey1Value.Content = CurrentKeyOne;
                    txtbxKeyOne.IsEnabled = true;
                    txtbxKeyOne.Text = CurrentKeyOne;
                }
                catch (Exception ex)
                {
                    if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception || ex.Message.Equals("Gen2 memory overrun - bad PC"))
                    {
                        crntKey1Value.Content = "Key1 is inserted and activated";
                        txtbxKeyOne.IsEnabled = false;
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

        private void rbFirstTagAuthenticateTb_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                ResetAuthenticateTab();
            }
        }

        /// <summary>
        ///  Reset locktag tab to default values
        /// </summary>
        public void ResetAuthenticateTab()
        {
            if (null != objReader)
            {
                lblSelectFilter.Content = "Showing tag:";
                btnRead.IsEnabled = true;
                btnRead.Content = "Read";
                rbSelectedTagAuthenticateTb.IsEnabled = false;
                rbFirstTagAuthenticateTb.IsChecked = true;
                rbFirstTagAuthenticateTb.IsEnabled = true;

                lblAuthenticateTagError.Content = "";
                lblAuthenticateTagError.Visibility = System.Windows.Visibility.Collapsed;
                txtbxVerifyKeyOne.Text = "";
                txtbxVerifyKeyZero.Text = "";
                lblDataValue.Content = "";
                lblTam2DataValue.Content = "";
                crntKey0Value.Content = "";
                crntKey1Value.Content = "";
                txtbxKeyZero.Text = "";
                txtbxKeyOne.Text = "";
                txtReadStartAddr.Text = "0";
                txtReadLength.Text = "1";
                gbKeys.IsEnabled = false;
                AuthReadData.IsEnabled = false;
            }
        }

        /// <summary>
        /// Reset previous error
        /// </summary>
        public void ResetUntraceableFields()
        {
            gbKeys.IsEnabled = true;
            AuthReadData.IsEnabled = true;
        }

        /// <summary>
        /// Perform read to get the Keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            Mouse.SetCursor(Cursors.Wait);
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
            TagReadData[] tagReads = null;
            try
            {
                txtbxKeyOne.Text = txtbxKeyZero.Text = "";
                if (btnRead.Content.Equals("Read"))
                {
                    SimpleReadPlan srp = new SimpleReadPlan(((null != GetSelectedAntennaList()) ? (new int[] { GetSelectedAntennaList()[0] }) : null), TagProtocol.GEN2, null, 0);
                    objReader.ParamSet("/reader/read/plan", srp);
                    tagReads = objReader.Read(500);
                    if ((null != tagReads) && (tagReads.Length > 0))
                    {
                        currentEPC = tagReads[0].EpcString;
                        txtEpc.Text = tagReads[0].EpcString;
                        lblSelectFilter.Content = "Showing tag: EPC ID = " + tagReads[0].EpcString;
                        lblSelectFilter.Visibility = Visibility.Visible;
                        if (tagReads.Length > 1)
                        {
                            lblAuthenticateTagError.Content = "Warning: More than one tag responded";
                            lblAuthenticateTagError.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            lblAuthenticateTagError.Visibility = System.Windows.Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        txtEpc.Text = "";
                        currentEPC = string.Empty;
                        MessageBox.Show("No tags found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    rbSelectedTagAuthenticateTb.IsChecked = true;
                }
                PopulateAuthenticateData();
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
        /// Allow only hex string in the textbox
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxVerifyKey_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
        }

        /// <summary>
        /// Don't accept space for keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxVerifyKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Refresh button to generate random 10 byte challenge
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            Byte[] b = new Byte[10];
            rnd.NextBytes(b);
            lblRandomChallengeValue.Content = ByteFormat.ToHex(b, "", " ");
        }

        /// <summary>
        /// Authenticate with key0
        /// </summary>
        private void btnAuthKeyZero_Click(object sender, RoutedEventArgs e)
        {
            //Gen2.NXP.AES.Tam2Authentication tam2Auth;
            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            ushort[] Key0, Ichallenge, returnedIchallenge;
            byte[] Response, Challenge;
            lblDataValue.Content = "";
            lblTam2DataValue.Content = "";
            lblChallenge1Value.Content = "";

            switch(cbxReadDataBank.Text)
            {
                case "TID":
                    MemoryProfile = Gen2.NXP.AES.Profile.TID;
                    break;
                case "User":
                    MemoryProfile = Gen2.NXP.AES.Profile.USER;
                    break;
                case "EPC":
                    MemoryProfile = Gen2.NXP.AES.Profile.EPC;
                    break;
            }
            
            if (txtbxVerifyKeyZero.Text.Length == 32)
            {
                byte[] KeytoWrite = ByteFormat.FromHex(txtbxVerifyKeyZero.Text);
                Key0 = new ushort[KeytoWrite.Length / 2];
                Key0 = ByteConv.ToU16s(KeytoWrite);
            }
            else
            {
                MessageBox.Show("Please input valid Key0", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] randmNumber = ByteFormat.FromHex(lblRandomChallengeValue.Content.ToString().Replace(" ",""));
            Ichallenge = new ushort[randmNumber.Length / 2];
            Ichallenge = ByteConv.ToU16s(randmNumber);
            try
            {
                if ((bool)chkReadData.IsChecked && (bool)chkReadBuffer.IsChecked)
                {
                    //ReadBuffer with Tam2  
                    MessageBox.Show("Operation is not supported", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (chkReadData.IsChecked == false && chkReadBuffer.IsChecked == false)
                {
                    //Authentication with tam1
                    tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, Key0, true);
                    Gen2.NXP.AES.Authenticate auth = new Gen2.NXP.AES.Authenticate(tam1Auth);
                    Response = (byte[])objReader.ExecuteTagOp(auth, searchSelect);
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key0)).Skip(6).Take(10).ToArray();
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    returnedIchallenge = new ushort[Challenge.Length / 2];
                    returnedIchallenge = ByteConv.ToU16s(Challenge);
                    byte[] generatedIChallenge = new byte[10];
                    byte[] tempBuffer = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, tempBuffer, 0, 16);
                    byte[] receivedIChallenge = DecryptIchallenge(tempBuffer, ByteConv.ConvertFromUshortArray(Key0).ToArray());
                    Array.Copy(receivedIChallenge, 6, receivedIChallenge, 0, 10);
                    Array.Resize(ref receivedIChallenge, 10);
                    if (generatedIChallenge.SequenceEqual(receivedIChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (chkReadData.IsChecked == false && chkReadBuffer.IsChecked == true)
                {
                    //Read Buffer with Tam1
                    tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, Key0, true);
                    Gen2.ReadBuffer buffer = new Gen2.NXP.AES.ReadBuffer(0, 128, tam1Auth);
                    Response = (byte[])objReader.ExecuteTagOp(buffer, searchSelect); 
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key0)).Skip(6).Take(10).ToArray();
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    lblDataValue.Content = "";
                    byte[] generatedIChallenge = new byte[10];
                    byte[] tempBuffer = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, tempBuffer, 0, 16);
                    byte[] receivedIChallenge = DecryptIchallenge(tempBuffer, ByteConv.ConvertFromUshortArray(Key0));
                    Array.Copy(receivedIChallenge, 6, receivedIChallenge, 0, 10);
                    Array.Resize(ref receivedIChallenge, 10);                    
                    if (generatedIChallenge.SequenceEqual(receivedIChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    //Authenticate with tam2
                    MessageBox.Show("Operation is not supported", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// Authenticate with key1
        /// </summary>
        private void btnAuthKeyOne_Click(object sender, RoutedEventArgs e)
        {
            Gen2.NXP.AES.Tam2Authentication tam2Auth;
            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            ushort[] Key1, Ichallenge, returnedIchallenge;
            byte[] Response, Challenge;
            lblDataValue.Content = "";
            lblTam2DataValue.Content = "";
            lblChallenge1Value.Content = "";
            switch (cbxReadDataBank.Text)
            {
                case "TID":
                    MemoryProfile = Gen2.NXP.AES.Profile.TID;
                    break;
                case "User":
                    MemoryProfile = Gen2.NXP.AES.Profile.USER;
                    break;
                case "EPC":
                    MemoryProfile = Gen2.NXP.AES.Profile.EPC;
                    break;
            }

            if (txtbxVerifyKeyOne.Text.Length == 32)
            {
                byte[] KeytoWrite = ByteFormat.FromHex(txtbxVerifyKeyOne.Text);
                Key1 = new ushort[KeytoWrite.Length / 2];
                Key1 = ByteConv.ToU16s(KeytoWrite);
            }
            else
            {
                MessageBox.Show("Please input valid Key1", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] randmNumber = ByteFormat.FromHex(lblRandomChallengeValue.Content.ToString().Replace(" ", ""));
            Ichallenge = new ushort[randmNumber.Length / 2];
            Ichallenge = ByteConv.ToU16s(randmNumber);

            try
            {
                if ((bool)chkReadData.IsChecked && (bool)chkReadBuffer.IsChecked)
                {
                    //ReadBuffer with Tam2
                    tam2Auth = new Gen2.NXP.AES.Tam2Authentication(Gen2.NXP.AES.KeyId.KEY1, Key1, MemoryProfile,
                       (ushort)Convert.ToUInt32(txtReadStartAddr.Text), (ushort)Convert.ToUInt32(txtReadLength.Text),1,true);
                    Gen2.ReadBuffer buffer = new Gen2.NXP.AES.ReadBuffer(0, 256, tam2Auth);
                    Response = (byte[])objReader.ExecuteTagOp(buffer, searchSelect);
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key1)).Skip(6).Take(10).ToArray();                    
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    returnedIchallenge = new ushort[Challenge.Length / 2];
                    returnedIchallenge = ByteConv.ToU16s(Challenge);
                    byte[] generatedIChallenge = new byte[10];
                    byte[] CipherData = new byte[16];
                    byte[] IV = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, IV, 0, 16);
                    Array.Copy(Response, 26, CipherData, 0, 16);
                    lblDataValue.Content = DecryptCustomData(CipherData, ByteConv.ConvertFromUshortArray(Key1), (byte[])IV.Clone());
                    byte[] iChallenge = DecryptIchallenge(IV, ByteConv.ConvertFromUshortArray(Key1).ToArray());
                    Array.Copy(iChallenge, 6, iChallenge, 0, 10);
                    Array.Resize(ref iChallenge, 10);
                    if (generatedIChallenge.SequenceEqual(iChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (chkReadData.IsChecked == false && chkReadBuffer.IsChecked == false)
                {
                    //Auth with tam1
                    tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, Key1, true);
                    Gen2.NXP.AES.Authenticate auth = new Gen2.NXP.AES.Authenticate(tam1Auth);
                    Response = (byte[])objReader.ExecuteTagOp(auth, searchSelect);
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key1)).Skip(6).Take(10).ToArray();
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    returnedIchallenge = new ushort[Challenge.Length / 2];
                    returnedIchallenge = ByteConv.ToU16s(Challenge);
                    byte[] generatedIChallenge = new byte[10];
                    byte[] CipherData = new byte[16];
                    byte[] IV = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, IV, 0, 16);
                    byte[] iChallenge = DecryptIchallenge(IV, ByteConv.ConvertFromUshortArray(Key1).ToArray());
                    Array.Copy(iChallenge, 6, iChallenge, 0, 10);
                    Array.Resize(ref iChallenge, 10);
                    if (generatedIChallenge.SequenceEqual(iChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (chkReadData.IsChecked == false && chkReadBuffer.IsChecked == true)
                {
                    //Readbuffer with Tam1
                    tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, Key1, true);
                    Gen2.ReadBuffer buffer = new Gen2.NXP.AES.ReadBuffer(0, 128, tam1Auth);
                    Response = (byte[])objReader.ExecuteTagOp(buffer, searchSelect);
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key1)).Skip(6).Take(10).ToArray();
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    lblDataValue.Content = "";
                    returnedIchallenge = new ushort[Challenge.Length / 2];
                    returnedIchallenge = ByteConv.ToU16s(Challenge);
                    byte[] generatedIChallenge = new byte[10];
                    byte[] CipherData = new byte[16];
                    byte[] IV = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, IV, 0, 16);
                    byte[] iChallenge = DecryptIchallenge(IV, ByteConv.ConvertFromUshortArray(Key1).ToArray());
                    Array.Copy(iChallenge, 6, iChallenge, 0, 10);
                    Array.Resize(ref iChallenge, 10);
                    if (generatedIChallenge.SequenceEqual(iChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    //Authenticate with tam2
                    tam2Auth = new Gen2.NXP.AES.Tam2Authentication(Gen2.NXP.AES.KeyId.KEY1, Key1, MemoryProfile,
                        (ushort)Convert.ToUInt32(txtReadStartAddr.Text), (ushort)Convert.ToUInt32(txtReadLength.Text),1,true);
                    Gen2.NXP.AES.Authenticate auth = new Gen2.NXP.AES.Authenticate(tam2Auth);
                    Response = (byte[])objReader.ExecuteTagOp(auth, searchSelect);                    
                    Challenge = DecryptIchallenge(Response.Skip(0).Take(16).ToArray(), ByteConv.ConvertFromUshortArray(Key1)).Skip(6).Take(10).ToArray();
                    lblChallenge1Value.Content = ByteFormat.ToHex(Challenge, "", " ");
                    returnedIchallenge = new ushort[Challenge.Length / 2];
                    returnedIchallenge = ByteConv.ToU16s(Challenge);
                    byte[] generatedIChallenge = new byte[10];
                    byte[] CipherData = new byte[16];
                    byte[] IV = new byte[16];
                    Array.Copy(Response, 0, generatedIChallenge, 0, 10);
                    Array.Copy(Response, 10, IV, 0, 16);
                    byte[] iChallenge = DecryptIchallenge(IV, ByteConv.ConvertFromUshortArray(Key1).ToArray());
                    Array.Copy(iChallenge, 6, iChallenge, 0, 10);
                    Array.Resize(ref iChallenge, 10);
                    Array.Copy(Response, 26, CipherData, 0, 16);
                    if (Response != null)
                        lblTam2DataValue.Content = ByteFormat.ToHex(Response, "", " ");
                    lblTam2DataValue.Content = DecryptCustomData(CipherData, ByteConv.ConvertFromUshortArray(Key1), (byte[])IV.Clone());
                    if (generatedIChallenge.SequenceEqual(iChallenge))
                    {
                        MessageBox.Show("Tag Successfully Authenticated", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Tag Failed Authentication; Confirm Verification Key is Correct.", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// Decrypt the data and return ichallenge
        /// </summary>
        /// <param name="CipherData">Encrypted data</param>
        /// <param name="Key">key to decrypt encrypted data</param>
        /// <returns></returns>
        public byte[] DecryptIchallenge(byte[] CipherData, byte[] Key)
        {
            byte[] decipheredText = new byte[16];
            Aes a = new Aes(Aes.KeySize.Bits128, Key);
            a.InvCipher(CipherData, decipheredText);
            return decipheredText;
        }

        /// <summary>
        /// Decrypts custom data
        /// </summary>
        /// <param name="CipherData">Encrypted data</param>
        /// <param name="key">key to decrypt encrypted data</param>
        /// <param name="IV">Initialization vector</param>
        /// <returns></returns>
        public string DecryptCustomData(byte[] CipherData, byte[] key, byte[] IV)
        {
            byte[] decipheredText = new byte[16];
            decipheredText = DecryptIchallenge(CipherData, key);
            byte[] CustomData = new byte[16];
            for (int i = 0; i < IV.Length; i++)
            {
                CustomData[i] = (byte)(decipheredText[i] ^ IV[i]);
            }
            return ByteFormat.ToHex(CustomData,""," ");
        }


        /// <summary>
        /// Validate read data start address. Throw exception if the start address exceeds max limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReadStartAddr_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtReadStartAddr.Text == "")
            {
                MessageBox.Show("Authenticate Read Data: Starting Address to read from can't be empty.",
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtReadStartAddr.Text = "0";
            }
            try
            {
                uint valueHex = Utilities.CheckHexOrDecimal(txtReadStartAddr.Text);
                if (!(objReader is SerialReader) && (valueHex > 0xFFF))
                {
                    MessageBox.Show("Authenticate Read Data: Starting Address can't be more then 0xFFF",
                        "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtReadLength.Text = "0";
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Authenticate Read Data: Starting Word Address, " + ex.Message,
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtReadStartAddr.Text = "0";
            }
        }

        /// <summary>
        /// Validate read data length. Throw exception if the length exceeds max limit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReadLength_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtReadLength.Text == "")
            {
                MessageBox.Show("Authenticate Read Data: Length to read data can't be empty.",
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtReadLength.Text = "1";
            }
            try
            {
                uint valueHex = Utilities.CheckHexOrDecimal(txtReadLength.Text);
                if (valueHex > 15)
                {
                    MessageBox.Show("Authenticate Read Data: Length can't be more then 15",
                        "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtReadLength.Text = "1";
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Authenticate Read Data: Number of Words, " + ex.Message ,
                    "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                txtReadLength.Text = "1";
            }
        }

        /// <summary>
        /// Insert keys into tag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnInsertKeys_Click(object sender, RoutedEventArgs e)
        {
            TagOp op;
            try
            {
                if (txtbxKeyOne.Text.Replace(" ", "").Length == 32 && txtbxKeyZero.Text.Replace(" ", "").Length == 32)
                {
                    byte[] KeytoWrite = ByteFormat.FromHex(txtbxKeyZero.Text.Replace(" ", ""));
                    ushort[] Key0 = new ushort[KeytoWrite.Length / 2];
                    Key0 = ByteConv.ToU16s(KeytoWrite);
                    op = new Gen2.WriteData(Gen2.Bank.USER, 0xC0, Key0);
                    objReader.ExecuteTagOp(op, searchSelect);

                    KeytoWrite = ByteFormat.FromHex(txtbxKeyOne.Text.Replace(" ", ""));
                    ushort[] Key1 = new ushort[KeytoWrite.Length / 2];
                    Key1 = ByteConv.ToU16s(KeytoWrite);
                    op = new Gen2.WriteData(Gen2.Bank.USER, 0xD0, Key1);
                    objReader.ExecuteTagOp(op, searchSelect);
                    MessageBox.Show("Insert keys operation success", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please input valid Keys to insert ", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Activate tag keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnActivateKeys_Click(object sender, RoutedEventArgs e)
        {
            TagOp op;
            try
            {
                if (txtbxKeyOne.Text.Replace(" ", "").Length == 32 && txtbxKeyZero.Text.Replace(" ", "").Length == 32)
                {
                    op = new Gen2.WriteData(Gen2.Bank.USER, 0xC8, new ushort[] { 0xE200 });
                    objReader.ExecuteTagOp(op, searchSelect);

                    op = new Gen2.WriteData(Gen2.Bank.USER, 0xD8, new ushort[] { 0xE200 });
                    objReader.ExecuteTagOp(op, searchSelect);
                    MessageBox.Show("Activate keys operation success", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please Insert valid keys and try Activate", "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                if (ex is FAULT_GEN2_PROTOCOL_MEMORY_OVERRUN_BAD_PC_Exception)
                {
                    MessageBox.Show(ex.Message, "Universal Reader Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }

    public class IsEnabledConv : IMultiValueConverter
    {
        public object Convert(Object[] values, Type targetType,Object parameter,CultureInfo culture)
        {
            bool btnEnabled = false;
            if ((Boolean)values[0] && (Boolean)values[1])
            {
                btnEnabled = true;
            }
            else
            {
                btnEnabled = false;
            }
            return btnEnabled;
        }

        public object[] ConvertBack(Object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack should never be called");
        }
    }
    
      

}
