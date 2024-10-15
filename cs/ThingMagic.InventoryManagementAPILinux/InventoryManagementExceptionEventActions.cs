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
/// InventoryManagementExceptionEventActions.cs 
/// Sample Inventory Management Exception Event Actions related API Utilities
/// to meet the expectation of the Inventory Management Application 
/// Author : Rajakumar T. Kulkarni
/// Dated : 05-Aug-2022

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThingMagic;

namespace ThingMagic.InventoryManagementAPI
{
    /// <summary>
    /// This class handles Inventory Management Application Exception Event Actions related methods here
    /// </summary>
    ///
    public class InventoryManagementExceptionEventActions
    {
        internal int _totalMissedTagCount = 0;
        /// tracks the count of missedtag to match uniqueCount alert 
        public int totalMissedTagCount
        {
            get { return _totalMissedTagCount; }
            set { _totalMissedTagCount = value; }   
        }

        /// <summary>
        /// This method handles mark missed tags here..
        /// Also sends exception on missed tag
        /// public int markMissedTags(InventoryManagementConfigData cfg, Hashtable stag, Hashtable mtag)\n
        /// This method identify and process the missed tags ,if any \n
        /// </summary>
        /// <param name="cfg">InventoryManagementConfigData instance.</param>
        /// <param name="stag">Hashtable SeenTags instance.</param>
        /// <param name="mtag">Hashtable MissedTagStatus instance.</param>
        public int markMissedTags(InventoryManagementConfigData cfg, Hashtable stag, Hashtable mtag)
        {
            totalMissedTagCount = 0;
            foreach(var key in stag.Keys)
            {
                string remaining;
                /// Read fresh ReadTagsStatus here...
                string seenTagValues = Convert.ToString(stag[key]);
                string[] seenTagValuesList = seenTagValues.Split(',');
                /// Read fresh MissTagStatus here...
                string missedTagValues = Convert.ToString(mtag[key]);
                string[] missedTagValuesList = missedTagValues.Split(',');
                /// seenTagValuesList[0] contains to Antenna number
                /// seenTagValuesList[1] contains to LastSeenTag time
                if (seenTagValuesList[1] != missedTagValuesList[3] )
                {
                    /// MissedTagStatus hashtable on update tag here..
                    /// missedTagValuesList[0] contains to missedTagWaitCycles count index
                    /// missedTagValuesList[1] contains to status as 
                    /// "Add" or "Update" or "Miss" or "MissReported"
                    /// missedTagValuesList[2] contains to FirstSeenTag time
                    /// missedTagValuesList[3] contains to LastSeenTag time
                    missedTagValuesList[0] = "0";
                    missedTagValuesList[1] = "Update";
                    missedTagValuesList[3] = seenTagValuesList[1];
                }
                if (missedTagValuesList[1] == "Miss")
                {
                    totalMissedTagCount++;
                    int intP = Int32.Parse(missedTagValuesList[0]);
                    intP += 1;
                    if (intP == cfg.missedTagWaitCycles)
                    {   /// the missing tags are reported after missedtagwaitcycles count expires
                        /// missedTagValuesList[0] contains to missedTagWaitCycles count index
                        /// missedTagValuesList[1] contains to status as 
                        /// "Add" or "Update" or "Miss" or "MissReported"
                        missedTagValuesList[0] = "0";
                        missedTagValuesList[1] = "MissReported";
                        if (cfg.firstTimeTotalMissedTagCountFlag)
                        {
                            cfg.firstTimeTotalMissedTagCountFlag = false;
                            /// Inventory Management Alert with "OnMissedTag" 
                            /// triggered here (normal expected)...
                            cfg.OnInventoryManagementAlert("OnMissedTag", Convert.ToString(key));
                        }
                        else
                        {
                            /// Inventory Management Exception with "OnMissedTag" 
                            /// triggered here (unexpected event happened)...
                            cfg.OnInventoryManagementException("OnMissedTag", Convert.ToString(key));
                        }
                    }
                    else
                    {
                        /// Increment the missedtagwaitcycles counter here..
                        /// This will get automatic reset, if the tag are seen again.   
                        /// missedTagValuesList[0] contains to missedTagWaitCycles count index
                        missedTagValuesList[0] = intP.ToString();
                    }
                }
                else
                {
                    /// Ensure to re-initialize all tags as missed to get next updated tag list
                    /// Also, ensure not overwrite, if any "MissReported" identified. 
                    /// missedTagValuesList[0] contains to missedTagWaitCycles count index
                    /// missedTagValuesList[1] contains to status as 
                    /// "Add" or "Update" or "Miss" or "MissReported"
                    if (missedTagValuesList[1] != "MissReported" )
                    {
                        missedTagValuesList[0] = "0";
                        missedTagValuesList[1] = "Miss";
                    }
                }
                /// Ensure to get back the MissedTagStatus string here...
                remaining = missedTagValuesList[0] 
                    + "," 
                    + missedTagValuesList[1] 
                    + "," 
                    + missedTagValuesList[2] 
                    + "," 
                    + missedTagValuesList[3];
                mtag[key] = remaining;
            }
            return totalMissedTagCount;
        }
    }
}
