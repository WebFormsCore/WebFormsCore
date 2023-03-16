(function () {
    'use strict';

    var DOCUMENT_FRAGMENT_NODE$1 = 11;

    function morphAttrs(fromNode, toNode) {
        var toNodeAttrs = toNode.attributes;
        var attr;
        var attrName;
        var attrNamespaceURI;
        var attrValue;
        var fromValue;

        // document-fragments dont have attributes so lets not do anything
        if (toNode.nodeType === DOCUMENT_FRAGMENT_NODE$1 || fromNode.nodeType === DOCUMENT_FRAGMENT_NODE$1) {
          return;
        }

        // update attributes on original DOM element
        for (var i = toNodeAttrs.length - 1; i >= 0; i--) {
            attr = toNodeAttrs[i];
            attrName = attr.name;
            attrNamespaceURI = attr.namespaceURI;
            attrValue = attr.value;

            if (attrNamespaceURI) {
                attrName = attr.localName || attrName;
                fromValue = fromNode.getAttributeNS(attrNamespaceURI, attrName);

                if (fromValue !== attrValue) {
                    if (attr.prefix === 'xmlns'){
                        attrName = attr.name; // It's not allowed to set an attribute with the XMLNS namespace without specifying the `xmlns` prefix
                    }
                    fromNode.setAttributeNS(attrNamespaceURI, attrName, attrValue);
                }
            } else {
                fromValue = fromNode.getAttribute(attrName);

                if (fromValue !== attrValue) {
                    fromNode.setAttribute(attrName, attrValue);
                }
            }
        }

        // Remove any extra attributes found on the original DOM element that
        // weren't found on the target element.
        var fromNodeAttrs = fromNode.attributes;

        for (var d = fromNodeAttrs.length - 1; d >= 0; d--) {
            attr = fromNodeAttrs[d];
            attrName = attr.name;
            attrNamespaceURI = attr.namespaceURI;

            if (attrNamespaceURI) {
                attrName = attr.localName || attrName;

                if (!toNode.hasAttributeNS(attrNamespaceURI, attrName)) {
                    fromNode.removeAttributeNS(attrNamespaceURI, attrName);
                }
            } else {
                if (!toNode.hasAttribute(attrName)) {
                    fromNode.removeAttribute(attrName);
                }
            }
        }
    }

    var range; // Create a range object for efficently rendering strings to elements.
    var NS_XHTML = 'http://www.w3.org/1999/xhtml';

    var doc = typeof document === 'undefined' ? undefined : document;
    var HAS_TEMPLATE_SUPPORT = !!doc && 'content' in doc.createElement('template');
    var HAS_RANGE_SUPPORT = !!doc && doc.createRange && 'createContextualFragment' in doc.createRange();

    function createFragmentFromTemplate(str) {
        var template = doc.createElement('template');
        template.innerHTML = str;
        return template.content.childNodes[0];
    }

    function createFragmentFromRange(str) {
        if (!range) {
            range = doc.createRange();
            range.selectNode(doc.body);
        }

        var fragment = range.createContextualFragment(str);
        return fragment.childNodes[0];
    }

    function createFragmentFromWrap(str) {
        var fragment = doc.createElement('body');
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
        if (HAS_TEMPLATE_SUPPORT) {
          // avoid restrictions on content for things like `<tr><th>Hi</th></tr>` which
          // createContextualFragment doesn't support
          // <template> support not available in IE
          return createFragmentFromTemplate(str);
        } else if (HAS_RANGE_SUPPORT) {
          return createFragmentFromRange(str);
        }

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

        if (fromNodeName === toNodeName) {
            return true;
        }

        fromCodeStart = fromNodeName.charCodeAt(0);
        toCodeStart = toNodeName.charCodeAt(0);

        // If the target element is a virtual DOM node or SVG node then we may
        // need to normalize the tag name before comparing. Normal HTML elements that are
        // in the "http://www.w3.org/1999/xhtml"
        // are converted to upper case
        if (fromCodeStart <= 90 && toCodeStart >= 97) { // from is upper and to is lower
            return fromNodeName === toNodeName.toUpperCase();
        } else if (toCodeStart <= 90 && fromCodeStart >= 97) { // to is upper and from is lower
            return toNodeName === fromNodeName.toUpperCase();
        } else {
            return false;
        }
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
        return !namespaceURI || namespaceURI === NS_XHTML ?
            doc.createElement(name) :
            doc.createElementNS(namespaceURI, name);
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

    function syncBooleanAttrProp$1(fromEl, toEl, name) {
        if (fromEl[name] !== toEl[name]) {
            fromEl[name] = toEl[name];
            if (fromEl[name]) {
                fromEl.setAttribute(name, '');
            } else {
                fromEl.removeAttribute(name);
            }
        }
    }

    var specialElHandlers = {
        OPTION: function(fromEl, toEl) {
            var parentNode = fromEl.parentNode;
            if (parentNode) {
                var parentName = parentNode.nodeName.toUpperCase();
                if (parentName === 'OPTGROUP') {
                    parentNode = parentNode.parentNode;
                    parentName = parentNode && parentNode.nodeName.toUpperCase();
                }
                if (parentName === 'SELECT' && !parentNode.hasAttribute('multiple')) {
                    if (fromEl.hasAttribute('selected') && !toEl.selected) {
                        // Workaround for MS Edge bug where the 'selected' attribute can only be
                        // removed if set to a non-empty value:
                        // https://developer.microsoft.com/en-us/microsoft-edge/platform/issues/12087679/
                        fromEl.setAttribute('selected', 'selected');
                        fromEl.removeAttribute('selected');
                    }
                    // We have to reset select element's selectedIndex to -1, otherwise setting
                    // fromEl.selected using the syncBooleanAttrProp below has no effect.
                    // The correct selectedIndex will be set in the SELECT special handler below.
                    parentNode.selectedIndex = -1;
                }
            }
            syncBooleanAttrProp$1(fromEl, toEl, 'selected');
        },
        /**
         * The "value" attribute is special for the <input> element since it sets
         * the initial value. Changing the "value" attribute without changing the
         * "value" property will have no effect since it is only used to the set the
         * initial value.  Similar for the "checked" attribute, and "disabled".
         */
        INPUT: function(fromEl, toEl) {
            syncBooleanAttrProp$1(fromEl, toEl, 'checked');
            syncBooleanAttrProp$1(fromEl, toEl, 'disabled');

            if (fromEl.value !== toEl.value) {
                fromEl.value = toEl.value;
            }

            if (!toEl.hasAttribute('value')) {
                fromEl.removeAttribute('value');
            }
        },

        TEXTAREA: function(fromEl, toEl) {
            var newValue = toEl.value;
            if (fromEl.value !== newValue) {
                fromEl.value = newValue;
            }

            var firstChild = fromEl.firstChild;
            if (firstChild) {
                // Needed for IE. Apparently IE sets the placeholder as the
                // node value and vise versa. This ignores an empty update.
                var oldValue = firstChild.nodeValue;

                if (oldValue == newValue || (!newValue && oldValue == fromEl.placeholder)) {
                    return;
                }

                firstChild.nodeValue = newValue;
            }
        },
        SELECT: function(fromEl, toEl) {
            if (!toEl.hasAttribute('multiple')) {
                var selectedIndex = -1;
                var i = 0;
                // We have to loop through children of fromEl, not toEl since nodes can be moved
                // from toEl to fromEl directly when morphing.
                // At the time this special handler is invoked, all children have already been morphed
                // and appended to / removed from fromEl, so using fromEl here is safe and correct.
                var curChild = fromEl.firstChild;
                var optgroup;
                var nodeName;
                while(curChild) {
                    nodeName = curChild.nodeName && curChild.nodeName.toUpperCase();
                    if (nodeName === 'OPTGROUP') {
                        optgroup = curChild;
                        curChild = optgroup.firstChild;
                    } else {
                        if (nodeName === 'OPTION') {
                            if (curChild.hasAttribute('selected')) {
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

    var ELEMENT_NODE = 1;
    var DOCUMENT_FRAGMENT_NODE = 11;
    var TEXT_NODE = 3;
    var COMMENT_NODE = 8;

    function noop() {}

    function defaultGetNodeKey(node) {
      if (node) {
        return (node.getAttribute && node.getAttribute('id')) || node.id;
      }
    }

    function morphdomFactory(morphAttrs) {

      return function morphdom(fromNode, toNode, options) {
        if (!options) {
          options = {};
        }

        if (typeof toNode === 'string') {
          if (fromNode.nodeName === '#document' || fromNode.nodeName === 'HTML' || fromNode.nodeName === 'BODY') {
            var toNodeHtml = toNode;
            toNode = doc.createElement('html');
            toNode.innerHTML = toNodeHtml;
          } else {
            toNode = toElement(toNode);
          }
        } else if (toNode.nodeType === DOCUMENT_FRAGMENT_NODE) {
          toNode = toNode.firstElementChild;
        }

        var getNodeKey = options.getNodeKey || defaultGetNodeKey;
        var onBeforeNodeAdded = options.onBeforeNodeAdded || noop;
        var onNodeAdded = options.onNodeAdded || noop;
        var onBeforeElUpdated = options.onBeforeElUpdated || noop;
        var onElUpdated = options.onElUpdated || noop;
        var onBeforeNodeDiscarded = options.onBeforeNodeDiscarded || noop;
        var onNodeDiscarded = options.onNodeDiscarded || noop;
        var onBeforeElChildrenUpdated = options.onBeforeElChildrenUpdated || noop;
        var skipFromChildren = options.skipFromChildren || noop;
        var addChild = options.addChild || function(parent, child){ return parent.appendChild(child); };
        var childrenOnly = options.childrenOnly === true;

        // This object is used as a lookup to quickly find all keyed elements in the original DOM tree.
        var fromNodesLookup = Object.create(null);
        var keyedRemovalList = [];

        function addKeyedRemoval(key) {
          keyedRemovalList.push(key);
        }

        function walkDiscardedChildNodes(node, skipKeyedNodes) {
          if (node.nodeType === ELEMENT_NODE) {
            var curChild = node.firstChild;
            while (curChild) {

              var key = undefined;

              if (skipKeyedNodes && (key = getNodeKey(curChild))) {
                // If we are skipping keyed nodes then we add the key
                // to a list so that it can be handled at the very end.
                addKeyedRemoval(key);
              } else {
                // Only report the node as discarded if it is not keyed. We do this because
                // at the end we loop through all keyed elements that were unmatched
                // and then discard them in one final pass.
                onNodeDiscarded(curChild);
                if (curChild.firstChild) {
                  walkDiscardedChildNodes(curChild, skipKeyedNodes);
                }
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
          if (onBeforeNodeDiscarded(node) === false) {
            return;
          }

          if (parentNode) {
            parentNode.removeChild(node);
          }

          onNodeDiscarded(node);
          walkDiscardedChildNodes(node, skipKeyedNodes);
        }

        // // TreeWalker implementation is no faster, but keeping this around in case this changes in the future
        // function indexTree(root) {
        //     var treeWalker = document.createTreeWalker(
        //         root,
        //         NodeFilter.SHOW_ELEMENT);
        //
        //     var el;
        //     while((el = treeWalker.nextNode())) {
        //         var key = getNodeKey(el);
        //         if (key) {
        //             fromNodesLookup[key] = el;
        //         }
        //     }
        // }

        // // NodeIterator implementation is no faster, but keeping this around in case this changes in the future
        //
        // function indexTree(node) {
        //     var nodeIterator = document.createNodeIterator(node, NodeFilter.SHOW_ELEMENT);
        //     var el;
        //     while((el = nodeIterator.nextNode())) {
        //         var key = getNodeKey(el);
        //         if (key) {
        //             fromNodesLookup[key] = el;
        //         }
        //     }
        // }

        function indexTree(node) {
          if (node.nodeType === ELEMENT_NODE || node.nodeType === DOCUMENT_FRAGMENT_NODE) {
            var curChild = node.firstChild;
            while (curChild) {
              var key = getNodeKey(curChild);
              if (key) {
                fromNodesLookup[key] = curChild;
              }

              // Walk recursively
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
              // if we find a duplicate #id node in cache, replace `el` with cache value
              // and morph it to the child node.
              if (unmatchedFromEl && compareNodeNames(curChild, unmatchedFromEl)) {
                curChild.parentNode.replaceChild(unmatchedFromEl, curChild);
                morphEl(unmatchedFromEl, curChild);
              } else {
                handleNodeAdded(curChild);
              }
            } else {
              // recursively call for curChild and it's children to see if we find something in
              // fromNodesLookup
              handleNodeAdded(curChild);
            }

            curChild = nextSibling;
          }
        }

        function cleanupFromEl(fromEl, curFromNodeChild, curFromNodeKey) {
          // We have processed all of the "to nodes". If curFromNodeChild is
          // non-null then we still have some from nodes left over that need
          // to be removed
          while (curFromNodeChild) {
            var fromNextSibling = curFromNodeChild.nextSibling;
            if ((curFromNodeKey = getNodeKey(curFromNodeChild))) {
              // Since the node is keyed it might be matched up later so we defer
              // the actual removal to later
              addKeyedRemoval(curFromNodeKey);
            } else {
              // NOTE: we skip nested keyed nodes from being removed since there is
              //       still a chance they will be matched up later
              removeNode(curFromNodeChild, fromEl, true /* skip keyed nodes */);
            }
            curFromNodeChild = fromNextSibling;
          }
        }

        function morphEl(fromEl, toEl, childrenOnly) {
          var toElKey = getNodeKey(toEl);

          if (toElKey) {
            // If an element with an ID is being morphed then it will be in the final
            // DOM so clear it out of the saved elements collection
            delete fromNodesLookup[toElKey];
          }

          if (!childrenOnly) {
            // optional
            if (onBeforeElUpdated(fromEl, toEl) === false) {
              return;
            }

            // update attributes on original DOM element first
            morphAttrs(fromEl, toEl);
            // optional
            onElUpdated(fromEl);

            if (onBeforeElChildrenUpdated(fromEl, toEl) === false) {
              return;
            }
          }

          if (fromEl.nodeName !== 'TEXTAREA') {
            morphChildren(fromEl, toEl);
          } else {
            specialElHandlers.TEXTAREA(fromEl, toEl);
          }
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

          // walk the children
          outer: while (curToNodeChild) {
            toNextSibling = curToNodeChild.nextSibling;
            curToNodeKey = getNodeKey(curToNodeChild);

            // walk the fromNode children all the way through
            while (!skipFrom && curFromNodeChild) {
              fromNextSibling = curFromNodeChild.nextSibling;

              if (curToNodeChild.isSameNode && curToNodeChild.isSameNode(curFromNodeChild)) {
                curToNodeChild = toNextSibling;
                curFromNodeChild = fromNextSibling;
                continue outer;
              }

              curFromNodeKey = getNodeKey(curFromNodeChild);

              var curFromNodeType = curFromNodeChild.nodeType;

              // this means if the curFromNodeChild doesnt have a match with the curToNodeChild
              var isCompatible = undefined;

              if (curFromNodeType === curToNodeChild.nodeType) {
                if (curFromNodeType === ELEMENT_NODE) {
                  // Both nodes being compared are Element nodes

                  if (curToNodeKey) {
                    // The target node has a key so we want to match it up with the correct element
                    // in the original DOM tree
                    if (curToNodeKey !== curFromNodeKey) {
                      // The current element in the original DOM tree does not have a matching key so
                      // let's check our lookup to see if there is a matching element in the original
                      // DOM tree
                      if ((matchingFromEl = fromNodesLookup[curToNodeKey])) {
                        if (fromNextSibling === matchingFromEl) {
                          // Special case for single element removals. To avoid removing the original
                          // DOM node out of the tree (since that can break CSS transitions, etc.),
                          // we will instead discard the current node and wait until the next
                          // iteration to properly match up the keyed target element with its matching
                          // element in the original tree
                          isCompatible = false;
                        } else {
                          // We found a matching keyed element somewhere in the original DOM tree.
                          // Let's move the original DOM node into the current position and morph
                          // it.

                          // NOTE: We use insertBefore instead of replaceChild because we want to go through
                          // the `removeNode()` function for the node that is being discarded so that
                          // all lifecycle hooks are correctly invoked
                          fromEl.insertBefore(matchingFromEl, curFromNodeChild);

                          // fromNextSibling = curFromNodeChild.nextSibling;

                          if (curFromNodeKey) {
                            // Since the node is keyed it might be matched up later so we defer
                            // the actual removal to later
                            addKeyedRemoval(curFromNodeKey);
                          } else {
                            // NOTE: we skip nested keyed nodes from being removed since there is
                            //       still a chance they will be matched up later
                            removeNode(curFromNodeChild, fromEl, true /* skip keyed nodes */);
                          }

                          curFromNodeChild = matchingFromEl;
                        }
                      } else {
                        // The nodes are not compatible since the "to" node has a key and there
                        // is no matching keyed node in the source tree
                        isCompatible = false;
                      }
                    }
                  } else if (curFromNodeKey) {
                    // The original has a key
                    isCompatible = false;
                  }

                  isCompatible = isCompatible !== false && compareNodeNames(curFromNodeChild, curToNodeChild);
                  if (isCompatible) {
                    // We found compatible DOM elements so transform
                    // the current "from" node to match the current
                    // target DOM node.
                    // MORPH
                    morphEl(curFromNodeChild, curToNodeChild);
                  }

                } else if (curFromNodeType === TEXT_NODE || curFromNodeType == COMMENT_NODE) {
                  // Both nodes being compared are Text or Comment nodes
                  isCompatible = true;
                  // Simply update nodeValue on the original node to
                  // change the text value
                  if (curFromNodeChild.nodeValue !== curToNodeChild.nodeValue) {
                    curFromNodeChild.nodeValue = curToNodeChild.nodeValue;
                  }

                }
              }

              if (isCompatible) {
                // Advance both the "to" child and the "from" child since we found a match
                // Nothing else to do as we already recursively called morphChildren above
                curToNodeChild = toNextSibling;
                curFromNodeChild = fromNextSibling;
                continue outer;
              }

              // No compatible match so remove the old node from the DOM and continue trying to find a
              // match in the original DOM. However, we only do this if the from node is not keyed
              // since it is possible that a keyed node might match up with a node somewhere else in the
              // target tree and we don't want to discard it just yet since it still might find a
              // home in the final DOM tree. After everything is done we will remove any keyed nodes
              // that didn't find a home
              if (curFromNodeKey) {
                // Since the node is keyed it might be matched up later so we defer
                // the actual removal to later
                addKeyedRemoval(curFromNodeKey);
              } else {
                // NOTE: we skip nested keyed nodes from being removed since there is
                //       still a chance they will be matched up later
                removeNode(curFromNodeChild, fromEl, true /* skip keyed nodes */);
              }

              curFromNodeChild = fromNextSibling;
            } // END: while(curFromNodeChild) {}

            // If we got this far then we did not find a candidate match for
            // our "to node" and we exhausted all of the children "from"
            // nodes. Therefore, we will just append the current "to" node
            // to the end
            if (curToNodeKey && (matchingFromEl = fromNodesLookup[curToNodeKey]) && compareNodeNames(matchingFromEl, curToNodeChild)) {
              // MORPH
              if(!skipFrom){ addChild(fromEl, matchingFromEl); }
              morphEl(matchingFromEl, curToNodeChild);
            } else {
              var onBeforeNodeAddedResult = onBeforeNodeAdded(curToNodeChild);
              if (onBeforeNodeAddedResult !== false) {
                if (onBeforeNodeAddedResult) {
                  curToNodeChild = onBeforeNodeAddedResult;
                }

                if (curToNodeChild.actualize) {
                  curToNodeChild = curToNodeChild.actualize(fromEl.ownerDocument || doc);
                }
                addChild(fromEl, curToNodeChild);
                handleNodeAdded(curToNodeChild);
              }
            }

            curToNodeChild = toNextSibling;
            curFromNodeChild = fromNextSibling;
          }

          cleanupFromEl(fromEl, curFromNodeChild, curFromNodeKey);

          var specialElHandler = specialElHandlers[fromEl.nodeName];
          if (specialElHandler) {
            specialElHandler(fromEl, toEl);
          }
        } // END: morphChildren(...)

        var morphedNode = fromNode;
        var morphedNodeType = morphedNode.nodeType;
        var toNodeType = toNode.nodeType;

        if (!childrenOnly) {
          // Handle the case where we are given two DOM nodes that are not
          // compatible (e.g. <div> --> <span> or <div> --> TEXT)
          if (morphedNodeType === ELEMENT_NODE) {
            if (toNodeType === ELEMENT_NODE) {
              if (!compareNodeNames(fromNode, toNode)) {
                onNodeDiscarded(fromNode);
                morphedNode = moveChildren(fromNode, createElementNS(toNode.nodeName, toNode.namespaceURI));
              }
            } else {
              // Going from an element node to a text node
              morphedNode = toNode;
            }
          } else if (morphedNodeType === TEXT_NODE || morphedNodeType === COMMENT_NODE) { // Text or comment node
            if (toNodeType === morphedNodeType) {
              if (morphedNode.nodeValue !== toNode.nodeValue) {
                morphedNode.nodeValue = toNode.nodeValue;
              }

              return morphedNode;
            } else {
              // Text node to something else
              morphedNode = toNode;
            }
          }
        }

        if (morphedNode === toNode) {
          // The "to node" was not compatible with the "from node" so we had to
          // toss out the "from node" and use the "to node"
          onNodeDiscarded(fromNode);
        } else {
          if (toNode.isSameNode && toNode.isSameNode(morphedNode)) {
            return;
          }

          morphEl(morphedNode, toNode, childrenOnly);

          // We now need to loop over any keyed nodes that might need to be
          // removed. We only do the removal if we know that the keyed node
          // never found a match. When a keyed node is matched up we remove
          // it out of fromNodesLookup and we use fromNodesLookup to determine
          // if a keyed node has been matched up or not
          if (keyedRemovalList) {
            for (var i=0, len=keyedRemovalList.length; i<len; i++) {
              var elToRemove = fromNodesLookup[keyedRemovalList[i]];
              if (elToRemove) {
                removeNode(elToRemove, elToRemove.parentNode, false);
              }
            }
          }
        }

        if (!childrenOnly && morphedNode !== fromNode && fromNode.parentNode) {
          if (morphedNode.actualize) {
            morphedNode = morphedNode.actualize(fromNode.ownerDocument || doc);
          }
          // If we had to swap out the from node with a new node because the old
          // node was not compatible with the target node then we need to
          // replace the old DOM node in the original DOM tree. This is only
          // possible if the original DOM node was part of a DOM tree which
          // we know is the case if it has a parent node.
          fromNode.parentNode.replaceChild(morphedNode, fromNode);
        }

        return morphedNode;
      };
    }

    const E_CANCELED = new Error('request for lock canceled');

    var __awaiter$2 = function (thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    };
    class Semaphore {
        constructor(_value, _cancelError = E_CANCELED) {
            this._value = _value;
            this._cancelError = _cancelError;
            this._weightedQueues = [];
            this._weightedWaiters = [];
        }
        acquire(weight = 1) {
            if (weight <= 0)
                throw new Error(`invalid weight ${weight}: must be positive`);
            return new Promise((resolve, reject) => {
                if (!this._weightedQueues[weight - 1])
                    this._weightedQueues[weight - 1] = [];
                this._weightedQueues[weight - 1].push({ resolve, reject });
                this._dispatch();
            });
        }
        runExclusive(callback, weight = 1) {
            return __awaiter$2(this, void 0, void 0, function* () {
                const [value, release] = yield this.acquire(weight);
                try {
                    return yield callback(value);
                }
                finally {
                    release();
                }
            });
        }
        waitForUnlock(weight = 1) {
            if (weight <= 0)
                throw new Error(`invalid weight ${weight}: must be positive`);
            return new Promise((resolve) => {
                if (!this._weightedWaiters[weight - 1])
                    this._weightedWaiters[weight - 1] = [];
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
            if (weight <= 0)
                throw new Error(`invalid weight ${weight}: must be positive`);
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
                if (!queueEntry)
                    continue;
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
                if (called)
                    return;
                called = true;
                this.release(weight);
            };
        }
        _drainUnlockWaiters() {
            for (let weight = this._value; weight > 0; weight--) {
                if (!this._weightedWaiters[weight - 1])
                    continue;
                this._weightedWaiters[weight - 1].forEach((waiter) => waiter());
                this._weightedWaiters[weight - 1] = [];
            }
        }
    }

    var __awaiter$1 = function (thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    };
    class Mutex {
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
            if (this._semaphore.isLocked())
                this._semaphore.release();
        }
        cancel() {
            return this._semaphore.cancel();
        }
    }

    const morphdom = morphdomFactory(morphAttrs);
    const postbackMutex = new Mutex();
    function syncBooleanAttrProp(fromEl, toEl, name) {
        if (fromEl[name] !== toEl[name]) {
            fromEl[name] = toEl[name];
            if (fromEl[name]) {
                fromEl.setAttribute(name, '');
            }
            else {
                fromEl.removeAttribute(name);
            }
        }
    }
    async function submitForm(form, eventTarget, eventArgument) {
        const release = await postbackMutex.acquire();
        try {
            const pageState = document.getElementById("pagestate");
            const url = location.pathname + location.search;
            const formData = form ? new FormData(form) : new FormData();
            if (pageState) {
                // Add all the form elements that are not in a form
                const elements = document.body.querySelectorAll('input, select, textarea');
                for (let i = 0; i < elements.length; i++) {
                    const element = elements[i];
                    if (element.hasAttribute('data-wfc-ignore') || element.type === "button" ||
                        element.type === "submit" || element.type === "reset") {
                        continue;
                    }
                    const form = element.closest('form[data-wfc-form]');
                    if (form) {
                        continue;
                    }
                    if (element.type === "checkbox" || element.type === "radio") {
                        if (element.checked) {
                            formData.append(element.name, element.value);
                        }
                    }
                    else {
                        formData.append(element.name, element.value);
                    }
                }
                formData.append("__PAGESTATE", pageState.value);
            }
            if (eventTarget) {
                formData.append("__EVENTTARGET", eventTarget);
            }
            if (eventArgument) {
                formData.append("__EVENTARGUMENT", eventArgument);
            }
            document.dispatchEvent(new CustomEvent("wfc:beforeSubmit", { detail: { form, eventTarget, formData } }));
            const request = {
                method: "POST",
            };
            request.body = formData;
            const response = await fetch(url, request);
            if (!response.ok) {
                document.dispatchEvent(new CustomEvent("wfc:submitError", {
                    detail: {
                        form,
                        eventTarget,
                        response: response
                    }
                }));
                throw new Error(response.statusText);
            }
            const text = await response.text();
            const newElements = [];
            const options = {
                onNodeAdded(node) {
                    newElements.push(node);
                    document.dispatchEvent(new CustomEvent("wfc:addNode", { detail: { node, form, eventTarget } }));
                },
                onBeforeElUpdated: function (fromEl, toEl) {
                    if (fromEl.tagName === "INPUT" && fromEl.type !== "hidden") {
                        morphAttrs(fromEl, toEl);
                        syncBooleanAttrProp(fromEl, toEl, 'checked');
                        syncBooleanAttrProp(fromEl, toEl, 'disabled');
                        // Only update the value if the value attribute is present
                        if (toEl.hasAttribute('value')) {
                            fromEl.value = toEl.value;
                        }
                        return false;
                    }
                },
                onBeforeNodeDiscarded(node) {
                    var _a, _b;
                    if (node.tagName === "SCRIPT") {
                        return false;
                    }
                    if (node.tagName === 'FORM' && node.hasAttribute('data-wfc-form')) {
                        return false;
                    }
                    if (node.tagName === 'DIV' && node.hasAttribute('data-wfc-owner') && ((_a = node.getAttribute('data-wfc-owner')) !== null && _a !== void 0 ? _a : "") !== ((_b = form === null || form === void 0 ? void 0 : form.id) !== null && _b !== void 0 ? _b : "")) {
                        return false;
                    }
                    const result = document.dispatchEvent(new CustomEvent("wfc:discardNode", {
                        detail: {
                            node,
                            form,
                            eventTarget
                        }, cancelable: true
                    }));
                    if (!result) {
                        return false;
                    }
                }
            };
            const parser = new DOMParser();
            const htmlDoc = parser.parseFromString(text, 'text/html');
            morphdom(document.head, htmlDoc.querySelector('head'), options);
            morphdom(document.body, htmlDoc.querySelector('body'), options);
            document.dispatchEvent(new CustomEvent("wfc:afterSubmit", { detail: { form, eventTarget, newElements } }));
        }
        finally {
            release();
        }
    }
    const originalSubmit = HTMLFormElement.prototype.submit;
    HTMLFormElement.prototype.submit = async function () {
        if (this.hasAttribute('data-wfc-form')) {
            await submitForm(this);
        }
        else {
            originalSubmit.call(this);
        }
    };
    document.addEventListener('submit', async function (e) {
        if (e.target instanceof Element && e.target.hasAttribute('data-wfc-form')) {
            e.preventDefault();
            await submitForm(e.target);
        }
    });
    document.addEventListener('click', async function (e) {
        var _a, _b;
        if (!(e.target instanceof Element)) {
            return;
        }
        const eventTarget = (_b = (_a = e.target) === null || _a === void 0 ? void 0 : _a.closest("[data-wfc-postback]")) === null || _b === void 0 ? void 0 : _b.getAttribute('data-wfc-postback');
        if (!eventTarget) {
            return;
        }
        const form = e.target.closest('form[data-wfc-form]');
        e.preventDefault();
        await submitForm(form, eventTarget);
    });
    document.addEventListener('keypress', async function (e) {
        if (e.key !== 'Enter' && e.keyCode !== 13 && e.which !== 13) {
            return;
        }
        if (!(e.target instanceof Element) || e.target.tagName !== "INPUT") {
            return;
        }
        const type = e.target.getAttribute('type');
        if (type === "button" || type === "submit" || type === "reset") {
            return;
        }
        const form = e.target.closest('form[data-wfc-form]');
        const eventTarget = e.target.getAttribute('name');
        e.preventDefault();
        await submitForm(form, eventTarget, 'ENTER');
    });
    const timeouts = {};
    document.addEventListener('input', function (e) {
        var _a;
        if (!(e.target instanceof Element) || e.target.tagName !== "INPUT" || !e.target.hasAttribute('data-wfc-autopostback')) {
            return;
        }
        const type = e.target.getAttribute('type');
        if (type === "button" || type === "submit" || type === "reset") {
            return;
        }
        const form = e.target.closest('form[data-wfc-form]');
        const eventTarget = e.target.getAttribute('name');
        const key = ((_a = form === null || form === void 0 ? void 0 : form.id) !== null && _a !== void 0 ? _a : '') + eventTarget;
        if (timeouts[key]) {
            clearTimeout(timeouts[key]);
        }
        timeouts[key] = setTimeout(async () => {
            delete timeouts[key];
            await submitForm(form, eventTarget, 'CHANGE');
        }, 1000);
    });
    document.addEventListener('change', async function (e) {
        var _a;
        if (e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
            const eventTarget = e.target.getAttribute('name');
            const form = e.target.closest('form[data-wfc-form]');
            const key = ((_a = form === null || form === void 0 ? void 0 : form.id) !== null && _a !== void 0 ? _a : '') + eventTarget;
            if (timeouts[key]) {
                clearTimeout(timeouts[key]);
            }
            setTimeout(() => submitForm(form, eventTarget, 'CHANGE'), 10);
        }
    });

})();
