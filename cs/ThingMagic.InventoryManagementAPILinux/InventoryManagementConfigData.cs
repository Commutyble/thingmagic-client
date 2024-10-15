/*
 * Copyright (c) 2022 Novanta, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
/// InventoryManagementConfigData.cs 
/// Sample Inventory Management Configuration related Data API Utilities
/// to meet the expectation of the Inventory Management Application 
/// Author : Rajakumar T. Kulkarni
/// Dated : 02-Aug-2022

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ThingMagic;

namespace ThingMagic.InventoryManagementAPI
{
    /// <summary>
    /// This class is tagInventoryConfigInfo struct, used to store 
    /// all config related data here
    /// </summary>
    public class InventoryManagementConfigData
    {
        internal int _asyncOnTimeFromConfig=0;
        internal int _asyncOffTimeFromConfig=0;
        internal int _missedTagWaitCycles=0;
        internal int _gpiTrigTypeNum=0;
        internal int _gpoTrigTypeNum=0;
        internal string _printTagListFlag=null;
        internal int _printTagTimerInterval=0;
        internal string _readerCOMPort=null;
        internal string _autoReadType=null;
        internal string _modelNumber=null;
        internal string _serialNumber=null;
        internal string _hardwareNumber=null;
        internal string _softwareNumber=null;
        internal bool _addTagAlertFlag=false;
        internal bool _doorOpenAlertFlag=false;
        internal bool _doorCloseAlertFlag=false;
        internal bool _doorOpenGpoHighFlag = false;
        internal bool _doorOpenGpoLowFlag = false;
        internal bool _doorCloseGpoHighFlag = false;
        internal bool _doorCloseGpoLowFlag = false;
        internal bool _missedTagAlertFlag=false;
        internal bool _missedTagExceptionFlag=false;
        internal bool _deleteTagAlertFlag=false;
        internal bool _duplicateTagExceptionFlag=false;
        internal bool _firstTimeTotalMissedTagCountFlag=true;
        internal string _doorStatus="doorOpen";
        internal IDictionary<string, string> _appsettings;
        internal IDictionary<string, string> _alerteventsettings;
        internal IDictionary<string, string> _exceptioneventsettings;
        internal IDictionary<string, string> _writeablereaderparams;
        internal int[] _antennaList = null;
        internal SimpleReadPlan _srp;
        internal bool _enableAutoRead = true;
        internal IDictionary<string, string> _saveInventoryConfigData;

        /// asyncOnTime
        public int asyncOnTimeFromConfig
        {
            get { return _asyncOnTimeFromConfig; }
            set { _asyncOnTimeFromConfig = value; }
        }

        /// asyncOffTime
        public int asyncOffTimeFromConfig
        {
            get { return _asyncOffTimeFromConfig; }
            set { _asyncOffTimeFromConfig = value; }
        }

        /// minimum wait time cycles to declare missed tag report message
        public int missedTagWaitCycles
        {
            get { return _missedTagWaitCycles; }
            set { _missedTagWaitCycles = value; }
        }

        /// gpipin number saved in gpiTrigTypeNum
        public int gpiTrigTypeNum
        {
            get { return _gpiTrigTypeNum; }
            set { _gpiTrigTypeNum = value; }
        }

        /// gpipin number saved in gpoTrigTypeNum
        public int gpoTrigTypeNum
        {
            get { return _gpoTrigTypeNum; }
            set { _gpoTrigTypeNum = value; }
        }

        /// flag used to print the tag list
        public string printTagListFlag
        {
            get { return _printTagListFlag; }
            set { _printTagListFlag = value; }
        }

        /// TimerInterval used to print the tag list
        public int printTagTimerInterval
        {
            get { return _printTagTimerInterval; }
            set { _printTagTimerInterval = value; }
        }

        /// uri comport is saved here..
        public string readerCOMPort
        {
            get { return _readerCOMPort; }
            set { _readerCOMPort = value; }
        }

        /// auto readtype is defined here..
        public string autoReadType
        {
            get { return _autoReadType; }
            set { _autoReadType = value; }
        }

        /// save model information of the configured target platform
        public string modelNumber
        {
            get { return _modelNumber; }
            set { _modelNumber = value; }
        }

        /// save number information of the configured target platform
        public string serialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; }
        }

        /// save hardware information of the configured target platform
        public string hardwareNumber
        {
            get { return _hardwareNumber; }
            set { _hardwareNumber = value; }
        }

        /// save software information of the configured target platform
        public string softwareNumber
        {
            get { return _softwareNumber; }
            set { _softwareNumber = value; }
        }

        /// flag used to alert on any new added tag
        public bool addTagAlertFlag
        {
            get { return _addTagAlertFlag; }
            set { _addTagAlertFlag = value; }
        }

        /// flag used to alert on door open detected
        public bool doorOpenAlertFlag
        {
            get { return _doorOpenAlertFlag; }
            set { _doorOpenAlertFlag = value; }
        }

        /// flag used on door open gpo high detected
        public bool doorOpenGpoHighFlag
        {
            get { return _doorOpenGpoHighFlag; }
            set { _doorOpenGpoHighFlag = value; }
        }

        /// flag used on door open gpo low detected
        public bool doorOpenGpoLowFlag
        {
            get { return _doorOpenGpoLowFlag; }
            set { _doorOpenGpoLowFlag = value; }
        }

        /// flag used to alert on door close detected
        public bool doorCloseAlertFlag
        {
            get { return _doorCloseAlertFlag; }
            set { _doorCloseAlertFlag = value; }
        }

        /// flag used on door close gpo high detected
        public bool doorCloseGpoHighFlag
        {
            get { return _doorCloseGpoHighFlag; }
            set { _doorCloseGpoHighFlag = value; }
        }

        /// flag used on door close gpo low detected
        public bool doorCloseGpoLowFlag
        {
            get { return _doorCloseGpoLowFlag; }
            set { _doorCloseGpoLowFlag = value; }
        }

        /// flag used to alert on any missed tag detected
        public bool missedTagAlertFlag
        {
            get { return _missedTagAlertFlag; }
            set { _missedTagAlertFlag = value; }
        }

        /// flag used to alert on any missed tag detected
        public bool missedTagExceptionFlag
        {
            get { return _missedTagExceptionFlag; }
            set { _missedTagExceptionFlag = value; }
        }

        /// flag used to alert on any delete tag detected
        public bool deleteTagAlertFlag
        {
            get { return _deleteTagAlertFlag; }
            set { _deleteTagAlertFlag = value; }
        }

        /// flag used to alert on any duplicate tag detected
        public bool duplicateTagExceptionFlag
        {
            get { return _duplicateTagExceptionFlag; }
            set { _duplicateTagExceptionFlag = value; }
        }

        /// flag used to alert on first time totalMissedTagCount detected
        public bool firstTimeTotalMissedTagCountFlag
        {
            get { return _firstTimeTotalMissedTagCountFlag; }
            set { _firstTimeTotalMissedTagCountFlag = value; }
        }

        /// flag used to alert on door status detected
        public string doorStatus
        {
            get { return _doorStatus; }
            set { _doorStatus = value; }
        }

        /// dictionary to store appsettings
        private IDictionary<string, string> AppSettings
        {
            get { return _appsettings; }
            set { _appsettings = value; }
        }

        /// dictionary to store alerteventsettings
        private IDictionary<string, string> AlertEventSettings
        {
            get { return _alerteventsettings; }
            set { _alerteventsettings = value; }
        }

        /// dictionary to store exceptioneventsettings
        private IDictionary<string, string> ExceptionEventSettings
        {
            get { return _exceptioneventsettings; }
            set { _exceptioneventsettings = value; }
        }

        /// dictionary to store writeablereaderparams (like LOADCONFIG/SAVECONFIG)
        private IDictionary<string, string> WriteableReaderParams
        {
            get { return _writeablereaderparams; }
            set { _writeablereaderparams = value; }
        }

        /// antenna list for the reader 
        private int[] antennaList
        {
            get { return _antennaList; }
            set { _antennaList = value; }
        }

        /// simple read plan for the reader 
        private SimpleReadPlan srp
        {
            get { return _srp; }
            set { _srp = value; }
        }

        /// defines the default autonomousmode enabled always...
        private bool enableAutoRead
        {
            get { return _enableAutoRead; }
            set { _enableAutoRead = value; }
        }

        /// SaveInventoryConfigData here...
        private IDictionary<string, string> SaveInventoryConfigData
        {
            get { return _saveInventoryConfigData; }
           set { _saveInventoryConfigData = value; }
        }

        #region SaveConfiguration
        /// <summary>
        /// Save the Inventory Management configuration data to file at folder Linux/outInventoryManagement.config
        /// </summary>
        /// <param name="filePath">Configuration file path</param>
        /// <param name="saveConfigurationList">saveConfig List</param>
        private void SaveInventoryManagementConfiguration(String filePath, List<KeyValuePair<string, string>> saveConfigurationList)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.AutoFlush = true;
                foreach (KeyValuePair<string, string> item in saveConfigurationList)
                    {
                        writer.WriteLine(item.Key + "=" + item.Value);
                    }
                writer.Close();
            }
        }
        #endregion

        /// <summary>
        /// Restores reader SaveConfig and Inventory Management Configuration settings 
        /// related items to the defined out file here...\n
        /// private void restoreInventoryManagementSaveConfig(Reader r)\n
        /// This function process the application config to gets reader SaveConfig 
        /// items and also adds the application parameters back restored to the file 
        /// (Linux/outInventoryTracker.config\n
        /// </summary>
        /// <param name="r">Reader parameter</param>
        private void restoreInventoryManagementSaveConfig(Reader r)
        {
            /// writeablereaderparams config data are processed here..
            string filePath = Directory.GetCurrentDirectory();
            string tempFilePath;
            foreach (KeyValuePair<string, string> kvp in WriteableReaderParams)
                {
                    string writeableReaderParamsKeys = Convert.ToString(kvp.Key);
                    string[] writeableReaderParamsKeysList = writeableReaderParamsKeys.Split('/');
                    if (writeableReaderParamsKeysList[3] == "SAVECONFIG") /// actual kvp.Key comparison
                    {
                        tempFilePath = Path.Combine(filePath, "tempSave");
                        r.SaveConfig(tempFilePath);
                        List<KeyValuePair<string, string>> saveReaderConfig = new List<KeyValuePair<string, string>>();
                        saveReaderConfig = GetProperties(tempFilePath);
                        File.Delete(tempFilePath);
                        List<KeyValuePair<string, string>> SaveInventoryConfigData = new List<KeyValuePair<string, string>>();
                        /// Append AppSettings
                        SaveInventoryConfigData.AddRange(AppSettings);
                        /// Append AlertEventSettings
                        SaveInventoryConfigData.AddRange(AlertEventSettings);
                        /// Append ExceptionEventSettings
                        SaveInventoryConfigData.AddRange(ExceptionEventSettings);
                        /// Append WriteableReaderParams
                        SaveInventoryConfigData.AddRange(WriteableReaderParams);
                        /// Append saveReaderConfig
                        SaveInventoryConfigData.AddRange(saveReaderConfig);
                        tempFilePath = WriteableReaderParams[kvp.Key];
                        tempFilePath = Path.Combine(filePath, tempFilePath);
                        Console.WriteLine(kvp.Key + ": SaveConfig Path@{0}", tempFilePath);
                        SaveInventoryManagementConfiguration(tempFilePath, SaveInventoryConfigData);
                        Console.WriteLine("SaveConfig completed Sucessfully!!");
                    }
                }
            }

            /// <summary>
            /// This method initialize Inventory Management Configuration 
            /// Data such as configSettings etc
            /// public void InventoryManagementInitConfigData()
            /// </summary>
            public void InventoryManagementInitConfigData()
            {
                /// This will parse Inventory Management Application 
                /// Default Config File...
                parseInventoryManagementDefaultConfigFile();
                /// apply all Config parameters here...
                applyConfigSettings();
            }

            /// <summary>
            /// This method initialize Inventory Management 
            /// Remaining Configuration Data such as HardwareInfo, 
            /// applyReaderLoadConfig, readerInitialization, SaveConfig etc
            /// public void InventoryManagementInitRemainingConfigData(Reader r)
            /// </summary>
            /// <param name="r">Reader parameter</param>
            public void InventoryManagementInitRemainingConfigData(Reader r)
            {
                /// get Hardware Information such as model, serial, hardware, software etc from reader here...
                getHardwareInfo(r);
                /// apply Reader LoadConfig related Items here...
                applyReaderLoadConfig(r);
                /// Basic Reader Initialization  
                readerInitialization(r);
                /// restores Reader SaveConfig & Inventory er Settings related Items here...
                /// Also ensure to add the application parameters restored
                /// back to the out file. 
                restoreInventoryManagementSaveConfig(r);
            }

            /// <summary>
            /// This is handle to Inventory Management Alert Event Args class to manage all alert events
            /// </summary>
            private static event EventHandler AlertListener;

            /// <summary>
            /// This method invokes alert on Inventory Management Alert Event message...\n
            /// public void OnInventoryManagementAlert(string alertType, string alertValue) \n
            /// </summary>
            /// <param name="alertType">alert type.</param>
            /// <param name="alertValue">alert value.</param>
            public void OnInventoryManagementAlert(string alertType, string alertValue)
            {
                InventoryManagementAlertEventArgs args = new InventoryManagementAlertEventArgs(alertType, alertValue);
                AlertListener?.Invoke(this, args);
            }

            /// <summary>
            /// This is handle to Inventory Management Exception Event Args class to manage all exception events
            /// </summary>
            private static event EventHandler ExceptionListener;

            /// <summary>
            /// This method invokes exception handling on Inventory Management Exception Event message...\n
            /// public void OnInventoryManagementAlert(string alertType, string alertValue) \n
            /// </summary>
            /// <param name="alertType">alert type.</param>
            /// <param name="alertValue">alert value.</param>
            public void OnInventoryManagementException(string alertType, string alertValue)
            {
                InventoryManagementExceptionEventArgs args = new InventoryManagementExceptionEventArgs(alertType, alertValue);
                ExceptionListener?.Invoke(this, args);
            }

            #region ParseAntennaList
            /// <summary>
            /// This parse Antenna List of reader...\n
            /// private int[] ParseAntennaList(IList<string> args, int argPosition)\n
            /// This function parse reader AntennaList items \n
            /// </summary>
            /// <param name="args">Antennas list</param>
            /// <param name="argPosition">Antennas position</param>
            private int[] ParseAntennaList(IList<string> args, int argPosition)
            {
                int[] antennaList = null;
                try
                {
                    string str = args[argPosition + 1];
                    antennaList = Array.ConvertAll<string, int>(str.Split(','), int.Parse);
                    if (antennaList.Length == 0)
                    {
                        antennaList = null;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("Missing argument after args[{0:d}] \"{1}\"", argPosition, args[argPosition]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}\"{1}\"", ex.Message, args[argPosition + 1]);
                }
                return antennaList;
            }
            #endregion

            #region GetProperties
            /// <summary>
            /// Read the properties from TestConfiguration.properties file
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            private List<KeyValuePair<string, string>> GetProperties(string path)
            {
                List<KeyValuePair<string, string>> Properties = new List<KeyValuePair<string, string>>();
                using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        if ((!string.IsNullOrEmpty(s)) &&
                            (!s.StartsWith(";")) &&
                            (!s.StartsWith("#")) &&
                            (!s.StartsWith("'")) &&
                            (!s.StartsWith("//")) &&
                            (!s.StartsWith("*")) &&
                            (s.IndexOf('=') != -1))
                        {
                            int index = s.IndexOf('=');
                            string keyConfig = s.Substring(0, index).Trim();
                            string valueConfig = s.Substring(index + 1).Trim();
                            if ((valueConfig.StartsWith("\"") 
                                && valueConfig.EndsWith("\"")) 
                                || (valueConfig.StartsWith("'") 
                                && valueConfig.EndsWith("'")))
                            {
                                valueConfig = valueConfig.Substring(1, valueConfig.Length - 2);
                            }
                            Properties.Add(new KeyValuePair<string, string>(keyConfig, valueConfig));
                        }
                    }
                    bool flag1 = true, flag2 = true;
                    for (int i = 0; i < Properties.Count; i++)
                    {
                        if (Properties[i].Key == "/reader/radio/portReadPowerList" && flag1)
                        {
                            KeyValuePair<string, string> keyValuePair = Properties[i];
                            Properties.RemoveAt(i);
                            Properties.Add(keyValuePair);
                            i--;
                            flag1 = false;
                        }
                        else if (Properties[i].Key == "/reader/radio/portWritePowerList" && flag2)
                        {
                            KeyValuePair<string, string> keyValuePair = Properties[i];
                            Properties.RemoveAt(i);
                            Properties.Add(keyValuePair);
                            i--;
                            flag2 = false;
                        }
                    }
                }
                return Properties;
            }
            #endregion

            /// <summary>
            /// This method will parse Inventory Management Application Default Config File...\n
            /// private void parseInventoryManagementDefaultConfigFile()\n
            /// </summary>
            private void parseInventoryManagementDefaultConfigFile()
            {
                AppSettings = new Dictionary<string, string>();
                AlertEventSettings = new Dictionary<string,string>();
                ExceptionEventSettings = new Dictionary<string,string>();
                WriteableReaderParams = new Dictionary<string,string>();
                string filePath = Directory.GetCurrentDirectory();
                string tempFilePathORG = "Linux/InventoryManagementAppDefault.config";
                string tempFilePath = Path.Combine(filePath, tempFilePathORG);
                if (File.Exists(tempFilePath))
                {
                    List<KeyValuePair<string, string>> loadConfigProperties = new List<KeyValuePair<string, string>>();
                    loadConfigProperties = GetProperties(tempFilePath);
                    foreach (KeyValuePair<string, string> item in loadConfigProperties)
                    {
                        if (!item.Key.StartsWith("/reader"))
                        {
                            string tmpKeys = Convert.ToString(item.Key);
                            string[] tmpKeysList = tmpKeys.Split('/');
                            switch(tmpKeysList[2])
                            {
                                case "appsettings":
                                    AppSettings.Add(item.Key, item.Value);
                                    break;
                                case "alerteventsettings":
                                    AlertEventSettings.Add(item.Key, item.Value);
                                    break;
                                case "exceptioneventsettings":
                                    ExceptionEventSettings.Add(item.Key, item.Value);
                                    break;
                                case "writeablereaderparams":
                                    WriteableReaderParams.Add(item.Key, item.Value);
                                    break;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// This method handles ExceptionEventSettings Config initialization...\n
            /// Also, handles to apply corresponding action determined from config file \n
            /// private void applyExceptionEventSettings()\n
            /// This method handles the config exceptioneventsettings parameters and \n 
            /// also handles its corresponding actions: \n
            /// onmissedtag \n
            ///     Action : missedtagexception
            /// onduplicatetag \n
            ///     Action : duplicatetagexception
            /// </summary>
            private void applyExceptionSettings()
            {
                /// exceptioneventsettings initialization handled here..
                missedTagExceptionFlag = false;
                duplicateTagExceptionFlag = false;
                foreach (KeyValuePair<string, string> kvp in ExceptionEventSettings)
                {
                    string exceptionEventSettingKeys = Convert.ToString(kvp.Key);
                    string[] exceptionEventSettingKeysList = exceptionEventSettingKeys.Split('/');
                    string exceptionEventSettingValues = Convert.ToString(ExceptionEventSettings[kvp.Key]);
                    string[] exceptionEventSettingValuesList = exceptionEventSettingValues.Split('/');
                    if (exceptionEventSettingValuesList[0] == null)
                        exceptionEventSettingValuesList[0] = exceptionEventSettingValues;
                    switch(exceptionEventSettingKeysList[3]) /// actual kvp.Key comparison
                    {
                        case "onmissedtag":
                            foreach(string ele in exceptionEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "missedtagexception":
                                        missedTagExceptionFlag = true;
                                        break;
                                }
                            }
                            break;
                        case "onduplicatetag":
                            foreach(string ele in exceptionEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "duplicatetagexception":
                                        duplicateTagExceptionFlag = true;
                                        break;
                                }
                            }
                            break;
                    }
                }
            }

            /// <summary>
            /// This method handles AlertEventSettings Config initialization...\n
            /// Also, handles to applying corresponding action determined from config file \n
            /// private void applyAlertEventSettings()\n
            /// This method handles the config alerteventsettings parameters and \n 
            /// also handles its corresponding actions: \n
            /// onaddedtag \n
            ///     Action : addtagalert
            /// onmissedtag \n
            ///     Action : missedtagalert
            /// ondeletetag \n
            ///     Action : deletetagalert
            /// ondooropen \n
            ///     Action : dooropenalert, dooropengpohigh/dooropengpolow
            /// ondoorclose \n
            ///     Action : doorclosealert, doorclosegpolow/doorclosegpohigh
            /// </summary>
            private void applyAlertEventSettings()
            {
                /// alerteventsettings config initialization data are processed here..
                addTagAlertFlag = false;
                deleteTagAlertFlag = false;
                doorOpenAlertFlag = false;
                doorCloseAlertFlag = false;
                foreach (KeyValuePair<string, string> kvp in AlertEventSettings)
                {
                    string alertEventSettingKeys = Convert.ToString(kvp.Key);
                    string[] alertEventSettingKeysList = alertEventSettingKeys.Split('/');
                    string alertEventSettingValues = Convert.ToString(AlertEventSettings[kvp.Key]);
                    string[] alertEventSettingValuesList = alertEventSettingValues.Split(',');
                    if (alertEventSettingValuesList[0] == null)
                        alertEventSettingValuesList[0] = alertEventSettingValues;
                    switch(alertEventSettingKeysList[3]) /// actual kvp.Key comparison
                    {
                        case "onaddedtag":
                            foreach(string ele in alertEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "addtagalert":
                                        addTagAlertFlag = true;
                                        break;
                                }
                            }
                            break;
                        case "onmissedtag":
                            foreach(string ele in alertEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "missedtagalert":
                                        missedTagAlertFlag = true;
                                        break;
                                }
                            }
                            break;
                        case "ondeletetag":
                            foreach(string ele in alertEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "deletetagalert":
                                        deleteTagAlertFlag = true;
                                        break;
                                }
                            }
                            break;
                        case "ondooropen":
                            foreach(string ele in alertEventSettingValuesList)
                            {
                                switch(ele)
                                {
                                    case "dooropenalert":
                                        doorOpenAlertFlag = true;
                                        break;
                                    case "dooropengpohigh":
                                        doorOpenGpoHighFlag = true;
                                        break;
                                    case "dooropengpolow":
                                        doorOpenGpoLowFlag = true;
                                        break;
                                }
                            }
                            break;
                        case "ondoorclose":
                            foreach(string ele in alertEventSettingValuesList)
                            {
                                switch(ele)
                                    {
                                        case "doorclosealert":
                                            doorCloseAlertFlag = true;
                                            break;
                                        case "doorclosegpohigh":
                                            doorCloseGpoHighFlag = true;
                                            break;
                                        case "doorclosegpolow":
                                            doorCloseGpoLowFlag = true;
                                            break;
                                    }
                            }
                            break;
                    }
                }
            }

            /// <summary>
            /// This method handles to apply AppSettings Config initialization data.\n
            /// private void applyAppSettings()\n
            /// This method handles AppSettings parameters initialization item like \n
            /// uricomport \n
            /// antennas \n
            /// trigger \n
            /// doorhandlergpipin \n
            /// dooropengpopin \n
            /// rfasyncontime \n
            /// rfasyncofftime \n
            /// missedTagWaitCycles \n
            /// printtaglist \n
            /// printtagtimerinterval \n
            /// </summary>
            private void applyAppSettings()
            {
                /// Initalize the AppSettings item to default here...
                printTagListFlag = string.Empty;
                readerCOMPort = string.Empty;
                /// AppSettings config initialization data are processed here..
                foreach (KeyValuePair<string, string> kvp in AppSettings)
                {
                    string appSettingKeys = Convert.ToString(kvp.Key);
                    string[] appSettingKeysList = appSettingKeys.Split('/');
                    switch(appSettingKeysList[3]) /// compare with actual element of kvp.Key
                    {
                        case "uricomport":
                            readerCOMPort = AppSettings[kvp.Key];
                            break;
                        case "antennas":
                            List<string> args = new List<string>();
                            args.Add(appSettingKeysList[2]); /// store actual kvp.Key here..
                            args.Add(AppSettings[kvp.Key]);
                            antennaList = ParseAntennaList(args, 0);
                            break;
                        case "trigger":
                            switch (AppSettings[kvp.Key])
                            {
                                case "0":
                                    autoReadType = "ReadOnBoot";
                                    Console.WriteLine("trigger: {0} with {1} is not allowed!!", 
                                    AppSettings[kvp.Key], autoReadType);
                                    Environment.Exit(0);
                                    break;
                                case "1":
                                    autoReadType = "ReadOnGPI";
                                    break;
                                case "3":
                                    autoReadType = "ReadOnGPIAndDuration";
                                    break;
                                default:
                                    Console.WriteLine("Please select trigger option between 0 and 3");
                                    break;
                            }
                            break;
                        case "doorhandlergpipin":
                            switch(AppSettings[kvp.Key])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    gpiTrigTypeNum = Convert.ToInt32(AppSettings[kvp.Key]);
                                    break;
                                default:
                                    Console.WriteLine("Please select gpipin option between 1 and 4");
                                    break;
                            }
                            break;
                        case "dooropengpopin":
                            switch(AppSettings[kvp.Key])
                            {
                                case "1":
                                case "2":
                                case "3":
                                case "4":
                                    gpoTrigTypeNum = Convert.ToInt32(AppSettings[kvp.Key]);
                                    break;
                                default:
                                    Console.WriteLine("Please select gpopin option between 1 and 4");
                                    break;
                            }
                            break;
                        case "rfasyncontime":
                            asyncOnTimeFromConfig = Convert.ToInt32(AppSettings[kvp.Key]);
                            break;
                        case "rfasyncofftime":
                            asyncOffTimeFromConfig = Convert.ToInt32(AppSettings[kvp.Key]);
                            break;
                        case "missedtagwaitcycles":
                            missedTagWaitCycles = Convert.ToInt32(AppSettings[kvp.Key]);
                            break;
                        case "printtaglist":
                            if (AppSettings[kvp.Key] == "enable")
                                printTagListFlag = "true";
                            if (AppSettings[kvp.Key] == "disable")
                                printTagListFlag = "false";
                            break;
                        case "printtagtimerinterval":
                            printTagTimerInterval = Convert.ToInt32(AppSettings[kvp.Key]);
                            break;
                    }
                }
                /// Evaluation GpiPin and GpoPin are same, then throw error  
                if (gpiTrigTypeNum == gpoTrigTypeNum)
                {
                    Console.WriteLine("ERROR: Ensure configured gpipin and gpopin are different!!");
                    Environment.Exit(0);
                }
            }

            /// <summary>
            /// This method apply all Config parameters here...\n
            /// private void applyConfigSettings()\n
            /// This method handles to apply config settings from default config file \n
            /// </summary>
            private void applyConfigSettings()
            {
                /// AppSettings config initialization...\n
                applyAppSettings();
                /// alerteventsettings config initialization...\n
                applyAlertEventSettings();
                /// ExceptionEventSettings Config initialization...\n
                applyExceptionSettings();
            }

            /// <summary>
            /// This methods gets hardware information such as model, serial, 
            /// hardware, software etc from reader here...\n
            /// private void getHardwareInfo(Reader r)\n
            /// This method handles to get hardware information from reader and 
            /// saves in the configInfo struct to process the config items, if needed \n
            /// </summary>
            /// <param name="r">Reader parameter</param>
            private void getHardwareInfo(Reader r)
            {
                /// ReadOnlyReaderParams config data are processed here..
                /// Get model information
                modelNumber = (string)r.ParamGet("/reader/version/model").ToString();
                Console.WriteLine("model= " + modelNumber);
                /// Get number information
                serialNumber = (string)r.ParamGet("/reader/version/serial").ToString();
                Console.WriteLine("serial= " + serialNumber);
                /// Get hardware information
                hardwareNumber = (string)r.ParamGet("/reader/version/hardware").ToString();
                Console.WriteLine("hardware= " + hardwareNumber);
                /// Get software information
                softwareNumber = (string)r.ParamGet("/reader/version/software").ToString();
                Console.WriteLine("software= " + softwareNumber);
            }

            /// <summary>
            /// This method handles to Apply Reader LoadConfig...\n
            /// private void applyReaderLoadConfig(Reader r)\n
            /// This method apply the reader LoadConfig config items 
            /// from default config file (Linux/InventoryTrackerDefault.config) \n
            /// </summary> 
            /// <param name="r">Reader parameter</param>
            private void applyReaderLoadConfig(Reader r)
            {
                /// writeablereaderparams Reader LoadConfig data are processed here..
                string filePath = Directory.GetCurrentDirectory();
                string tempFilePath;
                foreach (KeyValuePair<string, string> kvp in WriteableReaderParams)
                {
                    string writeableReaderParamsKeys = Convert.ToString(kvp.Key);
                    string[] writeableReaderParamsKeysList = writeableReaderParamsKeys.Split('/');
                    if (writeableReaderParamsKeysList[3] == "LOADCONFIG") /// actual kvp.Key comparison
                    {
                        tempFilePath = WriteableReaderParams[kvp.Key];
                        tempFilePath = Path.Combine(filePath, tempFilePath);
                        Console.WriteLine(kvp.Key + ": LoadConfig Path@{0}", tempFilePath);
                        r.LoadConfig(tempFilePath);
                        Console.WriteLine("LoadConfig completed Sucessfully!!");
                    }
                }
            }

            #region SaveConfiguration
            /// <summary>
            /// Save the Inventory Tracker configuration data to file
            /// </summary>
            /// <param name="filePath">Configuration file path</param>
            /// <param name="saveConfigurationList">Configuration parameter list</param>
            private void SaveInventoryTrackerConfiguration(String filePath, List<KeyValuePair<string, string>> saveConfigurationList)
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.AutoFlush = true;
                    foreach (KeyValuePair<string, string> item in saveConfigurationList)
                    {
                        writer.WriteLine(item.Key + "=" + item.Value);
                    }
                    writer.Close();
                }
            }
            #endregion

            #region Configure UHF Persistent Setting
            /// <summary>
            /// This configure UHF persistent settings of reader...\n
            /// private SimpleReadPlan configureUHFPersistentSettings(Reader r, 
            /// string model, int[] antennaList)\n
            /// This function initialize reader UHF Persistent settings items \n
            /// </summary>
            /// <param name="r">Reader parameter</param>
            /// <param name="model">Model number</param>
            /// <param name="antennaList">Antennas list</param>
            private SimpleReadPlan configureUHFPersistentSettings(Reader r, string model, int[] antennaList)
            {
                /// baudrate            
                r.ParamSet("/reader/baudRate", 115200);
                /// Region
                Reader.Region[] supportRegion = (Reader.Region[])r.ParamGet("/reader/region/supportedRegions");
                if (supportRegion.Length < 1)
                {
                    throw new Exception("Reader doesn't support any regions");
                }
                else
                {
                    r.ParamSet("/reader/region/id", supportRegion[0]);
                }
                /// Protocol
                TagProtocol protocol = TagProtocol.GEN2;
                r.ParamSet("/reader/tagop/protocol", protocol);
                /// Gen2 setting
                r.ParamSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
                r.ParamSet("/reader/gen2/tari", Gen2.Tari.TARI_25US);
                r.ParamSet("/reader/gen2/target", Gen2.Target.A);
                r.ParamSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M2);
                r.ParamSet("/reader/gen2/session", Gen2.Session.S0);
                r.ParamSet("/reader/gen2/q", new Gen2.DynamicQ());
                /// RF Power settings
                r.ParamSet("/reader/radio/readPower", 2000);
                r.ParamSet("/reader/radio/writePower", 2000);
                /// Hop Table
                int[] hopTable = (int[])r.ParamGet("/reader/region/hopTable");
                r.ParamSet("/reader/region/hopTable", hopTable);
                int hopTimeValue = (int)r.ParamGet("/reader/region/hopTime");
                r.ParamSet("/reader/region/hopTime", hopTimeValue);
                /// For Open region, dwell time, minimum frequency, 
                /// quantization step can also be configured persistently
                if (Reader.Region.OPEN == (Reader.Region)r.ParamGet("/reader/region/id"))
                {
                    /// Set dwell time enable before stting dwell time
                    r.ParamSet("/reader/region/dwellTime/enable", true);
                    /// set quantization step
                    r.ParamSet("/reader/region/quantizationStep", 25000);
                    /// set dwell time
                    r.ParamSet("/reader/region/dwellTime", 250);
                    /// set minimum frequency
                    r.ParamSet("/reader/region/minimumFrequency", 859000);
                }
                /// Gpi pin trigger setting done here...
                GpiPinTrigger gpiTrigger = null;
                if ((autoReadType != null) && (autoReadType == "ReadOnGPI"))
                {
                    gpiTrigger = new GpiPinTrigger();
                    gpiTrigger.enable = true;
                    /// set the gpi pin to read on
                    r.ParamSet("/reader/read/trigger/gpi", new int[] { gpiTrigTypeNum });
                }
                SimpleReadPlan srp = new SimpleReadPlan(antennaList, protocol, null, null, 1000);
                if ((autoReadType != null) && (autoReadType == "ReadOnGPI"))
                {
                    srp.ReadTrigger = gpiTrigger;
                }
                return srp;
            }
            #endregion

            /// <summary>
            /// This is basic reader initialization functions...\n
            /// private void readerInitialization(Reader r)\n
            /// This function initialize reader param items \n
            /// </summary>
            /// <param name="r">Reader parameter</param>
            private void readerInitialization(Reader r)
            {
                if (Reader.Region.UNSPEC == (Reader.Region)r.ParamGet("/reader/region/id"))
                {
                    Reader.Region[] supportedRegions = (Reader.Region[])r.ParamGet("/reader/region/supportedRegions");
                    if (supportedRegions.Length < 1)
                    {
                        throw new FAULT_INVALID_REGION_Exception();
                    }
                    r.ParamSet("/reader/region/id", supportedRegions[0]);
                }

                /// Default connected antennaList initialization
                if (antennaList == null)
                {
                    /// get the connected antennaList
                    antennaList = (int[])r.ParamGet("/reader/antenna/connectedPortList");
                    /// print the connected antennaList
                    foreach(int item in antennaList)
                        Console.WriteLine("Connected Antenna Number: {0}", item);
                } 

                string model = (string)r.ParamGet("/reader/version/model").ToString();
                if (r.isAntDetectEnabled(antennaList))
                {
                    Console.WriteLine("Module doesn't has antenna detection support please provide antenna list");
                }
                /// asyncOnTime value will set here using values from config
                /// asyncOnTimeFromConfig derived from config file
                r.ParamSet("/reader/read/asyncOnTime", asyncOnTimeFromConfig);
                /// asyncOffTime value will set here using values from config
                /// asyncOffTimeFromConfig derived from config file
                r.ParamSet("/reader/read/asyncOffTime", asyncOffTimeFromConfig);
                /// Uncomment this if readerstats need to be included
                r.ParamSet("/reader/stats/enable", Reader.Stat.StatsFlag.TEMPERATURE);
                /// r.ParamSet("/reader/stats/enable", Reader.Stat.StatsFlag.NONE);
                /// Create a simplereadplan which uses the antenna list created above
                SimpleReadPlan plan;
                plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
                /// Set the created readplan
                r.ParamSet("/reader/read/plan", plan);
                srp = configureUHFPersistentSettings(r, model, antennaList);
                // Ensure the enableAutoRead=true is set always...
                srp.enableAutonomousRead = enableAutoRead;
                r.ParamSet("/reader/read/plan", srp);
                r.ParamSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.UserConfigOperation.SAVEWITHREADPLAN));
                /// Console.WriteLine("User profile set option:save all configuration with read plan is successfull");
                try
                {
                    r.ParamSet("/reader/userConfig", 
                    new SerialReader.UserConfigOp(SerialReader.UserConfigOperation.RESTORE));
                    /// Console.WriteLine("User profile set option:restore all configuration is successfull");
                }
                catch (ReaderException ex)
                {
                    if (ex.Message.Contains("Verifying flash contents failed"))
                    {
                        Console.WriteLine("RESTORE UserConfigOperation failed!!");
                    }
                }
            }

        }
}
