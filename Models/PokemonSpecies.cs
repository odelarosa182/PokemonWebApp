namespace PokemonWebApp.Models
{
    public class PokemonSpecies
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FlavorTextEntry> Flavor_text_entries { get; set; }
        public APIResource Evolution_chain { get; set; }
    }
    public class FlavorTextEntry
    {
        public string Flavor_text { get; set; }
        public NamedAPIResource Language { get; set; }
    }
    public class APIResource
    {
        public string Url { get; set; }
    }

    public class NamedAPIResource
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class NamedApiListResponse
    {
        public int Count { get; set; }
        public string Next { get; set; }
        public List<NamedAPIResource> Results { get; set; }
    }
}
