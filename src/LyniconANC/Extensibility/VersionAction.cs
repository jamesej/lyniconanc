using System;
using System.Collections.Generic;
using System.Text;

namespace Lynicon.Extensibility
{
    public class VersionAction : IEquatable<VersionAction>
    {
        public DataOp Op { get; set; }
        public ItemVersion Version { get; set; }

        public VersionAction()
        { }
        public VersionAction(ItemVersion version, string key, VersionKeyAction keyAction)
        {
            Op = keyAction.Op;
            Version = version.Superimpose(new ItemVersion((key, keyAction.Value)));
        }

        // Equality

        public override int GetHashCode()
        {
            // Note the hash is independent of the order of the keys in the dictionary
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Op.GetHashCode();
                hash = hash * 23 + Version.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(VersionAction lhs, VersionAction rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;

                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(VersionAction lhs, VersionAction rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ItemVersion);
        }

        public bool Equals(VersionAction other)
        {
            if (other == null)
                return false;

            return this.Op == other.Op && this.Version == other.Version;
        }

        public override string ToString()
        {
            return $"{Version}|{Op}";
        }
    }
}
