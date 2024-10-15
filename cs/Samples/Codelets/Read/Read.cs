using System;
using System.Collections.Generic;
using System.Text;

// Reference the API
using ThingMagic;

namespace Read
{
    /// <summary>
    /// Sample program that reads tags for a fixed period of time (500ms)
    /// and prints the tags found.
    /// </summary>
    class Program
    {
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
            Reader r = null;
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
                r = Reader.Create(args[0]);
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
                            throw new Exception(ex.Message);
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

                    // Enable printTagMetada Flags to print Metadata value
                    bool printTagMetadata = false;
                    SerialReader.TagMetadataFlag flagSet;
                    SimpleReadPlan plan;
                    // Metadata is not supported for M6 reader. Hence conditionalize here.
                    if (!model.Equals("Mercury6"))
                    {
                        //flagSet = SerialReader.TagMetadataFlag.ANTENNAID | SerialReader.TagMetadataFlag.FREQUENCY;
                        flagSet = SerialReader.TagMetadataFlag.ALL;
                        r.ParamSet("/reader/metadata", flagSet);
                    }
                    // Create a simplereadplan which uses the antenna list created above
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
                    {
                        Console.WriteLine("Tag ID: " + tr.EpcString);
                        if (printTagMetadata)
                        {
                            foreach (SerialReader.TagMetadataFlag flg in Enum.GetValues(typeof(SerialReader.TagMetadataFlag)))
                            {
                                if ((0 != (tr.metaDataFlags & flg)))
                                {
                                    switch ((SerialReader.TagMetadataFlag)(flg))
                                    {
                                        case SerialReader.TagMetadataFlag.ANTENNAID:
                                            Console.WriteLine("Antenna ID: " + tr.Antenna.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.DATA:
                                            if (tr.isErrorData)
                                            {
                                                // In case of error, show the error to user. Extract error code.
                                                int errorCode = ByteConv.ToU16(tr.Data, 0);
                                                Console.WriteLine("Embedded Tag operation failed. Error: " + ReaderCodeException.faultCodeToMessage(errorCode));
                                            }
                                            else
                                            {
                                                Console.WriteLine("Data[" + (tr.dataLength/8) + "]: " + ByteFormat.ToHex(tr.Data, "", " "));
                                            }
                                            break;
                                        case SerialReader.TagMetadataFlag.FREQUENCY:
                                            Console.WriteLine("Frequency: " + tr.Frequency.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.GPIO:

                                            if (r is SerialReader)
                                            {
                                                Console.WriteLine("GPO Status:");
                                                foreach (GpioPin pin in tr.GPIO)
                                                    Console.WriteLine("Pin" + pin.Id.ToString() + ": " + (pin.High ? "High" : "Low"));
                                                Console.WriteLine("GPI Status:");
                                                foreach (GpioPin pin in tr.GPIO)
                                                    Console.WriteLine("Pin" + pin.Id.ToString() + ": " + (pin.Output ? "High" : "Low"));
                                            }
                                            else
                                            {
                                                Console.WriteLine("GPI Status:");
                                                for (int i = 0; i < tr.GPIO.Length / 2; i++)
                                                {
                                                    Console.WriteLine("Pin" + tr.GPIO[i].Id.ToString() + ": " + (tr.GPIO[i].High ? "High" : "Low"));
                                                }
                                                Console.WriteLine("GPO Status:");
                                                for (int i = tr.GPIO.Length / 2; i < tr.GPIO.Length; i++)
                                                {
                                                    Console.WriteLine("Pin" + tr.GPIO[i].Id.ToString() + ": " + (tr.GPIO[i].High ? "High" : "Low"));
                                                }
                                            }
                                            break;
                                        case SerialReader.TagMetadataFlag.PHASE:
                                            Console.WriteLine("Phase: " + tr.Phase.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.PROTOCOL:
                                            Console.WriteLine("Protocol: " + tr.Tag.Protocol.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.READCOUNT:
                                            Console.WriteLine("Read Count: " + tr.ReadCount.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.RSSI:
                                            Console.WriteLine("RSSI: " + tr.Rssi.ToString());
                                            break;
                                        case SerialReader.TagMetadataFlag.TIMESTAMP:
                                            Console.WriteLine("Timestamp: " + tr.Time.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"));
                                            break;
                                        case SerialReader.TagMetadataFlag.TAGTYPE:
                                            switch (tr.Tag.Protocol)
                                            {
                                                case TagProtocol.ISO14443A:
                                                    Console.WriteLine("TagType: " + (Iso14443a.TagType)tr.TagType);
                                                    break;
                                                case TagProtocol.ISO15693:
                                                    Console.WriteLine("TagType: " + (Iso15693.TagType)tr.TagType);
                                                    break;
                                                case TagProtocol.LF125KHZ:
                                                    Console.WriteLine("TagType: " + (Lf125khz.TagType)tr.TagType);
                                                    break;
                                                case TagProtocol.LF134KHZ:
                                                    Console.WriteLine("TagType: " + (Lf134khz.TagType)tr.TagType);
                                                    break;
                                                default:
                                                    Console.WriteLine("TagType: " + tr.TagType);
                                                    break;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                    if (TagProtocol.GEN2 == tr.Tag.Protocol)
                                    {
                                        Gen2.TagReadData gen2 = (Gen2.TagReadData)(tr.prd);
                                        switch ((SerialReader.TagMetadataFlag)(flg))
                                        {
                                            case SerialReader.TagMetadataFlag.GEN2_Q:
                                                Console.WriteLine("Gen2Q: " + gen2.Q.ToString());
                                                break;
                                            case SerialReader.TagMetadataFlag.GEN2_LF:
                                                Console.WriteLine("Gen2LinkFrequency: " + gen2.LF.ToString());
                                                break;
                                            case SerialReader.TagMetadataFlag.GEN2_TARGET:
                                                Console.WriteLine("Gen2Target: " + gen2.Target.ToString());
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ReaderException re)
            {
                Console.WriteLine("Error: " + re.Message);
                // In case of tag id buffer full exception, pull the tags from the module's buffer
                if (re is FAULT_TAG_ID_BUFFER_FULL_Exception)
                {
                    if (r is SerialReader)
                    {
                        // retrieve all the tags 
                        TagReadData[] tagReads = ((SerialReader)r).GetAllTagReadsFromBuffer();
                        foreach (TagReadData tr in tagReads)
                        {
                            Console.WriteLine("Tag ID: " + tr.EpcString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                r.Destroy();
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