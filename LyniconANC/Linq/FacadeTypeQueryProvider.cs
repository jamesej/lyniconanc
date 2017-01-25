using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lynicon.Utility;

namespace Lynicon.Linq
{
    /// <summary>
    /// Query provider base class for a facaded IQueryable for all differently typed facaded IQueryables 
    /// </summary>
    public abstract class FacadeTypeQueryProvider : ExpressionVisitor
    {
        private readonly FacadedIQueryable _source;

        protected FacadeTypeQueryProvider(IQueryable source)
        {
            if (source == null || !(source is FacadedIQueryable))
            {
                throw new ArgumentNullException("source");
            }

            _source = (FacadedIQueryable)source;
        }

        public abstract Expression DropFacade(Expression exp);

        /// <summary>
        /// The original IQueryable before any facades were applied
        /// </summary>
        internal FacadedIQueryable Source
        {
            get { return _source; }
        }
    }

    /// <summary>
    /// Just stores an IQueryable together with its facade type as an IQueryable
    /// </summary>
    internal class FacadedIQueryable : IQueryable
    {
        public FacadedIQueryable(IQueryable inner, Type facadeType)
        {
            InnerIQueryable = inner;
            FacadeType = facadeType;
        }

        public IQueryable InnerIQueryable { get; set; }
        public Type FacadeType { get; set; }

        #region IQueryable Members

        public Type ElementType
        {
            get { return InnerIQueryable.ElementType; }
        }

        public Expression Expression
        {
            get { return InnerIQueryable.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return InnerIQueryable.Provider; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// A query provider for facaded IQueryables.  This is also an ExpressionVisitor so it contains the
    /// functionality for converting expressions needed for managing facades.
    /// </summary>
    /// <typeparam name="T">The type of the IQueryable</typeparam>
    public class FacadeTypeQueryProvider<T> : FacadeTypeQueryProvider, IQueryProvider
    {
        public FacadeTypeQueryProvider(IQueryable source)
            : base(source is FacadedIQueryable ? source : new FacadedIQueryable(source, typeof(T)))
        {
            PropertyMap = new Dictionary<string, string>();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            return new FacadeTypeQueryable<TElement>(Source, expression) as IQueryable<TElement>;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            Type elementType = expression.Type.GetGenericArguments().First();
            IQueryable result = (IQueryable)Activator.CreateInstance(typeof(FacadeTypeQueryable<>).MakeGenericType(elementType),
                    new object[] { Source, expression });
            return result;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            object result = (this as IQueryProvider).Execute(expression);
            return (TResult)result;
        }

        public object Execute(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            Expression translated = DropFacade(expression);
            return Source.Provider.Execute(translated);
        }

        internal IEnumerable ExecuteEnumerable(Expression expression)
        {
            return CreateUnderlyingQuery(expression);
        }

        /// <summary>
        /// Takes an expression in the facade type, convert it to an expression in the underlying query type
        /// and create a query from the source in that type
        /// </summary>
        /// <param name="expression">Expression in facade type</param>
        /// <returns>IQueryable in underlying type</returns>
        internal IQueryable CreateUnderlyingQuery(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");
            Expression translated = DropFacade(expression);
            return Source.Provider.CreateQuery(translated);
        }

        public Type FacadeType
        {
            get { return this.Source.FacadeType; }
        }

        /// <summary>
        /// Converts all elements in the expression in terms of the FacadeType into the source element type
        /// </summary>
        /// <param name="expression">expression to convert type references from one type to another</param>
        /// <returns>converted expression</returns>
        public override Expression DropFacade(Expression expression)
        {
            this.from = this.Source.FacadeType;
            this.to = this.Source.ElementType;
            return this.Visit(expression);
        }

        /// <summary>
        /// Convert a constant.  This just has to deal with the case where the constant is another
        /// FacadeTypeQueryable (to which other Queryable operators have been applied)
        /// to handle chaining of AsFacade calls to different types.  It simply
        /// pulls the expression out of the inner FacadeTypeQueryable and continues converting it
        /// as all the expressions down to the original source will need converting to the
        /// underlying type of the source.
        /// </summary>
        /// <param name="node">Expression node</param>
        /// <returns>Converted expression</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            // Fix up the Expression tree to work with the underlying LINQ provider
            if (node.Type.IsGenericType &&
                node.Type.GetGenericTypeDefinition() == typeof(FacadeTypeQueryable<>))
            {

                var provider = ((IQueryable)node.Value).Provider as FacadeTypeQueryProvider;

                if (provider != null)
                {
                    try
                    {
                        return provider.Source.InnerIQueryable.Expression;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                return Source.InnerIQueryable.Expression;
            }

            return base.VisitConstant(node);
        }

        private Type from, to;

        private Dictionary<ParameterExpression, ParameterExpression> parameterMappings = new Dictionary<ParameterExpression, ParameterExpression>();

        protected internal Dictionary<string, string> PropertyMap { get; set; }
        /// <summary>
        /// Convert the type of a parameter to the underlying type if necessary
        /// </summary>
        /// <param name="node">ParameterExpression node</param>
        /// <returns>Converted expression</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Type changedType = ReflectionX.SubstituteType(node.Type, from, to);
            if (changedType != null)
            {
                // Ensure the same converted parameter object is used whereever the same parameter appears in the original expression
                if (!parameterMappings.ContainsKey(node))
                    parameterMappings.Add(node, Expression.Parameter(changedType, node.Name));
                return parameterMappings[node];
            }

            return base.VisitParameter(node);
        }

        /// <summary>
        /// Convert the type of a lambda in T if T is or contains the facade type
        /// to a lambda with T converted partially or totally to the underlying type
        /// </summary>
        /// <typeparam name="T">Type of lambda</typeparam>
        /// <param name="node">Lambda expression</param>
        /// <returns>Converted (if necessary) lambda expression</returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Type changedType = ReflectionX.SubstituteType(typeof(T), from, to);
            if (changedType != null)
                return Expression.Lambda(changedType, Visit(node.Body),
                    node.Parameters.Select(p => (ParameterExpression)Visit(p)));

            return base.VisitLambda<T>(node);
        }

        /// <summary>
        /// Convert a member access of the facade type to a member access of the underlying type
        /// </summary>
        /// <param name="node">Member access expression</param>
        /// <returns>Converted (if necessary) member access expression</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType.IsAssignableFrom(from))
            {
                string underlyingMemberName = PropertyMap.ContainsKey(node.Member.Name) ? PropertyMap[node.Member.Name] : node.Member.Name;
                var mInfo = to.GetMember(
                        underlyingMemberName,
                        node.Member.MemberType,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (mInfo.Length < 1)
                    throw new InvalidOperationException("Facade translation of member access failed, type " + to.FullName + " does not have " + node.Member.MemberType + " " + node.Member.Name);
                return Expression.MakeMemberAccess(Visit(node.Expression), mInfo.Single());
            }

            return base.VisitMember(node);
        }

        /// <summary>
        /// Convert a generic method call in the facade type or a type containing it to one
        /// in the underlying type or a type containing it
        /// </summary>
        /// <param name="node">Method call expression</param>
        /// <returns>Converted (if necessary) method call expression</returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var mi = node.Method;
            if (mi.IsGenericMethod)
            {
                Type[] changedTypes = ReflectionX.SubstituteTypes(mi.GetGenericArguments(), from, to);
                if (changedTypes != null)
                {
                    MethodInfo newMi = mi.GetGenericMethodDefinition().MakeGenericMethod(changedTypes);
                    if (node.Object == null)
                        return Expression.Call(newMi,
                            node.Arguments.Select(a => Visit(a)));
                    else
                        return Expression.Call(node.Object, newMi,
                            node.Arguments.Select(a => Visit(a)));
                }
            }
            return base.VisitMethodCall(node);
        }
    }
}
