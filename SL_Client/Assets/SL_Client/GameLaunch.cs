using PlayerMsg;
using RoomMsg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLaunch : MonoSingleton<GameLaunch>
{
    public static Msg_PlayerInput CurGameInput = new Msg_PlayerInput();

    [Header("服务端IP")]
    public string hostAddr;
    [Header("服务端端口")]
    public int hostPort;

    [Header("客户端模式")]
    public bool IsClientMode;
    public Msg_PlayerServerInfo ClientModeInfo = new Msg_PlayerServerInfo();

    [Header("回放模式")]
    public bool IsReplay = false;
    public string recordFilePath;
    private static int _maxServerFrameIdx;

    [Header("帧数据")]
    public int mapId;
    private bool _hasStart = false;

    [HideInInspector]public int predictTickCount = 3;
    [HideInInspector]public int inputTick;
    [HideInInspector]public int localPlayerId = 0;
    [HideInInspector]public int playerCount = 1; 
    [HideInInspector]public int curMapId = 0;
    public int curFrameIdx = 0;
    [HideInInspector] public Msg_FrameInput curFrameInput;
    [HideInInspector] public List<Msg_FrameInput> frames = new List<Msg_FrameInput>();

    [Header("Ping")] public static int PingVal;
    public static List<float> Delays = new List<float>();
    public Dictionary<int, float> tick2SendTimer = new Dictionary<int, float>();

    //[Header("游戏数据")] public static List<Player> allPlayers = new List<Player>();

    [HideInInspector] public float remainTime; //Update的延时
    private TCPClient tcpClient;

    private void Awake()
    {
        /*配置热更
        ModuleConfig launchModule = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "20210902121943",
            moduleUrl = "http://10.100.177.91:8000/"
        };
        

        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result == true)
        {
            // 在这里 把代码控制权交给Lua 完毕！
            Debug.Log("Lua 代码开始...");
        }
        */
        DontDestroyOnLoad(this);
        gameObject.AddComponent<PingMono>();
        gameObject.AddComponent<InputMono>();


        //预初始化所有的管理器
        SingletonManager.Instance.InitSingletons();
    }


    private void Start()
    {
        _Start();
    }

    private void _Start()
    {
        DoStart();
        Debuger.LogWarning("在开始游戏前记录IdCounter");
        if(!IsReplay && !IsClientMode)
        {
            //正常开启游戏
            tcpClient = new TCPClient();
            tcpClient.Connect(hostAddr, hostPort);
            //TODO: 发送加入房间的消息

        }
        else
        {
            //回放模式
        }
    }
    private void DoStart()
    {
        if (IsReplay)
        {
            //回放模式
        }
        if (IsClientMode)
        {
            playerCount = 1;
            localPlayerId = 0;
            frames = new List<Msg_FrameInput>();
        }
    }

    private void Update()
    {
        _DoUpdate();
    }

    private void _DoUpdate()
    {
        if (!_hasStart) return;
        remainTime += Time.deltaTime;
        while(remainTime >= 0.03f)
        {
            remainTime -= 0.03f;
            if (!IsReplay)
            {
                //发送输入信息
                SendInput();
            }
            //这里检测是否服务器的帧已经到了
            //发到了进入step
            if(GetFrame(curFrameIdx) == null)
            {
                return;
            }

            //实际逻辑更新
            Step();
        }
    }

    /*连接回调
    public static void StartGame(Msg_StartGame msg)
    {
    }
    */
    public void StartGame(int mapId, Msg_PlayerServerInfo[] playerInfos, int localPlayerId)
    {
        _hasStart = true;
    }

    public void SendInput()
    {
        if (IsClientMode)
        {
            var nI = new Msg_FrameInput()
            {
                Tick = curFrameIdx
            };
            nI.Inputs.Add(CurGameInput);
            //客户端模式,不联网
            PushFrameInput(nI);
            return;
        }

        predictTickCount = 2;
        if(inputTick > predictTickCount + _maxServerFrameIdx)
        {
            return;
        }
        var playerInput = CurGameInput;
        //在这里发送用户的输入
        //tcpClient?.SendAsync();

        tick2SendTimer[inputTick] = Time.realtimeSinceStartup;
        inputTick++;
    }

    public static void PushFrameInput(Msg_FrameInput input)
    {
        var frames = GameLaunch.Instance.frames;
        for(int i = frames.Count;i<= input.Tick; ++i)
        {
            frames.Add(new Msg_FrameInput());
        }

        if (frames.Count == 0)
        {
            Instance.remainTime = 0;
        }

        _maxServerFrameIdx = Mathf.Max(_maxServerFrameIdx, input.Tick);
        if(Instance.tick2SendTimer.TryGetValue(input.Tick, out var val))
        {
            Delays.Add(Time.realtimeSinceStartup - val);
        }
        frames[input.Tick] = input;
    }

    public Msg_FrameInput GetFrame(int tick)
    {
        if(frames.Count > tick)
        {
            var frame = frames[tick];
            if(frame!=null && frame.Tick == tick)
            {
                return frame;
            }
        }
        return null;
    }

    private void Step()
    {
        //更新每个角色的输入
        UpdateFrameInput();
        if (IsReplay)
        {
            /*回放模式
            if (curFrameIdx < frames.Count)
            {
                Replay(curFrameIdx);
                curFrameIdx++;
            }
            */
        }
        else
        {
            //先记录当前帧的输入
            //并进入真正的Update
            //Recoder();
            //发送当前帧的Hash
            //tcpClient?.SendAsync();
            curFrameIdx++;
        }
    }

    /// <summary>
    /// 先将服务器里的帧设置到全局变量中
    /// 同时将每个玩家的帧数数据赋值到每个玩家身上
    /// 在这里赋值inputAgent
    /// </summary>
    private void UpdateFrameInput()
    {
        curFrameInput = GetFrame(curFrameIdx);
        var frame = curFrameInput;
        for (int i = 0; i < playerCount; i++)
        {
            //在这里设置每个角色的输入
            //allPlayers[i].InputAgent = frame.inputs[i];
        }
    }

    /// <summary>
    /// 获得当前帧的Hash值
    /// 用于校验
    /// </summary>
    /// <returns></returns>
    public int GetHash()
    {
        return 0;
    }

    private void Recoder()
    {
        _Update();
    }

    /// <summary>
    /// 回放
    /// </summary>
    /// <param name="frameIdx"></param>
    private void Replay(int frameIdx)
    {
        _Update();
    }

    /// <summary>
    /// 真正的Update
    /// </summary>
    private void _Update()
    {
        float deltaTime = 30;
        //实际的Update
        SingletonManager.Instance._DoUpdate();
    }

    private void OnDestroy()
    {
        //退出房间
        //tcpClient?.SendAsync();
    }
}
