/**
 * Sample program that demonstrates different types and uses of TagFilter objects.
 * @file filter.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <tmr_utils.h>
#include <string.h>
#include <inttypes.h>

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

void readAndPrintTags(TMR_Reader *rp, int timeout)
{
  TMR_Status ret;
  TMR_TagReadData trd;
  char epcString[128];

  ret = TMR_read(rp, timeout, NULL);
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

  while (TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    ret = TMR_getNextTag(rp, &trd);
    checkerr(rp, ret, 1, "fetching tag");
    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcString);
    printf("EPC : %s\n", epcString);
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

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
#ifdef TMR_ENABLE_UHF
  TMR_Region region;
  TMR_TagReadData trd, Temptrd;
  TMR_TagFilter filter;
  TMR_ReadPlan filteredReadPlan;
  TMR_TagOp tagop;
  char epcString[128];
  uint8_t mask[2];
  uint8_t tmask[4];
  uint8_t count = 0;
#endif /* TMR_ENABLE_UHF */
  uint8_t *antennaList = NULL;
  uint8_t buffer[20];
  uint8_t i;
  uint8_t antennaCount = 0x0;
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

#ifdef TMR_ENABLE_UHF
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

  // initialize the read plan
  ret = TMR_RP_init_simple(&filteredReadPlan, antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
  checkerr(rp, ret, 1, "initializing the  read plan");
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &filteredReadPlan);
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
  ret = TMR_hasMoreTags(rp);
  if(TMR_ERROR_NO_TAGS == ret)
  {
    errx(1, "No tags found for test\n");
  } 
  else
  {
    do{
        ret = TMR_getNextTag(rp, &trd);
        checkerr(rp, ret, 1, "getting tags");
        if(count == 0)
        {
          memcpy(&Temptrd, &trd, sizeof(trd));
          count++;
        }
        TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcString);
        printf("Tag Read : %s\n", epcString);
      }while(TMR_SUCCESS==TMR_hasMoreTags(rp));
  }

  /*
   * A TagData object may be used as a filter, for example to
   * perform a tag data operation on a particular tag.
   * Read kill password of tag found in previous operation
   */
  TMR_TF_init_tag(&filter, &Temptrd.tag);
  {
    TMR_uint8List response;
    uint8_t responseData[16];
    char dataStr[128];
    response.list = responseData;
    response.max = sizeof(responseData) / sizeof(responseData[0]);
    response.len = 0;

    TMR_bytesToHex(filter.u.tagData.epc, filter.u.tagData.epcByteCount,epcString);
    printf("Read kill password of tag that begin with %s\n",epcString);
    TMR_TagOp_init_GEN2_ReadData(&tagop,TMR_GEN2_BANK_RESERVED,0,2);
    ret = TMR_executeTagOp(rp,&tagop,&filter,&response);
    if(ret != TMR_SUCCESS)
      printf("Error reading kill Password\n");
    else
    {
      printf("Read Kill-Password successful.");
      TMR_bytesToHex(response.list, response.len, dataStr);
      printf("\nRead Data:%s,length : %d words\n", dataStr, response.len/2);
    }
  }
      
  /*
   * A TagData with a short EPC will filter for tags whose EPC
   * starts with the same sequence.
   */
  filter.type = TMR_FILTER_TYPE_TAG_DATA;
  filter.u.tagData.epcByteCount = 4;
  tm_memcpy(filter.u.tagData.epc, Temptrd.tag.epc, (size_t)filter.u.tagData.epcByteCount);
  
  TMR_RP_init_simple(&filteredReadPlan,
                     antennaCount, antennaList, TMR_TAG_PROTOCOL_GEN2, 1000);
  TMR_RP_set_filter(&filteredReadPlan, &filter);

  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &filteredReadPlan);
  checkerr(rp, ret, 1, "setting read plan");

  TMR_bytesToHex(filter.u.tagData.epc, filter.u.tagData.epcByteCount,
                 epcString);
  printf("Reading tags that begin with %.*s\n", filter.u.tagData.epcByteCount, epcString);
  readAndPrintTags(rp, 500);

  /*
   * A filter can also be an explicit Gen2 Select operation.  For
   * example, this filter matches all Gen2 tags where bits 8-19 of
   * the TID are 0x30 (that is, tags manufactured by Alien
   * Technology).
   */
  // In case of Network readers, ensure that bitLength is a multiple of 8
  mask[0] = 0x00;
  mask[1] = 0x30;
  TMR_TF_init_gen2_select(&filter, false, TMR_GEN2_BANK_TID, 8, 12, mask);
  /*
   * filteredReadPlan already points to filter, and
   * "/reader/read/plan" already points to filteredReadPlan.
   * However, we need to set it again in case the reader has 
   * saved internal state based on the read plan.
   */
  ret = TMR_paramSet(rp, TMR_paramID("/reader/read/plan"), &filteredReadPlan);
  checkerr(rp, ret, 1, "setting read plan");
  printf("Reading tags with a TID manufacturer of 0x0030\n");
  readAndPrintTags(rp, 500);

	if (rp->readerType == TMR_READER_TYPE_SERIAL)
  {
		/*
		 * A filter can also perform Gen2 Truncate Select operation. 
		 * Truncate indicates whether a Tag\92s backscattered reply shall be truncated to those EPC bits that follow Mask.
		 * For example, truncated select starting with PC word start address and length of 16 bits
		 */
		// In case of Network readers, ensure that bitLength is a multiple of 8
		tmask[0] = 0x30;
		tmask[1] = 0x00;
		tmask[2] = 0xDE;
		tmask[3] = 0xAD;
		TMR_TF_init_gen2_select(&filter, false, TMR_GEN2_EPC_TRUNCATE, 16, 32, tmask);
		/*
		 * filteredReadPlan already points to filter, and
		 * "/reader/read/plan" already points to filteredReadPlan.
		 * However, we need to set it again in case the reader has 
		 * saved internal state based on the read plan.
		 */
		ret = TMR_paramSet(rp, TMR_paramID("/reader/read/plan"), &filteredReadPlan);
		checkerr(rp, ret, 1, "setting read plan");
		printf("Gen2 Truncate operation to those EPC that follows the mask\n");
		readAndPrintTags(rp, 500);

		/*
		 * A filter can also perform Gen2 Tag Filtering.
		 * Major advantage of this feature is to limit the EPC response to user specified length field and all others will be rejected by firmware.
		 * invert, bitPointer, mask : Parameters will be ignored when TMR_GEN2_EPC_LENGTH_FILTER is used
		 * maskBitLength : Specified EPC Length used for filtering
		 * For example, Tag filtering will be applied on EPC with 128 bits length, rest of the tags will be ignored
		 */
		// In case of Network readers, ensure that bitLength is a multiple of 8
		mask[0] = 0x30;
		mask[1] = 0x00;
		TMR_TF_init_gen2_select(&filter, false, TMR_GEN2_EPC_LENGTH_FILTER, 16, 128, mask);
		/*
		 * filteredReadPlan already points to filter, and
		 * "/reader/read/plan" already points to filteredReadPlan.
		 * However, we need to set it again in case the reader has 
		 * saved internal state based on the read plan.
		 */
		ret = TMR_paramSet(rp, TMR_paramID("/reader/read/plan"), &filteredReadPlan);
		checkerr(rp, ret, 1, "setting read plan");
		printf("Filtering Tag with EPC Length of %d bits\n", filter.u.gen2Select.maskBitLength);
		readAndPrintTags(rp, 500);
	}

  /*
   * Filters can also be used to match tags that have already been
   * read. This form can only match on the EPC, as that's the only
   * data from the tag's memory that is contained in a TagData
   * object.
   * Note that this filter has invert=true. This filter will match
   * tags whose bits do not match the selection mask.
   * Also note the offset - the EPC code starts at bit 32 of the
   * EPC memory bank, after the StoredCRC and StoredPC.
   */
  // In case of Network readers, ensure that bitLength is a multiple of 8
  TMR_TF_init_gen2_select(&filter, true, TMR_GEN2_BANK_EPC, 32, 2, mask);

  printf("Reading tags with EPC's having first two bytes equal to zero (post-filtered):\n");
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

  while (TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    ret = TMR_getNextTag(rp, &trd);
    checkerr(rp, ret, 1, "fetching tag");
    if (TMR_TF_match(&filter, &trd.tag))
    {
      TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, epcString);
      printf("%s\n", epcString);
    }
  }
  {
    /**
     * Multi filter creation and initialization. This filter will match those tags whose bits matches the
     * selection mask of both tidFilter(tags manufactured by Alien Technology) and epcFilter.
     * Target and action are the two new parameters of TMR_GEN2_Select structure whose
     * default values could be "gen2Select.target =  SELECT" and "gen2Select.action = ON_N_OFF" respectively
     * if not provided by the user.
     *******************************************************************************
     TMR_GEN2_Select_action indicates which Select action to take
     (See Gen2 spec /Select commands / Tag response to Action parameter)
     |-----------------------------------------------------------------------------|
     |  Action  |        Tag Matching            |        Tag Not-Matching         |
     |----------|--------------------------------|---------------------------------|
     |   0x00   |  Assert SL or Inventoried->A   |  Deassert SL or Inventoried->B  |
     |   0x01   |  Assert SL or Inventoried->A   |  Do nothing                     |
     |   0x02   |  Do nothing                    |  Deassert SL or Inventoried->B  |
     |   0x03   |  Negate SL or (A->B,B->A)      |  Do nothing                     |
     |   0x04   |  Deassert SL or Inventoried->B |  Assert SL or Inventoried->A    |
     |   0x05   |  Deassert SL or Inventoried->B |  Do nothing                     |
     |   0x06   |  Do nothing                    |  Assert SL or Inventoried->A    |
     |   0x07   |  Do nothing                    |  Negate SL or (A->B,B->A)       |
     ********************************************************************************
     *
     *  To improve readability and ease of typing, these names abbreviate the official terminology of the Gen2 spec.
     *  <A>_N_<B>: The "_N_" stands for "Non-Matching".
     *  The <A> clause before the N describes what happens to Matching tags.
     *  The <B> clause after the N describes what happens to Non-Matching tags.
     *  (Alternately, you can pronounce "_N_" as "and", or "&"; i.e.,
     *  the pair of Matching / Non-Matching actions is known as "<A> and <B>".)
     *
     *  ON: assert SL or inventoried -> A
     *  OFF: deassert SL or inventoried -> B
     *  NEG: negate SL or (A->B, B->A)
     *  NOP: do nothing
     *
     *  The enum is simply a transliteration of the Gen2 spec's table: "Tag response to Action parameter"
     */
     TMR_TagFilter tidFilter, epcFilter, *filterArray[2];
     TMR_MultiFilter filterList;
 
     filterList.tagFilterList = filterArray;
     filterList.len = 0;
     {
       /* This select filter matches all Gen2 tags where bits 08-12 of the TID are 0x0030(that is, tags manufactured
        * by Alien Technology). */ 
	   // In case of Network readers, ensure that bitLength is a multiple of 8
       mask[0] = 0x00;
       mask[1] = 0x30;
       TMR_TF_init_gen2_select(&tidFilter, false, TMR_GEN2_BANK_TID, 8, 12, mask);
       tidFilter.u.gen2Select.target =  SELECT;
       tidFilter.u.gen2Select.action =  ON_N_OFF;
     }
     {
       /* This select filter matches all Gen2 tags whose EPC starts with the same sequence */
	   // In case of Network readers, ensure that bitLength is a multiple of 8.
       TMR_TF_init_gen2_select(&epcFilter, false, TMR_GEN2_BANK_EPC, 32, 16, trd.tag.epc);
       epcFilter.u.gen2Select.target =  SELECT;
       epcFilter.u.gen2Select.action =  NOP_N_OFF;
     }
     /*Assemble two filters in filterArray*/
     filterArray[filterList.len++] = &tidFilter;
     filterArray[filterList.len++] = &epcFilter;

     /* Assign TMR_GEN2_MultiSelect filter to TMR_TagFilter filter */
     filter.type = TMR_FILTER_TYPE_MULTI;
     filter.u.multiFilterList = filterList;
     printf("Reading tags with double select \n");
     readAndPrintTags(rp, 500);
  }
#endif /* TMR_ENABLE_UHF */
  TMR_destroy(rp);
  return 0;
}
