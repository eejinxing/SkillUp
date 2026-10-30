@echo off
rem Starts Skill UP for testing on other devices on the same Wi-Fi (asks for administrator rights once).
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -NoExit -File \"%~dp0Start-LAN.ps1\"'"
