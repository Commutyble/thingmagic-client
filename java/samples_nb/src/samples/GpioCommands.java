/*
 * Sample program that shows GPIO operations.
 */
package samples;

import com.thingmagic.*;


public class GpioCommands
{
    
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "GpioCommands [-v] [reader-uri] [Gpio options] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "1) get-gpi: --Read input pins\n"+
                  "2) set-gpo: --Write output pins e.g.,set-gpo 1 1 2 0\n"+
                      "   set-gpo pin value ... where pin -- Pin number, value -- Pin state:1 to set pin high, 0 to set pin low\n" +
                  "3) testgpiodirection: -- verify gpio directionality\n"
                + "e.g: tmr:///com1 get-gpi ; tmr:///com2 set-gpo 1 0 2 1 ; tmr:///com3 testgpiodirection\n ");
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
    // Program setup
    Reader r = null;
    int nextarg = 0;
    boolean trace = false;

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
        r = Reader.create(argv[nextarg]);
        nextarg++;
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

        String arg = argv[nextarg];
        if (arg.equalsIgnoreCase("get-gpi"))
        {
            Reader.GpioPin[] state = r.gpiGet();

            for (Reader.GpioPin gp : state)
            {
                System.out.printf("Pin %d: %s\n", gp.id, gp.high ? "High" : "Low");
            }
        }
        else if (arg.equalsIgnoreCase("set-gpo"))
        {
            nextarg++;
            for(;nextarg < argv.length; nextarg++)
            {
                try
                {
                    int pin = Integer.parseInt(argv[nextarg]);
                    nextarg++;
                    boolean value = parseBool(argv[nextarg]);
                    r.gpoSet(new Reader.GpioPin[]{new Reader.GpioPin(pin, value)});
                    System.out.println("Set-GPO success with pin:" + pin + " " + value);
                }
                catch (IndexOutOfBoundsException iobe)
                {
                    System.out.println("Missing argument after args " + argv[nextarg]);
                    usage();
                }
            }
        }
        else if (arg.equalsIgnoreCase("testgpiodirection"))
        {
            int[] input = new int[] { 1,2 };
            int[] output = new int[] { 1,2 };
            r.paramSet("/reader/gpio/inputList", input);
            System.out.println("Input list set.");
            r.paramSet("/reader/gpio/outputList", output);
            System.out.println("Output list set.");
            
            int[] inputList = (int[]) r.paramGet("/reader/gpio/inputList");
            for (int i : inputList)
            {
                System.out.println("input list "+ i);
            }

            int[] outputList = (int[]) r.paramGet("/reader/gpio/outputList");
            for (int i : outputList)
            {
                System.out.println("output list " + i);
            }
        }
        else
        {
            System.out.println("Argument "+arg+" is not recognised");
            usage();
        }
        r.destroy();
    }
    catch(IndexOutOfBoundsException iob)
    {
      usage();
    }
    catch(IllegalArgumentException ie)
    {
        System.out.println("Illegal Argument Exception : " + ie.getMessage());
        usage();
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
  
  static boolean parseBool(String boolString)
  {
    String s = boolString.toLowerCase();

    if (s.equals("true") || s.equals("high") || s.equals("1"))
    {
      return true;
    }
    else if (s.equals("false") || s.equals("low") || s.equals("0"))
    {
      return false;
    }
    else
    {
        throw new IllegalArgumentException("Can't parse " + s +" as GPO state. Allowed values are 0(low) and 1(high).");
    }
  }
}
