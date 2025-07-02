using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml;
using PokemonWebApp.Models;
using PokemonWebApp.Services;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Xml.Linq;

namespace PokemonWebApp.Controllers
{
    public class PokemonController : Controller
    {
        private readonly PokemonService _pokemonService;
        private readonly ILogger<PokemonController> _logger;
        private const int PageSize = 20;
        private readonly ExcelService _excelService;
        private readonly EmailSettings _emailSettings;

        public PokemonController(PokemonService pokemonService, ILogger<PokemonController> logger, ExcelService excelService, IOptions<EmailSettings> emailOptions)
        {
            _pokemonService = pokemonService;
            _logger = logger;
            _excelService = excelService;
            _emailSettings = emailOptions.Value;
        }


        public async Task<IActionResult> Index(int page = 1)
        {
            string name = string.Empty;
            _logger.LogInformation("Entro a accion Index del controller - {Time}", DateTime.Now);
            const int PageSize = 20;
            var allPokemon = await _pokemonService.GetAllPokemonAsync(_logger);

            if (!string.IsNullOrWhiteSpace(name))
            {
                allPokemon = allPokemon
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalCount = allPokemon.Count;
            var pagedData = await _pokemonService.setSpritePokemon(_logger, allPokemon
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList());

            var model = new PokemonPageModel
            {
                Pokemons = pagedData,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
            };



            return View(model);
        }



        public async Task<IActionResult> Filter(string name = "", string species = "", int page = 1)
        {
            const int PageSize = 20;
            var allPokemon = await _pokemonService.GetAllPokemonAsync(_logger);

            if (!string.IsNullOrWhiteSpace(name))
            {
                allPokemon = allPokemon
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                allPokemon = allPokemon.Where(p => p.name.Contains(species.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalCount = allPokemon.Count;
            var pagedData = await _pokemonService.setSpritePokemon(_logger, allPokemon
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList());

            var model = new PokemonPageModel
            {
                Pokemons = pagedData,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
            };

            return PartialView("PokemonListPartial", model);
        }

        public async Task<IActionResult> GetSpeciesList()
        {
            var list = await _pokemonService.GetAllSpeciesAsync(_logger, 200);
            var simplified = list.Select(s => new { s.Name }).OrderBy(s => s.Name);
            return Json(simplified);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string name = "", string species = "")
        {
            const int PageSize = 1000; // máximo a exportar (ajustable)
            try
            {
                ExcelPackage.License.SetNonCommercialPersonal("Orlando Ruben De La Rosa Garcia");

                var allPokemon = await _pokemonService.GetAllPokemonAsync(_logger);


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
                allPokemon = await _pokemonService.setSpritePokemon(_logger, allPokemon);

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

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al exportar a Excel - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                return StatusCode(500, "Error al generar el archivo Excel. Por favor, inténtelo de nuevo más tarde.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcelFiltered(string name = "", string species = "")
        {
            var fileName = $"Pokemon-List-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            
            byte[] excelBytes = await _excelService.GenerateExcelReportAsync(name, species, _logger, _pokemonService);
            return new FileContentResult(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        [HttpGet]
        public async Task<IActionResult> CountFiltered(string name = "", string species = "")
        {
            var all = await _pokemonService.GetAllPokemonAsync(_logger);

            if (!string.IsNullOrWhiteSpace(name))
            {
                all = all
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                all = all
                    .Where(p => p.name.Contains(species.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(new { count = all.Count });
        }

        [HttpPost]
        public async Task<IActionResult> SendReportByEmail([FromBody] EmailReportRequest request)
        {
            try
            {

                // Se genera el reporte Excel
                var bytes = await _excelService.GenerateExcelReportAsync(request.Name, request.Species, _logger, _pokemonService);

                // Enviar por correo
                await SendEmailWithAttachmentAsync(request.Email, "Reporte de Pokémon", "Adjunto se encuentra el archivo con el reporte filtrado.", bytes, "reporte-pokemon.xlsx");

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

     
        private async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] fileBytes, string filename)
        {
            _logger.LogInformation("Entro a SendEmailWithAttachmentAsync - {Time}", DateTime.Now);
            try
            {
                using var message = new MailMessage(_emailSettings.From, toEmail, subject, body);
                message.Attachments.Add(new Attachment(new MemoryStream(fileBytes), filename));

                using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.User, _emailSettings.Password),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al enviar el correo electrónico: {Error}", ex.Message);
                throw;  // Re-lanzar la excepción para que el controlador pueda manejarla
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(string name)
        {
           

            var result = await _pokemonService.getDetails(_logger, name);

            return Json(result);
        }
    }
}
