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
    /// 加载PB文件
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
    /// 编辑器模式下加载PB文件
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
    /// 真机模式(即AB包模式)下加载PB文件
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="protoPath"></param>
    /// <returns></returns>
    public static string LoadProtoFile_Runtime(string moduleName, string protoPath)
    {
        string assetPath = protoPath + ".bytes";

        TextAsset textAsset = AssetLoader.Instance.CreateAsset<TextAsset>(moduleName, assetPath, SingletonManager.Instance.gameObject);

        // 解密并直接返回

        return AESHelper.Decipher(textAsset.text, AESHelper.keyValue);
    }
}