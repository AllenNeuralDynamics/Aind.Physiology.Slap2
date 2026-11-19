# Aind.Physiology.Slap2

Bonsai workflow for SLAP2 physiology acquisition. It talks to a Harp Cuttlefish board, records rising edges on one digital input pin, and logs everything the board sends and receives.

## Setup

Run `.bonsai/setup.cmd` once. It downloads Bonsai into `.bonsai/` and installs the packages listed in `.bonsai/Bonsai.config`.

## Running

### With the editor

Open `src/main.bonsai` in `.bonsai/Bonsai.exe`, set the properties below in the property grid, and press Start.

### Headless (no editor)

Run the workflow straight from the command line and pass the properties with `-p:<Name>=<Value>`:

```powershell
.\.bonsai\Bonsai.exe .\src\main.bonsai --no-editor `
    -p:ComPort=COM4 `
    -p:OutputPath="C:\data\my_session"
```

`--no-editor` starts the workflow right away without opening the editor. The `SlapVisualizer` window still opens, because it is part of the workflow.

### Properties

| Property     | What it sets                                  | Example              |
| ------------ | --------------------------------------------- | -------------------- |
| `OutputPath` | Root folder for all logged data               | `C:\data\my_session` |
| `ComPort`    | Serial port of the Cuttlefish board           | `COM4`               |

### Digital input

The signal is expected on **DI0** (`Pin0`). The workflow monitors and displays that pin, and `sync.csv` only contains edges from it.

Rising-edge events are enabled on **all** pins (`Pin0`–`Pin7`), so rising edges on every pin are saved in the raw Harp log (`Cuttlefish.harp/`) and can be recovered later.

The monitored pin is set in the editor, on the `DIPin` source in `main.bonsai`. It can't be changed from the command line yet.

> [!WARNING]
> Logging never overwrites existing files. Use a new `OutputPath` for every run, or the workflow fails to start.

## Output

Everything is written under `OutputPath`:

```text
📁 <OutputPath>/
├── 📁 Cuttlefish.harp/        # 📥 everything the board sent
├── 📁 HarpCommands/
│   └── 📁 Cuttlefish.harp/    # 📤 every command sent to the board
└── 📊 sync.csv                # 📈 one row per rising edge on DI0
```

`sync.csv` has one column, `ReferenceTime`: the Harp timestamp of each rising edge on DI0, in seconds.
