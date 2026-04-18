# Drone Packaging Simulation — Unity

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2021.3.45f1_LTS-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-10.0-239120?style=for-the-badge&logo=c-sharp)
![ArticulationBody](https://img.shields.io/badge/Physics-ArticulationBody-orange?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active-success?style=for-the-badge)

**Robotic Assembly Cell Simulation**  
Coordinated Articulated Arms · JSON-Driven Motion · Realistic Physics

**English** | [Español](#simulación-de-empaquetado-de-dron--unity)

<br/>

![Simulation Overview](docs/simulation_overview.png)
> *Isometric view of the robotic assembly cell — 4 articulated arms (Alpha, Beta, Omega) + Palletizer with mecanum wheels. Unity 2021.3.45f1 LTS.*

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

This project is a **Unity-based simulation** of a robotic drone assembly and palletizing cell. Four robotic arms collaborate to assemble a drone through physically realistic interactions, JSON-driven motion sequences, and differentiated gripping mechanisms. The completed drone is then transported and palletized into production carts by a fourth arm that moves autonomously on **mecanum wheels**.

The simulation is intended for **virtual process validation** in technical and academic contexts.

### Key Features

- 🦾 **Four coordinated robotic arms** (Alpha, Beta, Omega, Paletizador) with ArticulationBody physics
- 🔄 **JSON-driven motion** — each arm reads its own pose file; no central orchestrator
- ⚙️ **Dual end effectors**: Gripper (`Brazos.cs`) and Suction Cup (`Ventosa.cs`)
- 🎯 **Proximity-based snap system** for component assembly
- 📊 **Coroutine-based asynchronous execution** with dependency management
- 🔧 **World-space preservation** to prevent rotation artifacts
- 🏭 **Production spawner** with staggered coroutine-based part instantiation
- 📦 **Paletizador** — mecanum-wheel arm + `CarroPaletizador.cs` navigation system
- 🚁 **DronListo.cs** — unifies all drone parts into a single rigidbody unit before pickup

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
graph LR
    subgraph ORCH["Orchestration"]
        direction TB
        O[OrquestadorDron]
        JSON[(ensamblaje_dron.json<br/>8 stages)]
        PROD[Produccion.cs]
        O -- reads --> JSON
    end

    subgraph ASSEMBLY["Assembly Cell"]
        direction TB
        subgraph GRIPPERS["Gripper Arms"]
            B1[Alpha · Brazos]
            B2[Beta · Brazos]
        end
        subgraph OMEGA["Suction Arm"]
            B3[Omega · Ventosa]
        end
        subgraph PARTS["Drone Parts"]
            BASE[Base] --- PCB[PCB]
            MOTOR[Motors ×4] --- HELICE[Hélices ×4]
            TAPA[Tapa]
        end
        B1 & B2 -->|grip + snap| MOTOR & HELICE
        B1 -->|grip + snap| BASE
        B3 -->|suction + snap| PCB & TAPA
    end

    subgraph PALLET["Palletizing"]
        direction TB
        B4[Paletizador · Ventosa<br/>mecanum wheels]
        CARRO1[Cart 1]
        CARRO2[Cart 2]
        B4 -->|fills| CARRO1
        B4 -.->|rotates to| CARRO2
    end

    PROD -->|spawns| PARTS
    O -->|triggers| B1 & B2 & B3 & B4
    B3 -->|transfers drone| B4

    style O fill:#534AB7,stroke:#26215C,color:#fff
    style B1 fill:#1D9E75,stroke:#085041,color:#fff
    style B2 fill:#1D9E75,stroke:#085041,color:#fff
    style B3 fill:#378ADD,stroke:#042C53,color:#fff
    style B4 fill:#B75A34,stroke:#5C2506,color:#fff
```

### Arm Configuration

| Arm | Class | End Effector | Status | Components Handled |
|-----|-------|-------------|--------|-------------------|
| **Alpha** | `Brazos.cs` | Gripper (pinza) | ✅ Implemented | Base, diagonal Motors x2 |
| **Beta** | `Brazos.cs` | Gripper (pinza) | ✅ Implemented | diagonal Motors x2 |
| **Omega** | `Ventosa.cs` | Suction Cup (ventosa) | ✅ Implemented | PCB, Tapa *(poses exist)* |
| **Paletizador** | `Ventosa.cs` + mecanum wheels | Suction Cup (ventosa) | 🔧 In progress | Completed drones → Cart 1 / Cart 2 |

### Assembly Sequence Flow

```mermaid
sequenceDiagram
    participant O as Orchestrator
    participant A as Alpha
    participant B as Beta
    participant W as Omega
    participant P as Paletizador
    participant D as Drone

    rect rgb(30, 80, 60)
        Note over O,D: Assembly Phase (< 1 min)
        O->>A: Stage 1 — Place Base
        A->>D: grip → release Base

        O->>W: Stage 2 — Place PCB
        W->>D: suction → snap PCB

        O->>+A: Stage 3 — Motors 1 & 3
        O->>+B: Stage 3 — Motors 1 & 3
        A->>D: snap Motor
        B->>D: snap Motor
        deactivate A
        deactivate B

        O->>+A: Stage 4 — Motors 2 & 4
        O->>+B: Stage 4 — Motors 2 & 4
        A->>D: snap Motor
        B->>D: snap Motor
        deactivate A
        deactivate B

        O->>W: Stage 5 — Place Tapa
        W->>D: suction → snap Tapa

        O->>+A: Stage 6 — Hélices 1 & 3
        O->>+B: Stage 6 — Hélices 1 & 3
        A->>D: snap Hélice
        B->>D: snap Hélice
        deactivate A
        deactivate B

        O->>+A: Stage 7 — Hélices 2 & 4
        O->>+B: Stage 7 — Hélices 2 & 4
        A->>D: snap Hélice
        B->>D: snap Hélice
        deactivate A
        deactivate B

        O->>W: Stage 8 — Transfer drone
        W->>P: place drone in staging zone
    end

    rect rgb(120, 60, 20)
        Note over P: Palletizing Phase (parallel loop)
        loop Each drone
            P->>D: pick from staging zone
            P->>P: move to active cart
            P->>P: place in box · close box
        end
        Note over P: Cart full → swap cart → repeat
    end
```

### Script Interaction Diagram

```mermaid
classDiagram
    direction TB

    class OrquestadorDron {
        +Brazos alfa, beta
        +Ventosa omega, paletizador
        +string archivoMaestro
        +int etapaActual
        +CargarMaestro()
        +EjecutarEtapa(int index)
    }

    class Brazos {
        +ArticulationBody Waist..Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +AgarrarObjeto()
        +LiberarObjeto()
        +LoadFromFile()
    }

    class Ventosa {
        +ArticulationBody Waist..Arm03
        +bool suctionActive
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +NotifyObjectInside()
        +LoadFromFile()
    }

    class EnsambleGri {
        +Transform puntoEnsamble
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +Transform baseParent
        +ConfigurarRotacionPorNumero()
    }

    class Ensamble {
        +Transform puntoEnsamble
        +bool snapPorProximidad
        +bool congelarAlLiberar
        +Vector3 rotacionFinalEnsamble
        +NotificarLiberad()
    }

    class Produccion {
        +Spawner spawnBase, spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +IEnumerator SecuenciaEnsamblaje()
    }

    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Spawn()
    }

    class CentrarBase {
        +float targetX, targetZ
        +CenterOnXZ()
    }

    OrquestadorDron --> Brazos : alfa / beta
    OrquestadorDron --> Ventosa : omega / paletizador
    Brazos --> EnsambleGri : snap via GripperTrigger
    Ventosa --> Ensamble : snap via SuctionTrigger
    Brazos --> CentrarBase : centers Base after release
    Produccion --> Spawner : manages
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

### 4. Decentralized Motion Architecture

The central orchestrator (`OrquestadorDron.cs`) was **removed** from the active scene. Motion coordination is now fully decentralized: each arm reads and executes its own JSON pose file independently. The four arms operate in sequence by design of their respective JSON files, without a master coordinator polling their state.

**Motion flow per arm**:
```
Arm's own JSON file (Poses_*.json)
    → LoadFromFile() on Awake
        → IniciarSecuencia() on Start / trigger
            → SmoothX / SmoothZ per frame
                → ArticulationDrive.target updated
```

**Active JSON files and their arms**:

| File | Arm | Poses |
|------|-----|-------|
| `Poses_BaseNueva.json` | Alpha | 6 |
| `Poses_PCB.json` | Omega | 7 |
| `Poses_Motor1.json` | Beta | 6 |
| `Poses_Motor2.json` | Beta | 4 |
| `Poses_Motor3.json` | Beta | 6 |
| `Poses_Motor4.json` | Beta | 4 |
| `Poses_Tapa.json` | Omega | 5 |
| `Poses_Omega.json` | Omega | 18 |
| `Poses_Palet.json` | Paletizador | 3 |
| `Poses_Alpha.json` | Alpha | 29 |
| `Poses_Beta.json` | Beta | 24 |
| `poses2_cubo.json` | — | 1 (debug) |

---

### 5. Drone Unification (`DronListo.cs`)

Before Omega lifts the completed drone, all assembled parts must behave as a single rigid unit. `DronListo.cs` is attached to `BasePrefab` and handles this transition.

```csharp
public void PrepararParaLevantamiento() {
    dronesListo = true;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}

public void SoltarDron() {
    dronesListo = false;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
```

---

### 6. Palletizer Navigation (`CarroPaletizador.cs`)

`CarroPaletizador.cs` manages the Paletizador's floor movement. The Paletizador arm (`Ventosa`) is a **child** of the cart GameObject, so the entire unit — arm + cart — moves together. Navigation moves in XZ only (Y stays fixed) and rotates on the Y axis toward each waypoint.

**Waypoints** (defined as `Transform` references in the Inspector):

| Point | Purpose |
|-------|---------|
| `puntoInicio` | Starting / home position |
| `puntoGiro` | Rotation waypoint — arm orients before travelling to delivery point |
| `punto_1_1` | Cart 1, slot 1 |
| `punto_1_2` | Cart 1, slot 2 |
| `punto_2_1` | Cart 2, slot 1 |
| `punto_2_2` | Cart 2, slot 2 |

**Palletizing sequence** (coroutine):
```
Cart 1:
  punto_1_1 → deposit drone 1 → deposit drone 2
  punto_1_2 → deposit drone 1 → deposit drone 2
Cart 2:
  punto_2_1 → deposit drone 1 → deposit drone 2
  punto_2_2 → deposit drone 1 → deposit drone 2
Return to puntoInicio
```

**Movement logic** (`IrA` coroutine): translates to XZ target → rotates to face destination on Y axis, both with configurable speed and tolerance.

---

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

### 7. Race Condition Prevention

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

### 8. Production Spawner (`Produccion.cs`)

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

## Project Structure

```
drone-packaging-simulation-unity/
├── docs/
│   └── simulation_overview.png      # Isometric view of the robotic assembly cell
├── Assets/
│   ├── Brazos.cs                    # Gripper arm — Alpha, Beta (579 lines)
│   ├── Ventosa.cs                   # Suction arm — Omega, Paletizador (688 lines)
│   ├── CarroPaletizador.cs          # Paletizador floor navigation — mecanum waypoints (163 lines)
│   ├── DronListo.cs                 # Unifies drone parts as single rigidbody before pickup (39 lines)
│   ├── Ensamble.cs                  # Snap logic for PCB / Tapa (144 lines)
│   ├── EnsambleGri.cs               # Snap logic for Motors / Hélices (174 lines)
│   ├── Spawner.cs                   # Instantiates prefabs and assigns assembly refs (32 lines)
│   ├── Produccion.cs                # Staggered coroutine spawn sequencer (56 lines)
│   ├── Angulos.cs                   # Manual joint angle controller (debug/test)
│   ├── CentrarBase.cs               # Centers Base on XZ after placement
│   ├── GripperTrigger.cs            # OnTriggerEnter → Brazos.NotifyObjectInside()
│   ├── SuctionTrigger.cs            # OnTriggerEnter → Ventosa.NotifyObjectInside()
│   ├── MoverCajon.cs                # Moves cart between waypoints
│   ├── OrquestadorDron.cs           # Kept in Assets but removed from scene
│   ├── Cian.mat
│   ├── CV_1.renderTexture
│   ├── CV_5.renderTexture
│   ├── New Animator Controller.*
│   ├── StreamingAssets/
│   │   └── ensamblaje_dron.json     # Master JSON (legacy — not used in current scene)
│   ├── JSON_Generados/              # 12 pose JSON files — each arm reads its own
│   └── Scenes/
│       └── SampleScene.unity
├── Packages/
│   └── manifest.json
└── ProjectSettings/
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
   - Each arm loads its own JSON file automatically from `Assets/JSON_Generados/`
   - The file for each arm is assigned in the Inspector of the `Brazos` or `Ventosa` component via the `saveFileName` field
   - Paletizador waypoints are assigned in the `CarroPaletizador` Inspector

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

[English](#drone-packaging-simulation--unity) | **Español**

<br/>

![Vista general de la simulación](docs/simulation_overview.png)
> *Vista isométrica de la celda robótica de ensamblaje — 4 brazos articulados (Alpha, Beta, Omega) + Paletizador con ruedas mecanum. Unity 2021.3.45f1 LTS.*

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

Este proyecto es una **simulación basada en Unity** de una celda robótica de ensamblaje y paletizado de drones. Cuatro brazos robóticos colaboran para ensamblar un dron mediante interacciones físicas realistas y secuencias de movimiento impulsadas por JSON. El dron completado es luego transportado y paletizado en carros de producción por un cuarto brazo que se desplaza autónomamente con **ruedas mecanum**.

La simulación está orientada a la **validación virtual de procesos** en contextos técnicos y académicos.

### Características Clave

- 🦾 **Cuatro brazos robóticos coordinados** (Alpha, Beta, Omega, Paletizador) con física ArticulationBody
- 🔄 **Movimiento impulsado por JSON** — cada brazo lee su propio archivo de poses; sin orquestador central
- ⚙️ **Efectores finales duales**: Gripper (`Brazos.cs`) y Ventosa (`Ventosa.cs`)
- 🎯 **Sistema de snap por proximidad** para ensamblaje de componentes
- 📊 **Ejecución asíncrona basada en coroutines** con gestión de dependencias
- 🔧 **Preservación de world-space** para prevenir artefactos de rotación
- 🏭 **Spawner de producción** con instanciación escalonada de piezas
- 📦 **Paletizador** — brazo con ruedas mecanum + sistema de navegación `CarroPaletizador.cs`
- 🚁 **DronListo.cs** — unifica todas las piezas del dron en una sola unidad rígida antes del levantamiento

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
graph LR
    subgraph ORCH["Orquestación"]
        direction TB
        O[OrquestadorDron]
        JSON[(ensamblaje_dron.json<br/>8 etapas)]
        PROD[Produccion.cs]
        O -- lee --> JSON
    end

    subgraph ASSEMBLY["Celda de Ensamblaje"]
        direction TB
        subgraph GRIPPERS["Brazos Gripper"]
            B1[Alpha · Brazos]
            B2[Beta · Brazos]
        end
        subgraph SUCTION["Brazo Ventosa"]
            B3[Omega · Ventosa]
        end
        subgraph PARTS["Piezas del Dron"]
            BASE[Base] --- PCB[PCB]
            MOTOR[Motores ×4] --- HELICE[Hélices ×4]
            TAPA[Tapa]
        end
        B1 & B2 -->|agarre + snap| MOTOR & HELICE
        B1 -->|agarre + snap| BASE
        B3 -->|ventosa + snap| PCB & TAPA
    end

    subgraph PALLET["Paletizado"]
        direction TB
        B4[Paletizador · Ventosa<br/>ruedas mecanum]
        CARRO1[Carro 1]
        CARRO2[Carro 2]
        B4 -->|llena| CARRO1
        B4 -.->|rota a| CARRO2
    end

    PROD -->|instancia| PARTS
    O -->|activa| B1 & B2 & B3 & B4
    B3 -->|transfiere dron| B4

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
| **Paletizador** | `Ventosa.cs` | Ventosa (succión) | Paletizado — recoge dron, se desplaza con ruedas mecanum, llena carros | Drones completados → Cajas de Carro 1 / Carro 2 |

### Flujo de Secuencia de Ensamblaje

```mermaid
sequenceDiagram
    participant O as Orquestador
    participant A as Alpha
    participant B as Beta
    participant W as Omega
    participant P as Paletizador
    participant D as Dron

    rect rgb(30, 80, 60)
        Note over O,D: Fase de Ensamblaje (< 1 min)
        O->>A: Etapa 1 — Colocar Base
        A->>D: agarre → suelta Base

        O->>W: Etapa 2 — Colocar PCB
        W->>D: ventosa → snap PCB

        O->>+A: Etapa 3 — Motores 1 y 3
        O->>+B: Etapa 3 — Motores 1 y 3
        A->>D: snap Motor
        B->>D: snap Motor
        deactivate A
        deactivate B

        O->>+A: Etapa 4 — Motores 2 y 4
        O->>+B: Etapa 4 — Motores 2 y 4
        A->>D: snap Motor
        B->>D: snap Motor
        deactivate A
        deactivate B

        O->>W: Etapa 5 — Colocar Tapa
        W->>D: ventosa → snap Tapa

        O->>+A: Etapa 6 — Hélices 1 y 3
        O->>+B: Etapa 6 — Hélices 1 y 3
        A->>D: snap Hélice
        B->>D: snap Hélice
        deactivate A
        deactivate B

        O->>+A: Etapa 7 — Hélices 2 y 4
        O->>+B: Etapa 7 — Hélices 2 y 4
        A->>D: snap Hélice
        B->>D: snap Hélice
        deactivate A
        deactivate B

        O->>W: Etapa 8 — Transferir dron
        W->>P: ubica dron en zona de paletizado
    end

    rect rgb(120, 60, 20)
        Note over P: Fase de Paletizado (bucle paralelo)
        loop Cada dron
            P->>D: recoge de zona de paletizado
            P->>P: se desplaza al carro activo
            P->>P: ubica en caja · cierra caja
        end
        Note over P: Carro lleno → cambia carro → repite
    end
```

### Diagrama de Interacción de Scripts

```mermaid
classDiagram
    direction TB

    class OrquestadorDron {
        +Brazos alfa, beta
        +Ventosa omega, paletizador
        +string archivoMaestro
        +int etapaActual
        +CargarMaestro()
        +EjecutarEtapa(int index)
    }

    class Brazos {
        +ArticulationBody Waist..Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +AgarrarObjeto()
        +LiberarObjeto()
        +LoadFromFile()
    }

    class Ventosa {
        +ArticulationBody Waist..Arm03
        +bool suctionActive
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +IniciarSecuencia()
        +NotifyObjectInside()
        +LoadFromFile()
    }

    class EnsambleGri {
        +Transform puntoEnsamble
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +Transform baseParent
        +ConfigurarRotacionPorNumero()
    }

    class Ensamble {
        +Transform puntoEnsamble
        +bool snapPorProximidad
        +bool congelarAlLiberar
        +Vector3 rotacionFinalEnsamble
        +NotificarLiberad()
    }

    class Produccion {
        +Spawner spawnBase, spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +IEnumerator SecuenciaEnsamblaje()
    }

    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Spawn()
    }

    class CentrarBase {
        +float targetX, targetZ
        +CentrarEnXZ()
    }

    OrquestadorDron --> Brazos : alfa / beta
    OrquestadorDron --> Ventosa : omega / paletizador
    Brazos --> EnsambleGri : snap via GripperTrigger
    Ventosa --> Ensamble : snap via SuctionTrigger
    Brazos --> CentrarBase : centra Base al soltar
    Produccion --> Spawner : gestiona
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

| Archivo | Brazo | Poses | Descripción |
|---------|-------|-------|-------------|
| `Poses_BaseNueva.json` | Alpha | 6 | Colocar base del dron |
| `Poses_PCB.json` | Omega | 7 | Colocar PCB con ventosa |
| `Poses_Motor1.json` | Beta | 6 | Motor diagonal 1 |
| `Poses_Motor2.json` | Beta | 4 | Motor diagonal 2 |
| `Poses_Motor3.json` | Beta | 6 | Motor diagonal 3 |
| `Poses_Motor4.json` | Beta | 4 | Motor diagonal 4 |
| `Poses_Tapa.json` | Omega | 5 | Colocar tapa (cierre final) |
| `Poses_Alpha.json` | Alpha | 29 | Secuencia completa Alpha |
| `Poses_Beta.json` | Beta | 24 | Secuencia completa Beta |
| `Poses_Omega.json` | Omega | 18 | Secuencia completa Omega (incluye transferencia del dron) |
| `Poses_Palet.json` | Paletizador | 3 | Secuencia de agarre del Paletizador |
| `poses2_cubo.json` | — | 1 | Prueba/debug |

---

### 4. Arquitectura de Movimiento Descentralizada

El orquestador central (`OrquestadorDron.cs`) fue **eliminado de la escena activa**. La coordinación de movimiento ahora es completamente descentralizada: cada brazo lee y ejecuta su propio archivo JSON de poses de forma independiente. Los cuatro brazos operan en secuencia por diseño de sus respectivos archivos JSON, sin un coordinador maestro sondeando su estado.

**Flujo de movimiento por brazo**:
```
Archivo JSON propio (Poses_*.json)
    → LoadFromFile() en Awake
        → IniciarSecuencia() en Start / trigger
            → SmoothX / SmoothZ por frame
                → ArticulationDrive.target actualizado
```

**Archivos JSON activos y sus brazos**:

| Archivo | Brazo | Poses |
|---------|-------|-------|
| `Poses_BaseNueva.json` | Alpha | 6 |
| `Poses_PCB.json` | Omega | 7 |
| `Poses_Motor1.json` | Beta | 6 |
| `Poses_Motor2.json` | Beta | 4 |
| `Poses_Motor3.json` | Beta | 6 |
| `Poses_Motor4.json` | Beta | 4 |
| `Poses_Tapa.json` | Omega | 5 |
| `Poses_Omega.json` | Omega | 18 |
| `Poses_Palet.json` | Paletizador | 3 |
| `Poses_Alpha.json` | Alpha | 29 |
| `Poses_Beta.json` | Beta | 24 |
| `poses2_cubo.json` | — | 1 (debug) |

---

### 5. Unificación del Dron (`DronListo.cs`)

Antes de que Omega levante el dron completo, todas las piezas ensambladas deben comportarse como una sola unidad rígida. `DronListo.cs` se adjunta a `BasePrefab` y gestiona esta transición.

```csharp
public void PrepararParaLevantamiento() {
    dronesListo = true;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}

public void SoltarDron() {
    dronesListo = false;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
```

---

### 6. Navegación del Paletizador (`CarroPaletizador.cs`)

`CarroPaletizador.cs` gestiona el movimiento del Paletizador por el suelo. El brazo Paletizador (`Ventosa`) es un **hijo** del GameObject del carro, por lo que toda la unidad — brazo + carro — se desplaza junta. La navegación se realiza solo en XZ (Y permanece fijo) y rota en el eje Y hacia cada waypoint.

**Waypoints** (definidos como referencias `Transform` en el Inspector):

| Punto | Propósito |
|-------|-----------|
| `puntoInicio` | Posición de inicio / home |
| `puntoGiro` | Waypoint de rotación — el brazo se orienta antes de viajar al punto de entrega |
| `punto_1_1` | Carro 1, slot 1 |
| `punto_1_2` | Carro 1, slot 2 |
| `punto_2_1` | Carro 2, slot 1 |
| `punto_2_2` | Carro 2, slot 2 |

**Secuencia de paletizado** (coroutine):
```
Carro 1:
  punto_1_1 → deposita dron 1 → deposita dron 2
  punto_1_2 → deposita dron 1 → deposita dron 2
Carro 2:
  punto_2_1 → deposita dron 1 → deposita dron 2
  punto_2_2 → deposita dron 1 → deposita dron 2
Regresa a puntoInicio
```

**Lógica de movimiento** (coroutine `IrA`): traslada al objetivo XZ → rota para orientarse hacia el destino en el eje Y, ambos con velocidad y tolerancia configurables.

---

### 7. Mecánicas de Snap

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

### 8. Prevención de Race Conditions

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

### 9. Spawner de Producción (`Produccion.cs`)

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

## Estructura del Proyecto

```
drone-packaging-simulation-unity/
├── docs/
│   └── simulation_overview.png      # Vista isométrica de la celda robótica de ensamblaje
├── Assets/
│   ├── Brazos.cs                    # Brazo gripper — Alpha, Beta (579 líneas)
│   ├── Ventosa.cs                   # Brazo ventosa — Omega, Paletizador (688 líneas)
│   ├── CarroPaletizador.cs          # Navegación del Paletizador — waypoints mecanum (163 líneas)
│   ├── DronListo.cs                 # Unifica piezas del dron como un solo cuerpo rígido (39 líneas)
│   ├── Ensamble.cs                  # Lógica snap para PCB / Tapa (144 líneas)
│   ├── EnsambleGri.cs               # Lógica snap para Motores / Hélices (174 líneas)
│   ├── Spawner.cs                   # Instancia prefabs y asigna refs de ensamble (32 líneas)
│   ├── Produccion.cs                # Secuenciador de spawn con coroutine escalonado (56 líneas)
│   ├── Angulos.cs                   # Controlador manual de ángulos de articulaciones
│   ├── CentrarBase.cs               # Centra la Base en XZ después de la colocación
│   ├── GripperTrigger.cs            # OnTriggerEnter → Brazos.NotifyObjectInside()
│   ├── SuctionTrigger.cs            # OnTriggerEnter → Ventosa.NotifyObjectInside()
│   ├── MoverCajon.cs                # Mueve el carro entre waypoints
│   ├── OrquestadorDron.cs           # En Assets pero eliminado de la escena activa
│   ├── Cian.mat
│   ├── CV_1.renderTexture
│   ├── CV_5.renderTexture
│   ├── New Animator Controller.*
│   ├── StreamingAssets/
│   │   └── ensamblaje_dron.json     # JSON maestro (legacy — no activo en la escena actual)
│   ├── JSON_Generados/              # 12 archivos JSON de poses — cada brazo lee el suyo
│   └── Scenes/
│       └── SampleScene.unity
├── Packages/
│   └── manifest.json
└── ProjectSettings/
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
   - Cada brazo carga su propio archivo JSON automáticamente desde `Assets/JSON_Generados/`
   - El archivo de cada brazo se asigna en el Inspector del componente `Brazos` o `Ventosa` mediante el campo `saveFileName`
   - Los waypoints del Paletizador se asignan en el Inspector de `CarroPaletizador`

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
