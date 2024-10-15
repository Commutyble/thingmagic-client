/*
 * Copyright (c) 2014 ThingMagic, Inc.
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

import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.util.Collections;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;
import java.util.Set;
import java.util.TreeSet;

/**
 *
 * 
 */
public class LoadSaveConfig
{
    
    Map<String,String> configurationMap = new HashMap<String,String>();
    public Properties prop;
    
    public void loadConfigurations(String filePath) throws Exception
    {

        if (configurationMap.isEmpty())
        {
            createLoadSaveConfigMapping();
        }
        prop = new Properties();
        File configFile = new File(filePath);
        if (!configFile.exists())
        {
            throw new FileNotFoundException("Unable to find the configuration properties file in " + filePath);
        }
        FileInputStream fis =  new FileInputStream(configFile);
        prop.load(fis);
        fis.close();
        validateParameters(prop);
    }
    
    public void validateParameters(Properties prop) throws Exception
    {
       Set<Object> keySet = prop.keySet();
       for(String param : configurationMap.keySet())
       {
           if(keySet.contains(param))
           { 
               
               if("".equals(prop.get(param)))
               {
                  throw new Exception("Configuration setting : "+ param +" can not be empty. "
                          + "\n Please enter a value for this setting and reload the configuration."); 
               }
           }
           else
           {
               throw new Exception("Configuration setting is missing: ["+ param+"]");
           }           
       }
    }
    
    public void saveConfigurations(String filePath, Map<String,String>savedParams)
    {
        try
        {
            if (configurationMap.isEmpty())
            {
                createLoadSaveConfigMapping();
            }
          
            prop = new Properties(){
                @Override
                public synchronized Enumeration<Object> keys()
                {
                    return Collections.enumeration(new TreeSet<Object>(super.keySet()));
                }
            };
            for (String param : savedParams.keySet())
            {
                prop.put(param, savedParams.get(param));
            }
           
            String osname = System.getProperty("os.name").toLowerCase();
            if(osname.equalsIgnoreCase("linux"))
            {
                filePath = filePath +".urac"; 
            }
            
            File file = new File(filePath);
            OutputStream out = new FileOutputStream(file);
            prop.store(out, "Configuration parameters");
            out.close();
        }
        catch (Exception ex)
        {

        }
    }
    
    public void createLoadSaveConfigMapping()
    {
        configurationMap.put("/application/readwriteOption/Protocols", "// Set protocols Gen2, ISO18000-6B, IPX64, IPX256. \n "
                + "// For ex: /application/readwriteOption/Protocols=Gen2(Default), "
                + "/application/readwriteOption/Protocols=Gen2,IPX64,IPX256 ");

        configurationMap.put("/application/readwriteOption/Antennas", "// Set antenna. For ex: /application/readwriteOption/Antennas=1, "
                + " /application/readwriteOption/Antennas=1,2,3,4");

        configurationMap.put("/reader/antenna/checkPort", "// Enable or disable antenna detection. true - Enable false - Disable");
        
        configurationMap.put("/application/readwriteOption/enableAutonomousRead", "// Enable or disable AutonomousRead. \n true - Enable false - Disable");

        configurationMap.put("/application/readwriteOption/enableAutoReadGPI", "// Enable or disable auto read on gpi. \n true - Enable false - Disable");

        configurationMap.put("/application/readwriteOption/autoReadgpiPin", "// Set gpi pin. For ex:/application/readwriteOption/autoReadgpiPin=1");

        configurationMap.put("/application/readwriteOption/enableEmbeddedReadData", "// Add an embedded ReadData TagOp to Inventory."
                + " true - Enable false - Disable");

        configurationMap.put("/application/readwriteOption/enableEmbeddedReadData/MemBank", "// Select the Memory Bank to read from."
                + " Options available are TID, Reserved, EPC, User. "
                + "// For ex: /application/readwriteOption/enableEmbeddedReadData/MemBank=TID, "
                + "/application/readwriteOption/enableEmbeddedReadData/MemBank=Reserved ");
        
        configurationMap.put("/reader/tagReadData/uniqueByData","// Makes Data read a unique characteristic of a Tag Results Entry."
               +"\n// true - Enable false - Disable");
        
        configurationMap.put("/application/readwriteOption/enableEmbeddedReadData/StartAddress","// Starting Word Address to read "
               +  "// For ex: /application/readwriteOption/enableEmbeddedReadData/StartAddress=0");
        
        configurationMap.put("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead","// Number of Words to read in "
               +" 0 start address and 0 length = Full"
               + "// For ex: /application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead=0");


        configurationMap.put("/reader/radio/readPower", "// Set read power in cdBm. For ex: /reader/radio/readPower=1000, "
                + "/reader/radio/readPower=3000, /reader/radio/readPower=-1000");

        configurationMap.put("/reader/radio/writePower", "// Set read power in cdBm. For ex: /reader/radio/writePower=1000, "
                + "/reader/radio/writePower=3000, /reader/radio/writePower=-1000");

        configurationMap.put("/reader/gen2/BLF", "// Set BLF LINK250KHZ, LINK640KHZ."
                + "// For ex: /reader/gen2/BLF=LINK250KHZ ");
     
        configurationMap.put("/reader/gen2/tagEncoding", "// Set tag encoding FM0, M2, M4, M8"
                + "// For ex: /reader/gen2/tagEncoding=FM0 ");
      
        configurationMap.put("/reader/gen2/session", "// Set session S0, S1, S2, S3"
                + "// For ex: /reader/gen2/session=S0");
       
        configurationMap.put("/reader/gen2/target", "// Set target A, B, AB, BA."
                + "// For ex: /reader/gen2/target=A");
       
        configurationMap.put("/application/displayOption/tagResultColumnSelection/enableAntenna", "// Antenna column to be "
                +" displayed on tag results.  \n true - Enable false - Disable ");
        
        configurationMap.put("/application/displayOption/tagResultColumnSelection/enableProtocol", "// Protocol column to be "
                +" displayed on tag results.  \n true - Enable false - Disable ");
    
        configurationMap.put("/application/displayOption/tagResultColumnSelection/enableFrequency", "// Frequency column to "
                +" be displayed on tag results.  \n true - Enable false - Disable ");
       
        configurationMap.put("/application/displayOption/tagResultColumnSelection/enablePhase", "// Phase column to be displayed "
                +" on tag results.  \n true - Enable false - Disable ");          
        
        configurationMap.put("/reader/read/asyncOffTime", "// Set Async off time in ms. For ex: /reader/read/asyncOffTime = 250."); 
        
        configurationMap.put("/reader/read/asyncOnTime", "// Set Async on time in ms. For ex: /reader/read/asyncOnTime = 250."); 
    }
}
