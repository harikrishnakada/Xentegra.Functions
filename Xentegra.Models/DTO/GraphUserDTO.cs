using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xentegra.Models.Constants;

namespace Xentegra.Models.DTO
{
    public class GraphUserDTO
    {
        public string userPrincipalName { get; set; }
        public string userId { get; set; }
        public IList<GraphGroupDTO> groups { get; set; } = new List<GraphGroupDTO>();

        public OperationTypes action { get; set; }
    }
}
