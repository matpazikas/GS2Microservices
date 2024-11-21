using GS_Microservices_Sem2.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using StackExchange.Redis;
using GS_Microservices_Sem2.Models;

namespace GS_Microservices_Sem2.Controllers
{
    [Route("api/[consumo]")]
    [ApiController]
    public class ConsumoController : ControllerBase
    {
        private readonly IMongoCollection<Consumo> _consumoCollection;
        private readonly IDatabase _redisDatabase;

        public ConsumoController(MongoService mongoService, RedisService redisService)
        {
            _consumoCollection = mongoService.GetCollection<Consumo>("Consumo");
            _redisDatabase = redisService.GetDatabase();
        }

        [HttpPost]
        public IActionResult SalvarConsumo([FromBody] Consumo consumo)
        {
            if (string.IsNullOrEmpty(consumo.NomeAparelho) || consumo.ConsumoMedio <= 0)
            {
                return BadRequest("Os campos 'NomeAparelho' e 'ConsumoMedio' são obrigatórios e válidos.");
            }

            _consumoCollection.InsertOne(consumo);
            return Created($"/consumo/{consumo.Id}", consumo);
        }

        [HttpGet]
        public IActionResult ObterConsumos()
        {
            const string cacheKey = "Consumos";
            var cachedData = _redisDatabase.StringGet(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var consumosCache = JsonConvert.DeserializeObject<List<Consumo>>(cachedData);
                return Ok(consumosCache);
            }

            var consumos = _consumoCollection.Find(_ => true).ToList();
            if (!consumos.Any())
            {
                return NotFound("Nenhum consumo registrado.");
            }

            _redisDatabase.StringSet(cacheKey, JsonConvert.SerializeObject(consumos), TimeSpan.FromMinutes(5));
            return Ok(consumos);
        }

        [HttpGet("{id}")]
        public IActionResult ObterConsumoPorId(string id)
        {
            var consumo = _consumoCollection.Find(c => c.Id == id).FirstOrDefault();
            if (consumo == null)
            {
                return NotFound($"Consumo com o ID '{id}' não foi encontrado.");
            }

            return Ok(consumo);
        }


    }
}
