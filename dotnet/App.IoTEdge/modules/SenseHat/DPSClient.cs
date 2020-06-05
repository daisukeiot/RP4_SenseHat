using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RP4SenseHat.csharp
{
    //
    // Provision the device to IoT Central/IoT Hub with Device Provisioning Service (DPS).
    // Example with Symmetric Key authentication
    //
    // 1. Generates Sysmetric Key
    // 2. Provision through DPS
    // 3. Pass Authentication information to IoT Hub Client module
    //
    public class DPSClient
    {
        private const string s_global_endpoint = "global.azure-devices-provisioning.net";
        private static string s_idScope = Environment.GetEnvironmentVariable("DPS_IDSCOPE");
        private static string s_sasKey  = Environment.GetEnvironmentVariable("SAS_KEY");
        private static string _deviceId;
        private ProvisioningDeviceClient _provClient;

        public DPSClient(string s_deviceId)
        {
            _deviceId = s_deviceId;
            _provClient = null;
        }

        public async Task<IoTHubDeviceClient> ProvisionDeviceAsync()
        {
            string connectionKey;
            IoTHubDeviceClient iotHubClient = null;

            Console.WriteLine("Provisioning...");

            if (!String.IsNullOrEmpty(_deviceId) && !String.IsNullOrEmpty(s_sasKey) && !String.IsNullOrEmpty(s_idScope))
            {
                connectionKey = GenerateSymmetricKey();
                Console.WriteLine($"Connection Key : {connectionKey}");

                using (var securityProvider = new SecurityProviderSymmetricKey(_deviceId, connectionKey, null))
                using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
                {
                    _provClient = ProvisioningDeviceClient.Create(s_global_endpoint, s_idScope, securityProvider, transport);

                    // Sanity check
                    Console.WriteLine($"Device ID      : {securityProvider.GetRegistrationID()}");

                    DeviceRegistrationResult result = await _provClient.RegisterAsync().ConfigureAwait(false);

                    if (result.Status == ProvisioningRegistrationStatusType.Assigned)
                    {
                        Console.WriteLine($"Provisioned    : {result.Status}");
                        Console.WriteLine($"  Device ID    : {result.DeviceId}");
                        Console.WriteLine($"  IoT Hub      : {result.AssignedHub}");

                        IAuthenticationMethod authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, securityProvider.GetPrimaryKey());

                        iotHubClient = new IoTHubDeviceClient(result.AssignedHub, authenticationMethod);
                    }
                    else
                    {
                        Console.WriteLine($"Err : Provisioning Failed {result.Status}");
                    }
                }
            } else {
                Console.WriteLine("Please set following Environment Variables");
                Console.WriteLine("DPS_IDSCOPE : ID Scope");
                Console.WriteLine("SAS_KEY     : SAS Key");
            }

            return iotHubClient;
        }

        //
        // Genetate Symmetric Key with SAS Key and Device Id
        //
        private static string GenerateSymmetricKey()
        {
            using (var hmac = new HMACSHA256(Convert.FromBase64String(s_sasKey)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(_deviceId)));
            }
        }
    }
}
