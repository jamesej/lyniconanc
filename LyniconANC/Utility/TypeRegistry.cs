using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    /// <summary>
    /// A registry of handlers for different types
    /// </summary>
    /// <typeparam name="I">The type of a type handler</typeparam>
    /// <typeparam name="DefaultT">The default type handler type (when nothing else is registered)</typeparam>
    public class TypeRegistry<I>
    {
        protected Dictionary<Type, I> registered = new Dictionary<Type, I>();
        private I defaultHandler = default(I);

        /// <summary>
        /// Get the default handler
        /// </summary>
        public I DefaultHandler
        {
            get { return defaultHandler; }
            protected set { defaultHandler = value; }
        }

        /// <summary>
        /// Register a handler for a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="typeHandler">The handler for the type</param>
        public virtual void Register(Type type, I typeHandler)
        {
            if (type == null)
                defaultHandler = typeHandler;
            else
                registered[type] = typeHandler;
        }

        /// <summary>
        /// Get the registered handler for a type
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Handler registered for type (or default if none registered)</returns>
        public virtual I Registered(Type type)
        {
            I typeHandler = registered.FirstSelectOrDefault(r => r.Key.IsAssignableFrom(type), r => r.Value);
            if (typeHandler == null)
                typeHandler = defaultHandler;
            return typeHandler;
        }
        /// <summary>
        /// Get the registered handler for a type
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>Handler registered for type (or default if none registered)</returns>
        public I Registered<T>()
        {
            return Registered(typeof(T));
        }
    }
}
