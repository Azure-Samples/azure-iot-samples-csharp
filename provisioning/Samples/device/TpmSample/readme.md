# Provisioning Device Client Sample - TPM Attestation
The sample that previously existed here has been removed since this sample repository will be retired and archived.

Please see here for the up to date [TpmSample](https://github.com/Azure/azure-iot-sdk-csharp/tree/main/provisioning/device/samples/How%20To/TpmSample).
## Overview

This is a quick tutorial with the steps to register a device in the Microsoft Azure IoT Hub Device Provisioning Service using the Trusted Platform Module (TPM) attestation.

## How to run the sample

1. Ensure that all prerequisite steps presented in [samples](../) have been performed.
1. In order to access the Hardware Security Module (HSM), the application must be run in administrative mode, so open VS as an admin, or open a console window as an admin.
1. You'll need the endorsement key (EK) of your TPM device to create an individual enrollment. This sample has a parameter `--GetTpmEndorsementKey` that can be used to get it and print it to the console.
1. If using a console, enter: `dotnet run -s <IdScope> -r <RegistrationId>`.
1. If using VS, edit project properties | debug | application arguments and add the parameters: `-s <IdScope> -r <RegistrationId>`

> Replace `IdScope` with the value found within the Device Provisioning Service Overview tab, and `RegistrationId` with the individual enrollment registration Id.
> To see a full list of parameters, run `dotnet run -?`.

Continue by following the instructions presented by the sample.
