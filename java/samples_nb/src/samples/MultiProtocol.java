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
 * @author rsoni
 */
public class MultiProtocol
{

    static SerialPrinter serialPrinter;
    static StringPrinter stringPrinter;
    static TransportListener currentListener;

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "MultiProtocol [-v] [reader-uri] [--ant n[,n...]] \n" +
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
                {
                    System.out.printf("\n         ");
                }
                System.out.printf(" %02x", data[i]);
            }
            System.out.printf("\n");
        }
    }

    static class StringPrinter implements TransportListener
    {
        public void message(boolean tx, byte[] data, int timeout)
        {
            System.out.println((tx ? "Sending:\n" : "Receiving:\n")
                    + new String(data));
        }
    }

    public static void main(String argv[]) throws ReaderException
    {
        Reader r = null;
        int nextarg = 0;
        boolean trace = false;
        int[] antennaList = null;

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
                else
                {
                    System.out.println("Argument "+argv[nextarg] +" is not recognised");
                    usage();
                }
            }
            
            r = Reader.create(readerURI);
            if (trace)
            {
                setTrace(r, new String[]{"on"});
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
            TagProtocol[] protocolList = (TagProtocol[]) r.paramGet("/reader/version/supportedProtocols");
            if(model.equalsIgnoreCase("M3e"))
            {
                //Set the multiple protocols using TMR_PARAM_PROTOCOL_LIST param for dynamic protocol switching
                r.paramSet(TMConstants.TMR_PARAM_PROTOCOL_LIST, protocolList);
                
                // If TMR_PARAM_PROTOCOL_LIST param is set, API ignores the protocol mentioned in readplan.
                SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.ISO14443A, null, null, 1000);
                r.paramSet("/reader/read/plan", plan);
            }
            else
            {
                ReadPlan rp[] = new ReadPlan[protocolList.length];
                for (int i = 0; i < protocolList.length; i++)
                {
                    rp[i] = new SimpleReadPlan(antennaList, protocolList[i], null, null, 10);
                }
                MultiReadPlan testMultiReadPlan = new MultiReadPlan(rp);
                r.paramSet("/reader/read/plan", testMultiReadPlan);
            }
            ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
            r.addReadExceptionListener(exceptionListener);
            // Create and add tag listener
            ReadListener rl = new PrintListener();
            r.addReadListener(rl);
            // search for tags in the background
            r.startReading();   
            System.out.println("Do other work here");
            Thread.sleep(500);
            System.out.println("Do other work here");
            Thread.sleep(500);
            r.stopReading();

            r.removeReadListener(rl);
            r.removeReadExceptionListener(exceptionListener);
        }
        catch (ReaderException re)
        {
            System.out.println("ReaderException: " + re.getMessage());
        }
        catch (Exception re)
        {
            System.out.println("Exception: " + re.getMessage());
        }
        finally
        {
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

    static class PrintListener implements ReadListener
    {
        public void tagRead(Reader r, TagReadData tr)
        {
          System.out.println("Background read: " + tr.toString() + " Protocol: " + tr.getTag().getProtocol());
        }
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
}
