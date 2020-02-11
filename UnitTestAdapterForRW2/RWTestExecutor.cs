using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.IO;
using RPG_Inventory_Remake_Common.UnitTest;
using System.Reflection;

namespace UnitTestAdapterForRW
{
    [ExtensionUri(RWConstants.ExecutorUri)]
    public class RWTestExecutor : ITestExecutor
    {
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent readDone =
            new ManualResetEvent(false);

        private static StateObject _state;


        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            IPEndPoint localEP = new IPEndPoint(ipAddress, 11001);

            Socket listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEP);
            listener.Listen(100);

            listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

            // Create a TCP/IP socket.  
            Socket client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            connectDone.WaitOne();
            Send(client, "StartTest");
            readDone.WaitOne();
            string message = _state.sb.Append(Encoding.ASCII.GetString(
                    _state.buffer)).ToString();
            string path = @"D:\Modding\UnitTestAdapterForRW\UnitTestAdapterForRW\bin\Debug\TestResult.txt";
            // Create a file to write to.
            StreamWriter sw = File.CreateText(path);
            sw.WriteLine(message);
            foreach (TestCase testCase in tests)
            {
                TestResult testResult = new TestResult(testCase)
                {
                    Outcome = TestOutcome.Passed
                };
                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, message));
                frameworkHandle.RecordResult(testResult);
            }



            //foreach (TestCase testCase in tests)
            //{
            //    frameworkHandle.RecordResult(new TestResult(testCase)
            //    {
            //        Outcome = TestOutcome.Passed
            //    });
            //}
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            //if (sources == null)
            //    throw new ArgumentNullException(nameof(sources));

            //string path = @"D:\Modding\UnitTestAdapterForRW\UnitTestAdapterForRW\bin\Debug\TestSources.txt";
            //// Create a file to write to.
            //StreamWriter sw = File.CreateText(path);
            //sw.WriteLine("Write from Executor");


            //foreach (string source in sources)
            //{
            //    Assembly assembly = Assembly.LoadFrom(source);
            //    foreach (var name in assembly.GetReferencedAssemblies())
            //    {
            //        sw.WriteLine(name.FullName);
            //    }
            //    foreach (Type type in assembly.GetTypes())
            //    {
            //        if (typeof(RPGIUnitTest).IsAssignableFrom(type))
            //        {
            //            sw.WriteLine(type.FullName);
            //        }
            //    }
            //    sw.WriteLine(source);
            //}
            //sw.Dispose();
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);
                connectDone.Set();

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            listener.Close();
            if (!handler.Connected)
            {
                Console.WriteLine("Handler is not connected");
            }

            // Create the state object.  
            _state = new StateObject();
            _state.workSocket = handler;
            handler.BeginReceive(_state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), _state);
        }

        public class StateObject
        {
            // Client  socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 1024;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                readDone.Set();
                // Check for end-of-file tag. If it is not there, read   
                // more data.  
                //content = state.sb.ToString();
                //Console.WriteLine("Message from Rimworld: " + content);
                //if (content.IndexOf("<EOF>") > -1)
                //{
                //    // All the data has been read from the   
                //    // client. Display it on the console.  
                //    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                //        content.Length, content);
                //}
                //else
                //{
                //    // Not all data received. Get more.  
                //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                //    new AsyncCallback(ReadCallback), state);
                //}
            }
        }
    }
}
