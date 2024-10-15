########################################

NOTICE: This directory is under reorganization to cater to the
NetBeans project structure.  Initially, NetBeans projects will be
checked in separate from the main sources, but eventually, the main
sources will merge down.  Unix makefiles will track the changes in
directory structure.

This directory is arranged around the NetBeans project structure,
which requires sources to live in a <project>/src folder.

  mercuryapi_nb
    src/com/thingmagic
  samples_nb
    src/samples

Unix makefiles are written to refer to these source directories as appropriate.



########################################
# Rebuilding JNI libraries for Windows

Build the C code in Visual Studio

  (mercuryapi/c/proj/jni or mercuryapi/c/projVS2019/jni)
  You may have to edit the include paths to point to your own JDK installation


Copy the resulting DLLs to mercuryapi/java; e.g.,

  mercuryapi/c/projVS2019/jni/Debug/win-x86.dll -> mercuryapi/java/win-x86.lib
  mercuryapi/c/projVS2019/jni/x64/Debug/win-x64.dll -> mercuryapi/java/win-64.lib

  mercuryapi/c/projVS2019/jni/Debug/win-x86.dll -> mercuryapi/java/mercuryapi_nb/src/com/thingmagic/win-x86.lib
  mercuryapi/c/projVS2019/jni/x64/Debug/win-x64.dll -> mercuryapi/java/mercuryapi_nb/src/com/thingmagic/win-64.lib

  NOTE: !!! We copy to two different locations, one for NetBeans and one for the Makefile.
  TODO: Consolidate down to one .lib location.


Open NetBeans project (mercuryapi/java/mercuryapi_nb/)

  Ignore errors on ConfigurationTool
  Clean and Build mercuryapi_nb
  Run samples_nb to test
    (If it's not running with the options you want,
     change it in the project properties Run tab)

#################################################
!!! NOTE: !!!
You may choose between two versions of ltkjava.jar

 1) ltkjava-1.0.0.6.jar
  includes LTK Java and every library it depends on.  This is easiest to use.

2) ltkjava-1.0.0.6-without-slf4j.jar
  omits the slf4j logging framework.  This prevents conflicts for users who want to provide their own version of SLF4J.

  Note: This still includes a copy of log4j.jar for the non-SLF-enabled parts of LTK, which assume that some version of log4j Version 1 is always present.
  
  2a)slf4j-dependencies.jar
    includes all the parts that were omitted from ltkjava-1.0.0.6-without-slf4j.jar, in case you want to quickly swap in the original components.


slf4j-dependencies.jar which should be used along with ltkjava-1.0.0.6-without-slf4j-logging.jar.


!!! NOTE: !!!
SerialReader users need to include some version of slf4j, but they don't need all of ltkjava-1.0.0.6.jar to get it.  Use slf4j-dependencies.jar instead, or provide your own version of slf4j and a compatible logging backend.