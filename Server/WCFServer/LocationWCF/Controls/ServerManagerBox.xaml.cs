﻿using LocationWCFService.ServiceHelper;
using LocationWCFServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DbModel.Tools;
using LocationServices.LocationCallbacks;
using LocationServices.Locations;
using LocationWCFService;
using System.Web.Http.SelfHost;
using WebApiService;
using LocationServices.Tools;
using Microsoft.Owin.Hosting;
using SignalRService.Hubs;

using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Windows.Controls;
using BLL;
using DbModel.LocationHistory.Data;
using EngineClient;
using Location.BLL;
using Location.TModel.Location.Alarm;
using Location.TModel.Location.AreaAndDev;
using LocationServer;
using LocationServer.Windows;
using LocationServices.Converters;
using LocationServices.Locations.Interfaces;
using TModel.Location.Data;
using TModel.Tools;
using WebNSQLib;
using LocationServer.Tools;

namespace LocationServer.Controls
{
    /// <summary>
    /// Interaction logic for ServerManagerBox.xaml
    /// </summary>
    public partial class ServerManagerBox : UserControl
    {
        public ServerManagerBox()
        {
            InitializeComponent();
        }

        public void StopServices()
        {
            try
            {
                Location.BLL.Tool.Log.NewLogEvent -= ListenToLog;

                WriteLog("停止服务");
                //StopConnectEngine();
                if (LocationService.u3dositionSP != null)
                {
                    LocationService.u3dositionSP.Stop();
                    LocationService.u3dositionSP = null;
                }
                if (locationServiceHost != null)
                {
                    locationServiceHost.Close();
                    locationServiceHost = null;
                }
                if (httpHost != null)
                {
                    httpHost.CloseAsync();
                    httpHost = null;
                }
                if (SignalR != null)
                {
                    SignalR.Dispose();
                    SignalR = null;
                }

                if (wcfApiHost != null)
                {
                    wcfApiHost.Close();
                    wcfApiHost = null;
                }
                
                StopReceiveAlarm();

            }
            catch (Exception ex)
            {
                ListenToLog(ex.ToString());
            }
        }


        private void StartReceiveAlarm()
        {
            RealAlarm ra = new RealAlarm();
            ra.MessageHandler.DevAlarmReceived += Mh_DevAlarmReceived;
            if (alarmReceiveThread == null)
            {
                alarmReceiveThread = new Thread(ra.ReceiveRealAlarmInfo);
                alarmReceiveThread.IsBackground = true;
                alarmReceiveThread.Start();
            }
        }

        private void Mh_DevAlarmReceived(DbModel.Location.Alarm.DevAlarm obj)
        {
            AlarmHub.SendDeviceAlarms(obj.ToTModel());
        }

        private void StopReceiveAlarm()
        {
            if (alarmReceiveThread != null)
            {
                alarmReceiveThread.Abort();
                alarmReceiveThread = null;
            }
        }

        private Thread alarmReceiveThread;

        private void BtnStartService_Click(object sender, RoutedEventArgs e)
        {
            ClickStart();
        }

        public void ClickStart()
        {
            if (BtnStartService.Content.ToString() == "启动服务")
            {
                string host = TbHost.Text;
                string port = TbPort.Text;
                StartService(host, port);
                BtnStartService.Content = "停止服务";
            }
            else
            {
                StopServices();
                BtnStartService.Content = "启动服务";
            }
        }

        private void StartService(string host, string port)
        {
            try
            {
                Location.BLL.Tool.Log.NewLogEvent += ListenToLog;
                WriteLog("启动服务");

                StartLocationService(host, port);
                StartLocationServiceApi(host, port);
                StartReceiveAlarm();
                StartLocationAlarmService();
                StartWebApiService(host, port);
                StartSignalRService(host, port);
                
            }
            catch (Exception ex)
            {
                ListenToLog(ex.ToString());
            }
        }

        public void ShowTest(string str)
        {
            //textBox_U3DTEST.Text = str;
            //textBox_U3DTEST.AppendText( str);
        }

        private IDisposable SignalR;

        private void StartSignalRService(string host, string port)
        {
            //端口和主服务器(8733)一致的情况下，2D和3D无法连接SignalR服务器
            port = "8735";
            //string ServerURI = string.Format("http://{0}:{1}/", host,port);
            string ServerURI = string.Format("http://{0}:{1}/", "*", port);
            try
            {
                SignalR = WebApp.Start(ServerURI);
                WriteLog("SiganlR: " + ServerURI + "realtime");
            }
            catch (Exception ex)
            {
                //WriteToConsole("A server is already running at " + ServerURI);
                //this.Dispatcher.Invoke(() => ButtonStart.IsEnabled = true);
                //return;
                WriteLog(ex.ToString());
            }
        }

        string logText = "";

        private void WriteLog(string txt)
        {
            //string log = string.Format("[{0}][{1}]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"), txt);
            Location.BLL.Tool.Log.Info(txt);
        }

        private void ListenToLog(string log)
        {

            if (logText.Length > 20000)
            {
                logText = "";
            }
            logText = log + "\n" + logText;
            TbResult1.Dispatcher.Invoke(() =>
            {
                TbResult1.Text = logText;
            });
        }

        HttpSelfHostServer httpHost;

        private void StartWebApiService(string host, string port)
        {
            string path = string.Format("http://{0}:{1}/", host, port);
            var config = new HttpSelfHostConfiguration(path);
            WebApiConfiguration.Configure(config);
            httpHost = new HttpSelfHostServer(config);
            httpHost.OpenAsync().Wait();

            WriteLog("WebApiService: " + path + "api");
        }

        private ServiceHost locationServiceHost;

        private void StartLocationService(string host, string port)
        {
            //1.配置方式启动服务
            //locationServiceHost = new ServiceHost(typeof(LocationService));
            //locationServiceHost.SetProxyDataContractResolver();
            //locationServiceHost.Open();

            //2.编程方式启动服务
            string url = string.Format("http://{0}:{1}/LocationService", host, port);
            Uri baseAddres = new Uri(url);
            locationServiceHost = new ServiceHost(typeof(LocationService), baseAddres);
            BasicHttpBinding httpBinding = new BasicHttpBinding();
            //httpBinding.MaxReceivedMessageSize = 2147483647;
            //httpBinding.MaxBufferPoolSize = 2147483647;

            //Binding httpBinding = new WSHttpBinding();
            locationServiceHost.AddServiceEndpoint(typeof(ILocationService), httpBinding, baseAddres);
            Binding binding = MetadataExchangeBindings.CreateMexHttpBinding();
            locationServiceHost.AddServiceEndpoint(typeof(IMetadataExchange), binding, "MEX");
            //开放数据交付终结点，客户端才能添加/更新服务引用。
            ServiceThrottlingBehavior throttle = locationServiceHost.Description.Behaviors.Find<ServiceThrottlingBehavior>();
            if (throttle == null)
            {
                throttle = new ServiceThrottlingBehavior();
                locationServiceHost.Description.Behaviors.Add(throttle);
            }
            throttle.MaxConcurrentCalls = ConfigurationHelper.GetIntValue("MaxConcurrentCalls");
            throttle.MaxConcurrentSessions = ConfigurationHelper.GetIntValue("MaxConcurrentSessions");
            throttle.MaxConcurrentInstances = ConfigurationHelper.GetIntValue("MaxConcurrentInstances");

            locationServiceHost.Open();
            WriteLog("LocationService: " + locationServiceHost.BaseAddresses[0]);
        }

        private WebServiceHost wcfApiHost;

        private void StartLocationServiceApi(string host, string port)
        {
            string path = string.Format("http://{0}:{1}/LocationService/api", host, port);
            //LocationService demoServices = new LocationService();
            wcfApiHost = new WebServiceHost(typeof(LocationService));
            WebHttpBinding httpBinding = new WebHttpBinding();
            wcfApiHost.AddServiceEndpoint(typeof(ITestService), httpBinding, new Uri(path + "/test"));//不能是相同的地址，不同的地址的话可以有多个。
            //wcfApiHost.AddServiceEndpoint(typeof(IDevService), httpBinding, new Uri(path));
            wcfApiHost.AddServiceEndpoint(typeof(IPhysicalTopologyService), httpBinding, new Uri(path));
            wcfApiHost.Open();
            WriteLog("LocationService: " + path);
        }

        private ServiceHost locationAlarmServiceHost;
        private void StartLocationAlarmService()
        {
            locationAlarmServiceHost = new ServiceHost(typeof(LocationCallbackService));
            locationAlarmServiceHost.SetProxyDataContractResolver();

            locationAlarmServiceHost.Open();

            WriteLog("LocationAlarmService: " + locationAlarmServiceHost.BaseAddresses[0]);
        }
    }
}
