using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.EventHandlers.Commands
{
    public class historyUpdateCommand : INotification
    {
        public int Id { get; set; }
        public string JsonResponse { get; set; }
        public bool State { get; set; }
       
    }
}
