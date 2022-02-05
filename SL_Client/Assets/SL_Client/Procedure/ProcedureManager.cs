using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 流程管理器,刚开始要注册所有的流程
/// </summary>
public class ProcedureManager : Singleton<ProcedureManager>
{
    private IFsmManager m_FsmManager;
    private IFsm<ProcedureManager> m_ProcedureFsm;

    private List<ProcedureBase> procedures;

    private bool IsInitialized = false;
    public bool IsInit => (IsInitialized);

    /// <summary>
    /// 初始化流程管理的实例
    /// </summary>
    public ProcedureManager()
    {
        m_FsmManager = null;
        m_ProcedureFsm = null;
    }

    public override void Awake()
    {
        base.Awake();
        IsInitialized = false;
        //在这里加流程注册
        procedures = new List<ProcedureBase>();


        singletonManager.Procedure_Initialize(procedures.ToArray());
    }

    /// <summary>
    /// 获取当前流程
    /// </summary>
    public ProcedureBase CurrentProcedure
    {
        get
        {
            if(m_ProcedureFsm == null)
            {
                throw new System.Exception("You must initialize procedure first.");
            }
            return (ProcedureBase)m_ProcedureFsm.CurrentState;
        }
    }
    /// <summary>
    /// 获取当前流程持续时间。
    /// </summary>
    public float CurrentProcedureTime
    {
        get
        {
            if(m_ProcedureFsm == null)
            {
                throw new System.Exception("You must initialize procedure first.");
            }

            return m_ProcedureFsm.CurrentStateTime;
        }
    }

    /// <summary>
    /// 流程轮询管理器
    /// </summary>
    /// <param name="elapseSeconds"></param>
    /// <param name="realElapseSeconds"></param>
    public override void Update(float elapseSeconds, float realElapseSeconds)
    {
        
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Shutdown();
    }

    public void Shutdown()
    {
        if (m_FsmManager != null)
        {
            if (m_ProcedureFsm != null)
            {
                m_FsmManager.DestroyFsm(m_ProcedureFsm);
                m_ProcedureFsm = null;
            }

            m_FsmManager = null;
        }
    }

    /// <summary>
    /// 初始化流程管理器
    /// </summary>
    /// <param name="fsmManager">有限状态机管理器</param>
    /// <param name="procedures">流程管理器包含的流程</param>
    public void Initialize(IFsmManager fsmManager,params ProcedureBase[] procedures)
    {
        if (fsmManager == null)
        {
            throw new System.Exception("FSM manager is invalid.");
        }

        m_FsmManager = fsmManager;
        m_ProcedureFsm = m_FsmManager.CreateFsm(this, procedures);
        IsInitialized = true;
    }

    /// <summary>
    /// 开始流程
    /// </summary>
    /// <typeparam name="T">要开始的流程类型</typeparam>
    public void StartProcedure<T>() where T:ProcedureBase
    {
        if(m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }

        m_ProcedureFsm.Start<T>();
    }

    /// <summary>
    /// 开始流程。
    /// </summary>
    /// <param name="procedureType">要开始的流程类型</param>
    public void StartProcedure(Type procedureType)
    {
        if (m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }
        m_ProcedureFsm.Start(procedureType);
    }
    /// <summary>
    /// 是否存在流程。
    /// </summary>
    /// <typeparam name="T">要检查的流程类型。</typeparam>
    /// <returns>是否存在流程。</returns>
    public bool HasProcedure<T>() where T : ProcedureBase
    {
        if (m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }

        return m_ProcedureFsm.HasState<T>();
    }

    /// <summary>
    /// 是否存在流程。
    /// </summary>
    /// <param name="procedureType">要检查的流程类型。</param>
    /// <returns>是否存在流程。</returns>
    public bool HasProcedure(Type procedureType)
    {
        if (m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }

        return m_ProcedureFsm.HasState(procedureType);
    }

    /// <summary>
    /// 获取流程。
    /// </summary>
    /// <typeparam name="T">要获取的流程类型。</typeparam>
    /// <returns>要获取的流程。</returns>
    public ProcedureBase GetProcedure<T>() where T : ProcedureBase
    {
        if (m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }

        return m_ProcedureFsm.GetState<T>();
    }

    /// <summary>
    /// 获取流程。
    /// </summary>
    /// <param name="procedureType">要获取的流程类型。</param>
    /// <returns>要获取的流程。</returns>
    public ProcedureBase GetProcedure(Type procedureType)
    {
        if (m_ProcedureFsm == null)
        {
            throw new System.Exception("You must initialize procedure first.");
        }

        return (ProcedureBase)m_ProcedureFsm.GetState(procedureType);
    }

    private ProcedureBase AddProcedure(Type procedureType)
    {
        ProcedureBase pd = (ProcedureBase)Activator.CreateInstance(procedureType);
        procedures.Add(pd);
        return pd;
    }

}
