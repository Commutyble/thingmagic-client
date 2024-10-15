/**
 * Sample program that reads tags in the background and prints the
 * tags found for M7e and M3e.
 * The code sample also demonstrates how to connect to M6E series modules
 * and read tags using the Mercury API v1.37.1. The M6E family users must modify their
 * application by referring this code sample in order to use the latest API version 
 * (a) To enable M6E compatible code, uncomment ENABLE_M6E_COMPATIBILITY macro.
 * (b) To enable standalone tag operation, uncomment ENABLE_TAGOP_PROTOCOL macro.
 * 
 * @file readasync.c
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

/**@def ENABLE_TAGOP_PROTOCOL
 * To set tagOp protocol before performing tag operation.
 * By default, protocol is set to NONE on the M6e family.
 * Make sure to set Gen2 protocol before performing Gen2 standalone tag operation.
 */
#define ENABLE_TAGOP_PROTOCOL 0

#ifdef BARE_METAL
  #define printf(...) {}
#endif

#ifndef BARE_METAL
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
  if ((TMR_SUCCESS != ret) && (TMR_SUCCESS_STREAMING != ret))
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
      fprintf(out, "\n         ");
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

void exceptionHandler(TMR_Reader* reader);
#endif /* BARE_METAL */
void callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);

#ifdef SINGLE_THREAD_ASYNC_READ
uint32_t totalTagRcved = 0;
bool stopReadCommandSent = false;

TMR_Status
parseSingleThreadedResponse(TMR_Reader* rp, uint32_t readTime);
#endif /* SINGLE_THREAD_ASYNC_READ */

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  uint8_t buffer[20];
  uint8_t i;
  TMR_ReadPlan plan;
  TMR_ReadListenerBlock rlb;
  TMR_ReadExceptionListenerBlock reb;
  uint8_t *antennaList = NULL;
  uint8_t antennaCount = 0x0;
  char string[100];
  TMR_String model;
  uint64_t startTime;

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
  if ((TMR_READER_TYPE_SERIAL == rp->readerType) && (TMR_SUCCESS != ret))
  {
      /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
       * The connection may fail if the module is configured to a different baud rate. If
       * that is the case, the MercuryAPI tries connecting to the module with other supported
       * baud rates until the connection is successful using baud rate probing mechanism.
       */
      if (TMR_ERROR_TIMEOUT == ret)
      {
          uint32_t currentBaudRate;

          /* Start probing mechanism. */
          ret = TMR_SR_cmdProbeBaudRate(rp, &currentBaudRate);
          checkerr(rp, ret, 1, "Probe the baudrate");

          /* Set the current baudrate, so that
           * next TMR_Connect() call can use this baudrate to connect.
           */
          TMR_paramSet(rp, TMR_PARAM_BAUDRATE, &currentBaudRate);
      }

      /* When the module is streaming the tags,
       * TMR_connect() returns with TMR_SUCCESS_STREAMING status, which should be handled in the codelet.
       * User can either continue to parse streaming responses or stop the streaming.
       * Use 'stream' option demonstrated in the AutonomousMode.c codelet to
       * continue streaming. To stop the streaming, use TMR_stopStreaming() as demonstrated below.
       */
      if (TMR_SUCCESS_STREAMING == ret)
      {
          ret = TMR_stopStreaming(rp);
          checkerr(rp, ret, 1, "Stoping the read");
      }

      if (TMR_SUCCESS == ret)
      {
          ret = TMR_connect(rp);
      }     
  }
  checkerr(rp, ret, 1, "Connecting reader");

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
  }

#if TMR_ENABLE_M6E_COMPATIBILITY
  {
    /* To make the latest API compatible with M6e family modules,
     * set below configurations.
     * 1. tagOp protocol:  This parameter is not needed for Continuous Read or Async Read, but it
     *                     must be set when standalone tag operation is performed, because the
     *                     protocol is set to NONE, by default, in the M6e family modules.
     *                     So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
     * 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
     *                     This filter is enabled, by default, in the M6e family modules.
     * 3. Metadata flag:   TMR_TRD_METADATA_FLAG_ALL includes all flags (Supported by UHF and HF/LF readers).
     *                     Disable unsupported flags for M6E family as shown below.
     * Note: tagOp protocol and read filter are one time configurations - These must be set on the module 
     *       once after every power ON.
     *       We do not have to set them in every read cycle.
     *       But the Metadata flag must be set once after establishing a connection to the module using TMR_connect().
     */
#if ENABLE_TAGOP_PROTOCOL
    {
      /* 1. tagOp protocol: This parameter is not needed for Continuous Read or Async Read, but it
      *                    must be set when standalone tag operation is performed, because the
      *                    protocol is set to NONE, by default, in the M6e family modules.
      *                    So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
      */
      TMR_TagOp readop;
      TMR_TagProtocol protocol = TMR_TAG_PROTOCOL_GEN2;
      ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_PROTOCOL, &protocol);
      checkerr(rp, ret, 1, "setting protocol");
   
      TMR_TagOp_init_GEN2_ReadData(&readop, TMR_GEN2_BANK_EPC, 0, 2);
      ret = TMR_executeTagOp(rp, &readop, NULL, NULL);
      checkerr(rp, ret, 1, "executing read data tag operation");
    }
#endif /* ENABLE_TAGOP_PROTOCOL */

    {
      /* 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
       *                     This filter is enabled, by default, in the M6e family modules.
       *                     Note that this is a one time configuration while connecting to the module after
       *                     power ON. We do not have to set it for every read cycle.
       */
      bool readFilter = false;
      ret = TMR_paramSet(rp, TMR_PARAM_TAGREADDATA_ENABLEREADFILTER, &readFilter);
      checkerr(rp, ret, 1, "setting read filter");
    }

    {
      /* 3. Metadata flag: TMR_TRD_METADATA_FLAG_ALL includes all flags (Supported by UHF and HF/LF readers).
       *                   Disable unsupported flags for M6e family as shown below.
       */
      TMR_TRD_MetadataFlag metadata = (uint16_t)(TMR_TRD_METADATA_FLAG_ALL & (~TMR_TRD_METADATA_FLAG_TAGTYPE));
      ret = TMR_paramSet(rp, TMR_PARAM_METADATAFLAG, &metadata);
      checkerr(rp, ret, 1, "Setting Metadata Flags");
    }
  }
#endif /* TMR_ENABLE_M6E_COMPATIBILITY */

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
  checkerr(rp, ret, 1, "initializing the read plan");

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
  checkerr(rp, ret, 1, "setting read plan");

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

#ifndef SINGLE_THREAD_ASYNC_READ
  /* Exit the while loop,
   * 1. When error occurs 
   * 2. When sleep timeout expires
   */
  startTime = tmr_gettime();
  while(1)
  {
    if((tmr_gettime() - startTime) >= READ_TIME)
    {
      /* break if sleep timeout expired */
      break;
    }

    /* Exit the process if any error occurred */
    if(rp->lastReportedException != TMR_SUCCESS)
    {
      exceptionHandler(rp);

      /* Can add recovery mechanism here*/
      /* DO some work here */
    }

#ifndef WIN32
  usleep(1);
#else
  Sleep(1);
#endif
  }

  ret = TMR_stopReading(rp);
  checkerr(rp, ret, 1, "stopping reading");
#else
  parseSingleThreadedResponse(rp, READ_TIME);
#endif /* SINGLE_THREAD_ASYNC_READ */

  TMR_destroy(rp);
  return 0;

}

void
callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];
  char timeStr[128];

  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
#ifndef BARE_METAL
  TMR_getTimeStamp(reader, t, timeStr);
#endif /* BARE_METAL */
  printf("Background read: Tag ID:%s ant:%d count:%d ", epcStr, t->antenna, t->readCount);
  printf("time:%s\n", timeStr);

  /* Reset the variable for valid tag response. */
  reader->lastReportedException = 0;
}

void 
exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie)
{
  if(reader->lastReportedException != error)
  {
#ifndef BARE_METAL
    fprintf(stdout, "Error:%s\n", TMR_strerr(reader, error));
#endif /* BARE_METAL */
  }

  reader->lastReportedException = error;
}

#ifndef BARE_METAL
void exceptionHandler(TMR_Reader *reader)
{
  TMR_Status ret = TMR_SUCCESS;
  switch(reader->lastReportedException)
  {
    case TMR_ERROR_MSG_INVALID_PARAMETER_VALUE:
    case TMR_ERROR_UNIMPLEMENTED_FEATURE:
    {
      ret = TMR_stopReading(reader);
      checkerr(reader, ret, 1, "stopping reading");
      TMR_destroy(reader);
      exit(1);
    }
    case TMR_ERROR_TIMEOUT:
    {
      TMR_flush(reader);
      TMR_destroy(reader);
      exit(1);
    }
    case TMR_ERROR_BUFFER_OVERFLOW:
    {
      printf("!!! API buffer overflow occurred. It may be due to more processing delay in the read listener. !!!\n");
      printf("!!! As part of recovery mechanism, API will stop and restart the read. !!!\n\n");
      printf("To avoid the error :\n1) It is advisable keep read listener as faster as possible.\n2) Increase queue slot size by modifying TMR_MAX_QUEUE_SLOTS macro value in tm_config.h file.\n\n");
      break;
    }
    case TMR_ERROR_SYSTEM_UNKNOWN_ERROR:
    case TMR_ERROR_TM_ASSERT_FAILED:
    case TMR_ERROR_UNSUPPORTED:
    {
      TMR_destroy(reader);
      exit(1);
    }
    default:
    {
      break;
    }
  }
}
#endif /* BARE_METAL */

#ifdef SINGLE_THREAD_ASYNC_READ
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
      TMR_getNextTag(rp, &trd);
      notify_read_listeners(rp, &trd);
      totalTagRcved++;
    }
    else if (TMR_ERROR_END_OF_READING == ret)
    {
      break;
    }
    else
    {
      if ((TMR_ERROR_NO_TAGS != ret) && (TMR_ERROR_NO_TAGS_FOUND != ret))
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
#endif /* SINGLE_THREAD_ASYNC_READ */
