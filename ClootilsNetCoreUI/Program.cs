﻿using System;
using Avalonia;
using Avalonia.Logging.Serilog;

namespace ClootilsNetCoreUI.VS2017
{
    class Program
    {
        static void Main(string[] args)
        {
            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
#if DEBUG            
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
#else
                .UsePlatformDetect()
                .UseReactiveUI();
#endif
    }
}
