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
        BuildProject iot-hub\Samples\service "IoTHub Service Samples"
        BuildProject provisioning\Samples\service "Provisioning Service Samples"
    }

    if ($run)
    {
        $sampleRunningTimeInSeconds = 60

        # Run the iot-hub\device samples
        $pnpDeviceSecurityType = "connectionString"
        RunApp iot-hub\Samples\device\ConventionBasedOperations\TemperatureController "IoTHub\Device\ConventionBasedOperations\TemperatureController" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_TC_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"
        RunApp iot-hub\Samples\device\ConventionBasedOperations\Thermostat "IoTHub\Device\ConventionBasedOperations\Thermostat" "--DeviceSecurityType $pnpDeviceSecurityType -p ""$env:PNP_THERMOSTAT_DEVICE_CONN_STRING"" -r $sampleRunningTimeInSeconds"

        # These samples are currently not added to the pipeilne run. The open items against them need to be addressed before they can be added to the pipeline run.

        # Tested manually:

        # Ignore: iot-hub\Samples\device\DeviceStreamingSample - requires the service-side counterpart to run.

        # Ignore: iot-hub\Samples\service\DeviceStreamingSample - requires the device-side counterpart to run.
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
