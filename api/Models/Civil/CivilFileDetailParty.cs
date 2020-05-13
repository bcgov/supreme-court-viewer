using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Newtonsoft.Json;

namespace Scv.Api.Models.Civil
{
    public class CivilFileDetailParty : CvfcParty3
    {
        [JsonProperty("roleTypeDescription", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public string RoleTypeDescription { get; set; }
        
    }
}
