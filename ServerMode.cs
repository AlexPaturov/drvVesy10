using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace drvVesy10
{
    /// <summary>
    /// Description of ServerMode.
    /// </summary>
    public class ServerMode
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        volatile bool  _run = true; // указатель на изменяемый объект
        Connection     conn  = null;
        
        public ServerMode()
        {
        }

        public int Run(Dictionary<string, string> d, SerialPort sp)
        {
            logger.Info("SOCKET SERVER MODE"); // SOCKET SERVER MODE  - waits for inbound connections
            DateTime dtPrevious = DateTime.Now;
            _run = true;

            Socket srv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            srv.Bind(new IPEndPoint(IPAddress.Any, int.Parse( d["socketport"].Trim() )));
            srv.Listen(1);
            srv.ReceiveTimeout = 10;
            //srv.SendTimeout = 2000;
            // srv.Blocking = false;

            while (_run)
            {
                Socket soc = null;

                if ((sp.IsOpen == false) && (srv.Poll(1000, SelectMode.SelectRead) == true))
                {
                    soc = srv.Accept();
                }

                if ((sp.IsOpen == false) && (soc != null))
                {
                    logger.Info("Tcp client connected");
                    conn = new Connection();
                    try
                    {
                        bool didStart = conn.StartConnection(soc, d, sp);
                    }
                    catch(Exception ex)
                    {
                        logger.Error("IP-to-SERIAL connection initialization failed" + "\r\nMS:" + ex.Message + "\r\nST:" + ex.StackTrace); // OK...may be the serial port was not free at the moment...
                        conn = null;
                    }
                }
                else
                {
                    if (DateTime.Now.Subtract( dtPrevious).TotalSeconds > 10)
                    {
                        logger.Info("Server active and idle");
                        dtPrevious = DateTime.Now;
                    }
                    Thread.Sleep(1);
                }
            }

            logger.Info("Server shutting down");
            srv.Close();
            srv = null;
            conn = null;
            
            return(0);
        }

        public void StopRequest()
        {
            if (conn != null)
                conn.StopRequest();

            _run = false;
            Thread.Sleep(1000); // 23.05.2024 - не тестировал
        }
    }
}
