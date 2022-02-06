using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    /// <summary>
    /// 实现TcpServer的编码器
    /// 发包给客户端的时候调用
    /// </summary>
    public class TcpServerEncoder : MessageToByteEncoder<NetPackage>
    {
        protected override void Encode(IChannelHandlerContext context, NetPackage netPackage, IByteBuffer output)
        {
            output.WriteInt(netPackage.bodyData.Length);
            output.WriteInt(netPackage.protoID);
            output.WriteBytes(netPackage.bodyData);
        }
    }
}
