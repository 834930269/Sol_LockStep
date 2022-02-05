using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using LitJson;
using System;

/// <summary>
/// ����,ɨ��GAssets����ÿһ���ļ���,ÿ���ļ��б���Ϊ��һ��ģ��
/// ���ÿ��ģ��,���������е�AB���ļ�
/// </summary>
public class ABEditor : MonoBehaviour
{
    /// <summary>
    /// �ȸ���Դ�ĸ�Ŀ¼
    /// </summary>
    public static string rootPath = Application.dataPath + "/GAssets";

    /// <summary>
    /// ������Ҫ�����AB����Ϣ: һ��AssetBbundle�ļ��ж�Ӧ��һ��AssetBundleBuild����
    /// </summary>
    public static List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>();

    /// <summary>
    /// AB���ļ������·��
    /// </summary>
    public static string abOutputPath = Application.streamingAssetsPath;

    /// <summary>
    /// ��¼�ĸ�asset��Դ�����ĸ�AB���ļ�
    /// </summary>
    public static Dictionary<string, string> asset2bundle = new Dictionary<string, string>();

    /// <summary>
    /// ��¼ÿ��asset��Դ������AB���ļ��б�
    /// </summary>
    public static Dictionary<string, List<string>> asset2Dependencies = new Dictionary<string, List<string>>();

    /// <summary>
    /// ��ʱ�����Ҫ���ܵ������ļ��ĸ�·��
    /// </summary>
    public static string tempGAssets;

    /// <summary>
    /// ���AssetBundle��Դ
    /// </summary>
    public static void BuildAssetBundle()
    {
        Debug.Log("��ʼ--->>>������ģ���Lua��Proto�����ļ����м���!");

        EncryptLuaAndProto();

        Debug.Log("��ʼ--->>>��������ģ���AB��");
        try
        {


            if (Directory.Exists(abOutputPath) == true)
            {
                Directory.Delete(abOutputPath, true);
            }

            //��������ģ��,�������ģ�鶼�ֱ���

            DirectoryInfo rootDir = new DirectoryInfo(rootPath);
            DirectoryInfo[] Dirs = rootDir.GetDirectories();

            foreach (DirectoryInfo moduleDir in Dirs)
            {
                string moduleName = moduleDir.Name;

                assetBundleBuildList.Clear();

                asset2bundle.Clear();

                asset2Dependencies.Clear();

                //��ʼ���ģ������AB���ļ�

                ScanChildDireations(moduleDir);

                AssetDatabase.Refresh();

                string moduleOutputPath = abOutputPath + "/" + moduleName;

                if (Directory.Exists(moduleOutputPath) == true)
                {
                    Directory.Delete(moduleOutputPath, true);
                }

                Directory.CreateDirectory(moduleOutputPath);

                //ѹ��ѡ�����
                //BuildAssetBundleOptions.None: ʹ��LZMA�㷨ѹ��,ѹ���İ���С,���Ǽ���ʱ�����,ʹ��֮ǰ��Ҫ��ѹ.
                //һ������ѹ,�������ʹ��LZ4����ѹ��,ʹ����Դ��ʱ����Ҫ�����ѹ�������ص�ʱ�����ʹ��LZMA�㷨
                //һ���������Ժ�,����ʹ��LZ4�㷨�����ڱ�����.
                //BuildAssetBundleOptions.UncompressedAssetBundle:��ѹ��,����,���ؿ�
                //BuildAssetBundleOptions.ChunkBasedCompression: ʹ��LZ4ѹ��,ѹ����û��LZMA��,�������ǿ��Լ���
                //ָ����Դ�����ý�ѹȫ��

                //����һ: bundle�ļ��б�����·��
                //������: ����bundle�ļ��б�����Ҫ��AssetBundleBuild��������(����ָ��Unity������Щbundle�ļ�,ÿ��
                //�ļ��������Լ��ļ��������Щ��Դ)
                //������: ѹ��ѡ��BuildAssetBundleOptions.None��Ĭ��LZMA�㷨ѹ��
                //������; �����ĸ�ƽ̨��bundle�ļ�,��Ŀ��ƽ̨

                BuildPipeline.BuildAssetBundles(moduleOutputPath, assetBundleBuildList.ToArray(),
                    BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

                //��������
                CalculateDependencies();

                SaveModuleABConfig(moduleName);

                AssetDatabase.Refresh();

                //ɾ������
                DeleteManifest(moduleOutputPath);

                File.Delete(moduleOutputPath + "/" + moduleName);
            }
        }
        finally
        {
            Debug.Log("����--->>>��������ģ���AB��!");
            RestoreModules();
            Debug.Log("��ʼ--->>>������ģ���Lua��Proto�����ļ����лָ�!");
        }


    }

    /// <summary>
    /// ����ָ�����ļ���
    /// 1. ������ļ����µ�����һ�����ļ������һ��AssetBundle
    /// 2. ���ҵݹ��������ļ����µ��������ļ���
    /// </summary>
    /// <param name="directoryInfo"></param>
    public static void ScanChildDireations(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Name.EndsWith("CSProject~"))
        {
            return;
        }

        //�ռ���ǰ·���µ��ļ�,�����Ǵ����һ��AB��
        ScanCurrDirectory(directoryInfo);

        //������ǰ·���µ����ļ���
        DirectoryInfo[] dirs = directoryInfo.GetDirectories();

        foreach(DirectoryInfo info in dirs)
        {
            ScanChildDireations(info);
        }

    }

    /// <summary>
    /// ������ǰ·���µ��ļ�,�����Ǵ����һ��AB��
    /// </summary>
    /// <param name="directoryInfo"></param>
    private static void ScanCurrDirectory(DirectoryInfo directoryInfo)
    {
        List<string> assetNames = new List<string>();

        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach(FileInfo fileInfo in fileInfoList)
        {
            if (fileInfo.Name.EndsWith(".meta"))
            {
                continue;
            }

            //assetName�ĸ�ʽ���� "Assets/GAssets/Launch/Sphere.prefab"
            string assetName = fileInfo.FullName.Substring(Application.dataPath.Length - "Assets".Length)
                                        .Replace("\\", "/");

            assetNames.Add(assetName);
        }

        if(assetNames.Count > 0)
        {
            //��ʽ������ gassets_Launch
            string assetbundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1)
                                                  .Replace("\\", "_").ToLower();

            AssetBundleBuild build = new AssetBundleBuild();

            build.assetBundleName = assetbundleName;
            build.assetNames = new string[assetNames.Count];

            for(int i = 0; i < assetNames.Count; ++i)
            {
                build.assetNames[i] = assetNames[i];

                //��¼������Դ�����ĸ�bundle�ļ�

                asset2bundle.Add(assetNames[i],assetbundleName);
            }
            assetBundleBuildList.Add(build);
        }
    }

    /// <summary>
    /// ����ÿ����Դ��������ab���ļ��б�
    /// </summary>
    public static void CalculateDependencies()
    {
        foreach(string asset in asset2bundle.Keys)
        {
            //�����Դ�Լ����ڵ�bundle
            string assetBundle = asset2bundle[asset];

            //��ȡ��������Դ
            string[] dependencies = AssetDatabase.GetDependencies(asset);

            //��������Դ�б�
            List<string> assetList = new List<string>();

            if(dependencies != null && dependencies.Length > 0)
            {
                foreach (string oneAsset in dependencies)
                {
                    //�������Լ����߽ű�,����
                    if (oneAsset == asset || oneAsset.EndsWith(".cs"))
                    {
                        continue;
                    }

                    assetList.Add(oneAsset);
                }
            }

            if(assetList.Count > 0)
            {
                List<string> abList = new List<string>();

                foreach(string oneAsset in assetList)
                {
                    //���Ի�ȡ����Դ������ab��
                    bool result = asset2bundle.TryGetValue(oneAsset, out string bundle);

                    if(result == true)
                    {
                        //�������һ��AB����
                        if(bundle != assetBundle)
                        {
                            abList.Add(bundle);
                        }
                    }
                }

                asset2Dependencies.Add(asset, abList);
            }

        
        }
    }

    /// <summary>
    /// ��һ��ģ�����Դ������ϵ���ݱ����json��ʽ���ļ�
    /// </summary>
    /// <param name="moduleName"></param>
    private static void SaveModuleABConfig(string moduleName)
    {
        ModuleABConfig moduleABConfig = new ModuleABConfig(asset2bundle.Count);
        //��¼AB����Ϣ

        foreach (AssetBundleBuild build in assetBundleBuildList)
        {
            BundleInfo bundleInfo = new BundleInfo();

            bundleInfo.bundle_name = build.assetBundleName;

            bundleInfo.assets = new List<string>();

            foreach (string asset in build.assetNames)
            {
                bundleInfo.assets.Add(asset);
            }

            // ����һ��bundle�ļ���CRCɢ����

            string abFilePath = abOutputPath + "/" + moduleName + "/" + bundleInfo.bundle_name;

            using (FileStream stream = File.OpenRead(abFilePath))
            {
                bundleInfo.crc = AssetUtility.GetCRC32Hash(stream);

                //˳��д���ļ���С
                bundleInfo.size = (int)stream.Length;
            }

            moduleABConfig.AddBundle(bundleInfo.bundle_name, bundleInfo);

        }

        //��¼ÿ����Դ��������ϵ
        int assetIndex = 0;

        foreach (var item in asset2bundle)
        {
            AssetInfo assetInfo = new AssetInfo();
            assetInfo.asset_path = item.Key;
            assetInfo.bundle_name = item.Value;
            assetInfo.dependencies = new List<string>();

            bool result = asset2Dependencies.TryGetValue(item.Key, out List<string> dependencies);

            if (result == true)
            {
                for(int i=0;i< dependencies.Count; ++i)
                {
                    string bundleName = dependencies[i];
                    assetInfo.dependencies.Add(bundleName);
                }
            }

            moduleABConfig.AddAsset(assetIndex, assetInfo);

            assetIndex++;
        }

        //��ʼд��Json�ļ�
        string moduleConfigName = moduleName.ToLower() + ".json";
        string jsonPath = abOutputPath + "/" + moduleName + "/" + moduleConfigName;
        if(File.Exists(jsonPath) == true)
        {
            File.Delete(jsonPath);
        }

        File.Create(jsonPath).Dispose();

        string jsonData = LitJson.JsonMapper.ToJson(moduleABConfig);

        File.WriteAllText(jsonPath, ConvertJsonString(jsonData));
    }

    /// <summary>
    /// ��ʽ��json
    /// </summary>
    /// <param name="str">����json�ַ���</param>
    /// <returns>���ظ�ʽ������ַ���</returns>
    private static string ConvertJsonString(string str)
    {
        JsonSerializer serializer = new JsonSerializer();

        TextReader tr = new StringReader(str);

        JsonTextReader jtr = new JsonTextReader(tr);

        object obj = serializer.Deserialize(jtr);
        if (obj != null)
        {
            StringWriter textWriter = new StringWriter();

            JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
            {
                Formatting = Formatting.Indented,

                Indentation = 4,

                IndentChar = ' '
            };

            serializer.Serialize(jsonWriter, obj);

            return textWriter.ToString();
        }
        else
        {
            return str;
        }
    }

    /// <summary>
    /// ����ʽ���汾�İ汾��Դ
    /// </summary>
    [MenuItem("ABEditor/BuildAssetBundle_Base")]
    public static void BuildAssetBundle_Base()
    {
        abOutputPath = Application.dataPath + "/../AssetBundle_Base";

        BuildAssetBundle();
    }

    /// <summary>
    /// ��ʽ�� �ȸ��汾��
    /// </summary>
    [MenuItem("ABEditor/BuildAssetBundle_Update")]
    public static void BuildAssetBundle_Update()
    {
        //1. ����AssetBundle_Update�ļ����а�AB�������ɳ���
        abOutputPath = Application.dataPath + "/../AssetBundle_Update";
        BuildAssetBundle();

        //2.�ٺ�AssetBundle_Base�İ汾���бȶ�,ɾ����Щ��AssetBundle_Base�汾һ������Դ
        string baseABPath = Application.dataPath + "/../AssetBundle_Base";

        string updateABPath = abOutputPath;

        DirectoryInfo baseDir = new DirectoryInfo(baseABPath);

        DirectoryInfo[] Dirs = baseDir.GetDirectories();

        foreach(DirectoryInfo moduleDir in Dirs)
        {
            string moduleName = moduleDir.Name;
            ModuleABConfig baseABConfig = LoadABConfig(baseABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");

            ModuleABConfig updateABConfig = LoadABConfig(updateABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");

            //�������Щ��base�汾���,û�б仯��bundle�ļ�,����Ҫ���ȸ�����ɾ�����ļ�

            List<BundleInfo> removeList = Calculate(baseABConfig, updateABConfig);

            foreach(BundleInfo info in removeList)
            {
                string filePath = updateABPath + "/" + moduleName + "/" + info.bundle_name;

                File.Delete(filePath);

                //ͬʱҪ����һ���ȸ����汾���AB��Դ�����ļ�

                updateABConfig.BundleArray.Remove(info.bundle_name);
            }

            //���������ȸ����� AB��Դ�����ļ�
            string jsonPath = updateABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json";

            if(File.Exists(jsonPath) == true)
            {
                File.Delete(jsonPath);
            }

            File.Create(jsonPath).Dispose();

            string jsonData = LitJson.JsonMapper.ToJson(updateABConfig);

            File.WriteAllText(jsonPath, ConvertJsonString(jsonData));
        }
    }


    /// <summary>
    /// �����ȸ�������Ҫɾ����bundle�ļ��б�
    /// </summary>
    /// <param name="baseABConfig"></param>
    /// <param name="updateABConfig"></param>
    /// <returns></returns>
    private static List<BundleInfo> Calculate(ModuleABConfig baseABConfig,ModuleABConfig updateABConfig)
    {
        //�ռ����е�base�汾��bundle�ļ�,�ŵ����baseBundleDic�ֵ���
        Dictionary<string, BundleInfo> baseBundleDic = new Dictionary<string, BundleInfo>();
        if(baseABConfig != null)
        {
            foreach(BundleInfo bundleInfo in baseABConfig.BundleArray.Values)
            {
                string uniqueId = string.Format("{0}|{1}",bundleInfo.bundle_name,bundleInfo.crc);
                baseBundleDic.Add(uniqueId,bundleInfo);
            }
        }

        //����Update�汾�е�bundle�ļ�,����Щ��Ҫɾ����bundle���������removeList������
        //����һ��: ����base�汾��ͬ����Щbundle�ļ�,������Ҫɾ����
        List<BundleInfo> removeList = new List<BundleInfo>();

        foreach(BundleInfo  bundleInfo in updateABConfig.BundleArray.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

            //�ҵ���Щ�ظ���bundle�ļ�,��removeList������ɾ��
            if(baseBundleDic.ContainsKey(uniqueId) == true)
            {
                removeList.Add(bundleInfo);
            }
        }

        return removeList;
    }


    /// <summary>
    /// ������ߵĹ��ߺ���
    /// </summary>
    /// <param name="abConfigPath"></param>
    /// <returns></returns>
    private static ModuleABConfig LoadABConfig(string abConfigPath)
    {
        File.ReadAllText(abConfigPath);
        return JsonMapper.ToObject<ModuleABConfig>(File.ReadAllText(abConfigPath));
    }

    [MenuItem("ABEditor/BuildAssetBundle_Dev")]
    public static void BuildAssetBundle_Dev()
    {
        abOutputPath = Application.streamingAssetsPath;

        BuildAssetBundle();
    }

    /// <summary>
    /// ɾ��Unity���������ɵ�.manifest�ļ�,�����ǲ���Ҫ��
    /// </summary>
    /// <param name="moduleOutputPath">ģ���Ӧ��ab�ļ����·��</param>
    private static void DeleteManifest(string moduleOutputPath)
    {
        FileInfo[] files = new DirectoryInfo(moduleOutputPath).GetFiles();

        foreach(FileInfo file in files)
        {
            if (file.Name.EndsWith(".manifest"))
            {
                file.Delete();
            }
        }
    }

    /// <summary>
    /// ��ÿ��ģ���/Src/�ļ��к�Res/Proto/�ļ��н��м���
    /// </summary>
    private static void EncryptLuaAndProto()
    {
        //��������ģ��,�������ģ�鶼����lua��proto�����ļ���
        DirectoryInfo rootDir = new DirectoryInfo(rootPath);
        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        //������ʱ�ļ���,������ʱ���ÿ��ģ����Ҫ���ܵ������ļ�
        CreateTempGAssets();

        foreach(DirectoryInfo moduleDir in Dirs)
        {
            //���ÿ��ģ��
            //1. ���Ȱ�Src/�ļ��к�Res/Proto/�ļ��ж����Ƶ���ʱ�ļ�����
            CopyOneModule(moduleDir);

            // 2. ���Ű�Src/�ļ��к�Res/Proto/�ļ��н��о͵ؼ���

            EncryptOneModule(moduleDir);

            // 3. Ȼ���Src/�ļ��к�Res/Proto/�ļ��е������ļ��͵�ɾ��

            DeleteOneModule(moduleDir);
        }

        //������Ϻ�ˢ����
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// ������ʱ�ļ��У���������ʱ���ÿ��ģ����Ҫ���ܵ������ļ�
    /// </summary>
    private static void CreateTempGAssets()
    {
        tempGAssets = Application.dataPath + "/../TempGAssets";
        if(Directory.Exists(tempGAssets) == true)
        {
            Directory.Delete(tempGAssets, true);
        }

        Directory.CreateDirectory(tempGAssets);
    }

    /// <summary>
    /// ��Src/�ļ��к�Res/Proto/�ļ��ж����Ƶ���ʱ�ļ���
    /// </summary>
    /// <param name="moduleDir">ģ���·��</param>
    private static void CopyOneModule(DirectoryInfo moduleDir)
    {
        string srcLuaPath = Path.Combine(moduleDir.FullName, "Src");
        string destLuaPath = Path.Combine(tempGAssets, moduleDir.Name, "Src");
        CopyFolder(srcLuaPath, destLuaPath);

        string srcProtoPath = Path.Combine(moduleDir.FullName, "Res/Proto");
        string destProtoPath = Path.Combine(tempGAssets, moduleDir.Name, "Res/Proto");
        CopyFolder(srcProtoPath, destProtoPath);
    }

    /// <summary>
    /// �Ե���ģ���Src/�ļ��к�Res/Proto/�ļ����µ������ļ����о͵ؼ���
    /// </summary>
    /// <param name="moduleDir">ģ���·��</param>
    private static void EncryptOneModule(DirectoryInfo moduleDir)
    {
        EncryptOnePath(Path.Combine(moduleDir.FullName, "Src"));

        EncryptOnePath(Path.Combine(moduleDir.FullName, "Res/Proto"));
    }

    /// <summary>
    /// �Ե���·���µ�������Դ���м��ܣ������ɶ�Ӧ�ļ����ļ�
    /// </summary>
    /// <param name="path"></param>
    private static void EncryptOnePath(string path)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach(FileInfo fileInfo in fileInfoList)
        {
            //����ֻ��lua�ļ���proto�ļ���Ҫ����
            if(fileInfo.FullName.EndsWith(".lua") == false && fileInfo.FullName.EndsWith(".proto") == false)
            {
                continue;
            }
            //��ȡ��������
            string plainText = File.ReadAllText(fileInfo.FullName);
            //����ASE����
            string cipherText = AESHelper.Encrypt(plainText, AESHelper.keyValue);
            // �������ܺ���ļ�
            CreateEncryptFile(fileInfo.FullName + ".bytes", cipherText);
        }
        DirectoryInfo[] Dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo oneDirInfo in Dirs)
        {
            EncryptOnePath(oneDirInfo.FullName);
        }
    }

    /// <summary>
    /// ��Src/�ļ��к�Res/Proto/�ļ��е������ļ��͵�ɾ��
    /// </summary>
    /// <param name="moduleDir">ģ���·��</param>
    private static void DeleteOneModule(DirectoryInfo moduleDir)
    {
        DeleteOnePath(Path.Combine(moduleDir.FullName, "Src"));

        DeleteOnePath(Path.Combine(moduleDir.FullName, "Res/Proto"));
    }

    /// <summary>
    /// �Ե���·���µ�lua����proto�����ļ�����ɾ��
    /// </summary>
    /// <param name="path"></param>
    private static void DeleteOnePath(string path)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);

        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach (FileInfo fileInfo in fileInfoList)
        {
            // ����ֻ��lua�ļ���proto�����ļ�����ɾ��

            if (fileInfo.FullName.EndsWith(".lua") == false &&
                fileInfo.FullName.EndsWith(".lua.meta") == false &&
                fileInfo.FullName.EndsWith(".proto") == false &&
                fileInfo.FullName.EndsWith(".proto.meta") == false)
            {
                continue;
            }

            // ɾ�������ļ������Ӧ��meta�ļ�

            fileInfo.Delete();
        }

        DirectoryInfo[] Dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo oneDirInfo in Dirs)
        {
            DeleteOnePath(oneDirInfo.FullName);
        }
    }

    /// <summary>
    /// ������ģ���AB������������Ժ�
    /// 1. ɾ��GAssets�ĸ���ģ���еļ����ļ�
    /// 2. Ȼ��Ѵ������ʱ�ļ��е������ļ��ٿ�����GAssets�ĸ���ģ����
    /// 3. ���Ҫɾ����ʱ�ļ���
    /// </summary>
    private static void RestoreModules()
    {
        DirectoryInfo rootDir = new DirectoryInfo(rootPath);

        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        foreach (DirectoryInfo moduleDir in Dirs)
        {
            // ����Lua�ļ���

            string luaPath = Path.Combine(moduleDir.FullName, "Src");

            Directory.Delete(luaPath, true);

            string tempLuaPath = Path.Combine(tempGAssets, moduleDir.Name, "Src");

            CopyFolder(tempLuaPath, luaPath);

            // ����Proto�ļ���

            string protoPath = Path.Combine(moduleDir.FullName, "Res/Proto");

            Directory.Delete(protoPath, true);

            string tempProtoPath = Path.Combine(tempGAssets, moduleDir.Name, "Res/Proto");

            CopyFolder(tempProtoPath, protoPath);
        }

        // ɾ����ʱ�ļ���

        Directory.Delete(tempGAssets, true);
    }
    /// <summary>
    /// �������ܺ���ļ�
    /// </summary>
    /// <param name="filePath">�����ļ���·��</param>
    /// <param name="fileText">���ĵ�����</param>
    private static void CreateEncryptFile(string filePath, string fileText)
    {
        FileStream fs = new FileStream(filePath, FileMode.CreateNew);

        StreamWriter sw = new StreamWriter(fs);

        sw.Write(fileText);

        sw.Flush();

        sw.Close();

        fs.Close();
    }

    /// <summary>
    /// ���ߺ���,�����ļ���
    /// </summary>
    /// <param name="sourceFolder">ԭ�ļ���·��</param>
    /// <param name="destFolder">Ŀ���ļ���·��</param>
    private static void CopyFolder(string sourceFolder,string destFolder)
    {
        try
        {
            if(Directory.Exists(destFolder) == true)
            {
                Directory.Delete(destFolder, true);
            }

            Directory.CreateDirectory(destFolder);
            //�õ�ԭ�ļ��е����ļ��б�
            string[] filePathList = Directory.GetFiles(sourceFolder);

            foreach (string filePath in filePathList)
            {
                string fileName = Path.GetFileName(filePath);

                string destPath = Path.Combine(destFolder, fileName);

                File.Copy(filePath, destPath);
            }

            //�õ�ԭ�ļ����µ��������ļ���
            string[] folders = Directory.GetDirectories(sourceFolder);

            foreach (string srcPath in folders)
            {
                string folderName = Path.GetFileName(srcPath);

                string destPath = Path.Combine(destFolder, folderName);

                CopyFolder(srcPath, destPath);//����Ŀ��·��,�ݹ鸴���ļ�
            }
        }
        catch (Exception e)
        {
            Debug.LogError("�����ļ��г���" + e.ToString());
        }
    }
}


