# PickMe

Une plateforme de livraison pensée pour Abidjan, construite comme une simulation complète.

Un client commande une livraison, un livreur l'accepte, et le client suit la course en temps réel sur la carte. Un administrateur valide les livreurs et suit l'activité.

## Trois rôles

**Client**
- Passe une commande en indiquant les adresses de départ et d'arrivée
- Voit le prix calculé avant de valider
- Suit sa livraison en direct
- Note la livraison une fois terminée
- Retrouve l'historique de ses commandes

**Livreur**
- S'inscrit, puis attend la validation d'un administrateur
- Accepte ou refuse les courses proposées
- Met à jour l'état de la livraison
- Consulte son historique

**Administrateur**
- Approuve, suspend ou bloque les livreurs
- Suit toutes les commandes
- Consulte les rapports d'activité

## Le calcul du prix

Le prix tient compte de la **distance réelle par la route**, calculée avec OSRM, et de **la météo** au point de départ, récupérée sur OpenWeatherMap : sous la pluie ou l'orage, la course coûte 15 % de plus.

## Technologies

| Partie | Outils |
|---|---|
| Application | ASP.NET Core 9 (MVC), C# |
| Base de données | SQL Server, Entity Framework Core |
| Comptes et rôles | ASP.NET Core Identity |
| Temps réel | SignalR |
| Services externes | OSRM (itinéraires), OpenWeatherMap (météo) |
| Journalisation | Serilog |

## Lancer le projet

Il faut le SDK .NET 9 et SQL Server (la version Express suffit).

```bash
# 1. Adapter la chaîne de connexion dans appsettings.json
#    (ConnectionStrings > DefaultConnection)

# 2. Créer la base de données
dotnet ef database update

# 3. Lancer l'application
dotnet run
```

La météo est facultative. Pour l'activer, ajouter une clé OpenWeatherMap dans la configuration, sous `OpenWeatherMap:ApiKey`. Sans clé, le prix est calculé sans majoration météo.

## Auteure

Aya Aidibi, [aya-aidibi-portfolio.netlify.app](https://aya-aidibi-portfolio.netlify.app)
