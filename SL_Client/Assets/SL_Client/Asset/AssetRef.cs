using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ڴ��е�����Դ����
/// </summary>
public class AssetRef 
{
    /// <summary>
    /// �����Դ��������Ϣ
    /// </summary>
    public AssetInfo assetInfo;

    /// <summary>
    /// �����Դ������BundleRef����
    /// </summary>
    public BundleRef bundleRef;

    /// <summary>
    /// �����Դ��������BundleRef�����б�
    /// </summary>
    public BundleRef[] dependencies;

    /// <summary>
    /// ��bundle�ļ�����ȡ��������Դ����
    /// </summary>
    public Object asset;

    /// <summary>
    /// �����Դ�Ƿ���prefab
    /// </summary>
    public bool isGameObject;

    /// <summary>
    /// ���AssetRef������ЩGameObject������
    /// </summary>
    public List<GameObject> children;

    /// <summary>
    /// AssetRef����Ĺ��캯��
    /// </summary>
    /// <param name="assetInfo"></param>
    public AssetRef(AssetInfo assetInfo)
    {
        this.assetInfo = assetInfo;
    }
}
