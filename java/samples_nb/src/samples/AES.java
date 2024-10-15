package samples;

import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

public class AES 
{
    public static byte[] decrypt(byte[] key , byte[] ciphertext) throws Exception 
    {
        SecretKeySpec secretKeySpec = new SecretKeySpec(key, "AES");
        Cipher cipher = Cipher.getInstance("AES/CBC/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, secretKeySpec, new IvParameterSpec(new byte[16]));
        byte[] recoveredtext = cipher.doFinal(ciphertext);
        return recoveredtext;
    }
}
