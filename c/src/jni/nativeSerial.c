
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>

#include "com_thingmagic_SerialTransportNative.h"

#include "tmr_status.h"
#include "tmr_serial_transport.h"

static jfieldID transportPtrID;

#define GET_TRANSPORT() ((void *)(intptr_t)(*env)->GetLongField(env, obj, transportPtrID))

static void
throwCommError(JNIEnv *env, TMR_Status status)
{
    jclass cls = (*env)->FindClass(env, "com/thingmagic/ReaderCommException");
    /* if cls is NULL, an exception has already been thrown */
    if (cls != NULL) {
      (*env)->ThrowNew(env, cls, TMR_strerror(status));
    }
    /* free the local ref */
    (*env)->DeleteLocalRef(env, cls);
}

/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeInit
 * Signature: ()I
 */
JNIEXPORT jint JNICALL Java_com_thingmagic_SerialTransportNative_nativeInit
  (JNIEnv *env, jclass class)
{

  if (NULL == transportPtrID)
  {
    transportPtrID = (*env)->GetFieldID(env, class, "transportPtr", "J");
  }

  return 0;
}

/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeCreate
 * Signature: (Ljava/lang/String;)I
 */
JNIEXPORT jint JNICALL Java_com_thingmagic_SerialTransportNative_nativeCreate
  (JNIEnv *env, jobject obj, jstring port)
{
  TMR_SR_SerialTransport *transport;
  TMR_SR_SerialPortNativeContext *context;
  const char *portStr;
  TMR_Status ret;

  transport = malloc(sizeof(*transport));
  if(NULL == transport)
  {
     ret = TMR_ERROR_OUT_OF_MEMORY;
    throwCommError(env, ret);
    return ret;
   }
   context = malloc(sizeof(*context));
   if(NULL == context)
   {
      ret = TMR_ERROR_OUT_OF_MEMORY;
      throwCommError(env, ret);
      return ret;
   }
   portStr = (*env)->GetStringUTFChars(env, port, NULL);
   if (NULL == portStr)
   {
      return 0; /* OutOfMemoryError already thrown */
   }

  ret = TMR_SR_SerialTransportNativeInit(transport, context, portStr);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
    return ret;
  }
  
  (*env)->SetLongField(env, obj, transportPtrID, (jlong)
#if defined(__i386__)
		       /* (jlong) [which is always a 64-bit integer]
			* is the correct type to pass to SetLongField,
			* but some 32-bit compilers complain that
			* we're implicitly changing the size of the
			* "transport" pointer from 32 to 64 bits.
			* 
			* To keep these 32-bit compilers happy, cast
			* to a 32-bit type first to signal that we're
			* fully aware we have a 32-bit source. */
		       (int32_t)
#endif		       
		       transport);
  (*env)->ReleaseStringUTFChars(env, port, portStr);

  return ret;
}


/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeOpen
 * Signature: ()I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeOpen
  (JNIEnv *env, jobject obj)
{
  TMR_SR_SerialTransport *transport;
  TMR_Status ret;

  transport = (void *)(intptr_t)(*env)->GetLongField(env, obj, transportPtrID);
  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }

  ret = transport->open(transport);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  return ret;
}

/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeSendBytes
 * Signature: (I[BII)I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeSendBytes
  (JNIEnv *env, jobject obj, jint length, jbyteArray message, jint offset,
   jint timeoutMs)
{
  TMR_SR_SerialTransport *transport;
  jbyte *messageBytes;
  TMR_Status ret;

  transport = GET_TRANSPORT();
  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }

  messageBytes = (*env)->GetByteArrayElements(env, message, NULL);
  if (NULL == messageBytes)
  {
    return 0; /* exception already thrown */
  }

  ret = transport->sendBytes(transport, length,
                             (uint8_t *)messageBytes + offset,
                             timeoutMs);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  (*env)->ReleaseByteArrayElements(env, message, messageBytes, 0);

  return ret;
}

/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeReceiveBytes
 * Signature: (I[BII)I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeReceiveBytes
  (JNIEnv *env, jobject obj, jint length, jbyteArray message, jint offset,
   jint timeoutMs)
{
  TMR_SR_SerialTransport *transport;
  jbyte *messageBytes;
  TMR_Status ret;

  transport = GET_TRANSPORT();
  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }

  messageBytes = (*env)->GetByteArrayElements(env, message, NULL);
  if (NULL == messageBytes)
  {
    return 0; /* exception already thrown */
  }
  {
	uint32_t messageLength;
	ret = transport->receiveBytes(transport, length, &messageLength,
                                (uint8_t *)messageBytes + offset,
                                timeoutMs);
  }
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  (*env)->ReleaseByteArrayElements(env, message, messageBytes, 0);

  return ret;
}

/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeSetBaudRate
 * Signature: (I)I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeSetBaudRate
  (JNIEnv *env, jobject obj, jint rate)
{
  TMR_SR_SerialTransport *transport;
  TMR_Status ret;

  transport = GET_TRANSPORT();
  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }

  ret = transport->setBaudRate(transport, rate);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  return ret;
}


/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeGetBaudRate
 * Signature: ()I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeGetBaudRate
  (JNIEnv *env, jobject obj)
{

  return 0xFFFF;
}


/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeShutdown
 * Signature: ()I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeShutdown
  (JNIEnv *env, jobject obj)
{

  TMR_SR_SerialTransport *transport;
  TMR_Status ret;

  transport = GET_TRANSPORT();

  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }
  ret = transport->shutdown(transport);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  (*env)->SetLongField(env, obj, transportPtrID, 0L);

  /** @todo Is this the right place to free these? */
  free(transport->cookie);
  transport->cookie = NULL;
  free(transport);
  return ret;
}


/*
 * Class:     com_thingmagic_SerialTransportNative
 * Method:    nativeFlush
 * Signature: ()I
 */
JNIEXPORT jint JNICALL
Java_com_thingmagic_SerialTransportNative_nativeFlush
  (JNIEnv *env, jobject obj)
{
  TMR_SR_SerialTransport *transport;
  TMR_Status ret;

  transport = GET_TRANSPORT();
  if(NULL == transport)
  {
     ret = TMR_ERROR_NULL_POINTER;
     throwCommError(env, ret);
     return ret;
  }
  ret = transport->flush(transport);
  if (TMR_SUCCESS != ret)
  {
    throwCommError(env, ret);
  }

  return ret;
}




