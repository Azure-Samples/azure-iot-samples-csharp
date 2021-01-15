using CommandLine;
using Microsoft.Azure.Devices.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace X509DeviceCertWithChainSample
{
    class Program
    {
        /// <summary>
        /// A sample to illustrate authenticating with a device by passing in the device certificate and 
        /// full chain of certificates from the one used to sign the device certificate to the one uploaded to the service.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        public static async Task<int> Main(string[] args)
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

            var chainCerts = new X509Certificate2Collection();
            chainCerts.Add(new X509Certificate2(parameters.RootCertPath));
            chainCerts.Add(new X509Certificate2(parameters.Intermediate1CertPassword));
            chainCerts.Add(new X509Certificate2(parameters.Intermediate2CertPassword));
            var deviceCert = new X509Certificate2(parameters.DevicePfxPath, parameters.DevicePfxPassword);
            var auth = new DeviceAuthenticationWithX509Certificate(parameters.DevicePfxPath, deviceCert);
            var transportType = TransportType.Amqp_Tcp_Only;
            var deviceClient = DeviceClient.Create(
                parameters.HostName,
                auth,
                transportType);
            var sample = new X509DeviceCertWithChainSample(deviceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
