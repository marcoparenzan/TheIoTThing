async function loadAllTemplates(mappings) {
  const promises = Object.entries(mappings).map(([elementId, templateFile]) => {
    return loadTemplateToElement(elementId, templateFile);
  });
  
  await Promise.all(promises);
}

async function loadTemplateToElement(elementId, templateFile) {
  try {
    const newElement = document.createElement('div');
    newElement.id = elementId;
    newElement.classList.add('dialog-overlay');
    document.body.appendChild(newElement);
    
    // Continue with the newly created element
    const content = await fetchTemplate(templateFile);
    newElement.innerHTML = content;
  } catch (error) {
    console.error(`Error loading template "${templateFile}"`, error);
  }
}

async function fetchTemplate(templateName) {
  const response = await fetch(`templates/${templateName}.html`);
  if (!response.ok) {
    throw new Error(`Failed to load template: ${templateName}`);
  }
  return await response.text();
}

function initializeDialogEvents() {
  // Close buttons
  document.querySelectorAll('.dialog-close, [data-close]').forEach(button => {
    button.addEventListener('click', function() {
      const dialogId = this.getAttribute('data-close');
      if (dialogId) {
        document.getElementById(dialogId).style.display = 'none';
      } else {
        const dialog = this.closest('.dialog-overlay');
        if (dialog) dialog.style.display = 'none';
      }
    });
  });
  
  // Re-initialize other dialog-specific event handlers
  const saveScriptBtn = document.getElementById('saveScriptBtn');
  if (saveScriptBtn) {
    saveScriptBtn.addEventListener('click', function() {
      document.getElementById('scriptDialog').style.display = 'none';
    });
  }
}

// Initialize the editor when the page loads
document.addEventListener("DOMContentLoaded", async () => {

    // Define the template mappings (element ID -> template file name)
  const templateMappings = {
    'scriptDialog': 'script-dialog',
    'propertyDialog': 'property-dialog',
    'saveDialog': 'save-dialog',
    'loadDialog': 'load-dialog'
  };

  try {
    // Load all templates and replace the content of corresponding elements
    await loadAllTemplates(templateMappings);
    
    // Re-initialize event listeners for elements loaded from templates
    initializeDialogEvents();
  } catch (error) {
    console.error('Error loading templates:', error);
  }
  
  window.flowEditor = new FlowEditor("canvas");
});

