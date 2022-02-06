using System;
using System.Collections.Generic;
using System.Text;

namespace SL_Server.Net
{
    [Flags]
    public enum PacketFlags
    {
        None = 0,
        Reliable = 1<<0,
        Unsequenced = 1<<1,
        NoAllocate = 1<<2
    }

    public enum ChannelType
    {
        Connect,
        Accept,
    }

    public class NetBase : IDisposable
    {
        public long Id;
        public bool IsDisposed;

        public virtual void Dispose() {}
    }

    public class IdGenerator {
        private static long id = 0;
        private static object locker = new object();
        public static long GenerateId()
        {
            lock (locker)
            {
                return id++;
            }
        }
    }
}
