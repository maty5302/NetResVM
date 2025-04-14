using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Interface
{
    /// <summary>
    /// This interface defines the properties of a lab model.
    /// </summary>
    public interface ILabModel
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        DateTime Last_modified { get; }
    }
}
