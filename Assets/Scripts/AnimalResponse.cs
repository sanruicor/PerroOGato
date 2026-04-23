using System;
using System.Collections.Generic;
 
// ── Dog CEO API ──────────────────────────────────────────────────────────────
// Endpoint: https://dog.ceo/api/breeds/image/random
// Respuesta: { "message": "https://...", "status": "success" }
[Serializable]
public class DogApiResponse
{
    public string message;   // URL de la imagen
    public string status;
}
 
// ── TheCatAPI ────────────────────────────────────────────────────────────────
// Endpoint: https://api.thecatapi.com/v1/images/search
// Respuesta: [ { "id": "...", "url": "https://...", "width": 0, "height": 0 } ]
// Nota: devuelve un array, por eso usamos un wrapper
[Serializable]
public class CatApiEntry
{
    public string id;
    public string url;
    public int width;
    public int height;
}
 
// Wrapper para deserializar el array con JsonUtility
// Uso: JsonUtility.FromJson<CatApiResponseWrapper>("{\"items\":" + json + "}")
[Serializable]
public class CatApiResponseWrapper
{
    public List<CatApiEntry> items;
}
 