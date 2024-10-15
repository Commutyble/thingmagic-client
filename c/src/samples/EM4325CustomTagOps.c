/**
 * Sample program that demonstrates EM4325 Custom tag operations.
 * @file EM4325CustomTagOps.c
 */

#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#include <tmr_utils.h>

#if WIN32
#define snprintf sprintf_s
#endif 

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

/* Enable this to enable filter */
#ifndef ENABLE_FILTER
#define ENABLE_FILTER 0
#endif

/* Enable this to enable embedded read */
#ifndef ENABLE_EMBEDDED_TAGOP
#define ENABLE_EMBEDDED_TAGOP 1
#endif

#ifdef TMR_ENABLE_UHF
//LowBatteryAlarm.
typedef enum LowBatteryAlarm
{
  LBA_NOPROBLEM = 0,
  LBA_LOWBATTERYDETECTED = 1,
}LowBatteryAlarm;

//AuxAlarm.
typedef enum AuxAlarm
{
  AA_NOPROBLEM = 0,
  AA_TAMPER_OR_SPI_ALARM_DETECTED = 1,
}AuxAlarm;

//OverTempAlarm.
typedef enum OverTempAlarm
{
  OTA_NOPROBLEM = 0,
  OTA_OVERTEMPERATURE_DETECTED = 1
}OverTempAlarm;

//UnderTempAlarm.
typedef enum UnderTempAlarm
{
  UTA_NOPROBLEM = 0,
  UTA_UNDERTEMPERATURE_DETECTED = 1
}UnderTempAlarm;

//P3Input.
typedef enum P3Input
{
  P3I_NOSIGNAL = 0,
  P3I_SIGNALLEVEL = 1
}P3Input;

//MonitorEnabled.
typedef enum MonitorEnabled
{
  ME_DISABLED = 0,
  ME_ENABLED = 1
}MonitorEnabled;

typedef struct SensorData
{     
  //LowBatteryAlarm status- MSW Bit 0
  LowBatteryAlarm lowBatteryAlarmStatus;

  //AuxAlarm status - MSW Bit 1
  AuxAlarm auxAlarmStatus;

  //OverTempAlarm status - MSW Bit 2
  OverTempAlarm overTempAlarmStatus;

  //UnderTempAlarm status - MSW Bit 3
  UnderTempAlarm underTempAlarmStatus;

  //P3Input status - MSW Bit 4
  P3Input p3InputStatus;

  //MonitorEnabled status - MSW Bit 5
  MonitorEnabled monitorEnabledStatus;
  //MSW Bit 6 always 0.

  //Temperature value in degree celsius -  MSW Bit 7 - F (9 bits)
  uint16_t temperature;

  //Aborted Temperature Count - LSW Bits 0 - 5
  uint8_t abortedTemperatureCount;

  //Under Temperature Count - LSW Bits 6 - A
  uint8_t underTemperatureCount;

  //Over Temperature Count  - LSW Bits B - F
  uint8_t overTemperatureCount;
}SensorData;

typedef struct GetSensorDataResponse
{        
  //UID of Tag
  TMR_uint8List uid;

  //Sensor data
  SensorData sensorData;

  //UTC Timestamp
  uint32_t utcTimestamp;
}GetSensorDataResponse;
#endif /* TMR_ENABLE_UHF */

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
#ifdef TMR_ENABLE_UHF
//Declarations.
void GetSensorDataRsp(GetSensorDataResponse *sensorData, uint8_t *response, uint8_t respLen);
void parseSensorData(SensorData *sensorStatus, uint32_t sensorData);
void displaySensorData(SensorData *sensorData);
void displaySensorDataResponse(GetSensorDataResponse *sensorDataRsp);

#if ENABLE_EMBEDDED_TAGOP
void ReadTags(TMR_Reader* rp)
{
  TMR_Status ret;
  ret = TMR_read(rp, 500, NULL);
  if (TMR_ERROR_TAG_ID_BUFFER_FULL == ret)
  {
    /* In case of TAG ID Buffer Full, extract the tags present
     * in buffer
    */
    fprintf(stdout, "reading tags:%s\n", TMR_strerr(rp, ret));
  }
  else
  {
    checkerr(rp, ret, 1, "reading tags");
  }
  printf("Embedded opeartion is successful.\n");
  while (TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    TMR_TagReadData trd;
    char epcStr[128];
    uint8_t dataBuf[256];

    ret = TMR_TRD_init_data(&trd, sizeof(dataBuf)/sizeof(uint8_t), dataBuf);
    checkerr(rp, ret, 1, "creating tag read data");

    ret = TMR_getNextTag(rp, &trd);
    checkerr(rp, ret, 1, "fetching tag");

    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr);
    printf("\nTag ID %s", epcStr);

    if (0 < trd.data.len)
    {
      //Check tag op type.
      if(rp->readParams.readPlan->u.simple.tagop->type == TMR_TagOP_GEN2_EM4325_GET_SENSOR_DATA)
      {
        GetSensorDataResponse sensorDataRsp;
        uint8_t data[16];
        uint8_t idx = 0;
        uint8_t respLen = 0;

        //Initialize.
        sensorDataRsp.uid.list = data;
        sensorDataRsp.uid.max = sizeof(data) / sizeof(data[0]);
        sensorDataRsp.uid.len = 0;

        //Get the data length(in bits).
        respLen = tm_u8s_per_bits(GETU16(trd.data.list, idx));

        // Parse the response.
        GetSensorDataRsp(&sensorDataRsp, &trd.data.list[idx], respLen);

        //Display sensor response
        displaySensorDataResponse(&sensorDataRsp);
      }
    }
  }
  printf("\n");
}

void performEmbeddedOperation(TMR_Reader *reader, TMR_ReadPlan *plan, TMR_TagOp *tagOp, TMR_TagFilter *filter)
{
  TMR_Status ret;

  // Set tagOp to read plan
  ret = TMR_RP_set_tagop(plan, tagOp);
  checkerr(reader, ret, 1, "setting tagop");

  // Set filter to read plan
#if ENABLE_FILTER
  ret = TMR_RP_set_filter(plan, filter);
  checkerr(reader, ret, 1, "setting filter");
#endif

  // Commit read plan
  ret = TMR_paramSet(reader, TMR_PARAM_READ_PLAN, plan);
  checkerr(reader, ret, 1, "setting read plan");

  ReadTags(reader);
}
#endif /* ENABLE_EMBEDDED_TAGOP */
#endif /* TMR_ENABLE_UHF */
int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
#ifdef TMR_ENABLE_UHF
#if ENABLE_EMBEDDED_TAGOP
  TMR_ReadPlan plan;
#endif * ENABLE_EMBEDDED_TAGOP */
#endif /* TMR_ENABLE_UHF */
  TMR_Region region;
  uint8_t *antennaList = NULL;
  uint8_t buffer[20];
  uint8_t i;
  uint8_t antennaCount = 0x0;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
#ifdef TMR_ENABLE_UHF
  TMR_TagOp tagOp;
  TMR_uint8List response;
  uint8_t responseData[32];
  TMR_TagFilter filter, *pfilter = &filter;
  GetSensorDataResponse sensorDataRsp;
  uint8_t mask[12] = {0x30, 0x28, 0x35, 0x4d, 0x82, 0x02, 0x02, 0x80, 0x00, 0x01, 0x04, 0xAC};
  uint8_t maskTempCtrlWord[2] = {0x04, 0xC2};

  //Initialization.
  response.list = responseData;
  response.max = sizeof(responseData) / sizeof(responseData[0]);
  response.len = 0;
#endif /* TMR_ENABLE_UHF */
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
#ifdef TMR_ENABLE_UHF
  //Use first antenna for operation
  if (NULL != antennaList)
  {
    ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_ANTENNA, &antennaList[0]);
    checkerr(rp, ret, 1, "setting tagop antenna");  
  }

  //em4325 tag op.
  {
    bool sendUID = true;
    bool sendNewSample = true;

#if ENABLE_FILTER
    {
      /* This select filter matches all Gen2 tags where bits 32-48 of the EPC are 0xABAB */
      TMR_TF_init_gen2_select(pfilter, false, TMR_GEN2_BANK_EPC, 32, 96, mask);
    }
#else
    pfilter = NULL;
#endif

    /* Create the Get Sensor Data tag operation */
    ret = TMR_TagOp_init_GEN2_EM4325_GetSensorData(&tagOp, 0x00, sendUID, sendNewSample);
    checkerr(rp, ret, 1, "initializing GEN2_EM4325_getSensorData");

    /* Execute the Get Sensor Data tag op */
    printf("\n****Executing standalone tag operation of Get sensor Data command of EM4325 tag***\n");
    ret = TMR_executeTagOp(rp, &tagOp, pfilter, &response);
    checkerr(rp, ret, 1, "executing EM4325 get sensor data");
    printf("EM4325 Get sensor data is successfull.\n");

    if(response.len)
    {
      uint8_t data[16];
      uint8_t respLen = 0;
      uint8_t idx = 0;

      //Initialize.
      sensorDataRsp.uid.list = data;
      sensorDataRsp.uid.max = sizeof(data) / sizeof(data[0]);
      sensorDataRsp.uid.len = 0;

      //Get the data length(in bits).
      respLen = tm_u8s_per_bits(GETU16(response.list, idx));

      //Parse the response.
      GetSensorDataRsp(&sensorDataRsp, &response.list[idx], respLen);
      //Display sensor response
      displaySensorDataResponse(&sensorDataRsp);
    }

    // Enable embedded tag operation by enabling macro ENABLE_EMBEDDED_TAGOP
#if ENABLE_EMBEDDED_TAGOP
    // initialize the read plan
    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
    checkerr(rp, ret, 1, "initializing the  read plan");

    printf("\n****Executing embedded tag operation of Get sensor Data command of EM4325 tag***\n");
    performEmbeddedOperation(rp, &plan, &tagOp, pfilter);
#endif /* ENABLE_EMBEDDED_TAGOP */
  }

  //Reset alarms command
  {
    // Read back the temperature control word at 0xEC address to verify reset enable alarm bit is set before executing reset alarm tag op.
    printf("\nReading Temperature control word 1 before resetting alarms to ensure reset enable bit is set to 1\n");
#if ENABLE_FILTER
    {
      /* This select filter matches all Gen2 tags where bits 32-48 of the EPC are 0xABAB */
      TMR_TF_init_gen2_select(pfilter, false, TMR_GEN2_BANK_EPC, 0x70, 16, maskTempCtrlWord);
    }
#else
    pfilter = NULL;
#endif
    /* Create the Get Sensor Data tag operation */
    ret = TMR_TagOp_init_GEN2_ReadData(&tagOp, TMR_GEN2_BANK_USER, 0xEC, 0x01);
    checkerr(rp, ret, 1, "initializing GEN2_ReadData");

    /* Execute the Read data tag op */
    ret = TMR_executeTagOp(rp, &tagOp, pfilter, &response);
    checkerr(rp, ret, 1, "executing read data");
    if(response.len)
    {
      uint16_t tempCtrlWord;

      tempCtrlWord = GETU16AT(response.list, 0);
      printf("Temp control word 1: 0x%x\n", tempCtrlWord);
    }

    // If temperature control word is not 0x4000, write the data
    if (response.list[0] != 0x4000)
    {
      uint16_t writeData[] = { 0x4000 };
      TMR_uint16List writeArgs;
      uint8_t wordCount = 1;
      writeArgs.list = writeData;
      writeArgs.len = writeArgs.max = sizeof(writeData)/sizeof(writeData[0]);

      /* Initialize the write and read tagop */
      TMR_TagOp_init_GEN2_WriteData(&tagOp, TMR_GEN2_BANK_USER, 0xEC, &writeArgs);
      checkerr(rp, ret, 1, "initializing GEN2_WriteData");

      /* Execute the write data tag op */
      ret = TMR_executeTagOp(rp, &tagOp, pfilter, &response);
      checkerr(rp, ret, 1, "executing write data");
    }

    /* Create the Get Sensor Data tag operation */
    ret = TMR_TagOp_init_GEN2_EM4325_ResetAlarms(&tagOp, 0x00);    
    checkerr(rp, ret, 1, "initializing GEN2_EM4325_ResetAlarms");

#if ENABLE_FILTER
    {
      /* This select filter matches all Gen2 tags where bits 32-48 of the EPC are 0xABAB */
      TMR_TF_init_gen2_select(pfilter, false, TMR_GEN2_BANK_EPC, 32, 96, mask);
    }
#else
    pfilter = NULL;
#endif

    /* Execute the Reset alarms tag op */
    printf("\n****Executing standalone tag operation of Reset alarms command of EM4325 tag***\n");
    ret = TMR_executeTagOp(rp, &tagOp, pfilter, &response);
    checkerr(rp, ret, 1, "executing EM4325 Reset alarms");
    printf("EM4325 Reset alarms is successfull.\n");

    // Enable embedded tag operation by enabling macro ENABLE_EMBEDDED_TAGOP
#if ENABLE_EMBEDDED_TAGOP
    // initialize the read plan
    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
    checkerr(rp, ret, 1, "initializing the  read plan");

    printf("\n***Executing embedded tag operation of reset alarms command of EM4325 tag***\n");
    performEmbeddedOperation(rp, &plan, &tagOp, pfilter);
#endif /* ENABLE_EMBEDDED_TAGOP */
  }
#endif /* TMR_ENABLE_UHF */

  TMR_destroy(rp);
  return 0;
}

#ifdef TMR_ENABLE_UHF
//GetSensorDataResponse - parses the response and fills uid , sensor data and utc timestamp with values
void GetSensorDataRsp(GetSensorDataResponse *sensorDataRsp, uint8_t *response, uint8_t respLen)
{
   /* Get sensor data response contains.
    * UIDlength(2 bytes) + UID(8 or 10 or 12 bytes) + Sensor Data(4 bytes) + UTC Timestamp(4 bytes)
    */
  uint8_t idx = 0;
  uint32_t data = 0;

  //Get UID length : Deduct Sensor Data(4 bytes) + UTC Timestamp(4 bytes) length. 
  sensorDataRsp->uid.len = respLen - 8;

  //Extract UID.
  memcpy(sensorDataRsp->uid.list, &response[idx], sensorDataRsp->uid.len);
  idx += sensorDataRsp->uid.len;

  //Extract sensor data.
  data = GETU32(response, idx);
  parseSensorData(&sensorDataRsp->sensorData, data);

  //Extract timestamp.
  sensorDataRsp->utcTimestamp = GETU32(response, idx);
}

void parseSensorData(SensorData *sensorStatus, uint32_t sensorData)
{
  //16 bits of MSW + 16 bits of LSW
  uint16_t sensorDataMSW;
  uint16_t sensorDataLSW;

  sensorDataMSW = (uint16_t)((sensorData & 0xFFFF0000) >> 16);
  sensorDataLSW = (uint16_t)(sensorData & 0xFFFF);

  //MSW parsing:
  //MSW Bit 0
  sensorStatus->lowBatteryAlarmStatus = (LowBatteryAlarm)(sensorDataMSW & (1 << 15));
  //MSW Bit 1
  sensorStatus->auxAlarmStatus = (AuxAlarm)(sensorDataMSW & (1 << 14));
  //MSW Bit 2
  sensorStatus->overTempAlarmStatus = (OverTempAlarm)(sensorDataMSW & (1 << 13));
  //MSW Bit 3
  sensorStatus->underTempAlarmStatus = (UnderTempAlarm)(sensorDataMSW & (1 << 12));
  //MSW Bit 4
  sensorStatus->p3InputStatus = (P3Input)(sensorDataMSW & (1 << 11));
  //MSW Bit 5
  sensorStatus->monitorEnabledStatus = (MonitorEnabled)(sensorDataMSW & (1 << 10));
  //MSW Bit 6 always 0.
  //MSW Bit 7 - F (9 bits) for Temperature
  if ((sensorDataMSW & 0x1ff) == 0x100){
    sensorStatus->temperature = 0x100;  //Invalid Measurement
  }
  else {
    sensorStatus->temperature = (uint16_t)((sensorDataMSW & 0x1ff) * 0.25);
  }

  //LSW parsing:
  //LSW Bits 0 - 5 
  sensorStatus->abortedTemperatureCount = (uint8_t)((sensorDataLSW >> 10) & 0xFC);
  //LSW Bits 6 - 10
  sensorStatus->underTemperatureCount = (uint8_t)((sensorDataLSW >> 5) & 0x1F);
  //LSW Bits 11 - 15
  sensorStatus->overTemperatureCount = (uint8_t)((sensorDataLSW >> 0) & 0x1F);
}

void displaySensorData(SensorData *sensorData)
{
  printf("SensorData  :\n");

  //LowBatteryAlarm.
  printf("\t      LowBatteryAlarmStatus   = ");
  sensorData->lowBatteryAlarmStatus ? printf("LOWBATTERYDETECTED\n") : printf("NOPROBLEM\n");

  //AuxAlarm.
  printf("\t      AuxAlarmStatus          = ");
  sensorData->auxAlarmStatus ? printf("TAMPER_OR_SPI_ALARM_DETECTED\n") : printf("NOPROBLEM\n");

  //OverTempAlarm.
  printf("\t      OverTempAlarmStatus     = ");
  sensorData->overTempAlarmStatus ? printf("OVERTEMPERATURE_DETECTED\n") : printf("NOPROBLEM\n");

 //UnderTempAlarm.
  printf("\t      UnderTempAlarmStatus    = ");
  sensorData->underTempAlarmStatus ? printf("UNDERTEMPERATURE_DETECTED\n") : printf("NOPROBLEM\n");

  //P3Input.
  printf("\t      P3InputStatus           = ");
  sensorData->p3InputStatus ? printf("SIGNALLEVEL\n") : printf("NOSIGNAL\n");

  //MonitorEnabled.
  printf("\t      MonitorEnabledStatus    = ");
  sensorData->monitorEnabledStatus ? printf("ENABLED\n") : printf("DISABLED\n");

  //Temperature.
  if (sensorData->temperature == 0x100) {
    printf("\t      Temperature             = Invalid Measurement\n");
  }
  else {
    printf("\t      Temperature             = %d C \n", sensorData->temperature);
  }

  //AbortedTemperatureCount.
  printf("\t      AbortedTemperatureCount = %d\n", sensorData->abortedTemperatureCount);

  //UnderTemperatureCount.
  printf("\t      UnderTemperatureCount   = %d\n", sensorData->underTemperatureCount);

  //OverTemperatureCount.
  printf("\t      OverTemperatureCount    = %d\n", sensorData->overTemperatureCount);
}

void displaySensorDataResponse(GetSensorDataResponse *sensorDataRsp)
{
  //Display UID.
  {
    uint8_t idStr[256];
    TMR_bytesToHex(sensorDataRsp->uid.list, sensorDataRsp->uid.len, idStr);
    printf("\nUID         : %s\n", idStr);
  }

  //Display Sensor Data.
  displaySensorData(&sensorDataRsp->sensorData);

  //Display UTC Timestamp.
  {  
    //UTCTimestamp.
    printf("UTCTimestamp: %d\n", sensorDataRsp->utcTimestamp);
  }
}
#endif /* TMR_ENABLE_UHF */