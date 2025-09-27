using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppServer.Models
{
    public class Payload
    {
        public string Sender { get; set; }
        public string Message { get; set; }

        public Payload(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }
    }
}
