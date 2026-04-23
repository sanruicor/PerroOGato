using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
 
public class DogOrCatManager : MonoBehaviour
{
    // ── URLs de las APIs ─────────────────────────────────────────────────────
    private const string DOG_URL     = "https://dog.ceo/api/breeds/image/random";
    private const string CAT_URL     = "https://api.thecatapi.com/v1/images/search?has_breeds=1&mime_types=jpg";
    private const string CAT_API_KEY = "DEMO-API-KEY";
 
    // ── Configuración ────────────────────────────────────────────────────────
    [Header("Game Settings")]
    public int   totalRounds = 10;
    public float pixelSize   = 32f;   // Tamaño del bloque pixelado
 
    // ── Estado del juego ─────────────────────────────────────────────────────
    private int         currentRound    = 0;
    private int         score           = 0;
    private AnimalEntry currentEntry;
    private bool        waitingForAnswer = false;
 
    // ── Paneles UI ───────────────────────────────────────────────────────────
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject loadingPanel;
    public GameObject gamePanel;
    public GameObject resultPanel;
 
    // ── Start Panel ──────────────────────────────────────────────────────────
    [Header("Start Panel")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Button          startButton;
 
    // ── Loading Panel ────────────────────────────────────────────────────────
    [Header("Loading Panel")]
    public TextMeshProUGUI loadingText;
 
    // ── Game Panel ───────────────────────────────────────────────────────────
    [Header("Game Panel")]
    public TextMeshProUGUI roundNumberText;
    public RawImage        animalImage;            // La foto del animal
    public Material        pixelateMaterial;       // Arrastra aquí PixelateMaterial.mat
    public GameObject      imageLoadingIndicator;
    public Button          hintButton;             // "🔍 Ver imagen"
    public TextMeshProUGUI breedNameText;
    public Button          dogButton;
    public Button          catButton;
    public TextMeshProUGUI feedbackText;
    public Button          nextButton;
 
    // ── Result Panel ─────────────────────────────────────────────────────────
    [Header("Result Panel")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;
    public Button          restartButton;
 
    // Material original del RawImage (guardado para restaurarlo al quitar el efecto)
    private Material _originalMaterial;
 
    
    void Start()
    {
        _originalMaterial = animalImage.material;
 
        if (titleText    != null) titleText.text    = "¿Perro o Gato?";
        if (subtitleText != null) subtitleText.text = "¿Sabrás distinguir la raza?";
 
        startButton.onClick.AddListener(OnStartClicked);
        hintButton.onClick.AddListener(OnHintClicked);
        dogButton.onClick.AddListener(() => OnAnswerSelected("PERRO"));
        catButton.onClick.AddListener(() => OnAnswerSelected("GATO"));
        nextButton.onClick.AddListener(OnNextClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
 
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
    void LoadRound()
    {
        currentRound++;
        waitingForAnswer = false;
        ShowPanel(loadingPanel);
        StartCoroutine(FetchAnimalImage());
    }
 
    // ── CORRUTINA: decide el animal ──────────────────────────────────────────
    IEnumerator FetchAnimalImage()
    {
        if (loadingText != null) loadingText.text = "Cargando ronda...";
 
        bool isDog = UnityEngine.Random.value > 0.5f;
        if (isDog)
            yield return StartCoroutine(FetchDog());
        else
            yield return StartCoroutine(FetchCat());
    }
 
    // ── CORRUTINA: Dog CEO API ───────────────────────────────────────────────
    IEnumerator FetchDog()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(DOG_URL))
        {
            yield return req.SendWebRequest();
 
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Dog API: " + req.error);
                yield return StartCoroutine(FetchCat());
                yield break;
            }
 
            DogApiResponse res  = JsonUtility.FromJson<DogApiResponse>(req.downloadHandler.text);
            string breedName    = ExtractBreedFromDogUrl(res.message);
            Debug.Log($"[DogAPI] Ronda {currentRound + 1} | Raza: {breedName}");
 
            currentEntry = new AnimalEntry(AnimalType.Dog, res.message, breedName);
            yield return StartCoroutine(DownloadAndShowTexture(res.message));
        }
    }
 
    // ── CORRUTINA: TheCatAPI ─────────────────────────────────────────────────
    IEnumerator FetchCat()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(CAT_URL))
        {
            req.SetRequestHeader("x-api-key", CAT_API_KEY);
            yield return req.SendWebRequest();
 
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Cat API: " + req.error);
                yield break;
            }
 
            string wrapped = "{\"items\":" + req.downloadHandler.text + "}";
            CatApiResponseWrapper res = JsonUtility.FromJson<CatApiResponseWrapper>(wrapped);
 
            if (res.items == null || res.items.Count == 0) { Debug.LogError("Sin imágenes de gato."); yield break; }
 
            CatApiEntry  cat  = res.items[0];
            string breed = "", origin = "", temperament = "";
 
            if (cat.breeds != null && cat.breeds.Count > 0)
            {
                breed       = cat.breeds[0].name;
                origin      = cat.breeds[0].origin;
                temperament = cat.breeds[0].temperament;
            }
 
            Debug.Log($"[CatAPI] Ronda {currentRound + 1} | Raza: {breed}");
 
            currentEntry = new AnimalEntry(AnimalType.Cat, cat.url, breed, origin, temperament);
            yield return StartCoroutine(DownloadAndShowTexture(cat.url));
        }
    }
 
    // ── CORRUTINA: descarga la textura ───────────────────────────────────────
    IEnumerator DownloadAndShowTexture(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
 
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error descargando imagen: " + req.error);
                ShowGamePanel();
                yield break;
            }
 
            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            animalImage.texture = tex;
            AdjustImageAspect(tex);
            ShowGamePanel();
        }
    }
 
    // ── Muestra el GamePanel con la imagen pixelada ──────────────────────────
    void ShowGamePanel()
    {
        ShowPanel(gamePanel);
 
        roundNumberText.text = $"Ronda {currentRound} / {totalRounds}";
 
        animalImage.gameObject.SetActive(true);
        imageLoadingIndicator.SetActive(false);
 
        // Aplicar el material pixelado
        ApplyPixelate(true);
 
        hintButton.gameObject.SetActive(true);
        hintButton.interactable = true;
 
        dogButton.interactable = true;
        catButton.interactable = true;
 
        feedbackText.text = "";
        nextButton.gameObject.SetActive(false);
 
        ShowBreedInfo();
        waitingForAnswer = true;
    }
 
    // ── Aplica o quita el efecto pixelado en el RawImage ────────────────────
    void ApplyPixelate(bool on)
    {
        if (pixelateMaterial == null) return;
 
        if (on)
        {
            // Crear una instancia del material para no modificar el asset original
            Material instance = new Material(pixelateMaterial);
            instance.SetFloat("_PixelSize", pixelSize);
            animalImage.material = instance;
        }
        else
        {
            // Destruir la instancia y restaurar el material por defecto
            if (animalImage.material != _originalMaterial)
                Destroy(animalImage.material);
            animalImage.material = _originalMaterial;
        }
    }
 
    // ── Botón Pista: quita el pixelado ───────────────────────────────────────
    void OnHintClicked()
    {
        ApplyPixelate(false);
        hintButton.gameObject.SetActive(false);
    }
 
    // ── Muestra nombre y detalles de raza ────────────────────────────────────
    void ShowBreedInfo()
    {
        breedNameText.text = !string.IsNullOrEmpty(currentEntry.breedName)
            ? currentEntry.breedName
            : "Raza desconocida";
    }
 
    void AdjustImageAspect(Texture2D tex)
    {
        if (tex == null) return;
        var fitter = animalImage.GetComponent<AspectRatioFitter>();
        if (fitter != null) fitter.aspectRatio = (float)tex.width / tex.height;
    }
 
    // ── Respuesta seleccionada ───────────────────────────────────────────────
    void OnAnswerSelected(string choice)
    {
        if (!waitingForAnswer) return;
        waitingForAnswer = false;
 
        // Revelar la imagen si el jugador respondió sin usar la pista
        ApplyPixelate(false);
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
 
    void OnNextClicked()
    {
        if (currentRound < totalRounds) LoadRound();
        else ShowResults();
    }
 
    void ShowResults()
    {
        scoreText.text = $"Puntuación final\n{score} / {totalRounds}";
        rankText.text  = GetRank(score, totalRounds);
        ShowPanel(resultPanel);
    }
 
    void OnRestartClicked() => ShowPanel(startPanel);
 
    void ShowPanel(GameObject panel)
    {
        startPanel.SetActive(panel   == startPanel);
        loadingPanel.SetActive(panel == loadingPanel);
        gamePanel.SetActive(panel    == gamePanel);
        resultPanel.SetActive(panel  == resultPanel);
    }
 
    string ExtractBreedFromDogUrl(string url)
    {
        try
        {
            string[] parts = url.Split('/');
            string seg     = parts[parts.Length - 2];
            string[] words = seg.Split('-');
            if (words.Length == 2)
                return $"{Capitalize(words[1])} {Capitalize(words[0])}";
            string r = "";
            foreach (string w in words) r += Capitalize(w) + " ";
            return r.Trim();
        }
        catch { return "Raza desconocida"; }
    }
 
    string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
 
    string GetRank(int s, int total)
    {
        float p = (float)s / total;
        if (p == 1f)   return "🏆 ¡Experto en razas! Ojo de lince.";
        if (p >= 0.8f) return "😎 ¡Casi perfecto! Muy buen olfato.";
        if (p >= 0.5f) return "🤔 No está mal... pero alguna raza te engañó.";
        return "🐾 ¡Las razas te tienen confundido!";
    }
}