/*
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
InstallUtil.exe D:\repos\cSharp\job\pipeComToIp\drvVesy10\bin\Debug\drvVesy10.exe
InstallUtil.exe /u D:\repos\cSharp\job\pipeComToIp\drvVesy10\bin\Debug\drvVesy10.exe
Ставим запуск службы в автомате в ручную. При установке автозапуска службы в проекте, почему-то иногда не срабатывает. 
 */

/* Проработать FileAppender.LockingModel в App.config. Стоит ли переназначать дефолтное значение?  */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;

namespace drvVesy10
{
    public partial class Service1 : ServiceBase
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static SerialPort sp = null; // на удаление
        Socket moxaTC = null;  // нк
        ServerMode sm = null;
        Dictionary<string, string> dn = new Dictionary<string, string>();
        protected Thread thServiceThread = null;
        protected bool _connected = false;
        protected bool _is_shown = false;

        public Service1()
        {
            SetDefaultCulture();
            InitializeComponent();
            logger.Info("InitializeComponent()");
            #region словарь с настройками COM порта settCom
            dn = new Dictionary<string, string>();

            //dn.Add("serialport", "COM3"); // на удаление
            //dn.Add("baudrate", "9600");   // на удаление
            dn.Add("moxaHost", "10.10.10.1");       // нк
            dn.Add("moxaHostDKZ", "dkz-moxa-010");  // нк
            dn.Add("moxaPort", "4001");             // нк
            dn.Add("clientHost", "127.0.0.1");      // нк
            dn.Add("clientPort", "8888");           // нк

            //dn.Add("socketport", "8888");   // на удаление
            #endregion
        }

        protected override void OnStart(string[] args)
        {
            logger.Info("OnStart");
            thServiceThread = new Thread(ServiceThread);
            thServiceThread.Start();
        }

        void ServiceThread()
        {
            int retval = 0;

            //logger.Info("Service start " + dn["serialport"] + " " + int.Parse(dn["baudrate"].Trim()).ToString());
            logger.Info("ServiceThread start " + this.dn["clientHost"] + " " + int.Parse(this.dn["clientPort"].Trim()).ToString());
            try
            {
                //sp = new SerialPort(dn["serialport"], int.Parse(dn["baudrate"].Trim()), Parity.None, 8, StopBits.One);
                moxaTC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sm = new ServerMode();
                retval = sm.Run(dn, moxaTC);
            }
            catch (Exception ex) 
            { 
                logger.Error("Mode start failed.." + "\r\nMS:" + ex.Message + "\r\nST:" + ex.StackTrace);
            }
            logger.Info("Service stopped");
            
        }

        protected override void OnStop()
        {
            logger.Info("OnStop");
            HandleStop();
        }

        void HandleStop()
        {
            if (sm != null)
            {
                logger.Info("Stop request set to Server Mode");
                sm.StopRequest();
            }
            sm = null; 
        }

        public static void SetDefaultCulture()
        {
            CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            Type type = typeof(CultureInfo);
            type.InvokeMember("s_userDefaultCulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });

            type.InvokeMember("s_userDefaultUICulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });
        }

    }

}
