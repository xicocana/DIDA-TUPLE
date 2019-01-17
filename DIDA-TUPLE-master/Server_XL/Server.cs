
using DIDATupleImlp;
using DTServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;

namespace Server_XL
{
    class Server
    {
        static void Main(string[] args)
        {
            ServerXLImpl server = new ServerXLImpl();
            server.StartServer(args);
        }
    }

    class TupleSpace
    {
        IDictionary<string, List<DIDATuple>> tupleSpace;
        int size;

        public TupleSpace()
        {
            this.tupleSpace = new Dictionary<string, List<DIDATuple>>();
            this.size = 0;
        }

        public bool Add(DIDATuple didaAdd)
        {
            Type typeFirstElem = didaAdd.GetTupleList()[0].GetType();

            if (tupleSpace.ContainsKey(typeFirstElem.ToString()))
            {
                //UPDATE AO DICIONARIO
                List<DIDATuple> oldDidaList = new List<DIDATuple>();
                bool result = tupleSpace.TryGetValue(typeFirstElem.ToString(), out oldDidaList);

                if (result)
                {
                    oldDidaList.Add(didaAdd);

                }
                size++;
            }
            else
            {
                //Adicionar ao Dicionario
                List<DIDATuple> newDidaList = new List<DIDATuple>();
                newDidaList.Add(didaAdd);
                tupleSpace.Add(typeFirstElem.ToString(), newDidaList);
                size++;
            }


            return true;
        }

        public DIDATuple Read(DIDATuple didaRead)
        {
            Type typeFirstElem = didaRead.GetTupleList()[0].GetType();

            if (tupleSpace.ContainsKey(typeFirstElem.ToString()))
            {
                List<DIDATuple> didaList = new List<DIDATuple>();
                bool result = tupleSpace.TryGetValue(typeFirstElem.ToString(), out didaList);

                if (result)
                {
                    foreach (DIDATuple didaTuple in didaList)
                    {
                        if (didaTuple.CompareParams(didaRead))
                        {
                            return didaTuple;
                        }
                    }

                }
            }
            return null;
        }

        public List<IDIDATuple> Take(DIDATuple didaTake)
        {
            Type typeFirstElem = didaTake.GetTupleList()[0].GetType();
            List<IDIDATuple> listToReturn = new List<IDIDATuple>();

            if (tupleSpace.ContainsKey(typeFirstElem.ToString()))
            {
                List<DIDATuple> didaList = new List<DIDATuple>();
                bool result = tupleSpace.TryGetValue(typeFirstElem.ToString(), out didaList);

                if (result)
                {
                    foreach (DIDATuple didaTuple in didaList)
                    {
                        if (didaTuple.CompareParams(didaTake))
                        {
                            listToReturn.Add(didaTake);
                        }
                    }

                }
            }
            return listToReturn;
        }

        public bool Remove(DIDATuple didaTake)
        {
            Type typeFirstElem = didaTake.GetTupleList()[0].GetType();
            List<DIDATuple> listToReturn = new List<DIDATuple>();

            if (tupleSpace.ContainsKey(typeFirstElem.ToString()))
            {
                listToReturn = tupleSpace[typeFirstElem.ToString()];
                tupleSpace.Remove(typeFirstElem.ToString());
                listToReturn.Remove(didaTake);

                tupleSpace.Add(typeFirstElem.ToString(), listToReturn);


                return true;
            }


            return false;
        }
    }

    class ServerXLImpl : MarshalByRefObject, IServerInterface
    {

        private TupleSpace tupleSpace = new TupleSpace();
        int size = 0;
        TcpServerChannel channelPPM;
        private TcpServerChannel channel;
        private bool isfrozen = false;
        private Queue pending = new Queue();
        private int MinDelay = -1;
        private int MaxDelay = -1;

        public void StartServer(string[] args)
        {
            Console.WriteLine("Welcome to Server-XL");

            if (args.Length > 0 && args[0].Equals("--ppm"))
            {
                Console.Write("insert server_PPM name : ");
                string server_name_ppm = Console.ReadLine();
                //Channel for PPM
                Console.WriteLine("Running Server with PPM ENABLE ");
                channelPPM = new TcpServerChannel(server_name_ppm, 10000);
                ChannelServices.RegisterChannel(channelPPM, false);
                RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ServerXLImpl), server_name_ppm, WellKnownObjectMode.Singleton);
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

                WriteDelayFile(min, max, server_name);

                //Channel for clients
                channel = new TcpServerChannel(server_name, Int32.Parse(port));
                ChannelServices.RegisterChannel(channel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(ServerXLImpl), server_name, WellKnownObjectMode.Singleton);
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



        public IDIDATuple Add(IDIDATuple didatuple)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }

            return Add(didatuple, false);
        }

        public IDIDATuple Add(IDIDATuple didatuple, bool fromXLBroadcast)
        {
            bool ackFromServers = false;
            DIDATuple didaAdd = null;
            if (!fromXLBroadcast)
            {
                //broadCast to all servers
                ackFromServers = BroadcastAdd((DIDATuple)didatuple);
            }

            if (ackFromServers || fromXLBroadcast)
            {
                didaAdd = (DIDATuple)didatuple;
                tupleSpace.Add(didaAdd);
                size++;
                Console.Write("Client added new tuple : ");
                this.PrintTuple(didaAdd);
            }
            else
            {
                Add(didatuple, false);
            }

            return didaAdd;
        }

        private bool BroadcastAdd(DIDATuple didaAdd)
        {
            int numberOfActiveServers = 1;
            int numberOfAcknowledge = 1;

            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 13);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";

            string[] lines = System.IO.File.ReadAllLines(pathToList);

            foreach (string line in lines)
            {
                //port : name : priority
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

                        if (servernovo.XLBroadcastAdd(didaAdd))
                        {
                            numberOfAcknowledge++;
                        }

                        ChannelServices.UnregisterChannel(channelnovo);

                        numberOfActiveServers++;
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            return numberOfAcknowledge >= (numberOfActiveServers / 2) + 1;

        }

        public IDIDATuple Read(IDIDATuple didatuple, string name, int port)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }
            return Read(didatuple, name, port, false);
        }

        private IDIDATuple Read(IDIDATuple didatuple, string name, int port, bool fromXLBroadcast)
        {
            //MULTICAST TO ALL SERVERS - TODO
            //
            lock (this)
            {
                DIDATuple didaResult = tupleSpace.Read((DIDATuple)didatuple);

                if (didaResult != null)
                {
                    return didaResult;
                }
                else
                {
                    if (!fromXLBroadcast)
                    {
                        //broadCast to all servers
                        return BroadcastRead((DIDATuple)didatuple);
                    }

                }

            }
            return null;
        }

        private DIDATuple BroadcastRead(DIDATuple didaAdd)
        {

            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 13);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";
            DIDATuple didaRead = null;

            string[] lines = System.IO.File.ReadAllLines(pathToList);

            foreach (string line in lines)
            {
                //port : name : priority
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

                        didaRead = (DIDATuple)servernovo.XLBroadcastRead(didaAdd);
                        if (didaRead != null)
                        {
                            break;
                        }

                        ChannelServices.UnregisterChannel(channelnovo);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            return didaRead;

        }

        public IDIDATuple Take(IDIDATuple didatuple, string name, int port)
        {
            if (MaxDelay == -1)
            {
                this.SimulateDelay(didatuple);
            }
            //broadCast to all servers
            return BroadcastTake((DIDATuple)didatuple);
        }

        private DIDATuple BroadcastTake(DIDATuple didatuple)
        {
            IDictionary<DIDATuple, int> intersectDictionairy = new Dictionary<DIDATuple, int>();
            List<IDIDATuple> didaResult = tupleSpace.Take((DIDATuple)didatuple);
            int numberOfActiveServers = 1;

            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 13);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToList = newpath + "ListaServers.txt";
            List<IDIDATuple> resultOfOtherS = null;

            string[] lines = System.IO.File.ReadAllLines(pathToList);
            bool repeat = false;
            do
            {
                repeat = false;

                foreach (string line in lines)
                {
                    //port : name : priority
                    string[] args = line.Split(':');
                    int server_port = Int32.Parse(args[0]);
                    string server_name = args[1];

                    if (didatuple.getName() != server_name)
                    {
                        try
                        {
                            string url = "tcp://localhost:" + server_port + "/" + server_name;
                            TcpClientChannel channelnovo = new TcpClientChannel(server_name, null);
                            ChannelServices.RegisterChannel(channelnovo, false);
                            IServerInterface servernovo = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);

                            resultOfOtherS = servernovo.XLBroadcastTake(didatuple);
                            ChannelServices.UnregisterChannel(channelnovo);

                            if (resultOfOtherS != null)
                            {
                                didaResult.AddRange(resultOfOtherS);
                                numberOfActiveServers++;
                            }
                            else
                            {
                                //TODO falta implementar se tiver null
                                repeat = true;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            } while (repeat);

            //ADD to dict
            foreach (IDIDATuple didaElem in didaResult)
            {
                DIDATuple didaTupleElem = (DIDATuple)didaElem;
                if (intersectDictionairy.ContainsKey(didaTupleElem))
                {
                    intersectDictionairy[didaTupleElem]++;
                }
                else
                {
                    intersectDictionairy.Add(didaTupleElem, 1);
                }
            }

            List<DIDATuple> matches = new List<DIDATuple>();

            //getIntersect
            foreach (KeyValuePair<DIDATuple, int> elem in intersectDictionairy)
            {
                if (elem.Value == numberOfActiveServers)
                {
                    matches.Add(elem.Key);
                }
            }

            Random r = new Random();
            int a = r.Next(0, matches.Count - 1);

            if (BroadcastRemove(matches[a], lines))
            {
                if (tupleSpace.Remove(matches[a]))
                {
                    Console.Write("Client removed  tuple : ");
                    this.PrintTuple(matches[a]);
                }
                else
                {
                    Console.Write("Error removing Tuple");
                }

                return matches[a];
            }

            return null;
        }

        private bool BroadcastRemove(DIDATuple didatuple, string[] lines)
        {
            int numberOfActiveServers = 1;
            bool ack = false;
            int numberOfAcks = 1;

            do
            {
                foreach (string line in lines)
                {
                    //port : name : priority
                    string[] args = line.Split(':');
                    int server_port = Int32.Parse(args[0]);
                    string server_name = args[1];

                    if (didatuple.getName() != server_name)
                    {
                        try
                        {
                            string url = "tcp://localhost:" + server_port + "/" + server_name;
                            TcpClientChannel channelnovo = new TcpClientChannel(server_name, null);
                            ChannelServices.RegisterChannel(channelnovo, false);
                            IServerInterface servernovo = (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);

                            ack = servernovo.XLBroadcastRemove(didatuple);
                            ChannelServices.UnregisterChannel(channelnovo);

                            if (ack)
                            {
                                numberOfAcks++;
                            }
                            else
                            {
                                break;
                            }

                            numberOfActiveServers++;
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
            } while (numberOfAcks != numberOfActiveServers);

            return true;
        }

        public void Wait(int mil)
        {
            string info = "Resting for " + mil.ToString() + " milliseconds..";
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
            throw new NotImplementedException();
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
                else if (valuePair.Key.ToLower().Equals("read"))
                {
                    this.Read(valuePair.Value, null, 0, true);
                }
                else if (valuePair.Key.ToLower().Equals("take"))
                {
                    //this.Take(valuePair.Value, null, 0, true);
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
            typeof(ServerXLImpl), serverName, WellKnownObjectMode.Singleton);

            WriteDelayFile(min, max, serverName);

            Console.WriteLine("Listenning for Clients in port " + port);

            Freeze();
            Random rnd = new Random();
            int randomNumber = rnd.Next(Int32.Parse(min), Int32.Parse(max));
            Task.Delay(randomNumber).ContinueWith(t => Unfreeze());

            return serverName;
        }

        private static void WriteDelayFile(string min, string max, string serverName)
        {
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string finalpath = mypath.Substring(0, mypath.Length - 10);
            string newpath = Path.GetFullPath(Path.Combine(finalpath, @"..\..\"));
            string pathToDelay = newpath + serverName + "Delay.txt";

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

        public bool XLBroadcastAdd(IDIDATuple didaTuple)
        {
            return Add(didaTuple, true) != null;
        }

        public IDIDATuple XLBroadcastRead(IDIDATuple didaTuple)
        {
            DIDATuple didaRead = (DIDATuple)didaTuple;
            return Read(didaTuple, didaRead.getName(), didaRead.getPort(), true);
        }

        public List<IDIDATuple> XLBroadcastTake(IDIDATuple didaTuple)
        {

            return tupleSpace.Take((DIDATuple)didaTuple);
        }


        public bool XLBroadcastRemove(IDIDATuple didaTuple)
        {
            bool result = tupleSpace.Remove((DIDATuple)didaTuple);
            if (result)
            {
                Console.Write("Client removed  tuple : ");
                this.PrintTuple((DIDATuple)didaTuple);
            }
            return result;
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


        public int GetNumOps()
        {
            throw new NotImplementedException();
        }

        public List<IDIDATuple> GetTupleSpace()
        {
            throw new NotImplementedException();
        }

    }
}

