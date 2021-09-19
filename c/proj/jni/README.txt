Native serial drivers for the JNI portion of Java Mercury API.

Build here, then copy the resulting shared libaries / DLLs to
mercuryapi/java before building mercuryapi.jar.

(Some of the copying might already be implemented in these project
files, but this support is inconsistent.)


Windows 64-bit
  mercuryapi/c/projVS2019/jni/jni.sln
    Build Release / x64 config
  Copy mercuryapi/c/projVS2019/jni/x64/Release/win-x64.dll
    to mercuryapi/java/win-x64.lib and
       mercuryapi/java/mercuryapi_nb/src/com/thingmagic/win-x64.lib

Windows 32-bit
  mercuryapi/c/projVS2019/jni/jni.sln
    Build Release / Win32 config
  Copy mercuryapi/c/projVS2019/jni/Release/win-x86.dll
    to mercuryapi/java/win-x86.lib and
       mercuryapi/java/mercuryapi_nb/src/com/thingmagic/win-x86.lib
    

Linux 64-bit
Linux 32-bit

  make -C mercuryapi/c/proj/jni/buildbox-linux

  This Makefile automatically copies results to
    mercuryapi/java/linux-amd64.lib
    mercuryapi/java/linux-x86.lib

  Prerequisites:
    openjdk
    docker
      (Internet connection to Docker Hub to pull phusion/holy-build-box images
       OR use "docker load" with the files in tm-svn/repos/swtree/archive/phusion/)


Linux ARM
  ???

MacOS
  ???
