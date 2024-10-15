/**
 * Sample program that writes an EPC to a tag
 * and also demonstrates read after write functionality.
 * @file writetag.c
 */

#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <inttypes.h>
#ifndef WIN32
#include <string.h>
#endif
#include <tmr_utils.h>

/* Enable this to enable ReadAfterWrite feature */
#ifndef ENABLE_READ_AFTER_WRITE
#define ENABLE_READ_AFTER_WRITE 0
/* Enable this to enable filter */
#ifndef ENABLE_FILTER
#define ENABLE_FILTER 0
#endif
/***  ENABLE_FILTER  ***/
#endif

#define ENABLE_EMBEDDED_READ 0

#ifdef TMR_ENABLE_HF_LF
#define ENABLE_SYSTEM_INFORMATION_MEMORY 0
#define ENABLE_BLOCK_PROTECTION_STATUS   0
#define ENABLE_SECURE_ID_EMBEDDED_READ   0
#define ENABLE_SET_ACCESS_PASSWORD       0
#endif /* TMR_ENABLE_HF_LF */


/* Enable this to use transportListener */
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER 0
#endif

#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "Example for UHF modules: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' \n"\
                         "Example for HF/LF modules: 'tmr:///com4' \n");}

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

#ifdef TMR_ENABLE_HF_LF
#if ENABLE_SYSTEM_INFORMATION_MEMORY
void parseGetSystemInfoResponse(uint8_t *systemInfo)
{
  uint8_t infoFlags, uidLength, idx;
  uint8_t UID[8];
  char uidStr[17];
  uidLength = 8;
  idx = 0;

  // Extract 1 byte of Information Flags from the response
  infoFlags = systemInfo[idx++];

  //Extract UID - 8 bytes for Iso15693
  memcpy(UID, &systemInfo[idx], uidLength);
  TMR_bytesToHex(UID, uidLength, uidStr);
  printf("UID: %s\n", uidStr);
  idx += uidLength;

  // Checks Information flags are supported or not and then extracts respective fields information.
  if (infoFlags == 0)
  {
    printf("No information flags are enabled\n");
    return;
  }
  if (infoFlags & 0x0001)
  {
    uint8_t dsfid;

    printf("DSFID is supported and DSFID field is present in the response\n");
    //Extract 1 byte of DSFID
    dsfid = systemInfo[idx++];
    printf("DSFID: %d\n", dsfid);
  }
  if (infoFlags & 0x0002)
  {
    uint8_t afi;

    printf("AFI is supported and AFI field is present in the response\n");
    //Extract 1 byte of AFI
    afi = systemInfo[idx++];
    printf("AFI: %d\n", afi);
  }
  if (infoFlags & 0x0004)
  {
    uint8_t maxBlockCount, blocksize;
    uint16_t viccInfo;

    printf("VICC memory size is supported and VICC field is present in the response\n");
    //Extract 2 bytes of VICC information
    viccInfo = GETU16(systemInfo, idx);
    maxBlockCount = (uint8_t)(viccInfo & 0xFF); // holds the number of blocks
    blocksize = (uint8_t)((viccInfo & 0x1F00) >> 8); // holds blocksize
    printf("Max Block Count: %d\nBlock size:%d\n", maxBlockCount, blocksize);
  }
  if (infoFlags & 0x0008)
  {
    uint8_t icRef;

    printf("IC reference is supported and IC reference is present in the response\n");
    // Extract 1 byte of IC reference
    icRef = systemInfo[idx++];
    printf("IC Reference: %d\n", icRef);
  }
}
#endif /* ENABLE_SYSTEM_INFORMATION_MEMORY */

#if ENABLE_BLOCK_PROTECTION_STATUS
void parseBlockProtectionStatusResponse(uint8_t *data, uint32_t address, uint8_t length)
{
  uint8_t i, lockStatus;

  for (i = 0; i < length; i++, address++)
  {
    lockStatus = data[i];

    if (lockStatus & 0x01)
    {
      printf("Block %d is locked.\n", address);
    }
    else
    {
      printf("Block %d is not locked.\n", address);
    }

    if (lockStatus & 0x02)
    {
      printf("Read password protection is enabled for the block %d.\n", address);
    }
    else
    {
      printf("Read password protection is disabled for the block %d.\n", address);
    }

    if (lockStatus & 0x04)
    {
      printf("Write password protection is enabled for block %d.\n", address);
    }
    else
    {
      printf("Write password protection is disabled for the block %d.\n", address);
    }

    if (lockStatus & 0x08)
    {
      printf("Page protection is locked for the block %d.\n", address);
    }
    else
    {
      printf("Page protection is not locked for the block %d.\n", address);
    }
  }
}
#endif /* ENABLE_BLOCK_PROTECTION_STATUS */
#endif /* TMR_ENABLE_HF_LF */

#if ENABLE_EMBEDDED_READ || ENABLE_SECURE_ID_EMBEDDED_READ
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
    printf("Embedded opeartion is successful.");
    printf("\nTag ID  : %s\n", epcStr);

    if (0 < trd.data.len)
    {
      if (0x8000 == trd.data.len)
      {
        ret = TMR_translateErrorCode(GETU16AT(trd.data.list, 0));
        checkerr(rp, ret, 0, "Embedded tagOp failed:");
      }
      else
      {
        char dataStr[512];
        uint8_t dataLen = (trd.data.len / 8);

        TMR_bytesToHex(trd.data.list, dataLen, dataStr);
        printf("Data(%d): %s\n", dataLen, dataStr);
      }
    }
  }
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
#endif /* ENABLE_EMBEDDED_READ */

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
  TMR_Region region;
  uint8_t buffer[20];
  uint8_t i;
  uint8_t *antennaList = NULL;
  uint8_t antennaCount = 0x0;
  char string[100];
  TMR_String model;
#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif

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

#ifdef TMR_ENABLE_UHF
  //Use first antenna for operation
  if (NULL != antennaList)
  {
    ret = TMR_paramSet(rp, TMR_PARAM_TAGOP_ANTENNA, &antennaList[0]);
    checkerr(rp, ret, 1, "setting tagop antenna");  
  }

  {  
    uint8_t epcData[] = {
      0x01, 0x23, 0x45, 0x67, 0x89, 0xAB,
      0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67,
      };
    TMR_TagData epc;
    TMR_TagOp tagop;

	/* Set the tag EPC to a known value*/

    epc.epcByteCount = sizeof(epcData) / sizeof(epcData[0]);
    memcpy(epc.epc, epcData, epc.epcByteCount * sizeof(uint8_t));
    ret = TMR_TagOp_init_GEN2_WriteTag(&tagop, &epc);
    checkerr(rp, ret, 1, "initializing GEN2_WriteTag");

    ret = TMR_executeTagOp(rp, &tagop, NULL, NULL);
    checkerr(rp, ret, 1, "executing GEN2_WriteTag");

	{  /* Write Tag EPC with a select filter*/	  

	  uint8_t newEpcData[] = {
	    0xAB, 0xAB, 0xAB, 0xAB, 0xAB, 0xAB,
		};
	  TMR_TagFilter filter;
	  TMR_TagData newEpc;
	  TMR_TagOp newtagop;

	  newEpc.epcByteCount = sizeof(newEpcData) / sizeof(newEpcData[0]);
      memcpy(newEpc.epc, newEpcData, newEpc.epcByteCount * sizeof(uint8_t));
	  
	  /* Initialize the new tagop to write the new epc*/
	  
	  ret = TMR_TagOp_init_GEN2_WriteTag(&newtagop, &newEpc);
	  checkerr(rp, ret, 1, "initializing GEN2_WriteTag");

      /* Initialize the filter with the original epc of the tag which is set earlier*/
	  ret = TMR_TF_init_tag(&filter, &epc);
	  checkerr(rp, ret, 1, "initializing TMR_TagFilter");

	  /* Execute the tag operation Gen2 writeTag with select filter applied*/
	  ret = TMR_executeTagOp(rp, &newtagop, &filter, NULL);
	  checkerr(rp, ret, 1, "executing GEN2_WriteTag");
	}
  }

#if ENABLE_READ_AFTER_WRITE
  /* Reads data from a tag memory bank after writing data to the requested memory bank without powering down of tag */
  {
    TMR_TagFilter filter, *pfilter = &filter;
    uint8_t wordCount;
    /* Create individual write and read tagops */
    TMR_TagOp writeop, readop;
    TMR_TagOp* tagopArray[8];
    /* Create an tagop array(to store  write tagop followed by read tagop) and a tagopList */
    TMR_TagOp_List tagopList;
    TMR_TagOp listop;
    TMR_uint8List response;
    uint8_t responseData[16];
    TMR_uint16List writeArgs;
    char dataStr[128];
    TMR_ReadPlan plan;
#if ENABLE_FILTER
    uint8_t mask[2] = {0xAB, 0xAB};

    /* This select filter matches all Gen2 tags where bits 32-48 of the EPC are 0xABAB */
    TMR_TF_init_gen2_select(pfilter, false, TMR_GEN2_BANK_EPC, 32, 16, mask);
#else
    pfilter = NULL;
#endif
#ifdef ENABLE_EMBEDDED_READ
      TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
#endif /* ENABLE_EMBEDDED_READ */

    /* WriteData and ReadData */
    {
      /* Write one word of data to USER memory and read back 8 words from EPC memory */
      uint16_t writeData[] = { 0x1234 };
      wordCount = 8;
      writeArgs.list = writeData;
      writeArgs.len = writeArgs.max = sizeof(writeData)/sizeof(writeData[0]);

      /* Initialize the write and read tagop */
      TMR_TagOp_init_GEN2_WriteData(&writeop, TMR_GEN2_BANK_USER, 2, &writeArgs);
      checkerr(rp, ret, 1, "initializing GEN2_WriteData");

      TMR_TagOp_init_GEN2_ReadData(&readop, TMR_GEN2_BANK_EPC, 0, wordCount);
      checkerr(rp, ret, 1, "initializing GEN2_ReadData");

      /* Assemble tagops into list */

      tagopList.list = tagopArray;
      tagopList.len = 0;

      tagopArray[tagopList.len++] = &writeop;
      tagopArray[tagopList.len++] = &readop;

      /* Call executeTagOp with list of tagops and collect returned data (from last tagop) */

      listop.type = TMR_TAGOP_LIST;
      listop.u.list = tagopList;

      response.list = responseData;
      response.max = sizeof(responseData) / sizeof(responseData[0]);
      response.len = 0;

      ret = TMR_executeTagOp(rp, &listop, pfilter, &response);
      checkerr(rp, ret, 1, "executing GEN2_ReadAfterWrite");
      printf("ReadData after WriteData is successful.");
      TMR_bytesToHex(response.list, response.len, dataStr);
      printf("\nRead Data:%s,length : %d words\n", dataStr, response.len/2);

      // Enable embedded ReadAfterWrite operation by enabling macro ENABLE_EMBEDDED_READ
#if ENABLE_EMBEDDED_READ
        performEmbeddedOperation(rp, &plan, &listop, pfilter);
#endif /* ENABLE_EMBEDDED_READ */
    }
    /* Clear the tagopList for next operation */
    tagopList.list = NULL;

    /* WriteTag and ReadData */
    {
      TMR_TagData epc;
      /* Write 12 bytes(6 words) of EPC and read back 8 words from EPC memory */
      uint8_t epcData[] = { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66,0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC};
      wordCount = 8;
      /* Set the tag EPC to a known value */
      epc.epcByteCount = sizeof(epcData) / sizeof(epcData[0]);
      memcpy(epc.epc, epcData, epc.epcByteCount * sizeof(uint8_t));

      TMR_TagOp_init_GEN2_WriteTag(&writeop, &epc);
      checkerr(rp, ret, 1, "initializing GEN2_WriteTag");

      TMR_TagOp_init_GEN2_ReadData(&readop, TMR_GEN2_BANK_EPC, 0, wordCount);
      checkerr(rp, ret, 1, "initializing GEN2_ReadData");

      /* Assemble tagops into list */
      tagopList.list = tagopArray;
      tagopList.len = 0;

      tagopArray[tagopList.len++] = &writeop;
      tagopArray[tagopList.len++] = &readop;

      /* Call executeTagOp with list of tagops and collect returned data (from last tagop) */

      listop.type = TMR_TAGOP_LIST;
      listop.u.list = tagopList;

      response.list = responseData;
      response.max = sizeof(responseData) / sizeof(responseData[0]);
      response.len = 0;

      ret = TMR_executeTagOp(rp, &listop, pfilter, &response);
      checkerr(rp, ret, 1, "executing GEN2_ReadAfterWrite");
      printf("ReadData after WriteTag is successful.");
      TMR_bytesToHex(response.list, response.len, dataStr);
      printf("\nRead Data:%s,length : %d words\n", dataStr, response.len/2);

      // Enable embedded ReadAfterWrite operation by enabling macro ENABLE_EMBEDDED_READ
#if ENABLE_EMBEDDED_READ
      {
#if ENABLE_FILTER
       /* Here in stand-alone ReadAfterWrite operation EPC has been changed.
       * To read the same tag in Embedded ReadAfterWrite operation, filter has to replace by new written EPC.
       */
       uint16_t bitCount = (epc.epcByteCount*8);
       TMR_TF_init_gen2_select(pfilter, false, TMR_GEN2_BANK_EPC, 32, bitCount, epcData);
#endif /* ENABLE_FILTER */
        performEmbeddedOperation(rp, &plan, &listop, pfilter);
      }
#endif /* ENABLE_EMBEDDED_READ */
    }
  }
#endif
#endif /* TMR_ENABLE_UHF */
  }
  else
  {
#ifdef TMR_ENABLE_HF_LF
    TMR_ReadPlan plan;
    TMR_TagReadData trd;
    char epcStr[128];
    TMR_TagOp writeop, readop;
    uint32_t address;
    uint8_t dataLen;
    TMR_uint8List data;
    TMR_uint8List response;
    uint8_t responseData[255];
#if ENABLE_FILTER
    TMR_MultiFilter filterList;
    TMR_TagFilter uidSelect, tagtypeSelect, *filterArray[2];
#endif /* ENABLE_FILTER */
    TMR_TagFilter filter, *pfilter = &filter;
    uint8_t writeData[] = { 0x11, 0x22, 0x33, 0x44};
    
     /* Read Plan */
     // initialize the read plan

    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO15693, 1000);
    checkerr(rp, ret, 1, "initializing the  read plan");

    /* Commit read plan */
    ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
    checkerr(rp, ret, 1, "setting read plan");

    ret = TMR_read(rp, 500, NULL);
    if (TMR_ERROR_TAG_ID_BUFFER_FULL == ret) 
    { 
      /* In case of TAG ID Buffer Full, extract the tags present 
       * in buffer.
       */
       fprintf(stdout, "reading tags:%s\n", TMR_strerr(rp, ret)); 
    }
    else 
    {
      checkerr(rp, ret, 1, "reading tags"); 
    }

    if (TMR_SUCCESS == TMR_hasMoreTags(rp))
    {
      ret = TMR_getNextTag(rp, &trd); 
      checkerr(rp, ret, 1, "fetching tag"); 

      TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcStr); 
      printf("UID: %s\n", epcStr); 
      printf("TagType: 0x%08lx\n", (long unsigned int)trd.tagType);
    }

#if ENABLE_FILTER
    //Initialize filter
    filterList.tagFilterList = filterArray;
    filterList.len = 0;

    // Filters the tag based on tagType
    TMR_TF_init_tagtype_select(&tagtypeSelect, trd.tagType);
    // Filters the tag based on UID
    TMR_TF_init_uid_select(&uidSelect, (trd.tag.epcByteCount * 8), trd.tag.epc);

    /*Assemble two filters in filterArray*/
    filterArray[filterList.len++] = &tagtypeSelect;
    filterArray[filterList.len++] = &uidSelect;

    pfilter->type = TMR_FILTER_TYPE_MULTI;
    pfilter->u.multiFilterList = filterList;
#else
    pfilter = NULL;
#endif /* ENABLE_FILTER */

    address = 0;
    dataLen = 1;

    response.list = responseData;
    response.max = sizeof(responseData) / sizeof(responseData[0]);
    response.len = 0;

    //perform read data operation to read existing data before writing
    printf("\nRead the existing data before performing write\n");

    //  Initialize the read memory tagOp
    ret = TMR_TagOp_init_ReadMemory(&readop, TMR_TAGOP_TAG_MEMORY, address, dataLen);
    checkerr(rp, ret, 1, "creating read memory tagop");

    // Enable and set the password before execute tag op
#if ENABLE_SET_ACCESS_PASSWORD
    {
      TMR_uint8List *pAccessPW, accessPW;
      uint8_t key[4] = {0xFF, 0xFF, 0xFF, 0xFF};

      accessPW.list = key;
      accessPW.len  = accessPW.max = sizeof(key) / sizeof(key[0]);
      pAccessPW     = &accessPW;
    
      //Set password
      ret = TMR_set_accessPassword(&readop, pAccessPW);
      checkerr(rp, ret, 1, "setting access password");
	}
#endif

    // Perform the read memory standalone tag operation 
    ret = TMR_executeTagOp(rp, &readop, pfilter, &response);
    checkerr(rp, ret, 1, "executing read memory tagop");
    {
      int i;
      printf("Read Data:");
      for (i=0; i < response.len; i++)
      {
        printf(" %02X", response.list[i]);
      }
      printf("\n");
    }

    // Perform write data operation
    printf("\nPerforming write memory\n");

    data.list = writeData;
    data.max = data.len = sizeof(writeData) / sizeof(writeData[0]);

    //  Initialize the write memory tagOp
    ret = TMR_TagOp_init_WriteMemory(&writeop, TMR_TAGOP_TAG_MEMORY, address, &data);
    checkerr(rp, ret, 1, "creating write memory tagop");

    // Perform the write memory standalone tag operation 
    ret = TMR_executeTagOp(rp, &writeop, pfilter, NULL);
    checkerr(rp, ret, 1, "executing write memory tagop");

    //Verify the written data
    printf("\nVerify the written data in the writeMemory operation\n");

    //  Initialize the read memory tagOp
    ret = TMR_TagOp_init_ReadMemory(&readop, TMR_TAGOP_TAG_MEMORY, address, dataLen);
    checkerr(rp, ret, 1, "creating read memory tagop");

    // Perform the read memory standalone tag operation 
    ret = TMR_executeTagOp(rp, &readop, pfilter, &response);
    checkerr(rp, ret, 1, "executing read memory tagop");

    {
      int i;
      printf("Verify Read Data:");
      for (i=0; i < response.len; i++)
      {
        printf(" %02X", response.list[i]);
      }
      printf("\n");
    }

    // Enable embedded Read operation by enabling macro ENABLE_EMBEDDED_READ
#if ENABLE_EMBEDDED_READ
      performEmbeddedOperation(rp, &plan, &readop, pfilter);
#endif /* ENABLE_EMBEDDED_READ */

#if ENABLE_SYSTEM_INFORMATION_MEMORY
    /** 
     * Get the system information of the tag 
     * Address and length fields have no significance if memory type is TMR_TAGOP_BLOCK_SYSTEM_INFORMATION_MEMORY.
     */
    {
#define CONFIGURATION_BLOCK_ADDRESS 0
#define CONFIGURATION_BLOCK_NUM 0
      //  Initialize the read memory tagOp
      ret = TMR_TagOp_init_ReadMemory(&readop, TMR_TAGOP_TAG_INFO,
                                      CONFIGURATION_BLOCK_ADDRESS, CONFIGURATION_BLOCK_NUM);
      checkerr(rp, ret, 1, "creating system information tagop");

      // Perform the read memory standalone tag operation
      /* Make sure to provide enough response buffer - 'response.list'
       * If the provided response buffer size is less than the number
       * of bytes requested to read, then the operation will result in
       * TMR_ERROR_OUT_OF_MEMORY error.
       */
      ret = TMR_executeTagOp(rp, &readop, pfilter, &response);
      checkerr(rp, ret, 1, "executing system information tagop");

      if(response.len)
      {
        // parsing the system info response
        parseGetSystemInfoResponse(response.list);
      }
    }
#endif /* ENABLE_SYSTEM_INFORMATION_MEMORY */

#if ENABLE_BLOCK_PROTECTION_STATUS
    /* Get the block protection status of block 0 */
    {
      address = 0;
      dataLen = 1;

      //  Initialize the read memory tagOp
      ret = TMR_TagOp_init_ReadMemory(&readop, TMR_TAGOP_PROTECTION_SECURITY_STATUS, address, dataLen);
      checkerr(rp, ret, 1, "creating Get block protection status tagop");

      // Perform the read memory standalone tag operation
      ret = TMR_executeTagOp(rp, &readop, pfilter, &response);
      checkerr(rp, ret, 1, "executing Get block protection status tagop");

      // parsing the protection status response
      if (response.len == dataLen)
      {
        parseBlockProtectionStatusResponse(response.list, address, dataLen);
      }
    }
#endif /* ENABLE_BLOCK_PROTECTION_STATUS */

#if ENABLE_SECURE_ID_EMBEDDED_READ
    /* Read secure id of tag. Address and length fields have no significance if memory type is SECURE_ID. */
    {
      address = 0;
      dataLen = 0;

      //  Initialize the read memory tagOp
      ret = TMR_TagOp_init_ReadMemory(&readop, TMR_TAGOP_SECURE_ID, address, dataLen);
      checkerr(rp, ret, 1, "creating Secure read ID tagop");

      // Perform embedded read operation for Secure read as standalone is unsupported for it.
      performEmbeddedOperation(rp, &plan, &readop, pfilter);
    }
#endif /* ENABLE_SECURE_ID_EMBEDDED_READ */
#endif /* TMR_ENABLE_HF_LF */
  }

  TMR_destroy(rp);
  return 0;
}
