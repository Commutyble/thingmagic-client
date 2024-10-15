using System;
using System.Collections.Generic;
using System.Text;

// Reference the API
using ThingMagic;

namespace SerialTime
{
    /// <summary>
    /// Sample program that reads tags for a fixed period of time (500ms)
    /// and prints the tags found, while logging the serial message with timestamps
    ///i.e., custom transport listener.
    /// </summary>
    class SerialTime
    {
        static String Readertype = null;
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
                // Create Reader object, but do not connect to physical device.
                // Wrap reader in a "using" block to get automatic
                // reader shutdown (using IDisposable interface).
                using (Reader r = Reader.Create(args[0]))
                {
                    if (r is SerialReader)
                    {
                      Readertype = "Serial";
                    }
                    else
                    {
                      Readertype = "Network";
                    }
                    // Add the serial-reader-specific message logger
                    // before connecting, so we can see the initialization.
                    //r.Transport += TimestampListener;
                    // Now connect to physical device
                    r.Connect();
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
                    // Read tags
                    TagReadData[] tagReads = r.Read(500);
                    // Print tag reads
                    foreach (TagReadData tr in tagReads)
                        Console.WriteLine(tr.ToString());
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

        static void TimestampListener(Object sender, TransportListenerEventArgs e)
        {
            Console.Write("[" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") + "]");

            if (Readertype.Equals("Serial"))
            {
               Console.WriteLine(String.Format(
               "{0} {1} (timeout={2:D}ms)",
               e.Tx ? "  TX:\n" : "  RX:\n",
               ByteFormat.ToHex(e.Data, "", " "),
               e.Timeout
               ));
            }
            else
            {
              string msg;
              msg = Encoding.ASCII.GetString(e.Data, 0, e.Data.Length);
              Console.WriteLine(String.Format(
              "{0} {1} (timeout={2:D}ms)",
              e.Tx ? "  TX:\n" : "  RX:\n",
              msg,
              e.Timeout
              ));
              Console.WriteLine();
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
