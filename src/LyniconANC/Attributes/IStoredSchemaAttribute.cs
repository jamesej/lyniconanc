using System;
using System.Collections.Generic;
using System.Text;

namespace Lynicon.Attributes
{
    public interface IStoredSchemaAttribute
    {
        string Serialize();
        void Deserialize(string serialized);
    }
}
