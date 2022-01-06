using EmailLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web.Http;

namespace Email_API.Controller
{

    public class EmailController : ApiController
    {
        [HttpGet]
        public string Get()
        {
            //return all emails in database
            return EmailRepository.GetEmails();
        }
        [HttpPost]
        [ActionName("EmailPost")]
        public string Post(PostPayload payload)
        {
            
            //create email obj to be stored in database
            var email = new Email(payload.destination, payload.subject, payload.body, DateTime.Now, false);

            EmailRepository.SendEmail(email);
            return "Thank you";
        }
    }
    public class PostPayload
    {
        public string destination { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
    }
}
