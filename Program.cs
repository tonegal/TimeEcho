// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using System.Data.SqlClient;

namespace Microsoft.BotBuilderSamples
{
    public class Program
    {
        public static SqlConnection botDBConnection;

        public static void Main(string[] args)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "second-echo-bot-dbserver.database.windows.net";
            builder.UserID = "azureuser";
            builder.Password = "resuer1!";
            builder.InitialCatalog = "secondEchoBot_db";

            botDBConnection = new SqlConnection(builder.ConnectionString);
            botDBConnection.Open();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
