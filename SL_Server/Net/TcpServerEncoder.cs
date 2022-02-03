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
    public class TcpServerEncoder : MessageToByteEncoder<TcpMessage>
    {
        protected override void Encode(IChannelHandlerContext context, TcpMessage message, IByteBuffer output)
        {
            byte[] body = message.message.ToByteArray();
            //它会自动进行大小端的转换
            output.WriteInt(body.Length);
            output.WriteInt(message.protoID);
            output.WriteBytes(body);

            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 发送协议 {message.type} 数据！");
        }
    }
}
