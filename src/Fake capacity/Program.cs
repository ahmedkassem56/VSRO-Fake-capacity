using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace Fake_capacity
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          [Out] byte[] lpBuffer,
          int dwSize,
          out int lpNumberOfBytesRead
         );
        [DllImport("kernel32.dll")]
        static extern uint VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType = 0x1000, uint flProtect = 0x4);

        static IntPtr Handle = IntPtr.Zero;

        static void Main(string[] args)
        {
            // Opening process
            Process[] p = Process.GetProcessesByName("GatewayServer");
            if (p.Count() == 0)
            {
                Console.WriteLine("Gateway server is not running");
                return;
            }
            Handle = OpenProcess(0x001F0FFF, true, p[0].Id);
            if (Handle == IntPtr.Zero)
            {
                Console.WriteLine("Couldn't open process.");
                return;
            }
            Console.WriteLine("Enter fake capacity number:");
            string str = Console.ReadLine();
            // Checking the value
            short capacity;
            if (!short.TryParse(str, out capacity))
            {
                Console.WriteLine("Max value is 65535 because its short.");
            }

            int count;
            uint CallAddr = 0x004064E5;
            byte[] _capacity = BitConverter.GetBytes(capacity);
            byte[] Codecave_Function = {
                                           0x66,0x81,0xC1,_capacity[0],_capacity[1],                     // ADD CX,xxxx
                                           0x83,0xC4,0x04,                                               // ADD ESP,4
                                           0x83,0xEF,0x01,                                               // SUB EDI,1
                                           0x68,0xEB,0x64,0x40,0x00,                                     // PUSH 004064EB (ReturnAddress)
                                           0xC3                                                          // RETN
                                       };
            uint Codecave = VirtualAllocEx(Handle, IntPtr.Zero, Codecave_Function.Length);
            byte[] CodeCaveAddr = BitConverter.GetBytes(Codecave - CallAddr - 5);
            byte[] CallFunc = { 0xE9, CodeCaveAddr[0], CodeCaveAddr[1], CodeCaveAddr[2], CodeCaveAddr[3], 0x90 };
            WriteProcessMemory(Handle, (IntPtr)Codecave, Codecave_Function, Codecave_Function.Length, out count);
            if (count == 0)
            {
                Console.WriteLine("Failed to write to process memory");
                return;
            }
            WriteProcessMemory(Handle, (IntPtr)CallAddr, CallFunc, CallFunc.Length, out count);
            if (count == 0)
            {
                Console.WriteLine("Failed to write to process memory");
                return;
            }
            Console.WriteLine("Successfuly faked capacity.\nPress any key to exit ...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
