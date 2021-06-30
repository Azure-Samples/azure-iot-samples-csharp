---
page_type: sample
description: "A set of easy-to-understand, continuously-tested samples for connecting to Azure IoT Hub."
languages:
- csharp
products:
- azure
- azure-iot-hub
- dotnet
urlFragment: azure-iot-samples-for-csharp-net
---

# Azure IoT Samples for C# (.NET)

azure-iot-samples-csharp provides a set of easy-to-understand, continuously-tested samples for connecting to Azure IoT Hub via Azure/azure-iot-sdk-csharp.

These samples are against the `preview` releases of the Azure IoT Hub SDK for .NET. For samples against the GA releases, see [here](https://github.com/Azure-Samples/azure-iot-samples-csharp).

> NOTES: 
> - Device streaming feature is not being included in our newer preview releases as there is no active development going on in the service. For more details on the feature, see [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview).
>  
>   This feature has not been included in any preview release after [2020-10-14](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/preview_2020-10-14). However, the feature is still available under [previews/deviceStreaming](https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/deviceStreaming) branch.  
>  
>   The latest preview nuget versions that contain the feature are:  
>   Microsoft.Azure.Devices.Client - 1.32.0-preview-001  
>   Microsoft.Azure.Devices - 1.28.0-preview-001
>
>   - [Device streaming device sample](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/preview/iot-hub/Samples/device/DeviceStreamingSample)
>   - [Device streaming service sample](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/preview/iot-hub/Samples/service/DeviceStreamingSample)
>
> - The `preview` branch is meant to hold only those samples that are relevant to the corresponding `preview` feature added in the SDK. Once the `preview` feature is GA'd, these samples will be moved over to the main branch. This branch is not intended to be directly merged into the main branch.
>
> - It is not recommended to take dependency on preview nugets for production applications as breaking changes can be introduced in preview nugets.

## Prerequisites

- .NET 5.0 on your development machine.  You can download the .NET 5.0 SDK for multiple platforms from [.NET](https://dotnet.microsoft.com/download/dotnet/5.0).  You can verify the current version of .NET on your development machine using 'dotnet --version'.

## Resources

- [azure-iot-sdk-csharp](https://github.com/Azure/azure-iot-sdk-csharp/tree/preview): contains the `preview` source code for Azure IoT Hub SDK for .NET
- [Azure IoT Hub Documentation](https://docs.microsoft.com/azure/iot-hub/)
- [Get-started](https://docs.microsoft.com/azure/iot-hub/quickstart-send-telemetry-dotnet)
