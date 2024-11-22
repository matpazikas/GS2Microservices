using GS_Microservices_Sem2.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using StackExchange.Redis;
using GS_Microservices_Sem2.Models;

namespace GS_Microservices_Sem2.Controllers
{
    [Route("api/[controller]")]
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
        public async Task<IActionResult> SalvarConsumo([FromBody] Consumo consumo)
        {
            if (string.IsNullOrEmpty(consumo.NomeAparelho) || consumo.ConsumoMedio <= 0)
            {
                return BadRequest("Os campos 'NomeAparelho' e 'ConsumoMedio' são obrigatórios e válidos.");
            }

            await _consumoCollection.InsertOneAsync(consumo);

            const string cacheKey = "Consumos";
            var consumos = await _consumoCollection.Find(_ => true).ToListAsync();
            await _redisDatabase.StringSetAsync(cacheKey, JsonConvert.SerializeObject(consumos), TimeSpan.FromMinutes(5)); // Armazenar em cache de forma assíncrona

            return Created($"/consumo/{consumo.Id}", consumo);
        }

        [HttpGet]
        public async Task<IActionResult> ObterConsumos()
        {
            const string cacheKey = "Consumos";
            var cachedData = await _redisDatabase.StringGetAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var consumosCache = JsonConvert.DeserializeObject<List<Consumo>>(cachedData);
                return Ok(consumosCache);
            }

            var consumos = await _consumoCollection.Find(_ => true).ToListAsync();
            if (!consumos.Any())
            {
                return NotFound("Nenhum consumo registrado.");
            }

            await _redisDatabase.StringSetAsync(cacheKey, JsonConvert.SerializeObject(consumos), TimeSpan.FromMinutes(5));

            return Ok(consumos);
        }

        //[HttpGet]
        //public async Task<IActionResult> ObterConsumos()
        //{
        //    // Buscar os dados diretamente do MongoDB
        //    var consumos = await _consumoCollection.Find(_ => true).ToListAsync();

        //    // Verifica se existem dados
        //    if (!consumos.Any())
        //    {
        //        return NotFound("Nenhum consumo registrado.");
        //    }

        //    return Ok(consumos);
        //}
    }
}
