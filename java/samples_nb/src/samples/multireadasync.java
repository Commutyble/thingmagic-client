/**
 * Sample program that reads tags in the background and prints the tags found.
 */
// Import the API
package samples;

import com.thingmagic.*;
import java.util.HashMap;
import java.util.Map;
import java.util.logging.Level;

public class multireadasync
{

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "multireadasync [-v] [reader-uri1] [--ant n[,n...]] [reader-uri2] [--ant n[,n...]]  \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n" +
                  " e.g:'tmr:///COM1 --ant 1,2 tmr:///COM2 --ant 1,2 or 'tmr:///COM1 --ant 1,2 tmr://10.11.115.36 --ant 1,2\n");
        System.exit(1);
    }
    
     public static void setTrace(Reader r, String args[])
    {    
        if (args[0].toLowerCase().equals("on"))
        {
           r.addTransportListener(r.simpleTransportListener);
        }    
    }

    public static void main(String argv[])
    {

        // Create Reader object, connecting to physical device
        try
        {
            int[] antennaList = null;
            String readerName = null;
            Map<String, int[]> readerPort = new HashMap<String, int[]>();           
            TagReadData[] tags;
            int nextarg = 0;
            boolean trace = false;

            if (argv.length < 1)
               usage();

            if (argv[nextarg].equals("-v"))
            {
               trace = true;
               nextarg++;
            }
            
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
                    if (readerName != null)
                    {
                        readerPort.put(readerName, antennaList);
                        antennaList = null;
                    }
                    readerName = arg;
                }
            }

            if (readerName != null)
            {
                readerPort.put(readerName, antennaList);
                readerName = null;
                antennaList = null;
            }
           
            Reader[] r = new Reader[readerPort.size()];
            int i =0;
            for (String reader: readerPort.keySet())
            {
                r[i] = Reader.create(reader);
                if (trace)
                {
                   setTrace(r[i], new String[] {"on"});
                }
                r[i].connect();
                if (Reader.Region.UNSPEC == (Reader.Region) r[i].paramGet("/reader/region/id"))
                {
                    Reader.Region[] supportedRegions = (Reader.Region[]) r[i].paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
                    if (supportedRegions.length < 1)
                    {
                        throw new Exception("Reader doesn't support any regions");
                    }
                    else
                    {
                        r[i].paramSet("/reader/region/id", supportedRegions[0]);
                    }
                }
                SimpleReadPlan plan = new SimpleReadPlan(readerPort.get(reader), TagProtocol.GEN2, null, null, 1000);
                r[i].paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
               
                // Create and add tag listener
                ReadListener rl = new PrintListener();
                r[i].addReadListener(rl);

                ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
                r[i].addReadExceptionListener(exceptionListener);

                // search for tags in the background
                r[i].startReading();
                i++;
            }
            Thread.sleep(1000);

            for (i = 0; i < readerPort.size(); i++)
            {
                r[i].stopReading();
                // Shut down reader
                r[i].destroy();
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
    }

    static class PrintListener implements ReadListener
    {

        public void tagRead(Reader r, TagReadData tr)
        {
            try
            {
                System.out.println(r.paramGet("/reader/uri").toString()+" Background read: " + tr.toString());
            }
            catch (ReaderException ex)
            {
                java.util.logging.Logger.getLogger(multireadasync.class.getName()).log(Level.SEVERE, null, ex);
            }
        }

    }

    static class TagReadExceptionReceiver implements ReadExceptionListener
    {

        public void tagReadException(com.thingmagic.Reader r, ReaderException re)
        {
            System.out.println("Reader Exception: " + re.getMessage());
            if (re.getMessage().equals("Connection Lost"))
            {
                System.exit(1);
            }
        }
    }

    static int[] parseAntennaList(String[] args, int argPosition)
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
