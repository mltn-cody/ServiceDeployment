using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Newtonsoft.Json.Linq;

namespace ServiceManager
{
    public class ServiceProcessManager
    {
        private static readonly ILog Logger = LogManager.GetLogger("ServiceManager.Logging");
        private static readonly string _baseDirectory = @"C:\inetpub\wwwroot\";
        private readonly Process[] _processList;

        public ServiceProcessManager()
        {
            _processList = Process.GetProcesses();
        }

        public bool  RedeployService(byte[] userDataJson)
        {
            var results = File.ReadAllText(@"ProcessManagement\ServiceMaps.json");
            Logger.Debug($"Parse Sevice Map...");
            var serviceMap = JObject.Parse(results);

            var userData = JObject.Parse(Encoding.UTF8.GetString(userDataJson, 0, userDataJson.Length));

            JToken activeServiceNode;
            if (!serviceMap.TryGetValue(userData.Value<string>("Node"), out activeServiceNode)) return true;
            if (activeServiceNode.Value<string>("Version") == userData.Value<string>("Version")) return false;
            DeleteDirectory($"{_baseDirectory}\\{userData.Value<string>("Node")}", activeServiceNode.Value<int>("ProcessId"));
            return true;
        }

        public  bool DeleteDirectory(string directoryPath, int processId)
        {
            Logger.Debug("Kill Process...");
            KillProcess(processId);
            Logger.Debug($"Delete Directory...{directoryPath}");
            Directory.Delete(directoryPath, true);
            Logger.Debug(Directory.Exists(directoryPath));
           return Directory.Exists(directoryPath);
        }

        public  void KillProcess(int id)
        {
            _processList.Single(x => x.Id == id)?.Kill();
        }


        public  void StartProcess(string serviceName, string searchDirectory, string version)
        {
            Logger.Debug($"Search Director: {searchDirectory + @"\bin\release"}");
            var executable = Directory.GetFiles(searchDirectory + @"\bin\release", $"*.WebAPI.server.exe", SearchOption.AllDirectories).ToList().First();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = executable
                }
            };
            process.Start();
            var activeProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)).FirstOrDefault(x => _processList.All(y => y.Id != x.Id));
            Logger.Debug($"Process Id: {activeProcess?.Id}");
            var serviceData = new JProperty(serviceName, JObject.Parse($"{{ 'ProcessId': {activeProcess?.Id}, 'Version': '{version}' }}"));
            Logger.Debug($"ServiceData Object: {serviceData}");
            UpdateServiceMap(serviceData, serviceName);
        }

        private  void UpdateServiceMap(JToken serviceData, string serviceName)
        {
            Logger.Debug("Searching Local ServiceMap.json");
            string serviceMap;
            
            Logger.Debug("Reading Resource Stream.");
            using (var stream = new FileStream(@"ProcessManagement\ServiceMaps.json", FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                serviceMap = reader.ReadToEnd();
            }
            Logger.Debug($"ServiceMap: {serviceMap}");
            if (string.IsNullOrEmpty(serviceMap))
            {
                Logger.Error("Invalid Service Map.");
                return;
            }
            var serviceMapJson = JObject.Parse(serviceMap);
            JToken test;
            if (serviceMapJson.TryGetValue(serviceName, out test))
            {
                if (test.Value<string>("Version") != serviceData.Value<string>("Version"))
                {
                    Logger.Debug($"Replacing ServiceData: {serviceData}");
                    serviceMapJson[serviceName].Replace(serviceData);
                }
            }
            else
            {
                Logger.Debug($"Adding Service Data: {serviceData}");
                serviceMapJson.Add(serviceData);
            }
            Logger.Debug($"Modified ServiceMap: {serviceMapJson}");

            using (var stream = new FileStream(@"ProcessManagement\ServiceMaps.json", FileMode.Open))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(serviceMapJson.ToString());
            }
            
        }

        public override string ToString()
        {
            var processes = new StringBuilder();
            foreach (var process in _processList)
            {
                processes.AppendLine($"Process: {process.ProcessName} ID: {process.Id}");
            }
            return processes.ToString();
        }
    }
}
