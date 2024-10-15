/**
 * Sample program that reads tags in the background and prints the
 * tags found using M7e and M3e.
 * 
 * The code sample also demonstrates how to connect to M6E series modules
 * and read tags using the Mercury API v1.37.1. The M6E family users must
 * modify their application by referring this code sample in order to use
 * the latest API version.
 *    (a) Set "enableM6eCompatibility" true to make the API compatible with M6E.
 *    (b) Set "enableTagOpProtocol" true to perform standalone tag operation on M6E modules.
 */

package samples;
import com.thingmagic.*;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.EnumSet;
import java.util.Set;

public class readasync
{
  static SerialPrinter serialPrinter;
  static StringPrinter stringPrinter;
  static TransportListener currentListener;
  static ReadListener rl;
  static ReadExceptionListener exceptionListener;
  // This indicates the read time of async read, i.e., sleep time between start and stop read.
  private static final int SLEEPTIME = 5000;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "readasync [-v] [reader-uri] [--ant n[,n...]] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n"
                + "Example for UHF: tmr:///com1 --ant 1,2 ; tmr://10.11.115.32 --ant 1,2\n "
                + "Example for HF/LF: 'tmr:///com4'\n ");
    System.exit(1);
  }

   public static void setTrace(Reader r, String args[])
  {
    if (args[0].toLowerCase().equals("on"))
    {
        r.addTransportListener(Reader.simpleTransportListener);
        currentListener = Reader.simpleTransportListener;
    }
    else if (currentListener != null)
    {
        r.removeTransportListener(Reader.simpleTransportListener);
    }
  }

   static class SerialPrinter implements TransportListener
  {
    public void message(boolean tx, byte[] data, int timeout)
    {
      System.out.print(tx ? "Sending: " : "Received:");
      for (int i = 0; i < data.length; i++)
      {
        if (i > 0 && (i & 15) == 0)
          System.out.printf("\n         ");
        System.out.printf(" %02x", data[i]);
      }
      System.out.printf("\n");
    }
  }

  static class StringPrinter implements TransportListener
  {
    public void message(boolean tx, byte[] data, int timeout)
    {
      System.out.println((tx ? "Sending:\n" : "Receiving:\n") +
                         new String(data));
    }
  }
  public static void main(String argv[])
  {
    // Program setup
    Reader r = null;
    int nextarg = 0;
    boolean trace = false;
    int[] antennaList = null;
    // Capture the start time before starting the read.
    long startTime = 0;
    /**
     * To set tagOp protocol before performing tag operation, set the value to true.
     * By default, protocol is set to NONE on the M6e family.
     * Make sure to set Gen2 protocol before performing Gen2 standalone tag operation.
     */
    boolean enableTagOpProtocol = false;
    /**
     *  To make the API compatible with M6e, set the flag to true.
     */
    boolean enableM6eCompatibility = false;

    if (argv.length < 1)
      usage();

    if (argv[nextarg].equals("-v"))
    {
      trace = true;
      nextarg++;
    }

    // Create Reader object, connecting to physical device
    try
    {
        String readerURI = argv[nextarg];
        nextarg++;

        for (; nextarg < argv.length; nextarg++)
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
            else
            {
                System.out.println("Argument " + argv[nextarg] + " is not recognised");
                usage();
            }
        }

        r = Reader.create(readerURI);
        if (trace)
        {
          setTrace(r, new String[] {"on"});
        }
        try
        {
            r.connect();
        }
        catch (Exception ex)
        {
            if(r instanceof SerialReader)
            {
                /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
                 * The connection may fail if the module is configured to a different baud rate. If
                 * that is the case, the MercuryAPI tries connecting to the module with other supported
                 * baud rates until the connection is successful using baud rate probing mechanism.
                 */
                if(ex.getMessage().contains("Timeout") )
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
        if (Reader.Region.UNSPEC == (Reader.Region)r.paramGet("/reader/region/id"))
        {
            Reader.Region[] supportedRegions = (Reader.Region[])r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
            if (supportedRegions.length < 1)
            {
                 throw new Exception("Reader doesn't support any regions");
            }
            else
            {
                 r.paramSet("/reader/region/id", supportedRegions[0]);
            }
        }
        if(enableM6eCompatibility)
        {
            /* To make the latest API compatible with M6e family modules,
             * configure the following parameters.
             * 1. tagOp protocol:  This parameter is not needed for Continuous Read or Async Read, but it
             *                     must be set when standalone tag operation is performed, because the
             *                     protocol is set to NONE, by default, in the M6e family modules.
             *                     So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
             * 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
             *                     This filter is enabled, by default, in the M6e family modules.
             * 3. Metadata flag:   SerialReader.TagMetadataFlag.ALL includes all flags (Supported by UHF and HF/LF readers).
             *                     Disable unsupported flags for M6E family as shown below.
             * Note: tagOp protocol and read filter are one time configurations - These must be set on the module once after every power ON. 
             *       We do not have to set them in every read cycle.
             *       But the Metadata flag must be set once after establishing a connection to the module using r.connect().
             */

            if(enableTagOpProtocol)
            {
                /* 1. tagOp protocol: This parameter is not needed for Continuous Read or Async Read, but it
                 *                    must be set when standalone tag operation is performed, because the
                 *                    protocol is set to NONE, by default, in the M6e family modules.
                 *                    So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
                 */
                r.paramSet("/reader/tagop/protocol", TagProtocol.GEN2);
                Gen2.ReadData readOp = new Gen2.ReadData(Gen2.Bank.EPC, 0, (byte)2);
                short[] response = (short[])r.executeTagOp(readOp, null);
                System.out.println("ReadData: ");
                for (short dt : response)
                {
                    System.out.printf("%02x \t", dt);
                }
                System.out.printf("\n");
            }
            {
                /* 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
                 *                     This filter is enabled, by default, in the M6e family modules.
                 *                     Note that this is a one time configuration while connecting to the module after
                 *                     power ON. We do not have to set it for every read cycle.
                 */
                r.paramSet("/reader/tagReadData/enableReadFilter", false);

                /* 3. Metadata flag: SerialReader.TagMetadataFlag.ALL includes all flags (Supported by UHF and HF/LF readers).
                 *                   Disable unsupported flags for M6e family as shown below.
                 */
                Set<TagReadData.TagMetadataFlag> flagSet = EnumSet.of(TagReadData.TagMetadataFlag.ANTENNAID ,TagReadData.TagMetadataFlag.DATA,
                                                                      TagReadData.TagMetadataFlag.FREQUENCY, TagReadData.TagMetadataFlag.GEN2_LF, 
                                                                      TagReadData.TagMetadataFlag.GEN2_Q,    TagReadData.TagMetadataFlag.GEN2_TARGET, 
                                                                      TagReadData.TagMetadataFlag.GPIO_STATUS, TagReadData.TagMetadataFlag.PHASE,
                                                                      TagReadData.TagMetadataFlag.PROTOCOL, TagReadData.TagMetadataFlag.READCOUNT, 
                                                                      TagReadData.TagMetadataFlag.RSSI, TagReadData.TagMetadataFlag.TIMESTAMP);
                r.paramSet("/reader/metadata", flagSet);
            }
        }

        String model = (String)r.paramGet("/reader/version/model");
        if (!(model.equalsIgnoreCase("M3e")))
        {
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
            r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
        }
        else
        {
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.ISO14443A, null, null, 1000);
            r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
        }
        
        exceptionListener = new TagReadExceptionReceiver();
        r.addReadExceptionListener(exceptionListener);
        // Create and add tag listener
        rl = new PrintListener();
        r.addReadListener(rl);

        // search for tags in the background
        r.startReading();

        startTime = System.currentTimeMillis();

        /** Exit the while loop,
          * 1. When error occurs 
          * 2. When sleep timeout expires
          */
        while(true)
        {
            if((System.currentTimeMillis() - startTime) > SLEEPTIME)
            {
                /* break if sleep timeout expired */
                break;
            }

            /* Exit the process if any error occured */
            if(r.lastReportedException != null)
            {
                exceptionHandler(r);
                /* Can add recovery mechanism here*/
                /* Do some work here */
                System.exit(0);
            }
            Thread.sleep(1);
        }

        r.stopReading();

        r.removeReadListener(rl);
        r.removeReadExceptionListener(exceptionListener);

        // Shut down reader
        r.destroy();
    } 
    catch (ReaderException re)
    {
        // Shut down reader
        if(r!=null)
        {
            r.destroy();
        }
        System.out.println("ReaderException: " + re.getMessage());
    }
    catch (Exception re)
    {
        // Shut down reader
        if(r!=null)
        {
            r.destroy();
        }
        System.out.println("Exception: " + re.getMessage());
    }
  }

  static class PrintListener implements ReadListener
  {
    public void tagRead(Reader r, TagReadData tr)
    {
      System.out.println("Background read: " + tr.toString());

      /* Reset the variable for valid tag response. */
      r.lastReportedException = null;
    }

  }

  static class TagReadExceptionReceiver implements ReadExceptionListener
  {
        String strDateFormat = "M/d/yyyy h:m:s a";
        SimpleDateFormat sdf = new SimpleDateFormat(strDateFormat);
        public void tagReadException(com.thingmagic.Reader r, ReaderException re)
        {
            String format = sdf.format(Calendar.getInstance().getTime());
            String currentReportedException = re.getMessage();
            if(r.lastReportedException == null || (r.lastReportedException != null && 
                    !(r.lastReportedException.getMessage().equalsIgnoreCase(currentReportedException))))    
            {
                System.out.println("Reader Exception: " + re.getMessage() + " Occured on :" + format);
            }
            r.lastReportedException = re;
        }
    }
    // Function to handle different exception received
    static void exceptionHandler(Reader r)
    {
        ReaderException re = r.lastReportedException;
        if(re.getMessage().equalsIgnoreCase("The reader received a valid command with an unsupported or invalid parameter")
           || re.getMessage().equalsIgnoreCase("Unimplemented feature."))
        {
            r.stopReading();
            r.destroy();
        }
        else if(re.getMessage().equalsIgnoreCase("Timeout"))
        {
         ((SerialReader)r).TMR_flush();
         r.destroy();
        }
        else
        {
            /* Do not send stop read for unknown errors */
            r.destroy();
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
  
}
