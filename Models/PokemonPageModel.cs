namespace PokemonWebApp.Models
{
    public class PokemonPageModel
    {
        public List<Pokemon> Pokemons { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
