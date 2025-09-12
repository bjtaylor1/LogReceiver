# LogTest - NLog Test Message Sender

LogTest is a console application for sending test log messages to the LogReceiver application using NLog with TCP network logging.

## Features

- **Command-line interface** with required and optional parameters
- **Interactive prompting** for missing required parameters
- **Flexible parameter format** supporting multiple argument styles
- **Uses your nlog.config.user configuration** to send logs to LogReceiver
- **Support for all log levels** (Trace, Debug, Info, Warn, Error, Fatal)
- **Exception testing** with custom exception messages

## Usage

### Command Line Arguments

```bash
dotnet run -- -logger <logger-name> -message <message> [-level <level>] [-exception <exception-text>]
```

#### Required Parameters:
- `-logger` - Logger name (e.g., 'BT.Debug.Test' or 'MyApp.Service')
- `-message` - Log message text

#### Optional Parameters:
- `-level` - Log level: Trace, Debug, Info, Warn, Error, Fatal (default: Info)
- `-exception` - Exception text to include with the log message

### Examples

#### Basic Usage:
```bash
dotnet run -- -logger BT.Debug.Test -message "Test message"
```

#### With Log Level:
```bash
dotnet run -- -logger BT.Debug.CommandInfo -message "Command executed" -level Debug
```

#### With Exception:
```bash
dotnet run -- -logger MyApp.Service -message "Service error" -level Error -exception "Connection failed"
```

#### Interactive Mode:
If you don't provide required parameters, LogTest will prompt you:
```bash
dotnet run
# Will prompt for logger name and message
```

## Configuration

LogTest uses the `nlog.config` file which is configured to send logs to `tcp://localhost:4505` (the default LogReceiver port).

The configuration includes:
- JSON formatted messages with timestamp, level, logger, message, and exception
- Warning+ level for all loggers
- All levels for BT.Debug* loggers

## Building and Running

1. Build the solution:
   ```bash
   dotnet build
   ```

2. Run the application:
   ```bash
   cd LogTest
   dotnet run -- -logger BT.Debug.Test -message "Hello World"
   ```

   Or for interactive mode:
   ```bash
   dotnet run
   ```

## Testing with LogReceiver

1. Start the LogReceiver application
2. Ensure it's listening on port 4505
3. Run LogTest with your desired parameters:
   ```bash
   dotnet run -- -logger BT.Debug.Test -message "Test message"
   ```
4. The messages should appear in the LogReceiver hierarchical tree and message list

## Troubleshooting

- **Connection issues**: Ensure LogReceiver is running and listening on port 4505
- **Missing logs**: Check that your logger name matches the filtering rules in nlog.config
- **Build errors**: Ensure NLog NuGet package is restored

For BT.Debug* loggers, all log levels will be sent. For other loggers, only Warning and above will be sent (as per the nlog.config rules).
