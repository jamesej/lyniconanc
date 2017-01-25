using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lynicon.Collation
{
    /// <summary>
    /// The types of content type change problems which exist
    /// </summary>
    public enum ChangeProblemType
    {
        DeletionNeeded,
        NotBinarySerializable,
        NullObjectValue,
        PropertyDroppedFromSummary,
        PropertyDropped,
        PropertyAddedToSummary
    }

    /// <summary>
    /// Represents a change to the definition of content types in the CMS that could potentially cause data loss or problems
    /// </summary>
    public class ChangeProblem
    {
        /// <summary>
        /// The type of the problem
        /// </summary>
        public ChangeProblemType ProblemType { get; set; }
        /// <summary>
        /// The name of the property (if any) involved in the problem
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// The name of the content type affected by the problem
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// The Id of the problem
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Whether the problem could cause data loss
        /// </summary>
        public bool IsDataProblem
        {
            get
            {
                switch (ProblemType)
                {
                    case ChangeProblemType.DeletionNeeded:
                    case ChangeProblemType.PropertyDroppedFromSummary:
                    case ChangeProblemType.PropertyDropped:
                    case ChangeProblemType.PropertyAddedToSummary:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Create an empty change problem
        /// </summary>
        public ChangeProblem()
        {
            Id = Guid.NewGuid();
        }
        /// <summary>
        /// Create a change problem
        /// </summary>
        /// <param name="typeName">The type affected by the problem</param>
        /// <param name="propertyName">The property (if any) affected by the problem</param>
        /// <param name="problemType">The kind of problem</param>
        public ChangeProblem(string typeName, string propertyName, ChangeProblemType problemType) : this()
        {
            ProblemType = problemType;
            PropertyName = propertyName;
            TypeName = typeName;
        }
    }
}
