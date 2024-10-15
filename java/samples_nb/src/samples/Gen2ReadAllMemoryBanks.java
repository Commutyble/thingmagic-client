/*
 * Sample program that demonstrates how to read all gen2 memory banks data
 */
package samples;

import com.thingmagic.*;
import java.util.EnumSet;
import java.util.Iterator;

public class Gen2ReadAllMemoryBanks
{
    
  private static Reader r = null;
  private static int[] antennaList = null;
  
  static void usage()
  {
    System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Gen2ReadAllMemoryBanks [-v] [reader-uri] [--ant n[,n...]] \n" +
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
      r.addTransportListener(r.simpleTransportListener);
    }    
  }
 
  
  public static void main(String argv[])
  {
    // Program setup
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

        byte length;
        String model = r.paramGet("/reader/version/model").toString();
 
        if ("M6e".equalsIgnoreCase(model)
                || "M6e PRC".equalsIgnoreCase(model)
                || "M6e JIC".equalsIgnoreCase(model)
                || "M6e Micro".equalsIgnoreCase(model)
                || "Mercury6".equalsIgnoreCase(model)
                || "Sargas".equalsIgnoreCase(model)
                || "Astra-EX".equalsIgnoreCase(model))
        {
            // Specifying the readLength = 0 will return full TID for any tag read in case of M6e varients, M6, Astra-EX and Sargas readers.
            length = 0;
        }
        else
        {
            length = 2;
        }
        
        Gen2ReadAllMemoryBanks program =new Gen2ReadAllMemoryBanks();
        program.performWriteOperation();
        
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
        r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);
        
        // Read tags
        tagReads = r.read(500);
        if(tagReads.length == 0)
        {
            System.out.println("No tags found"); 
        }
        
        TagFilter filter = new TagData(tagReads[0].epcString());
        System.out.println("Perform embedded and standalone tag operation - read only user memory without filter");
        TagOp op = new Gen2.ReadData(Gen2.Bank.USER, 0, length);
        program.performReadAllMemOperation(filter, op);
       
        EnumSet<Gen2.Bank> memBanks = EnumSet.of
        (
                Gen2.Bank.USER,
                Gen2.Bank.GEN2BANKUSERENABLED, Gen2.Bank.GEN2BANKRESERVEDENABLED,
                Gen2.Bank.GEN2BANKEPCENABLED, Gen2.Bank.GEN2BANKTIDENABLED
        );
      
       
        System.out.println("Perform embedded and standalone tag operation - read user memory, reserved memory, tid memory and epc memory without filter");
        op = null;
        op = new Gen2.ReadData(memBanks, 0, length);
        program.performReadAllMemOperation(null, op);

        System.out.println("Perform embedded and standalone tag operation - read only user memory with filter");
        op = null;
        op = new Gen2.ReadData(Gen2.Bank.USER, 0, length);
        program.performReadAllMemOperation(filter, op);

        memBanks = EnumSet.of(
                Gen2.Bank.USER,
                Gen2.Bank.GEN2BANKUSERENABLED, Gen2.Bank.GEN2BANKRESERVEDENABLED
        );
        System.out.println("Perform embedded and standalone tag operation - read user memory, reserved memory with filter");
        op = null;
        op = new Gen2.ReadData(memBanks, 0, length);
        program.performReadAllMemOperation(filter, op);

        System.out.println("Perform embedded and standalone tag operation - read user memory, reserved memory and tid memory with filter");
        memBanks = EnumSet.of(
                Gen2.Bank.USER,
                Gen2.Bank.GEN2BANKUSERENABLED, Gen2.Bank.GEN2BANKRESERVEDENABLED,
                Gen2.Bank.GEN2BANKTIDENABLED
        );
        op = null;
        op = new Gen2.ReadData(memBanks, 0, length);
        program.performReadAllMemOperation(filter, op);
        
        System.out.println("Perform embedded and standalone tag operation - read user memory, reserved memory, tid memory and epc memory with filter");
        memBanks = EnumSet.of(
                Gen2.Bank.USER,
                Gen2.Bank.GEN2BANKUSERENABLED, Gen2.Bank.GEN2BANKRESERVEDENABLED,
                Gen2.Bank.GEN2BANKEPCENABLED, Gen2.Bank.GEN2BANKTIDENABLED
        );

        op = null;
        op = new Gen2.ReadData(memBanks, 0, length);
        program.performReadAllMemOperation(filter, op);
        }
        catch (ReaderException re)
        {
          System.out.println("Reader Exception : " + re.getMessage());
        }
        catch (Exception re)
        {
            System.out.println("Exception : " + re.getMessage());
        }
        finally
        {
            // Shut down reader
            r.destroy();
        }
  }
  
 private void performReadAllMemOperation(TagFilter filter, TagOp op) throws ReaderException
    {
        TagReadData[] tagReads = null;
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, op, 1000);
        r.paramSet("/reader/read/plan", plan);
        Gen2.ReadData readOp = (Gen2.ReadData)op;
        System.out.println("Embedded tag operation - ");
        // Read tags
        tagReads = r.read(500);
        if(tagReads.length == 0)
        {
            System.out.println("No tags found"); 
        }
        else
        {
            for (TagReadData tr : tagReads)
            {
                System.out.println(tr.toString());
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
                        System.out.println(" Embedded read data: " + ReaderUtil.byteArrayToHexString(tr.getData()));
                        //If more than 1 memory bank is requested, data is available in individual membanks. Hence get data using those individual fields.
                        // As part of this tag operation, if any error is occurred for a particular memory bank, the user will be notified of the error using below code.
                        if (getMemBankValues(readOp)> 3)
                        {
                            if (tr.getUserMemData().length > 0)
                            {
                                System.out.println(" User memory : " + ReaderUtil.byteArrayToHexString(tr.getUserMemData()));
                            }
                            else
                            {
                                if(tr.getUserMemReadError() != -1)
                                    System.out.println(" User memory read failed with error: " + readOp.getReadDataError(tr.getUserMemReadError()));
                            }
                            if (tr.getReservedMemData().length > 0)
                            {
                                System.out.println(" Reserved memory Data: " + ReaderUtil.byteArrayToHexString(tr.getReservedMemData()));
                            }
                            else
                            {
                                if(tr.getReservedMemReadError() != -1)
                                    System.out.println(" Reserved memory read failed with error: " + readOp.getReadDataError(tr.getReservedMemReadError()));
                            }
                            if (tr.getTIDMemData().length > 0)
                            {
                                System.out.println(" Tid memory Data: " + ReaderUtil.byteArrayToHexString(tr.getTIDMemData()));
                            }
                            else
                            {
                                if(tr.getTidMemReadError() != -1)
                                    System.out.println(" Tid memory read failed with error: " + readOp.getReadDataError(tr.getTidMemReadError()));
                            }
                            if (tr.getEPCMemData().length > 0)
                            {
                                System.out.println(" EPC memory Data: " + ReaderUtil.byteArrayToHexString(tr.getEPCMemData()));
                            }
                            else
                            {
                                if(tr.getEpcMemReadError() != -1)
                                    System.out.println(" EPC memory read failed with error: " + readOp.getReadDataError(tr.getEpcMemReadError()));
                            }
                        }
                    }
                }
                System.out.println(" Embedded read data length:" + tr.dataLength/8);
            }

            System.out.println("Standalone tag operation - ");
            //Use first antenna for operation
            if (antennaList != null)
            {
                r.paramSet("/reader/tagop/antenna", antennaList[0]);
            }

            short[] data = (short[]) r.executeTagOp(op, filter);
            // Print tag reads
            if (0 < data.length)
            {
                System.out.println(" Standalone read data:"
                        + ReaderUtil.byteArrayToHexString(ReaderUtil.convertShortArraytoByteArray(data)));
                System.out.println(" Standalone read data length:" + (data.length)*2);
                // If more than one memory bank is enabled, parse each bank data individually using the below code.
                // As part of this tag operation, if any error is occurred for a particular memory bank, the user will be notified of the error using below code.
                if (getMemBankValues(readOp)> 3)
                {
                    parseStandaloneMemReadData(readOp, ReaderUtil.convertShortArraytoByteArray(data));
                }
            }
            data = null;
        }
    }
 
  private void performWriteOperation() throws ReaderException
  {
      //Use first antenna for operation
      if (antennaList != null)
      {
          r.paramSet("/reader/tagop/antenna", antennaList[0]);
      }

      Gen2.TagData epc = new Gen2.TagData(new byte[]
      {
          (byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB,
          (byte) 0xCD, (byte) 0xEF, (byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67,
      });
     
      System.out.println("Write on epc mem: " + epc.epcString());
      Gen2.WriteTag tagop = new Gen2.WriteTag(epc);
      r.executeTagOp(tagop, null);

      short[] data = new short[] { 0x1234, 0x5678 };
      
      System.out.println("Write on reserved mem: " + 
              ReaderUtil.byteArrayToHexString(ReaderUtil.convertShortArraytoByteArray(data)));
      Gen2.BlockWrite blockwrite = new Gen2.BlockWrite(Gen2.Bank.RESERVED, 0, (byte) data.length, data);
      r.executeTagOp(blockwrite, null);

      data = null;
      data = new short[] {(short) 0xFFF1, (short) 0x1122};
      
      System.out.println("Write on user mem: " + 
              ReaderUtil.byteArrayToHexString(ReaderUtil.convertShortArraytoByteArray(data)));
      blockwrite = new Gen2.BlockWrite(Gen2.Bank.USER, 0, (byte) data.length, data);
      r.executeTagOp(blockwrite, null);

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
    /***
     * This function is used to parse the standalone tag operation data
     *  and prints the individual memory bank data to console.
     * @param readOp - the Gen2.ReadData tag operation
     * @param data - the data bytes from the reader
     */ 
    static void parseStandaloneMemReadData(Gen2.ReadData readOp, byte[] data)
    {
        int readIndex = 0;
        while (data.length != 0)
        {
            if (readIndex >= data.length) 
            {
                break;
            }
            int bank = ((data[readIndex] >> 4) & 0x1F);
            Gen2.Bank memBank = Gen2.Bank.getBank(bank);
            int error = (data[readIndex] & 0x0F);
            int dataLength = data[readIndex + 1] * 2;

            switch (memBank)
            {
                case EPC:
                    byte[] epcData = new byte[dataLength];
                    System.arraycopy(data, (readIndex + 2), epcData, 0, dataLength);
                    if(epcData.length > 0)
                    {
                        System.out.println(" EPC memory Data: " + ReaderUtil.byteArrayToHexString(epcData));
                    }
                    else
                    {
                        System.out.println(" EPC memory read failed with error: " + readOp.getReadDataError(error));
                    }
                    readIndex += (2 + dataLength);
                    break;
                case RESERVED:
                    byte[] reservedData = new byte[dataLength];
                    System.arraycopy(data, (readIndex + 2), reservedData, 0, dataLength);
                    if(reservedData.length > 0)
                    {
                        System.out.println(" Reserved memory Data: " + ReaderUtil.byteArrayToHexString(reservedData));
                    }
                    else
                    {
                        System.out.println(" Reserved memory read failed with error: " + readOp.getReadDataError(error));
                    }
                    readIndex += (2 + dataLength);
                    break;
                case TID:
                    byte[] tidData = new byte[dataLength];
                    System.arraycopy(data, (readIndex + 2), tidData, 0, dataLength);
                    if(tidData.length > 0)
                    {
                        System.out.println(" Tid memory Data: " + ReaderUtil.byteArrayToHexString(tidData));
                    }
                    else
                    {
                        System.out.println(" Tid memory read failed with error: " + readOp.getReadDataError(error));
                    }
                    readIndex += (2 + dataLength);
                    break;
                case USER:
                    byte[] userData = new byte[dataLength];
                    System.arraycopy(data, (readIndex + 2), userData, 0, dataLength);
                    if(userData.length > 0)
                    {
                        System.out.println(" User memory Data: " + ReaderUtil.byteArrayToHexString(userData));   
                    }
                    else
                    {
                        System.out.println(" User memory read failed with error: " + readOp.getReadDataError(error));
                    }
                    readIndex += (2 + dataLength);
                    break;
                default:
                    break;
            }
        }
    }
    
    /***
     * This function is used to return the membank values of the read data tag operation
     * @param readOp
     * @return int value of membank
     */
    public int getMemBankValues(Gen2.ReadData readOp)
    {
        int bankValue = 0;
        if(readOp.Bank != null)
        {
            bankValue = readOp.Bank.rep;
        }
        if(readOp.banks != null)
        {
            EnumSet<Gen2.Bank> banks = readOp.banks;
            Iterator<Gen2.Bank> iterator = banks.iterator();
            while(iterator.hasNext())
            {
                bankValue |= iterator.next().rep;
            }
        }
        return bankValue;
    }
}
