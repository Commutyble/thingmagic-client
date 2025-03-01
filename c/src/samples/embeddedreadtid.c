/**
 * Sample program that performs embedded tag operation - read TID memory.
 * @file embeddedreadtid.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#include <tmr_utils.h>

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "Example: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' \n");}

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

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
#ifdef TMR_ENABLE_UHF
  TMR_Region region;
#endif /* TMR_ENABLE_UHF */
  uint8_t *antennaList = NULL;
  uint8_t buffer[20];
  uint8_t i;
  uint8_t antennaCount = 0x0;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
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

#ifdef TMR_ENABLE_UHF
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

  /* Read Plan */
  {
    TMR_ReadPlan plan;
    TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);

    /* (Optional) Tag Filter
     * Not required to read TID, but useful for limiting target tags */
    if (0)  /* Change to "if (1)" to enable filter */
    {
      TMR_TagData td;
      static TMR_TagFilter filt;
      td.protocol = TMR_TAG_PROTOCOL_GEN2;
      {
        int i = 0;
        td.epc[i++] = 0x01;
        td.epc[i++] = 0x23;
        td.epcByteCount = i;
      }
      ret = TMR_TF_init_tag(&filt, &td);
      checkerr(rp, ret, 1, "creating tag filter");
      ret = TMR_RP_set_filter(&plan, &filt);
      checkerr(rp, ret, 1, "setting tag filter");
    }

    /* Embedded Tagop */
    {
      /**
       * Checking the software version of the sargas.
       * The antenna detection is supported on sargas from software version of 5.3.x.x.
       * If the Sargas software version is 5.1.x.x then antenna detection is not supported.
       * User has to pass the antenna as arguments.
       */
      static TMR_TagOp op;
      uint8_t readLen;
      TMR_String model;
      char str[64];

      model.value = str;
      model.max = 64;
      TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);

      if ((0 == strcmp("M6e", model.value)) || (0 == strcmp("M6e PRC", model.value))
          || (0 == strcmp("M6e Micro", model.value)) || (0 == strcmp("Mercury6", model.value)) 
          || (0 == strcmp("Astra-EX", model.value)) || (0 == strcmp("M6e JIC", model.value)) 
          || (0 == strcmp("Sargas", model.value)) || (0 == strcmp("Izar", model.value)))
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

      ret = TMR_TagOp_init_GEN2_ReadData(&op, TMR_GEN2_BANK_TID, 0, readLen);
      checkerr(rp, ret, 1, "creating tagop: GEN2 read data");
      ret = TMR_RP_set_tagop(&plan, &op);
      checkerr(rp, ret, 1, "setting tagop");
    }

    /* Commit read plan */
    ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
    checkerr(rp, ret, 1, "setting read plan");
  }

  ret = TMR_read(rp, 500, NULL);
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
    uint8_t dataBuf[255];
    char epcStr[128];

    ret = TMR_TRD_init_data(&trd, sizeof(dataBuf)/sizeof(uint8_t), dataBuf);
    checkerr(rp, ret, 1, "creating tag read data");

    ret = TMR_getNextTag(rp, &trd);
    checkerr(rp, ret, 1, "fetching tag");

    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);
    printf("%s\n", epcStr);
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
        printf("  data(%d): %s\n", dataLen, dataStr);
      }
    }
  }
#endif /* TMR_ENABLE_UHF */

  TMR_destroy(rp);
  return 0;
}
