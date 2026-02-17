namespace InventarioApp.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal PrecioTotal => Cantidad * Precio;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
