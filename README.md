# DiscordCableMuter

Automatically mutes Discord's audio session on a VB-Audio Virtual Cable (CABLE Input) the moment it becomes active. This fixes the issue where the Virtual Cable's audio unintentionally passes through to your Discord stream or screen share, being heard by your viewers when it shouldn't be.

---

## How It Works

The program runs silently in the background with no window or tray icon. It monitors the VB-Audio Virtual Cable Input device for any Discord audio session and mutes it immediately using SoundVolumeCommandLine (`svcl.exe`). It also trims its own log file on startup, keeping only the last 7 days of entries.

---

## Requirements

- Windows 10 or 11
- [VB-Audio Virtual Cable](https://vb-audio.com/Cable/) installed
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (only needed to build)
- `svcl.exe` from [NirSoft SoundVolumeCommandLine](https://www.nirsoft.net/utils/sound_volume_command_line.html)

---

## Building

**1. Install the .NET 8 SDK** from https://dotnet.microsoft.com/download

**2. Create a new console project**
```
mkdir DiscordCableMuter
cd DiscordCableMuter
dotnet new console
```

**3. Delete the auto-generated `Program.cs`** that `dotnet new console` creates (it contains just a Hello World line — delete it entirely).

**4. Copy the `Program.cs` file** into the project folder.

**5. Open `DiscordCableMuter.csproj`** and replace its contents with:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NAudio" Version="2.2.1" />
  </ItemGroup>
</Project>
```

**6. Add the NAudio package** (exact version, with NuGet as the source)
```
dotnet add package NAudio --version 2.2.1 --source https://api.nuget.org/v3/index.json
```

**7. Build and publish as a single executable**
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output `.exe` will be at:
```
bin\Release\net8.0-windows\win-x64\publish\DiscordCableMuter.exe
```

---

## Setup

1. Copy `DiscordCableMuter.exe` to any folder you like.
2. Download `svcl.exe` from https://www.nirsoft.net/utils/sound_volume_command_line.html and place it in the **same folder** as the `.exe`.
3. Make sure VB-Audio Virtual Cable is installed and shows up in your Windows sound devices.

Your folder should look like this:
```
DiscordCableMuter\
  DiscordCableMuter.exe
  svcl.exe
```

---

## Running

Just double-click `DiscordCableMuter.exe`. It runs silently with no window.

To have it start automatically with Windows, place a shortcut to it in:
```
shell:startup
```
(Press `Win + R`, paste that path, and hit Enter.)

---

## Logging

The program writes a log file called `muter.log` in the same folder as the `.exe`. It records:
- When monitoring starts
- When a Discord session is detected
- When a mute is applied
- Any errors (e.g. missing `svcl.exe` or missing VB-Cable device)

Log entries older than 7 days are automatically removed each time the program starts.

---

## Stopping

Since there is no window, kill it via Task Manager — find `DiscordCableMuter` under the process list and end it.