# Colossus: Boss Rush

Juego de acción 2D en arenas cerradas (Boss Rush / Soulslike) hecho en **Unity 6 (6000.6.0f1)**.
El jugador enfrenta jefes consecutivos con una sola vida. Después de cada combate recibe un **resumen de su rendimiento** y una cantidad de **puntos de mejora** según la nota obtenida; con ellos arma su propia build (atributos y habilidades), así que ninguna partida es igual a otra.

![Menú principal](Docs/menu.png)

## Cómo jugar

| Acción | Control |
|---|---|
| Moverse | A / D |
| Saltar | Espacio |
| Golpe normal | Clic izquierdo (toque) |
| Golpe cargado | Mantener clic izquierdo 0.6 s y soltar |
| Esquiva (dash) | Shift izquierdo |
| Parry (habilidad) | Clic derecho |
| Reiniciar partida | R |
| Volver al menú | Esc |

Para jugar en el editor: abrir `Assets/Scenes/MainMenu.unity` y presionar Play.

---

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
| Parry | 4 | Clic derecho abre una ventana de 0.10 s: el golpe recibido se anula, el jefe queda aturdido 0.8 s y las bolas de fuego rebotan hacia él (25 de daño). Si falla, hay 0.7 s de espera; si acierta, no |
| Combo de 3 golpes | 3 | Clics seguidos (menos de 0.55 s entre uno y otro) encadenan 3 golpes; el tercero hace ×1.8 de daño y empuja al jefe |
| Onda de espada | 3 | El golpe cargado lanza además una onda de corte a distancia (×1.5 del daño base) |
| Dash doble | 3 | El dash tiene 2 cargas; cada una se recupera tras la espera del dash |
| Vampirismo | 3 | Cada golpe que acierta cura 2 HP |
| Segundo aliento | 4 | Una vez por partida, al caer a 0 HP se revive con el 30 % de la vida y 1.5 s de invulnerabilidad |

Un indicador en la esquina inferior izquierda muestra las cargas del dash, el estado del parry y las habilidades obtenidas.

### Jugador
- **Movimiento y salto** con `Rigidbody2D`, detección de suelo (`GroundCheck`) y material sin fricción.
- **Dash** con invulnerabilidad temporal.
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
- **`PlayerMovement.cs`**: movimiento horizontal, salto con detección de suelo, dash con cargas (2 con Dash doble), ralentización y retroceso.
- **`PlayerAttack.cs`**: golpe normal, cargado y remate del combo, con daño en área, empuje, cortes visibles, onda de espada, vampirismo y game feel.
- **`PlayerHealth.cs`**: vida del jugador y su barra; aplica el parry y el segundo aliento; al morir o presionar R reinicia la partida desde la Arena 1.
- **`Scripts/Player/PlayerParry.cs`**: habilidad Parry (ventana de 0.10 s, aturdimiento del jefe y espera si falla).
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
- **`Editor/ArenaBuilder.cs`**: menú *Colossus → Construir Boss Rush* que configura la Arena 1, genera la Arena 2 (conservando su GUID) y ordena las escenas en Build Settings.
- **`Editor/MenuBuilder.cs`**: menú *Colossus → Construir Menú Principal* que genera la escena del menú.

### Heredados del prototipo (sin uso en las escenas actuales)
- **`EnemyAttack.cs`** (reemplazado por `DemonBoss`), **`Collectible.cs`** (monedas, reemplazadas por la tienda de puntos), **`DamageZone.cs`** y **`ScoreManager.cs`** (en escena, pendiente de conectar).
