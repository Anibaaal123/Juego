# Colossus: Boss Rush

Juego de acción 2D en arenas cerradas (Boss Rush / Soulslike) hecho en **Unity 6 (6000.6.0f1)**.
El jugador enfrenta jefes consecutivos con una sola vida y elige una mejora aleatoria por rareza entre cada combate.

![Menú principal](Docs/menu.png)

## Cómo jugar

| Acción | Control |
|---|---|
| Moverse | A / D |
| Saltar | Espacio |
| Golpe normal | Clic izquierdo (toque) |
| Golpe cargado | Mantener clic izquierdo 0.6 s y soltar |
| Esquiva (dash) | Shift izquierdo |
| Reiniciar partida | R |
| Volver al menú | Esc |

Para jugar en el editor: abrir `Assets/Scenes/MainMenu.unity` y presionar Play.

---

## 1. Requerimientos del GDD cubiertos

### Estructura Boss Rush
- **Menú principal:** Jugar, Controles y Salir; se puede usar con mouse o teclado.
- **2 arenas consecutivas:** Arena 1 (El Demonio, 250 HP) → Arena 2 (El Centinela, 220 HP).
- **Una sola vida:** al morir (o con R) la partida se reinicia desde la Arena 1 y se pierden las mejoras. Esc abandona la partida y vuelve al menú.
- **Progresión entre arenas:** la vida y las mejoras obtenidas se conservan al pasar de arena.
- **Recompensa por jefe:** al vencer al Jefe 1 el jugador recupera toda su vida, aparecen los pedestales y, tras elegir, se abre la puerta a la siguiente arena en 1.5 s.
- **Victoria:** al derrotar al último jefe se muestra el mensaje de victoria y se vuelve al menú principal.

### Pedestales de mejora (RNG ponderado)
Aparecen 2 pedestales de tipos distintos; cada uno tira `Random.Range(1, 101)` para definir su rareza. Al pisar uno, se aplica la mejora y el otro se destruye.

| Rareza | Probabilidad | Color | Núcleo de Poder | Núcleo de Agilidad | Núcleo Vital |
|---|---|---|---|---|---|
| Común | 60 % | Blanco | +15 % daño | -15 % espera del dash | +20 HP máx. |
| Raro | 30 % | Azul | +40 % daño | -35 % espera del dash | +50 HP máx. |
| Legendario | 10 % | Dorado | +80 % daño | -60 % espera del dash | +100 HP máx. y curación total |

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

### Ambientes
- **Arena 1:** columnas laterales de fuego que dañan de forma periódica.
- **Arena 2 — Piso de sobrecarga eléctrica:** 4 baldosas; en cada ciclo 2 al azar parpadean 1.0 s y descargan 2.0 s, aplicando **15 HP cada 0.5 s** y **ralentizando al 50 % por 1.0 s** a cualquier entidad encima (también al jefe).

### Interfaz, sonido y game feel
- Barras de vida del jugador y del jefe, mensajes en pantalla y textos sobre los pedestales.
- Hitstop al golpear, sacudida de cámara, números de daño flotantes y retroceso del jefe.
- Efectos de sonido (salto, dash, espada, daño, muerte), rugido del demonio y música de fondo.

---

## 2. Requerimientos fuera de alcance (por ahora)

- **Jefe 3 (El Guardián) y Arena 3:** pendientes. Cuando existan, la Arena 2 también mostrará pedestales y puerta.
- **Estamina, endlag y ventanas exactas de i-frames del GDD:** el jugador aún usa valores del prototipo (10 de daño, 3 s de espera del dash).
- **Sistema de puntuación y rangos S/A/B/C:** `ScoreManager` está en la escena pero todavía no suma puntos.
- **Pausa, pantalla de estadísticas al ganar y guardado de récords (`SaveData.json`).**

---

## 3. Descripción de scripts (una línea por script)

### Jugador
- **`PlayerMovement.cs`**: movimiento horizontal, salto con detección de suelo, dash con invulnerabilidad, ralentización y retroceso.
- **`PlayerAttack.cs`**: golpe normal y golpe cargado, con daño en área, empuje al enemigo, cortes visibles y game feel.
- **`PlayerHealth.cs`**: vida del jugador y su barra; al morir o presionar R reinicia la partida desde la Arena 1.

### Jefes
- **`EnemyHealth.cs`**: vida de cualquier jefe, barra, parpadeo al recibir daño, números de daño, retroceso y desvanecimiento al morir.
- **`Scripts/Bosses/DemonBoss.cs`**: IA del Jefe 1 (aliento, picada, lluvia de fuego, fase 2 con rugido y elección aleatoria de ataques).
- **`Scripts/Bosses/CentinelaBoss.cs`**: IA del Jefe 2 (movimiento a distancia, bola de fuego, abanico y pilar de fuego con teletransporte).
- **`Scripts/Bosses/BossProjectile.cs`**: proyectil enemigo en línea recta que daña al jugador y se destruye al chocar.

### Menú
- **`Scripts/UI/MainMenu.cs`**: botones del menú de inicio (Jugar, Controles, Salir) y navegación con teclado.
- **`Scripts/Run/GameScenes.cs`**: nombres de las escenas del juego (menú y primera arena).

### Boss Rush
- **`Scripts/Run/RunState.cs`**: guarda las mejoras y la vida del jugador entre escenas.
- **`Scripts/Run/Upgrade.cs`**: define los tipos de mejora, la tirada de rareza (60/30/10 %) y sus valores.
- **`Scripts/Run/PlayerRunStats.cs`**: aplica las mejoras acumuladas al jugador al entrar a cada arena.
- **`Scripts/Run/BossArenaManager.cs`**: controla el final de cada arena (curación, pedestales, puerta, victoria y mensajes) y la tecla Esc para volver al menú.
- **`Scripts/Run/UpgradePedestal.cs`**: pedestal que muestra su mejora y la aplica al pisarlo.
- **`Scripts/Run/ExitDoor.cs`**: puerta que lleva a la siguiente arena conservando la vida del jugador.

### Arena
- **`HazardFire.cs`**: daño periódico de las columnas de fuego de la Arena 1.
- **`Scripts/Arena/ElectricTile.cs`**: baldosa eléctrica (inactiva, advertencia, activa) que daña y ralentiza.
- **`Scripts/Arena/ElectricFloorController.cs`**: ciclo aleatorio de las baldosas de la Arena 2; se apaga al morir el jefe.

### Utilidades
- **`Scripts/Common/GameFeel.cs`**: hitstop, sacudida de cámara y números de daño flotantes.
- **`Scripts/Common/ProceduralFx.cs`**: corte visual y rugido sintetizado, generados por código.
- **`Scripts/Common/SimpleSpriteAnimation.cs`**, **`FadeAndDestroy.cs`**, **`WarningPulse.cs`**: animación por cuadros, desvanecido de efectos y parpadeo de advertencias.
- **`Editor/ArenaBuilder.cs`**: menú *Colossus → Construir Boss Rush* que configura la Arena 1, genera la Arena 2 y ordena las escenas en Build Settings.
- **`Editor/MenuBuilder.cs`**: menú *Colossus → Construir Menú Principal* que genera la escena del menú.

### Heredados del prototipo (sin uso en las escenas actuales)
- **`EnemyAttack.cs`** (reemplazado por `DemonBoss`), **`Collectible.cs`** (monedas, reemplazadas por los pedestales), **`DamageZone.cs`** y **`ScoreManager.cs`** (en escena, pendiente de conectar).
