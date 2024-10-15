/*
 * Copyright (c) 2023 Novanta, Inc.
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

using System;
using System.Collections.Generic;
using System.Text;

namespace ThingMagic
{
    /// <summary>
    /// Abstract class for all the extended tag operations.
    /// </summary>
    abstract public class ExtendedTagOperations { }

    /// <summary>
    /// Class used for extended tag operations
    /// </summary>

    public class ExtTagOp : ThingMagic.TagOp
    {
        /// <summary>
        /// Tag Type
        /// </summary>
        public long tagType;

        /// <summary>
        /// Access Password
        /// </summary>
        public byte[] accessPassword;

        /// <summary>
        /// Read Memory
        /// </summary>
        public ReadMemory readMem;

        /// <summary>
        /// Write Memory
        /// </summary>
        public WriteMemory writeMem;

        /// <summary>
        /// ultralight N tag tagops
        /// </summary>
        public ThingMagic.UltraLightNtagTagOp ulNtag = new ThingMagic.UltraLightNtagTagOp();
    }
}
