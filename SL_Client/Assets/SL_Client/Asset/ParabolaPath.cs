using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 抛物线类
/// </summary>
public class ParabolaPath
{
    private Vector3 m_start;
    private Vector3 m_end;
    private float m_gravity;
    private float m_totalTime;
    private Vector3 m_velocityStart;
    private Vector3 m_position;
    private float m_time;
    /// <summary>
    /// 开始位置
    /// </summary>
    public Vector3 start { get { return m_start; } }

    /// <summary>
    /// 结束位置
    /// </summary>
    public Vector3 end { get { return m_end; } }
    /// <summary>
    /// 总时间
    /// </summary>
    public float totalTime { get { return m_totalTime; } }
    /// <summary>
    /// 初始速度
    /// </summary>
    public Vector3 velocityStart { get { return m_velocityStart; } }

    /// <summary>
    /// 当前位置
    /// </summary>
    public Vector3 position { get { return m_position; } }

    /// <summary>
    /// 当前速度
    /// </summary>
    public Vector3 velocity { get { return GetVelocity(m_time); } }

    /// <summary>
    /// 当前时间
    /// </summary>
    public float time
    {
        get
        {
            return m_time;
        }

        set
        {
            value = Mathf.Clamp(value, 0, m_totalTime);

            m_time = value;

            m_position = GetPosition(value);
        }
    }

    /// <summary> 初始化抛物线运动轨迹 </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <param name="height">高度(相对于两个点的最高位置 高出多少)</param>
    /// <param name="gravity">重力加速度(负数)</param>
    /// <returns></returns>
    public ParabolaPath(Vector3 start, Vector3 end, float height = 10, float gravity = -9.8f)
    {
        Init(start, end, height, gravity);
    }

    /// <summary> 初始化抛物线运动轨迹 </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <param name="height">高度(相对于两个点的最高位置 高出多少)</param>
    /// <param name="gravity">重力加速度(负数)</param>
    /// <returns></returns>
    public void Init(Vector3 start, Vector3 end, float height = 10, float gravity = -9.8f)
    {
        //求处最高点
        float topY = Mathf.Max(start.y, end.y) + height;
        //上升阶段的竖直距离
        float d1 = topY - start.y;
        // 下降阶段的竖直距离
        float d2 = topY - end.y;
        float g2 = 2 / -gravity;
        // 利用公式 h = g * t * t / 2 来算出上升阶段的时间
        float t1 = Mathf.Sqrt(g2 * d1);

        // 利用公式 h = g * t * t / 2 来算出下降阶段的时间
        float t2 = Mathf.Sqrt(g2 * d2);

        // 抛物线运行的总时间
        float t = t1 + t2;

        // 计算出在水平方向上的两个轴的移动速度 vX vZ (同时也是水平方向的初始速度)
        float vX = (end.x - start.x) / t;
        float vZ = (end.z - start.z) / t;

        // 计算出竖直方向上的初速度 vY
        float vY = -gravity * t1;

        // 到这里初始速度的3个分量都计算完毕
        m_start = start;
        m_end = end;
        m_gravity = gravity;
        m_totalTime = t;
        m_velocityStart = new Vector3(vX, vY, vZ);
        m_position = m_start;
        m_time = 0;
    }

    /// <summary>
    /// 获取某个时间点的位置
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public Vector3 GetPosition(float time)
    {
        if (time == 0)
        {
            return m_start;
        }

        if (time == m_totalTime)
        {
            return m_end;
        }
        //1/2 g t^2
        float dY = 0.5f * m_gravity * time * time;

        return m_start + m_velocityStart * time + new Vector3(0, dY, 0);
    }

    /// <summary>
    /// 获取某个时间点的速度
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public Vector3 GetVelocity(float time)
    {
        if (time == 0)
        {
            return m_velocityStart;
        }

        return m_velocityStart + new Vector3(0, m_velocityStart.y + m_gravity * time, 0);
    }

}
