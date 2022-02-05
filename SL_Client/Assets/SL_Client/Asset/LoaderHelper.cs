using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LoaderHelper
{


    /// <summary>
    /// ����PB�ļ�
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="protoPath"></param>
    /// <returns></returns>
    public static string LoadProtoFile(string moduleName, string protoPath)
    {
#if UNITY_EDITOR
        if (GlobalConfig.BundleMode == false)
        {
            return LoadProtoFile_Editor(moduleName, protoPath);
        }
        else
        {
            return LoadProtoFile_Runtime(moduleName, protoPath);
        }
#else
        return LoadProtoFile_Runtime(moduleName, protoPath);
#endif
    }

    /// <summary>
    /// �༭��ģʽ�¼���PB�ļ�
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="protoPath"></param>
    /// <returns></returns>
    public static string LoadProtoFile_Editor(string moduleName, string protoPath)
    {
        string assetPath = Application.dataPath + protoPath.Substring(6);

        string result = File.ReadAllText(assetPath);

        return result;
    }

    /// <summary>
    /// ���ģʽ(��AB��ģʽ)�¼���PB�ļ�
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="protoPath"></param>
    /// <returns></returns>
    public static string LoadProtoFile_Runtime(string moduleName, string protoPath)
    {
        string assetPath = protoPath + ".bytes";

        TextAsset textAsset = AssetLoader.Instance.CreateAsset<TextAsset>(moduleName, assetPath, SingletonManager.Instance.gameObject);

        // ���ܲ�ֱ�ӷ���

        return AESHelper.Decipher(textAsset.text, AESHelper.keyValue);
    }
}