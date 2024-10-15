using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThingMagic;
using ThingMagic.URA2.BL;

namespace ThingMagic.URA2.ViewModel
{
    class TagResultsViewModel
    {


        public void ReadTags(ref TagDatabase tagdb, Reader objReader, int readTimeOut, out TagReadData[] trd)
        {
            //fontSize_TextChanged_1(sender, e);
            //Change the datagrid data font size
            //dataGrid1.FontSize = Convert.ToDouble(txtfontSize.Text);
            //GUIturnoffWarning();
            //simpleReadPlans.Clear();
            DateTime now = DateTime.Now;
            DateTime endTime = now.AddMilliseconds(readTimeOut);
            trd = null;
            while (now <= endTime)
            {
                TimeSpan totalTagFetchTime = new TimeSpan();
                DateTime tagFetchStartTime;
                try
                {
                    //FontFamily fFamily = new FontFamily("Arial");
                    //dataGrid1.ColumnHeaderStyle 
                    //Font objFont = new Font(fFamily, Convert.ToInt64(fontSize.Text));
                    //tagResultsGrid.Font = objFont;

                    trd = objReader.Read(readTimeOut);
                    tagdb.AddRange(trd);
                }
                catch(ReaderException re)
                {
                    if (objReader is SerialReader)
                    {
                        if (re is FAULT_TAG_ID_BUFFER_FULL_Exception)
                        {
                            tagFetchStartTime = DateTime.Now;
                            trd = ((SerialReader)objReader).GetAllTagReadsFromBuffer();
                            tagdb.AddRange(trd);
                            totalTagFetchTime = DateTime.Now - tagFetchStartTime;
                            objReader.notifyExceptionListeners(re);
                        }
                        else
                        {
                            objReader.notifyExceptionListeners(re);
                            throw re;
                        }
                    }
                    else
                    {
                        throw re;
                    }

                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    now = DateTime.Now - totalTagFetchTime;
                }
            }//end of while
        }

    }
}
