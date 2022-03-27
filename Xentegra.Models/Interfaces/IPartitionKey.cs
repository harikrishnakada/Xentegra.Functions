using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models.Interfaces
{
    internal interface IPartitionKey
    {
        void SetPartitionKey();
        string pk { get; set; }
    }
}
