using Microservice.VPDDataImport.Persistence.Database;
using Microservice.VPDDataImport.Services.EventHandlers.Commands;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microservice.VPDDataImport.Services.EventHandlers
{
    public class BackgroundTask: IHostedService
    {
        /// <summary>
        /// Tarea de inicio del Background
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("task started");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Tarea de terminación  del Background
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Tarea de inicio del Background para llamado en las diferentes clases
        /// </summary>
        /// <param name="task"></param>
        internal void StartAsync(Task task )
        {
            Console.WriteLine("task started");
        }
    }
}