// Copyright 2011 - Present RealDimensions Software, LLC, the original 
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery 
// at https://github.com/NuGet/NuGetGallery
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using AnglicanGeek.MarkdownMailer;
using MarkdownSharp;

namespace NuGetGallery
{
    /// <summary>
    ///   This is an override of MarkdownMailer.MailSender
    /// </summary>
    /// <remarks>https://github.com/half-ogre/MarkdownMailer/blob/master/LICENSE.txt</remarks>
    public class MailSender : IMailSender
    {
        private const int SendTimeout = 5000;

        private readonly SmtpClient _smtpClient;

        public MailSender() : this(new SmtpClient(), null)
        {
        }

        public MailSender(MailSenderConfiguration configuration) : this(new SmtpClient(), configuration)
        {
        }

        public MailSender(SmtpClient smtpClient) : this(smtpClient, null)
        {
        }

        internal MailSender(SmtpClient smtpClient, MailSenderConfiguration configuration)
        {
            if (smtpClient == null) throw new ArgumentNullException("smtpClient");

            if (configuration != null) ConfigureSmtpClient(smtpClient, configuration);

            _smtpClient = smtpClient;
        }

        internal static void ConfigureSmtpClient(SmtpClient smtpClient, MailSenderConfiguration configuration)
        {
            if (configuration.Host != null) smtpClient.Host = configuration.Host;
            if (configuration.Port.HasValue) smtpClient.Port = configuration.Port.Value;
            if (configuration.EnableSsl.HasValue) smtpClient.EnableSsl = configuration.EnableSsl.Value;
            if (configuration.DeliveryMethod.HasValue) smtpClient.DeliveryMethod = configuration.DeliveryMethod.Value;
            if (configuration.UseDefaultCredentials.HasValue) smtpClient.UseDefaultCredentials = configuration.UseDefaultCredentials.Value;
            if (configuration.Credentials != null) smtpClient.Credentials = configuration.Credentials;
            if (configuration.PickupDirectoryLocation != null) smtpClient.PickupDirectoryLocation = configuration.PickupDirectoryLocation;
        }

        public void Send(string fromAddress, string toAddress, string subject, string markdownBody)
        {
            Send(new MailAddress(fromAddress), new MailAddress(toAddress), subject, markdownBody);
        }

        public void Send(MailAddress fromAddress, MailAddress toAddress, string subject, string markdownBody)
        {
            var mailMessage = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = markdownBody
            };

            Send(mailMessage);
        }

        public void Send(MailMessage mailMessage)
        {
            if (_smtpClient.DeliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory
                && !Directory.Exists(_smtpClient.PickupDirectoryLocation)) Directory.CreateDirectory(_smtpClient.PickupDirectoryLocation);

            string markdownBody = mailMessage.Body;

            AlternateView textView = AlternateView.CreateAlternateViewFromString(
                markdownBody,
                null,
                MediaTypeNames.Text.Plain);

            mailMessage.AlternateViews.Add(textView);

            //this is what is different. Tired of receiving plaintext messages with the markdown crap in them.
            var markdownGenerator = new Markdown
            {
                AutoHyperlink = true,
                AutoNewLines = true,
                //EncodeProblemUrlCharacters = true,
                LinkEmails = true,
            };

            string htmlBody = markdownGenerator.Transform(markdownBody);

            AlternateView htmlView = AlternateView.CreateAlternateViewFromString(
                htmlBody,
                null,
                MediaTypeNames.Text.Html);
            mailMessage.AlternateViews.Add(htmlView);

            if (!Monitor.TryEnter(_smtpClient, SendTimeout)) throw new TimeoutException("Smtp client busy, unable to send a mail.");
            try
            {
                _smtpClient.Send(mailMessage);
            }
            finally
            {
                Monitor.Exit(_smtpClient);
            }
        }
    }
}
