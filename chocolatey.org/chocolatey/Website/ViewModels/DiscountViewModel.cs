using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using NuGetGallery.Infrastructure;

namespace NuGetGallery
{
    public class DiscountViewModel : ISpamValidationModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [StringLength(150)]
        [DataType(DataType.EmailAddress)]
        [Hint("Provide your email address so we can follow up with you.")]
        [RegularExpression(@"[.\S]+\@[.\S]+\.[.\S]+", ErrorMessage = "This doesn't appear to be a valid email address.")]
        public string Email { get; set; }

        [Display(Name = "First name")]
        [Required(ErrorMessage = "Please enter your first name.")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        [Required(ErrorMessage = "Please enter your last name.")]
        public string LastName { get; set; }

        [Display(Name = "Discount Requested")]
        [Required(ErrorMessage = "Please make a selection.")]
        public string DiscountType { get; set; }

        [ScaffoldColumn(false)]
        public string SpamValidationResponse { get; set; }

        public IEnumerable<SelectListItem> DiscountTypeItems
        {
            get
            {
                yield return new SelectListItem { Text = "Pro Student Discount", Value = "StudentDiscount" };
                //yield return new SelectListItem { Text = "Microsoft MVP Discount", Value = "MVPDiscount" };
            }
        }
    }
}