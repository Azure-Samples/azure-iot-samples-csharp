using CommandLine;

namespace Microsoft.Azure.Devices.Provisioning.Service.Samples
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    internal class Parameters
    {
        [Option(
            "CertificatePath",
            Required = true,
            HelpText = "The path to X509 certificate.")]
        public string CertificatePath { get; set; }

        [Option(
            "ProvisioningConnectionString",
            Required = false,
            HelpText = "The primary connection string of device provisioning service. Not required when environment variable is set.")]
        public string ProvisioningConnectionString { get; set; }
    }
}
