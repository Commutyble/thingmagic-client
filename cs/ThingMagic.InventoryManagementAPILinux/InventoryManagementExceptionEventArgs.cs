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
/// InventoryManagementExceptionEventArgs.cs 
/// Sample Inventory Management Exception Events related API Utilities
/// to meet the expectation of the Inventory Management Application 
/// Author : Rajakumar T. Kulkarni
/// Dated : 22-July-2022

using System;
using System.Collections.Generic;
using System.Text;

namespace ThingMagic.InventoryManagementAPI
{
    /// <summary>
    /// This object sends inventory management exception event statics to ExceptionListener
    /// The following are inventory management exception events: 
    /// OnUnwantedTag \n 
    /// OnMissedTag \n 
    /// OnDuplicateTag \n 
    /// </summary>
    public class InventoryManagementExceptionEventArgs : EventArgs
    {
        #region Fields
        /// local string to store exception event type
        internal string _exceptionEventType = null; 
        /// local string to store exception event action
        internal string _exceptionEventAction = null;
        /// InventoryManagement ExceptionEventType allowed events...  
        private List<string> _allowedExceptionEventType = new List<string> { "OnUnwantedTag","OnMissedTag", "OnDuplicateTag" };
        #endregion

        #region Properties
        /// <summary>
        /// Message Type: contents, such as OnUnwantedTag, OnMissedTag, OnDeleteTag, OnDuplicateTag
        /// </summary>    
        public string exceptionEventType
        {
            get { return _exceptionEventType; }
            set { _exceptionEventType = value; }
        }
        
        /// <summary>
        /// Message Value: contents either text or tag
        /// </summary>    
        public string exceptionEventAction
        {
            get { return _exceptionEventAction; }
            set { _exceptionEventAction = value; }
        }
        
        /// <summary>
        /// Message Type: contents, such as OnUnwantedTag, OnMissedTag, OnDuplicateTag
        /// </summary>    
        public List<string> allowedExceptionEventType
        {
            get { return _allowedExceptionEventType; }
            set { _allowedExceptionEventType = value; }
        }
        #endregion

        #region Construction
        /// <summary>
        /// InventoryManagementExceptionEventArgs Constructor
        /// </summary>
        /// <param name="exEventType">the exception event type</param>
        /// <param name="exEventAction">the exception event action</param>
        public InventoryManagementExceptionEventArgs(string exEventType, string exEventAction)
        {
            if (!allowedExceptionEventType.Contains(exEventType))
            {
                Console.WriteLine("ExceptionType: {0} IS NOT SUPPORTED!!!", exEventType);
                Environment.Exit(0);
            }
            else
            {
                exceptionEventType = exEventType;
                exceptionEventAction = exEventAction;
                Console.WriteLine("ExceptionType:{0} Message:{1}", exceptionEventType, exceptionEventAction);
            }
        }
    #endregion
    
    }

}
