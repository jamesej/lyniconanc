using Lynicon.DataSources;
using Lynicon.Repositories;
using Lynicon.Services;
using LyniconANC.Extensibility;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Collation
{
    public interface ITypeSystemRegistrar
    {
        /// <summary>
        /// The Lynicon data system in which this type system registrar exists
        /// </summary>
        LyniconSystem System { get; set; }

        /// <summary>
        /// Initialise the type system, the collator, the repository and the editor redirect for a content type
        /// </summary>
        /// <param name="t">content type</param>
        /// <param name="coll">the collator</param>
        /// <param name="repo">the repository</param>
        /// <param name="redir">the editor redirect</param>
        void SetupType(Type t, ICollator coll, IRepository repo, Func<IRouter, RouteContext, object, IRouter> redir);
    }

    public static class ITypeSystemRegistrarX
    {
        /// <summary>
        /// Initialise the type system, the collator, the repository and the editor redirect for a content type
        /// </summary>
        /// <typeparam name="T">content type</typeparam>
        /// <param name="coll">the collator</param>
        /// <param name="repo">the repository</param>
        /// <param name="redir">the editor redirect</param>
        public static void SetupType<T>(this ITypeSystemRegistrar regr, ICollator coll, IRepository repo, Func<IRouter, RouteContext, object, IRouter> redir)
        {
            regr.SetupType(typeof(T), coll, repo, redir);
        }
        /// <summary>
        /// Initialise the type system, the collator and the repository for a content type
        /// </summary>
        /// <typeparam name="T">content type</typeparam>
        /// <param name="coll">the collator</param>
        /// <param name="repo">the repository</param>
        public static void SetupType<T>(this ITypeSystemRegistrar regr, ICollator coll, IRepository repo)
        {
            regr.SetupType(typeof(T), coll, repo, null);
        }
        /// <summary>
        /// Initialise the type system, the collator and the repository for a content type
        /// </summary>
        /// <param name="t">content type</param>
        /// <param name="coll">the collator</param>
        /// <param name="repo">the repository</param>
        public static void SetupType(this ITypeSystemRegistrar regr, Type t, ICollator coll, IRepository repo)
        {
            regr.SetupType(t, coll, repo, null);
        }
        /// <summary>
        /// Initialise the type system with default collator and repository for a content type
        /// </summary>
        /// <param name="t">content type</param>
        public static void SetupType(this ITypeSystemRegistrar regr, Type t)
        {
            regr.SetupType(t, null, null, null);
        }
        /// <summary>
        /// Initialise the type system with default collator and repository for a content type
        /// </summary>
        /// <typeparam name="T">content type</typeparam>
        public static void SetupType<T>(this ITypeSystemRegistrar regr)
        {
            regr.SetupType(typeof(T), null, null, null);
        }
        
        /// <summary>
        /// Initialise the core dbcontext, the collator, the repository for a content type so as to use basic persistence
        /// </summary>
        /// <typeparam name="T">content type</typeparam>
        public static void SetupTypeForBasic<T>(this ITypeSystemRegistrar regr)
        {
            regr.SetupType(typeof(T), new BasicCollator(regr.System), new BasicRepository(regr.System, new CoreDataSourceFactory(regr.System)));
        }
   
    }
}
