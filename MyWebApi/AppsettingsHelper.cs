using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyWebApi
{
    public static class AppSettingsHelper
    {
        //Nuget安装Configuration
        public static IConfiguration Configuration { get; set; }
        static AppSettingsHelper()
        {
            Configuration = new ConfigurationBuilder()
            .Add(new JsonConfigurationSource { Path = "RateLimit.json", ReloadOnChange = true })
            .Build();
        }
        public static bool GetApp(string[] sectionNames)
        {
            //Middleware: ClientIdRateLimit: Enabled
            var section = Configuration.GetSection($"{sectionNames[0]}:{sectionNames[1]}:{sectionNames[2]}").Value;
            return Convert.ToBoolean(section);
        }
    }
}
