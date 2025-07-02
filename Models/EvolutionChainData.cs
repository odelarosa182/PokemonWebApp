namespace PokemonWebApp.Models
{
    public class EvolutionChainData
    {
        public ChainLink Chain { get; set; }
    }

    public class ChainLink
    {
        public NamedAPIResource Species { get; set; }
        public List<ChainLink> Evolves_to { get; set; }
    }
}
