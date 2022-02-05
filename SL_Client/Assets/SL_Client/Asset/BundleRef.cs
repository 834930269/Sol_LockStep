using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ڴ��е�һ��Bundle����
/// </summary>
public class BundleRef
{
    /// <summary>
    /// ���bundle�ľ�̬������Ϣ
    /// </summary>
    public BundleInfo bundleInfo;

    /// <summary>
    /// ���ص��ڴ��е�bundle����
    /// </summary>
    public AssetBundle bundle;

    /// <summary>
    /// ��ЩBundleRef������ЩAssetRef��������
    /// </summary>
    public List<AssetRef> children;

    /// <summary>
    /// ��¼���BundleRef��Ӧ��AB�ļ���Ҫ���������
    /// </summary>
    public BaseOrUpdate witch;

    /// <summary>
    /// BundleRef�Ĺ��캯��
    /// </summary>
    /// <param name="bundleInfo"></param>
    public BundleRef(BundleInfo bundleInfo,BaseOrUpdate wicth_)
    {
        this.bundleInfo = bundleInfo;

        this.witch = wicth_;
    }
}
