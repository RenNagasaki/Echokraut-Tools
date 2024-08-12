using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FF14_Echokraut.Exceptions
{
    public class AlltalkUnauthorizedException : AlltalkFailedException
    {
        public AlltalkUnauthorizedException(HttpStatusCode status, string? message) : base(status, message)
        {
        }
    }
}
