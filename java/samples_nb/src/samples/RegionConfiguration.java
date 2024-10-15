/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package samples;
import com.thingmagic.*;
import java.util.Arrays;

/**
 *
 * @author sghatak
 */
public class RegionConfiguration {
    
    /**Open region parameters
     * Set custom values by amending default values below.
     */
    static boolean lbtEnable = true;
    static boolean dwellTimeEnable = true;
    /**
     * dwellTime value Min 1 - Max 65525
     * 0 is not valid for dwellTime. 
     **/
    static int dwellTime = 100;
    /**
     * lbtThreshold value should be between -128 to 0
     * LBT threshold default value is -72.
     **/
    static int lbtThreshold = -72;
    /**
     * hopTime, quantizationStep, minFrequency & hopTable to be changed according to region.
     **/
    static int hopTime = 3675;
    static int quantizationStep = 100000;
    static int minFrequency = 865700;
    static int[] hopTable = {865700, 866300, 866900, 867500};
    
    public enum ConfigRegion
    {
        DEFAULT(0),
        BAHRAIN(1),
        BRAZIL(2);
        int value;
        ConfigRegion(int v)
        {
          value = v;
        }
        public int getValue()
        {
          return value;
        }
        
    };
    
    /**
     * Enum region codes:
     * 0 - Default
     * 1 - Bahrain
     * 2 - Brazil
     */
    static ConfigRegion confRegion = ConfigRegion.DEFAULT;
    static void usage()
    {
      System.out.printf("Usage: Please provide valid arguments, such as:\n"
                  + "read [-v] [reader-uri] [--ant n[,n...]] [--lbtThreshold] [LBT threshold value] [--dwellTime] [dwell time value] \n" +
                    "-v  Verbose: Turn on transport listener\n" +
                    "reader-uri  Reader URI: e.g., \"tmr:///COM1\", \"tmr://astra-2100d3\"\n"
                  + "--ant  Antenna List: e.g., \"--ant 1\", \"--ant 1,2\"\n"
                  + "--region region value: e.g., \"--region Brazil\" or \"--region Bahrain\" or do not pass this argument for default values\n"
                  + "e.g: tmr:///com1 --ant 1,2 --lbtThreshold -72 --dwellTime 100 --region Brazil\n ");
      System.exit(1);
    }

    static void setRegionParams(ConfigRegion region)
    {
        switch(region)
        {
            //Amend case 0 for custome values
            case DEFAULT:
                //default values - set to Bharain region values
                System.out.println("********Setting default region params********"+"\n");
                break;
            case BAHRAIN:
                //default values will hold for Bharain region
                System.out.println("********Setting params for Bahrain region********"+"\n");
                lbtEnable = true;
                lbtThreshold = -72;
                dwellTimeEnable = true;
                dwellTime = 100;
                hopTime = 3675;
                quantizationStep = 100000;
                minFrequency = 865700;
                //hop table for Bahrain region
                hopTable = new int[]{865700, 866300, 866900, 867500};
                break;
            case BRAZIL:
                System.out.println("********Setting params for Brazil region********"+"\n");
                lbtEnable = false;
                dwellTimeEnable = false;
                hopTime = 375;
                quantizationStep = 250000;
                minFrequency = 902750;
                //hop table for Brazil region
                hopTable = new int[]{925500,905000,906000,925000,922750,902750,921000,926000,921750,916000,
                                     920250,919000,917750,925250,906500,920500,926500,916250,904000,919250,
                                     923750,906750,917250,924000,923000,915750,926750,924250,904250,918500,
                                     923500,920000,924750,905500,903500,921500,916500,922000,924500,920750,
                                     917500,927000,922250,918750,906250,905250,903750,919750,904750,921250};
                break;
        }
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
              else if (arg.equalsIgnoreCase("--region"))
              {
                  confRegion = parseArguments(argv, nextarg);
                  if(confRegion == ConfigRegion.DEFAULT)
                  {
                      System.out.println("Argument " + argv[nextarg+1] + " is not recognised. "+"Supported regions : \"--region Bahrain\" & \"--region Brazil\"");
                      usage();
                  }
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
          //Step 1 - Set region to OPEN
          r.paramSet(TMConstants.TMR_PARAM_REGION_ID, Reader.Region.OPEN);
          Reader.Region region = (Reader.Region) r.paramGet(TMConstants.TMR_PARAM_REGION_ID);
          if(region == Reader.Region.OPEN)
          {
              System.out.println("\n"+"********Region set to OPEN********"+"\n");
              //Set region parameters as per the selected region
              setRegionParams(confRegion);
              //Step 2 - Set frequency hop table
              System.out.println("********Setting Hop table********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_HOPTABLE, hopTable);
              int[] hopTableVal = (int[])r.paramGet(TMConstants.TMR_PARAM_REGION_HOPTABLE);
              System.out.println("Hop table set to : " +Arrays.toString(hopTableVal)+"\n");
              //Step 3 - Set LBT enable
              System.out.println("********Setting LBT enable********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_LBT_ENABLE, lbtEnable);
              //Get & print LBT enable value
              boolean lbtEnableVal = (Boolean) r.paramGet(TMConstants.TMR_PARAM_REGION_LBT_ENABLE);
              System.out.println("LBT enable : "+lbtEnableVal+"\n");
              //Step 4 - Set LBT threshold
              if(lbtEnable)
              {
                System.out.println("********Setting LBT threshold********");
                r.paramSet(TMConstants.TMR_PARAM_REGION_LBT_THRESHOLD, lbtThreshold);
                //Get & print LBT threshold value
                int lbtThresholdVal = (Integer) r.paramGet(TMConstants.TMR_PARAM_REGION_LBT_THRESHOLD);
                System.out.println("LBT threshold set to : "+lbtThresholdVal+" dBm"+"\n");
              }
              else
              {
                  System.out.println("LBT enable set to false, not setting LBT threshold"+"\n");
              }
              //Step 5 - Set dwell time enable
              System.out.println("********Setting dwell time enable********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_DWELL_TIME_ENABLE, dwellTimeEnable);
              //Get & print dwell time enable value
              boolean dwellTimeEnableVal = (Boolean) r.paramGet(TMConstants.TMR_PARAM_REGION_DWELL_TIME_ENABLE);
              System.out.println("Dwell time enable : "+dwellTimeEnableVal+"\n");
              //Step 6 - Set dwell time
              if(dwellTimeEnable)
              {
                System.out.println("********Setting dwell time********");
                r.paramSet(TMConstants.TMR_PARAM_REGION_DWELL_TIME, dwellTime);//Get & print dwell time value
                int dwellTimeVal = (Integer) r.paramGet(TMConstants.TMR_PARAM_REGION_DWELL_TIME);
                System.out.println("Dwell time set to : "+dwellTimeVal+" ms"+"\n");
              }
              else
              {
                  System.out.println("Dwell time enable set to false, not setting dwell time"+"\n");
              }
              //Step 7 - Set hop time
              System.out.println("********Setting Hop time********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_HOPTIME, hopTime);
              int hopTimeVal = (Integer) r.paramGet(TMConstants.TMR_PARAM_REGION_HOPTIME);
              System.out.println("Hop time set to : "+hopTimeVal+" ms"+"\n");
              //Step 8 - Set quantization step
              System.out.println("********Setting Quantization step********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_QUANTIZATION_STEP, quantizationStep);
              int quantizationStepVal = (Integer) r.paramGet(TMConstants.TMR_PARAM_REGION_QUANTIZATION_STEP);
              System.out.println("Quantization step set to - "+((quantizationStepVal/1000))+" kHz"+"\n");
              //Step 9 - Set region minimum frequency
              System.out.println("********Setting minimum frequency********");
              r.paramSet(TMConstants.TMR_PARAM_REGION_MINIMUM_FREQUENCY, minFrequency);
              int minFrequencyVal = (Integer) r.paramGet(TMConstants.TMR_PARAM_REGION_MINIMUM_FREQUENCY);
              System.out.println("Minimum region frequency set to : "+minFrequencyVal+"\n");

              /**
               * Uncomment Step 10 through 11 to save OPEN region settings as persistent.
               */
              //Step 10 - Save configuration
              //r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.SAVE));
              //System.out.println("User profile set option:save all configuration");
              //Step 11 - Restore configuration
              //r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.RESTORE));
              //System.out.println("User profile set option:restore all configuration");
          }
          else
          {
              System.out.println("Region configuration is only supported for OPEN region");
              r.destroy();
              System.exit(1);
          }

          SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
          r.paramSet("/reader/read/plan", plan);
          // Read tags
          tagReads = r.read(1000);
          // Print tag reads
          for (TagReadData tr : tagReads)
          {
              System.out.println("EPC: " + tr.epcString());
          }
          // Shut down reader
          r.destroy();
      } 
      catch (ReaderException re)
      {
        System.out.println("Reader Exception: " + re.getMessage());
      }
      catch (Exception re)
      {
          System.out.println("Exception: " + re.getMessage());
      }
    }
  
    static int[] parseAntennaList(String[] args,int argPosition)
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
  
    static ConfigRegion parseArguments(String[] args,int argPosition)
    {
        ConfigRegion reg = ConfigRegion.DEFAULT;
        try
        {
            String param = args[argPosition];
            String argument = args[argPosition + 1];
            if(param.equalsIgnoreCase("--region"))
            {
                String region = argument.toUpperCase();
                if(region.equalsIgnoreCase("Bahrain"))
                {
                    reg = ConfigRegion.BAHRAIN;
                }
                else if(region.equalsIgnoreCase("Brazil"))
                {
                    reg = ConfigRegion.BRAZIL;
                }
                else
                {
                    reg = ConfigRegion.DEFAULT;
                }
            }
            else
            {
                reg = ConfigRegion.DEFAULT;
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
        return reg;
    }
}
