using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using log4net;
using Newtonsoft.Json.Linq;
using ServiceManager;

namespace ServiceDeployment
{

    class Program
    {
        private static string _apiHostName;
        private static readonly ILog Logger = LogManager.GetLogger("ServiceManager.Logging");
        private static readonly string _baseDirectory = @"C:\inetpub\wwwroot\";

        private static byte[] GenerateHash(byte[] keyValue)
        {
            byte[] hashValue;
            using (var sHhash = new SHA1Managed())
            {
                hashValue = sHhash.ComputeHash(keyValue);
            }
            return hashValue;
        }

        private static void CheckArguements(string[] args)
        {
            Logger.Debug("Reading Host Name: ");
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                // Values are available here
                if (options.ApiHostName != null)
                {
                    Logger.Debug($"ApiHostName: {options.ApiHostName}");
                    _apiHostName = options.ApiHostName;
                }
            }
            else
            {
                if (File.Exists($"{_baseDirectory}\\whoami.txt"))
                {
                    using (var apiHostFile = File.OpenRead($"{_baseDirectory}\\whoami.txt"))
                    {
                        var b = new byte[1024];

                        var temp = new UTF8Encoding(true);

                        while (apiHostFile.Read(b, 0, b.Length) > 0)
                        {
                            _apiHostName = temp.GetString(b);
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            // Read from the expermental.json 
            log4net.Config.XmlConfigurator.Configure();

            try
            {
                var assembly = typeof(Program).Assembly;
                var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
                var id = attribute.Value;
                using (new SingleAppMutexControl(id))
                {
                    CheckArguements(args);
                    var apiHost = ConsulAgent.GetApiHost(_apiHostName).GetAwaiter().GetResult();
                    byte[] hashvalue;
                    byte[] newHash;

                    do
                    {
                        hashvalue = GenerateHash(apiHost.Value);
                        var apiServices = ConsulAgent.GetApiHostsServices(_apiHostName);

                        // download from the s3 location 
                        foreach (var service in apiServices[_apiHostName])
                        {
                            var serviceProcessManager = new ServiceProcessManager();
                            var userdataFile = ConsulAgent.GetUserDataJsonFile(service).GetAwaiter().GetResult();

                            if (!serviceProcessManager.RedeployService(userdataFile)) continue;

                            Logger.Debug("Starting zip download of -");
                            Logger.Debug($"Service Name: {service}");

                            var archiveFileName = ConsulAgent.GetServiceS3ZipFile(service).GetAwaiter().GetResult();
                            var version = JObject.Parse(Encoding.UTF8.GetString(userdataFile, 0, userdataFile.Length))
                                .Value<string>("Version");

                            Logger.Debug($"Version: {version}");

                            var outfolder = _baseDirectory + $"{service}_{version}";
                            ServiceDownloader.ExtractZipFile(archiveFileName, null, outfolder);

                            ServiceDownloader.CreateUserDataFile(userdataFile, outfolder);
                            serviceProcessManager.StartProcess(service, outfolder, version);
                        }

                        apiHost = ConsulAgent.GetApiHost(_apiHostName).GetAwaiter().GetResult();
                        newHash = GenerateHash(apiHost.Value);

                    } while (Encoding.ASCII.GetString(hashvalue) != Encoding.ASCII.GetString(newHash));
                }
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error($"Timeout Exception. {timeoutException}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Messaging: {ex}");
            }
        }
    }
}
