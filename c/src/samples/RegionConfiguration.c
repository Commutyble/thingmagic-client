/**
 * Sample program that demonstrates open region configuration 
 * @file RegionConfiguration.c
 */

#include <tm_reader.h>
#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#include <serial_reader_imp.h>

#ifndef BARE_METAL
#if WIN32
#define snprintf sprintf_s
#endif 

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif
#define PRINT_TAG_METADATA 0
#define numberof(x) (sizeof((x))/sizeof((x)[0]))

#define usage() {errx(1, "Please provide valid arguments, such as: reader-uri [--ant n] [--region BRAZIL/Brazil/brazil]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "[--region region] : e.g, '--region Brazil'\n"\
                         "Example: 'tmr:///com4 --ant 1,2 --region Brazil'\n");}

void printU32List(TMR_uint32List *list)
{
  int i;
  putchar('[');
  for (i = 0; i < list->len && i < list->max; i++)
  {
    printf("%"PRIu32"%s", list->list[i], ((i + 1) == list->len) ? "" : ",");
  }
  if (list->len > list->max)
  {
    printf("...");
  }
  putchar(']');
}

void errx(int exitval, const char *fmt, ...)
{
  va_list ap;

  va_start(ap, fmt);
  vfprintf(stderr, fmt, ap);

  exit(exitval);
}

void checkerr(TMR_Reader* rp, TMR_Status ret, int exitval, const char *msg)
{
  if (TMR_SUCCESS != ret)
  {
    errx(exitval, "Error %s: %s\n", msg, TMR_strerr(rp, ret));
  }
}

void serialPrinter(bool tx, uint32_t dataLen, const uint8_t data[],
                   uint32_t timeout, void *cookie)
{
  FILE *out = cookie;
  uint32_t i;

  fprintf(out, "%s", tx ? "Sending: " : "Received:");
  for (i = 0; i < dataLen; i++)
  {
    if (i > 0 && (i & 15) == 0)
    {
      fprintf(out, "\n         ");
    }
    fprintf(out, " %02x", data[i]);
  }
  fprintf(out, "\n");
}

void stringPrinter(bool tx,uint32_t dataLen, const uint8_t data[],uint32_t timeout, void *cookie)
{
  FILE *out = cookie;

  fprintf(out, "%s", tx ? "Sending: " : "Received:");
  fprintf(out, "%s\n", data);
}

void parseAntennaList(uint8_t *antenna, uint8_t *antennaCount, char *args)
{
  char *token = NULL;
  char *str = ",";
  uint8_t i = 0x00;
  int scans;

  /* get the first token */
  if (NULL == args)
  {
    fprintf(stdout, "Missing argument\n");
    usage();
  }

  token = strtok(args, str);
  if (NULL == token)
  {
    fprintf(stdout, "Missing argument after %s\n", args);
    usage();
  }

  while(NULL != token)
  {
#ifdef WIN32
      scans = sscanf(token, "%hh"SCNu8, &antenna[i]);
#else
      scans = sscanf(token, "%"SCNu8, &antenna[i]);
#endif
    if (1 != scans)
    {
      fprintf(stdout, "Can't parse '%s' as an 8-bit unsigned integer value\n", token);
      usage();
    }
    i++;
    token = strtok(NULL, str);
  }
  *antennaCount = i;
}
#endif

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
#ifdef TMR_ENABLE_UHF
  TMR_Region region;
  TMR_RegionList regions;
  TMR_Region _regionStore[32];
  uint8_t antennaCount = 0x0;
  TMR_ReadPlan plan;
  uint8_t i, setRegion = 0;
  uint8_t buffer[20];
  uint8_t *antennaList = NULL;
  TMR_uint32List value;
  //hopTime, quantizationStep & minFrequency to be changed according to region
  uint32_t valueList[4] = { 865700,866300,866900,867500 };
  uint32_t hopTime = 100;
  uint32_t quantizationStep = 100000; /* 100 KHz */ 
  uint32_t minFrequency = 865700;
  bool lbtEnable  = true;
  bool dwellTimeEnable = true;
  /* dwellTime value Min 1 - Max 65535.
   * dwellTime default value is 100
   */
  uint32_t dwellTime = 100;
  /* LBTThreshold value should be between -128 to -1.
   * LBT default value is -72
   */
  int8_t lbtThreshold  = -72;
  enum configRegion
  {
    TMR_REG_BAHRAIN = 1,
    TMR_REG_BRAZIL = 2
  };
#endif /* TMR_ENABLE_UHF */

#ifndef BARE_METAL
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
 
  if (argc < 2)
  {
    fprintf(stdout, "Not enough arguments.  Please provide reader URL.\n");
    usage(); 
  }

#ifdef TMR_ENABLE_UHF
  for (i = 2; i < argc; i+=2)
  {
    if(0x00 == strcmp("--ant", argv[i]))
    {
      if (NULL != antennaList)
      {
        fprintf(stdout, "Duplicate argument: --ant specified more than once\n");
        usage();
      }
      parseAntennaList(buffer, &antennaCount, argv[i+1]);
      antennaList = buffer;
    }
    else if (0 == strcmp("--region", argv[i]))
    {
      if ((0 == strcmp("bahrain", argv[i+1])) || (0 == strcmp("Bahrain", argv[i+1])) || (0 == strcmp("BAHRAIN", argv[i+1])))
      {
        setRegion = 1;
      }
      else if ((0 == strcmp("brazil", argv[i+1])) || (0 == strcmp("Brazil", argv[i+1])) || (0 == strcmp("BRAZIL", argv[i+1])))
      {
        setRegion = 2;
      }
      else
      {
        fprintf(stdout, "Can't parse region value : %s\n,Please make manual changes in codelete to support custom region!", argv[i+1]);
      }
    }
    else
    {
      fprintf(stdout, "Argument %s is not recognized\n", argv[i]);
      usage();
    }
  }
#endif /* TMR_ENABLE_UHF */
#endif

  rp = &r;
#ifndef BARE_METAL
  ret = TMR_create(rp, argv[1]);
#else
  ret = TMR_create(rp, "tmr:///com1");
  buffer[0] = 1;
  antennaList = buffer;
  antennaCount = 0x01;
#endif

#ifndef BARE_METAL
  checkerr(rp, ret, 1, "creating reader");

#if USE_TRANSPORT_LISTENER

  if (TMR_READER_TYPE_SERIAL == rp->readerType)
  {
    tb.listener = serialPrinter;
  }
  else
  {
    tb.listener = stringPrinter;
  }
  tb.cookie = stdout;

  TMR_addTransportListener(rp, &tb);
#endif
#endif

  ret = TMR_connect(rp);

#ifdef TMR_ENABLE_UHF
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "connecting reader");
#endif
  region = TMR_REGION_NONE;
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "getting region");
#endif

  if (TMR_REGION_NONE == region)
  {
    regions.list = _regionStore;
    regions.max = sizeof(_regionStore)/sizeof(_regionStore[0]);
    regions.len = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_SUPPORTEDREGIONS, &regions);
#ifndef BARE_METAL
    checkerr(rp, ret, __LINE__, "getting supported regions");

    if (regions.len < 1)
    {
      checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't support any regions");
    }
#endif
    region = regions.list[0];
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting region");
#endif
  }

  value.max = numberof(valueList);
  value.len = numberof(valueList);
  value.list = valueList;

  //Step 1 - Set region to OPEN
  region = TMR_REGION_OPEN;
  ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
  checkerr(rp, ret, 1, "setting region");

  /* Step 2 - Set frequency hop table accordingly for a region. */
  switch (setRegion)
  {
    case TMR_REG_BAHRAIN:
    {
      uint32_t hopTable[4] = { 865700,866300,866900,867500 };
      value.max = numberof(hopTable);
      value.len = numberof(hopTable);
      value.list = hopTable;

      lbtEnable  = true;
      dwellTimeEnable = true;
      hopTime = 100; /* 100ms */
      quantizationStep = 100000; /* 100 KHz */
      minFrequency = 865700; /* 902MHz */
      dwellTime = 100;
      lbtThreshold = -72;
    }
    break;
    case TMR_REG_BRAZIL:
    {
      uint32_t brazilHopTable[50] = {925500,905000,906000,925000,922750,902750,921000,926000,921750,916000,
                                     920250,919000,917750,925250,906500,920500,926500,916250,904000,919250,
                                     923750,906750,917250,924000,923000,915750,926750,924250,904250,918500,
                                     923500,920000,924750,905500,903500,921500,916500,922000,924500,920750,
                                     917500,927000,922250,918750,906250,905250,903750,919750,904750,921250};
      value.max = numberof(brazilHopTable);
      value.len = numberof(brazilHopTable);
      value.list = brazilHopTable;

      hopTime = 375; /* 375ms */
      quantizationStep = 250000; /* 250 KHz */
      minFrequency = 902750; /* 902MHz */
      lbtEnable  = false;
      dwellTimeEnable = false;
    }
    break;
    default:
    {
      printf("\nSetting default region params...!\n");
    }
    break;
  }
  {
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_HOPTABLE, &value);
    checkerr(rp, ret, 1, "Setting Hoptable");
    ret = TMR_paramGet(rp, TMR_PARAM_REGION_HOPTABLE, &value);
    checkerr(rp, ret, 1, "New Hoptable");
    putchar('\n');
    printU32List(&value);
    putchar('\n');
  }

  //Set lbtEnable value to true or false
  ret = TMR_paramSet(rp, TMR_PARAM_REGION_LBT_ENABLE, &lbtEnable);  //Step 3 - Set LBT enable
  checkerr(rp, ret, 1, "Setting LBT Enable");
  //Set lbtThreshold value only if lbtEnable is set to true.
  if (lbtEnable)
  {
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_LBT_THRESHOLD, &lbtThreshold);  //Step 4 - Set LBT threshold
    checkerr(rp, ret, 1, "Setting LBT Threshold");
  }

  //Set dwellTimeEnable value to true or false
  ret = TMR_paramSet(rp, TMR_PARAM_REGION_DWELL_TIME_ENABLE, &dwellTimeEnable);  //Step 5 - Set Dwell time enable
  checkerr(rp, ret, 1, "Setting Dwell Time Enable");
  //Set dwellTime value only if dwellTimeEnable is set to true.
  if(dwellTimeEnable)
  {
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_DWELL_TIME, &dwellTime);  //Step 6 -Set Dwell time
    checkerr(rp, ret, 1, "Setting Dwell Time");
  }

  //Get & print LBT enable
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_LBT_ENABLE, &lbtEnable);
  checkerr(rp, ret, 1, "Getting LBT En");
  printf("LBT Enable: %d \n", lbtEnable);
  //Get & print LBT threshold
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_LBT_THRESHOLD, &lbtThreshold);
  checkerr(rp, ret, 1, "Setting LBT Threshold");
  printf("LBT Threshold(dBm): %d \n", lbtThreshold);
  //Get & print Dwell time enable
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_DWELL_TIME_ENABLE, &dwellTimeEnable);
  checkerr(rp, ret, 1, "Setting Dwell Time Enable");
  printf("Dwell Time Enable: %d \n", dwellTimeEnable);
  //Get & print Dwell time
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_DWELL_TIME, &dwellTime);
  checkerr(rp, ret, 1, "Setting Dwell Time");
  printf("Dwell Time(ms): %d \n", dwellTime);

  //Step 7 - Set hop time
  ret = TMR_paramSet(rp, TMR_PARAM_REGION_HOPTIME, &hopTime);
  checkerr(rp, ret, 1, "Setting Hop Time");
  //Get & print hop time
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_HOPTIME, &hopTime);
  checkerr(rp, ret, 1, "Getting Hop Time");
  printf("Hop Time: %d\n", hopTime);

  //Step 8 - Set Quantization Step
  TMR_paramSet(rp, TMR_PARAM_REGION_QUANTIZATION_STEP, &quantizationStep);
  checkerr(rp, ret, 1, "Setting Quantization Step");
  //Get & print Quantization Step
  TMR_paramGet(rp, TMR_PARAM_REGION_QUANTIZATION_STEP, &quantizationStep);
  checkerr(rp, ret, 1, "Setting Quantization Step");
  printf("Quantization Step: %d\n", quantizationStep);

  //Step 9 - Set Minimum Frequency
  TMR_paramSet(rp, TMR_PARAM_REGION_MINIMUM_FREQUENCY, &minFrequency);
  checkerr(rp, ret, 1, "Setting Minimum Frequency");
  //Get & print Minimum Frequency
  TMR_paramGet(rp, TMR_PARAM_REGION_MINIMUM_FREQUENCY, &minFrequency);
  checkerr(rp, ret, 1, "Setting Minimum Frequency");
  printf("Minimum Frequency: %d\n", minFrequency);

  /* Uncomment Step 10 to Step 12 to save OPEN region settings as persistent. */
  //{
  //  TMR_SR_UserConfigOp config;
  //  //Step 10 - Init UserConfigOp structure to save configuration
  //  TMR_init_UserConfigOp(&config, TMR_USERCONFIG_SAVE);
  //  ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
  //  checkerr(rp, ret, 1, "setting user configuration: save all configuration");
  //  printf("User config set option:save all configuration\n");

  //  //Step 11 - Init UserConfigOp structure to Restore all saved configuration parameters
  //  TMR_init_UserConfigOp(&config, TMR_USERCONFIG_RESTORE);
  //  ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
  //  checkerr(rp, ret, 1, "setting configuration: restore all saved configuration params");
  //  printf("User config set option:restore all saved configuration params\n");

  //Initialize the read plan
  ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "initializing the  read plan");
#endif

  //Commit read plan
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "setting read plan");
#endif
  ret = TMR_read(rp, 500, NULL);

#ifndef BARE_METAL
if (TMR_ERROR_TAG_ID_BUFFER_FULL == ret)
  {
    /* In case of TAG ID Buffer Full, extract the tags present
    * in buffer.
    */
    fprintf(stdout, "reading tags:%s\n", TMR_strerr(rp, ret));
  }
  else
  {
    checkerr(rp, ret, 1, "reading tags");
  }
#endif
  while (TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    TMR_TagReadData trd;
    char epcStr[128];

    ret = TMR_getNextTag(rp, &trd);
#ifndef BARE_METAL  
  checkerr(rp, ret, 1, "fetching tag");
#endif
    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);
    printf("EPC: %s\n ", epcStr);
  }
#endif /* TMR_ENABLE_UHF */

  TMR_destroy(rp);
  return 0;
}
