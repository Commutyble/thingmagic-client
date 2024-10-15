/**
 * Sample program that reads tags in the background and track tags
 * that have been seen; only print the tags that have not been seen
 * before.
 */

// Import the API
package samples;
import com.thingmagic.*;

import java.util.HashSet;

public class readasynctrack
{
  static SerialPrinter serialPrinter;
  static StringPrinter stringPrinter;
  static TransportListener currentListener;
  static int uniqueCount = 0;
  static int totalCount = 0;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "readasynctrack [-v] [reader-uri] [--ant n[,n...]] \n" +
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
        r.connect();
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
        String model = (String)r.paramGet("/reader/version/model");
        SimpleReadPlan plan;
        // Create a simplereadplan which uses the antenna list created above
        if (model.equalsIgnoreCase("M3e"))
        {
            // initializing the simple read plan with tag type
            plan = new SimpleReadPlan(antennaList, TagProtocol.ISO14443A, null, null, 1000);
        }
        else
        {
            plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
        }
        r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
            
        // Create and add tag listener
        ReadListener rl = new PrintNewListener();
        r.addReadListener(rl);

        // Search for tags in the background
        r.startReading();
        Thread.sleep(1000); // Run for a while so we see some tags repeatedly
        r.stopReading();

        r.removeReadListener(rl);
        System.out.println("Unique Tags:" + uniqueCount + " Total Tags: " + totalCount);
        // Shut down reader
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

  static class PrintNewListener implements ReadListener
  {
    HashSet<TagData> seenTags = new HashSet<TagData>();

    public void tagRead(Reader r, TagReadData tr)
    {
      TagData t = tr.getTag();
      if (!seenTags.contains(t))
      {
          uniqueCount++;
          System.out.println("New tag: " + t.epcString());
          seenTags.add(t);
      }
      totalCount++;
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
