using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace drvVesy10
{   
    /// <summary>
    /// Description of Connection.
    /// </summary>
    public class Connection
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Socket socket;
        private SerialPort _sp;
        private bool   _isfree = true;
        private Dictionary<string, string> _d = null;
        bool    _keepOpen = true;
        bool transferOccured = false;

        public Connection( )
        {            
        }
        
        public Connection(Socket soc, Dictionary<string, string> d, SerialPort sp)
        {
            StartConnection(soc, d, sp);
        }

        public bool StartConnection(Socket soc, Dictionary<string, string> d, SerialPort sp)
        {
            socket = soc;
            _sp = sp;
            _d = d;
            _isfree = false;

            if (_sp.IsOpen == false)
            {
                logger.Info("Trying to open serial port " + _sp.PortName);
                try
                {
                    _sp.DataReceived += new SerialDataReceivedEventHandler(ReceiveData);
                    _sp.Open();
                }
                catch(Exception ee)
                {
                    logger.Error("SERIAL PORT OPEN - ERROR OCCURED" + "\r\nMS:" + ee.Message + "\r\nST:" + ee.StackTrace);
                    if (socket.Connected)
                        socket.Close();

                    if(_sp != null)
                      _sp.DataReceived -= ReceiveData;  // <- 04.01.2024 not tested

                    throw (ee); // throw it again
                }
                logger.Info("Serial port opened OK");
            }
            
            new Thread(Tranceiver).Start();     // здесь я стартую основной поток выполнения опроса устройства -> передачи данных       
            return(true);
        }

        // this will select the minimum of two numbers, i.e. limits result to given limit
        private int LimitTo( int i, int limit)
        {
            if (i > limit)
            {
                return( limit);
            }
            return( i );
        }
        
        // this is a loop where a live connection runs until a stop request
        // is set or until the socket connection is broken
        private void Tranceiver()
        {
            int bytesAvail = 0;            
            bool part1 = false;
            bool part2 = false;
            byte[] buf = new byte[8192];            // the std lower buffer size

            _keepOpen = true;
            logger.Info("Client connected from " + socket.RemoteEndPoint.ToString());
            
            while (_keepOpen)
            {
                transferOccured = false; // передача произошла
               
                // process IP-to-SERIAL 
                bytesAvail = LimitTo( socket.Available, 8192 );
                if (bytesAvail > 0)
                {
                    #region socket.Receive(buf, bytesAvail, SocketFlags.None);
                    try
                    {
                        socket.Receive(buf, bytesAvail, SocketFlags.None);
                        // need test 05.01.2024
                    }
                    catch (ArgumentNullException ex){ logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); } //  throw ?
                    catch (ArgumentOutOfRangeException ex){ logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (SocketException ex){ logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (ObjectDisposedException ex){ logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (System.Security.SecurityException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    #endregion

                    logger.Info("IP-to-SERIAL colByte: " + bytesAvail.ToString() + "  mess: " + Encoding.GetEncoding(1251).GetString(buf, 0, bytesAvail));

                    #region _sp.Write(buf, 0, bytesAvail);
                    try
                    {
                        _sp.Write(buf, 0, bytesAvail);
                        transferOccured = true;
                        // need test 05.01.2024
                    }
                    catch (ArgumentNullException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (InvalidOperationException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (ArgumentOutOfRangeException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (ArgumentException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    catch (System.TimeoutException ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
                    #endregion
                }

                // Check for broken socket connection
                part1 = socket.Poll(3000, SelectMode.SelectRead);
                part2 = (socket.Available == 0);
                
                if (part1 & part2)                                  /* connection lost. This seems to work*/
                {
                    logger.Info("IP connection lost ");
                    _keepOpen = false;
                }                    

                if (!transferOccured)
                    Thread.Sleep(10); // sleep a little, if nothing to do...
            }

            logger.Info("Client disconnected from " + socket.RemoteEndPoint.ToString());
            
            if (_sp.IsOpen == true)
            {
                logger.Info("Closing the serial port " + _sp.PortName);
                _sp.DataReceived -= ReceiveData;
                _sp.Close();
            }
           
            socket.Close();                       
            _isfree = true;
        }

        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(200);
            int bytesAvail = 0;
            
            #region bytesAvail = _sp.BytesToRead;
            try
            {
                if (_sp.IsOpen)
                  bytesAvail = _sp.BytesToRead;
            }
            catch (Exception ex) { logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace); throw (ex); }
            #endregion
            
            if (bytesAvail > 0)
            {
                byte[] bufMess = new byte[bytesAvail];

                #region _sp.Read(bufMess, 0, bytesAvail);
                try
                {
                    _sp.Read(bufMess, 0, bytesAvail);
                }
                catch (ArgumentNullException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (ArgumentException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (TimeoutException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                #endregion

                #region socket.Send(bufMess, bytesAvail, SocketFlags.None);
                try
                {
                    socket.Send(bufMess, bytesAvail, SocketFlags.None);
                    logger.Info("SERIAL-to-IP " + bytesAvail.ToString() + " mess: " + Encoding.GetEncoding(1251).GetString(bufMess, 0, bytesAvail));
                    transferOccured = true;
                }
                catch (ArgumentNullException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (SocketException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                catch (ObjectDisposedException ex)
                {
                    logger.Error("MS:" + ex.Message + "\r\nST:" + ex.StackTrace);
                    transferOccured = false;
                    throw (ex);
                }
                #endregion
            }
        }

        public bool IsFree()
        {
            return (_isfree);
        }

        public void StopRequest()
        {
            _keepOpen = false;
        }
    }
}
