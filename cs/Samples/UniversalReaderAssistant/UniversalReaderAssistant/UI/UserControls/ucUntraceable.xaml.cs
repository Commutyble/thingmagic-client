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
using System.Text.RegularExpressions;

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for ucUntraceable.xaml
    /// </summary>
    public partial class ucUntraceable : UserControl
    {
        Reader objReader;
        int antenna = 0;
        uint accessPwddStartAddress = 0;
        string model = string.Empty;
        TagReadRecord selectedTagReadRecord;
        TagFilter searchSelect = null;
        Gen2.Bank selectMemBank;
        Gen2.Untraceable.EPC epc;
        Gen2.Untraceable.TID tid;
        Gen2.Untraceable.UserMemory user;
        Gen2.Untraceable.Range range = Gen2.Untraceable.Range.NORMAL;
        int epcLen;
        public ucUntraceable()
        {
            InitializeComponent();
        }

        /// <summary>
        /// LoadUntraceableMemory
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="readerModel"></param>
        public void LoadUntraceableMemory(Reader reader, string readerModel)
        {
            objReader = reader;
            model = readerModel;
        }

        /// <summary>
        /// LoadUntraceableMemory to get the access password
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="address"></param>
        /// <param name="selectedBank"></param>
        /// <param name="selectedTagRed"></param>
        /// <param name="readerModel"></param>
        public void LoadUntraceableMemory(Reader reader, uint address, Gen2.Bank selectedBank, TagReadRecord selectedTagRed, string readerModel)
        {
            objReader = reader;
            accessPwddStartAddress = address;
            model = readerModel;
            spUntraceable.IsEnabled = true;

            btnRead.Content = "Refresh";
            rbSelectedTagUntraceableTb.IsChecked = true;
            rbSelectedTagUntraceableTb.IsEnabled = true;
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
            PopulateUntraceableData();
        }

        private void rbFirstTagUntraceableTb_Checked(object sender, RoutedEventArgs e)
        {
            if (null != objReader)
            {
                ResetUntraceableTab();
            }
        }

        /// <summary>
        ///  Reset Untraceable tab to default values
        /// </summary>
        public void ResetUntraceableTab()
        {
            if (null != objReader)
            {
                lblSelectFilter.Content = "Showing tag:";
                btnRead.IsEnabled = true;
                btnRead.Content = "Read";
                rbSelectedTagUntraceableTb.IsEnabled = false;
                rbFirstTagUntraceableTb.IsChecked = true;
                rbFirstTagUntraceableTb.IsEnabled = true;
                spUntraceableFields.IsEnabled = false;

                lblUntraceableTagError.Content = "";
                txtbxAccesspaasword.Text = "";
                txtbxEpcLen.Text = "";
                ResetUntraceableActionCheckBoxes();
                lblUntraceableTagError.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Reset previous data
        /// </summary>
        public void ResetUntraceableActionCheckBoxes()
        {
            rdBtnShowEntireEpc.IsChecked = true;
            rdbtnShowAllTid.IsChecked = true;
            rdbtnShowUserMem.IsChecked = true;
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

        string currentEPC = string.Empty;
        /// <summary>
        /// Perform read to get the access password
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
                            lblUntraceableTagError.Content = "Warning: More than one tag responded";
                            lblUntraceableTagError.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            lblUntraceableTagError.Visibility = System.Windows.Visibility.Collapsed;
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
                    rbSelectedTagUntraceableTb.IsChecked = true;
                }
                PopulateUntraceableData();
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
        /// Populate access password of the tag
        /// </summary>
        private void PopulateUntraceableData()
        {
            try
            {
                Mouse.SetCursor(Cursors.Wait);
                spUntraceableFields.IsEnabled = true;
                ResetUntraceableActionCheckBoxes();
                if ((bool)rbFirstTagUntraceableTb.IsChecked)
                {
                    antenna = GetSelectedAntennaList()[0];
                }

                objReader.ParamSet("/reader/tagop/antenna", antenna);
               

                if ((bool)rbSelectedTagUntraceableTb.IsChecked)
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

                        searchSelect = new Gen2.Select(false, selectMemBank, Convert.ToUInt16(accessPwddStartAddress * 16), Convert.ToUInt16(dataLength * 8), SearchSelectData);
                    }
                }
                else
                {
                    searchSelect = new TagData(currentEPC);
                }

                //Read Reserved memory bank data
                TagOp op;
                ushort[] reservedData = null;
                txtbxAccesspaasword.Text = "";
                try
                {
                    string reservedBankData = string.Empty;
                    //Read access password
                    op = new Gen2.ReadData(Gen2.Bank.RESERVED, 2, 2);
                    reservedData = (ushort[])objReader.ExecuteTagOp(op, searchSelect);

                    if (null != reservedData)
                        reservedBankData = ByteFormat.ToHex(ByteConv.ConvertFromUshortArray(reservedData), "", " ");

                    txtbxAccesspaasword.Text = reservedBankData.Trim(' ');
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Don't accept space for access password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxAccesspaasword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Allow only hex string in the textbox
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxAccesspaasword_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int hexNumber;
            e.Handled = !int.TryParse(e.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
        }

        /// <summary>
        /// Show entire EPC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnShowEntireEpc_Checked(object sender, RoutedEventArgs e)
        {
            epc = Gen2.Untraceable.EPC.SHOW;
        }

        /// <summary>
        /// Hide EPC of specified length
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnShowSpecificEpc_Checked(object sender, RoutedEventArgs e)
        {
            txtbxEpcLen.IsEnabled = true;
            epc = Gen2.Untraceable.EPC.HIDE;
        }

        /// <summary>
        /// Get the selected TID option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grpTid_Checked(object sender, RoutedEventArgs e)
        {
            var selectedOption = sender as RadioButton;
            if (selectedOption.Content.Equals("Show All"))
            {
                tid = Gen2.Untraceable.TID.HIDE_NONE;
            }
            else if (selectedOption.Content.Equals("Show Tag Info Only"))
            {
                tid = Gen2.Untraceable.TID.HIDE_SOME;
            }
            else
            {
                tid = Gen2.Untraceable.TID.HIDE_ALL;
            }
        }

        /// <summary>
        /// Get the selected USER memory option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grpUser_Checked(object sender, RoutedEventArgs e)
        {
            var selectedOption = sender as RadioButton;
            if (selectedOption.Content.Equals("Show All"))
            {
                user = Gen2.Untraceable.UserMemory.SHOW;
            }
            else
            {
                user = Gen2.Untraceable.UserMemory.HIDE;
            }
        }

        /// <summary>
        /// Write Untraceable operation to tag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btmWriteToTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (((bool)rdBtnShowSpecificEpc.IsChecked) && (txtbxEpcLen.Text == ""))
                {
                    MessageBox.Show("Please Enter length of EPC to Show", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    epcLen=6;
                }
                antenna = ((null != GetSelectedAntennaList()) ? (GetSelectedAntennaList()[0]) : antenna);
                objReader.ParamSet("/reader/tagop/antenna", antenna);
                if ((bool)rdBtnShowSpecificEpc.IsChecked)
                {   
                    epcLen = Convert.ToInt32(txtbxEpcLen.Text);
                }
                Gen2.Password accessPassWord = new Gen2.Password(ByteConv.ToU32(ByteFormat.FromHex(txtbxAccesspaasword.Text.Replace(" ","")),0));
                Gen2.Untraceable tagOp = new Gen2.NXP.AES.Untraceable(epc, epcLen, tid, user, range, accessPassWord.Value);
                objReader.ExecuteTagOp(tagOp, searchSelect);
                MessageBox.Show("Write operation is successful", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Information); 
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Text box to accept only numbers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtbxEpcLen_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Disable the text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rdBtnShowSpecificEpc_Unchecked(object sender, RoutedEventArgs e)
        {
            txtbxEpcLen.IsEnabled = false;
        }
    }
}
