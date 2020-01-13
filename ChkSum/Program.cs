using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;

namespace ChkSum
{
    class Program
    {
        [DllImport("user32.dll")]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

        static void Main(string[] args)
        {
            if (args.Length == 0) Console.WriteLine("Input the path to calculate check sum.");
            else
            {
                string filePath = args[0];

                if (File.Exists(filePath))
                {
                    string hexFile = System.IO.File.ReadAllText(filePath);
                    int chkSum = IntelCheckSUM(hexFile, 384 * 1024);    // ex) 0x12345678
                    string chkSumStrInv = BitConverter.ToString(BitConverter.GetBytes(chkSum)).Replace("-", "");    // ex) 7856452301
                    string chkSumStr = string.Concat("0x", chkSumStrInv.Substring(2, 2), chkSumStrInv.Substring(0, 2)); // ex) 0x5678

                    // Copy to Clipboard
                    OpenClipboard(IntPtr.Zero);
                    var ptr = Marshal.StringToHGlobalUni(chkSumStr);
                    SetClipboardData(13, ptr);
                    CloseClipboard();
                    Marshal.FreeHGlobal(ptr);


                    Console.WriteLine("\n▶ File :");
                    Console.WriteLine("{0}", filePath);
                    Console.WriteLine("\n▶ ChkSum : {0}", chkSumStr);
                }
                else
                {
                    Console.WriteLine("File does not exist.");
                }
            }
            System.Console.ReadKey();
        }

        static int IntelCheckSUM(string file_buf, int RomLength)
        {
            int i = 0;
            int DataCnt = 0;
            int chksum = 0;
            int LoopFlag = 0;
            int Line_LEN;
            int DUMMY;
            while (LoopFlag == 0)
            {
                if (file_buf[i++] != ':') continue;
                Line_LEN = ASC_to_HEX_1Byte(file_buf[i], file_buf[i + 1]);
                i += 2;                                 // Line Length부분 증가
                i += 4;                                 // HEX파일의 ROM Address부분 SKIP
                DUMMY = ASC_to_HEX_1Byte(file_buf[i], file_buf[i + 1]);
                i += 2;                                 // 
                if (DUMMY == 0x00)                      // DUMMY Code값이 0x00일때만 덧셈을 한다.
                {
                    while ( (Line_LEN--) > 0)
                    {
                        DataCnt++;
                        chksum += ASC_to_HEX_1Byte(file_buf[i], file_buf[i + 1]);
                        i += 2;                         // 

                    }

                }
                else if ((DUMMY == 0x01) || (DUMMY == 0x05))// DUMMY값이 0x01 or 0x05일때 while Loop종료
                {
                    LoopFlag = 1;
                }
                else
                {
                    i += (Line_LEN * 2);                    // DUMMY값이 Data가 아닐때 SKIP
                }

                i += 3;                                 // Line Checksum(2), Line feed code(1) SKIP

            }

            chksum += (RomLength - DataCnt) * 0xff;
            return chksum;
        }

        private static int ASC_to_HEX_1Byte(char ch1, char ch2)
        {
            string hex = string.Concat(ch1, ch2);
            return Convert.ToInt32(hex, 16);
        }
    }
}
