using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetManager : Singleton<AssetManager>
{
    /// <summary>
    /// 初始化全局变量
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
            moduleVersion = "*",//本地测试
            moduleUrl = "*"//本地测试
        };
        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result)
        {
            Debuger.Log("加载正常");
        }
    }

    public override void Update()
    {
        //执行卸载策略
        AssetLoader.Instance.Unload(AssetLoader.Instance.base2Assets);
    }
}
