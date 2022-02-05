using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class SingletonManager : MonoSingleton<SingletonManager>
{
    private GameObject _rootObj;
    private List<IMonoBehaviour> singltonList;
    private List<IApplication> singltonList_Application;

    private static List<Action> _singletonReleaseList = new List<Action>();

    /// <summary>
    /// gizmos绘制辅助
    /// </summary>
    public Action gizmosDrawer;

    private bool isDone = false;
    public bool IsDone => (isDone);

    public void Awake()
    {
        isDone = false;
#if UNITY_EDITOR
        Debuger.debugerEnable = true;
#endif
        Debuger.debugerEnable = true;
        singltonList = new List<IMonoBehaviour>();
        singltonList_Application = new List<IApplication>();
        _rootObj = gameObject;
        GameObject.DontDestroyOnLoad(_rootObj);

    }

    /// <summary>
    /// 在这里进行所有单例的销毁
    /// </summary>
    public void OnApplicationQuit()
    {
        for (int i = _singletonReleaseList.Count - 1; i >= 0; i--)
        {
            _singletonReleaseList[i]();
        }
    }

    public void SetToSingletonList(IMonoBehaviour manager)
    {
        singltonList.Add(manager);
    }

    public void SetToApplicationControlList(IApplication manager)
    {
        singltonList_Application.Add(manager);
    }

    #region 单例集
    ProcedureManager procedureManager;
    FsmManager fsmManager;
    AssetManager assetManager;
    #endregion
    /// <summary>
    /// 在这里进行所有单例的初始化
    /// </summary>
    /// <returns></returns>
    public void InitSingletons()
    {
        // zluaManager = LuaManager.Instance.InitSingleton(this);
        procedureManager = ProcedureManager.Instance.InitSingleton(this);
        fsmManager = FsmManager.Instance.InitSingleton(this);
        assetManager = AssetManager.Instance.InitSingleton(this);

        OnInit();
        _singletonReleaseList.Add(delegate ()
        {
            if (singltonList != null)
            {
                foreach (var subManager in singltonList)
                {
                    if (subManager != null)
                        subManager.OnRelease();
                }
            }
        });
        isDone = true;
    }

    #region 帧同步部分
    public void _DoUpdate()
    {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                {
                    subManager.Update();
                    subManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }
        }
    }
    #endregion

    #region Mono调用模块
    private void OnInit()
    {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.Awake();
            }
        }
    }

    private void FixedUpdate()
    {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.FixedUpdate();
            }
        }
    }

    private void LateUpdate()
    {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.LateUpdate();
            }
        }
    }

    public void OnGUI() {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.OnGUI();
            }
        }
    }
    public void OnDisable() {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.OnDisable();
            }
        }
    }
    public void OnDestroy() {
        if (singltonList != null)
        {
            foreach (var subManager in singltonList)
            {
                if (subManager != null)
                    subManager.OnDestroy();
            }
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (singltonList_Application != null)
        {
            foreach (var subManager in singltonList_Application)
            {
                if (subManager != null)
                    subManager.OnApplicationFocus(focus);
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (singltonList_Application != null)
        {
            foreach (var subManager in singltonList_Application)
            {
                if (subManager != null)
                    subManager.OnApplicationPause(pause);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        gizmosDrawer?.Invoke();
    }
#endif
#endregion
}
