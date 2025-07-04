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
            var allPokemon = await _pokemonService.GetAllPokemonAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                allPokemon = allPokemon
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalCount = allPokemon.Count;
            var pagedData = await _pokemonService.setSpritePokemon(allPokemon
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
            var allPokemon = await _pokemonService.GetAllPokemonAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                allPokemon = allPokemon
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                // Obtener todos los nombres de la cadena evolutiva de la especie seleccionada
                var evolutionNames = await _pokemonService.GetEvolutionChainPokemonNamesAsync(species);

                if (evolutionNames.Any())
                {
                    allPokemon = allPokemon
                        .Where(p => evolutionNames.Contains(p.name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    allPokemon = new List<Pokemon>(); // si no hay cadena, vacía
                }
            }

            int totalCount = allPokemon.Count;
            var pagedData = await _pokemonService.setSpritePokemon(allPokemon
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
            var list = await _pokemonService.GetAllSpeciesAsync(200);
            var simplified = list.Select(s => new { s.Name }).OrderBy(s => s.Name);
            return Json(simplified);
        }

        

        [HttpGet]
        public async Task<IActionResult> ExportToExcelFiltered(string name = "", string species = "")
        {
            var fileName = $"Pokemon-List-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            
            byte[] excelBytes = await _excelService.GenerateExcelReportAsync(name, species);
            return new FileContentResult(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        [HttpGet]
        public async Task<IActionResult> CountFiltered(string name = "", string species = "")
        {
            var all = await _pokemonService.GetAllPokemonAsync();

            if (!string.IsNullOrWhiteSpace(name))
            {
                all = all
                    .Where(p => p.name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(species))
            {
                // Obtener todos los nombres de la cadena evolutiva de la especie seleccionada
                var evolutionNames = await _pokemonService.GetEvolutionChainPokemonNamesAsync(species);

                if (evolutionNames.Any())
                {
                    all = all
                        .Where(p => evolutionNames.Contains(p.name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    all = new List<Pokemon>(); // si no hay cadena, vacía
                }
            }

            return Json(new { count = all.Count });
        }

        [HttpPost]
        public async Task<IActionResult> SendReportByEmail([FromBody] EmailReportRequest request)
        {
            try
            {

                // Se genera el reporte Excel
                var bytes = await _excelService.GenerateExcelReportAsync(request.Name, request.Species);

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
           

            var result = await _pokemonService.getDetails(name);

            return Json(result);
        }
    }
}
