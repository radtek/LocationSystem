﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchorUDPTool.Commands;
using ArchorUDPTool.Models;
using ArchorUDPTool.Tools;
using Coldairarrow.Util.Sockets;
using DbModel.Tools;
using TModel.Tools;

namespace ArchorUDPTool
{

    public class ArchorManager
    {
        Dictionary<string, LightUDP> udps = new Dictionary<string, LightUDP>();

        private int archorPort;

        CommandResultManager resultList;

        public UDPArchorList archorList;

        public CommandResultGroup AddArchor(System.Net.IPEndPoint iep, byte[] data)
        {
            lock (resultList.Groups)
            {
                if (archorList == null)
                {
                    archorList = new UDPArchorList();
                    //archorList.DataUpdated += (archor) =>
                    //{

                    //};
                    //archorList.DataAdded += (archor) =>
                    //{
                    //    //if (ArchorListChanged != null)
                    //    //{
                    //    //    ArchorListChanged(archorList);
                    //    //}
                    //};
                }

                CommandResultGroup group =resultList.Add(iep, data);
                archorList = OnDataReceive(group);
                return group;
            }

        }

        public string GetStatistics()
        {
            return resultList.GetStatistics();
        }

        public event Action<UDPArchorList> ArchorListChanged;

        public event Action<UDPArchor> ArchorUpdated;

        public event Action<string> LogChanged;

        public Stopwatch Stopwatch;

        public void StopTime()
        {
            if (Stopwatch == null)
            {
                Stopwatch = new Stopwatch();
            }
            Stopwatch.Stop();
        }

        public TimeSpan GetTimeSpan()
        {
            if (Stopwatch == null)
            {
                return TimeSpan.FromMilliseconds(0);
            }
            return Stopwatch.Elapsed;
        }

        private void StartTime()
        {
            StopTime();
            Stopwatch.Reset();
            Stopwatch.Start();
        }

        public class ScanArg
        {
            public string ipsText;
            public string port;
            public bool OneIPS;
            public bool ScanList;
            public bool Ping;
            public string[] cmds;
        }

        ScanArg arg;

        public void ScanArchors(ScanArg arg)
        {
            if (arg == null) return;
            this.arg = arg;
            var ips = arg.ipsText.Split(';');
            archorPort = arg.port.ToInt();
            if (resultList == null)
            {
                resultList = new CommandResultManager();
            }
            StartTime();

            List<string> localIps = GetLocalIps(ips);

            if (arg.OneIPS)
            {
                List<string> allIps = new List<string>();
                foreach (var ip in localIps)
                {
                    var ipList = IpHelper.GetIPS(ip);
                    allIps.AddRange(ipList);
                }
                ScanArchors(arg.cmds, allIps.ToArray());
            }
            else if (arg.ScanList)
            {
                List<string> allIps = new List<string>();
                if(archors!=null)
                    foreach (var archor in archors.ArchorList)
                    {
                        allIps.Add(archor.ArchorIp);
                    }
                else
                {
                    foreach (var archor in archorList)
                    {
                        allIps.Add(archor.GetClientIP());
                    }
                }
                ScanArchors(arg.cmds, allIps.ToArray());
            }
            else
            {
                ScanArchors(arg.cmds, localIps.ToArray());
            }
        }

        private List<string> GetLocalIps(string[] ips)
        {
            List<string> localIps = new List<string>();
            foreach (var ip in ips)
            {
                var localIp = IpHelper.GetLocalIp(ip);
                if (localIp != null)
                {
                    localIps.Add(ip);
                    AddLog("存在IP段:" + ip);
                }
                else
                {
                    AddLog("不存在IP段:" + ip);
                }
            }
            return localIps;
        }

        public string[] Ips;
        public string[] Cmds;

        private void ScanArchors(string cmd, string[] ips)
        {
            ScanArchors(new string[] { cmd }, ips);
        }

        private void ScanArchor(string cmd, string ip)
        {
            ScanArchors(new string[] { cmd }, new string[] { ip });
        }

        private void ScanArchors(string[] cmds, string[] ips)
        {
            this.Cmds = cmds;
            this.Ips = ips;

            if (arg.Ping)
            {
                if (pingEx == null)
                {
                    pingEx = new PingEx();
                    pingEx.ProgressChanged += PingEx_ProgressChanged;
                    pingEx.Error += (ex) =>
                    {
                        AddLog(ex.ToString());
                    };
                }
                pingEx.PingRange(Ips, 4);
            }
            else
            {
                worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;
                worker.WorkerReportsProgress = true;
                worker.DoWork += Worker_DoWork;
                worker.ProgressChanged += Worker_ProgressChanged;
                worker.RunWorkerAsync();
            }

            //Ping ping = new Ping();
            //ping.
        }

        BackgroundWorker worker;

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (PercentChanged != null)
            {
                PercentChanged(e.ProgressPercentage);
            }
        }

        public event Action<int> PercentChanged;

        PingEx pingEx;

        public int CmdSleepTime = 100;

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            IsCancel = false;
            BackgroundWorker worker = sender as BackgroundWorker;
 
            for (int j = 0; j < Ips.Length; j++)
            {
                string ip = Ips[j];

                //if (arg.Ping)
                //{
                //    if (pingEx == null)
                //    {
                //        pingEx = new PingEx();
                //        pingEx.ProgressChanged += PingEx_ProgressChanged;
                //    }
                //    pingEx.Ping(ip, 4);
                //}

                var localIp = IpHelper.GetLocalIp(ip);

                var udp = GetLightUDP(localIp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), archorPort);

                foreach (var cmd in Cmds)
                {
                    AddLog(string.Format("发送 :: [{0}]:{1}", ipEndPoint, cmd));
                    udp.SendHex(cmd, ipEndPoint);
                    Thread.Sleep(CmdSleepTime);
                }
                Thread.Sleep(CmdSleepTime * Cmds.Length);

                int percent = (int)((j + 1.0) / Ips.Length * 100);
                worker.ReportProgress(percent,Ips.Length);

                if (IsCancel)
                {
                    return;
                }
            }
        }

        private void PingEx_ProgressChanged(int arg1, PingResult arg2)
        {
            if (arg2.Type == 1)
            {
                var group = resultList.GetByIp(arg2.Ip);
                if (group == null)
                {
                    group = resultList.Add(arg2.Ip + ":Ping");
                    group.Archor.Ping = arg2.GetResult();
                }
                else
                {
                    group.Archor.Ping = arg2.GetResult();
                }

                archorList = OnDataReceive(group);
            }
            else
            {
                if (PercentChanged != null)
                {
                    PercentChanged(arg1);
                }
            }
        }

        public string Log = "";

        int logCount = 0;

        private void AddLog(string log)
        {
            logCount++;
            log = "[" + logCount + "]" + log;
            Log = log + "\n" + Log;
            if (Log.Length > 2000)
            {
                Log = Log.Substring(0, 1000);
            }
            if (LogChanged != null)
            {
                LogChanged(Log);
            }
        }

        private LightUDP GetLightUDP(IPAddress localIp)
        {
            LightUDP udp = null;
            var id = localIp.ToString();
            if (udps.ContainsKey(id))
            {
                udp = udps[id];
            }
            else
            {
                udp = new LightUDP(localIp, 1111);
                udp.DGramRecieved += Udp_DGramRecieved;
                udps[id] = udp;
            }
            return udp;
        }

        internal void SaveArchorList(string path)
        {
            XmlSerializeHelper.Save(archorList, path);
        }

        private void Udp_DGramRecieved(object sender, BUDPGram dgram)
        {
            //string hex = ByteHelper.byteToHexStr(dgram.data);
            ////string str = Encoding.UTF7.GetString(dgram.data);
            //string txt = string.Format("[{0}]:{1}", dgram.iep, hex);
            //AddLog(txt);

            var group=AddArchor(dgram.iep, dgram.data);
            AddLog(string.Format("收到 :: {0}", group.ToString()));
        }

        public void SendCmd(string cmd)
        {
            foreach (var archor in archorList)
            {
                SendCmd(cmd, archor);
                Thread.Sleep(100);
            }
        }

        private void SendCmd(string cmd, UDPArchor archor)
        {
            var archorIp = archor.GetClientIP();
            var localIp = IpHelper.GetLocalIp(archorIp);
            var udp = GetLightUDP(localIp);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(archorIp), archorPort);
            udp.SendHex(cmd, ipEndPoint);
            AddLog(string.Format("发送 :: [{0}]:{1}", ipEndPoint, cmd));

        }

        public void ResetAll(int port)
        {
            archorPort = port;
            Thread thread = ThreadTool.Start(() =>
            {
                SendCmd(UDPCommands.Restart);
            });
        }

        public void Reset(params UDPArchor[] archors)
        {
            foreach (var archor in archors)
            {
                SendCmd(UDPCommands.Restart, archor);
            }
        }

        public void SetServerIp251(int port)
        {
            archorPort = port;
            foreach (var archor in archorList)
            {
                var localIp = IpHelper.GetLocalIp(archor.Ip);
                var udp = GetLightUDP(localIp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(archor.Ip), archorPort);
                var cmd = "";
                if (archor.Ip.StartsWith("192.168.3."))
                {
                    if(archor.ServerIp!="192.168.3.251")
                        cmd = UDPCommands.ServerIp3251;
                }
                else if (archor.Ip.StartsWith("192.168.4."))
                {
                    if (archor.ServerIp != "192.168.4.251")
                        cmd = UDPCommands.ServerIp4251;
                }
                else if(archor.Ip.StartsWith("192.168.5."))
                {
                    if (archor.ServerIp != "192.168.5.251")
                        cmd = UDPCommands.ServerIp5251;
                }
                udp.SendHex(cmd, ipEndPoint);
                AddLog(string.Format("发送 :: [{0}]:{1}", ipEndPoint, cmd));
            }
        }

        public void SetServerIp253(int port)
        {
            archorPort = port;
            foreach (var archor in archorList)
            {
                string archorIp = archor.GetClientIP();
                var localIp = IpHelper.GetLocalIp(archorIp);
                if (localIp == null)
                {
                    continue;
                }
                var udp = GetLightUDP(localIp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(archorIp), archorPort);
                var cmd = "";
                if (archorIp.StartsWith("192.168.3."))
                {
                    if (archor.ServerIp != "192.168.3.253")
                        cmd = UDPCommands.ServerIp3253;
                }
                else if (archorIp.StartsWith("192.168.4."))
                {
                    if (archor.ServerIp != "192.168.4.253")
                        cmd = UDPCommands.ServerIp4253;
                }
                else if (archorIp.StartsWith("192.168.5."))
                {
                    if (archor.ServerIp != "192.168.5.253")
                        cmd = UDPCommands.ServerIp5253;
                }
                udp.SendHex(cmd, ipEndPoint);
                AddLog(string.Format("发送 :: [{0}]:{1}", ipEndPoint, cmd));
            }
        }

        internal void ClearBuffer()
        {
            resultList = new CommandResultManager();
        }

        internal void ScanArchor(params UDPArchor[] archors)
        {
            var ips = new List<string>();
            foreach (UDPArchor item in archors)
            {
                ips.Add(item.GetClientIP());
            }
            ScanArchors(UDPCommands.GetAll().ToArray(), ips.ToArray());
        }

        internal void GetArchorInfo(UDPArchor archor,string key)
        {
            //key = key.ToLower();
            if (key == "Id")
            {
                ScanArchor(UDPCommands.GetId, archor.GetClientIP());
            }
            else if (key == "Ip")
            {
                ScanArchor(UDPCommands.GetIp, archor.GetClientIP());
            }
            else if (key == "Type")
            {
                ScanArchor(UDPCommands.GetType, archor.GetClientIP());
            }
            else if (key == "ServerIp")
            {
                ScanArchor(UDPCommands.GetServerIp, archor.GetClientIP());
            }
            else if (key == "ServerPort")
            {
                ScanArchor(UDPCommands.GetServerPort, archor.GetClientIP());
            }
            else if (key == "Mask")
            {
                ScanArchor(UDPCommands.GetMask, archor.GetClientIP());
            }
            else if (key == "Gateway")
            {
                ScanArchor(UDPCommands.GetGateway, archor.GetClientIP());
            }
            else if (key == "DHCP")
            {
                ScanArchor(UDPCommands.GetDHCP, archor.GetClientIP());
            }
            else if (key == "SoftVersion")
            {
                ScanArchor(UDPCommands.GetSoftVersion, archor.GetClientIP());
            }
            else if (key == "HardVersion")
            {
                ScanArchor(UDPCommands.GetHardVersion, archor.GetClientIP());
            }
            else if (key == "Power")
            {
                ScanArchor(UDPCommands.GetPower, archor.GetClientIP());
            }
            else if (key == "MAC")
            {
                ScanArchor(UDPCommands.GetMAC, archor.GetClientIP());
            }

        }

        internal void UnCheckAll()
        {
            if (archorList != null)
            {
                foreach (var item in archorList)
                {
                    item.IsChecked = false;
                }
            }
        }

        internal void CheckAll()
        {
            if (archorList != null)
            {
                foreach (var item in archorList)
                {
                    item.IsChecked = true;
                }
            }
        }

        internal void Cancel()
        {
            if (arg == null) return;
            if (arg.Ping)
            {
                pingEx.Cancel();
            }
            else
            {
                worker.CancelAsync();
                IsCancel = true;
            }
        }

        private bool IsCancel = false;

        public List<LightUDP> serverUdps = new List<LightUDP>();

        internal void StartListen()
        {
            
            //var servers = archorList.ServerList;
            //foreach (var server in servers)
            //{
            //    var localIp = IpHelper.GetLocalIp(server.Ip);
            //    if (localIp != null && localIp.Address.ToString() == server.Ip)
            //    {
            //        LightUDP udp = new LightUDP(server.Ip, server.Port);
            //        udp.DGramRecieved += Udp_DGramRecieved1;
            //        serverUdps.Add(udp);
            //    }
            //}

            string[] ips = new string[] { "192.168.3.253", "192.168.4.253", "192.168.5.253" };
            foreach (var ip in ips)
            {
                var localIp = IpHelper.GetLocalIp(ip);
                if (localIp != null && localIp.ToString() == ip)
                {
                    LightUDP udp = new LightUDP(ip, 1998);
                    udp.DGramRecieved += Udp_DGramRecieved1;
                    serverUdps.Add(udp);
                }
            }

        }

        public UDPArchorValueList valueList = new UDPArchorValueList();

        private void Udp_DGramRecieved1(object sender, BUDPGram dgram)
        {
            if (resultList == null)
            {
                resultList = new CommandResultManager();
            }
            var group = resultList.Add(dgram.iep,dgram.data);
            group.Archor.Value = ByteHelper.byteToHexStr(dgram.data);

            valueList.Add(dgram.iep, dgram.data);
            AddLog(string.Format("收到 :: {0}", group.ToString()));


            archorList=OnDataReceive(group);
        }

        private UDPArchorList OnDataReceive(CommandResultGroup group)
        {
            UDPArchorList list = new UDPArchorList();
            foreach (var item in resultList.Groups)
            {
                list.Add(item.Archor);
                item.Archor.Num = list.Count;
            }
            if (ArchorUpdated != null)
            {
                ArchorUpdated(group.Archor);
            }
            if (ArchorListChanged != null)
            {
                ArchorListChanged(list);
            }
            return list;
        }

        public void StopListen()
        {
            foreach (var item in serverUdps)
            {
                item.Close();
            }
            serverUdps.Clear();
        }

        internal void SetArchorInfo(UDPArchor archor, string key)
        {
            if (key == "id")
            {

            }
            ScanArchors(UDPCommands.GetAll().ToArray(), new string[] { archor.GetClientIP() });
        }

        ArchorDevList archors;

        internal void LoadList(ArchorDevList archors)
        {
            this.archors = archors;
            resultList = new CommandResultManager();
            foreach (var item in archors.ArchorList)
            {
                var group=resultList.Add(item);
                //group.Archor.Ip = item.ArchorIp;
                group.Archor.Area = item.InstallArea;
            }

            UDPArchorList list = new UDPArchorList();
            foreach (var item in resultList.Groups)
            {
                list.Add(item.Archor);
                item.Archor.Num = list.Count;
            }

            archorList = list;
            if (ArchorListChanged != null)
            {
                ArchorListChanged(list);
            }
        }

        internal void LoadArchorList(string path)
        {
            archorList = XmlSerializeHelper.LoadFromFile<UDPArchorList>(path);

            resultList = new CommandResultManager();
            foreach (var item in archorList)
            {
                var group = resultList.Add(item);
            }

            if (ArchorListChanged != null)
            {
                ArchorListChanged(archorList);
            }
        }
    }
}