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

using System.Net.Mail;

namespace NuGetGallery
{
    public interface IMessageService
    {
        void SendContactOwnersMessage(
            MailAddress fromAddress,
            PackageRegistration packageRegistration,
            string message,
            string emailSettingsUrl,
            string packageUrl,
            bool copySender);

        void SendCommentNotificationToMaintainers(
            PackageRegistration packageRegistration, CommentViewModel comment, string packageUrl);

        void ReportAbuse(MailAddress fromAddress, Package package, string message, string packageUrl, bool copySender);
        void ContactSiteAdmins(MailAddress fromAddress, Package package, string message, string packageUrl, bool copySender);
        void ContactTrial(MailAddress fromAddress, string message, string optionalSubject);
        void ContactGeneral(MailAddress fromAddress, string contactType, string message, string optionalSubject);
        void ContactDiscount(MailAddress fromAddress, string message, string optionalSubject);
        void ContactPartner(MailAddress fromAddress, string message, string optionalSubject);
        void ContactSales(MailAddress fromAddress, string message, string optionalSubject, bool pipeline);
        void ContactBlocked(MailAddress fromAddress, string message, string optionalSubject);
        void ContactQuickDeployment(MailAddress fromAddress, string message, string optionalSubject);
        void Discount(string message, string emailTo, string fullName, string discountType);
        void SendNewAccountEmail(MailAddress toAddress, string confirmationUrl);
        void SendEmailChangeConfirmationNotice(MailAddress newEmailAddress, string confirmationUrl);
        void SendPasswordResetInstructions(User user, string resetPasswordUrl);
        void SendEmailChangeNoticeToPreviousEmailAddress(User user, string oldEmailAddress);
        void SendPackageOwnerRequest(User fromUser, User toUser, PackageRegistration package, string confirmationUrl);
        void SendPackageOwnerConfirmation(User fromUser, User toUser, PackageRegistration package);
        void SendPackageModerationEmail(Package package, string comments, string subjectComment, User fromUser);
        void SendPackageModerationReviewerEmail(Package package, string comments, User fromUser);
        void SendPackageTestFailureMessage(Package package, string resultDetailsUrl);
    }
}
