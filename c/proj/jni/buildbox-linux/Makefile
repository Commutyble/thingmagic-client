MERCURYAPI ?= ../../../..

default: linux-amd64 linux-x86


########################################

linux-amd64: $(MERCURYAPI)/java/linux-amd64.lib
linux-x86: $(MERCURYAPI)/java/linux-x86.lib

JNIDEPS += $(MERCURYAPI)/c/src/jni/nativeSerial.c
JNIDEPS += $(MERCURYAPI)/c/src/jni/com_thingmagic_SerialTransportNative.h
JNIDEPS += $(MERCURYAPI)/c/src/api/serial_transport_posix.c
JNIDEPS += $(MERCURYAPI)/c/src/api/tmr_strerror.c

$(MERCURYAPI)/java/linux-amd64.lib: $(JNIDEPS)
	make tmp-linux-64.so
	cp -p tmp-linux-64.so $@
$(MERCURYAPI)/java/linux-x86.lib: $(JNIDEPS)
	make tmp-linux-32.so
	cp -p tmp-linux-32.so $@

tmp-linux-64.so: myhbb-64
	$(call lib-build)
tmp-linux-32.so: myhbb-32
	$(call lib-build, linux32)

define lib-build
	docker run --rm \
	  -v $(abspath $(MERCURYAPI)/c):/io \
	  -v /usr/lib/jvm:/usr/lib/jvm \
	  $< \
	  /hbb_exe/activate-exec \
	  $(1) \
	  make -B -C /io/proj/jni -f Makefile.jni
	cp -p $(MERCURYAPI)/c/proj/jni/libSerialTransportNative.so.0 $@
endef

########################################

myhbb-32: projDocker/myhbb-32/Dockerfile
myhbb-64: projDocker/myhbb-64/Dockerfile

myhbb-%: projDocker/myhbb-%/Dockerfile
	docker images $@ |grep $@ \
	|| \
	docker build $@ \
	  --build-arg USER_ID=$$(id -u) \
	  --build-arg GROUP_ID=$$(id -g) \
	  $(dir $<)


# NOTE: We're not using the latest tag [2.2.0 as of 2021 Jan 22] of
#       holy-build-box-32 because Versions 2.1.0 and 2.2.0 didn't work
#       -- they failed with
#         /opt/rh/devtoolset-8/enable: No such file or directory

HBB32_BASEIMAGE := phusion/holy-build-box-32:2.0.0
HBB64_BASEIMAGE := phusion/holy-build-box-64:2.2.0

projDocker/myhbb-64/Dockerfile: Dockerfile.template
	mkdir -p $(dir $@)
	sed $< -e 's|^FROM.*$$|FROM $(HBB64_BASEIMAGE)|' >$@
projDocker/myhbb-32/Dockerfile: Dockerfile.template
	mkdir -p $(dir $@)
	sed $< -e 's|^FROM.*$$|FROM $(HBB32_BASEIMAGE)|' >$@


DOCKERSAVEDIR ?= ./

# Transform docker image name into a string safe to use in a filename
define escape-filename
	$$(echo $(1) |sed -e 's|[/:]|_|g')
endef

HBB32_BACKUPFILE := $(DOCKERSAVEDIR)$(shell echo $(call escape-filename,$(HBB32_BASEIMAGE))).tar
HBB64_BACKUPFILE := $(DOCKERSAVEDIR)$(shell echo $(call escape-filename,$(HBB64_BASEIMAGE))).tar
test-hbb-backupfilenames:
	# HBB32_BACKUPFILE = $(HBB32_BACKUPFILE)
	# HBB64_BACKUPFILE = $(HBB64_BACKUPFILE)

backup-base-images: backup-base-image-64 backup-base-image-32
backup-base-image-32: $(HBB32_BACKUPFILE)
backup-base-image-64: $(HBB64_BACKUPFILE)
$(HBB32_BACKUPFILE):
	docker save '$(HBB32_BASEIMAGE)' -o $@
$(HBB64_BACKUPFILE):
	docker save '$(HBB64_BASEIMAGE)' -o $@

restore-base-images: restore-base-image-64 restore-base-image-32
restore-base-image-32:
	docker load -i $(HBB32_BACKUPFILE)
restore-base-image-64:
	docker load -i $(HBB64_BACKUPFILE)


########################################

clean:
	-rm -fr projDocker
	-docker image rm myhbb-32 myhbb-64
	-rm -f tmp-linux*.so
	-rm -f $(MERCURYAPI)/java/linux-amd64.lib
	-rm -f $(MERCURYAPI)/java/linux-x86.lib

clean-base-images:
	-docker image rm $$(docker images |docker images |awk '/phusion\/holy-build-box-/{print $$1 ":" $$2}')

tidy: clean clean-base-images
