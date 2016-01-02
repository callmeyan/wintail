using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Tail
{
    //定义处理程序委托  
    public delegate bool ConsoleCtrlDelegate(int ctrlType);
    class Program
    {
        //导入SetCtrlHandlerHandler API  
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        //当用户关闭Console时，系统会发送次消息  
        private const int CTRL_CLOSE_EVENT = 2;
        //Ctrl+C，系统会发送次消息  
        private const int CTRL_C_EVENT = 0;
        //Ctrl+break，系统会发送次消息  
        private const int CTRL_BREAK_EVENT = 1;
        //用户退出（注销），系统会发送次消息  
        private const int CTRL_LOGOFF_EVENT = 5;
        //系统关闭，系统会发送次消息  
        private const int CTRL_SHUTDOWN_EVENT = 6;

        FileSystemWatcher fsw = new FileSystemWatcher();
        private FileStream fs;
        private string fileName = null;
        private long fileEnd = 0;
        private FileInfo fi = null;
        //是否keep print
        private bool loop;
        //默认读取行数
        private int readLine = 10;

        private void Read(long offset, long length)
        {
            fileEnd = fs.Length;
            long l = length;
            if (offset > 0)
            {
                fs.Seek(offset, SeekOrigin.Begin);
            }
            byte[] buffer = new byte[length];
            fs.Position = offset;
            fs.Read(buffer, 0, (int)length);
            //此处加入你的处理逻辑  
            Console.Write(Encoding.Default.GetString(buffer));
            fs.Close();
            fs.Dispose();
            fs = null;
        }

        private void ReadLineFromEnd(int line)
        {
            List<byte> content = new List<byte>();
            long end = fs.Length;
            while (line >= 0 && end > 0)
            {
                byte[] b = new byte[1];
                end--;
                fs.Seek(end, SeekOrigin.Begin);
                fs.Read(b, 0, 1);
                if (b[0] == '\n')
                {
                    line--;
                }
                content.Insert(0, b[0]);
            }
            fs.Close();
            byte[] bs = new byte[content.Count];
            int index = 0;
            foreach (byte item in content)
            {
                bs[index++] = item;
            }
            Console.Write(Encoding.Default.GetString(bs));
            if (loop) Console.WriteLine();
            fs.Dispose();
            fs = null;
        }

        public void Start(string filePath)
        {
            fi = new FileInfo(filePath);
            fs = new FileStream(
                fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (fs.CanRead == false)
            {
                Console.WriteLine("File can not be read");
                return;
            }
            fileEnd = fs.Length;
            fileName = fi.Name;
            fsw.NotifyFilter = NotifyFilters.Size;
            fsw.Path = fi.DirectoryName;
            fsw.Changed += fsw_Changed;
            fsw.EnableRaisingEvents = true;
            ReadLineFromEnd(readLine);//读取默认行数


            do
            {
                ConsoleKeyInfo key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        break;
                }
            } while (loop);//just keep print
        }

        private void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.Name == fileName)
            {
                if (null == fs)
                {
                    try
                    {
                        Thread.Sleep(50);//避免其他程序的进程尚未退出
                        fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR:{0}", ex.Message);
                        try
                        {
                            Thread.Sleep(50);//避免其他程序的进程尚未退出
                            fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }
                        catch (Exception)
                        { Console.WriteLine("ERROR: open file failed"); }
                    }
                }
                if (null != fs && fs.Length > fileEnd)
                {
                    Read(fileEnd, fs.Length - fileEnd);
                }
            }
        }

        static void printUsage()
        {
            Console.Write("");
            Console.WriteLine(@"Usage: tail [OPTION]... [FILE]...
默认显示最后10行数据,OPTION参数如下：
    -f 循环读取
    -q 不显示处理信息
    -v 显示详细的处理信息
    -c <数目> 显示的字节数
    -n <行数> 显示行数
    --pid=PID 与-f合用,表示在进程ID,PID死掉之后结束. 
    -q, --quiet, --silent 从不输出给出文件名的首部 
    -s, --sleep-interval=S 与-f合用,表示在每次反复的间隔休眠S秒");

        }

        public void Process(string[] args)
        {
            string[] option = null;
            string filePath = null;
            foreach (string arg in args)
            {
                option = arg.Split(new char[] { '=' }, StringSplitOptions.None);
                switch (option[0])
                {
                    case "-f":
                        loop = true;
                        break;
                    case "-n":
                        if (option.Length != 2)
                        {
                            printUsage();
                            return;
                        }
                        readLine = int.Parse(option[1]);
                        break;
                    case "-c":
                        break;
                    case "-e":
                        break;
                    default:
                        filePath = arg;
                        break;
                }
            }
            if (string.IsNullOrEmpty(filePath))
            {
                printUsage(); return;
            }
            if (!File.Exists(filePath))
            {
                Console.WriteLine("ERROR: File not exists"); return;
            }
            Start(filePath);
        }

        static void Main(string[] args)
        {
            ConsoleCtrlDelegate consoleDelegete = new ConsoleCtrlDelegate(ProcessEvent);
            SetConsoleCtrlHandler(consoleDelegete, true);//处理ctrl按键
            if (args.Length < 2)
            {
                printUsage();
                return;
            }
            new Program().Process(args);

        }

        private static bool ProcessEvent(int ctrlType)
        {
            return false;
            //switch (ctrlType)
            //{
            //    case CTRL_C_EVENT:
            //        return true; //这里返回true，表示阻止响应系统对该程序的操作  
            //    //break;  
            //    case CTRL_BREAK_EVENT:
            //        Console.WriteLine("BREAK");
            //        break;
            //    case CTRL_CLOSE_EVENT:
            //        Console.WriteLine("CLOSE");
            //        break;
            //    case CTRL_LOGOFF_EVENT:
            //        Console.WriteLine("LOGOFF");
            //        break;
            //    case CTRL_SHUTDOWN_EVENT:
            //        Console.WriteLine("SHUTDOWN");
            //        break;
            //}
            ////return true;//表示阻止响应系统对该程序的操作  
            //return false;//忽略处理，让系统进行默认操作 
        }
    }
}
