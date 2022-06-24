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
        BuildProject provisioning\Samples\device "Provisioning Device Samples"
    }

    if ($run)
    {
        # Run provisioning\device samples

        # The pre-requisite listed here has been completed for this enrollment: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/previews/dpsClientCertificates/provisioning/Samples/device/SymmetricKeyWithOperationalCertificateSample
        RunApp provisioning\Samples\device\SymmetricKeyWithOperationalCertificateSample "Provisioning\Device\SymmetricKeyWithOperationalCertificateSample" "-s ""$env:DPS_IDSCOPE"" -i ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLMENT_REGISTRATION_ID"" -p ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLEMNT_PRIMARY_KEY"""

        # The pre-requisite listed here has been completed for this enrollment: https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/previews/dpsClientCertificates/provisioning/Samples/device/SymmetricKeyWithTrustBundleSample
        RunApp provisioning\Samples\device\SymmetricKeyWithTrustBundleSample "Provisioning\Device\SymmetricKeyWithTrustBundleSample" "-s ""$env:DPS_IDSCOPE"" -i ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLMENT_REGISTRATION_ID"" -p ""$env:DPS_SYMMETRIC_KEY_INDIVIDUAL_ENROLLEMNT_PRIMARY_KEY"""
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