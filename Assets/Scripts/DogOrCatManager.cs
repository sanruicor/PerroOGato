using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
 
public class DogOrCatManager : MonoBehaviour
{
    // ── URLs de las APIs ─────────────────────────────────────────────────────
    private const string DOG_URL    = "https://dog.ceo/api/breeds/image/random";
    private const string CAT_URL    = "https://api.thecatapi.com/v1/images/search?has_breeds=1&mime_types=jpg";
    private const string CAT_API_KEY = "DEMO-API-KEY";
 
    // ── Configuración ────────────────────────────────────────────────────────
    [Header("Game Settings")]
    public int totalRounds = 10;
 
    // ── Estado del juego ─────────────────────────────────────────────────────
    private int currentRound    = 0;
    private int score           = 0;
    private AnimalEntry currentEntry;
    private bool waitingForAnswer = false;
 
    // ── Paneles UI ───────────────────────────────────────────────────────────
    [Header("Panels")]
    public GameObject startPanel;       // Pantalla de inicio con título y botón Start
    public GameObject loadingPanel;     // Pantalla de carga (spinner mientras llama a la API)
    public GameObject gamePanel;        // Pantalla de juego
    public GameObject resultPanel;      // Pantalla de resultados
 
    // ── Start Panel ──────────────────────────────────────────────────────────
    [Header("Start Panel")]
    public TextMeshProUGUI titleText;       // "🐾 ¿Perro o Gato?"
    public TextMeshProUGUI subtitleText;    // "¿Sabrás distinguir la raza?"
    public Button startButton;             // "¡Jugar!"
 
    // ── Loading Panel ────────────────────────────────────────────────────────
    [Header("Loading Panel")]
    public TextMeshProUGUI loadingText;    // "Cargando..."
 
    // ── Game Panel ───────────────────────────────────────────────────────────
    [Header("Game Panel")]
    public TextMeshProUGUI roundNumberText;     // "Ronda 3 / 10"
    public RawImage animalImage;               // Foto del animal
    public Image blurOverlay;                  // Overlay negro semitransparente sobre la imagen
    public GameObject imageLoadingIndicator;   // Visible mientras descarga la foto
    public Button hintButton;                  // "🔍 Ver imagen"
    public TextMeshProUGUI breedNameText;      // "Golden Retriever"
    public Button dogButton;                   // "🐶 PERRO"
    public Button catButton;                   // "🐱 GATO"
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
        // Textos del StartPanel (puedes editarlos también desde el Inspector)
        if (titleText    != null) titleText.text    = "🐾 ¿Perro o Gato?";
        if (subtitleText != null) subtitleText.text = "¿Sabrás distinguir la raza?";
 
        // Conectar botones
        startButton.onClick.AddListener(OnStartClicked);
        hintButton.onClick.AddListener(OnHintClicked);
        dogButton.onClick.AddListener(() => OnAnswerSelected("PERRO"));
        catButton.onClick.AddListener(() => OnAnswerSelected("GATO"));
        nextButton.onClick.AddListener(OnNextClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
 
        // Arrancar en el StartPanel
        ShowPanel(startPanel);
    }
 
    // ── Botón Start ──────────────────────────────────────────────────────────
    void OnStartClicked()
    {
        currentRound = 0;
        score        = 0;
        ShowPanel(loadingPanel);
        StartCoroutine(FetchAnimalImage());
    }
 
    // ── Cargar ronda ─────────────────────────────────────────────────────────
    // Llamado tras cada respuesta para preparar la siguiente ronda.
    // Muestra el loadingPanel mientras descarga la nueva imagen.
    void LoadRound()
    {
        currentRound++;
        waitingForAnswer = false;
 
        ShowPanel(loadingPanel);
        StartCoroutine(FetchAnimalImage());
    }
 
    // ── CORRUTINA: decide el animal y llama a la API correcta ────────────────
    IEnumerator FetchAnimalImage()
    {
        if (loadingText != null)
            loadingText.text = "Cargando ronda...";
 
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
                yield return StartCoroutine(FetchCat()); // fallback
                yield break;
            }
 
            DogApiResponse response = JsonUtility.FromJson<DogApiResponse>(request.downloadHandler.text);
            string breedName = ExtractBreedFromDogUrl(response.message);
 
            Debug.Log($"[DogAPI] Ronda {currentRound + 1} | Raza: {breedName} | URL: {response.message}");
 
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
 
            CatApiEntry cat    = response.items[0];
            string breedName   = "";
            string origin      = "";
            string temperament = "";
 
            if (cat.breeds != null && cat.breeds.Count > 0)
            {
                CatBreedInfo breed = cat.breeds[0];
                breedName   = breed.name;
                origin      = breed.origin;
                temperament = breed.temperament;
            }
 
            Debug.Log($"[CatAPI] Ronda {currentRound + 1} | Raza: {breedName} | Origen: {origin}");
 
            currentEntry = new AnimalEntry(AnimalType.Cat, cat.url, breedName, origin, temperament);
            yield return StartCoroutine(DownloadAndShowTexture(cat.url));
        }
    }
 
    // ── CORRUTINA: descarga la imagen ────────────────────────────────────────
    IEnumerator DownloadAndShowTexture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error descargando imagen: " + request.error);
                ShowGamePanel();
                yield break;
            }
 
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            animalImage.texture = texture;
            AdjustImageAspect(texture);
 
            ShowGamePanel();
        }
    }
 
    // ── Mostrar el GamePanel con el estado inicial de la ronda ───────────────
    // La imagen está lista pero cubierta por el overlay; se muestra solo la raza.
    void ShowGamePanel()
    {
        ShowPanel(gamePanel);
 
        roundNumberText.text = $"Ronda {currentRound} / {totalRounds}";
 
        // Imagen visible pero tapada por el overlay
        animalImage.gameObject.SetActive(true);
        imageLoadingIndicator.SetActive(false);
        blurOverlay.gameObject.SetActive(true);  // ← imagen oculta hasta pulsar Pista
 
        // Botón Pista activo
        hintButton.gameObject.SetActive(true);
        hintButton.interactable = true;
 
        // Botones de respuesta deshabilitados hasta que el jugador decida
        dogButton.interactable = true;
        catButton.interactable = true;
 
        feedbackText.text = "";
        nextButton.gameObject.SetActive(false);
 
        ShowBreedInfo();
 
        waitingForAnswer = true;
    }
 
    // ── Botón Pista: quita el overlay y revela la imagen ─────────────────────
    void OnHintClicked()
    {
        blurOverlay.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(false);
    }
 
    // ── Muestra el nombre y detalles de raza ─────────────────────────────────
    void ShowBreedInfo()
    {
        breedNameText.text = !string.IsNullOrEmpty(currentEntry.breedName) ? currentEntry.breedName : "Raza desconocida";
    }
 
    void AdjustImageAspect(Texture2D texture)
    {
        if (texture == null) return;
        AspectRatioFitter fitter = animalImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            fitter.aspectRatio = (float)texture.width / texture.height;
    }
 
    // ── Respuesta seleccionada ───────────────────────────────────────────────
    void OnAnswerSelected(string choice)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;
 
        // Revelar la imagen si el jugador respondió sin usarla
        blurOverlay.gameObject.SetActive(false);
        hintButton.gameObject.SetActive(false);
 
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
 
    // ── Reiniciar → volver al StartPanel ────────────────────────────────────
    void OnRestartClicked() => ShowPanel(startPanel);
 
    // ── Helpers ──────────────────────────────────────────────────────────────
    void ShowPanel(GameObject panel)
    {
        startPanel.SetActive(panel   == startPanel);
        loadingPanel.SetActive(panel == loadingPanel);
        gamePanel.SetActive(panel    == gamePanel);
        resultPanel.SetActive(panel  == resultPanel);
    }
 
    // Extrae la raza del path de la URL de Dog CEO
    // https://images.dog.ceo/breeds/golden-retriever/foto.jpg → "Golden Retriever"
    // https://images.dog.ceo/breeds/hound-afghan/foto.jpg     → "Afghan Hound"
    string ExtractBreedFromDogUrl(string url)
    {
        try
        {
            string[] parts        = url.Split('/');
            string breedSegment   = parts[parts.Length - 2];
            string[] words        = breedSegment.Split('-');
 
            if (words.Length == 2)
            {
                // Dog CEO usa "tipo-subtipo" → invertir para nombre natural
                return $"{Capitalize(words[1])} {Capitalize(words[0])}";
            }
 
            string result = "";
            foreach (string w in words)
                result += Capitalize(w) + " ";
            return result.Trim();
        }
        catch { return "Raza desconocida"; }
    }
 
    string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
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