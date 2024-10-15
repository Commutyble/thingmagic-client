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
/// InventoryManagementAlertEventActions.cs 
/// Sample Inventory Management Alert Event Actions related API Utilities
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
    /// This class handles Inventory Management Application Alert Event Actions related methods here
    /// </summary>
    ///
    public class InventoryManagementAlertEventActions
    {
        /// <summary>
        /// This is Inventory Management Door Close Event handler performs actions 
        /// send alert messages on door close. Also performs the reader GpoSet needs, if any
        /// public void doorCloseEventHandler(InventoryManagementConfigData cfg, Reader r)
        /// </summary>
        /// <param name="cfg">InventoryManagementConfigData instance.</param>
        /// <param name="r">Reader instance.</param>
        public void doorCloseEventHandler(InventoryManagementConfigData cfg, Reader r)
        {
            //AlertListener message with "OnDoorClose" triggered here...
            if (cfg.doorCloseAlertFlag)
            {
                cfg.doorStatus = "doorClose";
                cfg.OnInventoryManagementAlert("OnDoorClose", "DOOR IS CLOSED!!");
            }
            ///if doorclosegpohigh detected...
            if (cfg.doorCloseGpoHighFlag)
            {
                cfg.OnInventoryManagementAlert("DoorCloseGPOHigh",(string)("GPO"+cfg.gpoTrigTypeNum+" IS SET HIGH!!"));
                r.GpoSet(new GpioPin[] { new GpioPin(cfg.gpoTrigTypeNum, true)});
            }
            ///if doorclosegpolow detected...
            else if (cfg.doorCloseGpoLowFlag)
            {
                cfg.OnInventoryManagementAlert("DoorCloseGPOLow",(string)("GPO"+cfg.gpoTrigTypeNum+" IS SET LOW!!"));
                r.GpoSet(new GpioPin[] { new GpioPin(cfg.gpoTrigTypeNum, false)});
            }
        }

        /// <summary>
        /// This is Inventory Management Door Open Event handler performs 
        /// actions to send alert messages on door open. Also performs the reader GpoSet needs, if any
        /// public void doorOpenEventHandler(InventoryManagementConfigData cfg, Reader r)
        /// </summary>
        /// <param name="cfg">InventoryManagementConfigData instance.</param>
        /// <param name="r">Reader instance.</param>
        public void doorOpenEventHandler(InventoryManagementConfigData cfg, Reader r)
        {
            ///AlertListener message with "OnDoorOpen" triggered here...
            if (cfg.doorOpenAlertFlag)
            {
                cfg.doorStatus = "doorOpen";
                cfg.OnInventoryManagementAlert("OnDoorOpen", "DOOR IS OPENED!!");
            }
            ///if dooropengpohigh configured, then setGPOHigh...
            if (cfg.doorOpenGpoHighFlag)
            {
                cfg.OnInventoryManagementAlert("DoorOpenGPOHigh",(string)("GPO"+cfg.gpoTrigTypeNum+" IS SET HIGH!!"));
                r.GpoSet(new GpioPin[] { new GpioPin(cfg.gpoTrigTypeNum, true)});
            }
            ///if dooropengpolow configured, the setGPOLow
            else if (cfg.doorOpenGpoLowFlag)
            {
                cfg.OnInventoryManagementAlert("DoorOpenGPOLow",(string)("GPO"+cfg.gpoTrigTypeNum+" IS SET LOW!!"));
                r.GpoSet(new GpioPin[] { new GpioPin(cfg.gpoTrigTypeNum, false)});
            }
        }
    }

}
