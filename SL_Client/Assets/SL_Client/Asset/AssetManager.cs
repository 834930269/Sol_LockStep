using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManager : Singleton<AssetManager>
{
    /// <summary>
    /// ��ʼ��ȫ�ֱ���
    /// </summary>
    private void InitGlobal()
    {
        GlobalConfig.HotUpdate = false;
        GlobalConfig.BundleMode = false;
        AssetLoader.Instance = new AssetLoader();
        ModuleManager.Instance = new ModuleManager();
    }
    public async override void Awake()
    {
        InitGlobal();
        ModuleConfig launchModule = new ModuleConfig()
        {
            moduleName = "Lockstep",
            moduleVersion = "*",//���ز���
            moduleUrl = "*"//���ز���
        };
        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result)
        {
            Debuger.Log("��������");
        }
    }

    public override void Update()
    {
        //ִ��ж�ز���
        AssetLoader.Instance.Unload(AssetLoader.Instance.base2Assets);
    }
}
