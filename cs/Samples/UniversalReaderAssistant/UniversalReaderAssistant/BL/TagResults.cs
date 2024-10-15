using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections;

namespace ThingMagic.URA2.BL
{
    /// <summary>
    /// DataGridView adapter for a TagReadData
    /// </summary>
    public class TagReadRecord : INotifyPropertyChanged
    {
        protected TagReadData RawRead = null;
        protected bool dataChecked = false;
        protected UInt32 serialNo = 0;
        protected string epcInAscii = null;
        protected string dataInAscii = null;
        protected string epcInReverseBase36 = null;
        //protected string dataInReverseBase36 = null;
        public TagReadRecord(TagReadData newData)
        {
            lock (new Object())
            {
                RawRead = newData;
            }
        }
        /// <summary>
        /// Merge new tag read with existing one
        /// </summary>
        /// <param name="data">New tag read</param>
        public void Update(TagReadData mergeData)
        {
            mergeData.ReadCount += ReadCount;
            TimeSpan timediff = mergeData.Time.ToUniversalTime() - this.TimeStamp.ToUniversalTime();
            // Update only the read counts and not overwriting the tag
            // read data of the existing tag in tag database when we 
            // receive tags in incorrect order.
            if (0 <= timediff.TotalMilliseconds)
            {
                RawRead = mergeData;
            }
            else
            {
                RawRead.ReadCount = mergeData.ReadCount;
            }
            OnPropertyChanged(null);
        }

        public UInt32 SerialNumber
        {
            get { return serialNo; }
            set { serialNo = value; }
        }

        public string EPCInASCII
        {
            get
            {
                if (RawRead.Epc.Length > 0)
                    epcInAscii = Utilities.HexStringToAsciiString(RawRead.EpcString);
                else
                    epcInAscii = String.Empty;
                return epcInAscii;
            }
        }

        public string EPCInReverseBase36
        {
            get
            {
                if (RawRead.EpcString.Length > 0)
                    epcInReverseBase36 = Utilities.ConvertHexToBase36(RawRead.EpcString);
                else
                    epcInReverseBase36 = String.Empty;
                return epcInReverseBase36;
            }

        }

        public string DataInASCII
        {
            get
            {
                if (RawRead.Data.Length > 0)
                    dataInAscii = Utilities.HexStringToAsciiString(ByteFormat.ToHex(RawRead.Data).Split('x')[1]);
                else
                    dataInAscii = String.Empty;
                return dataInAscii;
            }
        }
        //public string DataInReverseBase36
        //{
        //    get { return dataInReverseBase36; }
        //    set 
        //    {
        //        if (value != string.Empty)
        //        {
        //            dataInReverseBase36 = value;
        //        }
        //        else
        //        {
        //            dataInReverseBase36 = string.Empty;
        //        }
        //    }
        //}
        public DateTime TimeStamp
        {
            get
            {
                //return DateTime.Now.ToLocalTime();
                TimeSpan difftime = (DateTime.Now.ToUniversalTime() - RawRead.Time.ToUniversalTime());
                //double a1111 = difftime.TotalSeconds;
                if (difftime.TotalHours > 24)
                    return DateTime.Now.ToLocalTime();
                else
                    return RawRead.Time.ToLocalTime();
            }
        }
        public int ReadCount
        {
            get { return RawRead.ReadCount; }
        }
        public int Antenna
        {
            get { return RawRead.Antenna; }
        }
        public TagProtocol Protocol
        {
            get { return RawRead.Tag.Protocol; }
        }
        public int RSSI
        {
            get { return RawRead.Rssi; }
        }
        public string EPC
        {
            get { return RawRead.EpcString; }
        }
        public string Data
        {
            get
            {
                if (RawRead.isErrorData)
                {
                    // In case of error, show the error to user. Extract error code.
                    int errorCode = ByteConv.ToU16(RawRead.Data, 0);
                    string errorString = "Error: ";
                    switch (errorCode)
                    {
                        case 0x105:
                            return errorString + "Parameter to command is invalid";
                        case 0x109:
                            return errorString + "Unimplemented feature";
                        case 0x404:
                            return errorString + "No data could be read from a tag";
                        case 0x406:
                            return errorString + "Tag write operation failed";
                        case 0x409:
                            return errorString + "Invalid address in tag address space";
                        case 0x40A:
                            return errorString + "General tag error";
                        case 0x411:
                            return errorString + "Invalid amount of data provided";
                        default:
                            return errorString + ReaderCodeException.faultCodeToMessage(errorCode);
                            //return errorString + "Unknown error code";
                    }
                }
                else
                {
                    return ByteFormat.ToHex(RawRead.Data, "", " ");
                }
            }
        }
        public string Frequency
        {
            get {
                if (RawRead.Frequency == 0)
                {
                    return " ";
                }
                else
                {
                    return RawRead.Frequency.ToString();
                }
            }
        }
        public string Phase
        {
            get
            {
                if (RawRead.Phase == 0)
                {
                    return " ";
                }
                else
                {
                    return RawRead.Phase.ToString();
                }
            }
        }
        public bool Checked
        {
            get { return dataChecked; }
            set
            {
                dataChecked = value;
            }
        }

        //public string BrandID { 
        //    get { return RawRead.BRAND_IDENTIFIER;}
        //}

        public string GPIO
        {
            get
            {
                string gpi = "IN:", gpo = "OUT:";
                if (RawRead.GPIO != null)
                {
                    foreach (GpioPin item in RawRead.GPIO)
                    {
                        gpi += " " + item.Id + "-" + (item.Output ? "H" : "L");
                        gpo += " " + item.Id + "-" + (item.High ? "H" : "L");
                    }
                }
                else
                {
                    gpi = gpo = " ";
                }
                return (gpi + "\r" + gpo);
            }
        }

        public Object TagType
        {
            get
            {
                switch (Protocol)
                {
                    case TagProtocol.LF125KHZ:
                        return ((Lf125khz.TagType)RawRead.TagType).ToString().Replace('_', ' ');
                    case TagProtocol.LF134KHZ:
                        return ((Lf134khz.TagType)RawRead.TagType).ToString().Replace('_', ' ');
                    case TagProtocol.ISO14443A:
                        return ((Iso14443a.TagType)RawRead.TagType).ToString().Replace('_',' ');
                    //case TagProtocol.ISO14443B:
                    //    break;
                    case TagProtocol.ISO15693:
                        return ((Iso15693.TagType)RawRead.TagType).ToString().Replace('_', ' ');
                    //case TagProtocol.ISO180003M3:
                    //    break;
                    //case TagProtocol.ISO180006B:
                    //    break;
                    //case TagProtocol.ISO180006B_UCODE:
                    //    break;
                    //case TagProtocol.ISO18092:
                    //    break;
                    //case TagProtocol.NONE:
                    //    break;
                    default:
                        return RawRead.TagType;
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventArgs td = new PropertyChangedEventArgs(name);
            try
            {

                if (null != PropertyChanged)
                {
                    PropertyChanged(this, td);
                }
            }
            finally
            {
                td = null;
            }
        }

        #endregion
    }

    public class TagReadRecordBindingList : SortableBindingList<TagReadRecord>
    {
        protected override Comparison<TagReadRecord> GetComparer(PropertyDescriptor prop)
        {
            Comparison<TagReadRecord> comparer = null;
            switch (prop.Name)
            {
                case "TimeStamp":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return DateTime.Compare(a.TimeStamp, b.TimeStamp);
                    });
                    break;
                case "SerialNumber":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return (int)(a.SerialNumber - b.SerialNumber);
                    });
                    break;
                case "ReadCount":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return a.ReadCount - b.ReadCount;
                    });
                    break;
                case "Antenna":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return a.Antenna - b.Antenna;
                    });
                    break;
                case "Protocol":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.Protocol.ToString(), b.Protocol.ToString());
                    });
                    break;
                case "RSSI":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return a.RSSI - b.RSSI;
                    });
                    break;
                case "EPC":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.EPC, b.EPC);
                    });
                    break;
                case "EPCInASCII":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.EPCInASCII, b.EPCInASCII);
                    });
                    break;
                case "EPCInReverseBase36":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.EPCInReverseBase36, b.EPCInReverseBase36);
                    });
                    break;
                case "DataInASCII":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.DataInASCII, b.DataInASCII);
                    });
                    break;
                //case "DataInReverseBase36":
                //    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                //    {
                //        return String.Compare(a.DataInReverseBase36, b.DataInReverseBase36);
                //    });
                //    break;
                case "Data":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        return String.Compare(a.Data, b.Data);
                    });
                    break;
                case "Frequency":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        if (a.Frequency != " " && b.Frequency != " ")
                        {
                            return int.Parse(a.Frequency) - int.Parse(b.Frequency);
                        }
                        else
                        {
                            return 0;
                        }
                    });
                    break;
                case "Phase":
                    comparer = new Comparison<TagReadRecord>(delegate(TagReadRecord a, TagReadRecord b)
                    {
                        if (a.Phase != " " && b.Phase != " ")
                        {
                            return int.Parse(a.Phase) - int.Parse(b.Phase);
                        }
                        else
                        {
                            return 0;
                        }
                    });
                    break;
            }
            return comparer;
        }
    }

    public class TagDatabase
    {
        /// <summary>
        /// TagReadData model (backs data grid display)
        /// </summary>
        TagReadRecordBindingList _tagList = new TagReadRecordBindingList();

        /// <summary>
        /// Cache unique by data checkbox status set in read / write options | enable filter 
        /// </summary>
        public bool chkbxUniqueByData = false;

        /// <summary>
        /// Cache unique by Antenna checkbox status set in Display options
        /// </summary>
        public bool chkbxUniqueByAntenna = false;

        /// <summary>
        /// Cache unique by Frequency checkbox status set in Display options
        /// </summary>
        public bool chkbxUniqueByFrequency = false;

        /// <summary>
        /// Cache show failed data reads checkbox status set in read / write options | enable filter 
        /// </summary>
        public bool chkbxShowFailedDataReads = false;

        /// <summary>
        /// EPC index into tag list
        /// </summary>
        Dictionary<string, TagReadRecord> EpcIndex = new Dictionary<string, TagReadRecord>();

        static long UniqueTagCounts = 0;
        static long TotalTagCounts = 0;

        public TagDatabase()
        {
            // GUI can't keep up with fast updates, so disable automatic triggers
            _tagList.RaiseListChangedEvents = false;
        }

        public TagReadRecordBindingList TagList
        {
            get { return _tagList; }
        }
        public long UniqueTagCount
        {
            get { return UniqueTagCounts; }
        }
        public long TotalTagCount
        {
            get { return TotalTagCounts; }
        }

        public void Clear()
        {
            EpcIndex.Clear();
            UniqueTagCounts = 0;
            TotalTagCounts = 0;
            _tagList.Clear();
            // Clear doesn't fire notifications on its own
            _tagList.ResetBindings();
        }

        public void Add(TagReadData addData)
        {
            lock (new Object())
            {
                string key = null;

                if (chkbxUniqueByData)
                {
                    if (true == chkbxShowFailedDataReads)
                    {
                        //key = addData.EpcString + ByteFormat.ToHex(addData.Data, "", " ");
                        // When CHECKED - Add the entry to the database. This will result in
                        // potentially two entries for every tag: one with the requested data and one without.
                        if (addData.Data.Length > 0)
                        {
                            key = addData.EpcString + ByteFormat.ToHex(addData.Data, "", " ");
                        }
                        else
                        {
                            key = addData.EpcString + "";
                        }

                    }
                    else if ((false == chkbxShowFailedDataReads) && (addData.Data.Length == 0))
                    {
                        // When UNCHECKED (default) - If the embedded read data fails (data.length==0) then don't add the entry to
                        // the database, thus it won't be displayed.
                        return;
                    }
                    else
                    {
                        key = addData.EpcString + ByteFormat.ToHex(addData.Data, "", " ");
                    }
                }
                else
                {
                    key = addData.EpcString; //if only keying on EPCID
                }
                //if (chkbxUniqueByAntenna)
                //{
                //    key += addData.Antenna.ToString();
                //}
                //if (chkbxUniqueByFrequency)
                //{
                //    key += addData.Frequency.ToString();
                //}

                UniqueTagCounts = 0;
                TotalTagCounts = 0;

                if (!EpcIndex.ContainsKey(key))
                {
                    TagReadRecord value = new TagReadRecord(addData);
                    value.SerialNumber = (uint)EpcIndex.Count + 1;
                    _tagList.Add(value);
                    EpcIndex.Add(key, value);
                    //Call this method to calculate total tag reads and unique tag read counts 
                    UpdateTagCountTextBox(EpcIndex);
                }
                else
                {
                    EpcIndex[key].Update(addData);
                    UpdateTagCountTextBox(EpcIndex);
                }
            }
        }

        //Calculate total tag reads and unique tag reads.
        public void UpdateTagCountTextBox(Dictionary<string, TagReadRecord> EpcIndex)
        {
            UniqueTagCounts += EpcIndex.Count;
            TagReadRecord[] dataRecord = new TagReadRecord[EpcIndex.Count];
            EpcIndex.Values.CopyTo(dataRecord, 0);
            TotalTagCounts = 0;
            for (int i = 0; i < dataRecord.Length; i++)
            {
                TotalTagCounts += dataRecord[i].ReadCount;
            }
        }

        public void AddRange(ICollection<TagReadData> reads)
        {
            foreach (TagReadData read in reads)
            {
                Add(read);
            }
        }

        /// <summary>
        /// Manually release change events
        /// </summary>
        public void Repaint()
        {
            _tagList.RaiseListChangedEvents = true;

            //Causes a control bound to the BindingSource to reread all the items in the list and refresh their displayed values.
            _tagList.ResetBindings();

            _tagList.RaiseListChangedEvents = false;
        }

        /// <summary>
        /// Generates a random string with the given length
        /// </summary>
        /// <param name="size">Size of the string</param>
        /// <param name="lowerCase">If true, generate lowercase string</param>
        /// <returns>Random string</returns>
        private string RandomString(int size, bool lowerCase)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }
    }
}