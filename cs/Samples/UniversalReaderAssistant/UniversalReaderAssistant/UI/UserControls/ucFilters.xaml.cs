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

namespace ThingMagic.URA2
{
    /// <summary>
    /// Interaction logic for ucFilters.xaml
    /// </summary>
    public partial class ucFilters : UserControl
    {
        public ucFilters()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Validate filter data in filter section 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtFilterData_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                if (txtFilterData.Text == "")
                {
                    MessageBox.Show("Filter: Filter data can't be empty.", "Universal Reader Assistant Message",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtFilterData.Text = "0";
                }
                if (txtFilterData.Text.EndsWith("0x") || txtFilterData.Text.EndsWith("0X"))
                {
                    throw new Exception("Not a valid hex number");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Filter: Filter data: " + ex.Message + " If hex, prefix with 0x", "Universal Reader Assistant Message",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtFilterData.Text = "0";
            }
        }


        /// <summary>
        /// Change filter start address based on the memory selected in the 
        /// filter memory bank combo-box selection 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxFilterMemBank_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (null != txtFilterStartAddr)
            {
                if (null != cbxFilterMemBank.SelectedItem)
                {
                    lblFilterStartAddr.Content = "Start :";
                    txtFilterEPCLength.Visibility = Visibility.Collapsed;
                    string selectedItemtext = ((ComboBoxItem)cbxFilterMemBank.SelectedItem).Content.ToString();
                    if (selectedItemtext.ToString() == "EPC ID")
                        txtFilterStartAddr.Text = "32";
                    else if (selectedItemtext.ToString() == "EPC Truncate")
                        txtFilterStartAddr.Text = "16";
                    else if (selectedItemtext.ToString() == "EPC Length")
                    {
                        lblFilterStartAddr.Content = "Length :";
                        txtFilterEPCLength.Text = "112";
                        txtFilterStartAddr.Text = "0";
                        txtFilterEPCLength.Visibility = Visibility.Visible;
                    }
                    else
                        txtFilterStartAddr.Text = "0";
                }
            }
        }

        private void txtFilterStartAddr_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            string controlData = "";
            string controlName = "";
            TextBox tbx = sender as TextBox;

            controlName = tbx.Name;
            controlData = tbx.Text;

            if (controlData == "")
            {
                if (controlName == "txtFilterStartAddr")
                    MessageBox.Show("Filter: Starting BIT Address to apply Filter from can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (controlName == "txtFilterEPCLength")
                    MessageBox.Show("Filter: Length to apply Filter from can't be empty.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);

                if (null != cbxFilterMemBank.SelectedItem)
                {
                    lblFilterStartAddr.Content = "Start :";
                    txtFilterEPCLength.Visibility = Visibility.Collapsed;
                    string selectedItemtext = ((ComboBoxItem)cbxFilterMemBank.SelectedItem).Content.ToString();
                    if (selectedItemtext.ToString() == "EPC ID")
                        txtFilterStartAddr.Text = "32";
                    else if (selectedItemtext.ToString() == "EPC Truncate")
                        txtFilterStartAddr.Text = "16";
                    else if (selectedItemtext.ToString() == "EPC Length")
                    {
                        lblFilterStartAddr.Content = "Length :";
                        txtFilterEPCLength.Text = "128";
                        txtFilterStartAddr.Text = "0";
                        txtFilterEPCLength.Visibility = Visibility.Visible;
                    }
                    else
                        txtFilterStartAddr.Text = "0";
                }
            }
            try
            {
                ushort dataLength = Convert.ToUInt16(Utilities.CheckHexOrDecimal(controlData));
                if (controlName == "txtFilterEPCLength" && (dataLength < 16 || dataLength > 512))
                {
                    MessageBox.Show("Valid range for EPC length is between 16 and 512 bits.", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtFilterEPCLength.Text = "112";
                }
            }
            catch (Exception ex)
            {
                if (controlName == "txtFilterStartAddr")
                    MessageBox.Show("Filter: Starting BIT Address to apply Filter from: " + ex.Message + " If hex, prefix with 0x", "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (controlName == "txtFilterEPCLength")
                    MessageBox.Show("Filter: Length to apply Filter from is incorrect : " + ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show(ex.Message, "Universal Reader Assistant Message", MessageBoxButton.OK, MessageBoxImage.Error);

                //if (null != cbxfiltermembank.selecteditem)
                //{
                //    lblfilterstartaddr.content = "start :";
                //    txtfilterepclength.visibility = visibility.collapsed;
                //    string selecteditemtext = cbxfiltermembank.selecteditem.tostring();
                //    if (selecteditemtext.tostring() == "epc id")
                //        txtfilterstartaddr.text = "32";
                //    else if (selecteditemtext.tostring() == "epc truncate")
                //        txtfilterstartaddr.text = "16";
                //    else if (selecteditemtext.tostring() == "epc length")
                //    {
                //        lblfilterstartaddr.content = "length :";
                //        txtfilterepclength.text = "128";
                //        txtfilterstartaddr.text = "0";
                //        txtfilterepclength.visibility = visibility.visible;
                //    }
                //    else
                //        txtfilterstartaddr.text = "0";
                //}
            }
        }

        private void txtFilterData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Utilities.HexStringChecker(e.Text);
            base.OnPreviewTextInput(e);
        }




        //private void LoadAfterConnectConfigurations()

        // Apply filter
        //if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter"].ToLower().Equals("true"))
        //{
        //    chkApplyFilter.IsChecked = true;
        //    if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter/FilterMemBank"))
        //    {
        //        cbxFilterMemBank.SelectedIndex = GetIndexOf(cbxFilterMemBank,
        //            loadSaveConfig.Properties["/application/readwriteOption/applyFilter/FilterMemBank"], "Filter MemBank");
        //    }
        //    if (loadSaveConfig.Properties.ContainsKey("/application/readwriteOption/applyFilter/FilterStartAddress"))
        //    {
        //        string tempValueFilterStartAdd = txtFilterStartAddr.Text;
        //        try
        //        {
        //            Utilities.CheckHexOrDecimal(loadSaveConfig.Properties[
        //                "/application/readwriteOption/applyFilter/FilterStartAddress"]);
        //            txtFilterStartAddr.Text = loadSaveConfig.Properties[
        //                "/application/readwriteOption/applyFilter/FilterStartAddress"];
        //            txtFilterStartAddr.Focus();
        //        }
        //        catch (Exception)
        //        {
        //            NotifyLoadSaveConfigErrorMessage("Saved filter start address "
        //                + "[/application/readwriteOption/applyFilter/FilterStartAddress]"
        //                + " value [" + loadSaveConfig.Properties["/application/readwriteOption/applyFilter/FilterStartAddress"] + "] "
        //                + "invalid. Please enter valid dec or hex with prefix as 0x. URA sets filter start address to previous set value "
        //                + " [" + tempValueFilterStartAdd + "] or change to the supported value and reload the configuration");
        //            txtFilterStartAddr.Text = tempValueFilterStartAdd;
        //        }
        //    }
        //    if (ValidateFilterDataFromConfig(loadSaveConfig.Properties[
        //        "/application/readwriteOption/applyFilter/FilterData"]))
        //    {
        //        txtFilterData.Text = loadSaveConfig.Properties[
        //            "/application/readwriteOption/applyFilter/FilterData"];
        //        txtFilterData.Focus();
        //    }
        //    else
        //    {
        //        string tempValueFilterData = txtFilterData.Text;
        //        NotifyLoadSaveConfigErrorMessage("Saved Filter data [/application/readwriteOption/applyFilter/FilterData] value ["
        //               + loadSaveConfig.Properties["/application/readwriteOption/applyFilter/FilterData"] + "] invalid. Please enter valid"
        //               + " hex number. URA sets filter data to previous set value [" + tempValueFilterData + "] or change to the "
        //               + " supported value and reload the configuration");
        //        txtFilterData.Text = tempValueFilterData;
        //    }

        //    // Invert filter
        //    if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter/InvertFilter"].ToLower().Equals("true"))
        //    {
        //        chkFilterInvert.IsChecked = true;
        //    }
        //    else if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter/InvertFilter"].ToLower().Equals("false"))
        //    {
        //        chkFilterInvert.IsChecked = false;
        //    }
        //    else
        //    {
        //        // Notify the error message to the user
        //        NotifyInvalidLoadConfigOption("/application/readwriteOption/applyFilter/InvertFilter");
        //    }
        //}
        //else if (loadSaveConfig.Properties["/application/readwriteOption/applyFilter"].ToLower().Equals("false"))
        //{
        //    chkApplyFilter.IsChecked = false;
        //}
        //else
        //{
        //    // Notify the error message to the user
        //    NotifyInvalidLoadConfigOption("/application/readwriteOption/applyFilter");
        //}

        // Performance tuning
        // Save Performance tuning settings



        // private void SetReadPlans()
        //if ((bool)chkApplyFilter.IsChecked)
        //{

        //    switch (cbxFilterMemBank.Text)
        //    {
        //        case "EPC":
        //            selectMemBank = Gen2.Bank.EPC;
        //            break;
        //        case "TID":
        //            selectMemBank = Gen2.Bank.TID;
        //            break;
        //        case "User":
        //            selectMemBank = Gen2.Bank.USER;
        //            break;
        //        case "EPC Truncate":
        //            selectMemBank = Gen2.Bank.GEN2EPCTRUNCATE;
        //            break;
        //        case "EPC Length":
        //            selectMemBank = Gen2.Bank.GEN2EPCLENGTHFILTER;
        //            break;
        //    };

        //    int discard;
        //    byte[] SearchSelectData = Utilities.GetBytes(
        //        Utilities.RemoveHexstringPrefix(txtFilterData.Text), out discard);
        //    // Enter inside if condition, only if filter length is in odd nibbles(hex characters)
        //    if (discard == 1 && SearchSelectData.Length == 0)
        //    {
        //        // If only one hex character(one nibble) is specified as a filter.
        //        // For ex: 0xa, convert into byte array
        //        byte objByte = (byte)(Utilities.HexToByte(
        //            Utilities.RemoveHexstringPrefix(txtFilterData.Text).TrimEnd()) << 4);
        //        SearchSelectData = new byte[] { objByte };
        //    }
        //    else if (discard == 1)
        //    {
        //        // If filter length is in odd nibbles. For ex: 0xabc , 0xabcde, 0xabcdefg
        //        // after converting to byte array we get 0xab, 0xc0 if the specified filter is 0xabc
        //        //Adding omitted character to byte array
        //        Array.Resize(ref SearchSelectData, SearchSelectData.Length + 1);
        //        byte objByte = (byte)(((Utilities.HexToByte(
        //            Utilities.RemoveHexstringPrefix(txtFilterData.Text).Substring(
        //            Utilities.RemoveHexstringPrefix(
        //            txtFilterData.Text).Length - 1).TrimEnd())) << 4));
        //        Array.Copy(new object[] { objByte }, 0, SearchSelectData, SearchSelectData.Length - 1, 1);
        //    }

        //    UInt16 dataLength;
        //    if (txtFilterData.Text != "")
        //        dataLength = Convert.ToUInt16(Utilities.RemoveHexstringPrefix(txtFilterData.Text).Length * 4);//calculate the length in the form of nibbles
        //    else
        //        dataLength = 0;

        //    if (cbxFilterMemBank.Text == "EPC Length")
        //    {
        //        dataLength = Convert.ToUInt16(Utilities.CheckHexOrDecimal(txtFilterEPCLength.Text));
        //        searchSelect = new Gen2.Select(false, Gen2.Bank.GEN2EPCLENGTHFILTER, 16, dataLength, new byte[] { 0x30, 0x00 });
        //    }
        //    else
        //    {
        //        searchSelect = new Gen2.Select((bool)chkFilterInvert.IsChecked, selectMemBank, Convert.ToUInt32(Utilities.CheckHexOrDecimal(txtFilterStartAddr.Text)), dataLength, SearchSelectData);
        //    }
        //}
        //else
        //{
        //    searchSelect = null;
        //}
        //END Setup Select Filter settings if option checked



        //    private Dictionary<string, string> GetParametersToSave()
        // Apply filter
        //if ((bool)chkApplyFilter.IsChecked)
        //{
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter", "true");

        //    // MemBank
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterMemBank",
        //        cbxFilterMemBank.Text);

        //    // Filter start address
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterStartAddress",
        //        txtFilterStartAddr.Text);

        //    // Filter data
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterData",
        //        txtFilterData.Text);
        //}
        //else
        //{
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter", "false");
        //    // MemBank
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterMemBank",
        //        cbxFilterMemBank.Text);

        //    // Filter start address
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterStartAddress",
        //        txtFilterStartAddr.Text);

        //    // Filter data
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/FilterData",
        //        txtFilterData.Text);
        //}

        //if ((bool)chkFilterInvert.IsChecked)
        //{
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/InvertFilter", "true");
        //}
        //else
        //{
        //    saveConfigurationList.Add("/application/readwriteOption/applyFilter/InvertFilter", "false");
        //}

    }
}
