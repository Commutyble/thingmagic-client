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

import java.util.EnumMap;
import com.thingmagic.SerialReader.StatusReport;
import java.util.Vector;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.io.InputStream;
import java.io.IOException;
import java.util.Collections;
import static com.thingmagic.TMConstants.*;
import java.util.EnumSet;
import java.util.LinkedHashMap;
import java.util.Set;

/**
 * The Reader class encapsulates a connection to a ThingMagic RFID
 * reader device and provides an interface to perform RFID operations
 * such as reading tags and writing tag IDs. Reads can be done on
 * demand, with the {@link #read} method, or continuously in the
 * background with the {@link #startReading} method. Background reads
 * notify a listener objects of tags that are read.
 *
 * Methods which communicate with the reader can throw ReaderException
 * if the communication breaks down. Other reasons for throwing
 * ReaderException are documented in the individual methods.
 *
 * Operations which take an argument for a tag to operate on may
 * optionally be passed a null argument. This lets the reader choose
 * what tag to use, but may not work if multiple tags are
 * present. This use is recommended only when exactly one tag is known
 * to be in range.
 */
abstract public class Reader
{

  final protected List<ReadListener> readListeners;
  final protected List<ReadExceptionListener> readExceptionListeners;
  final protected List<ReadAuthenticationListener> readAuthenticationListener;
  protected List<StatusListener> statusListeners;
  protected List<StatsListener> statsListeners;
  boolean connected;
  Thread readerThread, notifierThread, exceptionNotifierThread;
  BackgroundReader backgroundReader;
  ContinuousReader continuousReader;
  BackgroundNotifier backgroundNotifier;
  ExceptionNotifier exceptionNotifier;
  final BlockingQueue<TagReadData> tagReadQueue;
  final BlockingQueue<ReaderException> exceptionQueue;
  Map<String,Setting> params;
  Map<StatusListener,StatusReport> statusMap;
  URI uri;
  boolean isTrueAsyncStopped=false;
  int transportTimeout = 5000;
  boolean finishedReading = false;
  int commandTimeout = 1000;
  boolean userTransportTimeout = false;
  public static TransportListener simpleTransportListener;
  long timeStart, timeEnd;
  static Map<String,ReaderFactory> readerFactoryDispatchTable  = new HashMap<String,ReaderFactory>();
  LoadSaveConfiguration loadsaveConfig;
  List<String> readOnlyParameters;
  boolean sleepContRead;
  public boolean hasContinuousReadStarted = false;
  public Set<ReaderFeaturesFlag> featuresFlag = EnumSet.noneOf(ReaderFeaturesFlag.class);
  public boolean isOffTimeAdded = false;
  public int subOffTime = 0;
  public long tagFetchTime = 0;
  // This flag is used to calculate tagFetchtime in psuedo async multireadplan case.
  public boolean fetchTagReads = false;
  public boolean probeSettingPassed = false;
  // variable to hold the last reported exception from the reader
  public ReaderException lastReportedException = null;

  /**
   * RFID regulatory regions
   */
  public enum Region
  {
      /** Region not set */ UNSPEC,
      /** European Union */ EU, 
      /** India */ IN,
      /** Japan */ JP, 
      /** Korea */ KR, 
      /** Korea (revised) */ KR2, 
      /** North America */ NA, 
      /** China */ PRC,
      /** China(840MHZ) */ PRC2,
      /** European Union (revised) */ EU2, 
      /** European Union (revised) */ EU3,
      /** Australia */ AU,
      /** New Zealand !!EXPERIMENTAL!! */ NZ,
      /** No-limit region */ OPEN,
      /** Unrestricted access to full hardware range */ MANUFACTURING,
      /** Reduced FCC region */ NA2 ,
      /** 5MHZ FCC band */ NA3 ,
      /** Israel applicable for Micro, M6e-JIC, M6ePlus */ IS,
      /** Malaysia */MY,
      /** Indonesia */ID,
      /** Philippines */PH,
      /** Taiwan */TW,
      /** Macau */MO,
      /** Russia */RU,
      /** Singapore */SG,
      /** Japan2 (24dBm with 13 channels) */JP2,
      /** Japan3 (24dBm with 6 channels) */JP3,
      /** Vietnam */VN,
      /** Thailand */TH,
      /** Argentina */AR,
      /** Hong Kong */HK,
      /** Bangladesh */BD,
      /** European Union (revised) */ EU4,
      /** Universal region applicable for M3e product */UNIVERSAL,
      /** Israel2(IS2) region applicable for Micro and Nano */IS2,
      /** NA4 region applicable for Micro and M6e */NA4,
      /** OPEN region with extended frequency range 840-960MHz for M6ePlus module**/OPEN_EXTENDED;
  }

  static final Map<Reader.Region, Integer> regionToCodeMap;
  static
  {
    regionToCodeMap = new EnumMap<Reader.Region, Integer>(Reader.Region.class);
    regionToCodeMap.put(Reader.Region.UNSPEC, 0);
    regionToCodeMap.put(Reader.Region.NA, 1);
    regionToCodeMap.put(Reader.Region.EU, 2);
    regionToCodeMap.put(Reader.Region.KR, 3);
    regionToCodeMap.put(Reader.Region.IN, 4);
    regionToCodeMap.put(Reader.Region.JP, 5);
    regionToCodeMap.put(Reader.Region.PRC, 6);
    regionToCodeMap.put(Reader.Region.EU2, 7);
    regionToCodeMap.put(Reader.Region.EU3, 8);
    regionToCodeMap.put(Reader.Region.KR2, 9);
    regionToCodeMap.put(Reader.Region.PRC2, 10);
    regionToCodeMap.put(Reader.Region.AU, 11);
    regionToCodeMap.put(Reader.Region.NZ, 12);
    regionToCodeMap.put(Reader.Region.NA2 , 13);
    regionToCodeMap.put(Reader.Region.NA3 , 14);
    regionToCodeMap.put(Reader.Region.IS , 15);
    regionToCodeMap.put(Reader.Region.MY , 16);
    regionToCodeMap.put(Reader.Region.ID , 17);
    regionToCodeMap.put(Reader.Region.PH , 18);
    regionToCodeMap.put(Reader.Region.TW , 19);
    regionToCodeMap.put(Reader.Region.MO , 20);
    regionToCodeMap.put(Reader.Region.RU , 21);
    regionToCodeMap.put(Reader.Region.SG , 22);
    regionToCodeMap.put(Reader.Region.JP2 , 23);
    regionToCodeMap.put(Reader.Region.JP3 , 24);
    regionToCodeMap.put(Reader.Region.VN , 25);
    regionToCodeMap.put(Reader.Region.TH , 26);
    regionToCodeMap.put(Reader.Region.AR , 27);
    regionToCodeMap.put(Reader.Region.HK , 28);
    regionToCodeMap.put(Reader.Region.BD , 29);
    regionToCodeMap.put(Reader.Region.EU4, 30);
    regionToCodeMap.put(Reader.Region.UNIVERSAL, 31);
    regionToCodeMap.put(Reader.Region.IS2, 32);
    regionToCodeMap.put(Reader.Region.NA4, 33);
    regionToCodeMap.put(Reader.Region.OPEN_EXTENDED , 254);
    regionToCodeMap.put(Reader.Region.OPEN, 255);
  }

  static final Map<Integer, Reader.Region> codeToRegionMap;
  static
  {
    codeToRegionMap = new HashMap<Integer, Reader.Region>();
    codeToRegionMap.put(0, Reader.Region.UNSPEC);
    codeToRegionMap.put(1, Reader.Region.NA);
    codeToRegionMap.put(2, Reader.Region.EU);
    codeToRegionMap.put(3, Reader.Region.KR);
    codeToRegionMap.put(4, Reader.Region.IN);
    codeToRegionMap.put(5, Reader.Region.JP);
    codeToRegionMap.put(6, Reader.Region.PRC);
    codeToRegionMap.put(7, Reader.Region.EU2);
    codeToRegionMap.put(8, Reader.Region.EU3);
    codeToRegionMap.put(9, Reader.Region.KR2);
    codeToRegionMap.put(10, Reader.Region.PRC2);
    codeToRegionMap.put(11, Reader.Region.AU);
    codeToRegionMap.put(12, Reader.Region.NZ);
    codeToRegionMap.put(13, Reader.Region.NA2);
    codeToRegionMap.put(14, Reader.Region.NA3);
    codeToRegionMap.put(15, Reader.Region.IS);
    codeToRegionMap.put(16, Reader.Region.MY);
    codeToRegionMap.put(17, Reader.Region.ID);
    codeToRegionMap.put(18, Reader.Region.PH);
    codeToRegionMap.put(19, Reader.Region.TW);
    codeToRegionMap.put(20, Reader.Region.MO);
    codeToRegionMap.put(21, Reader.Region.RU);
    codeToRegionMap.put(22, Reader.Region.SG);
    codeToRegionMap.put(23, Reader.Region.JP2);
    codeToRegionMap.put(24, Reader.Region.JP3);
    codeToRegionMap.put(25, Reader.Region.VN);
    codeToRegionMap.put(26, Reader.Region.TH);
    codeToRegionMap.put(27, Reader.Region.AR);
    codeToRegionMap.put(28, Reader.Region.HK);
    codeToRegionMap.put(29, Reader.Region.BD);
    codeToRegionMap.put(30, Reader.Region.EU4);
    codeToRegionMap.put(31, Reader.Region.UNIVERSAL);
    codeToRegionMap.put(32, Reader.Region.IS2);
    codeToRegionMap.put(33, Reader.Region.NA4);
    codeToRegionMap.put(254, Reader.Region.OPEN_EXTENDED);
    codeToRegionMap.put(255, Reader.Region.OPEN);
  }

  protected void checkConnection()
    throws ReaderException
  {
    if (connected == false)
    {
      throw new ReaderException("No reader connected to reader object");
    }
  }

  // Level 1 API
  // note no public constructor
  Reader()
  {
    readListeners = new Vector<ReadListener>();
    readExceptionListeners = new Vector<ReadExceptionListener>();
    readAuthenticationListener = new Vector<ReadAuthenticationListener>();
    statusListeners = new ArrayList<StatusListener>();
    statusListeners = Collections.synchronizedList(statusListeners);
    statsListeners = new ArrayList<StatsListener>();
    statsListeners = Collections.synchronizedList(statsListeners);
    statusMap = new HashMap<StatusListener,StatusReport>();
    connected = false;
    sleepContRead = false;
    
    tagReadQueue = new LinkedBlockingQueue<TagReadData>();
    exceptionQueue = new LinkedBlockingQueue<ReaderException>();

    initparams();
  }

  /**
   * Return an instance of a Reader class that is associated with a
   * RFID reader on a particular communication channel. The
   * communication channel is not established until the connect()
   * method is called. Note that some readers may need parameters
   * (such as the regulatory region) set before the connect will
   * succeed.
   *
   * @param uriString an identifier for the reader to connect to, with
   * a URI syntax. The scheme can be <tt>eapi</tt> for the embedded
   * module protocol, <tt>rql</tt> for the request query language, or
   * <tt>tmr</tt> to guess. The remainder of the URI identifies the
   * stream that the protocol will be spoken over, either a local host
   * serial port device or a TCP network port.
   *
   *  Examples include:<tt>
   *  <ul>
   *  <li>"eapi:///dev/ttyUSB0"
   *  <li>"eapi:///com1"
   *  <li>"rql://reader.example.com/"
   *  <li>"rql://proxy.example.com:2500/"
   *  <li>"tmr:///dev/ttyS0"
   *  <li>"tmr://192.168.1.101/"
   *  </ul></tt>
   *
   * @return the {@link Reader} object connected to the specified device
   * @throws ReaderException if reader initialization failed
   */
  public static Reader create(String uriString)
    throws ReaderException
  {    
    Reader reader = null;
    URI uri;

    try
    {
      uri = new URI(uriString);
    } 
    catch (URISyntaxException e)
    {
      throw new ReaderException(e.getMessage());
    }

    String scheme = uri.getScheme();
    String authority = uri.getAuthority();
    String path = uri.getPath();
    String host = uri.getHost();
    int port = uri.getPort();

    if (scheme == null)
    {
      throw new ReaderException("Blank URI scheme");
    }
    
    String javaRunTime = System.getProperty("java.runtime.name");
    if (javaRunTime.equalsIgnoreCase("Android Runtime")) 
    {
       if (scheme.equals("tmr") && uri.toString().contains("///"))
       {
          reader = new SerialReader(path);
          return reader;
       }
    }
    else if (readerFactoryDispatchTable.isEmpty())
    {
          // If the dispatch table is empty add the standard uri schemes 
          readerFactoryDispatchTable.put("eapi", new SerialTransportNative.Factory());
          readerFactoryDispatchTable.put("tmr", new SerialTransportNative.Factory());
    }
    
    if (scheme.equals("tmr"))
    {
      if(!(uri.toString().contains("///")))
      {
        reader = new LLRPReader(uri.getHost(), (uri.getPort()==-1)?5084:uri.getPort());
        if(host!=null && isLLRPReader((LLRPReader)reader))
        {
            scheme = "llrp";
        }
        else if(host != null)
        {
            scheme = "rql";
        }
      }
      else
      {
        scheme = "eapi";
      }
    }

    if (readerFactoryDispatchTable.containsKey(scheme))
    {
        ReaderFactory factory =  readerFactoryDispatchTable.get(scheme);
        reader = factory.createReader(uriString);
    }
    else if (scheme.equals("rql"))
    {
      if (!(path.equals("") || path.equals("/")))
      {
        throw new ReaderException("Path not supported for " + scheme);
      }

      if (port == -1)
      {
        reader = new RqlReader(host);
      }
      else
      {
        reader = new RqlReader(host, port);
      }
    }
    else if (scheme.equals("llrp"))
    {
        if(!_isConnected)
        {
            // llrp - low level reader protocol
            if (port == -1)
            {
                reader = new LLRPReader(host);
            }
            else
            {
                reader = new LLRPReader(host, port);
            }
        }
    }
    else
    {
      throw new ReaderException("Unknown URI scheme");
    }
    reader.uri = uri;


    return reader;
  }
  
  public abstract void receiveAutonomousReading();
  
  public abstract void regionConfiguration(boolean LBTEnable, int LBTThreshold, boolean dwellTimeEnable, int dwellTime) throws ReaderException;
  
  public static void setSerialTransport(String scheme, ReaderFactory factory) throws ReaderException
  {
      try
      {
          if(scheme.equalsIgnoreCase("llrp") || scheme.equalsIgnoreCase("rql"))
          {
             throw new ReaderException("Invalid serial transport scheme");  
          }
          if(readerFactoryDispatchTable.isEmpty())
          {
              readerFactoryDispatchTable.put("eapi", new SerialTransportNative.Factory());
              readerFactoryDispatchTable.put("tmr",  new SerialTransportNative.Factory());
          }
          readerFactoryDispatchTable.put(scheme, factory);
      }
      catch(Exception ex)
      {
         throw new ReaderException(ex.getMessage());
      }
  }
  
  static boolean _isLLRP = true;
  static boolean _isConnected = false;

  protected void setReaderType(boolean isLLRP)
  {     
    _isLLRP = isLLRP;
  }
  protected static boolean isLLRPReader(LLRPReader llrpReader)
  {
      try
      {       
          llrpReader.llrpConnect();
          llrpReader.getReaderCapabilities(LLRPReader.ReaderConfigParams.SOFTWARE_VERSION);
          _isConnected = true;
          return true;
      } 
      catch (ReaderException ex)
      {
          if(!_isLLRP)
          {
              // GET READER CAPABILITIES MESSAGE fails saying UNSUPPORTED for non-llrp readers              
              llrpReader.destroy();
              return false;
          }                         
      }
      return false;
  }

  /**
   * Open the communication channel and initialize the session with
   * the reader.
   */
  public abstract void connect()
    throws ReaderException;

   /**
   * Loads the reader configuration parameters from file and applies to module
     * @param filePath
   */
  public  void loadConfig(String filePath)  throws ReaderException
  {
      loadsaveConfig = new LoadSaveConfiguration();
      loadsaveConfig.loadConfiguration(filePath, this);
      
  }
  
  /**
   * Saves the current reader configuration parameters and its values to a file
   * @param filePath
   */
  public void saveConfig(String filePath) throws ReaderException
  {
      try
      {
        
          if(readOnlyParameters == null)
          {
            String[] paramsList = this.paramList();
            readOnlyParameters = new ArrayList();
            for(String param : paramsList )
            {
                if (false
                    || param.equalsIgnoreCase("/reader/gen2/bap")
                    || param.equalsIgnoreCase("/reader/iso180006b/BLF")
                    || param.equalsIgnoreCase("/reader/description")
                    || param.equalsIgnoreCase("/reader/iso180006b/delimiter")
                    || param.equalsIgnoreCase("/reader/region/lbt/enable")
                    || param.equalsIgnoreCase("/reader/region/lbtThreshold")
                    || param.equalsIgnoreCase("/reader/region/dwellTime/enable")
                    || param.equalsIgnoreCase("/reader/region/dwellTime")
                    || param.equalsIgnoreCase("/reader/radio/enableSJC")
                    || param.equalsIgnoreCase("/reader/radio/enablePowerSave")
                    || param.equalsIgnoreCase("/reader/hostname")
                    || param.equalsIgnoreCase("/reader/licenseKey")
                    || param.equalsIgnoreCase("/reader/manageLicenseKey")
                    || param.equalsIgnoreCase("/reader/iso180006b/modulationDepth")
                    || param.equalsIgnoreCase("/reader/version/serial")
                    || param.equalsIgnoreCase("/reader/stats")
                    || param.equalsIgnoreCase("/reader/statistics")
                    || param.equalsIgnoreCase("/reader/userConfig")
                    || param.equalsIgnoreCase("/reader/userMode")
                    || param.equalsIgnoreCase("/reader/uri")
                    || param.equalsIgnoreCase("/reader/gen2/writeMode")
                    || param.equalsIgnoreCase("/reader/gen2/writeEarlyExit")
                    || param.equalsIgnoreCase("/reader/gen2/writeReplyTimeout")
                    || param.equalsIgnoreCase("/reader/tagop/antenna")
                    || param.equalsIgnoreCase("/reader/regulatory/enable")
                    || param.equalsIgnoreCase("/reader/region/minimumFrequency")
                    || param.equalsIgnoreCase("/reader/region/quantizationStep")
                    || param.equalsIgnoreCase("/reader/stats/enable")
                    || param.equalsIgnoreCase("/reader/metadata"))
                {
                    readOnlyParameters.add(param);
                    continue;
                }
                Setting set;
                if (params.containsKey(param.toLowerCase()))
                {
                    set = params.get(param.toLowerCase());
                    if (!set.writable)
                    {
                        readOnlyParameters.add(param);
                    }
                }
            }
          }
          if(loadsaveConfig == null)
          {
             loadsaveConfig = new LoadSaveConfiguration();
          }         
          loadsaveConfig.saveConfiguration(filePath, this, readOnlyParameters);
      }
      catch(Exception ex)
      {
          throw new ReaderException(ex.getMessage());
      }
  }
  
  /**
   * Shuts down the connection with the reader device.
   */
  public abstract void destroy();

  /**
   * reboots the reader device.
   */
  public abstract void reboot()
     throws ReaderException;
  
  /**
   * Read RFID tags for a fixed duration.
   *
   * @param duration the time to spend reading tags, in milliseconds
   * @return the tags read
   * @see TagReadData
   */ 
  public abstract TagReadData[] read(long duration)
    throws ReaderException;

  /**
   * Read data from the memory bank of a tag. 
   *
   * @param target the tag to read from, or null
   * @param bank the tag memory bank to read from
   * @param address the byte address to start reading at
   * @param count the number of bytes to read
   * @return the bytes read
   *
   */
  public abstract byte[] readTagMemBytes(TagFilter target,
                                         int bank, int address, int count)
    throws ReaderException;


  /**
   * Read data from the memory bank of a tag. 
   *
   * @param target the tag to read from, or null
   * @param bank the tag memory bank to read from
   * @param address the word address to start reading from
   * @param count the number of words to read
   * @return the words read
   *
   */
  public abstract short[] readTagMemWords(TagFilter target,
                                          int bank, int address, int count)
    throws ReaderException;

  /**
   * Write data to the memory bank of a tag.
   *
   * @param target the tag to write to, or null
   * @param bank the tag memory bank to write to
   * @param address the byte address to start writing to
   * @param data the bytes to write
   *
   */
  public abstract void writeTagMemBytes(TagFilter target,
                                        int bank, int address, byte[] data)
    throws ReaderException;


  /**
   * Write data to the memory bank of a tag.
   *
   * @param target the tag to read from, or null
   * @param bank the tag memory bank to write to
   * @param address the word address to start writing to
   * @param data the words to write
   *
   */
  public abstract void writeTagMemWords(TagFilter target,
                                        int bank, int address, short[] data)
    throws ReaderException;

  /**
   * Write a new ID to a tag. 
   *
   * @param target the tag to write to, or null
   * @param newID the new tag ID to write
   */
  public abstract void writeTag(TagFilter target, TagData newID)
    throws ReaderException;

  /**
   * Perform a lock or unlock operation on a tag. The first tag seen
   * is operated on - the singulation parameter may be used to control
   * this. Note that a tag without an access password set may not
   * accept a lock operation or remain locked.
   *
   * @param target the tag to lock, or null
   * @param lock the locking action to take.
   */
  public abstract void lockTag(TagFilter target, TagLockAction lock)
    throws ReaderException;

  /**
   * Kill a tag. The first tag seen is killed.
   *
   * @param target the tag kill, or null
   * @param auth the authentication needed to kill the tag
   */
  public abstract void killTag(TagFilter target, TagAuthentication auth)
    throws ReaderException;
  /** 
   * Register a listener to be notified of asynchronous RFID read events.
   *
   * @param listener the ReadListener to add
   */
  public void addReadListener(ReadListener listener)
  {
      readListeners.add(listener);
  }

  /** 
   * Remove an listener from the list of listeners notified of asynchronous
   * RFID read events.
   *
   * @param listener the ReadListener to remove
   */
  public void removeReadListener(ReadListener listener)
  {
      readListeners.remove(listener);
  }


  /** 
   * Register a listener to be notified of asynchronous RFID read exceptions.
   *
   * @param listener the ReadExceptionListener to add
   */
    public void addReadExceptionListener(ReadExceptionListener listener) {
        readExceptionListeners.add(listener);
    }

  /** 
   * Remove a listener from the list of listeners notified of asynchronous
   * RFID read events.
   *
   * @param listener The ReadExceptionListener to remove
   */
    public void removeReadExceptionListener(ReadExceptionListener listener) {
        readExceptionListeners.remove(listener);
    }
 /**
   * Register a listener to be notified of asynchronous RFID authentication exceptions.
   *
   * @param listener the ReadAuthenticationListener to add
   */
    public void addReadAuthenticationListener(ReadAuthenticationListener listener) {
        readAuthenticationListener.add(listener);
    }

  /**
   * Remove a listener from the list of listeners notified of asynchronous
   * RFID authentication  events.
   *
   * @param listener The ReadAuthenticationListener to remove
   */
    public void removeReadAuthenticationListener(ReadAuthenticationListener listener) {
        readAuthenticationListener.remove(listener);
    }

    /**
   * Start reading RFID tags in the background. The tags found will be
   * passed to the registered read listeners, and any exceptions that
   * occur during reading will be passed to the registered exception
   * listeners. Reading will continue until stopReading() is called.
   *
   * @see #addReadListener
   * @see #addReadExceptionListener
   */
  public abstract void startReading();

   /**
   * Stop reading RFID tags in the background.
   * @return stop reading success or failure
   */
  public abstract boolean stopReading();


  /**
   * Start reading RFID tags in the background. The tags found will be
   * passed to the registered read listeners, and any exceptions that
   * occur during reading will be passed to the registered exception
   * listeners. Reading will continue until stopReading() is called.
   *
   * @see #addReadListener
   * @see #addReadExceptionListener
   */
  protected synchronized void startReadingGivenRead(boolean isContinuous)
  {
    if(isContinuous)
    {        
        if (continuousReader == null)
        {     
            continuousReader = new ContinuousReader();
            readerThread = new Thread(continuousReader, "continuous reader");
            readerThread.setDaemon(true);
            readerThread.start();
        }
    }
    else
    {
        if (backgroundReader == null)
        {
            backgroundReader = new BackgroundReader();
            readerThread = new Thread(backgroundReader, "background reader");
            readerThread.setDaemon(true);
            readerThread.start();
        }
    }
    if (backgroundNotifier == null)
    {
      backgroundNotifier = new BackgroundNotifier();
      notifierThread = new Thread(backgroundNotifier, "background notifier");
      notifierThread.setDaemon(true);
      notifierThread.start();
    }
    if (exceptionNotifier == null)
    {
      exceptionNotifier = new ExceptionNotifier();
      exceptionNotifierThread = new Thread(exceptionNotifier, "exception notifier");
      exceptionNotifierThread.setDaemon(true);
      exceptionNotifierThread.start();
    }
    if(isContinuous)
    {
        continuousReader.readOn();
    }
    else
    {
        backgroundReader.readOn();
    }
  }

  /**
   * Stop reading RFID tags in the background.
   */
  protected void stopReadingGivenRead()
    throws InterruptedException
  {

      if (continuousReader != null) {
          continuousReader.readOff();
          continuousReader = null;
          readerThread.interrupt();
      }

      if (backgroundReader != null) {
          backgroundReader.readOff();
          backgroundReader = null;
          readerThread.interrupt();
      }
      if (backgroundNotifier != null) {
          backgroundNotifier.drainQueue();
          backgroundNotifier = null;
          notifierThread.interrupt();
      }
      if (exceptionNotifier != null) {
          exceptionNotifier.drainQueue();
          exceptionNotifier = null;
          exceptionNotifierThread.interrupt();
      }
  }

    /** This function will return the status
      *  of finishedReading flag
      */
    public boolean isReadStopped()
    {
        /** finishedReading flag will be true only after
          * receiving Stop Read response.
          */
        
        /**If finishedReading is true, make continuousReader, backgroundNotifier 
         * and exceptionNotifier as null to destroy the object
         */
        if(finishedReading)
        {
            continuousReader = null;
            backgroundNotifier = null;
            exceptionNotifier = null;
        }
        return finishedReading;
    }

    void notifyReadListeners(TagReadData t) {

        synchronized (readListeners) {
            for (ReadListener rl : readListeners) {
                rl.tagRead(this, t);
            }
        }
    }

    void notifyExceptionListeners(ReaderException re) {
        synchronized (readExceptionListeners) {
            for (ReadExceptionListener rel : readExceptionListeners) {
                rel.tagReadException(this, re);
            }
        }
    }
     void notifyAuthenticationListeners(TagReadData t,Reader reader) throws Exception {
        synchronized (readAuthenticationListener) {
            for (ReadAuthenticationListener ral : readAuthenticationListener) {
                 ral.readTagAuthentication(t,reader);
            }
        }
    }

    void notifyStatusListeners(StatusReport[] t) {
        synchronized (statusListeners) {
            for (StatusListener rl : statusListeners) {

                rl.statusMessage(this, t);
            }
        }
    }

    void notifyStatsListeners(SerialReader.ReaderStats rs)
    {
        synchronized (statsListeners)
        {
            for (StatsListener sl : statsListeners)
            {
                sl.statsRead(rs);
            }
        }
    }
  
  public static class GpioPin
  {
    // The class is immutable, so there's potential for caching the objects,
    // as per the "flyweight" pattern, if necessary.
    final public int id;
    final public boolean high;
    final public boolean output;

    public GpioPin(int id, boolean high)
    {
      this.id = id;
      this.high = high;
      this.output = false;
    }

    public GpioPin(int id, boolean high, boolean output)
    {
      this.id = id;
      this.high = high;
      this.output = output;
    }

    @Override public boolean equals(Object o)
    {
      if (!(o instanceof GpioPin))
      {
        return false;
      }
      GpioPin gp = (GpioPin)o;
      return (this.id == gp.id) && (this.high == gp.high) && (this.output == gp.output);
    }

    @Override
    public int hashCode()
    {
      int x;
      x = id * 2;
      if (high)
      {
        x++;
      }
      return x;
    }

    @Override
    public String toString()
    {
      return String.format("%2d%s%s", this.id, this.high?"H":"L", this.output?"O":"I");
    }

  }

  /**
   * Get the state of all of the reader's GPI pins.
   *
   * @return array of GpioPin objects representing the state of all input pins
   */
  public abstract GpioPin[] gpiGet()
    throws ReaderException;

  /**
   * Set the state of some GPO pins.
   *
   * @param state Array of GpioPin objects
   */
  public abstract void gpoSet(GpioPin[] state)
    throws ReaderException;

  /*
   * Quirk of Settings: if the type is modifiable (not primitive,
   * primitive-wrapper, or a String), then you have to be careful
   * about what is returned by paramGet, so that the caller can't
   * change the reader state by changing the returned object. As
   * there's no universal deep-copy in Java, we'll all just have to do
   * it ourselves in the get operation.
   */

  interface SettingAction
  {
    Object set(Object value)
      throws ReaderException;
    Object get(Object value)
      throws ReaderException;
  }

  abstract class ReadOnlyAction implements SettingAction
  {
    public Object set(Object value)
    {
      throw new UnsupportedOperationException();
    }
    public Object get(Object value)
      throws ReaderException
    {
      return value;
    }
  }

  class Setting
  {
    String originalName;
    Class type;
    Object value;
    SettingAction action;
    boolean writable;
    boolean confirmed;

    Setting(String name, Class t, Object def, boolean w, SettingAction act, boolean confirmed)
    {
      originalName = name;
      type = t;
      value = def;
      writable = w;
      
      action = act;
      this.confirmed = confirmed;
    }
  }


  void initparams()
  {
    Setting s;

    params = new LinkedHashMap<String, Setting>();
    addParam(TMR_PARAM_READ_ASYNCONTIME,
             Integer.class, 250, true,
             new SettingAction() {

            public Object set(Object value) throws ReaderException {
                if((Integer)value < 0 || (Integer)value > 65535)
                {
                    throw new IllegalArgumentException("Value of " + value + " to the parameter /reader/read/asyncOnTime is out of range." );
                }
                return value;
            }

            public Object get(Object value) throws ReaderException {
                return value;
            }
        }
             );
    addParam(TMR_PARAM_READ_ASYNCOFFTIME,
               Integer.class, 0, true,
               new SettingAction() {

            public Object set(Object value) throws ReaderException {
                if((Integer)value < 0 || (Integer)value > 65535)
                {
                    throw new IllegalArgumentException("Value of " + value + " to the parameter /reader/read/asyncOffTime is out of range." );
                }
                return value;
            }

            public Object get(Object value) throws ReaderException {
                return value;
            }
        });
        addParam(TMR_PARAM_GEN2_ACCESSPASSWORD,
             Gen2.Password.class, new Gen2.Password(0), true,
             new SettingAction()
             {
               public Object set(Object value)
               {
                 if (value == null)
                   value = new Gen2.Password(0);
                 else
                    value = ((Gen2.Password)value);//.value;
                 return value;
               }
               public Object get(Object value)
               {
                 return value;
               }
             });

      addParam(TMR_PARAM_READER_URI, String.class, null , true, 
              new ReadOnlyAction()
              {            
                public Object get(Object value) throws ReaderException 
                {
                    return uri.toString();
                }                
              });
      addParam(TMR_PARAM_TRANSPORTTIMEOUT,
              Integer.class, transportTimeout, true,
              new SettingAction() {

                  public Object set(Object value) throws ReaderException {
                      if ((Integer) value < 0 || (Integer) value > 65535) {
                          throw new IllegalArgumentException("Negative Transport timeout value not supported ");
                      }
                      userTransportTimeout = true;
                      transportTimeout = (Integer) value;
                      return transportTimeout;
                  }

                  public Object get(Object value) {
                      return transportTimeout;
                  }
              });
      addParam(TMR_PARAM_COMMANDTIMEOUT,
              Integer.class, commandTimeout, true,
              new SettingAction()
              {
                  public Object set(Object value)
                  {
                      if ((Integer) value < 0 || (Integer) value > 65535)
                      {
                          throw new IllegalArgumentException("Negative Command Timeout " + value);
                      }
                      commandTimeout = (Integer) value;
                      return value;
                  }

                  public Object get(Object value)
                  {
                      return commandTimeout;
                  }
              });
  }

  void addParam(String name, Class t, Object def, boolean w, SettingAction act)
  {
    if (def != null && t.isInstance(def) == false)
      throw new IllegalArgumentException("Wrong type for parameter initial value");
    Setting s = new Setting(name, t, def, w, act, true);
    params.put(name.toLowerCase(), s);
  }

  void addUnconfirmedParam(String name, Class t, Object def, boolean w, SettingAction act)
  {
    if (def != null && t.isInstance(def) == false)
      throw new IllegalArgumentException("Wrong type for parameter initial value");
    Setting s = new Setting(name, t, def, w, act, false);
    params.put(name.toLowerCase(), s);
  }

  /**
   * Get a list of the parameters available
   *
   * <p>Supported Parameters:
   * <ul>
   * <li> /reader/antenna/checkPort
   * <li> /reader/antenna/checkport
   * <li> /reader/antenna/connectedPortList
   * <li> /reader/antenna/perAntennaTime
   * <li> /reader/antenna/portList
   * <li> /reader/antenna/portSwitchGpos
   * <li> /reader/antenna/portswitchgpos
   * <li> /reader/antenna/returnLoss
   * <li> /reader/antenna/settlingTimeList
   * <li> /reader/antenna/txRxMap
   * <li> /reader/antennaMode
   * <li> /reader/baudRate
   * <li> /reader/commandTimeout
   * <li> /reader/currentTime
   * <li> /reader/description
   * <li> /reader/extendedEPC
   * <li> /reader/extendedepc
   * <li> /reader/gen2/BLF
   * <li> /reader/gen2/accessPassword
   * <li> /reader/gen2/accesspassword
   * <li> /reader/gen2/bap
   * <li> /reader/gen2/blf
   * <li> /reader/gen2/initQ
   * <li> /reader/gen2/protocolExtension
   * <li> /reader/gen2/protocolextension
   * <li> /reader/gen2/q
   * <li> /reader/gen2/rfMode
   * <li> /reader/gen2/sendSelect
   * <li> /reader/gen2/sendselect
   * <li> /reader/gen2/session
   * <li> /reader/gen2/t4
   * <li> /reader/gen2/tagEncoding
   * <li> /reader/gen2/tagencoding
   * <li> /reader/gen2/target
   * <li> /reader/gen2/tari
   * <li> /reader/gen2/writeEarlyExit
   * <li> /reader/gen2/writeMode
   * <li> /reader/gen2/writeReplyTimeout
   * <li> /reader/gen2/writeearlyexit
   * <li> /reader/gpio/inputList
   * <li> /reader/gpio/outputList
   * <li> /reader/hostname
   * <li> /reader/iso14443a/supportedTagFeatures
   * <li> /reader/iso14443a/supportedTagTypes
   * <li> /reader/iso14443a/tagType
   * <li> /reader/iso14443a/tagtype
   * <li> /reader/iso14443b/supportedTagTypes
   * <li> /reader/iso14443b/tagType
   * <li> /reader/iso14443b/tagtype
   * <li> /reader/iso15693/supportedTagFeatures
   * <li> /reader/iso15693/supportedTagTypes
   * <li> /reader/iso15693/tagType
   * <li> /reader/iso15693/tagtype
   * <li> /reader/iso180006b/BLF
   * <li> /reader/iso180006b/delimiter
   * <li> /reader/iso180006b/modulationDepth
   * <li> /reader/lf125khz/secureRdFormat
   * <li> /reader/lf125khz/supportedTagFeatures
   * <li> /reader/lf125khz/supportedTagTypes
   * <li> /reader/lf125khz/tagType
   * <li> /reader/lf125khz/tagtype
   * <li> /reader/lf134khz/supportedTagTypes
   * <li> /reader/lf134khz/tagType
   * <li> /reader/lf134khz/tagtype
   * <li> /reader/licenseKey
   * <li> /reader/manageLicenseKey
   * <li> /reader/metadata
   * <li> /reader/metadata\
   * <li> /reader/powerMode
   * <li> /reader/powermode
   * <li> /reader/probeBaudRates
   * <li> /reader/protocolList
   * <li> /reader/protocollist
   * <li> /reader/radio/enablePowerSave
   * <li> /reader/radio/enableSJC
   * <li> /reader/radio/enablepowersave
   * <li> /reader/radio/enablesjc
   * <li> /reader/radio/portReadPowerList
   * <li> /reader/radio/portWritePowerList
   * <li> /reader/radio/powerMax
   * <li> /reader/radio/powerMin
   * <li> /reader/radio/readPower
   * <li> /reader/radio/temperature
   * <li> /reader/radio/writePower
   * <li> /reader/read/asyncOffTime
   * <li> /reader/read/asyncOnTime
   * <li> /reader/read/plan
   * <li> /reader/read/trigger/gpi
   * <li> /reader/region/dwellTime
   * <li> /reader/region/dwellTime/enable
   * <li> /reader/region/hopTable
   * <li> /reader/region/hopTime
   * <li> /reader/region/id
   * <li> /reader/region/lbt/enable
   * <li> /reader/region/lbtThreshold
   * <li> /reader/region/minimumFrequency
   * <li> /reader/region/quantizationStep
   * <li> /reader/region/supportedRegions
   * <li> /reader/regulatory/enable
   * <li> /reader/regulatory/mode
   * <li> /reader/regulatory/modulation
   * <li> /reader/regulatory/offTime
   * <li> /reader/regulatory/onTime
   * <li> /reader/statistics
   * <li> /reader/stats
   * <li> /reader/stats/enable
   * <li> /reader/status/antennaEnable
   * <li> /reader/status/antennaenable
   * <li> /reader/status/frequencyEnable
   * <li> /reader/status/frequencyenable
   * <li> /reader/status/temperatureEnable
   * <li> /reader/status/temperatureenable
   * <li> /reader/tagReadData/enableReadFilter
   * <li> /reader/tagReadData/readFilterTimeout
   * <li> /reader/tagReadData/recordHighestRssi
   * <li> /reader/tagReadData/reportRssiInDbm
   * <li> /reader/tagReadData/tagopFailures
   * <li> /reader/tagReadData/tagopSuccesses
   * <li> /reader/tagReadData/uniqueByAntenna
   * <li> /reader/tagReadData/uniqueByData
   * <li> /reader/tagReadData/uniqueByProtocol
   * <li> /reader/tagop/antenna
   * <li> /reader/tagop/protocol
   * <li> /reader/tagreaddata/enablereadfilter
   * <li> /reader/tagreaddata/recordhighestrssi
   * <li> /reader/tagreaddata/reportrssiIndbm
   * <li> /reader/tagreaddata/reportrssiindbm
   * <li> /reader/tagreaddata/uniquebyantenna
   * <li> /reader/tagreaddata/uniquebydata
   * <li> /reader/tagreaddata/uniquebyprotocol
   * <li> /reader/transportTimeout
   * <li> /reader/uri
   * <li> /reader/userConfig
   * <li> /reader/userMode
   * <li> /reader/version/hardware
   * <li> /reader/version/model
   * <li> /reader/version/productGroup
   * <li> /reader/version/productGroupID
   * <li> /reader/version/productID
   * <li> /reader/version/serial
   * <li> /reader/version/software
   * <li> /reader/version/supportedProtocols
   * </ul>
   *
   * @return an array of the parameter names
   */
  public String[] paramList()
  {
    Vector<String> nameVec = new Vector<String>();
    int i = 0;
    // Create a new container so that removing entries isn't a problem
    for (Setting s : new ArrayList<Setting>(params.values()))
    {
      if (s.confirmed == false)
      {
        if (probeSetting(s) == false)
        {
          continue;
        }
      }
      nameVec.add(s.originalName);
    }
    return nameVec.toArray(new String[nameVec.size()]);
  }

  boolean probeSetting(Setting s)
  {
    try
    {
        s.value = s.action.get(s.value);
        s.confirmed = true;
        probeSettingPassed = true;
    }
    catch (ReaderException e)
    {
        probeSettingPassed = false;
    }

    if (s.confirmed == false)
    {
      params.remove(s.originalName);
    }
      
    return s.confirmed;
  }

  /** execute a TagOp */
   public abstract Object executeTagOp(TagOp tagOP,TagFilter target) throws ReaderException;



  /**
   * Get the value of a Reader parameter.
   *
   * @param key the parameter name
   * @return the value of the parameter, as an Object
   * @throws IllegalArgumentException if the parameter does not exist
   */
  public Object paramGet(String key)
    throws ReaderException
  {
    Setting s;
    probeSettingPassed = false;
    if(hasContinuousReadStarted)
    {
        sleepContRead = true;
    }
    s = params.get(key.toLowerCase());
    if (s == null)
    {
      throw new IllegalArgumentException("No parameter named '" + key + "'.");
    }

    // Maybe mention here that the parameter doesn't work here rather than
    // that it doesn't exist?
    if (s.confirmed == false && probeSetting(s) == false)
    {
      throw new IllegalArgumentException("No parameter named '" + key + "'.");
    }
    
    if(probeSettingPassed & s.value != null)
    {
       return s.value; 
    }
    else
    {
        if (s.action != null)
        {
            s.value = s.action.get(s.value);
        }
        sleepContRead = false;
        return s.value;
    }
  }

  /**
   * Set the value of a Reader parameter.
   *
   * @param key the parameter name
   * @param value value of the parameter, as an Object
   * @throws IllegalArgumentException if the parameter does not exist,
   * is read-only, or if the Object is the wrong type for the
   * parameter.
   */
  public void paramSet(String key, Object value)
    throws ReaderException
  {
    Setting s;

    if(hasContinuousReadStarted)
    {
        sleepContRead = true;
    }
    s = params.get(key.toLowerCase());
    if (s == null)
    {
      throw new IllegalArgumentException("No parameter named '" + key + "'.");
    }
    // Maybe mention here that the parameter doesn't work here rather than
    // that it doesn't exist?
    if (s.confirmed == false && probeSetting(s) == false)
    {
      throw new IllegalArgumentException("No parameter named '" + key + "'.");
    }
    if (s.writable == false)
    {
      throw new IllegalArgumentException("Parameter '" + key + "' is read-only.");
    }
    if (value != null && !s.type.isInstance(value))
    {
      throw new IllegalArgumentException("Wrong type " + value.getClass().getName() + 
                                         " for parameter '" + key + "'.");
    }
    if (s.action != null)
    {
      value = s.action.set(value);
    }
    s.value = value;
    sleepContRead = false;
  }

  /**
   * Load a new firmware image into the device's nonvolatile memory.
   * This installs the given image data onto the device and restarts
   * it with that image. The firmware must be of an appropriate type
   * for the device. Interrupting this operation may damage the
   * reader.
   *
   * @param firmware a data stream of the firmware contents
   */
  public abstract void firmwareLoad(InputStream firmware)
    throws ReaderException, IOException;

  /**
   * Load a new firmware image into the device's nonvolatile memory.
   * This installs the given image data onto the device and restarts
   * it with that image. The firmware must be of an appropriate type
   * for the device. Interrupting this operation may damage the
   * reader.
   *
   * @param firmware a data stream of the firmware contents
   */
  public abstract void firmwareLoad(InputStream firmware,FirmwareLoadOptions loadOptions)
    throws ReaderException, IOException;

    class BackgroundNotifier implements Runnable {

    public void run()
    {
      TagReadData t;
      try
      {
        while (true) {
          synchronized (tagReadQueue)
          {
            if (tagReadQueue.isEmpty())
            {
              tagReadQueue.notifyAll();
            }
          }
          t = tagReadQueue.take();
          notifyReadListeners(t);
        }
      }
      catch(InterruptedException ex)
      {
        // stopReading uses interrupt() to kill this thread;
        // users should not see that exception
      }
    }

    void drainQueue()
      throws InterruptedException
    {
      synchronized (tagReadQueue)
      {
        while (tagReadQueue.isEmpty() == false)
        {
          tagReadQueue.wait();
        }
      }
    }
  }

  class ExceptionNotifier implements Runnable
  {
    public void run()
    {
      try
      {
        while (true) {
          synchronized (exceptionQueue)
          {
            if (exceptionQueue.isEmpty())
            {
              exceptionQueue.notifyAll();
            }
          }
          ReaderException re = exceptionQueue.take();
          notifyExceptionListeners(re);
        }
      }
      catch(InterruptedException ex)
      {
        // stopReading uses interrupt() to kill this thread;
        // users should not see that exception
      }
    }

    void drainQueue()
      throws InterruptedException
    {
      synchronized (exceptionQueue)
      {
        while (exceptionQueue.isEmpty() == false)
        {
          exceptionQueue.wait();
        }
      }
    }
  }

  class BackgroundReader implements Runnable
  {
    boolean enabled, running;
    
    public void run()
    {
      TagReadData[] tags;
      int readTime;
      long sleepTime;
      try
      {
        while (true)
        {
          synchronized (this)
          {
            running = false;
            this.notifyAll();  // Notify change in running
            while (enabled == false)
            {
              this.wait();  // Wait for enabled to change
            }
            running = true;
            this.notifyAll();  // Notify change in running
          }
          try
          {
            readTime = (Integer)paramGet(TMR_PARAM_READ_ASYNCONTIME);
            sleepTime = (Integer)paramGet(TMR_PARAM_READ_ASYNCOFFTIME);
            fetchTagReads = true;
            tags = read(readTime);
            if(!isOffTimeAdded)
            {
                for (TagReadData t : tags)
                {
                  tagReadQueue.put(t);
                }
                timeEnd = System.currentTimeMillis();
                tagFetchTime = timeEnd - timeStart;
                sleepTime = sleepTime - tagFetchTime;
                if (sleepTime > 0)
                {
                  Thread.sleep(sleepTime);
                }
            }
          } 
          catch (ReaderException re)
          {
            exceptionQueue.put(re);
            enabled = false;
            running = false;
          }
        }
      } 
      catch (InterruptedException ie)
      {
        Thread.currentThread().interrupt();
        running = false;
        enabled = false;
      }
    }

    synchronized void readOn()
    {
      enabled = true;
      this.notifyAll();  // Notify change in enabled
    }

    synchronized void readOff()
    {
      enabled = false;
      try
      {
        while (running == true)
        {
          this.wait();  // Wait for running to change
        }
      }
      catch (InterruptedException ie)
      {
        Thread.currentThread().interrupt();
      }
    }

  }


  class ContinuousReader implements Runnable
  {
    public boolean enabled;
    boolean running, trueReading = true;
    boolean _continuousReading = false;

    ContinuousReader()
    {
        _continuousReading = true;
    }

    public void run()
    {      
      int readTime, sleepTime;
      try
      {
          while (!isTrueAsyncStopped) // true async is not stopped
          {
              synchronized (this)
              {
                  running = false;
                  this.notifyAll();  // Notify change in running
                  while (enabled == false)
                  {
                      this.wait();  // Wait for enabled to change
                  }
                  running = true;
                  this.notifyAll();  // Notify change in running
              }
              try
              {
                  readTime = (Integer) paramGet(TMR_PARAM_READ_ASYNCONTIME);

                  if (trueReading)
                  {
                      // 2f (true continuous reading) command is sent only once
                      read(readTime);
                      trueReading = false;
                  }
              }
              catch(IllegalArgumentException ie)
              {
                  System.out.println(ie.getMessage());
              }
              catch(UnsupportedOperationException ue)
              {
                  System.out.println(ue.getMessage()); // handle the exception here and exit
                  System.exit(0);
              }
              catch (ReaderException re)
              {                  
                  if(re instanceof ReaderCodeException && ((ReaderCodeException) re).code == EmbeddedReaderMessage.FAULT_TAG_ID_BUFFER_FULL)
                  {
                    trueReading = true;
                    hasContinuousReadStarted = false;
                  }
                  else if(re instanceof ReaderCommException)
                  {                   
                    if(re.getMessage().equalsIgnoreCase("Timeout") || re.getMessage().equalsIgnoreCase("Invalid argument")
                            || re.getMessage().startsWith("Device was reset externally.")
                            || re.getMessage().startsWith("Connection lost")
                            || re.getMessage().equalsIgnoreCase("No soh FOund"))
                    {
                        notifyExceptionListeners(re);
                        running = false;
                        _continuousReading = false;
                        //stopReadingGivenRead();
                    }
                  }
                  else if(re.getMessage().contains("No connected antennas found"))
                  {
                    // do nothing, just notify listeners
                  }
                  else if(re.getMessage().contains("Command out of index range"))
                  {
                      running = false;
                      enabled = false;
                      _continuousReading = false;
                  }
                  else
                  {
                    running = false;
                    enabled = false; 
                  }
                  exceptionQueue.put(re);
              }
          }//end of while
      }
      catch (InterruptedException ie)
      {
        Thread.currentThread().interrupt();
        running = false;
        enabled = false;
      }
    }

    synchronized void readOn()
    {
      enabled = true;
      this.notifyAll();  // Notify change in enabled
    }

    synchronized void readOff()
    {     
      try
      {
        if(!running && _continuousReading)
        {
            this.wait();
        }
        enabled = false;
      
        while (running == true)
        {
          this.wait();  // Wait for running to change
        }
      }
      catch (InterruptedException ie)
      {
        Thread.currentThread().interrupt();
      }
    }

  }
  
  /** Enum values for License key operations */
  public enum LicenseOption
  {
      /**
       * Sets the license key
       */
      SET_LICENSE_KEY(1),
      /**
       * Erases the license key
       */
      ERASE_LICENSE_KEY(2);
      
      int rep; 
      LicenseOption(int rep)
      {
          this.rep = rep;
      }

      private static final Map<Integer, LicenseOption> lookup = new HashMap<Integer, LicenseOption>();
      static
      {
          for (LicenseOption option : EnumSet.allOf(LicenseOption.class))
          {
              lookup.put(option.getCode(), option);
          }
      }
      public int getCode()
      {
          return rep;
      }
      public static LicenseOption get(int rep)
      {
          return lookup.get(rep);
      }
  }

    /** Class for License key operation*/
    public static class LicenseOperation
    {
        /** option for license operation*/
        public LicenseOption option;
        
        /** license key for setting license to the module */
        public byte[] key;

        public LicenseOperation() 
        {
        }
    }

    /**
     * Enums for Regulatory mode
     */
    public enum RegulatoryMode 
    {
        CONTINUOUS,
        TIMED;
    }

    /**
     * Enums for Regulatory modulation
     */
    public enum RegulatoryModulation 
    {
        CW(1),
        PRBS(2);
        int value;
        RegulatoryModulation(int value)
        {
            this.value = value;
        }
    }

    /**
     * Reader features flag enum
     */
    public enum ReaderFeaturesFlag
    {
        READER_FEATURES_FLAG_NONE(0),
        READER_FEATURES_FLAG_DUTY_CYCLE(1),// Serial Reader
        READER_FEATURES_FLAG_MULTI_SELECT(2),// Serial Reader
        READER_FEATURES_FLAG_ANTENNA_READ_TIME(4), //Both Serial and LLRP Reader
        READER_FEATURES_FLAG_INVENTORYSPEC_ID(8), //LLRP Reader
        READER_FEATURES_FLAG_EXTENDED_LOGICAL_ANTENNA(32),// Serial Reader
        READER_FEATURES_FLAG_READER_METADATA(64), //LLRP Reader
        READER_FEATURES_FLAG_READER_STATS(128), // LLRP Reader
        READER_FEATURES_FLAG_STATE_AWARE(256), //LLRP Reader
        READER_FEATURES_FLAG_ADDR_BYTE_EXTENSION(512); //Serial Reader (M3e)
        int value;

        ReaderFeaturesFlag(int value)
        {
            this.value = value;
        }
    }
    
    // Memory Type - Type of memory operation
    public enum MemoryType
    {
        // Tag memory- Both read and write are supported
        TAG_MEMORY(0x21),
        // Tag Information - Only read is supported 
        TAG_INFO(0x22),
        // Protection security status of tag - only read is supported
        PROTECTION_SECURITY_STATUS(0x23),
        //  Secure id of tag - only read is supported
        SECURE_ID(0x24),
        // Used for extended tag operations - Both read and write are supported
        EXT_TAG_MEMORY(0x25);
        int value;
        
        MemoryType(int value)
        {
            this.value = value;
        }
    }
    
    // WriteMemory
    public static class WriteMemory extends ExtTagOp 
   {
        // the type of memory operation
        public MemoryType memType;
        // the block number to start write data into
        public int address;
        // the data to be written
        public byte[] Data;
            
        public WriteMemory(MemoryType memType, int address, byte[] data)
        {
            this.memType = memType;
            this.address = address;
            this.Data = data;
            super.writeMem = this;
            super.accessPassword = null;
        }
    }
    
    // ReadMemory
    public static class ReadMemory extends ExtTagOp
    {
        // the type of memory operation
        public MemoryType memType;
        // the block number to start write data into
        public int address;
        // number of blocks to be written
        public byte length;
            
        public ReadMemory(MemoryType memType, int address, byte length)
        {
            this.memType = memType;
            this.address = address;
            this.length = length;
            super.readMem = this;
            super.accessPassword = null;
        }
    }

    /**
     * Initialize access password field of ExtendedTagOp class
     * @param tagOp - Tag operation
     * @param accessPwd - access password
     */
    public void setAccessPassword(TagOp tagOp, byte[] accessPwd)
    {
        ExtTagOp op = (ExtTagOp)tagOp;
        op.accessPassword = accessPwd;
    }

  /**
   * Register a listener to be notified of message packets.
   *
   * @param listener the TransportListener to add
   */
  public abstract void addTransportListener(TransportListener listener);

  /**     
   * Remove a listener from the list of listeners to be notified of
   * message packets.
   *
   * @param listener the TransportListener to add
   */
  public abstract void removeTransportListener(TransportListener listener);

  /**
   * Register a listener to be notified about the read statistics
   * @param listener - StatusLisenter to add
   */
  public abstract void addStatusListener(StatusListener listener);

  /**
   * remove a listener from the list of listeners to be notified of read statistics
   * @param listener - StatusListener to remove
   */
  public abstract void removeStatusListener(StatusListener listener);  

  /**
   * Register a listener to be notified about the read stats
   * @param listener - StatsLisenter to add
   */
  public abstract void addStatsListener(StatsListener listener);

  /**
   * remove a listener from the list of listeners to be notified of read stats
   * @param listener - StatsListener to remove
   */
  public abstract void removeStatsListener(StatsListener listener);

  // compares the versions of str1 and str2 and returns int value.
  public static int versionCompare(String str1, String str2) 
    {
        String[] vals1 = str1.split("\\.");
        String[] vals2 = str2.split("\\.");
        int i = 0;
        // set index to first non-equal ordinal or length of shortest version string
        while (i < vals1.length && i < vals2.length && vals1[i].equals(vals2[i])) {
          i++;
        }
        // compare first non-equal ordinal number
        if (i < vals1.length && i < vals2.length) {
            int diff = Integer.valueOf(vals1[i]).compareTo(Integer.valueOf(vals2[i]));
            return Integer.signum(diff);
        }
        // the strings are equal or one string is a substring of the other
        // e.g. "1.2.3" = "1.2.3" or "1.2.3" < "1.2.3.4"
        return Integer.signum(vals1.length - vals2.length);
    }
  
    /** Enum values for Supported Tag Features */
      public enum SupportedTagFeatures
      {
        // None
        NONE(0x00000000),
        // HF HID Iclass SE Secure read
        HF_HID_ICLASS_SE_SECURE_RD(0x00000001),
        //LF HID Prox Secure read
        LF_HID_PROX_SECURE_RD(0x00000010);

        int rep;

        SupportedTagFeatures(int rep)
        {
            this.rep = rep;
        }

        public static Set<SupportedTagFeatures> supportedFeaturesSet(int bits)
        {
            EnumSet<SupportedTagFeatures> features = EnumSet.noneOf(SupportedTagFeatures.class);
            for (SupportedTagFeatures f : SupportedTagFeatures.values())
            {
                if (0 != (f.rep & bits))
                {
                    features.add(f);
                }
            }
            if (features.isEmpty())
            {
                features.add(SupportedTagFeatures.NONE);
            }
            return features;
        }
    }
}
