namespace FilterModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Newtonsoft.Json;

    class MessageBody
    {
        public double humidity { get; set; }
        public double tempC { get; set; }
        public double tempF { get; set; }

        public MessageBody()
        {
            humidity = 0.0;
            tempC = 0.0;
            tempF = 0.0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("TelemetryInput", ProcessTelemetry, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> ProcessTelemetry(Message message, object userContext)
        {
            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string s_MessageIn = Encoding.UTF8.GetString(messageBytes);
            Console.WriteLine($"Message In  : {s_MessageIn}");

            if (!string.IsNullOrEmpty(s_MessageIn))
            {
                var messageIn = JsonConvert.DeserializeObject<MessageBody>(s_MessageIn);
                var messageOut = new MessageBody();
                string s_MessageOut = null;

                if (messageIn.tempF == 0)
                {
                    // add Fahrenheit
                    double fahrenheit = (messageIn.tempC * 9 / 5) + 32;

                    s_MessageOut = $"{{\"humidity\":{messageIn.humidity:F2},\"tempC\":{messageIn.tempC:F2},\"tempF\":{fahrenheit:F2}}}";
                }
                else if (messageIn.tempC == 0)
                {
                    // add Fahrenheit
                    double celsius = (messageIn.tempF - 32) * 5 / 9;

                    s_MessageOut = $"{{\"humidity\":{messageIn.humidity:F2},\"tempC\":{celsius:F2},\"tempF\":{messageIn.tempF:F2}}}";
                }

                using (var telemetryMessage = new Message(Encoding.UTF8.GetBytes(s_MessageOut)))
                {
                    Console.WriteLine($"Message Out : {s_MessageOut}");
                    await moduleClient.SendEventAsync("TelemetryOutput", telemetryMessage);
                }
            }

            return MessageResponse.Completed;
        }
    }
}
