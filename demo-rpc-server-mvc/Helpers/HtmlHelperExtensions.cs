using System.Web;
using System.Web.Mvc;

namespace demo_rpc_server_mvc
{
    public static class HtmlHelperExtensions
    {
        public static string AbsolutePath(this HtmlHelper html, string path)
        {
            return VirtualPathUtility.ToAbsolute(path);
        }
        public static string Script(this HtmlHelper html, string path)
        {
            var filePath = AbsolutePath(html, path);
            return "<script type=\"text/javascript\" src=\"" + filePath + "\"></script>";
        }
    }
}
