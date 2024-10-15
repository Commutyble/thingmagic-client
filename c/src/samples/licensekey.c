/**
 * Sample program that performs license key operations (set or erase).
 * @file licensekey.c
 */

#include <tm_reader.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <stdarg.h>
#include "serial_reader_imp.h"
#include <inttypes.h>

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--option <license operation>] [--key <licence key>]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "<license operation> : a)set \n"\
                         "                      b)erase \n"\
                         "Example: tmr:///com4 --option set --key AB CD\n"\
                         "Example: tmr:///com4 --option erase\n");}

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

void parseLicenseKey(uint8_t *license, uint8_t *length, char *args[])
{
  uint8_t tempLicense[100];
  uint8_t i, j, count;
  count = 0;
  for (i = 0; i < *length; i++)
  {
    for (j = 0; args[i][j] ;j++)
    {
      tempLicense[count++] = args[i][j];
    }
  }
  tempLicense[count] = '\0';
  count = 0;
  for (i = 0; tempLicense[i]; i = i + 2)
  {
    const char tempdata[2] = {tempLicense[i] , tempLicense[i+1]};
#ifdef WIN32
    sscanf(tempdata, "%2hh"SCNx8, &license[count++]);
#else
    sscanf(tempdata, "%2"SCNx8, &license[count++]);
#endif
  }
  license[count] = '\0';
  *length = count;
}

/**
 * Parse the license operation option 
 *
 * @param index : starting index of option
 * @param option : points to the variable where option need to store
 * @param args : pointer to command line argument array
*/
void parseLicenseOperationOption(int index, int *option, char *args[])
{
  /* parse license option provided by user */
  if((0x00 == strcmp("set", args[index + 1])) || (0x00 == strcmp("SET", args[index + 1])))
    *option = TMR_SET_LICENSE_KEY;
  else if((0x00 == strcmp("erase", args[index + 1])) || (0x00 == strcmp("ERASE", args[index + 1])))
    *option = TMR_ERASE_LICENSE_KEY;
  else
  {
    fprintf(stdout, "Unsupported license operation\n");
    usage();
  }    
}

/**
 * Calculates the license key length 
 *
 * @param index : starting index of license key
 * @param isOptionFound : option keyword found flag
 * @param argCount : arguments count
 * @param args : pointer to command line argument array
*/
int calculateLicenseKeyLength(int index, bool isOptionFound, int argCount, char *args[])
{ 
  int keyLen = 0;
  int nextIndex = 0;
  bool nextArgIsOption = 0;
  
  if(isOptionFound)
  {
      keyLen = argCount - 5;
  }
  else
  {
      nextIndex = index + 1;
    for(; (index < argCount) && !nextArgIsOption ; index++, nextIndex++)
    {
      if((0x00 == strcmp("--option", args[nextIndex])) || (0x00 == strcmp("--OPTION", args[nextIndex])))
        nextArgIsOption = true; 
      else if((nextIndex + 1) == argCount)
      {
        fprintf(stdout, "license operation option is not found\n");
        usage();
      }
      else
        keyLen++;
    }
    index--;
  }
  return keyLen;
}

#ifdef TMR_ENABLE_HF_LF
void parseTagFeatures(TMR_SupportedTagFeatures tagFeatures)
{
  uint8_t j = 1;

  if (tagFeatures)
  {
    uint8_t i;
    for (i = 0; i < 8; i++)
    {
      if ((j << i) & tagFeatures)
      {
        switch(j << i)
        {
          case TMR_HF_HID_ICLASS_SE_SECURE_RD:
          {
            printf("Enabled HID iClass SE Secure Read\n");
            break;
          }
          case TMR_LF_HID_PROX_SECURE_RD:
          {
            printf("Enabled HID Prox Secure Read\n");
            break;
          }
          default:
          {
            printf("Enabled Unknown feature\n");
            break;
          }
        }
      }
    }
  }
  else
  {
    printf("No Tag Features are enabled\n");
  }
}
#endif /* TMR_ENABLE_HF_LF */

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  uint8_t *keyData = NULL;
  uint8_t buffer[100];
  uint8_t keyLength = 0;
  int i = 0;
  int keyIndex = 0;
  int option = 0;
  bool optionFound = false;
  bool keyFound = false;
  TMR_LicenseOperation licenseOperation;
  char string[100];
  TMR_String model;

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
  if (argc < 4)
  {
    fprintf(stdout, "Not enough arguments.\n");
    usage();
  }
  else
  {
    for (i = 2; i < argc; i++)
    {
      /* check for license operation option */
      if((!optionFound) && ((0x00 == strcmp("--option", argv[i])) || (0x00 == strcmp("--OPTION", argv[i]))))
      {
        optionFound = true;
        parseLicenseOperationOption(i, &option, argv);
        i++; /* move index to next place */
      }

      /* check for license key */
      else if((!keyFound) && ((0x00 == strcmp("--key", argv[i])) || (0x00 == strcmp("--KEY", argv[i]))))
      {
        keyFound = true;
        keyIndex = i;  /* Store the key index */ 

        /* Calculate the license key length */
        keyLength = calculateLicenseKeyLength(i, optionFound, argc, argv);	
        i += keyLength;

        if(keyLength)
        {
          /* license key parsing */
          parseLicenseKey(buffer, &keyLength, argv + keyIndex + 1);
          keyData = buffer;
        }
        else
        { 
          fprintf(stdout, "license key not found\n");
          usage();
        }
      }
      else
      {
        fprintf(stdout, "Arguments are not recognized\n");
        usage();
      }
    }
    /* Error handling */
    if(!optionFound)
    { 
      fprintf(stdout, "license operation option is not found\n");
      usage();
    }
    else if ((option == 1) && (!keyFound))
    { 
      fprintf(stdout, "license key not found\n");
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

  /* Manage license key */
  {
    TMR_uint8List key;
#if(1) /* Change to "if (0)" to provide key from code */
    key.list = keyData;
    key.len = key.max = keyLength;
#else
    uint8_t keyData1[] = {
      0xAB, 0xCD
    };
    key.list = keyData1;
    key.len = key.max = sizeof(keyData1) / sizeof(keyData1[0]);
#endif

    /* Set license operation */
    licenseOperation.option = option;
    licenseOperation.license = &key;

    printf("License Key operation started...\n");

    //ret = TMR_paramSet(rp, TMR_PARAM_LICENSE_KEY, &key);
    ret = TMR_paramSet(rp, TMR_PARAM_MANAGE_LICENSE_KEY, &licenseOperation);
    if (TMR_SUCCESS != ret)
    {
      fprintf(stderr, "Error license operation: %s\n", TMR_strerr(rp, ret));
    }
    else
    {
      printf("License operation succeeded.\n");
    }
  }

  // Report protocols enabled by current license key
  {
#ifdef TMR_ENABLE_HF_LF
    TMR_SupportedTagFeatures tagFeatures;
#endif /* TMR_ENABLE_HF_LF */
    TMR_TagProtocolList protocols;
    TMR_TagProtocol protocolData[16];
    char* protocolName = NULL;
    int i;

    protocols.list = protocolData;
    protocols.max = sizeof(protocolData) / sizeof(protocolData[0]);
    protocols.len = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_VERSION_SUPPORTEDPROTOCOLS, &protocols);
    checkerr(rp, ret, 1, "getting supported protocols");
    printf("Supported Protocols:\n");
    for (i=0; i<protocols.len; i++)
    {
      switch (protocols.list[i])
      {
        case TMR_TAG_PROTOCOL_GEN2:
          protocolName = "GEN2";
          break;
#ifdef TMR_ENABLE_ISO180006B
        case TMR_TAG_PROTOCOL_ISO180006B:
          protocolName = "ISO18000-6B";
          break;
        case TMR_TAG_PROTOCOL_ISO180006B_UCODE:
          protocolName = "ISO18000-6B_UCODE";
          break;
#endif /* TMR_ENABLE_ISO180006B */
#ifndef TMR_ENABLE_GEN2_ONLY
        case TMR_TAG_PROTOCOL_IPX64:
          protocolName = "IPX64";
          break;
        case TMR_TAG_PROTOCOL_IPX256:
          protocolName = "IPX256";
          break;
        case TMR_TAG_PROTOCOL_ATA:
          protocolName = "ATA";
          break;
#endif /* TMR_ENABLE_GEN2_ONLY */
        case TMR_TAG_PROTOCOL_ISO14443A:
          protocolName = "ISO14443A";
          break;
        case TMR_TAG_PROTOCOL_ISO15693:
          protocolName = "ISO15693";
          break;
        case TMR_TAG_PROTOCOL_LF125KHZ:
          protocolName = "LF125KHZ";
          break;
        case TMR_TAG_PROTOCOL_LF134KHZ:
          protocolName = "LF134KHZ";
          break;
        default:
          protocolName = NULL;
          break;
      }

      if (NULL != protocolName)
      {
        printf("%s\n", protocolName);
      }
      else
      {
        printf("0x%02X\n", protocols.list[i]);
      }
    }
    printf("\n");

#ifdef TMR_ENABLE_HF_LF
    if (NULL != protocolName)
    {
      for (i = 0; i < protocols.len; i++)
      {
        switch (protocols.list[i])
        {
          case TMR_TAG_PROTOCOL_ISO14443A:
          {
            printf("ISO14443A Tag Features:\n");
            ret = TMR_paramGet(rp, TMR_PARAM_ISO14443A_SUPPORTED_TAG_FEATURES, &tagFeatures);
            checkerr(rp, ret, 1, "Getting ISO14443A supported tag features");
            parseTagFeatures(tagFeatures);
            break;
          }
          case TMR_TAG_PROTOCOL_ISO15693:
          {
            printf("\nISO15693 Tag Features:\n");
            ret = TMR_paramGet(rp, TMR_PARAM_ISO15693_SUPPORTED_TAG_FEATURES, &tagFeatures);
            checkerr(rp, ret, 1, "Getting ISO15693 supported tag features");
            parseTagFeatures(tagFeatures);
            break;
          }
          case TMR_TAG_PROTOCOL_LF125KHZ:
          {
            printf("\nLF125KHZ Tag Features:\n");
            ret = TMR_paramGet(rp, TMR_PARAM_LF125KHZ_SUPPORTED_TAG_FEATURES, &tagFeatures);
            checkerr(rp, ret, 1, "Getting LF125KHZ supported tag features");
            parseTagFeatures(tagFeatures);
            break;
          }
          default:
            break;
        }
      }
    }
#endif /* TMR_ENABLE_HF_LF */
  }

  TMR_destroy(rp);
  return 0;
}
