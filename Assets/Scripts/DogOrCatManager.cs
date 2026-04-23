using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
 
public class DogOrCatManager : MonoBehaviour
{
    // ── URLs de las APIs ─────────────────────────────────────────────────────
    private const string DOG_URL = "https://dog.ceo/api/breeds/image/random";
    private const string CAT_URL = "https://api.thecatapi.com/v1/images/search";
    private const string CAT_API_KEY = "DEMO-API-KEY";
 
    // ── Configuración de partida ─────────────────────────────────────────────
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
    public TextMeshProUGUI roundNumberText;   // "Ronda 3 / 10"
    public RawImage animalImage;              // Imagen del animal
    public GameObject imageLoadingIndicator; // Spinner o texto "Cargando imagen..."
    public Button dogButton;                  // "🐶 PERRO"
    public Button catButton;                  // "🐱 GATO"
    public TextMeshProUGUI feedbackText;      // "✔ ¡Correcto!" / "✘ Era un gato"
    public Button nextButton;                 // "Siguiente →"
 
    // ── Result Panel ─────────────────────────────────────────────────────────
    [Header("Result Panel")]
    public TextMeshProUGUI scoreText;         // "Puntuación: 7 / 10"
    public TextMeshProUGUI rankText;          // "¡Experto en animales!"
    public Button restartButton;
 
    
    void Start()
    {
        dogButton.onClick.AddListener(() => OnAnswerSelected("PERRO"));
        catButton.onClick.AddListener(() => OnAnswerSelected("GATO"));
        nextButton.onClick.AddListener(OnNextClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
 
        StartGame();
    }
 
    // ── Iniciar / reiniciar partida ──────────────────────────────────────────
    void StartGame()
    {
        currentRound = 0;
        score = 0;
        ShowPanel(gamePanel);
        LoadRound();
    }
 
    // ── Cargar ronda: UNA llamada a API por ronda ────────────────────────────
    void LoadRound()
    {
        currentRound++;
        waitingForAnswer = false;
 
        feedbackText.text = "";
        nextButton.gameObject.SetActive(false);
        dogButton.interactable = false;
        catButton.interactable = false;
 
        animalImage.gameObject.SetActive(false);
        imageLoadingIndicator.SetActive(true);
 
        roundNumberText.text = $"Ronda {currentRound} / {totalRounds}";
 
        // Decidir aleatoriamente perro o gato y hacer la llamada correspondiente
        StartCoroutine(FetchAnimalImage());
    }
 
    // ── CORRUTINA: decide el animal y llama a la API correcta ────────────────
    IEnumerator FetchAnimalImage()
    {
        // 50% de probabilidad de perro o gato
        bool isDog = Random.value > 0.5f;
 
        if (isDog)
            yield return StartCoroutine(FetchDog());
        else
            yield return StartCoroutine(FetchCat());
    }
 
    // ── CORRUTINA: llamada a Dog CEO API ─────────────────────────────────────
    IEnumerator FetchDog()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(DOG_URL))
        {
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Dog API: " + request.error);
                // Fallback: intentar con gato
                yield return StartCoroutine(FetchCat());
                yield break;
            }
 
            DogApiResponse response = JsonUtility.FromJson<DogApiResponse>(request.downloadHandler.text);
            Debug.Log($"[DogAPI] Ronda {currentRound} - URL: {response.message}");
 
            currentEntry = new AnimalEntry(AnimalType.Dog, response.message);
            yield return StartCoroutine(DownloadAndShowTexture(response.message));
        }
    }
 
    // ── CORRUTINA: llamada a TheCatAPI ───────────────────────────────────────
    IEnumerator FetchCat()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(CAT_URL))
        {
            // TheCatAPI acepta DEMO-API-KEY sin registro
            request.SetRequestHeader("x-api-key", CAT_API_KEY);
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Cat API: " + request.error);
                yield break;
            }
 
            // La respuesta es un array JSON → wrapeamos para JsonUtility
            string json = request.downloadHandler.text;
            string wrapped = "{\"items\":" + json + "}";
            CatApiResponseWrapper response = JsonUtility.FromJson<CatApiResponseWrapper>(wrapped);
 
            if (response.items == null || response.items.Count == 0)
            {
                Debug.LogError("TheCatAPI no devolvió imágenes.");
                yield break;
            }
 
            string imageUrl = response.items[0].url;
            Debug.Log($"[CatAPI] Ronda {currentRound} - URL: {imageUrl}");
 
            currentEntry = new AnimalEntry(AnimalType.Cat, imageUrl);
            yield return StartCoroutine(DownloadAndShowTexture(imageUrl));
        }
    }
 
    // ── CORRUTINA: descarga la imagen y la muestra en el RawImage ────────────
    IEnumerator DownloadAndShowTexture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
 
            imageLoadingIndicator.SetActive(false);
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error descargando imagen: " + request.error);
                // Mostrar imagen de error y habilitar botones igualmente
                EnableAnswerButtons();
                yield break;
            }
 
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            animalImage.texture = texture;
            animalImage.gameObject.SetActive(true);
 
            // Ajustar aspect ratio del RawImage para que no se deforme
            AdjustImageAspect(texture);
 
            EnableAnswerButtons();
        }
    }
 
    // ── Ajusta el RawImage para respetar la proporción de la imagen ──────────
    void AdjustImageAspect(Texture2D texture)
    {
        if (texture == null) return;
 
        AspectRatioFitter fitter = animalImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectRatio = (float)texture.width / texture.height;
        }
    }
 
    // ── Habilita los botones de respuesta ────────────────────────────────────
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
            feedbackText.text = $"✔ ¡Correcto! Era un {currentEntry.CorrectAnswer.ToLower()}.";
            feedbackText.color = Color.green;
        }
        else
        {
            feedbackText.text = $"✘ ¡Incorrecto! Era un {currentEntry.CorrectAnswer.ToLower()}.";
            feedbackText.color = Color.red;
        }
 
        nextButton.gameObject.SetActive(true);
        nextButton.GetComponentInChildren<TextMeshProUGUI>().text =
            currentRound < totalRounds ? "Siguiente →" : "Ver resultado";
    }
 
    // ── Botón Siguiente ──────────────────────────────────────────────────────
    void OnNextClicked()
    {
        if (currentRound < totalRounds)
            LoadRound();
        else
            ShowResults();
    }
 
    // ── Mostrar resultados ───────────────────────────────────────────────────
    void ShowResults()
    {
        scoreText.text = $"Puntuación final\n{score} / {totalRounds}";
        rankText.text  = GetRank(score, totalRounds);
        ShowPanel(resultPanel);
    }
 
    // ── Reiniciar ────────────────────────────────────────────────────────────
    void OnRestartClicked() => StartGame();
 
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
        if (pct == 1f)   return "🏆 ¡Experto en animales! Ojo de lince.";
        if (pct >= 0.8f) return "😎 ¡Casi perfecto! Los animales te gustan.";
        if (pct >= 0.5f) return "🤔 No está mal... pero alguno te engañó.";
        return "🐾 ¡Los animales te tienen confundido!";
    }
}