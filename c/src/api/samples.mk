../samples/read.o: $(HEADERS) $(LIB)
read: ../samples/read.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/thingmagic_read.o: $(HEADERS) $(LIB)
thingmagic_read: ../samples/thingmagic_read.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/blockpermalock.o: $(HEADERS) $(LIB)
blockpermalock: ../samples/blockpermalock.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/blockwrite.o: $(HEADERS) $(LIB)
blockwrite: ../samples/blockwrite.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/embeddedreadtid.o: $(HEADERS) $(LIB)
embeddedreadtid: ../samples/embeddedreadtid.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/licensekey.o: $(HEADERS) $(LIB)
licensekey: ../samples/licensekey.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/multiprotocolread.o: $(HEADERS) $(LIB)
multiprotocolread: ../samples/multiprotocolread.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/writetag.o: $(HEADERS) $(LIB)
writetag: ../samples/writetag.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readintoarray.o: $(HEADERS) $(LIB)
readintoarray: ../samples/readintoarray.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readasync.o: $(HEADERS) $(LIB)
readasync: ../samples/readasync.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readasyncgpo.o: $(HEADERS) $(LIB)
readasyncgpo: ../samples/readasyncgpo.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/multireadasync.o: $(HEADERS) $(LIB)
multireadasync: ../samples/multireadasync.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/filter.o: $(HEADERS) $(LIB)
filter: ../samples/filter.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/customantennaconfig.o: $(HEADERS) $(LIB)
customantennaconfig: ../samples/customantennaconfig.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/firmwareload.o: $(HEADERS) $(LIB)
firmwareload: ../samples/firmwareload.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

./samples/locktag.o: $(HEADERS) $(LIB)
locktag: ../samples/locktag.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readasynctrack.o: $(HEADERS) $(LIB)
readasynctrack: ../samples/readasynctrack.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readasyncfilter.o: $(HEADERS) $(LIB)
readasyncfilter: ../samples/readasyncfilter.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/serialtime.o: $(HEADERS) $(LIB)
serialtime: ../samples/serialtime.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/fastid.o: $(HEADERS) $(LIB)
fastid: ../samples/fastid.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)
../samples/tagdir.o: $(HEADERS) $(LIB)
tagdir: ../samples/tagdir.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/onreader-tagdir.o: $(HEADERS) $(LIB)
onreader-tagdir: ../samples/onreader-tagdir.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readerstats.o: $(HEADERS) $(LIB)
readerstats: ../samples/readerstats.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readerInfo.o: $(HEADERS) $(LIB)
readerInfo: ../samples/readerInfo.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readstopTrigger.o: $(HEADERS) $(LIB)
readstopTrigger: ../samples/readstopTrigger.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/rebootReader.o: $(HEADERS) $(LIB)
rebootReader: ../samples/rebootReader.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/readasyncGPIOControl.o: $(HEADERS) $(LIB)
readasyncGPIOControl: ../samples/readasyncGPIOControl.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/loadsaveconfiguration.o: $(HEADERS) $(LIB)
loadsaveconfiguration: ../samples/loadsaveconfiguration.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	

../samples/readallmembanks-GEN2.o: $(HEADERS) $(LIB)
readallmembanks-GEN2: ../samples/readallmembanks-GEN2.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	

../samples/gpiocommands.o: $(HEADERS) $(LIB)
gpiocommands: ../samples/gpiocommands.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	

../samples/authenticate.o: $(HEADERS) $(LIB)
authenticate: ../samples/authenticate.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/untraceable.o: $(HEADERS) $(LIB)
untraceable: ../samples/untraceable.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)

../samples/autonomousmode.o: $(HEADERS) $(LIB)
autonomousmode: ../samples/autonomousmode.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)		
	
../samples/RegulatoryTesting.o: $(HEADERS) $(LIB)
RegulatoryTesting: ../samples/RegulatoryTesting.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	

../samples/returnloss.o: $(HEADERS) $(LIB)
returnloss: ../samples/returnloss.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	
	
../samples/RegionConfiguration.o: $(HEADERS) $(LIB)
RegionConfiguration: ../samples/RegionConfiguration.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	
	
../samples/deviceDetection.o: $(HEADERS) $(LIB)
deviceDetection: ../samples/deviceDetection.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	

../samples/passThrough.o: $(HEADERS) $(LIB)
passThrough: ../samples/passThrough.o $(LIB)
	$(CC) $(CFLAGS) -o $@ $^ -lpthread $(LTKC_LIBS)	
