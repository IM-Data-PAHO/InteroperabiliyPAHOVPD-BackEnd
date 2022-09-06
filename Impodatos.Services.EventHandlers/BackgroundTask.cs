using Impodatos.Persistence.Database;
using Impodatos.Services.EventHandlers.Commands;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Impodatos.Services.EventHandlers
{
    public class BackgroundTask: IHostedService
    {
            
       

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("task started");
            return Task.FromResult(true);
        }       

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        internal void StartAsync(Task task )
        {
            Console.WriteLine("task started");
        }
    }
}