/**
 * Sample program that gets and prints the reader stats
 */
// Import the API
package samples;

import com.thingmagic.*;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class ReaderStatistics
{

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "ReaderStatistics [-v] [reader-uri] [--ant n[,n...]] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n" 
                + "Example for UHF: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' or '-v tmr:///com4 --ant 1,2'\n "
                + "Example for HF/LF: 'tmr:///com4'\n ");
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
        int[] antennaList = null;
        SimpleReadPlan plan;
        
        if (argv.length < 1)
        {
            usage();
        }

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
            
            String model = r.paramGet("/reader/version/model").toString();
            if (model.equalsIgnoreCase("M3e"))
            {
               // initializing the simple read plan with tag type
               plan = new SimpleReadPlan(antennaList, TagProtocol.ISO15693, null, null, 1000);
            }
            else
            {
               plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
            }
            // Set the created readplan
            r.paramSet("/reader/read/plan", plan);
            /** Request for the statics fields of your interest, before search
            * Temperature and Antenna port stats flags are mandatory for TMReader and it doesn't allow disabling these two flags**/
            SerialReader.ReaderStatsFlag[] READER_STATISTIC_FLAGS = {SerialReader.ReaderStatsFlag.ALL};

            r.paramSet(TMConstants.TMR_PARAM_READER_STATS_ENABLE, READER_STATISTIC_FLAGS);
            SerialReader.ReaderStatsFlag[] getReaderStatisticFlag = (SerialReader.ReaderStatsFlag[]) r.paramGet(TMConstants.TMR_PARAM_READER_STATS_ENABLE);
            if (READER_STATISTIC_FLAGS.equals(getReaderStatisticFlag))
            {
               System.out.println("GetReaderStatsEnable--pass");
            }
            else
            {
               System.out.println("GetReaderStatsEnable--Fail");
            }
            TagReadData[] tagReads;
            tagReads = r.read(500);
            // Print tag reads
            List<String> epcList = new ArrayList<String>();
            for (TagReadData tr : tagReads)
            {
                String epcString = tr.getTag().epcString();
                System.out.println(tr.toString());
                if (!epcList.contains(epcString))
                {
                    epcList.add(epcString);
                }
            }

        SerialReader.ReaderStats readerStats = (SerialReader.ReaderStats) r.paramGet(TMConstants.TMR_PARAM_READER_STATS);

        try
        {
            if(Arrays.asList(getReaderStatisticFlag).contains(SerialReader.ReaderStatsFlag.CONNECTED_ANTENNA_PORTS) ||
                    (Arrays.asList(getReaderStatisticFlag).contains(SerialReader.ReaderStatsFlag.ALL) && (!model.equalsIgnoreCase("M3e"))))
            {

                int[] connectedAntennaPorts = readerStats.connectedAntennaPorts;
                if(connectedAntennaPorts.length > 0)
                {
                    System.out.print("\nAntenna Connection status");
                    for(int i = 0; i < connectedAntennaPorts.length; i+=2)
                    {
                        System.out.print("\nAntenna " + connectedAntennaPorts[i] + " | " + ((connectedAntennaPorts[i+1] == 1) ?" Connected" : " Disconnected"));
                    }
                }
                System.out.print("\n");
            }
        }
        catch(Exception e)
        {
            e.printStackTrace();
        }

        if(readerStats.noiseFloorTxOn != null)
        {
            byte[] noiseFloorTxOn = readerStats.noiseFloorTxOn;
            for (int antenna = 0; antenna < noiseFloorTxOn.length; antenna++)
            {
              System.out.println("NOISE_FLOOR_TX_ON for antenna [" + (antenna + 1) + "] is : " + noiseFloorTxOn[antenna] +" db");
            }
        }

        if(readerStats.rfOnTime != null)
        {
            int[] rfontimes = readerStats.rfOnTime;
            for (int antenna = 0; antenna < rfontimes.length; antenna++)
            {
              System.out.println("RF_ON_TIME for antenna [" + (antenna + 1) + "] is : " + rfontimes[antenna] +" ms");
            }
        }

        if(readerStats.frequency!=0)
        {
            System.out.println("Frequency   :  " + readerStats.frequency + " kHz");
        }
        if(readerStats.temperature!=0)
        {
            System.out.println("Temperature :  " + readerStats.temperature + " C");
        }
        if(readerStats.protocol!=null)
        {
            System.out.println("Protocol    :  " + readerStats.protocol);
        }
        if(readerStats.antenna!=0)
        {
            System.out.println("Connected antenna port : " + readerStats.antenna);
        }

        /* Get the antenna return loss value, this parameter is not the part of reader stats */
        if (!model.equalsIgnoreCase("M3e"))
        {
           int[][] returnLoss=(int[][]) r.paramGet(TMConstants.TMR_PARAM_ANTENNA_RETURNLOSS);
           for (int[] rl : returnLoss)
           {
               System.out.println("Antenna ["+rl[0] +"] returnloss :"+ rl[1]);
           }
        }
        r.destroy();
        }
        catch (ReaderException re)
        {
            if(r!=null)
            {
                r.destroy();
            }
            System.out.println("Reader Exception : " + re.getMessage());
        } 
        catch (Exception re)
        {
            if(r!=null)
            {
                r.destroy();
            }
            System.out.println("Exception : " + re.getMessage());
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
