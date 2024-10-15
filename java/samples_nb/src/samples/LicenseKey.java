/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

package samples;
import com.thingmagic.*;
import com.thingmagic.Reader.LicenseOperation;
import java.util.Set;

/**
 *
 * @author rsoni
 */
public class LicenseKey
{
    static SerialPrinter serialPrinter;
    static StringPrinter stringPrinter;
    static TransportListener currentListener;
    static LicenseOperation op = new LicenseOperation();
    static byte[] byteLic = new byte[]{};
    static String readerURI;
    static void usage()
   {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Licensekey [-v] [reader-uri] [--option] [set/erase] [--key] <license Key>\n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--option: to select the options(1.set 2.erase)\n" +
                  "set: update given license e.g.,tmr:///COM4 --option set --key AB CD\n"+
                  "erase: erase: erase existing license e.g.,tmr:///COM4 --option erase '\n\n"
                + "e.g: tmr:///com1 --option set --key 112233 ; tmr://10.11.115.32 --option erase \n ");
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
      System.out.println((tx ? "Sending:\n" : "Receiving:\n") +
                         new String(data));
    }
  }
    public static void main(String argv[]) throws ReaderException
    {
        Reader r = null;
        int nextarg = 0;
        boolean trace = false;
        StringBuilder key = new StringBuilder(); 
        //String readerURI;
        int optionIndex = 0;
        int keyIndex = 0;
        int keyLength = 0;
        boolean optionFound = false;
        boolean keyFound = false;
        int startIndex = 0;
        int minArgc = 0;

        try
        {
            if (argv[nextarg].equals("-v"))
            {
                trace = true;
                nextarg++;
                startIndex = 2;
                minArgc = 4;
            }
            else
            {
                startIndex = 1;
                minArgc = 3;
            }

            readerURI = argv[nextarg];
            nextarg++;

            if (argv.length < minArgc)
            {
                usage();
            }
            else
            {
                for( nextarg = startIndex; nextarg < argv.length; nextarg++)
                {
                    /* check for license operation option */
                    if((!optionFound) && (argv[nextarg].equalsIgnoreCase("--option")))
                    {
                        optionFound = true;
                        /* parse license option provided by user */
                        parseLicenseOperationOption(nextarg, argv);
                        nextarg++;
                    }

                    /* check for license key */
                    else if((!keyFound) && (argv[nextarg].equalsIgnoreCase("--key")))
                    {
                        keyFound = true;
                        keyIndex = nextarg; /* Store the key index */ 

                        /* Calculate the license key length */
                        keyLength = calculateLicenseKeyLength(keyIndex, optionFound, argv.length, argv);
                        nextarg+= keyLength;

                        if(keyLength!=0)
                        {
                            /* parse the license key */
                            byteLic = parseLicenseKey(keyIndex, keyLength, argv);
                            op.key = byteLic;
                        }
                        else
                        {
                            System.out.println("License key not found");
                            usage();
                        }
                    }
                    else
                    {
                        System.out.println("Arguments are not recognized");
                        usage();
                    }
                }

                /* Error handling */
                if(!optionFound)
                { 
                  System.out.println("license operation option is not found");
                  usage();
                }
                else if ((op.option == Reader.LicenseOption.SET_LICENSE_KEY) && (!keyFound))
                { 
                  System.out.println("License key not found");
                  usage();
                }
            }
        }
        catch(Exception ex)
        {
            System.out.println("Exception: " + ex.getMessage());
        }

        try
        {
            r = Reader.create(readerURI);
            if (trace)
            {
                  setTrace(r, new String[]{"on"});
            }
            r.connect();
            if (Reader.Region.UNSPEC == (Reader.Region) r.paramGet("/reader/region/id")) {
                Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
                if (supportedRegions.length < 1) {
                    throw new Exception("Reader doesn't support any regions");
                } else {
                    r.paramSet("/reader/region/id", supportedRegions[0]);
                }
            }

            //Uncomment this to only "Set" licensekey.      
            /*System.out.println("Set Protocol License Key started.\n");
            r.paramSet("/reader/licensekey", byteLic);
            System.out.println("Set Protocol License Key succeeded.\n");*/

            // Manage License key param supports both setting and erasing the license.
            System.out.println("License operation started.\n");
            r.paramSet(TMConstants.TMR_PARAM_MANAGE_LICENSE_KEY, op);
            System.out.println("License operation succeeded.\n");
            // Report protocols enabled by current license key
            TagProtocol[] protocolList = ( TagProtocol[])r.paramGet("/reader/version/supportedProtocols");
            System.out.println("Supported Protocols:" );
            for(TagProtocol p : protocolList)
            {
                System.out.println(p);
            }
            String model = (String)r.paramGet("/reader/version/model");
            if (model.equalsIgnoreCase("M3e"))
            {
                Set<Reader.SupportedTagFeatures> tagFeatures;
                for(TagProtocol proto : protocolList)
                {
                    switch(proto)
                    {
                        case ISO14443A:
                            tagFeatures = (Set<Reader.SupportedTagFeatures>)r.paramGet("/reader/iso14443a/supportedTagFeatures");
                            System.out.println("ISO14443A Tag Features: " + tagFeatures);
                            break;
                        case ISO15693:
                            tagFeatures = (Set<Reader.SupportedTagFeatures>)r.paramGet("/reader/iso15693/supportedTagFeatures");
                            System.out.println("ISO15693 Tag Features: " + tagFeatures);
                            break;
                        case LF125KHZ:
                            tagFeatures = (Set<Reader.SupportedTagFeatures>)r.paramGet("/reader/lf125khz/supportedTagFeatures");
                            System.out.println("LF125KHZ Tag Features: " + tagFeatures);
                            break;
                        default:
                            System.out.println("No Tag Features are enabled");
                            break;
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
        finally
        {
            r.destroy();
        }
    }

    public static byte[] parseLicenseKey(int keyIndex, int keyLength, String[] argv)
    {
        /* parse license option provided by user */
        int keyStartPointer = keyIndex + 1;
        StringBuilder key = new StringBuilder();
        for (int i =0; i < keyLength; i++) {
            key.append(argv[keyStartPointer + i]);
        }
        String s = new String(key);
        byteLic = ReaderUtil.hexStringToByteArray(s);
        return byteLic;
    }
    
    public static void parseLicenseOperationOption(int index, String[] argv)
    {
        /* parse license option provided by user */
        String argument = argv[index + 1];
        if(argument.equalsIgnoreCase("SET"))
        {
            op.option = Reader.LicenseOption.SET_LICENSE_KEY;
        }
        else if(argument.equalsIgnoreCase("ERASE"))
        {
            op.option = Reader.LicenseOption.ERASE_LICENSE_KEY;
        }
        else
        {
            System.out.println("Unsupported license operation");
            usage();
        }
    }
    
    public static int calculateLicenseKeyLength(int index, boolean isOptionFound, int argCount, String[] argv )
    {
        int keyLen = 0;
        int nextIndex = 0;
        boolean nextArgIsOption = false;
        if(isOptionFound)
        {
            if(argv[0].equals("-v"))
            {
                keyLen = argCount - 5;
            }
            else
            {
                keyLen = argCount - 4;
            }
        }
        else
        {
            nextIndex = index + 1;
            for(; (index < argCount)&&(!nextArgIsOption) ; index++, nextIndex++)
            {
                if((argv[nextIndex].equalsIgnoreCase("--option")))
                {
                    nextArgIsOption = true;
                }
                else if((nextIndex + 1) == argCount)
                {
                    System.out.println("License operation option is not found");
                    usage();
                }
                else
                {
                    keyLen++;
                }
            }
            index--;
        }
        return keyLen;
    }

}
