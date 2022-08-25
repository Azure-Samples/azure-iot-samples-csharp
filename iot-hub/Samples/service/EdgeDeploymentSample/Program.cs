using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            if (!parameters.Validate())
            {
                throw new ArgumentException("Required parameters are not set. Please recheck required variables by using \"--help\"");
            }

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(parameters.HubConnectionString);

            var sample = new EdgeDeploymentSample(registryManager);
            await sample.RunSampleAsync().ConfigureAwait(false);

            Console.WriteLine("Done.");
        }
    }
}