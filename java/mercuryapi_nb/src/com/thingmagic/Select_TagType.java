/*  @file Select_TagType.java
 *  @brief Mercury API - tagtype filter
 *  @author bchanda
 *  @date 06/08/2020
 *  Copyright (c) 2023 Novanta, Inc.
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

// TagType filter class and it implements TagFilter
public class Select_TagType implements TagFilter
{
    //TagType 
    public long tagType;
    
    /* <summary>
    Constructor to initialize TagType
    <param name="tagType">tag type</param>*/
    public Select_TagType(long tagType)
    {
        this.tagType = tagType;
    }
    
    /* <summary>
    Test if a tag Matches this filter. Only applies to selects based
    on the TagType.
    <param name="t">tag data to screen</param>
    <returns>Return true to allow tag through the filter.
    Return false to reject tag.</returns> */
    public boolean matches(com.thingmagic.TagData t)
    {
        throw new UnsupportedOperationException("Unsupported operation");
    }
}
