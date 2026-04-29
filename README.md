# Drone Packaging Simulation  -  Unity

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
> *Isometric view of the robotic assembly cell  -  4 articulated arms (Alpha, Beta, Omega, Paletizador) with mecanum wheels.*

<br/>

[![Demo Video](https://img.shields.io/badge/▶_Demo_Video-YouTube-red?style=for-the-badge&logo=youtube)](https://youtu.be/U491eei--Xc?si=DweGneszA-7RkUbz)

> *Full simulation run — assembly, palletizing, and cart swap with CODESYS & FluidSIM integration.*

</div>

---

## Table of Contents

- [Overview](#overview)
- [Technical Stack](#technical-stack)
- [CODESYS & FluidSIM Integration](#codesys--fluidsim-integration)
  - [Tool Versions](#tool-versions)
  - [Communication Architecture](#communication-architecture)
  - [Variable Map](#variable-map)
  - [Safety & Control Logic](#safety--control-logic)
  - [CODESYS PLC Program](#codesys-plc-program-plc_prg)
  - [FluidSIM Pneumatic Simulation](#fluidsim-pneumatic-simulation)
- [System Architecture](#system-architecture)
- [Implemented Systems](#implemented-systems)
  - [1. Gripper System](#1-gripper-system-brazoscs)
  - [2. Suction Cup System](#2-suction-cup-system-ventosacs)
  - [3. JSON Motion Sequencer](#3-json-motion-sequencer)
  - [4. Decentralized Motion Architecture](#4-decentralized-motion-architecture)
  - [5. Drone Unification](#5-drone-unification-dronlistocs)
  - [6. Palletizer Navigation](#6-palletizer-navigation-carropaletizadords)
  - [7. Snap Mechanics](#7-snap-mechanics)
  - [8. Race Condition Prevention](#8-race-condition-prevention)
  - [9. Production Spawner](#9-production-spawner-produccioncs)
  - [10. Box Lid Closure and Cart Retirement](#10-box-lid-closure-and-cart-retirement)
  - [11. HMI Dashboard](#11-hmi-dashboard-hmimanagercs--codesystcpclientcs)
  - [12. Safety Vision System](#12-safety-vision-system)
  - [13. STOP / EMERGENCIA Pause](#13-stop--emergencia-pause-produccioncs)
  - [14. Statistical Analysis & OEE Report](#14-statistical-analysis--oee-report-htmlanalisis-estadistico-y-oee-finalhtml)
- [Project Structure](#project-structure)
- [Installation](#installation)
- [Resolved Issues](#resolved-issues)
- [Authors](#authors)
- [License](#license-and-rights)

> **14 C# scripts · 1 Python script · 4 JSON pose files (duplicated in StreamingAssets/)**

---

## Overview

This project is a **Unity-based simulation** of a robotic drone assembly and palletizing cell. Four robotic arms collaborate to assemble a drone through physically realistic interactions and JSON-driven motion sequences. The completed drone is then transported and palletized into production carts by a fourth arm that moves autonomously on **mecanum wheels**.

The simulation is intended for **virtual process validation** of industrial robotic cells. It integrates with a **CODESYS PLC project** and a **FluidSim OPC simulation** that model the industrial automation layer.

### Key Features

- 🦾 **Four coordinated robotic arms** (Alpha, Beta, Omega, Paletizador) with ArticulationBody physics
- 🔄 **Decentralized JSON-driven motion**  -  each arm reads its own pose file and executes independently
- ⚙️ **Dual end effectors**: Gripper (`Brazos.cs`) and Suction Cup (`Ventosa.cs`)
- 🎯 **Proximity-based snap system** for component assembly
- 📊 **Coroutine-based asynchronous execution** with dependency management
- 🔧 **World-space preservation** to prevent rotation artifacts
- 🏭 **Production spawner** with staggered coroutine-based part instantiation, including box spawning
- 📦 **Paletizador**  -  mecanum-wheel arm + `CarroPaletizador.cs` navigation system
- 🚁 **DronListo.cs**  -  unifies all drone parts into a single rigidbody unit before pickup
- 🖥️ **CODESYS / FluidSim integration**  -  PLC and OPC simulation files included in the repository
- 📡 **Real-time HMI** (`HmiManager.cs`)  -  in-Unity dashboard with TextMeshPro; arm states, LED panel, TCP log, cycle timer
- 📷 **Safety vision system** (`Auxiliar System.py`)  -  ESP32-CAM stream over WebSocket, MediaPipe hand detection, alerts via voice/email/Telegram

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
| `com.unity.feature.development` | 1.0.1 | Development tools bundle |
| `com.unity.ide.rider` | 3.0.31 | Rider IDE integration |
| `com.unity.ide.visualstudio` | 2.0.22 | Visual Studio integration |
| `com.unity.ide.vscode` | 1.2.5 | VS Code integration |
| `com.unity.ugui` | 1.0.0 | Legacy UI system |

---

## CODESYS & FluidSIM Integration

### Tool Versions

| Tool | Version | Vendor |
|------|---------|--------|
| **CODESYS** | 3.5.15.40 (V3.5 SP9 P1) | 3S-Smart Software Solutions GmbH |
| **FluidSIM** | 4.2p / 1.67 Pneumatics (19.02.2010) | Festo Didactic GmbH & Co. KG |

#### Required CODESYS Libraries

| Library | Version | Purpose |
|---------|---------|---------|
| **SysSocket** | 3.5.15.0 | TCP socket communication (Unity ↔ CODESYS) |
| **SysSocket Interfaces** | 3.5.15.0 | Interface definitions for SysSocket |
| **SysTypes2 Interfaces** | 3.5.15.0 | Primitive type definitions used by Sys libraries |

Add via **Tools → Library Manager → Add Library**.

---

### Communication Architecture

The three layers communicate over two protocols: **TCP/IP** (Unity ↔ CODESYS) and **OPC DA** (CODESYS ↔ FluidSIM).

```mermaid
graph LR
    subgraph UNITY["Unity  ·  CodesysTcpClient.cs"]
        direction TB
        UA["Ventosa.cs · CarroPaletizador.cs"]
        UTCP["TX  0xAA · VENTOSAS · LEDS  3 bytes / 50 ms\nRX  0xBB · plc1 · plc2 · inputs · SISTEMA_ON  5 bytes / 50 ms"]
        UA --> UTCP
    end

    subgraph PLC["CODESYS V3.5 SP9 P1  ·  port 8888"]
        direction TB
        PA["PLC_PRG\nSysSockSelect non-blocking"]
        PB["salidas_plc1\nventosa solenoids · LED5-8"]
        PC["salidas_plc2\nLED1-4 · NEUMATICA"]
        PA --> PB
        PA --> PC
    end

    subgraph OPC["FluidSIM 4.2p  ·  OPC DA"]
        direction TB
        FA["Pneumatic circuit\nNEUMATICA_ON / OFF"]
        FB["Physical inputs\nBP1 · BP2 · START · STOP · EMERG"]
    end

    UTCP <-->|"TCP/IP · port 8888"| PA
    PB -->|"OPC"| FA
    PC -->|"OPC"| FA
    FB -->|"OPC"| PA

    style UNITY fill:#1a3a5c,color:#fff,stroke:#4a6fa5
    style PLC   fill:#8b0000,color:#fff,stroke:#c0392b
    style OPC   fill:#1a5c2a,color:#fff,stroke:#27ae60
```

---

### Variable Map

#### TCP Variables (Unity → CODESYS)

| Variable | Type | Bits | Description |
|----------|------|------|-------------|
| `TCP_COMANDOS_VENTOSAS` | `BYTE` | Bit 0 = Omega · Bit 1 = Paletizador | Suction cup on/off commands sent from Unity over TCP |
| `TCP_COMANDOS_LEDS` | `BYTE` | Bits 0–7 = LED1–LED8 | LED panel state sent from Unity over TCP |

#### Physical Inputs (`entradas_plc1` byte  -  bit mask)

| Bit | Mask | Variable | Description |
|-----|------|----------|-------------|
| 0 | `16#01` | `BP1` | Pushbutton 1 |
| 1 | `16#02` | `BP2` | Pushbutton 2 |
| 2 | `16#04` | `START` | Start command |
| 3 | `16#08` | `STOP` | Stop command (NC  -  must be HIGH to run) |
| 4 | `16#10` | `EMERGENCIA` | Emergency stop (NC  -  must be HIGH to run) |

#### Output Module 1 (`salidas_plc1` byte)

| Bit | Mask | Signal | Description |
|-----|------|--------|-------------|
| 0 | `16#01` | `VENTOSA_OMEGA_ON` | Activate Omega suction |
| 1 | `16#02` | `VENTOSA_OMEGA_OFF` | Deactivate Omega suction |
| 2 | `16#04` | `VENTOSA_PALETIZADOR_ON` | Activate Palletizer suction |
| 3 | `16#08` | `VENTOSA_PALETIZADOR_OFF` | Deactivate Palletizer suction |
| 4 | `16#10` | `LED7` | LED 7 |
| 5 | `16#20` | `LED8` | LED 8 |
| 6 | `16#40` | `LED5` | LED 5 |
| 7 | `16#80` | `LED6` | LED 6 |

#### Output Module 2 (`salidas_plc2` byte)

| Bit | Mask | Signal | Description |
|-----|------|--------|-------------|
| 0 | `16#01` | `LED2` | LED 2 |
| 1 | `16#02` | `LED1` | LED 1 |
| 2 | `16#04` | `LED4` | LED 4 |
| 3 | `16#08` | `LED3` | LED 3 |
| 4 | `16#10` | `NEUMATICA_OFF` | Pneumatics off → FluidSIM via OPC |
| 5 | `16#20` | `NEUMATICA_ON` | Pneumatics on → FluidSIM via OPC |

#### FluidSIM I/O Module Mapping

The three FluidSIM modules bridge CODESYS bytes to the physical circuit elements simulated in FluidSIM 4.2p. "FluidSIM In" means CODESYS **writes** to FluidSIM (actuator commands); "FluidSIM Out" means FluidSIM **sends** to CODESYS (sensor feedback).

| Module | Port | Direction | CODESYS Variable | Physical Elements |
|--------|------|-----------|-----------------|-------------------|
| **Module 1** | Port 1 | FluidSIM In  -  CODESYS → FluidSIM | `salidas_plc1` (8 bits) | Solenoids 1M1/1M2 (Omega) · 2M1/2M2 (Paletizador) · Cart 2 LEDs (LED5–8) |
| **Module 2** | Port 1 | FluidSIM Out  -  FluidSIM → CODESYS | `entradas_plc1` (8 bits) | Push-buttons BP1 · BP2 · START · STOP · EMERGENCIA |
| **Module 3** | Port 1 | FluidSIM In  -  CODESYS → FluidSIM | `salidas_plc2` (8 bits) | Main valve 3M1/3M2 (pneumatics) · Cart 1 LEDs (LED1–4) |

---

### Safety & Control Logic

```mermaid
stateDiagram-v2
    [*] --> SISTEMA_OFF : power on

    SISTEMA_OFF --> SISTEMA_ON : START↑ + STOP=1 + EMERGENCIA=1
    SISTEMA_ON --> SISTEMA_OFF : STOP=0 OR EMERGENCIA=0

    state SISTEMA_ON {
        [*] --> LED_TEST
        LED_TEST --> NEUMATICA_ON : 1 s TON elapsed
        NEUMATICA_ON --> VENTOSAS_ON : TCP_COMANDOS_VENTOSAS bit set
        VENTOSAS_ON --> NEUMATICA_ON : bit cleared
    }
```

| Condition | Effect |
|-----------|--------|
| `START` rising edge + `STOP` + `EMERGENCIA` = HIGH | `SISTEMA_ON := TRUE` + 1 s LED test |
| `STOP` = LOW **or** `EMERGENCIA` = LOW | `SISTEMA_ON := FALSE` immediately |
| `SISTEMA_ON` | `NEUMATICA_ON` → FluidSIM activates pneumatic circuit |
| `TCP_COMANDOS_VENTOSAS` bit 0 | `VENTOSA_OMEGA_ON` → Module 1 bit 0 |
| `TCP_COMANDOS_VENTOSAS` bit 1 | `VENTOSA_PALETIZADOR_ON` → Module 1 bit 2 |
| `TCP_COMANDOS_LEDS` bits 0–7 | `LED1–LED8` → Modules 1 & 2 |

---

### CODESYS PLC Program (`PLC_PRG`)

`PLC_PRG` is a Structured Text program that acts as the automation core of the cell. Its main responsibilities are:

- **TCP server on port 8888** — accepts a single Unity client, receives 3-byte command packets (`0xAA · VENTOSAS · LEDS`) and sends back 5-byte status packets (`0xBB · plc1 · plc2 · inputs · SISTEMA_ON`) every 50 ms.
- **Safety logic** — monitors `STOP` and `EMERGENCIA` inputs; any LOW signal immediately sets `SISTEMA_ON := FALSE` and cuts pneumatics. System only restarts on a `START` rising edge with both signals HIGH.
- **Actuator control** — decodes `TCP_COMANDOS_VENTOSAS` bits to drive solenoid valves for the two suction cups (Omega and Paletizador), and `TCP_COMANDOS_LEDS` bits to drive 8 indicator LEDs via OPC DA.
- **OPC DA bridge** — publishes `salidas_plc1/2` and reads `entradas_plc1` through the FluidSIM OPC server, synchronizing the virtual pneumatic circuit with Unity in real time.

**Key design advantages:**

| Technique | Benefit |
|-----------|---------|
| `SysSockSelect` with zero timeout | Non-blocking socket I/O — PLC scan cycle never stalls |
| 50 ms `tSendTimer` heartbeat | Deterministic update rate; Unity thread times out cleanly |
| `tNoDataTimer` 3 s watchdog | Auto-disconnects if Unity crashes without closing the socket |
| `nBytesReceived <= 0` disconnect | Handles both graceful close (0) and RST/error (−1) |
| Positional `SysSock*` parameters | Required by CODESYS V3.5 SP9 P1 — named params unsupported |

---

### FluidSIM Pneumatic Simulation

FluidSIM 4.2p (Festo Didactic, build 19.02.2010) simulates the full pneumatic circuit for the cell. It receives actuator commands from CODESYS through the **OPC DA server** and feeds sensor feedback (push-button states, pressure confirmations) back via the FluidSIM Out module.

**Pneumatic components in the circuit:**

| Element | Type | Function |
|---------|------|----------|
| `1M1` / `1M2` | 5/2 solenoid valve | Omega suction cup (activate / deactivate) |
| `2M1` / `2M2` | 5/2 solenoid valve | Paletizador suction cup (activate / deactivate) |
| `3M1` / `3M2` | 5/2 solenoid valve | Main pneumatic supply (system ON / OFF) |
| `1BP1` | Pressure/proximity sensor | Omega cup grip confirmation → `BP1` bit |
| `1BP2` | Pressure/proximity sensor | Paletizador cup grip confirmation → `BP2` bit |
| Cart 1 LED panel | Indicator | 4 LEDs driven by `LED1–LED4` (salidas_plc2) |
| Cart 2 LED panel | Indicator | 4 LEDs driven by `LED5–LED8` (salidas_plc1) |

```mermaid
graph LR
    subgraph CMD["CODESYS Outputs"]
        direction TB
        PLC1["salidas_plc1\nbits 0–3 · ventosa solenoids\nbits 4–7 · LED5-8"]
        PLC2["salidas_plc2\nbits 0–3 · LED1-4\nbits 4–5 · NEUMATICA"]
    end

    subgraph ACT["FluidSIM In — Actuators"]
        direction TB
        V1["1M1/1M2 · 5/2 valve\nOmega suction cup"]
        V2["2M1/2M2 · 5/2 valve\nPaletizador suction cup"]
        V3["3M1/3M2 · 5/2 valve\nMain pneumatic supply"]
        LC["LED panels\nLED1–4 · LED5–8"]
    end

    subgraph SENS["FluidSIM Out — Sensor Feedback → entradas_plc1"]
        direction TB
        S1["1BP1 · grip confirm Omega\n→ BP1 bit 0"]
        S2["1BP2 · grip confirm Paletizador\n→ BP2 bit 1"]
    end

    PLC1 -->|"bits 0–1"| V1
    PLC1 -->|"bits 2–3"| V2
    PLC1 -->|"bits 4–7"| LC
    PLC2 -->|"bits 4–5"| V3
    PLC2 -->|"bits 0–3"| LC
    V1 -->|"pressure sensor"| S1
    V2 -->|"pressure sensor"| S2

    style CMD  fill:#8b0000,color:#fff,stroke:#c0392b
    style ACT  fill:#1a5c2a,color:#fff,stroke:#27ae60
    style SENS fill:#1a3a5c,color:#fff,stroke:#4a6fa5
```

> **Note:** Physical buttons START, STOP, and EMERGENCIA (entradas\_plc1 bits 2–4) are wired directly to FluidSIM's input module and are not driven by the pneumatic circuit shown above.

---

## System Architecture

### Component Diagram

```mermaid
graph TB
    subgraph DATA["Data & Spawning"]
        direction LR
        JF[(StreamingAssets · 8 pose files)]
        SP["Spawners · parts + boxes"]
        P["Produccion.cs"] -->|staggered spawn| SP
    end

    subgraph CELL["Assembly Cell  ·  reads pose JSON"]
        direction LR
        B1["Alpha · Brazos\nBase · Motors ×2 · Hélices ×2"]
        B2["Beta · Brazos\nMotors ×2 · Hélices ×2"]
        B3["Omega · Ventosa\nPCB · Tapa · drone transfer"]
    end

    subgraph PAL["Palletizing"]
        direction LR
        CARRO["CarroPaletizador.cs"]
        B4["Paletizador · Ventosa\nmecanum wheels"]
        CARRO --> B4
        B4 --> C1["Cart 1"]
        B4 -.-> C2["Cart 2"]
    end

    subgraph AUTO["Industrial Automation"]
        direction LR
        PLC["CODESYS 3.5.15.40"]
        OPC["FluidSIM 4.2p\nPneumatics · OPC DA"]
        HW["BP1 · BP2 · START · STOP · EMERG\nentradas_plc1"]
        PLC -->|"salidas_plc1/2 · OPC DA"| OPC
        HW -->|"FluidSIM Out"| PLC
    end

    DATA --> CELL
    JF -. pose .-> CELL
    JF -. pose .-> PAL
    SP -->|parts| CELL
    B3 -->|"drone transfer"| B4
    B3 & B4 -->|"TCP/IP · port 8888"| PLC

    style B1   fill:#1D9E75,stroke:#085041,color:#fff
    style B2   fill:#1D9E75,stroke:#085041,color:#fff
    style B3   fill:#378ADD,stroke:#042C53,color:#fff
    style B4   fill:#B75A34,stroke:#5C2506,color:#fff
    style CARRO fill:#534AB7,stroke:#26215C,color:#fff
    style PLC  fill:#8b0000,color:#fff,stroke:#8b0000
    style OPC  fill:#1a5c2a,color:#fff,stroke:#1a5c2a
    style HW   fill:#4a4a4a,color:#fff,stroke:#4a4a4a
```

### Arm Configuration

| Arm | Class | End Effector | Status | Components Handled |
|-----|-------|-------------|--------|-------------------|
| **Alpha** | `Brazos.cs` | Gripper (pinza) | ✅ Implemented | Base, Motors ×2, Hélices ×2 |
| **Beta** | `Brazos.cs` | Gripper (pinza) | ✅ Implemented | Motors ×2, Hélices ×2 |
| **Omega** | `Ventosa.cs` | Suction Cup (ventosa) | ✅ Implemented | PCB, Tapa, drone transfer |
| **Paletizador** | `Ventosa.cs` + mecanum wheels | Suction Cup (ventosa) | ✅ Implemented | Completed drones → Cart 1 / Cart 2 |

### Assembly Sequence Flow

```mermaid
sequenceDiagram
    participant P as Produccion
    participant A as Alpha (Brazos)
    participant B as Beta (Brazos)
    participant W as Omega (Ventosa)
    participant PAL as Paletizador
    participant D as Drone

    rect rgb(30, 80, 60)
        Note over P,D: Phase 1  -  Spawning
        Note over P: Boxes pre-spawned in Start() before assembly begins
        P->>D: spawn Base → 1s → PCB → 1s → Motors 1+2 → 1s → Motors 3+4 → 1s → Hélices 1+2 → 2s → Hélices 3+4 → 2s → Tapa
    end

    rect rgb(25, 60, 90)
        Note over A,D: Phase 2  -  Assembly (arms start concurrently, each reads its own JSON)
        A->>D: grip Base → CentrarBase.IniciarCentrado()
        W->>D: suction + snap PCB

        par Alpha assembles motors and hélices
            A->>D: snap Motor 1, Motor 2, Hélice 1, Hélice 2
        and Beta assembles motors and hélices
            B->>D: snap Motor 3, Motor 4, Hélice 3, Hélice 4
        end

        W->>D: suction + snap Tapa
        Note over D: DronListo auto-detects piezasEsperadas=10 reached → seals drone
        W->>PAL: transfer sealed drone to staging zone
    end

    rect rgb(120, 60, 20)
        Note over PAL: Phase 3  -  Palletizing (EjecutarSecuencia loop)
        loop For each MovimientoPaletizado in movimientos list
            PAL->>D: pick drone (ventosa.TieneObjeto)
            PAL->>PAL: TrasladarA(zonaGiro) → GirarCarroSobrePunto(anguloGiro)
            PAL->>PAL: TrasladarConPivotRotado / TrasladarEnL(puntoDestino)
            PAL->>D: permisoParaSoltar=true → drone deposited in box
            PAL->>PAL: return to zonaGiro → GirarCarroSobrePunto(-anguloGiro)
            PAL->>PAL: TrasladarA(puntoInicio)
        end
    end
```

### Script Interaction Diagram

```mermaid
classDiagram
    direction LR

    class Brazos {
        +ArticulationBody Waist, Arm01, Arm02, Arm03
        +ArticulationBody GripperAssembly, Gear1, Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +bool secuenciaTerminada
        +string saveFileName
        +float speed
        +float tiempoEsperaInicial
        +CentrarBase scriptCentrarBase
        +IniciarSecuencia()
        +IniciarSecuenciaConEspera()
        +ResetCompleto()
        +LoadFromFile()
        +SaveToFile()
        +NotifyObjectInside(GameObject)
        +NotifyObjectExit(GameObject)
        -AgarrarObjeto()
        -LiberarObjeto()
    }

    class Ventosa {
        +ArticulationBody Waist, Arm01, Arm02, Arm03, GripperAssembly
        +bool suctionActive
        +float suctionForce
        +float speed
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +bool secuenciaTerminada
        +bool dronDepositado
        +bool permisoParaSoltar
        +bool esperarPermisoParaSoltar
        +bool usarSecuenciaDeCajas
        +int[] ordenCajas
        +bool emparentarACaja
        +float delayCierreTapa
        +bool TieneObjeto
        +Transform puntoDestinoDron
        +Vector3 rotacionFijaAlAgarrar
        +IniciarSecuencia()
        +IniciarSecuenciaConEspera()
        +ResetCompleto()
        +ReiniciarContadorDrones()
        +LoadFromFile()
        +SaveToFile()
        +NotifyObjectInside(GameObject)
        +NotifyObjectExit(GameObject)
        +LiberarObjeto()
        -AgarrarObjetoConSuccion()
        -LiberarEnSecuencia()
    }

    class CarroPaletizador {
        +Transform puntoInicio
        +List~MovimientoPaletizado~ movimientos
        +Ventosa ventosa
        +ArticulationBody articulacionRaiz
        +float velocidadMovimiento
        +float duracionGiro
        +float duracionTrasladoFinal
        +float delayDespuesSoltar
        +int totalDrones
        +IniciarSecuenciaCarro()
        -EjecutarSecuencia()
        -TrasladarA(Transform, Transform)
        -TrasladarConPivotRotado(Transform, Transform)
        -TrasladarEnL(Transform, Transform, bool)
        -GirarCarroSobrePunto(float, Transform)
        -ActualizarPosicionBrazo()
    }

    class CodesysTcpClient {
        +string codesysIP
        +int codesysPort
        +float reconnectInterval
        +bool isConnected
        +byte TCP_COMANDOS_VENTOSAS
        +byte TCP_COMANDOS_LEDS
        +byte salidas_plc1, salidas_plc2, entradas_plc1
        +bool VENTOSA_OMEGA_ON, VENTOSA_OMEGA_OFF
        +bool VENTOSA_PALET_ON, VENTOSA_PALET_OFF
        +bool LED1..LED8
        +bool NEUMATICA_ON, NEUMATICA_OFF
        +bool SISTEMA_ON
        +event OnLogMessage
        +SetVentosaOmega(bool)
        +SetVentosaPaletizador(bool)
        +SetLed(int, bool)
        +SetAllLeds(bool)
    }

    class HmiManager {
        +CodesysTcpClient tcp
        +Produccion produccion
        +Image imgSistema, imgNeumatica, imgTcpEstado
        +Image imgOmegaEstado, imgPaletEstado
        +Image[] imgLeds
        +Color colorOn, colorOff, colorWarn, colorAct
        -RefreshConexion()
        -RefreshBrazos()
        -RefreshCajas()
        -RefreshPLC()
        -RefreshMetricas()
        +AddLog(string)
    }

    class DronListo {
        +bool dronesListo
        +int piezasEsperadas
        +PrepararParaLevantamiento()
        +SoltarDron()
        -ContarPiezasEnsambladas()
    }

    class EnsambleGri {
        +Transform puntoEnsamble
        +Transform baseParent
        +float distanciaActivacion
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +bool usarRotacionPorNumero
        +Vector3 rotacionForzada
        +NotificarLiberad(Collider[])
        -ConfigurarRotacionPorNumero(string)
        -IniciarEncaje()
    }

    class Ensamble {
        +Transform puntoEnsamble
        +float offsetHundimiento
        +float velocidadEncaje
        +bool snapPorProximidad
        +float distanciaActivacionSnap
        +bool congelarAlLiberar
        +bool yaEnsamblado
        +Vector3 rotacionFinalEnsamble
        +AsignarBase(Transform)
        +NotificarLiberad()
        -IniciarEncaje()
    }

    class Produccion {
        +Spawner spawnBase, spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +Spawner[] spawnsCaja
        +Brazos brazoAlpha, brazoBeta
        +Ventosa brazoOmega, brazoPaletizador
        +CodesysTcpClient tcp
        +int dronesAProducir
        +bool OmegaActivo
        +bool PaletActivo
        +string CarroActualTag
        +float TiempoCicloActual
        +IEnumerator LoopProduccion()
        +IEnumerator SecuenciaEnsamblaje()
        -IEnumerator SwapCarro()
    }

    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Transform baseParent
        +GameObject Spawn()
    }

    class CentrarBase {
        +Transform puntoDestino
        +Vector3 rotacionFija
        +IniciarCentrado()
    }

    class CerradorTapa {
        +Transform tapa
        +Vector3 rotacionAbierta
        +Vector3 rotacionCerrada
        +float duracionCierre
        +AnimationCurve curva
        +bool tapaCerrada
        +CerrarTapa()
        +AbrirTapaInstantaneo()
    }

    class RetiradorCarro {
        +int[] cajasAsignadas
        +int numeroCajaFinal
        +bool cajasAdoptadas
        +IntentarAdoptarCajas()
        +AdoptarCajas()
    }

    class GripperTrigger {
        +Brazos mainScript
    }

    class SuctionTrigger {
        +Ventosa mainScript
    }

    CarroPaletizador --> Ventosa : drives paletizador
    CarroPaletizador --> RetiradorCarro : triggers box adoption
    GripperTrigger --> Brazos : OnTriggerEnter / OnTriggerExit
    SuctionTrigger --> Ventosa : OnTriggerEnter / OnTriggerExit
    Brazos --> EnsambleGri : snap on release
    Ventosa --> Ensamble : snap on release
    Ventosa --> CerradorTapa : triggers lid close after deposit
    Brazos --> CentrarBase : centers Base after release
    Produccion --> Spawner : manages
    Produccion --> CodesysTcpClient : reads SISTEMA_ON gate
    Spawner ..> EnsambleGri : assigns baseParent
    Spawner ..> Ensamble : assigns puntoEnsamble
    DronListo ..> Ventosa : dronesListo flag read by Omega
    HmiManager --> CodesysTcpClient : monitors TCP + PLC state
    HmiManager --> Produccion : reads arm/production state
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

**Behavior**: Magnetic attraction animation before attachment. Omega handles the PCB, Tapa, and full drone transfer to the palletizing zone. The Paletizador uses the same class to pick up and transport completed drones.

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

**Paletizador-specific fields** (only active when the arm is the Paletizador):

| Field | Type | Description |
|-------|------|-------------|
| `usarSecuenciaDeCajas` | `bool` | Cycles through boxes in `ordenCajas[]` order |
| `ordenCajas` | `int[]` | Box index sequence for drone deposit |
| `emparentarACaja` | `bool` | When true, reparents the drone to the box after deposit |
| `delayCierreTapa` | `float` | Seconds to wait before triggering `CerradorTapa.CerrarTapa()` (default 1 s) |

**Advantages**:
- Clear visual feedback for the user
- Fixed rotation on grab via `rotacionFijaAlAgarrar`
- Smooth transition without teleportation

---

### 3. JSON Motion Sequencer

Each arm's movement is defined in external JSON files under `Assets/JSON_Generados/` and loaded at runtime. Each file stores a list of `RobotPose` objects with full joint targets.

**Real pose data structures**:

`RobotPose` (Brazos - Alpha, Beta):
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

`VentosaPose` (Ventosa - Omega, Paletizador):
```json
{
  "poses": [
    {
      "waist": 180.0,
      "arm01": 65.0,
      "arm02": -10.0,
      "arm03": 0.0,
      "gripperAssembly": 63.0,
      "suctionActive": false,
      "delay": 0.0
    }
  ]
}
```

**Available JSON files** (4 unique files  -  each arm loads its own consolidated sequence):

| File | Arm | Poses | Description |
|------|-----|-------|-------------|
| `Poses_Alpha.json` | Alpha | 29 | Full Alpha sequence: base placement, motors 1 & 2, hélices 1 & 2 |
| `Poses_Beta.json` | Beta | 24 | Full Beta sequence: motors 3 & 4, hélices 3 & 4 |
| `Poses_Omega.json` | Omega | 18 | Full Omega sequence: PCB, tapa, and drone transfer to palletizing zone |
| `Poses_Palet.json` | Paletizador | 7 | Paletizador grip and deposit sequence |

> Files are located in both `Assets/JSON_Generados/` (editor) and `Assets/StreamingAssets/` (runtime).

---

### 4. Decentralized Motion Architecture

Motion coordination is fully decentralized: each arm reads and executes its own JSON pose file independently via `LoadFromFile()` on `Awake` and `IniciarSecuencia()` on `Start` or external trigger. The four arms operate in sequence by design of their respective JSON files.

**Motion flow per arm**:
```
Arm's own JSON file (Poses_*.json)
    → LoadFromFile() on Awake
        → IniciarSecuencia() on Start / trigger
            → SmoothX / SmoothZ per frame
                → ArticulationDrive.target updated
```

---

### 5. Drone Unification (`DronListo.cs`)

Before Omega lifts the completed drone, all assembled parts must behave as a single rigid unit. `DronListo.cs` is attached to `BasePrefab` and handles this transition. It **auto-detects** assembly completion by counting child `Rigidbody` components against the configurable `piezasEsperadas` threshold (default 10 = PCB + 4 motors + 4 hélices + tapa), sealing the drone automatically without requiring external triggers.

```csharp
// Auto-detection in Update()
void Update() {
    if (!yaSellado && !dronesListo) {
        int piezasActuales = ContarPiezasEnsambladas();
        if (piezasActuales >= piezasEsperadas)
            PrepararParaLevantamiento();
    }
}

public void PrepararParaLevantamiento() {
    if (yaSellado) return;
    yaSellado = true;
    dronesListo = true;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}

public void SoltarDron() {
    dronesListo = false;
    yaSellado = false;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
```

---

### 6. Palletizer Navigation (`CarroPaletizador.cs`)

`CarroPaletizador.cs` manages the Paletizador's floor movement. The Paletizador arm (`Ventosa`) is a **child** of the cart GameObject, so the entire unit  -  arm + cart  -  moves together. Navigation moves in XZ only (Y stays fixed). The cart rotates on the Y axis by pivoting around configurable `zonaGiro` points.

**Movement configuration**  -  Inspector-defined `List<MovimientoPaletizado>`, one entry per drone. Each entry specifies:

| Field | Type | Description |
|-------|------|-------------|
| `nombre` | `string` | Descriptive label (e.g. "Dron 1 → Punto1_1") |
| `zonaGiro` | `Transform` | Pivot point  -  cart rotates around this position |
| `anguloGiro` | `float` | Rotation angle on Y (-90° or +90°) |
| `puntoDestino` | `Transform` | Final deposit position |
| `patron` | `PatronMovimiento` | `Directo` (straight) or `EnL_XLuegoZ` (L-shaped path) |

**Palletizing sequence per drone** (coroutine `EjecutarSecuencia`):
```
1. Wait until arm holds drone       (ventosa.TieneObjeto)
2. TrasladarA(zonaGiro)              -  translate to pivot zone
3. GirarCarroSobrePunto(anguloGiro)  -  rotate around pivot on Y axis
4. TrasladarConPivotRotado / TrasladarEnL(puntoDestino)  -  move to deposit
5. permisoParaSoltar = true          -  grant arm permission to release
6. Wait until arm releases drone    (!ventosa.TieneObjeto)
7. TrasladarConPivotRotado / TrasladarEnL(zonaGiro)  -  return to pivot
8. GirarCarroSobrePunto(-anguloGiro) -  un-rotate
9. TrasladarA(puntoInicio)           -  return home
```

**Movement patterns**:

| Pattern | Description |
|---------|-------------|
| `Directo` | Smooth Lerp directly to target (diagonal if X and Z differ) |
| `EnL_XLuegoZ` | L-shaped: moves X axis first, then Z axis |

**Key fields**:
```csharp
public Transform puntoInicio;                  // Home position
public List<MovimientoPaletizado> movimientos; // One entry per drone
public Ventosa ventosa;                        // Reference to arm script
public float velocidadMovimiento = 1f;         // Translation speed (m/s)
public float duracionGiro = 0.5f;              // Rotation duration (s)
public float duracionTrasladoFinal = 0.5f;     // Translation duration (s)
public int totalDrones = 0;                    // Synced from Produccion.cs
```

---

### 7. Snap Mechanics

**Two approaches** depending on piece type:

| Method | Script | Trigger | Used For |
|--------|--------|---------|----------|
| **Proximity** | `Ensamble.cs` | `snapPorProximidad` + distance check | PCB, Tapa |
| **Trigger collision** | `EnsambleGri.cs` | `distanciaActivacion` | Motors, Hélices |

**Snap Animation** (Ensamble.cs):
```csharp
// Exponential smoothing in Update() — runs until distance < 0.001f
transform.position = Vector3.Lerp(
    transform.position,
    posicionFinal,
    Time.deltaTime * velocidadEncaje
);

transform.rotation = Quaternion.Lerp(
    transform.rotation,
    Quaternion.Euler(rotacionFinalEnsamble),
    Time.deltaTime * velocidadEncaje
);

if (Vector3.Distance(transform.position, posicionFinal) < 0.001f)
{
    transform.position = posicionFinal;
    transform.rotation = Quaternion.Euler(rotacionFinalEnsamble);
    // snap complete -> reparent + mark assembled
}
```

**Final assembly rotation** is configurable per piece:
```csharp
public Vector3 rotacionFinalEnsamble = new Vector3(-90f, 0f, 180f); // Ensamble.cs
public Vector3 rotacionForzada       = new Vector3(-90f, 0f, 0f);   // EnsambleGri.cs
```

---

### 8. Race Condition Prevention

**Problem**: `PlaySequence()` and `ReleaseInSequence()` ran in parallel.

**Solution: Boolean Semaphore** (in `Ventosa.cs`):
```csharp
private bool liberandoObjeto = false;

IEnumerator LiberarEnSecuencia() {
    liberandoObjeto = true;
    // ... lower to band or freeze animation
    LiberarObjeto();
    liberandoObjeto = false;
}

void ReproducirSecuencia() {
    if (liberandoObjeto) return; // block sequence until release completes
    // ... execute pose
}
```

---

### 9. Production Spawner (`Produccion.cs`)

Parts are not pre-placed in the scene  -  they are instantiated at runtime by `Produccion.cs` using individual `Spawner` components. **Boxes** (`spawnsCaja`) are spawned once in `Start()` before any assembly begins, naming them `CajaPrefab(Clone1)` through `CajaPrefab(Clone8)`. **Assembly parts** are spawned per-drone via `SecuenciaEnsamblaje()` with staggered delays (1 s for base/PCB/motor pairs, 2 s for hélice pairs and tapa).

```csharp
// Boxes are spawned once at Start(), before assembly begins
void Start() {
    for (int i = 0; i < spawnsCaja.Length; i++) {
        GameObject caja = spawnsCaja[i].Spawn();
        caja.name = "CajaPrefab(Clone" + (i + 1) + ")";
    }
    StartCoroutine(LoopProduccion());
}

// Per-drone assembly spawn sequence
IEnumerator SecuenciaEnsamblaje() {
    baseActual = spawnBase.Spawn();
    yield return new WaitForSeconds(1);
    spawnPCB.Spawn();
    yield return new WaitForSeconds(1);
    spawnMotor1.Spawn(); spawnMotor2.Spawn();
    yield return new WaitForSeconds(1);
    spawnMotor3.Spawn(); spawnMotor4.Spawn();
    yield return new WaitForSeconds(1);
    spawnHelice1.Spawn(); spawnHelice2.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice3.Spawn(); spawnHelice4.Spawn();
    yield return new WaitForSeconds(2);
    spawnTapa.Spawn();
    yield return new WaitForSeconds(2);
}
```

Each `Spawner` also auto-assigns `puntoEnsamble` (for `Ensamble`) and `baseParent` (for `EnsambleGri`) on the instantiated prefab. For box prefabs, it additionally wires the `HingeJoint.connectedBody` to the box's own `Rigidbody`, enabling the lid hinge to function correctly.

---

### 10. Box Lid Closure and Cart Retirement

Two scripts handle the final packaging step after drones are deposited into boxes.

**`CerradorTapa.cs`**  -  Animates the box lid from an open pose (`rotacionAbierta = Vector3(80,0,0)`) to a closed pose (`rotacionCerrada = Vector3.zero`) using a configurable `AnimationCurve` (ease in/out by default). Exposes a `tapaCerrada` flag that other scripts can poll. Also provides `AbrirTapaInstantaneo()` to reset the lid instantly.

```csharp
[ContextMenu("Cerrar tapa (animación)")]
public void CerrarTapa() {
    StartCoroutine(AnimarCierre());
}

private IEnumerator AnimarCierre() {
    float tiempo = 0f;
    while (tiempo < duracionCierre) {
        float tCurva = curva.Evaluate(tiempo / duracionCierre);
        tapa.localRotation = Quaternion.Slerp(rotInicio, rotFin, tCurva);
        tiempo += Time.deltaTime;
        yield return null;
    }
    tapaCerrada = true;
}
```

**`RetiradorCarro.cs`**  -  Once a cart is ready to leave the palletizing zone, this script reparents all assigned box GameObjects as children of the cart so they move with it. It waits (via coroutine) until `CerradorTapa.tapaCerrada` is `true` on the last box before adopting.

```csharp
public void IntentarAdoptarCajas() {
    CerradorTapa cerrador = cajaFinal.GetComponent<CerradorTapa>();
    if (!cerrador.tapaCerrada) {
        StartCoroutine(EsperarYAdoptar(cerrador));
        return;
    }
    AdoptarCajas();
}
```

After the drone is deposited, `CerradorTapa` closes the lid and then destroys the `BasePrefab(Clone)` inside the box to free memory. `RetiradorCarro` polls until the last box lid is closed before reparenting the boxes to the cart.

| Script | Trigger | Key Output |
|--------|---------|-----------|
| `CerradorTapa.cs` | `CerrarTapa()` call | `tapaCerrada = true`, drone GameObject destroyed |
| `RetiradorCarro.cs` | `IntentarAdoptarCajas()` call | boxes reparented to cart |

---

### 11. HMI Dashboard (`HmiManager.cs` + `CodesysTcpClient.cs`)

<div align="center">

![HMI Unity-CODESYS](docs/HMI%20UNITY-CODESYS.png)

</div>

`HmiManager.cs` provides a real-time HMI panel inside Unity using **TextMeshPro**. It auto-discovers the `CodesysTcpClient` and `Produccion` components on scene start and refreshes every frame.

**HMI panels:**

| Panel | Source | Description |
|-------|--------|-------------|
| TCP status indicator | `CodesysTcpClient.isConnected` | Green = connected, orange = disconnected |
| SISTEMA ON / OFF | `SISTEMA_ON` (5th byte of RX packet) | System state sent directly by CODESYS |
| NEUMATICA ON / OFF | `salidas_plc2 bit 5` (NEUMATICA_ON) | Pneumatics state from FluidSIM via OPC |
| Omega arm state | `Produccion.OmegaActivo` | ACTIVE / IDLE / MOVING |
| Paletizador arm state | `Produccion.PaletActivo` | HOLDING / IDLE / MOVING |
| LED panel (8 LEDs) | `salidas_plc1` + `salidas_plc2` | Box count 1–8 |
| Cycle timer | `Time.time − tiempoInicioDronActual` | MM:SS for current drone |
| TCP log | `CodesysTcpClient.OnLogMessage` | Rolling 10-line log |

**TCP protocol** (`CodesysTcpClient.cs`):
```
TX → CODESYS  [0xAA, TCP_COMANDOS_VENTOSAS, TCP_COMANDOS_LEDS]                    -  3 bytes, every 50 ms
RX ← CODESYS  [0xBB, salidas_plc1, salidas_plc2, entradas_plc1, SISTEMA_ON]      -  5 bytes, every 50 ms
```

The `CodesysTcpClient` runs dedicated send and receive background threads with automatic reconnect every `reconnectInterval` seconds (default 3 s). A connection-generation counter prevents stale threads from interfering after reconnects. Unity pauses (`Time.timeScale = 0`) whenever `salidas_plc2 & 0x10` (NEUMATICA_OFF) is set, which CODESYS asserts on STOP or EMERGENCIA.

---

### 12. Safety Vision System (`ESP32 CAM WEBSOCKET/ESP32CAM/Auxiliar System.py`)

An auxiliary Python script that receives a live MJPEG stream from an **ESP32-CAM** over WebSocket and runs **MediaPipe** hand detection on every frame. When a hand is detected inside the work area, it triggers a multi-channel safety alert.

> For a detailed breakdown of the code, configuration, and deployment instructions see the dedicated repository:
> **[esp32cam-hand-detection-safety-system](https://github.com/jorgefajardom-coder/esp32cam-hand-detection-safety-system.git)**

**Technology stack:**

| Library | Role |
|---------|------|
| `websockets` | Async WebSocket client to ESP32-CAM (default: `ws://192.168.1.3:81`) |
| `opencv-python` | Frame decode, visual overlay (`STOP` text + red rectangle) |
| `mediapipe` | Real-time hand landmark detection (up to 2 hands, 0.5 confidence) |
| `pyttsx3` | Text-to-speech alert  -  "Retire la mano" (Sabina voice, es-MX) |
| `smtplib` | Email alert via Gmail SMTP (TLS port 587) |
| `requests` | Telegram Bot API alert |
| `python-dotenv` | Credentials loaded from `.env` |

**Alert pipeline** (15-second cooldown between alerts):
```
Hand detected → voice TTS → email (SMTP) → Telegram message
```

**Environment variables** (`.env`):
```
EMAIL_ADDRESS, EMAIL_PASSWORD, TO_EMAIL
TELEGRAM_BOT_TOKEN, TELEGRAM_CHAT_ID
WEBSOCKET_URL  (default: ws://192.168.1.3:81)
```

---

### 13. STOP / EMERGENCIA Pause (`Produccion.cs`)

When FluidSIM sends a STOP or EMERGENCIA signal, CODESYS deasserts the pneumatic circuit (`NEUMATICA_OFF` → `salidas_plc2 bit 4`). Unity detects this and **freezes the entire simulation** as if the user pressed Pause.

**Implementation in `Produccion.cs`**:

```csharp
// Pause condition: connected + NEUMATICA_OFF bit set
public bool SistemaPausado =>
    tcp != null && tcp.isConnected && (tcp.salidas_plc2 & 0x10) != 0;

void Update()
{
    Time.timeScale = SistemaPausado ? 0f : 1f;  // Freeze/resume everything
    if (!simulacionActiva) return;
    tiempoTotalSimulacion += Time.deltaTime;     // Only counts while running
}

// All WaitUntil calls wrapped to also check for pause
IEnumerator Esperar(Func<bool> condicion)
{
    yield return new WaitUntil(() => !SistemaPausado && condicion());
}
```

**Behavior**:

| Condition | Effect |
|-----------|--------|
| `STOP` or `EMERGENCIA` = LOW in FluidSIM | `Time.timeScale = 0` — arms freeze, coroutines suspend |
| Signal restored | `Time.timeScale = 1` — simulation resumes from where it stopped |
| TCP disconnected | `SistemaPausado` returns `false` — simulation continues unpaused |

`Time.timeScale = 0` freezes all Unity physics and `WaitForSeconds` timers but does **not** affect background TCP threads, which continue running at OS level.

---

### 14. Statistical Analysis & OEE Report (`Html/Analisis estadistico y OEE FINAL.html`)

> Open `Html/Analisis estadistico y OEE FINAL.html` in any browser — self-contained, no server needed.  
> n = 100 cycles · 2026-04-28 · 60.37 min total session

#### OEE — Overall Equipment Effectiveness

```
 OEE  ████████████████████████░░░░  86.80 %   ✦ WORLD-CLASS  (≥ 85 %)
 AVL  ██████████████████████░░░░░░  88.18 %   Active / Planned time
 PRF  █████████████████████████░░░  98.43 %   Ideal cycle / Real cycle
 QLT  ████████████████████████████ 100.00 %   0 rejects / 100 produced
```

```mermaid
pie showData
    title OEE Loss Breakdown — 100 cycles
    "Productive time (OEE 86.80%)" : 86.80
    "Availability loss (11.82%)"   : 11.82
    "Performance loss (1.38%)"     : 1.38
```

#### Cycle Time — Statistical Control

```mermaid
xychart-beta
    title "Mean cycle time per arm (seconds)"
    x-axis ["Alpha", "Beta", "Omega", "Paletizador"]
    y-axis "seconds" 0 --> 35
    bar [25.27, 22.91, 29.50, 5.37]
```

| Arm | Mean (s) | σ (s) | CV | Notes |
|-----|:--------:|:-----:|:--:|-------|
| **Alpha** | 25.27 | 0.230 | 0.91% | Gripper — Base · Motors · Hélices |
| **Beta** | 22.91 | 0.274 | 1.20% | Gripper — Motors · Hélices |
| **Omega** | 29.50 | 0.264 | 0.89% | Suction — PCB · Tapa · transfer |
| **Paletizador** | 5.37 | 0.618 | 11.5% | Suction — bimodal ~4.6 s / ~6.0 s |

> CV < 5% on all arms except Paletizador (bimodal path) — process fully under statistical control.  
> μ = 32.00 s · σ = 0.298 s · UCL = 32.894 s · **0 / 100 points outside control limits**

The interactive HTML report covers 7 sections: descriptive statistics, per-arm breakdown, percentiles & CDF, correlation & trend, SPC control chart, consolidated summary, and full OEE analysis with rolling gauge and loss cascade.

---

## Project Structure

```
drone-packaging-simulation-unity/
├── docs/
│   └── simulation_overview.png           # Isometric view of the robotic assembly cell
├── CODESYS II/                           # PLC project (CODESYS runtime)
│   ├── CODESYS SIMULATION II.project     # Main CODESYS project file
│   ├── CODESYS SIMULATION II.Device.Application.*.bootinfo
│   ├── CODESYS SIMULATION II.Device.Application.*.compileinfo
│   ├── CODESYS SIMULATION II.Device.Application.xml
│   └── CODESYS SIMULATION II-*.opt       # User/machine options
├── Fluidsim/                             # OPC simulation for FluidSim
│   └── OPC SIMULATION FLUIDSIM.ct
├── Assets/
│   ├── Brazos.cs                    # Gripper arm  -  Alpha, Beta (643 lines)
│   ├── Ventosa.cs                   # Suction arm  -  Omega, Paletizador (857 lines)
│   ├── CarroPaletizador.cs          # Paletizador floor navigation  -  configurable movement list (317 lines)
│   ├── DronListo.cs                 # Unifies drone parts as single rigidbody, auto-detects completion (70 lines)
│   ├── Ensamble.cs                  # Snap logic for PCB / Tapa (157 lines)
│   ├── EnsambleGri.cs               # Snap logic for Motors / Hélices (150 lines)
│   ├── Spawner.cs                   # Instantiates prefabs and assigns assembly refs (40 lines)
│   ├── Produccion.cs                # Production loop  -  staggered spawner + cart swap (371 lines)
│   ├── CentrarBase.cs               # Centers Base via rb.MovePosition in FixedUpdate, stops on collision (91 lines)
│   ├── RetiradorCarro.cs            # Adopts boxes as children when cart retires (91 lines)
│   ├── CerradorTapa.cs              # Animated box lid closure with AnimationCurve (94 lines)
│   ├── GripperTrigger.cs            # OnTriggerEnter / OnTriggerExit → Brazos.NotifyObjectInside/Exit()
│   ├── SuctionTrigger.cs            # OnTriggerEnter / OnTriggerExit → Ventosa.NotifyObjectInside/Exit()
│   ├── CodesysTcpClient.cs          # TCP client  -  Unity ↔ CODESYS 3.5.15.40 (port 8888)
│   ├── HmiManager.cs                # HMI dashboard  -  TextMeshPro, arm states, LED indicators
│   ├── CV_1.renderTexture
│   ├── CV_5.renderTexture
│   ├── JSON_Generados/              # 4 pose JSON files  -  each arm reads its own
│   │   ├── Poses_Alpha.json
│   │   ├── Poses_Beta.json
│   │   ├── Poses_Omega.json
│   │   └── Poses_Palet.json
│   ├── StreamingAssets/             # Runtime copies of the 4 JSON files
│   │   ├── Poses_Alpha.json
│   │   ├── Poses_Beta.json
│   │   ├── Poses_Omega.json
│   │   └── Poses_Palet.json
│   ├── Materials/
│   │   ├── SafetyStripes.shader
│   │   └── SafetyStripesMat.mat
│   └── Scenes/
│       └── SampleScene.unity
├── Html/
│   └── Analisis estadistico y OEE FINAL.html  # Interactive statistical report — 100-cycle run, OEE 86.80%
├── ESP32 CAM WEBSOCKET/
│   └── ESP32CAM/
│       └── Auxiliar System.py       # Hand-detection safety system (MediaPipe + WebSocket)
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
- **CODESYS** (optional)  -  to run the PLC simulation layer

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

5. **CODESYS / FluidSim** (optional)
   - Open `CODESYS II/CODESYS SIMULATION II.project` with **CODESYS 3.5.15.40**
   - The OPC simulation file for FluidSim is located at `Fluidsim/OPC SIMULATION FLUIDSIM.ct`
   - Unity connects to CODESYS via TCP on `127.0.0.1:8888` (configurable in `CodesysTcpClient` Inspector)

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
bool isPieceToFreeze = ensambleScript != null && 
                       ensambleScript.congelarAlLiberar;

if (!isPieceToFreeze) {
    PosicionarSobreBanda(); // Only for PCB
}

// For Lid:
// snapPorProximidad = true
// congelarAlLiberar = true
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
// In EnsambleGri.cs  -  uses object name matching via ConfigurarRotacionPorNumero()
if (esHelice && usarRotacionPorNumero) {
    if (nombreLimpio.Contains("Helice1")) {
        rotacionForzada = new Vector3(90f, 0f, 0f);
    } else if (nombreLimpio.Contains("Helice2")) {
        rotacionForzada = new Vector3(-90f, 90f, 0f);
    } else if (nombreLimpio.Contains("Helice3")) {
        rotacionForzada = new Vector3(-90f, 180f, 0f);
    } else if (nombreLimpio.Contains("Helice4")) {
        rotacionForzada = new Vector3(90f, 270f, 0f);
    }
}
```

**Inspector Configuration**:
- `Es Helice`: ✅
- `Forzar Rotacion Absoluta`: ✅
- `Usar Rotacion Por Numero`: ✅ (auto-detects hélice number from prefab name)

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

## Bug Summary Table

| # | Issue | Severity | Status | Solution |
|---|-------|----------|--------|----------|
| 1 | Rotation flips on grip | 🔴 Critical | ✅ Resolved | Preserve world-space |
| 2 | Lid penetrates components | 🔴 Critical | ✅ Resolved | Kinematic + collision layers |
| 3 | Sequence race condition | 🟡 High | ✅ Resolved | Semaphore flag |
| 4 | Propeller rotation | 🟡 High | ✅ Resolved | Absolute rotation by number |
| 5 | Movement stuttering | 🟢 Medium | ✅ Resolved | Correct t accumulation |
| 6 | TCP reconnect fails after first disconnect | 🔴 Critical | ✅ Resolved | Connection-generation counter in `CodesysTcpClient` |
| 7 | CODESYS blocks on `SysSockRecv` — no new connections | 🔴 Critical | ✅ Resolved | `SysSockSelect` non-blocking check before each recv |

---

## Robotic Arm Hierarchy

```
BrazoBase (fixed)
└── Waist (Revolute  -  X Drive)
    └── Arm01 (Revolute  -  Z Drive)
        └── Arm02 (Revolute  -  Z Drive)
            └── Arm03 (Revolute  -  X Drive)
                └── GripperAssembly (Revolute  -  Z Drive)
                    ├── Gear1 (Prismatic  -  X Drive, open/close)
                    └── Gear2 (Prismatic  -  X Drive, mirror)
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
| **stiffness** | Joint rigidity  -  higher values produce firmer response |
| **damping** | Oscillation attenuation |
| **forceLimit** | Maximum applicable force |
| **target** | Target position or rotation value |

### Tags

```
"Pickable"  -  All graspable drone parts (Base, PCB, Motors, Hélices, Tapa)
```

> **Note**: All graspable parts share the single `"Pickable"` tag. The scripts differentiate piece types by their attached component (`Ensamble` for suction pieces, `EnsambleGri` for gripper pieces) and by prefab name matching.

---

## Authors

**Jorge Andres Fajardo Mora**  
**Laura Vanesa Castro Sierra**

---

## License and Rights

**Copyright © 2025 Jorge Andres Fajardo Mora and Laura Vanesa Castro Sierra. All rights reserved.**

This repository and all its contents  -  including but not limited to source code, scripts, configuration files, data files, and documentation  -  are provided for **read-only and reference purposes only**.

**No permission is granted** to copy, modify, distribute, sublicense, or use any part of this project for commercial or non-commercial purposes without **explicit written authorization** from the authors.

**Unauthorized reproduction or redistribution** of this work, in whole or in part, is **strictly prohibited**.

---
---
---

# Simulación de Empaquetado de Dron  -  Unity

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
> *Vista isométrica de la celda robótica de ensamblaje  -  4 brazos articulados (Alpha, Beta, Omega, Paletizador) con ruedas mecanum.*

<br/>

[![Video Demo](https://img.shields.io/badge/▶_Video_Demo-YouTube-red?style=for-the-badge&logo=youtube)](https://youtu.be/U491eei--Xc?si=DweGneszA-7RkUbz)

> *Corrida completa de la simulación — ensamblaje, paletizado y swap de carros con integración CODESYS & FluidSIM.*

</div>

---

## Tabla de Contenidos

- [Descripción General](#descripción-general)
- [Stack Técnico](#stack-técnico)
- [Integración CODESYS & FluidSIM](#integración-codesys--fluidsim)
  - [Versiones de Herramientas](#versiones-de-herramientas)
  - [Arquitectura de Comunicación](#arquitectura-de-comunicación)
  - [Mapa de Variables](#mapa-de-variables)
  - [Lógica de Seguridad y Control](#lógica-de-seguridad-y-control)
  - [Programa PLC CODESYS](#programa-plc-codesys-plc_prg)
  - [Simulación Neumática FluidSIM](#simulación-neumática-fluidsim)
- [Arquitectura del Sistema](#arquitectura-del-sistema)
- [Sistemas Implementados](#sistemas-implementados)
  - [1. Sistema Gripper](#1-sistema-gripper-brazoscs)
  - [2. Sistema Ventosa](#2-sistema-de-ventosa-ventosacs)
  - [3. Secuenciador de Movimiento JSON](#3-secuenciador-de-movimiento-json)
  - [4. Arquitectura de Movimiento Descentralizado](#4-arquitectura-de-movimiento-descentralizado)
  - [5. Unificación de Dron](#5-unificación-de-dron-dronlistocs)
  - [6. Navegación del Paletizador](#6-navegación-del-paletizador-carropaletizadords)
  - [7. Mecánicas de Snap](#7-mecánicas-de-snap)
  - [8. Prevención de Race Condition](#8-prevención-de-race-condition)
  - [9. Spawner de Producción](#9-spawner-de-producción-produccioncs)
  - [10. Cierre de Tapa y Retiro de Carro](#10-cierre-de-tapa-y-retiro-de-carro)
  - [11. HMI Dashboard](#11-hmi-dashboard-hmimanagercs--codesystcpclientcs-1)
  - [12. Sistema de Visión de Seguridad](#12-sistema-de-visión-de-seguridad)
  - [13. Pausa por STOP / EMERGENCIA](#13-pausa-por-stop--emergencia-produccioncs)
  - [14. Informe Estadístico y OEE](#14-informe-de-análisis-estadístico-y-oee-htmlanalisis-estadistico-y-oee-finalhtml)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Instalación](#instalación)
- [Problemas Resueltos](#problemas-resueltos)
- [Autores](#autores)
- [Licencia](#licencia-y-derechos)

> **14 scripts C# · 1 script Python · 4 archivos JSON de poses (duplicados en StreamingAssets/)**

---

## Descripción General

Este proyecto es una **simulación basada en Unity** de una celda robótica de ensamblaje y paletizado de drones. Cuatro brazos robóticos colaboran para ensamblar un dron mediante interacciones físicas realistas y secuencias de movimiento impulsadas por JSON. El dron completado es luego transportado y paletizado en carros de producción por un cuarto brazo que se desplaza autónomamente con **ruedas mecanum**.

La simulación está orientada a la **validación virtual de procesos** de celdas robóticas industriales. Se integra con un **proyecto PLC de CODESYS** y una **simulación OPC de FluidSim** que modelan la capa de automatización industrial.

### Características Clave

- 🦾 **Cuatro brazos robóticos coordinados** (Alpha, Beta, Omega, Paletizador) con física ArticulationBody
- 🔄 **Movimiento descentralizado impulsado por JSON**  -  cada brazo lee su propio archivo de poses y se ejecuta de forma independiente
- ⚙️ **Efectores finales duales**: Gripper (`Brazos.cs`) y Ventosa (`Ventosa.cs`)
- 🎯 **Sistema de snap por proximidad** para ensamblaje de componentes
- 📊 **Ejecución asíncrona basada en coroutines** con gestión de dependencias
- 🔧 **Preservación de world-space** para prevenir artefactos de rotación
- 🏭 **Spawner de producción** con instanciación escalonada de piezas y cajas
- 📦 **Paletizador**  -  brazo con ruedas mecanum + sistema de navegación `CarroPaletizador.cs`
- 🚁 **DronListo.cs**  -  unifica todas las piezas del dron en una sola unidad rígida antes del levantamiento
- 🖥️ **Integración CODESYS / FluidSim**  -  archivos de simulación PLC y OPC incluidos en el repositorio
- 📡 **HMI en tiempo real** (`HmiManager.cs`)  -  panel interno en Unity con TextMeshPro; estados de brazos, panel LED, log TCP, cronómetro de ciclo
- 📷 **Sistema de visión de seguridad** (`Auxiliar System.py`)  -  stream ESP32-CAM por WebSocket, detección de manos con MediaPipe, alertas por voz/correo/Telegram

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
| `com.unity.feature.development` | 1.0.1 | Paquete de herramientas de desarrollo |
| `com.unity.ide.rider` | 3.0.31 | Integración con Rider IDE |
| `com.unity.ide.visualstudio` | 2.0.22 | Integración con Visual Studio |
| `com.unity.ide.vscode` | 1.2.5 | Integración con VS Code |
| `com.unity.ugui` | 1.0.0 | Sistema de UI legacy |

---

## Integración CODESYS & FluidSIM

### Versiones de Herramientas

| Herramienta | Versión | Fabricante |
|-------------|---------|-----------|
| **CODESYS** | 3.5.15.40 (V3.5 SP9 P1) | 3S-Smart Software Solutions GmbH |
| **FluidSIM** | 4.2p / 1.67 Neumática (19.02.2010) | Festo Didactic GmbH & Co. KG |

#### Librerías CODESYS Requeridas

| Librería | Versión | Propósito |
|----------|---------|-----------|
| **SysSocket** | 3.5.15.0 | Comunicación TCP por socket (Unity ↔ CODESYS) |
| **SysSocket Interfaces** | 3.5.15.0 | Definiciones de interfaz para SysSocket |
| **SysTypes2 Interfaces** | 3.5.15.0 | Definiciones de tipos primitivos usadas por las librerías Sys |

Agregar vía **Herramientas → Library Manager → Agregar librería**.

---

### Arquitectura de Comunicación

Las tres capas se comunican mediante dos protocolos: **TCP/IP** (Unity ↔ CODESYS) y **OPC DA** (CODESYS ↔ FluidSIM).

```mermaid
graph LR
    subgraph UNITY["Unity  ·  CodesysTcpClient.cs"]
        direction TB
        UA["Ventosa.cs · CarroPaletizador.cs"]
        UTCP["TX  0xAA · VENTOSAS · LEDS  3 bytes / 50 ms\nRX  0xBB · plc1 · plc2 · entradas · SISTEMA_ON  5 bytes / 50 ms"]
        UA --> UTCP
    end
    subgraph PLC["CODESYS V3.5 SP9 P1  ·  puerto 8888"]
        direction TB
        PA["PLC_PRG\nSysSockSelect no bloqueante"]
        PB["salidas_plc1\nsolenoides ventosa · LED5–8"]
        PC["salidas_plc2\nLED1–4 · NEUMATICA"]
        PA --> PB
        PA --> PC
    end
    subgraph OPC["FluidSIM 4.2p  ·  OPC DA"]
        direction TB
        FA["Circuito neumático\nNEUMATICA_ON / OFF"]
        FB["Entradas físicas\nBP1 · BP2 · START · STOP · EMERG"]
    end
    UTCP <-->|"TCP/IP · puerto 8888"| PA
    PB -->|"OPC"| FA
    PC -->|"OPC"| FA
    FB -->|"OPC"| PA
    style UNITY fill:#1a3a5c,color:#fff,stroke:#4a6fa5
    style PLC   fill:#8b0000,color:#fff,stroke:#c0392b
    style OPC   fill:#1a5c2a,color:#fff,stroke:#27ae60
```

---

### Mapa de Variables

#### Variables TCP (Unity → CODESYS)

| Variable | Tipo | Bits | Descripción |
|----------|------|------|-------------|
| `TCP_COMANDOS_VENTOSAS` | `BYTE` | Bit 0 = Omega · Bit 1 = Paletizador | Comandos ON/OFF de ventosas enviados desde Unity por TCP |
| `TCP_COMANDOS_LEDS` | `BYTE` | Bits 0–7 = LED1–LED8 | Estado del panel LED enviado desde Unity por TCP |

#### Entradas Físicas (`entradas_plc1` byte  -  máscara de bits)

| Bit | Máscara | Variable | Descripción |
|-----|---------|----------|-------------|
| 0 | `16#01` | `BP1` | Pulsador 1 (confirmación ventosa Omega) |
| 1 | `16#02` | `BP2` | Pulsador 2 (confirmación ventosa Paletizador) |
| 2 | `16#04` | `START` | Comando de inicio (flanco ascendente) |
| 3 | `16#08` | `STOP` | Parada (NC  -  debe estar en HIGH para funcionar) |
| 4 | `16#10` | `EMERGENCIA` | Parada de emergencia (NC  -  debe estar en HIGH para funcionar) |

#### Módulo de Salida 1 (`salidas_plc1` byte)

| Bit | Máscara | Señal | Descripción |
|-----|---------|-------|-------------|
| 0 | `16#01` | `VENTOSA_OMEGA_ON` | Activar succión Omega |
| 1 | `16#02` | `VENTOSA_OMEGA_OFF` | Desactivar succión Omega |
| 2 | `16#04` | `VENTOSA_PALETIZADOR_ON` | Activar succión Paletizador |
| 3 | `16#08` | `VENTOSA_PALETIZADOR_OFF` | Desactivar succión Paletizador |
| 4 | `16#10` | `LED7` | LED 7 |
| 5 | `16#20` | `LED8` | LED 8 |
| 6 | `16#40` | `LED5` | LED 5 |
| 7 | `16#80` | `LED6` | LED 6 |

#### Módulo de Salida 2 (`salidas_plc2` byte)

| Bit | Máscara | Señal | Descripción |
|-----|---------|-------|-------------|
| 0 | `16#01` | `LED2` | LED 2 |
| 1 | `16#02` | `LED1` | LED 1 |
| 2 | `16#04` | `LED4` | LED 4 |
| 3 | `16#08` | `LED3` | LED 3 |
| 4 | `16#10` | `NEUMATICA_OFF` | Neumática apagada → FluidSIM vía OPC |
| 5 | `16#20` | `NEUMATICA_ON` | Neumática encendida → FluidSIM vía OPC |

#### Mapeo de Módulos I/O FluidSIM

"FluidSIM In" significa que CODESYS **escribe** hacia FluidSIM (comandos de actuadores); "FluidSIM Out" significa que FluidSIM **envía** a CODESYS (retroalimentación de sensores).

| Módulo | Puerto | Dirección | Variable CODESYS | Elementos Físicos |
|--------|--------|-----------|-----------------|-------------------|
| **Módulo 1** | Port 1 | FluidSIM In  -  CODESYS → FluidSIM | `salidas_plc1` (8 bits) | Solenoides 1M1/1M2 (Omega) · 2M1/2M2 (Paletizador) · LEDs Carro 2 (LED5–8) |
| **Módulo 2** | Port 1 | FluidSIM Out  -  FluidSIM → CODESYS | `entradas_plc1` (8 bits) | Pulsadores BP1 · BP2 · START · STOP · EMERGENCIA |
| **Módulo 3** | Port 1 | FluidSIM In  -  CODESYS → FluidSIM | `salidas_plc2` (8 bits) | Válvula principal 3M1/3M2 (neumática) · LEDs Carro 1 (LED1–4) |

---

### Lógica de Seguridad y Control

```mermaid
stateDiagram-v2
    [*] --> SISTEMA_OFF : encendido

    SISTEMA_OFF --> SISTEMA_ON : START↑ + STOP=1 + EMERGENCIA=1
    SISTEMA_ON --> SISTEMA_OFF : STOP=0 O EMERGENCIA=0

    state SISTEMA_ON {
        [*] --> LED_TEST
        LED_TEST --> NEUMATICA_ON : TON 1s expirado
        NEUMATICA_ON --> VENTOSAS_ON : bit TCP_COMANDOS_VENTOSAS activo
        VENTOSAS_ON --> NEUMATICA_ON : bit limpiado
    }
```

| Condición | Efecto |
|-----------|--------|
| Flanco `START` + `STOP` + `EMERGENCIA` = HIGH | `SISTEMA_ON := TRUE` + test LED de 1 s |
| `STOP` = LOW **o** `EMERGENCIA` = LOW | `SISTEMA_ON := FALSE` inmediatamente |
| `SISTEMA_ON` | `NEUMATICA_ON` → FluidSIM activa el circuito neumático |
| Bit 0 de `TCP_COMANDOS_VENTOSAS` | `VENTOSA_OMEGA_ON` → Módulo 1 bit 0 |
| Bit 1 de `TCP_COMANDOS_VENTOSAS` | `VENTOSA_PALETIZADOR_ON` → Módulo 1 bit 2 |
| Bits 0–7 de `TCP_COMANDOS_LEDS` | `LED1–LED8` → Módulos 1 y 2 |

---

### Programa PLC CODESYS (`PLC_PRG`)

`PLC_PRG` es el programa en Texto Estructurado que actúa como núcleo de automatización de la celda. Sus responsabilidades principales son:

- **Servidor TCP en el puerto 8888** — acepta un único cliente Unity, recibe paquetes de 3 bytes (`0xAA · VENTOSAS · LEDS`) y devuelve paquetes de estado de 5 bytes (`0xBB · plc1 · plc2 · entradas · SISTEMA_ON`) cada 50 ms.
- **Lógica de seguridad** — monitorea las entradas `STOP` y `EMERGENCIA`; cualquier señal LOW pone `SISTEMA_ON := FALSE` y corta la neumática de inmediato. El sistema solo arranca con un flanco ascendente de `START` y ambas señales en HIGH.
- **Control de actuadores** — decodifica los bits de `TCP_COMANDOS_VENTOSAS` para accionar las electroválvulas de las dos ventosas (Omega y Paletizador), y los bits de `TCP_COMANDOS_LEDS` para los 8 indicadores LED vía OPC DA.
- **Puente OPC DA** — publica `salidas_plc1/2` y lee `entradas_plc1` a través del servidor OPC de FluidSIM, sincronizando el circuito neumático virtual con Unity en tiempo real.

**Ventajas de diseño clave:**

| Técnica | Ventaja |
|---------|---------|
| `SysSockSelect` con timeout cero | I/O de socket no bloqueante — el ciclo PLC nunca se congela |
| Heartbeat `tSendTimer` de 50 ms | Tasa de actualización determinista; el hilo Unity retorna limpiamente |
| Watchdog `tNoDataTimer` de 3 s | Desconexión automática si Unity se cierra sin cerrar el socket |
| `nBytesReceived <= 0` para desconexión | Maneja cierre gracioso (0) y RST/error (−1) |
| Parámetros posicionales en `SysSock*` | Requerido por CODESYS V3.5 SP9 P1 — parámetros nombrados no soportados |

---

### Simulación Neumática FluidSIM

FluidSIM 4.2p (Festo Didactic, build 19.02.2010) simula el circuito neumático completo de la celda. Recibe comandos de actuadores de CODESYS a través del **servidor OPC DA** y devuelve retroalimentación de sensores (estados de pulsadores, confirmaciones de presión) a través del módulo FluidSIM Out.

**Componentes neumáticos del circuito:**

| Elemento | Tipo | Función |
|----------|------|---------|
| `1M1` / `1M2` | Válvula solenoide 5/2 | Ventosa Omega (activar / desactivar) |
| `2M1` / `2M2` | Válvula solenoide 5/2 | Ventosa Paletizador (activar / desactivar) |
| `3M1` / `3M2` | Válvula solenoide 5/2 | Suministro neumático principal (sistema ON / OFF) |
| `1BP1` | Sensor de presión/proximidad | Confirmación agarre ventosa Omega → bit `BP1` |
| `1BP2` | Sensor de presión/proximidad | Confirmación agarre ventosa Paletizador → bit `BP2` |
| Panel LED Carro 1 | Indicador | 4 LEDs controlados por `LED1–LED4` (salidas_plc2) |
| Panel LED Carro 2 | Indicador | 4 LEDs controlados por `LED5–LED8` (salidas_plc1) |

```mermaid
graph LR
    subgraph CMD["Salidas CODESYS"]
        direction TB
        PLC1["salidas_plc1\nbits 0–3 · solenoides ventosa\nbits 4–7 · LED5-8"]
        PLC2["salidas_plc2\nbits 0–3 · LED1-4\nbits 4–5 · NEUMATICA"]
    end

    subgraph ACT["FluidSIM In — Actuadores"]
        direction TB
        V1["1M1/1M2 · válvula 5/2\nVentosa Omega"]
        V2["2M1/2M2 · válvula 5/2\nVentosa Paletizador"]
        V3["3M1/3M2 · válvula 5/2\nSuministro neumático"]
        LC["Paneles LED\nLED1–4 · LED5–8"]
    end

    subgraph SENS["FluidSIM Out — Retroalimentación → entradas_plc1"]
        direction TB
        S1["1BP1 · confirmación agarre Omega\n→ BP1 bit 0"]
        S2["1BP2 · confirmación agarre Paletizador\n→ BP2 bit 1"]
    end

    PLC1 -->|"bits 0–1"| V1
    PLC1 -->|"bits 2–3"| V2
    PLC1 -->|"bits 4–7"| LC
    PLC2 -->|"bits 4–5"| V3
    PLC2 -->|"bits 0–3"| LC
    V1 -->|"sensor de presión"| S1
    V2 -->|"sensor de presión"| S2

    style CMD  fill:#8b0000,color:#fff,stroke:#c0392b
    style ACT  fill:#1a5c2a,color:#fff,stroke:#27ae60
    style SENS fill:#1a3a5c,color:#fff,stroke:#4a6fa5
```

> **Nota:** Los pulsadores físicos START, STOP y EMERGENCIA (bits 2–4 de entradas\_plc1) están cableados directamente al módulo de entradas de FluidSIM y no son parte del circuito neumático representado arriba.

---

## Arquitectura del Sistema

### Diagrama de Componentes

```mermaid
graph TB
    subgraph DATA["Datos y Spawn"]
        direction LR
        JF[(StreamingAssets · 8 archivos de poses)]
        SP["Spawners · piezas + cajas"]
        P["Produccion.cs"] -->|spawn escalonado| SP
    end

    subgraph CELL["Celda de Ensamblaje  ·  lee poses JSON"]
        direction LR
        B1["Alpha · Brazos\nBase · Motores ×2 · Hélices ×2"]
        B2["Beta · Brazos\nMotores ×2 · Hélices ×2"]
        B3["Omega · Ventosa\nPCB · Tapa · transferencia dron"]
    end

    subgraph PAL["Paletizado"]
        direction LR
        CARRO["CarroPaletizador.cs"]
        B4["Paletizador · Ventosa\nruedas mecanum"]
        CARRO --> B4
        B4 --> C1["Carro 1"]
        B4 -.-> C2["Carro 2"]
    end

    subgraph AUTO["Automatización Industrial"]
        direction LR
        PLC["CODESYS 3.5.15.40"]
        OPC["FluidSIM 4.2p\nNeumática · OPC DA"]
        HW["BP1 · BP2 · START · STOP · EMERG\nentradas_plc1"]
        PLC -->|"salidas_plc1/2 · OPC DA"| OPC
        HW -->|"FluidSIM Out"| PLC
    end

    DATA --> CELL
    JF -. poses .-> CELL
    JF -. poses .-> PAL
    SP -->|piezas| CELL
    B3 -->|"transferencia dron"| B4
    B3 & B4 -->|"TCP/IP · puerto 8888"| PLC

    style B1   fill:#1D9E75,stroke:#085041,color:#fff
    style B2   fill:#1D9E75,stroke:#085041,color:#fff
    style B3   fill:#378ADD,stroke:#042C53,color:#fff
    style B4   fill:#B75A34,stroke:#5C2506,color:#fff
    style CARRO fill:#534AB7,stroke:#26215C,color:#fff
    style PLC  fill:#8b0000,color:#fff,stroke:#8b0000
    style OPC  fill:#1a5c2a,color:#fff,stroke:#1a5c2a
    style HW   fill:#4a4a4a,color:#fff,stroke:#4a4a4a
```

### Configuración de Brazos

| Brazo | Clase | Efector Final | Estado | Componentes Manejados |
|-------|-------|--------------|--------|----------------------|
| **Alpha** | `Brazos.cs` | Gripper (pinza) | ✅ Implementado | Base, Motores ×2, Hélices ×2 |
| **Beta** | `Brazos.cs` | Gripper (pinza) | ✅ Implementado | Motores ×2, Hélices ×2 |
| **Omega** | `Ventosa.cs` | Ventosa (succión) | ✅ Implementado | PCB, Tapa, transferencia del dron |
| **Paletizador** | `Ventosa.cs` + ruedas mecanum | Ventosa (succión) | ✅ Implementado | Drones completados → Carro 1 / Carro 2 |

### Flujo de Secuencia de Ensamblaje

```mermaid
sequenceDiagram
    participant P as Produccion
    participant A as Alpha (Brazos)
    participant B as Beta (Brazos)
    participant W as Omega (Ventosa)
    participant PAL as Paletizador
    participant D as Dron

    rect rgb(30, 80, 60)
        Note over P,D: Fase 1  -  Spawn
        Note over P: Cajas pre-spawneadas en Start() antes del ensamblaje
        P->>D: Base → 1s → PCB → 1s → Motores 1+2 → 1s → Motores 3+4 → 1s → Hélices 1+2 → 2s → Hélices 3+4 → 2s → Tapa
    end

    rect rgb(25, 60, 90)
        Note over A,D: Fase 2  -  Ensamblaje (brazos arrancan concurrentemente, cada uno lee su JSON)
        A->>D: agarre Base → CentrarBase.IniciarCentrado()
        W->>D: ventosa + snap PCB

        par Alpha ensambla motores y hélices
            A->>D: snap Motor 1, Motor 2, Hélice 1, Hélice 2
        and Beta ensambla motores y hélices
            B->>D: snap Motor 3, Motor 4, Hélice 3, Hélice 4
        end

        W->>D: ventosa + snap Tapa
        Note over D: DronListo auto-detecta piezasEsperadas=10 → sella el dron
        W->>PAL: transfiere dron sellado a zona de paletizado
    end

    rect rgb(120, 60, 20)
        Note over PAL: Fase 3  -  Paletizado (bucle EjecutarSecuencia)
        loop Para cada MovimientoPaletizado en la lista movimientos
            PAL->>D: recoge dron (ventosa.TieneObjeto)
            PAL->>PAL: TrasladarA(zonaGiro) → GirarCarroSobrePunto(anguloGiro)
            PAL->>PAL: TrasladarConPivotRotado / TrasladarEnL(puntoDestino)
            PAL->>D: permisoParaSoltar=true → dron depositado en caja
            PAL->>PAL: regresa a zonaGiro → GirarCarroSobrePunto(-anguloGiro)
            PAL->>PAL: TrasladarA(puntoInicio)
        end
    end
```

### Diagrama de Interacción de Scripts

```mermaid
classDiagram
    direction LR

    class Brazos {
        +ArticulationBody Waist, Arm01, Arm02, Arm03
        +ArticulationBody GripperAssembly, Gear1, Gear2
        +List~RobotPose~ poses
        +bool jugandoSecuencia
        +bool secuenciaTerminada
        +string saveFileName
        +float speed
        +float tiempoEsperaInicial
        +CentrarBase scriptCentrarBase
        +IniciarSecuencia()
        +IniciarSecuenciaConEspera()
        +ResetCompleto()
        +LoadFromFile()
        +SaveToFile()
        +NotifyObjectInside(GameObject)
        +NotifyObjectExit(GameObject)
        -AgarrarObjeto()
        -LiberarObjeto()
    }

    class Ventosa {
        +ArticulationBody Waist, Arm01, Arm02, Arm03, GripperAssembly
        +bool suctionActive
        +float suctionForce
        +float speed
        +List~VentosaPose~ poses
        +bool jugandoSecuencia
        +bool secuenciaTerminada
        +bool dronDepositado
        +bool permisoParaSoltar
        +bool esperarPermisoParaSoltar
        +bool usarSecuenciaDeCajas
        +int[] ordenCajas
        +bool emparentarACaja
        +float delayCierreTapa
        +bool TieneObjeto
        +Transform puntoDestinoDron
        +Vector3 rotacionFijaAlAgarrar
        +IniciarSecuencia()
        +IniciarSecuenciaConEspera()
        +ResetCompleto()
        +ReiniciarContadorDrones()
        +LoadFromFile()
        +SaveToFile()
        +NotifyObjectInside(GameObject)
        +NotifyObjectExit(GameObject)
        +LiberarObjeto()
        -AgarrarObjetoConSuccion()
        -LiberarEnSecuencia()
    }

    class CarroPaletizador {
        +Transform puntoInicio
        +List~MovimientoPaletizado~ movimientos
        +Ventosa ventosa
        +ArticulationBody articulacionRaiz
        +float velocidadMovimiento
        +float duracionGiro
        +float duracionTrasladoFinal
        +float delayDespuesSoltar
        +int totalDrones
        +IniciarSecuenciaCarro()
        -EjecutarSecuencia()
        -TrasladarA(Transform, Transform)
        -TrasladarConPivotRotado(Transform, Transform)
        -TrasladarEnL(Transform, Transform, bool)
        -GirarCarroSobrePunto(float, Transform)
        -ActualizarPosicionBrazo()
    }

    class CodesysTcpClient {
        +string codesysIP
        +int codesysPort
        +float reconnectInterval
        +bool isConnected
        +byte TCP_COMANDOS_VENTOSAS
        +byte TCP_COMANDOS_LEDS
        +byte salidas_plc1, salidas_plc2, entradas_plc1
        +bool VENTOSA_OMEGA_ON, VENTOSA_OMEGA_OFF
        +bool VENTOSA_PALET_ON, VENTOSA_PALET_OFF
        +bool LED1..LED8
        +bool NEUMATICA_ON, NEUMATICA_OFF
        +bool SISTEMA_ON
        +event OnLogMessage
        +SetVentosaOmega(bool)
        +SetVentosaPaletizador(bool)
        +SetLed(int, bool)
        +SetAllLeds(bool)
    }

    class HmiManager {
        +CodesysTcpClient tcp
        +Produccion produccion
        +Image imgSistema, imgNeumatica, imgTcpEstado
        +Image imgOmegaEstado, imgPaletEstado
        +Image[] imgLeds
        +Color colorOn, colorOff, colorWarn, colorAct
        -RefreshConexion()
        -RefreshBrazos()
        -RefreshCajas()
        -RefreshPLC()
        -RefreshMetricas()
        +AddLog(string)
    }

    class DronListo {
        +bool dronesListo
        +int piezasEsperadas
        +PrepararParaLevantamiento()
        +SoltarDron()
        -ContarPiezasEnsambladas()
    }

    class EnsambleGri {
        +Transform puntoEnsamble
        +Transform baseParent
        +float distanciaActivacion
        +bool esHelice
        +bool forzarRotacionAbsoluta
        +bool usarRotacionPorNumero
        +Vector3 rotacionForzada
        +NotificarLiberad(Collider[])
        -ConfigurarRotacionPorNumero(string)
        -IniciarEncaje()
    }

    class Ensamble {
        +Transform puntoEnsamble
        +float offsetHundimiento
        +float velocidadEncaje
        +bool snapPorProximidad
        +float distanciaActivacionSnap
        +bool congelarAlLiberar
        +bool yaEnsamblado
        +Vector3 rotacionFinalEnsamble
        +AsignarBase(Transform)
        +NotificarLiberad()
        -IniciarEncaje()
    }

    class Produccion {
        +Spawner spawnBase, spawnPCB
        +Spawner spawnMotor1..4
        +Spawner spawnHelice1..4
        +Spawner spawnTapa
        +Spawner[] spawnsCaja
        +Brazos brazoAlpha, brazoBeta
        +Ventosa brazoOmega, brazoPaletizador
        +CodesysTcpClient tcp
        +int dronesAProducir
        +bool OmegaActivo
        +bool PaletActivo
        +string CarroActualTag
        +float TiempoCicloActual
        +IEnumerator LoopProduccion()
        +IEnumerator SecuenciaEnsamblaje()
        -IEnumerator SwapCarro()
    }

    class Spawner {
        +GameObject prefab
        +Transform puntoEnsamble
        +Transform baseParent
        +GameObject Spawn()
    }

    class CentrarBase {
        +Transform puntoDestino
        +Vector3 rotacionFija
        +IniciarCentrado()
    }

    class CerradorTapa {
        +Transform tapa
        +Vector3 rotacionAbierta
        +Vector3 rotacionCerrada
        +float duracionCierre
        +AnimationCurve curva
        +bool tapaCerrada
        +CerrarTapa()
        +AbrirTapaInstantaneo()
    }

    class RetiradorCarro {
        +int[] cajasAsignadas
        +int numeroCajaFinal
        +bool cajasAdoptadas
        +IntentarAdoptarCajas()
        +AdoptarCajas()
    }

    class GripperTrigger {
        +Brazos mainScript
    }

    class SuctionTrigger {
        +Ventosa mainScript
    }

    CarroPaletizador --> Ventosa : conduce paletizador
    CarroPaletizador --> RetiradorCarro : activa adopción de cajas
    GripperTrigger --> Brazos : OnTriggerEnter / OnTriggerExit
    SuctionTrigger --> Ventosa : OnTriggerEnter / OnTriggerExit
    Brazos --> EnsambleGri : snap al soltar
    Ventosa --> Ensamble : snap al soltar
    Ventosa --> CerradorTapa : activa cierre de tapa al depositar
    Brazos --> CentrarBase : centra Base al soltar
    Produccion --> Spawner : gestiona
    Produccion --> CodesysTcpClient : lee compuerta SISTEMA_ON
    Spawner ..> EnsambleGri : asigna baseParent
    Spawner ..> Ensamble : asigna puntoEnsamble
    DronListo ..> Ventosa : flag dronesListo leído por Omega
    HmiManager --> CodesysTcpClient : monitorea TCP + estado PLC
    HmiManager --> Produccion : lee estado brazos/producción
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

**Campos específicos del Paletizador** (solo activos cuando el brazo es el Paletizador):

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `usarSecuenciaDeCajas` | `bool` | Recorre las cajas en el orden de `ordenCajas[]` |
| `ordenCajas` | `int[]` | Secuencia de índices de caja para el depósito de drones |
| `emparentarACaja` | `bool` | Si es true, reparentea el dron a la caja tras el depósito |
| `delayCierreTapa` | `float` | Segundos de espera antes de llamar a `CerradorTapa.CerrarTapa()` (por defecto 1 s) |

**Ventajas**:
- Feedback visual claro para el usuario
- Rotación fija al agarrar configurable (`rotacionFijaAlAgarrar`)
- Transición suave sin teletransporte

---

### 3. Secuenciador JSON

Los movimientos de cada brazo se definen en archivos JSON externos en `Assets/JSON_Generados/` y se cargan en tiempo de ejecución. Cada archivo almacena una lista de objetos `RobotPose` con todos los targets de articulación.

**Estructuras de datos reales de pose**:

`RobotPose` (Brazos - Alpha, Beta):
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

`VentosaPose` (Ventosa - Omega, Paletizador):
```json
{
  "poses": [
    {
      "waist": 180.0,
      "arm01": 65.0,
      "arm02": -10.0,
      "arm03": 0.0,
      "gripperAssembly": 63.0,
      "suctionActive": false,
      "delay": 0.0
    }
  ]
}
```

**Archivos JSON disponibles** (4 archivos únicos  -  cada brazo carga su secuencia consolidada):

| Archivo | Brazo | Poses | Descripción |
|---------|-------|-------|-------------|
| `Poses_Alpha.json` | Alpha | 29 | Secuencia completa Alpha: base, motores 1 y 2, hélices 1 y 2 |
| `Poses_Beta.json` | Beta | 24 | Secuencia completa Beta: motores 3 y 4, hélices 3 y 4 |
| `Poses_Omega.json` | Omega | 18 | Secuencia completa Omega: PCB, tapa y transferencia del dron |
| `Poses_Palet.json` | Paletizador | 7 | Secuencia de agarre y depósito del Paletizador |

> Archivos ubicados en `Assets/JSON_Generados/` (editor) y `Assets/StreamingAssets/` (runtime).

---

### 4. Arquitectura de Movimiento Descentralizada

La coordinación de movimiento es completamente descentralizada: cada brazo lee y ejecuta su propio archivo JSON de poses de forma independiente mediante `LoadFromFile()` en `Awake` e `IniciarSecuencia()` en `Start` o trigger externo. Los cuatro brazos operan en secuencia por diseño de sus respectivos archivos JSON.

**Flujo de movimiento por brazo**:
```
Archivo JSON propio (Poses_*.json)
    → LoadFromFile() en Awake
        → IniciarSecuencia() en Start / trigger
            → SmoothX / SmoothZ por frame
                → ArticulationDrive.target actualizado
```

---

### 5. Unificación del Dron (`DronListo.cs`)

Antes de que Omega levante el dron completo, todas las piezas ensambladas deben comportarse como una sola unidad rígida. `DronListo.cs` se adjunta a `BasePrefab` y gestiona esta transición. **Auto-detecta** el ensamblaje completo contando componentes `Rigidbody` hijos y comparándolos contra el umbral configurable `piezasEsperadas` (por defecto 10 = PCB + 4 motores + 4 hélices + tapa), sellando el dron automáticamente sin necesidad de triggers externos.

```csharp
// Auto-detección en Update()
void Update() {
    if (!yaSellado && !dronesListo) {
        int piezasActuales = ContarPiezasEnsambladas();
        if (piezasActuales >= piezasEsperadas)
            PrepararParaLevantamiento();
    }
}

public void PrepararParaLevantamiento() {
    if (yaSellado) return;
    yaSellado = true;
    dronesListo = true;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}

public void SoltarDron() {
    dronesListo = false;
    yaSellado = false;
    foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
        if (rb.gameObject == this.gameObject) continue;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
```

---

### 6. Navegación del Paletizador (`CarroPaletizador.cs`)

`CarroPaletizador.cs` gestiona el movimiento del Paletizador por el suelo. El brazo Paletizador (`Ventosa`) es un **hijo** del GameObject del carro, por lo que toda la unidad  -  brazo + carro  -  se desplaza junta. La navegación se realiza solo en XZ (Y permanece fijo). El carro rota en el eje Y pivotando alrededor de puntos `zonaGiro` configurables.

**Configuración de movimientos**  -  `List<MovimientoPaletizado>` definida en el Inspector, una entrada por dron. Cada entrada especifica:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `nombre` | `string` | Etiqueta descriptiva (ej: "Dron 1 → Punto1_1") |
| `zonaGiro` | `Transform` | Punto pivote  -  el carro gira alrededor de esta posición |
| `anguloGiro` | `float` | Ángulo de rotación en Y (-90° o +90°) |
| `puntoDestino` | `Transform` | Posición final de depósito |
| `patron` | `PatronMovimiento` | `Directo` (recto) o `EnL_XLuegoZ` (trayectoria en L) |

**Secuencia de paletizado por dron** (coroutine `EjecutarSecuencia`):
```
1. Esperar a que el brazo agarre el dron  (ventosa.TieneObjeto)
2. TrasladarA(zonaGiro)                    -  trasladarse al pivote
3. GirarCarroSobrePunto(anguloGiro)        -  rotar en Y alrededor del pivote
4. TrasladarConPivotRotado / TrasladarEnL(puntoDestino)   -  ir al depósito
5. permisoParaSoltar = true                -  habilitar al brazo para soltar
6. Esperar hasta que el brazo suelte      (!ventosa.TieneObjeto)
7. TrasladarConPivotRotado / TrasladarEnL(zonaGiro)   -  regresar al pivote
8. GirarCarroSobrePunto(-anguloGiro)       -  desgirar
9. TrasladarA(puntoInicio)                -  regresar al home
```

**Patrones de movimiento**:

| Patrón | Descripción |
|--------|-------------|
| `Directo` | Lerp suave directo al objetivo (diagonal si X y Z difieren) |
| `EnL_XLuegoZ` | En L: mueve primero el eje X, luego el eje Z |

**Campos clave**:
```csharp
public Transform puntoInicio;                  // Posición home
public List<MovimientoPaletizado> movimientos; // Una entrada por dron
public Ventosa ventosa;                        // Referencia al script del brazo
public float velocidadMovimiento = 1f;         // Velocidad de traslación (m/s)
public float duracionGiro = 0.5f;              // Duración de rotación (s)
public float duracionTrasladoFinal = 0.5f;     // Duración de traslado (s)
public int totalDrones = 0;                    // Sincronizado desde Produccion.cs
```

---

### 7. Mecánicas de Snap

**Dos enfoques** según el tipo de pieza:

| Método | Script | Trigger | Usado para |
|--------|--------|---------|-----------|
| **Proximidad** | `Ensamble.cs` | `snapPorProximidad` + verificación de distancia | PCB, Tapa |
| **Colisión Trigger** | `EnsambleGri.cs` | `distanciaActivacion` | Motores, Hélices |

**Animación de Snap** (Ensamble.cs):
```csharp
// Suavizado exponencial en Update() — se ejecuta hasta distancia < 0.001f
transform.position = Vector3.Lerp(
    transform.position,
    posicionFinal,
    Time.deltaTime * velocidadEncaje
);

transform.rotation = Quaternion.Lerp(
    transform.rotation,
    Quaternion.Euler(rotacionFinalEnsamble),
    Time.deltaTime * velocidadEncaje
);

if (Vector3.Distance(transform.position, posicionFinal) < 0.001f)
{
    transform.position = posicionFinal;
    transform.rotation = Quaternion.Euler(rotacionFinalEnsamble);
    // snap completo -> re-parenteo + marcar ensamblado
}
```

**La rotación final del ensamble** es configurable por pieza:
```csharp
public Vector3 rotacionFinalEnsamble = new Vector3(-90f, 0f, 180f); // Ensamble.cs
public Vector3 rotacionForzada       = new Vector3(-90f, 0f, 0f);   // EnsambleGri.cs
```

---

### 8. Prevención de Race Conditions

**Problema**: `ReproducirSecuencia()` y `LiberarEnSecuencia()` corrían en paralelo.

**Solución: Semáforo Booleano** (en `Ventosa.cs`):
```csharp
private bool liberandoObjeto = false;

IEnumerator LiberarEnSecuencia() {
    liberandoObjeto = true;
    // ... animación de bajada a banda o congelado
    LiberarObjeto();
    liberandoObjeto = false;
}

void ReproducirSecuencia() {
    if (liberandoObjeto) return; // bloquea la secuencia hasta que la liberación termine
    // ... ejecutar pose
}
```

---

### 9. Spawner de Producción (`Produccion.cs`)

Las piezas no se pre-colocan en la escena  -  se instancian en tiempo de ejecución por `Produccion.cs` usando componentes `Spawner` individuales. Las **cajas** (`spawnsCaja`) se instancian una sola vez en `Start()` antes de que comience el ensamblaje, nombrándolas `CajaPrefab(Clone1)` hasta `CajaPrefab(Clone8)`. Las **piezas de ensamblaje** se instancian por dron en `SecuenciaEnsamblaje()` con retrasos escalonados (1 s para base/PCB/pares de motores, 2 s para pares de hélices y tapa).

```csharp
// Las cajas se instancian una vez en Start(), antes del ensamblaje
void Start() {
    for (int i = 0; i < spawnsCaja.Length; i++) {
        GameObject caja = spawnsCaja[i].Spawn();
        caja.name = "CajaPrefab(Clone" + (i + 1) + ")";
    }
    StartCoroutine(LoopProduccion());
}

// Secuencia de spawn por dron
IEnumerator SecuenciaEnsamblaje() {
    baseActual = spawnBase.Spawn();
    yield return new WaitForSeconds(1);
    spawnPCB.Spawn();
    yield return new WaitForSeconds(1);
    spawnMotor1.Spawn(); spawnMotor2.Spawn();
    yield return new WaitForSeconds(1);
    spawnMotor3.Spawn(); spawnMotor4.Spawn();
    yield return new WaitForSeconds(1);
    spawnHelice1.Spawn(); spawnHelice2.Spawn();
    yield return new WaitForSeconds(2);
    spawnHelice3.Spawn(); spawnHelice4.Spawn();
    yield return new WaitForSeconds(2);
    spawnTapa.Spawn();
    yield return new WaitForSeconds(2);
}
```

Cada `Spawner` también asigna automáticamente `puntoEnsamble` (para `Ensamble`) y `baseParent` (para `EnsambleGri`) en el prefab instanciado. Para prefabs de cajas, además conecta el `HingeJoint.connectedBody` al propio `Rigidbody` de la caja, permitiendo que la bisagra de la tapa funcione correctamente.

---

### 10. Cierre de Tapa y Retiro de Carro

Dos scripts gestionan el paso final de empaquetado después de que los drones son depositados en las cajas.

**`CerradorTapa.cs`**  -  Anima la tapa de la caja desde una pose abierta (`rotacionAbierta = Vector3(80,0,0)`) hasta una pose cerrada (`rotacionCerrada = Vector3.zero`) usando una `AnimationCurve` configurable (ease in/out por defecto). Expone un flag `tapaCerrada` que otros scripts pueden consultar. También provee `AbrirTapaInstantaneo()` para resetear la tapa de forma instantánea.

```csharp
[ContextMenu("Cerrar tapa (animación)")]
public void CerrarTapa() {
    StartCoroutine(AnimarCierre());
}

private IEnumerator AnimarCierre() {
    float tiempo = 0f;
    while (tiempo < duracionCierre) {
        float tCurva = curva.Evaluate(tiempo / duracionCierre);
        tapa.localRotation = Quaternion.Slerp(rotInicio, rotFin, tCurva);
        tiempo += Time.deltaTime;
        yield return null;
    }
    tapaCerrada = true;
}
```

**`RetiradorCarro.cs`**  -  Una vez que el carro está listo para abandonar la zona de paletizado, este script reparenta todos los GameObjects de cajas asignados como hijos del carro para que se muevan con él. Espera (vía coroutine) hasta que `CerradorTapa.tapaCerrada` sea `true` en la última caja antes de adoptar.

```csharp
public void IntentarAdoptarCajas() {
    CerradorTapa cerrador = cajaFinal.GetComponent<CerradorTapa>();
    if (!cerrador.tapaCerrada) {
        StartCoroutine(EsperarYAdoptar(cerrador));
        return;
    }
    AdoptarCajas();
}
```

Tras depositar el dron, `CerradorTapa` cierra la tapa y luego destruye el GameObject `BasePrefab(Clone)` dentro de la caja para liberar memoria. `RetiradorCarro` espera hasta que la tapa de la última caja esté cerrada antes de reparentear las cajas al carro.

| Script | Trigger | Salida Clave |
|--------|---------|-------------|
| `CerradorTapa.cs` | Llamada a `CerrarTapa()` | `tapaCerrada = true`, GameObject del dron destruido |
| `RetiradorCarro.cs` | Llamada a `IntentarAdoptarCajas()` | cajas reparentadas al carro |

---

### 11. Panel HMI (`HmiManager.cs` + `CodesysTcpClient.cs`)

<div align="center">

![HMI Unity-CODESYS](docs/HMI%20UNITY-CODESYS.png)

</div>

`HmiManager.cs` proporciona un panel HMI en tiempo real dentro de Unity usando **TextMeshPro**. Auto-descubre los componentes `CodesysTcpClient` y `Produccion` al iniciar la escena y se refresca cada frame.

**Paneles del HMI:**

| Panel | Fuente | Descripción |
|-------|--------|-------------|
| Indicador TCP | `CodesysTcpClient.isConnected` | Verde = conectado, naranja = desconectado |
| SISTEMA ON / OFF | `SISTEMA_ON` (5.º byte del paquete RX) | Estado del sistema enviado directamente por CODESYS |
| NEUMÁTICA ON / OFF | `salidas_plc2 bit 5` (NEUMATICA_ON) | Estado neumática desde FluidSIM vía OPC |
| Estado brazo Omega | `Produccion.OmegaActivo` | ACTIVO / IDLE / MOVING |
| Estado Paletizador | `Produccion.PaletActivo` | HOLDING / IDLE / MOVING |
| Panel LED (8 LEDs) | `salidas_plc1` + `salidas_plc2` | Conteo de cajas 1–8 |
| Cronómetro ciclo | `Time.time − tiempoInicioDronActual` | MM:SS del dron actual |
| Log TCP | `CodesysTcpClient.OnLogMessage` | Log rodante de 10 líneas |

**Protocolo TCP** (`CodesysTcpClient.cs`):
```
TX → CODESYS  [0xAA, TCP_COMANDOS_VENTOSAS, TCP_COMANDOS_LEDS]                    -  3 bytes, cada 50 ms
RX ← CODESYS  [0xBB, salidas_plc1, salidas_plc2, entradas_plc1, SISTEMA_ON]      -  5 bytes, cada 50 ms
```

El `CodesysTcpClient` ejecuta hilos dedicados de envío y recepción con reconexión automática cada `reconnectInterval` segundos (default 3 s). Un contador de generación de conexión evita que hilos obsoletos interfieran tras reconexiones. Unity pausa (`Time.timeScale = 0`) cuando `salidas_plc2 & 0x10` (NEUMATICA_OFF) está activo, lo cual CODESYS aserta ante STOP o EMERGENCIA.

El `CodesysTcpClient` ejecuta hilos dedicados de envío y recepción en segundo plano con reconexión automática cada `reconnectInterval` segundos (por defecto 3 s).

---

### 12. Sistema de Visión de Seguridad (`ESP32 CAM WEBSOCKET/ESP32CAM/Auxiliar System.py`)

Script Python auxiliar que recibe el stream en vivo de una **ESP32-CAM** por WebSocket y ejecuta detección de manos con **MediaPipe** en cada frame. Al detectar una mano en el área de trabajo, dispara una alerta de seguridad multicanal.

> Para un desglose detallado del código, configuración e instrucciones de despliegue consulta el repositorio dedicado:
> **[esp32cam-hand-detection-safety-system](https://github.com/jorgefajardom-coder/esp32cam-hand-detection-safety-system.git)**

**Stack tecnológico:**

| Librería | Rol |
|----------|-----|
| `websockets` | Cliente WebSocket asíncrono a la ESP32-CAM (por defecto: `ws://192.168.1.3:81`) |
| `opencv-python` | Decodificación de frames, overlay visual (texto `STOP` + rectángulo rojo) |
| `mediapipe` | Detección de landmarks de mano en tiempo real (hasta 2 manos, confianza 0.5) |
| `pyttsx3` | Alerta TTS  -  "Retire la mano" (voz Sabina, es-MX) |
| `smtplib` | Alerta por correo vía Gmail SMTP (TLS puerto 587) |
| `requests` | Alerta por Telegram Bot API |
| `python-dotenv` | Credenciales cargadas desde `.env` |

**Pipeline de alertas** (cooldown de 15 segundos entre alertas):
```
Mano detectada → TTS voz → correo (SMTP) → mensaje Telegram
```

**Variables de entorno** (`.env`):
```
EMAIL_ADDRESS, EMAIL_PASSWORD, TO_EMAIL
TELEGRAM_BOT_TOKEN, TELEGRAM_CHAT_ID
WEBSOCKET_URL  (por defecto: ws://192.168.1.3:81)
```

---

### 13. Pausa por STOP / EMERGENCIA (`Produccion.cs`)

Cuando FluidSIM envía señal de STOP o EMERGENCIA, CODESYS desactiva el circuito neumático (`NEUMATICA_OFF` → `salidas_plc2 bit 4`). Unity lo detecta y **congela toda la simulación** como si el usuario presionara Pause.

**Implementación en `Produccion.cs`**:

```csharp
// Condición de pausa: conectado + bit NEUMATICA_OFF activo
public bool SistemaPausado =>
    tcp != null && tcp.isConnected && (tcp.salidas_plc2 & 0x10) != 0;

void Update()
{
    Time.timeScale = SistemaPausado ? 0f : 1f;  // Congela / reanuda todo
    if (!simulacionActiva) return;
    tiempoTotalSimulacion += Time.deltaTime;     // Solo cuenta mientras corre
}

// Todas las esperas también verifican la pausa
IEnumerator Esperar(Func<bool> condicion)
{
    yield return new WaitUntil(() => !SistemaPausado && condicion());
}
```

**Comportamiento:**

| Condición | Efecto |
|-----------|--------|
| `STOP` o `EMERGENCIA` = LOW en FluidSIM | `Time.timeScale = 0` — brazos congelados, coroutines suspendidas |
| Señal restaurada | `Time.timeScale = 1` — la simulación continúa desde donde se detuvo |
| TCP desconectado | `SistemaPausado` devuelve `false` — simulación sigue sin pausar |

`Time.timeScale = 0` congela toda la física de Unity y los temporizadores `WaitForSeconds`, pero **no afecta** los hilos TCP en segundo plano, que siguen corriendo a nivel de SO.

---

### 14. Informe de Análisis Estadístico y OEE (`Html/Analisis estadistico y OEE FINAL.html`)

> Abrir `Html/Analisis estadistico y OEE FINAL.html` en cualquier navegador — autónomo, sin servidor.  
> n = 100 ciclos · 2026-04-28 · 60.37 min sesión total

#### OEE — Eficiencia Global del Equipo

```
 OEE  ████████████████████████░░░░  86.80 %   ✦ CLASE MUNDIAL  (≥ 85 %)
 DIS  ██████████████████████░░░░░░  88.18 %   Tiempo activo / Tiempo planificado
 REN  █████████████████████████░░░  98.43 %   Ciclo ideal / Ciclo real
 CAL  ████████████████████████████ 100.00 %   0 rechazos / 100 producidos
```

```mermaid
pie showData
    title Desglose de Pérdidas OEE — 100 ciclos
    "Tiempo productivo (OEE 86.80%)" : 86.80
    "Pérdida Disponibilidad (11.82%)": 11.82
    "Pérdida Rendimiento (1.38%)"    : 1.38
```

#### Tiempo de Ciclo — Control Estadístico

```mermaid
xychart-beta
    title "Tiempo de ciclo medio por brazo (segundos)"
    x-axis ["Alpha", "Beta", "Omega", "Paletizador"]
    y-axis "segundos" 0 --> 35
    bar [25.27, 22.91, 29.50, 5.37]
```

| Brazo | Media (s) | σ (s) | CV | Notas |
|-------|:---------:|:-----:|:--:|-------|
| **Alpha** | 25.27 | 0.230 | 0.91% | Pinza — Base · Motores · Hélices |
| **Beta** | 22.91 | 0.274 | 1.20% | Pinza — Motores · Hélices |
| **Omega** | 29.50 | 0.264 | 0.89% | Ventosa — PCB · Tapa · transferencia |
| **Paletizador** | 5.37 | 0.618 | 11.5% | Ventosa — bimodal ~4.6 s / ~6.0 s |

> CV < 5% en todos los brazos excepto Paletizador (ruta bimodal) — proceso completamente bajo control estadístico.  
> μ = 32.00 s · σ = 0.298 s · UCL = 32.894 s · **0 / 100 puntos fuera de los límites de control**

El informe HTML incluye 7 secciones interactivas: estadística descriptiva, desglose por brazo, percentiles y FDA, correlación y tendencia, carta de control SPC, resumen consolidado y análisis OEE completo con gauge rolling y cascada de pérdidas.

---

## Estructura del Proyecto

```
drone-packaging-simulation-unity/
├── docs/
│   └── simulation_overview.png           # Vista isométrica de la celda robótica de ensamblaje
├── CODESYS II/                           # Proyecto PLC (runtime CODESYS)
│   ├── CODESYS SIMULATION II.project     # Archivo principal del proyecto CODESYS
│   ├── CODESYS SIMULATION II.Device.Application.*.bootinfo
│   ├── CODESYS SIMULATION II.Device.Application.*.compileinfo
│   ├── CODESYS SIMULATION II.Device.Application.xml
│   └── CODESYS SIMULATION II-*.opt       # Opciones de usuario/máquina
├── Fluidsim/                             # Simulación OPC para FluidSim
│   └── OPC SIMULATION FLUIDSIM.ct
├── Assets/
│   ├── Brazos.cs                    # Brazo gripper  -  Alpha, Beta (643 líneas)
│   ├── Ventosa.cs                   # Brazo ventosa  -  Omega, Paletizador (857 líneas)
│   ├── CarroPaletizador.cs          # Navegación del Paletizador  -  lista de movimientos configurable (317 líneas)
│   ├── DronListo.cs                 # Unifica piezas, auto-detecta ensamblaje completo (70 líneas)
│   ├── Ensamble.cs                  # Lógica snap para PCB / Tapa (157 líneas)
│   ├── EnsambleGri.cs               # Lógica snap para Motores / Hélices (150 líneas)
│   ├── Spawner.cs                   # Instancia prefabs y asigna refs de ensamble (40 líneas)
│   ├── Produccion.cs                # Bucle de producción  -  spawn escalonado + swap de carros (371 líneas)
│   ├── CentrarBase.cs               # Centra la Base vía rb.MovePosition en FixedUpdate, para al colisionar (91 líneas)
│   ├── RetiradorCarro.cs            # Adopta cajas como hijos cuando el carro se retira (91 líneas)
│   ├── CerradorTapa.cs              # Cierre animado de tapa de caja con AnimationCurve (94 líneas)
│   ├── GripperTrigger.cs            # OnTriggerEnter / OnTriggerExit → Brazos.NotifyObjectInside/Exit()
│   ├── SuctionTrigger.cs            # OnTriggerEnter / OnTriggerExit → Ventosa.NotifyObjectInside/Exit()
│   ├── CodesysTcpClient.cs          # Cliente TCP  -  Unity ↔ CODESYS 3.5.15.40 (puerto 8888)
│   ├── HmiManager.cs                # Panel HMI  -  TextMeshPro, estados de brazos, indicadores LED
│   ├── CV_1.renderTexture
│   ├── CV_5.renderTexture
│   ├── JSON_Generados/              # 4 archivos JSON de poses  -  cada brazo lee el suyo
│   │   ├── Poses_Alpha.json
│   │   ├── Poses_Beta.json
│   │   ├── Poses_Omega.json
│   │   └── Poses_Palet.json
│   ├── StreamingAssets/             # Copias runtime de los 4 archivos JSON
│   │   ├── Poses_Alpha.json
│   │   ├── Poses_Beta.json
│   │   ├── Poses_Omega.json
│   │   └── Poses_Palet.json
│   ├── Materials/
│   │   ├── SafetyStripes.shader
│   │   └── SafetyStripesMat.mat
│   └── Scenes/
│       └── SampleScene.unity
├── Html/
│   └── Analisis estadistico y OEE FINAL.html  # Informe estadístico interactivo — 100 ciclos, OEE 86.80%
├── ESP32 CAM WEBSOCKET/
│   └── ESP32CAM/
│       └── Auxiliar System.py       # Sistema de seguridad con visión (MediaPipe + WebSocket)
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
- **CODESYS** (opcional)  -  para ejecutar la capa de simulación PLC

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

5. **CODESYS / FluidSim** (opcional)
   - Abrir `CODESYS II/CODESYS SIMULATION II.project` con **CODESYS 3.5.15.40**
   - El archivo de simulación OPC para FluidSim está en `Fluidsim/OPC SIMULATION FLUIDSIM.ct`
   - Unity se conecta a CODESYS por TCP en `127.0.0.1:8888` (configurable en el Inspector de `CodesysTcpClient`)

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
// En EnsambleGri.cs  -  usa coincidencia de nombre vía ConfigurarRotacionPorNumero()
if (esHelice && usarRotacionPorNumero) {
    if (nombreLimpio.Contains("Helice1")) {
        rotacionForzada = new Vector3(90f, 0f, 0f);
    } else if (nombreLimpio.Contains("Helice2")) {
        rotacionForzada = new Vector3(-90f, 90f, 0f);
    } else if (nombreLimpio.Contains("Helice3")) {
        rotacionForzada = new Vector3(-90f, 180f, 0f);
    } else if (nombreLimpio.Contains("Helice4")) {
        rotacionForzada = new Vector3(90f, 270f, 0f);
    }
}
```

**Configuración Inspector**:
- `Es Helice`: ✅
- `Forzar Rotacion Absoluta`: ✅
- `Usar Rotacion Por Numero`: ✅ (detecta automáticamente el número de hélice del nombre del prefab)

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

## Tabla Resumen de Bugs

| # | Bug | Severidad | Estado | Solución |
|---|-----|-----------|--------|----------|
| 1 | Flips de rotación al agarrar | 🔴 Crítico | ✅ Resuelto | Preservar world-space |
| 2 | Tapa atraviesa componentes | 🔴 Crítico | ✅ Resuelto | Kinematic + collision layers |
| 3 | Race condition secuencias | 🟡 Alto | ✅ Resuelto | Flag semáforo |
| 4 | Rotación hélices | 🟡 Alto | ✅ Resuelto | Rotación absoluta por número |
| 5 | Stuttering movimiento | 🟢 Medio | ✅ Resuelto | Acumulación correcta de t |
| 6 | Reconexión TCP falla tras primera desconexión | 🔴 Crítico | ✅ Resuelto | Contador de generación de conexión en `CodesysTcpClient` |
| 7 | CODESYS bloquea en `SysSockRecv` — no acepta nuevas conexiones | 🔴 Crítico | ✅ Resuelto | `SysSockSelect` no bloqueante antes de cada recv |

---

## Jerarquía del Brazo Robótico

```
BrazoBase (fixed)
└── Waist (Revolute  -  X Drive)
    └── Arm01 (Revolute  -  Z Drive)
        └── Arm02 (Revolute  -  Z Drive)
            └── Arm03 (Revolute  -  X Drive)
                └── GripperAssembly (Revolute  -  Z Drive)
                    ├── Gear1 (Prismatic  -  X Drive, apertura/cierre)
                    └── Gear2 (Prismatic  -  X Drive, espejo)
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
| **stiffness** | Rigidez de la articulación  -  valores altos producen respuesta más firme |
| **damping** | Amortiguación de oscilaciones |
| **forceLimit** | Fuerza máxima aplicable |
| **target** | Valor objetivo de posición o rotación |

### Tags del Proyecto

```
"Pickable"  -  Todas las piezas agarrables (Base, PCB, Motores, Hélices, Tapa)
```

> **Nota**: Todas las piezas agarrables comparten el único tag `"Pickable"`. Los scripts diferencian tipos de pieza por su componente adjunto (`Ensamble` para piezas de ventosa, `EnsambleGri` para piezas de gripper) y por coincidencia del nombre del prefab.

---

## Autores

**Jorge Andres Fajardo Mora**  
**Laura Vanesa Castro Sierra**

---

## Licencia y Derechos

**Copyright © 2025 Jorge Andres Fajardo Mora y Laura Vanesa Castro Sierra. Todos los derechos reservados.**

Este repositorio y la totalidad de su contenido  -  incluyendo, entre otros, código fuente, scripts, archivos de configuración, archivos de datos y documentación  -  se proporcionan exclusivamente para **fines de lectura y referencia**.

**No se otorga ningún permiso** para copiar, modificar, distribuir, sublicenciar ni utilizar ninguna parte de este proyecto con fines comerciales o no comerciales sin **autorización escrita explícita** de los autores.

Queda **estrictamente prohibida** la reproducción o redistribución no autorizada de este trabajo, en todo o en parte.
