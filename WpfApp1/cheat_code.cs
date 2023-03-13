using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class cheat_signature_code
    {
        public static string CRC_PASS = "48 89 54 24 10 48 89 4C 24 08 48 81 EC 88 04";
        public static string NO_DMG = "48 8B C4 55 53 56 57 41 54 41 55 41 56 41 57 48 8D A8 68 F8";
        public static string NO_FLY_DMG = "48 8B C4 55 53 56 57 41 54 41 55 41 56 41 57 48 8D A8 E8 FC";
        public static string FAST_DROP = "0F 85 EA 00 00 00 89 98";
        public static string MOB_FOLLOW = "74 56 49 8D 8E 10 0A 00 00";
        public static string SKILL_NO_Delay = "44 89 AE 84 08 00 00 49 8B 04";
        public static string RUNE_TIMER = "8B 97 04 01 00 00 2B";
        public static string MONSTER_XY = "41 89 87 C8 0F 00 00";
        public static string FLASH = "44 8B CB 44 8B C7 33";
        public static string FLASH_ENABLE = "45 39 AC 24 A0 0B 02 00";
        public static string RUNE_CRACK = "9C 51 48 81 EC 08 00 00 00 48 89 0C 24 48 81 34";

    }
    public class cheat_code
    {
        public static byte[] CRC_pass_reset = { 0x48,0x89 ,0x54 ,0x24, 0x10, 0x48, 0x89, 0x4C, 0x24, 0x08 };
        public static byte[] CRC_pass = { 0x31,0xC0,0xC3, 0x90, 0x90, 0x90, 0x90,0x90,0x90,0x90 };
        public static byte[] NO_DMG_reset = { 0x48, 0x8B, 0xC4 };
        public static byte[] NO_DMG = { 0xC3, 0x90, 0x90 };
        public static byte[] NO_FLY_DMG_reset = { 0x48, 0x8B, 0xC4 };
        public static byte[] NO_FLY_DMG = { 0xC3, 0x90, 0x90 };
        public static byte[] FAST_DROP_reset = { 0x0F ,0x85 ,0xEA ,0x00 ,0x00 ,0x00 };
        public static byte[] FAST_DROP = { 0x90, 0x90,0x90, 0x90, 0x90, 0x90 };
        public static byte[] MOB_FOLLOW_reset = { 0x74, 0x56, 0x49, 0x8D, 0x8E, 0x10, 0x0A, 0x00, 0x00 };
        public static byte[] MOB_FOLLOW = { 0x90, 0x90 };
        public static byte[] SKILL_NO_Delay_reset = { 0x44, 0x89, 0xAE, 0x84, 0x08, 0x00, 0x00, 0x49, 0x8B, 0x04, 0x24, 0x89, 0x6C, 0x24, 0x20 };
        public static byte[] RUNE_TIMER_reset = { 0x8B, 0x97, 0x04, 0x01, 0x00, 0x00, 0x2B, 0x55, 0x44, 0x48, 0x8B, 0xCF };
        public static byte[] MONSTER_XY_reset = { 0x41, 0x89, 0x87, 0xC8, 0x0F, 0x00, 0x00, 0x49, 0x8B, 0x87, 0xE8, 0x0F, 0x00, 0x00, 0x48, 0x85, 0xC0 };
        public static byte[] FLASH_reset = { 0x44, 0x8B, 0xCB, 0x44, 0x8B, 0xC7, 0x33, 0xD2, 0xE8, 0x77, 0xAB, 0xB7, 0xFC };
        public static byte[] FLASH_ENABLE_rest = { 0x45, 0x39, 0xAC, 0x24, 0xA0, 0x0B, 0x02, 0x00, 0x0F, 0x84, 0x34, 0x2D, 0x00, 0x00 };
        public static byte[] ITEM_XY_rest = { 0x48 ,0x89 ,0x4A ,0x14 ,0x48 ,0x89 ,0x16 };
        public static byte[] RUNE_CRACK_rest = { 0x9C ,0x51 ,0x48 ,0x81 ,0xEC ,0x08 ,0x00 ,0x00 ,0x00 };
        public static byte[] AUTO_Entry_rest = { 0xD0 ,0x39 ,0x32 ,0x57 ,0xFC ,0x7F ,0x00 ,0x00 };
    }
    public class cheat_state
    {
        public static bool CRC_bool = false;
        public static bool NO_DMG_bool = false;
        public static bool NO_FLY_DMG_bool = false;
        public static bool FAST_DROP_bool = false;
        public static bool MOB_FOLLOW_bool = false;
        public static bool SKILL_NO_Delay_bool = false;
        public static bool RUNE_TIMER_bool = false;
        public static bool RUNE_CRACK_bool = false;
        public static bool MONSTER_XY_bool = false;
        public static bool FLASH_bool = false;
        public static bool FLASH_ENABLE_bool = false;
        public static bool ITEM_XY_bool  = false;
        public static bool AUTO_Entry_bool = false;
    }
    public class cheat_Ptr
    {
        public static IntPtr NOW_Alloc_address { get; set; }= IntPtr.Zero;
        public static IntPtr JMPPointerBaseAddress { get; set; } = IntPtr.Zero;
        public static IntPtr CRC_pass { get; set; } = IntPtr.Zero;

        public static IntPtr NO_DMG { get; set; } = IntPtr.Zero;
        public static IntPtr NO_FLY_DMG { get; set; } = IntPtr.Zero;

        public static IntPtr FAST_DROP { get; set; } = IntPtr.Zero;

        public static IntPtr MOB_FOLLOW { get; set; } = IntPtr.Zero;

        public static IntPtr SKILL_NO_Delay_StaticAddress { get; set; } = IntPtr.Zero;
        public static IntPtr SKILL_NO_Delay_OverrideAddress { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0004
        /// </summary>
        public static IntPtr SKILL_Counter { get; set; } = IntPtr.Zero;

        public static IntPtr RUNE_TIMER_Static_Address { get; set; } = IntPtr.Zero;
        public static IntPtr RUNE_TIMER_OverrideAddress { get; set; } = IntPtr.Zero;

        public static IntPtr RUNE_CRACK_Static_Address { get; set; } = IntPtr.Zero;
        public static IntPtr RUNE_CRACK_OverrideAddress { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0020
        /// </summary>
        public static IntPtr RUNE_ARROW01_DATA { get; set;  } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0024
        /// </summary>
        public static IntPtr RUNE_ARROW02_DATA { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0028
        /// </summary>
        public static IntPtr RUNE_ARROW03_DATA { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF002C
        /// </summary>
        public static IntPtr RUNE_ARROW04_DATA { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0030
        /// </summary>
        public static IntPtr RUNE_READY_DATA { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0000
        /// </summary>
        public static IntPtr GET_RUNE_TIMER { get; set; } = IntPtr.Zero;

        public static IntPtr MONSTER_XY_static_address { get; set; } = IntPtr.Zero;
        public static IntPtr MONSTER_XY_OverrideAddress { get; set; } = IntPtr.Zero;
        /// <summary>
        /// //13FFF0008
        /// </summary>
        public static IntPtr MONSTER_X { get; set; } = IntPtr.Zero;
        /// <summary>
        /// //13FFF0010
        /// </summary>
        public static IntPtr MONSTER_Y { get; set; } = IntPtr.Zero;

        public static IntPtr FLASH_static_address { get; set; } = IntPtr.Zero;
        public static IntPtr FLASH_OverrideAddress { get; set; } = IntPtr.Zero;

        public static IntPtr FLASH_ENABLE_static_address { get; set; } = IntPtr.Zero;
        public static IntPtr FLASH_ENABLE_OverrideAddress { get; set; } = IntPtr.Zero;
        public static IntPtr ITEM_XY_static_address { get; set; } = IntPtr.Zero;
        public static IntPtr ITEM_XY_OverrideAddress { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0018
        /// </summary>
        public static IntPtr ITEM_XY { get; set; } = IntPtr.Zero;

        /// <summary>
        /// 148228A40
        /// </summary>
        public static IntPtr AUTO_Entry_static_address { get; set; } = (IntPtr)0x148228A40;

        public static IntPtr AUTO_Entry_OverrideAddress { get; set; } = IntPtr.Zero;

        public static IntPtr AUTO_Main_OverrideAddress { get; set; } = IntPtr.Zero;
        /// <summary>
        /// 0x13FFF0034
        /// </summary>
        public static IntPtr AUTO_Entry_chek { get; set; } = IntPtr.Zero;



    }
}
