using System.Collections.Generic;

namespace ServiceDeployment
{
    public class ApiServices
    {
        public ConfigurationDefaults ConfigurationDefaults { get; set; }
        public dynamic Services { get; set; }
    }

    public class ConfigurationDefaults
    {
        public string Port { get; set;  }
        public string Domain { get; set;  }
    }

    public class Service
    {
        public string Name { get; set;  }
        public string Artifact { get; set; }
        public string Version { get; set;  }
        public Configuration Configuration { get; set; } 
        public List<string> Secrets { get; set; }
        public List<string> AwsKeys { get; set; } 
    }

    public class Configuration
    {
        public string Instance { get; set; }
        public bool WritePdfs { get; set; }
    }
}