namespace PokemonWebApp.Models
{
    public class EmailSettings
    {
        public string From { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
