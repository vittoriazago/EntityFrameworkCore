﻿using System;
using System.Diagnostics;
using System.IO;
using Blueshift.EntityFrameworkCore.MongoDB.SampleDomain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blueshift.EntityFrameworkCore.MongoDB.Tests
{
    public class MongoDbFixture : IDisposable
    {
        private static readonly bool IsCiBuild;
        private Process _mongodProcess;

        static MongoDbFixture()
        {
            IsCiBuild =
                (Boolean.TryParse(Environment.ExpandEnvironmentVariables("%APPVEYOR%"), out bool isAppVeyor) && isAppVeyor) ||
                (Boolean.TryParse(Environment.ExpandEnvironmentVariables("%TRAVIS%"), out bool isTravisCI) && isTravisCI);
        }

        public MongoDbFixture()
        {
            if (!IsCiBuild)
            {
                Directory.CreateDirectory(MongoDbConstants.DataFolder);
                _mongodProcess = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = MongoDbConstants.MongodExe,
                        Arguments = $@"-vvvvv --port {MongoDbConstants.MongodPort} --logpath "".data\{MongoDbConstants.MongodPort}.log"" --dbpath ""{MongoDbConstants.DataFolder}""",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
            }
        }

        public ZooDbContext ZooDbContext => new ServiceCollection()
                .AddDbContext<ZooDbContext>(options => options.UseMongoDb(connectionString: MongoDbConstants.MongoUrl))
                .BuildServiceProvider()
                .GetService<ZooDbContext>();

        public void Dispose()
        {
            if (!IsCiBuild && _mongodProcess != null && !_mongodProcess.HasExited)
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = MongoDbConstants.MongoExe,
                        Arguments = $@"""{MongoDbConstants.MongoUrl}/admin"" --eval ""db.shutdownServer();""",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                _mongodProcess.WaitForExit(milliseconds: 5000);
                _mongodProcess.Dispose();
                _mongodProcess = null;
            }
        }
    }
}
