using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;

namespace DSTBuilder.Helpers
{
    public class SendMail
    {
        #region Methods
        public void EmailSender(string recipiants, string subject, string body)
        {
            EmailSender(recipiants, subject, body, string.Empty);
        }
        public void EmailSender(string recipiants, string subject, string body, string attachPath)
        {
            EmailSender(recipiants, subject, body, new string[] { attachPath });
        }
        public void EmailSender(string recipiants, string subject, string body, string[] attachPath)
        {
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
            MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
            using (MailMessage mail = new MailMessage())
            {

                mail.From = new MailAddress(mailSettings.Smtp.From, "ProModel Build Service");
                mail.To.Add(new MailAddress(recipiants));
                mail.Subject = subject;
                mail.Body = body;

                foreach (string attachment in attachPath)
                {
                    if (File.Exists(attachment))
                    {
                        mail.Attachments.Add(new Attachment(attachment));
                    }
                }
                    SmtpClient smtp = new SmtpClient(mailSettings.Smtp.Network.Host);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.EnableSsl = mailSettings.Smtp.Network.EnableSsl;
                    smtp.Port = mailSettings.Smtp.Network.Port;
                    smtp.Credentials = new NetworkCredential(mailSettings.Smtp.Network.UserName, mailSettings.Smtp.Network.Password);
                    smtp.Send(mail);
            }
        }

        public void EmailTammyOnly(string subject, string body)
        {
            Configuration configurationFile = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
            MailSettingsSectionGroup mailSettings = configurationFile.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
            using (MailMessage mail = new MailMessage())
            {

                mail.From = new MailAddress(mailSettings.Smtp.From, "ProModel Build Service");
                mail.To.Add(new MailAddress("tmcclellan@promodel.com"));
                mail.Subject = subject;
                mail.Body = body;
                SmtpClient smtp = new SmtpClient(mailSettings.Smtp.Network.Host);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.EnableSsl = mailSettings.Smtp.Network.EnableSsl;
                smtp.Port = mailSettings.Smtp.Network.Port;
                smtp.Credentials = new NetworkCredential(mailSettings.Smtp.Network.UserName, mailSettings.Smtp.Network.Password);
                smtp.Send(mail);
            }
        }       
        
        #endregion Methods

      
    }
}