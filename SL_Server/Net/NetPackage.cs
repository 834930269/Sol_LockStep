using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    /// <summary>
    /// 定义GateServer和CardServer之间传递的网络包格式
    /// </summary>
    public class NetPackage
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public int protoID;

        /// <summary>
        /// 消息体
        /// </summary>
        public byte[] bodyData;
    }
}
