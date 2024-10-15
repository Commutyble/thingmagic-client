/**
 * Sample program that reads tags for a fixed period of time (500ms)
 * and prints the tags found.
 * @file read.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#include <tmr_utils.h>

#ifdef BARE_METAL
  #define printf(...) {}
#endif

#ifndef BARE_METAL
/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define PRINT_TAG_METADATA 0
#define numberof(x) (sizeof((x))/sizeof((x)[0]))

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n] [--pow read_power]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "[--pow read_power] : e.g, '--pow 2300'\n"\
                         "Example for UHF modules: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' or 'tmr:///com4 --ant 1,2 --pow 2300'\n"\
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

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_ReadPlan plan;
  TMR_Region region;
#define READPOWER_NULL (-12345)
  int readpower = READPOWER_NULL;
#ifndef BARE_METAL
  uint8_t i;
#endif /* BARE_METAL*/
  uint8_t buffer[20];
  uint8_t *antennaList = NULL;
  uint8_t antennaCount = 0x0;
  TMR_TRD_MetadataFlag metadata = TMR_TRD_METADATA_FLAG_ALL;
  char string[100];
  TMR_String model;

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif /* USE_TRANSPORT_LISTENER */
  rp = &r;

#ifndef BARE_METAL
  if (argc < 2)
  {
    fprintf(stdout, "Not enough arguments.  Please provide reader URL.\n");
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
    else if (0 == strcmp("--pow", argv[i]))
    {
      long retval;
      char *startptr;
      char *endptr;
      startptr = argv[i+1];
      retval = strtol(startptr, &endptr, 0);
      if (endptr != startptr)
      {
        readpower = retval;
        fprintf(stdout, "Requested read power: %d cdBm\n", readpower);
      }
      else
      {
        fprintf(stdout, "Can't parse read power: %s\n", argv[i+1]);
      }
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
        checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't support any regions");
      }

      region = regions.list[0];
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
      checkerr(rp, ret, 1, "setting region");
    }

    if (READPOWER_NULL != readpower)
    {
      int value;

      ret = TMR_paramGet(rp, TMR_PARAM_RADIO_READPOWER, &value);
      checkerr(rp, ret, 1, "getting read power");
      printf("Old read power = %d dBm\n", value);

      value = readpower;
      ret = TMR_paramSet(rp, TMR_PARAM_RADIO_READPOWER, &value);
      checkerr(rp, ret, 1, "setting read power");
    }

    {
      int value;
      ret = TMR_paramGet(rp, TMR_PARAM_RADIO_READPOWER, &value);
      checkerr(rp, ret, 1, "getting read power");
      printf("Read power = %d dBm\n", value);
    }
  }

#ifdef TMR_ENABLE_LLRP_READER
  if (0 != strcmp("Mercury6", model.value))
#endif /* TMR_ENABLE_LLRP_READER */
  {
	// Set the metadata flags. Protocol is mandatory metadata flag and reader don't allow to disable the same
	// metadata = TMR_TRD_METADATA_FLAG_ANTENNAID | TMR_TRD_METADATA_FLAG_FREQUENCY | TMR_TRD_METADATA_FLAG_PROTOCOL;
	ret = TMR_paramSet(rp, TMR_PARAM_METADATAFLAG, &metadata);
	checkerr(rp, ret, 1, "Setting Metadata Flags");
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
  checkerr(rp, ret, 1, "initializing the  read plan");

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
  checkerr(rp, ret, 1, "setting read plan");

  ret = TMR_read(rp, 500, NULL);
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
    char idStr[128];
#ifndef BARE_METAL
    char timeStr[128];
#endif /* BARE_METAL */

    ret = TMR_getNextTag(rp, &trd); 
    checkerr(rp, ret, 1, "fetching tag");

    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, idStr);

#ifndef BARE_METAL
  TMR_getTimeStamp(rp, &trd, timeStr);
  printf("Tag ID: %s ", idStr);

// Enable PRINT_TAG_METADATA Flags to print Metadata value
#if PRINT_TAG_METADATA
{
  uint16_t j = 0;

  printf("\n");
  for (j=0; (1<<j) <= TMR_TRD_METADATA_FLAG_MAX; j++)
  {
    if ((TMR_TRD_MetadataFlag)trd.metadataFlags & (1<<j))
    {
      switch ((TMR_TRD_MetadataFlag)trd.metadataFlags & (1<<j))
      {
        case TMR_TRD_METADATA_FLAG_READCOUNT:
          printf("Read Count: %d\n", trd.readCount);
          break;
        case TMR_TRD_METADATA_FLAG_ANTENNAID:
          printf("Antenna ID: %d\n", trd.antenna);
          break;
        case TMR_TRD_METADATA_FLAG_TIMESTAMP:
          printf("Timestamp: %s\n", timeStr);
          break;
        case TMR_TRD_METADATA_FLAG_PROTOCOL:
          printf("Protocol: %d\n", trd.tag.protocol);
          break;
#ifdef TMR_ENABLE_UHF
        case TMR_TRD_METADATA_FLAG_RSSI:
          printf("RSSI: %d\n", trd.rssi);
          break;
        case TMR_TRD_METADATA_FLAG_FREQUENCY:
          printf("Frequency: %d\n", trd.frequency);
          break;
        case TMR_TRD_METADATA_FLAG_PHASE:
          printf("Phase: %d\n", trd.phase);
          break;
#endif /* TMR_ENABLE_UHF */
        case TMR_TRD_METADATA_FLAG_DATA:
        {
          //TODO : Initialize Read Data
          if (0 < trd.data.len)
          {
            if (0x8000 == trd.data.len)
            {
              ret = TMR_translateErrorCode(GETU16AT(trd.data.list, 0));
              checkerr(rp, ret, 0, "Embedded tagOp failed:");
            }
            else
            {
              char dataStr[255];
              uint8_t dataLen = (trd.data.len / 8);

              TMR_bytesToHex(trd.data.list, dataLen, dataStr);
              printf("Data(%d): %s\n", dataLen, dataStr);
            }
          }
        }
        break;
#ifdef TMR_ENABLE_UHF
        case TMR_TRD_METADATA_FLAG_GPIO_STATUS:
        {
          if (rp->readerType == TMR_READER_TYPE_SERIAL)
          {
            printf("GPI status:\n");
            for (i = 0 ; i < trd.gpioCount ; i++)
            {
              printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].bGPIStsTagRdMeta ? "High" : "Low");
            }
            printf("GPO status:\n");
            for (i = 0 ; i < trd.gpioCount ; i++)
            {
              printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
            }
          }
          else
          {
            printf("GPI status:\n");
            for (i = 0 ; i < trd.gpioCount/2 ; i++)
            {
              printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
            }
            printf("GPO status:\n");
            for (i = trd.gpioCount/2 ; i < trd.gpioCount ; i++)
            {
              printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
            }
          }
        }
        break;
        if (TMR_TAG_PROTOCOL_GEN2 == trd.tag.protocol)
        {
          case TMR_TRD_METADATA_FLAG_GEN2_Q:
            printf("Gen2Q: %d\n", trd.u.gen2.q.u.staticQ.initialQ);
            break;
          case TMR_TRD_METADATA_FLAG_GEN2_LF:
          {
            printf("Gen2Linkfrequency: ");
            switch(trd.u.gen2.lf)
            {
              case TMR_GEN2_LINKFREQUENCY_250KHZ:
                printf("250(khz)\n");
                break;
              case TMR_GEN2_LINKFREQUENCY_320KHZ:
                printf("320(khz)\n");
                break;
              case TMR_GEN2_LINKFREQUENCY_640KHZ:
                printf("640(khz)\n"); 
                break;
              default:
                printf("Unknown value(%d)\n",trd.u.gen2.lf);
                break;
            }
            break;
          }
          case TMR_TRD_METADATA_FLAG_GEN2_TARGET:
          {
            printf("Gen2Target: ");
            switch(trd.u.gen2.target)
            {
              case TMR_GEN2_TARGET_A:
                printf("A\n");
                break;
              case TMR_GEN2_TARGET_B:
                printf("B\n");
                break;
              default:
                printf("Unknown Value(%d)\n",trd.u.gen2.target);
                break;
            }
            break;
          }
        }
#endif /* TMR_ENABLE_UHF */
#ifdef TMR_ENABLE_HF_LF
        case TMR_TRD_METADATA_FLAG_TAGTYPE:
        {
          printf("TagType: 0x%08lx\n", trd.tagType);
          break;
        }
#endif /* TMR_ENABLE_HF_LF */
        default:
          break;
        }
      }
    }
  }
#endif
  printf ("\n");
#endif
  }

  TMR_destroy(rp);
  return 0;
}
