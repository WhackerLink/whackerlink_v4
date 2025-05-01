# WhackerLink V4 Installation and Setup Guide

[![License](https://img.shields.io/badge/License-AGPLv3-blue?style=for-the-badge)](https://www.gnu.org/licenses/agpl-3.0)

## Windows Quick Setup

### Quick Start Guide
**WILL UPDATE SOON**
If you encounter issues, seek assistance in the Discord community or refer to the manual compilation steps below.

---

## Windows Build from Source
### Installing Git
1. Download Git from [this link](https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe).
2. Run the downloaded installer. Follow the prompts, leaving all options at their default settings.

### Installing Build Tools
**OUT OF DATE WILL UPDATE SOON**
1. Download Visual Studio Build Tools from [here](https://aka.ms/vs/17/release/vs_BuildTools.exe).
2. Execute `vs_BuildTools.exe` to start the setup.
3. In the **Workloads** section, select:
    - .NET desktop build tools
    - Desktop development with C++
4. Move to the **Individual components** tab and ensure the following are selected:
    - .NET 8.0 (May already be selected)
    - .NET 6.0 WebAssembly Build Tools
    - .NET 6.0 Runtime
    - .NET 3.1 Runtime
    - C++/CLI support for v143 build tools (14.32-17.2)
5. Select **Install while downloading** option and click **Install**.
6. Wait for the installation process to complete.

### Building WhackerLink
1. Open the **Developer Command Prompt for Visual Studio 2022**. You can find it in the recently added apps after installation.
2. Navigate to a directory where you want to clone the repository:
    ```bash
    cd ../../../../Users/Public
    ```
3. Clone the WhackerLink repository:
    ```bash
    git clone https://github.com/WhackerLink/whackerlink_v4 --recurse-submodules
    cd whackerlink_v4
    ```
4. Build the solution:
    ```bash
    dotnet restore
    msbuild
    ```
5. Navigate to the debug output directory:
    ```bash
    cd x64/Debug
    ```
6. Copy the example configuration file and rename it:
    ```bash
    copy ..\..\whackerlinkserver\configs\config.example.yml .\config.yml
    ```
7. Run the server with the new configuration:
    ```bash
    whackerlinkserver.exe -c config.yml
    ```
8. You should see the server starting up, ending with a message indicating it is listening on port 3000.

If the server does not start as expected, please join the WhackerLink Discord server and ask for assistance, providing screenshots of your issue.

---

## Linux
### Installing Dependencies
```sh
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0 dotnet-runtime-8.0 git
```

### Building WhackerLink
```sh
git clone https://github.com/whackerlink/whackerlink_v4 --recurse-submodules
cd whackerlink_v4

dotnet build WhackerLinkServer --configuration Linux
dotnet build WhackerLinkBridge --configuration Linux
dotnet build WhackerLink2Dvm --configuration Linux

sudo mkdir ../whackerlink-built
sudo cp WhackerLinkServer/bin/Linux/* ../whackerlink-built/ -r
sudo cp WhackerLink2Dvm/bin/Linux/* ../whackerlink-built/ -r
sudo cp WhackerLinkBridge/bin/Linux/* ../whackerlink-built/ -r

sudo cp WhackerLinkServer/configs/* ../whackerlink-built/configs
sudo cp WhackerLink2Dvm/configs/config.example.yml ../whackerlink-built/configs/whackerlinkdvm-config.yml
sudo cp WhackerLinkBridge/configs/config.example.yml ../whackerlink-built/configs/whackerlinkbridge-config.yml

cd ../whackerlink-built
```

### Installing the Precompiled Vocoder
```sh
mkdir -p /tmp/vocoder_temp && cd /tmp/vocoder_temp

sudo apt install curl
curl -L -O https://github.com/DVMProject/dvmvocoder/releases/download/v0.1/libvocoder-v0.1-linux.tar.gz
tar -xzf libvocoder-v0.1-linux.tar.gz
sudo cp libvocoder.so "$OLDPWD"
cd "$OLDPWD"
rm -rf /tmp/vocoder_temp
```

### Compiling the Vocoder from Source (If the pre compiled causes issues)
```sh
cd ..
git clone https://github.com/WhackerLink/dvmvocoder/
cd dvmvocoder
sudo apt install build-essential cmake
mkdir build && cd build
cmake ..
make

sudo cp libvocoder.so ../../whackerlink-built/
cd ../../whackerlink-built
```

### Running the Server
```sh
sudo ./WhackerLinkServer -c configs/config.example.yml
```

---

## Configuration
After installation, customize `config.yml` inside the server directory (`whackerlink-built` or your extracted server folder).  
Adjust settings like port, database connections, devices, and more as needed.

For questions or support, visit the WhackerLink Discord community.

## Desktop App Setup
If you plan to use the WPF desktop app:
- Copy a `codeplug.yml` from `WhackerLinkMobileRadio/codeplugs/` to the app output folder at:
  ```
  WhackerLinkMobileRadio/bin/Debug/
  ```

---
## About
- **WhackerLink V1**: Static HTML frontend with Node.js backend using WebRTC and Socket.IO.
- **WhackerLink V2**: Canceled; never used in production.
- **WhackerLink V3**: Node.js backend with EJS templates, WebAudioAPI for audio, and Socket.IO.
- **WhackerLink V4**: Modern C# .NET Core server with CommonLib for shared logic, and a WPF-based desktop frontend.

---
