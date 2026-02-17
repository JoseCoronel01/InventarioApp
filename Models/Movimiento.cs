namespace InventarioApp.Models
{
    public enum TipoMovimiento
    {
        Entrada=0,
        Salida=1
    }

    public class Movimiento
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total => Cantidad * Precio;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
