# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#

.SYNOPSIS
Microsoft Azure IoT SDK .NET Samples build script.

.DESCRIPTION
Builds Azure IoT SDK samples.

Parameters:
    -clean: Runs dotnet clean. Use `git clean -xdf` if this is not sufficient.
    -nobuild: Skips build step (use if re-running tests after a successful build).
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
    [switch] $nobuild,
    [switch] $norun,
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
    cd (Join-Path $rootDir $path)

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

Function RunApp($path, $message, $args) {

    $label = "RUN: --- $message $configuration ($args)---"

    Write-Host
    Write-Host -ForegroundColor Cyan $label
    cd (Join-Path $rootDir $path)

    & dotnet run $args

    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed: $label"
    }
}

$rootDir = (Get-Item -Path ".\" -Verbose).FullName
$startTime = Get-Date
$buildFailed = $true
$errorMessage = ""

try {
    if (-not $nobuild)
    {
        BuildProject iot-hub\Samples\device "IoTHub Device Samples"
        BuildProject iot-hub\Samples\module "IoTHub Module Samples"
        BuildProject iot-hub\Samples\service "IoTHub Service Samples"
        BuildProject iot-hub\Quickstarts "IoTHub Device Quickstarts"
        BuildProject iot-hub\Tutorials\Routing "IoTHub Tutorials - Routing"
        BuildProject provisioning\Samples\device "Provisioning Device Samples"
        BuildProject provisioning\Samples\service "Provisioning Service Samples"
        BuildProject security\Samples "Security Samples"
    }

    if (-not $norun)
    {
        RunApp iot-hub\Samples\device\FileUploadSample "IoTHub\Device\FileUploadSample"
        RunApp iot-hub\Samples\device\KeysRolloverSample "IoTHub\Device\KeysRolloverSample"
        RunApp iot-hub\Samples\device\MessageSample "IoTHub\Device\MessageSample"
        RunApp iot-hub\Samples\device\MethodSample "IoTHub\Device\MethodSample"
        RunApp iot-hub\Samples\device\TwinSample "IoTHub\Device\TwinSample"

        # TODO #10: Add module configuration in Jenkins.
        #RunApp iot-hub\Samples\module\MessageSample "IoTHub\Module\MessageSample"
        #RunApp iot-hub\Samples\module\MethodSample "IoTHub\Module\MethodSample"

        # TODO: Modify registry manager device deletion to delete devices in bulk
        #RunApp iot-hub\Samples\service\CleanUpDevicesSample "IoTHub\Service\CleanUpDevicesSample"

        RunApp iot-hub\Samples\service\AutomaticDeviceManagementSample "IoTHub\Service\AutomaticDeviceManagementSample"
        RunApp iot-hub\Samples\service\JobsSample "IoTHub\Service\JobsSample"
        RunApp iot-hub\Samples\service\RegistryManagerSample "IoTHub\Service\RegistryManagerSample"

        $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | where {$_ -like "DeviceId*"}).Split("=")[1]
        Write-Warning $deviceId
        RunApp iot-hub\Samples\service\ServiceClientSample "IoTHub\Service\ServiceClientSample" - $deviceId

        # TODO #11: Modify Provisioning\device samples to run unattended.

        # TODO: Modify bulk enrollment operation to take in more device per operation
        #RunApp provisioning\Samples\service\CleanupEnrollmentsSample "Provisioning\Service\CleanupEnrollmentsSample"

        RunApp provisioning\Samples\service\BulkOperationSample "Provisioning\Service\BulkOperationSample"
        # TODO #11 :RunApp provisioning\Samples\service\EnrollmentGroupSample "Provisioning\Service\EnrollmentGroupSample"
        RunApp provisioning\Samples\service\EnrollmentSample "Provisioning\Service\EnrollmentSample"
    }

    $buildFailed = $false
}
catch [Exception]{
    $buildFailed = $true
    $errorMessage = $Error[0]
}
finally {
    cd $rootDir
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
