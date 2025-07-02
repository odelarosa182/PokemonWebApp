using Newtonsoft.Json;

namespace PokemonWebApp.Models
{
    public class Sprite
    {
        [JsonProperty("front_default")]
        public string frontDefault { get; set; }
    }
}
