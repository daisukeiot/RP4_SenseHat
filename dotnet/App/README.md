# SenseHat + RP4 + Dotnet Core Sample

This sample app connects to IoT Hub / IoT Central and :

- Sends Temperature and Humidity Sensor Data
- Receives Desired Property (Setting/Property in IoT Central) "isCelsius"  

  - isCelsius = True => Sends telemetry in Celsius
  - isCelsius = False => Sends telemetry in Fahrenheit

- Receives Command "displayMessage" to display string on LED

## Requirements

- Raspberry Pi3 or 4
- SenseHat
- Raspbian Buster
- Dotnet Core SDK 3.1.300

  - Tested with dotnet-sdk-3.1.300-linux-arm.tar.gz

- SenseHatNet  

  - To access SenseHat

## Setup

1. Flash Raspbian Buster
1. Set up OS with :

    ```bash
    sudo apt-get update && \
    sudo apt-get install -y git && \
    sudo raspi-config nonint do_expand_rootfs && \
    sudo raspi-config nonint do_memory_split 16 && \
    sudo raspi-config nonint do_spi 0 && \
    sudo raspi-config nonint do_i2c 0 && \
    sudo raspi-config nonint do_wifi_country US && \
    sudo raspi-config nonint do_change_locale en_US.UTF-8 && \
    sudo raspi-config nonint do_configure_keyboard us && \
    sudo raspi-config nonint do_change_timezone US/Pacific && \
    sudo reboot now

    ```

1. Download .Net Core SDK and expand to ~/dotnet with :  

    ```bash
    cd /tmp && \
    wget https://download.visualstudio.microsoft.com/download/pr/f2e1cb4a-0c70-49b6-871c-ebdea5ebf09d/acb1ea0c0dbaface9e19796083fe1a6b/dotnet-sdk-3.1.300-linux-arm.tar.gz
    mkdir -p $HOME/dotnet && \
    tar zxf dotnet-sdk-3.1.300-linux-arm.tar.gz -C $HOME/dotnet && \
    rm -rf dotnet-sdk-3.1.300-linux-arm.tar.gz && \
    echo export DOTNET_ROOT=$HOME/dotnet >> $HOME/.bashrc && \
    echo export PATH=$PATH:$HOME/dotnet >> $HOME/.bashrc
    ```

1. Clone the repo and copy library files from SenseHatNet with :

    ```bash
    cd ~/ && \
    git clone -b WIP https://github.com/daisukeiot/RP4_SenseHat.git && \
    cd /tmp && \
    git clone https://github.com/johannesegger/SenseHatNet.git && \
    cp SenseHatNet/Sense/Native/libRTIMULibWrapper.so $HOME/RP4_SenseHat/dotnet/App  && \
    cp SenseHatNet/Sense/Native/libRTIMULib.so.7 $HOME/RP4_SenseHat/dotnet/App  && \
    rm -rf SenseHatNet
    ```

1. Setup the app with :

    ```bash
    cd $HOME/RP4_SenseHat/dotnet/App && \
    export DOTNET_ROOT=$HOME/dotnet && \
    export PATH=$PATH:$HOME/dotnet && \
    dotnet restore && \
    dotnet publish -c release

    ```

1. Set environment variables  

    In IoT Central, Administration -> Device Connection => ID Scope and SAS Tokens

    ![IoTC00](media/IoTCentral_00.png)

    ```bash
    export DPS_IDSCOPE=<IS Scope from IoT Central>
    export SAS_KEY=<SAS Token from IoT Central>
    ```

1. Run the app with :

    ```bash
    cd $HOME/RP4_SenseHat/dotnet/App
    dotnet ./bin/release/netcoreapp3.1/publish/RP4SenseHat.csharp.dll
    ```
