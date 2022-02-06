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
    /// 实现TcpServer的解码器
    /// 收到的字节转换成对象
    /// </summary>
    class TcpServerDecoder : ByteToMessageDecoder
    {
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                //网络包的包头约定是8个字节
                if(input.ReadableBytes < 8)
                {
                    return;
                }
                //获取包体的长度
                int bodyLength = input.GetInt(input.ReaderIndex);
                if(bodyLength <0 || bodyLength > (1024 * 8))
                {
                    //包体长度不合法
                    context.CloseAsync();
                    return;
                }

                //检查现在长度是不是够一个完整的网络包
                if (input.ReadableBytes < (8 + bodyLength))
                {
                    //还不够一个完整的网络包长度 等待下次重新接受
                    return;
                }

                //读取包头中记录包体长度的部分
                input.ReadInt();
                //读取包头中记录协议号的部分
                int protoID = input.ReadInt();
                //读取包体部分
                byte[] bodyData = new byte[bodyLength];
                input.ReadBytes(bodyData);
                // 包装成一个NetPackage对象
                NetPackage netPackage = new NetPackage()
                {
                    protoID = protoID,
                    bodyData = bodyData
                };
                output.Add(netPackage);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("解析数据异常," + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
