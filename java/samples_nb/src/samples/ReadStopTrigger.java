/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package samples;

import com.thingmagic.*;
import java.text.SimpleDateFormat;
import java.util.Calendar;

/**
 *
 * Sample program that stops the read after finding N tags 
 * and prints the tags found.
 */
public class ReadStopTrigger {
    
     
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "ReadStopTrigger [-v] [reader-uri] [--ant n[,n...]] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n"
                + "Example for UHF: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' or '-v tmr:///com4 --ant 1,2'\n "
                + "Example for HF/LF: 'tmr:///com4'\n ");
    System.exit(1);
  }

  public static void setTrace(Reader r, String args[])
  {    
    if (args[0].toLowerCase().equals("on"))
    {
      r.addTransportListener(r.simpleTransportListener);
    }    
  }
   
  static int totalTagCount = 0;

  
  public static void main(String argv[])
  {
    // Program setup
    Reader r = null;
    int nextarg = 0;
    boolean trace = false;
    int[] antennaList = null;
    /**Flag when set to true, executes sync read.
     * set to false, executes async read.
     */
    boolean enableSyncRead = false;
    /**Flag when set to true, sets simple read plan
     * set to false, sets multi read plan
     */
    boolean enableSimpleReadplan = true;
    /**Flag when set to true, performs dynamic protocol
     * switching on M3e.
     */
    boolean isDynamicSwEnable = true;

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
     
        TagReadData[] tagReads;

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
            /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
             * The connection may fail if the module is configured to a different baud rate. If
             * that is the case, the MercuryAPI tries connecting to the module with other supported
             * baud rates until the connection is successful using baud rate probing mechanism.
             */
            r.connect();
        }
        catch (Exception ex)
        {
            if((ex.getMessage().contains("Timeout")) && (r instanceof SerialReader))
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
            else
            {
                throw new Exception(ex.getMessage().toString());
            }
        }
        if (Reader.Region.UNSPEC == (Reader.Region) r.paramGet("/reader/region/id"))
        {
            Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
            if (supportedRegions.length < 1)
            {
                throw new Exception("Reader doesn't support any regions");
            }
            else
            {
                r.paramSet("/reader/region/id", supportedRegions[0]);
            }
        }
        String model = (String)r.paramGet("/reader/version/model");
        if (!(model.equalsIgnoreCase("M3e")))
        {
            int initQ = 0;// set the Q value
            Gen2.StaticQ setQ = new Gen2.StaticQ(initQ);
            r.paramSet("/reader/gen2/q", setQ);
        }
        StopOnTagCount sotc = new StopOnTagCount();
        sotc.N = 5; // number of tags to read
        StopTriggerReadPlan strp;
        if(enableSimpleReadplan)
        {
            if(model.equalsIgnoreCase("M3e"))
            {
                strp = new StopTriggerReadPlan(sotc, antennaList, TagProtocol.ISO14443A);
            }
            else
            {
               isDynamicSwEnable = false; // supported only for M3e. Hence making it false for other readers.
               strp = new StopTriggerReadPlan(sotc, antennaList, TagProtocol.GEN2); 
            }
            r.paramSet("/reader/read/plan", strp);
        }
        else
        {
            // In case of sync read,dynamic protocol switching is not supported for M3e.So, it should fall back to multi protocol sync read similar to UHF
            if(enableSyncRead)
            {
                isDynamicSwEnable = false;
            }
            TagProtocol[] protocolList = (TagProtocol[]) r.paramGet("/reader/version/supportedProtocols");
            if(model.equalsIgnoreCase("M3e") && isDynamicSwEnable)
            {
                //Set the multiple protocols using TMR_PARAM_PROTOCOL_LIST param for dynamic protocol switching
                r.paramSet(TMConstants.TMR_PARAM_PROTOCOL_LIST, protocolList);
                
                // If TMR_PARAM_PROTOCOL_LIST param is set, API ignores the protocol mentioned in readplan.
                strp = new StopTriggerReadPlan(sotc, antennaList, TagProtocol.ISO14443A);
                r.paramSet("/reader/read/plan", strp);
            }
            else
            {
                StopTriggerReadPlan rp[] = new StopTriggerReadPlan[protocolList.length];
                for (int i = 0; i < protocolList.length; i++)
                {
                    rp[i] = new StopTriggerReadPlan(sotc, antennaList, protocolList[i]);
                }
                MultiReadPlan testMultiReadPlan = new MultiReadPlan(rp);
                r.paramSet("/reader/read/plan", testMultiReadPlan);
            }
        }
        // Sync Read
        if(enableSyncRead)
        {
            // Read tags
            tagReads = r.read(1000);
            // Print tag reads
            for (TagReadData tr : tagReads)
            {
                System.out.println(tr.toString() + " protocol:" + tr.getTag().getProtocol());
                totalTagCount += tr.getReadCount();
            }
        }
        // Async Read
        else
        {
            //create and add exception listener
            ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
            r.addReadExceptionListener(exceptionListener);
            // Create and add tag listener
            ReadListener rl = new PrintListener();
            r.addReadListener(rl);
            // search for tags in the background
            r.startReading();
            
            while(!r.isReadStopped())
            {
                //Wait till tag read completion
                Thread.sleep(1);
            }

            r.removeReadListener(rl);
            r.removeReadExceptionListener(exceptionListener);
        }
        System.out.println("\nTotal tag count: " + totalTagCount);

        // Shut down reader
        r.destroy();
    } 
    catch (ReaderException re)
    {
      System.out.println("Reader Exception : " + re.getMessage());
    }
    catch (Exception re)
    {
      System.out.println("Exception : " + re.getMessage());
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

    // Print Listener
    static class PrintListener implements ReadListener
    {
        public void tagRead(Reader r, TagReadData tr)
        {
          totalTagCount += tr.getReadCount();
          System.out.println("Background read: " + tr.toString() + " protocol:" + tr.getTag().getProtocol());
        }
    }
    // Exception Listener
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
    
}
