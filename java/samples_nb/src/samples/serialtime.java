/**
 * Sample program that reads tags for a fixed period of time (500ms)
 * and prints the tags found, while logging the serial message with timestamps.
 */

// Import the API
package samples;
import com.thingmagic.*;
import java.text.SimpleDateFormat;
import java.util.Date;

public class serialtime
{
  static TimestampSerialPrinter serialPrinter;
  static TimestampStringPrinter stringPrinter;
  static TransportListener currentListener;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "serialtime [-v] [reader-uri] [--ant n[,n...]] \n" +
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
      if(r instanceof SerialReader)
      {
        currentListener = new TimestampSerialPrinter();
      }
      else
      {
        currentListener = new TimestampStringPrinter();
      }
      // Add the serial-reader or LLRP reader specific message logger based on reader type
      r.addTransportListener(currentListener);
    }
    else if (currentListener != null)
    {
        r.removeTransportListener(Reader.simpleTransportListener);
    }
  }

   static class TimestampSerialPrinter implements TransportListener
  {
    public void message(boolean tx, byte[] data, int timeout)
    {
      System.out.print("[" + new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS").format(new Date(System.currentTimeMillis())) + "]");
      System.out.print(tx ? "  Sending:\n" : "  Received:\n");
      for (int i = 0; i < data.length; i++)
      {
        if (i > 0 && (i & 15) == 0)
          System.out.printf("\n");
        System.out.printf(" %02x", data[i]);
      }
      System.out.printf("\n");
    }
  }

  static class TimestampStringPrinter implements TransportListener
  {
    public void message(boolean tx, byte[] data, int timeout)
    {
      System.out.print("[" + new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS").format(new Date(System.currentTimeMillis())) + "]");
      System.out.println((tx ? "  Sending:\n" : "  Receiving:\n") +
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
        r.connect();
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
       
        // Read tags
        tagReads = r.read(500);
        // Print tag reads
        for (TagReadData tr : tagReads)
        {
            System.out.println(tr.toString());
        }
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

  static class TimestampListener implements TransportListener
  {
    public void message(boolean tx, byte[] data, int timeout)
    {
      
      System.out.printf("%d %s\n  ",
                        new Date().getTime(),
                        tx ? "Sending" : "Received");
      for (int i = 0; i < data.length; i++)
      {
        if (i > 0 && (i & 15) == 0)
          System.out.printf("\n  ");
        System.out.printf(" %02x", data[i]);
      }
      System.out.printf("\n");
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

