using PlayerMsg;
using RoomMsg;
using SL_Server.Net;
using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;

namespace SL_Server.GameLogic
{
    public class Room
    {
        public bool isRunning;
        public const int maxPlayerCount = 2;
        public Msg_PlayerServerInfo[] playerInfos;
        public Session[] playerSessions;
        public int curCount = 0;
        public Dictionary<int, int> id2LocalId = new Dictionary<int, int>();

        private int curTick;

        public Dictionary<int, Msg_PlayerInput[]> tick2Inputs = new Dictionary<int, Msg_PlayerInput[]>();
        public Dictionary<int, int[]> tick2Hashes = new Dictionary<int, int[]>();
        public int curLocalId;

        public void Init(int type)
        {
            playerInfos = new Msg_PlayerServerInfo[maxPlayerCount];
            playerSessions = new Session[maxPlayerCount];
        }

        public void DoUpdate(float timeSinceStartUp, float deltaTime)
        {
            if (!isRunning) return;
            CheckInput();
        }

        private void CheckInput()
        {
            //每次获取输入后检查输入
            if(tick2Inputs.TryGetValue(curTick,out var inputs))
            {
                if (inputs != null)
                {
                    bool isFullInput = true;
                    //如果输入没有完全到达,就不发送
                    for(int i = 0; i < inputs.Length; ++i)
                    {
                        if (inputs[i] == null)
                        {
                            isFullInput = false;
                            break;
                        }
                    }
                    //如果输入完全,就发送
                    if (isFullInput)
                    {
                        BoardInputMsg(curTick, inputs);
                        tick2Inputs.Remove(curTick);
                        curTick++;
                    }
                }
            }
        }

        /// <summary>
        /// 当前帧数据收集完毕,广播
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="inputs"></param>
        public void BoardInputMsg(int tick, Msg_PlayerInput[] inputs)
        {
            var frame = new Msg_FrameInput();
            frame.Inputs.AddRange(inputs);
            frame.Tick = tick;
            //frameInput的ToBytes
            var bytes = frame.ToByteArray();
            //对每个session发送FrameInput
            for(int i = 0; i < maxPlayerCount; ++i)
            {
                Session session = playerSessions[i];
                session.Send((int)OpCode.ProtoCode.SFrameInput,bytes);
            }
        }

        /// <summary>
        /// 当前客户端一个输入到来
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="msg"></param>
        public void OnPlayerInput(int userId, Msg_PlayerInput msg)
        {
            int localId = 0;
            if (!id2LocalId.TryGetValue(userId, out localId)) return;
            Msg_PlayerInput[] inputs;
            //tick是第几帧,如果没有的话就创建 
            if (!tick2Inputs.TryGetValue(msg.Tick, out inputs))
            {
                inputs = new Msg_PlayerInput[maxPlayerCount];
                tick2Inputs.Add(msg.Tick, inputs);
            }
            //设置当前的input到这个输入数组中
            inputs[localId] = msg;
            CheckInput();
        }

        public void Join(Session session,Msg_PlayerServerInfo player)
        {
            if (id2LocalId.ContainsKey(player.Id)) return;
            id2LocalId[player.Id] = curLocalId;
            playerInfos[curLocalId] = player;
            playerSessions[curLocalId] = session;
            curLocalId++;
        }

        /// <summary>
        /// 哈希校验
        /// </summary>
        /// <param name="useId"></param>
        /// <param name="msg"></param>
        public void OnPlayerHashCode(int useId, Msg_HashCode msg)
        {
            int localId = 0;
            if (!id2LocalId.TryGetValue(useId, out localId)) return;
            int[] hashes;
            if (!tick2Hashes.TryGetValue(msg.Tick, out hashes))
            {
                hashes = new int[maxPlayerCount];
                tick2Hashes.Add(msg.Tick, hashes);
            }

            hashes[localId] = msg.Hash;
            //check hash
            foreach (var hash in hashes)
            {
                if (hash == 0)
                    return;
            }

            bool isSame = true;
            var val = hashes[0];
            foreach (var hash in hashes)
            {
                if (hash != val)
                {
                    isSame = false;
                    break;
                }
            }

            if (!isSame)
            {
                Logger.Instance.Debug(msg.Tick + " Hash is different " + val);
            }

            tick2Hashes.Remove(msg.Tick);
        }
    }
}
