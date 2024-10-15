/*
 * Sample program that reboots the reader
 * 
 */
package samples;

import com.thingmagic.*;


public class RebootReader 
{
      
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Reboot [-v] [reader-uri] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n");
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
      if (trace)
      {
        setTrace(r, new String[] {"on"});
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
      r.reboot();      
      r.destroy();
      boolean notConnected = true;
      int retryCount =1;
      do
      {
          try
          {
              System.out.println("Trying to reconnect.... Attempt:"+retryCount);
              r = Reader.create(argv[nextarg]);
              if (trace)
              {
                setTrace(r, new String[] {"on"});
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
              notConnected = false;
          }
          catch(Exception ex)
          {
          }
          finally
          {
              r.destroy();
          }
          retryCount++; 
      }while(notConnected);
      System.out.println("Reader is reconnected successfully");
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
}
