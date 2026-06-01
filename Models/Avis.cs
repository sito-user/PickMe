namespace DeliveryWeb.Models
{
    public class Avis
    {
        public int Id { get; set; }
        public int Note { get; set; } // 1 à 5
        public string? Commentaire { get; set; }
        public DateTime DateAvis { get; set; } = DateTime.Now;

        // Clés étrangères
        public int CommandeId { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string LivreurId { get; set; } = string.Empty;

        // Navigation
        public Commande? Commande { get; set; }
        public Client? Client { get; set; }
        public Livreur? Livreur { get; set; }
    }
}