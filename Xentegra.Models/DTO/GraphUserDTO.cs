using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models.DTO
{
    public class GraphUserDTO
    {
        public string UserPrincipalName { get; set; }
        public string UserId { get; set; }
        public IList<GraphGroupDTO> Groups { get; set; } = new List<GraphGroupDTO>();
    }
}
