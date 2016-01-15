using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace DSTBuilder.Helpers
{
    public class Email
    {
        #region Fields/Properties

        #endregion Fields/Properties

        #region Constructors

        #endregion Constructors

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
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("BuildM@promodel.com", "ProModel Build Service");
            mail.To.Add(recipiants);
            mail.Subject = subject;
            mail.Body = body;

            foreach (string attachment in attachPath)
            {
                if (File.Exists(attachment))
                {
                    mail.Attachments.Add(new Attachment(attachment));
                }
            }

            try
            {
                SmtpClient smtp = new SmtpClient("bl2prd0511.outlook.com");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.EnableSsl = true;
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("BuildM@promodel.com", "special123$");
                smtp.Send(mail);
            }
            catch (SmtpException ex)
            {
                AddLog("Failed to send e-mail! " + ex.Message);
            }
        }
        private void AddLog(string logEntry)
        {
            string filePath = "C:\\BuildLog.log";

            if (!File.Exists(filePath))
            {
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(DateTime.Now + " ||" + " " + logEntry);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(DateTime.Now + " ||" + " " + logEntry);
                }
            }
        }
        #endregion Methods

        #region Event Handlers

        #endregion Event Handlers

    }
}