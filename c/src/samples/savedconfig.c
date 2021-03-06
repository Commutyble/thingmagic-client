/**
 * Sample program that reads tags for a fixed period of time (500ms)
 * @file SavedConfig.c
 */

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

const char* protocolName(TMR_TagProtocol protocol)
{
	switch (protocol)
	{
	case TMR_TAG_PROTOCOL_NONE:
		return "NONE";
	case TMR_TAG_PROTOCOL_ISO180006B:
		return "ISO180006B";
	case TMR_TAG_PROTOCOL_GEN2:
		return "GEN2";
	case TMR_TAG_PROTOCOL_ISO180006B_UCODE:
		return "ISO180006B_UCODE";
	case TMR_TAG_PROTOCOL_IPX64:
		return "IPX64";
	case TMR_TAG_PROTOCOL_IPX256:
		return "IPX256";
	case TMR_TAG_PROTOCOL_ATA:
		return "ATA";
    case TMR_TAG_PROTOCOL_ISO14443A:
      return "ISO14443A";
    case TMR_TAG_PROTOCOL_ISO15693:
     return "ISO15693";
    case TMR_TAG_PROTOCOL_LF125KHZ:
      return "LF125KHZ";
    case TMR_TAG_PROTOCOL_LF134KHZ:
      return "LF134KHZ";
	default:
		return "unknown";
	}
}

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_SR_UserConfigOp config;
 #if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
  TMR_TagProtocol protocol;
  TMR_Region region;
  TMR_String model;
  char string[64];
  model.max = 64;

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
  checkerr(rp, ret, 1, "connecting reader");

  model.value = string;
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

  if (0 != strcmp("M3e", model.value))
  {
    protocol = TMR_TAG_PROTOCOL_GEN2;
  }
  else
  {
    protocol = TMR_TAG_PROTOCOL_ISO14443A;
  }

  ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_PROTOCOL, &protocol);   // This to set the protocol
  checkerr(rp, ret, 1, "setting protocol");
  
  if ((0 == strcmp("M6e", model.value)) || (0 == strcmp("M6e PRC", model.value))
      || (0 == strcmp("M6e Micro", model.value)) || (0 == strcmp("M6e Nano", model.value))
      || (0 == strcmp("M6e Micro USB", model.value)) || (0 == strcmp("M6e Micro USBPro", model.value))
      || (0 == strcmp("M6e JIC", model.value)) || (0 == strcmp("M3e", model.value)))
  {
		//Init UserConfigOp structure to save configuration
        TMR_init_UserConfigOp(&config, TMR_USERCONFIG_SAVE);
		ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
		checkerr(rp, ret, 1, "setting user configuration: save all configuration");
		printf("User config set option:save all configuration\n");

		//Init UserConfigOp structure to Restore all saved configuration parameters
        TMR_init_UserConfigOp(&config, TMR_USERCONFIG_RESTORE);
		ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
		checkerr(rp, ret, 1, "setting configuration: restore all saved configuration params");
		printf("User config set option:restore all saved configuration params\n");

		//Init UserConfigOp structure to verify all saved configuration parameters
        TMR_init_UserConfigOp(&config, TMR_USERCONFIG_VERIFY);
		ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
		checkerr(rp, ret, 1, "setting configuration: verify all saved configuration params");
		printf("User config set option:verify all configuration\n");

  // Get User Profile
  {
    TMR_Region region;
    TMR_TagProtocol proto;
    uint32_t baudrate;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
    printf("Get user config success - option:Region\n");
    printf("%d\n", region);

    ret = TMR_paramGet(rp, TMR_PARAM_TAGOP_PROTOCOL, &proto);
    printf("Get user config success - option:Protocol\n");
    printf("%s\n", protocolName(proto));

    ret = TMR_paramGet(rp, TMR_PARAM_BAUDRATE, &baudrate);
    printf("Get user config success option:Baudrate\n");
    printf("%d\n", baudrate);
		}

		//Init UserConfigOp structure to reset/clear all configuration parameter
  TMR_init_UserConfigOp(&config, TMR_USERCONFIG_CLEAR);
		ret = TMR_paramSet(rp, TMR_PARAM_USER_CONFIG, &config);
		checkerr(rp, ret, 1, "setting user configuration option: reset all configuration parameters");
		printf("User config set option:reset all configuration parameters\n");
    //Set the Region and protocol for further operations after reset/clear configuration
    {
      TMR_Region region;
      TMR_TagProtocol proto;

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

      ret = TMR_paramGet(rp, TMR_PARAM_TAGOP_PROTOCOL, &proto);
      checkerr(rp, ret, 1, "getting protocol");
      if (TMR_TAG_PROTOCOL_NONE == proto)
      {
        if (0 != strcmp("M3e", model.value))
		{
          proto = TMR_TAG_PROTOCOL_GEN2;
		}
		else
		{
          proto = TMR_TAG_PROTOCOL_ISO14443A;
		}
        ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_PROTOCOL, &proto);
        checkerr(rp, ret, 1, "setting protocol");
      }
    }
  }
  else
  {
    printf("Error: This codelet works only on M6e variants and on M3e.\n");
  }

  TMR_destroy(rp);
  return 0;
}
