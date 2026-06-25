# wtKST

This repository is a fork of wtKST, originally written by Frank Schmahling DL2ALF for the DL0GTH team and later maintained at https://github.com/dl8aau/wtkst.

wtKST is a Windows client for the [ON4KST](http://www.on4kst.com/chat/start.php) chat, optimized for VHF/UHF/SHF contest sked management. It can combine ON4KST chat information with contest log data, AirScout information, and local QRV state to make active stations easier to find and prioritize.

This version is licensed under the GPL v3 or later. See [LICENSE](LICENSE) for details.

## Features

* Connects to ON4KST chat using the port 23001 feed.
* Supports the available ON4KST chat rooms.
* Filters messages addressed to or from your station.
* Filters displayed users by distance, here/away state, and whether they are already in the log.
* Sorts users alphabetically or by antenna direction.
* Shows likely QRV band information from chat names and AirScout/station data.
* Supports airplane scatter status through [AirScout](http://airscout.eu/index.php).
* Supports Win-Test v4 file and network based log integration.
* Supports QARTest log integration.
* Supports N1MM Logger+ database and live UDP integration.
* Supports ADIF log files.
* Can create skeds in Win-Test.

## N1MM Logger+ Support

N1MM Logger+ support has two modes:

* **DB only** reads an N1MM `.s3db` database on a refresh interval.
* **DB + Live UDP** reads the same database and also listens for N1MM `contactinfo` UDP packets on port `12060`.

To configure DB mode:

1. Open the wtKST options dialog.
2. In the N1MM Logger+ section, select the N1MM database file.
   The usual location is:
   `C:\Users\<username>\Documents\N1MM Logger+\Databases`
3. Click **Load** to list contests in the database.
4. Select the active contest.
5. Choose either **DB only** or **DB + Live UDP**.

For live UDP updates, configure N1MM Logger+ to send `contactinfo` packets to UDP port `12060`.

For a single-machine setup, `127.0.0.1:12060` is sufficient. For a networked multi-station setup, send to the subnet broadcast address, for example `192.168.1.255:12060` on a `/24` network. wtKST listens on all local interfaces, so it can receive broadcast packets if Windows Firewall allows inbound UDP traffic on port `12060`.

In N1MM+ network mode, each station should also store networked contacts in its local database. That means DB polling can recover the current status after wtKST is restarted, while live UDP provides faster updates while wtKST is running.

## Installation

Releases are published from this fork at:

https://github.com/m0vse/wtkst/releases

Tagged releases build an MSI installer and a portable ZIP. Use the MSI for a normal Windows installation, or extract the portable ZIP and run `wtKST.exe`.

## Logging

wtKST writes log files under:

`%localappdata%\wtKST\wtKST`

The log file name uses the form:

`wtKST_dd.mm.yyyy.log`

## Bug Reports And Feedback

Please report issues for this fork at:

https://github.com/m0vse/wtkst/issues

## Building From Source

Build with Visual Studio 2022 on Windows.

Load `wtKST.sln` and build the `Release|x86` configuration. The installer project uses the Visual Studio Installer Projects extension.

ScoutBase DLLs are currently included as external binaries. They are built from the AirScout tree upstream.
