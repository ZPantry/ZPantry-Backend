using System;
using System.Collections.Generic;
using System.Text;

namespace AuthenticationModule.DTOs
{
    public class VerifyRequest
    {
        public string OtpCode { get; set; }

        public string Email { get; set; }
    }
}
