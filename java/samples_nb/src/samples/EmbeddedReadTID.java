package samples;

import com.thingmagic.*;
import java.io.IOException;

public class EmbeddedReadTID
{
    static SerialPrinter serialPrinter;
    static StringPrinter stringPrinter;
    static TransportListener currentListener;

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "EmbeddedReadTID [-v] [reader-uri] [--ant n[,n...]] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n"
                + "e.g: tmr:///com1 --ant 1,2 ; tmr://10.11.115.32 --ant 1,2\n ");
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

    public static void main(String argv[]) throws IOException
    {
        // Program setup
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
        // Create Reader object, connecting to physical device
        try
        {

            TagReadData[] tagReads;

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
            
            byte length;
            String model = r.paramGet("/reader/version/model").toString();
            
            if ("M6e".equals(model) || "M6e Micro".equals(model) || "M6e PRC".equals(model) || "M6e JIC".equals(model) || "Mercury6".equals(model) || "Astra-EX".equals(model) || "Sargas".equals(model))
            {
                // Specifying the readLength = 0 will return full TID for any tag read in case of M6e, M6,Astra-EX and Sargas readers.
                length = 0;
            }
            else
            {
                length = 2;
            }
            TagOp op = new Gen2.ReadData(Gen2.Bank.TID, 0, length);
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, op, 1000);
            r.paramSet("/reader/read/plan", plan);
            // Read tags
            tagReads = r.read(500);
            for (TagReadData tr : tagReads)
            {
                System.out.println("Tag ID: " + tr.epcString());
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
        catch (ReaderException re)
        {
            System.out.println("ReaderException: " + re.getMessage());
        }
        catch (Exception re)
        {
            System.out.println("Exception: " + re.getMessage());
        }
        // Shut down reader
        r.destroy();
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
