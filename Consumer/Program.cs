using Common.Context;
using Common.Data;
using Common.Display;
using Dapper;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Immutable;
using System.Data;
using System.Drawing;
using static Dapper.SqlMapper;

Console.WriteLine("\nONE-WAY MESSAGING : CONSUMER");

var configuration = new ConfigurationBuilder()
     .AddJsonFile($"appsettings.json");
var Configuration = configuration.Build();

IDbConnection db = new DapperContext(Configuration).CreateConnection();

//Create fake data an insert in DB
db.Execute("TRUNCATE TABLE [Destination]");

var connectionFactory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest"
};

using var connection = connectionFactory.CreateConnection();

using var channel = connection.CreateModel();

var queue = channel.QueueDeclare(
    queue: "sum_queue",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: ImmutableDictionary<string, object>.Empty);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (sender, eventArgs) =>
{
    var messageBody = eventArgs.Body.ToArray();
    var entity = Source.FromBytes(messageBody);
    
    _ = db.Execute("Insert into [Destination] (A,B,Result) Values (@A, @B, @Result)", new { A = entity.A, B = entity.B,  Result = entity.A + entity.B });

    DisplayInfo<Source>
    .For(entity)
        .SetExchange(eventArgs.Exchange)
        .SetQueue(queue)
        .SetRoutingKey(eventArgs.RoutingKey)
        .SetVirtualHost(connectionFactory.VirtualHost)
        .Display(Color.Yellow);

    channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
};

channel.BasicConsume(
    queue: queue.QueueName,
    autoAck: false,
    consumer: consumer);

Console.ReadLine();
