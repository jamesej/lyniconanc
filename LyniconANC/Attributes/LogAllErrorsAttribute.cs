using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Lynicon.Membership;

namespace Lynicon.Attributes
{
    /// <summary>
    /// Specifies that all errors should be trapped and reported in the log4net log file for the relevant action methods or controller classes or globally
    /// </summary>
    public class LogAllErrorsAttribute : FilterAttribute, IExceptionFilter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The method called when an exception happens
        /// </summary>
        /// <param name="filterContext">The exception context</param>
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext != null && filterContext.HttpContext != null)
            {
                if (!filterContext.ExceptionHandled)
                {
                    string userName = null;
                    try
                    {
                        var u = LyniconSecurityManager.Current.User;
                        if (u != null)
                            userName = u.UserName;
                    }
                    catch { }

                    string path = null;
                    string referrer = null;
                    try
                    {
                        path = filterContext.HttpContext.Request.RawUrl;
                        referrer = filterContext.HttpContext.Request.UrlReferrer.OriginalString;
                    }
                    catch { }
                    
                    // Log the exception. This is using Log4net as logging tool
                    log.Error((userName == null ? "" : "User: " + userName + " - ")
                        + (path == null ? "" : " request url: " + path + (referrer == null ? "" : "(Referrer: " + referrer + ")") + " - ")
                        + "Uncaught exception: ",
                        filterContext.Exception);
                }
            }
        }
    }
}
