# Uptime Checker

## Description

Tracks your network connectivity over time by performing a configurable ICMP ping to a target host.

## Configuration

Configuration modified at `/appsettings.json`. Serilog via `IConfiguration` is supported.

- `Settings:PingFrequencyMs` - How often to attempt to ping the remote resource.
- `Settings:PingTimeoutMs` - How often before an ICMP ping should be considered a time out (ie. Offline).
- `Settings:PingTargetHost` - The remote resource must formatted as an IPv4 Address.

## Screenshots

![image](https://github.com/nickmartin1ee7/UptimeChecker/assets/58752614/b5baf4f3-d9f7-4e82-b3e4-f83b8200d03b)
