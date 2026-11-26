<div align="center">

# ⚔️ COLOSSUS: BOSS RUSH

### *Una sola vida. Cada jefe te enseña. Cada victoria te forja.*

![El Demonio lanza su lluvia de fuego sobre el héroe](Docs/boss1.png)

**Acción 2D · Boss Rush · Un jugador · PC**
Hecho en Unity 6 · Distribución planificada en Steam · Precio planificado: **US$ 3.99**, sin microtransacciones

</div>

---

## La campaña en 30 segundos

**Colossus** es un boss rush 2D de reflejos y lectura de patrones. Aquí no hay niveles de relleno ni hordas de enemigos menores: solo tú, tu espada y una arena cerrada frente a un coloso que quiere verte caer.

Cada jefe tiene ataques que **avisan antes de golpear**. Aprende su ritmo, esquiva en el momento justo y castígalo. Al derrotarlo, el juego te muestra **cómo peleaste** (tiempo, golpes recibidos y precisión) y te premia con **puntos para mejorar a tu personaje**. Cuanto mejor peleas, más fuerte llegas al siguiente combate.

> Tienes **una sola vida**. Si caes, empiezas de nuevo con una build distinta.

---

## Lo que hace único a Colossus

- 🔍 **Jefes que se leen, no que se adivinan.** El demonio marca el suelo en rojo antes de lanzarse o de soltar su lluvia de fuego; el Centinela destella antes de disparar. Todo ataque se puede anticipar.
- 🏆 **Tu rendimiento es tu poder.** Cada combate termina con una nota **S, A, B o C**. Una S te da el triple de puntos que una C.
- 🧬 **Tu build, tu partida.** Gasta los puntos en cuatro atributos y en **seis habilidades** (parry, combo de tres golpes, onda de espada, dash doble, vampirismo y segundo aliento). La tienda ofrece **tres habilidades al azar** en cada visita: no hay dos partidas iguales.
- 🛡️ **Defensa con riesgo y recompensa.** Cúbrete y recibe solo una parte del daño, o arriésgate a un **parry perfecto** (ventana de 0.10 s): anula el golpe, aturde al jefe y le devuelve sus bolas de fuego.
- 💥 **Cada golpe se siente.** Congelamiento al impactar, sacudida de cámara, números de daño y retroceso del jefe.
- 🔥 **Arenas con carácter.** Columnas de fuego en la primera arena y géiseres que escupen llamas hacia el cielo en la segunda (el jefe es inmune; tú no).

---

## Así se ve

### Los jefes

<table>
<tr>
<td width="50%"><img src="Docs/boss1.png" alt="El Demonio: lluvia de fuego"><br><sub><b>El Demonio</b> marca el suelo y deja caer una lluvia de fuego.</sub></td>
<td width="50%"><img src="Docs/boss1_picada.png" alt="El Demonio: picada"><br><sub>…o se eleva y se lanza en picada. Es tu momento para castigarlo.</sub></td>
</tr>
<tr>
<td colspan="2"><img src="Docs/boss2.png" alt="El Centinela entre géiseres de fuego"><br><sub><b>El Centinela</b> pelea a distancia mientras la arena escupe columnas de fuego hacia arriba.</sub></td>
</tr>
</table>

### Tus herramientas

<table>
<tr>
<td width="50%"><img src="Docs/escudo.png" alt="Cubrirse con el escudo"><br><sub><b>Cúbrete</b> y recibe solo el 30 % del daño. Con la habilidad Parry, un toque en el momento justo lo anula por completo.</sub></td>
<td width="50%"><img src="Docs/onda.png" alt="Onda de espada"><br><sub>Con la <b>onda de espada</b>, el golpe cargado lanza un corte que cruza la arena.</sub></td>
</tr>
</table>

### El ciclo: pelea, aprende, mejora

<table>
<tr>
<td width="50%"><img src="Docs/resumen.png" alt="Resumen del combate con la nota"><br><sub>El <b>resumen</b> premia el tiempo, la limpieza y la precisión con una nota.</sub></td>
<td width="50%"><img src="Docs/tienda.png" alt="Tienda de mejoras"><br><sub>La <b>tienda</b> convierte esa nota en atributos y habilidades.</sub></td>
</tr>
</table>

---

## Los jefes

### 1. El Demonio — 250 HP · *jugable*
Un coloso alado que domina el aire. Elige sus ataques al azar según la distancia, sin repetir el mismo dos veces seguidas:
- **Aliento de fuego** si te acercas demasiado.
- **Picada:** se eleva, marca el suelo y se lanza en diagonal. Después queda un instante vulnerable en el suelo.
- **Lluvia de fuego:** cinco columnas avisan y luego caen las llamas.
- **Fase 2 al 50 % de vida:** ruge, te empuja con una onda expansiva y se enfurece: más rápido, más ataques seguidos y más columnas de fuego.

### 2. El Centinela — 220 HP · *jugable*
Un guardián del fuego que no quiere que te acerques. Se mantiene a distancia y dispara bolas de fuego apuntadas y, cada tres ataques, un abanico de tres. Si lo presionas demasiado se transforma en un **pilar de fuego** y reaparece lejos. Mientras tanto, el suelo de su arena se abre en géiseres de fuego.

### 3. El Guardián — 450 HP · *en desarrollo*
El jefe final: una arena que se va reduciendo con bordes peligrosos que te arrinconan, y un ataque que toma un porcentaje de tu vida. Llegarás a él con todo lo que hayas construido… si llegas.

---

## Arma tu build

Al vencer a un jefe, tu **nota** define cuántos puntos ganas:

| Nota | **S** | **A** | **B** | **C** |
|---|---|---|---|---|
| Puntos de mejora | 6 | 4 | 3 | 2 |

Empiezas la partida con 2 puntos (solo alcanzan para atributos). Después de cada jefe, la tienda te deja gastar los puntos:

**Atributos** (5 niveles cada uno): **Vida** +15 HP · **Daño** +10 % · **Dash** −10 % de espera · **Carga** golpe cargado más rápido.

**Habilidades** (se compran una vez; la tienda ofrece 3 al azar):

| Habilidad | Costo | Qué hace |
|---|---|---|
| 🛡️ **Parry** | 4 | Clic derecho justo antes de un golpe: lo anulas, aturdes al jefe y devuelves sus bolas de fuego |
| ⚔️ **Combo de 3 golpes** | 3 | El tercer golpe seguido hace ×1.8 de daño y empuja al jefe |
| 🌙 **Onda de espada** | 3 | El golpe cargado lanza un corte a distancia |
| 💨 **Dash doble** | 3 | Dos esquivas seguidas |
| 🩸 **Vampirismo** | 3 | Cada golpe que acierta te cura 2 HP |
| 🔥 **Segundo aliento** | 4 | Una vez por partida, al caer revives con el 30 % de tu vida |

---

## Cómo se juega

| Acción | Control |
|---|---|
| Moverse | A / D |
| Saltar | Espacio (suéltalo antes para un salto corto) |
| Golpe normal | Clic izquierdo (toque) |
| Golpe cargado | Mantener clic izquierdo 0.6 s y soltar |
| Esquiva (dash) | Shift izquierdo |
| Cubrirse | Mantener clic derecho |
| Parry (habilidad) | Clic derecho justo antes de un golpe |
| Reiniciar partida | R |
| Volver al menú | Esc |

![Menú principal de Colossus](Docs/menu.png)

---

## Estado del proyecto

**Ya funciona**
- ✅ Dos jefes completos, cada uno con su arena, sus ataques y su comportamiento.
- ✅ Resumen de rendimiento, notas S/A/B/C, tienda de puntos y seis habilidades.
- ✅ Defensa con cubrirse y parry, salto con peso, dash con cargas.
- ✅ Menú principal, puerta entre arenas y reinicio de partida.

**En camino**
- 🔧 **El Guardián**, el jefe final, y su arena que se reduce.
- 🔧 **Más jefes**: la meta es ir bastante más allá de los tres iniciales.
- 🔧 Estamina y ventanas de invulnerabilidad más finas en el dash, tal como pide el diseño.
- 🔧 Puntuación global de la partida y récords guardados en el disco.
- 🔧 Soporte para mando y publicación en Steam.

---

## Campaña conceptual: metas y recompensas

> Colossus es un **proyecto universitario**. Esta sección muestra cómo sería su campaña de financiamiento; los montos son ilustrativos y no hay una recaudación real en curso.

**Recompensas**

| Aporte | Recompensa |
|---|---|
| **US$ 1** · Espectador | Tu nombre en los agradecimientos del juego |
| **US$ 4** · Cazador | Copia digital del juego al lanzarse (precio planificado: US$ 3.99) |
| **US$ 10** · Cazador de jefes | Copia digital + tu nombre grabado en una lápida del cementerio del juego |
| **US$ 25** · Domador de colosos | Copia digital + propones el nombre y el epitafio de un jefe futuro |

**Metas adicionales (stretch goals)**
1. **El Guardián** y su arena que se reduce.
2. Un **modo contrarreloj** para competir por la mejor nota.
3. Jefes nuevos con arte propio, para llegar a seis.
4. Banda sonora original.
5. Soporte para mando y traducción al inglés.

**¿En qué se usaría el dinero?** Arte y música originales para los jefes nuevos, la publicación en Steam y pruebas con jugadores para ajustar la dificultad.

---

## Riesgos y desafíos

- **Equipo de una persona.** El alcance se controla con una regla: pocos jefes, pero cuidados al detalle.
- **Dificultad.** Es un juego exigente a propósito; ajustar el balance exige muchas pruebas.
- **Arte y audio.** El prototipo usa paquetes de arte y sonido de terceros. Antes de una publicación comercial habría que reemplazarlos o confirmar sus licencias.

---

## Preguntas frecuentes

**¿En qué plataforma se juega?** En PC, con teclado y mouse. El soporte para mando está planificado.

**¿Cuánto dura una partida?** Es un juego de partidas cortas e intensas: pocos combates, todos importantes.

**¿Es muy difícil?** Sí, y es parte del diseño: una sola vida y jefes que castigan los errores. Pero cada golpe te enseña algo, y mejorar tu build te da herramientas para superarlos.

**¿Tiene multijugador o compras dentro del juego?** No. Es una experiencia de un jugador, de pago único y sin microtransacciones.

**¿Puedo probarlo ya?** Sí, el proyecto es público. Mira la siguiente sección.

---

## Pruébalo

1. Instala **Unity 6000.6.0f1** (con Unity Hub), **Git** y **Git LFS**.
2. Clona el repositorio. Es importante usar `git lfs install` antes, o las imágenes y los sonidos llegarán rotos:
   ```
   git lfs install
   git clone https://github.com/Anibaaal123/Juego.git
   ```
3. Abre la carpeta en Unity Hub, abre la escena `Assets/Scenes/MainMenu.unity` y presiona **Play**.

---

## Créditos

- **Diseño y programación:** Anibal San Martin — INACAP, sede San Pedro de la Paz.
- **Arte:** *Legacy Collection* de Ansimuz (personajes, jefes, fondos y tileset de cementerio), recompuesto en las plataformas y el suelo de las arenas.
- **Efectos de sonido:** *Impact Sounds* de Kenney y otros packs de efectos libres.
- **Música:** «Final Epic Battle» (autor y licencia por confirmar antes de cualquier publicación).

---

<details>
<summary><b>📄 Documentación técnica del proyecto</b> (requerimientos del GDD cubiertos, alcance y descripción de los scripts)</summary>

## 1. Requerimientos del GDD cubiertos

### Estructura Boss Rush
- **Menú principal:** Jugar, Controles y Salir; se puede usar con mouse o teclado.
- **2 arenas consecutivas:** Arena 1 (El Demonio, 250 HP) → Arena 2 (El Centinela, 220 HP).
- **Una sola vida:** al morir (o con R) la partida se reinicia desde la Arena 1 y se pierde la build. Esc abandona la partida y vuelve al menú.
- **Progresión entre arenas:** la vida, los atributos y las habilidades se conservan al pasar de arena.
- **Victoria:** al derrotar al último jefe se muestra el resumen, el mensaje de victoria y se vuelve al menú principal.

### Resumen de rendimiento y puntos de mejora
Al derrotar a un jefe el juego se pausa y aparece un resumen con tres métricas:

| Métrica | Puntaje máximo | Cómo se calcula |
|---|---|---|
| Tiempo | 40 | Máximo si se termina en el tiempo rápido de la arena (60 s en la Arena 1, 50 s en la 2); baja de forma lineal hasta 0 en el tiempo lento (150 s / 140 s) |
| Golpes recibidos | 40 | 40 menos 10 por cada golpe recibido |
| Precisión | 20 | 20 × (golpes que acertaron / golpes lanzados) |

La suma (de 0 a 100) define la **nota** y los **puntos de mejora** que gana el jugador:

| Nota | Puntuación | Puntos de mejora |
|---|---|---|
| S | 85 o más | 6 |
| A | 65 o más | 4 |
| B | 40 o más | 3 |
| C | menos de 40 | 2 |

![Resumen del combate](Docs/resumen.png)

### Tienda de mejoras
Se abre antes del primer jefe (con **2 puntos iniciales**, que solo alcanzan para mejorar atributos: las habilidades se desbloquean al vencer al primer jefe) y después de cada jefe que tenga una arena siguiente. Los puntos que no se gastan se guardan para el siguiente jefe. Al vencer a un jefe también se restaura toda la vida.

![Tienda de mejoras](Docs/tienda.png)

**Atributos** (5 niveles cada uno; el nivel 1 cuesta 1 punto, el 2 cuesta 1, el 3 cuesta 2, el 4 cuesta 2 y el 5 cuesta 3):

| Atributo | Efecto por nivel |
|---|---|
| Vida | +15 HP máximos (y recupera esos HP) |
| Daño | +10 % de daño |
| Dash | -10 % de espera del dash |
| Carga | -0.08 s para cargar el golpe cargado |

**Habilidades** (se compran una vez; en cada tienda se ofrecen 3 al azar entre las que aún no se tienen):

| Habilidad | Costo | Efecto |
|---|---|---|
| Parry | 4 | Al apretar clic derecho se abre una ventana de 0.10 s: el golpe recibido se anula por completo, el jefe queda aturdido 0.8 s y las bolas de fuego rebotan hacia él (25 de daño). Si falla, hay 0.7 s de espera; si acierta, no |
| Combo de 3 golpes | 3 | Clics seguidos (menos de 0.55 s entre uno y otro) encadenan 3 golpes; el tercero hace ×1.8 de daño y empuja al jefe |
| Onda de espada | 3 | El golpe cargado lanza además una onda de corte a distancia (×1.5 del daño base) |
| Dash doble | 3 | El dash tiene 2 cargas; cada una se recupera tras la espera del dash |
| Vampirismo | 3 | Cada golpe que acierta cura 2 HP |
| Segundo aliento | 4 | Una vez por partida, al caer a 0 HP se revive con el 30 % de la vida y 1.5 s de invulnerabilidad |

Un indicador en la esquina inferior izquierda muestra las cargas del dash, el estado del parry y las habilidades obtenidas.

### Jugador
- **Movimiento** con `Rigidbody2D`, detección de suelo (`GroundCheck`) y material sin fricción.
- **Salto con peso:** llega a 10.5 u de altura (alcanza las plataformas) y dura cerca de 1.2 s en el aire (gravedad ×5, caída 1.6 veces más rápida que la subida). Soltar Espacio durante la subida acorta el salto.
- **Dash** con invulnerabilidad temporal.
- **Cubrirse** (siempre disponible): mientras se mantiene el clic derecho, los golpes de los enemigos hacen solo el 30 % de su daño (mínimo 1) y el jugador se mueve al 50 % de velocidad. No se puede cubrir ni cargar un golpe a la vez, y el fuego del piso no se bloquea. Un golpe cubierto cuenta como golpe recibido en la nota; un parry no.
- **Golpe normal** y **golpe cargado** (×3.5 de daño, ×1.5 de alcance y empuje fuerte; el personaje se tiñe de dorado mientras carga y se mueve al 35 % de velocidad).

### Jefe 1 — El Demonio (volador, 250 HP)
Elige sus ataques al azar según la distancia, sin repetir el mismo dos veces seguidas:
- **Aliento de fuego:** quema un área frente a él cuando el jugador está cerca.
- **Picada:** se eleva, marca el suelo en rojo 0.6 s y se lanza en diagonal a 14 u/s (25 HP). Luego queda 0.7 s en el suelo, vulnerable.
- **Lluvia de fuego:** marca 5 columnas en el suelo durante 0.8 s y deja caer bolas de fuego (15 HP c/u).
- **Fase 2 al 50 % de vida:** ruge (sonido, onda expansiva y mensaje en pantalla), queda teñido de rojo y pasa a ser 30 % más rápido, con 7 columnas de lluvia y 2 picadas seguidas.

### Jefe 2 — El Centinela (rango, 220 HP)
- Se desplaza a **4.0 u/s** manteniendo entre 7 y 12 u de distancia del jugador.
- **Bola de fuego** apuntada (15 HP) y, cada 3er ataque, **abanico** de 3 proyectiles (12 HP c/u).
- **Pilar de fuego** si el jugador se acerca a menos de 3 u: se vuelve un pilar invulnerable (30 HP) y reaparece lejos.

Ambos jefes pueden quedar aturdidos por un parry exitoso, salvo mientras rugen o son un pilar de fuego.

### Ambientes
- **Terreno (ambas arenas):** plataformas como islas de roca con pasto y calaveras, y un suelo de pasto sobre adoquines morados. Los colliders no cambian: es solo aspecto.
- **Arena 1:** columnas laterales de fuego que dañan de forma periódica.
- **Arena 2 — Géiseres de fuego:** 4 grietas en el suelo; en cada ciclo 2 al azar parpadean 1.0 s (brotan llamas bajas que avisan) y luego lanzan una columna de fuego hacia arriba durante 2.0 s (unos 8 u de alto), que daña **15 HP cada 0.5 s** a quien esté dentro. El jefe es inmune a este fuego. (En el GDD era un piso eléctrico que además ralentizaba; se cambió a fuego para que encaje con el jefe.)

### Interfaz, sonido y game feel
- Barras de vida del jugador y del jefe, mensajes en pantalla, resumen de combate, tienda de mejoras e indicador de habilidades.
- Hitstop al golpear, sacudida de cámara, números de daño flotantes, textos flotantes (¡PARRY!, ¡ATURDIDO!) y retroceso del jefe.
- Efectos de sonido (salto, dash, espada, daño, muerte), rugido del demonio y música de fondo.

---

## 2. Requerimientos fuera de alcance (por ahora)

- **Jefe 3 (El Guardián) y Arena 3:** pendientes. Cuando existan, la Arena 2 también mostrará tienda y puerta (el constructor ya soporta más arenas).
- **Estamina, endlag y ventanas exactas de i-frames del GDD:** el jugador aún usa valores del prototipo (10 de daño, 3 s de espera del dash).
- **Puntaje global del GDD** (+1000 por jefe, rangos S/A/B/C sobre el total de la partida): hoy la nota se calcula por jefe y sirve para repartir puntos de mejora. `ScoreManager` sigue en la escena sin usarse.
- **Pausa manual, pantalla final con estadísticas de toda la partida y guardado de récords (`SaveData.json`).**

---

## 3. Descripción de scripts (una línea por script)

### Jugador
- **`PlayerMovement.cs`**: movimiento horizontal, salto con peso (gravedad alta, caída rápida y salto corto), dash con cargas (2 con Dash doble), ralentización y retroceso.
- **`PlayerAttack.cs`**: golpe normal, cargado y remate del combo, con daño en área, empuje, cortes visibles, onda de espada, vampirismo y game feel.
- **`PlayerHealth.cs`**: vida del jugador y su barra; aplica el cubrirse, el parry y el segundo aliento; al morir o presionar R reinicia la partida desde la Arena 1.
- **`Scripts/Player/PlayerGuard.cs`**: defensa con el clic derecho: cubrirse (daño reducido y movimiento lento) y la habilidad Parry (ventana de 0.10 s, aturdimiento del jefe y espera si falla).
- **`Scripts/Player/SwordWave.cs`**: onda de corte que lanza el golpe cargado con la habilidad Onda de espada.

### Jefes
- **`EnemyHealth.cs`**: vida de cualquier jefe, barra, parpadeo al recibir daño, números de daño, retroceso y desvanecimiento al morir.
- **`Scripts/Bosses/DemonBoss.cs`**: IA del Jefe 1 (aliento, picada, lluvia de fuego, fase 2 con rugido, elección aleatoria de ataques y aturdimiento).
- **`Scripts/Bosses/CentinelaBoss.cs`**: IA del Jefe 2 (movimiento a distancia, bola de fuego, abanico y pilar de fuego con teletransporte).
- **`Scripts/Bosses/BossProjectile.cs`**: proyectil enemigo en línea recta; con un parry exitoso rebota hacia el jefe.
- **`Scripts/Bosses/IStunnable.cs`**: interfaz de los jefes que pueden quedar aturdidos.

### Menú
- **`Scripts/UI/MainMenu.cs`**: botones del menú de inicio (Jugar, Controles, Salir) y navegación con teclado.
- **`Scripts/Run/GameScenes.cs`**: nombres de las escenas del juego (menú y primera arena).

### Boss Rush: puntos, tienda y build
- **`Scripts/Run/BuildCatalog.cs`**: catálogo de atributos y habilidades con sus costos, valores y textos.
- **`Scripts/Run/RunState.cs`**: estado de la partida entre escenas (puntos, niveles de atributos, habilidades, vida arrastrada).
- **`Scripts/Run/CombatStats.cs`**: mide tiempo, golpes recibidos, precisión y parries del combate actual.
- **`Scripts/Run/PerformanceReport.cs`**: calcula el puntaje, la nota (S/A/B/C) y los puntos de mejora de un combate.
- **`Scripts/Run/PlayerRunStats.cs`**: aplica al jugador su build al entrar a cada arena y al comprar mejoras.
- **`Scripts/Run/BossArenaManager.cs`**: controla cada arena (tienda inicial, medición del combate, resumen, curación, tienda, puerta y victoria) y la tecla Esc.
- **`Scripts/Run/ExitDoor.cs`**: puerta que lleva a la siguiente arena conservando la vida del jugador.
- **`Scripts/UI/RunPanels.cs`**: pantallas de resumen del combate y de tienda de mejoras (pausan el juego mientras están abiertas).
- **`Scripts/UI/UiKit.cs`**: utilidades para construir interfaz por código.
- **`Scripts/UI/SkillHud.cs`**: indicador de dash, parry y habilidades en pantalla.

### Arena
- **`HazardFire.cs`**: daño periódico de las columnas de fuego de la Arena 1.
- **`Scripts/Arena/FireVent.cs`**: géiseres de fuego (inactivo, advertencia, activo) con su columna de llamas, que dañan solo al jugador.
- **`Scripts/Arena/FireFloorController.cs`**: ciclo aleatorio de los géiseres de la Arena 2; se apaga al morir el jefe.

### Utilidades
- **`Scripts/Common/GameFeel.cs`**: hitstop, sacudida de cámara, números de daño y textos flotantes.
- **`Scripts/Common/ProceduralFx.cs`**: corte visual y rugido sintetizado, generados por código.
- **`Scripts/Common/SimpleSpriteAnimation.cs`**, **`FadeAndDestroy.cs`**, **`WarningPulse.cs`**: animación por cuadros, desvanecido de efectos y parpadeo de advertencias.
- **`Editor/ArenaBuilder.cs`**: menú *Colossus → Construir Boss Rush* que configura la Arena 1, aplica el arte del terreno (plataformas y suelo), genera la Arena 2 (conservando su GUID) y ordena las escenas en Build Settings.
- **`Editor/MenuBuilder.cs`**: menú *Colossus → Construir Menú Principal* que genera la escena del menú.

### Heredados del prototipo (sin uso en las escenas actuales)
- **`EnemyAttack.cs`** (reemplazado por `DemonBoss`), **`Collectible.cs`** (monedas, reemplazadas por la tienda de puntos), **`DamageZone.cs`** y **`ScoreManager.cs`** (en escena, pendiente de conectar).

</details>
