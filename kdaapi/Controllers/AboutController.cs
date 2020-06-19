using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DevelopingInsanity.KDM.kdaapi.Controllers
{
    public class About
    {
        public string Company { get; set; }
        public string Package { get; set; }
        public string Version { get; set; }

        public About()
        {
            Company = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            Version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Package = Assembly.GetEntryAssembly().GetName().Name;
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    [Route("about")]
    [ApiController]
    public class AboutController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { new About().Serialize() };
        }
    }
}
