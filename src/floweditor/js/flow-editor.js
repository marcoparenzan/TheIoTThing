// FlowEditor - main controller class
class FlowEditor {
  constructor(canvasId) {
    this.canvas = document.getElementById(canvasId);
    this.canvasContainer = document.getElementById("canvasContainer");
    this.propertyPanel = document.getElementById("propertyPanel");
    this.steps = [];
    this.pipes = [];
    this.selectedSteps = [];
    this.dragState = null;
    this.connectionState = null;
    this.resizeState = null;
    this.scale = 1;
    this.historyManager = new HistoryManager(this);

    this.initEvents();
    this.saveCurrentState();
  }

  initEvents() {
    // Canvas events
    this.canvas.addEventListener("click", (e) => {
      if (e.target === this.canvas) {
        this.clearSelection();
      }
    });

    this.canvas.addEventListener("mousemove", (e) => {
      this.handleMouseMove(e);
    });

    document.addEventListener("mouseup", () => {
      this.handleMouseUp();
    });

    // Add to FlowEditor initEvents
    // Add keyboard shortcut for delete
    document.addEventListener("keydown", (e) => {
      if (e.key === "Delete" || e.key === "Backspace") {
        if (this.selectedPipe) {
          this.deletePipe();
        } else if (this.selectedSteps.length > 0) {
          this.selectedSteps.forEach((step) => this.removeStep(step));
        }
      }
    });

    // Toolbox drag start
    document.querySelectorAll(".toolbox-item").forEach((item) => {
      item.addEventListener("dragstart", (e) => {
        e.dataTransfer.setData("type", item.dataset.type);
      });
    });

    // Canvas drop area
    this.canvas.addEventListener("dragover", (e) => {
      e.preventDefault();
    });

    this.canvas.addEventListener("drop", (e) => {
      e.preventDefault();
      const type = e.dataTransfer.getData("type");
      if (type) {
        const canvasRect = this.canvas.getBoundingClientRect();
        const x = e.clientX - canvasRect.left + this.canvas.scrollLeft;
        const y = e.clientY - canvasRect.top + this.canvas.scrollTop;
        this.addStep(type, x, y);
      }
    });

    // Toolbar buttons
    document.getElementById("newBtn").addEventListener("click", () => {
      if (
        confirm(
          "Are you sure you want to create a new flow? All unsaved changes will be lost."
        )
      ) {
        this.clear();
      }
    });

    document.getElementById("saveBtn").addEventListener("click", () => {
      this.save();
    });

    document.getElementById("loadBtn").addEventListener("click", () => {
      this.showLoadDialog();
    });

    document.getElementById("undoBtn").addEventListener("click", () => {
      this.historyManager.undo();
    });

    document.getElementById("redoBtn").addEventListener("click", () => {
      this.historyManager.redo();
    });

    document.getElementById("zoomInBtn").addEventListener("click", () => {
      this.zoom(0.1);
    });

    document.getElementById("zoomOutBtn").addEventListener("click", () => {
      this.zoom(-0.1);
    });

    document.getElementById("zoomResetBtn").addEventListener("click", () => {
      this.scale = 1;
      this.applyZoom();
    });

    // Property panel buttons
    document.getElementById("editScriptBtn").addEventListener("click", () => {
      if (this.selectedSteps.length === 1) {
        this.showScriptDialog(this.selectedSteps[0]);
      }
    });

    document.getElementById("addPropertyBtn").addEventListener("click", () => {
      if (this.selectedSteps.length === 1) {
        this.showPropertyDialog();
      }
    });

    // Handle property changes
    this.propertyPanel
      .querySelector("#stepName")
      .addEventListener("change", (e) => {
        if (this.selectedSteps.length === 1) {
          this.selectedSteps[0].updateProperties({
            name: e.target.value,
          });
          this.saveCurrentState();
        }
      });

    this.propertyPanel
      .querySelector("#stepColor")
      .addEventListener("change", (e) => {
        if (this.selectedSteps.length === 1) {
          this.selectedSteps[0].updateProperties({
            color: e.target.value,
          });
          this.saveCurrentState();
        }
      });

    // Dialog events
    document.getElementById("saveScriptBtn").addEventListener("click", () => {
      this.saveScript();
    });

    document.getElementById("savePropertyBtn").addEventListener("click", () => {
      this.saveProperty();
    });

    document.getElementById("downloadBtn").addEventListener("click", () => {
      this.downloadFlow();
    });

    document.getElementById("loadFlowBtn").addEventListener("click", () => {
      this.loadFlowFromDialog();
    });

    // Close dialogs
    document.querySelectorAll("[data-close]").forEach((el) => {
      el.addEventListener("click", () => {
        const dialogId = el.dataset.close;
        document.getElementById(dialogId).style.display = "none";
      });
    });

    // Property type change event
    document.getElementById("propertyType").addEventListener("change", (e) => {
      const optionsRow = document.getElementById("propertyOptionsRow");
      optionsRow.style.display = e.target.value === "select" ? "block" : "none";
    });
  }

  addStep(type, x, y) {
    const step = new Step(null, type, x, y, this);
    step.render();
    this.steps.push(step);
    this.selectStep(step, false);
    this.saveCurrentState();
    return step;
  }

  removeStep(step) {
    // Remove all pipes connected to this step
    this.pipes = this.pipes.filter((pipe) => {
      if (pipe.sourceStep === step || pipe.targetStep === step) {
        pipe.remove();
        return false;
      }
      return true;
    });

    // Remove the step from the canvas
    if (step.element) {
      step.element.remove();
    }

    // Remove from steps array
    this.steps = this.steps.filter((s) => s !== step);

    // Remove from selection
    this.selectedSteps = this.selectedSteps.filter((s) => s !== step);

    if (this.selectedSteps.length === 0) {
      this.propertyPanel.classList.remove("visible");
    }

    this.saveCurrentState();
  }

  connectSteps(sourceStep, sourceOutput, targetStep, targetInput) {
    // Check if connection already exists
    const existingPipe = this.pipes.find(
      (p) =>
        p.sourceStep === sourceStep &&
        p.sourceOutput === sourceOutput &&
        p.targetStep === targetStep &&
        p.targetInput === targetInput
    );

    if (existingPipe) return;

    // Check if target input already has a connection
    const inputInUse = this.pipes.some(
      (p) => p.targetStep === targetStep && p.targetInput === targetInput
    );

    if (inputInUse) {
      // Remove the existing connection
      this.pipes = this.pipes.filter((p) => {
        if (p.targetStep === targetStep && p.targetInput === targetInput) {
          p.remove();
          return false;
        }
        return true;
      });
    }

    // Create new connection
    const pipe = new Pipe(
      null,
      sourceStep,
      sourceOutput,
      targetStep,
      targetInput,
      this
    );
    pipe.render();
    this.pipes.push(pipe);
    this.saveCurrentState();
  }

  selectStep(step, multiSelect = false) {
    if (!multiSelect) {
      // Deselect all other steps
      this.selectedSteps.forEach((s) => {
        if (s.element) s.element.classList.remove("selected");
      });
      this.selectedSteps = [];
    }

    if (!this.selectedSteps.includes(step)) {
      this.selectedSteps.push(step);
    }

    this.selectedSteps.forEach((s) => {
      if (s.element) s.element.classList.add("selected");
    });

    this.updatePropertyPanel();
  }

  // Add to FlowEditor class
  selectPipe(pipe) {
    // Clear any step selection
    this.clearSelection();

    // Deselect previous pipe
    if (this.selectedPipe) {
      this.selectedPipe.selected = false;
      this.selectedPipe.render();
    }

    // Select new pipe
    this.selectedPipe = pipe;
    pipe.selected = true;
    pipe.render();

    this.updatePipePropertyPanel();
  }

  updatePipePropertyPanel() {
    if (!this.selectedPipe) return;

    this.propertyPanel.innerHTML = `
            <div class="property-group">
              <h3>Pipe Properties</h3>
              <div class="property-row">
                <label for="pipeName_${this.selectedPipe.id}">Name</label>
                <input type="text" id="pipeName_${this.selectedPipe.id}" value="${this.selectedPipe.name}">
              </div>
              <div class="property-row">
                <label for="pipeType_${this.selectedPipe.id}">Type</label>
                <input type="text" id="pipeType_${this.selectedPipe.id}" value="${this.selectedPipe.type}">
              </div>
              <div class="property-row">
                <label for="pipeColor_${this.selectedPipe.id}">Color</label>
                <input type="color" id="pipeColor_${this.selectedPipe.id}" value="${this.selectedPipe.color}">
              </div>
              <div class="property-row">
                  <button id="editPipeScriptBtn_${this.selectedPipe.id}">Edit Script</button>
                </div>
              <div class="property-row">
                <button id="deletePipeBtn_${this.selectedPipe.id}">Delete Pipe</button>
              </div>
            </div>
            <div class="property-group">
              <h3>Custom Properties</h3>
              <div id="pipePropertiesList_${this.selectedPipe.id}"></div>
              <button id="addPipePropertyBtn_${this.selectedPipe.id}">Add Property</button>
            </div>
          `;

    this.propertyPanel.classList.add("visible");

    // Add event listeners
    document
      .getElementById(`pipeName_${this.selectedPipe.id}`)
      .addEventListener("change", (e) => {
        this.selectedPipe.updateProperties({ name: e.target.value });
        this.saveCurrentState();
      });

    document
      .getElementById(`pipeType_${this.selectedPipe.id}`)
      .addEventListener("change", (e) => {
        this.selectedPipe.updateProperties({ type: e.target.value });
        this.saveCurrentState();
      });
    document
      .getElementById(`pipeColor_${this.selectedPipe.id}`)
      .addEventListener("change", (e) => {
        this.selectedPipe.updateProperties({ color: e.target.value });
        this.saveCurrentState();
      });

    document
      .getElementById(`deletePipeBtn_${this.selectedPipe.id}`)
      .addEventListener("click", () => {
        this.deletePipe();
      });

    document
      .getElementById(`editPipeScriptBtn_${this.selectedPipe.id}`)
      .addEventListener("click", () => {
        this.showScriptDialog(this.selectedPipe);
      });


    document
      .getElementById(`addPipePropertyBtn_${this.selectedPipe.id}`)
      .addEventListener("click", () => {
        this.showPipePropertyDialog();
      });

    this.updatePipePropertiesList();
  }

  // Update updatePipePropertiesList method to use pipe-specific identifiers

  // Add method to show pipe properties
  updatePipePropertiesList() {
    const list = document.getElementById(
      `pipePropertiesList_${this.selectedPipe.id}`
    );
    if (!list) return;

    list.innerHTML = "";

    for (const [key, value] of Object.entries(this.selectedPipe.properties)) {
      const row = document.createElement("div");
      row.className = "property-row";
      row.innerHTML = `
              <label>${key}</label>
              <div style="display: flex; gap: 5px;">
                <input type="text" value="${value}" id="pipe_prop_${this.selectedPipe.id}_${key}" style="flex-grow: 1;">
                <button class="remove-prop" data-key="${key}" data-pipe="${this.selectedPipe.id}">❌</button>
              </div>
            `;
      list.appendChild(row);

      row
        .querySelector(`#pipe_prop_${this.selectedPipe.id}_${key}`)
        .addEventListener("change", (e) => {
          this.selectedPipe.properties[key] = e.target.value;
          this.saveCurrentState();
        });

      row.querySelector(".remove-prop").addEventListener("click", (e) => {
        delete this.selectedPipe.properties[e.target.dataset.key];
        this.updatePipePropertiesList();
        this.saveCurrentState();
      });
    }
  }

  // Add method for pipe property dialog
  showPipePropertyDialog() {
    document.getElementById("propertyName").value = "";
    document.getElementById("propertyDefault").value = "";
    document.getElementById("propertyType").value = "text";
    document.getElementById("propertyOptionsRow").style.display = "none";

    const saveBtn = document.getElementById("savePropertyBtn");
    const oldClick = saveBtn.onclick;
    saveBtn.onclick = () => this.savePipeProperty();

    document.getElementById("propertyDialog").style.display = "flex";

    // Restore original handler when closed
    document.querySelectorAll("[data-close='propertyDialog']").forEach((el) => {
      el.addEventListener(
        "click",
        () => {
          saveBtn.onclick = oldClick;
        },
        { once: true }
      );
    });
  }

  // Add method to save pipe property
  savePipeProperty() {
    if (!this.selectedPipe) return;

    const name = document.getElementById("propertyName").value.trim();
    const value = document.getElementById("propertyDefault").value;

    if (name) {
      this.selectedPipe.properties[name] = value;
      this.updatePipePropertiesList();
      document.getElementById("propertyDialog").style.display = "none";
      this.saveCurrentState();
    }
  }

  // Modify the FlowEditor clearSelection method
  clearSelection() {
    this.selectedSteps.forEach((step) => {
      if (step.element) step.element.classList.remove("selected");
    });
    this.selectedSteps = [];

    if (this.selectedPipe) {
      this.selectedPipe.selected = false;
      this.selectedPipe.render();
      this.selectedPipe = null;
    }

    this.propertyPanel.classList.remove("visible");
  }

  // Add method to delete selected pipe
  deletePipe() {
    if (!this.selectedPipe) return;

    this.pipes = this.pipes.filter((p) => p !== this.selectedPipe);
    this.selectedPipe.remove();
    this.selectedPipe = null;
    this.propertyPanel.classList.remove("visible");
    this.saveCurrentState();
  }

  updatePropertyPanel() {
    if (this.selectedSteps.length === 1) {
      const step = this.selectedSteps[0];

      // Build step-specific property panel HTML
      this.propertyPanel.innerHTML = `
              <div class="property-group">
                <h3>Step Properties</h3>
                <div class="property-row">
                  <label for="stepName_${step.id}">Name</label>
                  <input type="text" id="stepName_${step.id}" value="${step.name}">
                </div>
                <div class="property-row">
                  <label for="stepType_${step.id}">Type</label>
                  <input type="text" id="stepType_${step.id}" value="${step.type}" readonly>
                </div>
                <div class="property-row">
                  <label for="stepColor_${step.id}">Color</label>
                  <input type="color" id="stepColor_${step.id}" value="${step.color}">
                </div>
                <div class="property-row">
                  <button id="editScriptBtn_${step.id}">Edit Script</button>
                </div>
              </div>
              <div class="property-group">
                <h3>Custom Properties</h3>
                <div id="propertiesList_${step.id}"></div>
                <button id="addPropertyBtn_${step.id}">Add Property</button>
              </div>
            `;

      // Add event listeners for step properties
      document
        .getElementById(`stepName_${step.id}`)
        .addEventListener("change", (e) => {
          step.updateProperties({ name: e.target.value });
          this.saveCurrentState();
        });

      document
        .getElementById(`stepColor_${step.id}`)
        .addEventListener("change", (e) => {
          step.updateProperties({ color: e.target.value });
          this.saveCurrentState();
        });

      document
        .getElementById(`editScriptBtn_${step.id}`)
        .addEventListener("click", () => {
          this.showScriptDialog(step);
        });

      document
        .getElementById(`addPropertyBtn_${step.id}`)
        .addEventListener("click", () => {
          this.showPropertyDialog();
        });

      // Update custom properties list
      const propertiesList = document.getElementById(
        `propertiesList_${step.id}`
      );
      propertiesList.innerHTML = "";

      for (const [key, value] of Object.entries(step.properties)) {
        const propRow = document.createElement("div");
        propRow.className = "property-row";
        propRow.innerHTML = `
                <label for="prop_${step.id}_${key}">${key}</label>
                <div style="display: flex; gap: 5px;">
                  <input type="text" id="prop_${step.id}_${key}" value="${value}" style="flex-grow: 1;">
                  <button class="remove-prop" data-key="${key}" data-step="${step.id}">❌</button>
                </div>
              `;
        propertiesList.appendChild(propRow);

        // Add change listener
        propRow
          .querySelector(`#prop_${step.id}_${key}`)
          .addEventListener("change", (e) => {
            step.properties[key] = e.target.value;
            this.saveCurrentState();
          });

        propRow.querySelector(".remove-prop").addEventListener("click", (e) => {
          delete step.properties[e.target.dataset.key];
          this.updatePropertyPanel();
          this.saveCurrentState();
        });
      }

      this.propertyPanel.classList.add("visible");
    } else {
      this.propertyPanel.classList.remove("visible");
    }
  }

  startDrag(event) {
    if (this.selectedSteps.length === 0) return;

    this.dragState = {
      startX: event.clientX,
      startY: event.clientY,
      steps: this.selectedSteps.map((step) => ({
        step,
        startX: step.x,
        startY: step.y,
      })),
    };
  }

  startConnection(step, connectorId, connectorType) {
    const tempLine = document.createElementNS(
      "http://www.w3.org/2000/svg",
      "svg"
    );
    tempLine.classList.add("pipe", "temp-connection");
    tempLine.style.position = "absolute";
    tempLine.style.width = "100%";
    tempLine.style.height = "100%";
    tempLine.style.zIndex = "1000";
    tempLine.innerHTML =
      '<path stroke="#666" stroke-width="2" fill="none" stroke-dasharray="5,5"></path>';
    this.canvas.appendChild(tempLine);

    const startPos = step.getConnectorPosition(connectorId, connectorType);

    this.connectionState = {
      step,
      connectorId,
      connectorType,
      startPos,
      tempLine,
    };
  }

  startResize(step, event) {
    this.resizeState = {
      step,
      startX: event.clientX,
      startY: event.clientY,
      startWidth: step.width,
      startHeight: step.height,
    };
  }

  handleMouseMove(event) {
    if (this.dragState) {
      const dx = event.clientX - this.dragState.startX;
      const dy = event.clientY - this.dragState.startY;

      this.dragState.steps.forEach(({ step, startX, startY }) => {
        step.x = startX + dx;
        step.y = startY + dy;
        step.render();
      });

      this.updatePipes();
    } else if (this.connectionState) {
      const canvasRect = this.canvas.getBoundingClientRect();
      const currentPos = {
        x: event.clientX - canvasRect.left + this.canvas.scrollLeft,
        y: event.clientY - canvasRect.top + this.canvas.scrollTop,
      };

      const startPos = this.connectionState.startPos;

      // Calculate control points for the bezier curve
      const dx = Math.abs(currentPos.x - startPos.x);
      const controlPointOffset = Math.min(100, dx * 0.8);

      const path =
        this.connectionState.connectorType === "output"
          ? `M ${startPos.x} ${startPos.y} C ${
              startPos.x + controlPointOffset
            } ${startPos.y}, ${currentPos.x - controlPointOffset} ${
              currentPos.y
            }, ${currentPos.x} ${currentPos.y}`
          : `M ${currentPos.x} ${currentPos.y} C ${
              currentPos.x + controlPointOffset
            } ${currentPos.y}, ${startPos.x - controlPointOffset} ${
              startPos.y
            }, ${startPos.x} ${startPos.y}`;

      this.connectionState.tempLine
        .querySelector("path")
        .setAttribute("d", path);

      // Highlight potential connection points
      document.querySelectorAll(".connector").forEach((el) => {
        el.classList.remove("highlight");

        const targetType = el.dataset.type;
        const targetStepId = el.closest(".step").id;
        const targetStep = this.steps.find((s) => s.id === targetStepId);

        // Skip if it's the same step or incompatible connector type
        if (targetStep === this.connectionState.step) return;

        // Can only connect output->input or input->output
        if (
          (this.connectionState.connectorType === "output" &&
            targetType === "input") ||
          (this.connectionState.connectorType === "input" &&
            targetType === "output")
        ) {
          const connectorId = el.dataset.id;
          const position = targetStep.getConnectorPosition(
            connectorId,
            targetType
          );
          const canvasRect = this.canvas.getBoundingClientRect();

          // Convert mouse coordinates to canvas space
          const mouseX =
            event.clientX - canvasRect.left + this.canvas.scrollLeft;
          const mouseY = event.clientY - canvasRect.top + this.canvas.scrollTop;

          // Account for canvas scaling
          const scaledMouseX = mouseX / this.scale;
          const scaledMouseY = mouseY / this.scale;

          // Check if mouse is near this connector
          const distance = Math.sqrt(
            Math.pow(scaledMouseX - position.x, 2) +
              Math.pow(scaledMouseY - position.y, 2)
          );

          if (distance < 30) {
            el.classList.add("highlight");
          }
        }
      });
    } else if (this.resizeState) {
      const dx = event.clientX - this.resizeState.startX;
      const dy = event.clientY - this.resizeState.startY;

      const step = this.resizeState.step;
      step.width = Math.max(100, this.resizeState.startWidth + dx);
      step.height = Math.max(80, this.resizeState.startHeight + dy);
      step.render();

      this.updatePipes();
    }
  }

  handleMouseUp() {
    if (this.dragState) {
      this.dragState = null;
      this.saveCurrentState();
    }

    if (this.connectionState) {
      // Find if we're over a valid connection point
      const highlightedConnector = document.querySelector(
        ".connector.highlight"
      );

      if (highlightedConnector) {
        const sourceStep = this.connectionState.step;
        const sourceConnector = this.connectionState.connectorId;
        const sourceType = this.connectionState.connectorType;

        const targetId = highlightedConnector.dataset.id;
        const targetType = highlightedConnector.dataset.type;
        const targetStepId = highlightedConnector.closest(".step").id;
        const targetStep = this.steps.find((s) => s.id === targetStepId);

        if (sourceStep !== targetStep) {
          if (sourceType === "output" && targetType === "input") {
            this.connectSteps(
              sourceStep,
              sourceConnector,
              targetStep,
              targetId
            );
          } else if (sourceType === "input" && targetType === "output") {
            this.connectSteps(
              targetStep,
              targetId,
              sourceStep,
              sourceConnector
            );
          }
        }
      }

      highlightedConnector?.classList.remove("highlight");
      this.connectionState.tempLine.remove();
      this.connectionState = null;
      this.updatePipes(); // Ensure all pipes are properly rendered
    }

    if (this.resizeState) {
      this.resizeState = null;
      this.saveCurrentState();
    }
  }

  updatePipes() {
    this.pipes.forEach((pipe) => pipe.render());
  }

  showScriptDialog(step) {
    document.getElementById("scriptEditor").value = step.script || "";
    document.getElementById("scriptDialog").style.display = "flex";
  }

  saveScript() {
    if (this.selectedSteps.length === 1) {
      const step = this.selectedSteps[0];
      step.script = document.getElementById("scriptEditor").value;
      document.getElementById("scriptDialog").style.display = "none";
      this.saveCurrentState();
    }
    else if (this.selectedPipe) {
      const pipe = this.selectedPipe;
      pipe.script = document.getElementById("scriptEditor").value;
      document.getElementById("scriptDialog").style.display = "none";
      this.saveCurrentState();
    }  
  }

  showPropertyDialog() {
    document.getElementById("propertyName").value = "";
    document.getElementById("propertyType").value = "text";
    document.getElementById("propertyOptions").value = "";
    document.getElementById("propertyDefault").value = "";
    document.getElementById("propertyOptionsRow").style.display = "none";
    document.getElementById("propertyDialog").style.display = "flex";
  }

  saveProperty() {
    if (this.selectedSteps.length !== 1) return;

    const step = this.selectedSteps[0];
    const name = document.getElementById("propertyName").value.trim();
    const value = document.getElementById("propertyDefault").value;

    if (name) {
      step.properties[name] = value;
      document.getElementById("propertyDialog").style.display = "none";
      this.updatePropertyPanel();
      this.saveCurrentState();
    }
  }

  zoom(delta) {
    this.scale = Math.max(0.1, Math.min(3, this.scale + delta));
    this.applyZoom();
  }

  applyZoom() {
    this.canvas.style.transform = `scale(${this.scale})`;
  }

  save() {
    const flowData = {
      steps: this.steps.map((step) => step.toJSON()),
      pipes: this.pipes.map((pipe) => pipe.toJSON()),
      scale: this.scale,
    };

    const flowJSON = document.getElementById("flowJSON");
    const saveDialog = document.getElementById("saveDialog");
    
    if (flowJSON && saveDialog) {
      flowJSON.value = JSON.stringify(flowData, null, 2);
      saveDialog.style.display = "flex";
    } else {
      console.error("Save dialog elements not found in the DOM");
    }
  }
  
  showLoadDialog() {
    const loadJSON = document.getElementById("loadJSON");
    const loadFile = document.getElementById("loadFile");
    const loadDialog = document.getElementById("loadDialog");
    
    if (loadJSON && loadFile && loadDialog) {
      loadJSON.value = "";
      loadFile.value = "";
      loadDialog.style.display = "flex";
    } else {
      console.error("Load dialog elements not found in the DOM");
    }
  }

  downloadFlow() {
    const flowName = document.getElementById("flowName").value.trim() || "flow";
    const flowData = document.getElementById("flowJSON").value;

    const blob = new Blob([flowData], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${flowName}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  loadFlowFromDialog() {
    const fileInput = document.getElementById("loadFile");
    const jsonInput = document.getElementById("loadJSON");

    if (fileInput.files.length > 0) {
      const reader = new FileReader();
      reader.onload = (e) => {
        try {
          const data = JSON.parse(e.target.result);
          this.loadState(data);
          document.getElementById("loadDialog").style.display = "none";
        } catch (error) {
          alert("Error loading file: " + error.message);
        }
      };
      reader.readAsText(fileInput.files[0]);
    } else if (jsonInput.value.trim()) {
      try {
        const data = JSON.parse(jsonInput.value);
        this.loadState(data);
        document.getElementById("loadDialog").style.display = "none";
      } catch (error) {
        alert("Error parsing JSON: " + error.message);
      }
    }
  }

  loadState(data, saveToHistory = true) {
    this.clear(false);

    // First create all steps
    const stepMap = {};
    for (const stepData of data.steps) {
      const step = new Step(
        stepData.id,
        stepData.type,
        stepData.x,
        stepData.y,
        this
      );
      step.name = stepData.name;
      step.width = stepData.width;
      step.height = stepData.height;
      step.color = stepData.color;
      step.inputs = stepData.inputs;
      step.outputs = stepData.outputs;
      step.properties = stepData.properties || {};
      step.script = stepData.script || "";

      step.render();
      this.steps.push(step);
      stepMap[step.id] = step;
    }

    // Then create all pipes
    for (const pipeData of data.pipes) {
      const sourceStep = stepMap[pipeData.sourceStep];
      const targetStep = stepMap[pipeData.targetStep];

      if (sourceStep && targetStep) {
        const pipe = new Pipe(
          pipeData.id,
          sourceStep,
          pipeData.sourceOutput,
          targetStep,
          pipeData.targetInput,
          this
        );

        // Update loadState method to include pipe properties and color
        // Inside the pipe creation in loadState:
        if (pipeData.properties) {
          pipe.properties = pipeData.properties;
        }
        if (pipeData.color) {
          pipe.color = pipeData.color;
        }
        if (pipeData.name) {
          pipe.name = pipeData.name;
        }        
        if (pipeData.type) {
          pipe.type = pipeData.type;
        }
        if (pipeData.script) {
          pipe.script = pipeData.script;
        }
          pipe.render();
        this.pipes.push(pipe);
      }
    }

    // Set zoom level if available
    if (data.scale) {
      this.scale = data.scale;
      this.applyZoom();
    }

    if (saveToHistory) {
      this.saveCurrentState();
    }
  }

  clear(saveToHistory = true) {
    // Remove all steps and pipes
    this.steps.forEach((step) => {
      if (step.element) step.element.remove();
    });

    this.pipes.forEach((pipe) => {
      pipe.remove();
    });

    this.steps = [];
    this.pipes = [];
    this.selectedSteps = [];
    this.propertyPanel.classList.remove("visible");

    if (saveToHistory) {
      this.historyManager.clear();
      this.saveCurrentState();
    }
  }

  saveCurrentState() {
    const state = {
      steps: this.steps.map((step) => step.toJSON()),
      pipes: this.pipes.map((pipe) => pipe.toJSON()),
      scale: this.scale,
    };

    this.historyManager.pushState(state);
    this.updateUndoRedoButtons();
  }

  updateUndoRedoButtons() {
    document.getElementById("undoBtn").disabled =
      !this.historyManager.canUndo();
    document.getElementById("redoBtn").disabled =
      !this.historyManager.canRedo();
  }
}