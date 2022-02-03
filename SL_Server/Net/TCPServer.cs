using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SL_Server.Net
{
    public static class TCPServer
    {
        private static IEventLoopGroup bossGroup;
        private static IEventLoopGroup workerGroup;
        private static IChannel bootstrapChannel;
        /// <summary>
        /// 启动TCPServer
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Task Start(int port)
        {
            return RunServerAsync(port);
        }

        private static async Task RunServerAsync(int port)
        {
            bossGroup = new MultithreadEventLoopGroup(1);
            //默认是CPU核数*2
            workerGroup = new MultithreadEventLoopGroup();

            try
            {
                ServerBootstrap bootstrap = new ServerBootstrap();
                bootstrap.Group(bossGroup, workerGroup);
                //设置为TCP协议
                bootstrap.Channel<TcpServerSocketChannel>();
                bootstrap
                    //表示处理线程满的时候,用于临时存放已完成三次握手的请求的队列的最大长度
                    .Option(ChannelOption.SoBacklog, 65535)
                    .Option(ChannelOption.RcvbufAllocator, new AdaptiveRecvByteBufAllocator())
                    .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .ChildOption(ChannelOption.SoKeepalive, true)
                    .ChildOption(ChannelOption.TcpNodelay, true)
                    .ChildHandler(new ActionChannelInitializer<IChannel>((channel) => {
                        IChannelPipeline pipeline = channel.Pipeline;
                        //配置数据流管线
                        pipeline.AddLast("IdleChecker", new IdleStateHandler(50, 50, 0));
                        //在这里添加了Encoder和Decoder还有消息的Handler
                        //先Decoder进入到Handler,然后Handler出数据时经过Encoder
                        pipeline.AddLast(new TcpServerEncoder(), new TcpServerDecoder(), new TcpServerHandler());
                    }));
                bootstrapChannel = await bootstrap.BindAsync(port);
                Console.WriteLine($"启动网关服务器成功！监听端口号：{port}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);

                throw new Exception("启动 TcpServer 失败! \n" + e.StackTrace);
            }

        }

        /// <summary>
        /// 关闭TcpServer
        /// </summary>
        /// <returns></returns>
        public static async Task Stop()
        {
            await Task.WhenAll(
                        bootstrapChannel.CloseAsync(),
                        bossGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)),
                        workerGroup.ShutdownGracefullyAsync(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2))
                    );

            Console.WriteLine("关闭网关服务器成功！");
        }
    }
}
