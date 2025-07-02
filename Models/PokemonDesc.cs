using Newtonsoft.Json;

namespace PokemonWebApp.Models
{
    //Descripción de un Pokémon
    public class PokemonDesc
    {
        public int id { get; set; }
        
        [JsonProperty("sprites")]
        public Sprite sprites { get; set; }
        [JsonProperty("types")]
        public List<TypeWrapper> types { get; set; }

        [JsonProperty("abilities")]
        public List<AbilityWrapper> abilities { get; set; }

    }

    // Tipos
    public class TypeWrapper
    {
        [JsonProperty("type")]
        public NamedAPIResourceDescription type { get; set; }
    }

    // Habilidades
    public class AbilityWrapper
    {
        [JsonProperty("ability")]
        public NamedAPIResourceDescription ability { get; set; }

        [JsonProperty("is_hidden")]
        public bool is_hidden { get; set; }
    }


    public class NamedAPIResourceDescription
    {
        public string name { get; set; }
        public string url { get; set; }
    }
}
