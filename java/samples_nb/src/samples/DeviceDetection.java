/*
 * Sample program that shows information of all connected thingmagic serial readers 
 */
package samples;

import com.fazecast.jSerialComm.SerialPort;
import com.thingmagic.Reader;
import com.thingmagic.ReaderException;
import com.thingmagic.SerialReader;
import com.thingmagic.TMConstants;
import com.thingmagic.TransportListener;
/**
 *
 * @author pchinnapapannagari
 */
public class DeviceDetection
{
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
    
   public static void main(String argv[]) throws ReaderException
   {
       Reader r = null;
       try
       {
          SerialPort[] portList = SerialPort.getCommPorts();
          String[] results = new String[portList.length];
          for (int i=0; i<portList.length;i++) 
          {
              results[i] = portList[i].getSystemPortName();
              if (results[i].contains("COM"))
              {
                 try
                 {
                    r = Reader.create("tmr:///"+ results[i]);
                    r.paramSet(TMConstants.TMR_PARAM_TRANSPORTTIMEOUT, 100);
                    r.paramSet(TMConstants.TMR_PARAM_COMMANDTIMEOUT, 100);
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
                    if (Reader.Region.UNSPEC == (Reader.Region) r.paramGet(TMConstants.TMR_PARAM_REGION_ID))
                    {
                       Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
                       if (supportedRegions.length < 1)
                       {
                          throw new Exception("Reader doesn't support any regions");
                       }
                       else
                       {
                          r.paramSet(TMConstants.TMR_PARAM_REGION_ID, supportedRegions[0]);
                       }
                    }
                    r.addTransportListener(r.simpleTransportListener);
                    try
                    {
                       String version_hardware = (String) r.paramGet(TMConstants.TMR_PARAM_VERSION_HARDWARE);
                       System.out.println("Hardware Version :" + version_hardware);
                    } 
                    catch (Exception ex)
                    {
                       System.out.println("Hardware Version :" + ex.getMessage());
                    }
                    try
                    {
                       String version_serial = (String) r.paramGet(TMConstants.TMR_PARAM_VERSION_SERIAL);
                       System.out.println("Serial Version :" + version_serial);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Serial Version :" + ex.getMessage());
                    }
                    try
                    {
                       String version_model = (String) r.paramGet(TMConstants.TMR_PARAM_VERSION_MODEL);
                       System.out.println("Model Version  :" + version_model);
                    }  
                    catch (Exception ex)
                    {
                       System.out.println("Model Version :" + ex.getMessage());
                    }
                    try
                    {
                       String version_software = (String) r.paramGet(TMConstants.TMR_PARAM_VERSION_SOFTWARE);
                       System.out.println("Software Version :" + version_software);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Software Version :" + ex.getMessage());
                    }
                    try
                    {
                       String reader_uri = (String) r.paramGet(TMConstants.TMR_PARAM_READER_URI);
                       System.out.println("Reader Uri :" + reader_uri);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Reader Uri :" + ex.getMessage());
                    }
                    try
                    {
                       int reader_productgroupid = (Integer) r.paramGet(TMConstants.TMR_PARAM_READER_PRODUCTGROUPID);
                       System.out.println("Reader Product GroupId :" + reader_productgroupid);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Reader Product GroupId :" + ex.getMessage());
                    }
                    try
                    {
                       String reader_productgroup = (String) r.paramGet(TMConstants.TMR_PARAM_READER_PRODUCTGROUP);
                       System.out.println("Reader Product Group :" + reader_productgroup);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Reader Product Group :" + ex.getMessage());
                    }
                    try
                    {
                       int reader_productid = (Integer) r.paramGet(TMConstants.TMR_PARAM_READER_PRODUCTID);
                       System.out.println("Reader Product Id :" + reader_productid);
                    }
                    catch (Exception ex)
                    {
                       System.out.println("Reader Product Id :" + ex.getMessage());
                    }
                }
                catch (Exception ex)
                {
                   //Exception raised because the device detected is unsupported
                }
              }
          }
       }
       catch (Exception ex)
       {
           System.out.println("Error :" + ex.getMessage());
       }
   }
}
