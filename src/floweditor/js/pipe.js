// Pipe class - represents a connection between steps
class Pipe {
  constructor(id, sourceStep, sourceOutput, targetStep, targetInput, editor, name = '', type = '', script = '') {
    this.id = id || generateId();
    this.sourceStep = sourceStep;
    this.sourceOutput = sourceOutput;
    this.targetStep = targetStep;
    this.targetInput = targetInput;
    this.name = name;
    this.type = type;
    this.script = script

    this.editor = editor;
    this.element = null;
    // Add to the Pipe class constructor
    this.properties = {}; // Store custom properties
    this.color = "#666"; // Default color
    this.selected = false;
  }

  render() {
    if (!this.element) {
      this.element = document.createElementNS(
        "http://www.w3.org/2000/svg",
        "svg"
      );
      this.element.classList.add("pipe");
      this.element.id = this.id;
      this.element.setAttribute("width", "100%");
      this.element.setAttribute("height", "100%");
      this.editor.canvas.appendChild(this.element);

      // Add click event to select pipe
      this.element.addEventListener("click", (e) => {
        e.stopPropagation();
        this.editor.selectPipe(this);
      });
    }

    const source = this.sourceStep.getConnectorPosition(
      this.sourceOutput,
      "output"
    );
    const target = this.targetStep.getConnectorPosition(
      this.targetInput,
      "input"
    );

    if (!source || !target) return;

    const dx = Math.abs(target.x - source.x);
    const controlPointOffset = Math.min(100, dx * 0.8);

    const path = `M ${source.x} ${source.y} C ${
      source.x + controlPointOffset
    } ${source.y}, ${target.x - controlPointOffset} ${target.y}, ${target.x} ${
      target.y
    }`;

    this.element.innerHTML = "";
    const pathElement = document.createElementNS(
      "http://www.w3.org/2000/svg",
      "path"
    );
    pathElement.setAttribute("d", path);
    pathElement.setAttribute("fill", "none");
    pathElement.setAttribute("stroke", this.color);
    pathElement.setAttribute("stroke-width", this.selected ? "4" : "2");
    pathElement.setAttribute("pointer-events", "stroke"); // Make the path clickable

    if (this.selected) {
      pathElement.setAttribute(
        "filter",
        "drop-shadow(0 0 3px rgba(0,0,0,0.3))"
      );
    }

    this.element.appendChild(pathElement);

    // If the pipe has a name, add a label
    if (this.name && this.element) {
      const source = this.sourceStep.getConnectorPosition(this.sourceOutput, "output");
      const target = this.targetStep.getConnectorPosition(this.targetInput, "input");
      
      if (!source || !target) return;
      
      // Calculate position for the label (middle of the pipe)
      const labelX = (source.x + target.x) / 2;
      const labelY = (source.y + target.y) / 2 - 10; // Offset slightly above the pipe
      
      // Create or update text element for the name
      let textElement = this.element.querySelector('.pipe-label');
      if (!textElement) {
        textElement = document.createElementNS("http://www.w3.org/2000/svg", "text");
        textElement.classList.add('pipe-label');
        this.element.appendChild(textElement);
      }
      
      textElement.setAttribute("x", labelX);
      textElement.setAttribute("y", labelY);
      textElement.setAttribute("text-anchor", "middle");
      textElement.setAttribute("fill", this.selected ? "#000" : "#666");
      textElement.setAttribute("font-size", "12px");
      textElement.textContent = this.name;
      
      // Add type as a second line if available
      if (this.type) {
        let typeElement = this.element.querySelector('.pipe-type');
        if (!typeElement) {
          typeElement = document.createElementNS("http://www.w3.org/2000/svg", "text");
          typeElement.classList.add('pipe-type');
          this.element.appendChild(typeElement);
        }
        
        typeElement.setAttribute("x", labelX);
        typeElement.setAttribute("y", labelY + 15); // Position below the name
        typeElement.setAttribute("text-anchor", "middle");
        typeElement.setAttribute("fill", this.selected ? "#000" : "#888");
        typeElement.setAttribute("font-size", "10px");
        typeElement.textContent = `[${this.type}]`;
      }
    }
  }

  remove() {
    if (this.element) {
      this.element.remove();
    }
  }

  // Add to Pipe class
  updateProperties(props) {
    Object.assign(this, props);
    this.render();
  }

  // Modify Pipe toJSON method to include properties and color
  toJSON() {
    return {
      id: this.id,
      sourceStep: this.sourceStep.id,
      sourceOutput: this.sourceOutput,
      targetStep: this.targetStep.id,
      targetInput: this.targetInput,
      properties: this.properties,
      color: this.color,
      script: this.script,
      name: this.name,
      type: this.type
    };
  }
}