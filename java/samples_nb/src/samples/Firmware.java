/*
 * Sample program that flash the firmware on module
 * 
 */
package samples;

import com.thingmagic.FirmwareLoadOptions;
import com.thingmagic.LLRPFirmwareLoadOptions;
import com.thingmagic.LLRPReader;
import com.thingmagic.ReadListener;
import com.thingmagic.Reader;
import com.thingmagic.ReaderException;
import com.thingmagic.RqlFirmwareLoadOptions;
import com.thingmagic.RqlReader;
import com.thingmagic.SerialReader;
import com.thingmagic.TMConstants;
import com.thingmagic.TagReadData;
import java.io.FileInputStream;
import java.io.InputStream;


public class Firmware {   
      
  static void usage()
  {
      
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Firmware [-v] [reader-uri] [--fw] [path of the fw] --erase/revert \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--fw: Update the firmware\n" +
                  "--erase  Erase: Wipe device program memory before installing new firmware(applicable to n/w readers only)\n"+
                  "--revert  Revert: Wipe device configuration(applicable to n/w readers only)\n"
                + "e.g: tmr:///com1 --fw filepath ; tmr://10.11.115.32 --fw filepath --erase/revert \n ");
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
        try
        {
            // Program setup
            Reader r = null;
            
            boolean trace = false;
            FileInputStream fileStream;
            boolean erase = false;
            boolean revert = false;
            boolean argError = false;
            String fwFilename = null;
            String readerUri = null;
            String arg = null;
            
            if (argv.length < 1)
            {
                usage();
            }
            
            for (int nextarg = 0; nextarg < argv.length; nextarg++) 
            {
                arg = argv[nextarg];
                if (arg.equalsIgnoreCase("-v"))
                {
                    trace = true;                    
                }
                else if (arg.equalsIgnoreCase("--erase"))
                {
                    erase = true;                    
                }
                else if (arg.equalsIgnoreCase("--revert")) 
                {
                    revert = true;
                }
                else if (arg.equalsIgnoreCase("--fw")) 
                {
                    fwFilename = argv[++nextarg];                    
                }
                else if (readerUri == null)
                {
                    readerUri = arg;                    
                }                
                else 
                {
                    System.out.println("Argument " + arg + " is not recognized");
                    argError = true;                    
                }                
            }
            if (readerUri == null) 
            {
                System.out.println("Reader URI not specified");                
                argError = true;
            }
            if (fwFilename == null) 
            {
                System.out.println("Firmware filename not specified");                
                argError = true;
            }
            if (argError) 
            {
                usage();
                System.exit(1);
            }

            // Create Reader object, connecting to physical device       
            r = Reader.create(readerUri);
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

                if (ex.getMessage().equalsIgnoreCase("CRC validation of firmware image failed")) 
                {
                    System.out.println("Current image failed CRC check. Proceeding to load firmware, anyway.");                    
                }
                else if (ex.getMessage().equalsIgnoreCase("The last word of the firmware image stored in the reader's " 
                                                           + "flash ROM does not have the correct address value.")) 
                {
                    System.out.println("The last word of the firmware image stored in the reader's "
                            + "flash ROM does not have the correct address value. Proceeding to load firmware, anyway.");                    
                }
                else if (ex.getMessage().equalsIgnoreCase("Timeout")) 
                {
                    System.out.println("Connection to reader failed. Proceeding to load firmware, anyway.");                    
                }
                else if (ex.getMessage().equalsIgnoreCase("Software assert.")) 
                {
                    System.out.println("Assert failed. Proceeding to load firmware, anyway.");                    
                }
                else
                {
                    throw new Exception(ex);
                }
            }
            
            if (r instanceof RqlReader || r instanceof LLRPReader) 
            {
                fileStream = new FileInputStream(fwFilename);
                FirmwareLoadOptions flo = new LLRPFirmwareLoadOptions(erase, revert);
                System.out.println("Loading firmware...");
                r.firmwareLoad(fileStream, flo);
            } 
            else if (r instanceof SerialReader) 
            {
                fileStream = new FileInputStream(fwFilename);
                System.out.println("Loading firmware...");
                r.firmwareLoad(fileStream);
                fileStream.close();
            }
            String version = (String) r.paramGet("/reader/version/software");
            System.out.println("Firmware load successful with version " + version);
            // Shut down reader
            r.destroy();
        } 
        catch (ReaderException re) 
        {
            if (re.getMessage().equals("Invalid firmware load arguments")) 
            {
                usage();
                System.exit(1);
            } 
            else if (re.getMessage().equalsIgnoreCase("Timeout")) 
            {
                System.out.println("Error: The firmware loading might have been corrupted. Please try loading it again.");
            }
            else 
            {
                System.out.println("Reader Exception : " + re.getMessage());
            }
        } 
        catch (Exception re)
        {
            System.out.println("Exception : " + re.getMessage());
        }
    }    
}
