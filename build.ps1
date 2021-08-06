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

    Write-Host $runCommand
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
        BuildProject iot-hub\Samples\service "IoTHub Service Samples"
    }

    if ($run)
    {
        RunApp iot-hub\Samples\service\CleanupDevicesSample "IoTHub\Service\CleanupDevicesSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -a ""$env:STORAGE_ACCOUNT_CONNECTION_STRING"" --PathToDevicePrefixForDeletion ""$env:PATH_TO_DEVICE_PREFIX_FOR_DELETION_FILE"""

        # Run the iot-hub\service samples
        $deviceId = ($Env:IOTHUB_DEVICE_CONN_STRING.Split(';') | Where-Object {$_ -like "DeviceId=*"}).Split("=")[1]
        $iothubHost = ($Env:IOTHUB_CONNECTION_STRING.Split(';') | Where-Object {$_ -like "HostName=*"}).Split("=")[1]

        Write-Warning "Using device $deviceId for the AzureSasCredentialAuthenticationSample."
        RunApp iot-hub\Samples\service\AzureSasCredentialAuthenticationSample "IoTHub\Service\AzureSasCredentialAuthenticationSample" "-r $iothubHost -d $deviceId -s ""$env:IOT_HUB_SAS_KEY"" -n ""$env:IOT_HUB_SAS_KEY_NAME"""
        
        Write-Warning "Using device $deviceId for the RoleBasedAuthenticationSample."
        RunApp iot-hub\Samples\service\RoleBasedAuthenticationSample "IoTHub\Service\RoleBasedAuthenticationSample" "-h $iothubHost -d $deviceId --ClientId ""$env:IOTHUB_CLIENT_ID"" --TenantId ""$env:MSFT_TENANT_ID"" --ClientSecret ""$env:IOTHUB_CLIENT_SECRET"""

        Write-Warning "Using device $deviceId for the ServiceClientSample."
        RunApp iot-hub\Samples\service\ServiceClientSample "IoTHub\Service\ServiceClientSample" "-c ""$env:IOTHUB_CONNECTION_STRING"" -d $deviceId -r $sampleRunningTimeInSeconds"
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
