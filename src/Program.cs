using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using PerfMonLib;
using Newtonsoft.Json;

namespace PerfmonA
{
    class Program
    {
        public const string ApplicationName = "PerfmonA";
        public static readonly string ConfigurationFileName = $"{ApplicationName}.config";

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            ReadConfiguration();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
                
        public static string ConfigurationFileFullPath
        {
            get
            {
                var cfgpath = Environment.GetFolderPath( 
                                Environment.SpecialFolder.ApplicationData,
                                Environment.SpecialFolderOption.Create );
                cfgpath = Path.GetFullPath(
                                Program.ApplicationName,
                                cfgpath );
                if ( !Directory.Exists( cfgpath ) ) Directory.CreateDirectory( cfgpath );

                var cfgfile = Path.GetFullPath(
                                ConfigurationFileName,
                                cfgpath );

                return cfgfile;
            }
        }
        internal static void ReadConfiguration()
        {
            if ( File.Exists( ConfigurationFileFullPath ) )
            {
                LoadConfiguration();
            }
        }

        public static void LoadConfiguration()
        {
            try
            {
                PerfMonContext.Config = JsonConvert.DeserializeObject<PerfMonConfig>( File.ReadAllText( ConfigurationFileFullPath ) ) ?? new PerfMonConfig();
            }
            catch( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( ex.ToString() );
            }
        }
        public static void SaveConfiguration()
        {
            var st = JsonConvert.SerializeObject( PerfMonContext.Config );
            File.WriteAllText( ConfigurationFileFullPath, st );
        }
    }
}
