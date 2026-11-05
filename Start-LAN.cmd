@echo off
rem Runs Skill UP so other devices on the same Wi-Fi can open it (http://<this PC's address>:5080), for testing calls between two devices.
rem Windows asks once to allow the app through the firewall. PostgreSQL must be running.
cd /d "%~dp0"
dotnet run --launch-profile lan
