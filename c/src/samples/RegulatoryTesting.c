/**
 * Sample program that demonstrates how to execute
 * CW/PRBS in TIMED or CONTINUOUS mode
 * @file RegulatoryTesting.c
 */

#include <tm_reader.h>
#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#ifndef WIN32
#include <unistd.h>
#endif

#ifndef BARE_METAL
#if WIN32
#define snprintf sprintf_s
#endif 

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif
#define numberof(x) (sizeof((x))/sizeof((x)[0]))

#ifdef TMR_ENABLE_UHF
bool turnRFoff = false;
uint32_t onTime;
uint32_t offTime;
TMR_SR_RegulatoryMode regMode;
TMR_SR_RegulatoryModulation regModulation;
pthread_t backgrndTemperature;
pthread_mutex_t backgrndTemperatureLock;
TMR_Status stop_RF(TMR_Reader *rp);
TMR_Status start_RF(TMR_Reader *rp);
TMR_Status get_Temperature(TMR_Reader *rp, uint32_t onTime);
static void * Get_backgrnd_Temperature(void *arg);
#endif /* TMR_ENABLE_UHF */

#define usage() {errx(1, " Please provide valid arguments, such as: reader-uri [--ant antenna] [--mode regulatory_mode] [--modulation regulatory_modulation] [--ontime regulatory_ontime] [--offtime regulatory_offtime]\n"\
                         " reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/'\n"\
                         " [--ant antenna] : e.g., '--ant 1'\n"\
                         " [--mode regulatory_mode] : e.g., '--mode CONTINUOUS'\n"\
                         " [--modulation regulatory_modulation] : e.g., '--mode CW'\n"\
                         " [--ontime regulatory_ontime] : e.g., '--ontime 1000'\n"\
                         " [--offtime regulatory_offtime] : e.g., '--offtime 500'\n"\
                         " Example:tmr:///com4 --mode CONTINUOUS --modulation CW --ontime 1000 --offtime 500\n"\
                         );}

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
#endif

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
#ifdef TMR_ENABLE_UHF
  uint8_t *antennaList = NULL;
  uint8_t buffer[20];
  uint8_t i;
  uint32_t totalTimeout;
  uint8_t antennaCount = 0x0;
#endif /* TMR_ENABLE_UHF */
#ifndef BARE_METAL
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
 
  if (argc < 2)
  {
    fprintf(stdout, "Not enough arguments. Please provide reader URL.\n");
    usage(); 
  }
#ifdef TMR_ENABLE_UHF
  for (i = 2; i < argc; i+=2)
  {
    if(0x00 == strcmp("--ant", argv[i]))
    {
      if (NULL != antennaList)
      {
        fprintf(stdout, "Duplicate argument: --ant specified more than once\n\n");
        usage();
      }
      parseAntennaList(buffer, &antennaCount, argv[i+1]);
      antennaList = buffer;
    }
    else if(strcmp(argv[i], "--mode") == 0) 
    {
      if(strcmp(argv[i + 1], "CONTINUOUS") == 0)
      {
        regMode = CONTINUOUS;
      }
      else if(strcmp(argv[i + 1], "TIMED") == 0)
      {
        regMode = TIMED;
      }
      else
      {
        fprintf(stdout, "Argument %s is not recognized. Regulatory mode can be either CONTINUOUS or TIMED.\n\n",argv[i + 1]);
        usage();
      }
    }
    else if(strcmp(argv[i], "--modulation") == 0) 
    {
      if(strcmp(argv[i + 1], "CW") == 0)
      {
        regModulation = CW;
      }
      else if(strcmp(argv[i + 1], "PRBS") == 0)
      {
        regModulation = PRBS;
      }
      else
      {
        fprintf(stdout, "Argument %s is not recognized. Regulatory modulation can be either CW or PRBS.\n\n",argv[i + 1]);
        usage();
      }
    }
    else if(strcmp(argv[i], "--ontime") == 0) 
    {
      onTime = atoi(argv[i + 1]);
    }
    else if(strcmp(argv[i], "--offtime") == 0) 
    {
      offTime = atoi(argv[i + 1]);
    }
    else
    {
      fprintf(stdout, "Argument %s is not recognized\n\n", argv[i]);
      usage();
    }
  }
#endif /* TMR_ENABLE_UHF */  
#endif
  
  rp = &r;
#ifndef BARE_METAL
  ret = TMR_create(rp, argv[1]);
#else
  ret = TMR_create(rp, "tmr:///com1");
  buffer[0] = 1;
  antennaList = buffer;
  antennaCount = 0x01;
#endif

#ifndef BARE_METAL
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
#endif

  ret = TMR_connect(rp);

#ifndef BARE_METAL
  checkerr(rp, ret, 1, "connecting reader");
#endif
  region = TMR_REGION_NONE;
  ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "getting region");
#endif
  
  if (TMR_REGION_NONE == region)
  {
    TMR_RegionList regions;
    TMR_Region _regionStore[32];
    regions.list = _regionStore;
    regions.max = sizeof(_regionStore)/sizeof(_regionStore[0]);
    regions.len = 0;

    ret = TMR_paramGet(rp, TMR_PARAM_REGION_SUPPORTEDREGIONS, &regions);
#ifndef BARE_METAL
    checkerr(rp, ret, __LINE__, "getting supported regions");

    if (regions.len < 1)
    {
      checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't support any regions");
    }
#endif  
    region = regions.list[0];
    ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, &region);
#ifndef BARE_METAL    
    checkerr(rp, ret, 1, "setting region");
#endif 
  }

#ifdef TMR_ENABLE_UHF
  {
    totalTimeout = (onTime + offTime);

    ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_MODE, &regMode);
    checkerr(rp, ret, 1, "Setting mode");

    ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_MODULATION, &regModulation);
    checkerr(rp, ret, 1, "Setting modulation");

    ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_ONTIME, &onTime);
    checkerr(rp, ret, 1, "Setting ontime");

    ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_OFFTIME, &offTime);
    checkerr(rp, ret, 1, "Setting offtime");

    ret = TMR_paramSet(rp, TMR_PARAM_COMMANDTIMEOUT, &totalTimeout);
    checkerr(rp, ret, 1, "Setting Command timeout");

    ret = start_RF(rp);
#ifndef WIN32
    sleep(5);
#else
    Sleep(5000);
#endif
    ret = stop_RF(rp);
  } 
#endif /* TMR_ENABLE_UHF */

  TMR_destroy(rp);
  return 0;
}

#ifdef TMR_ENABLE_UHF
TMR_Status
start_RF(TMR_Reader *rp)
{
  TMR_Status ret = TMR_SUCCESS;
  bool enable = true;

  printf("\n  !!!!! ALERT !!!!");
  printf("\n Module may get hot when RF ON time is more than 10 seconds");
  printf("\n Risk of damage to the module despite auto cut off feature \n");

  if(pthread_mutex_init(&backgrndTemperatureLock, NULL) != 0)
  {
    printf("\n Mutex init failed\n");
  }
  
  ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_ENABLE, &enable);
  checkerr(rp, ret, 1, "Turn ON RF");
  
  if(TMR_SUCCESS == ret)
  {
    if(regMode == TIMED)
    {
      ret = get_Temperature(rp, onTime);
    }
    if(regMode == CONTINUOUS)
    {
      ret = pthread_create(&backgrndTemperature, NULL, Get_backgrnd_Temperature, rp);
      if (0 != ret)
      {
        printf("\n No thread created. \n");
      }
    }
  }
  return ret;
}

TMR_Status
stop_RF(TMR_Reader *rp)
{
  TMR_Status ret = TMR_SUCCESS;
  bool enable = false;

  if(!turnRFoff)
  {
    turnRFoff = true;

    pthread_mutex_lock(&backgrndTemperatureLock);
    ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_ENABLE, &enable);
    pthread_mutex_unlock(&backgrndTemperatureLock);

    checkerr(rp, ret, 1, "Turn OFF RF");
  }
  return ret;
}

TMR_Status
get_Temperature(TMR_Reader *rp, uint32_t onTime)
{
  TMR_Status ret = TMR_SUCCESS;
  uint8_t temp, i;
  i = (onTime / 1000);

  while(i--)
  {
    pthread_mutex_lock(&backgrndTemperatureLock);
    ret = TMR_paramGet(rp, TMR_PARAM_RADIO_TEMPERATURE, &temp);
    pthread_mutex_unlock(&backgrndTemperatureLock);
    printf("\nTemperature = %d\n",temp);

    if(TMR_ERROR_TEMPERATURE_EXCEED_LIMITS == ret)
    {
      bool enable = false;
      printf("Error : Reader temperature is too high.\n");
      ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_ENABLE, &enable);
      checkerr(rp, ret, 1, "Turn OFF RF");
      break;
    }
#ifndef WIN32
    sleep(1);
#else
    Sleep(1000);
#endif
  }
  return ret;
}

static void *
Get_backgrnd_Temperature(void *arg)
{
  TMR_Status ret = TMR_SUCCESS;
  TMR_Reader *rp;
  uint8_t temp;
  rp = arg;
  turnRFoff = false;

  while(!turnRFoff)
  {
    pthread_mutex_lock(&backgrndTemperatureLock);
    ret = TMR_paramGet(rp, TMR_PARAM_RADIO_TEMPERATURE, &temp);
    pthread_mutex_unlock(&backgrndTemperatureLock);
    printf("\nTemperature = %d\n",temp);

    if(TMR_ERROR_TEMPERATURE_EXCEED_LIMITS == ret)
    {
      bool enable = false;
      printf("Error : Reader temperature is too high.\n");
      if(!turnRFoff)
      {
        turnRFoff = true;
        ret = TMR_paramSet(rp, TMR_PARAM_REGULATORY_ENABLE, &enable);
        checkerr(rp, ret, 1, "Turn OFF RF");
        break;
      }
    }
#ifndef WIN32
    sleep(1);
#else
    Sleep(1000);
#endif
  }
  if (turnRFoff)
  {
    pthread_exit(NULL);
  }
  return NULL;
}
#endif /* TMR_ENABLE_UHF */