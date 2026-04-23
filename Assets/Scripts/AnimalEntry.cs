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
 
    // La respuesta correcta en texto, para comparar con el botón pulsado
    public string CorrectAnswer => animalType == AnimalType.Dog ? "PERRO" : "GATO";
 
    public AnimalEntry(AnimalType type, string url)
    {
        animalType = type;
        imageUrl   = url;
    }
}