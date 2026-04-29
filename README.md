# DBD AI Simulator

A Unity ML-Agents simulation project inspired by Dead by Daylight, featuring pretrained AI models and support for training new agents.

---

## Requirements

- **Unity Editor:** `6000.4.0f1` (install via [Unity Hub](https://unity.com/download))
- **Anaconda or Miniconda:** [Download here](https://www.anaconda.com/download)
- **Python:** 3.10.12 (recommended for ML-Agents 1.1.0 compatibility)

---

## Setup & Running the Project (with Pretrained Models)

1. **Clone the repository**
   ```bash
   git clone https://github.com/bfontenot23/DBDAISim.git
   cd DBDAISim
   ```

2. **Install Unity Hub and Unity Editor `6000.4.0f1`**
   - Open Unity Hub → Installs → Add → select version `6000.4.0f1`

3. **Open the project in Unity Hub**
   - Unity Hub → Projects → Open → select the cloned repo folder
   - This loads the game with the 2 pretrained models included
   - Press Play in the Unity Editor to start the simulation using pretrained model v2

---

## Environment Setup (Conda)

1. **Create and activate a new Conda environment**
   ```bash
   conda create -n dbdaisim python=3.10.12 -y
   conda activate dbdaisim
   ```

2. **Install the required packages**

   The primary dependencies to install are `mlagents` and `onnxscript` — all other packages will be pulled in automatically as dependencies of `mlagents`:
   ```bash
   pip install mlagents==1.1.0 onnxscript==0.7.0
   ```

   <details>
   <summary>Full list of installed packages (for reference)</summary>

   | Package                  | Version   |
   |--------------------------|-----------|
   | absl-py                  | 2.4.0     |
   | annotated-doc            | 0.0.4     |
   | anyio                    | 4.13.0    |
   | attrs                    | 26.1.0    |
   | cattrs                   | 1.5.0     |
   | certifi                  | 2026.4.22 |
   | click                    | 8.3.3     |
   | cloudpickle              | 3.1.2     |
   | colorama                 | 0.4.6     |
   | exceptiongroup           | 1.3.1     |
   | filelock                 | 3.29.0    |
   | fsspec                   | 2026.3.0  |
   | grpcio                   | 1.48.2    |
   | gym                      | 0.26.2    |
   | gym-notices              | 0.1.0     |
   | h11                      | 0.16.0    |
   | h5py                     | 3.16.0    |
   | hf-xet                   | 1.4.3     |
   | httpcore                 | 1.0.9     |
   | httpx                    | 0.28.1    |
   | huggingface_hub          | 1.12.0    |
   | idna                     | 3.13      |
   | Jinja2                   | 3.1.6     |
   | Markdown                 | 3.10.2    |
   | markdown-it-py           | 4.0.0     |
   | MarkupSafe               | 3.0.3     |
   | mdurl                    | 0.1.2     |
   | ml_dtypes                | 0.5.4     |
   | mlagents                 | 1.1.0     |
   | mlagents-envs            | 1.1.0     |
   | mpmath                   | 1.3.0     |
   | networkx                 | 3.4.2     |
   | numpy                    | 1.23.5    |
   | onnx                     | 1.17.0    |
   | onnx-ir                  | 0.2.1     |
   | onnxscript               | 0.7.0     |
   | packaging                | 26.0      |
   | PettingZoo               | 1.15.0    |
   | pillow                   | 12.2.0    |
   | pip                      | 26.0.1    |
   | protobuf                 | 3.20.3    |
   | Pygments                 | 2.20.0    |
   | pypiwin32                | 223       |
   | pywin32                  | 311       |
   | PyYAML                   | 6.0.3     |
   | rich                     | 15.0.0    |
   | setuptools               | 81.0.0    |
   | shellingham              | 1.5.4     |
   | six                      | 1.17.0    |
   | sympy                    | 1.14.0    |
   | tensorboard              | 2.20.0    |
   | tensorboard-data-server  | 0.7.2     |
   | torch                    | 2.1.1     |
   | tqdm                     | 4.67.3    |
   | typer                    | 0.25.0    |
   | typing_extensions        | 4.15.0    |
   | Werkzeug                 | 3.1.8     |
   | wheel                    | 0.46.3    |

   </details>

---

## Training a New Model

1. Complete all steps in [Setup & Running the Project](#setup--running-the-project-with-pretrained-models) and [Environment Setup](#environment-setup-conda) above.

2. **Navigate to the project root**
   ```bash
   cd path/to/DBDAISim
   ```

3. **Modify agent prefabs** In the Unity Editor, navigate to `Assets/Prefabs/Agents` and select the agent prefab you want to train (e.g. `SurvivorAgent.prefab`). In the Inspector, find the `Behavior Parameters` component and set the `Behavior Type` to `Default`. This allows the agent to learn from the trainer instead of using a pretrained model.

4. **Start the ML-Agents trainer** (replace `RUN_ID_HERE` with a unique name for your run)
   ```bash
   mlagents-learn Assets/Configs/agentconfig.yaml --run-id=RUN_ID_HERE
   ```

5. **Press Play in the Unity Editor** — training will begin once Unity connects to the trainer.

6. *(Optional)* Monitor training progress with TensorBoard:
   ```bash
   tensorboard --logdir results
   ```
   Then open `http://localhost:6006` in your browser.
