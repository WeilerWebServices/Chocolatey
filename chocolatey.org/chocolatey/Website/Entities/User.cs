using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NuGetGallery
{
    public class User : IEntity
    {
        public User()
            : this(null, null)
        {
        }

        public User(
            string username,
            string hashedPassword)
        {
            HashedPassword = hashedPassword;
            Messages = new HashSet<EmailMessage>();
            Username = username;
        }

        public int Key { get; set; }

        public Guid ApiKey { get; set; }
        [StringLength(150)]
        public string EmailAddress { get; set; }
        [StringLength(150)]
        public string UnconfirmedEmailAddress { get; set; }
        [StringLength(256)]
        public string HashedPassword { get; set; }
        [StringLength(20)]
        public string PasswordHashAlgorithm { get; set; }
        public virtual ICollection<EmailMessage> Messages { get; set; }
        [StringLength(64)]
        public string Username { get; set; }
        public virtual ICollection<Role> Roles { get; set; }
        public bool EmailAllowed { get; set; }
        public bool Confirmed
        {
            get
            {
                return !String.IsNullOrEmpty(EmailAddress);
            }
        }
        [StringLength(256)]
        public string EmailConfirmationToken { get; set; }
        [StringLength(256)]
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpirationDate { get; set; }
        public bool EmailAllModerationNotifications { get; set; }
        public bool IsTrusted { get; set; }
        public bool IsBanned { get; set; }

        public void ConfirmEmailAddress()
        {
            if (String.IsNullOrEmpty(UnconfirmedEmailAddress))
            {
                throw new InvalidOperationException("User does not have an email address to confirm");
            }
            EmailAddress = UnconfirmedEmailAddress;
            EmailConfirmationToken = null;
            UnconfirmedEmailAddress = null;
        }
    }
}