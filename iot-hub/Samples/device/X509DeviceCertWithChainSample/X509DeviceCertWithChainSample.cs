using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace X509DeviceCertWithChainSample
{
    public class X509DeviceCertWithChainSample
    {
        private readonly DeviceClient _deviceClient;

        public X509DeviceCertWithChainSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        }

        public async Task RunSampleAsync()
        {
            await _deviceClient.OpenAsync();
            await _deviceClient.CloseAsync();
        }
    }
}
