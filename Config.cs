using System;
using System.Collections.Generic;
using System.Diagnostics;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DomainFinder {

    public class Config {
        public static string MongoDbConn { get; set; }

        static Config() {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("app.json");

            var configuration = builder.Build();

            MongoDbConn = configuration["MongoDbConn"];
        }
    }
}