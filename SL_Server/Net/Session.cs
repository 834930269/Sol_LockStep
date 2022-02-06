using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SL_Server.Net
{
    public class Session : NetBase
    {
        private long RpcId { get; set; }
        private IChannelHandlerContext context;

        //是否已经登陆过了
        private bool isLogin;

        public Session(IChannelHandlerContext context)
        {
            this.RpcId = IdGenerator.GenerateId();
            this.context = context;
            isLogin = false;
        }
        /// <summary>
        /// 处理来自游戏客户端的数据包
        /// </summary>
        public async Task DispatchReceivePacket(NetPackage netPackage)
        {
            try
            {
                if(netPackage.protoID == (int)OpCode.ProtoCode.SJoinRoom)
                {
                    //加入房间的消息
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.ToString());
            }
        }

        public void Send(int opCode,byte[] body)
        {
            NetPackage netPackage = new NetPackage() { 
                protoID = opCode,
                bodyData = body
            };
            //传给encoder,发送
            context.WriteAndFlushAsync(netPackage);
        }

        private async Task NotifyOnLine(NetPackage resultPackage)
        {

        }
        public void Disconnect()
        {
        }
    }
}
