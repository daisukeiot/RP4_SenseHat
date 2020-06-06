
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Collections;

namespace RP4SenseHat.csharp
{
    class Program
    {
        // Main entry point.
        // 1. Provision the device to IoT Hub/IoT Central with DPS
        // 2. Receives IoT Hub Client class instance
        // 3. Calls IoT Hub Client Class instance to run the loop
        //

        private static string s_deviceId = "RaspberryPi4SenseHat";

        public static int Main(string[] args)
        {
           foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                Console.WriteLine("  {0} = {1}", de.Key, de.Value);

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
            {
                // Create a new DPSClient to provision the device
                DPSClient dpsClient = new DPSClient(s_deviceId);

                // Start provisioning, and receive IoT Hub Device Client
                IoTHubDeviceClient iotHubClient = dpsClient.ProvisionDeviceAsync().GetAwaiter().GetResult();

                if (iotHubClient != null)
                {
                    iotHubClient.Initialize().Wait();

                    // Run the IoT Hub Device Client
                    iotHubClient.Run().GetAwaiter().GetResult();
                }
            } else {
                // Device Authentication is done by IoT Edge Runtime so we do not need to process device provisioning for IoT Edge Module
                // IoT Edge Module
                IoTEdgeModuleClient iotEdgeModuleClient = new IoTEdgeModuleClient();

                iotEdgeModuleClient.Initialize().Wait();

                iotEdgeModuleClient.Run().GetAwaiter().GetResult();
            }
            return 0;
        }
    }
}
