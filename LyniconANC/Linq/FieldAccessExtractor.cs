using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Linq
{
    public static class FieldAccessExtractorX
    {
        public static HashSet<string> ExtractFields(this IQueryable iq)
        {
            var fae = new FieldAccessExtractor();
            return fae.ExtractFields(iq);
        }
    }

    /// <summary>
    /// An expression visitor which records all the members of the element type of an IQueryable
    /// which are referenced in the query.  Can be used to detect whether a query is safe to use
    /// on a facade where the facade type has more members than the underlying type.
    /// </summary>
    public class FieldAccessExtractor : ExpressionVisitor
    {
        private Type elementType { get; set; }
        
        /// <summary>
        /// The names of the members of the underlying type which are accessed in the query
        /// </summary>
        public HashSet<string> AccessedFields { get; set; }

        public FieldAccessExtractor()
        {
            AccessedFields = new HashSet<string>();
        }

        /// <summary>
        /// Extract the members of the element type of an IQueryable that are accessed within it
        /// </summary>
        /// <param name="iq">The IQueryable</param>
        /// <returns>The names of the members of the type accessed in the IQueryable</returns>
        public HashSet<string> ExtractFields(IQueryable iq)
        {
            elementType = iq.ElementType;
            Visit(iq.Expression);
            return AccessedFields;
        }
        /// <summary>
        /// Extract the members of the element type of an operator on an IQueryable<T> that are accessed within it
        /// </summary>
        /// <typeparam name="T">The element type of the IQueryable the operator works on</typeparam>
        /// <param name="queryBody">The operator on IQueryable<T></param>
        /// <returns>The names of the members of the item type access in the operator</returns>
        public HashSet<string> ExtractFields<T>(Func<IQueryable<T>, IQueryable<T>> queryBody)
        {
            var dummyIq = queryBody(new List<T>().AsQueryable());
            return ExtractFields(dummyIq);
        }

        /// <summary>
        /// Deals with any inner facades, continuing search inside them
        /// </summary>
        /// <param name="node">Constant expression</param>
        /// <returns>The same expression, having scanned for member accesses</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsGenericType && node.Type.GetGenericTypeDefinition() == typeof(FacadeTypeQueryable<>))
            {
                var provider = ((IQueryable)node.Value).Provider as FacadeTypeQueryProvider;
                
                if (provider != null)
                {
                    var inner = new FieldAccessExtractor();
                    foreach (var field in inner.ExtractFields(provider.Source.InnerIQueryable))
                        this.AccessedFields.Add(field);
                }
            }
            // Fix up the Expression tree to work with the underlying LINQ provider
            //if ()
            //{

            //    var provider = ((IQueryable)node.Value).Provider as FacadeTypeQueryProvider;

            //    if (provider != null)
            //    {
            //        try
            //        {
            //            return provider.Source.InnerIQueryable.Expression;
            //        }
            //        catch (Exception ex)
            //        {
            //            throw ex;
            //        }
            //    }

            //    return Source.InnerIQueryable.Expression;
            //}

            return base.VisitConstant(node);
        }

        /// <summary>
        /// Record any members of the element type accessed in a member expression
        /// </summary>
        /// <param name="node">MemberExpression</param>
        /// <returns>The same MemberExpression, scanning it for members of the element type</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType.IsAssignableFrom(elementType))
            {
                AccessedFields.Add(node.Member.Name);
            }

            return base.VisitMember(node);
        }
    }
}
