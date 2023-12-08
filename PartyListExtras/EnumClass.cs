using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PartyListExtras
{
    /// <summary>
    /// Base for enumeration class pattern, basically an enum with required properties
    /// </summary>
    internal class EnumClass : IComparable
    {
        internal EnumClass() { }

        public required int id;

        public bool Equals(EnumClass? other)
        {
            if (other == null) return false;
            return other.id == this.id;
        }

        public static IEnumerable<T> GetAll<T>() where T : EnumClass
        {
            return typeof(T).GetFields(
                BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        int IComparable.CompareTo(object? obj)
        {
            throw new NotImplementedException();
        }
    }
}
