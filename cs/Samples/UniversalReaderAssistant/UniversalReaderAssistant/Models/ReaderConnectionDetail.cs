using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.IO.Ports;
using System.Collections.ObjectModel;

namespace ThingMagic.URA2.Models
{
    static class ISO14443AMemoryLayout
    {
        /// <summary>
        /// Get the Block count for ISO14443A tags
        /// </summary>
        /// <param name="tag">Tag Type</param>
        /// <returns>Return the block count in natural number</returns>
        public static int GetBlockCount(Iso14443a.TagType tag)
        {
            int blockCount = 0;
            switch (tag)
            {
                case Iso14443a.TagType.AUTO_DETECT:
                    break;
                case Iso14443a.TagType.MIFARE_DESFIRE:
                    break;
                case Iso14443a.TagType.MIFARE_MINI:
                    break;
                case Iso14443a.TagType.MIFARE_CLASSIC:
                    blockCount = 64;
                    break;
                case Iso14443a.TagType.MIFARE_PLUS:
                    break;
                case Iso14443a.TagType.MIFARE_ULTRALIGHT:
                    blockCount = 15;
                    break;
                case Iso14443a.TagType.NTAG:
                    break;
                default:

                    break;
            }
            return blockCount;
        }
        /// <summary>
        /// Get the block size for ISO14443A
        /// </summary>
        /// <param name="tag">Tag Type</param>
        /// <returns>Returns tag block size</returns>
        public static int GetBlockSize(Iso14443a.TagType tag)
        {
            int blockSize = 0;
            switch (tag)
            {
                case Iso14443a.TagType.AUTO_DETECT:
                    break;
                case Iso14443a.TagType.MIFARE_DESFIRE:
                    break;
                case Iso14443a.TagType.MIFARE_MINI:
                    break;
                case Iso14443a.TagType.MIFARE_CLASSIC:
                    blockSize = 16;
                    break;
                case Iso14443a.TagType.MIFARE_PLUS:
                    break;
                case Iso14443a.TagType.ULTRALIGHT_NTAG:
                case Iso14443a.TagType.MIFARE_ULTRALIGHT:
                    blockSize = 4;
                    break;
                case Iso14443a.TagType.NTAG:
                    blockSize = 4;
                    break;
                default:
                    break;
            }
            return blockSize;
        }
    }

    /// <summary>
    /// Contains the Memory Architecture tags
    /// </summary>
    public static class TagMemoryArchitecture
    {
        public static List<byte> Lockstatus = new List<byte>();


        public static bool WriteTagMemory(uint address, byte[] data, string uid, Reader reader)
        {
            bool flag = false;
            byte[] _uid = ByteFormat.FromHex(uid);
            TagFilter filter = new Select_UID((byte)(_uid.Length * 8), _uid);
            MemoryType type = MemoryType.TAG_MEMORY;
            WriteMemory writeOp = new WriteMemory(type, address, data);
            try
            {
                reader.ExecuteTagOp(writeOp, filter);
                flag = true;
            }
            catch (Exception)
            {
                flag = false;
                //throw;
            }
            return flag;
        }

        public static byte[] ReadTagMemory(uint address, string uid, Reader reader)
        {
            byte[] _uid = ByteFormat.FromHex(uid);
            TagFilter filter = new Select_UID((byte)(_uid.Length * 8), _uid);
            MemoryType type = MemoryType.TAG_MEMORY;

            ReadMemory read = new ReadMemory(type, address, 1);
            byte[] dataRead = (byte[])reader.ExecuteTagOp(read, filter);

            return dataRead;
        }

        public static Dictionary<string, string> UIDBreakdown(string uid)
        {
            Dictionary<string, string> uidBreakDown = new Dictionary<string, string>();
            uidBreakDown.Add("Unique Identifier", (uid.Substring(0, 2)));

            switch (uid.Substring(2, 2))
            {
                case "04":
                    uidBreakDown.Add("04", "NXP");
                    break;
                case "07":
                    uidBreakDown.Add("07", "TI");
                    break;
                case "05":
                    uidBreakDown.Add("05", "Infineon");
                    break;
                default:
                    uidBreakDown.Add(uid.Substring(2, 2), "Unknown");
                    break;
            }

            switch (uid.Substring(4, 2))
            {
                case "01":
                    uidBreakDown.Add("01", "ICODE SLI");
                    break;
                case "02":
                    uidBreakDown.Add("02", "ICODE SLI-S");
                    break;
                case "03":
                    uidBreakDown.Add("03", "ICODE SLI-L");
                    break;
                default:
                    uidBreakDown.Add(uid.Substring(4, 2), "Unknown");
                    break;
            }

            uidBreakDown.Add(uid.Substring(6, (uid.Length - 6)), "");
            return uidBreakDown;
        }


        /// <summary>
        /// Get the Memory Architecture of the ISO14443A tags.
        /// </summary>
        /// <param name="tag">Tag Type</param>
        /// <returns>Returns memory organisation in String Arry</returns>
        public static string[] Get14443AMemoryLayout(Iso14443a.TagType tag)
        {
            return new string[] { (ISO14443AMemoryLayout.GetBlockSize(tag)).ToString(), (ISO14443AMemoryLayout.GetBlockCount(tag)).ToString() };//right now we are getting only block size.
        }

        public static List<byte> Get14443AMemory(Reader reader, Iso14443a.TagType tag, ref uint blockCount, string UID)
        {
            List<byte> data = new List<byte>();
            byte[] _uid = ByteFormat.FromHex(UID);
            TagFilter tagFilter = new Select_UID((byte)(_uid.Length * 8), _uid);
            string[] block = Get14443AMemoryLayout(tag);

            data = GetTagData(reader, tagFilter, Convert.ToUInt32(block[0]), 0, ref blockCount);
            return data;
        }

        /// <summary>
        /// Get the tag dat of the ISO15693 protocols tag.
        /// </summary>
        /// <param name="reader">Connected reader</param>
        /// <param name="tag">Selected tag of ISO15693</param>
        /// <param name="blockCount">No of memory blocks in the tag</param>
        /// <param name="blockSize">Size of each memory block</param>
        /// <param name="UID">UID of the tag for data to be read</param>
        /// <returns>Return list of data</returns>
        public static List<byte> Get15693Memory(Reader reader, Iso15693.TagType tag, uint blockCount, uint blockSize, string UID)
        {
            List<byte> data = new List<byte>();
            byte[] _uid = ByteFormat.FromHex(UID);
            TagFilter tagFilter = new Select_UID((byte)(_uid.Length * 8), _uid);

            uint actualBlockCount = 0;
            data = GetTagData(reader, tagFilter, blockSize, blockCount, ref actualBlockCount);

            return data;
        }

        /// <summary>
        /// Get the data from the tag 
        /// </summary>
        /// <param name="reader">Connected reader</param>
        /// <param name="filter">Tag Filter</param>
        /// <param name="blockSize">Size of each memory block</param>
        /// <param name="blockCount">No of memory blocks in the tag</param>
        /// <param name="actualBlockCount">Returns the actual block count of the tag</param>
        /// <returns>Return list of data </returns>
        private static List<byte> GetTagData(Reader reader, TagFilter filter, uint blockSize, uint blockCount, ref uint actualBlockCount)
        {
            List<byte> data = new List<byte>();
            //M3e module supports 220 bytes of data for standalone tag operation. (Block count * Block Size) should always less than 220 byte. Accordingly block count size should be taken for Iso14443A tagtypes as we don't get any info from module.
            byte blockCountSize = (byte)((blockSize == 16) ? 13 : 55);//RIGHT NOW WE HAVE ONLY BLOCK SIZE A 4 BYTE AND 16 BYTE SO WE ARE SENDING 55 BLOCK AND 13 BLOCK.
            MemoryType readType = MemoryType.TAG_MEMORY;
            uint address = 0;
            Lockstatus.Clear();
            if (blockCount == 0)//this section is for ISO14443A
            {
                try
                {
                    while (true)
                    {
                        ReadMemory readData = new ReadMemory(readType, address, blockCountSize);
                        data.AddRange((byte[])reader.ExecuteTagOp(readData, filter));
                        address += blockCountSize;
                        for (int i = 0; i < blockCountSize; i++)
                        {
                            Lockstatus.Add(00);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("A command was received to write to an invalid address in the tag data address space.") || ex.Message.Contains("04A7") || ex.Message.Contains("A Read Tag ID or Data command was sent but did not succeed.") || ex.Message.Contains("Tag Authentication Failed."))
                    {
                        try
                        {
                            while (true)
                            {
                                try
                                {
                                    ReadMemory readData = new ReadMemory(readType, address, 1);
                                    data.AddRange((byte[])reader.ExecuteTagOp(readData, filter));
                                    address += 1;
                                    Lockstatus.Add(00);
                                }
                                catch (Exception ex2)
                                {
                                    if (ex2.Message.Contains("04A7") || ex2.Message.Contains("A Read Tag ID or Data command was sent but did not succeed.") || ex2.Message.Contains("Tag Authentication Failed."))
                                    {
                                        byte[] defaultData = new byte[blockSize];
                                        for (int i = 0; i < defaultData.Length; i++)
                                        {
                                            defaultData[i] = 00;
                                        }
                                        data.AddRange(defaultData);
                                        address += 1;
                                        Lockstatus.Add(01);
                                        continue;
                                    }
                                    else
                                    {
                                        throw ex2;
                                    }
                                }
                            }
                        }
                        catch (Exception ex1)
                        {
                            if (ex1.Message.Contains("A command was received to write to an invalid address in the tag data address space."))
                            {
                                actualBlockCount = address;
                                return data;
                            }
                            else
                            {
                                throw ex1;
                            }
                        }
                        actualBlockCount = address;
                        return data;
                    }

                }
            }
            else// this section is for IOS15693
            {
                if ((blockCount * blockSize) <= 220)//it finds can we bring the whole data in one command.
                {
                    ReadMemory readData = new ReadMemory(readType, address, (byte)blockCount);
                    //BlockReadErrorHandle(reader, filter, blockCount, 0, blockSize);
                    try
                    {
                        data.AddRange((byte[])reader.ExecuteTagOp(readData, filter));
                    }
                    catch (Exception ex)//proper exception code need to be added
                    {
                        if (ex.Message.Contains("04A7"))
                        {
                            data.AddRange(BlockReadErrorHandle(reader, filter, (byte)blockCount, 0, blockSize));
                        }
                        //throw;
                    }

                    return data;
                }
                else
                {
                    UInt32 startAddress = 0;
                    bool exitRead = true;
                    byte blockLength = (byte)(220 / blockSize);
                    try
                    {
                        while (exitRead)
                        {
                            if (blockCount > blockLength)
                            {
                                blockCount -= blockLength;
                                try
                                {
                                    ReadMemory readData = new ReadMemory(readType, startAddress, blockLength);
                                    data.AddRange((byte[])reader.ExecuteTagOp(readData, filter));
                                    startAddress += blockLength;
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.Contains("04A7"))
                                    {
                                        data.AddRange(BlockReadErrorHandle(reader, filter, (byte)blockCount, startAddress, blockSize));
                                        startAddress += blockCount;
                                        blockCount = (uint)(blockLength);
                                    }
                                }
                            }
                            else if (blockCount <= blockLength)
                            {
                                ReadMemory readData = new ReadMemory(readType, startAddress, (byte)blockCount);
                                data.AddRange((byte[])reader.ExecuteTagOp(readData, filter));
                                blockCount -= blockCount;
                            }

                            if (blockCount <= 0)
                            {
                                exitRead = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("04A7"))
                        {
                            data.AddRange(BlockReadErrorHandle(reader, filter, (byte)blockCount, startAddress, blockSize));
                        }
                    }
                }
            }
            actualBlockCount = address;//(byte)(address-4);
            return data;
        }

        /// <summary>
        /// Call this function when read exception is seen
        /// </summary>
        /// <param name="reader">Connected reader</param>
        /// <param name="filter">Tag filter</param>
        /// <param name="blockCount">Blocks to read</param>
        /// <param name="address">Starting address</param>
        /// <param name="blockSize">Size of a block</param>
        /// <returns>Return List of byte data</returns>
        private static List<byte> BlockReadErrorHandle(Reader reader, TagFilter filter, byte blockCount, uint address, uint blockSize)
        {
            List<byte> data = new List<byte>();
            for (int i = (int)address; i < blockCount; i++)
            {
                data.AddRange(ReadData(reader, filter, (uint)i, blockSize));
            }
            return data;
        }

        /// <summary>
        /// Get data after the exception
        /// </summary>
        /// <param name="reader">Connected reader</param>
        /// <param name="filter">Tag filter</param>
        /// <param name="address">Start address</param>
        /// <param name="blockSize">Size of each block</param>
        /// <returns>Returns byte array</returns>
        private static byte[] ReadData(Reader reader, TagFilter filter, uint address, uint blockSize)
        {
            byte[] data = new byte[blockSize];
            MemoryType readType = MemoryType.TAG_MEMORY;
            ReadMemory readData = new ReadMemory(readType, address, 1);
            try
            {
                //throw new Exception("1584");
                data = (byte[])reader.ExecuteTagOp(readData, filter);
            }
            catch (Exception ex)//add the correct exception
            {
                if (ex.Message.Contains("04A7"))
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = 00;
                    }
                }
                //throw;
            }
            return data;
        }

        /// <summary>
        /// Get the Memory Architecture of the ISO15693 tags.
        /// </summary>
        /// <param name="reader">Connected reader</param>
        /// <param name="tag">ISO15693 Tag type of the seclected tag</param>
        /// <param name="uid">UID of the selected tag</param>
        /// <returns>Returns memory organisation in String Array</returns>
        public static Dictionary<string, uint> Get15693AMemoryLayout(Reader reader, Iso15693.TagType tag, string uid)
        {
            Dictionary<string, uint> tagMemorystruct = new Dictionary<string, uint>();

            if ((tag == Iso15693.TagType.PICOPASS) || (tag == Iso15693.TagType.HID_ICLASS_SE))
            {
              tagMemorystruct.Add("Block Size", 8);
              tagMemorystruct.Add("Block Count", 255);
            }
            else
            {
              //paramget to get the memory organisation
              //we will have the object which contains the tag info

              byte[] _uid = ByteFormat.FromHex(uid);
              TagFilter filter = new Select_UID((byte)(_uid.Length * 8), _uid);


              //TagFilter filter=new Select_TagType((UInt32)tag);
              MemoryType readType = MemoryType.TAG_INFO;
              ReadMemory systemInfoOp = new ReadMemory(readType, 0, 0);
              byte[] systemInfo = (byte[])reader.ExecuteTagOp(systemInfoOp, filter);

              #region Info Flag
              byte infoFlag = systemInfo[0];
              int systemInfoOffSet = 9;
              if ((infoFlag & 0x0001) == 0x0001)
              {
                byte dsfid = systemInfo[systemInfoOffSet++];
                tagMemorystruct.Add("DSFID", dsfid);
              }
              if ((infoFlag & 0x0002) == 0x0002)
              {
                byte afi = systemInfo[systemInfoOffSet++];
                tagMemorystruct.Add("AFI", afi);
              }
              if ((infoFlag & 0x0004) == 0x0004)
              {
                tagMemorystruct.Add("Block Size", systemInfo[systemInfoOffSet++]);
                tagMemorystruct.Add("Block Count", systemInfo[systemInfoOffSet++]);
              }
              #endregion
            }
            return tagMemorystruct;
        }


        public static List<byte> MemoryLockStatus(Reader reader, uint blockLength, TagProtocol protocol, string uid)
        {
            List<byte> lockStatus = new List<byte>();
            TagFilter filter;
            byte[] _uid = ByteFormat.FromHex(uid);

            switch (protocol)
            {
                case TagProtocol.ISO14443A:
                    break;
                case TagProtocol.ISO14443B:
                    break;
                case TagProtocol.ISO15693:
                    //Iso15693.TagType tag = (Iso15693.TagType)tagType;//(Enum.GetName(typeof(Iso15693.TagType), tagType));
                    filter = new Select_UID((byte)(_uid.Length * 8), _uid);
                    MemoryType type = MemoryType.PROTECTION_SECURITY_STATUS;
                    ReadMemory blockStatus = new ReadMemory(type, 0, (byte)blockLength);
                    lockStatus.AddRange((byte[])reader.ExecuteTagOp(blockStatus, filter));
                    break;
                case TagProtocol.ISO180003M3:
                    break;
                case TagProtocol.ISO180006B:
                    break;
                case TagProtocol.ISO180006B_UCODE:
                    break;
                case TagProtocol.ISO18092:
                    break;
                case TagProtocol.LF125KHZ:
                    break;
                case TagProtocol.LF134KHZ:
                    break;
                case TagProtocol.NONE:
                    break;
                default:
                    break;
            }

            return lockStatus;
        }
    }

    public static class TagFamilyDatabase
    {
        public static string GetTagFamilyName(String tagType)
        {
            switch (tagType)
            {
                case "iCLASS SE":
                    return "HID Tag Family";
                case "DuoProx II":
                    return "HID Tag Family";
                case "ISOProx II":
                    return "HID Tag Family";
                case "MicroProx":
                    return "HID Tag Family";
                case "ProxKey III":
                    return "HID Tag Family";
                case "Prox":
                    return "HID Tag Family";
                case "VIGO":
                    return "HID Tag Family";
                case "Prox II":
                    return "HID Tag Family";
                case "MIFARE PLUS":
                    return "MIFARE Tag Family";
                case "MIFARE ULTRALIGHT":
                    return "MIFARE Tag Family";
                case "MIFARE CLASSIC":
                    return "MIFARE Tag Family";
                case "MIFARE DESFIRE":
                    return "MIFARE Tag Family";
                case "MIFARE MINI":
                    return "MIFARE Tag Family";
                case "EM4133":
                    return "EM Tag Family";
                case "EM4233":
                    return "EM Tag Family";
                case "EM4237":
                    return "EM Tag Family";
                case "ICODE DNA":
                    return "ICODE Tag Family";
                case "ICODE SLI":
                    return "ICODE Tag Family";
                case "ICODE SLI L":
                    return "ICODE Tag Family";
                case "ICODE SLI S":
                    return "ICODE Tag Family";
                case "ICODE SLIX":
                    return "ICODE Tag Family";
                case "ICODE SLIX L":
                    return "ICODE Tag Family";
                case "ICODE SLIX2":
                    return "ICODE Tag Family";
                case "ICODE SLIX S":
                    return "ICODE Tag Family";
                case "HITAG 1":
                    return "HITAG Tag Family";
                case "HITAG 2":
                    return "HITAG Tag Family";
                case "NTAG":
                    return "Other Tags";
                default:
                    return "Other Tags";
            }
        }
    }

    public class TagFamily : DependencyObject
    {
        public string Name { get; set; }
        public ObservableCollection<Tag> Members { get; set; }
    }

    public class Tag : DependencyObject
    {
        public ColumnSelectionForTagtype Name { get; set; }
    }

    public class ItemHelper : DependencyObject
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(ItemHelper), new PropertyMetadata(false, new PropertyChangedCallback(OnIsCheckedPropertyChanged)));
        private static void OnIsCheckedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((d is TagFamily) && (((bool?)e.NewValue).HasValue))
            {
                foreach (Tag tag in (d as TagFamily).Members)
                {
                    //tag.IsTagChecked = true;
                    ItemHelper.SetIsChecked(tag, (bool?)e.NewValue);
                }
            }

            if (d is Tag)
            {
                ItemHelper.CheckParent((d as Tag));
            }
        }

        public static void CheckParent(Tag d)
        {
            int check = ((d as Tag).GetValue(ItemHelper.ParentProperty) as TagFamily).Members.Where(x => ItemHelper.GetIsChecked(x) == true).Count();
            int uncheck = ((d as Tag).GetValue(ItemHelper.ParentProperty) as TagFamily).Members.Where(x => ItemHelper.GetIsChecked(x) == false).Count();
            if (check > 0 && uncheck > 0)
            {
                ItemHelper.SetIsChecked((d as Tag).GetValue(ItemHelper.ParentProperty) as DependencyObject, null);
                return;
            }
            if (check > 0)
            {
                ItemHelper.SetIsChecked((d as Tag).GetValue(ItemHelper.ParentProperty) as DependencyObject, true);
                return;
            }
            ItemHelper.SetIsChecked((d as Tag).GetValue(ItemHelper.ParentProperty) as DependencyObject, false);
        }

        public static void SetIsChecked(DependencyObject element, bool? IsChecked)
        {
            Tag p = element as Tag;
            if (p != null)
            {
                p.Name.IsTagChecked = (bool)IsChecked;
            }
            element.SetValue(ItemHelper.IsCheckedProperty, IsChecked);
        }

        public static bool? GetIsChecked(DependencyObject element)
        {
            return (bool?)element.GetValue(ItemHelper.IsCheckedProperty);
        }

        public static readonly DependencyProperty ParentProperty = DependencyProperty.RegisterAttached("Parent", typeof(object), typeof(ItemHelper));

        public static void SetParent(DependencyObject element, object Parent)
        {
            element.SetValue(ItemHelper.ParentProperty, Parent);
        }

        public static object GetParent(DependencyObject element)
        {
            return (object)element.GetValue(ItemHelper.ParentProperty);
        }
    }

    public class TagTypeComboBox
    {
        private ObservableCollection<ColumnSelectionForTagtype> _tagComboBox;

        public ObservableCollection<ColumnSelectionForTagtype> TagComboBox
        {
            get { return _tagComboBox; }
            set { _tagComboBox = value; }
        }


        public TagTypeComboBox(ObservableCollection<ColumnSelectionForTagtype> tagCheckBox)
        {
            TagComboBox = tagCheckBox;
        }
    }

    public class ColumnSelectionForTagButton : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _buttonNameTag;

        public string ButtonNameTag
        {
            get { return _buttonNameTag; }
            set
            {
                _buttonNameTag = value;
                NotifyPropertyChanged("ButtonNameTag");
            }
        }

        public ColumnSelectionForTagButton(string buttonTagName)
        {
            ButtonNameTag = buttonTagName;
        }
    }

    public class ColumnSelectionForTagtype : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _isTagChecked = false;
        public bool IsTagChecked
        {
            get
            {
                return _isTagChecked;
            }
            set
            {
                _isTagChecked = value;
                NotifyPropertyChanged("IsTagChecked");
            }
        }
        private string _tagName;
        public string TagName
        {
            get
            {
                return _tagName;
            }
            set
            {
                _tagName = value;
            }
        }

        public ColumnSelectionForTagtype(string tagName, bool isTagChecked)
        {
            TagName = tagName;
            IsTagChecked = isTagChecked;
        }
    }

    public class ReaderConnectionDetail
    {

        private static string readername;
        public static string ReaderName
        {
            get { return readername; }
            set { readername = value; }
        }

        private static string readermodel;
        public static string ReaderModel
        {
            get { return readermodel; }
            set { readermodel = value; }
        }

        private static string baudrate;
        public static string BaudRate
        {
            get { return baudrate; }
            set { baudrate = value; }
        }

        private static string readerType;
        public static string ReaderType
        {
            get { return readerType; }
            set { readerType = value; }
        }

        private static string region;
        public static string Region
        {
            get { return region; }
            set { region = value; }
        }

        private static string antenna;
        public static string Antenna
        {
            get { return antenna; }
            set { antenna = value; }
        }

        private static string protocol;
        public static string Protocol
        {
            get { return protocol; }
            set { protocol = value; }
        }

        static ReaderConnectionDetail()
        { }

        public ReaderConnectionDetail()
        { }

        public static void Display()
        {
            MessageBox.Show(ReaderName + " " + BaudRate + " " + ReaderType + " " + Region);
        }
    }
}