using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SingletonManager
{
    /// <summary>
    /// 初始化流程
    /// </summary>
    /// <param name="procedures"></param>
    public void Procedure_Initialize(ProcedureBase[] procedures)
    {
        if (!procedureManager.IsInit)
        {
            procedureManager.Initialize(fsmManager, procedures);
        }
    }

    /// <summary>
    /// 获取当前的流程
    /// </summary>
    public ProcedureBase CurrentProcedure
    {
        get
        {
            return procedureManager.CurrentProcedure;
        }
    }

    /// <summary>
    /// 获取当前流程时间
    /// </summary>
    public float CurrentProcedureTime
    {
        get
        {
            return procedureManager.CurrentProcedureTime;
        }
    }

    /// <summary>
    /// 是否存在流程。
    /// </summary>
    /// <typeparam name="T">要检查的流程类型。</typeparam>
    /// <returns>是否存在流程。</returns>
    public bool HasProcedure<T>() where T : ProcedureBase
    {
        return procedureManager.HasProcedure<T>();
    }

    /// <summary>
    /// 是否存在流程。
    /// </summary>
    /// <param name="procedureType">要检查的流程类型。</param>
    /// <returns>是否存在流程。</returns>
    public bool HasProcedure(Type procedureType)
    {
        return procedureManager.HasProcedure(procedureType);
    }

    /// <summary>
    /// 获取流程。
    /// </summary>
    /// <typeparam name="T">要获取的流程类型。</typeparam>
    /// <returns>要获取的流程。</returns>
    public ProcedureBase GetProcedure<T>() where T : ProcedureBase
    {
        return procedureManager.GetProcedure<T>();
    }

    /// <summary>
    /// 获取流程。
    /// </summary>
    /// <param name="procedureType">要获取的流程类型。</param>
    /// <returns>要获取的流程。</returns>
    public ProcedureBase GetProcedure(Type procedureType)
    {
        return procedureManager.GetProcedure(procedureType);
    }

    /// <summary>
    /// 开启流程
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void StartProcedure<T>() where T : ProcedureBase
    {
        procedureManager.StartProcedure<T>();
    }

    /// <summary>
    /// 开始流程。
    /// </summary>
    /// <param name="procedureType">要开始的流程类型</param>
    public void StartProcedure(Type procedureType)
    {
        procedureManager.StartProcedure(procedureType);
    }

}
