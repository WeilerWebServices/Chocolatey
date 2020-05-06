using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NuGetGallery
{
    public class HyphenatedRouteHandler : MvcRouteHandler
    {
        protected override IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            if (requestContext.RouteData.Values.ContainsKey("area"))
            {
                requestContext.RouteData.Values["area"] = requestContext.RouteData.Values["area"].ToString().Replace("-", "");
            }
            
            requestContext.RouteData.Values["controller"] = requestContext.RouteData.Values["controller"].ToString().Replace("-", "_");
            requestContext.RouteData.Values["action"] = requestContext.RouteData.Values["action"].ToString().Replace("-", "");

            return base.GetHttpHandler(requestContext);
        }
    }
}