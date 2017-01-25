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
        public static object CreateInstanceWithParameters(this IServiceProvider svcs, Type instanceType, params object[] parms)
        {
            Type[] paramTypes = parms.Select(p => p.GetType()).ToArray();
            var cons = instanceType.GetConstructorByNonServiceParams(paramTypes);
            if (cons == null)
                throw new Exception($"Cannot construct module of type {instanceType.FullName} using parameter types {paramTypes.Select(t => t.FullName).Join(", ")}");

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
