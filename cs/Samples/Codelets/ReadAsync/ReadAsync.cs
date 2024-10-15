/**@def ENABLE_TAGOP_PROTOCOL
 * Uncomment to set tagOp protocol before performing tag operation.
 * By default, protocol is set to NONE on the M6e family.
 * Make sure to set Gen2 protocol before performing Gen2 standalone tag operation.
 */
//#define ENABLE_TAGOP_PROTOCOL

/** @def ENABLE_M6E_COMPATIBILITY
 *  Uncomment to make the API compatible with M6e.
 */
//#define ENABLE_M6E_COMPATIBILITY

using System;
using System.Collections.Generic;
using System.Text;
// for Thread.Sleep
using System.Threading;

// Reference the API
using ThingMagic;

namespace ReadAsync
{
    /// <summary>
    /// Sample program that reads tags in the background and prints the
    /// tags found for M7e and M3e. 
    /// The code sample also demonstrates how to connect to M6E series modules 
    /// and read tags using the Mercury API v1.37.1. The M6E family users must modify their 
    /// application by referring this code sample in order to use the latest API version. 
    /// (a) To enable M6E compatible code, uncomment ENABLE_M6E_COMPATIBILITY macro.
    /// (b) To enable standalone tag operation, uncomment ENABLE_TAGOP_PROTOCOL macro.
    /// </summary>
    class Program
    {
        /// <summary>
        /// This indicates the read time of async read, i.e., sleep time between start and stop read.
        /// </summary>
        private static double SLEEP_TIME = 5000;
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
            int[] antennaList = null;
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
                // Create Reader object, connecting to physical device.
                // Wrap reader in a "using" block to get automatic
                // reader shutdown (using IDisposable interface).
                using (Reader r = Reader.Create(args[0]))
                {
                    //Uncomment this line to add default transport listener.
                    //r.Transport += r.SimpleTransportListener;

                    try
                    {
                        r.Connect();
                    }
                    catch (Exception ex)
                    {
                        if (r is SerialReader)
                        {
                            /* MercuryAPI tries connecting to the module using default baud rate of 115200 bps.
                             * The connection may fail if the module is configured to a different baud rate. If
                             * that is the case, the MercuryAPI tries connecting to the module with other supported
                             * baud rates until the connection is successful using baud rate probing mechanism.
                             */
                            if (ex.Message.Contains("The operation has timed out"))
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
                            /* When the module is streaming the tags,
                             * r.Connect() returns with "Connect Successful...Streaming tags" exception, which should be handled in the codelet.
                             * User can either continue to parse streaming responses or stop the streaming.
                             * Use 'stream' option demonstrated in the AutonomousMode.cs codelet to
                             * continue streaming. To stop the streaming, use stopStreaming() as demonstrated below.
                             * Set baudrate if non-115200 and re-issue connect.
                             */
                            else if (ex.Message.Contains("Connect Successful...Streaming tags"))
                            {
                                //stop the streaming
                                ((SerialReader)r).stopStreaming();

                                // Re-issue connect
                                r.Connect();
                            }
                            else
                            {
                                throw new Exception(ex.Message);
                            }
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

#if ENABLE_M6E_COMPATIBILITY
                    /* To make the latest API compatible with M6e family modules,
                     * configure the following parameters.
                     * 1. tagOp protocol:  This parameter is not needed for Continuous Read or Async Read, but it
                     *                     must be set when standalone tag operation is performed, because the
                     *                     protocol is set to NONE, by default, in the M6e family modules.
                     *                     So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
                     * 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
                     *                     This filter is enabled, by default, in the M6e family modules.
                     * 3. Metadata flag:   SerialReader.TagMetadataFlag.ALL includes all flags (Supported by UHF and HF/LF readers).
                     *                     Disable unsupported flags for M6E family as shown below.
                     * Note: tagOp protocol and read filter are one time configurations - These must be set on the module once after every power ON. 
                     *       We do not have to set them in every read cycle.
                     *       But the Metadata flag must be set once after establishing a connection to the module using r.connect().
                     */
#if ENABLE_TAGOP_PROTOCOL
                    {
                        /* 1. tagOp protocol: This parameter is not needed for Continuous Read or Async Read, but it
                         *                    must be set when standalone tag operation is performed, because the
                         *                    protocol is set to NONE, by default, in the M6e family modules.
                         *                    So, users must set Gen2 protocol prior to performing Gen2 standalone tag operation.
                         */
                        r.ParamSet("/reader/tagop/protocol", TagProtocol.GEN2);
                        Gen2.ReadData readOp = new Gen2.ReadData(Gen2.Bank.EPC, 0, 2);
                        ushort[] response = (ushort[])r.ExecuteTagOp(readOp, null);
                        Console.WriteLine("ReadData: ");
                        foreach (ushort word in response)
                        {
                            Console.Write(" {0:X4}", word);
                        }
                        Console.WriteLine("\n");
                    }

#endif

                    {
                        /* 2. Set read filter: To report repeated tag entries of same tag, users must disable read filter.
                         *                     This filter is enabled, by default, in the M6e family modules.
                         *                     Note that this is a one time configuration while connecting to the module after
                         *                     power ON. We do not have to set it for every read cycle.
                         */
                        r.ParamSet("/reader/tagReadData/enableReadFilter", false);

                        /* 3. Metadata flag: SerialReader.TagMetadataFlag.ALL includes all flags (Supported by UHF and HF/LF readers).
                         *                   Disable unsupported flags for M6e family as shown below.
                         */
                        SerialReader.TagMetadataFlag flagSet =
                        SerialReader.TagMetadataFlag.ALL & (~SerialReader.TagMetadataFlag.TAGTYPE);
                        r.ParamSet("/reader/metadata", flagSet);
                    }
#endif
                    string model = (string)r.ParamGet("/reader/version/model").ToString();

                    // Create a simplereadplan which uses the antenna list created above
                    SimpleReadPlan plan;
                    if (model.Equals("M3e"))
                    {
                        // initializing the simple read plan
                        plan = new SimpleReadPlan(antennaList, TagProtocol.ISO14443A, null, null, 1000);
                    }
                    else
                    {
                        plan = new SimpleReadPlan(antennaList, TagProtocol.GEN2, null, null, 1000);
                    }
                    // Set the created readplan
                    r.ParamSet("/reader/read/plan", plan);

                    // Create and add tag listener
                    r.TagRead += delegate(Object sender, TagReadDataEventArgs e)
                    {
                        Console.WriteLine("Background read: " + e.TagReadData);

                        /* Reset the variable for valid tag response. */
                        r.lastReportedException = null;
                    };
                    // Create and add read exception listener

                    r.ReadException += delegate(object sender, ReaderExceptionEventArgs e)
                    {
                        if (r.lastReportedException == null || (r.lastReportedException == null) ? true : (!e.ReaderException.Message.Contains(r.lastReportedException.Message)))
                        {
                            Console.WriteLine("Error: " + e.ReaderException.Message);
                        }
                        r.lastReportedException = e.ReaderException;
                    };

                    // Search for tags in the background
                    r.StartReading();

                    // Exit the while loop,
                    //1. When error occurs 
                    //2. When sleep timeout expires

                    ///Capture the start time before starting the read.
                    DateTime startTime = DateTime.Now;
                    Console.WriteLine("\r\n<Do other work here>\r\n");
                    while (true)
                    {
                        if (ValidateReadTime(startTime))
                        {
                            //break if sleep timeout expired    
                            break;
                        }
                        //Exit the process if any error occured
                        if (r.lastReportedException != null)
                        {
                            errorHandler(r);
                            //Can add recovery mechanism here
                            //Do some work here 
                            Environment.Exit(1);
                        }
                        Thread.Sleep(1);
                    }
                    r.StopReading();

                }
            }
            catch (ReaderException re)
            {
                Console.WriteLine("Error: " + re.Message);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Compare the time for Async read
        /// </summary>
        /// <param name="time">Start Time</param>
        /// <returns>bool</returns>
        private static bool ValidateReadTime(DateTime time)
        {
            TimeSpan tr = DateTime.Now - time;
            if (tr.TotalMilliseconds >= SLEEP_TIME)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Function to handle different errors received
        /// </summary>
        /// <param name="r"></param>
        private static void errorHandler(Reader r)
        {
            SerialReader sr = r as SerialReader;
            ReaderException re = r.lastReportedException;
            switch (re.Message)
            {
                case "The reader received a valid command with an unsupported or invalid parameter":
                case "Unimplemented feature.":
                    r.StopReading();
                    r.Destroy();
                    break;
                case "The operation has timed out.":
                    sr.TMR_flush();
                    r.Destroy();
                    break;
                default:
                    r.Destroy();
                    break;
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
    }
}