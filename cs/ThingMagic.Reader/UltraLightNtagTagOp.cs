using System;
using System.Collections.Generic;
using System.Text;


namespace ThingMagic
{
    /// <summary>
    /// Class for all Ultralight Ntag standard tagops
    /// </summary>
    public class UltraLightNtagTagOp: ExtendedTagOperations
    {
        #region Fields
        /// <summary>
        /// class to hold Ultralight N tag read data info
        /// </summary>
        public UltraLightNtagTagOpReadData readData;

        /// <summary>
        /// class to hold Ultralight N tag write data info
        /// </summary>
        public UltraLightNtagTagOpWriteData writeData;

        #endregion

        /// <summary>
        /// Default constructor which initializes readData and writeData 
        /// </summary>
        public UltraLightNtagTagOp()
        {
            this.readData = new UltraLightNtagTagOpReadData();
            this.writeData = new UltraLightNtagTagOpWriteData();
        }
    }

    /// <summary>
    /// class to hold Ultralight N tag read data info
    /// </summary>
    public class UltraLightNtagTagOpReadData
    {
        /** Sub option to read */
        public byte subCmd = 0x00;
    }

    /// <summary>
    /// class to hold Ultralight N tag write data info
    /// </summary>
    public class UltraLightNtagTagOpWriteData
    {
        /** Sub option to write */
        public byte subCmd = 0x00;
    }

    /// <summary>
    /// UltraLightNtagCmd
    /// </summary>
    #region UltraLightNtagCmd

    public enum UltraLightNtagCmd
    {
        /// <summary>
        /// None
        /// </summary>
        NONE = 0x00,
        /// <summary>
        /// Fast read
        /// </summary>
        FAST_READ = 0x01,
        /// <summary>
        /// Read
        /// </summary>
        READ = 0x02,
        /// <summary>
        ///  Write
        /// </summary>
        WRITE = 0x02,
        /// <summary>
        ///  Get version
        /// </summary>
        GET_VERSION = 0x03
    }

    #endregion
}
