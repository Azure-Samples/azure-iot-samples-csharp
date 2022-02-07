using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProvisioningAzureSasCredentialSample
{
    /// <summary>
    /// Parameters for the sample
    /// </summary>
    internal class Parameters
    {
        [Option(
            'h',
            "HostName",
            Required = true,
            HelpText = "The DPS host name. Ex: my-dps.azure-devices-provisioning.net")]
        public string HostName { get; set; }

        [Option(
            's',
            "SharedAccessKey",
            Required = true,
            HelpText = "The shared access key for connecting to DPS.")]
        public string SharedAccessKey { get; set; }

        [Option(
            'n'
            "SharedAccessKeyName",
            Required = true,
            HelpText = "The shared access key name for the access key supplied. Eg. provisioningserviceowner")]
        public string SharedAccessKeyName { get; set; }
    }
}
