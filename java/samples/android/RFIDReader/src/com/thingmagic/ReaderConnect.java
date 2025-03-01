package com.thingmagic;

import com.thingmagic.util.LoggerUtil;

import android.util.Log;
import java.util.EnumSet;
import java.util.Set;


public class ReaderConnect {
	private static String TAG = "ReaderConnect";
	public static void setTrace(Reader r, String args[]) {
		System.out.println("setTrace " );
		if (args[0].toLowerCase().equals("on")) {
			r.addTransportListener(r.simpleTransportListener);
		}
	}
	
	public static Reader connect(String uriString) throws Exception 
	{
		Reader reader = null;
		try 
		{
			reader = Reader.create(uriString);
			reader.connect();
			//setTrace(reader, new String[] {"on"});
			if (Reader.Region.UNSPEC == (Reader.Region) reader
					.paramGet("/reader/region/id"))
			{
				Reader.Region[] supportedRegions = (Reader.Region[]) reader
						.paramGet(TMConstants.TMR_PARAM_REGION_SUPPORTEDREGIONS);
				if (supportedRegions.length < 1) 
				{
					throw new Exception(
							"Reader doesn't support any regions");
				} 
				else 
				{
					reader.paramSet("/reader/region/id", supportedRegions[0]);
				}
			}
			//Setting metadata flags
			Set<TagReadData.TagMetadataFlag> flagSet = EnumSet.of(TagReadData.TagMetadataFlag.ANTENNAID ,TagReadData.TagMetadataFlag.DATA,
					TagReadData.TagMetadataFlag.FREQUENCY, TagReadData.TagMetadataFlag.GEN2_LF,
					TagReadData.TagMetadataFlag.GEN2_Q,    TagReadData.TagMetadataFlag.GEN2_TARGET,
					TagReadData.TagMetadataFlag.GPIO_STATUS, TagReadData.TagMetadataFlag.PHASE,
					TagReadData.TagMetadataFlag.PROTOCOL, TagReadData.TagMetadataFlag.READCOUNT,
					TagReadData.TagMetadataFlag.RSSI, TagReadData.TagMetadataFlag.TIMESTAMP);
			reader.paramSet("/reader/metadata", flagSet);
		}catch(Exception ex){
			throw ex;
		}
		return reader;
	}
}
