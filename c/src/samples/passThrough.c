/**
 * Sample program that demonstrate the passThrough functionality
 * @file passThrough.c
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

#define OPCODE_SELECT_TAG         0x25
#define MAX_UID_LEN               0x08
#define OPCODE_GET_RANDOM_NUMBER  0xB2
#define IC_MFG_CODE_NXP           0x04
#define MAX_RESPONSE_LENGTH       240

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

//Forward declarations.
void appendReverseUID(uint8_t *cmdStr, uint8_t *uidISO15693);

int main(int argc, char *argv[])
{
  TMR_Reader r, *rp;
  TMR_Status ret;
#ifdef TMR_ENABLE_HF_LF
  uint32_t timeout;
  TMR_uint8List cmd;
  TMR_uint8List response;
  TMR_TagOp passThroughOp;
  uint8_t flags, responseData[MAX_RESPONSE_LENGTH];
  TMR_Reader_configFlags configFlags;
  uint8_t cmdStr[TMR_SR_MAX_PACKET_SIZE];
  TMR_ReadPlan plan;
  uint8_t buffer[20];
  uint8_t *antennaList = NULL;
  uint8_t antennaCount = 0x0;
  TMR_TagReadData trd;
#endif /* TMR_ENABLE_HF_LF */

#if USE_TRANSPORT_LISTENER
  TMR_TransportListenerBlock tb;
#endif
 
#ifdef TMR_ENABLE_HF_LF
  antennaList = buffer;
#endif /* TMR_ENABLE_HF_LF */

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

#ifdef TMR_ENABLE_HF_LF
  // initialize the read plan
  ret = TMR_RP_init_simple(&plan, antennaCount, antennaList, TMR_TAG_PROTOCOL_ISO15693, 1000);

#ifndef BARE_METAL
  checkerr(rp, ret, 1, "initializing the  read plan");
#endif

  /* Commit read plan */
  ret = TMR_paramSet(rp, TMR_PARAM_READ_PLAN, &plan);
#ifndef BARE_METAL
  checkerr(rp, ret, 1, "setting read plan");
#endif
  ret = TMR_read(rp, 500, NULL);
#ifndef BARE_METAL
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
#endif
  if(TMR_SUCCESS == TMR_hasMoreTags(rp))
  {
    char idStr[128];

    ret = TMR_getNextTag(rp, &trd);
#ifndef BARE_METAL  
    checkerr(rp, ret, 1, "fetching tag");
#endif
    TMR_bytesToHex(trd.tag.epc, trd.tag.epcByteCount, idStr);
    printf("Tag ID : %s \n", idStr);
 }

  //Initialization.
  response.list = responseData;
  response.max = sizeof(responseData) / sizeof(responseData[0]);
  response.len = 0;

  cmd.len = cmd.max = 0;
  cmd.list = cmdStr;

  //Select Tag.
  timeout = 500; //Timeout in Ms.
  flags = 0x22;
  configFlags = TMR_READER_CONFIG_FLAGS_ENABLE_TX_CRC | TMR_READER_CONFIG_FLAGS_ENABLE_RX_CRC |
                TMR_READER_CONFIG_FLAGS_ENABLE_INVENTORY;

  /* Frame payload data as per 15693 protocol(ICODE Slix-S) */
  cmd.list[cmd.len++] = flags;
  cmd.list[cmd.len++] = OPCODE_SELECT_TAG;

  //Append UID(reverse).
  appendReverseUID(&cmdStr[cmd.len], trd.tag.epc);
  cmd.len += MAX_UID_LEN;

  ret = TMR_TagOp_init_PassThrough(&passThroughOp, timeout, configFlags, &cmd);
  checkerr(rp, ret, 1, "Creating passthrough tagop to select tag");

  ret = TMR_executeTagOp(rp, &passThroughOp, NULL, &response);
  checkerr(rp, ret, 1, "Executing passthrough tagop to select tag");

  if (0 < response.len)
  {
    char dataStr[255];

    TMR_bytesToHex(response.list, response.len, dataStr);
    printf("Select Tag| Data(%d): %s\n", response.len, dataStr);
  }

  //Reset command buffer.
  memset(cmdStr, 0x00, MAX_RESPONSE_LENGTH);
  cmd.len = 0;

  //Reset command buffer.
  memset(responseData, 0x00, MAX_RESPONSE_LENGTH);
  response.len = 0;

  //Get random number.
  timeout = 500; //Timeout in Ms.
  flags = 0x12;
  configFlags = TMR_READER_CONFIG_FLAGS_ENABLE_TX_CRC | TMR_READER_CONFIG_FLAGS_ENABLE_RX_CRC |
                TMR_READER_CONFIG_FLAGS_ENABLE_INVENTORY;

  /* Frame payload data as per 15693 protocol(ICODE Slix-S) */
  cmd.list[cmd.len++] = flags;
  cmd.list[cmd.len++] = OPCODE_GET_RANDOM_NUMBER;
  cmd.list[cmd.len++] = IC_MFG_CODE_NXP;

  ret = TMR_TagOp_init_PassThrough(&passThroughOp, timeout, configFlags, &cmd);
  checkerr(rp, ret, 1, "Creating passthrough tagop to get RN");

  ret = TMR_executeTagOp(rp, &passThroughOp, NULL, &response);
  checkerr(rp, ret, 1, "Executing passthrough tagop to get RN");

  if (0 < response.len)
  {
    char dataStr[255];

    TMR_bytesToHex(response.list, response.len, dataStr);
    printf("RN number | Data(%d): %s\n", response.len, dataStr);
  }
#endif /* TMR_ENABLE_HF_LF */

  TMR_destroy(rp);
  return 0;
}

void appendReverseUID(uint8_t *cmdStr, uint8_t *uidISO15693)
{
  uint8_t i = 0;

  while(i < MAX_UID_LEN)
  {
    cmdStr[i] = uidISO15693[(MAX_UID_LEN - 1) - i];
    i++;
  }
}