/*
cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
InstallUtil.exe D:\repos\cSharp\job\pipeComToIp\drvVesy10\bin\Debug\drvVesy10.exe
InstallUtil.exe /u D:\repos\cSharp\job\pipeComToIp\drvVesy10\bin\Debug\drvVesy10.exe
 */

/* Проработать FileAppender.LockingModel в App.config. Стоит ли переназначать дефолтное значение?  */

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.ServiceProcess;
using System.Threading;

namespace drvVesy10
{
    public partial class Service1 : ServiceBase
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static SerialPort sp = null;
        ServerMode sm = null;
        Dictionary<string, string> settCom = new Dictionary<string, string>();
        protected Thread thServiceThread = null;
        protected bool _connected = false;
        protected bool _is_shown = false;

        public Service1()
        {
            InitializeComponent();
            logger.Info("InitializeComponent()");
            #region словарь с настройками COM порта settCom
            settCom = new Dictionary<string, string>();

            settCom.Add("serialport", "COM3");
            settCom.Add("baudrate", "9600");

            //settCom.Add("serialport", "COM1");
            //settCom.Add("baudrate", "115200");

            settCom.Add("socketport", "8888");
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

            logger.Info("Service start " + settCom["serialport"] + " " + int.Parse(settCom["baudrate"].Trim()).ToString());
            try
            {
                sp = new SerialPort(settCom["serialport"], int.Parse(settCom["baudrate"].Trim()), Parity.None, 8, StopBits.One);
                sm = new ServerMode();
                retval = sm.Run(settCom, sp);
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

    }

}
