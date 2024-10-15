/**
 * Sample program to load firmware
 * @file firmwareload.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri firmwarefilename\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/'\n"\
                         "Example: 'tmr:///com4' firmware file/path to load\n"\
                         "Usage: %s readeruri firmwarefilename\n", argv[0]);}

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
  char *filename = NULL;
  FILE* f = NULL;
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

  if (argc < 3)
  {
    errx(2, "Please provide firmware filename\n"
            "Usage: %s readeruri firmwarefilename\n", argv[0]);
  }
  filename = argv[2];
  
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
  switch (ret)
  {
  case TMR_ERROR_BL_INVALID_IMAGE_CRC:
    fprintf(stderr, "Error: Current image failed CRC check. Proceeding to load firmware, anyway.\n");
    break;
  case TMR_ERROR_BL_INVALID_APP_END_ADDR:
    fprintf(stderr, "Error: The last word of the firmware image stored in the reader's"\
       "flash ROM does not have the correct address value. Proceeding to load firmware, anyway.\n");
    break;
  case TMR_ERROR_TIMEOUT:
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
      if(TMR_ERROR_TIMEOUT == ret)
      {
        fprintf(stderr, "Error: Connection to reader failed. Proceeding to load firmware, anyway.\n");
      }
      else
      {
        checkerr(rp, ret, 1, "Connecting reader");
      }
    }
    break;
  case TMR_ERROR_TM_ASSERT_FAILED:
    fprintf(stderr, "Error: Assert failed. Proceeding to load firmware, anyway.\n");
    break;
  default:
    checkerr(rp, ret, 1, "connecting reader");
    break;
  }
  printf("Connected to reader\n");

  printf("Opening \"%s\"\n", filename);
  f = fopen(filename, "rb");
  if (NULL == f)
  {
    perror("Can't open file");
    return 1;
  }
  
  printf("Loading firmware\n");
  ret = TMR_firmwareLoad(rp, f, TMR_fileProvider);

  if (ret == TMR_ERROR_TIMEOUT)
  {
    fprintf(stderr, "Error: The firmware loading might have been corrupted. Please try loading it again.\n");
    exit(1);
  }
  else
  {
    checkerr(rp, ret, 1, "loading firmware");
  }

  {
    TMR_String value;
    char value_data[64];
    value.value = value_data;
    value.max = sizeof(value_data)/sizeof(value_data[0]);

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_SOFTWARE, &value);
    checkerr(rp, ret, 1, "getting software version");
    printf("New firmware version: %s\n", value.value);
  }

  TMR_destroy(rp);
  return 0;
}
