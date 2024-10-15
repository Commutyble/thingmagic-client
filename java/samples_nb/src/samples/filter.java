/**
 * Sample program that demonstrates different types and uses of
 * TagFilter objects.
 */

// Import the API
package samples;
import com.thingmagic.*;

public class filter
{
  static SerialPrinter serialPrinter;
  static StringPrinter stringPrinter;
  static TransportListener currentListener;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "filter [-v] [reader-uri] [--ant n[,n...]] \n" +
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
  public static void main(String argv[])
  {
    // Program setup
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

    // Create Reader object, connecting to physical device
    try
    {

        TagReadData[] tagReads, filteredTagReads;
        TagFilter filter;
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
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
        r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
        //Use first antenna for tag operation
        if (antennaList != null)
        {
            r.paramSet("/reader/tagop/antenna", antennaList[0]);
        }

      // In the current system, sequences of Gen2 operations require Session 0,
        // since each operation resingulates the tag.  In other sessions,
        // the tag will still be "asleep" from the preceding singulation.
        Gen2.Session oldSession = (Gen2.Session) r.paramGet("/reader/gen2/session");
        Gen2.Session newSession = Gen2.Session.S0;
        System.out.println("Changing to Session " + newSession + " (from Session " + oldSession + ")");
        r.paramSet("/reader/gen2/session", newSession);
        System.out.println();

        try
        {
            System.out.println("Unfiltered Read:");
            // Read the tags in the field
            tagReads = r.read(500);
            for (TagReadData tr : tagReads)
            {
                System.out.println(tr.toString());
            }
            System.out.println();

            if (0 == tagReads.length)
            {
                System.out.println("No tags found.");
            }
            else
            {
                // A TagData object may be used as a filter, for example to
                // perform a tag data operation on a particular tag.
                System.out.println("Filtered Tagop:");
                // Read kill password of tag found in previous operation
                filter = tagReads[0].getTag();
                System.out.printf("Read kill password of tag %s\n", filter);
                Gen2.ReadData tagop = new Gen2.ReadData(Gen2.Bank.RESERVED, 0, (byte) 2);
                try
                {
                    short[] data = (short[]) r.executeTagOp(tagop, filter);
                    for (short word : data)
                    {
                        System.out.printf("%04X", word);
                    }
                    System.out.println();
                }
                catch (ReaderException re)
                {
                    System.out.printf("Can't read tag: %s\n", re);
                }
                System.out.println();

                // Filter objects that apply to multiple tags are most useful in
                // narrowing the set of tags that will be read. This is
                // performed by setting a read plan that contains a filter.
                // A TagData with a short EPC will filter for tags whose EPC
                // starts with the same sequence.
                filter = new TagData(tagReads[0].getTag().epcString().substring(0, 4));
                System.out.printf("EPCs that begin with %s\n", filter);
                r.paramSet("/reader/read/plan",
                        new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, 1000));
                filteredTagReads = r.read(500);
                for (TagReadData tr : filteredTagReads)
                {
                    System.out.println(tr.toString());
                }
                System.out.println();

                // A filter can also be a full Gen2 Select operation.  For
                // example, this filter matches all Gen2 tags where bits 8-19 of
                // the TID are 0x003 (that is, tags manufactured by Alien
                // Technology). In case of Network readers, ensure that bitLength is
                // a multiple of 8.
                System.out.println("Tags with Alien Technology TID");
                filter = new Gen2.Select(false, Gen2.Bank.TID, 8, 12, new byte[] {0, 0x30});
                r.paramSet("/reader/read/plan",
                        new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, 1000));
                System.out.printf("Reading tags with a TID manufacturer of 0x003\n",
                        filter.toString());
                filteredTagReads = r.read(500);
                for (TagReadData tr : filteredTagReads)
                {
                    System.out.println(tr.toString());
                }
                System.out.println();

                // Gen2 Select may also be inverted, to give all non-matching tags
                // In case of Network readers, ensure that bitLength is a multiple of 8.
                System.out.println("Tags without Alien Technology TID");
                filter = new Gen2.Select(true, Gen2.Bank.TID, 8, 12, new byte[] {0, 0x30});
                r.paramSet("/reader/read/plan",
                        new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, 1000));
                System.out.printf("Reading tags with a TID manufacturer of 0x003\n",
                        filter.toString());
                filteredTagReads = r.read(500);
                for (TagReadData tr : filteredTagReads)
                {
                    System.out.println(tr.toString());
                }
                System.out.println();
                
                /*
                * A filter can also perform Gen2 Truncate Select operation. 
                * Truncate indicates whether a Tag’s backscattered reply shall be truncated to those EPC bits that follow Mask.
                * For example, truncated select starting with PC word start address 0x10 and length of 32 bits
                */
                if (r.getClass().getName() == ("com.thingmagic.SerialReader")) 
                {
                    filter = new Gen2.Select(false, Gen2.Bank.GEN2EPCTRUNCATE, 0x10, 0x20, new byte[]{(byte) 0x30, (byte) 0x00,
                        (byte) 0xAB, (byte) 0xCD});
                    r.paramSet("/reader/read/plan",
                            new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, 1000));
                    System.out.printf("Reading tags with Gen2 Truncate operation to those EPC's that follows the mask\n",
                            filter.toString());
                    filteredTagReads = r.read(500);
                    for (TagReadData tr : filteredTagReads) {
                        System.out.println(tr.toString());
                    }
                    System.out.println();

                    /*
                     * A filter can also perform Gen2 Tag Filtering.
                     * Major advantage of this feature is to limit the EPC response to user specified length field and all others will be rejected by firmware.
                     * invert, bitPointer, mask : Parameters will be ignored when GEN2EPCLENGTHFILTER is used
                     * maskBitLength : Specified EPC Length used for filtering
                     * For example, Tag filtering will be applied on EPC with 128 bits length, rest of the tags will be ignored
                     */
                    filter = new Gen2.Select(false, Gen2.Bank.GEN2EPCLENGTHFILTER, 16, 128, new byte[]{(byte) 0x30, (byte) 0x00});
                    r.paramSet("/reader/read/plan",
                            new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, 1000));
                    System.out.printf("Reading tags with Gen2 Tag EPC filtering operation \n",
                            filter.toString());
                    filteredTagReads = r.read(500);
                    for (TagReadData tr : filteredTagReads) {
                        System.out.println(tr.toString());
                    }
                    System.out.println();
                }
                // Filters can also be used to match tags that have already been
                // read. This form can only match on the EPC, as that's the only
                // data from the tag's memory that is contained in a TagData
                // object.
                // Note that this filter has invert=true. This filter will match
                // tags whose bits do not match the selection mask.
                // Also note the offset - the EPC code starts at bit 32 of the
                // EPC memory bank, after the StoredCRC and StoredPC.
                // In case of Network readers, ensure that bitLength is a multiple of 8.
                filter = new Gen2.Select(true, Gen2.Bank.EPC, 32, 2,new byte[] {(byte)0xC0});
                System.out.println("EPCs with first 2 bits equal to zero (post-filtered):");
                for (TagReadData tr : tagReads) // unfiltered tag reads from the first example
                {
                    if (filter.matches(tr.getTag()))
                    {
                        System.out.println(tr.toString());
                    }
                }
                System.out.println();

                {
                    /**
                     * Multi filter creation and initialization. This filter will match those tags whose bits matches the
                     * selection mask of both tidFilter(tags manufactured by Alien Technology) and epcFilter(epc of first
                     * tag read from tagReads[0]). Target and action are the two new parameters of Gen2.Select class whose
                     * default values could be "Gen2.Select.Target.Select" and "Gen2.Select.Action.ON_N_OFF" respectively
                     * if not provided by the user.
                     *
                     * Gen2 Select Action indicates which Select action to take
                     * (See Gen2 spec /Select commands / Tag response to Action parameter)
                     * |-----------------------------------------------------------------------------|
                     * |  Action  |        Tag Matching            |        Tag Not-Matching         |
                     * |----------|--------------------------------|---------------------------------|
                     * |   0x00   |  Assert SL or Inventoried->A   |  Deassert SL or Inventoried->B  |
                     * |   0x01   |  Assert SL or Inventoried->A   |  Do nothing                     |
                     * |   0x02   |  Do nothing                    |  Deassert SL or Inventoried->B  |
                     * |   0x03   |  Negate SL or (A->B,B->A)      |  Do nothing                     |
                     * |   0x04   |  Deassert SL or Inventoried->B |  Assert SL or Inventoried->A    |
                     * |   0x05   |  Deassert SL or Inventoried->B |  Do nothing                     |
                     * |   0x06   |  Do nothing                    |  Assert SL or Inventoried->A    |
                     * |   0x07   |  Do nothing                    |  Negate SL or (A->B,B->A)       |
                     * -------------------------------------------------------------------------------
                     *
                     * To improve readability and ease typing, these names abbreviate the official terminology of the Gen2 spec.
                     * <A>_N_<B>: The "_N_" stands for "Non-Matching".
                     * The <A> clause before the _N_ describes what happens to Matching tags.
                     * The <B> clause after the _N_ describes what happens to Non-Matching tags.
                     * (Alternately, you can pronounce "_N_" as "and", or "&"; i.e.,
                     * the pair of Matching / Non-Matching actions is known as "<A> and <B>".)
                     *
                     * ON: assert SL or inventoried -> A
                     * OFF: deassert SL or inventoried -> B
                     * NEG: negate SL or (A->B, B->A)
                     * NOP: do nothing
                     *
                     * The enum is simply a transliteration of the Gen2 spec's table: "Tag response to Action parameter"
                     */

                    // create and initialize tidFilter.
                    // In case of Network readers, ensure that bitLength is a multiple of 8.
                    Gen2.Select tidFilter = new Gen2.Select(false, Gen2.Bank.TID, 8, 12, new byte[] {(byte)0x00, (byte)0x30});
                    tidFilter.target = Gen2.Select.Target.Select;
                    tidFilter.action = Gen2.Select.Action.ON_N_OFF;

                    // create and initialize epcFilter
                    Gen2.Select epcFilter = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, ReaderUtil.hexStringToByteArray(tagReads[0].epcString()));
                    epcFilter.target = Gen2.Select.Target.Select;
                    epcFilter.action = Gen2.Select.Action.ON_N_OFF;

                    // Initialize multifilter with tagFilter array containing list of filters
                    MultiFilter multiFilter = new MultiFilter(new TagFilter[]{tidFilter, epcFilter});
                    r.paramSet("/reader/read/plan", new SimpleReadPlan(antennaList, TagProtocol.GEN2, multiFilter, 1000));
                    System.out.printf("Reading tags which matches multi filter criteria \n",filter.toString());
                    filteredTagReads = r.read(500);
                    for (TagReadData tr : filteredTagReads) 
                    {
                        System.out.println(tr.toString());
                    }
                    System.out.println();
                }
            }
      }
      finally
      {
        // Restore original settings
        System.out.println("Restoring Session " + oldSession);
        r.paramSet("/reader/gen2/session", oldSession);
      }

      // Shut down reader
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
