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
///InventoryManagementApp.cs 
///This is skeleton sample Inventory Management Application
///Author : Rajakumar T. Kulkarni
///Dated  : 28 June, 2022
///
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// for Thread.Sleep
using System.Threading;

/// Reference the API
using ThingMagic;
using ThingMagic.InventoryManagementAPI;

namespace ThingMagic.InventoryManagementApp
{
    /// <summary>
    /// Sample program that reads tags in the background and track tags
    /// that have been seen; only print the tags that have not been seen
    /// before.
    /// </summary>
    class Program
    {
        static void Main()
        {
            /// Get acess to reference basic Tag Inventory Container object here (called from the class TagInventoryContainer)  
            using (TagInventoryManagementContainer tagStore = new TagInventoryManagementContainer())
            {
                /// Create Reader object, connecting to physical device.
                /// Also Tag Inventory Container Initialization is done here..
                tagStore.inventoryInitialization();
                /// Search for tags in the background
                while (true)
                {
                    Thread.Sleep(5000);
                }
            }
        }
    }
    
}
