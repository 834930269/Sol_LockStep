using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fsm<T> : FsmBase,IFsm<T> where T:class
{
    private T m_Owner;

    private readonly Dictionary<Type, FsmState<T>> m_States;
    private Dictionary<string, Variable> m_Datas;
    private FsmState<T> m_CurrentState;
    private float m_CurrentStateTime;
    private bool m_IsDestroyed;

    /// <summary>
    /// 初始化有限状态机的新实例
    /// </summary>
    public Fsm()
    {
        m_Owner = null;
        m_States = new Dictionary<Type, FsmState<T>>();
        m_Datas = null;
        m_CurrentState = null;
        m_CurrentStateTime = 0f;
        m_IsDestroyed = true;

    }

    public T Owner => (m_Owner);

    /// <summary>
    /// 获取有限状态机持有者类型。
    /// </summary>
    public override Type OwnerType
    {
        get
        {
            return typeof(T);
        }
    }

    /// <summary>
    /// 获取有限状态机中状态的数量。
    /// </summary>
    public override int FsmStateCount
    {
        get
        {
            return m_States.Count;
        }
    }


    /// <summary>
    /// 获取有限状态机是否正在运行。
    /// </summary>
    public override bool IsRunning
    {
        get
        {
            return m_CurrentState != null;
        }
    }


    /// <summary>
    /// 获取有限状态机是否被销毁。
    /// </summary>
    public override bool IsDestroyed
    {
        get
        {
            return m_IsDestroyed;
        }
    }


    /// <summary>
    /// 获取当前有限状态机状态。
    /// </summary>
    public FsmState<T> CurrentState
    {
        get
        {
            return m_CurrentState;
        }
    }


    /// <summary>
    /// 获取当前有限状态机状态名称。
    /// </summary>
    public override string CurrentStateName
    {
        get
        {
            return m_CurrentState != null ? m_CurrentState.GetType().FullName : null;
        }
    }
    /// <summary>
    /// 获取当前有限状态机状态持续时间。
    /// </summary>
    public override float CurrentStateTime
    {
        get
        {
            return m_CurrentStateTime;
        }
    }

    /// <summary>
    /// 创建有限状态机
    /// </summary>
    /// <param name="name">有限状态机名称。</param>
    /// <param name="owner">有限状态机持有者。</param>
    /// <param name="states">有限状态机状态集合。</param>
    /// <returns>创建的有限状态机。</returns>
    public static Fsm<T> Create(string name,T owner,params FsmState<T>[] states)
    {
        if(owner == null)
        {
            Debuger.LogError("FSM owner is invalid.");
            throw new Exception("FSM owner is invalid.");
        }
        if (states == null)
        {
            Debuger.LogError("FSM states is invalid.");
            throw new Exception("FSM states is invalid.");
        }

        Fsm<T> fsm = new Fsm<T>();
        fsm.Name = name;
        fsm.m_Owner = owner;
        fsm.m_IsDestroyed = false;
        foreach (FsmState<T> state in states)
        {
            if(state == null)
            {
                Debuger.LogError("FSM states is invalid.");
                throw new Exception("FSM states is invalid.");
            }
            Type stateType = state.GetType();
            if (fsm.m_States.ContainsKey(stateType))
            {
                Debuger.LogError(string.Format("FSM '{0}' state '{1}' is already exist.", new TypeNamePair(typeof(T), name).ToString(), stateType));
                throw new Exception(string.Format("FSM '{0}' state '{1}' is already exist.", new TypeNamePair(typeof(T), name).ToString(), stateType));
            }

            fsm.m_States.Add(stateType, state);
            state.OnInit(fsm);
        }
        return fsm;
    }

    /// <summary>
    /// 创建有限状态机。
    /// </summary>
    /// <param name="name">有限状态机名称。</param>
    /// <param name="owner">有限状态机持有者。</param>
    /// <param name="states">有限状态机状态集合。</param>
    /// <returns>创建的有限状态机。</returns>
    public static Fsm<T> Create(string name, T owner, List<FsmState<T>> states)
    {
        if (owner == null)
        {
            throw new Exception("FSM owner is invalid.");
        }

        if (states == null || states.Count < 1)
        {
            throw new Exception("FSM states is invalid.");
        }

        Fsm<T> fsm = new Fsm<T>();
        fsm.Name = name;
        fsm.m_Owner = owner;
        fsm.m_IsDestroyed = false;
        foreach (FsmState<T> state in states)
        {
            if (state == null)
            {
                throw new Exception("FSM states is invalid.");
            }

            Type stateType = state.GetType();
            if (fsm.m_States.ContainsKey(stateType))
            {
                throw new Exception(string.Format("FSM '{0}' state '{1}' is already exist.", new TypeNamePair(typeof(T), name).ToString(), stateType));
            }

            fsm.m_States.Add(stateType, state);
            state.OnInit(fsm);
        }

        return fsm;
    }

    public FsmState<T>[] GetAllStates()
    {
        int index = 0;
        FsmState<T>[] results = new FsmState<T>[m_States.Count];
        foreach (KeyValuePair<Type, FsmState<T>> state in m_States)
        {
            results[index++] = state.Value;
        }

        return results;
    }

    public void GetAllStates(List<FsmState<T>> results)
    {
        if (results == null)
        {
            Debuger.LogError("Results is invalid.");
            throw new Exception("Results is invalid.");
        }

        results.Clear();
        foreach (KeyValuePair<Type, FsmState<T>> state in m_States)
        {
            results.Add(state.Value);
        }
    }

    public TData GetData<TData>(string name) where TData : Variable
    {
        return (TData)GetData(name);
    }

    public Variable GetData(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debuger.LogError("Data name is invalid.");
            throw new Exception("Data name is invalid.");
        }
        if (m_Datas == null)
        {
            return null;
        }
        Variable data = null;
        if(m_Datas.TryGetValue(name,out data))
        {
            return data;
        }
        return null;
    }

    public TState GetState<TState>() where TState : FsmState<T>
    {
        FsmState<T> state = null;
        if(m_States.TryGetValue(typeof(TState),out state))
        {
            return (TState)state;
        }
        return null;
    }

    public FsmState<T> GetState(Type stateType)
    {
        if (stateType == null)
        {
            throw new Exception("State type is invalid.");
        }
        if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
        {
            throw new Exception(string.Format("State type '{0}' is invalid.", stateType.FullName));
        }

        FsmState<T> state = null;
        if (m_States.TryGetValue(stateType, out state))
        {
            return state;
        }

        return null;
    }

    public bool HasData(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debuger.LogError("Data name is invalid.");
            throw new Exception("Data name is invalid.");
        }

        if (m_Datas == null)
        {
            return false;
        }

        return m_Datas.ContainsKey(name);
    }

    public bool HasState<TState>() where TState : FsmState<T>
    {
        return m_States.ContainsKey(typeof(TState));
    }
    public bool HasState(Type stateType)
    {
        if (stateType == null)
        {
            throw new Exception("State type is invalid.");
        }

        if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
        {
            throw new Exception(string.Format("State type '{0}' is invalid.", stateType.FullName));
        }

        return m_States.ContainsKey(stateType);
    }

    public bool RemoveData(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new Exception("Data name is invalid.");
        }

        if (m_Datas == null)
        {
            return false;
        }

        Variable oldData = GetData(name);
        if (oldData != null)
        {
            oldData.Clear();
        }

        return m_Datas.Remove(name);
    }

    /// <summary>
    /// 设置有限状态机数据。
    /// </summary>
    /// <typeparam name="TData">要设置的有限状态机数据的类型。</typeparam>
    /// <param name="name">有限状态机数据名称。</param>
    /// <param name="data">要设置的有限状态机数据。</param>
    public void SetData<TData>(string name, TData data) where TData : Variable
    {
        SetData(name, (Variable)data);
    }

    /// <summary>
    /// 设置有限状态机数据。
    /// </summary>
    /// <param name="name">有限状态机数据名称。</param>
    /// <param name="data">要设置的有限状态机数据。</param>
    public void SetData(string name, Variable data)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new Exception("Data name is invalid.");
        }

        if (m_Datas == null)
        {
            m_Datas = new Dictionary<string, Variable>(StringComparer.Ordinal);
        }

        Variable oldData = GetData(name);
        if (oldData != null)
        {
            oldData.Clear();
        }

        m_Datas[name] = data;
    }

    /// <summary>
    /// 开始有限状态机。
    /// </summary>
    /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
    public void Start<TState>() where TState : FsmState<T>
    {
        if (IsRunning)
        {
            throw new Exception("FSM is running, can not start again.");
        }

        FsmState<T> state = GetState<TState>();
        if (state == null)
        {
            throw new Exception(string.Format("FSM '{0}' can not start state '{1}' which is not exist.", new TypeNamePair(typeof(T), Name).ToString(), typeof(TState).FullName));
        }

        m_CurrentStateTime = 0f;
        m_CurrentState = state;
        m_CurrentState.OnEnter(this);
    }

    /// <summary>
    /// 开始有限状态机。
    /// </summary>
    /// <param name="stateType">要开始的有限状态机状态类型。</param>
    public void Start(Type stateType)
    {
        if (IsRunning)
        {
            throw new Exception("FSM is running, can not start again.");
        }

        if (stateType == null)
        {
            throw new Exception("State type is invalid.");
        }

        if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
        {
            throw new Exception(string.Format("State type '{0}' is invalid.", stateType.FullName));
        }

        FsmState<T> state = GetState(stateType);
        if (state == null)
        {
            throw new Exception(string.Format("FSM '{0}' can not start state '{1}' which is not exist.", new TypeNamePair(typeof(T), Name).ToString(), stateType.FullName));
        }

        m_CurrentStateTime = 0f;
        m_CurrentState = state;
        m_CurrentState.OnEnter(this);
    }

    /// <summary>
    /// 有限状态机轮询。
    /// </summary>
    /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
    /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
    public override void Update(float elapseSeconds, float realElapseSeconds)
    {
        if (m_CurrentState == null)
        {
            return;
        }

        m_CurrentStateTime += elapseSeconds;
        m_CurrentState.OnUpdate(this, elapseSeconds, realElapseSeconds);
    }

    /// <summary>
    /// 关闭并清理有限状态机。
    /// </summary>
    public override void Shutdown()
    {
        //ReferencePool.Release(this);
    }

    /// <summary>
    /// 切换当前有限状态机状态。
    /// </summary>
    /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
    public void ChangeState<TState>() where TState : FsmState<T>
    {
        ChangeState(typeof(TState));
    }

    /// <summary>
    /// 切换当前有限状态机状态。
    /// </summary>
    /// <param name="stateType">要切换到的有限状态机状态类型。</param>
    public void ChangeState(Type stateType)
    {
        if (m_CurrentState == null)
        {
            throw new Exception("Current state is invalid.");
        }

        FsmState<T> state = GetState(stateType);
        if (state == null)
        {
            throw new Exception(string.Format("FSM '{0}' can not change state to '{1}' which is not exist.", new TypeNamePair(typeof(T), Name).ToString(), stateType.FullName));
        }

        m_CurrentState.OnLeave(this, false);
        m_CurrentStateTime = 0f;
        m_CurrentState = state;
        m_CurrentState.OnEnter(this);
    }
}
