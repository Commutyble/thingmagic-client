/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

package samples;
import com.thingmagic.*;
/**
 *
 * @author rsoni
 */
public class BlockPermalock
{
  static SerialPrinter serialPrinter;
  static StringPrinter stringPrinter;
  static TransportListener currentListener;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "BlockPermalock [-v] [reader-uri] [--ant n[,n...]] \n" +
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
public static void main(String argv[]) throws ReaderException
  {

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

        //Use first antenna for tag operation
        if (antennaList != null)
            r.paramSet("/reader/tagop/antenna", antennaList[0]);

        /* readLock bits indicates whether to show the permalock status bits or to permalock one or more blocks.
         * If readLock = 0x00, reads the permalock status bits.
         * If readLock = 0x01, permalocks one or more blocks within the memory bank specified.
         */
        byte readLock = 0x00;
        
        /* mask is a 16-bit value and each bit corresponds to a particular block.  Mask will be taken into account if readLock = 0x01, i.e., permalock action.
         * If user wants to permalock block - 0 , bit 0 should be set to 1. Hence mask should be given as 0x8000(in binary 1000 0000 0000 0000)
         * Similarly to permalock blocks - 0, 1 and 2, set those bit fields to 1. Hence mask should be given as 0xe000 (in binary 1110 0000 0000 0000)
         */
        short[] mask = new short[]{(short)0x8000};  

        Gen2.BlockPermaLock tagop = new Gen2.BlockPermaLock(Gen2.Bank.USER, readLock, 0, (byte) 1, mask);
        byte[] data = (byte[]) r.executeTagOp(tagop, null);

        // Retrieve the permalock status bits
        if(readLock == 0x00)
        {
            System.out.println("Permalock bits: ");
            for (int i = 0; i < data.length; i++)
            {
                System.out.printf("%02x" , data[i]);
                System.out.println("");
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
