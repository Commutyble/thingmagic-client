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
public class CustomAntConfig
{
    int antCount = 0;
    boolean perAntFastSearch = false;
    CustomAntConfigPerAntenna[] customConfigPerAnt;
    int antSwitchingType = 0;
    int tagReadTimeout = 50000;

    public CustomAntConfig(int antCount, CustomAntConfigPerAntenna[] customConfigPerAnt, boolean perAntFastSearch, int antSwitchingType, int tagReadTimeout)
    {
        this.antCount = antCount;
        this.perAntFastSearch = perAntFastSearch;
        this.customConfigPerAnt = customConfigPerAnt;
        this.antSwitchingType = antSwitchingType;
        this.tagReadTimeout = tagReadTimeout;
    }
}
