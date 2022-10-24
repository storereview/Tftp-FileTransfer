using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tftp_FileTransfer.Protocol_Implementation;

namespace Tftp_FileTransfer
{
    class TelnetClient
    {
        // telnet连接状态
        public static Boolean Status = false;


        /** 
            创建一个 telnet 客户端
            创建成功后检测连接是否成功，成功则返回该客户端，否则返回 null
        **/
        public static TelnetConnection CreateTelnetClient(string ip, string username, string password)
        {
            TelnetConnection tc = new TelnetConnection();
            string msg = "";
            try
            {
                // 连接到 telnet 端口
                tc = new TelnetConnection(ip, 23);
                //进行登陆
                string loginRet = tc.Login(username, password, 1000);
                string prompt = loginRet.TrimEnd();
                if (prompt.Length == 0)
                    msg = "【Telnet】连接成功，但连接后返回的是空字符串";
                else
                {
                    prompt = loginRet.Substring(prompt.Length - 1, 1);
                    if (prompt != "$" && prompt != "#" && prompt != ">")
                        msg = "【Telnet】用户名和密码错误，请检查后重试";
                    else
                    {
                        msg = "【Telnet】登陆成功";
                        tc.LoginStatus = true;
                        Status = true;
                    }
                }
            }
            catch (Exception e)
            {
                msg = string.Format("【Telnet】连接失败，请检查网络后重试");
            }

            tc.RetMsg = msg;
            return tc;
        }


        /**
            通过tftp传输文件
        **/
        public static string TransferFileByTftp(TelnetConnection tc, string absolutePath, string device_log_dir)
        {

            //1.字符串分割，分割出文件目录路径和文件名
            string filename = absolutePath.Split('/')[absolutePath.Split('/').Length - 1];
            string dir = absolutePath.Replace(filename, "");
            //2.检查本地是否存在目标名称文件，存在则删除
            string localFilename = tc.Hostname + "_" + filename + ".txt";
            //3.Tftp传输文件
            //3.1获得和设备同一网段的本机IP地址，传输到本机电脑
            string pcIp = GetMostSimilarlyIP(Dns.GetHostEntry(Dns.GetHostName()).AddressList, tc.Hostname);

            tc.WriteLine(string.Format("cd {0}; tftp -l {1} -r {2} -p {3}", dir, filename, localFilename, pcIp));
            // tftp传输需要一定的时间，所以等待一段时间后。目前使用显式等待的方式，固定等待时间
            Thread.Sleep(5000);


            //4.传输完成以后，检查本地是否存在目标名称文件，存在则返回成功，不存在则返回文件传输失败
            string retMsg = "";
            string tftpFilename = Environment.CurrentDirectory + "\\" + localFilename;
            if (File.Exists(Environment.CurrentDirectory + "\\" + localFilename))
            {
                // 移动文件到指定路径
                File.Move(tftpFilename, device_log_dir + "\\" + localFilename);
                retMsg = filename + " 文件传输成功！\r\n";
            }
            else
            {
                retMsg = filename + " 文件传输失败（目录下不存在该文件）\r\n";
            }

            return "【打包】" + retMsg + tc.Read();
        }

        public static string GetMostSimilarlyIP(IPAddress[] ips, string targetIP)
        {
            string[] targetIpPartition = targetIP.Split('.');
            // 匹配位数
            var max = 0;
            var mostSimilarlyIP = "nullSimilarlyIP";

            for (int i = 0; i < ips.Length; i++)
            {
                var ip = ips[i].ToString();

                string[] ipPartition = ip.Split('.');

                if (ipPartition.Length != 4)
                    continue;

                int count = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (ipPartition[j].Equals(targetIpPartition[j]))
                    {
                        count += 1;
                    }
                    else
                    {
                        break;
                    }
                }
                if (count > max)
                {
                    mostSimilarlyIP = ip;
                    max = count;
                }
            }

            return mostSimilarlyIP;
        }

        /**
        public static void StartSetconsoleLog(TelnetConnection tc, MainWindow mw)
        {
            Thread.Sleep(200);
            tc.WriteLine("setconsole -r; setconsole;");

            String setconsoleRet = "";

            try
            {
                var nullTimes = 0;
                while (true)
                {
                    StringBuilder sb = new StringBuilder();
                    TelnetConnection.ParseTelnet(tc.TcpSocket, sb);
                    Thread.Sleep(100);
                    setconsoleRet = sb.ToString();
                    if (setconsoleRet.Equals(""))
                    {
                        nullTimes += 1;
                        if (nullTimes >= 100)
                        {
                            var ret = TelnetConnection.PingIP(tc.Hostname);
                            if (!ret)
                            {
                                mw.PrintSetconsoleLog("--------------------> 注意：setconsole运行日志已中断传输 <------------------- ");
                                // 释放资源
                                TelnetRunner.Status = false;
                                tc.TcpSocket.Close();
                                return;
                            }
                        }
                        continue;
                    }
                    nullTimes = 0;
                    mw.PrintSetconsoleLog(setconsoleRet);

                }
            }
            catch (TaskCanceledException e)
            {
                mw.PrintSetconsoleLog("--------------------> 注意：setconsole运行日志已中断传输 <------1------------- ");
            }
        }
        **/
    }
}

