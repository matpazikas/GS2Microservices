using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using StackExchange.Redis;
using GS_Microservices_Sem2.Controllers;
using GS_Microservices_Sem2.Models;
using GS_Microservices_Sem2.Services;

namespace GS_Microservices_Sem2.Tests
{
    public class ConsumoControllerTests
    {
        private readonly Mock<IMongoCollection<Consumo>> _mockMongoCollection;
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<MongoService> _mockMongoService;
        private readonly Mock<RedisService> _mockRedisService;

        public ConsumoControllerTests()
        {
            _mockMongoCollection = new Mock<IMongoCollection<Consumo>>();
            _mockDatabase = new Mock<IDatabase>();
            _mockMongoService = new Mock<MongoService>();
            _mockRedisService = new Mock<RedisService>();

            _mockMongoService
           .Setup(m => m.GetCollection<Consumo>("Consumo"))
           .Returns(_mockMongoCollection.Object);

            _mockRedisService
                .Setup(r => r.GetDatabase())
                .Returns(_mockDatabase.Object);
        }

        [Fact]
        public async Task SalvarConsumo_DeveRetornarCreatedComDadosValidos()
        {
            var controller = new ConsumoController(_mockMongoService.Object, _mockRedisService.Object);
            var consumo = new Consumo { NomeAparelho = "TV", ConsumoMedio = 100.5 };

            var result = await controller.SalvarConsumo(consumo) as CreatedResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal($"/consumo/{consumo.Id}", result.Location);
        }

        [Fact]
        public async Task SalvarConsumo_DeveRetornarBadRequestSeDadosInvalidos()
        {
            var controller = new ConsumoController(_mockMongoService.Object, _mockRedisService.Object);
            var consumo = new Consumo { NomeAparelho = "", ConsumoMedio = 0 };

            var result = await controller.SalvarConsumo(consumo) as BadRequestObjectResult;

            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Os campos 'NomeAparelho' e 'ConsumoMedio' são obrigatórios e válidos.", result.Value);
        }

        [Fact]
        public async Task ObterConsumos_DeveRetornarDadosDoRedisSeExistirem()
        {
            var consumos = new List<Consumo> { new Consumo { NomeAparelho = "Geladeira", ConsumoMedio = 150.0 } };
            _mockDatabase
                .Setup(db => db.StringGetAsync("Consumos", CommandFlags.None))
                .ReturnsAsync(JsonConvert.SerializeObject(consumos));

            var controller = new ConsumoController(_mockMongoService.Object, _mockRedisService.Object);

            var result = await controller.ObterConsumos() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var consumosResult = result.Value as List<Consumo>;
            Assert.Single(consumosResult);
        }

        [Fact]
        public async Task ObterConsumos_DeveRetornarNotFoundSeNenhumDadoExistir()
        {
            var consumos = new List<Consumo>();
            var mockCursor = new Mock<IAsyncCursor<Consumo>>();
            mockCursor
                .SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .Setup(c => c.Current)
                .Returns(consumos);

            _mockMongoCollection
                .Setup(m => m.FindAsync(It.IsAny<FilterDefinition<Consumo>>(), null, default))
                .ReturnsAsync(mockCursor.Object);

            var controller = new ConsumoController(_mockMongoService.Object, _mockRedisService.Object);

            var result = await controller.ObterConsumos() as NotFoundObjectResult;

            Assert.NotNull(result);
            Assert.Equal(404, result.StatusCode);
            Assert.Equal("Nenhum consumo registrado.", result.Value);
        }

        [Fact]
        public async Task ObterConsumos_DeveAtualizarRedisSeNaoExistiremDadosNoCache()
        {
            var consumos = new List<Consumo> { new Consumo { NomeAparelho = "Ar Condicionado", ConsumoMedio = 350.0 } };
            var mockCursor = new Mock<IAsyncCursor<Consumo>>();
            mockCursor
                .SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .Setup(c => c.Current)
                .Returns(consumos);

            _mockMongoCollection
                .Setup(m => m.FindAsync(It.IsAny<FilterDefinition<Consumo>>(), null, default))
                .ReturnsAsync(mockCursor.Object);

            _mockDatabase
                .Setup(db => db.StringGetAsync("Consumos", CommandFlags.None))
                .ReturnsAsync((string)null);

            var controller = new ConsumoController(_mockMongoService.Object, _mockRedisService.Object);

            var result = await controller.ObterConsumos() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            _mockDatabase.Verify(db => db.StringSetAsync(
                "Consumos",
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
                Times.Once);
        }
    }
}
