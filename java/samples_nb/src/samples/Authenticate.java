/*
 * Sample program that shows how to authenticate a Gen2v2 
 * tag using a preconfigured key.
 */
package samples;

import com.thingmagic.Gen2;
import com.thingmagic.Reader;
import com.thingmagic.ReaderUtil;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.TMConstants;
import com.thingmagic.TagFilter;
import com.thingmagic.TagOp;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;
import com.thingmagic.TransportListener;

public class Authenticate
{
    private static Reader r = null;
    private static int[] antennaList = null;
    static BlockPermalock.SerialPrinter serialPrinter;
    static BlockPermalock.StringPrinter stringPrinter;
    static TransportListener currentListener;
    static byte[] response;
    static boolean sendRawData = true;
    static boolean _isNMV2DTag = false;

    static void usage()
    {
        System.out.printf("Usage: Please provide valid arguments, such as:\n"
                + "Authenticate [-v] [reader-uri] [--ant n[,n...]] \n" +
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

            byte[]  key0  =  new byte[]{(byte)0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB, (byte) 0xCD, (byte) 0xEF, (byte) 0x01, (byte) 0x23, (byte) 0x45, (byte) 0x67, (byte) 0x89, (byte) 0xAB, (byte) 0xCD, (byte) 0xEF};
            byte[]  key1  =  new byte[]{(byte)0x11, (byte)0x22, (byte) 0x33, (byte) 0x44, (byte) 0x55, (byte) 0x66, (byte) 0x77, (byte) 0x88, (byte) 0x11, (byte) 0x22, (byte) 0x33, (byte) 0x44, (byte) 0x55, (byte) 0x66, (byte) 0x77, (byte) 0x88};

            Gen2.NXP.AES.Tam1Authentication tam1Auth;
            Gen2.NXP.AES.Tam2Authentication tam2Auth;
            int protMode;

            if(enableFilter)
            {
                target = new Gen2.Select(false, Gen2.Bank.EPC, 32, 16, new byte[] {(byte)0x11,(byte)0x22});
            }

            // Uncomment this to enable TAM1 Authentication with KEY0 for NXPUCODE AES Tag
            tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, key0, sendRawData);
            Gen2.Authenticate tam1AuthKey0 = new Gen2.NXP.AES.Authenticate(tam1Auth);
            System.out.println(tam1AuthKey0.toString());
            response = (byte[])r.executeTagOp(tam1AuthKey0, target);

            if(sendRawData)
            {
                parseTAM1AuthenticationResponse(response, key0);
            }
            else
            {
                System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(response));
            }

            //Uncomment this to enable TAM1 Authentication with KEY1
            /*tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, key1, sendRawData);
            Gen2.Authenticate tam1AuthKey1 = new Gen2.NXP.AES.Authenticate(tam1Auth);
            System.out.println(tam1AuthKey1.toString());
            response = (byte[])r.executeTagOp(tam1AuthKey1, target);

            if(sendRawData)
            {
                parseTAM1AuthenticationResponse(response, key1);
            }
            else
            {
                System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(response));
            }*/

            //Uncomment this to enable TAM2 Authentication with KEY1
            // supported protMode value is 1 for NXPUCODE AES
            /*protMode = 1;
            tam2Auth = new Gen2.NXP.AES.Tam2Authentication(Gen2.NXP.AES.KeyId.KEY1,key1, Gen2.NXP.AES.Profile.EPC, 0, 1, protMode, sendRawData);
            Gen2.Authenticate tam2AuthKey1 = new Gen2.NXP.AES.Authenticate(tam2Auth);
            System.out.println(tam2AuthKey1.toString());
            response = (byte[])r.executeTagOp(tam2AuthKey1, target);
            if(sendRawData)
            {
                parseTAM2AuthenticationResponse(response, key1, protMode);
            }
            else
            {
                byte[] generatedIchallenge = new byte[10];
                byte[] dataRequested = new byte[16];
                System.arraycopy(response, 0, generatedIchallenge, 0, 10);
                System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(generatedIchallenge));
                System.arraycopy(response, 10, dataRequested, 0, 16);
                System.out.println("Data :"+ ReaderUtil.byteArrayToHexString(dataRequested));
            }*/
            
            /* Embedded tag operations */
            {
                // Uncomment this to execute embedded tagop for TAM1 Authentication with KEY0 
                /*tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, key0, sendRawData);
                Gen2.Authenticate embeddedTam1AuthKey0 = new Gen2.NXP.AES.Authenticate(tam1Auth);
                System.out.println("Embedded Tag operation of " + embeddedTam1AuthKey0.toString());
                response = performEmbeddedOperation(target, embeddedTam1AuthKey0);

                if(sendRawData)
                {
                    parseTAM1AuthenticationResponse(response, key0);
                }
                else
                {
                    System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(response));
                }*/

                //Uncomment this to execute embedded tagop for TAM1 Authentication with KEY1
                /*tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY1, key1, sendRawData);
                Gen2.Authenticate embeddedTam1AuthKey1 = new Gen2.NXP.AES.Authenticate(tam1Auth);
                System.out.println("Embedded Tag operation of " + embeddedTam1AuthKey1.toString());
                response = performEmbeddedOperation(target, embeddedTam1AuthKey1);

                if(sendRawData)
                {
                    parseTAM1AuthenticationResponse(response, key1);
                }
                else
                {
                    System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(response));
                }*/

                //Uncomment this to execute embedded tagop for TAM2 Authentication with KEY1
                // supported protMode value is 1 for NXPUCODE DNA
                /*protMode = 1;
                tam2Auth = new Gen2.NXP.AES.Tam2Authentication(Gen2.NXP.AES.KeyId.KEY1,key1, Gen2.NXP.AES.Profile.EPC, 0, 1, protMode, sendRawData);
                Gen2.Authenticate embeddedTam2AuthKey1 = new Gen2.NXP.AES.Authenticate(tam2Auth);
                System.out.println("Embedded Tag operation of " + embeddedTam2AuthKey1.toString());
                response = performEmbeddedOperation(target, embeddedTam2AuthKey1);
                if(sendRawData)
                {
                    parseTAM2AuthenticationResponse(response, key1, protMode);
                } 
                else
                {
                    byte[] generatedIchallenge = new byte[10];
                    byte[] dataRequested = new byte[16];
                    System.arraycopy(response, 0, generatedIchallenge, 0, 10);
                    System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(generatedIchallenge));
                    System.arraycopy(response, 10, dataRequested, 0, 16);
                    System.out.println("Data :"+ ReaderUtil.byteArrayToHexString(dataRequested));
                }*/
            }
            //Enable flag _isNMV2DTag for TAM1/TAM2 Authentication with KEY0 for NMV2D Tag
            if (_isNMV2DTag) 
            {
                // NMV2D tag only supports KEY0
                // TAM1 Authentication with KEY0
                byte[] key0_NMV2D = new byte[]{(byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x00};
                //Uncomment this to enable TAM1 with key0
                /*tam1Auth = new Gen2.NXP.AES.Tam1Authentication(Gen2.NXP.AES.KeyId.KEY0, key0_NMV2D, sendRawData);
                Gen2.Authenticate tam1AuthenticateKey0 = new Gen2.NXP.AES.Authenticate(tam1Auth);
                System.out.println(tam1AuthenticateKey0.toString());
                response = (byte[]) r.executeTagOp(tam1AuthenticateKey0, target);
                if(sendRawData)
                {
                    parseTAM1AuthenticationResponse(response, key0_NMV2D);
                }
                else
                {
                    System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(response));
                }*/

                //TAM2 Authentication with KEY0
                //supported protMode values are 0,1,2,3
                protMode = 0;
                tam2Auth = new Gen2.NXP.AES.Tam2Authentication(Gen2.NXP.AES.KeyId.KEY0, key0_NMV2D, Gen2.NXP.AES.Profile.EPC, 0, 1, protMode, sendRawData);
                Gen2.Authenticate tam2AuthKey0 = new Gen2.NXP.AES.Authenticate(tam2Auth);
                System.out.println(tam2AuthKey0.toString());
                response = (byte[]) r.executeTagOp(tam2AuthKey0, target);
                if (sendRawData) 
                {
                    parseTAM2AuthenticationResponse(response, key0_NMV2D, protMode);
                } 
                else 
                {
                    byte[] generatedIchallenge = new byte[10];
                    byte[] dataRequested = new byte[16];
                    System.arraycopy(response, 0, generatedIchallenge, 0, 10);
                    System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(generatedIchallenge));
                    System.arraycopy(response, 10, dataRequested, 0, 16);
                    System.out.println("Data :"+ ReaderUtil.byteArrayToHexString(dataRequested));
                }
            }
        }
        catch(Exception e)
        {
            e.printStackTrace();
        }
        finally
        {
            r.destroy();
        }
    }

    public static byte[] decryptIchallenge(byte[] key, byte[] response, boolean resize) throws Exception
    {
        byte[] decryperText = AES.decrypt(key, response);
        if(resize)
            return  resizeArray(decryperText.clone());
        return  decryperText;
    }

    public static String decryptCustomData(byte[] key, byte[] cipherData, byte[] IV) throws Exception
    {
        byte[] decipheredText = new byte[16];
        decipheredText = decryptIchallenge(key,cipherData,false);
        byte[] CustomData = new byte[16];
        for (int i = 0; i < IV.length; i++)
        {
            CustomData[i] = (byte)(decipheredText[i] ^ IV[i]);
        }
        return ReaderUtil.byteArrayToHexString(CustomData);
    }

    public static byte[] resizeArray(byte[] arrayToResize) 
    {
        int newCapacity = arrayToResize.length-6;
        byte[] newArray = new byte[newCapacity];
        System.arraycopy(arrayToResize, 6,newArray, 0, newCapacity);
        return newArray;
    }
    
    public static byte[] performEmbeddedOperation(TagFilter filter, TagOp op) throws Exception
    {
        TagReadData[] tagReads = null;
        byte[] response = null;
        SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, filter, op, 1000);
        r.paramSet("/reader/read/plan", plan);
        tagReads = r.read(1000);
        for (TagReadData tr : tagReads)
        {
            System.out.println("EPC: " + tr.epcString());
            response = tr.getData();
        }
        return response;
    }

    // This method will parse TAM1Authentication response
    public static void parseTAM1AuthenticationResponse(byte[] response, byte[] key) throws Exception
    {
        byte[] generatedIchallenge = new byte[10];
        byte[] IV = new byte[16];
        if(response.length > 0)
        {
            System.arraycopy(response, 0, generatedIchallenge, 0, 10);
            System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(generatedIchallenge));
            System.arraycopy(response, 10, IV, 0, 16);
            byte[] receivedIchallenge = decryptIchallenge(key, IV , true);
            System.out.println("Returned Ichallenge :"+ ReaderUtil.byteArrayToHexString(receivedIchallenge));
        }
    }

    // This method will parse TAM2Authentication response
    public static void parseTAM2AuthenticationResponse(byte[] response, byte[] key, int protMode) throws Exception
    {
        byte[] generatedIchallenge = new byte[10];
        byte[] cipherData = new byte[16];
        byte[] IV = new byte[16];
        if(response.length > 0)
        {
            System.arraycopy(response, 0, generatedIchallenge, 0, 10);
            System.out.println("Generated Ichallenge :"+ ReaderUtil.byteArrayToHexString(generatedIchallenge));
            System.arraycopy(response, 10, IV, 0, 16);
            byte[] challenge = decryptIchallenge(key, IV, true);
            System.out.println("Returned Ichallenge :"+ ReaderUtil.byteArrayToHexString(challenge));
            System.arraycopy(response, 26, cipherData, 0, 16);
            if (protMode == 1 || protMode == 3) 
            {
                String customData = decryptCustomData(key, cipherData, IV.clone());
                System.out.println("customData :" + customData);
            }
            else
            {
                System.out.println("customData :" + ReaderUtil.byteArrayToHexString(cipherData));
            }
        }
    }
}
