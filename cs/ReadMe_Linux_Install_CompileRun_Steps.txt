README-Ubuntu.20.04 & dotnet 5.0 Install-STEPS:

Get your Ubuntu 20.04 and .Net 5.0 installed.

How to upgrade Linux 18.04 LTS to 20.04 LTS? 
Please follow the steps as mentioned below reference 
links with detailed procedure as given, whichever procedure 
suits your Ubuntu upgrade requirements. 
https://www.cyberciti.biz/faq/upgrade-ubuntu-18-04-to-20-04-lts-using-command-line/ 
https://ubuntu.com/blog/how-to-upgrade-from-ubuntu-18-04-lts-to-20-04-lts-today 
https://www.ubuntu18.com/upgrade-ubuntu-18-04-to-20-04-in-command-line/ 

How to verify upgraded Ubuntu 20.04 LTS version? 
Check your Ubuntu version:  lsb_release -a 
Sample outputs: 
No LSB modules are available. 
Distributor ID:	Ubuntu 
Description:	Ubuntu 20.04 LTS 
Release:	20.04 
Codename:	focal

How to install .NET version 5.0? 
Install .NET5 on Ubuntu 20.04 
The process of installing the .NET5 SDK for development 
on Ubuntu 20.04 takes 2 steps. First, we install the package 
repository and then we install the .NET5 SDK. 

Just follow the steps as provided in this link: 
https://www.davidhayden.me/blog/install-net5-on-ubuntu-20-04 


README-LINUX-CompileRun-Steps:
Steps are given below 
(How to run and build mercurapi csharp linux excutables):

download mercuryapi-BILBO-1.37.0.xx.zip (from daily buildbot server)
unzip mercuryapi-BILBO-1.37.0.xx.zip

cd mercuryapi-1.37.0.xx/cs

//Presuming you have connected to usb port of m6e devkit board.
sudo chmod a+rw /dev/ttyACM0  

Then run the ReadAsync sample codelets from Linux folder as below 
(NOTE: pre-compiled as is default executables available at Linux folder):
(if physically usb cable is connected to port USB on m6e devkit)
Linux/ReadAsync tmr:///dev/ttyACM0 --ant 1,2,3,4
OR
(if physically usb cable is connected to port USB/RS232 on m6e devkit)
Linux/ReadAsync tmr:///dev/ttyUSB0 --ant 1,2,3,4

Then to further compile & execute locally by yourself, 
follow these commands as given below:

//This will compile and replace Linux/mercuryapi.dll 
dotnet clean ThingMagic.ReaderLinux/ThingMagic.ReaderLinux.csproj
dotnet build ThingMagic.ReaderLinux/ThingMagic.ReaderLinux.csproj

//This will compile and replace sample ReadAsync in the folder Linux/ReadAsync 
dotnet clean Samples/Codelets/ReadAsyncLinux/ReadAsyncLinux.csproj
dotnet build Samples/Codelets/ReadAsyncLinux/ReadAsyncLinux.csproj

Then run the ReadAsync codelets as below:
(if physically usb cable is connected to port USB on m6e devkit)
Linux/ReadAsync tmr:///dev/ttyACM0 --ant 1,2,3,4
OR
(if physically usb cable is connected to port USB/RS232 on m6e devkit)
Linux/ReadAsync tmr:///dev/ttyUSB0 --ant 1,2,3,4


Additional NOTES:(For Sample Codelets build, these below additional files are required
at runtime to resolve configuration settings and dependencies)
MyApp.dll - The managed assembly for MyApp, including an ECMA-compliant entry point token.
MyApp.exe - A copy of the corehost.exe executable.
MyApp.runtimeconfig.json - this is mandatory configuration file.
MyApp.deps.json - A list of dependencies, as well as compilation context data and 
compilation dependencies. Not technically required, but required to use the servicing or 
package cache/shared package install features.

README-LINUX-InventoryTrackerApp-CompileRun-Steps:
Steps are given below
(How to run and build InventoryAPI csharp linux excutables):

download mercuryapi-BILBO-1.37.0.xx.zip (from daily buildbot server)
unzip mercuryapi-BILBO-1.37.0.xx.zip

cd mercuryapi-1.37.0.xx/cs

//Presuming you have connected to usb port of m6e devkit board.
sudo chmod a+rw /dev/ttyACM0

Then run the InventoryTrackerApp sample inventory management application
with inbuilt InventoryAPI.dll (utilities) from Linux folder as below
(NOTE: pre-compiled as is default executables available at Linux folder):
(if physically usb cable is connected to port USB on m6e devkit)
Linux/InventoryTrackerApp 
OR
(if physically usb cable is connected to port USB/RS232 on m6e devkit)
Linux/InventoryTrackerApp 

NOTE: 
1) GPI based trigger to perform TagReadBuildList skeleton development integrated with AutonomousMode.
2) AppSettings are added to Linux/InventoryTrackerApp.dll.config, read automatically from config file. 
(includes COMPort, --ant, --config, Autonomous enable/disable, --trigger parameters too)

Then to further compile & execute locally by yourself,
follow these commands as given below:

//This will compile and replace Linux/InventoryAPI.dll
dotnet clean ThingMagic.InventoryAPILinux/ThingMagic.InventoryAPILinux.csproj
dotnet build ThingMagic.InventoryAPILinux/ThingMagic.InventoryAPILinux.csproj

//This will compile and replace sample InventoryTrackerApp in the folder Linux/InventoryTrackerApp
dotnet clean Samples/InventoryTrackerAppLinux/InventoryTrackerAppLinux.csproj
dotnet build Samples/InventoryTrackerAppLinux/InventoryTrackerAppLinux.csproj

Then run the ReadAsync codelets as below:
(if physically usb cable is connected to port USB on m6e devkit)
Linux/InventoryTrackerApp 
OR
(if physically usb cable is connected to port USB/RS232 on m6e devkit)
Linux/InventoryTrackerApp 

 
README-LINUX-InventoryTrackerApp.dll.config 
(Configuration Management file available at Linux/InventoryTrackerApp.dll.config):
//NOTE: This config will be automatically read using the default 
// System.Configuration.ConfigurationManager.dll utility provided from microsoft dotnet
// tools
<?xml version="1.0"?>
<configuration>
// Currently 2 sections are defined below (WriteableReaderParams /ReadOnlyReaderParams) 
// to monitor parameters or update as needed.
// NOTE: Additional sections can be defined, based on further developments as needed, if any
  <configSections>
     <section name="AppSettings" type="System.Configuration.NameValueSectionHandler"/>
      <section name="ReadOnlyReaderParams" type="System.Configuration.NameValueSectionHandler"/>
      <section name="WriteableReaderParams" type="System.Configuration.NameValueSectionHandler"/>
  </configSections>
  <AppSettings>
     <!-- COMPort of M6e devkit can take values as ttyUSB0 or ttyACM0 -->
     <add key="tmr:///dev/ttyUSB0" value="" />
     <!-- Antennas of M6e devkit can take values from 1,2,3,4 based on availability -->
     <add key="--ant" value="1,2,3,4" />
     <!-- AutononmousMode can take values as "enable" or "disable" -->
     <add key="--enable" value="" />
     <!--config can take values as "saveAndRead" or "save" or "stream" or "verify" or "clear" -->
     <add key="--config" value="saveAndRead" />
     <!--trigger can take values 0=ReadOnReboot or For M6e devkit ReadOnGPI as 1,2,3,4 used GPI pins -->
     <add key="--trigger" value="1" />
  </AppSettings>
// This sections below (WriteableReaderParams) defines the file to LoadConfig/SaveConfig parameters
// NOTE: the file name DeviceConfig.iapp can be changed to anything as needed, if any
  <WriteableReaderParams>
     <!-- For "SAVECONFIG", the default file is DeviceConfig.iapp and can be changed, if any -->
     <add key="SAVECONFIG" value="Linux/DeviceConfig.iapp" />
     <!-- For "LOADCONFIG", the default file is DeviceConfig.iapp and can be changed, if any -->
     <add key="LOADCONFIG" value="Linux/DeviceConfig.iapp" />
  </WriteableReaderParams>
// This sections below (ReadOnlyReaderParams) defines the additional information needed from
// reader as given below runtime to get these details, if needed
  <ReadOnlyReaderParams>
          <add key="/reader/version/model" value="" />
          <add key="/reader/version/serial" value="" />
          <add key="/reader/version/hardware" value="" />
          <add key="/reader/version/software" value="" />
  </ReadOnlyReaderParams>
</configuration>
