/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.thingmagic;

/**
 *
 * @author Jadaktech
 */
public class CustomAntConfigPerAntenna
{
    Gen2.Session session = null;
    Gen2.Target target = null;
    TagFilter filter = null;
    int antID = 0;

    public CustomAntConfigPerAntenna(Gen2.Session session, Gen2.Target target, TagFilter filter, int antID)
    {
        this.session = session;
        this.target = target;
        this.filter = filter;
        this.antID = antID;
    }
}
