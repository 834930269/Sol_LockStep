using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��������
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
    /// ��ʼλ��
    /// </summary>
    public Vector3 start { get { return m_start; } }

    /// <summary>
    /// ����λ��
    /// </summary>
    public Vector3 end { get { return m_end; } }
    /// <summary>
    /// ��ʱ��
    /// </summary>
    public float totalTime { get { return m_totalTime; } }
    /// <summary>
    /// ��ʼ�ٶ�
    /// </summary>
    public Vector3 velocityStart { get { return m_velocityStart; } }

    /// <summary>
    /// ��ǰλ��
    /// </summary>
    public Vector3 position { get { return m_position; } }

    /// <summary>
    /// ��ǰ�ٶ�
    /// </summary>
    public Vector3 velocity { get { return GetVelocity(m_time); } }

    /// <summary>
    /// ��ǰʱ��
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

    /// <summary> ��ʼ���������˶��켣 </summary>
    /// <param name="start">���</param>
    /// <param name="end">�յ�</param>
    /// <param name="height">�߶�(���������������λ�� �߳�����)</param>
    /// <param name="gravity">�������ٶ�(����)</param>
    /// <returns></returns>
    public ParabolaPath(Vector3 start, Vector3 end, float height = 10, float gravity = -9.8f)
    {
        Init(start, end, height, gravity);
    }

    /// <summary> ��ʼ���������˶��켣 </summary>
    /// <param name="start">���</param>
    /// <param name="end">�յ�</param>
    /// <param name="height">�߶�(���������������λ�� �߳�����)</param>
    /// <param name="gravity">�������ٶ�(����)</param>
    /// <returns></returns>
    public void Init(Vector3 start, Vector3 end, float height = 10, float gravity = -9.8f)
    {
        //����ߵ�
        float topY = Mathf.Max(start.y, end.y) + height;
        //�����׶ε���ֱ����
        float d1 = topY - start.y;
        // �½��׶ε���ֱ����
        float d2 = topY - end.y;
        float g2 = 2 / -gravity;
        // ���ù�ʽ h = g * t * t / 2 ����������׶ε�ʱ��
        float t1 = Mathf.Sqrt(g2 * d1);

        // ���ù�ʽ h = g * t * t / 2 ������½��׶ε�ʱ��
        float t2 = Mathf.Sqrt(g2 * d2);

        // ���������е���ʱ��
        float t = t1 + t2;

        // �������ˮƽ�����ϵ���������ƶ��ٶ� vX vZ (ͬʱҲ��ˮƽ����ĳ�ʼ�ٶ�)
        float vX = (end.x - start.x) / t;
        float vZ = (end.z - start.z) / t;

        // �������ֱ�����ϵĳ��ٶ� vY
        float vY = -gravity * t1;

        // �������ʼ�ٶȵ�3���������������
        m_start = start;
        m_end = end;
        m_gravity = gravity;
        m_totalTime = t;
        m_velocityStart = new Vector3(vX, vY, vZ);
        m_position = m_start;
        m_time = 0;
    }

    /// <summary>
    /// ��ȡĳ��ʱ����λ��
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
    /// ��ȡĳ��ʱ�����ٶ�
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
