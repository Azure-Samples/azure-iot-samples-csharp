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

## Prerequisites

- .NET Core SDK 3.0.0 or greater on your development machine.  You can download the .NET Core SDK for multiple platforms from [.NET](https://www.microsoft.com/net/download/all).  You can verify the current version of C# on your development machine using 'dotnet --version'.

  **Note:** The samples can be compiled using the NET Core SDK 2.1 SDK if the language version of projects using C# 8 features are changed to `preview`.

## Preview features

Samples showing how to use the various preview features of Microsoft Azure IoT .NET SDK are present [here](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/preview).

> Note: 
> Device streaming feature is not being included in our newer preview releases as there is no active development going on in the service. For more details on the feature, see [here](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview).
>  
> Device streaming feature has not been included in any preview release after [2020-10-14](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/preview_2020-10-14). However, the feature is still available under [previews/deviceStreaming](https://github.com/Azure/azure-iot-sdk-csharp/tree/previews/deviceStreaming) branch.  
>  
> The latest preview nuget versions that contain the feature are:  
Microsoft.Azure.Devices.Client - 1.32.0-preview-001  
Microsoft.Azure.Devices - 1.28.0-preview-001
>
> - [Device streaming device sample](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/preview/iot-hub/Samples/device/DeviceStreamingSample)
> - [Device streaming service sample](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/preview/iot-hub/Samples/service/DeviceStreamingSample)

> It is not recommended to take dependency on preview nugets for production applications as breaking changes can be introduced in preview nugets.

## Resources

- [azure-iot-sdk-csharp](https://github.com/Azure/azure-iot-sdk-csharp): contains the source code for Azure IoT C# SDK.
- [Azure IoT Hub Documentation](https://docs.microsoft.com/azure/iot-hub/)
- [Get-started](https://docs.microsoft.com/azure/iot-hub/quickstart-send-telemetry-dotnet)
