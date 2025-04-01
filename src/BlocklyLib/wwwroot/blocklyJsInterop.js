export function initWorkspace(name, elName) {
    debugger;

    if (window.blocklyLib == undefined) {
        window.blocklyLib = {};
        window.blocklyLib.toolbox = {
            // There are two kinds of toolboxes. The simpler one is a flyout toolbox.
            "kind": "categoryToolbox",
            // The contents is the blocks and other items that exist in your toolbox.
            "contents": [
                {
                    "kind": "category",
                    "name": "Opc/Ua Blocks",
                    "categorystyle": "logic_category",
                    "contents": [{
                        "type": "messagestatus",
                        "kind": "block"
                    }
                    ]
                },
                {
                    "kind": "category",
                    "name": "Opc/Ua Nodes",
                    "custom": "VARIABLE_DYNAMIC",
                    "categorystyle": "variable_category"
                }, {
                    "kind": "sep"
                },
                {
                    "kind": "category",
                    "name": "Variables",
                    "custom": "VARIABLE",
                    "categorystyle": "variable_category"
                }, {
                    "kind": "sep"
                }
                , {
                    "kind": "category",
                    "name": "Logic",
                    "categorystyle": "logic_category",
                    "contents": [
                        {
                            "type": "controls_if",
                            "kind": "block"
                        },
                        {
                            "type": "logic_compare",
                            "kind": "block",
                            "fields": {
                                "OP": "EQ"
                            }
                        },
                        {
                            "type": "logic_operation",
                            "kind": "block",
                            "fields": {
                                "OP": "AND"
                            }
                        },
                        {
                            "type": "logic_negate",
                            "kind": "block"
                        },
                        {
                            "type": "logic_boolean",
                            "kind": "block",
                            "fields": {
                                "BOOL": "TRUE"
                            }
                        },
                        {
                            "type": "logic_null",
                            "kind": "block",
                            "enabled": false
                        },
                        {
                            "type": "logic_ternary",
                            "kind": "block"
                        }
                    ]
                },
                {
                    "kind": "category",
                    "name": "Loops",
                    "categorystyle": "loop_category",
                    "contents": [
                        {
                            "type": "controls_repeat_ext",
                            "kind": "block",
                            "inputs": {
                                "TIMES": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 10
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "controls_repeat",
                            "kind": "block",
                            "enabled": false,
                            "fields": {
                                "TIMES": 10
                            }
                        },
                        {
                            "type": "controls_whileUntil",
                            "kind": "block",
                            "fields": {
                                "MODE": "WHILE"
                            }
                        },
                        {
                            "type": "controls_for",
                            "kind": "block",
                            "fields": {
                                "VAR": {
                                    "name": "i"
                                }
                            },
                            "inputs": {
                                "FROM": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                },
                                "TO": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 10
                                        }
                                    }
                                },
                                "BY": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "controls_forEach",
                            "kind": "block",
                            "fields": {
                                "VAR": {
                                    "name": "j"
                                }
                            }
                        },
                        {
                            "type": "controls_flow_statements",
                            "kind": "block",
                            "enabled": false,
                            "fields": {
                                "FLOW": "BREAK"
                            }
                        }
                    ]
                },
                {
                    "kind": "category",
                    "name": "Math",
                    "categorystyle": "math_category",
                    "contents": [
                        {
                            "type": "math_number",
                            "kind": "block",
                            "fields": {
                                "NUM": 123
                            }
                        },
                        {
                            "type": "math_arithmetic",
                            "kind": "block",
                            "fields": {
                                "OP": "ADD"
                            },
                            "inputs": {
                                "A": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                },
                                "B": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_single",
                            "kind": "block",
                            "fields": {
                                "OP": "ROOT"
                            },
                            "inputs": {
                                "NUM": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 9
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_trig",
                            "kind": "block",
                            "fields": {
                                "OP": "SIN"
                            },
                            "inputs": {
                                "NUM": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 45
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_constant",
                            "kind": "block",
                            "fields": {
                                "CONSTANT": "PI"
                            }
                        },
                        {
                            "type": "math_number_property",
                            "kind": "block",
                            "fields": {
                                "PROPERTY": "EVEN"
                            },
                            "inputs": {
                                "NUMBER_TO_CHECK": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 0
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_round",
                            "kind": "block",
                            "fields": {
                                "OP": "ROUND"
                            },
                            "inputs": {
                                "NUM": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 3.1
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_on_list",
                            "kind": "block",
                            "fields": {
                                "OP": "SUM"
                            }
                        },
                        {
                            "type": "math_modulo",
                            "kind": "block",
                            "inputs": {
                                "DIVIDEND": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 64
                                        }
                                    }
                                },
                                "DIVISOR": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 10
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_constrain",
                            "kind": "block",
                            "inputs": {
                                "VALUE": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 50
                                        }
                                    }
                                },
                                "LOW": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                },
                                "HIGH": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 100
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_random_int",
                            "kind": "block",
                            "inputs": {
                                "FROM": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                },
                                "TO": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 100
                                        }
                                    }
                                }
                            }
                        },
                        {
                            "type": "math_random_float",
                            "kind": "block"
                        },
                        {
                            "type": "math_atan2",
                            "kind": "block",
                            "inputs": {
                                "X": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                },
                                "Y": {
                                    "shadow": {
                                        "type": "math_number",
                                        "fields": {
                                            "NUM": 1
                                        }
                                    }
                                }
                            }
                        }
                    ]
                }
            ]
        };
    }

    if (window.blocklyLib[name] != undefined) return;
    window.blocklyLib[name] = {};

    window.blocklyLib[name].workspace = Blockly.inject(window.document.getElementById(elName), {
        toolbox: window.blocklyLib.toolbox,
        scrollbars: true,
        verticalLayout: true,
        horizontalLayout: false,
        toolboxPosition: "start"
    });

    Blockly.common.defineBlocksWithJsonArray([
        {
            "type": "messagestatus",
            "message0": "Power %1 Running %2",
            "args0": [
                {
                    "type": "input_value",
                    "name": "Power",
                    "check": "Boolean"
                },
                {
                    "type": "input_value",
                    "name": "Running",
                    "check": "Boolean"
                }
            ],
            "inputsInline": false,
            "previousStatement": null,
            "nextStatement": null,
            "colour": 330,
            "tooltip": "MessageStatus",
            "helpUrl": "https://fdddd.com/MessageStatus"
        }
    ]);
}

export function createVariable(name, varName) {
    debugger;
    window.blocklyLib[name].workspace.createVariable(varName);
}

export function loadWorkspace(name, json) {
    debugger;
    let state = JSON.parse(json);
    Blockly.serialization.workspaces.load(state, window.blocklyLib[name].workspace);
}

export function saveWorkspace(name) {
    debugger;
    let state = Blockly.serialization.workspaces.save(window.blocklyLib[name].workspace);
    let json = JSON.stringify(state);
    return json;
}
