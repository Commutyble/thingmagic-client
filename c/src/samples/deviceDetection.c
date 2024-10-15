/**
 * Sample program that detects the serial devices connected
 * and prints the information of the readers found.
 * @file deviceDetection.c
 */
#define UNICODE
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#ifdef WIN32
#include <windows.h>
#else
#include <sys/stat.h>
#include <sys/fcntl.h>
#include <unistd.h>
#endif

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

int DiscoverDevices();
int ReaderInfo(int);

unsigned int iy = 0;

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

int main()
{

#ifndef WIN32
  //Linux part
  char* devPrefixes[] = {"/dev/ttyACM", "/dev/ttyUSB", NULL};
  char buffer[64];
  int fd;
  struct stat devStat;
  unsigned int ix = 0;

   for(iy = 0; devPrefixes[iy] != NULL; iy++)
  {
    for(ix = 0; ix < 256; ix++)
    {
      snprintf(buffer, 64, "%s%d", devPrefixes[iy], ix);
      if(stat(buffer, &devStat) == -1)
      {
        continue;
      }

      if((fd = open(buffer, O_RDWR | O_NOCTTY)) == -1)
      {
        continue;
      }
      else
      {
        close(fd);
        ReaderInfo(ix);
      }
    }
  }
#else
  //Windows Part
  TCHAR portName[12];
  int i;
  HANDLE hSerial;

  memset(portName, 0, sizeof(TCHAR) * 12);

  for(i = 1; i <= 257; i++)
  {
    _snwprintf(portName, 12, TEXT("\\\\.\\COM%d"), i);

    hSerial = CreateFile(portName, GENERIC_READ | GENERIC_WRITE, 0, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
    if(hSerial == INVALID_HANDLE_VALUE)
    {
      continue;
    }
    else
    {
      CloseHandle(hSerial);
      ReaderInfo(i);
    }
  }
#endif

  return 0;
}

int ReaderInfo(int portnumber)
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  char arr[4];
  char portname[64];
  uint32_t Timeout = 100;
  char string[100];
  TMR_String model;

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif

#ifndef WIN32
  if(iy == 0)
  {
    strcpy(portname, "tmr:///dev/ttyACM");
  }
  else
  {
    strcpy(portname, "tmr:///dev/ttyUSB");
  }
#else
  strcpy(portname, "tmr:///com");
#endif

  rp = &r;

  sprintf(arr, "%d", portnumber);
#ifndef WIN32
  strcpy(portname + 17, arr);
#else
  strcpy(portname + 10, arr);
#endif

  //create reader
  ret = TMR_create(rp, portname);
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

  ret = TMR_paramSet(rp, TMR_PARAM_COMMANDTIMEOUT, &Timeout);
  checkerr(rp, ret, 1, "Setting Command timeout");
  ret = TMR_paramSet(rp, TMR_PARAM_TRANSPORTTIMEOUT, &Timeout);
  checkerr(rp, ret, 1, "Setting Transport timeout");

  //connect reader
  ret = TMR_connect(rp);

  /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
   * The connection may fail if the module is configured to a different baud rate. If
   * that is the case, the MercuryAPI tries connecting to the module with other supported
   * baud rates until the connection is successful using baud rate probing mechanism.
   */
  if (TMR_SUCCESS != ret)
  {
    if((ret == TMR_ERROR_TIMEOUT) && 
       (TMR_READER_TYPE_SERIAL == rp->readerType))
    {
      uint32_t currentBaudRate;

      /* Start probing mechanism. */
      ret = TMR_SR_cmdProbeBaudRate(rp, &currentBaudRate);
      if (TMR_SUCCESS != ret)
      {
        //there maybe other readers connected hence skipping and moving to next readers 
        return 0;
      }

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
      //there maybe other readers connected hence skipping and moving to next readers 
      return 0;
    }
  }

  model.value = string;
  model.max   = sizeof(string);
  TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);
  checkerr(rp, ret, 1, "Getting version model");

  if (0 != strcmp("M3e", model.value))
  {
    TMR_Region region;
    region = TMR_REGION_NONE;
    ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
    checkerr(rp, ret, 1, "getting region");

    if (TMR_REGION_NONE == region)
    {
      TMR_RegionList regions;
      TMR_Region _regionStore[32];
      regions.list = _regionStore;
      regions.max = sizeof(_regionStore) / sizeof(_regionStore[0]);
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
    str.value = string;

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
    str.value = string;

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
     str.value = string;

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
     str.value = string;

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
     str.value = string;

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
  }

  TMR_destroy(rp);

  return 0;
}
