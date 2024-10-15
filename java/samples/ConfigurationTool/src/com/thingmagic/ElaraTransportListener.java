/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.thingmagic;

/**
 * The listener interface is for JSON messages which are sent to 
 * the Elara reader in CDC mode. 
 */
public interface ElaraTransportListener {
    public void message(boolean tx, String data);
}
