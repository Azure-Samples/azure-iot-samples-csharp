// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// An X509Certificate2 helper class for generating self-signed and CA-signed certificates.
    /// This class uses openssl for certificate generation since <see cref="X509Certificate2"/> class currently doesn't have certificate generation APIs.
    /// </summary>
    internal static class X509Certificate2Helper
    {
        internal static void GenerateCertificateSigningRequestFiles(string subject, DirectoryInfo destinationCertificateFolder, ILogger logger)
        {
            string keyFile = Path.Combine(destinationCertificateFolder.FullName, $"{subject}.key");
            string csrFile = Path.Combine(destinationCertificateFolder.FullName, $"{subject}.csr");

            // Generate the private key for the certificate
            logger.LogInformation($"Generating the private key for the certificate with subject {subject} using ...\n");
            string keyGen = $"genpkey" +
                $" -out \"{keyFile}\"" +
                $" -algorithm RSA" +
                $" -pkeyopt rsa_keygen_bits:2048";

            logger.LogInformation($"openssl {keyGen}\n");
            using Process keyGenCmdProcess = CreateErrorObservantProcess("openssl", keyGen);
            keyGenCmdProcess.Start();
            keyGenCmdProcess.WaitForExit();

            if (keyGenCmdProcess.ExitCode != 0)
            {
                logger.LogError($"\"{keyGen}\" exited with error {keyGenCmdProcess.StandardError.ReadToEnd()}.");
                return;
            }

            // Generate the certificate signing request for the certificate
            logger.LogInformation($"Generating the certificate signing request for the certificate with subject {subject} using ...\n");
            string csrGen = $"req" +
                $" -new" +
                $" -subj /CN={subject}" +
                $" -key \"{keyFile}\"" +
                $" -out \"{csrFile}\"";

            logger.LogInformation($"openssl {csrGen}\n");
            using Process csrGenCmdProcess = CreateErrorObservantProcess("openssl", csrGen);
            csrGenCmdProcess.Start();
            csrGenCmdProcess.WaitForExit();

            if (csrGenCmdProcess.ExitCode != 0)
            {
                logger.LogError($"\"{csrGen}\" exited with error {csrGenCmdProcess.StandardError.ReadToEnd()}.");
                return;
            }
        }

        internal static void GeneratePfxFromPublicCertificateAndPrivateKey(string subject, DirectoryInfo destinationCertificateFolder, ILogger logger)
        {
            string keyFile = Path.Combine(destinationCertificateFolder.FullName, $"{subject}.key");
            string cerFile = Path.Combine(destinationCertificateFolder.FullName, $"{subject}.cer");
            string pfxFile = Path.Combine(destinationCertificateFolder.FullName, $"{subject}.pfx");

            // Generate the pfx file containing both public certificate and private key information
            logger.LogInformation($"Generating {subject}.pfx file using ...\n");
            string pfxGen = $"pkcs12" +
                $" -export" +
                $" -in \"{cerFile}\"" +
                $" -inkey \"{keyFile}\"" +
                $" -out \"{pfxFile}\"" +
                $" -passout pass:";

            logger.LogInformation($"openssl {pfxGen}\n");
            using Process pfxGenCmdProcess = CreateErrorObservantProcess("openssl", pfxGen);
            pfxGenCmdProcess.Start();
            pfxGenCmdProcess.WaitForExit();

            if (pfxGenCmdProcess.ExitCode != 0)
            {
                logger.LogError($"\"{pfxGen}\" exited with error {pfxGenCmdProcess.StandardError.ReadToEnd()}.");
                return;
            }
        }

        internal static X509Certificate2 CreateX509Certificate2FromPfxFile(string subjectName, DirectoryInfo certificateFolder)
        {
            return new X509Certificate2(Path.Combine(certificateFolder.FullName, $"{subjectName}.pfx"));
        }

        private static Process CreateErrorObservantProcess(string processName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo(processName, arguments)
            {
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return new Process
            {
                StartInfo = processStartInfo
            };
        }
    }
}
