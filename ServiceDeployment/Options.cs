using CommandLine;
using CommandLine.Text;

namespace ServiceDeployment
{
    class Options
    {
        [Option('h', "ApiHostName", Required = true, HelpText = "Input file to be processed.")]
        public string ApiHostName { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
