/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.thingmagic;

import com.fazecast.jSerialComm.SerialPort;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.text.SimpleDateFormat;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.HashMap;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;


public class ElaraJSONParser {

    private String type;
    //Parse GetInfo JSON command response
    public HashMap parseGetInfo(String response) throws JSONException
    {
        HashMap<String,String> hmap = new HashMap<>();
        ArrayList<String> regionsList = new ArrayList<>();
        JSONObject jsono = new JSONObject(response);
        String command = (String) jsono.get("Report");
        int errID = (Integer) jsono.get("ErrID");
        JSONArray freqRegSet = (JSONArray) jsono.get("FreqRegSet");
        for(int i=0; i < freqRegSet.length(); i++){
           regionsList.add(freqRegSet.get(i).toString());
        }
        int rdrBufSize = (Integer) jsono.get("RdrBufSize");
        String rdrModel = (String) jsono.get("RdrModel");
        String rdrSN = (String) jsono.get("RdrSN");
        int rdrTemp = (Integer) jsono.get("RdrTemp");
        int rdrTempPA = (Integer) jsono.get("RdrTempPA");
        String version = (String) jsono.get("Version");
        JSONArray _interfaces = (JSONArray) jsono.get("_Interfaces");
        JSONObject _type = (JSONObject) _interfaces.get(0);
        type = (String) _type.get("_Type");
        JSONArray _sensors = (JSONArray) jsono.get("_Sensors");
        JSONObject _model = (JSONObject) _sensors.get(0);
        String model = (String) _model.get("_Model");
        String sn = (String) _model.get("_SN");
        String fVersion = (String) _model.get("_Version");
        hmap.put("report", command);
        hmap.put("ErrID", String.valueOf(errID));
        hmap.put("FreqRegSet", regionsList.toString());
        hmap.put("RdrBufSize", String.valueOf(rdrBufSize));
        hmap.put("RdrModel", rdrModel);
        hmap.put("rdrSN", rdrSN);
        hmap.put("rdrTemp", String.valueOf(rdrTemp));
        hmap.put("rdrTempPA", String.valueOf(rdrTempPA));
        hmap.put("version", version);
        hmap.put("type", type);
        hmap.put("model", model);
        hmap.put("sn",sn);
        hmap.put("fVersion",fVersion);
        return hmap;
    }

    //Parse GetInfo JSON command response
    public HashMap parseGetCfgWorkflowResponse(String response) throws JSONException
    {
        HashMap<String,String> hmap = new HashMap<>();
        double readPwr, writePwr;
        JSONObject json = new JSONObject(response);
        String command = (String) json.get("Report");
        int errID = (Integer) json.get("ErrID");
        String mode = json.getString("Mode");
        String mode_cfg = json.getString("Mode.Cfg");
        JSONObject parseWorkflowCfgs = new JSONObject(mode_cfg);
        if(parseWorkflowCfgs.get("ReadPwr").toString().contains("."))
        {
            readPwr = (Double)(parseWorkflowCfgs.get("ReadPwr"));
        }
        else
        {
            readPwr = ((Integer)parseWorkflowCfgs.get("ReadPwr")).doubleValue();
        }
        if(parseWorkflowCfgs.get("WritePwr").toString().contains("."))
        {
            writePwr = (Double)(parseWorkflowCfgs.get("WritePwr"));
        }
        else
        {
            writePwr = ((Integer)parseWorkflowCfgs.get("WritePwr")).doubleValue();
        }
        int session = parseWorkflowCfgs.getInt("Session");
        String target = parseWorkflowCfgs.getString("Target");
        String gen2Q = parseWorkflowCfgs.getString("Gen2Q");
        boolean gen2QEnable = parseWorkflowCfgs.getBoolean("Gen2QEnable");
        int gen2InitQVal = parseWorkflowCfgs.getInt("Gen2QInitVal");
        int blf = parseWorkflowCfgs.getInt("BLF");
        String dataEncoding = parseWorkflowCfgs.getString("DataEncoding");
        int rfOnTime = parseWorkflowCfgs.getInt("RFOnTime");
        int rfOffTime = parseWorkflowCfgs.getInt("RFOffTime");
        hmap.put("report", command);
        hmap.put("ErrID", String.valueOf(errID));
        hmap.put("Mode", mode);
        hmap.put("ReadPwr", String.valueOf(readPwr));
        hmap.put("WritePwr", String.valueOf(writePwr));
        hmap.put("Session", String.valueOf(session));
        hmap.put("Target", target);
        hmap.put("Gen2Q", gen2Q);
        hmap.put("Gen2QEnable", String.valueOf(gen2QEnable));
        hmap.put("Gen2QInitVal", String.valueOf(gen2InitQVal));
        hmap.put("BLF",String.valueOf(blf));
        hmap.put("DataEncoding",dataEncoding);
        hmap.put("RFOnTime",String.valueOf(rfOnTime));
        hmap.put("RFOffTime",String.valueOf(rfOffTime));
        return hmap;
    }

    //Parse GetCfg all JSON command response
    public HashMap parseGetCfgAllResponse(String response) throws JSONException
    {
        HashMap<String,String> hmap = new HashMap<>();
        int baudRateSet = 115200;
        double readPwr, writePwr;
        JSONObject json = new JSONObject(response);
        if(json.has("Report"))
        {
            String command = (String) json.get("Report");
            hmap.put("report", command);
        }
        if(json.has("ErrID"))
        {
            int errID = (Integer) json.get("ErrID");
            hmap.put("ErrID", String.valueOf(errID));
        }
        String mode;
        if(json.has("Mode"))
        {
            mode = json.getString("Mode");
            hmap.put("Mode", mode);
        }
        String mode_cfg;
        JSONObject parseWorkflowCfgs = new JSONObject();
        if(json.has("Mode.Cfg"))
        {
            mode_cfg = json.getString("Mode.Cfg");
            parseWorkflowCfgs = new JSONObject(mode_cfg);
        }
        if(parseWorkflowCfgs.has("ReadPwr") && parseWorkflowCfgs.get("ReadPwr").toString().contains("."))
        {
            readPwr = (Double)(parseWorkflowCfgs.get("ReadPwr"));
        }
        else
        {
            readPwr = ((Integer)parseWorkflowCfgs.get("ReadPwr")).doubleValue();
        }
        if(parseWorkflowCfgs.has("WritePwr") &&parseWorkflowCfgs.get("WritePwr").toString().contains("."))
        {
            writePwr = (Double)(parseWorkflowCfgs.get("WritePwr"));
        }
        else
        {
            writePwr = ((Integer)parseWorkflowCfgs.get("WritePwr")).doubleValue();
        }
        hmap.put("ReadPwr", String.valueOf(readPwr));
        hmap.put("WritePwr", String.valueOf(writePwr));
        if(parseWorkflowCfgs.has("Session"))
        {
            int session = parseWorkflowCfgs.getInt("Session");
            hmap.put("Session", String.valueOf(session));
        }
        if(parseWorkflowCfgs.has("Target"))
        {
            String target = parseWorkflowCfgs.getString("Target");
            hmap.put("Target", target);
        }
        if(parseWorkflowCfgs.has("Gen2Q"))
        {
            String gen2Q = parseWorkflowCfgs.getString("Gen2Q");
            hmap.put("Gen2Q", gen2Q);
        }
        if(parseWorkflowCfgs.has("Gen2QEnable"))
        {
            boolean gen2QEnable = parseWorkflowCfgs.getBoolean("Gen2QEnable");
            hmap.put("Gen2QEnable", String.valueOf(gen2QEnable));
        }
        if(parseWorkflowCfgs.has("Gen2QInitVal"))
        {
            int gen2InitQVal = parseWorkflowCfgs.getInt("Gen2QInitVal");
            hmap.put("Gen2QInitVal", String.valueOf(gen2InitQVal));
        }
        if(parseWorkflowCfgs.has("BLF"))
        {
            int blf = parseWorkflowCfgs.getInt("BLF");
            hmap.put("BLF", String.valueOf(blf));
        }
        if(parseWorkflowCfgs.has("DataEncoding"))
        {
            String dataEncoding = parseWorkflowCfgs.getString("DataEncoding");   
            hmap.put("DataEncoding", dataEncoding);
        }
        if(parseWorkflowCfgs.has("RFOnTime"))
        {
            int rfOnTime = parseWorkflowCfgs.getInt("RFOnTime");
            hmap.put("RFOnTime", String.valueOf(rfOnTime));
        }
        if(parseWorkflowCfgs.has("RFOffTime"))
        {
            int rfOffTime = parseWorkflowCfgs.getInt("RFOffTime");
            hmap.put("RFOffTime", String.valueOf(rfOffTime));
        }
        if(json.has("_USBKBEnable"))
        {
            String usbKBEnable = json.getString("_USBKBEnable");
            hmap.put("_USBKBEnable", usbKBEnable);
        }
        if(json.has("_KBDataFormat"))
        {
            String kbDataFormat = json.getString("_KBDataFormat");
            hmap.put("_KBDataFormat", kbDataFormat);
        }
        if(json.has("ReadMode"))
        {
            String readMode = json.getString("ReadMode");
            hmap.put("ReadMode", readMode);
        }
        if(json.has("_KBFieldSeparator"))
        {
            String kbFieldSeparator = json.getString("_KBFieldSeparator");
            hmap.put("_KBFieldSeparator", kbFieldSeparator);
        }
        if(json.has("_KBFieldSeparator"))
        {
            String kbRecordSeparator = json.getString("_KBRecordSeparator");
            hmap.put("_KBRecordSeparator", kbRecordSeparator);
        }
        if(json.has("_AudioVolume"))
        {
            String audioVolume = json.getString("_AudioVolume");
            hmap.put("_AudioVolume", audioVolume);
        }
        if(json.has("DateTime"))
        {
            String dateTime = json.getString("DateTime");
            hmap.put("DateTime", dateTime);
        }
        if(json.has("RdrStart"))
        {
            String rdrStart = json.getString("RdrStart");
            hmap.put("RdrStart", rdrStart);
        }
        if(json.has("FreqReg"))
        {
            String freqReg = json.getString("FreqReg");
            hmap.put("FreqReg", freqReg);
        }
        if(type.equalsIgnoreCase("RS232"))
        {
            JSONArray baudRateArray = json.getJSONArray("SerCfg");
            baudRateSet = baudRateArray.getInt(0); 
        }
        if(json.has("SpotInvCnt"))
        {
            boolean spotInvCnt = json.getBoolean("SpotInvCnt");
            hmap.put("SpotInvCnt",String.valueOf(spotInvCnt));
        }
        if(json.has("SpotRSSI"))
        {
            boolean spotRSSI = json.getBoolean("SpotRSSI");
            hmap.put("SpotRSSI",String.valueOf(spotRSSI));
        }
        if(json.has("SpotAnt"))
        {
            boolean spotAnt = json.getBoolean("SpotAnt");
            hmap.put("SpotAnt",String.valueOf(spotAnt));
        }
        if(json.has("SpotDT"))
        {
            boolean spotDT = json.getBoolean("SpotDT");
            hmap.put("SpotDT",String.valueOf(spotDT));
        }
        if(json.has("SpotPhase"))
        {
            boolean spotPhase = json.getBoolean("SpotPhase");
            hmap.put("SpotPhase",String.valueOf(spotPhase));
        }
        if(json.has("SpotProf"))
        {
            boolean spotProf = json.getBoolean("SpotProf");
            hmap.put("SpotProf",String.valueOf(spotProf));
        }
        if(json.has("SpotRz"))
        {
            boolean spotRz = json.getBoolean("SpotRz");
            hmap.put("SpotRz",String.valueOf(spotRz));
        }
        if(json.has("SpotFreq"))
        {
            boolean spotFreq = json.getBoolean("SpotFreq");
            hmap.put("SpotFreq",String.valueOf(spotFreq));
        }
        if(json.has("SpotGen2_BI"))
        {
            boolean spotGen2_BI = json.getBoolean("SpotGen2_BI");
            hmap.put("SpotGen2_BI",String.valueOf(spotGen2_BI));
        }
        if(json.has("SpotGen2_Q"))
        {
            boolean spotGen2_Q = json.getBoolean("SpotGen2_Q");
            hmap.put("SpotGen2_Q",String.valueOf(spotGen2_Q));
        }
        if(json.has("SpotGen2_LF"))
        {
            boolean spotGen2_LF = json.getBoolean("SpotGen2_LF");
            hmap.put("SpotGen2_LF",String.valueOf(spotGen2_LF));
        }
        if(json.has("SpotGen2_Target"))
        {
            boolean spotGen2_Target = json.getBoolean("SpotGen2_Target");
            hmap.put("SpotGen2_Target",String.valueOf(spotGen2_Target));
        }
        if(json.has("SpotGPIO"))
        {
            boolean spotGPIO = json.getBoolean("SpotGPIO");
            hmap.put("SpotGPIO",String.valueOf(spotGPIO));
        }
        if(json.has("SpotProt"))
        {
            boolean spotProt = json.getBoolean("SpotProt");
            hmap.put("SpotProt",String.valueOf(spotProt));
        }
        if(json.has("_LicenseKey"))
        {
            String licenseKey = json.getString("_LicenseKey");
            hmap.put("_LicenseKey",licenseKey);
        }
        if(type.equalsIgnoreCase("RS232"))
        {
            hmap.put("SerCfg",String.valueOf(baudRateSet));
        }
        if(json.has("_NewTagDelay"))
        {
            int newTagDelay = json.getInt("_NewTagDelay");
            hmap.put("_NewTagDelay",String.valueOf(newTagDelay));
        }
        if(json.has("_SameTagDelay"))
        {
            int sameTagDelay = json.getInt("_SameTagDelay");
            hmap.put("_SameTagDelay",String.valueOf(sameTagDelay));
        }
        if(json.has("_ReReportTag"))
        {
            String reReport = json.getString("_ReReportTag");
            hmap.put("_ReReportTag",reReport);
        }
        
        //boolean spotSensor = json.getBoolean("SpotSensor");
        //hmap.put("SpotSensor",String.valueOf(spotSensor));
        return hmap;
    }

    public String formJSONCommand(String command)
    {
        final String commandHeader = "Cmd";
        final String commandWorkflow = "Mode";
        final String commandRdrStart = "RdrStart";
        final String commandProfile = "Prof";
        JSONObject obj = new JSONObject();
        try
        {
            switch(command){
                case "GetInfoFieldsAll":
                    obj.put(commandHeader, "GetInfo");
                    JSONArray infoarray = new JSONArray();
                    infoarray.put("ALL");
                    obj.put("Fields", infoarray);
                    break;
                case "GetCfgFieldsAll":
                    obj.put(commandHeader, "GetCfg");
                    JSONArray cfgarrayall = new JSONArray();
                    cfgarrayall.put("ALL");
                    obj.put("Fields", cfgarrayall);
                    break;
                case "ActivateUpdateMode":
                    obj.put(commandHeader, "ActivateUpdateMode");
                    break;
                case "StartUpdate":
                    obj.put(commandHeader,"StartUpdate");
                    obj.put("Section","App");
                    obj.put("Password","0x02254410");
                    break;
                case "EndUpdate":
                    obj.put(commandHeader, "EndUpdate");
                    break;
                case "GetCfgFieldsDateTime":
                    obj.put(commandHeader,"GetCfg");
                    JSONArray cfgarraydatetime = new JSONArray();
                    cfgarraydatetime.put("DateTime");
                    obj.put("Fields", cfgarraydatetime);
                    break;
                case "GetCfgFieldsMode":
                    obj.put(commandHeader,"GetCfg");
                    JSONArray cfgArrayWorkflow = new JSONArray();
                    cfgArrayWorkflow.put("Mode");
                    obj.put("Fields", cfgArrayWorkflow);
                    break;
                case "StartRZ":
                    obj.put(commandHeader,"StartRZ");
                    break;
                case "StopRZ":
                    obj.put(commandHeader, "StopRZ");
                    break;
                case "Reboot":
                    obj.put(commandHeader,"Reboot");
                    break;
                case "GetGPIOs":
                    obj.put(commandHeader,"GetGPIOs");
                    break;
                case "ReadFields":
                    obj.put(commandHeader,"ReadFields");
                    break;
                case "SetCfgModeHDR":
                    obj.put(commandHeader, "SetCfg");
                    obj.put(commandWorkflow,"HDR");
                    break;
                case "SetCfgModeMonitor":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandWorkflow, "MONITOR");
                    break;
                case "SetCfgModeTagCommission":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandWorkflow, "TagCommission");
                    break;
                case "SetCfgModeTagUpdate":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandWorkflow, "TagUpdate");
                    break;
                case "SetCfgFieldsRdrStartActive":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandRdrStart, "ACTIVE");
                    break;
                case "SetCfgFieldsRdrStartNotActive":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandRdrStart, "NOTACTIVE");
                    break;
                case "GetCfgFieldsRdrStart":
                    obj.put(commandHeader, "GetCfg");
                    JSONArray getcfgRdrStart = new JSONArray();
                    getcfgRdrStart.put("RdrStart");
                    obj.put("Fields", getcfgRdrStart);
                    break;
                case "SaveFields":
                    obj.put(commandHeader,"SaveFields");
                    break;
                case "ClearFields":
                    obj.put(commandHeader,"DefaultFields");
                    break;
                case "SetCfgSingleReadAutonomousTrue":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandWorkflow, "SingleRead");
                    obj.put("Autonomous", "true");
                    break;
                case "SetCfgSingleReadAutonomousFalse":
                    obj.put(commandHeader,"SetCfg");
                    obj.put(commandWorkflow, "SingleRead");
                    obj.put("Autonomous", "false");
                    break;
                case "ThisTagProf":
                    obj.put(commandHeader, "ThisTag");
                    JSONArray profNum = new JSONArray();
                    profNum.put(1);
                    obj.put(commandProfile, profNum);
                    break;
                case "SetCfgHeartbeat":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("HBPeriod", 3.0);
                    break;
                case "SetCfgStopHeartbeat":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("HBPeriod", 0.0);
                    break;
                case "GetCfgFieldsRegion":
                    obj.put(commandHeader,"GetCfg");
                    obj.put("_Class", "_TM_PRIV_HWINFO_");
                    JSONArray regionArray = new JSONArray();
                    regionArray.put("Region");
                    obj.put("Fields", regionArray);
                    break;
                case "SetCfgUSBKBEnable":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("_USBKBEnable", true);
                    break;
                case "SetCfgUSBKBDisable":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("_USBKBEnable", false);
                    break;
                case "GetRZQ":
                    obj.put(commandHeader,"GetRZ");
                    JSONArray qArray = new JSONArray();
                    qArray.put("Q");
                    qArray.put("_InitQ");
                    qArray.put("_QMode");
                    obj.put("Fields",qArray);
                    break;
            }
        }
        catch(JSONException ex)
        {
            java.util.logging.Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return obj.toString();
    }

    /**
     * Frames the JSON message required for executing write data profile.
     *
     * @param memBank - the memory bank to write data.
     * @param startAddress - the start address of the memory to write data.
     * @param wordCount - the number of words to write.
     * @param retryLimit - the number of times write data operation is to be repeated, if fails upon first attempt.
     * @param writeData - the hexadecimal data to be written(in words).
     * @param check - When set to "true", the reader shall read the written data to confirm it was correctly written.
     * @param isDTSelected - When set to true, user wants to write current date and time to the specified memory bank
     * @return the framed JSON string to send to reader.
     */
    public String setProfileWriteData(String memBank, int startAddress, int wordCount, int retryLimit, String writeData, boolean check, boolean isDTSelected, boolean lckTag,boolean permaLock, String workflowSelected)
    {
        final String commandProfile = "Write";
        final String commandHeader = "Cmd";
        JSONObject obj = new JSONObject();
        ElaraUtil utils = new ElaraUtil();
        try
        {
            obj.put(commandHeader,"SetProf");
            
            JSONArray setWriteProperties = new JSONArray();
            JSONArray setWriteProfile = new JSONArray();
            
            setWriteProfile.put(utils.getMemBank(memBank));
            setWriteProfile.put(startAddress);
            setWriteProfile.put(wordCount);
            setWriteProfile.put(retryLimit);
            
            JSONArray writeDataVal = new JSONArray();
            if(isDTSelected)
            {
                writeDataVal.put("DT");
            }
            else
            {
                writeDataVal.put("VAL");
                writeDataVal.put(utils.splitToNChar(writeData,4));
            }
            setWriteProfile.put(writeDataVal);
            setWriteProperties.put(setWriteProfile);
            setWriteProfile.put(check);
            if(lckTag)
            {
                setWriteProfile.put("SECURE");
            }
            else if(permaLock)
            {
                setWriteProfile.put("PERMALOCK");
            }
            obj.put(commandProfile, setWriteProperties);
        }
        catch(JSONException ex)
        {
            java.util.logging.Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return obj.toString();
    }

    public String setProfileReadData(String memBank, int startAddress, int wordCount, int retryLimit)
    {
        final String commandProfile = "Read";
        final String commandHeader = "Cmd";
        JSONObject obj = new JSONObject();
        ElaraUtil utils = new ElaraUtil();
        try
        {
            obj.put(commandHeader,"SetProf");
            
            JSONArray setReadProperties = new JSONArray();
            JSONArray setReadProfile = new JSONArray();
            
            setReadProfile.put(utils.getMemBank(memBank));
            setReadProfile.put(startAddress);
            setReadProfile.put(wordCount);
            setReadProfile.put(retryLimit);
            
            setReadProperties.put(setReadProfile);
            obj.put(commandProfile, setReadProperties);
        }
        catch(JSONException ex)
        {
            java.util.logging.Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return obj.toString();
    }

    /**
     * Check for the RAIN command being sent is successful or not.
     *
     * @param response - the response received.
     * @return commandStatus - whether it is successful or not.
     */
    public boolean isCommandSuccess(String response) throws JSONException
    {
        boolean commandStatus;
        JSONObject jsonObj = new JSONObject(response);
        int errID = (Integer) jsonObj.get("ErrID");
        if(errID == 0)
        {
            commandStatus = true;
        }
        else
        {
            commandStatus = false;
        }
        return commandStatus;
    }

    /**
     * Return the error information received from the response of RAIN command.
     *
     * @param response - the response received to RAIN command.
     * @return errorMsg - the ErrorInfo field value from response.
     */
    public String errorInfo(String response) throws JSONException
    {
        JSONObject jsonObj;
        String errorMsg = "";
        try
        {
            jsonObj = new JSONObject(response);
            errorMsg =  jsonObj.getString("ErrInfo");
        }
        catch(JSONException ex)
        {
            java.util.logging.Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return errorMsg;
    }

    public boolean parseBootLoaderResponse(String response)
    {
        boolean commandStatus = false;
        try {
            
            JSONObject jsonObj = new JSONObject(response);
            String version = (String) jsonObj.get("Version");
            if(version.isEmpty() || version.length() ==0 || version == null)
            {
                commandStatus = false;
            }
            else
            {
                if(!version.contains("."))
                {
                    commandStatus = true;
                }
            }
            
        } catch (JSONException ex) {
            commandStatus = false;
            Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return commandStatus;
    }

    public String customJSONCommand(String command, StringBuffer sb)
    {
        final String commandHeader = "Cmd";
        final String commandWorkflow = "Workflow";
        JSONObject obj = new JSONObject();
        try
        {
            switch(command)
            {
                case "SetCfgFieldsDateTime":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("DateTime", sb.toString());
                    break;
                case "SetCfgAudioVolume":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("_AudioVolume",sb.toString());
                    break;
                case "SetCfgKBDataFormat":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("_KBDataFormat",sb.toString());
                    break;
                case "SetRzReadPwr":
                    obj.put(commandHeader,"SetRZ");
                    obj.put("ID",1);
                    obj.put("ReadPwr",Double.parseDouble(sb.toString()));
                    break;
                case "SetRzWritePwr":
                    obj.put(commandHeader,"SetRZ");
                    obj.put("ID",1);
                    obj.put("WritePwr",Double.parseDouble(sb.toString()));
                    break;
                case "SetCfgFreqReg":
                    obj.put(commandHeader,"SetCfg");
                    obj.put("FreqReg",sb.toString());
                    break;
                case "SetCfgMetadata":
                    obj.put(commandHeader, "SetCfg");
                    String[] fields = sb.toString().split(",");
                    for (String field : fields) 
                    {
                        String[] values = field.split(":");
                        if(values[1].equalsIgnoreCase("true"))
                        {
                            obj.put(values[0], true);
                        }
                        else
                        {
                            obj.put(values[0], false);
                        }
                        
                    }
                    break;
                case "SetCfgSerCfg":
                    obj.put(commandHeader, "SetCfg");
                    JSONArray baudRateArray = new JSONArray();
                    baudRateArray.put(Integer.parseInt(sb.toString()));
                    baudRateArray.put(8);
                    baudRateArray.put("n");
                    baudRateArray.put(1);
                    baudRateArray.put("n");
                    obj.put("SerCfg", baudRateArray);
                    break;
                case "SetRZPassword":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("Password", sb.toString());
                    break;
                case "SetRZDutyCycle":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    String[] dcValues = sb.toString().split(",");
                    int[] dcVal = {0,Integer.valueOf(dcValues[0]),Integer.valueOf(dcValues[1])};
                    obj.put("DutyCycle", dcVal);
                    break;
                case "SetRZSession":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("Session", Integer.parseInt(sb.toString()));
                    break;
                case "SetRZTarget":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("Target", sb.toString());
                    break;
                case "SetRZDataEncoding":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("DataEncoding", sb.toString());
                    break;
                case "SetRZBLF":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("BLD", Integer.parseInt(sb.toString()));
                    break;
                case "SetRZQMode":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("_QMode", sb.toString());
                    break;
                case "SetRZQ":
                    obj.put(commandHeader, "SetRZ");
                    obj.put("ID", 1);
                    obj.put("Q", Integer.parseInt(sb.toString()));
                    break;
                case"_NewTagDelay":
                    obj.put(commandHeader, "SetCfg");
                    obj.put("_NewTagDelay", Integer.parseInt(sb.toString()));
                    break;
                case"_SameTagDelay":
                    obj.put(commandHeader, "SetCfg");
                    obj.put("_SameTagDelay", Integer.parseInt(sb.toString()));
                    break;
                case"_ReReportTag":
                    obj.put(commandHeader, "SetCfg");
                    if(sb.toString().equalsIgnoreCase("true"))
                    {
                        obj.put("_ReReportTag", true);
                    }
                    else
                    {
                        obj.put("_ReReportTag", false);
                    }
                    break;
            }
        }
        catch(JSONException ex)
        {
            java.util.logging.Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return obj.toString();
    }

    public HashMap parseDateTime(String response)
    {
        HashMap<String, String> dateTimeHMap =  new HashMap<>();
        try 
        {
            JSONObject jsono = new JSONObject(response);
            String report = (String) jsono.get("Report");
            int ErrID = (Integer) jsono.get("ErrID");
            String DateTime = (String) jsono.get("DateTime");
            dateTimeHMap.put("report", report);
            dateTimeHMap.put("ErrID", String.valueOf(ErrID));
            dateTimeHMap.put("DateTime", DateTime);
        } 
        catch (JSONException ex) 
        {
            Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return dateTimeHMap;
    }

    public String[][] formatDateTime(String value)
    {
            int startIndex = 0;
            int tIndex = value.indexOf("T");
            String dateString = value.substring(startIndex, tIndex);
            String timeString = value.substring(tIndex+1,value.length()-1);
            String[] date = dateString.split("-");
            String[] time = timeString.split(":");
            String[][] dateTimeArray = {date, time};
            return dateTimeArray;
    }

    public String getCurrentTime()
    {
        Calendar cal = Calendar.getInstance();
        SimpleDateFormat sdf = new SimpleDateFormat("HH:mm:ss");
        return sdf.format(cal.getTime());
    }
    
    public static final LocalDate currentDate(){
        String date = new SimpleDateFormat("dd-MM-yyyy").format(Calendar.getInstance().getTime());
        DateTimeFormatter formatter = DateTimeFormatter.ofPattern("dd-MM-yyyy");
        LocalDate localDate = LocalDate.parse(date , formatter);
        return localDate;
    }
}
