/**
 * Sample program that reads the number of tags requested
 * and prints the tags. It is currently not supporting streaming.
 * stop trigger.
 * @file readstopTrigger.c 
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#ifndef WIN32
#include <unistd.h>
#endif

/* Enable this to use async read */
#define ENABLE_SYNC_READ 0

/* Enable this to use simple read-plan */
#define ENABLE_SIMPLE_READ_PLAN 1

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define SUBPLAN_MAX (6)

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "Examplefor UHF modules: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' \n"\
                         "Example for HF/LF modules: 'tmr:///com4' \n");}

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

const char* protocolName(TMR_TagProtocol protocol)
{
  switch (protocol)
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
      return "unknown";
  }
}

#if !ENABLE_SYNC_READ
void callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);
#endif

uint16_t totalTagCount = 0;
int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  uint8_t i;
  uint8_t buffer[20];
  char string[100];
  TMR_String model;
  uint8_t antennaCount = 0x0;
  uint8_t *antennaList = NULL;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
#if !ENABLE_SYNC_READ
  TMR_ReadListenerBlock rlb;
  TMR_ReadExceptionListenerBlock reb;
#endif
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

  rp = &r;
  ret = TMR_create(rp, argv[1]);
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
  }
{
  TMR_ReadPlan readPlan;
#if !ENABLE_SIMPLE_READ_PLAN
  TMR_ReadPlan subplans[SUBPLAN_MAX];
  TMR_ReadPlan* subplanPtrs[SUBPLAN_MAX];
  TMR_TagProtocolList protocolList;
  TMR_TagProtocol value[TMR_MAX_PROTOCOLS];
  int subplanCount = 0;
  int j;
  bool isDynamicSwEnable = true;
#endif
  uint32_t tagCount = 5;

#ifdef TMR_ENABLE_UHF
  if (0 != strcmp("M3e", model.value))
  {
    TMR_SR_GEN2_Q qValue;

    /* Initial test setup */
    /* Set the Q value as per the tag population */
    qValue.type = TMR_SR_GEN2_Q_STATIC;
    qValue.u.staticQ.initialQ = 0;

    ret = TMR_paramSet(rp, TMR_PARAM_GEN2_Q, &qValue);
    checkerr(rp, ret, 1, "setting the Q Value");  
  }
#endif /* TMR_ENABLE_UHF */

#if ENABLE_SIMPLE_READ_PLAN
  /* The simple read plan */
  if (0 != strcmp("M3e", model.value))
  {
    ret = TMR_RP_init_simple(&readPlan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
  }
  else
  {
    ret = TMR_RP_init_simple(&readPlan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO14443A, 1000);
  }
  checkerr(rp, ret, 1, "initializing the  read plan");

  /* Stop N trigger */
  TMR_RP_set_stopTrigger (&readPlan, tagCount);
#else
  /* The multi read plan */
  {
    protocolList.max = TMR_MAX_PROTOCOLS;
    protocolList.list = value;
    protocolList.len = 0;

    /* Before setting the readplen, we must get list of supported protocols */
    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_SUPPORTEDPROTOCOLS, &protocolList);
    checkerr(rp, ret, 1, "Getting the supported protocols");

#if ENABLE_SYNC_READ
    isDynamicSwEnable = false;
#endif

    if ((0 == strcmp("M3e", model.value)) && isDynamicSwEnable)
    {
#ifdef TMR_ENABLE_HF_LF
      ret = TMR_paramSet(rp, TMR_PARAM_PROTOCOL_LIST, &protocolList);
      checkerr(rp, ret, 1, "Setting protocol list");

      /**
       * If the protocol list is set through TMR_PARAM_PROTOCOL_LIST param,
       * then read plan protocol has no significance
       */
      ret = TMR_RP_init_simple(&readPlan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO14443A, 1000); 
      checkerr(rp, ret, 1, "creating simple read plan");

       /* Stop N trigger */
       TMR_RP_set_stopTrigger (&readPlan, tagCount);
#endif /* TMR_ENABLE_HF_LF */
    }
    else
    {
      for (i = 0; i < protocolList.len && i < protocolList.max; i++)
      {
        ret = TMR_RP_init_simple(&subplans[subplanCount], antennaCount, antennaList, protocolList.list[i], 0);
        /* Stop N trigger */
        TMR_RP_set_stopTrigger (&subplans[subplanCount++], tagCount);
      }

      for (i = 0; i < subplanCount; i++)
      {
        subplanPtrs[i] = &subplans[i];
      }

      ret = TMR_RP_init_multi(&readPlan, subplanPtrs, subplanCount, 0);
      checkerr(rp, ret, 1, "creating multi read plan");
    }
  }
#endif

  /* Set the read plan */
   ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &readPlan);
  checkerr(rp, ret, 1, "setting read plan");

    /* The sync read */
#if ENABLE_SYNC_READ
  {
    ret = TMR_read(rp, 1000, NULL);
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

    while (TMR_SUCCESS == TMR_hasMoreTags(rp))
    {
      TMR_TagReadData trd;
      char epcStr[128];
      char timeStr[128];

      ret = TMR_getNextTag(rp, &trd);
      checkerr(rp, ret, 1, "fetching tag");

      TMR_getTimeStamp(rp, &trd, timeStr);
      TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);

      printf("Tag ID:%s ant:%d count:%d ", epcStr, trd.antenna, trd.readCount);
      printf("time:%s protocol:%s\n", timeStr, protocolName(trd.tag.protocol));
      totalTagCount += trd.readCount;
    }
  }
#else
  {
    rlb.listener = callback;
    rlb.cookie = NULL;

    reb.listener = exceptionCallback;
    reb.cookie = NULL;

    ret = TMR_addReadListener(rp, &rlb);
    checkerr(rp, ret, 1, "adding read listener");

    ret = TMR_addReadExceptionListener(rp, &reb);
    checkerr(rp, ret, 1, "adding exception listener");

    ret = TMR_startReading(rp);
    checkerr(rp, ret, 1, "starting reading");

    //Wait till tag read completion.
    while(!TMR_isReadStopped(rp));
  }
#endif
}
printf("\nTotal tag count: %d\n", totalTagCount);
TMR_destroy(rp);
return 0;
}

#if !ENABLE_SYNC_READ
void
callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];
  char timeStr[128];

  totalTagCount++;
  TMR_getTimeStamp(reader, t, timeStr);
  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);

  printf("Background read: Tag ID:%s ant:%d count:%d ", epcStr, t->antenna, t->readCount);
  printf("time:%s protocol:%s\n", timeStr, protocolName(t->tag.protocol));
}

void 
exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie)
{
  fprintf(stdout, "Error:%s\n", TMR_strerr(reader, error));
}
#endif
