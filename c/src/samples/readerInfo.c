/**
 * Sample program that display reader parameters
 * @file readerInfo.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>

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
        checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't support any regions");
      }
 
      region = regions.list[0];
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);    
      checkerr(rp, ret, 1, "setting region");
	}
  }

  {
    uint16_t productGroupID, productID;
    TMR_String str;
    char string[100];
    str.value = string;
    str.max = 50;

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_HARDWARE, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/hardware: %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/hardware not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting version hardware",
            TMR_strerr(rp, ret));
      }
    }
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
    str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_SERIAL, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/serial: %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/serial not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting version serial",
            TMR_strerr(rp, ret));
      }
    }
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
    str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/model:  %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/model not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting version model",
            TMR_strerr(rp, ret));
      }
    }
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
     str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_SOFTWARE, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/software: %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/software not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting version software",
            TMR_strerr(rp, ret));
      }
    }
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
     str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_URI, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/uri:  %s\n",str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/uri:  Unsupported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting reader URI",
            TMR_strerr(rp, ret));
      }
    }
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
     str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_PRODUCT_ID, &productID);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/productID: %d\n", productID);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/productID not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting product id",
            TMR_strerr(rp, ret));
      }
    }

    ret = TMR_paramGet(rp, TMR_PARAM_PRODUCT_GROUP_ID, &productGroupID);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/productGroupID: %d\n", productGroupID);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/productGroupID not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting product group id",
            TMR_strerr(rp, ret));
      }
    }

    ret = TMR_paramGet(rp, TMR_PARAM_PRODUCT_GROUP, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/version/productGroup: %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/version/productGroup not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting product group",
            TMR_strerr(rp, ret));
      }
    }
#ifdef TMR_ENABLE_UHF
    /**
     * for failure case API is modifying the str.value to some constant string,
     * to fix that, restoring the str.value variable
     **/ 
     str.value=string;

    ret = TMR_paramGet(rp, TMR_PARAM_READER_DESCRIPTION, &str);
    if (TMR_SUCCESS == ret)
    {
      printf("/reader/description:  %s\n", str.value);
    }
    else
    {
      if (TMR_ERROR_NOT_FOUND == ret)
      {
        printf("/reader/description not supported\n");
      }
      else
      {
        printf("Error %s: %s\n", "getting reader description",
            TMR_strerr(rp, ret));
      }
    }
#endif /* TMR_ENABLE_UHF */
  }

  TMR_destroy(rp);
  return 0;
}
