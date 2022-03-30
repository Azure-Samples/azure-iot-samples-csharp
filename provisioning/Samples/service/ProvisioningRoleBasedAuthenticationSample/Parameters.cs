using CommandLine;

namespace ProvisioningRoleBasedAuthenticationSample
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
            't',
            "TenantId",
            Required = true,
            HelpText = "The Azure Active Directory tenant (directory) Id." +
            " This sample uses ClientSecretCredential. For other ways to use role based authentication, see https://docs.microsoft.com/dotnet/api/azure.identity?view=azure-dotnet.")]
        public string TenantId { get; set; }

        [Option(
            'c',
            "ClientId",
            Required = true,
            HelpText = "The client Id of the Azure Active Directory application." +
            " This sample uses ClientSecretCredential. For other ways to use role based authentication, see https://docs.microsoft.com/dotnet/api/azure.identity?view=azure-dotnet.")]
        public string ClientId { get; set; }

        [Option(
            's',
            "ClientSecret",
            Required = true,
            HelpText = "A client secret that was generated for the application Registration used to authenticate the client." +
            " This sample uses ClientSecretCredential. For other ways to use role based authentication, see https://docs.microsoft.com/dotnet/api/azure.identity?view=azure-dotnet.")]
        public string ClientSecret { get; set; }
    }
}
