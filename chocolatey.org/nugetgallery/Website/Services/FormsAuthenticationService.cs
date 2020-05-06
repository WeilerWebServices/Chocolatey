using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;

namespace NuGetGallery
{
    public class FormsAuthenticationService : IFormsAuthenticationService
    {
        public void SetAuthCookie(
            string userName,
            bool createPersistentCookie,
            IEnumerable<string> roles)
        {

            string formattedRoles = String.Empty;
            if (roles.AnySafe())
            {
                formattedRoles = String.Join("|", roles);
            }

            HttpContext context = HttpContext.Current;

            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                     version: 1,
                     name: userName,
                     issueDate: DateTime.UtcNow,
                     expiration: DateTime.UtcNow.AddMinutes(4320),
                     isPersistent: createPersistentCookie,
                     userData: formattedRoles
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var sslRequired = System.Configuration.ConfigurationManager.AppSettings.Get("ForceSSL").Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase);
            var formsCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

            if (!HttpContext.Current.Request.IsLocal)
            {
                formsCookie.HttpOnly = true;
                formsCookie.Secure = sslRequired;
            }

            context.Response.Cookies.Add(formsCookie);

        }

        public void SignOut()
        {
            FormsAuthentication.SignOut();
            var context = HttpContext.Current;
            var formsCookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (formsCookie != null)
            {
                formsCookie.Expires = DateTime.UtcNow.AddDays(-1);
                context.Response.Cookies.Add(formsCookie);
            }
        }
    }
}