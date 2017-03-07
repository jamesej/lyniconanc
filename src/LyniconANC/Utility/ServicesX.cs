using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Utility
{
    public static class ServicesX
    {
        /// <summary>
        /// Create an instance of an object injecting services into all initial constructor parameters marked with FromServicesAttribute
        /// while supplying a list of values for subsequent parameters.  Multiple constructors can be selected between on the basis that
        /// they all include all the services attributes as their initial parameters by matching subsequent parameters to the
        /// supplied list of values.
        /// </summary>
        /// <param name="svcs">Service provider</param>
        /// <param name="instanceType">the instance type to create</param>
        /// <param name="parms">list of non-service parameter values</param>
        /// <returns>newly constructed object</returns>
        public static object CreateInstanceWithParameters(this IServiceProvider svcs, Type instanceType, params object[] parms)
        {
            Type[] paramTypes = parms.Select(p => p.GetType()).ToArray();
            var cons = instanceType.GetConstructorByNonServiceParams(paramTypes);
            if (cons == null)
                throw new Exception($"Cannot construct module of type {instanceType.FullName} {(paramTypes.Length == 0 ? "without parameters" : "using parameter types")} {paramTypes.Select(t => t.FullName).Join(", ")}");

            var serviceVals = cons.GetParameters()
                .Where(p => p.GetCustomAttribute<FromServicesAttribute>() != null)
                .Select(p => svcs.GetService(p.ParameterType));

            var allVals = serviceVals.Concat((IEnumerable<object>)parms).ToArray();
            // Adjust values to work with a final 'params' argument in the constructor if necessary
            var conformedVals = cons.GetParameters().ConformParameterValuesToParameters(allVals);

            return cons.Invoke(conformedVals);
        }

        public static T CreateInstanceWithParameters<T>(this IServiceProvider svcs, params object[] parms)
        {
            return (T)svcs.CreateInstanceWithParameters(typeof(T), parms);
        }
    }
}
