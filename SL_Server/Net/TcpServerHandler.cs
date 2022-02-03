using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    /// <summary>
    /// 实现TcpServer的处理器
    /// </summary>
    class TcpServerHandler : SimpleChannelInboundHandler<TcpMessage>
    {
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接成功！");
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接断开！");
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接异常 {exception}！");
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, TcpMessage msg)
        {
            //从Decoder过来 
            Console.WriteLine($"{ctx.Channel.RemoteAddress.ToString()} 收到协议 {msg.type} 数据！");
            if (msg.type == typeof(LaunchPB.Hero))
            {
                LaunchPB.Hero hero = msg.message as LaunchPB.Hero;

                hero.Name = "Test";

                hero.Age = 28;

                // 返回给客户端

                TcpMessage respMessage = new TcpMessage()
                {
                    protoID = msg.protoID,
                    message = hero,
                    type = typeof(LaunchPB.Hero)
                };
                //后进入Encoder
                ctx.WriteAndFlushAsync(respMessage);
            }
        }
    }
}
