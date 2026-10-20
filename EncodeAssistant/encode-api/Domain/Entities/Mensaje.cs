namespace EncodeAssistant.EncodeApi.Domain.Entities
{
    public class Mensaje
    {
        public int Id { get; set; }
        public string? Consulta { get; set; }
        public DateTime FechaCreacion { get; set; }

        public bool Estado { get; set; }
  
    }
}