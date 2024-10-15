# MSBuild
# Note multiple versions, each one for a different version of the .NET framework
# v3.5 is capable of building older v2.0 projects, but I'm afraid of accidentally
# introducing forward dependencies on the 3.5 framework.
MSBUILD2 ?= "/cygdrive/c/WINDOWS/Microsoft.NET/Framework/v2.0.50727/MSBuild.exe"
MSBUILD3 ?= "/cygdrive/c/WINDOWS/Microsoft.NET/Framework/v3.5/MSBuild.exe"

VCBUILD8 ?= "/cygdrive/c/Program Files/Microsoft Visual Studio 8/vc/vcpackages/vcbuild.exe"

#MSBuild for visual studio 2022
MSBUILD22 ?= "/cygdrive/c/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe"

# Add path to VCBuild
VCBUILDPATH ?= "/cygdrive/c/Program Files/Microsoft Visual Studio 8/VC/vcpackages"


default: Samples.sln
	$(MSBUILD22) /t:Rebuild /p:Configuration=Release Samples.sln
clean:
	rm -fr *-Release/*  *-Debug/*

