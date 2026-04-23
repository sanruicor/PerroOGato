# 🐾 ¿Perro o Gato?

Minijuego desarrollado en **Unity 6** para Android que consume las APIs públicas [Dog CEO API](https://dog.ceo/dog-api/) y [The Cat API](https://thecatapi.com/).

## 🕹️ ¿En qué consiste?

¿Perro o Gato? es un juego de identificación de razas en el que el jugador ve el nombre de una raza animal y debe adivinar si se trata de un perro o un gato. La imagen del animal aparece **pixelada** al inicio de cada ronda: el jugador puede responder solo con la raza como pista, o pulsar **"Ver imagen"** para revelarla y ayudarse.

Las imágenes y los datos de raza se obtienen en tiempo real desde las APIs en cada ronda.

## 📡 APIs utilizadas

| API | Datos obtenidos | Autenticación |
|-----|----------------|---------------|
| [Dog CEO API](https://dog.ceo/dog-api/) | Imagen aleatoria + raza (extraída de la URL) | Sin clave |
| [The Cat API](https://thecatapi.com/) | Imagen aleatoria + nombre, origen y temperamento de raza | `DEMO-API-KEY` |

## 📱 Flujo de juego

1. Se muestra la **pantalla de inicio** con el título y un botón para empezar
2. Cada ronda carga una imagen desde una de las dos APIs de forma aleatoria
3. El jugador ve el **nombre de la raza** y la imagen **pixelada**
4. Puede pulsar **"🔍 Ver imagen"** para revelarla (pista única por ronda)
5. El jugador pulsa **🐶 PERRO** o **🐱 GATO**
6. Se muestra feedback con la respuesta correcta y se revela la imagen
7. Al completar las 10 rondas se muestra la **puntuación final** y un rango

## 🏆 Rangos finales

| Puntuación | Rango |
|------------|-------|
| 10 / 10    | 🏆 ¡Experto en razas! Ojo de lince. |
| 8 – 9      | 😎 ¡Casi perfecto! Muy buen olfato. |
| 5 – 7      | 🤔 No está mal... pero alguna raza te engañó. |
| 0 – 4      | 🐾 ¡Las razas te tienen confundido! |

## 🗂️ Estructura de scripts

| Script | Responsabilidad |
|--------|----------------|
| `DogOrCatManager.cs` | Lógica principal del juego, llamadas a las APIs y gestión de la UI |
| `AnimalApiResponse.cs` | Modelos de datos JSON para Dog CEO API y The Cat API |
| `AnimalEntry.cs` | Modelo de una ronda jugable (tipo de animal, URL, datos de raza) |

## ✨ Características técnicas

- **Una llamada a API por ronda**, no hay precarga al inicio
- Efecto **pixelado** mediante shader HLSL personalizado aplicado al `RawImage`
- La raza de los perros se extrae del **path de la URL** sin llamada adicional
- Los gatos incluyen **origen y temperamento** gracias al parámetro `has_breeds=1`
- El tamaño del pixelado es ajustable desde el Inspector (`Pixel Size`, recomendado: 20–32)
