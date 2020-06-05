
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace RP4SenseHat.csharp
{
    class Program
    {
        // Main entry point.
        // 1. Provision the device to IoT Hub/IoT Central with DPS
        // 2. Receives IoT Hub Client class instance
        // 3. Calls IoT Hub Client Class instance to run the loop
        //
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");
        private static string s_sasKey  = Environment.GetEnvironmentVariable("SAS_KEY");

        private static string s_deviceId = "RaspberryPi4SenseHat";

        public static int Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(s_idScope) || string.IsNullOrWhiteSpace(s_sasKey))
            {
                Console.WriteLine("Please set following Environment Variables");
                Console.WriteLine("DPS_IDSCOPE : ID Scope");
                Console.WriteLine("SAS_KEY     : SAS Key");
                return 1;
            }

            Console.WriteLine($"DPS_IDSCOPE : {s_idScope}");
            Console.WriteLine($"SAS_KEY     : {s_sasKey}");

            // Create a new DPSClient to provision the device
            DPSClient dpsClient = new DPSClient(s_deviceId, s_idScope, s_sasKey);

            // Start provisioning, and receive IoT Hub Device Client
            IoTHubDeviceClient iotHubClient = dpsClient.ProvisionDeviceAsync().GetAwaiter().GetResult();

            // Run the IoT Hub Device Client
            iotHubClient.Run().GetAwaiter().GetResult();

            return 0;
        }
    }
}
