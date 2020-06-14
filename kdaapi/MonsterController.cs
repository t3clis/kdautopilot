using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevelopingInsanity.KDM.kdaapi
{
    [Route("api/monsters")]
    [ApiController]
    public class MonsterController : ControllerBase
    {
        // GET: api/Monster
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "White Lion", "Screaming Antelope" };
        }

        // GET: api/Monster/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Monster
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT: api/Monster/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
