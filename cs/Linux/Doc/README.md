\mainpage
A RFID tag inventory management system provides a rapid method of conducting tag inventory assessment and knowing what tags added and/or missing ?

A sample ThingMagic Inventory Management Application is built showcasing how tag inventory management events and its actions are handled using InventoryManagementAPIs and ThingMagic Reader MercuryAPIs to convey response with alert and exception through console message printing. 

This document will guide you through the following below items:\n
* Sample Inventory Manangement Application Software Organization  
* ThingMagic Inventory Management API Class & methods 
* APPENDIX: Basic Tools Installation guide 

##1. Sample Inventory Management Application Software Organization 
The Inventory Management Application is broadly architectured and classified
into three software sections:\n
The whole software documentation of these below sections are derived automatically using \n
Doxygen generated html documentation describing all necessary class, functions traced thru easily.\n
    ###a) Inventory Management Application (Linux/InventoryManagementApp)
    This is sample skeleton Inventory Management Application and self explanatory.
    It does not need any further help documentation. \n
\n
```
    Reference sample Inventory Management application software folder: 
    mercuryapi/cs/Samples/InventoryManagementAppLinux   
    source files:
    mercuryapi/cs/Samples/InventoryManagementAppLinux/InventoryManagementApp.cs 
    mercuryapi/cs/Samples/InventoryManagementAppLinux/InventoryManagementAppLinux.csproj 
```
\n
    ###b) Inventory Management API ( Linux/InventoryManagementAPI.dll) 
    This provides details of InventoryManagement API class, members and implementation \n
\n
```
    Reference sample Inventory Management API software folder: 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux   
    source files: 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementAPI.cs 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementConfigData.cs 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementAlertEventArgs.cs 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementAlertEventActions.cs 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementExceptionEventArgs.cs
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementExceptionEventActions.cs 
    mercuryapi/cs/ThingMagic.InventoryManagementAPILinux/InventoryManagementAPI.csproj
    The reference InventoryManagementAPI.chm documentation help can be accessed  
    from folder: 
    mercuryapi/cs/Linux/Doc/InventoryManagementAPI.chm 
```
\n
    ###c) Mercuryapi API Utility (Linux/MercuryAPI.dll)
\n
```
    The reference MercuryAPI.chm documentation help can be accessed  
    from folder:
    mercuryapi/cs/Doc/MercuryAPI.chm 
    Reference Reader related MercuryAPI utilities folder: 
    mercuryapi/cs/ThingMagic.ReaderLinux   
```
\n
##2. ThingMagic Inventory Management API Class & methods 
The **InventoryManagementAPI namespace** is broadly classified into **ITagInventoryManagement interface** to handle inventory tracker API methods & includes the classes such as **TagInventoryManagementContainer class**, **InventoryManagementConfigData class**, **InventoryManagementAlertEventArgs class** , **InventoryManagementAlertEventActions class**, **InventoryManagementExceptionEventArgs class** and **InventoryManagementExceptionEventActions class** \n
\n
**ITagInventoryManagement interface** exposes the following methods as below
| Members | Description |
| --- | ----------- |
| **inventoryInitialization**  | This method performs complete inventory initialization including all event handler registrations  |
| **inventoryTagListener** | This method handles alert on incoming new tag reads and builds the Inventory Management list of tags with metadata information to process and handle further... |
\n
The **InventoryManagementAPI** is broadly divides into the following classes below\n 
| Class | Description |
| --- | ----------- |
| **TagInventoryManagementContainer** | This is the main Inventory Management API class which deals with inventory initialization, reader initialization, inventory Tag List handling and then attached with inventory timer and reader listeners too. |
| **InventoryManagementConfigData** | This is Inventory Management Configuration Data class which deals with all config parsing and restore needs. |
| **InventoryManagementAlertEventArgs** | This is Inventory Management Alert Event Args class which handles all alert events such as \n OnAddedTag \n OnMissedTag \n OnDeleteTag \n OnDoorOpen \n OnDoorClose \n DoorOpenGPOHigh \n DoorOpenGPOLow \n DoorCloseGPOHigh \n DoorCloseGPOLow |
| **InventoryManagementAlertEventActions** | This is Inventory Management Alert Event Actions deals with mainly action and message responses such as OnAddedTag, OnMissedTag, OnDeleteTag, OnDoorOpen, OnDoorClose, DoorOpenGPOHigh, DoorOpenGPOLow, DoorCloseGPOHigh, DoorCloseGPOLow |
| **InventoryManagementExceptionEventArgs** | This is Inventory Management Exception Event Args class which handles all exception events such as \n OnMissedTag \n OnDuplicateTag \n OnUnwantedTag |
| **InventoryManagementExceptionEventActions** | This is Inventory Management Exception Event Actions deals with mainly action and message responses such as OnMissedTag, OnDuplicateTag, OnUnwantedTag |

The **TagInventoryManagementContainer class** contains the following methods as below.

| Methods | Description |
| --- | ----------- |
| **TagInventoryManagementContainer class** ||
| ``` Public member functions ``` ||
| **inventoryInitialization**  | This method performs complete inventory initialization including all event handler registrations  |
| **inventoryTagListener** | This method handles alert on incomming new tag reads and builds the Inventory Management list of tags with metadata information to process and handle further...|
| **Dispose** | Dispose all used resources |
\n
The **InventoryManagementConfigData class** contains the following methods.

| Methods | Description |
| --- | ----------- |
| **InventoryManagementConfigData class** ||
| ``` Event Handler member ``` ||
| **AlertListener** | This is handle to Inventory Management Alert Event Args class to manage all alert events such as "OnAddedTag", "OnMissedTag", "OnDeleteTag", "OnDoorOpen","OnDoorClose", "DoorOpenGPOHigh", "DoorOpenGPOLow", "DoorCloseGPOHigh", "DoorCloseGPOLow" |
| **ExceptionListener** | This is handle to Inventory Management Exception Event Args class to manage all exception events such as "OnUnwantedTag","OnMissedTag", "OnDuplicateTag" |
| ``` Public member functions ``` ||
| **InventoryManagementInitConfigData** | This method initialize Inventory Management Configuration Data such as configSettings etc |
| **InventoryManagementInitRemainingConfigData** | This method initialize Inventory Management Remaining Configuration Data such as HardwareInfo, applyReaderLoadConfig, readerInitialization, SaveConfig etc |
| **OnInventoryManagementAlert** | This method invokes alert on Inventory Management Alert Event message such as "OnAddedTag", "OnMissedTag", "OnDeleteTag", "OnDoorOpen","OnDoorClose", "DoorOpenGPOHigh", "DoorOpenGPOLow", "DoorCloseGPOHigh", "DoorCloseGPOLow" |
| **OnInventoryManagementException** | This method invokes exception on Inventory Management Exception Event message such as "OnUnwantedTag","OnMissedTag", "OnDuplicateTag" |
\n
The **InventoryManagementAlertEventArgs class** contains the following methods.
\n
| Methods | Description |
| --- | ----------- |
| **InventoryManagementAlertEventArgs class** ||
| ``` Public member functions ``` ||
| **InventoryManagementAlertEventArgs** | InventoryManagementAlertEventArgs Constructor. This method handles the following events such as "OnAddedTag", "OnMissedTag", "OnDeleteTag", "OnDoorOpen","OnDoorClose", "DoorOpenGPOHigh", "DoorOpenGPOLow", "DoorCloseGPOHigh", "DoorCloseGPOLow" |
\n
The **InventoryManagementAlertEventActions class** contains the following methods.
\n
| Methods | Description |
| --- | ----------- |
| **InventoryManagementAlertEventActions class** ||
| ``` Public member functions ``` ||
| **doorCloseEventHandler** | This is Inventory Management Door Close Event Handler performs actions such send alert messages on door close. Also performs the reader GpoSet needs, if any |
| **doorOpenEventHandler** | This is Inventory Management Door Open Event Handler performs actions such send alert messages on door open. Also performs the reader GpoSet needs, if any |
\n
The **InventoryManagementExceptionEventArgs class** contains the following methods.

| Methods | Description |
| --- | ----------- |
| **InventoryManagementExceptionEventArgs class** ||
| ``` Public member functions ``` ||
| **InventoryManagementExceptionEventArgs** | InventoryManagementExceptionEventArgs Constructor. This method handles the following events such as "OnUnwantedTag","OnMissedTag", "OnDuplicateTag" |
\n
The **InventoryManagementExceptionEventActions class** contains the following methods.

| Methods | Description |
| --- | ----------- |
| **InventoryManagementExceptionEventActions class** ||
| ``` Public member functions ``` ||
| **markMissedTags** | This method identify and process the missed tags ,if any |
\n
##3. APPENDIX: Basic Tools Installation guide 
The basic installation guide is a technical communication document intended to assist people \n
on how to install a particular tools and program. These steps are detailed below: \n
    1. **Ubuntu.20.04 & dotnet 5.0 Install-STEPS:** \n
    Get your Ubuntu 20.04 and .Net 5.0 installed.\n
    \n
    2. **How to upgrade Linux 18.04 LTS to 20.04 LTS?** \n
    Please follow the steps as mentioned below reference\n 
    links with detailed procedure as given, whichever procedure\n 
    suits your Ubuntu upgrade requirements. \n
    https://www.cyberciti.biz/faq/upgrade-ubuntu-18-04-to-20-04-lts-using-command-line/ \n
    https://ubuntu.com/blog/how-to-upgrade-from-ubuntu-18-04-lts-to-20-04-lts-today \n
    https://www.ubuntu18.com/upgrade-ubuntu-18-04-to-20-04-in-command-line/ \n
    \n
    3. **How to verify upgraded Ubuntu 20.04 LTS version?** \n 
    Check your Ubuntu version: **lsb_release -a** \n 
    \n
    *Observations as below* 
| Description | Values |
| --- | ----------- |
| *Sample outputs:* | No LSB modules are available. |
| *Distributor ID:* | Ubuntu |
| *Description:*    | Ubuntu 20.04 LTS |
| *Release:*  |	20.04 | 
| *Codename:* |	focal | 
    \n
    4. **How to install .NET version 5.0?** \n
    Install .NET5 on Ubuntu 20.04 \n
    The process of installing the .NET5 SDK for development on Ubuntu 20.04 takes 2 steps. \n 
    First, we install the package repository and then we install the .NET5 SDK. \n 
    Just follow the steps as provided in this link: \n
    https://www.davidhayden.me/blog/install-net5-on-ubuntu-20-04 \n
    \n
    5. **How to install doxygen, graphviz and xChm?** \n 
    *sudo apt-get install doxygen* \n 
    *sudo apt install graphviz* \n
    *sudo apt-get install xChm* \n
    \n
    6. **LINUX-ReadAsync-Codelet-CompileRun-Steps:** \n
    *Get your latest mercurapi csharp linux excutables* \n
| Complete below steps ||
| --- | ----------- |
| *download mercuryapi-BILBO-1.37.0.xx.zip* || 
| *unzip mercuryapi-BILBO-1.37.0.xx.zip* || 
| *cd mercuryapi-1.37.0.xx/cs* ||
    \n
    **Presuming you have connected to usb port of m6e devkit board.** \n
    *sudo chmod a+rw /dev/ttyACM0* \n
    \n 
    Then run the ReadAsync sample codelets from Linux folder as below \n
    **NOTE: pre-compiled as is default executables available at Linux folder:** \n
    **if physically usb cable is connected to port USB on m6e devkit** \n
    *Linux/ReadAsync tmr:///dev/ttyACM0 --ant 1,2,3,4* \n 
    OR \n
    **if physically usb cable is connected to port USB/RS232 on m6e devkit** \n
    *Linux/ReadAsync tmr:///dev/ttyUSB0 --ant 1,2,3,4* \n
    \n
    Then to further compile & execute locally by yourself, follow these commands as given below:\n
    \n
    **This will compile and replace Linux/mercuryapi.dll**
| Run below commands ||
| --- | ----------- |
| *dotnet clean ThingMagic.ReaderLinux/ThingMagic.ReaderLinux.csproj* ||
| *dotnet build ThingMagic.ReaderLinux/ThingMagic.ReaderLinux.csproj* ||
    \n
    **This will compile and replace sample ReadAsync in the folder Linux/ReadAsync**
| Run below commands ||
| --- | ----------- |
| *dotnet clean Samples/Codelets/ReadAsyncLinux/ReadAsyncLinux.csproj* ||
| *dotnet build Samples/Codelets/ReadAsyncLinux/ReadAsyncLinux.csproj* ||
    \n
    Then run the ReadAsync codelets as below:\n
    **if physically usb cable is connected to port USB on m6e devkit** \n 
    *Linux/ReadAsync tmr:///dev/ttyACM0 --ant 1,2,3,4* \n  
    OR \n
    **if physically usb cable is connected to port USB/RS232 on m6e devkit** \n 
    *Linux/ReadAsync tmr:///dev/ttyUSB0 --ant 1,2,3,4* \n
    \n
    **Additional NOTES:** For Sample Codelets build, these below additional files are required at runtime to resolve configuration settings and dependencies 
| Executables | Description |
| --- | ----------- |
|    **MyApp.dll** | The managed assembly for MyApp, including an ECMA-compliant entry point token.|
|    **MyApp.exe** | A copy of the corehost.exe executable. |
|    **MyApp.runtimeconfig.json** | this is mandatory configuration file. |
|    **MyApp.deps.json** | A list of dependencies, as well as compilation context data and compilation dependencies. \n Not technically required, but required to use the servicing or package cache/shared package install features. |
    \n
    7. **LINUX-InventoryManagementApp-CompileRun-Steps:** \n
    *Get your latest InventoryManagementAPI csharp linux excutables:* \n 
| Complete below steps ||
| --- | ----------- |
| *download mercuryapi-BILBO-1.37.0.xx.zip* ||
| *unzip mercuryapi-BILBO-1.37.0.xx.zip* ||
| *cd mercuryapi-1.37.0.xx/cs* ||
    \n
    **Presuming you have connected to usb port of m6e devkit board.** \n
    *sudo chmod a+rw /dev/ttyACM0* \n
    \n
    Then run the InventoryManagementApp sample inventory management application
    with inbuilt InventoryManagementAPI.dll (utilities) from Linux folder as below \n
    \n
    **NOTE: pre-compiled as is default executables available at Linux folder:** \n 
    *Linux/InventoryManagementApp* \n
    \n
    **Additional NOTES:** For Sample InventoryManagementApp build ready executables (at folder mercuryapi/cs/Linux/), these are below file details at runtime to resolve configuration settings and dependencies \n
| Executables | Description |
| --- | ----------- |
| **InventoryManagementApp.dll** | The managed assembly App, including an ECMA-compliant entry point token.|
| **InventoryManagementApp** | A copy of the corehost application executable .|
| **InventoryManagementAPI.dll** | The managed inventory management APIs |
| **InventoryManagementAppDefault.config** | The managed inventory management default configuration settings for the application and RFID Reader input parameters |
| **InventoryManagementApp.runtimeconfig.json** | this is runtime mandatory configuration compilation dependencies. |
| **InventoryManagementApp.runtimeconfig.deps.json** | this is mandatory runtime configuration dependencies, if any. |
| **InventoryManagementApp.deps.json** | A list of dependencies, as well as compilation context data and compilation dependencies. \n Not technically required, but required to use the servicing or package cache/shared package install features.\n |
| **InventoryManagementApp.deps.json** | A list of dependencies, as well as compilation context data and compilation dependencies. |
| **System.IO.Ports.dll** | A mandatory nugets.org package installed. Provides classes for controlling serial ports.\n Commonly Used Types: System.IO.Ports.SerialPort |
\n
    Then to further compile & execute locally by yourself, follow these commands as given below:\n
    \n
    **This will compile and replace Linux/InventoryManagementAPI.dll**
| Run below commands ||
| --- | ----------- |
| *dotnet clean ThingMagic.InventoryManagementAPILinux/ThingMagic.InventoryManagementAPI.csproj* || 
| *dotnet build ThingMagic.InventoryManagementAPILinux/ThingMagic.InventoryManagementAPI.csproj* ||
    \n 
    **This will compile and replace sample InventoryManagementApp in the folder Linux/InventoryManagementApp** 
| Run below commands ||
| --- | ----------- |
|  *dotnet clean Samples/InventoryManagementAppLinux/InventoryManagementAppLinux.csproj* ||
| *dotnet build Samples/InventoryManagementAppLinux/InventoryManagementAppLinux.csproj* ||
    \n
    Then run the InventoryManagementApp sample application as below: \n 
    **Linux/InventoryManagementApp** \n
    \n
    8. **LINUX-outInventoryManagement.config** \n 
    The Inventory Management API has capability to do reader parameters saveConfig and also restore output application config parameters too at **Linux/outInventoryManagement.config** \n
\n
    9. **LINUX-InventoryManagementAppDefault.config** \n 
    Standard ThingMagic Reader Load/SaveConfig file format followed on this inventory management application and available at Linux/InventoryManagementAppDefault.config\n
    \n
    //NOTE: This config will be automatically read using Inventory Management Application \n

```
/application/appsettings/uricomport=tmr:///dev/ttyUSB0
/application/appsettings/trigger=1
/application/appsettings/doorhandlergpipin=1
/application/appsettings/dooropengpopin=2
/application/appsettings/rfasyncontime=750
/application/appsettings/rfasyncofftime=250
/application/appsettings/missedtagwaitcycles=3
/application/appsettings/printtaglist=enable
/application/appsettings/printtagtimerinterval=5
/application/alerteventsettings/onaddedtag=addtagalert
/application/alerteventsettings/onmissedtag=missedtagalert
/application/alerteventsettings/ondeletetag=deletetagalert
/application/alerteventsettings/ondooropen=dooropenalert,dooropengpohigh
/application/alerteventsettings/ondoorclose=doorclosealert,doorclosegpolow
/application/exceptioneventsettings/onmissedtag=missedtagexception
/application/exceptioneventsettings/onduplicatetag=duplicatetagexception
/application/writeablereaderparams/LOADCONFIG=Linux/InventoryManagementAppDefault.config
/application/writeablereaderparams/SAVECONFIG=Linux/outInventoryManagement.config
/reader/read/asyncOnTime=1000
/reader/read/asyncOffTime=0
/reader/gen2/accessPassword=00000000
/reader/transportTimeout=5000
/reader/commandTimeout=1000
/reader/baudRate=115200
/reader/probeBaudRates=[9600,115200,921600,19200,38400,57600,230400,460800]
/reader/antenna/txRxMap=[[1,1,1],[2,2,2],[3,3,3],[4,4,4]]
/reader/read/plan=SimpleReadPlan:[Antennas=[1,2,3,4],Protocol=GEN2,Filter=null,Op=null,UseFastSearch=False,Weight=1000]
/reader/radio/readPower=3000
/reader/radio/writePower=3000
/reader/radio/portReadPowerList=[[1,3000],[2,3000],[3,3000],[4,3000]]
/reader/radio/portWritePowerList=[[1,3000],[2,3000],[3,3000],[4,3000]]
/reader/gen2/BLF=LINK250KHZ
/reader/gen2/tari=TARI_25US
/reader/gen2/tagEncoding=M4
/reader/gen2/session=S0
/reader/gen2/target=A
/reader/gen2/q=DynamicQ
/reader/status/antennaEnable=False
/reader/status/frequencyEnable=False
/reader/status/temperatureEnable=False
/reader/regulatory/mode=TIMED
/reader/regulatory/modulation=CW
/reader/regulatory/onTime=500
/reader/regulatory/offTime=0
/reader/tagReadData/enableReadFilter=False
/reader/antenna/checkPort=False

```
