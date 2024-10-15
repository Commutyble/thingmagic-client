/**
 * Sample program that reads tags in the background and activates GPOs 
 * based on whether a whitelisted tag is seen or not.
 *
 *  * Assume one tag is presented at a time
 *    * (For bonus points, tolerate stray clutter tags to some extent)
 *  * If the tag presented is whitelisted, pulse the first GPO (e.g., permit entry)
 *  * If the tag presented is *not* whitelisted, pulse the second GPO (e.g., provide a "Denied" signal)
 *  * Tolerate high tag repeat rates -- even if the current tag is reported 100s of times per second,
 *    * Don't bog down.  Still react to tags expediently
 */

/*
 * !!!! WARNING !!!!
 * DO NOT call GPIO functions (e.g., TMR_gpoSet) inside the read listener callback function!
 * It WILL DEADLOCK the Mercury API!
 * 
 * Due to architectural issues in the Mercury API's resource locking scheme, you must return
 * from the read listener callback before the GPIO functions can acquire all of their locks.
 * This means that the read listener callback should merely set signals, leaving the actual
 * GPIO handling to be done on a separate thread.
 */
 
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
// #define READ_TIME 5000 //In ms
#define READ_TIME 0  // Special Value: 0 = Never stop reading

#ifndef BARE_METAL
/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif


#define ENABLE_DEBUG (0)
#if 0 != ENABLE_DEBUG
  #define dbg printf
#else
  #define dbg(...) {}
#endif


/** App state driven by tag read listener
 * Used mainly to collate all the incoming tag reads.
 * 
 * Read callback (called from a Mercury API thread) compares each tag read against a whitelist.
 * * If tag is on the whitelist, update tLastWhiteRead with current time.
 * * If tag is *not* on the whitelist, update tLastBlackRead with current time.
 * 
 * Another thread (provided by app developer - may be the main thread) periodically samples
 * its own current time and compares against last-read times.  A last-read time is considered "fresh"
 * if its elapsed time (current time - last-read time) falls beneath a threshold (defined by the app).
 * * If a whitelisted tag is fresh, then signal "admitted"
 * * If a non-whitelisted tag is fresh, then signal "denied"
 * * Else do nothing
 */
typedef struct {
  /** Time of last read of a whitelisted tag (in milliseconds) */
  uint64_t tLastWhiteRead;

  /** Time of last read of a non-whitelisted tag (in milliseconds) */
  uint64_t tLastBlackRead;
} TagReadState;


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
    scans = sscanf(token, "%"SCNu8, &antenna[i]);
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
void onTagRead(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);

#ifdef BARE_METAL
uint32_t totalTagRcved = 0;
bool stopReadCommandSent = false;

TMR_Status
parseSingleThreadedResponse(TMR_Reader* rp, uint32_t readTime);
#endif /* BARE_METAL */

/** Set GPO output value
 * @param rdr  Handle of reader to control
 * @param id  Numeric ID of GPIO pin
 * @param high  true to set pin high, false to set pin low
 * @return  error code
 */
TMR_Status _setGPO( TMR_Reader *rdr, uint8_t id, bool high )
{
  TMR_Status ret = TMR_ERROR_SYSTEM_UNKNOWN_ERROR;

  TMR_GpioPin state = {
    .id = id,
    .high = high,
  };
  uint8_t stateCount = 1;
  ret = TMR_gpoSet(rdr, stateCount, &state);

  return ret;
}

void doGPOs( TMR_Reader* rp, TagReadState* trsp )
{
  uint64_t now = tmr_gettime(); 

  /** How old can a read be for its tag to still be considered present? */
  #define FRESH_THRESHOLD (250)  // milliseconds

  uint64_t elapsedWhite = now - trsp->tLastWhiteRead;
  uint64_t elapsedBlack = now - trsp->tLastBlackRead;

  if ( elapsedWhite <= FRESH_THRESHOLD )
  {
    printf("++++ Admitted ++++\n");
    #define ADMIT_GPO 2
    _setGPO( rp, ADMIT_GPO, 1 );
    tmr_sleep(250);
    _setGPO( rp, ADMIT_GPO, 0 );
  }
  else if ( elapsedBlack <= FRESH_THRESHOLD )
  {
    printf("---- DENIED ----\n");
    #define DENY_GPO 3
    int i;
    for ( i=0; i<3; i++ )
    {
      _setGPO( rp, DENY_GPO, 1 );
      tmr_sleep(50);
      _setGPO( rp, DENY_GPO, 0 );
      tmr_sleep(50);
    }
  }
  else
  {
  }
}

void testGPOs( TMR_Reader* rp )
{
  tmr_sleep(300);  // Mark start of cycle (for easier reading on scope)

  int id;
  // for ( id=0; id<=6; id++ )  // Test all GPO numbers (The values vary by product)
  for ( id=2; id<=3; id++ )  // Sargas: (id 2 = Pin 3 (User OUT 1), (id 3 = Pin 4 (User OUT 2))
  {
    int nHigh;
    for ( nHigh=1; nHigh>=0; nHigh-- )
    {
      bool high = (bool)nHigh;
      dbg( "id=%d, high=%d", id, high );  fflush(stdout);

      TMR_Status ret;
      ret = _setGPO( rp, id, high );
      if ( TMR_SUCCESS == ret )
      {
        dbg("\n");
      }
      else
      {
        dbg( "  ret=%d, 0x%X (%s)\n", ret, ret, TMR_strerror(ret) );
      }

      tmr_sleep( 100 );
    }
  }
}


int main(int argc, char *argv[])
{
  dbg("START\n");

  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  uint8_t buffer[20];
  uint8_t i;
  TMR_ReadPlan plan;
  TMR_ReadListenerBlock rlb;
  TagReadState rlState = {
    .tLastBlackRead = 0,
    .tLastWhiteRead = 0,
  };
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
  checkerr(rp, ret, 1, "connecting reader");

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
  else
  {
    if (antennaList != NULL)
      {
#ifndef BARE_METAL
        printf("Module doesn't support antenna input\n");
        usage();
#endif /* BARE_METAL */
      }
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
  checkerr(rp, ret, 1, "initializing the read plan");

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
  checkerr(rp, ret, 1, "setting read plan");

  rlb.listener = onTagRead;
  rlb.cookie = &rlState;

  reb.listener = exceptionCallback;
  reb.cookie = NULL;

  ret = TMR_addReadListener(rp, &rlb);
  checkerr(rp, ret, 1, "adding read listener");

  ret = TMR_addReadExceptionListener(rp, &reb);
  checkerr(rp, ret, 1, "adding exception listener");

  ret = TMR_startReading(rp);
  checkerr(rp, ret, 1, "starting reading");

#ifndef BARE_METAL
  /* Exit the while loop,
   * 1. When error occurs 
   * 2. When sleep timeout expires
   */
  startTime = tmr_gettime();
  while(1)
  {
    if( (0 != READ_TIME)
        &&
        ((tmr_gettime() - startTime) >= READ_TIME) )
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

      return 0;
    }

    doGPOs(rp, &rlState);

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
#endif /* BARE_METAL */

  TMR_destroy(rp);
  return 0;

}

bool isWhiteListed(const char* epcStr)
{
  // Hard-coded hexstring whitelist, for demo purposes
  // (Real-world applications may substitute more sophisticated methods)
  const char* whitelist[] = {
    "E28011606000020529BE13C7"
  };
  int i;
  for ( i=0; i<sizeof(whitelist)/sizeof(whitelist[0]); i++ )
  {
    const char* whiteID = whitelist[i];
    if (0 == strncasecmp(epcStr, whiteID, strlen(whiteID)))
    {
      dbg("  WHITE: %s\n", epcStr);
      return true;
    }
  }
  dbg("  black: %s\n", epcStr);
  return false;
}

void
onTagRead(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  /*
   * !!!! WARNING !!!!
   * DO NOT call GPIO functions (e.g., TMR_gpoSet) from inside this function!
   * It WILL DEADLOCK the Mercury API!  (See top of this file for more details.)
   */

  uint64_t now = tmr_gettime();
  TagReadState* state = (TagReadState*)cookie;

  char epcStr[128];
  char timeStr[128];


  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
  if ( isWhiteListed(epcStr) )
  {
    state->tLastWhiteRead = now;
  }
  else
  {
    state->tLastBlackRead = now;
  }

#ifndef BARE_METAL
  TMR_getTimeStamp(reader, t, timeStr);

  printf("Background read: Tag ID:%s ant:%d count:%d ", epcStr, t->antenna, t->readCount);
  printf("time:%s\n", timeStr);
#endif /* BARE_METAL */
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
    }
    default:
    {
      /* Do not send stop read for unknown errors */
      TMR_destroy(reader);
    }
  }
}
#endif /* BARE_METAL */

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
      if (TMR_ERROR_NO_TAGS != ret)
      {
        notify_exception_listeners(rp, ret);
      }
    }

    elapsedTime = tmr_gettime() - startTime;

    if (( (0 != readTime) && (elapsedTime > readTime) ) && (!stopReadCommandSent))
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
