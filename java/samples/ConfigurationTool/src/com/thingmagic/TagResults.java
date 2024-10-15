package com.thingmagic;

public class TagResults 
{
    private String deviceId;
    private String epc;
    private String time;
    private String rssi;
    private String count;
    private String antenna;
    private String protocol;
    private String frequency;
    private String phase;
    private String data;

    public TagResults(String deviceId, String epc,String time,String rssi,String count,String antenna,String protocol,String frequency,String phase)
    {
        this.deviceId=deviceId;
        this.epc=epc;
        this.time= time;
        this.rssi= rssi;
        this.count= count;
        this.antenna = antenna;                
        this.protocol = protocol;
        this.frequency = frequency;
        this.phase = phase;
    }
    
    public TagResults(String deviceId, String epc,String data,String time,String rssi,String count,String antenna,String protocol,String frequency,String phase)
    {
        this.deviceId=deviceId;
        this.epc=epc;
        this.data=data;
        this.time= time;
        this.rssi= rssi;
        this.count= count;
        this.antenna = antenna;
        this.protocol = protocol;
        this.frequency = frequency;
        this.phase = phase;
    }
    
    public String getDeviceId()
    {
        return deviceId;
    }
    public void setDeviceId(String deviceId)
    {
        this.deviceId=deviceId;
    }
    
    public String getEpc()
    {
        return epc;
    }
    public void setEpc(String epc)
    {
        this.epc=epc;
    }
    
    public String getData()
    {
        return data;
    }

    public void setData(String data)
    {
        this.data = data;
    }
    
    public String getTime()
    {
        return time;
    }
    public void setTime(String time)
    {
        this.time=time;
    }
    
    public String getRssi()
    {
        return rssi;
    }
    public void setRssi(String rssi)
    {
        this.rssi = rssi;
    }
    
    public String getCount()
    {
        return count;
    }
    public void setCount(String count)
    {
        this.count=count;
    }
    
    public String getAntenna()
    {
        return antenna;
    }                
    public void setAntenna(String antenna)
    {
        this.antenna=antenna;
    }
    
    public String getProtocol()
    {
        return protocol;
    }                
    public void setProtocol(String protocol)
    {
        this.protocol=protocol;
    }
    
    public String getFrequency()
    {
        return frequency;
    }                
    public void setFrequency(String frequency)
    {
        this.frequency=frequency;
    }
    
    public String getPhase()
    {
        return phase;
    }                
    public void setPhase(String phase)
    {
        this.phase=phase;
    }
    
}
