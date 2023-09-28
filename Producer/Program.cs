using System.Threading.Tasks;
using RabbitMQ.Client;
using System;
using System.Drawing;
using System.Collections.Immutable;
using Common.Display;
using Common.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Dapper;
using Common.Data;
using System.Text.Json;
using System.Text;

Console.WriteLine("\nONE-WAY MESSAGING : PRODUCER");

var configuration = new ConfigurationBuilder()
     .AddJsonFile($"appsettings.json");
var Configuration = configuration.Build();

IDbConnection db = new DapperContext(Configuration).CreateConnection();

//Create fake data an insert in DB
db.Execute("TRUNCATE TABLE [Source]");
for (byte i = 1; i <= 5; i++)
{

    var entity = Source.GetFakeData();
    _ = db.Execute("Insert into [Source] (A,B) Values (@A, @B)", new { A = entity.A, B = entity.B});
}


const string ExchangeName = "";
const string QueueName = "sum_queue";

var connectionFactory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest"
};

using var connection = connectionFactory.CreateConnection();

using var channel = connection.CreateModel();

var queue = channel.QueueDeclare(
    queue: QueueName,
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: ImmutableDictionary<string, object>.Empty);

var entities = db.Query<Source>("Select * from [Source]");

foreach(var entity in entities)
{
    //var trade = TradeData.GetFakeTrade();

    channel.BasicPublish(
        exchange: ExchangeName,
        routingKey: QueueName,
        body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                entity,
                new JsonSerializerOptions { WriteIndented = true })));

    DisplayInfo<Source>
        .For(entity)
        .SetExchange(ExchangeName)
        .SetQueue(QueueName)
        .SetRoutingKey(QueueName)
        .SetVirtualHost(connectionFactory.VirtualHost)
        .Display(Color.Cyan);

    await Task.Delay(millisecondsDelay: 1000);
}

Console.ReadLine();