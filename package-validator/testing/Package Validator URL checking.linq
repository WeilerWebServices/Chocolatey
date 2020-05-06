<Query Kind="Program" />

void Main()
{
	Console.WriteLine(url_is_valid(new Uri("https://www.elster.de/elsterweb/infoseite/elsterformular")));
}
public static bool url_is_valid(Uri url)
{
    if (url == null)
    {
        return true;
    }
    if (url.Scheme == "mailto")
    {
        // mailto links are not expected/allowed, therefore immediately fail with no further processing
        return false;
    }
    if (!url.Scheme.StartsWith("http"))
    {
        // Currently we can only validate http/https URL's, therefore simply return true for any others.
        return true;
    }
    try
    {
        var request = (System.Net.HttpWebRequest) System.Net.WebRequest.Create(url);
        var cookieContainer = new System.Net.CookieContainer();
        request.CookieContainer = cookieContainer;
        request.Timeout = 30000;
        //This would allow 301 and 302 to be valid as well
        request.AllowAutoRedirect = true;
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
		request.Headers["Sec-Fetch-Mode"] = "navigate";
		request.Headers["Sec-Fetch-Dest"] = "document";
		request.Headers["Sec-Fetch-Site"] = "cross-site";
		request.Headers["Sec-Fetch-User"] = "?1";
        using (var response = (System.Net.HttpWebResponse) request.GetResponse())
        {
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        }
    }
    catch (System.Net.WebException ex)
    {
		if (ex.Status == System.Net.WebExceptionStatus.ProtocolError && ex.Message == "The remote server returned an error: (403) Forbidden." && ex.Response.Headers["Server"] == "AkamaiGHost")
		{
			Console.WriteLine("Error validating Url {0} - {1}", url.ToString(), ex.Message);
			Console.WriteLine("Since this is likely due to the fact that the server is using Akamai, which expects request headers to be in a VERY specific order and case, this URL will be marked as valid for the time being.");
			Console.WriteLine("This check was put in place as a result of this issue: https://github.com/chocolatey/package-validator/issues/225");
			return true;
		}
        if (ex.Status == System.Net.WebExceptionStatus.ProtocolError && ex.Message == "The remote server returned an error: (403) Forbidden." && ex.Response.Headers["Server"] == "cloudflare")
        {
            Console.WriteLine("Error validating Url {0} - {1}", url.ToString(), ex.Message);
            Console.WriteLine("Since this is likely due to the fact that the server is using Cloudflare, is sometimes popping up a Captcha which needs to be solved, obviously not possible by package-validator.");
            Console.WriteLine("This check was put in place as a result of this issue: https://github.com/chocolatey/package-validator/issues/229");
            return true;
        }
        if (ex.Status == System.Net.WebExceptionStatus.SecureChannelFailure || (ex.Status == System.Net.WebExceptionStatus.UnknownError && ex.Message == "The SSL connection could not be established, see inner exception. Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.."))
        {
            Console.WriteLine("Error validating Url {0} - {1}", url.ToString(), ex.Message);
            Console.WriteLine("Since this is likely due to missing Ciphers on the machine hosting package-validator, this URL will be marked as valid for the time being.");
            return true;
        }
        if (ex.Status == System.Net.WebExceptionStatus.ProtocolError && ex.Message == "The remote server returned an error: (503) Server Unavailable.")
        {
            Console.WriteLine("Error validating Url {0} - {1}", url.ToString(), ex.Message);
            Console.WriteLine("This could be due to Cloudflare DDOS protection acting in front of the site, or another valid reason, as such, this URL will be marked as valid for the time being.");
            return true;
        }
        Console.WriteLine("Web Exception - Error validating Url {0} - {1}", url.ToString(), ex.Message);
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine("General Exception - Error validating Url {0} - {1}", url.ToString(), ex.Message);
        return false;
    }
}
// Define other methods and classes here