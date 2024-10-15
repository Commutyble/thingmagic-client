/*
 * Sample program to demonstrate the functionality of passthrough 
 */
package samples;

import com.thingmagic.Reader;
import com.thingmagic.TransportListener;
import java.util.ArrayList;
import java.util.List;
import com.thingmagic.PassThrough;
import com.thingmagic.ReaderUtil;
import com.thingmagic.Select_UID;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;

/**
 *
 * @author bchanda
 */
public class PassThroughDemo
{
    public static final byte OPCODE_SELECT_TAG = 0x25;
    public static final byte GET_RANDOM_NUMBER = (byte)0xB2;
    public static final byte IC_MFG_CODE_NXP = 0x04;
    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "[-v] [reader-uri] \n" +
                  "-v  Verbose: Turn on transport listener\n" +
                  "reader-uri  Reader URI: e.g., \"tmr:///COM1\"\n"
                + "e.g: tmr:///com1  ; tmr:///dev/ttyS0/\n ");
        System.exit(1);
    }

    public static void setTrace(Reader r, String args[])
    {
        if (args[0].toLowerCase().equals("on"))
        {
            r.addTransportListener(r.simpleTransportListener);
        }
    }

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
                    System.out.printf("\n");
                }
                System.out.printf(" %02x", data[i]);
            }
            System.out.printf("\n");
        }
    }

    public static void main(String argv[])
    {
        Reader r = null;
        int nextarg = 0;
        boolean trace = false;
        int timeout = 0;
        int configFlags = 0;
        List<Byte> buffer = new ArrayList<Byte>(); //buffer
        byte flags = 0;
        PassThrough passThroughOp;
        byte[] passThroughResp;

        if (argv.length < 1)
        {
            usage();
        }

        if (argv[nextarg].equals("-v"))
        {
            trace = true;
            nextarg++;
        }

        try
        {
            r = Reader.create(argv[nextarg]);
            if (trace)
            {
                setTrace(r, new String[]{"on"});
            }
            r.connect();
            
            // Perform sync read for 500ms
            SimpleReadPlan plan = new SimpleReadPlan(null, TagProtocol.ISO15693, null, null, 1000);
            // Set the created readplan
            r.paramSet("/reader/read/plan", plan);
            // Read tags
            TagReadData[] tagReads = r.read(500);
            // Print first tag read epc
            byte[] epc = tagReads[0].getTag().epcBytes();
            System.out.println("Tag ID: " + (tagReads[0].getTag().epcString()));

            //Select Tag
            timeout = 500; //timeout in milliseconds.
            flags = 0x22;
            configFlags = PassThrough.ConfigFlags.ENABLE_TX_CRC.getCode() | 
                          PassThrough.ConfigFlags.ENABLE_RX_CRC.getCode() |
                          PassThrough.ConfigFlags.ENABLE_INVENTORY.getCode();

            /* Frame payload data as per 15693 protocol(ICODE Slix-S) */
            buffer.add(flags);
            buffer.add(OPCODE_SELECT_TAG);

            //Append UID(reverse).
            buffer.addAll(appendReverseUID(epc));
            
            //Execute passthrough tag op to select a tag
            passThroughOp = new PassThrough(timeout, configFlags, buffer);
            passThroughResp = (byte[])r.executeTagOp(passThroughOp, null);
            if(passThroughResp.length > 0)
            {
                System.out.println(String.format("Select Tag| Data(%d): %s\n", 
                        passThroughResp.length, ReaderUtil.byteArrayToHexString(passThroughResp)));
            }
            
            //Reset command buffer
            buffer.clear();

            //Get random number.
            // Initialize passthrough tag operation with all the required fields
            flags = 0x12;
            configFlags = PassThrough.ConfigFlags.ENABLE_TX_CRC.getCode() | 
                          PassThrough.ConfigFlags.ENABLE_RX_CRC.getCode() |
                          PassThrough.ConfigFlags.ENABLE_INVENTORY.getCode();

            /* Frame payload data as per 15693 protocol(ICODE Slix-S) */
            buffer.add(flags); // flags
            buffer.add(GET_RANDOM_NUMBER); // GET_RANDOM_NUMBER
            buffer.add(IC_MFG_CODE_NXP); //IC_MFG_CODE_NXP

            passThroughOp = new PassThrough(timeout, configFlags, buffer);
            passThroughResp = (byte[])r.executeTagOp(passThroughOp, null);
            if(passThroughResp.length > 0)
            {
                System.out.println(String.format("RN number |  Data(%d): %s\n", 
                        passThroughResp.length, ReaderUtil.byteArrayToHexString(passThroughResp)));
            }

            // Shut down reader
            r.destroy();
        }
        catch (Exception ex)
        {
            ex.printStackTrace();
        }
    }

    //Reverses the uid
    public static List<Byte> appendReverseUID(byte[] uidISO15693)
    {
        List<Byte> reversedUid = new ArrayList<Byte>(); 
        int length = uidISO15693.length;
        int i = 0;
        while(i < length)
        {
          reversedUid.add(uidISO15693[(length - 1) - i]);
          i++;
        }
        return reversedUid;
    }
}
