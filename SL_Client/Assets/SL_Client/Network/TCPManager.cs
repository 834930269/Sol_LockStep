using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCPManager : MonoSingleton<TCPManager>
{
    Action ConnectSuccessCallback;

    private TCPClient tcpClient;
    Dictionary<OpCode.ProtoCode, Action<NetPacket>> taker;

    List<NetPacket> packetQueue;
    void Awake()
    {
        taker = new Dictionary<OpCode.ProtoCode, Action<NetPacket>>();
        packetQueue = new List<NetPacket>();
    }

    void Update()
    {
        tcpClient.GetPackets(packetQueue);
        for(int i = 0; i < packetQueue.Count; ++i)
        {
            if (packetQueue[i].packetType == PacketType.TcpPacket)
            {
                OnTcpPacket(packetQueue[i]);
            }
            else if(packetQueue[i].packetType == PacketType.ConnectSuccess)
            {
                OnConnectSuccess();
            }
            else if (packetQueue[i].packetType == PacketType.ConnectFailed)
            {
                OnConnectFailed();
            }
            else if (packetQueue[i].packetType == PacketType.ConnectDisconnect)
            {
                OnConnectDisconnect();
            }
        }
        packetQueue.Clear();
    }
    public void Connect(string hostAddr,int hostPort,Action ConnectSuccessCallback)
    {
        this.ConnectSuccessCallback = ConnectSuccessCallback;
        if (tcpClient != null)
        {
            tcpClient.Disconnect();
        }
        tcpClient = new TCPClient();
        tcpClient.Connect(hostAddr, hostPort);
    }

    public void Send(int pCode, byte[] body)
    {
        tcpClient?.SendAsync(pCode,body);
    }

    /// <summary>
    /// 注册Tcp包回调
    /// </summary>
    /// <param name="protoCode"></param>
    /// <param name="func"></param>
    public void Register(OpCode.ProtoCode protoCode, Action<NetPacket> func)
    {
        taker.Add(protoCode,func);
    }

    /// <summary>
    /// 注销Tcp包回调
    /// </summary>
    /// <param name="protoCode"></param>
    public void UnRegister(OpCode.ProtoCode protoCode)
    {
        taker.Remove(protoCode);
    }

    /// <summary>
    /// Tcp完整包分发中心
    /// </summary>
    /// <param name="packet"></param>
    private void OnTcpPacket(NetPacket packet)
    {
        Action<NetPacket> action;
        taker.TryGetValue((OpCode.ProtoCode)packet.protoCode,out action);
        action?.Invoke(packet);
    }

    private void OnConnectSuccess()
    {
        Debuger.Log("连接成功!");
        ConnectSuccessCallback?.Invoke();
    }
    private void OnConnectFailed()
    {
        Debuger.LogError("连接失败.");
    }
    private void OnConnectDisconnect()
    {
        Debuger.LogError("断开连接.");
    }
    private void OnDestroy()
    {
        tcpClient.Disconnect();
    }
}
