using DIDATupleImlp;
using DTServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            ServerSMRImpl server = new ServerSMRImpl();
            server.StartServer(args);
        }
    }

    class ServerSMRImpl : MarshalByRefObject, IServerInterface
    {
        private List<IDIDATuple> tupleSpace = new List<IDIDATuple>();
        private Queue pending = new Queue();
        private TcpServerChannel channel;
        TcpServerChannel channelPPM;
        private int size = 0;
        private bool isfrozen = false;
        private int numOps = 0; //contador do num de add's e takes's

        private int MinDelay = -1;
        private int MaxDelay = -1;

        Dictionary<string, DIDATuple> queuedClients = new Dictionary<string, DIDATuple>();

        public int GetNumOps() { return numOps; }

        public void StartServer(string[] args)
        {
            Console.WriteLine("Welcome to Server-SMR");

            if (args.Length > 0 && args[0].Equals("--ppm"))
            {
                Console.Write("insert server_PPM name : ");
                string server_name_ppm = Console.ReadLine();
                //Channel for PPM
                Console.WriteLine("Running Server with PPM ENABLE ");
                channelPPM = new TcpServerChannel(server_name_ppm, 10000);
                ChannelServices.RegisterChannel(channelPPM, false);
                RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ServerSMRImpl), server_name_ppm, WellKnownObjectMode.Singleton);
                Console.WriteLine("Listenning for PPM in port 10000");
            }
            else if (args.Length > 0 && args[0].Equals("--terminal"))
            {
                string server_name;
                string port = "";
                string min = "";
                string max = "";

                //Ask information
                Console.Write("insert port : ");
                port = Console.ReadLine();
                Console.Write("insert server name : ");
                server_name = Console.ReadLine();
                Console.Write("insert server min_delay : ");
                min = Console.ReadLine();
                Console.Write("insert server max_delay : ");
                max = Console.ReadLine();

                WriteDelayFile(server_name, min, max);

                //Channel for clients
                channel = new TcpServerChannel(server_name, Int32.Parse(port));
                ChannelServices.RegisterChannel(channel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ServerSMRImpl), server_name, WellKnownObjectMode.Singleton);
                Console.WriteLine("Listenning for Clients in port " + port);

            }
            else if (args.Length > 0)
            {
                //waiting 
                string url = args[0];
                string min = args[1];
                string max = args[2];

                Server(url, min, max);

            }

            Console.WriteLine("Press any key to exit.");
            System.Console.ReadLine();
        }

        private static void WriteDelayFile(string server_name, string min, string max)
        {
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToDelay = newpath + server_name + "Delay.txt";

            if (!File.Exists(pathToDelay))
            {
                //File.Create(pathToDelay);
                File.WriteAllText(pathToDelay, "");
                TextWriter tw = new StreamWriter(pathToDelay);
                string values = "" + min + ":" + max;
                tw.WriteLine(values);
                tw.Close();
            }
            else
            {
                TextWriter tw = new StreamWriter(pathToDelay);
                string values = "" + min + ":" + max;
                tw.WriteLine(values);
                tw.Close();
            }
        }

        public IDIDATuple Add(IDIDATuple didatuple)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }
            
            return Add(didatuple, false);
        }

        private IDIDATuple Add(IDIDATuple didatuple, bool fromUnfreeze)
        {
            DIDATuple didaAdd = (DIDATuple)didatuple;
            if ((pending.Count == 0 && isfrozen == false) || fromUnfreeze)
            {
                tupleSpace.Add(didaAdd);
                numOps++;
                size++;
                Console.Write("Client added new tuple : ");
                this.PrintTuple(didaAdd);

                //se tiver algum client a espera deste tuplo
                foreach (KeyValuePair<string, DIDATuple> elem in queuedClients)
                {
                    if (elem.Value.CompareParams(didaAdd))
                    {
                        notifyClient(elem.Key);
                    }
                }

                if (didaAdd.getBroadme())
                {
                    didaAdd.setBroadme(false);
                    this.BroadcastAdd(didaAdd);
                }

                return didaAdd;
            }
            else
            {
                Console.Write("Client added new tuple inserted to the queue : ");
                this.PrintTuple(didaAdd);

                pending.Enqueue(new KeyValuePair<string, DIDATuple>("add", didaAdd));
                return null;
            }
        }

        //signal queued client
        private void notifyClient(string url)
        {
            string channel_name = url.Substring(21, (url.Length - 21));
            TcpClientChannel channelNotify = new TcpClientChannel(channel_name, null);
            ChannelServices.RegisterChannel(channelNotify, false);
            IClientSMRInterface client = (IClientSMRInterface)Activator.GetObject(typeof(IClientSMRInterface), url);
            client.ContinueCommand();
            ChannelServices.UnregisterChannel(channelNotify);
        }

        private void BroadcastAdd(DIDATuple didaAdd)
        {
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";

            string[] lines = System.IO.File.ReadAllLines(pathToList);
            foreach (string line in lines)
            {
                //port : name 
                string[] args = line.Split(':');

                int server_port = Int32.Parse(args[0]);
                string server_name = args[1];

                if (didaAdd.getName() != server_name)
                {
                    try
                    {
                        string url = "tcp://localhost:" + server_port + "/" + server_name;
                        TcpClientChannel channelnovo = new TcpClientChannel(server_name, null);
                        ChannelServices.RegisterChannel(channelnovo, false);
                        IServerInterface servernovo = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);
                        servernovo.Add(didaAdd);
                        ChannelServices.UnregisterChannel(channelnovo);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public IDIDATuple Read(IDIDATuple didatuple, string name, int port)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }
            this.RequestMostRecent();
            string url = "tcp://localhost:" + port + "/" + name;
            DIDATuple didaRead;
            lock (this)
            {
                didaRead = (DIDATuple)didatuple;
                if (pending.Count == 0 && isfrozen == false)
                {
                    foreach (DIDATuple didaTuple in tupleSpace)
                    {
                        if (didaTuple.CompareParams(didaRead))
                        {
                            Console.Write("Client reading tuple   : ");
                            this.PrintTuple(didaRead);
                            return didatuple;
                        }
                    }
                }
                else
                {
                    Console.Write("Client reading tuple  inserted to the queue : ");
                    this.PrintTuple(didaRead);

                    pending.Enqueue(new KeyValuePair<string, DIDATuple>("read", didaRead));
                }
            }
            queuedClients.Add(url, didaRead);
            return null;
        }

        public IDIDATuple Take(IDIDATuple didatuple, string name, int port)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }

            if (name == null)
            {
                numOps++;
            }
            this.RequestMostRecent();
            string url = "tcp://localhost:" + port + "/" + name;
            DIDATuple didaTake;
            //lock (this)
            //{
            didaTake = (DIDATuple)didatuple;

            int count = 0;
            if (pending.Count == 0 && isfrozen == false)
            {
                foreach (DIDATuple didaTuple in tupleSpace)
                {
                    if (didaTuple.CompareParams(didaTake))
                    {
                        //remove element from tuple space
                        Console.Write("Client removing tuple  : ");
                       
                        tupleSpace.RemoveAt(count);
                        this.PrintTuple(didaTake);
                        if (didaTake.getTakeme())
                        {
                            numOps++;
                            didaTake.setTakeme(false);
                            this.BroadcastTake(didaTake);
                        }
                        return didatuple;
                    }
                   
                    count++;
                }
            }
            else
            {
                Console.Write("Client removing tuple inserted to the queue  : ");
                this.PrintTuple(didaTake);

                pending.Enqueue(new KeyValuePair<string, DIDATuple>("take", didaTake));
            }
            //  }
            if (name != null)
            {
                queuedClients.Add(url, didaTake);
            }
            return null;
        }

        private void BroadcastTake(DIDATuple didaTake)
        {
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";

            string[] lines = System.IO.File.ReadAllLines(pathToList);
            foreach (string line in lines)
            {
                //port : name 
                string[] args = line.Split(':');

                int server_port = Int32.Parse(args[0]);
                string server_name = args[1];

                if (didaTake.getName() != server_name)
                {
                    try
                    {
                        string url = "tcp://localhost:" + server_port + "/" + server_name;
                        TcpClientChannel channelnovo = new TcpClientChannel(server_name, null);
                        ChannelServices.RegisterChannel(channelnovo, false);
                        IServerInterface servernovo = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);
                        DIDATuple didaToSend = didaTake;
                        didaToSend.setName(server_name);
                        
                        servernovo.Take(didaToSend, null, 0);
                        ChannelServices.UnregisterChannel(channelnovo);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void RequestMostRecent()
        {
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";

            string[] lines = System.IO.File.ReadAllLines(pathToList);
            foreach (string line in lines)
            {
                //port : name 
                string[] args = line.Split(':');

                int server_port = Int32.Parse(args[0]);
                string server_name = args[1];

                try
                {
                    string url = "tcp://localhost:" + server_port + "/" + server_name;
                    TcpClientChannel channelnovo = new TcpClientChannel(server_name, null);
                    ChannelServices.RegisterChannel(channelnovo, false);
                    IServerInterface servernovo = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);
                    if (servernovo.GetNumOps() > numOps)
                    {
                        tupleSpace = servernovo.GetTupleSpace();
                    }
                    ChannelServices.UnregisterChannel(channelnovo);
                }
                catch { }
            }
        }

        public List<IDIDATuple> GetTupleSpace()
        {
            return tupleSpace;
        }

        public void Wait(int mil)
        {
            string info = "Resting for " + mil + " milliseconds..";
            Console.WriteLine(info);
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

        public void Crash()
        {
            try
            {
                Thread.CurrentThread.Abort();
            }
            catch (ThreadAbortException abortException)
            {
                Console.WriteLine((string)abortException.ExceptionState);
            }
        }

        public void Freeze()
        {
            Console.WriteLine("Server is Freezed");
            isfrozen = true;
        }

        public void Unfreeze()
        {
            Console.WriteLine("Server is unFreezed");
            isfrozen = false;
            while (pending.Count > 0)
            {
                KeyValuePair<string, DIDATuple> valuePair = (KeyValuePair<string, DIDATuple>)pending.Dequeue();
                if (valuePair.Key.ToLower().Equals("add"))
                {
                    this.Add(valuePair.Value, true);
                }
            }

        }

        public string Server(string url, string min, string max)
        {


            string[] url_port = url.Split('/');
            string port = url_port[2].Split(':')[1];
            string serverName = url_port[3];

            //Channel for clients
            channel = new TcpServerChannel(serverName, Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(
            typeof(ServerSMRImpl), serverName, WellKnownObjectMode.Singleton);

            WriteDelayFile(serverName, min, max);

            Console.WriteLine("Listenning for Clients in port " + port);

            Freeze();
            Random rnd = new Random();
            int randomNumber = rnd.Next(Int32.Parse(min), Int32.Parse(max));
            Task.Delay(randomNumber).ContinueWith(t => Unfreeze());

            return serverName;
        }

        public bool XLBroadcastAdd(IDIDATuple didaTuple)
        {
            throw new NotImplementedException();
        }

        public IDIDATuple XLBroadcastRead(IDIDATuple didaTuple)
        {
            throw new NotImplementedException();
        }

        public List<IDIDATuple> XLBroadcastTake(IDIDATuple didaTuple)
        {
            throw new NotImplementedException();
        }

        public bool XLBroadcastRemove(IDIDATuple didaTuple)
        {
            throw new NotImplementedException();
        }

        public void SimulateDelay(IDIDATuple x)
        {
            DIDATuple didat = (DIDATuple)x;
            string name = didat.getName();
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToDelay = newpath + name + "Delay.txt";
            string[] lines = System.IO.File.ReadAllLines(pathToDelay);
            foreach (string line in lines)
            {
                //min : max 
                string[] args = line.Split(':');

                MinDelay = Int32.Parse(args[0]);
                MaxDelay = Int32.Parse(args[1]);
            }

            Random r = new Random();
            int delay = r.Next(MinDelay, MaxDelay);
            this.Wait(delay);
        }
    }
}
