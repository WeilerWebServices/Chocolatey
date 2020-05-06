using System.Web.Mvc;

namespace NuGetGallery
{
    /// <summary>
    ///   This provides even more perf as vbhtml is no longer searched against
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/a/8689069/18475
    /// </remarks>
    public class CSharpRazorViewEngine : RazorViewEngine
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="CSharpRazorViewEngine" /> class.
        /// </summary>
        public CSharpRazorViewEngine()
        {
            base.FileExtensions = new[] {"cshtml"};

            base.AreaViewLocationFormats = new[]
                {
                    "~/Areas/{2}/Views/{1}/{0}.cshtml",
                    "~/Areas/{2}/Views/Shared/{0}.cshtml"
                };

            base.AreaMasterLocationFormats = new[]
                {
                    "~/Areas/{2}/Views/{1}/{0}.cshtml",
                    "~/Areas/{2}/Views/Shared/{0}.cshtml"
                };

            base.AreaPartialViewLocationFormats = new[]
                {
                    "~/Areas/{2}/Views/{1}/{0}.cshtml",
                    "~/Areas/{2}/Views/Shared/{0}.cshtml"
                };

            base.ViewLocationFormats = new[]
                {
                    "~/Views/{1}/{0}.cshtml",
                    "~/Views/Shared/{0}.cshtml"
                };

            base.PartialViewLocationFormats = new[]
                {
                    "~/Views/{1}/{0}.cshtml",
                    "~/Views/Shared/{0}.cshtml"
                };

            base.MasterLocationFormats = new[]
                {
                    "~/Views/{1}/{0}.cshtml",
                    "~/Views/Shared/{0}.cshtml"
                };
        }
    }
}