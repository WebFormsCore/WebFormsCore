(function() {

//#region rolldown:runtime
	var __create = Object.create;
	var __defProp = Object.defineProperty;
	var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
	var __getOwnPropNames = Object.getOwnPropertyNames;
	var __getProtoOf = Object.getPrototypeOf;
	var __hasOwnProp = Object.prototype.hasOwnProperty;
	var __commonJSMin = (cb, mod) => () => (mod || cb((mod = { exports: {} }).exports, mod), mod.exports);
	var __copyProps = (to, from, except, desc) => {
		if (from && typeof from === "object" || typeof from === "function") {
			for (var keys = __getOwnPropNames(from), i = 0, n = keys.length, key; i < n; i++) {
				key = keys[i];
				if (!__hasOwnProp.call(to, key) && key !== except) {
					__defProp(to, key, {
						get: ((k) => from[k]).bind(null, key),
						enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable
					});
				}
			}
		}
		return to;
	};
	var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", {
		value: mod,
		enumerable: true
	}) : target, mod));

//#endregion

//#region node_modules/morphdom/src/morphAttrs.js
	var DOCUMENT_FRAGMENT_NODE$1 = 11;
	function morphAttrs(fromNode, toNode) {
		var toNodeAttrs = toNode.attributes;
		var attr;
		var attrName;
		var attrNamespaceURI;
		var attrValue;
		var fromValue;
		if (toNode.nodeType === DOCUMENT_FRAGMENT_NODE$1 || fromNode.nodeType === DOCUMENT_FRAGMENT_NODE$1) return;
		for (var i = toNodeAttrs.length - 1; i >= 0; i--) {
			attr = toNodeAttrs[i];
			attrName = attr.name;
			attrNamespaceURI = attr.namespaceURI;
			attrValue = attr.value;
			if (attrNamespaceURI) {
				attrName = attr.localName || attrName;
				fromValue = fromNode.getAttributeNS(attrNamespaceURI, attrName);
				if (fromValue !== attrValue) {
					if (attr.prefix === "xmlns") attrName = attr.name;
					fromNode.setAttributeNS(attrNamespaceURI, attrName, attrValue);
				}
			} else {
				fromValue = fromNode.getAttribute(attrName);
				if (fromValue !== attrValue) fromNode.setAttribute(attrName, attrValue);
			}
		}
		var fromNodeAttrs = fromNode.attributes;
		for (var d = fromNodeAttrs.length - 1; d >= 0; d--) {
			attr = fromNodeAttrs[d];
			attrName = attr.name;
			attrNamespaceURI = attr.namespaceURI;
			if (attrNamespaceURI) {
				attrName = attr.localName || attrName;
				if (!toNode.hasAttributeNS(attrNamespaceURI, attrName)) fromNode.removeAttributeNS(attrNamespaceURI, attrName);
			} else if (!toNode.hasAttribute(attrName)) fromNode.removeAttribute(attrName);
		}
	}

//#endregion
//#region node_modules/morphdom/src/util.js
	var range;
	var NS_XHTML = "http://www.w3.org/1999/xhtml";
	var doc = typeof document === "undefined" ? void 0 : document;
	var HAS_TEMPLATE_SUPPORT = !!doc && "content" in doc.createElement("template");
	var HAS_RANGE_SUPPORT = !!doc && doc.createRange && "createContextualFragment" in doc.createRange();
	function createFragmentFromTemplate(str) {
		var template = doc.createElement("template");
		template.innerHTML = str;
		return template.content.childNodes[0];
	}
	function createFragmentFromRange(str) {
		if (!range) {
			range = doc.createRange();
			range.selectNode(doc.body);
		}
		return range.createContextualFragment(str).childNodes[0];
	}
	function createFragmentFromWrap(str) {
		var fragment = doc.createElement("body");
		fragment.innerHTML = str;
		return fragment.childNodes[0];
	}
	/**
	* This is about the same
	* var html = new DOMParser().parseFromString(str, 'text/html');
	* return html.body.firstChild;
	*
	* @method toElement
	* @param {String} str
	*/
	function toElement(str) {
		str = str.trim();
		if (HAS_TEMPLATE_SUPPORT) return createFragmentFromTemplate(str);
		else if (HAS_RANGE_SUPPORT) return createFragmentFromRange(str);
		return createFragmentFromWrap(str);
	}
	/**
	* Returns true if two node's names are the same.
	*
	* NOTE: We don't bother checking `namespaceURI` because you will never find two HTML elements with the same
	*       nodeName and different namespace URIs.
	*
	* @param {Element} a
	* @param {Element} b The target element
	* @return {boolean}
	*/
	function compareNodeNames(fromEl, toEl) {
		var fromNodeName = fromEl.nodeName;
		var toNodeName = toEl.nodeName;
		var fromCodeStart, toCodeStart;
		if (fromNodeName === toNodeName) return true;
		fromCodeStart = fromNodeName.charCodeAt(0);
		toCodeStart = toNodeName.charCodeAt(0);
		if (fromCodeStart <= 90 && toCodeStart >= 97) return fromNodeName === toNodeName.toUpperCase();
		else if (toCodeStart <= 90 && fromCodeStart >= 97) return toNodeName === fromNodeName.toUpperCase();
		else return false;
	}
	/**
	* Create an element, optionally with a known namespace URI.
	*
	* @param {string} name the element name, e.g. 'div' or 'svg'
	* @param {string} [namespaceURI] the element's namespace URI, i.e. the value of
	* its `xmlns` attribute or its inferred namespace.
	*
	* @return {Element}
	*/
	function createElementNS(name, namespaceURI) {
		return !namespaceURI || namespaceURI === NS_XHTML ? doc.createElement(name) : doc.createElementNS(namespaceURI, name);
	}
	/**
	* Copies the children of one DOM element to another DOM element
	*/
	function moveChildren(fromEl, toEl) {
		var curChild = fromEl.firstChild;
		while (curChild) {
			var nextChild = curChild.nextSibling;
			toEl.appendChild(curChild);
			curChild = nextChild;
		}
		return toEl;
	}

//#endregion
//#region node_modules/morphdom/src/specialElHandlers.js
	function syncBooleanAttrProp$1(fromEl, toEl, name) {
		if (fromEl[name] !== toEl[name]) {
			fromEl[name] = toEl[name];
			if (fromEl[name]) fromEl.setAttribute(name, "");
			else fromEl.removeAttribute(name);
		}
	}
	var specialElHandlers_default = {
		OPTION: function(fromEl, toEl) {
			var parentNode = fromEl.parentNode;
			if (parentNode) {
				var parentName = parentNode.nodeName.toUpperCase();
				if (parentName === "OPTGROUP") {
					parentNode = parentNode.parentNode;
					parentName = parentNode && parentNode.nodeName.toUpperCase();
				}
				if (parentName === "SELECT" && !parentNode.hasAttribute("multiple")) {
					if (fromEl.hasAttribute("selected") && !toEl.selected) {
						fromEl.setAttribute("selected", "selected");
						fromEl.removeAttribute("selected");
					}
					parentNode.selectedIndex = -1;
				}
			}
			syncBooleanAttrProp$1(fromEl, toEl, "selected");
		},
		INPUT: function(fromEl, toEl) {
			syncBooleanAttrProp$1(fromEl, toEl, "checked");
			syncBooleanAttrProp$1(fromEl, toEl, "disabled");
			if (fromEl.value !== toEl.value) fromEl.value = toEl.value;
			if (!toEl.hasAttribute("value")) fromEl.removeAttribute("value");
		},
		TEXTAREA: function(fromEl, toEl) {
			var newValue = toEl.value;
			if (fromEl.value !== newValue) fromEl.value = newValue;
			var firstChild = fromEl.firstChild;
			if (firstChild) {
				var oldValue = firstChild.nodeValue;
				if (oldValue == newValue || !newValue && oldValue == fromEl.placeholder) return;
				firstChild.nodeValue = newValue;
			}
		},
		SELECT: function(fromEl, toEl) {
			if (!toEl.hasAttribute("multiple")) {
				var selectedIndex = -1;
				var i = 0;
				var curChild = fromEl.firstChild;
				var optgroup;
				var nodeName;
				while (curChild) {
					nodeName = curChild.nodeName && curChild.nodeName.toUpperCase();
					if (nodeName === "OPTGROUP") {
						optgroup = curChild;
						curChild = optgroup.firstChild;
					} else {
						if (nodeName === "OPTION") {
							if (curChild.hasAttribute("selected")) {
								selectedIndex = i;
								break;
							}
							i++;
						}
						curChild = curChild.nextSibling;
						if (!curChild && optgroup) {
							curChild = optgroup.nextSibling;
							optgroup = null;
						}
					}
				}
				fromEl.selectedIndex = selectedIndex;
			}
		}
	};

//#endregion
//#region node_modules/morphdom/src/morphdom.js
	var ELEMENT_NODE = 1;
	var DOCUMENT_FRAGMENT_NODE = 11;
	var TEXT_NODE = 3;
	var COMMENT_NODE = 8;
	function noop() {}
	function defaultGetNodeKey(node) {
		if (node) return node.getAttribute && node.getAttribute("id") || node.id;
	}
	function morphdomFactory(morphAttrs$1) {
		return function morphdom$1(fromNode, toNode, options) {
			if (!options) options = {};
			if (typeof toNode === "string") if (fromNode.nodeName === "#document" || fromNode.nodeName === "HTML" || fromNode.nodeName === "BODY") {
				var toNodeHtml = toNode;
				toNode = doc.createElement("html");
				toNode.innerHTML = toNodeHtml;
			} else toNode = toElement(toNode);
			else if (toNode.nodeType === DOCUMENT_FRAGMENT_NODE) toNode = toNode.firstElementChild;
			var getNodeKey = options.getNodeKey || defaultGetNodeKey;
			var onBeforeNodeAdded = options.onBeforeNodeAdded || noop;
			var onNodeAdded = options.onNodeAdded || noop;
			var onBeforeElUpdated = options.onBeforeElUpdated || noop;
			var onElUpdated = options.onElUpdated || noop;
			var onBeforeNodeDiscarded = options.onBeforeNodeDiscarded || noop;
			var onNodeDiscarded = options.onNodeDiscarded || noop;
			var onBeforeElChildrenUpdated = options.onBeforeElChildrenUpdated || noop;
			var skipFromChildren = options.skipFromChildren || noop;
			var addChild = options.addChild || function(parent, child) {
				return parent.appendChild(child);
			};
			var childrenOnly = options.childrenOnly === true;
			var fromNodesLookup = Object.create(null);
			var keyedRemovalList = [];
			function addKeyedRemoval(key) {
				keyedRemovalList.push(key);
			}
			function walkDiscardedChildNodes(node, skipKeyedNodes) {
				if (node.nodeType === ELEMENT_NODE) {
					var curChild = node.firstChild;
					while (curChild) {
						var key = void 0;
						if (skipKeyedNodes && (key = getNodeKey(curChild))) addKeyedRemoval(key);
						else {
							onNodeDiscarded(curChild);
							if (curChild.firstChild) walkDiscardedChildNodes(curChild, skipKeyedNodes);
						}
						curChild = curChild.nextSibling;
					}
				}
			}
			/**
			* Removes a DOM node out of the original DOM
			*
			* @param  {Node} node The node to remove
			* @param  {Node} parentNode The nodes parent
			* @param  {Boolean} skipKeyedNodes If true then elements with keys will be skipped and not discarded.
			* @return {undefined}
			*/
			function removeNode(node, parentNode, skipKeyedNodes) {
				if (onBeforeNodeDiscarded(node) === false) return;
				if (parentNode) parentNode.removeChild(node);
				onNodeDiscarded(node);
				walkDiscardedChildNodes(node, skipKeyedNodes);
			}
			function indexTree(node) {
				if (node.nodeType === ELEMENT_NODE || node.nodeType === DOCUMENT_FRAGMENT_NODE) {
					var curChild = node.firstChild;
					while (curChild) {
						var key = getNodeKey(curChild);
						if (key) fromNodesLookup[key] = curChild;
						indexTree(curChild);
						curChild = curChild.nextSibling;
					}
				}
			}
			indexTree(fromNode);
			function handleNodeAdded(el) {
				onNodeAdded(el);
				var curChild = el.firstChild;
				while (curChild) {
					var nextSibling = curChild.nextSibling;
					var key = getNodeKey(curChild);
					if (key) {
						var unmatchedFromEl = fromNodesLookup[key];
						if (unmatchedFromEl && compareNodeNames(curChild, unmatchedFromEl)) {
							curChild.parentNode.replaceChild(unmatchedFromEl, curChild);
							morphEl(unmatchedFromEl, curChild);
						} else handleNodeAdded(curChild);
					} else handleNodeAdded(curChild);
					curChild = nextSibling;
				}
			}
			function cleanupFromEl(fromEl, curFromNodeChild, curFromNodeKey) {
				while (curFromNodeChild) {
					var fromNextSibling = curFromNodeChild.nextSibling;
					if (curFromNodeKey = getNodeKey(curFromNodeChild)) addKeyedRemoval(curFromNodeKey);
					else removeNode(curFromNodeChild, fromEl, true);
					curFromNodeChild = fromNextSibling;
				}
			}
			function morphEl(fromEl, toEl, childrenOnly$1) {
				var toElKey = getNodeKey(toEl);
				if (toElKey) delete fromNodesLookup[toElKey];
				if (!childrenOnly$1) {
					if (onBeforeElUpdated(fromEl, toEl) === false) return;
					morphAttrs$1(fromEl, toEl);
					onElUpdated(fromEl);
					if (onBeforeElChildrenUpdated(fromEl, toEl) === false) return;
				}
				if (fromEl.nodeName !== "TEXTAREA") morphChildren(fromEl, toEl);
				else specialElHandlers_default.TEXTAREA(fromEl, toEl);
			}
			function morphChildren(fromEl, toEl) {
				var skipFrom = skipFromChildren(fromEl);
				var curToNodeChild = toEl.firstChild;
				var curFromNodeChild = fromEl.firstChild;
				var curToNodeKey;
				var curFromNodeKey;
				var fromNextSibling;
				var toNextSibling;
				var matchingFromEl;
				outer: while (curToNodeChild) {
					toNextSibling = curToNodeChild.nextSibling;
					curToNodeKey = getNodeKey(curToNodeChild);
					while (!skipFrom && curFromNodeChild) {
						fromNextSibling = curFromNodeChild.nextSibling;
						if (curToNodeChild.isSameNode && curToNodeChild.isSameNode(curFromNodeChild)) {
							curToNodeChild = toNextSibling;
							curFromNodeChild = fromNextSibling;
							continue outer;
						}
						curFromNodeKey = getNodeKey(curFromNodeChild);
						var curFromNodeType = curFromNodeChild.nodeType;
						var isCompatible = void 0;
						if (curFromNodeType === curToNodeChild.nodeType) {
							if (curFromNodeType === ELEMENT_NODE) {
								if (curToNodeKey) {
									if (curToNodeKey !== curFromNodeKey) if (matchingFromEl = fromNodesLookup[curToNodeKey]) if (fromNextSibling === matchingFromEl) isCompatible = false;
									else {
										fromEl.insertBefore(matchingFromEl, curFromNodeChild);
										if (curFromNodeKey) addKeyedRemoval(curFromNodeKey);
										else removeNode(curFromNodeChild, fromEl, true);
										curFromNodeChild = matchingFromEl;
									}
									else isCompatible = false;
								} else if (curFromNodeKey) isCompatible = false;
								isCompatible = isCompatible !== false && compareNodeNames(curFromNodeChild, curToNodeChild);
								if (isCompatible) morphEl(curFromNodeChild, curToNodeChild);
							} else if (curFromNodeType === TEXT_NODE || curFromNodeType == COMMENT_NODE) {
								isCompatible = true;
								if (curFromNodeChild.nodeValue !== curToNodeChild.nodeValue) curFromNodeChild.nodeValue = curToNodeChild.nodeValue;
							}
						}
						if (isCompatible) {
							curToNodeChild = toNextSibling;
							curFromNodeChild = fromNextSibling;
							continue outer;
						}
						if (curFromNodeKey) addKeyedRemoval(curFromNodeKey);
						else removeNode(curFromNodeChild, fromEl, true);
						curFromNodeChild = fromNextSibling;
					}
					if (curToNodeKey && (matchingFromEl = fromNodesLookup[curToNodeKey]) && compareNodeNames(matchingFromEl, curToNodeChild)) {
						if (!skipFrom) addChild(fromEl, matchingFromEl);
						morphEl(matchingFromEl, curToNodeChild);
					} else {
						var onBeforeNodeAddedResult = onBeforeNodeAdded(curToNodeChild);
						if (onBeforeNodeAddedResult !== false) {
							if (onBeforeNodeAddedResult) curToNodeChild = onBeforeNodeAddedResult;
							if (curToNodeChild.actualize) curToNodeChild = curToNodeChild.actualize(fromEl.ownerDocument || doc);
							addChild(fromEl, curToNodeChild);
							handleNodeAdded(curToNodeChild);
						}
					}
					curToNodeChild = toNextSibling;
					curFromNodeChild = fromNextSibling;
				}
				cleanupFromEl(fromEl, curFromNodeChild, curFromNodeKey);
				var specialElHandler = specialElHandlers_default[fromEl.nodeName];
				if (specialElHandler) specialElHandler(fromEl, toEl);
			}
			var morphedNode = fromNode;
			var morphedNodeType = morphedNode.nodeType;
			var toNodeType = toNode.nodeType;
			if (!childrenOnly) {
				if (morphedNodeType === ELEMENT_NODE) if (toNodeType === ELEMENT_NODE) {
					if (!compareNodeNames(fromNode, toNode)) {
						onNodeDiscarded(fromNode);
						morphedNode = moveChildren(fromNode, createElementNS(toNode.nodeName, toNode.namespaceURI));
					}
				} else morphedNode = toNode;
				else if (morphedNodeType === TEXT_NODE || morphedNodeType === COMMENT_NODE) if (toNodeType === morphedNodeType) {
					if (morphedNode.nodeValue !== toNode.nodeValue) morphedNode.nodeValue = toNode.nodeValue;
					return morphedNode;
				} else morphedNode = toNode;
			}
			if (morphedNode === toNode) onNodeDiscarded(fromNode);
			else {
				if (toNode.isSameNode && toNode.isSameNode(morphedNode)) return;
				morphEl(morphedNode, toNode, childrenOnly);
				if (keyedRemovalList) for (var i = 0, len = keyedRemovalList.length; i < len; i++) {
					var elToRemove = fromNodesLookup[keyedRemovalList[i]];
					if (elToRemove) removeNode(elToRemove, elToRemove.parentNode, false);
				}
			}
			if (!childrenOnly && morphedNode !== fromNode && fromNode.parentNode) {
				if (morphedNode.actualize) morphedNode = morphedNode.actualize(fromNode.ownerDocument || doc);
				fromNode.parentNode.replaceChild(morphedNode, fromNode);
			}
			return morphedNode;
		};
	}

//#endregion
//#region node_modules/dompurify/dist/purify.js
	var require_purify = /* @__PURE__ */ __commonJSMin(((exports, module) => {
		/*! @license DOMPurify 3.1.6 | (c) Cure53 and other contributors | Released under the Apache license 2.0 and Mozilla Public License 2.0 | github.com/cure53/DOMPurify/blob/3.1.6/LICENSE */
		(function(global, factory) {
			typeof exports === "object" && typeof module !== "undefined" ? module.exports = factory() : typeof define === "function" && define.amd ? define(factory) : (global = typeof globalThis !== "undefined" ? globalThis : global || self, global.DOMPurify = factory());
		})(exports, (function() {
			"use strict";
			const { entries, setPrototypeOf, isFrozen, getPrototypeOf, getOwnPropertyDescriptor } = Object;
			let { freeze, seal, create } = Object;
			let { apply, construct } = typeof Reflect !== "undefined" && Reflect;
			if (!freeze) freeze = function freeze$1(x) {
				return x;
			};
			if (!seal) seal = function seal$1(x) {
				return x;
			};
			if (!apply) apply = function apply$1(fun, thisValue, args) {
				return fun.apply(thisValue, args);
			};
			if (!construct) construct = function construct$1(Func, args) {
				return new Func(...args);
			};
			const arrayForEach = unapply(Array.prototype.forEach);
			const arrayPop = unapply(Array.prototype.pop);
			const arrayPush = unapply(Array.prototype.push);
			const stringToLowerCase = unapply(String.prototype.toLowerCase);
			const stringToString = unapply(String.prototype.toString);
			const stringMatch = unapply(String.prototype.match);
			const stringReplace = unapply(String.prototype.replace);
			const stringIndexOf = unapply(String.prototype.indexOf);
			const stringTrim = unapply(String.prototype.trim);
			const objectHasOwnProperty = unapply(Object.prototype.hasOwnProperty);
			const regExpTest = unapply(RegExp.prototype.test);
			const typeErrorCreate = unconstruct(TypeError);
			/**
			* Creates a new function that calls the given function with a specified thisArg and arguments.
			*
			* @param {Function} func - The function to be wrapped and called.
			* @returns {Function} A new function that calls the given function with a specified thisArg and arguments.
			*/
			function unapply(func) {
				return function(thisArg) {
					for (var _len = arguments.length, args = new Array(_len > 1 ? _len - 1 : 0), _key = 1; _key < _len; _key++) args[_key - 1] = arguments[_key];
					return apply(func, thisArg, args);
				};
			}
			/**
			* Creates a new function that constructs an instance of the given constructor function with the provided arguments.
			*
			* @param {Function} func - The constructor function to be wrapped and called.
			* @returns {Function} A new function that constructs an instance of the given constructor function with the provided arguments.
			*/
			function unconstruct(func) {
				return function() {
					for (var _len2 = arguments.length, args = new Array(_len2), _key2 = 0; _key2 < _len2; _key2++) args[_key2] = arguments[_key2];
					return construct(func, args);
				};
			}
			/**
			* Add properties to a lookup table
			*
			* @param {Object} set - The set to which elements will be added.
			* @param {Array} array - The array containing elements to be added to the set.
			* @param {Function} transformCaseFunc - An optional function to transform the case of each element before adding to the set.
			* @returns {Object} The modified set with added elements.
			*/
			function addToSet(set, array) {
				let transformCaseFunc = arguments.length > 2 && arguments[2] !== void 0 ? arguments[2] : stringToLowerCase;
				if (setPrototypeOf) setPrototypeOf(set, null);
				let l = array.length;
				while (l--) {
					let element = array[l];
					if (typeof element === "string") {
						const lcElement = transformCaseFunc(element);
						if (lcElement !== element) {
							if (!isFrozen(array)) array[l] = lcElement;
							element = lcElement;
						}
					}
					set[element] = true;
				}
				return set;
			}
			/**
			* Clean up an array to harden against CSPP
			*
			* @param {Array} array - The array to be cleaned.
			* @returns {Array} The cleaned version of the array
			*/
			function cleanArray(array) {
				for (let index = 0; index < array.length; index++) if (!objectHasOwnProperty(array, index)) array[index] = null;
				return array;
			}
			/**
			* Shallow clone an object
			*
			* @param {Object} object - The object to be cloned.
			* @returns {Object} A new object that copies the original.
			*/
			function clone(object) {
				const newObject = create(null);
				for (const [property, value] of entries(object)) if (objectHasOwnProperty(object, property)) if (Array.isArray(value)) newObject[property] = cleanArray(value);
				else if (value && typeof value === "object" && value.constructor === Object) newObject[property] = clone(value);
				else newObject[property] = value;
				return newObject;
			}
			/**
			* This method automatically checks if the prop is function or getter and behaves accordingly.
			*
			* @param {Object} object - The object to look up the getter function in its prototype chain.
			* @param {String} prop - The property name for which to find the getter function.
			* @returns {Function} The getter function found in the prototype chain or a fallback function.
			*/
			function lookupGetter(object, prop) {
				while (object !== null) {
					const desc = getOwnPropertyDescriptor(object, prop);
					if (desc) {
						if (desc.get) return unapply(desc.get);
						if (typeof desc.value === "function") return unapply(desc.value);
					}
					object = getPrototypeOf(object);
				}
				function fallbackValue() {
					return null;
				}
				return fallbackValue;
			}
			const html$1 = freeze([
				"a",
				"abbr",
				"acronym",
				"address",
				"area",
				"article",
				"aside",
				"audio",
				"b",
				"bdi",
				"bdo",
				"big",
				"blink",
				"blockquote",
				"body",
				"br",
				"button",
				"canvas",
				"caption",
				"center",
				"cite",
				"code",
				"col",
				"colgroup",
				"content",
				"data",
				"datalist",
				"dd",
				"decorator",
				"del",
				"details",
				"dfn",
				"dialog",
				"dir",
				"div",
				"dl",
				"dt",
				"element",
				"em",
				"fieldset",
				"figcaption",
				"figure",
				"font",
				"footer",
				"form",
				"h1",
				"h2",
				"h3",
				"h4",
				"h5",
				"h6",
				"head",
				"header",
				"hgroup",
				"hr",
				"html",
				"i",
				"img",
				"input",
				"ins",
				"kbd",
				"label",
				"legend",
				"li",
				"main",
				"map",
				"mark",
				"marquee",
				"menu",
				"menuitem",
				"meter",
				"nav",
				"nobr",
				"ol",
				"optgroup",
				"option",
				"output",
				"p",
				"picture",
				"pre",
				"progress",
				"q",
				"rp",
				"rt",
				"ruby",
				"s",
				"samp",
				"section",
				"select",
				"shadow",
				"small",
				"source",
				"spacer",
				"span",
				"strike",
				"strong",
				"style",
				"sub",
				"summary",
				"sup",
				"table",
				"tbody",
				"td",
				"template",
				"textarea",
				"tfoot",
				"th",
				"thead",
				"time",
				"tr",
				"track",
				"tt",
				"u",
				"ul",
				"var",
				"video",
				"wbr"
			]);
			const svg$1 = freeze([
				"svg",
				"a",
				"altglyph",
				"altglyphdef",
				"altglyphitem",
				"animatecolor",
				"animatemotion",
				"animatetransform",
				"circle",
				"clippath",
				"defs",
				"desc",
				"ellipse",
				"filter",
				"font",
				"g",
				"glyph",
				"glyphref",
				"hkern",
				"image",
				"line",
				"lineargradient",
				"marker",
				"mask",
				"metadata",
				"mpath",
				"path",
				"pattern",
				"polygon",
				"polyline",
				"radialgradient",
				"rect",
				"stop",
				"style",
				"switch",
				"symbol",
				"text",
				"textpath",
				"title",
				"tref",
				"tspan",
				"view",
				"vkern"
			]);
			const svgFilters = freeze([
				"feBlend",
				"feColorMatrix",
				"feComponentTransfer",
				"feComposite",
				"feConvolveMatrix",
				"feDiffuseLighting",
				"feDisplacementMap",
				"feDistantLight",
				"feDropShadow",
				"feFlood",
				"feFuncA",
				"feFuncB",
				"feFuncG",
				"feFuncR",
				"feGaussianBlur",
				"feImage",
				"feMerge",
				"feMergeNode",
				"feMorphology",
				"feOffset",
				"fePointLight",
				"feSpecularLighting",
				"feSpotLight",
				"feTile",
				"feTurbulence"
			]);
			const svgDisallowed = freeze([
				"animate",
				"color-profile",
				"cursor",
				"discard",
				"font-face",
				"font-face-format",
				"font-face-name",
				"font-face-src",
				"font-face-uri",
				"foreignobject",
				"hatch",
				"hatchpath",
				"mesh",
				"meshgradient",
				"meshpatch",
				"meshrow",
				"missing-glyph",
				"script",
				"set",
				"solidcolor",
				"unknown",
				"use"
			]);
			const mathMl$1 = freeze([
				"math",
				"menclose",
				"merror",
				"mfenced",
				"mfrac",
				"mglyph",
				"mi",
				"mlabeledtr",
				"mmultiscripts",
				"mn",
				"mo",
				"mover",
				"mpadded",
				"mphantom",
				"mroot",
				"mrow",
				"ms",
				"mspace",
				"msqrt",
				"mstyle",
				"msub",
				"msup",
				"msubsup",
				"mtable",
				"mtd",
				"mtext",
				"mtr",
				"munder",
				"munderover",
				"mprescripts"
			]);
			const mathMlDisallowed = freeze([
				"maction",
				"maligngroup",
				"malignmark",
				"mlongdiv",
				"mscarries",
				"mscarry",
				"msgroup",
				"mstack",
				"msline",
				"msrow",
				"semantics",
				"annotation",
				"annotation-xml",
				"mprescripts",
				"none"
			]);
			const text = freeze(["#text"]);
			const html = freeze([
				"accept",
				"action",
				"align",
				"alt",
				"autocapitalize",
				"autocomplete",
				"autopictureinpicture",
				"autoplay",
				"background",
				"bgcolor",
				"border",
				"capture",
				"cellpadding",
				"cellspacing",
				"checked",
				"cite",
				"class",
				"clear",
				"color",
				"cols",
				"colspan",
				"controls",
				"controlslist",
				"coords",
				"crossorigin",
				"datetime",
				"decoding",
				"default",
				"dir",
				"disabled",
				"disablepictureinpicture",
				"disableremoteplayback",
				"download",
				"draggable",
				"enctype",
				"enterkeyhint",
				"face",
				"for",
				"headers",
				"height",
				"hidden",
				"high",
				"href",
				"hreflang",
				"id",
				"inputmode",
				"integrity",
				"ismap",
				"kind",
				"label",
				"lang",
				"list",
				"loading",
				"loop",
				"low",
				"max",
				"maxlength",
				"media",
				"method",
				"min",
				"minlength",
				"multiple",
				"muted",
				"name",
				"nonce",
				"noshade",
				"novalidate",
				"nowrap",
				"open",
				"optimum",
				"pattern",
				"placeholder",
				"playsinline",
				"popover",
				"popovertarget",
				"popovertargetaction",
				"poster",
				"preload",
				"pubdate",
				"radiogroup",
				"readonly",
				"rel",
				"required",
				"rev",
				"reversed",
				"role",
				"rows",
				"rowspan",
				"spellcheck",
				"scope",
				"selected",
				"shape",
				"size",
				"sizes",
				"span",
				"srclang",
				"start",
				"src",
				"srcset",
				"step",
				"style",
				"summary",
				"tabindex",
				"title",
				"translate",
				"type",
				"usemap",
				"valign",
				"value",
				"width",
				"wrap",
				"xmlns",
				"slot"
			]);
			const svg = freeze([
				"accent-height",
				"accumulate",
				"additive",
				"alignment-baseline",
				"ascent",
				"attributename",
				"attributetype",
				"azimuth",
				"basefrequency",
				"baseline-shift",
				"begin",
				"bias",
				"by",
				"class",
				"clip",
				"clippathunits",
				"clip-path",
				"clip-rule",
				"color",
				"color-interpolation",
				"color-interpolation-filters",
				"color-profile",
				"color-rendering",
				"cx",
				"cy",
				"d",
				"dx",
				"dy",
				"diffuseconstant",
				"direction",
				"display",
				"divisor",
				"dur",
				"edgemode",
				"elevation",
				"end",
				"fill",
				"fill-opacity",
				"fill-rule",
				"filter",
				"filterunits",
				"flood-color",
				"flood-opacity",
				"font-family",
				"font-size",
				"font-size-adjust",
				"font-stretch",
				"font-style",
				"font-variant",
				"font-weight",
				"fx",
				"fy",
				"g1",
				"g2",
				"glyph-name",
				"glyphref",
				"gradientunits",
				"gradienttransform",
				"height",
				"href",
				"id",
				"image-rendering",
				"in",
				"in2",
				"k",
				"k1",
				"k2",
				"k3",
				"k4",
				"kerning",
				"keypoints",
				"keysplines",
				"keytimes",
				"lang",
				"lengthadjust",
				"letter-spacing",
				"kernelmatrix",
				"kernelunitlength",
				"lighting-color",
				"local",
				"marker-end",
				"marker-mid",
				"marker-start",
				"markerheight",
				"markerunits",
				"markerwidth",
				"maskcontentunits",
				"maskunits",
				"max",
				"mask",
				"media",
				"method",
				"mode",
				"min",
				"name",
				"numoctaves",
				"offset",
				"operator",
				"opacity",
				"order",
				"orient",
				"orientation",
				"origin",
				"overflow",
				"paint-order",
				"path",
				"pathlength",
				"patterncontentunits",
				"patterntransform",
				"patternunits",
				"points",
				"preservealpha",
				"preserveaspectratio",
				"primitiveunits",
				"r",
				"rx",
				"ry",
				"radius",
				"refx",
				"refy",
				"repeatcount",
				"repeatdur",
				"restart",
				"result",
				"rotate",
				"scale",
				"seed",
				"shape-rendering",
				"specularconstant",
				"specularexponent",
				"spreadmethod",
				"startoffset",
				"stddeviation",
				"stitchtiles",
				"stop-color",
				"stop-opacity",
				"stroke-dasharray",
				"stroke-dashoffset",
				"stroke-linecap",
				"stroke-linejoin",
				"stroke-miterlimit",
				"stroke-opacity",
				"stroke",
				"stroke-width",
				"style",
				"surfacescale",
				"systemlanguage",
				"tabindex",
				"targetx",
				"targety",
				"transform",
				"transform-origin",
				"text-anchor",
				"text-decoration",
				"text-rendering",
				"textlength",
				"type",
				"u1",
				"u2",
				"unicode",
				"values",
				"viewbox",
				"visibility",
				"version",
				"vert-adv-y",
				"vert-origin-x",
				"vert-origin-y",
				"width",
				"word-spacing",
				"wrap",
				"writing-mode",
				"xchannelselector",
				"ychannelselector",
				"x",
				"x1",
				"x2",
				"xmlns",
				"y",
				"y1",
				"y2",
				"z",
				"zoomandpan"
			]);
			const mathMl = freeze([
				"accent",
				"accentunder",
				"align",
				"bevelled",
				"close",
				"columnsalign",
				"columnlines",
				"columnspan",
				"denomalign",
				"depth",
				"dir",
				"display",
				"displaystyle",
				"encoding",
				"fence",
				"frame",
				"height",
				"href",
				"id",
				"largeop",
				"length",
				"linethickness",
				"lspace",
				"lquote",
				"mathbackground",
				"mathcolor",
				"mathsize",
				"mathvariant",
				"maxsize",
				"minsize",
				"movablelimits",
				"notation",
				"numalign",
				"open",
				"rowalign",
				"rowlines",
				"rowspacing",
				"rowspan",
				"rspace",
				"rquote",
				"scriptlevel",
				"scriptminsize",
				"scriptsizemultiplier",
				"selection",
				"separator",
				"separators",
				"stretchy",
				"subscriptshift",
				"supscriptshift",
				"symmetric",
				"voffset",
				"width",
				"xmlns"
			]);
			const xml = freeze([
				"xlink:href",
				"xml:id",
				"xlink:title",
				"xml:space",
				"xmlns:xlink"
			]);
			const MUSTACHE_EXPR = seal(/\{\{[\w\W]*|[\w\W]*\}\}/gm);
			const ERB_EXPR = seal(/<%[\w\W]*|[\w\W]*%>/gm);
			const TMPLIT_EXPR = seal(/\${[\w\W]*}/gm);
			const DATA_ATTR = seal(/^data-[\-\w.\u00B7-\uFFFF]/);
			const ARIA_ATTR = seal(/^aria-[\-\w]+$/);
			const IS_ALLOWED_URI = seal(/^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i);
			const IS_SCRIPT_OR_DATA = seal(/^(?:\w+script|data):/i);
			const ATTR_WHITESPACE = seal(/[\u0000-\u0020\u00A0\u1680\u180E\u2000-\u2029\u205F\u3000]/g);
			const DOCTYPE_NAME = seal(/^html$/i);
			const CUSTOM_ELEMENT = seal(/^[a-z][.\w]*(-[.\w]+)+$/i);
			var EXPRESSIONS = /* @__PURE__ */ Object.freeze({
				__proto__: null,
				MUSTACHE_EXPR,
				ERB_EXPR,
				TMPLIT_EXPR,
				DATA_ATTR,
				ARIA_ATTR,
				IS_ALLOWED_URI,
				IS_SCRIPT_OR_DATA,
				ATTR_WHITESPACE,
				DOCTYPE_NAME,
				CUSTOM_ELEMENT
			});
			const NODE_TYPE = {
				element: 1,
				attribute: 2,
				text: 3,
				cdataSection: 4,
				entityReference: 5,
				entityNode: 6,
				progressingInstruction: 7,
				comment: 8,
				document: 9,
				documentType: 10,
				documentFragment: 11,
				notation: 12
			};
			const getGlobal = function getGlobal$1() {
				return typeof window === "undefined" ? null : window;
			};
			/**
			* Creates a no-op policy for internal use only.
			* Don't export this function outside this module!
			* @param {TrustedTypePolicyFactory} trustedTypes The policy factory.
			* @param {HTMLScriptElement} purifyHostElement The Script element used to load DOMPurify (to determine policy name suffix).
			* @return {TrustedTypePolicy} The policy created (or null, if Trusted Types
			* are not supported or creating the policy failed).
			*/
			const _createTrustedTypesPolicy = function _createTrustedTypesPolicy$1(trustedTypes, purifyHostElement) {
				if (typeof trustedTypes !== "object" || typeof trustedTypes.createPolicy !== "function") return null;
				let suffix = null;
				const ATTR_NAME = "data-tt-policy-suffix";
				if (purifyHostElement && purifyHostElement.hasAttribute(ATTR_NAME)) suffix = purifyHostElement.getAttribute(ATTR_NAME);
				const policyName = "dompurify" + (suffix ? "#" + suffix : "");
				try {
					return trustedTypes.createPolicy(policyName, {
						createHTML(html$2) {
							return html$2;
						},
						createScriptURL(scriptUrl) {
							return scriptUrl;
						}
					});
				} catch (_) {
					console.warn("TrustedTypes policy " + policyName + " could not be created.");
					return null;
				}
			};
			function createDOMPurify() {
				let window$1 = arguments.length > 0 && arguments[0] !== void 0 ? arguments[0] : getGlobal();
				const DOMPurify$1 = (root) => createDOMPurify(root);
				/**
				* Version label, exposed for easier checks
				* if DOMPurify is up to date or not
				*/
				DOMPurify$1.version = "3.1.6";
				/**
				* Array of elements that DOMPurify removed during sanitation.
				* Empty if nothing was removed.
				*/
				DOMPurify$1.removed = [];
				if (!window$1 || !window$1.document || window$1.document.nodeType !== NODE_TYPE.document) {
					DOMPurify$1.isSupported = false;
					return DOMPurify$1;
				}
				let { document: document$1 } = window$1;
				const originalDocument = document$1;
				const currentScript = originalDocument.currentScript;
				const { DocumentFragment, HTMLTemplateElement, Node: Node$1, Element: Element$1, NodeFilter, NamedNodeMap = window$1.NamedNodeMap || window$1.MozNamedAttrMap, HTMLFormElement: HTMLFormElement$1, DOMParser: DOMParser$1, trustedTypes } = window$1;
				const ElementPrototype = Element$1.prototype;
				const cloneNode = lookupGetter(ElementPrototype, "cloneNode");
				const remove = lookupGetter(ElementPrototype, "remove");
				const getNextSibling = lookupGetter(ElementPrototype, "nextSibling");
				const getChildNodes = lookupGetter(ElementPrototype, "childNodes");
				const getParentNode = lookupGetter(ElementPrototype, "parentNode");
				if (typeof HTMLTemplateElement === "function") {
					const template = document$1.createElement("template");
					if (template.content && template.content.ownerDocument) document$1 = template.content.ownerDocument;
				}
				let trustedTypesPolicy;
				let emptyHTML = "";
				const { implementation, createNodeIterator, createDocumentFragment, getElementsByTagName } = document$1;
				const { importNode } = originalDocument;
				let hooks = {};
				/**
				* Expose whether this browser supports running the full DOMPurify.
				*/
				DOMPurify$1.isSupported = typeof entries === "function" && typeof getParentNode === "function" && implementation && implementation.createHTMLDocument !== void 0;
				const { MUSTACHE_EXPR: MUSTACHE_EXPR$1, ERB_EXPR: ERB_EXPR$1, TMPLIT_EXPR: TMPLIT_EXPR$1, DATA_ATTR: DATA_ATTR$1, ARIA_ATTR: ARIA_ATTR$1, IS_SCRIPT_OR_DATA: IS_SCRIPT_OR_DATA$1, ATTR_WHITESPACE: ATTR_WHITESPACE$1, CUSTOM_ELEMENT: CUSTOM_ELEMENT$1 } = EXPRESSIONS;
				let { IS_ALLOWED_URI: IS_ALLOWED_URI$1 } = EXPRESSIONS;
				/**
				* We consider the elements and attributes below to be safe. Ideally
				* don't add any new ones but feel free to remove unwanted ones.
				*/
				let ALLOWED_TAGS = null;
				const DEFAULT_ALLOWED_TAGS = addToSet({}, [
					...html$1,
					...svg$1,
					...svgFilters,
					...mathMl$1,
					...text
				]);
				let ALLOWED_ATTR = null;
				const DEFAULT_ALLOWED_ATTR = addToSet({}, [
					...html,
					...svg,
					...mathMl,
					...xml
				]);
				let CUSTOM_ELEMENT_HANDLING = Object.seal(create(null, {
					tagNameCheck: {
						writable: true,
						configurable: false,
						enumerable: true,
						value: null
					},
					attributeNameCheck: {
						writable: true,
						configurable: false,
						enumerable: true,
						value: null
					},
					allowCustomizedBuiltInElements: {
						writable: true,
						configurable: false,
						enumerable: true,
						value: false
					}
				}));
				let FORBID_TAGS = null;
				let FORBID_ATTR = null;
				let ALLOW_ARIA_ATTR = true;
				let ALLOW_DATA_ATTR = true;
				let ALLOW_UNKNOWN_PROTOCOLS = false;
				let ALLOW_SELF_CLOSE_IN_ATTR = true;
				let SAFE_FOR_TEMPLATES = false;
				let SAFE_FOR_XML = true;
				let WHOLE_DOCUMENT = false;
				let SET_CONFIG = false;
				let FORCE_BODY = false;
				let RETURN_DOM = false;
				let RETURN_DOM_FRAGMENT = false;
				let RETURN_TRUSTED_TYPE = false;
				let SANITIZE_DOM = true;
				let SANITIZE_NAMED_PROPS = false;
				const SANITIZE_NAMED_PROPS_PREFIX = "user-content-";
				let KEEP_CONTENT = true;
				let IN_PLACE = false;
				let USE_PROFILES = {};
				let FORBID_CONTENTS = null;
				const DEFAULT_FORBID_CONTENTS = addToSet({}, [
					"annotation-xml",
					"audio",
					"colgroup",
					"desc",
					"foreignobject",
					"head",
					"iframe",
					"math",
					"mi",
					"mn",
					"mo",
					"ms",
					"mtext",
					"noembed",
					"noframes",
					"noscript",
					"plaintext",
					"script",
					"style",
					"svg",
					"template",
					"thead",
					"title",
					"video",
					"xmp"
				]);
				let DATA_URI_TAGS = null;
				const DEFAULT_DATA_URI_TAGS = addToSet({}, [
					"audio",
					"video",
					"img",
					"source",
					"image",
					"track"
				]);
				let URI_SAFE_ATTRIBUTES = null;
				const DEFAULT_URI_SAFE_ATTRIBUTES = addToSet({}, [
					"alt",
					"class",
					"for",
					"id",
					"label",
					"name",
					"pattern",
					"placeholder",
					"role",
					"summary",
					"title",
					"value",
					"style",
					"xmlns"
				]);
				const MATHML_NAMESPACE = "http://www.w3.org/1998/Math/MathML";
				const SVG_NAMESPACE = "http://www.w3.org/2000/svg";
				const HTML_NAMESPACE = "http://www.w3.org/1999/xhtml";
				let NAMESPACE = HTML_NAMESPACE;
				let IS_EMPTY_INPUT = false;
				let ALLOWED_NAMESPACES = null;
				const DEFAULT_ALLOWED_NAMESPACES = addToSet({}, [
					MATHML_NAMESPACE,
					SVG_NAMESPACE,
					HTML_NAMESPACE
				], stringToString);
				let PARSER_MEDIA_TYPE = null;
				const SUPPORTED_PARSER_MEDIA_TYPES = ["application/xhtml+xml", "text/html"];
				const DEFAULT_PARSER_MEDIA_TYPE = "text/html";
				let transformCaseFunc = null;
				let CONFIG = null;
				const formElement = document$1.createElement("form");
				const isRegexOrFunction = function isRegexOrFunction$1(testValue) {
					return testValue instanceof RegExp || testValue instanceof Function;
				};
				/**
				* _parseConfig
				*
				* @param  {Object} cfg optional config literal
				*/
				const _parseConfig = function _parseConfig$1() {
					let cfg = arguments.length > 0 && arguments[0] !== void 0 ? arguments[0] : {};
					if (CONFIG && CONFIG === cfg) return;
					if (!cfg || typeof cfg !== "object") cfg = {};
					cfg = clone(cfg);
					PARSER_MEDIA_TYPE = SUPPORTED_PARSER_MEDIA_TYPES.indexOf(cfg.PARSER_MEDIA_TYPE) === -1 ? DEFAULT_PARSER_MEDIA_TYPE : cfg.PARSER_MEDIA_TYPE;
					transformCaseFunc = PARSER_MEDIA_TYPE === "application/xhtml+xml" ? stringToString : stringToLowerCase;
					ALLOWED_TAGS = objectHasOwnProperty(cfg, "ALLOWED_TAGS") ? addToSet({}, cfg.ALLOWED_TAGS, transformCaseFunc) : DEFAULT_ALLOWED_TAGS;
					ALLOWED_ATTR = objectHasOwnProperty(cfg, "ALLOWED_ATTR") ? addToSet({}, cfg.ALLOWED_ATTR, transformCaseFunc) : DEFAULT_ALLOWED_ATTR;
					ALLOWED_NAMESPACES = objectHasOwnProperty(cfg, "ALLOWED_NAMESPACES") ? addToSet({}, cfg.ALLOWED_NAMESPACES, stringToString) : DEFAULT_ALLOWED_NAMESPACES;
					URI_SAFE_ATTRIBUTES = objectHasOwnProperty(cfg, "ADD_URI_SAFE_ATTR") ? addToSet(clone(DEFAULT_URI_SAFE_ATTRIBUTES), cfg.ADD_URI_SAFE_ATTR, transformCaseFunc) : DEFAULT_URI_SAFE_ATTRIBUTES;
					DATA_URI_TAGS = objectHasOwnProperty(cfg, "ADD_DATA_URI_TAGS") ? addToSet(clone(DEFAULT_DATA_URI_TAGS), cfg.ADD_DATA_URI_TAGS, transformCaseFunc) : DEFAULT_DATA_URI_TAGS;
					FORBID_CONTENTS = objectHasOwnProperty(cfg, "FORBID_CONTENTS") ? addToSet({}, cfg.FORBID_CONTENTS, transformCaseFunc) : DEFAULT_FORBID_CONTENTS;
					FORBID_TAGS = objectHasOwnProperty(cfg, "FORBID_TAGS") ? addToSet({}, cfg.FORBID_TAGS, transformCaseFunc) : {};
					FORBID_ATTR = objectHasOwnProperty(cfg, "FORBID_ATTR") ? addToSet({}, cfg.FORBID_ATTR, transformCaseFunc) : {};
					USE_PROFILES = objectHasOwnProperty(cfg, "USE_PROFILES") ? cfg.USE_PROFILES : false;
					ALLOW_ARIA_ATTR = cfg.ALLOW_ARIA_ATTR !== false;
					ALLOW_DATA_ATTR = cfg.ALLOW_DATA_ATTR !== false;
					ALLOW_UNKNOWN_PROTOCOLS = cfg.ALLOW_UNKNOWN_PROTOCOLS || false;
					ALLOW_SELF_CLOSE_IN_ATTR = cfg.ALLOW_SELF_CLOSE_IN_ATTR !== false;
					SAFE_FOR_TEMPLATES = cfg.SAFE_FOR_TEMPLATES || false;
					SAFE_FOR_XML = cfg.SAFE_FOR_XML !== false;
					WHOLE_DOCUMENT = cfg.WHOLE_DOCUMENT || false;
					RETURN_DOM = cfg.RETURN_DOM || false;
					RETURN_DOM_FRAGMENT = cfg.RETURN_DOM_FRAGMENT || false;
					RETURN_TRUSTED_TYPE = cfg.RETURN_TRUSTED_TYPE || false;
					FORCE_BODY = cfg.FORCE_BODY || false;
					SANITIZE_DOM = cfg.SANITIZE_DOM !== false;
					SANITIZE_NAMED_PROPS = cfg.SANITIZE_NAMED_PROPS || false;
					KEEP_CONTENT = cfg.KEEP_CONTENT !== false;
					IN_PLACE = cfg.IN_PLACE || false;
					IS_ALLOWED_URI$1 = cfg.ALLOWED_URI_REGEXP || IS_ALLOWED_URI;
					NAMESPACE = cfg.NAMESPACE || HTML_NAMESPACE;
					CUSTOM_ELEMENT_HANDLING = cfg.CUSTOM_ELEMENT_HANDLING || {};
					if (cfg.CUSTOM_ELEMENT_HANDLING && isRegexOrFunction(cfg.CUSTOM_ELEMENT_HANDLING.tagNameCheck)) CUSTOM_ELEMENT_HANDLING.tagNameCheck = cfg.CUSTOM_ELEMENT_HANDLING.tagNameCheck;
					if (cfg.CUSTOM_ELEMENT_HANDLING && isRegexOrFunction(cfg.CUSTOM_ELEMENT_HANDLING.attributeNameCheck)) CUSTOM_ELEMENT_HANDLING.attributeNameCheck = cfg.CUSTOM_ELEMENT_HANDLING.attributeNameCheck;
					if (cfg.CUSTOM_ELEMENT_HANDLING && typeof cfg.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements === "boolean") CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements = cfg.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements;
					if (SAFE_FOR_TEMPLATES) ALLOW_DATA_ATTR = false;
					if (RETURN_DOM_FRAGMENT) RETURN_DOM = true;
					if (USE_PROFILES) {
						ALLOWED_TAGS = addToSet({}, text);
						ALLOWED_ATTR = [];
						if (USE_PROFILES.html === true) {
							addToSet(ALLOWED_TAGS, html$1);
							addToSet(ALLOWED_ATTR, html);
						}
						if (USE_PROFILES.svg === true) {
							addToSet(ALLOWED_TAGS, svg$1);
							addToSet(ALLOWED_ATTR, svg);
							addToSet(ALLOWED_ATTR, xml);
						}
						if (USE_PROFILES.svgFilters === true) {
							addToSet(ALLOWED_TAGS, svgFilters);
							addToSet(ALLOWED_ATTR, svg);
							addToSet(ALLOWED_ATTR, xml);
						}
						if (USE_PROFILES.mathMl === true) {
							addToSet(ALLOWED_TAGS, mathMl$1);
							addToSet(ALLOWED_ATTR, mathMl);
							addToSet(ALLOWED_ATTR, xml);
						}
					}
					if (cfg.ADD_TAGS) {
						if (ALLOWED_TAGS === DEFAULT_ALLOWED_TAGS) ALLOWED_TAGS = clone(ALLOWED_TAGS);
						addToSet(ALLOWED_TAGS, cfg.ADD_TAGS, transformCaseFunc);
					}
					if (cfg.ADD_ATTR) {
						if (ALLOWED_ATTR === DEFAULT_ALLOWED_ATTR) ALLOWED_ATTR = clone(ALLOWED_ATTR);
						addToSet(ALLOWED_ATTR, cfg.ADD_ATTR, transformCaseFunc);
					}
					if (cfg.ADD_URI_SAFE_ATTR) addToSet(URI_SAFE_ATTRIBUTES, cfg.ADD_URI_SAFE_ATTR, transformCaseFunc);
					if (cfg.FORBID_CONTENTS) {
						if (FORBID_CONTENTS === DEFAULT_FORBID_CONTENTS) FORBID_CONTENTS = clone(FORBID_CONTENTS);
						addToSet(FORBID_CONTENTS, cfg.FORBID_CONTENTS, transformCaseFunc);
					}
					if (KEEP_CONTENT) ALLOWED_TAGS["#text"] = true;
					if (WHOLE_DOCUMENT) addToSet(ALLOWED_TAGS, [
						"html",
						"head",
						"body"
					]);
					if (ALLOWED_TAGS.table) {
						addToSet(ALLOWED_TAGS, ["tbody"]);
						delete FORBID_TAGS.tbody;
					}
					if (cfg.TRUSTED_TYPES_POLICY) {
						if (typeof cfg.TRUSTED_TYPES_POLICY.createHTML !== "function") throw typeErrorCreate("TRUSTED_TYPES_POLICY configuration option must provide a \"createHTML\" hook.");
						if (typeof cfg.TRUSTED_TYPES_POLICY.createScriptURL !== "function") throw typeErrorCreate("TRUSTED_TYPES_POLICY configuration option must provide a \"createScriptURL\" hook.");
						trustedTypesPolicy = cfg.TRUSTED_TYPES_POLICY;
						emptyHTML = trustedTypesPolicy.createHTML("");
					} else {
						if (trustedTypesPolicy === void 0) trustedTypesPolicy = _createTrustedTypesPolicy(trustedTypes, currentScript);
						if (trustedTypesPolicy !== null && typeof emptyHTML === "string") emptyHTML = trustedTypesPolicy.createHTML("");
					}
					if (freeze) freeze(cfg);
					CONFIG = cfg;
				};
				const MATHML_TEXT_INTEGRATION_POINTS = addToSet({}, [
					"mi",
					"mo",
					"mn",
					"ms",
					"mtext"
				]);
				const HTML_INTEGRATION_POINTS = addToSet({}, ["foreignobject", "annotation-xml"]);
				const COMMON_SVG_AND_HTML_ELEMENTS = addToSet({}, [
					"title",
					"style",
					"font",
					"a",
					"script"
				]);
				const ALL_SVG_TAGS = addToSet({}, [
					...svg$1,
					...svgFilters,
					...svgDisallowed
				]);
				const ALL_MATHML_TAGS = addToSet({}, [...mathMl$1, ...mathMlDisallowed]);
				/**
				* @param  {Element} element a DOM element whose namespace is being checked
				* @returns {boolean} Return false if the element has a
				*  namespace that a spec-compliant parser would never
				*  return. Return true otherwise.
				*/
				const _checkValidNamespace = function _checkValidNamespace$1(element) {
					let parent = getParentNode(element);
					if (!parent || !parent.tagName) parent = {
						namespaceURI: NAMESPACE,
						tagName: "template"
					};
					const tagName = stringToLowerCase(element.tagName);
					const parentTagName = stringToLowerCase(parent.tagName);
					if (!ALLOWED_NAMESPACES[element.namespaceURI]) return false;
					if (element.namespaceURI === SVG_NAMESPACE) {
						if (parent.namespaceURI === HTML_NAMESPACE) return tagName === "svg";
						if (parent.namespaceURI === MATHML_NAMESPACE) return tagName === "svg" && (parentTagName === "annotation-xml" || MATHML_TEXT_INTEGRATION_POINTS[parentTagName]);
						return Boolean(ALL_SVG_TAGS[tagName]);
					}
					if (element.namespaceURI === MATHML_NAMESPACE) {
						if (parent.namespaceURI === HTML_NAMESPACE) return tagName === "math";
						if (parent.namespaceURI === SVG_NAMESPACE) return tagName === "math" && HTML_INTEGRATION_POINTS[parentTagName];
						return Boolean(ALL_MATHML_TAGS[tagName]);
					}
					if (element.namespaceURI === HTML_NAMESPACE) {
						if (parent.namespaceURI === SVG_NAMESPACE && !HTML_INTEGRATION_POINTS[parentTagName]) return false;
						if (parent.namespaceURI === MATHML_NAMESPACE && !MATHML_TEXT_INTEGRATION_POINTS[parentTagName]) return false;
						return !ALL_MATHML_TAGS[tagName] && (COMMON_SVG_AND_HTML_ELEMENTS[tagName] || !ALL_SVG_TAGS[tagName]);
					}
					if (PARSER_MEDIA_TYPE === "application/xhtml+xml" && ALLOWED_NAMESPACES[element.namespaceURI]) return true;
					return false;
				};
				/**
				* _forceRemove
				*
				* @param  {Node} node a DOM node
				*/
				const _forceRemove = function _forceRemove$1(node) {
					arrayPush(DOMPurify$1.removed, { element: node });
					try {
						getParentNode(node).removeChild(node);
					} catch (_) {
						remove(node);
					}
				};
				/**
				* _removeAttribute
				*
				* @param  {String} name an Attribute name
				* @param  {Node} node a DOM node
				*/
				const _removeAttribute = function _removeAttribute$1(name, node) {
					try {
						arrayPush(DOMPurify$1.removed, {
							attribute: node.getAttributeNode(name),
							from: node
						});
					} catch (_) {
						arrayPush(DOMPurify$1.removed, {
							attribute: null,
							from: node
						});
					}
					node.removeAttribute(name);
					if (name === "is" && !ALLOWED_ATTR[name]) if (RETURN_DOM || RETURN_DOM_FRAGMENT) try {
						_forceRemove(node);
					} catch (_) {}
					else try {
						node.setAttribute(name, "");
					} catch (_) {}
				};
				/**
				* _initDocument
				*
				* @param  {String} dirty a string of dirty markup
				* @return {Document} a DOM, filled with the dirty markup
				*/
				const _initDocument = function _initDocument$1(dirty) {
					let doc$1 = null;
					let leadingWhitespace = null;
					if (FORCE_BODY) dirty = "<remove></remove>" + dirty;
					else {
						const matches = stringMatch(dirty, /^[\r\n\t ]+/);
						leadingWhitespace = matches && matches[0];
					}
					if (PARSER_MEDIA_TYPE === "application/xhtml+xml" && NAMESPACE === HTML_NAMESPACE) dirty = "<html xmlns=\"http://www.w3.org/1999/xhtml\"><head></head><body>" + dirty + "</body></html>";
					const dirtyPayload = trustedTypesPolicy ? trustedTypesPolicy.createHTML(dirty) : dirty;
					if (NAMESPACE === HTML_NAMESPACE) try {
						doc$1 = new DOMParser$1().parseFromString(dirtyPayload, PARSER_MEDIA_TYPE);
					} catch (_) {}
					if (!doc$1 || !doc$1.documentElement) {
						doc$1 = implementation.createDocument(NAMESPACE, "template", null);
						try {
							doc$1.documentElement.innerHTML = IS_EMPTY_INPUT ? emptyHTML : dirtyPayload;
						} catch (_) {}
					}
					const body = doc$1.body || doc$1.documentElement;
					if (dirty && leadingWhitespace) body.insertBefore(document$1.createTextNode(leadingWhitespace), body.childNodes[0] || null);
					if (NAMESPACE === HTML_NAMESPACE) return getElementsByTagName.call(doc$1, WHOLE_DOCUMENT ? "html" : "body")[0];
					return WHOLE_DOCUMENT ? doc$1.documentElement : body;
				};
				/**
				* Creates a NodeIterator object that you can use to traverse filtered lists of nodes or elements in a document.
				*
				* @param  {Node} root The root element or node to start traversing on.
				* @return {NodeIterator} The created NodeIterator
				*/
				const _createNodeIterator = function _createNodeIterator$1(root) {
					return createNodeIterator.call(root.ownerDocument || root, root, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_COMMENT | NodeFilter.SHOW_TEXT | NodeFilter.SHOW_PROCESSING_INSTRUCTION | NodeFilter.SHOW_CDATA_SECTION, null);
				};
				/**
				* _isClobbered
				*
				* @param  {Node} elm element to check for clobbering attacks
				* @return {Boolean} true if clobbered, false if safe
				*/
				const _isClobbered = function _isClobbered$1(elm) {
					return elm instanceof HTMLFormElement$1 && (typeof elm.nodeName !== "string" || typeof elm.textContent !== "string" || typeof elm.removeChild !== "function" || !(elm.attributes instanceof NamedNodeMap) || typeof elm.removeAttribute !== "function" || typeof elm.setAttribute !== "function" || typeof elm.namespaceURI !== "string" || typeof elm.insertBefore !== "function" || typeof elm.hasChildNodes !== "function");
				};
				/**
				* Checks whether the given object is a DOM node.
				*
				* @param  {Node} object object to check whether it's a DOM node
				* @return {Boolean} true is object is a DOM node
				*/
				const _isNode = function _isNode$1(object) {
					return typeof Node$1 === "function" && object instanceof Node$1;
				};
				/**
				* _executeHook
				* Execute user configurable hooks
				*
				* @param  {String} entryPoint  Name of the hook's entry point
				* @param  {Node} currentNode node to work on with the hook
				* @param  {Object} data additional hook parameters
				*/
				const _executeHook = function _executeHook$1(entryPoint, currentNode, data) {
					if (!hooks[entryPoint]) return;
					arrayForEach(hooks[entryPoint], (hook) => {
						hook.call(DOMPurify$1, currentNode, data, CONFIG);
					});
				};
				/**
				* _sanitizeElements
				*
				* @protect nodeName
				* @protect textContent
				* @protect removeChild
				*
				* @param   {Node} currentNode to check for permission to exist
				* @return  {Boolean} true if node was killed, false if left alive
				*/
				const _sanitizeElements = function _sanitizeElements$1(currentNode) {
					let content = null;
					_executeHook("beforeSanitizeElements", currentNode, null);
					if (_isClobbered(currentNode)) {
						_forceRemove(currentNode);
						return true;
					}
					const tagName = transformCaseFunc(currentNode.nodeName);
					_executeHook("uponSanitizeElement", currentNode, {
						tagName,
						allowedTags: ALLOWED_TAGS
					});
					if (currentNode.hasChildNodes() && !_isNode(currentNode.firstElementChild) && regExpTest(/<[/\w]/g, currentNode.innerHTML) && regExpTest(/<[/\w]/g, currentNode.textContent)) {
						_forceRemove(currentNode);
						return true;
					}
					if (currentNode.nodeType === NODE_TYPE.progressingInstruction) {
						_forceRemove(currentNode);
						return true;
					}
					if (SAFE_FOR_XML && currentNode.nodeType === NODE_TYPE.comment && regExpTest(/<[/\w]/g, currentNode.data)) {
						_forceRemove(currentNode);
						return true;
					}
					if (!ALLOWED_TAGS[tagName] || FORBID_TAGS[tagName]) {
						if (!FORBID_TAGS[tagName] && _isBasicCustomElement(tagName)) {
							if (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, tagName)) return false;
							if (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(tagName)) return false;
						}
						if (KEEP_CONTENT && !FORBID_CONTENTS[tagName]) {
							const parentNode = getParentNode(currentNode) || currentNode.parentNode;
							const childNodes = getChildNodes(currentNode) || currentNode.childNodes;
							if (childNodes && parentNode) {
								const childCount = childNodes.length;
								for (let i = childCount - 1; i >= 0; --i) {
									const childClone = cloneNode(childNodes[i], true);
									childClone.__removalCount = (currentNode.__removalCount || 0) + 1;
									parentNode.insertBefore(childClone, getNextSibling(currentNode));
								}
							}
						}
						_forceRemove(currentNode);
						return true;
					}
					if (currentNode instanceof Element$1 && !_checkValidNamespace(currentNode)) {
						_forceRemove(currentNode);
						return true;
					}
					if ((tagName === "noscript" || tagName === "noembed" || tagName === "noframes") && regExpTest(/<\/no(script|embed|frames)/i, currentNode.innerHTML)) {
						_forceRemove(currentNode);
						return true;
					}
					if (SAFE_FOR_TEMPLATES && currentNode.nodeType === NODE_TYPE.text) {
						content = currentNode.textContent;
						arrayForEach([
							MUSTACHE_EXPR$1,
							ERB_EXPR$1,
							TMPLIT_EXPR$1
						], (expr) => {
							content = stringReplace(content, expr, " ");
						});
						if (currentNode.textContent !== content) {
							arrayPush(DOMPurify$1.removed, { element: currentNode.cloneNode() });
							currentNode.textContent = content;
						}
					}
					_executeHook("afterSanitizeElements", currentNode, null);
					return false;
				};
				/**
				* _isValidAttribute
				*
				* @param  {string} lcTag Lowercase tag name of containing element.
				* @param  {string} lcName Lowercase attribute name.
				* @param  {string} value Attribute value.
				* @return {Boolean} Returns true if `value` is valid, otherwise false.
				*/
				const _isValidAttribute = function _isValidAttribute$1(lcTag, lcName, value) {
					if (SANITIZE_DOM && (lcName === "id" || lcName === "name") && (value in document$1 || value in formElement)) return false;
					if (ALLOW_DATA_ATTR && !FORBID_ATTR[lcName] && regExpTest(DATA_ATTR$1, lcName));
					else if (ALLOW_ARIA_ATTR && regExpTest(ARIA_ATTR$1, lcName));
					else if (!ALLOWED_ATTR[lcName] || FORBID_ATTR[lcName]) if (_isBasicCustomElement(lcTag) && (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, lcTag) || CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(lcTag)) && (CUSTOM_ELEMENT_HANDLING.attributeNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.attributeNameCheck, lcName) || CUSTOM_ELEMENT_HANDLING.attributeNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.attributeNameCheck(lcName)) || lcName === "is" && CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements && (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, value) || CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(value)));
					else return false;
					else if (URI_SAFE_ATTRIBUTES[lcName]);
					else if (regExpTest(IS_ALLOWED_URI$1, stringReplace(value, ATTR_WHITESPACE$1, "")));
					else if ((lcName === "src" || lcName === "xlink:href" || lcName === "href") && lcTag !== "script" && stringIndexOf(value, "data:") === 0 && DATA_URI_TAGS[lcTag]);
					else if (ALLOW_UNKNOWN_PROTOCOLS && !regExpTest(IS_SCRIPT_OR_DATA$1, stringReplace(value, ATTR_WHITESPACE$1, "")));
					else if (value) return false;
					return true;
				};
				/**
				* _isBasicCustomElement
				* checks if at least one dash is included in tagName, and it's not the first char
				* for more sophisticated checking see https://github.com/sindresorhus/validate-element-name
				*
				* @param {string} tagName name of the tag of the node to sanitize
				* @returns {boolean} Returns true if the tag name meets the basic criteria for a custom element, otherwise false.
				*/
				const _isBasicCustomElement = function _isBasicCustomElement$1(tagName) {
					return tagName !== "annotation-xml" && stringMatch(tagName, CUSTOM_ELEMENT$1);
				};
				/**
				* _sanitizeAttributes
				*
				* @protect attributes
				* @protect nodeName
				* @protect removeAttribute
				* @protect setAttribute
				*
				* @param  {Node} currentNode to sanitize
				*/
				const _sanitizeAttributes = function _sanitizeAttributes$1(currentNode) {
					_executeHook("beforeSanitizeAttributes", currentNode, null);
					const { attributes } = currentNode;
					if (!attributes) return;
					const hookEvent = {
						attrName: "",
						attrValue: "",
						keepAttr: true,
						allowedAttributes: ALLOWED_ATTR
					};
					let l = attributes.length;
					while (l--) {
						const { name, namespaceURI, value: attrValue } = attributes[l];
						const lcName = transformCaseFunc(name);
						let value = name === "value" ? attrValue : stringTrim(attrValue);
						hookEvent.attrName = lcName;
						hookEvent.attrValue = value;
						hookEvent.keepAttr = true;
						hookEvent.forceKeepAttr = void 0;
						_executeHook("uponSanitizeAttribute", currentNode, hookEvent);
						value = hookEvent.attrValue;
						if (SAFE_FOR_XML && regExpTest(/((--!?|])>)|<\/(style|title)/i, value)) {
							_removeAttribute(name, currentNode);
							continue;
						}
						if (hookEvent.forceKeepAttr) continue;
						_removeAttribute(name, currentNode);
						if (!hookEvent.keepAttr) continue;
						if (!ALLOW_SELF_CLOSE_IN_ATTR && regExpTest(/\/>/i, value)) {
							_removeAttribute(name, currentNode);
							continue;
						}
						if (SAFE_FOR_TEMPLATES) arrayForEach([
							MUSTACHE_EXPR$1,
							ERB_EXPR$1,
							TMPLIT_EXPR$1
						], (expr) => {
							value = stringReplace(value, expr, " ");
						});
						const lcTag = transformCaseFunc(currentNode.nodeName);
						if (!_isValidAttribute(lcTag, lcName, value)) continue;
						if (SANITIZE_NAMED_PROPS && (lcName === "id" || lcName === "name")) {
							_removeAttribute(name, currentNode);
							value = SANITIZE_NAMED_PROPS_PREFIX + value;
						}
						if (trustedTypesPolicy && typeof trustedTypes === "object" && typeof trustedTypes.getAttributeType === "function") if (namespaceURI);
						else switch (trustedTypes.getAttributeType(lcTag, lcName)) {
							case "TrustedHTML":
								value = trustedTypesPolicy.createHTML(value);
								break;
							case "TrustedScriptURL":
								value = trustedTypesPolicy.createScriptURL(value);
								break;
						}
						try {
							if (namespaceURI) currentNode.setAttributeNS(namespaceURI, name, value);
							else currentNode.setAttribute(name, value);
							if (_isClobbered(currentNode)) _forceRemove(currentNode);
							else arrayPop(DOMPurify$1.removed);
						} catch (_) {}
					}
					_executeHook("afterSanitizeAttributes", currentNode, null);
				};
				/**
				* _sanitizeShadowDOM
				*
				* @param  {DocumentFragment} fragment to iterate over recursively
				*/
				const _sanitizeShadowDOM = function _sanitizeShadowDOM$1(fragment) {
					let shadowNode = null;
					const shadowIterator = _createNodeIterator(fragment);
					_executeHook("beforeSanitizeShadowDOM", fragment, null);
					while (shadowNode = shadowIterator.nextNode()) {
						_executeHook("uponSanitizeShadowNode", shadowNode, null);
						if (_sanitizeElements(shadowNode)) continue;
						if (shadowNode.content instanceof DocumentFragment) _sanitizeShadowDOM$1(shadowNode.content);
						_sanitizeAttributes(shadowNode);
					}
					_executeHook("afterSanitizeShadowDOM", fragment, null);
				};
				/**
				* Sanitize
				* Public method providing core sanitation functionality
				*
				* @param {String|Node} dirty string or DOM node
				* @param {Object} cfg object
				*/
				DOMPurify$1.sanitize = function(dirty) {
					let cfg = arguments.length > 1 && arguments[1] !== void 0 ? arguments[1] : {};
					let body = null;
					let importedNode = null;
					let currentNode = null;
					let returnNode = null;
					IS_EMPTY_INPUT = !dirty;
					if (IS_EMPTY_INPUT) dirty = "<!-->";
					if (typeof dirty !== "string" && !_isNode(dirty)) if (typeof dirty.toString === "function") {
						dirty = dirty.toString();
						if (typeof dirty !== "string") throw typeErrorCreate("dirty is not a string, aborting");
					} else throw typeErrorCreate("toString is not a function");
					if (!DOMPurify$1.isSupported) return dirty;
					if (!SET_CONFIG) _parseConfig(cfg);
					DOMPurify$1.removed = [];
					if (typeof dirty === "string") IN_PLACE = false;
					if (IN_PLACE) {
						if (dirty.nodeName) {
							const tagName = transformCaseFunc(dirty.nodeName);
							if (!ALLOWED_TAGS[tagName] || FORBID_TAGS[tagName]) throw typeErrorCreate("root node is forbidden and cannot be sanitized in-place");
						}
					} else if (dirty instanceof Node$1) {
						body = _initDocument("<!---->");
						importedNode = body.ownerDocument.importNode(dirty, true);
						if (importedNode.nodeType === NODE_TYPE.element && importedNode.nodeName === "BODY") body = importedNode;
						else if (importedNode.nodeName === "HTML") body = importedNode;
						else body.appendChild(importedNode);
					} else {
						if (!RETURN_DOM && !SAFE_FOR_TEMPLATES && !WHOLE_DOCUMENT && dirty.indexOf("<") === -1) return trustedTypesPolicy && RETURN_TRUSTED_TYPE ? trustedTypesPolicy.createHTML(dirty) : dirty;
						body = _initDocument(dirty);
						if (!body) return RETURN_DOM ? null : RETURN_TRUSTED_TYPE ? emptyHTML : "";
					}
					if (body && FORCE_BODY) _forceRemove(body.firstChild);
					const nodeIterator = _createNodeIterator(IN_PLACE ? dirty : body);
					while (currentNode = nodeIterator.nextNode()) {
						if (_sanitizeElements(currentNode)) continue;
						if (currentNode.content instanceof DocumentFragment) _sanitizeShadowDOM(currentNode.content);
						_sanitizeAttributes(currentNode);
					}
					if (IN_PLACE) return dirty;
					if (RETURN_DOM) {
						if (RETURN_DOM_FRAGMENT) {
							returnNode = createDocumentFragment.call(body.ownerDocument);
							while (body.firstChild) returnNode.appendChild(body.firstChild);
						} else returnNode = body;
						if (ALLOWED_ATTR.shadowroot || ALLOWED_ATTR.shadowrootmode) returnNode = importNode.call(originalDocument, returnNode, true);
						return returnNode;
					}
					let serializedHTML = WHOLE_DOCUMENT ? body.outerHTML : body.innerHTML;
					if (WHOLE_DOCUMENT && ALLOWED_TAGS["!doctype"] && body.ownerDocument && body.ownerDocument.doctype && body.ownerDocument.doctype.name && regExpTest(DOCTYPE_NAME, body.ownerDocument.doctype.name)) serializedHTML = "<!DOCTYPE " + body.ownerDocument.doctype.name + ">\n" + serializedHTML;
					if (SAFE_FOR_TEMPLATES) arrayForEach([
						MUSTACHE_EXPR$1,
						ERB_EXPR$1,
						TMPLIT_EXPR$1
					], (expr) => {
						serializedHTML = stringReplace(serializedHTML, expr, " ");
					});
					return trustedTypesPolicy && RETURN_TRUSTED_TYPE ? trustedTypesPolicy.createHTML(serializedHTML) : serializedHTML;
				};
				/**
				* Public method to set the configuration once
				* setConfig
				*
				* @param {Object} cfg configuration object
				*/
				DOMPurify$1.setConfig = function() {
					_parseConfig(arguments.length > 0 && arguments[0] !== void 0 ? arguments[0] : {});
					SET_CONFIG = true;
				};
				/**
				* Public method to remove the configuration
				* clearConfig
				*
				*/
				DOMPurify$1.clearConfig = function() {
					CONFIG = null;
					SET_CONFIG = false;
				};
				/**
				* Public method to check if an attribute value is valid.
				* Uses last set config, if any. Otherwise, uses config defaults.
				* isValidAttribute
				*
				* @param  {String} tag Tag name of containing element.
				* @param  {String} attr Attribute name.
				* @param  {String} value Attribute value.
				* @return {Boolean} Returns true if `value` is valid. Otherwise, returns false.
				*/
				DOMPurify$1.isValidAttribute = function(tag, attr, value) {
					if (!CONFIG) _parseConfig({});
					return _isValidAttribute(transformCaseFunc(tag), transformCaseFunc(attr), value);
				};
				/**
				* AddHook
				* Public method to add DOMPurify hooks
				*
				* @param {String} entryPoint entry point for the hook to add
				* @param {Function} hookFunction function to execute
				*/
				DOMPurify$1.addHook = function(entryPoint, hookFunction) {
					if (typeof hookFunction !== "function") return;
					hooks[entryPoint] = hooks[entryPoint] || [];
					arrayPush(hooks[entryPoint], hookFunction);
				};
				/**
				* RemoveHook
				* Public method to remove a DOMPurify hook at a given entryPoint
				* (pops it from the stack of hooks if more are present)
				*
				* @param {String} entryPoint entry point for the hook to remove
				* @return {Function} removed(popped) hook
				*/
				DOMPurify$1.removeHook = function(entryPoint) {
					if (hooks[entryPoint]) return arrayPop(hooks[entryPoint]);
				};
				/**
				* RemoveHooks
				* Public method to remove all DOMPurify hooks at a given entryPoint
				*
				* @param  {String} entryPoint entry point for the hooks to remove
				*/
				DOMPurify$1.removeHooks = function(entryPoint) {
					if (hooks[entryPoint]) hooks[entryPoint] = [];
				};
				/**
				* RemoveAllHooks
				* Public method to remove all DOMPurify hooks
				*/
				DOMPurify$1.removeAllHooks = function() {
					hooks = {};
				};
				return DOMPurify$1;
			}
			return createDOMPurify();
		}));
	}));

//#endregion
//#region node_modules/async-mutex/index.mjs
var import_purify = /* @__PURE__ */ __toESM(require_purify(), 1);
	const E_CANCELED = /* @__PURE__ */ new Error("request for lock canceled");
	var __awaiter$2 = function(thisArg, _arguments, P, generator) {
		function adopt(value) {
			return value instanceof P ? value : new P(function(resolve) {
				resolve(value);
			});
		}
		return new (P || (P = Promise))(function(resolve, reject) {
			function fulfilled(value) {
				try {
					step(generator.next(value));
				} catch (e) {
					reject(e);
				}
			}
			function rejected(value) {
				try {
					step(generator["throw"](value));
				} catch (e) {
					reject(e);
				}
			}
			function step(result) {
				result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected);
			}
			step((generator = generator.apply(thisArg, _arguments || [])).next());
		});
	};
	var Semaphore = class {
		constructor(_value, _cancelError = E_CANCELED) {
			this._value = _value;
			this._cancelError = _cancelError;
			this._weightedQueues = [];
			this._weightedWaiters = [];
		}
		acquire(weight = 1) {
			if (weight <= 0) throw new Error(`invalid weight ${weight}: must be positive`);
			return new Promise((resolve, reject) => {
				if (!this._weightedQueues[weight - 1]) this._weightedQueues[weight - 1] = [];
				this._weightedQueues[weight - 1].push({
					resolve,
					reject
				});
				this._dispatch();
			});
		}
		runExclusive(callback, weight = 1) {
			return __awaiter$2(this, void 0, void 0, function* () {
				const [value, release] = yield this.acquire(weight);
				try {
					return yield callback(value);
				} finally {
					release();
				}
			});
		}
		waitForUnlock(weight = 1) {
			if (weight <= 0) throw new Error(`invalid weight ${weight}: must be positive`);
			return new Promise((resolve) => {
				if (!this._weightedWaiters[weight - 1]) this._weightedWaiters[weight - 1] = [];
				this._weightedWaiters[weight - 1].push(resolve);
				this._dispatch();
			});
		}
		isLocked() {
			return this._value <= 0;
		}
		getValue() {
			return this._value;
		}
		setValue(value) {
			this._value = value;
			this._dispatch();
		}
		release(weight = 1) {
			if (weight <= 0) throw new Error(`invalid weight ${weight}: must be positive`);
			this._value += weight;
			this._dispatch();
		}
		cancel() {
			this._weightedQueues.forEach((queue) => queue.forEach((entry) => entry.reject(this._cancelError)));
			this._weightedQueues = [];
		}
		_dispatch() {
			var _a;
			for (let weight = this._value; weight > 0; weight--) {
				const queueEntry = (_a = this._weightedQueues[weight - 1]) === null || _a === void 0 ? void 0 : _a.shift();
				if (!queueEntry) continue;
				const previousValue = this._value;
				const previousWeight = weight;
				this._value -= weight;
				weight = this._value + 1;
				queueEntry.resolve([previousValue, this._newReleaser(previousWeight)]);
			}
			this._drainUnlockWaiters();
		}
		_newReleaser(weight) {
			let called = false;
			return () => {
				if (called) return;
				called = true;
				this.release(weight);
			};
		}
		_drainUnlockWaiters() {
			for (let weight = this._value; weight > 0; weight--) {
				if (!this._weightedWaiters[weight - 1]) continue;
				this._weightedWaiters[weight - 1].forEach((waiter) => waiter());
				this._weightedWaiters[weight - 1] = [];
			}
		}
	};
	var __awaiter$1 = function(thisArg, _arguments, P, generator) {
		function adopt(value) {
			return value instanceof P ? value : new P(function(resolve) {
				resolve(value);
			});
		}
		return new (P || (P = Promise))(function(resolve, reject) {
			function fulfilled(value) {
				try {
					step(generator.next(value));
				} catch (e) {
					reject(e);
				}
			}
			function rejected(value) {
				try {
					step(generator["throw"](value));
				} catch (e) {
					reject(e);
				}
			}
			function step(result) {
				result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected);
			}
			step((generator = generator.apply(thisArg, _arguments || [])).next());
		});
	};
	var Mutex = class {
		constructor(cancelError) {
			this._semaphore = new Semaphore(1, cancelError);
		}
		acquire() {
			return __awaiter$1(this, void 0, void 0, function* () {
				const [, releaser] = yield this._semaphore.acquire();
				return releaser;
			});
		}
		runExclusive(callback) {
			return this._semaphore.runExclusive(() => callback());
		}
		isLocked() {
			return this._semaphore.isLocked();
		}
		waitForUnlock() {
			return this._semaphore.waitForUnlock();
		}
		release() {
			if (this._semaphore.isLocked()) this._semaphore.release();
		}
		cancel() {
			return this._semaphore.cancel();
		}
	};

//#endregion
//#region src/WebFormsCore/Scripts/form.ts
	const callbackDefinitions = {};
	const pendingCallbacks = {};
	const sanitise = (input, options) => {
		const allowedTags = [];
		if (options.updateScripts) allowedTags.push("script");
		if (options.updateStyles) allowedTags.push("style");
		return import_purify.default.sanitize(input, {
			RETURN_TRUSTED_TYPE: true,
			WHOLE_DOCUMENT: true,
			ADD_TAGS: allowedTags
		});
	};
	const morphdom = morphdomFactory((fromEl, toEl) => {
		if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateAttributes", {
			cancelable: true,
			bubbles: true,
			detail: {
				node: fromEl,
				source: toEl
			}
		}))) return;
		morphAttrs(fromEl, toEl);
		if (!fromEl.dispatchEvent(new CustomEvent("wfc:updateAttributes", {
			bubbles: true,
			detail: {
				node: fromEl,
				source: toEl
			}
		}))) return;
	});
	const postbackMutex = new Mutex();
	let pendingPostbacks = 0;
	var ViewStateContainer = class {
		constructor(element, formData) {
			this.element = element;
			this.formData = formData;
		}
		querySelector(selector) {
			if (this.element) {
				const result = this.element.querySelector(selector);
				if (result) return result;
			}
			return document.body.closest(":not([data-wfc-form]) " + selector);
		}
		querySelectorAll(selector) {
			const elements = document.body.querySelectorAll(":not([data-wfc-form]) " + selector);
			if (this.element) return [...this.element.querySelectorAll(selector), ...elements];
			else return Array.from(elements);
		}
		addInputs(selector) {
			const elements = this.querySelectorAll(selector);
			for (let i = 0; i < elements.length; i++) {
				const element = elements[i];
				addElement(element, this.formData);
			}
		}
	};
	async function postBackElement(element, eventTarget, eventArgument, options) {
		if (!eventTarget) eventTarget = element.getAttribute("data-wfc-postback") ?? "";
		if ((options?.validate ?? true) && !await wfc.validate(element)) return;
		element.dispatchEvent(new CustomEvent("wfc:postbackTriggered"));
		try {
			const form = getRootForm(element);
			const streamPanel = getStreamPanel(element);
			if (streamPanel) await sendToStream(streamPanel, eventTarget, eventArgument);
			else await submitForm(element, form, eventTarget, eventArgument);
		} finally {
			element.dispatchEvent(new CustomEvent("wfc:afterPostbackTriggered"));
		}
	}
	function sendToStream(streamPanel, eventTarget, eventArgument) {
		const webSocket = streamPanel.webSocket;
		if (!webSocket) throw new Error("No WebSocket connection");
		const data = {
			t: eventTarget,
			a: eventArgument
		};
		webSocket.send(JSON.stringify(data));
		return Promise.resolve();
	}
	function addElement(element, formData) {
		if (element.type === "checkbox" || element.type === "radio") {
			if (element.checked) formData.append(element.name, element.value);
		} else formData.append(element.name, element.value);
	}
	function syncBooleanAttrProp(fromEl, toEl, name) {
		if (fromEl[name] !== toEl[name]) {
			fromEl[name] = toEl[name];
			if (fromEl[name]) fromEl.setAttribute(name, "");
			else fromEl.removeAttribute(name);
		}
	}
	function hasElementFile(element) {
		const elements = document.body.querySelectorAll("input[type=\"file\"]");
		for (let i = 0; i < elements.length; i++) if (elements[i].files.length > 0) return true;
		return false;
	}
	function getForm(element) {
		return element.closest("[data-wfc-form]");
	}
	function getRootForm(element) {
		let form = element.closest("[data-wfc-form]");
		if (!form) return null;
		while (form && form.tagName !== "FORM") {
			const parent = form.parentElement?.closest("[data-wfc-form]");
			if (!parent) break;
			form = parent;
		}
		return form;
	}
	function getStreamPanel(element) {
		return element.closest("[data-wfc-stream]");
	}
	function addInputs(formData, root, addFormElements, allowFileUploads) {
		const elements = [];
		for (const element of root.querySelectorAll("input, select, textarea")) if (!element.closest("[data-wfc-ignore]")) elements.push(element);
		document.dispatchEvent(new CustomEvent("wfc:addInputs", { detail: { elements } }));
		for (let i = 0; i < elements.length; i++) {
			const element = elements[i];
			if (element.hasAttribute("data-wfc-ignore") || element.type === "button" || element.type === "submit" || element.type === "reset") continue;
			if (element.closest("[data-wfc-ignore]")) continue;
			if (!addFormElements && getForm(element)) continue;
			if (getStreamPanel(element)) continue;
			if (!allowFileUploads && element.type === "file") continue;
			addElement(element, formData);
		}
	}
	function getFormData(form, eventTarget, eventArgument, allowFileUploads = true) {
		let formData;
		if (form) if (form.tagName === "FORM" && allowFileUploads) formData = new FormData(form);
		else {
			formData = new FormData();
			addInputs(formData, form, true, allowFileUploads);
		}
		else formData = new FormData();
		addInputs(formData, document.body, false, allowFileUploads);
		if (eventTarget) formData.append("wfcTarget", eventTarget);
		if (eventArgument) formData.append("wfcArgument", eventArgument);
		return formData;
	}
	function getOptions(data) {
		return {
			updateScripts: data[0] === "1",
			updateStyles: data[1] === "1"
		};
	}
	async function submitForm(element, form, eventTarget, eventArgument) {
		const baseElement = element.closest("[data-wfc-base]");
		let target;
		if (baseElement) target = baseElement;
		else target = document.body;
		const url = baseElement?.getAttribute("data-wfc-base") ?? location.toString();
		const formData = getFormData(form, eventTarget, eventArgument);
		const container = new ViewStateContainer(form, formData);
		pendingPostbacks++;
		const release = await postbackMutex.acquire();
		const interceptors = [];
		try {
			if (!target.dispatchEvent(new CustomEvent("wfc:beforeSubmit", {
				bubbles: true,
				cancelable: true,
				detail: {
					target,
					container,
					eventTarget,
					element,
					addRequestInterceptor(input) {
						interceptors.push(input);
					}
				}
			}))) return;
			const request = {
				method: "POST",
				redirect: "error",
				credentials: "include",
				headers: { "X-IsPostback": "true" }
			};
			request.body = hasElementFile(document.body) ? formData : new URLSearchParams(formData);
			for (const interceptor of interceptors) {
				const result = interceptor(request);
				if (result instanceof Promise) await result;
			}
			let response;
			try {
				response = await fetch(url, request);
			} catch (e) {
				target.dispatchEvent(new CustomEvent("wfc:submitError", {
					bubbles: true,
					detail: {
						form,
						eventTarget,
						response: void 0,
						error: e
					}
				}));
				throw e;
			}
			if (!response.ok) {
				target.dispatchEvent(new CustomEvent("wfc:submitError", {
					bubbles: true,
					detail: {
						form,
						eventTarget,
						response,
						error: void 0
					}
				}));
				throw new Error(response.statusText);
			}
			const redirectTo = response.headers.get("x-redirect-to");
			if (redirectTo) {
				window.location.assign(redirectTo);
				return;
			}
			const contentDisposition = response.headers.get("content-disposition");
			if (response.status === 204) {} else if (response.ok && contentDisposition && contentDisposition.indexOf("attachment") !== -1) receiveFile(element, response, contentDisposition);
			else {
				const text = await response.text();
				const jsOptions = response.headers.has("x-wfc-options") ? getOptions(response.headers.get("x-wfc-options")) : {
					updateScripts: false,
					updateStyles: false
				};
				const options = getMorpdomSettings(jsOptions, form);
				const htmlDoc = new DOMParser().parseFromString(sanitise(text, jsOptions), "text/html");
				if (form && form.getAttribute("data-wfc-form") === "self") morphdom(form, htmlDoc.querySelector("[data-wfc-form]"), options);
				else if (baseElement) morphdom(baseElement, htmlDoc.querySelector("[data-wfc-base]"), options);
				else {
					morphdom(document.head, htmlDoc.querySelector("head"), options);
					morphdom(document.body, htmlDoc.querySelector("body"), options);
				}
			}
		} finally {
			pendingPostbacks--;
			release();
			target.dispatchEvent(new CustomEvent("wfc:afterSubmit", {
				bubbles: true,
				detail: {
					target,
					container,
					form,
					eventTarget
				}
			}));
			const validationGroups = /* @__PURE__ */ new Set();
			validationGroups.add("");
			for (const element$1 of document.querySelectorAll("[data-wfc-validate]")) {
				const validationGroup = element$1.getAttribute("data-wfc-validate") ?? "";
				if (validationGroup) validationGroups.add(validationGroup);
			}
			for (const validationGroup of validationGroups) await wfc.validate(validationGroup, true);
		}
	}
	async function receiveFile(element, response, contentDisposition) {
		document.dispatchEvent(new CustomEvent("wfc:beforeFileDownload", { detail: {
			element,
			response
		} }));
		try {
			const contentEncoding = response.headers.get("content-encoding");
			const contentLength = response.headers.get(contentEncoding ? "x-file-size" : "content-length");
			if (contentLength) {
				const total = parseInt(contentLength, 10);
				let loaded = 0;
				const reader = response.body.getReader();
				let onProgress = function(loaded$1, total$1) {
					const percent = Math.round(loaded$1 / total$1 * 100);
					document.dispatchEvent(new CustomEvent("wfc:progressFileDownload", { detail: {
						element,
						response,
						loaded: loaded$1,
						total: total$1,
						percent
					} }));
				};
				response = new Response(new ReadableStream({ start(controller) {
					read();
					function read() {
						reader.read().then(({ done, value }) => {
							if (done) {
								if (total === 0) onProgress(loaded, total);
								controller.close();
								return;
							}
							loaded += value.byteLength;
							onProgress(loaded, total);
							controller.enqueue(value);
							read();
						}).catch((error) => {
							console.error(error);
							controller.error(error);
						});
					}
				} }));
			}
			const fileNameMatch = contentDisposition.match(/filename=(?:"([^"]+)"|([^;]+))/);
			const blob = await response.blob();
			const url = URL.createObjectURL(blob);
			const a = document.createElement("a");
			a.href = url;
			a.style.display = "none";
			if (fileNameMatch) a.download = fileNameMatch[1] ?? fileNameMatch[2];
			else a.download = "download";
			document.body.appendChild(a);
			a.click();
			setTimeout(() => {
				document.body.removeChild(a);
				URL.revokeObjectURL(url);
			}, 0);
		} finally {
			document.dispatchEvent(new CustomEvent("wfc:afterFileDownload", { detail: {
				element,
				response
			} }));
		}
	}
	function getMorpdomSettings(options, form) {
		return {
			getNodeKey(node) {
				if (node) {
					if (node.nodeName === "SCRIPT" && (node.src || node.innerHTML)) return node.src || node.innerHTML;
					if (node.nodeName === "TEMPLATE" && node.innerHTML) return node.innerHTML;
					if (node.nodeName === "STYLE" && node.innerHTML) return node.innerHTML;
					if (node.nodeName === "LINK" && node.href) return node.href;
					return node.getAttribute && node.getAttribute("id") || node.id;
				}
			},
			onBeforeNodeAdded: function(node) {
				if (node.nodeName === "TEMPLATE" && node.hasAttribute("data-wfc-callbacks")) {
					handleCallbacks(node);
					return false;
				}
				return node;
			},
			onNodeAdded(node) {
				document.dispatchEvent(new CustomEvent("wfc:addNode", { detail: {
					node,
					form
				} }));
				if (node.nodeType === Node.ELEMENT_NODE) document.dispatchEvent(new CustomEvent("wfc:addElement", { detail: {
					element: node,
					form
				} }));
				if (node.nodeName === "SCRIPT") {
					const script = document.createElement("script");
					for (let i = 0; i < node.attributes.length; i++) {
						const attr = node.attributes[i];
						script.setAttribute(attr.name, attr.value);
					}
					script.innerHTML = node.innerHTML;
					node.replaceWith(script);
				}
			},
			onBeforeElUpdated: function(fromEl, toEl) {
				if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateNode", {
					cancelable: true,
					bubbles: true,
					detail: {
						node: fromEl,
						source: toEl,
						form
					}
				}))) return false;
				if (fromEl.nodeType === Node.ELEMENT_NODE && !fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateElement", {
					cancelable: true,
					bubbles: true,
					detail: {
						element: fromEl,
						source: toEl,
						form
					}
				}))) return false;
				if (fromEl.hasAttribute("data-wfc-ignore") || toEl.hasAttribute("data-wfc-ignore")) return false;
				if (fromEl.tagName === "INPUT" && fromEl.type !== "hidden") {
					const hasValue = toEl.hasAttribute("value");
					if (!hasValue && fromEl.hasAttribute("value")) toEl.setAttribute("value", fromEl.getAttribute("value") ?? "");
					morphAttrs(fromEl, toEl);
					if (hasValue && fromEl.value !== toEl.value) fromEl.value = toEl.value;
					syncBooleanAttrProp(fromEl, toEl, "checked");
					syncBooleanAttrProp(fromEl, toEl, "disabled");
					return false;
				}
				if (fromEl.tagName === "TEXTAREA") {
					morphAttrs(fromEl, toEl);
					if (fromEl.value !== toEl.value) fromEl.value = toEl.value;
					syncBooleanAttrProp(fromEl, toEl, "disabled");
					return false;
				}
				if (fromEl.nodeName === "SCRIPT" && toEl.nodeName === "SCRIPT") {
					if (fromEl.src === toEl.src && fromEl.innerHTML === toEl.innerHTML) return false;
					const script = document.createElement("script");
					for (let i = 0; i < toEl.attributes.length; i++) {
						const attr = toEl.attributes[i];
						script.setAttribute(attr.name, attr.value);
					}
					script.innerHTML = toEl.innerHTML;
					fromEl.replaceWith(script);
					return false;
				}
			},
			onElUpdated(el) {
				if (el.nodeType === Node.ELEMENT_NODE) el.dispatchEvent(new CustomEvent("wfc:updateElement", {
					bubbles: true,
					detail: {
						element: el,
						form
					}
				}));
			},
			onBeforeNodeDiscarded(node) {
				if (node.tagName === "SCRIPT" && !options.updateScripts || node.tagName === "STYLE" && !options.updateStyles || node.tagName === "LINK" && node.hasAttribute("rel") && node.getAttribute("rel") === "stylesheet" && !options.updateStyles) return false;
				if (node instanceof Element && node.hasAttribute("data-wfc-form")) return false;
				if (node.tagName === "DIV" && node.hasAttribute("data-wfc-owner") && (node.getAttribute("data-wfc-owner") ?? "") !== (form?.id ?? "")) return false;
				if (!node.dispatchEvent(new CustomEvent("wfc:discardNode", {
					bubbles: true,
					cancelable: true,
					detail: {
						node,
						form
					}
				}))) return false;
				if (node.nodeType === Node.ELEMENT_NODE && !node.dispatchEvent(new CustomEvent("wfc:discardElement", {
					bubbles: true,
					cancelable: true,
					detail: {
						element: node,
						form
					}
				}))) return false;
			}
		};
	}
	const originalSubmit = HTMLFormElement.prototype.submit;
	HTMLFormElement.prototype.submit = async function() {
		if (this.hasAttribute("data-wfc-form")) await submitForm(this, this);
		else originalSubmit.call(this);
	};
	async function handleCallbacks(element) {
		const callbacks = JSON.parse(element.innerHTML);
		if (!callbacks || !Array.isArray(callbacks)) return;
		for (const callback of callbacks) {
			const { k: key, a: argument } = callback;
			if (key in callbackDefinitions) {
				const cb = callbackDefinitions[key];
				if (typeof cb === "function") try {
					cb(argument);
				} catch (e) {
					console.error(`Error executing callback ${key}`, e);
				}
				else console.warn(`Callback ${key} is not a function`, cb);
			} else if (key in pendingCallbacks) {
				const arr = pendingCallbacks[key];
				if (arr.length < 100) arr.push(argument);
				else console.warn(`Too many pending callbacks for ${key}, discarding callback`, callback);
			} else pendingCallbacks[key] = [argument];
		}
	}
	document.addEventListener("DOMContentLoaded", async function() {
		const release = await postbackMutex.acquire();
		try {
			const callbacks = document.querySelectorAll("template[data-wfc-callbacks]");
			for (const callback of callbacks) {
				handleCallbacks(callback);
				callback.remove();
			}
		} finally {
			release();
		}
	});
	document.addEventListener("submit", async function(e) {
		if (e.target instanceof Element && e.target.hasAttribute("data-wfc-form")) {
			e.preventDefault();
			await submitForm(e.target, e.target);
		}
	});
	document.addEventListener("click", async function(e) {
		if (!(e.target instanceof Element)) return;
		const postbackControl = e.target?.closest("[data-wfc-postback]");
		if (!postbackControl) return;
		e.preventDefault();
		if (postbackControl.getAttribute("data-wfc-disabled") === "true") return;
		await postBackElement(e.target);
	});
	document.addEventListener("keypress", async function(e) {
		if (e.key !== "Enter" && e.keyCode !== 13 && e.which !== 13) return;
		if (!(e.target instanceof Element) || e.target.tagName !== "INPUT") return;
		const type = e.target.getAttribute("type");
		if (type === "button" || type === "submit" || type === "reset") return;
		const eventTarget = e.target.getAttribute("name");
		e.preventDefault();
		await postBackElement(e.target, eventTarget, "ENTER");
	});
	const timeouts = {};
	document.addEventListener("input", function(e) {
		if (!(e.target instanceof Element) || e.target.tagName !== "INPUT" || !e.target.hasAttribute("data-wfc-autopostback")) return;
		const type = e.target.getAttribute("type");
		if (type === "button" || type === "submit" || type === "reset") return;
		postBackChange(e.target);
	});
	function postBackChange(target, timeOut = 1e3, eventArgument = "CHANGE", options) {
		const container = getStreamPanel(target) ?? getForm(target);
		const eventTarget = target.getAttribute("name");
		const key = (container?.id ?? "") + eventTarget + eventArgument;
		if (timeouts[key]) {
			pendingPostbacks--;
			clearTimeout(timeouts[key]);
		}
		pendingPostbacks++;
		timeouts[key] = setTimeout(async () => {
			pendingPostbacks--;
			delete timeouts[key];
			await postBackElement(target, eventTarget, eventArgument, options);
		}, timeOut);
	}
	function postBack(target, eventArgument, options) {
		return postBackElement(target, target.getAttribute("name"), eventArgument, options);
	}
	document.addEventListener("change", async function(e) {
		if (e.target instanceof Element && e.target.hasAttribute("data-wfc-autopostback")) postBackChange(e.target, 10);
	});
	const wfc = {
		_callbacks: callbackDefinitions,
		_pendingCallbacks: pendingCallbacks,
		hiddenClass: "",
		postBackChange,
		postBack,
		get hasPendingPostbacks() {
			return pendingPostbacks > 0;
		},
		init: function(arg) {
			arg();
		},
		show: function(element) {
			if (wfc.hiddenClass) element.classList.remove(wfc.hiddenClass);
			else element.style.display = "";
		},
		hide: function(element) {
			if (wfc.hiddenClass) element.classList.add(wfc.hiddenClass);
			else element.style.display = "none";
		},
		toggle: function(element, value) {
			if (value) wfc.show(element);
			else wfc.hide(element);
		},
		validate: async function(validationGroup = "", serverOnly = false) {
			if (typeof validationGroup === "object" && validationGroup instanceof Element) {
				if (!validationGroup.hasAttribute("data-wfc-validate")) return true;
				validationGroup = validationGroup.getAttribute("data-wfc-validate") ?? "";
			}
			const validators = [];
			const validatorsByElement = /* @__PURE__ */ new Map();
			const detail = { addValidator(validator, element) {
				if (element) {
					if (!validatorsByElement.has(element)) validatorsByElement.set(element, []);
					validatorsByElement.get(element).push(validator);
				} else validators.push(validator);
			} };
			for (const element of document.querySelectorAll("[data-wfc-validate]")) {
				if ((element.getAttribute("data-wfc-validate") ?? "") !== validationGroup) continue;
				element.dispatchEvent(new CustomEvent("wfc:validate", {
					bubbles: true,
					detail
				}));
			}
			let isValid = true;
			for (const validator of validators) try {
				if (!await validator(serverOnly)) isValid = false;
			} catch (e) {
				console.error("Validation error:", e);
				isValid = false;
			}
			for (const [element, elementValidators] of validatorsByElement.entries()) {
				let isElementValid = true;
				for (const validator of elementValidators) try {
					if (!await validator(serverOnly)) isElementValid = false;
				} catch (e) {
					console.error("Validation error:", e);
					isElementValid = false;
				}
				if (!isElementValid) isValid = false;
				element.dispatchEvent(new CustomEvent("wfc:elementValidated", {
					bubbles: true,
					detail: {
						isValid: isElementValid,
						element
					}
				}));
			}
			document.dispatchEvent(new CustomEvent("wfc:validated", {
				bubbles: true,
				detail: { isValid }
			}));
			return isValid;
		},
		bind: async function(selectors, options) {
			const init = (options.init ?? function() {}).bind(options);
			const update = (options.update ?? function() {}).bind(options);
			const afterUpdate = (options.afterUpdate ?? function() {}).bind(options);
			const submit = options.submit?.bind(options);
			const destroy = options.destroy?.bind(options);
			for (const element of document.querySelectorAll(selectors)) {
				await init(element);
				update(element, element);
				afterUpdate(element);
			}
			document.addEventListener("wfc:addElement", async function(e) {
				const { element } = e.detail;
				if (element.matches(selectors)) {
					await init(element);
					update(element, element);
					afterUpdate(element);
				}
			});
			document.addEventListener("wfc:beforeUpdateElement", function(e) {
				const { element, source } = e.detail;
				if (element.matches(selectors) && update(element, source)) e.preventDefault();
			});
			if (afterUpdate) document.addEventListener("wfc:updateElement", function(e) {
				const { element } = e.detail;
				if (element.matches(selectors)) afterUpdate(element);
			});
			if (submit) document.addEventListener("wfc:beforeSubmit", function(e) {
				const { container } = e.detail;
				for (const element of container.querySelectorAll(selectors)) submit(element, container.formData);
			});
			if (destroy) document.addEventListener("wfc:discardElement", function(e) {
				const { element } = e.detail;
				if (element.matches(selectors)) destroy(element);
			});
		},
		registerCallback: async function(name, callback) {
			callbackDefinitions[name] = callback;
			if (!(name in pendingCallbacks)) return;
			const pending = pendingCallbacks[name];
			delete pendingCallbacks[name];
			for (const arg of pending) try {
				callback(arg);
			} catch (e) {
				console.error(`Error executing callback ${name}`, e);
			}
		},
		bindValidator: function(selectors, options) {
			wfc.bind(selectors, {
				init: function(element) {
					element._isValid = true;
					if ("init" in options) options.init(element);
				},
				afterUpdate: function(element) {
					const isValidStr = element.getAttribute("data-wfc-validated");
					if (isValidStr) element._isValid = isValidStr === "true";
					else wfc.toggle(element, !element._isValid);
					const idToValidate = element.getAttribute("data-wfc-validator");
					if (!idToValidate) {
						console.warn("No data-wfc-validator attribute found", element);
						return;
					}
					const elementToValidate = document.getElementById(idToValidate);
					if (element._elementToValidate === elementToValidate) return;
					this.destroy(element);
					element._elementToValidate = elementToValidate;
					if (!elementToValidate) {
						console.warn(`Element with id ${idToValidate} not found`);
						return;
					}
					element._callback = async function(e) {
						e.detail.addValidator(async function(serverOnly) {
							if (serverOnly) return element._isValid;
							const isValid = element.hasAttribute("data-wfc-disabled") || (options.validate ? await options.validate(elementToValidate, element) : this._isValid);
							element._isValid = isValid;
							wfc.toggle(element, !isValid);
							return isValid;
						}, elementToValidate);
					};
					elementToValidate.addEventListener("wfc:validate", element._callback);
				},
				destroy: function(element) {
					if (element._callback && element._elementToValidate) {
						element._elementToValidate.removeEventListener("wfc:validate", element._callback);
						element._callback = void 0;
						element._elementToValidate = void 0;
					}
				}
			});
		},
		getStringValue: async (element) => {
			if ("getStringValue" in element) return (await Promise.resolve(element.getStringValue()))?.toString() ?? "";
			else if ("value" in element) return element.value?.toString() ?? "";
			else if ("textContent" in element) return element.textContent ?? "";
			else return "";
		},
		isEmpty: async (element, initialValue = "") => {
			if ("isEmpty" in element) {
				const isEmpty = element.isEmpty;
				if (typeof isEmpty === "function") {
					const result = await Promise.resolve(isEmpty(initialValue));
					if (typeof result === "boolean") return result;
					console.warn("isEmpty did not return a boolean", element);
				} else if (typeof isEmpty === "boolean") return isEmpty;
				else console.warn("isEmpty is not a function", element);
			}
			return initialValue === await wfc.getStringValue(element);
		}
	};
	wfc.bind("[data-wfc-stream]", {
		init: function(element) {
			const id = element.id;
			const baseElement = element.closest("[data-wfc-base]");
			const url = baseElement ? new URL(baseElement.getAttribute("data-wfc-base")) : location;
			let search = url.search;
			if (!search) search = "?";
			else search += "&";
			search += "__panel=" + id;
			const webSocket = new WebSocket((url.protocol === "https:" ? "wss://" : "ws://") + url.host + url.pathname + search);
			element.webSocket = webSocket;
			element.isUpdating = false;
			webSocket.addEventListener("message", function(e) {
				const parser = new DOMParser();
				const index = e.data.indexOf("|");
				const options = getOptions(e.data.substring(0, index));
				const data = e.data.substring(index + 1);
				const htmlDoc = parser.parseFromString(sanitise(`<!DOCTYPE html><html><body>${data}</body></html>`, options), "text/html");
				element.isUpdating = true;
				morphdom(element, htmlDoc.getElementById(id), getMorpdomSettings(options));
				element.isUpdating = false;
			});
			webSocket.addEventListener("open", function() {
				const formData = getFormData(element);
				webSocket.send(new URLSearchParams(formData).toString());
			});
		},
		update: function(element, source) {
			if (!element.isUpdating) return true;
		},
		destroy: function(element) {
			const webSocket = element.webSocket;
			if (webSocket) webSocket.close();
		}
	});
	wfc.bind("[data-wfc-lazy]", {
		init: async function(element) {
			const uniqueId = element.getAttribute("data-wfc-lazy");
			if (!uniqueId) return;
			setTimeout(async () => {
				await postBackElement(element, uniqueId, "LAZY_LOAD", { validate: false });
			}, 0);
		},
		update: function(element, source) {
			const elementLazy = element.getAttribute("data-wfc-lazy");
			const sourceLazy = source.getAttribute("data-wfc-lazy");
			if (elementLazy === "" && sourceLazy) return true;
		}
	});
	if ("wfc" in window) {
		const current = window.wfc;
		if ("hiddenClass" in current) wfc.hiddenClass = current.hiddenClass;
		window.wfc = wfc;
		if ("_" in current) for (const bind of current._) {
			const [type, p1, p2] = bind;
			if (type === 0) wfc.bind(p1, p2);
			else if (type === 1) wfc.bindValidator(p1, p2);
			else if (type === 2) wfc.init(p2);
			else if (type === 3) wfc.registerCallback(p1, p2);
			else console.warn("Unknown bind type", type);
		}
	}
	wfc.bindValidator("[data-wfc-requiredvalidator]", { validate: async function(elementToValidate, validator) {
		return !await wfc.isEmpty(elementToValidate, validator.getAttribute("data-wfc-requiredvalidator"));
	} });
	wfc.bindValidator("[data-wfc-customvalidator]", { validate: function() {
		return true;
	} });
	window.wfc = wfc;

//#endregion
})();