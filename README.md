# Prototipo 2D: Arena de Combate y Jefe

## 1. Requerimientos del GDD Cubiertos

- **Control del Personaje 2D:** Movimiento horizontal controlado por teclado con velocidad ajustable vía Inspector (`[SerializeField] private`) y volteo automático del sprite (`flipX`).
- **Físicas y Salto Estricto:** Salto implementado mediante `Rigidbody2D` con material sin fricción (`NoFriction`) y detección rigurosa de suelo (`GroundCheck`), impidiendo saltos infinitos en el aire.
- **Mecánicas Avanzadas:** Habilidad de esquiva rápida (*Dash*) con ventana de invulnerabilidad temporal y combate cuerpo a cuerpo funcional tanto en tierra como en el aire.
- **Enemigo / Jefe Aéreo:** Inteligencia artificial con persecución omnidireccional en 2D, oscilación sinusoidal, ráfaga de fuego sincronizada, collider en modo *Trigger* para no trabarse en plataformas y daño por contacto con enfriamiento.
- **Zonas de Daño Ambiental:** Columnas laterales de fuego con daño periódico continuo al entrar en contacto.
- **Coleccionables y Contador:** Monedas con detección por disparador (`Trigger`), eliminación del objeto al contacto e incremento visual en el contador del juego.
- **Sonido Integral:** Múltiples efectos de sonido (SFX) para salto, dash, ataque de espada, impactos recibidos y muertes, acompañados de música de fondo en bucle.
- **Interfaz y Flujo:** Barras de vida dedicadas para el héroe y el jefe, reinicio manual con la tecla **R** y reinicio automático tras la derrota o victoria.

---

## 2. Requerimientos Fuera de Alcance y Justificación

- **Nivel Extenso con Scroll Lateral:** Se descartó para priorizar una arena de combate cerrada con plataformas suspendidas, permitiendo concentrar el desarrollo en la inteligencia artificial, el balance del jefe volador y las físicas requeridas en la evaluación.
- **Variedad de Armas y Proyectiles:** Quedó fuera para consolidar la arquitectura modular del combate cuerpo a cuerpo y la respuesta de impacto sin duplicar estados de juego.
- **Menú Principal y Pantalla de Opciones:** Se postergó para la siguiente entrega curricular (Evaluación 3), enfocando esta entrega en la lógica interna del motor y la solidez del prototipo jugable.

---

## 3. Descripción de Scripts (Una línea por script)

- **`PlayerMovement.cs`**: Controla el desplazamiento horizontal, el salto físico con detección de suelo, el dash con invulnerabilidad y la actualización del Animator.
- **`PlayerAttack.cs`**: Detecta y ejecuta el ataque cuerpo a cuerpo orientado según el frente del personaje, aplicando daño a los enemigos alcanzados.
- **`PlayerHealth.cs`**: Gestiona la vida del jugador, sincroniza su barra en la interfaz y administra el reinicio de la escena al morir o presionar R.
- **`EnemyAttack.cs`**: Maneja la navegación de vuelo del jefe, el seguimiento al héroe, el daño por contacto corporal y la ráfaga de fuego.
- **`EnemyHealth.cs`**: Controla la salud del jefe, actualiza su barra de vida en pantalla y ejecuta la secuencia de victoria al llegar a 0.
- **`HazardFire.cs`**: Aplica daño periódico continuo al jugador mientras permanezca dentro del área de peligro ambiental de los laterales.
- **`Collectible.cs`**: Registra la recolección del ítem al colisionar con el jugador, suma al contador y destruye el objeto.
- **`ScoreManager.cs`**: Centraliza el almacenamiento del valor numérico de coleccionables y actualiza su visualización en pantalla.
