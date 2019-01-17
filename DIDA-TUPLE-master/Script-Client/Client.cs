using DIDATupleImlp;
using DTServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Script_Client
{
    class Client : MarshalByRefObject, IClientSMRInterface
    {
        TcpClientChannel channel;
        private IServerInterface server;
        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            //string id;
            string url;
            string scriptname;

            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 17);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string path;
            string server_name = "";
            string server_port = "";
            Console.WriteLine("Welcome to Script-Client");

            if (args.Length == 2)
            {
                //id = args[0];
                url = args[0];
                scriptname = args[1];
                string[] url_port = url.Split('/');
                server_port = url_port[2].Split(':')[1];
            }
            else
            {
                Console.Write("insert server port : ");
                server_port = Console.ReadLine();
                Console.Write("insert server name : ");
                server_name = Console.ReadLine();
                Console.WriteLine("note: script must be in the same directory as script-client.exe!");
                Console.Write("insert script name : ");
                scriptname = Console.ReadLine();

                url = "tcp://localhost:" + server_port + "/" + server_name;
               
            }

            path = newpath + "scripts\\" + scriptname;

            Client p = new Client();
            p.StartClient(url, path, server_name, server_port);
            // Keep the console window open in debug mode.
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit.");
            System.Console.ReadLine();
        }

        List<string> linesToRepeat = new List<string>();
        int repeat = 0;
        public void StartClient(string url,string script, string name, string port)
        {
            channel= new TcpClientChannel("client", null);
            ChannelServices.RegisterChannel(channel, false);
            server = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);
            string[] lines = System.IO.File.ReadAllLines(script);
            // Display the file contents by using a foreach loop.
            System.Console.WriteLine("Running script...");

            foreach (string line in lines)
            {
                if (!String.IsNullOrEmpty(line)) {
                    if (repeat == 0 || line.Contains("end-repeat"))
                    {

                        List<string> initialLineArray = line.Split(' ').ToList();
                        if (initialLineArray.Count() > 1)
                        {
                            List<string> lineSeperated = Regex.Split(constructLine(initialLineArray[1]), @"(?!<(?:\(|\[)[^)\]]+),(?![^(\[]+(?:\)|\]))").ToList();
                            initialLineArray.RemoveAt(1);
                            initialLineArray.AddRange(lineSeperated);
                        }
                       
                        Parser(initialLineArray, name, port);
                    }
                    else
                    {
                        linesToRepeat.Add(line);
                    }
                }
            }
        }

        private string constructLine(string line)
        {
            //refatorizar
            int index = line.IndexOf("<");
            string finalLine = (index < 0)
                ? line
                : line.Remove(index, "<".Length);

            index = finalLine.LastIndexOf(">");
            finalLine = (index < 0)
                ? finalLine
                : finalLine.Remove(index, ">".Length);

            return finalLine;
        }

        public void Parser(List<string> command, string server_name, string server_port)
        {
            Console.WriteLine();
            Random r = new Random();

            switch (command[0].ToLower())
            {
                case "add":
                    Console.Write("Adding new tuple : ");
                    List<string> fieldsList = command.ToList();
                    fieldsList.RemoveAt(0);
                    DIDATuple didatuple = new DIDATuple(fieldsList);
                    didatuple.setName(server_name);
                    didatuple.setPort(Int32.Parse(server_port));
                    Add(didatuple);
                    this.PrintTuple(didatuple);
                    break;
                case "read":
                    Console.Write("Reading tuple    : ");
                    fieldsList = command.ToList();
                    fieldsList.RemoveAt(0);
                    didatuple = new DIDATuple(fieldsList);

                    didatuple.setName(server_name);
                    didatuple.setPort(Int32.Parse(server_port));

                    int channel_port = r.Next(7000, 8000);
                    string channel_name = "client" + channel_port;

                    IDIDATuple didatuplefinal = (DIDATuple)Read(didatuple, channel_name, channel_port);
                    this.PrintTuple(didatuple);

                    if (didatuplefinal == null )
                    {
                        Console.WriteLine("-> Unsuccesful search! Waiting..");

                        TcpServerChannel channel = new TcpServerChannel(channel_name, channel_port);
                        ChannelServices.RegisterChannel(channel, false);
                        RemotingConfiguration.RegisterWellKnownServiceType(
                        typeof(Client), channel_name, WellKnownObjectMode.Singleton);

                        BlockAndWait();
                        Read(didatuple, channel_name, channel_port);
                        Console.WriteLine("-> Succesful Read!");
                        ChannelServices.UnregisterChannel(channel);
                    }
                    else
                    {
                         Console.WriteLine("-> Succesful Read!");
                    }
                    break;
                case "take":
                    Console.Write("Removing tuple   : ");
                    fieldsList = command.ToList();
                    fieldsList.RemoveAt(0);
                    didatuple = new DIDATuple(fieldsList);

                    didatuple.setName(server_name);
                    didatuple.setPort(Int32.Parse(server_port));

                    int channel_port2 = r.Next(7000, 8000);
                    string channel_name2 = "client" + channel_port2;

                    IDIDATuple takeFromServer = (DIDATuple)Take(didatuple, channel_name2, channel_port2);
                    this.PrintTuple(didatuple);

                    if (takeFromServer == null)
                    {
                        Console.WriteLine("-> Unsuccesful take! Waiting..");

                        TcpServerChannel channel2 = new TcpServerChannel(channel_name2, channel_port2);
                        ChannelServices.RegisterChannel(channel2, false);
                        RemotingConfiguration.RegisterWellKnownServiceType(
                        typeof(Client), channel_name2, WellKnownObjectMode.Singleton);

                        BlockAndWait();
                        Take(didatuple, channel_name2, channel_port2);
                        Console.WriteLine("-> Succesful take!");
                        ChannelServices.UnregisterChannel(channel2);
                    }
                    else
                    {
                        Console.WriteLine("-> Succesful take!");
                    }
                    break;
                case "wait":
                    Console.WriteLine("Waiting");
                    Wait(Int32.Parse(command[1]));
                    break;
                case "begin-repeat":
                    Console.WriteLine("Start Repeat");
                    BeginRepeat(command[1]);
                    break;
                case "end-repeat":
                    EndRepeat(server_name, server_port);
                    break;
            }
        }

        private void BlockAndWait()
        {
            mre.WaitOne();
        }

        public void ContinueCommand()
        {
            mre.Set();
        }

        private IDIDATuple Add(IDIDATuple didatuple)
        {
            return server.Add(didatuple);
        }

        private IDIDATuple Read(IDIDATuple didatuple, string name, int port)
        {
            return server.Read(didatuple, name, port);
        }

        private IDIDATuple Take(IDIDATuple didatuple, string name, int port)
        {
            return server.Take(didatuple, name, port);
        }

        private void BeginRepeat(string other)
        {
            repeat = Int32.Parse(other);
        }

        private void EndRepeat(string name, string port)
        {
            while (this.repeat > 0)
            {
                foreach (string line in linesToRepeat)
                {
                    List<string> initialLineArray = line.Split(' ').ToList();
                    List<string> lineSeperated = Regex.Split(constructLine(initialLineArray[1]), @"(?!<(?:\(|\[)[^)\]]+),(?![^(\[]+(?:\)|\]))").ToList();
                    initialLineArray.RemoveAt(1);
                    initialLineArray.AddRange(lineSeperated);
                    Parser(initialLineArray, name, port);
                }
                this.repeat--;
            }
            Console.WriteLine("End Repeat");
        }

        private void Wait(int mil)
        {
            Thread.Sleep(mil);
        }

        public void PrintTuple(DIDATuple didatuple)
        {
            int tuplesize = didatuple.GetTupleList().Count;
            for (int i = 0; i < tuplesize; i++)
            {
                if (i == tuplesize - 1)
                {
                    Console.Write(didatuple.GetTupleList()[i]);
                    Console.WriteLine("");
                }
                else
                {
                    Console.Write(didatuple.GetTupleList()[i]);
                    Console.Write(",");
                }
            }
        }
    }
}
