# WhackerLink V4 Installation and Setup Guide

[![License](https://img.shields.io/badge/License-GPLv3-blue?style=for-the-badge)](https://www.gnu.org/licenses/gpl-3.0)

## Want to just try downloading the server and see if it works?
1. Install [NET 8.0 runtime](https://aka.ms/dotnet-core-applaunch?missing_runtime=true&arch=x64&rid=win-x64&os=win10&apphost_version=8.0.11).
2. Install [ASP NET Core framework](https://aka.ms/dotnet-core-applaunch?framework=Microsoft.AspNetCore.App&framework_version=8.0.0&arch=x64&rid=win-x64&os=win10).
3. Download WhackerLinkServer from the releases tab.
4. Try running `WhackerLinkServer.exe -c configs/config.example.yml`, you should see the server start with the default values. 
5. If these steps do not work, either ask for help in the discord or follow the below steps for manual compliation.

## Installing Git
1. Download Git from [this link](https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe).
2. Run the downloaded installer. Follow the prompts, leaving all options at their default settings.

## Installing Build Tools
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

## Building WhackerLink
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

## Configuration
Modify `config.yml` as needed for your environment. For specific questions or more detailed setup options, refer to discussions in the WhackerLink Discord server.

## Desktop App Setup
1. For the desktop application, copy `codeplug.yml` from `WhackerLinkMobileRadio/codeplugs` to the output directory at `WhackerLinkMobileRadio/bin/Debug`.

## About
WhackerLink V1 was a static HTML and NodeJS backend application using WebRTC and Socket.IO.
WhackerLink V2 was never released or used in producion and should never have existed.
WhackerLink V3 was a NodeJS backend with a EJS templated front end using WebAudioAPI with Socket.IO
WhackerLink V4 is a C# .NET based application which utlizes a CommonLib for code reusability and a WPF front end.
