using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    /// <summary>
    /// 实现TcpServer的处理器
    /// </summary>
    class TcpServerHandler : SimpleChannelInboundHandler<NetPackage>
    {
        private Session session;
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            session = new Session(context);
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接成功！");
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            session.Disconnect();
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接断开！");
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
            Console.WriteLine($"{context.Channel.RemoteAddress.ToString()} 链接异常 {exception}！");
        }

        protected async override void ChannelRead0(IChannelHandlerContext ctx, NetPackage netPackage)
        {
            await session.DispatchReceivePacket(netPackage);
        }
    }
}
