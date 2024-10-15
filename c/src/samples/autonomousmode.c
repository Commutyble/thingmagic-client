/**
* Sample program that demonstrates enable/disable AutonomousMode.
* @file autonomousmode.c
*/

#include <tm_reader.h>
#include <serial_reader_imp.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#include <tmr_utils.h>
#ifndef WIN32
#include <unistd.h>
#endif


/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif
 
#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n] [--config option] [--trigger pinNum]\n"\
                         "reader-uri        : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'                 \n"\
                         "[--ant n]         : e.g., '--ant 1'                                                                \n"\
                         "[--config option] : Indicates configuration options of the reader                                  \n"\
                         "                    option: 1 - saveAndRead,                                                       \n"\
                         "                            2 - save,                                                              \n"\
                         "                            3 - stream,                                                           \n"\
                         "                            4 - clear,                                                             \n"\
                         "                    e.g., --config 1 for saving and enabling autonomous read.                      \n"\
                         "[--trigger pinNum]: e.g., --trigger 0 for auto read on boot,                                       \n"\
                         "                          --trigger 1 for read on gpi pin 1.                                       \n"\
                         "[--model option]  : Indicates model of the reader.                                                 \n"\
                         "                    option: 1 - UHF Reader,                                                        \n"\
                         "                            2 - M3E Reader,                                                        \n"\
                         "Example for UHF   : tmr:///com1 --ant 1,2 --config 1 --trigger 0 for autonomous read on boot       \n"\
                         "                    tmr:///com1 --ant 1,2 --config 1 --trigger 1 for gpi trigger read on pin 1     \n"\
                         "                    tmr:///com1 --ant 1,2 --config 2, tmr:///com1 --ant 1,2 --config 3 --model 1   \n"\
                         "Example for HF/LF : tmr:///com1 --config 1 --trigger 0                                             \n"\
                         "                    tmr:///com1 --config 3 --model 2                                               \n");}
void errx(int exitval, const char *fmt, ...)
{
  va_list ap;

  va_start(ap, fmt);
  vfprintf(stderr, fmt, ap);

  exit(exitval);
}

void checkerr(TMR_Reader* rp, TMR_Status ret, int exitval, const char *msg)
{
  if ((TMR_SUCCESS != ret) && (TMR_SUCCESS_STREAMING != ret))
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
    default:
      return "unknown";
  }
}

//Externs.
extern bool isMultiSelectEnabled;
extern bool isStreamEnabled;

//Global Variables.
char configOption[16]  = {0};  // To store the config options i.e. a)saveAndRead, b)save, c)stream, d)clear,
char autoReadType[16]  = {0};  // To store the Autonomous read type i.e. ReadOnBoot or ReadOnGPI.
uint8_t triggerTypeNum =   0;  // To store the tgigger value i.e. 0 to 4.
uint8_t modelID        =   0;  // To store module type i.e 1: UHF , 2: M3e.

//Declarations.
void callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void streamCallback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);
void statsCallback (TMR_Reader *reader, const TMR_Reader_StatsValues* stats, void *cookie);
void configureUHFPersistentSettings(TMR_Reader *reader, TMR_String *model, uint8_t *antennaList, uint8_t antennaCnt,
                                    TMR_ReadPlan *plan, TMR_TagProtocol *protocol, TMR_TagOp *op, TMR_TagFilter *filt);
void configureM3ePersistentSettings(TMR_Reader *rp, uint8_t *antennaList, uint8_t antennaCount, TMR_ReadPlan *plan,
                                    TMR_TagProtocol *protocol);
void extractStreamReadResponses(TMR_Reader *reader, bool keepStreaming);
TMR_Status serial_connect(TMR_Reader *reader);
void parsingSpecificInit(TMR_Reader *reader);

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_SR_UserConfigOp config;
  TMR_ReadPlan plan;
  uint8_t buffer[20];
  TMR_uint8List gpiPort;
  uint8_t *antennaList = NULL;
  uint8_t i;
  uint8_t antennaCount = 0x0;
  uint8_t data[1] = {2};
  TMR_TagProtocol protocol = TMR_TAG_PROTOCOL_NONE;
  bool enableAutoRead = true;
  TMR_TagOp op;
  TMR_TagFilter filt;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
  char str[64];
  TMR_String model;

  model.value = str;
  model.max = 64;
  gpiPort.len = gpiPort.max = 1;
  gpiPort.list = data;
  if (argc < 2)
  {
    usage();
  }

  for (i = 2; i < argc; i++)
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
      i++;
    }
    else
    if(0x00 == strcmp("--config",argv[i]))
    {
      uint8_t option = 0;

      //Extract option.
      i++;
      option = atoi(argv[i]);

      switch(option)
      {
        // Saves the configuration and performs read with the saved configuration.
        case 1:
          strcpy(configOption,"saveAndRead");
          break;
        // Only saves the configuration.
        case 2:
          strcpy(configOption,"save");
          break;
        // Reads with the restored configuration.
        case 3:
          strcpy(configOption,"stream");
          break;
        //Clears the configuration.
        case 4:
          strcpy(configOption,"clear");
          break;
        default:
          fprintf(stdout, "Please select config option between 1 and 5\n");
          usage();
          break;
      }
    }
    else
    if(0x00 == strcmp("--trigger",argv[i]))
    {
      //Extract option.
      i++;
      triggerTypeNum = atoi(argv[i]);

      switch(triggerTypeNum)
      {
        case 0:
          strcpy(autoReadType,"ReadOnBoot");
          break;
        case 1:
        case 2:
        case 3:
        case 4:
          strcpy(autoReadType,"ReadOnGPI");
          break;
        default:
          fprintf(stdout, "Please select trigger option between 0 and 4\n");
          usage();
          break;
      }
    }
    else
    if(0x00 == strcmp("--model",argv[i]))
    {
      uint8_t model = 0;

      //Extract option.
      i++;
      model = atoi(argv[i]);

      switch(model)
      {
        case 1:
        case 2:
          modelID = model;
          break;
        default:
          fprintf(stdout, "Please select model option between 1 and 2\n");
          usage();
          break;
      }
    }
    else
    {
      fprintf(stdout, "Argument %s is not recognized\n", argv[i]);
      usage();
    }
  }
  if (((autoReadType[0] != '\0') && 
       ((0x00 == strcmp("ReadOnBoot", autoReadType)) || (0x00 == strcmp("ReadOnGPI", autoReadType)))) &&
       !(0x00 == strcmp("saveAndRead", configOption)))
   {
     fprintf(stdout, "Please select saveAndRead config option to enable autoReadType\n");
     usage();
   }

  //--model option is supported with "stream" option only.
  if (modelID && (0x00 != strcmp("stream", configOption)))
  {
    fprintf(stdout, "Please select model with config option 3 only\n");
    usage();
  }

  //--model option is mandatory for "stream" option.
  if ((!modelID) && (0x00 == strcmp("stream", configOption)))
  {
    fprintf(stdout, "Please select model for config option 3\n");
    usage();
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

  if(0x00 == strcmp("stream", configOption))
  {
    //Connect to the reader port.
    ret = serial_connect(rp);
    if (TMR_SUCCESS == ret)
    {
      printf("Connection to the module is successfull\n");
    }
    else
    {
      checkerr(rp, ret, 1, "connecting reader");
    }

    //Initialization.
    parsingSpecificInit(rp);

    /* Extract Autonomous read responses */
    extractStreamReadResponses(rp, 1);

    /* remove the transport listener */
#if USE_TRANSPORT_LISTENER
    ret = TMR_removeTransportListener(rp, &tb);
    checkerr(rp, ret, 1, "remove transport listener");
#endif

    TMR_destroy(rp);
    return 0;
  }

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

  TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);
  checkerr(rp, ret, 1, "Getting version model");

  {
    // initialize the read plan
    if (0 == strcmp("M3e", model.value))
    {
      protocol = TMR_TAG_PROTOCOL_ISO14443A;
    }
    else
    {
      protocol = TMR_TAG_PROTOCOL_GEN2;
    }

    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, protocol, 1000);
    checkerr(rp, ret, 1, "initializing the read plan");

    if(0x00 == strcmp("saveAndRead", configOption))
    {
      if (0 == strcmp("M3e", model.value))
      {
        configureM3ePersistentSettings(rp, antennaList, antennaCount, &plan, &protocol);
      }
      else
      {
        configureUHFPersistentSettings(rp, &model, antennaList, antennaCount, &plan, &protocol, &op, &filt);
      }

      /* To disable autonomous read make enableAutonomousRead flag to false and do SAVEWITHRREADPLAN */
      {
        ret = TMR_RP_set_enableAutonomousRead(&plan, enableAutoRead);
        checkerr(rp, ret, 1, "setting autonomous read");
      }

      /* Commit read plan */
      ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
      checkerr(rp, ret, 1, "setting read plan");

      /* The Reader Stats, Currently supporting only Temperature field */
#if 0/*  Change to "if (1)" to enable reader stats */
      {
        TMR_Reader_StatsFlag setFlag = TMR_READER_STATS_FLAG_TEMPERATURE;

        /** request for the statics fields of your interest, before search */
        ret = TMR_paramSet(rp, TMR_PARAM_READER_STATS_ENABLE, &setFlag);
        checkerr(rp, ret, 1, "setting the  fields");
      }
#endif

      /* Init UserConfigOp structure to save read plan configuration */
      ret = TMR_init_UserConfigOp(&config, TMR_USERCONFIG_SAVE_WITH_READPLAN);
      checkerr(rp, ret, 1, "Initializing user configuration: save read plan configuration");

      ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
      checkerr(rp, ret, 1, "setting user configuration: save read plan configuration");
      printf("User config set option:save with read plan configuration\n");

      /* Restore the save read plan configuration */
      /* Init UserConfigOp structure to Restore all saved configuration parameters */
      ret = TMR_init_UserConfigOp(&config, TMR_USERCONFIG_RESTORE);
      checkerr(rp, ret, 1, "Initialization configuration: restore all saved configuration params");

      ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
      checkerr(rp, ret, 1, "setting configuration: restore all saved configuration params");
      printf("User config set option:restore all saved configuration params\n");

      if(enableAutoRead)
      {
        /* Extract Autonomous read responses */
#ifdef TMR_ENABLE_BACKGROUND_READS
        extractStreamReadResponses(rp, 0);

        /* remove the transport listener */
#if USE_TRANSPORT_LISTENER
        ret = TMR_removeTransportListener(rp, &tb);
        checkerr(rp, ret, 1, "remove transport listener");
#endif
#else
        {
          /* code for non thread platform */
          TMR_TagReadData trd;

          TMR_TRD_init(&trd);
          ret = TMR_receiveAutonomousReading(rp, &trd, NULL);
          checkerr(rp, ret, 1, "Autonomous reading");
        }
#endif
      }
    }
    else if(0x00 == strcmp("save", configOption))
    {
      if (0 == strcmp("M3e", model.value))
      {
        configureM3ePersistentSettings(rp, antennaList, antennaCount, &plan, &protocol);
      }
      else
      {
        configureUHFPersistentSettings(rp, &model, antennaList, antennaCount, &plan, &protocol, &op, &filt);
      }
      /* To disable autonomous read make enableAutonomousRead flag to false and do SAVEWITHRREADPLAN */
      {
        ret = TMR_RP_set_enableAutonomousRead(&plan, enableAutoRead);
        checkerr(rp, ret, 1, "setting autonomous read");
      }

      /* Commit read plan */
      ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
      checkerr(rp, ret, 1, "setting read plan");

      /* Init UserConfigOp structure to save read plan configuration */
      ret = TMR_init_UserConfigOp(&config, TMR_USERCONFIG_SAVE_WITH_READPLAN);
      checkerr(rp, ret, 1, "Initializing user configuration: save read plan configuration");

      ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
      checkerr(rp, ret, 1, "setting user configuration: save read plan configuration");
      printf("User config set option:save with read plan configuration\n");
    }
    else if(0x00 == strcmp("clear", configOption))
    {
      //Init UserConfigOp structure to reset/clear all configuration parameter
      TMR_init_UserConfigOp(&config, TMR_USERCONFIG_CLEAR);
      checkerr(rp, ret, 1, "Initializing user configuration: clear all saved configuration");

      ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
      checkerr(rp, ret, 1, "setting user configuration option: clear all configuration parameters");
      printf("User config set option:clear all configuration parameters\n");   
    }
    else
    {
      printf("Please input correct config option\n");
      usage();
    }
  }

  TMR_destroy(rp);
  return 0;
}

uint8_t getAntennaId(uint8_t txRxAntenna)
{
  uint8_t tx = 0;
  uint8_t rx = 0;

  tx = (txRxAntenna >> 4) & 0xF;
  rx = (txRxAntenna >> 0) & 0xF;

  // Due to limited space, Antenna 16 wraps around to 0
  if (0 == tx) { tx = 16; }
  if (0 == rx) { rx = 16; }
  if (tx == rx)
  {
    return tx;
  }

  return txRxAntenna;
}

void
callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];

  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
  printf("%s %s ant: %d readcount: %d\n", protocolName(t->tag.protocol), epcStr, t->antenna, t->readCount);

  if (0 < t->data.len)
  {
    if (0x8000 == t->data.len)
    {
      TMR_Status ret = TMR_SUCCESS;

      ret = TMR_translateErrorCode(GETU16AT(t->data.list, 0));
      checkerr(reader, ret, 0, "Embedded tagOp failed:");
    }
    else
    {
      char dataStr[255];
      uint32_t dataLen;

      //Convert data len from bits to byte.
      dataLen = tm_u8s_per_bits(t->data.len);

      TMR_bytesToHex(t->data.list, dataLen, dataStr);
      printf("  data(%d): %s\n", dataLen, dataStr);
    }
  }
}

void 
exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie)
{
  fprintf(stdout, "Error:%s\n", TMR_strerr(reader, error));
}

void 
statsCallback (TMR_Reader *reader, const TMR_Reader_StatsValues* stats, void *cookie)
{
  /** Each  field should be validated before extracting the value */
  /** Currently supporting only temperature value */
  if (TMR_READER_STATS_FLAG_TEMPERATURE & stats->valid)
  {
    printf("Temperature %d(C)\n", stats->temperature);
  }
}

void configureUHFPersistentSettings(TMR_Reader *rp, TMR_String *model, uint8_t *antennaList, uint8_t antennaCount,
                                    TMR_ReadPlan *plan, TMR_TagProtocol *protocol, TMR_TagOp *op, TMR_TagFilter *filt)
{
  TMR_Status ret = TMR_SUCCESS;
  TMR_uint8List gpiPort;
  uint8_t gpiPortData[1] = {0};

  //Baudrate.
  {
    uint32_t baudrate = 115200;
    ret = TMR_paramSet(rp, TMR_PARAM_BAUDRATE, &baudrate);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting baudrate");
#endif
  }

  //Region.
  {
    TMR_Region region;
    TMR_RegionList regions;
    TMR_Region _regionStore[32];
    regions.list = _regionStore;
    regions.max = sizeof(_regionStore)/sizeof(_regionStore[0]);
    regions.len = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_SUPPORTEDREGIONS, &regions);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "getting supported regions");

    if (regions.len < 1)
    {
      checkerr(rp, TMR_ERROR_INVALID_REGION, 1, "Reader doesn't support any regions");
    }
#endif
    region = regions.list[0];
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL    
    checkerr(rp, ret, 1, "setting region");  
#endif 
  }

  //Protocol.
  {
    ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_PROTOCOL, protocol);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting protocol");
#endif
  }

#ifdef TMR_ENABLE_UHF
  //Gen2 setting
  {
    TMR_GEN2_LinkFrequency linkFreq = TMR_GEN2_LINKFREQUENCY_250KHZ;
    TMR_GEN2_Tari tari              =  TMR_GEN2_TARI_25US;
    TMR_GEN2_Target target          = TMR_GEN2_TARGET_A;
    TMR_GEN2_TagEncoding encoding   = TMR_GEN2_MILLER_M_4;
    TMR_GEN2_Session session        = TMR_GEN2_SESSION_S0;
    TMR_GEN2_Q q;
    q.type                          = TMR_SR_GEN2_Q_DYNAMIC;

    if(rp->u.serialReader.versionInfo.hardware[0] != TMR_SR_MODEL_M7E)
    {
      //Link frequency: 250KHZ
      linkFreq = TMR_GEN2_LINKFREQUENCY_250KHZ;
      ret = TMR_paramSet(rp, TMR_PARAM_GEN2_BLF, &linkFreq);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting blf");
#endif

      //Tari: 25US
      ret = TMR_paramSet(rp, TMR_PARAM_GEN2_TARI, &tari);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting tari");
#endif

      //Encoding: M4
      ret = TMR_paramSet(rp, TMR_PARAM_GEN2_TAGENCODING, &encoding);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting tag encoding");
#endif
    }

    //Target: A
    ret = TMR_paramSet(rp, TMR_PARAM_GEN2_TARGET, &target);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting target");
#endif

    //Session: S0
    ret = TMR_paramSet(rp, TMR_PARAM_GEN2_SESSION, &session);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting session");
#endif

    //Q: Dynamic
    ret = TMR_paramSet(rp, TMR_PARAM_GEN2_Q, &q);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting q");
#endif
  }
#endif /* TMR_ENABLE_UHF */

  //RF Power settings
  {
    uint32_t readPower  = 2000;
    uint32_t writePower = 2000;

    //Read Power: 2000
    ret = TMR_paramSet(rp, TMR_PARAM_RADIO_READPOWER, &readPower);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting read power");
#endif

    //Write Power: 2000
    ret = TMR_paramSet(rp, TMR_PARAM_RADIO_WRITEPOWER, &writePower);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting write power");
#endif
  }

#ifdef TMR_ENABLE_UHF
  // Hop Table
  {
    TMR_uint32List hopTable;
    uint32_t data[255];

    //Initialize.
    hopTable.list = data;
    hopTable.max = sizeof(data) / sizeof(data[0]);
    hopTable.len = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_HOPTABLE, &hopTable);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "getting hop table");
#endif

    ret = TMR_paramSet(rp, TMR_PARAM_REGION_HOPTABLE, &hopTable);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting hop table");
#endif
  }

  // Hop Time
  {
    uint32_t hopTime = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_HOPTIME, &hopTime);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "getting hop time");
#endif

    ret = TMR_paramSet(rp, TMR_PARAM_REGION_HOPTIME, &hopTime);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting hop time");
#endif
  }

  //For Open region, dwell time, minimum frequency, quantization step can also be configured persistently
  {
    TMR_Region region = TMR_REGION_NONE;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "getting region");
#endif

    if(TMR_REGION_OPEN == region)
    {
      bool dwellTimeEnable = true;
      uint32_t quantStep    = 100000;
      uint32_t dwellTime    = 250;
      uint32_t minFreq      = 865700;

      //Set dwell time enable before stting dwell time
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_DWELL_TIME_ENABLE, &dwellTimeEnable);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting dwell time enable");
#endif

      //set quantization step
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_QUANTIZATION_STEP, &quantStep);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting quantization step");
#endif

      //set dwell time
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_DWELL_TIME, &dwellTime);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting dwell time");
#endif

      //set minimum frequency
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_MINIMUM_FREQUENCY, &minFreq);
#ifndef BARE_METAL
     checkerr(rp, ret, 1, "setting minimum frequency");
#endif
    }
  }
#endif /* TMR_ENABLE_UHF */
  // Filter
  /* (Optional) Tag Filter
   * Not required to read TID, but useful for limiting target tags */

#if (0)  /* Change to "if (1)" to enable filter */
  {
    TMR_TagData td;
    td.protocol = TMR_TAG_PROTOCOL_GEN2;
    {
      int i = 0;
      td.epc[i++] = 0x01;
      td.epc[i++] = 0x23;
      td.epcByteCount = i;
    }
    ret = TMR_TF_init_tag(filt, &td);
    checkerr(rp, ret, 1, "creating tag filter");

    ret = TMR_RP_set_filter(plan, filt);
    checkerr(rp, ret, 1, "setting tag filter");
  }
#endif

  /* Embedded Tagop */
#if 0 /* Change to "if (1)" to enable Embedded TagOp */
  {
    uint8_t readLen;

    /* Specify the read length for readData */
    if ((0 == strcmp("M6e", model->value)) || (0 == strcmp("M6e PRC", model->value))
    || (0 == strcmp("M6e Micro", model->value)) || (0 == strcmp("Mercury6", model->value)) 
    || (0 == strcmp("Astra-EX", model->value)))
    {
      /**
       * Specifying the readLength = 0 will retutrn full TID for any
       * tag read in case of M6e and M6 reader.
       **/
      readLen = 0;
    }
    else
    {
      /**
       * In other case readLen is minimum.i.e 2 words
       **/
      readLen = 2;
    }

    ret = TMR_TagOp_init_GEN2_ReadData(op, TMR_GEN2_BANK_EPC, 0, readLen);
    checkerr(rp, ret, 1, "creating tagop: GEN2 read data");

    ret = TMR_RP_set_tagop(plan, op);
    checkerr(rp, ret, 1, "setting tagop");
  }
#endif

  {
    if((autoReadType[0] != '\0' ) && (0x00 == strcmp("ReadOnGPI", autoReadType)))
    {
      //Gpi trigger option not there for M6e Micro USB
      if (0 != strcmp("M6e Micro USB", model->value))
      {
        TMR_GPITriggerRead triggerRead;
        gpiPort.len = gpiPort.max = 1;
        gpiPort.list = gpiPortData;

        ret = TMR_GPITR_init_enable (&triggerRead, true);
#ifndef BARE_METAL
        checkerr(rp, ret, 1, "Initializing the trigger read");
#endif
        ret = TMR_RP_set_enableTriggerRead(plan, &triggerRead);
#ifndef BARE_METAL
        checkerr(rp, ret, 1, "setting trigger read");
#endif
      }
    }
  }

  // Set Trigger.
  {
    if((autoReadType[0] != '\0' ) && (0x00 == strcmp("ReadOnGPI", autoReadType)))
    {
      if (0 != strcmp("M6e Micro USB", model->value))
      {
        /* Specify the GPI pin to be used for trigger read */
        gpiPortData[0] = triggerTypeNum;
        ret = TMR_paramSet(rp, TMR_PARAM_TRIGGER_READ_GPI, &gpiPort);
#ifndef BARE_METAL
        checkerr(rp, ret, 1, "setting GPI port");
#endif
      }
    }
  }
}

void configureM3ePersistentSettings(TMR_Reader *rp, uint8_t *antennaList, uint8_t antennaCount, TMR_ReadPlan *plan, 
                                    TMR_TagProtocol *protocol)
{
  TMR_Status ret = TMR_SUCCESS;
  TMR_uint8List gpiPort;
  uint8_t gpiPortData[1] = {0};

  //Baudrate.
  {
    uint32_t baudrate = 115200;
    ret = TMR_paramSet(rp, TMR_PARAM_BAUDRATE, &baudrate);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting baudrate");
#endif
  }

  //Protocol.
  {
    ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_PROTOCOL, protocol);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting protocol");
#endif
  }

  //Enable read filter
  {
    bool enableReadFilter = true;
    ret = TMR_paramSet(rp, TMR_PARAM_TAGREADDATA_ENABLEREADFILTER, &enableReadFilter);
#ifndef BARE_METAL
    checkerr(rp, ret, 1, "setting read filter");
#endif
  }

  {
    if((autoReadType[0] != '\0' ) && (0x00 == strcmp("ReadOnGPI", autoReadType)))
    {
      TMR_GPITriggerRead triggerRead;
      gpiPort.len = gpiPort.max = 1;
      gpiPort.list = gpiPortData;

      ret = TMR_GPITR_init_enable (&triggerRead, true);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "Initializing the trigger read");
#endif

      ret = TMR_RP_set_enableTriggerRead(plan, &triggerRead);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting trigger read");
#endif
    }
  }

  // Set Trigger.
  {
    if((autoReadType[0] != '\0' ) && (0x00 == strcmp("ReadOnGPI", autoReadType)))
    {
      /* Specify the GPI pin to be used for trigger read */
      gpiPortData[0] = triggerTypeNum;
      ret = TMR_paramSet(rp, TMR_PARAM_TRIGGER_READ_GPI, &gpiPort);
#ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting GPI port");
#endif
    }
  }
}

void parsingSpecificInit(TMR_Reader *reader)
{
  //Enable stream flag.
  isStreamEnabled = true;

  //Check for M3e module.
  if(modelID == 2)
  {
    reader->u.serialReader.versionInfo.hardware[0] = TMR_SR_MODEL_M3E;
  }

  //Enable stats.
  reader->statsFlag = TMR_READER_STATS_FLAG_TEMPERATURE;
}

TMR_Status serial_connect(TMR_Reader *reader)
{
  TMR_Status ret = TMR_SUCCESS;
  uint32_t probeBaudRates[] = {115200, 9600, 921600, 19200, 38400, 57600,230400, 460800};
  TMR_SR_SerialTransport *transport;
  uint8_t msg[255];

  transport = &reader->u.serialReader.transport;

  /*open the serial port */
  ret = transport->open(transport);
  if(TMR_SUCCESS != ret)
  {
    /* Not able to open the serial port, exit early */
    return ret;
  }

  /* Find the baudrate */
  printf("Waiting for streaming...\n");
  do
  {
    uint8_t i;
    for (i = 0; i < 8; i++)
    {
      //Set baudrate.
      ret = transport->setBaudRate(transport, probeBaudRates[i]);

      //Flush the data.
      transport->flush(transport);

      //Receive message and check for tag reads.
      ret = TMR_SR_receiveMessage(reader, msg, TMR_SR_OPCODE_READ_TAG_ID_MULTIPLE, 5000);
      if (TMR_SUCCESS == ret)
      {
#ifdef TMR_ENABLE_UHF
        if(msg[5] == 0x88)
        {
          isMultiSelectEnabled = true;
        }
#endif /* TMR_ENABLE_UHF */
        reader->connected = true;
        break;
      }
      else
      {
        printf("Failed to connect with %d baudRate\n", probeBaudRates[i]);
      }
    }
  }while(TMR_SUCCESS != ret);

  return ret;
}

void
streamCallback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];
  uint8_t antennaID = 0;

  //Retrieve Antenna ID.
  antennaID = getAntennaId(t->antenna);

  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
  printf("%s %s ant: %d readcount: %d\n", protocolName(t->tag.protocol), epcStr, antennaID, t->readCount);

  if (0 < t->data.len)
  {
    if (0x8000 == t->data.len)
    {
      TMR_Status ret = TMR_SUCCESS;
      ret = TMR_translateErrorCode(GETU16AT(t->data.list, 0));
      checkerr(reader, ret, 0, "Embedded tagOp failed:");
    }
    else
    {
      char dataStr[255];
      uint32_t dataLen;

      //Convert data len from bits to byte.
      dataLen = tm_u8s_per_bits(t->data.len);

      TMR_bytesToHex(t->data.list, dataLen, dataStr);
      printf("  data(%d): %s\n", dataLen, dataStr);
    }
  }
}

void 
extractStreamReadResponses(TMR_Reader *reader, bool keepStreaming)
{
  TMR_Status ret = TMR_SUCCESS;

#ifdef TMR_ENABLE_BACKGROUND_READS
  {
    TMR_ReadListenerBlock rlb;
    TMR_ReadExceptionListenerBlock reb;
    TMR_StatsListenerBlock slb;

    if(keepStreaming)
    {
      rlb.listener = streamCallback;
    }
    else
    {
      rlb.listener = callback;
    }
    rlb.cookie = NULL;

    reb.listener = exceptionCallback;
    reb.cookie = NULL;

    slb.listener = statsCallback;
    slb.cookie = NULL;

    ret = TMR_addReadListener(reader, &rlb);
    checkerr(reader, ret, 1, "adding read listener");

    ret = TMR_addReadExceptionListener(reader, &reb);
    checkerr(reader, ret, 1, "adding exception listener");

    ret = TMR_addStatsListener(reader, &slb);
    checkerr(reader, ret, 1, "adding the stats listener");

    ret = TMR_receiveAutonomousReading(reader, NULL, NULL);
    checkerr(reader, ret, 1, "Autonomous reading");

    do
    {
#ifndef WIN32
      sleep(5);
#else
      Sleep(5000);
#endif
    }while(keepStreaming);

    /* Remove the read listener to stop receiving the tags */
    ret = TMR_removeReadListener(reader, &rlb);
    checkerr(reader, ret, 1, "remove read listener");

    /* Remove the read exception listener */
    ret = TMR_removeReadExceptionListener (reader, &reb);
    checkerr(reader, ret, 1, "remove exception listener");

    /* Remove the stats listener */
    ret = TMR_removeStatsListener(reader, &slb);
    checkerr(reader, ret, 1, "remove stats listener");
  }
#endif
}
