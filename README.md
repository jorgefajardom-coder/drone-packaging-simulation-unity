# Drone Packaging Simulation — Unity

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2021.3.45f1_LTS-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-10.0-239120?style=for-the-badge&logo=c-sharp)
![ArticulationBody](https://img.shields.io/badge/Physics-ArticulationBody-orange?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active-success?style=for-the-badge)

**Robotic Assembly Cell Simulation**  
Coordinated Articulated Arms · JSON-Driven Motion · Realistic Physics

**English** | [Español](#simulación-de-empaquetado-de-dron--unity)

</div>

---

## Table of Contents

- [Overview](#overview)
- [Technical Stack](#technical-stack)
- [System Architecture](#system-architecture)
- [Implemented Systems](#implemented-systems)
- [Project Structure](#project-structure)
- [Installation](#installation)
- [Resolved Issues](#resolved-issues)
- [Authors](#authors)
- [License](#license-and-rights)

---

## Overview

This project is a **Unity-based simulation** of a robotic drone assembly cell. It reproduces an automated production process in which three articulated robotic arms collaborate to assemble a drone through physically realistic interactions, coordinated motion sequences, and differentiated gripping mechanisms.

The simulation is intended for **virtual process validation** in technical and academic contexts.

### Key Features

- 🦾 **Four coordinated robotic arms** (Alpha, Beta, Omega, Paletizador) with ArticulationBody physics
- 🔄 **8-stage assembly orchestration** with JSON-driven sequences
- ⚙️ **Dual end effectors**: Gripper (`Brazos.cs`) and Suction Cup (`Ventosa.cs`)
- 🎯 **Proximity-based snap system** for component assembly
- 📊 **Coroutine-based asynchronous execution** with dependency management
- 🔧 **World-space preservation** to prevent rotation artifacts
- 🏭 **Production spawner** with staggered coroutine-based part instantiation
- 📦 **Palletizer arm** with dual-cart rotation system — continuous packing cycle synchronized with assembly time

---

## Technical Stack

### Unity 2021.3.45f1 LTS

| Criterion | Justification |
|-----------|---------------|
| **LTS (Long-Term Support)** | Stability guaranteed through 2024, ideal for simulation projects |
| **Mature ArticulationBody** | Introduced in 2020.1, fully stable in 2021.3 for precise robotic simulation |
| **Deterministic Physics** | Configurable solver iterations, essential for robotics |
| **C# 10.0** | Modern language features: records, pattern matching, global usings |
| **Native JSON Support** | Optimized `JsonUtility` for pose serialization/deserialization |
| **Performance** | DOTS preview available for future scalability |

### Core Unity Components

```csharp
ArticulationBody      // Robotic joint system (superior to standard Rigidbody)
ArticulationDrive     // Motor control (target, stiffness, damping)
ArticulationJointType // Revolute (rotation) and Prismatic (linear)
Coroutines            // Asynchronous sequences
JsonUtility           // Data serialization (RobotPose / VentosaPose)
Physics.IgnoreCollision // Dynamic collision control
```

### Package Dependencies (`manifest.json`)

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.formats.fbx` | 4.1.3 | Asset export for external workflows |
| `com.unity.textmeshpro` | 3.0.6 | UI text rendering |
| `com.unity.timeline` | 1.6.5 | Animation timeline support |
| `com.unity.visualscripting` | 1.9.4 | Visual scripting support |
| `com.unity.collab-proxy` | 2.5.2 | Version control integration |
| `com.unity.test-framework` | 1.1.33 | Unit testing |

---

## System Architecture

### Component Diagram

```mermaid
graph TB
    subgraph "Orchestration Layer"
        O[OrquestadorDron]
        JSON[ensamblaje_dron.json<br/>4 Stages]
        P[Produccion.cs<br/>Spawn Sequencer]
    end
    
    subgraph "Robotic Arms Layer"
        B1[Alpha<br/>Brazos — Gripper]
        B2[Beta<br/>Brazos — Gripper]
        B3[Omega<br/>Ventosa — Suction]
        B4[Paletizador<br/>Ventosa — Suction]
    end
    
    subgraph "Grip Systems Layer"
        G[GripperTrigger]
        V[SuctionTrigger]
        E1[Ensamble.cs]
        E2[EnsambleGri.cs]
    end
    
    subgraph "Drone Components"
        BASE[Base]
        PCB[PCB]
        MOTOR[Motors x4]
        HELICE[Hélices x4]
        TAPA[Tapa]
    end

    subgraph "Palletizing System"
        CARRO1[Cart 1<br/>boxes]
        CARRO2[Cart 2<br/>boxes]
    end
    
    O -->|Reads JSON| JSON
    O -->|Coordinates| B1
    O -->|Coordinates| B2
    O -->|Coordinates| B3
    O -->|Coordinates| B4
    P -->|Spawns parts| BASE
    P -->|Spawns parts| PCB
    P -->|Spawns parts| MOTOR
    P -->|Spawns parts| HELICE
    P -->|Spawns parts| TAPA
    
    B1 -.->|Uses| G
    B2 -.->|Uses| G
    B3 -.->|Uses| V
    B4 -.->|Uses| V
    
    G -->|Snap Logic| E2
    V -->|Snap Logic| E1
    
    B1 -->|Assembles| BASE
    B3 -->|Assembles| PCB
    B1 -->|Assembles| MOTOR
    B2 -->|Assembles| MOTOR
    B1 -->|Assembles| HELICE
    B2 -->|Assembles| HELICE
    B3 -->|Assembles| TAPA
    B3 -->|Moves drone to zone| B4
    B4 -->|Palletizes into| CARRO1
    B4 -->|Palletizes into| CARRO2
    
    style O fill:#534AB7,stroke:#26215C,color:#fff
    style B1 fill:#1D9E75,stroke:#085041,color:#fff
    style B2 fill:#1D9E75,stroke:#085041,color:#fff
    style B3 fill:#378ADD,stroke:#042C53,color:#fff
    style B4 fill:#B75A34,stroke:#5C2506,color:#fff
```

### Arm Configuration

| Arm | Class | End Effector | Role | Components Handled |
|-----|-------|-------------|------|-------------------|
| **Alpha** | `Brazos.cs` | Gripper (pinza) | Assembly — large & mechanical parts | Base, diagonal Motors x2, diagonal Hélices x2 |
| **Beta** | `Brazos.cs` | Gripper (pinza) | Assembly — mechanical parts (paired with Alpha) | diagonal Motors x2, diagonal Hélices x2 |
| **Omega** | `Ventosa.cs` | Suction Cup (ventosa) | Assembly — delicate parts + drone transfer | PCB, Tapa, completed drone → staging zone |
| **Paletizador** | `Ventosa.cs` | Suction Cup (ventosa) | Palletizing — picks drone, fills carts | Completed drones → Cart 1 / Cart 2 boxes |

### Assembly Sequence Flow

```mermaid
sequenceDiagram
    participant O as OrquestadorDron
    participant A as Alpha (Brazos)
    participant B as Beta (Brazos)
    participant W as Omega (Ventosa)
    participant P as Paletizador (Ventosa)
    participant D as Assembled Drone
    participant C as Cart 1 / Cart 2

    Note over O: Start — Load ensamblaje_dron.json

    O->>A: Stage 1 — Place Base
    activate A
    A->>D: Grip & release Base at assembly point
    A-->>O: jugandoSecuencia = false
    deactivate A

    O->>W: Stage 2 — Place PCB
    activate W
    W->>D: Suction grip → assemble PCB on Base
    W-->>O: jugandoSecuencia = false
    deactivate W

    O->>A: Stage 3 — Diagonal Motors (1 & 3)
    O->>B: Stage 3 — Diagonal Motors (1 & 3)
    activate A
    activate B
    A->>D: Assemble Motor (diagonal pair)
    B->>D: Assemble Motor (diagonal pair)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>A: Stage 4 — Diagonal Motors (2 & 4)
    O->>B: Stage 4 — Diagonal Motors (2 & 4)
    activate A
    activate B
    A->>D: Assemble Motor (diagonal pair)
    B->>D: Assemble Motor (diagonal pair)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>W: Stage 5 — Place Tapa
    activate W
    W->>D: Suction grip → assemble Tapa (final closure)
    W-->>O: jugandoSecuencia = false
    deactivate W

    O->>A: Stage 6 — Diagonal Hélices (1 & 3)
    O->>B: Stage 6 — Diagonal Hélices (1 & 3)
    activate A
    activate B
    A->>D: Assemble Hélice (diagonal pair)
    B->>D: Assemble Hélice (diagonal pair)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>A: Stage 7 — Diagonal Hélices (2 & 4)
    O->>B: Stage 7 — Diagonal Hélices (2 & 4)
    activate A
    activate B
    A->>D: Assemble Hélice (diagonal pair)
    B->>D: Assemble Hélice (diagonal pair)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>W: Stage 8 — Transfer drone to staging zone
    activate W
    W->>W: Suction grip completed drone
    W->>D: Place drone in Paletizador staging zone
    W-->>O: jugandoSecuencia = false
    deactivate W

    Note over O,D: Assembly complete (< 1 min cycle) — Paletizador takes over

    loop Until Cart is full
        P->>D: Pick drone from staging zone
        P->>C: Move to active cart
        P->>C: Place drone in box & close box
    end

    Note over P,C: Cart 1 full → exits zone
    Note over P,C: Paletizador switches to Cart 2
    Note over P,C: Cart 1 returns with empty boxes while Cart 2 is being filled
    Note over P,C: Cycle repeats continuously
```

### Script Interaction Diagram

```mermaid
classDiagram
    class OrquestadorDron {
        +Brazos alfa
        +Brazos beta
        +Ventosa omega
        +Ventosa paletizador
        +string archivoMaestro
        +int etapaActual
        +bool ensamblajeFinalizado
        +CargarMaestro()
        +EjecutarEtapa(int index)
    }
    
    class Brazos {
        +ArticulationBody Waist
        +ArticulationBody Arm01
        +ArticulationBody Arm02
        +ArticulationBody Arm03
        +ArticulationBody GripperAssembly
        +ArticulationBody Gear1
        +ArticulationBody Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +string saveFileName
        +IniciarSecuencia()
        +LoadFromFile()
        +SaveToFile()
        +AgarrarObjeto()
        +LiberarObjeto()
    }
    
    class Ventosa {
        +ArticulationBody Waist
        +ArticulationBody Arm01
        +ArticulationBody Arm02
        +ArticulationBody Arm03
        +bool suctionActive
        +float suctionForce
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +LoadFromFile()
        +NotifyObjectInside()
    }
    
    class Ensamble {
        +Transform puntoEnsamble
        +float offsetHundimiento
        +float velocidadEncaje
        +bool snapPorProximidad
        +float distanciaActivacionSnap
        +bool congelarAlLiberar
        +Vector3 rotacionFinalEnsamble
        +NotificarLiberad()
    }
    
    class EnsambleGri {
        +Transform puntoEnsamble
        +float distanciaActivacion
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +Vector3 rotacionForzada
        +bool usarRotacionPorNumero
        +Transform baseParent
        +ConfigurarRotacionPorNumero()
    }
    
    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Spawn()
    }
    
    class Produccion {
        +Spawner spawnBase
        +Spawner spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +IEnumerator SecuenciaEnsamblaje()
    }
    
    class CentrarBase {
        +float targetX
        +float targetZ
        +CenterOnXZ()
    }
    
    OrquestadorDron "1" --> "1" Brazos : alfa
    OrquestadorDron "1" --> "1" Brazos : beta
    OrquestadorDron "1" --> "1" Ventosa : omega
    OrquestadorDron "1" --> "1" Ventosa : paletizador
    Brazos "1" --> "*" EnsambleGri : interacts via GripperTrigger
    Ventosa "1" --> "*" Ensamble : interacts via SuctionTrigger
    Brazos "1" --> "0..1" CentrarBase : uses
    Produccion "1" --> "*" Spawner : manages
    Spawner ..> EnsambleGri : assigns baseParent
    Spawner ..> Ensamble : assigns puntoEnsamble
```

---

## Implemented Systems

### 1. Gripper System (`Brazos.cs`)

**Challenge**: When using `SetParent`, the object's rotation and position would change unexpectedly.

**Solution**: Preserve offsets in world-space before re-parenting:

```csharp
// Save offsets in world space
Vector3 worldPos = grippedObject.transform.position;
Quaternion worldRot = grippedObject.transform.rotation;

grippedObject.transform.SetParent(gripPoint);

// Restore in world space
grippedObject.transform.position = worldPos;
grippedObject.transform.rotation = worldRot;
```

**Critical bug fixed**: Removed `localRotation = Quaternion.identity` which was causing unexpected flips.

**Configuration**:
- ✅ Local offsets: `grabLocalOffset`, `grabLocalRotOffset`
- ✅ Fixed rotations per prefab in Inspector
- ❌ **Never** use `localRotation = Quaternion.identity` after `SetParent`

**Articulations controlled**:
```csharp
public ArticulationBody Waist;           // X Drive
public ArticulationBody Arm01;           // Z Drive
public ArticulationBody Arm02;           // Z Drive
public ArticulationBody Arm03;           // X Drive
public ArticulationBody GripperAssembly; // Z Drive
public ArticulationBody Gear1;           // X Drive (open/close)
public ArticulationBody Gear2;           // X Drive (mirror of Gear1)
```

---

### 2. Suction Cup System (`Ventosa.cs`)

**Behavior**: Magnetic attraction animation before attachment. Omega is the arm that handles the PCB and Tapa (lid).

**Implementation**:
```csharp
// Suction control fields
public bool suctionActive = false;
public float suctionForce = 10f;
public Vector3 rotacionFijaAlAgarrar = new Vector3(90f, 0f, 0f);
public float alturaLiberacion = 0.02f;
```

**Trigger detection** via `SuctionTrigger.cs`:
```csharp
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Pickable"))
        mainScript.NotifyObjectInside(other.gameObject);
}
```

**Advantages**:
- Clear visual feedback for the user
- Fixed rotation on grab via `rotacionFijaAlAgarrar`
- Smooth transition without teleportation

---

### 3. JSON Motion Sequencer

Each arm's movement is defined in external JSON files under `Assets/JSON_Generados/` and loaded at runtime from `StreamingAssets/`. Each file stores a list of `RobotPose` objects with full joint targets.

**Real pose data structure** (`RobotPose`):
```json
{
  "poses": [
    {
      "waist": 180.0,
      "arm01": 35.0,
      "arm02": 0.0,
      "arm03": 0.0,
      "gripperAssembly": 0.0,
      "gripperClosed": true,
      "gripperOpenAngle": -20.0,
      "gripperClosedAngle": -15.0,
      "delay": 0.0
    }
  ]
}
```

**Available JSON files** (12 total):

| File | Arm | Description |
|------|-----|-------------|
| `Poses_BaseNueva.json` | Alpha | Place drone base (6 poses) |
| `Poses_PCB.json` | Omega | Place PCB with suction |
| `Poses_Motor1.json` | Alpha | Diagonal motor 1 (6 poses) |
| `Poses_Motor2.json` | Alpha | Diagonal motor 2 (6 poses) |
| `Poses_Motor3.json` | Beta | Diagonal motor 3 |
| `Poses_Motor4.json` | Beta | Diagonal motor 4 |
| `Poses_Tapa.json` | Omega | Place lid (final closure) |
| `Poses_Alpha.json` | Alpha | Alpha alternate sequence |
| `Poses_Beta.json` | Beta | Beta alternate sequence |
| `Poses_Omega.json` | Omega | Omega alternate sequence |
| `Poses_Palet.json` | Paletizador | Palletizing / cart movement sequence |
| `poses2_cubo.json` | — | Test/debug sequence |

---

### 4. Multi-Arm Orchestrator (`OrquestadorDron.cs`)

`OrquestadorDron.cs` is the central coordination MonoBehaviour. It reads the master assembly sequence (`ensamblaje_dron.json`) from `StreamingAssets`, triggers each arm by name (`Alpha`, `Beta`, `Omega`, `Paletizador`), and polls `jugandoSecuencia` flags in `Update()` before advancing.

**Real master JSON** (`ensamblaje_dron.json`) — 8 assembly stages + palletizing:
```json
{
  "etapas": [
    { "nombre": "Colocar base",                           "brazos": [{"brazo":"Alpha","archivo":"Poses_BaseNueva.json"}, ...] },
    { "nombre": "Colocar PCB",                            "brazos": [{"brazo":"Omega","archivo":"Poses_PCB.json"}, ...] },
    { "nombre": "Motores diagonales 1 y 3",               "brazos": [{"brazo":"Alpha","archivo":"Poses_Motor1.json"}, {"brazo":"Beta","archivo":"Poses_Motor3.json"}] },
    { "nombre": "Motores diagonales 2 y 4",               "brazos": [{"brazo":"Alpha","archivo":"Poses_Motor2.json"}, {"brazo":"Beta","archivo":"Poses_Motor4.json"}] },
    { "nombre": "Colocar tapa",                           "brazos": [{"brazo":"Omega","archivo":"Poses_Tapa.json"}, ...] },
    { "nombre": "Helices diagonales 1 y 3",               "brazos": [{"brazo":"Alpha","archivo":"Poses_Helice1.json"}, {"brazo":"Beta","archivo":"Poses_Helice3.json"}] },
    { "nombre": "Helices diagonales 2 y 4",               "brazos": [{"brazo":"Alpha","archivo":"Poses_Helice2.json"}, {"brazo":"Beta","archivo":"Poses_Helice4.json"}] },
    { "nombre": "Transferir dron a zona paletizador",     "brazos": [{"brazo":"Omega","archivo":"Poses_TransferDron.json"}, ...] }
  ]
}
```

> **Note**: The Paletizador arm runs its own independent loop after Stage 8, operating in parallel with the next assembly cycle. Its sequence is not part of `ensamblaje_dron.json` — it is driven by a dedicated palletizing controller.

**Orchestration pattern** (poll-based, not coroutine):
```csharp
void Update() {
    bool alfaListo       = !alfaActivo       || !alfa.jugandoSecuencia;
    bool betaListo       = !betaActivo       || !beta.jugandoSecuencia;
    bool omegaListo      = !omegaActivo      || !omega.jugandoSecuencia;
    bool paletizadorListo = !paletizadorActivo || !paletizador.jugandoSecuencia;

    if (alfaListo && betaListo && omegaListo && paletizadorListo) {
        etapaActual++;
        if (etapaActual < maestro.etapas.Count)
            EjecutarEtapa(etapaActual);
        else {
            ensamblajeFinalizado = true;
            Debug.Log("✅ Ensamblaje del dron COMPLETADO.");
        }
    }
}
```

---

### 5. Snap Mechanics

**Two approaches** depending on piece type:

| Method | Script | Trigger | Used For |
|--------|--------|---------|----------|
| **Proximity** | `Ensamble.cs` | `snapPorProximidad` + distance check | PCB, Tapa |
| **Trigger collision** | `EnsambleGri.cs` | `distanciaActivacion` | Motors, Hélices |

**Snap Animation** (Ensamble.cs):
```csharp
Vector3 startPos = piece.transform.position;
Vector3 finalPos = puntoEnsamble.position + 
                   puntoEnsamble.up * offsetHundimiento;

float t = 0f;
while (t < 1f) {
    t += Time.deltaTime * velocidadEncaje;
    piece.transform.position = Vector3.Lerp(startPos, finalPos, t);
    yield return null;
}

piece.transform.SetParent(basePrefab.transform);
piece.GetComponent<Rigidbody>().isKinematic = true;
```

**Final assembly rotation** is configurable per piece:
```csharp
public Vector3 rotacionFinalEnsamble = new Vector3(-90f, 0f, 180f); // Ensamble.cs
public Vector3 rotacionForzada       = new Vector3(-90f, 0f, 0f);   // EnsambleGri.cs
```

---

### 6. Race Condition Prevention

**Problem**: `PlaySequence()` and `ReleaseInSequence()` ran in parallel.

**Solution: Boolean Semaphore** (in `Brazos.cs`):
```csharp
private bool liberandoObjeto = false;

IEnumerator LiberarEnSecuencia() {
    liberandoObjeto = true;
    yield return new WaitForSeconds(tiempoPreSoltar);
    // ... release object
    yield return new WaitForSeconds(tiempoPostSoltar);
    liberandoObjeto = false;
}

IEnumerator ReproducirSecuencia() {
    if (liberandoObjeto) {
        yield return new WaitUntil(() => !liberandoObjeto);
    }
    // ... execute pose
}
```

---

### 7. Production Spawner (`Produccion.cs`)

Parts are not pre-placed in the scene — they are instantiated at runtime by `Produccion.cs` using individual `Spawner` components, with staggered 2-second delays.

```csharp
IEnumerator SecuenciaEnsamblaje() {
    spawnBase.Spawn();
    yield return new WaitForSeconds(2);
    spawnPCB.Spawn();
    yield return new WaitForSeconds(2);
    spawnMotor1.Spawn(); spawnMotor2.Spawn();
    yield return new WaitForSeconds(2);
    spawnMotor3.Spawn(); spawnMotor4.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice1.Spawn(); spawnHelice2.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice3.Spawn(); spawnHelice4.Spawn();
    yield return new WaitForSeconds(2);
    spawnTapa.Spawn();
}
```

Each `Spawner` also auto-assigns `puntoEnsamble` (for `Ensamble`) and `baseParent` (for `EnsambleGri`) on the instantiated prefab.

---

### 8. Palletizer System (`Paletizador` — `Ventosa.cs`)

The Paletizador is a fourth suction-cup arm that operates independently from the assembly sequence. Once Omega transfers the completed drone to the staging zone (Stage 8), the Paletizador picks it up and places it into a box on the active cart. This cycle is designed to complete within the same time budget as one full assembly cycle (< 1 minute), enabling seamless parallelism.

**Dual-cart rotation logic**:
```
Cart 1 active → Paletizador fills boxes
Cart 1 full   → Cart 1 exits zone
              → Paletizador switches to Cart 2
Cart 1 returns with empty boxes
              → Paletizador finishes Cart 2 → switches back to Cart 1
              → Cycle repeats indefinitely
```

**Timing constraint**:
- Assembly cycle: **≤ 1 minute**
- Palletizing one cart: **≤ assembly cycle time**
- This ensures Cart 1 is always ready again before Cart 2 is full, preventing downtime.

**Key behaviors**:
- Suction grip on completed drone at staging zone
- Linear travel to active cart position
- Place drone inside open box
- Close box lid
- Advance to next box slot
- On cart full: signal cart exit, switch target cart

---

```
drone-packaging-simulation-unity/
├── Assets/
│   ├── Brazos.cs                    # Gripper arm — ArticulationBody + pose sequencer (Alpha, Beta)
│   ├── Ventosa.cs                   # Suction arm — ArticulationBody + suction logic (Omega, Paletizador)
│   ├── OrquestadorDron.cs           # Master coordinator — reads JSON, polls all 4 arms
│   ├── Ensamble.cs                  # Snap logic for PCB / Tapa (ventosa pieces)
│   ├── EnsambleGri.cs               # Snap logic for Motors / Hélices (gripper pieces)
│   ├── Spawner.cs                   # Instantiates prefabs and assigns assembly refs
│   ├── Produccion.cs                # Staggered coroutine spawn sequencer
│   ├── Angulos.cs                   # Manual joint angle controller (debug/test)
│   ├── CentrarBase.cs               # Centers Base on XZ after placement
│   ├── GripperTrigger.cs            # OnTriggerEnter → Brazos.NotifyObjectInside()
│   ├── SuctionTrigger.cs            # OnTriggerEnter → Ventosa.NotifyObjectInside()
│   ├── MoverCajon.cs                # Moves cart between waypoints (Cart 1 / Cart 2 rotation)
│   ├── Cian.mat                     # Material asset
│   ├── CV_1.renderTexture           # Render texture (camera view 1)
│   ├── CV_5.renderTexture           # Render texture (camera view 5)
│   ├── New Animator Controller.*    # Animator assets
│   ├── StreamingAssets/
│   │   └── ensamblaje_dron.json     # Master JSON — 8 assembly stages
│   ├── JSON_Generados/              # 12+ pose JSON files (Alpha, Beta, Omega, Paletizador…)
│   └── Scenes/
│       └── SampleScene.unity        # Main simulation scene
├── Packages/
│   └── manifest.json               # Unity package dependencies
└── ProjectSettings/                # Unity project configuration
```

---

## Installation

### Prerequisites

- **Unity Hub** 3.x or higher
- **Unity 2021.3.45f1 LTS** (installable from Unity Hub)
- **Git** (to clone the repository)
- **OS**: Windows 10/11, macOS 10.15+, or Ubuntu 20.04+

### Installation Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/jorgefajardom-coder/drone-packaging-simulation-unity.git
   cd drone-packaging-simulation-unity
   ```

2. **Open in Unity Hub**
   - Open Unity Hub
   - Click "Add" → Select the project folder
   - Verify version is **2021.3.45f1 LTS**
   - If not installed, Unity Hub will download it automatically

3. **First Run**
   - Open `Assets/Scenes/SampleScene.unity`
   - Wait for initial script compilation (1-2 min)
   - Press **Play** ▶️

4. **JSON Configuration**
   - Verify paths in `OrquestadorDron` Inspector
   - Master JSON: `Assets/StreamingAssets/ensamblaje_dron.json`
   - Individual pose JSONs: `Assets/JSON_Generados/Poses_*.json`
   - Arm name field: must match exactly `"Alpha"`, `"Beta"`, or `"Omega"`

---

## Resolved Issues

### Issue #1: Rotation Flips on Grip

**Symptoms**:
- Object rotates 180° unexpectedly when `SetParent` is called
- Incorrect orientation after gripping

**Root Cause**:
```csharp
// ❌ INCORRECT
grippedObject.transform.SetParent(gripPoint);
grippedObject.transform.localRotation = Quaternion.identity; // <-- BUG
```

**Solution**:
```csharp
// ✅ CORRECT
Vector3 worldPos = grippedObject.transform.position;
Quaternion worldRot = grippedObject.transform.rotation;

grippedObject.transform.SetParent(gripPoint);

grippedObject.transform.position = worldPos;
grippedObject.transform.rotation = worldRot;
// DO NOT touch localRotation
```

**Lesson**: Preserve **world-space** before and after `SetParent`.

---

### Issue #2: Lid Penetrating Components

**Symptoms**:
- Lid falls through assembled PCB/motors
- Reaches table and "jumps" upward

**Root Cause**:
- Abrupt repositioning with active gravity
- Incorrect collision layers

**Solution**:
```csharp
bool isPieceToFreeze = assembleScript != null && 
                       assembleScript.freezeOnRelease;

if (!isPieceToFreeze) {
    PositionOnBand(); // Only for PCB
}

// For Lid:
// snapByProximity = true
// freezeOnRelease = true
// isKinematic = true BEFORE releasing
```

**Collision Matrix Configuration**:
```
✅ Lid vs WorkTable: Enabled
✅ Lid vs AssemblablePart: Enabled
❌ Lid vs AssemblyPoint: Disabled (trigger only)
```

---

### Issue #3: Sequence Race Condition

**Symptoms**:
- Objects hit during release
- Deviated trajectories
- Non-deterministic behavior

**Root Cause**:
- `ReleaseInSequence()` and `PlaySequence()` ran in parallel
- No synchronization between coroutines

**Solution: Semaphore Flag**
```csharp
private bool releasingObject = false;

IEnumerator ReleaseInSequence() {
    releasingObject = true;
    yield return new WaitForSeconds(preReleaseTime);
    // ... release object
    yield return new WaitForSeconds(postReleaseTime);
    releasingObject = false;
}

IEnumerator PlaySequence() {
    if (releasingObject) {
        yield return new WaitUntil(() => !releasingObject);
    }
    // ... execute pose
}
```

---

### Issue #4: Incorrect Propeller Rotation

**Symptoms**:
- Propellers 2 and 4 visually "upside down"
- Erratic rotations: `(270, 90, 0)`, `(270, 270, 0)`

**Root Cause**:
- Arms gripped from different angles
- Spawner generated inconsistent orientations
- Script forced absolute rotation without considering grip offset

**Solution**:
```csharp
// In EnsambleGri.cs
if (esHelice && forzarRotacionAbsoluta) {
    Quaternion rotacionObjetivo = Quaternion.identity;
    
    switch (numeroHelice) {
        case 1: rotacionObjetivo = Quaternion.Euler(-90, 0, 0); break;
        case 2: rotacionObjetivo = Quaternion.Euler(-90, 90, 0); break;
        case 3: rotacionObjetivo = Quaternion.Euler(-90, 180, 0); break;
        case 4: rotacionObjetivo = Quaternion.Euler(-90, 270, 0); break;
    }
    
    transform.rotation = rotacionObjetivo;
}
```

**Inspector Configuration**:
- `Es Helice`: ✅
- `Forzar Rotacion Absoluta`: ✅
- `Numero Helice`: 1-4 (assigned by spawner)

---

### Issue #5: Movement Stuttering

**Symptoms**:
- Jerky arm movement
- Micro-stops during Lerp
- Inconsistent velocity

**Root Cause**:
```csharp
// ❌ INCORRECT: t not accumulated correctly
Vector3.Lerp(posInicial, posFinal, Time.deltaTime / duracion);
```

**Solution**:
```csharp
// ✅ CORRECT: Accumulate t explicitly
float t = 0f;
while (t < 1f) {
    t += Time.deltaTime / duracion;
    transform.position = Vector3.Lerp(posInicial, posFinal, t);
    yield return null;
}
```

**Rule**: All physics logic should be in `FixedUpdate` for movements with `Rigidbody`.

---

### Issue #6: Inconsistent Heights After Snap

**Symptoms**:
- Components at different heights after snap
- Visual gaps or overlaps

**Root Cause**:
- **Incorrect pivots in exported prefabs**
- Model origin doesn't match actual contact point
- Generic sink offset without considering geometry

**Solution**:
1. **Correction in modeling software** (Blender/Fusion 360):
   - Place pivot at lower contact point
   - Export with "Apply Transform"

2. **Compensation in Unity** (temporary):
   ```csharp
   // In Ensamble.cs - offsets per piece type
   if (gameObject.name.Contains("Motor")) {
       offsetHundimiento = -0.02f;
   } else if (gameObject.name.Contains("PCB")) {
       offsetHundimiento = -0.005f;
   }
   ```

**Status**: ⚠️ Definitive correction pending in CAD prefabs.

---

## Bug Summary Table

| # | Issue | Severity | Status | Solution |
|---|-------|----------|--------|----------|
| 1 | Rotation flips on grip | 🔴 Critical | ✅ Resolved | Preserve world-space |
| 2 | Lid penetrates components | 🔴 Critical | ✅ Resolved | Kinematic + collision layers |
| 3 | Sequence race condition | 🟡 High | ✅ Resolved | Semaphore flag |
| 4 | Propeller rotation | 🟡 High | ✅ Resolved | Absolute rotation by number |
| 5 | Movement stuttering | 🟢 Medium | ✅ Resolved | Correct t accumulation |
| 6 | Inconsistent heights | 🟡 High | ⚠️ Mitigated | Pending: CAD pivot correction |

---

## Robotic Arm Hierarchy

```
BrazoBase (fixed)
└── Waist (Revolute — X Drive)
    └── Arm01 (Revolute — Z Drive)
        └── Arm02 (Revolute — Z Drive)
            └── Arm03 (Revolute — X Drive)
                └── GripperAssembly (Revolute — Z Drive)
                    ├── Gear1 (Prismatic — X Drive, open/close)
                    └── Gear2 (Prismatic — X Drive, mirror)
```

---

## Physics Configuration

### ArticulationBody Setup

```csharp
// Typical revolute joint configuration
ArticulationBody body = GetComponent<ArticulationBody>();
body.jointType = ArticulationJointType.RevoluteJoint;
body.anchorRotation = Quaternion.Euler(0, 90, 0);

ArticulationDrive drive = body.xDrive;
drive.stiffness = 10000f;  // Rigidity
drive.damping = 100f;      // Damping
drive.forceLimit = 1000f;  // Force limit
drive.target = 45f;        // Target position (degrees)
body.xDrive = drive;
```

### Drive Parameters

| Parameter | Purpose |
|-----------|---------|
| **stiffness** | Joint rigidity — higher values produce firmer response |
| **damping** | Oscillation attenuation |
| **forceLimit** | Maximum applicable force |
| **target** | Target position or rotation value |

### Tags

```
"Pickable" — All graspable drone parts (Base, PCB, Motors, Hélices, Tapa)
"PCB"      — PCB-specific tag for specialized handling
```

---

## Authors

**Jorge Andres Fajardo Mora**  
**Laura Vanesa Castro Sierra**

---

## License and Rights

**Copyright © 2025 Jorge Andres Fajardo Mora and Laura Vanesa Castro Sierra. All rights reserved.**

This repository and all its contents — including but not limited to source code, scripts, configuration files, data files, and documentation — are provided for **read-only and reference purposes only**. 

**No permission is granted** to copy, modify, distribute, sublicense, or use any part of this project for commercial or non-commercial purposes without **explicit written authorization** from the authors.

**Unauthorized reproduction or redistribution** of this work, in whole or in part, is **strictly prohibited**.

---
---
---

# Simulación de Empaquetado de Dron — Unity

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2021.3.45f1_LTS-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-10.0-239120?style=for-the-badge&logo=c-sharp)
![ArticulationBody](https://img.shields.io/badge/Física-ArticulationBody-orange?style=for-the-badge)
![Estado](https://img.shields.io/badge/Estado-Activo-success?style=for-the-badge)

**Simulación de Celda de Ensamblaje Robótico**  
Brazos Articulados Coordinados · Movimiento JSON · Física Realista

[English](#-drone-packaging-simulation--unity) | **Español**

</div>

---

## Tabla de Contenidos

- [Descripción General](#descripción-general)
- [Stack Técnico](#stack-técnico)
- [Arquitectura del Sistema](#arquitectura-del-sistema)
- [Sistemas Implementados](#sistemas-implementados)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Instalación](#instalación)
- [Problemas Resueltos](#problemas-resueltos)
- [Autores](#autores)
- [Licencia](#licencia-y-derechos)

---

## Descripción General

Este proyecto es una **simulación basada en Unity** que recrea una celda robótica de ensamblaje y paletizado de drones. Reproduce un proceso de producción automatizado en el que cuatro brazos robóticos articulados colaboran para ensamblar un dron y empaquetarlo en carros de producción, mediante interacciones físicas realistas, secuencias de movimiento coordinadas y mecanismos de agarre diferenciados.

La simulación está orientada a la **validación virtual de procesos** en contextos técnicos y académicos.

### Características Clave

- 🦾 **Cuatro brazos robóticos coordinados** (Alpha, Beta, Omega, Paletizador) con física ArticulationBody
- 🔄 **Orquestación de 8 etapas** con secuencias basadas en JSON
- ⚙️ **Efectores finales duales**: Gripper (`Brazos.cs`) y Ventosa (`Ventosa.cs`)
- 🎯 **Sistema de snap por proximidad** para ensamblaje de componentes
- 📊 **Ejecución asíncrona basada en coroutines** con gestión de dependencias
- 🔧 **Preservación de world-space** para prevenir artefactos de rotación
- 🏭 **Spawner de producción** con instanciación escalonada de piezas
- 📦 **Brazo paletizador** con sistema de rotación de dos carros — ciclo continuo de empaquetado sincronizado con el tiempo de ensamblaje

---

## Stack Técnico

### Unity 2021.3.45f1 LTS

| Criterio | Justificación |
|----------|---------------|
| **LTS (Long-Term Support)** | Estabilidad garantizada hasta 2024, ideal para proyectos de simulación |
| **ArticulationBody maduro** | Introducido en 2020.1, completamente estable en 2021.3 para simulación robótica precisa |
| **Física determinista** | Solver iterations configurables, esencial para robótica |
| **C# 10.0** | Características modernas: records, pattern matching, global usings |
| **Soporte JSON nativo** | `JsonUtility` optimizado para serialización/deserialización de poses |
| **Rendimiento** | DOTS preview disponible para escalabilidad futura |

### Componentes Core de Unity

```csharp
ArticulationBody      // Sistema de articulaciones robóticas (superior a Rigidbody estándar)
ArticulationDrive     // Control de motores (target, stiffness, damping)
ArticulationJointType // Revolute (rotación) y Prismatic (lineal)
Coroutines            // Secuencias asíncronas
JsonUtility           // Serialización de datos (RobotPose / VentosaPose)
Physics.IgnoreCollision // Control dinámico de colisiones
```

### Dependencias de Paquetes (`manifest.json`)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `com.unity.formats.fbx` | 4.1.3 | Exportación de assets para flujos externos |
| `com.unity.textmeshpro` | 3.0.6 | Renderizado de texto UI |
| `com.unity.timeline` | 1.6.5 | Soporte de timeline de animación |
| `com.unity.visualscripting` | 1.9.4 | Soporte de scripting visual |
| `com.unity.collab-proxy` | 2.5.2 | Integración de control de versiones |
| `com.unity.test-framework` | 1.1.33 | Testing unitario |

---

## Arquitectura del Sistema

### Diagrama de Componentes

```mermaid
graph TB
    subgraph "Capa de Orquestación"
        O[OrquestadorDron]
        JSON[ensamblaje_dron.json<br/>8 Etapas]
        P[Produccion.cs<br/>Secuenciador de Spawn]
    end
    
    subgraph "Capa de Brazos Robóticos"
        B1[Alpha<br/>Brazos — Gripper]
        B2[Beta<br/>Brazos — Gripper]
        B3[Omega<br/>Ventosa — Succión]
        B4[Paletizador<br/>Ventosa — Succión]
    end
    
    subgraph "Capa de Sistemas de Agarre"
        G[GripperTrigger]
        V[SuctionTrigger]
        E1[Ensamble.cs]
        E2[EnsambleGri.cs]
    end
    
    subgraph "Componentes del Dron"
        BASE[Base]
        PCB[PCB]
        MOTOR[Motores x4]
        HELICE[Hélices x4]
        TAPA[Tapa]
    end

    subgraph "Sistema de Paletizado"
        CARRO1[Carro 1<br/>cajas]
        CARRO2[Carro 2<br/>cajas]
    end
    
    O -->|Lee JSON| JSON
    O -->|Coordina| B1
    O -->|Coordina| B2
    O -->|Coordina| B3
    O -->|Coordina| B4
    P -->|Instancia piezas| BASE
    P -->|Instancia piezas| PCB
    P -->|Instancia piezas| MOTOR
    P -->|Instancia piezas| HELICE
    P -->|Instancia piezas| TAPA
    
    B1 -.->|Usa| G
    B2 -.->|Usa| G
    B3 -.->|Usa| V
    B4 -.->|Usa| V
    
    G -->|Lógica Snap| E2
    V -->|Lógica Snap| E1
    
    B1 -->|Ensambla| BASE
    B3 -->|Ensambla| PCB
    B1 -->|Ensambla| MOTOR
    B2 -->|Ensambla| MOTOR
    B1 -->|Ensambla| HELICE
    B2 -->|Ensambla| HELICE
    B3 -->|Ensambla| TAPA
    B3 -->|Transfiere dron a zona| B4
    B4 -->|Paletiza en| CARRO1
    B4 -->|Paletiza en| CARRO2
    
    style O fill:#534AB7,stroke:#26215C,color:#fff
    style B1 fill:#1D9E75,stroke:#085041,color:#fff
    style B2 fill:#1D9E75,stroke:#085041,color:#fff
    style B3 fill:#378ADD,stroke:#042C53,color:#fff
    style B4 fill:#B75A34,stroke:#5C2506,color:#fff
```

### Configuración de Brazos

| Brazo | Clase | Efector Final | Rol | Componentes Manejados |
|-------|-------|--------------|-----|----------------------|
| **Alpha** | `Brazos.cs` | Gripper (pinza) | Ensamble — piezas grandes y mecánicas | Base, Motores diagonales x2, Hélices diagonales x2 |
| **Beta** | `Brazos.cs` | Gripper (pinza) | Ensamble — piezas mecánicas (en pareja con Alpha) | Motores diagonales x2, Hélices diagonales x2 |
| **Omega** | `Ventosa.cs` | Ventosa (succión) | Ensamble — piezas delicadas + transferencia de dron | PCB, Tapa, dron completado → zona de paletizado |
| **Paletizador** | `Ventosa.cs` | Ventosa (succión) | Paletizado — recoge dron, llena carros | Drones completados → Cajas de Carro 1 / Carro 2 |

### Flujo de Secuencia de Ensamblaje

```mermaid
sequenceDiagram
    participant O as OrquestadorDron
    participant A as Alpha (Brazos)
    participant B as Beta (Brazos)
    participant W as Omega (Ventosa)
    participant P as Paletizador (Ventosa)
    participant D as Dron Ensamblado
    participant C as Carro 1 / Carro 2

    Note over O: Inicio — Carga ensamblaje_dron.json

    O->>A: Etapa 1 — Colocar Base
    activate A
    A->>D: Agarre y suelta Base en punto de ensamble
    A-->>O: jugandoSecuencia = false
    deactivate A

    O->>W: Etapa 2 — Colocar PCB
    activate W
    W->>D: Agarre por ventosa → ensambla PCB sobre Base
    W-->>O: jugandoSecuencia = false
    deactivate W

    O->>A: Etapa 3 — Motores diagonales (1 y 3)
    O->>B: Etapa 3 — Motores diagonales (1 y 3)
    activate A
    activate B
    A->>D: Ensamblar Motor (par diagonal)
    B->>D: Ensamblar Motor (par diagonal)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>A: Etapa 4 — Motores diagonales (2 y 4)
    O->>B: Etapa 4 — Motores diagonales (2 y 4)
    activate A
    activate B
    A->>D: Ensamblar Motor (par diagonal)
    B->>D: Ensamblar Motor (par diagonal)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>W: Etapa 5 — Colocar Tapa
    activate W
    W->>D: Agarre por ventosa → ensambla Tapa (cierre final)
    W-->>O: jugandoSecuencia = false
    deactivate W

    O->>A: Etapa 6 — Hélices diagonales (1 y 3)
    O->>B: Etapa 6 — Hélices diagonales (1 y 3)
    activate A
    activate B
    A->>D: Ensamblar Hélice (par diagonal)
    B->>D: Ensamblar Hélice (par diagonal)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>A: Etapa 7 — Hélices diagonales (2 y 4)
    O->>B: Etapa 7 — Hélices diagonales (2 y 4)
    activate A
    activate B
    A->>D: Ensamblar Hélice (par diagonal)
    B->>D: Ensamblar Hélice (par diagonal)
    A-->>O: jugandoSecuencia = false
    B-->>O: jugandoSecuencia = false
    deactivate A
    deactivate B

    O->>W: Etapa 8 — Transferir dron a zona paletizador
    activate W
    W->>W: Agarre por ventosa del dron completo
    W->>D: Ubica dron en zona de paletizado
    W-->>O: jugandoSecuencia = false
    deactivate W

    Note over O,D: Ensamblaje completo (< 1 min de ciclo) — Paletizador toma el control

    loop Hasta llenar el carro activo
        P->>D: Recoge dron de zona de paletizado
        P->>C: Se desplaza al carro activo
        P->>C: Ubica dron dentro de caja y cierra caja
    end

    Note over P,C: Carro 1 lleno → se retira de zona
    Note over P,C: Paletizador cambia a Carro 2
    Note over P,C: Carro 1 regresa con cajas vacías mientras se llena Carro 2
    Note over P,C: Ciclo se repite continuamente
```

### Diagrama de Interacción de Scripts

```mermaid
classDiagram
    class OrquestadorDron {
        +Brazos alfa
        +Brazos beta
        +Ventosa omega
        +Ventosa paletizador
        +string archivoMaestro
        +int etapaActual
        +bool ensamblajeFinalizado
        +CargarMaestro()
        +EjecutarEtapa(int index)
    }
    
    class Brazos {
        +ArticulationBody Waist
        +ArticulationBody Arm01
        +ArticulationBody Arm02
        +ArticulationBody Arm03
        +ArticulationBody GripperAssembly
        +ArticulationBody Gear1
        +ArticulationBody Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +string saveFileName
        +IniciarSecuencia()
        +LoadFromFile()
        +SaveToFile()
        +AgarrarObjeto()
        +LiberarObjeto()
    }
    
    class Ventosa {
        +ArticulationBody Waist
        +ArticulationBody Arm01
        +ArticulationBody Arm02
        +ArticulationBody Arm03
        +bool suctionActive
        +float suctionForce
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +LoadFromFile()
        +NotifyObjectInside()
    }
    
    class Ensamble {
        +Transform puntoEnsamble
        +float offsetHundimiento
        +float velocidadEncaje
        +bool snapPorProximidad
        +float distanciaActivacionSnap
        +bool congelarAlLiberar
        +Vector3 rotacionFinalEnsamble
        +NotificarLiberad()
    }
    
    class EnsambleGri {
        +Transform puntoEnsamble
        +float distanciaActivacion
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +Vector3 rotacionForzada
        +bool usarRotacionPorNumero
        +Transform baseParent
        +ConfigurarRotacionPorNumero()
    }
    
    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Spawn()
    }
    
    class Produccion {
        +Spawner spawnBase
        +Spawner spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +IEnumerator SecuenciaEnsamblaje()
    }
    
    class CentrarBase {
        +float targetX
        +float targetZ
        +CentrarEnXZ()
    }
    
    OrquestadorDron "1" --> "1" Brazos : alfa
    OrquestadorDron "1" --> "1" Brazos : beta
    OrquestadorDron "1" --> "1" Ventosa : omega
    OrquestadorDron "1" --> "1" Ventosa : paletizador
    Brazos "1" --> "*" EnsambleGri : interactúa via GripperTrigger
    Ventosa "1" --> "*" Ensamble : interactúa via SuctionTrigger
    Brazos "1" --> "0..1" CentrarBase : usa
    Produccion "1" --> "*" Spawner : gestiona
    Spawner ..> EnsambleGri : asigna baseParent
    Spawner ..> Ensamble : asigna puntoEnsamble
```

---

## Sistemas Implementados

### 1. Sistema de Gripper (`Brazos.cs`)

**Desafío**: Al usar `SetParent`, la rotación y posición del objeto cambiaban inesperadamente.

**Solución**: Preservar offsets en world-space antes del re-parenteo:

```csharp
// Guardar offsets en espacio global
Vector3 worldPos = objetoAgarrado.transform.position;
Quaternion worldRot = objetoAgarrado.transform.rotation;

objetoAgarrado.transform.SetParent(puntoAgarre);

// Restaurar en espacio global
objetoAgarrado.transform.position = worldPos;
objetoAgarrado.transform.rotation = worldRot;
```

**Bug crítico corregido**: Eliminado `localRotation = Quaternion.identity` que causaba flips inesperados.

**Configuración**:
- ✅ Offsets locales: `grabLocalOffset`, `grabLocalRotOffset`
- ✅ Rotaciones fijas por prefab en Inspector
- ❌ **Nunca** usar `localRotation = Quaternion.identity` después de `SetParent`

**Articulaciones controladas**:
```csharp
public ArticulationBody Waist;           // X Drive
public ArticulationBody Arm01;           // Z Drive
public ArticulationBody Arm02;           // Z Drive
public ArticulationBody Arm03;           // X Drive
public ArticulationBody GripperAssembly; // Z Drive
public ArticulationBody Gear1;           // X Drive (apertura/cierre)
public ArticulationBody Gear2;           // X Drive (espejo de Gear1)
```

---

### 2. Sistema de Ventosa (`Ventosa.cs`)

**Comportamiento**: Animación visual de atracción magnética antes de la fijación. Omega maneja el PCB, la Tapa y la transferencia del dron completo. El Paletizador usa la misma clase para recoger el dron y desplazarlo hasta los carros.

**Implementación**:
```csharp
// Campos de control de succión
public bool suctionActive = false;
public float suctionForce = 10f;
public Vector3 rotacionFijaAlAgarrar = new Vector3(90f, 0f, 0f);
public float alturaLiberacion = 0.02f;
```

**Detección por trigger** vía `SuctionTrigger.cs`:
```csharp
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Pickable"))
        mainScript.NotifyObjectInside(other.gameObject);
}
```

**Ventajas**:
- Feedback visual claro para el usuario
- Rotación fija al agarrar configurable (`rotacionFijaAlAgarrar`)
- Transición suave sin teletransporte

---

### 3. Secuenciador JSON

Los movimientos de cada brazo se definen en archivos JSON externos en `Assets/JSON_Generados/` y se cargan en tiempo de ejecución desde `StreamingAssets/`. Cada archivo almacena una lista de objetos `RobotPose` con todos los targets de articulación.

**Estructura de datos real de pose** (`RobotPose`):
```json
{
  "poses": [
    {
      "waist": 180.0,
      "arm01": 35.0,
      "arm02": 0.0,
      "arm03": 0.0,
      "gripperAssembly": 0.0,
      "gripperClosed": true,
      "gripperOpenAngle": -20.0,
      "gripperClosedAngle": -15.0,
      "delay": 0.0
    }
  ]
}
```

**Archivos JSON disponibles** (12 en total):

| Archivo | Brazo | Descripción |
|---------|-------|-------------|
| `Poses_BaseNueva.json` | Alpha | Colocar base del dron (6 poses) |
| `Poses_PCB.json` | Omega | Colocar PCB con ventosa |
| `Poses_Motor1.json` | Alpha | Motor diagonal 1 (6 poses) |
| `Poses_Motor2.json` | Alpha | Motor diagonal 2 (6 poses) |
| `Poses_Motor3.json` | Beta | Motor diagonal 3 |
| `Poses_Motor4.json` | Beta | Motor diagonal 4 |
| `Poses_Tapa.json` | Omega | Colocar tapa (cierre final) |
| `Poses_Alpha.json` | Alpha | Secuencia alternativa Alpha |
| `Poses_Beta.json` | Beta | Secuencia alternativa Beta |
| `Poses_Omega.json` | Omega | Secuencia alternativa Omega |
| `Poses_Palet.json` | Paletizador | Secuencia de paletizado / movimiento de carro |
| `poses2_cubo.json` | — | Secuencia de prueba/debug |

---

### 4. Orquestador Multi-Brazo (`OrquestadorDron.cs`)

`OrquestadorDron.cs` es el MonoBehaviour central de coordinación. Lee la secuencia de ensamble maestra (`ensamblaje_dron.json`) desde `StreamingAssets`, activa cada brazo por nombre (`Alpha`, `Beta`, `Omega`, `Paletizador`) y sondea los flags `jugandoSecuencia` en `Update()` antes de avanzar.

**JSON maestro real** (`ensamblaje_dron.json`) — 8 etapas de ensamblaje + paletizado:
```json
{
  "etapas": [
    { "nombre": "Colocar base",                       "brazos": [{"brazo":"Alpha","archivo":"Poses_BaseNueva.json"}, ...] },
    { "nombre": "Colocar PCB",                        "brazos": [{"brazo":"Omega","archivo":"Poses_PCB.json"}, ...] },
    { "nombre": "Motores diagonales 1 y 3",           "brazos": [{"brazo":"Alpha","archivo":"Poses_Motor1.json"}, {"brazo":"Beta","archivo":"Poses_Motor3.json"}] },
    { "nombre": "Motores diagonales 2 y 4",           "brazos": [{"brazo":"Alpha","archivo":"Poses_Motor2.json"}, {"brazo":"Beta","archivo":"Poses_Motor4.json"}] },
    { "nombre": "Colocar tapa",                       "brazos": [{"brazo":"Omega","archivo":"Poses_Tapa.json"}, ...] },
    { "nombre": "Helices diagonales 1 y 3",           "brazos": [{"brazo":"Alpha","archivo":"Poses_Helice1.json"}, {"brazo":"Beta","archivo":"Poses_Helice3.json"}] },
    { "nombre": "Helices diagonales 2 y 4",           "brazos": [{"brazo":"Alpha","archivo":"Poses_Helice2.json"}, {"brazo":"Beta","archivo":"Poses_Helice4.json"}] },
    { "nombre": "Transferir dron a zona paletizador", "brazos": [{"brazo":"Omega","archivo":"Poses_TransferDron.json"}, ...] }
  ]
}
```

> **Nota**: El Paletizador ejecuta su propio bucle independiente a partir de la Etapa 8, operando en paralelo con el siguiente ciclo de ensamblaje. Su secuencia no forma parte de `ensamblaje_dron.json` — es gestionada por un controlador de paletizado dedicado.

**Patrón de orquestación** (basado en polling, no en coroutine):
```csharp
void Update() {
    bool alfaListo        = !alfaActivo        || !alfa.jugandoSecuencia;
    bool betaListo        = !betaActivo        || !beta.jugandoSecuencia;
    bool omegaListo       = !omegaActivo       || !omega.jugandoSecuencia;
    bool paletizadorListo = !paletizadorActivo || !paletizador.jugandoSecuencia;

    if (alfaListo && betaListo && omegaListo && paletizadorListo) {
        etapaActual++;
        if (etapaActual < maestro.etapas.Count)
            EjecutarEtapa(etapaActual);
        else {
            ensamblajeFinalizado = true;
            Debug.Log("✅ Ensamblaje del dron COMPLETADO.");
        }
    }
}
```

---

### 5. Mecánicas de Snap

**Dos enfoques** según el tipo de pieza:

| Método | Script | Trigger | Usado para |
|--------|--------|---------|-----------|
| **Proximidad** | `Ensamble.cs` | `snapPorProximidad` + verificación de distancia | PCB, Tapa |
| **Colisión Trigger** | `EnsambleGri.cs` | `distanciaActivacion` | Motores, Hélices |

**Animación de Snap** (Ensamble.cs):
```csharp
Vector3 posInicial = pieza.transform.position;
Vector3 posFinal = puntoEnsamble.position + 
                   puntoEnsamble.up * offsetHundimiento;

float t = 0f;
while (t < 1f) {
    t += Time.deltaTime * velocidadEncaje;
    pieza.transform.position = Vector3.Lerp(posInicial, posFinal, t);
    yield return null;
}

pieza.transform.SetParent(basePrefab.transform);
pieza.GetComponent<Rigidbody>().isKinematic = true;
```

**La rotación final del ensamble** es configurable por pieza:
```csharp
public Vector3 rotacionFinalEnsamble = new Vector3(-90f, 0f, 180f); // Ensamble.cs
public Vector3 rotacionForzada       = new Vector3(-90f, 0f, 0f);   // EnsambleGri.cs
```

---

### 6. Prevención de Race Conditions

**Problema**: `ReproducirSecuencia()` y `LiberarEnSecuencia()` corrían en paralelo.

**Solución: Semáforo Booleano** (en `Brazos.cs`):
```csharp
private bool liberandoObjeto = false;

IEnumerator LiberarEnSecuencia() {
    liberandoObjeto = true;
    yield return new WaitForSeconds(tiempoPreSoltar);
    // ... soltar objeto
    yield return new WaitForSeconds(tiempoPostSoltar);
    liberandoObjeto = false;
}

IEnumerator ReproducirSecuencia() {
    if (liberandoObjeto) {
        yield return new WaitUntil(() => !liberandoObjeto);
    }
    // ... ejecutar pose
}
```

---

### 7. Spawner de Producción (`Produccion.cs`)

Las piezas no se pre-colocan en la escena — se instancian en tiempo de ejecución por `Produccion.cs` usando componentes `Spawner` individuales, con retrasos escalonados de 2 segundos.

```csharp
IEnumerator SecuenciaEnsamblaje() {
    spawnBase.Spawn();
    yield return new WaitForSeconds(2);
    spawnPCB.Spawn();
    yield return new WaitForSeconds(2);
    spawnMotor1.Spawn(); spawnMotor2.Spawn();
    yield return new WaitForSeconds(2);
    spawnMotor3.Spawn(); spawnMotor4.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice1.Spawn(); spawnHelice2.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice3.Spawn(); spawnHelice4.Spawn();
    yield return new WaitForSeconds(2);
    spawnTapa.Spawn();
}
```

Cada `Spawner` también asigna automáticamente `puntoEnsamble` (para `Ensamble`) y `baseParent` (para `EnsambleGri`) en el prefab instanciado.

---

### 8. Sistema de Paletizado (`Paletizador` — `Ventosa.cs`)

El Paletizador es un cuarto brazo de ventosa que opera de forma independiente de la secuencia de ensamblaje. Una vez que Omega transfiere el dron completado a la zona de paletizado (Etapa 8), el Paletizador lo recoge y lo ubica dentro de una caja en el carro activo. Este ciclo está diseñado para completarse dentro del mismo tiempo que un ciclo de ensamblaje completo (< 1 minuto), permitiendo un paralelismo continuo sin paradas.

**Lógica de rotación de dos carros**:
```
Carro 1 activo  → Paletizador llena cajas
Carro 1 lleno   → Carro 1 se retira de la zona
                → Paletizador cambia a Carro 2
Carro 1 regresa con cajas vacías
                → Paletizador termina Carro 2 → cambia de vuelta a Carro 1
                → Ciclo se repite indefinidamente
```

**Restricción de tiempo**:
- Ciclo de ensamblaje: **≤ 1 minuto**
- Paletizado de un carro: **≤ tiempo de ciclo de ensamblaje**
- Esto garantiza que el Carro 1 esté siempre listo antes de que el Carro 2 se llene, evitando tiempos muertos.

**Comportamientos clave**:
- Agarre por ventosa del dron completado en la zona de paletizado
- Desplazamiento lineal hasta la posición del carro activo
- Ubicación del dron dentro de la caja abierta
- Cierre de la caja
- Avance al siguiente slot de caja
- Al completar el carro: señal de salida del carro, cambio de carro objetivo

---

## Estructura del Proyecto

```
drone-packaging-simulation-unity/
├── Assets/
│   ├── Brazos.cs                    # Brazo gripper — ArticulationBody + secuenciador de poses (Alpha, Beta)
│   ├── Ventosa.cs                   # Brazo ventosa — ArticulationBody + lógica de succión (Omega, Paletizador)
│   ├── OrquestadorDron.cs           # Coordinador maestro — lee JSON, sondea los 4 brazos
│   ├── Ensamble.cs                  # Lógica snap para PCB / Tapa (piezas de ventosa)
│   ├── EnsambleGri.cs               # Lógica snap para Motores / Hélices (piezas de gripper)
│   ├── Spawner.cs                   # Instancia prefabs y asigna refs de ensamble
│   ├── Produccion.cs                # Secuenciador de spawn con coroutine escalonado
│   ├── Angulos.cs                   # Controlador manual de ángulos de articulaciones
│   ├── CentrarBase.cs               # Centra la Base en XZ después de la colocación
│   ├── GripperTrigger.cs            # OnTriggerEnter → Brazos.NotifyObjectInside()
│   ├── SuctionTrigger.cs            # OnTriggerEnter → Ventosa.NotifyObjectInside()
│   ├── MoverCajon.cs                # Mueve el carro entre waypoints (rotación Carro 1 / Carro 2)
│   ├── Cian.mat                     # Material asset
│   ├── CV_1.renderTexture           # Textura de renderizado (vista de cámara 1)
│   ├── CV_5.renderTexture           # Textura de renderizado (vista de cámara 5)
│   ├── New Animator Controller.*    # Assets de animador
│   ├── StreamingAssets/
│   │   └── ensamblaje_dron.json     # JSON maestro — 8 etapas de ensamblaje
│   ├── JSON_Generados/              # 12+ archivos JSON de poses (Alpha, Beta, Omega, Paletizador…)
│   └── Scenes/
│       └── SampleScene.unity        # Escena principal de simulación
├── Packages/
│   └── manifest.json               # Dependencias de paquetes Unity
└── ProjectSettings/                # Configuración del proyecto Unity
```

---

## Instalación

### Requisitos Previos

- **Unity Hub** 3.x o superior
- **Unity 2021.3.45f1 LTS** (instalable desde Unity Hub)
- **Git** (para clonar el repositorio)
- **SO**: Windows 10/11, macOS 10.15+, o Ubuntu 20.04+

### Pasos de Instalación

1. **Clonar el repositorio**
   ```bash
   git clone https://github.com/jorgefajardom-coder/drone-packaging-simulation-unity.git
   cd drone-packaging-simulation-unity
   ```

2. **Abrir en Unity Hub**
   - Abrir Unity Hub
   - Click en "Add" → Seleccionar la carpeta del proyecto
   - Verificar que la versión sea **2021.3.45f1 LTS**
   - Si no está instalada, Unity Hub la descargará automáticamente

3. **Primera Ejecución**
   - Abrir `Assets/Scenes/SampleScene.unity`
   - Esperar compilación inicial de scripts (1-2 min)
   - Presionar **Play** ▶️

4. **Configuración de JSON**
   - Verificar rutas en el Inspector de `OrquestadorDron`
   - JSON maestro: `Assets/StreamingAssets/ensamblaje_dron.json`
   - JSONs individuales de poses: `Assets/JSON_Generados/Poses_*.json`
   - El campo de nombre del brazo debe coincidir exactamente: `"Alpha"`, `"Beta"` o `"Omega"`

---

## Problemas Resueltos

### Problema #1: Flips de Rotación al Agarrar

**Síntomas**:
- Objeto rota 180° inesperadamente al hacer `SetParent`
- Orientación incorrecta después del agarre

**Causa Raíz**:
```csharp
// ❌ INCORRECTO
objetoAgarrado.transform.SetParent(puntoAgarre);
objetoAgarrado.transform.localRotation = Quaternion.identity; // <-- BUG
```

**Solución**:
```csharp
// ✅ CORRECTO
Vector3 worldPos = objetoAgarrado.transform.position;
Quaternion worldRot = objetoAgarrado.transform.rotation;

objetoAgarrado.transform.SetParent(puntoAgarre);

objetoAgarrado.transform.position = worldPos;
objetoAgarrado.transform.rotation = worldRot;
// NO tocar localRotation
```

**Lección**: Preservar **world-space** antes y después de `SetParent`.

---

### Problema #2: Tapa Atraviesa Componentes

**Síntomas**:
- La tapa cae a través de PCB/motores ya ensamblados
- Llega a la mesa y "salta" hacia arriba

**Causa Raíz**:
- Reposicionamiento brusco con gravedad activa
- Collision layers incorrectos

**Solución**:
```csharp
bool esPiezaQueSeCongela = ensambleScript != null && 
                           ensambleScript.congelarAlLiberar;

if (!esPiezaQueSeCongela) {
    PosicionarSobreBanda(); // Solo para PCB
}

// Para Tapa:
// snapPorProximidad = true
// congelarAlLiberar = true
// isKinematic = true ANTES de soltar
```

**Configuración Collision Matrix**:
```
✅ Tapa vs MesaTrabajo: Habilitado
✅ Tapa vs ParteEnsamblable: Habilitado
❌ Tapa vs PuntoEnsamble: Deshabilitado (solo trigger)
```

---

### Problema #3: Race Condition en Secuencias

**Síntomas**:
- Objetos golpeados durante liberación
- Trayectorias desviadas
- Comportamiento no determinista

**Causa Raíz**:
- `LiberarEnSecuencia()` y `ReproducirSecuencia()` corrían en paralelo
- Sin sincronización entre coroutines

**Solución: Flag Semáforo**
```csharp
private bool liberandoObjeto = false;

IEnumerator LiberarEnSecuencia() {
    liberandoObjeto = true;
    yield return new WaitForSeconds(tiempoPreSoltar);
    // ... soltar objeto
    yield return new WaitForSeconds(tiempoPostSoltar);
    liberandoObjeto = false;
}

IEnumerator ReproducirSecuencia() {
    if (liberandoObjeto) {
        yield return new WaitUntil(() => !liberandoObjeto);
    }
    // ... ejecutar pose
}
```

---

### Problema #4: Rotación Incorrecta de Hélices

**Síntomas**:
- Hélices 2 y 4 visualmente "al revés"
- Rotaciones erráticas: `(270, 90, 0)`, `(270, 270, 0)`

**Causa Raíz**:
- Brazos agarraban desde diferentes ángulos
- Spawner generaba orientaciones inconsistentes
- Script forzaba rotación absoluta sin considerar offset de agarre

**Solución**:
```csharp
// En EnsambleGri.cs
if (esHelice && forzarRotacionAbsoluta) {
    Quaternion rotacionObjetivo = Quaternion.identity;
    
    switch (numeroHelice) {
        case 1: rotacionObjetivo = Quaternion.Euler(-90, 0, 0); break;
        case 2: rotacionObjetivo = Quaternion.Euler(-90, 90, 0); break;
        case 3: rotacionObjetivo = Quaternion.Euler(-90, 180, 0); break;
        case 4: rotacionObjetivo = Quaternion.Euler(-90, 270, 0); break;
    }
    
    transform.rotation = rotacionObjetivo;
}
```

**Configuración Inspector**:
- `Es Helice`: ✅
- `Forzar Rotacion Absoluta`: ✅
- `Numero Helice`: 1-4 (asignado por spawner)

---

### Problema #5: Stuttering en Movimiento

**Síntomas**:
- Movimiento entrecortado de brazos
- Micro-paradas durante Lerp
- Inconsistencia de velocidad

**Causa Raíz**:
```csharp
// ❌ INCORRECTO: t no se acumula correctamente
Vector3.Lerp(posInicial, posFinal, Time.deltaTime / duracion);
```

**Solución**:
```csharp
// ✅ CORRECTO: Acumular t explícitamente
float t = 0f;
while (t < 1f) {
    t += Time.deltaTime / duracion;
    transform.position = Vector3.Lerp(posInicial, posFinal, t);
    yield return null;
}
```

**Regla**: Toda lógica física debe estar en `FixedUpdate` para movimientos con `Rigidbody`.

---

### Problema #6: Alturas Inconsistentes Post-Snap

**Síntomas**:
- Componentes a diferentes alturas después del snap
- Gaps o superposiciones visuales

**Causa Raíz**:
- **Pivots incorrectos en prefabs exportados**
- Origen del modelo no coincide con punto de contacto real
- Offset de hundimiento genérico sin considerar geometría

**Solución**:
1. **Corrección en software de modelado** (Blender/Fusion 360):
   - Ubicar pivot en el punto de contacto inferior
   - Exportar con "Apply Transform"

2. **Compensación en Unity** (temporal):
   ```csharp
   // En Ensamble.cs - offsets por tipo de pieza
   if (gameObject.name.Contains("Motor")) {
       offsetHundimiento = -0.02f;
   } else if (gameObject.name.Contains("PCB")) {
       offsetHundimiento = -0.005f;
   }
   ```

**Estado**: ⚠️ Corrección definitiva pendiente en prefabs CAD.

---

## Tabla Resumen de Bugs

| # | Bug | Severidad | Estado | Solución |
|---|-----|-----------|--------|----------|
| 1 | Flips de rotación al agarrar | 🔴 Crítico | ✅ Resuelto | Preservar world-space |
| 2 | Tapa atraviesa componentes | 🔴 Crítico | ✅ Resuelto | Kinematic + collision layers |
| 3 | Race condition secuencias | 🟡 Alto | ✅ Resuelto | Flag semáforo |
| 4 | Rotación hélices | 🟡 Alto | ✅ Resuelto | Rotación absoluta por número |
| 5 | Stuttering movimiento | 🟢 Medio | ✅ Resuelto | Acumulación correcta de t |
| 6 | Alturas inconsistentes | 🟡 Alto | ⚠️ Mitigado | Pending: corrección pivots CAD |

---

## Jerarquía del Brazo Robótico

```
BrazoBase (fixed)
└── Waist (Revolute — X Drive)
    └── Arm01 (Revolute — Z Drive)
        └── Arm02 (Revolute — Z Drive)
            └── Arm03 (Revolute — X Drive)
                └── GripperAssembly (Revolute — Z Drive)
                    ├── Gear1 (Prismatic — X Drive, apertura/cierre)
                    └── Gear2 (Prismatic — X Drive, espejo)
```

---

## Configuración de Física

### Configuración de ArticulationBody

```csharp
// Configuración típica de articulación revolute
ArticulationBody body = GetComponent<ArticulationBody>();
body.jointType = ArticulationJointType.RevoluteJoint;
body.anchorRotation = Quaternion.Euler(0, 90, 0);

ArticulationDrive drive = body.xDrive;
drive.stiffness = 10000f;  // Rigidez
drive.damping = 100f;      // Amortiguación
drive.forceLimit = 1000f;  // Límite de fuerza
drive.target = 45f;        // Posición objetivo (grados)
body.xDrive = drive;
```

### Parámetros de Drive

| Parámetro | Función |
|-----------|---------|
| **stiffness** | Rigidez de la articulación — valores altos producen respuesta más firme |
| **damping** | Amortiguación de oscilaciones |
| **forceLimit** | Fuerza máxima aplicable |
| **target** | Valor objetivo de posición o rotación |

### Tags del Proyecto

```
"Pickable" — Todas las piezas agarrables (Base, PCB, Motores, Hélices, Tapa)
"PCB"      — Tag específico del PCB para manejo especializado
```

---

## Autores

**Jorge Andres Fajardo Mora**  
**Laura Vanesa Castro Sierra**

---

## Licencia y Derechos

**Copyright © 2025 Jorge Andres Fajardo Mora y Laura Vanesa Castro Sierra. Todos los derechos reservados.**

Este repositorio y la totalidad de su contenido — incluyendo, entre otros, código fuente, scripts, archivos de configuración, archivos de datos y documentación — se proporcionan exclusivamente para **fines de lectura y referencia**. 

**No se otorga ningún permiso** para copiar, modificar, distribuir, sublicenciar ni utilizar ninguna parte de este proyecto con fines comerciales o no comerciales sin **autorización escrita explícita** de los autores.

Queda **estrictamente prohibida** la reproducción o redistribución no autorizada de este trabajo, en todo o en parte.
