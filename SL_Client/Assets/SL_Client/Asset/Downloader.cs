using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 下载器 工具类
/// 从服务端下载模块对应的AB配置文件,和本地的AB配置文件作比对
/// 根据比对查找哪些Bundle发生了变化
/// 下载有差异的文件
/// 删除没必要的文件
/// 
/// </summary>
public class Downloader : Singleton<Downloader>
{
    /// <summary>
    /// 根据模块的配置,下载对应的模块
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="action"></param>
    public async Task Download(ModuleConfig moduleConfig) {
        //用来存放热更下来的资源的本地路径
        string updatePath = GetUpdatePath(moduleConfig.moduleName);

        //远程服务器上这个模块的AB资源配置文件的URL
        string configURL = GetServerURL(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");

        UnityWebRequest request = UnityWebRequest.Get(configURL);
        request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/{1}_temp.json", updatePath, moduleConfig.moduleName));
        
        //先保存为temp配置文件

        Debug.Log("下载到本地路径: " + updatePath);
        
        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error) == false)
        {
            //下载出错
            Debug.LogWarning($"下载模块{moduleConfig.moduleName}的AB配置文件: {request.error}");
            bool result = await ShowMessageBox("网络异常,请检查网络后点击 继续下载", "继续下载", "退出下载");

            if(result == false)
            {
                Application.Quit();
                return;
            }
            //点击确认的话,重试下载
            await Download(moduleConfig);
            return;
        }

        //AB配置文件下载好了,获取需要下载的列表
        Tuple<List<BundleInfo>,BundleInfo[]> tuple = await GetDownloadList(moduleConfig.moduleName);

        List<BundleInfo> downloadList = tuple.Item1;

        BundleInfo[] removeList = tuple.Item2;

        long downLoadSize = CalculateSize(downloadList);

        if(downLoadSize == 0)
        {
            //没有需要下载的
            //把removeList中的给删掉
            Clear(moduleConfig,removeList);
            return;
        }

        //阻塞到这里,等待确认是否下载
        bool boxResult = await ShowMessageBox(moduleConfig, downLoadSize);
        if(boxResult == false)
        {
            Application.Quit();
            return;
        }
        //执行下载
        await ExecuteDownload(moduleConfig, downloadList);
        //删除掉需要删除的东西
        Clear(moduleConfig,removeList);

        return;
    }

    /// <summary>
    /// 执行下载行为
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="bundleList"></param>
    /// <returns></returns>
    private async Task ExecuteDownload(ModuleConfig moduleConfig,List<BundleInfo> bundleList)
    {
        while(bundleList.Count > 0)
        {
            BundleInfo bundleInfo = bundleList[0];
            UnityWebRequest request = UnityWebRequest.Get(GetServerURL(moduleConfig, bundleInfo.bundle_name));

            string updatePath = GetUpdatePath(moduleConfig.moduleName);

            request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/" + bundleInfo.bundle_name, updatePath));
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("下载资源：" + bundleInfo.bundle_name + " 成功");
                //下载成功了,把第一个元素给移除
                bundleList.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
        
        //这里逻辑是这样的,如果bundleList不为空,代表还有一部分资源没有下载完成
        //所以需要再次下载
        //这里弹出一个MessageBox,如果确定,就继续下载
        if(bundleList.Count > 0)
        {
            bool result = await ShowMessageBox("网络异常,请检查网络后点击 继续下载", "继续下载", "退出游戏");

            if(result == false)
            {
                Application.Quit();
                return;
            }
            await ExecuteDownload(moduleConfig, bundleList);

            return;
        }
    }


    /// <summary>
    /// 对于给定模块,返回其所有需要下载的BundleInfo组成的List
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<Tuple<List<BundleInfo>, BundleInfo[]>> GetDownloadList(string moduleName)
    {
        //Update条件下服务端和本地获取的是服务端的配置文件,和本地热更文件夹里的文件进行对比
        ModuleABConfig serverConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + "_temp.json");

        if(serverConfig == null)
        {
            return null;
        }

        ModuleABConfig localConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");

        //注意: 这里不用判断localConfig是否存在,本地的localConfig雀食可能不存在,比如在此模块第一次热更新之前,本地update路径下啥都没有

        return CalculateDiff(moduleName, localConfig, serverConfig); 
    }

    /// <summary>
    /// 通过两个AB资源配置文件,对比出有差异的Bundle
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="localConfg"></param>
    /// <param name="serverConfig"></param>
    /// <returns></returns>
    private Tuple<List<BundleInfo>, BundleInfo[]> CalculateDiff(string moduleName, ModuleABConfig localConfig, ModuleABConfig serverConfig)
    {
        // 记录需要下载的bundle文件列表
        List<BundleInfo> bundleList = new List<BundleInfo>();
        // 记录本地已经存在的bundle文件列表
        Dictionary<string, BundleInfo> localBundleDic = new Dictionary<string, BundleInfo>();
        //先构建本地bundle信息,为了之后和远端bundle信息做crc差异校验
        //这里得到的bundle配置信息是热更文件夹里的配置文件
        if (localConfig != null)
        {
            foreach (BundleInfo bundleInfo in localConfig.BundleArray.Values)
            {
                string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

                localBundleDic.Add(uniqueId, bundleInfo);
            }
        }

        //找到那些差异的bundle文件,放到bundleList容器中
        foreach (BundleInfo bundleInfo in serverConfig.BundleArray.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

            if (localBundleDic.ContainsKey(uniqueId) == false)
            {
                bundleList.Add(bundleInfo);
            }
        }

        //对于那些遗留在本地的无用的bundle文件,要清除,不然本地文件越积累越多
        //这里需要移出的都是哪些更新包,不移除基础包
        // 2. 对于那些遗留在本地的多出来的bundle文件，把它过滤在removeList容器里

        List<BundleInfo> removeList = new List<BundleInfo>();

        if (localConfig != null)
        {
            foreach (var localBundle in localConfig.BundleArray)
            {
                if (serverConfig.BundleArray.ContainsKey(localBundle.Key) == false)
                {
                    removeList.Add(localBundle.Value);
                }
            }
        }

        return new Tuple<List<BundleInfo>,BundleInfo[]>(bundleList, removeList.ToArray());
    }


    /// <summary>
    /// 客户端给定模块的热更资源存放地址
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private string GetUpdatePath(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }


    /// <summary>
    /// 返回 给定模块的给定文件在服务器端的完整URL
    /// </summary>
    /// <param name="moduleConfig">模块配置对象</param>
    /// <param name="fileName">文件名字</param>
    /// <returns></returns>
    public string GetServerURL(ModuleConfig moduleConfig,string fileName)
    {
#if UNITY_ANDROID
        return string.Format("{0}/{1}/{2}",moduleConfig.DownloadURL,"Android",fileName);
#elif UNITY_IOS
        return string.Format("{0}/{1}/{2}",moduleConfig.DownloadURL,"iOS",fileName);
#elif UNITY_STANDALONE_WIN
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "StandaloneWindows64", fileName);
#endif
    }

    /// <summary>
    /// 计算需要下载的资源大小 单位是字节
    /// </summary>
    /// <param name="bundleList"></param>
    /// <returns></returns>
    private static long CalculateSize(List<BundleInfo> bundleList)
    {
        long totalSize = 0;

        foreach(BundleInfo bundleInfo in bundleList)
        {
            totalSize += bundleInfo.size;
        }

        return totalSize;
    }

    /// <summary>
    /// 弹出对话框
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    private static async Task<bool> ShowMessageBox(ModuleConfig moduleConfig,long totalSize)
    {
        string downLoadSize = SizeToString(totalSize);
        string messageInfo = $"发现新版本,版本号为: {moduleConfig.moduleVersion}\n需要下载热更包,大小为: {downLoadSize}";

        MessageBox messageBox = new MessageBox(messageInfo, "开始下载", "退出游戏");

        MessageBox.BoxResult result = await messageBox.GetReplyAsync();
        messageBox.Close();
        if (result == MessageBox.BoxResult.First)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 新增给网络失败时的弹出对话框
    /// </summary>
    /// <param name="messageInfo"></param>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    private static async Task<bool> ShowMessageBox(string messageInfo, string first, string second)
    {
        MessageBox messageBox = new MessageBox(messageInfo, first, second);

        MessageBox.BoxResult result = await messageBox.GetReplyAsync();
        messageBox.Close();
        if (result == MessageBox.BoxResult.First)
        {
            return true;
        }

        return false;
    }

    private static string SizeToString(long size)
    {
        string sizeStr = "";
        if(size >= 1024 * 1024)
        {
            long m = size / (1024 * 1024);
            size = size % (1024 * 1024);
            sizeStr += $"{m}[M]";
        }

        if(size >= 1024)
        {
            long k = size / 1024;
            size = size % 1024;
            sizeStr += $"{k}[K]";
        }
        long b = size;
        sizeStr += $"{b}[B]";

        return sizeStr;
    }

    private void Clear(ModuleConfig moduleConfig,BundleInfo[] removeList)
    {
        string moduleName = moduleConfig.moduleName;

        string updatePath = GetUpdatePath(moduleName);

        //删除不需要的本地bundle文件列表

        for(int i = removeList.Length - 1; i >= 0; --i)
        {
            BundleInfo bundleInfo = removeList[i];
            string filePath = string.Format("{0}/" + bundleInfo.bundle_name, updatePath);
            File.Delete(filePath);
        }
        //删除旧的配置文件
        string oldFile = string.Format("{0}/{1}.json", updatePath, moduleName.ToLower());

        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }

        //用新的配置文件替代
        string newFile = string.Format("{0}/{1}_temp.json", updatePath, moduleName.ToLower());

        File.Move(newFile,oldFile);
    }

}
