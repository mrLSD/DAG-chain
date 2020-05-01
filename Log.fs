module DAG.Log

open Serilog
open Serilog.Sinks.SystemConsole.Themes
let Logger = LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme = AnsiConsoleTheme.Grayscale)
                .CreateLogger()
          
