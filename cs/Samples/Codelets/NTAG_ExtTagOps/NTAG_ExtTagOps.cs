/**
* Enable this macro to perform the secure read/write - The firmware sends
* "PWD_AUTH" command to the tag before performing any tag operation.
*/
#define ENABLE_SECURE_RDWR

/**
* Enable this macro to get version information from the tag. The firmware
* sends "GET_VERSION" command to the tag.
* This is used to find the exact tag type and figure out the memory layout
* of the tag.
*/
//#define ENABLE_TAG_MEM_INFO

using System;
using System.Collections.Generic;
using System.Text;

// Reference the API
using ThingMagic;

namespace NTAG_ExtTagOps
{
    // Static Class to store all the constants.
    static class Constants
    {

        /**TAG_MEM_RDWR_ADDR - used to configure the page address for performing the 
         * read/write tag operation on the tag.
         * It is configured to Page 4 - The user memory starts from this page.*/
        public const int TAG_MEM_RDWR_ADDR = 4;

        /**NUM_PAGES - used to configure the number of blocks/pages to be read
         * from tag memory or the number of blocks/pages to be written to the
         * tag memory.It is currently configured to a single block/page.*/
        public const int NUM_PAGES = 1;

        //Version number of NTAG/UL.
        public const int MIFARE_UL_EV1_MF0UL11 = 0x0B;
        public const int MIFARE_UL_EV1_MF0UL21 = 0x0E;
        public const int MIFARE_NTAG_210 = 0x0B;
        public const int MIFARE_NTAG_212 = 0x0E;
        public const int MIFARE_NTAG_213 = 0x0F;
        public const int MIFARE_NTAG_215 = 0x11;
        public const int MIFARE_NTAG_216 = 0x13;
        public const int INVALID_TAG_TYPE = 0x412;

        //API tag validation definition for NTAG/UL.
        public const int API_UL_NTAG_UNKNOWN = 0x00;
        public const int API_UL_EV1_MF0UL11 = 0x01;
        public const int API_UL_EV1_MF0UL21 = 0x02;
        public const int API_NTAG_210 = 0x03;
        public const int API_NTAG_212 = 0x04;
        public const int API_NTAG_213 = 0x05;
        public const int API_NTAG_215 = 0x06;
        public const int API_NTAG_216 = 0x07;
        public const int API_ULC_NTAG_203 = 0x08;

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
        public const int UL_NTAG_MEM_BEGIN = 0x00;  //Page No.
        public const int UL_NTAG_MFG_LCKBYTES_BEGIN = UL_NTAG_MEM_BEGIN;
        public const int UL_NTAG_MFG_LCKBYTES_LEN = 0x03;  //Pages
        public const int UL_NTAG_MFG_LCKBYTES_END = UL_NTAG_MEM_BEGIN + UL_NTAG_MFG_LCKBYTES_LEN;    //Page No. 
        public const int UL_NTAG_MFG_UID_MEM_BEGIN = UL_NTAG_MFG_LCKBYTES_BEGIN;                      //Page No
        public const int UL_NTAG_MFG_UID_LEN = 0x02;                                            //Pages
        public const int UL_NTAG_LCKBYTES_BEGIN = UL_NTAG_MFG_LCKBYTES_BEGIN + UL_NTAG_MFG_UID_LEN;//Page No

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
        public const int UL_NTAG_OTP_CC_MEM_BEGIN = 0x03;   //Page No.
        public const int UL_NTAG_OTP_CC_MEM_LEN = 0x01;   //Page
        public const int UL_NTAG_OTP_CC_MEM_END = 0x04;   //Page No.

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
        public const int UL_NTAG_USER_MEM_BEGIN = 0x04;   //Page No.

        public const int UL_EV1_MF0UL11_USER_MEM_END = 0x0F;
        public const int UL_EV1_MF0UL11_USER_MEM_LEN = UL_EV1_MF0UL11_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int UL_EV1_MF0UL21_USER_MEM_END = 0x23;
        public const int UL_EV1_MF0UL21_USER_MEM_LEN = UL_EV1_MF0UL21_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int NTAG_210_USER_MEM_END = 0x0F;
        public const int NTAG_210_USER_MEM_LEN = NTAG_210_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int NTAG_212_USER_MEM_END = 0x23;
        public const int NTAG_212_USER_MEM_LEN = NTAG_212_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int NTAG_213_USER_MEM_END = 0x27;
        public const int NTAG_213_USER_MEM_LEN = NTAG_213_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int NTAG_215_USER_MEM_END = 0x81;
        public const int NTAG_215_USER_MEM_LEN = NTAG_215_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

        public const int NTAG_216_USER_MEM_END = 0xE1;
        public const int NTAG_216_USER_MEM_LEN = NTAG_216_USER_MEM_END - UL_NTAG_USER_MEM_BEGIN;

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
        public const int UL_EV1_MF0UL11_CFG_MEM_BEGIN = 0x10;
        public const int UL_EV1_MF0UL11_CFG_MEM_END = 0x13;

        public const int UL_EV1_MF0UL21_CFG_MEM_BEGIN = 0x25;
        public const int UL_EV1_MF0UL21_CFG_MEM_END = 0x28;

        public const int NTAG_210_CFG_MEM_BEGIN = 0x10;
        public const int NTAG_210_CFG_MEM_END = 0x13;

        public const int NTAG_212_CFG_MEM_BEGIN = 0x25;
        public const int NTAG_212_CFG_MEM_END = 0x28;

        public const int NTAG_213_CFG_MEM_BEGIN = 0x29;
        public const int NTAG_213_CFG_MEM_END = 0x2C;

        public const int NTAG_215_CFG_MEM_BEGIN = 0x83;
        public const int NTAG_215_CFG_MEM_END = 0x86;

        public const int NTAG_216_CFG_MEM_BEGIN = 0xE3;
        public const int NTAG_216_CFG_MEM_END = 0xE6;

        public const int UL_NTAG_CFG_LEN = 0x04;        //Pages

        //Capability Container related defines.
        public const int CAPABILITY_CONTAINER_PAGE = 0x03;        //Page No.
        public const int CAPABILITY_CONTAINER_LEN = 0x01;        //Page

        public const int NTAG_210_CC_VAL = 0x06;
        public const int NTAG_212_CC_VAL = 0x10;
        public const int NTAG_213_CC_VAL = 0x12;
        public const int NTAG_215_CC_VAL = 0x3E;
        public const int NTAG_216_CC_VAL = 0x6D;
    }
    /// <summary>
    /// Sample program that demonstrates the functionality of reading/writing from/to tag memory of Ultralight N Tag.
    /// </summary>
    class NTAG_ExtTagOps
    {
        static int[] antennaList = null;
        static Reader r = null;
        static byte[] accessPwd = null;
        static void Usage()
        {
            Console.WriteLine(String.Join("\r\n", new string[] {
                    " Usage: "+"Please provide valid reader URL, such as: [-v] [reader-uri] [--ant n[,n...]]",
                    " -v : (Verbose)Turn on transport listener",
                    " reader-uri : e.g., 'tmr:///com4' or 'tmr:///dev/ttyS0/' or 'tmr://readerIP'",
                    " [--ant n[,n...]] : e.g., '--ant 1,2,..,n",
                    " Example for UHF: 'tmr:///com4' or 'tmr:///com4 --ant 1,2' or '-v tmr:///com4 --ant 1,2'",
                    " Example for HF/LF: 'tmr:///com4'"
            }));
            Environment.Exit(1);
        }
        static void Main(string[] args)
        {
            // Program setup
            if (1 > args.Length)
            {
                Usage();
            }

            for (int nextarg = 1; nextarg < args.Length; nextarg++)
            {
                string arg = args[nextarg];
                if (arg.Equals("--ant"))
                {
                    if (null != antennaList)
                    {
                        Console.WriteLine("Duplicate argument: --ant specified more than once");
                        Usage();
                    }
                    antennaList = ParseAntennaList(args, nextarg);
                    nextarg++;
                }
                else
                {
                    Console.WriteLine("Argument {0}:\"{1}\" is not recognized", nextarg, arg);
                    Usage();
                }
            }

            try
            {
                Console.WriteLine("NTAG Extended Tag Operations Code Sample \n");
                // Create Reader object, connecting to physical device.
                // Wrap reader in a "using" block to get automatic
                // reader shutdown (using IDisposable interface).
                using (r = Reader.Create(args[0]))
                {
                    //Uncomment this line to add default transport listener.
                    //r.Transport += r.SimpleTransportListener;

                    try
                    {
                        /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
                         * The connection may fail if the module is configured to a different baud rate. If
                         * that is the case, the MercuryAPI tries connecting to the module with other supported
                         * baud rates until the connection is successful using baud rate probing mechanism.
                         */
                        r.Connect();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The operation has timed out") && r is SerialReader)
                        {
                            int currentBaudRate = 0;
                            // Default baudrate connect failed. Try probing through the baudrate list
                            // to retrieve the module baudrate
                            ((SerialReader)r).probeBaudRate(ref currentBaudRate);
                            //Set the current baudrate so that next connect will use this baudrate.
                            r.ParamSet("/reader/baudRate", currentBaudRate);
                            // Now connect with current baudrate
                            r.Connect();
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                    if (Reader.Region.UNSPEC == (Reader.Region)r.ParamGet("/reader/region/id"))
                    {
                        Reader.Region[] supportedRegions = (Reader.Region[])r.ParamGet("/reader/region/supportedRegions");
                        if (supportedRegions.Length < 1)
                        {
                            throw new FAULT_INVALID_REGION_Exception();
                        }
                        r.ParamSet("/reader/region/id", supportedRegions[0]);
                    }
                    string model = (string)r.ParamGet("/reader/version/model").ToString();
                    if (!model.Equals("M3e"))
                    {
                        //Use first antenna for operation
                        if (antennaList != null)
                            r.ParamSet("/reader/tagop/antenna", antennaList[0]);
                    }

                    //Initialize the read plan
                    SimpleReadPlan plan = new SimpleReadPlan(antennaList, TagProtocol.ISO14443A, null, null, 1000);
                    r.ParamSet("/reader/read/plan", plan);

                    // Perform sync read and print UID and tagtype of tag found
                    TagReadData[] tagReads = r.Read(500);
                    Console.WriteLine("UID: " + tagReads[0].EpcString);
                    Console.WriteLine("TagType:  " + (Iso14443a.TagType)(tagReads[0].TagType));

                    /* Tag Operations */
                    if ((Iso14443a.TagType)(tagReads[0].TagType) == Iso14443a.TagType.ULTRALIGHT_NTAG)
                    {
                        UInt32 address = Constants.TAG_MEM_RDWR_ADDR;
                        byte length = Constants.NUM_PAGES;
                        accessPwd = new byte[] { 0xff, 0xff, 0xff, 0xff };
                        byte[] writeTagData = new byte[] { 0x11, 0x22, 0x33, 0x44 };

                        //Number of blocks
                        length = (length <= 0) ? (length = 1) : (length = Constants.NUM_PAGES);

                        //Initialize filter
                        // Filters the tag based on tagtype
                        TagFilter tagTypeFilter = new Select_TagType((UInt64)((Iso14443a.TagType)(tagReads[0].TagType)));
                        // Filters the tag based on UID
                        TagFilter uidFilter = new Select_UID((byte)(tagReads[0].Tag.EpcBytes.Length * 8), ByteFormat.FromHex(tagReads[0].Tag.EpcString));
                        // Initialize multi filter
                        MultiFilter mulfilter = new MultiFilter(new TagFilter[] { tagTypeFilter, uidFilter });

                        //Read Tag Memory
                        ReadTagMemory(r, address, length, mulfilter);

                        //Write to Tag Memory
                        WriteTagMemory(r, address, writeTagData, mulfilter);
                    }
                }

            }
            catch (ReaderException re)
            {
                Console.WriteLine("Error: " + re.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        #region ParseAntennaList

        private static int[] ParseAntennaList(IList<string> args, int argPosition)
        {
            int[] antennaList = null;
            try
            {
                string str = args[argPosition + 1];
                antennaList = Array.ConvertAll<string, int>(str.Split(','), int.Parse);
                if (antennaList.Length == 0)
                {
                    antennaList = null;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Missing argument after args[{0:d}] \"{1}\"", argPosition, args[argPosition]);
                Usage();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}\"{1}\"", ex.Message, args[argPosition + 1]);
                Usage();
            }
            return antennaList;
        }

        #endregion

        #region ReadTagMemory
        /// <summary>
        /// Reads the tag memory and parses the response.
        /// </summary>
        /// <param name="r">the reader object</param>
        /// <param name="address">address to read from</param>
        /// <param name="length">length to read</param>
        /// <param name="filter">filter</param>
        public static void ReadTagMemory(Reader r, uint address, byte length, TagFilter filter)
        {
            Console.WriteLine("\n/-- Tag Memory Read --/\n");

#if ENABLE_TAG_MEM_INFO
            uint tagFound = Constants.API_UL_NTAG_UNKNOWN;
            tagFound = GetTagInfo_NTAG_UL(r, filter);
            if (Constants.API_UL_NTAG_UNKNOWN != tagFound)
            {
                GetMemInfo(tagFound, address, length);
            }
#endif
            // Initialize the read memory tagOp
            ReadMemory readOp = new ReadMemory(MemoryType.EXT_TAG_MEMORY, address, length);
#if ENABLE_SECURE_RDWR
            // Set Password
            r.SetAccessPassword(readOp, accessPwd);
#endif
            //Set tagtype
            readOp.tagType = (long)Iso14443a.TagType.ULTRALIGHT_NTAG;

            // Enable Read (Extended Operation)
            readOp.ulNtag.readData.subCmd = (byte)UltraLightNtagCmd.READ;

            // Perform Read Memory Tag Operation (Standalone Tag Operation)
            byte[] response = (byte[])r.ExecuteTagOp(readOp, filter);

            // parse the response
            parseExtTagOpResponse(r, (uint)readOp.tagType, response, readOp);

            Console.WriteLine("\n/-- End --/");
        }
        #endregion

        # region WriteTagMemory
        /// <summary>
        /// This function is used to initiate write operation to the tag memory.
        /// </summary>
        /// <param name="r">The reader object</param>
        /// <param name="address">Reader starts writing the data in the tag memory from this page.</param>
        /// <param name="data">the data to be written in the tag memory</param>
        /// <param name="filter">Type of filter to be enabled</param>
        ///</summary> 
        public static void WriteTagMemory(Reader r, uint address, byte[] data, TagFilter filter)
        {
            byte length = (byte)(data.Length / 4);

            Console.WriteLine("\n/-- Tag Memory Write --/\n");

#if ENABLE_TAG_MEM_INFO
            uint tagFound = Constants.API_UL_NTAG_UNKNOWN;
            tagFound = GetTagInfo_NTAG_UL(r, filter);
            if (Constants.API_UL_NTAG_UNKNOWN != tagFound)
            {
                GetMemInfo(tagFound, address, length);
            }
#endif
            // Initialize the write memory tagOp
            WriteMemory writeOp = new WriteMemory(MemoryType.EXT_TAG_MEMORY, address, data);

#if ENABLE_SECURE_RDWR
            // Set Password
            r.SetAccessPassword(writeOp, accessPwd);
#endif
            //Set tagtype
            writeOp.tagType = (long)Iso14443a.TagType.ULTRALIGHT_NTAG;

            // Enable Read (Extended Operation)
            writeOp.ulNtag.writeData.subCmd = (byte)UltraLightNtagCmd.WRITE;

            // Perform Read Memory Tag Operation (Standalone Tag Operation)
            byte[] response = (byte[])r.ExecuteTagOp(writeOp, filter);

            // Tag Write Successful
            Console.WriteLine("Tag memory write successful.");

            parseExtTagOpResponse(r, (uint)writeOp.tagType, response, writeOp);

            Console.WriteLine("/-- End --/");
        }
        #endregion

        #region parseExtTagOpResponse
        /// <summary>
        /// This function is used to parse the read tag/write tag memory response.
        /// </summary>
        /// <param name="r">The reader object</param>
        /// <param name="tagFound">Type of the tag found by the reader during the search operation.</param>
        /// <param name="response">the response data to be parsed</param>
        /// <param name="tagOp">Type of the tag operation performed.</param>
        ///</summary> 
        public static void parseExtTagOpResponse(Reader r, uint tagFound, byte[] response, TagOp tagOp)
        {
            byte option = 0x00;
            int idx = 0;
            int auxDataLen = 0;
            byte SINGULATION_OPTION_SECURE_READ_DATA = 0x40; //don't change this. This constant indicates secure read data option byte is enabled.
            if (response.Length > 0)
            {
                //Parse first byte as option flags.
                option = response[idx++];
                if ((option & (byte)(SerialReader.SingulationOptions.EXT_TAGOP_PARAMS)) != 0)
                {
                    if (tagFound == (uint)(Iso14443a.TagType.ULTRALIGHT_NTAG))
                    {
                        //Check if authentication requested.
                        if ((option & SINGULATION_OPTION_SECURE_READ_DATA) != (byte)0x00)
                        {
                            auxDataLen += 2;
                        }

                        //Parse the data.
                        if (tagOp is ReadMemory)
                        {
                            byte[] idBytes = new byte[response.Length - idx - auxDataLen];
                            Array.Copy(response, idx, idBytes, 0, response.Length - idx - auxDataLen);
                            Console.WriteLine("Read Data : {0}, Length : {1} bytes ", ByteFormat.ToHex(idBytes), (response.Length - idx - auxDataLen));
                            idx += response.Length - idx - auxDataLen;
                        }

                        if ((option & SINGULATION_OPTION_SECURE_READ_DATA) != (byte)0x00)
                        {
                            /* Parse the auxilary data.
                             * a) Pack data: 2 bytes.
                             */
                            byte[] packBytes = new byte[2];
                            Array.Copy(response, idx, packBytes, 0, auxDataLen);
                            Console.WriteLine("PACK Data : " + ByteFormat.ToHex(packBytes));
                        }
                    }
                }
            }
        }
        #endregion


        #region isUsrMem_NTAG_UL
        /// <summary>
        /// This function is used to print the tagtype found and configure user memory end address based on tagtype.
        /// </summary>
        /// <param name="tagFound">Type of the tag found by the reader during the search operation.</param>
        /// <param name="address">address to read from</param>
        /// <param name="len">length to read</param>
        ///</summary> 
        public static int isUsrMem_NTAG_UL(uint tagFound, uint address, byte len)
        {
            uint usrStart = Constants.UL_NTAG_USER_MEM_BEGIN;
            uint usrEnd = 0;
            int ret = 0;

            if ((len < 1) && (len > Constants.UL_NTAG_CFG_LEN))
            {
                return Constants.INVALID_TAG_TYPE;
            }
            switch (tagFound)
            {
                //CFG MEM: 0x10 - 0x13
                case Constants.API_UL_EV1_MF0UL11:
                    {
                        // Ultralight EV 1(MF0UL11) (cfg memory is from 0x10 to 0x13)
                        usrEnd = Constants.UL_EV1_MF0UL11_USER_MEM_END;
                        Console.WriteLine("Tag Type     : Ultralight EV 1(MF0UL11)\n");
                    }
                    break;
                //CFG MEM: 0x25 - 0x28   
                case Constants.API_UL_EV1_MF0UL21:
                    {
                        // Ultralight EV 1(MF0UL21) (cfg memory is from 0x25 to 0x28)
                        usrEnd = Constants.UL_EV1_MF0UL21_USER_MEM_END;
                        Console.WriteLine("Tag Type     : Ultralight EV 1(MF0UL21)\n");
                    }
                    break;
                //CFG MEM: 0x10 - 0x13
                case Constants.API_NTAG_210:
                    {
                        // NTAG 210 detected (cfg memory is from 0x10 to 0x13)
                        usrEnd = Constants.NTAG_210_USER_MEM_END;
                        Console.WriteLine("Tag Type     : NTAG_210\n");
                    }
                    break;
                //CFG MEM: 0x25 - 0x28
                case Constants.API_NTAG_212:
                    {
                        // NTAG 213 detected (cfg memory is from 0x25 to 0x28)
                        usrEnd = Constants.NTAG_212_USER_MEM_END;
                        Console.WriteLine("Tag Type     : NTAG_212\n");
                    }
                    break;
                //CFG MEM: 0x29 - 0x2C
                case Constants.API_NTAG_213:
                    {
                        // NTAG 213 detected (cfg memory is from 0x29 to 0x2C)
                        usrEnd = Constants.NTAG_213_USER_MEM_END;
                        Console.WriteLine("Tag Type     : NTAG_213\n");
                    }
                    break;
                //CFG MEM: 0x83 - 0x86
                case Constants.API_NTAG_215:
                    {
                        // NTAG 215 detected (cfg memory is from 0x83 to 0x86)
                        usrEnd = Constants.NTAG_215_USER_MEM_END;
                        Console.WriteLine("Tag Type     : NTAG_215\n");
                    }
                    break;
                //CFG MEM: 0xE3 - 0xE6
                case Constants.API_NTAG_216:
                    {
                        // NTAG 216 detected (cfg memory is from 0xE3 to 0xE6)
                        usrEnd = Constants.NTAG_216_USER_MEM_END;
                        Console.WriteLine("Tag Type     : NTAG_216\n");
                    }
                    break;
                default:
                    return Constants.INVALID_TAG_TYPE;
            }

            if (((address < usrStart) && (address > usrEnd)) && (((address + len) < usrStart) && ((address + len) < usrEnd)))
            {
                return FAULT_PROTOCOL_INVALID_ADDRESS_Exception.StatusCode;
            }
            return ret;
        }
        #endregion

        #region isCfgMem_NTAG_UL
        /// <summary>
        /// This function is used to print the tagtype found and configure the start and end address of configuration memory based on tagtype.
        /// </summary>
        /// <param name="tagFound">Type of the tag found by the reader during the search operation.</param>
        /// <param name="address">address to read from</param>
        /// <param name="len">length to read</param>
        ///</summary> 
        public static int isCfgMem_NTAG_UL(uint tagFound, uint address, byte len)
        {
            int ret = 0;
            uint cfgStart = 0, cfgEnd = 0;

            if ((len < 1) && (len > Constants.UL_NTAG_CFG_LEN))
            {
                throw new ReaderException("Invalid TagType: " + Constants.INVALID_TAG_TYPE);
            }
            switch (tagFound)
            {
                //CFG MEM: 0x10 - 0x13
                case Constants.API_UL_EV1_MF0UL11:
                    {
                        // Ultralight EV 1(MF0UL11) (cfg memory is from 0x10 to 0x13)
                        cfgStart = 0x10;
                        cfgEnd = 0x13;
                        Console.WriteLine("Tag Type     : Ultralight EV 1(MF0UL11)\n");
                    }
                    break;
                //CFG MEM: 0x25 - 0x28   
                case Constants.API_UL_EV1_MF0UL21:
                    {
                        // Ultralight EV 1(MF0UL21) (cfg memory is from 0x25 to 0x28)
                        cfgStart = 0x25;
                        cfgEnd = 0x28;
                        Console.WriteLine("Tag Type     : Ultralight EV 1(MF0UL21)\n");
                    }
                    break;
                //CFG MEM: 0x10 - 0x13
                case Constants.API_NTAG_210:
                    {
                        // NTAG 210 detected (cfg memory is from 0x10 to 0x13)
                        cfgStart = 0x10;
                        cfgEnd = 0x13;
                        Console.WriteLine("Tag Type     : NTAG_210\n");
                    }
                    break;
                //CFG MEM: 0x25 - 0x28
                case Constants.API_NTAG_212:
                    {
                        // NTAG 212 detected (cfg memory is from 0x25 to 0x28)
                        cfgStart = 0x25;
                        cfgEnd = 0x28;
                        Console.WriteLine("Tag Type     : NTAG_212\n");
                    }
                    break;
                //CFG MEM: 0x29 - 0x2C
                case Constants.API_NTAG_213:
                    {
                        // NTAG 213 detected (cfg memory is from 0x29 to 0x2C)
                        cfgStart = 0x29;
                        cfgEnd = 0x2C;
                        Console.WriteLine("Tag Type     : NTAG_213\n");
                    }
                    break;
                //CFG MEM: 0x83 - 0x86
                case Constants.API_NTAG_215:
                    {
                        // NTAG 215 detected (cfg memory is from 0x83 to 0x86)
                        Console.WriteLine("Tag Type     : NTAG_215\n");
                    }
                    break;
                //CFG MEM: 0xE3 - 0xE6
                case Constants.API_NTAG_216:
                    {
                        // NTAG 216 detected (cfg memory is from 0xE3 to 0xE6)
                        cfgStart = 0xE3;
                        cfgEnd = 0xE6;
                        Console.WriteLine("Tag Type     : NTAG_216\n");
                    }
                    break;
                default:
                    return Constants.INVALID_TAG_TYPE;
            }

            if (((address < cfgStart) && (address > cfgEnd)) &&
                (((address + len) < cfgStart) && ((address + len) < cfgEnd)))
            {
                return FAULT_PROTOCOL_INVALID_ADDRESS_Exception.StatusCode;
            }
            return ret;
        }
        #endregion

        #region GetTagInfo_NTAG_UL
        /// <summary>
        /// This function is used to get the tag information
        /// </summary>
        /// <param name="r">The reader object.</param>
        /// <param name="filter">The type of filter to be enabled</param>
        ///</summary> 
        public static uint GetTagInfo_NTAG_UL(Reader r, TagFilter filter)
        {
            uint address = 0;
            byte length = 0;
            uint tagFound = Constants.API_UL_NTAG_UNKNOWN;

            Console.WriteLine("\nGetting Tag Info..\n");

            // Initialize the read memory tagOp
            ReadMemory readOp = new ReadMemory(MemoryType.EXT_TAG_MEMORY, address, length);

            // Set Password
            r.SetAccessPassword(readOp, accessPwd);

            //Set tagtype
            readOp.tagType = (long)Iso14443a.TagType.ULTRALIGHT_NTAG;

            // Enable Read (Extended Operation)
            readOp.ulNtag.readData.subCmd = (byte)UltraLightNtagCmd.GET_VERSION;

            // Perform Read Memory Tag Operation (Standalone Tag Operation)
            byte[] response = (byte[])r.ExecuteTagOp(readOp, filter);

            //skip option byte in the response and copy the rest of the data to tagInfoRsp buffer.
            byte[] tagInfoRsp = new byte[response.Length - 1];
            Array.Copy(response, 1, tagInfoRsp, 0, response.Length - 1);

            //Tag found?
            if (tagInfoRsp[2] == 0x03)
            {
                switch (tagInfoRsp[6])
                {
                    case Constants.MIFARE_UL_EV1_MF0UL11:
                        tagFound = Constants.API_UL_EV1_MF0UL11;
                        break;
                    case Constants.MIFARE_UL_EV1_MF0UL21:
                        tagFound = Constants.API_UL_EV1_MF0UL21;
                        break;
                    default:
                        tagFound = Constants.API_UL_NTAG_UNKNOWN;
                        break;
                }
            }
            else if (tagInfoRsp[2] == 0x04)
            {
                switch (tagInfoRsp[6])
                {
                    case Constants.MIFARE_NTAG_210:
                        tagFound = Constants.API_NTAG_210;
                        break;
                    case Constants.MIFARE_NTAG_212:
                        tagFound = Constants.API_NTAG_212;
                        break;
                    case Constants.MIFARE_NTAG_213:
                        tagFound = Constants.API_NTAG_213;
                        break;
                    case Constants.MIFARE_NTAG_215:
                        tagFound = Constants.API_NTAG_215;
                        break;
                    case Constants.MIFARE_NTAG_216:
                        tagFound = Constants.API_NTAG_216;
                        break;
                    default:
                        tagFound = Constants.API_UL_NTAG_UNKNOWN;
                        break;
                }
            }
            else
            {
                // Ultralight C or NTag 203 (cfg memory is from 0x2B to 0x2F)
                tagFound = Constants.API_ULC_NTAG_203;
            }
            return tagFound;
        }
        #endregion

        #region GetMemInfo
        /// <summary>
        /// This function is used to get the memory information i.e., which memory info is accessing when called.
        /// </summary>
        /// <param name="tagFound">Type of the tag found by the reader during the search operation.</param>
        /// <param name="address">address to read from</param>
        /// <param name="len">length to read</param>
        ///</summary> 
        public static void GetMemInfo(uint tagFound, uint address, byte len)
        {
            Console.WriteLine("\n");
            if ((address == 0) && (len == 2))
            {
                Console.WriteLine("Accessing Manufacturer Data\n");
            }
            if ((address == 2) && (len == 1))
            {
                Console.WriteLine("Accessing Manufacturer Data and Static Lock Bytes\n");
            }
            if ((address == 3) && (len == 1))
            {
                if ((Constants.API_UL_EV1_MF0UL21 == tagFound) || (Constants.API_UL_EV1_MF0UL11 == tagFound))
                {
                    Console.WriteLine("Accessing OTP\n");
                }
                else
                {
                    Console.WriteLine("Accessing Capability Container\n");
                }
            }
            else if (isUsrMem_NTAG_UL(tagFound, address, len) == 0)
            {
                Console.WriteLine("Accessing User Memory\n");
            }
            else if (isCfgMem_NTAG_UL(tagFound, address, len) == 0)
            {
                Console.WriteLine("Accessing Config Memory\n");
            }
            else
            {
                Console.WriteLine("Accessing Unknown Memory\n");
            }
        }
        #endregion
    }
}