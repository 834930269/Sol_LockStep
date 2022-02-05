using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ģ������� ������
/// </summary>
public class ModuleManager
{
    public static ModuleManager Instance;
    /// <summary>
    /// ����һ��ģ��,Ψһ����API����
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="moduleAction"></param>
    public async Task<bool> Load(ModuleConfig moduleConfig)
    {
        if(GlobalConfig.HotUpdate == false)
        {
            //����ر����ȸ�,ֱ�ӱ��ؼ���,����ab
            if(GlobalConfig.BundleMode == false)
            {
                return true;
            }
            else
            {
                bool baseBundleOK = await LoadBase_Bundle(moduleConfig.moduleName);

                if(baseBundleOK == false)
                {
                    return false;
                }

                return await LoadBase(moduleConfig.moduleName);
            }
        }
        else
        {
            //�ȸ�
            await Downloader.Instance.Download(moduleConfig);

            bool updateBundleOK = await LoadUpdate_Bundle(moduleConfig.moduleName);

            if(updateBundleOK == false)
            {
                return false;
            }

            bool baseBundleOK = await LoadBase_Bundle(moduleConfig.moduleName);

            if(baseBundleOK == false)
            {
                return false;
            }

            bool updateOk = await LoadUpdate(moduleConfig.moduleName);

            return updateOk;
        }
    }

    /// <summary>
    /// ���ػ�����
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<bool> LoadBase(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Base, moduleName, moduleName.ToLower() + ".json");

        if(moduleABConfig == null)
        {
            return false;
        }

        Debug.Log($"ģ��{moduleName}��ֻ��·�� ������AB��������: {moduleABConfig.BundleArray.Count}");

        Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);

        AssetLoader.Instance.base2Assets.Add(moduleName, Path2AssetRef);

        return true; 
    }

    /// <summary>
    /// ��Դ��ӳ��
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<bool> LoadUpdate(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");

        if (moduleABConfig == null)
        {
            return false;
        }

        Debug.Log($"ģ��{moduleName}�Ŀɶ���д·�� ������AB��������: {moduleABConfig.BundleArray.Count}");

        Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);

        AssetLoader.Instance.update2Assets.Add(moduleName, Path2AssetRef);

        return true;
    }


    private async Task<bool> LoadUpdate_Bundle(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");

        if(moduleABConfig == null)
        {
            Debug.LogError("LoadUpdate_Bundle...");

            return false;
        }

        foreach(KeyValuePair<string,BundleInfo> keyValue in moduleABConfig.BundleArray)
        {
            string bundleName = keyValue.Key;

            BundleInfo bundleInfo = keyValue.Value;

            AssetLoader.Instance.name2BundleRef[bundleName] = new BundleRef(bundleInfo, BaseOrUpdate.Update);
        }

        return true;
    }

    private async Task<bool> LoadBase_Bundle(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Base, moduleName, moduleName.ToLower() + ".json");

        if(moduleABConfig == null)
        {
            Debug.LogError("LoadBase_Bundle...");

            return false;
        }

        foreach(KeyValuePair<string,BundleInfo> keyValue in moduleABConfig.BundleArray)
        {
            string bundleName = keyValue.Key;

            if(AssetLoader.Instance.name2BundleRef.ContainsKey(bundleName) == false)
            {
                BundleInfo bundleInfo = keyValue.Value;

                AssetLoader.Instance.name2BundleRef[bundleName] = new BundleRef(bundleInfo,BaseOrUpdate.Base);
            }
        }
        return true;
    }
}
