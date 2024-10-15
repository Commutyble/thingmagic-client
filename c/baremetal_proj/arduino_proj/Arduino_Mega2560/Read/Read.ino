#include <tm_reader.h>

#include <serial_reader_imp.h>

#define ERROR_BLINK_COUNT				10
#define ERROR_BLINK_INTERVAL			100

/* Description: This macro is used to set params needed to make this code sample compatible with M6e modules.
 *              This code sample is compatible with M7e modules by default.
 *              1 : To enable M6e compatible params.
 *              0 : To disable M6e compatible params.
 */
#define SET_M6E_COMPATIBLE_PARAMS	0

/* Description: This macro is used to select Timed Read or Continuous Read.
 *              1 : To select Continuous Read.
 *              0 : To select Timed Read.
 */
#define ENABLE_CONTINUOUS_READ			1

/* Description: This macro is used to select the baud rate of the host UART.
 *              Default Baud Rate : 115200
 *              Other Baud Rates  : 9600, 19200, 38400, 57600, 230400, 460800 and 921600.
 */
#define CONSOLE_BAUD_RATE				115200

/* Description: This macro is used to set the tag read time when Continuous Read is selected.
 *              1. Read tags in the background and print tags for specified read time (500 ms) in case of Continuous Read.
 *              2. In case of Timed read, read tags duration is constant at 500 ms.
 *
 * Note- Increase "TAG_SEARCH_TIME" to around 5000 for Arduino Nano ESP32.
 * 500ms is too short to actually receive any tag reads on host, 
 * because the ESP32's USB-CDC port takes a second or two to come up after boot.
 */
#define TAG_SEARCH_TIME				500 //ms

/* Global Variables */
const int sysLedPin = 13;
static int ledState = LOW;  // ledState used to set the LED
//Comment below line for Arduino Nano ESP32
static HardwareSerial *console = &Serial;
//Uncomment below line for Arduino Nano ESP32
//static USBCDC *console = &Serial;

TMR_Reader r, *rp;
TMR_ReadListenerBlock rlb;
TMR_ReadExceptionListenerBlock reb;

char string[100];
TMR_String model;
uint32_t count = 0;
bool stopReadCommandSent = false;

TMR_Status parseSingleThreadedResponse(TMR_Reader* rp, uint32_t readTime);
void notify_read_listeners(TMR_Reader *reader, TMR_TagReadData *trd);
void notify_exception_listeners(TMR_Reader *reader, TMR_Status status);
void reset_continuous_reading(struct TMR_Reader *reader, bool dueToError);
void readCallback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);

static void blink(int count, int blinkInterval)
{
  unsigned long blinkTime;
  unsigned long currentTime;

  blinkTime = 0;
  currentTime = millis();

  while (count)
  {
    if (currentTime > blinkTime) {
      // save the last time you blinked the LED
      blinkTime = currentTime + blinkInterval;

      // if the LED is off turn it on and vice-versa:
      if (ledState == LOW)
      {ledState = HIGH;}
      else
      {
        ledState = LOW;
        --count;
      }

      // set the LED with the ledState of the variable:
      digitalWrite(sysLedPin, ledState);
    }

    currentTime = millis();
  }
}

static void checkerr(TMR_Reader *rp, TMR_Status ret, int exitval, const char *msg)
{
  if (TMR_SUCCESS != ret)
  {
    console->print("ERROR ");
    console->print(msg);
    console->print(": 0x");
    console->print(ret, HEX);
    console->print(": ");
    // console->println(TMR_strerror(ret));
    while (1)
    {
      blink(ERROR_BLINK_COUNT, ERROR_BLINK_INTERVAL);
    }
  }
}

static void printComment(char *msg)
{
  console->print("#");
  console->println(msg);
}


/* This function is used to print tag data while tags are read in the background.
 *
 * @param reader  Reader object
 * @param trd     TagReadData object
 * @param cookie  Arbitrary data structure
 */
void readCallback(TMR_Reader *reader, const TMR_TagReadData *trd, void *cookie) {
  char epcStr[128];
  char timeStr[128];

  TMR_bytesToHex(trd->tag.epc, trd->tag.epcByteCount, epcStr);
  console->print("TAGREAD EPC:");
  console->print(epcStr);

  /* Report tag metadata values */
  reportMetaData(*trd);
  console->print("\n");
}


/* This function is used to report any exceptions occurred during continuous read.
 *
 * @param reader  Reader object
 * @param error   Error code
 * @param cookie  Arbitrary data structure
 */
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie) 
{
  console->println("Async read failed.");
}


/* This function is used to print tag metadata values.
 * @param trd  TagReadData object
 */
static void reportMetaData(TMR_TagReadData trd)
{
  if (TMR_TRD_METADATA_FLAG_PROTOCOL & trd.metadataFlags)
  {
    console->print(" PROTOCOL:");
    console->print(trd.tag.protocol);
  }

  if (TMR_TRD_METADATA_FLAG_ANTENNAID & trd.metadataFlags)
  {
    console->print(" ANT:");
    console->print(trd.antenna);
  }

  if (TMR_TRD_METADATA_FLAG_READCOUNT & trd.metadataFlags)
  {
    console->print(" READCOUNT:");
    console->print(trd.readCount);
  }
}


/* This function is used to parse tag response.
 * @param rp        Reader object
 * @param readTime  TAG_SEARCH_TIME
 */
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
    }
    else if (TMR_ERROR_END_OF_READING == ret)
    {break;}
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

static void continuousRead()
{
  TMR_Status ret;

  
  rlb.listener = readCallback;
  rlb.cookie = NULL;

  reb.listener = exceptionCallback;
  reb.cookie = NULL;

  ret = TMR_addReadListener(rp, &rlb);
  checkerr(rp, ret, 1, "adding read listener");

  ret = TMR_addReadExceptionListener(rp, &reb);
  checkerr(rp, ret, 1, "adding exception listener");

  ret = TMR_startReading(rp);
  checkerr(rp, ret, 1, "Start reading");
  
  parseSingleThreadedResponse(rp, TAG_SEARCH_TIME);
}

static void timedRead()
{
  /* Data Object to hold tag results */
  TMR_TagReadData trd;
  char epcStr[128];
  TMR_Status ret;

  ret = TMR_read(rp, TAG_SEARCH_TIME, NULL);
  checkerr(rp, ret, 1, "reading tags");

  while (TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    ret = TMR_getNextTag(rp, &trd);
    checkerr(rp, ret, 1, "fetching tag");

    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);
    console->print("TAGREAD EPC:");
    console->print(epcStr);

    /* Report tag metadata values */
    reportMetaData(trd);
    console->print("\n");
  }
}

static void initializeReader()
{
  TMR_Status ret;
  TMR_ReadPlan plan;
  uint8_t antennaList[] = { 1 };
  uint8_t antennaCount = 1;
  TMR_TagProtocol protocol;

  rp = &r;
  //Use Serial0 for Arduino Nano ESP32
  ret = TMR_create(rp, "tmr:///Serial1");
  checkerr(rp, ret, 1, "creating reader");

  ret = TMR_connect(rp);
  checkerr(rp, ret, 1, "connecting reader");

   if (TMR_ERROR_TIMEOUT == ret)
      {
          uint32_t currentBaudRate;
           console->println("\n!!! TimeOut !!!");
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
     
 
      if (TMR_SUCCESS == ret)
      {
          ret = TMR_connect(rp);
      }    
  
  checkerr(rp, ret, 1, "Connecting reader");

  model.value = string;
  model.max   = sizeof(string);
  TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);
  checkerr(rp, ret, 1, "Getting version model");

  /* Set region to North America */
  if (0 != strcmp("M3e", model.value))
  {
    TMR_Region region = TMR_REGION_NA;
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
    checkerr(rp, ret, 1, "setting region");
  }
    
  /* Set protocol */
  if (0 != strcmp("M3e", model.value))
  {protocol = TMR_TAG_PROTOCOL_GEN2;}
  else
  {protocol =TMR_TAG_PROTOCOL_ISO14443A;}


#if SET_M6E_COMPATIBLE_PARAMS
{
  /* To make this code compatible with M6e family modules,
   * set below configurations.
   */
  #if ENABLE_CONTINUOUS_READ
  {
     {
       /* 1. Disable read filter: To report repeated tag entries of same tag, users must disable read filter for
        *                    continuous read. This filter is enabled by default in the M6e family modules.
        *                    Note that this is a one time configuration while connecting to the module after
        *                    power ON. We do not have to set it in every read cycle.
	    */
        bool readFilter = false;
        ret = TMR_paramSet(rp, TMR_PARAM_TAGREADDATA_ENABLEREADFILTER, &readFilter);
        checkerr(rp, ret, 1, "setting read filter");
     }

     {
       /* 2. Metadata flag: TMR_TRD_METADATA_FLAG_ALL includes all flags (Supported by UHF and HF/LF readers).
        *                   Disable unsupported flags for M6e family as shown below.
        *                   Note that Metadata flag must be set once after connecting to the module using TMR_connect().
        */
        TMR_TRD_MetadataFlag metadata = (uint16_t)(TMR_TRD_METADATA_FLAG_ALL & (~TMR_TRD_METADATA_FLAG_TAGTYPE));
        ret = TMR_paramSet(rp, TMR_PARAM_METADATAFLAG, &metadata);
        checkerr(rp, ret, 1, "Setting Metadata Flags");
     }
  }
  #else
  {
       /* 1. Enable read filter: This step is optional in case of Timed Reads because read filter is enabled by
        *                     default in the M6e family modules.
        *                     But if we observe multiple entries of the same tag in the tag reports, then the
        *                     read filter might have been disabled prevoiusly. So, we must enable the read filter.
        */
        bool readFilter = true;
        ret = TMR_paramSet(rp, TMR_PARAM_TAGREADDATA_ENABLEREADFILTER, &readFilter);
        checkerr(rp, ret, 1, "setting read filter");
  }
  #endif
 }
#endif /* SET_M6E_COMPATIBLE_PARAMS */
 

  /* Initializing the Read Plan */
  TMR_RP_init_simple(&plan, antennaCount, antennaList, protocol, 100);

  /* Commit the Read Plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
  checkerr(rp, ret, 1, "setting read plan");
}

void setup()
{
  /* Set the LED pin on the Arduino as output */
  pinMode(sysLedPin, OUTPUT);

  /* Turn OFF the LED on the Arduino */
  digitalWrite(sysLedPin, 0);

  /* Start the console interface with the selected baudrate */
  console->begin(CONSOLE_BAUD_RATE);

  /* Initialize the RFID Module */
  initializeReader();

  /* Start tag read and report tags */ 
  #if ENABLE_CONTINUOUS_READ
  {
    console->println("\n!!! Start continuous read !!!");
    /* Read tags in the background and print the tags found. */
    continuousRead();
  }
  #else
  {
    console->println("!!! Start timed read !!!");
    /* Read tags for a fixed period of time and print the tags found. */
    timedRead();
  }
  #endif
  
  console->println("!!! Read stopped !!!");

  TMR_destroy(rp);
}

void loop() {
}