using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PokemonWebApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PokemonWebApp.Services
{
    public class PokemonService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _memoryCache;
        private const string allPokemonCacheKey = "all_pokemon_cache";
        private const string allSpeciesCacheKey = "all_species_cache";


        public PokemonService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _memoryCache = cache;
        }

        public async Task<List<Pokemon>> GetAllPokemonAsync(ILogger _logger)
        {
            _logger.LogInformation("Entro a GetAllPokemonAsync - {Time}", DateTime.Now);
 
            // Verificar si ya existe en el cache
            if (_memoryCache.TryGetValue(allPokemonCacheKey, out List<Pokemon> cached))
            {
                return cached;
            }

            List<Pokemon> allPokemon = new List<Pokemon>();
            string url = _http.BaseAddress?.ToString() + "pokemon?limit=100&offset=0" ?? string.Empty;
            try
            {

                while (!string.IsNullOrEmpty(url)) // Mientras haya una URL para seguir obteniendo
                {
                    // se obtienen 100 pokemon por request
                    var response = await _http.GetAsync(url);
                    response.EnsureSuccessStatusCode(); // Lanza una excepción si el código de estado no es exitoso


                    var contenido = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PokemonResponse>(contenido);

                    if (result?.Results != null)
                    {
                        allPokemon.AddRange(result.Results);

                        
                    }
                    url = result?.Next;
                }

                // Cache por 1 hora
                _memoryCache.Set(allPokemonCacheKey, allPokemon, TimeSpan.FromHours(1));

                return allPokemon;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error en GetAllPokemonAsync - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                return new List<Pokemon>();
            }
        }

        

        public async Task<List<Pokemon>> setSpritePokemon(ILogger _logger,List<Pokemon> listaPokemon)
        {
            _logger.LogInformation("Entro a setSpritePokemon - {Time}", DateTime.Now);


            try
            {
                foreach (var pok in listaPokemon)
                {
                    string namePokemon = pok.name;
                    string cacheKey = $"pokemon_name_{namePokemon}";

                    // Verificar si ya existe la info del pokemon en el cache
                    if (_memoryCache.TryGetValue(cacheKey, out PokemonDesc cached))
                    {
                        pok.imageUrl = cached.sprites.frontDefault ?? string.Empty;
                        continue;
                    }

                    var response = await _http.GetAsync($"pokemon/{namePokemon}");
                    response.EnsureSuccessStatusCode(); // Lanza una excepción si el código de estado no es exitoso

                    var contenido = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PokemonDesc>(contenido);

                    pok.imageUrl = result?.sprites.frontDefault ?? string.Empty;

                    // Cache por 1 hora
                    _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(1));
                }
                return listaPokemon;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error en setSpritePokemon - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                return listaPokemon; // Retornar la lista original en caso de error
            }
        }

        public async Task<List<PokemonSpecies>> GetAllSpeciesAsync(ILogger _logger, int limit)
        {
            try
            {
                // Verificar si ya existe la info del pokemon en el cache
                if (_memoryCache.TryGetValue(allSpeciesCacheKey, out List<PokemonSpecies> cached))
                {
                    return cached;
                }

                var url = $"pokemon-species?limit={limit}";
                var resp = await _http.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var list = System.Text.Json.JsonSerializer.Deserialize<NamedApiListResponse>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var result = new List<PokemonSpecies>();

                foreach (var item in list.Results)
                {
                    var r = await _http.GetAsync(item.Url);
                    r.EnsureSuccessStatusCode();
                    var species = System.Text.Json.JsonSerializer.Deserialize<PokemonSpecies>(await r.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result.Add(species);
                }

                // Cache por 1 hora
                _memoryCache.Set(allSpeciesCacheKey, result, TimeSpan.FromHours(1));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error en GetAllSpeciesAsync - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                return new List<PokemonSpecies>();
            }
        }

        public async Task<dynamic> getDetails(ILogger _logger, string namePokemon)
        {
            _logger.LogInformation("Entro a setSpritePokemon - {Time}", DateTime.Now);

            try
            {          
                    string cacheKey = $"pokemon_name_{namePokemon}";

                    // Verificar si ya existe la info del pokemon en el cache
                    if (_memoryCache.TryGetValue(cacheKey, out PokemonDesc cached))
                    {
                    return  new
                    {
                        id = cached.id,
                        image = cached.sprites.frontDefault,
                        types = cached.types.Select(t => t.type.name).ToList(),
                        abilities = cached.abilities.Select(a => new {
                            name = a.ability.name,
                            isHidden = a.is_hidden
                        }).ToList()
                    };
                }

                    var response = await _http.GetAsync($"pokemon/{namePokemon}");
                    response.EnsureSuccessStatusCode(); // Lanza una excepción si el código de estado no es exitoso

                    var contenido = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PokemonDesc>(contenido);

                var data = new
                {
                    id = result.id,
                    image = result.sprites.frontDefault,
                    types = result.types.Select(t => t.type.name).ToList(),
                    abilities = result.abilities.Select(a => new {
                        name = a.ability.name,
                        isHidden = a.is_hidden
                    }).ToList()
                };

                // Cache por 1 hora
                _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(1));
                
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error en getDetails - {Time} - Error info: {Error}", DateTime.Now, ex.Message);
                return null;
            }
        }

        public async Task<List<string>> GetEvolutionChainPokemonNamesAsync(ILogger _logger, string speciesName)
        {
            var species = await GetAllSpeciesAsync(_logger, 200);

            var specie = species.FirstOrDefault(s => s.Name.Equals(speciesName, StringComparison.OrdinalIgnoreCase));

            if (specie?.Evolution_chain == null) return new List<string>();

            var response = await _http.GetAsync(specie.Evolution_chain.Url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var chainData = System.Text.Json.JsonSerializer.Deserialize<EvolutionChainData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var names = new List<string>();
            ExtractEvolutionNames(chainData.Chain, names);
            return names;
        }

        private void ExtractEvolutionNames(ChainLink node, List<string> names)
        {
            if (node?.Species != null)
                names.Add(node.Species.Name);

            if (node?.Evolves_to != null)
            {
                foreach (var child in node.Evolves_to)
                {
                    ExtractEvolutionNames(child, names);
                }
            }
        }
    }
}
