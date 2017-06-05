using System.Collections.Generic;

namespace ServiceManager
{
    public class ApiHost
    {
        public string Id { get; set; }
        public string Size { get; set; }
        public dynamic Location { get; set; }
        public List<string> Services { get; set; }
        public string Url { get; set; }
    }
}
