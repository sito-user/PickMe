namespace DeliveryWeb.Models
{
    public class RefusLivraison
    {
        public int Id { get; set; }
        public int CommandeId { get; set; }
        public string LivreurId { get; set; } = string.Empty;
        public DateTime DateRefus { get; set; } = DateTime.Now;

        public Commande? Commande { get; set; }
    }
}