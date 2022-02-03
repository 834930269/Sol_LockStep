using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    /// <summary>
    /// 网关服务器和客户端通信时的消息格式
    /// </summary>
    public class TcpMessage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int protoID;

        /// <summary>
        /// 消息对象
        /// </summary>
        public IMessage message;
        /// <summary>
        /// 消息对象IMessage的具体类型
        /// </summary>
        public Type type;
    }
}
