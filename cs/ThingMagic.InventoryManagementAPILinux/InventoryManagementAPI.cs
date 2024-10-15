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

/// InventoryManagementAPI.cs 
/// Sample Inventory Management API Utilities
/// to meet the expectation of the Inventory Management Application 
/// Author : Rajakumar T. Kulkarni
/// Dated : 08-July-2022
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.Collections.Specialized;

/// for Thread.Sleep
using System.Threading;
using ThingMagic;

namespace ThingMagic.InventoryManagementAPI
{
    /// <summary> 
    /// This is main ITagMonitor interface to handle inventory tracker API methods
    /// Also, Implements the defined interfaces.
    /// </summary>
    interface ITagInventoryManagement
    {
        /// This method performs complete inventory initialization including 
        /// all event handler registrations \n
        public void inventoryInitialization();
        /// This method handles alert on incoming new tag reads and \n 
        /// builds the list of tags with metadata information to process \n
        /// and handle further...
        public void inventoryTagListener(object sender, ThingMagic.TagReadDataEventArgs e);
    }

    /// <summary> 
    /// This is main Tag Inventory Container API class which covers all the defined methods
    /// Also, Implements the defined interfaces.
    /// </summary>
    public class TagInventoryManagementContainer : ITagInventoryManagement, IDisposable 
    {
        /// <summary> 
        /// This is tagInventoryDBInfo struct, used to store all unique tags received from reader
        /// </summary>
        public struct tagInventoryDBInfo
        {
            /// Reader Object information saved here..
            public Reader rdrObj;
            /// SeenTags Hashtable information saved here...
            public Hashtable SeenTags;
            /// MissedTagStatus information handled here..
            public Hashtable MissedTagStatus;
        }
        /// tagDBInfo object reference to tagInventoryDBInfo struct
        public tagInventoryDBInfo tagDBInfo;

        /// configInfo object reference to tagInventoryConfigInfo struct
        /// public InventoryManagementConfigData configInfo;
        public InventoryManagementConfigData configInfo = new InventoryManagementConfigData();

        ///  AlertEventActions
        public InventoryManagementAlertEventActions alertActions= new InventoryManagementAlertEventActions();
        ///  ExceptionEventActions
        public InventoryManagementExceptionEventActions exceptionActions = new InventoryManagementExceptionEventActions();

        /// Below variables are specific to tag inventory container application 
        /// specific intermediate handling needs...
        /// maxTimerCount to track and monitor the define print tag list timer interval
        public static int maxTimerCount = 0;
        /// tracks and handle to skip on false missed tag detected
        public static bool skipMarkMissedTagFlag = true;
        /// tracks the first time tag read
        public static bool firstTimeSeenTagFlag = false;
        /// tracks the first time missed tag alert 
        public static bool firstTimeMarkMissedTagFlag = false;
        /// tracks the count of missedtag to match uniqueCount alert 
        public static int totalMissedTagCount = 0;
        /// local disposed flag 
        private bool disposed = false;

        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        /// <param name="disposing">Indicates the source call to dispose.</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }
            if (disposing)
            {
                /// Number of instance you want to dispose
                tagDBInfo.rdrObj.TagRead -= inventoryTagListener;
                tagDBInfo.rdrObj.ReadException -= readerException;
            }
            disposed = true;
        }

        /// <summary>
        /// This is Inventory Tracker Handle Timer...\n
        /// private void inventoryTimer()\n
        /// This is callback function triggered every 1 second with system timer \n
        /// </summary>
        public void inventoryTimer()
        {
            ///Mark the missing tags here on every HandleTimer...     
            if (!skipMarkMissedTagFlag)
            {
                totalMissedTagCount = exceptionActions.markMissedTags(configInfo, 
                tagDBInfo.SeenTags, tagDBInfo.MissedTagStatus);
                if (totalMissedTagCount == 0)
                {
                    configInfo.firstTimeTotalMissedTagCountFlag = false;
                }
                if (totalMissedTagCount == tagDBInfo.SeenTags.Count) /// comparison to uniqueCount
                {
                    firstTimeSeenTagFlag = false;
                    skipMarkMissedTagFlag = true;
                    configInfo.firstTimeTotalMissedTagCountFlag = true;
                    totalMissedTagCount = 0;
                    /// perform Door Open Alert Action here...
                    alertActions.doorOpenEventHandler(configInfo, tagDBInfo.rdrObj);
                }
            } 
            else
            {
                /// comparison to uniqueCount
                if ((!firstTimeMarkMissedTagFlag)&&
                    (totalMissedTagCount == tagDBInfo.SeenTags.Count)) 
                {
                    firstTimeMarkMissedTagFlag = true;
                    configInfo.firstTimeTotalMissedTagCountFlag = true;
                    /// perform Door Open Alert Action here...
                    alertActions.doorOpenEventHandler(configInfo, tagDBInfo.rdrObj);
                }
            }
            if (configInfo.printTagListFlag == "true")
            {
                maxTimerCount++;
                if (maxTimerCount == configInfo.printTagTimerInterval)
                {
                    /// print InventoryList here on every 5 seconds...
                    maxTimerCount = 0;
                    if (!skipMarkMissedTagFlag)
                    {
                        /// discrepency on Delete tags is handled here...
                        deleteTagFromInventoryList(configInfo, 
                        tagDBInfo.SeenTags, tagDBInfo.MissedTagStatus);
                        /// Final available complete Inventory list is printed here...
                        printInventoryList(tagDBInfo.SeenTags);
                        Console.WriteLine("Unique Tags: " + tagDBInfo.SeenTags.Count);
                    }
                }
            }
        }

        /// <summary>
        /// This method handles reader stats message...\n
        /// private void readerStats(object sender, StatsReportEventArgs e)\n
        /// This function handles any incoming stats like TEMPERATUES, VOLTAGE etc from the reader \n
        /// </summary>
        private void readerStats(object sender, StatsReportEventArgs e)
        {
            string tmpTemp = e.StatsReport.ToString();
            /// Console.WriteLine(e.StatsReport.ToString()); /// commented.
        }

        /// <summary>
        /// This method handles reader exception message...\n
        /// private void readerException(object sender, ReaderExceptionEventArgs e)\n
        /// This method handles any incoming exceptions from the reader \n
        /// </summary>
        private void readerException(object sender, ReaderExceptionEventArgs e)
        {
            string tmpExceptions = e.ReaderException.Message;
            string[] tmpExceptionsList = tmpExceptions.Split('/');
            /// skip application specification reader LoadConfig exceptions received, if any
            if (tmpExceptionsList[1] != "application") 
                Console.WriteLine(e.ReaderException.Message);
        }

        /// <summary>
        /// This method handles Inventory initialization...\n
        /// public void inventoryInitialization()\n
        /// This method performs complete inventory initialization including all event handler registrations \n
        /// such as \n
        /// get reader object (rdrObj)
        /// readerStats \n
        /// inventoryAlert \n
        /// inventoryTimer \n
        /// readerException \n
        /// inventoryException \n
        /// </summary>
        public void inventoryInitialization()
        {
            /// This method initialize Inventory Management Configuration 
            /// Data such as configSettings etc
            configInfo.InventoryManagementInitConfigData();
            /// Create Reader object, connecting to physical device.
            tagDBInfo.rdrObj = Reader.Create(configInfo.readerCOMPort);
            try
            {
                /// Uncomment this line to add default transport listener.
                // tagDBInfo.rdrObj.Transport += tagDBInfo.rdrObj.SimpleTransportListener;
                /// Set the current baudrate so that next connect will use this baudrate.
                tagDBInfo.rdrObj.ParamSet("/reader/baudRate", 115200);
                /// connect reader now
                tagDBInfo.rdrObj.Connect();
                /// Add reader stats listener
                tagDBInfo.rdrObj.StatsListener += readerStats;
                /// Create and add read exception listener
                tagDBInfo.rdrObj.ReadException += readerException;
                /// Inventory Management Initialization of Configuration Data
                configInfo.InventoryManagementInitRemainingConfigData(tagDBInfo.rdrObj);
                /// Create and add tag listener (called from the class InventoryListListener)
                tagDBInfo.rdrObj.TagRead += inventoryTagListener;
                /// Hashtable are initialized here...
                tagDBInfo.SeenTags = new Hashtable();
                tagDBInfo.MissedTagStatus = new Hashtable();
                /// system timer alert listener too here....
                System.Timers.Timer timer = new ();
                timer.Interval = (int) (configInfo.asyncOnTimeFromConfig + configInfo.asyncOffTimeFromConfig);
                timer.Elapsed += ( sender, e ) => inventoryTimer();
                timer.Start();
                /// Start Autonomous tag reading here...
                tagDBInfo.rdrObj.ReceiveAutonomousReading();
            }
            catch (ReaderException re)
            {
                Console.WriteLine("Error: " + re.Message);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// This method handles add tag to Inventory Management List ...\n
        /// private void addTagToInventoryList(ThingMagic.TagReadDataEventArgs e, Hashtable htag)\n
        /// This function identify and process the any new tag added, if any  here \n
        /// </summary>
        private void addTagToInventoryList(ThingMagic.TagReadDataEventArgs e, Hashtable htag, Hashtable mtag)
        {
            string remaining;
            ThingMagic.TagData t = e.TagReadData.Tag;
            /// extract epc info here..
            string epc = t.EpcString;
            /// time stamp format here..
            string etime = e.TagReadData.Time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
            /// remaining string for SeenTags Hashtable contains: Antenna, LastSeenTag
            /// SeenTags[0] contains Antenna number
            /// SeenTags[1] contains LastSeenTag time
            remaining = e.TagReadData.Antenna 
                + "," 
                + etime;
            /// Fill SeenTags HashTable here...
            htag.Add(epc, remaining);

            /// Mark the NEW TAG Added in MissedTagStatus
            /// remaining = MissedTagReportCount, "Add", FirstSeenTag, LastSeenTag
            /// missedTagStatus[0] contains to missedTagWaitCycles count index
            /// missedTagStatus[1] contains to status as 
            /// "Add" or "Update" or "Miss" or "MissReported"
            /// missedTagStatus[2] contains to FirstSeenTag time
            /// missedTagStatus[3] contains to LastSeenTag time
            remaining = "0" 
                + "," 
                + "Add" 
                + "," 
                + etime 
                + "," 
                + etime;
            mtag.Add(epc, remaining);
        }

        /// <summary>
        /// This method handles update tag to inventory management list
        /// private void updateTagToInventoryList(ThingMagic.TagReadDataEventArgs e)\n
        /// This function identify and process all existing tags, if any here \n
        /// </summary>
        private void updateTagToInventoryList(ThingMagic.TagReadDataEventArgs e, Hashtable stag)
        {
            ThingMagic.TagData t = e.TagReadData.Tag;
            string epc = t.EpcString;
            /// SeenTags contains: Antenna, @LastSeenTag
            /// SeenTags[0] contains Antenna number
            /// SeenTags[1] contains LastSeenTag time
            stag[epc] = e.TagReadData.Antenna 
                + "," 
                + e.TagReadData.Time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

        /// <summary>
        /// This handles alert on incoming new tag reads and \n
        /// builds the list of tags with metadata information to process \n
        /// and handle further...
        /// public void inventoryTagListener(object sender, ThingMagic.TagReadDataEventArgs e)\n
        /// This function identify and process the incoming tag reads here \n
        /// </summary>
        public void inventoryTagListener(object sender, ThingMagic.TagReadDataEventArgs e)
        {
            if (!firstTimeSeenTagFlag)
            {
                skipMarkMissedTagFlag = false;
                firstTimeSeenTagFlag = true;
                firstTimeMarkMissedTagFlag = false;
                alertActions.doorCloseEventHandler(configInfo, tagDBInfo.rdrObj);
            }
            lock (tagDBInfo.SeenTags.SyncRoot)
            {
                ThingMagic.TagData t = e.TagReadData.Tag;
                string epc = t.EpcString;
                if (!tagDBInfo.SeenTags.ContainsKey(epc))
                {
                    /// Send alert on added tag here...
                    addTagToInventoryList(e, tagDBInfo.SeenTags, 
                    tagDBInfo.MissedTagStatus);
                    /// AlertListener message with "OnAddedTag" triggered here...   
                    if (configInfo.addTagAlertFlag)
                        configInfo.OnInventoryManagementAlert("OnAddedTag",(string)(epc+","+tagDBInfo.SeenTags[epc]));
                }
                else
                {
                    /// all remaining update tag done here...
                    updateTagToInventoryList(e, tagDBInfo.SeenTags);
                }
            }
        }

        /// <summary>
        /// This method handles pre-defined interval on print inventory list\n
        /// private void printInventoryList(Hashtable stag)\n
        /// This method prints all tags available in the inventory list here \n
        /// </summary>
        private void printInventoryList(Hashtable stag)
        {
            Console.WriteLine("///Inventory List as maintained in SeenTags///");
            foreach(DictionaryEntry ele in stag)
                Console.WriteLine("{0},{1} ", ele.Key, ele.Value);
            Console.WriteLine("///Inventory List completed!!! ///");
        }

        /// <summary>
        /// This method handles descrepency on delete Tags as needed ...\n
        /// private void deleteTagFromInventoryList(InventoryManagementConfigData cfg, 
        /// Hashtable stag, Hashtable mtag)\n
        /// Delete the missing seenTags from List
        /// </summary>
        private void deleteTagFromInventoryList(InventoryManagementConfigData cfg, Hashtable stag, Hashtable mtag)
        {
            foreach(var key in mtag.Keys)
            {
                string missedTagValues = Convert.ToString(mtag[key]);
                string[] missedTagValuesList = missedTagValues.Split(',');
                
                /// Hashtable contains as below
                /// missedTagValuesList[0] contains to missedTagWaitCycles count index
                /// missedTagValuesList[1] contains to status as 
                /// "Add" or "Update" or "Miss" or "MissReported"
                /// missedTagValuesList[2] contains to FirstSeenTag time
                /// missedTagValuesList[3] contains to LastSeenTag time
                if (missedTagValuesList[1] == "MissReported")
                {
                    /// Inventory Management Alert with "OnDeleteTag" triggered here...
                    if (cfg.deleteTagAlertFlag)
                    {
                        cfg.OnInventoryManagementAlert("OnDeleteTag", Convert.ToString(key));
                        /// actual delete tag done here... 
                        stag.Remove(key);
                        mtag.Remove(key);
                    }
                }
            }
        }

    }

}
