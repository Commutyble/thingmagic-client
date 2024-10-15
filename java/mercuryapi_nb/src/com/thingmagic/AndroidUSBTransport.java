/*
 * Copyright (c) 2023 Novanta, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
package com.thingmagic;

import java.io.IOException;
import java.io.PipedInputStream;
import java.io.PipedOutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 *
 * 
 */
public class AndroidUSBTransport implements SerialTransport 
{
    private AndroidUsbReflection androidUSBReflection = null;
    private static final int DEFAULT_READ_BUFFER_SIZE = 256;
    private static final int DEFAULT_WRITE_BUFFER_SIZE = 256;
    private Object mWriteBufferLock;
    private byte[] writeBuffer;

    // Buffer Size should be >= 16KB, to accomodate the worst case of
    // having to empty an entire D2XX driver buffer.
    //
    // I am doubling this to 32KB for safety.  An extra 16KB RAM
    // should be easily available in Android. [Harry Tsai 20180531]
    //
    private final int READBUF_SIZE = 32*1024;

    private static PipedInputStream iStream = null;
    private static PipedOutputStream oStream = null;

    private volatile boolean enableReadThread = true;

    private static void log(Level level, String message)
    {
        Logger.getLogger(AndroidUSBTransport.class.getName())
            .log(level, message);
    }
    private static void log(String message)
    {
        /* Interfacing java.util.logging with Android logcat is kind
         * of funky.
         *
         * The default configuration doesn't show anything finer than
         * Level.INFO, and the most common fix didn't work for me
         * [Harry Tsai 20180601].
         * (https://stackoverflow.com/questions/4561345/how-to-configure-java-util-logging-on-android#answer-9047282)
         *
         * So for now, just hard-code the level.  When debugging,
         * uncomment Level.INFO and recompile Mercury API.  For
         * production, leave it set to Level.FINE.
         */
        log(
            Level.FINE,
            //Level.INFO,
            message);
    }
    private static void log(Exception ex)
    {
        StringWriter sw = new StringWriter();
        ex.printStackTrace(new PrintWriter(sw));
        log(Level.SEVERE, sw.toString());
    }

    AndroidUSBTransport()
    {
        androidUSBReflection = new AndroidUsbReflection();
        writeBuffer = new byte[DEFAULT_WRITE_BUFFER_SIZE];
        mWriteBufferLock = new Object();
    }

    public void open() throws ReaderException {
        try
        {
           boolean isOpen = androidUSBReflection.isOpen();
           if(!isOpen)
           {
               throw new ReaderException("Couldn't open device");
           }
           flush();
           startRead();
        }
        catch (Exception ex)
        {
            log(ex);
            shutdown();
            throw new ReaderException("Couldn't open device");
        }
    }

    public void sendBytes(int length, byte[] message, int offset, int timeoutMs) throws ReaderException
    {
        try
        {
            int writeSize;
            int writtenSize = 0;
            writeSize = length;

            while (writtenSize < writeSize)
            {
                writeBuffer = new byte[DEFAULT_WRITE_BUFFER_SIZE];
                System.arraycopy(message, (offset+writtenSize), writeBuffer, 0, writeSize);
                synchronized (mWriteBufferLock)
                {
                    writtenSize += androidUSBReflection.write(writeBuffer, writeSize);
                }
            }
        }
        catch (Exception ex)
        {
            log(ex);
            throw new ReaderCommException("Send error");
        }
    }

    public byte[] receiveBytes(int length, byte[] messageSpace, int offset, int timeoutMillis) throws ReaderException
    {
        try
        {
            int bytesRead = 0;
            long startTime = System.currentTimeMillis();
            messageSpace = (messageSpace == null) ? new byte[DEFAULT_READ_BUFFER_SIZE] : messageSpace;

            while (bytesRead < length)
            {
                long elapsedMs = System.currentTimeMillis() - startTime;
                if (elapsedMs >= timeoutMillis)
                {
                    Exception ex = new ReaderCommException(String.format("Timeout (%d > %d ms)", 
                                                                         elapsedMs, timeoutMillis));
                    log("receiveBytes throwing exception: "+ex.toString());
                    throw ex;
                }

                /* Always check if istream is available before
                 * reading, because read() blocks forever 
                 * if no bytes ever come in(e.g., when reader is
                 * turned off.) */
                // When stop read command is sent, the pipe is said to be broken if no tags in the field, which is resulting in missing of
                // stop read response(Bug#7694). Inorder to receive stop read response, always check for condition (iStream.available() < READBUF_SIZE).
                if(iStream.available() < READBUF_SIZE)
                {
                    while(iStream.available() < (length-bytesRead))
                    {
                        Thread.sleep(1);

                        long elapsedTimeMs = System.currentTimeMillis() - startTime;
                        if (elapsedTimeMs >= timeoutMillis)
                        {
                            Exception ex = new ReaderCommException(String.format("Timeout (%d > %d ms)", 
                                                                                 elapsedTimeMs, timeoutMillis));
                            log("receiveBytes throwing exception: "+ex.toString());
                            throw ex;
                        }
                    }

                    int readLen = iStream.read(messageSpace, offset+bytesRead, length-bytesRead);
                    if (readLen > 0)
                    {
                        bytesRead += readLen;
                        log(String.format("readbuf -%d bytes (iStream.available = %d))", length, iStream.available())); 
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new ReaderCommException(ex.getMessage());
        }
        return messageSpace;
    }

    private void stopRead()
    {
        enableReadThread = false;
    }

    private void startRead()
    {
        enableReadThread = true;
        Thread rt = new Thread(readThread);
        rt.start();
    }

    private Runnable readThread = new Runnable() {
        @Override
        public void run()
        {
            // run() is not allowed to throw exceptions, so we have to catch them all
            try
            {
                while(enableReadThread)
                {
                    // Don't poll the FTDI driver too often, or we will steal too much time from its worker thread.
                    // Sleep 50 to 100 ms between polls.
                    Thread.sleep(100);
                
                    int qlen = androidUSBReflection.getstatusQ();
                    if (qlen > 16384)
                    {
                        log(new Exception("0xBABA:FTDI BUFFER FULL"));
                    }
                    if (qlen > 0)
                    {
                        byte[] readBuff = new byte[qlen];
                        {
                            log(String.format("qlen=%d", qlen));

                            int len = androidUSBReflection.read(readBuff);
                            log(String.format("readlen=%d", len));

                            {
                                try
                                {
                                    oStream.write(readBuff, 0, readBuff.length);
                                    oStream.flush();
                                }
                                catch (IOException ex)
                                {
                                    String EXMESSAGE = ex.getMessage().toUpperCase();
                                    if (false
                                        || EXMESSAGE.contains("PIPE BROKEN")
                                        || EXMESSAGE.contains("PIPE IS CLOSED")
                                        || EXMESSAGE.contains("PIPE NOT CONNECTED")
                                        )
                                    {
                                        /* "Pipe broken" is non-fatal.  Ignore it.
                                         *
                                         * "Pipe broken" just means that whatever thread last read
                                         * from the other end of the pipe has died.  This is a
                                         * normal occurence when stopReading is called, because it
                                         * tears down all the background read threads.
                                         *
                                         * This is ignoreable because the data still goes into
                                         * PipedInputStream's buffer, ready for the next time
                                         * startReading creates a reader thread.
                                         *
                                         * Similarly, "Pipe is closed" means we just haven't gotten
                                         * around to calling startReading yet, and "Pipe not
                                         * connected" means we have yet to call startReading for the
                                         * first time.
                                         */
                                    }
                                    else
                                    {
                                        throw ex;
                                    }
                                }
                                log(String.format("readbuf +%d bytes (iStream.available = %d)", readBuff.length, iStream.available()));
                            }
                        }
                    }
                    else if(qlen < 0)
                    {
                        /* Non-recoverable error
                         * 
                         * "suggests an error in the parameters of the function, or a fatal error
                         * like a USB disconnect has occurred."
                         * (according to "D2XX Programmer's Guide (FT_000071)")
                         */
                        Exception ex = new Exception(String.format(" 0xDADA:FTDI ERROR: qlen=%d", qlen));
                        log("readThread throwing exception: "+ex);
                        throw ex;
                    }
                    if (!enableReadThread)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                log("Fatal readThread error");
                log(ex);
                try { shutdown(); } catch (Exception exx) {}
            }
        } // end of run()
    }; // end of runnable

    int baud = 115200;
    public int getBaudRate() throws ReaderException
    {
        return baud;
    }

    public void setBaudRate(int baudRate) throws ReaderException
    {
        boolean isBaudSet = false;
        isBaudSet = androidUSBReflection.setBaudRate(baudRate);
        if(isBaudSet)
        {
            baud =  baudRate;
        }
    }

    public void flush() throws ReaderException
    {
        try
        {
            PipedInputStream iStream0 = iStream;
            PipedOutputStream oStream0 = oStream;
            iStream = new PipedInputStream(READBUF_SIZE);
            oStream = new PipedOutputStream(iStream);
            if (null != iStream0) { iStream0.close(); }
            if (null != oStream0) { oStream0.close(); }
            androidUSBReflection.reSet();
            // call purge from here
            setBaudRate(baud);
        }
        catch (Exception ex)
        {
            throw new ReaderException(ex.getMessage());
        }
    }

    public void shutdown() throws ReaderException
    {
        try
        {
            stopRead();
            androidUSBReflection.close();
        }
        catch (Exception ex)
        {
            throw new ReaderException(ex.getMessage());
        }
    }

    public SerialReader createSerialReader(String uri) throws ReaderException
    {
        throw new UnsupportedOperationException("Not supported yet.");
    }
}
