namespace EncodeAssistant.EncodeApi.Domain.Entities
{
    public class Operador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public Departamento Departamento { get; set; } = Departamento.Soporte;
    }

    public enum Departamento
    {
        Soporte,
        Desarrollo,
        Ventas,
        Marketing
    }
}