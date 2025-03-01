/*
 * Sample program that reads tags for a fixed period of time (500ms)
 * and prints the tags found.
 * @file read.c
 */

#include <tm_reader.h>
#include <time.h>
#include <unistd.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <inttypes.h>
#ifdef TMR_ENABLE_HF_LF
#include <tmr_utils.h>
#endif /* TMR_ENABLE_HF_LF */



#ifndef BARE_METAL

/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif
#define numberof(x) (sizeof((x))/sizeof((x)[0]))

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n] [--pow read_power]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "[--pow read_power] : e.g, '--pow 2300'\n"\
                         "[--pow1 read_power] : e.g, '--pow 2300'\n"\
                         "[--pow2 read_power] : e.g, '--pow 2300'\n"\
                         "Example: 'tmr:///dev/ttyACM0 --ant 1,2 --pow1 1700 --pow2 1500'\n");}

void errx(int exitval,
  const char * fmt, ...) {
  va_list ap;

  va_start(ap, fmt);
  vfprintf(stderr, fmt, ap);

  exit(exitval);
}

void checkerr(TMR_Reader * rp, TMR_Status ret, int exitval,
  const char * msg) {
  if (TMR_SUCCESS != ret) {
    errx(exitval, "Error %s: %s\n", msg, TMR_strerr(rp, ret));
  }
}

void serialPrinter(bool tx, uint32_t dataLen,
  const uint8_t data[],
    uint32_t timeout, void * cookie) {
  FILE * out = cookie;
  uint32_t i;

  fprintf(out, "%s", tx ? "Sending: " : "Received:");
  for (i = 0; i < dataLen; i++) {
    if (i > 0 && (i & 15) == 0) {
      fprintf(out, "\n         ");
    }
    fprintf(out, " %02x", data[i]);
  }
  fprintf(out, "\n");
}

void stringPrinter(bool tx, uint32_t dataLen,
  const uint8_t data[], uint32_t timeout, void * cookie) {
  FILE * out = cookie;

  fprintf(out, "%s", tx ? "Sending: " : "Received:");
  fprintf(out, "%s\n", data);
}

void parseAntennaList(uint8_t * antenna, uint8_t * antennaCount, char * args) {
  char * token = NULL;
  char * str = ",";
  uint8_t i = 0x00;
  int scans;

  /* get the first token */
  if (NULL == args) {
    fprintf(stdout, "Missing argument\n");
    usage();
  }

  token = strtok(args, str);
  if (NULL == token) {
    fprintf(stdout, "Missing argument after %s\n", args);
    usage();
  }

  while (NULL != token) {
    scans = sscanf(token, "%"
      SCNu8, & antenna[i]);
    if (1 != scans) {
      fprintf(stdout, "Can't parse '%s' as an 8-bit unsigned integer value\n", token);
      usage();
    }
    i++;
    token = strtok(NULL, str);
  }
  * antennaCount = i;
}
#endif

int main(int argc, char * argv[]) {
  TMR_Reader r, * rp;
  TMR_Status ret;
  TMR_TagFilter filter;
  TMR_ReadPlan plan;
  TMR_Region region;
  #define READPOWER_NULL (-12345)
  int readpower = READPOWER_NULL;
  int readpower1 = READPOWER_NULL;
  int readpower2 = READPOWER_NULL;
  #ifndef BARE_METAL
  uint8_t i;
  #endif /* BARE_METAL*/
  uint8_t buffer[20];
  uint8_t * antennaList = NULL;
  uint8_t antennaCount = 0x0;
  TMR_TRD_MetadataFlag metadata = TMR_TRD_METADATA_FLAG_ALL;
  char string[100];
  TMR_String model;

  #ifndef BARE_METAL
  #if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
  #endif

  if (argc < 2) {
    fprintf(stdout, "Not enough arguments.  Please provide reader URL.\n");
    usage();
  }

  for (i = 2; i < argc; i += 2) {
    if (0x00 == strcmp("--ant", argv[i])) {
      if (NULL != antennaList) {
        fprintf(stdout, "Duplicate argument: --ant specified more than once\n");
        usage();
      }
      parseAntennaList(buffer, & antennaCount, argv[i + 1]);
      antennaList = buffer;
    } 
    else if (0 == strcmp("--pow1", argv[i])) {
      long retval;
      char * startptr;
      char * endptr;
      startptr = argv[i + 1];
      retval = strtol(startptr, & endptr, 0);
      if (endptr != startptr) {
        readpower1 = retval;
        fprintf(stdout, "Requested read power1: %d cdBm\n", readpower1);
      } else {
        fprintf(stdout, "Can't parse read power1: %s\n", argv[i + 1]);
      }
    } 
    else if (0 == strcmp("--pow2", argv[i])) {
      long retval;
      char * startptr;
      char * endptr;
      startptr = argv[i + 1];
      retval = strtol(startptr, & endptr, 0);
      if (endptr != startptr) {
        readpower2 = retval;
        fprintf(stdout, "Requested read power2: %d cdBm\n", readpower2);
      } else {
        fprintf(stdout, "Can't parse read power2: %s\n", argv[i + 1]);
      }
    } 
    else if (0 == strcmp("--pow", argv[i])) {
      long retval;
      char * startptr;
      char * endptr;
      startptr = argv[i + 1];
      retval = strtol(startptr, & endptr, 0);
      if (endptr != startptr) {
        readpower = retval;
        fprintf(stdout, "Requested read power: %d cdBm\n", readpower);
      } else {
        fprintf(stdout, "Can't parse read power: %s\n", argv[i + 1]);
      }
    } 
    else {
      fprintf(stdout, "Argument %s is not recognized\n", argv[i]);
      usage();
    }
  }
  #endif

  rp = & r;
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

  if (TMR_READER_TYPE_SERIAL == rp -> readerType) {
    tb.listener = serialPrinter;
  } else {
    tb.listener = stringPrinter;
  }
  tb.cookie = stdout;

  TMR_addTransportListener(rp, & tb);
  #endif
  #endif

  for (int
    try = 0;
    try < 2;
    try ++) {
    ret = TMR_connect(rp);
    if (ret == TMR_SUCCESS) {
      break;
    }
    printf("Error connecting.\n");
  }
  checkerr(rp, ret, 1, "Connecting reader");

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

  printf("Awaiting reboot...\n");
  usleep(250000);

  for (int
    try = 0;
    try < 2;
    try ++) {
    ret = TMR_connect(rp);
    if (ret == TMR_SUCCESS) {
      break;
    }
    printf("Error connecting.\n");
  }
  checkerr(rp, ret, 1, "Connecting reader");

  model.value = string;
  model.max = sizeof(string);
  TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, & model);
  #ifndef BARE_METAL
  checkerr(rp, ret, 1, "Getting version model");
  #endif

printf("connected to %s\n", model.value);
  if (0 != strcmp("M3e", model.value)) {
    region = TMR_REGION_NONE;
    ret = TMR_paramGet(rp, TMR_PARAM_REGION_ID, & region);
    #ifndef BARE_METAL
    //checkerr(rp, ret, 1, "getting region");
    printf("Region get call returned %d\n", ret);
    #endif

    if (TMR_REGION_NONE == region) {
      TMR_RegionList regions;
      TMR_Region _regionStore[32];
      regions.list = _regionStore;
      regions.max = sizeof(_regionStore) / sizeof(_regionStore[0]);
      regions.len = 0;

      ret = TMR_paramGet(rp, TMR_PARAM_REGION_SUPPORTEDREGIONS, & regions);
      #ifndef BARE_METAL
      checkerr(rp, ret, __LINE__, "getting supported regions");

      if (regions.len < 1) {
        checkerr(rp, TMR_ERROR_INVALID_REGION, __LINE__, "Reader doesn't support any regions");
      }
      #endif
      region = regions.list[0];
      ret = TMR_paramSet(rp, TMR_PARAM_REGION_ID, & region);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting region");
      #endif
    }

    if (READPOWER_NULL != readpower) {
      int value;

      ret = TMR_paramGet(rp, TMR_PARAM_RADIO_READPOWER, & value);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "getting read power");
      printf("Old read power = %d dBm\n", value);
      #endif
      value = readpower;

      ret = TMR_paramSet(rp, TMR_PARAM_RADIO_READPOWER, & value);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "setting read power");
      #endif
    }

    {
      int value;
      ret = TMR_paramGet(rp, TMR_PARAM_RADIO_READPOWER, & value);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "getting read power");
      printf("Read power = %d dBm\n", value);
      #endif
    }


  // Read existing antenna read power settings
  {
    TMR_PortValueList value;
    TMR_PortValue valueList[64];

    value.max = numberof(valueList);
    value.list = valueList;

    ret = TMR_paramGet(rp, TMR_PARAM_RADIO_PORTREADPOWERLIST, &value);
    if (TMR_SUCCESS != ret)
    {
      printf("Can't read antenna power list, error = %d\n", ret);
    }
    else {
      int i;

      printf("Existing AntennaPowerList: [");
      for (i = 0; i < value.len && i < value.max; i++)
      {
        printf("[%u,%u]%s", value.list[i].port, value.list[i].value,
               ((i + 1) == value.len) ? "" : ",");
      }
      if (value.len > value.max)
      {
        printf("...");
      }
      putchar(']');

      printf("\n");
    }
  }
  // Write new power list

  int powerlistcount = 0;
  if (READPOWER_NULL != readpower1) {  
    powerlistcount++;
  }
  if (READPOWER_NULL != readpower2) {  
    powerlistcount++;
  }
  if (powerlistcount > 0) {

    TMR_PortValueList value;
    TMR_PortValue valueList[2];
    value.len = powerlistcount;
    value.max = 2;
    value.list = valueList;
    int index = 0;
    if (READPOWER_NULL != readpower1) {  
      value.list[index].port = 1; 
      value.list[index].value = readpower1; 
      printf("Setting antenna1 read power to %d\n", readpower1);
      index++;
    }
    if (READPOWER_NULL != readpower2) {  
      value.list[index].port = 2; 
      value.list[index].value = readpower2; 
      printf("Setting antenna2 read power to %d\n", readpower2);
      index++;
    }
    ret = TMR_paramSet(rp, TMR_PARAM_RADIO_PORTREADPOWERLIST, &value);
    if (TMR_SUCCESS != ret)
    {
      printf("Failed to set antenna read power, error = %d\n", ret);
    }
    else {
      printf("Successfully set antenna power\n");    
    }

    // Read existing antenna read power settings
    {
      TMR_PortValueList value;
      TMR_PortValue valueList[64];

      value.max = numberof(valueList);
      value.list = valueList;

      ret = TMR_paramGet(rp, TMR_PARAM_RADIO_PORTREADPOWERLIST, &value);
      if (TMR_SUCCESS != ret)
      {
        printf("Can't read antenna power list, error = %d\n", ret);
      }
      else {
        int i;

        printf("New AntennaPowerList: [");
        for (i = 0; i < value.len && i < value.max; i++)
        {
          printf("[%u,%u]%s", value.list[i].port, value.list[i].value,
                 ((i + 1) == value.len) ? "" : ",");
        }
        if (value.len > value.max)
        {
          printf("...");
        }
        putchar(']');

        printf("\n");
      }

    }  

  }

  } else {
    if (antennaList != NULL) {
      #ifndef BARE_METAL
      printf("Module doesn't support antenna input\n");
      usage();
      #endif /* BARE_METAL */
    }
  }


  #ifdef TMR_ENABLE_LLRP_READER
  if (0 != strcmp("Mercury6", model.value))
  #endif /* TMR_ENABLE_LLRP_READER */
    {
      // Set the metadata flags. Protocol is mandatory metadata flag and reader don't allow to disable the same
      // metadata = TMR_TRD_METADATA_FLAG_ANTENNAID | TMR_TRD_METADATA_FLAG_FREQUENCY | TMR_TRD_METADATA_FLAG_PROTOCOL;
      ret = TMR_paramSet(rp, TMR_PARAM_METADATAFLAG, & metadata);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "Setting Metadata Flags");
      #endif

    }

  /**
   * for antenna configuration we need two parameters
   * 1. antennaCount : specifies the no of antennas should
   *    be included in the read plan, out of the provided antenna list.
   * 2. antennaList  : specifies  a list of antennas for the read plan.
   **/
  // initialize the read plan
  if (0 != strcmp("M3e", model.value)) // we have M6e -dgy, so the first block executes
  {
    ret = TMR_RP_init_simple( & plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);

  } else {
    ret = TMR_RP_init_simple( & plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO14443A, 1000);
  }
  #ifndef BARE_METAL
  checkerr(rp, ret, 1, "initializing the  read plan");
  #endif

  /*
   * A TagData with a short EPC will filter for tags whose EPC
   * starts with the same sequence.
   */
  filter.type = TMR_FILTER_TYPE_TAG_DATA;
  filter.u.tagData.epcByteCount = 2;
  filter.u.tagData.epc[0] = 0xc0;
  filter.u.tagData.epc[1] = 0x33;
  //tm_memcpy(filter.u.tagData.epc, Temptrd.tag.epc, (size_t)filter.u.tagData.epcByteCount);

  ret = TMR_RP_set_filter( & plan, & filter);
  checkerr(rp, ret, 1, "setting read plan filter");


  

  /**
   * Specifying the readLength = 0 will retutrn full TID for any
   * tag read in case of M6e and M6 reader.
   **/ 
  static TMR_TagOp op;
  uint8_t readLen = 0;
  ret = TMR_TagOp_init_GEN2_ReadData(&op, TMR_GEN2_BANK_TID, 0, readLen);
  checkerr(rp, ret, 1, "creating tagop: GEN2 read data");
  ret = TMR_RP_set_tagop(&plan, &op);
  checkerr(rp, ret, 1, "setting tagop");
  
  
  // Set fast search 
  {
    TMR_GEN2_Target target = TMR_GEN2_TARGET_AB;
    ret = TMR_paramSet(rp, TMR_PARAM_GEN2_TARGET, &target);
    checkerr(rp, ret, 1, "setting Target AB (fast search)");
  }

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, & plan);
  #ifndef BARE_METAL
  checkerr(rp, ret, 1, "setting read plan");
  #endif

  int recent_read_counts[1000];
  for (int i = 0; i < 1000; i++) {
    recent_read_counts[i] = 0;
  }
  int read_index = 0;

  fflush(stdout);
  while (true) {
    ret = TMR_read(rp, 100, NULL);
    int total_count = 0;

    #ifndef BARE_META
    if (ret == TMR_ERROR_TAG_ID_BUFFER_FULL) {
        printf("Tag buffer full.  We must reboot reader.\n");
        exit(0);
    } else {
      checkerr(rp, ret, 1, "reading tags");
    }
    #endif

    while (TMR_SUCCESS == TMR_hasMoreTags(rp)) {
      total_count++;

      TMR_TagReadData trd;
      uint8_t dataBuf[258];
      uint8_t dataBuf1[258];
      uint8_t dataBuf2[258];
      uint8_t dataBuf3[258];
      uint8_t dataBuf4[258];

      ret = TMR_TRD_init_data( & trd, sizeof(dataBuf) / sizeof(uint8_t), dataBuf);
      checkerr(rp, ret, 1, "creating tag read data");

      trd.userMemData.list = dataBuf1;
      trd.epcMemData.list = dataBuf2;
      trd.reservedMemData.list = dataBuf3;
      trd.tidMemData.list = dataBuf4;

      trd.userMemData.max = 258;
      trd.userMemData.len = 0;
      trd.epcMemData.max = 258;
      trd.epcMemData.len = 0;
      trd.reservedMemData.max = 258;
      trd.reservedMemData.len = 0;
      trd.tidMemData.max = 258;
      trd.tidMemData.len = 0;

      //TMR_TagReadData trd;
      char idStr[128];
      char tidStr[258];
      int hasTid = 0;
      #ifndef BARE_METAL
      char timeStr[128];
      #endif /* BARE_METAL */

      ret = TMR_getNextTag(rp, & trd);
      #ifndef BARE_METAL
      checkerr(rp, ret, 1, "fetching tag");
      #endif
      TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, idStr);

      if (0 < trd.tidMemData.len) {
        TMR_bytesToHex(trd.tidMemData.list, trd.tidMemData.len, tidStr);
        hasTid = 1;
      } else {
        hasTid = 0;
      }
      #ifndef BARE_METAL
      TMR_getTimeStamp(rp, & trd, timeStr);
      printf("%s Tag ID: %s ", timeStr, idStr);
      if (0 < trd.data.len)
      {
        char dataStr[2048];
        if (trd.data.len < 512) {
          TMR_bytesToHex(trd.data.list, trd.data.len, dataStr);
          printf("DATA(%d): %s ", trd.data.len, dataStr);
        }
        else {
          printf("Not printing data too long: %d\n", trd.data.len);
        }
      }

      // Enable PRINT_TAG_METADATA Flags to print Metadata value
      if (trd.data.len < 512)
      {
        uint16_t j = 0;
        for (j = 0;
          (1 << j) <= TMR_TRD_METADATA_FLAG_MAX; j++) {
          if ((TMR_TRD_MetadataFlag) trd.metadataFlags & (1 << j)) {
            switch ((TMR_TRD_MetadataFlag) trd.metadataFlags & (1 << j)) {
            case TMR_TRD_METADATA_FLAG_READCOUNT:
              printf("Count: %d ", trd.readCount);
              break;
            case TMR_TRD_METADATA_FLAG_ANTENNAID:
              printf("Ant: %d ", trd.antenna);
              break;
            case TMR_TRD_METADATA_FLAG_TIMESTAMP:
              //printf("Timestamp: %s\n", timeStr);
              break;
            case TMR_TRD_METADATA_FLAG_PROTOCOL:
              //printf("Protocol: %d\n", trd.tag.protocol);
              break;
              #ifdef TMR_ENABLE_UHF
            case TMR_TRD_METADATA_FLAG_RSSI:
              printf("RSSI: %d ", trd.rssi);
              break;
            case TMR_TRD_METADATA_FLAG_FREQUENCY:
              //printf("Frequency: %d\n", trd.frequency);
              break;
            case TMR_TRD_METADATA_FLAG_PHASE:
              //printf("Phase: %d\n", trd.phase);
              break;
              #endif /* TMR_ENABLE_UHF */
            case TMR_TRD_METADATA_FLAG_DATA:
              //TODO : Initialize Read Data
              if (0 < trd.data.len) {
                #ifdef TMR_ENABLE_HF_LF
                if (0x8000 == trd.data.len) {
                  // This happsens sometimes.  We want to go on. -dgy
                  printf("Error Embedded tagOp failed:: Bit decoding failed.  Continuing...");
                  continue;
                  //ret = TMR_translateErrorCode(GETU16AT(trd.data.list, 0));
                  //checkerr(rp, ret, 0, "Embedded tagOp failed:");
                } 
                else
                #endif /* TMR_ENABLE_HF_LF */ 
                {
                    char dataStr[255];
                    uint32_t dataLen = trd.data.len;

                    //Convert data len from bits to byte(For M3e only).
                    if (0 == strcmp("M3e", model.value)) {
                      dataLen = tm_u8s_per_bits(trd.data.len);
                    }

                    TMR_bytesToHex(trd.data.list, dataLen, dataStr);
                    //printf("Data(%d): %s\n", trd.data.len, dataStr);
                  }
              }
              break;
              #ifdef TMR_ENABLE_UHF
            case TMR_TRD_METADATA_FLAG_GPIO_STATUS: {
              if (rp -> readerType == TMR_READER_TYPE_SERIAL) {
                //printf("GPI status:\n");
                for (i = 0; i < trd.gpioCount; i++) {
                  //printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].bGPIStsTagRdMeta ? "High" : "Low");
                }
                //printf("GPO status:\n");
                for (i = 0; i < trd.gpioCount; i++) {
                  //printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
                }
              } else {
                //printf("GPI status:\n");
                for (i = 0; i < trd.gpioCount / 2; i++) {
                  //printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
                }
                //printf("GPO status:\n");
                for (i = trd.gpioCount / 2; i < trd.gpioCount; i++) {
                  //printf("Pin %d: %s\n", trd.gpio[i].id, trd.gpio[i].high ? "High" : "Low");
                }
              }
            }
            break;
            if (TMR_TAG_PROTOCOL_GEN2 == trd.tag.protocol) {
              case TMR_TRD_METADATA_FLAG_GEN2_Q:
                //printf("Gen2Q: %d\n", trd.u.gen2.q.u.staticQ.initialQ);
                break;
              case TMR_TRD_METADATA_FLAG_GEN2_LF: {
                //printf("Gen2Linkfrequency: ");
                switch (trd.u.gen2.lf) {
                case TMR_GEN2_LINKFREQUENCY_250KHZ:
                  //printf("250(khz)\n");
                  break;
                case TMR_GEN2_LINKFREQUENCY_320KHZ:
                  //printf("320(khz)\n");
                  break;
                case TMR_GEN2_LINKFREQUENCY_640KHZ:
                  //printf("640(khz)\n"); 
                  break;
                default:
                  //printf("Unknown value(%d)\n",trd.u.gen2.lf);
                  break;
                }
                break;
              }
              case TMR_TRD_METADATA_FLAG_GEN2_TARGET: {
                //printf("Gen2Target: ");
                switch (trd.u.gen2.target) {
                case TMR_GEN2_TARGET_A:
                  //printf("A\n");
                  break;
                case TMR_GEN2_TARGET_B:
                  //printf("B\n");
                  break;
                default:
                  //printf("Unknown Value(%d)\n",trd.u.gen2.target);
                  break;
                }
                break;
              }
            }
            #endif /* TMR_ENABLE_UHF */
            #ifdef TMR_ENABLE_HF_LF
            case TMR_TRD_METADATA_FLAG_TAGTYPE: {
              //printf("TagType: 0x%08lx\n", trd.tagType);
              break;
            }
            #endif /* TMR_ENABLE_HF_LF */
            default:
              break;
            }
          }
        }
      }
      if (hasTid) {
        printf("TID: %s ", tidStr);
      }

      printf("\n");
      fflush(stdout);

    } // end while has more tags
    #endif
    
    // note sure why this goes here *after* we did the tag read.  Seems like it should be before
    // actually, I think it is for writing to the tag
    /*
    TMR_uint8List dataList;
    uint8_t data[258];
    dataList.len = dataList.max = 258;
    dataList.list = data;

    ret = TMR_executeTagOp(rp, op, filter,&dataList);
    checkerr(rp, ret, 1, "executing the read all mem bank");
    if (0 < dataList.len)
    {
      char dataStr[258];
      TMR_bytesToHex(dataList.list, dataList.len, dataStr);
      printf("  Data(%d): %s\n", dataList.len, dataStr);
    }
    */

    recent_read_counts[read_index] = total_count;
    read_index = (read_index + 1) % 1000;
    int total_recent_read_count = 0;
    for (int i = 0; i < 1000; i++) {
      total_recent_read_count += recent_read_counts[i];
    }

    if (total_recent_read_count > 1000) {
      printf("Total reads in 100 secs: %d.  Sleeping to avoid overheating.\n", total_recent_read_count);
      usleep(100000); // 100 ms
      recent_read_counts[read_index] = 0;
      read_index = (read_index + 1) % 1000;
    }
    else if (read_index == 0) {
      printf("Total reads in 100 secs: %d.\n", total_recent_read_count);
    }

  } // end while true

  TMR_destroy(rp);
  return 0;
}

