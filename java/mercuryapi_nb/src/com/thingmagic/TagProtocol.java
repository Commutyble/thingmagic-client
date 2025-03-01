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
package com.thingmagic;

/**
 * RFID Protocols
 */
public enum TagProtocol
{

  GEN2, ISO180006B, ISO180006B_UCODE, IPX64, IPX256, ATA,// UHF protocols
  ISO14443A, ISO14443B, ISO15693, ISO18092, FELICA, ISO18000_3M3,// HF protocols
  LF125KHZ, LF134KHZ,// LF protocols
  NONE;

  public static TagProtocol getProtocol(String s)
  {
    s = s.toUpperCase();
    if (s.equals("GEN2") || 
        s.equals("ISO18000-6C") || s.equals("ISO180006C") ||
        s.equals("ISO18K6C") || s.equals("ISO18K6C") || s.equals("12"))
    {
      return GEN2;
    }
    else if (s.equals("ISO18000-6B") || s.equals("ISO180006B") ||
             s.equals("ISO18K6B") || s.equals("ISO18K6B") || s.equals("8"))
    {
      return ISO180006B;
    }
    else if (s.equals("ISO180006B_UCODE") ||
             s.equals("UCODE"))
    {
      return ISO180006B_UCODE;
    }
    else if (s.equals("IPX64") || s.equals("13"))
    {
      return IPX64;
    }
    else if (s.equals("IPX256") || s.equals("14"))
    {
      return IPX256;
    }
    else if (s.equals("ATA") || s.equals("29"))
    {
      return ATA;
    }
    else if (s.equals("ISO14443A") || s.equals("9"))
    {
      return ISO14443A;
    }
    else if (s.equals("ISO14443B") || s.equals("10"))
    {
      return ISO14443B;
    }
    else if (s.equals("ISO15693") || s.equals("11"))
    {
      return ISO15693;
    }
    else if (s.equals("ISO18092") || s.equals("12"))
    {
      return ISO18092;
    }
    else if (s.equals("FELICA") || s.equals("13"))
    {
      return FELICA;
    }
    else if (s.equals("ISO18000_3M3") || s.equals("14"))
    {
      return ISO18000_3M3;
    }
    else if (s.equals("LF125KHZ") || s.equals("20"))
    {
      return LF125KHZ;
    }
    else if (s.equals("LF134KHZ") || s.equals("21"))
    {
      return LF134KHZ;
    }
    else
    {
      return null;
    }
   }
  }

