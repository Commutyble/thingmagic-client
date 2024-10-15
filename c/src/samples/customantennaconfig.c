/**
 * Sample program that demonstrates custom antenna configuration(session,target,filter).
 * @file customantennaconfig.c
 */

#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <tmr_utils.h>
#include <string.h>
#include <inttypes.h>
#ifndef WIN32
#include <unistd.h>
#endif

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

void callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie);
void exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie);

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
#ifdef TMR_ENABLE_LLRP_READER 
  TMR_TagFilter filter;
  TMR_TagFilter filter1;
  TMR_ReadPlan filteredReadPlan;
  TMR_ReadListenerBlock rlb;
  TMR_ReadExceptionListenerBlock reb;
  uint8_t mask[2];
  uint8_t mask1[2];
#endif /* TMR_ENABLE_LLRP_READER */
  uint8_t *antennaList = NULL;
  uint8_t buffer[20];
  uint8_t i;
  uint8_t antennaCount = 0x0;
#ifdef TMR_ENABLE_LLRP_READER
  TMR_GEN2_Session s1=TMR_GEN2_SESSION_S1;
  TMR_GEN2_Target t1=TMR_GEN2_TARGET_A;
  TMR_GEN2_Session s2=TMR_GEN2_SESSION_S1;
  TMR_GEN2_Target t2=TMR_GEN2_TARGET_A;

  TMR_CustomAntConfigPerAntenna config[2];
  TMR_CustomAntConfig cfg;

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif

  cfg.antennaCount=2;
  cfg.antSwitchingType=1;   /*0=Equal switching and 1=Dynamic switching. Setting antenna switching type as dynamic by default*/
  cfg.tagReadTimeout=50000; /*Timeout in MicroSec to switch to next antenna when no tag observations at antenna*/
  cfg.customAntConfigPerAntenna= malloc(cfg.antennaCount*sizeof(struct TMR_CustomAntConfigPerAntenna *));
  config[0].antID=1;
  config[0].session=&s1;
  config[0].target=&t1;
  config[0].filter=&filter;
  config[1].antID=2;
  config[1].session=&s2;
  config[1].target=&t2;
  config[1].filter=&filter1;
  cfg.customAntConfigPerAntenna[0]=&config[0];
  cfg.customAntConfigPerAntenna[1]=&config[1];
#endif /* TMR_ENABLE_LLRP_READER */
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
  
  // Create Reader object, connecting to physical device
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

#ifdef TMR_ENABLE_LLRP_READER
  TMR_RP_init_simple(&filteredReadPlan,
                     antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
  TMR_RP_set_customAntConfig(&filteredReadPlan,&cfg);

  // In case of Network readers, ensure that bitLength is a multiple of 8
  mask[0] = 0xde;
  mask[1] = 0xad;
  TMR_TF_init_gen2_select(&filter, false, TMR_GEN2_BANK_EPC, 32, 16, mask);
  filter.u.gen2Select.target =  INVENTORIED_S1;
  filter.u.gen2Select.action =  ON_N_NOP;
  mask1[0] = 0x11;
  mask1[1] = 0x11;
  TMR_TF_init_gen2_select(&filter1, false, TMR_GEN2_BANK_EPC, 32, 16, mask1);
  filter1.u.gen2Select.target =  INVENTORIED_S1;
  filter1.u.gen2Select.action =  ON_N_NOP;
  /*
   * filteredReadPlan already points to filter, and
   * "/reader/read/plan" already points to filteredReadPlan.
   * However, we need to set it again in case the reader has 
   * saved internal state based on the read plan.
   */
  ret = TMR_paramSet(rp, TMR_paramID("/reader/read/plan"), &filteredReadPlan);
  checkerr(rp, ret, 1, "setting read plan");

  rlb.listener = callback;
  rlb.cookie = NULL;

  reb.listener = exceptionCallback;
  reb.cookie = NULL;

  ret = TMR_addReadListener(rp, &rlb);
  checkerr(rp, ret, 1, "adding read listener");

  ret = TMR_addReadExceptionListener(rp, &reb);
  checkerr(rp, ret, 1, "adding exception listener");

  ret = TMR_startReading(rp);
  checkerr(rp, ret, 1, "starting reading");

#ifndef WIN32
  sleep(5);
#else
  Sleep(5000);
#endif

  ret = TMR_stopReading(rp);
  checkerr(rp, ret, 1, "stopping reading");
#endif /* TMR_ENABLE_LLRP_READER */

  TMR_destroy(rp);
  return 0;
}
#ifdef TMR_ENABLE_LLRP_READER
void
callback(TMR_Reader *reader, const TMR_TagReadData *t, void *cookie)
{
  char epcStr[128];

  TMR_bytesToHex(t->tag.epc, t->tag.epcByteCount, epcStr);
  printf("Background read: %s and antenna:%d\n", epcStr, t->antenna);
}

void 
exceptionCallback(TMR_Reader *reader, TMR_Status error, void *cookie)
{
  fprintf(stdout, "Error:%s\n", TMR_strerr(reader, error));
}
#endif /* TMR_ENABLE_LLRP_READER */
