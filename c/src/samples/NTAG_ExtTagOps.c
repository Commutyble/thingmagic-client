/**
 * Sample program that reads tags for a fixed period of time (500ms)
 * and performs Extended tag operation on the tag found.
 * @file NTAG_ExtTagOps.c
 */
#include <serial_reader_imp.h>
#include <tm_reader.h>
#include <stdio.h>
#include <stdlib.h>
#include <inttypes.h>
#include <tmr_utils.h>

/**
* Baremetal or Embedded Applications?
*/
#ifdef BARE_METAL
#define printf(...) {}
#else
/**@def USE_TRANSPORT_LISTENER
* Enable this macro to turn ON the serial communication data logs.
*/
#ifndef USE_TRANSPORT_LISTENER
#define USE_TRANSPORT_LISTENER                  0
#endif
  
/**@def PRINT_TAG_METADATA
* Enable this macro to display additional information like Antena ID, Protocol, etc. along with tag data.
*/
#define PRINT_TAG_METADATA                      0
#define numberof(x) (sizeof((x))/sizeof((x)[0]))
  
/**@def usage
* Enable this function to display correct usage when there is any error in the command line inputs.
*/
#define usage() {errx(1, "Please provide valid reader URL, such as: reader-uri [--ant n] [--pow read_power]\n"\
                         "reader-uri : e.g., 'tmr:///COM1' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'\n"\
                         "[--ant n] : e.g., '--ant 1'\n"\
                         "[--pow read_power] : e.g, '--pow 2300'\n"\
                         "Example for UHF modules: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' or 'tmr:///com4 --ant 1,2 --pow 2300'\n"\
                         "Example for HF/LF modules: 'tmr:///com4' \n");}

/**
* Prints the error message passed in the argument.
*
* @param exitval    Error code from API
* @param fmt        Error message
*/
void errx(int exitval, const char* fmt, ...)
{
    va_list ap;

    va_start(ap, fmt);
    vfprintf(stderr, fmt, ap);

    exit(exitval);
}
#endif /* BARE_METAL */

/**
* Displays a short description of the error received from API.
*
* @param rp      Pointer to the reader object
* @param ret     Error code from API
* @param exitVal Error status
* @param msg     Error message
*/
void checkerr(TMR_Reader* rp, TMR_Status ret, int exitval, const char* msg)
{
#ifndef BARE_METAL
    if (TMR_SUCCESS != ret)
    {
        errx(exitval, "Error %s: %s\n", msg, TMR_strerr(rp, ret));
    }
#endif /* BARE_METAL */
}

#ifdef USE_TRANSPORT_LISTENER
/**
* This function is used to print serial communication data.
*
* @param tx      Indicates transmitted data or received data
* @param dataLen Number of data bytes sent or received
* @param data    Data to be printed
* @param timeout Time out value
* @param cookie  Context value to callback functions
*/
void serialPrinter(bool tx, uint32_t dataLen, const uint8_t data[],
    uint32_t timeout, void* cookie)
{
    FILE* out = cookie;
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

/**
* This function is used to print serial communication data.
*
* @param tx      Indicates transmitted data or received data
* @param dataLen Number of data bytes sent or received
* @param data    Data to be printed
* @param timeout Time out value
* @param cookie  Context value to callback functions
*/
void stringPrinter(bool tx, uint32_t dataLen, const uint8_t data[], uint32_t timeout, void* cookie)
{
    FILE* out = cookie;

    fprintf(out, "%s", tx ? "Sending: " : "Received:");
    fprintf(out, "%s\n", data);
}
#endif /* USE_TRANSPORT_LISTENER */

/**@def ENABLE_SECURE_RDWR
* Enable this macro to perform the secure read/write - The firmware sends
* "PWD_AUTH" command to the tag before performing any tag operation.
*/
#define ENABLE_SECURE_RDWR              1

/**@def ENABLE_TAG_MEM_INFO
* Enable this macro to get version information from the tag. The firmware
* sends "GET_VERSION" command to the tag.
* This is used to find the exact tag type and figure out the memory layout
* of the tag.
*/
#define ENABLE_TAG_MEM_INFO             0

/**@def TAG_MEM_RDWR_ADDR
* This macro is used to configure the page address for performing the 
* read/write tag operation on the tag.
* It is configured to Page 4 - The user memory starts from this page.
*/
#define TAG_MEM_RDWR_ADDR               4

/**@def NUM_BLKS
* This macro is used to configure the number of blocks/pages to be read
* from tag memory or the number of blocks/pages to be written to the
* tag memory. It is currently configured to a single block/page.
*/
#define NUM_PAGES                       1

#ifdef ENABLE_TAG_MEM_INFO
//Version number of NTAG/UL.
#define MIFARE_UL_EV1_MF0UL11           0x0B
#define MIFARE_UL_EV1_MF0UL21           0x0E
#define MIFARE_NTAG_210                 0x0B
#define MIFARE_NTAG_212                 0x0E
#define MIFARE_NTAG_213                 0x0F
#define MIFARE_NTAG_215                 0x11
#define MIFARE_NTAG_216                 0x13
#define TMR_ERROR_INVALID_TAG_TYPE      0x412

//API tag validation definition for NTAG/UL.
#define API_UL_NTAG_UNKNOWN             0x00
#define API_UL_EV1_MF0UL11              0x01
#define API_UL_EV1_MF0UL21              0x02
#define API_NTAG_210                    0x03
#define API_NTAG_212                    0x04
#define API_NTAG_213                    0x05
#define API_NTAG_215                    0x06
#define API_NTAG_216                    0x07
#define API_ULC_NTAG_203                0x08

/*
 * Ultraligt/NTAG "Manufacturer data and lock bytes" related defines.
 *
 * MIFARE_UL_EV1_MF0UL11                //MFG DATA: 0x00 - 0x03
 * MIFARE_UL_EV1_MF0UL21                //MFG DATA: 0x00 - 0x03
 * MIFARE_NTAG_210                      //MFG DATA: 0x00 - 0x03
 * MIFARE_NTAG_212                      //MFG DATA: 0x00 - 0x03
 * MIFARE_NTAG_213                      //MFG DATA: 0x00 - 0x03
 * MIFARE_NTAG_215                      //MFG DATA: 0x00 - 0x03
 * MIFARE_NTAG_216                      //MFG DATA: 0x00 - 0x03
 */
#define UL_NTAG_MEM_BEGIN               0x00  //Page No.
#define UL_NTAG_MFG_LCKBYTES_BEGIN      UL_NTAG_MEM_BEGIN
#define UL_NTAG_MFG_LCKBYTES_LEN        0x03  //Pages
#define UL_NTAG_MFG_LCKBYTES_END        UL_NTAG_MEM_BEGIN + UL_NTAG_MFG_LCKBYTES_LEN    //Page No. 


#define UL_NTAG_MFG_UID_MEM_BEGIN       UL_NTAG_MFG_LCKBYTES_BEGIN                      //Page No
#define UL_NTAG_MFG_UID_LEN             0x02                                            //Pages
#define UL_NTAG_LCKBYTES_BEGIN          UL_NTAG_MFG_LCKBYTES_BEGIN + UL_NTAG_MFG_UID_LEN//Page No

 /*
  * Ultraligt/NTAG "OTP/Capability Container" defines.
  *
  * MIFARE_UL_EV1_MF0UL11               //OTP MEM: 0x03
  * MIFARE_UL_EV1_MF0UL21               //OTP MEM: 0x04
  * MIFARE_NTAG_210                     //CAPABILITY_CONTAINER MEM: 0x03
  * MIFARE_NTAG_212                     //CAPABILITY_CONTAINER MEM: 0x03
  * MIFARE_NTAG_213                     //CAPABILITY_CONTAINER MEM: 0x03
  * MIFARE_NTAG_215                     //CAPABILITY_CONTAINER MEM: 0x03
  * MIFARE_NTAG_216                     //CAPABILITY_CONTAINER MEM: 0x03
  */
#define UL_NTAG_OTP_CC_MEM_BEGIN        0x03   //Page No.
#define UL_NTAG_OTP_CC_MEM_LEN          0x01   //Page
#define UL_NTAG_OTP_CC_MEM_END          0x04   //Page No.

  /*
   * Ultraligt/NTAG "user memory" defines.
   *
   * MIFARE_UL_EV1_MF0UL11              //USER MEM: 0x04 - 0x0F
   * MIFARE_UL_EV1_MF0UL21              //USER MEM: 0x04 - 0x23
   * MIFARE_NTAG_210                    //USER MEM: 0x04 - 0x0F
   * MIFARE_NTAG_212                    //USER MEM: 0x04 - 0x23
   * MIFARE_NTAG_213                    //USER MEM: 0x04 - 0x27
   * MIFARE_NTAG_215                    //USER MEM: 0x04 - 0x81
   * MIFARE_NTAG_216                    //USER MEM: 0x04 - 0xE1
   */
#define UL_NTAG_USER_MEM_BEGIN          0x04   //Page No.

#define UL_EV1_MF0UL11_USER_MEM_END     0x0F
#define UL_EV1_MF0UL11_USER_MEM_LEN     UL_EV1_MF0UL11_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define UL_EV1_MF0UL21_USER_MEM_END     0x23
#define UL_EV1_MF0UL21_USER_MEM_LEN     UL_EV1_MF0UL21_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define NTAG_210_USER_MEM_END           0x0F
#define NTAG_210_USER_MEM_LEN           NTAG_210_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define NTAG_212_USER_MEM_END           0x23
#define NTAG_212_USER_MEM_LEN           NTAG_212_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define NTAG_213_USER_MEM_END           0x27
#define NTAG_213_USER_MEM_LEN           NTAG_213_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define NTAG_215_USER_MEM_END           0x81
#define NTAG_215_USER_MEM_LEN           NTAG_215_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

#define NTAG_216_USER_MEM_END           0xE1
#define NTAG_216_USER_MEM_LEN           NTAG_216_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN

   /*
    * Ultraligt/NTAG "configuration memory" defines.
    *
    * MIFARE_UL_EV1_MF0UL11             //CFG MEM: 0x10 - 0x13
    * MIFARE_UL_EV1_MF0UL21             //CFG MEM: 0x25 - 0x28
    * MIFARE_NTAG_210                   //CFG MEM: 0x10 - 0x13
    * MIFARE_NTAG_212                   //CFG MEM: 0x25 - 0x28
    * MIFARE_NTAG_213                   //CFG MEM: 0x29 - 0x2C
    * MIFARE_NTAG_215                   //CFG MEM: 0x83 - 0x86
    * MIFARE_NTAG_216                   //CFG MEM: 0xE3 - 0xE6
    */
#define UL_EV1_MF0UL11_CFG_MEM_BEGIN    0x10
#define UL_EV1_MF0UL11_CFG_MEM_END      0x13

#define UL_EV1_MF0UL21_CFG_MEM_BEGIN    0x25
#define UL_EV1_MF0UL21_CFG_MEM_END      0x28

#define NTAG_210_CFG_MEM_BEGIN          0x10
#define NTAG_210_CFG_MEM_END            0x13

#define NTAG_212_CFG_MEM_BEGIN          0x25
#define NTAG_212_CFG_MEM_END            0x28

#define NTAG_213_CFG_MEM_BEGIN          0x29
#define NTAG_213_CFG_MEM_END            0x2C

#define NTAG_215_CFG_MEM_BEGIN          0x83
#define NTAG_215_CFG_MEM_END            0x86

#define NTAG_216_CFG_MEM_BEGIN          0xE3
#define NTAG_216_CFG_MEM_END            0xE6

#define UL_NTAG_CFG_LEN                 0x04        //Pages

    //Capability Container related defines.
#define CAPABILITY_CONTAINER_PAGE       0x03        //Page No.
#define CAPABILITY_CONTAINER_LEN        0x01        //Page

#define NTAG_210_CC_VAL                 0x06
#define NTAG_212_CC_VAL                 0x10
#define NTAG_213_CC_VAL                 0x12
#define NTAG_215_CC_VAL                 0x3E
#define NTAG_216_CC_VAL                 0x6D
#endif  //ENABLE_TAG_MEM_INFO

//Function Declarations
#ifdef ENABLE_TAG_MEM_INFO
TMR_Status  isUsrMem_NTAG_UL(uint8_t* tagFound, uint32_t* address, uint8_t* len);
TMR_Status  isCfgMem_NTAG_UL(uint8_t* tagFound, uint32_t* address, uint8_t* len);
void        GetMemInfo(uint8_t tagFound, uint32_t* address, uint8_t* len);
uint8_t     GetTagInfo_NTAG_UL(TMR_Reader* rp, TMR_TagFilter* filter, TMR_uint8List* response);
#endif  //ENABLE_TAG_MEM_INFO

/**
* This function is used to initiate read operation from the tag memory.
*
* @param rp        Pointer to the reader object.
* @param address   Reader starts reading data present in the tag memory from this page.
* @param len       Number of pages to read.
* @param filter    Type of filter to be enabled.
* @param response  Pointer to the response bytes received from the reader.
*/
TMR_Status  TMR_ReadTagMemory(TMR_Reader* rp, uint32_t* address, uint8_t* len, TMR_TagFilter* filter, TMR_uint8List* response);

/**
* This function is used to initiate write operation to the tag memory.
*
* @param rp        Pointer to the reader object.
* @param address   Reader starts writing the data in the tag memory from this page.
* @param data      Pointer to the data to be written in the tag memory.
* @param filter    Type of filter to be enabled.
* @param response  Pointer to the response bytes received from the reader.
*/
TMR_Status  TMR_WriteTagMemory(TMR_Reader* rp, uint32_t* address, TMR_uint8List* data, TMR_TagFilter* filter, TMR_uint8List* response);

// Global Variables
uint8_t         wrTagData[] = { 0x11, 0x22, 0x33, 0x44 };       /** Buffer holding the data to be written in the tag memory */
uint8_t         password[4] = { 0xFF, 0xFF, 0xFF, 0xFF };       /** Buffer holding the password */

TMR_uint8List*  pAccPW, accPw;                                  /** Pointer to the access passwords */

/**
* Main function.
*
* @param argc   Number of command line arguments.
* @param argv   Pointer to the command line variables.
*/
int main(int argc, char* argv[])
{
    TMR_Reader r, * rp;
    TMR_Status ret;
    TMR_ReadPlan plan;
    uint8_t* antennaList = NULL;
    uint8_t antennaCount = 0x0;
    TMR_TRD_MetadataFlag metadata = TMR_TRD_METADATA_FLAG_ALL;
    char string[100] = {0};
    TMR_String model;

#if USE_TRANSPORT_LISTENER
    TMR_TransportListenerBlock tb;
#endif /* USE_TRANSPORT_LISTENER */
    rp = &r;

#ifndef BARE_METAL
    printf("/*******************************************\n");
    printf(" *NTAG Extended Tag Operations Code Sample* \n");
    printf("*******************************************/\n");

    if (argc < 2)
    {
        fprintf(stdout, "Not enough arguments.  Please provide reader URL.\n");
        usage();
    }

    ret = TMR_create(rp, argv[1]);
    checkerr(rp, ret, 1, "creating reader");
#else
    ret = TMR_create(rp, "tmr:///com1");

#endif /* BARE_METAL */

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
#endif /* USE_TRANSPORT_LISTENER */

    ret = TMR_connect(rp);
    /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
     * The connection may fail if the module is configured to a different baud rate. If
     * that is the case, the MercuryAPI tries connecting to the module with other supported
     * baud rates until the connection is successful using baud rate probing mechanism.
     */
    if ((ret == TMR_ERROR_TIMEOUT) &&
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

    model.value = string;
    model.max = sizeof(string);
    TMR_paramGet(rp, TMR_PARAM_VERSION_MODEL, &model);
    checkerr(rp, ret, 1, "Getting version model");

    if (0 != strcmp("M3e", model.value))
    {
        ret = TMR_ERROR_UNSUPPORTED_READER_TYPE;
        checkerr(rp, ret, 1, "Checking Reader");
    }

    // Set the metadata flags.
    // Protocol is a mandatory metadata flag. It cannot be disabled.
    ret = TMR_paramSet(rp, TMR_PARAM_METADATAFLAG, &metadata);
    checkerr(rp, ret, 1, "Setting Metadata Flags");

    /**
    * for antenna configuration we need two parameters
    * 1. antennaCount : specifies the no of antennas should
    *    be included in the read plan, out of the provided antenna list.
    * 2. antennaList  : specifies  a list of antennas for the read plan.
    **/
    // initialize the read plan
    ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO14443A, 1000);
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
#ifndef BARE_METAL
        fprintf(stdout, "reading tags:%s\n", TMR_strerr(rp, ret));
#endif /* BARE_METAL */
    }
    else
    {
        checkerr(rp, ret, 1, "reading tags");
    }

    while (TMR_SUCCESS == TMR_hasMoreTags(rp))
    {
        TMR_TagReadData trd;
        char idStr[128];
#ifndef BARE_METAL
        char timeStr[128];
#endif /* BARE_METAL */

        ret = TMR_getNextTag(rp, &trd);
        checkerr(rp, ret, 1, "fetching tag");

        TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, idStr);

#ifndef BARE_METAL
        TMR_getTimeStamp(rp, &trd, timeStr);
        printf("Tag ID: %s ", idStr);

        // Enable PRINT_TAG_METADATA Flags to print Metadata value
#if PRINT_TAG_METADATA
        {
            uint16_t j = 0;

            printf("\n");
            for (j = 0; (1 << j) <= TMR_TRD_METADATA_FLAG_MAX; j++)
            {
                if ((TMR_TRD_MetadataFlag)trd.metadataFlags & (1 << j))
                {
                    switch ((TMR_TRD_MetadataFlag)trd.metadataFlags & (1 << j))
                    {
                    case TMR_TRD_METADATA_FLAG_READCOUNT:
                        printf("Read Count: %d\n", trd.readCount);
                        break;
                    case TMR_TRD_METADATA_FLAG_ANTENNAID:
                        printf("Antenna ID: %d\n", trd.antenna);
                        break;
                    case TMR_TRD_METADATA_FLAG_TIMESTAMP:
                        printf("Timestamp: %s\n", timeStr);
                        break;
                    case TMR_TRD_METADATA_FLAG_PROTOCOL:
                        printf("Protocol: %d\n", trd.tag.protocol);
                        break;
                    case TMR_TRD_METADATA_FLAG_DATA:
                    {
                        //TODO : Initialize Read Data
                        if (0 < trd.data.len)
                        {
                            if (0x8000 == trd.data.len)
                            {
                                ret = TMR_translateErrorCode(GETU16AT(trd.data.list, 0));
                                checkerr(rp, ret, 0, "Embedded tagOp failed:");
                            }
                            else
                            {
                                char dataStr[255];
                                uint8_t dataLen = (trd.data.len / 8);

                                TMR_bytesToHex(trd.data.list, dataLen, dataStr);
                                printf("Data(%d): %s\n", dataLen, dataStr);
                            }
                        }
                    }
                    break;
                    case TMR_TRD_METADATA_FLAG_TAGTYPE:
                    {
                        printf("TagType: 0x%08lx\n", trd.tagType);
                        break;
                    }
                    default:
                        break;
                    }
                }
            }
        }
#endif

        /* Tag Operations */
        if (trd.tagType == TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG)
        {
            uint32_t        address = TAG_MEM_RDWR_ADDR;
            uint8_t         dataBlockLen = NUM_PAGES;
            uint8_t         responseData[256] = {0};

            TMR_uint8List   response, data;
            TMR_MultiFilter filterList;
            TMR_TagFilter   filter, * pfilter = &filter;
            TMR_TagFilter   uidSelect, tagtypeSelect, * filterArray[2];

            //Number of blocks
            (dataBlockLen <= 0) ? (dataBlockLen = 1) : (dataBlockLen = NUM_PAGES);

            /* Initialize filter. */
            filterList.tagFilterList = filterArray;
            filterList.len = 0;

            // Initialize response object
            response.list = responseData;
            response.max = response.len = sizeof(responseData) / sizeof(responseData[0]);
            //response.len = 0;

            // Initialize data object
            data.list = wrTagData;
            data.len = data.max = sizeof(wrTagData) / sizeof(wrTagData[0]);

            // Set Access Password
            pAccPW = NULL;
            accPw.list = password;
            accPw.len = accPw.max = sizeof(password) / sizeof(password[0]);
            pAccPW = &accPw;

            /** Initialize tag type filter. */
            TMR_TF_init_tagtype_select(&tagtypeSelect, trd.tagType);
            /** Initialize UID filter. */
            TMR_TF_init_uid_select(&uidSelect, (trd.tag.epcByteCount * 8), trd.tag.epc);

            /* Assemble filters in filterArray. */
            filterArray[filterList.len++] = &tagtypeSelect;
            filterArray[filterList.len++] = &uidSelect;

            pfilter->type = TMR_FILTER_TYPE_MULTI;
            pfilter->u.multiFilterList = filterList;

            //Read Tag Memory
            ret = TMR_ReadTagMemory(rp, &address, &dataBlockLen, pfilter, &response);
            checkerr(rp, ret, 1, "Unable to Read tag memory!");

            //Write to Tag Memory
            ret = TMR_WriteTagMemory(rp, &address, &data, pfilter, &response);
            checkerr(rp, ret, 1, "Unable to Write tag memory!");

            //Read Tag Memory again to verify
            //ret = TMR_ReadTagMemory(rp, &address, &dataBlockLen, pfilter, &response);
            //checkerr(rp, ret, 1, "Unable to Read tag memory!");
        }
        printf("\n");
#endif
    }

    TMR_destroy(rp);
    return 0;
}

#ifdef ENABLE_TAG_MEM_INFO
//Definitions.
TMR_Status isUsrMem_NTAG_UL(uint8_t* tagFound, uint32_t* address, uint8_t* len)
{
    TMR_Status ret = TMR_SUCCESS;
    uint8_t usrStart = UL_NTAG_USER_MEM_BEGIN;
    uint8_t usrEnd = 0;

    if ((*len < 1) && (*len > UL_NTAG_CFG_LEN))
    {
        return TMR_ERROR_INVALID_TAG_TYPE;
    }
    switch (*tagFound)
    {
        //CFG MEM: 0x10 - 0x13
    case API_UL_EV1_MF0UL11:
    {
        // Ultralight EV 1(MF0UL11) (cfg memory is from 0x10 to 0x13)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = UL_EV1_MF0UL11_USER_MEM_END;
        printf("Tag Type     : Ultralight EV 1(MF0UL11)\n");
    }
    break;
    //CFG MEM: 0x25 - 0x28   
    case API_UL_EV1_MF0UL21:
    {
        // Ultralight EV 1(MF0UL21) (cfg memory is from 0x25 to 0x28)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = UL_EV1_MF0UL21_USER_MEM_END;
        printf("Tag Type     : Ultralight EV 1(MF0UL21)\n");
    }
    break;
    //CFG MEM: 0x10 - 0x13
    case API_NTAG_210:
    {
        // NTAG 210 detected (cfg memory is from 0x10 to 0x13)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = NTAG_210_USER_MEM_END;
        printf("Tag Type     : NTAG_210\n");
    }
    break;
    //CFG MEM: 0x25 - 0x28
    case API_NTAG_212:
    {
        // NTAG 213 detected (cfg memory is from 0x25 to 0x28)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = NTAG_212_USER_MEM_END;
        printf("Tag Type     : NTAG_212\n");
    }
    break;
    //CFG MEM: 0x29 - 0x2C
    case API_NTAG_213:
    {
        // NTAG 213 detected (cfg memory is from 0x29 to 0x2C)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = NTAG_213_USER_MEM_END;
        printf("Tag Type     : NTAG_213\n");
    }
    break;
    //CFG MEM: 0x83 - 0x86
    case API_NTAG_215:
    {
        // NTAG 215 detected (cfg memory is from 0x83 to 0x86)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = NTAG_215_USER_MEM_END;
        printf("Tag Type     : NTAG_215\n");
    }
    break;
    //CFG MEM: 0xE3 - 0xE6
    case API_NTAG_216:
    {
        // NTAG 216 detected (cfg memory is from 0xE3 to 0xE6)
        //usrStart = UL_NTAG_USER_MEM_BEGIN;
        usrEnd = NTAG_216_USER_MEM_END;
        printf("Tag Type     : NTAG_216\n");
    }
    break;
    default:
        ret = TMR_ERROR_INVALID_TAG_TYPE;
        break;
    }

    if (((*address < usrStart) && (*address > usrEnd)) &&
        (((*address + *len) < usrStart) && ((*address + *len) < usrEnd))
        )
    {
        ret = TMR_ERROR_PROTOCOL_INVALID_ADDRESS;
    }

    return ret;
}

TMR_Status isCfgMem_NTAG_UL(uint8_t* tagFound, uint32_t* address, uint8_t* len)
{
    TMR_Status ret = TMR_SUCCESS;
    uint8_t cfgStart = 0;
    uint8_t cfgEnd   = 0;

    if ((*len < 1) && (*len > UL_NTAG_CFG_LEN))
    {
        return TMR_ERROR_INVALID_TAG_TYPE;
    }
    switch (*tagFound)
    {
        //CFG MEM: 0x10 - 0x13
    case API_UL_EV1_MF0UL11:
    {
        // Ultralight EV 1(MF0UL11) (cfg memory is from 0x10 to 0x13)
        cfgStart = 0x10;
        cfgEnd = 0x13;
        printf("Tag Type     : Ultralight EV 1(MF0UL11)\n");
    }
    break;
    //CFG MEM: 0x25 - 0x28   
    case API_UL_EV1_MF0UL21:
    {
        // Ultralight EV 1(MF0UL21) (cfg memory is from 0x25 to 0x28)
        cfgStart = 0x25;
        cfgEnd = 0x28;
        printf("Tag Type     : Ultralight EV 1(MF0UL21)\n");
    }
    break;
    //CFG MEM: 0x10 - 0x13
    case API_NTAG_210:
    {
        // NTAG 210 detected (cfg memory is from 0x10 to 0x13)
        cfgStart = 0x10;
        cfgEnd = 0x13;
        printf("Tag Type     : NTAG_210\n");
    }
    break;
    //CFG MEM: 0x25 - 0x28
    case API_NTAG_212:
    {
        // NTAG 213 detected (cfg memory is from 0x25 to 0x28)
        cfgStart = 0x25;
        cfgEnd = 0x28;
        printf("Tag Type     : NTAG_212\n");
    }
    break;
    //CFG MEM: 0x29 - 0x2C
    case API_NTAG_213:
    {
        // NTAG 213 detected (cfg memory is from 0x29 to 0x2C)
        cfgStart = 0x29;
        cfgEnd = 0x2C;
        printf("Tag Type     : NTAG_213\n");
    }
    break;
    //CFG MEM: 0x83 - 0x86
    case API_NTAG_215:
    {
        // NTAG 215 detected (cfg memory is from 0x83 to 0x86)
        printf("Tag Type     : NTAG_215\n");
    }
    break;
    //CFG MEM: 0xE3 - 0xE6
    case API_NTAG_216:
    {
        // NTAG 216 detected (cfg memory is from 0xE3 to 0xE6)
        cfgStart = 0xE3;
        cfgEnd = 0xE6;
        printf("Tag Type     : NTAG_216\n");
    }
    break;
    default:
        ret = TMR_ERROR_INVALID_TAG_TYPE;
        break;
    }

    if (((*address < cfgStart) && (*address > cfgEnd)) &&
        (((*address + *len) < cfgStart) && ((*address + *len) < cfgEnd))
        )
    {
        ret = TMR_ERROR_PROTOCOL_INVALID_ADDRESS;
    }

    return ret;
}

void GetMemInfo(uint8_t tagFound, uint32_t* address, uint8_t* len)
{
    printf("\n");

    if ((*address == 0) && (*len == 2))
    {
        printf("Accessing Manufacturer Data\n");
    }
    if ((*address == 2) && (*len == 1))
    {
        printf("Accessing Manufacturer Data and Static Lock Bytes\n");
    }
    if ((*address == 3) && (*len == 1))
    {
        if ((API_UL_EV1_MF0UL21 == tagFound) || (API_UL_EV1_MF0UL11 == tagFound))
        {
            printf("Accessing OTP\n");
        }
        else
        {
            printf("Accessing Capability Container\n");
        }
    }
    else if (TMR_SUCCESS == isUsrMem_NTAG_UL(&tagFound, address, len))
    {
        printf("Accessing User Memory\n");
    }
    else if (TMR_SUCCESS == isCfgMem_NTAG_UL(&tagFound, address, len))
    {
        printf("Accessing Config Memory\n");
    }
    else
    {
        printf("Accessing Unknown Memory\n");
    }
}

uint8_t GetTagInfo_NTAG_UL(TMR_Reader* rp, TMR_TagFilter* filter, TMR_uint8List* response)
{
    TMR_Status ret = TMR_SUCCESS;
    TMR_TagOp readOp;
    char idStr[128];
    uint8_t dataBuffer[32] = { 0 };
    uint8_t tagFound       = API_UL_NTAG_UNKNOWN;
    uint32_t address       = 0;
    uint8_t len            = 0;
    uint8_t option         = 0;
    uint8_t idx            = 0;
    uint8_t auxDataLen     = 0;

    printf("\nGetting Tag Info..\n");

    // Initialize the read memory tagOp
    TMR_TagOp_init_ReadMemory(&readOp, TMR_TAGOP_EXT_TAG_MEMORY, address, len);

    // Set Password
#if ENABLE_SECURE_RDWR
    TMR_set_accessPassword(&readOp, pAccPW);
#endif  //ENABLE_SECURE_RDWR

    // Set tag type.
    readOp.u.extTagOp.tagType = TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG;

    // Enable Read (Extended Operation)
    readOp.u.extTagOp.u.ulNtag.u.readData.subCmd = TMR_TAGOP_UL_NTAG_CMD_GET_VERSION;

    // Perform Read Memory Tag Operation (Standalone Tag Operation)
    ret = TMR_executeTagOp(rp, &readOp, filter, response);
    //checkerr(rp, ret, 1, "Unable to get tag version info!");
    if (TMR_SUCCESS != ret)
    {
        //Return.
        return tagFound;
    }

    //printf("Read tag info successful");
#if 0// Enable to display data received.
    if (response->len)
    {
        TMR_bytesToHex(response->list, response->len, idStr);
        //printf(" \nGet Tag Info Data: %s, Length: %d bytes\n", idStr, response->len);
    }
#endif

    //Parse first byte as option flags.
    option = response->list[idx++];
    if (response->list[0] & TMR_SR_SINGULATION_OPTION_EXT_TAGOP_PARAMS)
    {
        if (readOp.u.extTagOp.tagType == TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG)
        {
            //Check if authendication requested.
            if (option & TMR_SR_GEN2_SINGULATION_OPTION_SECURE_READ_DATA)
            {
                auxDataLen += 2;
            }

            //Parse the data.
            if (readOp.u.extTagOp.u.ulNtag.u.readData.subCmd == TMR_TAGOP_UL_NTAG_CMD_GET_VERSION)
            {
                //Copy Get Version response.
                memcpy(dataBuffer, &response->list[idx], response->len - idx - auxDataLen);
#if 0// Enable to display data received.
                TMR_bytesToHex(dataBuffer, response->len - idx - auxDataLen, idStr);
                printf("Tag Info Data    : %s, Length : %d bytes\n", idStr, (response->len - idx - auxDataLen));
#endif
                idx += response->len - idx - auxDataLen;
            }
        }
    }

    //Tag found?
    if (dataBuffer[2] == 0x03)
    {
        switch (dataBuffer[6])
        {
        case MIFARE_UL_EV1_MF0UL11:
            tagFound = API_UL_EV1_MF0UL11;
            break;
        case MIFARE_UL_EV1_MF0UL21:
            tagFound = API_UL_EV1_MF0UL21;
            break;
        default:
            ret = API_UL_NTAG_UNKNOWN;
            break;
        }
    }
    else if (dataBuffer[2] == 0x04)
    {
        switch (dataBuffer[6])
        {
        case MIFARE_NTAG_210:
            tagFound = API_NTAG_210;
            break;
        case MIFARE_NTAG_212:
            tagFound = API_NTAG_212;
            break;
        case MIFARE_NTAG_213:
            tagFound = API_NTAG_213;
            break;
        case MIFARE_NTAG_215:
            tagFound = API_NTAG_215;
            break;
        case MIFARE_NTAG_216:
            tagFound = API_NTAG_216;
            break;
        default:
            ret = API_UL_NTAG_UNKNOWN;
            break;
        }
    }
    else
    {
        // Ultralight C or NTag 203 (cfg memory is from 0x2B to 0x2F)
        tagFound = API_ULC_NTAG_203;
    }

    return tagFound;
}
#endif  //ENABLE_TAG_MEM_INFO

/**
* This function is used to initiate write operation to the tag memory.
*
* @param rp        Pointer to the reader object.
* @param tagFound  Type of the tag found by the reader during the search operation.
* @param tagOpType Type of the tag operation performed.
* @param response  Pointer to the response bytes received from the reader.
*/
TMR_Status parseExtTagOpResponse(TMR_Reader* rp, uint64_t tagType, TMR_TagOpType tagOpType, TMR_uint8List* response)
{
    TMR_Status ret = TMR_SUCCESS;
    char idStr[512];
    uint8_t option = 0;
    uint8_t idx = 0;
    uint8_t auxDataLen = 0;

    if (response->len)
    {
        //Parse first byte as option flags.
        option = response->list[idx++];
        if (option & TMR_SR_SINGULATION_OPTION_EXT_TAGOP_PARAMS)
        {

            if (tagType == TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG)
            {
                //Check if authendication requested.
                if (option & TMR_SR_GEN2_SINGULATION_OPTION_SECURE_READ_DATA)
                {
                    auxDataLen += 2;
                }

                //Parse the data.
                if (tagOpType == TMR_TAGOP_READ_MEMORY)
                {
                    TMR_bytesToHex(&response->list[idx], response->len - idx - auxDataLen, idStr);
                    printf("Read Data    : %s, Length : %d bytes\n", idStr, (response->len - idx - auxDataLen));
                    idx += response->len - idx - auxDataLen;
                }

                if (option & TMR_SR_GEN2_SINGULATION_OPTION_SECURE_READ_DATA)
                {
                    /* Parse the auxilary data.
                     * a) Pack data: 2 bytes.
                     */
                    memset(idStr, 0, 128);
                    TMR_bytesToHex(&response->list[idx], auxDataLen, idStr);
                    printf("PACK Data    : %s\n", idStr);
                }
            }
        }
    }

    return ret;
}

/**
* This function is used to initiate read operation from the tag memory.
*
* @param rp        Pointer to the reader object.
* @param address   Reader starts reading data present in the tag memory from this page.
* @param len       Number of pages to read.
* @param filter    Type of filter to be enabled.
* @param response  Pointer to the response bytes received from the reader.
*/
TMR_Status TMR_ReadTagMemory(TMR_Reader* rp, uint32_t* address, uint8_t* len, TMR_TagFilter* filter, TMR_uint8List* response)
{
    TMR_Status ret = TMR_SUCCESS;
    TMR_TagOp readOp;
    uint8_t tagFound = API_UL_NTAG_UNKNOWN;

    printf("\n\n/-- Tag Memory Read --/\n");

#if ENABLE_TAG_MEM_INFO
    tagFound = GetTagInfo_NTAG_UL(rp, filter, response);
    checkerr(rp, ret, 1, "Get tag info failed!");
    if (API_UL_NTAG_UNKNOWN != tagFound)
    {
        GetMemInfo(tagFound, address, len);
    }
#endif  //ENABLE_TAG_MEM_INFO

    // Initialize the read memory tagOp
    ret = TMR_TagOp_init_ReadMemory(&readOp, TMR_TAGOP_EXT_TAG_MEMORY, *address, *len);
    checkerr(rp, ret, 1, "Initializing read tag op memory!");

    // Set Password
#if ENABLE_SECURE_RDWR
    ret = TMR_set_accessPassword(&readOp, pAccPW);
    checkerr(rp, ret, 1, "Setting access password!");
#endif  //ENABLE_SECURE_RDWR

    // Set tag type
    readOp.u.extTagOp.tagType = TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG;

    // Enable Read (Extended Operation)
    readOp.u.extTagOp.u.ulNtag.u.readData.subCmd = TMR_TAGOP_UL_NTAG_CMD_READ;

    // Perform Read Memory Tag Operation (Standalone Tag Operation)
    ret = TMR_executeTagOp(rp, &readOp, filter, response);
    checkerr(rp, ret, 1, "Unable to execute read tag operation!");

    // Tag Read Successful
    //printf("Tag memory read successful.\n");
    parseExtTagOpResponse(rp, readOp.u.extTagOp.tagType, readOp.type, response);

    printf("\n/-- End --/");
    return ret;
}

/**
* This function is used to initiate write operation to the tag memory.
*
* @param rp        Pointer to the reader object.
* @param address   Reader starts writing the data in the tag memory from this page.
* @param data      Pointer to the data to be written in the tag memory.
* @param filter    Type of filter to be enabled.
* @param response  Pointer to the response bytes received from the reader.
*/
TMR_Status TMR_WriteTagMemory(TMR_Reader* rp, uint32_t* address, TMR_uint8List* data, TMR_TagFilter* filter, TMR_uint8List* response)
{
    TMR_Status ret = TMR_SUCCESS;
    TMR_TagOp writeOp;
    uint8_t tagFound = API_UL_NTAG_UNKNOWN;
    uint8_t len = data->len / 4;

    printf("\n\n/-- Tag Memory Write --/\n");

#if ENABLE_TAG_MEM_INFO
    tagFound = GetTagInfo_NTAG_UL(rp, filter, response);
    checkerr(rp, ret, 1, "Get Tag Info");
    if (API_UL_NTAG_UNKNOWN != tagFound)
    {
        GetMemInfo(tagFound, address, &len);
    }
#endif  //ENABLE_TAG_MEM_INFO

    // Initialize the write memory tagOp
    ret = TMR_TagOp_init_WriteMemory(&writeOp, TMR_TAGOP_EXT_TAG_MEMORY, *address, data);
    checkerr(rp, ret, 1, "Initializing write tag op memory!");

    // Set Password
#if ENABLE_SECURE_RDWR
    ret = TMR_set_accessPassword(&writeOp, pAccPW);
    checkerr(rp, ret, 1, "Setting access password!");
#endif  //ENABLE_SECURE_RDWR

    // Set tag type.
    writeOp.u.extTagOp.tagType = TMR_ISO14443A_TAGTYPE_ULTRALIGHT_NTAG;

    // Enable Write (Extended Operation)
    writeOp.u.extTagOp.u.ulNtag.u.writeData.subCmd = TMR_TAGOP_UL_NTAG_CMD_WRITE;

    // Perform Write Memory Tag Operation (Standalone Tag Operation)
    ret = TMR_executeTagOp(rp, &writeOp, filter, response);
    checkerr(rp, ret, 1, "Unable to execute write tag operation!");

    // Tag Write Successful
    printf("Tag memory write successful.\n");
    parseExtTagOpResponse(rp, writeOp.u.extTagOp.tagType, writeOp.type, response);

    printf("\n/-- End --/");
    return ret;
}
