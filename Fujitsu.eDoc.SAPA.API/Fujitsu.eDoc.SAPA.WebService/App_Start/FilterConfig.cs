using System.Web;
using System.Web.Mvc;

namespace Fujitsu.eDoc.SAPA.WebService
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
