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

import java.util.Date;
import java.util.EnumSet;
import java.util.Set;
import java.text.DateFormat;
import java.text.SimpleDateFormat;


/**
 * A class to represent a read of an RFID tag. Provides access to the
 * tag structure and the metadata of the read event, such as the time
 * of the read, the antenna that read the tag, and the number of times
 * the tag was seen by the air protocol.
 */
public class TagReadData
{
  TagData tag;

  /** An abstract class which can be extended for specific protocol.
   * Currently Gen2TagReadData for Gen2 protocol
   */
   public ProtocolTagReadData prd;
   
  /**
   * Flags that indicate the metadata stored in this structure 
   */
  public enum TagMetadataFlag
  {
      NONE,
      READCOUNT,
      RSSI,
      ANTENNAID,
      FREQUENCY,
      TIMESTAMP,
      PHASE,
      PROTOCOL,
      DATA,
      GPIO_STATUS,
      GEN2_Q,
      GEN2_LF,
      GEN2_TARGET,
      BRAND_IDENTIFIER,
      TAGTYPE,
      ALL;

    static final Set<TagMetadataFlag> emptyMetadata = 
      EnumSet.noneOf(TagMetadataFlag.class);

  };


  public Set<TagMetadataFlag> metadataFlags;

  int antenna = 0;
  int readCount = 0;
  int rssi = 0;
  int frequency = 0;
  int phase = 0;
  long tagType = 0;
  SerialReader.GpioPin[] gpio = null;
  Reader reader = null;

  long readBase = 0;
  int readOffset = 0;
  String brandIdentifier= null ;

  TagProtocol readProtocol = TagProtocol.NONE;

  byte[] data = noData;
  byte[] dataEpcMem = noData;
  byte[] dataReservedMem = noData;
  byte[] dataTidMem = noData;
  byte[] dataUserMem = noData;
  static final DateFormat df = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ");
  int reservedMemError = -1;
  int epcMemError = -1;
  int tidMemError = -1;
  int userMemError = -1;
  static final byte[] noData = new byte[0];

  boolean isAsyncRead = false;
  public boolean isErrorData = false;
  // This indicates length of data in bits for all modules.
  // In case of data length not a multiple of 8, data gets padded since it is represented in bytes.
  // In this case, dataLength tells the actual size of data
  public int dataLength = 0;
  // Non-public constructor
  TagReadData()
  {
  }

  /**
   * Return the tag that was read.
   *
   * @return the tag
   */ 
  public TagData getTag()
  {
    return tag;
  }

  /**
   * Returns a hexadecimal string version of the read tag's EPC.
   *
   * @return a string representation of the EPC.
   */
  public String epcString()
  {
    return tag.epcString();
  }

  /**
   * Return the identity of the antenna on the reader that read the tag.
   *
   * @return the antenna number
   */
  public int getAntenna()
  {
    return antenna;
  }

  /**
   * Return the returned signal strength of the tag read.
   *
   * @return the rssi value. See /reader/tagReadData/returnRssiInDbm
   * for information about units.
   */
  public int getRssi()
  {
    return rssi;
  }

  /**
   * Return the frequency at which the tag was read
   *
   * @return the frequency, in kHz
   */
  public int getFrequency()
  {
    return frequency;
  }

  /**
   * Return the number of times the tag was read during the operation.
   *
   * @return the number of reads
   */
  public int getReadCount()
  {
    return readCount;
  }

  /**
   * Return the brandIdentifier value.
   *
   * @return the brand Identifier of the tag
   */
  public String getBrandIdentifier()
  {
    return brandIdentifier;
  }
  /**
   * Return the time at which the tag was read.
   * 
   * @return the time, in milliseconds since the epoch
   */
  public long getTime()
  {
      if (isAsyncRead)
      {
        return readBase;
      }
      else
      {
        return readBase + readOffset;
      }
    
  }

  /**
   * Return the data read from the tag.
   */
  public byte[] getData()
  {
    return data.clone();
  }

  public byte[] getEPCMemData()
  {
      return dataEpcMem.clone();
  }

  public byte[] getReservedMemData()
  {
      return dataReservedMem.clone();
  }

  public byte[] getUserMemData()
  {
      return dataUserMem.clone();
  }

  public byte[] getTIDMemData()
  {
      return dataTidMem.clone();
  }

  public int getReservedMemReadError()
  {
      return reservedMemError;
  }
  public int getEpcMemReadError()
  {
      return epcMemError;
  }
  public int getTidMemReadError()
  {
      return tidMemError;
  }
  public int getUserMemReadError()
  {
      return userMemError;
  }
  
  public Reader.GpioPin[] getGpio()
  {
    return gpio;
  }
  /**
   * Return the phase the tag is in 
   *
   * @return the phase
   */
  public int getPhase()
  {
    return phase;
  }

  /**
   * return the reader instance
   * @return the reader 
   */
  public Reader getReader()
  {
    return reader;
  }
  
  // UID of tag
  // </summary>
 public long TagType()
 {
   return tagType;
 }

  /**
   * Returns a <code>String</code> object representing this
   * object. The string contains a whitespace-delimited set of
   * field:value pairs representing the tag ID and metadata.
   *
   * @return the representation string
   */
  public String toString() 
  {
    return String.format("Tag ID :%s ant:%d count:%d time:%s",
                         (tag == null) ? "none" : tag.epcString(),
                         antenna,
                         readCount,
                         df.format(new Date(getTime())));
  }
}