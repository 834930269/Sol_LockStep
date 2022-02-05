using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 有限状态机状态基类
/// T:有限状态机持有者类型
/// </summary>
public abstract class FsmState<T> where T : class
{
    /// <summary>
    /// 初始化
    /// </summary>
    public FsmState()
    {

    }

    public IFsm<T> owner;

    /// <summary>
    /// 有限状态机进入时调用
    /// </summary>
    /// <param name="fsm"></param>
    public virtual void OnInit(IFsm<T> fsm)
    {
        owner = fsm;
    }

    /// <summary>
    /// 有限状态机状态进入时调用。
    /// </summary>
    /// <param name="fsm">有限状态机引用。</param>
    public virtual void OnEnter(IFsm<T> fsm)
    {
    }

    /// <summary>
    /// 有限状态机轮询时调用
    /// </summary>
    /// <param name="fsm">有限状态机引用</param>
    /// <param name="elapseSeconds">逻辑流逝时间,以秒为单位</param>
    /// <param name="realElapseSeconds">真实流逝时间,以秒为单位</param>
    public virtual void OnUpdate(IFsm<T> fsm,float elapseSeconds,float realElapseSeconds)
    {

    }
    /// <summary>
    /// 有限状态机离开时调用
    /// </summary>
    /// <param name="fsm">有限状态机实例</param>
    /// <param name="isShutDown">是否关闭有限状态机时触发</param>
    public virtual void OnLeave(IFsm<T> fsm,bool isShutDown)
    {

    }
    /// <summary>
    /// 有限状态机销毁时调用
    /// </summary>
    /// <param name="fsm"></param>
    public virtual void OnDestroy(IFsm<T> fsm)
    {

    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="fsm"></param>
    public void ChangeState<TState>(IFsm<T> fsm) where TState : FsmState<T>
    {
        Fsm<T> fsmImplement = (Fsm<T>)fsm;
        if (fsmImplement == null)
        {
            Debuger.LogError("FSM is invalid.");
            return;
        }

        fsmImplement.ChangeState<TState>();
    }
    /// <summary>
    /// 切换当前有限状态机状态。
    /// </summary>
    /// <param name="fsm">有限状态机引用。</param>
    /// <param name="stateType">要切换到的有限状态机状态类型。</param>
    public void ChangeState(IFsm<T> fsm, Type stateType)
    {
        Fsm<T> fsmImplement = (Fsm<T>)fsm;
        if (fsmImplement == null)
        {
            Debuger.LogError("FSM is invalid.");
            return;
        }

        if (stateType == null)
        {
            Debuger.LogError("State type is invalid.");
            return;
        }

        if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
        {
            Debuger.LogError(string.Format("State type '{0}' is invalid.", stateType.FullName));
        }

        fsmImplement.ChangeState(stateType);
    }

}
