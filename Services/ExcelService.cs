using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.IO;

namespace PokemonWebApp.Services
{
    public class ExcelService
    {
        public async Task<byte[]> GenerateExcelReportAsync(string name, string species, ILogger logger, PokemonService pokemonService)
        {
            logger.LogInformation("Generating Excel report at {Time}", DateTime.Now);
            const int PageSize = 1000; // máximo a exportar (ajustable)
            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("Orlando Ruben De La Rosa Garcia");

                var allPokemon = await pokemonService.GetAllPokemonAsync(logger);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    allPokemon = allPokemon
                        .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(species))
                {
                    allPokemon = allPokemon
                        .Where(p => p.name.Contains(species.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                //Cargo las url de las imagenes de todos los pokemon antes de exportar a excel
                allPokemon = await pokemonService.setSpritePokemon(logger, allPokemon);

                var exportList = allPokemon.Take(PageSize).ToList(); // evita exportar miles

                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add("Pokémon");
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nombre";
                worksheet.Cells[1, 3].Value = "Imagen";

                int row = 2;
                foreach (var p in allPokemon)
                {
                    var id = p.url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
                    worksheet.Cells[row, 1].Value = id;
                    worksheet.Cells[row, 2].Value = p.name;
                    worksheet.Cells[row, 3].Value = p.imageUrl;

                    row++;
                }

                worksheet.Cells.AutoFitColumns();

                var excelBytes = package.GetAsByteArray();
                var fileName = $"Pokemon-List-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                // devolver el archivo como un byte array
                return excelBytes;
            }
            catch (Exception ex)
            {
                logger.LogError("Error al exportar a Excel - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                //Devolver un array vacío en caso de error
                return Array.Empty<byte>();
            }
        }
    }
    
}
