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
/// InventoryManagementAlertEventArgs.cs 
/// Sample Inventory Management Alert Events related API Utilities
/// to meet the expectation of the Inventory Management Application 
/// Author : Rajakumar T. Kulkarni
/// Dated : 22-July-2022

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace ThingMagic.InventoryManagementAPI
{

    /// <summary>
    /// This class object sends inventory management alert event statics to inventoryAlertListener
    /// public class InventoryManagementAlertEventArgs : EventArgs
    /// The following inventory management alert events supported: 
    /// OnAddedTag 
    /// OnDoorOpen 
    /// OnDoorClose 
    /// DoorOpenGPOHigh
    /// DoorOpenGPOLow
    /// DoorCloseGPOHigh
    /// DoorcloseGPOLow 
    /// </summary>
    public class InventoryManagementAlertEventArgs : EventArgs
    {
        #region Fields
        /// local string alert event type
        internal string _alertEventType = null;
        /// local string alert event action 
        internal string _alertEventAction = null;
        /// InventoryManagement AlertEventType allowed events...  
        internal List<string> _allowedEventType = new List<string> { "OnAddedTag","OnMissedTag", 
            "OnDeleteTag", "OnDoorOpen","OnDoorClose", "DoorOpenGPOHigh", "DoorOpenGPOLow", 
            "DoorCloseGPOHigh", "DoorCloseGPOLow"};
        #endregion

        #region Properties
        /// <summary>
        /// Message Type: contents, such as OnAddedTag, OnMissedTag, OnDeleteTag, 
        /// OnDoorOpen, OnDoorClose, DoorOpenGPOHigh, DoorOpenGPOLow, DoorCloseGPOHigh, DoorCloseGPOLow
        /// </summary>
        public string alertEventType
        {
            get { return _alertEventType; }
            set { _alertEventType = value; }
        }

        /// <summary>
        /// Message Value: contents either text or tag
        /// </summary>    
        public string alertEventAction
        {
            get { return _alertEventAction; }
            set { _alertEventAction = value; }
        }

        /// <summary>
        /// Message Type: contents, such as OnAddedTag, OnMissedTag, 
        /// OnDeleteTag, OnDoorOpen, OnDoorClose, DoorOpenGPOHigh, 
        /// DoorOpenGPOLow, DoorCloseGPOHigh, DoorCloseGPOLow
        /// </summary>
        public List<string> allowedEventType
        {
            get { return _allowedEventType; }
            set { _allowedEventType = value; }
        }
        #endregion

        #region Construction
        /// <summary>
        /// InventoryManagementAlertEventArgs Constructor
        /// </summary>
        /// <param name="alrtEventType">the alert event type</param>
        /// <param name="alrtEventAction">the alert event action</param>
        public InventoryManagementAlertEventArgs(string alrtEventType, string alrtEventAction)
        {
            if (!allowedEventType.Contains(alrtEventType))
            {
                Console.WriteLine("AlertType: {0} IS NOT SUPPORTED!!!", alrtEventType);
                Environment.Exit(0);
            }
            else
            {
                alertEventType = alrtEventType;
                alertEventAction = alrtEventAction;
                Console.WriteLine("AlertType:{0} Message:{1}", alertEventType, alertEventAction);
            }
        }
        #endregion

    }

}
