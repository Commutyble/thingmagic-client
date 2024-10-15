/**
 * Sample programme that gets and prints the reader stats
 * It shows both the sync and async way of getting the reader stats.
 * @file readerstats.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#ifndef BARE_METAL
#ifndef WIN32
#include <unistd.h>
#endif
#endif /* BARE_METAL */

 /* Change total read time here */
#define READ_TIME 5000 //In ms

#ifdef BARE_METAL
  #define printf(...) {}
#endif

#ifndef BARE_METAL
#if WIN32
#define snprintf sprintf_s
#endif

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "Example for UHF modules: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' \n"\
                         "Example for HF/LF modules: 'tmr:///com4' \n");}

void errx(int exitval, const char *fmt, ...)
{
  va_list ap;

  va_start(ap, fmt);
  vfprintf(stderr, fmt, ap);

  exit(exitval);
}
#endif /* BARE_METAL */

void checkerr(TMR_Reader* rp, TMR_Status ret, int exitval, const char *msg)
{
#ifndef BARE_METAL
  if (TMR_SUCCESS != ret)
  {
    errx(exitval, "Error %s: %s\n", msg, TMR_strerr(rp, ret));
  }
#endif /* BARE_METAL */
}

#ifdef USE_TRANSPORT_LISTENER
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
#endif /* USE_TRANSPORT_LISTENER */

#ifndef BARE_METAL
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
#endif /* BARE_METAL */

void callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);
void statsCallback (TMR_Reader *reader, const TMR_Reader_StatsValues* stats, void *cookie);

#ifdef TMR_ENABLE_UHF
static char _protocolNameBuf[32];
static const char* protocolName(enum TMR_TagProtocol value)
{
  switch (value)
  {
  case TMR_TAG_PROTOCOL_NONE:
    return "NONE";
  case TMR_TAG_PROTOCOL_GEN2:
    return "GEN2";
#ifdef TMR_ENABLE_ISO180006B
  case TMR_TAG_PROTOCOL_ISO180006B:
    return "ISO180006B";
  case TMR_TAG_PROTOCOL_ISO180006B_UCODE:
    return "ISO180006B_UCODE";
#endif /* TMR_ENABLE_ISO180006B */
#ifndef TMR_ENABLE_GEN2_ONLY
  case TMR_TAG_PROTOCOL_IPX64:
    return "IPX64";
  case TMR_TAG_PROTOCOL_IPX256:
    return "IPX256";
  case TMR_TAG_PROTOCOL_ATA:
	return "ATA";
#endif /* TMR_ENABLE_GEN2_ONLY */
  case TMR_TAG_PROTOCOL_ISO14443A:
    return "ISO14443A";
  case TMR_TAG_PROTOCOL_ISO15693:
    return "ISO15693";
  case TMR_TAG_PROTOCOL_LF125KHZ:
    return "LF125KHZ";
  case TMR_TAG_PROTOCOL_LF134KHZ:
    return "LF134KHZ";
  default:
    snprintf(_protocolNameBuf, sizeof(_protocolNameBuf), "TagProtocol:%d", (int)value);
    return _protocolNameBuf;
  }
}
#endif /* TMR_ENABLE_UHF */

void parseReaderStas(const TMR_Reader_StatsValues *stats)
{
#ifdef TMR_ENABLE_UHF
  uint8_t i = 0;

  /** Each  field should be validated before extracting the value */
  if (TMR_READER_STATS_FLAG_CONNECTED_ANTENNAS & stats->valid)
  {
    printf("Antenna Connection Status\n");

    for (i = 0; i < stats->connectedAntennas.len; i += 2)
    {
      printf("Antenna %d |%s\n", stats->connectedAntennas.list[i],
               stats->connectedAntennas.list[i + 1] ? "connected":"Disconnected");
    }
  }

  if (TMR_READER_STATS_FLAG_NOISE_FLOOR_SEARCH_RX_TX_WITH_TX_ON & stats->valid)
  {
    printf("Noise Floor With Tx On\n");

    for (i = 0; i < stats->perAntenna.len; i++)
    {
      printf("Antenna %d | %d db\n", stats->perAntenna.list[i].antenna,
               stats->perAntenna.list[i].noiseFloor);
    }
  }

  if (TMR_READER_STATS_FLAG_RF_ON_TIME & stats->valid)
  {
    printf("RF On Time\n");

    for (i = 0; i < stats->perAntenna.len; i++)
    {
      printf("Antenna %d | %d ms\n", stats->perAntenna.list[i].antenna,
               stats->perAntenna.list[i].rfOnTime);
    }
  }

  if (TMR_READER_STATS_FLAG_FREQUENCY & stats->valid)
  {
    printf("Frequency %d(khz)\n", stats->frequency);
  }
#endif /* TMR_ENABLE_UHF */

  if (TMR_READER_STATS_FLAG_TEMPERATURE & stats->valid)
  {
    printf("Temperature %d(C)\n", stats->temperature);
  }

#ifdef TMR_ENABLE_UHF
  if (TMR_READER_STATS_FLAG_PROTOCOL & stats->valid)
  {
    printf("Protocol %s\n", protocolName(stats->protocol));
  }

  if (TMR_READER_STATS_FLAG_ANTENNA_PORTS & stats->valid)
  {
    printf("currentAntenna %d\n", stats->antenna);
  }
#endif /* TMR_ENABLE_UHF */
}

#ifdef BARE_METAL
uint32_t totalTagRcved   = 0;
bool stopReadCommandSent = false;

TMR_Status
parseSingleThreadedResponse(TMR_Reader* rp, uint32_t readTime);
#endif /* BARE_METAL */

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  uint8_t *antennaList = NULL;
  uint8_t antennaCount = 0x0;
  TMR_ReadPlan plan;
  TMR_ReadListenerBlock rlb;
  TMR_ReadExceptionListenerBlock reb;
  TMR_StatsListenerBlock slb;
  uint8_t buffer[20];
  uint8_t i;
  TMR_Region region;
  char string[100];
  TMR_String model;

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif /* USE_TRANSPORT_LISTENER */
  rp = &r;

#ifndef BARE_METAL
  if (argc < 2)
  {
    usage();
  }

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
    else
    {
      fprintf(stdout, "Argument %s is not recognized\n", argv[i]);
      usage();
    }
  }
  ret = TMR_create(rp, argv[1]);
  checkerr(rp, ret, 1, "creating reader");
#else
  ret = TMR_create(rp, "tmr:///com1");

#ifdef TMR_ENABLE_UHF
  buffer[0] = 1;
  antennaList = buffer;
  antennaCount = 0x01;
#endif /* TMR_ENABLE_UHF */
#endif /* BARE_METAL */

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
#endif /* USE_TRANSPORT_LISTENER */

  ret = TMR_connect(rp);
  /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
   * The connection may fail if the module is configured to a different baud rate. If
   * that is the case, the MercuryAPI tries connecting to the module with other supported
   * baud rates until the connection is successful using baud rate probing mechanism.
   */
  if((ret == TMR_ERROR_TIMEOUT) && 
     (TMR_READER_TYPE_SERIAL == rp->readerType))
  {
    uint32_t currentBaudRate;

    /* Start probing mechanism. */
    ret = TMR_SR_cmdProbeBaudRate(rp, &currentBaudRate);
    checkerr(rp, ret, 1, "Probe the baudrate");

    /* Set the current baudrate, so that 
     * next TMR_Connect() call can use this baudrate to connect.
     */
    ret = TMR_paramSet(rp, TMR_PARAM_BAUDRATE, &currentBaudRate);
    checkerr(rp, ret, 1, "Setting baudrate"); 

    /* Connect using current baudrate */
    ret = TMR_connect(rp);
    checkerr(rp, ret, 1, "Connecting reader");
  }
  else
  {
    checkerr(rp, ret, 1, "Connecting reader");
  }

  model.value = string;
  model.max   = sizeof(string);
  TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);
  checkerr(rp, ret, 1, "Getting version model");

  if (0 != strcmp("M3e", model.value))
  {
    region = TMR_REGION_NONE;
    ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
    checkerr(rp, ret, 1, "getting region");

    if (TMR_REGION_NONE == region)
    {
      TMR_RegionList regions;
      TMR_Region _regionStore[32];
      regions.list = _regionStore;
      regions.max = sizeof(_regionStore)/sizeof(_regionStore[0]);
      regions.len = 0;

      ret = TMR_paramGet(rp, TMR_PARAM_REGION_SUPPORTEDREGIONS, &regions);
      checkerr(rp, ret, __LINE__, "getting supported regions");

      if (regions.len < 1)
      {
        checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't supportany regions");
      }
      region = regions.list[0];
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
      checkerr(rp, ret, 1, "setting region");  
    }

#ifdef TMR_ENABLE_UHF
    {
      bool checkPort = true;
      
      ret = TMR_paramSet(rp, TMR_PARAM_ANTENNA_CHECKPORT, &checkPort);
      checkerr(rp, ret, 1, "setting antenna checkport");
    }
#endif /* TMR_ENABLE_UHF */
  }

  /**
  * for antenna configuration we need two parameters
  * 1. antennaCount : specifies the no of antennas should
  *    be included in the read plan, out of the provided antenna list.
  * 2. antennaList  : specifies  a list of antennas for the read plan.
  **/ 

  // initialize the read plan
  if (0 != strcmp("M3e", model.value))
  {
    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
  }
  else
  {
    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO14443A, 1000);
  }
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "initializing the  read plan");
#endif

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
  checkerr(rp, ret, 1, "setting read plan");

  {
    /* Code to get the reader stats after the sync read */
    TMR_Reader_StatsValues stats;
    TMR_Reader_StatsFlag setFlag;
#ifdef TMR_ENABLE_UHF
    TMR_PortValueList value;
    TMR_PortValue valueList[64];
    int i;
#endif /* TMR_ENABLE_UHF */
    int j;


    printf("\nReader stats after the sync read\n");


    /** request for the statics fields of your interest, before search
      * Temperature and Antenna port stats are mandatory for TMReader and it don't allow to disable these two flags
     **/
    setFlag = TMR_READER_STATS_FLAG_ALL;

    ret = TMR_paramSet(rp, TMR_PARAM_READER_STATS_ENABLE, &setFlag);
    checkerr(rp, ret, 1, "Setting the reader stats flag");

    for (j = 1; j < 4; j++)
    {
      /**
       * perform three iterations to see that reader stats are
       * resetting after each search operation.
       **/ 
      printf("\nIteration:%d\n", j);
      /**
       * performing the search operation. for 1 sec,
       * Individual search will reset the reader stats, before doing the search
       */
      printf("Performing the search operation. for 1 sec\n");


      ret = TMR_read(rp, 1000, NULL);
      if (TMR_ERROR_TAG_ID_BUFFER_FULL == ret)
      {
        /* In case of TAG ID Buffer Full, extract the tags present
        * in buffer.
        */
#ifndef BARE_METAL
        fprintf(stdout, "reading tags:%s\n", TMR_strerr(rp, ret));
#endif /* BARE_METAL */
      }
      else
      {
        checkerr(rp, ret, 1, "reading tags");
      }

      while (TMR_SUCCESS == TMR_hasMoreTags(rp))
      {
        TMR_TagReadData trd;
        char epcStr[128];

        ret = TMR_getNextTag(rp, &trd);
        checkerr(rp, ret, 1, "fetching tag");
        TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);
        printf("EPC: %s \n", epcStr);

       }

#ifdef TMR_ENABLE_UHF
      /** Initialize the reader statics variable to default values */
      TMR_STATS_init(&stats);
#endif /* TMR_ENABLE_UHF */

      /* Search is completed. Get the reader stats */
      printf("Search is completed. Get the reader stats\n");
      ret = TMR_paramGet(rp, TMR_PARAM_READER_STATS, &stats);
      checkerr(rp, ret, 1, "getting the reader statistics");

      parseReaderStas(&stats);

#ifdef TMR_ENABLE_UHF
      if (0 != strcmp("M3e", model.value))
      {
        /* Get the antenna return loss value, this parameter is not the part of reader stats */
        value.max = sizeof(valueList)/sizeof(valueList[0]);
        value.list = valueList;

        ret = TMR_paramGet(rp, TMR_PARAM_ANTENNA_RETURNLOSS, &value);
        checkerr(rp, ret, 1, "getting the antenna return loss");

        printf("Antenna Return Loss\n");

        for (i = 0; i < value.len && i < value.max; i++)
        {
          printf("Antenna %d | %d \n", value.list[i].port, value.list[i].value);
        }
      }
#endif /* TMR_ENABLE_UHF */
    }
  }

  {
    /* Code to get the reader stats after the async read */
    TMR_Reader_StatsFlag setFlag = TMR_READER_STATS_FLAG_ALL;

    rlb.listener = callback;
    rlb.cookie = NULL;

    reb.listener = exceptionCallback;
    reb.cookie = NULL;

    slb.listener = statsCallback;
    slb.cookie = NULL;

    ret = TMR_addReadListener(rp, &rlb);
    checkerr(rp, ret, 1, "adding read listener");

    ret = TMR_addReadExceptionListener(rp, &reb);
    checkerr(rp, ret, 1, "adding exception listener");

    ret = TMR_addStatsListener(rp, &slb);
    checkerr(rp, ret, 1, "adding the stats listener");

    printf("\nReader stats after the async read \n");


    /** request for the statics fields of your interest, before search */
    ret = TMR_paramSet(rp, TMR_PARAM_READER_STATS_ENABLE, &setFlag);
    checkerr(rp, ret, 1, "setting the  fields");

    printf("Initiating the search operation. for 1 sec and the listener will provide the reader stats\n");


    ret = TMR_startReading(rp);
    checkerr(rp, ret, 1, "starting reading");

#ifndef BARE_METAL
#ifndef WIN32
    sleep(1);
#else
    Sleep(1000);
#endif

    ret = TMR_stopReading(rp);
    checkerr(rp, ret, 1, "stopping reading");
#else
    parseSingleThreadedResponse(rp, READ_TIME);
#endif /* BARE_METAL */
  }

  TMR_destroy(rp);
  return 0;
}

void
callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];

  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
  printf("Background read: %s\n", epcStr);

}

void 
exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie)
{
#ifndef BARE_METAL
  fprintf(stdout, "Error:%s\n", TMR_strerr(reader, error));
#endif /* BARE_METAL */
}

void statsCallback (TMR_Reader *reader, const TMR_Reader_StatsValues* stats, void *cookie)
{
#ifndef BARE_METAL
  parseReaderStas(stats);
#endif /* BARE_METAL */
}

#ifdef BARE_METAL
TMR_Status
parseSingleThreadedResponse(TMR_Reader* rp, uint32_t readTime)
{
  TMR_Status ret = TMR_SUCCESS;
  uint32_t elapsedTime = 0;
  uint64_t startTime = tmr_gettime();

  while (true)
  {
    TMR_TagReadData trd;

    ret = TMR_hasMoreTags(rp);
    if (TMR_SUCCESS == ret)
    {
      if (false == rp->isStatusResponse)
      {
        TMR_TagReadData trd;

        TMR_getNextTag(rp, &trd);
        notify_read_listeners(rp, &trd);
        totalTagRcved++;
      }
      else
      {
        TMR_Reader_StatsValues stats;
        TMR_SR_SerialReader* sr;

        sr = &rp->u.serialReader;
        TMR_STATS_init(&stats);

        TMR_parseTagStats(rp, &stats, sr->bufResponse, sr->bufPointer);
        notify_stats_listeners(rp, &stats);
      }
    }
    else if (TMR_ERROR_END_OF_READING == ret)
    {
      break;
    }
    else
    {
      if (TMR_ERROR_NO_TAGS != ret)
      {
        notify_exception_listeners(rp, ret);
      }
    }

    elapsedTime = tmr_gettime() - startTime;

    if ((elapsedTime > readTime) && (!stopReadCommandSent))
    {
      ret = TMR_stopReading(rp);
      if (TMR_SUCCESS == ret)
      {
        stopReadCommandSent = true;
      }
    }
  }
  reset_continuous_reading(rp);
  
  return TMR_SUCCESS;
}
#endif /* BARE_METAL */