/*
 * This codelet demonstrates the functionality of EM4325 custom tag operations
 */

package samples;
import com.thingmagic.Gen2;
import com.thingmagic.Reader;
import com.thingmagic.ReaderCodeException;
import com.thingmagic.ReaderException;
import com.thingmagic.ReaderUtil;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.TMConstants;
import com.thingmagic.TagFilter;
import com.thingmagic.TagOp;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;
import java.util.EnumSet;
import java.util.HashMap;
import java.util.Map;

/**
 *
 * @author bchanda
 */
public class EM4325CustomTagOps 
{
    private static int[] antennaList = null;
    private static Reader r = null;

    static void usage()
    { 
      System.out.printf("Usage: Please provide valid arguments, such as:\n"
                  + "EM4325TagOps [-v] [reader-uri] [--ant n[,n...]] \n" +
                    "-v  Verbose: Turn on transport listener\n" +
                    "reader-uri  Reader URI: e.g., \"tmr:///COM1\" \n"
                  + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n"
                  + "e.g: tmr:///com1 --ant 1,2 \n ");
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
    TagFilter filter = null;
    boolean enableFilter = false;

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
                System.out.println("Argument " + argv[nextarg] + " is not recognised");
                usage();
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

        //Use first antenna for tag operation
        if (antennaList != null)
            r.paramSet("/reader/tagop/antenna", antennaList[0]);     
        
        if(enableFilter)
        {
            // If select filter is to be enabled
            byte mask[] = new byte[]{(byte)0x30,(byte)0x28,(byte)0x35,(byte)0x4D,
                (byte)0x82,(byte)0x02,(byte)0x02,(byte)0x80,(byte)0x00,(byte)0x01,
                (byte)0x04,(byte)0xAC};
            filter = new Gen2.Select(false, Gen2.Bank.EPC, 32, 96, mask);
        }
        //Get Sensor Data of EM4325 tag
        boolean sendUid = true;
        boolean sendNewSample = true;
        Gen2.EMMicro.EM4325.GetSensorData getSensorOp = new Gen2.EMMicro.EM4325.GetSensorData(sendUid, sendNewSample);
        try
        {
            System.out.println("****Executing standalone tag operation of Get sensor Data command of EM4325 tag****");
            byte[] response = (byte[])r.executeTagOp(getSensorOp, filter);
            
            // Parse the response of GetSensorData
            System.out.println("****Get sensor Data command is success****");
            GetSensorDataResponse rData = new GetSensorDataResponse(response);
            System.out.println(rData.toString());
        }
        catch(Exception ex)
        {
            System.out.println("Exception from executing get Sensor Data command: " + ex.getMessage());
        }
        
        //Embedded tag operation of Get sensor data
        try
        {
            System.out.println("****Executing embedded tag operation of Get sensor Data command of EM4325 tag****");
            performEmbeddedOperation(filter, TagProtocol.GEN2, getSensorOp);
        }
        catch(Exception ex)
        {
            System.out.println("Exception from embedded get sensor data: " + ex.getMessage());
        }

        //Reset alarms command
        // Read back the temperature control word at 0xEC address to verify reset enable alarm bit is set before executing reset alarm tag op. 
        System.out.println("Reading Temperature control word 1 before resetting alarms to ensure reset enable bit is set to 1");
        byte []filterMask = new byte[]{(byte)0x04, (byte)0xc2};
        Gen2.Select select = new Gen2.Select(false, Gen2.Bank.EPC, 0x70, 16, filterMask);
        Gen2.ReadData rdata = new Gen2.ReadData(Gen2.Bank.USER, 0xEC, (byte)0x1);
        short[] resp = (short[])r.executeTagOp(rdata, select);
        System.out.println("Temp control word 1: ");
        for (short dt : resp)
        {
            System.out.printf("%02x \t", dt);
        }
        System.out.println("\n");
        
        // If temperature control word is not 0x4000, write the data
        if(resp[0] != 0x4000)
        {
            short[] writeData = new short[]{0x4000};
            Gen2.WriteData wData = new Gen2.WriteData(Gen2.Bank.USER, 0xEC, writeData);
            r.executeTagOp(wData, select);
        }
        Gen2.EMMicro.EM4325.ResetAlarms resetAlarmsOp = new Gen2.EMMicro.EM4325.ResetAlarms();
        try
        {
            System.out.println("****Executing standalone tag operation of Reset alarms command of EM4325 tag****");
            r.executeTagOp(resetAlarmsOp, filter);
            System.out.println("****Reset Alarms command is success****");
        }
        catch(Exception ex)
        {
            System.out.println("Exception from executing reset alarms : " + ex.getMessage());
        }
        
        //Embedded tag operation of reset alarms command
        try
        {
           System.out.println("****Executing embedded tag operation of reset alarms command of EM4325 tag****");
           performEmbeddedOperation(filter, TagProtocol.GEN2, resetAlarmsOp);
        }
        catch(Exception ex)
        {
            System.out.println("Exception from embedded reset alarms command: " + ex.getMessage());
        }
        
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
            System.out.println(tr.toString());
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
                if(tr.getData().length > 0)
                {
                    if (op instanceof Gen2.EMMicro.EM4325.GetSensorData)
                    {
                        GetSensorDataResponse rData = new GetSensorDataResponse(tr.getData());
                        System.out.println("Data:" + rData.toString());
                    }
                    else
                    {
                        System.out.println("Data: " + ReaderUtil.byteArrayToHexString(tr.getData()));
                        System.out.printf("\n");
                    }
                }
            }
        }
    }
  
    public static class GetSensorDataResponse
    {
        // uid of tag
        byte[] uid;
            
        // Sensor data
        SensorData sensorData;
            
        // UTC Timestamp
        int utcTimestamp;
        
        /**
         * GetSensorDataResponse - parses the response and fills uid , sensor data and utc timestamp with values
         * @param response
         */
        public GetSensorDataResponse(byte[] response)
        {
            // Get sensor data response contains
            // UIDlength(2 bytes) + UID(8 or 10 or 12 bytes) + Sensor Data(4 bytes) + UTC Timestamp(4 bytes) 

            // read index
            int readIndex = 0;

            // uid length in bytes
            int uidLen = 0;

            // Extract 2 bytes of dataLength. Datalength includes length of uid + sensorData + UTCTimestamp
            byte[] dataLenBits = new byte[2];
            System.arraycopy(response, readIndex, dataLenBits, 0, 2);
            int offset = 0;

            //converts byte array to int value
            int dataLength = ((dataLenBits[offset] & 0xff) << 8) | ((dataLenBits[offset + 1] & 0xff) << 0);
            // dataLength is in bits, so divide by 8 to get overall length in bytes and
            //  Subtract 4(Sensor Data) + 4(UTC timestamp) = 8 bytes to get uid Length
            uidLen = (dataLength / 8) - 8;

            uid = getUID(response, readIndex, uidLen);
            sensorData = getSensorData(response, readIndex, uidLen);
            utcTimestamp = getUTCTimestamp(response, readIndex, uidLen);
        }
        
        //getUID - retrieves the uid from the response
        public byte[] getUID(byte[] response, int readIndex, int uidLen)
        {
            
            uid = new byte[uidLen];

            // Extract uid of tag if sendUid flag is enabled
            if(uidLen > 0)
            {
                // Now extract uid based on the length
                System.arraycopy(response, (readIndex + 2), uid, 0, (uidLen));
            }
            return uid;
        }

        //getSensorData - retrieves the sensor data from the response        
        public SensorData getSensorData(byte[] response, int readIndex, int uidLen)
        {
            // Extract sensor data(4 bytes)
            byte []sensorDataArray = new byte[4];
            if(uidLen > 0)
            {
                readIndex = uidLen + 2;// exclude uidLength(2 bytes) and uid bits(uidLen bytes)
            }
            else
            {
                readIndex = 0;
            }
            System.arraycopy(response, readIndex, sensorDataArray, 0, 4);
            int sensorData = ReaderUtil.byteArrayToInt(sensorDataArray, 0);
            return new SensorData(sensorData);
        }

        //getUTCTimestamp - retrieves the UTC timestamp from the response
        public int getUTCTimestamp(byte[] response, int readIndex, int uidLen)
        {
            if(uidLen > 0)
            {
                readIndex = uidLen + 2 + 4; // exclude uidLength(2 bytes), uidBits(uidLen bytes) and sensorData(4 bytes) 
            }
            else
            {
                readIndex = 4; //exclude sensor data
            }
            // Extract utc timestamp(4 bytes)
            byte []utcTimeArray = new byte[4];
            System.arraycopy(response, readIndex, utcTimeArray, 0, 4);
            int utcTimestamp = ReaderUtil.byteArrayToInt(utcTimeArray, 0);
            return utcTimestamp;
        }
        
        /**
          * Returns a <code>String</code> object representing this
          * object. The string contains a whitespace-delimited set of
          * field:value pairs representing the UID, sensor Data and UTCTimestamp.
          *
          * @return the representation string
          */
        public String toString() 
        {
            return String.format("UID :%s \n SensorData:%s \n UTCTimestamp:%d",
                        ReaderUtil.byteArrayToHexString(uid),
                        sensorData,
                        utcTimestamp);
        }

    }
    
    public static class SensorData
    {
        //Enums
        public enum LowBatteryAlarm
        {
            NOPROBLEM(0),
            LOWBATTERYDETECTED(1);
            int rep;
            private LowBatteryAlarm(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, LowBatteryAlarm> lookup = new HashMap<Integer, LowBatteryAlarm>();
            static
            {
                for (LowBatteryAlarm value : EnumSet.allOf(LowBatteryAlarm.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static LowBatteryAlarm get(int rep)
            {
                return lookup.get(rep);
            }
        }
        
        public enum AuxAlarm
        {
            NOPROBLEM(0),
            TAMPER_OR_SPI_ALARM_DETECTED(1);
            int rep;
            private AuxAlarm(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, AuxAlarm> lookup = new HashMap<Integer, AuxAlarm>();
            static
            {
                for (AuxAlarm value : EnumSet.allOf(AuxAlarm.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static AuxAlarm get(int rep)
            {
                return lookup.get(rep);
            }
        } 
        
        public enum OverTempAlarm
        {
            NOPROBLEM(0),
            OVERTEMPERATURE_DETECTED(1);
            int rep;
            private OverTempAlarm(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, OverTempAlarm> lookup = new HashMap<Integer, OverTempAlarm>();
            static
            {
                for (OverTempAlarm value : EnumSet.allOf(OverTempAlarm.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static OverTempAlarm get(int rep)
            {
                return lookup.get(rep);
            }
        }
        
        public enum UnderTempAlarm
        {
            NOPROBLEM(0),
            UNDERTEMPERATURE_DETECTED(1);
            int rep;
            private UnderTempAlarm(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, UnderTempAlarm> lookup = new HashMap<Integer, UnderTempAlarm>();
            static
            {
                for (UnderTempAlarm value : EnumSet.allOf(UnderTempAlarm.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static UnderTempAlarm get(int rep)
            {
                return lookup.get(rep);
            }
        }
        
        public enum P3Input
        {
            NOSIGNAL(0),
            SIGNALLEVEL(1);
            int rep;
            private P3Input(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, P3Input> lookup = new HashMap<Integer, P3Input>();
            static
            {
                for (P3Input value : EnumSet.allOf(P3Input.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static P3Input get(int rep)
            {
                return lookup.get(rep);
            }
        }
        
        public enum MonitorEnabled
        {
            DISABLED(0),
            ENABLED(1);
            int rep;
            private MonitorEnabled(int rep)
            {
               this.rep = rep;
            }
            private static final Map<Integer, MonitorEnabled> lookup = new HashMap<Integer, MonitorEnabled>();
            static
            {
                for (MonitorEnabled value : EnumSet.allOf(MonitorEnabled.class))
                {
                    lookup.put(value.getCode(), value);
                }
            }
            public int getCode() 
            {
                return rep;
            }
            public static MonitorEnabled get(int rep)
            {
                return lookup.get(rep);
            }
        }
        
        //Instance variables
        // LowBatteryAlarm status- MSW Bit 0
        public LowBatteryAlarm lowBatteryAlarmStatus;
        // AuxAlarm status - MSW Bit 1
        public AuxAlarm auxAlarmStatus;
        // OverTempAlarm status - MSW Bit 2
        public OverTempAlarm overTempAlarmStatus;
        // UnderTempAlarm status - MSW Bit 3
        public UnderTempAlarm underTempAlarmStatus;
        // P3Input status - MSW Bit 4
        public P3Input p3InputStatus;
        // MonitorEnabled status - MSW Bit 5
        public MonitorEnabled monitorEnabledStatus;
        //MSW Bit 6 always 0.
        // Temperature value in degree celsius -  MSW Bit 7 - F (9 bits)
        public double temperature;
        // Aborted Temperature Count - LSW Bits 0 - 5
        public byte abortedTemperatureCount;
        // Under Temperature Count - LSW Bits 6 - A
        public byte underTemperatureCount;
        // Over Temperature Count  - LSW Bits B - F
        public byte overTemperatureCount;
        
        // SensorData constructor
        public SensorData(int sensorData)
        {
            //16 bits of MSW + 16 bits of LSW
            short sensorDataRplyMSW = (short)((sensorData & 0xFFFF0000) >> 16);
            short sensorDataRplyLSW = (short)(sensorData & 0xFFFF);
            
            //MSW parsing
            lowBatteryAlarmStatus = getLowBatteryAlarmStatus(sensorDataRplyMSW);
            auxAlarmStatus = getAuxAlarmStatus(sensorDataRplyMSW);
            overTempAlarmStatus = getOverTempAlarmStatus(sensorDataRplyMSW);
            underTempAlarmStatus = getUnderTempAlarmStatus(sensorDataRplyMSW);
            p3InputStatus = getP3InputStatus(sensorDataRplyMSW);
            monitorEnabledStatus = getMonitorEnabledStatus(sensorDataRplyMSW);
            temperature = getTemperature(sensorDataRplyMSW);

            //LSW parsing
            abortedTemperatureCount = getAbortedTemperatureCount(sensorDataRplyLSW);
            underTemperatureCount = getUnderTemperatureCount(sensorDataRplyLSW);
            overTemperatureCount = getOverTemperatureCount(sensorDataRplyLSW);
        }

        //MSW Bit 0
        public LowBatteryAlarm getLowBatteryAlarmStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 15)) != 0) return LowBatteryAlarm.get(1);
            else return LowBatteryAlarm.get(0);
        }
        //MSW Bit 1
        public AuxAlarm getAuxAlarmStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 14)) != 0) return AuxAlarm.get(1);
            else return AuxAlarm.get(0);
        }
        //MSW Bit 2
        public OverTempAlarm getOverTempAlarmStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 13)) != 0) return OverTempAlarm.get(1);
            else return OverTempAlarm.get(0);
        }
        //MSW Bit 3
        public UnderTempAlarm getUnderTempAlarmStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 12)) != 0) return UnderTempAlarm.get(1);
            else return UnderTempAlarm.get(0);
        }
        //MSW Bit 4
        public P3Input getP3InputStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 11)) != 0) return P3Input.get(1);
            else return P3Input.get(0);
        }
        //MSW Bit 5
        public MonitorEnabled getMonitorEnabledStatus(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & (1 << 10)) != 0) return MonitorEnabled.get(1);
            else return MonitorEnabled.get(0);
        }
        //MSW Bit 6 always 0.
        //MSW Bit 7 - F (9 bits) for Temperature
        public double getTemperature(short sensorDataRplyMSW)
        {
            if((sensorDataRplyMSW & 0x1ff) == 0x100)
            {
                return 0x100; //Invalid Measurement
            }
            else
            {
                double temp = (double)((sensorDataRplyMSW & 0x1ff) * 0.25);
                return temp;
            }
        }
        //LSW Bits 0 - 5 
        public byte getAbortedTemperatureCount(short sensorDataRplyLSW)
        {
           return (byte)((sensorDataRplyLSW >> 10) & 0xFC);
        }
        //LSW Bits 6 - A
        public byte getUnderTemperatureCount(short sensorDataRplyLSW)
        {
            return (byte)((sensorDataRplyLSW >> 5) & 0x1F);
        }
        //LSW Bits B - F
        public byte getOverTemperatureCount(short sensorDataRplyLSW)
        {
            return (byte)((sensorDataRplyLSW >> 0) & 0x1F);
        }
        
        /**
          * Returns a <code>String</code> object representing this
          * object. The string contains a whitespace-delimited set of
          * field:value pairs representing the UID, sensor Data and UTCTimestamp.
          *
          * @return the representation string
          */
        public String toString() 
        {
            String s1,s2,s3;
            
            s1 = String.format(
                    "\n lowBatteryAlarmStatus = %s \n auxAlarmStatus = %s \n overTempAlarmStatus = %s" +
                    "\n underTempAlarmStatus = %s \n p3InputStatus = %s \n monitorEnabledStatus = %s", 
                    lowBatteryAlarmStatus, auxAlarmStatus,overTempAlarmStatus,
                    underTempAlarmStatus, p3InputStatus, monitorEnabledStatus);
            
            if (temperature == 0x100)
            {
                s2 = String.format("\n Temperature = Invalid Measurement");
            }
            else
            {
                s2 = String.format("\n Temperature = {%f} C", temperature);
            }
            
            s3 = String.format(
                    "\n abortedTemperatureCount = %d \n underTemperatureCount = %d \n overTemperatureCount = %d\n",
                    abortedTemperatureCount, underTemperatureCount, overTemperatureCount);
            
            return String.join(" ",new String[] {s1, s2, s3});
        }
    }
}
