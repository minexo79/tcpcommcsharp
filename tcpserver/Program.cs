using System;
using System.Net;
using System.Net.Sockets;

namespace tcpserver
{
    class Program
    {
        static byte[] TestData = new byte[]
        {
            0x90, 0x04, 0x18,
            0x06,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            0xc7, 0x00
        };

        static byte[] TestData2 = new byte[]
{
            0x90, 0x04, 0x18,
            0x0a,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a,
            0xed, 0x00
};

        static byte[] TestData3 = new byte[]
{
            0x90, 0x04, 0x18,
            0x02,
            0x01, 0x02,
            0xb1, 0x00
};

        static TcpListener server = new TcpListener(IPAddress.Any, 1234);
        static TcpClient client;

        static void Main(string[] args)
        {
            // open server
            begin:
            server.Start();
            Console.WriteLine("Server started on port 1234");

            try
            {
                // wait for client
                client = server.AcceptTcpClient();

                // if client is connected, accept it and keep send data until client disconnect
                Console.WriteLine("Client connected");
                NetworkStream stream = client.GetStream();

                while (client.Connected)
                {
                    if (stream.CanWrite)
                    {
                        stream.Write(TestData, 0, TestData.Length);
                        Thread.Sleep(10);
                        stream.Write(TestData2, 0, TestData2.Length);
                        Thread.Sleep(10);
                        stream.Write(TestData3, 0, TestData3.Length);
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                client.Close();
                server.Stop();
            }

            goto begin;
        }
    }
}