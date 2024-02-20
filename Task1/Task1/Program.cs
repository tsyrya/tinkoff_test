// See https://aka.ms/new-console-template for more information

using ConsoleApp1;
using Microsoft.Extensions.Logging;

ILoggerFactory f = new LoggerFactory();
var client1 = new ClientWithFail(2000);
var client2 = new Client(1000);
// var client2 = new ClientWithRetry(1000, TimeSpan.FromMilliseconds(500));
var handler = new Handler(client1, client2, new Logger<Handler>(f));

var response = await handler.GetApplicationStatus("123");
        
Console.WriteLine(response.ToString());