/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */

/** Sample program to set session, target and select per antenna.
 */

// Import the API
package samples;
import com.thingmagic.*;
import samples.readasync.PrintListener;
import samples.readasync.TagReadExceptionReceiver;

public class CustomAntennaConfig
{  
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "read [-v] [reader-uri] [--ant n[,n...]] \n" +
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
      r.addTransportListener(r.simpleTransportListener);
    }    
  }
 
  public static void main(String argv[])
  {
    //Program setup
    Reader r = null;
    int nextarg = 0;
    boolean trace = false;
    int[] antennaList = null;
    int antennaCount = 2;
    CustomAntConfig customAntConfig;
    CustomAntConfigPerAntenna[] customAntConfigPerAntennas;
    CustomAntConfigPerAntenna customConfigPerAnt1;
    CustomAntConfigPerAntenna customConfigPerAnt2;
    int antSwitchingType = 1;
    int tagReadTimeout = 50000;
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
        //Set filter
        Gen2.Select epcFilter1 = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, new byte[]{(byte)0x66, (byte)0x66});
        epcFilter1.target = Gen2.Select.Target.Inventoried_S1;
        epcFilter1.action = Gen2.Select.Action.ON_N_NOP;
        
        Gen2.Select epcFilter2 = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, new byte[]{(byte)0x11, (byte)0x11});
        epcFilter2.target = Gen2.Select.Target.Inventoried_S1;
        epcFilter2.action = Gen2.Select.Action.ON_N_NOP;
        //Set per antenna config
        customConfigPerAnt1 = new CustomAntConfigPerAntenna(Gen2.Session.S1, Gen2.Target.A, (TagFilter)epcFilter1, 1);
        customConfigPerAnt2 = new CustomAntConfigPerAntenna(Gen2.Session.S1, Gen2.Target.A, (TagFilter)epcFilter2, 2);
        customAntConfigPerAntennas = new CustomAntConfigPerAntenna[antennaCount];
        customAntConfigPerAntennas[0] = customConfigPerAnt1;
        customAntConfigPerAntennas[1] = customConfigPerAnt2;
        /**
         * @param antennaCount - number of antennas
         * @param customAntConfigPerAntennas - List of customConfigPerAnt
         * @param fastSearch
         * @param antSwitchingType - Equal(any other number except 1) or Dynamic(1)
         * @param tagReadTimeout - timeout to read tags on antenna
         **/
        customAntConfig = new CustomAntConfig(antennaCount, customAntConfigPerAntennas, false, antSwitchingType, tagReadTimeout);
        //Create read plan with custome antenna config
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, 1000, customAntConfig);
        r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
        // Read tags
        ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
        r.addReadExceptionListener(exceptionListener);
        // Create and add tag listener
        ReadListener rl = new PrintListener();
        r.addReadListener(rl);
        // search for tags in the background
        r.startReading(); 
        Thread.sleep(500);
        r.stopReading();
        r.removeReadListener(rl);
        r.removeReadExceptionListener(exceptionListener);
        // Shut down reader
        r.destroy();
    } 
    catch (ReaderException re)
    {
      System.out.println("Reader Exception: " + re.getMessage());
    }
    catch (Exception re)
    {
        System.out.println("Exception: " + re.getMessage());
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

