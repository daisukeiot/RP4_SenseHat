using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sense.RTIMU;
using Newtonsoft.Json;

namespace RP4SenseHat.csharp
{
    public class IoTHubDeviceClient
    {
        // A wrapper for Azure Device Client class.
        // 1. Connects to IoT Hub/IoT Central using authentication information from DPS
        // 2. Set up callback functions for connection status change, Desired Property change (Settings from Cloud), and commands
        // 3. Reads sensor data from SenseHat
        // 4. Sends telemetry to IoT Hub/IoT Central
        //
      
        //https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.client.deviceclient?view=azure-dotnet
        private DeviceClient _client;

        private IAuthenticationMethod _authenticationMethod;
        private string _iotHub;
        private bool _bCelsius = true;
        private bool _hasSenseHat = true;
        static readonly Random Rnd = new Random();

        public IoTHubDeviceClient(string iothub, IAuthenticationMethod authenticationMethod)
        {
            _iotHub = iothub;
            _authenticationMethod = authenticationMethod;
            _bCelsius = true;
            Console.WriteLine($"SenseHat Device Client : Has SenseHat : {_hasSenseHat}");
        }

        public async Task Initialize()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
            {
                //CleanSession = true,
                // set proxy etc
            };
            ITransportSettings[] settings = { mqttSetting };
            _client = DeviceClient.Create(_iotHub, _authenticationMethod, settings);
            _client.ProductInfo = "IoTHubClientSample";
            await _client.OpenAsync();
        }

        public async Task Run()
        {
            // runs loop to send telemetry

            // set up a callback for connection status change
            _client.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // set up callback for Desired Property update (Settings/Properties in IoT Central)
            await _client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null).ConfigureAwait(false);

            // set up callback for Methond (Command in IoT Central)
            await _client.SetMethodHandlerAsync("displayMessage", DisplayMessage, null).ConfigureAwait(false);

            Twin twin = await _client.GetTwinAsync().ConfigureAwait(false);
            Console.WriteLine("\r\nDevice Twin received:");
            Console.WriteLine($"{twin.ToJson(Newtonsoft.Json.Formatting.Indented)}");

            if (twin.Properties.Desired.Contains("isCelsius"))
            {
                _bCelsius = twin.Properties.Desired["isCelsius"]["value"];
            }

            if (_hasSenseHat)
            {
                // read sensor data from SenseHat
                using (var settings = RTIMUSettings.CreateDefault())
                using (var imu = settings.CreateIMU())
                using (var humidity = settings.CreateHumidity())
                using (var pressure = settings.CreatePressure())
                {
                    while (true)
                    {
                        var imuData = imu.GetData();
                        var humidityReadResult = humidity.Read();

                        if (humidityReadResult.HumidityValid && humidityReadResult.TemperatureValid)
                        {
                            string buffer;
                            // format telemetry based on settings from Cloud
                            if (_bCelsius)
                            {
                                buffer = $"{{\"humidity\":{humidityReadResult.Humidity:F2},\"tempC\":{humidityReadResult.Temperatur:F2}}}";
                            } else
                            {
                                double fahrenheit = (humidityReadResult.Temperatur * 9 / 5) + 32;
                                buffer = $"{{\"humidity\":{humidityReadResult.Humidity:F2},\"tempF\":{fahrenheit:F2}}}";
                            }

                            using (var telemetryMessage = new Message(Encoding.UTF8.GetBytes(buffer)))
                            {
                                Console.WriteLine($"Message Out : [{buffer}]");
                                await _client.SendEventAsync(telemetryMessage).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Err : Sensor data not valid");
                        }

                        await Task.Delay(3 * 1000);
                    }
                }
            } else {
                // simulate sensor date
                string buffer;
                var simulatorData = new SimulatorData
                {
                    TempCMax = 36,
                    HumidityMax = 100,
                    TempC = 30,
                    Humidity = 40,
                };

                while (true)
                {
                    if (simulatorData.TempC > simulatorData.TempCMax)
                    {
                        simulatorData.TempC += Rnd.NextDouble() - 0.5; // add value between [-0.5..0.5]
                    }
                    else
                    {
                        simulatorData.TempC += -0.25 + (Rnd.NextDouble() * 1.5); // add value between [-0.25..1.25] - average +0.5
                    }

                    if (simulatorData.Humidity > simulatorData.HumidityMax)
                    {
                        simulatorData.Humidity += Rnd.NextDouble() - 0.5; // add value between [-0.5..0.5]
                    }
                    else
                    {
                        simulatorData.Humidity += -0.25 + (Rnd.NextDouble() * 1.5); // add value between [-0.25..1.25] - average +0.5
                    }

                    // format telemetry based on settings from Cloud
                    if (_bCelsius)
                    {
                        buffer = $"{{\"humidity\":{simulatorData.Humidity:F2},\"tempC\":{simulatorData.TempC:F2}}}";
                    } else
                    {
                        double fahrenheit = (simulatorData.TempC * 9 / 5) + 32;
                        buffer = $"{{\"humidity\":{simulatorData.Humidity:F2},\"tempF\":{fahrenheit:F2}}}";
                    }

                    using (var telemetryMessage = new Message(Encoding.UTF8.GetBytes(buffer)))
                    {
                        Console.WriteLine(buffer);
                        await _client.SendEventAsync(telemetryMessage).ConfigureAwait(false);
                    }

                    await Task.Delay(3 * 1000);
                }
            }        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            // callback when connection to IoT Hub/IoT Central changed
            Console.WriteLine();
            Console.WriteLine($"Connection status changed to {status}.");
            Console.WriteLine($"Connection status changed reason is {reason}.");
            Console.WriteLine();
        }
        
        private Task<MethodResponse> DisplayMessage(MethodRequest methodRequest, object userContext)
        {
            // callback when a command is sent from cloud
            Console.WriteLine($"\r\n*** {methodRequest.Name} was called.");
            Console.WriteLine("{0}", methodRequest.DataAsJson);

            // display on SenseHat LED
            if (_hasSenseHat)
            {
                Sense.Led.LedMatrix.ShowMessage(methodRequest.DataAsJson.Replace("\"", string.Empty).Trim());
            }

            // return response to IoT Hub/IoT Central
            return Task.FromResult(new MethodResponse(new byte[0], 200));
        }

        private async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
        {
            // callback when Desired Property (settings) is changed/updated by Cloud/Backend
            Console.WriteLine("\r\nDesired property (Settings) changed:");
            Console.WriteLine($"{desiredProperties.ToJson(Newtonsoft.Json.Formatting.Indented)}");


            // IoT Central expects the following payloads in Reported Property (as a response and communicate synchronization status) 
            _bCelsius = desiredProperties["isCelsius"]["value"];

            TwinCollection twinValue = new TwinCollection();
            twinValue["value"] = _bCelsius;
            twinValue["desiredVersion"] = desiredProperties["$version"];
            twinValue["statusCode"] = 200;
            twinValue["status"] = "completed";

            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["isCelsius"] = twinValue;

            await _client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }

        internal class SimulatorData
        {
            public double TempCMin { get; set; }
            public double TempCMax { get; set; }
            public double HumidityMin { get; set; }
            public double HumidityMax { get; set; }
            public double TempC { get; set; }
            public double Humidity { get; set; }
        }
    }
}
