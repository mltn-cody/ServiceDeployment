using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Consul;
using log4net;
using Newtonsoft.Json.Linq;

namespace ServiceManager
{
    public static class ConsulAgent
    {
        private static Dictionary<string, List<string>> ApiHosts = new Dictionary<string, List<string>>();
        private static readonly ILog _logger = LogManager.GetLogger("ServiceManager.Logging");

        public static Dictionary<string, List<string>> GetApiHostsServices(string apiHostName)
        {

             var response = GetApiHost(apiHostName).GetAwaiter().GetResult();
            var resultString = Encoding.UTF8.GetString(response.Value, 0, response.Value.Length);
            var services = JArray.Parse(resultString).ToObject<List<string>>();
            ApiHosts.Add(response.Key, services);
            return ApiHosts;
        }

        public static async Task<KVPair> GetApiHost(string apiHostName)
        {
            using (var client = new ConsulClient())
            {
                var servicesKvPairRequest = await client.KV.Get(apiHostName);
                var respone = servicesKvPairRequest.Response;
                return respone;
            }
        }

        public static async Task<string> GetServiceS3ZipFile(string serviceName)
        {
            using (var client = new ConsulClient())
            {
                var s3LocationUrlRequest = await client.KV.Get(serviceName + @"\S3Location");
                var response = s3LocationUrlRequest.Response;
                var locationUrl = Encoding.UTF8.GetString(response.Value, 0, response.Value.Length);
                _logger.Debug($"Location Url: {locationUrl}");
                DownloadS3Zip(locationUrl, serviceName);
                return Path.GetFullPath($"{serviceName}.zip");
            }
        }

        private static void DownloadS3Zip(string url, string saveFileName)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Arguments = $"/C aws s3 cp {url} {saveFileName}.zip"
                }
            };
            _logger.Debug($"Process Arguements: {process.StartInfo.Arguments}");
            process.Start();

            //* Set your output and error (asynchronous) handlers
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += OutputHandler;

            //* Start process and handlers
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

        }

        public static async Task<byte[]> GetUserDataJsonFile(string serviceName)
        {
            using (var client = new ConsulClient())
            {
                var s3LocationUrlRequest = await client.KV.Get(serviceName + @"\Userdata");
                var response = s3LocationUrlRequest.Response;
                var jsondata = Encoding.UTF8.GetString(response.Value, 0, response.Value.Length);
                _logger.Debug($"Location Url: {jsondata}");
                return response.Value;
            }
        }

        private static void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            _logger.Debug(e.Data);
        }
    }
}
