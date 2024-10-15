package samples;

import com.thingmagic.Gen2;
import com.thingmagic.GpiPinTrigger;
import com.thingmagic.Iso14443a;
import com.thingmagic.Iso15693;
import com.thingmagic.ReadListener;
import com.thingmagic.Reader;
import com.thingmagic.ReaderException;
import com.thingmagic.ReadExceptionListener;
import com.thingmagic.ReaderCodeException;
import com.thingmagic.ReaderCommException;
import com.thingmagic.ReaderUtil;
import com.thingmagic.SerialReader;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.StatsListener;
import com.thingmagic.TMConstants;
import com.thingmagic.TagOp;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;
import com.thingmagic.TagType;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import com.thingmagic.Select_UID;
import com.thingmagic.SerialTransport;
import com.thingmagic.SerialTransportNative;
import java.util.Date;

public class AutonomousMode 
{
    //Stats Listener
    static StatsListener statsListener;
    //To store the configuration option
    static String configOption = null;
    //To store the autoread type
    static String autoReadType = null;
    //To store the triggerNumber on which read has to happen
    static int trigTypeNum;
    //To store the model of the reader
    static int modelID;


    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "AutonomousMode [-v] [reader-uri] [--ant n[,n...]] [--config option] [--trigger pinNum] [--model option]\n" +
                  "[-v]  Verbose: Turn on transport listener\n" +
                  "[reader-uri]  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "[--ant n]  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n" +
                  "[--config option]: Indicates configuration options of the reader \n"
                + "                   options: 1 - saveAndRead\n "
                + "                            2 - save\n "
                + "                            3 - stream\n "
                + "                            4 - clear\n"
                + " , e.g., --config 1 for saving and enabling autonomous read\n"
                + "[--trigger pinNum] e.g., --trigger 0 for auto read on boot, --trigger 1 for read on gpi pin 1\n "
                + "[--model option] : model indicates model of the reader\n"
                + "                   option : 1 - UHF Reader\n "
                + "                            2 - M3e reader\n"
                +"Example for UHF   : tmr:///com1 --ant 1,2 --config 1 --trigger 0 for autonomous read on boot       \n"
                +         "                    tmr:///com1 --ant 1,2 --config 1 --trigger 1 for gpi trigger read on pin 1     \n"
                +         "                    tmr:///com1 --ant 1,2 --config 2, tmr:///com1 --ant 1,2 --config 3 --model 1   \n"
                +"Example for HF/LF : tmr:///com1 --config 1 --trigger 0                                             \n"
                +         "                    tmr:///com1 --config 3 --model 2 \n");
        System.exit(1);
    }

    public static void setTrace(Reader r, String args[])
    {
        if (args[0].toLowerCase().equals("on"))
        {
            r.addTransportListener(Reader.simpleTransportListener);
        }      
    }

    public static void main(String argv[]) throws ReaderException
    {
        Reader r = null;
        int nextarg = 0;
        boolean trace = false;
        int[] antennaList = null;
        SimpleReadPlan srp = new SimpleReadPlan();
        //To check model option is provided by the user ir not
        boolean isModelOptionConfigured = false;
        //Flag indicating the status of auto read - enable or disable
        boolean enableAutoRead = true;

        if (argv.length < 1)
        {
            usage();
        }
        if (argv[nextarg].equals("-v"))
        {
            trace = true;
            nextarg++;
        }

        try
        {
            String readerURI = argv[nextarg];
            nextarg++;
            
            for ( ; nextarg < argv.length; nextarg++)
            {
                String arg = argv[nextarg];
                if (arg.equalsIgnoreCase("--ant"))
                {
                    if (antennaList != null)
                    {
                        System.out.println("Duplicate argument: --ant specified more than once");
                        usage();
                    }
                    antennaList = parseAntennaList(argv, nextarg);
                    nextarg++;
                }
                else if(arg.equalsIgnoreCase("--config"))
                {
                    String option = argv[++nextarg];
                    int optionNum = Integer.parseInt(option);
                    switch(optionNum)
                    {
                        case 1:
                            // Saves the configuration and performs read with the saved configuration
                            configOption = "saveAndRead";
                            break;
                        case 2:
                            // Only saves the configuration
                            configOption = "save";
                            break;
                        case 3:
                            // Streams the tag responses if autonomous read is already enabled
                            configOption = "stream";
                            break;
                        case 4:
                            //Clears the configuration
                            configOption = "clear";
                            break;
                        default:
                            System.out.println("Please select config option between 1 and 5");
                            usage();
                    }
                }
                else if(arg.equalsIgnoreCase("--trigger"))
                {
                    String trigType = argv[++nextarg];
                    trigTypeNum = Integer.parseInt(trigType);
                    switch(trigTypeNum)
                    {
                        case 0:
                            autoReadType = "ReadOnBoot";
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            autoReadType = "ReadOnGPI";
                            break;
                        default:
                            System.out.println("Please select trigger option between 0 and 4");
                            usage();
                    }
                }
                else if(arg.equalsIgnoreCase("--model"))
                {
                    String option = argv[++nextarg];
                    int model = Integer.parseInt(option);
                    switch(model)
                    {
                        case 1:
                        case 2:
                            modelID = model;
                            isModelOptionConfigured = true;
                            break;
                        default:
                            System.out.println("Please select model option between 1 and 2\n");
                            usage();
                    }
                }
                else
                {
                    System.out.println("Argument "+argv[nextarg] +" is not recognised");
                    usage();
                }
            }
            if(((autoReadType != null) && (autoReadType.equalsIgnoreCase("ReadOnBoot") || autoReadType.equalsIgnoreCase("ReadOnGPI"))) 
                && !(configOption.equalsIgnoreCase("saveAndRead")))
            {
                System.out.println("Please select saveAndRead config option to enable autoReadType");
                usage();
            }

            //model option is supported only with "stream" configuration, throw error if user sets model option with other config options like saveAndRead, save, clear.
            if((isModelOptionConfigured) && !(configOption.equalsIgnoreCase("stream")))
            {
                System.out.println("Please select model with config option 3 only");
                usage();
            }
            //--model option is mandatory for "stream" option.
            if((configOption.equalsIgnoreCase("stream")) && (!isModelOptionConfigured))
            {
                System.out.println("Model is a mandatory field for config option 3. Please provide model.");
                usage();
            }

            r = Reader.create(readerURI);
            if (trace)
            {
                setTrace(r, new String[]{"on"});
            }

            //stream option will open the serial port and try to receive the autonomous responses
            if(configOption.equalsIgnoreCase("stream"))
            {
                // Initialize the params
                ((SerialReader)r).autonomousStreaming = true;
                ((SerialReader)r).statsFlags = SerialReader.ReaderStatsFlag.TEMPERATURE.value;
                if(modelID == 2)
                {
                    ((SerialReader)r).model = "M3e";
                }

                //Connect to the serial port
                SerialConnect sc = new SerialConnect();
                boolean isConnected = sc.connect(readerURI, (SerialReader)r, modelID);
                if(isConnected)
                {
                    //Create and enable the listeners before extracting the responses
                    ReadListener readListener = new PrintListener();
                    r.addReadListener(readListener);
                    ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
                    r.addReadExceptionListener(exceptionListener);
                    statsListener = new ReaderStatsListener();
                    r.addStatsListener(statsListener);
                    
                    //Extract the streaming responses
                    r.receiveAutonomousReading();
                    while(true)
                    {
                       Thread.sleep(5000);
                    }
                }
            }
            else
            {
                try
                {
                    /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
                     * The connection may fail if the module is configured to a different baud rate. If
                     * that is the case, the MercuryAPI tries connecting to the module with other supported
                     * baud rates until the connection is successful using baud rate probing mechanism.
                     */
                    r.connect();
                }
                catch (Exception ex)
                {
                    if(r instanceof SerialReader)
                    {
                        if(ex.getMessage().contains("Timeout"))
                        {
                            // create a single element array and pass it as parameter to probeBaudRate().
                            int currentBaudRate[] = new int[1];
                            // Default baudrate connect failed. Try probing through the baudrate list
                            // to retrieve the module baudrate
                            ((SerialReader)r).probeBaudRate(currentBaudRate);
                            //Set the current baudrate so that next connect will use this baudrate.
                            r.paramSet("/reader/baudRate", currentBaudRate[0]);
                            // Now connect with current baudrate
                            r.connect();
                        }
                        /* When the module is streaming the tags,
                         * r.connect() returns with "Connect Successful...Streaming tags" exception, which should be handled in the codelet.
                         * User can either continue to parse streaming responses or stop the streaming.
                         * Use 'stream' option demonstrated in the AutonomousMode.java codelet to
                         * continue streaming. To stop the streaming, use stopStreaming() as demonstrated below.
                         */
                        else if(ex.getMessage().contains("Connect Successful...Streaming tags"))
                        {
                            //stop the streaming
                            ((SerialReader)r).stopStreaming();

                            // Re-issue connect
                            r.connect();
                        }
                        else
                        {
                            throw new Exception(ex.getMessage().toString());
                        }
                    }
                }
                String model = (String) r.paramGet("/reader/version/model");
                {
                    if (!(model.equalsIgnoreCase("M3e")))
                    {
                       if ((model.equalsIgnoreCase("M6e Micro") || model.equalsIgnoreCase("M6e Nano")) && antennaList == null)
                       {
                           System.out.println("Module doesn't has antenna detection support, please provide antenna list");
                           r.destroy();
                           usage();
                       }
                    }
                    if(configOption.equalsIgnoreCase("saveAndRead"))
                    {
                        if(model.equalsIgnoreCase("M3e"))
                        {
                            srp = configureM3ePersistentSettings(r, antennaList);
                        }
                        else
                        {
                            srp = configureUHFPersistentSettings(r, model, antennaList);
                        }
                        //To disable autonomous read make enableAutonomousRead flag to false by setting enableAutoRead to false and do SAVEWITHRREADPLAN 
                        srp.enableAutonomousRead = enableAutoRead;
                        r.paramSet("/reader/read/plan", srp);

                        //Enable the readerstats
                        SerialReader.ReaderStatsFlag[] READER_STATISTIC_FLAGS = {SerialReader.ReaderStatsFlag.TEMPERATURE};
                        //Uncomment the line if reader stats need to be included as part of autonomous operation
                        //r.paramSet("/reader/stats/enable",READER_STATISTIC_FLAGS);

                        //Save the configuration
                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.SAVEWITHREADPLAN));
                        System.out.println("User profile set option:save all configuration with read plan is successful");
                        
                        try
                        {
                            //Restore the configuration
                            r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.RESTORE));
                            System.out.println("User profile set option:restore all configuration is successful");
                        }
                        catch(ReaderException ex)
                        {
                            if(ex.getMessage().contains("Verifying flash contents failed"))
                            {
                                System.out.println("Please use saveAndRead option to trigger autonomous read");
                            }
                        }

                        if(enableAutoRead)
                        {
                            ReadListener readListener = new PrintListener();
                            r.addReadListener(readListener);
                            ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
                            r.addReadExceptionListener(exceptionListener);
                            statsListener = new ReaderStatsListener();
                            r.addStatsListener(statsListener);

                            //receive the tags for 5 secs
                            r.receiveAutonomousReading();
                            Thread.sleep(5000);

                            r.removeReadListener(readListener);
                            r.removeReadExceptionListener(exceptionListener);
                        }
                    }
                    else if(configOption.equalsIgnoreCase("save"))
                    {
                        if(model.equalsIgnoreCase("M3e"))
                        {
                            srp = configureM3ePersistentSettings(r, antennaList);
                        }
                        else
                        {
                            srp = configureUHFPersistentSettings(r, model, antennaList);
                        }

                        //To disable autonomous read make enableAutonomousRead flag to false and do SAVEWITHRREADPLAN 
                        srp.enableAutonomousRead = enableAutoRead;
                        r.paramSet("/reader/read/plan", srp);

                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.SAVEWITHREADPLAN));
                        System.out.println("User profile set option:save all configuration with read plan is successful");
                    }
                    else if(configOption.equalsIgnoreCase("clear"))
                    {
                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.CLEAR));
                        System.out.println("User profile set option:clear all configuration is successful");
                    }
                    else
                    {
                        throw new IllegalArgumentException("Please input correct config option");
                    }
                }
            }
            r.destroy();
        }
        catch (ReaderException re)
        {
            System.out.println("ReaderException: " + re.getMessage());
        }
        catch (Exception re)
        {
            System.out.println("Exception: " + re.getMessage());
        }
    }

    // Function to configure UHF persistent settings
    static SimpleReadPlan configureUHFPersistentSettings(Reader r, String model, int []antennaList) throws Exception
    {
        // baudrate
        r.paramSet("/reader/baudRate", 115200);

        //Region
        Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
        if (supportedRegions.length < 1)
        {
            throw new Exception("Reader doesn't support any regions");
        }
        else
        {
            r.paramSet("/reader/region/id", supportedRegions[0]);
        }

        //Protocol
        TagProtocol protocol = TagProtocol.GEN2;
        r.paramSet("/reader/tagop/protocol", protocol);

        //Gen2 settings

        //Uncomment this to set BLF, Tari, Tag Encoding for M6e family modules or Network readers
        //r.paramSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
        //r.paramSet("/reader/gen2/tari", Gen2.Tari.TARI_25US);
        //r.paramSet("/reader/gen2/tagEncoding", Gen2.TagEncoding.M4);
        
        r.paramSet("/reader/gen2/target", Gen2.Target.A);
        r.paramSet("/reader/gen2/session", Gen2.Session.S0);
        r.paramSet("/reader/gen2/q", new Gen2.DynamicQ());

        //RF Power settings
        r.paramSet("/reader/radio/readPower", 2000);
        r.paramSet("/reader/radio/writePower", 2000);
        
        //hopTable
        int[] hopTable = (int[])r.paramGet("/reader/region/hopTable");
        r.paramSet("/reader/region/hopTable", hopTable);
        
        //hopTime
        int hopTimeValue = (Integer) r.paramGet("/reader/region/hopTime");
        r.paramSet("/reader/region/hopTime", hopTimeValue);
        
        //For Open region, dwell time, minimum frequency, quantization step can also be configured persistently
        if(Reader.Region.OPEN == r.paramGet("/reader/region/id"))
        {
            //Set dwell time enable before stting dwell time
            r.paramSet("/reader/region/dwellTime/enable", true);
            
            //set quantization step
            r.paramSet("/reader/region/quantizationStep", 100000);

            //set dwell time
            r.paramSet("/reader/region/dwellTime", 250);
            
            //set minimum frequency
            r.paramSet("/reader/region/minimumFrequency", 865700);
        }

        // Embedded tag operation
        /* Uncomment the following line if want to enable embedded read with autonomous operation.
         * Add tagop object op in simple read plan constructor.
         * Add filter object epcFilter in simple read plan constructor.
         */
         // TagOp op = new Gen2.ReadData(Gen2.Bank.RESERVED, 2, (byte)2);
         //Gen2.TagData epcFilter = new Gen2.TagData("112233445566778899009999");
         
        GpiPinTrigger gpiTrigger = null;
        if((autoReadType != null) && (autoReadType.equalsIgnoreCase("ReadonGPI")))
        {
            gpiTrigger = new GpiPinTrigger();
            //Gpi trigger option not there for M6e Micro USB
            if(!model.equalsIgnoreCase("M6e Micro USB"))
            {
                gpiTrigger.enable = true;
                //set the gpi pin to read on
                r.paramSet("/reader/read/trigger/gpi", new int[]{trigTypeNum});
            }
        }
        SimpleReadPlan srp = new SimpleReadPlan(antennaList, protocol, null, null, 1000);
        if((autoReadType != null) && (autoReadType.equalsIgnoreCase("ReadonGPI")))
        {
            if(!model.equalsIgnoreCase("M6e Micro USB"))
            {
              srp.triggerRead = gpiTrigger;
            }
        }
        return srp;
    }

    // Function to configure M3e persistent settings
    static SimpleReadPlan configureM3ePersistentSettings(Reader r, int []antennaList) throws Exception
    {
        // baudrate
        r.paramSet("/reader/baudRate", 115200);
     
        //Protocol
        TagProtocol protocol = TagProtocol.ISO14443A;
        r.paramSet("/reader/tagop/protocol", protocol);
        
        //enableReadFiltering
        r.paramSet("/reader/tagReadData/enableReadFilter", true);
        
        // Embedded tag operation
        /* Uncomment the following line if want to enable embedded read with autonomous operation.
         * Add tagop object op in simple read plan constructor.
         * Add filter object epcFilter in simple read plan constructor.
         */
//        Reader.MemoryType type = Reader.MemoryType.TAG_MEMORY;
//        int address = 0;
//        byte length = 1;
//        Reader.ReadMemory tagOp = new Reader.ReadMemory(type, address, length);
//        Select_UID filter = new Select_UID(32, new byte[]{0x11, 0x22, 0x33, 0x44});
         
        GpiPinTrigger gpiTrigger = null;
        if((autoReadType != null) && (autoReadType.equalsIgnoreCase("ReadonGPI")))
        {
            gpiTrigger = new GpiPinTrigger();
            gpiTrigger.enable = true;
            //set the gpi pin to read on
            r.paramSet("/reader/read/trigger/gpi", new int[]{trigTypeNum});
        }
        
        SimpleReadPlan srp = new SimpleReadPlan(antennaList, protocol, null, null, 1000);
        if((autoReadType != null) && (autoReadType.equalsIgnoreCase("ReadonGPI")))
        {
            srp.triggerRead = gpiTrigger;
        }
        return srp;
    }

    static class PrintListener implements ReadListener
    {
        public void tagRead(Reader r, TagReadData tr)
        {
            if(configOption == "stream")
            {
                System.out.println("Background read:"+ String.format("Tag ID :%s ant:%d count:%d time:%s",
                         tr.getTag().epcString(),
                         getAntennaID((byte)tr.getAntenna()),
                         tr.getReadCount(),
                         new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ").format(new Date(tr.getTime()))));
            }
            else
            {
                System.out.println("Background read: " + tr.toString());
            }
            if (tr.getData().length > 0)
            {
                if (tr.isErrorData)
                {
                    // In case of error, show the error to user. Extract error code.
                    byte[] errorCodeBytes = tr.getData();
                    int offset = 0;
                    //converts byte array to int value
                    int errorCode = ((errorCodeBytes[offset] & 0xff) <<  8)| ((errorCodeBytes[offset + 1] & 0xff) <<  0);
                    System.out.println("Embedded Tag operation failed. Error: " + new ReaderCodeException(errorCode));
                }
                else
                {
                    System.out.println( String.format("Data[%d]: %s", 
                            tr.dataLength/8, ReaderUtil.byteArrayToHexString(tr.getData())));
                }
            }
        }
    }

    /**
     * Function to decode antenna id
     * @param txRxAntenna
     * @return antenna id
     */
        private static int getAntennaID(byte txRxAntenna)
        {
            int tx = (txRxAntenna >> 4) & 0xF;
            int rx = (txRxAntenna >> 0) & 0xF;

            // Due to limited space, Antenna 16 wraps around to 0
            if (0 == tx) { tx = 16; }
            if (0 == rx) { rx = 16; }
            if (tx == rx)
            {
                return tx;
            }

            return txRxAntenna;
        }

    static class ReaderStatsListener implements StatsListener
    {
        public void statsRead(SerialReader.ReaderStats readerStats)
        {
            System.out.println("Temperature: " + readerStats.temperature);
        }
    }

    static  int[] parseAntennaList(String[] args,int argPosition)
    {
        int[] antennaList = null;
        try
        {
            String argument = args[argPosition + 1];
            String[] antennas = argument.split(",");
            int i = 0;
            antennaList = new int[antennas.length];
            for (String ant : antennas)
            {
                antennaList[i] = Integer.parseInt(ant);
                i++;
            }
        }
        catch (IndexOutOfBoundsException ex)
        {
            System.out.println("Missing argument after " + args[argPosition]);
            usage();
        }
        catch (Exception ex)
        {
            System.out.println("Invalid argument at position " + (argPosition + 1) + ". " + ex.getMessage());
            usage();
        }
        return antennaList;
    }

    static class TagReadExceptionReceiver implements ReadExceptionListener
    {
        String strDateFormat = "M/d/yyyy h:m:s a";
        SimpleDateFormat sdf = new SimpleDateFormat(strDateFormat);
        public void tagReadException(com.thingmagic.Reader r, ReaderException re)
        {
            String format = sdf.format(Calendar.getInstance().getTime());
            System.out.println("Reader Exception: " + re.getMessage() + " Occured on :" + format);
            if(re.getMessage().equals("Connection Lost"))
            {
                System.exit(1);
            }
        }
    }

    //Class to open the serial port connection with the reader in case of streaming
    static class SerialConnect
    {
        SerialTransportNative st;
        SerialTransport srt;
        int transportTimeout = 5000;
        long baseTimeStamp = 0;
        String comPort;
        public boolean connect(String deviceName, SerialReader r, int model) throws ReaderException
        {
            //serial transport native comport should be /COM12 or /COMxx
            deviceName = deviceName.substring(6).toUpperCase();
            comPort = deviceName;
            boolean isConnected = false;
            System.out.println("Waiting for streaming...");
            while(!isConnected)
            {
                try
                {
                    if (st != null)
                    {
                        st.shutdown();
                    }
                    st = new SerialTransportNative(deviceName);
                    st.open();
                    int[] bps =
                    {
                        115200, 9600, 921600, 19200, 38400, 57600, 230400, 460800
                    };

                    for (int count = 0; count < bps.length; count++)
                    {
                        st.setBaudRate(bps[count]);
                        st.flush();
                        try
                        {
                            ((SerialReader)r).receiveResponse(st, modelID);
                            isConnected = true;
                            System.out.println("Connected to the reader successfully");
                            break;
                        }
                        catch (ReaderCommException ex)
                        {
                            System.out.println("Exception: " +ex.getMessage());
                        }
                        catch(ReaderException re)
                        {
                            if(re.getMessage().contains("Timeout"))
                            {
                                System.out.println("Failed to connect with " + bps[count] + " baudrate.");
                            }
                            else
                            {
                                throw re;
                            }
                        }
                    }
                } 
                catch (Exception ex)
                {
                    
                    if(ex.getMessage().contains("Input/output error"))
                    {
                        //Exit the while loop because the port is busy or already in use by other application.
                        System.out.println("Please check if the port is already in use by another application.");
                        break;
                    }
                    else
                    {
                        System.out.println(ex.getMessage());
                    }
                }
            }
            return isConnected;
        }
    }
}
