// See https://aka.ms/new-console-template for more information
using System;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Renci.SshNet.Common;

namespace SshNet
{
    class AccessSsh
    {
        string name;
        string address;
        int port;
        string user;
        string password;
        string path;

        public AccessSsh(string inputName, string inputAddress, int inputPort, string inputUser, string inputPassword, string inputPath)
        {
            name = inputName;
            address = inputAddress;
            port = inputPort;
            user = inputUser;
            password = inputPassword;
            path = inputPath;
        }
        //Access SSH & Run SmartConnector (run.sh) 
        public void ConnectEdge()
        {
            try
            {
                //create new client & session
                SshClient client = new SshClient(address, port, user, password);
                client.Connect();

                //create terminal -> used by ShellStream
                //create a dictionary of terminal modes & add terminal mode
                IDictionary<TerminalModes, uint> termkvp = new Dictionary<TerminalModes, uint>();
                termkvp.Add(TerminalModes.ECHO, 53);

                //execute start.sh script
                ShellStream shellStream = client.CreateShellStream("xterm", 80, 24, 800, 600, 1024, termkvp);
                var output = shellStream.Expect(new Regex(@"[$>]"));
                shellStream.WriteLine($"cd {path}");
                shellStream.WriteLine("sudo sh run.sh");
                output = shellStream.Expect(new Regex(@"([$#>:])"));
                shellStream.WriteLine(password);

                string line;
                while ((line = shellStream.ReadLine(TimeSpan.FromSeconds(2))) != null)
                {
                    Console.WriteLine(line);
                }

                Console.WriteLine($"{name} SmartConnector Started. Press Z to exit...");
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey();
                        if (key.Key == ConsoleKey.Z)
                        {
                            shellStream.WriteLine($"cd {path}");
                            output = shellStream.Expect(new Regex(@"[$>]"));
                            shellStream.WriteLine("sudo sh stop.sh");
                            output = shellStream.Expect(new Regex(@"([$#>:])"));
                            shellStream.WriteLine(password);
                            Console.WriteLine($"\r\n--- {name}SmartConnector Stopped ---");
                            client.Disconnect();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            ThreadStart ts2 = new ThreadStart(KukaConnect);
            Thread kuka = new Thread(ts2);
            kuka.Start();
        }

        static void KukaConnect()
        {
            //KUKA config
            string kukaName = "KUKA";
            string kukaAddress = "10.20.193.101";
            int kukaPort = 22;
            string kukaUser = "hyundai";
            string kukaPw = "opcua5497";
            string kukaPath = "/home/hyundai/Hyundai-KUKA";
            AccessSsh kuka = new AccessSsh(kukaName, kukaAddress, kukaPort, kukaUser, kukaPw, kukaPath);
            kuka.ConnectEdge();
        }
    }
}
