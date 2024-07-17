using System.Net;
using System;
using System.Net.Sockets;

namespace tcpclient
{

    // example
    // 0x90, 0x04, 0x18 > Header
    // 0x06 > Length
    // 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 > packet data
    // 0xc1, 0x00 > CRC
    class CustomPacket
    {
        public byte[] header { get; set; }
        public byte length { get; set; }
        public byte[] data { get; set; }
        public ushort checksum { get; set; }

        public CustomPacket()
        {
            header = new byte[3];
            data = new byte[6];
        }

        internal static int FindHeader(byte[] src, int start, int length)
        {
            for (int i = start; i < length; i++)
            {
                if (i >= src.Length)
                    return -1;

                if (src[i] == 0x90 && src[i + 1] == 0x04 && src[i + 2] == 0x18)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static ushort CalculateChecksum(byte[] src, int start, int length)
        {
            ushort sum = 0;
            for (int i = 0; i < length; i++)
            {
                if (i >= src.Length)
                    return 0;

                sum += src[start + i];
            }

            return sum;
        }

        // try to parse rray to CustomPacket from "every byte", if success return true and out the packet
        public static int TryParse(byte[] src, int start, int length, out CustomPacket packet)
        {
            // find header
            int headerIndex = FindHeader(src, start, length);

            // if header not found, return false
            if (headerIndex == -1)
            {
                packet = null;
                return -1;
            }

            // if header found, try to calculate checksum
            // get length of packet
            int _length = src[headerIndex + 3];

            // calculate checksum (header + length + packet)
            ushort checksum = CalculateChecksum(src, headerIndex, 4 + _length);
            ushort checksum2 = (ushort)(src[headerIndex + 4 + _length + 1] << 8 | src[headerIndex + 4 + _length]);

            // if checksum not correct return false
            if (checksum != checksum2)
            {
                packet = null;
                return -1;
            }

            // if parse the packet
            packet = new CustomPacket();
            Array.Copy(src, headerIndex, packet.header, 0, 3);
            packet.length = (byte)_length;
            Array.Copy(src, headerIndex + 4, packet.data, 0, 6);
            packet.checksum = checksum;

            // return next packet index
            int nextPacket = headerIndex + 4 + src[headerIndex + 3] + 2;     // header + length + packet + checksum
                                                                             // +1 To Go to the next byte
            if (nextPacket >= src.Length)
                return -1;

            return nextPacket;
        }
    }

        class Program
    {
        public static void Main(string[] args)
        {
            // create client
            TcpClient client = new TcpClient();
            client.Connect("127.0.0.1", 1234);

            // keep read data from server
            while (client.Connected)
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[256];
                    int bytesRead = 0;

                    while (true)
                    {
                        try
                        {
                            if (stream.CanRead)
                            {
                                if (stream.DataAvailable)
                                {
                                    bytesRead = stream.Read(buffer, 0, buffer.Length);

                                    // if no data read, break the loop
                                    if (bytesRead == 0)
                                    {
                                        Console.WriteLine("No Data");
                                    }

                                    Console.WriteLine("Read {0} bytes", bytesRead);

                                    // Try to parse the packet
                                    CustomPacket packet;
                                    int nextPacket = 0;
                                    do
                                    {
                                        nextPacket = CustomPacket.TryParse(buffer, nextPacket, bytesRead - 1, out packet);
                                        if (nextPacket == -1)
                                        {
                                            Console.WriteLine("No Found Packet, Skip...");
                                            break;
                                        }

                                        Console.WriteLine("Length: {0}\t/ Crc: {1:X4}\t/ Next Packet: {2} ", packet.length, packet.checksum, nextPacket);
                                    }
                                    while (nextPacket > 0);
                                }
                                else
                                {
                                    Thread.Sleep(100);
                                }

                                Array.Clear(buffer, 0, buffer.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }
                }
            }

            client.Close();
        }
    }
}
