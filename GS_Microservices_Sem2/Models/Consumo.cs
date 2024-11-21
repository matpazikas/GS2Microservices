namespace GS_Microservices_Sem2.Models
{
    public class Consumo
    {
        public int Id { get; set; }              // ID único
        public string NomeAparelho { get; set; }    // Nome do aparelho
        public double ConsumoMedio { get; set; }    // Consumo médio em kWh

        // public DateTime DataRegistro { get; set; } = DateTime.UtcNow; // Data do registro
    }
}
