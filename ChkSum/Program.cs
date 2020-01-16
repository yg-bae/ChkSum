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

        enum MICOM
        {
            T470,
            T383,
            T370,
            T60,            
        };

        static void Main(string[] args)
        {
            Dictionary<MICOM, int> romSizes = new Dictionary<MICOM, int>()
            {
                [MICOM.T470] = 1024 * 384,
                [MICOM.T383] = 1024 * 256,
                [MICOM.T370] = 1024 * 256,
                [MICOM.T60] = 1024 * 60,
            };
#if DEBUG
            string filePath = @"D:\ChkSum\ChkSum\0x301F.hex";
#else
            if (args.Length == 0)
            {
                Console.WriteLine("Check Sum을 계산할 파일 경로를 입력하세요.");
                Console.WriteLine("ex) C:\\> ChkSum.exe \"C:\\TestPJT\\Test.hex\"");
                Console.ReadKey();
                return;
            }

            string filePath = args[0];
#endif
            if (File.Exists(filePath))
            {
                Dictionary<MICOM, int> chkSum = new Dictionary<MICOM, int>();
                Dictionary<MICOM, string> chkSumStr = new Dictionary<MICOM, string>();
                string hexFile = System.IO.File.ReadAllText(filePath);

                Console.WriteLine("\n▶ File :");
                Console.WriteLine("{0}\n", filePath);
                Console.WriteLine("\n▶ ChkSum");

                foreach (MICOM micom in Enum.GetValues(typeof(MICOM)))
                {
                    chkSum[micom] = IntelCheckSUM(hexFile, romSizes[micom]);    // ex) 0x12345678
                    string chkSumStrInv = BitConverter.ToString(BitConverter.GetBytes(chkSum[micom])).Replace("-", "");    // ex) 7856452301
                    chkSumStr[micom] = string.Concat("0x", chkSumStrInv.Substring(2, 2), chkSumStrInv.Substring(0, 2)); // ex) 0x5678
                    Console.WriteLine("\n {0:D}) {1} : {2}", micom, micom, chkSumStr[micom]);
                }

                Console.Write("\nClipboard로 복사할 MICOM의 번호({0}~{1})를 입력해 주세요 : ", 0, romSizes.Count - 1);

                int selectedMicom = Console.Read() - '0';
                MICOM idx = MICOM.T470;

                if ((0 <= selectedMicom) && (selectedMicom <= (romSizes.Count - 1)))
                {
                    idx = (MICOM)selectedMicom;
                }
                CopyToClipboard(chkSumStr[idx]);
                Console.WriteLine("\nClipboard로 복사되었습니다. Ctrl+V 하시면 됩니다.");
                Console.WriteLine("▶ {0}", chkSumStr[idx]);
            }
            else
            {
                Console.WriteLine("\n파일이 존재하지 않습니다.");
            }
            Console.ReadKey();
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

        private static void CopyToClipboard(string str)
        {
            // Copy to Clipboard
            OpenClipboard(IntPtr.Zero);
            var ptr = Marshal.StringToHGlobalUni(str);
            SetClipboardData(13, ptr);
            CloseClipboard();
            Marshal.FreeHGlobal(ptr);
        }
    }
}
