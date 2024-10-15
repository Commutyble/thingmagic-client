/**
 * Sample program that reads tags for a fixed period of time (500ms)
 * and prints the tags found.
 */

// Import the API
package samples;
import com.thingmagic.*;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.EnumSet;
import java.util.Set;

public class read
{  
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "read [-v] [reader-uri] [--ant n[,n...]] \n" +
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
    boolean printTagMetaData = false;

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
        String model = (String)r.paramGet("/reader/version/model");
        SimpleReadPlan plan;
        // Metadata is not supported for M6 reader. Hence conditionalize here.
        if (!model.equalsIgnoreCase("Mercury6"))
        {
            Set<TagReadData.TagMetadataFlag> setMetaDataFlags = EnumSet.of(TagReadData.TagMetadataFlag.ALL);
            r.paramSet(TMConstants.TMR_PARAM_READER_METADATA, setMetaDataFlags);
        }
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
        // Set the created readplan
        r.paramSet("/reader/read/plan", plan);
        // Read tags
        tagReads = r.read(500);
        // Print tag reads
        for (TagReadData tr : tagReads)
        {
            System.out.println("Tag ID: " + tr.epcString());
            
            // Enable printTagMetaData to print meta data
            if (printTagMetaData) 
            {
                for (TagReadData.TagMetadataFlag metaData : TagReadData.TagMetadataFlag.values()) 
                {
                    if (tr.metadataFlags.contains(metaData))
                    {
                        switch(metaData)
                        {
                            case ANTENNAID:
                                System.out.println("Antenna ID: " + tr.getAntenna());
                                break;
                            case DATA:
                                // User should initialize Read Data
                                if (tr.getData().length > 0)
                                {
                                    if (tr.isErrorData)
                                    {
                                        // In case of error, show the error to user. Extract error code.
                                        byte[] errorCodeBytes = tr.getData();
                                        int offset = 0;
                                        //converts byte array to int value
                                        int errorCode = ((errorCodeBytes[offset] & 0xff) <<  8)| ((errorCodeBytes[offset + 1] & 0xff) <<  0);
                                        System.out.println("Embedded Tag operation failed. Error: " + new ReaderCodeException(errorCode));
                                    }
                                    else
                                    {
                                        System.out.println( String.format("Data[%d]: %s", 
                                                tr.dataLength/8, ReaderUtil.byteArrayToHexString(tr.getData())));
                                    }
                                }
                                break;
                            case FREQUENCY:
                                System.out.println("Frequency: " + tr.getFrequency());
                                break;
                            case GPIO_STATUS:
                                Reader.GpioPin[] state = tr.getGpio();
                                if(r.getClass().getName() == ("com.thingmagic.SerialReader"))
                                {
                                    System.out.printf("GPO Status: \n");
                                    for (Reader.GpioPin gp : state){
                                        System.out.printf("Pin %d: %s\n", gp.id, gp.high ? "High" : "Low");
                                    }
                                    System.out.printf("GPI Status: \n");
                                    for (Reader.GpioPin gp : state){
                                        System.out.printf("Pin %d: %s\n", gp.id, gp.output ? "High" : "Low");
                                    }
                                }
                                else
                                {
                                    System.out.printf("GPI Status: \n");
                                    for(int i=0; i<(state.length)/2; i++){
                                        System.out.printf("Pin %d: %s\n", state[i].id, state[i].high ? "High" : "Low");
                                    }
                                    System.out.printf("GPO Status: \n");
                                    for (int i=(state.length)/2; i<state.length; i++){
                                        System.out.printf("Pin %d: %s\n", state[i].id, state[i].high ? "High" : "Low");
                                    }
                                }
                                break;
                            case PHASE:
                                System.out.println("Phase: " + tr.getPhase());
                                break;
                            case PROTOCOL:
                                System.out.println("Protocol: " + tr.getTag().getProtocol());
                                break;
                            case READCOUNT:
                                System.out.println("ReadCount: " + tr.getReadCount());
                                break;
                            case RSSI:
                                System.out.println("RSSI: " + tr.getRssi());
                                break;
                            case TIMESTAMP:
                                System.out.println("Timestamp: " + new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ").format(new Date(tr.getTime())));
                                break;
                            case TAGTYPE:
                                switch (tr.getTag().getProtocol())
                                {
                                    case ISO14443A:
                                        System.out.println("TagType: " + SerialReader.tagTypeSet1443a(tr.TagType()));
                                        break;
                                    case ISO15693:
                                        System.out.println("TagType: " + SerialReader.tagTypeSet15693(tr.TagType()));
                                        break;
                                    case LF125KHZ:
                                        System.out.println("TagType: " + SerialReader.tagTypeSetLf125(tr.TagType()));
                                        break;
                                    case LF134KHZ:
                                        System.out.println("TagType: " + SerialReader.tagTypeSetLf134(tr.TagType()));
                                        break;
                                    default:
                                        System.out.println("TagType: " + tr.TagType());
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        if (TagProtocol.GEN2 == tr.getTag().getProtocol()) 
                        {
                            Gen2.TagReadData gen2 = (Gen2.TagReadData) (tr.prd);
                            switch (metaData) 
                            {
                                case GEN2_Q:
                                    System.out.println("Gen2Q: " + gen2.getGen2Q());
                                    break;
                                case GEN2_LF:
                                    System.out.println("Gen2LinkFrequency: " + gen2.getGen2LF());
                                    break;
                                case GEN2_TARGET:
                                    System.out.println("Gen2Target: " + gen2.getGen2Target());
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // Shut down reader
        r.destroy();
    } 
    catch (ReaderException re)
    {
        System.out.println("Reader Exception: " + re.getMessage());
        // In case of tag id buffer full exception, pull the tags from the module's buffer
        if(re instanceof ReaderCodeException && re.getMessage().equalsIgnoreCase("Tag ID buffer full."))
        {
            if(r instanceof SerialReader)
            {
                try
                {
                    // retrieve all the tags
                    TagReadData[] tagReads = ((SerialReader) r).getAllTagReadsFromBuffer().toArray(new TagReadData[0]);
                    
                    // Print tag reads
                    for (TagReadData tr : tagReads)
                    {
                        System.out.println("Tag ID: " + tr.epcString());
                    }
                }
                catch(ReaderException ex)
                {
                    System.out.println("Reader Exception while pulling tags from buffer: " + ex.getMessage());
                }
            }
        }
        // Shut down reader
        if(r!=null)
        {
            r.destroy();
        }

    }
    catch (Exception re)
    {
        // Shut down reader
        if(r!=null)
        {
            r.destroy();
        }
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
