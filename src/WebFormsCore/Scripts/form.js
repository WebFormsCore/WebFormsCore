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

    /*! @license DOMPurify 3.1.6 | (c) Cure53 and other contributors | Released under the Apache license 2.0 and Mozilla Public License 2.0 | github.com/cure53/DOMPurify/blob/3.1.6/LICENSE */

    const {
      entries,
      setPrototypeOf,
      isFrozen,
      getPrototypeOf,
      getOwnPropertyDescriptor
    } = Object;
    let {
      freeze,
      seal,
      create
    } = Object; // eslint-disable-line import/no-mutable-exports
    let {
      apply,
      construct
    } = typeof Reflect !== 'undefined' && Reflect;
    if (!freeze) {
      freeze = function freeze(x) {
        return x;
      };
    }
    if (!seal) {
      seal = function seal(x) {
        return x;
      };
    }
    if (!apply) {
      apply = function apply(fun, thisValue, args) {
        return fun.apply(thisValue, args);
      };
    }
    if (!construct) {
      construct = function construct(Func, args) {
        return new Func(...args);
      };
    }
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
      return function (thisArg) {
        for (var _len = arguments.length, args = new Array(_len > 1 ? _len - 1 : 0), _key = 1; _key < _len; _key++) {
          args[_key - 1] = arguments[_key];
        }
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
      return function () {
        for (var _len2 = arguments.length, args = new Array(_len2), _key2 = 0; _key2 < _len2; _key2++) {
          args[_key2] = arguments[_key2];
        }
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
      let transformCaseFunc = arguments.length > 2 && arguments[2] !== undefined ? arguments[2] : stringToLowerCase;
      if (setPrototypeOf) {
        // Make 'in' and truthy checks like Boolean(set.constructor)
        // independent of any properties defined on Object.prototype.
        // Prevent prototype setters from intercepting set as a this value.
        setPrototypeOf(set, null);
      }
      let l = array.length;
      while (l--) {
        let element = array[l];
        if (typeof element === 'string') {
          const lcElement = transformCaseFunc(element);
          if (lcElement !== element) {
            // Config presets (e.g. tags.js, attrs.js) are immutable.
            if (!isFrozen(array)) {
              array[l] = lcElement;
            }
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
      for (let index = 0; index < array.length; index++) {
        const isPropertyExist = objectHasOwnProperty(array, index);
        if (!isPropertyExist) {
          array[index] = null;
        }
      }
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
      for (const [property, value] of entries(object)) {
        const isPropertyExist = objectHasOwnProperty(object, property);
        if (isPropertyExist) {
          if (Array.isArray(value)) {
            newObject[property] = cleanArray(value);
          } else if (value && typeof value === 'object' && value.constructor === Object) {
            newObject[property] = clone(value);
          } else {
            newObject[property] = value;
          }
        }
      }
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
          if (desc.get) {
            return unapply(desc.get);
          }
          if (typeof desc.value === 'function') {
            return unapply(desc.value);
          }
        }
        object = getPrototypeOf(object);
      }
      function fallbackValue() {
        return null;
      }
      return fallbackValue;
    }

    const html$1 = freeze(['a', 'abbr', 'acronym', 'address', 'area', 'article', 'aside', 'audio', 'b', 'bdi', 'bdo', 'big', 'blink', 'blockquote', 'body', 'br', 'button', 'canvas', 'caption', 'center', 'cite', 'code', 'col', 'colgroup', 'content', 'data', 'datalist', 'dd', 'decorator', 'del', 'details', 'dfn', 'dialog', 'dir', 'div', 'dl', 'dt', 'element', 'em', 'fieldset', 'figcaption', 'figure', 'font', 'footer', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'head', 'header', 'hgroup', 'hr', 'html', 'i', 'img', 'input', 'ins', 'kbd', 'label', 'legend', 'li', 'main', 'map', 'mark', 'marquee', 'menu', 'menuitem', 'meter', 'nav', 'nobr', 'ol', 'optgroup', 'option', 'output', 'p', 'picture', 'pre', 'progress', 'q', 'rp', 'rt', 'ruby', 's', 'samp', 'section', 'select', 'shadow', 'small', 'source', 'spacer', 'span', 'strike', 'strong', 'style', 'sub', 'summary', 'sup', 'table', 'tbody', 'td', 'template', 'textarea', 'tfoot', 'th', 'thead', 'time', 'tr', 'track', 'tt', 'u', 'ul', 'var', 'video', 'wbr']);

    // SVG
    const svg$1 = freeze(['svg', 'a', 'altglyph', 'altglyphdef', 'altglyphitem', 'animatecolor', 'animatemotion', 'animatetransform', 'circle', 'clippath', 'defs', 'desc', 'ellipse', 'filter', 'font', 'g', 'glyph', 'glyphref', 'hkern', 'image', 'line', 'lineargradient', 'marker', 'mask', 'metadata', 'mpath', 'path', 'pattern', 'polygon', 'polyline', 'radialgradient', 'rect', 'stop', 'style', 'switch', 'symbol', 'text', 'textpath', 'title', 'tref', 'tspan', 'view', 'vkern']);
    const svgFilters = freeze(['feBlend', 'feColorMatrix', 'feComponentTransfer', 'feComposite', 'feConvolveMatrix', 'feDiffuseLighting', 'feDisplacementMap', 'feDistantLight', 'feDropShadow', 'feFlood', 'feFuncA', 'feFuncB', 'feFuncG', 'feFuncR', 'feGaussianBlur', 'feImage', 'feMerge', 'feMergeNode', 'feMorphology', 'feOffset', 'fePointLight', 'feSpecularLighting', 'feSpotLight', 'feTile', 'feTurbulence']);

    // List of SVG elements that are disallowed by default.
    // We still need to know them so that we can do namespace
    // checks properly in case one wants to add them to
    // allow-list.
    const svgDisallowed = freeze(['animate', 'color-profile', 'cursor', 'discard', 'font-face', 'font-face-format', 'font-face-name', 'font-face-src', 'font-face-uri', 'foreignobject', 'hatch', 'hatchpath', 'mesh', 'meshgradient', 'meshpatch', 'meshrow', 'missing-glyph', 'script', 'set', 'solidcolor', 'unknown', 'use']);
    const mathMl$1 = freeze(['math', 'menclose', 'merror', 'mfenced', 'mfrac', 'mglyph', 'mi', 'mlabeledtr', 'mmultiscripts', 'mn', 'mo', 'mover', 'mpadded', 'mphantom', 'mroot', 'mrow', 'ms', 'mspace', 'msqrt', 'mstyle', 'msub', 'msup', 'msubsup', 'mtable', 'mtd', 'mtext', 'mtr', 'munder', 'munderover', 'mprescripts']);

    // Similarly to SVG, we want to know all MathML elements,
    // even those that we disallow by default.
    const mathMlDisallowed = freeze(['maction', 'maligngroup', 'malignmark', 'mlongdiv', 'mscarries', 'mscarry', 'msgroup', 'mstack', 'msline', 'msrow', 'semantics', 'annotation', 'annotation-xml', 'mprescripts', 'none']);
    const text = freeze(['#text']);

    const html = freeze(['accept', 'action', 'align', 'alt', 'autocapitalize', 'autocomplete', 'autopictureinpicture', 'autoplay', 'background', 'bgcolor', 'border', 'capture', 'cellpadding', 'cellspacing', 'checked', 'cite', 'class', 'clear', 'color', 'cols', 'colspan', 'controls', 'controlslist', 'coords', 'crossorigin', 'datetime', 'decoding', 'default', 'dir', 'disabled', 'disablepictureinpicture', 'disableremoteplayback', 'download', 'draggable', 'enctype', 'enterkeyhint', 'face', 'for', 'headers', 'height', 'hidden', 'high', 'href', 'hreflang', 'id', 'inputmode', 'integrity', 'ismap', 'kind', 'label', 'lang', 'list', 'loading', 'loop', 'low', 'max', 'maxlength', 'media', 'method', 'min', 'minlength', 'multiple', 'muted', 'name', 'nonce', 'noshade', 'novalidate', 'nowrap', 'open', 'optimum', 'pattern', 'placeholder', 'playsinline', 'popover', 'popovertarget', 'popovertargetaction', 'poster', 'preload', 'pubdate', 'radiogroup', 'readonly', 'rel', 'required', 'rev', 'reversed', 'role', 'rows', 'rowspan', 'spellcheck', 'scope', 'selected', 'shape', 'size', 'sizes', 'span', 'srclang', 'start', 'src', 'srcset', 'step', 'style', 'summary', 'tabindex', 'title', 'translate', 'type', 'usemap', 'valign', 'value', 'width', 'wrap', 'xmlns', 'slot']);
    const svg = freeze(['accent-height', 'accumulate', 'additive', 'alignment-baseline', 'ascent', 'attributename', 'attributetype', 'azimuth', 'basefrequency', 'baseline-shift', 'begin', 'bias', 'by', 'class', 'clip', 'clippathunits', 'clip-path', 'clip-rule', 'color', 'color-interpolation', 'color-interpolation-filters', 'color-profile', 'color-rendering', 'cx', 'cy', 'd', 'dx', 'dy', 'diffuseconstant', 'direction', 'display', 'divisor', 'dur', 'edgemode', 'elevation', 'end', 'fill', 'fill-opacity', 'fill-rule', 'filter', 'filterunits', 'flood-color', 'flood-opacity', 'font-family', 'font-size', 'font-size-adjust', 'font-stretch', 'font-style', 'font-variant', 'font-weight', 'fx', 'fy', 'g1', 'g2', 'glyph-name', 'glyphref', 'gradientunits', 'gradienttransform', 'height', 'href', 'id', 'image-rendering', 'in', 'in2', 'k', 'k1', 'k2', 'k3', 'k4', 'kerning', 'keypoints', 'keysplines', 'keytimes', 'lang', 'lengthadjust', 'letter-spacing', 'kernelmatrix', 'kernelunitlength', 'lighting-color', 'local', 'marker-end', 'marker-mid', 'marker-start', 'markerheight', 'markerunits', 'markerwidth', 'maskcontentunits', 'maskunits', 'max', 'mask', 'media', 'method', 'mode', 'min', 'name', 'numoctaves', 'offset', 'operator', 'opacity', 'order', 'orient', 'orientation', 'origin', 'overflow', 'paint-order', 'path', 'pathlength', 'patterncontentunits', 'patterntransform', 'patternunits', 'points', 'preservealpha', 'preserveaspectratio', 'primitiveunits', 'r', 'rx', 'ry', 'radius', 'refx', 'refy', 'repeatcount', 'repeatdur', 'restart', 'result', 'rotate', 'scale', 'seed', 'shape-rendering', 'specularconstant', 'specularexponent', 'spreadmethod', 'startoffset', 'stddeviation', 'stitchtiles', 'stop-color', 'stop-opacity', 'stroke-dasharray', 'stroke-dashoffset', 'stroke-linecap', 'stroke-linejoin', 'stroke-miterlimit', 'stroke-opacity', 'stroke', 'stroke-width', 'style', 'surfacescale', 'systemlanguage', 'tabindex', 'targetx', 'targety', 'transform', 'transform-origin', 'text-anchor', 'text-decoration', 'text-rendering', 'textlength', 'type', 'u1', 'u2', 'unicode', 'values', 'viewbox', 'visibility', 'version', 'vert-adv-y', 'vert-origin-x', 'vert-origin-y', 'width', 'word-spacing', 'wrap', 'writing-mode', 'xchannelselector', 'ychannelselector', 'x', 'x1', 'x2', 'xmlns', 'y', 'y1', 'y2', 'z', 'zoomandpan']);
    const mathMl = freeze(['accent', 'accentunder', 'align', 'bevelled', 'close', 'columnsalign', 'columnlines', 'columnspan', 'denomalign', 'depth', 'dir', 'display', 'displaystyle', 'encoding', 'fence', 'frame', 'height', 'href', 'id', 'largeop', 'length', 'linethickness', 'lspace', 'lquote', 'mathbackground', 'mathcolor', 'mathsize', 'mathvariant', 'maxsize', 'minsize', 'movablelimits', 'notation', 'numalign', 'open', 'rowalign', 'rowlines', 'rowspacing', 'rowspan', 'rspace', 'rquote', 'scriptlevel', 'scriptminsize', 'scriptsizemultiplier', 'selection', 'separator', 'separators', 'stretchy', 'subscriptshift', 'supscriptshift', 'symmetric', 'voffset', 'width', 'xmlns']);
    const xml = freeze(['xlink:href', 'xml:id', 'xlink:title', 'xml:space', 'xmlns:xlink']);

    // eslint-disable-next-line unicorn/better-regex
    const MUSTACHE_EXPR = seal(/\{\{[\w\W]*|[\w\W]*\}\}/gm); // Specify template detection regex for SAFE_FOR_TEMPLATES mode
    const ERB_EXPR = seal(/<%[\w\W]*|[\w\W]*%>/gm);
    const TMPLIT_EXPR = seal(/\${[\w\W]*}/gm);
    const DATA_ATTR = seal(/^data-[\-\w.\u00B7-\uFFFF]/); // eslint-disable-line no-useless-escape
    const ARIA_ATTR = seal(/^aria-[\-\w]+$/); // eslint-disable-line no-useless-escape
    const IS_ALLOWED_URI = seal(/^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z+.\-]+(?:[^a-z+.\-:]|$))/i // eslint-disable-line no-useless-escape
    );
    const IS_SCRIPT_OR_DATA = seal(/^(?:\w+script|data):/i);
    const ATTR_WHITESPACE = seal(/[\u0000-\u0020\u00A0\u1680\u180E\u2000-\u2029\u205F\u3000]/g // eslint-disable-line no-control-regex
    );
    const DOCTYPE_NAME = seal(/^html$/i);
    const CUSTOM_ELEMENT = seal(/^[a-z][.\w]*(-[.\w]+)+$/i);

    var EXPRESSIONS = /*#__PURE__*/Object.freeze({
      __proto__: null,
      MUSTACHE_EXPR: MUSTACHE_EXPR,
      ERB_EXPR: ERB_EXPR,
      TMPLIT_EXPR: TMPLIT_EXPR,
      DATA_ATTR: DATA_ATTR,
      ARIA_ATTR: ARIA_ATTR,
      IS_ALLOWED_URI: IS_ALLOWED_URI,
      IS_SCRIPT_OR_DATA: IS_SCRIPT_OR_DATA,
      ATTR_WHITESPACE: ATTR_WHITESPACE,
      DOCTYPE_NAME: DOCTYPE_NAME,
      CUSTOM_ELEMENT: CUSTOM_ELEMENT
    });

    // https://developer.mozilla.org/en-US/docs/Web/API/Node/nodeType
    const NODE_TYPE = {
      element: 1,
      attribute: 2,
      text: 3,
      cdataSection: 4,
      entityReference: 5,
      // Deprecated
      entityNode: 6,
      // Deprecated
      progressingInstruction: 7,
      comment: 8,
      document: 9,
      documentType: 10,
      documentFragment: 11,
      notation: 12 // Deprecated
    };
    const getGlobal = function getGlobal() {
      return typeof window === 'undefined' ? null : window;
    };

    /**
     * Creates a no-op policy for internal use only.
     * Don't export this function outside this module!
     * @param {TrustedTypePolicyFactory} trustedTypes The policy factory.
     * @param {HTMLScriptElement} purifyHostElement The Script element used to load DOMPurify (to determine policy name suffix).
     * @return {TrustedTypePolicy} The policy created (or null, if Trusted Types
     * are not supported or creating the policy failed).
     */
    const _createTrustedTypesPolicy = function _createTrustedTypesPolicy(trustedTypes, purifyHostElement) {
      if (typeof trustedTypes !== 'object' || typeof trustedTypes.createPolicy !== 'function') {
        return null;
      }

      // Allow the callers to control the unique policy name
      // by adding a data-tt-policy-suffix to the script element with the DOMPurify.
      // Policy creation with duplicate names throws in Trusted Types.
      let suffix = null;
      const ATTR_NAME = 'data-tt-policy-suffix';
      if (purifyHostElement && purifyHostElement.hasAttribute(ATTR_NAME)) {
        suffix = purifyHostElement.getAttribute(ATTR_NAME);
      }
      const policyName = 'dompurify' + (suffix ? '#' + suffix : '');
      try {
        return trustedTypes.createPolicy(policyName, {
          createHTML(html) {
            return html;
          },
          createScriptURL(scriptUrl) {
            return scriptUrl;
          }
        });
      } catch (_) {
        // Policy creation failed (most likely another DOMPurify script has
        // already run). Skip creating the policy, as this will only cause errors
        // if TT are enforced.
        console.warn('TrustedTypes policy ' + policyName + ' could not be created.');
        return null;
      }
    };
    function createDOMPurify() {
      let window = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : getGlobal();
      const DOMPurify = root => createDOMPurify(root);

      /**
       * Version label, exposed for easier checks
       * if DOMPurify is up to date or not
       */
      DOMPurify.version = '3.1.6';

      /**
       * Array of elements that DOMPurify removed during sanitation.
       * Empty if nothing was removed.
       */
      DOMPurify.removed = [];
      if (!window || !window.document || window.document.nodeType !== NODE_TYPE.document) {
        // Not running in a browser, provide a factory function
        // so that you can pass your own Window
        DOMPurify.isSupported = false;
        return DOMPurify;
      }
      let {
        document
      } = window;
      const originalDocument = document;
      const currentScript = originalDocument.currentScript;
      const {
        DocumentFragment,
        HTMLTemplateElement,
        Node,
        Element,
        NodeFilter,
        NamedNodeMap = window.NamedNodeMap || window.MozNamedAttrMap,
        HTMLFormElement,
        DOMParser,
        trustedTypes
      } = window;
      const ElementPrototype = Element.prototype;
      const cloneNode = lookupGetter(ElementPrototype, 'cloneNode');
      const remove = lookupGetter(ElementPrototype, 'remove');
      const getNextSibling = lookupGetter(ElementPrototype, 'nextSibling');
      const getChildNodes = lookupGetter(ElementPrototype, 'childNodes');
      const getParentNode = lookupGetter(ElementPrototype, 'parentNode');

      // As per issue #47, the web-components registry is inherited by a
      // new document created via createHTMLDocument. As per the spec
      // (http://w3c.github.io/webcomponents/spec/custom/#creating-and-passing-registries)
      // a new empty registry is used when creating a template contents owner
      // document, so we use that as our parent document to ensure nothing
      // is inherited.
      if (typeof HTMLTemplateElement === 'function') {
        const template = document.createElement('template');
        if (template.content && template.content.ownerDocument) {
          document = template.content.ownerDocument;
        }
      }
      let trustedTypesPolicy;
      let emptyHTML = '';
      const {
        implementation,
        createNodeIterator,
        createDocumentFragment,
        getElementsByTagName
      } = document;
      const {
        importNode
      } = originalDocument;
      let hooks = {};

      /**
       * Expose whether this browser supports running the full DOMPurify.
       */
      DOMPurify.isSupported = typeof entries === 'function' && typeof getParentNode === 'function' && implementation && implementation.createHTMLDocument !== undefined;
      const {
        MUSTACHE_EXPR,
        ERB_EXPR,
        TMPLIT_EXPR,
        DATA_ATTR,
        ARIA_ATTR,
        IS_SCRIPT_OR_DATA,
        ATTR_WHITESPACE,
        CUSTOM_ELEMENT
      } = EXPRESSIONS;
      let {
        IS_ALLOWED_URI: IS_ALLOWED_URI$1
      } = EXPRESSIONS;

      /**
       * We consider the elements and attributes below to be safe. Ideally
       * don't add any new ones but feel free to remove unwanted ones.
       */

      /* allowed element names */
      let ALLOWED_TAGS = null;
      const DEFAULT_ALLOWED_TAGS = addToSet({}, [...html$1, ...svg$1, ...svgFilters, ...mathMl$1, ...text]);

      /* Allowed attribute names */
      let ALLOWED_ATTR = null;
      const DEFAULT_ALLOWED_ATTR = addToSet({}, [...html, ...svg, ...mathMl, ...xml]);

      /*
       * Configure how DOMPUrify should handle custom elements and their attributes as well as customized built-in elements.
       * @property {RegExp|Function|null} tagNameCheck one of [null, regexPattern, predicate]. Default: `null` (disallow any custom elements)
       * @property {RegExp|Function|null} attributeNameCheck one of [null, regexPattern, predicate]. Default: `null` (disallow any attributes not on the allow list)
       * @property {boolean} allowCustomizedBuiltInElements allow custom elements derived from built-ins if they pass CUSTOM_ELEMENT_HANDLING.tagNameCheck. Default: `false`.
       */
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

      /* Explicitly forbidden tags (overrides ALLOWED_TAGS/ADD_TAGS) */
      let FORBID_TAGS = null;

      /* Explicitly forbidden attributes (overrides ALLOWED_ATTR/ADD_ATTR) */
      let FORBID_ATTR = null;

      /* Decide if ARIA attributes are okay */
      let ALLOW_ARIA_ATTR = true;

      /* Decide if custom data attributes are okay */
      let ALLOW_DATA_ATTR = true;

      /* Decide if unknown protocols are okay */
      let ALLOW_UNKNOWN_PROTOCOLS = false;

      /* Decide if self-closing tags in attributes are allowed.
       * Usually removed due to a mXSS issue in jQuery 3.0 */
      let ALLOW_SELF_CLOSE_IN_ATTR = true;

      /* Output should be safe for common template engines.
       * This means, DOMPurify removes data attributes, mustaches and ERB
       */
      let SAFE_FOR_TEMPLATES = false;

      /* Output should be safe even for XML used within HTML and alike.
       * This means, DOMPurify removes comments when containing risky content.
       */
      let SAFE_FOR_XML = true;

      /* Decide if document with <html>... should be returned */
      let WHOLE_DOCUMENT = false;

      /* Track whether config is already set on this instance of DOMPurify. */
      let SET_CONFIG = false;

      /* Decide if all elements (e.g. style, script) must be children of
       * document.body. By default, browsers might move them to document.head */
      let FORCE_BODY = false;

      /* Decide if a DOM `HTMLBodyElement` should be returned, instead of a html
       * string (or a TrustedHTML object if Trusted Types are supported).
       * If `WHOLE_DOCUMENT` is enabled a `HTMLHtmlElement` will be returned instead
       */
      let RETURN_DOM = false;

      /* Decide if a DOM `DocumentFragment` should be returned, instead of a html
       * string  (or a TrustedHTML object if Trusted Types are supported) */
      let RETURN_DOM_FRAGMENT = false;

      /* Try to return a Trusted Type object instead of a string, return a string in
       * case Trusted Types are not supported  */
      let RETURN_TRUSTED_TYPE = false;

      /* Output should be free from DOM clobbering attacks?
       * This sanitizes markups named with colliding, clobberable built-in DOM APIs.
       */
      let SANITIZE_DOM = true;

      /* Achieve full DOM Clobbering protection by isolating the namespace of named
       * properties and JS variables, mitigating attacks that abuse the HTML/DOM spec rules.
       *
       * HTML/DOM spec rules that enable DOM Clobbering:
       *   - Named Access on Window (§7.3.3)
       *   - DOM Tree Accessors (§3.1.5)
       *   - Form Element Parent-Child Relations (§4.10.3)
       *   - Iframe srcdoc / Nested WindowProxies (§4.8.5)
       *   - HTMLCollection (§4.2.10.2)
       *
       * Namespace isolation is implemented by prefixing `id` and `name` attributes
       * with a constant string, i.e., `user-content-`
       */
      let SANITIZE_NAMED_PROPS = false;
      const SANITIZE_NAMED_PROPS_PREFIX = 'user-content-';

      /* Keep element content when removing element? */
      let KEEP_CONTENT = true;

      /* If a `Node` is passed to sanitize(), then performs sanitization in-place instead
       * of importing it into a new Document and returning a sanitized copy */
      let IN_PLACE = false;

      /* Allow usage of profiles like html, svg and mathMl */
      let USE_PROFILES = {};

      /* Tags to ignore content of when KEEP_CONTENT is true */
      let FORBID_CONTENTS = null;
      const DEFAULT_FORBID_CONTENTS = addToSet({}, ['annotation-xml', 'audio', 'colgroup', 'desc', 'foreignobject', 'head', 'iframe', 'math', 'mi', 'mn', 'mo', 'ms', 'mtext', 'noembed', 'noframes', 'noscript', 'plaintext', 'script', 'style', 'svg', 'template', 'thead', 'title', 'video', 'xmp']);

      /* Tags that are safe for data: URIs */
      let DATA_URI_TAGS = null;
      const DEFAULT_DATA_URI_TAGS = addToSet({}, ['audio', 'video', 'img', 'source', 'image', 'track']);

      /* Attributes safe for values like "javascript:" */
      let URI_SAFE_ATTRIBUTES = null;
      const DEFAULT_URI_SAFE_ATTRIBUTES = addToSet({}, ['alt', 'class', 'for', 'id', 'label', 'name', 'pattern', 'placeholder', 'role', 'summary', 'title', 'value', 'style', 'xmlns']);
      const MATHML_NAMESPACE = 'http://www.w3.org/1998/Math/MathML';
      const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';
      const HTML_NAMESPACE = 'http://www.w3.org/1999/xhtml';
      /* Document namespace */
      let NAMESPACE = HTML_NAMESPACE;
      let IS_EMPTY_INPUT = false;

      /* Allowed XHTML+XML namespaces */
      let ALLOWED_NAMESPACES = null;
      const DEFAULT_ALLOWED_NAMESPACES = addToSet({}, [MATHML_NAMESPACE, SVG_NAMESPACE, HTML_NAMESPACE], stringToString);

      /* Parsing of strict XHTML documents */
      let PARSER_MEDIA_TYPE = null;
      const SUPPORTED_PARSER_MEDIA_TYPES = ['application/xhtml+xml', 'text/html'];
      const DEFAULT_PARSER_MEDIA_TYPE = 'text/html';
      let transformCaseFunc = null;

      /* Keep a reference to config to pass to hooks */
      let CONFIG = null;

      /* Ideally, do not touch anything below this line */
      /* ______________________________________________ */

      const formElement = document.createElement('form');
      const isRegexOrFunction = function isRegexOrFunction(testValue) {
        return testValue instanceof RegExp || testValue instanceof Function;
      };

      /**
       * _parseConfig
       *
       * @param  {Object} cfg optional config literal
       */
      // eslint-disable-next-line complexity
      const _parseConfig = function _parseConfig() {
        let cfg = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
        if (CONFIG && CONFIG === cfg) {
          return;
        }

        /* Shield configuration object from tampering */
        if (!cfg || typeof cfg !== 'object') {
          cfg = {};
        }

        /* Shield configuration object from prototype pollution */
        cfg = clone(cfg);
        PARSER_MEDIA_TYPE =
        // eslint-disable-next-line unicorn/prefer-includes
        SUPPORTED_PARSER_MEDIA_TYPES.indexOf(cfg.PARSER_MEDIA_TYPE) === -1 ? DEFAULT_PARSER_MEDIA_TYPE : cfg.PARSER_MEDIA_TYPE;

        // HTML tags and attributes are not case-sensitive, converting to lowercase. Keeping XHTML as is.
        transformCaseFunc = PARSER_MEDIA_TYPE === 'application/xhtml+xml' ? stringToString : stringToLowerCase;

        /* Set configuration parameters */
        ALLOWED_TAGS = objectHasOwnProperty(cfg, 'ALLOWED_TAGS') ? addToSet({}, cfg.ALLOWED_TAGS, transformCaseFunc) : DEFAULT_ALLOWED_TAGS;
        ALLOWED_ATTR = objectHasOwnProperty(cfg, 'ALLOWED_ATTR') ? addToSet({}, cfg.ALLOWED_ATTR, transformCaseFunc) : DEFAULT_ALLOWED_ATTR;
        ALLOWED_NAMESPACES = objectHasOwnProperty(cfg, 'ALLOWED_NAMESPACES') ? addToSet({}, cfg.ALLOWED_NAMESPACES, stringToString) : DEFAULT_ALLOWED_NAMESPACES;
        URI_SAFE_ATTRIBUTES = objectHasOwnProperty(cfg, 'ADD_URI_SAFE_ATTR') ? addToSet(clone(DEFAULT_URI_SAFE_ATTRIBUTES),
        // eslint-disable-line indent
        cfg.ADD_URI_SAFE_ATTR,
        // eslint-disable-line indent
        transformCaseFunc // eslint-disable-line indent
        ) // eslint-disable-line indent
        : DEFAULT_URI_SAFE_ATTRIBUTES;
        DATA_URI_TAGS = objectHasOwnProperty(cfg, 'ADD_DATA_URI_TAGS') ? addToSet(clone(DEFAULT_DATA_URI_TAGS),
        // eslint-disable-line indent
        cfg.ADD_DATA_URI_TAGS,
        // eslint-disable-line indent
        transformCaseFunc // eslint-disable-line indent
        ) // eslint-disable-line indent
        : DEFAULT_DATA_URI_TAGS;
        FORBID_CONTENTS = objectHasOwnProperty(cfg, 'FORBID_CONTENTS') ? addToSet({}, cfg.FORBID_CONTENTS, transformCaseFunc) : DEFAULT_FORBID_CONTENTS;
        FORBID_TAGS = objectHasOwnProperty(cfg, 'FORBID_TAGS') ? addToSet({}, cfg.FORBID_TAGS, transformCaseFunc) : {};
        FORBID_ATTR = objectHasOwnProperty(cfg, 'FORBID_ATTR') ? addToSet({}, cfg.FORBID_ATTR, transformCaseFunc) : {};
        USE_PROFILES = objectHasOwnProperty(cfg, 'USE_PROFILES') ? cfg.USE_PROFILES : false;
        ALLOW_ARIA_ATTR = cfg.ALLOW_ARIA_ATTR !== false; // Default true
        ALLOW_DATA_ATTR = cfg.ALLOW_DATA_ATTR !== false; // Default true
        ALLOW_UNKNOWN_PROTOCOLS = cfg.ALLOW_UNKNOWN_PROTOCOLS || false; // Default false
        ALLOW_SELF_CLOSE_IN_ATTR = cfg.ALLOW_SELF_CLOSE_IN_ATTR !== false; // Default true
        SAFE_FOR_TEMPLATES = cfg.SAFE_FOR_TEMPLATES || false; // Default false
        SAFE_FOR_XML = cfg.SAFE_FOR_XML !== false; // Default true
        WHOLE_DOCUMENT = cfg.WHOLE_DOCUMENT || false; // Default false
        RETURN_DOM = cfg.RETURN_DOM || false; // Default false
        RETURN_DOM_FRAGMENT = cfg.RETURN_DOM_FRAGMENT || false; // Default false
        RETURN_TRUSTED_TYPE = cfg.RETURN_TRUSTED_TYPE || false; // Default false
        FORCE_BODY = cfg.FORCE_BODY || false; // Default false
        SANITIZE_DOM = cfg.SANITIZE_DOM !== false; // Default true
        SANITIZE_NAMED_PROPS = cfg.SANITIZE_NAMED_PROPS || false; // Default false
        KEEP_CONTENT = cfg.KEEP_CONTENT !== false; // Default true
        IN_PLACE = cfg.IN_PLACE || false; // Default false
        IS_ALLOWED_URI$1 = cfg.ALLOWED_URI_REGEXP || IS_ALLOWED_URI;
        NAMESPACE = cfg.NAMESPACE || HTML_NAMESPACE;
        CUSTOM_ELEMENT_HANDLING = cfg.CUSTOM_ELEMENT_HANDLING || {};
        if (cfg.CUSTOM_ELEMENT_HANDLING && isRegexOrFunction(cfg.CUSTOM_ELEMENT_HANDLING.tagNameCheck)) {
          CUSTOM_ELEMENT_HANDLING.tagNameCheck = cfg.CUSTOM_ELEMENT_HANDLING.tagNameCheck;
        }
        if (cfg.CUSTOM_ELEMENT_HANDLING && isRegexOrFunction(cfg.CUSTOM_ELEMENT_HANDLING.attributeNameCheck)) {
          CUSTOM_ELEMENT_HANDLING.attributeNameCheck = cfg.CUSTOM_ELEMENT_HANDLING.attributeNameCheck;
        }
        if (cfg.CUSTOM_ELEMENT_HANDLING && typeof cfg.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements === 'boolean') {
          CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements = cfg.CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements;
        }
        if (SAFE_FOR_TEMPLATES) {
          ALLOW_DATA_ATTR = false;
        }
        if (RETURN_DOM_FRAGMENT) {
          RETURN_DOM = true;
        }

        /* Parse profile info */
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

        /* Merge configuration parameters */
        if (cfg.ADD_TAGS) {
          if (ALLOWED_TAGS === DEFAULT_ALLOWED_TAGS) {
            ALLOWED_TAGS = clone(ALLOWED_TAGS);
          }
          addToSet(ALLOWED_TAGS, cfg.ADD_TAGS, transformCaseFunc);
        }
        if (cfg.ADD_ATTR) {
          if (ALLOWED_ATTR === DEFAULT_ALLOWED_ATTR) {
            ALLOWED_ATTR = clone(ALLOWED_ATTR);
          }
          addToSet(ALLOWED_ATTR, cfg.ADD_ATTR, transformCaseFunc);
        }
        if (cfg.ADD_URI_SAFE_ATTR) {
          addToSet(URI_SAFE_ATTRIBUTES, cfg.ADD_URI_SAFE_ATTR, transformCaseFunc);
        }
        if (cfg.FORBID_CONTENTS) {
          if (FORBID_CONTENTS === DEFAULT_FORBID_CONTENTS) {
            FORBID_CONTENTS = clone(FORBID_CONTENTS);
          }
          addToSet(FORBID_CONTENTS, cfg.FORBID_CONTENTS, transformCaseFunc);
        }

        /* Add #text in case KEEP_CONTENT is set to true */
        if (KEEP_CONTENT) {
          ALLOWED_TAGS['#text'] = true;
        }

        /* Add html, head and body to ALLOWED_TAGS in case WHOLE_DOCUMENT is true */
        if (WHOLE_DOCUMENT) {
          addToSet(ALLOWED_TAGS, ['html', 'head', 'body']);
        }

        /* Add tbody to ALLOWED_TAGS in case tables are permitted, see #286, #365 */
        if (ALLOWED_TAGS.table) {
          addToSet(ALLOWED_TAGS, ['tbody']);
          delete FORBID_TAGS.tbody;
        }
        if (cfg.TRUSTED_TYPES_POLICY) {
          if (typeof cfg.TRUSTED_TYPES_POLICY.createHTML !== 'function') {
            throw typeErrorCreate('TRUSTED_TYPES_POLICY configuration option must provide a "createHTML" hook.');
          }
          if (typeof cfg.TRUSTED_TYPES_POLICY.createScriptURL !== 'function') {
            throw typeErrorCreate('TRUSTED_TYPES_POLICY configuration option must provide a "createScriptURL" hook.');
          }

          // Overwrite existing TrustedTypes policy.
          trustedTypesPolicy = cfg.TRUSTED_TYPES_POLICY;

          // Sign local variables required by `sanitize`.
          emptyHTML = trustedTypesPolicy.createHTML('');
        } else {
          // Uninitialized policy, attempt to initialize the internal dompurify policy.
          if (trustedTypesPolicy === undefined) {
            trustedTypesPolicy = _createTrustedTypesPolicy(trustedTypes, currentScript);
          }

          // If creating the internal policy succeeded sign internal variables.
          if (trustedTypesPolicy !== null && typeof emptyHTML === 'string') {
            emptyHTML = trustedTypesPolicy.createHTML('');
          }
        }

        // Prevent further manipulation of configuration.
        // Not available in IE8, Safari 5, etc.
        if (freeze) {
          freeze(cfg);
        }
        CONFIG = cfg;
      };
      const MATHML_TEXT_INTEGRATION_POINTS = addToSet({}, ['mi', 'mo', 'mn', 'ms', 'mtext']);
      const HTML_INTEGRATION_POINTS = addToSet({}, ['foreignobject', 'annotation-xml']);

      // Certain elements are allowed in both SVG and HTML
      // namespace. We need to specify them explicitly
      // so that they don't get erroneously deleted from
      // HTML namespace.
      const COMMON_SVG_AND_HTML_ELEMENTS = addToSet({}, ['title', 'style', 'font', 'a', 'script']);

      /* Keep track of all possible SVG and MathML tags
       * so that we can perform the namespace checks
       * correctly. */
      const ALL_SVG_TAGS = addToSet({}, [...svg$1, ...svgFilters, ...svgDisallowed]);
      const ALL_MATHML_TAGS = addToSet({}, [...mathMl$1, ...mathMlDisallowed]);

      /**
       * @param  {Element} element a DOM element whose namespace is being checked
       * @returns {boolean} Return false if the element has a
       *  namespace that a spec-compliant parser would never
       *  return. Return true otherwise.
       */
      const _checkValidNamespace = function _checkValidNamespace(element) {
        let parent = getParentNode(element);

        // In JSDOM, if we're inside shadow DOM, then parentNode
        // can be null. We just simulate parent in this case.
        if (!parent || !parent.tagName) {
          parent = {
            namespaceURI: NAMESPACE,
            tagName: 'template'
          };
        }
        const tagName = stringToLowerCase(element.tagName);
        const parentTagName = stringToLowerCase(parent.tagName);
        if (!ALLOWED_NAMESPACES[element.namespaceURI]) {
          return false;
        }
        if (element.namespaceURI === SVG_NAMESPACE) {
          // The only way to switch from HTML namespace to SVG
          // is via <svg>. If it happens via any other tag, then
          // it should be killed.
          if (parent.namespaceURI === HTML_NAMESPACE) {
            return tagName === 'svg';
          }

          // The only way to switch from MathML to SVG is via`
          // svg if parent is either <annotation-xml> or MathML
          // text integration points.
          if (parent.namespaceURI === MATHML_NAMESPACE) {
            return tagName === 'svg' && (parentTagName === 'annotation-xml' || MATHML_TEXT_INTEGRATION_POINTS[parentTagName]);
          }

          // We only allow elements that are defined in SVG
          // spec. All others are disallowed in SVG namespace.
          return Boolean(ALL_SVG_TAGS[tagName]);
        }
        if (element.namespaceURI === MATHML_NAMESPACE) {
          // The only way to switch from HTML namespace to MathML
          // is via <math>. If it happens via any other tag, then
          // it should be killed.
          if (parent.namespaceURI === HTML_NAMESPACE) {
            return tagName === 'math';
          }

          // The only way to switch from SVG to MathML is via
          // <math> and HTML integration points
          if (parent.namespaceURI === SVG_NAMESPACE) {
            return tagName === 'math' && HTML_INTEGRATION_POINTS[parentTagName];
          }

          // We only allow elements that are defined in MathML
          // spec. All others are disallowed in MathML namespace.
          return Boolean(ALL_MATHML_TAGS[tagName]);
        }
        if (element.namespaceURI === HTML_NAMESPACE) {
          // The only way to switch from SVG to HTML is via
          // HTML integration points, and from MathML to HTML
          // is via MathML text integration points
          if (parent.namespaceURI === SVG_NAMESPACE && !HTML_INTEGRATION_POINTS[parentTagName]) {
            return false;
          }
          if (parent.namespaceURI === MATHML_NAMESPACE && !MATHML_TEXT_INTEGRATION_POINTS[parentTagName]) {
            return false;
          }

          // We disallow tags that are specific for MathML
          // or SVG and should never appear in HTML namespace
          return !ALL_MATHML_TAGS[tagName] && (COMMON_SVG_AND_HTML_ELEMENTS[tagName] || !ALL_SVG_TAGS[tagName]);
        }

        // For XHTML and XML documents that support custom namespaces
        if (PARSER_MEDIA_TYPE === 'application/xhtml+xml' && ALLOWED_NAMESPACES[element.namespaceURI]) {
          return true;
        }

        // The code should never reach this place (this means
        // that the element somehow got namespace that is not
        // HTML, SVG, MathML or allowed via ALLOWED_NAMESPACES).
        // Return false just in case.
        return false;
      };

      /**
       * _forceRemove
       *
       * @param  {Node} node a DOM node
       */
      const _forceRemove = function _forceRemove(node) {
        arrayPush(DOMPurify.removed, {
          element: node
        });
        try {
          // eslint-disable-next-line unicorn/prefer-dom-node-remove
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
      const _removeAttribute = function _removeAttribute(name, node) {
        try {
          arrayPush(DOMPurify.removed, {
            attribute: node.getAttributeNode(name),
            from: node
          });
        } catch (_) {
          arrayPush(DOMPurify.removed, {
            attribute: null,
            from: node
          });
        }
        node.removeAttribute(name);

        // We void attribute values for unremovable "is"" attributes
        if (name === 'is' && !ALLOWED_ATTR[name]) {
          if (RETURN_DOM || RETURN_DOM_FRAGMENT) {
            try {
              _forceRemove(node);
            } catch (_) {}
          } else {
            try {
              node.setAttribute(name, '');
            } catch (_) {}
          }
        }
      };

      /**
       * _initDocument
       *
       * @param  {String} dirty a string of dirty markup
       * @return {Document} a DOM, filled with the dirty markup
       */
      const _initDocument = function _initDocument(dirty) {
        /* Create a HTML document */
        let doc = null;
        let leadingWhitespace = null;
        if (FORCE_BODY) {
          dirty = '<remove></remove>' + dirty;
        } else {
          /* If FORCE_BODY isn't used, leading whitespace needs to be preserved manually */
          const matches = stringMatch(dirty, /^[\r\n\t ]+/);
          leadingWhitespace = matches && matches[0];
        }
        if (PARSER_MEDIA_TYPE === 'application/xhtml+xml' && NAMESPACE === HTML_NAMESPACE) {
          // Root of XHTML doc must contain xmlns declaration (see https://www.w3.org/TR/xhtml1/normative.html#strict)
          dirty = '<html xmlns="http://www.w3.org/1999/xhtml"><head></head><body>' + dirty + '</body></html>';
        }
        const dirtyPayload = trustedTypesPolicy ? trustedTypesPolicy.createHTML(dirty) : dirty;
        /*
         * Use the DOMParser API by default, fallback later if needs be
         * DOMParser not work for svg when has multiple root element.
         */
        if (NAMESPACE === HTML_NAMESPACE) {
          try {
            doc = new DOMParser().parseFromString(dirtyPayload, PARSER_MEDIA_TYPE);
          } catch (_) {}
        }

        /* Use createHTMLDocument in case DOMParser is not available */
        if (!doc || !doc.documentElement) {
          doc = implementation.createDocument(NAMESPACE, 'template', null);
          try {
            doc.documentElement.innerHTML = IS_EMPTY_INPUT ? emptyHTML : dirtyPayload;
          } catch (_) {
            // Syntax error if dirtyPayload is invalid xml
          }
        }
        const body = doc.body || doc.documentElement;
        if (dirty && leadingWhitespace) {
          body.insertBefore(document.createTextNode(leadingWhitespace), body.childNodes[0] || null);
        }

        /* Work on whole document or just its body */
        if (NAMESPACE === HTML_NAMESPACE) {
          return getElementsByTagName.call(doc, WHOLE_DOCUMENT ? 'html' : 'body')[0];
        }
        return WHOLE_DOCUMENT ? doc.documentElement : body;
      };

      /**
       * Creates a NodeIterator object that you can use to traverse filtered lists of nodes or elements in a document.
       *
       * @param  {Node} root The root element or node to start traversing on.
       * @return {NodeIterator} The created NodeIterator
       */
      const _createNodeIterator = function _createNodeIterator(root) {
        return createNodeIterator.call(root.ownerDocument || root, root,
        // eslint-disable-next-line no-bitwise
        NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_COMMENT | NodeFilter.SHOW_TEXT | NodeFilter.SHOW_PROCESSING_INSTRUCTION | NodeFilter.SHOW_CDATA_SECTION, null);
      };

      /**
       * _isClobbered
       *
       * @param  {Node} elm element to check for clobbering attacks
       * @return {Boolean} true if clobbered, false if safe
       */
      const _isClobbered = function _isClobbered(elm) {
        return elm instanceof HTMLFormElement && (typeof elm.nodeName !== 'string' || typeof elm.textContent !== 'string' || typeof elm.removeChild !== 'function' || !(elm.attributes instanceof NamedNodeMap) || typeof elm.removeAttribute !== 'function' || typeof elm.setAttribute !== 'function' || typeof elm.namespaceURI !== 'string' || typeof elm.insertBefore !== 'function' || typeof elm.hasChildNodes !== 'function');
      };

      /**
       * Checks whether the given object is a DOM node.
       *
       * @param  {Node} object object to check whether it's a DOM node
       * @return {Boolean} true is object is a DOM node
       */
      const _isNode = function _isNode(object) {
        return typeof Node === 'function' && object instanceof Node;
      };

      /**
       * _executeHook
       * Execute user configurable hooks
       *
       * @param  {String} entryPoint  Name of the hook's entry point
       * @param  {Node} currentNode node to work on with the hook
       * @param  {Object} data additional hook parameters
       */
      const _executeHook = function _executeHook(entryPoint, currentNode, data) {
        if (!hooks[entryPoint]) {
          return;
        }
        arrayForEach(hooks[entryPoint], hook => {
          hook.call(DOMPurify, currentNode, data, CONFIG);
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
      const _sanitizeElements = function _sanitizeElements(currentNode) {
        let content = null;

        /* Execute a hook if present */
        _executeHook('beforeSanitizeElements', currentNode, null);

        /* Check if element is clobbered or can clobber */
        if (_isClobbered(currentNode)) {
          _forceRemove(currentNode);
          return true;
        }

        /* Now let's check the element's type and name */
        const tagName = transformCaseFunc(currentNode.nodeName);

        /* Execute a hook if present */
        _executeHook('uponSanitizeElement', currentNode, {
          tagName,
          allowedTags: ALLOWED_TAGS
        });

        /* Detect mXSS attempts abusing namespace confusion */
        if (currentNode.hasChildNodes() && !_isNode(currentNode.firstElementChild) && regExpTest(/<[/\w]/g, currentNode.innerHTML) && regExpTest(/<[/\w]/g, currentNode.textContent)) {
          _forceRemove(currentNode);
          return true;
        }

        /* Remove any occurrence of processing instructions */
        if (currentNode.nodeType === NODE_TYPE.progressingInstruction) {
          _forceRemove(currentNode);
          return true;
        }

        /* Remove any kind of possibly harmful comments */
        if (SAFE_FOR_XML && currentNode.nodeType === NODE_TYPE.comment && regExpTest(/<[/\w]/g, currentNode.data)) {
          _forceRemove(currentNode);
          return true;
        }

        /* Remove element if anything forbids its presence */
        if (!ALLOWED_TAGS[tagName] || FORBID_TAGS[tagName]) {
          /* Check if we have a custom element to handle */
          if (!FORBID_TAGS[tagName] && _isBasicCustomElement(tagName)) {
            if (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, tagName)) {
              return false;
            }
            if (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(tagName)) {
              return false;
            }
          }

          /* Keep content except for bad-listed elements */
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

        /* Check whether element has a valid namespace */
        if (currentNode instanceof Element && !_checkValidNamespace(currentNode)) {
          _forceRemove(currentNode);
          return true;
        }

        /* Make sure that older browsers don't get fallback-tag mXSS */
        if ((tagName === 'noscript' || tagName === 'noembed' || tagName === 'noframes') && regExpTest(/<\/no(script|embed|frames)/i, currentNode.innerHTML)) {
          _forceRemove(currentNode);
          return true;
        }

        /* Sanitize element content to be template-safe */
        if (SAFE_FOR_TEMPLATES && currentNode.nodeType === NODE_TYPE.text) {
          /* Get the element's text content */
          content = currentNode.textContent;
          arrayForEach([MUSTACHE_EXPR, ERB_EXPR, TMPLIT_EXPR], expr => {
            content = stringReplace(content, expr, ' ');
          });
          if (currentNode.textContent !== content) {
            arrayPush(DOMPurify.removed, {
              element: currentNode.cloneNode()
            });
            currentNode.textContent = content;
          }
        }

        /* Execute a hook if present */
        _executeHook('afterSanitizeElements', currentNode, null);
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
      // eslint-disable-next-line complexity
      const _isValidAttribute = function _isValidAttribute(lcTag, lcName, value) {
        /* Make sure attribute cannot clobber */
        if (SANITIZE_DOM && (lcName === 'id' || lcName === 'name') && (value in document || value in formElement)) {
          return false;
        }

        /* Allow valid data-* attributes: At least one character after "-"
            (https://html.spec.whatwg.org/multipage/dom.html#embedding-custom-non-visible-data-with-the-data-*-attributes)
            XML-compatible (https://html.spec.whatwg.org/multipage/infrastructure.html#xml-compatible and http://www.w3.org/TR/xml/#d0e804)
            We don't need to check the value; it's always URI safe. */
        if (ALLOW_DATA_ATTR && !FORBID_ATTR[lcName] && regExpTest(DATA_ATTR, lcName)) ; else if (ALLOW_ARIA_ATTR && regExpTest(ARIA_ATTR, lcName)) ; else if (!ALLOWED_ATTR[lcName] || FORBID_ATTR[lcName]) {
          if (
          // First condition does a very basic check if a) it's basically a valid custom element tagname AND
          // b) if the tagName passes whatever the user has configured for CUSTOM_ELEMENT_HANDLING.tagNameCheck
          // and c) if the attribute name passes whatever the user has configured for CUSTOM_ELEMENT_HANDLING.attributeNameCheck
          _isBasicCustomElement(lcTag) && (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, lcTag) || CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(lcTag)) && (CUSTOM_ELEMENT_HANDLING.attributeNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.attributeNameCheck, lcName) || CUSTOM_ELEMENT_HANDLING.attributeNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.attributeNameCheck(lcName)) ||
          // Alternative, second condition checks if it's an `is`-attribute, AND
          // the value passes whatever the user has configured for CUSTOM_ELEMENT_HANDLING.tagNameCheck
          lcName === 'is' && CUSTOM_ELEMENT_HANDLING.allowCustomizedBuiltInElements && (CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof RegExp && regExpTest(CUSTOM_ELEMENT_HANDLING.tagNameCheck, value) || CUSTOM_ELEMENT_HANDLING.tagNameCheck instanceof Function && CUSTOM_ELEMENT_HANDLING.tagNameCheck(value))) ; else {
            return false;
          }
          /* Check value is safe. First, is attr inert? If so, is safe */
        } else if (URI_SAFE_ATTRIBUTES[lcName]) ; else if (regExpTest(IS_ALLOWED_URI$1, stringReplace(value, ATTR_WHITESPACE, ''))) ; else if ((lcName === 'src' || lcName === 'xlink:href' || lcName === 'href') && lcTag !== 'script' && stringIndexOf(value, 'data:') === 0 && DATA_URI_TAGS[lcTag]) ; else if (ALLOW_UNKNOWN_PROTOCOLS && !regExpTest(IS_SCRIPT_OR_DATA, stringReplace(value, ATTR_WHITESPACE, ''))) ; else if (value) {
          return false;
        } else ;
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
      const _isBasicCustomElement = function _isBasicCustomElement(tagName) {
        return tagName !== 'annotation-xml' && stringMatch(tagName, CUSTOM_ELEMENT);
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
      const _sanitizeAttributes = function _sanitizeAttributes(currentNode) {
        /* Execute a hook if present */
        _executeHook('beforeSanitizeAttributes', currentNode, null);
        const {
          attributes
        } = currentNode;

        /* Check if we have attributes; if not we might have a text node */
        if (!attributes) {
          return;
        }
        const hookEvent = {
          attrName: '',
          attrValue: '',
          keepAttr: true,
          allowedAttributes: ALLOWED_ATTR
        };
        let l = attributes.length;

        /* Go backwards over all attributes; safely remove bad ones */
        while (l--) {
          const attr = attributes[l];
          const {
            name,
            namespaceURI,
            value: attrValue
          } = attr;
          const lcName = transformCaseFunc(name);
          let value = name === 'value' ? attrValue : stringTrim(attrValue);

          /* Execute a hook if present */
          hookEvent.attrName = lcName;
          hookEvent.attrValue = value;
          hookEvent.keepAttr = true;
          hookEvent.forceKeepAttr = undefined; // Allows developers to see this is a property they can set
          _executeHook('uponSanitizeAttribute', currentNode, hookEvent);
          value = hookEvent.attrValue;

          /* Work around a security issue with comments inside attributes */
          if (SAFE_FOR_XML && regExpTest(/((--!?|])>)|<\/(style|title)/i, value)) {
            _removeAttribute(name, currentNode);
            continue;
          }

          /* Did the hooks approve of the attribute? */
          if (hookEvent.forceKeepAttr) {
            continue;
          }

          /* Remove attribute */
          _removeAttribute(name, currentNode);

          /* Did the hooks approve of the attribute? */
          if (!hookEvent.keepAttr) {
            continue;
          }

          /* Work around a security issue in jQuery 3.0 */
          if (!ALLOW_SELF_CLOSE_IN_ATTR && regExpTest(/\/>/i, value)) {
            _removeAttribute(name, currentNode);
            continue;
          }

          /* Sanitize attribute content to be template-safe */
          if (SAFE_FOR_TEMPLATES) {
            arrayForEach([MUSTACHE_EXPR, ERB_EXPR, TMPLIT_EXPR], expr => {
              value = stringReplace(value, expr, ' ');
            });
          }

          /* Is `value` valid for this attribute? */
          const lcTag = transformCaseFunc(currentNode.nodeName);
          if (!_isValidAttribute(lcTag, lcName, value)) {
            continue;
          }

          /* Full DOM Clobbering protection via namespace isolation,
           * Prefix id and name attributes with `user-content-`
           */
          if (SANITIZE_NAMED_PROPS && (lcName === 'id' || lcName === 'name')) {
            // Remove the attribute with this value
            _removeAttribute(name, currentNode);

            // Prefix the value and later re-create the attribute with the sanitized value
            value = SANITIZE_NAMED_PROPS_PREFIX + value;
          }

          /* Handle attributes that require Trusted Types */
          if (trustedTypesPolicy && typeof trustedTypes === 'object' && typeof trustedTypes.getAttributeType === 'function') {
            if (namespaceURI) ; else {
              switch (trustedTypes.getAttributeType(lcTag, lcName)) {
                case 'TrustedHTML':
                  {
                    value = trustedTypesPolicy.createHTML(value);
                    break;
                  }
                case 'TrustedScriptURL':
                  {
                    value = trustedTypesPolicy.createScriptURL(value);
                    break;
                  }
              }
            }
          }

          /* Handle invalid data-* attribute set by try-catching it */
          try {
            if (namespaceURI) {
              currentNode.setAttributeNS(namespaceURI, name, value);
            } else {
              /* Fallback to setAttribute() for browser-unrecognized namespaces e.g. "x-schema". */
              currentNode.setAttribute(name, value);
            }
            if (_isClobbered(currentNode)) {
              _forceRemove(currentNode);
            } else {
              arrayPop(DOMPurify.removed);
            }
          } catch (_) {}
        }

        /* Execute a hook if present */
        _executeHook('afterSanitizeAttributes', currentNode, null);
      };

      /**
       * _sanitizeShadowDOM
       *
       * @param  {DocumentFragment} fragment to iterate over recursively
       */
      const _sanitizeShadowDOM = function _sanitizeShadowDOM(fragment) {
        let shadowNode = null;
        const shadowIterator = _createNodeIterator(fragment);

        /* Execute a hook if present */
        _executeHook('beforeSanitizeShadowDOM', fragment, null);
        while (shadowNode = shadowIterator.nextNode()) {
          /* Execute a hook if present */
          _executeHook('uponSanitizeShadowNode', shadowNode, null);

          /* Sanitize tags and elements */
          if (_sanitizeElements(shadowNode)) {
            continue;
          }

          /* Deep shadow DOM detected */
          if (shadowNode.content instanceof DocumentFragment) {
            _sanitizeShadowDOM(shadowNode.content);
          }

          /* Check attributes, sanitize if necessary */
          _sanitizeAttributes(shadowNode);
        }

        /* Execute a hook if present */
        _executeHook('afterSanitizeShadowDOM', fragment, null);
      };

      /**
       * Sanitize
       * Public method providing core sanitation functionality
       *
       * @param {String|Node} dirty string or DOM node
       * @param {Object} cfg object
       */
      // eslint-disable-next-line complexity
      DOMPurify.sanitize = function (dirty) {
        let cfg = arguments.length > 1 && arguments[1] !== undefined ? arguments[1] : {};
        let body = null;
        let importedNode = null;
        let currentNode = null;
        let returnNode = null;
        /* Make sure we have a string to sanitize.
          DO NOT return early, as this will return the wrong type if
          the user has requested a DOM object rather than a string */
        IS_EMPTY_INPUT = !dirty;
        if (IS_EMPTY_INPUT) {
          dirty = '<!-->';
        }

        /* Stringify, in case dirty is an object */
        if (typeof dirty !== 'string' && !_isNode(dirty)) {
          if (typeof dirty.toString === 'function') {
            dirty = dirty.toString();
            if (typeof dirty !== 'string') {
              throw typeErrorCreate('dirty is not a string, aborting');
            }
          } else {
            throw typeErrorCreate('toString is not a function');
          }
        }

        /* Return dirty HTML if DOMPurify cannot run */
        if (!DOMPurify.isSupported) {
          return dirty;
        }

        /* Assign config vars */
        if (!SET_CONFIG) {
          _parseConfig(cfg);
        }

        /* Clean up removed elements */
        DOMPurify.removed = [];

        /* Check if dirty is correctly typed for IN_PLACE */
        if (typeof dirty === 'string') {
          IN_PLACE = false;
        }
        if (IN_PLACE) {
          /* Do some early pre-sanitization to avoid unsafe root nodes */
          if (dirty.nodeName) {
            const tagName = transformCaseFunc(dirty.nodeName);
            if (!ALLOWED_TAGS[tagName] || FORBID_TAGS[tagName]) {
              throw typeErrorCreate('root node is forbidden and cannot be sanitized in-place');
            }
          }
        } else if (dirty instanceof Node) {
          /* If dirty is a DOM element, append to an empty document to avoid
             elements being stripped by the parser */
          body = _initDocument('<!---->');
          importedNode = body.ownerDocument.importNode(dirty, true);
          if (importedNode.nodeType === NODE_TYPE.element && importedNode.nodeName === 'BODY') {
            /* Node is already a body, use as is */
            body = importedNode;
          } else if (importedNode.nodeName === 'HTML') {
            body = importedNode;
          } else {
            // eslint-disable-next-line unicorn/prefer-dom-node-append
            body.appendChild(importedNode);
          }
        } else {
          /* Exit directly if we have nothing to do */
          if (!RETURN_DOM && !SAFE_FOR_TEMPLATES && !WHOLE_DOCUMENT &&
          // eslint-disable-next-line unicorn/prefer-includes
          dirty.indexOf('<') === -1) {
            return trustedTypesPolicy && RETURN_TRUSTED_TYPE ? trustedTypesPolicy.createHTML(dirty) : dirty;
          }

          /* Initialize the document to work on */
          body = _initDocument(dirty);

          /* Check we have a DOM node from the data */
          if (!body) {
            return RETURN_DOM ? null : RETURN_TRUSTED_TYPE ? emptyHTML : '';
          }
        }

        /* Remove first element node (ours) if FORCE_BODY is set */
        if (body && FORCE_BODY) {
          _forceRemove(body.firstChild);
        }

        /* Get node iterator */
        const nodeIterator = _createNodeIterator(IN_PLACE ? dirty : body);

        /* Now start iterating over the created document */
        while (currentNode = nodeIterator.nextNode()) {
          /* Sanitize tags and elements */
          if (_sanitizeElements(currentNode)) {
            continue;
          }

          /* Shadow DOM detected, sanitize it */
          if (currentNode.content instanceof DocumentFragment) {
            _sanitizeShadowDOM(currentNode.content);
          }

          /* Check attributes, sanitize if necessary */
          _sanitizeAttributes(currentNode);
        }

        /* If we sanitized `dirty` in-place, return it. */
        if (IN_PLACE) {
          return dirty;
        }

        /* Return sanitized string or DOM */
        if (RETURN_DOM) {
          if (RETURN_DOM_FRAGMENT) {
            returnNode = createDocumentFragment.call(body.ownerDocument);
            while (body.firstChild) {
              // eslint-disable-next-line unicorn/prefer-dom-node-append
              returnNode.appendChild(body.firstChild);
            }
          } else {
            returnNode = body;
          }
          if (ALLOWED_ATTR.shadowroot || ALLOWED_ATTR.shadowrootmode) {
            /*
              AdoptNode() is not used because internal state is not reset
              (e.g. the past names map of a HTMLFormElement), this is safe
              in theory but we would rather not risk another attack vector.
              The state that is cloned by importNode() is explicitly defined
              by the specs.
            */
            returnNode = importNode.call(originalDocument, returnNode, true);
          }
          return returnNode;
        }
        let serializedHTML = WHOLE_DOCUMENT ? body.outerHTML : body.innerHTML;

        /* Serialize doctype if allowed */
        if (WHOLE_DOCUMENT && ALLOWED_TAGS['!doctype'] && body.ownerDocument && body.ownerDocument.doctype && body.ownerDocument.doctype.name && regExpTest(DOCTYPE_NAME, body.ownerDocument.doctype.name)) {
          serializedHTML = '<!DOCTYPE ' + body.ownerDocument.doctype.name + '>\n' + serializedHTML;
        }

        /* Sanitize final string template-safe */
        if (SAFE_FOR_TEMPLATES) {
          arrayForEach([MUSTACHE_EXPR, ERB_EXPR, TMPLIT_EXPR], expr => {
            serializedHTML = stringReplace(serializedHTML, expr, ' ');
          });
        }
        return trustedTypesPolicy && RETURN_TRUSTED_TYPE ? trustedTypesPolicy.createHTML(serializedHTML) : serializedHTML;
      };

      /**
       * Public method to set the configuration once
       * setConfig
       *
       * @param {Object} cfg configuration object
       */
      DOMPurify.setConfig = function () {
        let cfg = arguments.length > 0 && arguments[0] !== undefined ? arguments[0] : {};
        _parseConfig(cfg);
        SET_CONFIG = true;
      };

      /**
       * Public method to remove the configuration
       * clearConfig
       *
       */
      DOMPurify.clearConfig = function () {
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
      DOMPurify.isValidAttribute = function (tag, attr, value) {
        /* Initialize shared config vars if necessary. */
        if (!CONFIG) {
          _parseConfig({});
        }
        const lcTag = transformCaseFunc(tag);
        const lcName = transformCaseFunc(attr);
        return _isValidAttribute(lcTag, lcName, value);
      };

      /**
       * AddHook
       * Public method to add DOMPurify hooks
       *
       * @param {String} entryPoint entry point for the hook to add
       * @param {Function} hookFunction function to execute
       */
      DOMPurify.addHook = function (entryPoint, hookFunction) {
        if (typeof hookFunction !== 'function') {
          return;
        }
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
      DOMPurify.removeHook = function (entryPoint) {
        if (hooks[entryPoint]) {
          return arrayPop(hooks[entryPoint]);
        }
      };

      /**
       * RemoveHooks
       * Public method to remove all DOMPurify hooks at a given entryPoint
       *
       * @param  {String} entryPoint entry point for the hooks to remove
       */
      DOMPurify.removeHooks = function (entryPoint) {
        if (hooks[entryPoint]) {
          hooks[entryPoint] = [];
        }
      };

      /**
       * RemoveAllHooks
       * Public method to remove all DOMPurify hooks
       */
      DOMPurify.removeAllHooks = function () {
        hooks = {};
      };
      return DOMPurify;
    }
    var purify = createDOMPurify();

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

    const sanitise = (input) => purify.sanitize(input, { RETURN_TRUSTED_TYPE: true });
    const morphdom = morphdomFactory((fromEl, toEl) => {
        if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateAttributes", { cancelable: true, bubbles: true, detail: { node: fromEl, source: toEl } }))) {
            return;
        }
        morphAttrs(fromEl, toEl);
        if (!fromEl.dispatchEvent(new CustomEvent("wfc:updateAttributes", { bubbles: true, detail: { node: fromEl, source: toEl } }))) {
            return;
        }
    });
    const postbackMutex = new Mutex();
    let pendingPostbacks = 0;
    class ViewStateContainer {
        constructor(element, formData) {
            this.element = element;
            this.formData = formData;
        }
        querySelector(selector) {
            if (this.element) {
                const result = this.element.querySelector(selector);
                if (result) {
                    return result;
                }
            }
            return document.body.closest(":not([data-wfc-form]) " + selector);
        }
        querySelectorAll(selector) {
            const elements = document.body.querySelectorAll(":not([data-wfc-form]) " + selector);
            if (this.element) {
                return [
                    ...this.element.querySelectorAll(selector),
                    ...elements
                ];
            }
            else {
                return Array.from(elements);
            }
        }
        addInputs(selector) {
            const elements = this.querySelectorAll(selector);
            for (let i = 0; i < elements.length; i++) {
                const element = elements[i];
                addElement(element, this.formData);
            }
        }
    }
    function postBackElement(element, eventTarget, eventArgument) {
        const form = getForm(element);
        const streamPanel = getStreamPanel(element);
        if (streamPanel) {
            return sendToStream(streamPanel, eventTarget, eventArgument);
        }
        else {
            return submitForm(element, form, eventTarget, eventArgument);
        }
    }
    function sendToStream(streamPanel, eventTarget, eventArgument) {
        const webSocket = streamPanel.webSocket;
        if (!webSocket) {
            throw new Error("No WebSocket connection");
        }
        const data = {
            t: eventTarget,
            a: eventArgument
        };
        webSocket.send(JSON.stringify(data));
        return Promise.resolve();
    }
    function addElement(element, formData) {
        if (element.type === "checkbox" || element.type === "radio") {
            if (element.checked) {
                formData.append(element.name, element.value);
            }
        }
        else {
            formData.append(element.name, element.value);
        }
    }
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
    function hasElementFile(element) {
        const elements = document.body.querySelectorAll('input[type="file"]');
        for (let i = 0; i < elements.length; i++) {
            const element = elements[i];
            if (element.files.length > 0) {
                return true;
            }
        }
        return false;
    }
    function getForm(element) {
        return element.closest('[data-wfc-form]');
    }
    function getStreamPanel(element) {
        return element.closest('[data-wfc-stream]');
    }
    function addInputs(formData, root, addFormElements, allowFileUploads) {
        // Add all the form elements that are not in a form
        const elements = [];
        // @ts-ignore
        for (const element of root.querySelectorAll('input, select, textarea')) {
            if (!element.closest('[data-wfc-ignore]')) {
                elements.push(element);
            }
        }
        document.dispatchEvent(new CustomEvent("wfc:addInputs", { detail: { elements } }));
        for (let i = 0; i < elements.length; i++) {
            const element = elements[i];
            if (element.hasAttribute('data-wfc-ignore') || element.type === "button" ||
                element.type === "submit" || element.type === "reset") {
                continue;
            }
            if (element.closest('[data-wfc-ignore]')) {
                continue;
            }
            if (!addFormElements && getForm(element)) {
                continue;
            }
            if (getStreamPanel(element)) {
                continue;
            }
            if (!allowFileUploads && element.type === "file") {
                continue;
            }
            addElement(element, formData);
        }
    }
    function getFormData(form, eventTarget, eventArgument, allowFileUploads = true) {
        let formData;
        if (form) {
            if (form.tagName === "FORM" && allowFileUploads) {
                formData = new FormData(form);
            }
            else {
                formData = new FormData();
                addInputs(formData, form, true, allowFileUploads);
            }
        }
        else {
            formData = new FormData();
        }
        addInputs(formData, document.body, false, allowFileUploads);
        if (eventTarget) {
            formData.append("wfcTarget", eventTarget);
        }
        if (eventArgument) {
            formData.append("wfcArgument", eventArgument);
        }
        return formData;
    }
    async function submitForm(element, form, eventTarget, eventArgument) {
        var _a;
        const baseElement = element.closest('[data-wfc-base]');
        let target;
        if (form && form.getAttribute('data-wfc-form') === 'self') {
            target = form;
        }
        else if (baseElement) {
            target = baseElement;
        }
        else {
            target = document.body;
        }
        const url = (_a = baseElement === null || baseElement === void 0 ? void 0 : baseElement.getAttribute('data-wfc-base')) !== null && _a !== void 0 ? _a : location.toString();
        const formData = getFormData(form, eventTarget, eventArgument);
        const container = new ViewStateContainer(form, formData);
        if (!wfc.validate(element)) {
            return;
        }
        pendingPostbacks++;
        const release = await postbackMutex.acquire();
        const interceptors = [];
        try {
            const cancelled = !target.dispatchEvent(new CustomEvent("wfc:beforeSubmit", {
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
            }));
            if (cancelled) {
                return;
            }
            const request = {
                method: "POST",
                redirect: "error",
                credentials: "include",
                headers: {
                    'X-IsPostback': 'true',
                }
            };
            request.body = hasElementFile(document.body) ? formData : new URLSearchParams(formData);
            for (const interceptor of interceptors) {
                const result = interceptor(request);
                if (result instanceof Promise) {
                    await result;
                }
            }
            let response;
            try {
                response = await fetch(url, request);
            }
            catch (e) {
                target.dispatchEvent(new CustomEvent("wfc:submitError", {
                    bubbles: true,
                    detail: {
                        form,
                        eventTarget,
                        response: undefined,
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
                        response: response,
                        error: undefined
                    }
                }));
                throw new Error(response.statusText);
            }
            const redirectTo = response.headers.get('x-redirect-to');
            if (redirectTo) {
                window.location.assign(redirectTo);
                return;
            }
            const contentDisposition = response.headers.get('content-disposition');
            if (response.status === 204) {
                // No Content
            }
            else if (response.ok && contentDisposition && contentDisposition.indexOf('attachment') !== -1) {
                // noinspection ES6MissingAwait
                receiveFile(element, response, contentDisposition);
            }
            else {
                const text = await response.text();
                const options = getMorpdomSettings(form);
                const parser = new DOMParser();
                const htmlDoc = parser.parseFromString(sanitise(text), 'text/html');
                if (form && form.getAttribute('data-wfc-form') === 'self') {
                    morphdom(form, htmlDoc.querySelector('[data-wfc-form]'), options);
                }
                else if (baseElement) {
                    morphdom(baseElement, htmlDoc.querySelector('[data-wfc-base]'), options);
                }
                else {
                    morphdom(document.head, htmlDoc.querySelector('head'), options);
                    morphdom(document.body, htmlDoc.querySelector('body'), options);
                }
            }
        }
        finally {
            pendingPostbacks--;
            release();
            target.dispatchEvent(new CustomEvent("wfc:afterSubmit", { bubbles: true, detail: { target, container, form, eventTarget } }));
        }
    }
    async function receiveFile(element, response, contentDisposition) {
        var _a;
        document.dispatchEvent(new CustomEvent("wfc:beforeFileDownload", { detail: { element, response } }));
        try {
            const contentEncoding = response.headers.get('content-encoding');
            const contentLength = response.headers.get(contentEncoding ? 'x-file-size' : 'content-length');
            if (contentLength) {
                const total = parseInt(contentLength, 10);
                let loaded = 0;
                const reader = response.body.getReader();
                let cancelRequested = false;
                let onProgress = function (loaded, total) {
                    const percent = Math.round(loaded / total * 100);
                    document.dispatchEvent(new CustomEvent("wfc:progressFileDownload", { detail: { element, response, loaded, total, percent } }));
                };
                response = new Response(new ReadableStream({
                    start(controller) {
                        if (cancelRequested) ;
                        read();
                        function read() {
                            reader.read().then(({ done, value }) => {
                                if (done) {
                                    // ensure onProgress called when content-length=0
                                    if (total === 0) {
                                        onProgress(loaded, total);
                                    }
                                    controller.close();
                                    return;
                                }
                                loaded += value.byteLength;
                                onProgress(loaded, total);
                                controller.enqueue(value);
                                read();
                            }).catch(error => {
                                console.error(error);
                                controller.error(error);
                            });
                        }
                    }
                }));
            }
            const fileNameMatch = contentDisposition.match(/filename=(?:"([^"]+)"|([^;]+))/);
            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.style.display = 'none';
            if (fileNameMatch) {
                a.download = (_a = fileNameMatch[1]) !== null && _a !== void 0 ? _a : fileNameMatch[2];
            }
            else {
                a.download = "download";
            }
            document.body.appendChild(a);
            a.click();
            setTimeout(() => {
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            }, 0);
        }
        finally {
            document.dispatchEvent(new CustomEvent("wfc:afterFileDownload", { detail: { element, response } }));
        }
    }
    function getMorpdomSettings(form) {
        return {
            onNodeAdded(node) {
                document.dispatchEvent(new CustomEvent("wfc:addNode", { detail: { node, form } }));
                if (node.nodeType === Node.ELEMENT_NODE) {
                    document.dispatchEvent(new CustomEvent("wfc:addElement", { detail: { element: node, form } }));
                }
            },
            onBeforeElUpdated: function (fromEl, toEl) {
                if (!fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateNode", { cancelable: true, bubbles: true, detail: { node: fromEl, source: toEl, form } }))) {
                    return false;
                }
                if (fromEl.nodeType === Node.ELEMENT_NODE && !fromEl.dispatchEvent(new CustomEvent("wfc:beforeUpdateElement", { cancelable: true, bubbles: true, detail: { element: fromEl, source: toEl, form } }))) {
                    return false;
                }
                if (fromEl.hasAttribute('data-wfc-ignore') || toEl.hasAttribute('data-wfc-ignore')) {
                    return false;
                }
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
            onElUpdated(el) {
                if (el.nodeType === Node.ELEMENT_NODE) {
                    el.dispatchEvent(new CustomEvent("wfc:updateElement", { bubbles: true, detail: { element: el, form } }));
                }
            },
            onBeforeNodeDiscarded(node) {
                var _a, _b;
                if (node.tagName === "SCRIPT" || node.tagName === "STYLE" || node.tagName === "LINK" && node.hasAttribute('rel') && node.getAttribute('rel') === 'stylesheet') {
                    return false;
                }
                if (node instanceof Element && node.hasAttribute('data-wfc-form')) {
                    return false;
                }
                if (node.tagName === 'DIV' && node.hasAttribute('data-wfc-owner') && ((_a = node.getAttribute('data-wfc-owner')) !== null && _a !== void 0 ? _a : "") !== ((_b = form === null || form === void 0 ? void 0 : form.id) !== null && _b !== void 0 ? _b : "")) {
                    return false;
                }
                if (!node.dispatchEvent(new CustomEvent("wfc:discardNode", { bubbles: true, cancelable: true, detail: { node, form } }))) {
                    return false;
                }
                if (node.nodeType === Node.ELEMENT_NODE && !node.dispatchEvent(new CustomEvent("wfc:discardElement", { bubbles: true, cancelable: true, detail: { element: node, form } }))) {
                    return false;
                }
            }
        };
    }
    const originalSubmit = HTMLFormElement.prototype.submit;
    HTMLFormElement.prototype.submit = async function () {
        if (this.hasAttribute('data-wfc-form')) {
            await submitForm(this, this);
        }
        else {
            originalSubmit.call(this);
        }
    };
    document.addEventListener('submit', async function (e) {
        if (e.target instanceof Element && e.target.hasAttribute('data-wfc-form')) {
            e.preventDefault();
            await submitForm(e.target, e.target);
        }
    });
    document.addEventListener('click', async function (e) {
        var _a;
        if (!(e.target instanceof Element)) {
            return;
        }
        const postbackControl = (_a = e.target) === null || _a === void 0 ? void 0 : _a.closest("[data-wfc-postback]");
        if (!postbackControl) {
            return;
        }
        e.preventDefault();
        const wfcDisabled = postbackControl.getAttribute('data-wfc-disabled');
        if (wfcDisabled === "true") {
            return;
        }
        const eventTarget = postbackControl.getAttribute('data-wfc-postback');
        postBackElement(e.target, eventTarget);
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
        const eventTarget = e.target.getAttribute('name');
        e.preventDefault();
        await postBackElement(e.target, eventTarget, 'ENTER');
    });
    const timeouts = {};
    document.addEventListener('input', function (e) {
        if (!(e.target instanceof Element) || e.target.tagName !== "INPUT" || !e.target.hasAttribute('data-wfc-autopostback')) {
            return;
        }
        const type = e.target.getAttribute('type');
        if (type === "button" || type === "submit" || type === "reset") {
            return;
        }
        postBackChange(e.target);
    });
    function postBackChange(target, timeOut = 1000, eventArgument = 'CHANGE') {
        var _a, _b;
        const container = (_a = getStreamPanel(target)) !== null && _a !== void 0 ? _a : getForm(target);
        const eventTarget = target.getAttribute('name');
        const key = ((_b = container === null || container === void 0 ? void 0 : container.id) !== null && _b !== void 0 ? _b : '') + eventTarget + eventArgument;
        if (timeouts[key]) {
            pendingPostbacks--;
            clearTimeout(timeouts[key]);
        }
        pendingPostbacks++;
        timeouts[key] = setTimeout(async () => {
            pendingPostbacks--;
            delete timeouts[key];
            await postBackElement(target, eventTarget, eventArgument);
        }, timeOut);
    }
    function postBack(target, eventArgument) {
        const eventTarget = target.getAttribute('name');
        return postBackElement(target, eventTarget, eventArgument);
    }
    document.addEventListener('change', async function (e) {
        var _a, _b;
        if (e.target instanceof Element && e.target.hasAttribute('data-wfc-autopostback')) {
            const eventTarget = e.target.getAttribute('name');
            const container = (_a = getStreamPanel(e.target)) !== null && _a !== void 0 ? _a : getForm(e.target);
            const key = ((_b = container === null || container === void 0 ? void 0 : container.id) !== null && _b !== void 0 ? _b : '') + eventTarget;
            if (timeouts[key]) {
                clearTimeout(timeouts[key]);
            }
            setTimeout(() => postBackElement(e.target, eventTarget, 'CHANGE'), 10);
        }
    });
    const wfc = {
        hiddenClass: '',
        postBackChange,
        postBack,
        get hasPendingPostbacks() {
            return pendingPostbacks > 0;
        },
        init: function (arg) {
            arg();
        },
        show: function (element) {
            if (wfc.hiddenClass) {
                element.classList.remove(wfc.hiddenClass);
            }
            else {
                element.style.display = '';
            }
        },
        hide: function (element) {
            if (wfc.hiddenClass) {
                element.classList.add(wfc.hiddenClass);
            }
            else {
                element.style.display = 'none';
            }
        },
        toggle: function (element, value) {
            if (value) {
                wfc.show(element);
            }
            else {
                wfc.hide(element);
            }
        },
        validate: function (validationGroup = "") {
            var _a, _b;
            if (typeof validationGroup === "object") {
                if (!validationGroup.hasAttribute('data-wfc-validate')) {
                    return true;
                }
                validationGroup = (_a = validationGroup.getAttribute('data-wfc-validate')) !== null && _a !== void 0 ? _a : "";
            }
            const detail = { isValid: true };
            for (const element of document.querySelectorAll('[data-wfc-validate]')) {
                const elementValidationGroup = (_b = element.getAttribute('data-wfc-validate')) !== null && _b !== void 0 ? _b : "";
                if (elementValidationGroup !== validationGroup) {
                    continue;
                }
                element.dispatchEvent(new CustomEvent('wfc:validate', {
                    bubbles: true,
                    detail
                }));
            }
            return detail.isValid;
        },
        bind: async function (selectors, options) {
            var _a, _b, _c, _d, _e;
            const init = ((_a = options.init) !== null && _a !== void 0 ? _a : function () { }).bind(options);
            const update = ((_b = options.update) !== null && _b !== void 0 ? _b : function () { }).bind(options);
            const afterUpdate = ((_c = options.afterUpdate) !== null && _c !== void 0 ? _c : function () { }).bind(options);
            const submit = (_d = options.submit) === null || _d === void 0 ? void 0 : _d.bind(options);
            const destroy = (_e = options.destroy) === null || _e === void 0 ? void 0 : _e.bind(options);
            for (const element of document.querySelectorAll(selectors)) {
                await init(element);
                update(element, element);
                afterUpdate(element);
            }
            document.addEventListener('wfc:addElement', async function (e) {
                const { element } = e.detail;
                if (element.matches(selectors)) {
                    await init(element);
                    update(element, element);
                    afterUpdate(element);
                }
            });
            document.addEventListener('wfc:beforeUpdateElement', function (e) {
                const { element, source } = e.detail;
                if (element.matches(selectors) && update(element, source)) {
                    e.preventDefault();
                }
            });
            if (afterUpdate) {
                document.addEventListener('wfc:updateElement', function (e) {
                    const { element } = e.detail;
                    if (element.matches(selectors)) {
                        afterUpdate(element);
                    }
                });
            }
            if (submit) {
                document.addEventListener('wfc:beforeSubmit', function (e) {
                    const { container } = e.detail;
                    for (const element of container.querySelectorAll(selectors)) {
                        submit(element, container.formData);
                    }
                });
            }
            if (destroy) {
                document.addEventListener('wfc:discardElement', function (e) {
                    const { element } = e.detail;
                    if (element.matches(selectors)) {
                        destroy(element);
                        e.preventDefault();
                    }
                });
            }
        },
        bindValidator: function (selectors, validate) {
            wfc.bind(selectors, {
                init: function (element) {
                    element._isValid = true;
                },
                afterUpdate: function (element) {
                    // Restore old state
                    const isValidStr = element.getAttribute('data-wfc-validated');
                    if (isValidStr) {
                        element._isValid = isValidStr === 'true';
                    }
                    else {
                        wfc.toggle(element, !element._isValid);
                    }
                    // Bind to element
                    const idToValidate = element.getAttribute('data-wfc-validator');
                    if (!idToValidate) {
                        console.warn('No data-wfc-validator attribute found', element);
                        return;
                    }
                    const elementToValidate = document.getElementById(idToValidate);
                    if (element._elementToValidate === elementToValidate) {
                        return;
                    }
                    this.destroy(element);
                    element._elementToValidate = elementToValidate;
                    if (!elementToValidate) {
                        console.warn(`Element with id ${idToValidate} not found`);
                        return;
                    }
                    element._callback = function (e) {
                        const isValid = validate(elementToValidate, element);
                        element._isValid = isValid;
                        wfc.toggle(element, !isValid);
                        if (!isValid) {
                            e.detail.isValid = false;
                        }
                    };
                    elementToValidate.addEventListener('wfc:validate', element._callback);
                },
                destroy: function (element) {
                    if (element._callback && element._elementToValidate) {
                        element._elementToValidate.removeEventListener('wfc:validate', element._callback);
                        element._callback = undefined;
                        element._elementToValidate = undefined;
                    }
                }
            });
        }
    };
    // Stream
    wfc.bind('[data-wfc-stream]', {
        init: function (element) {
            const id = element.id;
            const baseElement = element.closest('[data-wfc-base]');
            const url = baseElement ? new URL(baseElement.getAttribute('data-wfc-base')) : location;
            let search = url.search;
            if (!search) {
                search = "?";
            }
            else {
                search += "&";
            }
            search += "__panel=" + id;
            const webSocket = new WebSocket((url.protocol === "https:" ? "wss://" : "ws://") + url.host + url.pathname + search);
            element.webSocket = webSocket;
            element.isUpdating = false;
            webSocket.addEventListener('message', function (e) {
                const parser = new DOMParser();
                const htmlDoc = parser.parseFromString(sanitise(`<!DOCTYPE html><html><body>${e.data}</body></html>`), 'text/html');
                element.isUpdating = true;
                morphdom(element, htmlDoc.getElementById(id), getMorpdomSettings());
                element.isUpdating = false;
            });
            webSocket.addEventListener('open', function () {
                const formData = getFormData(element);
                webSocket.send(new URLSearchParams(formData).toString());
            });
        },
        update: function (element, source) {
            if (!element.isUpdating) {
                return true;
            }
        },
        destroy: function (element) {
            const webSocket = element.webSocket;
            if (webSocket) {
                webSocket.close();
            }
        }
    });
    if ('wfc' in window) {
        const current = window.wfc;
        if ('hiddenClass' in current) {
            wfc.hiddenClass = current.hiddenClass;
        }
        window.wfc = wfc;
        if ('_' in current) {
            for (const bind of current._) {
                const [type, selector, func] = bind;
                if (type === 0) {
                    wfc.bind(selector, func);
                }
                else if (type === 1) {
                    wfc.bindValidator(selector, func);
                }
                else if (type === 2) {
                    wfc.init(func);
                }
                else {
                    console.warn('Unknown bind type', type);
                }
            }
        }
    }
    wfc.bindValidator('[data-wfc-requiredvalidator]', function (elementToValidate, element) {
        var _a, _b;
        const initialValue = ((_a = element.getAttribute('data-wfc-requiredvalidator')) !== null && _a !== void 0 ? _a : "").trim();
        const value = ((_b = elementToValidate.value) !== null && _b !== void 0 ? _b : "").trim();
        return initialValue !== value;
    });
    window.wfc = wfc;

})();
