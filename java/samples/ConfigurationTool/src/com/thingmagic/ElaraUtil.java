
package com.thingmagic;

/**
 *
 * @author bchanda
 */
public class ElaraUtil
{
    /**
     * Returns the bank number associated with the string.
     *
     * @param bank the bank to be used.
     * @return integer number associated with the bank.
     */
    public int getMemBank(String bank)
    {
        int bankNum = 0;
        switch(bank)
        {
            case "RESERVED":
                bankNum = 0;
                break;
            case "EPC":
                bankNum = 1;
                break;
            case "TID":
                bankNum = 2;
                break;
            case "USER":
                bankNum = 3;
                break;
        }
        return bankNum;
    }

    /**
     * Split text into 'n' number of characters.
     *
     * @param text the text to be split.
     * @param size the split size.
     * @return string buffer split text.
     */
    public StringBuffer splitToNChar(String text, int size) {
        StringBuffer sb = new StringBuffer();
        int length = text.length();
        for (int i = 0; i < length; i += size) {
            sb.append(":");
            sb.append(text.substring(i, Math.min(length, i + size)));
        }
        return sb;
    }
}
