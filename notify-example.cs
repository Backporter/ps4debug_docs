using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using libdebug;

namespace example
{
    class Program
    {
        static string ICON =  "https://www.akcpetinsurance.com/res/akc/images/icons/home/home_dog.png";
        static int _ProcessID;
        static PS4DBG _PS4;

        [StructLayout(LayoutKind.Sequential, Size = 0xC30, Pack = 1)]
        public unsafe struct NotifyBuffer
        {
            public int Type;
            public int ReqId;
            public int Priority;
            public int MsgId;
            public int TargetId;
            public int UserId;
            public int unk1;
            public int unk2;
            public int AppId;
            public int ErrorNum;
            public int unk3;
            public bool UseIconImageUri;

            public fixed byte Message[1024];
            public fixed byte Uri[1024];
            public fixed byte unkstr[1024];

            public byte[] ToBytes(object _struct)
            {
                int size = Marshal.SizeOf(_struct);
                byte[] arr = new byte[size];
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(_struct, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);
                return arr;
            }

            public byte[] InitAndGetBytes(string msg, string url)
            {
                Type = 0;
                unk3 = 0;
                UseIconImageUri = true;
                TargetId = -1;

                if (msg.Length > 1024)
                {
                    throw new ArgumentException("Message to large to send");
                    return new byte[0];
                }

                byte[] msgbytes = Encoding.ASCII.GetBytes(msg);
                byte[] urlbytes = Encoding.ASCII.GetBytes(url);

                byte[] _buffer = ToBytes(this);

                Array.Copy(msgbytes, 0, _buffer, 45, msgbytes.Length);          // 45 is the offset of Message inside the NotifyBuffer structure
                Array.Copy(urlbytes, 0, _buffer, 1069, urlbytes.Length);        // 1069 is the offset of the Uri buffer inside the notify structure

                return _buffer;
            }

        };

        static void Main(string[] args)
        {
            Console.WriteLine("What's your PS4's IP?");
            string IP = Console.ReadLine();

            _PS4 = new PS4DBG(IP);
            _PS4.Connect();

            foreach (Process process in _PS4.GetProcessList().processes)
            {
                if (process.name == "eboot.bin")
                {
                    _ProcessID = process.pid;
                    break;
                }
            }

            var _NotifyBuffer = new NotifyBuffer().InitAndGetBytes("hello", ICON);

            ulong rpc = _PS4.InstallRPC(_ProcessID);
            var maps = _PS4.GetProcessMaps(_ProcessID);
            var ent = maps.FindEntry("libkernel.sprx");
            var prxbase = ent.start;

            ulong mem = _PS4.AllocateMemory(_ProcessID, 3120);
            _PS4.WriteMemory(_ProcessID, mem, _NotifyBuffer);

            // + 0x191C0 is specific to 5.05, you should have this be dynamiclly set based of FW version, I'm unsure if PS4debug has a functio that will return a function address based of its NID
            ulong ret = _PS4.Call(_ProcessID, rpc, (prxbase + 0x191C0), new object[] { 0, mem, 3120, 0 });
            _PS4.FreeMemory(_ProcessID, mem, 3120);
            if (ret != 0)
            {
                // somthing went wrong
            }
        }
    }
}
