// HistoryManager for undo/redo functionality
class HistoryManager {
  constructor(editor) {
    this.editor = editor;
    this.undoStack = [];
    this.redoStack = [];
    this.maxSteps = 50;
    this.isPerformingUndoRedo = false;
  }

  pushState(state) {
    if (this.isPerformingUndoRedo) return;

    this.undoStack.push(JSON.stringify(state));
    if (this.undoStack.length > this.maxSteps) {
      this.undoStack.shift();
    }
    this.redoStack = [];
  }

  undo() {
    if (this.undoStack.length <= 1) return;

    this.isPerformingUndoRedo = true;
    const currentState = this.undoStack.pop();
    this.redoStack.push(currentState);
    const previousState = this.undoStack[this.undoStack.length - 1];

    this.editor.loadState(JSON.parse(previousState), false);
    this.isPerformingUndoRedo = false;
  }

  redo() {
    if (this.redoStack.length === 0) return;

    this.isPerformingUndoRedo = true;
    const nextState = this.redoStack.pop();
    this.undoStack.push(nextState);

    this.editor.loadState(JSON.parse(nextState), false);
    this.isPerformingUndoRedo = false;
  }

  canUndo() {
    return this.undoStack.length > 1;
  }

  canRedo() {
    return this.redoStack.length > 0;
  }

  clear() {
    this.undoStack = [];
    this.redoStack = [];
  }
}
