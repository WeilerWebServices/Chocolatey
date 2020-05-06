using System;
using System.Text;
using System.Web.Routing;

namespace NuGetGallery
{
    public class SeoRoute : Route
    {
        public SeoRoute(string url, IRouteHandler routeHandler)
            : base(url, routeHandler) { }

        public SeoRoute(string url, RouteValueDictionary defaults, IRouteHandler routeHandler)
            : base(url, defaults, routeHandler) { }

        public SeoRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouteHandler routeHandler)
            : base(url, defaults, constraints, routeHandler) { }

        public SeoRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouteHandler routeHandler)
            : base(url, defaults, constraints, dataTokens, routeHandler) { }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            //return TransactionLock.Enter("getvirtualpath", () =>
            //{
                var hyphenatedValue = new StringBuilder();
                var lowerCaseValues = new RouteValueDictionary();

                foreach (var v in values)
                {
                    hyphenatedValue.Clear();

                    if (v.Key == null || v.Value == null)
                    {
                        continue;
                    }

                    foreach (var valueChar in v.Value.ToString())
                    {
                        if (Char.IsUpper(valueChar) && hyphenatedValue.Length != 0)
                        {
                            hyphenatedValue.Append("-");
                        }

                        hyphenatedValue.Append(valueChar.ToString().Replace('_', '-'));
                    }

                    switch (v.Key.ToUpperInvariant())
                    {
                        case "ACTION":
                        case "AREA":
                        case "CONTROLLER":
                            lowerCaseValues.Add(v.Key, (hyphenatedValue.Replace("--", "-").ToString()).ToLowerInvariant());
                            break;
                        default:
                            lowerCaseValues.Add(v.Key, v.Value);
                            break;
                    }
                }

                return base.GetVirtualPath(requestContext, lowerCaseValues);
            //});
        }
    }
}