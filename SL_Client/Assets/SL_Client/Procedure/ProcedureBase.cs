using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcedureOwner = IFsm<ProcedureManager>;

public class ProcedureBase : FsmState<ProcedureManager>
{
    /// <summary>
    /// 状态初始化调用
    /// </summary>
    /// <param name="fsm">流程持有者</param>
    public override void OnInit(ProcedureOwner fsm)
    {
        base.OnInit(fsm);
    }

    /// <summary>
    /// 进入状态调用
    /// </summary>
    /// <param name="fsm">流程持有者</param>
    public override void OnEnter(ProcedureOwner fsm)
    {
        base.OnEnter(fsm);
    }

    /// <summary>
    /// 状态轮询调用
    /// </summary>
    /// <param name="fsm">流程持有者</param>
    /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
    /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
    public override void OnUpdate(ProcedureOwner fsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);
    }

    /// <summary>
    /// 离开状态时调用
    /// </summary>
    /// <param name="fsm">流程持有者。</param>
    /// <param name="isShutDown">是否是关闭状态机时触发。</param>
    public override void OnLeave(ProcedureOwner fsm, bool isShutDown)
    {
        base.OnLeave(fsm, isShutDown);
    }
    /// <summary>
    /// 状态销毁时调用
    /// </summary>
    /// <param name="fsm">流程持有者</param>
    public override void OnDestroy(ProcedureOwner fsm)
    {
        base.OnDestroy(fsm);
    }

}
