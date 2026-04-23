using System;
 
// ── Tipo de animal ───────────────────────────────────────────────────────────
public enum AnimalType
{
    Dog,
    Cat
}
 
// ── Dato de una ronda jugable ────────────────────────────────────────────────
[Serializable]
public class AnimalEntry
{
    public AnimalType animalType;
    public string imageUrl;
    public string breedName;    // "Golden Retriever", "Ragdoll", etc.
    public string origin;       // Solo gatos: país de origen
    public string temperament;  // Solo gatos: temperamento
 
    public string CorrectAnswer => animalType == AnimalType.Dog ? "PERRO" : "GATO";
 
    public AnimalEntry(AnimalType type, string url, string breed, string origin = "", string temperament = "")
    {
        animalType       = type;
        imageUrl         = url;
        breedName        = breed;
        this.origin      = origin;
        this.temperament = temperament;
    }
}