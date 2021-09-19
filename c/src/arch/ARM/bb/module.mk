#
#  BeagleBone Linux Tools specification
#

TOOLS           = gcc-linaro
PROCESSOR       = arm
BB_ARM_CC_TOOLS = bbArmCc-tools
BB_ARM_CC_DEPS  = bbArmCc-deps

#
# Paths
#
TOOLS_ROOT      ?= /usr/local/$(BB_ARM_CC_TOOLS)
TOOLS_PATH	?= $(TOOLS_ROOT)/bin
TOOLS_LIB_PATH	?= $(TOOLS_ROOT)/lib
TOOLS_INC       ?= $(TOOLS_ROOT)/include
DEPS_PATH       ?= /usr/local/$(BB_ARM_CC_DEPS)

ifeq (,$(wildcard $(TOOLS_PATH)))
      $(info Beaglebone ARM cross compiler tools not found in $(TOOLS_PATH)/ directory)
      $(info Download bbArmCc-tools.zip from ThingMagic website or Download gcc-linaro cross compiler toolchain)
      $(info Unzip & copy to /usr/local/ directory as "bbArmCc-tools" in your linux-PC)
endif

ifeq (,$(wildcard $(DEPS_PATH)))
      $(info Beaglebone ARM cross compiler dependecies not found in $(DEPS_PATH)/ directory)
      $(info Download bbArmCc-deps.zip from ThingMagic website, unzip & copy /usr/local/ as "bbArmCc-deps" in your linux-PC)
endif

#
# Tools 
#
CC	= $(TOOLS_PATH)/arm-linux-gnueabihf-gcc
C++     = $(TOOLS_PATH)/arm-linux-gnueabihf-g++
CiPP	= $(TOOLS_PATH)/arm-linux-gnueabihf-g++
OBJDUMP = $(TOOLS_PATH)/arm-linux-gnueabihf-objdump
ASM	= $(TOOLS_PATH)/arm-linux-gnueabihf-gcc
AR	= $(TOOLS_PATH)/arm-linux-gnueabihf-ar
LD      = $(TOOLS_PATH)/arm-linux-gnueabihf-ld

ifeq ($(BUILD), Debug)
STRIP ?= ls
else
STRIP = $(TOOLS_PATH)/arm-linux-gnueabihf-strip
endif

CFLAGS  += -DPC
CFLAGS  += -I $(DEPS_PATH)/include

ETH_NAME ?= eth0

ifeq ($(BUILD), Debug)
CFLAGS          += -g
CDEFINES        += -DDEBUG
endif

