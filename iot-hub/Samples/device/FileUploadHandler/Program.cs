using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FileUploadHandler
{
    class Program
    {
        static ServiceClient _serviceClient = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            try
            {
                string connectionString = "HostName=azadAttemp2-hub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FpDX9KNs3V/Hdp+wv7093wIgHFSqTObiwRDdSD+VTF0=";
                Console.WriteLine("Receive file upload notifications\n");
                _serviceClient = ServiceClient.CreateFromConnectionString(connectionString, TransportType.Amqp_WebSocket_Only);

                await ReceiveFileUploadNotificationAsync();
                await _serviceClient.CloseAsync();
                Console.WriteLine("Press Enter to exit\n");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                await _serviceClient?.CloseAsync();
                Console.WriteLine(e);
            }
        }

        private static async Task ReceiveFileUploadNotificationAsync()
        {
            var notificationReceiver = _serviceClient.GetFileNotificationReceiver();
            Console.WriteLine("\nReceiving file upload notification from service");
            int totalDownloadedFile = 1;
            var enqueTimeList = new List<double>();

            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var fileUploadNotification = await notificationReceiver.ReceiveAsync(TimeSpan.FromSeconds(2));
                if (fileUploadNotification == null)
                {
                    Console.WriteLine("Didn't find any notification, waiting 2 seconds");
                    await Task.Delay(2000);
                    continue;
                }

                var totalSeconds = DateTime.UtcNow - fileUploadNotification.EnqueuedTimeUtc;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Number of notification recieved :{totalDownloadedFile++}, TimeTaken : {stopwatch.ElapsedMilliseconds} , FileName : {fileUploadNotification.BlobName} ," +
                    $" DeviceId : {fileUploadNotification}");
                Console.ResetColor();
                await notificationReceiver.CompleteAsync(fileUploadNotification);
                enqueTimeList.Add(totalSeconds.TotalSeconds);
            }
        }
    }
}
