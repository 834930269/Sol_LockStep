using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ѡ�� ԭʼֻ��·�� ���� �ɶ���д·��
/// </summary>
public enum BaseOrUpdate
{
    /// <summary>
    /// App��װʱ,���ɵ�ԭʼֻ��·��
    /// </summary>
    Base,
    /// <summary>
    /// App�ṩ�� �ɶ���д·��
    /// </summary>
    Update
} 

/// <summary>
/// ȫ������
/// </summary>
public static class GlobalConfig
{
    /// <summary>
    /// �Ƿ����ȸ�
    /// </summary>
    public static bool HotUpdate;

    /// <summary>
    /// �Ƿ����bundle��ʽ����
    /// </summary>
    public static bool BundleMode;

    /// <summary>
    /// ȫ�����õĹ��캯��
    /// </summary>
    static GlobalConfig()
    {
        HotUpdate = false;
        BundleMode = false;
    }
}

/// <summary>
/// ����ģ������ö���
/// </summary>
public class ModuleConfig {
    /// <summary>
    /// ģ�������
    /// </summary>
    public string moduleName;

    /// <summary>
    /// ģ��İ汾��
    /// </summary>
    public string moduleVersion;

    /// <summary>
    /// ģ����ȸ���������ַ
    /// </summary>
    public string moduleUrl;

    /// <summary>
    /// ģ����Դ��Զ�̷������ϵĻ�����ַ
    /// </summary>
    public string DownloadURL
    {
        get
        {
            return moduleUrl + "/" + moduleName + "/" + moduleVersion;
        }
    }
}

