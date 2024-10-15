package com.thingmagic;

import static com.thingmagic.TMConstants.TMR_PARAM_REGION_HOPTABLE;
import static com.thingmagic.TMConstants.TMR_PARAM_REGION_HOPTIME;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.net.URL;
import java.text.DecimalFormat;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Date;
import java.util.HashMap;
import java.util.Iterator; 
import java.util.List;
import java.util.Map;
import java.util.Properties;
import java.util.ResourceBundle;
import java.util.concurrent.ConcurrentHashMap;
import java.util.logging.Level;
import java.util.logging.Logger;
import javafx.application.Platform;
import javafx.beans.value.ChangeListener;
import javafx.beans.value.ObservableValue;
import javafx.collections.FXCollections;
import javafx.collections.ObservableList;
import javafx.concurrent.Task;
import javafx.event.ActionEvent;
import javafx.event.EventHandler;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.geometry.Pos;
import javafx.scene.Node;
import javafx.scene.control.Accordion;
import javafx.scene.control.Button;
import javafx.scene.control.CheckBox;
import javafx.scene.control.ComboBox;
import javafx.scene.control.Label;
import javafx.scene.control.ProgressBar;
import javafx.scene.control.ProgressIndicator;
import javafx.scene.control.RadioButton;
import javafx.scene.control.Slider;
import javafx.scene.control.Tab;
import javafx.scene.control.TabPane;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableView;
import javafx.scene.control.TextArea;
import javafx.scene.control.TextField;
import javafx.scene.control.TitledPane;
import javafx.scene.control.ToggleGroup;
import javafx.scene.control.Tooltip;
import javafx.scene.control.cell.PropertyValueFactory;
import javafx.scene.image.Image;
import javafx.scene.image.ImageView;
import javafx.scene.input.KeyEvent;
import javafx.scene.input.MouseEvent;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Pane;
import javafx.scene.layout.VBox;
import javafx.scene.paint.Color;
import javafx.scene.shape.Circle;
import javafx.scene.text.Text;
import javafx.scene.text.TextAlignment;
import javafx.scene.web.WebEngine;
import javafx.scene.web.WebView;
import javafx.stage.FileChooser;
import javafx.stage.Stage;
import javax.swing.JOptionPane;
import javax.swing.ToolTipManager;
import com.fazecast.jSerialComm.*;
import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.text.DateFormat;
import java.text.ParseException;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.Base64;
import java.util.Optional;
import javafx.scene.control.Alert;
import javafx.scene.control.Alert.AlertType;
import javafx.scene.control.DatePicker;
import javax.swing.JFrame;
import org.json.JSONException;
import org.json.JSONObject;
import purejavahidapi.HidDeviceInfo;
import purejavahidapi.PureJavaHidApi;

public class MainController implements Initializable{

    public Stage stage;
    @FXML
    private TabPane mainTabs;
    @FXML
    private Tab homeTab;
    @FXML
    private Tab connectTab;
    @FXML
    private Tab configureTab;
    @FXML
    private Tab configureTabElara;
    @FXML
    private Tab utilsTabElara;
    @FXML
    private Tab readTab;
    @FXML
    private Tab resultsTabElaraHID;
    @FXML
    private Tab resultsTabElaraCDC;
    @FXML
    private TextArea hidText;
    @FXML
    private TextArea cdcText;
    @FXML
    private Tab helpTab;
    
    @FXML
    private DatePicker datepicker;
    //Home Tab
    @FXML
    private BorderPane homeBorderPane;

    //Connect Tab Controllers
    @FXML
    private ImageView reloadDevices;
    @FXML
    private Button reloadDevicesButton;
    @FXML
    private VBox readerList;
    @FXML
    private Pane suggestionPane;
    @FXML
    private ProgressIndicator reloadDevicesProgress;
    @FXML
    private VBox readerProperties;

    @FXML
    private ComboBox probeBaudRate;
    @FXML
    private ComboBox probeElaraBaudRate;
    @FXML
    private Pane elaraBaudRatePane;
    
    @FXML
    private Button connectionButton;
    @FXML
    private ProgressIndicator connectProgressIndicator;

    //Configure Tab Controllers
    @FXML
    private Accordion accordion;
    @FXML
    private TitledPane readWriteTitledPane;
    @FXML
    private TitledPane performanceTuningTitlePane;
    @FXML
    private TitledPane regulatoryTestingPane;
    @FXML
    private TitledPane displayOptionsTitlePane;
    @FXML
    private TitledPane profileTitlePane;
    @FXML
    private TitledPane firmwareUpdateTitledPane;
    @FXML
    private TitledPane aboutTitledPane;
    @FXML
    private TitledPane generalTitledPane;
    @FXML
    private ComboBox region;
    @FXML
    private ComboBox regionElara;
    @FXML
    private ComboBox buzzerTone;
    @FXML
    private ComboBox outputDataFormat;
    @FXML
    private CheckBox gen2;
    @FXML
    private CheckBox gen2Elara;
    @FXML
    private CheckBox iso18000;
    @FXML
    private CheckBox ipx64;
    @FXML
    private CheckBox ipx256;
    @FXML
    private CheckBox cbTransportLogging;
    @FXML
    private CheckBox antenna1;
    @FXML
    private CheckBox antenna1Elara;
    @FXML
    private CheckBox antenna2;
    @FXML
    private CheckBox antenna3;
    @FXML
    private CheckBox antenna4;
    @FXML
    private CheckBox antennaDetection;
    @FXML
    private CheckBox elaraMetadataAntenna;
    @FXML
    private CheckBox elaraMetadataDateTime;
    @FXML
    private CheckBox elaraMetadataInventoryCount;
    @FXML
    private CheckBox elaraMetadataPhase;
    @FXML
    private CheckBox elaraMetadataProfile;
    @FXML
    private CheckBox elaraMetadataRSSI;
    @FXML
    private CheckBox elaraMetadataRZ;
    @FXML
    private CheckBox elaraMetadataFreq;
    @FXML
    private CheckBox elaraMetadataGen2BI;
    @FXML
    private CheckBox elaraMetadataGen2Q;
    @FXML
    private CheckBox elaraMetadataGen2LF;
    @FXML
    private CheckBox elaraMetadataGen2Target;
    @FXML
    private CheckBox elaraMetadataGPIO;
    @FXML
    private CheckBox elaraMetadataProtocol;
//    @FXML
//    private CheckBox elaraMetadataSensor;
    @FXML
    private CheckBox elaraHIDModeEnabled;
    @FXML
    private Label temperatureElara;
	
    @FXML
    private CheckBox quickSearch;
    @FXML
    private RadioButton dynamic;
    @FXML
    private RadioButton equalTime;

    @FXML
    private TextField dutyCycleOn;
    @FXML
    private TextField dutyCycleOff;
    @FXML
    private Text dutyCycleOnText;
    @FXML
    private Text dutyCycleOffText;

    @FXML
    private ComboBox epcStreamFormat;

    @FXML
    private CheckBox gpiTriggerRead;

    @FXML
    private CheckBox autonomousRead;
    @FXML
    private CheckBox gpiTriggerReadElara;

    @FXML
    private RadioButton autoReadGpi1;
    @FXML
    private RadioButton autoReadGpi2;
    @FXML
    private RadioButton autoReadGpi3;
    @FXML
    private RadioButton autoReadGpi4;

    @FXML
    private CheckBox metaDataEpc;
    @FXML
    private CheckBox metaDataTimeStamp;
    @FXML
    private CheckBox metaDataRssi;
    @FXML
    private CheckBox metaDataReadCount;   
    
    @FXML
    private CheckBox metaDataAntenna;
    @FXML
    private CheckBox metaDataProtocol;    
    @FXML
    private CheckBox metaDataFrequency;   
    @FXML
    private CheckBox metaDataPhase;    
    
    @FXML
    private CheckBox embeddedReadEnable;
    @FXML
    private CheckBox embeddedReadUnique;
    @FXML
    private ComboBox embeddedMemoryBank;
    @FXML
    private TextField embeddedStart;
    @FXML
    private TextField embeddedEnd;

    @FXML
    private TextField rfRead;
    @FXML
    private TextField rfWrite;
    @FXML
    private TextField hoursTextField;
    @FXML
    private TextField minutesTextField;
    @FXML
    private TextField secondsTextField;

    @FXML
    private Text elaraReaderModel;
    @FXML
    private Text readerSerialNumber;
    @FXML
    private Text readerVersion;
    @FXML
    private Text readerType;
    @FXML
    private Text sensorSerial;
    @FXML
    private Text sensorVersion;
    
    @FXML
    private Pane infoTitlePane;
    @FXML
    private Pane elaraReaderModelPane;
    @FXML
    private Pane readerSerialNumberPane;
    @FXML
    private Pane readerVersionPane;
    @FXML
    private Pane readerTypePane;
    @FXML
    private Pane sensorSerialPane;
    @FXML
    private Pane sensorVersionPane;
    @FXML
    private TitledPane elaraFirmwareUpdate;
    @FXML
    private TitledPane regulatoryElara;
    @FXML
    private TitledPane diagnosticsElara;
    
    @FXML
    private RadioButton link640Khz;
    @FXML
    private RadioButton link250Khz;

    @FXML
    private RadioButton tari25us;
    @FXML
    private RadioButton tari12_5us;
    @FXML
    private RadioButton tari6_25us;

    @FXML
    private RadioButton fm0;
    @FXML
    private RadioButton m2;
    @FXML
    private RadioButton m4;
    @FXML
    private RadioButton m8;

    @FXML
    private RadioButton sessionS0;
    @FXML
    private RadioButton sessionS1;
    @FXML
    private RadioButton sessionS2;
    @FXML
    private RadioButton sessionS3;

    @FXML
    private RadioButton targetA;
    @FXML
    private RadioButton targetB;
    @FXML
    private RadioButton targetAB;
    @FXML
    private RadioButton targetBA;

    @FXML
    private RadioButton dynamicQ;
    @FXML
    private RadioButton staticQ;
    @FXML
    private ComboBox staticQList;

    @FXML
    private Label lRfidEngine;
    @FXML
    private Label lFirmwareVersion;
    @FXML
    private Label lHardwareVersion;
    @FXML
    private Label lActVersion;
    @FXML
    private Label lMercuryApiVersion;

    @FXML
    private Label minPower;
    @FXML
    private Label minPower1;
    @FXML
    private Label maxPower;
    @FXML
    private Label maxPower1;
    @FXML
    private Label minReadPowerElara;
    @FXML
    private Label maxReadPowerElara;
    @FXML
    private Label minWritePowerElara;
    @FXML
    private Label maxWritePowerElara;
    @FXML
    private Slider readPowerSlider;
    @FXML
    private Slider writePowerSlider;
    @FXML
    private CheckBox powerEquator;

    @FXML
    private ToggleGroup linkFreqGroup;
    @FXML
    private ToggleGroup tariGroup;
    @FXML
    private ToggleGroup tagEncodeGroup;
    @FXML
    private ToggleGroup targetGroup;
    @FXML
    private ToggleGroup sessionGroup;
    @FXML
    private ToggleGroup qGroup;
    @FXML
    private ToggleGroup autoReadGpiGroup;
    @FXML
    private Button applyButton;
    @FXML
    private Button revertButton;
    @FXML
    private Button loadConfigButton;
    @FXML
    private Button saveConfigButton;
    @FXML
    private Button setButtonElara;
    @FXML
    private Button saveButtonElara;
    @FXML
    private Button revertButtonElara;
    @FXML
    private Button loadDefaultButtonElara;
    @FXML
    private Button getDateTimeElara;
    @FXML
    private Button applyDateTimeButton;
    @FXML
    private VBox changeListContainer;
    @FXML
    private HBox regionParentNode;
    @FXML
    private ComboBox lockTag;
    @FXML
    private TextField accessPassword;
    @FXML
    private Text accessPasswordText;
    @FXML
    private Text writeDataSettings;
    @FXML
    private Text writeDataSettingsText;
    @FXML
    private ComboBox readWriteTag;
    
    private String firmwareFile ;
    private FileInputStream fi;
    private boolean isReading = false;
    private boolean isElaraReading = false;
    @FXML
    private Label keepAliveTimeLabel;
    @FXML
    private TextField keepAliveTime;
    
    @FXML
    private TextArea hopTable;
    
    @FXML
    private TextField hopTime;

    private boolean isLoadSaveConfiguration = false;
    private boolean inBootLoaderMode =false;
    private boolean inBootFailed = false;
    private double percentComplete;
    private boolean firmwareUpdateSuccess=false;
    private boolean elaraRS232 = false;
    private String elaraReadPowerValue;
    private String elaraWritePowerValue;
    //Read Tab
    ConcurrentHashMap<String, TagReadData> tagData = new ConcurrentHashMap<String, TagReadData>();
    ConcurrentHashMap<String, String> elaraTagData = new ConcurrentHashMap<String, String>();
    @FXML
    private TableView tableView;
    @FXML
    private TableColumn deviceIdColumn;
    @FXML
    private TableColumn dataColumn;
    @FXML
    private TableColumn epcColumn;
    @FXML
    private TableColumn timeStampColumn;
    @FXML
    private TableColumn rssiColumn;
    @FXML
    private TableColumn countColumn;    
    @FXML
    private TableColumn antennaColumn;
    @FXML
    private TableColumn protocolColumn;
    @FXML
    private TableColumn frequencyColumn;
    @FXML
    private TableColumn phaseColumn;
    @FXML
    private TextField selectedFilePath;
    @FXML
    private Button loadFirmware;
    @FXML
    private Button updateFirmware;
    @FXML
    private Button btGetStarted;
    @FXML
    private ProgressBar progressBar;
    @FXML
    private Label temperature;
    @FXML
    private Text updatePercent;
    @FXML
    private TextField selectedElaraFilePath;
    @FXML
    private Button loadElaraFirmware;
    @FXML
    private Button updateElaraFirmware;
    @FXML
    private ProgressBar progressBarElara;
    @FXML
    private BorderPane popupMsgBorderPane;
    @FXML
    private Pane popupMsgPane;
    @FXML
    private ImageView popupCloseImg;
    @FXML
    private ImageView warningDownArrow;
    
    @FXML
    private ImageView successDownArrow;
    
    @FXML
    private ImageView errorDownArrow;
    
    @FXML
    private Text popupMsgContentLabel;
    @FXML
    private Text  popupMsgTitle;
    
    //Help Tab
    @FXML
    private BorderPane helpBorderPane;

    //Footer Controllers
    @FXML
    private Circle connectStatus;
    @FXML
    private Label statusLabel;

    //Connect Tab Objects 
    Reader r = null;
    private static boolean isConnected = false;
    HashMap<String,HashMap> comportInfo = new HashMap<String,HashMap>();

    //private Text comportName;
    private String deviceName;

    // flag to check for ElaraHID and updates readTabUI accordingly
    private boolean isElaraHID = false;
    private boolean hidEnabled = false;
    private boolean streamUpdatefailed = false;
    //stores the index of readTab
    private int readTabIndex;

    //stores the index of configureTab
    private int configureTabIndex;

    //ConfigureTabElara Controllers
    @FXML
    private Accordion accordionElara;
    @FXML
    private Accordion accordionElaraDiagnostics;
    @FXML
    private ComboBox workflow;
    @FXML
    private TitledPane advancedSettingsTitledPane;
    @FXML
    private TextField rfReadElara;
    @FXML
    private TextField rfWriteElara;

    @FXML
    private Slider readPowerSliderElara;
    @FXML
    private Slider writePowerSliderElara;
    @FXML
    private CheckBox powerEquatorElara;
    @FXML
    private CheckBox autonomousReadElara;

    @FXML
    private ToggleGroup linkFreqGroupElara;
    @FXML
    private ToggleGroup tariGroupElara;
    @FXML
    private ToggleGroup tagEncodeGroupElara;
    @FXML
    private ToggleGroup targetGroupElara;
    @FXML
    private ToggleGroup sessionGroupElara;
    @FXML
    private ToggleGroup qGroupElara;
    @FXML
    private ToggleGroup qEnable;

    @FXML
    private Button toggleButtonStart;

    @FXML
    private RadioButton link250KhzElara;
    @FXML
    private RadioButton link640KhzElara;

    @FXML
    private RadioButton tari25usElara;
    @FXML
    private RadioButton tari12_5usElara;
    @FXML
    private RadioButton tari6_25usElara;

    @FXML
    private RadioButton fm0Elara;
    @FXML
    private RadioButton m2Elara;
    @FXML
    private RadioButton m4Elara;
    @FXML
    private RadioButton m8Elara;

    @FXML
    private RadioButton sessionS0Elara;
    @FXML
    private RadioButton sessionS1Elara;
    @FXML
    private RadioButton sessionS2Elara;
    @FXML
    private RadioButton sessionS3Elara;

    @FXML
    private RadioButton targetAElara;
    @FXML
    private RadioButton targetBElara;
    @FXML
    private RadioButton targetABElara;
    @FXML
    private RadioButton targetBAElara;

    @FXML
    private RadioButton dynamicQElara;
    @FXML
    private RadioButton staticQElara;
    @FXML
    private Text initQText;
    @FXML
    private ComboBox qListElara;
    @FXML
    private ComboBox filterMemoryBank;
    @FXML
    private TextField startAddress;
    @FXML
    private TextField filterData;

    @FXML
    private TextField rfOnTime;
    @FXML
    private TextField rfOffTime;
    @FXML
    private Text rfOnTimeText;
    @FXML
    private Text rfOffTimeText;
    @FXML
    private ComboBox writeMemoryBank;
    @FXML
    private Text writeMemoryBankText;
    @FXML
    private TextField writeStartAddress;
    @FXML
    private Text writeStartAddressText;
    @FXML
    private TextField writeWordCount;
    @FXML
    private Text writeWordCountText;
    @FXML
    private TextField writeData;
    @FXML
    private Text writeDataText;
    @FXML
    private CheckBox writeTimestamp;
    @FXML
    private Pane writeDataPane;
    @FXML
    private CheckBox reReport;
    @FXML
    private CheckBox reReportEnable;
    @FXML
    private TextField newTagDelay;
    @FXML
    private TextField sameTagDelay;
    @FXML
    private Text newTagDelayText;
    @FXML
    private Text sameTagDelayText;
    @FXML
    private Text tagEncodingText;
    @FXML
    private Text sessionText;
    @FXML
    private Text targetText;
    @FXML
    private Text qText;

    //Read Tab
    ReadListener readListener;
    ReadExceptionListener exceptionListener;
    StatsListener statsListener;
    int cloumnCount=4;
    long totalTag;
    int uniqueTag;
    @FXML 
    private Text uniqueTagCount;
    @FXML 
    private Text totalTagCount;
    ObservableList<TagResults> row;
    private boolean isAutonomousReadStarted = false;

    //Results Tab Elara CDC
    @FXML 
    private Text resultsTabWorkflowName;

    @FXML 
    private Text uniqueTagCountElara;
    @FXML 
    private Text totalTagCountElara;
    long totalTagsElara = 0;
    int uniqueTagsElara = 0;

    //Enable or Disable Opacity
    double buttonEnableOpacity = 1.0;
    double enableOpacity = 0.7;
    double disableElaraOpacity = 0.3;
    double enableElaraOpacity = 1;
    double disableOpacity = 0.09;
    
    List<String> myList;
    Task progressTask;
    FileChooser fileChooser;
    Map<String, String> saveParams = new HashMap<String, String>();
    LoadSaveConfig loadSave ;
    List<String> fileFilters;
    int minReaderPower = 0;
    int maxReaderPower = 0;
    List<Integer> existingAntennas;
    List<String> supportedProtocols;
    String hardWareVersion, readerModel = "", firmwareVerson;

    //configurations change massage variables for change list
    boolean showChangeList = false;
    HashMap<String, String> changeListMap = new HashMap<String, String>();
    HashMap<String,String> elaraChangeListMap = new HashMap<>();
    HashMap<String,String> currentListMap = new HashMap<>();
    TextField customComportField;
    boolean isAutonomousSupported = false, isTransportLogsEnabled = false;
    String connectedDevice;
    List<String> comportList = new ArrayList<String>();
    Alert alert = new Alert(AlertType.INFORMATION);
    int statsTemperature = 0;
    SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss.SSS");
    SimpleDateFormat dfhms = new SimpleDateFormat("yyyy-MM-dd-HH-mm-ss");
    TransportListener transportListener;
    ElaraTransportListener elaraTransportListener;
    BufferedWriter transportWriter;
    Reader.Region currentRegion = Reader.Region.UNSPEC;
    TagProtocol currentProtocol = null;
    StringBuffer loadSaveError = null;
    Properties loadSaveProperties;
    volatile boolean isLoadSaveCompleted = false;
    boolean isRegionChanged = true;

    // SerialPort object
    public static SerialPort sp;
    //variable to check if Elara is in CDC mode
    private boolean isElaraCDC = false;
    //Boolean to check if listener is attached
    private boolean hasElaraListener = false;
    // hid variables
    private static final Integer VENDOR_ID = 0x2008;
    private static final Integer PRODUCT_ID = 0x2001;
    public static final String SERIAL_NUMBER = null;
    private int negativeBufferCount = 0;
    public ElaraJSONParser ejsonp;
    HashMap<String,String> getInfoResponse = new HashMap<>();
    HashMap<String,String> initSettingsMap = new HashMap<>();
    private String elaraRFIDSensorModel;
    Thread checkThread;
    private String accessPwd;
    private boolean isRSKit = false;
    String defaultElaraWorklfow[] = {"HDR (Single Tag Read Near)", "Monitor (Bulk Read Far)", "Commission Single Tag", "Update Tag"}; 
    private String elaraFwVersion;
    
    @Override
    public void initialize(URL location, ResourceBundle resources) 
    {
        // store the actual readTab and configureTab index here
        readTabIndex = mainTabs.getTabs().indexOf(readTab);
        configureTabIndex = mainTabs.getTabs().indexOf(configureTab);
        new Thread(findReadersThread).start();
        mainTabListener();
        disableFeatures();
        powerSlider();
        elaraPowerSlider();
        validateTextFields();
        checkBoxListener();
        //Setting ToolTip
        setTooltip();
        //Addind and deleting rows based on selection
        tableViewConfiguration();

        ObservableList<String> row = FXCollections.observableArrayList();

        probeBaudRate.getSelectionModel().select("115200");
        probeElaraBaudRate.getSelectionModel().select("115200");
        setNoColoumsInTable(cloumnCount);
        //Select First TitledPane in Accordion
        ObservableList<TitledPane> list = accordion.getPanes();
        accordion.setExpandedPane(list.get(0));
        applyButton.setDisable(true);
        applyButton.setOpacity(buttonEnableOpacity);
        revertButton.setDisable(true);
        revertButton.setOpacity(buttonEnableOpacity);
        setHomeContent();
        showAboutInfo();
    }
    
    public void disableFeatures()
    {
        // By default readTab should be there. Hence removing readTabElaraHID from mainTabs at initialization.
        mainTabs.getTabs().remove(resultsTabElaraHID);
        mainTabs.getTabs().remove(configureTabElara);
        mainTabs.getTabs().remove(utilsTabElara);
        mainTabs.getTabs().remove(resultsTabElaraCDC);
        quickSearch.setDisable(true);
        quickSearch.setOpacity(enableOpacity);

        dutyCycleOff.setDisable(false);
        dutyCycleOff.setOpacity(enableOpacity);
        dutyCycleOn.setDisable(false);
        dutyCycleOn.setOpacity(enableOpacity);

        dutyCycleOffText.setOpacity(buttonEnableOpacity);
        dutyCycleOnText.setOpacity(buttonEnableOpacity);
        
        dutyCycleOff.setText("0");
        dutyCycleOn.setText("1000");

        epcStreamFormat.setDisable(true);
        epcStreamFormat.setOpacity(disableOpacity);
        
        dynamic.setDisable(true);
        dynamic.setOpacity(disableOpacity);
        equalTime.setOpacity(disableOpacity);
        equalTime.setDisable(true);
        connectProgressIndicator.visibleProperty().setValue(false);
        
        keepAliveTime.setVisible(false);
        keepAliveTimeLabel.setVisible(false);
        
        antenna1Elara.setDisable(true);
        antenna1Elara.setSelected(true);
        
        gen2Elara.setDisable(true);
        gen2Elara.setSelected(true);
        
        gpiTriggerReadElara.setDisable(true);
        regulatoryElara.setDisable(true);
        diagnosticsElara.setDisable(true);
        
        link250KhzElara.setDisable(true);
        tari25usElara.setDisable(true);
        tari12_5usElara.setDisable(true);
        tari6_25usElara.setDisable(true);
        filterMemoryBank.setDisable(true);
        startAddress.setDisable(true);
        filterData.setDisable(true);
        elaraMetadataProfile.setDisable(true);
        elaraMetadataRZ.setDisable(true);
        elaraMetadataGen2BI.setDisable(true);
        //elaraMetadataSensor.setDisable(true);
        infoTitlePane.setVisible(false);
        elaraReaderModelPane.setVisible(false);
        readerSerialNumberPane.setVisible(false);
        readerVersionPane.setVisible(false);
        readerTypePane.setVisible(false);
        sensorSerialPane.setVisible(false);
        sensorVersionPane.setVisible(false);
        datepicker.setDisable(true);
        hoursTextField.setDisable(true);
        minutesTextField.setDisable(true);
        secondsTextField.setDisable(true);
        datepicker.setOpacity(1);
        hoursTextField.setOpacity(0.7);
        minutesTextField.setOpacity(0.7);
        secondsTextField.setOpacity(0.7);
    }

    public void setStage(Stage stage) 
    {
        this.stage = stage;
    }

    public void mainTabListener() 
    {
        ImageView imv = new ImageView(new Image("images/home_active.png"));
        homeTab.setGraphic(imv);
        imv = new ImageView(new Image("images/connect.png"));
        connectTab.setGraphic(imv);
        imv = new ImageView(new Image("images/config.png"));
        configureTab.setGraphic(imv);
        imv = new ImageView(new Image("images/config.png"));
        configureTabElara.setGraphic(imv);
        imv = new ImageView(new Image("images/utility_inactive.png"));
        utilsTabElara.setGraphic(imv);
        imv = new ImageView(new Image("images/read.png"));
        readTab.setGraphic(imv);
        imv = new ImageView(new Image("images/read.png"));
        resultsTabElaraHID.setGraphic(imv);
        imv = new ImageView(new Image("images/read.png"));
        resultsTabElaraCDC.setGraphic(imv);
        imv = new ImageView(new Image("images/help.png"));
        helpTab.setGraphic(imv);

        mainTabs.getSelectionModel().selectedItemProperty().addListener(
                new ChangeListener<Tab>() 
                {
                    public void changed(ObservableValue<? extends Tab> ov, Tab previousTab, Tab newTab) 
                    {
                        hideMessagePopup();
                        if (previousTab.getText().equalsIgnoreCase("home")) 
                        {
                            ImageView imv = new ImageView(new Image("images/home.png"));
                            homeTab.setGraphic(imv);
                        } 
                        else if (previousTab.getText().equalsIgnoreCase("connect")) 
                        {
                            ImageView imv = new ImageView(new Image("images/connect.png"));
                            connectTab.setGraphic(imv);
                        }
                        else if (previousTab.getText().equalsIgnoreCase("configure")) 
                        {
                            ImageView imv = new ImageView(new Image("images/config.png"));
                            configureTab.setGraphic(imv);
                            configureTabElara.setGraphic(imv);
                        }
                        else if (previousTab.getText().equalsIgnoreCase("help")) 
                        {
                            ImageView imv = new ImageView(new Image("images/help.png"));
                            helpTab.setGraphic(imv);
                        } 
                        else if (previousTab.getText().equalsIgnoreCase("read")) 
                        {
                            ImageView imv = new ImageView(new Image("images/read.png"));
                            readTab.setGraphic(imv);
                        }
                        else if (previousTab.getText().equalsIgnoreCase("results")) 
                        {
                            ImageView imv = new ImageView(new Image("images/read.png"));
                            resultsTabElaraHID.setGraphic(imv);
                            resultsTabElaraCDC.setGraphic(imv);
                        }
                        else if (previousTab.getText().equalsIgnoreCase("utilities")) 
                        {
                            ImageView imv = new ImageView(new Image("images/utility_inactive.png"));
                            utilsTabElara.setGraphic(imv);
                        }

                        if (newTab.getText().equalsIgnoreCase("home")) 
                        {
                            ImageView imv = new ImageView(new Image("images/home_active.png"));
                            homeTab.setGraphic(imv);
                        }
                        else if (newTab.getText().equalsIgnoreCase("connect")) 
                        {   
                            ImageView imv = new ImageView(new Image("images/connect_active.png"));
                            connectTab.setGraphic(imv);
                        } 
                        else if (newTab.getText().equalsIgnoreCase("configure")) 
                        {
                            ImageView imv = new ImageView(new Image("images/config_active.png"));
                            configureTab.setGraphic(imv);
                            configureTabElara.setGraphic(imv);
                            if (!isConnected)
                            {
                                mainTabs.getSelectionModel().select(connectTab);
                                showWarningErrorMessage("warning","Please connect reader to configure");
                            }
//                            else if(isAutonomousReadStarted)
//                            {
//                                showWarningErrorMessage("warning","Please disconnect and connect back to configure the reader");
//                            }
                        }
                        else if (newTab.getText().equalsIgnoreCase("utilities")) 
                        {
                            ImageView imv = new ImageView(new Image("images/utility_active.png"));
                            utilsTabElara.setGraphic(imv);
                            if (!isConnected)
                            {
                                mainTabs.getSelectionModel().select(connectTab);
                                showWarningErrorMessage("warning","Please connect reader to access utilities");
                            }
                        }
                        else if (newTab.getText().equalsIgnoreCase("help")) 
                        {
                            ImageView imv = new ImageView(new Image("images/help_active.png"));
                            helpTab.setGraphic(imv);
                        }
                        else if (newTab.getText().equalsIgnoreCase("read")) 
                        {
                            ImageView imv = new ImageView(new Image("images/read_active.png"));
                            readTab.setGraphic(imv);
                            if (!isConnected)
                            {
                                mainTabs.getSelectionModel().select(connectTab);
                                showWarningErrorMessage("warning","Please connect reader to access read tab");
                            }
                        }
                        else if (newTab.getText().equalsIgnoreCase("results")) 
                        {
                            ImageView imv = new ImageView(new Image("images/read_active.png"));
                            resultsTabElaraHID.setGraphic(imv);
                            resultsTabElaraCDC.setGraphic(imv);
                            if (!isConnected && !deviceName.equalsIgnoreCase("Elara HID"))
                            {
                                mainTabs.getSelectionModel().select(connectTab);
                                showWarningErrorMessage("warning","Please connect reader to access results tab");
                            }
                        }
                    }
                });
    }

    //Realod Devices list on  mouse released
    @FXML
    private void reloadDevices()
    {
        if(firmwareUpdateSuccess)
        {
            try 
            {
                Thread.sleep(3000);
            } 
            catch (InterruptedException ex) 
            {
                Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
            }
        }
        readerList.getChildren().clear();
        readerProperties.setVisible(false);
        reloadDevicesButton.setDisable(true);
        new Thread(findReadersThread).start();
    }
    
    //show list of readers
    private Runnable findReadersThread = new Runnable()
    {

        @Override
        public void run()
        {            
            Platform.runLater(new Runnable()
           {
               @Override
               public void run()
               {
                   reloadDevicesProgress.visibleProperty().setValue(true);
                   progressTask = createProgress();
                   reloadDevicesProgress.progressProperty().unbind();
                   reloadDevicesProgress.progressProperty().bind(progressTask.progressProperty());
               }
               
           });
           try
           {
            comportInfo.clear();
            comportList.clear();
            SerialPort[] portList;
            String[] results;
            // get comm ports
            portList = SerialPort.getCommPorts();
            results = new String[portList.length];
            for (int i = 0; i < portList.length; i++) 
            {
                results[i] = portList[i].getSystemPortName();
                if(portList[i].getPortDescription().equalsIgnoreCase("Elara Reader"))
                {
                    String deviceName = portList[i].getDescriptivePortName();
                    String deviceNameString = portList[i].getSystemPortName(); //deviceName.substring(deviceName.indexOf("(") + 1, deviceName.indexOf(")"));
                    setReaders("Elara (" + deviceNameString + ")");
                    comportList.add("Elara (" + deviceNameString + ")");
                }
                else
                {
                    setReaders(portList[i].getDescriptivePortName());
                    comportList.add(portList[i].getDescriptivePortName());
                }
            }
 
           //get HID devices connected
           enumerateHIDDevices();

           }
           catch(Error e)
           {
               
           }
           catch(Exception ex)
           {
               
           }
           setReaders("");
           stopReaderFindProgressBar();
        }
    };

    public void stopReaderFindProgressBar()
    {
        Platform.runLater(new Runnable()
           {
               @Override
               public void run()
               {
                   progressTask.cancel(true);
                   reloadDevicesProgress.progressProperty().unbind();
                   reloadDevicesProgress.setProgress(0);
                   reloadDevicesProgress.visibleProperty().setValue(false);
                   reloadDevicesButton.setDisable(false);
               }
           });
    }
    
    public void setReaders(final String deviceId)
    {
        Platform.runLater(new Runnable()
        {
            @Override
            public void run()
            {
                BorderPane bPane = new BorderPane();
                bPane.setStyle("-fx-background-color: transparent");
                bPane.setId("readerBorderPane");
                bPane.setPrefHeight(40);
                bPane.setMinHeight(40);

                Pane textPane = new Pane();
                if(deviceId.equals(""))
                {
                    customComportField = new TextField();
                    customComportField.setId("deviceText");
                    customComportField.setAlignment(Pos.BASELINE_LEFT);
                    customComportField.setText(deviceId);
                    customComportField.setEditable(true);
                    customComportField.setPromptText("Enter com port");
                    textPane.getChildren().add(customComportField);
                    customComportField.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
                    {

                        @Override
                        public void handle(MouseEvent event)
                        {
                            customComportField.setStyle("-fx-text-inner-color:black");
                        }
                    });
                    customComportField.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>() 
                    {

                        @Override
                        public void handle(MouseEvent event)
                        {
                            customComportField.setStyle("-fx-text-inner-color:black");
                            String comport = customComportField.getText();
                            if (comport.length() > 3 && !isConnected)
                            {
                                if (!comport.toUpperCase().startsWith("COM")
                                        && !comport.startsWith("/dev"))
                                {
                                    deviceName = "dummy";
                                    customComportField.setText("");
                                    customComportField.setPromptText("Enter com port");
                                }
                                else if (comportList.contains(comport.toUpperCase()) || comportList.contains(comport))
                                {
                                   showWarningErrorMessage("warning", "Com port already exist in the list");
                                   customComportField.setText("");
                                   customComportField.setPromptText("Enter com port");
                                }
                                else
                                {
                                    deviceName = comport;
                                    HashMap hashMap = (HashMap) comportInfo.get("dummy");
                                    comportInfo.put(deviceName, hashMap);
                                    hideMessagePopup();
                                }
                            }
                        }
                    });
                    customComportField.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>() 
                    {
                        @Override
                        public void handle(KeyEvent keyEvent) 
                        {
                            customComportField.setStyle("-fx-text-inner-color:black");
                            String comport = customComportField.getText();
                            if (comport.length() > 3 )
                            {
                                if (!comport.toUpperCase().startsWith("COM")
                                        && !comport.startsWith("/dev")) 
                                {
                                    showWarningErrorMessage("warning", "Please enter valid com port");
                                    customComportField.setStyle("-fx-text-inner-color:red");
                                    keyEvent.consume();
                                }
                            }
                        }
                    });
                } 
                else 
                {
                    Text comportName = new Text();
                    comportName.setId("deviceText");
                    comportName.setTextAlignment(TextAlignment.LEFT);
                    comportName.setText(deviceId);
                    comportName.setLayoutY(20);
                    textPane.getChildren().add(comportName);
                }

                Pane pane = new Pane();
                pane.setMinWidth(100);
                pane.setPrefWidth(100);
                pane.setMaxWidth(100);

                Pane p = new Pane();
                ImageView img;
                if(deviceId.contains("Elara HID"))
                {
                    img = new ImageView(new Image("images/h_inactive.png")); 
                }
                else
                {
                    img = new ImageView(new Image("images/serial-small.png"));  
                }
                img.setFitWidth(30);
                img.setFitHeight(30);
                img.setLayoutY(-1);
                img.setOpacity(1);
                ImageView gretarImage = new ImageView(new Image("images/arrow-small.png"));
                gretarImage.setFitHeight(30);
                gretarImage.setFitWidth(20);
                gretarImage.setLayoutX(80);
                gretarImage.setOpacity(0.5);
                gretarImage.setLayoutY(-2);
                p.getChildren().add(img);
                p.getChildren().add(gretarImage);
                p.setOpacity(1);

                ImageView chainImage = new ImageView(new Image("images/link-big-active.png"));
                chainImage.setFitHeight(30);
                chainImage.setFitWidth(30);
                chainImage.setLayoutX(43);
                chainImage.setLayoutY(-2);
                chainImage.setOpacity(0.2);
                pane.getChildren().add(p);
                pane.getChildren().add(chainImage);

                bPane.setLeft(textPane);
                bPane.setRight(pane);
                readerList.getChildren().add(bPane);

                HashMap hashMap = new HashMap<String, Object>();
                hashMap.put("chainImage", chainImage);
                if(deviceId.equals(""))
                {
                    comportInfo.put("dummy", hashMap);
                } 
                else 
                {
                    comportInfo.put(deviceId, hashMap);
                }
                
                bPane.addEventFilter(
                        MouseEvent.MOUSE_RELEASED,
                        new EventHandler<MouseEvent>()
                        {
                            public void handle(final MouseEvent mouseEvent)
                            {
                                ObservableList<Node> children = readerList.getChildren();
                                for (Node node : children)
                                {
                                    //reverting css for all borderpanes to normal state 
                                    ((BorderPane) node).setStyle("-fx-background-color: transparent");
                                    BorderPane bp = (BorderPane) node;
                                    Pane p = (Pane) bp.getLeft();
                                    if (p.getChildren().get(0).getClass() == Text.class) 
                                    {
                                        Text t = (Text) p.getChildren().get(0);
                                        t.setFill(Color.BLACK);
                                    } 
                                    else
                                    {
                                        TextField t = (TextField) p.getChildren().get(0);
                                        t.setStyle("-fx-text-inner-color:black");
                                    }

                                    Pane borderPaneRightPane = (Pane) bp.getRight();
                                    //getting first childern in right border pane
                                    Pane childern1 = (Pane) borderPaneRightPane.getChildren().get(0);

                                    //getting second childern in right border pane
                                    ImageView childern2ChainImage = (ImageView) borderPaneRightPane.getChildren().get(1);

                                    //getting serial image and arrow image objects from childern1
                                    ImageView childern1SerialImage = (ImageView) childern1.getChildren().get(0);
                                    ImageView childern1ArrowImage = (ImageView) childern1.getChildren().get(1);

                                    childern2ChainImage.setImage(new Image("images/link-big-active.png"));
                                    if(p.getChildren().get(0).toString().contains("Elara HID"))
                                    {
                                        childern1SerialImage.setImage(new Image("images/h_inactive.png"));
                                    }
                                    else
                                    {
                                        childern1SerialImage.setImage(new Image("images/serial-small.png"));
                                    }
                                    childern1ArrowImage.setImage(new Image("images/arrow-small.png"));
                                    childern1ArrowImage.setOpacity(disableOpacity);

                                }
                                //changing selected borderpane css
                                BorderPane bp = (BorderPane) mouseEvent.getSource();
                                bp.setStyle("-fx-background-color: #4F8ABD");
                                Pane p = (Pane) bp.getLeft();
                                if (p.getChildren().get(0).getClass() == Text.class)
                                {
                                    Text t = (Text) p.getChildren().get(0);
                                    t.setFill(Color.WHITE);
                                    deviceName = ((Text) p.getChildren().get(0)).getText();
                                }
                                else
                                {
                                    TextField t = (TextField) p.getChildren().get(0);
                                    t.setStyle("-fx-text-inner-color:white");
                                    HashMap hashMap = (HashMap) comportInfo.get("dummy");
                                    deviceName = ((TextField) p.getChildren().get(0)).getText();

                                    if (false
                                    || deviceName.toUpperCase().startsWith("COM")
                                    || deviceName.startsWith("/dev")) 
                                    {
                                        comportInfo.put(deviceName, hashMap);
                                    } 
                                    else 
                                    {
                                        deviceName = "dummy";
                                    }
                                }

                                Pane borderPaneRightPane = (Pane) bp.getRight();

                                //first childern in right border pane
                                Pane childern1 = (Pane) borderPaneRightPane.getChildren().get(0);

                                //second childern in right border pane
                                ImageView childern2ChainImage = (ImageView) borderPaneRightPane.getChildren().get(1);

                                //getting serial image and arrow image from childern1
                                ImageView childern1SerialImage = (ImageView) childern1.getChildren().get(0);
                                ImageView childern1ArrowImage = (ImageView) childern1.getChildren().get(1);

                                //changing images when selected
                                childern2ChainImage.setImage(new Image("images/link-big-active-select.png"));
                                if(deviceName.equalsIgnoreCase("Elara HID"))
                                {
                                    childern1SerialImage.setImage(new Image("images/h_active.png"));  
                                }
                                else
                                {
                                    childern1SerialImage.setImage(new Image("images/serial-small-select.png"));
                                }
                                childern1ArrowImage.setImage(new Image("images/arrow-small-select.png"));
                                childern1ArrowImage.setOpacity(buttonEnableOpacity);

                                /* If the selected device is Elara HID device, remove existing readTab UI and 
                                 * replace it with resultsTabElaraHID UI.
                                 */
                                isElaraHID = false;
                                if(deviceName.equalsIgnoreCase("Elara HID") || deviceName.contains("Elara"))
                                {
                                    hidText.clear();
                                    readerProperties.setVisible(false);
                                    mainTabs.getTabs().remove(readTab);
                                    if(deviceName.equalsIgnoreCase("Elara HID"))
                                    {
                                        mainTabs.getTabs().remove(resultsTabElaraCDC);
                                        mainTabs.getTabs().add(readTabIndex, resultsTabElaraHID);
                                        mainTabs.getSelectionModel().select(resultsTabElaraHID);
                                        setReaderStatus(2);
                                        isElaraHID = true;
                                        if(isConnected)
                                        {
                                            new Thread(disConnectThread).start();
                                        }
                                    }
                                    else if(deviceName.contains("Elara (COM"))
                                    {                                       
                                        mainTabs.getTabs().remove(resultsTabElaraHID);
                                        mainTabs.getTabs().add(readTabIndex, resultsTabElaraCDC);
                                        if(!isConnected)
                                        {
                                            setReaderStatus(-1);
                                        }
                                        else
                                        {
                                            setReaderStatus(0); 
                                        }
                                    }
                                    Platform.runLater(new Runnable()
                                    {
                                        @Override
                                        public void run()
                                        {
                                            hidText.requestFocus();
                                            cdcText.requestFocus();
                                        }
                                    });
                                }
                                else
                                {
                                    mainTabs.getTabs().remove(resultsTabElaraHID);
                                    mainTabs.getTabs().remove(resultsTabElaraCDC);
                                    mainTabs.getTabs().add(readTabIndex, readTab);
                                    isElaraHID = false;
                                }
                                if(deviceName.contains("Elara"))
                                {
                                    mainTabs.getTabs().remove(configureTab);
                                    mainTabs.getTabs().add(configureTabIndex, configureTabElara);
                                    mainTabs.getTabs().add(configureTabIndex+2,utilsTabElara);
                                }
                                else
                                {
                                    mainTabs.getTabs().remove(configureTabElara);
                                    mainTabs.getTabs().remove(utilsTabElara);
                                    mainTabs.getTabs().add(configureTabIndex, configureTab);
                                }

                                if(!isElaraHID)
                                {
                                    if (isConnected)
                                    {
                                        HashMap hashMap = (HashMap) comportInfo.get(deviceName);
                                        if (hashMap.get("isConnected") != null)
                                        {
                                            connectionButton.setText("Disconnect");
                                            connectionButton.setStyle("-fx-background-color: #D80000");
                                            connectionButton.setDisable(false);
                                            connectionButton.setOpacity(1);
                                            readerProperties.setVisible(true);
                                        }
                                        else
                                        {
                                            readerProperties.setVisible(true);
                                            connectionButton.setText("Connect");
                                            connectionButton.setStyle("-fx-background-color:green");
                                            connectionButton.setDisable(true);
                                            connectionButton.setOpacity(disableOpacity);
                                        }
                                    }
                                    else
                                    {
                                    readerProperties.setVisible(true);
                                    connectionButton.setText("Connect");
                                    connectionButton.setStyle("-fx-background-color:green");
                                    connectionButton.setOpacity(buttonEnableOpacity);
                                    }
                                }
                            }
                        }
                );
            }
        });
    }
    
    @FXML
    private void connect(ActionEvent event) 
    {
        connectionButton.setDisable(true);
        cbTransportLogging.setDisable(true);
        cbTransportLogging.setOpacity(disableOpacity);
        isAutonomousSupported = false;
        try
        {          
            if(deviceName.equals("dummy"))
            {
                showWarningErrorMessage("warning", "Please enter valid comport or select from the list.");
                connectionButton.setDisable(false);
                return;
            }
            
            if (connectionButton.getText().equalsIgnoreCase("Connect"))
            {
                if(cbTransportLogging.isSelected())
                {
                    createTransportLogsIntoFile();
                    isTransportLogsEnabled = true;
                }
                new Thread(connectThread).start();
            }
            else
            {
                new Thread(disConnectThread).start();
            }
        }
        catch (Exception e)
        {
            notifyException(e);
        }
    }
  
    public void showWarningErrorMessage(final String type, final String message)
    {
        Platform.runLater(new Runnable()
        {
            public void run()
            {
                hideMessagePopup();
                popupMsgTitle.setFill(Color.WHITE);
                if (type.equals("warning"))
                {
                   popupMsgPane.setStyle("-fx-background-color:#FF970F");
                   popupMsgTitle.setText("Warning");
                   warningDownArrow.setVisible(true);
                }
                else if (type.equals("error"))
                {
                    popupMsgPane.setStyle("-fx-background-color:#D00006");
                    popupMsgTitle.setText("Error");
                    errorDownArrow.setVisible(true);
                } 
                else
                {
                    popupMsgPane.setStyle("-fx-background-color:#07871C");
                    popupMsgTitle.setText("Success");
                    successDownArrow.setVisible(true);
                }
                popupMsgPane.setVisible(true);
                popupMsgBorderPane.setVisible(true);
                popupMsgContentLabel.setText(message);
            }
        });
    }
 
    void hideMessagePopup()
    {
        warningDownArrow.setVisible(false);
        successDownArrow.setVisible(false);
        errorDownArrow.setVisible(false);
        popupMsgBorderPane.setVisible(false);
        popupMsgPane.setVisible(false);
    }
    
    public void notifyException(final Exception ex)
    {
        Platform.runLater(new Runnable()
        {
            @Override
            public void run()
            {
                customComportField.setEditable(true);
                progressTask.cancel(true);
                connectProgressIndicator.progressProperty().unbind();
                connectProgressIndicator.setProgress(0);
                connectProgressIndicator.visibleProperty().setValue(false);
                reloadDevices.setDisable(false);
                reloadDevicesButton.disableProperty().set(false);
                reloadDevices.setOpacity(1);
                connectionButton.setDisable(false);
                probeBaudRate.disableProperty().setValue(false);
                region.disableProperty().setValue(false);
                connectionButton.setText("Connect");
                cbTransportLogging.setDisable(false);
                cbTransportLogging.setOpacity(enableOpacity);
               
                if(ex.getMessage() != null && ex.getMessage().contains("Invalid argument"))
                {
                  showWarningErrorMessage("error", "Port does not exist.");  
                }
                else if(ex.getMessage() != null && ex.getMessage().equalsIgnoreCase(Constants.APPLICATION_IMAGE_FAILED))
                { 
                    isConnected = true;
                    JOptionPane.showMessageDialog(null, ex.getMessage() , "ERROR", JOptionPane.OK_OPTION);
                    mainTabs.getSelectionModel().select(configureTab);
                    setTitledPanesStatus(true, true, true, true, true, false, true);
                    setTitledPanesExpandedStatus(false, false, false, false, false, true, false);
                }
                else if(ex.getMessage() != null)
                {
                  showWarningErrorMessage("error", ex.getMessage());
                }
            }
        });
    }
   
    private Runnable connectThread = new Runnable()
    {
        @Override
        public void run()
        {
            String port = deviceName;
            boolean errorOpeningPort = false;
            final HashMap hashMap = (HashMap) comportInfo.get(port);
            final ImageView chainImageView = (ImageView) hashMap.get("chainImage");
            Platform.runLater(new Runnable()
            {
                @Override
                public void run()
                {
                    probeBaudRate.disableProperty().setValue(true);
                    region.disableProperty().setValue(true);
                    reloadDevicesButton.disableProperty().set(true);
                    reloadDevices.setDisable(true);
                    reloadDevices.setOpacity(0.07);
                    connectProgressIndicator.visibleProperty().setValue(true);
                    progressTask = createProgress();
                    connectProgressIndicator.progressProperty().unbind();
                    connectProgressIndicator.progressProperty().bind(progressTask.progressProperty());
                    customComportField.setEditable(false);
                    hideMessagePopup();
                }
            });
            try
            {
                String deviceNameString = port.substring(port.indexOf("(") + 1, port.indexOf(")"));
                JSONObject getInfoInterfaceJSON=null;
                sp = SerialPort.getCommPort(deviceNameString);
                sp.setComPortParameters(Integer.parseInt(probeBaudRate.getSelectionModel().getSelectedItem().toString()), 8, 1, 0);
                boolean portOpen = sp.openPort();
                if(portOpen)
                {
                    if(isTransportLogsEnabled)
                    {
                        elaraTransportListener = new SaveElaraTransportLogs();
                        hasElaraListener = true;
                    }
                    String getInfoInterfaceResp = null;
                    String getInfoInterfaceCmd = null;
                    sp.closePort();
                    boolean success = connectToPort(port);
                    if(success)
                    {  
                        String setStopHBCmd = ejsonp.formJSONCommand(ECTConstants.SET_CFG_STOP_HEARTBEAT);
                        String setStopHBResp = sendMessage(sp, setStopHBCmd, elaraTransportListener);
                        Thread.sleep(1000);
                        getInfoInterfaceCmd = ejsonp.formJSONCommand(ECTConstants.GET_INFO_FIELDS_ALL);
                        getInfoInterfaceResp = sendMessage(sp, getInfoInterfaceCmd, elaraTransportListener);
                        if(isValidJSON(getInfoInterfaceResp))
                        {
                            getInfoInterfaceJSON = new JSONObject(getInfoInterfaceResp);
                            if(getInfoInterfaceJSON.get("ErrID").equals(0)) 
                            //getInfoInterfaceJSON.get("Report").toString().equalsIgnoreCase("GetInfo")&&
                            {
                                isConnected = true;
                                if(getInfoInterfaceJSON.get("_Interfaces").toString().contains("RS232"))
                                {
                                    elaraRS232 = true;
                                    elaraBaudRatePane.setVisible(true);
                                    Platform.runLater(new Runnable() {
                                        @Override
                                        public void run() { 
                                            mainTabs.getTabs().remove(readTab);
                                            mainTabs.getTabs().remove(configureTab);
                                            mainTabs.getTabs().add(configureTabIndex, configureTabElara);
                                            mainTabs.getTabs().add(configureTabIndex+1, resultsTabElaraCDC);
                                            mainTabs.getTabs().add(configureTabIndex+2, utilsTabElara);
                                        }
                                    });
                                }
                                else
                                {
                                    elaraBaudRatePane.setVisible(false);
                                }
                                if(portOpen)
                                {
                                    InputStream in = sp.getInputStream();
                                    while(in.available() > 0)
                                    {
                                       in.read();
                                    }
                                    connectedDevice = deviceName ;
                                    isElaraCDC = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        sp.closePort();
                        r = Reader.create("tmr:///" + deviceNameString);
                        if(isTransportLogsEnabled)
                        {
                            transportListener = new SaveTransportLogs();
                            r.addTransportListener(transportListener);
                        }
                        exceptionListener = new TagReadExceptionReceiver();
                        r.addReadExceptionListener(exceptionListener);
                        r.paramSet("/reader/baudRate", Integer.parseInt(probeBaudRate.getSelectionModel().getSelectedItem().toString()));
                        r.connect();
                        r.removeReadExceptionListener(exceptionListener);
                        exceptionListener = null;
                        isConnected = true;
                        connectedDevice = deviceName ; 
                    }
                }
                else
                {
                    errorOpeningPort = true;
                    connectionButton.setDisable(false);
                    cbTransportLogging.setDisable(false);
                    cbTransportLogging.setOpacity(enableOpacity);
                    progressTask.cancel(true);
                    connectProgressIndicator.progressProperty().unbind();
                    connectProgressIndicator.visibleProperty().setValue(false);
                    probeBaudRate.setDisable(false);
                }
            }
            catch (Exception ex)
            {
                if(sp.isOpen())
                {
                    sp.closePort();
                }
                notifyException(ex);
                return;
            }

            Platform.runLater(new Runnable()
            {
                @Override
                public void run()
                {
                    try
                    {
                        hideMessagePopup();
                        if (isConnected)
                        {
                            boolean isConnectSuccess=false;
                            setReaderStatus(0);
                            connectionButton.setText("Disconnect");
                            connectionButton.setDisable(false);
                            regionElara.getItems().clear();
                            setTitledPanesStatus(false, false, false, false, false, false, false);
                            setTitledPanesExpandedStatus(true, false, false, false, false, false, false);
                            //setDisableTitledPanes(false);
                            if(!isElaraCDC)
                            {
                                initParams(r, hashMap);
                                isConnectSuccess = true;
                            }
                            else
                            {
                                cdcText.clear();
                                ejsonp = new ElaraJSONParser();
                                isConnectSuccess = getInfoForConnect(ejsonp);
                            }
                            if(isConnectSuccess)
                            {
                                if(!inBootLoaderMode)
                                {
                                    connectionButton.setStyle("-fx-background-color: #CE6060");
                                    hashMap.put("isConnected", isConnected);
                                    chainImageView.setOpacity(1);
                                    //Enable after connection
                                    applyButton.setDisable(false);
                                    applyButton.setOpacity(enableOpacity);
                                    revertButton.setDisable(false);
                                    revertButton.setOpacity(enableOpacity);
                                    loadFirmware.disableProperty().setValue(false);
                                    loadFirmware.setOpacity(enableOpacity);
                                    loadElaraFirmware.disableProperty().setValue(false);
                                    loadElaraFirmware.setOpacity(enableOpacity);
                                    loadConfigButton.disableProperty().setValue(false);
                                    loadConfigButton.setOpacity(enableOpacity);

                                    saveConfigButton.disableProperty().setValue(false);
                                    saveConfigButton.setOpacity(enableOpacity);
                                    region.setDisable(false);
                                    probeBaudRate.setDisable(true);
                                    //clear tags
                                    clearTags(new ActionEvent());
                                    progressTask.cancel(true);
                                    connectProgressIndicator.progressProperty().unbind();
                                    connectProgressIndicator.setProgress(0);
                                    connectProgressIndicator.visibleProperty().setValue(false);
                                }
                                if(isElaraCDC)
                                {
                                    if(!inBootLoaderMode)
                                    {
                                        new Thread(initParamsElaraThread).start();
                                    }
                                }
                            }
                            else
                            {
                                showWarningErrorMessage("error", "No response from reader. Unable to connect.");
                                connectionButton.setText("Connect");
                                connectionButton.setDisable(false);
                                regionElara.getItems().clear();
                                new Thread(disConnectThread).start(); 
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if(sp.isOpen())
                        {
                            sp.closePort();
                        }
                        new Thread(disConnectThread).start();
                        notifyException(ex);
                    }
                }
            });
            if(errorOpeningPort)
            {
                reloadDevicesButton.setDisable(false);
                reloadDevices.setOpacity(1);
                showWarningErrorMessage("error", "Error opening port. Try reloading devices and try again.");
            }
            return;
        }
    };

    // Send JSON message
    public String sendMessage(SerialPort sp, String message, ElaraTransportListener etl) {
        String response=null;
        try {
            boolean doNotReceive = false;
            //initialize input and output streams for use to communicate data
            InputStream in = sp.getInputStream();
            OutputStream out = sp.getOutputStream();
            StringBuilder data = new StringBuilder();
            data.append(message);
            data.append("\n");
            if(hasElaraListener)
            {
                etl.message(true, message);
            }
            if(message.contains("GetInfo"))
            {
                while(in.available() > 0)
                {
                    in.read();
                }
            }
            out.write(data.toString().getBytes());
            if((message.contains("ActivateUpdate") || message.contains("EndUpdate")) && !elaraRS232)
            {
                doNotReceive = true;
            }
            if(!doNotReceive)
            {
                response = receiveCMDMessage(message, in, etl);
            }
        } catch (IOException ex) {
            Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
            String exception = ex.getMessage();
            response = exception;
        }
        return response;
    }

    //Receive tagReads JSON response
    public String receiveMessage(InputStream in, ElaraTransportListener etl) {
        String result=null;
        try {
            int numBytes = 0;
            byte[] readBuffer;
            numBytes = in.available();
            if(numBytes < 0)
            {
                readBuffer = new byte[0];
                //Check to see if there is a physical disconnection of the reader.
                negativeBufferCount ++;
                if(negativeBufferCount > 50)
                {
                    isElaraReading = false;
                    checkThread = new Thread(new CheckThread(), "Check Thread");
                    checkThread.start();
                }
            }
            else
            {
                readBuffer = new byte[numBytes];
                //Received response, reset the count
                negativeBufferCount = 0;
            }
            long startTime = System.currentTimeMillis();
            while (in.available() > 0) 
            {
                long currentTime = System.currentTimeMillis();
                in.read(readBuffer);
                
                if((currentTime - startTime) > 2000)
                {
                    break;
                }
            }
            // print data
            result = new String(readBuffer);
            if(hasElaraListener)
            {
                etl.message(false, result);
            }
        } catch (IOException ex) {
            Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        }
        return result;
    }
    
    //Receive command JSON response
    public String receiveCMDMessage(String message, InputStream in, ElaraTransportListener etl) {
        String result=null;
        try {
            int numBytes = 0;
            boolean streamAvailable = false;
            JSONObject messageJSON = null;
            JSONObject resultJSON = null;
            int passCnt = 0;
            if(message!="" && message!= null && message.length()!=0)
            {
                messageJSON = new JSONObject(message);
            }
            long startTime = System.currentTimeMillis();
            StringBuffer sbin = new StringBuffer();
            StringBuffer readInStream = new StringBuffer();
            //Keep reading an appeding results
            if(messageJSON.get("Cmd").equals("StopRZ"))
            {
                do
                {
                    numBytes = in.available();
                    if(numBytes > 0)
                    {
                        byte[] readBuffer = new byte[numBytes];
                        in.read(readBuffer);
                        String inputResult = new String(readBuffer);
                        readInStream.append(inputResult);
                    }
                    else if(numBytes == 0)
                    {
                        Thread.sleep(500);
                    }
                    //Filtering StopRZ response
                    if(readInStream.toString().contains("StopRZ"))
                    {
                        String tempStr = readInStream.toString();
                        int stopRZIndex = tempStr.indexOf("{\"Report\":\"StopRZ\",\"ErrID\":0}");
                        if(stopRZIndex != -1)
                        {
                            String subStr = tempStr.substring(stopRZIndex, stopRZIndex + 29);
                            result = subStr;
                        }
                        else
                        {
                            result = readInStream.toString();
                        }
                    }
                    else
                    {
                        result = readInStream.toString();
                    }
                    passCnt++;
                }while(passCnt < 3);
            }
            else if(messageJSON.has("HBPeriod"))
            {
                do
                {
                    numBytes = in.available();
                    if(numBytes > 0)
                    {
                        byte[] readBuffer = new byte[numBytes];
                        in.read(readBuffer);
                        String inputResult = new String(readBuffer);
                        readInStream.append(inputResult);
                    }
                    else if(numBytes == 0)
                    {
                        Thread.sleep(200);
                    }
                    //Filtering SetCfg response
                    if(readInStream.toString().contains("SetCfg"))
                    {
                        String tempStr = readInStream.toString();
                        int setCfgIndex = tempStr.indexOf("{\"Report\":\"SetCfg\",\"ErrID\":0}");
                        if(setCfgIndex != -1)
                        {
                            String subStr = tempStr.substring(setCfgIndex, setCfgIndex + 29);
                            result = subStr;
                        }
                        else
                        {
                            result = readInStream.toString();
                        }
                    }
                    else
                    {
                        result = readInStream.toString();
                    }
                    passCnt++;
                }while(passCnt < 3);
            }
            else
            {
                while (!streamAvailable) 
                {
                    numBytes = in.available();
                    byte[] readBuffer = new byte[numBytes];
                    in.read(readBuffer);
                    if(message.contains("ThisTag") && readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("READ"))
                    {
                        String inputResult = new String(readBuffer);
                        sbin.append(inputResult);
                        result = sbin.toString();
                        if(isValidJSON(sbin.toString()))
                        {
                            resultJSON = new JSONObject(result);
                            if(resultJSON.get("Report").equals("TagEvent"))
                            {
                                streamAvailable = true;
                            }
                        }
                    }
                    else if(messageJSON.has("Read") || messageJSON.has("Write"))
                    {
                        String inputResult = new String(readBuffer);
                        sbin.append(inputResult);
                        if(isValidJSON(sbin.toString()))
                        {
                            String tempStr = sbin.toString().trim();
                            resultJSON = new JSONObject(tempStr);
                            int setProfReadIndex = tempStr.indexOf("{\"Report\":\"SetProf\",\"ErrID\":0}");
                            if(setProfReadIndex != -1)
                            {
                                String subStr = tempStr.substring(setProfReadIndex, setProfReadIndex + 30);
                                result = subStr;
                                streamAvailable=true;
                            }
                            else if(!resultJSON.toString().contains("TagEvent") && resultJSON.has("ErrID") && resultJSON.has("ErrInfo"))
                            {
                                result = tempStr;
                                streamAvailable=true;
                            }
                        }
                    }
                    else
                    {
                        String inputResult = new String(readBuffer);
                        sbin.append(inputResult);
                        if(isValidJSON(sbin.toString()))
                        {
                            //result = sbin.toString();
                            resultJSON = new JSONObject(sbin.toString());
                            if(resultJSON.get("Report").equals("HB"))
                            {
                               streamAvailable = true; 
                            }
                            else
                            {
                                if(resultJSON.toString().contains("ErrID"))
                                {
                                    if(resultJSON.get("ErrID").equals(0)) 
                                    //resultJSON != null && messageJSON.get("Cmd").toString().equalsIgnoreCase(resultJSON.get("Report").toString()))&&    
                                    {
                                       streamAvailable = true;
                                       result = sbin.toString();
                                       break;
                                    }
                                    else if(!resultJSON.get("ErrID").equals(0))
                                    {
                                        result = sbin.toString();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    long currentTime = System.currentTimeMillis();
                    if((currentTime - startTime) > 5000)
                    {
                        break;
                    }
                }
            }
            sbin.delete(0, sbin.length());
            readInStream.delete(0, sbin.length());
            if(hasElaraListener)
            {
                etl.message(false, result);
            }
        } catch (IOException ex) {
            Logger.getLogger(ElaraJSONParser.class.getName()).log(Level.SEVERE, null, ex);
        } catch (JSONException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        } catch (InterruptedException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
        return result;
    }
    
    private Runnable initParamsElaraThread = new Runnable()
    {
        @Override
        public void run() {
            try {
                    Platform.runLater(() -> {   
                        try
                        {
                            initParamsElara(ejsonp);
                        }
                        catch (Exception ex) 
                        {
                            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
                        }
                    });
            } catch (Exception ex) {
                Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
            }
        }
    };
    
    private Runnable disConnectThread = new Runnable()
    {
        @Override
        public void run() 
        {
            try
            {
                Platform.runLater(new Runnable() 
                {
                    @Override
                    public void run() 
                    {
                        connectTab.setDisable(false);
                        btGetStarted.setDisable(false);
                        connectProgressIndicator.visibleProperty().setValue(true);
                        progressTask = createProgress();
                        connectProgressIndicator.progressProperty().unbind();
                        connectProgressIndicator.progressProperty().bind(progressTask.progressProperty());
                        infoTitlePane.setVisible(false);
                        elaraReaderModelPane.setVisible(false);
                        readerSerialNumberPane.setVisible(false);
                        readerVersionPane.setVisible(false);
                        readerTypePane.setVisible(false);
                        sensorSerialPane.setVisible(false);
                        sensorVersionPane.setVisible(false);
                        configureTabElara.setDisable(false);
                        utilsTabElara.setDisable(false);
                        resultsTabElaraCDC.setDisable(false);
                        resultsTabElaraHID.setDisable(false);
                    }
                });
                
                long startTime = System.currentTimeMillis();
                final HashMap hashMap = (HashMap)comportInfo.get(connectedDevice);
                final ImageView chainImageView =(ImageView)hashMap.get("chainImage");
                Platform.runLater(new Runnable()
                {
                    @Override
                    public void run()
                    {
                        connectProgressIndicator.visibleProperty().setValue(true);
                        progressTask = createProgress();
                        connectProgressIndicator.progressProperty().unbind();
                        connectProgressIndicator.progressProperty().bind(progressTask.progressProperty());
                    }
                });
                if(isAutonomousReadStarted)
                {
                    isReading = false;
                    r.removeReadListener(readListener);
                    r.removeReadExceptionListener(exceptionListener);
                    r.removeStatsListener(statsListener);
                    if(isTransportLogsEnabled)
                    {    
                        r.removeTransportListener(transportListener);
                        isTransportLogsEnabled = false;
                    }
                    isAutonomousReadStarted = false;
                }
               
                try
                {
                    if(isElaraCDC)
                    {
                        hasElaraListener = false;
                        if(sp.isOpen())
                        {   
                            checkThread.interrupt();
                            String setStopRzCmd = ejsonp.formJSONCommand(ECTConstants.STOP_RZ);
                            String setStopRzResp = sendMessage(sp, setStopRzCmd, elaraTransportListener);
                            String elaraHeartbeatCommand = ejsonp.formJSONCommand(ECTConstants.SET_CFG_STOP_HEARTBEAT);
                            sendMessage(sp, elaraHeartbeatCommand, elaraTransportListener);
                            sp.closePort();
                        }
                        Platform.runLater(new Runnable()
                        {
                            @Override
                            public void run()
                            {
                                writeStartAddress.setText("");
                                writeWordCount.setText("");
                                writeData.setText("");
                                accessPassword.setText("");
                            }
                        });
                    }
                    if(sp.isOpen())
                    {
                        boolean closePort = sp.closePort();
                    }
                    if(!isElaraCDC && !elaraRS232)
                    {
                        r.destroy();
                        r = null;
                    }
                    isElaraCDC = false;
                    elaraRS232 = false;
                    hidEnabled = false;
                    isElaraHID = false;
                    isRSKit = false;
                    reenableFields();
                    isElaraReading = false;
                    isTransportLogsEnabled = false;
                    negativeBufferCount = 0;
                } 
                catch (Exception ex)
                {

                }
                
                Platform.runLater(new Runnable() 
                {
                    @Override
                    public void run() 
                    {
                        hideMessagePopup();
                        probeBaudRate.disableProperty().setValue(false);
                        probeBaudRate.setOpacity(1.0);
                        connectionButton.setDisable(false);
                        isConnected = false;
                        reloadDevices.setDisable(false);
                        reloadDevices.setOpacity(1);
                        //setReaderStatus(-1);
                        connectionButton.setText("Connect");
                        connectionButton.setStyle("-fx-background-color:green");
                        connectionButton.setOpacity(buttonEnableOpacity);
                        chainImageView.setOpacity(0.2);
                        comportInfo.remove(hashMap);
                        applyButton.setDisable(true);
                        applyButton.setOpacity(disableOpacity);
                        revertButton.setDisable(true);
                        revertButton.setOpacity(disableOpacity);
                        reloadDevicesButton.disableProperty().set(false);
                        resultsTabWorkflowName.setText("");
                        progressTask.cancel(true);
                        connectProgressIndicator.progressProperty().unbind();
                        connectProgressIndicator.setProgress(0);
                        connectProgressIndicator.visibleProperty().setValue(false);
                        customComportField.setEditable(true);
                        cbTransportLogging.setDisable(false);
                        cbTransportLogging.setOpacity(enableOpacity);
                        cbTransportLogging.setSelected(false);
                        if(!deviceName.equalsIgnoreCase("Elara HID"))
                        {
                            setReaderStatus(-1);
                            mainTabs.getSelectionModel().select(connectTab);   
                        }
                        toggleButtonStart.setText("Start");
                        toggleButtonStart.setStyle("-fx-background-color: #28B86D; ");
                        temperatureElara.setText("");
                    }
                });
                return ;
            }
            catch(Exception e)
            {
                
            }
        }   
    };

    public boolean getInfoForConnect(ElaraJSONParser ejsonp)
    {
        boolean connectSuccess = false;
        try 
        {
            String getInfoFieldsCmd = ejsonp.formJSONCommand(ECTConstants.GET_INFO_FIELDS_ALL);
            String getInfoFieldsResp = sendMessage(sp, getInfoFieldsCmd, elaraTransportListener);
            JSONObject getInfoJSON = new JSONObject(getInfoFieldsResp);
            if(!getInfoJSON.get("Version").toString().contains("."))
            {
                accordionElaraDiagnostics.setExpandedPane(elaraFirmwareUpdate);
                inBootLoaderMode = true;
                mainTabs.getSelectionModel().select(utilsTabElara);
                connectTab.setDisable(true);
                btGetStarted.setDisable(true);
                configureTabElara.setDisable(true);
                resultsTabElaraCDC.setDisable(true);
                resultsTabElaraHID.setDisable(true);
                showWarningErrorMessage("warning", "Reader in boot loader mode. Please update firmware to continue");
                connectSuccess=true;
            }
            else
            {
                // parse the response
                if(getInfoJSON.get("Report").equals("GetInfo") && getInfoJSON.get("ErrID").equals(0))
                {
                    getInfoResponse = ejsonp.parseGetInfo(getInfoFieldsResp);
                    if(myList!=null)
                    {
                        myList.clear();
                    }
                    elaraReaderModel.setText(getInfoResponse.get("RdrModel"));
                    readerSerialNumber.setText(getInfoResponse.get("rdrSN"));
                    readerVersion.setText(getInfoResponse.get("version"));
                    elaraFwVersion = getInfoResponse.get("version");
                    readerType.setText(getInfoResponse.get("type"));
                    sensorSerial.setText(getInfoResponse.get("sn"));
                    sensorVersion.setText(getInfoResponse.get("fVersion"));
                    infoTitlePane.setVisible(true);
                    elaraReaderModelPane.setVisible(true);
                    readerSerialNumberPane.setVisible(true);
                    readerVersionPane.setVisible(true);
                    readerTypePane.setVisible(true);
                    sensorSerialPane.setVisible(true);
                    sensorVersionPane.setVisible(true);
                    connectSuccess= true;
                }
                else
                {
                    if(getInfoJSON.toString().contains("ErrInfo"))
                    {
                        String errorMsg = (String) getInfoJSON.get("ErrInfo");
                        showWarningErrorMessage("error", errorMsg);
                    }
                    else
                    {
                        showWarningErrorMessage("error", "Failed to get information from reader.");
                    }
                    
                }
            }
        }
        catch (JSONException ex) {
            connectSuccess = false;
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
        return connectSuccess;
    }
    
    public void initParams(Reader r,HashMap hashMap) throws Exception
    {
        try
        { 
            hideMessagePopup();
            getReaderDiagnostics();
            isSupportsAutonomus(firmwareVerson);
            disableModuleUnsupportedFeatures();

            region.getItems().removeAll(region.getItems());
            currentRegion = (Reader.Region) r.paramGet("/reader/region/id");
            Reader.Region[] supportedRegions = (Reader.Region[]) r.paramGet("/reader/region/supportedRegions");
            //Adding Supported Regions to Hash Map 
            List supportedRegionsList = new ArrayList();
            supportedRegionsList.add(Arrays.asList(supportedRegions));
            hashMap.put("supportedRegions", supportedRegionsList);

            region.getItems().addAll(Arrays.asList(supportedRegions));
            if(currentRegion != null && currentRegion != Reader.Region.UNSPEC)
            {
               isRegionChanged = false;
               region.getSelectionModel().select(currentRegion);
            }
            else
            {
                region.getSelectionModel().clearSelection();
                region.setValue(null);
            }
            
            currentProtocol = (TagProtocol) r.paramGet("/reader/tagop/protocol");
            if(currentProtocol != null && TagProtocol.GEN2 == currentProtocol)
            {
                gen2.setSelected(true);
            }
            else
            {
                gen2.setSelected(false);
            }
            if(!isLoadSaveConfiguration)
            {
                antennaDetection.setSelected(Boolean.parseBoolean(r.paramGet("/reader/antenna/checkPort").toString()));
                configureAntennaBoxes(r);
            }
              regionBasedPowerListener();

            //embeddedMemoryBank.getSelectionModel().select("EPC");

            try
            {    
                Gen2.LinkFrequency lf = (Gen2.LinkFrequency) r.paramGet("/reader/gen2/BLF");
                if (lf == Gen2.LinkFrequency.LINK250KHZ) 
                {
                    link250Khz.setSelected(true);
                } 
                else if (lf == Gen2.LinkFrequency.LINK640KHZ) 
                {
                    link640Khz.setSelected(true);
                }
            }
            catch(Exception e)
            {
                r.paramSet("/reader/gen2/BLF", Gen2.LinkFrequency.LINK250KHZ);
                showWarningErrorMessage("error", "Unknown link frequency found, applying default link frequency to module");
            }

            try
            {
                Gen2.Tari tari = (Gen2.Tari) r.paramGet("/reader/gen2/tari");
                if (tari == Gen2.Tari.TARI_6_25US) 
                {
                    tari6_25us.setSelected(true);
                }
                else if (tari == Gen2.Tari.TARI_12_5US)
                {
                    tari12_5us.setSelected(true);
                }
                else if (tari == Gen2.Tari.TARI_25US) 
                {
                    tari25us.setSelected(true);
                }
            }
            catch(Exception e){
                r.paramSet("/reader/gen2/tari", Gen2.Tari.TARI_6_25US);
                showWarningErrorMessage("error", "Unknown tari found, applying default link frequency to module");
            }

            //storing Gen2 BLF Configuration for change list
            //gen2BlfChangeConfiguration();

            Gen2.Target target = (Gen2.Target) r.paramGet("/reader/gen2/target");

            if (target == Gen2.Target.A) 
            {
                targetA.setSelected(true);
            }
            else if (target == Gen2.Target.AB)
            {
                targetAB.setSelected(true);
            } 
            else if (target == Gen2.Target.B) 
            {
                targetB.setSelected(true);
            }
            else if (target == Gen2.Target.BA)
            {
                targetBA.setSelected(true);
            }

            //storing Gen2 Target Configuration for change list
            //getGen2TargetChangeConfiguration();

            Gen2.TagEncoding tagEncoding = (Gen2.TagEncoding) r.paramGet("/reader/gen2/tagEncoding");
            if (tagEncoding == Gen2.TagEncoding.FM0) 
            {
                fm0.setSelected(true);
            } 
            else if (tagEncoding == Gen2.TagEncoding.M2) 
            {
                m2.setSelected(true);
            }
            else if (tagEncoding == Gen2.TagEncoding.M4) 
            {
                m4.setSelected(true);
            }
            else if (tagEncoding == Gen2.TagEncoding.M8) 
            {
                m8.setSelected(true);
            }

            //storing Gen2 Tag Encoding configuration for change list
            //getGen2TagEncodingChangeConfiguration();

            Gen2.Session session =(Gen2.Session) r.paramGet("/reader/gen2/session");
            if(session == Gen2.Session.S0)
            {
                sessionS0.setSelected(true);
            }
            else if(session == Gen2.Session.S1)
            {
                sessionS1.setSelected(true);
            }
            else if(session == Gen2.Session.S2)
            {
                sessionS2.setSelected(true);
            }
            else if(session == Gen2.Session.S3)
            {
                sessionS3.setSelected(true);
            }
            enableDisableGen2Settings(new ActionEvent());

            Gen2.Q gen2Q = (Gen2.Q)r.paramGet("/reader/gen2/q");
            if(gen2Q instanceof Gen2.DynamicQ)
            {
                dynamicQ.setSelected(true);
            }
            else if(gen2Q instanceof Gen2.StaticQ){
                staticQ.setSelected(true);
                Gen2.StaticQ staticQObj = (Gen2.StaticQ)gen2Q;
                staticQList.getSelectionModel().select(staticQObj.initialQ);
            }
            gen2Q(new ActionEvent());

            //storing session value for change list
            //getGen2SessionChangeConfiguration();
            //showChangeList = true;
       }
        catch(Exception e)
        {
            e.printStackTrace();
        }
    }

    // Function to initialize params for Elara
    public void initParamsElara(ElaraJSONParser ejsonp) throws Exception
    {
        try
        {
            hideMessagePopup();
            // get all the current configuration fields on the reader to populate in UI
            getCfgFieldsAllAndSetToUI(ejsonp);
            checkThread = new Thread(new CheckThread(), "Check Thread");
            checkThread.start();
            maxReadPowerElara.setText("27dBm");
            maxWritePowerElara.setText("27dBm");
            readPowerSliderElara.setMax(27.0);
            writePowerSliderElara.setMax(27.0);
        }
        catch(Exception e)
        {
            e.printStackTrace();
        }
    }

    private void getAndSetRegulatorySettingsToUI() throws ReaderException
    {
        setHopTimeToUI();
        setHopTableToUI();
    }

    private void setHopTimeToUI() throws ReaderException
    {
        hopTime.clear();
        int hopTimeValue = (Integer) r.paramGet(TMR_PARAM_REGION_HOPTIME);
        hopTime.setText(""+hopTimeValue);
    }

    private void setHopTableToUI() throws ReaderException
    {
        hopTable.clear();
        int[] hopTableValues = (int[]) r.paramGet(TMR_PARAM_REGION_HOPTABLE);
        hopTable.setText(parseHopTableValues(hopTableValues));
    }

    private String parseHopTableValues(int[] values){
        String string = Arrays.toString(values);
        string = string.substring(1, string.length()-1);
        String stringArray[] = string.split(",");
        int count = 0;
        int totalCoutnt = 0;
        for(int i = 0; i <= stringArray.length-1; i++){
            ++count;
            ++totalCoutnt;
            if(count >= 7 )
            {
             count = 0;
             stringArray[i] = "\n"+stringArray[i];
            }
        }
        hopTable.setPrefRowCount(totalCoutnt/6);
        String output = Arrays.toString(stringArray);
        output = output.substring(1, output.length()-1);
        return output;
    }

    private boolean setAntennaCheckPort(boolean checkPort)
    {
        try
        {
            r.paramSet("/reader/antenna/checkPort", checkPort);
            return true;
        }
        catch(Exception e)
        {
            antennaDetection.setSelected(false);
            showWarningErrorMessage("error", "unsupported parameter");
        }
        return false;
    }

    public void configureAntennaBoxes(Reader r) throws ReaderException
    {
        existingAntennas = new ArrayList<Integer>();
        List<Integer> detectedAntennas = new ArrayList<Integer>();
        List<Integer> validAntenns = new ArrayList<Integer>();
        boolean checkport;
        checkport = Boolean.parseBoolean(r.paramGet("/reader/antenna/checkPort").toString());        
        int[] temp = (int[])r.paramGet("/reader/antenna/portList");
        for (int i=0;i<temp.length;i++) {
            existingAntennas.add(temp[i]);
        }
        int[] temp1 = (int[])r.paramGet("/reader/antenna/connectedPortList");
        for (int i=0;i<temp1.length;i++) 
        {
            detectedAntennas.add(temp1[i]);
        }
        validAntenns = checkport?detectedAntennas:existingAntennas;
        
        CheckBox[] antennaBoxes = {antenna1,antenna2,antenna3,antenna4};
        int antNum =1;
        for (CheckBox cb : antennaBoxes)
        {
            if(existingAntennas.contains(antNum))
            {
                //cb.setDisable(false);
                cb.setVisible(true);
            }
            else
            {
                cb.setVisible(false);
            }
            if(!validAntenns.contains(antNum))
            {
                cb.setDisable(true);
            }
            else
            {
                cb.setDisable(false);
            }
            if(detectedAntennas.contains(antNum))
            {
                cb.setSelected(true);
            }
            else
            {
                cb.setSelected(false);
            }
            antNum++;
        }
    }

    @FXML
    private void findAntennas(ActionEvent event)
    {
        try
        {
            if(currentRegion != Reader.Region.UNSPEC)
            {    
                if(antennaDetection.isSelected())
                {
                   setAntennaCheckPort(true);
                   configureAntennaBoxes(r);
                  // addChangeList("Antenna detection was disabled. \n Now enabled");
                }
                else
                {
                    setAntennaCheckPort(false);
                    configureAntennaBoxes(r);
                  //  addChangeList("Antenna detection was enabled. \n Now disabled");
                }
            }
            else
            {
                antennaDetection.setSelected(false);
                showWarningErrorMessage("warning", "Please select the region");
            }
        }
        catch(ReaderException e)
        {
            e.printStackTrace();
        }
    }

    public void checkBoxListener()
    {

        //By default Auto Read On Gpi Disabled
        autoReadGpi1.setDisable(true);
        autoReadGpi1.setOpacity(disableOpacity);
        autoReadGpi2.setDisable(true);
        autoReadGpi2.setOpacity(disableOpacity);
        autoReadGpi3.setDisable(true);
        autoReadGpi3.setOpacity(disableOpacity);
        autoReadGpi4.setDisable(true);
        autoReadGpi4.setOpacity(disableOpacity);

        // By Default Embedded read data Disabled
        embeddedReadUnique.setDisable(true);
        embeddedReadUnique.setOpacity(disableOpacity);
        embeddedMemoryBank.setDisable(true);
        embeddedMemoryBank.setOpacity(disableOpacity);
        embeddedStart.setDisable(true);
        embeddedStart.setOpacity(disableOpacity);
        embeddedEnd.setDisable(true);
        embeddedEnd.setOpacity(disableOpacity);

        gpiTriggerRead.selectedProperty().addListener(new ChangeListener<Boolean>() 
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if (gpiTriggerRead.isSelected()) 
                {
                    changeUIOnGpiTriggerRead(false, enableOpacity);
                } 
                else
                {
                    changeUIOnGpiTriggerRead(true, disableOpacity);
                    autonomousRead.setSelected(false);
                }
            }
        });

        autonomousRead.selectedProperty().addListener(new ChangeListener<Boolean>() 
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if(autonomousRead.isSelected())
                {
                    gpiTriggerRead.setSelected(false);
                }
                else
                {
                }
            }
        });

        embeddedReadEnable.selectedProperty().addListener(new ChangeListener<Boolean>() 
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if (embeddedReadEnable.isSelected()) 
                {
                    embeddedReadUnique.setDisable(false);
                    embeddedReadUnique.setOpacity(enableOpacity);
                    embeddedMemoryBank.setDisable(false);
                    embeddedMemoryBank.setOpacity(enableOpacity);
                    embeddedMemoryBank.getSelectionModel().select(0);
                    embeddedStart.setDisable(false);
                    embeddedStart.setText("0");
                    embeddedStart.setOpacity(enableOpacity);
                    embeddedEnd.setDisable(false);
                    embeddedEnd.setOpacity(enableOpacity);
                    embeddedEnd.setText("0");
//                    addChangeList("Embedded read was disabled.\n Now enabled");
                    changeListMap.put("embeddedStart", embeddedStart.getText());
                    changeListMap.put("embeddedEnd", embeddedEnd.getText());
                }
                else
                {
//                    addChangeList("Embedded read was enabled.\n Now disabled");
                      disableEmbeddedReadData();
                }
            }
        });
        
       embeddedReadUnique.selectedProperty().addListener(new ChangeListener<Boolean>()
        {
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue)
            {
                if(embeddedReadUnique.isSelected())
                {
//                   addChangeList("Read unique by data was disabled.\n Now enabled");
                }
                else
                {
//                   addChangeList("Read unique by data was enabled.\n Now disabled");
                }
            }
        });
       
        embeddedMemoryBank.valueProperty().addListener(new ChangeListener<String>()
        {
            @Override
            public void changed(ObservableValue ov, String t, String t1)
            {
//                addChangeList("Embedded read memory bank was "+t+".\n Now "+ t1);
            }
        });
       
        popupCloseImg.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event)
            {
                warningDownArrow.setVisible(false);
                errorDownArrow.setVisible(false);
                successDownArrow.setVisible(false);
                popupMsgPane.setVisible(false);
                popupMsgBorderPane.setVisible(false);
            }
        });

        region.valueProperty().addListener(new ChangeListener<Reader.Region>()
        {
            @Override
            public void changed(ObservableValue<? extends Reader.Region> observable, Reader.Region oldValue, Reader.Region newValue) 
            {
                if(newValue != null && newValue != Reader.Region.UNSPEC)
                {
                    try
                    {
                        if(oldValue != newValue && isRegionChanged)
                        {
                            r.paramSet("/reader/region/id", newValue);
                        }
                        else if(!isRegionChanged)
                        {
                           isRegionChanged = true;
                        }
                        currentRegion = newValue;
                        regionBasedPowerListener();
                        getAndSetRegulatorySettingsToUI();
                    }
                    catch(Exception e)
                    {
                        showWarningErrorMessage("error","invalid or unsupported parameter");
                    }
                }
                else
                {
                    region.getSelectionModel().clearSelection();
                }
            }
      });
      
       gen2.selectedProperty().addListener(new ChangeListener<Boolean>() 
       {
             @Override
             public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
             {
                if(newValue)
                {
                    try
                    {    
                        r.paramSet("/reader/tagop/protocol", TagProtocol.GEN2);
                    }
                    catch(Exception e)
                    {
                        
                    }    
                }
             }
      });

    }

    public void powerSlider()
    {
        readPowerSlider.addEventFilter(MouseEvent.MOUSE_DRAGGED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                //readPowerChanged();
            }
        });

        readPowerSlider.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                readPowerChanged();
                writePowerChanged();
            }
        });
        writePowerSlider.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(writePowerSlider, readPowerSlider, rfWrite, rfRead);
                writePowerChanged();
                readPowerChanged();
            }
        });
        
        writePowerSlider.addEventFilter(MouseEvent.MOUSE_DRAGGED, new EventHandler<MouseEvent>()
        {

            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(writePowerSlider, readPowerSlider, rfWrite, rfRead);
            }
        });
        
        powerEquator.selectedProperty().addListener(new ChangeListener<Boolean>()
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if (powerEquator.isSelected())
                {
                    if(rfRead.getText().equals(""))
                    {
                       rfRead.setText(""+minReaderPower);
                    }
                    if(rfWrite.getText().equals(""))
                    {
                       rfWrite.setText(""+minReaderPower);
                    }
                    double d = Math.max(Double.parseDouble(rfRead.getText()), Double.parseDouble(rfWrite.getText()));
                    String text = "" + new DecimalFormat("##.##").format(d);
                    readPowerSlider.setValue(d);
                    writePowerSlider.setValue(d);
                    rfRead.setText(text);
                    rfRead.positionCaret(text.length());
                    rfWrite.setText(text);
                    rfWrite.positionCaret(text.length());
                    readPowerChanged();
                    writePowerChanged();
                } 

            }
        
        });
        
        rfRead.focusedProperty().addListener(new ChangeListener<Boolean>()
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) {
                if(!newValue) 
                {
                    powerFocusedlistener(rfRead, rfWrite, readPowerSlider);
                    readPowerChanged();
                }
            }
        });
        
        rfWrite.focusedProperty().addListener(new ChangeListener<Boolean>()
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if(!newValue) 
                {
                    powerFocusedlistener(rfWrite, rfRead, readPowerSlider);
                    writePowerChanged();
                }
            }
        });
        
        rfRead.addEventFilter(KeyEvent.KEY_RELEASED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent event) 
            {
                powerEventListener(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                readPowerChanged();
            }
        });
        
        rfWrite.addEventFilter(KeyEvent.KEY_RELEASED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent event) 
            {
                powerEventListener(writePowerSlider, readPowerSlider, rfWrite,rfRead);
               writePowerChanged();
            }
        });
        
        rfRead.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>()
        {            @Override
            public void handle(MouseEvent event) 
            {
                String text = rfRead.getText();
                if("".equals(text) || text.startsWith("-."))
                {
                    powerValueChanged(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                }
            }
        });
      
        rfWrite.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>()
        {            @Override
            public void handle(MouseEvent event) 
            {
                String text = rfWrite.getText();
                if("".equals(text) || text.startsWith("-."))
                {
                   powerValueChanged(writePowerSlider, readPowerSlider, rfWrite, rfRead);
                }
            }
        });
    }
    
    public void elaraPowerSlider()
    {
        readPowerSliderElara.addEventFilter(MouseEvent.MOUSE_DRAGGED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(readPowerSliderElara, writePowerSliderElara, rfReadElara, rfWriteElara);
            }
        });

        readPowerSliderElara.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(readPowerSliderElara, writePowerSliderElara, rfReadElara, rfWriteElara);
            }
        });
        writePowerSliderElara.addEventFilter(MouseEvent.MOUSE_CLICKED, new EventHandler<MouseEvent>()
        {
            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(writePowerSliderElara, readPowerSliderElara, rfWriteElara, rfReadElara);
            }
        });
        
        writePowerSliderElara.addEventFilter(MouseEvent.MOUSE_DRAGGED, new EventHandler<MouseEvent>()
        {

            @Override
            public void handle(MouseEvent event) 
            {
                powerSliderListener(writePowerSliderElara, readPowerSliderElara, rfWriteElara, rfReadElara);
            }
        });
        
        rfReadElara.focusedProperty().addListener(new ChangeListener<Boolean>()
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) {
                if(!newValue) 
                {
                    powerFocusedlistener(rfReadElara, rfWriteElara, readPowerSliderElara);
                }
            }
        });
        
        rfWriteElara.focusedProperty().addListener(new ChangeListener<Boolean>()
        {
            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue) 
            {
                if(!newValue) 
                {
                    powerFocusedlistener(rfWriteElara, rfReadElara, readPowerSliderElara);
                }
            }
        });
        
        rfReadElara.addEventFilter(KeyEvent.KEY_RELEASED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent event) 
            {
                powerEventListener(readPowerSliderElara, writePowerSliderElara, rfReadElara, rfWriteElara);
            }
        });
        
        rfWriteElara.addEventFilter(KeyEvent.KEY_RELEASED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent event) 
            {
                powerEventListener(writePowerSliderElara, readPowerSliderElara, rfWriteElara, rfReadElara);
            }
        });
        
        rfReadElara.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>()
        {            @Override
            public void handle(MouseEvent event) 
            {
                String text = rfReadElara.getText();
                if("".equals(text) || text.startsWith("-."))
                {
                    powerValueChanged(readPowerSliderElara, writePowerSliderElara, rfWriteElara, rfWriteElara);
                }
            }
        });
      
        rfWriteElara.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>()
        {            @Override
            public void handle(MouseEvent event) 
            {
                String text = rfWriteElara.getText();
                if("".equals(text) || text.startsWith("-."))
                {
                   powerValueChanged(writePowerSliderElara, readPowerSliderElara, rfWriteElara, rfWriteElara);
                }
            }
        });
    }
    
    public void powerValueChanged(Slider slider1, Slider slider2, TextField field1, TextField field2)
    {
        double value = slider1.getValue();
        
        if (powerEquator.isSelected())
        {
            if (value <= writePowerSlider.getValue())
            {
                field1.setText(""+slider2.getValue());
                field2.setText(""+slider2.getValue());
            }
            else
            {
                field2.setText(""+slider1.getValue());
            }
        } 
        else
        {
            field1.setText(""+slider1.getValue());
        }
    }

    public void powerEventListener(Slider slider1, Slider slider2, TextField field1, TextField field2)
    {
        String text = field1.getText();
        String tempText = "";
        int length = text.length();
        if(minReaderPower < 0 && (text.startsWith("-") || text.startsWith(".")))
        {
          if(text.length() > 1)
          {
             tempText = text.substring(1);
             if(tempText.contains("-"))
             {
               tempText = tempText.replaceAll("[^0-9.]", "");
               field1.setText("-"+tempText);
               field1.selectPositionCaret(("-"+tempText).length());
             }             
             text = tempText;
          }
          else
          {
              return;
          }
        }
        
        if (length > 0)
        {
            if (!text.matches("^[0-9]+([\\.][0-9]+)?$"))
            {
                int index = text.indexOf(".");
                text = text.substring(0, index + 1) + text.substring(index + 1, text.length()).replace(".", "");
                text = text.replaceAll("[^0-9.]", "");
                if (tempText.length() >= 1)
                {
                    field1.setText("-"+text);
                    field1.positionCaret(text.length()+1);
                }
                else
                {
                   field1.setText(text);
                   field1.positionCaret(text.length());
                }
            }
            
            text = field1.getText();
           
            double d = 0;
            if (text != null && (!"".equalsIgnoreCase(text) && !text.startsWith("-.")))
            {
                d = Double.parseDouble(text);                
            }
            if (d <= slider1.getMax())
            {
                if (powerEquator.isSelected())
                {
                    slider1.setValue(d);
                    field2.setText(text);
                    field2.positionCaret(text.length());
                } 
                else
                {
                    slider1.setValue(d);
                }
            }
            else
            {
                if (powerEquator.isSelected())
                {
                    slider1.setValue(slider1.getMax());
                    slider2.setValue(slider1.getMax());
                    text = "" + slider1.getMax();
                    field1.setText(text);
                    field1.positionCaret(text.length());
                    field2.setText(text);
                    field2.positionCaret(text.length());
                } 
                else
                {
                    slider1.setValue(slider1.getMax());
                    text = "" + slider1.getMax();
                    field1.setText(text);
                    field1.positionCaret(text.length());
                }
            }
        } 
        else
        {
            if (powerEquator.isSelected())
            {
                field2.setText("");
                slider1.setValue(slider1.getMin());
                slider2.setValue(slider1.getMin());
            }
        }
    }
    
    public void readPowerChanged()
    {
        String prevPower = changeListMap.get("readPower");
        String currentPower = rfRead.getText();
        if (!prevPower.equals(currentPower))
        {
            changeListMap.put("readPower", currentPower);
//            addChangeList("Read power changed from " + prevPower + " dBm to " + currentPower + " dBm.");
        }
        checkReadWritePowerOnUSBProModule();
    }
    
    public void writePowerChanged()
    {
        String prevPower = changeListMap.get("writePower");
        String currentPower = rfWrite.getText();
        if (!prevPower.equals(currentPower))
        {
            changeListMap.put("writePower", currentPower);
//            addChangeList("Write power changed from " + prevPower + " dBm to " + currentPower + " dBm.");
        }
        checkReadWritePowerOnUSBProModule();
    }
    
    public void powerFocusedlistener(TextField field1, TextField field2, Slider slider)
    {
        String text = field1.getText();
        if (text.equals(""))
        {
            text = "0";
        }
        else if((text.startsWith("-") || text.startsWith(".")) && text.length() == 1)
        {
            text = "0";
            field1.setText(text);
        }

        double d = Double.parseDouble(text);
        if (d < slider.getMin())
        {
            text = "" + slider.getMin();
            field1.setText(text);
            field1.positionCaret(text.length());

            if (powerEquator.isSelected())
            {
                field2.setText(text);
                field2.positionCaret(text.length());
            }
        }
    }
    
    public void powerSliderListener(Slider slider1, Slider slider2, TextField text1, TextField text2)
    {
        double d = slider1.getValue();
        String text = "" + new DecimalFormat("##.##").format(d);

        if (powerEquator.isSelected())
        {
            slider2.setValue(d);
            text1.setText(text);
            text1.positionCaret(text.length());
            text2.setText(text);
            text2.positionCaret(text.length());
        } 
        else
        {
            text1.setText(text);
            slider1.setValue(d);
            text1.positionCaret(text.length());
        }  
    }

    public void validateTextFields() 
    {
        // Listener for DutyCycle On TextField
        dutyCycleOn.textProperty().addListener(new ChangeListener<String>()
        {
            @Override
            public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue)
            {
                try
                {
                    if ((isDecimal(newValue)))
                    {
                        if(newValue.length() > 5 || Integer.parseInt(newValue) > 65535)
                        {
                            showWarningErrorMessage("error", "Please input dutycycle on timeout less than 65535");
                            dutyCycleOn.setText("1000");
                        }
                        else
                        {
                        dutyCycleOn.setText(newValue);
                        }
                    }
                    else
                    {
                        dutyCycleOn.setText("");
                    }
                }
                catch(Exception e)
                {
                    dutyCycleOn.setText("");
                }
            }
        });

        // Listener for DutyCycle Off  TextField
        dutyCycleOff.textProperty().addListener(new ChangeListener<String>()
        {
            @Override
            public void changed(ObservableValue<? extends String> observable, String oldValue, String newValue) 
            {
                try
                {
                    if ((isDecimal(newValue)))
                    {
                        if(newValue.length() > 5 || Integer.parseInt(newValue) > 65535)
                        {
                            showWarningErrorMessage("error", "Please input dutycycle off timeout less than 65535");
                            dutyCycleOff.setText("0");
                        }
                        else
                        {
                        dutyCycleOff.setText(newValue);
                        }
                    }
                    else
                    {
                        dutyCycleOff.setText("");
                    }
                }
                catch(Exception e)
                {
                    dutyCycleOff.setText("");
                }
            }
        });
        
        embeddedStart.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789xX".contains(ch) || embeddedStart.getText().length() > 9) 
                {
                    keyEvent.consume();
                }
            }
        });

        embeddedEnd.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>() 
        {
            @Override
            public void handle(KeyEvent keyEvent)
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789xX".contains(ch) || embeddedEnd.getText().length() > 3)
                {
                      keyEvent.consume();
                   }
                }
        });
        
        embeddedStart.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>() 
        { 
            public void handle(MouseEvent event)
            {
                String addr = embeddedStart.getText();
                if(addr.length() == 0)
                {
                   embeddedStart.setText("0");
                }
                else if(addr.toLowerCase().startsWith("0x"))
                {
                    if(addr.substring(2).contains("x"))
                    {
                        showWarningErrorMessage("warning", "Invalid value for start address");
                        embeddedStart.setText("0");
                    }
                }
            }
        });

        embeddedEnd.addEventFilter(MouseEvent.MOUSE_EXITED, new EventHandler<MouseEvent>() 
        { 
            public void handle(MouseEvent event)
            {
                String addr = embeddedEnd.getText();
                if(addr.length() == 0)
                {
                   embeddedEnd.setText("0");
                }
                else if(addr.toLowerCase().startsWith("0x"))
                {
                    if(addr.substring(2).contains("x"))
                    {
                        showWarningErrorMessage("warning", "Invalid value for length");
                        embeddedEnd.setText("0");
                    }
                }
            }
        });

        hopTime.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });

        hopTable.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });
    }
                
    public boolean isNumber(String value)
    {
        String reg = "[0-9]+";
        return value.matches(reg);
    }

    public boolean isDecimal(String value) 
    {
        String reg = "[0-9]+(\\.[0-9][0-9]?)?";
        return value.matches(reg);
    }

    public boolean isInteger(String value) 
    {
        try
        {
            Integer.parseInt(value);
            return true;
        }
        catch(Exception e){
            return false;
        }
    }

    public boolean isIntegerArray(String[] values) 
    {
        for(String value : values){
            if(isInteger(value.trim()))
            {
             //Continue
            }else{
                return false;
            }
        }
        return true;
    }

    //Below methods for showing change list for all Configurations

    @FXML
    private void protocolChangeConfiguration(ActionEvent event)
    {
        
        String previousInfo = "Previous selected protocols :"+ changeListMap.get("protocol");
//        getSelectedProtocols();
//        addChangeList(previousInfo +"\nNow selected : "+changeListMap.get("protocol"));
    }

    public void getSelectedProtocols()
    {
        String  protocolsStatusInfo = "";
        if(gen2.isSelected())
        {
            protocolsStatusInfo += " GEN2,";
        }
        if(iso18000.isSelected())
        {
            protocolsStatusInfo += " ISO18000-6B,";
        }
        if(ipx64.isSelected())
        {
            protocolsStatusInfo += " IPX64,";
        }
        if(ipx256.isSelected())
        {
            protocolsStatusInfo += " IPX256,";
        }
       
        if(protocolsStatusInfo.length() == 0)
        {
           protocolsStatusInfo = "NONE,";
        }
        protocolsStatusInfo = protocolsStatusInfo.substring(0, protocolsStatusInfo.length() - 1);
        changeListMap.put("protocol",protocolsStatusInfo);
    }
    
    @FXML
    private void antennaChangeConfiguration(ActionEvent event)
    {
        getAntennaChangeConfiguration();
    }
    
    public void getAntennaChangeConfiguration()
    {
        String prevAntenna = changeListMap.get("antenna");
        String info = "Previous selected antennas :"+ prevAntenna;
//        getSelectedAntennas();
//        if (!changeListMap.get("antenna").equals(prevAntenna))
//        {
//            addChangeList(info + "\nNow selected : " + changeListMap.get("antenna"));
//        }
    }
    
    public void  getSelectedAntennas()
    {
      String  antennas = "";
      if(antenna1.isSelected())
      {
         antennas += " 1,"; 
      }
      if(antenna2.isSelected())
      {
          antennas += " 2,"; 
      }
      if(antenna3.isSelected())
      {
       antennas += " 3,";    
      }
      if(antenna4.isSelected())
      {
         antennas += " 4,";  
      }
      if(antennas.length() == 0)
      {
        antennas = "NONE,";
      }
      antennas = antennas.substring(0, antennas.length()-1);
      changeListMap.put("antenna", antennas);
    }
    
    @FXML
    private void autonomousReadChangeConfiguration(ActionEvent event)
    {
//        if(autonomousRead.isSelected())
//        {
//            addChangeList("Autonomous read was disabled"+"\nNow Enabled");
//        }
//        else
//        {
//            addChangeList("Autonomous read was Enabled"+"\nNow disabled");
//        }
        
    }
    
    @FXML
    private void getEmbeddedStartChangeConfiguration(KeyEvent event)
    {
//        addChangeList("Embedded Start previous value :"+changeListMap.get("embeddedStart")+" Now: "+embeddedStart.getText());
        changeListMap.put("embeddedStart", embeddedStart.getText());
    }
    
    @FXML
    private void getEmbeddedEndChangeConfiguration(KeyEvent event)
    {
//        addChangeList("Embedded End previous value: "+changeListMap.get("embeddedEnd")+" Now: "+embeddedEnd.getText());
        changeListMap.put("embeddedEnd", embeddedEnd.getText());
    }

    @FXML
    private void writeTimestampChangeConfiguration(ActionEvent event)
    {
        if(writeTimestamp.isSelected())
        {
            writeData.setDisable(true);
            writeDataText.setOpacity(0.5);
            showWarningErrorMessage("warning", "Date and time configured in the reader will be set. ");
        }
        else
        {
            writeData.setDisable(false);
            writeDataText.setOpacity(1);
            hideMessagePopup();
        }
    }

    @FXML
    private void validateWriteDataFields(KeyEvent event)
    {
        writeStartAddress.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });

        writeWordCount.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });

        writeData.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789abcdefABCDEF".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });
        
        accessPassword.addEventFilter(KeyEvent.KEY_TYPED, new EventHandler<KeyEvent>()
        {
            @Override
            public void handle(KeyEvent keyEvent) 
            {
                String ch = keyEvent.getCharacter();
                if (!"0123456789abcdefABCDEF".contains(ch)) 
                {
                    keyEvent.consume();
                }
            }
        });
    }
    
    @FXML
    private void gpiTriggerReadChangeConfiguration(ActionEvent event)
    {
        if (gpiTriggerRead.isSelected())
        {
//            addChangeList("GPI Trigger read was disabled \nNow: Enabled " );
        }
        else
        {
//            addChangeList("GPI Trigger read was enabled \nNow: Disabled" );
        }
    }
    
    public void gpiPinChangeConfiguration()
    {
        changeListMap.put("gpiPin", autoReadGpiGroup.getSelectedToggle().getUserData().toString());
    }
  
    public void gen2BlfChangeConfiguration()
    {
//       String prevLinkFreq =   changeListMap.get("linkFreq");
//       String currentLinkFreq = linkFreqGroup.getSelectedToggle().getUserData().toString();
//       changeListMap.put("linkFreq", currentLinkFreq);
//       if(!(prevLinkFreq != null && currentLinkFreq.equals(prevLinkFreq)))
//       {
//           addChangeList("Link frequency was "+prevLinkFreq +"\n Now "+ currentLinkFreq);
//       }
    }
    
    public void gen2TagEncodingChangeConfiguration()
    {
//        String previousInfo = changeListMap.get("tagEncoding");
//        getGen2TagEncodingChangeConfiguration();
//        if(!changeListMap.get("tagEncoding").equals(previousInfo))
//        {
//          addChangeList("Tag encoding was "+previousInfo+"\n Now "+changeListMap.get("tagEncoding"));
//        }
    }

    public void getGen2TagEncodingChangeConfiguration()
    {
        changeListMap.put("tagEncoding", tagEncodeGroup.getSelectedToggle().getUserData().toString());
    }

    public void getGen2SessionChangeConfiguration()
    {
        changeListMap.put("session", sessionGroup.getSelectedToggle().getUserData().toString());
    }

    @FXML
    private void gen2TargetChangeConfiguration()
    {
        String previousInfo = changeListMap.get("target");
//        getGen2TargetChangeConfiguration();
//        addChangeList("Gen2 Target was "+previousInfo+"\n Now "+changeListMap.get("target"));
    }

    public void getGen2TargetChangeConfiguration()
    { 
        changeListMap.put("target", targetGroup.getSelectedToggle().getUserData().toString());
    }

    @FXML
    private void gen2Q(ActionEvent event){
        if(staticQ.isSelected()){
            staticQList.setDisable(false);
            staticQList.setOpacity(enableOpacity);
        }else{
            staticQList.setDisable(true);
            staticQList.setOpacity(disableOpacity);
        }
    }

    @FXML
    private void gen2QElara(ActionEvent event){
        if(isGen2Supported())
        {
            int currentVal = Integer.valueOf(qListElara.getSelectionModel().getSelectedItem().toString());
            if(staticQElara.isSelected()){
                String staticQValues[] = {"0","1","2","3","4","5","6","7","8","9","10","11","12","13","14","15"};
                qListElara.setItems(FXCollections.observableArrayList(staticQValues));
                qListElara.setValue(currentVal);
                initQText.setOpacity(disableElaraOpacity);
            }
            else
            {
                String dynamicQValues[] = {"2","3","4","5","6","7","8","9","10"};
                qListElara.setItems(FXCollections.observableArrayList(dynamicQValues));
                if(currentVal > 10 || currentVal < 2)
                {
                    qListElara.setValue(2);
                }
                else
                {
                    qListElara.setValue(currentVal);
                }
                initQText.setOpacity(enableElaraOpacity);
            }
        }
    }

    @FXML
    private void dispalyChangeConfiguration(ActionEvent event)
    {
        String previousInfo = changeListMap.get("displayOption");
//        getDisplayOptions();
//        addChangeList("Display options was  "+previousInfo+"\nNow "+changeListMap.get("displayOption"));
    }
    
    public void getDisplayOptions()
    {
        String displayOptionInfo = "";
        if(metaDataAntenna.isSelected())
        {
            displayOptionInfo += " Antenna";
        }
        if(metaDataProtocol.isSelected())
        {
            displayOptionInfo += " Protocol";
        }
        if(metaDataFrequency.isSelected())
        {
            displayOptionInfo += " Frequency";
        }
        if(metaDataPhase.isSelected())
        {
            displayOptionInfo += " Phase";
        }
        changeListMap.put("displayOption", displayOptionInfo);
    }
    
//    public void addChangeList(String information)
//    {
//        if(showChangeList)
//        {
//            final Label label = new Label();
//            label.setText(information);
//            label.setWrapText(true);
//            ImageView img = new ImageView(new Image("/images/cancel.png"));
//            Button button = new Button("", img);
//            label.setGraphic(button);
//            changeListContainer.getChildren().add(label);
//            button.setOnAction(new EventHandler<ActionEvent>()
//            {
//                @Override
//                public void handle(ActionEvent event)
//                {
//                    changeListContainer.getChildren().remove(label);
//                }
//            });
//        }
//    }
    
    @FXML
    private void clearChageList(ActionEvent event)
    {
        changeListContainer.getChildren().clear();
    }

    @FXML
    private void enableDisableGen2Settings(ActionEvent event) 
    {
        if (link640Khz.isSelected()) 
        {
            fm0.setSelected(true);
            tari6_25us.setSelected(true);

            m2.setSelected(false);
            m4.setSelected(false);
            m8.setSelected(false);
            tari25us.setSelected(false);
            tari12_5us.setSelected(false);

            m2.setDisable(true);
            m4.setDisable(true);
            m8.setDisable(true);
            tari25us.setDisable(true);
            tari12_5us.setDisable(true);

            m2.setOpacity(disableOpacity);
            m4.setOpacity(disableOpacity);
            m8.setOpacity(disableOpacity);
            tari25us.setOpacity(disableOpacity);
            tari12_5us.setOpacity(disableOpacity);

        }
        else if (link250Khz.isSelected()) 
        {
            if (readerModel.contains("M5e") || readerModel.equalsIgnoreCase("M6e Nano"))
            {
                fm0.setDisable(true);
                tari6_25us.setDisable(true);
                tari12_5us.setDisable(true);
                fm0.setOpacity(disableOpacity);
                tari6_25us.setOpacity(disableOpacity);
                tari12_5us.setOpacity(disableOpacity);
            }
            else
            {
                fm0.setDisable(false);
                tari6_25us.setDisable(false);
                tari12_5us.setDisable(false);
                fm0.setOpacity(enableOpacity);
                tari6_25us.setOpacity(enableOpacity);
                tari12_5us.setOpacity(enableOpacity);
            }

            m2.setDisable(false);
            m4.setDisable(false);
            m8.setDisable(false);
            tari25us.setDisable(false);

            m2.setOpacity(enableOpacity);
            m4.setOpacity(enableOpacity);
            m8.setOpacity(enableOpacity);
            tari25us.setOpacity(enableOpacity);
        }
    }

    @FXML
    private void applyConfigurations(ActionEvent event) 
    {
        hideMessagePopup();
        String message = "Applying selected configurations on module.";
      
        if(region.getSelectionModel().getSelectedItem() == null || "".equals(region.getSelectionModel().getSelectedItem()))
        {
            showWarningErrorMessage("warning", "Please select valid region");
            return;
        }
        
        if(!gen2.isSelected())
        {
           showWarningErrorMessage("warning", "Please select Gen2 protocol");
           return; 
        }
        if (autonomousRead.isSelected() || gpiTriggerRead.isSelected())
        {
            message = "Applying configurations along with autonomous read will navigate to tag results.";

        }
        int option = JOptionPane.showConfirmDialog(null, message, "Confirmation", JOptionPane.YES_NO_OPTION);
        if (option == JOptionPane.YES_OPTION)
        {
            applyConfigurationsToModule();
        }
    }
    
    @FXML
    private void workflowReadWritePower(ActionEvent event) 
    {
        String workflowitem = workflow.getSelectionModel().getSelectedItem().toString();
        if(workflowitem.equalsIgnoreCase("HDR (Single Tag Read Near)"))
        {
            readPowerSliderElara.setValue(20.0);
            writePowerSliderElara.setValue(23.0);
            rfReadElara.setText("20.0");
            rfWriteElara.setText("23.0");

            rfOnTime.setText("1000");
            rfOffTime.setText("0");

            linkFreqGroupElara.selectToggle(link250KhzElara);
            tariGroupElara.selectToggle(tari6_25usElara);
            tagEncodeGroupElara.selectToggle(m2Elara);
            sessionGroupElara.selectToggle(sessionS0Elara);
            targetGroupElara.selectToggle(targetAElara);
            qGroupElara.selectToggle(dynamicQElara);
            qListElara.setValue(2);
            disableDutyCycle(false);
        }
        else if(workflowitem.equalsIgnoreCase("Monitor (Bulk Read Far)"))
        {
            readPowerSliderElara.setValue(20.0);
            writePowerSliderElara.setValue(23.0);
            rfReadElara.setText("20.0");
            rfWriteElara.setText("23.0");

            rfOnTime.setText("250");
            rfOffTime.setText("0");

            linkFreqGroupElara.selectToggle(link250KhzElara);
            tariGroupElara.selectToggle(tari6_25usElara);
            tagEncodeGroupElara.selectToggle(m4Elara);
            sessionGroupElara.selectToggle(sessionS1Elara);
            targetGroupElara.selectToggle(targetAElara);
            qGroupElara.selectToggle(dynamicQElara);
            qListElara.setValue(4);
            disableDutyCycle(false);
        }
        else if(workflowitem.equalsIgnoreCase("Commission Single Tag"))
        {
            readPowerSliderElara.setValue(20.0);
            writePowerSliderElara.setValue(23.0);
            rfReadElara.setText("20.0");
            rfWriteElara.setText("23.0");

            rfOnTime.setText("1000");
            rfOffTime.setText("0");

            linkFreqGroupElara.selectToggle(link250KhzElara);
            tariGroupElara.selectToggle(tari6_25usElara);
            tagEncodeGroupElara.selectToggle(m2Elara);
            sessionGroupElara.selectToggle(sessionS0Elara);
            targetGroupElara.selectToggle(targetAElara);
            qGroupElara.selectToggle(dynamicQElara);
            qListElara.setValue(2);
            disableDutyCycle(true);
        }
        else if(workflowitem.equalsIgnoreCase("Update Tag"))
        {
            readPowerSliderElara.setValue(20.0);
            writePowerSliderElara.setValue(23.0);
            rfReadElara.setText("20.0");
            rfWriteElara.setText("23.0");

            rfOnTime.setText("1000");
            rfOffTime.setText("0");

            linkFreqGroupElara.selectToggle(link250KhzElara);
            tariGroupElara.selectToggle(tari6_25usElara);
            tagEncodeGroupElara.selectToggle(m2Elara);
            sessionGroupElara.selectToggle(sessionS0Elara);
            targetGroupElara.selectToggle(targetAElara);
            qGroupElara.selectToggle(dynamicQElara);
            qListElara.setValue(2);
            disableDutyCycle(true);
        }
    }

    private void showMessageDialog(String message)
    {
        alert.setGraphic(new ImageView("/images/info.png"));
        alert.setTitle("Information Alert");
        alert.setHeaderText(null);
        alert.getDialogPane().setStyle("-fx-border-color: transparent; -fx-border-width: 5; -fx-border-radius: 10;");
        alert.setContentText(message);
        alert.show();
    }

    @FXML
    private void setElaraConfigurations(ActionEvent event) 
    {
        String message = "Please wait while we set configurations.";
        showMessageDialog(message);
        checkThread.interrupt();
        hideMessagePopup();
        cdcText.clear();
        clearInputStream();
        try
        {
            applyConfigurationsToElara();
        }
        catch(NumberFormatException ex)
        {
            alert.hide();
            showWarningErrorMessage("error", "Please enter valid values");
        }
        catch (Exception re)
        {
            alert.hide();
            showWarningErrorMessage("error", re.getMessage());
        }
        checkThread = new Thread(new CheckThread(), "Check Thread");
        checkThread.start();
    }
    @FXML
    private void loadDefaultElaraConfigurations(ActionEvent event) 
    {
        checkThread.interrupt();
        hideMessagePopup();
        cdcText.clear();

        if (isConnected) 
        {
            try
            {
                clearInputStream();
                String message = "Please wait while we load default configurations.";
                showMessageDialog(message);
                // clear the configurations saved in Non-Volatile Memory. which means default configurations are set. 
                String clearCommand = ejsonp.formJSONCommand(ECTConstants.CLEAR_FIELDS);
                String responseReceived = sendMessage(sp, clearCommand, elaraTransportListener);
                // get all the configurations and populate in UI.
                getCfgFieldsAllAndSetToUI(ejsonp);
                alert.hide();
                if(ejsonp.isCommandSuccess(responseReceived))
                {
                    showWarningErrorMessage("success", "Configurations reverted to default settings.");
                }
                else
                {
                    showWarningErrorMessage("error", "Reverting to default settings failed.");
                }
            }
            catch(NumberFormatException ex)
            {
                alert.hide();
                showWarningErrorMessage("error", "Please enter valid values");
            }
            catch (Exception re)
            {
                showWarningErrorMessage("error", re.getMessage());
            }
        }
        checkThread = new Thread(new CheckThread(), "Check Thread");
        checkThread.start();
    }

    @FXML
    private void saveElaraConfigurations(ActionEvent event) 
    {
        checkThread.interrupt();
        hideMessagePopup();
        cdcText.clear();

        if (isConnected) 
        {
            try
            {
                clearInputStream();
                String message = "Please wait while we save configurations.";
                showMessageDialog(message);
                // set all the configurations
                applyConfigurationsToElara();
                // save the configurations to Non-Volatile Memory.
                String saveCommand = ejsonp.formJSONCommand(ECTConstants.SAVE_FIELDS);
                String responseReceived = sendMessage(sp, saveCommand, elaraTransportListener);
                if(ejsonp.isCommandSuccess(responseReceived))
                {
                    showWarningErrorMessage("success", "Configurations saved persistently.");
                }
                else
                {
                    showWarningErrorMessage("error", "Attempt to save configurations failed.");
                }
            }
            catch(NumberFormatException ex)
            {
                alert.hide();
                showWarningErrorMessage("error", "Please enter valid values");
            }
            catch (Exception re)
            {
                alert.hide();
                showWarningErrorMessage("error", re.getMessage());
            }
        }
        checkThread = new Thread(new CheckThread(), "Check Thread");
        checkThread.start();
    }

    @FXML
    private void revertElaraConfigurations(ActionEvent event) 
    {
        checkThread.interrupt();
        hideMessagePopup();
        cdcText.clear();

        if (isConnected) 
        {
            try
            {
                clearInputStream();
                String message = "Please wait while we revert configurations.";
                showMessageDialog(message);
                // Read the configurations from Non-Volatile Memory.
                String readCommand = ejsonp.formJSONCommand(ECTConstants.READ_FIELDS);
                String responseReceived = sendMessage(sp, readCommand, elaraTransportListener);
                // get all the configurations and populate in UI.
                getCfgFieldsAllAndSetToUI(ejsonp);
                alert.hide();
                if(ejsonp.isCommandSuccess(responseReceived))
                {
                    showWarningErrorMessage("success", "Configurations are restored successfully.");
                }
                else
                {
                    showWarningErrorMessage("error", "Attempt to restore configurations failed.");
                }
            }
            catch(NumberFormatException ex)
            {
                alert.hide();
                showWarningErrorMessage("error", "Please enter valid values");
            }
            catch (Exception re)
            {
                showWarningErrorMessage("error", re.getMessage());
            }
        }
        checkThread = new Thread(new CheckThread(), "Check Thread");
        checkThread.start();
    }

    // sends get configuration of all fields and sets the results in UI.
    private void getCfgFieldsAllAndSetToUI(ElaraJSONParser p) throws JSONException
    {
        // get the current configuration fields on the reader to populate in UI
        String regionSet = null;
        String getCfgFieldsAllCmd = p.formJSONCommand(ECTConstants.GET_CFG_FIELDS_ALL);
        String getCfgFieldsAllResp = sendMessage(sp, getCfgFieldsAllCmd, elaraTransportListener);
        if(!isGen2Supported())
        {
            disableGen2Settings();
        }
        JSONObject getCfgJSON = new JSONObject(getCfgFieldsAllResp);
        if(getCfgJSON.get("Report").equals("GetCfg") && getCfgJSON.get("ErrID").equals(0))
        {
            // parse the response
            initSettingsMap = p.parseGetCfgAllResponse(getCfgFieldsAllResp);
            regionElara.getItems().clear();
            if(initSettingsMap.containsKey("_LicenseKey"))
            {
                if(initSettingsMap.get("_LicenseKey").equalsIgnoreCase("RAIN Starter Kit"))
                {
                    //populate workflow drop down for rain starter kit
                    isRSKit = true;
                    String starterKitWorklfow[] = {"Monitor (Bulk Read Far)", "Commission Single Tag"}; 
                    workflow.setItems(FXCollections.observableArrayList(starterKitWorklfow));
                    //Disable get/set date time fields
                    getDateTimeElara.setDisable(true);
                    applyDateTimeButton.setDisable(true);
                    regionElara.setDisable(true);
                    rfReadElara.setDisable(true);
                    readPowerSliderElara.setDisable(true);
                    rfWriteElara.setDisable(true);
                    writePowerSliderElara.setDisable(true);
                    saveButtonElara.setDisable(true);
                    revertButtonElara.setDisable(true);
                    disableGen2Settings();
                }
            }
            if(initSettingsMap.containsKey("FreqReg"))
            {
                regionSet = initSettingsMap.get("FreqReg");
            }
            setPopulateElaraRegion(ejsonp, regionSet);
            if(elaraRS232)
            {
                hidEnabled = false;
                elaraHIDModeEnabled.setSelected(false);
                elaraHIDModeEnabled.setDisable(true);
                outputDataFormat.setOpacity(0.7);
                outputDataFormat.setDisable(true);
            }
            else
            {
                String hidValue = initSettingsMap.get("_USBKBEnable");
                elaraHIDModeEnabled.setDisable(false);
                if(hidValue.equalsIgnoreCase("true"))
                {
                    hidEnabled = true;
                    elaraHIDModeEnabled.setSelected(true);
                    outputDataFormat.setDisable(false);
                    outputDataFormat.setOpacity(1);
                    resultsTabElaraCDC.setDisable(true);
                    resultsTabElaraHID.setDisable(true);
                }
                else
                {
                    hidEnabled = false;
                    elaraHIDModeEnabled.setSelected(false);
                    outputDataFormat.setOpacity(0.7);
                    outputDataFormat.setDisable(true);
                    resultsTabElaraCDC.setDisable(false);
                    resultsTabElaraHID.setDisable(false);
                }
            }
            //Set reader date time
            if(initSettingsMap.containsKey("DateTime"))
            {
                String dateTime = (String) initSettingsMap.get("DateTime");
                String day, month, year;
                String[][] dateTimeArray = ejsonp.formatDateTime(dateTime);
                String[] date = dateTimeArray[0];
                String[] time = dateTimeArray[1];
                if(date[2].length()==1){
                    day = "0"+date[2];
                }
                else
                {
                    day = date[2];
                }
                if(date[1].length()==1){
                    month = "0"+date[1];
                }
                else
                {
                    month = date[1];
                }
                year = date[0];
                String formattedDate = day+"-"+month+"-"+year;
                DateTimeFormatter formatter = DateTimeFormatter.ofPattern("dd-MM-yyyy");
                LocalDate localDate = LocalDate.parse(formattedDate , formatter);
                datepicker.setValue(localDate);
                hoursTextField.setText(time[0]);
                minutesTextField.setText(time[1]);
                secondsTextField.setText(time[2]);
            }
            else
            {   
                datepicker.setDisable(true);
                hoursTextField.setDisable(true);
                minutesTextField.setDisable(true);
                secondsTextField.setDisable(true);
            }
            // update autonomous read is active or not in UI
            if(initSettingsMap.containsKey("RdrStart"))
            {
                String isRdrActive = (String) initSettingsMap.get("RdrStart");
                if(isRdrActive.equalsIgnoreCase("ACTIVE"))
                {
                    autonomousReadElara.setSelected(true);
                }
                else if(isRdrActive.equalsIgnoreCase("NOTACTIVE"))
                {
                    autonomousReadElara.setSelected(false);
                }
            }
            else
            {
                autonomousReadElara.setDisable(true);
            }
            // update UI settings related to current workflow
            if(initSettingsMap.containsKey("Mode"))
            {
                String workflowSet = initSettingsMap.get("Mode");
                if(workflowSet.equalsIgnoreCase("HDR"))
                {
                    workflow.setValue("HDR (Single Tag Read Near)");
                    resultsTabWorkflowName.setText("HDR (Single Tag Read Near)");
                    disableWritePaneElements();
                    disableDutyCycle(false);
                }
                else if(workflowSet.equalsIgnoreCase("MONITOR"))
                {
                    workflow.setValue("Monitor (Bulk Read Far)");
                    resultsTabWorkflowName.setText("Monitor (Bulk Read Far)");
                    disableWritePaneElements();
                    disableDutyCycle(false);
                }
                else if(workflowSet.equalsIgnoreCase("Tag Commission"))
                {
                    workflow.setValue("Commission Single Tag");
                    resultsTabWorkflowName.setText("Commission Single Tag");
                    enableWritePaneElements();
                    writeTimestamp.setVisible(false);
                    writeTimestamp.setSelected(false);
                    validateReadWrite();
                    disableDutyCycle(true);
                }
                else if(workflowSet.equalsIgnoreCase("Tag Update"))
                {
                    workflow.setValue("Update Tag");
                    resultsTabWorkflowName.setText("Update Tag");
                    enableWritePaneElements();
                    writeTimestamp.setVisible(true);
                    writeTimestamp.setSelected(false);
                    validateReadWrite();
                    disableDutyCycle(true);
                }
            }
            if(initSettingsMap.containsKey("ReadPwr"))
            {
                rfReadElara.setText(initSettingsMap.get("ReadPwr"));
                readPowerSliderElara.setValue(Double.parseDouble(initSettingsMap.get("ReadPwr")));
            }
            else
            {
                rfReadElara.setDisable(true);
                readPowerSliderElara.setDisable(true);
            }
            if(initSettingsMap.containsKey("WritePwr"))
            {
                rfWriteElara.setText(initSettingsMap.get("WritePwr"));
                writePowerSliderElara.setValue(Double.parseDouble(initSettingsMap.get("WritePwr")));
            }
            else
            {
                rfWriteElara.setDisable(true);
                writePowerSliderElara.setDisable(true);
            }
            if(isGen2Supported())
            {
                if(initSettingsMap.containsKey("RFOnTime"))
                {
                    rfOnTime.setText(initSettingsMap.get("RFOnTime"));
                }
                else
                {
                    rfOnTime.setDisable(true);
                }
                if(initSettingsMap.containsKey("RFOffTime"))
                {
                    rfOffTime.setText(initSettingsMap.get("RFOffTime"));
                }
                else
                {
                    rfOffTime.setDisable(true);
                }
            }
            if(initSettingsMap.containsKey("BLF"))
            {
                String lf = initSettingsMap.get("BLF");
                if (lf.equals("250"))
                {
                    link250KhzElara.setSelected(true);
                } 
                else if (lf.equals("640")) 
                {
                    link640KhzElara.setSelected(true);
                }
            }
            if(initSettingsMap.containsKey("DataEncoding"))
            {
                String tagEncoding = initSettingsMap.get("DataEncoding");
                if (tagEncoding.equals("FM0"))
                {
                    fm0Elara.setSelected(true);
                } 
                else if (tagEncoding.equals("M2")) 
                {
                    m2Elara.setSelected(true);
                }
                else if (tagEncoding.equals("M4")) 
                {
                    m4Elara.setSelected(true);
                }
                else if (tagEncoding.equals("M8")) 
                {
                    m8Elara.setSelected(true);
                }
            }
            if(initSettingsMap.containsKey("Session"))
            {
                int session = Integer.parseInt(initSettingsMap.get("Session"));
                if (session == 0)
                {
                    sessionS0Elara.setSelected(true);
                } 
                else if (session == 1) 
                {
                    sessionS1Elara.setSelected(true);
                }
                else if (session == 2) 
                {
                    sessionS2Elara.setSelected(true);
                }
                else if (session == 3) 
                {
                    sessionS3Elara.setSelected(true);
                }
            }
            if(initSettingsMap.containsKey("Target"))
            {
                String target = initSettingsMap.get("Target");
                if (target.equals("A"))
                {
                    targetAElara.setSelected(true);
                } 
                else if (target.equals("B")) 
                {
                    targetBElara.setSelected(true);
                }
                else if (target.equals("AB")) 
                {
                    targetABElara.setSelected(true);
                }
                else if (target.equals("BA")) 
                {
                    targetBAElara.setSelected(true);
                }
            }
            if(isGen2Supported() && !isRSKit)
            {
                String getQCmd = p.formJSONCommand(ECTConstants.GET_RZ_Q);
                String getQResponse = sendMessage(sp, getQCmd, elaraTransportListener);
                JSONObject getQJSON = new JSONObject(getQResponse);
                if(getQJSON.get("Report").equals("GetRZ") && getQJSON.get("ErrID").equals(0))
                {
                    if(getQJSON.get("_QMODE").toString().equalsIgnoreCase("Dynamic"))
                    {
                        dynamicQElara.setSelected(true);
                    }
                    else
                    {
                        staticQElara.setSelected(true);
                    }
                    gen2QElara(new ActionEvent());
                    qListElara.setValue(getQJSON.get("Q"));
                }
                else
                {
                    gen2QElara(new ActionEvent());
                    dynamicQElara.setSelected(true);
                    qListElara.setValue(2);
                }
            }
            tariGroupElara.selectToggle(tari6_25usElara);
            
            // User Interface settings
            if(initSettingsMap.containsKey("_AudioVolume"))
            {
                String audioVolume = initSettingsMap.get("_AudioVolume");
                if(audioVolume.equals("MUTE"))
                {
                    buzzerTone.setValue(buzzerTone.getItems().get(0));
                }
                else if(audioVolume.equals("LOW"))
                {
                    buzzerTone.setValue(buzzerTone.getItems().get(1));
                }
                else if(audioVolume.equals("MED"))
                {
                    buzzerTone.setValue(buzzerTone.getItems().get(2));
                }
                else if(audioVolume.equals("HIGH"))
                {
                    buzzerTone.setValue(buzzerTone.getItems().get(3));
                }
            }
            else
            {
                buzzerTone.setDisable(true);
            }
            //Output Data Format settings
            if(initSettingsMap.containsKey("_KBDataFormat"))
            {
                if(initSettingsMap.get("_KBDataFormat").equalsIgnoreCase("EPC"))
                {
                    outputDataFormat.setValue(outputDataFormat.getItems().get(0));
                }
                else if(initSettingsMap.get("_KBDataFormat").equalsIgnoreCase("Metadata"))
                {
                    outputDataFormat.setValue(outputDataFormat.getItems().get(1));
                }
                else if(initSettingsMap.get("_KBDataFormat").equalsIgnoreCase("RAIN"))
                {
                    outputDataFormat.setValue(outputDataFormat.getItems().get(2));
                }
            }
            else
            {
                outputDataFormat.setDisable(true);
            }
            //Metadata settings
            if(initSettingsMap.containsKey("SpotInvCnt"))
            {
                if(initSettingsMap.get("SpotInvCnt").equalsIgnoreCase("true"))
                {
                    elaraMetadataInventoryCount.setSelected(true);
                }
                else
                {
                    elaraMetadataInventoryCount.setSelected(false);
                }
            }
            else
            {
                elaraMetadataInventoryCount.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotRSSI"))
            {
                if(initSettingsMap.get("SpotRSSI").equalsIgnoreCase("true"))
                {
                    elaraMetadataRSSI.setSelected(true);
                }
                else
                {
                    elaraMetadataRSSI.setSelected(false);
                }
            }
            else
            {
                elaraMetadataRSSI.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotAnt"))
            {
                if(initSettingsMap.get("SpotAnt").equalsIgnoreCase("true"))
                {
                    elaraMetadataAntenna.setSelected(true);
                }
                else
                {
                    elaraMetadataAntenna.setSelected(false);
                }
            }
            else
            {
                elaraMetadataAntenna.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotDT"))
            {
                if(initSettingsMap.get("SpotDT").equalsIgnoreCase("true"))
                {
                    elaraMetadataDateTime.setSelected(true);
                }
                else
                {
                    elaraMetadataDateTime.setSelected(false);
                }
            }
            else
            {
                elaraMetadataDateTime.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotPhase"))
            {
                if(initSettingsMap.get("SpotPhase").equalsIgnoreCase("true"))
                {
                    elaraMetadataPhase.setSelected(true);
                }
                else
                {
                    elaraMetadataPhase.setSelected(false);
                }
            }
            else
            {
                elaraMetadataPhase.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotProf"))
            {
                if(initSettingsMap.get("SpotProf").equalsIgnoreCase("true"))
                {
                    elaraMetadataProfile.setSelected(true);
                }
                else
                {
                    elaraMetadataProfile.setSelected(false);
                }
            }
            else
            {
                elaraMetadataProfile.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotRz"))
            {
                if(initSettingsMap.get("SpotRz").equalsIgnoreCase("true"))
                {
                    elaraMetadataRZ.setSelected(true);
                }
                else
                {
                    elaraMetadataRZ.setSelected(false);
                }
            }
            else
            {
                elaraMetadataRZ.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotFreq"))
            {
                if(initSettingsMap.get("SpotFreq").equalsIgnoreCase("true"))
                {
                    elaraMetadataFreq.setSelected(true);
                }
                else
                {
                    elaraMetadataFreq.setSelected(false);
                }
            }
            else
            {
                elaraMetadataFreq.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotGen2_Q"))
            {
                if(initSettingsMap.get("SpotGen2_Q").equalsIgnoreCase("true"))
                {
                    elaraMetadataGen2Q.setSelected(true);
                }
                else
                {
                    elaraMetadataGen2Q.setSelected(false);
                }
            }
            else
            {
                elaraMetadataGen2Q.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotGen2_LF"))
            {
                if(initSettingsMap.get("SpotGen2_LF").equalsIgnoreCase("true"))
                {
                    elaraMetadataGen2LF.setSelected(true);
                }
                else
                {
                    elaraMetadataGen2LF.setSelected(false);
                }
            }
            else
            {
                elaraMetadataGen2LF.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotGen2_Target"))
            {
                if(initSettingsMap.get("SpotGen2_Target").equalsIgnoreCase("true"))
                {
                    elaraMetadataGen2Target.setSelected(true);
                }
                else
                {
                    elaraMetadataGen2Target.setSelected(false);
                }
            }
            else
            {
                elaraMetadataGen2Target.setDisable(true);
            }
            if(initSettingsMap.containsKey("SpotProt"))
            {
                if(initSettingsMap.get("SpotProt").equalsIgnoreCase("true"))
                {
                    elaraMetadataProtocol.setSelected(true);
                }
                else
                {
                    elaraMetadataProtocol.setSelected(false);
                }
            }
            else
            {
                elaraMetadataProtocol.setDisable(true);
            }
//            if(initSettingsMap.get("SpotSensor").equalsIgnoreCase("true"))
//            {
//                elaraMetadataSensor.setSelected(true);
//            }
//            else
//            {
//                elaraMetadataSensor.setSelected(false);
//            }
            if(initSettingsMap.containsKey("SpotGPIO"))
            {
                if(initSettingsMap.get("SpotGPIO").equalsIgnoreCase("true"))
                {
                    elaraMetadataGPIO.setSelected(true);
                }
                else
                {
                    elaraMetadataGPIO.setSelected(false);
                }
            }
            else
            {
                elaraMetadataGPIO.setDisable(true);
            }
            if(elaraRS232)
            {
                String baudRate = initSettingsMap.get("SerCfg");
                if(baudRate.length() != 0 && !baudRate.isEmpty())
                {
                    probeElaraBaudRate.getSelectionModel().select(baudRate);
                }
                else
                {
                    probeElaraBaudRate.getSelectionModel().select("115200");
                }
            }
            if(isGen2Supported())
            {
                if(initSettingsMap.containsKey("_ReReportTag") && 
                        workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("Monitor (Bulk Read Far)"))
                {
                    if(initSettingsMap.get("_ReReportTag").equalsIgnoreCase("true"))
                    {
                        reReport.setSelected(true);
                    }
                    else
                    {
                        reReport.setSelected(false);
                    }
                    if(initSettingsMap.get("_NewTagDelay").equalsIgnoreCase("0") && initSettingsMap.get("_SameTagDelay").equalsIgnoreCase("0"))
                    {
                        reReportEnable.setSelected(false);
                    }
                    else
                    {
                        reReportEnable.setSelected(true);
                    }
                    enableTagReReport();
                    newTagDelay.setText(initSettingsMap.get("_NewTagDelay"));
                    sameTagDelay.setText(initSettingsMap.get("_SameTagDelay"));
                }
                else
                {
                    reReport.setSelected(false);
                    reReport.setDisable(true);
                    newTagDelay.setDisable(true);
                    sameTagDelay.setDisable(true);
                    reReportEnable.setDisable(true);
                    newTagDelayText.setOpacity(disableElaraOpacity);
                    sameTagDelayText.setOpacity(disableElaraOpacity);
                }
            }
        }
        else
        {
            if(getCfgJSON.toString().contains("ErrInfo"))
            {
                String errorMsg = (String) getCfgJSON.get("ErrInfo");
                showWarningErrorMessage("error", errorMsg);
            }
            else
            {
                showWarningErrorMessage("error", "Failed to get configuration from reader.");
            }
            
        }
        saveConfigValues();
    }

    @FXML
    private void applyWorkflowConfigurations(ActionEvent event)
    {
        try
        {
            if(isConnected)
            {
                hideMessagePopup();
                ElaraJSONParser jsonParser = new ElaraJSONParser();
                String workFlowCommand;
                String responseReceived;
                String elaraHeartbeatCommand;
                String currentWorkflow = resultsTabWorkflowName.getText().toString();
                String readDataStatus = "";
                String writeDataStatus = "";
                boolean readWriteStatus = false;
                //Execute the start/stop button functionality based on workflow set.
                if(currentWorkflow.equalsIgnoreCase("HDR (Single Tag Read Near)") || currentWorkflow.equalsIgnoreCase("Monitor (Bulk Read Far)")
                    || currentWorkflow.equalsIgnoreCase("Update Tag"))
                {
                    checkThread.interrupt();
                    Thread.sleep(500);
                    if((currentWorkflow.equalsIgnoreCase("Update Tag")) && (toggleButtonStart.getText().equals("Start")))
                    {
                        if(readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("READ"))
                        {
                            String readDataResponse = setReadDataProfile(jsonParser);
                            if(isValidJSON(readDataResponse))
                            {
                                JSONObject readDataJSON = new JSONObject(readDataResponse);
                                if(readDataJSON.toString().contains("ErrInfo"))
                                {
                                    readDataStatus = (String) readDataJSON.get("ErrInfo");
                                    showWarningErrorMessage("error", readDataStatus);
                                }
                                else if(readDataJSON.toString().contains("ErrID"))
                                {
                                    if(readDataJSON.get("ErrID").equals(0))
                                    {
                                        readDataStatus = "Success";
                                    }
                                }
                            }
                            else
                            {
                                showWarningErrorMessage("error", "Failed to read data");
                            }
                        }
                        else
                        {
                            String writeDataResponse = setWriteDataProfile(jsonParser);
                            if(isValidJSON(writeDataResponse))
                            {
                                JSONObject writeDataJSON = new JSONObject(writeDataResponse);
                                if(writeDataJSON.toString().contains("ErrInfo"))
                                {
                                    writeDataStatus = (String) writeDataJSON.get("ErrInfo");
                                    showWarningErrorMessage("error", writeDataStatus);
                                }
                                else if(writeDataJSON.toString().contains("ErrID"))
                                {
                                    if(writeDataJSON.get("ErrID").equals(0))
                                    {
                                        writeDataStatus = "Success";
                                    }
                                }
                            }
                            else
                            {
                                showWarningErrorMessage("error", "Failed to write data");
                            }
                            if(!(accessPassword.getText() == null || accessPassword.getText().equalsIgnoreCase("") || accessPassword.getText().length()==0))
                            {
                                StringBuffer sb = new StringBuffer();
                                sb.append(":");
                                sb.append(accessPassword.getText());
                                String setPwdCmd = jsonParser.customJSONCommand(ECTConstants.SET_RZ_PASSWORD, sb);
                                String pwdResponse = sendMessage(sp, setPwdCmd, elaraTransportListener);
                            }
                        }
                    }
                    // change button text to "Stop" and colour to red if "Start" is clicked.
                    if(currentWorkflow.equalsIgnoreCase("Update Tag"))
                    {
                        if(readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("READ"))
                        {
                            if(readDataStatus.equalsIgnoreCase("Success"))
                            {
                                readWriteStatus = true;
                            }
                        }
                        else if(readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("WRITE"))
                        {
                            if(writeDataStatus.equalsIgnoreCase("Success"))
                            {
                                readWriteStatus = true;
                            }  
                        }
                    }
                    else
                    {
                        readWriteStatus = true;
                    }
                    if(toggleButtonStart.getText().equals("Start") && readWriteStatus)
                    {
                        if(hidEnabled)
                        {
                            showWarningErrorMessage("warning", "Device configured to HID interface.Connect in HID mode from Connect Tab.");
                        }
                        else
                        {
                            Platform.runLater(new Runnable()
                            {
                                @Override
                                public void run()
                                {  
                                    toggleButtonStart.setText("Stop");
                                    toggleButtonStart.setStyle("-fx-background-color: #D80000; ");
                                    configureTabElara.setDisable(true);
                                    utilsTabElara.setDisable(true);
                                    // While read is in progress, do not allow user to check/uncheck writeTimestamp. Hence disabling it.
                                    if(currentWorkflow.equalsIgnoreCase("Update Tag"))
                                    {
                                        writeTimestamp.setDisable(true);
                                    }
                                }
                            });
                            workFlowCommand = jsonParser.formJSONCommand(ECTConstants.START_RZ);
                            responseReceived = sendMessage(sp, workFlowCommand, elaraTransportListener);
                            elaraHeartbeatCommand = jsonParser.formJSONCommand(ECTConstants.SET_CFG_HEARTBEAT);
                            sendMessage(sp, elaraHeartbeatCommand, elaraTransportListener);
                            isElaraReading = true;
                            setReaderStatus(1);
                            new Thread(dataPostThread).start();
                        }
                    }
                    // change button text to "Start" and colour to green if "Stop" is clicked.
                    else if(toggleButtonStart.getText().equals("Stop"))
                    {
                        Platform.runLater(new Runnable()
                        {
                            @Override
                            public void run()
                            {  
                                toggleButtonStart.setText("Start");
                                toggleButtonStart.setStyle("-fx-background-color: #28B86D; ");
                                configureTabElara.setDisable(false);
                                utilsTabElara.setDisable(false);
                                // When stopped, enable the writeTimestamp control back.
                                if(currentWorkflow.equalsIgnoreCase("Update Tag"))
                                {
                                    validateReadWrite();
                                }
                            }
                        });
                        String message = "Please wait...";
                        showMessageDialog(message);
                        workFlowCommand = jsonParser.formJSONCommand(ECTConstants.STOP_RZ);
                        responseReceived = sendMessage(sp, workFlowCommand, elaraTransportListener);
                        isElaraReading = false;
                        elaraHeartbeatCommand = jsonParser.formJSONCommand(ECTConstants.SET_CFG_STOP_HEARTBEAT);
                        sendMessage(sp, elaraHeartbeatCommand, elaraTransportListener);
                        setReaderStatus(0);
                        alert.hide();
                        checkThread = new Thread(new CheckThread(), "Check Thread");
                        checkThread.start();
                    }
                }
                else if(currentWorkflow.equalsIgnoreCase("Commission Single Tag"))
                {
                    checkThread.interrupt();
                    Thread.sleep(500);
                    try
                    {
                        if(readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("READ"))
                        {
                            String message = "Reading data...";
                            String readDataSuccess = "";
                            showMessageDialog(message);
                            //Send read command
                            String readDataResponse = setReadDataProfile(jsonParser);
                            if(isValidJSON(readDataResponse))
                            {
                                JSONObject readDataJSON = new JSONObject(readDataResponse);
                                if(readDataJSON.toString().contains("ErrInfo"))
                                {
                                    readDataSuccess = (String) readDataJSON.get("ErrInfo");
                                    showWarningErrorMessage("error", readDataSuccess);
                                }
                                else if(readDataJSON.toString().contains("ErrID"))
                                {
                                    if(readDataJSON.get("ErrID").equals(0))
                                    {
                                        readDataSuccess = "Success";
                                    }
                                }
                            }
                            else
                            {
                                showWarningErrorMessage("error", "Failed to read data");
                            }
                            //Send this tag command
                            if(readDataSuccess.equalsIgnoreCase("Success"))
                            {
                                String thisTagCmd = jsonParser.formJSONCommand(ECTConstants.THIS_TAG_PROF);
                                responseReceived = sendMessage(sp, thisTagCmd, elaraTransportListener);
                                if(isValidJSON(responseReceived))
                                {
                                    JSONObject thisTagJSON = new JSONObject(responseReceived);
                                    if(thisTagJSON.toString().contains("ErrInfo"))
                                    {
                                        String errorMessage = (String) thisTagJSON.get("ErrInfo");
                                        showWarningErrorMessage("error", errorMessage);
                                    }
                                    else
                                    {
                                        String[] splitResponse = responseReceived.split("\\r?\\n");
                                        for(int i=0; i < splitResponse.length; i++)
                                        {
                                            if(splitResponse[i].contains("TagEvent"))
                                            {
                                                cdcText.clear();
                                                cdcText.appendText("\n");
                                                cdcText.appendText(splitResponse[i]);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    showWarningErrorMessage("error", "Failed to read data");
                                }
                            }
                            alert.hide();
                        }
                        else
                        {
                            String message = "Writing data...";
                            showMessageDialog(message);
                            String pwdSuccess="";
                            String pwdResponse="";
                            String writeDataSuccess="";
                            String writeDataResponse = setWriteDataProfile(jsonParser);
                            if(toggleButtonStart.getText().equals("Start"))
                            {
                                /** Disable start button and do not allow user to configure any setting during this process. 
                                 *  Hence disable utils tab and configure tab.*/
                                Platform.runLater(new Runnable()
                                {
                                    @Override
                                    public void run()
                                    {  
                                        toggleButtonStart.setDisable(true);
                                        configureTabElara.setDisable(true);
                                        utilsTabElara.setDisable(true);
                                    }
                                });
                            }
                            if(isValidJSON(writeDataResponse))
                            {
                                JSONObject writeDataJSON = new JSONObject(writeDataResponse);
                                if(writeDataJSON.toString().contains("ErrInfo"))
                                {
                                    writeDataSuccess = (String)writeDataJSON.get("ErrInfo");
                                    showWarningErrorMessage("error", writeDataSuccess);
                                }
                                else
                                {
                                    if(writeDataJSON.get("ErrID").equals(0))
                                    {
                                        writeDataSuccess="Success";
                                    }
                                }
                            }
                            else
                            {
                                showWarningErrorMessage("error", "Failed to write data");
                            }
                            if(writeDataSuccess.equalsIgnoreCase("Success"))
                            {
                                if(!(accessPassword.getText() == null || accessPassword.getText().equalsIgnoreCase("") || accessPassword.getText().length()==0))
                                {
                                    StringBuffer sb = new StringBuffer();
                                    sb.append(":");
                                    sb.append(accessPassword.getText());
                                    String setPwdCmd = jsonParser.customJSONCommand(ECTConstants.SET_RZ_PASSWORD, sb);
                                    pwdResponse = sendMessage(sp, setPwdCmd, elaraTransportListener);
                                }
                                else
                                {
                                    pwdSuccess="Success";
                                }
                            }
                            if(isValidJSON(pwdResponse))
                            {
                                JSONObject pwdJSON = new JSONObject(pwdResponse);
                                if(pwdJSON.toString().contains("ErrInfo"))
                                {
                                    pwdSuccess = (String)pwdJSON.get("ErrInfo");
                                    showWarningErrorMessage("error", pwdSuccess);
                                }
                                
                                else
                                {
                                    if(pwdJSON.get("ErrID").equals(0))
                                    {
                                        pwdSuccess="Success";
                                    }
                                }
                            }
                            if(pwdSuccess.equalsIgnoreCase("Success"))
                            {
                                String thisTagCmd = jsonParser.formJSONCommand(ECTConstants.THIS_TAG_PROF);
                                responseReceived = sendMessage(sp, thisTagCmd, elaraTransportListener);
                                if(isValidJSON(responseReceived))
                                {
                                    JSONObject thisTagJSON = new JSONObject(responseReceived);
                                    if(thisTagJSON.toString().contains("ErrInfo"))
                                    {
                                        String errorMessage = (String) thisTagJSON.get("ErrInfo");
                                        showWarningErrorMessage("error", errorMessage);
                                    }
                                    else
                                    {
                                        showWarningErrorMessage("success", "Write data is successful");
                                    }
                                }
                                else
                                {
                                    showWarningErrorMessage("error", "Failed to write data");
                                }
                            }
                        }
                        alert.hide();
                        checkThread = new Thread(new CheckThread(), "Check Thread");
                        checkThread.start();
                    }
                    catch(Exception ex)
                    {
                        ex.printStackTrace();
                    }
                    finally
                    {
                        Platform.runLater(new Runnable()
                        {
                            @Override
                            public void run()
                            {  
                                toggleButtonStart.setDisable(false);
                                configureTabElara.setDisable(false);
                                utilsTabElara.setDisable(false);
                            }
                        });
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.printStackTrace();
        }
    }
    @FXML
    public void enableOutputDataFormat()
    {
        if(!isRSKit)
        {
            if(elaraHIDModeEnabled.isSelected())
            {
                outputDataFormat.setDisable(false);
                outputDataFormat.setOpacity(1.0);
            }
            else
            {
                outputDataFormat.setDisable(true);
                outputDataFormat.setOpacity(0.7);
            }
        }
    }
    @FXML
    public void enableTagReReport()
    {
        if(isGen2Supported())
        {
            if(reReportEnable.isSelected())
            {
                newTagDelay.setDisable(false);
                sameTagDelay.setDisable(false);
                newTagDelayText.setOpacity(enableElaraOpacity);
                sameTagDelayText.setOpacity(enableElaraOpacity);
                reReport.setDisable(false);
            }
            else
            {
                newTagDelay.setDisable(true);
                sameTagDelay.setDisable(true);
                newTagDelayText.setOpacity(disableElaraOpacity);
                sameTagDelayText.setOpacity(disableElaraOpacity);
                reReport.setDisable(true);
                newTagDelay.setText("0");
                sameTagDelay.setText("0");
            }
        }
    }
    /**
     * Gets all the write data fields from UI and sends the framed JSON command to reader.
     * Receives the response for the command.
     *
     * @param jsonParser - the json parser to be used.
     * @return the response to set profile write data command.
     */
    private String setWriteDataProfile(ElaraJSONParser jsonParser) throws Exception
    {
        String responseReceived = null;
        try
        {
            boolean lckTag;
            boolean permaLock;
            String workflowSelected = resultsTabWorkflowName.getText().toString();
            String memBankToWrite = writeMemoryBank.getValue().toString();
            int startAddress = Integer.parseInt(writeStartAddress.getText());
            int wordCount = Integer.parseInt(writeWordCount.getText());
            int retryLimit = 0;
            String dataToWrite = writeData.getText().toString();
            boolean check = false;
            if(lockTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("SECURE"))
            {
                lckTag = true;
            }
            else
            {
                lckTag = false;
            }
            if(lockTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("PERMALOCK"))
            {
                permaLock = true;
            }
            else
            {
                permaLock = false;
            }
            boolean isDTselected = writeTimestamp.isSelected();
            String writeProfileSendCmd = jsonParser.setProfileWriteData(memBankToWrite, startAddress, wordCount, retryLimit, dataToWrite, check, isDTselected, lckTag, permaLock, workflowSelected);
            responseReceived = sendMessage(sp, writeProfileSendCmd, elaraTransportListener);
            if(responseReceived!=null)
            {
                if(!isValidJSON(responseReceived))
                {
                    if(responseReceived.contains("ErrInfo"))
                    {
                        String errorInfo = jsonParser.errorInfo(responseReceived);
                        showWarningErrorMessage("error", errorInfo);
                        throw new Exception(errorInfo);
                    }
                    else
                    {
                        showWarningErrorMessage("error", "Failed to write data");
                    }
                }
            }
            else
            {
                showWarningErrorMessage("error", "Failed to write data");
            }
        }
        catch(NumberFormatException ex)
        {
            String errorMessage = "Please enter valid values for write Data fields";
            showWarningErrorMessage("error", errorMessage);
            throw new NumberFormatException(errorMessage);
        }
        catch(JSONException e)
        {
            showWarningErrorMessage("error", e.getMessage());
            throw new JSONException(e.getMessage());
        }
        return responseReceived;
    }
    
    private String setReadDataProfile(ElaraJSONParser jsonParser) throws Exception
    {
        String responseReceived = null;
        try
        {
            String workflowSelected = resultsTabWorkflowName.getText().toString();
            String memBankToRead = writeMemoryBank.getValue().toString();
            int startAddress = Integer.parseInt(writeStartAddress.getText());
            int wordCount = Integer.parseInt(writeWordCount.getText());
            int retryLimit = 0;
            boolean check = false;
            String readProfileSendCmd = jsonParser.setProfileReadData(memBankToRead, startAddress, wordCount, retryLimit);
            responseReceived = sendMessage(sp, readProfileSendCmd, elaraTransportListener);
        }
        catch(NumberFormatException ex)
        {
            String errorMessage = "Please enter valid values for write Data fields";
            showWarningErrorMessage("error", errorMessage);
            throw new NumberFormatException(errorMessage);
        }
        return responseReceived;
    }

    @FXML
    private void setDateTime(ActionEvent event) throws IOException{

        checkThread.interrupt();
        String setDTMessage = "Please wait while we set PC date/time to reader.";
        showMessageDialog(setDTMessage);
        clearInputStream();
        try {
            String date = new SimpleDateFormat("dd-MM-yyyy").format(Calendar.getInstance().getTime());
            String time = ejsonp.getCurrentTime();
            String[] splitDate = date.split("-");
            String[] splitTime = time.split(":");
            String day = splitDate[0];
            String month = splitDate[1];
            String year = splitDate[2];
            String hours = splitTime[0];
            String minutes = splitTime[1];
            String seconds = splitTime[2];
            StringBuffer sb = new StringBuffer();
            sb.append(year);
            sb.append("-");
            sb.append(month);
            sb.append("-");
            sb.append(day);
            sb.append("T");
            sb.append(hours);
            sb.append(":");
            sb.append(minutes);
            sb.append(":");
            sb.append(seconds);
            sb.append("Z");
            
            String message = ejsonp.customJSONCommand(ECTConstants.SET_CFG_FIELDS_DATETIME, sb);
            String responseReceived = sendMessage(sp, message, elaraTransportListener);
            if(ejsonp.isCommandSuccess(responseReceived))
            {
                 boolean settingsSuccessful = getReaderDateTime();
                 if(settingsSuccessful)
                 {
                    alert.hide();
                    showWarningErrorMessage("success", "Set Date/Time successful");
                 }
                 else
                 {
                    alert.hide();
                    showWarningErrorMessage("error", "Set Date/Time fail");
                 }
            }
            else
            {
                alert.hide();
                showWarningErrorMessage("error", "Set Date/Time fail");
            }
        } catch (JSONException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        } catch (ParseException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
    
    @FXML
    private boolean getReaderDateTime() throws ParseException,IOException,JSONException{
        
        checkThread.interrupt();
        clearInputStream();
        String getDTMessage = "Please wait while we get reader date/time.";
        showMessageDialog(getDTMessage);
        String day, month, year;
        Boolean dateTimeSet = false;
        String dateTimeResponse =sendMessage(sp, ejsonp.formJSONCommand(ECTConstants.GET_CFG_FIELDS_DATETIME), elaraTransportListener);
        HashMap<String,String> dateTimeHMap = ejsonp.parseDateTime(dateTimeResponse);
        if(dateTimeHMap.get("ErrID").equalsIgnoreCase("0"))
        {
            String[][] dateTimeArray = ejsonp.formatDateTime(dateTimeHMap.get("DateTime"));
            String[] date = dateTimeArray[0];
            String[] time = dateTimeArray[1];
            if(date[2].length()==1){
                day = "0"+date[2];
            }
            else
            {
                day = date[2];
            }
            if(date[1].length()==1){
                month = "0"+date[1];
            }
            else
            {
                month = date[1];
            }
            year = date[0];
            String formattedDate = day+"-"+month+"-"+year;
            DateTimeFormatter formatter = DateTimeFormatter.ofPattern("dd-MM-yyyy");
            LocalDate localDate = LocalDate.parse(formattedDate , formatter);
            datepicker.setValue(localDate);
            hoursTextField.setText(time[0]);
            minutesTextField.setText(time[1]);
            secondsTextField.setText(time[2]);
            dateTimeSet = true;
            alert.hide();
            showWarningErrorMessage("success", "Get Date/Time successful");
        }
        else
        {
            alert.hide();
            showWarningErrorMessage("error", "Get Date/Time fail");
        }
        checkThread = new Thread(new CheckThread(), "Check Thread");
        checkThread.start();
        return dateTimeSet;
    }

    private void getCfgFieldsModeAndSetToUI(ElaraJSONParser p) throws JSONException
    {
        String getCfgWorkflowCmd = p.formJSONCommand(ECTConstants.GET_CFG_FIELDS_MODE);
        String getCfgWorkflowResponse = sendMessage(sp, getCfgWorkflowCmd, elaraTransportListener);
        if(ejsonp.isCommandSuccess(getCfgWorkflowResponse))
        {
            HashMap<String,String> settingsMap = ejsonp.parseGetCfgWorkflowResponse(getCfgWorkflowResponse);

            // update UI settings related to current workflow
            String workflowSet = settingsMap.get("Mode");
            if(workflowSet.equalsIgnoreCase("HDR"))
            {
                workflow.setValue("HDR (Single Tag Read Near)");
                resultsTabWorkflowName.setText("HDR (Single Tag Read Near)");
                disableWritePaneElements();
                reReportEnable.setSelected(false);
                reReportEnable.setDisable(true);
                enableTagReReport();
            }
            else if(workflowSet.equalsIgnoreCase("MONITOR"))
            {
                workflow.setValue("Monitor (Bulk Read Far)");
                resultsTabWorkflowName.setText("Monitor (Bulk Read Far)");
                disableWritePaneElements();
                reReportEnable.setSelected(true);
                reReportEnable.setDisable(false);
                enableTagReReport();
            }
            else if(workflowSet.equalsIgnoreCase("Tag Commission"))
            {
                workflow.setValue("Commission Single Tag");
                resultsTabWorkflowName.setText("Commission Single Tag");
                enableWritePaneElements();
                writeTimestamp.setSelected(false);
                writeTimestamp.setVisible(false);
                validateReadWrite();
                reReportEnable.setSelected(false);
                reReportEnable.setDisable(true);
                enableTagReReport();
            }
            else if(workflowSet.equalsIgnoreCase("Tag Update"))
            {
                workflow.setValue("Update Tag");
                resultsTabWorkflowName.setText("Update Tag");
                enableWritePaneElements();
                writeTimestamp.setVisible(true);
                writeTimestamp.setSelected(false);
                validateReadWrite();
                reReportEnable.setSelected(false);
                reReportEnable.setDisable(true);
                enableTagReReport();
            }
            rfReadElara.setText(settingsMap.get("ReadPwr"));
            readPowerSliderElara.setValue(Double.parseDouble(settingsMap.get("ReadPwr")));
            rfWriteElara.setText(settingsMap.get("WritePwr"));
            writePowerSliderElara.setValue(Double.parseDouble(settingsMap.get("WritePwr")));
            rfOnTime.setText(settingsMap.get("RFOnTime"));
            rfOffTime.setText(settingsMap.get("RFOffTime"));

            String lf = settingsMap.get("BLF");
            if (lf.equals("250"))
            {
                link250KhzElara.setSelected(true);
            } 
            else if (lf.equals("640")) 
            {
                link640KhzElara.setSelected(true);
            }

            String tagEncoding = settingsMap.get("DataEncoding");
            if (tagEncoding.equals("FM0"))
            {
                fm0Elara.setSelected(true);
            } 
            else if (tagEncoding.equals("M2")) 
            {
                m2Elara.setSelected(true);
            }
            else if (tagEncoding.equals("M4")) 
            {
                m4Elara.setSelected(true);
            }
            else if (tagEncoding.equals("M8")) 
            {
                m8Elara.setSelected(true);
            }

            int session = Integer.parseInt(settingsMap.get("Session"));
            if (session == 0)
            {
                sessionS0Elara.setSelected(true);
            } 
            else if (session == 1) 
            {
                sessionS1Elara.setSelected(true);
            }
            else if (session == 2) 
            {
                sessionS2Elara.setSelected(true);
            }
            else if (session == 3) 
            {
                sessionS3Elara.setSelected(true);
            }

            String target = settingsMap.get("Target");
            if (target.equals("A"))
            {
                targetAElara.setSelected(true);
            } 
            else if (target.equals("B")) 
            {
                targetBElara.setSelected(true);
            }
            else if (target.equals("AB")) 
            {
                targetABElara.setSelected(true);
            }
            else if (target.equals("BA")) 
            {
                targetBAElara.setSelected(true);
            }

            String gen2Q = settingsMap.get("Gen2Q");
            if (gen2Q.equals("Dynamic"))
            {
                dynamicQElara.setSelected(true);
            } 
            else if (gen2Q.equals("Static")) 
            {
                staticQElara.setSelected(true);
                qListElara.setValue("");
            }
            int gen2QInitVal = Integer.parseInt(settingsMap.get("Gen2QInitVal"));
            qListElara.setValue(gen2QInitVal);
            tariGroupElara.selectToggle(tari6_25usElara);
        }
    }

    void applyConfigurationsToElara() throws JSONException,ReaderException
    {
        if (isConnected) 
        {
            try
            {
                // Configure WorkFlow settings
                compareUpdateConfigValues();
                elaraReadPowerValue = rfReadElara.getText();
                elaraWritePowerValue = rfWriteElara.getText();
                String rfOnVal = rfOnTime.getText();
                String rfOffVal = rfOffTime.getText();
                if(currentListMap.containsKey("Mode"))
                {
                    if(currentListMap.get("Mode").equals("true"))
                    {
                        configureWorkflowSettingsToElara(ejsonp);
                    }
                }
                if(currentListMap.containsKey("_AudioVolume"))
                {
                    if(currentListMap.get("_AudioVolume").equals("true"))
                    {
                        configureBuzzerTone(ejsonp);
                    }
                }
                if(currentListMap.containsKey("RdrStart"))
                {
                    if(currentListMap.get("RdrStart").equals("true"))
                    {
                        configureAutonomousRead(ejsonp);
                    }
                }
                if(!rfReadElara.isDisabled())
                {
                    configureReadPower(ejsonp, elaraReadPowerValue);
                }
                if(!rfWriteElara.isDisabled())
                {
                    configureWritePower(ejsonp, elaraWritePowerValue);
                }
                if(currentListMap.containsKey("Metadata"))
                {
                    if(currentListMap.get("Metadata").equals("true"))
                    {
                        configureMetadataFields(ejsonp);
                    }
                }
                if(currentListMap.containsKey("FreqReg"))
                {
                    if(currentListMap.get("FreqReg").equals("true"))
                    {
                        configureRegion(ejsonp);
                    }
                }
                if(isGen2Supported())
                {
                    if(currentListMap.containsKey("DutyCycle") && 
                            (workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase(defaultElaraWorklfow[0]) 
                            || workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase(defaultElaraWorklfow[1])))
                    {
                        if(currentListMap.get("DutyCycle").equals("true"))
                        {
                            configureDutyCycle(ejsonp, rfOnVal, rfOffVal);
                        }
                    }
                    if(currentListMap.containsKey("Session"))
                    {
                        configureSession(ejsonp, currentListMap.get("Session"));
                    }
                    if(currentListMap.containsKey("Target"))
                    {
                        configureTarget(ejsonp, currentListMap.get("Target"));
                    }
                    if(currentListMap.containsKey("DataEncoding"))
                    {
                        configureDataEncoding(ejsonp, currentListMap.get("DataEncoding"));
                    }
                    if(currentListMap.containsKey("BLF"))
                    {
                        configureBLF(ejsonp, currentListMap.get("BLF"));
                    }
                    if(currentListMap.containsKey("Q"))
                    {
                        configureQ(ejsonp, currentListMap.get("Q"));
                    }
                    if(currentListMap.containsKey("ReReport"))
                    {
                        configureReReport(ejsonp,String.valueOf(reReport.isSelected()));
                    }
                    if(currentListMap.containsKey("NewTagDelay"))
                    {
                        configureNewTag(ejsonp);
                    }
                    if(currentListMap.containsKey("SameTagDelay"))
                    {
                        configureSameTag(ejsonp);
                    }
                }
                if(!elaraRS232)
                {
                    if(currentListMap.containsKey("_USBKBEnable"))
                    {
                        if(currentListMap.get("_USBKBEnable").equals("true"))
                        {
                            configureHID(ejsonp);
                        }
                    }
                    //ConfigureDataFormat
                    if(hidEnabled && !outputDataFormat.isDisabled())
                    {
                        if(currentListMap.containsKey("_KBDataFormat"))
                        {
                            if(currentListMap.get("_KBDataFormat").equals("true"))
                            {
                                configureOutputDataFormat(ejsonp);
                            }
                        }
                        resultsTabElaraCDC.setDisable(true);
                        resultsTabElaraHID.setDisable(true);
                    }
                    else if(hidEnabled && isRSKit)
                    {
                        resultsTabElaraCDC.setDisable(true);
                        resultsTabElaraHID.setDisable(true);
                    }
                    else
                    {
                        resultsTabElaraCDC.setDisable(false);
                        resultsTabElaraHID.setDisable(false);
                    }
                }
                if(currentListMap.containsKey("SerCfg"))
                {
                    if(currentListMap.get("SerCfg").equals("true") && elaraRS232)
                    {
                        int currentBaudRate = Integer.parseInt(probeElaraBaudRate.getSelectionModel().getSelectedItem().toString());
                        configureElaraBaudRate(ejsonp, currentBaudRate);
                        sp.setBaudRate(currentBaudRate);
                    }
                }
                alert.hide();
                showWarningErrorMessage("success", "Configurations applied successfully.");
                getCfgFieldsAllAndSetToUI(ejsonp);
                saveConfigValues();
            }
            finally
            {
                uniqueTagCountElara.setText(""+0);
                totalTagCountElara.setText(""+0);
                elaraTagData.clear();
                uniqueTagsElara = 0;
                totalTagsElara = 0;
                hideMessagePopup();
            }
        }
    }

    /**
     * Elara - Set Workflow mode
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureWorkflowSettingsToElara(ElaraJSONParser p) throws JSONException,ReaderException
    {
        String workFlowCommand = null;
        String responseReceived = null;
        String workflowSuccess;
        //get workflow selected from UI interface
        if(workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("HDR (Single Tag Read Near)"))
        {
            workFlowCommand = p.formJSONCommand(ECTConstants.SET_CFG_MODE_HDR);
        }
        else if(workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("Monitor (Bulk Read Far)"))
        {
            workFlowCommand = p.formJSONCommand(ECTConstants.SET_CFG_MODE_MONITOR);
        }
        else if(workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("Commission Single Tag"))
        {
            workFlowCommand = p.formJSONCommand(ECTConstants.SET_CFG_MODE_TAGCOMMISSION);
        }
        else if(workflow.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("Update Tag"))
        {
            workFlowCommand = p.formJSONCommand(ECTConstants.SET_CFG_MODE_TAGUPDATE);
        }

        // Send and receive the workflow command.
        responseReceived = sendMessage(sp, workFlowCommand, elaraTransportListener);
        JSONObject workflowJSON = new JSONObject(responseReceived);
        if(workflowJSON.get("Report").equals("SetCfg") && workflowJSON.get("ErrID").equals(0))
        {
            // get workflow configurations and update UI settings.
            getCfgFieldsModeAndSetToUI(p);
        }
        else
        {
            if(workflowJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)workflowJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set workflow");
            }
        }
    }

    /**
     * Elara - Set Buzzer tone
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureBuzzerTone(ElaraJSONParser p) throws JSONException,ReaderException
    {
        String selectedTone = buzzerTone.getSelectionModel().getSelectedItem().toString();
        String setTone = selectedTone;
        if(selectedTone.equalsIgnoreCase("Tone 1"))
        {
            setTone = "MUTE";
        }
        else if(selectedTone.equalsIgnoreCase("Tone 2"))
        {
            setTone = "LOW";
        }
        else if(selectedTone.equalsIgnoreCase("Tone 3"))
        {
            setTone = "MED";
        }
        else if(selectedTone.equalsIgnoreCase("Tone 4"))
        {
            setTone = "HIGH";
        }
        StringBuffer sb = new StringBuffer();
        sb.append(setTone);
        String setBuzzerToneCommand = p.customJSONCommand(ECTConstants.SET_CFG_AUDIO_VOLUME, sb);
        String responseReceived = sendMessage(sp, setBuzzerToneCommand, elaraTransportListener);
        JSONObject buzzerJSON = new JSONObject(responseReceived);
        if(buzzerJSON.get("Report").equals("SetCfg") && buzzerJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(buzzerJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)buzzerJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set beeper volume");
            }
        }
    }

    /**
     * Elara - Set output data format
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureOutputDataFormat(ElaraJSONParser p) throws JSONException,ReaderException
    {
        String selectedDataFormat = outputDataFormat.getSelectionModel().getSelectedItem().toString();
        String setDataFormat = selectedDataFormat;
        if(selectedDataFormat.equalsIgnoreCase("EPC"))
        {
            setDataFormat = "EPC";
        }
        else if(selectedDataFormat.equalsIgnoreCase("Plain Metadata"))
        {
            setDataFormat = "Metadata";
        }
        else if(selectedDataFormat.equalsIgnoreCase("RAIN Metadata"))
        {
            setDataFormat = "RAIN";
        }
        StringBuffer sb = new StringBuffer();
        sb.append(setDataFormat);
        String setOutputDataFormatCommand = p.customJSONCommand(ECTConstants.SET_CFG_KB_DATA_FORMAT, sb);
        String responseReceived = sendMessage(sp, setOutputDataFormatCommand, elaraTransportListener);
        JSONObject outputDataFormatJSON = new JSONObject(responseReceived);
        if(outputDataFormatJSON.get("Report").equals("SetCfg") && outputDataFormatJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(outputDataFormatJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)outputDataFormatJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set output data format");
            }
        }
    }

    /**
     * Elara - Set Autonomous read on boot
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureAutonomousRead(ElaraJSONParser p) throws JSONException,ReaderException
    {
        String setCfgRdrStartCommand;
        String setCfgRdrStartResp;
        //Get the status of auto read
        if(autonomousReadElara.isSelected())
        {
            setCfgRdrStartCommand = ejsonp.formJSONCommand(ECTConstants.SET_CFG_FIELDS_RDRSTART_ACTIVE);
            setCfgRdrStartResp = sendMessage(sp, setCfgRdrStartCommand, elaraTransportListener);
        }
        else
        {
            setCfgRdrStartCommand = ejsonp.formJSONCommand(ECTConstants.SET_CFG_FIELDS_RDRSTART_NOTACTIVE);
            setCfgRdrStartResp = sendMessage(sp, setCfgRdrStartCommand, elaraTransportListener);
        }
        JSONObject autonomousJSON = new JSONObject(setCfgRdrStartResp);
        if(autonomousJSON.get("Report").equals("SetCfg") && autonomousJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(autonomousJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)autonomousJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set autonomous mode");
            }
        }
    }

    /**
     * Elara - Set read power
     * @param p
     * @param readPowerValue
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureReadPower(ElaraJSONParser p, String readPowerValue) throws JSONException,ReaderException
    {
        JSONObject readPowerJSON = null;
        StringBuffer sbRead = new StringBuffer();
        sbRead.append(readPowerValue);
        String setReadPowerCommand = p.customJSONCommand(ECTConstants.SET_RZ_READ_POWER, sbRead);
        String responseRead = sendMessage(sp, setReadPowerCommand, elaraTransportListener);
        if(isValidJSON(responseRead))
        {
            readPowerJSON = new JSONObject(responseRead);
        }
        if(readPowerJSON.get("Report").equals("SetRZ") && readPowerJSON.get("ErrID").equals(0))
        {
            rfReadElara.setText(readPowerValue);
            readPowerSliderElara.setValue(Double.parseDouble(readPowerValue));
        }
        else
        {
            if(readPowerJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)readPowerJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set read power");
            }
        }
    }

    /**
     * Elara - Set write power
     * @param p
     * @param writePowerValue
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureWritePower(ElaraJSONParser p, String writePowerValue) throws JSONException,ReaderException
    {
        JSONObject writePowerJSON = null;
        StringBuffer sbWrite = new StringBuffer();
        sbWrite.append(writePowerValue);
        String setWritePowerCommand = p.customJSONCommand(ECTConstants.SET_RZ_WRITE_POWER, sbWrite);
        String responseWrite = sendMessage(sp, setWritePowerCommand, elaraTransportListener);
        if(isValidJSON(responseWrite))
        {
            writePowerJSON = new JSONObject(responseWrite);
        }
        if(writePowerJSON.get("Report").equals("SetRZ") && writePowerJSON.get("ErrID").equals(0))
        {
            rfWriteElara.setText(writePowerValue);
            writePowerSliderElara.setValue(Double.parseDouble(writePowerValue));
        }
        else
        {
            if(writePowerJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)writePowerJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set write power");
            }
        }
    }

    /**
     * Elara - Configure region
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureRegion(ElaraJSONParser p) throws JSONException,ReaderException
    {
        String region = (String) regionElara.getSelectionModel().getSelectedItem();
        StringBuffer sb = new StringBuffer();
        sb.append(region.trim());
        String setRegionCommand = p.customJSONCommand(ECTConstants.SET_CFG_FREQ_REG, sb);
        String responseReceived = sendMessage(sp, setRegionCommand, elaraTransportListener);
        JSONObject regionJSON = new JSONObject(responseReceived);
        if(regionJSON.get("Report").equals("SetCfg") && regionJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(regionJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)regionJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set region");
            }
        }
    }

    /**
     * Elara - Configure duty cycle
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureDutyCycle(ElaraJSONParser p, String rfOnVal, String rfOffVal) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(rfOnVal);
        sb.append(",");
        sb.append(rfOffVal);
        String setDutyCycle = p.customJSONCommand(ECTConstants.SET_RZ_DUTY_CYCLE, sb);
        String responseReceived = sendMessage(sp, setDutyCycle, elaraTransportListener);
        JSONObject dutyCycleJSON = new JSONObject(responseReceived);
        if(dutyCycleJSON.get("Report").equals("SetRZ") && dutyCycleJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(dutyCycleJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)dutyCycleJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set duty cycle");
            }
        }
    }

    /**
     * Elara - Configure Session
     * @param p
     * @param session
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureSession(ElaraJSONParser p, String session) throws JSONException,ReaderException
    {
        int sessionVal = 0;
        switch(session){
            case "S0":
                sessionVal = 0;
                break;
            case "S1":
                sessionVal = 1;
                break;
            case "S2":
                sessionVal = 2;
                break;
            case "S3":
                sessionVal = 3;
                break;
        }
        StringBuffer sb = new StringBuffer();
        sb.append(sessionVal);
        String setSession = p.customJSONCommand(ECTConstants.SET_RZ_SESSION, sb);
        String responseReceived = sendMessage(sp, setSession, elaraTransportListener);
        JSONObject sessionJSON = new JSONObject(responseReceived);
        if(sessionJSON.get("Report").equals("SetRZ") && sessionJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(sessionJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)sessionJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set session");
            }
        }
    }

    /**
     * Elara - Set target
     * @param p
     * @param target
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureTarget(ElaraJSONParser p, String target) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(target);
        String setTarget = p.customJSONCommand(ECTConstants.SET_RZ_TARGET, sb);
        String responseReceived = sendMessage(sp, setTarget, elaraTransportListener);
        JSONObject targetJSON = new JSONObject(responseReceived);
        if(targetJSON.get("Report").equals("SetRZ") && targetJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(targetJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)targetJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set target");
            }
        }
    }

    /**
     * Elara - Configure data encoding(M)
     * @param p
     * @param dataEncoding
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureDataEncoding(ElaraJSONParser p, String dataEncoding) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(dataEncoding);
        String setDataEncoding = p.customJSONCommand(ECTConstants.SET_RZ_DATA_ENCODING, sb);
        String responseReceived = sendMessage(sp, setDataEncoding, elaraTransportListener);
        JSONObject dEJSON = new JSONObject(responseReceived);
        if(dEJSON.get("Report").equals("SetRZ") && dEJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(dEJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)dEJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set tag encoding");
            }
        }
    }

    /**
     * Elara - Configure BLF
     * @param p
     * @param blf
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureBLF(ElaraJSONParser p, String blf) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(blf);
        String setELF = p.customJSONCommand(ECTConstants.SET_RZ_BLF, sb);
        String responseReceived = sendMessage(sp, setELF, elaraTransportListener);
        JSONObject blfJSON = new JSONObject(responseReceived);
        if(blfJSON.get("Report").equals("SetRZ") && blfJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(blfJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)blfJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set BLF");
            }
        }
    }

    /**
     * Elara - Configure Q
     * @param p
     * @param qMode
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureQ(ElaraJSONParser p, String qMode) throws JSONException,ReaderException
    {
        boolean QVal = false;
        boolean QMode = false;
        if(qMode.equalsIgnoreCase("Dynamic"))
        {
            QVal = setQ(p, qMode);
            QMode = setQMode(p, qMode);
        }
        else if(qMode.equalsIgnoreCase("Static"))
        {
            QMode = setQMode(p, qMode);
            QVal = setQ(p, qMode);
        }
        if(QVal && QMode)
        {
            //Do nothing
        }
        else
        {
            throw new ReaderException("Failed to set Q");
        }
    }

    /**
     * Elara - Helper method to set Q
     * @param p
     * @param qMode
     * @return
     * @throws JSONException 
     */
    private boolean setQ(ElaraJSONParser p , String qMode) throws JSONException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(qListElara.getValue());
        String setQ = p.customJSONCommand(ECTConstants.SET_RZ_Q, sb);
        String qResponse = sendMessage(sp, setQ, elaraTransportListener);
        JSONObject qJSON = new JSONObject(qResponse);
        return qJSON.get("Report").equals("SetRZ") && qJSON.get("ErrID").equals(0);
    }

    /**
     * Elara - Helper method to set QMode
     * @param p
     * @param qMode
     * @return
     * @throws JSONException 
     */
    private boolean setQMode(ElaraJSONParser p , String qMode)throws JSONException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(qMode);
        String setQMode = p.customJSONCommand(ECTConstants.SET_RZ_QMODE, sb);
        String qModeResponse = sendMessage(sp, setQMode, elaraTransportListener);
        JSONObject qModeJSON = new JSONObject(qModeResponse);
        return qModeJSON.get("Report").equals("SetRZ") && qModeJSON.get("ErrID").equals(0);
    }

    /**
     * Elara - Configure Tag Re-report
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureReReport(ElaraJSONParser p, String value) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(value);
        String setReReport = p.customJSONCommand(ECTConstants.SET_CFG_REREPORT, sb);
        String reRepRes = sendMessage(sp, setReReport, elaraTransportListener);
        JSONObject reRepJSON = new JSONObject(reRepRes);
        if(reRepJSON.get("Report").equals("SetCfg") && reRepJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(reRepJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)reRepJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set Tag ReReport");
            }
        }
    }

    /**
     * Elara - Helper method for Tag Re-report - New tag delay
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureNewTag(ElaraJSONParser p) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(newTagDelay.getText());
        String setNewTag = p.customJSONCommand(ECTConstants.SET_CFG_NEW_TAG_DELAY, sb);
        String newTagRes = sendMessage(sp, setNewTag, elaraTransportListener);
        JSONObject newTagJSON = new JSONObject(newTagRes);
        if(newTagJSON.get("Report").equals("SetCfg") && newTagJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(newTagJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)newTagJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set new tag delay");
            }
        }
    }

    /**
     * Elara - Helper method for Tag Re-report - Same tag delay
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureSameTag(ElaraJSONParser p) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(sameTagDelay.getText());
        String setSameTag = p.customJSONCommand(ECTConstants.SET_CFG_SAME_TAG_DELAY, sb);
        String sameTagRes = sendMessage(sp, setSameTag, elaraTransportListener);
        JSONObject sameTagJSON = new JSONObject(sameTagRes);
        if(sameTagJSON.get("Report").equals("SetCfg") && sameTagJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(sameTagJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)sameTagJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set same tag delay");
            }
        }
    }

    /**
     * Elara - Configure metadata
     * @param p
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureMetadataFields(ElaraJSONParser p) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        if(!elaraMetadataAntenna.isDisabled())
        {
            if(elaraMetadataAntenna.isSelected())
            {
                sb.append("SpotAnt");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotAnt");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataDateTime.isDisabled())
        {
            if(elaraMetadataDateTime.isSelected())
            {
                sb.append("SpotDT");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotDT");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataInventoryCount.isDisabled())
        {
            if(elaraMetadataInventoryCount.isSelected())
            {
                sb.append("SpotInvCnt");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotInvCnt");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataPhase.isDisabled())
        {
            if(elaraMetadataPhase.isSelected())
            {
                sb.append("SpotPhase");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotPhase");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataRSSI.isDisabled())
        {
            if(elaraMetadataRSSI.isSelected())
            {
                sb.append("SpotRSSI");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotRSSI");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataFreq.isDisabled())
        {
            if(elaraMetadataFreq.isSelected())
            {
                sb.append("SpotFreq");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotFreq");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataGen2Q.isDisabled())
        {
            if(elaraMetadataGen2Q.isSelected())
            {
                sb.append("SpotGen2_Q");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotGen2_Q");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataGen2LF.isDisabled())
        {
            if(elaraMetadataGen2LF.isSelected())
            {
                sb.append("SpotGen2_LF");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotGen2_LF");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataGen2Target.isDisabled())
        {
            if(elaraMetadataGen2Target.isSelected())
            {
                sb.append("SpotGen2_Target");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotGen2_Target");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataGPIO.isDisabled())
        {
            if(elaraMetadataGPIO.isSelected())
            {
                sb.append("SpotGPIO");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotGPIO");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
        if(!elaraMetadataProtocol.isDisabled())
        {
            if(elaraMetadataProtocol.isSelected())
            {
                sb.append("SpotProt");
                sb.append(":");
                sb.append(true);
                sb.append(",");
            }
            else
            {
                sb.append("SpotProt");
                sb.append(":");
                sb.append(false);
                sb.append(",");
            }
        }
//        if(elaraMetadataSensor.isSelected())
//        {
//            sb.append("SpotSensor");
//            sb.append(":");
//            sb.append(true);
//            sb.append(",");
//        }
//        else
//        {
//            sb.append("SpotSensor");
//            sb.append(":");
//            sb.append(false);
//            sb.append(",");
//        }
        String setMetadataCommand = p.customJSONCommand(ECTConstants.SET_CFG_METADATA, sb);
        String responseReceived = sendMessage(sp, setMetadataCommand, elaraTransportListener);
        JSONObject metadataJSON = new JSONObject(responseReceived);
        if(metadataJSON.get("Report").equals("SetCfg") && metadataJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(metadataJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)metadataJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set metadata fields");
            }
        }
    }

    /**
     * Elara - Configure Baud rate
     * @param p
     * @param baudRate
     * @throws JSONException
     * @throws ReaderException 
     */
    private void configureElaraBaudRate (ElaraJSONParser p, int baudRate) throws JSONException,ReaderException
    {
        StringBuffer sb = new StringBuffer();
        sb.append(baudRate);
        String setBaudRateCommand = p.customJSONCommand(ECTConstants.SET_CFG_BAUDRATE, sb);
        String responseReceived = sendMessage(sp, setBaudRateCommand, elaraTransportListener);
        JSONObject baudRateJSON = new JSONObject(responseReceived);
        if(baudRateJSON.get("Report").equals("SetCfg") && baudRateJSON.get("ErrID").equals(0))
        {
            //Do nothing
        }
        else
        {
            if(baudRateJSON.toString().contains("ErrInfo"))
            {
                throw new ReaderException((String)baudRateJSON.get("ErrInfo"));
            }
            else
            {
                throw new ReaderException("Failed to set baudrate");
            }
        }
    }

    /**
     * Method to set configurations to Elara
     */
    void applyConfigurationsToModule()
    {
        if (isConnected) 
        {
            try
            {
                int len = 0;
                int startAddr = 0;
                GpiPinTrigger gpiPinTrigger = new GpiPinTrigger();
                List<Integer> antennaList = new ArrayList<Integer>();
                List<TagProtocol> protocolList = new ArrayList<TagProtocol>();
                TagProtocol protocol = null;
                Gen2.LinkFrequency linkFreq = null;
                Gen2.Tari tari = null;
                Gen2.Target target = null;
                Gen2.TagEncoding tagEncoding = null;
                Gen2.Session session = null;
                Gen2.Q q = null;
                TagOp Op = null;
                int asyncOnTime, asyncOffTime;

                if(region.getSelectionModel().getSelectedItem() != null)
                {
                    r.paramSet("/reader/region/id", Reader.Region.valueOf(region.getSelectionModel().getSelectedItem().toString()));
                }
                r.paramSet("/reader/baudRate", Integer.parseInt(probeBaudRate.getSelectionModel().getSelectedItem().toString()));

                if (antenna1.isSelected()) 
                {
                    antennaList.add(1);
                }
                if (antenna2.isSelected()) 
                {
                    antennaList.add(2);
                }
                if (antenna3.isSelected()) 
                {
                    antennaList.add(3);
                }
                if (antenna4.isSelected()) 
                {
                    antennaList.add(4);
                }

                if((autonomousRead.isSelected() || gpiTriggerRead.isSelected()) && antennaList.isEmpty())
                {
                   showWarningErrorMessage("warning", "Select atleast one antenna");
                   Platform.runLater(new Runnable() 
                   {
                       @Override
                       public void run() 
                       {
                           readWriteTitledPane.setExpanded(true);
                       }
                   });
                   return;
                }

                if (gen2.isSelected()) 
                {
                    protocolList.add(TagProtocol.GEN2);
                    r.paramSet("/reader/tagop/protocol", TagProtocol.GEN2);
                }
                if (iso18000.isSelected()) 
                {
                    protocolList.add(TagProtocol.ISO180006B);
                }
                if (ipx64.isSelected()) 
                {                  
                    protocolList.add(TagProtocol.IPX64);
                }
                if (ipx256.isSelected()) 
                {
                    protocolList.add(TagProtocol.IPX256);
                }
              
                if(embeddedReadEnable.isSelected())
                {
                    
                    if (embeddedEnd.getText().toLowerCase().startsWith("0x"))
                    {
                       len = Integer.parseInt(embeddedEnd.getText().substring(2),16);
                    }
                    else
                    {
                       len = Integer.parseInt(embeddedEnd.getText());
                    }
                   
                    if (embeddedStart.getText().toLowerCase().startsWith("0x"))
                    {
                        startAddr = Integer.parseInt(embeddedStart.getText().substring(2),16);
                    }
                    else
                    {
                       startAddr = Integer.parseInt(embeddedStart.getText());   
                    }
                    
                    if(len > 100)
                    {
                        showWarningErrorMessage("error", "Embedded Read Data: Number of words can't be more than 0x64 words,it can be"
                          +" read by incrementing start address and length of target read data");
                        return;
                    }
                    
                    if(startAddr > 0xFFFF)
                    {
                       showWarningErrorMessage("error", "Embedded Read Data: Starting Word Address can't be more than 0xFFFF");
                        return;
                    }
                }
                
                if(false
                        || iso18000.isSelected()
                        || ipx64.isSelected()
                        || ipx256.isSelected())
                {
                    if (gpiTriggerRead.isSelected() || autonomousRead.isSelected())
                    {
                        showWarningErrorMessage("warning", "Autonomous read and Gpi trigger are not supported other than GEN2 protocol."
                                + " \n Application will read only Gen2 tags.");
                        iso18000.setSelected(false);
                        ipx64.setSelected(false);
                        ipx256.setSelected(false);
                        if(!gen2.isSelected())
                        {
                            gen2.setSelected(true);
                            protocolList.add(TagProtocol.GEN2);
                        }
                    }
                }
                if(protocolList.isEmpty())
                {
                   showWarningErrorMessage("warning","Select Gen2 protocol");
                   return;
                }
                
                if(dutyCycleOn.getText() == null || dutyCycleOn.getText().equals(""))
                {
                    showWarningErrorMessage("error", "DutyCycle On(ms) can't be empty");
                    dutyCycleOn.setText("1000");
                    return;
                }
                if(dutyCycleOff.getText() == null || dutyCycleOff.getText().equals("")) 
                {
                    showWarningErrorMessage("error", "DutyCycle Off(ms) can't be empty");
                    dutyCycleOff.setText("0");
                    return;
                }
                
                asyncOnTime = Integer.parseInt(dutyCycleOn.getText());    
                asyncOffTime = Integer.parseInt(dutyCycleOff.getText());
                
                r.paramSet("/reader/read/asyncOnTime",asyncOnTime);    
                r.paramSet("/reader/read/asyncOffTime",asyncOffTime);

                linkFreq = Gen2.LinkFrequency.valueOf(linkFreqGroup.getSelectedToggle().getUserData().toString());
                tari = Gen2.Tari.valueOf(tariGroup.getSelectedToggle().getUserData().toString());
                tagEncoding = Gen2.TagEncoding.valueOf(tagEncodeGroup.getSelectedToggle().getUserData().toString());
                target = Gen2.Target.valueOf(targetGroup.getSelectedToggle().getUserData().toString());
                session = Gen2.Session.valueOf(sessionGroup.getSelectedToggle().getUserData().toString());

                int qSelectedOption =  Integer.parseInt(qGroup.getSelectedToggle().getUserData().toString());
                if(qSelectedOption == 0){
                    q = new Gen2.DynamicQ();
                }
                else if(qSelectedOption == 1){
                    int staticQvalue = Integer.parseInt(staticQList.getSelectionModel().getSelectedItem().toString());
                    q = new Gen2.StaticQ(staticQvalue);
                }

                if (gpiTriggerRead.isSelected()) 
                {
                    int[] gpiPin = new int[1];
                    gpiPin[0] = Integer.parseInt(autoReadGpiGroup.getSelectedToggle().getUserData().toString());
                    gpiPinTrigger.enable = true;
                    try
                    {
                       r.paramSet("/reader/read/trigger/gpi", gpiPin);
                    }
                    catch (Exception ex)
                    {
                        if(false
                            || ex.getMessage().contains("The reader received a valid command with an unsupported or invalid parameter")
                            || ex.getMessage().contains("The data length in the message is less than or more"))
                        {
                           showWarningErrorMessage("warning","Gpi trigger read is not supported on this firmware");
                           autonomousRead.selectedProperty().setValue(false);
                           return ;
                        }
                        else
                        {
                            showWarningErrorMessage("error", ex.getMessage());
                            return ;
                        }
                    }
                }

                int readPower = (int) (Double.parseDouble(rfRead.getText()) * 100);
                int writePower = (int) (Double.parseDouble(rfWrite.getText()) * 100);

                r.paramSet("/reader/gen2/BLF", linkFreq);
                r.paramSet("/reader/gen2/tari", tari);
                r.paramSet("/reader/gen2/target", target);
                r.paramSet("/reader/gen2/tagEncoding", tagEncoding);
                r.paramSet("/reader/gen2/session", session);
                r.paramSet("/reader/gen2/q", q);
                r.paramSet("/reader/radio/readPower", readPower);
                r.paramSet("/reader/radio/writePower", writePower);

                SimpleReadPlan srp = null;
                ReadPlan[] rp = new ReadPlan[protocolList.size()];

                if (embeddedReadEnable.isSelected() && (autonomousRead.isSelected() || gpiTriggerRead.isSelected())) 
                {
                    if(antennaList.size() > 0)
                    {
                       r.paramSet("/reader/tagop/antenna", antennaList.get(0)); 
                    }
                    
                    String memBank = embeddedMemoryBank.getSelectionModel().getSelectedItem().toString();
                    if (len < 0) {
                        len = 0;
                    }
                    Op = new Gen2.ReadData(Gen2.Bank.valueOf(memBank), startAddr, (byte) len);                    
                }
                int i = 0;
                for (TagProtocol proto : protocolList)
                {
                    srp = new SimpleReadPlan(ReaderUtil.buildIntArray(antennaList), proto, null, Op, 100, false);
                    rp[i] = srp;
                    i++;
                }

                if (autonomousRead.isSelected())
                {
                    srp.enableAutonomousRead = true;
                }
                if(gpiTriggerRead.isSelected())
                {
                    srp.enableAutonomousRead = true;
                    srp.triggerRead = gpiPinTrigger;
                }

                try
                {
                    SerialReader.ReaderStatsFlag[] READER_STATISTIC_FLAGS = {SerialReader.ReaderStatsFlag.TEMPERATURE};
                    r.paramSet("/reader/stats/enable", READER_STATISTIC_FLAGS);
                }
                catch (Exception ex)
                {
                    /*Ignore the exception if reader stats does not support on current reader.
                     *Still need to do autonomous read. 
                     */
                }
                try
                {
                    setHopTime();
                }
                catch(Exception ex)
                {
                    showWarningErrorMessage("error", ex.getMessage());
                    return;
                }

                try
                {
                  setHopTable();
                }
                catch(Exception ex)
                {
                    showWarningErrorMessage("error", ex.getMessage());
                    return;
                }

                if (autonomousRead.isSelected() || gpiTriggerRead.isSelected())
                {
                    if (protocolList.size() > 1)
                    {
                        MultiReadPlan mrp = new MultiReadPlan(rp);
                        mrp.enableAutonomousRead = true;
                        r.paramSet("/reader/read/plan", mrp);
                    } 
                    else
                    {
                        r.paramSet("/reader/read/plan", srp);
                    }
                    
                    try
                    {
                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.SAVEWITHREADPLAN));
                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.RESTORE));
                      
                        setTitledPanesStatus(true, true, true, false, true, true, false);
                        setTitledPanesExpandedStatus(false, false, false, true, false, false, true);

                        applyButton.setDisable(true);
                        applyButton.setOpacity(disableOpacity);
                        revertButton.setDisable(true);
                        revertButton.setOpacity(disableOpacity);
                        isAutonomousReadStarted = true;

                        read();
                        isReading = true;
                        setReaderStatus(1);
                        r.receiveAutonomousReading();
                        mainTabs.getSelectionModel().select(readTab);
                        tableView.getItems().clear();
                        uniqueTagCount.setText("");
                        totalTagCount.setText("");
                        new Thread(dataPostThread).start();
                    }
                    catch (Exception ex)
                    {
                        if(false 
                            || ex.getMessage().contains("/reader/userConfig")
                            || ex.getMessage().contains("The reader received a valid command with an unsupported or invalid parameter")
                            || ex.getMessage().contains("The data length in the message is less than or more"))
                        {
                           showWarningErrorMessage("warning","Autonomous read is not supported on this fimware");
                           autonomousRead.selectedProperty().setValue(false);
                        }
                        else
                        {
                            showWarningErrorMessage("error", ex.getMessage());
                        }
                    }
                }
                else
                {
                    if(!antennaList.isEmpty())
                    {
                        srp.enableAutonomousRead = false;
                        srp.antennas = ReaderUtil.buildIntArray(antennaList);
                        r.paramSet("/reader/read/plan", srp);
                        r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.SAVEWITHREADPLAN));
                        showWarningErrorMessage("success", "Configurations applied successfully");
                    }
                    else
                    {
                        showWarningErrorMessage("error", "Please select atleast one antenna to save the configurations with autonomous read enabled or disabled");
                    }
                }
            }
            catch (Exception re)
            {
                re.printStackTrace();
                showWarningErrorMessage("error", re.getMessage());
            }
        }
    }

    private void setHopTime() throws ReaderException
    {
        String hotTimeTxt = hopTime.getText();
        if(!isInteger(hotTimeTxt)){
            
            throw new ReaderException("Hoptime value was either too large or too small for int");
        }
        int hopTime = Integer.parseInt(hotTimeTxt);
        r.paramSet("/reader/region/hopTime", hopTime);
    }

    private void setHopTable() throws ReaderException
    {
        String hotTableTxt = hopTable.getText();
        if(!isIntegerArray(hotTableTxt.split(",")))
        {
            throw new ReaderException("Hoptable values was either too large or too small for int");
        }
        int hopTable[] = stringArray2IntArray(hotTableTxt.split(","));
        r.paramSet("/reader/region/hopTable", hopTable);
    }

    private int[] stringArray2IntArray(String[] values){
        try{
            if(values != null && values.length > 0){
                int[] num = new int[values.length];
                for(int i = 0; i <= values.length-1; i++ ){
                    num[i] = Integer.parseInt(values[i].trim());
                }
                return num;
            }
        }catch(Exception e){
           
        }
        return null;
    }
    
    public void setTitledPanesStatus(boolean rw, boolean pt, boolean rt, boolean dp, boolean pf, boolean fu, boolean abt)
    {
        Platform.runLater(new Runnable() 
        {
            @Override
            public void run() 
            {
                readWriteTitledPane.setDisable(rw);
                performanceTuningTitlePane.setDisable(pt);
                regulatoryTestingPane.setDisable(rt);
                displayOptionsTitlePane.setDisable(dp);
                profileTitlePane.setDisable(pf);
                firmwareUpdateTitledPane.setDisable(fu);
                aboutTitledPane.setDisable(abt);
            }
        });
    }

    public void setTitledPanesExpandedStatus(boolean rw, boolean pt, boolean rt, boolean dp, boolean pf, boolean fu, boolean abt)
    {
        Platform.runLater(new Runnable() 
        {
            @Override
            public void run() 
            {
                readWriteTitledPane.setExpanded(rw);
                performanceTuningTitlePane.setExpanded(pt);
                regulatoryTestingPane.setExpanded(rt);
                displayOptionsTitlePane.setExpanded(dp);
                profileTitlePane.setExpanded(pf);
                firmwareUpdateTitledPane.setExpanded(fu);
                aboutTitledPane.setExpanded(abt);
            }
        });
    }

    @FXML
    private void loadConfig()
    {
        try
        {
            ReadExceptionListener loadExceptionListener = new LoadSaveExceptionReciver();
            r.addReadExceptionListener(loadExceptionListener);
            fileFilters = new ArrayList<String>();
            fileFilters.add("*.urac");
            chooseFile("Choose configuration file", "Configuration file (*.urac)", fileFilters);
            File configFile = fileChooser.showOpenDialog(stage);

            if (configFile != null)
            {
                isLoadSaveCompleted = false;
                final String path  = configFile.getAbsolutePath();

                new Thread(new Runnable() 
                {
                    @Override
                    public void run() 
                    {
                        try
                        {
                            if(loadSaveError == null)
                            {
                                loadSaveError = new StringBuffer();
                            }
                            loadSaveProperties = new Properties();
                            FileInputStream fis =  new FileInputStream(configFile);
                            loadSaveProperties.load(fis);
                            fis.close();

                            Platform.runLater(new Runnable()
                            {
                                @Override
                                public void run() 
                                {
                                    try
                                    {
                                        loadConfigurations();
                                        enableDisableGen2Settings(new ActionEvent());
                                        isLoadSaveCompleted = true;
                                    }    
                                    catch(Exception e)
                                    {
                                        e.printStackTrace();
                                        isLoadSaveCompleted = true;
                                    }
                                }
                            });
                            while(!isLoadSaveCompleted)
                            {
                                // do nothing wait for untill load save completes
                            }    
                            r.removeReadExceptionListener(loadExceptionListener);
                            if(loadSaveError != null && !loadSaveError.toString().equals(""))
                            {
                                JOptionPane.showMessageDialog(null, loadSaveError , "Load Save Errors", JOptionPane.ERROR_MESSAGE);
                                loadSaveError = null;
                            }

                            int option = JOptionPane.showConfirmDialog(null, "Applying configurations along with autonomous read will navigate to tag results.", "Confirmation", JOptionPane.YES_NO_OPTION);
                            if (option == JOptionPane.YES_OPTION)
                            {
                                applyConfigurationsToModule();
                            }
                        }
                        catch(Exception e)
                        {
                            //e.printStackTrace();
                            loadSaveError = null;
                        }
                    }
                }).start();
            }
            isLoadSaveConfiguration = false;
        } 
        catch (Exception ex)
        {
            JOptionPane.showMessageDialog(null, ex.getMessage(), "Error", JOptionPane.ERROR_MESSAGE);
            loadSaveError = null;
        }
    }

    public void loadConfigurations()
    {
        if(loadSaveProperties != null)
        {
            Properties prop = loadSaveProperties;
            
            for(Object key: prop.keySet())
            {
                String param = key.toString();
                String value = prop.get(key).toString().trim();

                if(param.equalsIgnoreCase("/reader/region/id"))
                 {
                    Reader.Region[] supportedRegions  = null;
                    Reader.Region currentRegion       = null;
                    try 
                    {
                        supportedRegions = (Reader.Region[]) r.paramGet("/reader/region/supportedRegions");
                    } 
                    catch (Exception e) 
                    {

                    }
                    
                    if(null == value || value.equals(""))
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                    else
                    {
                        try
                        {
                            currentRegion = Reader.Region.valueOf(value);
                            r.paramSet("/reader/region/id", currentRegion);
                            region.getSelectionModel().select(currentRegion);
                        }
                        catch(Exception e)
                        {
                            loadSaveError.append("Invalid value " + value + " for " + param +". Please enter a supported region"
                            + " ACT gets and applies region "+currentRegion+" to ACT or change to the supported value and reload the configuration. \n");
                        }
                    }
                }

                else if(param.equalsIgnoreCase("/reader/baudRate"))
                {
                    if(probeBaudRate.getItems().contains(value))
                    {
                        probeBaudRate.getSelectionModel().select(value);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                }

                else if(param.equalsIgnoreCase("/reader/radio/readPower"))
                {
                   try
                   {
                      Float power = Float.parseFloat(value);
                      if(power >= minReaderPower && power <= maxReaderPower)
                      {
                          rfRead.setText(String.valueOf(power / 100));
                          powerEventListener(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                          readPowerChanged();
                      }
                      else
                      {
                         value = value.isEmpty() ? "empty" : value; 
                         loadSaveError.append("Read power "+ param +" value "+ value
                               + " setting failed.  Please enter within "+minReaderPower+" and  "+maxReaderPower
                         + " in Load/Save file. Application sets this parameter to previous set value. \n");
                      }
                   }
                   catch(Exception ex)
                   {
                       loadSaveError.append("Invalid value "+ value +" for "+ param
                               + ". Application sets this parameter to previous set value. \n");
                   }
                }
                else if(param.equalsIgnoreCase("/reader/radio/writePower"))
                {
                   try
                   {
                      Float power = Float.parseFloat(value);
                      if(power >= minReaderPower && power <= maxReaderPower)
                      {
                          rfWrite.setText(String.valueOf(power / 100));
                          powerEventListener(readPowerSlider, writePowerSlider, rfRead, rfWrite);
                          writePowerChanged();
                      }
                      else
                      {
                         value = value.isEmpty() ? "empty" : value; 
                         loadSaveError.append("Write power "+ param +" value "+ value
                               + " setting failed.  Please enter within "+minReaderPower+" and  "+maxReaderPower
                         + " in Load/Save file. Application sets this parameter to previous set value. \n");
                      }
                   }
                   catch(Exception ex)
                   {
                       loadSaveError.append("Invalid value "+ value +" for "+ param
                               + ". Application sets this parameter to previous set value. \n");
                   }
                }
                else if(param.equalsIgnoreCase("/reader/gen2/BLF"))
                {
                    if(value.equalsIgnoreCase("LINK640KHZ"))
                    {
                        if (false
                                || readerModel.equalsIgnoreCase("M5e")
                                || readerModel.equalsIgnoreCase("M5e PRC")
                                || readerModel.equalsIgnoreCase("M5e EU"))
                        {
                            loadSaveError.append("Value "+value +" not supported on this reader."
                                    + ". Application skips this parameter. \n");
                        }
                        else
                        {
                          String tagEncoding = prop.getProperty("/reader/gen2/tagEncoding");
                          if(false
                                  || tagEncoding.equalsIgnoreCase("M2")
                                  || tagEncoding.equalsIgnoreCase("M4")
                                  || tagEncoding.equalsIgnoreCase("M8"))
                          {
                            continue;
                          }
                          link640Khz.selectedProperty().setValue(true);
                        }
                    }
                    else if(value.equalsIgnoreCase("LINK250KHZ"))
                    {
                        link250Khz.selectedProperty().setValue(true);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/gen2/tari"))
                {
                    String linkFreq = prop.getProperty("/reader/gen2/BLF");
                    value = value.isEmpty() ? "empty" : value;
                    if(linkFreq.equalsIgnoreCase("LINK640KHZ") && 
                            (false 
                            || value.equalsIgnoreCase("TARI_12_5US")
                            || value.equalsIgnoreCase("TARI_25US")))
                    {
                        loadSaveError.append("Invalid tagencoding value "+ value +" for the given link frequency "+linkFreq
                        +". Apllication skips this parameter and set to previous set value. \n");
                        continue;
                    }
                    else if (value.equalsIgnoreCase("TARI_6_25US"))
                    {
                       tari6_25us.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("TARI_12_5US"))
                    {
                       tari12_5us.selectedProperty().setValue(true);
                    }
                    else if(value.equalsIgnoreCase("TARI_25US"))
                    {
                       tari25us.selectedProperty().setValue(true);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/gen2/tagEncoding"))
                {
                    String linkFreq = prop.getProperty("/reader/gen2/BLF");
                    value = value.isEmpty() ? "empty" : value;
                    if(linkFreq.equalsIgnoreCase("LINK640KHZ") && 
                            (false 
                            || value.equalsIgnoreCase("M2")
                            || value.equalsIgnoreCase("M4")
                            || value.equalsIgnoreCase("M8")))
                    {
                        loadSaveError.append("Invalid tagencoding value "+ value +" for the given link frequency "+linkFreq
                        +". Apllication skips this parameter and set to previous set value. \n");
                        continue;
                    }
                    
                    if(value.equalsIgnoreCase("FM0"))
                    {
                       fm0.selectedProperty().setValue(true);
                    }
                    else if (value.equalsIgnoreCase("M2"))
                    {
                       m2.selectedProperty().setValue(true);
                    }
                    else if (value.equalsIgnoreCase("M4"))
                    {
                       m4.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("M8"))
                    {
                       m8.selectedProperty().setValue(true);
                    }
                    else
                    {
                      loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/gen2/session"))
                {
                    if(value.equalsIgnoreCase("S0"))
                    {
                       sessionS0.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("S1"))
                    {
                       sessionS1.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("S2"))
                    {
                       sessionS2.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("S3"))
                    {
                        sessionS3.selectedProperty().setValue(true);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/gen2/target"))
                {
                    if(value.equalsIgnoreCase("A"))
                    {
                       targetA.selectedProperty().setValue(true);
                    }
                    else if (value.equalsIgnoreCase("AB"))
                    {
                       targetAB.selectedProperty().setValue(true); 
                    }
                    else if (value.equalsIgnoreCase("B"))
                    {
                       targetB.selectedProperty().setValue(true);
                    }
                    else if (value.equalsIgnoreCase("BA"))
                    {
                       targetBA.selectedProperty().setValue(true);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                    gen2TargetChangeConfiguration();
                }
                else if(param.equalsIgnoreCase("/reader/gen2/q"))
                {
                    if(value != null && !value.isEmpty() && value.equalsIgnoreCase("StaticQ"))
                    {
                       staticQ.selectedProperty().setValue(true);
                       String qText = prop.getProperty("/application/performanceTuning/staticQValue");
                        if(!isInteger(qText))
                        {
                            loadSaveError.append("Invalid value "+qText +" for /application/performanceTuning/staticQValue"
                                    + ". Application sets this parameter to previous set value. \n");
                        }
                        else
                        {
                            int qValue = Integer.parseInt(qText);
                            if(qValue >= 0 && qValue <= 15)
                            {
                                staticQList.getSelectionModel().select(qValue);
                            }
                            else
                            {
                                loadSaveError.append("Invalid value "+qText+" for /application/performanceTuning/staticQValue value within 0 to 15"
                                    + ". Application sets this parameter to previous set value. \n");
                            }
                        }
                    }
                    else if (value != null && !value.isEmpty() && value.equalsIgnoreCase("DynamicQ"))
                    {
                       dynamicQ.selectedProperty().setValue(true); 
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+value +" for "+ param
                                    + ". Application sets this parameter to previous set value. \n");
                    }
                    gen2Q(new ActionEvent());
                }

                else if(param.equalsIgnoreCase("/reader/region/hopTime"))
                {
                    if (value != null && !value.isEmpty() && isInteger(value))
                    {
                        hopTime.setText(value);
                        try
                        {
                            setHopTime();
                        }
                        catch(Exception e)
                        {
                            value = value.isEmpty() ? "empty" : value;
                            loadSaveError.append("Invalid value "+ value +" for "+ param+". "+e.getMessage()
                             +". Application sets this parameter based on region value. \n");
                            try
                            {
                                setHopTimeToUI();
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+ value +" for "+ param+"."
                         + " This parameter accepts only integer. Application sets this parameter based on region value. \n");
                    }
                }

                else if(param.equalsIgnoreCase("/reader/region/hopTable"))
                {
                    if (value != null && !value.isEmpty() && isIntegerArray(value.substring(1, value.length()-1).split(",")))
                    {
                        hopTable.setText(parseHopTableValues(stringArray2IntArray(value.substring(1, value.length()-1).split(","))));
                        try
                        {
                            setHopTable();
                        }
                        catch(Exception e)
                        {
                            value = value.isEmpty() ? "empty" : value;
                            loadSaveError.append("Invalid value \n"+ value +"\n for "+ param+". "+e.getMessage()
                             +". Application sets this parameter based on region value. \n");
                            try
                            {
                                setHopTableToUI();
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+ value +" for "+ param+"."
                         + " This parameter accepts only Ineteger Array. Application sets this parameter based on region value. \n");
                    }
                }

                else if(param.equalsIgnoreCase("/reader/antenna/checkPort"))
                {
                    isLoadSaveConfiguration = true;
                    if (value.equalsIgnoreCase("true"))
                    {
                        try
                        {
                           antennaDetection.setSelected(true);
                           findAntennas(new ActionEvent());
                        }
                        catch(Exception ex)
                        {
                            
                        }
                    }
                    else if (value.equalsIgnoreCase("false"))
                    {
                        antennaDetection.selectedProperty().setValue(false);
                    }
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value "+ value +" for "+ param+"."
                         + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }

                else if(param.equalsIgnoreCase("/application/readwriteOption/Protocols"))
                {
                    String[] protocols = value.split(",");
                    for(String proto : protocols)
                    {
                        if(!supportedProtocols.contains(proto.toUpperCase()))
                        {
                            proto = proto.isEmpty() ? "empty" : proto;
                            loadSaveError.append(proto + " is not supported protocol."
                                    + " Application skips this parameter "+ param + " and sets to previous set value. \n");
                            continue;
                        }
                        if(proto.equalsIgnoreCase("gen2"))
                        {
                            gen2.selectedProperty().setValue(true);
                        }
                        else if(proto.equalsIgnoreCase("ISO18000-6B"))
                        {
                            iso18000.selectedProperty().setValue(true);
                        }
                        else if(proto.equalsIgnoreCase("ipx64"))
                        {
                            ipx64.selectedProperty().setValue(true);
                        }
                        else if(proto.equalsIgnoreCase("ipx256"))
                        {
                            ipx256.selectedProperty().setValue(true);
                        }
                        else
                        {
                            loadSaveError.append(proto + " is not valid protocol option for "+ param + "."
                                    + " Application skips this protocol \n");
                        }
                    }
                }

                else if(param.equalsIgnoreCase("/application/readwriteOption/Antennas"))
                {
                    if(!antennaDetection.isSelected())
                    {    
                        String[] antennaValue = value.split(",");
                        antenna1.setDisable(false);
                        antenna2.setDisable(false);
                        antenna3.setDisable(false);
                        antenna4.setDisable(false);
                        antenna1.selectedProperty().setValue(false);
                        antenna2.selectedProperty().setValue(false);
                        antenna3.selectedProperty().setValue(false);
                        antenna4.selectedProperty().setValue(false);
                        for (String ant : antennaValue)
                        {
                            try
                            {
                                int antenna = Integer.parseInt(ant);
                                if (!existingAntennas.contains(antenna))
                                {
                                    ant = ant.isEmpty() ? "empty" : ant;
                                    loadSaveError.append("Antenna " + ant + " is not supported on this reader."
                                            + " Application skips this antenna. \n");    
                                    continue;
                                }
                            }
                            catch(Exception ex)
                            {
                               ant = ant.isEmpty() ? "empty" : ant; 
                               loadSaveError.append("Invalid value "+ ant+"  for parameter "+ param + ". Application skips this parameter "
                                       +" and sets to previous set value. \n");
                               continue;
                            }
                            if (ant.equalsIgnoreCase("1"))
                            {
                               antenna1.selectedProperty().setValue(true);
                            } 
                            else if (ant.equalsIgnoreCase("2"))
                            {
                               antenna2.selectedProperty().setValue(true);
                            }
                            else if (ant.equalsIgnoreCase("3"))
                            {
                                antenna3.selectedProperty().setValue(true);
                            }
                            else if (ant.equalsIgnoreCase("4"))
                            {
                                antenna4.selectedProperty().setValue(true);
                            }
                            else
                            {
                              loadSaveError.append("Invalid antenna value. Supported antennas are "+ existingAntennas.toString()
                              + " Application skips "+param+" parameter. \n");
                            }
                        }
                    }
                }
                else if(param.equalsIgnoreCase("/application/readwriteOption/enableAutonomousRead"))
                {
                    if(autonomousRead.isDisabled())
                    {
                        loadSaveError.append("Autonomous read is not supported on this reader. "
                                + " Application skips "+param+" parameter. \n");
                    }
                    else
                    {
                        if(value.equalsIgnoreCase("true"))
                        {
                           autonomousRead.selectedProperty().setValue(true);
                        }
                        else if(value.equalsIgnoreCase("false"))
                        {
                           autonomousRead.selectedProperty().setValue(false); 
                        }
                        else
                        {
                            value = value.isEmpty() ? "empty" : value;
                            loadSaveError.append("Invalid value " + value + " for " + param + "."
                                    + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                        }
                    }
                    autonomousReadChangeConfiguration(new ActionEvent());
                }
                else if(param.equalsIgnoreCase("/application/readwriteOption/enableAutoReadGPI"))
                {
                    if(gpiTriggerRead.isDisabled())
                    {
                        loadSaveError.append("GPI trigger read is not supported on this reader. "
                                + " Application skips "+param+" parameter. \n");
                    }
                    else
                    {
                        if(value.equalsIgnoreCase("true"))
                        {
                            gpiTriggerRead.selectedProperty().setValue(true);
                            String pin = prop.get("/application/readwriteOption/autoReadgpiPin").toString();
                            if (pin.equalsIgnoreCase("1"))
                            {
                                autoReadGpiGroup.selectToggle(autoReadGpi1);
                            }
                            else if (pin.equalsIgnoreCase("2"))
                            {
                                autoReadGpiGroup.selectToggle(autoReadGpi2);
                            }
                            else if (pin.equalsIgnoreCase("3")  && !(readerModel.equalsIgnoreCase("M6e Micro")||readerModel.equalsIgnoreCase("M6e Micro USBPro")))
                            {
                                autoReadGpiGroup.selectToggle(autoReadGpi3);
                            }
                            else if (pin.equalsIgnoreCase("4") && !(readerModel.equalsIgnoreCase("M6e Micro")||readerModel.equalsIgnoreCase("M6e Micro USBPro")))
                            {
                                autoReadGpiGroup.selectToggle(autoReadGpi4);
                            }
                            else
                            {
                                pin = pin.isEmpty() ? "empty" : pin;
                                loadSaveError.append("Invalid GPI pin value "+ pin +" for parameter /application/readwriteOption/autoReadgpiPin."
                                        + " Applicaion sets this parameter to previous set value. \n");
                            }
                        }
                        else if(value.equalsIgnoreCase("false"))
                        {
                           gpiTriggerRead.selectedProperty().setValue(false); 
                        }
                        else
                        {
                            value = value.isEmpty() ? "empty" : value;
                            loadSaveError.append("Invalid value " + value + " for " + param + "."
                                    + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                        }
                    }
                }

                else if(param.equalsIgnoreCase("/application/displayOption/tagResultColumnSelection/enableAntenna"))
                {
                    if (value.equalsIgnoreCase("true"))
                    {
                        metaDataAntenna.selectedProperty().setValue(true);
                    } 
                    else if (value.equalsIgnoreCase("false"))
                    {
                        metaDataAntenna.selectedProperty().setValue(false);
                    } 
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/application/displayOption/tagResultColumnSelection/enableProtocol"))
                {
                    if (value.equalsIgnoreCase("true"))
                    {
                        metaDataProtocol.selectedProperty().setValue(true);
                    } 
                    else if (value.equalsIgnoreCase("false"))
                    {
                        metaDataProtocol.selectedProperty().setValue(false);
                    } 
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/application/displayOption/tagResultColumnSelection/enableFrequency"))
                {
                    if (value.equalsIgnoreCase("true"))
                    {
                        metaDataFrequency.selectedProperty().setValue(true);
                    } 
                    else if (value.equalsIgnoreCase("false"))
                    {
                        metaDataFrequency.selectedProperty().setValue(false);
                    } 
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/application/displayOption/tagResultColumnSelection/enablePhase"))
                {
                    if (value.equalsIgnoreCase("true"))
                    {
                        metaDataPhase.selectedProperty().setValue(true);
                    } 
                    else if (value.equalsIgnoreCase("false"))
                    {
                        metaDataPhase.selectedProperty().setValue(false);
                    } 
                    else
                    {
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/application/readwriteOption/enableEmbeddedReadData"))
                {
                    if (value.equalsIgnoreCase("true"))
                    {
                        embeddedReadEnable.selectedProperty().setValue(true);
                        String isUniqByData = prop.getProperty("/reader/tagReadData/uniqueByData").trim();
                        if (isUniqByData.equalsIgnoreCase("true")) 
                        {
                            embeddedReadUnique.selectedProperty().setValue(true);
                        } 
                        else if (isUniqByData.equalsIgnoreCase("false")) 
                        {
                            embeddedReadUnique.selectedProperty().setValue(false);
                        } 
                        else
                        {
                            isUniqByData = isUniqByData.isEmpty() ? "empty" : isUniqByData;
                            loadSaveError.append("Invalid value " + isUniqByData + " for /reader/tagReadData/uniqueByData."
                                    + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                        }
                        String memBank = prop.getProperty("/application/readwriteOption/enableEmbeddedReadData/MemBank").trim();
                        if(memBank.equalsIgnoreCase("EPC"))
                        {
                            embeddedMemoryBank.getSelectionModel().select("EPC");
                        }
                        else if(memBank.equalsIgnoreCase("RESERVED"))
                        {
                            embeddedMemoryBank.getSelectionModel().select("RESERVED"); 
                        }
                        else if(memBank.equalsIgnoreCase("TID"))
                        {
                           embeddedMemoryBank.getSelectionModel().select("TID");
                        }
                        else if(memBank.equalsIgnoreCase("USER"))
                        {
                           embeddedMemoryBank.getSelectionModel().select("USER");
                        }
                        else
                        {
                          memBank = memBank.isEmpty() ? "empty" : memBank;  
                          loadSaveError.append("Invalid memory bank " + memBank + " for /application/readwriteOption/enableEmbeddedReadData/MemBank."
                                    + " This parameter accepts only tag memory banks. Application sets this parameter to previous set value. \n");
                        }
                        
                       String strtAddr = prop.getProperty("/application/readwriteOption/enableEmbeddedReadData/StartAddress").trim();
                       try
                       {
                           changeListMap.put("embeddedStart", embeddedStart.getText());
                           int addr = Integer.parseInt(strtAddr);
                           embeddedStart.setText(strtAddr);
//                           addChangeList("Embedded Start previous value :" + changeListMap.get("embeddedStart") + " Now: " + embeddedStart.getText());
                           changeListMap.put("embeddedStart", embeddedStart.getText());
                       }
                       catch(Exception ex)
                       {
                           strtAddr = strtAddr.isEmpty() ? "empty" : strtAddr;
                            loadSaveError.append("Invalid start address  " + strtAddr + " for /application/readwriteOption/enableEmbeddedReadData/StartAddress."
                                    + " This parameter accepts only integer value. Application sets this parameter to previous set value. \n");
                       }
                       String endAddr = prop.getProperty("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead").trim();
                       try
                       {
                           changeListMap.put("embeddedEnd", embeddedEnd.getText());
                           int addr = Integer.parseInt(strtAddr);
                           embeddedEnd.setText(endAddr);
//                           addChangeList("Embedded End previous value: " + changeListMap.get("embeddedEnd") + " Now: " + embeddedEnd.getText());
                           changeListMap.put("embeddedEnd", embeddedEnd.getText());
                       }
                       catch(Exception ex)
                       {
                         endAddr = endAddr.isEmpty() ? "empty" : endAddr;
                         loadSaveError.append("Invalid value for length  " + endAddr + " for /application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead."
                                    + " This parameter accepts only integer value. Application sets this parameter to previous set value. \n");
                       }
                    }
                    else if (value.equalsIgnoreCase("false"))
                    {
                        embeddedReadEnable.selectedProperty().setValue(false);
                    } 
                    else
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + " This parameter accepts only true or false. Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/read/asyncOnTime"))
                {
                    try
                    {
                        int asyncOnTimeValue = Integer.parseInt(value);
                        if(asyncOnTimeValue >= 0)
                        {
                            dutyCycleOn.setText(String.valueOf(asyncOnTimeValue));
                            validateTextFields();
                        }
                    }
                    catch(Exception e)
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + "Application sets this parameter to previous set value. \n");
                    }
                }
                else if(param.equalsIgnoreCase("/reader/read/asyncOffTime"))
                {
                    try
                    {
                        int asyncOffTimeValue = Integer.parseInt(value);
                        if(asyncOffTimeValue >= 0)
                        {
                            dutyCycleOff.setText(String.valueOf(asyncOffTimeValue));
                            validateTextFields();
                        }
                    }
                    catch(Exception e)
                    {
                        value = value.isEmpty() ? "empty" : value;
                        loadSaveError.append("Invalid value " + value + " for " + param + "."
                                + "Application sets this parameter to previous set value. \n");
                    }
                }
            }
            if((prop.getProperty("/application/readwriteOption/enableAutoReadGPI").toString().equalsIgnoreCase("true"))
                    && (prop.getProperty("/application/readwriteOption/enableAutonomousRead").toString().equalsIgnoreCase("true")))
            {
                loadSaveError.append("Params /application/readwriteOption/enableAutoReadGPI and "
                        + "application/readwriteOption/enableAutonomousRead can't be enabled at the same time. "
                        + "Please select either one of them. Skipping these params");
            }
            loadSaveError.trimToSize();
        }
    }
    
    void notifyLoadSaveException(String message)
    {
        showWarningErrorMessage("warning", message);
    }
    
    @FXML
    private void saveConfig()
    {
        if(fileFilters == null)
        {
            fileFilters = new ArrayList<String>();
            fileFilters.add("*.urac");
        }
        if(loadSave == null)
        {
            loadSave = new LoadSaveConfig();
        }
        chooseFile("Select a configuration file to save reader UI configuration parameters", ".urac", fileFilters);
        String loadFileName = readerModel+"_"+ deviceName;
        fileChooser.setInitialFileName(loadFileName.replace("/", ""));
        File configFile = fileChooser.showSaveDialog(stage);
        if(configFile != null)
        {
            getParametersToSave();
            loadSave.saveConfigurations(configFile.getAbsolutePath(), saveParams);
            showWarningErrorMessage("success", "Saved reader and UI configuration parameters successfully.");
        }
    }
    
    public void getParametersToSave()
    {
        //save Region configuration
        StringBuilder sb = new StringBuilder();
        if(region.getSelectionModel().getSelectedItem() == null || "".equals(region.getSelectionModel().getSelectedItem()))
        {
            sb.append("");
        }
        else
        {
            sb.append(region.getSelectionModel().getSelectedItem().toString());
        }
        saveParams.put("/reader/region/id", sb.toString());
        
        //save Region configuration
        sb = new StringBuilder();
        if(probeBaudRate.getSelectionModel().getSelectedItem() != null || !("".equals(probeBaudRate.getSelectionModel().getSelectedItem().toString())))
        {
            sb.append(probeBaudRate.getSelectionModel().getSelectedItem().toString());
        }
        saveParams.put("/reader/baudRate", sb.toString());

        //save protocol configuration
        sb = new StringBuilder();
        if(gen2.isSelected())
        {
            sb.append(gen2.getText());
            sb.append(",");
        }
        if(iso18000.isSelected())
        {
            sb.append(iso18000.getText());
            sb.append(",");
        }
        if(ipx64.isSelected())
        {
            sb.append(ipx64.getText());
            sb.append(",");
        }
        if(ipx256.isSelected())
        {
            sb.append(ipx256.getText());
            sb.append(",");
        }
       
        if (sb.length() > 0)
        {
            sb.deleteCharAt(sb.length() - 1);
        }
      
        saveParams.put("/application/readwriteOption/Protocols", sb.toString());
        
        //save antenna configuration
         sb = new StringBuilder();
         if(antenna1.isSelected())
         {
             sb.append(antenna1.getText());
             sb.append(",");
         }
         if(antenna2.isSelected())
         {
             sb.append(antenna2.getText());
             sb.append(",");
         }
         if(antenna3.isSelected())
         {
             sb.append(antenna3.getText());
             sb.append(",");
         }
         if(antenna4.isSelected())
         {
             sb.append(antenna4.getText());
             sb.append(",");
         }
        
         if (sb.length() > 0)
        {
            sb.deleteCharAt(sb.length() - 1);
        }
      
        saveParams.put("/application/readwriteOption/Antennas", sb.toString());
        
        //antenna detection 
        if(antennaDetection.isDisabled())
        {
            // If the connected reader doesn't support antenna detection
             saveParams.put("/reader/antenna/checkPort", "false");
        }
        else
        {
           if(antennaDetection.isSelected())
           {
               saveParams.put("/reader/antenna/checkPort", "true");
           }
           else
           {
               saveParams.put("/reader/antenna/checkPort", "false");
           }
        }
        
        //Autonomous read
        if (autonomousRead.isSelected())
        {
            saveParams.put("/application/readwriteOption/enableAutonomousRead", "true");
        } 
        else
        {
            saveParams.put("/application/readwriteOption/enableAutonomousRead", "false");
        }

        //Gpi trigger read
        if (gpiTriggerRead.isSelected())
        {
            saveParams.put("/application/readwriteOption/enableAutoReadGPI", "true");
        } 
        else
        {
            saveParams.put("/application/readwriteOption/enableAutoReadGPI", "false");
        }
      
        saveParams.put("/application/readwriteOption/autoReadgpiPin", autoReadGpiGroup.getSelectedToggle().getUserData().toString());
      
        //Embeded read data
        if(embeddedReadEnable.isSelected())
        {
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData", "true");
          
            // MemBank
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData/MemBank",
                    embeddedMemoryBank.getSelectionModel().getSelectedItem().toString());

            // Start address
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData/StartAddress", 
                    embeddedStart.getText());

            // Number of words to read
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead",
                    embeddedEnd.getText());
            
            // UniqueByData
            if(embeddedReadUnique.isSelected())
            {
                saveParams.put("/reader/tagReadData/uniqueByData","true");
            }
            else
            {
                saveParams.put("/reader/tagReadData/uniqueByData","false");
            }
        }
        else
        {
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData", "false");
            
            // MemBank
            if(embeddedMemoryBank.getSelectionModel().getSelectedItem() == null)
            {
                saveParams.put("/application/readwriteOption/enableEmbeddedReadData/MemBank","");
            }
            else
            {    
                saveParams.put("/application/readwriteOption/enableEmbeddedReadData/MemBank", embeddedMemoryBank.getSelectionModel().getSelectedItem().toString());
            }

            // Start address
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData/StartAddress", 
                    "0");

            // Number of words to read
            saveParams.put("/application/readwriteOption/enableEmbeddedReadData/NoOfWordsToRead",
                    "0");
            
            //UniqueByData
            saveParams.put("/reader/tagReadData/uniqueByData","false");
        }

        //read power
        saveParams.put("/reader/radio/readPower",String.valueOf((int)(Double.parseDouble(rfRead.getText()) * 100)));

        //write power
        saveParams.put("/reader/radio/writePower",String.valueOf((int)(Double.parseDouble(rfWrite.getText()) * 100)));

        //Gen2 settings
        saveParams.put("/reader/gen2/BLF", linkFreqGroup.getSelectedToggle().getUserData().toString());
        saveParams.put("/reader/gen2/tari", tariGroup.getSelectedToggle().getUserData().toString());
        saveParams.put("/reader/gen2/tagEncoding", tagEncodeGroup.getSelectedToggle().getUserData().toString());
        saveParams.put("/reader/gen2/session", sessionGroup.getSelectedToggle().getUserData().toString());
        saveParams.put("/reader/gen2/target", targetGroup.getSelectedToggle().getUserData().toString());

        if(dynamicQ.isSelected()){
            saveParams.put("/reader/gen2/q", "DynamicQ");
        }else{
            saveParams.put("/reader/gen2/q", "StaticQ");
            saveParams.put("/application/performanceTuning/staticQValue", staticQList.getSelectionModel().getSelectedItem().toString());
        }

        // Regulatory Testing
        saveParams.put("/reader/region/hopTime", hopTime.getText());
        saveParams.put("/reader/region/hopTable", Arrays.toString(hopTable.getText().split(",")).replaceAll("\\s+", ""));

        // Tag result column selection
        if(metaDataAntenna.isSelected())
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableAntenna", "true");
        }
        else
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableAntenna", "false"); 
        }
        
        if(metaDataProtocol.isSelected())
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableProtocol", "true");
        }
        else
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableProtocol", "false"); 
        }
        
        if(metaDataFrequency.isSelected())
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableFrequency", "true");
        }
        else
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enableFrequency", "false"); 
        }
        
        if(metaDataPhase.isSelected())
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enablePhase", "true");
        }
        else
        {
           saveParams.put("/application/displayOption/tagResultColumnSelection/enablePhase", "false"); 
        }
        //Async on time
        saveParams.put("/reader/read/asyncOnTime", dutyCycleOn.getText());
        
        //Async off time
        saveParams.put("/reader/read/asyncOffTime", dutyCycleOff.getText());
    }
    
    public void chooseFile(String title, String filterDescrition, List<String> filters)
    {
        fileChooser = new FileChooser();
        fileChooser.setTitle(title);
        FileChooser.ExtensionFilter extFilter = new FileChooser.ExtensionFilter(filterDescrition, filters);
        fileChooser.getExtensionFilters().add(extFilter);
    }
    
    private Runnable dataPostThread = new Runnable()
    {
        @Override
        public void run()
        {
            try
            {
                while(isReading)
                {
                    Iterator iterator = tagData.entrySet().iterator();
                    row = FXCollections.observableArrayList();
                    while (iterator.hasNext())
                    {
                        Map.Entry entry = (Map.Entry) iterator.next();
                        String epc = (String) entry.getKey();
                        TagReadData tr = (TagReadData) tagData.get(epc);
                        if(embeddedReadEnable.isSelected())
                        {
                            row.add(new TagResults(deviceName,tr.epcString(),ReaderUtil.byteArrayToHexString(tr.data),
                                    ""+sdf.format(new Date(tr.readBase)),""+ tr.rssi,""+tr.readCount,""+ tr.antenna, ""+tr.readProtocol,""+ tr.frequency,""+ tr.phase));                            
                        }
                        else
                        {
                            row.add(new TagResults(deviceName,tr.epcString(),""+sdf.format(new Date(tr.readBase)),""+ 
                                    tr.rssi,""+tr.readCount,""+ tr.antenna, ""+tr.readProtocol,""+ tr.frequency,""+ tr.phase));
                        }
                    }
                    
                    if(!isConnected)
                    {
                        return;
                    }
                    Platform.runLater(new Runnable()
                    {
                        @Override
                        public void run()
                        {  
                            if(!isReading)
                            {
                                return;
                            }
                            tableView.getItems().clear();
                            tableView.setItems(row);
                            uniqueTagCount.setText("" + uniqueTag);
                            totalTagCount.setText("" + totalTag);
                            temperature.setText(String.valueOf(statsTemperature));
                        }
                    });
                    Thread.sleep(200);
                }
                while(isElaraReading)
                {
                    String response = receiveMessage(sp.getInputStream(), elaraTransportListener);
                    if(!isConnected)
                    {
                        return;
                    }
                    Platform.runLater(new Runnable()
                    {
                        @Override
                        public void run()
                        {  
                            if(!isElaraReading)
                            {
                                return;
                            }
                            if(response.length() > 0)
                            {
                                try
                                {
                                    String hbResponse = null;
                                    String tagResponse = null;
                                    StringBuffer hbBuffer = new StringBuffer();
                                    StringBuffer trBuffer = new StringBuffer();
                                    if(response.contains("HB") && response.contains("TagEvent"))
                                    {
                                        String[] splitResponse = response.split("\\r?\\n");
                                        for(int i=0; i < splitResponse.length; i++)
                                        {
                                            if(splitResponse[i].contains("HB"))
                                            {
                                                hbBuffer.append(splitResponse[i]+'\n');
                                            }
                                            else if(splitResponse[i].contains("TagEvent"))
                                            {
                                                trBuffer.append(splitResponse[i]+'\n');
                                            }

                                        }
                                        hbResponse = hbBuffer.toString();
                                        tagResponse = trBuffer.toString();
                                    }
                                    else if(response.contains("HB") && !response.contains("TagEvent"))
                                    {
                                        hbResponse = response;
                                        tagResponse = "";
                                    }
                                    else if(response.contains("TagEvent") && !response.contains("HB"))
                                    {
                                        tagResponse = response;
                                        hbResponse = "";
                                    }
                                    if(isValidJSON(hbResponse) && hbResponse != "" && hbResponse != null)
                                    {
                                        JSONObject json = new JSONObject(hbResponse);
                                        if(json.get("Report").equals("HB"))
                                        {
                                            temperatureElara.setText(String.valueOf(json.get("SensorTemp")));
                                        }
                                    }
                                    if(isValidJSON(tagResponse) && tagResponse != "" && tagResponse != null)
                                    {
                                        int count = ( tagResponse.split("TagEvent", -1).length ) - 1;
                                        JSONObject json = new JSONObject(tagResponse);
                                        if(json.get("Report").equals("TagEvent"))
                                        {
                                            cdcText.appendText(tagResponse);
                                            String key = json.get("EPC").toString();
                                            if(!elaraTagData.containsKey(key))
                                            {
                                                uniqueTagsElara++;
                                                elaraTagData.put(key, response);
                                                uniqueTagCountElara.setText("" + uniqueTagsElara);
                                            }
                                            totalTagsElara = totalTagsElara + count;
                                            totalTagCountElara.setText("" + totalTagsElara);
                                        }
                                    }
                                }
                                catch(JSONException e)
                                {
                                    Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, e);
                                }
                            }
                        }
                    });
                    Thread.sleep(200);
                }
            }
            catch (InterruptedException ex)
            {
                Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
            }
        }
    };
    
    public void tableViewConfiguration()
    {
        deviceIdColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("deviceId"));
        epcColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("epc"));
        timeStampColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("time"));
        rssiColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("rssi"));
        countColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("count"));
        antennaColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("antenna"));
        protocolColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("protocol"));
        frequencyColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("frequency"));
        phaseColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("phase"));
        dataColumn.setCellValueFactory(new PropertyValueFactory<TagResults, String>("data"));
                                
        tableView.widthProperty().divide(4);
        deviceIdColumn.setVisible(false);
        antennaColumn.setVisible(false);
        protocolColumn.setVisible(false);
        frequencyColumn.setVisible(false);
        phaseColumn.setVisible(false);
        dataColumn.setVisible(false);

        metaDataAntenna.selectedProperty().addListener(new ChangeListener<Boolean>()
        {
                public void changed(ObservableValue ov,Boolean old_val, Boolean new_val)
                {
                        if(metaDataAntenna.isSelected())
                        {
                            cloumnCount++;
                            antennaColumn.setVisible(true);
                            setNoColoumsInTable(cloumnCount);
                        }
                        else
                        {
                            cloumnCount--;
                            antennaColumn.setVisible(false);
                            setNoColoumsInTable(cloumnCount);
                        }
                }
            });
        metaDataProtocol.selectedProperty().addListener(new ChangeListener<Boolean>()
        {
                public void changed(ObservableValue ov,Boolean old_val, Boolean new_val)
                {
                        if(metaDataProtocol.isSelected())
                        {
                            cloumnCount++;
                            protocolColumn.setVisible(true);
                            setNoColoumsInTable(cloumnCount);
                        }
                        else
                        {
                            cloumnCount--;
                            protocolColumn.setVisible(false);
                            setNoColoumsInTable(cloumnCount);
                        }
                }
            });
         metaDataFrequency.selectedProperty().addListener(new ChangeListener<Boolean>()
         {
                public void changed(ObservableValue ov,Boolean old_val, Boolean new_val) 
                {
                        if(metaDataFrequency.isSelected())
                        {
                            cloumnCount++;
                            frequencyColumn.setVisible(true);
                            setNoColoumsInTable(cloumnCount);
                        }
                        else
                        {
                            cloumnCount--;
                            frequencyColumn.setVisible(false);
                            setNoColoumsInTable(cloumnCount);
                        }
                }
            });
         metaDataPhase.selectedProperty().addListener(new ChangeListener<Boolean>()
         {
                public void changed(ObservableValue ov,Boolean old_val, Boolean new_val)
                {
                        if(metaDataPhase.isSelected())
                        {
                            cloumnCount++;
                            phaseColumn.setVisible(true);
                            setNoColoumsInTable(cloumnCount);
                        }
                        else
                        {
                            cloumnCount--;
                            phaseColumn.setVisible(false);
                            setNoColoumsInTable(cloumnCount);
                        }
                }
            });
         embeddedReadEnable.selectedProperty().addListener(new ChangeListener<Boolean>()
         {

            @Override
            public void changed(ObservableValue<? extends Boolean> observable, Boolean oldValue, Boolean newValue)
            {
                if (embeddedReadEnable.isSelected())
                {
                    cloumnCount++;
                    dataColumn.setVisible(true);
                    setNoColoumsInTable(cloumnCount);
                }
                else
                {
                    cloumnCount--;
                    dataColumn.setVisible(false);
                    setNoColoumsInTable(cloumnCount);
                }
            }
         
         });
    }
    
    @FXML
    private void loadFirmware(ActionEvent event) 
    {
        FileChooser fileChooser = new FileChooser();
        fileChooser.setTitle("Choose firmware");
        FileChooser.ExtensionFilter extFilter;
        if(isElaraCDC)
        {
            extFilter = new FileChooser.ExtensionFilter("Firmware (*.bin)", "*.bin");
        }
        else
        {
            extFilter = new FileChooser.ExtensionFilter("Firmware (*.sim)", "*.sim");
        }
        fileChooser.getExtensionFilters().add(extFilter);
        File firmware = fileChooser.showOpenDialog(stage);
        if(firmware != null)
        {
           if(isElaraCDC)
           {
               firmwareFile = firmware.getAbsolutePath();
               if(firmwareFile.contains("elara_app"))
               {
                   hideMessagePopup();
                   selectedElaraFilePath.setText(firmwareFile);
                   updateElaraFirmware.setDisable(false);
                   updateElaraFirmware.setStyle("-fx-background-color:#28B86D");
               }
               else
               {
                   showWarningErrorMessage("warning", "The firmware file is invalid. Please select valid \"elara_app\" bin file and try again.");
               }
           }else
           {
                firmwareFile = firmware.getAbsolutePath();
                selectedFilePath.setText(firmwareFile);
                updateFirmware.setDisable(false);
                updateFirmware.setStyle("-fx-background-color:#28B86D");
           }
        }
    }

    @FXML
    private void updateFirmware(ActionEvent event)
    {
        try
        {
            hideMessagePopup();
            if(isElaraCDC)
            {
               updateElaraFirmware.setDisable(true);
               updateElaraFirmware.setStyle("-fx-background-color:#C6C6C6");
               loadElaraFirmware.setOpacity(disableOpacity);
               loadElaraFirmware.setStyle("-fx-background-color:#C6C6C6");
               loadElaraFirmware.setDisable(true);
               progressBarElara.visibleProperty().setValue(true);
               progressTask = createProgress();
               progressBarElara.progressProperty().unbind();
               progressBarElara.progressProperty().bind(progressTask.progressProperty());
               connectTab.setDisable(true);
               configureTabElara.setDisable(true);
               resultsTabElaraCDC.setDisable(true);
               resultsTabElaraHID.setDisable(true);
               btGetStarted.setDisable(true);
               updatePercent.setText("Starting update...");
               updatePercent.setVisible(true);
               new Thread(updateFirmwareThread).start();
            }
            else
            {
                updateFirmware.setDisable(true);
                updateFirmware.setStyle("-fx-background-color:#C6C6C6");
                loadFirmware.setOpacity(disableOpacity);
                loadFirmware.setStyle("-fx-background-color:#C6C6C6");
                loadFirmware.setDisable(true);
                loadConfigButton.setDisable(true);
                saveConfigButton.setDisable(true);
                fi = new FileInputStream(firmwareFile);
                progressBar.visibleProperty().setValue(true);
                progressTask = createProgress();
                progressBar.progressProperty().unbind();
                progressBar.progressProperty().bind(progressTask.progressProperty());
                applyButton.setDisable(true);
                revertButton.setDisable(true);
                connectTab.setDisable(true);
                btGetStarted.setDisable(true);
                updatePercent.setVisible(false);
                new Thread(updateFirmwareThread).start();
            }
        }
        catch(Exception ex)
        {
            showWarningErrorMessage("error", ex.getMessage());
            stopFirmwareProgress();
        }
    }

    private Runnable updateFirmwareThread = new Runnable()
    {
        @Override
        public void run()
        {
            try
            {
                if(isElaraCDC)
                {
                    configureTabElara.setDisable(true);
                    firmwareUpdateSuccess = elaraFirmwareUpgrade();
                    if(firmwareUpdateSuccess)
                    {
                        stopElaraFirmwareProgress();
                        new Thread(disConnectThread).start();
                        showWarningErrorMessage("success", "Firmware update successful, please reload devices and reconnect to the reader");
                    }
                    else
                    {
                        if(inBootLoaderMode)
                        {
                            stopBootElaraFirmwareProgress();
                            showWarningErrorMessage("error", "Firmware update failed. Please check if device is connected and try updating again.");
                        }
                        else
                        {
                            stopElaraFirmwareProgress();
                            new Thread(disConnectThread).start();
                            showWarningErrorMessage("error", "Firmware update failed, please reconnect to the reader");
                        }
                    }
                }
                else
                {
                    if(r != null)
                    {
                        r.destroy();
                        r = null;
                    }
                    String deviceNameString = deviceName.substring(deviceName.indexOf("(") + 1, deviceName.indexOf(")"));
                    r = Reader.create("tmr:///" + deviceNameString);
                    try
                    {
                      r.connect();
                    }
                    catch(ReaderException re)
                    {
                        if(re.getMessage().equalsIgnoreCase(Constants.APPLICATION_IMAGE_FAILED))
                        {
                            // do nothing when we got this error and update the firmware
                            revertButton.setDisable(true);
                        }
                        else
                        {
                            throw re;
                        }
                    }
                    r.firmwareLoad(fi);
                    stopFirmwareProgress();
                    fi.close();
                    new Thread(disConnectThread).start();
                    showWarningErrorMessage("success", "Firmware updated successfully, please reconnect to the reader");
                }
            }
            catch (Exception ex)
            {
                setTitledPanesStatus(false, false, false, false, false, false,false);
                setTitledPanesExpandedStatus(false, false, false, false, false, false, true);
                String errorMessage = ex.getMessage();
                if(isElaraCDC)
                {
                    loadElaraFirmware.setDisable(false);
                    loadElaraFirmware.setOpacity(buttonEnableOpacity);
                    loadElaraFirmware.setStyle("-fx-background-color:#28B86D");
                    progressTask.cancel(true);
                    progressBarElara.progressProperty().unbind();
                    progressBarElara.visibleProperty().setValue(false);
                    //connectTab.setDisable(false);
                    updateElaraFirmware.setDisable(false);
                    updateElaraFirmware.setOpacity(buttonEnableOpacity);
                    updateElaraFirmware.setStyle("-fx-background-color:#28B86D");
                    btGetStarted.setDisable(false);
                    updatePercent.setText("");
                    updatePercent.setVisible(false);
                    reloadDevicesButton.setDisable(false);
                    showWarningErrorMessage("error", "Firmware update failed. Please check if device is connected and try updating again.");
                }
                else
                {
                    if(ex instanceof IndexOutOfBoundsException)
                    {
                       errorMessage = "Invalid firmware file.";
                       stopFirmwareProgress();
                    }
                    else if(errorMessage.equalsIgnoreCase("Firmware Update is successful. Autonomous mode is already enabled on reader"))
                    {
                        try
                        {
                            stopFirmwareProgress();
                            fi.close();
                            new Thread(disConnectThread).start();
                        }
                        catch(Exception e)
                        {

                        }
                        showWarningErrorMessage("warning", errorMessage);
                        return;
                    }
                    stopFirmwareProgress();
                    showWarningErrorMessage("error", errorMessage);
                }
            }
            return;
        }
    };
    public boolean elaraFirmwareUpgrade(){
        if(!inBootLoaderMode)
        {
            checkThread.interrupt();
            try {
                Thread.sleep(500);
            } catch (InterruptedException ex) {
                Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
            }
        }
        boolean updateSuccessful = false;
        int savedBaudrate;
        SerialPort spFWUpgrade = null;
        try {
            if(inBootLoaderMode)
            {
                inBootFailed=false;
                String elaraFWDevicePort = null;
                if(sp.isOpen() && !elaraRS232)
                {
                    sp.closePort();
                }
                Thread.sleep(4000);
                if(!elaraRS232)
                {
                    SerialPort[] portList = SerialPort.getCommPorts();
                    String[] results = new String[portList.length];
                    for (int i = 0; i < portList.length; i++)
                    {
                        results[i] = portList[i].getSystemPortName();
                        if(portList[i].getPortDescription().equalsIgnoreCase("Elara Reader"))
                        {
                            String deviceName = portList[i].getDescriptivePortName();
                            elaraFWDevicePort = deviceName.substring(deviceName.indexOf("(") + 1, deviceName.indexOf(")"));
                        }
                    }
                    spFWUpgrade = SerialPort.getCommPort(elaraFWDevicePort);
                    spFWUpgrade.setComPortParameters(115200, 8, 1, 0);
                }
                else
                {
                    StringBuffer baudBuffer = new StringBuffer();
                    baudBuffer.append("115200");
                    String setBaudRateCommand = ejsonp.customJSONCommand(ECTConstants.SET_CFG_BAUDRATE, baudBuffer);
                    String responseReceived = sendMessage(sp, setBaudRateCommand, elaraTransportListener);
                    sp.setBaudRate(115200);
                    spFWUpgrade = sp;
                }
                if(elaraRS232)
                {
                    Thread.sleep(2000);
                }
                if(spFWUpgrade.openPort(3000))
                {
                    int max_chunksize=1024;
                    InputStream in = spFWUpgrade.getInputStream();
                    OutputStream os = spFWUpgrade.getOutputStream();
                    if(streamUpdatefailed){
                        if(in.available() > 0)
                        {
                            BufferedReader bfr = new BufferedReader(new InputStreamReader(in));
                            String line = bfr.readLine();
                        }
                    }
                    String updateCommand = ejsonp.formJSONCommand(ECTConstants.START_UPDATE);
                    String startUpdateResponse = sendMessage(spFWUpgrade, updateCommand, elaraTransportListener);
                    InputStream inputStream = new FileInputStream(firmwareFile);
                    byte[] b = new byte[max_chunksize];
                    int totalBytes = inputStream.available();
                    int count =0;
                    int iter = totalBytes/max_chunksize;
                    long inStreamStartTime=0,inStreamEndTime=0;
                    percentComplete=0.00;
                    StringBuffer inputStreamResult = new StringBuffer();
                    while(inputStream.read(b)!= -1)
                    {
                        boolean inAvailable = false;
                        JSONObject obj = new JSONObject();
                        obj.put("Cmd","SendData");
                        obj.put("Data",Base64.getEncoder().encodeToString(b));
                        StringBuilder sb = new StringBuilder();
                        sb.append(obj.toString());
                        sb.append("\n");
                        OutputStreamWriter osw = new OutputStreamWriter(os);
                        osw.write(sb.toString());
                        osw.flush();
                        inStreamStartTime = System.currentTimeMillis();
                        while(!inAvailable)
                        {
                            if(elaraRS232)
                            {
                                Thread.sleep(25);
                            }
                            inStreamEndTime = System.currentTimeMillis();
                            int numbytes = in.available();
                            byte[] inputByte = new byte[numbytes];
                            in.read(inputByte);
                            String result = new String(inputByte);
                            inputStreamResult.append(result);
                            if(isValidJSON(inputStreamResult.toString()))
                            {
                                inAvailable = true;
                            }
                            long elapsedTime = inStreamEndTime - inStreamStartTime;
                            if(elapsedTime == 3000)
                            {   
                                showWarningErrorMessage("warning", "Please wait...");
                            }
                            if(elapsedTime > 5000)
                            {   
                                streamUpdatefailed = true;
                                break;
                            }
                        }
                        String line = inputStreamResult.toString();
                        JSONObject jsono = new JSONObject(line);
                        int err = (Integer)jsono.get("ErrID");
                        percentComplete = ((double)count/iter)*100;
                        Platform.runLater(new Runnable() {
                            @Override
                            public void run() {
                                updatePercent.setText("Firmware update in progress "+String.format("%.1f", percentComplete)+"%");
                            }
                        });
                        count++;
                        if(err!=0){
                            updateSuccessful=false;
                            break;
                        }else{
                            updateSuccessful=true;
                            inputStreamResult.delete(0, inputStreamResult.length());
                        }
                    }
                    inputStream.close();
                    in.close();
                    os.close();
                }
                else
                {
                    inBootFailed = true;
                    stopBootElaraFirmwareProgress();
                }
                if(!inBootFailed)
                {
                    String endUpdateCmd = ejsonp.formJSONCommand(ECTConstants.END_UPDATE);
                    sendMessage(spFWUpgrade, endUpdateCmd, elaraTransportListener);
                    String rebootCmd = ejsonp.formJSONCommand(ECTConstants.REBOOT);
                    sendMessage(spFWUpgrade, rebootCmd, elaraTransportListener);
                    if(elaraRS232)
                    {
                        Thread.sleep(1000);
                        if(elaraChangeListMap.get("SerCfg") == null)
                        {
                            savedBaudrate = 115200;
                        }
                        else
                        {
                            savedBaudrate = Integer.parseInt(elaraChangeListMap.get("SerCfg"));
                        }
                        StringBuffer baudBuffer = new StringBuffer();
                        baudBuffer.append(savedBaudrate);
                        String setBaudRateCommand = ejsonp.customJSONCommand(ECTConstants.SET_CFG_BAUDRATE, baudBuffer);
                        String responseReceived = sendMessage(spFWUpgrade, setBaudRateCommand, elaraTransportListener);
                    }
                }
                spFWUpgrade.closePort();
            }
            else
            {
                inBootFailed=false;
                String elaraFWDevicePort = null;
                ElaraJSONParser ejsonp = new ElaraJSONParser();
                String command = ejsonp.formJSONCommand(ECTConstants.ACTIVATE_UPDATE_MODE);
                sendMessage(sp, command, elaraTransportListener);
                if(sp.isOpen() && !elaraRS232)
                {
                    sp.closePort();
                }
                Thread.sleep(4000);
                if(!elaraRS232)
                {
                    SerialPort[] portList = SerialPort.getCommPorts();
                    String[] results = new String[portList.length];
                    for (int i = 0; i < portList.length; i++)
                    {
                        results[i] = portList[i].getSystemPortName();
                        if(portList[i].getPortDescription().equalsIgnoreCase("Elara Reader"))
                        {
                            String deviceName = portList[i].getDescriptivePortName();
                            elaraFWDevicePort = deviceName.substring(deviceName.indexOf("(") + 1, deviceName.indexOf(")"));
                        }
                    }
                    spFWUpgrade = SerialPort.getCommPort(elaraFWDevicePort);
                    spFWUpgrade.setComPortParameters(115200, 8, 1, 0);
                }
                else
                {
                    StringBuffer baudBuffer = new StringBuffer();
                    baudBuffer.append("115200");
                    String setBaudRateCommand = ejsonp.customJSONCommand(ECTConstants.SET_CFG_BAUDRATE, baudBuffer);
                    String responseReceived = sendMessage(sp, setBaudRateCommand, elaraTransportListener);
                    sp.setBaudRate(115200);
                    spFWUpgrade = sp;
                }
                if (spFWUpgrade.openPort(3000))
                {
                    int max_chunksize=1024;
                    InputStream in = spFWUpgrade.getInputStream();
                    OutputStream os = spFWUpgrade.getOutputStream();
                    if(elaraRS232)
                    {
                        Thread.sleep(2000);
                    }
                    String getBootInfo = ejsonp.formJSONCommand(ECTConstants.GET_INFO_FIELDS_ALL);
                    String getBootInfoResponse = sendMessage(spFWUpgrade, getBootInfo, elaraTransportListener);
                    inBootLoaderMode = ejsonp.parseBootLoaderResponse(getBootInfoResponse);
                    String updateCommand = ejsonp.formJSONCommand(ECTConstants.START_UPDATE);
                    String startUpdateResponse = sendMessage(spFWUpgrade, updateCommand, elaraTransportListener);
                    InputStream inputStream = new FileInputStream(firmwareFile);
                    byte[] b = new byte[max_chunksize];
                    int totalBytes = inputStream.available();
                    int count =0;
                    long inStreamStartTime=0,inStreamEndTime=0;
                    int iter = totalBytes/max_chunksize;
                    percentComplete=0.00;
                    StringBuffer inputStreamResult = new StringBuffer();
                    while(inputStream.read(b)!= -1)
                    {
                        boolean inAvailable = false;
                        JSONObject obj = new JSONObject();
                        obj.put("Cmd","SendData");
                        obj.put("Data",Base64.getEncoder().encodeToString(b));
                        StringBuilder sb = new StringBuilder();
                        sb.append(obj.toString());
                        sb.append("\n");
                        OutputStreamWriter osw = new OutputStreamWriter(os);
                        osw.write(sb.toString());
                        osw.flush();
                        inStreamStartTime = System.currentTimeMillis();
                        while(!inAvailable)
                        {
                            if(elaraRS232)
                            {
                                Thread.sleep(25);
                            }
                            inStreamEndTime = System.currentTimeMillis();
                            int numbytes = in.available();
                            byte[] inputByte = new byte[numbytes];
                            in.read(inputByte);
                            String result = new String(inputByte);
                            inputStreamResult.append(result);
                            if(isValidJSON(inputStreamResult.toString()))
                            {
                                inAvailable = true;
                            }
                            long elapsedTime = inStreamEndTime - inStreamStartTime;
                            if(elapsedTime==3000)
                            {
                                showWarningErrorMessage("warning", "Please wait...");
                            }
                            if(elapsedTime>5000)
                            {
                                streamUpdatefailed = true;
                                break;
                            }
                        }
                        String line = inputStreamResult.toString();
                        JSONObject jsono = new JSONObject(line);
                        int err = (Integer)jsono.get("ErrID");
                        percentComplete = ((double)count/iter)*100;
                        Platform.runLater(new Runnable() {
                            @Override
                            public void run() {
                                updatePercent.setText("Firmware update in progress "+String.format("%.1f", percentComplete)+"%");
                            }
                        });
                        count++;
                        if(err!=0){
                            updateSuccessful=false;
                            break;
                        }else{
                            updateSuccessful=true;
                            inputStreamResult.delete(0, inputStreamResult.length());
                        }
                    }
                    inputStream.close();
                    in.close();
                    os.close();
                }
                else
                {
                    inBootFailed = true;
                    stopBootElaraFirmwareProgress();
                }
                if(!inBootFailed)
                {
                    String endUpdateCmd = ejsonp.formJSONCommand(ECTConstants.END_UPDATE);
                    sendMessage(spFWUpgrade, endUpdateCmd, elaraTransportListener);
                    String rebootCmd = ejsonp.formJSONCommand(ECTConstants.REBOOT);
                    sendMessage(spFWUpgrade, rebootCmd, elaraTransportListener);
                    if(elaraRS232)
                    {
                        Thread.sleep(1000);
                        if(elaraChangeListMap.get("SerCfg") == null)
                        {
                            savedBaudrate = 115200;
                        }
                        else
                        {
                            savedBaudrate = Integer.parseInt(elaraChangeListMap.get("SerCfg"));
                        }
                        StringBuffer baudBuffer = new StringBuffer();
                        baudBuffer.append(savedBaudrate);
                        String setBaudRateCommand = ejsonp.customJSONCommand(ECTConstants.SET_CFG_BAUDRATE, baudBuffer);
                        String responseReceived = sendMessage(spFWUpgrade, setBaudRateCommand, elaraTransportListener);
                    }
                }
                spFWUpgrade.closePort();
            }
        } catch (FileNotFoundException ex) {
            spFWUpgrade.closePort();
            updateSuccessful=false;
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        } catch (IOException | JSONException ex) {
            spFWUpgrade.closePort();
            updateSuccessful=false;
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        } catch (InterruptedException ex) {
            spFWUpgrade.closePort();
            updateSuccessful=false;
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        } catch (Exception ex) {
            spFWUpgrade.closePort();
            updateSuccessful=false;
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
        return updateSuccessful;
   } 
   
   public void stopFirmwareProgress()
    {
        Platform.runLater(new Runnable()
        {
            public void run()
            {
                loadFirmware.setDisable(false);
                loadFirmware.setOpacity(buttonEnableOpacity);
                loadFirmware.setStyle("-fx-background-color:#28B86D");
                progressTask.cancel(true);
                progressBar.progressProperty().unbind();
                progressBar.setProgress(0);
                progressBar.visibleProperty().setValue(false);
                selectedFilePath.clear();
                loadConfigButton.setDisable(false);
                saveConfigButton.setDisable(false);
                connectTab.setDisable(false);
                btGetStarted.setDisable(false);
            }
        });
    }
   
   public void stopElaraFirmwareProgress()
    {
        Platform.runLater(new Runnable()
        {
            public void run()
            {
                loadElaraFirmware.setDisable(false);
                loadElaraFirmware.setOpacity(buttonEnableOpacity);
                loadElaraFirmware.setStyle("-fx-background-color:#28B86D");
                progressTask.cancel(true);
                progressBarElara.progressProperty().unbind();
                progressBarElara.setProgress(0);
                progressBarElara.visibleProperty().setValue(false);
                selectedElaraFilePath.clear();
                connectTab.setDisable(false);
                configureTabElara.setDisable(false);
                resultsTabElaraCDC.setDisable(false);
                resultsTabElaraHID.setDisable(false);
                btGetStarted.setDisable(false);
                updatePercent.setText("");
                updatePercent.setVisible(false);
                inBootLoaderMode = false;
                streamUpdatefailed = false;
                reloadDevicesButton.setDisable(false);
                reloadDevices();
                firmwareUpdateSuccess=false;
            }
        });
    }
   
    public void stopBootElaraFirmwareProgress()
    {
        Platform.runLater(new Runnable()
        {
            public void run()
            {
                loadElaraFirmware.setDisable(false);
                loadElaraFirmware.setOpacity(buttonEnableOpacity);
                loadElaraFirmware.setStyle("-fx-background-color:#28B86D");
                updateElaraFirmware.setDisable(false);
                updateElaraFirmware.setOpacity(buttonEnableOpacity);
                updateElaraFirmware.setStyle("-fx-background-color:#28B86D");
                progressTask.cancel(true);
                progressBarElara.progressProperty().unbind();
                progressBarElara.visibleProperty().setValue(false);
                updatePercent.setText("");
                updatePercent.setVisible(false);
                reloadDevicesButton.setDisable(false);
            }
        });
    }
   
    public void setNoColoumsInTable(int cloumnCount)
    {
        epcColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        timeStampColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        rssiColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        countColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount)); 
        deviceIdColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        
        if(metaDataAntenna.isSelected())
        {
            antennaColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        }
        if(metaDataProtocol.isSelected())
        {
            protocolColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        }
        if(metaDataFrequency.isSelected())
        {
            frequencyColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        }
        if(metaDataPhase.isSelected())
        {
            phaseColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        }        
        if(embeddedReadEnable.isSelected())
        {
            dataColumn.prefWidthProperty().bind(tableView.widthProperty().divide(cloumnCount));
        }
    }

    public void setTooltip()
    {
        ToolTipManager.sharedInstance().setInitialDelay(10);
        ToolTipManager.sharedInstance().setReshowDelay(10);
        ToolTipManager.sharedInstance().setDismissDelay(5000);

        Tooltip tpHome = new Tooltip("Home");
        tpHome.getStyleClass().add("toolTip");
        homeTab.setTooltip(tpHome);

        Tooltip tpConnect = new Tooltip("Connect");
        tpConnect.getStyleClass().add("toolTip");
        connectTab.setTooltip(tpConnect);

        Tooltip tpConfigureTab = new Tooltip("Configure");
        tpConfigureTab.getStyleClass().add("toolTip");
        configureTab.setTooltip(tpConfigureTab);

        Tooltip tpConfigureTabElara = new Tooltip("Configure");
        tpConfigureTabElara.getStyleClass().add("toolTip");
        configureTabElara.setTooltip(tpConfigureTabElara);

        Tooltip tpReadTab = new Tooltip("Read");
        tpReadTab.getStyleClass().add("toolTip");
        readTab.setTooltip(tpReadTab);

        Tooltip tpResultsTab = new Tooltip("Results");
        tpResultsTab.getStyleClass().add("toolTip");
        resultsTabElaraHID.setTooltip(tpResultsTab);
        resultsTabElaraCDC.setTooltip(tpResultsTab);

        Tooltip tpUtilitiesTab = new Tooltip("Utilities");
        tpUtilitiesTab.getStyleClass().add("toolTip");
        utilsTabElara.setTooltip(tpUtilitiesTab);

        Tooltip tpHelpTab = new Tooltip("Help");
        tpHelpTab.getStyleClass().add("toolTip");
        helpTab.setTooltip(tpHelpTab);

        Tooltip tpReloadDevices = new Tooltip("Reload Devices");
        tpReloadDevices.getStyleClass().add("toolTip");
        reloadDevicesButton.setTooltip(tpReloadDevices);

        Tooltip tpAntennaDetection = new Tooltip("Enable antenna detection");
        tpAntennaDetection.getStyleClass().add("toolTip");
        antennaDetection.setTooltip(tpAntennaDetection);

        Tooltip tptagResultColumn = new Tooltip("Select column to display on tag results.");
        tptagResultColumn.getStyleClass().add("toolTip");

        metaDataAntenna.setTooltip(tptagResultColumn);
        metaDataProtocol.setTooltip(tptagResultColumn);    
        metaDataFrequency.setTooltip(tptagResultColumn);   
        metaDataPhase.setTooltip(tptagResultColumn);
        
        Tooltip toolTip = new Tooltip("Click to save the configurations.");
        toolTip.getStyleClass().add("toolTip");
        applyButton.setTooltip(toolTip);
        
        toolTip = new Tooltip("Click to load the configuration file.");
        toolTip.getStyleClass().add("toolTip");
        loadConfigButton.setTooltip(toolTip);
        
        toolTip = new Tooltip("Click to save the configurations to file.");
        toolTip.getStyleClass().add("toolTip");
        saveConfigButton.setTooltip(toolTip);
        
        toolTip = new Tooltip("Click to choose the firmware.");
        toolTip.getStyleClass().add("toolTip");
        loadFirmware.setTooltip(toolTip);
        
        toolTip = new Tooltip("Click to update the firmware.");
        toolTip.getStyleClass().add("toolTip");
        updateFirmware.setTooltip(toolTip);
        
        toolTip = new Tooltip("Click to revert to module default settings.");
        toolTip.getStyleClass().add("toolTip");
        revertButton.setTooltip(toolTip);
        
        toolTip = new Tooltip("Apply settings temporarily - applicable until reset");
        toolTip.getStyleClass().add("toolTip");
        setButtonElara.setTooltip(toolTip);
        
        toolTip = new Tooltip("Apply and save settings persistently - applicable even after reset");
        toolTip.getStyleClass().add("toolTip");
        saveButtonElara.setTooltip(toolTip);
        
        toolTip = new Tooltip("Apply persistently saved settings - applicable until reset");
        toolTip.getStyleClass().add("toolTip");
        revertButtonElara.setTooltip(toolTip);
        
        toolTip = new Tooltip("Apply factory recommended settings temporarily - applicable until reset");
        toolTip.getStyleClass().add("toolTip");
        loadDefaultButtonElara.setTooltip(toolTip);
        
        toolTip = new Tooltip("Get reader date and time");
        toolTip.getStyleClass().add("toolTip");
        getDateTimeElara.setTooltip(toolTip);
        
        toolTip = new Tooltip("Set PC date and time to reader");
        toolTip.getStyleClass().add("toolTip");
        applyDateTimeButton.setTooltip(toolTip);
        
        toolTip = new Tooltip("Start/Stop tag reads");
        toolTip.getStyleClass().add("toolTip");
        toggleButtonStart.setTooltip(toolTip);
        
        toolTip = new Tooltip("Browse and select .bin file");
        toolTip.getStyleClass().add("toolTip");
        loadElaraFirmware.setTooltip(toolTip);
        
        toolTip = new Tooltip("Start firmware update");
        toolTip.getStyleClass().add("toolTip");
        updateElaraFirmware.setTooltip(toolTip);
        
        toolTip = new Tooltip("Connect to/disconnect from reader.");
        toolTip.getStyleClass().add("toolTip");
        connectionButton.setTooltip(toolTip);
    }

    //Read Data From Reader
    public void read()
    {
        // Create and add tag listener
        readListener = new PrintListener();
        exceptionListener = new TagReadExceptionReceiver();
        statsListener = new ReaderStatsListener();
        r.addStatsListener(statsListener);
        r.addReadListener(readListener);
        r.addReadExceptionListener(exceptionListener);
    }

    class PrintListener implements ReadListener
    {
        @Override
        public void tagRead(Reader r, TagReadData tr)
        {
            try
            {
                String tagEpc = tr.tag.epcString();
                String key = tagEpc;
                int count = tr.getReadCount();
                if(embeddedReadUnique.isSelected())
                {
                    if(tr.data.length > 0)
                    {
                       key += ""+ReaderUtil.byteArrayToHexString(tr.data);
                    }
                    else
                    {
                        return;
                    }
                }
                
                totalTag+=count;
                if(tagData.containsKey(key))
                {
                    TagReadData trd = (TagReadData)tagData.get(key);
                    trd.readBase = tr.readBase;
                    trd.rssi = tr.rssi;
                    trd.readCount += count;
                    trd.antenna = tr.antenna;
                    trd.readProtocol = tr.readProtocol;
                    trd.frequency = tr.frequency;
                    trd.phase = tr.phase;
                    trd.data = tr.data;
                }
                else
                {
                    uniqueTag++;
                    tagData.put(key, tr);
                }
            }
            catch (Exception e)
            {
            }
        }
    }
    
    class  TagReadExceptionReceiver implements ReadExceptionListener
    {
        @Override
        public void tagReadException(Reader r, ReaderException re)
        {
           if(false 
                   || re.getMessage() == null  
                   || re.getMessage().contains("Invalid argument")
                   || re.getMessage().contains("Timeout"))
           {
                   new Thread(disConnectThread).start();
                   showWarningErrorMessage("error", "Connection lost");
           }
           else if(!isConnected && re.getMessage().contains("Failed to connect with baudrate"))
           {
              showWarningErrorMessage("warning", re.getMessage());
           }
           else
           {

           }
        }
    }
   
    public Task createProgress()
    {
        return new Task()
        {
            @Override
            protected Object call() throws Exception
            {
                for (int i = 0; i < 10; i++)
                {
                    Thread.sleep(2000);
                    updateMessage("2000 milliseconds");
                    updateProgress(i + 1, 10);
                }
                return true;
            }
        };
    }

   public void disableModuleUnsupportedFeatures()
    {
        try
        {
            if (readerModel.contains("M5e") || readerModel.equalsIgnoreCase("M6e Nano"))
            {
                link640Khz.setDisable(true);
                link640Khz.setOpacity(disableOpacity);
                fm0.setDisable(true);
                fm0.setOpacity(disableOpacity);
                tari6_25us.setDisable(true);
                tari6_25us.setOpacity(disableOpacity);
                tari12_5us.setDisable(true);
                tari12_5us.setOpacity(disableOpacity);
            }
            else
            {
                link640Khz.setDisable(false);
                link640Khz.setOpacity(enableOpacity);
                fm0.setDisable(false);
                fm0.setOpacity(enableOpacity);
                tari6_25us.setDisable(false);
                tari6_25us.setOpacity(enableOpacity);
                tari12_5us.setDisable(false);
                tari12_5us.setOpacity(enableOpacity);
            }

            if(!isAutonomousSupported )
            {
                autonomousRead.disableProperty().setValue(true);
                autonomousRead.setOpacity(disableOpacity);
                gpiTriggerRead.disableProperty().setValue(true);
                gpiTriggerRead.setOpacity(disableOpacity);
                showWarningErrorMessage("warning", "Autonomous operation is not supported on current firmware");
            }
            else
            {
               autonomousRead.disableProperty().setValue(false);
               autonomousRead.setOpacity(enableOpacity);
               gpiTriggerRead.disableProperty().setValue(false);
               gpiTriggerRead.setOpacity(enableOpacity); 
            }
            
            if (readerModel.equalsIgnoreCase("M6e Micro") || readerModel.equalsIgnoreCase("M6e Micro USB")
                    || readerModel.equalsIgnoreCase(Constants.M6E_MICRO_USB_PRO))
            {
                autoReadGpi3.setVisible(false);
                autoReadGpi4.setVisible(false);
            }
            else
            {
                autoReadGpi3.setVisible(true);
                autoReadGpi4.setVisible(true);
            }

            ipx256.setVisible(false);
            ipx64.setVisible(false);
            iso18000.setVisible(false);

            TagProtocol[] protocols = (TagProtocol[]) r.paramGet("/reader/version/supportedProtocols");
            TagProtocol currentProto =(TagProtocol) r.paramGet("/reader/tagop/protocol");
            supportedProtocols = new ArrayList<String>();
            supportedProtocols.add("GEN2");
        }
        catch (ReaderException ex)
        {
            
        }
    }
   
   public void setHomeContent()
   {
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/Home.html");
       webEngine.load(url.toExternalForm());
       homeBorderPane.setCenter(browser);
   }

   @FXML
   private void getStarted(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(connectTab);
   }
   
   @FXML
   private void showAboutInfo(MouseEvent event)
   {
        showAboutInfo();
   }
   
   private void showAboutInfo()
   {
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/About.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }
   
   @FXML
   private void showSupportedDevicesInfo(MouseEvent event)
   {
       helpBorderPane.getChildren().clear();
       mainTabs.getSelectionModel().select(helpTab);
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/SupportedDevices.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }
   
   @FXML
   private void showHelpInfo(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(helpTab);
   }
   
   @FXML
   private void showConnectInfo(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(helpTab);
       helpBorderPane.getChildren().clear();
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/Connect.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }
   
    @FXML
    private void showConfigureInfo(MouseEvent event)
    {
        mainTabs.getSelectionModel().select(helpTab);
        helpBorderPane.getChildren().clear();
        WebView browser = new WebView();
        WebEngine webEngine = browser.getEngine();
        URL url = getClass().getResource("/fxml/Configure.html");
        webEngine.load(url.toExternalForm());
        helpBorderPane.setCenter(browser);
        helpBorderPane.setVisible(true);
    }
   
   @FXML
   private void showReadInfo(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(helpTab);
       helpBorderPane.getChildren().clear();
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/Read.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }
   
   @FXML
   private void showUpdateFirmwareInfo(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(helpTab);
       helpBorderPane.getChildren().clear();
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/Utilities.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }

   @FXML
   private void showReadTab(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(readTab);
   }

   @FXML
   private void revertToDefaultSettings(ActionEvent event)
   {
       try
       {
           int option = JOptionPane.showConfirmDialog(null, "Do you want to revert the module settings to factory defaults?", "Confirmation", JOptionPane.YES_NO_OPTION);
           if (option == JOptionPane.YES_OPTION)
           {
               clearConfiguationToolParameter();
               r.paramSet("/reader/userConfig", new SerialReader.UserConfigOp(SerialReader.SetUserProfileOption.CLEAR));
           }
           else
           {
               return;
           }
           initParams(r, changeListMap);
           showWarningErrorMessage("sucessfull", "Sucessfully reverted to default settings");
       }
       catch (Exception ex)
       {
           showWarningErrorMessage("error", "Failed to revert settings to default values due to "+ex.getMessage());
       }
   }

   private void clearConfiguationToolParameter() throws Exception
   {
       //r.paramSet("/reader/antenna/checkPort", false);
       antennaDetection.setSelected(false);
       configureAntennaBoxes(r);
       gpiTriggerRead.setSelected(false);
       embeddedReadEnable.setSelected(false);
       disableEmbeddedReadData();
       gpiTriggerRead.setSelected(false);
       changeUIOnGpiTriggerRead(true, disableOpacity);
       autonomousRead.setSelected(false);
       metaDataAntenna.setSelected(false);
       metaDataPhase.setSelected(false);
       metaDataProtocol.setSelected(false);
       metaDataFrequency.setSelected(false);
       selectedFilePath.clear();
       hopTime.clear();
       hopTable.clear();
   }

   @FXML
   private void showTroubleShootInfo(MouseEvent event)
   {
       mainTabs.getSelectionModel().select(helpTab);
       helpBorderPane.getChildren().clear();
       WebView browser = new WebView();
       WebEngine webEngine = browser.getEngine();
       URL url = getClass().getResource("/fxml/Troubleshoot.html");
       webEngine.load(url.toExternalForm());
       helpBorderPane.setCenter(browser);
       helpBorderPane.setVisible(true);
   }
   
   @FXML
   private void clearTags(ActionEvent event)
   {
        tagData.clear();
        tableView.getItems().clear();
        uniqueTag = 0;
        totalTag = 0;
        uniqueTagCount.setText(""+0);
        totalTagCount.setText(""+0); 
        temperature.setText("");
        statsTemperature = 0;

        // Elara configurations clear
        cdcText.clear();
        elaraTagData.clear();
        uniqueTagsElara = 0;
        totalTagsElara = 0;
        uniqueTagCountElara.setText(""+0);
        totalTagCountElara.setText(""+0);
        temperatureElara.setText("");
   }
   
   class ReaderStatsListener implements StatsListener
    {
      public void statsRead(SerialReader.ReaderStats readerStats)
      {
         if(statsTemperature != readerStats.temperature)
         {
            statsTemperature = readerStats.temperature;
         }
      }
    }

   private void isSupportsAutonomus(String versionsoftware)
   {       
       try
       {
           String[] version = versionsoftware.split("-")[0].split("\\.");
           int part1 = Integer.parseInt(version[0], 16);
           int part2 = Integer.parseInt(version[1], 16);
           int part3 = Integer.parseInt(version[2], 16);
           int part4 = Integer.parseInt(version[3], 16);
           SerialReader.VersionNumber vNumber = new SerialReader.VersionNumber(part1, part2, part3, part4);
           if(false 
               || ((readerModel.equalsIgnoreCase(Constants.M6E) || readerModel.equalsIgnoreCase(Constants.M6E_I_PRC) && vNumber.compareTo(new SerialReader.VersionNumber(1, 23, 0, 1)) >= 0)
               || (readerModel.equalsIgnoreCase(Constants.M6E_I_JIC)) && vNumber.compareTo(new SerialReader.VersionNumber(1, 21, 0, 5)) >= 0)
               || ((readerModel.equalsIgnoreCase(Constants.M6E_MICRO) || readerModel.equalsIgnoreCase(Constants.M6E_MICRO_USB))&& vNumber.compareTo(new SerialReader.VersionNumber(1, 5, 0, 1)) >= 0)
               ||  readerModel.equalsIgnoreCase(Constants.M6E_NANO)
               || (readerModel.equalsIgnoreCase(Constants.M6E_MICRO_USB_PRO) && vNumber.compareTo(new SerialReader.VersionNumber(1, 7, 2, 0)) >= 0))
           {
             isAutonomousSupported = true;
           }
       } 
       catch (Exception ex)
       {

       }     
   }

   private void setReaderStatus(int option)
   {
       switch(option)
       {
           case -1:
               connectStatus.fillProperty().set(Color.RED);
               statusLabel.setText("Not Connected");
               break;
           case 0:
               connectStatus.fillProperty().set(Color.ORANGE);
               statusLabel.setText("Connected");
               break;
           case 1:
               connectStatus.fillProperty().set(Color.GREEN);
               statusLabel.setText("Reading");
               break;
           case 2:
               connectStatus.fillProperty().set(Color.WHITE);
               statusLabel.setText("");
       }
   }
   
   class SaveTransportLogs implements TransportListener
   {
        @Override
        public void message(boolean tx, byte[] data, int timeout)
        {
            if(transportWriter != null)
            {
                try
                {
                    StringBuffer sb = new StringBuffer();
                    sb.append(tx ? "Sending: " : "Received: ");
                    for (int i = 0; i < data.length; i++)
                    {
                      if (i > 0 && (i & 15) == 0)
                        sb.append("\n");
                      sb.append(String.format(" %02x", data[i]));
                    }
                    transportWriter.append(sb);
                    transportWriter.newLine();
                    transportWriter.flush();
                }
                catch(IOException e)
                {

                }
            }
        }
    }
   
   class SaveElaraTransportLogs implements ElaraTransportListener{

        @Override
        public void message(boolean tx, String data) {
            if(transportWriter!=null)
            {
                try {
                    StringBuffer sb = new StringBuffer();
                    if(data!= null)
                    {
                        if(data.length() > 0)
                        {
                            sb.append(tx ? "Sending: " : "Received: ");
                            sb.append(data);
                            sb.append("\n");
                            transportWriter.append(sb);
                            transportWriter.newLine();
                            transportWriter.flush();
                        }
                    }
                } catch (IOException ex) {
                    Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
                }
            }
        }
   }

    private void createTransportLogsIntoFile()
   {
        try
        {  
            fileFilters = new ArrayList<String>();
            fileFilters.add("*.txt");
            chooseFile("Choose configuration file", "Configuration file (*.txt)", fileFilters);
            fileChooser.setInitialFileName("TCT_TransportLogs_"+dfhms.format(new Date(Calendar.getInstance().getTimeInMillis()))+".txt");
            File recordsDir = new File(System.getProperty("user.home")+ File.separatorChar + "Desktop");
            if(recordsDir.exists())
            {
                fileChooser.setInitialDirectory(recordsDir);
            }
            File transLogFile = fileChooser.showSaveDialog(stage);
            if(transLogFile != null)
            {
                transLogFile.setWritable(true);
                transLogFile.setExecutable(true);
                FileWriter fileWriter = new FileWriter(transLogFile.getAbsoluteFile(),true);
                transportWriter = new BufferedWriter(fileWriter);
            }
            else
            {   
                isTransportLogsEnabled = false;
                cbTransportLogging.setSelected(false);
            }
        }
        catch(Exception e)
        {

        }
   }

   private void checkReadWritePowerOnUSBProModule()
   {
       if(readerModel.equalsIgnoreCase("M6e Micro USBPro") && !(rfRead.getText().equals("") || rfWrite.getText().equals("")))
       {
           float readPower = Float.parseFloat(rfRead.getText());
           float writePower = Float.parseFloat(rfWrite.getText());

           if(readPower >20 || writePower >20)
           {
               showWarningErrorMessage("warning", "Please make sure to provide additional DC power source to the reader");
           }
       }
   }

    private void disableEmbeddedReadData()
    {
        embeddedReadUnique.setDisable(true);
        embeddedReadUnique.setOpacity(disableOpacity);
        embeddedReadUnique.setSelected(false);
        embeddedMemoryBank.setDisable(true);
        embeddedMemoryBank.setOpacity(disableOpacity);
        embeddedStart.setDisable(true);
        embeddedStart.setOpacity(disableOpacity);
        embeddedEnd.setDisable(true);
        embeddedEnd.setOpacity(disableOpacity);
    }
    
    private void changeUIOnGpiTriggerRead(boolean status, double opacity)
    {
        autoReadGpi1.setDisable(status);
        autoReadGpi1.setOpacity(opacity);
        autoReadGpi2.setDisable(status);
        autoReadGpi2.setOpacity(opacity);
        autoReadGpi3.setDisable(status);
        autoReadGpi3.setOpacity(opacity);
        autoReadGpi4.setDisable(status);
        autoReadGpi4.setOpacity(opacity);
        autonomousRead.setSelected(status);
    }        

    private void getReaderDiagnostics() throws Exception
    {
        readerModel = (String) r.paramGet("/reader/version/model");
        firmwareVerson = (String) r.paramGet("/reader/version/software");
        hardWareVersion = (String) r.paramGet(TMConstants.TMR_PARAM_VERSION_HARDWARE);
        lRfidEngine.setText(readerModel);
        lFirmwareVersion.setText(firmwareVerson);
        lHardwareVersion.setText(hardWareVersion);
        lActVersion.setText(Constants.ACT_VERSION);
        lMercuryApiVersion.setText(Constants.MERCURY_API_VERSION);
    }

    class LoadSaveExceptionReciver implements ReadExceptionListener
    {
        @Override
        public void tagReadException(com.thingmagic.Reader r, ReaderException re)
        {
           if(!re.getMessage().toLowerCase().contains(Constants.SKIPPING))
           {
              loadSaveError.append(re.getMessage());
              loadSaveError.append(System.getProperty("line.separator"));
           }
        }
    }

    private void regionBasedPowerListener() throws ReaderException
    {
        try{
            int readPower = (Integer) r.paramGet("/reader/radio/readPower");
            int writePower = (Integer) r.paramGet("/reader/radio/writePower");
            if(readerModel.equalsIgnoreCase(Constants.M6E_MICRO_USB_PRO))
            {
                rfRead.setText("20");
                rfWrite.setText("20");
                readPower = 2000;
                writePower = 2000;
                r.paramSet("/reader/radio/readPower", readPower);
                r.paramSet("/reader/radio/writePower", writePower);
            }
            else
            {
                Float readPowerValue = (float)readPower / 100;
                Float writePowerValue = (float)writePower / 100;
                rfRead.setText(String.valueOf(readPowerValue));
                rfWrite.setText(String.valueOf(writePowerValue));
            }

            minReaderPower = (Integer) r.paramGet("/reader/radio/powerMin");
            minPower.setText(String.valueOf(minReaderPower / 100) + "dBm");
            minPower1.setText(String.valueOf(minReaderPower / 100) + "dBm");

            maxReaderPower = (Integer) r.paramGet("/reader/radio/powerMax");
            maxPower.setText(String.valueOf(maxReaderPower / 100.0) + "dBm");
            maxPower1.setText(String.valueOf(maxReaderPower / 100.0) + "dBm");
            checkReadWritePowerOnUSBProModule();

            changeListMap.put("readPower", rfRead.getText());
            changeListMap.put("writePower", rfWrite.getText());
            readPowerSlider.setMin(minReaderPower/100.0);
            writePowerSlider.setMin(minReaderPower/100.0);
            readPowerSlider.setMax(maxReaderPower / 100.0);
            writePowerSlider.setMax(maxReaderPower / 100.0);

            readPowerSlider.setValue(readPower / 100.0);
            writePowerSlider.setValue(writePower / 100.0);
        }
        catch(Exception e)
        {

        }
    }

    public void enumerateHIDDevices()
    {
        try
        {    
            List<HidDeviceInfo> deviceList = PureJavaHidApi.enumerateDevices();
            for (HidDeviceInfo hidDevice : deviceList)
            {
                if((hidDevice.getVendorId() == 0x2008) && (hidDevice.getProductId() == 0x2001))
                {
                    comportList.add("Elara HID");
                    setReaders("Elara HID");
                }
            }
        }
        catch(Exception ex)
        {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
    
    public void cleanUpResources()
    {
        try
        {
            if(sp != null && isConnected)
            {
                ElaraJSONParser ejp = new ElaraJSONParser();
                String stopRZ = ejp.formJSONCommand(ECTConstants.STOP_RZ);
                sendMessage(sp, stopRZ, elaraTransportListener);
                String stopHB = ejp.formJSONCommand(ECTConstants.SET_CFG_STOP_HEARTBEAT);
                sendMessage(sp, stopHB, elaraTransportListener);
                isElaraReading = false;
                isConnected = false;
                isElaraCDC = false;
                elaraRS232 = false;
                hidEnabled = false;
                isElaraHID = false;
                sp.clearBreak();
                sp.clearDTR();
                sp.clearRTS();
                sp.closePort();
                sp = null;
            }
            else
            {
                if(sp!=null)
                {
                    sp.closePort();
                    sp = null;
                }
                isElaraReading = false;
                isConnected = false;
                isElaraCDC = false;
                elaraRS232 = false;
                hidEnabled = false;
                isElaraHID = false;
            }
        }
        catch(Exception ex)
        {
             Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
    
    public void setPopulateElaraRegion(ElaraJSONParser ejsonp, String regionSet)
    {
        try 
        {
            String region;
            String getHWRegion = ejsonp.formJSONCommand(ECTConstants.GET_CFG_FIELDS_REGION);
            String responseReceived = sendMessage(sp, getHWRegion, elaraTransportListener);
            JSONObject jsono = new JSONObject(responseReceived);
            if(jsono.get("Report").equals("GetCfg") && jsono.get("ErrID").equals(0))
            {
                region = (String) jsono.get("Region");
                if(region.equalsIgnoreCase("0x1"))
                {
                    String[] subRegion = 
                        {"JP","PRC","KR2","AU","NZ",
                        "NA2","NA3","MY","ID","PH",
                        "TW","MO","SG","JP2","JP3",
                        "TH","AR","BD","IN","EU3",
                        "RU","VN","HK"};
                    myList = new ArrayList<String>( Arrays.asList(subRegion));
                    regionElara.getItems().addAll(myList);
                    regionElara.getSelectionModel().select(regionSet);  
                }
                else if(region.equalsIgnoreCase("0x2"))
                {
                    String[] subRegion = 
                        {"JP","PRC","KR2","AU","NZ",
                        "NA2","NA3","MY","ID","PH",
                        "TW","MO","SG","JP2","JP3",
                        "TH","AR","BD"};
                    myList = new ArrayList<String>( Arrays.asList(subRegion));
                    regionElara.getItems().addAll(myList);
                    regionElara.getSelectionModel().select(regionSet); 
                }
                else if(region.equalsIgnoreCase("0x3"))
                {
                    String[] subRegion = 
                        {"IN","EU3",
                        "RU","VN","HK"};
                    myList = new ArrayList<String>( Arrays.asList(subRegion));
                    regionElara.getItems().addAll(myList);
                    regionElara.getSelectionModel().select(regionSet);
                }
                else if(region=="0x04")
                {
                    regionElara.getItems().add("INVALID REGION");
                    regionElara.getSelectionModel().select(regionSet);
                }
            }
            else
            {
                region = "NOTSET";
            }
        } catch (JSONException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
    
    private void configureHID(ElaraJSONParser ejsonp) throws JSONException,ReaderException
    {
        if(elaraHIDModeEnabled.isSelected())
        {
            //Enable HID
            String enableHIDCommand = ejsonp.formJSONCommand(ECTConstants.SET_CFG_USBKBENABLE);
            String responseReceived = sendMessage(sp, enableHIDCommand, elaraTransportListener);
            JSONObject hidJSON = new JSONObject(responseReceived);
            if(hidJSON.get("Report").equals("SetCfg") && hidJSON.get("ErrID").equals(0))
            {
                hidEnabled = true;
            }
            else
            {
                if(hidJSON.toString().contains("ErrInfo"))
                {
                    throw new ReaderException((String)hidJSON.get("ErrInfo"));
                }
                else
                {
                    throw new ReaderException("Failed to enable HID mode");
                }
            }
        }
        else
        {
            //Disable HID
            String disableHIDCommand = ejsonp.formJSONCommand(ECTConstants.SET_CFG_USBKBDISABLE);
            String responseReceived = sendMessage(sp, disableHIDCommand, elaraTransportListener);
            JSONObject hidJSON = new JSONObject(responseReceived);
            if(hidJSON.get("Report").equals("SetCfg") && hidJSON.get("ErrID").equals(0))
            {
                hidEnabled = false;
            }
            else
            {
                if(hidJSON.toString().contains("ErrInfo"))
                {
                    throw new ReaderException((String)hidJSON.get("ErrInfo"));
                }
                else
                {
                    throw new ReaderException("Failed to disable HID mode");
                }
            }
        }
     }
    
    public class CheckThread implements Runnable{
        
        @Override
        public void run() 
        {
            while(isConnected)
            {
                try 
                {
                    Thread.sleep(3000);
                    SerialPort[] portList;
                    String[] results;
                    SerialPort currentSP = null;
                    // get comm ports
                    portList = SerialPort.getCommPorts();
                    results = new String[portList.length];
                    for (int i = 0; i < portList.length; i++) 
                    {
                        results[i] = portList[i].getSystemPortName();
                        if(portList[i].getPortDescription().equalsIgnoreCase("Elara Reader"))
                        {
                            String deviceName = portList[i].getDescriptivePortName();
                            String deviceNameString = portList[i].getSystemPortName();
                            currentSP = SerialPort.getCommPort(deviceNameString);
                        }
                        else if(elaraRS232)
                        {
                            currentSP = sp;
                        }
                    }
                    if(currentSP!=null)
                    {
                        //Do Nothing
                    }
                    else
                    {
                        new Thread(disConnectThread).start();
                    }
                }
                catch (InterruptedException ex) {
                    throw new RuntimeException("Check thread interrupted"); 
                }
            }       
        }
    }
       
    private boolean isValidJSON(String toTestStr) 
    {
        if(toTestStr==null)
        {
            return false;
        }
        else
        {
            try 
            {
                new JSONObject(toTestStr);
            } 
            catch (JSONException ex) 
            {
                return false;
            }
            return true; 
        } 
    }
    
    private void clearInputStream()
    {
        try
        {
        if(!hidEnabled)
        {
            while(sp.getInputStream().available() > 0)
            {
                sp.getInputStream().read();
            }
            String stopRZ = ejsonp.formJSONCommand(ECTConstants.STOP_RZ);
            String response = sendMessage(sp, stopRZ, elaraTransportListener);
        }
        else
        {
            String stopRZ = ejsonp.formJSONCommand(ECTConstants.STOP_RZ);
            String response = sendMessage(sp, stopRZ, elaraTransportListener);
            while(sp.getInputStream().available() > 0)
            {
                sp.getInputStream().read();
            }
        }
        } 
        catch (IOException ex) {
            Logger.getLogger(MainController.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
    /**
     * Try to connect Elara RS232 reader with different baud rates
     *
     * @param port - the COM port on which reader is connected.
     * @return a boolean response.
     */
    private boolean connectToPort(String port)
    {
        boolean success = false;
        boolean isBaudRateOk = false;
        String setStopRzResponse = "";
        //Try connecting with the user selected baud rate
        int selectedBaudRate = Integer.parseInt(probeBaudRate.getSelectionModel().getSelectedItem().toString());
        String deviceNameString = port.substring(port.indexOf("(") + 1, port.indexOf(")"));
        sp = SerialPort.getCommPort(deviceNameString);
        sp.setComPortParameters(selectedBaudRate, 8, 1, 0);
        boolean portOpen = sp.openPort();
        if(portOpen)
        {
            ejsonp = new ElaraJSONParser();
            /**For RS232 variants response there is no response to StopRZ command the first time.
            * Hence trying twice.
            */
            for(int i=0 ; i < 2 ; i++)
            {
                String setStopRzCmd = ejsonp.formJSONCommand(ECTConstants.STOP_RZ);
                setStopRzResponse = sendMessage(sp, setStopRzCmd, elaraTransportListener);
            }
            if(isValidJSON(setStopRzResponse))
            {
                success = true;
            }
            else
            {
                sp.closePort();
                showWarningErrorMessage("warning", "Failed to connect with baudrate "+ selectedBaudRate+" and trying with other baudrates...");
                success = false;
            }
        }
        //Return is user selected baud rate succeeds 
        if(success)
        {
            return true;
        }
        //Try with other baud rates
        else
        {
            int[] bps = new int[] { 9600, 115200, 19200, 38400, 57600, 230400, 460800 };
            for (int count=0;count<bps.length;count++)
            {
                if(bps[count] == selectedBaudRate)
                {
                    //Tried with this baudrate already...continue
                    continue;
                }
                sp = SerialPort.getCommPort(deviceNameString);
                sp.setComPortParameters(bps[count], 8, 1, 0);
                boolean openPort = sp.openPort();
                if(portOpen)
                {
                    ejsonp = new ElaraJSONParser();
                    for(int i=0 ; i < 2 ; i++)
                    {
                        String setStopRzCmd = ejsonp.formJSONCommand(ECTConstants.STOP_RZ);
                        setStopRzResponse = sendMessage(sp, setStopRzCmd, elaraTransportListener);
                    }
                    if(isValidJSON(setStopRzResponse))
                    {
                        success = true;
                        break;
                    }
                    else
                    {
                        sp.closePort();
                        showWarningErrorMessage("warning", "Failed to connect with baudrate "+ String.valueOf(bps[count])+" and trying with other baudrates...");
                        continue;
                    }
                }
            }
            if(success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /**
     * Method to save config values in a HashMap
     */
    private void saveConfigValues()
    {
        elaraChangeListMap.clear();
        if(!regionElara.isDisabled())
        {
            elaraChangeListMap.put("FreqReg", regionElara.getSelectionModel().getSelectedItem().toString());
        }
        if(!autonomousReadElara.isDisabled())
        {
            elaraChangeListMap.put("RdrStart",String.valueOf(autonomousReadElara.isSelected()));
        }
        if(!workflow.isDisabled())
        {
            elaraChangeListMap.put("Mode",workflow.getSelectionModel().getSelectedItem().toString());
        }
        if(!rfReadElara.isDisabled())
        {
            elaraChangeListMap.put("ReadPwr",rfReadElara.getText());
        }
        if(!rfWriteElara.isDisabled())
        {
            elaraChangeListMap.put("WritePwr",rfWriteElara.getText());
        }
        if(!elaraHIDModeEnabled.isDisabled())
        {
            elaraChangeListMap.put("_USBKBEnable",String.valueOf(elaraHIDModeEnabled.isSelected()));
        }
        if(!buzzerTone.isDisabled())
        {
            elaraChangeListMap.put("_AudioVolume",buzzerTone.getValue().toString());
        }
        if(!outputDataFormat.isDisabled())
        {
            elaraChangeListMap.put("_KBDataFormat",outputDataFormat.getSelectionModel().getSelectedItem().toString());
        }
        if(!probeElaraBaudRate.isDisabled())
        {
            elaraChangeListMap.put("SerCfg",probeElaraBaudRate.getSelectionModel().getSelectedItem().toString());
        }
        if(!elaraMetadataInventoryCount.isDisabled())
        {
            elaraChangeListMap.put("SpotInvCnt",String.valueOf(elaraMetadataInventoryCount.isSelected()));
        }
        if(!elaraMetadataRSSI.isDisabled())
        {
            elaraChangeListMap.put("SpotRSSI",String.valueOf(elaraMetadataRSSI.isSelected()));
        }
        if(!elaraMetadataAntenna.isDisabled())
        {
            elaraChangeListMap.put("SpotAnt",String.valueOf(elaraMetadataAntenna.isSelected()));
        }
        if(!elaraMetadataDateTime.isDisabled())
        {
            elaraChangeListMap.put("SpotDT",String.valueOf(elaraMetadataDateTime.isSelected()));
        }
        if(!elaraMetadataPhase.isDisabled())
        {
            elaraChangeListMap.put("SpotPhase",String.valueOf(elaraMetadataPhase.isSelected()));
        }
        if(!elaraMetadataProfile.isDisabled())
        {
            elaraChangeListMap.put("SpotProf",String.valueOf(elaraMetadataProfile.isSelected()));
        }
        if(!elaraMetadataRZ.isDisabled())
        {
            elaraChangeListMap.put("SpotRz",String.valueOf(elaraMetadataRZ.isSelected()));
        }
        if(!elaraMetadataFreq.isDisabled())
        {
            elaraChangeListMap.put("SpotFreq",String.valueOf(elaraMetadataFreq.isSelected()));
        }
        if(!elaraMetadataGen2BI.isDisabled())
        {
            elaraChangeListMap.put("SpotGen2_BI",String.valueOf(elaraMetadataGen2BI.isSelected()));
        }
        if(!elaraMetadataGen2Q.isDisabled())
        {
            elaraChangeListMap.put("SpotGen2_Q",String.valueOf(elaraMetadataGen2Q.isSelected()));
        }
        if(!elaraMetadataGen2LF.isDisabled())
        {
            elaraChangeListMap.put("SpotGen2_LF",String.valueOf(elaraMetadataGen2LF.isSelected()));
        }
        if(!elaraMetadataGen2Target.isDisabled())
        {
            elaraChangeListMap.put("SpotGen2_Target",String.valueOf(elaraMetadataGen2Target.isSelected()));
        }
        if(!elaraMetadataGPIO.isDisabled())
        {
            elaraChangeListMap.put("SpotGPIO",String.valueOf(elaraMetadataGPIO.isSelected()));
        }
        if(!elaraMetadataProtocol.isDisabled())
        {
            elaraChangeListMap.put("SpotProt",String.valueOf(elaraMetadataProtocol.isSelected()));
        }
        elaraChangeListMap.put("RFOnTime",String.valueOf(rfOnTime.getText()));
        elaraChangeListMap.put("RFOffTime",String.valueOf(rfOffTime.getText()));
        elaraChangeListMap.put("Session", sessionGroupElara.getSelectedToggle().getUserData().toString());
        elaraChangeListMap.put("Target", targetGroupElara.getSelectedToggle().getUserData().toString());
        elaraChangeListMap.put("DataEncoding", tagEncodeGroupElara.getSelectedToggle().getUserData().toString());
        elaraChangeListMap.put("BLF", linkFreqGroupElara.getSelectedToggle().getUserData().toString());
        elaraChangeListMap.put("Q", qGroupElara.getSelectedToggle().getUserData().toString());
        elaraChangeListMap.put("QListValue", qListElara.getValue().toString());
        elaraChangeListMap.put("NewTagDelay",String.valueOf(newTagDelay.getText()));
        elaraChangeListMap.put("SameTagDelay",String.valueOf(sameTagDelay.getText()));
        elaraChangeListMap.put("ReReport",String.valueOf(reReport.isSelected()));
    }

    /**
     * Elara - Compare and update the field changes in a HashMap
    */
    private void compareUpdateConfigValues()
    {
        currentListMap.clear();
        if(elaraChangeListMap.containsKey("FreqReg"))
        {
            if(!(elaraChangeListMap.get("FreqReg").equals(regionElara.getSelectionModel().getSelectedItem().toString())))
            {
                currentListMap.put("FreqReg", "true");
            }
            else
            {
                currentListMap.put("FreqReg", "false");
            }
        }
        if(elaraChangeListMap.containsKey("RdrStart"))
        {
            if(!(elaraChangeListMap.get("RdrStart").equals(String.valueOf(autonomousReadElara.isSelected()))))
            {
                currentListMap.put("RdrStart", "true");
            }
            else
            {
                currentListMap.put("RdrStart", "false");
            }
        }
        if(elaraChangeListMap.containsKey("ReadPwr"))
        {
            if(!(elaraChangeListMap.get("ReadPwr").equals(rfReadElara.getText())))
            {
                currentListMap.put("ReadPwr", "true");
            }
            else
            {
                currentListMap.put("ReadPwr", "false");
            }
        }
        if(elaraChangeListMap.containsKey("WritePwr"))
        {
            if(!(elaraChangeListMap.get("WritePwr").equals(rfWriteElara.getText())))
            {
                currentListMap.put("WritePwr", "true");
            }
            else
            {
                currentListMap.put("WritePwr", "false");
            }
        }
        if(elaraChangeListMap.containsKey("Mode"))
        {
            if(!(elaraChangeListMap.get("Mode").equals(workflow.getSelectionModel().getSelectedItem().toString())))
            {
                currentListMap.put("Mode", "true");
            }
            else
            {
                currentListMap.put("Mode", "false");
            }
        }
        if(elaraChangeListMap.containsKey("_USBKBEnable"))
        {
            if(!(elaraChangeListMap.get("_USBKBEnable").equals(String.valueOf(elaraHIDModeEnabled.isSelected()))))
            {
                currentListMap.put("_USBKBEnable", "true");
            }
            else
            {
                currentListMap.put("_USBKBEnable", "false");
            }
        }
        if(elaraChangeListMap.containsKey("_AudioVolume"))
        {
            if(!(elaraChangeListMap.get("_AudioVolume").equals(buzzerTone.getSelectionModel().getSelectedItem().toString())))
            {
                currentListMap.put("_AudioVolume", "true");
            }
            else
            {
                currentListMap.put("_AudioVolume", "false");
            }
        }
        if(elaraChangeListMap.containsKey("SerCfg"))
        {
            if(!(elaraChangeListMap.get("SerCfg").equals(probeElaraBaudRate.getSelectionModel().getSelectedItem().toString())))
            {
                currentListMap.put("SerCfg", "true");
            }
            else
            {
                currentListMap.put("SerCfg", "false");
            }
        }
        if(elaraChangeListMap.containsKey("_KBDataFormat"))
        {
            if(!(elaraChangeListMap.get("_KBDataFormat").equals(outputDataFormat.getSelectionModel().getSelectedItem().toString())))
            {
                currentListMap.put("_KBDataFormat", "true");
            }
            else
            {
                currentListMap.put("_KBDataFormat", "false");
            }
        }
        if(elaraChangeListMap.containsKey("RFOnTime") || elaraChangeListMap.containsKey("RFOffTime"))
        {
            if(!(elaraChangeListMap.get("RFOnTime").equals(rfOnTime.getText())) || !(elaraChangeListMap.get("RFOffTime").equals(rfOffTime.getText())))
            {
                currentListMap.put("DutyCycle", "true");
            }
        }
        else
        {
            currentListMap.put("DutyCycle", "false");
        }
        if(elaraChangeListMap.containsKey("Session"))
        {
            String session = elaraChangeListMap.get("Session");
            if(!session.equalsIgnoreCase(sessionGroupElara.getSelectedToggle().getUserData().toString()))
            {
                currentListMap.put("Session",sessionGroupElara.getSelectedToggle().getUserData().toString());
            }
        }
        if(elaraChangeListMap.containsKey("Target"))
        {
            String target = elaraChangeListMap.get("Target");
            if(!target.equalsIgnoreCase(targetGroupElara.getSelectedToggle().getUserData().toString()))
            {
                currentListMap.put("Target",targetGroupElara.getSelectedToggle().getUserData().toString());
            }
        }
        if(elaraChangeListMap.containsKey("DataEncoding"))
        {
            String dataEncoding = elaraChangeListMap.get("DataEncoding");
            if(!dataEncoding.equalsIgnoreCase(tagEncodeGroupElara.getSelectedToggle().getUserData().toString()))
            {
                currentListMap.put("DataEncoding",tagEncodeGroupElara.getSelectedToggle().getUserData().toString());
            }
        }
        if(elaraChangeListMap.containsKey("BLF"))
        {
            String blf = elaraChangeListMap.get("BLF");
            if(!blf.equalsIgnoreCase(linkFreqGroupElara.getSelectedToggle().getUserData().toString()))
            {
                currentListMap.put("BLF",linkFreqGroupElara.getSelectedToggle().getUserData().toString());
            }
        }
        if(elaraChangeListMap.containsKey("Q"))
        {
            String qVal = elaraChangeListMap.get("Q");
            if(!qVal.equalsIgnoreCase(qGroupElara.getSelectedToggle().getUserData().toString()) 
                    || !qListElara.getValue().toString().equalsIgnoreCase(elaraChangeListMap.get("QListValue")))
            {
                currentListMap.put("Q",qGroupElara.getSelectedToggle().getUserData().toString());
            }
        }
        if(elaraChangeListMap.containsKey("NewTagDelay"))
        {
            if(!(elaraChangeListMap.get("NewTagDelay").equalsIgnoreCase(newTagDelay.getText())))
            {
                currentListMap.put("NewTagDelay","true");
            }
        }
        if(elaraChangeListMap.containsKey("SameTagDelay"))
        {
            if(!(elaraChangeListMap.get("SameTagDelay").equalsIgnoreCase(sameTagDelay.getText())))
            {
                currentListMap.put("SameTagDelay","true");
            }
        }
        if(elaraChangeListMap.containsKey("ReReport"))
        {
            if(!(elaraChangeListMap.get("ReReport").equalsIgnoreCase(String.valueOf(reReport.isSelected()))))
            {
                currentListMap.put("ReReport","true");
            }
        }
        if(isRSKit)
        {
            if(!(elaraChangeListMap.get("SpotDT").equals(String.valueOf(elaraMetadataDateTime.isSelected()))
                    && elaraChangeListMap.get("SpotRSSI").equals(String.valueOf(elaraMetadataRSSI.isSelected()))))
            {
                currentListMap.put("Metadata", "true");
            }
            else
            {
                currentListMap.put("Metadata", "false");
            }
        }
        else
        {
            if(!((elaraChangeListMap.get("SpotInvCnt").equals(String.valueOf(elaraMetadataInventoryCount.isSelected())))
                    && (elaraChangeListMap.get("SpotRSSI").equals(String.valueOf(elaraMetadataRSSI.isSelected())))
                    && (elaraChangeListMap.get("SpotAnt").equals(String.valueOf(elaraMetadataAntenna.isSelected())))
                    && (elaraChangeListMap.get("SpotDT").equals(String.valueOf(elaraMetadataDateTime.isSelected())))
                    && (elaraChangeListMap.get("SpotPhase").equals(String.valueOf(elaraMetadataPhase.isSelected())))
                    && (elaraChangeListMap.get("SpotFreq").equals(String.valueOf(elaraMetadataFreq.isSelected())))
                    && (elaraChangeListMap.get("SpotGen2_Q").equals(String.valueOf(elaraMetadataGen2Q.isSelected())))
                    && (elaraChangeListMap.get("SpotGen2_LF").equals(String.valueOf(elaraMetadataGen2LF.isSelected())))
                    && (elaraChangeListMap.get("SpotGen2_Target").equals(String.valueOf(elaraMetadataGen2Target.isSelected())))
                    && (elaraChangeListMap.get("SpotProt").equals(String.valueOf(elaraMetadataProtocol.isSelected())))
                    && (elaraChangeListMap.get("SpotGPIO").equals(String.valueOf(elaraMetadataGPIO.isSelected())))))
                    //&& (elaraChangeListMap.get("SpotSensor").equals(String.valueOf(elaraMetadataSensor.isSelected())))
                    //&& (elaraChangeListMap.get("SpotProf").equals(String.valueOf(elaraMetadataProfile.isSelected())))
                    //&& (elaraChangeListMap.get("SpotRz").equals(String.valueOf(elaraMetadataRZ.isSelected())))
                    //&& (elaraChangeListMap.get("SpotGen2_BI").equals(String.valueOf(elaraMetadataGen2BI.isSelected())))))
            {
                currentListMap.put("Metadata", "true");
            }
            else
            {
                currentListMap.put("Metadata", "false");
            }
        }
    }

    private void disableWritePaneElements()
    {
        writeDataSettings.setOpacity(0.5);
        readWriteTag.setDisable(true);
        writeMemoryBank.setDisable(true);
        writeMemoryBankText.setOpacity(0.5);
        writeStartAddress.setDisable(true);
        writeStartAddressText.setOpacity(0.5);
        writeWordCount.setDisable(true);
        writeWordCountText.setOpacity(0.5);
        writeData.setDisable(true);
        writeDataText.setOpacity(0.5);
        lockTag.setDisable(true);
        writeTimestamp.setDisable(true);
        accessPasswordText.setOpacity(0.5);
        accessPassword.setDisable(true);
    }

    private void enableWritePaneElements()
    {
        writeDataSettings.setOpacity(1);
        readWriteTag.setDisable(false);
        writeMemoryBank.setDisable(false);
        writeMemoryBankText.setOpacity(1);
        writeStartAddress.setDisable(false);
        writeStartAddressText.setOpacity(1);
        writeWordCount.setDisable(false);
        writeWordCountText.setOpacity(1);
        writeData.setDisable(false);
        writeDataText.setOpacity(1);
        lockTag.setDisable(false);
        writeTimestamp.setDisable(false);
        accessPasswordText.setOpacity(1);
        accessPassword.setDisable(false);
    }

    private void disableDutyCycle(boolean isDiable)
    {
       if(isDiable)
       {
            rfOnTime.setDisable(true);
            rfOnTimeText.setOpacity(disableElaraOpacity);
            rfOffTime.setDisable(true);
            rfOffTimeText.setOpacity(disableElaraOpacity);
       }
       else
       {
            rfOnTime.setDisable(false);
            rfOnTimeText.setOpacity(enableElaraOpacity);
            rfOffTime.setDisable(false);
            rfOffTimeText.setOpacity(enableElaraOpacity);
       }
    }
    
    @FXML
    private void validateReadWrite()
    {
        if(readWriteTag.getSelectionModel().getSelectedItem().toString().equalsIgnoreCase("READ"))
        {
            lockTag.setDisable(true);
            writeDataText.setOpacity(0.5);
            writeData.setDisable(true);
            writeTimestamp.setDisable(true);
            accessPassword.setDisable(true);
            accessPasswordText.setOpacity(0.5);
            writeMemoryBank.getItems().clear();
            writeMemoryBank.getItems().addAll("RESERVED","EPC","TID","USER");
            writeMemoryBank.getSelectionModel().selectLast();
            
        }
        else
        {
            writeMemoryBank.getItems().clear();
            writeMemoryBank.getItems().addAll("RESERVED","EPC","USER");
            writeMemoryBank.getSelectionModel().selectLast();
            if(writeTimestamp.isSelected())
            {
                writeTimestamp.setDisable(false);
                writeData.setDisable(true);
                writeDataText.setOpacity(0.5);
                lockTag.setDisable(false);
                accessPassword.setDisable(false);
                accessPasswordText.setOpacity(1);
            }
            else
            {
                writeTimestamp.setDisable(false);
                writeData.setDisable(false);
                writeDataText.setOpacity(1);
                lockTag.setDisable(false);
                accessPassword.setDisable(false);
                accessPasswordText.setOpacity(1);
            }
        }
    }

    /**
     * Elara - Re-enable all fields
     */
    private void reenableFields()
    {
        getDateTimeElara.setDisable(false);
        applyDateTimeButton.setDisable(false);
        regionElara.setDisable(false);
        rfReadElara.setDisable(false);
        readPowerSliderElara.setDisable(false);
        rfWriteElara.setDisable(false);
        writePowerSliderElara.setDisable(false);
        saveButtonElara.setDisable(false);
        revertButtonElara.setDisable(false);
        elaraMetadataInventoryCount.setDisable(false);
        elaraMetadataRSSI.setDisable(false);
        elaraMetadataAntenna.setDisable(false);
        elaraMetadataDateTime.setDisable(false);
        elaraMetadataPhase.setDisable(false);
        elaraMetadataFreq.setDisable(false);
        elaraMetadataGen2Q.setDisable(false);
        elaraMetadataGen2LF.setDisable(false);
        elaraMetadataGen2Target.setDisable(false);
        elaraMetadataProtocol.setDisable(false);
        elaraMetadataGPIO.setDisable(false);
        outputDataFormat.setDisable(false);
        buzzerTone.setDisable(false);
        autonomousReadElara.setDisable(false);
        datepicker.setValue(null);
        hoursTextField.clear();
        minutesTextField.clear();
        secondsTextField.clear();
        m2Elara.setDisable(false);
        m4Elara.setDisable(false);
        m8Elara.setDisable(false);
        sessionS0Elara.setDisable(false);
        sessionS1Elara.setDisable(false);
        sessionS2Elara.setDisable(false);
        sessionS3Elara.setDisable(false);
        targetAElara.setDisable(false);
        targetBElara.setDisable(false);
        targetABElara.setDisable(false);
        targetBAElara.setDisable(false);
        dynamicQElara.setDisable(false);
        staticQElara.setDisable(false);
        qListElara.setDisable(false);
        reReport.setDisable(false);
        newTagDelay.setDisable(false);
        newTagDelayText.setDisable(false);
        sameTagDelay.setDisable(false);
        sameTagDelayText.setDisable(false);
        rfOnTime.setDisable(false);
        rfOnTimeText.setDisable(false);
        rfOffTime.setDisable(false);
        rfOffTimeText.setDisable(false);
        //reset opacity
        tagEncodingText.setOpacity(enableElaraOpacity);
        sessionText.setOpacity(enableElaraOpacity);
        targetText.setOpacity(enableElaraOpacity);
        qText.setOpacity(enableElaraOpacity);
        newTagDelayText.setOpacity(enableElaraOpacity);
        sameTagDelayText.setOpacity(enableElaraOpacity);
        rfOnTimeText.setOpacity(enableElaraOpacity);
        rfOffTimeText.setOpacity(enableElaraOpacity);
        reReportEnable.setSelected(false);
        reReportEnable.setDisable(false);
        enableTagReReport();
    }

    /**
     * Elara - Version check to see if Gen2 features are supported
     * @return boolean
     */
    private boolean isGen2Supported()
    {
        String checkVersion = "1.15.1.8"; //Converted to decimal - Actual version - 1.0F.01.08
        String readerVersion = elaraFwVersion;
        String versionSplit[] = elaraFwVersion.split("\\.");
        for (int i = 0; i < versionSplit.length; i++) 
        {
            versionSplit[i] = Integer.toString(Integer.parseInt(versionSplit[i].trim(), 16));
            if (i == 0)
            {
                readerVersion = versionSplit[i];
            }
            else
            {
                readerVersion = readerVersion + "." + versionSplit[i];
            }
        }
        int compareVer = SerialReader.versionCompare(readerVersion, checkVersion);
        if (compareVer < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /**
     * Elara - Disable Gen2 settings
     */
    private void disableGen2Settings()
    { 
        //Gen2 settings
        m2Elara.setDisable(true);
        m4Elara.setDisable(true);
        m8Elara.setDisable(true);
        sessionS0Elara.setDisable(true);
        sessionS1Elara.setDisable(true);
        sessionS2Elara.setDisable(true);
        sessionS3Elara.setDisable(true);
        targetAElara.setDisable(true);
        targetBElara.setDisable(true);
        targetABElara.setDisable(true);
        targetBAElara.setDisable(true);
        dynamicQElara.setDisable(true);
        staticQElara.setDisable(true);
        qListElara.setDisable(true);
        tagEncodingText.setOpacity(disableElaraOpacity);
        sessionText.setOpacity(disableElaraOpacity);
        targetText.setOpacity(disableElaraOpacity);
        qText.setOpacity(disableElaraOpacity);
        //TagReReport settings
        reReport.setDisable(true);
        newTagDelay.setDisable(true);
        newTagDelayText.setDisable(true);
        newTagDelayText.setOpacity(disableElaraOpacity);
        sameTagDelay.setDisable(true);
        sameTagDelayText.setDisable(true);
        sameTagDelayText.setOpacity(disableElaraOpacity);
        //Duty cycle settings
        rfOnTime.setDisable(true);
        rfOnTimeText.setOpacity(disableElaraOpacity);
        rfOffTime.setDisable(true);
        rfOffTimeText.setOpacity(disableElaraOpacity);
    }
}
