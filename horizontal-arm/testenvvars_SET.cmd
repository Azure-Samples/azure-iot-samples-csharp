
echo on
//set env variables. Fill in the fields below, then run this at the command level before running the NET Core app.
//Doing this instead of hardcoding them ensures that you don't accidentally save them to github.
//
SET IOT_DEVICE_ID=Contoso-Test-Device
SET IOT_HUB_URI="IOT-HUB-NAME-GOES-HERE.azure-devices-net";
SET IOT_DEVICE_KEY="IOT-DEVICE-KEY-GOES-HERE"
//
//to see local environment variables, type set and hit return, and it will show them all

//now run the app from the project folder

cd C:\_github\azure-iot-samples-csharp\horizontal-arm\arm-read-write

dotnet run C:\_github\azure-iot-samples-csharp\horizontal-arm\arm-read-write
