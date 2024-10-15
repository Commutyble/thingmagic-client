/*  @file Iso15693.java
 *  @brief Mercury API - Iso15693 tag information
 *  @author pchinnapapannagari
 *  @date 03/11/2020
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
 * THE SOFTWARE
*/
package com.thingmagic;

import java.util.EnumSet;
import java.util.HashMap;
import java.util.Map;

public class Iso15693
{
    
    Iso15693()
    {
    }
        // enum to define variants of tag type under ISO15693 protocol
        
    public enum TagType
    {
        // Auto detect - supports all tag types
        AUTO_DETECT(0x00000001),
        // HID Iclass SE tagtype
        HID_ICLASS_SE(0x00000100),
        // NXP Icode SLI tagtype
        ICODE_SLI(0x00000200),
        // NXP Icode SLI-L tagtype
        ICODE_SLI_L(0x00000400),
        // NXP Icode SLI-S tag type
        ICODE_SLI_S(0x00000800),
        // NXP ICODE DNA tagtype
        ICODE_DNA(0x00001000),
        // NXP ICODE SLIX tagtype
        ICODE_SLIX(0x00002000),
        // NXP ICODE SLIX-L tagtype
        ICODE_SLIX_L(0x00004000),
        // NXP ICODE SLIX-S tagtype
        ICODE_SLIX_S(0x00008000),
        // NXP Icode SLIX-2 tagtype
        ICODE_SLIX_2(0x00010000),
        // VIGO tagtype
        VIGO(0x00020000),
        //TAGIT tagtype
        TAGIT(0x00040000),
        // Pico Pass
        PICOPASS(0x00080000),
        //UNKNOWN tagtype
        UNKNOWN (-1),
        // ALL tag types
        ALL(0x000FFF01);
        
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

    public static class TagData extends com.thingmagic.TagData
    {
        /// <summary>
        /// Tag's RFID protocol
        /// </summary>
        @Override
        public  TagProtocol getProtocol()
        {
                return TagProtocol.ISO15693;
        }

        /// <summary>
        /// Create TagData with blank CRC
        /// </summary>
        /// <param name="uidBytes">UID value</param>
        public TagData(byte[] uidBytes) { super(uidBytes); }

        /**
        * Construct an ISO15693 tag data from a byte array.
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