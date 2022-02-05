using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using LitJson;
using System;

/// <summary>
/// 任务,扫描GAssets下面每一级文件夹,每个文件夹被认为是一个模块
/// 针对每个模块,生成其所有的AB包文件
/// </summary>
public class ABEditor : MonoBehaviour
{
    /// <summary>
    /// 热更资源的根目录
    /// </summary>
    public static string rootPath = Application.dataPath + "/GAssets";

    /// <summary>
    /// 所有需要打包的AB包信息: 一个AssetBbundle文件夹对应了一个AssetBundleBuild对象
    /// </summary>
    public static List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>();

    /// <summary>
    /// AB包文件的输出路径
    /// </summary>
    public static string abOutputPath = Application.streamingAssetsPath;

    /// <summary>
    /// 记录哪个asset资源属于哪个AB包文件
    /// </summary>
    public static Dictionary<string, string> asset2bundle = new Dictionary<string, string>();

    /// <summary>
    /// 记录每个asset资源依赖的AB包文件列表
    /// </summary>
    public static Dictionary<string, List<string>> asset2Dependencies = new Dictionary<string, List<string>>();

    /// <summary>
    /// 临时存放需要加密的明文文件的根路径
    /// </summary>
    public static string tempGAssets;

    /// <summary>
    /// 打包AssetBundle资源
    /// </summary>
    public static void BuildAssetBundle()
    {
        Debug.Log("开始--->>>对所有模块的Lua和Proto明文文件进行加密!");

        EncryptLuaAndProto();

        Debug.Log("开始--->>>生成所有模块的AB包");
        try
        {


            if (Directory.Exists(abOutputPath) == true)
            {
                Directory.Delete(abOutputPath, true);
            }

            //遍历所有模块,针对所有模块都分别打包

            DirectoryInfo rootDir = new DirectoryInfo(rootPath);
            DirectoryInfo[] Dirs = rootDir.GetDirectories();

            foreach (DirectoryInfo moduleDir in Dirs)
            {
                string moduleName = moduleDir.Name;

                assetBundleBuildList.Clear();

                asset2bundle.Clear();

                asset2Dependencies.Clear();

                //开始这个模块生成AB包文件

                ScanChildDireations(moduleDir);

                AssetDatabase.Refresh();

                string moduleOutputPath = abOutputPath + "/" + moduleName;

                if (Directory.Exists(moduleOutputPath) == true)
                {
                    Directory.Delete(moduleOutputPath, true);
                }

                Directory.CreateDirectory(moduleOutputPath);

                //压缩选项详解
                //BuildAssetBundleOptions.None: 使用LZMA算法压缩,压缩的包更小,但是加载时间更长,使用之前需要解压.
                //一旦被解压,这个包会使用LZ4重新压缩,使用资源的时候不需要整体解压。在下载的时候可以使用LZMA算法
                //一旦被下载以后,它会使用LZ4算法保存在本地上.
                //BuildAssetBundleOptions.UncompressedAssetBundle:不压缩,包大,加载快
                //BuildAssetBundleOptions.ChunkBasedCompression: 使用LZ4压缩,压缩率没有LZMA高,但是我们可以加载
                //指定资源而不用解压全部

                //参数一: bundle文件列表的输出路径
                //参数二: 生成bundle文件列表所需要的AssetBundleBuild对象数组(用来指导Unity生成哪些bundle文件,每个
                //文件的名字以及文件里包含哪些资源)
                //参数三: 压缩选项BuildAssetBundleOptions.None是默认LZMA算法压缩
                //参数四; 生成哪个平台的bundle文件,即目标平台

                BuildPipeline.BuildAssetBundles(moduleOutputPath, assetBundleBuildList.ToArray(),
                    BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

                //计算依赖
                CalculateDependencies();

                SaveModuleABConfig(moduleName);

                AssetDatabase.Refresh();

                //删除依赖
                DeleteManifest(moduleOutputPath);

                File.Delete(moduleOutputPath + "/" + moduleName);
            }
        }
        finally
        {
            Debug.Log("结束--->>>生成所有模块的AB包!");
            RestoreModules();
            Debug.Log("开始--->>>对所有模块的Lua和Proto明文文件进行恢复!");
        }


    }

    /// <summary>
    /// 根据指定的文件夹
    /// 1. 将这个文件夹下的所有一级子文件打包成一个AssetBundle
    /// 2. 并且递归遍历这个文件夹下的所有子文件夹
    /// </summary>
    /// <param name="directoryInfo"></param>
    public static void ScanChildDireations(DirectoryInfo directoryInfo)
    {
        if (directoryInfo.Name.EndsWith("CSProject~"))
        {
            return;
        }

        //收集当前路径下的文件,把他们打包成一个AB包
        ScanCurrDirectory(directoryInfo);

        //遍历当前路径下的子文件夹
        DirectoryInfo[] dirs = directoryInfo.GetDirectories();

        foreach(DirectoryInfo info in dirs)
        {
            ScanChildDireations(info);
        }

    }

    /// <summary>
    /// 遍历当前路径下的文件,把它们打包成一个AB包
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

            //assetName的格式类似 "Assets/GAssets/Launch/Sphere.prefab"
            string assetName = fileInfo.FullName.Substring(Application.dataPath.Length - "Assets".Length)
                                        .Replace("\\", "/");

            assetNames.Add(assetName);
        }

        if(assetNames.Count > 0)
        {
            //格式类似于 gassets_Launch
            string assetbundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1)
                                                  .Replace("\\", "_").ToLower();

            AssetBundleBuild build = new AssetBundleBuild();

            build.assetBundleName = assetbundleName;
            build.assetNames = new string[assetNames.Count];

            for(int i = 0; i < assetNames.Count; ++i)
            {
                build.assetNames[i] = assetNames[i];

                //记录单个资源属于哪个bundle文件

                asset2bundle.Add(assetNames[i],assetbundleName);
            }
            assetBundleBuildList.Add(build);
        }
    }

    /// <summary>
    /// 计算每个资源所依赖的ab包文件列表
    /// </summary>
    public static void CalculateDependencies()
    {
        foreach(string asset in asset2bundle.Keys)
        {
            //这个资源自己所在的bundle
            string assetBundle = asset2bundle[asset];

            //获取依赖的资源
            string[] dependencies = AssetDatabase.GetDependencies(asset);

            //依赖的资源列表
            List<string> assetList = new List<string>();

            if(dependencies != null && dependencies.Length > 0)
            {
                foreach (string oneAsset in dependencies)
                {
                    //依赖是自己或者脚本,忽略
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
                    //尝试获取该资源所属的ab包
                    bool result = asset2bundle.TryGetValue(oneAsset, out string bundle);

                    if(result == true)
                    {
                        //如果不在一个AB包里
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
    /// 将一个模块的资源依赖关系数据保存成json格式的文件
    /// </summary>
    /// <param name="moduleName"></param>
    private static void SaveModuleABConfig(string moduleName)
    {
        ModuleABConfig moduleABConfig = new ModuleABConfig(asset2bundle.Count);
        //记录AB包信息

        foreach (AssetBundleBuild build in assetBundleBuildList)
        {
            BundleInfo bundleInfo = new BundleInfo();

            bundleInfo.bundle_name = build.assetBundleName;

            bundleInfo.assets = new List<string>();

            foreach (string asset in build.assetNames)
            {
                bundleInfo.assets.Add(asset);
            }

            // 计算一个bundle文件的CRC散列码

            string abFilePath = abOutputPath + "/" + moduleName + "/" + bundleInfo.bundle_name;

            using (FileStream stream = File.OpenRead(abFilePath))
            {
                bundleInfo.crc = AssetUtility.GetCRC32Hash(stream);

                //顺便写入文件大小
                bundleInfo.size = (int)stream.Length;
            }

            moduleABConfig.AddBundle(bundleInfo.bundle_name, bundleInfo);

        }

        //记录每个资源的依赖关系
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

        //开始写入Json文件
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
    /// 格式化json
    /// </summary>
    /// <param name="str">输入json字符串</param>
    /// <returns>返回格式化后的字符串</returns>
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
    /// 打正式打大版本的版本资源
    /// </summary>
    [MenuItem("ABEditor/BuildAssetBundle_Base")]
    public static void BuildAssetBundle_Base()
    {
        abOutputPath = Application.dataPath + "/../AssetBundle_Base";

        BuildAssetBundle();
    }

    /// <summary>
    /// 正式打 热更版本包
    /// </summary>
    [MenuItem("ABEditor/BuildAssetBundle_Update")]
    public static void BuildAssetBundle_Update()
    {
        //1. 先在AssetBundle_Update文件夹中把AB包都生成出来
        abOutputPath = Application.dataPath + "/../AssetBundle_Update";
        BuildAssetBundle();

        //2.再和AssetBundle_Base的版本进行比对,删除那些和AssetBundle_Base版本一样的资源
        string baseABPath = Application.dataPath + "/../AssetBundle_Base";

        string updateABPath = abOutputPath;

        DirectoryInfo baseDir = new DirectoryInfo(baseABPath);

        DirectoryInfo[] Dirs = baseDir.GetDirectories();

        foreach(DirectoryInfo moduleDir in Dirs)
        {
            string moduleName = moduleDir.Name;
            ModuleABConfig baseABConfig = LoadABConfig(baseABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");

            ModuleABConfig updateABConfig = LoadABConfig(updateABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");

            //计算出那些跟base版本相比,没有变化的bundle文件,即需要从热更包中删除的文件

            List<BundleInfo> removeList = Calculate(baseABConfig, updateABConfig);

            foreach(BundleInfo info in removeList)
            {
                string filePath = updateABPath + "/" + moduleName + "/" + info.bundle_name;

                File.Delete(filePath);

                //同时要处理一下热更包版本里的AB资源配置文件

                updateABConfig.BundleArray.Remove(info.bundle_name);
            }

            //重新生成热更包的 AB资源配置文件
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
    /// 计算热更包中需要删除的bundle文件列表
    /// </summary>
    /// <param name="baseABConfig"></param>
    /// <param name="updateABConfig"></param>
    /// <returns></returns>
    private static List<BundleInfo> Calculate(ModuleABConfig baseABConfig,ModuleABConfig updateABConfig)
    {
        //收集所有的base版本的bundle文件,放到这个baseBundleDic字典中
        Dictionary<string, BundleInfo> baseBundleDic = new Dictionary<string, BundleInfo>();
        if(baseABConfig != null)
        {
            foreach(BundleInfo bundleInfo in baseABConfig.BundleArray.Values)
            {
                string uniqueId = string.Format("{0}|{1}",bundleInfo.bundle_name,bundleInfo.crc);
                baseBundleDic.Add(uniqueId,bundleInfo);
            }
        }

        //遍历Update版本中的bundle文件,把那些需要删除的bundle放入下面的removeList容器中
        //解释一下: 即和base版本相同的那些bundle文件,就是需要删除的
        List<BundleInfo> removeList = new List<BundleInfo>();

        foreach(BundleInfo  bundleInfo in updateABConfig.BundleArray.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundle_name, bundleInfo.crc);

            //找到那些重复的bundle文件,从removeList容器中删除
            if(baseBundleDic.ContainsKey(uniqueId) == true)
            {
                removeList.Add(bundleInfo);
            }
        }

        return removeList;
    }


    /// <summary>
    /// 打包工具的工具函数
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
    /// 删除Unity帮我们生成的.manifest文件,我们是不需要的
    /// </summary>
    /// <param name="moduleOutputPath">模块对应的ab文件输出路径</param>
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
    /// 对每个模块的/Src/文件夹和Res/Proto/文件夹进行加密
    /// </summary>
    private static void EncryptLuaAndProto()
    {
        //遍历所有模块,针对所有模块都进行lua和proto的明文加密
        DirectoryInfo rootDir = new DirectoryInfo(rootPath);
        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        //创建临时文件夹,用来临时存放每个模块需要加密的明文文件
        CreateTempGAssets();

        foreach(DirectoryInfo moduleDir in Dirs)
        {
            //针对每个模块
            //1. 首先把Src/文件夹和Res/Proto/文件夹都复制到临时文件夹中
            CopyOneModule(moduleDir);

            // 2. 接着把Src/文件夹和Res/Proto/文件夹进行就地加密

            EncryptOneModule(moduleDir);

            // 3. 然后把Src/文件夹和Res/Proto/文件夹的明文文件就地删除

            DeleteOneModule(moduleDir);
        }

        //加密完毕后刷新下
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 创建临时文件夹，用来存临时存放每个模块需要加密的明文文件
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
    /// 把Src/文件夹和Res/Proto/文件夹都复制到临时文件夹
    /// </summary>
    /// <param name="moduleDir">模块的路径</param>
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
    /// 对单个模块的Src/文件夹和Res/Proto/文件夹下的明文文件进行就地加密
    /// </summary>
    /// <param name="moduleDir">模块的路径</param>
    private static void EncryptOneModule(DirectoryInfo moduleDir)
    {
        EncryptOnePath(Path.Combine(moduleDir.FullName, "Src"));

        EncryptOnePath(Path.Combine(moduleDir.FullName, "Res/Proto"));
    }

    /// <summary>
    /// 对单个路径下的所有资源进行加密，并生成对应的加密文件
    /// </summary>
    /// <param name="path"></param>
    private static void EncryptOnePath(string path)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);
        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach(FileInfo fileInfo in fileInfoList)
        {
            //我们只对lua文件和proto文件需要加密
            if(fileInfo.FullName.EndsWith(".lua") == false && fileInfo.FullName.EndsWith(".proto") == false)
            {
                continue;
            }
            //读取明文数据
            string plainText = File.ReadAllText(fileInfo.FullName);
            //进行ASE加密
            string cipherText = AESHelper.Encrypt(plainText, AESHelper.keyValue);
            // 创建加密后的文件
            CreateEncryptFile(fileInfo.FullName + ".bytes", cipherText);
        }
        DirectoryInfo[] Dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo oneDirInfo in Dirs)
        {
            EncryptOnePath(oneDirInfo.FullName);
        }
    }

    /// <summary>
    /// 把Src/文件夹和Res/Proto/文件夹的明文文件就地删除
    /// </summary>
    /// <param name="moduleDir">模块的路径</param>
    private static void DeleteOneModule(DirectoryInfo moduleDir)
    {
        DeleteOnePath(Path.Combine(moduleDir.FullName, "Src"));

        DeleteOnePath(Path.Combine(moduleDir.FullName, "Res/Proto"));
    }

    /// <summary>
    /// 对单个路径下的lua或者proto明文文件进行删除
    /// </summary>
    /// <param name="path"></param>
    private static void DeleteOnePath(string path)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(path);

        FileInfo[] fileInfoList = directoryInfo.GetFiles();

        foreach (FileInfo fileInfo in fileInfoList)
        {
            // 我们只对lua文件和proto明文文件进行删除

            if (fileInfo.FullName.EndsWith(".lua") == false &&
                fileInfo.FullName.EndsWith(".lua.meta") == false &&
                fileInfo.FullName.EndsWith(".proto") == false &&
                fileInfo.FullName.EndsWith(".proto.meta") == false)
            {
                continue;
            }

            // 删除明文文件和其对应的meta文件

            fileInfo.Delete();
        }

        DirectoryInfo[] Dirs = directoryInfo.GetDirectories();

        foreach (DirectoryInfo oneDirInfo in Dirs)
        {
            DeleteOnePath(oneDirInfo.FullName);
        }
    }

    /// <summary>
    /// 等所有模块的AB包都生成完成以后
    /// 1. 删除GAssets的各个模块中的加密文件
    /// 2. 然后把存放在临时文件夹的明文文件再拷贝回GAssets的各个模块中
    /// 3. 最后要删除临时文件夹
    /// </summary>
    private static void RestoreModules()
    {
        DirectoryInfo rootDir = new DirectoryInfo(rootPath);

        DirectoryInfo[] Dirs = rootDir.GetDirectories();

        foreach (DirectoryInfo moduleDir in Dirs)
        {
            // 处理Lua文件夹

            string luaPath = Path.Combine(moduleDir.FullName, "Src");

            Directory.Delete(luaPath, true);

            string tempLuaPath = Path.Combine(tempGAssets, moduleDir.Name, "Src");

            CopyFolder(tempLuaPath, luaPath);

            // 处理Proto文件夹

            string protoPath = Path.Combine(moduleDir.FullName, "Res/Proto");

            Directory.Delete(protoPath, true);

            string tempProtoPath = Path.Combine(tempGAssets, moduleDir.Name, "Res/Proto");

            CopyFolder(tempProtoPath, protoPath);
        }

        // 删除临时文件夹

        Directory.Delete(tempGAssets, true);
    }
    /// <summary>
    /// 创建加密后的文件
    /// </summary>
    /// <param name="filePath">密文文件的路径</param>
    /// <param name="fileText">密文的内容</param>
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
    /// 工具函数,复制文件夹
    /// </summary>
    /// <param name="sourceFolder">原文件夹路径</param>
    /// <param name="destFolder">目标文件夹路径</param>
    private static void CopyFolder(string sourceFolder,string destFolder)
    {
        try
        {
            if(Directory.Exists(destFolder) == true)
            {
                Directory.Delete(destFolder, true);
            }

            Directory.CreateDirectory(destFolder);
            //得到原文件夹的子文件列表
            string[] filePathList = Directory.GetFiles(sourceFolder);

            foreach (string filePath in filePathList)
            {
                string fileName = Path.GetFileName(filePath);

                string destPath = Path.Combine(destFolder, fileName);

                File.Copy(filePath, destPath);
            }

            //得到原文件夹下的所有子文件夹
            string[] folders = Directory.GetDirectories(sourceFolder);

            foreach (string srcPath in folders)
            {
                string folderName = Path.GetFileName(srcPath);

                string destPath = Path.Combine(destFolder, folderName);

                CopyFolder(srcPath, destPath);//构建目标路径,递归复制文件
            }
        }
        catch (Exception e)
        {
            Debug.LogError("复制文件夹出错：" + e.ToString());
        }
    }
}


