/*
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

import com.thingmagic.Gen2.NXP.G2I.ConfigWord;
import java.io.BufferedReader;
import java.math.BigInteger;
import java.util.Collections;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeoutException;
import org.llrp.ltk.exceptions.InvalidLLRPMessageException;
import org.llrp.ltk.generated.messages.*;
import org.llrp.ltk.generated.enumerations.*;
import org.llrp.ltk.types.*;
import org.llrp.ltk.generated.parameters.*;
import java.io.IOException;
import java.io.InputStream;
import org.llrp.ltk.generated.interfaces.*;
import org.llrp.ltk.net.*;
import java.util.Iterator;
import com.thingmagic.Gen2.Bank;
import com.thingmagic.Reader.LicenseOperation;

import static com.thingmagic.TMConstants.*;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Date;
import java.util.EnumSet;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.Map;
import java.util.Properties;
import java.util.Set;
import java.util.SortedMap;
import java.util.StringTokenizer;
import java.util.TimeZone;
import java.util.Timer;
import java.util.TimerTask;
import java.util.TreeMap;
import java.util.Vector;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import org.llrp.ltk.generated.custom.parameters.*;
import org.llrp.ltk.generated.custom.enumerations.*;
import org.llrp.ltk.generated.custom.messages.THINGMAGIC_CONTROL_REQUEST_POWER_CYCLE_READER;
import org.llrp.ltk.generated.custom.messages.THINGMAGIC_CONTROL_RESPONSE_POWER_CYCLE_READER;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 *
 * @author qvantel
 */
public class LLRPReader extends Reader implements com.thingmagic.Logger
{
    //roSpecList needs to be global, as the same needs to be accessed by different methods.
    List<ROSpec> _roSpecList;
    int roSpecId;
    int accessSpecId;
    int opSpecId;
    String _hostname;
    int _port;
    LLRPConnection readerConn;
    List<TagReadData> readData;
    final BlockingQueue<TagReportData> tagReportQueue;
    final BlockingQueue<RFSurveyReportData> rfReportQueue;
    protected List<TransportListener> _llrpListeners;
    protected boolean hasLLRPListeners;
    protected long readDuration;
    boolean continuousReading = false;
    int numberOfTagsToRead = 0;
    private static Logger llrpLogger;
    final int KEEPALIVE_TRIGGER = 5000;

    // Maximum milliseconds required for reader to stop an ongoing search
    final int STOP_TIMEOUT = 5000;
    boolean isStopNTags = false;
    Thread bkgThread;
    Thread rfThread;
    TagProcessor tagProcessor = null;
    RFReportProcessor rfReportProcessor = null;
    boolean stopRequested = false;
    long keepAliveTime = System.currentTimeMillis();
    MonitorKeepAlives monitorKeepAlives;

    private int[] _connectedPortList;
    private int[] _portList;
    private int _rfPowerMax;
    private int _rfPowerMin;   
    private int  maxAntennas;
    private  int[] powerArray;
    private String _model;
    private String _softwareVersion;
    private int[] gpiList;
    private int[] gpoList;
    private Map<Integer,RFMode> capabilitiesCache = null;
    boolean endOfAISpec = false;
    boolean endOfROSpec = false;
    boolean reportReceived = false;
    boolean standalone = false;
    boolean processData = false;
    private int TM_MANUFACTURER_ID = 26554;
    SortedMap<Integer, Integer> powerLevelMap ;
    SortedMap<Integer, Integer> powerLevelReverseMap;
    Reader.Region regionName;
    Set<TagProtocol> protocolSet;
    private Object tagOpResponse;
    TagProtocol[] protocols;
    private ReaderException readerException;
    List<FrequencyHopTable> frequencyHopTableList;
    int[] freqHopTable;
    Map<Integer,TagProtocol> mapRoSpecIdToProtocol ;
    private int invSpecId = 0;
    private int maxSubPlanCount = 5; /*For TMreader build <5.3.2.93*/ 
    static Set<TagReadData.TagMetadataFlag> metaDataFlags = EnumSet.of(TagReadData.TagMetadataFlag.ALL);
    boolean gen2LFFlag = false; 
    boolean gen2QFlag = false; 
    boolean gen2TargetFlag = false;
    int statsFlags = 0x00;
    int allStatsFlags = 4033;
    //Indicates start time of any operation. Any time an LLRP message is sent or received, msgStartTime is updated.
    long msgStartTime = 0;
    // capture the type of message sent here
    public LLRPMessage msgSent = null;
    // Flag to indicate RESPONSE is received
    boolean isMsgRespReceived = false;
    private SerialReader.ReaderStatsFlag[] statsEnabledFlags = {SerialReader.ReaderStatsFlag.NONE};
    
    LLRPReader(String hostname, int port)
    {
        _hostname = hostname;
        _port = port;
        _llrpListeners = new ArrayList<TransportListener>();
        tagReportQueue = new LinkedBlockingQueue<TagReportData>();
        rfReportQueue = new LinkedBlockingQueue<RFSurveyReportData>();
        configureLogging();
    }

    LLRPReader(String hostname)
    {
        this(hostname, 5084);
        //configureLogging();
    }

    public static void configureLogging()
    {
        try
         {
            llrpLogger = LoggerFactory.getLogger(LLRPReader.class);
         } catch (Exception ex) {
                System.out.println("Error : " + ex.getMessage());
         }
    }

    protected void llrpConnect() throws ReaderException
    {
        readerConn = new LLRPConnector(null, _hostname, _port);
        readerConn.setEndpoint(new TagReadEndPoint(this));    
        try
        {
            ((LLRPConnector) readerConn).connect();
        }
        catch (LLRPConnectionAttemptFailedException ex)
        {
            throw new ReaderException(ex.getMessage());
        }
    }

    private int getTargetValue(Object configValue)
    {
        int val = 0;
        switch ((Gen2.Target) configValue)
        {
            case A:
                val = 0;
                break;
            case B:
                val = 1;
                break;
            case AB:
                val = 2;
                break;
            case BA:
                val = 3;
                break;
        }
        return val;
    }

    private void initLLRP() throws ReaderException
    {
        //addReadExceptionListener(getDefaultReadExceptionListener());
        holdEventsAndReportsUponReconnect(true);
       /**
         * Make sure no ROSpecs are present while starting.
         */
        stopActiveROSpecs();

        //get the  Region
        getRegion();
        setKeepAlive();
        initSupportedProtocols();

        //Cache the maxAntenna value
        maxAntennas = getAntennaCount();
        //Cache PowerMax & PowerMin values
        initRadioPower();
        // setting specifications - build a default ROSpec
        addParam(TMR_PARAM_READ_PLAN,
                ReadPlan.class, new SimpleReadPlan(), true, null);
      
        addParam(TMR_PARAM_ANTENNA_CONNECTEDPORTLIST,
                int[].class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getReaderConfiguration(ReaderConfigParams.CONNECTEDPORTLIST);
                    }
                });
        addParam(TMR_PARAM_ANTENNA_RETURNLOSS,
                int[][].class, null, false,
                new ReadOnlyAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.ANTENNA_RETURNLOSS);
                    }
                });
        addParam(TMR_PARAM_ANTENNA_PORTLIST,
                int[].class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return _portList;
                    }
                });

        addParam(TMR_PARAM_RADIO_PORTREADPOWERLIST,
                int[][].class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getReaderConfiguration(ReaderConfigParams.PORTREADPOWERLIST);
                    }
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setReaderConfiguration(ReaderConfigParams.PORTREADPOWERLIST, value);
                        return value;
                    }
                });

        addParam(TMR_PARAM_RADIO_PORTWRITEPOWERLIST,
                int[][].class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.PORTWRITEPOWERLIST);
                    }
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.PORTWRITEPOWERLIST, value);
                        return value;
                    }
                });

        addParam(TMR_PARAM_RADIO_POWERMIN,
                Integer.class, 0, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return _rfPowerMin;
                    }
                });

        addParam(TMR_PARAM_RADIO_POWERMAX,
                Integer.class, 0, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return _rfPowerMax;
                    }
                });
        addParam(TMR_PARAM_GEN2_BLF,
                Gen2.LinkFrequency.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {                                                
                        return getLinkFrequency(getReaderConfiguration(ReaderConfigParams.GEN2_BLF));
                    }
                    public Object set(Object value) throws ReaderException
                    {
                        Integer linkFreq = Gen2.LinkFrequency.get((Gen2.LinkFrequency) value);
                        if (linkFreq == 1 || linkFreq == 3)
                        {
                            throw new ReaderException("Link Frequency not supported");
                        }
                        setReaderConfiguration(ReaderConfigParams.GEN2_BLF, value);
                        return value;
                    }
                });

        addUnconfirmedParam(TMR_PARAM_REGION_ID,
                Reader.Region.class, regionName, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return value;
                    }
                });

        addParam(TMR_PARAM_REGION_SUPPORTEDREGIONS,
                Reader.Region[].class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value)
                            throws ReaderException
                    {
                        /*
                         * Parameters /reader/region/id and /reader/region/supportedRegions
                         * are same in case of fixed readers.
                         */
                        Reader.Region[] regions = new Reader.Region[]{regionName};
                        return  regions;
                    }
                });
        addParam(TMR_PARAM_REGION_HOPTABLE,
                int[].class, null, true,
                new SettingAction()
                {
                    @Override
                     public Object set(Object value) throws ReaderException
                    {
                        int[] hoptableList = (int[])value;
                        if(hoptableList.length <= 0)
                        {
                            throw new IllegalArgumentException("Hoptable cannot be empty.");
                        }
                        setCustomReaderConfiguration(ReaderConfigParams.REGION_HOPTABLE, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getRegulatoryCapabilities();
                    }
                });
        addParam(TMR_PARAM_REGULATORY_MODE,
                RegulatoryMode.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.REGULATORY_MODE, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.REGULATORY_MODE);
                    }
                });

        addParam(TMR_PARAM_REGULATORY_MODULATION,
                RegulatoryModulation.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.REGULATORY_MODULATION, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.REGULATORY_MODULATION);
                    }
                });
        addParam(TMR_PARAM_REGULATORY_ONTIME,
                Integer.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.REGULATORY_ONTIME, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.REGULATORY_ONTIME);
                    }
                });
        addParam(TMR_PARAM_REGULATORY_OFFTIME,
                Integer.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.REGULATORY_OFFTIME, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.REGULATORY_OFFTIME);
                    }
                });
        addParam(TMR_PARAM_REGULATORY_ENABLE,
                Boolean.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.REGULATORY_ENABLE, value);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        throw new UnsupportedOperationException("Unsupported operation");
                    }
                });
        addParam(TMR_PARAM_READER_DESCRIPTION,
                String.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.READERDESCRIPTION, value);
                        return value.toString();
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.READERDESCRIPTION);
                    }
                });

        addParam(TMR_PARAM_READER_PRODUCTID,
                Integer.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object val) throws ReaderException
                    {
                        return getCustomReaderCapabilities(ReaderConfigParams.PRODUCT_ID);
                    }
                });

        addParam(TMR_PARAM_READER_PRODUCTGROUPID,
                Integer.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object val) throws ReaderException
                    {
                        return getCustomReaderCapabilities(ReaderConfigParams.PRODUCT_GROUP_ID);
                    }
                });

        addParam(TMR_PARAM_READER_PRODUCTGROUP,
                String.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderCapabilities(ReaderConfigParams.PRODUCT_GROUP);
                    }
                });

        addParam(TMR_PARAM_VERSION_HARDWARE,
                String.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderCapabilities(ReaderConfigParams.HARDWARE_VERSION);
                    }
                });

         addParam(TMR_PARAM_VERSION_MODEL,
                 String.class, null, false,
                 new ReadOnlyAction()
                 {
                     @Override
                     public Object get(Object value) throws ReaderException
                     {
                         return _model;
                     }
                });

        addParam(TMR_PARAM_VERSION_SOFTWARE,
                String.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return _softwareVersion;
                    }
                });
        addParam(TMR_PARAM_READER_METADATA,
                Set.class, null, true,
                new SettingAction() 
                {
                    @Override
                    public Object set(Object value) throws ReaderException {
                        try
                        {
                            metaDataFlags = (Set<TagReadData.TagMetadataFlag>)value;
                            //tagType metadata flag is not supported for llrp readers. Hence throw error when user sets.
                            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.TAGTYPE))
                            {
                                throw new ReaderException("Invalid Argument in \"/reader/metadata\" : " + metaDataFlags);
                            }
                            int mFlagValue = 0;
                            /**
                             * Need to pass all Gen2 custom parameters and not single values.
                             **/
                            for(TagReadData.TagMetadataFlag trdFlag : metaDataFlags)
                            {
                                if(trdFlag.equals(TagReadData.TagMetadataFlag.GEN2_LF))
                                {
                                    metaDataFlags.remove(trdFlag);
                                    gen2LFFlag = true;
                                }
                                if(trdFlag.equals(TagReadData.TagMetadataFlag.GEN2_Q))
                                {
                                    metaDataFlags.remove(trdFlag);
                                    gen2QFlag = true;
                                }
                                if(trdFlag.equals(TagReadData.TagMetadataFlag.GEN2_TARGET))
                                {
                                    metaDataFlags.remove(trdFlag);
                                    gen2TargetFlag = true;
                                }
                            }
                            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ALL))
                            {
                                // remove tagtype flag here
                                mFlagValue = SerialReader.tagMetadataSetValue(metaDataFlags) & (~SerialReader.tagMetadataSetValue(EnumSet.of(TagReadData.TagMetadataFlag.TAGTYPE)));
                            }
                            else
                            {
                                mFlagValue = SerialReader.tagMetadataSetValue(metaDataFlags);
                            }
                            if(gen2LFFlag || gen2TargetFlag || gen2QFlag)
                            {
                                mFlagValue |= SerialReader.tagMetadataSetValue(EnumSet.of(TagReadData.TagMetadataFlag.GEN2_Q, 
                                                                                TagReadData.TagMetadataFlag.GEN2_LF,    
                                                                                TagReadData.TagMetadataFlag.GEN2_TARGET));
                                metaDataFlags.add(TagReadData.TagMetadataFlag.GEN2_Q);
                                metaDataFlags.add(TagReadData.TagMetadataFlag.GEN2_LF);
                                metaDataFlags.add(TagReadData.TagMetadataFlag.GEN2_TARGET);
                            }
                            else if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ALL))
                            {
                                gen2LFFlag = true;
                                gen2TargetFlag = true;
                                gen2QFlag = true;
                            }
                            setCustomReaderConfiguration(ReaderConfigParams.READER_METADATA, mFlagValue);
                            return metaDataFlags;
                        }
                        catch(ReaderException re)
                        {
                            if(re.getMessage().contains("Invalid Argument in \"/reader/metadata\" :"))
                            {
                                throw re;
                            }
                            else
                            {
                                metaDataFlags = EnumSet.of(TagReadData.TagMetadataFlag.NONE);
                                throw re;
                            }
                        }
                    }
                    @Override
                    public Object get(Object value) throws ReaderException {
                        return getCustomReaderConfiguration(ReaderConfigParams.READER_METADATA);
                    }
                });
        
        addParam(TMR_PARAM_READER_STATS_ENABLE,
                SerialReader.ReaderStatsFlag[].class, null, true,
                new SettingAction()
                {                   
                    public Object set(Object value) throws ReaderException{
                        try
                        {
                            statsEnabledFlags=(SerialReader.ReaderStatsFlag[])value;
                            for (SerialReader.ReaderStatsFlag rs : statsEnabledFlags) {
                               statsFlags |= rs.value;
                            }
                            if(statsFlags == 0x00)
                            {
                                setCustomReaderConfiguration(ReaderConfigParams.READER_STATS_ENABLE, statsFlags);
                            }
                            else
                            {
                                setCustomReaderConfiguration(ReaderConfigParams.READER_STATS_ENABLE, allStatsFlags);
                            }
                            return statsFlags;
                        }
                        catch(ReaderException re)
                        {
                            throw new UnsupportedOperationException("Unsupported parameter : "+TMR_PARAM_READER_STATS_ENABLE);
                        }
                    }

                    public Object get(Object value)
                            throws ReaderException
                    {
                        return statsEnabledFlags;
                    }
                });
        addParam(TMR_PARAM_READER_STATS,
                SerialReader.ReaderStatsFlag[].class, null, true,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.READER_STATS);
                    }
                });
        addParam(TMR_PARAM_ANTENNA_PORTSWITCHGPOS,
                int[].class, null, true,
                new SettingAction()
                {                   
                    public Object set(Object value) throws ReaderException{
                        try
                        {
                            int[] portSwGpos = (int[])value;
                            setCustomReaderConfiguration(ReaderConfigParams.PORT_SWITCH_GPOS, portSwGpos);
                            //Update portList with actual values after setting portswitchgpos
                            _portList = getAntennaPortList();
                            return portSwGpos;
                        }
                        catch(ReaderException re)
                        {
                            throw new ReaderException(re.getMessage());
                        }
                    }

                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.PORT_SWITCH_GPOS);
                    }
                });
        //_softwareVersion = (String) getReaderCapabilities(ReaderConfigParams.SOFTWARE_VERSION);

        checkForAvailableReaderFeatures();
        //_model = (String) getReaderCapabilities(ReaderConfigParams.MODEL);

        if (_model.equals(TMR_READER_MERCURY6) || _model.equals(TMR_READER_ASTRA) || _model.equals(TMR_READER_ASTRA_EX))
        {
            gpiList = new int[]{3, 4, 6, 7};
            gpoList = new int[]{0, 1, 2, 5};
        }
        else if (_model.equals(TMR_READER_SARGAS))
        {
            gpiList = new int[]{0,1};
            gpoList = new int[]{2,3};
        }
        else if (_model.equals(TMR_READER_IZAR))
        {
            gpiList = new int[]{1,2,3,4};
            gpoList = new int[]{1,2,3,4};
        }

        addParam(TMR_PARAM_GPIO_INPUTLIST,
                int[].class, gpiList, false, null);

        addParam(TMR_PARAM_GPIO_OUTPUTLIST,
                int[].class, gpoList, false, null);

        addParam(TMR_PARAM_TAGREADDATA_UNIQUEBYANTENNA,
                Boolean.class, false, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.UNIQUEBYANTENNA, value);
                        return value;
                    }
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.UNIQUEBYANTENNA);
                    }
                });

        addParam(TMR_PARAM_TAGREADDATA_UNIQUEBYDATA,
                Boolean.class, false, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.UNIQUEBYDATA, value);
                        return value;
                    }
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.UNIQUEBYDATA);
                    }
                });

        addParam(TMR_PARAM_TAGREADDATA_RECORDHIGHESTRSSI,
                Boolean.class, false, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.RECORDHIGHESTRSSI, value);
                        return value;
                    }
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.RECORDHIGHESTRSSI);
                    }
                });

        addParam(TMR_PARAM_GEN2_TARI,
                Gen2.Tari.class, null, true,
                new SettingAction() 
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getReaderConfiguration(ReaderConfigParams.GEN2_TARI);
                    }
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        setReaderConfiguration(ReaderConfigParams.GEN2_TARI, value);
                        return value;
                    }
                });

        addParam(TMR_PARAM_VERSION_SERIAL,
                String.class, null, true,
                new ReadOnlyAction() 
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                         return getCustomReaderCapabilities(ReaderConfigParams.SERIAL);
                    }
                });

        addParam(TMR_PARAM_RADIO_READPOWER,
                Integer.class, null, true,
                new SettingAction() 
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        int power = (Integer) value;
                        validatePower(power);
                        setReaderConfiguration(ReaderConfigParams.READPOWER, power);
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        return getReaderConfiguration(ReaderConfigParams.READPOWER);
                    }
                });

        addParam(TMR_PARAM_CURRENTTIME,
                Date.class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        try
                        {
                            String rawDate = (String) getCustomReaderConfiguration(ReaderConfigParams.CURRENTTIME);
                            rawDate = rawDate.substring(0, 19);
                            DateFormat dbFormatter = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
                            dbFormatter.setTimeZone(TimeZone.getTimeZone("UTC"));
                            Date scheduledDate = dbFormatter.parse(rawDate);
                            return scheduledDate;
                        }
                        catch (ParseException ex)
                        {
                            log(ex.toString());
                            throw new ReaderException("Parse Exception occurred");
                        }
                    }
                });

        addParam(TMR_PARAM_GEN2_Q,
                Gen2.Q.class, null, true,
                new SettingAction()
                {
                    @Override
                    public Object set(Object value) throws ReaderException
                    {
                        if (value instanceof Gen2.StaticQ) {
                            Gen2.StaticQ q = ((Gen2.StaticQ) value);
                            if (q.initialQ < 0 || q.initialQ > 15)
                            {
                                throw new IllegalArgumentException("Value of /reader/gen2/q is out of range. Should be between 0 and 15");
                            }
                            setCustomReaderConfiguration(ReaderConfigParams.GEN2_Q, new int[][]{{QType.Static, q.initialQ}});
                        }
                        else
                        {
                            setCustomReaderConfiguration(ReaderConfigParams.GEN2_Q, new int[][]{{QType.Dynamic, 0}});
                        }
                        return value;
                    }
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        int q[];
                        q = (int[]) getCustomReaderConfiguration(ReaderConfigParams.GEN2_Q);

                        if (q[1] == 1)
                        {
                            return new Gen2.StaticQ(q[0]);
                        } 
                        else
                        {
                            return new Gen2.DynamicQ();
                        }
                    }
                });
        addParam(TMR_PARAM_GEN2_INITIAL_Q,
                Gen2.InitQ.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value)
                            throws ReaderException
                    {
                        Gen2.InitQ q = (Gen2.InitQ)value;
                        if(q.qEnable)
                        {
                            if (q.initialQ < 2 || q.initialQ > 10)
                            {
                                throw new IllegalArgumentException("Value of /reader/gen2/initQ is out of range. Should be between 2 and 10");
                            }
                        }
                        setCustomReaderConfiguration(ReaderConfigParams.GEN2_INITQ, q);
                        return value;
                    }
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.GEN2_INITQ);
                    }
                });
        addParam(TMR_PARAM_GEN2_SEND_SELECT,
                Boolean.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.SENDSELECT, value);
                        return value;
                    }
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.SENDSELECT);
                    }
                });

        addParam(TMR_PARAM_VERSION_SUPPORTEDPROTOCOLS,
                TagProtocol[].class, null, false,
                new ReadOnlyAction()
                {
                    @Override
                    public Object get(Object value) throws ReaderException
                    {
                        //return protocols.clone();
                        return initSupportedProtocols();
                    }
                });

        addParam(TMR_PARAM_GEN2_SESSION,
                Gen2.Session.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setReaderConfiguration(ReaderConfigParams.SESSION, value);
                        return value;
                    }
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getReaderConfiguration(ReaderConfigParams.SESSION);
                    }
                });
        
        addParam(TMR_PARAM_RADIO_TEMPERATURE,
                Integer.class, null, false,
                new ReadOnlyAction()
                {
                  @Override
                  public Object get(Object value)
                          throws ReaderException
                  {
                      return getCustomReaderConfiguration(ReaderConfigParams.TEMPERATURE);
                  }
                });

        addParam(TMR_PARAM_ANTENNA_CHECKPORT, Boolean.class, null, true,
              new SettingAction()
              {
                  @Override
                  public Object set(Object value)
                          throws ReaderException
                  {
                      setCustomReaderConfiguration(ReaderConfigParams.CHECKPORT, value);
                      return value;
                  }
                  @Override
                  public Object get(Object value)
                          throws ReaderException
                  {
                      return getCustomReaderConfiguration(ReaderConfigParams.CHECKPORT);
                  }
                });
                
       addParam(TMR_PARAM_GEN2_TAGENCODING,
            Gen2.TagEncoding.class, null, true,
              new SettingAction() 
              {
                  @Override
                  public Object get(Object value)
                          throws ReaderException
                  {
                      return getReaderConfiguration(ReaderConfigParams.TAGENCODING);
                  }
                  @Override
                  public Object set(Object value)
                          throws ReaderException
                  {
                      setReaderConfiguration(ReaderConfigParams.TAGENCODING, value);
                      return value;
                  }
              });

         addParam(TMR_PARAM_RADIO_WRITEPOWER,
                Integer.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        int power = (Integer) value;
                        validatePower(power);
                        setCustomReaderConfiguration(ReaderConfigParams.WRITEPOWER, power);
                        return value;
                    }
                    public Object get(Object value) throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.WRITEPOWER);
                    }
                });
          addParam(TMR_PARAM_GEN2_TARGET,
                Gen2.Target.class, null, true,
                new SettingAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.TARGET);
                    }
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.TARGET, value);
                        return value;
                    }
                });
          addParam(TMR_PARAM_GEN2_T4,
                Integer.class, null, true,
                new SettingAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.GEN2_T4);
                    }
                    public Object set(Object value)
                            throws ReaderException
                    {
                        if(null == value)
                        {
                            throw new IllegalArgumentException("Invalid argument");
                        }
                        setCustomReaderConfiguration(ReaderConfigParams.GEN2_T4, value);
                        return value;
                    }
                });
          addParam(TMR_PARAM_GEN2_PROTOCOLEXTENSION,
                Gen2.ProtocolExtension.class, null, false,
                new ReadOnlyAction()
                {
                  @Override
                  public Object get(Object value)
                          throws ReaderException
                  {
                      return getCustomReaderConfiguration(ReaderConfigParams.GEN2_PROTOCOLEXTENSION);
                  }
                });
          addParam(TMR_PARAM_ISO180006B_BLF,
                Iso180006b.LinkFrequency.class, null, true,
                new SettingAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomISO18k6bProtocolConfiguration(ReaderConfigParams.ISO180006B_BLF);
                    }
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setCustomIS018k6bProtocolConfiguration(value, ReaderConfigParams.ISO180006B_BLF);
                        return value;
                    }
                });
         addParam(TMR_PARAM_ISO180006B_DELIMITER,
                Iso180006b.Delimiter.class, null, true,
                new SettingAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomISO18k6bProtocolConfiguration(ReaderConfigParams.ISO180006B_DELIMITER);
                    }
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setCustomIS018k6bProtocolConfiguration(value, ReaderConfigParams.ISO180006B_DELIMITER);
                        return value;
                    }
                });
         addParam(TMR_PARAM_ISO180006B_MODULATION_DEPTH,
                Iso180006b.ModulationDepth.class, null, true,
                new SettingAction()
                {
                    public Object get(Object value)
                            throws ReaderException
                    {
                        return getCustomISO18k6bProtocolConfiguration(ReaderConfigParams.ISO180006B_MODULATIONDEPTH);
                    }
                    public Object set(Object value)
                            throws ReaderException
                    {
                        setCustomIS018k6bProtocolConfiguration(value, ReaderConfigParams.ISO180006B_MODULATIONDEPTH);
                        return value;
                    }
                });
         addParam(TMR_PARAM_LICENSE_KEY, byte[].class, null, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                   {
                        setCustomReaderConfiguration(ReaderConfigParams.LICENSEKEY, value);
                        return null;
                    }

                    public Object get(Object value) throws ReaderException {
                        throw new UnsupportedOperationException("Unsupported operation");
                    }
                });
         addParam(TMR_PARAM_MANAGE_LICENSE_KEY, LicenseOperation.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        LicenseOperation operation = (LicenseOperation)value;
                        if(operation.option == Reader.LicenseOption.SET_LICENSE_KEY)
                        {
                            setCustomReaderConfiguration(ReaderConfigParams.LICENSEKEY, operation.key);
                        }
                        else
                        {
                            throw new FeatureNotSupportedException("Unimplemented feature");
                        }
                        return null;
                    }
                    public Object get(Object value) throws ReaderException {
                        throw new UnsupportedOperationException("Unsupported operation");
                    }
                });
         addParam(TMR_PARAM_HOSTNAME, String.class, null, true,
                new SettingAction()
                {
                    public Object set(Object value) throws ReaderException
                    {
                        setCustomReaderConfiguration(ReaderConfigParams.READERHOSTNAME, value);
                        return null;
                    }

                    public Object get(Object value) throws ReaderException 
                    {
                        return getCustomReaderConfiguration(ReaderConfigParams.READERHOSTNAME);
                    }
                });
        addParam(TMR_PARAM_TAGOP_ANTENNA,
              Integer.class, getFirstConnectedAntenna(), true,
              new SettingAction()
               {
                   public Object set(Object value)
                   {
                       int a = (Integer) value;
                       if (a < 1 || a > maxAntennas)
                       {
                           throw new IllegalArgumentException(
                                   "Invalid antenna " + a + ".");
   }
                       return value;
                  }
                  public Object get(Object value)
                  {
                      return value;
                  }
              });

        addParam(TMR_PARAM_TAGOP_PROTOCOL,
                TagProtocol.class, TagProtocol.GEN2, true,
                    new SettingAction()
                    {
                      public Object set(Object value)
                      {
                        TagProtocol p = (TagProtocol)value;
                        validateProtocol(p);
                        return value;
                      }
                      public Object get(Object value)
                      {
                        return value;
                      }
        });

        addParam(TMR_PARAM_READ_ASYNCOFFTIME,
               Integer.class, 0, true,
               new SettingAction() {

            public Object set(Object value) throws ReaderException
            {
                if((Integer)value<0)
                {
                throw new IllegalArgumentException("negative value not permitted");
                }
                setCustomReaderConfiguration(ReaderConfigParams.ASYNC_OFF, value);
                return null;
            }

            public Object get(Object value) throws ReaderException {
                return getCustomReaderConfiguration(ReaderConfigParams.ASYNC_OFF);
            }
        });
        

        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
        {
            addParam(TMR_PARAM_READ_ASYNCONTIME,
                 Integer.class, 250, true,
                 new SettingAction() {

                public Object set(Object value) throws ReaderException {
                    if((Integer)value < 0 || (Integer)value > 65535)
                    {
                        throw new IllegalArgumentException("Value of " + value + " to the parameter /reader/read/asyncOnTime is out of range." );
                    }
                    setCustomReaderConfiguration(ReaderConfigParams.ASYNC_ON, value);
                    return null;
                }

                public Object get(Object value) throws ReaderException {
                    return getCustomReaderConfiguration(ReaderConfigParams.ASYNC_ON);
                }
            });
        }
        monitorKeepAlives = new MonitorKeepAlives();
        monitorKeepAlives.start();
   }

    /**
    * This method will check whether the below features are supported by the reader
    * or not. If respective feature is supported, the corresponding flag gets added
    * to featuresFlag.
    */
    public void checkForAvailableReaderFeatures()
    {
        if(isTMReaderPerAntennaOntimeSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME);
        }
        if(isTMReaderMultiSelectSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT);
        }
        if(isTMReaderInvSpecIDSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_INVENTORYSPEC_ID);
        }
        if(isTMReaderReaderMetadataSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_READER_METADATA);
        }
        if(isTMReaderReaderStatsSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_READER_STATS);
        }
        if(isTMReaderStateAwareFilterSupported())
        {
            featuresFlag.add(ReaderFeaturesFlag.READER_FEATURES_FLAG_STATE_AWARE);
        }
    }

    /**
     * This method will check whether the current firmware version supports per antenna on time feature and returns true or false.
     */
    private boolean isTMReaderPerAntennaOntimeSupported()
    {
        try
        {
            String checkVersion = "5.3.2.38";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }

    /**
     * This method will check whether the current firmware version supports multi select feature and returns true or false.
     */
    private boolean isTMReaderMultiSelectSupported()
    {
        try
        {
            String checkVersion = "5.3.2.12";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }
	
    /**
     * This method will check whether we can add custom inventory spec ID in AccessSpec - allowed if supported firmware is used.
     */
    private boolean isTMReaderInvSpecIDSupported()
    {
        try
        {
            String checkVersion = "5.3.2.97";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }
    
    /**
     * This method will check whether reader metadata is supported.
     */
    private boolean isTMReaderReaderMetadataSupported()
    {
        try
        {
            String checkVersion = "5.3.4.24";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }
    
    /**
     * This method will check whether reader stats are supported.
     */
    private boolean isTMReaderReaderStatsSupported()
    {
        try
        {
            String checkVersion = "5.3.4.50";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }
    
    /**
     * This method will check whether state aware filter action is supported.
     */
    private boolean isTMReaderStateAwareFilterSupported()
    {
        try
        {
            String checkVersion = "5.3.4.59";
            String readerVersion = _softwareVersion;
            int compareVer = versionCompare(readerVersion, checkVersion);
            if (compareVer < 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            System.out.println(ex.getMessage());
        }
        return false;
    }

    /**
     * returns first connected antenna
     * @return
     */
    private int getFirstConnectedAntenna() throws ReaderException
    {
        int[] validAnts = (int[]) paramGet(TMR_PARAM_ANTENNA_CONNECTEDPORTLIST);

        if (0 < validAnts.length)
        {
            return validAnts[0];
        }
        return 0;

    }
    private Gen2.TagEncoding getMValue(Object mValue)
    {
        C1G2MValue m = new C1G2MValue();
        int mVal = m.getValue((String)mValue);
        return Gen2.TagEncoding.get(mVal);
    }

    private TagProtocol[] initSupportedProtocols() throws ReaderException
    {
        List<SupportedProtocols> supportedProtocolsList = null;
        GET_READER_CAPABILITIES_RESPONSE readerCapabilitiesResp = getCustomReaderCapabilitiesResponse(ThingMagicControlCapabilities.DeviceProtocolCapabilities);
        int index = 0;
        //cache the Hardware Version value
        List<Custom> deviceProtocolList = readerCapabilitiesResp.getCustomList();

        for (Custom deviceProtocol : deviceProtocolList)
        {
            DeviceProtocolCapabilities protocolCap = new DeviceProtocolCapabilities(deviceProtocol);
            supportedProtocolsList = protocolCap.getSupportedProtocolsList();
        }
        protocolSet = EnumSet.noneOf(TagProtocol.class);
        /*
         * If the supportedProtocolsList is one(i.e if the user set the License Key that supports only Gen2 protocol)
         * then create the  TagProtocol with size one.
         *
         */
        if(supportedProtocolsList.size() == 1){
            protocols = new TagProtocol[1];
            String protocol = supportedProtocolsList.get(0).getProtocol().toString();
            TagProtocol p = TagProtocol.getProtocol(protocol);
            protocols[0] = p;
            protocolSet.add(p);
        }
        else
        {
            protocols = new TagProtocol[supportedProtocolsList.size()];
        for (SupportedProtocols sProtocol : supportedProtocolsList)
        {
            String protocol = sProtocol.getProtocol().toString();
                if (protocol.equals("GEN2") || protocol.equals("ISO180006B") || protocol.equals("ATA") || protocol.equals("IPX64") || protocol.equals("IPX256"))
                {
            TagProtocol p = TagProtocol.getProtocol(protocol);
            if (p != null)
            {
                protocols[index] = p;
                        protocolSet.add(p);
            }
            index++;
        }
            }
        }
        _softwareVersion = parseGetFirmwareVersion(readerCapabilitiesResp);
        _model = parseGetModel(readerCapabilitiesResp);
        return protocols;
    }

    @Override
    public void connect() throws ReaderException
    {
        if(!_isConnected)
        {
            llrpConnect();            
        }
        initLLRP();
    }
    
    @Override
    public void reboot() throws ReaderException
    {
        try
        {
            if(readerConn != null)
           {
                THINGMAGIC_CONTROL_REQUEST_POWER_CYCLE_READER reqPowerCycle = new THINGMAGIC_CONTROL_REQUEST_POWER_CYCLE_READER();
               
                reqPowerCycle.setBootToSafeMode(new Bit(false));
                reqPowerCycle.setMagicNumber(new UnsignedInteger(0x20000920));
                reqPowerCycle.setVersion(new BitList(0,0,1));
                
                THINGMAGIC_CONTROL_RESPONSE_POWER_CYCLE_READER response = (THINGMAGIC_CONTROL_RESPONSE_POWER_CYCLE_READER)LLRP_SendReceive(reqPowerCycle, commandTimeout + transportTimeout);
                Thread.sleep(90000);
                if (null != response && response.getLLRPStatus().getStatusCode().intValue() == StatusCode.M_Success)
                {
                    System.out.println("Reader rebooted successfully");
                }
           }
        }
        catch(Exception ex)
        {
            throw new ReaderException(ex.getMessage());
        }
    }
  
    private Region getRegion() throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicRegionConfiguration);
        List<Custom> customregionId = readerConfigResponse.getCustomList();
        Custom region = customregionId.get(0);
        ThingMagicRegionConfiguration regionConfig = new ThingMagicRegionConfiguration(region);
        int regionId = regionConfig.getRegionID().intValue();
        regionName = codeToRegionMap.get(regionId);
        return regionName;
    }

    private Gen2.LinkFrequency getLinkFrequency(Object frequency)
    {
        int freq = Integer.parseInt(frequency.toString());
        switch (freq)
        {
            case 250000:
                return Gen2.LinkFrequency.LINK250KHZ;
            case 640000:
                return Gen2.LinkFrequency.LINK640KHZ;
            case 320000:
                return Gen2.LinkFrequency.LINK320KHZ;
            default:
                throw new IllegalArgumentException("Unknown BLF ");
        }
    }

    private Iso180006b.LinkFrequency getI186bLinkFrequency(int frequency)
    {
        switch(frequency)
        {
            case 160:
                return Iso180006b.LinkFrequency.LINK160KHZ;
            case 40:
                return Iso180006b.LinkFrequency.LINK40KHZ;
            default:
                throw new IllegalArgumentException("Unknown BLF ");
        }
    }
    /**
     * Maximum Number of Antennas
     * @return maxAntennas
     * @throws ReaderException
     */
    private int getAntennaCount() throws ReaderException
    {
        _portList = getAntennaPortList();
        maxAntennas = _portList.length;
        return maxAntennas;
    }

    /**
     * gets the antenna port list
     * @return portlist
     * @throws ReaderException
     */
    private int[] getAntennaPortList() throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigRes = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaProperties);
        //Fetch the antenna List from the response
        List<AntennaProperties> antPropertiesList = readerConfigRes.getAntennaPropertiesList();
        int listIndex = 0;
        int antCount = antPropertiesList.size();
        //Cache the portList value
        _portList = new int[antCount];
        for(AntennaProperties antProps:antPropertiesList)
        {
            _portList[listIndex++] = antProps.getAntennaID().intValue();
        }
        return _portList;
    }

    /**
     * Get the Standard Reader Configuration
     * @param configParam
     * @return Object
     * @throws ReaderException
     */
    private Object getReaderConfiguration(ReaderConfigParams configParam) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResp;
        ActiveModeIndex activeMode;
        switch(configParam)
        {
            case CONNECTEDPORTLIST:
                readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaProperties);
                // return the connectedPortList values
                return parseGetConnectedPortList(readerConfigResp);
            case PORTREADPOWERLIST:
                readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
                //return the  portReadPowerList values
                return parseGetPortReadPowerList(readerConfigResp);
            case KEEP_ALIVE:
                readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.KeepaliveSpec);
                return null;
            case GEN2_BLF:
                activeMode = getActiveMode();
                int getBlfValue = activeMode.getActiveModeIndex();
                // get the values from cached modes based on key mode index
                return parseGetReaderParam(configParam, getBlfValue);
            case GEN2_TARI:
                activeMode = getActiveMode();
                int tari = activeMode.getTari();
                return getTari(tari);
            case SERIAL:
                readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.Identification);
                return readerConfigResp.getIdentification().getReaderID().toString();
            case READPOWER:
                readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
                return parseGetReadPower(readerConfigResp);
            case SESSION:
                return getSession();
            case TAGENCODING:
                activeMode = getActiveMode();
                int getModeIndex = activeMode.getActiveModeIndex();
                // get the values from cached modes based on key mode index
                return getMValue(parseGetReaderParam(configParam, getModeIndex));
        }
        return configParam;
    }

    private Gen2.Tari getTari(int tari)
    {
        switch(tari)
        {
            case 25000:
                return Gen2.Tari.TARI_25US;
            case 12500:
                return Gen2.Tari.TARI_12_5US;
            case 6250:
                return Gen2.Tari.TARI_6_25US;
            default:
                return Gen2.Tari.get(0);
        }
    }

    private Gen2.Session getSession() throws ReaderException
    {
        // Now the message is fully framed, Send the message
        GET_READER_CONFIG_RESPONSE readerConfigResp = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
        // Fetch the antenna configurationList from the response
        List<AntennaConfiguration> antConfigList = readerConfigResp.getAntennaConfigurationList();
        C1G2SingulationControl c1g2SingulationControl = null;
        AntennaConfiguration antConfig = antConfigList.get(0);
        List<AirProtocolInventoryCommandSettings> apicSettingsList = antConfig.getAirProtocolInventoryCommandSettingsList();
        AirProtocolInventoryCommandSettings apicSettings = apicSettingsList.get(0);
        c1g2SingulationControl = ((C1G2InventoryCommand) apicSettings).getC1G2SingulationControl();
        int session = c1g2SingulationControl.getSession().intValue();
        return Gen2.Session.get(session);
    }

    private Object getCustomReaderConfiguration(ReaderConfigParams configParam) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResp;
        List<Custom> customList;
        switch (configParam)
        {
            case PORTWRITEPOWERLIST:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaConfiguration);
                customList = readerConfigResp.getCustomList();
                //catch the portWritePowerList values
                int[][] portWriteArray = new int[customList.size()][];
                int index = 0;
                for (Custom portWriteList : customList)
                {
                    ThingMagicAntennaConfiguration antennaConfig = new ThingMagicAntennaConfiguration(portWriteList);
                    if(powerLevelMap.containsKey(antennaConfig.getWriteTransmitPower().getWriteTransmitPower().intValue()))
                    {
                        portWriteArray[index] = new int[]{
                                antennaConfig.getAntennaID().intValue(),
                                powerLevelMap.get(antennaConfig.getWriteTransmitPower().getWriteTransmitPower().intValue())
                            };
                    }
                    else
                    {
                        throw new IllegalArgumentException("Invalid Write TransmitPower index. Key not found.");
                    }
                    index++;
                }
                return portWriteArray;
            case READERDESCRIPTION:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderConfiguration);
                customList = readerConfigResp.getCustomList();
                //catch the ReaderDescription value.
                String readerDescription = null;
                Custom readerDesc = customList.get(0);
                ThingMagicReaderConfiguration readerConfiguration = new ThingMagicReaderConfiguration(readerDesc);
                readerDescription = readerConfiguration.getReaderDescription().toString();
                return readerDescription;
            case UNIQUEBYANTENNA:
                ThingMagicDeDuplication deduplication = getCustomDeduplication();
                return deduplication.getUniqueByAntenna().toBoolean();
            case UNIQUEBYDATA:
                ThingMagicDeDuplication tmDeduplication = getCustomDeduplication();
                return tmDeduplication.getUniqueByData().toBoolean();
            case RECORDHIGHESTRSSI:
                ThingMagicDeDuplication thingmagicDeduplication = getCustomDeduplication();
                return thingmagicDeduplication.getRecordHighestRSSI().toBoolean();
            case CURRENTTIME:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicCurrentTime);
                customList = readerConfigResp.getCustomList();
                String currentTime = null;
                Custom tmCurrentTime = customList.get(0);
                ThingMagicCurrentTime tm_currentTime = new ThingMagicCurrentTime(tmCurrentTime);
                currentTime = tm_currentTime.getReaderCurrentTime().toString();
                return currentTime;
            case GEN2_Q:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                int[] qValue = new int[2];
                Custom gen2Q = customList.get(0);
                ThingMagicProtocolConfiguration protocolConfig = new ThingMagicProtocolConfiguration(gen2Q);
                Gen2Q genQValue = protocolConfig.getGen2CustomParameters().getGen2Q();
                qValue[0] = genQValue.getInitQValue().intValue();
                qValue[1] = genQValue.getGen2QType().intValue();
                return qValue;
            case GEN2_INITQ:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Gen2.InitQ init_q = new Gen2.InitQ();
                Custom gen2initQ = customList.get(0);
                ThingMagicProtocolConfiguration protoConfiguration = new ThingMagicProtocolConfiguration(gen2initQ);
                InitQ initQ = protoConfiguration.getGen2CustomParameters().getInitQ();
                init_q.qEnable = initQ.getqEnable().toBoolean();
                qValue qval = initQ.getQValue();
                init_q.initialQ = qval.getvalue().intValue();
                return init_q;
            case SENDSELECT:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom sendSelect = customList.get(0);
                ThingMagicProtocolConfiguration proto_config = new ThingMagicProtocolConfiguration(sendSelect);
                sendSelect getSelect = proto_config.getGen2CustomParameters().getSendSelect();
                boolean send_select = getSelect.getselectValue().toBoolean();
                return send_select;
            case TEMPERATURE:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderModuleTemperature);
                customList = readerConfigResp.getCustomList();
                Custom readerTemperature = customList.get(0);
                ThingMagicReaderModuleTemperature moduleTemperature = new ThingMagicReaderModuleTemperature(readerTemperature);
                return moduleTemperature.getReaderModuleTemperature().intValue();
            case CHECKPORT:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaDetection);
                customList = readerConfigResp.getCustomList();
                Custom antennaDetection = customList.get(0);
                ThingMagicAntennaDetection antDetection = new ThingMagicAntennaDetection(antennaDetection);
                return antDetection.getAntennaDetection().toBoolean();
            case WRITEPOWER:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaConfiguration);
                ThingMagicAntennaConfiguration antennaConfig;
                customList = readerConfigResp.getCustomList();
                int count = 0;
                Custom writePower = customList.get(0);
                antennaConfig = new ThingMagicAntennaConfiguration(writePower);
                int writeValue = antennaConfig.getWriteTransmitPower().getWriteTransmitPower().intValue();
                for (Custom portWriteList : customList)
                {
                    antennaConfig = new ThingMagicAntennaConfiguration(portWriteList);
                    int writeArray = antennaConfig.getWriteTransmitPower().getWriteTransmitPower().intValue();
                    if(!(writeArray == writeValue))
                    {
                        throw new ReaderParseException("Undefined Values");
                    }
                }
                return powerLevelMap.get(writeValue);
            case TARGET:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customProtocolConfig = customList.get(0);
                ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration(customProtocolConfig);
                ThingMagicTargetStrategy targetStrategy = protocolConfiguration.getGen2CustomParameters().getThingMagicTargetStrategy();
                int targetValue = targetStrategy.getThingMagicTargetStrategyValue().intValue();
                return getTarget(targetValue);
            case ASYNC_OFF:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAsyncOFFTime);
                customList = readerConfigResp.getCustomList();
                Custom customAsyncOffTime = customList.get(0);
                ThingMagicAsyncOFFTime asyncOffTime = new ThingMagicAsyncOFFTime(customAsyncOffTime);
                return asyncOffTime.getAsyncOFFTime().intValue();
            case ASYNC_ON:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAsyncONTime);
                customList = readerConfigResp.getCustomList();
                Custom customAsyncOnTime = customList.get(0);
                ThingMagicAsyncONTime asyncOnTime = new ThingMagicAsyncONTime(customAsyncOnTime);
                return asyncOnTime.getAsyncONTime().intValue();
            case READERHOSTNAME:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderConfiguration);
                customList = readerConfigResp.getCustomList();
                //catch the ReaderDescription value.
                String readerHostName = null;
                Custom readerHost = customList.get(0);
                ThingMagicReaderConfiguration readerConfig = new ThingMagicReaderConfiguration(readerHost);
                readerHostName = readerConfig.getReaderHostName().toString();
                return readerHostName;
            case GEN2_T4:
                /**
                 * "/reader/gen2/t4" is a custom parameter. It is available as part of
                 * ThingMagicProtocolConfiguration under ThingMagicControlConfiguration.
                 **/
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customProtocolConfiguration = customList.get(0);
                ThingMagicProtocolConfiguration protoConfig = new ThingMagicProtocolConfiguration(customProtocolConfiguration);
                return protoConfig.getGen2CustomParameters().getGen2T4Param().getT4ParamValue();
            case GEN2_PROTOCOLEXTENSION:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicGEN2ProtocolExtension);
                customList = readerConfigResp.getCustomList();
                Custom customProtocolExtension = customList.get(0);
                ThingMagicGEN2ProtocolExtension protocolExtension = new ThingMagicGEN2ProtocolExtension(customProtocolExtension);
                int gen2ProtocolExtensionValue = protocolExtension.getGEN2ProtocolExtension().intValue();
                return Gen2.ProtocolExtension.get(gen2ProtocolExtensionValue);
            case REGULATORY_MODE:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicRegulatoryConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customRegMode = customList.get(0);
                ThingMagicRegulatoryConfiguration regConfig = new ThingMagicRegulatoryConfiguration(customRegMode);
                return regConfig.getRegulatoryMode().getModeParam();
            case REGULATORY_MODULATION:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicRegulatoryConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customRegModulation = customList.get(0);
                ThingMagicRegulatoryConfiguration regConfiguration = new ThingMagicRegulatoryConfiguration(customRegModulation);
                return regConfiguration.getRegulatoryModulation().getModulationParam();
            case REGULATORY_ONTIME:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicRegulatoryConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customRegOnTime = customList.get(0);
                ThingMagicRegulatoryConfiguration regconfig = new ThingMagicRegulatoryConfiguration(customRegOnTime);
                return regconfig.getRegulatoryOntime().getOntimeParam();
            case REGULATORY_OFFTIME:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicRegulatoryConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customRegOffTime = customList.get(0);
                ThingMagicRegulatoryConfiguration regltryConfig = new ThingMagicRegulatoryConfiguration(customRegOffTime);
                return regltryConfig.getRegulatoryOfftime().getOfftimeParam();
            case ANTENNA_RETURNLOSS:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaReturnloss);
                customList = readerConfigResp.getCustomList();
                Custom customAntennaReturnLoss = customList.get(0);
                ThingMagicAntennaReturnloss antRetLoss = new ThingMagicAntennaReturnloss(customAntennaReturnLoss);
                List<ReturnlossValue> list = antRetLoss.getReturnlossValueList();
                int[][] response = new int[list.size()][];
                for(int i = 0; i < list.size(); i++)
                {
                    response[i] = new int[]{list.get(i).getPort().intValue(), list.get(i).getValue().intValue()};
                }
                return response;
            case READER_METADATA:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicMetadata);
                customList = readerConfigResp.getCustomList();
                Custom customReaderMetadata = customList.get(0);
                ThingMagicMetadata thingMagicMetadata = new ThingMagicMetadata(customReaderMetadata);
                int customMetadata = thingMagicMetadata.getMetadata().toInteger();
                return customMetadata;
            case READER_STATS_ENABLE:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicStatsEnable);
                customList = readerConfigResp.getCustomList();
                Custom customStatsEnable = customList.get(0);
                ThingMagicStatsEnable thingMagicStatsEnable = new ThingMagicStatsEnable(customStatsEnable);
                int customStatsValue = thingMagicStatsEnable.getStatsEnable().toInteger();
                return customStatsValue;
            case READER_STATS:
                CustomStatsValue cStatsFlag = null;
                boolean allstats = false;
                SerialReader.ReaderStats statValues = new SerialReader.ReaderStats();
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderStats);
                customList = readerConfigResp.getCustomList();
                Custom customReaderStats = customList.get(0);
                ThingMagicReaderStats tmReaderStats = new ThingMagicReaderStats(customReaderStats);
                cStatsFlag = tmReaderStats.getCustomStatsValue();
                statValues = processCustomStats(statValues, cStatsFlag, allstats);
                return statValues;
            case PORT_SWITCH_GPOS:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicPortSwitchGPO);
                customList = readerConfigResp.getCustomList();
                Custom customPortSwGpos = customList.get(0);
                ThingMagicPortSwitchGPO portSwGpos = new ThingMagicPortSwitchGPO(customPortSwGpos);
                UnsignedByteArray byteArray = portSwGpos.getportSwitchGPOList();
                byte[] bArr = new byte[byteArray.getByteLength()];
                for(int i = 0; i< byteArray.getByteLength(); i++)
                {
                    bArr[i] = byteArray.get(i).toByte();
                }
                int[] gposList = new int[bArr.length];
                for(int i = 0; i < bArr.length; i++)
                {
                    gposList[i] = (int)bArr[i];
                }
                return gposList;
        }
        return configParam;
    }

    public Gen2.Target getTarget(int targetValue)
    {
        Gen2.Target target = null;
        switch (targetValue)
        {
            case 0:
                target = Gen2.Target.A;
                break;
            case 1:
                target = Gen2.Target.B;
                break;
            case 2:
                target = Gen2.Target.AB;
                break;
            case 3:
                target = Gen2.Target.BA;
                break;
        }
        return target;
    }
    protected Object getCustomISO18k6bProtocolConfiguration(ReaderConfigParams configParams) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResp;
        List<Custom> customList;
        ThingMagicProtocolConfiguration protocolConfiguration;
        switch(configParams)
        {
             case ISO180006B_BLF:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom customProtocol = customList.get(0);
                protocolConfiguration = new ThingMagicProtocolConfiguration(customProtocol);
                ThingMagicISO18K6BLinkFrequency tp = protocolConfiguration.getISO18K6BCustomParameters().getThingMagicISO18K6BLinkFrequency();
                int data = protocolConfiguration.getISO18K6BCustomParameters().getThingMagicISO18K6BLinkFrequency().getISO18K6BLinkFrequency().toInteger();
                return getI186bLinkFrequency(data);
             case ISO180006B_DELIMITER:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom protocolList = customList.get(0);
                protocolConfiguration = new ThingMagicProtocolConfiguration(protocolList);
                int delimiter = protocolConfiguration.getISO18K6BCustomParameters().getThingMagicISO180006BDelimiter().getISO18K6BDelimiter().toInteger();
                return Iso180006b.Delimiter.get(delimiter);
            case ISO180006B_MODULATIONDEPTH:
                readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
                customList = readerConfigResp.getCustomList();
                Custom protocol = customList.get(0);
                protocolConfiguration = new ThingMagicProtocolConfiguration(protocol);
                int modulationDepth = protocolConfiguration.getISO18K6BCustomParameters().getThingMagicISO18K6BModulationDepth().getISO18K6BModulationDepth().toInteger();
                return Iso180006b.ModulationDepth.get(modulationDepth);
            default:
                throw new ReaderException("Unknown parameter");
        }
    }
    protected Object getReaderCapabilities(ReaderConfigParams configParams) throws ReaderException
    {
       //Initialize GET_READER_CAPABILITIES message
       GET_READER_CAPABILITIES readerCapabilities=new GET_READER_CAPABILITIES();
       GET_READER_CAPABILITIES_RESPONSE readerResponse;
       switch(configParams)
       {
           case MODEL:
               readerCapabilities.setRequestedData(new GetReaderCapabilitiesRequestedData(GetReaderCapabilitiesRequestedData.General_Device_Capabilities));
               //now the message is fully framed send the message
               readerResponse = (GET_READER_CAPABILITIES_RESPONSE) LLRP_SendReceive(readerCapabilities);
               return parseGetModel(readerResponse);
           case SOFTWARE_VERSION:
               readerCapabilities.setRequestedData(new GetReaderCapabilitiesRequestedData(GetReaderCapabilitiesRequestedData.General_Device_Capabilities));
               //now the message is fully framed send the message
               readerResponse = (GET_READER_CAPABILITIES_RESPONSE) LLRP_SendReceive(readerCapabilities);
               return parseGetFirmwareVersion(readerResponse);
       }
       return configParams;
    }

    private Object getCustomReaderCapabilities(ReaderConfigParams configParams) throws ReaderException
    {
        GET_READER_CAPABILITIES_RESPONSE readerCapabilitiesResp;
        readerCapabilitiesResp = getCustomReaderCapabilitiesResponse(ThingMagicControlCapabilities.DeviceInformationCapabilities);
        //cache the Hardware Version value
        List<Custom> hwList = readerCapabilitiesResp.getCustomList();
        Custom getHardWare = hwList.get(0);
        DeviceInformationCapabilities informationCapabilities = new DeviceInformationCapabilities(getHardWare);
        switch(configParams)
        {
            case HARDWARE_VERSION:
                String hardwareVersion = null;
                hardwareVersion = informationCapabilities.getHardwareVersion().toString();
                return hardwareVersion;

            case SERIAL:
                String serialVersion = null;
                serialVersion = informationCapabilities.getReaderSerialNumber().toString();
                return serialVersion;

            case PRODUCT_ID:
                int productID = informationCapabilities.getReaderProductID().getProductID().intValue();
                return productID;

            case PRODUCT_GROUP_ID:
                int productGroupID = informationCapabilities.getReaderProductGroupID().getProductGroupID().intValue();
                return productGroupID;

            case PRODUCT_GROUP:
                String productGroup = null;
                productGroup = informationCapabilities.getReaderProductGroup().getProductGroup().toString();
                return productGroup;
        }
        return configParams;
    }
    private Object parseGetReaderParam(ReaderConfigParams configParam, int activeMode)
    {
        RFMode rfMode = capabilitiesCache.get(activeMode);

        switch(configParam)
        {
            case GEN2_BLF:
                return rfMode.getbDRValue();
            case TAGENCODING:
                return rfMode.getmValue();
        }
        return null;
    }

    @Override
    public void receiveAutonomousReading() {
        throw new UnsupportedOperationException("Not supported yet."); //To change body of generated methods, choose Tools | Templates.
    }

    @Override
    public void regionConfiguration(boolean lbtEnable, int lbtThreshold, boolean dwellTimeEnable, int dwellTime) throws ReaderException{
        throw new UnsupportedOperationException("Not supported yet."); //To change body of generated methods, choose Tools | Templates.
    }

    private static class ActiveModeIndex
     {
        int activeModeIndex;
        int tari;

        public int getActiveModeIndex()
        {
            return activeModeIndex;
        }

        public void setActiveModeIndex(int activeModeIndex)
        {
            this.activeModeIndex = activeModeIndex;
        }

        public int getTari()
        {
            return tari;
        }

        public void setTari(int tari)
        {
            this.tari = tari;
        }
    }

    private ActiveModeIndex getActiveMode() throws ReaderException
    {
        TM_GET_READER_CONFIG readerConfig = new TM_GET_READER_CONFIG();
        GetReaderConfigRequestedData reqData = new GetReaderConfigRequestedData();
        // Reader configuration is available as part of antenna Configuration
        reqData.set(GetReaderConfigRequestedData.AntennaConfiguration);
        readerConfig.setRequestedData(reqData);
        // Now the message is fully framed, Send the message
        GET_READER_CONFIG_RESPONSE readerConfigResp = (GET_READER_CONFIG_RESPONSE) LLRP_SendReceive(readerConfig);
        // get active mode index
        // Fetch the antenna configurationList from the response
        List<AntennaConfiguration> antConfigList = ((GET_READER_CONFIG_RESPONSE)readerConfigResp).getAntennaConfigurationList();
        ActiveModeIndex activeModeIndex = new ActiveModeIndex();
        for(AntennaConfiguration getAntCfg : antConfigList)
        {
            List<AirProtocolInventoryCommandSettings> getProtocolList = getAntCfg.getAirProtocolInventoryCommandSettingsList();
            for(AirProtocolInventoryCommandSettings apICSettings : getProtocolList)
            {
                activeModeIndex.setActiveModeIndex(((C1G2InventoryCommand)apICSettings).getC1G2RFControl().getModeIndex().intValue());
                activeModeIndex.setTari(((C1G2InventoryCommand)apICSettings).getC1G2RFControl().getTari().intValue());
            }
        }
        return activeModeIndex;
    }

    /**
     * Setting Standard Reader Configuration
     * @param configParam
     * @param configValue     
     * @throws ReaderException
     */
    private void setReaderConfiguration(ReaderConfigParams configParam,Object configValue) throws ReaderException
    {   
        // Create Set reader configuration message
        SET_READER_CONFIG setReaderConfig = new SET_READER_CONFIG();
        SET_READER_CONFIG_RESPONSE setReaderConfigResp;
        // ResetToFactoryDefault should be zero to disable resetting the members
        setReaderConfig.setResetToFactoryDefault(new Bit(0));
        switch (configParam)
        {
            case PORTREADPOWERLIST:
                List<AntennaConfiguration> antConfig = new ArrayList<AntennaConfiguration>();
                antConfig = makeSetPortReadPowerList(configValue);
                setReaderConfig.setAntennaConfigurationList(antConfig);
                break;
            case GEN2_BLF:
                ActiveModeIndex activeMode = getActiveMode();
                int getActiveMode = activeMode.getActiveModeIndex();
                Gen2.LinkFrequency setFreq = (Gen2.LinkFrequency)configValue;
                RFMode rfMode = capabilitiesCache.get(getActiveMode);
                int activeFreq = getLinkFrequency(rfMode.getbDRValue()).rep;
                String mValue = rfMode.getmValue();

                // find out the M_Value based on BLF specified by user and set the respective mode
                
                // user input 250, if its already 250...no need to set
                // else get M value and based on that set  respective mode
                // user input 640, if its already 640 active ... no need to set as only one mode for 640
                // else set mode 5
                boolean rfSet = false;
                if (setFreq.rep != activeFreq)
                {
                    // change mode only if existing mode is not the same
                    for (Map.Entry<Integer, RFMode> capEntry : capabilitiesCache.entrySet())
                    {
                        RFMode capList = capEntry.getValue();   
                        if (capList.getbDRValue().contains(String.valueOf(setFreq.rep)) && capList.getmValue().equals(mValue))
                        {
                            int modelIndex = capEntry.getKey();
                            setRFControlMode(modelIndex, activeMode);
                            rfSet = true;
                            break;
                        }
                    }
                    if(!rfSet)
                    {
                        throw new ReaderException("RF Mode cant be set");
                    }
                }
                break;
            case GEN2_TARI:
                List<AntennaConfiguration> antConfigList = makeSetTari(configValue);
                setReaderConfig.setAntennaConfigurationList(antConfigList);
                break;
            case READPOWER:
                List<AntennaConfiguration> readPowerList = makeSetReadPower(configValue);
                setReaderConfig.setAntennaConfigurationList(readPowerList);
                break;            
            case SESSION:
                List<AntennaConfiguration> sessionList = makeSetSession(configValue);
                setReaderConfig.setAntennaConfigurationList(sessionList);
                break;
            case TAGENCODING:
                ActiveModeIndex actMode = getActiveMode();
                int activeIndex = actMode.getActiveModeIndex();
                int tagEncoding = Gen2.TagEncoding.get((Gen2.TagEncoding)configValue);
                RFMode _rfMode = capabilitiesCache.get(activeIndex);
                C1G2MValue c1g2MValue = new C1G2MValue();
                int _mValue = c1g2MValue.getValue(_rfMode.getmValue());
                String actFreq = _rfMode.getbDRValue();
                boolean _rfSet = false;
                // Set only if the active m value is not same
                if (!(tagEncoding == _mValue))
                {
                    for (Map.Entry<Integer, RFMode> capEntry : capabilitiesCache.entrySet())
                    {
                        RFMode capList = capEntry.getValue();
                        // Find a suitable mode with the tagEncodingVal and actFreq
                        if (actFreq.equals(capList.getbDRValue()) &&c1g2MValue.getValue(capList.getmValue()) == tagEncoding)
                        {
                            int modelIndex = capEntry.getKey();
                            setRFControlMode(modelIndex, actMode);
                            _rfSet = true;
                            break;
                        }
                    }
                    if(!_rfSet)
                    {
                            throw new ReaderException("RF Mode cant be set");
                    }
                }
                break;
            case HOLDEVENTSANDREPORTS:
                EventsAndReports eventsAndReports = new EventsAndReports();
                boolean holdFlag = (Boolean)configValue;
                eventsAndReports.setHoldEventsAndReportsUponReconnect(new Bit(holdFlag));
                setReaderConfig.setEventsAndReports(eventsAndReports);
                break;
        }//end of switch case
        //Now the message is fully framed, Send the message
        setReaderConfigResp = (SET_READER_CONFIG_RESPONSE) LLRP_SendReceive(setReaderConfig);
        if(setReaderConfigResp != null)
        {
            LLRPStatus status = setReaderConfigResp.getLLRPStatus();
            String statusCode = status.getStatusCode().toString();
            if(!(statusCode.equals("M_Success")))
            {
                throw  new ReaderException(status.getErrorDescription().toString());
            }
        }
    }

    private List makeSetSession(Object configValue) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
        List<AntennaConfiguration> oldAntConfigList = readerConfigResponse.getAntennaConfigurationList();
        int index = 0;
        List<AntennaConfiguration> newAntConfigList = new ArrayList<AntennaConfiguration>();
        int val = (Enum.valueOf(Gen2.Session.class, configValue.toString())).rep;

        for (AntennaConfiguration antennaConfiguration : oldAntConfigList)
        {
            List<AirProtocolInventoryCommandSettings> airProtocolInventoryList = antennaConfiguration.getAirProtocolInventoryCommandSettingsList();
            AirProtocolInventoryCommandSettings aPICSettings = airProtocolInventoryList.get(0);
            C1G2InventoryCommand inventoryCommand = (C1G2InventoryCommand)aPICSettings;
            C1G2SingulationControl c1g2SingulationControl = inventoryCommand.getC1G2SingulationControl();
            c1g2SingulationControl.setSession(new TwoBitField(Integer.toString(val)));
            inventoryCommand.setC1G2SingulationControl(c1g2SingulationControl);
            airProtocolInventoryList.add(inventoryCommand);
            antennaConfiguration.setAirProtocolInventoryCommandSettingsList(airProtocolInventoryList);
            newAntConfigList.add(antennaConfiguration);
            index++;
        }
        return newAntConfigList;
    }

    private void setCustomReaderConfiguration(ReaderConfigParams configParam,Object configValue) throws ReaderException
    {
        // Create Set reader configuration message
        SET_READER_CONFIG setReaderConfig = new SET_READER_CONFIG();
        SET_READER_CONFIG_RESPONSE setReaderConfigResp;
        // ResetToFactoryDefault should be zero to disable resetting the members
        setReaderConfig.setResetToFactoryDefault(new Bit(0));
        switch (configParam)
        {
            case PORTWRITEPOWERLIST:
                List<Custom> tmAntConfig = new ArrayList<Custom>();
                tmAntConfig = makeSetPortWritePowerList(configValue);
                setReaderConfig.setCustomList(tmAntConfig);
                break;
            case READERDESCRIPTION:
                ThingMagicReaderConfiguration readerConfiguration = makeSetReaderDescription(configValue);
                setReaderConfig.addToCustomList(readerConfiguration);
                break;
            case UNIQUEBYANTENNA:
                ThingMagicDeDuplication customDeduplication = getCustomDeduplication();
                customDeduplication.setUniqueByAntenna(new Bit((Boolean)configValue));
                setReaderConfig.addToCustomList(customDeduplication);
                break;
            case UNIQUEBYDATA:
                ThingMagicDeDuplication tmDeduplication = getCustomDeduplication();
                tmDeduplication.setUniqueByData(new Bit((Boolean)configValue));
                setReaderConfig.addToCustomList(tmDeduplication);
                break;
            case RECORDHIGHESTRSSI:
                ThingMagicDeDuplication thingmagicDeduplication = getCustomDeduplication();
                thingmagicDeduplication.setRecordHighestRSSI(new Bit((Boolean)configValue));
                setReaderConfig.addToCustomList(thingmagicDeduplication);
                break;
            case GEN2_Q:
               ThingMagicProtocolConfiguration protocolConfig = makeSetGen2Q(configValue);
               setReaderConfig.addToCustomList(protocolConfig);
               break;
            case CHECKPORT:
                ThingMagicAntennaDetection antennaDetection = new ThingMagicAntennaDetection();
                boolean antennaDetect = (Boolean)configValue;
                antennaDetection.setAntennaDetection(new Bit(antennaDetect));
                setReaderConfig.addToCustomList(antennaDetection);
                break;
            case WRITEPOWER:
                List<Custom> antennaConfigList = makeSetWritePower(configValue);
                setReaderConfig.setCustomList(antennaConfigList);
                break;
            case TARGET:
                ThingMagicProtocolConfiguration protocolConfiguration = makeSetTarget(configValue);
                setReaderConfig.addToCustomList(protocolConfiguration);
                break;
            case LICENSEKEY:
                ThingMagicLicenseKey licenseKey = new ThingMagicLicenseKey();
                UnsignedByteArray ubArray = new UnsignedByteArray((byte[])configValue);
                licenseKey.setLicenseKey(ubArray);
                setReaderConfig.addToCustomList(licenseKey);
                break;
            case ASYNC_OFF:
                ThingMagicAsyncOFFTime tmAsyncOffTime = new ThingMagicAsyncOFFTime();
                int asyncOffTime = (Integer)configValue;
                tmAsyncOffTime.setAsyncOFFTime(new UnsignedInteger(asyncOffTime));
                setReaderConfig.addToCustomList(tmAsyncOffTime);
                break;
            case ASYNC_ON:
                ThingMagicAsyncONTime tmAsyncOnTime = new ThingMagicAsyncONTime();
                int asyncOnTime = (Integer)configValue;
                tmAsyncOnTime.setAsyncONTime(new UnsignedInteger(asyncOnTime));
                setReaderConfig.addToCustomList(tmAsyncOnTime);
                break;
            case READERHOSTNAME:
                ThingMagicReaderConfiguration readerHostName = makeSetReaderHostName(configValue);
                setReaderConfig.addToCustomList(readerHostName);
                break;
            case GEN2_T4:
               ThingMagicProtocolConfiguration protoConfig = makeSetGen2T4(configValue);
               setReaderConfig.addToCustomList(protoConfig);
               break;
            case REGULATORY_MODE:
                ThingMagicRegulatoryConfiguration regConfig = new ThingMagicRegulatoryConfiguration();
                ThingMagicRegulatoryMode modeParam = new ThingMagicRegulatoryMode(configValue.toString()); 
                org.llrp.ltk.generated.custom.parameters.RegulatoryMode mode = new org.llrp.ltk.generated.custom.parameters.RegulatoryMode();
                mode.setModeParam(modeParam);
                regConfig.setRegulatoryMode(mode);
                setReaderConfig.addToCustomList(regConfig);
                break;
            case REGULATORY_MODULATION: 
                ThingMagicRegulatoryConfiguration regulatoryConfig = new ThingMagicRegulatoryConfiguration();
                ThingMagicRegulatoryModulation modulationParam = new ThingMagicRegulatoryModulation(configValue.toString()); 
                org.llrp.ltk.generated.custom.parameters.RegulatoryModulation modulation = new org.llrp.ltk.generated.custom.parameters.RegulatoryModulation();
                modulation.setModulationParam(modulationParam);
                regulatoryConfig.setRegulatoryModulation(modulation);
                setReaderConfig.addToCustomList(regulatoryConfig);
                break;
            case REGULATORY_ONTIME: 
                ThingMagicRegulatoryConfiguration regulatoryConfiguration = new ThingMagicRegulatoryConfiguration();
                org.llrp.ltk.generated.custom.parameters.RegulatoryOntime onTime = new org.llrp.ltk.generated.custom.parameters.RegulatoryOntime();
                int regOnTime = (Integer)configValue;
                onTime.setOntimeParam(new UnsignedInteger(regOnTime));
                regulatoryConfiguration.setRegulatoryOntime(onTime);
                setReaderConfig.addToCustomList(regulatoryConfiguration);
                break;
            case REGULATORY_OFFTIME: 
                ThingMagicRegulatoryConfiguration regConfiguration = new ThingMagicRegulatoryConfiguration();
                org.llrp.ltk.generated.custom.parameters.RegulatoryOfftime offTime = new org.llrp.ltk.generated.custom.parameters.RegulatoryOfftime();
                int regOffTime = (Integer)configValue;
                offTime.setOfftimeParam(new UnsignedInteger(regOffTime));
                regConfiguration.setRegulatoryOfftime(offTime);
                setReaderConfig.addToCustomList(regConfiguration);
                break;
            case REGULATORY_ENABLE:
                ThingMagicRegulatoryConfiguration regconfig = new ThingMagicRegulatoryConfiguration();
                org.llrp.ltk.generated.custom.parameters.RegulatoryEnable enable = new org.llrp.ltk.generated.custom.parameters.RegulatoryEnable();
                enable.setEnableParam(new Bit((Boolean)configValue));
                regconfig.setRegulatoryEnable(enable);
                setReaderConfig.addToCustomList(regconfig);
                break;
            case REGION_HOPTABLE:
               /* Thingmagic Frequency configuration is available as a custom parameter
                  and can be set through SET_READER_CONFIG */
                ThingMagicFrequencyConfiguration freqConfig = setRegionHopTable(configValue);
                setReaderConfig.addToCustomList(freqConfig);
                break;
            case GEN2_INITQ:
               ThingMagicProtocolConfiguration protoConfiguration = makeSetGen2InitQ(configValue);
               setReaderConfig.addToCustomList(protoConfiguration);
               break;
            case SENDSELECT:
               ThingMagicProtocolConfiguration protoConf = makeSetGen2SendSelect(configValue);
               setReaderConfig.addToCustomList(protoConf);
               break;
            case READER_METADATA:
                ThingMagicMetadata readerMetadata = new ThingMagicMetadata();
                readerMetadata.setMetadata(new UnsignedShort((Integer) configValue));
                setReaderConfig.addToCustomList(readerMetadata);
                break;
            case READER_STATS_ENABLE:
                ThingMagicStatsEnable tmStatsEnable = new ThingMagicStatsEnable();
                tmStatsEnable.setStatsEnable(new UnsignedShort((Integer) configValue));
                setReaderConfig.addToCustomList(tmStatsEnable);
                break;
            case PORT_SWITCH_GPOS:
                ThingMagicPortSwitchGPO gpos = new ThingMagicPortSwitchGPO();
                int[] values = (int[])configValue;
                byte[] gposList = new byte[values.length];
                for(int i= 0; i < values.length; i++)
                {
                    gposList[i] = (byte)values[i];
                }
                gpos.setportSwitchGPOList(new UnsignedByteArray(gposList));
                setReaderConfig.addToCustomList(gpos);
                break;
        }
            //now the message is fully framed,send the message
            setReaderConfigResp = (SET_READER_CONFIG_RESPONSE) LLRP_SendReceive(setReaderConfig);
            if(setReaderConfigResp != null)
            {
                LLRPStatus status = setReaderConfigResp.getLLRPStatus();
                String statusCode = status.getStatusCode().toString();
                if(!(statusCode.equals("M_Success")))
                {
                    throw  new ReaderException(status.getErrorDescription().toString());
                }
            }
        }

     // Method to set Custom Hoptable param value
     private ThingMagicFrequencyConfiguration setRegionHopTable(Object configValue) throws ReaderException
     {
        /* Initialize Frequency configuration */
        ThingMagicFrequencyConfiguration freqConfig = new ThingMagicFrequencyConfiguration();

        /*Set Hopping to 1 to set Frequncy Hoptable*/
        freqConfig.setHopping(new Bit(1));

        /* Initialize Frequency Hoptable */
        CustomFrequencyHopTable custFreqHopTable = new CustomFrequencyHopTable();
        custFreqHopTable.setHopTableID(new UnsignedByte(1));
        int[] hopTableList = (int[])configValue;
        UnsignedInteger []ints = new UnsignedInteger[hopTableList.length];
        for(int i = 0; i < hopTableList.length ; i++)
        {
            ints[i] = new UnsignedInteger(hopTableList[i]);
        }
        UnsignedIntegerArray array = new UnsignedIntegerArray(ints);
        custFreqHopTable.setFrequency(array);
        List<CustomFrequencyHopTable> customFrequencyHopTableList = new ArrayList<CustomFrequencyHopTable>();
        customFrequencyHopTableList.add(custFreqHopTable);

         /* Set Frequency HopTable parameter to frequency configuration */
        freqConfig.setCustomFrequencyHopTableList(customFrequencyHopTableList);
        return freqConfig;
     }

     private ThingMagicProtocolConfiguration makeSetTarget(Object configValue) throws ReaderException
     {
         GET_READER_CONFIG_RESPONSE readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
         List<Custom> customList = readerConfigResp.getCustomList();
         Custom customProtocolConfig = customList.get(0);
         ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration(customProtocolConfig);
         Gen2CustomParameters gen2CustomParameters = protocolConfiguration.getGen2CustomParameters();
         int val = getTargetValue(configValue);
         ThingMagicTargetStrategy target = new ThingMagicTargetStrategy();
         target.setThingMagicTargetStrategyValue(new ThingMagicC1G2TargetStrategy(val));
         gen2CustomParameters.setThingMagicTargetStrategy(target);
         protocolConfiguration.setGen2CustomParameters(gen2CustomParameters);
         return protocolConfiguration;
     }

     private void setCustomIS018k6bProtocolConfiguration(Object configValue, ReaderConfigParams configParams) throws ReaderException
     {
         // Create Set reader configuration message
         SET_READER_CONFIG setReaderConfig = new SET_READER_CONFIG();
         SET_READER_CONFIG_RESPONSE setReaderConfigResp;
         // ResetToFactoryDefault should be zero to disable resetting the members
         setReaderConfig.setResetToFactoryDefault(new Bit(0));

         //Get readerConfigResponse
         GET_READER_CONFIG_RESPONSE readerConfigResp = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicProtocolConfiguration);
         List<Custom> customList = readerConfigResp.getCustomList();
         Custom customProtocolConfig = customList.get(0);
         ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration(customProtocolConfig);

         ISO18K6BCustomParameters i186bCustomParameters = protocolConfiguration.getISO18K6BCustomParameters();
         switch (configParams)
         {
             case ISO180006B_BLF:
                 int val = (Enum.valueOf(Iso180006b.LinkFrequency.class, configValue.toString())).rep;
                 ThingMagicISO18K6BLinkFrequency linkFrequency = new ThingMagicISO18K6BLinkFrequency();
                 ThingMagicCustom18K6BLinkFrequency tmLinkFreq = new ThingMagicCustom18K6BLinkFrequency(val);
                 linkFrequency.setISO18K6BLinkFrequency(tmLinkFreq);
                 i186bCustomParameters.setThingMagicISO18K6BLinkFrequency(linkFrequency);
                 break;
             case ISO180006B_DELIMITER:
                 int delimiter = (Enum.valueOf(Iso180006b.Delimiter.class, configValue.toString())).rep;
                 ThingMagicISO180006BDelimiter is186bDelimiter = new ThingMagicISO180006BDelimiter();
                 ThingMagicCustom18K6BDelimiter tmDelimiter = new ThingMagicCustom18K6BDelimiter(delimiter);
                 is186bDelimiter.setISO18K6BDelimiter(tmDelimiter);
                 i186bCustomParameters.setThingMagicISO180006BDelimiter(is186bDelimiter);
                 break;
             case ISO180006B_MODULATIONDEPTH:
                 int modulationDepth = (Enum.valueOf(Iso180006b.ModulationDepth.class, configValue.toString())).rep;
                 ThingMagicISO18K6BModulationDepth is186bModulationDepth = new ThingMagicISO18K6BModulationDepth();
                 ThingMagicCustom18K6BModulationDepth tmModulationDep = new ThingMagicCustom18K6BModulationDepth(modulationDepth);
                 is186bModulationDepth.setISO18K6BModulationDepth(tmModulationDep);
                 i186bCustomParameters.setThingMagicISO18K6BModulationDepth(is186bModulationDepth);
                 break;
         }
         protocolConfiguration.setISO18K6BCustomParameters(i186bCustomParameters);
         setReaderConfig.addToCustomList(protocolConfiguration);
         //now the message is fully framed,send the message
         setReaderConfigResp = (SET_READER_CONFIG_RESPONSE) LLRP_SendReceive(setReaderConfig);
         if(setReaderConfigResp != null)
         {
             LLRPStatus status = setReaderConfigResp.getLLRPStatus();
             String statusCode = status.getStatusCode().toString();
             if (!(statusCode.equals("M_Success")))
             {
                 throw new ReaderException(status.getErrorDescription().toString());
             }
         }    
     }


     private ThingMagicProtocolConfiguration makeSetGen2Q(Object configValue)
     {
        ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration();
        int[][] qValue = (int[][]) configValue;

        for (int q[] : qValue)
        {
            int qType = q[0];
            int initQValue = q[1];
            Gen2Q gen2q = new Gen2Q();
            gen2q.setGen2QType(new QType(qType));
            gen2q.setInitQValue(new UnsignedByte(initQValue));
            Gen2CustomParameters gen2Params = new Gen2CustomParameters();
            gen2Params.setGen2Q(gen2q);
            protocolConfiguration.setGen2CustomParameters(gen2Params);
        }
        return protocolConfiguration;
    }

    private ThingMagicProtocolConfiguration makeSetGen2T4(Object configValue)
    {
        ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration();
        int gen2T4Value = (Integer)configValue;
        Gen2T4Param t4Param = new Gen2T4Param();
        t4Param.setT4ParamValue(new UnsignedInteger(gen2T4Value));
        Gen2CustomParameters gen2CustomParameters = new Gen2CustomParameters();
        gen2CustomParameters.setGen2T4Param(t4Param);
        protocolConfiguration.setGen2CustomParameters(gen2CustomParameters);
        return protocolConfiguration;
    }

    private ThingMagicProtocolConfiguration makeSetGen2InitQ(Object configValue)
    {
        ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration();
        Gen2.InitQ gen2InitQ = (Gen2.InitQ)configValue;
        InitQ initQ = new InitQ();
        initQ.setqEnable(new Bit(gen2InitQ.qEnable));
        if(gen2InitQ.qEnable)
        {
            qValue qval = new qValue();
            qval.setvalue(new UnsignedByte(gen2InitQ.initialQ));
            initQ.setQValue(qval);
        }
        Gen2CustomParameters gen2CustomParameters = new Gen2CustomParameters();
        gen2CustomParameters.setInitQ(initQ);
        protocolConfiguration.setGen2CustomParameters(gen2CustomParameters);
        return protocolConfiguration;
    }

    private ThingMagicProtocolConfiguration makeSetGen2SendSelect(Object configValue)
    {
        ThingMagicProtocolConfiguration protocolConfiguration = new ThingMagicProtocolConfiguration();
        boolean sendSelect = (Boolean)configValue;
        sendSelect select = new sendSelect();
        select.setselectValue(new Bit(sendSelect));
        Gen2CustomParameters gen2CustomParameters = new Gen2CustomParameters();
        gen2CustomParameters.setSendSelect(select);
        protocolConfiguration.setGen2CustomParameters(gen2CustomParameters);
        return protocolConfiguration;
    }

    private int parseGetReadPower(GET_READER_CONFIG_RESPONSE readerConfigResponse) throws ReaderException
    {
        // Fetch the antenna configurationList from the response
        List<AntennaConfiguration> antConfigList = readerConfigResponse.getAntennaConfigurationList();
        AntennaConfiguration antennaConfiguration = antConfigList.get(0);
        int readValue = antennaConfiguration.getRFTransmitter().getTransmitPower().intValue();
        for (AntennaConfiguration antConfig : antConfigList)
        {
            int readPower = antConfig.getRFTransmitter().getTransmitPower().intValue();
            if (!(readValue == readPower))
            {
                throw new ReaderParseException("Undefined Values");
            }
        }
        return powerLevelMap.get(readValue);
    }
    private List makeSetTari(Object configValue) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
        List<AntennaConfiguration> oldAntConfigList = readerConfigResponse.getAntennaConfigurationList();
        //List<AntennaConfiguration> newAntConfigList = new ArrayList<AntennaConfiguration>();
        int value = getTariValue((Gen2.Tari)configValue);
        int modeIndex = 1;
        for (AntennaConfiguration antennaConfiguration : oldAntConfigList)
        {
            List<AirProtocolInventoryCommandSettings> airProtocolInventoryList = antennaConfiguration.getAirProtocolInventoryCommandSettingsList();
            AirProtocolInventoryCommandSettings aPICSettings = airProtocolInventoryList.get(0);
            C1G2InventoryCommand inventoryCommand = (C1G2InventoryCommand)aPICSettings;
            C1G2RFControl rfControl = inventoryCommand.getC1G2RFControl();
            int setTari = getTariValue((Gen2.Tari)configValue);
            modeIndex = rfControl.getModeIndex().toInteger();
            RFMode rfMode = capabilitiesCache.get(modeIndex);
            int minTari = Integer.parseInt(rfMode.getMinTariValue());
            int maxTari = Integer.parseInt(rfMode.getMaxTariValue());
            if(setTari < minTari || setTari > maxTari)
            {
               throw new ReaderException("Tari value is out of range for this RF mode");
            }
//            rfControl.setTari(new UnsignedShort(value));
//            inventoryCommand.setC1G2RFControl(rfControl);
//            airProtocolInventoryList.add(inventoryCommand);
//            antennaConfiguration.setAirProtocolInventoryCommandSettingsList(airProtocolInventoryList);
//            newAntConfigList.add(antennaConfiguration);
        }
        List<AntennaConfiguration> antCfgList = new ArrayList<AntennaConfiguration>();
        List<AirProtocolInventoryCommandSettings> airProtocolList = new ArrayList<AirProtocolInventoryCommandSettings>();
        AntennaConfiguration antCfg = new AntennaConfiguration();

        C1G2InventoryCommand airProtocol = new C1G2InventoryCommand();

        airProtocol.setTagInventoryStateAware(new Bit(0));
        C1G2RFControl rfControl1 = new C1G2RFControl();
        rfControl1.setModeIndex(new UnsignedShort(modeIndex));
        rfControl1.setTari(new UnsignedShort(value));
        airProtocol.setC1G2RFControl(rfControl1);

        airProtocolList.add(airProtocol);

        antCfg.setAirProtocolInventoryCommandSettingsList(airProtocolList);
        antCfg.setAntennaID(new UnsignedShort(0));
        antCfgList.add(antCfg);
        return antCfgList;
    }

    private int getTariValue(Gen2.Tari value)
    {
        switch(value)
        {
            case TARI_25US:
                return 25000;
            case TARI_12_5US:
                return 12500;
            case TARI_6_25US:
                return 6250;
            default:
                return 0;
        }
    }
    
   /**
     * Get the ConnectedPortList values
     * @param readerConfigResp
     * @return _connectedPortList   
     */
   private int[] parseGetConnectedPortList(GET_READER_CONFIG_RESPONSE readerConfigResp)
   {      
       // Fetch the antenna List from the response
       List<AntennaProperties> antennaList = readerConfigResp.getAntennaPropertiesList();
       int antennaID = 0;
       // Cache the connectedPortList value
       List<Integer> connectedPortList = new ArrayList<Integer>();
       for (AntennaProperties properties : antennaList)
       {
           int antConnected = properties.getAntennaConnected().intValue();
           antennaID++;
           if (0 != antConnected)
           {
               connectedPortList.add(antennaID);
           }
       }
       _connectedPortList = ReaderUtil.buildIntArray(connectedPortList);
       return _connectedPortList;
    }

    /**
     * Get the PortReadPowerList values
     * @param readerConfigResp
     * @return readPowerArray
     */
    private int[][] parseGetPortReadPowerList(GET_READER_CONFIG_RESPONSE readerConfigResp)
    {        
        // Fetch the antenna configurationList from the response
        List<AntennaConfiguration> antConfigList = readerConfigResp.getAntennaConfigurationList();
        int index = 0;
        //Cache the portReadPowerList value
        int[][] readPowerArray = new int[antConfigList.size()][];

        for (AntennaConfiguration antConfig : antConfigList)
        {
            if(powerLevelMap.containsKey(antConfig.getRFTransmitter().getTransmitPower().intValue()))
            {
                readPowerArray[index] = new int[]{
                        antConfig.getAntennaID().intValue(),
                        powerLevelMap.get(antConfig.getRFTransmitter().getTransmitPower().intValue())
                    };
            }
            else
            {
                throw new IllegalArgumentException("Invalid RF TransmitPower index. Key not found.");
            }
            index++;
        }
        return readPowerArray;
   }

   /**
     * Set the PortReadPowerList values
     * @param configValue
     * @return list
     */
   private List makeSetPortReadPowerList(Object configValue) throws ReaderException
   {
       List<AntennaConfiguration> antConfigList = new ArrayList<AntennaConfiguration>();
       int[][] prpListValues = (int[][]) configValue;
       GET_READER_CONFIG_RESPONSE readerConfigResponse = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
       List<AntennaConfiguration> antennaConfigurations = readerConfigResponse.getAntennaConfigurationList();
       for (int i = 0; i < prpListValues.length; i++)
       {
           int[] row = prpListValues[i];
           AntennaConfiguration antConfig = new AntennaConfiguration();
           int antennaId = row[0]; //indicates antenna id
           if (antennaId > 0)
           {
               int power = row[1];
               validatePower(power);
               if (_model.equals(TMR_READER_ASTRA_EX) || _model.equals(TMR_READER_ASTRA200) && regionName.equals(Region.NA) && antennaId == 1)
               {
                   if (power > 3000)
                   {
                       throw new IllegalArgumentException(String.format("Requested power %d too high (RFPowerMax=%d)", power, 3000));
                   }
               }
               if (_model.equals(TMR_READER_ASTRA200) && (regionName.equals(Region.EU)) && (antennaId == 1))
               {
                    if (power > 2900)
                    {
                        throw new IllegalArgumentException(String.format("Requested power %d too high (RFPowerMax=%d)", power, 2900));
                    }
               }
               /*
                * if the value provided by the user is within the range
                * but not in the powertable always down grade the value
                * to the nearest available lower power value
                */
               if (powerLevelReverseMap.get(power) == null)
               {
                   power = powerLevelReverseMap.headMap(power).lastKey();
               }
               int powerIndex = powerLevelReverseMap.get(power);
               antConfig.setAntennaID(new UnsignedShort(antennaId));
               antConfig = antennaConfigurations.get(i);
               RFTransmitter rfTransmitter = antConfig.getRFTransmitter();
               rfTransmitter.setTransmitPower(new UnsignedShort(powerIndex));
               antConfig.setRFTransmitter(rfTransmitter);
           }
           else
           {
               throw new IllegalArgumentException("Antenna id is invalid");
           }
           antConfigList.add(antConfig);
       }
       return antConfigList;
    }

    private List makeSetPortWritePowerList(Object configValue) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaConfiguration);
        List<Custom> antConfigList = readerConfigResponse.getCustomList();
        List<Custom> tmAntConfigList = new ArrayList<Custom>();
        int[][] pwpListValues = (int[][]) configValue;
        for (int i = 0; i < pwpListValues.length; i++)
        {
            int[] row = pwpListValues[i];
            int antennaId = row[0];
            Custom customAntennaConfig = antConfigList.get(i);
            ThingMagicAntennaConfiguration antennaConfiguration = new ThingMagicAntennaConfiguration(customAntennaConfig);
            if (antennaId > 0)
            {
                int power = row[1];
                validatePower(power);
                if (_model.equals(TMR_READER_ASTRA_EX) || _model.equals(TMR_READER_ASTRA200) && regionName.equals(Region.NA) && antennaId == 1)
                {
                    if (power > 3000)
                    {
                        throw new IllegalArgumentException(String.format("Requested power %d too high (RFPowerMax=%d)", power, 3000));
                    }
                }
                if (_model.equals(TMR_READER_ASTRA200) && (regionName.equals(Region.EU)) && (antennaId == 1))
                {
                    if (power > 2900)
                    {
                        throw new IllegalArgumentException(String.format("Requested power %d too high (RFPowerMax=%d)", power, 2900));
                    }
                }
                /*
                 * if the value provided by the user is within the range
                 * but not in the powertable always down grade the value
                 * to the nearest available lower power value
                 */
                if (powerLevelReverseMap.get(power) == null)
                {
                    power = powerLevelReverseMap.headMap(power).lastKey();
                }
                int powerIndex = powerLevelReverseMap.get(power);
                antennaConfiguration.setAntennaID(new UnsignedShort(antennaId));
                WriteTransmitPower writePower = new WriteTransmitPower();
                writePower.setWriteTransmitPower(new UnsignedShort(powerIndex));
                antennaConfiguration.setWriteTransmitPower(writePower);
                //Add antennaConfiguration to tmAntConfigList
                tmAntConfigList.add(antennaConfiguration);
            }
        }
        return tmAntConfigList;
    }
    
    private ThingMagicReaderConfiguration makeSetReaderDescription(Object configValue) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderConfiguration);
        List<Custom> readerConfigList = readerConfigResponse.getCustomList();
        Custom rdConfig = readerConfigList.get(0);
        ThingMagicReaderConfiguration readerConfiguration = new ThingMagicReaderConfiguration(rdConfig);
        readerConfiguration.setReaderDescription(new UTF8String(configValue.toString()));
        return readerConfiguration;
    }

     private ThingMagicReaderConfiguration makeSetReaderHostName(Object configValue) throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicReaderConfiguration);
        List<Custom> readerConfigList = readerConfigResponse.getCustomList();
        Custom rdConfig = readerConfigList.get(0);
        ThingMagicReaderConfiguration readerConfiguration = new ThingMagicReaderConfiguration(rdConfig);
        readerConfiguration.setReaderHostName(new UTF8String(configValue.toString()));
        return readerConfiguration;
    }
    private List<AntennaConfiguration> makeSetReadPower(Object configValue) throws ReaderException
    {
        int power = (Integer) configValue;
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getReaderConfigResponse(GetReaderConfigRequestedData.AntennaConfiguration);
        List<AntennaConfiguration> antConfigList = readerConfigResponse.getAntennaConfigurationList();
        /*
         * if the value provided by the user is within the range
         * but not in the powertable always down grade the value
         * to the nearest available lower power value
         */
        if(powerLevelReverseMap.get(power)== null)
        {
            power = powerLevelReverseMap.headMap(power).lastKey();
        }
        int powerIndex = powerLevelReverseMap.get(power);
        for (AntennaConfiguration antennaConfig : antConfigList)
        {
            RFTransmitter rfTransmitter = antennaConfig.getRFTransmitter();
            rfTransmitter.setTransmitPower(new UnsignedShort(powerIndex));
            antennaConfig.setRFTransmitter(rfTransmitter);
        }
        return antConfigList;
    }

    private List<Custom> makeSetWritePower(Object configValue) throws ReaderException
    {
        int power = (Integer)configValue;
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicAntennaConfiguration);       
        ThingMagicAntennaConfiguration antennaConfiguration ;
        List<Custom> antConfigList = readerConfigResponse.getCustomList();
        /*
         * if the value provided by the user is within the range
         * but not in the powertable always down grade the value
         * to the nearest available lower power value
         */
        if(powerLevelReverseMap.get(power)== null)
        {
            power = powerLevelReverseMap.headMap(power).lastKey();
        }
        int powerIndex = powerLevelReverseMap.get(power);
        for (Custom antennaConfig : antConfigList)
        {
            antennaConfiguration = (ThingMagicAntennaConfiguration)antennaConfig;
            WriteTransmitPower writePower = new WriteTransmitPower();
            writePower.setWriteTransmitPower(new UnsignedShort(powerIndex));
            antennaConfiguration.setWriteTransmitPower(writePower);
        }        
        return antConfigList;
    }

    private void validatePower(int power)
    {
        if (power < _rfPowerMin)
        {
            throw new IllegalArgumentException(String.format("Requested power %d too low (RFPowerMin=%d)", power, _rfPowerMin));
        }
        if (power > _rfPowerMax)
        {
            throw new IllegalArgumentException(String.format("Requested power %d too high (RFPowerMax=%d)", power, _rfPowerMax));
    }
    }

    private ThingMagicDeDuplication getCustomDeduplication() throws ReaderException
    {
        GET_READER_CONFIG_RESPONSE readerConfigResponse = getCustomReaderConfigResponse(ThingMagicControlConfiguration.ThingMagicDeDuplication);
        List<Custom> deduplicationList = readerConfigResponse.getCustomList();
        Custom customDeduplication = deduplicationList.get(0);
        ThingMagicDeDuplication deduplication = new ThingMagicDeDuplication(customDeduplication);
        return deduplication;
    }
    
    /**
     * Initializing PowerMaximum & PowerMinimum
     * @throws ReaderException
     */
     public void initRadioPower() throws ReaderException
     {
         powerLevelReverseMap = new TreeMap<Integer, Integer>();
         powerLevelMap = new TreeMap<Integer, Integer>();
         GET_READER_CAPABILITIES reader_capabilities = new GET_READER_CAPABILITIES();
         // Reader capabilities are available as part of Regular Capabilities
         reader_capabilities.setRequestedData(new GetReaderCapabilitiesRequestedData(GetReaderCapabilitiesRequestedData.Regulatory_Capabilities));
         GET_READER_CAPABILITIES_RESPONSE reader_response = (GET_READER_CAPABILITIES_RESPONSE) LLRP_SendReceive(reader_capabilities);
         List<Integer> powerList = new ArrayList<Integer>();
         // Fetch the UHFBandCapabilities value from the response
         UHFBandCapabilities capabilities = reader_response.getRegulatoryCapabilities().getUHFBandCapabilities();                  
         // Fetch the transmitPowerLevel  List from the capabilities
         List<TransmitPowerLevelTableEntry> transmitPowerTable = capabilities.getTransmitPowerLevelTableEntryList();
        // Extract frequency hopTable from response
        frequencyHopTableList = capabilities.getFrequencyInformation().getFrequencyHopTableList();
        FrequencyHopTable fHop = frequencyHopTableList.get(0);
        freqHopTable = new int[fHop.getFrequency().size()];
        for(int j = 0; j < fHop.getFrequency().size(); j++)
        {
            freqHopTable[j] = fHop.getFrequency().get(j).toInteger();
        }

         int transmitPowerSize = transmitPowerTable.size();
         for (TransmitPowerLevelTableEntry powerLevel : transmitPowerTable)
         {
             int txPower = powerLevel.getTransmitPowerValue().intValue();
             int txPowerIndex = powerLevel.getIndex().intValue();
             powerLevelReverseMap.put(txPower, txPowerIndex);
             powerList.add(txPower);
             powerLevelMap.put(txPowerIndex, txPower);
         }
         // Cache the transmit power values
         powerArray = new int[transmitPowerSize];
         int powerIndex = 0;
         Collections.sort(powerList);
         for (int powerValue : powerList)
         {
             powerArray[powerIndex] = powerValue;
             powerIndex++;
         }        
         //Cache PowerMax & PowerMin value
         _rfPowerMax = powerArray[powerList.size() - 1];
         _rfPowerMin = powerArray[0];

         cacheRFModeTable(capabilities);
    }

    // Method to get Regulatory capabilities
    private int[] getRegulatoryCapabilities() throws ReaderException
    {
        GET_READER_CAPABILITIES reader_capabilities = new GET_READER_CAPABILITIES();
        // Regulatory capabilities are available as part of Reader Capabilities
        reader_capabilities.setRequestedData(new GetReaderCapabilitiesRequestedData(GetReaderCapabilitiesRequestedData.Regulatory_Capabilities));
        GET_READER_CAPABILITIES_RESPONSE reader_response = (GET_READER_CAPABILITIES_RESPONSE) LLRP_SendReceive(reader_capabilities);

        // Fetch the UHFBandCapabilities value from the response
        UHFBandCapabilities capabilities = reader_response.getRegulatoryCapabilities().getUHFBandCapabilities();
        frequencyHopTableList = capabilities.getFrequencyInformation().getFrequencyHopTableList();

        // Extract frequency hopTable from response
        FrequencyHopTable fHop = frequencyHopTableList.get(0);
        freqHopTable = new int[fHop.getFrequency().size()];
        for(int j = 0; j < fHop.getFrequency().size(); j++)
        {
            freqHopTable[j] = fHop.getFrequency().get(j).toInteger();
        }
        return freqHopTable;
    }

    private void cacheRFModeTable(UHFBandCapabilities capabilities)
    {
        capabilitiesCache = new HashMap<Integer, RFMode>();
        List<AirProtocolUHFRFModeTable> rfModeList = capabilities.getAirProtocolUHFRFModeTableList();

        for (AirProtocolUHFRFModeTable rfModeIter : rfModeList)
        {
            List<C1G2UHFRFModeTableEntry> gen2RFList = ((C1G2UHFRFModeTable) rfModeIter).getC1G2UHFRFModeTableEntryList();
            for (C1G2UHFRFModeTableEntry gen2RFMode : gen2RFList)
            {                
                Integer modeIndex = gen2RFMode.getModeIdentifier().toInteger();                
                RFMode rfMode = new RFMode();

                rfMode.setdRValue(gen2RFMode.getDRValue().toString());
                rfMode.setePCHAGTCConformance(gen2RFMode.getEPCHAGTCConformance().toString());
                rfMode.setmValue(gen2RFMode.getMValue().toString());
                rfMode.setForwardLinkModulation(gen2RFMode.getForwardLinkModulation().toString());
                rfMode.setSpectralMaskIndicator(gen2RFMode.getSpectralMaskIndicator().toString());
                rfMode.setbDRValue(gen2RFMode.getBDRValue().toString());
                rfMode.setpIEValue(gen2RFMode.getPIEValue().toString());
                rfMode.setMinTariValue(gen2RFMode.getMinTariValue().toString());
                rfMode.setMaxTariValue(gen2RFMode.getMaxTariValue().toString());
                rfMode.setStepTariValue(gen2RFMode.getStepTariValue().toString());                

                capabilitiesCache.put(modeIndex, rfMode);
            }
        }
    }

    private String parseGetModel(GET_READER_CAPABILITIES_RESPONSE readerResponse)
    {
        String modelName = null;        
        GeneralDeviceCapabilities deviceCapabilities = readerResponse.getGeneralDeviceCapabilities();
        int getModel = deviceCapabilities.getModelName().intValue();
        /*
         * will get the modelName as integer value,
         * so we directly hard code the value.
         */
        if(deviceCapabilities.getDeviceManufacturerName().intValue()== TM_MANUFACTURER_ID)
        {
            switch(getModel)
            {
                case 0x06:
                    modelName = TMR_READER_MERCURY6;
                    break;
                case 0x30:
                    modelName = TMR_READER_ASTRA_EX;
                    break;
                case 0x3430:
                    modelName = TMR_READER_SARGAS;
                    break;
                 case 0x3530:
                    modelName = TMR_READER_IZAR;
                    break;
                 case 0x3630:
                    modelName = TMR_READER_ASTRA200;
                default:
                    modelName = "Unknown";
            }
        }
        return modelName;
    }

    private String parseGetFirmwareVersion(GET_READER_CAPABILITIES_RESPONSE readerResponse)
    {
        //cache the deviceCapabilities from readerResponse
        GeneralDeviceCapabilities deviceCapabilities = readerResponse.getGeneralDeviceCapabilities();
        //cache the firmware version value
        String firmWare = deviceCapabilities.getReaderFirmwareVersion().toString();
        return firmWare;
    }
    
    private GET_READER_CONFIG_RESPONSE getReaderConfigResponse(int requestData) throws ReaderException
    {
        TM_GET_READER_CONFIG readerConfig = new TM_GET_READER_CONFIG();
        GetReaderConfigRequestedData reqData = new GetReaderConfigRequestedData();
        reqData.set(requestData);
        readerConfig.setRequestedData(reqData);
        GET_READER_CONFIG_RESPONSE readerConfigResp = (GET_READER_CONFIG_RESPONSE) LLRP_SendReceive(readerConfig);
        return readerConfigResp;
    }

    private GET_READER_CONFIG_RESPONSE getCustomReaderConfigResponse(int requestData) throws ReaderException
    {
        // Create Get reader config message
        TM_GET_READER_CONFIG readerConfig = new TM_GET_READER_CONFIG();
        GetReaderConfigRequestedData reqData = new GetReaderConfigRequestedData();
        reqData.set(GetReaderConfigRequestedData.Identification);
        readerConfig.setRequestedData(reqData);
        ThingMagicControlConfiguration controlConfig = new ThingMagicControlConfiguration(requestData);
        ThingMagicDeviceControlConfiguration deviceConfiguration = new ThingMagicDeviceControlConfiguration();
        /**
         * Set the requested data (i.e., controlConfig object)
         * And add to GET_READER_CONFIG message.
         **/
        deviceConfiguration.setRequestedData(controlConfig);
        /*
         * LTK JAVA is not allowing if we do not set AntennaID field in deviceConfiguration
         * so setAntenaID as zero
         */
        deviceConfiguration.setAntennaID(new UnsignedShort(0));
        readerConfig.addToCustomList(deviceConfiguration);
        //now the message is fully framed send the message
        GET_READER_CONFIG_RESPONSE readerConfigResp = (GET_READER_CONFIG_RESPONSE) LLRP_SendReceive(readerConfig);
        return readerConfigResp;
    }

    private GET_READER_CAPABILITIES_RESPONSE getCustomReaderCapabilitiesResponse(int requestData) throws ReaderException{
        //Initialize GET_READER_CAPABILITIES message
        GET_READER_CAPABILITIES readerCapabilities = new GET_READER_CAPABILITIES();
        GetReaderCapabilitiesRequestedData requestedData = new GetReaderCapabilitiesRequestedData();
        requestedData.set(GetReaderCapabilitiesRequestedData.General_Device_Capabilities);
        readerCapabilities.setRequestedData(requestedData);
        ThingMagicControlCapabilities controlCapabilities = new ThingMagicControlCapabilities(requestData);
        ThingMagicDeviceControlCapabilities devicecontrolCapabilities = new ThingMagicDeviceControlCapabilities();
        /**
         * Set the requested data
         * And add to GET_READER_CAPABILITIES message.
         **/
        devicecontrolCapabilities.setRequestedData(controlCapabilities);
        readerCapabilities.addToCustomList(devicecontrolCapabilities);
        //now the message is fully framed send the message
        GET_READER_CAPABILITIES_RESPONSE capabilitiesResponse = (GET_READER_CAPABILITIES_RESPONSE) LLRP_SendReceive(readerCapabilities);
        return capabilitiesResponse;
    }

    public enum ReaderConfigParams
    {
        CONNECTEDPORTLIST(1),
        PORTREADPOWERLIST(2),
        PORTWRITEPOWERLIST(3),
        READERDESCRIPTION(4),
        KEEP_ALIVE(5),
        GEN2_BLF(6),
        HARDWARE_VERSION(7),
        MODEL(8),
        SOFTWARE_VERSION(9),
        GEN2_TARI(10),
        SERIAL(11),
        READPOWER(12),
        WRITEPOWER(13),
        UNIQUEBYANTENNA(14),
        UNIQUEBYDATA(15),
        RECORDHIGHESTRSSI(16),
        CURRENTTIME(17),
        GEN2_Q(18),
        TEMPERATURE(19),
        CHECKPORT(20),
        TAGENCODING(21),
        SESSION(22),
        TARGET(23),
        LICENSEKEY(24),
        HOLDEVENTSANDREPORTS(25),
        ISO180006B_BLF(26),
        ISO180006B_MODULATIONDEPTH(27),
        ISO180006B_DELIMITER(28),
        ASYNC_OFF(29),
        READERHOSTNAME(30),
        GEN2_T4(31),
        GEN2_PROTOCOLEXTENSION(32),
        PRODUCT_ID(33),
        PRODUCT_GROUP_ID(34),
        PRODUCT_GROUP(35),
        REGULATORY_MODE(36),
        REGULATORY_MODULATION(37),
        REGULATORY_ONTIME(38),
        REGULATORY_OFFTIME(39),
        REGULATORY_ENABLE(40),
        REGION_HOPTABLE(41),
        GEN2_INITQ(42),
        SENDSELECT(43),
        ASYNC_ON(44),
        ANTENNA_RETURNLOSS(45),
        READER_METADATA(46),
        READER_STATS_ENABLE(47),
        READER_STATS(48),
        PORT_SWITCH_GPOS(49);
        int value;

        ReaderConfigParams(int value)
        {
            this.value = value;
        }
    }
    @Override
    public void destroy()
    {        
        CLOSE_CONNECTION close = new CLOSE_CONNECTION();
        CLOSE_CONNECTION_RESPONSE response = null;
        try
        {
            response = (CLOSE_CONNECTION_RESPONSE) (LLRP_SendReceive(close));
            if (null != response && response.getLLRPStatus().getStatusCode().intValue() == StatusCode.M_Success)
            {
                log("close connection successfull");
                Thread.currentThread().interrupt();
            }
            // adding a bit delay before connecting again (temporary work around)
            Thread.sleep(10);
        }        
        catch (ReaderException re)
        {
            log(re.getMessage());
        }
        catch (InterruptedException ie)
        {
            log(ie.getMessage());
        }
        finally
        {
        log("Closing the connection");
            if (null != monitorKeepAlives)
            { 
                monitorKeepAlives.stop();
            }
            if (null != readerConn)
            {
        readerConn.setEndpoint(null);
            ((LLRPConnector) readerConn).disconnect();
                readerConn = null;
    }
            
    }
    }


    /**
     * This method is only for Query Application in Web UI.
     * When Applet is stopped/destroyed, to make cleanup fast this method is used
     */
    public void destroyConnection()
    {
        CLOSE_CONNECTION close = new CLOSE_CONNECTION();        
        try
        {
            LLRP_Send(close);
        }
        catch (ReaderException re)
        {
            
        }        
        finally
        {
            monitorKeepAlives = null;
            readerConn = null;
            System.exit(0);
        }
    }

    private void setKeepAlive() throws ReaderException
    {                      
        KeepaliveSpec kSpec = new KeepaliveSpec();
        KeepaliveTriggerType kType = new KeepaliveTriggerType();
        kType.set(KeepaliveTriggerType.Periodic);

        kSpec.setKeepaliveTriggerType(kType);
        kSpec.setPeriodicTriggerValue(new UnsignedInteger(KEEPALIVE_TRIGGER));

        SET_READER_CONFIG readerConfig = new SET_READER_CONFIG();
        readerConfig.setResetToFactoryDefault(new Bit(0));
        readerConfig.setKeepaliveSpec(kSpec);
        
        readerConn.getHandler().setKeepAliveForward(true);
        readerConn.getHandler().setKeepAliveAck(true);
        LLRP_SendReceive(readerConfig);
    }

    private void enableEventsAndReports() throws ReaderException
    {
        ENABLE_EVENTS_AND_REPORTS events = new ENABLE_EVENTS_AND_REPORTS();
        /**
         * For ENABLE_EVENTS_AND_REPORTS message, there
         * will be no response. So just send the message
         **/
        LLRP_Send(events);
    }

    private void holdEventsAndReportsUponReconnect(boolean holdFlag) throws ReaderException
    {
        setReaderConfiguration(ReaderConfigParams.HOLDEVENTSANDREPORTS, holdFlag);
    }

    private void enableReaderNotification() throws ReaderException
    {
        ReaderEventNotificationSpec rSpec = new ReaderEventNotificationSpec();

        List<EventNotificationState> eStateList = new ArrayList<EventNotificationState>();

        //TODO : Add required events from TMMPD down the line here
        EventNotificationState eventNotificationState = new EventNotificationState();
        eventNotificationState.setEventType(new NotificationEventType(NotificationEventType.AISpec_Event));
        eventNotificationState.setNotificationState(new Bit(1));
        eStateList.add(eventNotificationState);

        eventNotificationState = new EventNotificationState();
        eventNotificationState.setEventType(new NotificationEventType(NotificationEventType.ROSpec_Event));
        eventNotificationState.setNotificationState(new Bit(1));
        eStateList.add(eventNotificationState);

        rSpec.setEventNotificationStateList(eStateList);

        SET_READER_CONFIG readerConfig = new SET_READER_CONFIG();
        readerConfig.setResetToFactoryDefault(new Bit(0));
        readerConfig.setReaderEventNotificationSpec(rSpec);

        LLRP_SendReceive(readerConfig);
    }

    private void setRFControlMode(int modeIndex,ActiveModeIndex activeModeIndex) throws ReaderException
    {
        SET_READER_CONFIG setConfig = new SET_READER_CONFIG();
        setConfig.setResetToFactoryDefault(new Bit(0));
        List<AntennaConfiguration> antCfgList = new ArrayList<AntennaConfiguration>();
        List<AirProtocolInventoryCommandSettings> airProtocolList = new ArrayList<AirProtocolInventoryCommandSettings>();
        AntennaConfiguration antCfg = new AntennaConfiguration();
        int tari = activeModeIndex.getTari();

        C1G2InventoryCommand airProtocol = new C1G2InventoryCommand();

        airProtocol.setTagInventoryStateAware(new Bit(0));
        C1G2RFControl rfControl = new C1G2RFControl();
        rfControl.setModeIndex(new UnsignedShort(modeIndex));
        rfControl.setTari(new UnsignedShort(tari));
        airProtocol.setC1G2RFControl(rfControl);

        airProtocolList.add(airProtocol);

        antCfg.setAirProtocolInventoryCommandSettingsList(airProtocolList);
        antCfg.setAntennaID(new UnsignedShort(0));
        antCfgList.add(antCfg);
        setConfig.setAntennaConfigurationList(antCfgList);

        SET_READER_CONFIG_RESPONSE setResponse = (SET_READER_CONFIG_RESPONSE)LLRP_SendReceive(setConfig);
        log(setResponse.getLLRPStatus().getStatusCode().toString());
    }


    @Override
    public TagReadData[] read(long duration) throws ReaderException
    {
        readData = new Vector<TagReadData>();
        enableEventsAndReports();
        enableReaderNotification();
        ReadPlan rp = (ReadPlan)paramGet(TMR_PARAM_READ_PLAN);
        readInternal(rp, duration);
        // Reset num of readplans to 0 at the end of operation
        setNumPlans(0);
        return readData.toArray(new TagReadData[readData.size()]);
    }

    private void readInternal(ReadPlan rp, long duration) throws ReaderException
    {
        deleteROSpecs();
        deleteAccessSpecs();
        roSpecId = 0;
        accessSpecId = 0;
        opSpecId = 0;
        endOfROSpec = false;
        endOfAISpec = false;
        mapRoSpecIdToProtocol = new HashMap<Integer,TagProtocol>();

        startBackgroundParser();
        
        List<ROSpec> roSpecList = new ArrayList<ROSpec>();
        buildROSpec(rp, duration, roSpecList);        
        enableROSpecFlags(roSpecList.size());
        for(ROSpec roSpec : roSpecList)
        {            
            if (addROSpec(roSpec) && enableROSpec(roSpec.getROSpecID().intValue()))
            {
                if (!startROSpec(roSpec.getROSpecID().intValue()))
                {
                    endOfROSpecFlags[roSpec.getROSpecID().intValue() - 1] = true;
                }
            }
            else
            {
                endOfROSpecFlags[roSpec.getROSpecID().intValue()-1] = true;
            }
        }
        // verify for any failures during ROSPEC enable, add or start
        verifyROSpecEndStatus();
        msgStartTime = System.currentTimeMillis();
        while (!endOfROSpec)
        {
            long currentTime = System.currentTimeMillis();
            long diff = currentTime - msgStartTime;
            if(diff > duration + transportTimeout)
            {
                throw new ReaderException("Timeout");
            }
            try{
                //wait for end of ROSpec/AISpec event
                // This sleep causes other threads waiting for CPU active.
                Thread.sleep(10);
            } catch (InterruptedException ex) {
                llrpLogger.error(ex.getMessage());
            }
        }
        stopBackgroundParser();
    }

    protected synchronized void startBackgroundParser()
    {
        stopRequested = false;        
        if (null == tagProcessor)
        {
            tagProcessor = new TagProcessor(this);
            bkgThread = new Thread(tagProcessor);
            bkgThread.setDaemon(true);
            bkgThread.setPriority(Thread.MIN_PRIORITY);
            bkgThread.start();
        }
        
        if (null == rfReportProcessor)
        {
            rfReportProcessor = new RFReportProcessor(this);
            rfThread = new Thread(rfReportProcessor);
            rfThread.setDaemon(true);
            rfThread.setPriority(Thread.MIN_PRIORITY);
            rfThread.start();
        }
    }

    protected synchronized void stopBackgroundParser()
    {
        stopRequested = true;
        if (null != tagProcessor)
        {
            tagProcessor.parseOff();
            tagProcessor = null;
            bkgThread.interrupt();
            bkgThread = null;
        }
        if (null != rfReportProcessor)
        {
            rfReportProcessor.parseOff();
            rfReportProcessor = null;
            rfThread.interrupt();
            rfThread = null;
        }
    }

    private void sendMessage(LLRPMessage message) throws ReaderException
    {
        log("Sending LLRP Messag ...." + message.getName());
        notifyTransportListeners(message, true, 0);
        readerConn.send(message);
    }
    
    /**
     * Stopping orphan running/active ROSpecs
     */
    private void stopActiveROSpecs() throws ReaderException
    {        
        GET_ROSPECS_RESPONSE response;
        log("Resetting Reader.");
        GET_ROSPECS roSpecs = new GET_ROSPECS();
        try
        {
            response = (GET_ROSPECS_RESPONSE) LLRP_SendReceive(roSpecs);
            List<ROSpec> roSpecList = response.getROSpecList();
            log("ORPHAN ROSpec List " + roSpecList.size());
            for(ROSpec rSpec : roSpecList)
            {
                log("ORPHAN ROSpec : " + rSpec);
                if(rSpec.getCurrentState().intValue() ==  ROSpecState.Active)
                {
                    rSpec.setCurrentState(new ROSpecState(ROSpecState.Disabled));
                    stopROSpec(rSpec.getROSpecID());
                }
                if(rSpec.getROBoundarySpec().getROSpecStartTrigger().getROSpecStartTriggerType().intValue() == ROSpecStartTriggerType.Periodic)
                {
                    rSpec.setCurrentState(new ROSpecState(ROSpecState.Disabled));
                    disableROSpec(rSpec.getROSpecID().intValue());
                }
            }
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }
    }
    /**
     * Deleting all ROSpecs from the reader
     * @throws ReaderException
     */
    public void deleteROSpecs() throws ReaderException
    {
        DELETE_ROSPEC_RESPONSE response;

        log("Deleting all ROSpecs.");
        DELETE_ROSPEC del = new DELETE_ROSPEC();
        // Use zero as the ROSpec ID. This means delete all ROSpecs.
        del.setROSpecID(new UnsignedInteger(0));
        try
        {
            response = (DELETE_ROSPEC_RESPONSE) LLRP_SendReceive(del, STOP_TIMEOUT + commandTimeout + transportTimeout); 
            processData = true;
        } 
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }        
    }

    /**
     * Delete all AccessSpecs from the reader
     */
    private void deleteAccessSpecs() throws ReaderException
    {
        DELETE_ACCESSSPEC_RESPONSE response;

        log("Deleting all AccessSpecs.");
        DELETE_ACCESSSPEC delAcessSpec = new DELETE_ACCESSSPEC();
        // Use zero as the ROSpec ID, This means delete all AccessSpecs.
        delAcessSpec.setAccessSpecID(new UnsignedInteger(0));
        response = (DELETE_ACCESSSPEC_RESPONSE) LLRP_SendReceive(delAcessSpec);
    }

    /**
     * Add the ROSpec to the reader.
     * @throws ReaderException
     */
    private boolean addROSpec(ROSpec roSpec) throws ReaderException
    {
        ADD_ROSPEC_RESPONSE response;
        
        log("Adding the ROSpec : " + roSpec.getROSpecID());
        try
        {
            ADD_ROSPEC roSpecMsg = new ADD_ROSPEC();
            roSpecMsg.setROSpec(roSpec);
            response = (ADD_ROSPEC_RESPONSE) LLRP_SendReceive(roSpecMsg);            

            // Check  successfully added the ROSpec.
            return getStatusFromStatusCode(response.getLLRPStatus());
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }        
    }

    int numPlans = 0;

    public int getNumPlans()
    {
        return numPlans;
    }

    public void setNumPlans(int numPlans)
    {
        this.numPlans = numPlans;
    }
    /**
     * Building an ROSpec with proper AISpec, BoundarySpec, Inventory Specs
     */
    private void buildROSpec(ReadPlan readPlan, long readDuration, List<ROSpec> roSpecList) throws ReaderException
    {
        // Build RO Spec based on the read plan        
        log("Building the ROSpec based on Read Plan");
        int aiSpecIterations;
        UnsignedShortArray antennaIDs = new UnsignedShortArray();
        if(readPlan instanceof MultiReadPlan)
        {
            //roSpecList = new ArrayList<ROSpec>();
            MultiReadPlan mrp = (MultiReadPlan) readPlan;
            /**
             * Added this validation to limit number of read plans to 5 if build is <5.3.2.97
             * Fixed this limitation issue in build 5.3.2.97
             **/
            if ((!(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_INVENTORYSPEC_ID))) &&
                (mrp.plans.length > maxSubPlanCount))
            {
                throw new UnsupportedOperationException("Unsupported operation.");
            }

            setNumPlans(mrp.plans.length);
            //handling backward compatibility case for per antenna on time feature
            if(!featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
            {
                for (ReadPlan rp : mrp.plans)
                {
                    long subtimeout = (0 != mrp.totalWeight) ? ((int) readDuration * rp.weight / mrp.totalWeight)
                            : (readDuration / mrp.plans.length);
                    subtimeout = Math.min(subtimeout, Integer.MAX_VALUE);
                    buildROSpec(rp, subtimeout, roSpecList);
                }
                return;
            }
        }

        // Create a Reader Operation Spec (ROSpec).
        ROSpec roSpec = new ROSpec();
        roSpec.setPriority(new UnsignedByte(0));
        roSpec.setROSpecID(new UnsignedInteger(++roSpecId));
        roSpec.setCurrentState(new ROSpecState(ROSpecState.Disabled));

        log("Started building ROSpec : " + roSpecId);
        // Set up the ROBoundarySpec
        // This defines the start and stop triggers.
        ROBoundarySpec roBoundarySpec = new ROBoundarySpec();

        /**
         * ROSpec start trigger is set to null in all cases.
         * Because sending complete multi read plan information in multiple AISpecs in single ROSPec
         * No need to periodically execute ROSpec incase of multi read async
         **/
        ROSpecStartTrigger startTrig = new ROSpecStartTrigger();
        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
        {
            startTrig.setROSpecStartTriggerType(new ROSpecStartTriggerType(ROSpecStartTriggerType.Null));
        }
        else
        {
            // backward compatibility case for per antenna on time feature.
            if(continuousReading && getNumPlans()>1)
            {
                startTrig.setROSpecStartTriggerType(new ROSpecStartTriggerType(ROSpecStartTriggerType.Periodic));
                PeriodicTriggerValue triggerValue = new PeriodicTriggerValue();
                int asyncOnTime = (Integer)paramGet(TMR_PARAM_READ_ASYNCONTIME);
                triggerValue.setPeriod(new UnsignedInteger(asyncOnTime));
                triggerValue.setOffset(new UnsignedInteger(0));
                startTrig.setPeriodicTriggerValue(triggerValue);
            }
            else
            {
                if(!continuousReading && (readPlan instanceof StopTriggerReadPlan))
                {
                    /* Currently only supported for sync read case */
                    StopTriggerReadPlan strp = (StopTriggerReadPlan) readPlan;
                    if(strp.stopOnCount instanceof StopOnTagCount)
                    {
                        StopOnTagCount sotc=(StopOnTagCount) strp.stopOnCount;
                        isStopNTags = true;            
                        numberOfTagsToRead = sotc.N;  
                        if(isStopNTags && (numberOfTagsToRead == 0))
                        {
                            throw new IllegalArgumentException("Invalid number of tag count found");
                        }
                    }      
                }
                startTrig.setROSpecStartTriggerType(new ROSpecStartTriggerType(ROSpecStartTriggerType.Null));
            }
        }
        /* Set start trigger to BoundarySpec */
        roBoundarySpec.setROSpecStartTrigger(startTrig);

        // Set the ROSpecStopTrigger
        ROSpecStopTrigger stopTrig = new ROSpecStopTrigger();
        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
        {
            if(!continuousReading && getNumPlans()>1)
            {
                /**
                 * In case of non continuous reading and if multiple read plans exist,
                 * then ROSpec's stop trigger is set to
                 * duration trigger with the duration set to total read duration.
                 **/
                stopTrig.setROSpecStopTriggerType(new ROSpecStopTriggerType(ROSpecStopTriggerType.Duration));
                stopTrig.setDurationTriggerValue(new UnsignedInteger(readDuration));
            }
            else
            {
                /**
                 * ROSpec stop trigger is set to null in all cases.
                 **/
                stopTrig.setROSpecStopTriggerType(new ROSpecStopTriggerType(ROSpecStopTriggerType.Null));
                stopTrig.setDurationTriggerValue(new UnsignedInteger(0));
            }
        }
        else
        {
            stopTrig.setROSpecStopTriggerType(new ROSpecStopTriggerType(ROSpecStopTriggerType.Null));
            stopTrig.setDurationTriggerValue(new UnsignedInteger(0));
        }

        /* Set stop triggers to BoundarySpec */
        roBoundarySpec.setROSpecStopTrigger(stopTrig);

        /* Set ROBoundarySpec to ROSpec  */
        roSpec.setROBoundarySpec(roBoundarySpec);

        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
        {
            // Add an Antenna Inventory Spec (AISpec).
            if(getNumPlans() > 1)
            {
                // If Multi readplan is set, AISpecIterations should be equal to number of simple read plans.
                aiSpecIterations = getNumPlans();
            }
            else
            {
                // If simple read plan, AISpecIterations should be 1.
                aiSpecIterations = 1;
            }
        }
        else
        {
            aiSpecIterations = 1;
        }

        for (int planCount = 0; planCount < aiSpecIterations; planCount++) 
        {
            AISpec aispec = new AISpec();

            /* Initialize AISpec stop trigger  */
            AISpecStopTrigger aiStopTrigger = new AISpecStopTrigger();

            if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
            {
                if((!continuousReading) && (aiSpecIterations == 1))
                {
                    /**
                     * In sync read and for 
                     * AISpec stop trigger should be duration based.
                     **/
                    /* Currently only supported for sync read case */
                    if(readPlan instanceof StopTriggerReadPlan)
                    {
                        StopTriggerReadPlan strp = (StopTriggerReadPlan) readPlan;
                        if(strp.stopOnCount instanceof StopOnTagCount)
                        {
                            StopOnTagCount sotc=(StopOnTagCount) strp.stopOnCount;
                            isStopNTags = true;
                            numberOfTagsToRead = sotc.N;
                            if(isStopNTags && (numberOfTagsToRead == 0))
                            {
                                throw new IllegalArgumentException("Invalid number of tag count found");
                            }
                        }
                    }
                    else
                    {
                        isStopNTags = false;
                    }
                    if(isStopNTags)
                    {
                        TagObservationTrigger tagObsTrigger = new TagObservationTrigger();
                        // setting trigger type as Upon_Seeing_N_Tags_Or_Timeout (corresponding enum value is 0)
                        TagObservationTriggerType triggerType = new TagObservationTriggerType();
                        triggerType.set(0);
                        tagObsTrigger.setTriggerType(triggerType);
                        tagObsTrigger.setNumberOfTags(new UnsignedShort(numberOfTagsToRead));
                        tagObsTrigger.setT(new UnsignedShort(0));
                        tagObsTrigger.setTimeout(new UnsignedInteger(readDuration));
                        tagObsTrigger.setNumberOfAttempts(new UnsignedShort(0));

                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Tag_Observation));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(0));
                        aiStopTrigger.setTagObservationTrigger(tagObsTrigger);
                    }
                    else
                    {
                        // SYNC Mode - Set the AI stop trigger to inputted duration. AI spec will run for particular duration
                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Duration));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(readDuration));
                    }
                }
                else
                {
                    /**
                     * In all other cases, i.e., for both sync and async read
                     * AISpec stop trigger should be NULL.
                     **/
                    aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Null));
                    aiStopTrigger.setDurationTrigger(new UnsignedInteger(0));
                }
            }
            else
            {
                if(continuousReading)
                {
                    if(getNumPlans() > 1)
                    {
                        // ASYNC Mode - Set the AI stop trigger to Duration - AsyncOnTime. AI spec will run until the Disable ROSpec is sent.
                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Duration));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(readDuration));
                    }
                    else
                    {
                        // ASYNC Mode - Set the AI stop trigger to null. AI spec will run until the ROSpec stops.
                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Null));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(0));
                    }
                }
                else
                {
                    /**
                    * In all other cases, i.e., for both sync and async read
                    * AISpec stop trigger should be duration based.
                    **/
                    if(isStopNTags)
                    {
                        TagObservationTrigger tagObsTrigger = new TagObservationTrigger();
                        // setting trigger type as Upon_Seeing_N_Tags_Or_Timeout (corresponding enum value is 0)
                        TagObservationTriggerType triggerType = new TagObservationTriggerType();
                        triggerType.set(0);
                        tagObsTrigger.setTriggerType(triggerType);
                        tagObsTrigger.setNumberOfTags(new UnsignedShort(numberOfTagsToRead));
                        tagObsTrigger.setT(new UnsignedShort(0));
                        tagObsTrigger.setTimeout(new UnsignedInteger(readDuration));
                        tagObsTrigger.setNumberOfAttempts(new UnsignedShort(0));

                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Tag_Observation));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(0));
                        aiStopTrigger.setTagObservationTrigger(tagObsTrigger);
                    }
                    else
                    {
                        // SYNC Mode - Set the AI stop trigger to inputted duration. AI spec will run for particular duration
                        aiStopTrigger.setAISpecStopTriggerType(new AISpecStopTriggerType(AISpecStopTriggerType.Duration));
                        aiStopTrigger.setDurationTrigger(new UnsignedInteger(readDuration));
                    }
                }
            }

            /* Set AISpec stop trigger */
            aispec.setAISpecStopTrigger(aiStopTrigger);

            // Select which antenna ports we want to use based on the read plan settings and set antenna ids to aispec
            boolean isFastSearch = false;
            int[] antennaList = new int[]{};
            TagFilter tagFilter = null;
            TagProtocol protocol = TagProtocol.NONE;
            TagOp tagOperation = null;
            CustomAntConfig customAntConfig = null;
            boolean perAntFastSearch = false;
            if(readPlan instanceof SimpleReadPlan)
            {
                SimpleReadPlan srp = (SimpleReadPlan)readPlan;
                isFastSearch = srp.useFastSearch;
                antennaList = srp.antennas;
                validateProtocol(srp.protocol);
                protocol = srp.protocol;
                if(!standalone)
                {
                    //Add rospec id and protocol in the hashtable. So that it can be used to populate the tagdata's protocol member with the read tag protocol.
                    mapRoSpecIdToProtocol.put(roSpecId, srp.protocol);
                }
                tagFilter = srp.filter;
                tagOperation = srp.Op;
                if(srp.customAntConfig != null)
                {
                    customAntConfig = srp.customAntConfig;
                    perAntFastSearch = customAntConfig.perAntFastSearch;
                }
                }
            else if(readPlan instanceof MultiReadPlan)
            {
                MultiReadPlan mrp = (MultiReadPlan)readPlan;
                ReadPlan rplans[] = mrp.plans;
                if(rplans[planCount] instanceof SimpleReadPlan)
                {
                    SimpleReadPlan srp = (SimpleReadPlan)rplans[planCount];
                    isFastSearch = srp.useFastSearch;
                    antennaList = srp.antennas;
                    validateProtocol(srp.protocol);
                    protocol = srp.protocol;
                    tagFilter = srp.filter;
                    tagOperation = srp.Op;
                    if(srp.customAntConfig != null)
                    {
                        customAntConfig = srp.customAntConfig;
                        perAntFastSearch = customAntConfig.perAntFastSearch;
                    }
                }
            }

            // Select which antenna ports we want to use.
            // Setting this property to 0 means all antenna ports.
            antennaIDs = new UnsignedShortArray(antennaList.length);

            if(antennaList.length == 0)
            {
                antennaIDs.add(new UnsignedShort(0));
            }
            else
            {
                for(int i=0;i<antennaList.length;i++)
                {
                    antennaIDs.set(i,new UnsignedShort(antennaList[i]));
                }                
            }
            aispec.setAntennaIDs(antennaIDs);

            // Reading Gen2 Tags, specify in InventorySpec
            InventoryParameterSpec inventoryParam = new InventoryParameterSpec();
            //Session, target & select per antenna
            if(customAntConfig != null)
            {
                for(int i=0 ; i < customAntConfig.antCount; i++)
                {
                    C1G2Filter filter = new C1G2Filter();
                    C1G2TruncateAction truncateAction = new C1G2TruncateAction();
                    truncateAction.set(C1G2TruncateAction.Do_Not_Truncate);
                    filter.setT(truncateAction);

                    C1G2TagInventoryMask mask;
                    AntennaConfiguration customAntConf = new AntennaConfiguration();
                    customAntConf.setAntennaID(new UnsignedShort((Integer) customAntConfig.customConfigPerAnt[i].antID));
                    C1G2InventoryCommand inventoryCommand = new C1G2InventoryCommand();
                    TagFilter customTagFilter = customAntConfig.customConfigPerAnt[i].filter;
                    if (customTagFilter instanceof Gen2.Select)
                    {
                        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT))
                        {
                            /* Set TagInventoryStateAwareAction to true */
                            inventoryCommand.setTagInventoryStateAware(new Bit(1));
                        }
                        else
                        {
                            /* Set TagInventoryStateAwareAction to false */
                            inventoryCommand.setTagInventoryStateAware(new Bit(0));
                        }
                        Gen2.Select selectFilter = (Gen2.Select)customTagFilter;
                        mask = new C1G2TagInventoryMask();

                        // Memory Bank
                        if((selectFilter.bank.rep) > 3)
                        {
                            throw new IllegalArgumentException("Invalid argument");
                        }
                        TwoBitField memBank = new TwoBitField(String.valueOf(selectFilter.bank.rep));
                        mask.setMB(memBank);

                        // Validate bitLength and mask.length. Always ensure, bitlength should be less than or equal to mask.length
                        if(selectFilter.bitLength > ((selectFilter.mask.length)*8))
                        {
                            throw new IllegalArgumentException("Bitlength can't be greater than mask.length");
                        }
                        else
                        {
                            // LLRP Spec doesn't support filter operation, if bitLength is not a multiple of 8.
                            if((selectFilter.bitLength % 8) != 0)
                            {
                                throw new IllegalArgumentException("Can't parse bitLength "+ selectFilter.bitLength + " in multiples of 8."
                                        + "Please provide bitLength in multiples of 8.");
                            }
                            int length = (selectFilter.bitLength / 8);
                            byte[] tempMask = new byte[length];
                            System.arraycopy(selectFilter.mask, 0, tempMask, 0, length);
                            BitArray_HEX tagMask = new BitArray_HEX(ReaderUtil.byteArrayToHexString(tempMask));
                            mask.setTagMask(tagMask);
                        }
                        mask.setPointer(new UnsignedShort(selectFilter.bitPointer));
                        filter.setC1G2TagInventoryMask(mask);

                        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT))
                        {
                            /* Set TagInventory StateAware Action */
                            C1G2TagInventoryStateAwareFilterAction awareAction = new C1G2TagInventoryStateAwareFilterAction();

                            switch ((selectFilter.target.value))
                            {
                                case 0:
                                    awareAction.setTarget(new C1G2StateAwareTarget(1));
                                    break;
                                case 1:
                                    awareAction.setTarget(new C1G2StateAwareTarget(2));
                                    break;
                                case 2:
                                    awareAction.setTarget(new C1G2StateAwareTarget(3));
                                    break;
                                case 3:
                                    awareAction.setTarget(new C1G2StateAwareTarget(4));
                                    break;
                                case 4:
                                    awareAction.setTarget(new C1G2StateAwareTarget(0));
                                    break;
                            }
                            //awareAction.setTarget(new C1G2StateAwareTarget(selectFilter.target.value));
                            awareAction.setAction(new C1G2StateAwareAction(selectFilter.action.value));
                            filter.setC1G2TagInventoryStateAwareFilterAction(awareAction);
                        }
                        else
                        {
                            C1G2TagInventoryStateUnawareFilterAction unAwareAction = new C1G2TagInventoryStateUnawareFilterAction();
                            unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Select_Unselect));

                            if (selectFilter.invert)
                            {
                                unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Unselect_Select));
                            }
                            filter.setC1G2TagInventoryStateUnawareFilterAction(unAwareAction);
                        }
                    }
                    else if (customTagFilter instanceof TagData)
                    {
                        /* Set TagInventoryStateAwareAction to false */
                        inventoryCommand.setTagInventoryStateAware(new Bit(0));

                        TagData tagDataFilter = (TagData)customTagFilter;
                        mask = new C1G2TagInventoryMask();

                        // EPC Memory Bank
                        TwoBitField memBank = new TwoBitField();
                        memBank.clear(new Integer(0));
                        memBank.set(new Integer(1));
                        mask.setMB(memBank);

                        BitArray_HEX tagMask = new BitArray_HEX(tagDataFilter.epcString());
                        mask.setTagMask(tagMask);
                        mask.setPointer(new UnsignedShort(32));
                        filter.setC1G2TagInventoryMask(mask);

                        C1G2TagInventoryStateUnawareFilterAction unAwareAction = new C1G2TagInventoryStateUnawareFilterAction();
                        unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Select_Unselect));
                        filter.setC1G2TagInventoryStateUnawareFilterAction(unAwareAction);
                    }
                    else
                    {
                        throw new UnsupportedOperationException("Invalid select type");
                    }
                    C1G2SingulationControl c1G2SingulationControl = new C1G2SingulationControl();;
                    ThingMagicTargetStrategy tmTargetStrategy = new ThingMagicTargetStrategy();
                    if(customAntConfig.customConfigPerAnt[i].session != null)
                    {
                        Gen2.Session sessionValue = customAntConfig.customConfigPerAnt[i].session;
                        switch(sessionValue)
                        {
                            case S0:
                                c1G2SingulationControl.setSession(new TwoBitField(Integer.toString(0)));
                                break;
                            case S1:
                                c1G2SingulationControl.setSession(new TwoBitField(Integer.toString(1)));
                                break;
                            case S2:
                                c1G2SingulationControl.setSession(new TwoBitField(Integer.toString(2)));
                                break;
                            case S3:
                                c1G2SingulationControl.setSession(new TwoBitField(Integer.toString(3)));
                                break;
                            default:
                                throw new IllegalArgumentException("Invalid session");
                        }
                    }
                    c1G2SingulationControl.setTagPopulation(new UnsignedShort(0));
                    c1G2SingulationControl.setTagTransitTime(new UnsignedInteger(0));
                    if(customAntConfig.customConfigPerAnt[i].target != null)
                    {
                        tmTargetStrategy.setThingMagicTargetStrategyValue(new ThingMagicC1G2TargetStrategy(getTargetValue(customAntConfig.customConfigPerAnt[i].target)));
                    }
                    inventoryCommand.setC1G2SingulationControl(c1G2SingulationControl);
                    inventoryCommand.addToCustomList(tmTargetStrategy);
                    if(perAntFastSearch)
                    {
                        ThingMagicFastSearchMode fastSearch = new ThingMagicFastSearchMode();
                        ThingMagicFastSearchValue fastSearchValue = new ThingMagicFastSearchValue(ThingMagicFastSearchValue.Enabled);
                        fastSearch.setThingMagicFastSearch(fastSearchValue);
                        inventoryCommand.addToCustomList(fastSearch);
                    }
                    inventoryCommand.addToC1G2FilterList(filter);
                    customAntConf.addToAirProtocolInventoryCommandSettingsList(inventoryCommand);
                    inventoryParam.addToAntennaConfigurationList(customAntConf);
                }
                ThingMagicCustomAntennaSwitching tmCustomAntSwitch = new ThingMagicCustomAntennaSwitching();
                if(customAntConfig.antSwitchingType == 1)
                {
                    tmCustomAntSwitch.setAntSwitchingType(new ThingMagicCustomAntennaSwitchingType(ThingMagicCustomAntennaSwitchingType.Dynamic));
                }
                else
                {
                    tmCustomAntSwitch.setAntSwitchingType(new ThingMagicCustomAntennaSwitchingType(ThingMagicCustomAntennaSwitchingType.Equal));
                }
                tmCustomAntSwitch.setTimeout(new UnsignedInteger(customAntConfig.tagReadTimeout));
                inventoryParam.addToCustomList(tmCustomAntSwitch);
            }
            else if(tagFilter != null)
            {
                AntennaConfiguration antConfig = new AntennaConfiguration();
                if(protocol == TagProtocol.GEN2)
                {
                    if(tagFilter instanceof MultiFilter && featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT))
                    {
                        MultiFilter list = (MultiFilter)tagFilter;
                        TagFilter[] multiFilters = list.filters;

                        C1G2InventoryCommand inventoryCmd = new C1G2InventoryCommand();
                        /* Set TagInventoryStateAwareAction to true */
                        inventoryCmd.setTagInventoryStateAware(new Bit(1));
                        /* Set antenna id to antConfig (all antennas) */
                        antConfig.setAntennaID(new UnsignedShort(0));

                            for(int i=0; i < multiFilters.length ; i++)
                            {
                                C1G2Filter filter = new C1G2Filter();
                                C1G2TruncateAction truncateAction = new C1G2TruncateAction();
                                truncateAction.set(C1G2TruncateAction.Do_Not_Truncate);
                                filter.setT(truncateAction);
                                C1G2TagInventoryMask mask;
                                /**
                                 * If Gen2 protocol select filter
                                 **/
                                if(multiFilters[i] instanceof Gen2.Select)
                                {
                                    Gen2.Select selectFilter = (Gen2.Select)multiFilters[i];

                                    mask = new C1G2TagInventoryMask();
                                    // Memory Bank
                                    if((selectFilter.bank.rep) > 3)
                                    {
                                      throw new IllegalArgumentException("Invalid argument");
                                    }
                                    TwoBitField memBank = new TwoBitField(String.valueOf(selectFilter.bank.rep));
                                    mask.setMB(memBank);

                                    // Validate bitLength and mask.length. Always ensure, bitlength should be less than or equal to mask.length
                                    if(selectFilter.bitLength > ((selectFilter.mask.length)*8))
                                    {
                                        throw new IllegalArgumentException("Bitlength can't be greater than mask.length");
                                    }
                                    else
                                    {
                                        // LLRP Spec doesn't support filter operation, if bitLength is not a multiple of 8.
                                        if((selectFilter.bitLength % 8) != 0)
                                        {
                                            throw new IllegalArgumentException("Can't parse bitLength "+ selectFilter.bitLength + " in multiples of 8."
                                                    + "Please provide bitLength in multiples of 8.");
                                        }
                                        int length = (selectFilter.bitLength / 8);
                                        byte[] tempMask = new byte[length];
                                        System.arraycopy(selectFilter.mask, 0, tempMask, 0, length);
                                        BitArray_HEX tagMask = new BitArray_HEX(ReaderUtil.byteArrayToHexString(tempMask));
                                        mask.setTagMask(tagMask);
                                    }
                                    mask.setPointer(new UnsignedShort(selectFilter.bitPointer));
                                    filter.setC1G2TagInventoryMask(mask);

                                    /* Set TagInventory StateAware Action */
                                    C1G2TagInventoryStateAwareFilterAction awareAction = new C1G2TagInventoryStateAwareFilterAction();
                                    if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_STATE_AWARE))
                                    {
                                        switch ((selectFilter.target.value))
                                        {
                                            case 0:
                                                awareAction.setTarget(new C1G2StateAwareTarget(1));
                                                break;
                                            case 1:
                                                awareAction.setTarget(new C1G2StateAwareTarget(2));
                                                break;
                                            case 2:
                                                awareAction.setTarget(new C1G2StateAwareTarget(3));
                                                break;
                                            case 3:
                                                awareAction.setTarget(new C1G2StateAwareTarget(4));
                                                break;
                                            case 4:
                                                awareAction.setTarget(new C1G2StateAwareTarget(0));
                                                break;
                                        }
                                        //awareAction.setTarget(new C1G2StateAwareTarget(selectFilter.target.value));
                                        awareAction.setAction(new C1G2StateAwareAction(selectFilter.action.value));
                                        filter.setC1G2TagInventoryStateAwareFilterAction(awareAction);
                                    }
                                    else
                                    {
                                        awareAction.setTarget(new C1G2StateAwareTarget(selectFilter.target.value));
                                        awareAction.setAction(new C1G2StateAwareAction(selectFilter.action.value));
                                        filter.setC1G2TagInventoryStateAwareFilterAction(awareAction);
                                    }
                                }
                                else
                                {
                                    throw new UnsupportedOperationException("Unsupported operation.");
                                }
                                inventoryCmd.addToC1G2FilterList(filter);
                            }
                            antConfig.addToAirProtocolInventoryCommandSettingsList(inventoryCmd);
                            inventoryParam.addToAntennaConfigurationList(antConfig);
                    }
                    else if(tagFilter instanceof MultiFilter && !(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT)))
                    {
                        throw new UnsupportedOperationException("Operation not supported.");
                    }
                    else
                    {
                        C1G2Filter filter = new C1G2Filter();
                        C1G2TruncateAction truncateAction = new C1G2TruncateAction();
                        truncateAction.set(C1G2TruncateAction.Do_Not_Truncate);
                        filter.setT(truncateAction);

                        C1G2TagInventoryMask mask;
                        antConfig.setAntennaID(new UnsignedShort(0));

                        C1G2InventoryCommand inventoryCommand = new C1G2InventoryCommand();

                        if (tagFilter instanceof Gen2.Select)
                        {
                            if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT))
                            {
                                /* Set TagInventoryStateAwareAction to true */
                                inventoryCommand.setTagInventoryStateAware(new Bit(1));
                            }
                            else
                            {
                                /* Set TagInventoryStateAwareAction to false */
                                inventoryCommand.setTagInventoryStateAware(new Bit(0));
                            }
                            Gen2.Select selectFilter = (Gen2.Select)tagFilter;
                            mask = new C1G2TagInventoryMask();

                            // Memory Bank
                            if((selectFilter.bank.rep) > 3)
                            {
                                throw new IllegalArgumentException("Invalid argument");
                            }
                            TwoBitField memBank = new TwoBitField(String.valueOf(selectFilter.bank.rep));
                            mask.setMB(memBank);

                            // Validate bitLength and mask.length. Always ensure, bitlength should be less than or equal to mask.length
                            if(selectFilter.bitLength > ((selectFilter.mask.length)*8))
                            {
                                throw new IllegalArgumentException("Bitlength can't be greater than mask.length");
                            }
                            else
                            {
                                // LLRP Spec doesn't support filter operation, if bitLength is not a multiple of 8.
                                if((selectFilter.bitLength % 8) != 0)
                                {
                                    throw new IllegalArgumentException("Can't parse bitLength "+ selectFilter.bitLength + " in multiples of 8."
                                            + "Please provide bitLength in multiples of 8.");
                                }
                                int length = (selectFilter.bitLength / 8);
                                byte[] tempMask = new byte[length];
                                System.arraycopy(selectFilter.mask, 0, tempMask, 0, length);
                                BitArray_HEX tagMask = new BitArray_HEX(ReaderUtil.byteArrayToHexString(tempMask));
                                mask.setTagMask(tagMask);
                            }
                            mask.setPointer(new UnsignedShort(selectFilter.bitPointer));
                            filter.setC1G2TagInventoryMask(mask);

                            if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_MULTI_SELECT))
                            {
                                C1G2TagInventoryStateAwareFilterAction awareAction = new C1G2TagInventoryStateAwareFilterAction();
                                /* Set TagInventory StateAware Action */
                                if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_STATE_AWARE))
                                {

                                    switch ((selectFilter.target.value))
                                    {
                                        case 0:
                                            awareAction.setTarget(new C1G2StateAwareTarget(1));
                                            break;
                                        case 1:
                                            awareAction.setTarget(new C1G2StateAwareTarget(2));
                                            break;
                                        case 2:
                                            awareAction.setTarget(new C1G2StateAwareTarget(3));
                                            break;
                                        case 3:
                                            awareAction.setTarget(new C1G2StateAwareTarget(4));
                                            break;
                                        case 4:
                                            awareAction.setTarget(new C1G2StateAwareTarget(0));
                                            break;
                                    }
                                    //awareAction.setTarget(new C1G2StateAwareTarget(selectFilter.target.value));
                                    awareAction.setAction(new C1G2StateAwareAction(selectFilter.action.value));
                                    filter.setC1G2TagInventoryStateAwareFilterAction(awareAction);
                                }
                                else
                                {
                                    awareAction.setTarget(new C1G2StateAwareTarget(selectFilter.target.value));
                                    awareAction.setAction(new C1G2StateAwareAction(selectFilter.action.value));
                                    filter.setC1G2TagInventoryStateAwareFilterAction(awareAction);
                                }
                            }
                            else
                            {
                                C1G2TagInventoryStateUnawareFilterAction unAwareAction = new C1G2TagInventoryStateUnawareFilterAction();
                                unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Select_Unselect));

                                if (selectFilter.invert)
                                {
                                    unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Unselect_Select));
                                }
                                filter.setC1G2TagInventoryStateUnawareFilterAction(unAwareAction);
                            }
                        }
                        else if (tagFilter instanceof TagData)
                        {
                            /* Set TagInventoryStateAwareAction to false */
                            inventoryCommand.setTagInventoryStateAware(new Bit(0));

                            TagData tagDataFilter = (TagData)tagFilter;
                            mask = new C1G2TagInventoryMask();

                            // EPC Memory Bank
                            TwoBitField memBank = new TwoBitField();
                            memBank.clear(new Integer(0));
                            memBank.set(new Integer(1));
                            mask.setMB(memBank);

                            BitArray_HEX tagMask = new BitArray_HEX(tagDataFilter.epcString());
                            mask.setTagMask(tagMask);
                            mask.setPointer(new UnsignedShort(32));
                            filter.setC1G2TagInventoryMask(mask);

                            C1G2TagInventoryStateUnawareFilterAction unAwareAction = new C1G2TagInventoryStateUnawareFilterAction();
                            unAwareAction.setAction(new C1G2StateUnawareAction(C1G2StateUnawareAction.Select_Unselect));
                            filter.setC1G2TagInventoryStateUnawareFilterAction(unAwareAction);
                        }
                        else
                        {
                            throw new UnsupportedOperationException("Invalid select type");
                        }

                        inventoryCommand.addToC1G2FilterList(filter);
                        antConfig.addToAirProtocolInventoryCommandSettingsList(inventoryCommand);
                        inventoryParam.addToAntennaConfigurationList(antConfig);
                    }
                }//end of Gen2 filter
                else if(protocol == TagProtocol.ISO180006B) //start of ISO180006b filter
                {
                    ThingMagicISO180006BInventoryCommand inventory = new ThingMagicISO180006BInventoryCommand();
                    ThingMagicISO180006BTagPattern tagPattern = new ThingMagicISO180006BTagPattern();
                    ThingMagicISO180006BFilterType filterType;
                    if (tagFilter instanceof Iso180006b.Select)
                    {
                        Iso180006b.Select selectFilter = (Iso180006b.Select) tagFilter;

                        //Set invert
                        Bit invert = new Bit(selectFilter.invert);
                        tagPattern.setInvert(invert);

                        //Set Mask
                        tagPattern.setMask(new UnsignedByte(selectFilter.mask));

                        //Set SelectOp
                        TwoBitField data = new TwoBitField(String.valueOf(selectFilter.op.rep));
                        tagPattern.setSelectOp(data);

                        //Set Address
                        tagPattern.setAddress(new UnsignedByte(selectFilter.address));

                        //Set TagData
                        tagPattern.setTagData(new UnsignedByteArray_HEX(selectFilter.data));

                        filterType = new ThingMagicISO180006BFilterType(ThingMagicISO180006BFilterType.ISO180006BSelect);
                    }
                    else if (tagFilter instanceof TagData)
                    {
                        TagData filter = (TagData) tagFilter;

                        //Set Address
                        tagPattern.setAddress(new UnsignedByte(0));

                        /*
                         * Mercury API doesn't expect invert flag and SelectOp field  from TagData filter
                         * so defaulting 0;
                         */
                        tagPattern.setInvert(new Bit(0));

                        //Set SelectOp
                        tagPattern.setSelectOp(new TwoBitField(String.valueOf(0)));

                        // Convert the byte count to a MSB-based bit mask
                        int mask = (0xff00 >> filter.epc.length) & 0xff;
                        tagPattern.setMask(new UnsignedByte(mask));

                        //Set TagData
                        tagPattern.setTagData(new UnsignedByteArray_HEX(filter.epc));

                        filterType = new ThingMagicISO180006BFilterType(ThingMagicISO180006BFilterType.ISO180006BTagData);
                    }
                    else
                    {
                        throw new UnsupportedOperationException("Invalid select type");
                    }
                    tagPattern.setFilterType(filterType);
                    inventory.setThingMagicISO180006BTagPattern(tagPattern);
                    antConfig.addToAirProtocolInventoryCommandSettingsList(inventory);
                    antConfig.setAntennaID(new UnsignedShort(0));
                    inventoryParam.addToAntennaConfigurationList(antConfig);
                 }//end of ISO180006b filter
                 else
                 {
                    throw new UnsupportedOperationException("Only GEN2 and ISO18K6B protocol is supported as of now");
                 }
            }//end of tagfilter

            if(isFastSearch)
            {
                List<AntennaConfiguration> antennaConfigList = new ArrayList<AntennaConfiguration>();
                AntennaConfiguration antennaConfiguration = new AntennaConfiguration();
                C1G2InventoryCommand inventoryCommand = new C1G2InventoryCommand();
                ThingMagicFastSearchMode fastSearch = new ThingMagicFastSearchMode();
                ThingMagicFastSearchValue fastSearchValue = new ThingMagicFastSearchValue(ThingMagicFastSearchValue.Enabled);
                fastSearch.setThingMagicFastSearch(fastSearchValue);
                inventoryCommand.addToCustomList(fastSearch);
                inventoryCommand.setTagInventoryStateAware(new Bit(0));
                antennaConfiguration.setAntennaID(new UnsignedShort(0));
                antennaConfiguration.addToAirProtocolInventoryCommandSettingsList(inventoryCommand);
                antennaConfigList.add(antennaConfiguration);
                inventoryParam.addToAntennaConfigurationList(antennaConfiguration);
            }

            invSpecId = planCount + 1;
            if (null != tagOperation)
            {
                // If multi read plan
                if(readPlan instanceof MultiReadPlan)
                {
                    MultiReadPlan mrp = (MultiReadPlan)readPlan;
                    ReadPlan rplans[] = mrp.plans;
                    if(rplans[planCount] instanceof SimpleReadPlan)
                    {
                        SimpleReadPlan srp = (SimpleReadPlan)rplans[planCount];
                        AccessSpec accessSpec = createAccessSpec(srp);
                        addAccessSpec(accessSpec);
                        enableAccessSpec(accessSpec.getAccessSpecID().intValue());
                    }
                }
                else
                {
                    // If simple read plan
                    if(readPlan instanceof SimpleReadPlan)
                    {
                        SimpleReadPlan srp = (SimpleReadPlan)readPlan;
                        AccessSpec accessSpec = createAccessSpec(srp);
                        addAccessSpec(accessSpec);
                        enableAccessSpec(accessSpec.getAccessSpecID().intValue());
                    }
                }
            }
            ThingMagicCustomAirProtocols protocols = new ThingMagicCustomAirProtocols();
            if(protocol == TagProtocol.GEN2)
            {
                inventoryParam.setProtocolID(new AirProtocols(AirProtocols.EPCGlobalClass1Gen2));             
            }
            else if(protocol == TagProtocol.ISO180006B)
            {
                inventoryParam.setProtocolID(new AirProtocols(AirProtocols.Unspecified));
                ThingMagicCustomAirProtocolList pList = new ThingMagicCustomAirProtocolList(ThingMagicCustomAirProtocolList.Iso180006b);
                protocols.setCustomProtocolId(pList);
                inventoryParam.addToCustomList(protocols);
            }
            else if(protocol == TagProtocol.IPX64)
            {
                inventoryParam.setProtocolID(new AirProtocols(AirProtocols.Unspecified));
                ThingMagicCustomAirProtocolList pList = new ThingMagicCustomAirProtocolList(ThingMagicCustomAirProtocolList.IPX64);
                protocols.setCustomProtocolId(pList);
                inventoryParam.addToCustomList(protocols);
            }
            else if(protocol == TagProtocol.IPX256)
            {
                inventoryParam.setProtocolID(new AirProtocols(AirProtocols.Unspecified));
                ThingMagicCustomAirProtocolList pList = new ThingMagicCustomAirProtocolList(ThingMagicCustomAirProtocolList.IPX256);
                protocols.setCustomProtocolId(pList);
                inventoryParam.addToCustomList(protocols);
            }
            else if(protocol == TagProtocol.ATA)
            {
                inventoryParam.setProtocolID(new AirProtocols(AirProtocols.Unspecified));
                ThingMagicCustomAirProtocolList pList = new ThingMagicCustomAirProtocolList(ThingMagicCustomAirProtocolList.Ata);
                protocols.setCustomProtocolId(pList);
                inventoryParam.addToCustomList(protocols);
            }
            /* Set InventoryParameterSpec id */
            inventoryParam.setInventoryParameterSpecID(new UnsignedShort(planCount+1));
            aispec.addToInventoryParameterSpecList(inventoryParam);

            if(getNumPlans() > 1 && featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
            {
                /**
                 * Add readPlan weight option as Custom
                 * parameter to the Inventory parameter
                 **/
                MultiReadPlan mrp = (MultiReadPlan)readPlan;
                ReadPlan rplans[] = mrp.plans;
                if(rplans[planCount] instanceof SimpleReadPlan)
                {
                    SimpleReadPlan srp = (SimpleReadPlan)rplans[planCount];
                    ThingMagicCustomReadplanWeight readPlanWt = new ThingMagicCustomReadplanWeight();
                    readPlanWt.setplanWeight(new UnsignedInteger(srp.weight));
                    readPlanWt.setmultiPlanWeight(new UnsignedInteger(mrp.totalWeight));
                    inventoryParam.addToCustomList(readPlanWt);
                }
            }
            roSpec.addToSpecParameterList(aispec);
        }
        //StatsListener
        if(continuousReading && !(statsListeners == null || statsListeners.isEmpty()))
        {
            RFSurveySpec rFSurveySpec = new RFSurveySpec();
            // In RFSurveyspec, setting antenna id doesnot have any significance w.r.t antennas. Hence setting it to the first element in antennaIDs[]
            rFSurveySpec.setAntennaID((antennaIDs.get(0)));
            rFSurveySpec.setStartFrequency(new UnsignedInteger(frequencyHopTableList.get(0).getFrequency().get(0).toInteger()));
            rFSurveySpec.setEndFrequency(new UnsignedInteger(frequencyHopTableList.get(0).getFrequency().get(frequencyHopTableList.get(0).getFrequency().size()-1).toInteger()));
            RFSurveySpecStopTrigger rFSurveySpecStopTrigger = new RFSurveySpecStopTrigger();
            rFSurveySpecStopTrigger.setStopTriggerType(new RFSurveySpecStopTriggerType(RFSurveySpecStopTriggerType.Null));
            rFSurveySpecStopTrigger.setDurationPeriod(new UnsignedInteger(0));
            rFSurveySpecStopTrigger.setN(new UnsignedInteger(0));
            rFSurveySpec.setRFSurveySpecStopTrigger(rFSurveySpecStopTrigger);
            CustomRFSurveySpec customRFSurveySpec = new CustomRFSurveySpec();
            //ThingMagicCustomStatsEnableFlag tmStatsEnable = new ThingMagicCustomStatsEnableFlag(allStatsFlags);
            customRFSurveySpec.setStatsEnable(new UnsignedShort(allStatsFlags));
            rFSurveySpec.addToCustomList(customRFSurveySpec);
            roSpec.addToSpecParameterList(rFSurveySpec);
        }
        // Specify what type of tag reports we want to receive and when we want to receive them.
        ROReportSpec roReportSpec = new ROReportSpec();

        // Receive a report every time a tag is read.
        if(continuousReading)
        {
            roReportSpec.setROReportTrigger(new ROReportTriggerType(ROReportTriggerType.Upon_N_Tags_Or_End_Of_ROSpec));
            roReportSpec.setN(new UnsignedShort(1));
        }
        else
        {
            roReportSpec.setROReportTrigger(new ROReportTriggerType(ROReportTriggerType.Upon_N_Tags_Or_End_Of_ROSpec));
            roReportSpec.setN(new UnsignedShort(0));
        }

        TagReportContentSelector reportContent = new TagReportContentSelector();
        // Selecting which fields we want in the report.
        reportContent.setEnableAccessSpecID(new Bit(1));
        reportContent.setEnableROSpecID(new Bit(1));
        reportContent.setEnableSpecIndex(new Bit(1));
        reportContent.setEnableInventoryParameterSpecID(new Bit(1));
        
        if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ALL))
        {
            metaDataFlags.clear();
            for(TagReadData.TagMetadataFlag f : TagReadData.TagMetadataFlag.values())
            {
                if(!(f.equals(TagReadData.TagMetadataFlag.NONE) || f.equals(TagReadData.TagMetadataFlag.BRAND_IDENTIFIER) ||
                        f.equals(TagReadData.TagMetadataFlag.TAGTYPE)))
                {
                    metaDataFlags.add(f);
                }
            }
            reportContent.setEnableAntennaID(new Bit(1));
            reportContent.setEnableChannelIndex(new Bit(1));
            reportContent.setEnablePeakRSSI(new Bit(1));
            reportContent.setEnableFirstSeenTimestamp(new Bit(1));
            reportContent.setEnableLastSeenTimestamp(new Bit(1));
            reportContent.setEnableTagSeenCount(new Bit(1));
        }
        else
        {
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ANTENNAID))
            {
                reportContent.setEnableAntennaID(new Bit(1));
            }
            else
            {
                reportContent.setEnableAntennaID(new Bit(0));
            }
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.FREQUENCY))
            {
                reportContent.setEnableChannelIndex(new Bit(1));
            }
            else
            {
                reportContent.setEnableChannelIndex(new Bit(0));
            }
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.RSSI))
            {
                reportContent.setEnablePeakRSSI(new Bit(1));
            }
            else
            {
                reportContent.setEnablePeakRSSI(new Bit(0));
            }
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.TIMESTAMP))
            {
                reportContent.setEnableFirstSeenTimestamp(new Bit(1));
                reportContent.setEnableLastSeenTimestamp(new Bit(1));
            }
            else
            {
                reportContent.setEnableFirstSeenTimestamp(new Bit(0));
                reportContent.setEnableLastSeenTimestamp(new Bit(0));
            }
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.READCOUNT))
            {
                reportContent.setEnableTagSeenCount(new Bit(1));
            }
            else
            {
                reportContent.setEnableTagSeenCount(new Bit(0));
            }
        }
        // By default both PC and CRC bits are set, so sent from tmmpd
        C1G2EPCMemorySelector gen2MemSelector = new C1G2EPCMemorySelector();
        gen2MemSelector.setEnableCRC(new Bit(1));
        gen2MemSelector.setEnablePCBits(new Bit(1));

        reportContent.addToAirProtocolEPCMemorySelectorList(gen2MemSelector);
        roReportSpec.setTagReportContentSelector(reportContent);

        // Since Spruce release firmware doesn't support phase, there won't be ThingMagicTagReportContentSelector
        // custom paramter in ROReportSpec
        StringTokenizer versionSplit = new StringTokenizer(_softwareVersion, ".");
        int length = versionSplit.countTokens();
        if(length != 0)
        {
            int productVersion = Integer.parseInt(versionSplit.nextToken());
            int buildVersion = Integer.parseInt(versionSplit.nextToken());
            if ((productVersion == 4 && buildVersion >= 17) || productVersion > 4)
            {
                ThingMagicTagReportContentSelector reportContentSelector = new ThingMagicTagReportContentSelector();
                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ALL))
                {
                    ThingMagicPhaseMode phaseMode = new ThingMagicPhaseMode(ThingMagicPhaseMode.Enabled);
                    reportContentSelector.setPhaseMode(phaseMode);
                    //roReportSpec.addToCustomList(reportContentSelector);
                    
                    MetadataGPIOMode metadataGPIOMode = new MetadataGPIOMode();
                    ThingMagicMetadataFlagStatus tmFlagStatusGPIOMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                    metadataGPIOMode.setMode(tmFlagStatusGPIOMode);
                    reportContentSelector.setMetadataGPIOMode(metadataGPIOMode);
                    
                    MetadataGen2QMode metadataGen2QMode = new MetadataGen2QMode();
                    ThingMagicMetadataFlagStatus tmFlagStatusGen2QMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                    metadataGen2QMode.setMode(tmFlagStatusGen2QMode);
                    reportContentSelector.setMetadataGen2QMode(metadataGen2QMode);
                    
                    MetadataGen2LFMode metadataGen2LFMode = new MetadataGen2LFMode();
                    ThingMagicMetadataFlagStatus tmFlagStatusGen2LFMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                    metadataGen2LFMode.setMode(tmFlagStatusGen2LFMode);
                    reportContentSelector.setMetadataGen2LFMode(metadataGen2LFMode);
                        
                    MetadataGen2TargetMode metadataGen2TargetMode = new MetadataGen2TargetMode();
                    ThingMagicMetadataFlagStatus tmFlagStatusGen2TargetMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                    metadataGen2TargetMode.setMode(tmFlagStatusGen2TargetMode);
                    reportContentSelector.setMetadataGen2TargetMode(metadataGen2TargetMode);
                    
                    MetadataDataMode metadataDataMode = new MetadataDataMode();
                    ThingMagicMetadataFlagStatus tmFlagStatusDataMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                    metadataDataMode.setMode(tmFlagStatusDataMode);
                    reportContentSelector.setMetadataDataMode(metadataDataMode);
                }
                else
                {
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.PHASE))
                    {
                        ThingMagicPhaseMode phaseMode = new ThingMagicPhaseMode(ThingMagicPhaseMode.Enabled);
                        reportContentSelector.setPhaseMode(phaseMode);
                    }
                    else
                    {
                        ThingMagicPhaseMode phaseMode = new ThingMagicPhaseMode(ThingMagicPhaseMode.Disabled);
                        reportContentSelector.setPhaseMode(phaseMode);
                    }
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GPIO_STATUS))
                    {
                        MetadataGPIOMode metadataGPIOMode = new MetadataGPIOMode();
                        ThingMagicMetadataFlagStatus tmFlagStatusGPIOMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                        metadataGPIOMode.setMode(tmFlagStatusGPIOMode);
                        reportContentSelector.setMetadataGPIOMode(metadataGPIOMode);
                    }
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_Q))
                    {
                        MetadataGen2QMode metadataGen2QMode = new MetadataGen2QMode();
                        ThingMagicMetadataFlagStatus tmFlagStatusGen2QMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                        metadataGen2QMode.setMode(tmFlagStatusGen2QMode);
                        reportContentSelector.setMetadataGen2QMode(metadataGen2QMode);
                    }
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_LF))
                    {
                        MetadataGen2LFMode metadataGen2LFMode = new MetadataGen2LFMode();
                        ThingMagicMetadataFlagStatus tmFlagStatusGen2LFMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                        metadataGen2LFMode.setMode(tmFlagStatusGen2LFMode);
                        reportContentSelector.setMetadataGen2LFMode(metadataGen2LFMode);
                    }
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_TARGET))
                    {
                        MetadataGen2TargetMode metadataGen2TargetMode = new MetadataGen2TargetMode();
                        ThingMagicMetadataFlagStatus tmFlagStatusGen2TargetMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                        metadataGen2TargetMode.setMode(tmFlagStatusGen2TargetMode);
                        reportContentSelector.setMetadataGen2TargetMode(metadataGen2TargetMode);
                    }
                    if(metaDataFlags.contains(TagReadData.TagMetadataFlag.DATA))
                    {
                        MetadataDataMode metadataDataMode = new MetadataDataMode();
                        ThingMagicMetadataFlagStatus tmFlagStatusDataMode = new ThingMagicMetadataFlagStatus(ThingMagicMetadataFlagStatus.Enabled);
                        metadataDataMode.setMode(tmFlagStatusDataMode);
                        reportContentSelector.setMetadataDataMode(metadataDataMode);
                    }
                }
                roReportSpec.addToCustomList(reportContentSelector);
            }
        }
        roSpec.setROReportSpec(roReportSpec);
        roSpecList.add((roSpec));
    }

    private void validateProtocol(TagProtocol protocol)
    {
        if(!(protocolSet.contains(protocol))){
            throw new IllegalArgumentException("Unsupported protocol : " + protocol.toString());
        }
    }
    
    private AccessSpec createAccessSpec(SimpleReadPlan srp) throws ReaderException
    {
        AccessCommand accessCommand = new AccessCommand();
        AccessSpec accessSpec = new AccessSpec();
        if((srp.filter != null) && (srp.Op instanceof Gen2.NxpGen2TagOp.EasAlarm))
        {
            throw new FeatureNotSupportedException("NxpEasAlarm with filter is not supported");
        }
        if(srp.Op instanceof TagOpList)
        {
            // builds tagop list and adds to accessCommandOpSpecList
            buildtagOpListSpec(srp,accessCommand);
        }
        else
        {
            accessCommand.addToAccessCommandOpSpecList(buildOpSpec(srp));
        }
        accessSpec.setAccessSpecID(new UnsignedInteger(++accessSpecId));
        accessSpec.setAccessCommand(accessCommand);
        accessSpec.setROSpecID(new UnsignedInteger(roSpecId));
        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_INVENTORYSPEC_ID))
        {
            ThingMagicCustomInventorySpecID invSpecId = new ThingMagicCustomInventorySpecID();
            invSpecId.setInventorySpecId(new UnsignedInteger(this.invSpecId));
            accessSpec.addToCustomList(invSpecId);
        }
        
        if(standalone)
        {
            /**
             * For standalone tag operation the operation
             * has to be performed on the antenna specified in the
             * /reader/tagop/antenna parameter.
             */
            accessSpec.setAntennaID(new UnsignedShort((Integer) paramGet(TMR_PARAM_TAGOP_ANTENNA)));
        }
        else
        {
            /**
             * In case of embedded tag operation the antenna list is
             * adjusted in ROSpec's AISpec. And here set antenna Id to 0, so
             * that this spec is operational on all antennas mentioned in
             * AISpec.
             */
            accessSpec.setAntennaID(new UnsignedShort(0));
        }

        if (srp.protocol == TagProtocol.GEN2)
        {
        accessSpec.setProtocolID(new AirProtocols(AirProtocols.EPCGlobalClass1Gen2));
        }
        else
        {
            accessSpec.setProtocolID(new AirProtocols(AirProtocols.Unspecified));
        }
        accessSpec.setCurrentState(new AccessSpecState(AccessSpecState.Disabled));

        AccessSpecStopTrigger trigger = new AccessSpecStopTrigger();

        /*
         *If  OperationCountValue  set to 0  this is equivalent to no stop trigger is defined.
         */

        if(standalone)
        {
            trigger.setAccessSpecStopTrigger(new AccessSpecStopTriggerType(AccessSpecStopTriggerType.Operation_Count));
            trigger.setOperationCountValue(new UnsignedShort(1));
        }
        else
        {
        trigger.setAccessSpecStopTrigger(new AccessSpecStopTriggerType(AccessSpecStopTriggerType.Null));
            trigger.setOperationCountValue(new UnsignedShort(0));
        }
        accessSpec.setAccessSpecStopTrigger(trigger);

        C1G2TagSpec tagSpec = getTagSpec();
        // Add the tag spec to the access command.
        accessCommand.setAirProtocolTagSpec(tagSpec);
        return accessSpec;
    }

    private C1G2TagSpec getTagSpec() {
        // Add a list of target tags to the tag spec.
        // Initializing tagSpec as LTK-Java validates these objects though tmmpd
        // does not look at this target tag list
        C1G2TagSpec tagSpec = new C1G2TagSpec();
        C1G2TargetTag targetTag = new C1G2TargetTag();
        targetTag.setMB(new TwoBitField());
        targetTag.setMatch(new Bit());
        targetTag.setPointer(new UnsignedShort());
        targetTag.setTagData(new BitArray_HEX());
        targetTag.setTagMask(new BitArray_HEX());

        List<C1G2TargetTag> targetTagList = new ArrayList<C1G2TargetTag>();
        targetTagList.add(targetTag);
        tagSpec.setC1G2TargetTagList(targetTagList);
        return tagSpec;
    }
    boolean[] endOfROSpecFlags;
    private void enableROSpecFlags(int count)
    {
        endOfROSpecFlags = new boolean[count];
        for (int i = 0; i < count; i++)
        {
            endOfROSpecFlags[i] = false;
        }
    }

    // Enable the ROSpec.
    private boolean enableROSpec(int ROSPEC_ID) throws ReaderException
    {
        ENABLE_ROSPEC_RESPONSE response;

        log("Enabling the ROSpec : " + ROSPEC_ID);
        ENABLE_ROSPEC enable = new ENABLE_ROSPEC();
        enable.setROSpecID(new UnsignedInteger(ROSPEC_ID));
        try
        {
            response = (ENABLE_ROSPEC_RESPONSE) LLRP_SendReceive(enable);
            return getStatusFromStatusCode(response.getLLRPStatus());
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }
    }


    /**
     * Starting an RO Specification
     * @param ROSPEC_ID
     * @throws ReaderException
     */
    private boolean startROSpec(int ROSPEC_ID) throws ReaderException
    {
        START_ROSPEC_RESPONSE response;
        log("Starting the ROSpec : " + ROSPEC_ID);
        START_ROSPEC start = new START_ROSPEC();
        start.setROSpecID(new UnsignedInteger(ROSPEC_ID));
        try
        {
            response = (START_ROSPEC_RESPONSE) LLRP_SendReceive(start);
            return getStatusFromStatusCode(response.getLLRPStatus());
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }        
    }
    
    private AccessCommandOpSpec buildOpSpec(SimpleReadPlan srp) throws ReaderException
    {
        TagOp tagOp = srp.Op;
        if(tagOp instanceof Gen2.ReadData && !(tagOp instanceof Gen2.SecureReadData))
        {
            return buildReadOpSpec(tagOp);
        }
        else if(tagOp instanceof Gen2.WriteData)
        {
            return buildWriteOpSpec((Gen2.WriteData)tagOp);
        }
        else if(tagOp instanceof Gen2.WriteTag)
        {
            return buildWriteOpSpec((Gen2.WriteTag)tagOp);
        }
        else if(tagOp instanceof Gen2.BlockWrite)
        {
            return buildBlockWriteOpSpec((Gen2.BlockWrite)tagOp);
        }
        else if(tagOp instanceof Gen2.BlockErase)
        {
            return buildBlockEraseOpSpec((Gen2.BlockErase)tagOp);
        }
        else if(tagOp instanceof Gen2.BlockPermaLock)
        {
            return buildBlockPermaLockOpSpec((Gen2.BlockPermaLock)tagOp);
        }
        else if(tagOp instanceof Gen2.Kill)
        {
            return buildKillOpspec((Gen2.Kill)tagOp);
        }
        else if(tagOp instanceof Gen2.Lock)
        {
            return buildLockOpSpec((Gen2.Lock)tagOp);
        }
        else if(tagOp instanceof Gen2.Alien.Higgs2.FullLoadImage)
        {
            return buildHiggs2FullLoadImageOpSpec((Gen2.Alien.Higgs2.FullLoadImage)tagOp);
        }
        else if(tagOp instanceof Gen2.Alien.Higgs2.PartialLoadImage)
        {
            return buildHiggs2PartialLoadImageOpSpec((Gen2.Alien.Higgs2.PartialLoadImage)tagOp);
        }        
        else if(tagOp instanceof Gen2.Alien.Higgs3.FastLoadImage)
        {
            return buildHiggs3FastLoadImageOpSpec((Gen2.Alien.Higgs3.FastLoadImage)tagOp);
        }
        else if(tagOp instanceof Gen2.Alien.Higgs3.LoadImage)
        {
            return buildHiggs3LoadImageOpSpec((Gen2.Alien.Higgs3.LoadImage)tagOp);
        }
        else if(tagOp instanceof Gen2.Alien.Higgs3.BlockReadLock)
        {
            return buildHiggs3BlockReadLockOpSpec((Gen2.Alien.Higgs3.BlockReadLock)tagOp);
        }
        else if(tagOp instanceof Gen2.NxpGen2TagOp.SetReadProtect)
        {
            return  buildNXPGen2SetReadProtectOpSpec((Gen2.NxpGen2TagOp.SetReadProtect)tagOp);
        }
        else if(tagOp instanceof Gen2.NxpGen2TagOp.ResetReadProtect)
        {
            return  buildNXPGen2ResetReadProtectOpSpec((Gen2.NxpGen2TagOp.ResetReadProtect)tagOp);
        }        
        else if(tagOp instanceof Gen2.NxpGen2TagOp.ChangeEas)
        {
            return buildNXPGen2ChangeEASOpSpec((Gen2.NxpGen2TagOp.ChangeEas)tagOp);
        }
        else if(tagOp instanceof Gen2.NxpGen2TagOp.Calibrate)
        {
            return buildNXPGen2CalibrateOpSpec((Gen2.NxpGen2TagOp.Calibrate)tagOp);
        }
        else if(tagOp instanceof Gen2.NXP.G2I.ChangeConfig)
        {
            return buidNXPG2IChangeConfig((Gen2.NXP.G2I.ChangeConfig)tagOp);
        }
        else if(tagOp instanceof Gen2.Impinj.Monza4.QTReadWrite)
        {
            return buildImpinjMonza4QTReadWriteOpSpec((Gen2.Impinj.Monza4.QTReadWrite)tagOp);
        }
        else if(tagOp instanceof Gen2.NxpGen2TagOp.EasAlarm)
        {
            return buildNXPGen2EASAlarmOpSpec((Gen2.NxpGen2TagOp.EasAlarm)tagOp);
        }
        else if(tagOp instanceof Iso180006b.ReadData)
        {
            return buildIso180006bReadOpSpec((Iso180006b.ReadData)tagOp);
        }
        else if(tagOp instanceof Iso180006b.WriteData)
        {
            return buildIso180006bWriteOpSpec((Iso180006b.WriteData)tagOp);
        }
        else if(tagOp instanceof Iso180006b.Lock)
        {
            return buildIso180006bLockOpSpec((Iso180006b.Lock)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.EndLog)
        {
            return buildIDSSL900AEndLog((Gen2.IDS.SL900A.EndLog)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.Initialize)
        {
            return buildIDSSL900AInitialize((Gen2.IDS.SL900A.Initialize)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.GetLogState)
        {
            return buildIDSSL900ALoggingForm((Gen2.IDS.SL900A.GetLogState)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.GetSensorValue)
        {
            return buildIDSSL900ASensorValue((Gen2.IDS.SL900A.GetSensorValue)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.SetLogMode)
        {
            return buildIDSSL900ASetLogMode((Gen2.IDS.SL900A.SetLogMode)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.StartLog)
        {
            return buildIDSSL900AStartLog((Gen2.IDS.SL900A.StartLog)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.GetCalibrationData)
        {
            return buildIDSSL900AGetCalibrationData((Gen2.IDS.SL900A.GetCalibrationData)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.SetCalibrationData)
        {
            return buildIDSSL900ASetCalibrationData((Gen2.IDS.SL900A.SetCalibrationData)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.SetSfeParameters)
        {
            return buildIDSSL900ASetSFEParams((Gen2.IDS.SL900A.SetSfeParameters)tagOp);
        }
        else if(tagOp instanceof  Gen2.IDS.SL900A.GetMeasurementSetup)
        {
            return buildIDSSL900AGetMeasurementSetup((Gen2.IDS.SL900A.GetMeasurementSetup)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.AccessFifo)
        {
            return (AccessCommandOpSpec) buildIDSSL900AAccessFifo((Gen2.IDS.SL900A.AccessFifo)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.SetShelfLife)
        {
            return buildIDSSL900ASetShelfLife((Gen2.IDS.SL900A.SetShelfLife)tagOp);
        }
        else if(tagOp instanceof  Gen2.IDS.SL900A.SetLogLimit)
        {
            return buildIDSSL900ASetLogLimits((Gen2.IDS.SL900A.SetLogLimit)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.GetBatteryLevel)
        {
            return buildIDSSL900AGetBatteryLevel((Gen2.IDS.SL900A.GetBatteryLevel)tagOp);
        }
        else if(tagOp instanceof Gen2.IDS.SL900A.SetPassword)
        {
            return buildIDSSL900ASetIDSPassword((Gen2.IDS.SL900A.SetPassword)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.ActivateSecureMode)
        {
            return buildIAVActivateSecureMode((Gen2.Denatran.IAV.ActivateSecureMode)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.ActivateSiniavMode)
        {
            return buildIAVActivateSiniavMode((Gen2.Denatran.IAV.ActivateSiniavMode)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.AuthenticateOBU)
        {
            return buildIAVAuthenticateOBU((Gen2.Denatran.IAV.AuthenticateOBU)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.OBUAuthFullPass1)
        {
            return buildIAVOBUAuthenticateFullPass1((Gen2.Denatran.IAV.OBUAuthFullPass1)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.OBUAuthFullPass2)
        {
            return buildIAVOBUAuthenticateFullPass2((Gen2.Denatran.IAV.OBUAuthFullPass2)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.OBUAuthID)
        {
            return buildIAVOBUAuthenticateID((Gen2.Denatran.IAV.OBUAuthID)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.OBUReadFromMemMap)
        {
            return buildIAVOBUReadFromMemMap((Gen2.Denatran.IAV.OBUReadFromMemMap)tagOp);
        }
        else if(tagOp instanceof Gen2.Denatran.IAV.OBUWriteToMemMap)
        {
            return buildIAVOBUWriteToMemMap((Gen2.Denatran.IAV.OBUWriteToMemMap)tagOp);
        }
        else if(tagOp instanceof Gen2.NXP.AES.Authenticate)
        {
            return buildNXPAESAuthenticate((Gen2.NXP.AES.Authenticate)tagOp);
        }
        else if(tagOp instanceof Gen2.NXP.AES.ReadBuffer)
        {
            return buildNXPAESReadBuffer((Gen2.NXP.AES.ReadBuffer)tagOp);
        }
        else if(tagOp instanceof Gen2.NXP.AES.Untraceable)
        {
            return buildNXPAESUntraceable((Gen2.NXP.AES.Untraceable)tagOp);
        }
        else
        {
            throw new FeatureNotSupportedException("Tag Operation not supported");
        }
    }

    /** Builds tagopSpec list and adds the individual opSpecs to accessCommandOpSpecList
     */
    private void buildtagOpListSpec(SimpleReadPlan srp, AccessCommand accessCommand) throws ReaderException
    {
        TagOp tagOp = srp.Op;
        TagOpList tagOpList = (TagOpList)tagOp;
        if(tagOpList.list.size() == 1)
        {
            srp.Op = (TagOp)tagOpList.list.get(0);
            accessCommand.addToAccessCommandOpSpecList(buildOpSpec(srp));
        }
        else if(tagOpList.list.size() == 2)
        {
            if((tagOpList.list.get(0) instanceof Gen2.WriteData)&& (tagOpList.list.get(1) instanceof Gen2.ReadData))
            {
                Gen2.WriteData writeData = (Gen2.WriteData)tagOpList.list.get(0);
                Gen2.ReadData readData = (Gen2.ReadData)tagOpList.list.get(1);

                // Add writeData and ReadData as OpSpecs to accessSpec
                accessCommand.addToAccessCommandOpSpecList(buildWriteOpSpec(writeData));
                accessCommand.addToAccessCommandOpSpecList(buildReadOpSpec(readData));
            }
            else if((tagOpList.list.get(0) instanceof Gen2.WriteTag)&& (tagOpList.list.get(1) instanceof Gen2.ReadData))
            {
                Gen2.WriteTag writeTag = (Gen2.WriteTag)tagOpList.list.get(0);
                Gen2.ReadData rData = (Gen2.ReadData)tagOpList.list.get(1);

                // Add writeTag and ReadData as OpSpecs to accessSpec
                accessCommand.addToAccessCommandOpSpecList(buildWriteOpSpec(writeTag));
                accessCommand.addToAccessCommandOpSpecList(buildReadOpSpec(rData));
            }
            else
            {
                throw new FeatureNotSupportedException("Unsupported operation");
            }
        }
        else
        {
            throw new FeatureNotSupportedException("Unsupported operation");
        }
    }
    /**
     * Create a OpSpec that reads from user memory
     * @param tagOperation
     * @return c1g2Read
     * @throws ReaderException
     */
    private C1G2Read buildReadOpSpec(TagOp tagOperation) throws ReaderException
    {        
        C1G2Read c1g2Read = new C1G2Read();
        
        c1g2Read.setAccessPassword(getAccessPassword());

        int value = 0x0;
        // Memory Bank
        if(((Gen2.ReadData)tagOperation).banks != null)
        {
            EnumSet<Gen2.Bank> banks = ((Gen2.ReadData)tagOperation).banks;
            Iterator<Bank> iterator = banks.iterator();
            while(iterator.hasNext())
            {
                value |= iterator.next().rep;
            } 
            if(value > 3)
            {
                throw new FeatureNotSupportedException("Operation not supported");
            }
        }
        if((((Gen2.ReadData)tagOperation).Bank.rep) > 3)
        {
          throw new IllegalArgumentException("Invalid argument");
        }
        TwoBitField memBank = new TwoBitField(String.valueOf(((Gen2.ReadData)tagOperation).Bank.rep));
        c1g2Read.setMB(memBank);

        c1g2Read.setWordCount(new UnsignedShort(((Gen2.ReadData)tagOperation).Len));
        c1g2Read.setWordPointer(new UnsignedShort(((Gen2.ReadData)tagOperation).WordAddress));
        
        // Set the OpSpecID to a unique number.
        c1g2Read.setOpSpecID(new UnsignedShort(++opSpecId));
        return c1g2Read;
    }

    private ThingMagicISO180006BRead buildIso180006bReadOpSpec(TagOp tagOp) throws ReaderException
    {
        paramSet(TMConstants.TMR_PARAM_TAGOP_PROTOCOL, TagProtocol.ISO180006B);

        ThingMagicISO180006BRead read = new ThingMagicISO180006BRead();

        //Set byte length
        read.setByteLen(new UnsignedShort(((Iso180006b.ReadData)tagOp).Len));

        //set byte address 
        read.setByteAddress(new UnsignedShort(((Iso180006b.ReadData)tagOp).ByteAddress));

        //set OpSpecId
        read.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return read;
    }

    private ThingMagicISO180006BWrite buildIso180006bWriteOpSpec(TagOp tagOp) throws ReaderException
    {
        paramSet(TMConstants.TMR_PARAM_TAGOP_PROTOCOL, TagProtocol.ISO180006B);

        ThingMagicISO180006BWrite write = new  ThingMagicISO180006BWrite();
        
        //Set byte length
        write.setByteAddress(new UnsignedShort(((Iso180006b.WriteData)tagOp).ByteAddress));

        //Set write data
        write.setWriteData(new UnsignedByteArray_HEX(((Iso180006b.WriteData)tagOp).Data));

        //set OpSpecId
        write.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return write;
    }
    
    private ThingMagicISO180006BLock buildIso180006bLockOpSpec(TagOp tagOp) throws ReaderException
    {
        paramSet(TMConstants.TMR_PARAM_TAGOP_PROTOCOL, TagProtocol.ISO180006B);

        ThingMagicISO180006BLock lock = new ThingMagicISO180006BLock();

        //Set address
        lock.setAddress(new UnsignedByte(((Iso180006b.Lock)tagOp).ByteAddress));

        //Set OpSPecId
        lock.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return lock;
    }

    private C1G2Write buildWriteOpSpec(Gen2.WriteData tagOperation) throws ReaderException
    {
        C1G2Write c1g2Write = new C1G2Write();

        //AccessPassword
        c1g2Write.setAccessPassword(getAccessPassword());

        //Memory Bank
        if((tagOperation.Bank.rep) > 3)
        {
          throw new IllegalArgumentException("Invalid argument");
        }
        TwoBitField memBank = new TwoBitField(String.valueOf(tagOperation.Bank.rep));
        c1g2Write.setMB(memBank);

        //WriteData
        c1g2Write.setWriteData(new UnsignedShortArray_HEX(tagOperation.Data));

        //WordPointer
        c1g2Write.setWordPointer(new UnsignedShort(tagOperation.WordAddress));

        // Set the OpSpecID to a unique number.
        c1g2Write.setOpSpecID(new UnsignedShort(++opSpecId));
        return c1g2Write;
    }

    private ThingMagicWriteTag buildWriteOpSpec(Gen2.WriteTag tagOperation) throws ReaderException {

        ThingMagicWriteTag writeTag = new ThingMagicWriteTag();

        //AccessPassword
        writeTag.setAccessPassword(getAccessPassword());
        
        //Set WriteData
        Gen2.TagData tagData = tagOperation.Epc;
        byte[] epcBytes = tagData.epcBytes();
        UnsignedShortArray_HEX shortData = new UnsignedShortArray_HEX(ReaderUtil.convertByteArrayToShortArray(epcBytes));
        writeTag.setWriteData(shortData);
        
        //Set the OpSpecID to a unique number.
        writeTag.setOpSpecID(new UnsignedShort(++opSpecId));
        return writeTag;
    }
    
    private C1G2BlockWrite buildBlockWriteOpSpec(Gen2.BlockWrite tagOp) throws ReaderException
    {
        C1G2BlockWrite c1g2BlockWrite = new C1G2BlockWrite();

        //Memory Bank
        if((tagOp.Bank.rep) > 3)
        {
          throw new IllegalArgumentException("Invalid argument");
        }
        TwoBitField memBank = new TwoBitField(String.valueOf(tagOp.Bank.rep));
        c1g2BlockWrite.setMB(memBank);

        //AccessPassword
        c1g2BlockWrite.setAccessPassword(getAccessPassword());

        short[] writeData = tagOp.Data;
        c1g2BlockWrite.setWriteData(new UnsignedShortArray_HEX(writeData));
  
        //WordPointer
        c1g2BlockWrite.setWordPointer(new UnsignedShort(tagOp.WordPtr));

        // Set the OpSpecID to a unique number.
        c1g2BlockWrite.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return c1g2BlockWrite;
    }

    private C1G2BlockErase buildBlockEraseOpSpec(Gen2.BlockErase tagOp) throws ReaderException
    {
        C1G2BlockErase c1g2BlockErase = new C1G2BlockErase();

        //Memory Bank
        if((tagOp.Bank.rep) > 3)
        {
          throw new IllegalArgumentException("Invalid argument");
        }
        TwoBitField memBank = new TwoBitField(String.valueOf(tagOp.Bank.rep));
        c1g2BlockErase.setMB(memBank);

        //AccessPassword
        c1g2BlockErase.setAccessPassword(getAccessPassword());

        //WordPointer & WordCount
        c1g2BlockErase.setWordPointer(new UnsignedShort(tagOp.WordPtr));
        c1g2BlockErase.setWordCount(new UnsignedShort(tagOp.WordCount));

        // Set the OpSpecID to a unique number.
        c1g2BlockErase.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return c1g2BlockErase;
    }


    /**
     * Block PermaLock custom command
     * @param tagOp
     * @return tmBlockPermalock
     * @throws ReaderException
     */
    private AccessCommandOpSpec buildBlockPermaLockOpSpec(Gen2.BlockPermaLock tagOp) throws ReaderException
    {
        ThingMagicBlockPermalock tmBlockPermalock = new ThingMagicBlockPermalock();        

        //Memory Bank
        if((tagOp.Bank.rep) > 3)
        {
          throw new IllegalArgumentException("Invalid argument");
        }
        TwoBitField memBank = new TwoBitField(String.valueOf(tagOp.Bank.rep));
        tmBlockPermalock.setMB(memBank);

        //AccessPassword
        tmBlockPermalock.setAccessPassword(getAccessPassword());

        //BlockPointer, BlockMask, ReadLock
        tmBlockPermalock.setBlockPointer(new UnsignedInteger(tagOp.BlockPtr));
        if (null != tagOp.Mask)// && tagOp.BlockRange == tagOp.Mask.length)
        {
            tmBlockPermalock.setBlockMask(new UnsignedShortArray_HEX(tagOp.Mask));
        }
        else
        {
            UnsignedShortArray_HEX hexShortArray = new UnsignedShortArray_HEX();
            hexShortArray.add(new UnsignedShort(0));
            tmBlockPermalock.setBlockMask(hexShortArray);
//            throw new ReaderException("BlockPermaLock tag operation validation failed");
        }
        tmBlockPermalock.setReadLock(new UnsignedByte(tagOp.ReadLock));

        // Set the OpSpecID to a unique number.
        tmBlockPermalock.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return tmBlockPermalock;
    }
        
    private C1G2Kill buildKillOpspec(Gen2.Kill tagOp)
    {
        Gen2.Password password = new Gen2.Password(tagOp.KillPassword);
        C1G2Kill c1g2Kill = new C1G2Kill();
        int killPassword = password.value;
        
        //Set KillPassword
        c1g2Kill.setKillPassword(new UnsignedInteger(new Integer(killPassword)));
        
        //Set OpSpecId
        c1g2Kill.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return c1g2Kill;
    }

    private C1G2Lock buildLockOpSpec(Gen2.Lock tagOp) throws ReaderException
    {
        C1G2Lock c1g2Lock = new C1G2Lock();
        List<C1G2LockPayload> c1g2PayLoadlist = new ArrayList<C1G2LockPayload>();
        C1G2LockPayload c1g2LockPayload = new C1G2LockPayload();
        Gen2.LockAction lockAction = tagOp.Action;
        int[] initLockActionToName = (int[]) parseValue(lockAction.toString());

        //Set C1G2LockDataField
        C1G2LockDataField c1g2LockDataField = new C1G2LockDataField();
        c1g2LockDataField.set(initLockActionToName[0]);
        c1g2LockPayload.setDataField(c1g2LockDataField);

        //Set C1G2LockPrivilege
        C1G2LockPrivilege c1g2LockPrivilege = new C1G2LockPrivilege();
        c1g2LockPrivilege.set(initLockActionToName[1]);
        c1g2LockPayload.setPrivilege(c1g2LockPrivilege);

        c1g2PayLoadlist.add(c1g2LockPayload);
        c1g2Lock.setC1G2LockPayloadList(c1g2PayLoadlist);

        //Set AccessPassword
        c1g2Lock.setAccessPassword(new UnsignedInteger(new Integer(tagOp.AccessPassword)));

        //Set OpSpecId
        c1g2Lock.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return c1g2Lock;
    }

    private ThingMagicHiggs2FullLoadImage buildHiggs2FullLoadImageOpSpec(Gen2.Alien.Higgs2.FullLoadImage tagOp) throws ReaderException
    {
        ThingMagicHiggs2FullLoadImage tmFullLoadImage = new ThingMagicHiggs2FullLoadImage();

        //Set  CurrentAccessPassword
        tmFullLoadImage.setCurrentAccessPassword(getAccessPassword());

        //Set  AccessPassword
        tmFullLoadImage.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //set killPassword
        tmFullLoadImage.setKillPassword(new UnsignedInteger(new Integer(tagOp.killPassword)));

        //set LockBits
        tmFullLoadImage.setLockBits(new UnsignedShort(tagOp.lockBits));

        //Set PcWord
        tmFullLoadImage.setPCWord(new UnsignedShort(tagOp.pcWord));

        //Set EPCData
        tmFullLoadImage.setEPCData(new UnsignedByteArray_HEX(tagOp.epc));

        //Set OpSpecId
        tmFullLoadImage.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return tmFullLoadImage;
    }

    private ThingMagicHiggs2PartialLoadImage buildHiggs2PartialLoadImageOpSpec(Gen2.Alien.Higgs2.PartialLoadImage tagOp) throws ReaderException
    {
        ThingMagicHiggs2PartialLoadImage tmPartialLoadImage = new ThingMagicHiggs2PartialLoadImage();

        //Set  CurrentAccessPassword
        tmPartialLoadImage.setCurrentAccessPassword(getAccessPassword());

        //Set  AccessPassword
        tmPartialLoadImage.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //set killPassword
        tmPartialLoadImage.setKillPassword(new UnsignedInteger(new Integer(tagOp.killPassword)));
        
        //Set EPCData
        tmPartialLoadImage.setEPCData(new UnsignedByteArray_HEX(tagOp.epc));
        
        //Set OpSpecId
        tmPartialLoadImage.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return tmPartialLoadImage;
    }
    
    private ThingMagicHiggs3FastLoadImage buildHiggs3FastLoadImageOpSpec(Gen2.Alien.Higgs3.FastLoadImage tagOp)
    {
        ThingMagicHiggs3FastLoadImage tmFastLoadImage = new ThingMagicHiggs3FastLoadImage();

        //set currentAccessPassword
        tmFastLoadImage.setCurrentAccessPassword(new UnsignedInteger(new Integer(tagOp.currentAccessPassword)));

        //Set AccessPassword
        tmFastLoadImage.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //Set KillPassword
        tmFastLoadImage.setKillPassword(new UnsignedInteger(new Integer(tagOp.killPassword)));

        //Set pcWord
        tmFastLoadImage.setPCWord(new UnsignedShort(tagOp.pcWord));

        //Set EPCData
        tmFastLoadImage.setEPCData(new UnsignedByteArray_HEX(tagOp.epc));

        //Set OpSpecId
        tmFastLoadImage.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return tmFastLoadImage;
    }

    private ThingMagicHiggs3LoadImage buildHiggs3LoadImageOpSpec(Gen2.Alien.Higgs3.LoadImage tagOp)
    {
        ThingMagicHiggs3LoadImage loadImage = new ThingMagicHiggs3LoadImage();

        //Set AccessPassword
        loadImage.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //Set currentAccessPassword
        loadImage.setCurrentAccessPassword(new UnsignedInteger(new Integer(tagOp.currentAccessPassword)));

        //Set EPCAndUserData
        loadImage.setEPCAndUserData(new UnsignedByteArray_HEX(tagOp.EPCAndUserData));

        //Set killPassword
        loadImage.setKillPassword(new UnsignedInteger(new Integer(tagOp.killPassword)));

        //Set PcWord
        loadImage.setPCWord(new UnsignedShort(tagOp.pcWord));

        //Set OpSpecId
        loadImage.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return loadImage;
    }

    private ThingMagicHiggs3BlockReadLock buildHiggs3BlockReadLockOpSpec(Gen2.Alien.Higgs3.BlockReadLock tagOp)
    {
        ThingMagicHiggs3BlockReadLock blockReadLock = new ThingMagicHiggs3BlockReadLock();

        //Set AccessPassword
        blockReadLock.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //Set LockBits
        blockReadLock.setLockBits(new UnsignedByte(tagOp.lockBits));

        //Set OpSpecId
        blockReadLock.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return blockReadLock;
    }

    private Custom  buildNXPGen2SetReadProtectOpSpec(Gen2.NxpGen2TagOp.SetReadProtect tagOp)
    {
        if (tagOp instanceof Gen2.NXP.G2X.SetReadProtect)
        {
            ThingMagicNXPG2XSetReadProtect setReadProtect = new ThingMagicNXPG2XSetReadProtect();

            //Set AccessPassword
            setReadProtect.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            setReadProtect.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

            return setReadProtect;
        }
        else
        {
            ThingMagicNXPG2ISetReadProtect setReadProtect = new ThingMagicNXPG2ISetReadProtect();

            //Set AccessPassword
            setReadProtect.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            setReadProtect.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

            return setReadProtect;
        }
    }

    private Custom buildNXPGen2ResetReadProtectOpSpec(Gen2.NxpGen2TagOp.ResetReadProtect tagOp)
    {
        if (tagOp instanceof Gen2.NXP.G2X.ResetReadProtect)
        {
            ThingMagicNXPG2XResetReadProtect resetReadProtect = new ThingMagicNXPG2XResetReadProtect();

            //Set AccessPassword
            resetReadProtect.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            resetReadProtect.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return resetReadProtect;
        }
        else
        {
            ThingMagicNXPG2IResetReadProtect resetReadProtect = new ThingMagicNXPG2IResetReadProtect();

            //Set AccessPassword
            resetReadProtect.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            resetReadProtect.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return resetReadProtect;
        }
    }
    
    private Custom buildNXPGen2ChangeEASOpSpec(Gen2.NxpGen2TagOp.ChangeEas tagOp)
    {

        if (tagOp instanceof Gen2.NXP.G2X.ChangeEas)
        {
            ThingMagicNXPG2XChangeEAS changeEAS = new ThingMagicNXPG2XChangeEAS();

            //Set AccessPassword
            changeEAS.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set EASState
            changeEAS.setReset(new Bit(tagOp.reset));

            //Set OpSpecId
            changeEAS.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return changeEAS;
        }
        else
        {
            ThingMagicNXPG2IChangeEAS changeEAS = new ThingMagicNXPG2IChangeEAS();

            //Set AccessPassword
            changeEAS.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set EASState
            changeEAS.setReset(new Bit(tagOp.reset));

            //Set OpSpecId
            changeEAS.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return changeEAS;
        }
    }

    private Custom buildNXPGen2CalibrateOpSpec(Gen2.NxpGen2TagOp.Calibrate tagOp)
    {
        if (tagOp instanceof Gen2.NXP.G2X.Calibrate)
        {
            ThingMagicNXPG2XCalibrate tmCalibrate = new ThingMagicNXPG2XCalibrate();

            //Set AccessPassword
            tmCalibrate.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            tmCalibrate.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return tmCalibrate;
        }
        else
        {
            ThingMagicNXPG2ICalibrate tmCalibrate = new ThingMagicNXPG2ICalibrate();

            //Set AccessPassword
            tmCalibrate.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set OpSpecId
            tmCalibrate.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return tmCalibrate;
        }
    }

    private ThingMagicNXPG2IChangeConfig buidNXPG2IChangeConfig(Gen2.NXP.G2I.ChangeConfig tagOp)
    {
        ThingMagicNXPG2IChangeConfig changeConfig = new ThingMagicNXPG2IChangeConfig();
       
        //Set AccessPassword
        changeConfig.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        //Get ConfigWord
        Gen2.NXP.G2I.ConfigWord word = new Gen2.NXP.G2I.ConfigWord();
        ConfigWord configWord = word.getConfigWord(tagOp.configWord);

        //Set tmConfigWord
        ThingMagicNXPConfigWord tmConfigWord = new ThingMagicNXPConfigWord();

        tmConfigWord.setConditionalReadRangeReduction_OnOff(new Bit(configWord.conditionalReadRangeReduction_onOff));
        tmConfigWord.setConditionalReadRangeReduction_OpenShort(new Bit(configWord.conditionalReadRangeReduction_openShort));
        tmConfigWord.setDataMode(new Bit(configWord.dataMode));
        tmConfigWord.setDigitalOutput(new Bit(configWord.digitalOutput));
        tmConfigWord.setExternalSupply(new Bit(configWord.externalSupply));
        tmConfigWord.setInvertDigitalOutput(new Bit(configWord.invertDigitalOutput));
        tmConfigWord.setMaxBackscatterStrength(new Bit(configWord.maxBackscatterStrength));
        tmConfigWord.setPSFAlarm(new Bit(configWord.psfAlarm));
        tmConfigWord.setPrivacyMode(new Bit(configWord.privacyMode));
        tmConfigWord.setReadProtectEPC(new Bit(configWord.readProtectEPC));
        tmConfigWord.setReadProtectTID(new Bit(configWord.readProtectTID));
        tmConfigWord.setReadProtectUser(new Bit(configWord.readProtectUser));
        tmConfigWord.setTamperAlarm(new Bit(configWord.tamperAlarm));
        tmConfigWord.setTransparentMode(new Bit(configWord.transparentMode));
        changeConfig.setThingMagicNXPConfigWord(tmConfigWord);

        //Set OpSpecId
        changeConfig.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return changeConfig;
    }

    private ThingMagicImpinjMonza4QTReadWrite buildImpinjMonza4QTReadWriteOpSpec(Gen2.Impinj.Monza4.QTReadWrite tagOp)
    {
        ThingMagicImpinjMonza4QTReadWrite qtReadWrite = new ThingMagicImpinjMonza4QTReadWrite();
        qtReadWrite.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

        Gen2.Impinj.Monza4.QTControlByte qtControlByte = new Gen2.Impinj.Monza4.QTControlByte();
        qtControlByte.Persistence = false;
        qtControlByte.QTReadWrite = false;

        int controlByte = tagOp.controlByte;

        if((controlByte&0x80) != 0){
            qtControlByte.QTReadWrite = true;
        }
        if((controlByte&0x40) != 0){
            qtControlByte.Persistence = true;
        }

        //Set ReadWrite & Persistence
        ThingMagicMonza4ControlByte tmControlByte = new ThingMagicMonza4ControlByte();
        tmControlByte.setReadWrite(new Bit(qtControlByte.QTReadWrite));
        tmControlByte.setPersistance(new Bit(qtControlByte.Persistence));

        Gen2.Impinj.Monza4.QTPayload qtPayload = new Gen2.Impinj.Monza4.QTPayload();
        qtPayload.QTSR = false;
        qtPayload.QTMEM = false;
        int payload = tagOp.payloadWord;

        if((payload&0x8000)!=0)
        {
            qtPayload.QTSR = true;
        }
        if((payload&0x4000)!=0)
        {
            qtPayload.QTMEM = true;
        }

        //Set QTMEM & QTSR
        ThingMagicMonza4Payload tmPayload = new ThingMagicMonza4Payload();
        tmPayload.setQT_MEM(new Bit(qtPayload.QTMEM));
        tmPayload.setQT_SR(new Bit(qtPayload.QTSR));

        //Set Payload
        qtReadWrite.setThingMagicMonza4Payload(tmPayload);

        //Set ControlByte
        qtReadWrite.setThingMagicMonza4ControlByte(tmControlByte);

        //Set OpSpecId
        qtReadWrite.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return qtReadWrite;

    }
    private Custom buildNXPGen2EASAlarmOpSpec(Gen2.NxpGen2TagOp.EasAlarm tagOp)
    {
        ThingMagicGen2DivideRatio divideRatio = new ThingMagicGen2DivideRatio(tagOp.divideRatio.rep);
        ThingMagicGen2TagEncoding tagEncoding = new ThingMagicGen2TagEncoding(tagOp.tagEncoding.rep);

        if (tagOp instanceof Gen2.NXP.G2X.EasAlarm)
        {
            ThingMagicNXPG2XEASAlarm tmEASAlarm = new ThingMagicNXPG2XEASAlarm();

            //Set AccessPassword
            tmEASAlarm.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set DivideRatio
            tmEASAlarm.setDivideRatio(divideRatio);

            //Set TrExt
            tmEASAlarm.setPilotTone(new Bit(tagOp.trExt.rep));

            //Set TagEncoding

            tmEASAlarm.setTagEncoding(tagEncoding);

            //Set OpSpecId
            tmEASAlarm.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return tmEASAlarm;
        }
        else
        {
            ThingMagicNXPG2IEASAlarm tmEASAlarm = new ThingMagicNXPG2IEASAlarm();

            //Set AccessPassword
            tmEASAlarm.setAccessPassword(new UnsignedInteger(new Integer(tagOp.accessPassword)));

            //Set DivideRatio
            tmEASAlarm.setDivideRatio(divideRatio);

            //Set TrExt
            tmEASAlarm.setPilotTone(new Bit(tagOp.trExt.rep));

            //Set TagEncoding
            tmEASAlarm.setTagEncoding(tagEncoding);

            //Set OpSpecId
            tmEASAlarm.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
            return tmEASAlarm;
        }
    }

    /**
     * Create a OpSpec that reads the status of the logging process
     * @param tagOp
     * @return ThingMagicIDSSL900AGetLogState
     */
    private ThingMagicIDSSL900AGetLogState buildIDSSL900ALoggingForm(Gen2.IDS.SL900A.GetLogState tagOp)
    {
        ThingMagicIDSSL900AGetLogState logState = new ThingMagicIDSSL900AGetLogState();

        logState.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));
        
        return logState;
    }

    private ThingMagicIDSSL900ACommandRequest initIDSCommandRequest(Gen2.IDS.SL900A tagOp)
    {
        ThingMagicIDSSL900ACommandRequest cmdRequest = new ThingMagicIDSSL900ACommandRequest();

        //Set AccessPassword
        cmdRequest.setAccessPassword(new UnsignedInteger(tagOp.accessPassword));

        //Set CommandCode
        cmdRequest.setCommandCode(new UnsignedByte(tagOp.commandCode));

        //Set IDSPassword
        cmdRequest.setIDSPassword(new UnsignedInteger(tagOp.password));

        //Set PasswordLevel
        ThingMagicCustomIDSPasswordLevel passwordLevel = new ThingMagicCustomIDSPasswordLevel(tagOp.passwordLevel.rep);
        cmdRequest.setPasswordLevel(passwordLevel);

        //Set OpSpecId
        cmdRequest.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return cmdRequest;
    }

    /**
     * Create a OpSpec that stops the logging procedure
     * @param tagOp
     * @return ThingMagicIDSSL900AEndLog
     */
    private ThingMagicIDSSL900AEndLog buildIDSSL900AEndLog(Gen2.IDS.SL900A.EndLog tagOp)
    {
        ThingMagicIDSSL900AEndLog endLog = new ThingMagicIDSSL900AEndLog();

        endLog.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        return endLog;
    }

    /**
     * Create a OpSpec that sets the Delay time and Application Data fields
     * @param tagOp
     * @return ThingMagicIDSSL900AInitialize
     */
    private ThingMagicIDSSL900AInitialize buildIDSSL900AInitialize(Gen2.IDS.SL900A.Initialize tagOp)
    {
        ThingMagicIDSSL900AInitialize initialize = new ThingMagicIDSSL900AInitialize();

        //Set Command Request
        initialize.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //Set Application Data
        ThingMagicIDSApplicationData appData = new ThingMagicIDSApplicationData();
        appData.setnumberOfWords(new UnsignedShort(tagOp.appData.getNumberofWords()));
        appData.setbrokenWordPointer(new UnsignedByte(tagOp.appData.getBrokenWordPointer()));
        initialize.setThingMagicIDSApplicationData(appData);

        //Set Delay Time
        int delayMode = tagOp.delayTime.getMode().rep;
        ThingMagicIDSDelayTime delayTime = new ThingMagicIDSDelayTime();
        delayTime.setdelayMode(new UnsignedByte(delayMode));
        delayTime.setdelayTime(new UnsignedShort(tagOp.delayTime.getTime()));
        delayTime.settimerEnable(new Bit(tagOp.delayTime.getIrqTimerEnable()));
        initialize.setThingMagicIDSDelayTime(delayTime);

        return initialize;
    }

    /**
     * Create a OpSpec that starts the AD conversion on specified sensor
     * @param tagOp
     * @return ThingMagicIDSSL900ASensorValue
     */
    private ThingMagicIDSSL900ASensorValue buildIDSSL900ASensorValue(Gen2.IDS.SL900A.GetSensorValue tagOp)
    {
        ThingMagicIDSSL900ASensorValue sensorValue = new ThingMagicIDSSL900ASensorValue();

        //Set Command Request
        sensorValue.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //set Sensortype
        ThingMagicCustomIDSSensorType sensorType = new ThingMagicCustomIDSSensorType(tagOp.sensorType.rep);
        sensorValue.setSensorType(sensorType);

        return sensorValue;
    }

    /**
     * Create a OpSpec that sets the logging form
     * @param tagOp
     * @return ThingMagicIDSSL900ASetLogMode
     */
    private ThingMagicIDSSL900ASetLogMode buildIDSSL900ASetLogMode(Gen2.IDS.SL900A.SetLogMode tagOp)
    {
        ThingMagicIDSSL900ASetLogMode setLogMode = new ThingMagicIDSSL900ASetLogMode();

        //Set Command Request
        setLogMode.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //set Battery Sensor
        setLogMode.setBattEnable(new Bit(tagOp.battEnable));
        //Set EXT1 external sensor
        setLogMode.setExt1Enable(new Bit(tagOp.ext1Enable));
        //Set EXT2 external sensor
        setLogMode.setExt2Enable(new Bit(tagOp.ext2Enable));
        //Set Logging interval
        setLogMode.setLogInterval(new UnsignedShort(tagOp._logInterval));
        //Set Logging format
        ThingMagicCustomIDSLoggingForm loggingForm = new ThingMagicCustomIDSLoggingForm(tagOp.form.rep);
        setLogMode.setLoggingForm(loggingForm);
        //Set StorageRule
        ThingMagicCustomIDSStorageRule storageRule = new ThingMagicCustomIDSStorageRule(tagOp.storage.rep);
        setLogMode.setStorageRule(storageRule);
        //Set temperature sensor
        setLogMode.setTempEnable(new Bit(tagOp.tempEnable));

        return setLogMode;
    }

   /**
     * Create a OpSpec that starts the logging process
     * @param tagOp
     * @return ThingMagicIDSSL900AStartLog
     * throws ReaderException
     */
   private ThingMagicIDSSL900AStartLog buildIDSSL900AStartLog(Gen2.IDS.SL900A.StartLog tagOp) throws ReaderException
   {
        ThingMagicIDSSL900AStartLog startLog = new ThingMagicIDSSL900AStartLog();

        //Set Command Request
        startLog.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //Set Start time
        startLog.setStartTime(new UnsignedInteger(toSL900aTime(tagOp.startTime)));
        return startLog;
    }

     /**
     * Create a OpSpec that reads the calibration field and the SFE parameter field
     * @param tagOp
     * @return ThingMagicIDSSL900AGetCalibrationData
     */
    private ThingMagicIDSSL900AGetCalibrationData buildIDSSL900AGetCalibrationData(Gen2.IDS.SL900A.GetCalibrationData tagOp)
    {
        ThingMagicIDSSL900AGetCalibrationData getCalibrationData = new ThingMagicIDSSL900AGetCalibrationData();

        //Set Command Request
        getCalibrationData.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        return getCalibrationData;
    }


    /**
     * Create a OpSpec that writes the calibration block
     * @param tagOp
     * @return ThingMagicIDSSL900ASetCalibrationData
     */
    private ThingMagicIDSSL900ASetCalibrationData buildIDSSL900ASetCalibrationData(Gen2.IDS.SL900A.SetCalibrationData tagOp)
    {
        ThingMagicIDSSL900ASetCalibrationData setCalibrationData = new ThingMagicIDSSL900ASetCalibrationData();

        //Set Command Request
        setCalibrationData.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //Set Calibration Data
        ThingMagicIDSCalibrationData calibrationData = new ThingMagicIDSCalibrationData();
        calibrationData.setraw(new UnsignedLong(tagOp.cal.raw));
        calibrationData.setcoars1(new UnsignedByte(tagOp.cal.getCoarse1()));
        calibrationData.setcoars2(new UnsignedByte(tagOp.cal.getCoarse2()));
        calibrationData.setdf(new UnsignedByte(tagOp.cal.getDf()));
        calibrationData.setexcRes(new Bit(tagOp.cal.getExcRes()));
        calibrationData.setgndSwitch(new Bit(tagOp.cal.getGndSwitch()));
        calibrationData.setirlev(new UnsignedByte(tagOp.cal.getIrlev()));
        calibrationData.setselp12(new UnsignedByte(tagOp.cal.getSelp12()));
        calibrationData.setselp22(new UnsignedByte(tagOp.cal.getSelp22()));
        calibrationData.setswExtEn(new Bit(tagOp.cal.getSwExtEn()));
        calibrationData.setad1(new UnsignedByte(0));
        calibrationData.setad2(new UnsignedByte(0));
        calibrationData.setadf(new UnsignedByte(0));
        calibrationData.setringCal(new UnsignedByte(0));
        calibrationData.setoffInt(new UnsignedByte(0));
        calibrationData.setreftc(new UnsignedByte(0));
        calibrationData.setRFU(new UnsignedByte(0));
        calibrationData.setCalibrationType(new UnsignedByte(0));
        calibrationData.setcalibrationValueByteStream(new UnsignedByteArray(0));
        setCalibrationData.setThingMagicIDSCalibrationData(calibrationData);

        return setCalibrationData;
    }

    /**
     * Create a OpSpec that writes the sensor front end parameters to the memory
     * @param tagOp
     * @return ThingMagicIDSSL900ASetSFEParams
     */
    private ThingMagicIDSSL900ASetSFEParams buildIDSSL900ASetSFEParams(Gen2.IDS.SL900A.SetSfeParameters tagOp)
    {
        ThingMagicIDSSL900ASetSFEParams sfeParameter = new ThingMagicIDSSL900ASetSFEParams();

        //Set command Request
        sfeParameter.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        ThingMagicIDSSFEParam sfeParam = new ThingMagicIDSSFEParam();
        sfeParam.setraw(new UnsignedShort(tagOp.sfeParameter.raw));
        sfeParam.setAutoRangeDisable(new Bit(tagOp.sfeParameter.getAutorangeDisable()));
        sfeParam.setExt1(new UnsignedByte(tagOp.sfeParameter.getExt1()));
        sfeParam.setExt2(new UnsignedByte(tagOp.sfeParameter.getExt2()));
        ThingMagicCustomIDSSFEType sfeType = new ThingMagicCustomIDSSFEType(0);
        sfeParam.setSFEType(sfeType);
        sfeParam.setVerifySensorID(new UnsignedByte(tagOp.sfeParameter.getVerifySensorID()));
        sfeParam.setrange(new UnsignedByte(tagOp.sfeParameter.getRang()));
        sfeParam.setseti(new UnsignedByte(tagOp.sfeParameter.getSeti()));

        sfeParameter.setThingMagicIDSSFEParam(sfeParam);
        return sfeParameter;
    }

    /**
     * Create a OpSpec that reads the current system setup of the chip
     * @param tagOp
     * @return ThingMagicIDSSL900AGetMeasurementSetup
     */
    private ThingMagicIDSSL900AGetMeasurementSetup buildIDSSL900AGetMeasurementSetup(Gen2.IDS.SL900A.GetMeasurementSetup tagOp)
    {
        ThingMagicIDSSL900AGetMeasurementSetup measurementSetup = new ThingMagicIDSSL900AGetMeasurementSetup();

        //Set command request
        measurementSetup.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));
        return measurementSetup;
    }

    /**
     * Create a OpSpec that reads and write data from FIFO
     * @param tagOp
     * @return Object
     */
    private Object buildIDSSL900AAccessFifo(Gen2.IDS.SL900A.AccessFifo tagOp)
    {
        if (tagOp instanceof Gen2.IDS.SL900A.AccessFifoRead)
        {
            Gen2.IDS.SL900A.AccessFifoRead op = (Gen2.IDS.SL900A.AccessFifoRead) tagOp;

            ThingMagicIDSSL900AAccessFIFORead fifoRead = new ThingMagicIDSSL900AAccessFIFORead();

            //Set Command Request
            fifoRead.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

            //Set FIFO Read Length
            fifoRead.setFIFOReadLength(new UnsignedByte(op.length));

            return fifoRead;
        }
        else if (tagOp instanceof Gen2.IDS.SL900A.AccessFifoStatus)
        {
            ThingMagicIDSSL900AAccessFIFOStatus fifoStatus = new ThingMagicIDSSL900AAccessFIFOStatus();

            //Set Command Request
            fifoStatus.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

            return fifoStatus;
        }
        else if (tagOp instanceof Gen2.IDS.SL900A.AccessFifoWrite)
        {
            Gen2.IDS.SL900A.AccessFifoWrite op = (Gen2.IDS.SL900A.AccessFifoWrite) tagOp;

            ThingMagicIDSSL900AAccessFIFOWrite fifoWrite = new ThingMagicIDSSL900AAccessFIFOWrite();

            //Set Command Request
            fifoWrite.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

            //Set write payload
            fifoWrite.setwritePayLoad(new UnsignedByteArray(op.payload));

            return fifoWrite;
        }
        throw new IllegalArgumentException("Unsupported AccessFifo tagop: " + tagOp);
    }

    /**
     * buildIDSSL900ASetShelfLife
     * @param tagOp
     * @return ThingMagicIDSSetShelfLife
     */
    private ThingMagicIDSSetShelfLife buildIDSSL900ASetShelfLife(Gen2.IDS.SL900A.SetShelfLife tagOp)
    {
        ThingMagicIDSSetShelfLife setShelfLife = new ThingMagicIDSSetShelfLife();

        //Set SHelfLifeBlock0
        ThingMagicIDSSLBlock0 slBlock0 = new ThingMagicIDSSLBlock0();
        slBlock0.setEa(new UnsignedByte(tagOp.slBlock0.getEa()));
        slBlock0.setTimeMax(new UnsignedByte(tagOp.slBlock0.getTmax()));
        slBlock0.setTimeMin(new UnsignedByte(tagOp.slBlock0.getTmin()));
        slBlock0.setTimeStd(new UnsignedByte(tagOp.slBlock0.getTstd()));
        slBlock0.setraw(new UnsignedInteger(tagOp.slBlock0.raw));
        setShelfLife.setThingMagicIDSSLBlock0(slBlock0);

        //Set ShelfLifeBlock1
        ThingMagicIDSSLBlock1 slBlock1 = new ThingMagicIDSSLBlock1();
        slBlock1.setRFU(new UnsignedByte(0));
        slBlock1.setSLInit(new UnsignedShort(tagOp.slBlock1.getSLinit()));
        slBlock1.setTInit(new UnsignedShort(tagOp.slBlock1.getTint()));
        slBlock1.setalgorithmEnable(new Bit(tagOp.slBlock1.getAlgorithmEnable()));
        slBlock1.setSensorID(new UnsignedByte(tagOp.slBlock1.getSensor()));
        slBlock1.setenableNegative(new Bit(tagOp.slBlock1.getEnableNegative()));
        slBlock1.setraw(new UnsignedInteger(tagOp.slBlock1.raw));
        setShelfLife.setThingMagicIDSSLBlock1(slBlock1);

        //Set Command Request
        setShelfLife.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        return setShelfLife;
    }

    /**
     * Create a OpSpec that writes 4 log limits
     * @param tagOp
     * @return ThingMagicIDSSL900ASetLogLimits
     */
    private ThingMagicIDSSL900ASetLogLimits buildIDSSL900ASetLogLimits(Gen2.IDS.SL900A.SetLogLimit tagOp)
    {
        ThingMagicIDSSL900ASetLogLimits setLogLimit = new ThingMagicIDSSL900ASetLogLimits();

        //Set Log Limit Data
        ThingMagicIDSLogLimits logLimits = new ThingMagicIDSLogLimits();
        logLimits.setextremeLower(new UnsignedShort(tagOp.logLimit.getExtremeLowerLimit()));
        logLimits.setupper(new UnsignedShort(tagOp.logLimit.getUpperLimit()));
        logLimits.setextremeUpper(new UnsignedShort(tagOp.logLimit.getExtremeUpperLimit()));
        logLimits.setlower(new UnsignedShort(tagOp.logLimit.getLowerLimit()));
        setLogLimit.setThingMagicIDSLogLimits(logLimits);

        //Set Command Request
        setLogLimit.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        return setLogLimit;
    }

    /**
     * Create a OpSpec that starts the AD conversion on battery voltage
     * @param tagOp
     * @return ThingMagicIDSSL900AGetBatteryLevel
     */
    private ThingMagicIDSSL900AGetBatteryLevel buildIDSSL900AGetBatteryLevel(Gen2.IDS.SL900A.GetBatteryLevel tagOp)
    {
        ThingMagicIDSSL900AGetBatteryLevel batteryLevel = new ThingMagicIDSSL900AGetBatteryLevel();

        //Set Command Request
        batteryLevel.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        batteryLevel.setBatteryTrigger(new UnsignedByte(tagOp.type.rep));

        return batteryLevel;
    }

    /**
     * Create a OpSpec that writes 32bit password
     * @param tagOp
     * @return ThingMagicIDSSL900ASetIDSPassword
     */
    private ThingMagicIDSSL900ASetIDSPassword buildIDSSL900ASetIDSPassword(Gen2.IDS.SL900A.SetPassword tagOp)
    {
        ThingMagicIDSSL900ASetIDSPassword password = new ThingMagicIDSSL900ASetIDSPassword();

        //Set IDS Password
        password.setThingMagicIDSSL900ACommandRequest(initIDSCommandRequest(tagOp));

        //Set IDS new passwordLevel
        ThingMagicCustomIDSPasswordLevel passwordLevel = new ThingMagicCustomIDSPasswordLevel(tagOp.newPasswordLevel);
        password.setNewPasswordLevel(passwordLevel);

        //Set IDS new password
        password.setNewIDSPassword(new UnsignedInteger(tagOp.newPassword));

        return password;
    }

    private ThingMagicDenatranIAVActivateSecureMode buildIAVActivateSecureMode(Gen2.Denatran.IAV.ActivateSecureMode tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVActivateSecureMode activateSecureMode = new ThingMagicDenatranIAVActivateSecureMode();

        //Set IAVCommandRequest
        activateSecureMode.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return activateSecureMode;
    }

    private ThingMagicDenatranIAVActivateSiniavMode buildIAVActivateSiniavMode(Gen2.Denatran.IAV.ActivateSiniavMode tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVActivateSiniavMode activateSiniavMode = new ThingMagicDenatranIAVActivateSiniavMode();

        //Set IAVCommandRequest
        activateSiniavMode.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return activateSiniavMode;
    }

    private ThingMagicDenatranIAVAuthenticateOBU buildIAVAuthenticateOBU(Gen2.Denatran.IAV.AuthenticateOBU tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVAuthenticateOBU authenticateOBU = new ThingMagicDenatranIAVAuthenticateOBU();

        //Set IAVCommandRequest
        authenticateOBU.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return authenticateOBU;
    }

    private ThingMagicDenatranIAVOBUAuthenticateID buildIAVOBUAuthenticateID(Gen2.Denatran.IAV.OBUAuthID tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVOBUAuthenticateID authenticateID = new ThingMagicDenatranIAVOBUAuthenticateID();

        //Set IAVCommandRequest
        authenticateID.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return authenticateID;
    }

    private ThingMagicDenatranIAVOBUAuthenticateFullPass1 buildIAVOBUAuthenticateFullPass1(Gen2.Denatran.IAV.OBUAuthFullPass1 tagOp) throws ReaderException
    {

        ThingMagicDenatranIAVOBUAuthenticateFullPass1 authenticateFullPass1 = new ThingMagicDenatranIAVOBUAuthenticateFullPass1();

        //Set IAVCommandRequest
        authenticateFullPass1.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return authenticateFullPass1;

    }

    private ThingMagicDenatranIAVOBUAuthenticateFullPass2 buildIAVOBUAuthenticateFullPass2(Gen2.Denatran.IAV.OBUAuthFullPass2 tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVOBUAuthenticateFullPass2 authenticateFullPass2 = new ThingMagicDenatranIAVOBUAuthenticateFullPass2();

        //Set IAVCommandRequest
        authenticateFullPass2.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return authenticateFullPass2;
    }

    private ThingMagicDenatranIAVOBUReadFromMemMap buildIAVOBUReadFromMemMap(Gen2.Denatran.IAV.OBUReadFromMemMap tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVOBUReadFromMemMap readFromMemMap = new ThingMagicDenatranIAVOBUReadFromMemMap();

        //Set IAVCommandRequest
        readFromMemMap.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return readFromMemMap;
    }

    private ThingMagicDenatranIAVOBUWriteToMemMap buildIAVOBUWriteToMemMap(Gen2.Denatran.IAV.OBUWriteToMemMap tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVOBUWriteToMemMap writeToMemMap = new ThingMagicDenatranIAVOBUWriteToMemMap();

        //Set IAVCommandRequest
        writeToMemMap.setThingMagicDenatranIAVCommandRequest(initIAVCommandRequest(tagOp));

        return writeToMemMap;
    }
    public ThingMagicDenatranIAVCommandRequest initIAVCommandRequest(Gen2.Denatran.IAV tagOp) throws ReaderException
    {
        ThingMagicDenatranIAVCommandRequest commandRequest = new ThingMagicDenatranIAVCommandRequest();
        commandRequest.setPayLoad(new UnsignedByte(tagOp.payload));
        commandRequest.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));
        return commandRequest;
    }

    private ThingMagicNXPAuthentication buildNXPAESAuthenticate(Gen2.NXP.AES.Authenticate tagOp) throws ReaderException
    {
        //Construct and initialize ThingMagicNXPAuthentication
        ThingMagicNXPAuthentication nxpAuthentication = new ThingMagicNXPAuthentication();

        //Set cmdRequest to ThingMagicNXPAuthentication
        nxpAuthentication.setThingMagicNXPCommandRequest(initNXPCommandRequest());

        //Set subcommand
        nxpAuthentication.setsubCommand(new UnsignedByte(tagOp.subCommand));

        ThingMagicCustomNXPAuthenticationType authType;

        //Set authType to TAM1/TAM2 and set authType to ThingMagicNXPAuthentication
        authType = new ThingMagicCustomNXPAuthenticationType(tagOp.authenticateType.option);
        nxpAuthentication.setAuthType(authType);

        if(tagOp.authenticateType == Gen2.NXP.AES.AuthType.TAM1)
        {

            //TAM1 Authentication Data
            ThingMagicNXPTAM1AuthenticationData tam1AuthData = new ThingMagicNXPTAM1AuthenticationData();

            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            tam1Auth = tagOp.tam1;

            tam1AuthData.setAuthentication(new UnsignedByte(tam1Auth.authentication));
            tam1AuthData.setCSI(new UnsignedByte(tam1Auth.csi));
            tam1AuthData.setkeyID(new UnsignedByte(tam1Auth.keyId));
            tam1AuthData.setKeyLength(new UnsignedByte(tam1Auth.keyLength));
            tam1AuthData.setKey(new UnsignedByteArray(tam1Auth.key));

            nxpAuthentication.setThingMagicNXPTAM1AuthenticationData(tam1AuthData);
        }
        else if(tagOp.authenticateType == Gen2.NXP.AES.AuthType.TAM2)
        {
 
            //TAM2 Authentication Data
            ThingMagicNXPTAM2AuthenticationData tam2AuthData = new ThingMagicNXPTAM2AuthenticationData();

            Gen2.NXP.AES.Tam2Authentication tam2Auth;
            tam2Auth = tagOp.tam2;

            //TAM1 Authentication Data
            ThingMagicNXPTAM1AuthenticationData tam1AuthenticationData = new ThingMagicNXPTAM1AuthenticationData();

            tam1AuthenticationData.setAuthentication(new UnsignedByte(tam2Auth.authentication));
            tam1AuthenticationData.setCSI(new UnsignedByte(tam2Auth.csi));
            tam1AuthenticationData.setkeyID(new UnsignedByte(tam2Auth.keyId));
            tam1AuthenticationData.setKeyLength(new UnsignedByte(tam2Auth.keyLength));
            tam1AuthenticationData.setKey(new UnsignedByteArray(tam2Auth.key));

            tam2AuthData.setThingMagicNXPTAM1AuthenticationData(tam1AuthenticationData);
            tam2AuthData.setoffset(new UnsignedShort(tam2Auth.offset));
            tam2AuthData.setProtMode(new UnsignedByte(tam2Auth.protMode));
            tam2AuthData.setBlockCount(new UnsignedByte(tam2Auth.blockCount));
            tam2AuthData.setProfile(new ThingMagicNXPProfileType(tam2Auth.profile.option));

            nxpAuthentication.setThingMagicNXPTAM2AuthenticationData(tam2AuthData);
        }
        return nxpAuthentication;
    }
    
    private ThingMagicNXPReadbuffer buildNXPAESReadBuffer(Gen2.NXP.AES.ReadBuffer tagOp) throws ReaderException
    {
        //Construct and initialize ThingMagicNXPReadBuffer
        ThingMagicNXPReadbuffer nxpReadBuffer = new ThingMagicNXPReadbuffer();

        //Set cmdRequest to ThingMagicNXPReadBuffer
        nxpReadBuffer.setThingMagicNXPCommandRequest(initNXPCommandRequest());

        //Set subcommand, wordPointer, bitCount
        nxpReadBuffer.setsubCommand(new UnsignedByte(tagOp.subCommand));
        nxpReadBuffer.setwordPointer(new UnsignedShort(tagOp.wordPointer));
        nxpReadBuffer.setbitCount(new UnsignedShort(tagOp.bitCount));

        ThingMagicCustomNXPAuthenticationType authType;

        //Set authType to TAM1/TAM2 and set authType to ThingMagicNXPReadBuffer
        authType = new ThingMagicCustomNXPAuthenticationType(tagOp.authenticateType.option);
        nxpReadBuffer.setAuthtype(authType);

        if(tagOp.authenticateType == Gen2.NXP.AES.AuthType.TAM1)
        {

            //TAM1 Authentication Data
            ThingMagicNXPTAM1AuthenticationData tam1AuthData = new ThingMagicNXPTAM1AuthenticationData();

            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            tam1Auth = tagOp.tam1;

            tam1AuthData.setAuthentication(new UnsignedByte(tam1Auth.authentication));
            tam1AuthData.setCSI(new UnsignedByte(tam1Auth.csi));
            tam1AuthData.setkeyID(new UnsignedByte(tam1Auth.keyId));
            tam1AuthData.setKeyLength(new UnsignedByte(tam1Auth.keyLength));
            tam1AuthData.setKey(new UnsignedByteArray(tam1Auth.key));

            nxpReadBuffer.setThingMagicNXPTAM1AuthenticationData(tam1AuthData);
        }
        else if(tagOp.authenticateType == Gen2.NXP.AES.AuthType.TAM2)
        {
 
            //TAM2 Authentication Data
            ThingMagicNXPTAM2AuthenticationData tam2AuthData = new ThingMagicNXPTAM2AuthenticationData();

            Gen2.NXP.AES.Tam2Authentication tam2Auth;
            tam2Auth = tagOp.tam2;

            //TAM1 Authentication Data
            ThingMagicNXPTAM1AuthenticationData tam1AuthenticationData = new ThingMagicNXPTAM1AuthenticationData();

            tam1AuthenticationData.setAuthentication(new UnsignedByte(tam2Auth.authentication));
            tam1AuthenticationData.setCSI(new UnsignedByte(tam2Auth.csi));
            tam1AuthenticationData.setkeyID(new UnsignedByte(tam2Auth.keyId));
            tam1AuthenticationData.setKeyLength(new UnsignedByte(tam2Auth.keyLength));
            tam1AuthenticationData.setKey(new UnsignedByteArray(tam2Auth.key));

            tam2AuthData.setThingMagicNXPTAM1AuthenticationData(tam1AuthenticationData);
            tam2AuthData.setoffset(new UnsignedShort(tam2Auth.offset));
            tam2AuthData.setProtMode(new UnsignedByte(tam2Auth.protMode));
            tam2AuthData.setBlockCount(new UnsignedByte(tam2Auth.blockCount));
            tam2AuthData.setProfile(new ThingMagicNXPProfileType(tam2Auth.profile.option));

            nxpReadBuffer.setThingMagicNXPTAM2AuthenticationData(tam2AuthData);
        }
        return nxpReadBuffer;
    }
    
    private ThingMagicNXPUntraceable buildNXPAESUntraceable(Gen2.NXP.AES.Untraceable tagOp) throws ReaderException
    {
        //Construct and initialize ThingMagicNXPUntraceable
        ThingMagicNXPUntraceable nxpUntraceable = new ThingMagicNXPUntraceable();

        //Set cmdRequest to ThingMagicNXPReadBuffer
        nxpUntraceable.setThingMagicNXPCommandRequest(initNXPCommandRequest());

        // Set ThingMagicNXPUntraceableAuthentication
        ThingMagicNXPUntraceableAuthentication untraceAuth = new ThingMagicNXPUntraceableAuthentication();

        ThingMagicCustomNXPUntraceableAuthType untraceType;
        Gen2.NXP.AES.Untraceable untraceable = tagOp;

        //Set authType to TAM1/TAM2 and set authType to ThingMagicNXPReadBuffer
        untraceType = new ThingMagicCustomNXPUntraceableAuthType(tagOp.untraceType.option);
        untraceAuth.setAuthType(untraceType);

        if(tagOp.untraceType == Gen2.NXP.AES.Untraceable.UntraceType.UNTRACEABLE_WITH_AUTHENTICATION)
        {
            //TAM1 Authentication Data
            ThingMagicNXPTAM1AuthenticationData tam1AuthData = new ThingMagicNXPTAM1AuthenticationData();

            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            tam1Auth = tagOp.auth;

            tam1AuthData.setAuthentication(new UnsignedByte(tam1Auth.authentication));
            tam1AuthData.setCSI(new UnsignedByte(tam1Auth.csi));
            tam1AuthData.setkeyID(new UnsignedByte(tam1Auth.keyId));
            tam1AuthData.setKeyLength(new UnsignedByte(tam1Auth.keyLength));
            tam1AuthData.setKey(new UnsignedByteArray(tam1Auth.key));

            untraceAuth.setThingMagicNXPTAM1AuthenticationData(tam1AuthData);
        }

        //Set accessPassword
        untraceAuth.setaccessPassword(new UnsignedInteger(untraceable.aes.accessPassword));

        //Set ThingMagicNXPUntraceableAuthentication
        nxpUntraceable.setThingMagicNXPUntraceableAuthentication(untraceAuth);

        ThingMagicCustomNXPUntraceableEPC epc = new ThingMagicCustomNXPUntraceableEPC(untraceable.epc.option);
        nxpUntraceable.setEpc(epc);
        nxpUntraceable.setepcLength(new UnsignedInteger(untraceable.epcLen));

        ThingMagicCustomNXPUntraceableTID tid = new ThingMagicCustomNXPUntraceableTID(untraceable.tid.option);
        nxpUntraceable.setTid(tid);

        ThingMagicCustomNXPUntraceableUserMemory user = new ThingMagicCustomNXPUntraceableUserMemory(untraceable.user.option);
        nxpUntraceable.setUserMemory(user);

        ThingMagicCustomNXPUntraceableRange range = new ThingMagicCustomNXPUntraceableRange(untraceable.range.option);
        nxpUntraceable.setRange(range);

        nxpUntraceable.setsubCommand(new UnsignedByte(tagOp.subCommand));

        return nxpUntraceable;
    }

    //Initialize ThingMagicNXPCommandRequest
    public ThingMagicNXPCommandRequest initNXPCommandRequest() throws ReaderException
    {
        ThingMagicNXPCommandRequest commandRequest = new ThingMagicNXPCommandRequest();

        //Set accessPassword to ThingMagicNXPCommandRequest
        commandRequest.setAccessPassword(getAccessPassword());

        //Set OpSpecId to ThingMagicNXPCommandRequest
        commandRequest.setOpSpecID(new UnsignedShort(getNextOpSpecId(opSpecId)));

        return commandRequest;
    }

    // Convert DateTime to SL900A time
    public static int toSL900aTime(Calendar calendar) throws ReaderException
    {
        int t32 = 0;
        int year = Calendar.YEAR;
        if(0 <= (calendar.get(year) - 2010))
        {
            t32 |= (calendar.get(year) - 2010) << 26;
        }
        else
        {
           throw new ReaderException("Year must be >= 2010: " + calendar.get(year));
        }
        calendar.add(Calendar.MONTH , 1);
        t32 |= calendar.get(Calendar.MONTH) << 22;
        t32 |= calendar.get(Calendar.DAY_OF_MONTH) << 17;
        t32 |= calendar.get(Calendar.HOUR_OF_DAY) << 12;
        t32 |= calendar.get(Calendar.MINUTE) << 6;
        t32 |= calendar.get(Calendar.SECOND);
        return t32;
    }

    private UnsignedInteger getAccessPassword() throws ReaderException
    {
        //AccessPassword
        Gen2.Password accPass = (Gen2.Password)paramGet(TMR_PARAM_GEN2_ACCESSPASSWORD);
        return new UnsignedInteger(new Integer(accPass.value));
    }

    public static int[] parseValue(String value)
    {
        int[] action;
        initLockActionNameToLLRPAction();

        if (lockActionNameToLLRPAction.containsKey(value))
        {
            action = lockActionNameToLLRPAction.get(value);
        }
        else
        {
            throw new IllegalArgumentException("Unknown value " + value);
        }
        return action;
    }

    private int getNextOpSpecId(int opSpecId)
    {
        Object opSpecIdLock = new Object();
        synchronized(opSpecIdLock)
        {
            return  ++opSpecId;
        }
    }
    private static Map<String, int[]> lockActionNameToLLRPAction;

    private static void initLockActionNameToLLRPAction()
    {
        if (lockActionNameToLLRPAction != null)
        {
            return;
        }

        lockActionNameToLLRPAction = new HashMap<String, int[]>(20);
        lockActionNameToLLRPAction.put("EPC_LOCK", new int[]{C1G2LockDataField.EPC_Memory, C1G2LockPrivilege.Read_Write});
        lockActionNameToLLRPAction.put("EPC_UNLOCK", new int[]{C1G2LockDataField.EPC_Memory, C1G2LockPrivilege.Unlock});
        lockActionNameToLLRPAction.put("EPC_PERMALOCK", new int[]{C1G2LockDataField.EPC_Memory, C1G2LockPrivilege.Perma_Lock});
        lockActionNameToLLRPAction.put("EPC_PERMAUNLOCK", new int[]{C1G2LockDataField.EPC_Memory, C1G2LockPrivilege.Perma_Unlock});

        lockActionNameToLLRPAction.put("TID_LOCK", new int[]{C1G2LockDataField.TID_Memory, C1G2LockPrivilege.Read_Write});
        lockActionNameToLLRPAction.put("TID_UNLOCK", new int[]{C1G2LockDataField.TID_Memory, C1G2LockPrivilege.Unlock});
        lockActionNameToLLRPAction.put("TID_PERMALOCK", new int[]{C1G2LockDataField.TID_Memory, C1G2LockPrivilege.Perma_Lock});
        lockActionNameToLLRPAction.put("TID_PERMAUNLOCK", new int[]{C1G2LockDataField.TID_Memory, C1G2LockPrivilege.Perma_Unlock});

        lockActionNameToLLRPAction.put("USER_LOCK", new int[]{C1G2LockDataField.User_Memory, C1G2LockPrivilege.Read_Write});
        lockActionNameToLLRPAction.put("USER_UNLOCK", new int[]{C1G2LockDataField.User_Memory, C1G2LockPrivilege.Unlock});
        lockActionNameToLLRPAction.put("USER_PERMALOCK", new int[]{C1G2LockDataField.User_Memory, C1G2LockPrivilege.Perma_Lock});
        lockActionNameToLLRPAction.put("USER_PERMAUNLOCK", new int[]{C1G2LockDataField.User_Memory, C1G2LockPrivilege.Perma_Unlock});

        lockActionNameToLLRPAction.put("ACCESS_LOCK", new int[]{C1G2LockDataField.Access_Password, C1G2LockPrivilege.Read_Write});
        lockActionNameToLLRPAction.put("ACCESS_UNLOCK", new int[]{C1G2LockDataField.Access_Password, C1G2LockPrivilege.Unlock});
        lockActionNameToLLRPAction.put("ACCESS_PERMALOCK", new int[]{C1G2LockDataField.Access_Password, C1G2LockPrivilege.Perma_Lock});
        lockActionNameToLLRPAction.put("ACCESS_PERMAUNLOCK", new int[]{C1G2LockDataField.Access_Password, C1G2LockPrivilege.Perma_Unlock});

        lockActionNameToLLRPAction.put("KILL_LOCK", new int[]{C1G2LockDataField.Kill_Password, C1G2LockPrivilege.Read_Write});
        lockActionNameToLLRPAction.put("KILL_UNLOCK", new int[]{C1G2LockDataField.Kill_Password, C1G2LockPrivilege.Unlock});
        lockActionNameToLLRPAction.put("KILL_PERMALOCK", new int[]{C1G2LockDataField.Kill_Password, C1G2LockPrivilege.Perma_Lock});
        lockActionNameToLLRPAction.put("KILL_PERMAUNLOCK", new int[]{C1G2LockDataField.Kill_Password, C1G2LockPrivilege.Perma_Unlock});
    }

    // Add the AccessSpec to the reader.
    public boolean addAccessSpec(AccessSpec accessSpec) throws ReaderException
    {
        ADD_ACCESSSPEC_RESPONSE response;        
        log("Adding the AccessSpec.");
        ADD_ACCESSSPEC accessSpecMsg = new ADD_ACCESSSPEC();
        accessSpecMsg.setAccessSpec(accessSpec);
        response = (ADD_ACCESSSPEC_RESPONSE) LLRP_SendReceive(accessSpecMsg);
        return getStatusFromStatusCode(response.getLLRPStatus());
    }

    /**
     * Enable the AccessSpec
     * @param accessSpecID
     * @return boolean
     * @throws ReaderException
     */
    private boolean enableAccessSpec(int accessSpecID) throws ReaderException
    {
        ENABLE_ACCESSSPEC_RESPONSE response;
        log("Enabling the AccessSpec.");
        ENABLE_ACCESSSPEC enable = new ENABLE_ACCESSSPEC();
        enable.setAccessSpecID(new UnsignedInteger(accessSpecID));
        response = (ENABLE_ACCESSSPEC_RESPONSE)LLRP_SendReceive(enable);
        return getStatusFromStatusCode(response.getLLRPStatus());
    }

    private boolean getStatusFromStatusCode(LLRPStatus status) throws ReaderException
    {
        int statusCode = status.getStatusCode().intValue();
        String errorDescription = status.getErrorDescription().toString();
        if (statusCode == StatusCode.M_Success)
        {
            return true;
        }
        else
        {
            if(!standalone)
            {
                //For Embedded Operation
                notifyExceptionListeners(new ReaderException(errorDescription));
            return false;
        }
            throw new ReaderException(errorDescription);
    }
    }

    private boolean verifyROSpecEndStatus()
    {        
        for(int i=0; i< endOfROSpecFlags.length ; i++)
        {
            //return false if yet to receive any event
            if(!endOfROSpecFlags[i])
            {
                return false;
            }
        }
        // Received END OF ROSPEC event for all ROSPECS initiated
        endOfROSpec = true;
        return true;
    }

     // Disable the ROSpec.
    private boolean disableROSpec(int ROSPEC_ID) throws ReaderException
    {
        DISABLE_ROSPEC_RESPONSE response;

        log("Disabling the ROSpec : " + ROSPEC_ID);
        DISABLE_ROSPEC enable = new DISABLE_ROSPEC() ;
        enable.setROSpecID(new UnsignedInteger(ROSPEC_ID));
        try
        {
            response = (DISABLE_ROSPEC_RESPONSE) LLRP_SendReceive(enable, STOP_TIMEOUT + commandTimeout + transportTimeout);
            return getStatusFromStatusCode(response.getLLRPStatus());
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }
    }

    /**
     * Overloaded stopROSpec method
     * @param ROSPEC_ID
     * @throws ReaderException
     */
    private void stopROSpec(UnsignedInteger ROSPEC_ID) throws ReaderException
    {
        STOP_ROSPEC_RESPONSE response;
        log("Stopping the ROSpec : " + ROSPEC_ID);
        STOP_ROSPEC stop = new STOP_ROSPEC();
        stop.setROSpecID(ROSPEC_ID);
        try
        {
            response = (STOP_ROSPEC_RESPONSE) LLRP_SendReceive(stop, STOP_TIMEOUT + commandTimeout + transportTimeout);
            if(null != response && response.getLLRPStatus().getStatusCode().intValue() == StatusCode.M_Success)
            {
                log("Success " + response.toString());
            }
            else
            {
                if(response!= null){
            		 log("Failure " + response.toString());   
            	}                
            }
        }
        catch (ReaderCommException rce)
        {
            throw new ReaderException(rce.getMessage());
        }
    }
    /**
     * stopping RO Spec
     * @param ROSPEC_ID
     * @throws ReaderException
     */
    public void stopROSpec(int ROSPEC_ID) throws ReaderException
    {
         stopROSpec(new UnsignedInteger(ROSPEC_ID));
    }
       
    /**
     * transact llrp message with timeout
     * @param message
     * @param timeout
     * @return LLRPMessage
     * @throws ReaderCommException
     */
    private LLRPMessage LLRP_SendReceive(LLRPMessage message, int timeout) throws ReaderCommException, ReaderException
    {
        if(readerConn!=null)
        {
            LLRPMessage response = null;
            try
            {
                notifyTransportListeners(message, true, 1000);
                //Update msgStartTime before sending the message
                msgStartTime = System.currentTimeMillis();
                // Store the message sent here
                msgSent = message;
                //Reset the flag before sending the message 
                isMsgRespReceived = false;
                response = readerConn.transact(message, timeout);
                if(null != response)
                {
                    //validate the received message with sent
                    isMsgRespReceived = sentReceiveMessageValidator(msgSent, response);
                    notifyTransportListeners(response, false, 1000);
                }
                return response;
            }
            catch (TimeoutException ex)
            {
                //wait for RESPONSE event
                /* LTK message timeout is too low-level.  Wrap it with
                 * our own sequence-aware timeout logic */
                while(!isMsgRespReceived)
                {
                    TimeoutExceptionLogicTimerTask(timeout, ex);
                    try
                    {
                        //Give other threads a chance to run. (i.e., avoid taking 100% CPU)
                        Thread.sleep(1);
                    } catch (InterruptedException e) {
                        llrpLogger.error(e.getMessage());
                    }
                }
                return null;
            }
        }
        else
        {            
            return null;
        }
    }

    /**
     * Check high-level timeout.
     * @note msgStartTime is a global which is modified by other functions, too.
     * Any time an LLRP message is sent or received, msgStartTime is updated
     * to the current time, extending the timeout deadline.
     * @param timeout - timeout in ms
     * @param ex - timeout exception message 
     * @throws ReaderCommException
     */
    public void TimeoutExceptionLogicTimerTask(int timeout, TimeoutException ex) throws ReaderCommException, ReaderException
    {
        long msgCurrentTime = System.currentTimeMillis();
        long diff = msgCurrentTime - msgStartTime;
        if(diff > timeout)
        {
            throw new ReaderCommException(ex.getMessage());
        }
    }

    /**
     * Validates received message with the sent message
     * @param sentMsg - Message sent 
     * @param recMesg - Message received
     * @returns true/false.
     */
    public boolean sentReceiveMessageValidator(LLRPMessage sentMsg, LLRPMessage recMesg)
    {
       if(recMesg.getName().contains(sentMsg.getName()))
       {
          return true;
       }
       else
       {
          return false;
       }
    }

    /**
     * transact llrp message with timeout
     * @param message
     * @param timeout
     * @return LLRPMessage
     * @throws ReaderCommException
     */
    private void LLRP_Send(LLRPMessage message) throws ReaderCommException, ReaderException
    {
        if(readerConn!=null)
        {
            notifyTransportListeners(message, true, 1000);
            readerConn.send(message);
        }
    }

    /**
     * notifying transport listeners
     * @param msg
     * @param tx
     * @param timeout
     * @throws ReaderException
     */
    protected void notifyTransportListeners(LLRPMessage msg, boolean tx, int timeout) throws ReaderException
    {
        if(hasLLRPListeners)
        {
            try
            {
                byte[] byteMsg = msg.toXMLString().getBytes();
                msg.toHexString().getBytes();
                for (TransportListener l : _llrpListeners)
                {
                    l.message(tx, byteMsg, timeout);
                }
            }
            catch (InvalidLLRPMessageException ex)
            {
                throw new ReaderException(ex.getMessage());
            }
        }        
    }

    /**
     * transact llrp message with standard timeout
     * @param message
     * @return LLRPMessage
     * @throws ReaderCommException
     */
    private LLRPMessage LLRP_SendReceive(LLRPMessage message) throws ReaderException
    {
        return LLRP_SendReceive(message, commandTimeout + transportTimeout);
    }

    private static class RFMode
    {
        private String ePCHAGTCConformance;
        private String mValue;
        private String forwardLinkModulation;
        private String spectralMaskIndicator;
        private String bDRValue;
        private String pIEValue;
        private String minTariValue;
        private String maxTariValue;
        private String stepTariValue;
        private String dRValue;

        public String getbDRValue() {
            return bDRValue;
        }

        public void setbDRValue(String bDRValue) {
            this.bDRValue = bDRValue;
        }

        public String getdRValue() {
            return dRValue;
        }

        public void setdRValue(String dRValue) {
            this.dRValue = dRValue;
        }

        public String getePCHAGTCConformance() {
            return ePCHAGTCConformance;
        }

        public void setePCHAGTCConformance(String ePCHAGTCConformance) {
            this.ePCHAGTCConformance = ePCHAGTCConformance;
        }

        public String getForwardLinkModulation() {
            return forwardLinkModulation;
        }

        public void setForwardLinkModulation(String forwardLinkModulation) {
            this.forwardLinkModulation = forwardLinkModulation;
        }

        public String getmValue() {
            return mValue;
        }

        public void setmValue(String mValue) {
            this.mValue = mValue;
        }

        public String getMaxTariValue() {
            return maxTariValue;
        }

        public void setMaxTariValue(String maxTariValue) {
            this.maxTariValue = maxTariValue;
        }

        public String getMinTariValue() {
            return minTariValue;
        }

        public void setMinTariValue(String minTariValue) {
            this.minTariValue = minTariValue;
        }

        public String getpIEValue() {
            return pIEValue;
        }

        public void setpIEValue(String pIEValue) {
            this.pIEValue = pIEValue;
        }

        public String getSpectralMaskIndicator() {
            return spectralMaskIndicator;
        }

        public void setSpectralMaskIndicator(String spectralMaskIndicator) {
            this.spectralMaskIndicator = spectralMaskIndicator;
        }

        public String getStepTariValue() {
            return stepTariValue;
        }

        public void setStepTariValue(String stepTariValue) {
            this.stepTariValue = stepTariValue;
        }        
    }
    private static class TM_GET_READER_CONFIG extends GET_READER_CONFIG
    {
        /** LTK's GET_READER_CONFIG is not initializing all fields implicitly
         *  So, initializing to 0 in LLRPReader's customized class.
         */
        TM_GET_READER_CONFIG()
        {
            setAntennaID(new UnsignedShort(0));
            setGPIPortNum(new UnsignedShort(0));
            setGPOPortNum(new UnsignedShort(0));
        }
    }


    


    @Override
    public byte[] readTagMemBytes(TagFilter target, int bank, int address, int count) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public short[] readTagMemWords(TagFilter target, int bank, int address, int count) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void writeTagMemBytes(TagFilter target, int bank, int address, byte[] data) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void writeTagMemWords(TagFilter target, int bank, int address, short[] data) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void writeTag(TagFilter target, TagData newID) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void lockTag(TagFilter target, TagLockAction lock) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void killTag(TagFilter target, TagAuthentication auth) throws ReaderException {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void startReading()
    {
        continuousReading = true;
        // reverting the shared variables
        roSpecId = 0;
        _roSpecList = new ArrayList<ROSpec>();
        mapRoSpecIdToProtocol = new HashMap<Integer, TagProtocol>();
        try
        {
            enableEventsAndReports();
            startBackgroundParser();
            ReadPlan rp = (ReadPlan) paramGet(TMR_PARAM_READ_PLAN);
            deleteROSpecs();
            deleteAccessSpecs();
            if(rp instanceof MultiReadPlan)
            {
                int asyncOnTime = (Integer)paramGet(TMR_PARAM_READ_ASYNCONTIME);
                buildROSpec(rp, asyncOnTime, _roSpecList);
            }
            else
            {
                buildROSpec(rp, 0, _roSpecList);
            }
            for(ROSpec roSpec : _roSpecList)
            {
                if(addROSpec(roSpec))
                {
                    if(enableROSpec(roSpec.getROSpecID().intValue()))
                    {
                        if(roSpec.getROBoundarySpec().getROSpecStartTrigger().getROSpecStartTriggerType().intValue() != ROSpecStartTriggerType.Periodic)
                        {
                            if(!startROSpec(roSpec.getROSpecID().intValue()))
                            {
                                return;
                            }
                        }
                    }//end of enableROSpec success validation
                }//end of addROSpec success validation
            }//end of for loop - looping all ROSpecs
                    }
        catch(ReaderException e)
        {
            e.printStackTrace();
        }
    }

    @Override
    public boolean stopReading()
    {
        try
        {
            if(null != _roSpecList && !_roSpecList.isEmpty())
            {
            // sending stop ROSpec
            for(ROSpec roSpec : _roSpecList)
            {
                if(roSpec.getROBoundarySpec().getROSpecStartTrigger().getROSpecStartTriggerType().intValue() != ROSpecStartTriggerType.Periodic)
                {
                    stopROSpec(roSpec.getROSpecID().intValue());
                }
                else
                {
                    disableROSpec(roSpec.getROSpecID().intValue());
                }
            }
        }
        }
        catch(ReaderException e)
        {
            e.printStackTrace();
            return false;
        }
        finally
        {
            stopBackgroundParser();
            continuousReading = false;
            setNumPlans(0);
        }
        return true;
    }


    /**
     * This method is only for Query Application in Web UI.
     * When Applet is stopped, to make cleanup fast this method is used
     */
    public void queryStopReading()
    {
        try
        {
            if(null != _roSpecList && !_roSpecList.isEmpty())
            {
                // sending stop ROSpec
                for(ROSpec roSpec : _roSpecList)
                {
                    if(roSpec.getROBoundarySpec().getROSpecStartTrigger().getROSpecStartTriggerType().intValue() != ROSpecStartTriggerType.Periodic)
                    {
                        STOP_ROSPEC stop = new STOP_ROSPEC();
                        stop.setROSpecID(roSpec.getROSpecID());
                        LLRP_Send(stop);
                    }
                    else
                    {
                        DISABLE_ROSPEC disable = new DISABLE_ROSPEC();
                        disable.setROSpecID(roSpec.getROSpecID());
                        LLRP_Send(disable);
                    }
                }
            }
        }
        catch(ReaderException e)
        {
            e.printStackTrace();
        }
        finally
        {
            stopRequested = true;
            tagProcessor = null;
            bkgThread = null;
            continuousReading = false;
        }
    }

    @Override
    public GpioPin[] gpiGet() throws ReaderException {
        GET_READER_CONFIG_RESPONSE response = getReaderConfigResponse(GetReaderConfigRequestedData.GPIPortCurrentState);
        ArrayList<GpioPin> list = new ArrayList<GpioPin>();
        List<GPIPortCurrentState> stateList = response.getGPIPortCurrentStateList();
        if (null != stateList)
        {
            for (GPIPortCurrentState state : stateList )
            {
                int id = llrpToTmGpi(state.getGPIPortNum().intValue());
                boolean high = (GPIPortState.High == state.getState().toInteger());
                GpioPin pin = new GpioPin(id, high);
                list.add(pin);
    }
        }
        return list.toArray(new GpioPin[0]);
    }

    int llrpToTmGpi(int llrpNum) throws ReaderParseException
    {
        int index = llrpNum - 1;
        if ((llrpNum < 0) || (gpiList.length <= index))
        {
            throw new ReaderParseException("Invalid LLRP GpiPortNum: " + llrpNum);
        }
        return gpiList[llrpNum - 1];
    }

    @Override
    public void gpoSet(GpioPin[] state) throws ReaderException {
        SET_READER_CONFIG msg = new SET_READER_CONFIG();
        msg.setResetToFactoryDefault(new Bit(0));
        ArrayList<GPOWriteData> list = new ArrayList<GPOWriteData>();
        if (null != state)
        {
            for (GpioPin pin : state)
            {
                GPOWriteData data = new GPOWriteData();
                int llrpNum = tmToLlrpGpo(pin.id);
                data.setGPOPortNumber(new UnsignedShort(llrpNum));
                data.setGPOData(new Bit(pin.high?1:0));
                list.add(data);
    }
        }
        msg.setGPOWriteDataList(list);
        LLRP_SendReceive(msg);
    }

    private Map<Integer, Integer> _tmToLlrpGpoMap = null;
    int tmToLlrpGpo(int tmNum)
    {
        if (!getTmToLlrpGpoMap().containsKey(tmNum))
        {
            throw new IllegalArgumentException("Invalid GPO Number: " + tmNum);
        }
        return getTmToLlrpGpoMap().get(tmNum);
    }
    private Map<Integer,Integer> getTmToLlrpGpoMap()
    {
        if (null == _tmToLlrpGpoMap)
        {
            int llrpNum = 1;
            Map<Integer, Integer> map = _tmToLlrpGpoMap = new HashMap<Integer, Integer>();
            for (int tmNum : gpoList)
            {
                map.put(tmNum, llrpNum);
                llrpNum++;
            }
        }
        return _tmToLlrpGpoMap;
    }

    @Override
    public Object executeTagOp(TagOp tagOp, TagFilter target) throws ReaderException
    {
        /*
         * Though its a standalone tag operation, From server point of view
         * we need to submit requests in the following order.
         *
         * 1. Reset reader
         * 2. Add ROSpec with the filter specified
         * 3. Enable ROSpec
         * 4. Add AccessSpec
         * 5. Enable AccessSpec
         * 6. Start ROSpec
         * 7. Wait for response and verify the result
         */
        readerException = null;
        //Enable Reader Notification
        enableReaderNotification();
        startBackgroundParser();
        try
        {
            standalone = true;
            //Delete all ROSpec and AccessSpecs on the reader
            deleteAccessSpecs();
            deleteROSpecs();
            roSpecId = 0;
            accessSpecId = 0;
            opSpecId = 0;
            endOfROSpec = false;
            endOfAISpec = false;
            reportReceived = false;
            tagOpResponse = null;

            TagProtocol protocol;
             //Get the tagop protocol
            if(tagOp instanceof Iso180006b.ReadData || tagOp instanceof Iso180006b.WriteData || tagOp instanceof Iso180006b.Lock)
            {
                protocol = TagProtocol.ISO180006B;
            }
            else
            {
                protocol = (TagProtocol) paramGet(TMR_PARAM_TAGOP_PROTOCOL);
            }
            //Get the tagop antenna
            Integer antenna = (Integer) paramGet(TMR_PARAM_TAGOP_ANTENNA);

            //Prepare ROSpec
            SimpleReadPlan srp = new SimpleReadPlan(new int[]{antenna}, protocol, target, tagOp, 0);
            List<ROSpec> roSpecList = new ArrayList<ROSpec>();
            buildROSpec(srp, 0, roSpecList);
            enableROSpecFlags(roSpecList.size());

            ROSpec roSpec = roSpecList.get(0);
            if (addROSpec(roSpec) && enableROSpec(roSpec.getROSpecID().intValue()))
            {
                if (!startROSpec(roSpec.getROSpecID().intValue()))
                {
                    endOfROSpecFlags[roSpec.getROSpecID().intValue() - 1] = true;
                }
            }
            else
            {
                endOfROSpecFlags[roSpec.getROSpecID().intValue() - 1] = true;
            }
            // verify for any failures during ROSPEC enable, add or start
            verifyROSpecEndStatus();
            int timeOut = commandTimeout + transportTimeout;
            long startTime = System.currentTimeMillis();
            while (!reportReceived)
            {
                try
                {
                    Thread.sleep(10);
                }
                catch (InterruptedException ex)
                {
                    llrpLogger.error(ex.getMessage());
                }
                //wait for end of ROSpec/AISpec event
                if(readerException != null)
                {
                    throw readerException;
                }
                long diffTime = System.currentTimeMillis()-startTime;
                if(diffTime > timeOut){
                    throw new ReaderCommException("Timeout");
                }
            }
        }
        catch(ReaderException re)
        {
           throw re;
        }
        finally
        {
            standalone = false;
            stopBackgroundParser();
        }
        return tagOpResponse;
    }

    @Override
    public void firmwareLoad(InputStream firmware) throws ReaderException, IOException
    {
        webRequest(firmware, null);
    }

    @Override
    public void firmwareLoad(InputStream firmware, FirmwareLoadOptions loadOptions) throws ReaderException, IOException
    {
        webRequest(firmware, loadOptions);
    }

    public void webRequest(InputStream fwStr,FirmwareLoadOptions loadOptions)
            throws ReaderException, IOException
    {        
        // These are about to stop working                
        //readerConn = null;
        
        try
        {
            ReaderUtil.firmwareLoadUtil(fwStr, this, loadOptions);
        }
        finally
        {
            _isConnected = false;
            // Reconnect to the reader
            monitorKeepAlives.stop();
            ((LLRPConnector) readerConn).disconnect();
            int attempts = 0;
            boolean isLLRP = false;
            try{
                while(attempts <= 3){
                    isLLRP = Reader.isLLRPReader(this);
                    attempts++;
                    if(isLLRP){
                        break;
                    }else{
                        Thread.sleep(20000);
                    }
                }
                if(!isLLRP)
                {
                    throw new ReaderException("Reader Type changed...Please reconnect");
                }
            }
            catch(InterruptedException e){

            }
            connect();
        }
  }

    @Override
    public void addTransportListener(TransportListener listener)
    {
        if(null != listener)
        {
            hasLLRPListeners = true;
            _llrpListeners.add(listener);
        }
    }

    @Override
    public void removeTransportListener(TransportListener listener)
    {
        _llrpListeners.remove(listener);
        if(_llrpListeners.isEmpty())
        {
            hasLLRPListeners = false;
        }
    }

    @Override
    public void addStatusListener(StatusListener listener) {
        throw new UnsupportedOperationException("Not supported yet.");
    }

    @Override
    public void removeStatusListener(StatusListener listener) {
        throw new UnsupportedOperationException("Not supported yet.");
    }

	@Override
    public void addStatsListener(StatsListener listener) {
        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_READER_STATS))
        {
            statsListeners.add(listener);
        }
        else
        {
            throw new UnsupportedOperationException("Not supported yet.");
        }
    }

    @Override
    public void removeStatsListener(StatsListener listener) {
        throw new UnsupportedOperationException("Not supported yet.");
    }
	
    public void log(String message)
    {
        llrpLogger.debug(message);
    }


    static class SimpleTransportListener implements TransportListener
    {
        public void message(boolean tx, byte[] data, int timeout)
        {
            System.out.println((tx ? "Sending:\n" : "Receiving:\n") + new String(data));
        }
    }

    static
    {
        simpleTransportListener = new SimpleTransportListener();
    }

    protected class RFReportProcessor implements Runnable
    {
        RFSurveyReportData rfReportData;
        Reader reader;        

        public RFReportProcessor(LLRPReader readerName)
        {
            this.reader = readerName;
        }

        public void run()
        {
            while(!stopRequested)
            {                
                try
                {
                    synchronized (rfReportQueue)
                    {                                            
                        if (rfReportQueue.isEmpty())
                        {
                            rfReportQueue.wait();
                        }                                   
                    } //end of sync block
                    // Needs analysis on this addition of sleep
                    Thread.sleep(20);
                    if(!rfReportQueue.isEmpty())
                    {
                        rfReportData = rfReportQueue.take();
                        processRFData(rfReportData);
                    }
                } //end of infinite while loop
                catch (InterruptedException ex)
                {
                    // do nothing
                }
                catch (Exception ex)
                {
                    llrpLogger.error(ex.getMessage());
                }                
            }//end of infinite while loop
        }

        public void parseOff()
        {
            synchronized(rfReportQueue)
            {
                try
                {
                    while(rfReportQueue.isEmpty() == false)
                    {
                        rfReportData = rfReportQueue.take();
                        processRFData(rfReportData);
                    }
                }
                catch (InterruptedException ex)
                {
                    // do nothing
                }
                catch (Exception ex)
                {
                    llrpLogger.error(ex.getMessage());
                }
            }//end of sync block            
        }
        
        public void processRFData(RFSurveyReportData rfReportData)
        {
            SerialReader.ReaderStats statValues = new SerialReader.ReaderStats();
            boolean allstats = false;
            if(rfReportData!=null)
            {
                Custom customRFData = rfReportData.getCustomList().get(0);
                CustomStatsValue customRFStats = new CustomStatsValue(customRFData);
                statValues = processCustomStats(statValues, customRFStats, allstats);
            }
            if(continuousReading)
            {
               notifyStatsListeners(statValues);
            }
        }
    }
    
    protected class TagProcessor implements Runnable
    {
        TagReportData ltkTagData;
        Reader reader;        

        public TagProcessor(LLRPReader readerName)
        {
            this.reader = readerName;
        }

        public void run()
        {
            while(!stopRequested)
            {                
                try
                {
                    synchronized (tagReportQueue)
                    {                                            
                        if (tagReportQueue.isEmpty())
                        {
                            tagReportQueue.wait();
                        }                                   
                    } //end of sync block
                    // Needs analysis on this addition of sleep
                    if(isStopNTags)
                    {
                        Thread.sleep(20);
                    }
                    if(!tagReportQueue.isEmpty())
                    {
                        ltkTagData = tagReportQueue.take();
                        processData(ltkTagData);
                    }
                } //end of infinite while loop
                catch (InterruptedException ex)
                {
                    // do nothing
                }
                catch (Exception ex)
                {
                    llrpLogger.error(ex.getMessage());
                }                
            }//end of infinite while loop
        }//end of thread run method

        public void parseOff()
        {
            synchronized(tagReportQueue)
            {
                try
                {
                    while(tagReportQueue.isEmpty() == false)
                    {
                        ltkTagData = tagReportQueue.take();
                        processData(ltkTagData);
                    }
                }
                catch (InterruptedException ex)
                {
                    // do nothing
                }
                catch (Exception ex)
                {
                    llrpLogger.error(ex.getMessage());
                }
            }//end of sync block            
        }

        BigInteger usPerMs = BigInteger.valueOf(1000);
        public void processData(TagReportData tag)
        {
            String epc = null;
            if (tag.getEPCParameter() instanceof EPCData)
            {
                epc = ((EPCData)tag.getEPCParameter()).getEPC().toString();
            }
            else
            {
                //Temporary work around for LTK JAVA issue.
                epc = ((EPC_96)tag.getEPCParameter()).getEPC().toString();

                epc = String.format("%24s",epc).replace(' ', '0');

                if(epc.equals("00"))
                {
                    epc = "000000000000000000000000";
                }
            }            

            TagReadData trData = new TagReadData();

            if(!standalone)
            {
                //Currently protocol ID is available as 2nd custom parameter.
                TagProtocol protocol = TagProtocol.NONE;
                if(!tag.getCustomList().isEmpty())
                {
                    if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
                    {
                        /**
                         * Currently protocol ID is available as 2nd custom parameter.
                         **/
                        Custom customValue = null;
                        for(Custom cVal : tag.getCustomList())
                        {
                            if(cVal instanceof ThingMagicCustomProtocolID)
                            {
                                customValue = cVal;
                            }
                            
                        }
                        //Custom customValue = tag.getCustomList().get(3);
                        if (customValue != null && customValue instanceof ThingMagicCustomProtocolID)
                        {
                            ThingMagicCustomProtocol protocolID = ((ThingMagicCustomProtocolID)customValue).getProtocolId();
                            protocol = parseThingmagicCustomProtocol(protocolID.intValue());
                        }
                    }
                    else
                    {
                        //Get the protocol using tag rospecId - to handle backward compatibility case
                        protocol = mapRoSpecIdToProtocol.get(tag.getROSpecID().getROSpecID().intValue());
                    }
                }
                /**
                 * isTagDataEmpty decides the protocol.
                 */
                boolean isTagDataEmpty = tag.getAirProtocolTagDataList().isEmpty();
                if(!isTagDataEmpty && protocol == TagProtocol.GEN2)
                {
                    short pc = ((C1G2_PC)tag.getAirProtocolTagDataList().get(0)).getPC_Bits().toShort();
                    short crc = ((C1G2_CRC)tag.getAirProtocolTagDataList().get(1)).getCRC().toShort();
                    byte[] epc1 = ReaderUtil.hexStringToByteArray(epc);
                    byte[] pc1 = ReaderUtil.shortToByteArray(pc);
                    byte[] crc1 = ReaderUtil.shortToByteArray(crc);
                    Gen2.TagData tagData = new Gen2.TagData(epc1, crc1, pc1);
                    trData.tag = tagData;
                }
                else if(isTagDataEmpty && (protocol == TagProtocol.ISO180006B))
                {
                    Iso180006b.TagData tagData = new Iso180006b.TagData(epc);
                    trData.tag = tagData;
                }
                else if(isTagDataEmpty && (protocol == TagProtocol.IPX64))
                {
                    Ipx64.TagData tagData = new Ipx64.TagData(epc);
                    trData.tag = tagData;
                }
                else if(isTagDataEmpty && (protocol == TagProtocol.IPX256))
                {
                    Ipx256.TagData tagData = new Ipx256.TagData(epc);
                    trData.tag = tagData;
                }
                else if(isTagDataEmpty && (protocol == TagProtocol.ATA))
                {
                    Ata.TagData tagData = new Ata.TagData(epc);
                    trData.tag = tagData;
                }
                else
                {
                    // NON Gen2 and ISO Tags
                    trData.tag = new TagData(epc);
                }
                trData = parseTagData(tag,trData);
            }
            List<AccessCommandOpSpecResult> opSpecResult = tag.getAccessCommandOpSpecResultList();
            if(!opSpecResult.isEmpty())
            {
                AccessCommandOpSpecResult accessCommandOpSpecResult = opSpecResult.get(0);
                try
                {
                    parseOpSpecResult(accessCommandOpSpecResult,trData);
                }
                catch(ReaderException re)
                {
                    if(standalone)
                    {
                        readerException = re;                        
                    }
                    else
                    {
                        notifyExceptionListeners(re);
                    }
                }
            }
            reportReceived = true;
            if(continuousReading)
            {
               notifyReadListeners(trData);
            }
        }

        private TagReadData parseTagData(TagReportData tag,TagReadData trData)
        {
            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ALL))
            {
                trData.antenna = tag.getAntennaID().getAntennaID().intValue();
                trData.readCount = tag.getTagSeenCount().getTagCount().intValue();
                BigInteger usSinceEpoch = tag.getLastSeenTimestampUTC().getMicroseconds().toBigInteger();
                trData.readBase = usSinceEpoch.divide(usPerMs).longValue();
                trData.readOffset = 0;
                trData.rssi = tag.getPeakRSSI().getPeakRSSI().intValue();
                trData.reader = reader;
                int channelIndex = tag.getChannelIndex().getChannelIndex().intValue();
                if(!frequencyHopTableList.isEmpty() && channelIndex != 0)
                {
                    FrequencyHopTable fHopTable = frequencyHopTableList.get(0);
                    trData.frequency = fHopTable.getFrequency().get(channelIndex - 1).toInteger();
                }
            }
            else
            {
                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.ANTENNAID))
                {
                    trData.antenna = tag.getAntennaID().getAntennaID().intValue();
                }
                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.READCOUNT))
                {
                    trData.readCount = tag.getTagSeenCount().getTagCount().intValue();
                }
                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.TIMESTAMP))
                {
                    BigInteger usSinceEpoch = tag.getLastSeenTimestampUTC().getMicroseconds().toBigInteger();
                    trData.readBase = usSinceEpoch.divide(usPerMs).longValue();
                }

                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.RSSI))
                {
                    trData.rssi = tag.getPeakRSSI().getPeakRSSI().intValue();
                }

                if(metaDataFlags.contains(TagReadData.TagMetadataFlag.FREQUENCY))
                {
                    int channelIndex = tag.getChannelIndex().getChannelIndex().intValue();
                    if(!frequencyHopTableList.isEmpty())
                    {
                        FrequencyHopTable fHopTable = frequencyHopTableList.get(0);
                        trData.frequency = fHopTable.getFrequency().get(channelIndex - 1).toInteger();
                    }
                }
            }
            trData.metadataFlags = metaDataFlags;
            trData.readOffset = 0;
            trData.reader = reader;
            // Since Spruce release firmware doesn't support phase, there won't be ThingMagicTagReportContentSelector
            // custom paramter in ROReportSpec
            StringTokenizer versionSplit = new StringTokenizer(_softwareVersion, ".");
            int length = versionSplit.countTokens();
            if(length != 0)
            {
                int productVersion = Integer.parseInt(versionSplit.nextToken());
                int buildVersion = Integer.parseInt(versionSplit.nextToken());
                if ((productVersion == 4 && buildVersion >= 17) || productVersion > 4)
                {
                    if(!tag.getCustomList().isEmpty())
                    {
                        Custom customRFValue = null;
                        Custom customGpioValue = null;
                        Custom customGen2Value = null;
                        Custom customProtocolValue = null;
                        Custom customDataValue = null;
                        for(Custom cVal : tag.getCustomList())
                        {
                            if(cVal instanceof ThingMagicRFPhase )
                            {
                                customRFValue = cVal;
                            }
                            if(cVal instanceof ThingMagicMetadataGPIO )
                            {
                                customGpioValue = cVal;
                            }
                            if(cVal instanceof ThingMagicMetadataGen2 )
                            {
                                customGen2Value = cVal;
                            }
                            if(cVal instanceof ThingMagicCustomProtocolID )
                            {
                                customProtocolValue = cVal;
                            }
                            if(cVal instanceof ThingMagicCustomTagopResponse)
                            {
                                customDataValue = cVal;
                            }
                        }
                        if (customRFValue != null && customRFValue instanceof ThingMagicRFPhase)
                        {
                            trData.phase = ((ThingMagicRFPhase) customRFValue).getPhase().intValue();
                        }
                        if (customGpioValue != null && customGpioValue instanceof ThingMagicMetadataGPIO)
                        {
                             List<GPIOStatus> gpioStatus =  ((ThingMagicMetadataGPIO) customGpioValue).getGPIOStatusList();
                             List<GpioPin> gpioPins = new ArrayList<GpioPin>();
                             for(GPIOStatus gp : gpioStatus)
                             {
                                 int id = gp.getid().toInteger();
                                 boolean status = gp.getStatus().toBoolean();
                                 int direction = gp.getDirection().intValue();
                                 GpioPin gpioPin = new GpioPin(id, status);
                                 gpioPins.add(gpioPin);
                             }
                             trData.gpio = gpioPins.toArray(new GpioPin[0]);
                        }
                        if (customGen2Value != null && customGen2Value instanceof ThingMagicMetadataGen2)
                        {
                            int gen2LFResponse = 0;
                            int gen2TargetResponse = 2;
                            int gen2QResponse = 0;
                            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_Q) && gen2QFlag)
                            {
                                gen2QResponse = ((ThingMagicMetadataGen2) customGen2Value).getGen2QResponse().getQValue().intValue();
                            }
                            else
                            {
                                metaDataFlags.remove(TagReadData.TagMetadataFlag.GEN2_Q);
                            }
                            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_LF) && gen2LFFlag)
                            {
                                gen2LFResponse = ((ThingMagicMetadataGen2) customGen2Value).getGen2LFResponse().getLFValue().intValue();
                            }
                            else
                            {
                                metaDataFlags.remove(TagReadData.TagMetadataFlag.GEN2_LF);
                            }
                            if(metaDataFlags.contains(TagReadData.TagMetadataFlag.GEN2_TARGET) && gen2TargetFlag)
                            {
                                gen2TargetResponse = ((ThingMagicMetadataGen2) customGen2Value).getGen2TargetResponse().getTargetValue().intValue();
                            }
                            else
                            {
                                metaDataFlags.remove(TagReadData.TagMetadataFlag.GEN2_TARGET);
                            }
                            
                            switch(gen2LFResponse)
                            {
                                case 250:
                                   gen2LFResponse = 0;
                                   break;
                                case 320:
                                    gen2LFResponse = 2;
                                    break;
                                case 640:
                                    gen2LFResponse = 4;
                                    break;
                                default:
                                    break;
                            }
                            Gen2.TagReadData gen2 = new Gen2.TagReadData();
                            trData.prd = gen2;
                            gen2.q.initialQ = gen2QResponse;
                            gen2.lf = Gen2.LinkFrequency.getFrequency(gen2LFResponse);
                            switch (gen2TargetResponse) 
                            {
                                case 0:
                                    gen2.target = Gen2.Target.A;
                                    break;
                                case 1:
                                    gen2.target = Gen2.Target.B;
                                    break;
                                case 2:
                                    break;
                            }
                        }
                        if(featuresFlag.contains(ReaderFeaturesFlag.READER_FEATURES_FLAG_ANTENNA_READ_TIME))
                        {
                            /**
                             * Currently protocol ID is coming as 2nd custom parameter
                             **/
                            if (customProtocolValue != null && customProtocolValue instanceof ThingMagicCustomProtocolID)
                            {
                                ThingMagicCustomProtocol protocolID = ((ThingMagicCustomProtocolID)customProtocolValue).getProtocolId();
                                trData.readProtocol = parseThingmagicCustomProtocol(protocolID.intValue());
                            }
                        }
                        else
                        {
                            //Get the protocol using tag rospecId - to handle backward compatibility case
                            trData.readProtocol = mapRoSpecIdToProtocol.get(tag.getROSpecID().getROSpecID().intValue());
                        }
                        if(customDataValue != null && customDataValue instanceof ThingMagicCustomTagopResponse)
                        {
                            ThingMagicCustomTagopResponse customTagopResponse = (ThingMagicCustomTagopResponse) customDataValue;
                            UnsignedByteArray tagOpBytes = customTagopResponse.getTagopByteStreamParam().getByteStream();
                            byte[] customTagOpBytes = new byte[tagOpBytes.getByteLength()];
                            for (int i=0; i < tagOpBytes.getByteLength(); i++)
                            {
                                customTagOpBytes[i] = tagOpBytes.get(i).toByte();
                            }
                            trData.data = customTagOpBytes;
                        }
                    }
                }
            }
            if (!continuousReading)
            {
                //System.out.println("readata " + readData + " trData " + trData);
                if (null != readData && trData != null)
                {
                    readData.add(trData);
                }
            }
            return trData;
        }

        // Method to parse the thingmagic custom protocol id and return TagProtocol.
        private TagProtocol parseThingmagicCustomProtocol(int protocolID)
        {
            TagProtocol protocol = TagProtocol.NONE;
            switch(protocolID)
            {
                case 1:
                    protocol = TagProtocol.GEN2;
                    break;
                case 2:
                    protocol = TagProtocol.ISO180006B;
                    break;
                case 3:
                    protocol = TagProtocol.IPX64;
                    break;
                case 4:
                    protocol = TagProtocol.IPX256;
                    break;
                case 5:
                    protocol = TagProtocol.ATA;
                    break;
                default:
                    break;
            }
            return protocol;
        }

        private void parseOpSpecResult(AccessCommandOpSpecResult opSpecResult, TagReadData trData) throws ReaderException
        {
            if (opSpecResult instanceof C1G2ReadOpSpecResult)
            {
                C1G2ReadOpSpecResult result = (C1G2ReadOpSpecResult) opSpecResult;
                C1G2ReadResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch (value)
                {
                    case C1G2ReadResultType.Success:
                        trData.data = ReaderUtil.convertShortArraytoByteArray(result.getReadData().toShortArray());
                        // holds the length of data in bytes.
                        trData.dataLength = trData.data.length;
                        tagOpResponse = result.getReadData().toShortArray();
                        break;
                    case C1G2ReadResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2ReadResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2ReadResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                    case C1G2ReadResultType.Memory_Locked_Error:
                        throw new ReaderException("Gen2 memory locked");
                    case C1G2ReadResultType.Memory_Overrun_Error:
                        throw new ReaderException("Gen2 memory overrun - bad PC");
                }
            }
            else if (opSpecResult instanceof C1G2WriteOpSpecResult)
            {
                C1G2WriteOpSpecResult result = (C1G2WriteOpSpecResult) opSpecResult;
                C1G2WriteResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch (value)
                {
                    case C1G2WriteResultType.Success:
                        break;
                    case C1G2WriteResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2WriteResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2WriteResultType.Insufficient_Power:
                        throw new ReaderException("Gen2 tag has insufficient power for operation");
                    case C1G2WriteResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                    case C1G2WriteResultType.Tag_Memory_Locked_Error:
                        throw new ReaderException("Gen2 memory locked");
                    case C1G2WriteResultType.Tag_Memory_Overrun_Error:
                        throw new ReaderException("Gen2 memory overrun - bad PC");
                }
            }
            else if(opSpecResult instanceof ThingMagicISO180006BReadOpSpecResult)
            {
                ThingMagicISO180006BReadOpSpecResult result = (ThingMagicISO180006BReadOpSpecResult)opSpecResult;
                ThingMagicCustomTagOpSpecResultType resultType = result.getResult();
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    trData.data = ReaderUtil.hexStringToByteArray(result.getReadData().toString());
                    tagOpResponse = ReaderUtil.hexStringToByteArray(result.getReadData().toString());
                }
                else
                {
                    parseCustomTagOpSpecResultType(resultType);
                }
            }
            else if(opSpecResult instanceof ThingMagicISO180006BWriteOpSpecResult)
            {
                ThingMagicISO180006BWriteOpSpecResult result = (ThingMagicISO180006BWriteOpSpecResult)opSpecResult;
                ThingMagicCustomTagOpSpecResultType resultType = result.getResult();
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Tag_Memory_Overrun_Error)
                {
                    throw new ReaderException("Gen2 memory overrun - bad PC");
                }
                else if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Unsupported_Operation){
                    throw new ReaderException("Unsupported operation");
                }
                else
                {
                    parseCustomTagOpSpecResultType(resultType);
                }
            }
            else if(opSpecResult instanceof ThingMagicISO180006BLockOpSpecResult)
            {
                ThingMagicISO180006BLockOpSpecResult result = (ThingMagicISO180006BLockOpSpecResult)opSpecResult;
                ThingMagicCustomTagOpSpecResultType resultType = result.getResult();
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Tag_Memory_Overrun_Error)
                {
                    throw new ReaderException("Gen2 memory overrun - bad PC");
                }
                else if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Unsupported_Operation)
                {
                   throw new ReaderException("Unsupported operation");
                }
                else
                {
                    parseCustomTagOpSpecResultType(resultType);
                }
            }
            else if (opSpecResult instanceof C1G2KillOpSpecResult)
            {
                C1G2KillOpSpecResult result = (C1G2KillOpSpecResult) opSpecResult;
                C1G2KillResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch (value)
                {
                    case C1G2KillResultType.Success:
                        break;
                    case C1G2KillResultType.Insufficient_Power:
                        throw new ReaderException("Gen2 tag has insufficient power for operation");
                    case C1G2KillResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2KillResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2KillResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                    case C1G2KillResultType.Zero_Kill_Password_Error:
                        throw new ReaderException("Kill password is not correct");
                }
            }
            else if (opSpecResult instanceof C1G2BlockWriteOpSpecResult)
            {
                C1G2BlockWriteOpSpecResult result = (C1G2BlockWriteOpSpecResult) opSpecResult;
                C1G2BlockWriteResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch (value)
                {
                    case C1G2BlockWriteResultType.Success:
                        break;
                    case C1G2BlockWriteResultType.Insufficient_Power:
                        throw new ReaderException("Gen2 tag has insufficient power for operation");
                    case C1G2BlockWriteResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2BlockWriteResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2BlockWriteResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                    case C1G2BlockWriteResultType.Tag_Memory_Locked_Error:
                        throw new ReaderException("Gen2 memory locked");
                    case C1G2BlockWriteResultType.Tag_Memory_Overrun_Error:
                        throw new ReaderException("Gen2 memory overrun - bad PC");
                }
            }
            else if (opSpecResult instanceof C1G2LockOpSpecResult)
            {
                C1G2LockOpSpecResult result = (C1G2LockOpSpecResult) opSpecResult;
                C1G2LockResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch (value)
                {
                    case C1G2LockResultType.Success:
                        break;
                    case C1G2LockResultType.Insufficient_Power:
                        throw new ReaderException("Gen2 tag has insufficient power for operation");
                    case C1G2LockResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2LockResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2LockResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                }
            }
            else if(opSpecResult instanceof C1G2BlockEraseOpSpecResult)
            {
                C1G2BlockEraseOpSpecResult result = (C1G2BlockEraseOpSpecResult)opSpecResult;
                C1G2BlockEraseResultType resultType = result.getResult();
                int value = resultType.intValue();
                switch(value)
                {
                    case C1G2BlockEraseResultType.Success:
                        break;
                    case C1G2BlockEraseResultType.Insufficient_Power:
                        throw new ReaderException("Gen2 tag has insufficient power for operation");
                    case C1G2BlockEraseResultType.No_Response_From_Tag:
                        throw new ReaderException("Other Gen2 error");
                    case C1G2BlockEraseResultType.Nonspecific_Reader_Error:
                        throw new ReaderException("LLRP reader unknown error");
                    case C1G2BlockEraseResultType.Nonspecific_Tag_Error:
                        throw new ReaderException("General tag error");
                    case C1G2BlockEraseResultType.Tag_Memory_Locked_Error:
                        throw new ReaderException("Gen2 memory locked");
                    case C1G2BlockEraseResultType.Tag_Memory_Overrun_Error:
                        throw new ReaderException("Gen2 memory overrun - bad PC");
                }
            }
            else if(opSpecResult instanceof ThingMagicBlockPermalockOpSpecResult)
            {                
                ThingMagicBlockPermalockOpSpecResult result = (ThingMagicBlockPermalockOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    short[] lockStatus = result.getPermalockStatus().toShortArray();
                    tagOpResponse = ReaderUtil.convertShortArraytoByteArray(lockStatus);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if (opSpecResult instanceof ThingMagicHiggs2FullLoadImageOpSpecResult)
            {
                ThingMagicHiggs2FullLoadImageOpSpecResult result = (ThingMagicHiggs2FullLoadImageOpSpecResult) opSpecResult;                
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if (opSpecResult instanceof ThingMagicHiggs2PartialLoadImageOpSpecResult)
            {
                ThingMagicHiggs2PartialLoadImageOpSpecResult result = (ThingMagicHiggs2PartialLoadImageOpSpecResult) opSpecResult;                
                parseCustomTagOpSpecResultType(result.getResult());
            }            
            else if (opSpecResult instanceof ThingMagicHiggs3BlockReadLockOpSpecResult)
            {
                ThingMagicHiggs3BlockReadLockOpSpecResult result = (ThingMagicHiggs3BlockReadLockOpSpecResult) opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if (opSpecResult instanceof ThingMagicHiggs3FastLoadImageOpSpecResult)
            {
                ThingMagicHiggs3FastLoadImageOpSpecResult result = (ThingMagicHiggs3FastLoadImageOpSpecResult) opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if (opSpecResult instanceof ThingMagicHiggs3LoadImageOpSpecResult)
            {
                ThingMagicHiggs3LoadImageOpSpecResult result = (ThingMagicHiggs3LoadImageOpSpecResult) opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if ((opSpecResult instanceof ThingMagicNXPG2XEASAlarmOpSpecResult) ||
                     (opSpecResult instanceof ThingMagicNXPG2IEASAlarmOpSpecResult))
            {
                ThingMagicCustomTagOpSpecResultType resultType;
                resultType = (opSpecResult instanceof ThingMagicNXPG2XEASAlarmOpSpecResult)
                    ? ((ThingMagicNXPG2XEASAlarmOpSpecResult)opSpecResult).getResult()
                    : ((ThingMagicNXPG2IEASAlarmOpSpecResult)opSpecResult).getResult();
                if (resultType.intValue() != ThingMagicCustomTagOpSpecResultType.Success)
                {
                    parseCustomTagOpSpecResultType(resultType);
                }
                else
                {
                    UnsignedByteArray code;
                    code = (opSpecResult instanceof ThingMagicNXPG2XEASAlarmOpSpecResult)
                        ? ((ThingMagicNXPG2XEASAlarmOpSpecResult)opSpecResult).getEASAlarmCode()
                        : ((ThingMagicNXPG2IEASAlarmOpSpecResult)opSpecResult).getEASAlarmCode();

                    byte[] codeBytes = new byte[code.getByteLength()];
                    for (int i=0; i<code.getByteLength(); i++)
                    {
                        codeBytes[i] = code.get(i).toByte();
                    }
                    tagOpResponse = codeBytes;
                }
            }
            else if (opSpecResult instanceof ThingMagicWriteTagOpSpecResult)
            {
                ThingMagicWriteTagOpSpecResult result = (ThingMagicWriteTagOpSpecResult) opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicNXPG2XSetReadProtectOpSpecResult)
            {
                ThingMagicNXPG2XSetReadProtectOpSpecResult result = (ThingMagicNXPG2XSetReadProtectOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
    
            }
            else if(opSpecResult instanceof ThingMagicNXPG2ISetReadProtectOpSpecResult)
            {
                ThingMagicNXPG2ISetReadProtectOpSpecResult result = (ThingMagicNXPG2ISetReadProtectOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicNXPG2XResetReadProtectOpSpecResult)
            {
                ThingMagicNXPG2XResetReadProtectOpSpecResult result = (ThingMagicNXPG2XResetReadProtectOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicNXPG2IResetReadProtectOpSpecResult)
            {
                ThingMagicNXPG2IResetReadProtectOpSpecResult result = (ThingMagicNXPG2IResetReadProtectOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }                        
            else if(opSpecResult instanceof ThingMagicNXPG2XChangeEASOpSpecResult)
            {
                ThingMagicNXPG2XChangeEASOpSpecResult result = (ThingMagicNXPG2XChangeEASOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicNXPG2IChangeEASOpSpecResult)
            {
                ThingMagicNXPG2IChangeEASOpSpecResult result = (ThingMagicNXPG2IChangeEASOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicNXPG2XCalibrateOpSpecResult)
            {
                ThingMagicNXPG2XCalibrateOpSpecResult result = (ThingMagicNXPG2XCalibrateOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    String hexData = result.getCalibrateData().toString(16);
                    tagOpResponse = ReaderUtil.hexStringToByteArray(hexData);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicNXPG2ICalibrateOpSpecResult)
            {
                ThingMagicNXPG2ICalibrateOpSpecResult result = (ThingMagicNXPG2ICalibrateOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    String hexData = result.getCalibrateData().toString(16);
                    tagOpResponse = ReaderUtil.hexStringToByteArray(hexData);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicNXPG2IChangeConfigOpSpecResult)
            {
                ThingMagicNXPG2IChangeConfigOpSpecResult result = (ThingMagicNXPG2IChangeConfigOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    int intConfig = result.getConfigData().intValue();
                    Gen2.NXP.G2I.ConfigWord word = new Gen2.NXP.G2I.ConfigWord();
                    tagOpResponse = word.getConfigWord(intConfig);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicImpinjMonza4QTReadWriteOpSpecResult)
            {
                ThingMagicImpinjMonza4QTReadWriteOpSpecResult result = (ThingMagicImpinjMonza4QTReadWriteOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {                    
                    int s = result.getPayload().intValue();
                    Gen2.Impinj.Monza4.QTPayload qtPayload = new Gen2.Impinj.Monza4.QTPayload();

                    if((s&0x8000)!=0)
                    {
                        qtPayload.QTSR = true;
                    }
                    if((s&0x4000)!=0)
                    {
                        qtPayload.QTMEM = true;
                    }
                    tagOpResponse = qtPayload;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
        }
            else if(opSpecResult instanceof ThingMagicIDSSL900AEndLogOpSpecResult)
            {
                ThingMagicIDSSL900AEndLogOpSpecResult result = (ThingMagicIDSSL900AEndLogOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900AInitializeOpSpecResult)
            {
                ThingMagicIDSSL900AInitializeOpSpecResult result = (ThingMagicIDSSL900AInitializeOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ALogStateOpSpecResult)
            {
                ThingMagicIDSSL900ALogStateOpSpecResult result = (ThingMagicIDSSL900ALogStateOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray logState = result.getLogStateByteStream();
                    byte[] logBytes = new byte[logState.getByteLength()];
                    for (int i=0; i<logState.getByteLength(); i++)
                    {
                        logBytes[i] = logState.get(i).toByte();
                    }
                    trData.data = logBytes;
                    tagOpResponse = new Gen2.IDS.SL900A.LogState(logBytes);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASensorValueOpSpecResult)
            {
                ThingMagicIDSSL900ASensorValueOpSpecResult result = (ThingMagicIDSSL900ASensorValueOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray sensorValue = result.getSensorValueByteStream();
                    byte[] sensorBytes = new byte[sensorValue.getByteLength()];
                    for (int i=0; i<sensorValue.getByteLength(); i++)
                    {
                        sensorBytes[i] = sensorValue.get(i).toByte();
                    }
                    trData.data = sensorBytes;
                    tagOpResponse = new Gen2.IDS.SL900A.SensorReading(sensorBytes);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASetLogModeOpSpecResult)
            {
                ThingMagicIDSSL900ASetLogModeOpSpecResult result = (ThingMagicIDSSL900ASetLogModeOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900AStartLogOpSpecResult)
            {
                ThingMagicIDSSL900AStartLogOpSpecResult result = (ThingMagicIDSSL900AStartLogOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900AGetCalibrationDataOpSpecResult)
            {
                ThingMagicIDSSL900AGetCalibrationDataOpSpecResult result = (ThingMagicIDSSL900AGetCalibrationDataOpSpecResult) opSpecResult;
                if (result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray calibrationValue = result.getThingMagicIDSCalibrationData().getcalibrationValueByteStream();
                    byte[] calibrationBytes = new byte[calibrationValue.getByteLength()];
                    for (int i = 0; i < calibrationValue.getByteLength(); i++)
                    {
                        calibrationBytes[i] = calibrationValue.get(i).toByte();
                    }
                    trData.data = calibrationBytes;
                    tagOpResponse = new Gen2.IDS.SL900A.CalSfe(calibrationBytes, 0);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASetCalibrationDataOpSpecResult)
            {
                ThingMagicIDSSL900ASetCalibrationDataOpSpecResult result = (ThingMagicIDSSL900ASetCalibrationDataOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASetSFEParamsOpSpecResult)
            {
                ThingMagicIDSSL900ASetSFEParamsOpSpecResult result = (ThingMagicIDSSL900ASetSFEParamsOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900AGetMeasurementSetupOpSpecResult)
            {
                ThingMagicIDSSL900AGetMeasurementSetupOpSpecResult result = (ThingMagicIDSSL900AGetMeasurementSetupOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray measurementByteStream = result.getmeasurementByteStream();
                    byte[] measurementValue = new byte[measurementByteStream.getByteLength()];
                    for(int i = 0; i< measurementByteStream.getByteLength(); i++)
                    {
                        measurementValue[i] = measurementByteStream.get(i).toByte();
                    }
                    trData.data = measurementValue;
                    tagOpResponse = new Gen2.IDS.SL900A.MeasurementSetupData(measurementValue, 0);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if (opSpecResult instanceof ThingMagicIDSSL900AAccessFIFOReadOpSpecResult)
            {
                ThingMagicIDSSL900AAccessFIFOReadOpSpecResult result = (ThingMagicIDSSL900AAccessFIFOReadOpSpecResult) opSpecResult;
                if (result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray readPayLoad = result.getreadPayLoad();
                    byte[] fifoRead = new byte[readPayLoad.getByteLength()];
                    for (int i = 0; i < readPayLoad.getByteLength(); i++)
                    {
                        fifoRead[i] = readPayLoad.get(i).toByte();
                    }
                    trData.data = fifoRead;
                    tagOpResponse = fifoRead;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if (opSpecResult instanceof ThingMagicIDSSL900AAccessFIFOWriteOpSpecResult)
            {
                ThingMagicIDSSL900AAccessFIFOWriteOpSpecResult result = (ThingMagicIDSSL900AAccessFIFOWriteOpSpecResult) opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if (opSpecResult instanceof ThingMagicIDSSL900AAccessFIFOStatusOpSpecResult)
            {
               ThingMagicIDSSL900AAccessFIFOStatusOpSpecResult result = (ThingMagicIDSSL900AAccessFIFOStatusOpSpecResult) opSpecResult;
               if (result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
               {
                   UnsignedByte fifoStatusByte = result.getFIFOStatusRawByte();
                   /** IDS AccessFifoStatus response comes as 1 byte value instead of byte array. 
                    *  Hence setting the length of data array to 1.
                    */
                   trData.data = new byte[1];
                   trData.data[0] = fifoStatusByte.toByte();
                   tagOpResponse = new Gen2.IDS.SL900A.FifoStatus(fifoStatusByte.toByte());
               }
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASetLogLimitsOpSpecResult)
            {
                ThingMagicIDSSL900ASetLogLimitsOpSpecResult result = (ThingMagicIDSSL900ASetLogLimitsOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSetShelfLifeOpSpecResult)
            {
                ThingMagicIDSSetShelfLifeOpSpecResult result = (ThingMagicIDSSetShelfLifeOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900AGetBatteryLevelOpSpecResult)
            {
                ThingMagicIDSSL900AGetBatteryLevelOpSpecResult result = (ThingMagicIDSSL900AGetBatteryLevelOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray batteryValue = result.getThingMagicIDSBatteryLevel().getbatteryValueByteStream();
                    byte[] batteryLevelData = new byte[batteryValue.getByteLength()];
                    for(int i = 0; i< batteryValue.getByteLength(); i++)
                    {
                        batteryLevelData[i] = batteryValue.get(i).toByte();
                    }
                    trData.data = batteryLevelData;
                    tagOpResponse = new Gen2.IDS.SL900A.BatteryLevelReading(batteryLevelData);
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicIDSSL900ASetPasswordOpSpecResult)
            {
                 ThingMagicIDSSL900ASetPasswordOpSpecResult result = (ThingMagicIDSSL900ASetPasswordOpSpecResult)opSpecResult;
                 parseCustomTagOpSpecResultType(result.getResult());
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVActivateSecureModeOpSpecResult)
            {
                ThingMagicDenatranIAVActivateSecureModeOpSpecResult result = (ThingMagicDenatranIAVActivateSecureModeOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray activateSecureModeByteStream = result.getActivateSecureModeByteStream();
                    byte[] activateSecureModeValue = new byte[activateSecureModeByteStream.getByteLength()];
                    for(int i = 0; i< activateSecureModeByteStream.getByteLength(); i++)
                    {
                        activateSecureModeValue[i] = activateSecureModeByteStream.get(i).toByte();
                    }
                    trData.data = activateSecureModeValue;
                    tagOpResponse = activateSecureModeValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVActivateSiniavModeOpSpecResult)
            {
                ThingMagicDenatranIAVActivateSiniavModeOpSpecResult result = (ThingMagicDenatranIAVActivateSiniavModeOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray activateSiniavModeByteStream = result.getActivateSiniavModeByteStream();
                    byte[] activateSiniavModeValue = new byte[activateSiniavModeByteStream.getByteLength()];
                    for(int i = 0; i< activateSiniavModeByteStream.getByteLength(); i++)
                    {
                        activateSiniavModeValue[i] = activateSiniavModeByteStream.get(i).toByte();
                    }
                    trData.data = activateSiniavModeValue;
                    tagOpResponse = activateSiniavModeValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVAuthenticateOBUOpSpecResult)
            {
                ThingMagicDenatranIAVAuthenticateOBUOpSpecResult result = (ThingMagicDenatranIAVAuthenticateOBUOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray authenticateOBUByteStream = result.getAuthenitcateOBUByteStream();
                    byte[] authenticateOBUValue = new byte[authenticateOBUByteStream.getByteLength()];
                    for(int i = 0; i< authenticateOBUByteStream.getByteLength(); i++)
                    {
                        authenticateOBUValue[i] = authenticateOBUByteStream.get(i).toByte();
                    }
                    trData.data = authenticateOBUValue;
                    tagOpResponse = authenticateOBUValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVOBUAuthenticateFullPass1OpSpecResult)
            {
                ThingMagicDenatranIAVOBUAuthenticateFullPass1OpSpecResult result = (ThingMagicDenatranIAVOBUAuthenticateFullPass1OpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray authenticateFullPass1ByteStream = result.getOBUAuthenticateFullPass1ByteStream();
                    byte[] authenticateFullPass1Value = new byte[authenticateFullPass1ByteStream.getByteLength()];
                    for(int i = 0; i< authenticateFullPass1ByteStream.getByteLength(); i++)
                    {
                        authenticateFullPass1Value[i] = authenticateFullPass1ByteStream.get(i).toByte();
                    }
                    trData.data = authenticateFullPass1Value;
                    tagOpResponse = authenticateFullPass1Value;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVOBUAuthenticateFullPass2OpSpecResult)
            {
                ThingMagicDenatranIAVOBUAuthenticateFullPass2OpSpecResult result = (ThingMagicDenatranIAVOBUAuthenticateFullPass2OpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray authenticateFullPass2ByteStream = result.getOBUAuthenticateFullPass2ByteStream();
                    byte[] authenticateFullPass2Value = new byte[authenticateFullPass2ByteStream.getByteLength()];
                    for(int i = 0; i< authenticateFullPass2ByteStream.getByteLength(); i++)
                    {
                        authenticateFullPass2Value[i] = authenticateFullPass2ByteStream.get(i).toByte();
                    }
                    trData.data = authenticateFullPass2Value;
                    tagOpResponse = authenticateFullPass2Value;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVOBUAuthenticateIDOpSpecResult)
            {
                ThingMagicDenatranIAVOBUAuthenticateIDOpSpecResult result = (ThingMagicDenatranIAVOBUAuthenticateIDOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray authenticateIDByteStream = result.getOBUAuthenticateIDByteStream();
                    byte[] authenticateIDValue = new byte[authenticateIDByteStream.getByteLength()];
                    for(int i = 0; i< authenticateIDByteStream.getByteLength(); i++)
                    {
                        authenticateIDValue[i] = authenticateIDByteStream.get(i).toByte();
                    }
                    trData.data = authenticateIDValue;
                    tagOpResponse = authenticateIDValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVOBUReadFromMemMapOpSpecResult)
            {
                ThingMagicDenatranIAVOBUReadFromMemMapOpSpecResult result = (ThingMagicDenatranIAVOBUReadFromMemMapOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray readMemoryMapByteStream = result.getOBUReadMemoryMapByteStream();
                    byte[] readMemoryMapValue = new byte[readMemoryMapByteStream.getByteLength()];
                    for(int i = 0; i< readMemoryMapByteStream.getByteLength(); i++)
                    {
                        readMemoryMapValue[i] = readMemoryMapByteStream.get(i).toByte();
                    }
                    trData.data = readMemoryMapValue;
                    tagOpResponse = readMemoryMapValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicDenatranIAVOBUWriteToMemMapOpSpecResult)
            {
                ThingMagicDenatranIAVOBUWriteToMemMapOpSpecResult result = (ThingMagicDenatranIAVOBUWriteToMemMapOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray writeMemoryMapByteStream = result.getOBUWriteMemoryMapByteStream();
                    byte[] writeMemoryMapValue = new byte[writeMemoryMapByteStream.getByteLength()];
                    for(int i = 0; i< writeMemoryMapByteStream.getByteLength(); i++)
                    {
                        writeMemoryMapValue[i] = writeMemoryMapByteStream.get(i).toByte();
                    }
                    trData.data = writeMemoryMapValue;
                    tagOpResponse = writeMemoryMapValue;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicNXPAuthenticationOpSpecResult)
            {
                ThingMagicNXPAuthenticationOpSpecResult result = (ThingMagicNXPAuthenticationOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray authenticationByteStream = result.getNXPAuthenticationByteStream();
                    byte[] authenticationBytes = new byte[authenticationByteStream.getByteLength()];
                    for(int i = 0; i< authenticationByteStream.getByteLength(); i++)
                    {
                        authenticationBytes[i] = authenticationByteStream.get(i).toByte();
                    }
                    trData.data = authenticationBytes;
                    tagOpResponse = authenticationBytes;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicNXPReadbufferOpSpecResult)
            {
                ThingMagicNXPReadbufferOpSpecResult result = (ThingMagicNXPReadbufferOpSpecResult)opSpecResult;
                if(result.getResult().intValue() == ThingMagicCustomTagOpSpecResultType.Success)
                {
                    UnsignedByteArray readBufferByteStream = result.getNXPReadbufferByteStream();
                    byte[] readBufferBytes = new byte[readBufferByteStream.getByteLength()];
                    for(int i = 0; i< readBufferByteStream.getByteLength(); i++)
                    {
                        readBufferBytes[i] = readBufferByteStream.get(i).toByte();
                    }
                    trData.data = readBufferBytes;
                    tagOpResponse = readBufferBytes;
                }
                else
                {
                    parseCustomTagOpSpecResultType(result.getResult());
                }
            }
            else if(opSpecResult instanceof ThingMagicNXPUntraceableOpSpecResult)
            {
                ThingMagicNXPUntraceableOpSpecResult result = (ThingMagicNXPUntraceableOpSpecResult)opSpecResult;
                parseCustomTagOpSpecResultType(result.getResult());
            }
        }
        private void parseCustomTagOpSpecResultType(ThingMagicCustomTagOpSpecResultType resultType) throws ReaderException
        {
            int value = resultType.intValue();
            switch (value)
            {
                case ThingMagicCustomTagOpSpecResultType.Success:
                    break;
                case ThingMagicCustomTagOpSpecResultType.No_Response_From_Tag:
                    throw new ReaderException("Other Gen2 error");
                case ThingMagicCustomTagOpSpecResultType.Nonspecific_Reader_Error:
                    throw new ReaderException("LLRP reader unknown error");
                case ThingMagicCustomTagOpSpecResultType.Nonspecific_Tag_Error:
                    throw new ReaderException("General tag error");
                case ThingMagicCustomTagOpSpecResultType.Tag_Memory_Overrun_Error:
                    throw new ReaderException("Gen2 memory overrun - bad PC");
                case ThingMagicCustomTagOpSpecResultType.Unsupported_Operation:
                    throw new ReaderException("Unsupported operation");
                case ThingMagicCustomTagOpSpecResultType.Gen2V2_Authentication_Fail:
                    throw new ReaderException("Authentication failed with specified key");
                case ThingMagicCustomTagOpSpecResultType.Gen2V2_Untrace_Fail:
                    throw new ReaderException("Untrace operation failed");
            }
        }
    }
    
    /**
    *   SyncReadEndPoint
    */
    class TagReadEndPoint implements LLRPEndpoint
    {

        Reader readerName;
        public TagReadEndPoint(Reader reader) {
            readerName = reader;
        }

        public TagReadEndPoint()
        {

        }
        public void messageReceived(LLRPMessage message)
        {
            try
            {
                log("Receiver Call Back ...." + "Name - " + message.getName() + ": ResponseType - " + message.getResponseType() + ": TypeNum - " + message.getTypeNum());
                
                SignedShort msgTypeNum = message.getTypeNum();
                notifyTransportListeners(message, false, 0);
                //Now we are treating every message received from the host as a Keep Alive and 
                //resetting the "keepAliveTime" timestamp to avoid Mercury API client closing the connection as it is missing Keep Alives
                keepAliveTime = System.currentTimeMillis();
                // Received message from tmmpd, update msgStartTime here
                msgStartTime = System.currentTimeMillis();
                if(!isMsgRespReceived)
                {
                    isMsgRespReceived = sentReceiveMessageValidator(msgSent, message);
                }
                if (msgTypeNum == RO_ACCESS_REPORT.TYPENUM)
                {
                    // The message received is an Access Report.
                    RO_ACCESS_REPORT report = (RO_ACCESS_REPORT) message;
                    // Get a list of the tags read.
                    List<TagReportData> tags = report.getTagReportDataList();
                    List<RFSurveyReportData> rfReportData = report.getRFSurveyReportDataList();
                    //System.out.println("tags received from tmmpd : " + tags.size());
                    //System.out.println("outstanding tags in queue : " + tagReportQueue.size());
                    if(processData)
                    {
                        synchronized (tagReportQueue) 
                        {
                            tagReportQueue.addAll(tags);
                            tagReportQueue.notifyAll();
                        }
                        synchronized (rfReportQueue)
                        {
                            rfReportQueue.addAll(rfReportData);
                            rfReportQueue.notifyAll();
                        }
                    }
                }
                else if(msgTypeNum == ERROR_MESSAGE.TYPENUM)
                {
                    //M_Unsupported message at connect time
                    setReaderType(false);
                }                
                else if(msgTypeNum == READER_EVENT_NOTIFICATION.TYPENUM)
                {
                    ReaderEventNotificationData rEventData = ((READER_EVENT_NOTIFICATION)message).getReaderEventNotificationData();
                    AISpecEvent aiSpecEvent = rEventData.getAISpecEvent();
                    ROSpecEvent roSpecEvent = rEventData.getROSpecEvent();
                    ReaderExceptionEvent rexceptionEvent = rEventData.getReaderExceptionEvent();
                    if(roSpecEvent != null && (roSpecEvent.getEventType().intValue() == ROSpecEventType.End_Of_ROSpec))
                    {
                        // end of ROSpec
                        int roSpecID = roSpecEvent.getROSpecID().intValue();
                        endOfROSpecFlags[roSpecID-1] = true;
                        log("enable rospecs $$$$$$$$$$ " + "length : " + endOfROSpecFlags.length + " - " + endOfROSpecFlags[roSpecID-1]);
                        verifyROSpecEndStatus();
                    }
                    if(aiSpecEvent != null && (aiSpecEvent.getEventType().intValue() == AISpecEventType.End_Of_AISpec))
                    {
                        // end of AISPEC
                        endOfAISpec = true;
                    }
                    if(rexceptionEvent != null) 
                    {
                        int val = Integer.parseInt(rexceptionEvent.getMessage().toString());
                        String hexVal = Integer.toHexString(val);
                        notifyExceptionListeners(new ReaderCodeException(Integer.parseInt(hexVal.substring(3),16)));
                    }
                }
                else if(msgTypeNum == KEEPALIVE.TYPENUM)
                {
                    /**
                     * keep-alive message received, updating the keepAliveTime timestamp
                     */
                    //keepAliveTime = System.currentTimeMillis();
                    log("Received Keep Alive message");
//                    System.out.println("Received Keep Alive message at " + keepAliveTime);
                }
            }
            catch (ReaderException re)
            {
                llrpLogger.error(re.getMessage());
            }            
        }//end of messageReceived
        
        public void errorOccured(String message)
        {
            if(message.equalsIgnoreCase("java.io.IOException"))
            {
                log("IOException in callback : " + message);
                try {
                    connect();
                } catch (ReaderException ex) {
                   llrpLogger.error(ex.getMessage());
                }
            }
            log("Exception occured in callback" + message);
        }//end of errorOccured
    }//end of TagReadEndPoint class

    /**
     * MonitorKeepAlives task monitors keep-alive messages received from tmmpd
     * Would throw connection lost exception if keep-alive is not received within 20 seconds.
     */
    public class MonitorKeepAlives
    {
        long delay = KEEPALIVE_TRIGGER; // 5s delay
        LoopTask task = new LoopTask();
        Timer timer = new Timer("MonitorKeepAlivesTimer");

        public void start()
        {
            timer.cancel();
            timer = new Timer("MonitorKeepAlivesTask");
            Date executionDate = new Date();
            // this timer is scheduled to start right away with the current time
            timer.scheduleAtFixedRate(task, executionDate, delay);
        }

        public void stop()
        {
            timer.cancel();
    }

        private class LoopTask extends TimerTask
        {
            public void run()
            {
                long currTime = System.currentTimeMillis();
                long diff = (currTime - keepAliveTime); // time difference in seconds
                if(diff > (KEEPALIVE_TRIGGER * 4))  // 4 keep alives lost
                {
                    stop();
                    notifyExceptionListeners(new ReaderException("Connection Lost"));
                    //stopBackgroundParser();
                    //destroy();
                }
            }//end of run method
        }//end of LoopTask
    }//end of executingtask
    
    public SerialReader.ReaderStats processCustomStats(SerialReader.ReaderStats statValues, CustomStatsValue customStats, boolean allStats)
    {
        statValues.connectedAntennaPorts = null;
        statValues.rfOnTime = null;
        statValues.noiseFloorTxOn = null;
        for(int i=0; i < statsEnabledFlags.length; i++)
        {
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.ALL))
            {
                allStats = true;
            }
            //Get antenna
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.ANTENNA) || allStats)
            {
                statValues.antenna = customStats.getAntennaParam().getAntenna().toInteger();
            }
            //Get connected antenna list
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.CONNECTED_ANTENNA_PORTS) || allStats)
            {
                UnsignedByteArray ubArray =  customStats.getConnectedAntennaList().getconnectedAntennas();
                if(ubArray.getByteLength()>0)
                {
                    int[] capData = new int[ubArray.size()/2];
                    int len = ubArray.size();
                    for(int j=0; j < len; j+=2)
                    {
                       int ant = ubArray.get(j).toInteger();
                       int stats = ubArray.get(j+1).toInteger();
                       if(ant==1 && stats != 0)
                       {
                           capData[0] = stats;
                       }
                       else if(ant==2 && stats != 0)
                       {
                           capData[1] = stats;
                       }
                       else if(ant == 3 && stats != 0)
                       {
                           capData[2] = stats;
                       }
                       else if(ant == 4 && stats != 0)
                       {
                           capData[3] = stats;
                       }
                    }
                    statValues.connectedAntennaPorts = capData;
                }
            }
            //Get frequency
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.FREQUENCY)|| allStats)
            {
                statValues.frequency = customStats.getFrequencyParam().getFrequency().toInteger();
            }
            //Get per antenna stats
            List<perAntennaStatsList> tempData = customStats.getPerAntennaStatsListList();
            if(tempData.size() > 0)
            {
                byte[] nfData = new byte[tempData.size()];
                int[] rfData = new int[tempData.size()];
                for(int k=0 ; k < tempData.size(); k++)
                {
                    nfData[k] = tempData.get(k).getNoiseFloorParam().getnoiseFloor().toByte();
                    rfData[k] = tempData.get(k).getRFOntimeParam().getrfOntime().toInteger();
                }
                //Get noise floor
                if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.NOISE_FLOOR_SEARCH_RX_TX_WITH_TX_ON)|| allStats)
                {
                    statValues.noiseFloorTxOn = nfData;
                }
                //Get RF on time
                if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.RF_ON_TIME)|| allStats)
                {
                    statValues.rfOnTime = rfData;
                }
            }
            //Get protocol
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.PROTOCOL)|| allStats)
            {
                String proto= customStats.getProtocolParam().getProtocol().toString();
                statValues.protocol = TagProtocol.getProtocol(proto);
            }
            //Get temperature
            if(statsEnabledFlags[i].equals(SerialReader.ReaderStatsFlag.TEMPERATURE)|| allStats)
            {
                statValues.temperature = customStats.getTemperatureParam().getTemperature().intValue();
            }
        }
        return statValues;
    }
}//end of LLRPReader
