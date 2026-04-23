using System;
using System.Collections.Generic;
 
// ── Dog CEO API ──────────────────────────────────────────────────────────────
// Endpoint: https://dog.ceo/api/breeds/image/random
// Respuesta: { "message": "https://images.dog.ceo/breeds/golden-retriever/x.jpg", "status": "success" }
// La raza se extrae del path de la URL, sin llamada extra.
[Serializable]
public class DogApiResponse
{
    public string message;   // URL de la imagen (contiene la raza en el path)
    public string status;
}
 
// ── TheCatAPI ────────────────────────────────────────────────────────────────
// Endpoint: https://api.thecatapi.com/v1/images/search?has_breeds=1&mime_types=jpg
// Con has_breeds=1 la respuesta incluye el array breeds[] con info de raza
[Serializable]
public class CatBreedInfo
{
    public string name;         // "Ragdoll"
    public string origin;       // "United States"
    public string temperament;  // "Calm, Gentle, Sociable"
}
 
[Serializable]
public class CatApiEntry
{
    public string id;
    public string url;
    public int width;
    public int height;
    public List<CatBreedInfo> breeds;  // vacío si el gato no tiene raza identificada
}
 
// Wrapper para deserializar el array raíz con JsonUtility
// Uso: JsonUtility.FromJson<CatApiResponseWrapper>("{\"items\":" + json + "}")
[Serializable]
public class CatApiResponseWrapper
{
    public List<CatApiEntry> items;
}
 