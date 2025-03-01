/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package com.thingmagic;

/**
 *
 * @author qvantel
 */
public final class TMConstants {

    //Configuration Params

    public final static String TMR_PARAM_NONE = "No such parameter"; //return value from TMR_paramID().
    public final static String TMR_PARAM_BAUDRATE = "/reader/baudRate";
    public final static String TMR_PARAM_PROBE_BAUDRATE ="/reader/probeBaudRates";
    public final static String TMR_PARAM_COMMANDTIMEOUT = "/reader/commandTimeout";
    public final static String TMR_PARAM_TRANSPORTTIMEOUT = "/reader/transportTimeout";
    public final static String TMR_PARAM_POWERMODE = "/reader/powerMode";
    public final static String TMR_PARAM_USERMODE = "/reader/userMode";
    public final static String TMR_PARAM_ANTENNA_CHECKPORT = "/reader/antenna/checkPort";
    public final static String TMR_PARAM_ENABLE_SJC = "/reader/radio/enableSJC";
    public final static String TMR_PARAM_ANTENNA_PORTLIST = "/reader/antenna/portList";
    public final static String TMR_PARAM_ANTENNA_CONNECTEDPORTLIST = "/reader/antenna/connectedPortList";
    public final static String TMR_PARAM_ANTENNA_PORTSWITCHGPOS = "/reader/antenna/portSwitchGpos";
    public final static String TMR_PARAM_ANTENNA_SETTLINGTIMELIST = "/reader/antenna/settlingTimeList";
    public final static String TMR_PARAM_ANTENNA_TXRXMAP = "/reader/antenna/txRxMap";
    public final static String TMR_PARAM_ANTENNA_RETURNLOSS = "/reader/antenna/returnLoss";
    public final static String TMR_PARAM_GPIO_INPUTLIST = "/reader/gpio/inputList";
    public final static String TMR_PARAM_GPIO_OUTPUTLIST = "/reader/gpio/outputList";
    public final static String TMR_PARAM_GEN2_ACCESSPASSWORD = "/reader/gen2/accessPassword";
    public final static String TMR_PARAM_GEN2_Q = "/reader/gen2/q";
    public final static String TMR_PARAM_GEN2_TAGENCODING = "/reader/gen2/tagEncoding";
    public final static String TMR_PARAM_GEN2_TARGET = "/reader/gen2/target";
    public final static String TMR_PARAM_GEN2_BLF = "/reader/gen2/BLF";
    public final static String TMR_PARAM_GEN2_TARI = "/reader/gen2/tari";
    public final static String TMR_PARAM_GEN2_WRITEMODE = "/reader/gen2/writeMode";
    public final static String TMR_PARAM_GEN2_BAP = "/reader/gen2/bap";
    public final static String TMR_PARAM_ISO180006B_BLF = "/reader/iso180006b/BLF";
    public final static String TMR_PARAM_READ_ASYNCOFFTIME = "/reader/read/asyncOffTime";
    public final static String TMR_PARAM_READ_ASYNCONTIME = "/reader/read/asyncOnTime";
    public final static String TMR_PARAM_READ_PLAN = "/reader/read/plan";
    public final static String TMR_PARAM_RADIO_POWERMAX = "/reader/radio/powerMax";
    public final static String TMR_PARAM_RADIO_POWERMIN = "/reader/radio/powerMin";
    public final static String TMR_PARAM_RADIO_PORTREADPOWERLIST = "/reader/radio/portReadPowerList";
    public final static String TMR_PARAM_RADIO_PORTWRITEPOWERLIST = "/reader/radio/portWritePowerList";
    public final static String TMR_PARAM_RADIO_READPOWER = "/reader/radio/readPower";
    public final static String TMR_PARAM_RADIO_WRITEPOWER = "/reader/radio/writePower";
    public final static String TMR_PARAM_RADIO_TEMPERATURE = "/reader/radio/temperature";
    public final static String TMR_PARAM_TAGREADDATA_RECORDHIGHESTRSSI = "/reader/tagReadData/recordHighestRssi";
    public final static String TMR_PARAM_TAGREADDATA_REPORTRSSIINDBM = "/reader/tagReadData/reportRssiInDbm";
    public final static String TMR_PARAM_TAGREADDATA_UNIQUEBYANTENNA = "/reader/tagReadData/uniqueByAntenna";
    public final static String TMR_PARAM_TAGREADDATA_UNIQUEBYDATA = "/reader/tagReadData/uniqueByData";
    public final static String TMR_PARAM_TAGREADDATA_UNIQUEBYPROTOCOL = "/reader/tagReadData/uniqueByProtocol";
    public final static String TMR_PARAM_TAGOP_ANTENNA = "/reader/tagop/antenna";
    public final static String TMR_PARAM_TAGOP_PROTOCOL = "/reader/tagop/protocol";
    public final static String TMR_PARAM_VERSION_HARDWARE = "/reader/version/hardware";
    public final static String TMR_PARAM_VERSION_SERIAL = "/reader/version/serial";
    public final static String TMR_PARAM_VERSION_MODEL = "/reader/version/model";
    public final static String TMR_PARAM_VERSION_SOFTWARE = "/reader/version/software";
    public final static String TMR_PARAM_VERSION_SUPPORTEDPROTOCOLS = "/reader/version/supportedProtocols";
    public final static String TMR_PARAM_REGION_ID = "/reader/region/id";
    public final static String TMR_PARAM_REGION_SUPPORTEDREGIONS = "/reader/region/supportedRegions";
    public final static String TMR_PARAM_REGION_HOPTABLE = "/reader/region/hopTable";
    public final static String TMR_PARAM_REGION_HOPTIME = "/reader/region/hopTime";
    public final static String TMR_PARAM_REGION_QUANTIZATION_STEP  = "/reader/region/quantizationStep";
    public final static String TMR_PARAM_REGION_MINIMUM_FREQUENCY  = "/reader/region/minimumFrequency";
    public final static String TMR_PARAM_REGION_LBT_ENABLE = "/reader/region/lbt/enable";
    public final static String TMR_PARAM_REGION_LBT_THRESHOLD = "/reader/region/lbtThreshold";
    public final static String TMR_PARAM_REGION_DWELL_TIME_ENABLE = "/reader/region/dwellTime/enable";
    public final static String TMR_PARAM_REGION_DWELL_TIME = "/reader/region/dwellTime";
    public final static String TMR_PARAM_LICENSE_KEY = "/reader/licenseKey";
    public final static String TMR_PARAM_MANAGE_LICENSE_KEY = "/reader/manageLicenseKey";
    public final static String TMR_PARAM_USER_CONFIG = "/reader/userConfig";
    public final static String TMR_PARAM_CURRENTTIME = "/reader/currentTime";
    public final static String TMR_PARAM_HOSTNAME = "/reader/hostname";
    public final static String TMR_PARAM_ANTENNAMODE = "/reader/antennaMode";
    public final static String TMR_PARAM_GEN2_SESSION = "/reader/gen2/session";
    public final static String TMR_PARAM_READER_URI = "/reader/uri";
    public final static String TMR_PARAM_READER_DESCRIPTION = "/reader/description";
    public final static String TMR_PARAM_READER_EXTENDED_EPC = "/reader/extendedEPC";
    public final static String TMR_PARAM_READER_STATISTICS = "/reader/statistics";
    public final static String TMR_PARAM_READER_STATS_ENABLE = "/reader/stats/enable";
    public final static String TMR_PARAM_READER_STATS = "/reader/stats";
    public final static String TMR_PARAM_READER_METADATA = "/reader/metadata";
    public final static String TMR_PARAM_READER_PRODUCTGROUPID = "/reader/version/productGroupID";
    public final static String TMR_PARAM_READER_PRODUCTGROUP = "/reader/version/productGroup";
    public final static String TMR_PARAM_READER_PRODUCTID = "/reader/version/productID";
    public final static String TMR_PARAM_READER_TAGOP_SUCCESSES = "/reader/tagReadData/tagopSuccesses";
    public final static String TMR_PARAM_READER_TAGOP_FAILURES = "/reader/tagReadData/tagopFailures";
    public final static String TMR_PARAM_READER_WRITE_REPLY_TIMEOUT = "/reader/gen2/writeReplyTimeout";
    public final static String TMR_PARAM_READER_WRITE_EARLY_EXIT = "/reader/gen2/writeEarlyExit";
    public final static String TMR_PARAM_ISO180006B_MODULATION_DEPTH = "/reader/iso180006b/modulationDepth";
    public final static String TMR_PARAM_ISO180006B_DELIMITER = "/reader/iso180006b/delimiter";
    public final static String TMR_PARAM_ISO15693_TAGTYPE = "/reader/iso15693/tagType";
    public final static String TMR_PARAM_ISO14443A_TAGTYPE = "/reader/iso14443a/tagType";
    public final static String TMR_PARAM_ISO14443B_TAGTYPE = "/reader/iso14443b/tagType";
    public final static String TMR_PARAM_LF125KHZ_TAGTYPE = "/reader/lf125khz/tagType";
    public final static String TMR_PARAM_LF134KHZ_TAGTYPE = "/reader/lf134khz/tagType";
    public final static String TMR_PARAM_ISO15693_SUPPORTED_TAGTYPES = "/reader/iso15693/supportedTagTypes";
    public final static String TMR_PARAM_ISO14443A_SUPPORTED_TAGTYPES = "/reader/iso14443a/supportedTagTypes";
    public final static String TMR_PARAM_ISO14443B_SUPPORTED_TAGTYPES = "/reader/iso14443b/supportedTagTypes";
    public final static String TMR_PARAM_LF125KHZ_SUPPORTED_TAGTYPES = "/reader/lf125khz/supportedTagTypes";
    public final static String TMR_PARAM_LF134KHZ_SUPPORTED_TAGTYPES = "/reader/lf134khz/supportedTagTypes";
    public final static String TMR_PARAM_ISO14443A_SUPPORTED_TAG_FEATURES = "/reader/iso14443a/supportedTagFeatures";
    public final static String TMR_PARAM_ISO15693_SUPPORTED_TAG_FEATURES = "/reader/iso15693/supportedTagFeatures";
    public final static String TMR_PARAM_LF125KHZ_SUPPORTED_TAG_FEATURES = "/reader/lf125khz/supportedTagFeatures";
    public final static String TMR_PARAM_LF125KHZ_SECURE_RD_FORMAT = "/reader/lf125khz/secureRdFormat";
    public final static String TMR_PARAM_TRIGGER_READ_GPI = "/reader/read/trigger/gpi";
    public final static String TMR_PARAM_GEN2_PROTOCOLEXTENSION  = "/reader/gen2/protocolExtension";
    public final static String TMR_PARAM_GEN2_T4  = "/reader/gen2/t4";
    public final static String TMR_PARAM_GEN2_INITIAL_Q  = "/reader/gen2/initQ";
    public final static String TMR_PARAM_GEN2_SEND_SELECT  = "/reader/gen2/sendSelect";
    public final static String TMR_PARAM_GEN2_RF_MODE  = "/reader/gen2/rfMode";
    public final static String TMR_PARAM_PROTOCOL_LIST  = "/reader/protocolList";
    public final static String TMR_PARAM_PER_ANTENNA_TIME  = "/reader/antenna/perAntennaTime";

    public final static String TMR_PARAM_READ_FILTER_TIMEOUT = "/reader/tagReadData/readFilterTimeout";
    public final static String TMR_PARAM_ENABLE_READ_FILTERING = "/reader/tagReadData/enableReadFilter";

    // Regulatory test params
    public final static String TMR_PARAM_REGULATORY_MODE = "/reader/regulatory/mode";
    public final static String TMR_PARAM_REGULATORY_MODULATION = "/reader/regulatory/modulation";
    public final static String TMR_PARAM_REGULATORY_ONTIME = "/reader/regulatory/onTime";
    public final static String TMR_PARAM_REGULATORY_OFFTIME = "/reader/regulatory/offTime";
    public final static String TMR_PARAM_REGULATORY_ENABLE = "/reader/regulatory/enable";
    // status 
    public final static String TMR_PARAM_READER_STATUS_ANTENNA = "/reader/status/antennaEnable";
    public final static String TMR_PARAM_READER_STATUS_FREQUENCY = "/reader/status/frequencyEnable";
    public final static String TMR_PARAM_READER_STATUS_TEMPERATURE = "/reader/status/temperatureEnable";

    
    //RQL Schema

    public final static String TMR_RQL_PARAMS = "params";
    public final static String TMR_RQL_SAVED_SETTINGS = "saved_settings";
    public final static String TMR_RQL_SETTINGS = "settings";
    public final static String TMR_RQL_TAG_ID = "tag_id";
    public final static String TMR_RQL_TAG_DATA = "tag_data";
    public final static String TMR_RQL_TAG_LOCKED = "locked";
    public final static String TMR_RQL_LOCK_TYPE = "type";

    //public final static String TMR_RQL_PARAMS = "";


    //PARAMS FIELDS

    public final static String TMR_RQL_UHF_POWER = "uhf_power_centidBm";
    public final static String TMR_RQL_TX_POWER = "tx_power";
    public final static String TMR_RQL_GEN2INITQ = "gen2InitQ";
    public final static String TMR_RQL_GEN2MINQ = "gen2MinQ";
    public final static String TMR_RQL_GEN2MAXQ = "gen2MaxQ";
    public final static String TMR_RQL_GEN2TARGET = "gen2Target";
    public final static String TMR_RQL_ANTENNA_SAFETY= "antenna_safety";
    public final static String TMR_RQL_SJC= "enablesjc";
    public final static String TMR_RQL_GEN2_BLF= "gen2BLF";
    public final static String TMR_RQL_ISO_BLF= "i186bBLF";
    public final static String TMR_RQL_LICENSEKEY= "reader_licenseKey";


    
    public final static String TMR_RQL_REGIONNAME =  "regionName";
    public final static String TMR_RQL_REGIONVERSION = "region_version";
    public final static String TMR_RQL_UCODEEPC = "useUCodeEpc";
    public final static String TMR_RQL_RDR_AVAIL_ANTENNAS = "reader_available_antennas";
    
    public final static String TMR_RQL_RDR_SERIAL = "reader_serial";
    public final static String TMR_RQL_RDR_CONN_ANTENNAS = "reader_connected_antennas";
    public final static String TMR_RQL_POWERMODE = "powerMode";
    
    public final static String TMR_RQL_GEN2_TAGENCODING = "gen2TagEncoding";
    public final static String TMR_RQL_GEN2_TARI = "gen2Tari";

    public final static String TMR_RQL_PORTREADPOWERLIST = "portReadPowerList";
    public final static String TMR_RQL_PORTWRITEPOWERLIST = "portWritePowerList";



    // TAG ID FIELDS
    
    public final static String TMR_RQL_PROTOCOL_ID = "protocol_id";
    public final static String TMR_RQL_ID = "id";
    
    //SAVED SETTINGS FIELDS
    public final static String TMR_RQL_GEN2_SESSION = "gen2Session";
    public final static String TMR_RQL_USERMODE = "userMode";
    public final static String TMR_RQL_HOSTNAME = "hostname";
    public final static String TMR_RQL_IFACE = "iface";
    public final static String TMR_RQL_DHCPCD = "dhcpcd";
    public final static String TMR_RQL_IP_ADDRESS = "ip_address";
    public final static String TMR_RQL_NETMASK = "netmask";
    public final static String TMR_RQL_GATEWAY = "gateway";
    public final static String TMR_RQL_NTP_SERVERS = "ntp_servers";
    public final static String TMR_RQL_EPC_ID_LEN = "epc1_id_length";
    public final static String TMR_RQL_PRIMARY_DNS = "primary_dns";
    public final static String TMR_RQL_SECONDARY_DNS = "secondary_dns";
    public final static String TMR_RQL_DOMAIN_NAME = "domain_name";
    public final static String TMR_RQL_READER_DESC = "reader_description";
    public final static String TMR_RQL_READER_ROLE = "reader_role";
    public final static String TMR_RQL_ANT1_READPOINT = "ant1_readpoint_descr";
    public final static String TMR_RQL_ANT2_READPOINT = "ant2_readpoint_descr";
    public final static String TMR_RQL_ANT3_READPOINT = "ant3_readpoint_descr";
    public final static String TMR_RQL_ANT4_READPOINT = "ant4_readpoint_descr";
    
    public final static String TMR_RQL_HARDWARE = "reader_hwverdata";
    public final static String TMR_RQL_TEMPERATURE = "reader_temperature";





    // SETTINGS FIELDS
    public final static String TMR_RQL_ANTENNAMODE = "antenna_mode";
    public final static String TMR_RQL_VERSION = "version";
    public final static String TMR_RQL_MODEL = "pib_model";
    public final static String TMR_RQL_SUPP_PROTOCOLS = "supported_protocols";
    public final static String TMR_RQL_CURRENTTIME = "current_time";

    // Tag Protocols

    public final static String TMR_TAGPROTOCOL_GEN2 = "GEN2";
    public final static String TMR_TAGPROTOCOL_ISO180006B = "ISO18000-6B";
    public final static String TMR_TAGPROTOCOL_IPX256 = "IPX256";
    public final static String TMR_TAGPROTOCOL_IPX64 = "IPX64";

    // Fixed Readers

    public final static String TMR_READER_M4 = "M4";
    public final static String TMR_READER_M5 = "M5";
    public final static String TMR_READER_M6 = "M6";
    public final static String TMR_READER_ASTRA = "Astra";
    public final static String TMR_READER_ASTRA_EX = "Astra-EX";
    public final static String TMR_READER_MERCURY6 = "Mercury6";
    public final static String TMR_READER_SARGAS = "Sargas";
    public final static String TMR_READER_IZAR = "Izar";
    public final static String TMR_READER_ASTRA200 = "Astra200";
    // Serial Readers

    public final static String TMR_READER_M6E = "M6e";
    public final static String TMR_READER_M6E_I_PRC = "M6e PRC";
    public final static String TMR_READER_M6E_I_JIC = "M6e JIC";
    public final static String TMR_READER_M6E_MICRO = "M6e Micro";
    public final static String TMR_READER_M6E_MICRO_USB = "M6e Micro USB";
    public final static String TMR_READER_M6E_NANO = "M6e Nano";
    public final static String TMR_READER_M6E_MICRO_USB_PRO = "M6e Micro USBPro";
    public final static String TMR_READER_M7E_PICO = "M7e Pico";
    public final static String TMR_READER_M7E_DEKA = "M7e Deka";
    public final static String TMR_READER_M7E_HECTO = "M7e Hecto";
    public final static String TMR_READER_M7E_MEGA = "M7e Mega";
    public final static String TMR_READER_M7E_TERA = "M7e Tera";
    public final static String TMR_READER_M3E = "M3e";
}
