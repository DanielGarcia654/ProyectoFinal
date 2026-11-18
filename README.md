# AtomAR

App de Realidad Aumentada para aprender la tabla periódica en secundaria. El alumno apunta la cámara del teléfono a una tarjeta impresa y sobre ella aparece el átomo en 3D; después junta dos tarjetas para formar moléculas y pone a prueba lo aprendido con un reto de preguntas.

Proyecto final de la materia de Realidad Virtual y Aumentada.
Autor: Gerardo Daniel García Cerda (cuenta 319044546).

## Qué hace

| Modo | Qué hace el alumno | Qué aprende |
|---|---|---|
| **Explorar** | Apunta a una tarjeta y ve su átomo (modelo de Bohr) animado, con una carta de datos: número atómico, masa, protones, neutrones, electrones por capa y un "¿Sabías que…?". Con un dedo gira el átomo, con dos lo acerca y con doble toque lo regresa. | Estructura del átomo y dónde se encuentra cada elemento. |
| **Combinar** | Junta dos tarjetas y decide con los botones + y − cuántos átomos de cada elemento hacen falta. Con la proporción correcta, los átomos se unen y aparece la molécula. | Proporciones de los compuestos y su ecuación química real. |
| **Reto** | Contesta rondas de 10 preguntas (30 en total, 3 por elemento) y gana insignias de bronce, plata y oro. No usa la cámara. | Repaso de símbolos, protones y usos. |

Los tres modos están abiertos desde el inicio; la app solo sugiere el orden. Una guía animada sobre la cámara enseña qué hacer la primera vez en Explorar y Combinar, y se puede repetir con el botón **?**.

**Elementos (10):** H, He, C, N, O, Na, Cl, Ca, Fe, Au.
**Moléculas (4):** sal de mesa (NaCl), agua (H₂O), dióxido de carbono (CO₂) y óxido de hierro (Fe₂O₃).

## Requisitos

- Unity **6000.3.23f1** (Unity 6.3 LTS) con el módulo *Android Build Support* (OpenJDK, SDK y NDK).
- **Vuforia Engine 11.4.4** (incluido en `Packages/` como `.tgz`).
- **Git LFS**: el paquete de Vuforia y los modelos se guardan con LFS.
- Para el dispositivo: teléfono Android con cámara (ARM64). La app está pensada para usarse en vertical.

## Cómo abrir el proyecto

```bash
git lfs install
git clone https://github.com/DanielGarcia654/ProyectoFinal.git
```

1. Abre la carpeta con Unity Hub usando la versión 6000.3.23f1.
2. En `Assets/Resources/VuforiaConfiguration` escribe tu *License Key* de Vuforia si no está puesta.
3. Abre la escena `Assets/Scenes/AtomAR_Main.unity`.

## Cómo probarlo

**En el editor:** presiona Play con una cámara conectada (una webcam, o el celular como cámara con Camo o DroidCam; se elige en `VuforiaConfiguration > Play Mode`). Muestra a la cámara una tarjeta impresa o abierta en otra pantalla.

**En Android:** `File > Build Profiles > Android > Build And Run` con el teléfono conectado por USB y la depuración USB activada.

**Tarjetas:** las 10 imágenes están en `Assets/Editor/Vuforia/ImageTargetTextures/AtomAR/`. Imprímelas en papel mate.

## Menús del editor

En la barra de Unity aparece el menú **AtomAR**:

| Menú | Para qué sirve |
|---|---|
| Montar escena principal | Crea `AtomAR_Main` con la AR Camera y los 10 Image Targets con su átomo. |
| Montar interfaz | Vuelve a generar toda la interfaz (inicio, modos, guía, reto) y las fichas de datos. |
| Crear datos de elementos | Regenera solo las fichas de los 10 elementos. |
| Generar marca, ícono y pantalla de carga | Dibuja el logo y el ícono en `Assets/AtomAR_UI/Marca` y configura la pantalla de carga y el ícono de Android. |
| Reiniciar colección (pruebas) | Deja la colección de elementos en cero. |
| Reiniciar progreso completo (pruebas) | Borra colección, moléculas, insignias y guías vistas. |

> "Montar interfaz" reescribe las fichas con los textos que están en `AtomARUIBuilder.cs` y `AtomARAppBuilder.cs`. Si cambias un texto directo en una ficha, pásalo también al builder.

## Estructura

```
Assets/
  Scenes/AtomAR_Main.unity        Escena única de la app
  Scripts/AtomAR/                 Lógica de la app (ver tabla)
  Editor/AtomAR/                  Scripts que arman la escena y la interfaz
  AtomAR_Datos/Elementos/         Una ficha (ScriptableObject) por elemento
  AtomAR_Datos/Moleculas/         Una ficha por molécula
  AtomAR_Modelos/Elementos/       Átomos en FBX
  AtomAR_Modelos/Moleculas/       Moléculas en FBX
  AtomAR_UI/                      Tipografías, sprites y miniaturas de las tarjetas
  AtomAR_UI/Marca/                Logo, símbolo e ícono de la app
  StreamingAssets/Vuforia/        Base de datos de Image Targets "AtomAR"
Blender_Fuentes/
  AtomAR_Elementos.blend          Archivo maestro con los modelos y el script que los genera
  GLB/                            Respaldo de los modelos en GLB
```

### Scripts principales

| Script | Qué hace |
|---|---|
| `AppAtomAR` | Controla el modo actual (Inicio, Explorar, Combinar, Reto), la barra superior y las celebraciones. |
| `ElementoTarget` | Une cada Image Target con su ficha y avisa cuando la cámara lo ve o lo pierde. |
| `PanelElemento` | Carta del elemento en Explorar: decide qué elemento mostrar, lo anima y lleva la colección. |
| `ModoCombinar` | Detecta parejas de tarjetas, maneja los contadores, las pistas y la formación de la molécula. |
| `Quiz` | Preguntas, rondas, racha e insignias del Reto. |
| `GestosAtomo` | Girar, acercar y regresar el átomo con el dedo en Explorar. |
| `GuiaVisual` | Guía animada sobre la cámara (marco iluminado y ejemplo en movimiento). |
| `Celebracion` | Pantalla de logro al completar los 10 elementos o las 4 moléculas. |
| `AtomoAnimado` | Hace girar las capas de electrones y latir el núcleo de cada átomo 3D. |
| `DiagramaBohr`, `DibujoUI`, `AnilloUI` | Gráficos de la interfaz dibujados por código: mini átomo, ilustraciones y anillos de avance. |
| `Coleccion`, `ProgresoQuiz` | Guardan el progreso en el teléfono (`PlayerPrefs`). |
| `ElementoData`, `MoleculaData` | Definen los datos de cada elemento y molécula. |

## Marca

El logo muestra lo que hace la app: una tarjeta sobre la mesa y el átomo que aparece encima de ella. Hay versión para fondo oscuro (`AtomAR_Logo.png`), para fondo claro (`AtomAR_Logo_SobreClaro.png`), vertical, el símbolo solo y el ícono. La pantalla de carga muestra el logo de AtomAR con "Made with Unity" debajo.

## Modelos 3D

Los átomos no se modelaron a mano: los genera un script de Python dentro de Blender (`atomar_plantilla.py`, guardado en `AtomAR_Elementos.blend`) a partir del número de protones, neutrones y electrones por capa de cada elemento. El mismo script exporta cada átomo a FBX (para Unity) y a GLB (respaldo). El núcleo del oro omite las partículas totalmente cubiertas para que el modelo sea ligero en el celular.

## Tecnologías

Unity 6 · Vuforia Engine (Image Targets, hasta 4 simultáneos) · C# · TextMeshPro · Blender 5 con Python.

Tipografías: **Syne** e **Inter**, ambas con licencia SIL Open Font License.

## Privacidad

La app no pide cuenta ni datos personales, no se conecta a ningún servidor y no guarda imágenes de la cámara. El progreso se guarda solo en el teléfono.


### Guía animada en 3D
La guía de Explorar y Combinar muestra un ejemplo en 3D (`DioramaGuia.cs`): las tarjetas reales en perspectiva, con luz y sombra suave, y los mismos modelos de átomos y moléculas de la app. Unas yemas de dedo translúcidas enseñan a girar, acercar y tocar **+1**. Se dibuja en la capa 28, en una RenderTexture, y solo mientras la guía está visible. Para regenerarla sin tocar el resto de la interfaz: **AtomAR → Actualizar guía animada**.
