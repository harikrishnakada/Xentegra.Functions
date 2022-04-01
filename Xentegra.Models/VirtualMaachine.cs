using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models
{
    public class VirtualMaachine
    {
        public string id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public bool isTurnedOn { get; set; }
        public string resourceGroupName { get; set; }
    }
}
