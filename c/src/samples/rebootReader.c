/**
 * Sample program that power cycles the reader
 * and connects back once the reader is power cycled successfully.
 * @file rebootReader.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#ifndef WIN32
#include <unistd.h>
#endif

#if WIN32
#define snprintf sprintf_s
#endif 

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "Example: 'tmr:///com4'\n");}

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

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  char string[100];
  TMR_String model;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
 
  if (argc < 2)
  {
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
  }

  {
    uint16_t retryCount = 1;
    /**
     * Power cycle the reader. Calling TMR_reboot() will do that.
     **/
    ret = TMR_reboot(rp);
    checkerr(rp, ret, 1, "power cycling reader");

    /**
     * power cycle will take some time to complete.
     * 1. Fixed reader  : approximately 90sec.
     * 2. Serial reader : approximately 250ms.
     *
     * till then try to reconnect the reader.
     **/
    printf("The reader is being rebooted. Once it has finished, it will reconnect ....\n");

    //reconnect to the reader
    TMR_destroy(rp);

    do
    {
      printf("Trying to reconnect.... Attempt:%d\n", retryCount);
      TMR_create(rp, argv[1]);

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
      if(TMR_SUCCESS == ret)
      {
        break;
      }
      else if((TMR_ERROR_TIMEOUT == ret) && 
         (TMR_READER_TYPE_SERIAL == rp->readerType))
      {
        uint32_t currentBaudRate;

        /* Probe on all supported baudrates 
         * if failed to connect on default baudrate.
         */
        ret = TMR_SR_cmdProbeBaudRate(rp, &currentBaudRate);
        checkerr(rp, ret, 1, "Probe the baudrate");

        /* Set the current baudrate, so that 
         * next TMR_Connect() call can use this baudrate to connect.
         */
        ret = TMR_paramSet(rp, TMR_PARAM_BAUDRATE, &currentBaudRate);
        checkerr(rp, ret, 1, "Setting baudrate"); 

        /* Connect using current baudrate */
        ret = TMR_connect(rp);
        if(ret == TMR_SUCCESS)
        {
          break;
        }
      }

      retryCount++;
    }while(true);

    printf("Reader is reconnected successfully\n");
  }

  TMR_destroy(rp);
  return 0;
}
