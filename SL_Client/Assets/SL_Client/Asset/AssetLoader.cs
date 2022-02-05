using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
/// <summary>
/// ģ����Դ������
/// </summary>
public class AssetLoader
{
    public static AssetLoader Instance;
    /// <summary>
    /// ����ģ���AB��Դ�����ļ�
    /// </summary>
    /// <param name="baseOrUpdate">ֻ��·�����ǿɶ���д·��</param>
    /// <param name="moduleName">ģ������</param>
    /// <param name="bundleConfigName">AB��Դ�����ļ�������</param>
    /// <returns></returns>
    public async Task<ModuleABConfig> LoadAssetBundleConfig(BaseOrUpdate baseOrUpdate,string moduleName,string bundleConfigName)
    {
        string url = BundlePath(baseOrUpdate, moduleName, bundleConfigName);
        UnityWebRequest request = UnityWebRequest.Get(url);
        await request.SendWebRequest();

        if(string.IsNullOrEmpty(request.error) == true)
        {
            return JsonMapper.ToObject<ModuleABConfig>(request.downloadHandler.text);
        }
        return null;
    }

    #region ����ʱ 
    /// <summary>
    /// ƽ̨��Ӧ��ֻ��·���µ���Դ
    /// streamingAssetPath�Ǹ�·��
    /// </summary>
    public Dictionary<string, Hashtable> base2Assets;
    /// <summary>
    /// �ɶ���д·��
    /// persistentDataPath�Ǹ�·��
    /// </summary>
    public Dictionary<string, Hashtable> update2Assets;

    public Dictionary<string, BundleRef> name2BundleRef;
    public AssetLoader()
    {
        base2Assets = new Dictionary<string, Hashtable>();
        update2Assets = new Dictionary<string, Hashtable>();
        name2BundleRef = new Dictionary<string, BundleRef>();
    }

    /// <summary>
    /// ����ģ���json�����ļ� ���� �ڴ��е���Դ����
    /// </summary>
    /// <param name="moduleABConfig"></param>
    /// <returns></returns>
    public Hashtable ConfigAssembly(ModuleABConfig moduleABConfig)
    {
        //����Path->assetRef�Ķ���
        Hashtable Path2AssetRef = new Hashtable();

        for(int i = 0; i < moduleABConfig.AssetArray.Length; ++i)
        {
            AssetInfo assetInfo = moduleABConfig.AssetArray[i];

            //װ��һ��AssetRef����
            AssetRef assetRef = new AssetRef(assetInfo);
            assetRef.bundleRef = name2BundleRef[assetInfo.bundle_name];

            int count = assetInfo.dependencies.Count;
            //�����Դ��������ЩBundle
            assetRef.dependencies = new BundleRef[count];
            //����ЩBundle������Ϣ���浽�ڴ���
            for(int index = 0; index < count; ++index)
            {
                string bundleName = assetInfo.dependencies[index];
                assetRef.dependencies[index] = name2BundleRef[bundleName];
            }
            //·����AssetRef��Ӧ����
            Path2AssetRef.Add(assetInfo.asset_path,assetRef);
        }

        return Path2AssetRef;
    }


    /// <summary>
    /// ��¡һ��GameObject����
    /// </summary>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="Path"></param>
    /// <returns></returns>
    public GameObject Clone(string moduleName,string path)
    {
        //�Ȱ�������������
        AssetRef assetRef = LoadAssetRef<GameObject>(moduleName, path);
        if(assetRef == null || assetRef.asset == null)
        {
            return null;
        }
        //��asset��ʵ��������
        GameObject gameObject = UnityEngine.Object.Instantiate(assetRef.asset) as GameObject;
        
        //�ѵ�ǰʵ�����õĶ���ŵ���Դ�����ļ�¼��
        if(assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }

        assetRef.children.Add(gameObject);

        return gameObject;
    }

    /// <summary>
    /// ����AssetRef����
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moduleName">ģ������</param>
    /// <param name="assetPath">��Դ�����·��</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef<T>(string moduleName,string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if(GlobalConfig.BundleMode == false)
        {
            return LoadAssetRef_Editor<T>(moduleName, assetPath);
        }
        else
        {
            return LoadAssetRef_Runtime<T>(moduleName, assetPath);
        }
#else
        return LoadAssetRef_Runtime<T>(moduleName, assetPath);
#endif
    }

    /// <summary>
    /// Editor״̬��ֱ�Ӵ���Դ���м�����Դ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Editor<T>(string moduleName,string assetPath)where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }
        AssetRef assetRef = new AssetRef(null);
        assetRef.asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        return assetRef;
#else
        return null;
#endif
    }

    private AssetRef LoadAssetRef_Runtime<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        Hashtable module2AssetRef;

        if(GlobalConfig.HotUpdate == true)
        {
            module2AssetRef = update2Assets[moduleName];
        }
        else
        {
            module2AssetRef = base2Assets[moduleName];
        }

        AssetRef assetRef = (AssetRef)module2AssetRef[assetPath];

        if(assetRef == null)
        {
            Debug.LogError("δ�ҵ���Դ��moduleName " + moduleName + " path " + assetPath);

            return null;
        }

        //���assetRef��������,�����Ѿ�������һ�λ�ȡ,��û��ж��
        if(assetRef.asset != null)
        {
            return assetRef;
        }

        //�ߵ���һ��,������Ҫ���������Դ
        //���������Դ��Ҫ���������Դ��Ӧ��������Դ
        //1. ����assetRef������BundleRef�б�
        foreach(BundleRef oneBundleRef in assetRef.dependencies)
        {
            if(oneBundleRef.bundle == null)
            {
                string bundlePath = BundlePath(oneBundleRef.witch,moduleName, oneBundleRef.bundleInfo.bundle_name);
                oneBundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            if(oneBundleRef.children == null)
            {
                oneBundleRef.children = new List<AssetRef>();
            }

            oneBundleRef.children.Add(assetRef);
        }

        //2. ����assetRef���ڵ��Ǹ�BundleRef����
        //�����Լ���bundle
        BundleRef bundleRef = assetRef.bundleRef;
        if(bundleRef.bundle == null)
        {
            bundleRef.bundle = AssetBundle.LoadFromFile(BundlePath(bundleRef.witch,moduleName, bundleRef.bundleInfo.bundle_name));
        }

        if(bundleRef.children == null)
        {
            bundleRef.children = new List<AssetRef>();
        }

        bundleRef.children.Add(assetRef);

        //3. ��bundle����ȡasset
        //����֮ǰ,�Ѿ������������ӽ�ȥ��
        assetRef.asset = assetRef.bundleRef.bundle.LoadAsset<T>(assetRef.assetInfo.asset_path);

        if(typeof(T) == typeof(GameObject) && assetRef.assetInfo.asset_path.EndsWith(".prefab"))
        {
            assetRef.isGameObject = true;
        }
        else
        {
            assetRef.isGameObject = false;
        }

        return assetRef;
    }

    /// <summary>
    /// ���ߺ���,����ģ�����ֺ�bundle����,����ʵ����Դ·��
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="bundleName"></param>
    /// <returns></returns>
    private string BundlePath(BaseOrUpdate baseOrUpdate,string moduleName,string bundleName)
    {
        if(baseOrUpdate == BaseOrUpdate.Update)
        {
            return Application.persistentDataPath + "/Bundles/" + moduleName + "/" + bundleName;
        }
        else
        {
            return Application.streamingAssetsPath + "/" + moduleName + "/" + bundleName;
        }
    }

    /// <summary>
    /// ������Դ����,���ҽ��丳����Ϸ����gameObject
    /// 
    /// Tag1: ע��,�����GameObject����Դ��Ҫ���ص�GameObject��
    /// ���������Դ�����������Ǻ���������йص�
    /// �����Ҫ���GameObject��������������Դ���ͷ�ʱ��
    /// 
    /// </summary>
    /// <typeparam name="T">��Դ����</typeparam>
    /// <param name="moduleName">ģ�������</param>
    /// <param name="assetPath">��Դ��·��</param>
    /// <param name="gameObject">��Դ���غ�,Ҫ���ص�����Ϸ����</param>
    /// <returns></returns>
    public T CreateAsset<T>(string moduleName,string assetPath,GameObject gameObject) where T: UnityEngine.Object
    {
        if(typeof(T) == typeof(GameObject) || (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("�����Լ���GameObject����,��ֱ��ʹ��AssetLoader.Instance.Clone�ӿ�,path: " + assetPath);

            return null;
        }

        if(gameObject == null)
        {
            Debug.LogError("CreateAsset���봫��һ��gameObject�佫Ҫ���ص�GameObject����");

            return null;
        }

        AssetRef assetRef = LoadAssetRef<T>(moduleName, assetPath);

        if(assetRef == null || assetRef.asset == null)
        {
            return null;
        }

        if(assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }

        assetRef.children.Add(gameObject);
        return assetRef.asset as T;
    }

    /// <summary>
    /// ȫ��ж�غ���
    /// </summary>
    /// <param name="module2Assets"></param>
    public void Unload(Dictionary<string,Hashtable> module2Assets)
    {
        foreach(string moduleName in module2Assets.Keys)
        {
            Hashtable Path2AssetRef = module2Assets[moduleName];

            if(Path2AssetRef == null)
            {
                continue;
            }

            foreach(AssetRef assetRef in Path2AssetRef.Values)
            {
                if(assetRef.children==null || assetRef.children.Count == 0)
                {
                    continue;
                }

                for(int i =assetRef.children.Count - 1; i >= 0; --i)
                {
                    GameObject go = assetRef.children[i];

                    if(go == null)
                    {
                        assetRef.children.RemoveAt(i);
                    }
                }

                //��������ԴassetRef�Ѿ�û�б��κ�GameObject��������,��ôassetRef�Ϳ���ж����
                if(assetRef.children.Count == 0)
                {
                    assetRef.asset = null;
                    Resources.UnloadUnusedAssets();

                    //����assetRef���������bundle,�����ϵ
                    assetRef.bundleRef.children.Remove(assetRef);

                    if(assetRef.bundleRef.children.Count == 0)
                    {
                        assetRef.bundleRef.bundle.Unload(true);
                    }

                    //����assetRef����������Щbundle�б�,�����ϵ
                    foreach(BundleRef bundleRef in assetRef.dependencies)
                    {
                        bundleRef.children.Remove(assetRef);
                        if(bundleRef.children.Count == 0)
                        {
                            //bundleж�ص�ʱ��,˳�����е���ԴҲһ��ж����
                            bundleRef.bundle.Unload(true);
                        }
                    }
                }
            }

        }
    }


#endregion
}
