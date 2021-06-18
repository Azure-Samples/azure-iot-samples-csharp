# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET Samples build script.

.DESCRIPTION
Builds Azure IoT SDK samples.

Parameters:
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -build: Builds the project.
    -run: Runs the sample. The required environmental variables need to be set beforehand.
    -configuration {Debug|Release}
    -verbosity: Sets the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].

.EXAMPLE
.\build

Builds a Debug version of the SDK.
.EXAMPLE
.\build -config Release

Builds a Release version of the SDK.
.EXAMPLE
.\build -clean

.LINK
https://github.com/Azure-Samples/azure-iot-samples-csharp
https://github.com/azure/azure-iot-sdk-csharp

#>

Param(
    [switch] $clean,
    [switch] $build,
    [switch] $run,
    [string] $configuration = "Debug",
    [string] $verbosity = "q"
)

Function IsWindowsDevelopmentBox()
{
    return ([Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT)
}

Function BuildProject($path, $message) {

    $label = "BUILD: --- $message $configuration ---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    Set-Location (Join-Path $rootDir $path)

    if ($clean) {
        & dotnet clean --verbosity $verbosity --configuration $configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed: $label"
        }
    }

    & dotnet build --verbosity $verbosity --configuration $configuration

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed: $label"
    }
}

Function RunApp($path, $message, $params) {

    $label = "RUN: --- $message $configuration ($params)---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    Set-Location (Join-Path $rootDir $path)

    $runCommand = "dotnet run -- $params"
    Invoke-Expression $runCommand

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {
    if ($build)
    {
        BuildProject iot-hub\Samples\device "IoTHub Device Samples"
        BuildProject iot-hub\Samples\module "IoTHub Module Samples"
        BuildProject iot-hub\Samples\service "IoTHub Service Samples"
        BuildProject provisioning\Samples\device "Provisioning Device Samples"
        BuildProject provisioning\Samples\service "Provisioning Service Samples"
        BuildProject security\Samples "Security Samples"
    }

    if ($run)
    {
        $sampleRunningTimeInSeconds = 60

        # Run cleanup first so the samples don't get overloaded with old devices
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""

        # Run the iot-hub\device samples
        RunApp iot-hub\Samples\device\DeviceReconnectionSample "IoTHub\Device\DeviceReconnectionSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\FileUploadSample "IoTHub\Device\FileUploadSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        RunApp iot-hub\Samples\device\MessageReceiveSample "IoTHub\Device\MessageReceiveSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\MethodSample "IoTHub\Device\MethodSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\TwinSample "IoTHub\Device\TwinSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        $pnpDeviceSecurityType = "connectionString"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\TemperatureController "IoTHub\Device\PnpDeviceSamples\TemperatureController" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_TC_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\PnpDeviceSamples\Thermostat "IoTHub\Device\PnpDeviceSamples\Thermostat" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_THERMOSTAT_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        # Run the iot-hub\module sample
        RunApp iot-hub\Samples\module\ModuleSample "IoTHub\Module\ModuleSample" "-p ""$env:IOTHUB_MODULE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        # Run the iot-hub\service samples
        $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | Where-Object {$_ -like "DeviceId=*"}).Split("=")[1]
        $iothubHost = ($Env:IOTHUB_CONNECTION_STRING.Split(';') | Where-Object {$_ -like "HostName=*"}).Split("=")[1]

        RunApp iot-hub\Samples\service\AutomaticDeviceManagementSample "IoTHub\Service\AutomaticDeviceManagementSample"
        RunApp iot-hub\Samples\service\AzureSasCredentialAuthenticationSample "IoTHub\Service\AzureSasCredentialAuthenticationSample" "-r $iothubHost -d $deviceId -s ""$env:IOT_HUB_SAS_KEY"" -n ""$env:IOT_HUB_SAS_KEY_NAME"""
        RunApp iot-hub\Samples\service\EdgeDeploymentSample "IoTHub\Service\EdgeDeploymentSample"
        RunApp iot-hub\Samples\service\JobsSample "IoTHub\Service\JobsSample"
        RunApp iot-hub\Samples\service\RegistryManagerSample "IoTHub\Service\RegistryManagerSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -p ""$env:IOTHUB_PFX_X509_THUMBPRINT"""

        Write-Warning "Using device $deviceId for the ServiceClientSample."
        RunApp iot-hub\Samples\service\ServiceClientSample "IoTHub\Service\ServiceClientSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -d $deviceId -r $sampleRunningTimeInSeconds"

        # Run provisioning\device samples
        #RunApp provisioning\Samples\device\ComputeDerivedSymmetricKeySample
        #RunApp provisioning\Samples\device\SymmetricKeySample
        #RunApp provisioning\Samples\device\TpmSample
        #RunApp provisioning\Samples\device\X509Sample

        # Run provisioning\service samples
        RunApp provisioning\Samples\service\BulkOperationSample "Provisioning\Service\BulkOperationSample"
        RunApp provisioning\Samples\service\EnrollmentSample "Provisioning\Service\EnrollmentSample"

        # Run the cleanup again so that identities and enrollments created for the samples are cleaned up.
        RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:FAR_AWAY_IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""

        # These samples are currently not added to the pipeilne run. The open items against them need to be addressed before they can be added to the pipeline run.

        # Ignore: DeviceStreaming creates a bi-directional tunnel to enable client-server communication. These device and service samples need to be merged so that they have both the device and service components.
        # DeviceStreaming is available in Central US EUAP, so we can run this against our preview pipelines.
        # For more details on regional availability of device streams, see https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview#regional-availability.
        # RunApp iot-hub\Samples\device\DeviceStreamingSample "IoTHub\Device\DeviceStreamingSample" "-p ""$env:IOTHUB_DEVICE_CONN_STRING"""
        # $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | where {$_ -like "DeviceId*"}).Split("=")[1]
        # Write-Warning "Using device $deviceId for the DeviceStreamingSample."
        # RunApp iot-hub\Samples\service\DeviceStreamingSample "IoTHub\Service\DeviceStreamingSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -d $deviceId"

        # Ignore: X509DeviceCertWithChainSample has some pre-requisites associated. Need to make sure that they can run unattended.

        # Ignore: ImportExportDevicesSample needs to be refactored to accept command-line parameters.
        
        # Ignore: ImportExportDevicesWithManagedIdentitySample has some pre-requisites associated. Need to ensure the managed identity can access the storage accounts.

        # Ignore: RoleBasedAuthenticationSample requires an AAD app to be set up.

        # Ignore: EnrollmentGroupSample needs to be refactored to accept command-line parameters.
        
        # Ignore: DigitalTwinClientSamples requires device-side counterpart to run.
        # Ignore: PnpServiceSamples requires device-side counterpart to run.
    }

    $buildFailed = $false
}
catch [Exception]{
    $buildFailed = $true
    $errorMessage = $Error[0]
}
finally {
    Set-Location $rootDir
    $endTime = Get-Date
}

Write-Host
Write-Host

Write-Host ("Time Elapsed {0:c}" -f ($endTime - $startTime))

if ($buildFailed) {
    Write-Host -ForegroundColor Red "Build failed ($errorMessage)"
    exit 1
}
else {
    Write-Host -ForegroundColor Green "Build succeeded."
    exit 0
}
