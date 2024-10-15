REMOTEHOST ?= debian@sargas-8b7726
MAPI_DIR ?= mercuryapi-1.35.2.32

default: clean-build run

clean-build:
	rsync -aP readasyncgpo.c $(REMOTEHOST):$(MAPI_DIR)/c/src/samples/
	rsync -aP ../api/Makefile ../api/*.mk $(REMOTEHOST):$(MAPI_DIR)/c/src/api/
	ssh $(REMOTEHOST) 'rm -f $(MAPI_DIR)/c/src/samples/readasyncgpo.o; make -C $(MAPI_DIR)/c/src/api'

run:
	ssh $(REMOTEHOST) $(MAPI_DIR)/c/src/api/readasyncgpo tmr://localhost --ant 1
