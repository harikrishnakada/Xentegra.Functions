using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xentegra.Models.Constants
{
    public static class LookupType
    {
       // public const string Technology = "TECHNOLOGY";
    }

    public static class EntityType<T> where T : class
    {
        public static string GetEntityType()
        {
            return typeof(T).ToString();
        }
    }

    public static class RequestType
    {
        public const string Demo = "Demo";
        public const string POC = "POC";
    }

    public enum RequestStatus
    {
        Approved,
        Declined,
        Pending
    }
}
