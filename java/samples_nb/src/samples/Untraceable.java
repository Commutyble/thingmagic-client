/*
 * Sample program that demonstrates the usage of
 * Gen2v2 Untraceable
 */
package samples;

import com.thingmagic.Gen2;
import com.thingmagic.Reader;
import com.thingmagic.ReaderException;
import com.thingmagic.ReaderUtil;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.TMConstants;
import com.thingmagic.TagFilter;
import com.thingmagic.TagOp;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;
import com.thingmagic.TransportListener;

public class Untraceable
{
    private static Reader r = null;
    private static int[] antennaList = null;
    static BlockPermalock.SerialPrinter serialPrinter;
    static BlockPermalock.StringPrinter stringPrinter;
    static TransportListener currentListener;
    static boolean sendRawData = false;

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Untraceable [-v] [reader-uri] [--ant n[,n...]] \n" +
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
        } else if (currentListener != null)
        {
            r.removeTransportListener(Reader.simpleTransportListener);
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
    
    public static void main(String argv[])
    {
        // Program setup   
        TagFilter target = null;
        int nextarg = 0;
        boolean trace = false;
        // To enable select filter, set enableFilter flag to true
        boolean enableFilter = false;
        int epcLen;
        if (argv.length < 1 || (argv.length == 1 && argv[nextarg].equalsIgnoreCase("-v")))
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
                } else
                {
                    System.out.println("Argument " + argv[nextarg] + " is not recognised");
                    usage();
                }
            }

            r = Reader.create(readerURI);
            if (trace)
            {
                setTrace(r, new String[]
                {
                    "on"
                });
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
            //Use first antenna for operation
            if (antennaList != null)
            {
                r.paramSet("/reader/tagop/antenna", antennaList[0]);
            }
            SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
            r.paramSet(TMConstants.TMR_PARAM_READ_PLAN, plan);

            /* set the session to S0 */
            System.out.println("setting the session to S0");
            r.paramSet(TMConstants.TMR_PARAM_GEN2_SESSION, Gen2.Session.S0);

            //Untrace TAM1
            byte[]  key1  =  new byte[]{(byte) 0x11, (byte) 0x22, (byte) 0x33, (byte) 0x44, (byte) 0x55, (byte) 0x66, (byte) 0x77, (byte) 0x88, (byte) 0x11, (byte) 0x22, (byte) 0x33, (byte) 0x44, (byte) 0x55, (byte) 0x66, (byte) 0x77, (byte) 0x88};
            byte[]  key0  =  new byte[]{(byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB, (byte) 0xCD, (byte) 0xEF, (byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB, (byte) 0xCD, (byte) 0xEF};
            Gen2.NXP.AES.Tam1Authentication tam1 ;

            if(enableFilter)
            {
                target = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, new byte[] {(byte)0x11,(byte)0x22});
            }

            readTag(r);

            //show 4 epc words with TAM1 + Key0
            epcLen = 4;
            tam1  = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, key0, sendRawData);
            Gen2.Untraceable untraceTam1 = new Gen2.NXP.AES.Untraceable(
                    Gen2.Untraceable.EPC.HIDE,epcLen, 
                    Gen2.Untraceable.TID.HIDE_SOME, 
                    Gen2.Untraceable.User.SHOW,
                    Gen2.Untraceable.Range.NORMAL, 
                    tam1);

            System.out.println(untraceTam1.toString());
            r.executeTagOp(untraceTam1, target);
            readTag(r);

            //show 6 epc words with TAM1 + Key1
            /*epcLen = 6;
            tam1 = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, key1, sendRawData);
            Gen2.Untraceable untraceTam1Key1 = new Gen2.NXP.AES.Untraceable(
                    Gen2.Untraceable.EPC.HIDE, epcLen,
                    Gen2.Untraceable.TID.HIDE_SOME,
                    Gen2.Untraceable.User.SHOW,
                    Gen2.Untraceable.Range.NORMAL,
                    tam1);

            System.out.println(untraceTam1Key1.toString());
            r.executeTagOp(untraceTam1Key1, target);
            readTag(r);
            */

            //show 6 epc words with UntraceAcess

            /*Gen2.Password pwd = new Gen2.Password(0x00000000);
            Gen2.Untraceable untraceAcess = new Gen2.NXP.AES.Untraceable(Gen2.Untraceable.EPC.HIDE,6, Gen2.Untraceable.TID.HIDE_NONE, Gen2.Untraceable.User.SHOW, Gen2.Untraceable.Range.NORMAL, pwd.getValue());
            System.out.println(untraceAcess.toString());
            r.executeTagOp(untraceAcess, target);
            readTag(r);
            */

            /* Embedded tag operations */
            {

                //show 6 epc words with TAM1 + Key0
                /*epcLen = 6;
                tam1 = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, key0, sendRawData);
                Gen2.Untraceable embeddeduntraceTam1Key0 = new Gen2.NXP.AES.Untraceable(
                        Gen2.Untraceable.EPC.HIDE, epcLen,
                        Gen2.Untraceable.TID.HIDE_SOME,
                        Gen2.Untraceable.User.SHOW,
                        Gen2.Untraceable.Range.NORMAL,
                        tam1);

                System.out.println("Embedded Tag operation of " + embeddeduntraceTam1Key0.toString());
                performEmbeddedOperation(target, embeddeduntraceTam1Key0);
                readTag(r);
                */

                //show 6 epc words with TAM1 + Key1
                /*epcLen = 6;
                tam1 = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, key1, sendRawData);
                Gen2.Untraceable embeddeduntraceTam1Key1 = new Gen2.NXP.AES.Untraceable(
                        Gen2.Untraceable.EPC.HIDE, epcLen,
                        Gen2.Untraceable.TID.HIDE_SOME,
                        Gen2.Untraceable.User.SHOW,
                        Gen2.Untraceable.Range.NORMAL,
                        tam1);

                System.out.println("Embedded Tag operation of " + embeddeduntraceTam1Key1.toString());
                performEmbeddedOperation(target, embeddeduntraceTam1Key1);
                readTag(r);
                */

                //show 6 epc words with UntraceAcess
                /*Gen2.Password password = new Gen2.Password(0x00000000);
                Gen2.Untraceable embeddeduntraceAcess = new Gen2.NXP.AES.Untraceable(Gen2.Untraceable.EPC.HIDE, 6, Gen2.Untraceable.TID.HIDE_NONE, Gen2.Untraceable.User.SHOW, Gen2.Untraceable.Range.NORMAL, password.getValue());
                System.out.println("Embedded Tag operation of " + embeddeduntraceAcess.toString());
                performEmbeddedOperation(target, embeddeduntraceAcess);
                readTag(r);
                */

            }
            r.destroy();
        }
        catch(Exception e)
        {
            e.printStackTrace();
        }
    }
    
    public static void readTag(Reader r) throws ReaderException
    {
        TagReadData[] tagReads = r.read(500);
        for (TagReadData tr : tagReads)
        {
            System.out.println(tr.getTag());
        }
    }
    
    public static void performEmbeddedOperation(TagFilter filter, TagOp op) throws Exception
    {
        TagReadData[] tagReads = null;
        byte[] response = null;
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, op, 1000);
        r.paramSet("/reader/read/plan", plan);
        tagReads = r.read(1000);
        for (TagReadData tr : tagReads)
        {
            response = tr.getData();
        }
    }
}
