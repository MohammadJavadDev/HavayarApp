
(function (factory) {
	if (typeof define === 'function' && define.amd) {
		// Loading from AMD script loader. Register as an anonymous module.
		define(['jquery'], factory);
	} else {
		// Browser using plain <script> tag
		factory(jQuery);
	}
}(function (jQuery) {
	var oldManip = jQuery.fn.domManip, tmplItmAtt = "_tmplitem", htmlExpr = /^[^<]*(<[\w\W]+>)[^>]*$|\{\{\! /,
		newTmplItems = {}, wrappedItems = {}, appendToTmplItems, topTmplItem = { key: 0, data: {} }, itemKey = 0, cloneIndex = 0, stack = [];

	function newTmplItem(options, parentItem, fn, data) {
		// Returns a template item data structure for a new rendered instance of a template (a 'template item').
		// The content field is a hierarchical array of strings and nested items (to be
		// removed and replaced by nodes field of dom elements, once inserted in DOM).
		var newItem = {
			data: data || (data === 0 || data === false) ? data : (parentItem ? parentItem.data : {}),
			_wrap: parentItem ? parentItem._wrap : null,
			tmpl: null,
			parent: parentItem || null,
			nodes: [],
			calls: tiCalls,
			nest: tiNest,
			wrap: tiWrap,
			html: tiHtml,
			update: tiUpdate
		};
		if (options) {
			jQuery.extend(newItem, options, { nodes: [], parent: parentItem });
		}
		if (fn) {
			// Build the hierarchical content to be used during insertion into DOM
			newItem.tmpl = fn;
			newItem._ctnt = newItem._ctnt || newItem.tmpl(jQuery, newItem);
			newItem.key = ++itemKey;
			// Keep track of new template item, until it is stored as jQuery Data on DOM element
			(stack.length ? wrappedItems : newTmplItems)[itemKey] = newItem;
		}
		return newItem;
	}

	// Override appendTo etc., in order to provide support for targeting multiple elements. (This code would disappear if integrated in jquery core).
	jQuery.each({
		appendTo: "append",
		prependTo: "prepend",
		insertBefore: "before",
		insertAfter: "after",
		replaceAll: "replaceWith"
	}, function (name, original) {
		jQuery.fn[name] = function (selector) {
			var ret = [], insert = jQuery(selector), elems, i, l, tmplItems,
				parent = this.length === 1 && this[0].parentNode;

			appendToTmplItems = newTmplItems || {};
			if (parent && parent.nodeType === 11 && parent.childNodes.length === 1 && insert.length === 1) {
				insert[original](this[0]);
				ret = this;
			} else {
				for (i = 0, l = insert.length; i < l; i++) {
					cloneIndex = i;
					elems = (i > 0 ? this.clone(true) : this).get();
					jQuery(insert[i])[original](elems);
					ret = ret.concat(elems);
				}
				cloneIndex = 0;
				ret = this.pushStack(ret, name, insert.selector);
			}
			tmplItems = appendToTmplItems;
			appendToTmplItems = null;
			jQuery.tmpl.complete(tmplItems);
			return ret;
		};
	});

	jQuery.fn.extend({
		// Use first wrapped element as template markup.
		// Return wrapped set of template items, obtained by rendering template against data.
		tmpl: function (data, options, parentItem) {
			return jQuery.tmpl(this[0], data, options, parentItem);
		},

		// Find which rendered template item the first wrapped DOM element belongs to
		tmplItem: function () {
			return jQuery.tmplItem(this[0]);
		},

		// Consider the first wrapped element as a template declaration, and get the compiled template or store it as a named template.
		template: function (name) {
			return jQuery.template(name, this[0]);
		},

		domManip: function (args, table, callback, options) {
			if (args[0] && jQuery.isArray(args[0])) {
				var dmArgs = jQuery.makeArray(arguments), elems = args[0], elemsLength = elems.length, i = 0, tmplItem;
				while (i < elemsLength && !(tmplItem = jQuery.data(elems[i++], "tmplItem"))) { }
				if (tmplItem && cloneIndex) {
					dmArgs[2] = function (fragClone) {
						// Handler called by oldManip when rendered template has been inserted into DOM.
						jQuery.tmpl.afterManip(this, fragClone, callback);
					};
				}
				oldManip.apply(this, dmArgs);
			} else {
				oldManip.apply(this, arguments);
			}
			cloneIndex = 0;
			if (!appendToTmplItems) {
				jQuery.tmpl.complete(newTmplItems);
			}
			return this;
		}
	});

	jQuery.extend({
		// Return wrapped set of template items, obtained by rendering template against data.
		tmpl: function (tmpl, data, options, parentItem) {
			var ret, topLevel = !parentItem;
			if (topLevel) {
				// This is a top-level tmpl call (not from a nested template using {{tmpl}})
				parentItem = topTmplItem;
				tmpl = jQuery.template[tmpl] || jQuery.template(null, tmpl);
				wrappedItems = {}; // Any wrapped items will be rebuilt, since this is top level
			} else if (!tmpl) {
				// The template item is already associated with DOM - this is a refresh.
				// Re-evaluate rendered template for the parentItem
				tmpl = parentItem.tmpl;
				newTmplItems[parentItem.key] = parentItem;
				parentItem.nodes = [];
				if (parentItem.wrapped) {
					updateWrapped(parentItem, parentItem.wrapped);
				}
				// Rebuild, without creating a new template item
				return jQuery(build(parentItem, null, parentItem.tmpl(jQuery, parentItem)));
			}
			if (!tmpl) {
				return []; // Could throw...
			}
			if (typeof data === "function") {
				data = data.call(parentItem || {});
			}
			if (options && options.wrapped) {
				updateWrapped(options, options.wrapped);
			}
			ret = jQuery.isArray(data) ?
				jQuery.map(data, function (dataItem) {
					return dataItem ? newTmplItem(options, parentItem, tmpl, dataItem) : null;
				}) :
				[newTmplItem(options, parentItem, tmpl, data)];
			return topLevel ? jQuery(build(parentItem, null, ret)) : ret;
		},

		// Return rendered template item for an element.
		tmplItem: function (elem) {
			var tmplItem;
			if (elem instanceof jQuery) {
				elem = elem[0];
			}
			while (elem && elem.nodeType === 1 && !(tmplItem = jQuery.data(elem, "tmplItem")) && (elem = elem.parentNode)) { }
			return tmplItem || topTmplItem;
		},

		// Set:
		// Use $.template( name, tmpl ) to cache a named template,
		// where tmpl is a template string, a script element or a jQuery instance wrapping a script element, etc.
		// Use $( "selector" ).template( name ) to provide access by name to a script block template declaration.

		// Get:
		// Use $.template( name ) to access a cached template.
		// Also $( selectorToScriptBlock ).template(), or $.template( null, templateString )
		// will return the compiled template, without adding a name reference.
		// If templateString includes at least one HTML tag, $.template( templateString ) is equivalent
		// to $.template( null, templateString )
		template: function (name, tmpl) {
			if (tmpl) {
				// Compile template and associate with name
				if (typeof tmpl === "string") {
					// This is an HTML string being passed directly in.
					tmpl = buildTmplFn(tmpl);
				} else if (tmpl instanceof jQuery) {
					tmpl = tmpl[0] || {};
				}
				if (tmpl.nodeType) {
					// If this is a template block, use cached copy, or generate tmpl function and cache.
					tmpl = jQuery.data(tmpl, "tmpl") || jQuery.data(tmpl, "tmpl", buildTmplFn(tmpl.innerHTML));
					// Issue: In IE, if the container element is not a script block, the innerHTML will remove quotes from attribute values whenever the value does not include white space.
					// This means that foo="${x}" will not work if the value of x includes white space: foo="${x}" -> foo=value of x.
					// To correct this, include space in tag: foo="${ x }" -> foo="value of x"
				}
				return typeof name === "string" ? (jQuery.template[name] = tmpl) : tmpl;
			}
			// Return named compiled template
			return name ? (typeof name !== "string" ? jQuery.template(null, name) :
				(jQuery.template[name] ||
					// If not in map, and not containing at least on HTML tag, treat as a selector.
					// (If integrated with core, use quickExpr.exec)
					jQuery.template(null, htmlExpr.test(name) ? name : jQuery(name)))) : null;
		},

		encode: function (text) {
			// Do HTML encoding replacing < > & and ' and " by corresponding entities.
			return ("" + text).split("<").join("&lt;").split(">").join("&gt;").split('"').join("&#34;").split("'").join("&#39;");
		}
	});

	jQuery.extend(jQuery.tmpl, {
		tag: {
			"tmpl": {
				_default: { $2: "null" },
				open: "if($notnull_1){__=__.concat($item.nest($1,$2));}"
				// tmpl target parameter can be of type function, so use $1, not $1a (so not auto detection of functions)
				// This means that {{tmpl foo}} treats foo as a template (which IS a function).
				// Explicit parens can be used if foo is a function that returns a template: {{tmpl foo()}}.
			},
			"wrap": {
				_default: { $2: "null" },
				open: "$item.calls(__,$1,$2);__=[];",
				close: "call=$item.calls();__=call._.concat($item.wrap(call,__));"
			},
			"each": {
				_default: { $2: "$index, $value" },
				open: "if($notnull_1){$.each($1a,function($2){with(this){",
				close: "}});}"
			},
			"if": {
				open: "if(($notnull_1) && $1a){",
				close: "}"
			},
			"else": {
				_default: { $1: "true" },
				open: "}else if(($notnull_1) && $1a){"
			},
			"html": {
				// Unecoded expression evaluation.
				open: "if($notnull_1){__.push($1a);}"
			},
			"=": {
				// Encoded expression evaluation. Abbreviated form is ${}.
				_default: { $1: "$data" },
				open: "if($notnull_1){__.push($.encode($1a));}"
			},
			"!": {
				// Comment tag. Skipped by parser
				open: ""
			}
		},

		// This stub can be overridden, e.g. in jquery.tmplPlus for providing rendered events
		complete: function (items) {
			newTmplItems = {};
		},

		// Call this from code which overrides domManip, or equivalent
		// Manage cloning/storing template items etc.
		afterManip: function afterManip(elem, fragClone, callback) {
			// Provides cloned fragment ready for fixup prior to and after insertion into DOM
			var content = fragClone.nodeType === 11 ?
				jQuery.makeArray(fragClone.childNodes) :
				fragClone.nodeType === 1 ? [fragClone] : [];

			// Return fragment to original caller (e.g. append) for DOM insertion
			callback.call(elem, fragClone);

			// Fragment has been inserted:- Add inserted nodes to tmplItem data structure. Replace inserted element annotations by jQuery.data.
			storeTmplItems(content);
			cloneIndex++;
		}
	});

	//========================== Private helper functions, used by code above ==========================

	function build(tmplItem, nested, content) {
		// Convert hierarchical content into flat string array
		// and finally return array of fragments ready for DOM insertion
		var frag, ret = content ? jQuery.map(content, function (item) {
			return (typeof item === "string") ?
				// Insert template item annotations, to be converted to jQuery.data( "tmplItem" ) when elems are inserted into DOM.
				(tmplItem.key ? item.replace(/(<\w+)(?=[\s>])(?![^>]*_tmplitem)([^>]*)/g, "$1 " + tmplItmAtt + "=\"" + tmplItem.key + "\" $2") : item) :
				// This is a child template item. Build nested template.
				build(item, tmplItem, item._ctnt);
		}) :
			// If content is not defined, insert tmplItem directly. Not a template item. May be a string, or a string array, e.g. from {{html $item.html()}}.
			tmplItem;
		if (nested) {
			return ret;
		}

		// top-level template
		ret = ret.join("");

		// Support templates which have initial or final text nodes, or consist only of text
		// Also support HTML entities within the HTML markup.
		ret.replace(/^\s*([^<\s][^<]*)?(<[\w\W]+>)([^>]*[^>\s])?\s*$/, function (all, before, middle, after) {
			frag = jQuery(middle).get();

			storeTmplItems(frag);
			if (before) {
				frag = unencode(before).concat(frag);
			}
			if (after) {
				frag = frag.concat(unencode(after));
			}
		});
		return frag ? frag : unencode(ret);
	}

	function unencode(text) {
		// Use createElement, since createTextNode will not render HTML entities correctly
		var el = document.createElement("div");
		el.innerHTML = text;
		return jQuery.makeArray(el.childNodes);
	}

	// Generate a reusable function that will serve to render a template against data
	function buildTmplFn(markup) {
		return new Function("jQuery", "$item",
			// Use the variable __ to hold a string array while building the compiled template. (See https://github.com/jquery/jquery-tmpl/issues#issue/10).
			"var $=jQuery,call,__=[],$data=$item.data;" +

			// Introduce the data as local variables using with(){}
			"with($data){__.push('" +

			// Convert the template into pure JavaScript
			jQuery.trim(markup)
				.replace(/([\\'])/g, "\\$1")
				.replace(/[\r\t\n]/g, " ")
				.replace(/\$\{([^\}]*)\}/g, "{{= $1}}")
				.replace(/\{\{(\/?)(\w+|.)(?:\(((?:[^\}]|\}(?!\}))*?)?\))?(?:\s+(.*?)?)?(\(((?:[^\}]|\}(?!\}))*?)\))?\s*\}\}/g,
					function (all, slash, type, fnargs, target, parens, args) {
						var tag = jQuery.tmpl.tag[type], def, expr, exprAutoFnDetect;
						if (!tag) {
							throw "Unknown template tag: " + type;
						}
						def = tag._default || [];
						if (parens && !/\w$/.test(target)) {
							target += parens;
							parens = "";
						}
						if (target) {
							target = unescape(target);
							args = args ? ("," + unescape(args) + ")") : (parens ? ")" : "");
							// Support for target being things like a.toLowerCase();
							// In that case don't call with template item as 'this' pointer. Just evaluate...
							expr = parens ? (target.indexOf(".") > -1 ? target + unescape(parens) : ("(" + target + ").call($item" + args)) : target;
							exprAutoFnDetect = parens ? expr : "(typeof(" + target + ")==='function'?(" + target + ").call($item):(" + target + "))";
						} else {
							exprAutoFnDetect = expr = def.$1 || "null";
						}
						fnargs = unescape(fnargs);
						return "');" +
							tag[slash ? "close" : "open"]
								.split("$notnull_1").join(target ? "typeof(" + target + ")!=='undefined' && (" + target + ")!=null" : "true")
								.split("$1a").join(exprAutoFnDetect)
								.split("$1").join(expr)
								.split("$2").join(fnargs || def.$2 || "") +
							"__.push('";
					}) +
			"');}return __;"
		);
	}
	function updateWrapped(options, wrapped) {
		// Build the wrapped content.
		options._wrap = build(options, true,
			// Suport imperative scenario in which options.wrapped can be set to a selector or an HTML string.
			jQuery.isArray(wrapped) ? wrapped : [htmlExpr.test(wrapped) ? wrapped : jQuery(wrapped).html()]
		).join("");
	}

	function unescape(args) {
		return args ? args.replace(/\\'/g, "'").replace(/\\\\/g, "\\") : null;
	}
	function outerHtml(elem) {
		var div = document.createElement("div");
		div.appendChild(elem.cloneNode(true));
		return div.innerHTML;
	}

	// Store template items in jQuery.data(), ensuring a unique tmplItem data data structure for each rendered template instance.
	function storeTmplItems(content) {
		var keySuffix = "_" + cloneIndex, elem, elems, newClonedItems = {}, i, l, m;
		for (i = 0, l = content.length; i < l; i++) {
			if ((elem = content[i]).nodeType !== 1) {
				continue;
			}
			elems = elem.getElementsByTagName("*");
			for (m = elems.length - 1; m >= 0; m--) {
				processItemKey(elems[m]);
			}
			processItemKey(elem);
		}
		function processItemKey(el) {
			var pntKey, pntNode = el, pntItem, tmplItem, key;
			// Ensure that each rendered template inserted into the DOM has its own template item,
			if ((key = el.getAttribute(tmplItmAtt))) {
				while (pntNode.parentNode && (pntNode = pntNode.parentNode).nodeType === 1 && !(pntKey = pntNode.getAttribute(tmplItmAtt))) { }
				if (pntKey !== key) {
					// The next ancestor with a _tmplitem expando is on a different key than this one.
					// So this is a top-level element within this template item
					// Set pntNode to the key of the parentNode, or to 0 if pntNode.parentNode is null, or pntNode is a fragment.
					pntNode = pntNode.parentNode ? (pntNode.nodeType === 11 ? 0 : (pntNode.getAttribute(tmplItmAtt) || 0)) : 0;
					if (!(tmplItem = newTmplItems[key])) {
						// The item is for wrapped content, and was copied from the temporary parent wrappedItem.
						tmplItem = wrappedItems[key];
						tmplItem = newTmplItem(tmplItem, newTmplItems[pntNode] || wrappedItems[pntNode]);
						tmplItem.key = ++itemKey;
						newTmplItems[itemKey] = tmplItem;
					}
					if (cloneIndex) {
						cloneTmplItem(key);
					}
				}
				el.removeAttribute(tmplItmAtt);
			} else if (cloneIndex && (tmplItem = jQuery.data(el, "tmplItem"))) {
				// This was a rendered element, cloned during append or appendTo etc.
				// TmplItem stored in jQuery data has already been cloned in cloneCopyEvent. We must replace it with a fresh cloned tmplItem.
				cloneTmplItem(tmplItem.key);
				newTmplItems[tmplItem.key] = tmplItem;
				pntNode = jQuery.data(el.parentNode, "tmplItem");
				pntNode = pntNode ? pntNode.key : 0;
			}
			if (tmplItem) {
				pntItem = tmplItem;
				// Find the template item of the parent element.
				// (Using !=, not !==, since pntItem.key is number, and pntNode may be a string)
				while (pntItem && pntItem.key != pntNode) {
					// Add this element as a top-level node for this rendered template item, as well as for any
					// ancestor items between this item and the item of its parent element
					pntItem.nodes.push(el);
					pntItem = pntItem.parent;
				}
				// Delete content built during rendering - reduce API surface area and memory use, and avoid exposing of stale data after rendering...
				delete tmplItem._ctnt;
				delete tmplItem._wrap;
				// Store template item as jQuery data on the element
				jQuery.data(el, "tmplItem", tmplItem);
			}
			function cloneTmplItem(key) {
				key = key + keySuffix;
				tmplItem = newClonedItems[key] =
					(newClonedItems[key] || newTmplItem(tmplItem, newTmplItems[tmplItem.parent.key + keySuffix] || tmplItem.parent));
			}
		}
	}

	//---- Helper functions for template item ----

	function tiCalls(content, tmpl, data, options) {
		if (!content) {
			return stack.pop();
		}
		stack.push({ _: content, tmpl: tmpl, item: this, data: data, options: options });
	}

	function tiNest(tmpl, data, options) {
		// nested template, using {{tmpl}} tag
		return jQuery.tmpl(jQuery.template(tmpl), data, options, this);
	}

	function tiWrap(call, wrapped) {
		// nested template, using {{wrap}} tag
		var options = call.options || {};
		options.wrapped = wrapped;
		// Apply the template, which may incorporate wrapped content,
		return jQuery.tmpl(jQuery.template(call.tmpl), call.data, options, call.item);
	}

	function tiHtml(filter, textOnly) {
		var wrapped = this._wrap;
		return jQuery.map(
			jQuery(jQuery.isArray(wrapped) ? wrapped.join("") : wrapped).filter(filter || "*"),
			function (e) {
				return textOnly ?
					e.innerText || e.textContent :
					e.outerHTML || outerHtml(e);
			});
	}

	function tiUpdate() {
		var coll = this.nodes;
		jQuery.tmpl(null, null, null, this).insertBefore(coll[0]);
		jQuery(coll).remove();
	}

}));



/*!
* sweetalert2 v11.14.1
* Released under the MIT License.
*/
!function (e, t) { "object" == typeof exports && "undefined" != typeof module ? module.exports = t() : "function" == typeof define && define.amd ? define(t) : (e = "undefined" != typeof globalThis ? globalThis : e || self).Sweetalert2 = t() }(this, (function () { "use strict"; function e(e, t, n) { if ("function" == typeof e ? e === t : e.has(t)) return arguments.length < 3 ? t : n; throw new TypeError("Private element is not present on this object") } function t(t, n) { return t.get(e(t, n)) } function n(e, t, n) { (function (e, t) { if (t.has(e)) throw new TypeError("Cannot initialize the same private elements twice on an object") })(e, t), t.set(e, n) } const o = {}, i = e => new Promise((t => { if (!e) return t(); const n = window.scrollX, i = window.scrollY; o.restoreFocusTimeout = setTimeout((() => { o.previousActiveElement instanceof HTMLElement ? (o.previousActiveElement.focus(), o.previousActiveElement = null) : document.body && document.body.focus(), t() }), 100), window.scrollTo(n, i) })), s = "swal2-", r = ["container", "shown", "height-auto", "iosfix", "popup", "modal", "no-backdrop", "no-transition", "toast", "toast-shown", "show", "hide", "close", "title", "html-container", "actions", "confirm", "deny", "cancel", "default-outline", "footer", "icon", "icon-content", "image", "input", "file", "range", "select", "radio", "checkbox", "label", "textarea", "inputerror", "input-label", "validation-message", "progress-steps", "active-progress-step", "progress-step", "progress-step-line", "loader", "loading", "styled", "top", "top-start", "top-end", "top-left", "top-right", "center", "center-start", "center-end", "center-left", "center-right", "bottom", "bottom-start", "bottom-end", "bottom-left", "bottom-right", "grow-row", "grow-column", "grow-fullscreen", "rtl", "timer-progress-bar", "timer-progress-bar-container", "scrollbar-measure", "icon-success", "icon-warning", "icon-info", "icon-question", "icon-error"].reduce(((e, t) => (e[t] = s + t, e)), {}), a = ["success", "warning", "info", "question", "error"].reduce(((e, t) => (e[t] = s + t, e)), {}), l = "SweetAlert2:", c = e => e.charAt(0).toUpperCase() + e.slice(1), u = e => { console.warn(`${l} ${"object" == typeof e ? e.join(" ") : e}`) }, d = e => { console.error(`${l} ${e}`) }, p = [], m = function (e) { let t = arguments.length > 1 && void 0 !== arguments[1] ? arguments[1] : null; var n; n = `"${e}" is deprecated and will be removed in the next major release.${t ? ` Use "${t}" instead.` : ""}`, p.includes(n) || (p.push(n), u(n)) }, h = e => "function" == typeof e ? e() : e, g = e => e && "function" == typeof e.toPromise, f = e => g(e) ? e.toPromise() : Promise.resolve(e), b = e => e && Promise.resolve(e) === e, y = () => document.body.querySelector(`.${r.container}`), w = e => { const t = y(); return t ? t.querySelector(e) : null }, v = e => w(`.${e}`), C = () => v(r.popup), A = () => v(r.icon), k = () => v(r.title), E = () => v(r["html-container"]), B = () => v(r.image), $ = () => v(r["progress-steps"]), P = () => v(r["validation-message"]), x = () => w(`.${r.actions} .${r.confirm}`), T = () => w(`.${r.actions} .${r.cancel}`), L = () => w(`.${r.actions} .${r.deny}`), S = () => w(`.${r.loader}`), O = () => v(r.actions), M = () => v(r.footer), j = () => v(r["timer-progress-bar"]), H = () => v(r.close), I = () => { const e = C(); if (!e) return []; const t = e.querySelectorAll('[tabindex]:not([tabindex="-1"]):not([tabindex="0"])'), n = Array.from(t).sort(((e, t) => { const n = parseInt(e.getAttribute("tabindex") || "0"), o = parseInt(t.getAttribute("tabindex") || "0"); return n > o ? 1 : n < o ? -1 : 0 })), o = e.querySelectorAll('\n  a[href],\n  area[href],\n  input:not([disabled]),\n  select:not([disabled]),\n  textarea:not([disabled]),\n  button:not([disabled]),\n  iframe,\n  object,\n  embed,\n  [tabindex="0"],\n  [contenteditable],\n  audio[controls],\n  video[controls],\n  summary\n'), i = Array.from(o).filter((e => "-1" !== e.getAttribute("tabindex"))); return [...new Set(n.concat(i))].filter((e => ee(e))) }, D = () => N(document.body, r.shown) && !N(document.body, r["toast-shown"]) && !N(document.body, r["no-backdrop"]), q = () => { const e = C(); return !!e && N(e, r.toast) }, V = (e, t) => { if (e.textContent = "", t) { const n = (new DOMParser).parseFromString(t, "text/html"), o = n.querySelector("head"); o && Array.from(o.childNodes).forEach((t => { e.appendChild(t) })); const i = n.querySelector("body"); i && Array.from(i.childNodes).forEach((t => { t instanceof HTMLVideoElement || t instanceof HTMLAudioElement ? e.appendChild(t.cloneNode(!0)) : e.appendChild(t) })) } }, N = (e, t) => { if (!t) return !1; const n = t.split(/\s+/); for (let t = 0; t < n.length; t++)if (!e.classList.contains(n[t])) return !1; return !0 }, _ = (e, t, n) => { if (((e, t) => { Array.from(e.classList).forEach((n => { Object.values(r).includes(n) || Object.values(a).includes(n) || Object.values(t.showClass || {}).includes(n) || e.classList.remove(n) })) })(e, t), !t.customClass) return; const o = t.customClass[n]; o && ("string" == typeof o || o.forEach ? z(e, o) : u(`Invalid type of customClass.${n}! Expected string or iterable object, got "${typeof o}"`)) }, F = (e, t) => { if (!t) return null; switch (t) { case "select": case "textarea": case "file": return e.querySelector(`.${r.popup} > .${r[t]}`); case "checkbox": return e.querySelector(`.${r.popup} > .${r.checkbox} input`); case "radio": return e.querySelector(`.${r.popup} > .${r.radio} input:checked`) || e.querySelector(`.${r.popup} > .${r.radio} input:first-child`); case "range": return e.querySelector(`.${r.popup} > .${r.range} input`); default: return e.querySelector(`.${r.popup} > .${r.input}`) } }, R = e => { if (e.focus(), "file" !== e.type) { const t = e.value; e.value = "", e.value = t } }, U = (e, t, n) => { e && t && ("string" == typeof t && (t = t.split(/\s+/).filter(Boolean)), t.forEach((t => { Array.isArray(e) ? e.forEach((e => { n ? e.classList.add(t) : e.classList.remove(t) })) : n ? e.classList.add(t) : e.classList.remove(t) }))) }, z = (e, t) => { U(e, t, !0) }, K = (e, t) => { U(e, t, !1) }, W = (e, t) => { const n = Array.from(e.children); for (let e = 0; e < n.length; e++) { const o = n[e]; if (o instanceof HTMLElement && N(o, t)) return o } }, Y = (e, t, n) => { n === `${parseInt(n)}` && (n = parseInt(n)), n || 0 === parseInt(n) ? e.style.setProperty(t, "number" == typeof n ? `${n}px` : n) : e.style.removeProperty(t) }, Z = function (e) { let t = arguments.length > 1 && void 0 !== arguments[1] ? arguments[1] : "flex"; e && (e.style.display = t) }, J = e => { e && (e.style.display = "none") }, X = function (e) { let t = arguments.length > 1 && void 0 !== arguments[1] ? arguments[1] : "block"; e && new MutationObserver((() => { Q(e, e.innerHTML, t) })).observe(e, { childList: !0, subtree: !0 }) }, G = (e, t, n, o) => { const i = e.querySelector(t); i && i.style.setProperty(n, o) }, Q = function (e, t) { t ? Z(e, arguments.length > 2 && void 0 !== arguments[2] ? arguments[2] : "flex") : J(e) }, ee = e => !(!e || !(e.offsetWidth || e.offsetHeight || e.getClientRects().length)), te = e => !!(e.scrollHeight > e.clientHeight), ne = e => { const t = window.getComputedStyle(e), n = parseFloat(t.getPropertyValue("animation-duration") || "0"), o = parseFloat(t.getPropertyValue("transition-duration") || "0"); return n > 0 || o > 0 }, oe = function (e) { let t = arguments.length > 1 && void 0 !== arguments[1] && arguments[1]; const n = j(); n && ee(n) && (t && (n.style.transition = "none", n.style.width = "100%"), setTimeout((() => { n.style.transition = `width ${e / 1e3}s linear`, n.style.width = "0%" }), 10)) }, ie = () => "undefined" == typeof window || "undefined" == typeof document, se = `\n <div aria-labelledby="${r.title}" aria-describedby="${r["html-container"]}" class="${r.popup}" tabindex="-1">\n   <button type="button" class="${r.close}"></button>\n   <ul class="${r["progress-steps"]}"></ul>\n   <div class="${r.icon}"></div>\n   <img class="${r.image}" />\n   <h2 class="${r.title}" id="${r.title}"></h2>\n   <div class="${r["html-container"]}" id="${r["html-container"]}"></div>\n   <input class="${r.input}" id="${r.input}" />\n   <input type="file" class="${r.file}" />\n   <div class="${r.range}">\n     <input type="range" />\n     <output></output>\n   </div>\n   <select class="${r.select}" id="${r.select}"></select>\n   <div class="${r.radio}"></div>\n   <label class="${r.checkbox}">\n     <input type="checkbox" id="${r.checkbox}" />\n     <span class="${r.label}"></span>\n   </label>\n   <textarea class="${r.textarea}" id="${r.textarea}"></textarea>\n   <div class="${r["validation-message"]}" id="${r["validation-message"]}"></div>\n   <div class="${r.actions}">\n     <div class="${r.loader}"></div>\n     <button type="button" class="${r.confirm}"></button>\n     <button type="button" class="${r.deny}"></button>\n     <button type="button" class="${r.cancel}"></button>\n   </div>\n   <div class="${r.footer}"></div>\n   <div class="${r["timer-progress-bar-container"]}">\n     <div class="${r["timer-progress-bar"]}"></div>\n   </div>\n </div>\n`.replace(/(^|\n)\s*/g, ""), re = () => { o.currentInstance.resetValidationMessage() }, ae = e => { const t = (() => { const e = y(); return !!e && (e.remove(), K([document.documentElement, document.body], [r["no-backdrop"], r["toast-shown"], r["has-column"]]), !0) })(); if (ie()) return void d("SweetAlert2 requires document to initialize"); const n = document.createElement("div"); n.className = r.container, t && z(n, r["no-transition"]), V(n, se); const o = "string" == typeof (i = e.target) ? document.querySelector(i) : i; var i; o.appendChild(n), (e => { const t = C(); t.setAttribute("role", e.toast ? "alert" : "dialog"), t.setAttribute("aria-live", e.toast ? "polite" : "assertive"), e.toast || t.setAttribute("aria-modal", "true") })(e), (e => { "rtl" === window.getComputedStyle(e).direction && z(y(), r.rtl) })(o), (() => { const e = C(), t = W(e, r.input), n = W(e, r.file), o = e.querySelector(`.${r.range} input`), i = e.querySelector(`.${r.range} output`), s = W(e, r.select), a = e.querySelector(`.${r.checkbox} input`), l = W(e, r.textarea); t.oninput = re, n.onchange = re, s.onchange = re, a.onchange = re, l.oninput = re, o.oninput = () => { re(), i.value = o.value }, o.onchange = () => { re(), i.value = o.value } })() }, le = (e, t) => { e instanceof HTMLElement ? t.appendChild(e) : "object" == typeof e ? ce(e, t) : e && V(t, e) }, ce = (e, t) => { e.jquery ? ue(t, e) : V(t, e.toString()) }, ue = (e, t) => { if (e.textContent = "", 0 in t) for (let n = 0; n in t; n++)e.appendChild(t[n].cloneNode(!0)); else e.appendChild(t.cloneNode(!0)) }, de = (() => { if (ie()) return !1; const e = document.createElement("div"); return void 0 !== e.style.webkitAnimation ? "webkitAnimationEnd" : void 0 !== e.style.animation && "animationend" })(), pe = (e, t) => { const n = O(), o = S(); n && o && (t.showConfirmButton || t.showDenyButton || t.showCancelButton ? Z(n) : J(n), _(n, t, "actions"), function (e, t, n) { const o = x(), i = L(), s = T(); if (!o || !i || !s) return; me(o, "confirm", n), me(i, "deny", n), me(s, "cancel", n), function (e, t, n, o) { if (!o.buttonsStyling) return void K([e, t, n], r.styled); z([e, t, n], r.styled), o.confirmButtonColor && (e.style.backgroundColor = o.confirmButtonColor, z(e, r["default-outline"])); o.denyButtonColor && (t.style.backgroundColor = o.denyButtonColor, z(t, r["default-outline"])); o.cancelButtonColor && (n.style.backgroundColor = o.cancelButtonColor, z(n, r["default-outline"])) }(o, i, s, n), n.reverseButtons && (n.toast ? (e.insertBefore(s, o), e.insertBefore(i, o)) : (e.insertBefore(s, t), e.insertBefore(i, t), e.insertBefore(o, t))) }(n, o, t), V(o, t.loaderHtml || ""), _(o, t, "loader")) }; function me(e, t, n) { const o = c(t); Q(e, n[`show${o}Button`], "inline-block"), V(e, n[`${t}ButtonText`] || ""), e.setAttribute("aria-label", n[`${t}ButtonAriaLabel`] || ""), e.className = r[t], _(e, n, `${t}Button`) } const he = (e, t) => { const n = y(); n && (!function (e, t) { "string" == typeof t ? e.style.background = t : t || z([document.documentElement, document.body], r["no-backdrop"]) }(n, t.backdrop), function (e, t) { if (!t) return; t in r ? z(e, r[t]) : (u('The "position" parameter is not valid, defaulting to "center"'), z(e, r.center)) }(n, t.position), function (e, t) { if (!t) return; z(e, r[`grow-${t}`]) }(n, t.grow), _(n, t, "container")) }; var ge = { innerParams: new WeakMap, domCache: new WeakMap }; const fe = ["input", "file", "range", "select", "radio", "checkbox", "textarea"], be = e => { if (!e.input) return; if (!Ee[e.input]) return void d(`Unexpected type of input! Expected ${Object.keys(Ee).join(" | ")}, got "${e.input}"`); const t = Ae(e.input); if (!t) return; const n = Ee[e.input](t, e); Z(t), e.inputAutoFocus && setTimeout((() => { R(n) })) }, ye = (e, t) => { const n = C(); if (!n) return; const o = F(n, e); if (o) { (e => { for (let t = 0; t < e.attributes.length; t++) { const n = e.attributes[t].name;["id", "type", "value", "style"].includes(n) || e.removeAttribute(n) } })(o); for (const e in t) o.setAttribute(e, t[e]) } }, we = e => { if (!e.input) return; const t = Ae(e.input); t && _(t, e, "input") }, ve = (e, t) => { !e.placeholder && t.inputPlaceholder && (e.placeholder = t.inputPlaceholder) }, Ce = (e, t, n) => { if (n.inputLabel) { const o = document.createElement("label"), i = r["input-label"]; o.setAttribute("for", e.id), o.className = i, "object" == typeof n.customClass && z(o, n.customClass.inputLabel), o.innerText = n.inputLabel, t.insertAdjacentElement("beforebegin", o) } }, Ae = e => { const t = C(); if (t) return W(t, r[e] || r.input) }, ke = (e, t) => { ["string", "number"].includes(typeof t) ? e.value = `${t}` : b(t) || u(`Unexpected type of inputValue! Expected "string", "number" or "Promise", got "${typeof t}"`) }, Ee = {}; Ee.text = Ee.email = Ee.password = Ee.number = Ee.tel = Ee.url = Ee.search = Ee.date = Ee["datetime-local"] = Ee.time = Ee.week = Ee.month = (e, t) => (ke(e, t.inputValue), Ce(e, e, t), ve(e, t), e.type = t.input, e), Ee.file = (e, t) => (Ce(e, e, t), ve(e, t), e), Ee.range = (e, t) => { const n = e.querySelector("input"), o = e.querySelector("output"); return ke(n, t.inputValue), n.type = t.input, ke(o, t.inputValue), Ce(n, e, t), e }, Ee.select = (e, t) => { if (e.textContent = "", t.inputPlaceholder) { const n = document.createElement("option"); V(n, t.inputPlaceholder), n.value = "", n.disabled = !0, n.selected = !0, e.appendChild(n) } return Ce(e, e, t), e }, Ee.radio = e => (e.textContent = "", e), Ee.checkbox = (e, t) => { const n = F(C(), "checkbox"); n.value = "1", n.checked = Boolean(t.inputValue); const o = e.querySelector("span"); return V(o, t.inputPlaceholder || t.inputLabel), n }, Ee.textarea = (e, t) => { ke(e, t.inputValue), ve(e, t), Ce(e, e, t); return setTimeout((() => { if ("MutationObserver" in window) { const n = parseInt(window.getComputedStyle(C()).width); new MutationObserver((() => { if (!document.body.contains(e)) return; const o = e.offsetWidth + (i = e, parseInt(window.getComputedStyle(i).marginLeft) + parseInt(window.getComputedStyle(i).marginRight)); var i; o > n ? C().style.width = `${o}px` : Y(C(), "width", t.width) })).observe(e, { attributes: !0, attributeFilter: ["style"] }) } })), e }; const Be = (e, t) => { const n = E(); n && (X(n), _(n, t, "htmlContainer"), t.html ? (le(t.html, n), Z(n, "block")) : t.text ? (n.textContent = t.text, Z(n, "block")) : J(n), ((e, t) => { const n = C(); if (!n) return; const o = ge.innerParams.get(e), i = !o || t.input !== o.input; fe.forEach((e => { const o = W(n, r[e]); o && (ye(e, t.inputAttributes), o.className = r[e], i && J(o)) })), t.input && (i && be(t), we(t)) })(e, t)) }, $e = (e, t) => { for (const [n, o] of Object.entries(a)) t.icon !== n && K(e, o); z(e, t.icon && a[t.icon]), Te(e, t), Pe(), _(e, t, "icon") }, Pe = () => { const e = C(); if (!e) return; const t = window.getComputedStyle(e).getPropertyValue("background-color"), n = e.querySelectorAll("[class^=swal2-success-circular-line], .swal2-success-fix"); for (let e = 0; e < n.length; e++)n[e].style.backgroundColor = t }, xe = (e, t) => { if (!t.icon && !t.iconHtml) return; let n = e.innerHTML, o = ""; if (t.iconHtml) o = Le(t.iconHtml); else if ("success" === t.icon) o = '\n  <div class="swal2-success-circular-line-left"></div>\n  <span class="swal2-success-line-tip"></span> <span class="swal2-success-line-long"></span>\n  <div class="swal2-success-ring"></div> <div class="swal2-success-fix"></div>\n  <div class="swal2-success-circular-line-right"></div>\n', n = n.replace(/ style=".*?"/g, ""); else if ("error" === t.icon) o = '\n  <span class="swal2-x-mark">\n    <span class="swal2-x-mark-line-left"></span>\n    <span class="swal2-x-mark-line-right"></span>\n  </span>\n'; else if (t.icon) { o = Le({ question: "?", warning: "!", info: "i" }[t.icon]) } n.trim() !== o.trim() && V(e, o) }, Te = (e, t) => { if (t.iconColor) { e.style.color = t.iconColor, e.style.borderColor = t.iconColor; for (const n of [".swal2-success-line-tip", ".swal2-success-line-long", ".swal2-x-mark-line-left", ".swal2-x-mark-line-right"]) G(e, n, "background-color", t.iconColor); G(e, ".swal2-success-ring", "border-color", t.iconColor) } }, Le = e => `<div class="${r["icon-content"]}">${e}</div>`, Se = (e, t) => { const n = t.showClass || {}; e.className = `${r.popup} ${ee(e) ? n.popup : ""}`, t.toast ? (z([document.documentElement, document.body], r["toast-shown"]), z(e, r.toast)) : z(e, r.modal), _(e, t, "popup"), "string" == typeof t.customClass && z(e, t.customClass), t.icon && z(e, r[`icon-${t.icon}`]) }, Oe = e => { const t = document.createElement("li"); return z(t, r["progress-step"]), V(t, e), t }, Me = e => { const t = document.createElement("li"); return z(t, r["progress-step-line"]), e.progressStepsDistance && Y(t, "width", e.progressStepsDistance), t }, je = (e, t) => { ((e, t) => { const n = y(), o = C(); if (n && o) { if (t.toast) { Y(n, "width", t.width), o.style.width = "100%"; const e = S(); e && o.insertBefore(e, A()) } else Y(o, "width", t.width); Y(o, "padding", t.padding), t.color && (o.style.color = t.color), t.background && (o.style.background = t.background), J(P()), Se(o, t) } })(0, t), he(0, t), ((e, t) => { const n = $(); if (!n) return; const { progressSteps: o, currentProgressStep: i } = t; o && 0 !== o.length && void 0 !== i ? (Z(n), n.textContent = "", i >= o.length && u("Invalid currentProgressStep parameter, it should be less than progressSteps.length (currentProgressStep like JS arrays starts from 0)"), o.forEach(((e, s) => { const a = Oe(e); if (n.appendChild(a), s === i && z(a, r["active-progress-step"]), s !== o.length - 1) { const e = Me(t); n.appendChild(e) } }))) : J(n) })(0, t), ((e, t) => { const n = ge.innerParams.get(e), o = A(); if (o) { if (n && t.icon === n.icon) return xe(o, t), void $e(o, t); if (t.icon || t.iconHtml) { if (t.icon && -1 === Object.keys(a).indexOf(t.icon)) return d(`Unknown icon! Expected "success", "error", "warning", "info" or "question", got "${t.icon}"`), void J(o); Z(o), xe(o, t), $e(o, t), z(o, t.showClass && t.showClass.icon) } else J(o) } })(e, t), ((e, t) => { const n = B(); n && (t.imageUrl ? (Z(n, ""), n.setAttribute("src", t.imageUrl), n.setAttribute("alt", t.imageAlt || ""), Y(n, "width", t.imageWidth), Y(n, "height", t.imageHeight), n.className = r.image, _(n, t, "image")) : J(n)) })(0, t), ((e, t) => { const n = k(); n && (X(n), Q(n, t.title || t.titleText, "block"), t.title && le(t.title, n), t.titleText && (n.innerText = t.titleText), _(n, t, "title")) })(0, t), ((e, t) => { const n = H(); n && (V(n, t.closeButtonHtml || ""), _(n, t, "closeButton"), Q(n, t.showCloseButton), n.setAttribute("aria-label", t.closeButtonAriaLabel || "")) })(0, t), Be(e, t), pe(0, t), ((e, t) => { const n = M(); n && (X(n), Q(n, t.footer, "block"), t.footer && le(t.footer, n), _(n, t, "footer")) })(0, t); const n = C(); "function" == typeof t.didRender && n && t.didRender(n), o.eventEmitter.emit("didRender", n) }, He = () => { var e; return null === (e = x()) || void 0 === e ? void 0 : e.click() }, Ie = Object.freeze({ cancel: "cancel", backdrop: "backdrop", close: "close", esc: "esc", timer: "timer" }), De = e => { e.keydownTarget && e.keydownHandlerAdded && (e.keydownTarget.removeEventListener("keydown", e.keydownHandler, { capture: e.keydownListenerCapture }), e.keydownHandlerAdded = !1) }, qe = (e, t) => { var n; const o = I(); if (o.length) return (e += t) === o.length ? e = 0 : -1 === e && (e = o.length - 1), void o[e].focus(); null === (n = C()) || void 0 === n || n.focus() }, Ve = ["ArrowRight", "ArrowDown"], Ne = ["ArrowLeft", "ArrowUp"], _e = (e, t, n) => { e && (t.isComposing || 229 === t.keyCode || (e.stopKeydownPropagation && t.stopPropagation(), "Enter" === t.key ? Fe(t, e) : "Tab" === t.key ? Re(t) : [...Ve, ...Ne].includes(t.key) ? Ue(t.key) : "Escape" === t.key && ze(t, e, n))) }, Fe = (e, t) => { if (!h(t.allowEnterKey)) return; const n = F(C(), t.input); if (e.target && n && e.target instanceof HTMLElement && e.target.outerHTML === n.outerHTML) { if (["textarea", "file"].includes(t.input)) return; He(), e.preventDefault() } }, Re = e => { const t = e.target, n = I(); let o = -1; for (let e = 0; e < n.length; e++)if (t === n[e]) { o = e; break } e.shiftKey ? qe(o, -1) : qe(o, 1), e.stopPropagation(), e.preventDefault() }, Ue = e => { const t = O(), n = x(), o = L(), i = T(); if (!(t && n && o && i)) return; const s = [n, o, i]; if (document.activeElement instanceof HTMLElement && !s.includes(document.activeElement)) return; const r = Ve.includes(e) ? "nextElementSibling" : "previousElementSibling"; let a = document.activeElement; if (a) { for (let e = 0; e < t.children.length; e++) { if (a = a[r], !a) return; if (a instanceof HTMLButtonElement && ee(a)) break } a instanceof HTMLButtonElement && a.focus() } }, ze = (e, t, n) => { h(t.allowEscapeKey) && (e.preventDefault(), n(Ie.esc)) }; var Ke = { swalPromiseResolve: new WeakMap, swalPromiseReject: new WeakMap }; const We = () => { Array.from(document.body.children).forEach((e => { e.hasAttribute("data-previous-aria-hidden") ? (e.setAttribute("aria-hidden", e.getAttribute("data-previous-aria-hidden") || ""), e.removeAttribute("data-previous-aria-hidden")) : e.removeAttribute("aria-hidden") })) }, Ye = "undefined" != typeof window && !!window.GestureEvent, Ze = () => { const e = y(); if (!e) return; let t; e.ontouchstart = e => { t = Je(e) }, e.ontouchmove = e => { t && (e.preventDefault(), e.stopPropagation()) } }, Je = e => { const t = e.target, n = y(), o = E(); return !(!n || !o) && (!Xe(e) && !Ge(e) && (t === n || !te(n) && t instanceof HTMLElement && "INPUT" !== t.tagName && "TEXTAREA" !== t.tagName && (!te(o) || !o.contains(t)))) }, Xe = e => e.touches && e.touches.length && "stylus" === e.touches[0].touchType, Ge = e => e.touches && e.touches.length > 1; let Qe = null; const et = e => { null === Qe && (document.body.scrollHeight > window.innerHeight || "scroll" === e) && (Qe = parseInt(window.getComputedStyle(document.body).getPropertyValue("padding-right")), document.body.style.paddingRight = `${Qe + (() => { const e = document.createElement("div"); e.className = r["scrollbar-measure"], document.body.appendChild(e); const t = e.getBoundingClientRect().width - e.clientWidth; return document.body.removeChild(e), t })()}px`) }; function tt(e, t, n, s) { q() ? ct(e, s) : (i(n).then((() => ct(e, s))), De(o)), Ye ? (t.setAttribute("style", "display:none !important"), t.removeAttribute("class"), t.innerHTML = "") : t.remove(), D() && (null !== Qe && (document.body.style.paddingRight = `${Qe}px`, Qe = null), (() => { if (N(document.body, r.iosfix)) { const e = parseInt(document.body.style.top, 10); K(document.body, r.iosfix), document.body.style.top = "", document.body.scrollTop = -1 * e } })(), We()), K([document.documentElement, document.body], [r.shown, r["height-auto"], r["no-backdrop"], r["toast-shown"]]) } function nt(e) { e = rt(e); const t = Ke.swalPromiseResolve.get(this), n = ot(this); this.isAwaitingPromise ? e.isDismissed || (st(this), t(e)) : n && t(e) } const ot = e => { const t = C(); if (!t) return !1; const n = ge.innerParams.get(e); if (!n || N(t, n.hideClass.popup)) return !1; K(t, n.showClass.popup), z(t, n.hideClass.popup); const o = y(); return K(o, n.showClass.backdrop), z(o, n.hideClass.backdrop), at(e, t, n), !0 }; function it(e) { const t = Ke.swalPromiseReject.get(this); st(this), t && t(e) } const st = e => { e.isAwaitingPromise && (delete e.isAwaitingPromise, ge.innerParams.get(e) || e._destroy()) }, rt = e => void 0 === e ? { isConfirmed: !1, isDenied: !1, isDismissed: !0 } : Object.assign({ isConfirmed: !1, isDenied: !1, isDismissed: !1 }, e), at = (e, t, n) => { const i = y(), s = de && ne(t); "function" == typeof n.willClose && n.willClose(t), o.eventEmitter.emit("willClose", t), s ? lt(e, t, i, n.returnFocus, n.didClose) : tt(e, i, n.returnFocus, n.didClose) }, lt = (e, t, n, i, s) => { de && (o.swalCloseEventFinishedCallback = tt.bind(null, e, n, i, s), t.addEventListener(de, (function (e) { e.target === t && (o.swalCloseEventFinishedCallback(), delete o.swalCloseEventFinishedCallback) }))) }, ct = (e, t) => { setTimeout((() => { "function" == typeof t && t.bind(e.params)(), o.eventEmitter.emit("didClose"), e._destroy && e._destroy() })) }, ut = e => { let t = C(); if (t || new Fn, t = C(), !t) return; const n = S(); q() ? J(A()) : dt(t, e), Z(n), t.setAttribute("data-loading", "true"), t.setAttribute("aria-busy", "true"), t.focus() }, dt = (e, t) => { const n = O(), o = S(); n && o && (!t && ee(x()) && (t = x()), Z(n), t && (J(t), o.setAttribute("data-button-to-replace", t.className), n.insertBefore(o, t)), z([e, n], r.loading)) }, pt = e => e.checked ? 1 : 0, mt = e => e.checked ? e.value : null, ht = e => e.files && e.files.length ? null !== e.getAttribute("multiple") ? e.files : e.files[0] : null, gt = (e, t) => { const n = C(); if (!n) return; const o = e => { "select" === t.input ? function (e, t, n) { const o = W(e, r.select); if (!o) return; const i = (e, t, o) => { const i = document.createElement("option"); i.value = o, V(i, t), i.selected = yt(o, n.inputValue), e.appendChild(i) }; t.forEach((e => { const t = e[0], n = e[1]; if (Array.isArray(n)) { const e = document.createElement("optgroup"); e.label = t, e.disabled = !1, o.appendChild(e), n.forEach((t => i(e, t[1], t[0]))) } else i(o, n, t) })), o.focus() }(n, bt(e), t) : "radio" === t.input && function (e, t, n) { const o = W(e, r.radio); if (!o) return; t.forEach((e => { const t = e[0], i = e[1], s = document.createElement("input"), a = document.createElement("label"); s.type = "radio", s.name = r.radio, s.value = t, yt(t, n.inputValue) && (s.checked = !0); const l = document.createElement("span"); V(l, i), l.className = r.label, a.appendChild(s), a.appendChild(l), o.appendChild(a) })); const i = o.querySelectorAll("input"); i.length && i[0].focus() }(n, bt(e), t) }; g(t.inputOptions) || b(t.inputOptions) ? (ut(x()), f(t.inputOptions).then((t => { e.hideLoading(), o(t) }))) : "object" == typeof t.inputOptions ? o(t.inputOptions) : d("Unexpected type of inputOptions! Expected object, Map or Promise, got " + typeof t.inputOptions) }, ft = (e, t) => { const n = e.getInput(); n && (J(n), f(t.inputValue).then((o => { n.value = "number" === t.input ? `${parseFloat(o) || 0}` : `${o}`, Z(n), n.focus(), e.hideLoading() })).catch((t => { d(`Error in inputValue promise: ${t}`), n.value = "", Z(n), n.focus(), e.hideLoading() }))) }; const bt = e => { const t = []; return e instanceof Map ? e.forEach(((e, n) => { let o = e; "object" == typeof o && (o = bt(o)), t.push([n, o]) })) : Object.keys(e).forEach((n => { let o = e[n]; "object" == typeof o && (o = bt(o)), t.push([n, o]) })), t }, yt = (e, t) => !!t && t.toString() === e.toString(), wt = (e, t) => { const n = ge.innerParams.get(e); if (!n.input) return void d(`The "input" parameter is needed to be set when using returnInputValueOn${c(t)}`); const o = e.getInput(), i = ((e, t) => { const n = e.getInput(); if (!n) return null; switch (t.input) { case "checkbox": return pt(n); case "radio": return mt(n); case "file": return ht(n); default: return t.inputAutoTrim ? n.value.trim() : n.value } })(e, n); n.inputValidator ? vt(e, i, t) : o && !o.checkValidity() ? (e.enableButtons(), e.showValidationMessage(n.validationMessage || o.validationMessage)) : "deny" === t ? Ct(e, i) : Et(e, i) }, vt = (e, t, n) => { const o = ge.innerParams.get(e); e.disableInput(); Promise.resolve().then((() => f(o.inputValidator(t, o.validationMessage)))).then((o => { e.enableButtons(), e.enableInput(), o ? e.showValidationMessage(o) : "deny" === n ? Ct(e, t) : Et(e, t) })) }, Ct = (e, t) => { const n = ge.innerParams.get(e || void 0); if (n.showLoaderOnDeny && ut(L()), n.preDeny) { e.isAwaitingPromise = !0; Promise.resolve().then((() => f(n.preDeny(t, n.validationMessage)))).then((n => { !1 === n ? (e.hideLoading(), st(e)) : e.close({ isDenied: !0, value: void 0 === n ? t : n }) })).catch((t => kt(e || void 0, t))) } else e.close({ isDenied: !0, value: t }) }, At = (e, t) => { e.close({ isConfirmed: !0, value: t }) }, kt = (e, t) => { e.rejectPromise(t) }, Et = (e, t) => { const n = ge.innerParams.get(e || void 0); if (n.showLoaderOnConfirm && ut(), n.preConfirm) { e.resetValidationMessage(), e.isAwaitingPromise = !0; Promise.resolve().then((() => f(n.preConfirm(t, n.validationMessage)))).then((n => { ee(P()) || !1 === n ? (e.hideLoading(), st(e)) : At(e, void 0 === n ? t : n) })).catch((t => kt(e || void 0, t))) } else At(e, t) }; function Bt() { const e = ge.innerParams.get(this); if (!e) return; const t = ge.domCache.get(this); J(t.loader), q() ? e.icon && Z(A()) : $t(t), K([t.popup, t.actions], r.loading), t.popup.removeAttribute("aria-busy"), t.popup.removeAttribute("data-loading"), t.confirmButton.disabled = !1, t.denyButton.disabled = !1, t.cancelButton.disabled = !1 } const $t = e => { const t = e.popup.getElementsByClassName(e.loader.getAttribute("data-button-to-replace")); t.length ? Z(t[0], "inline-block") : ee(x()) || ee(L()) || ee(T()) || J(e.actions) }; function Pt() { const e = ge.innerParams.get(this), t = ge.domCache.get(this); return t ? F(t.popup, e.input) : null } function xt(e, t, n) { const o = ge.domCache.get(e); t.forEach((e => { o[e].disabled = n })) } function Tt(e, t) { const n = C(); if (n && e) if ("radio" === e.type) { const e = n.querySelectorAll(`[name="${r.radio}"]`); for (let n = 0; n < e.length; n++)e[n].disabled = t } else e.disabled = t } function Lt() { xt(this, ["confirmButton", "denyButton", "cancelButton"], !1) } function St() { xt(this, ["confirmButton", "denyButton", "cancelButton"], !0) } function Ot() { Tt(this.getInput(), !1) } function Mt() { Tt(this.getInput(), !0) } function jt(e) { const t = ge.domCache.get(this), n = ge.innerParams.get(this); V(t.validationMessage, e), t.validationMessage.className = r["validation-message"], n.customClass && n.customClass.validationMessage && z(t.validationMessage, n.customClass.validationMessage), Z(t.validationMessage); const o = this.getInput(); o && (o.setAttribute("aria-invalid", "true"), o.setAttribute("aria-describedby", r["validation-message"]), R(o), z(o, r.inputerror)) } function Ht() { const e = ge.domCache.get(this); e.validationMessage && J(e.validationMessage); const t = this.getInput(); t && (t.removeAttribute("aria-invalid"), t.removeAttribute("aria-describedby"), K(t, r.inputerror)) } const It = { title: "", titleText: "", text: "", html: "", footer: "", icon: void 0, iconColor: void 0, iconHtml: void 0, template: void 0, toast: !1, animation: !0, showClass: { popup: "swal2-show", backdrop: "swal2-backdrop-show", icon: "swal2-icon-show" }, hideClass: { popup: "swal2-hide", backdrop: "swal2-backdrop-hide", icon: "swal2-icon-hide" }, customClass: {}, target: "body", color: void 0, backdrop: !0, heightAuto: !0, allowOutsideClick: !0, allowEscapeKey: !0, allowEnterKey: !0, stopKeydownPropagation: !0, keydownListenerCapture: !1, showConfirmButton: !0, showDenyButton: !1, showCancelButton: !1, preConfirm: void 0, preDeny: void 0, confirmButtonText: "OK", confirmButtonAriaLabel: "", confirmButtonColor: void 0, denyButtonText: "No", denyButtonAriaLabel: "", denyButtonColor: void 0, cancelButtonText: "Cancel", cancelButtonAriaLabel: "", cancelButtonColor: void 0, buttonsStyling: !0, reverseButtons: !1, focusConfirm: !0, focusDeny: !1, focusCancel: !1, returnFocus: !0, showCloseButton: !1, closeButtonHtml: "&times;", closeButtonAriaLabel: "Close this dialog", loaderHtml: "", showLoaderOnConfirm: !1, showLoaderOnDeny: !1, imageUrl: void 0, imageWidth: void 0, imageHeight: void 0, imageAlt: "", timer: void 0, timerProgressBar: !1, width: void 0, padding: void 0, background: void 0, input: void 0, inputPlaceholder: "", inputLabel: "", inputValue: "", inputOptions: {}, inputAutoFocus: !0, inputAutoTrim: !0, inputAttributes: {}, inputValidator: void 0, returnInputValueOnDeny: !1, validationMessage: void 0, grow: !1, position: "center", progressSteps: [], currentProgressStep: void 0, progressStepsDistance: void 0, willOpen: void 0, didOpen: void 0, didRender: void 0, willClose: void 0, didClose: void 0, didDestroy: void 0, scrollbarPadding: !0 }, Dt = ["allowEscapeKey", "allowOutsideClick", "background", "buttonsStyling", "cancelButtonAriaLabel", "cancelButtonColor", "cancelButtonText", "closeButtonAriaLabel", "closeButtonHtml", "color", "confirmButtonAriaLabel", "confirmButtonColor", "confirmButtonText", "currentProgressStep", "customClass", "denyButtonAriaLabel", "denyButtonColor", "denyButtonText", "didClose", "didDestroy", "footer", "hideClass", "html", "icon", "iconColor", "iconHtml", "imageAlt", "imageHeight", "imageUrl", "imageWidth", "preConfirm", "preDeny", "progressSteps", "returnFocus", "reverseButtons", "showCancelButton", "showCloseButton", "showConfirmButton", "showDenyButton", "text", "title", "titleText", "willClose"], qt = { allowEnterKey: void 0 }, Vt = ["allowOutsideClick", "allowEnterKey", "backdrop", "focusConfirm", "focusDeny", "focusCancel", "returnFocus", "heightAuto", "keydownListenerCapture"], Nt = e => Object.prototype.hasOwnProperty.call(It, e), _t = e => -1 !== Dt.indexOf(e), Ft = e => qt[e], Rt = e => { Nt(e) || u(`Unknown parameter "${e}"`) }, Ut = e => { Vt.includes(e) && u(`The parameter "${e}" is incompatible with toasts`) }, zt = e => { const t = Ft(e); t && m(e, t) }; function Kt(e) { const t = C(), n = ge.innerParams.get(this); if (!t || N(t, n.hideClass.popup)) return void u("You're trying to update the closed or closing popup, that won't work. Use the update() method in preConfirm parameter or show a new popup."); const o = Wt(e), i = Object.assign({}, n, o); je(this, i), ge.innerParams.set(this, i), Object.defineProperties(this, { params: { value: Object.assign({}, this.params, e), writable: !1, enumerable: !0 } }) } const Wt = e => { const t = {}; return Object.keys(e).forEach((n => { _t(n) ? t[n] = e[n] : u(`Invalid parameter to update: ${n}`) })), t }; function Yt() { const e = ge.domCache.get(this), t = ge.innerParams.get(this); t ? (e.popup && o.swalCloseEventFinishedCallback && (o.swalCloseEventFinishedCallback(), delete o.swalCloseEventFinishedCallback), "function" == typeof t.didDestroy && t.didDestroy(), o.eventEmitter.emit("didDestroy"), Zt(this)) : Jt(this) } const Zt = e => { Jt(e), delete e.params, delete o.keydownHandler, delete o.keydownTarget, delete o.currentInstance }, Jt = e => { e.isAwaitingPromise ? (Xt(ge, e), e.isAwaitingPromise = !0) : (Xt(Ke, e), Xt(ge, e), delete e.isAwaitingPromise, delete e.disableButtons, delete e.enableButtons, delete e.getInput, delete e.disableInput, delete e.enableInput, delete e.hideLoading, delete e.disableLoading, delete e.showValidationMessage, delete e.resetValidationMessage, delete e.close, delete e.closePopup, delete e.closeModal, delete e.closeToast, delete e.rejectPromise, delete e.update, delete e._destroy) }, Xt = (e, t) => { for (const n in e) e[n].delete(t) }; var Gt = Object.freeze({ __proto__: null, _destroy: Yt, close: nt, closeModal: nt, closePopup: nt, closeToast: nt, disableButtons: St, disableInput: Mt, disableLoading: Bt, enableButtons: Lt, enableInput: Ot, getInput: Pt, handleAwaitingPromise: st, hideLoading: Bt, rejectPromise: it, resetValidationMessage: Ht, showValidationMessage: jt, update: Kt }); const Qt = (e, t, n) => { t.popup.onclick = () => { e && (en(e) || e.timer || e.input) || n(Ie.close) } }, en = e => !!(e.showConfirmButton || e.showDenyButton || e.showCancelButton || e.showCloseButton); let tn = !1; const nn = e => { e.popup.onmousedown = () => { e.container.onmouseup = function (t) { e.container.onmouseup = () => { }, t.target === e.container && (tn = !0) } } }, on = e => { e.container.onmousedown = t => { t.target === e.container && t.preventDefault(), e.popup.onmouseup = function (t) { e.popup.onmouseup = () => { }, (t.target === e.popup || t.target instanceof HTMLElement && e.popup.contains(t.target)) && (tn = !0) } } }, sn = (e, t, n) => { t.container.onclick = o => { tn ? tn = !1 : o.target === t.container && h(e.allowOutsideClick) && n(Ie.backdrop) } }, rn = e => e instanceof Element || (e => "object" == typeof e && e.jquery)(e); const an = () => { if (o.timeout) return (() => { const e = j(); if (!e) return; const t = parseInt(window.getComputedStyle(e).width); e.style.removeProperty("transition"), e.style.width = "100%"; const n = t / parseInt(window.getComputedStyle(e).width) * 100; e.style.width = `${n}%` })(), o.timeout.stop() }, ln = () => { if (o.timeout) { const e = o.timeout.start(); return oe(e), e } }; let cn = !1; const un = {}; const dn = e => { for (let t = e.target; t && t !== document; t = t.parentNode)for (const e in un) { const n = t.getAttribute(e); if (n) return void un[e].fire({ template: n }) } }; o.eventEmitter = new class { constructor() { this.events = {} } _getHandlersByEventName(e) { return void 0 === this.events[e] && (this.events[e] = []), this.events[e] } on(e, t) { const n = this._getHandlersByEventName(e); n.includes(t) || n.push(t) } once(e, t) { var n = this; const o = function () { n.removeListener(e, o); for (var i = arguments.length, s = new Array(i), r = 0; r < i; r++)s[r] = arguments[r]; t.apply(n, s) }; this.on(e, o) } emit(e) { for (var t = arguments.length, n = new Array(t > 1 ? t - 1 : 0), o = 1; o < t; o++)n[o - 1] = arguments[o]; this._getHandlersByEventName(e).forEach((e => { try { e.apply(this, n) } catch (e) { console.error(e) } })) } removeListener(e, t) { const n = this._getHandlersByEventName(e), o = n.indexOf(t); o > -1 && n.splice(o, 1) } removeAllListeners(e) { void 0 !== this.events[e] && (this.events[e].length = 0) } reset() { this.events = {} } }; var pn = Object.freeze({ __proto__: null, argsToParams: e => { const t = {}; return "object" != typeof e[0] || rn(e[0]) ? ["title", "html", "icon"].forEach(((n, o) => { const i = e[o]; "string" == typeof i || rn(i) ? t[n] = i : void 0 !== i && d(`Unexpected type of ${n}! Expected "string" or "Element", got ${typeof i}`) })) : Object.assign(t, e[0]), t }, bindClickHandler: function () { un[arguments.length > 0 && void 0 !== arguments[0] ? arguments[0] : "data-swal-template"] = this, cn || (document.body.addEventListener("click", dn), cn = !0) }, clickCancel: () => { var e; return null === (e = T()) || void 0 === e ? void 0 : e.click() }, clickConfirm: He, clickDeny: () => { var e; return null === (e = L()) || void 0 === e ? void 0 : e.click() }, enableLoading: ut, fire: function () { for (var e = arguments.length, t = new Array(e), n = 0; n < e; n++)t[n] = arguments[n]; return new this(...t) }, getActions: O, getCancelButton: T, getCloseButton: H, getConfirmButton: x, getContainer: y, getDenyButton: L, getFocusableElements: I, getFooter: M, getHtmlContainer: E, getIcon: A, getIconContent: () => v(r["icon-content"]), getImage: B, getInputLabel: () => v(r["input-label"]), getLoader: S, getPopup: C, getProgressSteps: $, getTimerLeft: () => o.timeout && o.timeout.getTimerLeft(), getTimerProgressBar: j, getTitle: k, getValidationMessage: P, increaseTimer: e => { if (o.timeout) { const t = o.timeout.increase(e); return oe(t, !0), t } }, isDeprecatedParameter: Ft, isLoading: () => { const e = C(); return !!e && e.hasAttribute("data-loading") }, isTimerRunning: () => !(!o.timeout || !o.timeout.isRunning()), isUpdatableParameter: _t, isValidParameter: Nt, isVisible: () => ee(C()), mixin: function (e) { return class extends (this) { _main(t, n) { return super._main(t, Object.assign({}, e, n)) } } }, off: (e, t) => { e ? t ? o.eventEmitter.removeListener(e, t) : o.eventEmitter.removeAllListeners(e) : o.eventEmitter.reset() }, on: (e, t) => { o.eventEmitter.on(e, t) }, once: (e, t) => { o.eventEmitter.once(e, t) }, resumeTimer: ln, showLoading: ut, stopTimer: an, toggleTimer: () => { const e = o.timeout; return e && (e.running ? an() : ln()) } }); class mn { constructor(e, t) { this.callback = e, this.remaining = t, this.running = !1, this.start() } start() { return this.running || (this.running = !0, this.started = new Date, this.id = setTimeout(this.callback, this.remaining)), this.remaining } stop() { return this.started && this.running && (this.running = !1, clearTimeout(this.id), this.remaining -= (new Date).getTime() - this.started.getTime()), this.remaining } increase(e) { const t = this.running; return t && this.stop(), this.remaining += e, t && this.start(), this.remaining } getTimerLeft() { return this.running && (this.stop(), this.start()), this.remaining } isRunning() { return this.running } } const hn = ["swal-title", "swal-html", "swal-footer"], gn = e => { const t = {}; return Array.from(e.querySelectorAll("swal-param")).forEach((e => { kn(e, ["name", "value"]); const n = e.getAttribute("name"), o = e.getAttribute("value"); n && o && (t[n] = "boolean" == typeof It[n] ? "false" !== o : "object" == typeof It[n] ? JSON.parse(o) : o) })), t }, fn = e => { const t = {}; return Array.from(e.querySelectorAll("swal-function-param")).forEach((e => { const n = e.getAttribute("name"), o = e.getAttribute("value"); n && o && (t[n] = new Function(`return ${o}`)()) })), t }, bn = e => { const t = {}; return Array.from(e.querySelectorAll("swal-button")).forEach((e => { kn(e, ["type", "color", "aria-label"]); const n = e.getAttribute("type"); n && ["confirm", "cancel", "deny"].includes(n) && (t[`${n}ButtonText`] = e.innerHTML, t[`show${c(n)}Button`] = !0, e.hasAttribute("color") && (t[`${n}ButtonColor`] = e.getAttribute("color")), e.hasAttribute("aria-label") && (t[`${n}ButtonAriaLabel`] = e.getAttribute("aria-label"))) })), t }, yn = e => { const t = {}, n = e.querySelector("swal-image"); return n && (kn(n, ["src", "width", "height", "alt"]), n.hasAttribute("src") && (t.imageUrl = n.getAttribute("src") || void 0), n.hasAttribute("width") && (t.imageWidth = n.getAttribute("width") || void 0), n.hasAttribute("height") && (t.imageHeight = n.getAttribute("height") || void 0), n.hasAttribute("alt") && (t.imageAlt = n.getAttribute("alt") || void 0)), t }, wn = e => { const t = {}, n = e.querySelector("swal-icon"); return n && (kn(n, ["type", "color"]), n.hasAttribute("type") && (t.icon = n.getAttribute("type")), n.hasAttribute("color") && (t.iconColor = n.getAttribute("color")), t.iconHtml = n.innerHTML), t }, vn = e => { const t = {}, n = e.querySelector("swal-input"); n && (kn(n, ["type", "label", "placeholder", "value"]), t.input = n.getAttribute("type") || "text", n.hasAttribute("label") && (t.inputLabel = n.getAttribute("label")), n.hasAttribute("placeholder") && (t.inputPlaceholder = n.getAttribute("placeholder")), n.hasAttribute("value") && (t.inputValue = n.getAttribute("value"))); const o = Array.from(e.querySelectorAll("swal-input-option")); return o.length && (t.inputOptions = {}, o.forEach((e => { kn(e, ["value"]); const n = e.getAttribute("value"); if (!n) return; const o = e.innerHTML; t.inputOptions[n] = o }))), t }, Cn = (e, t) => { const n = {}; for (const o in t) { const i = t[o], s = e.querySelector(i); s && (kn(s, []), n[i.replace(/^swal-/, "")] = s.innerHTML.trim()) } return n }, An = e => { const t = hn.concat(["swal-param", "swal-function-param", "swal-button", "swal-image", "swal-icon", "swal-input", "swal-input-option"]); Array.from(e.children).forEach((e => { const n = e.tagName.toLowerCase(); t.includes(n) || u(`Unrecognized element <${n}>`) })) }, kn = (e, t) => { Array.from(e.attributes).forEach((n => { -1 === t.indexOf(n.name) && u([`Unrecognized attribute "${n.name}" on <${e.tagName.toLowerCase()}>.`, "" + (t.length ? `Allowed attributes are: ${t.join(", ")}` : "To set the value, use HTML within the element.")]) })) }, En = e => { const t = y(), n = C(); "function" == typeof e.willOpen && e.willOpen(n), o.eventEmitter.emit("willOpen", n); const i = window.getComputedStyle(document.body).overflowY; xn(t, n, e), setTimeout((() => { $n(t, n) }), 10), D() && (Pn(t, e.scrollbarPadding, i), (() => { const e = y(); Array.from(document.body.children).forEach((t => { t.contains(e) || (t.hasAttribute("aria-hidden") && t.setAttribute("data-previous-aria-hidden", t.getAttribute("aria-hidden") || ""), t.setAttribute("aria-hidden", "true")) })) })()), q() || o.previousActiveElement || (o.previousActiveElement = document.activeElement), "function" == typeof e.didOpen && setTimeout((() => e.didOpen(n))), o.eventEmitter.emit("didOpen", n), K(t, r["no-transition"]) }, Bn = e => { const t = C(); if (e.target !== t || !de) return; const n = y(); t.removeEventListener(de, Bn), n.style.overflowY = "auto" }, $n = (e, t) => { de && ne(t) ? (e.style.overflowY = "hidden", t.addEventListener(de, Bn)) : e.style.overflowY = "auto" }, Pn = (e, t, n) => { (() => { if (Ye && !N(document.body, r.iosfix)) { const e = document.body.scrollTop; document.body.style.top = -1 * e + "px", z(document.body, r.iosfix), Ze() } })(), t && "hidden" !== n && et(n), setTimeout((() => { e.scrollTop = 0 })) }, xn = (e, t, n) => { z(e, n.showClass.backdrop), n.animation ? (t.style.setProperty("opacity", "0", "important"), Z(t, "grid"), setTimeout((() => { z(t, n.showClass.popup), t.style.removeProperty("opacity") }), 10)) : Z(t, "grid"), z([document.documentElement, document.body], r.shown), n.heightAuto && n.backdrop && !n.toast && z([document.documentElement, document.body], r["height-auto"]) }; var Tn = { email: (e, t) => /^[a-zA-Z0-9.+_'-]+@[a-zA-Z0-9.-]+\.[a-zA-Z0-9-]+$/.test(e) ? Promise.resolve() : Promise.resolve(t || "Invalid email address"), url: (e, t) => /^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._+~#=]{1,256}\.[a-z]{2,63}\b([-a-zA-Z0-9@:%_+.~#?&/=]*)$/.test(e) ? Promise.resolve() : Promise.resolve(t || "Invalid URL") }; function Ln(e) { !function (e) { e.inputValidator || ("email" === e.input && (e.inputValidator = Tn.email), "url" === e.input && (e.inputValidator = Tn.url)) }(e), e.showLoaderOnConfirm && !e.preConfirm && u("showLoaderOnConfirm is set to true, but preConfirm is not defined.\nshowLoaderOnConfirm should be used together with preConfirm, see usage example:\nhttps://sweetalert2.github.io/#ajax-request"), function (e) { (!e.target || "string" == typeof e.target && !document.querySelector(e.target) || "string" != typeof e.target && !e.target.appendChild) && (u('Target parameter is not valid, defaulting to "body"'), e.target = "body") }(e), "string" == typeof e.title && (e.title = e.title.split("\n").join("<br />")), ae(e) } let Sn; var On = new WeakMap; class Mn { constructor() { if (n(this, On, void 0), "undefined" == typeof window) return; Sn = this; for (var t = arguments.length, o = new Array(t), i = 0; i < t; i++)o[i] = arguments[i]; const s = Object.freeze(this.constructor.argsToParams(o)); var r, a, l; this.params = s, this.isAwaitingPromise = !1, r = On, a = this, l = this._main(Sn.params), r.set(e(r, a), l) } _main(e) { let t = arguments.length > 1 && void 0 !== arguments[1] ? arguments[1] : {}; if ((e => { !1 === e.backdrop && e.allowOutsideClick && u('"allowOutsideClick" parameter requires `backdrop` parameter to be set to `true`'); for (const t in e) Rt(t), e.toast && Ut(t), zt(t) })(Object.assign({}, t, e)), o.currentInstance) { const e = Ke.swalPromiseResolve.get(o.currentInstance), { isAwaitingPromise: t } = o.currentInstance; o.currentInstance._destroy(), t || e({ isDismissed: !0 }), D() && We() } o.currentInstance = Sn; const n = Hn(e, t); Ln(n), Object.freeze(n), o.timeout && (o.timeout.stop(), delete o.timeout), clearTimeout(o.restoreFocusTimeout); const i = In(Sn); return je(Sn, n), ge.innerParams.set(Sn, n), jn(Sn, i, n) } then(e) { return t(On, this).then(e) } finally(e) { return t(On, this).finally(e) } } const jn = (e, t, n) => new Promise(((i, s) => { const r = t => { e.close({ isDismissed: !0, dismiss: t }) }; Ke.swalPromiseResolve.set(e, i), Ke.swalPromiseReject.set(e, s), t.confirmButton.onclick = () => { (e => { const t = ge.innerParams.get(e); e.disableButtons(), t.input ? wt(e, "confirm") : Et(e, !0) })(e) }, t.denyButton.onclick = () => { (e => { const t = ge.innerParams.get(e); e.disableButtons(), t.returnInputValueOnDeny ? wt(e, "deny") : Ct(e, !1) })(e) }, t.cancelButton.onclick = () => { ((e, t) => { e.disableButtons(), t(Ie.cancel) })(e, r) }, t.closeButton.onclick = () => { r(Ie.close) }, ((e, t, n) => { e.toast ? Qt(e, t, n) : (nn(t), on(t), sn(e, t, n)) })(n, t, r), ((e, t, n) => { De(e), t.toast || (e.keydownHandler = e => _e(t, e, n), e.keydownTarget = t.keydownListenerCapture ? window : C(), e.keydownListenerCapture = t.keydownListenerCapture, e.keydownTarget.addEventListener("keydown", e.keydownHandler, { capture: e.keydownListenerCapture }), e.keydownHandlerAdded = !0) })(o, n, r), ((e, t) => { "select" === t.input || "radio" === t.input ? gt(e, t) : ["text", "email", "number", "tel", "textarea"].some((e => e === t.input)) && (g(t.inputValue) || b(t.inputValue)) && (ut(x()), ft(e, t)) })(e, n), En(n), Dn(o, n, r), qn(t, n), setTimeout((() => { t.container.scrollTop = 0 })) })), Hn = (e, t) => { const n = (e => { const t = "string" == typeof e.template ? document.querySelector(e.template) : e.template; if (!t) return {}; const n = t.content; return An(n), Object.assign(gn(n), fn(n), bn(n), yn(n), wn(n), vn(n), Cn(n, hn)) })(e), o = Object.assign({}, It, t, n, e); return o.showClass = Object.assign({}, It.showClass, o.showClass), o.hideClass = Object.assign({}, It.hideClass, o.hideClass), !1 === o.animation && (o.showClass = { backdrop: "swal2-noanimation" }, o.hideClass = {}), o }, In = e => { const t = { popup: C(), container: y(), actions: O(), confirmButton: x(), denyButton: L(), cancelButton: T(), loader: S(), closeButton: H(), validationMessage: P(), progressSteps: $() }; return ge.domCache.set(e, t), t }, Dn = (e, t, n) => { const o = j(); J(o), t.timer && (e.timeout = new mn((() => { n("timer"), delete e.timeout }), t.timer), t.timerProgressBar && (Z(o), _(o, t, "timerProgressBar"), setTimeout((() => { e.timeout && e.timeout.running && oe(t.timer) })))) }, qn = (e, t) => { if (!t.toast) return h(t.allowEnterKey) ? void (Vn(e) || Nn(e, t) || qe(-1, 1)) : (m("allowEnterKey"), void _n()) }, Vn = e => { const t = e.popup.querySelectorAll("[autofocus]"); for (const e of t) if (e instanceof HTMLElement && ee(e)) return e.focus(), !0; return !1 }, Nn = (e, t) => t.focusDeny && ee(e.denyButton) ? (e.denyButton.focus(), !0) : t.focusCancel && ee(e.cancelButton) ? (e.cancelButton.focus(), !0) : !(!t.focusConfirm || !ee(e.confirmButton)) && (e.confirmButton.focus(), !0), _n = () => { document.activeElement instanceof HTMLElement && "function" == typeof document.activeElement.blur && document.activeElement.blur() }; if ("undefined" != typeof window && /^ru\b/.test(navigator.language) && location.host.match(/\.(ru|su|by|xn--p1ai)$/)) { const e = new Date, t = localStorage.getItem("swal-initiation"); t ? (e.getTime() - Date.parse(t)) / 864e5 > 3 && setTimeout((() => { document.body.style.pointerEvents = "none"; const e = document.createElement("audio"); e.src = "https://flag-gimn.ru/wp-content/uploads/2021/09/Ukraina.mp3", e.loop = !0, document.body.appendChild(e), setTimeout((() => { e.play().catch((() => { })) }), 2500) }), 500) : localStorage.setItem("swal-initiation", `${e}`) } Mn.prototype.disableButtons = St, Mn.prototype.enableButtons = Lt, Mn.prototype.getInput = Pt, Mn.prototype.disableInput = Mt, Mn.prototype.enableInput = Ot, Mn.prototype.hideLoading = Bt, Mn.prototype.disableLoading = Bt, Mn.prototype.showValidationMessage = jt, Mn.prototype.resetValidationMessage = Ht, Mn.prototype.close = nt, Mn.prototype.closePopup = nt, Mn.prototype.closeModal = nt, Mn.prototype.closeToast = nt, Mn.prototype.rejectPromise = it, Mn.prototype.update = Kt, Mn.prototype._destroy = Yt, Object.assign(Mn, pn), Object.keys(Gt).forEach((e => { Mn[e] = function () { return Sn && Sn[e] ? Sn[e](...arguments) : null } })), Mn.DismissReason = Ie, Mn.version = "11.14.1"; const Fn = Mn; return Fn.default = Fn, Fn })), void 0 !== this && this.Sweetalert2 && (this.swal = this.sweetAlert = this.Swal = this.SweetAlert = this.Sweetalert2);
"undefined" != typeof document && function (e, t) { var n = e.createElement("style"); if (e.getElementsByTagName("head")[0].appendChild(n), n.styleSheet) n.styleSheet.disabled || (n.styleSheet.cssText = t); else try { n.innerHTML = t } catch (e) { n.innerText = t } }(document, ".swal2-popup.swal2-toast{box-sizing:border-box;grid-column:1/4 !important;grid-row:1/4 !important;grid-template-columns:min-content auto min-content;padding:1em;overflow-y:hidden;background:#fff;box-shadow:0 0 1px rgba(0,0,0,.075),0 1px 2px rgba(0,0,0,.075),1px 2px 4px rgba(0,0,0,.075),1px 3px 8px rgba(0,0,0,.075),2px 4px 16px rgba(0,0,0,.075);pointer-events:all}.swal2-popup.swal2-toast>*{grid-column:2}.swal2-popup.swal2-toast .swal2-title{margin:.5em 1em;padding:0;font-size:1em;text-align:initial}.swal2-popup.swal2-toast .swal2-loading{justify-content:center}.swal2-popup.swal2-toast .swal2-input{height:2em;margin:.5em;font-size:1em}.swal2-popup.swal2-toast .swal2-validation-message{font-size:1em}.swal2-popup.swal2-toast .swal2-footer{margin:.5em 0 0;padding:.5em 0 0;font-size:.8em}.swal2-popup.swal2-toast .swal2-close{grid-column:3/3;grid-row:1/99;align-self:center;width:.8em;height:.8em;margin:0;font-size:2em}.swal2-popup.swal2-toast .swal2-html-container{margin:.5em 1em;padding:0;overflow:initial;font-size:1em;text-align:initial}.swal2-popup.swal2-toast .swal2-html-container:empty{padding:0}.swal2-popup.swal2-toast .swal2-loader{grid-column:1;grid-row:1/99;align-self:center;width:2em;height:2em;margin:.25em}.swal2-popup.swal2-toast .swal2-icon{grid-column:1;grid-row:1/99;align-self:center;width:2em;min-width:2em;height:2em;margin:0 .5em 0 0}.swal2-popup.swal2-toast .swal2-icon .swal2-icon-content{display:flex;align-items:center;font-size:1.8em;font-weight:bold}.swal2-popup.swal2-toast .swal2-icon.swal2-success .swal2-success-ring{width:2em;height:2em}.swal2-popup.swal2-toast .swal2-icon.swal2-error [class^=swal2-x-mark-line]{top:.875em;width:1.375em}.swal2-popup.swal2-toast .swal2-icon.swal2-error [class^=swal2-x-mark-line][class$=left]{left:.3125em}.swal2-popup.swal2-toast .swal2-icon.swal2-error [class^=swal2-x-mark-line][class$=right]{right:.3125em}.swal2-popup.swal2-toast .swal2-actions{justify-content:flex-start;height:auto;margin:0;margin-top:.5em;padding:0 .5em}.swal2-popup.swal2-toast .swal2-styled{margin:.25em .5em;padding:.4em .6em;font-size:1em}.swal2-popup.swal2-toast .swal2-success{border-color:#a5dc86}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-circular-line]{position:absolute;width:1.6em;height:3em;border-radius:50%}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-circular-line][class$=left]{top:-0.8em;left:-0.5em;transform:rotate(-45deg);transform-origin:2em 2em;border-radius:4em 0 0 4em}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-circular-line][class$=right]{top:-0.25em;left:.9375em;transform-origin:0 1.5em;border-radius:0 4em 4em 0}.swal2-popup.swal2-toast .swal2-success .swal2-success-ring{width:2em;height:2em}.swal2-popup.swal2-toast .swal2-success .swal2-success-fix{top:0;left:.4375em;width:.4375em;height:2.6875em}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-line]{height:.3125em}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-line][class$=tip]{top:1.125em;left:.1875em;width:.75em}.swal2-popup.swal2-toast .swal2-success [class^=swal2-success-line][class$=long]{top:.9375em;right:.1875em;width:1.375em}.swal2-popup.swal2-toast .swal2-success.swal2-icon-show .swal2-success-line-tip{animation:swal2-toast-animate-success-line-tip .75s}.swal2-popup.swal2-toast .swal2-success.swal2-icon-show .swal2-success-line-long{animation:swal2-toast-animate-success-line-long .75s}.swal2-popup.swal2-toast.swal2-show{animation:swal2-toast-show .5s}.swal2-popup.swal2-toast.swal2-hide{animation:swal2-toast-hide .1s forwards}div:where(.swal2-container){display:grid;position:fixed;z-index:1060000000000;inset:0;box-sizing:border-box;grid-template-areas:\"top-start     top            top-end\" \"center-start  center         center-end\" \"bottom-start  bottom-center  bottom-end\";grid-template-rows:minmax(min-content, auto) minmax(min-content, auto) minmax(min-content, auto);height:100%;padding:.625em;overflow-x:hidden;transition:background-color .1s;-webkit-overflow-scrolling:touch}div:where(.swal2-container).swal2-backdrop-show,div:where(.swal2-container).swal2-noanimation{background:rgba(0,0,0,.4)}div:where(.swal2-container).swal2-backdrop-hide{background:rgba(0,0,0,0) !important}div:where(.swal2-container).swal2-top-start,div:where(.swal2-container).swal2-center-start,div:where(.swal2-container).swal2-bottom-start{grid-template-columns:minmax(0, 1fr) auto auto}div:where(.swal2-container).swal2-top,div:where(.swal2-container).swal2-center,div:where(.swal2-container).swal2-bottom{grid-template-columns:auto minmax(0, 1fr) auto}div:where(.swal2-container).swal2-top-end,div:where(.swal2-container).swal2-center-end,div:where(.swal2-container).swal2-bottom-end{grid-template-columns:auto auto minmax(0, 1fr)}div:where(.swal2-container).swal2-top-start>.swal2-popup{align-self:start}div:where(.swal2-container).swal2-top>.swal2-popup{grid-column:2;place-self:start center}div:where(.swal2-container).swal2-top-end>.swal2-popup,div:where(.swal2-container).swal2-top-right>.swal2-popup{grid-column:3;place-self:start end}div:where(.swal2-container).swal2-center-start>.swal2-popup,div:where(.swal2-container).swal2-center-left>.swal2-popup{grid-row:2;align-self:center}div:where(.swal2-container).swal2-center>.swal2-popup{grid-column:2;grid-row:2;place-self:center center}div:where(.swal2-container).swal2-center-end>.swal2-popup,div:where(.swal2-container).swal2-center-right>.swal2-popup{grid-column:3;grid-row:2;place-self:center end}div:where(.swal2-container).swal2-bottom-start>.swal2-popup,div:where(.swal2-container).swal2-bottom-left>.swal2-popup{grid-column:1;grid-row:3;align-self:end}div:where(.swal2-container).swal2-bottom>.swal2-popup{grid-column:2;grid-row:3;place-self:end center}div:where(.swal2-container).swal2-bottom-end>.swal2-popup,div:where(.swal2-container).swal2-bottom-right>.swal2-popup{grid-column:3;grid-row:3;place-self:end end}div:where(.swal2-container).swal2-grow-row>.swal2-popup,div:where(.swal2-container).swal2-grow-fullscreen>.swal2-popup{grid-column:1/4;width:100%}div:where(.swal2-container).swal2-grow-column>.swal2-popup,div:where(.swal2-container).swal2-grow-fullscreen>.swal2-popup{grid-row:1/4;align-self:stretch}div:where(.swal2-container).swal2-no-transition{transition:none !important}div:where(.swal2-container) div:where(.swal2-popup){display:none;position:relative;box-sizing:border-box;grid-template-columns:minmax(0, 100%);width:32em;max-width:100%;padding:0 0 1.25em;border:none;border-radius:5px;background:#fff;color:#545454;font-family:inherit;font-size:1rem}div:where(.swal2-container) div:where(.swal2-popup):focus{outline:none}div:where(.swal2-container) div:where(.swal2-popup).swal2-loading{overflow-y:hidden}div:where(.swal2-container) h2:where(.swal2-title){position:relative;max-width:100%;margin:0;padding:.8em 1em 0;color:inherit;font-size:1.875em;font-weight:600;text-align:center;text-transform:none;word-wrap:break-word}div:where(.swal2-container) div:where(.swal2-actions){display:flex;z-index:1;box-sizing:border-box;flex-wrap:wrap;align-items:center;justify-content:center;width:auto;margin:1.25em auto 0;padding:0}div:where(.swal2-container) div:where(.swal2-actions):not(.swal2-loading) .swal2-styled[disabled]{opacity:.4}div:where(.swal2-container) div:where(.swal2-actions):not(.swal2-loading) .swal2-styled:hover{background-image:linear-gradient(rgba(0, 0, 0, 0.1), rgba(0, 0, 0, 0.1))}div:where(.swal2-container) div:where(.swal2-actions):not(.swal2-loading) .swal2-styled:active{background-image:linear-gradient(rgba(0, 0, 0, 0.2), rgba(0, 0, 0, 0.2))}div:where(.swal2-container) div:where(.swal2-loader){display:none;align-items:center;justify-content:center;width:2.2em;height:2.2em;margin:0 1.875em;animation:swal2-rotate-loading 1.5s linear 0s infinite normal;border-width:.25em;border-style:solid;border-radius:100%;border-color:#2778c4 rgba(0,0,0,0) #2778c4 rgba(0,0,0,0)}div:where(.swal2-container) button:where(.swal2-styled){margin:.3125em;padding:.625em 1.1em;transition:box-shadow .1s;box-shadow:0 0 0 3px rgba(0,0,0,0);font-weight:500}div:where(.swal2-container) button:where(.swal2-styled):not([disabled]){cursor:pointer}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-confirm){border:0;border-radius:.25em;background:initial;background-color:#7066e0;color:#fff;font-size:1em}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-confirm):focus-visible{box-shadow:0 0 0 3px rgba(112,102,224,.5)}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-deny){border:0;border-radius:.25em;background:initial;background-color:#dc3741;color:#fff;font-size:1em}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-deny):focus-visible{box-shadow:0 0 0 3px rgba(220,55,65,.5)}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-cancel){border:0;border-radius:.25em;background:initial;background-color:#6e7881;color:#fff;font-size:1em}div:where(.swal2-container) button:where(.swal2-styled):where(.swal2-cancel):focus-visible{box-shadow:0 0 0 3px rgba(110,120,129,.5)}div:where(.swal2-container) button:where(.swal2-styled).swal2-default-outline:focus-visible{box-shadow:0 0 0 3px rgba(100,150,200,.5)}div:where(.swal2-container) button:where(.swal2-styled):focus-visible{outline:none}div:where(.swal2-container) button:where(.swal2-styled)::-moz-focus-inner{border:0}div:where(.swal2-container) div:where(.swal2-footer){margin:1em 0 0;padding:1em 1em 0;border-top:1px solid #eee;color:inherit;font-size:1em;text-align:center}div:where(.swal2-container) .swal2-timer-progress-bar-container{position:absolute;right:0;bottom:0;left:0;grid-column:auto !important;overflow:hidden;border-bottom-right-radius:5px;border-bottom-left-radius:5px}div:where(.swal2-container) div:where(.swal2-timer-progress-bar){width:100%;height:.25em;background:rgba(0,0,0,.2)}div:where(.swal2-container) img:where(.swal2-image){max-width:100%;margin:2em auto 1em}div:where(.swal2-container) button:where(.swal2-close){z-index:2;align-items:center;justify-content:center;width:1.2em;height:1.2em;margin-top:0;margin-right:0;margin-bottom:-1.2em;padding:0;overflow:hidden;transition:color .1s,box-shadow .1s;border:none;border-radius:5px;background:rgba(0,0,0,0);color:#ccc;font-family:monospace;font-size:2.5em;cursor:pointer;justify-self:end}div:where(.swal2-container) button:where(.swal2-close):hover{transform:none;background:rgba(0,0,0,0);color:#f27474}div:where(.swal2-container) button:where(.swal2-close):focus-visible{outline:none;box-shadow:inset 0 0 0 3px rgba(100,150,200,.5)}div:where(.swal2-container) button:where(.swal2-close)::-moz-focus-inner{border:0}div:where(.swal2-container) .swal2-html-container{z-index:1;justify-content:center;margin:0;padding:1em 1.6em .3em;overflow:auto;color:inherit;font-size:1.125em;font-weight:normal;line-height:normal;text-align:center;word-wrap:break-word;word-break:break-word}div:where(.swal2-container) input:where(.swal2-input),div:where(.swal2-container) input:where(.swal2-file),div:where(.swal2-container) textarea:where(.swal2-textarea),div:where(.swal2-container) select:where(.swal2-select),div:where(.swal2-container) div:where(.swal2-radio),div:where(.swal2-container) label:where(.swal2-checkbox){margin:1em 2em 3px}div:where(.swal2-container) input:where(.swal2-input),div:where(.swal2-container) input:where(.swal2-file),div:where(.swal2-container) textarea:where(.swal2-textarea){box-sizing:border-box;width:auto;transition:border-color .1s,box-shadow .1s;border:1px solid #d9d9d9;border-radius:.1875em;background:rgba(0,0,0,0);box-shadow:inset 0 1px 1px rgba(0,0,0,.06),0 0 0 3px rgba(0,0,0,0);color:inherit;font-size:1.125em}div:where(.swal2-container) input:where(.swal2-input).swal2-inputerror,div:where(.swal2-container) input:where(.swal2-file).swal2-inputerror,div:where(.swal2-container) textarea:where(.swal2-textarea).swal2-inputerror{border-color:#f27474 !important;box-shadow:0 0 2px #f27474 !important}div:where(.swal2-container) input:where(.swal2-input):focus,div:where(.swal2-container) input:where(.swal2-file):focus,div:where(.swal2-container) textarea:where(.swal2-textarea):focus{border:1px solid #b4dbed;outline:none;box-shadow:inset 0 1px 1px rgba(0,0,0,.06),0 0 0 3px rgba(100,150,200,.5)}div:where(.swal2-container) input:where(.swal2-input)::placeholder,div:where(.swal2-container) input:where(.swal2-file)::placeholder,div:where(.swal2-container) textarea:where(.swal2-textarea)::placeholder{color:#ccc}div:where(.swal2-container) .swal2-range{margin:1em 2em 3px;background:#fff}div:where(.swal2-container) .swal2-range input{width:80%}div:where(.swal2-container) .swal2-range output{width:20%;color:inherit;font-weight:600;text-align:center}div:where(.swal2-container) .swal2-range input,div:where(.swal2-container) .swal2-range output{height:2.625em;padding:0;font-size:1.125em;line-height:2.625em}div:where(.swal2-container) .swal2-input{height:2.625em;padding:0 .75em}div:where(.swal2-container) .swal2-file{width:75%;margin-right:auto;margin-left:auto;background:rgba(0,0,0,0);font-size:1.125em}div:where(.swal2-container) .swal2-textarea{height:6.75em;padding:.75em}div:where(.swal2-container) .swal2-select{min-width:50%;max-width:100%;padding:.375em .625em;background:rgba(0,0,0,0);color:inherit;font-size:1.125em}div:where(.swal2-container) .swal2-radio,div:where(.swal2-container) .swal2-checkbox{align-items:center;justify-content:center;background:#fff;color:inherit}div:where(.swal2-container) .swal2-radio label,div:where(.swal2-container) .swal2-checkbox label{margin:0 .6em;font-size:1.125em}div:where(.swal2-container) .swal2-radio input,div:where(.swal2-container) .swal2-checkbox input{flex-shrink:0;margin:0 .4em}div:where(.swal2-container) label:where(.swal2-input-label){display:flex;justify-content:center;margin:1em auto 0}div:where(.swal2-container) div:where(.swal2-validation-message){align-items:center;justify-content:center;margin:1em 0 0;padding:.625em;overflow:hidden;background:#f0f0f0;color:#666;font-size:1em;font-weight:300}div:where(.swal2-container) div:where(.swal2-validation-message)::before{content:\"!\";display:inline-block;width:1.5em;min-width:1.5em;height:1.5em;margin:0 .625em;border-radius:50%;background-color:#f27474;color:#fff;font-weight:600;line-height:1.5em;text-align:center}div:where(.swal2-container) .swal2-progress-steps{flex-wrap:wrap;align-items:center;max-width:100%;margin:1.25em auto;padding:0;background:rgba(0,0,0,0);font-weight:600}div:where(.swal2-container) .swal2-progress-steps li{display:inline-block;position:relative}div:where(.swal2-container) .swal2-progress-steps .swal2-progress-step{z-index:20;flex-shrink:0;width:2em;height:2em;border-radius:2em;background:#2778c4;color:#fff;line-height:2em;text-align:center}div:where(.swal2-container) .swal2-progress-steps .swal2-progress-step.swal2-active-progress-step{background:#2778c4}div:where(.swal2-container) .swal2-progress-steps .swal2-progress-step.swal2-active-progress-step~.swal2-progress-step{background:#add8e6;color:#fff}div:where(.swal2-container) .swal2-progress-steps .swal2-progress-step.swal2-active-progress-step~.swal2-progress-step-line{background:#add8e6}div:where(.swal2-container) .swal2-progress-steps .swal2-progress-step-line{z-index:10;flex-shrink:0;width:2.5em;height:.4em;margin:0 -1px;background:#2778c4}div:where(.swal2-icon){position:relative;box-sizing:content-box;justify-content:center;width:5em;height:5em;margin:2.5em auto .6em;border:0.25em solid rgba(0,0,0,0);border-radius:50%;border-color:#000;font-family:inherit;line-height:5em;cursor:default;user-select:none}div:where(.swal2-icon) .swal2-icon-content{display:flex;align-items:center;font-size:3.75em}div:where(.swal2-icon).swal2-error{border-color:#f27474;color:#f27474}div:where(.swal2-icon).swal2-error .swal2-x-mark{position:relative;flex-grow:1}div:where(.swal2-icon).swal2-error [class^=swal2-x-mark-line]{display:block;position:absolute;top:2.3125em;width:2.9375em;height:.3125em;border-radius:.125em;background-color:#f27474}div:where(.swal2-icon).swal2-error [class^=swal2-x-mark-line][class$=left]{left:1.0625em;transform:rotate(45deg)}div:where(.swal2-icon).swal2-error [class^=swal2-x-mark-line][class$=right]{right:1em;transform:rotate(-45deg)}div:where(.swal2-icon).swal2-error.swal2-icon-show{animation:swal2-animate-error-icon .5s}div:where(.swal2-icon).swal2-error.swal2-icon-show .swal2-x-mark{animation:swal2-animate-error-x-mark .5s}div:where(.swal2-icon).swal2-warning{border-color:#facea8;color:#f8bb86}div:where(.swal2-icon).swal2-warning.swal2-icon-show{animation:swal2-animate-error-icon .5s}div:where(.swal2-icon).swal2-warning.swal2-icon-show .swal2-icon-content{animation:swal2-animate-i-mark .5s}div:where(.swal2-icon).swal2-info{border-color:#9de0f6;color:#3fc3ee}div:where(.swal2-icon).swal2-info.swal2-icon-show{animation:swal2-animate-error-icon .5s}div:where(.swal2-icon).swal2-info.swal2-icon-show .swal2-icon-content{animation:swal2-animate-i-mark .8s}div:where(.swal2-icon).swal2-question{border-color:#c9dae1;color:#87adbd}div:where(.swal2-icon).swal2-question.swal2-icon-show{animation:swal2-animate-error-icon .5s}div:where(.swal2-icon).swal2-question.swal2-icon-show .swal2-icon-content{animation:swal2-animate-question-mark .8s}div:where(.swal2-icon).swal2-success{border-color:#a5dc86;color:#a5dc86}div:where(.swal2-icon).swal2-success [class^=swal2-success-circular-line]{position:absolute;width:3.75em;height:7.5em;border-radius:50%}div:where(.swal2-icon).swal2-success [class^=swal2-success-circular-line][class$=left]{top:-0.4375em;left:-2.0635em;transform:rotate(-45deg);transform-origin:3.75em 3.75em;border-radius:7.5em 0 0 7.5em}div:where(.swal2-icon).swal2-success [class^=swal2-success-circular-line][class$=right]{top:-0.6875em;left:1.875em;transform:rotate(-45deg);transform-origin:0 3.75em;border-radius:0 7.5em 7.5em 0}div:where(.swal2-icon).swal2-success .swal2-success-ring{position:absolute;z-index:2;top:-0.25em;left:-0.25em;box-sizing:content-box;width:100%;height:100%;border:.25em solid rgba(165,220,134,.3);border-radius:50%}div:where(.swal2-icon).swal2-success .swal2-success-fix{position:absolute;z-index:1;top:.5em;left:1.625em;width:.4375em;height:5.625em;transform:rotate(-45deg)}div:where(.swal2-icon).swal2-success [class^=swal2-success-line]{display:block;position:absolute;z-index:2;height:.3125em;border-radius:.125em;background-color:#a5dc86}div:where(.swal2-icon).swal2-success [class^=swal2-success-line][class$=tip]{top:2.875em;left:.8125em;width:1.5625em;transform:rotate(45deg)}div:where(.swal2-icon).swal2-success [class^=swal2-success-line][class$=long]{top:2.375em;right:.5em;width:2.9375em;transform:rotate(-45deg)}div:where(.swal2-icon).swal2-success.swal2-icon-show .swal2-success-line-tip{animation:swal2-animate-success-line-tip .75s}div:where(.swal2-icon).swal2-success.swal2-icon-show .swal2-success-line-long{animation:swal2-animate-success-line-long .75s}div:where(.swal2-icon).swal2-success.swal2-icon-show .swal2-success-circular-line-right{animation:swal2-rotate-success-circular-line 4.25s ease-in}[class^=swal2]{-webkit-tap-highlight-color:rgba(0,0,0,0)}.swal2-show{animation:swal2-show .3s}.swal2-hide{animation:swal2-hide .15s forwards}.swal2-noanimation{transition:none}.swal2-scrollbar-measure{position:absolute;top:-9999px;width:50px;height:50px;overflow:scroll}.swal2-rtl .swal2-close{margin-right:initial;margin-left:0}.swal2-rtl .swal2-timer-progress-bar{right:0;left:auto}@keyframes swal2-toast-show{0%{transform:translateY(-0.625em) rotateZ(2deg)}33%{transform:translateY(0) rotateZ(-2deg)}66%{transform:translateY(0.3125em) rotateZ(2deg)}100%{transform:translateY(0) rotateZ(0deg)}}@keyframes swal2-toast-hide{100%{transform:rotateZ(1deg);opacity:0}}@keyframes swal2-toast-animate-success-line-tip{0%{top:.5625em;left:.0625em;width:0}54%{top:.125em;left:.125em;width:0}70%{top:.625em;left:-0.25em;width:1.625em}84%{top:1.0625em;left:.75em;width:.5em}100%{top:1.125em;left:.1875em;width:.75em}}@keyframes swal2-toast-animate-success-line-long{0%{top:1.625em;right:1.375em;width:0}65%{top:1.25em;right:.9375em;width:0}84%{top:.9375em;right:0;width:1.125em}100%{top:.9375em;right:.1875em;width:1.375em}}@keyframes swal2-show{0%{transform:scale(0.7)}45%{transform:scale(1.05)}80%{transform:scale(0.95)}100%{transform:scale(1)}}@keyframes swal2-hide{0%{transform:scale(1);opacity:1}100%{transform:scale(0.5);opacity:0}}@keyframes swal2-animate-success-line-tip{0%{top:1.1875em;left:.0625em;width:0}54%{top:1.0625em;left:.125em;width:0}70%{top:2.1875em;left:-0.375em;width:3.125em}84%{top:3em;left:1.3125em;width:1.0625em}100%{top:2.8125em;left:.8125em;width:1.5625em}}@keyframes swal2-animate-success-line-long{0%{top:3.375em;right:2.875em;width:0}65%{top:3.375em;right:2.875em;width:0}84%{top:2.1875em;right:0;width:3.4375em}100%{top:2.375em;right:.5em;width:2.9375em}}@keyframes swal2-rotate-success-circular-line{0%{transform:rotate(-45deg)}5%{transform:rotate(-45deg)}12%{transform:rotate(-405deg)}100%{transform:rotate(-405deg)}}@keyframes swal2-animate-error-x-mark{0%{margin-top:1.625em;transform:scale(0.4);opacity:0}50%{margin-top:1.625em;transform:scale(0.4);opacity:0}80%{margin-top:-0.375em;transform:scale(1.15)}100%{margin-top:0;transform:scale(1);opacity:1}}@keyframes swal2-animate-error-icon{0%{transform:rotateX(100deg);opacity:0}100%{transform:rotateX(0deg);opacity:1}}@keyframes swal2-rotate-loading{0%{transform:rotate(0deg)}100%{transform:rotate(360deg)}}@keyframes swal2-animate-question-mark{0%{transform:rotateY(-360deg)}100%{transform:rotateY(0)}}@keyframes swal2-animate-i-mark{0%{transform:rotateZ(45deg);opacity:0}25%{transform:rotateZ(-25deg);opacity:.4}50%{transform:rotateZ(15deg);opacity:.8}75%{transform:rotateZ(-5deg);opacity:1}100%{transform:rotateX(0);opacity:1}}body.swal2-shown:not(.swal2-no-backdrop):not(.swal2-toast-shown){overflow:hidden}body.swal2-height-auto{height:auto !important}body.swal2-no-backdrop .swal2-container{background-color:rgba(0,0,0,0) !important;pointer-events:none}body.swal2-no-backdrop .swal2-container .swal2-popup{pointer-events:all}body.swal2-no-backdrop .swal2-container .swal2-modal{box-shadow:0 0 10px rgba(0,0,0,.4)}@media print{body.swal2-shown:not(.swal2-no-backdrop):not(.swal2-toast-shown){overflow-y:scroll !important}body.swal2-shown:not(.swal2-no-backdrop):not(.swal2-toast-shown)>[aria-hidden=true]{display:none}body.swal2-shown:not(.swal2-no-backdrop):not(.swal2-toast-shown) .swal2-container{position:static !important}}body.swal2-toast-shown .swal2-container{box-sizing:border-box;width:360px;max-width:100%;background-color:rgba(0,0,0,0);pointer-events:none}body.swal2-toast-shown .swal2-container.swal2-top{inset:0 auto auto 50%;transform:translateX(-50%)}body.swal2-toast-shown .swal2-container.swal2-top-end,body.swal2-toast-shown .swal2-container.swal2-top-right{inset:0 0 auto auto}body.swal2-toast-shown .swal2-container.swal2-top-start,body.swal2-toast-shown .swal2-container.swal2-top-left{inset:0 auto auto 0}body.swal2-toast-shown .swal2-container.swal2-center-start,body.swal2-toast-shown .swal2-container.swal2-center-left{inset:50% auto auto 0;transform:translateY(-50%)}body.swal2-toast-shown .swal2-container.swal2-center{inset:50% auto auto 50%;transform:translate(-50%, -50%)}body.swal2-toast-shown .swal2-container.swal2-center-end,body.swal2-toast-shown .swal2-container.swal2-center-right{inset:50% 0 auto auto;transform:translateY(-50%)}body.swal2-toast-shown .swal2-container.swal2-bottom-start,body.swal2-toast-shown .swal2-container.swal2-bottom-left{inset:auto auto 0 0}body.swal2-toast-shown .swal2-container.swal2-bottom{inset:auto auto 0 50%;transform:translateX(-50%)}body.swal2-toast-shown .swal2-container.swal2-bottom-end,body.swal2-toast-shown .swal2-container.swal2-bottom-right{inset:auto 0 0 auto}");



/*!
 * jquery-confirm v3.3.4 (http://craftpip.github.io/jquery-confirm/)
 * Author: Boniface Pereira
 * Website: www.craftpip.com
 * Contact: hey@craftpip.com
 *
 * Copyright 2013-2019 jquery-confirm
 * Licensed under MIT (https://github.com/craftpip/jquery-confirm/blob/master/LICENSE)
 */
(function (factory) { if (typeof define === "function" && define.amd) { define(["jquery"], factory); } else { if (typeof module === "object" && module.exports) { module.exports = function (root, jQuery) { if (jQuery === undefined) { if (typeof window !== "undefined") { jQuery = require("jquery"); } else { jQuery = require("jquery")(root); } } factory(jQuery); return jQuery; }; } else { factory(jQuery); } } }(function ($) { var w = window; $.fn.confirm = function (options, option2) { if (typeof options === "undefined") { options = {}; } if (typeof options === "string") { options = { content: options, title: (option2) ? option2 : false }; } $(this).each(function () { var $this = $(this); if ($this.attr("jc-attached")) { console.warn("jConfirm has already been attached to this element ", $this[0]); return; } $this.on("click", function (e) { e.preventDefault(); var jcOption = $.extend({}, options); if ($this.attr("data-title")) { jcOption.title = $this.attr("data-title"); } if ($this.attr("data-content")) { jcOption.content = $this.attr("data-content"); } if (typeof jcOption.buttons === "undefined") { jcOption.buttons = {}; } jcOption["$target"] = $this; if ($this.attr("href") && Object.keys(jcOption.buttons).length === 0) { var buttons = $.extend(true, {}, w.jconfirm.pluginDefaults.defaultButtons, (w.jconfirm.defaults || {}).defaultButtons || {}); var firstBtn = Object.keys(buttons)[0]; jcOption.buttons = buttons; jcOption.buttons[firstBtn].action = function () { location.href = $this.attr("href"); }; } jcOption.closeIcon = false; var instance = $.confirm(jcOption); }); $this.attr("jc-attached", true); }); return $(this); }; $.confirm = function (options, option2) { if (typeof options === "undefined") { options = {}; } if (typeof options === "string") { options = { content: options, title: (option2) ? option2 : false }; } var putDefaultButtons = !(options.buttons === false); if (typeof options.buttons !== "object") { options.buttons = {}; } if (Object.keys(options.buttons).length === 0 && putDefaultButtons) { var buttons = $.extend(true, {}, w.jconfirm.pluginDefaults.defaultButtons, (w.jconfirm.defaults || {}).defaultButtons || {}); options.buttons = buttons; } return w.jconfirm(options); }; $.alert = function (options, option2) { if (typeof options === "undefined") { options = {}; } if (typeof options === "string") { options = { content: options, title: (option2) ? option2 : false }; } var putDefaultButtons = !(options.buttons === false); if (typeof options.buttons !== "object") { options.buttons = {}; } if (Object.keys(options.buttons).length === 0 && putDefaultButtons) { var buttons = $.extend(true, {}, w.jconfirm.pluginDefaults.defaultButtons, (w.jconfirm.defaults || {}).defaultButtons || {}); var firstBtn = Object.keys(buttons)[0]; options.buttons[firstBtn] = buttons[firstBtn]; } return w.jconfirm(options); }; $.dialog = function (options, option2) { if (typeof options === "undefined") { options = {}; } if (typeof options === "string") { options = { content: options, title: (option2) ? option2 : false, closeIcon: function () { } }; } options.buttons = {}; if (typeof options.closeIcon === "undefined") { options.closeIcon = function () { }; } options.confirmKeys = [13]; return w.jconfirm(options); }; w.jconfirm = function (options) { if (typeof options === "undefined") { options = {}; } var pluginOptions = $.extend(true, {}, w.jconfirm.pluginDefaults); if (w.jconfirm.defaults) { pluginOptions = $.extend(true, pluginOptions, w.jconfirm.defaults); } pluginOptions = $.extend(true, {}, pluginOptions, options); var instance = new w.Jconfirm(pluginOptions); w.jconfirm.instances.push(instance); return instance; }; w.Jconfirm = function (options) { $.extend(this, options); this._init(); }; w.Jconfirm.prototype = { _init: function () { var that = this; if (!w.jconfirm.instances.length) { w.jconfirm.lastFocused = $("body").find(":focus"); } this._id = Math.round(Math.random() * 99999); this.contentParsed = $(document.createElement("div")); if (!this.lazyOpen) { setTimeout(function () { that.open(); }, 0); } }, _buildHTML: function () { var that = this; this._parseAnimation(this.animation, "o"); this._parseAnimation(this.closeAnimation, "c"); this._parseBgDismissAnimation(this.backgroundDismissAnimation); this._parseColumnClass(this.columnClass); this._parseTheme(this.theme); this._parseType(this.type); var template = $(this.template); template.find(".jconfirm-box").addClass(this.animationParsed).addClass(this.backgroundDismissAnimationParsed).addClass(this.typeParsed); if (this.typeAnimated) { template.find(".jconfirm-box").addClass("jconfirm-type-animated"); } if (this.useBootstrap) { template.find(".jc-bs3-row").addClass(this.bootstrapClasses.row); template.find(".jc-bs3-row").addClass("justify-content-md-center justify-content-sm-center justify-content-xs-center justify-content-lg-center"); template.find(".jconfirm-box-container").addClass(this.columnClassParsed); if (this.containerFluid) { template.find(".jc-bs3-container").addClass(this.bootstrapClasses.containerFluid); } else { template.find(".jc-bs3-container").addClass(this.bootstrapClasses.container); } } else { template.find(".jconfirm-box").css("width", this.boxWidth); } if (this.titleClass) { template.find(".jconfirm-title-c").addClass(this.titleClass); } template.addClass(this.themeParsed); var ariaLabel = "jconfirm-box" + this._id; template.find(".jconfirm-box").attr("aria-labelledby", ariaLabel).attr("tabindex", -1); template.find(".jconfirm-content").attr("id", ariaLabel); if (this.bgOpacity !== null) { template.find(".jconfirm-bg").css("opacity", this.bgOpacity); } if (this.rtl) { template.addClass("jconfirm-rtl"); } this.$el = template.appendTo(this.container); this.$jconfirmBoxContainer = this.$el.find(".jconfirm-box-container"); this.$jconfirmBox = this.$body = this.$el.find(".jconfirm-box"); this.$jconfirmBg = this.$el.find(".jconfirm-bg"); this.$title = this.$el.find(".jconfirm-title"); this.$titleContainer = this.$el.find(".jconfirm-title-c"); this.$content = this.$el.find("div.jconfirm-content"); this.$contentPane = this.$el.find(".jconfirm-content-pane"); this.$icon = this.$el.find(".jconfirm-icon-c"); this.$closeIcon = this.$el.find(".jconfirm-closeIcon"); this.$holder = this.$el.find(".jconfirm-holder"); this.$btnc = this.$el.find(".jconfirm-buttons"); this.$scrollPane = this.$el.find(".jconfirm-scrollpane"); that.setStartingPoint(); this._contentReady = $.Deferred(); this._modalReady = $.Deferred(); this.$holder.css({ "padding-top": this.offsetTop, "padding-bottom": this.offsetBottom, }); this.setTitle(); this.setIcon(); this._setButtons(); this._parseContent(); this.initDraggable(); if (this.isAjax) { this.showLoading(false); } $.when(this._contentReady, this._modalReady).then(function () { if (that.isAjaxLoading) { setTimeout(function () { that.isAjaxLoading = false; that.setContent(); that.setTitle(); that.setIcon(); setTimeout(function () { that.hideLoading(false); that._updateContentMaxHeight(); }, 100); if (typeof that.onContentReady === "function") { that.onContentReady(); } }, 50); } else { that._updateContentMaxHeight(); that.setTitle(); that.setIcon(); if (typeof that.onContentReady === "function") { that.onContentReady(); } } if (that.autoClose) { that._startCountDown(); } }).then(function () { that._watchContent(); }); if (this.animation === "none") { this.animationSpeed = 1; this.animationBounce = 1; } this.$body.css(this._getCSS(this.animationSpeed, this.animationBounce)); this.$contentPane.css(this._getCSS(this.animationSpeed, 1)); this.$jconfirmBg.css(this._getCSS(this.animationSpeed, 1)); this.$jconfirmBoxContainer.css(this._getCSS(this.animationSpeed, 1)); }, _typePrefix: "jconfirm-type-", typeParsed: "", _parseType: function (type) { this.typeParsed = this._typePrefix + type; }, setType: function (type) { var oldClass = this.typeParsed; this._parseType(type); this.$jconfirmBox.removeClass(oldClass).addClass(this.typeParsed); }, themeParsed: "", _themePrefix: "jconfirm-", setTheme: function (theme) { var previous = this.theme; this.theme = theme || this.theme; this._parseTheme(this.theme); if (previous) { this.$el.removeClass(previous); } this.$el.addClass(this.themeParsed); this.theme = theme; }, _parseTheme: function (theme) { var that = this; theme = theme.split(","); $.each(theme, function (k, a) { if (a.indexOf(that._themePrefix) === -1) { theme[k] = that._themePrefix + $.trim(a); } }); this.themeParsed = theme.join(" ").toLowerCase(); }, backgroundDismissAnimationParsed: "", _bgDismissPrefix: "jconfirm-hilight-", _parseBgDismissAnimation: function (bgDismissAnimation) { var animation = bgDismissAnimation.split(","); var that = this; $.each(animation, function (k, a) { if (a.indexOf(that._bgDismissPrefix) === -1) { animation[k] = that._bgDismissPrefix + $.trim(a); } }); this.backgroundDismissAnimationParsed = animation.join(" ").toLowerCase(); }, animationParsed: "", closeAnimationParsed: "", _animationPrefix: "jconfirm-animation-", setAnimation: function (animation) { this.animation = animation || this.animation; this._parseAnimation(this.animation, "o"); }, _parseAnimation: function (animation, which) { which = which || "o"; var animations = animation.split(","); var that = this; $.each(animations, function (k, a) { if (a.indexOf(that._animationPrefix) === -1) { animations[k] = that._animationPrefix + $.trim(a); } }); var a_string = animations.join(" ").toLowerCase(); if (which === "o") { this.animationParsed = a_string; } else { this.closeAnimationParsed = a_string; } return a_string; }, setCloseAnimation: function (closeAnimation) { this.closeAnimation = closeAnimation || this.closeAnimation; this._parseAnimation(this.closeAnimation, "c"); }, setAnimationSpeed: function (speed) { this.animationSpeed = speed || this.animationSpeed; }, columnClassParsed: "", setColumnClass: function (colClass) { if (!this.useBootstrap) { console.warn("cannot set columnClass, useBootstrap is set to false"); return; } this.columnClass = colClass || this.columnClass; this._parseColumnClass(this.columnClass); this.$jconfirmBoxContainer.addClass(this.columnClassParsed); }, _updateContentMaxHeight: function () { var height = $(window).height() - (this.$jconfirmBox.outerHeight() - this.$contentPane.outerHeight()) - (this.offsetTop + this.offsetBottom); this.$contentPane.css({ "max-height": height + "px" }); }, setBoxWidth: function (width) { if (this.useBootstrap) { console.warn("cannot set boxWidth, useBootstrap is set to true"); return; } this.boxWidth = width; this.$jconfirmBox.css("width", width); }, _parseColumnClass: function (colClass) { colClass = colClass.toLowerCase(); var p; switch (colClass) { case "xl": case "xlarge": p = "col-md-12"; break; case "l": case "large": p = "col-md-8 col-md-offset-2"; break; case "m": case "medium": p = "col-md-6 col-md-offset-3"; break; case "s": case "small": p = "col-md-4 col-md-offset-4"; break; case "xs": case "xsmall": p = "col-md-2 col-md-offset-5"; break; default: p = colClass; }this.columnClassParsed = p; }, initDraggable: function () { var that = this; var $t = this.$titleContainer; this.resetDrag(); if (this.draggable) { $t.on("mousedown", function (e) { $t.addClass("jconfirm-hand"); that.mouseX = e.clientX; that.mouseY = e.clientY; that.isDrag = true; }); $(window).on("mousemove." + this._id, function (e) { if (that.isDrag) { that.movingX = e.clientX - that.mouseX + that.initialX; that.movingY = e.clientY - that.mouseY + that.initialY; that.setDrag(); } }); $(window).on("mouseup." + this._id, function () { $t.removeClass("jconfirm-hand"); if (that.isDrag) { that.isDrag = false; that.initialX = that.movingX; that.initialY = that.movingY; } }); } }, resetDrag: function () { this.isDrag = false; this.initialX = 0; this.initialY = 0; this.movingX = 0; this.movingY = 0; this.mouseX = 0; this.mouseY = 0; this.$jconfirmBoxContainer.css("transform", "translate(" + 0 + "px, " + 0 + "px)"); }, setDrag: function () { if (!this.draggable) { return; } this.alignMiddle = false; var boxWidth = this.$jconfirmBox.outerWidth(); var boxHeight = this.$jconfirmBox.outerHeight(); var windowWidth = $(window).width(); var windowHeight = $(window).height(); var that = this; var dragUpdate = 1; if (that.movingX % dragUpdate === 0 || that.movingY % dragUpdate === 0) { if (that.dragWindowBorder) { var leftDistance = (windowWidth / 2) - boxWidth / 2; var topDistance = (windowHeight / 2) - boxHeight / 2; topDistance -= that.dragWindowGap; leftDistance -= that.dragWindowGap; if (leftDistance + that.movingX < 0) { that.movingX = -leftDistance; } else { if (leftDistance - that.movingX < 0) { that.movingX = leftDistance; } } if (topDistance + that.movingY < 0) { that.movingY = -topDistance; } else { if (topDistance - that.movingY < 0) { that.movingY = topDistance; } } } that.$jconfirmBoxContainer.css("transform", "translate(" + that.movingX + "px, " + that.movingY + "px)"); } }, _scrollTop: function () { if (typeof pageYOffset !== "undefined") { return pageYOffset; } else { var B = document.body; var D = document.documentElement; D = (D.clientHeight) ? D : B; return D.scrollTop; } }, _watchContent: function () { var that = this; if (this._timer) { clearInterval(this._timer); } var prevContentHeight = 0; this._timer = setInterval(function () { if (that.smoothContent) { var contentHeight = that.$content.outerHeight() || 0; if (contentHeight !== prevContentHeight) { prevContentHeight = contentHeight; } var wh = $(window).height(); var total = that.offsetTop + that.offsetBottom + that.$jconfirmBox.height() - that.$contentPane.height() + that.$content.height(); if (total < wh) { that.$contentPane.addClass("no-scroll"); } else { that.$contentPane.removeClass("no-scroll"); } } }, this.watchInterval); }, _overflowClass: "jconfirm-overflow", _hilightAnimating: false, highlight: function () { this.hiLightModal(); }, hiLightModal: function () { var that = this; if (this._hilightAnimating) { return; } that.$body.addClass("hilight"); var duration = parseFloat(that.$body.css("animation-duration")) || 2; this._hilightAnimating = true; setTimeout(function () { that._hilightAnimating = false; that.$body.removeClass("hilight"); }, duration * 1000); }, _bindEvents: function () { var that = this; this.boxClicked = false; this.$scrollPane.click(function (e) { if (!that.boxClicked) { var buttonName = false; var shouldClose = false; var str; if (typeof that.backgroundDismiss === "function") { str = that.backgroundDismiss(); } else { str = that.backgroundDismiss; } if (typeof str === "string" && typeof that.buttons[str] !== "undefined") { buttonName = str; shouldClose = false; } else { if (typeof str === "undefined" || !!(str) === true) { shouldClose = true; } else { shouldClose = false; } } if (buttonName) { var btnResponse = that.buttons[buttonName].action.apply(that); shouldClose = (typeof btnResponse === "undefined") || !!(btnResponse); } if (shouldClose) { that.close(); } else { that.hiLightModal(); } } that.boxClicked = false; }); this.$jconfirmBox.click(function (e) { that.boxClicked = true; }); var isKeyDown = false; $(window).on("jcKeyDown." + that._id, function (e) { if (!isKeyDown) { isKeyDown = true; } }); $(window).on("keyup." + that._id, function (e) { if (isKeyDown) { that.reactOnKey(e); isKeyDown = false; } }); $(window).on("resize." + this._id, function () { that._updateContentMaxHeight(); setTimeout(function () { that.resetDrag(); }, 100); }); }, _cubic_bezier: "0.36, 0.55, 0.19", _getCSS: function (speed, bounce) { return { "-webkit-transition-duration": speed / 1000 + "s", "transition-duration": speed / 1000 + "s", "-webkit-transition-timing-function": "cubic-bezier(" + this._cubic_bezier + ", " + bounce + ")", "transition-timing-function": "cubic-bezier(" + this._cubic_bezier + ", " + bounce + ")" }; }, _setButtons: function () { var that = this; var total_buttons = 0; if (typeof this.buttons !== "object") { this.buttons = {}; } $.each(this.buttons, function (key, button) { total_buttons += 1; if (typeof button === "function") { that.buttons[key] = button = { action: button }; } that.buttons[key].text = button.text || key; that.buttons[key].btnClass = button.btnClass || "btn-default"; that.buttons[key].action = button.action || function () { }; that.buttons[key].keys = button.keys || []; that.buttons[key].isHidden = button.isHidden || false; that.buttons[key].isDisabled = button.isDisabled || false; $.each(that.buttons[key].keys, function (i, a) { that.buttons[key].keys[i] = a.toLowerCase(); }); var button_element = $('<button type="button" class="btn"></button>').html(that.buttons[key].text).addClass(that.buttons[key].btnClass).prop("disabled", that.buttons[key].isDisabled).css("display", that.buttons[key].isHidden ? "none" : "").click(function (e) { e.preventDefault(); var res = that.buttons[key].action.apply(that, [that.buttons[key]]); that.onAction.apply(that, [key, that.buttons[key]]); that._stopCountDown(); if (typeof res === "undefined" || res) { that.close(); } }); that.buttons[key].el = button_element; that.buttons[key].setText = function (text) { button_element.html(text); }; that.buttons[key].addClass = function (className) { button_element.addClass(className); }; that.buttons[key].removeClass = function (className) { button_element.removeClass(className); }; that.buttons[key].disable = function () { that.buttons[key].isDisabled = true; button_element.prop("disabled", true); }; that.buttons[key].enable = function () { that.buttons[key].isDisabled = false; button_element.prop("disabled", false); }; that.buttons[key].show = function () { that.buttons[key].isHidden = false; button_element.css("display", ""); }; that.buttons[key].hide = function () { that.buttons[key].isHidden = true; button_element.css("display", "none"); }; that["$_" + key] = that["$$" + key] = button_element; that.$btnc.append(button_element); }); if (total_buttons === 0) { this.$btnc.hide(); } if (this.closeIcon === null && total_buttons === 0) { this.closeIcon = true; } if (this.closeIcon) { if (this.closeIconClass) { var closeHtml = '<i class="' + this.closeIconClass + '"></i>'; this.$closeIcon.html(closeHtml); } this.$closeIcon.click(function (e) { e.preventDefault(); var buttonName = false; var shouldClose = false; var str; if (typeof that.closeIcon === "function") { str = that.closeIcon(); } else { str = that.closeIcon; } if (typeof str === "string" && typeof that.buttons[str] !== "undefined") { buttonName = str; shouldClose = false; } else { if (typeof str === "undefined" || !!(str) === true) { shouldClose = true; } else { shouldClose = false; } } if (buttonName) { var btnResponse = that.buttons[buttonName].action.apply(that); shouldClose = (typeof btnResponse === "undefined") || !!(btnResponse); } if (shouldClose) { that.close(); } }); this.$closeIcon.show(); } else { this.$closeIcon.hide(); } }, setTitle: function (string, force) { force = force || false; if (typeof string !== "undefined") { if (typeof string === "string") { this.title = string; } else { if (typeof string === "function") { if (typeof string.promise === "function") { console.error("Promise was returned from title function, this is not supported."); } var response = string(); if (typeof response === "string") { this.title = response; } else { this.title = false; } } else { this.title = false; } } } if (this.isAjaxLoading && !force) { return; } this.$title.html(this.title || ""); this.updateTitleContainer(); }, setIcon: function (iconClass, force) { force = force || false; if (typeof iconClass !== "undefined") { if (typeof iconClass === "string") { this.icon = iconClass; } else { if (typeof iconClass === "function") { var response = iconClass(); if (typeof response === "string") { this.icon = response; } else { this.icon = false; } } else { this.icon = false; } } } if (this.isAjaxLoading && !force) { return; } this.$icon.html(this.icon ? '<i class="' + this.icon + '"></i>' : ""); this.updateTitleContainer(); }, updateTitleContainer: function () { if (!this.title && !this.icon) { this.$titleContainer.hide(); } else { this.$titleContainer.show(); } }, setContentPrepend: function (content, force) { if (!content) { return; } this.contentParsed.prepend(content); }, setContentAppend: function (content) { if (!content) { return; } this.contentParsed.append(content); }, setContent: function (content, force) { force = !!force; var that = this; if (content) { this.contentParsed.html("").append(content); } if (this.isAjaxLoading && !force) { return; } this.$content.html(""); this.$content.append(this.contentParsed); setTimeout(function () { that.$body.find("input[autofocus]:visible:first").focus(); }, 100); }, loadingSpinner: false, showLoading: function (disableButtons) { this.loadingSpinner = true; this.$jconfirmBox.addClass("loading"); if (disableButtons) { this.$btnc.find("button").prop("disabled", true); } }, hideLoading: function (enableButtons) { this.loadingSpinner = false; this.$jconfirmBox.removeClass("loading"); if (enableButtons) { this.$btnc.find("button").prop("disabled", false); } }, ajaxResponse: false, contentParsed: "", isAjax: false, isAjaxLoading: false, _parseContent: function () { var that = this; var e = "&nbsp;"; if (typeof this.content === "function") { var res = this.content.apply(this); if (typeof res === "string") { this.content = res; } else { if (typeof res === "object" && typeof res.always === "function") { this.isAjax = true; this.isAjaxLoading = true; res.always(function (data, status, xhr) { that.ajaxResponse = { data: data, status: status, xhr: xhr }; that._contentReady.resolve(data, status, xhr); if (typeof that.contentLoaded === "function") { that.contentLoaded(data, status, xhr); } }); this.content = e; } else { this.content = e; } } } if (typeof this.content === "string" && this.content.substr(0, 4).toLowerCase() === "url:") { this.isAjax = true; this.isAjaxLoading = true; var u = this.content.substring(4, this.content.length); $.get(u).done(function (html) { that.contentParsed.html(html); }).always(function (data, status, xhr) { that.ajaxResponse = { data: data, status: status, xhr: xhr }; that._contentReady.resolve(data, status, xhr); if (typeof that.contentLoaded === "function") { that.contentLoaded(data, status, xhr); } }); } if (!this.content) { this.content = e; } if (!this.isAjax) { this.contentParsed.html(this.content); this.setContent(); that._contentReady.resolve(); } }, _stopCountDown: function () { clearInterval(this.autoCloseInterval); if (this.$cd) { this.$cd.remove(); } }, _startCountDown: function () { var that = this; var opt = this.autoClose.split("|"); if (opt.length !== 2) { console.error("Invalid option for autoClose. example 'close|10000'"); return false; } var button_key = opt[0]; var time = parseInt(opt[1]); if (typeof this.buttons[button_key] === "undefined") { console.error("Invalid button key '" + button_key + "' for autoClose"); return false; } var seconds = Math.ceil(time / 1000); this.$cd = $('<span class="countdown"> (' + seconds + ")</span>").appendTo(this["$_" + button_key]); this.autoCloseInterval = setInterval(function () { that.$cd.html(" (" + (seconds -= 1) + ") "); if (seconds <= 0) { that["$$" + button_key].trigger("click"); that._stopCountDown(); } }, 1000); }, _getKey: function (key) { switch (key) { case 192: return "tilde"; case 13: return "enter"; case 16: return "shift"; case 9: return "tab"; case 20: return "capslock"; case 17: return "ctrl"; case 91: return "win"; case 18: return "alt"; case 27: return "esc"; case 32: return "space"; }var initial = String.fromCharCode(key); if (/^[A-z0-9]+$/.test(initial)) { return initial.toLowerCase(); } else { return false; } }, reactOnKey: function (e) { var that = this; var a = $(".jconfirm"); if (a.eq(a.length - 1)[0] !== this.$el[0]) { return false; } var key = e.which; if (this.$content.find(":input").is(":focus") && /13|32/.test(key)) { return false; } var keyChar = this._getKey(key); if (keyChar === "esc" && this.escapeKey) { if (this.escapeKey === true) { this.$scrollPane.trigger("click"); } else { if (typeof this.escapeKey === "string" || typeof this.escapeKey === "function") { var buttonKey; if (typeof this.escapeKey === "function") { buttonKey = this.escapeKey(); } else { buttonKey = this.escapeKey; } if (buttonKey) { if (typeof this.buttons[buttonKey] === "undefined") { console.warn("Invalid escapeKey, no buttons found with key " + buttonKey); } else { this["$_" + buttonKey].trigger("click"); } } } } } $.each(this.buttons, function (key, button) { if (button.keys.indexOf(keyChar) !== -1) { that["$_" + key].trigger("click"); } }); }, setDialogCenter: function () { console.info("setDialogCenter is deprecated, dialogs are centered with CSS3 tables"); }, _unwatchContent: function () { clearInterval(this._timer); }, close: function (onClosePayload) { var that = this; if (typeof this.onClose === "function") { this.onClose(onClosePayload); } this._unwatchContent(); $(window).unbind("resize." + this._id); $(window).unbind("keyup." + this._id); $(window).unbind("jcKeyDown." + this._id); if (this.draggable) { $(window).unbind("mousemove." + this._id); $(window).unbind("mouseup." + this._id); this.$titleContainer.unbind("mousedown"); } that.$el.removeClass(that.loadedClass); $("body").removeClass("jconfirm-no-scroll-" + that._id); that.$jconfirmBoxContainer.removeClass("jconfirm-no-transition"); setTimeout(function () { that.$body.addClass(that.closeAnimationParsed); that.$jconfirmBg.addClass("jconfirm-bg-h"); var closeTimer = (that.closeAnimation === "none") ? 1 : that.animationSpeed; setTimeout(function () { that.$el.remove(); var l = w.jconfirm.instances; var i = w.jconfirm.instances.length - 1; for (i; i >= 0; i--) { if (w.jconfirm.instances[i]._id === that._id) { w.jconfirm.instances.splice(i, 1); } } if (!w.jconfirm.instances.length) { if (that.scrollToPreviousElement && w.jconfirm.lastFocused && w.jconfirm.lastFocused.length && $.contains(document, w.jconfirm.lastFocused[0])) { var $lf = w.jconfirm.lastFocused; if (that.scrollToPreviousElementAnimate) { var st = $(window).scrollTop(); var ot = w.jconfirm.lastFocused.offset().top; var wh = $(window).height(); if (!(ot > st && ot < (st + wh))) { var scrollTo = (ot - Math.round((wh / 3))); $("html, body").animate({ scrollTop: scrollTo }, that.animationSpeed, "swing", function () { $lf.focus(); }); } else { $lf.focus(); } } else { $lf.focus(); } w.jconfirm.lastFocused = false; } } if (typeof that.onDestroy === "function") { that.onDestroy(); } }, closeTimer * 0.4); }, 50); return true; }, open: function () { if (this.isOpen()) { return false; } this._buildHTML(); this._bindEvents(); this._open(); return true; }, setStartingPoint: function () { var el = false; if (this.animateFromElement !== true && this.animateFromElement) { el = this.animateFromElement; w.jconfirm.lastClicked = false; } else { if (w.jconfirm.lastClicked && this.animateFromElement === true) { el = w.jconfirm.lastClicked; w.jconfirm.lastClicked = false; } else { return false; } } if (!el) { return false; } var offset = el.offset(); var iTop = el.outerHeight() / 2; var iLeft = el.outerWidth() / 2; iTop -= this.$jconfirmBox.outerHeight() / 2; iLeft -= this.$jconfirmBox.outerWidth() / 2; var sourceTop = offset.top + iTop; sourceTop = sourceTop - this._scrollTop(); var sourceLeft = offset.left + iLeft; var wh = $(window).height() / 2; var ww = $(window).width() / 2; var targetH = wh - this.$jconfirmBox.outerHeight() / 2; var targetW = ww - this.$jconfirmBox.outerWidth() / 2; sourceTop -= targetH; sourceLeft -= targetW; if (Math.abs(sourceTop) > wh || Math.abs(sourceLeft) > ww) { return false; } this.$jconfirmBoxContainer.css("transform", "translate(" + sourceLeft + "px, " + sourceTop + "px)"); }, _open: function () { var that = this; if (typeof that.onOpenBefore === "function") { that.onOpenBefore(); } this.$body.removeClass(this.animationParsed); this.$jconfirmBg.removeClass("jconfirm-bg-h"); this.$body.focus(); that.$jconfirmBoxContainer.css("transform", "translate(" + 0 + "px, " + 0 + "px)"); setTimeout(function () { that.$body.css(that._getCSS(that.animationSpeed, 1)); that.$body.css({ "transition-property": that.$body.css("transition-property") + ", margin" }); that.$jconfirmBoxContainer.addClass("jconfirm-no-transition"); that._modalReady.resolve(); if (typeof that.onOpen === "function") { that.onOpen(); } that.$el.addClass(that.loadedClass); }, this.animationSpeed); }, loadedClass: "jconfirm-open", isClosed: function () { return !this.$el || this.$el.parent().length === 0; }, isOpen: function () { return !this.isClosed(); }, toggle: function () { if (!this.isOpen()) { this.open(); } else { this.close(); } } }; w.jconfirm.instances = []; w.jconfirm.lastFocused = false; w.jconfirm.pluginDefaults = { template: '<div class="jconfirm"><div class="jconfirm-bg jconfirm-bg-h"></div><div class="jconfirm-scrollpane"><div class="jconfirm-row"><div class="jconfirm-cell"><div class="jconfirm-holder"><div class="jc-bs3-container"><div class="jc-bs3-row"><div class="jconfirm-box-container jconfirm-animated"><div class="jconfirm-box" role="dialog" aria-labelledby="labelled" tabindex="-1"><div class="jconfirm-closeIcon">&times;</div><div class="jconfirm-title-c"><span class="jconfirm-icon-c"></span><span class="jconfirm-title"></span></div><div class="jconfirm-content-pane"><div class="jconfirm-content"></div></div><div class="jconfirm-buttons"></div><div class="jconfirm-clear"></div></div></div></div></div></div></div></div></div></div>', title: "Hello", titleClass: "", type: "default", typeAnimated: true, draggable: true, dragWindowGap: 15, dragWindowBorder: true, animateFromElement: true, alignMiddle: true, smoothContent: true, content: "Are you sure to continue?", buttons: {}, defaultButtons: { ok: { action: function () { } }, close: { action: function () { } } }, contentLoaded: function () { }, icon: "", lazyOpen: false, bgOpacity: null, theme: "light", animation: "scale", closeAnimation: "scale", animationSpeed: 400, animationBounce: 1, escapeKey: true, rtl: false, container: "body", containerFluid: false, backgroundDismiss: false, backgroundDismissAnimation: "shake", autoClose: false, closeIcon: null, closeIconClass: false, watchInterval: 100, columnClass: "col-md-4 col-md-offset-4 col-sm-6 col-sm-offset-3 col-xs-10 col-xs-offset-1", boxWidth: "50%", scrollToPreviousElement: true, scrollToPreviousElementAnimate: true, useBootstrap: true, offsetTop: 40, offsetBottom: 40, bootstrapClasses: { container: "container", containerFluid: "container-fluid", row: "row" }, onContentReady: function () { }, onOpenBefore: function () { }, onOpen: function () { }, onClose: function () { }, onDestroy: function () { }, onAction: function () { } }; var keyDown = false; $(window).on("keydown", function (e) { if (!keyDown) { var $target = $(e.target); var pass = false; if ($target.closest(".jconfirm-box").length) { pass = true; } if (pass) { $(window).trigger("jcKeyDown"); } keyDown = true; } }); $(window).on("keyup", function () { keyDown = false; }); w.jconfirm.lastClicked = false; $(document).on("mousedown", "button, a, [jc-source]", function () { w.jconfirm.lastClicked = $(this); }); }));


error2 = function (message) {
	Swal.fire({
		icon: 'error',
		title: 'خطا',
		text: message,
		confirmButtonText: "بستن",
		customClass: {
			confirmButton: "btn btn-danger"
		}
	})
}

// Collect icon classes from same-origin CSS and render previews as DataURLs
async function collectProjectIcons(options = {}) {
	const {
		prefixes = ['fa', 'bi', 'la', 'ki'],
		size = 32,
		color = '#333'
	} = options;

	const isSameOrigin = (sheet) => {
		try { return !!sheet.cssRules; } catch { return false; }
	};

	const selectorRegexes = prefixes.map(function (p) { return new RegExp('\\.' + p + '-[\\w-]+::?before'); });
	const classNames = new Set();

	function normalizeIconClassTokens(tokens) {
		var set = new Set(tokens);
		var hasPrefix = function (prefix) { return tokens.some(function (t) { return t.indexOf(prefix + '-') === 0; }); };

		// Font Awesome: ensure a base/style class exists
		if (hasPrefix('fa')) {
			var faBases = ['fa', 'fas', 'far', 'fal', 'fab', 'fad', 'fa-solid', 'fa-regular', 'fa-light', 'fa-thin', 'fa-duotone', 'fa-brands'];
			var hasBase = faBases.some(function (b) { return set.has(b); });
			if (!hasBase) set.add('fa');
		}

		// Bootstrap Icons: ensure 'bi'
		if (hasPrefix('bi')) {
			if (!set.has('bi')) set.add('bi');
		}

		// Line Awesome: ensure a base exists (default 'la')
		if (hasPrefix('la')) {
			var laBases = ['la', 'las', 'lar', 'lab'];
			var hasLa = laBases.some(function (b) { return set.has(b); });
			if (!hasLa) set.add('la');
		}

		// Keenicons (Metronic): ensure a style class (default 'ki-duotone')
		if (hasPrefix('ki')) {
			var kiBases = ['ki', 'ki-outline', 'ki-duotone', 'ki-solid'];
			var hasKi = kiBases.some(function (b) { return set.has(b); });
			if (!hasKi) set.add('ki-duotone');
		}

		return Array.from(set).join(' ');
	}

	Array.from(document.styleSheets)
		.filter(isSameOrigin)
		.forEach(function (sheet) {
			Array.from(sheet.cssRules || []).forEach(function (rule) {
				if (!rule || rule.type !== CSSRule.STYLE_RULE || !rule.selectorText) return;
				var sel = rule.selectorText;
				if (!sel || (sel.indexOf(':before') === -1 && sel.indexOf('::before') === -1)) return;
				if (!selectorRegexes.some(function (rx) { return rx.test(sel); })) return;

				sel.split(',').forEach(function (part) {
					var pure = part.trim().replace(/::?before\s*$/i, '').trim();
					if (!pure || pure.charAt(0) !== '.') return;
					var tokens = pure.split('.').filter(Boolean);
					var normalized = normalizeIconClassTokens(tokens);
					classNames.add(normalized);
				});
			});
		});

	// Hidden container to compute ::before styles
	var container = document.createElement('div');
	container.style.cssText = 'position:fixed;left:-100000px;top:-100000px;visibility:hidden;';
	document.body.appendChild(container);

	var results = [];
	for (var className of classNames) {
		var el = document.createElement('span');
		el.className = className;
		container.appendChild(el);

		var before = getComputedStyle(el, '::before');
		var content = before.getPropertyValue('content') || '';
		content = content.replace(/^\s*["']?|["']?\s*$/g, '');
		if (!content || content.toLowerCase() === 'none') { container.removeChild(el); continue; }

		var fontFamily = before.getPropertyValue('font-family') || getComputedStyle(el).fontFamily || 'inherit';
		var fontWeight = before.getPropertyValue('font-weight') || '400';

		var image = (function renderGlyphToDataURL(char, opts) {
			var fontFamilyLocal = opts.fontFamily;
			var fontWeightLocal = opts.fontWeight || '400';
			var sizeLocal = opts.size || 32;
			var colorLocal = opts.color || '#333';

			var padX = Math.ceil(sizeLocal * 0.15);
			var padY = Math.ceil(sizeLocal * 0.10);

			var canvas = document.createElement('canvas');
			var ctx = canvas.getContext('2d');
			ctx.font = fontWeightLocal + ' ' + sizeLocal + 'px ' + fontFamilyLocal;
			var metrics = ctx.measureText(char);
			var width = Math.ceil(metrics.width) + padX * 2;
			var height = Math.ceil(sizeLocal * 1.2) + padY * 2;

			canvas.width = Math.max(width, 1);
			canvas.height = Math.max(height, 1);

			var ctx2 = canvas.getContext('2d');
			ctx2.font = fontWeightLocal + ' ' + sizeLocal + 'px ' + fontFamilyLocal;
			ctx2.textBaseline = 'top';
			ctx2.fillStyle = colorLocal;
			ctx2.fillText(char, padX, padY);

			return canvas.toDataURL('image/png');
		})(content, { fontFamily: fontFamily, fontWeight: fontWeight, size: size, color: color });

		results.push({ className: className, image: image });
		container.removeChild(el);
	}

	container.remove();
	return results;
}

// Expose globally for easy use
if (typeof window !== 'undefined') {
	window.collectProjectIcons = collectProjectIcons;
}

success2 = function (message) {
	Swal.fire({
		icon: 'success',
		text: message,
		confirmButtonText: "بستن",
		customClass: {
			confirmButton: "btn btn-success"
		}
	})
}

const initUploadFileDropzone = function ($el) {



	$el.find("[data-uploadFile=true]").each((c, i) => {

		var acceptedFiles = $(i).attr("data-uploadFileTypeFormat");
		var maxFileSize = $(i).attr("data-uploadFileMaxSize") ?? 10;
		var value = $(i).attr("value");



		var myDropzone = new Dropzone(i, {
			url: "/File/uploadFile", // Set the url for your upload script location
			paramName: "file", // The name that will be used to transfer the file
			maxFiles: 1,
			maxFilesize: maxFileSize, // MB
			addRemoveLinks: true,
			acceptedFiles: acceptedFiles,


		});

		if (value && value.length > 0) {


			let mockFile = { name: "Filename", size: 12345 };
			myDropzone.displayExistingFile(mockFile, value);

			$(myDropzone.element).find(".dz-success-mark").remove()
			$(myDropzone.element).find(".dz-error-mark").remove()
		}

		myDropzone.on("complete", function (file) {

			if (file.accepted === false) {
				error2("خطا در بارگذاری فایل");
				this.removeFile(file)
				return;
			}

			$(this.element).find(".dz-success-mark").remove()

			$(this.element).find(".dz-error-mark").remove()


			var type = file.type;

			var path = JSON.parse(file.xhr.response).data.path;
			var databindName = $(this.element).attr("data-uploadFileName");

			if (!file.type.includes("image")) {

				if (file.type.includes("sheet")) {
					$(this.element).find("img[data-dz-thumbnail]").attr("src", "/lib/webimg/filelogo/excel.jpg")
				}
				else if (file.type.includes("zip")) {
					$(this.element).find("img[data-dz-thumbnail]").attr("src", "/lib/webimg/filelogo/zip.jpg")
				}

				else if (file.type.includes("pdf")) {
					$(this.element).find("img[data-dz-thumbnail]").attr("src", "/lib/webimg/filelogo/pdf.jpg")
				}
				else {
					$(this.element).find("img[data-dz-thumbnail]").attr("src", "/lib/webimg/filelogo/rar.jpg")
				}

			}
			if ($(this.element).find(`[data-bind=${databindName}]`).length > 0) {
				$(this.element).find(`[data-bind=${databindName}]`).val(path);
			}
			else {
				$(this.element).append(`<input type="hidden" data-bind="${databindName}" value="${path}" />`)
			}

			//   $(this.element).find(".dz-success-mark").remove();
			// $(this.element).find(".dz-error-mark").remove();

			$(this.element).find("img[data-dz-thumbnail]").click(function () {

				if (file.dataURL) {
					let tmpFileName = Utils.Guid().replaceAll("-", "");
					Utils.downloadFile(file.dataURL.split(",")[1], type, tmpFileName + "." + type.split("/")[1])
				}
				else {
					let tmpFileName = Utils.Guid().replaceAll("-", "");
					let pathEl = $(file.previewElement).parent().find(`[data-bind=${databindName}]`).val();
					Utils.downloadFileByPath(pathEl, tmpFileName)
				}

			})

		});

		myDropzone.on("removedfile", function (file) {


			if (file.accepted === true) {

				var databindName = $(this.element).attr("data-uploadFileName");
				$(this.element).find(`[data-bind=${databindName}]`).val("")

			}
		});

		$(i).attr("data-uploadfile", "false");

	})


}
const persionDatePickerOptionsDateTime = {
	"format": "YYYY/MM/DD HH:mm:ss",
	"autoClose": true,
	"initialValue": false,
	"timePicker": { "enabled": true },
	 
};

const persionDatePickerOptionsDate = {
	"format": "YYYY/MM/DD",
	"autoClose": true,
	"initialValue": false,
	 
};

function InitDataTabel($el, columns, tabelName = "", path, searchBuilderOnButton = true, exportExcellPath = "/System/ReportBuilder/ExportDataToExcel" ) {
	let dataTableRequest = {};
	let fetchUrl = path ?? "/System/FetchData";
	let deleteUrl = "/System/Remove";
 
	let top2startInit = {};
	if (searchBuilderOnButton === false) {

		top2startInit = {
			searchBuilder: {
				liveSearch: false,
				conditions: {
					shamsidate: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						"between": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {

										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					shamsidatetime: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {

										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					datetime: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {
								 
								
								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										
										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					date: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					select: {
						"=": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<select/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)

								el.change(function () {
									fn(that, this);
								})


								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
					}
				}
			}
		}

	}
	else {

		top2startInit = {
			buttons: [{
				extend: 'searchBuilder',
				config: {
					liveSearch: false,
					conditions: {
						shamsidatetime: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {


									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {

											fn(that, el);
										}
									})
									$(el).on("change", function () {
										fn(that, this);
									})

									if (preDefined !== null && preDefined[0]) {
										$(el).val(preDefined[0]);
									}

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									if (preDefined !== null && preDefined[1]) {
										$(el2).val(preDefined[1]);
									}



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						},
						datetime: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						},
						date: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						},
						select: {
							"=": {
								init: function (that, fn, preDefined = null) {


									// Declare the input element
									let el = $('<select/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)

									el.change(function () {
										fn(that, this);
									})

									 
									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
						}
					}
				},
				className:"btn btn-outline btn-outline-warning "
			}],

		}
		
	}

	var table = new DataTable($el,{
		initComplete: function () {
			this.api()
				.columns()
				.every(function () {
					 
					var column = this;
					var title = column.header()?.textContent ?? "";
					var type = columns[column.index()].type;
					var place = column.header();
					if (place) {
						if (title.length > 0) {
							switch (type) {
								case 'string':
									$('<input type="text" class="form-control" placeholder="جستجو ' + title + '" />')
										.appendTo($(place).empty())

									break;
								case 'date':
								case 'shamsidate':


									var $divDateTime = $(`
									<div class="row">
									<div class="col-md-6" date-action="from"></div>
									<div class="col-md-6" date-action="to"></div>
									</div>`);


									$(place).empty();


									var fromInput = $('<input type="text" class="form-control" placeholder="از تاریخ" />')
										.appendTo($divDateTime.find("[date-action='from']"))
										.pDatepicker({
											format: 'YYYY/MM/DD',
											autoClose: true,
											initialValue: false,


										})



									var toInput = $('<input type="text" class="form-control" placeholder="تا تاریخ" />')
										.appendTo($divDateTime.find("[date-action='to']"))
										.pDatepicker({
											format: 'YYYY/MM/DD',
											autoClose: true,
											initialValue: false,

										})



									$divDateTime.appendTo(place)

									break;
								case 'datetime':
								case 'shamsidatetime':


									var $divDateTime = $(`
									<div class="row">
									<div class="col-md-6" date-action="from"></div>
									<div class="col-md-6" date-action="to"></div>
									</div>`);

									$(place).empty();


									var fromInput = $('<input type="text" class="form-control" placeholder="از تاریخ" />')
										.appendTo($divDateTime.find("[date-action='from']"))
										.pDatepicker({
											format: 'YYYY/MM/DD HH:mm:ss',
											autoClose: true,
											initialValue: false,
											timePicker: {
												enabled: true
											}

										})



									var toInput = $('<input type="text" class="form-control" placeholder="تا تاریخ" />')
										.appendTo($divDateTime.find("[date-action='to']"))
										.pDatepicker({
											format: 'YYYY/MM/DD HH:mm:ss',
											autoClose: true,
											initialValue: false,
											timePicker: {
												enabled: true
											}
										})



									$divDateTime.appendTo(place)


									break;
								case 'button':

									break;
								case 'select':

									var options = columns[column.index()].options;
									$('<select  class="form-control"  /> </select>')
										.append(`<option value="null">انتخاب کنید</option>`)
										.append(options.map(c => {
											return `<option value=${c.value}>${c.name}</option>`
										}))
										.appendTo($(place).empty())

									break;
								default:
									$('<input type="text" class="form-control" placeholder="جستجو ' + title + '" />')
										.appendTo($(place).empty())

									break;
							}
						}
					}
	

				});
		 
		},
		layout: {
			top2start: searchBuilderOnButton === false ? null : top2startInit,
			top2: searchBuilderOnButton === false ? top2startInit : null,
			top1start:null,
			topEnd: null,
			bottomStart: [{ pageLength: { text: "  تعداد در هر صفحه _MENU_  ", class: "mx-11" } }, 'info'],
			bottomEnd: 'paging',
			top1End: [
				{
					div: {
						className: 'btn btn-outline btn-outline-info mx-2',
						text: 'نمایش نتیجه',
						id:"draw",
					}
				},
				{
					div: {
						className: 'btn btn-outline btn-outline-primary',
						text: 'خروجی اکسل',
						id: "exportExcell",
					}
				}
			]
			 
		},
		language: {
			url: "/panelLib/plugins/custom/datatables/fa.json" 
 
		},
		"processing": true,
		"serverSide": true,
		"ajax": {
			"url": fetchUrl,
			"type": "POST",
			"contentType": "application/json",
		 
			"data": function (d,z,x) {
				  
				d.columns.forEach(d => {
					d.title = columns.firstOrDefault(c => c.data == d.data)?.title ?? "نامشخص"
					d.options = columns.firstOrDefault(c => c.data == d.data)?.options ?? [];
					d.type = columns.firstOrDefault(c => c.data == d.data)?.type ?? "string";
					d.tableName = columns.firstOrDefault(c => c.data == d.data)?.tableName ?? "";
					if (!Array.isArray(d.search.value)) {
						d.search.value = [];
					}
				})

				d.order = d.order.map(c => {
					return { column: d.columns[c.column].data, dir: c.dir }
				})
				d.tableName = tabelName;
				 
				d.search.value = [];
				d.searchBuilder = table.searchBuilder.getDetails()
				
				if (d.searchBuilder) {
					
					d.searchBuilder.criteria = addNameAndValueToSearchBuilder(d.searchBuilder.criteria, d.columns)
				}
				
				dataTableRequest = d;
				return JSON.stringify(d);
			},
			"dataSrc": function (json) {
				

				if (!json.isSuccess) return toastr.error(`${json.message}`, 'خطا');
				 
				columns.forEach(c => {
					if (c.sType === "bool") {
						json.data.forEach(d => {
							let v = d[c.data]
							if ((v ?? false) === true) {
								d[c.data] = "بله"
							}
							else {
								d[c.data] = "خیر"
							}
						})
					}
					else if (c.sType === "select") {
						if (c.options && c.options.length > 0) {
							json.data.forEach(d => {
								let v = d[c.data]
								d[c.name] = c.options.firstOrDefault(z => z.value === v?.toString())?.name ?? v
							})
						}
					}
				 
					
				})

		

				return json.data;
			},
			dataFilter: function (data) {

				var json = jQuery.parseJSON(data);
				var model = {
					isSuccess: json.isSuccess,
					message: json.message,
					...json.data
				}

				return JSON.stringify(model); // return JSON string
			}

		},
		"columns": columns,
          fixedColumns: true,
          scrollCollapse: true,
          scrollX: true,
		scrollY: '50vh'
	});
	table.on("draw", function () {

		 

		$(this).find(`[data-action=remove]`).each((i, c) => {

			$(c).click(function () {
				let url = $(this).attr("data-url");
				$.confirm({
					title: 'حذف اطلاعات',
					content: 'آیا از حذف کردن اطلاعات مطمئن هستید ؟',
					type: 'red',
					typeAnimated: true,
					buttons: {
						tryAgain: {
							text: 'بله',
							btnClass: 'btn-red',
							action: function () {
            					get(url, function (r) {
									if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');

									table.draw();
								})
							}
						},
						close: {
							text: 'بست',
							btnClass: 'btn',
							action: function () {
							}
						}
					}
				});


			});

		})
	
	})
 
	$(table.footer()[0])
		.find("button").click(function () {

			let needDraw = false;
			table.columns().every(function () {
				var col = this;
				var inputs = $(col.footer()).find("input");
				var select = $(col.footer()).find("select");

				if (select.length > 0) {
					if (select.val() != "null") {

						col.search([select.val()]);
						needDraw = true

					}
					else {
						col.search([]);
					}
				}
				else {
					if (inputs.length > 1) {
						let from = $(inputs[0]).val();
						let to = $(inputs[1]).val();

						if (from.length > 0 && to.length > 0) {
							 
							from = MJUtil.shamsiToMiladi(from)
							to = MJUtil.shamsiToMiladi(to)
							col.search([from, to])
							needDraw = true;
						}
					}
					else {


						if (inputs.val() != col.search()) {

							col.search([inputs.val()]);
							needDraw = true
						}

					}

				}


			})
			if (needDraw)
				table.draw();


			
		});
	 
	table.ready(() => {
		
		$("#draw").click(function () {
		
			table.draw();
			
		});

		$("#exportExcell").click(function () {
			let $btn = $(this).block();

			$.ajax({
				url: exportExcellPath,  
				method: 'POST',
				data: JSON.stringify(dataTableRequest),
				contentType: 'application/json',
				crossDomain: true,
				xhrFields: {
					responseType: 'blob'  
				},
				success: function (data, status, xhr) {
					// Create a Blob from the returned data
					var blob = new Blob([data], { type: xhr.getResponseHeader('Content-Type') });

		 
					var filename = "download.xlsx";
					var disposition = xhr.getResponseHeader('Content-Disposition');
					if (disposition && disposition.indexOf('attachment') !== -1) {
						var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
						var matches = filenameRegex.exec(disposition);
						if (matches != null && matches[1]) {
							filename = matches[1].replace(/['"]/g, '');
						}
					}

		 
					var link = document.createElement('a');
					var url = URL.createObjectURL(blob);
					link.href = url;
					link.download = filename;
					document.body.appendChild(link);
					link.click();
					document.body.removeChild(link);
					URL.revokeObjectURL(url); 
					$btn.block(false);
				},
				error: function (xhr, status, error) {
					alert("An error occurred while downloading the file.");
				}
			});

		});
 
		 
	});


	$.fn.dataTable.ext.errMode = function (settings, helpPage, message) {

		toastr.error("خطا در انجام فرایند" + message);
	};

	return table;
}
 

function InitDataTabelProfile($el, columns, profileId, searchBuilderOnButton = true) {
	let dataTableRequest = {};
	let fetchUrl =  "/System/FetchDataProfile";
	let deleteUrl = "/System/Remove";
	let searchBuilderCollapseId = $el.parent().parent().find("[data-place=searchBuilderCollapse]").attr("id");
	let dataProfileSelector = $el.closest(".card").find(`[data-action=dataProfile]`);
	 
	var newPath = dataProfileSelector.data("new-path")
	var editPath = dataProfileSelector.data("edit-path")
	var deletePath =dataProfileSelector.data("delete-path")
	var exportPath =dataProfileSelector.data("exportExcell-path")
 

	let canNew = (newPath && newPath.length > 0) ;
	let canEdit = (editPath && editPath.length > 0);
	let canExportExcell = (exportPath && exportPath.length > 0);
	let canDelete = (deletePath && deletePath.length > 0);
	let selectedRow = {};
	let $selectedRowEl ;

	let $editBtn, $deleteBtn, $newBtn, $exportEcellBtn, $drawBtn, $notificationbuilder;

	let handelEdit = function (id) {
		
		if (!canEdit) return toastr.error(`شما مجوز دسترسی برای ویرایش این اطلاعات ندارید`, 'عدم دسترسی');
		appController.addPage(editPath + "?id=" + id);
	}
	let handelDelete = function (id) {

		if (!canEdit) return toastr.error(`شما مجوز دسترسی برای حذف این اطلاعات ندارید`, 'عدم دسترسی');


		Swal.fire({
			icon: "warning",
			title: "آیا از حذف این اطلاعات مطمعن هستید ؟",
			showDenyButton: true,
			confirmButtonText: "بله",
			denyButtonText: `خیر`
		}).then((result) => {
			 
			if (result.isConfirmed) {
				get(deletePath + "?id=" + id, function (r) {
					if (!r.isSuccess) return error2(r.message);
					table.draw();
				});
			} else if (result.isDenied) {
				
			}
		});

		 
	}

	let top2startInit = {};
	if (searchBuilderOnButton === false) {

		top2startInit = {
			searchBuilder: {
				liveSearch: false,
				conditions: {
					shamsidate: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {

										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					shamsidatetime: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						"between": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {

										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}
					},
					datetime: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {


								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {

										fn(that, el);
									}
								})
								$(el).on("change", function () {
									fn(that, this);
								})

								if (preDefined !== null && preDefined[0]) {
									$(el).val(preDefined[0]);
								}

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								if (preDefined !== null && preDefined[1]) {
									$(el2).val(preDefined[1]);
								}



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD HH:mm:ss",
									"autoClose": true,
									"initialValue": false,
									"timePicker": { "enabled": true },
									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					},
					date: {
						"=": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},
						">": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"<": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								// If there is a preDefined value then add it
								if (preDefined !== null) {
									$(el).val(preDefined[0]);
								}

								return el;
							}
						},

						"between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						},
						"!between": {
							init: function (that, fn, preDefined = null) {

								// Declare the input element
								let el = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								let picker = el.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})

								let el2 = $('<input/>')
									.addClass(that.classes.value)
									.addClass(that.classes.input)


								picker = el2.persianDatepicker({
									"format": "YYYY/MM/DD",
									"autoClose": true,
									"initialValue": false,

									onSelect: function () {
										fn(that, this);
									}
								})



								return [el, el2];
							}
						}

					}
				}
			}
		}

	}
	else {

		top2startInit = {
			buttons: [{
				extend: 'searchBuilder',
				config: {
					liveSearch: false,
					conditions: {
						shamsidatetime: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {


									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {

											fn(that, el);
										}
									})
									$(el).on("change", function () {
										fn(that, this);
									})

									if (preDefined !== null && preDefined[0]) {
										$(el).val(preDefined[0]);
									}

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									if (preDefined !== null && preDefined[1]) {
										$(el2).val(preDefined[1]);
									}



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						},
						datetime: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD HH:mm:ss",
										"autoClose": true,
										"initialValue": false,
										"timePicker": { "enabled": true },
										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						},
						date: {
							"=": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},
							">": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"<": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									// If there is a preDefined value then add it
									if (preDefined !== null) {
										$(el).val(preDefined[0]);
									}

									return el;
								}
							},

							"between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							},
							"!between": {
								init: function (that, fn, preDefined = null) {

									// Declare the input element
									let el = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									let picker = el.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})

									let el2 = $('<input/>')
										.addClass(that.classes.value)
										.addClass(that.classes.input)


									picker = el2.persianDatepicker({
										"format": "YYYY/MM/DD",
										"autoClose": true,
										"initialValue": false,

										onSelect: function () {
											fn(that, this);
										}
									})



									return [el, el2];
								}
							}

						}
					}
				},
				className: "btn btn-outline btn-outline-warning "
			}],

		}

	}

 

	let top1startBtns = $(`
	<div class='mb-2 gap-1 d-md-flex justify-content-between align-items-center  col-md-auto me-auto'>
		<button   class='btn-sm btn btn-icon   btn-color-info btn-active-icon-dark ' data-action='draw' data-bs-toggle="tooltip" data-bs-placement="top" title="بارگذاری اطلاعات">
		    <i class='fs-2 fa-light fa-magnifying-glass-arrows-rotate fa-solid'  ></i>
		</button>
		<button ${canExportExcell == false ? "disabled" : "" }   class='btn-sm btn btn-icon   btn-color-success btn-active-icon-dark p-2' data-action='exportExcell' data-bs-toggle="tooltip" data-bs-placement="top" title="خروجی اکسل">
		    <i  class='fs-2 fa-light fa-file-xls'></i>

		</button>
	 
		<button ${canNew == false ? "disabled" : "" } class='btn-sm btn btn-icon btn-active-icon-dark btn-color-primary' data-action='new' data-bs-toggle="tooltip" data-bs-placement="top" title="جدید">
		   <i class="fs-2 fa-jelly fa-light fa-circle-plus"  ></i>
		</button>

		<button disabled class='btn-sm btn btn-icon btn-active-icon-dark  ' data-action='edit' data-bs-toggle="tooltip" data-bs-placement="top" title="ویرایش">
			<i class="fs-2 fa-light fa-pen-to-square"  ></i>
		</button>

		<button disabled class='btn-sm btn btn-icon btn-active-icon-dark ' data-action='delete' data-bs-toggle="tooltip" data-bs-placement="top" title="حذف">
		   <i class="fs-2 fa-light fa-trash"  ></i>
		</button>

		 <button   class='btn-sm btn btn-icon btn-active-icon-dark btn-color-info '  data-bs-toggle="collapse" data-bs-target="#${searchBuilderCollapseId}"    data-bs-placement="top" title="جستجوی پیشرفته">
		   <i class="fs-2 fa-light fa-magnifying-glass-plus "  ></i>
		</button>
		 <button   class='btn-sm btn btn-icon btn-active-icon-dark btn-color-warning'  data-action='notificationbuilder'   data-bs-toggle="tooltip" data-bs-placement="top" title="ایجاد اعلان">
 			<i class="ki-duotone ki-notification-on fs-1">
							<span class="path1"></span>
							<span class="path2"></span>
							<span class="path3"></span>	
							<span class="path4"></span>
							<span class="path5"></span>
						</i>
		 </button>

	
		 
	</div>
	`)

	$editBtn = top1startBtns.find("[data-action=edit]");
	$newBtn = top1startBtns.find("[data-action=new]");
	$deleteBtn = top1startBtns.find("[data-action=delete]");
	$exportEcellBtn = top1startBtns.find("[data-action=exportExcell]");
	$drawBtn = top1startBtns.find("[data-action=draw]");
	$notificationbuilder = top1startBtns.find("[data-action=notificationbuilder]");

	// Store filter popups for each column (using Map for better performance)
	const columnFilterPopups = new Map();
	
	// Store column references for better performance
	const columnReferences = new Map();
	
	// Track columns that are being cleared to prevent race conditions
	const columnsBeingCleared = new Set();
	
	// Flag to ensure document click handler is added only once
	let documentClickHandlerAdded = false;
	
	/**
	 * Closes all open popovers when clicking outside
	 */
	function setupDocumentClickHandler() {
		if (documentClickHandlerAdded) return;
		documentClickHandlerAdded = true;
		
		// Handle clicks outside popover to close them
		// Note: This handler runs after filter icon click handler due to event bubbling
		$(document).on('click', function(e) {
			const $target = $(e.target);
			
			// Check if click is on filter icon or inside popover
			const isClickOnFilterIcon = $target.closest('.filter-icon').length > 0;
			const isClickInsidePopover = $target.closest('.popover').length > 0;
			
			// Check if click is on datepicker calendar (Persian Datepicker)
			const isClickOnDatepicker = $target.closest('.datepicker-container').length > 0 || 
			                            $target.closest('.datepicker-plot-area').length > 0;
			
			// Check if click is on other common datepicker libraries
			const isClickOnOtherDatepicker = $target.closest('.flatpickr-calendar').length > 0 ||
			                                $target.closest('.tempus-dominus-widget').length > 0 ||
			                                $target.closest('.pwt-datepicker').length > 0;
			
			// Only close if click is outside icon, popover, and datepicker calendars
			if (!isClickOnFilterIcon && !isClickInsidePopover && !isClickOnDatepicker && !isClickOnOtherDatepicker) {
				// Close all open popovers
				columnFilterPopups.forEach(function(popoverInstance) {
					if (popoverInstance && popoverInstance._element) {
						try {
							const $icon = $(popoverInstance._element);
							// Check if popover is currently visible
							if ($icon.attr('aria-describedby')) {
								popoverInstance.hide();
							}
						} catch (err) {
							// Ignore errors if popover is already destroyed
							console.warn('Error closing popover:', err);
						}
					}
				});
			}
		});
	}
	
	/**
	 * Creates filter popup content based on column type
	 */
	function createFilterPopupContent(column, columnIndex, columnType) {
	 
		const columnData = columns[columnIndex];
		const filterId = `filter-popup-${columnIndex}`;
		let content = '';
		
		// Get current filter values
		const currentSearch = column ? column.search() : null;
		let stringValue = '';
		let selectValue = 'null';
		let booleanValue = 'null';
		let fromDateValue = '';
		let toDateValue = '';
		let fromDateTimeValue = '';
		let toDateTimeValue = '';
		
		// Parse current filter values
		if (currentSearch   && currentSearch.length > 0) {
			const typeLower = columnType.toLowerCase();
			if (typeLower === 'string') {
				stringValue = currentSearch[0] || '';
			} else if (typeLower === 'select') {
				selectValue = currentSearch[0] || 'null';
			} else if (typeLower === 'boolean' || typeLower === 'bool') {
				const boolVal = currentSearch[0];
				// Convert 1/0 or true/false to 'true'/'false' for select
				if (boolVal === '1' || boolVal === 1 || boolVal === 'true' || boolVal === true) {
					booleanValue = 'true';
				} else if (boolVal === '0' || boolVal === 0 || boolVal === 'false' || boolVal === false) {
					booleanValue = 'false';
				} else {
					booleanValue = 'null';
				}
			} else if (typeLower === 'date' || typeLower === 'shamsidate' || typeLower === 'dateshamsi' || 
			           typeLower === 'datetime' || typeLower === 'datetimeshamsi' || typeLower === 'shamsidatetime') {
						 
						currentSearch.toArray().forEach(function(val) {
					if (typeof val === 'string') {
						if (val.includes('from ')) {
							const dateStr = val.replace('from ', '');
							try {
								const date = new Date(dateStr);
								if (!isNaN(date.getTime())) {
									if (typeLower.includes('datetime')) {
										fromDateTimeValue = MJUtil.miladiToShamsi(date.toISOString());
									} else {
										fromDateValue = MJUtil.miladiToShamsi(date.toISOString());
									}
								}
							} catch(e) {
								console.warn('Error parsing from date:', e);
							}
						} else if (val.includes('to ')) {
							const dateStr = val.replace('to ', '');
							try {
								const date = new Date(dateStr);
								if (!isNaN(date.getTime())) {
									if (typeLower.includes('datetime')) {
										toDateTimeValue =MJUtil.miladiToShamsi(date.toISOString());
									} else {
										toDateValue = MJUtil.miladiToShamsi(date.toISOString());
									}
								}
							} catch(e) {
								console.warn('Error parsing to date:', e);
							}
						}
					}
				});
			}
		}
		
		// Escape HTML to prevent XSS
		const escapeHtml = (str) => {
			if (!str) return '';
			const div = document.createElement('div');
			div.textContent = str;
			return div.innerHTML;
		};
		
		switch (columnType.toLowerCase()) {
			case 'string':
				const escapedStringValue = escapeHtml(stringValue);
				content = `
					<div class="p-3">
						<div class="mb-3">
						 
							<input type="text" class="form-control filter-input" data-column-index="${columnIndex}" placeholder="مقدار را وارد کنید" value="${escapedStringValue}" />
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
				break;
				
			case 'date':
			case 'shamsidate':
			case 'dateshamsi':
				const escapedFromDateValue = escapeHtml(fromDateValue);
				const escapedToDateValue = escapeHtml(toDateValue);
				content = `
					<div class="p-3">
						<div class="mb-3">
				 
							<input type="text" class="form-control filter-date-from" data-column-index="${columnIndex}" placeholder="از تاریخ" value="${escapedFromDateValue}" />
						</div>
						<div class="mb-3">
							 
							<input type="text" class="form-control filter-date-to" data-column-index="${columnIndex}" placeholder="تا تاریخ" value="${escapedToDateValue}" />
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
				break;
				
			case 'datetime':
			case 'datetimeshamsi':
			case 'shamsidatetime':
				const escapedFromDateTimeValue = escapeHtml(fromDateTimeValue);
				const escapedToDateTimeValue = escapeHtml(toDateTimeValue);
				content = `
					<div class="p-3">
						<div class="mb-3">
						 
							<input type="text" class="form-control filter-datetime-from" data-column-index="${columnIndex}" placeholder="از تاریخ و زمان" value="${escapedFromDateTimeValue}" />
						</div>
						<div class="mb-3">
 
							<input type="text" class="form-control filter-datetime-to" data-column-index="${columnIndex}" placeholder="تا تاریخ و زمان" value="${escapedToDateTimeValue}" />
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
				break;
				
			case 'select':
				const options = columnData.options || [];
				const optionsHtml = options.map(opt => {
					const value = escapeHtml(String(opt.value || ''));
					const name = escapeHtml(String(opt.name || ''));
					const selected = (value === selectValue) ? 'selected' : '';
					return `<option value="${value}" ${selected}>${name}</option>`;
				}).join('');
				const selectedNull = (selectValue === 'null') ? 'selected' : '';
				content = `
					<div class="p-3">
						<div class="mb-3">
							<label class="form-label">انتخاب کنید:</label>
							<select class="form-control filter-select" data-column-index="${columnIndex}">
								<option value="null" ${selectedNull}>انتخاب کنید</option>
								${optionsHtml}
							</select>
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
				break;
				
			case 'boolean':
			case 'bool':
				const selectedTrue = (booleanValue === 'true') ? 'selected' : '';
				const selectedFalse = (booleanValue === 'false') ? 'selected' : '';
				const selectedBoolNull = (booleanValue === 'null') ? 'selected' : '';
				content = `
					<div class="p-3">
						<div class="mb-3">
							<label class="form-label">انتخاب کنید:</label>
							<select class="form-control filter-boolean" data-column-index="${columnIndex}">
								<option value="null" ${selectedBoolNull}>انتخاب کنید</option>
								<option value="true" ${selectedTrue}>بله</option>
								<option value="false" ${selectedFalse}>خیر</option>
							</select>
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
				break;
				
			case 'button':
				// No filter for button columns
				return '';
				
			default:
				const escapedDefaultStringValue = escapeHtml(stringValue);
				content = `
					<div class="p-3">
						<div class="mb-3">
						 
							<input type="text" class="form-control filter-input" data-column-index="${columnIndex}" placeholder="مقدار را وارد کنید" value="${escapedDefaultStringValue}" />
						</div>
						<div class="d-flex gap-2 justify-content-end">
							<button type="button" class="btn btn-sm btn-secondary filter-clear" data-column-index="${columnIndex}">پاک کردن</button>
							<button type="button" class="btn btn-sm btn-primary filter-apply" data-column-index="${columnIndex}">اعمال</button>
						</div>
					</div>
				`;
		}
		
		return content;
	}
	
	/**
	 * Updates filter icon color based on active filters
	 */
	function updateFilterIcon(columnIndex, hasFilter) {
		const $icon = $(`.filter-icon[data-column-index="${columnIndex}"]`);
	 
		if ($icon.length) {
			if (hasFilter) {
				 
				$icon.removeClass('text-muted').addClass('text-warning');
			} else {
				$icon.removeClass('text-warning').addClass('text-muted');
			}
		}
	}
	
	/**
	 * Checks if column has active filter
	 */
	function hasActiveFilter(column) {
		if (!column) {
			return false;
		}
		 
		try {
			const searchValue = column.search();
			
			// If searchValue is null, undefined, or empty string, no filter
			if (!searchValue || searchValue.length === 0) {
				return false;
			}
			  
			// If it's a string, check if it's not empty
			const strValue = searchValue[0].toString().trim();
			return strValue.length > 0;
		} catch (e) {
			console.warn('Error checking active filter:', e);
			return false;
		}
	}
	
	/**
	 * Applies filter to column
	 */
	function applyColumnFilter(columnIndex, column, columnType) {
		const columnData = columns[columnIndex];
		const $popup = $(`.filter-popup[data-column-index="${columnIndex}"]`);
		
		switch (columnType.toLowerCase()) {
			case 'string':
				const stringValue = $popup.find('.filter-input').val()?.trim() || '';
				if (stringValue) {
					column.search([stringValue]);
				} else {
					column.search([]);
				}
				break;
				
			case 'date':
			case 'shamsidate':
			case 'dateshamsi':
				const fromDate = $popup.find('.filter-date-from').val()?.trim() || '';
				const toDate = $popup.find('.filter-date-to').val()?.trim() || '';
				const dateValues = [];
				if (fromDate) {
					try {
						const fromInput = $popup.find('.filter-date-from')[0];
						if (fromInput) {
							let date;
							if (fromInput.model && fromInput.model.selected) {
								date = new Date(fromInput.model.selected);
							} else {
								// Try parsing the value directly
								date = new Date(MJUtil.shamsiToMiladi(fromDate));
							}
							if (!isNaN(date.getTime())) {
								dateValues.push(`from ${moment(date).format('yyyy-MM-DD HH:mm:ss')}`);
							}
						}
					} catch (e) {
						console.warn('Error parsing from date:', e);
					}
				}
				if (toDate) {
					try {
						const toInput = $popup.find('.filter-date-to')[0];
						if (toInput) {
							let date;
							if (toInput.model && toInput.model.selected) {
								date = new Date(toInput.model.selected);
							} else {
								// Try parsing the value directly
								date = new Date(MJUtil.shamsiToMiladi(toDate));
							}
							if (!isNaN(date.getTime())) {
								dateValues.push(`to ${moment(date).format('yyyy-MM-DD HH:mm:ss')}`);
							}
						}
					} catch (e) {
						console.warn('Error parsing to date:', e);
					}
				}
				column.search(dateValues);
				break;
				
			case 'datetime':
			case 'datetimeshamsi':
			case 'shamsidatetime':
				const fromDateTime = $popup.find('.filter-datetime-from').val()?.trim() || '';
				const toDateTime = $popup.find('.filter-datetime-to').val()?.trim() || '';
				const datetimeValues = [];
				if (fromDateTime) {
					try {
						const fromInput = $popup.find('.filter-datetime-from')[0];
						if (fromInput) {
							let date;
							if (fromInput.model && fromInput.model.selected) {
								date = new Date(fromInput.model.selected);
							} else {
								// Try parsing the value directly
								date = new Date(MJUtil.shamsiToMiladi(fromDateTime));
							}
							if (!isNaN(date.getTime())) {
								datetimeValues.push(`from ${moment(date).format('yyyy-MM-DD HH:mm:ss')}`);
							}
						}
					} catch (e) {
						console.warn('Error parsing from datetime:', e);
					}
				}
				if (toDateTime) {
					try {
						const toInput = $popup.find('.filter-datetime-to')[0];
						if (toInput) {
							let date;
							if (toInput.model && toInput.model.selected) {
								date = new Date(toInput.model.selected);
							} else {
								// Try parsing the value directly
								date = new Date(MJUtil.shamsiToMiladi(toDateTime));
							}
							if (!isNaN(date.getTime())) {
								datetimeValues.push(`to ${moment(date).format('yyyy-MM-DD HH:mm:ss')}`);
							}
						}
					} catch (e) {
						console.warn('Error parsing to datetime:', e);
					}
				}
				column.search(datetimeValues);
				break;
				
			case 'select':
				const selectValue = $popup.find('.filter-select').val();
				if (selectValue && selectValue !== 'null') {
					column.search([selectValue]);
				} else {
					column.search([]);
				}
				break;
				
			case 'boolean':
			case 'bool':
				const booleanSelectValue = $popup.find('.filter-boolean').val();
				if (booleanSelectValue && booleanSelectValue !== 'null') {
					// Convert 'true'/'false' to 1/0 for server
					const boolValue = (booleanSelectValue === 'true') ? '1' : '0';
					column.search([boolValue]);
				} else {
					column.search([]);
				}
				break;
				
			default:
				const defaultValue = $popup.find('.filter-input').val()?.trim() || '';
				if (defaultValue) {
					column.search([defaultValue]);
				} else {
					column.search([]);
				}
		}
		
		updateFilterIcon(columnIndex, hasActiveFilter(column));
		table.draw();
	}
	
	/**
	 * Clears filter for column
	 */
	function clearColumnFilter(columnIndex, column) {
		// Mark this column as being cleared
		columnsBeingCleared.add(columnIndex);
		
		// Clear popup inputs first
		const $popup = $(`.filter-popup[data-column-index="${columnIndex}"]`);
		if ($popup.length) {
			$popup.find('input').val('');
			$popup.find('select').val('null');
			$popup.find('.filter-boolean').val('null');
			
			// Clear datepicker values if they exist
			$popup.find('.filter-date-from, .filter-date-to, .filter-datetime-from, .filter-datetime-to').each(function() {
				const $input = $(this);
				if ($input[0] && $input[0].model) {
					$input[0].model.selected = null;
				}
				$input.val('');
			});
		}
		
		// Clear the search - use empty array to ensure filter is cleared
		if (column) {
			column.search([]);
		}
		
		// Force update icon immediately to reflect cleared state
		updateFilterIcon(columnIndex, false);
		
		// Draw table - the draw event will verify the icon state
		table.draw();
		
		// Remove from cleared set after draw completes and ensure icon stays cleared
		setTimeout(function() {
			// Double-check that filter is actually cleared
			if (column) {
				const searchValue = column.search();
				const isActuallyCleared = !searchValue || 
					(Array.isArray(searchValue) && searchValue.length === 0) ||
					(Array.isArray(searchValue) && !searchValue.some(v => v != null && v.toString().trim().length > 0));
				
				if (isActuallyCleared) {
					updateFilterIcon(columnIndex, false);
				}
			} else {
				// If column reference is lost, just set to inactive
				updateFilterIcon(columnIndex, false);
			}
			
			// Remove from cleared set after a delay to allow draw event to complete
			setTimeout(function() {
				columnsBeingCleared.delete(columnIndex);
			}, 200);
		}, 150);
	}

	let tableApi = null;
	// Store row render callbacks
	const rowRenderCallbacks = [];
	
	var table = new DataTable($el, {
		rowCallback: function(row, data) {
			// Call all registered row render callbacks
			const $row = $(row);
			rowRenderCallbacks.forEach(function(callback) {
				if (typeof callback === 'function') {
					try {
						callback(data, $row);
					} catch (err) {
						console.warn('Error in row render callback:', err);
					}
				}
			});
		},
		initComplete: function () {
			tableApi = this.api();
			const api = tableApi;
			
			// Add onRowRender method to table API for registering row render callbacks
			api.onRowRender = function(callback) {
				if (typeof callback === 'function') {
					rowRenderCallbacks.push(callback);
					
					// Apply callback to existing rows immediately
					// This ensures callback works on first load as well
					api.rows().every(function() {
						const rowData = this.data();
						const rowNode = this.node();
						if (rowNode) {
							try {
								callback(rowData, $(rowNode));
							} catch (err) {
								console.warn('Error in row render callback:', err);
							}
						}
					});
				} else {
					console.warn('onRowRender callback must be a function');
				}
				return api; // Return API for chaining
			};
			
			// Also add to table instance for direct access
			table.onRowRender = api.onRowRender;
			
			const thead = $(api.table().header());

			// Initialize filter icons and popups for each column
			api.columns().every(function () {
				const column = this;
				const columnIndex = column.index();
				const columnHeader = column.header();
				const columnType = columns[columnIndex]?.type || 'string';
				
				if (!columnHeader || !column.visible()) {
					return;
				}
				
				const $header = $(columnHeader);
				const title = $header.text().trim();
				
				if (title.length === 0 || columnType.toLowerCase() === 'button') {
					return;
				}
				
				// Store column reference for later use
				columnReferences.set(columnIndex, column);
				
				// Create filter icon with proper escaping
				const $filterIcon = $('<i>')
					.addClass('fa-light fa-filter filter-icon text-muted ')
					.css({ cursor: 'pointer', fontSize: '0.9em' })
					.attr('data-column-index', columnIndex)
					.attr('data-bs-toggle', 'popover')
					.attr('data-bs-trigger', 'manual') // Use manual trigger to have full control
					.attr('data-bs-placement', 'bottom')
					.attr('data-bs-html', 'true')
					.attr('title', 'فیلتر');
				
				// Prevent sort when clicking on filter icon and toggle popover
				// Use capture phase to handle before DataTable's sort handler
				$filterIcon[0].addEventListener('click', function(e) {
					// Stop event propagation to prevent sort
					e.stopPropagation();
					e.stopImmediatePropagation();
					e.preventDefault();
					
					// Toggle popover manually
					const popoverInstance = bootstrap.Popover.getInstance($filterIcon[0]);
					if (popoverInstance) {
						// Check if popover is currently shown
						const isShown = $filterIcon.attr('aria-describedby');
						if (isShown) {
							popoverInstance.hide();
						} else {
							// Hide other popovers first
							columnFilterPopups.forEach(function(otherPopover, otherIndex) {
								if (otherIndex !== columnIndex && otherPopover && otherPopover._element) {
									const $otherIcon = $(otherPopover._element);
									if ($otherIcon.attr('aria-describedby')) {
										otherPopover.hide();
									}
								}
							});
							popoverInstance.show();
						}
					}
					
					return false; // Additional prevention
				}, true); // Use capture phase
				
				// Append icon to header
				$header.append($filterIcon);
				
				// Initialize popover with proper configuration
				// Content will be generated dynamically when popover is shown
				const popover = new bootstrap.Popover($filterIcon[0], {
					content: function() {
						// Generate content dynamically to get latest filter values
						return createFilterPopupContent(column, columnIndex, columnType);
					},
					html: true,
					placement: 'bottom',
					trigger: 'manual', // Manual trigger for better control
					container: 'body',
					sanitize: false // We control the content, so no need for sanitization
				});
				
				// Store popover reference
				columnFilterPopups.set(columnIndex, popover);
				
				// Setup document click handler to close popovers (only once)
				setupDocumentClickHandler();
					
					// Initialize date pickers and bind events when popover is shown
					$filterIcon.on('shown.bs.popover', function () {
						// Use setTimeout to ensure DOM is ready
						setTimeout(function() {
							// Find the popover that was just shown (use the icon's popover instance)
							const popoverInstance = bootstrap.Popover.getInstance($filterIcon[0]);
							if (!popoverInstance) return;
							
							const popoverElement = popoverInstance.tip;
							if (!popoverElement) return;
							
							const $popoverContent = $(popoverElement).find('.popover-body');
							if (!$popoverContent.length) return;
							
							$popoverContent.addClass('filter-popup').attr('data-column-index', columnIndex);
							const $popup = $popoverContent;
							
							// Get column type
							const typeLower = columnType.toLowerCase();
							
							// Initialize date pickers based on type (values are already in HTML)
							if (typeLower === 'date' || typeLower === 'shamsidate' || typeLower === 'dateshamsi') {
								const $fromInput = $popup.find('.filter-date-from');
								const $toInput = $popup.find('.filter-date-to');
								
								// Initialize from datepicker if not already initialized
								if ($fromInput.length && !$fromInput.data('pDatepicker')) {
									// Get value from input (already set in HTML)
									const fromValue = $fromInput.val();
									
									$fromInput.pDatepicker({
										format: 'YYYY/MM/DD',
										autoClose: true,
										initialValue: false,
										onSelect: function() {}
									});
									
									// Set date value if exists
									if (fromValue) {
										try {
											const date = new Date(fromValue);
											if (!isNaN(date.getTime())) {
												setTimeout(function() {
													const fromPicker = $fromInput.data('pDatepicker');
													if (fromPicker && typeof fromPicker.setDate === 'function') {
														fromPicker.setDate(date.getTime());
													}
												}, 50);
											}
										} catch(e) {
											console.warn('Error setting from date:', e);
										}
									}
								}
								
								// Initialize to datepicker if not already initialized
								if ($toInput.length && !$toInput.data('pDatepicker')) {
									// Get value from input (already set in HTML)
									const toValue = $toInput.val();
									
									$toInput.pDatepicker({
										format: 'YYYY/MM/DD',
										autoClose: true,
										initialValue: false,
										onSelect: function() {}
									});
									
									// Set date value if exists
									if (toValue) {
										try {
											const date = new Date(toValue);
											if (!isNaN(date.getTime())) {
												setTimeout(function() {
													const toPicker = $toInput.data('pDatepicker');
													if (toPicker && typeof toPicker.setDate === 'function') {
														toPicker.setDate(date.getTime());
													}
												}, 50);
											}
										} catch(e) {
											console.warn('Error setting to date:', e);
										}
									}
								}
							} else if (typeLower === 'datetime' || typeLower === 'datetimeshamsi' || typeLower === 'shamsidatetime') {
								const $fromInput = $popup.find('.filter-datetime-from');
								const $toInput = $popup.find('.filter-datetime-to');
								
								// Initialize from datetime picker if not already initialized
								if ($fromInput.length && !$fromInput.data('pDatepicker')) {
									// Get value from input (already set in HTML)
									const fromValue = $fromInput.val();
									
									$fromInput.pDatepicker({
										format: 'YYYY/MM/DD HH:mm:ss',
										autoClose: true,
										initialValue: false,
										timePicker: { enabled: true },
										onSelect: function() {}
									});
									
									// Set datetime value if exists
									if (fromValue) {
										try {
											const date = new Date(fromValue);
											if (!isNaN(date.getTime())) {
												setTimeout(function() {
													const fromPicker = $fromInput.data('pDatepicker');
													if (fromPicker && typeof fromPicker.setDate === 'function') {
														fromPicker.setDate(date.getTime());
													}
												}, 50);
											}
										} catch(e) {
											console.warn('Error setting from datetime:', e);
										}
									}
								}
								
								// Initialize to datetime picker if not already initialized
								if ($toInput.length && !$toInput.data('pDatepicker')) {
									// Get value from input (already set in HTML)
									const toValue = $toInput.val();
									
									$toInput.pDatepicker({
										format: 'YYYY/MM/DD HH:mm:ss',
										autoClose: true,
										initialValue: false,
										timePicker: { enabled: true },
										onSelect: function() {}
									});
									
									// Set datetime value if exists
									if (toValue) {
										try {
											const date = new Date(toValue);
											if (!isNaN(date.getTime())) {
												setTimeout(function() {
													const toPicker = $toInput.data('pDatepicker');
													if (toPicker && typeof toPicker.setDate === 'function') {
														toPicker.setDate(date.getTime());
													}
												}, 50);
											}
										} catch(e) {
											console.warn('Error setting to datetime:', e);
										}
									}
								}
							}
							
							// Prevent clicks inside popover from closing it and triggering sort
							$popoverContent.on('click', function(e) {
								e.stopPropagation();
								e.stopImmediatePropagation();
							});
							
							// Also prevent clicks on popover container
							$(popoverElement).on('click', function(e) {
								e.stopPropagation();
							});
							
							// Prevent clicks on datepicker inputs from closing popover
							$popoverContent.on('click', '.filter-date-from, .filter-date-to, .filter-datetime-from, .filter-datetime-to', function(e) {
								e.stopPropagation();
							});
							
							// Bind filter apply button (use event delegation)
							$popoverContent.off('click', '.filter-apply').on('click', '.filter-apply', function(e) {
								e.preventDefault();
								e.stopPropagation();
								applyColumnFilter(columnIndex, column, columnType);
								if (popover) {
									popover.hide();
								}
							});
							
							// Bind filter clear button
							$popoverContent.off('click', '.filter-clear').on('click', '.filter-clear', function(e) {
								e.preventDefault();
								e.stopPropagation();
								clearColumnFilter(columnIndex, column);
								if (popover) {
									popover.hide();
								}
							});
							
							// Bind Enter key for text inputs
							$popoverContent.off('keypress', '.filter-input').on('keypress', '.filter-input', function(e) {
								if (e.which === 13) {
									e.preventDefault();
									e.stopPropagation();
									applyColumnFilter(columnIndex, column, columnType);
									if (popover) {
										popover.hide();
									}
								}
							});
						}, 100);
					});
				
				// Update filter icon on initial load
				updateFilterIcon(columnIndex, hasActiveFilter(column));
			});

 
			var sb = this.api().searchBuilder.container();
			 
			$(api.context[0].nTableWrapper).parent().parent()
				.find(`#${searchBuilderCollapseId}`).append(sb);


		},
		layout: {
			top2start: searchBuilderOnButton === false ? null : top2startInit,
			top2: searchBuilderOnButton === false ? top2startInit : null,
			top1start: top1startBtns ,
			topEnd: null,
			bottomStart: [{ pageLength: { text: "  تعداد در هر صفحه _MENU_  ", class: "mx-11" } }, 'info'],
			bottomEnd: 'paging',
			top1End: null,
			topStart:null

		},
		language: {
			url: "/panelLib/plugins/custom/datatables/fa.json"

		},
		"processing": true,
		"serverSide": true,
		"ajax": {
			"url": fetchUrl,
			"type": "POST",
			"contentType": "application/json",

			"data": function (d, z, x) {
				 
				 
				d.columns.forEach(d => {
					d.title = columns.firstOrDefault(c => c.data == d.data)?.title ?? "نامشخص"
					d.options = columns.firstOrDefault(c => c.data == d.data)?.options ?? [];
					d.type = columns.firstOrDefault(c => c.data == d.data)?.type ?? "string";
					d.tableName = columns.firstOrDefault(c => c.data == d.data)?.tableName ?? "";
					if (!Array.isArray(d.search.value)) {
						if (d.search.value.length > 0) {
							d.search.value = [d.search.value];
						}
						else {
							d.search.value = [];
						}
						
					}
				})

				d.order = d.order.map(c => {
					return { column: d.columns[c.column].data, dir: c.dir }
				})
				d.profileId = profileId;

				d.search.value = [];
				d.searchBuilder = table.searchBuilder.getDetails()
				 
				if (d.searchBuilder) {
					try {

						d.searchBuilder.criteria = addNameAndValueToSearchBuilder(d.searchBuilder.criteria, d.columns)
					}
					catch (e) {
						$(z.nTableWrapper).find("[role=status]").css("display", "none");
						throw null;
					}
				}

				dataTableRequest = d;
				return JSON.stringify(d);
			},
			"dataSrc": function (json) {


				if (!json.isSuccess) return toastr.error(`${json.message}`, 'خطا');

				columns.forEach(c => {
					 
					if (c.sType === "boolean" || c.sType === "bool") {
						json.data.forEach(d => {
							let v = d[c.data]
							if ((v ?? false) === true) {
								d[c.data] = "بله"
							}
							else {
								d[c.data] = "خیر"
							}
						})
					}
					else if (c.sType === "select") {
						if (c.options && c.options.length > 0) {
							json.data.forEach(d => {
								let v = d[c.data]
								d[c.name] = c.options.firstOrDefault(z => z.value === v?.toString())?.name ?? v
							})
						}
						 
					}


				})



				return json.data;
			},
			 dataFilter: function (data) {

				var json = jQuery.parseJSON(data);
				var model = {
					isSuccess: json.isSuccess,
					message: json.message,
					...json.data
				}

				return JSON.stringify(model); // return JSON string
			}

		},
		"columns": columns,
		fixedColumns: true,
		scrollCollapse: true,
		scrollX: true,
		scrollY: '50vh',
		 
	});
	table.on("draw", function () {
		// Apply row render callbacks to all visible rows after draw
		// This ensures callbacks are applied even on first load and after every draw
		if (tableApi && rowRenderCallbacks.length > 0) {
			tableApi.rows({ page: 'current' }).every(function() {
				const rowData = this.data();
				const rowNode = this.node();
				if (rowNode) {
					const $row = $(rowNode);
					rowRenderCallbacks.forEach(function(callback) {
						if (typeof callback === 'function') {
							try {
								callback(rowData, $row);
							} catch (err) {
								console.warn('Error in row render callback:', err);
							}
						}
					});
				}
			});
		}
		
		// Update filter icons after draw (use stored references for better performance)
		if (tableApi) {
			columnReferences.forEach(function(column, columnIndex) {
				// Skip updating icon if this column is currently being cleared
				// This prevents race condition where draw event fires before clear is complete
				if (columnsBeingCleared.has(columnIndex)) {
					// Force icon to inactive state during clearing
					updateFilterIcon(columnIndex, false);
					return;
				}
				
				// Check if filter is actually active
				const hasFilter = hasActiveFilter(column);
				updateFilterIcon(columnIndex, hasFilter);
			});
		}

		// Handle remove action buttons
		$(this).find(`[data-action=remove]`).each((i, c) => {
			$(c).off('click').on('click', function () {
				const url = $(this).attr("data-url");
				if (!url) return;
				
				$.confirm({
					title: 'حذف اطلاعات',
					content: 'آیا از حذف کردن اطلاعات مطمئن هستید ؟',
					type: 'red',
					typeAnimated: true,
					buttons: {
						tryAgain: {
							text: 'بله',
							btnClass: 'btn-red',
							action: function () {
								get(url, function (r) {
									if (!r.isSuccess) {
										return toastr.error(`${r.message}`, 'خطا');
									}
									table.draw();
								});
							}
						},
						close: {
							text: 'بستن',
							btnClass: 'btn',
							action: function () {}
						}
					}
				});
			});
		});
	});

	table.ready(() => {
		$drawBtn.click(function () {

			table.draw();

		});
		$exportEcellBtn.click(function () {
			let $btn = $(this).block();
			$.ajax({
				url: exportPath,
				method: 'POST',
				data: JSON.stringify(dataTableRequest),
				contentType: 'application/json',
				crossDomain: true,
				xhrFields: {
					responseType: 'blob'
				},
				success: function (data, status, xhr) {
					// Create a Blob from the returned data
					var blob = new Blob([data], { type: xhr.getResponseHeader('Content-Type') });


					var filename = "download.xlsx";
					var disposition = xhr.getResponseHeader('Content-Disposition');
					if (disposition && disposition.indexOf('attachment') !== -1) {
						var filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
						var matches = filenameRegex.exec(disposition);
						if (matches != null && matches[1]) {
							filename = matches[1].replace(/['"]/g, '');
						}
					}


					var link = document.createElement('a');
					var url = URL.createObjectURL(blob);
					link.href = url;
					link.download = filename;
					document.body.appendChild(link);
					link.click();
					document.body.removeChild(link);
					URL.revokeObjectURL(url);
					$btn.block(false);
				},
				error: function (xhr, status, error) {
					$btn.block(false);
					alert("An error occurred while downloading the file.");
				}
			});
		});
		$newBtn.click(() => appController.addPage(newPath));
		appController.createBootstrapTooltips();
		$editBtn.click(() => handelEdit(selectedRow.id));
		$deleteBtn.click(() => handelDelete(selectedRow.id));
		$drawBtn.parent().parent().parent().addClass("datatabel-action-btns");
		$notificationbuilder.click((e) => openNotifictionBuilder(e, profileId))
	});

	table.on('dblclick', 'tbody tr', (e) => {
		e.preventDefault();

		if (canEdit === true) {
			selectedRow = table.row(e.currentTarget).data()
			handelEdit(selectedRow.id)
		}
	})

	table.on('click', 'tbody tr', (e) => {
		let classList = e.currentTarget.classList;

		if (classList.contains('selected')) {
			classList.remove('selected');
			$selectedRowEl = null;
			selectedRow = null;
			$editBtn.prop("disabled", true).removeClass("btn-color-warning");
			$deleteBtn.prop("disabled", true).removeClass("btn-color-danger");

		}
		else {
			 
			table.rows('.selected').nodes().each((row) => row.classList.remove('selected'));
			classList.add('selected');
			$selectedRowEl = $(e.currentTarget);
			selectedRow = table.row(e.currentTarget).data()

			if (canEdit === true) {
				$editBtn.prop("disabled", false).addClass("btn-color-warning")
			}
			else {
				$editBtn.prop("disabled", true).removeClass("btn-color-warning")
			}
			if (canDelete === true) {
				$deleteBtn.prop("disabled", false).addClass("btn-color-danger")
			}
			else {
				$deleteBtn.prop("disabled", true).removeClass("btn-color-danger")
			}
		}
		
	});


	$.fn.dataTable.ext.errMode = function (settings, helpPage, message) {

		toastr.error("خطا در انجام فرایند" + message);
	};


	return table;
}

function getColFilters(table){
	table.columns().every(function () {
		var col = this;
		var inputs = $(col.footer()).find("input");
		var select = $(col.footer()).find("select");

		if (select.length > 0) {
			if (select.val() != "null") {

				col.search([select.val()]);
				needDraw = true

			}
			else {
				col.search([]);
			}
		}
		else {
			if (inputs.length > 1) {
				let from = $(inputs[0]).val();
				let to = $(inputs[1]).val();

				if (from.length > 0 && to.length > 0) {

					from = MJUtil.shamsiToMiladi(from)
					to = MJUtil.shamsiToMiladi(to)
					col.search([from, to])
					needDraw = true;
				}
			}
			else {


				if (inputs.val() != col.search()) {

					col.search([inputs.val()]);
					needDraw = true
				}

			}

		}


	})
}

function addNameAndValueToSearchBuilder(criteria, cols) {
	if (criteria) {
		if (Array.isArray(criteria)) {

			criteria.forEach(xc => {
				if (xc.criteria && xc.criteria.length > 0) {
					xc.criteria.forEach(cr => {
						cr = addNameAndValueToSearchBuilder(cr, cols);
					})
				}
				else {

					if (!xc.origData) {
						toastr.error("یکی از شرط های جستجوی پیشرفته مقدار دهی نشده است لطفا مقدار را وارد کنید . ")
						throw null;
					}

					var col = cols.firstOrDefault(c => c.data == xc.origData);
					xc.name = col.name;
					 
					if ((xc.value.length === 0 || xc.value.any(c => c === "") || xc.value.any(c => c === null)) && (xc.condition != 'null' && xc.condition != "!null")) {
						toastr.error("یکی از شرط های جستجوی پیشرفته مقدار دهی نشده است لطفا مقدار را وارد کنید . ")
						throw null;
					}

					if (col.type === "date" || col.type === "datetime" || col.type === "shamsidatetime" || col.type === "shamsidate") {
						xc.value = xc.value.map(dt => {
							return MJUtil.shamsiToMiladi(dt)
						})
					}
				}
			})
		}
		else {
			if (criteria.criteria && criteria.criteria.length > 0) {
				criteria.criteria.forEach(cr => {
					cr = addNameAndValueToSearchBuilder(cr, cols);
				})
			}
			else {

				if (!criteria.origData) {
					toastr.error("یکی از شرط های جستجوی پیشرفته مقدار دهی نشده است لطفا مقدار را وارد کنید . ")
					throw null;
				}

				var col = cols.firstOrDefault(c => c.data == criteria.origData);
				criteria.name = col.name;
				 
				if ((criteria.value.length === 0 || criteria.value.any(c => c === "") || criteria.value.any(c => c === null)) && (criteria.condition != 'null' && criteria.condition != "!null")) {
					toastr.error("یکی از شرط های جستجوی پیشرفته مقدار دهی نشده است لطفا مقدار را وارد کنید . ")
					throw null ;
				}

				if (col.type === "date" || col.type === "datetime" || col.type === "shamsidatetime" || col.type ==="shamsidate") {
					col.value = col.value.map(dt => {
						return MJUtil.shamsiToMiladi(dt)
					})
				}
				
			}
		}

	}
	
			
	return criteria
}

function openNotifictionBuilder($btn, profileId) {
	 
	$($btn.delegateTarget).block();


	get(`/NotifictionBuilder/EntityInfoByProfile/${profileId}` , function (r) {
		$($btn.delegateTarget).block(false);
		if (!r.isSuccess) return error2(r.message);
		 
		var entityData = r.data.entityData;
		var existRules = r.data.existRules;
		const entityFullName = entityData.entityFullName;
		const entityDisplayName = entityData.displayName;
		var $div = $(`<div style="    padding: 10px;">
		<ul class="nav nav-tabs nav-line-tabs nav-line-tabs-2x mb-5 fs-6">
		    <li class="nav-item">
			   <a class="nav-link active" data-bs-toggle="tab" href="#create_${profileId}">ایجاد</a>
		    </li>
		    <li class="nav-item">
			   <a class="nav-link" data-bs-toggle="tab" href="#edit_${profileId}">ویرایش</a>
		    </li>
		    <li class="nav-item">
			   <a class="nav-link" data-bs-toggle="tab" href="#delete_${profileId}">حذف</a>
		    </li>
		</ul>
		<div class="tab-content" id="myTabContent">
		    <div class="tab-pane fade show active" id="create_${profileId}" role="tabpanel">
		   <div class="row">
		    <div class='col-md-1 align-content-center' style="padding-top: 20px;">
					<div class="form-check">
							<input data-bind='createCheckBox' id="createCheckBox" class="form-check-input" type="checkbox"   />
							<label class="form-check-label" asp-for='createCheckBox'>
							    فعال
							</label>
						</div>
				</div>
		   <div class="form-group col-md-8">
				<label class="form-label">عنوان اعلان ایجاد</label>
				<input placeholder="اضافه شدن ${entityDisplayName}" class='form-control' data-bind='createTitle'  />
			</div>
				
		   </div>
			    <div data-queryBuilder=''>
			    </div>
		    </div>
		    <div class="tab-pane fade" id="edit_${profileId}" role="tabpanel">
		     <div class="row">
			 <div class='col-md-1 align-content-center' style="padding-top: 20px;">
					<div class="form-check">
							<input data-bind='editCheckBox' id="editCheckBox" class="form-check-input" type="checkbox"   />
							<label class="form-check-label" asp-for='editCheckBox'>
							    فعال
							</label>
						</div>
				</div>
			<div class="form-group col-md-8">
				<label class="form-label"> عنوان اعلان ویرایش</label>
				<input placeholder="ویرایش شدن ${entityDisplayName}" class='form-control' data-bind='editTitle'  />
			</div>
			
			</div>
			  <div data-queryBuilder=''>
			    </div>
		    </div>
		    <div class="tab-pane fade" id="delete_${profileId}" role="tabpanel">
		    <div class="row">
		     <div class='col-md-1 align-content-center' style="padding-top: 20px;">
					<div class="form-check">
							<input   data-bind='deleteCheckBox' id="deleteCheckBox" class="form-check-input" type="checkbox"  />
							<label class="form-check-label" asp-for='deleteCheckBox'>
							    فعال
							</label>
					</div>
				</div>
			    <div class="form-group col-md-8">
					<label class="form-label">عنوان اعلان حذف</label>
					<input placeholder="حذف شدن ${entityDisplayName}" class='form-control' data-bind='deleteTitle'  />
				</div>
				
		    </div>
			     <div data-queryBuilder=''>
			    </div>
		    </div>
		</div>
		<label class="badge badge-light-danger fs-6 d-none" id="errorsection"></label>

		</div>`);
 
		 
		fields = entityData.properties.map((c) => {
			return { name: c.name, type: c.dataType, label: c.displayName, fullName: c.relatedEntityTypeFullName, options : c.options }
		})

		const createquertBuilder =	$div.find(`#create_${profileId} [data-queryBuilder]`).queryBuilder({
			fields: fields,
			entityApiUrl: '/NotifictionBuilder/EntityInfoBy/' 
		});

		const editquertBuilder =	$div.find(`#edit_${profileId} [data-queryBuilder]`).queryBuilder({
			fields: fields,
			entityApiUrl: '/NotifictionBuilder/EntityInfoBy/' 
		});

		const deletequertBuilder =	$div.find(`#delete_${profileId} [data-queryBuilder]`).queryBuilder({
			fields: fields,
			entityApiUrl: '/NotifictionBuilder/EntityInfoBy/' 
		});
		const deleteCheckBoxEl = $div.find("[data-bind =deleteCheckBox]")
		const editCheckBoxEl = $div.find("[data-bind =editCheckBox]")
		const createCheckBoxEl = $div.find("[data-bind =createCheckBox]");

		const errorsectionEl = $div.find("#errorsection");

		const createTitleEl = $div.find("[data-bind =createTitle]");
		const editTitleEl = $div.find("[data-bind =editTitle]");
		const deleteTitleEl = $div.find("[data-bind =deleteTitle]");

		if (existRules.any(c => c.systemOpertion == 0)) //Create
		{  
			let createRole = existRules.firstOrDefault(c => c.systemOpertion == 0);
			createquertBuilder.data("querybuilder").fromJSON(createRole.conditionsJson);
			createCheckBoxEl[0].checked = createRole.isActive
			createTitleEl.val(createRole.messageTitle);

		}

		if (existRules.any(c => c.systemOpertion == 1)) //edit
		{
			let editRole = existRules.firstOrDefault(c => c.systemOpertion == 1);
			editquertBuilder.data("querybuilder").fromJSON(editRole.conditionsJson);
			editCheckBoxEl[0].checked = editRole.isActive
			editTitleEl.val(editRole.messageTitle);
		}

		if (existRules.any(c => c.systemOpertion == 2)) //delete
		{
			let deleteRole = existRules.firstOrDefault(c => c.systemOpertion == 2);
			deletequertBuilder.data("querybuilder").fromJSON(deleteRole.conditionsJson);
			deleteCheckBoxEl[0].checked = deleteRole.isActive
			deleteTitleEl.val(deleteRole.messageTitle);

		}

	

	 let $confirm =	$.confirm({
			title: 'ایجاد اعلان هوشمند',
			content: $div,
			columnClass: "col-md-9",
			buttons: {
				save: {
					text: 'ذخیره',
					btnClass: 'btn btn-success fs-8',
					action: function () {
						var createquertBuilderObj = createquertBuilder.data('querybuilder') ;
						var editquertBuilderObj = editquertBuilder.data('querybuilder') ;
						var deletequertBuilderObj = deletequertBuilder.data('querybuilder') ;

						var createEnable = createCheckBoxEl[0].checked;
						var eidtEnable = editCheckBoxEl[0].checked;
						var deleteEnable = deleteCheckBoxEl[0].checked;

						var createquertBuilderRule = createquertBuilderObj.getRules();
						var editquertBuilderRule = editquertBuilderObj.getRules();
						var deletequertBuilderRule = deletequertBuilderObj .getRules();

						if (createEnable) {
							if (!createquertBuilderObj.validate().valid) {
								errorsectionEl.text("خطا: شرط های ایجاد شده برای حالت 'ایجاد' معتبر نمیباشد.")
									.removeClass("d-none");
							 
								return false;
							}

							if (createquertBuilderRule.criteria.length == 0) {
								errorsectionEl.text("خطا: هیچ شرطی برای حالت 'ایجاد' انتخاب نشده است.")
									.removeClass("d-none");
								 
								return false;
							}
						}
						if (eidtEnable) {

							if (!editquertBuilderObj.validate().valid) {
								errorsectionEl.text("خطا: شرط های ایجاد شده برای حالت 'ویرایش' معتبر نمیباشد.")
									.removeClass("d-none");

								return false;
							}

							if (editquertBuilderRule.criteria.length == 0) {
								errorsectionEl.text("خطا: هیچ شرطی برای حالت 'ویرایش' انتخاب نشده است.")
									.removeClass("d-none");

								return false;
							}

						}
						if (deleteEnable) {

							if (!deletequertBuilderObj.validate().valid) {
								errorsectionEl.text("خطا: شرط های ایجاد شده برای حالت 'حذف' معتبر نمیباشد.")
									.removeClass("d-none");

								return false;
							}

							if (deletequertBuilderRule.criteria.length == 0) {
								errorsectionEl.text("خطا: هیچ شرطی برای حالت 'حذف' انتخاب نشده است.")
									.removeClass("d-none");

								return false;
							}
						}

						var createTitle = createTitleEl.val().length === 0 ? createTitleEl.attr("placeholder") : createTitleEl.val();
						var editTitle = editTitleEl.val().length === 0 ? editTitleEl.attr("placeholder") : editTitleEl.val();
						var deleteTitle = deleteTitleEl.val().length === 0 ? deleteTitleEl.attr("placeholder") : deleteTitleEl.val();

						var model = {
							entityFullName,
							create: { enable: createEnable, roles: createquertBuilderRule, title: createTitle },
							edit: { enable: eidtEnable, roles: editquertBuilderRule, title: editTitle },
							delete: { enable: deleteEnable, roles: deletequertBuilderRule, title: deleteTitle }
						}
						let $btn = this.$$save.block()
						 
						post("/NotifictionBuilder/Save", model, function (r) {
							 $btn.block(false)
							if (!r.isSuccess) {
								errorsectionEl.text(`خطا زمان ذخیره سازی : ${r.message}`)
									.removeClass("d-none");
								return false;
							}
							toastr.success("تنظیمات مورد نظر ذخیره شد");
							$confirm.close();
						})
						return false;
					}
				},
				cancel: {
					text: 'بستن',
					btnClass: 'btn btn-danger fs-8',
  
					action: function () {
					 
					}
				}
			}
		})
	})
}

 

function upload(entryObject) {
	const input = document.createElement('input');
	input.type = 'file';
	input.onchange = (_) => {
		const files = input.files[0];
		const formData = new FormData();
		formData.append('file', files);

		if (entryObject.path) {
			formData.append("path", entryObject.path)
		}


		if (entryObject.allowedTypes && entryObject.allowedTypes.length > 0) {
			if (!isFileTypeAllowed(files.name, entryObject.allowedTypes)) {
				alert('پسوند فایل غیر مجاز میباشد');
				return;
			}
		}

		if (entryObject.size) {
			if (files.size > entryObject.size) {
				alert('حجم فایل بیشتر از حد مجاز میباشد');
				return;
			}
		}

		var percent = 0;
		$.ajax({
			url: '/File/uploadFile',
			type: 'POST',
			data: formData,
			processData: false,
			contentType: false,

			xhr: function () {
				var xhr = new window.XMLHttpRequest();
				xhr.upload.addEventListener('progress', function (e) {
					if (e.lengthComputable) {
						percent = Math.round((e.loaded / e.total) * 100);
						if (entryObject.onProsess)
							entryObject.onProsess(percent)

					}
				});
				return xhr;
			},

			success: function (r) {
				if (entryObject.onComplete)
					entryObject.onComplete(r);
			},
			error: function (r) {
				if (entryObject.onError)
					entryObject.onError(r);
			}
		});


	};
	input.click();
}

function isFileTypeAllowed(fileName, allowedTypes) {
	// Split the allowed types string into an array of extensions
	const allowedExtensions = allowedTypes.split(',').map(type => type.trim().toLowerCase().replace('*.', ''));

	// Extract the file extension from the file name
	const fileExtension = fileName.split('.').pop().toLowerCase();

	// Check if the file extension is in the list of allowed extensions
	return allowedExtensions.includes(fileExtension);
}



post = function (url, data, callback) {

	$.ajax({
		url: url,
		type: "POST",
		data: JSON.stringify(data),
		contentType: 'application/json',
		crossDomain: true,
		xhrFields: {
			withCredentials: true
		},
		success: function (response) {
			if (response.isSuccess === undefined)
				response.isSuccess = true;

			callback(response)
		},
		error: function (xhr) {
			if (xhr.responseJSON)
				return callback(xhr.responseJSON)
			let errorModel = {
				message: `خطا با کد ${xhr.status} ${xhr?.responseText}`,
				isSuccess: false
			}

			callback(errorModel)
		}
	});

}

get = function (url, callback) {
	$.ajax({
		url: url,
		type: "GET",
		contentType: 'application/json',
		crossDomain: true,
		xhrFields: {
			withCredentials: true
		},
		success: function (response) {
			callback(response)
		},
		error: function (xhr) {
			callback(xhr.responseJSON)
		}
	});
}


var MJComponents = function () {
	// Public methods
	return {
		init: function () {
			MJPasswordMeter.init();
			MJUtil.init();
		}
	}
}();

if (document.readyState === "loading") {
	document.addEventListener("DOMContentLoaded", function () {
		MJComponents.init();
	});
} else {
	MJComponents.init();
}


window.MJUtilElementDataStore = {};
window.MJUtilElementDataStoreID = 0;
window.MJUtilDelegatedEventHandlers = {};
var MJUtil = function () {
	var resizeHandlers = [];


	var _windowResizeHandler = function () {
		var _runResizeHandlers = function () {
			// reinitialize other subscribed elements
			for (var i = 0; i < resizeHandlers.length; i++) {
				var each = resizeHandlers[i];
				each.call();
			}
		};

		var timer;

		window.addEventListener('resize', function () {
			MJUtil.throttle(timer, function () {
				_runResizeHandlers();
			}, 200);
		});
	};
	return {
		init: function (settings) {
			_windowResizeHandler();
		},
		addResizeHandler: function (callback) {
			resizeHandlers.push(callback);
		},
		hextorgb(hex_code) {
			var red = parseInt(hex_code[1] + hex_code[2], 16);
			var green = parseInt(hex_code[3] + hex_code[4], 16);
			var blue = parseInt(hex_code[5] + hex_code[6], 16);
			return { red, green, blue }
		},
		removeResizeHandler: function (callback) {
			for (var i = 0; i < resizeHandlers.length; i++) {
				if (callback === resizeHandlers[i]) {
					delete resizeHandlers[i];
				}
			}
		},
		runResizeHandlers: function () {
			_runResizeHandlers();
		},
		getURLParam: function (paramName) {
			var searchString = window.location.search.substring(1),
				i, val, params = searchString.split("&");

			for (i = 0; i < params.length; i++) {
				val = params[i].split("=");
				if (val[0] == paramName) {
					return unescape(val[1]);
				}
			}

			return null;
		},
		isMobileDevice: function () {
			var test = (this.getViewPort().width < this.getBreakpoint('lg') ? true : false);

			if (test === false) {
				// For use within normal web clients
				test = navigator.userAgent.match(/iPad/i) != null;
			}

			return test;
		},


		isDesktopDevice: function () {
			return KTUtil.isMobileDevice() ? false : true;
		},

		getUniqueId: function (prefix) {
			return prefix + Math.floor(Math.random() * (new Date()).getTime());
		},
		getRandomInt: function (min, max) {
			return Math.floor(Math.random() * (max - min + 1)) + min;
		},
		// Throttle function: Input as function which needs to be throttled and delay is the time interval in milliseconds
		throttle: function (timer, func, delay) {
			// If setTimeout is already scheduled, no need to do anything
			if (timer) {
				return;
			}

			// Schedule a setTimeout after delay seconds
			timer = setTimeout(function () {
				func();

				// Once setTimeout function execution is finished, timerId = undefined so that in <br>
				// the next scroll event function execution can be scheduled by the setTimeout
				timer = undefined;
			}, delay);
		},
		data: function (el) {
			return {
				set: function (name, data) {
					if (!el) {
						return;
					}

					if (el.customDataTag === undefined) {
						window.MJUtilElementDataStoreID++;
						el.customDataTag = window.MJUtilElementDataStoreID;
					}

					if (window.MJUtilElementDataStore[el.customDataTag] === undefined) {
						window.MJUtilElementDataStore[el.customDataTag] = {};
					}

					window.MJUtilElementDataStore[el.customDataTag][name] = data;
				},

				get: function (name) {
					if (!el) {
						return;
					}

					if (el.customDataTag === undefined) {
						return null;
					}

					return this.has(name) ? window.MJUtilElementDataStore[el.customDataTag][name] : null;
				},

				has: function (name) {
					if (!el) {
						return false;
					}

					if (el.customDataTag === undefined) {
						return false;
					}

					return (window.MJUtilElementDataStore[el.customDataTag] && window.MJUtilElementDataStore[el.customDataTag][name]) ? true : false;
				},

				remove: function (name) {
					if (el && this.has(name)) {
						delete window.MJUtilElementDataStore[el.customDataTag][name];
					}
				}
			};
		},

		deepExtend: function (out) {
			out = out || {};

			for (var i = 1; i < arguments.length; i++) {
				var obj = arguments[i];
				if (!obj) continue;

				for (var key in obj) {
					if (!obj.hasOwnProperty(key)) {
						continue;
					}


					if (Object.prototype.toString.call(obj[key]) === '[object Object]') {
						out[key] = MJUtil.deepExtend(out[key], obj[key]);
						continue;
					}

					out[key] = obj[key];
				}
			}

			return out;
		},
		downloadFile: function (data, contentType, fileName) {
			const b64Data = "BASE_64_DOC";
			const blob = b64toBlob(data, contentType);
			const blobUrl = URL.createObjectURL(blob);
			const a = document.createElement("a");
			a.href = blobUrl;
			a.download = fileName;
			a.click();
		},
		downloadFileByPath: function (path, filename) {
			const anchor = document.createElement('a');
			anchor.href = path;
			anchor.download = filename;
			document.body.appendChild(anchor);
			anchor.click();
			document.body.removeChild(anchor);
		},


		toEnglishNumbers: function (input) {
			if (!input || input.length == 0) {
				return null;
			}
			var persianToEnglishMap = {
				'۰': '0',
				'۱': '1',
				'۲': '2',
				'۳': '3',
				'۴': '4',
				'۵': '5',
				'۶': '6',
				'۷': '7',
				'۸': '8',
				'۹': '9'
			};

			var regex = new RegExp(Object.keys(persianToEnglishMap).join('|'), 'g');

			return input.replace(regex, function (match) {
				return persianToEnglishMap[match];
			});
		},
		shamsiDate: {
			g_days_in_month: [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31],
			j_days_in_month: [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29]
		},
		shamsiToMiladi: function (persianDate) {
			if (!persianDate || persianDate.length < 10)
				return null;
			var dateParts = persianDate.split('/');
			var timeSplit = persianDate.split(" ");




			var jY = parseInt(MJUtil.toEnglishNumbers(dateParts[0]));
			var jM = parseInt(MJUtil.toEnglishNumbers(dateParts[1]));
			var jD = parseInt(MJUtil.toEnglishNumbers(dateParts[2]));


			var jy = jY - 979;
			var jm = jM - 1;
			var jd = jD - 1;

			var jDayNo = 365 * jy + parseInt(jy / 33) * 8 + parseInt((jy % 33 + 3) / 4);
			for (var i = 0; i < jm; ++i) jDayNo += this.shamsiDate.j_days_in_month[i];

			jDayNo += jd;

			var gDayNo = jDayNo + 79;

			var gy = 1600 + 400 * parseInt(gDayNo / 146097); /* 146097 = 365*400 + 400/4 - 400/100 + 400/400 */
			gDayNo = gDayNo % 146097;

			var leap = true;
			if (gDayNo >= 36525) /* 36525 = 365*100 + 100/4 */ {
				gDayNo--;
				gy += 100 * parseInt(gDayNo / 36524); /* 36524 = 365*100 + 100/4 - 100/100 */
				gDayNo = gDayNo % 36524;

				if (gDayNo >= 365) gDayNo++;
				else leap = false;
			}

			gy += 4 * parseInt(gDayNo / 1461); /* 1461 = 365*4 + 4/4 */
			gDayNo %= 1461;

			if (gDayNo >= 366) {
				leap = false;

				gDayNo--;
				gy += parseInt(gDayNo / 365);
				gDayNo = gDayNo % 365;
			}

			for (var i = 0; gDayNo >= this.shamsiDate.g_days_in_month[i] + (i == 1 && leap); i++)
				gDayNo -= this.shamsiDate.g_days_in_month[i] + (i == 1 && leap);
			var gm = i + 1;
			var gd = gDayNo + 1;

			gm = gm < 10 ? "0" + gm : gm;
			gd = gd < 10 ? "0" + gd : gd;

			if (timeSplit.length > 1) {
				return `${gy}/${gm}/${gd} ${MJUtil.toEnglishNumbers(timeSplit[1])}`;
			}

			return `${gy}/${gm}/${gd}`;
		},
		miladiToShamsi: function (miladiDate) {

			return new Date(miladiDate).toLocaleString('fa-IR').replace(",", "");
		},

	}
}()

const b64toBlob = (b64Data, contentType = "", sliceSize = 512) => {

	const byteCharacters = atob(b64Data);
	const byteArrays = [];

	for (
		let offset = 0;
		offset < byteCharacters.length;
		offset += sliceSize
	) {
		const slice = byteCharacters.slice(offset, offset + sliceSize);
		const byteNumbers = new Array(slice.length);

		for (let i = 0; i < slice.length; i++) {
			byteNumbers[i] = slice.charCodeAt(i);
		}

		const byteArray = new Uint8Array(byteNumbers);
		byteArrays.push(byteArray);
	}

	const blob = new Blob(byteArrays, { type: contentType });

	return blob;
};



if (typeof module !== 'undefined' && typeof module.exports !== 'undefined') {
	module.exports = KTUtil;
}
var MJPasswordMeter = function (element, options) {

	var the = this;

	if (!element) {
		return;
	}

	// Default Options
	var defaultOptions = {
		minLength: 8,
		checkUppercase: true,
		checkLowercase: true,
		checkDigit: true,
		checkChar: true,
		scoreHighlightClass: 'active'
	};


	var _construct = function () {
		if (MJUtil.data(element).has('password-meter') === true) {
			the = MJUtil.data(element).get('password-meter');
		} else {
			_init();
		}
	}


	var _init = function () {

		the.options = MJUtil.deepExtend({}, defaultOptions, options);
		the.score = 0;
		the.checkSteps = 5;


		the.element = element;
		the.inputElement = the.element.querySelector('input[type]');
		the.visibilityElement = the.element.querySelector('[data-mj-password-meter-control="visibility"]');
		the.highlightElement = the.element.querySelector('[data-mj-password-meter-control="highlight"]');


		the.element.setAttribute('data-mj-password-meter', 'true');


		_handlers();


		MJUtil.data(the.element).set('password-meter', the);
	}


	var _handlers = function () {
		if (the.highlightElement) {
			the.inputElement.addEventListener('input', function () {
				_check();
			});
		}

		if (the.visibilityElement) {
			the.visibilityElement.addEventListener('click', function () {
				_visibility();
			});
		}
	}


	var _check = function () {
		var score = 0;
		var checkScore = _getCheckScore();

		if (_checkLength() === true) {
			score = score + checkScore;
		}

		if (the.options.checkUppercase === true && _checkLowercase() === true) {
			score = score + checkScore;
		}

		if (the.options.checkLowercase === true && _checkUppercase() === true) {
			score = score + checkScore;
		}

		if (the.options.checkDigit === true && _checkDigit() === true) {
			score = score + checkScore;
		}

		if (the.options.checkChar === true && _checkChar() === true) {
			score = score + checkScore;
		}

		the.score = score;

		_highlight();
	}

	var _checkLength = function () {
		return the.inputElement.value.length >= the.options.minLength;  // 20 score
	}

	var _checkLowercase = function () {
		return /[a-z]/.test(the.inputElement.value);  // 20 score
	}

	var _checkUppercase = function () {
		return /[A-Z]/.test(the.inputElement.value);  // 20 score
	}

	var _checkDigit = function () {
		return /[0-9]/.test(the.inputElement.value);  // 20 score
	}

	var _checkChar = function () {
		return /[~`!#@$%\^&*+=\-\[\]\\';,/{}|\\":<>\?]/g.test(the.inputElement.value);  // 20 score
	}

	var _getCheckScore = function () {
		var count = 1;

		if (the.options.checkUppercase === true) {
			count++;
		}

		if (the.options.checkLowercase === true) {
			count++;
		}

		if (the.options.checkDigit === true) {
			count++;
		}

		if (the.options.checkChar === true) {
			count++;
		}

		the.checkSteps = count;

		return 100 / the.checkSteps;
	}

	var _highlight = function () {
		var items = [].slice.call(the.highlightElement.querySelectorAll('div'));
		var total = items.length;
		var index = 0;
		var checkScore = _getCheckScore();
		var score = _getScore();

		items.map(function (item) {
			index++;

			if ((checkScore * index * (the.checkSteps / total)) <= score) {
				item.classList.add('active');
			} else {
				item.classList.remove('active');
			}
		});
	}

	var _visibility = function () {
		var visibleIcon = the.visibilityElement.querySelector(':scope > i:not(.d-none)');
		var hiddenIcon = the.visibilityElement.querySelector(':scope > i.d-none');

		if (the.inputElement.getAttribute('type').toLowerCase() === 'password') {
			the.inputElement.setAttribute('type', 'text');
		} else {
			the.inputElement.setAttribute('type', 'password');
		}

		visibleIcon.classList.add('d-none');
		hiddenIcon.classList.remove('d-none');

		the.inputElement.focus();
	}

	var _reset = function () {
		the.score = 0;

		_highlight();
	}


	var _getScore = function () {
		return the.score;
	}

	var _destroy = function () {
		MJUtil.data(the.element).remove('password-meter');
	}


	_construct();


	the.check = function () {
		return _check();
	}

	the.getScore = function () {
		return _getScore();
	}

	the.reset = function () {
		return _reset();
	}

	the.destroy = function () {
		return _destroy();
	}
};

// Static methods
MJPasswordMeter.getInstance = function (element) {
	if (element !== null && MJUtil.data(element).has('password-meter')) {
		return MJUtil.data(element).get('password-meter');
	} else {
		return null;
	}
}

// Create instances
MJPasswordMeter.createInstances = function (selector = '[data-mj-password-meter]') {
	// Get instances
	var elements = document.body.querySelectorAll(selector);

	if (elements && elements.length > 0) {
		for (var i = 0, len = elements.length; i < len; i++) {
			// Initialize instances
			new MJPasswordMeter(elements[i]);
		}
	}
}

// Global initialization
MJPasswordMeter.init = function () {
	MJPasswordMeter.createInstances();
};


var MJCookie = function () {
	return {
		// returns the cookie with the given name,
		// or undefined if not found
		get: function (name) {
			var matches = document.cookie.match(new RegExp(
				"(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
			));

			return matches ? decodeURIComponent(matches[1]) : null;
		},

		// Please note that a cookie value is encoded,
		// so getCookie uses a built-in decodeURIComponent function to decode it.
		set: function (name, value, options) {
			if (typeof options === "undefined" || options === null) {
				options = {};
			}

			options = Object.assign({}, {
				path: '/'
			}, options);

			if (options.expires instanceof Date) {
				options.expires = options.expires.toUTCString();
			}

			var updatedCookie = encodeURIComponent(name) + "=" + encodeURIComponent(value);

			for (var optionKey in options) {
				if (options.hasOwnProperty(optionKey) === false) {
					continue;
				}

				updatedCookie += "; " + optionKey;
				var optionValue = options[optionKey];

				if (optionValue !== true) {
					updatedCookie += "=" + optionValue;
				}
			}

			document.cookie = updatedCookie;
		},

		// To remove a cookie, we can call it with a negative expiration date:
		remove: function (name) {
			this.set(name, "", {
				'max-age': -1
			});
		}
	}
}();

// Webpack support
if (typeof module !== 'undefined' && typeof module.exports !== 'undefined') {
	module.exports = MJCookie;
}
 

Object.defineProperties(Array.prototype, {
	where: {
		value: function (callback, newArray) {
			var filteredArray = [];

			for (var i = 0; i < this.length; i++) {
				if (callback(this[i], i, this)) {
					filteredArray.push(this[i]);
				}
			}
			if (newArray)
				return JSON.parse(JSON.stringify(filteredArray));

			return filteredArray;
		},
		writable: false
	},
	firstOrDefault: {
		value: function (callback, clone = false) {
			var fdata = null;

			let i = 0;
			while (i < this.length) {
				if (callback(this[i], i, this)) {
					fdata = this[i];
					break;
				}
				i++;
			}

			if (fdata) {
				if (clone === true)
					return JSON.parse(JSON.stringify(fdata));

				return fdata;
			}

			return null;

		},
		writable: false
	},

	last: {
		value: function (clone = false) {

			if (this.length === 0)
				return null

			if (clone === true)
				return JSON.parse(JSON.stringify(this[this.length - 1]));
			return this[this.length - 1];

		},
		writable: false
	},
	any: {
		value: function (callback) {
			for (var i = 0; i < this.length; i++) {
				if (callback(this[i], i, this)) {
					return true;
				}
			}

			return false;
		},
		writable: false
	},
	remove: {
		value: function (callback) {
			for (var i = 0; i < this.length; i++) {
				if (callback(this[i], i, this)) {
					this.splice(i, 1);
				}
			}
		},
		writable: false
	},
	groupBy: {
		value: function (n) {
			return this.reduce(function (t, i) {
				return (t[i[n]] = t[i[n]] || []).push(i),
					t
			}, {})
		},
		writable: false
	}
})



!function (e) { e(["jquery"], function (e) { return function () { function t(e, t, n) { return g({ type: O.error, iconClass: m().iconClasses.error, message: e, optionsOverride: n, title: t }) } function n(t, n) { return t || (t = m()), v = e("#" + t.containerId), v.length ? v : (n && (v = d(t)), v) } function o(e, t, n) { return g({ type: O.info, iconClass: m().iconClasses.info, message: e, optionsOverride: n, title: t }) } function s(e) { C = e } function i(e, t, n) { return g({ type: O.success, iconClass: m().iconClasses.success, message: e, optionsOverride: n, title: t }) } function a(e, t, n) { return g({ type: O.warning, iconClass: m().iconClasses.warning, message: e, optionsOverride: n, title: t }) } function r(e, t) { var o = m(); v || n(o), u(e, o, t) || l(o) } function c(t) { var o = m(); return v || n(o), t && 0 === e(":focus", t).length ? void h(t) : void (v.children().length && v.remove()) } function l(t) { for (var n = v.children(), o = n.length - 1; o >= 0; o--)u(e(n[o]), t) } function u(t, n, o) { var s = !(!o || !o.force) && o.force; return !(!t || !s && 0 !== e(":focus", t).length) && (t[n.hideMethod]({ duration: n.hideDuration, easing: n.hideEasing, complete: function () { h(t) } }), !0) } function d(t) { return v = e("<div/>").attr("id", t.containerId).addClass(t.positionClass), v.appendTo(e(t.target)), v } function p() { return { tapToDismiss: !0, toastClass: "toast", containerId: "toast-container", debug: !1, showMethod: "fadeIn", showDuration: 300, showEasing: "swing", onShown: void 0, hideMethod: "fadeOut", hideDuration: 1e3, hideEasing: "swing", onHidden: void 0, closeMethod: !1, closeDuration: !1, closeEasing: !1, closeOnHover: !0, extendedTimeOut: 1e3, iconClasses: { error: "toast-error", info: "toast-info", success: "toast-success", warning: "toast-warning" }, iconClass: "toast-info", positionClass: "toast-top-right", timeOut: 5e3, titleClass: "toast-title", messageClass: "toast-message", escapeHtml: !1, target: "body", closeHtml: '<button type="button">&times;</button>', closeClass: "toast-close-button", newestOnTop: !0, preventDuplicates: !1, progressBar: !1, progressClass: "toast-progress", rtl: !1 } } function f(e) { C && C(e) } function g(t) { function o(e) { return null == e && (e = ""), e.replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/'/g, "&#39;").replace(/</g, "&lt;").replace(/>/g, "&gt;") } function s() { c(), u(), d(), p(), g(), C(), l(), i() } function i() { var e = ""; switch (t.iconClass) { case "toast-success": case "toast-info": e = "polite"; break; default: e = "assertive" }I.attr("aria-live", e) } function a() { E.closeOnHover && I.hover(H, D), !E.onclick && E.tapToDismiss && I.click(b), E.closeButton && j && j.click(function (e) { e.stopPropagation ? e.stopPropagation() : void 0 !== e.cancelBubble && e.cancelBubble !== !0 && (e.cancelBubble = !0), E.onCloseClick && E.onCloseClick(e), b(!0) }), E.onclick && I.click(function (e) { E.onclick(e), b() }) } function r() { I.hide(), I[E.showMethod]({ duration: E.showDuration, easing: E.showEasing, complete: E.onShown }), E.timeOut > 0 && (k = setTimeout(b, E.timeOut), F.maxHideTime = parseFloat(E.timeOut), F.hideEta = (new Date).getTime() + F.maxHideTime, E.progressBar && (F.intervalId = setInterval(x, 10))) } function c() { t.iconClass && I.addClass(E.toastClass).addClass(y) } function l() { E.newestOnTop ? v.prepend(I) : v.append(I) } function u() { if (t.title) { var e = t.title; E.escapeHtml && (e = o(t.title)), M.append(e).addClass(E.titleClass), I.append(M) } } function d() { if (t.message) { var e = t.message; E.escapeHtml && (e = o(t.message)), B.append(e).addClass(E.messageClass), I.append(B) } } function p() { E.closeButton && (j.addClass(E.closeClass).attr("role", "button"), I.prepend(j)) } function g() { E.progressBar && (q.addClass(E.progressClass), I.prepend(q)) } function C() { E.rtl && I.addClass("rtl") } function O(e, t) { if (e.preventDuplicates) { if (t.message === w) return !0; w = t.message } return !1 } function b(t) { var n = t && E.closeMethod !== !1 ? E.closeMethod : E.hideMethod, o = t && E.closeDuration !== !1 ? E.closeDuration : E.hideDuration, s = t && E.closeEasing !== !1 ? E.closeEasing : E.hideEasing; if (!e(":focus", I).length || t) return clearTimeout(F.intervalId), I[n]({ duration: o, easing: s, complete: function () { h(I), clearTimeout(k), E.onHidden && "hidden" !== P.state && E.onHidden(), P.state = "hidden", P.endTime = new Date, f(P) } }) } function D() { (E.timeOut > 0 || E.extendedTimeOut > 0) && (k = setTimeout(b, E.extendedTimeOut), F.maxHideTime = parseFloat(E.extendedTimeOut), F.hideEta = (new Date).getTime() + F.maxHideTime) } function H() { clearTimeout(k), F.hideEta = 0, I.stop(!0, !0)[E.showMethod]({ duration: E.showDuration, easing: E.showEasing }) } function x() { var e = (F.hideEta - (new Date).getTime()) / F.maxHideTime * 100; q.width(e + "%") } var E = m(), y = t.iconClass || E.iconClass; if ("undefined" != typeof t.optionsOverride && (E = e.extend(E, t.optionsOverride), y = t.optionsOverride.iconClass || y), !O(E, t)) { T++, v = n(E, !0); var k = null, I = e("<div/>"), M = e("<div/>"), B = e("<div/>"), q = e("<div/>"), j = e(E.closeHtml), F = { intervalId: null, hideEta: null, maxHideTime: null }, P = { toastId: T, state: "visible", startTime: new Date, options: E, map: t }; return s(), r(), a(), f(P), E.debug && console && console.log(P), I } } function m() { return e.extend({}, p(), b.options) } function h(e) { v || (v = n()), e.is(":visible") || (e.remove(), e = null, 0 === v.children().length && (v.remove(), w = void 0)) } var v, C, w, T = 0, O = { error: "error", info: "info", success: "success", warning: "warning" }, b = { clear: r, remove: c, error: t, getContainer: n, info: o, options: {}, subscribe: s, success: i, version: "2.1.3", warning: a }; return b }() }) }("function" == typeof define && define.amd ? define : function (e, t) { "undefined" != typeof module && module.exports ? module.exports = t(require("jquery")) : window.toastr = t(window.jQuery) });


toastr.options = {

	"positionClass": "toast-top-center",
}


databind = function () {
	var values = {};

	$('[data-bind]').each(function () {
		var propertyName = $(this).data('bind');
		var value = $(this).val();

		values[propertyName] = value;
	});

	return values;
}
$.fn.dataBind = function (model) {

    let context = this.$el ? this.$el : this;

 
    if (model) {
        for (let key in model) {

            let value = model[key];
            if (Array.isArray(value)) continue;

            let $el = context.find(`[data-bind="${key}"]`);
            if (!$el.length) continue;

            $el = $($el[0]);

            if ($el.is(':radio')) {
                if ($el.val() == value) $el.prop('checked', true).change();
            }
            else if ($el.is(':checkbox')) {
                $el.prop('checked', !!value).change();
            }
            else if ($el.is('span') || $el.is("label")) {
                $el.text(value);
            }
            else {
                $el.val(value).change();
            }
        }

        return;
    }

    // ------------------------------------------------------
    // READ MODE (UI → model)
    // ------------------------------------------------------
    let result = {};

    // Parse "items.id" → { arrayName: "items", propertyName: "id" }
    function parsePath(path) {
        let parts = path.split(".");
        if (parts.length !== 2) return null;
        return {
            arrayName: parts[0],
            propertyName: parts[1]
        };
    }

    // Extract correct value
    function extractValue($el) {
        if ($el.is(':radio'))
            return $el.prop('checked') ? $el.val() : null;

        if ($el.is(':checkbox'))
            return $el.prop('checked');

        if ($el.is('span') || $el.is('label'))
            return $el.text() || null;

        return $el.val() || null;
    }

    // ------------------------------------------------------
    // 1) عناصر را بر اساس arrayName و propertyName گروه‌بندی کنیم (COLUMN-WISE)
    // ------------------------------------------------------
    let columns = {};  // مثال: columns["items"] = { id: [el, el], title: [el, el] }

    context.find('[data-bind]').each(function () {

        let path = $(this).data('bind');
        let parsed = parsePath(path);

        // property ساده
        if (!parsed) {
            let v = extractValue($(this));
            result[path] = v;
			 
			if ($(this).is("input") && $(this).attr("data-persiondatepicker") != undefined) {
				result[path.toLowerCase().replace("shamsi", "miladi")] = MJUtil.shamsiToMiladi(v);
				result[path] = MJUtil.toEnglishNumbers(v)
			}
            return;
        }

        let a = parsed.arrayName;
        let p = parsed.propertyName;

        if (!columns[a]) columns[a] = {};
        if (!columns[a][p]) columns[a][p] = [];

        columns[a][p].push($(this));
    });

    // ------------------------------------------------------
    // 2) ساخت ردیف‌ها بر اساس index هر ستون
    // ------------------------------------------------------
    for (let arrayName in columns) {

        let props = columns[arrayName];
        let maxCount = 0;

        // بیشترین تعداد ردیف‌ها را پیدا کنیم
        for (let p in props)
            maxCount = Math.max(maxCount, props[p].length);

        let rows = [];

        // ایجاد رکوردها بر اساس index
        for (let i = 0; i < maxCount; i++) {
            let obj = {};

            for (let p in props) {
                let el = props[p][i];
                let val = el ? extractValue(el) : null;
 
                // Persian date support
                if (el && el.attr("data-persionDatePicker")) {
					 
                    obj[p] = val;
                    obj[p.replace("shamsi", "miladi")] = MJUtil.shamsiToMiladi(val);
                } else {
                    obj[p] = val;
                }
            }

            rows.push(obj);
        }

        result[arrayName] = rows;
    }

    return result;
};


// ==========================
// Script Loader
// ==========================
function JSLoader(jsList, callback) {
	if (!Array.isArray(jsList) || jsList.length === 0) {
		if (typeof callback === "function") callback();
		return;
	}
	const url = jsList.shift();
	const script = document.createElement("script");
	script.async = true;
	script.src = url;

	script.onload = () => JSLoader(jsList, callback);
	script.onerror = () => {
		console.error("Failed to load script:", url);
		JSLoader(jsList, callback);
	};

	document.body.appendChild(script);
}

// ==========================
// Page Class
// ==========================
class Page {
	constructor(address, $pageEl, $tabEl, pageInfo = {}) {
		this.address = address;
		this.$pageEl = $pageEl;
		this.$tabEl = $tabEl;

		this.id = pageInfo.id || null;
		this.title = pageInfo.title || "بدون عنوان";
		this.data = pageInfo.jsData ? JSON.parse(pageInfo.jsData) : {};
		this.script = pageInfo.script || "";
		this.listJsLink = pageInfo.listJsLink || [];
		this.css = pageInfo.css || "";
		this.listCssLink = pageInfo.listCssLink || [];

		// encapsulated jQuery helper
		const self = this;
		// Initialize self object for API access
		this.self = {
			FormActionButtons: null,
			table: null,
			TabItemManagers: null
		}
		this.$$ = function (selector) {
			return selector ? self.$pageEl.find(selector) : self.$pageEl;
		};
		this.$$.post = post;
		this.$$.get = get;
		this.$$.dataBind = this.$pageEl.dataBind;
	}

	activate() {
		this.$pageEl.addClass("show active");
		this.$tabEl.addClass("active_page_tab");
	}

	deactivate() {
		this.$pageEl.removeClass("show active");
		this.$tabEl.removeClass("active_page_tab");
	}

	destroy() {
		// Check if Bootstrap is available
		if (typeof bootstrap === 'undefined' || !bootstrap || !bootstrap.Tooltip) {
			// Bootstrap not available, just remove elements
			this.$pageEl.remove();
			this.$tabEl.remove();
			return;
		}

		// Destroy all Bootstrap tooltips within the page before removing
		if (this.$pageEl && this.$pageEl.length > 0) {
			// Find all elements with tooltips in the page
			const tooltipElements = this.$pageEl.find('[data-bs-toggle="tooltip"], [title]');
			tooltipElements.each(function() {
				const $el = $(this);
				// Check if element is still in DOM
				if (!document.body.contains(this)) {
					return;
				}
				try {
					// Get Bootstrap tooltip instance if exists
					const tooltipInstance = bootstrap.Tooltip.getInstance(this);
					if (tooltipInstance && typeof tooltipInstance.dispose === 'function') {
						// Hide tooltip if it's currently shown
						try {
							if (typeof tooltipInstance.hide === 'function') {
								tooltipInstance.hide();
							}
						} catch (e) {
							// Ignore errors if tooltip is already hidden
						}
						// Dispose the tooltip instance
						try {
							tooltipInstance.dispose();
						} catch (e) {
							// Ignore errors during disposal
							// This can happen if tooltip is already disposed or element is removed
						}
					}
					// Also try to remove tooltip data
					if ($el.data('bs.tooltip')) {
						$el.data('bs.tooltip', null);
					}
				} catch (e) {
					// Ignore errors for this element
					// Element might be already removed or tooltip might be in invalid state
				}
			});
		}
		
		// Destroy tooltips in tab element as well
		if (this.$tabEl && this.$tabEl.length > 0) {
			const tabTooltipElements = this.$tabEl.find('[data-bs-toggle="tooltip"], [title]');
			tabTooltipElements.each(function() {
				const $el = $(this);
				// Check if element is still in DOM
				if (!document.body.contains(this)) {
					return;
				}
				try {
					const tooltipInstance = bootstrap.Tooltip.getInstance(this);
					if (tooltipInstance && typeof tooltipInstance.dispose === 'function') {
						// Hide tooltip if it's currently shown
						try {
							if (typeof tooltipInstance.hide === 'function') {
								tooltipInstance.hide();
							}
						} catch (e) {
							// Ignore errors if tooltip is already hidden
						}
						// Dispose the tooltip instance
						try {
							tooltipInstance.dispose();
						} catch (e) {
							// Ignore errors during disposal
							// This can happen if tooltip is already disposed or element is removed
						}
					}
					if ($el.data('bs.tooltip')) {
						$el.data('bs.tooltip', null);
					}
				} catch (e) {
					// Ignore errors for this element
					// Element might be already removed or tooltip might be in invalid state
				}
			});
		}
		
		this.$pageEl.remove();
		this.$tabEl.remove();
	}

	execScripts() {
		let code = this.script;
		let f = new Function("page", "$$", "self", code);
 

		JSLoader([...this.listJsLink], () => {
			try {
				f(this, this.$$, this.self);
			} catch (err) {
			 
			}
		});
	}

	/**
	 * دریافت partial view از API
	 * @param {string} apiUrl - آدرس API partial view (مثال: '/Panel/Document/DocumentCommentPartial')
	 * @param {function} callback - تابع callback که HTML partial view به آن پاس داده می‌شود
	 * @param {object} data - داده‌های اختیاری برای ارسال به سرور (برای POST)
	 * @param {string} method - نوع درخواست: 'GET' یا 'POST' (پیش‌فرض: 'GET')
	 */
	getPartialView(apiUrl, callback, data = null, method = 'GET') {
		if (!apiUrl || typeof callback !== 'function') {
			console.error('getPartialView: apiUrl و callback الزامی هستند');
			return;
		}

		const self = this;
		$.ajax({
			url: apiUrl,
			type: method,
			data: data,
			contentType: method === 'POST' ? 'application/json' : undefined,
			crossDomain: true,
			xhrFields: {
				withCredentials: true
			},
			headers: { 
				"X-Requested-With": "XMLHttpRequest", 
				"X-Partial-Request": "true" 
			},
			success: function (html) {
				try {
					callback(html, self);
				} catch (err) {
					console.error('خطا در اجرای callback getPartialView:', err);
				}
			},
			error: function (xhr, status, error) {
				console.error(`خطا در دریافت partial view از ${apiUrl}:`, {
					status: xhr.status,
					statusText: xhr.statusText,
					error: error
				});
				// فراخوانی callback با null در صورت خطا
				try {
					callback(null, self, { error: true, status: xhr.status, message: error });
				} catch (err) {
					console.error('خطا در اجرای callback خطا:', err);
				}
			}
		});
	}
}

function isSameOrigin(url) {
	const loc = window.location;
	const a = document.createElement("a");
	a.href = url;
	return a.hostname === loc.hostname && a.protocol === loc.protocol;
}

// ==========================
// AppController
// ==========================
class AppController {
	constructor($pagesContainer, $tabsContainer, $loader, firstPageAddress) {
		this.pages = [];
		this.activePage = null;
		this.$pagesContainer = $pagesContainer;
		this.$tabsContainer = $tabsContainer;
		this.$loader = $loader;

		// In-app tab activation history management
		this.activationHistory = [];
		this.historyPointer = -1;

		this.initPopState();
		if (window.location.pathname !== '/')
		 this.addPage(firstPageAddress);
	}

	initPopState() {
		window.addEventListener("popstate", (e) => {
			  
			const state = e.state || {};
			if (state.address) {
				let targetIndex = -1;
				// Try immediate neighbors first to infer direction
				if (this.historyPointer > 0 && this.activationHistory[this.historyPointer - 1] === state.address) {
					targetIndex = this.historyPointer - 1;
				} else if (this.historyPointer >= 0 && this.historyPointer < this.activationHistory.length - 1 && this.activationHistory[this.historyPointer + 1] === state.address) {
					targetIndex = this.historyPointer + 1;
				} else {
					// Search backwards from current pointer
					for (let i = this.historyPointer - 1; i >= 0; i--) {
						if (this.activationHistory[i] === state.address) { targetIndex = i; break; }
					}
					// Then search forwards
					if (targetIndex === -1) {
						for (let i = this.historyPointer + 1; i < this.activationHistory.length; i++) {
							if (this.activationHistory[i] === state.address) { targetIndex = i; break; }
						}
					}
					// Fallback to last occurrence
					if (targetIndex === -1) {
						targetIndex = this.activationHistory.lastIndexOf(state.address);
					}
				}

				let page = this.pages.find(p => p.address === state.address);
				if (targetIndex !== -1) {
					this.historyPointer = targetIndex;
					if (page) this.setActivePage(page, false); else this.addPage(state.address, false);
				} else {
					// Address not in our activation history (e.g., first load or cleared)
					if (this.historyPointer < this.activationHistory.length - 1) {
						this.activationHistory = this.activationHistory.slice(0, this.historyPointer + 1);
					}
					this.activationHistory.push(state.address);
					this.historyPointer = this.activationHistory.length - 1;
					if (page) this.setActivePage(page, false); else this.addPage(state.address, false);
				}
				return;
			}

			// No address in state: try to navigate to previous in our history if possible
			if (this.historyPointer > 0) {
				const prevAddress = this.activationHistory[this.historyPointer - 1];
				let page = this.pages.find(p => p.address === prevAddress);
				this.historyPointer = this.historyPointer - 1;
				if (page) this.setActivePage(page, false); else this.addPage(prevAddress, false);
			} else if (this.activePage) {
				// Reaffirm current active tab without pushing new state
				this.setActivePage(this.activePage, false);
			}
		});
	}

	initHistorySentinel() {
		try { } catch (err) { }
	}

	addPage(address, pushState = true) {
		address = address.toLowerCase();
		let existing = this.pages.find(p => p.address === address);
		if (existing) { this.setActivePage(existing, pushState); return; }

		// Create Tab + Page DOM
		let $tab = $(`<li class="page_tab">
                        <span>در حال بارگذاری...</span>
					<div>
					   <i class="fa fa-times ms-2 close-btn"></i>
					</div>
                      </li>`);
		let $page = $(`<div class="tab-pane fade" data-sys="system-tab" id="${address}"></div>`);

		if (this.pages.length === 0) {
			this.$pagesContainer.children().addClass("fade")
		}

		this.$tabsContainer.append($tab);
		this.$pagesContainer.append($page);

	

		let page = new Page(address, $page, $tab);
		this.pages.push(page);
		this.setActivePage(page, pushState);

		// Handle close tab
		$tab.find(".close-btn").on("click", (e) => {
			 
			e.stopPropagation();
 
			this.closePage(address);
		});

		// Handle tab click
		$tab.on("click", () => this.setActivePage(page));
		 

		$.ajax({
			url: address,
			type: "GET",
			contentType: 'application/json',
			crossDomain: true,
			xhrFields: {
				withCredentials: true
			},
			headers: { "X-Requested-With": "XMLHttpRequest", "X-Partial-Request": "true" },
			success: function (r) {
 				function tryDecodeV1(resp) {
					if (!resp || resp.v !== 1 || !resp.p || resp.q === undefined) return null;
					try {
						const salt = [13, 71, 99, 201, 54, 11, 222, 39];
						const key = (resp.q ^ 0xA5) & 0xFF;
						const raw = window.atob(resp.p);
						const bytes = new Uint8Array(raw.length);
						for (let i = 0; i < raw.length; i++) {
							const ob = raw.charCodeAt(i);
							bytes[i] = (ob ^ key) ^ salt[i % salt.length];
						}
						// Use TextDecoder to avoid stack overflow on large payloads
						let utf8str;
						if (window.TextDecoder) {
							utf8str = new TextDecoder("utf-8").decode(bytes);
						} else {
							// Fallback, chunked to avoid call stack overflow
							let tmp = "";
							const chunkSize = 8192;
							for (let i = 0; i < bytes.length; i += chunkSize) {
								tmp += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
							}
							utf8str = decodeURIComponent(escape(tmp));
						}
						const payload = JSON.parse(utf8str);
						return {
							html: payload?.h || "",
							scripts: payload?.s || [],
							title: payload?.p?.t || "تب جدید"
						};
					} catch (err) {
						console.warn("Failed to decode obfuscated payload", err);
						return null;
					}
				}

			 
				function tryDecodeLegacy(resp) {
					const isBase64 = resp?.isBase64 === true;
					const decodePayload = function (val) {
						if (!isBase64 || typeof val !== "string") return val;
						try {
							return decodeURIComponent(escape(window.atob(val)));
						} catch (err) {
							try { return window.atob(val); } catch (e) { return val; }
						}
					};
					return {
						html: decodePayload(resp?.html) || resp?.html || "",
						scripts: Array.isArray(resp?.scripts) ? resp.scripts.map(decodePayload) : (resp?.scripts || []),
						title: resp?.pageInfo?.title || "تب جدید"
					};
				}

				const decoded = tryDecodeV1(r) || tryDecodeLegacy(r) || { html: "", scripts: [], title: "تب جدید" };

				$tab.find("span").text(decoded.title);
				$page.html(decoded.html);

				if (decoded.title === "-") {
					page.title = "تب جدید";
				} else {
					page.title = decoded.title;
				}

				page.script = decoded.scripts;
			  
					// Initialize FormActionButtons API
					const formActionButtonsApi = appController.initFormActionButtons($page, page);
					if (formActionButtonsApi) {
						page.self.FormActionButtons = formActionButtonsApi;
					}
					// Initialize Tab Item Managers
					const tabItemManagersApi = appController.initTabItemManager($page, page);
					if (tabItemManagersApi) {
						page.self.TabItemManagers = tabItemManagersApi;
					}
				// Initialize DataTable before executing scripts if page has a table
				const hasDataTable = $page.find("[data-action='dataProfile']").length > 0;
				if (hasDataTable) {
					// Initialize table and wait for it to be ready
					appController.initDataTableAndWait($page, page).then(function(tableApi) {
						 
						 
					
						// Add table API to page data
						if (tableApi) {
							page.self.table = tableApi;
						}
					
						// Execute scripts after table is initialized
				 
					}).catch(function(err) {
						console.warn('Error initializing table:', err);
						// Execute scripts anyway even if table initialization fails
					 
					});
				} else {
					// No table, execute scripts immediately
			 
					
				}
				page.execScripts();
				appController.initPage($page);
			 
			},
			error: function (xhr) {
				$page.html(
					`<h4>
					خطا در بارگذاری صفحه ${xhr.status}
					</h4>`
				);
			}
		});

		 
	}
	 
	
	/**
	 * Initializes DataTable and returns a Promise that resolves with the table API
	 * This is called before executing page scripts to ensure table is available
	 */
	initDataTableAndWait($pageEl, page) {
		return new Promise(function(resolve, reject) {
			const $dataProfileSelect = $pageEl.find("[data-action='dataProfile']");
		 
			if ($dataProfileSelect.length === 0) {
				// No table in this page
				resolve(null);
				return;
			}
		 
			// Check if table already has a value selected
			const profileId = $dataProfileSelect.val();
		 
			if (!profileId) {
				// No profile selected yet - check if select has a default option
				// If it does, select it and initialize table
				const $firstOption = $dataProfileSelect.find('option:not([value=""])').first();
				if ($firstOption.length > 0 && $firstOption.val()) {
					// Select the first available option
					$dataProfileSelect.val($firstOption.val());
					profileId = $firstOption.val();
					// Continue to initialization below
				} else {
					// No options available, resolve with null
					// Scripts will execute and table will be available after user selects a profile
					resolve(null);
					return;
				}
			}
		 
			// Profile selected (either initially or after selecting first option)
			// Initialize immediately
			const editPath = $dataProfileSelect.attr("data-edit-path");
			const deletePath = $dataProfileSelect.attr("data-delete-path");
				
				post("/System/FetchDataTableProfile", { id: profileId }, function(r) {
					if (!r.isSuccess) {
						reject(new Error(r.message || 'Failed to fetch table profile'));
						return;
					}
					
					r.data.forEach(z => {
						if (z.render) {
							z.render = z.render.replace("{editpath}", editPath);
							z.render = z.render.replace("{deletepath}", deletePath);
							z.render = eval(`(${z.render})`);
						}
					});
					
					// Ensure table element exists
					let $table = $pageEl.find('#itemsTable');
					if ($table.length === 0) {
						$pageEl.append(`<table id="itemsTable" class="table table-row-bordered nowrap table-hover" style="width: 100%"></table>`);
						$table = $pageEl.find('#itemsTable');
					}
					
					const table = InitDataTabelProfile(
						$table,
						r.data,
						profileId,
						false,
						"/System/ExportToExcelProfile"
					);
					
					// Wait for table to be fully initialized
					if (table) {
						// InitDataTabelProfile returns the DataTable instance
						// Use table's ready event to ensure initialization is complete
						if (table.ready && typeof table.ready === 'function') {
							table.ready(function() {
								// Return the table API (which is the same as table instance)
								resolve(table);
							});
						} else {
							// Fallback: use setTimeout to ensure table is initialized
							setTimeout(function() {
								resolve(table);
							}, 200);
						}
					} else {
						resolve(null);
					}
				});
		});
	}
	initFormActionButtons($pageEl, page) {
		 
		// Only find buttons that have data-action attribute (these are form-action-buttons)
		// This way we don't accidentally remove other buttons in .form-action-buttons div
		const $buttons = $pageEl.find(".form-action-buttons button[data-action]")
		
		if ($buttons.length === 0) {
			// No FormActionButtons in this page
			return null;
		}

		const api = {};
		const appController = this;

		// Create API for each button type
		const buttonTypes = ['new', 'save', 'saveandnew', 'saveandclose'];
		
		$buttons.each(function(i,buttonType) {
			const type = $(buttonType).data("action");
			
			// Since we're already filtering by [data-action], type should always exist
			if (!type || type.length === 0) {
				return; // Skip if button doesn't have valid action
			}

			// Store the first button element (in case there are multiple)
			const buttonElement = buttonType;
			let isBlocked = false;
			let customHandler = null;

			// Create button API
			api[type] = {
				element: buttonElement,
				onclick: function(handler) {
					if (typeof handler === 'function') {
						customHandler = handler;
					} else {
						console.warn(`onclick handler for ${type} must be a function`);
					}
					return api[type]; // Return API for chaining
				},
				block: function(blocked) {
					if (blocked === undefined) {
						return isBlocked;
					}
					isBlocked = blocked === true;
					$button.prop('disabled', isBlocked);
					return api[type]; // Return API for chaining
				},
				baseaction: function() {
					// Default action for each button type
					switch (type) {
						case 'new_page':
							// Default: navigate to new page (same address but without ID)
							const currentAddress = page.address;
							const newAddress = currentAddress.split('?')[0].replace(/\/edit$/, '').replace(/\/new$/, '') + '/new';
							appController.addPage(newAddress);
							break;
						case 'save':
							// Default: submit form
							const $form = $pageEl.find('form');
							if ($form.length > 0) {
								$form.submit();
							}
							break;
						case 'saveandnew':
							// Default: save and then navigate to new page
							const $formSaveAndNew = $pageEl.find('form');
							if ($formSaveAndNew.length > 0) {
								// Submit form and then navigate to new page
								$formSaveAndNew.one('submit', function() {
									setTimeout(function() {
										const currentAddress = page.address;
										const newAddress = currentAddress.split('?')[0].replace(/\/edit$/, '').replace(/\/new$/, '') + '/new';
										appController.addPage(newAddress);
									}, 500);
								});
								$formSaveAndNew.submit();
							}
							break;
						case 'saveandclose':
							// Default: save and close current page
							const $formSaveAndClose = $pageEl.find('form');
							if ($formSaveAndClose.length > 0) {
								$formSaveAndClose.one('submit', function() {
									setTimeout(function() {
										appController.closePage(page.address);
									}, 500);
								});
								$formSaveAndClose.submit();
							} else {
								// If no form, just close the page
								appController.closePage(page.address);
							}
							break;
					}
				}
			};

			// Set up default click handler
			$(buttonElement).off('click.formActionButton').on('click.formActionButton', function(e) {
				e.preventDefault();
				
				// If button is blocked, do nothing
				if (isBlocked) {
					return false;
				}

				// If custom handler exists, call it with context
				if (customHandler) {
					const context = {
						element: buttonElement,
						buttonType: type,
						page: page,
						baseaction: api[type].baseaction.bind(api[type])
					};
					
					// Call custom handler with context as 'this'
					customHandler.call(context);
				} else {
					// Call default action
					api[type].baseaction();
				}
			});
		});

		return api;
	}

	/**
	 * Initialize Tab-Based Item Manager System
	 * Creates a generic item management system with tabs, add/delete functionality, and row selection
	 * Similar to FormActionButtons API pattern
	 * 
	 * Tab management is encapsulated per manager instance to prevent cross-tab interference
	 * in multi-tab applications where the same page can be open in multiple tabs
	 */
	initTabItemManager($pageEl, page) {
		 
		const $itemManagers = $pageEl.find("[data-tab-item-manager]"); 
		if ($itemManagers.length === 0) {
			return null;
		}

		const appController = this;
		const itemManagers = {};

		$itemManagers.each(function() {
			const $manager = $(this);
			const managerId = $manager.attr("data-tab-item-manager");
			const itemSelector = $manager.attr("data-item-selector") || ".entity-item";
			
			// Create unique namespace for this manager instance to prevent event conflicts
			const managerEventNamespace = `.tabItemManager_${managerId}_${KTUtil.getUniqueId("tabItemManager")}`;
			
			// Find elements - scoped to this manager only
			const $tabs = $manager.find(".nav-tabs");
			const $tabContent = $manager.find(".tab-content");
			
			// Remove old shared action buttons if they exist
			$manager.find("[data-action='add-item'], [data-action='delete-selected'], [data-action='search-items']").closest('.d-flex').remove();
			
			// Tab Objects - each tab has its own object with isolated state
			const tabObjects = {};
			
			// Initialize each tab pane as a separate object
			$tabContent.find(".tab-pane").each(function() {
				const $tabPane = $(this);
				const tabId = $tabPane.attr("id");
				const $container = $tabPane.find(".entity-item-container");
				const addUrl = $tabPane.attr("data-add-url") || $manager.attr("data-add-url");
				
				if ($container.length > 0 && tabId) {
					// Create unique namespace for this tab
					const tabEventNamespace = `${managerEventNamespace}_tab_${tabId}`;
					
					// Create footer for this tab (like table footer) - inside container
					const $footer = $(`
						<div class="entity-item-footer">
							<div class="entity-item-footer-content d-flex justify-content-start align-items-center gap-2">
								<button type="button" class="btn btn-sm btn-icon btn-active-icon-dark btn-color-danger" 
									data-action="delete-selected" disabled>
									<i class="fs-2 fa-light fa-trash" style="padding-right: 2px;padding-top: 2px;"></i>
								</button>
								<button type="button" class="btn btn-sm btn-icon btn-active-icon-dark btn-color-primary" 
									data-action="add-item" data-bs-toggle="tooltip" data-bs-placement="top" title="افزودن آیتم">
									<i class="fs-2 fa-jelly fa-light fa-circle-plus" style="padding-right: 2px;padding-top: 2px;"></i>
								</button>
								<div class="position-relative" style="width: 250px;">
									<input type="text" class="form-control" data-action="search-items" placeholder="جستجو..." 
										style="padding-right: 25px; font-size: 0.875rem;" />
									<span class="position-absolute top-50 translate-middle-y" 
										style="right: 10px; cursor: pointer; bottom: 3px;" data-action="clear-search">
										<i class="fa fa-times fs-8 text-muted"></i>
									</span>
								</div>
							</div>
						</div>
					`);
					
					// Append footer to container (inside container, like header)
					$container.append($footer);
					
					// Get footer elements
					const $addButton = $footer.find("[data-action='add-item']");
					const $deleteButton = $footer.find("[data-action='delete-selected']");
					const $searchInput = $footer.find("[data-action='search-items']");
					
					// Create Tab Object with isolated state
					const tabObject = appController.createTabObject(
						tabId,
						$tabPane,
						$container,
						$footer,
						$addButton,
						$deleteButton,
						$searchInput,
						addUrl,
						itemSelector,
						tabEventNamespace,
						managerId,
						page
					);
					
					tabObjects[tabId] = tabObject;
					
					// Initialize header even if container is empty
					tabObject.ensureHeaderExists();
				}
			});
			
			// Disable Bootstrap tab switching for this manager's tabs
			// Remove data-bs-toggle to prevent Bootstrap from handling clicks
			$tabs.find(".nav-link").each(function() {
				const $link = $(this);
				// Store original data-bs-toggle if exists
				if ($link.attr("data-bs-toggle")) {
					$link.data("original-bs-toggle", $link.attr("data-bs-toggle"));
					$link.removeAttr("data-bs-toggle");
				}
			});
			
			// Custom tab switching - scoped to this manager only
			// Handle tab switching manually to ensure proper isolation
			const tabClickEventName = `click.customTabSwitch${managerEventNamespace}`;
			$tabs.off(tabClickEventName).on(tabClickEventName, ".nav-link", function(e) {
				e.preventDefault();
				e.stopPropagation();
				e.stopImmediatePropagation(); // Prevent Bootstrap handlers
				
				const $clickedLink = $(this);
				const targetTabId = $clickedLink.attr("data-bs-target") || $clickedLink.attr("href");
				
				if (!targetTabId) return;
				
				// Extract tab ID from target (remove # if present)
				const tabId = targetTabId.replace(/^#/, '');
				
				// Check if this tab belongs to this manager
				if (!tabObjects[tabId]) return;
				
				// Find the tab pane within this manager's tab content only
				const $targetTabPane = $tabContent.find(`#${tabId}`);
				if ($targetTabPane.length === 0) return;
				
				// Remove active class from all tabs and panes in this manager only
				$tabs.find(".nav-link").removeClass("active");
				$tabContent.find(".tab-pane").removeClass("active show");
				
				// Activate clicked tab
				$clickedLink.addClass("active");
				$targetTabPane.addClass("active show");
				
				// Footer will be shown automatically with tab-pane (it's inside container)
				// Ensure header exists for active tab
				if (tabObjects[tabId]) {
					tabObjects[tabId].ensureHeaderExists();
					// Sync grid-template-columns when tab becomes visible
					// Use requestAnimationFrame to ensure tab is fully visible
					requestAnimationFrame(function() {
						setTimeout(function() {
							if (tabObjects[tabId] && typeof tabObjects[tabId].syncGridColumns === 'function') {
								tabObjects[tabId].syncGridColumns();
							}
						}, 50);
					});
				}
			});
			
			// Footer will be shown automatically with active tab-pane (it's inside container)
			
			// Create Manager API that aggregates all tabs
			const api = {
				element: $manager[0],
				managerId: managerId,
				tabs: tabObjects,
				getTab: function(tabId) {
					return tabObjects[tabId] || null;
				},
				getActiveTab: function() {
					const $activeTab = $tabContent.find(".tab-pane.active");
					if ($activeTab.length > 0) {
						const tabId = $activeTab.attr("id");
						return tabObjects[tabId] || null;
					}
					return null;
				}
			};
			
			// Store API
			itemManagers[managerId] = api;
		});
		
		return itemManagers;
	}
	
	/**
	 * Create a Tab Object with isolated state and functionality
	 * Each tab has its own selectedItems, event handlers, and UI elements
	 */
	createTabObject(tabId, $tabPane, $container, $footer, $addButton, $deleteButton, $searchInput, addUrl, itemSelector, eventNamespace, managerId, page) {
		// Isolated state for this tab
		const selectedItems = new Set();
		let addCustomHandler = null;
		let beforeAddHandler = null;
		let afterAddHandler = null;
		let deleteCustomHandler = null;
		let beforeDeleteHandler = null;
		let afterDeleteHandler = null;
		let selectCustomHandler = null;
		let searchTimeout = null;
		
		// Footer is inside container, so it will be shown/hidden with tab-pane automatically
		
		// Search functionality - scoped to this tab's container
		function filterItems(searchTerm) {
			const $body = $container.find('.entity-item-body');
			const $rows = $body.find('.entity-item-row');
			
			if (!searchTerm || searchTerm.trim() === '') {
				// Show all items
				$rows.show();
				return;
			}
			
			const searchLower = searchTerm.toLowerCase().trim();
			
			$rows.each(function() {
				const $row = $(this);
				let found = false;
				
				// Search in all visible text content and input values
				$row.find('.entity-item-cell').each(function() {
					const $cell = $(this);
					
					// Get text content
					const text = $cell.text().toLowerCase();
					if (text.includes(searchLower)) {
						found = true;
						return false; // break
					}
					
					// Get input values
					$cell.find('input:not([type="hidden"]), select, textarea').each(function() {
						const $input = $(this);
						const value = $input.val() || '';
						if (value.toString().toLowerCase().includes(searchLower)) {
							found = true;
							return false; // break
						}
						
						// For select, also check selected option text
						if ($input.is('select')) {
							const selectedText = $input.find('option:selected').text().toLowerCase();
							if (selectedText.includes(searchLower)) {
								found = true;
								return false; // break
							}
						}
					});
					
					if (found) return false; // break outer loop
				});
				
				if (found) {
					$row.show();
				} else {
					$row.hide();
				}
			});
		}
		
		// Initialize search input - scoped to this tab
		if ($searchInput.length > 0) {
			const searchEventName = `input.searchItems${eventNamespace} keyup.searchItems${eventNamespace}`;
			const clearEventName = `click.clearSearch${eventNamespace}`;
			
			$searchInput.off(searchEventName).on(searchEventName, function() {
				const searchTerm = $(this).val();
				
				// Debounce search
				clearTimeout(searchTimeout);
				searchTimeout = setTimeout(function() {
					filterItems(searchTerm);
				}, 300);
			});
			
			// Clear search on clear button click - scoped to this tab
			$searchInput.siblings('[data-action="clear-search"]').off(clearEventName).on(clearEventName, function() {
				$searchInput.val('');
				filterItems('');
			});
		}
		
		// Function to initialize column resizing
		function initColumnResizing($header, $container) {
			let isResizing = false;
			let currentColumnIndex = -1;
			let startX = 0;
			let startWidth = 0;
			let $currentHandle = null;
			
			// Detect RTL direction
			const isRTL = $('html').attr('dir') === 'rtl' || 
			             $('html').css('direction') === 'rtl' ||
			             document.documentElement.dir === 'rtl';
			
			// Get all resize handles
			$header.find('.resize-handle').each(function(index) {
				const $handle = $(this);
				const $headerCell = $handle.closest('.entity-item-header-cell');
				const cellIndex = $headerCell.index(); // Index in header (0 = checkbox, 1+ = data columns)
				const columnIndex = cellIndex - 1; // Data column index (0-based, excluding checkbox)
				
				$handle.on('mousedown', function(e) {
					e.preventDefault();
					e.stopPropagation();
					
					isResizing = true;
					currentColumnIndex = columnIndex;
					startX = e.pageX || e.clientX;
					$currentHandle = $handle;
					
					// Get current column width
					const $headerCells = $header.find('.entity-item-header-cell');
					const $targetCell = $headerCells.eq(cellIndex);
					startWidth = $targetCell.outerWidth();
					
					$handle.addClass('active');
					$('body').css('cursor', 'col-resize').css('user-select', 'none');
					
					// Add overlay to prevent text selection
					const $overlay = $('<div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 9999; cursor: col-resize;"></div>');
					$('body').append($overlay);
					
					// Mouse move handler
					const mouseMoveHandler = function(e) {
						if (!isResizing) return;
						
						const currentX = e.pageX || e.clientX;
						let diffX = currentX - startX;
						
						// In RTL, reverse the direction
						if (isRTL) {
							diffX = -diffX;
						}
						
						const newWidth = Math.max(100, startWidth + diffX); // Minimum width 100px
						
						// Update column width
						updateColumnWidth($header, $container, currentColumnIndex, newWidth);
					};
					
					// Mouse up handler
					const mouseUpHandler = function() {
						isResizing = false;
						$handle.removeClass('active');
						$('body').css('cursor', '').css('user-select', '');
						$overlay.remove();
						$(document).off('mousemove', mouseMoveHandler);
						$(document).off('mouseup', mouseUpHandler);
					};
					
					$(document).on('mousemove', mouseMoveHandler);
					$(document).on('mouseup', mouseUpHandler);
				});
			});
		}
		
		// Function to sync grid-template-columns from header to all rows
		function syncGridTemplateColumns($header, $container) {
			const gridTemplateColumns = $header.css('grid-template-columns');
			if (gridTemplateColumns) {
				$container.find('.entity-item-row').css('grid-template-columns', gridTemplateColumns);
			}
		}
		
		// Function to update column width
		function updateColumnWidth($header, $container, columnIndex, newWidth) {
			// Get current grid template
			let currentGridTemplate = $header.css('grid-template-columns');
			// Parse grid template - format: "50px repeat(N, minmax(150px, auto))"
			// We need to handle the repeat() syntax
			if (currentGridTemplate.includes('repeat')) {
				// Extract the repeat part
				const repeatMatch = currentGridTemplate.match(/repeat\((\d+),\s*(.+?)\)/);
				if (repeatMatch) {
					const repeatCount = parseInt(repeatMatch[1]);
					
					// Convert to explicit column widths
					const columns = ['50px']; // Checkbox column
					for (let i = 0; i < repeatCount; i++) {
						if (i === columnIndex) {
							columns.push(`${newWidth}px`);
						} else {
							// Get current width of this column from DOM
							const $headerCells = $header.find('.entity-item-header-cell');
							const $cell = $headerCells.eq(i + 1); // +1 for checkbox
							const currentWidth = $cell.outerWidth();
							columns.push(`${currentWidth}px`);
						}
					}
					
					const newGridTemplate = columns.join(' ');
					$header.css('grid-template-columns', newGridTemplate);
					syncGridTemplateColumns($header, $container);
				}
			} else {
				// Already explicit columns
				const columns = currentGridTemplate.split(' ');
				const targetIndex = columnIndex + 1; // +1 for checkbox column
				if (columns[targetIndex]) {
					const currentValue = columns[targetIndex];
					if (currentValue.includes('minmax')) {
						columns[targetIndex] = `minmax(${newWidth}px, auto)`;
					} else {
						columns[targetIndex] = `${newWidth}px`;
					}
					
					const newGridTemplate = columns.join(' ');
					$header.css('grid-template-columns', newGridTemplate);
					syncGridTemplateColumns($header, $container);
				}
			}
		}
		
		// Function to ensure header exists (even if no items)
		function ensureHeaderExists() {
			// Check if header already exists
			if ($container.find('.entity-item-header').length > 0) {
				return;
			}
			
			// Try to get headers from existing items first
			const $firstItem = $container.find(itemSelector).first();
			if ($firstItem.length > 0) {
				transformContainerToTable();
				return;
			}
			
			// No items exist, fetch partial to extract headers
			if (addUrl) {
				$.get(addUrl, function(data) {
					const $tempItem = $(data);
					if ($tempItem.hasClass('entity-item')) {
						// Extract headers from partial view
						const headers = [];
						$tempItem.find('.col-md-3, .col-md-4, .col-md-6, .col-md-12').each(function() {
							const $col = $(this);
							const $label = $col.find('label');
							if ($label.length > 0) {
								headers.push($label.text().trim() || '');
							} else {
								const $fileUploader = $col.find('[file-uploader], fileuploader');
								if ($fileUploader.length > 0) {
									headers.push('فایل');
								} else {
									headers.push('');
								}
							}
						});
						
						if (headers.length > 0) {
							createHeaderFromHeaders(headers);
						}
					}
				});
			}
		}
		
		// Function to create header from headers array
		function createHeaderFromHeaders(headers) {
			// Check if header already exists
			if ($container.find('.entity-item-header').length > 0) {
				return;
			}
			
			// Calculate grid template columns
			const columnCount = headers.length;
			const gridTemplateColumns = `50px repeat(${columnCount}, minmax(150px, auto))`;
			
			// Create header row
			const $header = $('<div class="entity-item-header"></div>');
			$header.css('grid-template-columns', gridTemplateColumns);
			$header.append('<div class="entity-item-header-cell"></div>'); // Checkbox column
			headers.forEach(function(headerText, index) {
				const $headerCell = $(`<div class="entity-item-header-cell">${headerText}</div>`);
				// Add resize handle to all columns except the last one
				if (index < headers.length - 1) {
					$headerCell.append('<div class="resize-handle"></div>');
				}
				$header.append($headerCell);
			});
			
			// Create body wrapper
			const $body = $('<div class="entity-item-body"></div>');
			
			// Save footer before clearing container
			const $existingFooter = $container.find('.entity-item-footer');
			
			// Clear container and add header + body
			$container.prepend($body);
			$container.prepend($header);
			
			
			// Restore footer if it existed
			// Note: Event handlers will be re-attached after tabObject is created
		 
			
			// Sync grid-template-columns from header to all rows after DOM is ready
			// Use requestAnimationFrame to ensure DOM is fully rendered, especially for hidden tabs
			const syncAfterRender = function() {
				requestAnimationFrame(function() {
					// Check if tab is visible, if not, wait a bit more
					if ($tabPane.hasClass('active') && $tabPane.hasClass('show')) {
						syncGridTemplateColumns($header, $container);
					} else {
						// Tab is hidden, sync when it becomes visible
						setTimeout(syncAfterRender, 100);
					}
				});
			};
			syncAfterRender();
			
			// Initialize column resizing
			initColumnResizing($header, $container);
		}
		
	 
		// Function to transform container to table-like structure
		function transformContainerToTable() {
			// Check if already transformed
			if ($container.find('.entity-item-header').length > 0) {
				return;
			}
			
			const $firstItem = $container.find(itemSelector).first();
			if ($firstItem.length === 0) {
				return; // No items to transform
			}
			
			// Extract labels from first item to create header
			const headers = [];
			$firstItem.find('.col-md-3, .col-md-4, .col-md-6, .col-md-12').each(function() {
				const $col = $(this);
				const $label = $col.find('label');
				if ($label.length > 0) {
					headers.push($label.text().trim() || '');
				} else {
					// Check for fileuploader or other components
					const $fileUploader = $col.find('[file-uploader], fileuploader');
					if ($fileUploader.length > 0) {
						headers.push('فایل');
					} else {
						headers.push('');
					}
				}
			});
			
			// Calculate grid template columns
			const columnCount = headers.length;
			const gridTemplateColumns = `50px repeat(${columnCount}, minmax(150px, auto))`;
			
			// Create header row
			const $header = $('<div class="entity-item-header"></div>');
			$header.css('grid-template-columns', gridTemplateColumns);
			$header.append('<div class="entity-item-header-cell"></div>'); // Checkbox column
			headers.forEach(function(headerText, index) {
				const $headerCell = $(`<div class="entity-item-header-cell">${headerText}</div>`);
				// Add resize handle to all columns except the last one
				if (index < headers.length - 1) {
					$headerCell.append('<div class="resize-handle"></div>');
				}
				$header.append($headerCell);
			});
			
			// Initialize column resizing
	
			
			// Create body wrapper
			const $body = $('<div class="entity-item-body"></div>');
			
			// Transform all items to rows
			$container.find(itemSelector).each(function() {
				const $item = $(this);
				const $row = $('<div class="entity-item-row"></div>');
				$row.attr('data-item-id', $item.attr('data-item-id') || KTUtil.getUniqueId("item"));
				$row.css('grid-template-columns', gridTemplateColumns);
				
				// Add checkbox cell
				const $checkboxCell = $('<div class="entity-item-cell"></div>');
				if ($item.find("[data-action='select-item']").length === 0) {
					const $checkbox = $(`
						<div class="form-check form-check-sm form-check-custom form-check-solid">
							<input class="" type="checkbox" data-action="select-item" />
						</div>
					`);
					$checkboxCell.append($checkbox);
				} else {
					$checkboxCell.append($item.find("[data-action='select-item']").closest('.form-check'));
				}
				$row.append($checkboxCell);
				
				// Transform columns to cells
				$item.find('.col-md-3, .col-md-4, .col-md-6, .col-md-12').each(function () {
					
					const $col = $(this);
					const $cell = $('<div class="entity-item-cell"></div>');
					
					// Move input/select/content to cell (hide label)
					$col.find('label').hide();
					
					// Move entity selector container (entire div, not just the element)
					const $entitySelector = $col.find('.entity-selector-wrapper');
					if ($entitySelector.length > 0) {
						$entitySelector.appendTo($cell);
					}

					// Move fileuploader container (entire div, not just the element)
					const $fileUploader = $col.find('fileuploader, [file-uploader], .file-uploader-container');
					if ($fileUploader.length > 0) {
						// Move the entire fileuploader container
						const $fileContainer = $fileUploader.closest('.file-uploader-container');
						if ($fileContainer.length > 0) {
							$fileContainer.appendTo($cell);
						} else if ($fileUploader.hasClass('file-uploader-container')) {
							$fileUploader.appendTo($cell);
						} else {
							$fileUploader.appendTo($cell);
						}
					}
					
					// Move other form elements
					$col.find('input:not([type="hidden"]), select, textarea')
						.not($fileUploader.find('input, select, textarea'))
						.not($entitySelector.find('input, select, textarea'))
						.appendTo($cell);

					$col.find('input[type="hidden"]')
						.not($entitySelector.find('input[type="hidden"]'))
						.appendTo($cell);

					$col.find('[data-invalidmessagespan]').appendTo($cell);
					
					// If cell is empty, add a placeholder
					if ($cell.children().length === 0) {
						$cell.html('&nbsp;');
					}
					
					$row.append($cell);
				});
				$item.find("[type=hidden]").prependTo($row)
				$body.append($row);
			});
			
			// Save footer before clearing container
			const $existingFooter = $container.find('.entity-item-footer');
			
			// Clear container and add header + body
		 
			$container.append($header);
			$container.append($body);
			
			// Restore footer if it existed
			// Event handlers will be re-attached by reattachFooterHandlers() after tabObject is created
			if ($existingFooter.length > 0) {
				$container.append($existingFooter);
			}
			
			// Initialize file uploaders for transformed items
			if (typeof initFileUploaders === 'function') {
				initFileUploaders($body);
			}
			
			// Sync grid-template-columns from header to all rows after DOM is ready
			// Use requestAnimationFrame to ensure DOM is fully rendered, especially for hidden tabs
			const syncAfterRender = function() {
				requestAnimationFrame(function() {
					// Check if tab is visible, if not, wait a bit more
					if ($tabPane.hasClass('active') && $tabPane.hasClass('show')) {
						syncGridTemplateColumns($header, $container);
					} else {
						// Tab is hidden, sync when it becomes visible
						setTimeout(syncAfterRender, 100);
					}
				});
			};
			syncAfterRender();
			
			initColumnResizing($header, $container);
			
			// Re-initialize selection for new structure - scoped to this tab
			const selectEventName = `change.itemSelect${eventNamespace}`;
			$container.find('.entity-item-row').each(function() {
				const $row = $(this);
				const itemId = $row.attr('data-item-id');
				
				$row.find("[data-action='select-item']").off(selectEventName).on(selectEventName, function() {
					const isChecked = $(this).prop('checked');
					
					if (isChecked) {
						$row.addClass('selected');
						selectedItems.add(itemId);
					} else {
						$row.removeClass('selected');
						selectedItems.delete(itemId);
					}
					
					updateDeleteButtonState();
					
					if (selectCustomHandler) {
						const context = {
							element: $row[0],
							itemId: itemId,
							isSelected: isChecked,
							tabObject: tabObject,
							baseaction: tabObject.select.baseaction.bind(tabObject.select)
						};
						selectCustomHandler.call(context, itemId, isChecked);
					} else {
						tabObject.select.baseaction(itemId, isChecked);
					}
				});
			});
		}
		
		// Update delete button state
		function updateDeleteButtonState() {
			if (selectedItems.size > 0) {
				$deleteButton.prop('disabled', false).removeClass('disabled');
			} else {
				$deleteButton.prop('disabled', true).addClass('disabled');
			}
		}
		
		// Create Tab Object API
		const tabObject = {
			tabId: tabId,
			$tabPane: $tabPane,
			$container: $container,
			$footer: $footer,
			selectedItems: selectedItems,
			eventNamespace: eventNamespace,
			ensureHeaderExists: ensureHeaderExists,
			filterItems: filterItems,
			syncGridColumns: function() {
				const $header = $container.find('.entity-item-header');
				if ($header.length > 0) {
					syncGridTemplateColumns($header, $container);
				}
			},
			add: {
				onclick: function(handler) {
					if (typeof handler === 'function') {
						addCustomHandler = handler;
					}
					return tabObject.add;
				},
				onbeforeadd: function(handler) {
					if (typeof handler === 'function') {
						beforeAddHandler = handler;
					}
					return tabObject.add;
				},
				onafteradd: function(handler) {
					if (typeof handler === 'function') {
						afterAddHandler = handler;
					}
					return tabObject.add;
				},
				baseaction: function() {
					// Default: fetch partial and append to this tab's container
					if (addUrl) {
						$.get(addUrl, function (data) {
							 
							// Partial views already have entity-item wrapper
							let $item = $(data);
							const itemId = KTUtil.getUniqueId("item");
							
							// Ensure data-item-id
							if ($item.hasClass('entity-item')) {
								$item.attr("data-item-id", itemId);
							} else {
								$item = $('<div class="entity-item" data-item-id="' + itemId + '"></div>').append($item);
							}
							
							// Get body wrapper or create it
							let $body = $container.find('.entity-item-body');
							if ($body.length === 0) {
								// Container not transformed yet, check if we need to create header first
								
								const $existingItems = $container.find(itemSelector);
								if ($existingItems.length === 0) {
									// No items yet, create header structure from the new item
									const headers = [];
									$item.find('.col-md-3, .col-md-4, .col-md-6, .col-md-12').each(function() {
										const $col = $(this);
										const $label = $col.find('label');
										let headerText = '';
										if ($label.length > 0) {
											headerText = $label.text().trim() || '';
										} else {
											const $fileUploader = $col.find('[file-uploader], fileuploader');
											if ($fileUploader.length > 0) {
												headerText = 'فایل';
											}
										}
										headers.push(headerText);
									});
									
									// Create header from headers array
									createHeaderFromHeaders(headers);
									$body = $container.find('.entity-item-body');
								} else {
									// Transform existing container
									transformContainerToTable();
									$body = $container.find('.entity-item-body');
								}
							}
							
							// Get grid template from header if exists
							const $existingHeader = $container.find('.entity-item-header');
							let gridTemplateColumns = '50px repeat(auto-fill, minmax(150px, auto))';
							if ($existingHeader.length > 0) {
								gridTemplateColumns = $existingHeader.css('grid-template-columns') || gridTemplateColumns;
							}
							
							// Create row from item
							const $row = $('<div class="entity-item-row"></div>');
							$row.attr('data-item-id', itemId);
							$row.css('grid-template-columns', gridTemplateColumns);
							
							// Add checkbox cell
							const $checkboxCell = $('<div class="entity-item-cell"></div>');
							const $checkbox = $(`
								<div class="form-check form-check-sm form-check-custom form-check-solid">
									<input class=" " type="checkbox" data-action="select-item" />
								</div>
							`);
							$checkboxCell.append($checkbox);
							$row.append($checkboxCell);
							
							// Transform columns to cells
							$item.find('.col-md-3, .col-md-4, .col-md-6, .col-md-12').each(function() {
								const $col = $(this);
								const $cell = $('<div class="entity-item-cell"></div>');
								
								// Move input/select/content to cell (hide label)
								$col.find('label').hide();

								// Move entity selector container (entire div, not just the element)
								const $entitySelector = $col.find('.entity-selector-wrapper');
								if ($entitySelector.length > 0) {
									$entitySelector.appendTo($cell);
								}
								
								// Move fileuploader container (entire div, not just the element)
								let $fileUploader = $col.find('fileuploader, [file-uploader]');
								if ($fileUploader.length === 0) {
									$fileUploader = $col.find('.file-uploader-container');
								}
								
								if ($fileUploader.length > 0) {
									// Move the entire fileuploader container
									const $fileContainer = $fileUploader.closest('.file-uploader-container');
									if ($fileContainer.length > 0 && $fileContainer[0] !== $fileUploader[0]) {
										$fileContainer.appendTo($cell);
									} else if ($fileUploader.hasClass('file-uploader-container')) {
										$fileUploader.appendTo($cell);
									} else {
										$fileUploader.appendTo($cell);
									}
								}
								
								// Move other form elements (excluding those inside fileuploader)
								const $fileUploaderInputs = $fileUploader.length > 0 ? $fileUploader.find('input, select, textarea') : $();
								const $entitySelectorInputs = $entitySelector.length > 0 ? $entitySelector.find('input, select, textarea') : $();

								$col.find('input:not([type="hidden"]), select, textarea')
									.not($fileUploaderInputs)
									.not($entitySelectorInputs)
									.appendTo($cell);

								$col.find('input[type="hidden"]')
									.not($fileUploaderInputs)
									.not($entitySelectorInputs)
									.appendTo($cell);

								$col.find('[data-invalidmessagespan]').appendTo($cell);
								
								if ($cell.children().length === 0) {
									$cell.html('&nbsp;');
								}
								
								$row.append($cell);
							});

							$item.find("[type='hidden']").prependTo($row)
							
							// Call beforeAddHandler if exists (before appending to DOM)
							let shouldAdd = false; // Default: don't add unless baseaction is called
							if (beforeAddHandler) {
								const context = {
									element: $row[0],
									$element: $row,
									itemId: itemId,
									tabId: tabId,
									tabObject: tabObject,
									page: page,
									baseaction: function() {
										shouldAdd = true;
									}
								};
								beforeAddHandler.call(context, $row[0]);
								// If baseaction was not called, don't add the item
								if (!shouldAdd) {
									return;
								}
							} else {
								// If no handler, proceed with default action
								shouldAdd = true;
							}
							
							$body.append($row);
							
							// Sync grid-template-columns from header to ensure alignment
							const $headerForRowSync = $container.find('.entity-item-header');
							if ($headerForRowSync.length > 0) {
								const headerGridTemplate = $headerForRowSync.css('grid-template-columns');
								if (headerGridTemplate) {
									$row.css('grid-template-columns', headerGridTemplate);
								}
							}
							
							// Initialize forms and file uploaders
							initItemsForms($row);
							
							// Ensure file uploaders are initialized (in case initItemsForms didn't catch them)
							setTimeout(function() {
								if (typeof initFileUploaders === 'function') {
									initFileUploaders($row);
								}
								
								// Re-apply search filter if search is active
								if ($searchInput.length > 0) {
									const searchTerm = $searchInput.val();
									if (searchTerm) {
										filterItems(searchTerm);
									}
								}
								
								// Final sync to ensure perfect alignment after all initialization
								const $headerForSync = $container.find('.entity-item-header');
								if ($headerForSync.length > 0) {
									const finalGridTemplate = $headerForSync.css('grid-template-columns');
									if (finalGridTemplate) {
										$row.css('grid-template-columns', finalGridTemplate);
									}
								}
							}, 100);
							
							// Initialize selection checkbox - scoped to this tab
							const selectEventName = `change.itemSelect${eventNamespace}`;
							$row.find("[data-action='select-item']").off(selectEventName).on(selectEventName, function() {
								const isChecked = $(this).prop('checked');
								
								if (isChecked) {
									$row.addClass('selected');
									selectedItems.add(itemId);
								} else {
									$row.removeClass('selected');
									selectedItems.delete(itemId);
								}
								
								updateDeleteButtonState();
								
								if (selectCustomHandler) {
									const context = {
										element: $row[0],
										itemId: itemId,
										isSelected: isChecked,
										tabObject: tabObject,
										baseaction: tabObject.select.baseaction.bind(tabObject.select)
									};
									selectCustomHandler.call(context, itemId, isChecked);
								} else {
									tabObject.select.baseaction(itemId, isChecked);
								}
							});
							
							// Call afterAddHandler if exists (after item is fully added and initialized)
							if (afterAddHandler) {
								const context = {
									element: $row[0],
									$element: $row,
									itemId: itemId,
									tabId: tabId,
									tabObject: tabObject,
									page: page,
									baseaction: function() {
										// After add doesn't need to prevent default action
										// but we provide it for consistency
									}
								};
								afterAddHandler.call(context, $row[0]);
							}
						});
					} else {
						console.warn('No add URL found for tab:', tabId);
					}
				}
			},
			delete: {
				onclick: function(handler) {
					if (typeof handler === 'function') {
						deleteCustomHandler = handler;
					}
					return tabObject.delete;
				},
				onbeforedelete: function(handler) {
					if (typeof handler === 'function') {
						beforeDeleteHandler = handler;
					}
					return tabObject.delete;
				},
				onafterdelete: function(handler) {
					if (typeof handler === 'function') {
						afterDeleteHandler = handler;
					}
					return tabObject.delete;
				},
				baseaction: function() {
					// Default: remove selected items from this tab's container
					if (selectedItems.size === 0) {
						toastr.warning('لطفاً حداقل یک آیتم را انتخاب کنید', 'هشدار');
						return;
					}
					
					// Get elements to be deleted before removing them
					const elementsToDelete = [];
					selectedItems.forEach(function(itemId) {
						const $element = $container.find(`.entity-item-row[data-item-id="${itemId}"], [data-item-id="${itemId}"]`);
						if ($element.length > 0) {
							elementsToDelete.push({
								itemId: itemId,
								element: $element[0],
								$element: $element
							});
						}
					});
					
					// Call beforeDeleteHandler for each element if exists
					const itemsToDelete = [];
					if (beforeDeleteHandler) {
						elementsToDelete.forEach(function(itemInfo) {
							let shouldDelete = false; // Default: don't delete unless baseaction is called
							const context = {
								element: itemInfo.element,
								$element: itemInfo.$element,
								itemId: itemInfo.itemId,
								tabId: tabId,
								tabObject: tabObject,
								page: page,
								baseaction: function() {
									shouldDelete = true;
								}
							};
							beforeDeleteHandler.call(context, itemInfo.element);
							// If baseaction was called, add to items to delete
							if (shouldDelete) {
								itemsToDelete.push(itemInfo.itemId);
							}
						});
					} else {
						// If no handler, proceed with default action for all items
						elementsToDelete.forEach(function(itemInfo) {
							itemsToDelete.push(itemInfo.itemId);
						});
					}
					
					// If no items to delete, return early
					if (itemsToDelete.length === 0) {
						return;
					}
					
					// Store deleted items before clearing
					const deletedItems = itemsToDelete.slice();
					
					// Remove elements
					itemsToDelete.forEach(function(itemId) {
						// Remove row
						$container.find(`.entity-item-row[data-item-id="${itemId}"], [data-item-id="${itemId}"]`).remove();
						// Remove from selectedItems
						selectedItems.delete(itemId);
					});
					
					updateDeleteButtonState();
					
					// Call afterDeleteHandler if exists
					if (afterDeleteHandler) {
						const context = {
							deletedItems: deletedItems,
							tabId: tabId,
							tabObject: tabObject,
							page: page,
							baseaction: function() {
								// After delete doesn't need to prevent default action
								// but we provide it for consistency
							}
						};
						afterDeleteHandler.call(context, deletedItems);
					}
				}
			},
			select: {
				onclick: function(handler) {
					if (typeof handler === 'function') {
						selectCustomHandler = handler;
					}
					return tabObject.select;
				},
				baseaction: function(itemId, isSelected) {
					// Default: toggle selection
					if (isSelected) {
						selectedItems.add(itemId);
						// Update visual state
						$container.find(`.entity-item-row[data-item-id="${itemId}"], [data-item-id="${itemId}"]`).addClass('selected');
					} else {
						selectedItems.delete(itemId);
						// Update visual state
						$container.find(`.entity-item-row[data-item-id="${itemId}"], [data-item-id="${itemId}"]`).removeClass('selected');
					}
					updateDeleteButtonState();
				}
			},
			getSelectedItems: function() {
				return Array.from(selectedItems);
			},
			selectAll: function() {
				$container.find('.entity-item-row, ' + itemSelector).each(function() {
					const $item = $(this);
					const itemId = $item.attr("data-item-id") || KTUtil.getUniqueId("item");
					if (!$item.attr("data-item-id")) {
						$item.attr("data-item-id", itemId);
					}
					selectedItems.add(itemId);
					$item.find("[data-action='select-item']").prop('checked', true);
					$item.addClass('selected');
				});
				updateDeleteButtonState();
			},
			deselectAll: function() {
				selectedItems.clear();
				$container.find("[data-action='select-item']").prop('checked', false);
				$container.find('.entity-item-row').removeClass('selected');
				updateDeleteButtonState();
			}
		};
		
		// Handle add button click - scoped to this tab
		const addEventName = `click.itemManager${eventNamespace}`;
		$addButton.off(addEventName).on(addEventName, function(e) {
			e.preventDefault();
			e.stopPropagation();
			
			if (addCustomHandler) {
				const context = {
					element: $addButton[0],
					tabId: tabId,
					tabObject: tabObject,
					page: page,
					baseaction: tabObject.add.baseaction.bind(tabObject.add)
				};
				addCustomHandler.call(context);
			} else {
				tabObject.add.baseaction();
			}
		});
		
		// Handle delete button click - scoped to this tab
		const deleteEventName = `click.itemManager${eventNamespace}`;
		$deleteButton.off(deleteEventName).on(deleteEventName, function(e) {
			e.preventDefault();
			e.stopPropagation();
			
			if (deleteCustomHandler) {
				const context = {
					element: $deleteButton[0],
					tabId: tabId,
					selectedItems: Array.from(selectedItems),
					tabObject: tabObject,
					page: page,
					baseaction: tabObject.delete.baseaction.bind(tabObject.delete)
				};
				deleteCustomHandler.call(context);
			} else {
				tabObject.delete.baseaction();
			}
		});
		
		// Initialize delete button state
		updateDeleteButtonState();
		
		// Re-attach footer handlers in case footer was restored during header creation
		 
		
		// Initialize existing items if any
		const $firstItem = $container.find(itemSelector).first();
		if ($firstItem.length > 0) {
			transformContainerToTable();
			// Re-attach footer handlers after transform
		}
		
		// Sync grid columns for active tab on initialization
		if ($tabPane.hasClass('active') && $tabPane.hasClass('show')) {
			requestAnimationFrame(function() {
				setTimeout(function() {
					if (typeof tabObject.syncGridColumns === 'function') {
						tabObject.syncGridColumns();
					}
				}, 100);
			});
		}
		
		return tabObject;
	}

	setActivePage(page, pushState = true) {
		if (this.activePage) this.activePage.deactivate();
		this.activePage = page;
		page.activate();

		const address = page.address;
		if (pushState) {
			// Maintain in-app activation history (trim forward if user branched)
			if (this.historyPointer < this.activationHistory.length - 1) {
				this.activationHistory = this.activationHistory.slice(0, this.historyPointer + 1);
			}
			if (this.activationHistory[this.historyPointer] !== address) {
				this.activationHistory.push(address);
				this.historyPointer = this.activationHistory.length - 1;
			}

			let newUrl = address.startsWith("/") ? address : "/" + address;
			history.pushState({ address: address }, "", newUrl);
		}
	}

	closePage(address) {
		let page = this.pages.find(p => p.address === address);
		if (!page) return;

		page.destroy();
		this.pages = this.pages.filter(p => p.address !== address);

		// Remove address entries from activation history and fix pointer
		if (this.activationHistory && this.activationHistory.length > 0) {
			this.activationHistory = this.activationHistory.filter(a => a !== address);
			if (this.historyPointer >= this.activationHistory.length) {
				this.historyPointer = this.activationHistory.length - 1;
			}
		}

		if (this.activePage === page && this.pages.length > 0) {
			this.setActivePage(this.pages[this.pages.length - 1]);
		} else if (this.activePage === page) {
			this.activePage = null;
		}

		// Remove tab and page DOM
		// $(`#tabsContainer .page_tab[data-address="${address}"]`).remove();
		// $(`#pagesContainer #${address}`).remove();
	}

	initPage(el) {
		 
		const $el = $(el);
		initDataTableProflie($el);
		initItemsForms($el);
		initFileUploaders($el);
		this.createBootstrapTooltips(el);
		 
 
	}

	createBootstrapTooltip(el, options) {
		if (el.getAttribute("data-kt-initialized") === "1") {
			return;
		}

		var delay = {};

		// Handle delay options
		if (el.hasAttribute('data-bs-delay-hide')) {
			delay['hide'] = el.getAttribute('data-bs-delay-hide');
		}

		if (el.hasAttribute('data-bs-delay-show')) {
			delay['show'] = el.getAttribute('data-bs-delay-show');
		}

		if (delay) {
			options['delay'] = delay;
		}

		// Check dismiss options
		if (el.hasAttribute('data-bs-dismiss') && el.getAttribute('data-bs-dismiss') == 'click') {
			options['dismiss'] = 'click';
		}

		// Initialize popover
		var tp = new bootstrap.Tooltip(el, options);

		// Handle dismiss
		if (options['dismiss'] && options['dismiss'] === 'click') {
			// Hide popover on element click
			el.addEventListener("click", function (e) {
				tp.hide();
			});
		}

		el.setAttribute("data-kt-initialized", "1");

		return tp;
	}
	createBootstrapTooltips(el) {
		var tooltipTriggerList = [].slice.call(document.querySelectorAll('[title]'));
		 
		var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
			appController.createBootstrapTooltip(tooltipTriggerEl, {});
		});
	}
}

function CalculationCountUnreadNotifications() {
	 
 
	var unreadNotification = $("#notificationSection").children().length;
	$("#notificationUnreadCount").html(unreadNotification)

	if (unreadNotification === 0) {
		$("#kt_menu_item_notification").removeClass("has-notification");
	}
	else {
		$("#kt_menu_item_notification").addClass("has-notification");
	}

}
function markAsReadHeader(notificationId, $element) {
	 
	post('/System/Notify/MarkAsRead', notificationId,
		function (r) {
			if (!r.isSuccess) return error2(r.message);
			$element.fadeOut(500, function () {
				$element.remove();
				CalculationCountUnreadNotifications();
			})
			toastr.success('اعلان به عنوان خوانده شده علامت‌گذاری شد');

		


		});
}



 

function initDataTableProflie(page) {
	let currentTable;
	 
 
	if (page) {
		page.find("[data-action='dataProfile']")
			.change(function () {

				if (currentTable) {
					$(currentTable.containers()[0]).parent().append(`<table id="itemsTable" class="table table-row-bordered nowrap table-hover" style="width: 100%"></table>`)
					currentTable.destroy(true);
					currentTable = undefined;
				}

				var id = $(this).val();
				if (!id) {
					return toastr.error(`هیچ نمایه داده ای یافت نشد .`, 'خطا');
				}
				let editPath = $(this).attr("data-edit-path");
				let deletePath = $(this).attr("data-delete-path");

				post("/System/FetchDataTableProfile",
					{ id },
					function (r) {
						if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');

						r.data.forEach(z => {

							if (z.render) {
								z.render = z.render.replace("{editpath}", editPath);
								z.render = z.render.replace("{deletepath}", deletePath);
								z.render = eval(`(${z.render})`)

							}
						})
						currentTable = InitDataTabelProfile(
							page.find('#itemsTable'),
							r.data,
							id,
							false,
							"/System/ExportToExcelProfile"
						);
					});
			});

		page.find("[data-action='editDataProfile']").on("click",function () {
			let profileId = page.find("[data-action='dataProfile']").val();
			if (!profileId) {
				return toastr.error(`هیچ نمایه داده ای جهت ویرایش یافت نشد .`, 'خطا');
			}
			appController.addPage("/datatableprofilebuilder/edit?id=" + profileId);
		 
		});

		page.find("[data-action='newDataProfile']").click(function () {
			let entityNam = page.find("[data-action='dataProfile']").attr("data-entityName");
			if (!entityNam) {
				return toastr.error(`موجودیت برای ایجاد نمایه داده یافت نشد .`, 'خطا');
			}
			appController.addPage("/datatableprofilebuilder/new?entityName=" + entityNam);
		})
	}
	else {
		$("[data-action='dataProfile']")
			.change(function () {

				if (currentTable) {
					$(currentTable.containers()[0]).parent().append(`<table id="itemsTable" class="table table-row-bordered nowrap table-hover" style="width: 100%"></table>`)
					currentTable.destroy(true);
					currentTable = undefined;
				}

				var id = $(this).val();
				if (!id) {
					return toastr.error(`هیچ نمایه داده ای یافت نشد .`, 'خطا');
				}
				let editPath = $(this).attr("data-edit-path");
				let deletePath = $(this).attr("data-delete-path");

				post("/System/FetchDataTableProfile",
					{ id },
					function (r) {
						if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');

						r.data.forEach(z => {

							if (z.render) {
								z.render = z.render.replace("{editpath}", editPath);
								z.render = z.render.replace("{deletepath}", deletePath);
								z.render = eval(`(${z.render})`)

							}
						})
						currentTable = InitDataTabelProfile(
							$('#itemsTable'),
							r.data,
							id,
							false,
							"/System/ExportToExcelProfile"
						);
					});
			}).change();
		$("[data-action='editDataProfile']").click(function () {
			let profileId = $("[data-action='dataProfile']").val();
			if (!profileId) {
				return toastr.error(`هیچ نمایه داده ای جهت ویرایش یافت نشد .`, 'خطا');
			}
			window.location = "/datatableprofilebuilder/edit?id=" + profileId;
		});
		$("[data-action='newDataProfile']").click(function () {
			let entityNam = $("[data-action='dataProfile']").attr("data-entityName");
			if (!entityNam) {
				return toastr.error(`موجودیت برای ایجاد نمایه داده یافت نشد .`, 'خطا');
			}
			window.location = "/datatableprofilebuilder/new?entityName=" + entityNam;
		})
	}


}

function renderHistoryTmpl(histories) {

	let $c = $(`<div></div>`)
	histories.forEach(model => {

		let typeName = "";

		switch (model.type) {
			case 0:
				typeName = "درج"
				break
			case 1:
				typeName = "ویرایش"
				break;
			case 2:
				typeName = "حذف"
				break;

		}
		let $body = $(`<div class="text-center">
	<div class="card m-5 px-3">
	<div class="bg-success-subtle card-header row align-content-center">
	 
				<div class="col-md-4">
				<span>نام ستون</span>
				</div>
				<div class="col-md-4">
				<span>مقدار قبلی</span>
				</div>
				<div class="col-md-4">
				<span>مقدار جدید</span>
				</div>
		 
	</div>
    <div class="card-body col-md-12">
    
 
    </div>
               <div class='bg-success-subtle card-header row align-content-center'>
                        <div class='col-md-4'>
                            <span>ایجاد کننده :</span>
                            <span>${model.createdByName}</span>
                        </div>

                        <div class='col-md-4'>
                            <span>تاریخ ایجاد :</span>
                            <span>${model.createdOnShamsiDateTime}</span>
                        </div>

						   <div class='col-md-4'>
                            <span>نوع عملیات:</span>
                            <span>${typeName}</span>
                        </div>

                        
                </div>
</div>
	</div>`)

		 

		model.auditLogDetails.forEach(z => {
			let $item = $(`
			<div class="row mb-3 mt-2 pb-3 pt-2">
				<div class="col-md-4">
				<span>${z.propertyTitle}</span>
				</div>
				<div class="col-md-4">
				<span>${z.oldValue}</span>
				</div>
				<div class="col-md-4">
				<span>${z.newValue}</span>
				</div>
			</div>
					<div class="separator separator-dashed"></div>
		`)

			$body.find(".card-body").append($item);

		})

		$c.append($body);

	})

	$.confirm({
		title: 'تاریخچه اطلاعات',
		content: $c,
		columnClass:"col-md-9",
		typeAnimated: true,
		buttons: {
			close: {
				text: 'بستن',
				btnClass: 'btn btn-danger',
				action: function () {
				}
			}
		}
	});
	
}
 
function validateError($el) {
 
	$el = $el || $("body");
	let haveError = false;

	$el.find("[required]").each((i, c) => {
		let value;
		let $input = $(c);
		if ($(c).attr("data-uploadFile")) {
			var databindName = $(c).attr("data-uploadFileName");
			value = $(c).find(`[data-bind='${databindName}']`).val();
		}
		else if ($(c).attr("data-entity-selector"))
		{
			$input = $(c).parent().data("entitySelector").$input;

			if ($(c).val() == '0') {
				value = null;
			}
			else {
				value = $(c).val();
			}
		}
		else {

			value = $(c).val();
		}

	 

		if (!value || value.length === 0) {
			if ($input.hasClass("entity-selector-multi-input")) {
				$input.parent().addClass("border-danger");
				$input.parent().removeClass("border-success");
				haveError = true;
			}
			else {
				$input.addClass("border-danger");
				$input.removeClass("border-success");
				haveError = true;
			}
		
		}
		else {
			if ($input.hasClass("entity-selector-multi-input")) {
				$input.parent().removeClass("border-danger");
				$input.parent().addClass("border-success");
			}
			else {
				$input.removeClass("border-danger");
				$input.addClass("border-success");
			}
		}


	})

	if (haveError) {
		toastr.error(`لطفا فیلد های اجباری را مقدار دهی کنید`, 'خطا')
	}
	return haveError;
}

function entityTableTmplProflie() {
	let $table = $(`
    <div class="card position-absolute   col-md-9" style="right: 10%;z-index: 20;" data-entitySelectCard="true">
		<div class="card-header align-content-center"> 
					<div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-action='close' aria-label="Close">
						<i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
					</div>
				</div>
		<div class="card-body table-responsive">
			<table  class="table table-row-bordered nowrap table-bordered table-striped table-hover">
					 
			 </table>
		</div>
		
	</div>`)

 

	return $table;

}

/**
 * Initialize entity select profile handlers using event delegation
 * This function sets up global event handlers that work for both existing and dynamically added elements
 * @param {jQuery} $container - Optional container to initialize existing elements (defaults to document)
 */
function initEntitySelectProfile($container) {
	$container = $container || $(document);
	
	// Initialize existing elements that haven't been initialized yet
	$container.find("[data-action-entity-select-profile]:not([data-action-entity-select-init='true'])").each(function() {
		const $el = $(this);
		const status = $el.attr("data-action-entity-select-profile");
		
		// Mark as initialized to prevent duplicate handlers
		$el.attr("data-action-entity-select-init", "true");
	});
}

/**
 * Handle entity select profile click event
 * This function is called when user clicks on select entity button
 */
function handleEntitySelectProfileClick(event) {
	const $button = $(event.currentTarget);
	const $el = $button.closest("[data-action-entity-select-profile]");
	
	if ($el.length === 0) {
		return;
	}
	
	const status = $el.attr("data-action-entity-select-profile");
	
	// Skip if status is "true"
	if (status === "true") {
		return;
	}
	
	// Remove any existing entity select cards
	$("[data-entitySelectCard='true']").remove();
	
	const id = $el.find("[data-profileId]").attr("data-profileId");
	const showInRelationData = $el.attr("data-action-entity-select-showInRelationData");
	
	if (!id) {
		return toastr.error(`هیچ نمایه داده ای یافت نشد .`, 'خطا');
	}
	
	let $tmpl = entityTableTmplProflie();
	
	// Handle close button
	$tmpl.find("[data-action=close]").on("click", function () {
		$tmpl.remove();
	});
	
	// Fetch data table profile
	post("/System/FetchDataTableProfile",
		{ id, showInRelationData },
		function (r) {
			if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');
			
			r.data.forEach(z => {
				if (z.render) {
					z.render = function (data, type, row) { 
						return `<td> <a href='#' class= 'btn btn-bg-light btn-icon btn-primary btn-sm '> <i class='fs-2 ki-duotone ki-plus lh-0'></i> </a> </td>` 
					}
				}
			});
			
			var table = InitDataTabelProfile(
				$tmpl.find("table"),
				r.data,
				id,
				false,
				"/System/ExportToExcelProfile"
			);
			
			// Handle double click on row
			table.on('dblclick', 'tbody tr', function () {
				let data = table.row(this).data();
				let showDataModel = table.context[0].oInit.columns.firstOrDefault(c => c.showInRelationData)?.data ?? "c0";
				
				let showData = "";
				if (showDataModel != null) {
					showData = data[showDataModel]
				}
				else {
					showData = data.id;
				}
				
				if (!showData || showData.length === 0) {
					showData = data.id;
				}
				
				$el.find("input[type=hidden]").val(data.id);
				$el.find("input[readonly]").val(showData);
				$tmpl.remove();
			});
			
			// Handle click on add button
			table.on('click', 'a', function () {
				let data = table.row($(this).closest('tr')).data();
				
				let showDataModel = table.context[0].oInit.columns.firstOrDefault(c => c.showInRelationData)?.data ?? "id";
				
				let showData = "";
				if (showDataModel != null) {
					showData = data[showDataModel]
				}
				else {
					showData = data.id;
				}
				
				if (!showData || showData.length === 0) {
					showData = data.id;
				}
				
				$el.find("input[type=hidden]").val(data.id);
				$el.find("input[readonly]").val(showData);
				$tmpl.remove();
			});
			
			$el.append($tmpl);
		}
	);
}

function entityTableTmpl(model) {
	let $table = $(`
    <div class="card position-absolute   col-md-9" style="right: 10%;z-index: 20;" data-entitySelectCard="true">
		<div class="card-header align-content-center"> 
					<div class="btn btn-icon btn-sm btn-active-light-primary ms-2" data-action='close' aria-label="Close">
						<i class="ki-duotone ki-cross fs-1"><span class="path1"></span><span class="path2"></span></i>
					</div>
				</div>
		<div class="card-body table-responsive">
			<table id="itemsTable" class="table table-row-bordered nowrap table-bordered table-striped table-hover">
					<thead>
						<tr>
							<th>#</th>
						</tr>
					</thead>
					<tbody></tbody>
					<tfoot>
						<tr>
							<th>
								<button id="serachTabel" class="btn btn-primary">جستجو</button>
							</th>
						</tr>
					</tfoot>
				</table>
		</div>
		
	</div>`)


	model.columns.forEach(c => {
		if (c.title) {

			$table.find("thead tr")
				.append(
					`<th>${c.title}</th>`
				);

			$table.find("tfoot tr")
				.append(
					`<th>${c.title}</th>`
				);

		}
	})

	return $table;

}

$.fn.block = function (block = true, withText = false) {

	if (block === false) {
		return this.each(function () {
			$(this).find('.block-overlay').remove();
			$(this).css('pointer-events', 'auto');
		});
	}
	return this.each(function () {
		var $element = $(this);
		var $overlay;
		if (withText)
			$overlay=	$('<div class="block-overlay"><span>لطفا منتظر بمانید... <span style="width:1rem;height:1rem;" class="spinner-border text-primary"></span></span></div>');
		else
			$overlay=	$('<div class="block-overlay"> <span style="width:1rem;height:1rem;" class="spinner-border text-primary"></span></div>');

		// Calculate font size based on the element's size
		var fontSize = Math.min($element.width(), $element.height()) * 0.6; // Adjust the multiplier as needed

		$overlay.css({
			position: 'absolute',
			top: 0,
			left: 0,
			width: '100%',
			height: '100%',
			backgroundColor: 'rgba(0, 0, 0, 0.6)',
			color: '#fff',
			display: 'flex',
			justifyContent: 'center',
			alignItems: 'center',
			zIndex: 1000,
			pointerEvents: 'none', // Prevents clicks on the overlay itself
			fontSize: fontSize + 'px', // Set the calculated font size
			"border-radius": $element.css("border-radius")
		});

		$element.css('position', 'relative').append($overlay);
		$element.css('pointer-events', 'none'); // Prevents clicks on the element
	});
};

function initItemsForms($el) {
 
	$el = $el || $("body");

	$el.find("[data-action='removeItem']").each((i, c) => {
		$(c).click(function (e) {
			 
			e.preventDefault();
			$(this).closest(".entity-item").remove();
		});
	});

	$el.find("[data-action-entity-select]").each((i, c) => {

		var $el = $(c);
		var model = $(c).attr("data-action-entity-select");
		var inited = $(c).attr("data-action-entity-select-init");

		if (inited) {

		}
		else {
			var d = Function(`return ${model}`)();

			$(c).find("[data-action='selectentity']")
				.click(function () {

					$("[data-entitySelectCard='true']").remove();

					let $tmpl = entityTableTmpl(d);

					var table = InitDataTabel($tmpl.find("table"), d.columns, d.entityName, d.path);


					$tmpl.find("[data-action=close]").click(function () {
						$tmpl.remove();
					});


					table.on('dblclick ', 'tbody tr', function () {
						let data = table.row(this).data();
						let showDataModel = d.columns.firstOrDefault(c => c.showInRelationData === true);

						let showData = "";
						if (showDataModel != null) {
							showData = data[showDataModel.data]
						}
						else {
							showData = data.id;
						}

						if (!showData || showData.length === 0) {
							showData = data.id;
						}

						$el.find("input[type=hidden]").val(data.id);

						$el.find("input[readonly]").val(showData)
						$tmpl.remove();

					})

					table.on('click ', 'a', function () {
						let data = table.row(this.parentNode).data();
						let showDataModel = d.columns.firstOrDefault(c => c.showInRelationData === true);

						let showData = "";
						if (showDataModel != null) {
							showData = data[showDataModel.data]
						}
						else {
							showData = data.id;
						}

						if (!showData || showData.length === 0) {
							showData = data.id;
						}

						$el.find("input[type=hidden]").val(data.id);

						$el.find("input[readonly]").val(showData)
						$tmpl.remove();

					})


					$el.append($tmpl)



				});

			$(c).attr("data-action-entity-select-init", true);
		}

	})
	 
	// Initialize entity select profile handlers (will be handled globally via event delegation)
	initEntitySelectProfile($el);

	initPersionDatePicker($el)

	initFileUploaders($el)
}
const initPersionDatePicker = function ($el) {
	 

	$el.find("[data-persionDatePicker=true]").each((c, i) => {
		 
		let objOptionsStr = $(i).attr("data-persionDatePickerOption") ?? $(i).attr("persion-datetimepicker");
		if (objOptionsStr) {

			let objOptions = JSON.parse(objOptionsStr);
			$(i).persianDatepicker(objOptions)
			$(i).attr("data-persionDatePicker", "false");
		}



	})
}



// ============================================
// File Uploader TagHelper JavaScript Functions
// ============================================

/**
 * Initialize file uploader components
 * @param {jQuery} $container - Container element
 */
function initFileUploaders($container) {
    if (!$container) $container = $(document);
 
    $container.find('[data-file-uploader="true"]').each(function() {
        const $el = $(this);
        if ($el.data('dropzone-initialized')) return;

        // Cache DOM elements and data attributes
        const $containerEl = $el.closest('.file-uploader-container');
        const $wrapper = $el.closest('.file-uploader-wrapper');
        const $existingFile = $containerEl.find('.existing-file-item');
        const entityType = $el.data('entity-type');
        const entityPropName = $el.data('entity-prop-name');
        const bindName = $el.data('bindid');
        let fileId = $el.data('file-id');
        
        // Check if file exists: either fileId exists OR existing file element exists (from server render)
        const hasExistingFile = (fileId && fileId !== '') || $existingFile.length > 0;
        
        // Helper function to toggle visibility
        const toggleFileDisplay = function(showFile) {
            if (showFile) {
                // Show existing file, hide dropzone
                if ($wrapper.length) {
                    $wrapper.hide();
                } else {
                    $el.hide();
                }
                $existingFile.show();
            } else {
                // Show dropzone, hide existing file
                if ($wrapper.length) {
                    $wrapper.show();
                } else {
                    $el.show();
                }
                $existingFile.hide();
            }
        };
        
        // Initial display state
        toggleFileDisplay(hasExistingFile);
        
        // If existing file exists but no fileId in data attribute, extract it from the element
        if ($existingFile.length > 0 && (!fileId || fileId === '')) {
            fileId = $existingFile.data('file-id');
            if (fileId) {
                $el.data('file-id', fileId);
            }
        }
         
        const config = {
            url: `/File/upload/${entityType}/${entityPropName}`,
            paramName: 'file',
            maxFiles: parseInt($el.data('max-files') || 1),
            maxFilesize: parseFloat($el.data('max-file-size') || 10),
            acceptedFiles: $el.data('accepted-file-types') || '*/*',
            addRemoveLinks: true,
            dictDefaultMessage: '<i class="fas fa-cloud-upload-alt fs-3x text-primary"><span class="path1"></span><span class="path2"></span></i><p>فایل را اینجا رها کنید یا کلیک کنید</p>',
            dictRemoveFile: 'حذف',
            dictCancelUpload: 'لغو',
            dictUploadCanceled: 'آپلود لغو شد',
            dictInvalidFileType: 'نوع فایل مجاز نیست',
            dictFileTooBig: 'فایل خیلی بزرگ است ({{filesize}}MB). حداکثر: {{maxFilesize}}MB',
            dictMaxFilesExceeded: 'تعداد فایل‌ها بیش از حد مجاز است',
            autoProcessQueue: true,
            parallelUploads: 1,
            createImageThumbnails: false,
            previewTemplate: getDropzonePreviewTemplate(),
            init: function() {
                const dropzone = this;
                const $dropzoneEl = $(dropzone.element);
                 
                // Load existing file if fileId exists and no existing file element is present
                if (fileId && !$existingFile.length) {
                    loadExistingFile(dropzone, fileId, bindName, $el);
                }
                
                // Handle successful upload
                dropzone.on('success', function(file, response) {
					 
                    const data = response?.data || response?.Data || response || null;
                    const uploadedFileId = data?.fileId || data?.id || file?.fileId;
                    const fileName = data?.originalName || file?.name;
                    const fileSize = data?.size || file?.size;
                    
                    if (uploadedFileId) {
                        // Update file ID
                        $el.data('file-id', uploadedFileId);
                        fileId = uploadedFileId;
                        updateBindValue(bindName, uploadedFileId, $containerEl);
                        
                        // Remove old existing file if exists
                        $existingFile.remove();
                        
                        // Create and show new existing file
                        const showDownload = $el.data('show-download') !== false;
                        const showDelete = $el.data('show-delete') !== false;
						const ext = getFileExtension(file.name);
						const iconClass = getFileIconClass(ext);
                        
                        const existingFileHtml = `
                            <div class='existing-file-item' data-file-id='${uploadedFileId}'>
                                <div class='file-preview'>
                                    <i class='${iconClass}'> </i>
                                </div>
                                <div class='file-info'>
                                    <span class='file-name'>${escapeHtml(fileName)}</span>
                                    <span class='file-size'>${formatFileSize(fileSize)}</span>
                                </div>
                                <div class='file-actions'>
                                    ${showDownload ? `<button type='button' class='btn btn-sm btn-icon btn-outline btn-outline-success btn-download-file' data-file-id='${uploadedFileId}' title='دانلود'>
                                        <i class='ki-duotone ki-arrow-down fs-2'>
                                            <span class='path1'></span>
                                            <span class='path2'></span>
                                        </i>
                                    </button>` : ''}
                                    ${showDelete ? `<button type='button' class='btn btn-sm btn-icon  btn-outline btn-outline-danger btn-delete-file' data-file-id='${uploadedFileId}' title='حذف'>
                                        <i class='ki-duotone ki-trash fs-2'>
                                            <span class='path1'></span>
                                            <span class='path2'></span>
                                        </i>
                                    </button>` : ''}
                                </div>
                            </div>`;
                        
                        $containerEl.append(existingFileHtml);
                        initFileDownloadButtons($containerEl);
                        initFileDeleteButtons($containerEl);
                        
                        // Hide dropzone and show existing file
                        toggleFileDisplay(true);
                        
                        // Remove dropzone preview
                        $(file.previewElement).remove();
                    }
                });
                
                // Handle file removal
                dropzone.on('removedfile', function(file) {
                    const removedFileId = file.fileId || fileId;
					 
                    if (removedFileId) {
                        deleteFile(removedFileId);
                        $el.data('file-id', '');
                        fileId = '';
                        updateBindValue(bindName, '', $containerEl);
                        
                        // Remove existing file and show dropzone
                        $existingFile.remove();
                        toggleFileDisplay(false);
                        $dropzoneEl.find('.dz-message').removeClass('d-none');
                    }
                });
                
                // Handle errors
                dropzone.on('error', function(file, errorMessage) {
                    if (file.accepted === false) {
                        error2(errorMessage || 'خطا در بارگذاری فایل');
                        this.removeFile(file);
                    } else if (errorMessage && typeof errorMessage === 'object' && !errorMessage.isSuccess) {
                        error2(errorMessage.message || 'خطا در بارگذاری فایل');
                        this.removeFile(file);
                    }
                });
                
                // Set file icon after file is added
                dropzone.on('addedfile', function(file) {
					  
                    if (!file || !file.name) return;
                    
                    const ext = getFileExtension(file.name);
                    const iconClass = getFileIconClass(ext);
                    const $preview = $(file.previewElement);
                    const $iconContainer = $preview.find('.dz-icon-container');
                    
                    if ($iconContainer.length) {
                        $iconContainer.find('i').removeClass().addClass(iconClass);
                    }
                });
            }
        };
        
        // Initialize Dropzone
        if (typeof Dropzone !== 'undefined') {
            const dropzone = new Dropzone(this, config);
            $el.data('dropzone', dropzone);
            $el.data('dropzone-initialized', true);
        } else {
            console.warn('Dropzone library not loaded');
        }
    });
    
    // Initialize download and delete buttons
    initFileDownloadButtons($container);
    initFileDeleteButtons($container);
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Get Dropzone preview template
 */
function getDropzonePreviewTemplate() {
    return `
        <div class="dz-preview dz-file-preview">
            <div class="dz-icon-container">
                <i class="ki-duotone ki-file fs-2x text-primary" data-dz-icon>
                    <span class="path1"></span>
                    <span class="path2"></span>
                </i>
            </div>
            <div class="dz-details">
                <div class="dz-filename">
                    <span data-dz-name></span>
                </div>
                <div class="dz-size">
                    <span data-dz-size></span>
                </div>
            </div>
            <div class="dz-progress">
                <span class="dz-upload" data-dz-uploadprogress></span>
            </div>
            <div class="dz-error-message">
                <span data-dz-errormessage></span>
            </div>
            <div class='file-actions'>
				<button type='button' class='btn btn-sm btn-icon btn-light btn-download-file' data-file-id='{fileIdForHtml}' title='دانلود'>
					<i class='ki-duotone ki-arrow-down fs-5'>
						<span class='path1'></span>
						<span class='path2'></span>
					</i>
				</button>
				<button type='button' class='btn btn-sm btn-icon btn-light btn-delete-file' data-file-id='{fileIdForHtml}' title='حذف'>
					<i class='ki-duotone ki-trash fs-5'>
						<span class='path1'></span>
						<span class='path2'></span>
					</i>
				</button>
			</div>
        </div>
    `;
}

/**
 * Load existing file into dropzone
 * Note: This function is called when dropzone is initialized with an existing fileId
 * But if file already exists (from server), we should hide dropzone and show existing file item instead
 */
function loadExistingFile(dropzone, fileId, bindName, $el) {
    if (!fileId) return;
    
    // Cache DOM elements
    const $containerEl = $el.closest('.file-uploader-container');
    const $existingFile = $containerEl.find('.existing-file-item');
    const $wrapper = $el.closest('.file-uploader-wrapper');
    const $dropzoneEl = $(dropzone.element);
    
    // If existing file item exists (from server render), hide dropzone and show it
    if ($existingFile.length > 0) {
        if ($wrapper.length) {
            $wrapper.hide();
        } else {
            $el.hide();
        }
        $existingFile.show();
        return; // Don't load into dropzone
    }
    
    $.get(`/File/file/${fileId}`)
        .done(function(response) {
            const data = response.data || response.Data || response;
            if (data && data.id) {
                const mockFile = {
                    name: data.originalName || 'فایل',
                    size: data.size || 0,
                    fileId: data.id,
                    filePath: data.physicalPath
                };
                 
                // dropzone.displayExistingFile(mockFile, data.physicalPath);

				dropzone.emit("success", mockFile);
                updateBindValue(bindName, data.id, $containerEl);
                 
                // Customize icon and styling
                const ext = getFileExtension(mockFile.name || '');
                const iconClass = getFileIconClass(ext);
                
            }
        })
        .fail(function() {
            console.warn('Failed to load file:', fileId);
        });
}

/**
 * Initialize download buttons
 */
function initFileDownloadButtons($container) {
    if (!$container) $container = $(document);
    
    $container.find('.btn-download-file').off('click').on('click', function() {
        const fileId = $(this).data('file-id');
	    if (fileId) {
 
            downloadFile(fileId);
        }
    });
}

/**
 * Initialize delete buttons
 */
function initFileDeleteButtons($container) {
    if (!$container) $container = $(document);
    
    $container.find('.btn-delete-file').off('click.fileDelete').on('click.fileDelete', function() {
        const $btn = $(this);
        const fileId = $btn.data('file-id');
        if (!fileId) return;
        
        const $existingFileItem = $btn.closest('.existing-file-item');
        const $containerEl = $btn.closest('.file-uploader-container');
        const $dropzone = $containerEl.find('[data-file-uploader="true"]');
        const $wrapper = $dropzone.closest('.file-uploader-wrapper');
        const bindName = $dropzone.data('bindid');
        
        Swal.fire({
            title: 'آیا مطمئن هستید؟',
            text: 'این فایل حذف خواهد شد',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'بله، حذف کن',
            cancelButtonText: 'لغو',
            confirmButtonColor: '#d33'
        }).then((result) => {
            if (result.isConfirmed) {
                deleteFile(fileId, function() {
                    // Remove existing file item
                    $existingFileItem.remove();
                    
                    // Clear file ID from dropzone
                    $dropzone.data('file-id', '');
                    
                    // Clear bind value
                    if (bindName) {
                        updateBindValue(bindName, '', $containerEl);
                    }
                    
                    // Show dropzone again
                    if ($wrapper.length) {
                        $wrapper.show();
                    } else {
                        $dropzone.show();
                    }
                     
                    // Show dropzone message
                    $dropzone.find('.dz-message').removeClass('d-none');
                    $dropzone.css("display","block")
                    // Clear dropzone files if any
                    const dropzoneInstance = $dropzone.data('dropzone');
                    if (dropzoneInstance && dropzoneInstance.files.length > 0) {
                        dropzoneInstance.removeAllFiles(true);
                    }
                });
            }
        });
    });
}

/**
 * Format file size helper
 */
function formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '0 B';
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Download file by ID
 */
function downloadFile(fileId) {
    if (!fileId) return;
    
    const url = `/File/download/${fileId}`;
    const link = document.createElement('a');
    link.href = url;
    link.download = '';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Delete file by ID
 */
function deleteFile(fileId, callback) {
    if (!fileId) return;
    
    $.ajax({
        url: `/File/file/${fileId}`,
        type: 'DELETE',
        success: function() {
            if (callback) callback();
            success2('فایل با موفقیت حذف شد');
        },
        error: function() {
            error2('خطا در حذف فایل');
        }
    });
}

/**
 * Update file owner
 */
function updateFileOwner(fileId, entityType  , entityPropName) {
	if (!fileId || !entityType  ) return;
    
    $.post('/File/AddDataToFile', {
        fileId: fileId,
	    entityType: entityType,
	    entityId: entityId,
	    entityPropName: entityPropName
    }).fail(function() {
        console.warn('Failed to update file owner');
    });
}

/**
 * Update bind value
 */
function updateBindValue(bindName, value , $parentEl) {
    if (!bindName) return;
    
	const $bindEl = $parentEl.find(`[data-bind="${bindName}"]`);
    if ($bindEl.length) {
        $bindEl.val(value);
    }
}

/**
 * Get file extension from file name
 * @param {string} fileName - The file name
 * @returns {string} The file extension (without dot) or empty string
 */
function getFileExtension(fileName) {
    if (!fileName || typeof fileName !== 'string') return '';
    
    // Trim whitespace
    fileName = fileName.trim();
    if (!fileName) return '';
    
    // Handle hidden files (starting with dot) - return empty for .hidden files
    if (fileName.startsWith('.') && fileName.indexOf('.', 1) === -1) {
        return '';
    }
    
    // Split by dot and get last part
    const parts = fileName.split('.');
    
    // If no extension (only one part or last part is empty), return empty
    if (parts.length < 2 || !parts[parts.length - 1]) {
        return '';
    }
    
    // Return last part (extension) in lowercase
    return parts[parts.length - 1].toLowerCase();
}

/**
 * Get file icon class based on extension
 */
function getFileIconClass(ext) {
    const iconMap = {
	    'pdf': 'fas fa-file-pdf fs-2x text-danger',
	    'doc': 'fas fa-file-word fs-2x text-primary',
	    'docx': 'fas fa-file-word fs-2x text-primary',
	    'xls': 'fas fa-file-excel fs-2x text-success',
	    'xlsx': 'fas fa-file-excel fs-2x text-success',
	    'ppt': 'fas fa-file-prescription fs-2x text-warning',
	    'pptx': 'fas fa-file-prescription fs-2x text-warning',
	    'zip': 'fas fa-file-archive text-info',
	    'rar': 'fas fa-file-archive fs-2x text-info',
	    '7z': 'fas fa-file-archive fs-2x text-info',
        'jpg': 'ki-duotone ki-picture fs-2x text-primary',
        'jpeg': 'ki-duotone ki-picture fs-2x text-primary',
        'png': 'ki-duotone ki-picture fs-2x text-primary',
        'gif': 'ki-duotone ki-picture fs-2x text-primary',
	    'txt': 'fas fa-file fs-2x text-muted',
	    'csv': 'fas fa-file-csv fs-2x text-info',
	    'xml': 'fas fa-file fs-2x text-info',
	    'html': 'fas fa-file fs-2x text-warning',
	    'mp3': 'fas fa-file fs-2x text-primary',
	    'mp4': 'fas fa-file fs-2x text-primary',
	    'avi': 'fas fa-file fs-2x text-primary'
    };
    
    return iconMap[ext] || 'ki-duotone ki-file fs-2x text-primary';
}

/**
 * Get file icon path based on extension (legacy support)
 */
function getFileIconPath(ext) {
    const iconMap = {
        'pdf': '/lib/webimg/filelogo/pdf.jpg',
        'doc': '/lib/webimg/filelogo/word.jpg',
        'docx': '/lib/webimg/filelogo/word.jpg',
        'xls': '/lib/webimg/filelogo/excel.jpg',
        'xlsx': '/lib/webimg/filelogo/excel.jpg',
        'zip': '/lib/webimg/filelogo/zip.jpg',
        'rar': '/lib/webimg/filelogo/rar.jpg',
        '7z': '/lib/webimg/filelogo/rar.jpg'
    };
    
    return iconMap[ext] || '/lib/webimg/filelogo/file.jpg';
}

/**
 * Format file size
 */
function formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '0 B';
    
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

// Add CSS styles for file uploader
 
 
$(document).ready(function () {


	// Initialize entity select profile with event delegation for dynamic elements
	initEntitySelectProfile();

	// Set up global event delegation for entity select profile buttons
	// This will work for both existing and dynamically added elements
	$(document).on('click', '[data-action-profile="selectentity"]', handleEntitySelectProfileClick);

	initItemsForms();




	if (window.Inputmask) {
		Inputmask.extendDefaults({
			onKeyValidation: function (key, result) {


				if (!result) {
					let message = $(this).attr("data-invalidMessage");

					if (!message) {
						message = 'کاراکتر وارد شده غیر مجاز میباشد.';
					}

					if ($(this).parent().find("[data-invalidmessagespan]").length === 0) {
						$(this).after(`<div data-invalidmessagespan class='my-1 mx-1'> <span class='text-danger' >${message}</span> </div>`)
					}
					else {
						$(this).parent().find("[data-invalidmessagespan] span").html(message);
					}
				}
				else {
					$(this).parent().find("[data-invalidmessagespan] span").html("")
				}

			}
		});

		$("[data-inputmask]").inputmask()

	}

	initPersionDatePicker($("body"))


	$("input[focus]").on({
		keypress: function (evt) {

			if (evt.keyCode === 13) {
				let focusId = $(this).attr("focus");
				if ($(focusId).length > 0) {
					if ($(focusId).is("button")) {
						$(focusId).click();
					}
					if ($(focusId).is("input")) {
						$(focusId).focus();
					}
				}

			}
		},

	});


	window.onkeyup = function (e) {
		var event = e.which || e.keyCode || 0; // .which with fallback

		if (event == 27) { // ESC Key
			history.back() // Navigate to URL
		}
	}

	$("body")
		.on("click", "[data-system-action=history]", function () {
			var id = $(this).attr("data-system-action-id");
			var type = $(this).attr("data-system-history-type")
			if (!id || id.length == 0) {

				if ($(this).closest("[data-sys=system-tab]").find("[data-bind=id]").length > 0) {
					id = $(this).closest("[data-sys=system-tab]").find("[data-bind=id]").val();

					if (!id || id.length == 0)
						return toastr.error("تاریخچه ای برای این موجود وجود ندارد", 'خطا');
				}
				else {
					return toastr.error("تاریخچه ای برای این موجود وجود ندارد", 'خطا');
				}

			}
			if (!type || type.length == 0) {
				return toastr.error("نوع موجودیت مشخص نشده است", 'خطا');
			}
			let $btn = $(this).block()
			post("/System/GetHistory", { id, type }, function (r) {
				$btn.block(false);
				if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');

				renderHistoryTmpl(r.data)
			})
		});

	window.appController = new AppController(
		$("#pagesContainer"),
		$("#tabsContainer"),
		$("#loader"),
		window.location.pathname + window.location.search
	);


	$(document).on("click", "a", function (e) {

		const $a = $(this);
		const href = $a.attr("href");
		if (!href || href.startsWith("javascript:") || href.startsWith("#") || $a.closest("#pageMenuBuilder").length > 0) return;

		if (!isSameOrigin(href)) return;

		if (href.startsWith("/File/download")) {

			return;
		}

		if (e.ctrlKey || e.metaKey || e.shiftKey || $a.attr("target") === "_blank") return;

		e.preventDefault();
		if (href == "/Authenticate/Logout") {
			get(href, function (r) {
				window.location.assign("/Authenticate/Login")
			})
		}
		else {
			appController.addPage(href);

		}


	});


	$("#notificationSection").on("click", "[data-action=readNotification]", function () {

		let notificationId = $(this).attr("data-id");
		let $element = $(this).closest("[data-row=notification]");

		markAsReadHeader(notificationId, $element);
	})
 
	initFileUploaders();

	$("[data-action=changeMenu]").change(function () {
		const id = $(this).val();
		var path = "/System/MenuBuilder/GetMenuById?id=";
		if (id != 'null') {
			path += id;
		}
		post(path, null, function (r) {
			if (!r.isSuccess) return error2(r.message);
			 
			$("#kt_app_sidebar_menu").html(r.data)
		});
	});


	if (window.location.pathname.toLocaleLowerCase() == '/authenticate/login') {
 
		$('#login').click(function () {
			 
			if (validateError($('#form'))) { return; }

			var model = $('#form').dataBind();
			const $btn = $(this).block();

			post('/Authenticate/Login',
				model,
				function (r) {
					$btn.block(false);
					if (!r.isSuccess) return toastr.error(`${r.message}`, 'خطا');
					location.assign("/");

				});
		});
	}
});

 
$(document).on('DOMNodeInserted', function(e) {
    const $target = $(e.target);
    if ($target.find('[data-file-uploader="true"]').length) {
        initFileUploaders($target);
    }
});

 
if (typeof initItemsForms === 'function') {
    const originalInitItemsForms = initItemsForms;
    initItemsForms = function($el) {
        originalInitItemsForms($el);
        initFileUploaders($el);
    };
}

(function ($) {
	class EntitySelector {
		constructor(element, options) {
			 
			this.$container = $(element);
			if (this.$container.data('entitySelectorInstance')) return;
			this.$container.data('entitySelectorInstance', this);

			this.options = $.extend({}, EntitySelector.DEFAULTS, this.$container.data(), options);
			this.isMulti = this.options.multiSelect === true;

			// Basic Elements
			this.$input = this.$container.find('.entity-selector-input');
			this.$hidden = this.$container.find('.entity-selector-value');
			this.$hiddenName = this.$container.find('.entity-selector-text');
			this.$results = this.$container.find('.entity-selector-results');

			// --- FIX 1: Move Dropdown to Body to avoid Overflow Issues ---
			this.$results.appendTo('body');
			// ------------------------------------------------------------

			this.$arrow = this.$container.find('.entity-arrow');
			this.$clearBtn = this.$container.find('.entity-clear');

			// --- FIX 2: Style Adjustments ---
			this.$container.css({
				'width': '100%',
				'display': 'block'
			});
			// Ensure input takes full width if it's not multi-select (multi-select handles its own container)
			if (!this.isMulti) {
				this.$input.css('width', '100%');
				this.$container.find('.input-group').css('width', '100%');
			}
			// --------------------------------

			// --- TRANSFORM UI FOR MULTI-SELECT ---
			if (this.isMulti) {
				this.setupMultiSelectUI();
			}

			// State
			this.page = 1;
			this.total = 0;
			this.isLoading = false;
			this.selectedItems = [];
			this.debounceTimer = null;
			this.lastId = 0;
			this.lastQuery = null; // Track this to prevent double firing
			this.hasMore = true;
			this.init();
		}

		setupMultiSelectUI() {

			// چک میکنیم اگر سرور رندر کرده باشد
			if (this.$container.find('.entity-selector-multi-container').length > 0) {
				this.$multiContainer = this.$container.find('.entity-selector-multi-container');
				// اینپوت قبلا دیتچ شده و داخل این قرار گرفته
				this.$input = this.$multiContainer.find('.entity-selector-multi-input');
				// آیکون‌ها هم داخلش هستند، فقط باید سلکت کنیم
				this.$arrow = this.$multiContainer.find('.entity-arrow');
				this.$clearBtn = this.$multiContainer.find('.entity-clear');

				this.bindMultiEvents();
				return; // تمام
			}

			// اگر سرور رندر نکرده بود (حالت خالی)، خودمان میسازیم (کدهای قبلی)
			this.$multiContainer = $('<div class="entity-selector-multi-container"></div>');
			this.$multiContainer = $('<div class="entity-selector-multi-container"></div>');
			this.$container.find('.input-group').hide().before(this.$multiContainer);
			this.$input.detach().appendTo(this.$multiContainer);
			this.$input.addClass('entity-selector-multi-input').removeClass('form-control');
			this.$input.on('focus', () => this.$multiContainer.addClass('focused'));
			this.$input.on('blur', () => this.$multiContainer.removeClass('focused'));

			this.$container.find('.entity-icons').detach().appendTo(this.$multiContainer);

			this.$multiContainer.on('click', (e) => {
				if (e.target === this.$multiContainer[0]) {
					this.$input.focus();
					// --- CHANGE 1: Ensure click on gray area opens dropdown ---
					if (!this.$results.hasClass('show')) {
						this.search(this.$input.val());
					}
				}
			});

			this.$multiContainer.on('click', (e) => {
 				if (e.target === this.$multiContainer[0] || e.target === this.$input[0]) {
					this.$input.focus();
					if (!this.$results.hasClass('show')) this.search(this.$input.val());
				}
			});
		}

		init() {
			this.$input.on('input', () => {
				this.onInput();
				this.updateIcons();  
			});
			this.$input.on('focus click', () => {
				if (!this.$results.hasClass('show')) this.search(this.$input.val());
			});

			// --- CHANGE 2: Open on Focus/Click (even if empty) ---
			this.$input.on('focus click', () => {
				// Only open if not already open
				if (!this.$results.hasClass('show')) {
					// Pass current value (empty string = load all)
					this.search(this.$input.val());
				}
			});

			this.$clearBtn.on('click', (e) => {
				
				e.stopPropagation();  
				this.clearAll();
				this.$input.focus();
			});

			this.$input.on('keydown', (e) => this.onKeydown(e));

			$(document).on('click', (e) => {
				const target = e.target;
				const inContainer = this.$container.is(target) || this.$container.has(target).length > 0;

				if (!inContainer) {
					this.close();
				}
			});

			this.$results.on('scroll', () => {
				if (this.$results.scrollTop() + this.$results.innerHeight() >= this.$results[0].scrollHeight - 10) {
					// FIX: Use hasMore flag instead of total/page math
					if (!this.isLoading && this.hasMore) {
						this.nextPage();
					}
				}
			});

			// --- FIX: Handle scroll/resize to update dropdown position ---
			$(window).on('scroll resize', () => {
				if (this.$results.hasClass('show')) {
					this.updateDropdownPosition();
				}
			});
			
			// Handle modal scroll if inside a modal
			this.$container.closest('.modal').on('scroll', () => {
				if (this.$results.hasClass('show')) {
					this.updateDropdownPosition();
				}
			});
			// ------------------------------------------------------------

			const serverInitData = this.$container.attr('data-initial-items');

			if (serverInitData) {
				try {
					const items = JSON.parse(serverInitData);
					if (items && items.length > 0) {
						this.selectedItems = items;
						 
 						if (this.isMulti && this.$container.find('.entity-selector-multi-container').length > 0) {
							 
							this.$multiContainer = this.$container.find('.entity-selector-multi-container');
							this.$input = this.$multiContainer.find('.entity-selector-multi-input');

						 
							this.bindMultiEvents();
						}
						this.renderChips();
						this.updateIcons();
					}
				} catch (e) { console.error("Error parsing initial items", e); }
			} else {
			 
				const initIds = this.$hidden.val();
				if (initIds) this.loadInitial(initIds);
			}
	 

			this.updateIcons();
		}
		bindMultiEvents() {
			this.$input.on('input', () => { this.onInput(); this.updateIcons(); });
			this.$input.on('focus click', () => {
				if (!this.$results.hasClass('show')) this.search(this.$input.val());
			});
			this.$input.on('keydown', (e) => this.onKeydown(e));
			this.$input.on('focus', () => this.$multiContainer.addClass('focused'));
			this.$input.on('blur', () => this.$multiContainer.removeClass('focused'));

			this.$multiContainer.on('click', (e) => {
				if (e.target === this.$multiContainer[0] || e.target === this.$input[0]) {
					this.$input.focus();
					if (!this.$results.hasClass('show')) this.search(this.$input.val());
				}
			});
		}

		updateIcons() {
			 
			const hasText = this.$input.val().length > 0;
			const hasSelection = this.selectedItems.length > 0;
 
			if (hasText || hasSelection) {
				this.$arrow.hide();
				this.$clearBtn.show();
			} else {
				this.$arrow.show();
				this.$clearBtn.hide();
			}
		}

		onInput() {
			clearTimeout(this.debounceTimer);
			const val = this.$input.val(); // Do not trim immediately to allow space

			// --- CHANGE 3: Removed "if (!val) close()" logic ---
			// Now we search even if empty (to show all results again)

			this.page = 1;
			this.debounceTimer = setTimeout(() => this.search(val), 300);
		}

		updateDropdownPosition() {
			const offset = this.$container.offset();
			const height = this.$container.outerHeight();
			const width = this.$container.outerWidth();

			this.$results.css({
				'position': 'absolute',
				'top': offset.top + height + 'px',
				'left': offset.left + 'px',
				'width': width + 'px',
				'z-index': 1019100000,
				'display': 'block' // Ensure it's visible for calculations, 'show' class handles opacity/visibility usually
			});
		}

		search(query, append = false) {
			// === FIX: RESET STATE IF NOT APPENDING ===
			if (!append) {
				this.page = 1;
				this.lastId = 0;
				this.$results.scrollTop(0);
				this.hasMore = true;
			}
			// =========================================

			// Prevent duplicate searches (Optimization)
			if (this.lastQuery === query && this.$results.hasClass('show') && !append && this.page === 1) {
				return;
			}
			if (this.isMulti == false && this.selectedItems.length == 1 && this.selectedItems[0].display === query) {
				return;
			}
			 

			this.isLoading = true;
			this.lastQuery = query;

			const requestData = {
				q: query,
				page: this.page,
				pageSize: this.options.pageSize,
				lastId: this.lastId,
				queryId: this.options.queryId,
				defaultParams: this.$container.attr('data-default-params')

				 
			};

			if (this.isMulti) {
		 
				// this.$results.css('top', this.$multiContainer.outerHeight() + 'px'); // Removed: Position is handled by updateDropdownPosition
				this.updateDropdownPosition(); // Update position for multi-select too
			}

			if (!append) {
				this.$results.empty().addClass('show').html('<div class="dropdown-item text-muted">Loading...</div>');
				this.updateDropdownPosition();
			}

			$.ajax({
				url: `${this.options.apiUrl}/${this.options.entity}`,
				method: 'GET',
				data: requestData,
				success: (res) => {
					this.total = res.total;
					this.renderResults(res.items, append);
				},
				error: () => this.$results.html('<div class="dropdown-item text-danger">Error</div>'),
				complete: () => this.isLoading = false
			});
		}
		renderResults(items, append) {
			if (!append) this.$results.empty();

			if (items.length < this.options.pageSize) {
				this.hasMore = false;
			} else {
				this.hasMore = true;
			}

			if (items.length === 0 && this.page === 1) {
				this.$results.html('<div class="dropdown-item text-muted">هیچ مقداری یافت نشد</div>');
				return;
			}
			if (items.length > 0) {
				// Get the ID of the last item in the list for the next query
				const lastItem = items[items.length - 1];
				this.lastId = lastItem.id;
			}


			items.forEach(item => {
				const isSelected = this.selectedItems.some(x => x.id == item.id);
				const activeClass = isSelected ? 'active' : '';

				const $row = $(`<a href="#" class="dropdown-item ${activeClass}" data-id="${item.id}">${item.display}</a>`);

				$row.on('click', (e) => {
					e.preventDefault();
					this.select(item);
				});
				this.$results.append($row);
			});
		}

		select(item) {
			if (this.isMulti) {
				const index = this.selectedItems.findIndex(x => x.id == item.id);
				const $row = this.$results.find(`.dropdown-item[data-id="${item.id}"]`);

				if (index >= 0) {
					// Item exists: Remove it (Toggle OFF)
					this.selectedItems.splice(index, 1);
					$row.removeClass('active');
				} else {
					// Item new: Add it (Toggle ON)
					this.selectedItems.push(item);
					$row.addClass('active');
				}

				this.renderChips();
				this.updateHidden();
				this.$input.focus();
				this.updateIcons();

			} else {
				// Single Select
				this.selectedItems = [item];
				this.$input.val(item.display);
				this.updateHidden();
				this.updateIcons();
				this.close();
			}
		}

		renderChips() {
			if (!this.isMulti) return;
			this.$multiContainer.find('.entity-chip').remove();
			this.selectedItems.forEach(item => {
				const $chip = $(`
                    <span class="entity-chip badge bg-primary">
                        ${item.display}
                        <span class="remove-chip" title="حدف">&times;</span>
                    </span>
                `);

				$chip.find('.remove-chip').on('click', (e) => {
					e.stopPropagation();
					this.removeItem(item.id);
				});

				this.$input.before($chip);
			});
			// this.$results.css('top', this.$multiContainer.outerHeight() + 'px'); // Removed: Position is handled by updateDropdownPosition
			if (this.$results.hasClass('show')) {
				this.updateDropdownPosition();
			}
		}

		clearAll() {
			this.selectedItems = [];
			if (this.isMulti) this.renderChips();
			else this.$input.val('');

			this.updateHidden();
			this.close(); 

			this.updateIcons(); 
		}

		removeItem(id) {
			this.selectedItems = this.selectedItems.filter(x => x.id != id);
			this.renderChips();
			this.updateHidden();
			this.updateIcons();
		}

		updateHidden() {
			const ids = this.selectedItems.map(x => x.id).join(',');
			this.$hidden.val(ids).trigger('change');

			if (this.$hiddenName.length > 0) {

				const names = this.selectedItems.map(x => x.display).join(',');
				this.$hiddenName.val(names).trigger('change');
			}
		}

		loadInitial(ids) {
			$.ajax({
				url: `${this.options.apiUrl}/get-ids`,
				method: 'GET',
				data: {
					ids: ids,
					queryId: this.options.queryId,
					defaultParams: this.$container.attr('data-default-params'),
					displayTemplate: this.options.template
				},
				success: (items) => {
					if (items && items.length > 0) {
						this.selectedItems = items;
						if (this.isMulti) {
							this.renderChips();
						} else {
							this.$input.val(items[0].display);
						}
						this.updateHidden();
						this.updateIcons(); 
					}
				}
			});
		}

		nextPage() {
			this.page++;
			this.search(this.lastQuery, true);
		}

		close() {
			this.$results.removeClass('show');
			this.$results.css('display', 'none'); // Ensure it's hidden
			// Optional: Reset state on close so next open is definitely fresh
			this.page = 1;
			this.lastQuery = null;
		}

		onKeydown(e) {
			if (e.key === 'Backspace' && this.isMulti && this.$input.val() === '' && this.selectedItems.length > 0) {
				this.removeItem(this.selectedItems[this.selectedItems.length - 1].id);
			}
		}
	}

	EntitySelector.DEFAULTS = { pageSize: 20, apiUrl: '/api/entity-selector' };

	$.fn.entitySelector = function (option) {
		return this.each(function () {
			const $this = $(this);
			let data = $this.data('entitySelector');
			if (!data) {
				data = new EntitySelector(this, typeof option === 'object' && option);
				$this.data('entitySelector', data);
			}
		});
	};

	$(function () {
		$(".entity-selector-wrapper").entitySelector();
	});

})(jQuery);


const observer = new MutationObserver(function(mutations) {
    for (let i = 0; i < mutations.length; i++) {
        const addedNodes = mutations[i].addedNodes;
        for (let j = 0; j < addedNodes.length; j++) {
            const node = addedNodes[j];
            // Only check Element nodes
            if (node.nodeType !== 1) continue;

            // Check if the node itself is the target
            if (node.classList && node.classList.contains('entity-selector-wrapper')) {
                 $(node).entitySelector();
                 continue;
            }

            // Check children using native API for better performance
            if (node.querySelectorAll) {
                const selectors = node.querySelectorAll('.entity-selector-wrapper');
                if (selectors.length > 0) {
                    $(selectors).entitySelector();
                }
            }
        }
    }
});

observer.observe(document.body, { childList: true, subtree: true });

// Sidebar Menu Search Functionality
(function() {
    'use strict';

    const SidebarMenuSearch = {
        searchInput: null,
        menuContainer: null,
        menuItems: [],
        debounceTimer: null,
        changeMenuSelect: null,
        menuObserver: null,

        init: function() {
            this.searchInput = document.getElementById('sidebar_menu_search');
            this.menuContainer = document.getElementById('kt_app_sidebar_menu');
            this.changeMenuSelect = document.querySelector('[data-action="changeMenu"]');

            if (!this.searchInput || !this.menuContainer) return;

            this.cacheMenuItems();
            this.bindEvents();
            this.observeMenuChanges();
        },

        cacheMenuItems: function() {
            if (!this.menuContainer) return;
            this.menuItems = Array.from(this.menuContainer.querySelectorAll('.menu-item'));
        },

        bindEvents: function() {
            const self = this;

            this.searchInput.addEventListener('input', function() {
                clearTimeout(self.debounceTimer);
                self.debounceTimer = setTimeout(function() {
                    self.filterMenu(self.searchInput.value.trim().toLowerCase());
                }, 150);
            });

            this.searchInput.addEventListener('keydown', function(e) {
                if (e.key === 'Escape') {
                    self.searchInput.value = '';
                    self.filterMenu('');
                    self.searchInput.blur();
                }
            });

            // Listen for changeMenu select changes
            if (this.changeMenuSelect) {
                this.changeMenuSelect.addEventListener('change', function() {
                    // Wait for menu to update then refresh cache
                    setTimeout(function() {
                        self.refresh();
                    }, 300);
                });
            }
        },

        observeMenuChanges: function() {
            const self = this;
            
            // Observe menu container for dynamic changes
            if (this.menuObserver) {
                this.menuObserver.disconnect();
            }

            this.menuObserver = new MutationObserver(function(mutations) {
                let shouldRefresh = false;
                for (let i = 0; i < mutations.length; i++) {
                    if (mutations[i].type === 'childList' && 
                        (mutations[i].addedNodes.length > 0 || mutations[i].removedNodes.length > 0)) {
                        shouldRefresh = true;
                        break;
                    }
                }
                if (shouldRefresh) {
                    clearTimeout(self.debounceTimer);
                    self.debounceTimer = setTimeout(function() {
                        self.cacheMenuItems();
                        // Re-apply filter if there's a search term
                        if (self.searchInput && self.searchInput.value.trim()) {
                            self.filterMenu(self.searchInput.value.trim().toLowerCase());
                        }
                    }, 100);
                }
            });

            this.menuObserver.observe(this.menuContainer, { 
                childList: true, 
                subtree: true 
            });
        },

        filterMenu: function(searchTerm) {
            if (!searchTerm) {
                this.showAllItems();
                this.collapseAllSubmenus();
                return;
            }

            const matchedItems = new Set();
            const parentMatchedItems = new Set(); // Items that matched directly (not just as parents)
            const self = this;

            // First pass: find items that match the search term
            this.menuItems.forEach(function(item) {
                const titleElement = item.querySelector(':scope > .menu-link .menu-title, :scope > .menu-content .menu-section .menu-title');
                if (!titleElement) return;

                const title = titleElement.textContent.trim().toLowerCase();

                if (title.includes(searchTerm)) {
                    matchedItems.add(item);
                    parentMatchedItems.add(item); // Mark as directly matched
                    self.addParentItems(item, matchedItems);
                    // Also add all children of matched parent
                    self.addChildItems(item, matchedItems);
                }
            });

            // Second pass: show/hide items
            this.menuItems.forEach(function(item) {
                if (matchedItems.has(item)) {
                    item.style.display = '';
                    
                    // Only highlight if this item directly matched (not just a child of matched parent)
                    if (parentMatchedItems.has(item)) {
                        self.highlightMatch(item, searchTerm);
                    } else {
                        self.removeHighlight(item);
                    }
                    
                    // Expand submenus for matched parents
                    const subMenu = item.querySelector(':scope > .menu-sub');
                    if (subMenu && parentMatchedItems.has(item)) {
                        item.classList.add('show', 'hover');
                        subMenu.style.display = 'flex';
                    }
                } else {
                    item.style.display = 'none';
                    self.removeHighlight(item);
                }
            });
        },

        addParentItems: function(item, matchedItems) {
            let parent = item.parentElement;
            while (parent && parent !== this.menuContainer) {
                if (parent.classList && parent.classList.contains('menu-item')) {
                    matchedItems.add(parent);
                }
                parent = parent.parentElement;
            }
        },

        addChildItems: function(item, matchedItems) {
            // Add all child menu items of a matched parent
            const childItems = item.querySelectorAll('.menu-item');
            childItems.forEach(function(child) {
                matchedItems.add(child);
            });
        },

        highlightMatch: function(item, searchTerm) {
            const titleElement = item.querySelector(':scope > .menu-link .menu-title, :scope > .menu-content .menu-section .menu-title');
            if (!titleElement) return;

            const originalText = titleElement.getAttribute('data-original-text') || titleElement.textContent;
            titleElement.setAttribute('data-original-text', originalText);

            const regex = new RegExp('(' + this.escapeRegex(searchTerm) + ')', 'gi');
            titleElement.innerHTML = originalText.replace(regex, '<mark class="bg-warning text-dark px-0">$1</mark>');
        },

        removeHighlight: function(item) {
            const titleElement = item.querySelector(':scope > .menu-link .menu-title, :scope > .menu-content .menu-section .menu-title');
            if (!titleElement) return;

            const originalText = titleElement.getAttribute('data-original-text');
            if (originalText) {
                titleElement.textContent = originalText;
                titleElement.removeAttribute('data-original-text');
            }
        },

        showAllItems: function() {
            const self = this;
            this.menuItems.forEach(function(item) {
                item.style.display = '';
                self.removeHighlight(item);
                
                // Reset submenu display
                const subMenu = item.querySelector(':scope > .menu-sub');
                if (subMenu) {
                    subMenu.style.display = '';
                }
            });
        },

        collapseAllSubmenus: function() {
            this.menuItems.forEach(function(item) {
                item.classList.remove('hover');
                const subMenu = item.querySelector(':scope > .menu-sub');
                if (subMenu && !item.classList.contains('here')) {
                    item.classList.remove('show');
                    subMenu.style.display = '';
                }
            });
        },

        escapeRegex: function(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },

        refresh: function() {
            this.cacheMenuItems();
            if (this.searchInput) {
                const searchTerm = this.searchInput.value.trim().toLowerCase();
                if (searchTerm) {
                    this.filterMenu(searchTerm);
                } else {
                    this.showAllItems();
                }
            }
        },

        clearSearch: function() {
            if (this.searchInput) {
                this.searchInput.value = '';
                this.filterMenu('');
            }
        },

        destroy: function() {
            if (this.menuObserver) {
                this.menuObserver.disconnect();
                this.menuObserver = null;
            }
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            SidebarMenuSearch.init();
        });
    } else {
        SidebarMenuSearch.init();
    }

    // Expose for external use
    window.SidebarMenuSearch = SidebarMenuSearch;
})();



