using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class FileUploadSample
    {
        // The file to upload.
        private const string FilePath = "TestPayload.txt";
        private readonly DeviceClient _deviceClient;

        public FileUploadSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        public async Task RunSampleAsync()
        {
            using (var fileStreamSource = new FileStream(FilePath, FileMode.Open))
            {
                var fileName = Path.GetFileName(fileStreamSource.Name);

                Console.WriteLine("Uploading File: {0}", fileName);

                var watch = Stopwatch.StartNew();

                var sasUriReq = new Transport.FileUploadSasUriRequest() { BlobName = fileName };
                var sasUriRes = await _deviceClient.GetFileUploadSasUriAsync(sasUriReq);
                var isSuccess = false;

                try {
                    // Upload the file using BlobClient via HTTPS protocol, regardless of the DeviceClient protocol selection.
                    var blobClient = new BlobClient(sasUriRes.GetBlobUri());

                    var uploadRes = await blobClient.UploadAsync(fileStreamSource);
                    isSuccess = (uploadRes.GetRawResponse().Status == 201);
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e.Message);
                }
                finally
                {
                    // Notify IoT Hub of a completed file upload.
                    var uploadNotification = new Transport.FileUploadCompletionNotification() { CorrelationId = sasUriRes.CorrelationId, IsSuccess = isSuccess };
                    await _deviceClient.CompleteFileUploadAsync(uploadNotification);

                    watch.Stop();
                    Console.WriteLine("Time to upload file: {0}ms\n", watch.ElapsedMilliseconds);
                }

                // Explicitly close FileStream
                Console.WriteLine("FileStream Close.");
                fileStreamSource.Close();
            }
        }
    }
}
