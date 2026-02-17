using System.Text.Json;
using InventarioApp.Models;
using Microsoft.JSInterop;

namespace InventarioApp.Services
{
    public class InventarioService
    {
        private const string STORAGE_KEY_MATERIALS = "materials";
        private const string STORAGE_KEY_MOVEMENTS = "movements";
        private readonly HttpClient _httpClient;
        private IJSRuntime? _jsRuntime;

        private List<Material> _materials = new();
        private List<Movimiento> _movimientos = new();
        private int _nextMaterialId = 1;
        private int _nextMovimientoId = 1;
        private bool _initialized = false;

        public InventarioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void SetJSRuntime(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                await LoadMaterials();
                await LoadMovimientos();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en inicialización: {ex.Message}");
                _initialized = true;
            }
        }

        #region Materiales

        public async Task<List<Material>> GetMaterialesAsync()
        {
            if (!_initialized) await InitializeAsync();
            return _materials;
        }

        public Material? GetMaterial(int id)
        {
            return _materials.FirstOrDefault(m => m.Id == id);
        }

        public async Task<Material> AddMaterialAsync(Material material)
        {
            if (!_initialized) await InitializeAsync();

            material.Id = _nextMaterialId++;
            material.FechaCreacion = DateTime.UtcNow;
            material.FechaActualizacion = DateTime.UtcNow;
            _materials.Add(material);
            await SaveMaterials();
            return material;
        }

        public async Task<bool> UpdateMaterialAsync(Material material)
        {
            if (!_initialized) await InitializeAsync();

            var existingMaterial = _materials.FirstOrDefault(m => m.Id == material.Id);
            if (existingMaterial == null)
                return false;

            existingMaterial.Nombre = material.Nombre;
            existingMaterial.Tipo = material.Tipo;
            existingMaterial.Cantidad = material.Cantidad;
            existingMaterial.Precio = material.Precio;
            existingMaterial.FechaActualizacion = DateTime.UtcNow;

            await SaveMaterials();
            return true;
        }

        public async Task<bool> DeleteMaterialAsync(int id)
        {
            if (!_initialized) await InitializeAsync();

            var material = _materials.FirstOrDefault(m => m.Id == id);
            if (material == null)
                return false;

            _materials.Remove(material);
            _movimientos.RemoveAll(m => m.MaterialId == id);
            await SaveMaterials();
            await SaveMovimientos();
            return true;
        }

        #endregion

        #region Movimientos

        public List<Movimiento> GetMovimientos()
        {
            return _movimientos;
        }

        public List<Movimiento> GetMovimientosPorMaterial(int materialId)
        {
            return _movimientos.Where(m => m.MaterialId == materialId).ToList();
        }

        public async Task<Movimiento> AddMovimientoAsync(Movimiento movimiento)
        {
            if (!_initialized) await InitializeAsync();

            var material = _materials.FirstOrDefault(m => m.Id == movimiento.MaterialId);
            if (material == null)
                throw new Exception("Material no encontrado");

            movimiento.Id = _nextMovimientoId++;
            movimiento.Fecha = DateTime.UtcNow;

            // Actualizar cantidad del material
            if (movimiento.Tipo == TipoMovimiento.Entrada)
            {
                material.Cantidad += movimiento.Cantidad;
                material.Precio = movimiento.Precio;
            }
            else
            {
                if (material.Cantidad < movimiento.Cantidad)
                    throw new Exception("No hay suficiente cantidad para la salida");
                material.Cantidad -= movimiento.Cantidad;
            }

            material.FechaActualizacion = DateTime.UtcNow;
            _movimientos.Add(movimiento);

            await SaveMaterials();
            await SaveMovimientos();
            return movimiento;
        }

        public async Task<bool> DeleteMovimientoAsync(int id)
        {
            if (!_initialized) await InitializeAsync();

            var movimiento = _movimientos.FirstOrDefault(m => m.Id == id);
            if (movimiento == null)
                return false;

            var material = _materials.FirstOrDefault(m => m.Id == movimiento.MaterialId);
            if (material != null)
            {
                if (movimiento.Tipo == TipoMovimiento.Entrada)
                    material.Cantidad -= movimiento.Cantidad;
                else
                    material.Cantidad += movimiento.Cantidad;

                material.FechaActualizacion = DateTime.UtcNow;
            }

            _movimientos.Remove(movimiento);
            await SaveMaterials();
            await SaveMovimientos();
            return true;
        }

        #endregion

        #region Persistencia

        private async Task LoadMaterials()
        {
            try
            {
                var json = await GetFromStorage(STORAGE_KEY_MATERIALS);
                if (!string.IsNullOrEmpty(json))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _materials = JsonSerializer.Deserialize<List<Material>>(json, options) ?? new();
                }
                else
                {
                    _materials = new();
                }

                if (_materials.Any())
                    _nextMaterialId = _materials.Max(m => m.Id) + 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando materiales: {ex.Message}");
                _materials = new();
                _nextMaterialId = 1;
            }
        }

        private async Task SaveMaterials()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_materials, options);
                await SaveToStorage(STORAGE_KEY_MATERIALS, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando materiales: {ex.Message}");
            }
        }

        private async Task LoadMovimientos()
        {
            try
            {
                var json = await GetFromStorage(STORAGE_KEY_MOVEMENTS);
                if (!string.IsNullOrEmpty(json))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _movimientos = JsonSerializer.Deserialize<List<Movimiento>>(json, options) ?? new();
                }
                else
                {
                    _movimientos = new();
                }

                if (_movimientos.Any())
                    _nextMovimientoId = _movimientos.Max(m => m.Id) + 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando movimientos: {ex.Message}");
                _movimientos = new();
                _nextMovimientoId = 1;
            }
        }

        private async Task SaveMovimientos()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_movimientos, options);
                await SaveToStorage(STORAGE_KEY_MOVEMENTS, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando movimientos: {ex.Message}");
            }
        }

        private async Task SaveToStorage(string key, string value)
        {
            try
            {
                if (_jsRuntime != null && !string.IsNullOrEmpty(value))
                {
                    await _jsRuntime.InvokeVoidAsync("storageApi.setItem", key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en localStorage: {ex.Message}");
            }
        }

        private async Task<string> GetFromStorage(string key)
        {
            try
            {
                if (_jsRuntime != null)
                {
                    return await _jsRuntime.InvokeAsync<string>("storageApi.getItem", key) ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error leyendo localStorage: {ex.Message}");
            }
            return string.Empty;
        }

        #endregion
    }
}
