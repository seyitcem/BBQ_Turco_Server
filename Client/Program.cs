using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpClient tcpClient = new TcpClient("127.0.0.1", 1234);
                Console.WriteLine("Connected to the server.");
                Console.WriteLine("");

                Thread thread = new Thread(Read);
                thread.Start(tcpClient);

                StreamWriter sWriter = new StreamWriter(tcpClient.GetStream());

                while (true)
                {
                    if (tcpClient.Connected)
                    {
                        string input = Console.ReadLine();
                        sWriter.WriteLine(input);
                        sWriter.Flush();
                    }
                }

            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            Console.ReadKey();
        }

        static void Read(object obj)
        {
            TcpClient tcpClient = (TcpClient)obj;
            StreamReader sReader = new StreamReader(tcpClient.GetStream());

            while (true)
            {
                try
                {
                    string message = sReader.ReadLine();
                    ////////// DATA MANIPULATION
                    if (message == "Update")
                    {
                        Console.WriteLine("Updating.");
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                    //////////
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
            }
        }
    }
}