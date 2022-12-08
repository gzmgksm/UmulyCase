﻿/**
 * Bootstrap 5 autocomplete
 */

// #region config

/**
 * @typedef Config
 * @property {Boolean} showAllSuggestions Show all suggestions even if they don't match
 * @property {Number} suggestionsThreshold Number of chars required to show suggestions
 * @property {Number} maximumItems Maximum number of items to display
 * @property {Boolean} autoselectFirst Always select the first item
 * @property {Boolean} updateOnSelect Update input value on selection (doesn't play nice with autoselectFirst)
 * @property {Boolean} highlightTyped Highlight matched part of the label
 * @property {Boolean} fullWidth Match the width on the input field
 * @property {String} labelField Key for the label
 * @property {String} valueField Key for the value
 * @property {Array|Object} items An array of label/value objects or an object with key/values
 * @property {Function} source A function that provides the list of items
 * @property {String} datalist The id of the source datalist
 * @property {String} server Endpoint for data provider
 * @property {String|Object} serverParams Parameters to pass along to the server
 * @property {Boolean} liveServer Should the endpoint be called each time on input
 * @property {Boolean} noCache Prevent caching by appending a timestamp
 * @property {Boolean} debounceTime Debounce time for live server
 * @property {String} notFoundMessage Display a no suggestions found message. Leave empty to disable
 * @property {Function} onRenderItem Callback function that returns the label
 * @property {Function} onSelectItem Callback function to call on selection
 * @property {Function} onServerResponse Callback function to process server response
 */

/**
 * @type {Config}
 */
const DEFAULTS = {
    showAllSuggestions: false,
    suggestionsThreshold: 1,
    maximumItems: 0,
    autoselectFirst: true,
    updateOnSelect: false,
    highlightTyped: false,
    fullWidth: false,
    labelField: "label",
    valueField: "value",
    items: [],
    source: null,
    datalist: "",
    server: "",
    serverParams: {},
    liveServer: false,
    noCache: true,
    debounceTime: 300,
    notFoundMessage: "",
    onRenderItem: (item, label) => {
        return label;
    },
    onSelectItem: (item) => { },
    onServerResponse: (response) => {
        return response.json();
    },
};

// #endregion

// #region constants

const LOADING_CLASS = "is-loading";
const ACTIVE_CLASS = "is-active";
const ACTIVE_CLASSES = ["is-active", "bg-primary", "text-white"];
const NEXT = "next";
const PREV = "prev";

const INSTANCE_MAP = new WeakMap();
let counter = 0;

// #endregion

// #region functions

/**
 * @param {Function} func
 * @param {number} timeout
 * @returns {Function}
 */
function debounce(func, timeout = 300) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            func.apply(this, args);
        }, timeout);
    };
}

/**
 * @param {String} str
 * @returns {String}
 */
function removeDiacritics(str) {
    return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
}

/**
 * @param {HTMLElement} el
 * @param {HTMLElement} newEl
 * @returns {HTMLElement}
 */
function insertAfter(el, newEl) {
    return el.parentNode.insertBefore(newEl, el.nextSibling);
}

// #endregion

class Autocomplete {
    /**
     * @param {HTMLInputElement} el
     * @param {Config} config
     */
    constructor(el, config = {}) {
        INSTANCE_MAP.set(el, this);
        counter++;
        this._searchInput = el;

        this._configure(config);

        // Private vars
        this._preventInput = false;
        this._keyboardNavigation = false;
        this._searchFunc = debounce(() => {
            this._loadFromServer(true);
        }, this._config.debounceTime);

        // Create html
        this._configureSearchInput();
        this._configureDropElement();

        // Add listeners (remove then on dispose()). See handleEvent.
        this._searchInput.addEventListener("focus", this);
        this._searchInput.addEventListener("blur", this);
        this._searchInput.addEventListener("input", this);
        this._searchInput.addEventListener("keydown", this);
        this._dropElement.addEventListener("mousemove", this);

        this._fetchData();
    }

    // #region Core

    /**
     * Attach to all elements matched by the selector
     * @param {string} selector
     * @param {Object} opts
     */
    static init(selector = "input.autocomplete", config = {}) {
        document.querySelectorAll(selector).forEach((el) => {
            this.getOrCreateInstance(el, config);
        });
    }

    /**
     * @param {HTMLElement} el
     */
    static getInstance(el) {
        return INSTANCE_MAP.has(el) ? INSTANCE_MAP.get(el) : null;
    }

    /**
     * @param {HTMLElement} el
     * @param {Object} config
     */
    static getOrCreateInstance(el, config = {}) {
        return this.getInstance(el) || new this(el, config);
    }

    dispose() {
        this._searchInput.removeEventListener("focus", this);
        this._searchInput.removeEventListener("blur", this);
        this._searchInput.removeEventListener("input", this);
        this._searchInput.removeEventListener("keydown", this);
        this._dropElement.removeEventListener("mousemove", this);

        this._dropElement.parentElement.removeChild(this._dropElement);

        INSTANCE_MAP.delete(this._searchInput);
    }

    /**
     * @link https://gist.github.com/WebReflection/ec9f6687842aa385477c4afca625bbf4#handling-events
     * @param {Event} event
     */
    handleEvent(event) {
        this[`on${event.type}`](event);
    }

    /**
     * @param {Config} config
     */
    _configure(config = {}) {
        this._config = Object.assign({}, DEFAULTS);

        // Handle options, using arguments first and data attr as override
        const o = { ...config, ...this._searchInput.dataset };

        // Allow 1/0, true/false as strings
        const parseBool = (value) => ["true", "false", "1", "0", true, false].includes(value) && !!JSON.parse(value);

        // Typecast provided options based on defaults types
        for (const [key, defaultValue] of Object.entries(DEFAULTS)) {
            // Check for undefined keys
            if (o[key] === void 0) {
                continue;
            }
            const value = o[key];
            switch (typeof defaultValue) {
                case "number":
                    this._config[key] = parseInt(value);
                    break;
                case "boolean":
                    this._config[key] = parseBool(value);
                    break;
                case "string":
                    this._config[key] = value.toString();
                    break;
                case "object":
                    // Arrays have a type object in js
                    if (Array.isArray(defaultValue)) {
                        if (typeof value === "string") {
                            const separator = value.includes("|") ? "|" : ",";
                            this._config[key] = value.split(separator);
                        } else {
                            this._config[key] = value;
                        }
                    } else {
                        this._config[key] = typeof value === "string" ? JSON.parse(value) : value;
                    }
                    break;
                case "function":
                    this._config[key] = typeof value === "string" ? window[value] : value;
                    break;
                default:
                    this._config[key] = value;
                    break;
            }
        }
    }

    // #endregion

    // #region Html

    _configureSearchInput() {
        this._searchInput.autocomplete = "off";
        this._searchInput.spellcheck = false;
        // @link https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Attributes/aria-autocomplete
        this._searchInput.ariaAutoComplete = "list";
        // @link https://developer.mozilla.org/en-US/docs/Web/Accessibility/ARIA/Attributes/aria-expanded
        // use the aria-expanded state on the element with role combobox to communicate that the list is displayed.
        this._searchInput.ariaExpanded = "false";
        // include aria-haspopup matching the role of the element that contains the collection of suggested values.
        this._searchInput.ariaHasPopup = "menu";
        this._searchInput.setAttribute("role", "combobox");
    }

    _configureDropElement() {
        this._dropElement = document.createElement("ul");
        this._dropElement.setAttribute("id", "ac-menu-" + counter);
        this._dropElement.setAttribute("role", "menu");
        this._dropElement.classList.add(...["dropdown-menu", "autocomplete-menu", "p-0"]);
        this._dropElement.style.maxHeight = "280px";
        if (!this._config.fullWidth) {
            this._dropElement.style.maxWidth = "360px";
        }
        this._dropElement.style.overflowY = "auto";

        insertAfter(this._searchInput, this._dropElement);
        // include aria-controls with the value of the id of the suggested list of values.
        this._searchInput.setAttribute("aria-controls", this._dropElement.getAttribute("id"));
    }

    // #endregion

    // #region Events

    oninput(e) {
        if (this._preventInput) {
            return;
        }
        this._showOrSearch();
    }

    onblur(e) {
        this._hideSuggestions();
    }

    onfocus(e) {
        this._showOrSearch();
    }

    /**
     * keypress doesn't send arrow keys, so we use keydown
     * @param {KeyboardEvent} e
     */
    onkeydown(e) {
        const key = e.keyCode || e.key;
        switch (key) {
            case 13:
            case "Enter":
                e.preventDefault();
                const selection = this.getSelection();
                if (selection) {
                    selection.click();
                }
                break;
            case 38:
            case "ArrowUp":
                e.preventDefault();
                this._keyboardNavigation = true;
                this._moveSelection(PREV);
                break;
            case 40:
            case "ArrowDown":
                e.preventDefault();
                this._keyboardNavigation = true;
                this._moveSelection(NEXT);
                break;
            case 27:
            case "Escape":
                this._searchInput.focus();
                this._hideSuggestions();
                break;
        }
    }

    onmousemove(e) {
        // Moving the mouse means no longer using keyboard
        this._keyboardNavigation = false;
    }

    // #endregion

    // #region Api

    /**
     * @param {String} k
     * @returns {Config}
     */
    getConfig(k = null) {
        if (k !== null) {
            return this._config[k];
        }
        return this._config;
    }

    /**
     * @param {String} k
     * @param {*} v
     */
    setConfig(k, v) {
        this._config[k] = v;
    }

    setData(src) {
        this._items = {};
        this._addItems(src);
    }

    enable() {
        this._searchInput.setAttribute("disabled", "");
    }

    disable() {
        this._searchInput.removeAttribute("disabled");
    }

    /**
     * @returns {boolean}
     */
    isDisabled() {
        return this._searchInput.hasAttribute("disabled") || this._searchInput.disabled || this._searchInput.hasAttribute("readonly");
    }

    // #endregion

    // #region Selection management

    /**
     * @returns {HTMLElement}
     */
    getSelection() {
        return this._dropElement.querySelector("a." + ACTIVE_CLASS);
    }

    removeSelection() {
        const selection = this.getSelection();
        if (selection) {
            selection.classList.remove(...ACTIVE_CLASSES);
        }
    }

    /**
     * @param {String} dir
     * @returns {HTMLElement}
     */
    _moveSelection(dir = NEXT) {
        const active = this.getSelection();
        let sel = null;

        // select first li
        if (!active) {
            sel = this._dropElement.firstChild;
        } else {
            const sibling = dir === NEXT ? "nextSibling" : "previousSibling";

            // Iterate over visible li
            sel = active.parentNode;
            do {
                sel = sel[sibling];
            } while (sel && sel.style.display == "none");

            // We have a new selection
            if (sel) {
                // Change classes
                active.classList.remove(...ACTIVE_CLASSES);

                // Scroll if necessary
                if (dir === PREV) {
                    // Don't use scrollIntoView as it scrolls the whole window
                    sel.parentNode.scrollTop = sel.offsetTop - sel.parentNode.offsetTop;
                } else {
                    // This is the equivalent of scrollIntoView(false) but only for parent node
                    if (sel.offsetTop > sel.parentNode.offsetHeight - sel.offsetHeight) {
                        sel.parentNode.scrollTop += sel.offsetHeight;
                    }
                }
            } else if (active) {
                sel = active.parentElement;
            }
        }

        if (sel) {
            const a = sel.querySelector("a");
            a.classList.add(...ACTIVE_CLASSES);
            this._searchInput.setAttribute("aria-activedescendant", a.getAttribute("id"));
            if (this._config.updateOnSelect) {
                this._searchInput.value = a.dataset.label;
            }
        } else {
            this._searchInput.setAttribute("aria-activedescendant", "");
        }
        return sel;
    }

    // #endregion

    // #region Implementation

    /**
     * Do we have enough input to show suggestions ?
     * @returns {Boolean}
     */
    _shouldShow() {
        if (this.isDisabled()) {
            return false;
        }
        return this._searchInput.value.length >= this._config.suggestionsThreshold;
    }

    /**
     * Show suggestions or load them
     */
    _showOrSearch() {
        if (!this._shouldShow()) {
            this._hideSuggestions();
            return;
        }
        if (this._config.liveServer) {
            this._searchFunc();
        } else if (this._config.source) {
            this._config.source(this._searchInput.value, (items) => {
                this.setData(items);
                this._showSuggestions();
            });
        } else {
            this._showSuggestions();
        }
    }

    /**
     * @param {String} lookup
     * @param {Object} item
     * @returns {HTMLElement}
     */
    _createItem(lookup, item) {
        let label = item.label;

        if (this._config.highlightTyped) {
            const idx = removeDiacritics(label).toLowerCase().indexOf(lookup);
            label =
                label.substring(0, idx) +
                `<mark>${label.substring(idx, idx + lookup.length)}</mark>` +
                label.substring(idx + lookup.length, label.length);
        }

        label = this._config.onRenderItem(item, label);

        const newChild = document.createElement("li");
        newChild.setAttribute("role", "presentation");
        const newChildLink = document.createElement("a");
        newChild.append(newChildLink);
        newChildLink.setAttribute("id", this._dropElement.getAttribute("id") + "-" + this._dropElement.children.length);
        newChildLink.classList.add(...["dropdown-item", "text-truncate"]);
        newChildLink.setAttribute("data-value", item.value);
        newChildLink.setAttribute("data-label", item.label);
        // Behave like a datalist (tab doesn't allow item selection)
        // @link https://developer.mozilla.org/en-US/docs/Web/HTML/Element/datalist
        newChildLink.setAttribute("tabindex", "-1");
        newChildLink.setAttribute("role", "menuitem");
        newChildLink.setAttribute("href", "#");
        newChildLink.innerHTML = label;
        if (item.data) {
            for (const [key, value] of Object.entries(item.data)) {
                newChildLink.dataset[key] = value;
            }
        }

        // Hover sets active item
        newChildLink.addEventListener("mouseenter", (event) => {
            // Don't trigger enter if using arrows
            if (this._keyboardNavigation) {
                return;
            }
            this.removeSelection();
            newChild.querySelector("a").classList.add(...ACTIVE_CLASSES);
        });
        // Prevent searchInput losing focus and close the menu
        newChildLink.addEventListener("mousedown", (event) => {
            event.preventDefault();
        });
        // Apply value
        newChildLink.addEventListener("click", (event) => {
            event.preventDefault();

            // Prevent input otherwise it might trigger autocomplete due to value change
            this._preventInput = true;
            this._searchInput.value = item.label;
            this._config.onSelectItem(item);
            this._hideSuggestions();
            this._preventInput = false;
        });

        return newChild;
    }

    /**
     * Show drop menu with suggestions
     */
    _showSuggestions() {
        const lookup = removeDiacritics(this._searchInput.value).toLowerCase();
        this._dropElement.innerHTML = "";

        const keys = Object.keys(this._items);
        let count = 0;
        let firstItem = null;
        for (let i = 0; i < keys.length; i++) {
            const key = keys[i];
            const entry = this._items[key];

            const text = removeDiacritics(entry.label).toLowerCase();
            const isMatched = lookup.length > 0 ? text.indexOf(lookup) >= 0 : true;
            if (this._config.showAllSuggestions || isMatched) {
                count++;
                const newItem = this._createItem(lookup, entry);
                if (!firstItem) {
                    firstItem = newItem;
                }
                this._dropElement.appendChild(newItem);
                if (this._config.maximumItems > 0 && count >= this._config.maximumItems) {
                    break;
                }
            }
        }

        if (firstItem && this._config.autoselectFirst) {
            this._moveSelection(NEXT);
        }

        if (count === 0) {
            if (this._config.notFoundMessage) {
                const newChild = document.createElement("li");
                newChild.setAttribute("role", "presentation");
                newChild.innerHTML = `<span class="dropdown-item">${this._config.notFoundMessage}</span>`;
                this._dropElement.appendChild(newChild);
            } else {
                // Remove dropdown if not found
                this._hideSuggestions();
            }
        } else {
            // Or show it if necessary
            this._dropElement.classList.add("show");
            this._searchInput.ariaExpanded = "true";
            this._positionMenu();
        }
    }

    /**
     * Hide the dropdown menu
     */
    _hideSuggestions() {
        this._dropElement.classList.remove("show");
        this._searchInput.ariaExpanded = "false";
        this.removeSelection();
    }

    /**
     * Position the dropdown menu
     */
    _positionMenu() {
        if (this._config.fullWidth) {
            // Use full input width
            this._dropElement.style.left = this._searchInput.offsetLeft + "px";
            this._dropElement.style.width = this._searchInput.offsetWidth + "px";
        } else {
            // Position next to search input
            let left = this._searchInput.offsetLeft;

            // Overflow right
            const w = document.body.offsetWidth - 1; // avoid rounding issues
            const scrollbarOffset = 30; // scrollbars are not taken into account
            const wdiff = w - (left + this._dropElement.offsetWidth) - scrollbarOffset;

            // If the dropdowns goes out of the viewport, remove the diff from the left position
            if (wdiff < 0) {
                left = left + wdiff;
            }
            this._dropElement.style.left = left + "px";
        }

        // Overflow bottom
        const h = document.body.offsetHeight;
        const bottom = this._searchInput.getBoundingClientRect().y + window.pageYOffset + this._dropElement.offsetHeight;

        const hdiff = h - bottom;
        if (hdiff < 0) {
            // We display above input
            this._dropElement.style.transform = "translateY(calc(-100% - " + this._searchInput.offsetHeight + "px))";
        } else {
            this._dropElement.style.transform = "none";
        }
    }

    _fetchData() {
        this._items = {};

        // From an array of items or an object
        this._addItems(this._config.items);

        // From a datalist
        if (this._config.datalist) {
            const datalist = document.querySelector(`#${this._config.datalist}`);
            if (datalist) {
                const items = Array.from(datalist.children).map((o) => {
                    const value = o.getAttribute("value") ?? o.innerHTML.toLowerCase();
                    const label = o.innerHTML;

                    return {
                        value: value,
                        label: label,
                    };
                });
                this._addItems(items);
            }
        }

        // From an external source
        if (this._config.server && !this._config.liveServer) {
            this._loadFromServer();
        }
    }

    _addItems(src) {
        const keys = Object.keys(src);
        for (let i = 0; i < keys.length; i++) {
            const key = keys[i];
            const entry = src[key];
            const item = typeof entry === "string" ? {} : entry;

            // Normalize entry
            item.label = entry[this._config.labelField] ?? entry;
            item.value = entry[this._config.valueField] ?? key;
            this._items[item.value] = item;
        }
    }

    /**
     * @param {boolean} show
     */
    _loadFromServer(show = false) {
        if (this._abortController) {
            this._abortController.abort();
        }
        this._abortController = new AbortController();

        const params = Object.assign({}, this._config.serverParams);
        // Pass current value
        params.query = this._searchInput.value;
        // Prevent caching
        if (this._config.noCache) {
            params.t = Date.now();
        }
        // We have a related field
        if (params.related) {
            const input = document.getElementById(params.related);
            if (input) {
                params.related = input.value;
            }
        }
        const urlParams = new URLSearchParams(params).toString();

        this._searchInput.classList.add(LOADING_CLASS);
        fetch(this._config.server + "?" + urlParams, { signal: this._abortController.signal })
            .then((r) => this._config.onServerResponse(r))
            .then((suggestions) => {
                const data = suggestions.data || suggestions;
                this.setData(data);
                this._abortController = null;
                if (show) {
                    this._showSuggestions();
                }
            })
            .catch((e) => {
                if (e.name === "AbortError") {
                    return;
                }
                console.error(e);
            })
            .finally((e) => {
                this._searchInput.classList.remove(LOADING_CLASS);
            });
    }

    // #endregion
}

export default Autocomplete;