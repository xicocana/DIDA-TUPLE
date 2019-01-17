using DTServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PPM
{
    class PPM : IPuppetMasterInterface
    {

        TcpClientChannel channel;

        //server_name,url
        Dictionary<string, IServerInterface> serversUpa = new Dictionary<string, IServerInterface>();
        List<KeyValuePair<string, IServerInterface>> serversUp = new List<KeyValuePair<string, IServerInterface>>();

        static void Main(string[] args)
        {
            Console.WriteLine("PuppetMaster v1.0\r\n");
            PPM puppet = new PPM();
            puppet.StartPPM(args);
        }

        public void StartPPM(String[] args)
        {
            //Initialize Channel
            TcpClientChannel channel = new TcpClientChannel("PPM", null);
            ChannelServices.RegisterChannel(channel, false);

            if (args.Length > 0 && args[0].Equals("--terminal"))
            {

                bool exit = false;




                while (!exit)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Write a command of PPM -  'q' to quit");
                    
                    string command = Console.ReadLine();
                    if (!command.Equals("q"))
                    {
                        try
                        {
                            Parser(command.Split(' ').ToList());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Command Invalid");
                        }

                    }
                    else
                    {
                        exit = true;
                    }
                }

            }
            else
            {
                //By script
                string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
                string newpath = Path.GetFullPath(Path.Combine(mypath, @"..\..\..\"));
                string path = newpath + "scripts\\PPMScript1.txt";
                string[] lines = System.IO.File.ReadAllLines(path);
                // Display the file contents by using a foreach loop.
                System.Console.WriteLine("Running script...");
                foreach (string line in lines)
                {
                    this.Parser(line.Split(' ').ToList());
                }

                // Keep the console window open in debug mode.
                Console.WriteLine("Press any key to exit.");
                System.Console.ReadLine();
            }
        }

        public void Parser(List<string> command)
        {
            switch (command[0].ToLower())
            {
                case "server":
                    this.Server(command[1], command[2], command[3], command[4]);
                    break;
                case "server2":
                    this.Server2(command[1], command[2], command[3], command[4]);
                    break;
                case "client":
                    string processClientName = this.Client(command[1], command[2], command[3]);
                    break;
                case "status":

                    break;
                case "crash":
                    Crash(command[1]);
                    break;
                case "freeze":
                    Freeze(command[1]);
                    break;
                case "unfreeze":
                    Unfreeze(command[1]);
                    break;
                case "wait":
                    Wait(Int32.Parse(command[1]));
                    break;
                default:
                    throw new Exception();
                  
            }
        }

        //NEED A INSTANCE OF SERVER ALREADY UP - > WITH ONLY PPM
        public void Server(string processName, string url, string min, string max)
        {
            string[] url_port = url.Split('/');
            string urlFinal = "tcp://localhost:10000/" + url_port.ElementAt(url_port.Length - 1) + "_PPM";

            IServerInterface server = GetProxyObject(processName, urlFinal);

            IServerInterface server2 = GetProxyObject(processName, url);

            serversUp.Add(new KeyValuePair<string, IServerInterface>(processName, server2));
            Console.WriteLine("ServerID added " + processName);

            server.Server(url, min, max);
        }

        public void Server2(string processName, string url, string min, string max)
        {
            Process serverProcess = new Process();
            string mypath = System.Reflection.Assembly.GetEntryAssembly().Location;
            serverProcess.StartInfo.FileName = Path.GetFullPath(Path.Combine(mypath, @"C:\Users\ASUS\Desktop\IST\DAD\proj\DIDA-TUPLE\Server\bin\Debug\Server.exe"));

            serverProcess.StartInfo.Arguments = url + " " + min + " " + max;
            serverProcess.Start();

            //string[] url_port = url.Split('/');
            //string urlFinal = "tcp://localhost:10000/" + url_port.ElementAt(url_port.Length - 1) + "_PPM";

            IServerInterface server = GetProxyObject(processName, url);

            serversUp.Add(new KeyValuePair<string, IServerInterface>(processName, server));

        }

        public string Client(string client_id, string url, string path_to_script)
        {
            Process client = new Process();
            //string currenDir = Environment.CurrentDirectory;
            client.StartInfo.FileName = @"C:\Users\ASUS\Desktop\IST\DAD\proj\DIDA-TUPLE\Script-Client\bin\Debug\Script-Client.exe";
            client.StartInfo.Arguments = url + " " + path_to_script;
            client.Start();
            string client_process_name = client.ProcessName;

            //clientsUp.Add(client_id);
            return client_process_name;
        }

        public void Crash(string processName)
        {
            IServerInterface server = GetProxyObject(processName, null);
            try
            {
                server.Crash();
            }
            catch (Exception e)
            {
                Console.WriteLine("Server killed : " + processName);
            }

        }

        public void Freeze(string processName)
        {
            IServerInterface server = GetProxyObject(processName, null);
            server.Freeze();
        }

        public void Unfreeze(string processName)
        {
            IServerInterface server = GetProxyObject(processName, null);
            server.Unfreeze();
        }

        public void Wait(int mil)
        {
            string info = "Resting for " + mil.ToString() + " milliseconds..";
            Console.WriteLine(info);
            Thread.Sleep(mil);
        }

        public void Status()
        {
            throw new NotImplementedException();
        }

        private IServerInterface GetProxyObject(string serverName, string url)
        {


            if (url != null)
            {
                return (IServerInterface)Activator.GetObject(typeof(IServerInterface), url);
            }
            else
            {
                foreach (KeyValuePair<string, IServerInterface> keyValue in serversUp)
                {
                    if (keyValue.Key.Equals(serverName))
                    {
                        return keyValue.Value;
                    }
                }
            }
            return null;

        }
    }
}
