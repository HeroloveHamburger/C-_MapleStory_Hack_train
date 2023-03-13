using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace WpfApp1
{

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        const uint MEM_COMMIT = 0x00001000;
        const uint MEM_RESERVE = 0x00002000;
        const uint PAGE_READWRITE = 4;
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memset(IntPtr dest, int c, UIntPtr count);
        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
        uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static unsafe extern bool VirtualFreeEx(IntPtr hProcess, byte* pAddress, int size, AllocationType freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr handle, IntPtr addy, byte[] buffer, int size, ref int bytesRead);

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }
        [Flags]
        private enum ProcessAccessFlags : uint
        {
            QueryLimitedInformation = 0x00001000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(
              [In] IntPtr hProcess,
              [In] int dwFlags,
              [Out] StringBuilder lpExeName,
              ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
         ProcessAccessFlags processAccess,
         bool bInheritHandle,
         int processId);

        String GetProcessFilename(Process p)
        {
            int capacity = 2000;
            StringBuilder builder = new StringBuilder(capacity);
            IntPtr ptr = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, p.Id);
            if (!QueryFullProcessImageName(ptr, 0, builder, ref capacity))
            {
                return String.Empty;
            }

            return builder.ToString();
        }
        //Dictionary<int, string> procStates = new Dictionary<int, string>();
        private static byte[] Combine(byte[][] arrays)
        {
            byte[] bytes = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;

            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, bytes, offset, array.Length);
                offset += array.Length;
            }

            return bytes;
        }
        public void MemoryWrite(IntPtr address, byte[] writeData)
        {
            WriteProcessMemory(proc.Handle, (IntPtr)address, writeData, (uint)writeData.Length, out newAddress);
        }
        /// <summary>
        /// 開新的記憶體位置
        /// </summary>
        /// <param name="dwSize"></param>
        /// <returns></returns>
        public IntPtr AllocCreate(uint dwSize)
        {
            return VirtualAllocEx(proc.Handle, (IntPtr)0x13FFFF000, 256, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        }
        /// <summary>
        /// 開新的記憶體位置並指掉到選定地址
        /// </summary>
        /// <param name="address"></param>
        /// <param name="dwSize"></param>
        /// <returns></returns>
        public IntPtr AllocCreate(IntPtr address)
        {
            return VirtualAllocEx(proc.Handle, address, 256, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        }
        public void AllocFree(IntPtr address)
        {
            VirtualFreeEx(proc.Handle, address, 0, AllocationType.Release);
        }
        public static Process GetProc(int name)
        {
            return Process.GetProcessById(name);
        }


        public List<IntPtr> sigscan(string sig, byte[] buffer)
        {
            var intlist = transformarray(sig);
            var results = new List<IntPtr>();

            for (Int64 a = 0; a < buffer.Length; a++)
            {
                for (Int64 b = 0; b < intlist.Length; b++)
                {
                    if (intlist[b] != -1 && intlist[b] != buffer[a + b])
                        break;
                    if (b + 1 == intlist.Length)
                    {
                        var result = new IntPtr(a + (Int64)proc.MainModule.BaseAddress);
                        results.Add(result);
                    }
                }
            }

            return results;
        }


        public int[] transformarray(string sig)
        {
            var bytes = sig.Split(' ');
            int[] intlist = new int[bytes.Length];

            for (int i = 0; i < intlist.Length; i++)
            {
                if (bytes[i] == "??")
                    intlist[i] = -1;
                else
                    intlist[i] = int.Parse(bytes[i], NumberStyles.HexNumber);
            }
            return intlist;
        }
        public static byte[] GetStringToBytes(string value)
        {
            SoapHexBinary shb = SoapHexBinary.Parse(value);
            return shb.Value;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cheat_Ptr.ITEM_XY_static_address = (IntPtr)0x14203EF90;
        }
        Process proc;

        UIntPtr newAddress = UIntPtr.Zero;
        byte[] buffer;

        /// <summary>
        /// 獲取進程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openprocess_Click(object sender, RoutedEventArgs e)
        {
            Process[] procs = Process.GetProcessesByName("MapleStory");
            uint tests = (uint)((8) * Marshal.SizeOf(typeof(char)));
            CheckProcess.Text = Marshal.SizeOf(tests).ToString();
            if (procs.Length == 0)
                return;

            int bytesread = 0;
            proc = procs[0];
            buffer = new byte[proc.MainModule.ModuleMemorySize];
            ReadProcessMemory(proc.Handle, proc.MainModule.BaseAddress, buffer, buffer.Length, ref bytesread); // might require openprocess, idk 

            
            if (cheat_Ptr.NOW_Alloc_address == IntPtr.Zero)
                cheat_Ptr.NOW_Alloc_address = AllocCreate(4);
            //Add_NOW_Alloc_addres();
            cheat_Ptr.JMPPointerBaseAddress = cheat_Ptr.NOW_Alloc_address;
            //0x13FFF0000
            cheat_Ptr.GET_RUNE_TIMER = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13fff004
            cheat_Ptr.SKILL_Counter = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF0008
            cheat_Ptr.MONSTER_X = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 8;
            //0x13FFF0010
            cheat_Ptr.MONSTER_Y = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 8;
            //0x13FFF0018
            cheat_Ptr.ITEM_XY = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 8;
            //0x13FFF0020
            cheat_Ptr.RUNE_ARROW01_DATA = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF0024
            cheat_Ptr.RUNE_ARROW02_DATA = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF0028
            cheat_Ptr.RUNE_ARROW03_DATA = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF002C
            cheat_Ptr.RUNE_ARROW04_DATA = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF0030
            cheat_Ptr.RUNE_READY_DATA = cheat_Ptr.JMPPointerBaseAddress;
            cheat_Ptr.JMPPointerBaseAddress += 4;
            //0x13FFF0034
            cheat_Ptr.AUTO_Entry_chek = cheat_Ptr.JMPPointerBaseAddress;

            if (cheat_Ptr.MONSTER_XY_static_address == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.MONSTER_XY, buffer);
                cheat_Ptr.MONSTER_XY_static_address = addy[0];
            }

            if (cheat_Ptr.FLASH_static_address == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.FLASH, buffer);
                cheat_Ptr.FLASH_static_address = addy[0];
            }

            if (cheat_Ptr.FLASH_ENABLE_static_address == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.FLASH_ENABLE, buffer);
                cheat_Ptr.FLASH_ENABLE_static_address = addy[0];
            }
            if (cheat_Ptr.RUNE_TIMER_Static_Address == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.RUNE_TIMER, buffer);
                cheat_Ptr.RUNE_TIMER_Static_Address = addy[0];
            }
            if (cheat_Ptr.RUNE_CRACK_Static_Address == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.RUNE_CRACK, buffer);
                cheat_Ptr.RUNE_CRACK_Static_Address = addy[0];
            }
            CRCPASS_Click(sender, e);


            CheckProcess.Text = "抓到進程";

        }
        public (IntPtr, bool) CALL_CRACK_FUNCTION(IntPtr cheat_Ptr, string cheat_Signature_Code, bool cheat_State, byte[] cheat_Code, byte[] cheat_Code_reset)
        {

            if (cheat_Ptr == IntPtr.Zero)
            {
                var addy = sigscan(cheat_Signature_Code, buffer);
                cheat_Ptr = addy[0];
            }
            if (cheat_State == false)
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr, cheat_Code, (uint)cheat_Code.Length, out newAddress);
                cheat_State = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr, cheat_Code_reset, (uint)cheat_Code_reset.Length, out newAddress);
                cheat_State = false;
            }
            return (cheat_Ptr, cheat_State);
        }
        private void CRCPASS_Click(object sender, RoutedEventArgs e)
        {
            if (proc == null)
                return;
            (cheat_Ptr.CRC_pass, cheat_state.CRC_bool) = CALL_CRACK_FUNCTION(cheat_Ptr.CRC_pass, cheat_signature_code.CRC_PASS, cheat_state.CRC_bool, cheat_code.CRC_pass, cheat_code.CRC_pass_reset);

        }

        private void NO_DMG_Click(object sender, RoutedEventArgs e)
        {
            if (proc == null)
                return;
            //一般傷害無效
            (cheat_Ptr.NO_DMG, cheat_state.NO_DMG_bool) = CALL_CRACK_FUNCTION(cheat_Ptr.NO_DMG, cheat_signature_code.NO_DMG, cheat_state.NO_DMG_bool, cheat_code.NO_DMG, cheat_code.NO_DMG_reset);
            //飛行怪物免疫
            (cheat_Ptr.NO_FLY_DMG, cheat_state.NO_FLY_DMG_bool) = CALL_CRACK_FUNCTION(cheat_Ptr.NO_FLY_DMG, cheat_signature_code.NO_FLY_DMG, cheat_state.NO_FLY_DMG_bool, cheat_code.NO_FLY_DMG, cheat_code.NO_FLY_DMG_reset);

        }
        private void FAST_DROP_Click(object sender, RoutedEventArgs e)
        {
            if (proc == null)
                return;
            //快速掉落
            (cheat_Ptr.FAST_DROP, cheat_state.FAST_DROP_bool) = CALL_CRACK_FUNCTION(cheat_Ptr.FAST_DROP, cheat_signature_code.FAST_DROP, cheat_state.FAST_DROP_bool, cheat_code.FAST_DROP, cheat_code.FAST_DROP_reset);
        }
        private void MOB_FOLLOW_Click(object sender, RoutedEventArgs e)
        {
            if (proc == null)
                return;
            //快速掉落
            (cheat_Ptr.MOB_FOLLOW, cheat_state.MOB_FOLLOW_bool) = CALL_CRACK_FUNCTION(cheat_Ptr.MOB_FOLLOW, cheat_signature_code.MOB_FOLLOW, cheat_state.MOB_FOLLOW_bool, cheat_code.MOB_FOLLOW, cheat_code.MOB_FOLLOW_reset);
        }
        private void ALL_Click(object sender, RoutedEventArgs e)
        {
            NO_DMG_Click(sender, e);
            FAST_DROP_Click(sender, e);
            MOB_FOLLOW_Click(sender, e);
            SKILL_NO_Delay_Click(sender, e);
            /*
            */
        }
        /// <summary>
        /// 攻擊無延遲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SKILL_NO_Delay_Click(object sender, RoutedEventArgs e)
        {
            if (proc == null)
                return;
            if (cheat_Ptr.SKILL_NO_Delay_StaticAddress == IntPtr.Zero)
            {
                var addy = sigscan(cheat_signature_code.SKILL_NO_Delay, buffer);
                cheat_Ptr.SKILL_NO_Delay_StaticAddress = addy[0];
            }
            if (cheat_Ptr.SKILL_NO_Delay_StaticAddress == IntPtr.Zero)
                return;
            uint tests = (uint)((8) * Marshal.SizeOf(typeof(char)));
            if (cheat_state.SKILL_NO_Delay_bool == false)
            {
                if (cheat_Ptr.SKILL_NO_Delay_OverrideAddress == IntPtr.Zero)
                {
                    //0x13FFF104B
                    cheat_Ptr.SKILL_NO_Delay_OverrideAddress = (IntPtr)0x13FFF104B;
                }

                //寫入攻擊次數
                //byte[] counter = { 0x2};
                //MemoryWrite(cheat_Ptr.SKILL_Counter, counter);
                string smethod = "83 3D B2 EF FF FF 02 0F 84 0B 00 00 00 FF 05 A6 EF FF FF E9 11 00 00 00 C7 05 97 EF FF FF 00 00 00 00 44 89 AE 84 08 00 00 E9 30 05 D6 03";

                byte[] method = GetStringToBytes(smethod);

                MemoryWrite(cheat_Ptr.SKILL_NO_Delay_OverrideAddress, method);


                //將跳躍位置寫進hook位
                byte[] buffers = { 0xE9, 0xA4, 0xFA, 0x29, 0xFC, 0x66, 0x90 };
                MemoryWrite(cheat_Ptr.SKILL_NO_Delay_StaticAddress, buffers);


                cheat_state.SKILL_NO_Delay_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.SKILL_NO_Delay_StaticAddress, cheat_code.SKILL_NO_Delay_reset, (uint)cheat_code.SKILL_NO_Delay_reset.Length, out newAddress);
                cheat_state.SKILL_NO_Delay_bool = false;
            }


        }



        private void RUNE_TIMER_Check(object sender, RoutedEventArgs e)
        {

            if (cheat_Ptr.RUNE_TIMER_Static_Address == IntPtr.Zero)
                return;
            if (cheat_state.RUNE_TIMER_bool == false)
            {
                if (cheat_Ptr.RUNE_TIMER_OverrideAddress == IntPtr.Zero)
                {
                    //0x13FFF1079
                    cheat_Ptr.RUNE_TIMER_OverrideAddress = (IntPtr)0x13FFF1079;
                }

                string sfrist = "8B 97 04 01 00 00 81 BF A8 00 00 00 EA BC C4 04 0F 85 07 00 00 00 48 89 3D 6A EF FF FF E9 5F 09 E7 02";
                byte[] frist = GetStringToBytes(sfrist);
                MemoryWrite(cheat_Ptr.RUNE_TIMER_OverrideAddress, frist);

                //將跳躍位置寫進hook位
                byte[] buffers = { 0xE9, 0xF9, 0x06, 0x19, 0xFD, 0x90 };
                //byte[] buffers2 = { 0x66, 0x90 };
                MemoryWrite(cheat_Ptr.RUNE_TIMER_Static_Address, buffers);


                cheat_state.RUNE_TIMER_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.RUNE_TIMER_Static_Address, cheat_code.RUNE_TIMER_reset, (uint)cheat_code.RUNE_TIMER_reset.Length, out newAddress);
                cheat_state.RUNE_TIMER_bool = false;
            }

        }

        private void RUNE_CRACK(object sender,RoutedEventArgs e)
        {

            if (cheat_Ptr.RUNE_CRACK_Static_Address == IntPtr.Zero)
                return;
            if (cheat_state.RUNE_CRACK_bool == false)
            {
                if (cheat_Ptr.RUNE_CRACK_OverrideAddress == IntPtr.Zero)
                {
                    //0x13FFF1079
                    cheat_Ptr.RUNE_CRACK_OverrideAddress = (IntPtr)0x13FFF0500;
                }

                string sfrist = "50 8B 07 A3 20 00 FF 3F 01 00 00 00 8B 47 04 A3 24 00 FF 3F 01 00 00 00 8B 47 08 A3 28 00 FF 3F 01 00 00 00 8B 47 0C A3 2C 00 FF 3F 01 00 00 00 C7 05 F6 FA FF FF 01 00 00 00 58 9C 51 48 83 EC 08 E9 98 79 20 0A";
                byte[] frist = GetStringToBytes(sfrist);
                MemoryWrite(cheat_Ptr.RUNE_CRACK_OverrideAddress, frist);

                //將跳躍位置寫進hook位
                string sbuffers = "E9 26 86 DF F5 0F 1F 40 00";
                byte[] buffers = GetStringToBytes(sbuffers);
                //byte[] buffers2 = { 0x66, 0x90 };
                MemoryWrite(cheat_Ptr.RUNE_CRACK_Static_Address, buffers);


                cheat_state.RUNE_CRACK_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.RUNE_CRACK_Static_Address, cheat_code.RUNE_CRACK_rest, (uint)cheat_code.RUNE_CRACK_rest.Length, out newAddress);
                cheat_state.RUNE_CRACK_bool = false;
            }
            


        }


        private void ITEM_XY(object sender,RoutedEventArgs e)
        {
            if (cheat_Ptr.ITEM_XY_static_address == IntPtr.Zero)
                return;

            if (cheat_state.ITEM_XY_bool == false)
            {
                if (cheat_Ptr.ITEM_XY_OverrideAddress == IntPtr.Zero)
                {
                    cheat_Ptr.ITEM_XY_OverrideAddress = (IntPtr)0x13FFF109B;
                }
                //0x13FFF0018
                string sfrist = "48 89 4A 14 48 89 15 72 EF FF FF 48 89 16 E9 E9 DE 04 02";

                byte[] ITEM_XY_OverrideAddress_func = GetStringToBytes(sfrist);
                MemoryWrite(cheat_Ptr.ITEM_XY_OverrideAddress, ITEM_XY_OverrideAddress_func);
                string sitemXYTemp = "B1 05 06 44 01 00 00 00";
                byte[] itemXYTemp = GetStringToBytes(sitemXYTemp);
                MemoryWrite(cheat_Ptr.ITEM_XY, itemXYTemp);
                //0x13FFF109B
                //將跳躍位置寫進hook位
                string sITEM_XY_static_address_Hook = "E9 06 21 FB FD 66 90";
                byte[] ITEM_XY_static_address_Hook = GetStringToBytes(sITEM_XY_static_address_Hook);
                MemoryWrite(cheat_Ptr.ITEM_XY_static_address, ITEM_XY_static_address_Hook);
                cheat_state.ITEM_XY_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.ITEM_XY_static_address, cheat_code.ITEM_XY_rest, (uint)cheat_code.ITEM_XY_rest.Length, out newAddress);
                cheat_state.ITEM_XY_bool = false;
            }
        }
        private void MONSTER_XY(object sender, RoutedEventArgs e)
        {
            if (cheat_Ptr.MONSTER_XY_static_address == IntPtr.Zero)
                return;

            if (cheat_state.MONSTER_XY_bool == false)
            {
                if (cheat_Ptr.MONSTER_XY_OverrideAddress == IntPtr.Zero)
                {
                    cheat_Ptr.MONSTER_XY_OverrideAddress = (IntPtr)0x13FFF1000;
                }
                
                byte[] MONSTER_XY_OverrideAddress_func = { 0x41, 0x89, 0x87, 0xC8, 0x0F, 0x00, 0x00, 0x50,
                    0x53 , 0x41 , 0x8B , 0x9F , 0xC8 , 0x0F , 0x00 , 0x00 ,  0x89 ,
                    0x1D , 0xFA , 0xEF , 0xFF , 0xFF , 0x41 , 0x8B , 0x9F , 0xC4 , 0x0F ,
                    0x00 , 0x00 , 0x89 , 0x1D , 0xE5 , 0xEF , 0xFF , 0xFF , 0x5B , 0x58 ,
                    0xE9 , 0x14 , 0x3B , 0x39 , 0x04 };
                MemoryWrite(cheat_Ptr.MONSTER_XY_OverrideAddress, MONSTER_XY_OverrideAddress_func);

                //將跳躍位置寫進hook位
                byte[] MONSTER_XY_static_address_Hook = { 0xE9, 0xC4, 0xC4, 0xC6, 0xFB, 0x66, 0x90 };
                MemoryWrite(cheat_Ptr.MONSTER_XY_static_address, MONSTER_XY_static_address_Hook);
                cheat_state.MONSTER_XY_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.MONSTER_XY_static_address, cheat_code.MONSTER_XY_reset, (uint)cheat_code.MONSTER_XY_reset.Length, out newAddress);
                cheat_state.MONSTER_XY_bool = false;
            }
        }
        /// <summary>
        /// 舜移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FLASH(object sender, RoutedEventArgs e)
        {
            if (cheat_Ptr.FLASH_static_address == IntPtr.Zero)
                return;
            if (cheat_state.FLASH_bool == false)
            {
                if (cheat_Ptr.FLASH_OverrideAddress == IntPtr.Zero)
                {
                    
                    cheat_Ptr.FLASH_OverrideAddress = (IntPtr)0x13FFF2000;
                }
                string sFrist = "50 53 48 B8 A8 56 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 8B 40 78 48 8B 40 70 48 2D 84 03 00 00 48 8B 58 18 48 83 FB 04 0F 84 C6 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 B4 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 A2 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 90 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 7E 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 6C 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 5A 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 48 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 36 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 24 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 12 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 85 33 00 00 00 48 8B 1D 04 DF FF FF 81 BB A8 00 00 00 EA BC C4 04 0F 85 0D 00 00 00 83 BB 04 01 00 00 00 0F 85 0F 00 00 00 44 8B 40 50 44 8B 48 54 5B 58 E9 5A F3 07 04 5B";
                byte[] frist = GetStringToBytes(sFrist);
                string sSecond = "48 8B 05 E8 DE FF FF 81 38 F8 39 48 45 58 0F 84 06 00 00 00 0F 85 16 00 00 00 50 48 8B 05 CD DE FF FF 44 8B 48 18 44 8B 40 14 58 E9 29 D3 06 04";
                byte[] Second = GetStringToBytes(sSecond);
                string sTeleport_End = "57 48 BF 10 00 FF 3F 01 00 00 00 44 8B 0F 48 BF 08 00 FF 3F 01 00 00 00 44 8B 07 5F E9 08 D3 06 04";
                byte[] Teleport_End = GetStringToBytes(sTeleport_End);
                byte[][] arrays = {frist, Second,Teleport_End };
                byte[] rv = Combine(arrays);
                MemoryWrite(cheat_Ptr.FLASH_OverrideAddress, rv);



                string sbuffers = "E9 7F 2B F9 FB 90";
                //將跳躍位置寫進hook位
                byte[] buffers = GetStringToBytes(sbuffers);
                MemoryWrite(cheat_Ptr.FLASH_static_address, buffers);

                cheat_state.FLASH_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.FLASH_static_address, cheat_code.FLASH_reset, (uint)cheat_code.FLASH_reset.Length, out newAddress);
                cheat_state.FLASH_bool = false;
            }
        }

        private void FLASH_ENABLE(object sender, RoutedEventArgs e)
        {
                if (cheat_Ptr.FLASH_ENABLE_static_address == IntPtr.Zero)
                return;
            uint tests = (uint)((8) * Marshal.SizeOf(typeof(char)));
            if (cheat_state.FLASH_ENABLE_bool == false)
            {
                if (cheat_Ptr.FLASH_ENABLE_OverrideAddress == IntPtr.Zero)
                {
                    MONSTER_XY(sender, e);

                    FLASH(sender, e);
                    ITEM_XY(sender, e);
                    cheat_Ptr.FLASH_ENABLE_OverrideAddress = (IntPtr)0x13FFF102A;

                }
                string sfrist = "41 FF 84 24 A0 0B 02 00 45 39 AC 24 A0 0B 02 00 E9 BD E1 06 04";
                byte[] frist = GetStringToBytes(sfrist);
                MemoryWrite(cheat_Ptr.FLASH_ENABLE_OverrideAddress, frist);


                string sbuffers = "E9 31 1E F9 FB 0F 1F 00";
                //將跳躍位置寫進hook位
                byte[] buffers = GetStringToBytes(sbuffers);

                MemoryWrite(cheat_Ptr.FLASH_ENABLE_static_address, buffers);

                cheat_state.FLASH_ENABLE_bool = true;
            }
            else
            {
                WriteProcessMemory(proc.Handle, cheat_Ptr.FLASH_ENABLE_static_address, cheat_code.FLASH_ENABLE_rest, (uint)cheat_code.FLASH_ENABLE_rest.Length, out newAddress);
                cheat_state.FLASH_ENABLE_bool = false;
            }
        }

        private void AOTU_ATTACK_Check(object sender, RoutedEventArgs e)
        {
            if (cheat_Ptr.AUTO_Entry_static_address == IntPtr.Zero)
                return;

            if (cheat_state.AUTO_Entry_bool == false)
            {
                if (cheat_Ptr.AUTO_Entry_OverrideAddress == IntPtr.Zero)
                {
                    RUNE_CRACK(sender, e);
                    RUNE_TIMER_Check(sender, e);
                    cheat_Ptr.AUTO_Entry_OverrideAddress = (IntPtr)0x13FFF3000;
                }
                if (cheat_Ptr.AUTO_Main_OverrideAddress == IntPtr.Zero)
                {

                    cheat_Ptr.AUTO_Main_OverrideAddress = (IntPtr)0x13FFF3100;
                }
                byte[] set = {0x5};
                MemoryWrite(cheat_Ptr.AUTO_Entry_chek, set);
                MemoryWrite(cheat_Ptr.RUNE_ARROW01_DATA, set);
                MemoryWrite(cheat_Ptr.RUNE_ARROW02_DATA, set);
                MemoryWrite(cheat_Ptr.RUNE_ARROW03_DATA, set);
                MemoryWrite(cheat_Ptr.RUNE_ARROW04_DATA, set);
                MemoryWrite(cheat_Ptr.RUNE_READY_DATA, set);

                string sEntry = "50 48 B8 73 56 BF 40 01 00 00 00 48 39 44 24 08 0F 85 0F 00 00 00 48 B8 00 31 FF 3F 01 00 00 00 48 89 44 24 08 58 FF 25 00 00 00 00 D0 39 7E 2F FD 7F 00 00";
                       sEntry = "50 48 B8 73 56 BF 40 01 00 00 00 48 39 44 24 08 0F 85 0F 00 00 00 48 B8 00 31 FF 3F 01 00 00 00 48 89 44 24 08 58 FF 25 00 00 00 00 D0 39 AA 07 F9 7F 00 00";
                byte[] Entry = GetStringToBytes(sEntry);
                MemoryWrite(cheat_Ptr.AUTO_Entry_OverrideAddress, Entry);

                //string sFrist = "83 3D 2D CF FF FF 00 0F 84 66 25 C0 00 50 53 48 B8 A8 56 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 8B 40 78 48 8B 40 70 48 2D 84 03 00 00 48 8B 58 18 48 83 FB 04 0F 84 C6 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 B4 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 A2 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 90 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 7E 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 6C 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 5A 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 48 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 36 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 24 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 12 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 85 00 03 00 00 48 8B 1D F7 CD FF FF 81 BB A8 00 00 00 EA BC C4 04 0F 85 0D 00 00 00 83 BB 04 01 00 00 00 0F 85 DC 02 00 00 83 3D 03 CE FF FF 01 0F 85 5E 01 00 00 83 3D E6 CD FF FF 00 C7 05 EC CD FF FF 01 00 00 00 0F 84 CE 01 00 00 83 3D D3 CD FF FF 01 C7 05 D5 CD FF FF 01 00 00 00 0F 84 69 01 00 00 83 3D C0 CD FF FF 02 C7 05 BE CD FF FF 01 00 00 00 0F 84 3C 02 00 00 0F 85 E8 01 00 00 83 3D 9B CD FF FF 00 C7 05 A1 CD FF FF 02 00 00 00 0F 84 83 01 00 00 83 3D 88 CD FF FF 01 C7 05 8A CD FF FF 02 00 00 00 0F 84 1E 01 00 00 83 3D 75 CD FF FF 02 C7 05 13 0A 00 00 02 00 00 00 0F 84 F1 01 00 00 0F 85 9D 01 00 00 83 3D 50 CD FF FF 00 C7 05 56 CD FF FF 03 00 00 00 0F 84 38 01 00 00 83 3D 3D CD FF FF 01 C7 05 3F CD FF FF 03 00 00 00 0F 84 D3 00 00 00 83 3D 2A CD FF FF 02 C7 05 28 CD FF FF 03 00 00 00 0F 84 A6 01 00 00 0F 85 52 01 00 00 83 3D 05 CD FF FF 00 C7 05 0B CD FF FF 04 00 00 00 0F 84 ED 00 00 00 83 3D F2 CC FF FF 01 C7 05 F4 CC FF FF 04 00 00 00 0F 84 88 00 00 00 83 3D DF CC FF FF 02 C7 05 DD CC FF FF 04 00 00 00 0F 84 5B 01 00 00 0F 85 07 01 00 00 C7 05 C7 CC FF FF 00 00 00 00 C7 05 AD CC FF FF 05 00 00 00 C7 05 A7 CC FF FF 05 00 00 00 C7 05 A1 CC FF FF 05 00 00 00 C7 05 9B CC FF FF 05 00 00 00 5B 48 8B 05 97 CC FF FF 48 83 F8 01 58 0F 84 F1 22 C0 00 49 B8 00 00 39 00 00 00 00 00 BA 20 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 CB 65 F9 03 E9 CB 22 C0 00 49 B8 00 00 48 01 00 00 00 00 BA 26 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 A5 65 F9 03 83 3D 3E CC FF FF 01 0F 84 86 FE FF FF 83 3D 31 CC FF FF 02 0F 84 C4 FE FF FF 83 3D 24 CC FF FF 03 0F 84 02 FF FF FF 0F 85 47 FF FF FF 49 B8 00 00 50 01 00 00 00 00 BA 28 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 57 65 F9 03 83 3D F0 CB FF FF 01 0F 84 38 FE FF FF 83 3D E3 CB FF FF 02 0F 84 76 FE FF FF 83 3D D6 CB FF FF 03 0F 84 B4 FE FF FF 0F 85 F9 FE FF FF 49 B8 00 00 4D 01 00 00 00 00 BA 27 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 01 95 F4 F7 83 3D A2 CB FF FF 01 0F 84 EA FD FF FF 83 3D 95 CB FF FF 02 0F 84 28 FE FF FF 83 3D 88 CB FF FF 03 0F 84 66 FE FF FF 0F 85 AB FE FF FF 49 B8 00 00 4B 01 00 00 00 00 BA 25 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 BB 64 F9 03 83 3D 54 CB FF FF 01 0F 84 9C FD FF FF 83 3D 47 CB FF FF 02 0F 84 DA FD FF FF 83 3D 3A CB FF FF 03 0F 84 18 FE FF FF 0F 85 5D FE FF FF 5B 45 30 F6 C7 44 24 60 FF FF FF FF 48 B8 B8 4B 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 83 F8 03 58 0F 8C 3B 00 00 00 E8 F1 85 18 02 48 BF 48 B4 72 46 01 00 00 00 48 8B 3F 48 8B BF CC 23 00 00 83 FF 29 0F 85 05 00 00 00 BF 00 00 00 00 FF C7 48 8B C8 45 31 C0 8B D7 E8 D0 C9 11 02 E9 30 21 C0 00 49 B8 30 00 1D 00 00 00 00 00 BA 11 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 0A 64 F9 03 49 B8 03 00 2A 00 00 00 00 00 BA 10 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 E9 63 F9 03 49 B8 00 00 2C 00 00 00 00 00 BA 5A 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 C8 63 F9 03 E9 C8 20 C0 00";
                string sFrist = "83 3D 2D CF FF FF 00 0F 84 66 25 C0 00 50 53 48 B8 A8 56 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 8B 40 78 48 8B 40 70 48 2D 84 03 00 00 48 8B 58 18 48 83 FB 04 0F 84 C6 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 B4 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 A2 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 90 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 7E 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 6C 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 5A 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 48 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 36 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 24 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 84 12 00 00 00 48 83 C0 64 48 8B 58 18 48 83 FB 04 0F 85 00 03 00 00 48 8B 1D F7 CD FF FF 81 BB A8 00 00 00 EA BC C4 04 0F 85 0D 00 00 00 83 BB 04 01 00 00 00 0F 85 DC 02 00 00 83 3D 03 CE FF FF 01 0F 85 5E 01 00 00 83 3D E6 CD FF FF 00 C7 05 EC CD FF FF 01 00 00 00 0F 84 CE 01 00 00 83 3D D3 CD FF FF 01 C7 05 D5 CD FF FF 01 00 00 00 0F 84 69 01 00 00 83 3D C0 CD FF FF 02 C7 05 BE CD FF FF 01 00 00 00 0F 84 3C 02 00 00 0F 85 E8 01 00 00 83 3D 9B CD FF FF 00 C7 05 A1 CD FF FF 02 00 00 00 0F 84 83 01 00 00 83 3D 88 CD FF FF 01 C7 05 8A CD FF FF 02 00 00 00 0F 84 1E 01 00 00 83 3D 75 CD FF FF 02 C7 05 13 0A 00 00 02 00 00 00 0F 84 F1 01 00 00 0F 85 9D 01 00 00 83 3D 50 CD FF FF 00 C7 05 56 CD FF FF 03 00 00 00 0F 84 38 01 00 00 83 3D 3D CD FF FF 01 C7 05 3F CD FF FF 03 00 00 00 0F 84 D3 00 00 00 83 3D 2A CD FF FF 02 C7 05 28 CD FF FF 03 00 00 00 0F 84 A6 01 00 00 0F 85 52 01 00 00 83 3D 05 CD FF FF 00 C7 05 0B CD FF FF 04 00 00 00 0F 84 ED 00 00 00 83 3D F2 CC FF FF 01 C7 05 F4 CC FF FF 04 00 00 00 0F 84 88 00 00 00 83 3D DF CC FF FF 02 C7 05 DD CC FF FF 04 00 00 00 0F 84 5B 01 00 00 0F 85 07 01 00 00 C7 05 C7 CC FF FF 00 00 00 00 C7 05 AD CC FF FF 05 00 00 00 C7 05 A7 CC FF FF 05 00 00 00 C7 05 A1 CC FF FF 05 00 00 00 C7 05 9B CC FF FF 05 00 00 00 5B 48 8B 05 97 CC FF FF 48 83 F8 01 58 0F 84 F1 22 C0 00 49 B8 00 00 39 00 00 00 00 00 BA 20 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 CB 65 F9 03 E9 CB 22 C0 00 49 B8 00 00 48 01 00 00 00 00 BA 26 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 A5 65 F9 03 83 3D 3E CC FF FF 01 0F 84 86 FE FF FF 83 3D 31 CC FF FF 02 0F 84 C4 FE FF FF 83 3D 24 CC FF FF 03 0F 84 02 FF FF FF 0F 85 47 FF FF FF 49 B8 00 00 50 01 00 00 00 00 BA 28 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 57 65 F9 03 83 3D F0 CB FF FF 01 0F 84 38 FE FF FF 83 3D E3 CB FF FF 02 0F 84 76 FE FF FF 83 3D D6 CB FF FF 03 0F 84 B4 FE FF FF 0F 85 F9 FE FF FF 49 B8 00 00 4D 01 00 00 00 00 BA 27 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 01 95 F4 F7 83 3D A2 CB FF FF 01 0F 84 EA FD FF FF 83 3D 95 CB FF FF 02 0F 84 28 FE FF FF 83 3D 88 CB FF FF 03 0F 84 66 FE FF FF 0F 85 AB FE FF FF 49 B8 00 00 4B 01 00 00 00 00 BA 25 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 BB 64 F9 03 83 3D 54 CB FF FF 01 0F 84 9C FD FF FF 83 3D 47 CB FF FF 02 0F 84 DA FD FF FF 83 3D 3A CB FF FF 03 0F 84 18 FE FF FF 0F 85 5D FE FF FF 5B 45 30 F6 C7 44 24 60 FF FF FF FF 48 B8 B8 4B 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 83 F8 03 58  49 B8 30 00 1D 00 00 00 00 00 BA 11 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 0A 64 F9 03 49 B8 03 00 2A 00 00 00 00 00 BA 10 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 E9 63 F9 03 49 B8 00 00 2C 00 00 00 00 00 BA 5A 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 C8 63 F9 03 E9 C8 20 C0 00";
                sFrist = "83 3D 2D CF FF FF 00 0F 84 66 25 C0 00 50 53 48 B8 A8 56 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 8B 40 78 48 8B 40 70 48 2D 84 03 00 00 48 8B 58 18 83 FB 04 0F 84 BB 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 AA 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 99 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 88 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 77 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 66 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 55 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 44 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 33 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 22 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 84 11 00 00 00 48 83 C0 64 48 8B 58 18 83 FB 04 0F 85 02 03 00 00 48 83 FB 00 48 BB 00 00 FF 3F 01 00 00 00 0F 84 12 00 00 00 8B 9B 04 01 00 00 81 FB FA 00 00 00 0F 8F DC 02 00 00 83 3D 0D CE FF FF 01 0F 85 5E 01 00 00 83 3D F0 CD FF FF 00 C7 05 F6 CD FF FF 01 00 00 00 0F 84 CE 01 00 00 83 3D D9 CD FF FF 01 C7 05 DF CD FF FF 01 00 00 00 0F 84 69 01 00 00 83 3D C2 CD FF FF 02 C7 05 C8 CD FF FF 01 00 00 00 0F 84 3C 02 00 00 0F 85 E8 01 00 00 83 3D A9 CD FF FF 00 C7 05 AB CD FF FF 02 00 00 00 0F 84 83 01 00 00 83 3D 92 CD FF FF 01 C7 05 94 CD FF FF 02 00 00 00 0F 84 1E 01 00 00 83 3D 7B CD FF FF 02 C7 05 7D CD FF FF 02 00 00 00 0F 84 F1 01 00 00 0F 85 9D 01 00 00 83 3D 62 CD FF FF 00 C7 05 60 CD FF FF 03 00 00 00 0F 84 38 01 00 00 83 3D 4B CD FF FF 01 C7 05 49 CD FF FF 03 00 00 00 0F 84 D3 00 00 00 83 3D 34 CD FF FF 02 C7 05 32 CD FF FF 03 00 00 00 0F 84 A6 01 00 00 0F 85 52 01 00 00 83 3D 1B CD FF FF 00 C7 05 15 CD FF FF 04 00 00 00 0F 84 ED 00 00 00 83 3D 04 CD FF FF 01 C7 05 FE CC FF FF 04 00 00 00 0F 84 88 00 00 00 83 3D ED CC FF FF 02 C7 05 E7 CC FF FF 04 00 00 00 0F 84 5B 01 00 00 0F 85 07 01 00 00 C7 05 D1 CC FF FF 00 00 00 00 C7 05 B7 CC FF FF 05 00 00 00 C7 05 B1 CC FF FF 05 00 00 00 C7 05 AB CC FF FF 05 00 00 00 C7 05 A5 CC FF FF 05 00 00 00 5B 48 8B 05 A1 CC FF FF 48 83 F8 01 58 0F 84 FB 22 C0 00 49 B8 00 00 39 00 00 00 00 00 BA 20 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 D5 65 F9 03 E9 D5 22 C0 00 49 B8 00 00 48 01 00 00 00 00 BA 26 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 A7 95 F4 F7 83 3D 48 CC FF FF 01 0F 84 86 FE FF FF 83 3D 3B CC FF FF 02 0F 84 C4 FE FF FF 83 3D 2E CC FF FF 03 0F 84 02 FF FF FF 0F 85 47 FF FF FF 49 B8 00 00 50 01 00 00 00 00 BA 28 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 61 65 F9 03 83 3D FA CB FF FF 01 0F 84 38 FE FF FF 83 3D ED CB FF FF 02 0F 84 76 FE FF FF 83 3D E0 CB FF FF 03 0F 84 B4 FE FF FF 0F 85 F9 FE FF FF 49 B8 00 00 4D 01 00 00 00 00 BA 27 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 13 65 F9 03 83 3D AC CB FF FF 01 0F 84 EA FD FF FF 83 3D 9F CB FF FF 02 0F 84 28 FE FF FF 83 3D 92 CB FF FF 03 0F 84 66 FE FF FF 0F 85 AB FE FF FF 49 B8 00 00 4B 01 00 00 00 00 BA 25 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 C5 64 F9 03 83 3D 5E CB FF FF 01 0F 84 9C FD FF FF 83 3D 51 CB FF FF 02 0F 84 DA FD FF FF 83 3D 44 CB FF FF 03 0F 84 18 FE FF FF 0F 85 5D FE FF FF 5B 45 30 F6 C7 44 24 60 FF FF FF FF 48 B8 B8 4B 22 48 01 00 00 00 48 8B 00 48 8B 40 08 48 83 F8 03 58 49 B8 30 00 1D 00 00 00 00 00 BA 11 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 55 64 F9 03 49 B8 03 00 2A 00 00 00 00 00 BA 10 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 34 64 F9 03 49 B8 00 00 2C 00 00 00 00 00 BA 5A 00 00 00 48 B9 68 BB 72 46 01 00 00 00 48 8B 09 E8 13 64 F9 03 E9 13 21 C0 00";
                byte[] Frist = GetStringToBytes(sFrist);
                MemoryWrite(cheat_Ptr.AUTO_Main_OverrideAddress, Frist);
                

                string sbuffers = "00 30 FF 3F 01 00 00 00";
                //將跳躍位置寫進hook位
                byte[] buffers = GetStringToBytes(sbuffers);
                MemoryWrite(cheat_Ptr.AUTO_Entry_static_address, buffers);

                cheat_state.AUTO_Entry_bool = true;
            }
            else
            {
                byte[] set = { 0x0 };
                MemoryWrite(cheat_Ptr.AUTO_Entry_chek, set);
                //WriteProcessMemory(proc.Handle, cheat_Ptr.AUTO_Entry_static_address, cheat_code.AUTO_Entry_rest, (uint)cheat_code.AUTO_Entry_rest.Length, out newAddress);
                cheat_state.AUTO_Entry_bool = false;
            }


        }
    }
}
