using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ������ ������
/// �ӷ��������ģ���Ӧ��AB�����ļ�,�ͱ��ص�AB�����ļ����ȶ�
/// ���ݱȶԲ�����ЩBundle�����˱仯
/// �����в�����ļ�
/// ɾ��û��Ҫ���ļ�
/// 
/// </summary>
public class Downloader : Singleton<Downloader>
{
    /// <summary>
    /// ����ģ�������,���ض�Ӧ��ģ��
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="action"></param>
    public async Task Download(ModuleConfig moduleConfig) {
        //��������ȸ���������Դ�ı���·��
        string updatePath = GetUpdatePath(moduleConfig.moduleName);

        //Զ�̷����������ģ���AB��Դ�����ļ���URL
        string configURL = GetServerURL(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");

        UnityWebRequest request = UnityWebRequest.Get(configURL);
        request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/{1}_temp.json", updatePath, moduleConfig.moduleName));
        
        //�ȱ���Ϊtemp�����ļ�

        Debug.Log("���ص�����·��: " + updatePath);
        
        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error) == false)
        {
            //���س���
            Debug.LogWarning($"����ģ��{moduleConfig.moduleName}��AB�����ļ�: {request.error}");
            bool result = await ShowMessageBox("�����쳣,����������� ��������", "��������", "�˳�����");

            if(result == false)
            {
                Application.Quit();
                return;
            }
            //���ȷ�ϵĻ�,��������
            await Download(moduleConfig);
            return;
        }

        //AB�����ļ����غ���,��ȡ��Ҫ���ص��б�
        Tuple<List<BundleInfo>,BundleInfo[]> tuple = await GetDownloadList(moduleConfig.moduleName);

        List<BundleInfo> downloadList = tuple.Item1;

        BundleInfo[] removeList = tuple.Item2;

        long downLoadSize = CalculateSize(downloadList);

        if(downLoadSize == 0)
        {
            //û����Ҫ���ص�
            //��removeList�еĸ�ɾ��
            Clear(moduleConfig,removeList);
            return;
        }

        //����������,�ȴ�ȷ���Ƿ�����
        bool boxResult = await ShowMessageBox(moduleConfig, downLoadSize);
        if(boxResult == false)
        {
            Application.Quit();
            return;
        }
        //ִ������
        await ExecuteDownload(moduleConfig, downloadList);
        //ɾ������Ҫɾ���Ķ���
        Clear(moduleConfig,removeList);

        return;
    }

    /// <summary>
    /// ִ��������Ϊ
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
                Debug.Log("������Դ��" + bundleInfo.bundle_name + " �ɹ�");
                //���سɹ���,�ѵ�һ��Ԫ�ظ��Ƴ�
                bundleList.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
        
        //�����߼���������,���bundleList��Ϊ��,������һ������Դû���������
        //������Ҫ�ٴ�����
        //���ﵯ��һ��MessageBox,���ȷ��,�ͼ�������
        if(bundleList.Count > 0)
        {
            bool result = await ShowMessageBox("�����쳣,����������� ��������", "��������", "�˳���Ϸ");

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
    /// ���ڸ���ģ��,������������Ҫ���ص�BundleInfo��ɵ�List
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<Tuple<List<BundleInfo>, BundleInfo[]>> GetDownloadList(string moduleName)
    {
        //Update�����·���˺ͱ��ػ�ȡ���Ƿ���˵������ļ�,�ͱ����ȸ��ļ�������ļ����жԱ�
        ModuleABConfig serverConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + "_temp.json");

        if(serverConfig == null)
        {
            return null;
        }

        ModuleABConfig localConfig = await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");

        //ע��: ���ﲻ���ж�localConfig�Ƿ����,���ص�localConfigȸʳ���ܲ�����,�����ڴ�ģ���һ���ȸ���֮ǰ,����update·����ɶ��û��

        return CalculateDiff(moduleName, localConfig, serverConfig); 
    }

    /// <summary>
    /// ͨ������AB��Դ�����ļ�,�Աȳ��в����Bundle
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="localConfg"></param>
    /// <param name="serverConfig"></param>
    /// <returns></returns>
    private Tuple<List<BundleInfo>, BundleInfo[]> CalculateDiff(string moduleName, ModuleABConfig localConfig, ModuleABConfig serverConfig)
    {
        // ��¼��Ҫ���ص�bundle�ļ��б�
        List<BundleInfo> bundleList = new List<BundleInfo>();
        // ��¼�����Ѿ����ڵ�bundle�ļ��б�
        Dictionary<string, BundleInfo> localBundleDic = new Dictionary<string, BundleInfo>();
        //�ȹ�������bundle��Ϣ,Ϊ��֮���Զ��bundle��Ϣ��crc����У��
        //����õ���bundle������Ϣ���ȸ��ļ�����������ļ�
        if (localConfig != null)
        {
            foreach (BundleInfo bundleInfo in localConfig.BundleArray.Values)
            {
                string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

                localBundleDic.Add(uniqueId, bundleInfo);
            }
        }

        //�ҵ���Щ�����bundle�ļ�,�ŵ�bundleList������
        foreach (BundleInfo bundleInfo in serverConfig.BundleArray.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

            if (localBundleDic.ContainsKey(uniqueId) == false)
            {
                bundleList.Add(bundleInfo);
            }
        }

        //������Щ�����ڱ��ص����õ�bundle�ļ�,Ҫ���,��Ȼ�����ļ�Խ����Խ��
        //������Ҫ�Ƴ��Ķ�����Щ���°�,���Ƴ�������
        // 2. ������Щ�����ڱ��صĶ������bundle�ļ�������������removeList������

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
    /// �ͻ��˸���ģ����ȸ���Դ��ŵ�ַ
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private string GetUpdatePath(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }


    /// <summary>
    /// ���� ����ģ��ĸ����ļ��ڷ������˵�����URL
    /// </summary>
    /// <param name="moduleConfig">ģ�����ö���</param>
    /// <param name="fileName">�ļ�����</param>
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
    /// ������Ҫ���ص���Դ��С ��λ���ֽ�
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
    /// �����Ի���
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="totalSize"></param>
    /// <returns></returns>
    private static async Task<bool> ShowMessageBox(ModuleConfig moduleConfig,long totalSize)
    {
        string downLoadSize = SizeToString(totalSize);
        string messageInfo = $"�����°汾,�汾��Ϊ: {moduleConfig.moduleVersion}\n��Ҫ�����ȸ���,��СΪ: {downLoadSize}";

        MessageBox messageBox = new MessageBox(messageInfo, "��ʼ����", "�˳���Ϸ");

        MessageBox.BoxResult result = await messageBox.GetReplyAsync();
        messageBox.Close();
        if (result == MessageBox.BoxResult.First)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// ����������ʧ��ʱ�ĵ����Ի���
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

        //ɾ������Ҫ�ı���bundle�ļ��б�

        for(int i = removeList.Length - 1; i >= 0; --i)
        {
            BundleInfo bundleInfo = removeList[i];
            string filePath = string.Format("{0}/" + bundleInfo.bundle_name, updatePath);
            File.Delete(filePath);
        }
        //ɾ���ɵ������ļ�
        string oldFile = string.Format("{0}/{1}.json", updatePath, moduleName.ToLower());

        if (File.Exists(oldFile))
        {
            File.Delete(oldFile);
        }

        //���µ������ļ����
        string newFile = string.Format("{0}/{1}_temp.json", updatePath, moduleName.ToLower());

        File.Move(newFile,oldFile);
    }

}
