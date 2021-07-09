using System;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceReconnectionSampleException : ApplicationException
    {
        public DeviceReconnectionSampleException(string message)
            : base(message)
        {

        }
    }
}
