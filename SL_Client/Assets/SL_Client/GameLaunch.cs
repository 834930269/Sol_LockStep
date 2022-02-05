using PlayerMsg;
using RoomMsg;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLaunch : MonoSingleton<GameLaunch>
{
    public static Msg_PlayerInput CurGameInput = new Msg_PlayerInput();

    [Header("�����IP")]
    public string hostAddr;
    [Header("����˶˿�")]
    public int hostPort;

    [Header("�ͻ���ģʽ")]
    public bool IsClientMode;
    public Msg_PlayerServerInfo ClientModeInfo = new Msg_PlayerServerInfo();

    [Header("�ط�ģʽ")]
    public bool IsReplay = false;
    public string recordFilePath;
    private static int _maxServerFrameIdx;

    [Header("֡����")]
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

    //[Header("��Ϸ����")] public static List<Player> allPlayers = new List<Player>();

    [HideInInspector] public float remainTime; //Update����ʱ
    private TCPClient tcpClient;

    private void Awake()
    {
        /*�����ȸ�
        ModuleConfig launchModule = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "20210902121943",
            moduleUrl = "http://10.100.177.91:8000/"
        };
        

        bool result = await ModuleManager.Instance.Load(launchModule);
        if (result == true)
        {
            // ������ �Ѵ������Ȩ����Lua ��ϣ�
            Debug.Log("Lua ���뿪ʼ...");
        }
        */
        DontDestroyOnLoad(this);
        gameObject.AddComponent<PingMono>();
        gameObject.AddComponent<InputMono>();


        //Ԥ��ʼ�����еĹ�����
        SingletonManager.Instance.InitSingletons();
    }


    private void Start()
    {
        _Start();
    }

    private void _Start()
    {
        DoStart();
        Debuger.LogWarning("�ڿ�ʼ��Ϸǰ��¼IdCounter");
        if(!IsReplay && !IsClientMode)
        {
            //����������Ϸ
            tcpClient = new TCPClient();
            tcpClient.Connect(hostAddr, hostPort);
            //TODO: ���ͼ��뷿�����Ϣ

        }
        else
        {
            //�ط�ģʽ
        }
    }
    private void DoStart()
    {
        if (IsReplay)
        {
            //�ط�ģʽ
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
                //����������Ϣ
                SendInput();
            }
            //�������Ƿ��������֡�Ѿ�����
            //�����˽���step
            if(GetFrame(curFrameIdx) == null)
            {
                return;
            }

            //ʵ���߼�����
            Step();
        }
    }

    /*���ӻص�
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
            //�ͻ���ģʽ,������
            PushFrameInput(nI);
            return;
        }

        predictTickCount = 2;
        if(inputTick > predictTickCount + _maxServerFrameIdx)
        {
            return;
        }
        var playerInput = CurGameInput;
        //�����﷢���û�������
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
        //����ÿ����ɫ������
        UpdateFrameInput();
        if (IsReplay)
        {
            /*�ط�ģʽ
            if (curFrameIdx < frames.Count)
            {
                Replay(curFrameIdx);
                curFrameIdx++;
            }
            */
        }
        else
        {
            //�ȼ�¼��ǰ֡������
            //������������Update
            //Recoder();
            //���͵�ǰ֡��Hash
            //tcpClient?.SendAsync();
            curFrameIdx++;
        }
    }

    /// <summary>
    /// �Ƚ����������֡���õ�ȫ�ֱ�����
    /// ͬʱ��ÿ����ҵ�֡�����ݸ�ֵ��ÿ���������
    /// �����︳ֵinputAgent
    /// </summary>
    private void UpdateFrameInput()
    {
        curFrameInput = GetFrame(curFrameIdx);
        var frame = curFrameInput;
        for (int i = 0; i < playerCount; i++)
        {
            //����������ÿ����ɫ������
            //allPlayers[i].InputAgent = frame.inputs[i];
        }
    }

    /// <summary>
    /// ��õ�ǰ֡��Hashֵ
    /// ����У��
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
    /// �ط�
    /// </summary>
    /// <param name="frameIdx"></param>
    private void Replay(int frameIdx)
    {
        _Update();
    }

    /// <summary>
    /// ������Update
    /// </summary>
    private void _Update()
    {
        float deltaTime = 30;
        //ʵ�ʵ�Update
        SingletonManager.Instance._DoUpdate();
    }

    private void OnDestroy()
    {
        //�˳�����
        //tcpClient?.SendAsync();
    }
}
