const STEP_COLORS = {
  timer: "#3498db",
  opcuasource: "#2ecc71",
  processor: "#f39c12",
  mqttsender: "#9b59b6"
};

// Utility functions
function generateId() {
  return "id_" + Math.random().toString(36).substr(2, 9);
}
