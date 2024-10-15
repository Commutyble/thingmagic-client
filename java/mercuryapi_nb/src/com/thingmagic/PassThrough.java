/*  @file PassThrough.java
 *  @brief Mercury API - Pass through functionality
 *  @author bchanda
 *  @date 06/08/2020
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

import java.util.ArrayList;
import java.util.EnumSet;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

// class to demonstrate PassThrough functionality
public class PassThrough extends TagOp
{
    // Timeout in msec 
    public int timeout;

    // Configuration flags - RFU 
    public int configFlags;

    // Command buffer 
    public List<Byte> buffer = new ArrayList<Byte>();

    /*
    Constructor to initialize the parameters of PassThrough
    <param name="timeout">Timeout in msec </param>
    <param name="configFlags">Configuration flags - RFU</param>
    <param name="buffer">Command buffer </param>
    */
    public PassThrough(int timeout, int configFlags, List<Byte> buffer)
    {
        this.timeout = timeout;
        this.configFlags = configFlags;
        this.buffer = buffer;
    }

    // Configuration Flags
    public enum ConfigFlags
    {
        // enables TX CRC
        ENABLE_TX_CRC (0x01),
        // enables RX CRC
        ENABLE_RX_CRC (0x02),
        // enables Inventory
        ENABLE_INVENTORY (0x04);

        int value;
        
        ConfigFlags(int value)
        {
            this.value = value;
        }

        private static final Map<Integer, ConfigFlags> lookup = new HashMap<Integer, ConfigFlags>();
        static
        {
          for (ConfigFlags flags : EnumSet.allOf(ConfigFlags.class))
          {
              lookup.put(flags.getCode(), flags);
          }
        }
        public int getCode()
        {
          return value;
        }

        public static ConfigFlags get(int rep)
        {
          return lookup.get(rep);
        }
    }
}
