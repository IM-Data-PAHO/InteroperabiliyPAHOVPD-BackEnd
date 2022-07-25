using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Impodatos.Services.EventHandlers.Commands
{
    public class SendMail
    {

        public void SenEmailImport(string serverC, string subjectC, string bodyC, string toC, string fromC, string passC, string portC, string DocName)
        {
            var fromAddress = new MailAddress(fromC, subjectC);
            var toAddress = new MailAddress(toC, "To Name");
             string fromPassword = passC;
             string subject = subjectC;
             string body = bodyC;

            var smtp = new SmtpClient
            {
                Host = serverC,
                Port = Convert.ToInt32(portC),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = DocName+  body  
            })
            {
                smtp.Send(message);
            }
        }

    }
}
