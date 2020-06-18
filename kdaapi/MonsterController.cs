using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using DevelopingInsanity.KDM.kdaapi.DataModels;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace DevelopingInsanity.KDM.kdaapi
{
    public class MonsterBuildParameter
    {
        public string Monster { get; set; }
        public string Level { get; set; }
        public string Version { get; set; }

        public MonsterBuildParameter()
        {
        }
    }

    [Route("monsters")]
    [ApiController]
    public class MonsterController : ControllerBase
    {
        private const string API_URI = "https://kdaapi.azurewebsites.net"; //TODO: put in configuration

        [HttpGet]
        public IEnumerable<string> Get()
        {
            bool filterByLevel = false, filterByExpansion = false, filterByVersion = false;
            string[] levelStrings = new string[0], expansionStrings = new string[0], versionStrings = new string[0];

            if (HttpContext.Request.Query.ContainsKey("level"))
            {
                filterByLevel = true;
                levelStrings = HttpContext.Request.Query["level"].ToString().Split(",");
            }

            if (HttpContext.Request.Query.ContainsKey("exp"))
            {
                filterByExpansion = true;
                expansionStrings = HttpContext.Request.Query["exp"].ToString().Split(",");
            }

            if (HttpContext.Request.Query.ContainsKey("ver"))
            {
                filterByVersion = true;
                versionStrings = HttpContext.Request.Query["ver"].ToString().Split(",");
            }


            CloudTable table = DataConnection.TableClient.GetTableReference(MonsterEntity.TABLE_NAME);
            var entities = table.ExecuteQuery(new TableQuery<MonsterEntity>()).ToList();
            List<string> result = new List<string>();
            foreach (var entity in entities)
            {
                bool add = true;

                if (add && filterByLevel)
                {
                    add = false;

                    foreach (string l in levelStrings)
                        if (entity.RowKey.Equals(l.Trim(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            add = true;
                            break;
                        }
                }

                if (add && filterByExpansion)
                {
                    add = false;
                    string[] availableExpansions = entity.Expansion.Split(",");

                    foreach (string exp in expansionStrings)
                    {
                        if (add)
                            break;

                        foreach (string cmp in availableExpansions)
                            if (exp.Trim().Equals(cmp.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                add = true;
                                break;
                            }
                    }
                }

                if (add && filterByVersion)
                {
                    add = false;
                    string[] availableVersions = entity.Version.Split(",");

                    foreach (string ver in versionStrings)
                    {
                        if (add)
                            break;

                        foreach (string cmp in availableVersions)
                            if (ver.Trim().Equals(cmp.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                add = true;
                                break;
                            }
                    }
                }

                if (add)
                    result.Add(entity.Serialize());
            }
            return result;
        }

        [HttpPost]
        public void Post([FromBody] MonsterBuildParameter value)
        {

            MonsterInstanceEntity entity = null;
            try
            {
                entity = MonsterInstanceEntity.Generate(DataConnection.TableClient, value.Monster, value.Level, value.Version);
            }
            catch (Exception)
            {
            }

            if (entity != null)
            {
                HttpContext.Response.StatusCode = 201;

                HttpContext.Response.Headers.Add("Entity", new StringValues(entity.Serialize()));

                HttpContext.Response.Headers.Add("Location", new StringValues(new Uri($"{API_URI}/sessions/{entity.PartitionKey}").ToString()));
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
    }
}
