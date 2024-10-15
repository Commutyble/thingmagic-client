/*  @file Iso14443b.java
 *  @brief Mercury API - Iso14443b tag information
 *  @author Prasad Chinnapapannagari
 *  @date 5/27/2020
 *  Copyright (c) 2023 Novanta, Inc.
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
package com.thingmagic;

import java.util.EnumSet;
import java.util.HashMap;
import java.util.Map;

public class Iso14443b
{

    Iso14443b()
    {
    }
    
    public enum TagType
    {
        // Auto detect - supports all tag types
        AUTO_DETECT(0x00000001),
        // CALYPSO tag type
        CALYPSO(0x00000002),
        // CALYPSO_INNOVATRON_PROTOCOL tag type
        CALYPSO_INNOVATRON_PROTOCOL(0x00000004),
        // CEPAS tag type
        CEPAS(0x00000008),
        //CTS tag type
        CTS (0x00000010),
        // MONEO tag type
        MONEO(0x00000020),
        // PICO_PASS tag type
        PICO_PASS(0x00000040),
        /// SRI4K tag type
        SRI4K(0x00000080),
        // SRIX4K tag type
        SRIX4K(0x00000100),
        // SRI512 tag type
        SRI512(0x00000200),
        // SRT512 tag type
        SRT512(0x00000400),
        //ALL
        ALL(0x000865);
        int rep;
        TagType(int rep)
        {
          this.rep = rep;
        }

        private static final Map<Integer, TagType> lookup = new HashMap<Integer, TagType>();

        static 
        {
            for (TagType type : EnumSet.allOf(TagType.class)) {
                lookup.put(type.getCode(), type);
            }
        }

        public int getCode() {
            return rep;
        }

        public static TagType get(int rep) {
            return lookup.get(rep);
        }
    }
    // ISO14443B specific version of TagData.
    public static class TagData extends com.thingmagic.TagData
    {
        // Tag's RFID protocol
        @Override
        public  TagProtocol getProtocol()
        {
                return TagProtocol.ISO14443B;
        }
        // Create TagData with blank CRC
        // <param name="uidBytes">UID value</param>
        public TagData(byte[] uidBytes) { super(uidBytes); }

        /**
        * Construct an ISO14443B tag data from a byte array.
        *
        * @param uidBytes uid bytes.
        * @param crc CRC bytes
        */
       public TagData(byte[] uidBytes, byte[] crc)
       {
         super(uidBytes, crc);
       }
        
    }
}