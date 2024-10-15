Steps to run Read.ino.
-----------------------

1. Open Read.ino file in Arduino IDE. This file is present in the following path in the Mercury API SDK:
<mercuryapi-1.37.3.xx\c\baremetal_proj\arduino_proj\Arduino_Mega2560\Read>

2. Select appropriate board from the menu bar as shown below. Arduino Mega is selected in this example.
<Tools->Board->Arduino AVR Boards->Arduino Mega or Mega 2560>

3. Select comport in Tools->Port.

4. Add Mercury API library from the menu bar as shown below. A copy of the API zip folder (mercuryapi_src.zip) is already provided in the current project folder.
<Sketch->Include Library->Add .ZIP Library->Select "mercuryapi_src.zip" library>.

5. This code sample is made compatible with the M7E modules by default. To make it compatible with M6E modules, the SET_M6E_COMPATIBLE_PARAMS macro must be set to 1.

6. Build and run.
     a) ENABLE_CONTINUOUS_READ macro is used to select either CONTINUOUS READ or TIMED READ. This is set to 1 by default. The CONTINUOUS READ is performed for 500 milliseconds.
     b) If ENABLE_CONTINUOUS_READ macro is set to 0, then the module performs TIMED READ for 500 milliseconds.

Please note that "mercuryapi_src.zip" contains C API source files of the Mercury API Version 1.37.3.xx.

Note- We have a document "Baremetal_Interfacing_Guide" which describes steps to use Mercury C API on Baremetal platforms. It has details regarding how to use API with
      "Arduino Mega 2560" and "Arduino Nano ESP32". Please reach out to  "rfid-support@jadaktech.com"  to get "Baremetal_Interfacing_Guide".

