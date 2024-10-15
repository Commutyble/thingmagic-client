/*
 * Sample program that shows how to execute 
 * TIMED or CONTINUOUS mode of CW/PRBS
 * 
 */
package samples;

import com.thingmagic.*;

public class RegulatoryTest extends Thread
{
    static void usage()
    { 
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "RegulatoryTest [-v] [reader-uri] [--ant n[,n...]] [--mode regulatory_mode] [--modulation regulatory_modulation] [--ontime regulatory_ontime] [--offtime regulatory_offtime]n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n" +
                  "--mode: CONTINUOUS/TIMED \n"+
                  "--modulation: CW/PRBS \n" +
                  "--ontime: set on time\n" +
                  "--offtime: set off time\n" +
                  "e.g:tmr:///com4 --ant 1 --mode CONTINUOUS --modulation CW --ontime 1000 --offtime 500'\n");
        System.exit(1);
    }

    public static void setTrace(Reader r, String args[])
    {
        if (args[0].toLowerCase().equals("on"))
        {
            r.addTransportListener(r.simpleTransportListener);
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

    static class SerialPrinter implements TransportListener
    {
        public void message(boolean tx, byte[] data, int timeout)
        {
            System.out.print(tx ? "Sending: " : "Received:");
            for (int i = 0; i < data.length; i++)
            {
                if (i > 0 && (i & 15) == 0) {
                    System.out.printf("\n         ");
                }
                System.out.printf(" %02x", data[i]);
            }
            System.out.printf("\n");
        }
    }
    private static volatile boolean stopRequested = false;
    public static Reader r = null;
    public static void main(String argv[])
    {
        int[] antennaList = null;
        int nextarg = 0;
        boolean trace = false;
        Reader.RegulatoryMode regMode = null;
        Reader.RegulatoryModulation regModulation= null;
        int regOnTime = 0;
        int regOffTime = 0;
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
                else if(arg.equalsIgnoreCase("--mode"))
                {
                    String mode = argv[++nextarg];
                    if(mode.equalsIgnoreCase("TIMED"))
                    {
                        regMode = Reader.RegulatoryMode.TIMED;
                    }
                    else
                    {
                        regMode = Reader.RegulatoryMode.CONTINUOUS;
                    }
                }
                else if(arg.equalsIgnoreCase("--modulation"))
                {
                    String modulation = argv[++nextarg];
                    if(modulation.equalsIgnoreCase("CW"))
                    {
                        regModulation = Reader.RegulatoryModulation.CW;
                    }
                    else
                    {
                        regModulation = Reader.RegulatoryModulation.PRBS;
                    }
                }
                else if(arg.equalsIgnoreCase("--onTime"))
                {
                    String onTime = argv[++nextarg];
                    regOnTime = Integer.parseInt(onTime);
                }
                else if(arg.equalsIgnoreCase("--offTime"))
                {
                    String offTime = argv[++nextarg];
                    regOffTime = Integer.parseInt(offTime);
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
            r.connect();
            if (Reader.Region.UNSPEC == (Reader.Region) r.paramGet(TMConstants.TMR_PARAM_REGION_ID))
            {
                Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
                if (supportedRegions.length < 1) {
                    throw new Exception("Reader doesn't support any regions");
                } else {
                    r.paramSet(TMConstants.TMR_PARAM_REGION_ID, supportedRegions[0]);
                }
            }
            r.paramSet("/reader/regulatory/mode", regMode); 
            r.paramSet("/reader/regulatory/modulation", regModulation);
            r.paramSet("/reader/regulatory/onTime", regOnTime);
            r.paramSet("/reader/regulatory/offTime", regOffTime);
            
            System.out.println("!!!!! ALERT !!!!");
            System.out.println("Module may get hot when RF ON time is more than 10 seconds");
            System.out.println("Risk of damage to the module despite auto cut off feature");

            r.paramSet(TMConstants.TMR_PARAM_COMMANDTIMEOUT, (regOnTime + regOffTime));
            
            if(regMode.toString().equalsIgnoreCase("TIMED"))
            {
                r.paramSet("/reader/regulatory/enable", true);
                for(int iterations = 0; iterations < regOnTime/1000; iterations++)
                {
                    Thread.sleep(1000);
                    System.out.println("Temperature: " + r.paramGet(TMConstants.TMR_PARAM_RADIO_TEMPERATURE));
                }
            }
            else
            {
                RegulatoryTest regTest = new RegulatoryTest();
                Thread thread = new Thread(regTest);
                r.paramSet("/reader/regulatory/enable", true);
                thread.start();
                Thread.sleep(5000); 
                stopRequested = true;
                r.paramSet("/reader/regulatory/enable", false);
            }
        }
        catch (ReaderException re) 
        {
            if(re.getMessage().equalsIgnoreCase("The module has exceeded the maximum or minimum operating temperature "
                    + "and will not allow an RF operation until it is back in range"))
            {
                System.out.println("Reader temperature is too high. Turning OFF RF");
                try
                {
                    r.paramSet("/reader/regulatory/enable", false);
                }
                catch(Exception e)
                {
                    System.out.println(e.getMessage());
                }
            }
             System.out.println(re.getMessage());
        }
        catch (Exception ex) 
        {
            System.out.println(ex.getMessage());
        }
        finally
        {
            // Shut down reader
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

    public void run()
    {
       synchronized(this)
       {
            while(!stopRequested)
            {
                 try
                 {
                     System.out.println("Temperature: " + r.paramGet(TMConstants.TMR_PARAM_RADIO_TEMPERATURE));
                     Thread.sleep(1000);
                 }
                 catch(ReaderException re)
                 {
                     if(re.getMessage().equalsIgnoreCase("The module has exceeded the maximum or minimum operating temperature "
                    + "and will not allow an RF operation until it is back in range"))
                     {
                        System.out.println("Reader temperature is too high. Turning OFF RF");
                        try 
                        {
                            r.paramSet("/reader/regulatory/enable", false);
                        }
                        catch (Exception e)
                        {
                            System.out.println(e.getMessage());
                        }
                     }
                 }
                 catch(InterruptedException ie)
                 {
                     System.out.println(ie.getMessage());
                 }
             }
        }
    }
}
