/**
 * Sample program that writes an EPC to a tag and 
 * demonstrates the functionality of read after write.
 */

// Import the API
package samples;
import com.thingmagic.*;
import com.thingmagic.Reader.*;
import com.thingmagic.SerialReader.*;
import java.util.*;

public class writetag
{
  static SerialPrinter serialPrinter;
  static StringPrinter stringPrinter;
  static TransportListener currentListener;
  private static int[] antennaList = null;
  private static Reader r = null;

  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as: "
                + "writetag [-v] reader-uri [--ant n][--rw][--filter] \n"
                + "-v Verbose: Turn on transport listener \n"
                + "reader-uri Reader URI: e.g., 'tmr:///COM1' or 'tmr://astra-2100d3' or 'tmr:///dev/ttyS0/'\n"
                + "--ant Antenna List : e.g., '--ant 1' \n"
                + "--rw Enables ReadAfterWrite functionality \n"
                + "--filter Enables filtering \n"
                + "Example for UHF: 'tmr:///COM1 --ant 1' or 'tmr:///COM1 --ant 1 --rw' or 'tmr:///COM1 --ant 1 --rw --filter' \n"
                + "Example for HF/LF: 'tmr:///COM1' or 'tmr:///COM1 --rw' or 'tmr:///COM1 --rw --filter'");
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
    TagFilter target = null;
    int nextarg = 0;
    boolean trace = false;
    boolean enableReadAfterWrite = false;
    boolean enableFilter = false;
    boolean enableEmbeddedReadAfterWrite = false;
    boolean enableM3eFilter = false;
    boolean enableM3eBlockReadWrite = true;
    boolean enableM3eSetAccessPwd = false;
    boolean enableM3eGetSystemInfo = false;
    /** Enable this to read secure id of tag.
     *  Only embedded tag op is supported. Standalone is not supported
     */
    boolean enableM3eSecureIDEmbeddedRead = false;
    boolean enableM3eBlkProtectionStatus = false;
    MultiFilter multiFilter = null;
    TagFilter tagTypeFilter = null;
    TagFilter uidFilter = null; 

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
                if(arg.equalsIgnoreCase("--rw"))
                {
                    // Enables read after write functionality
                    enableReadAfterWrite = true;
                    
                }

                else if(argv[nextarg].equalsIgnoreCase("--filter"))
                {
                    // Enables filtering 
                    enableFilter = true;
                }
                else
                {
                    System.out.println("Argument " + argv[nextarg] + " is not recognised");
                    usage();
                }
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

        String model = (String) r.paramGet("/reader/version/model");
        if (!model.equalsIgnoreCase("M3e"))
        { 
           //Use first antenna for tag operation
            if (antennaList != null)
                r.paramSet("/reader/tagop/antenna", antennaList[0]);
        }

        if(enableFilter)
        {
            // This select filter matches all Gen2 tags where bits 32-48 of the EPC are 0x0123 
            target = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, new byte[] {(byte)0x01,(byte)0x23});
        }
        /*
        Gen2.TagData epc = new Gen2.TagData(new byte[]
           {(byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB,
            (byte)0xCD, (byte)0xEF, (byte)0x01, (byte)0x23, (byte)0x45, (byte)0x67,
            });
        Gen2.WriteTag tagop = new Gen2.WriteTag(epc);
        r.executeTagOp(tagop, target);
        */
        // Reads data from a tag memory bank after writing data to the requested memory bank without powering down of tag
        
        if(enableReadAfterWrite)
        {
            //create a tagopList with write tagop followed by read tagop
            TagOpList tagopList = new TagOpList();
            short[] readData;
            byte wordCount;
            
            //Write one word of data to USER memory and read back 8 words from EPC memory
            {

                // write data
                short writeData[] =
                {
                     (short) 0x1234
                };
                wordCount = 8;
                Gen2.WriteData wData = new Gen2.WriteData(Gen2.Bank.USER, 2, writeData);
                Gen2.ReadData rData = new Gen2.ReadData(Gen2.Bank.EPC, 0, wordCount);

                // assemble tagops into list
                tagopList.list.add(wData);
                tagopList.list.add(rData);

                // call executeTagOp with list of tagops
                readData = (short[])r.executeTagOp(tagopList, target);
                System.out.println("ReadData after WriteData is successful.");
                System.out.println("ReadData: ");
                for (short dt : readData)
                {
                    System.out.printf("%04x \t", dt);
                }
                System.out.println("\n");

                // enable flag enableEmbeddedReadAfterWrite to execute embedded read after write 
                if(enableEmbeddedReadAfterWrite)
                {
                    performEmbeddedOperation(target, TagProtocol.GEN2, tagopList);
                }
            }

            //clearing the list for next operation
            tagopList.list.clear();

            //Write 12 bytes(6 words) of EPC and read back 8 words from EPC memory 
            {
                Gen2.TagData epc1 = new Gen2.TagData(new byte[]
                   {(byte) 0x11, (byte) 0x22, (byte) 0x33, (byte) 0x44, (byte) 0x55, (byte) 0x66,
                    (byte)0x77, (byte)0x88, (byte)0x99, (byte)0xaa, (byte)0xbb, (byte)0xcc,
                    });

                wordCount = 8;

                Gen2.WriteTag wtag = new Gen2.WriteTag(epc1);
                Gen2.ReadData rData = new Gen2.ReadData(Gen2.Bank.EPC, 0, wordCount);

                // assemble tagops into list
                tagopList.list.add(wtag);
                tagopList.list.add(rData);

                // call executeTagOp with list of tagops
                readData = (short[])r.executeTagOp(tagopList, target);
                System.out.println("ReadData after WriteTag is successful.");
                System.out.println("ReadData: ");
                for (short dt : readData)
                {
                    System.out.printf("%04x \t", dt);
                }
                System.out.println("\n");

                // enable flag enableEmbeddedReadAfterWrite to execute embedded read after write
                if(enableEmbeddedReadAfterWrite)
                {
                    performEmbeddedOperation(target, TagProtocol.GEN2, tagopList);
                }
            }
        }
        if (model.equalsIgnoreCase("M3e"))
        {
            // Perform read and apply first tag found as filter
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.ISO15693, null, null, 1000);
            r.paramSet("/reader/read/plan", plan);
            TagReadData[] tagReads = r.read(1000);
            System.out.println("UID: " + tagReads[0].getTag().epcString());
            System.out.println("TagType: " + SerialReader.tagTypeSet15693(tagReads[0].TagType()));
            MemoryType type;
            int address;
            byte length;

            if (enableM3eFilter)
            {
                //Initialize filter
                tagTypeFilter = new Select_TagType(tagReads[0].TagType());
                uidFilter = new Select_UID(32, ReaderUtil.hexStringToByteArray(tagReads[0].getTag().epcString().substring(0, 8)));
                multiFilter = new MultiFilter(new TagFilter[] {tagTypeFilter, uidFilter});
            }
            if (enableM3eBlockReadWrite)
            {
                //Initialize all the fields required for BlockWrite Tag operation
                type = MemoryType.TAG_MEMORY;
                address = 0;
                length = 1;
                byte[] data = new byte[]{(byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44};

                // Read data before block write
                ReadMemory bRead = new ReadMemory(type, address, length);

                // Enable and set the password before execute tag op
                if(enableM3eSetAccessPwd)
                {
                    //Set password
                    byte accessPwd[] = new byte[]{(byte)0xff, (byte)0xff, (byte)0xff, (byte)0xff};
                    r.setAccessPassword(bRead, accessPwd);
                }
                byte[] dataRead = (byte[])r.executeTagOp(bRead, multiFilter);

                // prints the data read
                System.out.println("Read Data before performing block write:");
                for (byte i : dataRead)
                {
                    System.out.printf("%02x\t", i);
                }
                System.out.printf("\n");
                //Uncomment to enable embedded read
                //performEmbeddedOperation(multiFilter, TagProtocol.ISO15693, bRead);

                // Initialize block write
                WriteMemory writeOp = new WriteMemory(type, address, data);

                // Execute the tagop
                r.executeTagOp(writeOp, multiFilter);

                //Read data after block write
                ReadMemory readOp = new ReadMemory(type, address, length);
                byte[] readData = (byte[])r.executeTagOp(readOp, multiFilter);

                // prints the data read
                System.out.println("Read Data after performing block write operation: ");
                for (byte i : readData)
                {
                    System.out.printf("%02x\t", i);
                }
                System.out.printf("\n");
            }
            if(enableM3eGetSystemInfo)
            {
                //Get the system information of tag. Address and length fields have no significance if memory type is BLOCK_SYSTEM_INFORMATION_MEMORY.
                type = MemoryType.TAG_INFO;
                address = 0;
                length = 0;
                // Read data before block write
                ReadMemory sysInfoOp = new ReadMemory(type, address, length);
                byte[] systemInfo = (byte[])r.executeTagOp(sysInfoOp, multiFilter);
                if (systemInfo.length > 0)
                {
                    parseGetSystemInfoResponse(systemInfo);
                }
            }
            if(enableM3eSecureIDEmbeddedRead)
            {
                //Read secure id of tag. Address and length fields have no significance if memory type is SECURE_ID.
                type = MemoryType.SECURE_ID;
                address = 0;
                length = 0;
                //Initialize the ReadMemory tag op 
                ReadMemory secureIdOp = new ReadMemory(type, address, length);

                // perform embedded tag operation for secureId read as standalone is not supported.
                performEmbeddedOperation(multiFilter, TagProtocol.ISO15693, secureIdOp);
            }
            if(enableM3eBlkProtectionStatus)
            {
                // Get the block protection status of block 0.
                type = MemoryType.PROTECTION_SECURITY_STATUS;
                address = 0;
                length = 1;
                ReadMemory blkProtectionOp = new ReadMemory(type, address, length);
                byte[] statusData = (byte[])r.executeTagOp(blkProtectionOp, multiFilter);

                // parse the block protection status response.
                if (statusData.length == length)
                {
                    parseGetBlockProtectionStatusResponse(statusData, address, length);
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

    public static void performEmbeddedOperation(TagFilter filter, TagProtocol protocol, TagOp op) throws Exception
    {
        TagReadData[] tagReads = null;
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, protocol, filter, op, 1000);
        r.paramSet("/reader/read/plan", plan);
        tagReads = r.read(1000);
        for (TagReadData tr : tagReads)
        {
            if (0 < tr.getData().length)
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
        }
    }
 
    public static void parseGetSystemInfoResponse(byte[] systemInfo)
    {
        int readIndex = 0;
        // Extract 1 byte of Information Flags from response
        byte infoFlags = systemInfo[readIndex++];

        //Extract UID - 8 bytes for Iso15693
        int uidLength = 8;
        byte[] uid = new byte[uidLength];
        System.arraycopy(systemInfo, readIndex, uid, 0, uidLength);
        System.out.println("UID: " + ReaderUtil.byteArrayToHexString(uid));
        readIndex += uidLength;
        if (infoFlags == 0)
        {
            System.out.println("No information flags are enabled");
        }
        else
        {
            // Checks Information flags are supported or not and then extracts respective fields information.
            if ((infoFlags & 0x0001) == 0x0001)
            {
                System.out.println("DSFID is supported and DSFID field is present in the response");
                //Extract 1 byte of DSFID
                byte dsfid = systemInfo[readIndex++];
                System.out.println("DSFID: " + String.valueOf(dsfid));
            }
            if ((infoFlags & 0x0002) == 0x0002)
            {
                System.out.println("AFI is supported and AFI field is present in the response");
                //Extract 1 byte of AFI
                byte afi = systemInfo[readIndex++];
                System.out.println("AFI: " + String.valueOf(afi));
            }
            if ((infoFlags & 0x0004) == 0x0004)
            {
                System.out.println("VICC memory size is supported and VICC field is present in the response");
                //Extract 2 bytes of VICC information - converts byte array to 16 bit int
                int viccInfo = ((systemInfo[readIndex] & 0xff) <<  8)| ((systemInfo[readIndex + 1] & 0xff) <<  0);
                byte maxBlockCount = (byte)(viccInfo & 0xFF); // holds the number of blocks
                System.out.println("Max Block count: " + maxBlockCount);
                byte blocksize = (byte)((viccInfo & 0x1F00) >> 8); // holds blocksize
                System.out.println("Block Size: " + blocksize);
                readIndex += 2;
            }
            if ((infoFlags & 0x0008) == 0x0008)
            {
                System.out.println("IC reference is supported and IC reference is present in the response");
                // Extract 1 byte of IC reference
                byte icRef = systemInfo[readIndex++];
                System.out.println("IC Reference: " + String.valueOf(icRef));
            }
        }
    }
    public static void parseGetBlockProtectionStatusResponse(byte[] data, int address, byte length)
    {
        byte lockStatus;
        for (int i = 0; i < length; i++)
        {
            lockStatus = data[i];
            // Block lock status
            if ((lockStatus & 0x01) == 0x01)
            {
               System.out.println("Block" + address + "is locked");
            }
            else
            {
               System.out.println("Block" + address + "is not locked");
            } 
            // Read Password protection status
            if ((lockStatus & 0x02) == 0x02)
            {
               System.out.println("Read password protection is enabled for block" + address);
            }
            else
            {
               System.out.println("Read password protection is disabled for block" + address);
            }
            //write password protection status
            if ((lockStatus & 0x04) == 0x04)
            {
               System.out.println("Write password protection is enabled for block" + address);
            }
            else
            {
               System.out.println("Write password protection is disabled for block" + address);
            }
            // Page protection lock status
            if ((lockStatus & 0x08) == 0x08)
            {
               System.out.println("Page protection is locked for block {0}" + address);
            } 
            else
            {
               System.out.println("Page protection is not locked for block" + address);
            }
            address++;
        } 
    }
}
