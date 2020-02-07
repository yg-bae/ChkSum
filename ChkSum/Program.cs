using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ChkSum
{
    class Program
    {
        enum MicomIdx
        {
            T470,
            T383,
            T370,
            T60,
        };

        private struct MicomInfo
        {
            public int romSize;
            public ConsoleColor color;
        };
        
        /* [STAThread]
         * using System.Windows.Forms;에 있는 Clipboard class를 사용하기 위해 있어야 함.
         * Ref : https://stackoverflow.com/questions/3546016/how-to-copy-data-to-clipboard-in-c-sharp/34077334#34077334 */
        [STAThread]

        static void Main(string[] args)
        {
            Dictionary<MicomIdx, MicomInfo> micomInfos = new Dictionary<MicomIdx, MicomInfo>()
            {
                [MicomIdx.T470] = new MicomInfo { romSize = 1024 * 384, color = ConsoleColor.Red },
                [MicomIdx.T383] = new MicomInfo { romSize = 1024 * 256, color = ConsoleColor.Blue },
                [MicomIdx.T370] = new MicomInfo { romSize = 1024 * 256, color = ConsoleColor.Gray },
                [MicomIdx.T60] = new MicomInfo { romSize = 1024 * 60, color = ConsoleColor.Gray },
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
                Dictionary<MicomIdx, int> chkSum = new Dictionary<MicomIdx, int>();
                Dictionary<MicomIdx, string> chkSumStr = new Dictionary<MicomIdx, string>();
                string hexFile = System.IO.File.ReadAllText(filePath);

                Console.WriteLine("\n▶ File :");
                Console.WriteLine("{0}\n", filePath);
                Console.WriteLine("\n▶ ChkSum");

                foreach (MicomIdx micom in Enum.GetValues(typeof(MicomIdx)))
                {
                    chkSum[micom] = IntelCheckSUM(hexFile, micomInfos[micom].romSize);    // ex) 0x12345678
                    string chkSumStrInv = BitConverter.ToString(BitConverter.GetBytes(chkSum[micom])).Replace("-", "");    // ex) 7856452301
                    chkSumStr[micom] = string.Concat("0x", chkSumStrInv.Substring(2, 2), chkSumStrInv.Substring(0, 2)); // ex) 0x5678
                    Console.ForegroundColor = micomInfos[micom].color;
                    Console.WriteLine("\n {0:D}) {1} : {2}", micom, micom, chkSumStr[micom]);
                }

                Console.Write("\nClipboard로 복사할 MICOM의 번호({0}~{1})를 입력해 주세요 : ", 0, micomInfos.Count - 1);

                int selectedMicom = Console.Read() - '0';
                MicomIdx idx = MicomIdx.T470;

                if ((0 <= selectedMicom) && (selectedMicom <= (micomInfos.Count - 1)))
                {
                    idx = (MicomIdx)selectedMicom;
                }
                Clipboard.SetText(chkSumStr[idx]);
                Console.WriteLine("\nClipboard로 복사되었습니다. Ctrl+V 하시면 됩니다.");
                Console.ForegroundColor = micomInfos[idx].color;
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
    }
}
