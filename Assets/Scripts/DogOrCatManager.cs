using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
 
public class DogOrCatManager : MonoBehaviour
{
    // ── URLs de las APIs ─────────────────────────────────────────────────────
    // has_breeds=1  → incluye info de raza en la respuesta
    // mime_types=jpg → evita GIFs que no carga UnityWebRequestTexture
    private const string DOG_URL = "https://dog.ceo/api/breeds/image/random";
    private const string CAT_URL = "https://api.thecatapi.com/v1/images/search?has_breeds=1&mime_types=jpg";
    private const string CAT_API_KEY = "DEMO-API-KEY";
 
    // ── Configuración ────────────────────────────────────────────────────────
    [Header("Game Settings")]
    public int totalRounds = 10;
 
    // ── Estado del juego ─────────────────────────────────────────────────────
    private int currentRound = 0;
    private int score = 0;
    private AnimalEntry currentEntry;
    private bool waitingForAnswer = false;
 
    // ── Paneles UI ───────────────────────────────────────────────────────────
    [Header("Panels")]
    public GameObject loadingPanel;
    public GameObject gamePanel;
    public GameObject resultPanel;
 
    // ── Loading Panel ────────────────────────────────────────────────────────
    [Header("Loading Panel")]
    public TextMeshProUGUI loadingText;
 
    // ── Game Panel ───────────────────────────────────────────────────────────
    [Header("Game Panel")]
    public TextMeshProUGUI roundNumberText;       // "Ronda 3 / 10"
    public RawImage animalImage;                  // Foto del animal
    public GameObject imageLoadingIndicator;      // "Cargando imagen..."
    public TextMeshProUGUI breedNameText;         // "Golden Retriever"
    public Button dogButton;                      // "🐶 PERRO"
    public Button catButton;                      // "🐱 GATO"
    public TextMeshProUGUI feedbackText;
    public Button nextButton;
 
    // ── Result Panel ─────────────────────────────────────────────────────────
    [Header("Result Panel")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;
    public Button restartButton;
 
    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        dogButton.onClick.AddListener(() => OnAnswerSelected("PERRO"));
        catButton.onClick.AddListener(() => OnAnswerSelected("GATO"));
        nextButton.onClick.AddListener(OnNextClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
 
        StartGame();
    }
 
    void StartGame()
    {
        currentRound = 0;
        score = 0;
        ShowPanel(gamePanel);
        LoadRound();
    }
 
    // ── Cargar ronda: una llamada a API por ronda ────────────────────────────
    void LoadRound()
    {
        currentRound++;
        waitingForAnswer = false;
 
        feedbackText.text    = "";
        breedNameText.text   = "";
        nextButton.gameObject.SetActive(false);
        dogButton.interactable = false;
        catButton.interactable = false;
 
        animalImage.gameObject.SetActive(false);
        imageLoadingIndicator.SetActive(true);
 
        roundNumberText.text = $"Ronda {currentRound} / {totalRounds}";
 
        StartCoroutine(FetchAnimalImage());
    }
 
    // ── CORRUTINA: decide el animal y llama a la API correcta ────────────────
    IEnumerator FetchAnimalImage()
    {
        bool isDog = UnityEngine.Random.value > 0.5f;
 
        if (isDog)
            yield return StartCoroutine(FetchDog());
        else
            yield return StartCoroutine(FetchCat());
    }
 
    // ── CORRUTINA: Dog CEO API ───────────────────────────────────────────────
    IEnumerator FetchDog()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(DOG_URL))
        {
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Dog API: " + request.error);
                yield return StartCoroutine(FetchCat());
                yield break;
            }
 
            DogApiResponse response = JsonUtility.FromJson<DogApiResponse>(request.downloadHandler.text);
            string breedName = ExtractBreedFromDogUrl(response.message);
 
            Debug.Log($"[DogAPI] Ronda {currentRound} | Raza: {breedName} | URL: {response.message}");
 
            currentEntry = new AnimalEntry(AnimalType.Dog, response.message, breedName);
            yield return StartCoroutine(DownloadAndShowTexture(response.message));
        }
    }
 
    // ── CORRUTINA: TheCatAPI con info de raza ────────────────────────────────
    IEnumerator FetchCat()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(CAT_URL))
        {
            request.SetRequestHeader("x-api-key", CAT_API_KEY);
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Cat API: " + request.error);
                yield break;
            }
 
            string json    = request.downloadHandler.text;
            string wrapped = "{\"items\":" + json + "}";
            CatApiResponseWrapper response = JsonUtility.FromJson<CatApiResponseWrapper>(wrapped);
 
            if (response.items == null || response.items.Count == 0)
            {
                Debug.LogError("TheCatAPI no devolvió imágenes.");
                yield break;
            }
 
            CatApiEntry cat     = response.items[0];
            string breedName    = "";
            string origin       = "";
            string temperament  = "";
 
            // has_breeds=1 garantiza que casi siempre habrá info de raza
            if (cat.breeds != null && cat.breeds.Count > 0)
            {
                CatBreedInfo breed = cat.breeds[0];
                breedName   = breed.name;
                origin      = breed.origin;
                temperament = breed.temperament;
            }
 
            Debug.Log($"[CatAPI] Ronda {currentRound} | Raza: {breedName} | Origen: {origin}");
 
            currentEntry = new AnimalEntry(AnimalType.Cat, cat.url, breedName, origin, temperament);
            yield return StartCoroutine(DownloadAndShowTexture(cat.url));
        }
    }
 
    // ── CORRUTINA: descarga la imagen y la muestra ───────────────────────────
    IEnumerator DownloadAndShowTexture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
 
            imageLoadingIndicator.SetActive(false);
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error descargando imagen: " + request.error);
                EnableAnswerButtons();
                yield break;
            }
 
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            animalImage.texture = texture;
            animalImage.gameObject.SetActive(true);
 
            AdjustImageAspect(texture);
            ShowBreedInfo();
            EnableAnswerButtons();
        }
    }
 
    // ── Muestra la info de raza bajo la imagen ───────────────────────────────
    void ShowBreedInfo()
    {
        if (!string.IsNullOrEmpty(currentEntry.breedName))
        {
            breedNameText.text = currentEntry.breedName;
        }
        else
        {
            breedNameText.text    = "Raza desconocida";
        }
    }
 
    void AdjustImageAspect(Texture2D texture)
    {
        if (texture == null) return;
        AspectRatioFitter fitter = animalImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            fitter.aspectRatio = (float)texture.width / texture.height;
    }
 
    void EnableAnswerButtons()
    {
        dogButton.interactable = true;
        catButton.interactable = true;
        waitingForAnswer = true;
    }
 
    // ── Respuesta seleccionada ───────────────────────────────────────────────
    void OnAnswerSelected(string choice)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;
 
        dogButton.interactable = false;
        catButton.interactable = false;
 
        bool correct = choice == currentEntry.CorrectAnswer;
 
        if (correct)
        {
            score++;
            feedbackText.text  = $"✔ ¡Correcto! {currentEntry.breedName} es un {currentEntry.CorrectAnswer.ToLower()}.";
            feedbackText.color = Color.green;
        }
        else
        {
            feedbackText.text  = $"✘ ¡Incorrecto! {currentEntry.breedName} es un {currentEntry.CorrectAnswer.ToLower()}.";
            feedbackText.color = Color.red;
        }
 
        nextButton.gameObject.SetActive(true);
        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            currentRound < totalRounds ? "Siguiente →" : "Ver resultado";
    }
 
    void OnNextClicked()
    {
        if (currentRound < totalRounds)
            LoadRound();
        else
            ShowResults();
    }
 
    void ShowResults()
    {
        scoreText.text = $"Puntuación final\n{score} / {totalRounds}";
        rankText.text  = GetRank(score, totalRounds);
        ShowPanel(resultPanel);
    }
 
    void OnRestartClicked() => StartGame();
 
    // ── Extrae la raza del path de la URL de Dog CEO ─────────────────────────
    // Ejemplos:
    //   https://images.dog.ceo/breeds/golden-retriever/foto.jpg  → "Golden Retriever"
    //   https://images.dog.ceo/breeds/hound-afghan/foto.jpg      → "Afghan Hound"
    string ExtractBreedFromDogUrl(string url)
    {
        try
        {
            // El segmento de raza es el penúltimo: breeds/{raza}/archivo.jpg
            string[] parts = url.Split('/');
            string breedSegment = parts[parts.Length - 2]; // e.g. "golden-retriever"
 
            // Separar por guión: "golden-retriever" → ["golden", "retriever"]
            string[] words = breedSegment.Split('-');
 
            // Dog CEO usa "tipo-subtipo" (hound-afghan) pero la raza real es "Afghan Hound"
            // → invertir el orden si hay dos palabras para que suene natural
            if (words.Length == 2)
            {
                string main = Capitalize(words[0]);
                string sub  = Capitalize(words[1]);
                return $"{sub} {main}";   // "Afghan Hound" en vez de "Hound Afghan"
            }
 
            // Un solo término: simplemente capitalizar
            string result = "";
            foreach (string w in words)
                result += Capitalize(w) + " ";
            return result.Trim();
        }
        catch
        {
            return "Raza desconocida";
        }
    }
 
    string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
 
    // ── Helpers ──────────────────────────────────────────────────────────────
    void ShowPanel(GameObject panel)
    {
        loadingPanel.SetActive(panel == loadingPanel);
        gamePanel.SetActive(panel == gamePanel);
        resultPanel.SetActive(panel == resultPanel);
    }
 
    string GetRank(int s, int total)
    {
        float pct = (float)s / total;
        if (pct == 1f)   return "🏆 ¡Experto en razas! Ojo de lince.";
        if (pct >= 0.8f) return "😎 ¡Casi perfecto! Muy buen olfato.";
        if (pct >= 0.5f) return "🤔 No está mal... pero alguna raza te engañó.";
        return "🐾 ¡Las razas te tienen confundido!";
    }
}