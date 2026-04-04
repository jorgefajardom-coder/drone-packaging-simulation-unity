# Drone Packaging Simulation — Unity

**English** | [Español](#simulación-de-empaquetado-de-dron--unity)

---

## Overview

This project is a Unity-based industrial simulation of a robotic drone assembly cell. It reproduces an automated production process in which three articulated robotic arms collaborate to assemble a drone through physically realistic interactions, coordinated motion sequences, and differentiated gripping mechanisms.

The simulation is intended for virtual process validation in industrial and academic contexts, drawing inspiration from systems similar to those found in Festo-type automation platforms.

---

## Technical Architecture

The system is built around three robotic arms, each with a distinct end effector role:

| Arm | End Effector | Role |
|-----|-------------|------|
| Arm 1 | Gripper | Handling of large components |
| Arm 2 | Gripper | Handling of large components |
| Arm 3 | Suction Cup | Handling of delicate components |

A central orchestrator MonoBehaviour coordinates the assembly stages, reading a master JSON file and activating each arm in the correct order while managing inter-arm dependencies through chained coroutines.

---

## Systems Implemented

### Gripper System

Physically stable object attachment via `SetParent`, preserving world-space position and rotation before re-parenting to avoid unexpected flips or teleportation artifacts.

### Suction Cup System

A magnetic attraction animation is executed before attachment, using `Vector3.Lerp` and `Quaternion.Lerp` with a custom `AnimationCurve` for easing. Once the animation completes, the object is fixed to the suction tip using the same world-space preservation technique as the gripper.

### JSON Motion Sequencer

Each arm's movement is defined in an external JSON file specifying target position, target rotation, duration, and action on completion (grip / release / none). This architecture decouples motion data from logic, allowing sequence changes without recompilation.

```json
{
  "arm": "Arm1",
  "sequence": [
    {
      "position": { "x": 1.2, "y": 0.5, "z": -0.3 },
      "rotation": { "x": 0, "y": 90, "z": 0 },
      "duration": 2.0,
      "action": "grip"
    }
  ]
}
```

### Multi-Arm Orchestrator

`OrquestadorDron.cs` is the central coordination MonoBehaviour. It reads the master assembly sequence, triggers each arm in the correct order, and awaits completion callbacks before advancing to the next stage.

### Projectile Release Point Calculation

Inverse projectile kinematics are used to calculate the exact horizontal position at which an arm must release an object so that it lands on a target, accounting for free-fall physics from the release height.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── BrazoController.cs
│   ├── GripperController.cs
│   ├── VentosaController.cs
│   ├── OrquestadorDron.cs
│   └── SecuenciadorJSON.cs
├── Data/
│   ├── secuencia_brazo1.json
│   ├── secuencia_brazo2.json
│   ├── secuencia_brazo3.json
│   └── maestro_ensamble.json
└── Prefabs/
    └── CajaPrefab.prefab
```

---

## Robotic Arm Hierarchy

```
BrazoBase (fixed)
└── Hombro (Revolute)
    └── BrazoSuperior (Revolute)
        └── Codo (Revolute)
            └── Antebrazo (Revolute)
                └── Muñeca (Revolute)
                    └── GripperBase (fixed)
                        ├── PinzaIzquierda (Prismatic)
                        └── PinzaDerecha (Prismatic)
```

---

## Key Technologies

- Unity (C#) — MonoBehaviour, Coroutines, ArticulationBody
- ArticulationDrive — stiffness, damping, force limit, target
- SetParent with world-space preservation
- Vector3.Lerp / Quaternion.Lerp with AnimationCurve easing
- JsonUtility / Newtonsoft.Json for sequence parsing
- Projectile physics for release point calculation
- Unity FBX Exporter for asset export

---

## Physics Configuration

| Parameter | Purpose |
|-----------|---------|
| stiffness | Joint rigidity — higher values produce firmer response |
| damping | Oscillation attenuation |
| forceLimit | Maximum applicable force |
| target | Target position or rotation value |

Collision layers are configured to prevent undesired self-collisions between arm segments. Solver iterations are tuned on the base Rigidbody to ensure simulation stability.

---

## Known Issues Resolved

| Issue | Cause | Solution |
|-------|-------|----------|
| Object rotates on grip | `localRotation = Quaternion.identity` after SetParent | Removed; world rotation preserved |
| Object teleports on grip | SetParent without saving prior position | Save and restore world position/rotation |
| Rigid suction animation | No easing applied | Added AnimationCurve to Lerp |
| Arms interfering with each other | No phase coordination | Implemented OrquestadorDron with chained coroutines |
| Joint oscillations | Low damping relative to segment mass | Increased damping; tuned stiffness per segment |
| Gripper penetrating segments | Incorrect collider configuration | Configured collision layers and adjusted collider shapes |

---

## Authors

Jorge Andres Fajardo Mora
Laura Vanesa Castro Sierra

---

## License and Rights

Copyright (c) 2025 Jorge Andres Fajardo Mora and Laura Vanesa Castro Sierra. All rights reserved.

This repository and all its contents — including but not limited to source code, scripts, configuration files, data files, and documentation — are provided for read-only and reference purposes only. No permission is granted to copy, modify, distribute, sublicense, or use any part of this project for commercial or non-commercial purposes without explicit written authorization from the authors.

Unauthorized reproduction or redistribution of this work, in whole or in part, is strictly prohibited.

---
---

# Simulación de Empaquetado de Dron — Unity

[English](#drone-packaging-simulation--unity) | **Español**

---

## Descripción General

Este proyecto es una simulación industrial basada en Unity que recrea una celda robótica de ensamblaje de drones. Reproduce un proceso de producción automatizado en el que tres brazos robóticos articulados colaboran para ensamblar un dron mediante interacciones físicas realistas, secuencias de movimiento coordinadas y mecanismos de agarre diferenciados.

La simulación está orientada a la validación virtual de procesos en contextos industriales y académicos, con una arquitectura inspirada en sistemas de automatización tipo Festo.

---

## Arquitectura Técnica

El sistema se compone de tres brazos robóticos, cada uno con un rol específico de efector final:

| Brazo | Efector Final | Rol |
|-------|--------------|-----|
| Brazo 1 | Gripper (pinza) | Manejo de componentes de gran tamaño |
| Brazo 2 | Gripper (pinza) | Manejo de componentes de gran tamaño |
| Brazo 3 | Ventosa | Manejo de componentes delicados |

Un MonoBehaviour orquestador central coordina las etapas de ensamble, leyendo un archivo JSON maestro y activando cada brazo en el orden correcto mediante coroutines encadenadas que gestionan las dependencias entre brazos.

---

## Sistemas Implementados

### Sistema de Agarre con Gripper

Fijación estable de objetos mediante `SetParent`, preservando posición y rotación en espacio global antes del reparenteo para evitar giros inesperados o artefactos de teletransportación.

### Sistema de Agarre con Ventosa

Se ejecuta una animación de atracción magnética antes de la fijación del objeto, utilizando `Vector3.Lerp` y `Quaternion.Lerp` con una `AnimationCurve` personalizada para el suavizado. Una vez completada la animación, el objeto queda fijado a la punta de la ventosa con la misma técnica de preservación de espacio global que el gripper.

### Secuenciador de Movimientos en JSON

Los movimientos de cada brazo se definen en archivos JSON externos que especifican posición objetivo, rotación objetivo, duración y acción al finalizar (agarrar / soltar / ninguna). Esta arquitectura desacopla los datos de movimiento de la lógica del sistema, permitiendo modificar secuencias sin necesidad de recompilar el proyecto.

```json
{
  "brazo": "Brazo1",
  "secuencia": [
    {
      "posicion": { "x": 1.2, "y": 0.5, "z": -0.3 },
      "rotacion": { "x": 0, "y": 90, "z": 0 },
      "duracion": 2.0,
      "accion": "agarrar"
    }
  ]
}
```

### Orquestador Multi-Brazo

`OrquestadorDron.cs` es el MonoBehaviour central de coordinación. Lee la secuencia de ensamble maestra, activa cada brazo en el orden correcto y espera los callbacks de finalización antes de avanzar a la siguiente etapa.

### Calculo de Punto de Lanzamiento

Se aplica cinematica inversa de proyectil para calcular la posicion horizontal exacta en la que el brazo debe soltar un objeto para que este aterrice sobre un objetivo, considerando la caida libre desde la altura de release.

---

## Estructura del Proyecto

```
Assets/
├── Scripts/
│   ├── BrazoController.cs
│   ├── GripperController.cs
│   ├── VentosaController.cs
│   ├── OrquestadorDron.cs
│   └── SecuenciadorJSON.cs
├── Data/
│   ├── secuencia_brazo1.json
│   ├── secuencia_brazo2.json
│   ├── secuencia_brazo3.json
│   └── maestro_ensamble.json
└── Prefabs/
    └── CajaPrefab.prefab
```

---

## Jerarquia del Brazo Robotico

```
BrazoBase (fixed)
└── Hombro (Revolute)
    └── BrazoSuperior (Revolute)
        └── Codo (Revolute)
            └── Antebrazo (Revolute)
                └── Muñeca (Revolute)
                    └── GripperBase (fixed)
                        ├── PinzaIzquierda (Prismatic)
                        └── PinzaDerecha (Prismatic)
```

---

## Tecnologias Utilizadas

- Unity (C#) — MonoBehaviour, Coroutines, ArticulationBody
- ArticulationDrive — stiffness, damping, force limit, target
- SetParent con preservacion de espacio global
- Vector3.Lerp / Quaternion.Lerp con easing mediante AnimationCurve
- JsonUtility / Newtonsoft.Json para parsing de secuencias
- Fisica de proyectil para calculo de punto de release
- Unity FBX Exporter para exportacion de assets

---

## Configuracion de Fisica

| Parametro | Funcion |
|-----------|--------|
| stiffness | Rigidez de la articulacion — valores altos producen respuesta mas firme |
| damping | Amortiguacion de oscilaciones |
| forceLimit | Fuerza maxima aplicable |
| target | Valor objetivo de posicion o rotacion |

Se configuraron capas de colision para evitar auto-colisiones no deseadas entre los segmentos del brazo. Las solver iterations se ajustaron en el Rigidbody de la base para garantizar estabilidad en la simulacion.

---

## Problemas Resueltos

| Problema | Causa | Solucion |
|----------|-------|----------|
| El objeto rota al agarrarlo | `localRotation = Quaternion.identity` despues del SetParent | Eliminada esa linea; se preserva la rotacion global |
| El objeto se teletransporta al agarrarlo | SetParent sin guardar posicion previa | Se guarda y restaura posicion/rotacion en espacio global |
| Animacion de ventosa rigida | Sin suavizado aplicado | Se añadio AnimationCurve al Lerp |
| Brazos interfiriendo entre si | Sin coordinacion de fases | Se implemento OrquestadorDron con coroutines encadenadas |
| Oscilaciones en articulaciones | Damping bajo en relacion al peso del segmento | Se incremento damping y se ajusto stiffness por segmento |
| Gripper penetra otros segmentos | Configuracion incorrecta de colliders | Se configuraron capas de colision y se ajustaron formas de colliders |

---

## Autores

Jorge Andres Fajardo Mora
Laura Vanesa Castro Sierra

---

## Licencia y Derechos

Copyright (c) 2025 Jorge Andres Fajardo Mora y Laura Vanesa Castro Sierra. Todos los derechos reservados.

Este repositorio y la totalidad de su contenido — incluyendo, entre otros, codigo fuente, scripts, archivos de configuracion, archivos de datos y documentacion — se proporcionan exclusivamente para fines de lectura y referencia. No se otorga ningun permiso para copiar, modificar, distribuir, sublicenciar ni utilizar ninguna parte de este proyecto con fines comerciales o no comerciales sin autorizacion escrita explicita de los autores.

Queda estrictamente prohibida la reproduccion o redistribucion no autorizada de este trabajo, en todo o en parte.
