// Step class - represents a node in the workflow
class Step {
  constructor(id, type, x, y, editor) {
    this.id = id || generateId();
    this.type = type;
    this.name = type.charAt(0).toUpperCase() + type.slice(1);
    this.x = x;
    this.y = y;
    this.width = 150;
    this.height = 100;
    this.editor = editor;
    this.color = STEP_COLORS[type] || "#333";
    this.inputs = [];
    this.outputs = [];
    this.properties = {};
    this.script = "";
    this.element = null;

    switch(type) {
      case "timer":
        this.outputs.push({ id: generateId(), name: "trigger" });
        this.properties = {
          interval_milliseconds: 1000
        };
        break;
      case "opcuasource":
        this.inputs.push({ id: generateId(), name: "trigger" });
        this.outputs.push({ id: generateId(), name: "message" });
        this.properties = {
          source_uri: "opc.tcp://localhost:4840",
          node_ids: ["ns=1;s=Temperature"]
        };
        break;
      case "processor":
        this.inputs.push({ id: generateId(), name: "message-in" });
        this.outputs.push({ id: generateId(), name: "message-out" });
        break;
      case "mqttsender":
        this.inputs.push({ id: generateId(), name: "message-in" });
        this.properties = {
          target_uri: "mqtt://localhost:1883",
          topic: "test/nodes"
        };
        break;
    }
  }


  render() {
    // Create or update the DOM element for this step
    if (!this.element) {
      this.element = document.createElement("div");
      this.element.className = "step";
      this.element.id = this.id;
      this.editor.canvas.appendChild(this.element);

      // Add event listeners
      this.element.addEventListener("mousedown", (e) => {
        if (e.target.classList.contains("connector")) return;
        this.editor.selectStep(this, e.ctrlKey);
        this.editor.startDrag(e);
      });
    }

    // Update position and size
    this.element.style.left = `${this.x}px`;
    this.element.style.top = `${this.y}px`;
    this.element.style.width = `${this.width}px`;
    this.element.style.height = `${this.height}px`;

    // Create the header, body and footer
    this.element.innerHTML = `
                    <div class="step-header" style="background-color: ${
                      this.color
                    }">
                        <span>${this.name}</span>
                    </div>
                    <div class="step-body">
                        ${this.inputs
                          .map(
                            (input) => `
                            <div class="connector input-connector" 
                                 id="input-${input.id}" 
                                 data-id="${input.id}" 
                                 data-type="input"
                                 title="${input.name}"
                                 style="top: ${this.getInputPosition(
                                   input
                                 )}px"></div>
                        `
                          )
                          .join("")}
                        ${this.outputs
                          .map(
                            (output) => `
                            <div class="connector output-connector" 
                                 id="output-${output.id}" 
                                 data-id="${output.id}" 
                                 data-type="output"
                                 title="${output.name}"
                                 style="top: ${this.getOutputPosition(
                                   output
                                 )}px"></div>
                        `
                          )
                          .join("")}
                    </div>
                    <div class="step-footer">${this.type}</div>
                    <div class="resize-handle"></div>
                `;

    // Add event listeners for connectors
    this.element.querySelectorAll(".connector").forEach((connector) => {
      connector.addEventListener("mousedown", (e) => {
        e.stopPropagation();
        const connectorId = connector.dataset.id;
        const connectorType = connector.dataset.type;
        this.editor.startConnection(this, connectorId, connectorType);
      });
    });

    // Add resize handler
    const resizeHandle = this.element.querySelector(".resize-handle");
    resizeHandle.addEventListener("mousedown", (e) => {
      e.stopPropagation();
      this.editor.startResize(this, e);
    });
  }

  getInputPosition(input) {
    const index = this.inputs.findIndex((i) => i.id === input.id);
    const total = this.inputs.length;
    const step = this.height / (total + 1);
    return (index + 1) * step;
  }

  getOutputPosition(output) {
    const index = this.outputs.findIndex((o) => o.id === output.id);
    const total = this.outputs.length;
    const step = this.height / (total + 1);
    return (index + 1) * step;
  }

  getConnectorPosition(connectorId, type) {
    const connector = document.getElementById(`${type}-${connectorId}`);
    if (!connector) return null;

    const rect = connector.getBoundingClientRect();
    const canvasRect = this.editor.canvas.getBoundingClientRect();

    return {
      x:
        rect.left +
        rect.width / 2 -
        canvasRect.left +
        this.editor.canvas.scrollLeft,
      y:
        rect.top +
        rect.height / 2 -
        canvasRect.top +
        this.editor.canvas.scrollTop,
    };
  }

  updateProperties(props) {
    Object.assign(this, props);
    this.render();
    this.editor.updatePipes();
  }

  toJSON() {
    return {
      id: this.id,
      type: this.type,
      name: this.name,
      x: this.x,
      y: this.y,
      width: this.width,
      height: this.height,
      color: this.color,
      inputs: this.inputs,
      outputs: this.outputs,
      properties: this.properties,
      script: this.script,
    };
  }
}
