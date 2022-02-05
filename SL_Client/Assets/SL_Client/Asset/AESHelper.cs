using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class AESHelper
{
    /// <summary>
    /// AES���ܵ���Կ ������32λ
    /// </summary>
    public static string keyValue = "12345678123456781234567812345678";

    /// <summary>
    /// AES �㷨����
    /// </summary>
    /// <param name="content">����</param>
    /// <param name="Key">��Կ</param>
    /// <returns>���ܺ������</returns>
    public static string Encrypt(string content, string Key)
    {
        try
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);

            RijndaelManaged rDel = new RijndaelManaged();

            rDel.Key = keyBytes;

            rDel.Mode = CipherMode.ECB;

            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();

            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            byte[] resultBytes = cTransform.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            string result = Convert.ToBase64String(resultBytes, 0, resultBytes.Length);

            return result;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("���ܳ���" + ex.ToString());

            return null;
        }
    }

    /// <summary>
    /// AES �㷨����
    /// </summary>
    /// <param name="content"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Decipher(string content, string key)
    {
        try
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            RijndaelManaged rm = new RijndaelManaged();

            rm.Key = keyBytes;

            rm.Mode = CipherMode.ECB;

            rm.Padding = PaddingMode.PKCS7;

            ICryptoTransform ict = rm.CreateDecryptor();

            byte[] contentBytes = Convert.FromBase64String(content);

            byte[] resultBytes = ict.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

            return Encoding.UTF8.GetString(resultBytes);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("���ܳ���" + ex.ToString());

            return null;
        }
    }
}
