/*! jQuery v3.7.1 | (c) OpenJS Foundation and other contributors | jquery.org/license */
!function (e, t) { "use strict"; "object" == typeof module && "object" == typeof module.exports ? module.exports = e.document ? t(e, !0) : function (e) { if (!e.document) throw new Error("jQuery requires a window with a document"); return t(e) } : t(e) }("undefined" != typeof window ? window : this, function (ie, e) { "use strict"; var oe = [], r = Object.getPrototypeOf, ae = oe.slice, g = oe.flat ? function (e) { return oe.flat.call(e) } : function (e) { return oe.concat.apply([], e) }, s = oe.push, se = oe.indexOf, n = {}, i = n.toString, ue = n.hasOwnProperty, o = ue.toString, a = o.call(Object), le = {}, v = function (e) { return "function" == typeof e && "number" != typeof e.nodeType && "function" != typeof e.item }, y = function (e) { return null != e && e === e.window }, C = ie.document, u = { type: !0, src: !0, nonce: !0, noModule: !0 }; function m(e, t, n) { var r, i, o = (n = n || C).createElement("script"); if (o.text = e, t) for (r in u) (i = t[r] || t.getAttribute && t.getAttribute(r)) && o.setAttribute(r, i); n.head.appendChild(o).parentNode.removeChild(o) } function x(e) { return null == e ? e + "" : "object" == typeof e || "function" == typeof e ? n[i.call(e)] || "object" : typeof e } var t = "3.7.1", l = /HTML$/i, ce = function (e, t) { return new ce.fn.init(e, t) }; function c(e) { var t = !!e && "length" in e && e.length, n = x(e); return !v(e) && !y(e) && ("array" === n || 0 === t || "number" == typeof t && 0 < t && t - 1 in e) } function fe(e, t) { return e.nodeName && e.nodeName.toLowerCase() === t.toLowerCase() } ce.fn = ce.prototype = { jquery: t, constructor: ce, length: 0, toArray: function () { return ae.call(this) }, get: function (e) { return null == e ? ae.call(this) : e < 0 ? this[e + this.length] : this[e] }, pushStack: function (e) { var t = ce.merge(this.constructor(), e); return t.prevObject = this, t }, each: function (e) { return ce.each(this, e) }, map: function (n) { return this.pushStack(ce.map(this, function (e, t) { return n.call(e, t, e) })) }, slice: function () { return this.pushStack(ae.apply(this, arguments)) }, first: function () { return this.eq(0) }, last: function () { return this.eq(-1) }, even: function () { return this.pushStack(ce.grep(this, function (e, t) { return (t + 1) % 2 })) }, odd: function () { return this.pushStack(ce.grep(this, function (e, t) { return t % 2 })) }, eq: function (e) { var t = this.length, n = +e + (e < 0 ? t : 0); return this.pushStack(0 <= n && n < t ? [this[n]] : []) }, end: function () { return this.prevObject || this.constructor() }, push: s, sort: oe.sort, splice: oe.splice }, ce.extend = ce.fn.extend = function () { var e, t, n, r, i, o, a = arguments[0] || {}, s = 1, u = arguments.length, l = !1; for ("boolean" == typeof a && (l = a, a = arguments[s] || {}, s++), "object" == typeof a || v(a) || (a = {}), s === u && (a = this, s--); s < u; s++)if (null != (e = arguments[s])) for (t in e) r = e[t], "__proto__" !== t && a !== r && (l && r && (ce.isPlainObject(r) || (i = Array.isArray(r))) ? (n = a[t], o = i && !Array.isArray(n) ? [] : i || ce.isPlainObject(n) ? n : {}, i = !1, a[t] = ce.extend(l, o, r)) : void 0 !== r && (a[t] = r)); return a }, ce.extend({ expando: "jQuery" + (t + Math.random()).replace(/\D/g, ""), isReady: !0, error: function (e) { throw new Error(e) }, noop: function () { }, isPlainObject: function (e) { var t, n; return !(!e || "[object Object]" !== i.call(e)) && (!(t = r(e)) || "function" == typeof (n = ue.call(t, "constructor") && t.constructor) && o.call(n) === a) }, isEmptyObject: function (e) { var t; for (t in e) return !1; return !0 }, globalEval: function (e, t, n) { m(e, { nonce: t && t.nonce }, n) }, each: function (e, t) { var n, r = 0; if (c(e)) { for (n = e.length; r < n; r++)if (!1 === t.call(e[r], r, e[r])) break } else for (r in e) if (!1 === t.call(e[r], r, e[r])) break; return e }, text: function (e) { var t, n = "", r = 0, i = e.nodeType; if (!i) while (t = e[r++]) n += ce.text(t); return 1 === i || 11 === i ? e.textContent : 9 === i ? e.documentElement.textContent : 3 === i || 4 === i ? e.nodeValue : n }, makeArray: function (e, t) { var n = t || []; return null != e && (c(Object(e)) ? ce.merge(n, "string" == typeof e ? [e] : e) : s.call(n, e)), n }, inArray: function (e, t, n) { return null == t ? -1 : se.call(t, e, n) }, isXMLDoc: function (e) { var t = e && e.namespaceURI, n = e && (e.ownerDocument || e).documentElement; return !l.test(t || n && n.nodeName || "HTML") }, merge: function (e, t) { for (var n = +t.length, r = 0, i = e.length; r < n; r++)e[i++] = t[r]; return e.length = i, e }, grep: function (e, t, n) { for (var r = [], i = 0, o = e.length, a = !n; i < o; i++)!t(e[i], i) !== a && r.push(e[i]); return r }, map: function (e, t, n) { var r, i, o = 0, a = []; if (c(e)) for (r = e.length; o < r; o++)null != (i = t(e[o], o, n)) && a.push(i); else for (o in e) null != (i = t(e[o], o, n)) && a.push(i); return g(a) }, guid: 1, support: le }), "function" == typeof Symbol && (ce.fn[Symbol.iterator] = oe[Symbol.iterator]), ce.each("Boolean Number String Function Array Date RegExp Object Error Symbol".split(" "), function (e, t) { n["[object " + t + "]"] = t.toLowerCase() }); var pe = oe.pop, de = oe.sort, he = oe.splice, ge = "[\\x20\\t\\r\\n\\f]", ve = new RegExp("^" + ge + "+|((?:^|[^\\\\])(?:\\\\.)*)" + ge + "+$", "g"); ce.contains = function (e, t) { var n = t && t.parentNode; return e === n || !(!n || 1 !== n.nodeType || !(e.contains ? e.contains(n) : e.compareDocumentPosition && 16 & e.compareDocumentPosition(n))) }; var f = /([\0-\x1f\x7f]|^-?\d)|^-$|[^\x80-\uFFFF\w-]/g; function p(e, t) { return t ? "\0" === e ? "\ufffd" : e.slice(0, -1) + "\\" + e.charCodeAt(e.length - 1).toString(16) + " " : "\\" + e } ce.escapeSelector = function (e) { return (e + "").replace(f, p) }; var ye = C, me = s; !function () { var e, b, w, o, a, T, r, C, d, i, k = me, S = ce.expando, E = 0, n = 0, s = W(), c = W(), u = W(), h = W(), l = function (e, t) { return e === t && (a = !0), 0 }, f = "checked|selected|async|autofocus|autoplay|controls|defer|disabled|hidden|ismap|loop|multiple|open|readonly|required|scoped", t = "(?:\\\\[\\da-fA-F]{1,6}" + ge + "?|\\\\[^\\r\\n\\f]|[\\w-]|[^\0-\\x7f])+", p = "\\[" + ge + "*(" + t + ")(?:" + ge + "*([*^$|!~]?=)" + ge + "*(?:'((?:\\\\.|[^\\\\'])*)'|\"((?:\\\\.|[^\\\\\"])*)\"|(" + t + "))|)" + ge + "*\\]", g = ":(" + t + ")(?:\\((('((?:\\\\.|[^\\\\'])*)'|\"((?:\\\\.|[^\\\\\"])*)\")|((?:\\\\.|[^\\\\()[\\]]|" + p + ")*)|.*)\\)|)", v = new RegExp(ge + "+", "g"), y = new RegExp("^" + ge + "*," + ge + "*"), m = new RegExp("^" + ge + "*([>+~]|" + ge + ")" + ge + "*"), x = new RegExp(ge + "|>"), j = new RegExp(g), A = new RegExp("^" + t + "$"), D = { ID: new RegExp("^#(" + t + ")"), CLASS: new RegExp("^\\.(" + t + ")"), TAG: new RegExp("^(" + t + "|[*])"), ATTR: new RegExp("^" + p), PSEUDO: new RegExp("^" + g), CHILD: new RegExp("^:(only|first|last|nth|nth-last)-(child|of-type)(?:\\(" + ge + "*(even|odd|(([+-]|)(\\d*)n|)" + ge + "*(?:([+-]|)" + ge + "*(\\d+)|))" + ge + "*\\)|)", "i"), bool: new RegExp("^(?:" + f + ")$", "i"), needsContext: new RegExp("^" + ge + "*[>+~]|:(even|odd|eq|gt|lt|nth|first|last)(?:\\(" + ge + "*((?:-\\d)?\\d*)" + ge + "*\\)|)(?=[^-]|$)", "i") }, N = /^(?:input|select|textarea|button)$/i, q = /^h\d$/i, L = /^(?:#([\w-]+)|(\w+)|\.([\w-]+))$/, H = /[+~]/, O = new RegExp("\\\\[\\da-fA-F]{1,6}" + ge + "?|\\\\([^\\r\\n\\f])", "g"), P = function (e, t) { var n = "0x" + e.slice(1) - 65536; return t || (n < 0 ? String.fromCharCode(n + 65536) : String.fromCharCode(n >> 10 | 55296, 1023 & n | 56320)) }, M = function () { V() }, R = J(function (e) { return !0 === e.disabled && fe(e, "fieldset") }, { dir: "parentNode", next: "legend" }); try { k.apply(oe = ae.call(ye.childNodes), ye.childNodes), oe[ye.childNodes.length].nodeType } catch (e) { k = { apply: function (e, t) { me.apply(e, ae.call(t)) }, call: function (e) { me.apply(e, ae.call(arguments, 1)) } } } function I(t, e, n, r) { var i, o, a, s, u, l, c, f = e && e.ownerDocument, p = e ? e.nodeType : 9; if (n = n || [], "string" != typeof t || !t || 1 !== p && 9 !== p && 11 !== p) return n; if (!r && (V(e), e = e || T, C)) { if (11 !== p && (u = L.exec(t))) if (i = u[1]) { if (9 === p) { if (!(a = e.getElementById(i))) return n; if (a.id === i) return k.call(n, a), n } else if (f && (a = f.getElementById(i)) && I.contains(e, a) && a.id === i) return k.call(n, a), n } else { if (u[2]) return k.apply(n, e.getElementsByTagName(t)), n; if ((i = u[3]) && e.getElementsByClassName) return k.apply(n, e.getElementsByClassName(i)), n } if (!(h[t + " "] || d && d.test(t))) { if (c = t, f = e, 1 === p && (x.test(t) || m.test(t))) { (f = H.test(t) && U(e.parentNode) || e) == e && le.scope || ((s = e.getAttribute("id")) ? s = ce.escapeSelector(s) : e.setAttribute("id", s = S)), o = (l = Y(t)).length; while (o--) l[o] = (s ? "#" + s : ":scope") + " " + Q(l[o]); c = l.join(",") } try { return k.apply(n, f.querySelectorAll(c)), n } catch (e) { h(t, !0) } finally { s === S && e.removeAttribute("id") } } } return re(t.replace(ve, "$1"), e, n, r) } function W() { var r = []; return function e(t, n) { return r.push(t + " ") > b.cacheLength && delete e[r.shift()], e[t + " "] = n } } function F(e) { return e[S] = !0, e } function $(e) { var t = T.createElement("fieldset"); try { return !!e(t) } catch (e) { return !1 } finally { t.parentNode && t.parentNode.removeChild(t), t = null } } function B(t) { return function (e) { return fe(e, "input") && e.type === t } } function _(t) { return function (e) { return (fe(e, "input") || fe(e, "button")) && e.type === t } } function z(t) { return function (e) { return "form" in e ? e.parentNode && !1 === e.disabled ? "label" in e ? "label" in e.parentNode ? e.parentNode.disabled === t : e.disabled === t : e.isDisabled === t || e.isDisabled !== !t && R(e) === t : e.disabled === t : "label" in e && e.disabled === t } } function X(a) { return F(function (o) { return o = +o, F(function (e, t) { var n, r = a([], e.length, o), i = r.length; while (i--) e[n = r[i]] && (e[n] = !(t[n] = e[n])) }) }) } function U(e) { return e && "undefined" != typeof e.getElementsByTagName && e } function V(e) { var t, n = e ? e.ownerDocument || e : ye; return n != T && 9 === n.nodeType && n.documentElement && (r = (T = n).documentElement, C = !ce.isXMLDoc(T), i = r.matches || r.webkitMatchesSelector || r.msMatchesSelector, r.msMatchesSelector && ye != T && (t = T.defaultView) && t.top !== t && t.addEventListener("unload", M), le.getById = $(function (e) { return r.appendChild(e).id = ce.expando, !T.getElementsByName || !T.getElementsByName(ce.expando).length }), le.disconnectedMatch = $(function (e) { return i.call(e, "*") }), le.scope = $(function () { return T.querySelectorAll(":scope") }), le.cssHas = $(function () { try { return T.querySelector(":has(*,:jqfake)"), !1 } catch (e) { return !0 } }), le.getById ? (b.filter.ID = function (e) { var t = e.replace(O, P); return function (e) { return e.getAttribute("id") === t } }, b.find.ID = function (e, t) { if ("undefined" != typeof t.getElementById && C) { var n = t.getElementById(e); return n ? [n] : [] } }) : (b.filter.ID = function (e) { var n = e.replace(O, P); return function (e) { var t = "undefined" != typeof e.getAttributeNode && e.getAttributeNode("id"); return t && t.value === n } }, b.find.ID = function (e, t) { if ("undefined" != typeof t.getElementById && C) { var n, r, i, o = t.getElementById(e); if (o) { if ((n = o.getAttributeNode("id")) && n.value === e) return [o]; i = t.getElementsByName(e), r = 0; while (o = i[r++]) if ((n = o.getAttributeNode("id")) && n.value === e) return [o] } return [] } }), b.find.TAG = function (e, t) { return "undefined" != typeof t.getElementsByTagName ? t.getElementsByTagName(e) : t.querySelectorAll(e) }, b.find.CLASS = function (e, t) { if ("undefined" != typeof t.getElementsByClassName && C) return t.getElementsByClassName(e) }, d = [], $(function (e) { var t; r.appendChild(e).innerHTML = "<a id='" + S + "' href='' disabled='disabled'></a><select id='" + S + "-\r\\' disabled='disabled'><option selected=''></option></select>", e.querySelectorAll("[selected]").length || d.push("\\[" + ge + "*(?:value|" + f + ")"), e.querySelectorAll("[id~=" + S + "-]").length || d.push("~="), e.querySelectorAll("a#" + S + "+*").length || d.push(".#.+[+~]"), e.querySelectorAll(":checked").length || d.push(":checked"), (t = T.createElement("input")).setAttribute("type", "hidden"), e.appendChild(t).setAttribute("name", "D"), r.appendChild(e).disabled = !0, 2 !== e.querySelectorAll(":disabled").length && d.push(":enabled", ":disabled"), (t = T.createElement("input")).setAttribute("name", ""), e.appendChild(t), e.querySelectorAll("[name='']").length || d.push("\\[" + ge + "*name" + ge + "*=" + ge + "*(?:''|\"\")") }), le.cssHas || d.push(":has"), d = d.length && new RegExp(d.join("|")), l = function (e, t) { if (e === t) return a = !0, 0; var n = !e.compareDocumentPosition - !t.compareDocumentPosition; return n || (1 & (n = (e.ownerDocument || e) == (t.ownerDocument || t) ? e.compareDocumentPosition(t) : 1) || !le.sortDetached && t.compareDocumentPosition(e) === n ? e === T || e.ownerDocument == ye && I.contains(ye, e) ? -1 : t === T || t.ownerDocument == ye && I.contains(ye, t) ? 1 : o ? se.call(o, e) - se.call(o, t) : 0 : 4 & n ? -1 : 1) }), T } for (e in I.matches = function (e, t) { return I(e, null, null, t) }, I.matchesSelector = function (e, t) { if (V(e), C && !h[t + " "] && (!d || !d.test(t))) try { var n = i.call(e, t); if (n || le.disconnectedMatch || e.document && 11 !== e.document.nodeType) return n } catch (e) { h(t, !0) } return 0 < I(t, T, null, [e]).length }, I.contains = function (e, t) { return (e.ownerDocument || e) != T && V(e), ce.contains(e, t) }, I.attr = function (e, t) { (e.ownerDocument || e) != T && V(e); var n = b.attrHandle[t.toLowerCase()], r = n && ue.call(b.attrHandle, t.toLowerCase()) ? n(e, t, !C) : void 0; return void 0 !== r ? r : e.getAttribute(t) }, I.error = function (e) { throw new Error("Syntax error, unrecognized expression: " + e) }, ce.uniqueSort = function (e) { var t, n = [], r = 0, i = 0; if (a = !le.sortStable, o = !le.sortStable && ae.call(e, 0), de.call(e, l), a) { while (t = e[i++]) t === e[i] && (r = n.push(i)); while (r--) he.call(e, n[r], 1) } return o = null, e }, ce.fn.uniqueSort = function () { return this.pushStack(ce.uniqueSort(ae.apply(this))) }, (b = ce.expr = { cacheLength: 50, createPseudo: F, match: D, attrHandle: {}, find: {}, relative: { ">": { dir: "parentNode", first: !0 }, " ": { dir: "parentNode" }, "+": { dir: "previousSibling", first: !0 }, "~": { dir: "previousSibling" } }, preFilter: { ATTR: function (e) { return e[1] = e[1].replace(O, P), e[3] = (e[3] || e[4] || e[5] || "").replace(O, P), "~=" === e[2] && (e[3] = " " + e[3] + " "), e.slice(0, 4) }, CHILD: function (e) { return e[1] = e[1].toLowerCase(), "nth" === e[1].slice(0, 3) ? (e[3] || I.error(e[0]), e[4] = +(e[4] ? e[5] + (e[6] || 1) : 2 * ("even" === e[3] || "odd" === e[3])), e[5] = +(e[7] + e[8] || "odd" === e[3])) : e[3] && I.error(e[0]), e }, PSEUDO: function (e) { var t, n = !e[6] && e[2]; return D.CHILD.test(e[0]) ? null : (e[3] ? e[2] = e[4] || e[5] || "" : n && j.test(n) && (t = Y(n, !0)) && (t = n.indexOf(")", n.length - t) - n.length) && (e[0] = e[0].slice(0, t), e[2] = n.slice(0, t)), e.slice(0, 3)) } }, filter: { TAG: function (e) { var t = e.replace(O, P).toLowerCase(); return "*" === e ? function () { return !0 } : function (e) { return fe(e, t) } }, CLASS: function (e) { var t = s[e + " "]; return t || (t = new RegExp("(^|" + ge + ")" + e + "(" + ge + "|$)")) && s(e, function (e) { return t.test("string" == typeof e.className && e.className || "undefined" != typeof e.getAttribute && e.getAttribute("class") || "") }) }, ATTR: function (n, r, i) { return function (e) { var t = I.attr(e, n); return null == t ? "!=" === r : !r || (t += "", "=" === r ? t === i : "!=" === r ? t !== i : "^=" === r ? i && 0 === t.indexOf(i) : "*=" === r ? i && -1 < t.indexOf(i) : "$=" === r ? i && t.slice(-i.length) === i : "~=" === r ? -1 < (" " + t.replace(v, " ") + " ").indexOf(i) : "|=" === r && (t === i || t.slice(0, i.length + 1) === i + "-")) } }, CHILD: function (d, e, t, h, g) { var v = "nth" !== d.slice(0, 3), y = "last" !== d.slice(-4), m = "of-type" === e; return 1 === h && 0 === g ? function (e) { return !!e.parentNode } : function (e, t, n) { var r, i, o, a, s, u = v !== y ? "nextSibling" : "previousSibling", l = e.parentNode, c = m && e.nodeName.toLowerCase(), f = !n && !m, p = !1; if (l) { if (v) { while (u) { o = e; while (o = o[u]) if (m ? fe(o, c) : 1 === o.nodeType) return !1; s = u = "only" === d && !s && "nextSibling" } return !0 } if (s = [y ? l.firstChild : l.lastChild], y && f) { p = (a = (r = (i = l[S] || (l[S] = {}))[d] || [])[0] === E && r[1]) && r[2], o = a && l.childNodes[a]; while (o = ++a && o && o[u] || (p = a = 0) || s.pop()) if (1 === o.nodeType && ++p && o === e) { i[d] = [E, a, p]; break } } else if (f && (p = a = (r = (i = e[S] || (e[S] = {}))[d] || [])[0] === E && r[1]), !1 === p) while (o = ++a && o && o[u] || (p = a = 0) || s.pop()) if ((m ? fe(o, c) : 1 === o.nodeType) && ++p && (f && ((i = o[S] || (o[S] = {}))[d] = [E, p]), o === e)) break; return (p -= g) === h || p % h == 0 && 0 <= p / h } } }, PSEUDO: function (e, o) { var t, a = b.pseudos[e] || b.setFilters[e.toLowerCase()] || I.error("unsupported pseudo: " + e); return a[S] ? a(o) : 1 < a.length ? (t = [e, e, "", o], b.setFilters.hasOwnProperty(e.toLowerCase()) ? F(function (e, t) { var n, r = a(e, o), i = r.length; while (i--) e[n = se.call(e, r[i])] = !(t[n] = r[i]) }) : function (e) { return a(e, 0, t) }) : a } }, pseudos: { not: F(function (e) { var r = [], i = [], s = ne(e.replace(ve, "$1")); return s[S] ? F(function (e, t, n, r) { var i, o = s(e, null, r, []), a = e.length; while (a--) (i = o[a]) && (e[a] = !(t[a] = i)) }) : function (e, t, n) { return r[0] = e, s(r, null, n, i), r[0] = null, !i.pop() } }), has: F(function (t) { return function (e) { return 0 < I(t, e).length } }), contains: F(function (t) { return t = t.replace(O, P), function (e) { return -1 < (e.textContent || ce.text(e)).indexOf(t) } }), lang: F(function (n) { return A.test(n || "") || I.error("unsupported lang: " + n), n = n.replace(O, P).toLowerCase(), function (e) { var t; do { if (t = C ? e.lang : e.getAttribute("xml:lang") || e.getAttribute("lang")) return (t = t.toLowerCase()) === n || 0 === t.indexOf(n + "-") } while ((e = e.parentNode) && 1 === e.nodeType); return !1 } }), target: function (e) { var t = ie.location && ie.location.hash; return t && t.slice(1) === e.id }, root: function (e) { return e === r }, focus: function (e) { return e === function () { try { return T.activeElement } catch (e) { } }() && T.hasFocus() && !!(e.type || e.href || ~e.tabIndex) }, enabled: z(!1), disabled: z(!0), checked: function (e) { return fe(e, "input") && !!e.checked || fe(e, "option") && !!e.selected }, selected: function (e) { return e.parentNode && e.parentNode.selectedIndex, !0 === e.selected }, empty: function (e) { for (e = e.firstChild; e; e = e.nextSibling)if (e.nodeType < 6) return !1; return !0 }, parent: function (e) { return !b.pseudos.empty(e) }, header: function (e) { return q.test(e.nodeName) }, input: function (e) { return N.test(e.nodeName) }, button: function (e) { return fe(e, "input") && "button" === e.type || fe(e, "button") }, text: function (e) { var t; return fe(e, "input") && "text" === e.type && (null == (t = e.getAttribute("type")) || "text" === t.toLowerCase()) }, first: X(function () { return [0] }), last: X(function (e, t) { return [t - 1] }), eq: X(function (e, t, n) { return [n < 0 ? n + t : n] }), even: X(function (e, t) { for (var n = 0; n < t; n += 2)e.push(n); return e }), odd: X(function (e, t) { for (var n = 1; n < t; n += 2)e.push(n); return e }), lt: X(function (e, t, n) { var r; for (r = n < 0 ? n + t : t < n ? t : n; 0 <= --r;)e.push(r); return e }), gt: X(function (e, t, n) { for (var r = n < 0 ? n + t : n; ++r < t;)e.push(r); return e }) } }).pseudos.nth = b.pseudos.eq, { radio: !0, checkbox: !0, file: !0, password: !0, image: !0 }) b.pseudos[e] = B(e); for (e in { submit: !0, reset: !0 }) b.pseudos[e] = _(e); function G() { } function Y(e, t) { var n, r, i, o, a, s, u, l = c[e + " "]; if (l) return t ? 0 : l.slice(0); a = e, s = [], u = b.preFilter; while (a) { for (o in n && !(r = y.exec(a)) || (r && (a = a.slice(r[0].length) || a), s.push(i = [])), n = !1, (r = m.exec(a)) && (n = r.shift(), i.push({ value: n, type: r[0].replace(ve, " ") }), a = a.slice(n.length)), b.filter) !(r = D[o].exec(a)) || u[o] && !(r = u[o](r)) || (n = r.shift(), i.push({ value: n, type: o, matches: r }), a = a.slice(n.length)); if (!n) break } return t ? a.length : a ? I.error(e) : c(e, s).slice(0) } function Q(e) { for (var t = 0, n = e.length, r = ""; t < n; t++)r += e[t].value; return r } function J(a, e, t) { var s = e.dir, u = e.next, l = u || s, c = t && "parentNode" === l, f = n++; return e.first ? function (e, t, n) { while (e = e[s]) if (1 === e.nodeType || c) return a(e, t, n); return !1 } : function (e, t, n) { var r, i, o = [E, f]; if (n) { while (e = e[s]) if ((1 === e.nodeType || c) && a(e, t, n)) return !0 } else while (e = e[s]) if (1 === e.nodeType || c) if (i = e[S] || (e[S] = {}), u && fe(e, u)) e = e[s] || e; else { if ((r = i[l]) && r[0] === E && r[1] === f) return o[2] = r[2]; if ((i[l] = o)[2] = a(e, t, n)) return !0 } return !1 } } function K(i) { return 1 < i.length ? function (e, t, n) { var r = i.length; while (r--) if (!i[r](e, t, n)) return !1; return !0 } : i[0] } function Z(e, t, n, r, i) { for (var o, a = [], s = 0, u = e.length, l = null != t; s < u; s++)(o = e[s]) && (n && !n(o, r, i) || (a.push(o), l && t.push(s))); return a } function ee(d, h, g, v, y, e) { return v && !v[S] && (v = ee(v)), y && !y[S] && (y = ee(y, e)), F(function (e, t, n, r) { var i, o, a, s, u = [], l = [], c = t.length, f = e || function (e, t, n) { for (var r = 0, i = t.length; r < i; r++)I(e, t[r], n); return n }(h || "*", n.nodeType ? [n] : n, []), p = !d || !e && h ? f : Z(f, u, d, n, r); if (g ? g(p, s = y || (e ? d : c || v) ? [] : t, n, r) : s = p, v) { i = Z(s, l), v(i, [], n, r), o = i.length; while (o--) (a = i[o]) && (s[l[o]] = !(p[l[o]] = a)) } if (e) { if (y || d) { if (y) { i = [], o = s.length; while (o--) (a = s[o]) && i.push(p[o] = a); y(null, s = [], i, r) } o = s.length; while (o--) (a = s[o]) && -1 < (i = y ? se.call(e, a) : u[o]) && (e[i] = !(t[i] = a)) } } else s = Z(s === t ? s.splice(c, s.length) : s), y ? y(null, t, s, r) : k.apply(t, s) }) } function te(e) { for (var i, t, n, r = e.length, o = b.relative[e[0].type], a = o || b.relative[" "], s = o ? 1 : 0, u = J(function (e) { return e === i }, a, !0), l = J(function (e) { return -1 < se.call(i, e) }, a, !0), c = [function (e, t, n) { var r = !o && (n || t != w) || ((i = t).nodeType ? u(e, t, n) : l(e, t, n)); return i = null, r }]; s < r; s++)if (t = b.relative[e[s].type]) c = [J(K(c), t)]; else { if ((t = b.filter[e[s].type].apply(null, e[s].matches))[S]) { for (n = ++s; n < r; n++)if (b.relative[e[n].type]) break; return ee(1 < s && K(c), 1 < s && Q(e.slice(0, s - 1).concat({ value: " " === e[s - 2].type ? "*" : "" })).replace(ve, "$1"), t, s < n && te(e.slice(s, n)), n < r && te(e = e.slice(n)), n < r && Q(e)) } c.push(t) } return K(c) } function ne(e, t) { var n, v, y, m, x, r, i = [], o = [], a = u[e + " "]; if (!a) { t || (t = Y(e)), n = t.length; while (n--) (a = te(t[n]))[S] ? i.push(a) : o.push(a); (a = u(e, (v = o, m = 0 < (y = i).length, x = 0 < v.length, r = function (e, t, n, r, i) { var o, a, s, u = 0, l = "0", c = e && [], f = [], p = w, d = e || x && b.find.TAG("*", i), h = E += null == p ? 1 : Math.random() || .1, g = d.length; for (i && (w = t == T || t || i); l !== g && null != (o = d[l]); l++) { if (x && o) { a = 0, t || o.ownerDocument == T || (V(o), n = !C); while (s = v[a++]) if (s(o, t || T, n)) { k.call(r, o); break } i && (E = h) } m && ((o = !s && o) && u--, e && c.push(o)) } if (u += l, m && l !== u) { a = 0; while (s = y[a++]) s(c, f, t, n); if (e) { if (0 < u) while (l--) c[l] || f[l] || (f[l] = pe.call(r)); f = Z(f) } k.apply(r, f), i && !e && 0 < f.length && 1 < u + y.length && ce.uniqueSort(r) } return i && (E = h, w = p), c }, m ? F(r) : r))).selector = e } return a } function re(e, t, n, r) { var i, o, a, s, u, l = "function" == typeof e && e, c = !r && Y(e = l.selector || e); if (n = n || [], 1 === c.length) { if (2 < (o = c[0] = c[0].slice(0)).length && "ID" === (a = o[0]).type && 9 === t.nodeType && C && b.relative[o[1].type]) { if (!(t = (b.find.ID(a.matches[0].replace(O, P), t) || [])[0])) return n; l && (t = t.parentNode), e = e.slice(o.shift().value.length) } i = D.needsContext.test(e) ? 0 : o.length; while (i--) { if (a = o[i], b.relative[s = a.type]) break; if ((u = b.find[s]) && (r = u(a.matches[0].replace(O, P), H.test(o[0].type) && U(t.parentNode) || t))) { if (o.splice(i, 1), !(e = r.length && Q(o))) return k.apply(n, r), n; break } } } return (l || ne(e, c))(r, t, !C, n, !t || H.test(e) && U(t.parentNode) || t), n } G.prototype = b.filters = b.pseudos, b.setFilters = new G, le.sortStable = S.split("").sort(l).join("") === S, V(), le.sortDetached = $(function (e) { return 1 & e.compareDocumentPosition(T.createElement("fieldset")) }), ce.find = I, ce.expr[":"] = ce.expr.pseudos, ce.unique = ce.uniqueSort, I.compile = ne, I.select = re, I.setDocument = V, I.tokenize = Y, I.escape = ce.escapeSelector, I.getText = ce.text, I.isXML = ce.isXMLDoc, I.selectors = ce.expr, I.support = ce.support, I.uniqueSort = ce.uniqueSort }(); var d = function (e, t, n) { var r = [], i = void 0 !== n; while ((e = e[t]) && 9 !== e.nodeType) if (1 === e.nodeType) { if (i && ce(e).is(n)) break; r.push(e) } return r }, h = function (e, t) { for (var n = []; e; e = e.nextSibling)1 === e.nodeType && e !== t && n.push(e); return n }, b = ce.expr.match.needsContext, w = /^<([a-z][^\/\0>:\x20\t\r\n\f]*)[\x20\t\r\n\f]*\/?>(?:<\/\1>|)$/i; function T(e, n, r) { return v(n) ? ce.grep(e, function (e, t) { return !!n.call(e, t, e) !== r }) : n.nodeType ? ce.grep(e, function (e) { return e === n !== r }) : "string" != typeof n ? ce.grep(e, function (e) { return -1 < se.call(n, e) !== r }) : ce.filter(n, e, r) } ce.filter = function (e, t, n) { var r = t[0]; return n && (e = ":not(" + e + ")"), 1 === t.length && 1 === r.nodeType ? ce.find.matchesSelector(r, e) ? [r] : [] : ce.find.matches(e, ce.grep(t, function (e) { return 1 === e.nodeType })) }, ce.fn.extend({ find: function (e) { var t, n, r = this.length, i = this; if ("string" != typeof e) return this.pushStack(ce(e).filter(function () { for (t = 0; t < r; t++)if (ce.contains(i[t], this)) return !0 })); for (n = this.pushStack([]), t = 0; t < r; t++)ce.find(e, i[t], n); return 1 < r ? ce.uniqueSort(n) : n }, filter: function (e) { return this.pushStack(T(this, e || [], !1)) }, not: function (e) { return this.pushStack(T(this, e || [], !0)) }, is: function (e) { return !!T(this, "string" == typeof e && b.test(e) ? ce(e) : e || [], !1).length } }); var k, S = /^(?:\s*(<[\w\W]+>)[^>]*|#([\w-]+))$/; (ce.fn.init = function (e, t, n) { var r, i; if (!e) return this; if (n = n || k, "string" == typeof e) { if (!(r = "<" === e[0] && ">" === e[e.length - 1] && 3 <= e.length ? [null, e, null] : S.exec(e)) || !r[1] && t) return !t || t.jquery ? (t || n).find(e) : this.constructor(t).find(e); if (r[1]) { if (t = t instanceof ce ? t[0] : t, ce.merge(this, ce.parseHTML(r[1], t && t.nodeType ? t.ownerDocument || t : C, !0)), w.test(r[1]) && ce.isPlainObject(t)) for (r in t) v(this[r]) ? this[r](t[r]) : this.attr(r, t[r]); return this } return (i = C.getElementById(r[2])) && (this[0] = i, this.length = 1), this } return e.nodeType ? (this[0] = e, this.length = 1, this) : v(e) ? void 0 !== n.ready ? n.ready(e) : e(ce) : ce.makeArray(e, this) }).prototype = ce.fn, k = ce(C); var E = /^(?:parents|prev(?:Until|All))/, j = { children: !0, contents: !0, next: !0, prev: !0 }; function A(e, t) { while ((e = e[t]) && 1 !== e.nodeType); return e } ce.fn.extend({ has: function (e) { var t = ce(e, this), n = t.length; return this.filter(function () { for (var e = 0; e < n; e++)if (ce.contains(this, t[e])) return !0 }) }, closest: function (e, t) { var n, r = 0, i = this.length, o = [], a = "string" != typeof e && ce(e); if (!b.test(e)) for (; r < i; r++)for (n = this[r]; n && n !== t; n = n.parentNode)if (n.nodeType < 11 && (a ? -1 < a.index(n) : 1 === n.nodeType && ce.find.matchesSelector(n, e))) { o.push(n); break } return this.pushStack(1 < o.length ? ce.uniqueSort(o) : o) }, index: function (e) { return e ? "string" == typeof e ? se.call(ce(e), this[0]) : se.call(this, e.jquery ? e[0] : e) : this[0] && this[0].parentNode ? this.first().prevAll().length : -1 }, add: function (e, t) { return this.pushStack(ce.uniqueSort(ce.merge(this.get(), ce(e, t)))) }, addBack: function (e) { return this.add(null == e ? this.prevObject : this.prevObject.filter(e)) } }), ce.each({ parent: function (e) { var t = e.parentNode; return t && 11 !== t.nodeType ? t : null }, parents: function (e) { return d(e, "parentNode") }, parentsUntil: function (e, t, n) { return d(e, "parentNode", n) }, next: function (e) { return A(e, "nextSibling") }, prev: function (e) { return A(e, "previousSibling") }, nextAll: function (e) { return d(e, "nextSibling") }, prevAll: function (e) { return d(e, "previousSibling") }, nextUntil: function (e, t, n) { return d(e, "nextSibling", n) }, prevUntil: function (e, t, n) { return d(e, "previousSibling", n) }, siblings: function (e) { return h((e.parentNode || {}).firstChild, e) }, children: function (e) { return h(e.firstChild) }, contents: function (e) { return null != e.contentDocument && r(e.contentDocument) ? e.contentDocument : (fe(e, "template") && (e = e.content || e), ce.merge([], e.childNodes)) } }, function (r, i) { ce.fn[r] = function (e, t) { var n = ce.map(this, i, e); return "Until" !== r.slice(-5) && (t = e), t && "string" == typeof t && (n = ce.filter(t, n)), 1 < this.length && (j[r] || ce.uniqueSort(n), E.test(r) && n.reverse()), this.pushStack(n) } }); var D = /[^\x20\t\r\n\f]+/g; function N(e) { return e } function q(e) { throw e } function L(e, t, n, r) { var i; try { e && v(i = e.promise) ? i.call(e).done(t).fail(n) : e && v(i = e.then) ? i.call(e, t, n) : t.apply(void 0, [e].slice(r)) } catch (e) { n.apply(void 0, [e]) } } ce.Callbacks = function (r) { var e, n; r = "string" == typeof r ? (e = r, n = {}, ce.each(e.match(D) || [], function (e, t) { n[t] = !0 }), n) : ce.extend({}, r); var i, t, o, a, s = [], u = [], l = -1, c = function () { for (a = a || r.once, o = i = !0; u.length; l = -1) { t = u.shift(); while (++l < s.length) !1 === s[l].apply(t[0], t[1]) && r.stopOnFalse && (l = s.length, t = !1) } r.memory || (t = !1), i = !1, a && (s = t ? [] : "") }, f = { add: function () { return s && (t && !i && (l = s.length - 1, u.push(t)), function n(e) { ce.each(e, function (e, t) { v(t) ? r.unique && f.has(t) || s.push(t) : t && t.length && "string" !== x(t) && n(t) }) }(arguments), t && !i && c()), this }, remove: function () { return ce.each(arguments, function (e, t) { var n; while (-1 < (n = ce.inArray(t, s, n))) s.splice(n, 1), n <= l && l-- }), this }, has: function (e) { return e ? -1 < ce.inArray(e, s) : 0 < s.length }, empty: function () { return s && (s = []), this }, disable: function () { return a = u = [], s = t = "", this }, disabled: function () { return !s }, lock: function () { return a = u = [], t || i || (s = t = ""), this }, locked: function () { return !!a }, fireWith: function (e, t) { return a || (t = [e, (t = t || []).slice ? t.slice() : t], u.push(t), i || c()), this }, fire: function () { return f.fireWith(this, arguments), this }, fired: function () { return !!o } }; return f }, ce.extend({ Deferred: function (e) { var o = [["notify", "progress", ce.Callbacks("memory"), ce.Callbacks("memory"), 2], ["resolve", "done", ce.Callbacks("once memory"), ce.Callbacks("once memory"), 0, "resolved"], ["reject", "fail", ce.Callbacks("once memory"), ce.Callbacks("once memory"), 1, "rejected"]], i = "pending", a = { state: function () { return i }, always: function () { return s.done(arguments).fail(arguments), this }, "catch": function (e) { return a.then(null, e) }, pipe: function () { var i = arguments; return ce.Deferred(function (r) { ce.each(o, function (e, t) { var n = v(i[t[4]]) && i[t[4]]; s[t[1]](function () { var e = n && n.apply(this, arguments); e && v(e.promise) ? e.promise().progress(r.notify).done(r.resolve).fail(r.reject) : r[t[0] + "With"](this, n ? [e] : arguments) }) }), i = null }).promise() }, then: function (t, n, r) { var u = 0; function l(i, o, a, s) { return function () { var n = this, r = arguments, e = function () { var e, t; if (!(i < u)) { if ((e = a.apply(n, r)) === o.promise()) throw new TypeError("Thenable self-resolution"); t = e && ("object" == typeof e || "function" == typeof e) && e.then, v(t) ? s ? t.call(e, l(u, o, N, s), l(u, o, q, s)) : (u++, t.call(e, l(u, o, N, s), l(u, o, q, s), l(u, o, N, o.notifyWith))) : (a !== N && (n = void 0, r = [e]), (s || o.resolveWith)(n, r)) } }, t = s ? e : function () { try { e() } catch (e) { ce.Deferred.exceptionHook && ce.Deferred.exceptionHook(e, t.error), u <= i + 1 && (a !== q && (n = void 0, r = [e]), o.rejectWith(n, r)) } }; i ? t() : (ce.Deferred.getErrorHook ? t.error = ce.Deferred.getErrorHook() : ce.Deferred.getStackHook && (t.error = ce.Deferred.getStackHook()), ie.setTimeout(t)) } } return ce.Deferred(function (e) { o[0][3].add(l(0, e, v(r) ? r : N, e.notifyWith)), o[1][3].add(l(0, e, v(t) ? t : N)), o[2][3].add(l(0, e, v(n) ? n : q)) }).promise() }, promise: function (e) { return null != e ? ce.extend(e, a) : a } }, s = {}; return ce.each(o, function (e, t) { var n = t[2], r = t[5]; a[t[1]] = n.add, r && n.add(function () { i = r }, o[3 - e][2].disable, o[3 - e][3].disable, o[0][2].lock, o[0][3].lock), n.add(t[3].fire), s[t[0]] = function () { return s[t[0] + "With"](this === s ? void 0 : this, arguments), this }, s[t[0] + "With"] = n.fireWith }), a.promise(s), e && e.call(s, s), s }, when: function (e) { var n = arguments.length, t = n, r = Array(t), i = ae.call(arguments), o = ce.Deferred(), a = function (t) { return function (e) { r[t] = this, i[t] = 1 < arguments.length ? ae.call(arguments) : e, --n || o.resolveWith(r, i) } }; if (n <= 1 && (L(e, o.done(a(t)).resolve, o.reject, !n), "pending" === o.state() || v(i[t] && i[t].then))) return o.then(); while (t--) L(i[t], a(t), o.reject); return o.promise() } }); var H = /^(Eval|Internal|Range|Reference|Syntax|Type|URI)Error$/; ce.Deferred.exceptionHook = function (e, t) { ie.console && ie.console.warn && e && H.test(e.name) && ie.console.warn("jQuery.Deferred exception: " + e.message, e.stack, t) }, ce.readyException = function (e) { ie.setTimeout(function () { throw e }) }; var O = ce.Deferred(); function P() { C.removeEventListener("DOMContentLoaded", P), ie.removeEventListener("load", P), ce.ready() } ce.fn.ready = function (e) { return O.then(e)["catch"](function (e) { ce.readyException(e) }), this }, ce.extend({ isReady: !1, readyWait: 1, ready: function (e) { (!0 === e ? --ce.readyWait : ce.isReady) || (ce.isReady = !0) !== e && 0 < --ce.readyWait || O.resolveWith(C, [ce]) } }), ce.ready.then = O.then, "complete" === C.readyState || "loading" !== C.readyState && !C.documentElement.doScroll ? ie.setTimeout(ce.ready) : (C.addEventListener("DOMContentLoaded", P), ie.addEventListener("load", P)); var M = function (e, t, n, r, i, o, a) { var s = 0, u = e.length, l = null == n; if ("object" === x(n)) for (s in i = !0, n) M(e, t, s, n[s], !0, o, a); else if (void 0 !== r && (i = !0, v(r) || (a = !0), l && (a ? (t.call(e, r), t = null) : (l = t, t = function (e, t, n) { return l.call(ce(e), n) })), t)) for (; s < u; s++)t(e[s], n, a ? r : r.call(e[s], s, t(e[s], n))); return i ? e : l ? t.call(e) : u ? t(e[0], n) : o }, R = /^-ms-/, I = /-([a-z])/g; function W(e, t) { return t.toUpperCase() } function F(e) { return e.replace(R, "ms-").replace(I, W) } var $ = function (e) { return 1 === e.nodeType || 9 === e.nodeType || !+e.nodeType }; function B() { this.expando = ce.expando + B.uid++ } B.uid = 1, B.prototype = { cache: function (e) { var t = e[this.expando]; return t || (t = {}, $(e) && (e.nodeType ? e[this.expando] = t : Object.defineProperty(e, this.expando, { value: t, configurable: !0 }))), t }, set: function (e, t, n) { var r, i = this.cache(e); if ("string" == typeof t) i[F(t)] = n; else for (r in t) i[F(r)] = t[r]; return i }, get: function (e, t) { return void 0 === t ? this.cache(e) : e[this.expando] && e[this.expando][F(t)] }, access: function (e, t, n) { return void 0 === t || t && "string" == typeof t && void 0 === n ? this.get(e, t) : (this.set(e, t, n), void 0 !== n ? n : t) }, remove: function (e, t) { var n, r = e[this.expando]; if (void 0 !== r) { if (void 0 !== t) { n = (t = Array.isArray(t) ? t.map(F) : (t = F(t)) in r ? [t] : t.match(D) || []).length; while (n--) delete r[t[n]] } (void 0 === t || ce.isEmptyObject(r)) && (e.nodeType ? e[this.expando] = void 0 : delete e[this.expando]) } }, hasData: function (e) { var t = e[this.expando]; return void 0 !== t && !ce.isEmptyObject(t) } }; var _ = new B, z = new B, X = /^(?:\{[\w\W]*\}|\[[\w\W]*\])$/, U = /[A-Z]/g; function V(e, t, n) { var r, i; if (void 0 === n && 1 === e.nodeType) if (r = "data-" + t.replace(U, "-$&").toLowerCase(), "string" == typeof (n = e.getAttribute(r))) { try { n = "true" === (i = n) || "false" !== i && ("null" === i ? null : i === +i + "" ? +i : X.test(i) ? JSON.parse(i) : i) } catch (e) { } z.set(e, t, n) } else n = void 0; return n } ce.extend({ hasData: function (e) { return z.hasData(e) || _.hasData(e) }, data: function (e, t, n) { return z.access(e, t, n) }, removeData: function (e, t) { z.remove(e, t) }, _data: function (e, t, n) { return _.access(e, t, n) }, _removeData: function (e, t) { _.remove(e, t) } }), ce.fn.extend({ data: function (n, e) { var t, r, i, o = this[0], a = o && o.attributes; if (void 0 === n) { if (this.length && (i = z.get(o), 1 === o.nodeType && !_.get(o, "hasDataAttrs"))) { t = a.length; while (t--) a[t] && 0 === (r = a[t].name).indexOf("data-") && (r = F(r.slice(5)), V(o, r, i[r])); _.set(o, "hasDataAttrs", !0) } return i } return "object" == typeof n ? this.each(function () { z.set(this, n) }) : M(this, function (e) { var t; if (o && void 0 === e) return void 0 !== (t = z.get(o, n)) ? t : void 0 !== (t = V(o, n)) ? t : void 0; this.each(function () { z.set(this, n, e) }) }, null, e, 1 < arguments.length, null, !0) }, removeData: function (e) { return this.each(function () { z.remove(this, e) }) } }), ce.extend({ queue: function (e, t, n) { var r; if (e) return t = (t || "fx") + "queue", r = _.get(e, t), n && (!r || Array.isArray(n) ? r = _.access(e, t, ce.makeArray(n)) : r.push(n)), r || [] }, dequeue: function (e, t) { t = t || "fx"; var n = ce.queue(e, t), r = n.length, i = n.shift(), o = ce._queueHooks(e, t); "inprogress" === i && (i = n.shift(), r--), i && ("fx" === t && n.unshift("inprogress"), delete o.stop, i.call(e, function () { ce.dequeue(e, t) }, o)), !r && o && o.empty.fire() }, _queueHooks: function (e, t) { var n = t + "queueHooks"; return _.get(e, n) || _.access(e, n, { empty: ce.Callbacks("once memory").add(function () { _.remove(e, [t + "queue", n]) }) }) } }), ce.fn.extend({ queue: function (t, n) { var e = 2; return "string" != typeof t && (n = t, t = "fx", e--), arguments.length < e ? ce.queue(this[0], t) : void 0 === n ? this : this.each(function () { var e = ce.queue(this, t, n); ce._queueHooks(this, t), "fx" === t && "inprogress" !== e[0] && ce.dequeue(this, t) }) }, dequeue: function (e) { return this.each(function () { ce.dequeue(this, e) }) }, clearQueue: function (e) { return this.queue(e || "fx", []) }, promise: function (e, t) { var n, r = 1, i = ce.Deferred(), o = this, a = this.length, s = function () { --r || i.resolveWith(o, [o]) }; "string" != typeof e && (t = e, e = void 0), e = e || "fx"; while (a--) (n = _.get(o[a], e + "queueHooks")) && n.empty && (r++, n.empty.add(s)); return s(), i.promise(t) } }); var G = /[+-]?(?:\d*\.|)\d+(?:[eE][+-]?\d+|)/.source, Y = new RegExp("^(?:([+-])=|)(" + G + ")([a-z%]*)$", "i"), Q = ["Top", "Right", "Bottom", "Left"], J = C.documentElement, K = function (e) { return ce.contains(e.ownerDocument, e) }, Z = { composed: !0 }; J.getRootNode && (K = function (e) { return ce.contains(e.ownerDocument, e) || e.getRootNode(Z) === e.ownerDocument }); var ee = function (e, t) { return "none" === (e = t || e).style.display || "" === e.style.display && K(e) && "none" === ce.css(e, "display") }; function te(e, t, n, r) { var i, o, a = 20, s = r ? function () { return r.cur() } : function () { return ce.css(e, t, "") }, u = s(), l = n && n[3] || (ce.cssNumber[t] ? "" : "px"), c = e.nodeType && (ce.cssNumber[t] || "px" !== l && +u) && Y.exec(ce.css(e, t)); if (c && c[3] !== l) { u /= 2, l = l || c[3], c = +u || 1; while (a--) ce.style(e, t, c + l), (1 - o) * (1 - (o = s() / u || .5)) <= 0 && (a = 0), c /= o; c *= 2, ce.style(e, t, c + l), n = n || [] } return n && (c = +c || +u || 0, i = n[1] ? c + (n[1] + 1) * n[2] : +n[2], r && (r.unit = l, r.start = c, r.end = i)), i } var ne = {}; function re(e, t) { for (var n, r, i, o, a, s, u, l = [], c = 0, f = e.length; c < f; c++)(r = e[c]).style && (n = r.style.display, t ? ("none" === n && (l[c] = _.get(r, "display") || null, l[c] || (r.style.display = "")), "" === r.style.display && ee(r) && (l[c] = (u = a = o = void 0, a = (i = r).ownerDocument, s = i.nodeName, (u = ne[s]) || (o = a.body.appendChild(a.createElement(s)), u = ce.css(o, "display"), o.parentNode.removeChild(o), "none" === u && (u = "block"), ne[s] = u)))) : "none" !== n && (l[c] = "none", _.set(r, "display", n))); for (c = 0; c < f; c++)null != l[c] && (e[c].style.display = l[c]); return e } ce.fn.extend({ show: function () { return re(this, !0) }, hide: function () { return re(this) }, toggle: function (e) { return "boolean" == typeof e ? e ? this.show() : this.hide() : this.each(function () { ee(this) ? ce(this).show() : ce(this).hide() }) } }); var xe, be, we = /^(?:checkbox|radio)$/i, Te = /<([a-z][^\/\0>\x20\t\r\n\f]*)/i, Ce = /^$|^module$|\/(?:java|ecma)script/i; xe = C.createDocumentFragment().appendChild(C.createElement("div")), (be = C.createElement("input")).setAttribute("type", "radio"), be.setAttribute("checked", "checked"), be.setAttribute("name", "t"), xe.appendChild(be), le.checkClone = xe.cloneNode(!0).cloneNode(!0).lastChild.checked, xe.innerHTML = "<textarea>x</textarea>", le.noCloneChecked = !!xe.cloneNode(!0).lastChild.defaultValue, xe.innerHTML = "<option></option>", le.option = !!xe.lastChild; var ke = { thead: [1, "<table>", "</table>"], col: [2, "<table><colgroup>", "</colgroup></table>"], tr: [2, "<table><tbody>", "</tbody></table>"], td: [3, "<table><tbody><tr>", "</tr></tbody></table>"], _default: [0, "", ""] }; function Se(e, t) { var n; return n = "undefined" != typeof e.getElementsByTagName ? e.getElementsByTagName(t || "*") : "undefined" != typeof e.querySelectorAll ? e.querySelectorAll(t || "*") : [], void 0 === t || t && fe(e, t) ? ce.merge([e], n) : n } function Ee(e, t) { for (var n = 0, r = e.length; n < r; n++)_.set(e[n], "globalEval", !t || _.get(t[n], "globalEval")) } ke.tbody = ke.tfoot = ke.colgroup = ke.caption = ke.thead, ke.th = ke.td, le.option || (ke.optgroup = ke.option = [1, "<select multiple='multiple'>", "</select>"]); var je = /<|&#?\w+;/; function Ae(e, t, n, r, i) { for (var o, a, s, u, l, c, f = t.createDocumentFragment(), p = [], d = 0, h = e.length; d < h; d++)if ((o = e[d]) || 0 === o) if ("object" === x(o)) ce.merge(p, o.nodeType ? [o] : o); else if (je.test(o)) { a = a || f.appendChild(t.createElement("div")), s = (Te.exec(o) || ["", ""])[1].toLowerCase(), u = ke[s] || ke._default, a.innerHTML = u[1] + ce.htmlPrefilter(o) + u[2], c = u[0]; while (c--) a = a.lastChild; ce.merge(p, a.childNodes), (a = f.firstChild).textContent = "" } else p.push(t.createTextNode(o)); f.textContent = "", d = 0; while (o = p[d++]) if (r && -1 < ce.inArray(o, r)) i && i.push(o); else if (l = K(o), a = Se(f.appendChild(o), "script"), l && Ee(a), n) { c = 0; while (o = a[c++]) Ce.test(o.type || "") && n.push(o) } return f } var De = /^([^.]*)(?:\.(.+)|)/; function Ne() { return !0 } function qe() { return !1 } function Le(e, t, n, r, i, o) { var a, s; if ("object" == typeof t) { for (s in "string" != typeof n && (r = r || n, n = void 0), t) Le(e, s, n, r, t[s], o); return e } if (null == r && null == i ? (i = n, r = n = void 0) : null == i && ("string" == typeof n ? (i = r, r = void 0) : (i = r, r = n, n = void 0)), !1 === i) i = qe; else if (!i) return e; return 1 === o && (a = i, (i = function (e) { return ce().off(e), a.apply(this, arguments) }).guid = a.guid || (a.guid = ce.guid++)), e.each(function () { ce.event.add(this, t, i, r, n) }) } function He(e, r, t) { t ? (_.set(e, r, !1), ce.event.add(e, r, { namespace: !1, handler: function (e) { var t, n = _.get(this, r); if (1 & e.isTrigger && this[r]) { if (n) (ce.event.special[r] || {}).delegateType && e.stopPropagation(); else if (n = ae.call(arguments), _.set(this, r, n), this[r](), t = _.get(this, r), _.set(this, r, !1), n !== t) return e.stopImmediatePropagation(), e.preventDefault(), t } else n && (_.set(this, r, ce.event.trigger(n[0], n.slice(1), this)), e.stopPropagation(), e.isImmediatePropagationStopped = Ne) } })) : void 0 === _.get(e, r) && ce.event.add(e, r, Ne) } ce.event = { global: {}, add: function (t, e, n, r, i) { var o, a, s, u, l, c, f, p, d, h, g, v = _.get(t); if ($(t)) { n.handler && (n = (o = n).handler, i = o.selector), i && ce.find.matchesSelector(J, i), n.guid || (n.guid = ce.guid++), (u = v.events) || (u = v.events = Object.create(null)), (a = v.handle) || (a = v.handle = function (e) { return "undefined" != typeof ce && ce.event.triggered !== e.type ? ce.event.dispatch.apply(t, arguments) : void 0 }), l = (e = (e || "").match(D) || [""]).length; while (l--) d = g = (s = De.exec(e[l]) || [])[1], h = (s[2] || "").split(".").sort(), d && (f = ce.event.special[d] || {}, d = (i ? f.delegateType : f.bindType) || d, f = ce.event.special[d] || {}, c = ce.extend({ type: d, origType: g, data: r, handler: n, guid: n.guid, selector: i, needsContext: i && ce.expr.match.needsContext.test(i), namespace: h.join(".") }, o), (p = u[d]) || ((p = u[d] = []).delegateCount = 0, f.setup && !1 !== f.setup.call(t, r, h, a) || t.addEventListener && t.addEventListener(d, a)), f.add && (f.add.call(t, c), c.handler.guid || (c.handler.guid = n.guid)), i ? p.splice(p.delegateCount++, 0, c) : p.push(c), ce.event.global[d] = !0) } }, remove: function (e, t, n, r, i) { var o, a, s, u, l, c, f, p, d, h, g, v = _.hasData(e) && _.get(e); if (v && (u = v.events)) { l = (t = (t || "").match(D) || [""]).length; while (l--) if (d = g = (s = De.exec(t[l]) || [])[1], h = (s[2] || "").split(".").sort(), d) { f = ce.event.special[d] || {}, p = u[d = (r ? f.delegateType : f.bindType) || d] || [], s = s[2] && new RegExp("(^|\\.)" + h.join("\\.(?:.*\\.|)") + "(\\.|$)"), a = o = p.length; while (o--) c = p[o], !i && g !== c.origType || n && n.guid !== c.guid || s && !s.test(c.namespace) || r && r !== c.selector && ("**" !== r || !c.selector) || (p.splice(o, 1), c.selector && p.delegateCount--, f.remove && f.remove.call(e, c)); a && !p.length && (f.teardown && !1 !== f.teardown.call(e, h, v.handle) || ce.removeEvent(e, d, v.handle), delete u[d]) } else for (d in u) ce.event.remove(e, d + t[l], n, r, !0); ce.isEmptyObject(u) && _.remove(e, "handle events") } }, dispatch: function (e) { var t, n, r, i, o, a, s = new Array(arguments.length), u = ce.event.fix(e), l = (_.get(this, "events") || Object.create(null))[u.type] || [], c = ce.event.special[u.type] || {}; for (s[0] = u, t = 1; t < arguments.length; t++)s[t] = arguments[t]; if (u.delegateTarget = this, !c.preDispatch || !1 !== c.preDispatch.call(this, u)) { a = ce.event.handlers.call(this, u, l), t = 0; while ((i = a[t++]) && !u.isPropagationStopped()) { u.currentTarget = i.elem, n = 0; while ((o = i.handlers[n++]) && !u.isImmediatePropagationStopped()) u.rnamespace && !1 !== o.namespace && !u.rnamespace.test(o.namespace) || (u.handleObj = o, u.data = o.data, void 0 !== (r = ((ce.event.special[o.origType] || {}).handle || o.handler).apply(i.elem, s)) && !1 === (u.result = r) && (u.preventDefault(), u.stopPropagation())) } return c.postDispatch && c.postDispatch.call(this, u), u.result } }, handlers: function (e, t) { var n, r, i, o, a, s = [], u = t.delegateCount, l = e.target; if (u && l.nodeType && !("click" === e.type && 1 <= e.button)) for (; l !== this; l = l.parentNode || this)if (1 === l.nodeType && ("click" !== e.type || !0 !== l.disabled)) { for (o = [], a = {}, n = 0; n < u; n++)void 0 === a[i = (r = t[n]).selector + " "] && (a[i] = r.needsContext ? -1 < ce(i, this).index(l) : ce.find(i, this, null, [l]).length), a[i] && o.push(r); o.length && s.push({ elem: l, handlers: o }) } return l = this, u < t.length && s.push({ elem: l, handlers: t.slice(u) }), s }, addProp: function (t, e) { Object.defineProperty(ce.Event.prototype, t, { enumerable: !0, configurable: !0, get: v(e) ? function () { if (this.originalEvent) return e(this.originalEvent) } : function () { if (this.originalEvent) return this.originalEvent[t] }, set: function (e) { Object.defineProperty(this, t, { enumerable: !0, configurable: !0, writable: !0, value: e }) } }) }, fix: function (e) { return e[ce.expando] ? e : new ce.Event(e) }, special: { load: { noBubble: !0 }, click: { setup: function (e) { var t = this || e; return we.test(t.type) && t.click && fe(t, "input") && He(t, "click", !0), !1 }, trigger: function (e) { var t = this || e; return we.test(t.type) && t.click && fe(t, "input") && He(t, "click"), !0 }, _default: function (e) { var t = e.target; return we.test(t.type) && t.click && fe(t, "input") && _.get(t, "click") || fe(t, "a") } }, beforeunload: { postDispatch: function (e) { void 0 !== e.result && e.originalEvent && (e.originalEvent.returnValue = e.result) } } } }, ce.removeEvent = function (e, t, n) { e.removeEventListener && e.removeEventListener(t, n) }, ce.Event = function (e, t) { if (!(this instanceof ce.Event)) return new ce.Event(e, t); e && e.type ? (this.originalEvent = e, this.type = e.type, this.isDefaultPrevented = e.defaultPrevented || void 0 === e.defaultPrevented && !1 === e.returnValue ? Ne : qe, this.target = e.target && 3 === e.target.nodeType ? e.target.parentNode : e.target, this.currentTarget = e.currentTarget, this.relatedTarget = e.relatedTarget) : this.type = e, t && ce.extend(this, t), this.timeStamp = e && e.timeStamp || Date.now(), this[ce.expando] = !0 }, ce.Event.prototype = { constructor: ce.Event, isDefaultPrevented: qe, isPropagationStopped: qe, isImmediatePropagationStopped: qe, isSimulated: !1, preventDefault: function () { var e = this.originalEvent; this.isDefaultPrevented = Ne, e && !this.isSimulated && e.preventDefault() }, stopPropagation: function () { var e = this.originalEvent; this.isPropagationStopped = Ne, e && !this.isSimulated && e.stopPropagation() }, stopImmediatePropagation: function () { var e = this.originalEvent; this.isImmediatePropagationStopped = Ne, e && !this.isSimulated && e.stopImmediatePropagation(), this.stopPropagation() } }, ce.each({ altKey: !0, bubbles: !0, cancelable: !0, changedTouches: !0, ctrlKey: !0, detail: !0, eventPhase: !0, metaKey: !0, pageX: !0, pageY: !0, shiftKey: !0, view: !0, "char": !0, code: !0, charCode: !0, key: !0, keyCode: !0, button: !0, buttons: !0, clientX: !0, clientY: !0, offsetX: !0, offsetY: !0, pointerId: !0, pointerType: !0, screenX: !0, screenY: !0, targetTouches: !0, toElement: !0, touches: !0, which: !0 }, ce.event.addProp), ce.each({ focus: "focusin", blur: "focusout" }, function (r, i) { function o(e) { if (C.documentMode) { var t = _.get(this, "handle"), n = ce.event.fix(e); n.type = "focusin" === e.type ? "focus" : "blur", n.isSimulated = !0, t(e), n.target === n.currentTarget && t(n) } else ce.event.simulate(i, e.target, ce.event.fix(e)) } ce.event.special[r] = { setup: function () { var e; if (He(this, r, !0), !C.documentMode) return !1; (e = _.get(this, i)) || this.addEventListener(i, o), _.set(this, i, (e || 0) + 1) }, trigger: function () { return He(this, r), !0 }, teardown: function () { var e; if (!C.documentMode) return !1; (e = _.get(this, i) - 1) ? _.set(this, i, e) : (this.removeEventListener(i, o), _.remove(this, i)) }, _default: function (e) { return _.get(e.target, r) }, delegateType: i }, ce.event.special[i] = { setup: function () { var e = this.ownerDocument || this.document || this, t = C.documentMode ? this : e, n = _.get(t, i); n || (C.documentMode ? this.addEventListener(i, o) : e.addEventListener(r, o, !0)), _.set(t, i, (n || 0) + 1) }, teardown: function () { var e = this.ownerDocument || this.document || this, t = C.documentMode ? this : e, n = _.get(t, i) - 1; n ? _.set(t, i, n) : (C.documentMode ? this.removeEventListener(i, o) : e.removeEventListener(r, o, !0), _.remove(t, i)) } } }), ce.each({ mouseenter: "mouseover", mouseleave: "mouseout", pointerenter: "pointerover", pointerleave: "pointerout" }, function (e, i) { ce.event.special[e] = { delegateType: i, bindType: i, handle: function (e) { var t, n = e.relatedTarget, r = e.handleObj; return n && (n === this || ce.contains(this, n)) || (e.type = r.origType, t = r.handler.apply(this, arguments), e.type = i), t } } }), ce.fn.extend({ on: function (e, t, n, r) { return Le(this, e, t, n, r) }, one: function (e, t, n, r) { return Le(this, e, t, n, r, 1) }, off: function (e, t, n) { var r, i; if (e && e.preventDefault && e.handleObj) return r = e.handleObj, ce(e.delegateTarget).off(r.namespace ? r.origType + "." + r.namespace : r.origType, r.selector, r.handler), this; if ("object" == typeof e) { for (i in e) this.off(i, t, e[i]); return this } return !1 !== t && "function" != typeof t || (n = t, t = void 0), !1 === n && (n = qe), this.each(function () { ce.event.remove(this, e, n, t) }) } }); var Oe = /<script|<style|<link/i, Pe = /checked\s*(?:[^=]|=\s*.checked.)/i, Me = /^\s*<!\[CDATA\[|\]\]>\s*$/g; function Re(e, t) { return fe(e, "table") && fe(11 !== t.nodeType ? t : t.firstChild, "tr") && ce(e).children("tbody")[0] || e } function Ie(e) { return e.type = (null !== e.getAttribute("type")) + "/" + e.type, e } function We(e) { return "true/" === (e.type || "").slice(0, 5) ? e.type = e.type.slice(5) : e.removeAttribute("type"), e } function Fe(e, t) { var n, r, i, o, a, s; if (1 === t.nodeType) { if (_.hasData(e) && (s = _.get(e).events)) for (i in _.remove(t, "handle events"), s) for (n = 0, r = s[i].length; n < r; n++)ce.event.add(t, i, s[i][n]); z.hasData(e) && (o = z.access(e), a = ce.extend({}, o), z.set(t, a)) } } function $e(n, r, i, o) { r = g(r); var e, t, a, s, u, l, c = 0, f = n.length, p = f - 1, d = r[0], h = v(d); if (h || 1 < f && "string" == typeof d && !le.checkClone && Pe.test(d)) return n.each(function (e) { var t = n.eq(e); h && (r[0] = d.call(this, e, t.html())), $e(t, r, i, o) }); if (f && (t = (e = Ae(r, n[0].ownerDocument, !1, n, o)).firstChild, 1 === e.childNodes.length && (e = t), t || o)) { for (s = (a = ce.map(Se(e, "script"), Ie)).length; c < f; c++)u = e, c !== p && (u = ce.clone(u, !0, !0), s && ce.merge(a, Se(u, "script"))), i.call(n[c], u, c); if (s) for (l = a[a.length - 1].ownerDocument, ce.map(a, We), c = 0; c < s; c++)u = a[c], Ce.test(u.type || "") && !_.access(u, "globalEval") && ce.contains(l, u) && (u.src && "module" !== (u.type || "").toLowerCase() ? ce._evalUrl && !u.noModule && ce._evalUrl(u.src, { nonce: u.nonce || u.getAttribute("nonce") }, l) : m(u.textContent.replace(Me, ""), u, l)) } return n } function Be(e, t, n) { for (var r, i = t ? ce.filter(t, e) : e, o = 0; null != (r = i[o]); o++)n || 1 !== r.nodeType || ce.cleanData(Se(r)), r.parentNode && (n && K(r) && Ee(Se(r, "script")), r.parentNode.removeChild(r)); return e } ce.extend({ htmlPrefilter: function (e) { return e }, clone: function (e, t, n) { var r, i, o, a, s, u, l, c = e.cloneNode(!0), f = K(e); if (!(le.noCloneChecked || 1 !== e.nodeType && 11 !== e.nodeType || ce.isXMLDoc(e))) for (a = Se(c), r = 0, i = (o = Se(e)).length; r < i; r++)s = o[r], u = a[r], void 0, "input" === (l = u.nodeName.toLowerCase()) && we.test(s.type) ? u.checked = s.checked : "input" !== l && "textarea" !== l || (u.defaultValue = s.defaultValue); if (t) if (n) for (o = o || Se(e), a = a || Se(c), r = 0, i = o.length; r < i; r++)Fe(o[r], a[r]); else Fe(e, c); return 0 < (a = Se(c, "script")).length && Ee(a, !f && Se(e, "script")), c }, cleanData: function (e) { for (var t, n, r, i = ce.event.special, o = 0; void 0 !== (n = e[o]); o++)if ($(n)) { if (t = n[_.expando]) { if (t.events) for (r in t.events) i[r] ? ce.event.remove(n, r) : ce.removeEvent(n, r, t.handle); n[_.expando] = void 0 } n[z.expando] && (n[z.expando] = void 0) } } }), ce.fn.extend({ detach: function (e) { return Be(this, e, !0) }, remove: function (e) { return Be(this, e) }, text: function (e) { return M(this, function (e) { return void 0 === e ? ce.text(this) : this.empty().each(function () { 1 !== this.nodeType && 11 !== this.nodeType && 9 !== this.nodeType || (this.textContent = e) }) }, null, e, arguments.length) }, append: function () { return $e(this, arguments, function (e) { 1 !== this.nodeType && 11 !== this.nodeType && 9 !== this.nodeType || Re(this, e).appendChild(e) }) }, prepend: function () { return $e(this, arguments, function (e) { if (1 === this.nodeType || 11 === this.nodeType || 9 === this.nodeType) { var t = Re(this, e); t.insertBefore(e, t.firstChild) } }) }, before: function () { return $e(this, arguments, function (e) { this.parentNode && this.parentNode.insertBefore(e, this) }) }, after: function () { return $e(this, arguments, function (e) { this.parentNode && this.parentNode.insertBefore(e, this.nextSibling) }) }, empty: function () { for (var e, t = 0; null != (e = this[t]); t++)1 === e.nodeType && (ce.cleanData(Se(e, !1)), e.textContent = ""); return this }, clone: function (e, t) { return e = null != e && e, t = null == t ? e : t, this.map(function () { return ce.clone(this, e, t) }) }, html: function (e) { return M(this, function (e) { var t = this[0] || {}, n = 0, r = this.length; if (void 0 === e && 1 === t.nodeType) return t.innerHTML; if ("string" == typeof e && !Oe.test(e) && !ke[(Te.exec(e) || ["", ""])[1].toLowerCase()]) { e = ce.htmlPrefilter(e); try { for (; n < r; n++)1 === (t = this[n] || {}).nodeType && (ce.cleanData(Se(t, !1)), t.innerHTML = e); t = 0 } catch (e) { } } t && this.empty().append(e) }, null, e, arguments.length) }, replaceWith: function () { var n = []; return $e(this, arguments, function (e) { var t = this.parentNode; ce.inArray(this, n) < 0 && (ce.cleanData(Se(this)), t && t.replaceChild(e, this)) }, n) } }), ce.each({ appendTo: "append", prependTo: "prepend", insertBefore: "before", insertAfter: "after", replaceAll: "replaceWith" }, function (e, a) { ce.fn[e] = function (e) { for (var t, n = [], r = ce(e), i = r.length - 1, o = 0; o <= i; o++)t = o === i ? this : this.clone(!0), ce(r[o])[a](t), s.apply(n, t.get()); return this.pushStack(n) } }); var _e = new RegExp("^(" + G + ")(?!px)[a-z%]+$", "i"), ze = /^--/, Xe = function (e) { var t = e.ownerDocument.defaultView; return t && t.opener || (t = ie), t.getComputedStyle(e) }, Ue = function (e, t, n) { var r, i, o = {}; for (i in t) o[i] = e.style[i], e.style[i] = t[i]; for (i in r = n.call(e), t) e.style[i] = o[i]; return r }, Ve = new RegExp(Q.join("|"), "i"); function Ge(e, t, n) { var r, i, o, a, s = ze.test(t), u = e.style; return (n = n || Xe(e)) && (a = n.getPropertyValue(t) || n[t], s && a && (a = a.replace(ve, "$1") || void 0), "" !== a || K(e) || (a = ce.style(e, t)), !le.pixelBoxStyles() && _e.test(a) && Ve.test(t) && (r = u.width, i = u.minWidth, o = u.maxWidth, u.minWidth = u.maxWidth = u.width = a, a = n.width, u.width = r, u.minWidth = i, u.maxWidth = o)), void 0 !== a ? a + "" : a } function Ye(e, t) { return { get: function () { if (!e()) return (this.get = t).apply(this, arguments); delete this.get } } } !function () { function e() { if (l) { u.style.cssText = "position:absolute;left:-11111px;width:60px;margin-top:1px;padding:0;border:0", l.style.cssText = "position:relative;display:block;box-sizing:border-box;overflow:scroll;margin:auto;border:1px;padding:1px;width:60%;top:1%", J.appendChild(u).appendChild(l); var e = ie.getComputedStyle(l); n = "1%" !== e.top, s = 12 === t(e.marginLeft), l.style.right = "60%", o = 36 === t(e.right), r = 36 === t(e.width), l.style.position = "absolute", i = 12 === t(l.offsetWidth / 3), J.removeChild(u), l = null } } function t(e) { return Math.round(parseFloat(e)) } var n, r, i, o, a, s, u = C.createElement("div"), l = C.createElement("div"); l.style && (l.style.backgroundClip = "content-box", l.cloneNode(!0).style.backgroundClip = "", le.clearCloneStyle = "content-box" === l.style.backgroundClip, ce.extend(le, { boxSizingReliable: function () { return e(), r }, pixelBoxStyles: function () { return e(), o }, pixelPosition: function () { return e(), n }, reliableMarginLeft: function () { return e(), s }, scrollboxSize: function () { return e(), i }, reliableTrDimensions: function () { var e, t, n, r; return null == a && (e = C.createElement("table"), t = C.createElement("tr"), n = C.createElement("div"), e.style.cssText = "position:absolute;left:-11111px;border-collapse:separate", t.style.cssText = "box-sizing:content-box;border:1px solid", t.style.height = "1px", n.style.height = "9px", n.style.display = "block", J.appendChild(e).appendChild(t).appendChild(n), r = ie.getComputedStyle(t), a = parseInt(r.height, 10) + parseInt(r.borderTopWidth, 10) + parseInt(r.borderBottomWidth, 10) === t.offsetHeight, J.removeChild(e)), a } })) }(); var Qe = ["Webkit", "Moz", "ms"], Je = C.createElement("div").style, Ke = {}; function Ze(e) { var t = ce.cssProps[e] || Ke[e]; return t || (e in Je ? e : Ke[e] = function (e) { var t = e[0].toUpperCase() + e.slice(1), n = Qe.length; while (n--) if ((e = Qe[n] + t) in Je) return e }(e) || e) } var et = /^(none|table(?!-c[ea]).+)/, tt = { position: "absolute", visibility: "hidden", display: "block" }, nt = { letterSpacing: "0", fontWeight: "400" }; function rt(e, t, n) { var r = Y.exec(t); return r ? Math.max(0, r[2] - (n || 0)) + (r[3] || "px") : t } function it(e, t, n, r, i, o) { var a = "width" === t ? 1 : 0, s = 0, u = 0, l = 0; if (n === (r ? "border" : "content")) return 0; for (; a < 4; a += 2)"margin" === n && (l += ce.css(e, n + Q[a], !0, i)), r ? ("content" === n && (u -= ce.css(e, "padding" + Q[a], !0, i)), "margin" !== n && (u -= ce.css(e, "border" + Q[a] + "Width", !0, i))) : (u += ce.css(e, "padding" + Q[a], !0, i), "padding" !== n ? u += ce.css(e, "border" + Q[a] + "Width", !0, i) : s += ce.css(e, "border" + Q[a] + "Width", !0, i)); return !r && 0 <= o && (u += Math.max(0, Math.ceil(e["offset" + t[0].toUpperCase() + t.slice(1)] - o - u - s - .5)) || 0), u + l } function ot(e, t, n) { var r = Xe(e), i = (!le.boxSizingReliable() || n) && "border-box" === ce.css(e, "boxSizing", !1, r), o = i, a = Ge(e, t, r), s = "offset" + t[0].toUpperCase() + t.slice(1); if (_e.test(a)) { if (!n) return a; a = "auto" } return (!le.boxSizingReliable() && i || !le.reliableTrDimensions() && fe(e, "tr") || "auto" === a || !parseFloat(a) && "inline" === ce.css(e, "display", !1, r)) && e.getClientRects().length && (i = "border-box" === ce.css(e, "boxSizing", !1, r), (o = s in e) && (a = e[s])), (a = parseFloat(a) || 0) + it(e, t, n || (i ? "border" : "content"), o, r, a) + "px" } function at(e, t, n, r, i) { return new at.prototype.init(e, t, n, r, i) } ce.extend({ cssHooks: { opacity: { get: function (e, t) { if (t) { var n = Ge(e, "opacity"); return "" === n ? "1" : n } } } }, cssNumber: { animationIterationCount: !0, aspectRatio: !0, borderImageSlice: !0, columnCount: !0, flexGrow: !0, flexShrink: !0, fontWeight: !0, gridArea: !0, gridColumn: !0, gridColumnEnd: !0, gridColumnStart: !0, gridRow: !0, gridRowEnd: !0, gridRowStart: !0, lineHeight: !0, opacity: !0, order: !0, orphans: !0, scale: !0, widows: !0, zIndex: !0, zoom: !0, fillOpacity: !0, floodOpacity: !0, stopOpacity: !0, strokeMiterlimit: !0, strokeOpacity: !0 }, cssProps: {}, style: function (e, t, n, r) { if (e && 3 !== e.nodeType && 8 !== e.nodeType && e.style) { var i, o, a, s = F(t), u = ze.test(t), l = e.style; if (u || (t = Ze(s)), a = ce.cssHooks[t] || ce.cssHooks[s], void 0 === n) return a && "get" in a && void 0 !== (i = a.get(e, !1, r)) ? i : l[t]; "string" === (o = typeof n) && (i = Y.exec(n)) && i[1] && (n = te(e, t, i), o = "number"), null != n && n == n && ("number" !== o || u || (n += i && i[3] || (ce.cssNumber[s] ? "" : "px")), le.clearCloneStyle || "" !== n || 0 !== t.indexOf("background") || (l[t] = "inherit"), a && "set" in a && void 0 === (n = a.set(e, n, r)) || (u ? l.setProperty(t, n) : l[t] = n)) } }, css: function (e, t, n, r) { var i, o, a, s = F(t); return ze.test(t) || (t = Ze(s)), (a = ce.cssHooks[t] || ce.cssHooks[s]) && "get" in a && (i = a.get(e, !0, n)), void 0 === i && (i = Ge(e, t, r)), "normal" === i && t in nt && (i = nt[t]), "" === n || n ? (o = parseFloat(i), !0 === n || isFinite(o) ? o || 0 : i) : i } }), ce.each(["height", "width"], function (e, u) { ce.cssHooks[u] = { get: function (e, t, n) { if (t) return !et.test(ce.css(e, "display")) || e.getClientRects().length && e.getBoundingClientRect().width ? ot(e, u, n) : Ue(e, tt, function () { return ot(e, u, n) }) }, set: function (e, t, n) { var r, i = Xe(e), o = !le.scrollboxSize() && "absolute" === i.position, a = (o || n) && "border-box" === ce.css(e, "boxSizing", !1, i), s = n ? it(e, u, n, a, i) : 0; return a && o && (s -= Math.ceil(e["offset" + u[0].toUpperCase() + u.slice(1)] - parseFloat(i[u]) - it(e, u, "border", !1, i) - .5)), s && (r = Y.exec(t)) && "px" !== (r[3] || "px") && (e.style[u] = t, t = ce.css(e, u)), rt(0, t, s) } } }), ce.cssHooks.marginLeft = Ye(le.reliableMarginLeft, function (e, t) { if (t) return (parseFloat(Ge(e, "marginLeft")) || e.getBoundingClientRect().left - Ue(e, { marginLeft: 0 }, function () { return e.getBoundingClientRect().left })) + "px" }), ce.each({ margin: "", padding: "", border: "Width" }, function (i, o) { ce.cssHooks[i + o] = { expand: function (e) { for (var t = 0, n = {}, r = "string" == typeof e ? e.split(" ") : [e]; t < 4; t++)n[i + Q[t] + o] = r[t] || r[t - 2] || r[0]; return n } }, "margin" !== i && (ce.cssHooks[i + o].set = rt) }), ce.fn.extend({ css: function (e, t) { return M(this, function (e, t, n) { var r, i, o = {}, a = 0; if (Array.isArray(t)) { for (r = Xe(e), i = t.length; a < i; a++)o[t[a]] = ce.css(e, t[a], !1, r); return o } return void 0 !== n ? ce.style(e, t, n) : ce.css(e, t) }, e, t, 1 < arguments.length) } }), ((ce.Tween = at).prototype = { constructor: at, init: function (e, t, n, r, i, o) { this.elem = e, this.prop = n, this.easing = i || ce.easing._default, this.options = t, this.start = this.now = this.cur(), this.end = r, this.unit = o || (ce.cssNumber[n] ? "" : "px") }, cur: function () { var e = at.propHooks[this.prop]; return e && e.get ? e.get(this) : at.propHooks._default.get(this) }, run: function (e) { var t, n = at.propHooks[this.prop]; return this.options.duration ? this.pos = t = ce.easing[this.easing](e, this.options.duration * e, 0, 1, this.options.duration) : this.pos = t = e, this.now = (this.end - this.start) * t + this.start, this.options.step && this.options.step.call(this.elem, this.now, this), n && n.set ? n.set(this) : at.propHooks._default.set(this), this } }).init.prototype = at.prototype, (at.propHooks = { _default: { get: function (e) { var t; return 1 !== e.elem.nodeType || null != e.elem[e.prop] && null == e.elem.style[e.prop] ? e.elem[e.prop] : (t = ce.css(e.elem, e.prop, "")) && "auto" !== t ? t : 0 }, set: function (e) { ce.fx.step[e.prop] ? ce.fx.step[e.prop](e) : 1 !== e.elem.nodeType || !ce.cssHooks[e.prop] && null == e.elem.style[Ze(e.prop)] ? e.elem[e.prop] = e.now : ce.style(e.elem, e.prop, e.now + e.unit) } } }).scrollTop = at.propHooks.scrollLeft = { set: function (e) { e.elem.nodeType && e.elem.parentNode && (e.elem[e.prop] = e.now) } }, ce.easing = { linear: function (e) { return e }, swing: function (e) { return .5 - Math.cos(e * Math.PI) / 2 }, _default: "swing" }, ce.fx = at.prototype.init, ce.fx.step = {}; var st, ut, lt, ct, ft = /^(?:toggle|show|hide)$/, pt = /queueHooks$/; function dt() { ut && (!1 === C.hidden && ie.requestAnimationFrame ? ie.requestAnimationFrame(dt) : ie.setTimeout(dt, ce.fx.interval), ce.fx.tick()) } function ht() { return ie.setTimeout(function () { st = void 0 }), st = Date.now() } function gt(e, t) { var n, r = 0, i = { height: e }; for (t = t ? 1 : 0; r < 4; r += 2 - t)i["margin" + (n = Q[r])] = i["padding" + n] = e; return t && (i.opacity = i.width = e), i } function vt(e, t, n) { for (var r, i = (yt.tweeners[t] || []).concat(yt.tweeners["*"]), o = 0, a = i.length; o < a; o++)if (r = i[o].call(n, t, e)) return r } function yt(o, e, t) { var n, a, r = 0, i = yt.prefilters.length, s = ce.Deferred().always(function () { delete u.elem }), u = function () { if (a) return !1; for (var e = st || ht(), t = Math.max(0, l.startTime + l.duration - e), n = 1 - (t / l.duration || 0), r = 0, i = l.tweens.length; r < i; r++)l.tweens[r].run(n); return s.notifyWith(o, [l, n, t]), n < 1 && i ? t : (i || s.notifyWith(o, [l, 1, 0]), s.resolveWith(o, [l]), !1) }, l = s.promise({ elem: o, props: ce.extend({}, e), opts: ce.extend(!0, { specialEasing: {}, easing: ce.easing._default }, t), originalProperties: e, originalOptions: t, startTime: st || ht(), duration: t.duration, tweens: [], createTween: function (e, t) { var n = ce.Tween(o, l.opts, e, t, l.opts.specialEasing[e] || l.opts.easing); return l.tweens.push(n), n }, stop: function (e) { var t = 0, n = e ? l.tweens.length : 0; if (a) return this; for (a = !0; t < n; t++)l.tweens[t].run(1); return e ? (s.notifyWith(o, [l, 1, 0]), s.resolveWith(o, [l, e])) : s.rejectWith(o, [l, e]), this } }), c = l.props; for (!function (e, t) { var n, r, i, o, a; for (n in e) if (i = t[r = F(n)], o = e[n], Array.isArray(o) && (i = o[1], o = e[n] = o[0]), n !== r && (e[r] = o, delete e[n]), (a = ce.cssHooks[r]) && "expand" in a) for (n in o = a.expand(o), delete e[r], o) n in e || (e[n] = o[n], t[n] = i); else t[r] = i }(c, l.opts.specialEasing); r < i; r++)if (n = yt.prefilters[r].call(l, o, c, l.opts)) return v(n.stop) && (ce._queueHooks(l.elem, l.opts.queue).stop = n.stop.bind(n)), n; return ce.map(c, vt, l), v(l.opts.start) && l.opts.start.call(o, l), l.progress(l.opts.progress).done(l.opts.done, l.opts.complete).fail(l.opts.fail).always(l.opts.always), ce.fx.timer(ce.extend(u, { elem: o, anim: l, queue: l.opts.queue })), l } ce.Animation = ce.extend(yt, { tweeners: { "*": [function (e, t) { var n = this.createTween(e, t); return te(n.elem, e, Y.exec(t), n), n }] }, tweener: function (e, t) { v(e) ? (t = e, e = ["*"]) : e = e.match(D); for (var n, r = 0, i = e.length; r < i; r++)n = e[r], yt.tweeners[n] = yt.tweeners[n] || [], yt.tweeners[n].unshift(t) }, prefilters: [function (e, t, n) { var r, i, o, a, s, u, l, c, f = "width" in t || "height" in t, p = this, d = {}, h = e.style, g = e.nodeType && ee(e), v = _.get(e, "fxshow"); for (r in n.queue || (null == (a = ce._queueHooks(e, "fx")).unqueued && (a.unqueued = 0, s = a.empty.fire, a.empty.fire = function () { a.unqueued || s() }), a.unqueued++, p.always(function () { p.always(function () { a.unqueued--, ce.queue(e, "fx").length || a.empty.fire() }) })), t) if (i = t[r], ft.test(i)) { if (delete t[r], o = o || "toggle" === i, i === (g ? "hide" : "show")) { if ("show" !== i || !v || void 0 === v[r]) continue; g = !0 } d[r] = v && v[r] || ce.style(e, r) } if ((u = !ce.isEmptyObject(t)) || !ce.isEmptyObject(d)) for (r in f && 1 === e.nodeType && (n.overflow = [h.overflow, h.overflowX, h.overflowY], null == (l = v && v.display) && (l = _.get(e, "display")), "none" === (c = ce.css(e, "display")) && (l ? c = l : (re([e], !0), l = e.style.display || l, c = ce.css(e, "display"), re([e]))), ("inline" === c || "inline-block" === c && null != l) && "none" === ce.css(e, "float") && (u || (p.done(function () { h.display = l }), null == l && (c = h.display, l = "none" === c ? "" : c)), h.display = "inline-block")), n.overflow && (h.overflow = "hidden", p.always(function () { h.overflow = n.overflow[0], h.overflowX = n.overflow[1], h.overflowY = n.overflow[2] })), u = !1, d) u || (v ? "hidden" in v && (g = v.hidden) : v = _.access(e, "fxshow", { display: l }), o && (v.hidden = !g), g && re([e], !0), p.done(function () { for (r in g || re([e]), _.remove(e, "fxshow"), d) ce.style(e, r, d[r]) })), u = vt(g ? v[r] : 0, r, p), r in v || (v[r] = u.start, g && (u.end = u.start, u.start = 0)) }], prefilter: function (e, t) { t ? yt.prefilters.unshift(e) : yt.prefilters.push(e) } }), ce.speed = function (e, t, n) { var r = e && "object" == typeof e ? ce.extend({}, e) : { complete: n || !n && t || v(e) && e, duration: e, easing: n && t || t && !v(t) && t }; return ce.fx.off ? r.duration = 0 : "number" != typeof r.duration && (r.duration in ce.fx.speeds ? r.duration = ce.fx.speeds[r.duration] : r.duration = ce.fx.speeds._default), null != r.queue && !0 !== r.queue || (r.queue = "fx"), r.old = r.complete, r.complete = function () { v(r.old) && r.old.call(this), r.queue && ce.dequeue(this, r.queue) }, r }, ce.fn.extend({ fadeTo: function (e, t, n, r) { return this.filter(ee).css("opacity", 0).show().end().animate({ opacity: t }, e, n, r) }, animate: function (t, e, n, r) { var i = ce.isEmptyObject(t), o = ce.speed(e, n, r), a = function () { var e = yt(this, ce.extend({}, t), o); (i || _.get(this, "finish")) && e.stop(!0) }; return a.finish = a, i || !1 === o.queue ? this.each(a) : this.queue(o.queue, a) }, stop: function (i, e, o) { var a = function (e) { var t = e.stop; delete e.stop, t(o) }; return "string" != typeof i && (o = e, e = i, i = void 0), e && this.queue(i || "fx", []), this.each(function () { var e = !0, t = null != i && i + "queueHooks", n = ce.timers, r = _.get(this); if (t) r[t] && r[t].stop && a(r[t]); else for (t in r) r[t] && r[t].stop && pt.test(t) && a(r[t]); for (t = n.length; t--;)n[t].elem !== this || null != i && n[t].queue !== i || (n[t].anim.stop(o), e = !1, n.splice(t, 1)); !e && o || ce.dequeue(this, i) }) }, finish: function (a) { return !1 !== a && (a = a || "fx"), this.each(function () { var e, t = _.get(this), n = t[a + "queue"], r = t[a + "queueHooks"], i = ce.timers, o = n ? n.length : 0; for (t.finish = !0, ce.queue(this, a, []), r && r.stop && r.stop.call(this, !0), e = i.length; e--;)i[e].elem === this && i[e].queue === a && (i[e].anim.stop(!0), i.splice(e, 1)); for (e = 0; e < o; e++)n[e] && n[e].finish && n[e].finish.call(this); delete t.finish }) } }), ce.each(["toggle", "show", "hide"], function (e, r) { var i = ce.fn[r]; ce.fn[r] = function (e, t, n) { return null == e || "boolean" == typeof e ? i.apply(this, arguments) : this.animate(gt(r, !0), e, t, n) } }), ce.each({ slideDown: gt("show"), slideUp: gt("hide"), slideToggle: gt("toggle"), fadeIn: { opacity: "show" }, fadeOut: { opacity: "hide" }, fadeToggle: { opacity: "toggle" } }, function (e, r) { ce.fn[e] = function (e, t, n) { return this.animate(r, e, t, n) } }), ce.timers = [], ce.fx.tick = function () { var e, t = 0, n = ce.timers; for (st = Date.now(); t < n.length; t++)(e = n[t])() || n[t] !== e || n.splice(t--, 1); n.length || ce.fx.stop(), st = void 0 }, ce.fx.timer = function (e) { ce.timers.push(e), ce.fx.start() }, ce.fx.interval = 13, ce.fx.start = function () { ut || (ut = !0, dt()) }, ce.fx.stop = function () { ut = null }, ce.fx.speeds = { slow: 600, fast: 200, _default: 400 }, ce.fn.delay = function (r, e) { return r = ce.fx && ce.fx.speeds[r] || r, e = e || "fx", this.queue(e, function (e, t) { var n = ie.setTimeout(e, r); t.stop = function () { ie.clearTimeout(n) } }) }, lt = C.createElement("input"), ct = C.createElement("select").appendChild(C.createElement("option")), lt.type = "checkbox", le.checkOn = "" !== lt.value, le.optSelected = ct.selected, (lt = C.createElement("input")).value = "t", lt.type = "radio", le.radioValue = "t" === lt.value; var mt, xt = ce.expr.attrHandle; ce.fn.extend({ attr: function (e, t) { return M(this, ce.attr, e, t, 1 < arguments.length) }, removeAttr: function (e) { return this.each(function () { ce.removeAttr(this, e) }) } }), ce.extend({ attr: function (e, t, n) { var r, i, o = e.nodeType; if (3 !== o && 8 !== o && 2 !== o) return "undefined" == typeof e.getAttribute ? ce.prop(e, t, n) : (1 === o && ce.isXMLDoc(e) || (i = ce.attrHooks[t.toLowerCase()] || (ce.expr.match.bool.test(t) ? mt : void 0)), void 0 !== n ? null === n ? void ce.removeAttr(e, t) : i && "set" in i && void 0 !== (r = i.set(e, n, t)) ? r : (e.setAttribute(t, n + ""), n) : i && "get" in i && null !== (r = i.get(e, t)) ? r : null == (r = ce.find.attr(e, t)) ? void 0 : r) }, attrHooks: { type: { set: function (e, t) { if (!le.radioValue && "radio" === t && fe(e, "input")) { var n = e.value; return e.setAttribute("type", t), n && (e.value = n), t } } } }, removeAttr: function (e, t) { var n, r = 0, i = t && t.match(D); if (i && 1 === e.nodeType) while (n = i[r++]) e.removeAttribute(n) } }), mt = { set: function (e, t, n) { return !1 === t ? ce.removeAttr(e, n) : e.setAttribute(n, n), n } }, ce.each(ce.expr.match.bool.source.match(/\w+/g), function (e, t) { var a = xt[t] || ce.find.attr; xt[t] = function (e, t, n) { var r, i, o = t.toLowerCase(); return n || (i = xt[o], xt[o] = r, r = null != a(e, t, n) ? o : null, xt[o] = i), r } }); var bt = /^(?:input|select|textarea|button)$/i, wt = /^(?:a|area)$/i; function Tt(e) { return (e.match(D) || []).join(" ") } function Ct(e) { return e.getAttribute && e.getAttribute("class") || "" } function kt(e) { return Array.isArray(e) ? e : "string" == typeof e && e.match(D) || [] } ce.fn.extend({ prop: function (e, t) { return M(this, ce.prop, e, t, 1 < arguments.length) }, removeProp: function (e) { return this.each(function () { delete this[ce.propFix[e] || e] }) } }), ce.extend({ prop: function (e, t, n) { var r, i, o = e.nodeType; if (3 !== o && 8 !== o && 2 !== o) return 1 === o && ce.isXMLDoc(e) || (t = ce.propFix[t] || t, i = ce.propHooks[t]), void 0 !== n ? i && "set" in i && void 0 !== (r = i.set(e, n, t)) ? r : e[t] = n : i && "get" in i && null !== (r = i.get(e, t)) ? r : e[t] }, propHooks: { tabIndex: { get: function (e) { var t = ce.find.attr(e, "tabindex"); return t ? parseInt(t, 10) : bt.test(e.nodeName) || wt.test(e.nodeName) && e.href ? 0 : -1 } } }, propFix: { "for": "htmlFor", "class": "className" } }), le.optSelected || (ce.propHooks.selected = { get: function (e) { var t = e.parentNode; return t && t.parentNode && t.parentNode.selectedIndex, null }, set: function (e) { var t = e.parentNode; t && (t.selectedIndex, t.parentNode && t.parentNode.selectedIndex) } }), ce.each(["tabIndex", "readOnly", "maxLength", "cellSpacing", "cellPadding", "rowSpan", "colSpan", "useMap", "frameBorder", "contentEditable"], function () { ce.propFix[this.toLowerCase()] = this }), ce.fn.extend({ addClass: function (t) { var e, n, r, i, o, a; return v(t) ? this.each(function (e) { ce(this).addClass(t.call(this, e, Ct(this))) }) : (e = kt(t)).length ? this.each(function () { if (r = Ct(this), n = 1 === this.nodeType && " " + Tt(r) + " ") { for (o = 0; o < e.length; o++)i = e[o], n.indexOf(" " + i + " ") < 0 && (n += i + " "); a = Tt(n), r !== a && this.setAttribute("class", a) } }) : this }, removeClass: function (t) { var e, n, r, i, o, a; return v(t) ? this.each(function (e) { ce(this).removeClass(t.call(this, e, Ct(this))) }) : arguments.length ? (e = kt(t)).length ? this.each(function () { if (r = Ct(this), n = 1 === this.nodeType && " " + Tt(r) + " ") { for (o = 0; o < e.length; o++) { i = e[o]; while (-1 < n.indexOf(" " + i + " ")) n = n.replace(" " + i + " ", " ") } a = Tt(n), r !== a && this.setAttribute("class", a) } }) : this : this.attr("class", "") }, toggleClass: function (t, n) { var e, r, i, o, a = typeof t, s = "string" === a || Array.isArray(t); return v(t) ? this.each(function (e) { ce(this).toggleClass(t.call(this, e, Ct(this), n), n) }) : "boolean" == typeof n && s ? n ? this.addClass(t) : this.removeClass(t) : (e = kt(t), this.each(function () { if (s) for (o = ce(this), i = 0; i < e.length; i++)r = e[i], o.hasClass(r) ? o.removeClass(r) : o.addClass(r); else void 0 !== t && "boolean" !== a || ((r = Ct(this)) && _.set(this, "__className__", r), this.setAttribute && this.setAttribute("class", r || !1 === t ? "" : _.get(this, "__className__") || "")) })) }, hasClass: function (e) { var t, n, r = 0; t = " " + e + " "; while (n = this[r++]) if (1 === n.nodeType && -1 < (" " + Tt(Ct(n)) + " ").indexOf(t)) return !0; return !1 } }); var St = /\r/g; ce.fn.extend({ val: function (n) { var r, e, i, t = this[0]; return arguments.length ? (i = v(n), this.each(function (e) { var t; 1 === this.nodeType && (null == (t = i ? n.call(this, e, ce(this).val()) : n) ? t = "" : "number" == typeof t ? t += "" : Array.isArray(t) && (t = ce.map(t, function (e) { return null == e ? "" : e + "" })), (r = ce.valHooks[this.type] || ce.valHooks[this.nodeName.toLowerCase()]) && "set" in r && void 0 !== r.set(this, t, "value") || (this.value = t)) })) : t ? (r = ce.valHooks[t.type] || ce.valHooks[t.nodeName.toLowerCase()]) && "get" in r && void 0 !== (e = r.get(t, "value")) ? e : "string" == typeof (e = t.value) ? e.replace(St, "") : null == e ? "" : e : void 0 } }), ce.extend({ valHooks: { option: { get: function (e) { var t = ce.find.attr(e, "value"); return null != t ? t : Tt(ce.text(e)) } }, select: { get: function (e) { var t, n, r, i = e.options, o = e.selectedIndex, a = "select-one" === e.type, s = a ? null : [], u = a ? o + 1 : i.length; for (r = o < 0 ? u : a ? o : 0; r < u; r++)if (((n = i[r]).selected || r === o) && !n.disabled && (!n.parentNode.disabled || !fe(n.parentNode, "optgroup"))) { if (t = ce(n).val(), a) return t; s.push(t) } return s }, set: function (e, t) { var n, r, i = e.options, o = ce.makeArray(t), a = i.length; while (a--) ((r = i[a]).selected = -1 < ce.inArray(ce.valHooks.option.get(r), o)) && (n = !0); return n || (e.selectedIndex = -1), o } } } }), ce.each(["radio", "checkbox"], function () { ce.valHooks[this] = { set: function (e, t) { if (Array.isArray(t)) return e.checked = -1 < ce.inArray(ce(e).val(), t) } }, le.checkOn || (ce.valHooks[this].get = function (e) { return null === e.getAttribute("value") ? "on" : e.value }) }); var Et = ie.location, jt = { guid: Date.now() }, At = /\?/; ce.parseXML = function (e) { var t, n; if (!e || "string" != typeof e) return null; try { t = (new ie.DOMParser).parseFromString(e, "text/xml") } catch (e) { } return n = t && t.getElementsByTagName("parsererror")[0], t && !n || ce.error("Invalid XML: " + (n ? ce.map(n.childNodes, function (e) { return e.textContent }).join("\n") : e)), t }; var Dt = /^(?:focusinfocus|focusoutblur)$/, Nt = function (e) { e.stopPropagation() }; ce.extend(ce.event, { trigger: function (e, t, n, r) { var i, o, a, s, u, l, c, f, p = [n || C], d = ue.call(e, "type") ? e.type : e, h = ue.call(e, "namespace") ? e.namespace.split(".") : []; if (o = f = a = n = n || C, 3 !== n.nodeType && 8 !== n.nodeType && !Dt.test(d + ce.event.triggered) && (-1 < d.indexOf(".") && (d = (h = d.split(".")).shift(), h.sort()), u = d.indexOf(":") < 0 && "on" + d, (e = e[ce.expando] ? e : new ce.Event(d, "object" == typeof e && e)).isTrigger = r ? 2 : 3, e.namespace = h.join("."), e.rnamespace = e.namespace ? new RegExp("(^|\\.)" + h.join("\\.(?:.*\\.|)") + "(\\.|$)") : null, e.result = void 0, e.target || (e.target = n), t = null == t ? [e] : ce.makeArray(t, [e]), c = ce.event.special[d] || {}, r || !c.trigger || !1 !== c.trigger.apply(n, t))) { if (!r && !c.noBubble && !y(n)) { for (s = c.delegateType || d, Dt.test(s + d) || (o = o.parentNode); o; o = o.parentNode)p.push(o), a = o; a === (n.ownerDocument || C) && p.push(a.defaultView || a.parentWindow || ie) } i = 0; while ((o = p[i++]) && !e.isPropagationStopped()) f = o, e.type = 1 < i ? s : c.bindType || d, (l = (_.get(o, "events") || Object.create(null))[e.type] && _.get(o, "handle")) && l.apply(o, t), (l = u && o[u]) && l.apply && $(o) && (e.result = l.apply(o, t), !1 === e.result && e.preventDefault()); return e.type = d, r || e.isDefaultPrevented() || c._default && !1 !== c._default.apply(p.pop(), t) || !$(n) || u && v(n[d]) && !y(n) && ((a = n[u]) && (n[u] = null), ce.event.triggered = d, e.isPropagationStopped() && f.addEventListener(d, Nt), n[d](), e.isPropagationStopped() && f.removeEventListener(d, Nt), ce.event.triggered = void 0, a && (n[u] = a)), e.result } }, simulate: function (e, t, n) { var r = ce.extend(new ce.Event, n, { type: e, isSimulated: !0 }); ce.event.trigger(r, null, t) } }), ce.fn.extend({ trigger: function (e, t) { return this.each(function () { ce.event.trigger(e, t, this) }) }, triggerHandler: function (e, t) { var n = this[0]; if (n) return ce.event.trigger(e, t, n, !0) } }); var qt = /\[\]$/, Lt = /\r?\n/g, Ht = /^(?:submit|button|image|reset|file)$/i, Ot = /^(?:input|select|textarea|keygen)/i; function Pt(n, e, r, i) { var t; if (Array.isArray(e)) ce.each(e, function (e, t) { r || qt.test(n) ? i(n, t) : Pt(n + "[" + ("object" == typeof t && null != t ? e : "") + "]", t, r, i) }); else if (r || "object" !== x(e)) i(n, e); else for (t in e) Pt(n + "[" + t + "]", e[t], r, i) } ce.param = function (e, t) { var n, r = [], i = function (e, t) { var n = v(t) ? t() : t; r[r.length] = encodeURIComponent(e) + "=" + encodeURIComponent(null == n ? "" : n) }; if (null == e) return ""; if (Array.isArray(e) || e.jquery && !ce.isPlainObject(e)) ce.each(e, function () { i(this.name, this.value) }); else for (n in e) Pt(n, e[n], t, i); return r.join("&") }, ce.fn.extend({ serialize: function () { return ce.param(this.serializeArray()) }, serializeArray: function () { return this.map(function () { var e = ce.prop(this, "elements"); return e ? ce.makeArray(e) : this }).filter(function () { var e = this.type; return this.name && !ce(this).is(":disabled") && Ot.test(this.nodeName) && !Ht.test(e) && (this.checked || !we.test(e)) }).map(function (e, t) { var n = ce(this).val(); return null == n ? null : Array.isArray(n) ? ce.map(n, function (e) { return { name: t.name, value: e.replace(Lt, "\r\n") } }) : { name: t.name, value: n.replace(Lt, "\r\n") } }).get() } }); var Mt = /%20/g, Rt = /#.*$/, It = /([?&])_=[^&]*/, Wt = /^(.*?):[ \t]*([^\r\n]*)$/gm, Ft = /^(?:GET|HEAD)$/, $t = /^\/\//, Bt = {}, _t = {}, zt = "*/".concat("*"), Xt = C.createElement("a"); function Ut(o) { return function (e, t) { "string" != typeof e && (t = e, e = "*"); var n, r = 0, i = e.toLowerCase().match(D) || []; if (v(t)) while (n = i[r++]) "+" === n[0] ? (n = n.slice(1) || "*", (o[n] = o[n] || []).unshift(t)) : (o[n] = o[n] || []).push(t) } } function Vt(t, i, o, a) { var s = {}, u = t === _t; function l(e) { var r; return s[e] = !0, ce.each(t[e] || [], function (e, t) { var n = t(i, o, a); return "string" != typeof n || u || s[n] ? u ? !(r = n) : void 0 : (i.dataTypes.unshift(n), l(n), !1) }), r } return l(i.dataTypes[0]) || !s["*"] && l("*") } function Gt(e, t) { var n, r, i = ce.ajaxSettings.flatOptions || {}; for (n in t) void 0 !== t[n] && ((i[n] ? e : r || (r = {}))[n] = t[n]); return r && ce.extend(!0, e, r), e } Xt.href = Et.href, ce.extend({ active: 0, lastModified: {}, etag: {}, ajaxSettings: { url: Et.href, type: "GET", isLocal: /^(?:about|app|app-storage|.+-extension|file|res|widget):$/.test(Et.protocol), global: !0, processData: !0, async: !0, contentType: "application/x-www-form-urlencoded; charset=UTF-8", accepts: { "*": zt, text: "text/plain", html: "text/html", xml: "application/xml, text/xml", json: "application/json, text/javascript" }, contents: { xml: /\bxml\b/, html: /\bhtml/, json: /\bjson\b/ }, responseFields: { xml: "responseXML", text: "responseText", json: "responseJSON" }, converters: { "* text": String, "text html": !0, "text json": JSON.parse, "text xml": ce.parseXML }, flatOptions: { url: !0, context: !0 } }, ajaxSetup: function (e, t) { return t ? Gt(Gt(e, ce.ajaxSettings), t) : Gt(ce.ajaxSettings, e) }, ajaxPrefilter: Ut(Bt), ajaxTransport: Ut(_t), ajax: function (e, t) { "object" == typeof e && (t = e, e = void 0), t = t || {}; var c, f, p, n, d, r, h, g, i, o, v = ce.ajaxSetup({}, t), y = v.context || v, m = v.context && (y.nodeType || y.jquery) ? ce(y) : ce.event, x = ce.Deferred(), b = ce.Callbacks("once memory"), w = v.statusCode || {}, a = {}, s = {}, u = "canceled", T = { readyState: 0, getResponseHeader: function (e) { var t; if (h) { if (!n) { n = {}; while (t = Wt.exec(p)) n[t[1].toLowerCase() + " "] = (n[t[1].toLowerCase() + " "] || []).concat(t[2]) } t = n[e.toLowerCase() + " "] } return null == t ? null : t.join(", ") }, getAllResponseHeaders: function () { return h ? p : null }, setRequestHeader: function (e, t) { return null == h && (e = s[e.toLowerCase()] = s[e.toLowerCase()] || e, a[e] = t), this }, overrideMimeType: function (e) { return null == h && (v.mimeType = e), this }, statusCode: function (e) { var t; if (e) if (h) T.always(e[T.status]); else for (t in e) w[t] = [w[t], e[t]]; return this }, abort: function (e) { var t = e || u; return c && c.abort(t), l(0, t), this } }; if (x.promise(T), v.url = ((e || v.url || Et.href) + "").replace($t, Et.protocol + "//"), v.type = t.method || t.type || v.method || v.type, v.dataTypes = (v.dataType || "*").toLowerCase().match(D) || [""], null == v.crossDomain) { r = C.createElement("a"); try { r.href = v.url, r.href = r.href, v.crossDomain = Xt.protocol + "//" + Xt.host != r.protocol + "//" + r.host } catch (e) { v.crossDomain = !0 } } if (v.data && v.processData && "string" != typeof v.data && (v.data = ce.param(v.data, v.traditional)), Vt(Bt, v, t, T), h) return T; for (i in (g = ce.event && v.global) && 0 == ce.active++ && ce.event.trigger("ajaxStart"), v.type = v.type.toUpperCase(), v.hasContent = !Ft.test(v.type), f = v.url.replace(Rt, ""), v.hasContent ? v.data && v.processData && 0 === (v.contentType || "").indexOf("application/x-www-form-urlencoded") && (v.data = v.data.replace(Mt, "+")) : (o = v.url.slice(f.length), v.data && (v.processData || "string" == typeof v.data) && (f += (At.test(f) ? "&" : "?") + v.data, delete v.data), !1 === v.cache && (f = f.replace(It, "$1"), o = (At.test(f) ? "&" : "?") + "_=" + jt.guid++ + o), v.url = f + o), v.ifModified && (ce.lastModified[f] && T.setRequestHeader("If-Modified-Since", ce.lastModified[f]), ce.etag[f] && T.setRequestHeader("If-None-Match", ce.etag[f])), (v.data && v.hasContent && !1 !== v.contentType || t.contentType) && T.setRequestHeader("Content-Type", v.contentType), T.setRequestHeader("Accept", v.dataTypes[0] && v.accepts[v.dataTypes[0]] ? v.accepts[v.dataTypes[0]] + ("*" !== v.dataTypes[0] ? ", " + zt + "; q=0.01" : "") : v.accepts["*"]), v.headers) T.setRequestHeader(i, v.headers[i]); if (v.beforeSend && (!1 === v.beforeSend.call(y, T, v) || h)) return T.abort(); if (u = "abort", b.add(v.complete), T.done(v.success), T.fail(v.error), c = Vt(_t, v, t, T)) { if (T.readyState = 1, g && m.trigger("ajaxSend", [T, v]), h) return T; v.async && 0 < v.timeout && (d = ie.setTimeout(function () { T.abort("timeout") }, v.timeout)); try { h = !1, c.send(a, l) } catch (e) { if (h) throw e; l(-1, e) } } else l(-1, "No Transport"); function l(e, t, n, r) { var i, o, a, s, u, l = t; h || (h = !0, d && ie.clearTimeout(d), c = void 0, p = r || "", T.readyState = 0 < e ? 4 : 0, i = 200 <= e && e < 300 || 304 === e, n && (s = function (e, t, n) { var r, i, o, a, s = e.contents, u = e.dataTypes; while ("*" === u[0]) u.shift(), void 0 === r && (r = e.mimeType || t.getResponseHeader("Content-Type")); if (r) for (i in s) if (s[i] && s[i].test(r)) { u.unshift(i); break } if (u[0] in n) o = u[0]; else { for (i in n) { if (!u[0] || e.converters[i + " " + u[0]]) { o = i; break } a || (a = i) } o = o || a } if (o) return o !== u[0] && u.unshift(o), n[o] }(v, T, n)), !i && -1 < ce.inArray("script", v.dataTypes) && ce.inArray("json", v.dataTypes) < 0 && (v.converters["text script"] = function () { }), s = function (e, t, n, r) { var i, o, a, s, u, l = {}, c = e.dataTypes.slice(); if (c[1]) for (a in e.converters) l[a.toLowerCase()] = e.converters[a]; o = c.shift(); while (o) if (e.responseFields[o] && (n[e.responseFields[o]] = t), !u && r && e.dataFilter && (t = e.dataFilter(t, e.dataType)), u = o, o = c.shift()) if ("*" === o) o = u; else if ("*" !== u && u !== o) { if (!(a = l[u + " " + o] || l["* " + o])) for (i in l) if ((s = i.split(" "))[1] === o && (a = l[u + " " + s[0]] || l["* " + s[0]])) { !0 === a ? a = l[i] : !0 !== l[i] && (o = s[0], c.unshift(s[1])); break } if (!0 !== a) if (a && e["throws"]) t = a(t); else try { t = a(t) } catch (e) { return { state: "parsererror", error: a ? e : "No conversion from " + u + " to " + o } } } return { state: "success", data: t } }(v, s, T, i), i ? (v.ifModified && ((u = T.getResponseHeader("Last-Modified")) && (ce.lastModified[f] = u), (u = T.getResponseHeader("etag")) && (ce.etag[f] = u)), 204 === e || "HEAD" === v.type ? l = "nocontent" : 304 === e ? l = "notmodified" : (l = s.state, o = s.data, i = !(a = s.error))) : (a = l, !e && l || (l = "error", e < 0 && (e = 0))), T.status = e, T.statusText = (t || l) + "", i ? x.resolveWith(y, [o, l, T]) : x.rejectWith(y, [T, l, a]), T.statusCode(w), w = void 0, g && m.trigger(i ? "ajaxSuccess" : "ajaxError", [T, v, i ? o : a]), b.fireWith(y, [T, l]), g && (m.trigger("ajaxComplete", [T, v]), --ce.active || ce.event.trigger("ajaxStop"))) } return T }, getJSON: function (e, t, n) { return ce.get(e, t, n, "json") }, getScript: function (e, t) { return ce.get(e, void 0, t, "script") } }), ce.each(["get", "post"], function (e, i) { ce[i] = function (e, t, n, r) { return v(t) && (r = r || n, n = t, t = void 0), ce.ajax(ce.extend({ url: e, type: i, dataType: r, data: t, success: n }, ce.isPlainObject(e) && e)) } }), ce.ajaxPrefilter(function (e) { var t; for (t in e.headers) "content-type" === t.toLowerCase() && (e.contentType = e.headers[t] || "") }), ce._evalUrl = function (e, t, n) { return ce.ajax({ url: e, type: "GET", dataType: "script", cache: !0, async: !1, global: !1, converters: { "text script": function () { } }, dataFilter: function (e) { ce.globalEval(e, t, n) } }) }, ce.fn.extend({ wrapAll: function (e) { var t; return this[0] && (v(e) && (e = e.call(this[0])), t = ce(e, this[0].ownerDocument).eq(0).clone(!0), this[0].parentNode && t.insertBefore(this[0]), t.map(function () { var e = this; while (e.firstElementChild) e = e.firstElementChild; return e }).append(this)), this }, wrapInner: function (n) { return v(n) ? this.each(function (e) { ce(this).wrapInner(n.call(this, e)) }) : this.each(function () { var e = ce(this), t = e.contents(); t.length ? t.wrapAll(n) : e.append(n) }) }, wrap: function (t) { var n = v(t); return this.each(function (e) { ce(this).wrapAll(n ? t.call(this, e) : t) }) }, unwrap: function (e) { return this.parent(e).not("body").each(function () { ce(this).replaceWith(this.childNodes) }), this } }), ce.expr.pseudos.hidden = function (e) { return !ce.expr.pseudos.visible(e) }, ce.expr.pseudos.visible = function (e) { return !!(e.offsetWidth || e.offsetHeight || e.getClientRects().length) }, ce.ajaxSettings.xhr = function () { try { return new ie.XMLHttpRequest } catch (e) { } }; var Yt = { 0: 200, 1223: 204 }, Qt = ce.ajaxSettings.xhr(); le.cors = !!Qt && "withCredentials" in Qt, le.ajax = Qt = !!Qt, ce.ajaxTransport(function (i) { var o, a; if (le.cors || Qt && !i.crossDomain) return { send: function (e, t) { var n, r = i.xhr(); if (r.open(i.type, i.url, i.async, i.username, i.password), i.xhrFields) for (n in i.xhrFields) r[n] = i.xhrFields[n]; for (n in i.mimeType && r.overrideMimeType && r.overrideMimeType(i.mimeType), i.crossDomain || e["X-Requested-With"] || (e["X-Requested-With"] = "XMLHttpRequest"), e) r.setRequestHeader(n, e[n]); o = function (e) { return function () { o && (o = a = r.onload = r.onerror = r.onabort = r.ontimeout = r.onreadystatechange = null, "abort" === e ? r.abort() : "error" === e ? "number" != typeof r.status ? t(0, "error") : t(r.status, r.statusText) : t(Yt[r.status] || r.status, r.statusText, "text" !== (r.responseType || "text") || "string" != typeof r.responseText ? { binary: r.response } : { text: r.responseText }, r.getAllResponseHeaders())) } }, r.onload = o(), a = r.onerror = r.ontimeout = o("error"), void 0 !== r.onabort ? r.onabort = a : r.onreadystatechange = function () { 4 === r.readyState && ie.setTimeout(function () { o && a() }) }, o = o("abort"); try { r.send(i.hasContent && i.data || null) } catch (e) { if (o) throw e } }, abort: function () { o && o() } } }), ce.ajaxPrefilter(function (e) { e.crossDomain && (e.contents.script = !1) }), ce.ajaxSetup({ accepts: { script: "text/javascript, application/javascript, application/ecmascript, application/x-ecmascript" }, contents: { script: /\b(?:java|ecma)script\b/ }, converters: { "text script": function (e) { return ce.globalEval(e), e } } }), ce.ajaxPrefilter("script", function (e) { void 0 === e.cache && (e.cache = !1), e.crossDomain && (e.type = "GET") }), ce.ajaxTransport("script", function (n) { var r, i; if (n.crossDomain || n.scriptAttrs) return { send: function (e, t) { r = ce("<script>").attr(n.scriptAttrs || {}).prop({ charset: n.scriptCharset, src: n.url }).on("load error", i = function (e) { r.remove(), i = null, e && t("error" === e.type ? 404 : 200, e.type) }), C.head.appendChild(r[0]) }, abort: function () { i && i() } } }); var Jt, Kt = [], Zt = /(=)\?(?=&|$)|\?\?/; ce.ajaxSetup({ jsonp: "callback", jsonpCallback: function () { var e = Kt.pop() || ce.expando + "_" + jt.guid++; return this[e] = !0, e } }), ce.ajaxPrefilter("json jsonp", function (e, t, n) { var r, i, o, a = !1 !== e.jsonp && (Zt.test(e.url) ? "url" : "string" == typeof e.data && 0 === (e.contentType || "").indexOf("application/x-www-form-urlencoded") && Zt.test(e.data) && "data"); if (a || "jsonp" === e.dataTypes[0]) return r = e.jsonpCallback = v(e.jsonpCallback) ? e.jsonpCallback() : e.jsonpCallback, a ? e[a] = e[a].replace(Zt, "$1" + r) : !1 !== e.jsonp && (e.url += (At.test(e.url) ? "&" : "?") + e.jsonp + "=" + r), e.converters["script json"] = function () { return o || ce.error(r + " was not called"), o[0] }, e.dataTypes[0] = "json", i = ie[r], ie[r] = function () { o = arguments }, n.always(function () { void 0 === i ? ce(ie).removeProp(r) : ie[r] = i, e[r] && (e.jsonpCallback = t.jsonpCallback, Kt.push(r)), o && v(i) && i(o[0]), o = i = void 0 }), "script" }), le.createHTMLDocument = ((Jt = C.implementation.createHTMLDocument("").body).innerHTML = "<form></form><form></form>", 2 === Jt.childNodes.length), ce.parseHTML = function (e, t, n) { return "string" != typeof e ? [] : ("boolean" == typeof t && (n = t, t = !1), t || (le.createHTMLDocument ? ((r = (t = C.implementation.createHTMLDocument("")).createElement("base")).href = C.location.href, t.head.appendChild(r)) : t = C), o = !n && [], (i = w.exec(e)) ? [t.createElement(i[1])] : (i = Ae([e], t, o), o && o.length && ce(o).remove(), ce.merge([], i.childNodes))); var r, i, o }, ce.fn.load = function (e, t, n) { var r, i, o, a = this, s = e.indexOf(" "); return -1 < s && (r = Tt(e.slice(s)), e = e.slice(0, s)), v(t) ? (n = t, t = void 0) : t && "object" == typeof t && (i = "POST"), 0 < a.length && ce.ajax({ url: e, type: i || "GET", dataType: "html", data: t }).done(function (e) { o = arguments, a.html(r ? ce("<div>").append(ce.parseHTML(e)).find(r) : e) }).always(n && function (e, t) { a.each(function () { n.apply(this, o || [e.responseText, t, e]) }) }), this }, ce.expr.pseudos.animated = function (t) { return ce.grep(ce.timers, function (e) { return t === e.elem }).length }, ce.offset = { setOffset: function (e, t, n) { var r, i, o, a, s, u, l = ce.css(e, "position"), c = ce(e), f = {}; "static" === l && (e.style.position = "relative"), s = c.offset(), o = ce.css(e, "top"), u = ce.css(e, "left"), ("absolute" === l || "fixed" === l) && -1 < (o + u).indexOf("auto") ? (a = (r = c.position()).top, i = r.left) : (a = parseFloat(o) || 0, i = parseFloat(u) || 0), v(t) && (t = t.call(e, n, ce.extend({}, s))), null != t.top && (f.top = t.top - s.top + a), null != t.left && (f.left = t.left - s.left + i), "using" in t ? t.using.call(e, f) : c.css(f) } }, ce.fn.extend({ offset: function (t) { if (arguments.length) return void 0 === t ? this : this.each(function (e) { ce.offset.setOffset(this, t, e) }); var e, n, r = this[0]; return r ? r.getClientRects().length ? (e = r.getBoundingClientRect(), n = r.ownerDocument.defaultView, { top: e.top + n.pageYOffset, left: e.left + n.pageXOffset }) : { top: 0, left: 0 } : void 0 }, position: function () { if (this[0]) { var e, t, n, r = this[0], i = { top: 0, left: 0 }; if ("fixed" === ce.css(r, "position")) t = r.getBoundingClientRect(); else { t = this.offset(), n = r.ownerDocument, e = r.offsetParent || n.documentElement; while (e && (e === n.body || e === n.documentElement) && "static" === ce.css(e, "position")) e = e.parentNode; e && e !== r && 1 === e.nodeType && ((i = ce(e).offset()).top += ce.css(e, "borderTopWidth", !0), i.left += ce.css(e, "borderLeftWidth", !0)) } return { top: t.top - i.top - ce.css(r, "marginTop", !0), left: t.left - i.left - ce.css(r, "marginLeft", !0) } } }, offsetParent: function () { return this.map(function () { var e = this.offsetParent; while (e && "static" === ce.css(e, "position")) e = e.offsetParent; return e || J }) } }), ce.each({ scrollLeft: "pageXOffset", scrollTop: "pageYOffset" }, function (t, i) { var o = "pageYOffset" === i; ce.fn[t] = function (e) { return M(this, function (e, t, n) { var r; if (y(e) ? r = e : 9 === e.nodeType && (r = e.defaultView), void 0 === n) return r ? r[i] : e[t]; r ? r.scrollTo(o ? r.pageXOffset : n, o ? n : r.pageYOffset) : e[t] = n }, t, e, arguments.length) } }), ce.each(["top", "left"], function (e, n) { ce.cssHooks[n] = Ye(le.pixelPosition, function (e, t) { if (t) return t = Ge(e, n), _e.test(t) ? ce(e).position()[n] + "px" : t }) }), ce.each({ Height: "height", Width: "width" }, function (a, s) { ce.each({ padding: "inner" + a, content: s, "": "outer" + a }, function (r, o) { ce.fn[o] = function (e, t) { var n = arguments.length && (r || "boolean" != typeof e), i = r || (!0 === e || !0 === t ? "margin" : "border"); return M(this, function (e, t, n) { var r; return y(e) ? 0 === o.indexOf("outer") ? e["inner" + a] : e.document.documentElement["client" + a] : 9 === e.nodeType ? (r = e.documentElement, Math.max(e.body["scroll" + a], r["scroll" + a], e.body["offset" + a], r["offset" + a], r["client" + a])) : void 0 === n ? ce.css(e, t, i) : ce.style(e, t, n, i) }, s, n ? e : void 0, n) } }) }), ce.each(["ajaxStart", "ajaxStop", "ajaxComplete", "ajaxError", "ajaxSuccess", "ajaxSend"], function (e, t) { ce.fn[t] = function (e) { return this.on(t, e) } }), ce.fn.extend({ bind: function (e, t, n) { return this.on(e, null, t, n) }, unbind: function (e, t) { return this.off(e, null, t) }, delegate: function (e, t, n, r) { return this.on(t, e, n, r) }, undelegate: function (e, t, n) { return 1 === arguments.length ? this.off(e, "**") : this.off(t, e || "**", n) }, hover: function (e, t) { return this.on("mouseenter", e).on("mouseleave", t || e) } }), ce.each("blur focus focusin focusout resize scroll click dblclick mousedown mouseup mousemove mouseover mouseout mouseenter mouseleave change select submit keydown keypress keyup contextmenu".split(" "), function (e, n) { ce.fn[n] = function (e, t) { return 0 < arguments.length ? this.on(n, null, e, t) : this.trigger(n) } }); var en = /^[\s\uFEFF\xA0]+|([^\s\uFEFF\xA0])[\s\uFEFF\xA0]+$/g; ce.proxy = function (e, t) { var n, r, i; if ("string" == typeof t && (n = e[t], t = e, e = n), v(e)) return r = ae.call(arguments, 2), (i = function () { return e.apply(t || this, r.concat(ae.call(arguments))) }).guid = e.guid = e.guid || ce.guid++, i }, ce.holdReady = function (e) { e ? ce.readyWait++ : ce.ready(!0) }, ce.isArray = Array.isArray, ce.parseJSON = JSON.parse, ce.nodeName = fe, ce.isFunction = v, ce.isWindow = y, ce.camelCase = F, ce.type = x, ce.now = Date.now, ce.isNumeric = function (e) { var t = ce.type(e); return ("number" === t || "string" === t) && !isNaN(e - parseFloat(e)) }, ce.trim = function (e) { return null == e ? "" : (e + "").replace(en, "$1") }, "function" == typeof define && define.amd && define("jquery", [], function () { return ce }); var tn = ie.jQuery, nn = ie.$; return ce.noConflict = function (e) { return ie.$ === ce && (ie.$ = nn), e && ie.jQuery === ce && (ie.jQuery = tn), ce }, "undefined" == typeof e && (ie.jQuery = ie.$ = ce), ce });

/*!
  * Bootstrap v5.0.2 (https://getbootstrap.com/)
  * Copyright 2011-2021 The Bootstrap Authors (https://github.com/twbs/bootstrap/graphs/contributors)
  * Licensed under MIT (https://github.com/twbs/bootstrap/blob/main/LICENSE)
  */
!function (t, e) { "object" == typeof exports && "undefined" != typeof module ? module.exports = e() : "function" == typeof define && define.amd ? define(e) : (t = "undefined" != typeof globalThis ? globalThis : t || self).bootstrap = e() }(this, (function () { "use strict"; const t = { find: (t, e = document.documentElement) => [].concat(...Element.prototype.querySelectorAll.call(e, t)), findOne: (t, e = document.documentElement) => Element.prototype.querySelector.call(e, t), children: (t, e) => [].concat(...t.children).filter(t => t.matches(e)), parents(t, e) { const i = []; let n = t.parentNode; for (; n && n.nodeType === Node.ELEMENT_NODE && 3 !== n.nodeType;)n.matches(e) && i.push(n), n = n.parentNode; return i }, prev(t, e) { let i = t.previousElementSibling; for (; i;) { if (i.matches(e)) return [i]; i = i.previousElementSibling } return [] }, next(t, e) { let i = t.nextElementSibling; for (; i;) { if (i.matches(e)) return [i]; i = i.nextElementSibling } return [] } }, e = t => { do { t += Math.floor(1e6 * Math.random()) } while (document.getElementById(t)); return t }, i = t => { let e = t.getAttribute("data-bs-target"); if (!e || "#" === e) { let i = t.getAttribute("href"); if (!i || !i.includes("#") && !i.startsWith(".")) return null; i.includes("#") && !i.startsWith("#") && (i = "#" + i.split("#")[1]), e = i && "#" !== i ? i.trim() : null } return e }, n = t => { const e = i(t); return e && document.querySelector(e) ? e : null }, s = t => { const e = i(t); return e ? document.querySelector(e) : null }, o = t => { t.dispatchEvent(new Event("transitionend")) }, r = t => !(!t || "object" != typeof t) && (void 0 !== t.jquery && (t = t[0]), void 0 !== t.nodeType), a = e => r(e) ? e.jquery ? e[0] : e : "string" == typeof e && e.length > 0 ? t.findOne(e) : null, l = (t, e, i) => { Object.keys(i).forEach(n => { const s = i[n], o = e[n], a = o && r(o) ? "element" : null == (l = o) ? "" + l : {}.toString.call(l).match(/\s([a-z]+)/i)[1].toLowerCase(); var l; if (!new RegExp(s).test(a)) throw new TypeError(`${t.toUpperCase()}: Option "${n}" provided type "${a}" but expected type "${s}".`) }) }, c = t => !(!r(t) || 0 === t.getClientRects().length) && "visible" === getComputedStyle(t).getPropertyValue("visibility"), h = t => !t || t.nodeType !== Node.ELEMENT_NODE || !!t.classList.contains("disabled") || (void 0 !== t.disabled ? t.disabled : t.hasAttribute("disabled") && "false" !== t.getAttribute("disabled")), d = t => { if (!document.documentElement.attachShadow) return null; if ("function" == typeof t.getRootNode) { const e = t.getRootNode(); return e instanceof ShadowRoot ? e : null } return t instanceof ShadowRoot ? t : t.parentNode ? d(t.parentNode) : null }, u = () => { }, f = t => t.offsetHeight, p = () => { const { jQuery: t } = window; return t && !document.body.hasAttribute("data-bs-no-jquery") ? t : null }, m = [], g = () => "rtl" === document.documentElement.dir, _ = t => { var e; e = () => { const e = p(); if (e) { const i = t.NAME, n = e.fn[i]; e.fn[i] = t.jQueryInterface, e.fn[i].Constructor = t, e.fn[i].noConflict = () => (e.fn[i] = n, t.jQueryInterface) } }, "loading" === document.readyState ? (m.length || document.addEventListener("DOMContentLoaded", () => { m.forEach(t => t()) }), m.push(e)) : e() }, b = t => { "function" == typeof t && t() }, v = (t, e, i = !0) => { if (!i) return void b(t); const n = (t => { if (!t) return 0; let { transitionDuration: e, transitionDelay: i } = window.getComputedStyle(t); const n = Number.parseFloat(e), s = Number.parseFloat(i); return n || s ? (e = e.split(",")[0], i = i.split(",")[0], 1e3 * (Number.parseFloat(e) + Number.parseFloat(i))) : 0 })(e) + 5; let s = !1; const r = ({ target: i }) => { i === e && (s = !0, e.removeEventListener("transitionend", r), b(t)) }; e.addEventListener("transitionend", r), setTimeout(() => { s || o(e) }, n) }, y = (t, e, i, n) => { let s = t.indexOf(e); if (-1 === s) return t[!i && n ? t.length - 1 : 0]; const o = t.length; return s += i ? 1 : -1, n && (s = (s + o) % o), t[Math.max(0, Math.min(s, o - 1))] }, w = /[^.]*(?=\..*)\.|.*/, E = /\..*/, A = /::\d+$/, T = {}; let O = 1; const C = { mouseenter: "mouseover", mouseleave: "mouseout" }, k = /^(mouseenter|mouseleave)/i, L = new Set(["click", "dblclick", "mouseup", "mousedown", "contextmenu", "mousewheel", "DOMMouseScroll", "mouseover", "mouseout", "mousemove", "selectstart", "selectend", "keydown", "keypress", "keyup", "orientationchange", "touchstart", "touchmove", "touchend", "touchcancel", "pointerdown", "pointermove", "pointerup", "pointerleave", "pointercancel", "gesturestart", "gesturechange", "gestureend", "focus", "blur", "change", "reset", "select", "submit", "focusin", "focusout", "load", "unload", "beforeunload", "resize", "move", "DOMContentLoaded", "readystatechange", "error", "abort", "scroll"]); function x(t, e) { return e && `${e}::${O++}` || t.uidEvent || O++ } function D(t) { const e = x(t); return t.uidEvent = e, T[e] = T[e] || {}, T[e] } function S(t, e, i = null) { const n = Object.keys(t); for (let s = 0, o = n.length; s < o; s++) { const o = t[n[s]]; if (o.originalHandler === e && o.delegationSelector === i) return o } return null } function I(t, e, i) { const n = "string" == typeof e, s = n ? i : e; let o = M(t); return L.has(o) || (o = t), [n, s, o] } function N(t, e, i, n, s) { if ("string" != typeof e || !t) return; if (i || (i = n, n = null), k.test(e)) { const t = t => function (e) { if (!e.relatedTarget || e.relatedTarget !== e.delegateTarget && !e.delegateTarget.contains(e.relatedTarget)) return t.call(this, e) }; n ? n = t(n) : i = t(i) } const [o, r, a] = I(e, i, n), l = D(t), c = l[a] || (l[a] = {}), h = S(c, r, o ? i : null); if (h) return void (h.oneOff = h.oneOff && s); const d = x(r, e.replace(w, "")), u = o ? function (t, e, i) { return function n(s) { const o = t.querySelectorAll(e); for (let { target: r } = s; r && r !== this; r = r.parentNode)for (let a = o.length; a--;)if (o[a] === r) return s.delegateTarget = r, n.oneOff && P.off(t, s.type, e, i), i.apply(r, [s]); return null } }(t, i, n) : function (t, e) { return function i(n) { return n.delegateTarget = t, i.oneOff && P.off(t, n.type, e), e.apply(t, [n]) } }(t, i); u.delegationSelector = o ? i : null, u.originalHandler = r, u.oneOff = s, u.uidEvent = d, c[d] = u, t.addEventListener(a, u, o) } function j(t, e, i, n, s) { const o = S(e[i], n, s); o && (t.removeEventListener(i, o, Boolean(s)), delete e[i][o.uidEvent]) } function M(t) { return t = t.replace(E, ""), C[t] || t } const P = { on(t, e, i, n) { N(t, e, i, n, !1) }, one(t, e, i, n) { N(t, e, i, n, !0) }, off(t, e, i, n) { if ("string" != typeof e || !t) return; const [s, o, r] = I(e, i, n), a = r !== e, l = D(t), c = e.startsWith("."); if (void 0 !== o) { if (!l || !l[r]) return; return void j(t, l, r, o, s ? i : null) } c && Object.keys(l).forEach(i => { !function (t, e, i, n) { const s = e[i] || {}; Object.keys(s).forEach(o => { if (o.includes(n)) { const n = s[o]; j(t, e, i, n.originalHandler, n.delegationSelector) } }) }(t, l, i, e.slice(1)) }); const h = l[r] || {}; Object.keys(h).forEach(i => { const n = i.replace(A, ""); if (!a || e.includes(n)) { const e = h[i]; j(t, l, r, e.originalHandler, e.delegationSelector) } }) }, trigger(t, e, i) { if ("string" != typeof e || !t) return null; const n = p(), s = M(e), o = e !== s, r = L.has(s); let a, l = !0, c = !0, h = !1, d = null; return o && n && (a = n.Event(e, i), n(t).trigger(a), l = !a.isPropagationStopped(), c = !a.isImmediatePropagationStopped(), h = a.isDefaultPrevented()), r ? (d = document.createEvent("HTMLEvents"), d.initEvent(s, l, !0)) : d = new CustomEvent(e, { bubbles: l, cancelable: !0 }), void 0 !== i && Object.keys(i).forEach(t => { Object.defineProperty(d, t, { get: () => i[t] }) }), h && d.preventDefault(), c && t.dispatchEvent(d), d.defaultPrevented && void 0 !== a && a.preventDefault(), d } }, H = new Map; var R = { set(t, e, i) { H.has(t) || H.set(t, new Map); const n = H.get(t); n.has(e) || 0 === n.size ? n.set(e, i) :  "" }, get: (t, e) => H.has(t) && H.get(t).get(e) || null, remove(t, e) { if (!H.has(t)) return; const i = H.get(t); i.delete(e), 0 === i.size && H.delete(t) } }; class B { constructor(t) { (t = a(t)) && (this._element = t, R.set(this._element, this.constructor.DATA_KEY, this)) } dispose() { R.remove(this._element, this.constructor.DATA_KEY), P.off(this._element, this.constructor.EVENT_KEY), Object.getOwnPropertyNames(this).forEach(t => { this[t] = null }) } _queueCallback(t, e, i = !0) { v(t, e, i) } static getInstance(t) { return R.get(t, this.DATA_KEY) } static getOrCreateInstance(t, e = {}) { return this.getInstance(t) || new this(t, "object" == typeof e ? e : null) } static get VERSION() { return "5.0.2" } static get NAME() { throw new Error('You have to implement the static method "NAME", for each component!') } static get DATA_KEY() { return "bs." + this.NAME } static get EVENT_KEY() { return "." + this.DATA_KEY } } class W extends B { static get NAME() { return "alert" } close(t) { const e = t ? this._getRootElement(t) : this._element, i = this._triggerCloseEvent(e); null === i || i.defaultPrevented || this._removeElement(e) } _getRootElement(t) { return s(t) || t.closest(".alert") } _triggerCloseEvent(t) { return P.trigger(t, "close.bs.alert") } _removeElement(t) { t.classList.remove("show"); const e = t.classList.contains("fade"); this._queueCallback(() => this._destroyElement(t), t, e) } _destroyElement(t) { t.remove(), P.trigger(t, "closed.bs.alert") } static jQueryInterface(t) { return this.each((function () { const e = W.getOrCreateInstance(this); "close" === t && e[t](this) })) } static handleDismiss(t) { return function (e) { e && e.preventDefault(), t.close(this) } } } P.on(document, "click.bs.alert.data-api", '[data-bs-dismiss="alert"]', W.handleDismiss(new W)), _(W); class q extends B { static get NAME() { return "button" } toggle() { this._element.setAttribute("aria-pressed", this._element.classList.toggle("active")) } static jQueryInterface(t) { return this.each((function () { const e = q.getOrCreateInstance(this); "toggle" === t && e[t]() })) } } function z(t) { return "true" === t || "false" !== t && (t === Number(t).toString() ? Number(t) : "" === t || "null" === t ? null : t) } function $(t) { return t.replace(/[A-Z]/g, t => "-" + t.toLowerCase()) } P.on(document, "click.bs.button.data-api", '[data-bs-toggle="button"]', t => { t.preventDefault(); const e = t.target.closest('[data-bs-toggle="button"]'); q.getOrCreateInstance(e).toggle() }), _(q); const U = { setDataAttribute(t, e, i) { t.setAttribute("data-bs-" + $(e), i) }, removeDataAttribute(t, e) { t.removeAttribute("data-bs-" + $(e)) }, getDataAttributes(t) { if (!t) return {}; const e = {}; return Object.keys(t.dataset).filter(t => t.startsWith("bs")).forEach(i => { let n = i.replace(/^bs/, ""); n = n.charAt(0).toLowerCase() + n.slice(1, n.length), e[n] = z(t.dataset[i]) }), e }, getDataAttribute: (t, e) => z(t.getAttribute("data-bs-" + $(e))), offset(t) { const e = t.getBoundingClientRect(); return { top: e.top + document.body.scrollTop, left: e.left + document.body.scrollLeft } }, position: t => ({ top: t.offsetTop, left: t.offsetLeft }) }, F = { interval: 5e3, keyboard: !0, slide: !1, pause: "hover", wrap: !0, touch: !0 }, V = { interval: "(number|boolean)", keyboard: "boolean", slide: "(boolean|string)", pause: "(string|boolean)", wrap: "boolean", touch: "boolean" }, K = "next", X = "prev", Y = "left", Q = "right", G = { ArrowLeft: Q, ArrowRight: Y }; class Z extends B { constructor(e, i) { super(e), this._items = null, this._interval = null, this._activeElement = null, this._isPaused = !1, this._isSliding = !1, this.touchTimeout = null, this.touchStartX = 0, this.touchDeltaX = 0, this._config = this._getConfig(i), this._indicatorsElement = t.findOne(".carousel-indicators", this._element), this._touchSupported = "ontouchstart" in document.documentElement || navigator.maxTouchPoints > 0, this._pointerEvent = Boolean(window.PointerEvent), this._addEventListeners() } static get Default() { return F } static get NAME() { return "carousel" } next() { this._slide(K) } nextWhenVisible() { !document.hidden && c(this._element) && this.next() } prev() { this._slide(X) } pause(e) { e || (this._isPaused = !0), t.findOne(".carousel-item-next, .carousel-item-prev", this._element) && (o(this._element), this.cycle(!0)), clearInterval(this._interval), this._interval = null } cycle(t) { t || (this._isPaused = !1), this._interval && (clearInterval(this._interval), this._interval = null), this._config && this._config.interval && !this._isPaused && (this._updateInterval(), this._interval = setInterval((document.visibilityState ? this.nextWhenVisible : this.next).bind(this), this._config.interval)) } to(e) { this._activeElement = t.findOne(".active.carousel-item", this._element); const i = this._getItemIndex(this._activeElement); if (e > this._items.length - 1 || e < 0) return; if (this._isSliding) return void P.one(this._element, "slid.bs.carousel", () => this.to(e)); if (i === e) return this.pause(), void this.cycle(); const n = e > i ? K : X; this._slide(n, this._items[e]) } _getConfig(t) { return t = { ...F, ...U.getDataAttributes(this._element), ..."object" == typeof t ? t : {} }, l("carousel", t, V), t } _handleSwipe() { const t = Math.abs(this.touchDeltaX); if (t <= 40) return; const e = t / this.touchDeltaX; this.touchDeltaX = 0, e && this._slide(e > 0 ? Q : Y) } _addEventListeners() { this._config.keyboard && P.on(this._element, "keydown.bs.carousel", t => this._keydown(t)), "hover" === this._config.pause && (P.on(this._element, "mouseenter.bs.carousel", t => this.pause(t)), P.on(this._element, "mouseleave.bs.carousel", t => this.cycle(t))), this._config.touch && this._touchSupported && this._addTouchEventListeners() } _addTouchEventListeners() { const e = t => { !this._pointerEvent || "pen" !== t.pointerType && "touch" !== t.pointerType ? this._pointerEvent || (this.touchStartX = t.touches[0].clientX) : this.touchStartX = t.clientX }, i = t => { this.touchDeltaX = t.touches && t.touches.length > 1 ? 0 : t.touches[0].clientX - this.touchStartX }, n = t => { !this._pointerEvent || "pen" !== t.pointerType && "touch" !== t.pointerType || (this.touchDeltaX = t.clientX - this.touchStartX), this._handleSwipe(), "hover" === this._config.pause && (this.pause(), this.touchTimeout && clearTimeout(this.touchTimeout), this.touchTimeout = setTimeout(t => this.cycle(t), 500 + this._config.interval)) }; t.find(".carousel-item img", this._element).forEach(t => { P.on(t, "dragstart.bs.carousel", t => t.preventDefault()) }), this._pointerEvent ? (P.on(this._element, "pointerdown.bs.carousel", t => e(t)), P.on(this._element, "pointerup.bs.carousel", t => n(t)), this._element.classList.add("pointer-event")) : (P.on(this._element, "touchstart.bs.carousel", t => e(t)), P.on(this._element, "touchmove.bs.carousel", t => i(t)), P.on(this._element, "touchend.bs.carousel", t => n(t))) } _keydown(t) { if (/input|textarea/i.test(t.target.tagName)) return; const e = G[t.key]; e && (t.preventDefault(), this._slide(e)) } _getItemIndex(e) { return this._items = e && e.parentNode ? t.find(".carousel-item", e.parentNode) : [], this._items.indexOf(e) } _getItemByOrder(t, e) { const i = t === K; return y(this._items, e, i, this._config.wrap) } _triggerSlideEvent(e, i) { const n = this._getItemIndex(e), s = this._getItemIndex(t.findOne(".active.carousel-item", this._element)); return P.trigger(this._element, "slide.bs.carousel", { relatedTarget: e, direction: i, from: s, to: n }) } _setActiveIndicatorElement(e) { if (this._indicatorsElement) { const i = t.findOne(".active", this._indicatorsElement); i.classList.remove("active"), i.removeAttribute("aria-current"); const n = t.find("[data-bs-target]", this._indicatorsElement); for (let t = 0; t < n.length; t++)if (Number.parseInt(n[t].getAttribute("data-bs-slide-to"), 10) === this._getItemIndex(e)) { n[t].classList.add("active"), n[t].setAttribute("aria-current", "true"); break } } } _updateInterval() { const e = this._activeElement || t.findOne(".active.carousel-item", this._element); if (!e) return; const i = Number.parseInt(e.getAttribute("data-bs-interval"), 10); i ? (this._config.defaultInterval = this._config.defaultInterval || this._config.interval, this._config.interval = i) : this._config.interval = this._config.defaultInterval || this._config.interval } _slide(e, i) { const n = this._directionToOrder(e), s = t.findOne(".active.carousel-item", this._element), o = this._getItemIndex(s), r = i || this._getItemByOrder(n, s), a = this._getItemIndex(r), l = Boolean(this._interval), c = n === K, h = c ? "carousel-item-start" : "carousel-item-end", d = c ? "carousel-item-next" : "carousel-item-prev", u = this._orderToDirection(n); if (r && r.classList.contains("active")) return void (this._isSliding = !1); if (this._isSliding) return; if (this._triggerSlideEvent(r, u).defaultPrevented) return; if (!s || !r) return; this._isSliding = !0, l && this.pause(), this._setActiveIndicatorElement(r), this._activeElement = r; const p = () => { P.trigger(this._element, "slid.bs.carousel", { relatedTarget: r, direction: u, from: o, to: a }) }; if (this._element.classList.contains("slide")) { r.classList.add(d), f(r), s.classList.add(h), r.classList.add(h); const t = () => { r.classList.remove(h, d), r.classList.add("active"), s.classList.remove("active", d, h), this._isSliding = !1, setTimeout(p, 0) }; this._queueCallback(t, s, !0) } else s.classList.remove("active"), r.classList.add("active"), this._isSliding = !1, p(); l && this.cycle() } _directionToOrder(t) { return [Q, Y].includes(t) ? g() ? t === Y ? X : K : t === Y ? K : X : t } _orderToDirection(t) { return [K, X].includes(t) ? g() ? t === X ? Y : Q : t === X ? Q : Y : t } static carouselInterface(t, e) { const i = Z.getOrCreateInstance(t, e); let { _config: n } = i; "object" == typeof e && (n = { ...n, ...e }); const s = "string" == typeof e ? e : n.slide; if ("number" == typeof e) i.to(e); else if ("string" == typeof s) { if (void 0 === i[s]) throw new TypeError(`No method named "${s}"`); i[s]() } else n.interval && n.ride && (i.pause(), i.cycle()) } static jQueryInterface(t) { return this.each((function () { Z.carouselInterface(this, t) })) } static dataApiClickHandler(t) { const e = s(this); if (!e || !e.classList.contains("carousel")) return; const i = { ...U.getDataAttributes(e), ...U.getDataAttributes(this) }, n = this.getAttribute("data-bs-slide-to"); n && (i.interval = !1), Z.carouselInterface(e, i), n && Z.getInstance(e).to(n), t.preventDefault() } } P.on(document, "click.bs.carousel.data-api", "[data-bs-slide], [data-bs-slide-to]", Z.dataApiClickHandler), P.on(window, "load.bs.carousel.data-api", () => { const e = t.find('[data-bs-ride="carousel"]'); for (let t = 0, i = e.length; t < i; t++)Z.carouselInterface(e[t], Z.getInstance(e[t])) }), _(Z); const J = { toggle: !0, parent: "" }, tt = { toggle: "boolean", parent: "(string|element)" }; class et extends B { constructor(e, i) { super(e), this._isTransitioning = !1, this._config = this._getConfig(i), this._triggerArray = t.find(`[data-bs-toggle="collapse"][href="#${this._element.id}"],[data-bs-toggle="collapse"][data-bs-target="#${this._element.id}"]`); const s = t.find('[data-bs-toggle="collapse"]'); for (let e = 0, i = s.length; e < i; e++) { const i = s[e], o = n(i), r = t.find(o).filter(t => t === this._element); null !== o && r.length && (this._selector = o, this._triggerArray.push(i)) } this._parent = this._config.parent ? this._getParent() : null, this._config.parent || this._addAriaAndCollapsedClass(this._element, this._triggerArray), this._config.toggle && this.toggle() } static get Default() { return J } static get NAME() { return "collapse" } toggle() { this._element.classList.contains("show") ? this.hide() : this.show() } show() { if (this._isTransitioning || this._element.classList.contains("show")) return; let e, i; this._parent && (e = t.find(".show, .collapsing", this._parent).filter(t => "string" == typeof this._config.parent ? t.getAttribute("data-bs-parent") === this._config.parent : t.classList.contains("collapse")), 0 === e.length && (e = null)); const n = t.findOne(this._selector); if (e) { const t = e.find(t => n !== t); if (i = t ? et.getInstance(t) : null, i && i._isTransitioning) return } if (P.trigger(this._element, "show.bs.collapse").defaultPrevented) return; e && e.forEach(t => { n !== t && et.collapseInterface(t, "hide"), i || R.set(t, "bs.collapse", null) }); const s = this._getDimension(); this._element.classList.remove("collapse"), this._element.classList.add("collapsing"), this._element.style[s] = 0, this._triggerArray.length && this._triggerArray.forEach(t => { t.classList.remove("collapsed"), t.setAttribute("aria-expanded", !0) }), this.setTransitioning(!0); const o = "scroll" + (s[0].toUpperCase() + s.slice(1)); this._queueCallback(() => { this._element.classList.remove("collapsing"), this._element.classList.add("collapse", "show"), this._element.style[s] = "", this.setTransitioning(!1), P.trigger(this._element, "shown.bs.collapse") }, this._element, !0), this._element.style[s] = this._element[o] + "px" } hide() { if (this._isTransitioning || !this._element.classList.contains("show")) return; if (P.trigger(this._element, "hide.bs.collapse").defaultPrevented) return; const t = this._getDimension(); this._element.style[t] = this._element.getBoundingClientRect()[t] + "px", f(this._element), this._element.classList.add("collapsing"), this._element.classList.remove("collapse", "show"); const e = this._triggerArray.length; if (e > 0) for (let t = 0; t < e; t++) { const e = this._triggerArray[t], i = s(e); i && !i.classList.contains("show") && (e.classList.add("collapsed"), e.setAttribute("aria-expanded", !1)) } this.setTransitioning(!0), this._element.style[t] = "", this._queueCallback(() => { this.setTransitioning(!1), this._element.classList.remove("collapsing"), this._element.classList.add("collapse"), P.trigger(this._element, "hidden.bs.collapse") }, this._element, !0) } setTransitioning(t) { this._isTransitioning = t } _getConfig(t) { return (t = { ...J, ...t }).toggle = Boolean(t.toggle), l("collapse", t, tt), t } _getDimension() { return this._element.classList.contains("width") ? "width" : "height" } _getParent() { let { parent: e } = this._config; e = a(e); const i = `[data-bs-toggle="collapse"][data-bs-parent="${e}"]`; return t.find(i, e).forEach(t => { const e = s(t); this._addAriaAndCollapsedClass(e, [t]) }), e } _addAriaAndCollapsedClass(t, e) { if (!t || !e.length) return; const i = t.classList.contains("show"); e.forEach(t => { i ? t.classList.remove("collapsed") : t.classList.add("collapsed"), t.setAttribute("aria-expanded", i) }) } static collapseInterface(t, e) { let i = et.getInstance(t); const n = { ...J, ...U.getDataAttributes(t), ..."object" == typeof e && e ? e : {} }; if (!i && n.toggle && "string" == typeof e && /show|hide/.test(e) && (n.toggle = !1), i || (i = new et(t, n)), "string" == typeof e) { if (void 0 === i[e]) throw new TypeError(`No method named "${e}"`); i[e]() } } static jQueryInterface(t) { return this.each((function () { et.collapseInterface(this, t) })) } } P.on(document, "click.bs.collapse.data-api", '[data-bs-toggle="collapse"]', (function (e) { ("A" === e.target.tagName || e.delegateTarget && "A" === e.delegateTarget.tagName) && e.preventDefault(); const i = U.getDataAttributes(this), s = n(this); t.find(s).forEach(t => { const e = et.getInstance(t); let n; e ? (null === e._parent && "string" == typeof i.parent && (e._config.parent = i.parent, e._parent = e._getParent()), n = "toggle") : n = i, et.collapseInterface(t, n) }) })), _(et); var it = "top", nt = "bottom", st = "right", ot = "left", rt = [it, nt, st, ot], at = rt.reduce((function (t, e) { return t.concat([e + "-start", e + "-end"]) }), []), lt = [].concat(rt, ["auto"]).reduce((function (t, e) { return t.concat([e, e + "-start", e + "-end"]) }), []), ct = ["beforeRead", "read", "afterRead", "beforeMain", "main", "afterMain", "beforeWrite", "write", "afterWrite"]; function ht(t) { return t ? (t.nodeName || "").toLowerCase() : null } function dt(t) { if (null == t) return window; if ("[object Window]" !== t.toString()) { var e = t.ownerDocument; return e && e.defaultView || window } return t } function ut(t) { return t instanceof dt(t).Element || t instanceof Element } function ft(t) { return t instanceof dt(t).HTMLElement || t instanceof HTMLElement } function pt(t) { return "undefined" != typeof ShadowRoot && (t instanceof dt(t).ShadowRoot || t instanceof ShadowRoot) } var mt = { name: "applyStyles", enabled: !0, phase: "write", fn: function (t) { var e = t.state; Object.keys(e.elements).forEach((function (t) { var i = e.styles[t] || {}, n = e.attributes[t] || {}, s = e.elements[t]; ft(s) && ht(s) && (Object.assign(s.style, i), Object.keys(n).forEach((function (t) { var e = n[t]; !1 === e ? s.removeAttribute(t) : s.setAttribute(t, !0 === e ? "" : e) }))) })) }, effect: function (t) { var e = t.state, i = { popper: { position: e.options.strategy, left: "0", top: "0", margin: "0" }, arrow: { position: "absolute" }, reference: {} }; return Object.assign(e.elements.popper.style, i.popper), e.styles = i, e.elements.arrow && Object.assign(e.elements.arrow.style, i.arrow), function () { Object.keys(e.elements).forEach((function (t) { var n = e.elements[t], s = e.attributes[t] || {}, o = Object.keys(e.styles.hasOwnProperty(t) ? e.styles[t] : i[t]).reduce((function (t, e) { return t[e] = "", t }), {}); ft(n) && ht(n) && (Object.assign(n.style, o), Object.keys(s).forEach((function (t) { n.removeAttribute(t) }))) })) } }, requires: ["computeStyles"] }; function gt(t) { return t.split("-")[0] } function _t(t) { var e = t.getBoundingClientRect(); return { width: e.width, height: e.height, top: e.top, right: e.right, bottom: e.bottom, left: e.left, x: e.left, y: e.top } } function bt(t) { var e = _t(t), i = t.offsetWidth, n = t.offsetHeight; return Math.abs(e.width - i) <= 1 && (i = e.width), Math.abs(e.height - n) <= 1 && (n = e.height), { x: t.offsetLeft, y: t.offsetTop, width: i, height: n } } function vt(t, e) { var i = e.getRootNode && e.getRootNode(); if (t.contains(e)) return !0; if (i && pt(i)) { var n = e; do { if (n && t.isSameNode(n)) return !0; n = n.parentNode || n.host } while (n) } return !1 } function yt(t) { return dt(t).getComputedStyle(t) } function wt(t) { return ["table", "td", "th"].indexOf(ht(t)) >= 0 } function Et(t) { return ((ut(t) ? t.ownerDocument : t.document) || window.document).documentElement } function At(t) { return "html" === ht(t) ? t : t.assignedSlot || t.parentNode || (pt(t) ? t.host : null) || Et(t) } function Tt(t) { return ft(t) && "fixed" !== yt(t).position ? t.offsetParent : null } function Ot(t) { for (var e = dt(t), i = Tt(t); i && wt(i) && "static" === yt(i).position;)i = Tt(i); return i && ("html" === ht(i) || "body" === ht(i) && "static" === yt(i).position) ? e : i || function (t) { var e = -1 !== navigator.userAgent.toLowerCase().indexOf("firefox"); if (-1 !== navigator.userAgent.indexOf("Trident") && ft(t) && "fixed" === yt(t).position) return null; for (var i = At(t); ft(i) && ["html", "body"].indexOf(ht(i)) < 0;) { var n = yt(i); if ("none" !== n.transform || "none" !== n.perspective || "paint" === n.contain || -1 !== ["transform", "perspective"].indexOf(n.willChange) || e && "filter" === n.willChange || e && n.filter && "none" !== n.filter) return i; i = i.parentNode } return null }(t) || e } function Ct(t) { return ["top", "bottom"].indexOf(t) >= 0 ? "x" : "y" } var kt = Math.max, Lt = Math.min, xt = Math.round; function Dt(t, e, i) { return kt(t, Lt(e, i)) } function St(t) { return Object.assign({}, { top: 0, right: 0, bottom: 0, left: 0 }, t) } function It(t, e) { return e.reduce((function (e, i) { return e[i] = t, e }), {}) } var Nt = { name: "arrow", enabled: !0, phase: "main", fn: function (t) { var e, i = t.state, n = t.name, s = t.options, o = i.elements.arrow, r = i.modifiersData.popperOffsets, a = gt(i.placement), l = Ct(a), c = [ot, st].indexOf(a) >= 0 ? "height" : "width"; if (o && r) { var h = function (t, e) { return St("number" != typeof (t = "function" == typeof t ? t(Object.assign({}, e.rects, { placement: e.placement })) : t) ? t : It(t, rt)) }(s.padding, i), d = bt(o), u = "y" === l ? it : ot, f = "y" === l ? nt : st, p = i.rects.reference[c] + i.rects.reference[l] - r[l] - i.rects.popper[c], m = r[l] - i.rects.reference[l], g = Ot(o), _ = g ? "y" === l ? g.clientHeight || 0 : g.clientWidth || 0 : 0, b = p / 2 - m / 2, v = h[u], y = _ - d[c] - h[f], w = _ / 2 - d[c] / 2 + b, E = Dt(v, w, y), A = l; i.modifiersData[n] = ((e = {})[A] = E, e.centerOffset = E - w, e) } }, effect: function (t) { var e = t.state, i = t.options.element, n = void 0 === i ? "[data-popper-arrow]" : i; null != n && ("string" != typeof n || (n = e.elements.popper.querySelector(n))) && vt(e.elements.popper, n) && (e.elements.arrow = n) }, requires: ["popperOffsets"], requiresIfExists: ["preventOverflow"] }, jt = { top: "auto", right: "auto", bottom: "auto", left: "auto" }; function Mt(t) { var e, i = t.popper, n = t.popperRect, s = t.placement, o = t.offsets, r = t.position, a = t.gpuAcceleration, l = t.adaptive, c = t.roundOffsets, h = !0 === c ? function (t) { var e = t.x, i = t.y, n = window.devicePixelRatio || 1; return { x: xt(xt(e * n) / n) || 0, y: xt(xt(i * n) / n) || 0 } }(o) : "function" == typeof c ? c(o) : o, d = h.x, u = void 0 === d ? 0 : d, f = h.y, p = void 0 === f ? 0 : f, m = o.hasOwnProperty("x"), g = o.hasOwnProperty("y"), _ = ot, b = it, v = window; if (l) { var y = Ot(i), w = "clientHeight", E = "clientWidth"; y === dt(i) && "static" !== yt(y = Et(i)).position && (w = "scrollHeight", E = "scrollWidth"), y = y, s === it && (b = nt, p -= y[w] - n.height, p *= a ? 1 : -1), s === ot && (_ = st, u -= y[E] - n.width, u *= a ? 1 : -1) } var A, T = Object.assign({ position: r }, l && jt); return a ? Object.assign({}, T, ((A = {})[b] = g ? "0" : "", A[_] = m ? "0" : "", A.transform = (v.devicePixelRatio || 1) < 2 ? "translate(" + u + "px, " + p + "px)" : "translate3d(" + u + "px, " + p + "px, 0)", A)) : Object.assign({}, T, ((e = {})[b] = g ? p + "px" : "", e[_] = m ? u + "px" : "", e.transform = "", e)) } var Pt = { name: "computeStyles", enabled: !0, phase: "beforeWrite", fn: function (t) { var e = t.state, i = t.options, n = i.gpuAcceleration, s = void 0 === n || n, o = i.adaptive, r = void 0 === o || o, a = i.roundOffsets, l = void 0 === a || a, c = { placement: gt(e.placement), popper: e.elements.popper, popperRect: e.rects.popper, gpuAcceleration: s }; null != e.modifiersData.popperOffsets && (e.styles.popper = Object.assign({}, e.styles.popper, Mt(Object.assign({}, c, { offsets: e.modifiersData.popperOffsets, position: e.options.strategy, adaptive: r, roundOffsets: l })))), null != e.modifiersData.arrow && (e.styles.arrow = Object.assign({}, e.styles.arrow, Mt(Object.assign({}, c, { offsets: e.modifiersData.arrow, position: "absolute", adaptive: !1, roundOffsets: l })))), e.attributes.popper = Object.assign({}, e.attributes.popper, { "data-popper-placement": e.placement }) }, data: {} }, Ht = { passive: !0 }, Rt = { name: "eventListeners", enabled: !0, phase: "write", fn: function () { }, effect: function (t) { var e = t.state, i = t.instance, n = t.options, s = n.scroll, o = void 0 === s || s, r = n.resize, a = void 0 === r || r, l = dt(e.elements.popper), c = [].concat(e.scrollParents.reference, e.scrollParents.popper); return o && c.forEach((function (t) { t.addEventListener("scroll", i.update, Ht) })), a && l.addEventListener("resize", i.update, Ht), function () { o && c.forEach((function (t) { t.removeEventListener("scroll", i.update, Ht) })), a && l.removeEventListener("resize", i.update, Ht) } }, data: {} }, Bt = { left: "right", right: "left", bottom: "top", top: "bottom" }; function Wt(t) { return t.replace(/left|right|bottom|top/g, (function (t) { return Bt[t] })) } var qt = { start: "end", end: "start" }; function zt(t) { return t.replace(/start|end/g, (function (t) { return qt[t] })) } function $t(t) { var e = dt(t); return { scrollLeft: e.pageXOffset, scrollTop: e.pageYOffset } } function Ut(t) { return _t(Et(t)).left + $t(t).scrollLeft } function Ft(t) { var e = yt(t), i = e.overflow, n = e.overflowX, s = e.overflowY; return /auto|scroll|overlay|hidden/.test(i + s + n) } function Vt(t, e) { var i; void 0 === e && (e = []); var n = function t(e) { return ["html", "body", "#document"].indexOf(ht(e)) >= 0 ? e.ownerDocument.body : ft(e) && Ft(e) ? e : t(At(e)) }(t), s = n === (null == (i = t.ownerDocument) ? void 0 : i.body), o = dt(n), r = s ? [o].concat(o.visualViewport || [], Ft(n) ? n : []) : n, a = e.concat(r); return s ? a : a.concat(Vt(At(r))) } function Kt(t) { return Object.assign({}, t, { left: t.x, top: t.y, right: t.x + t.width, bottom: t.y + t.height }) } function Xt(t, e) { return "viewport" === e ? Kt(function (t) { var e = dt(t), i = Et(t), n = e.visualViewport, s = i.clientWidth, o = i.clientHeight, r = 0, a = 0; return n && (s = n.width, o = n.height, /^((?!chrome|android).)*safari/i.test(navigator.userAgent) || (r = n.offsetLeft, a = n.offsetTop)), { width: s, height: o, x: r + Ut(t), y: a } }(t)) : ft(e) ? function (t) { var e = _t(t); return e.top = e.top + t.clientTop, e.left = e.left + t.clientLeft, e.bottom = e.top + t.clientHeight, e.right = e.left + t.clientWidth, e.width = t.clientWidth, e.height = t.clientHeight, e.x = e.left, e.y = e.top, e }(e) : Kt(function (t) { var e, i = Et(t), n = $t(t), s = null == (e = t.ownerDocument) ? void 0 : e.body, o = kt(i.scrollWidth, i.clientWidth, s ? s.scrollWidth : 0, s ? s.clientWidth : 0), r = kt(i.scrollHeight, i.clientHeight, s ? s.scrollHeight : 0, s ? s.clientHeight : 0), a = -n.scrollLeft + Ut(t), l = -n.scrollTop; return "rtl" === yt(s || i).direction && (a += kt(i.clientWidth, s ? s.clientWidth : 0) - o), { width: o, height: r, x: a, y: l } }(Et(t))) } function Yt(t) { return t.split("-")[1] } function Qt(t) { var e, i = t.reference, n = t.element, s = t.placement, o = s ? gt(s) : null, r = s ? Yt(s) : null, a = i.x + i.width / 2 - n.width / 2, l = i.y + i.height / 2 - n.height / 2; switch (o) { case it: e = { x: a, y: i.y - n.height }; break; case nt: e = { x: a, y: i.y + i.height }; break; case st: e = { x: i.x + i.width, y: l }; break; case ot: e = { x: i.x - n.width, y: l }; break; default: e = { x: i.x, y: i.y } }var c = o ? Ct(o) : null; if (null != c) { var h = "y" === c ? "height" : "width"; switch (r) { case "start": e[c] = e[c] - (i[h] / 2 - n[h] / 2); break; case "end": e[c] = e[c] + (i[h] / 2 - n[h] / 2) } } return e } function Gt(t, e) { void 0 === e && (e = {}); var i = e, n = i.placement, s = void 0 === n ? t.placement : n, o = i.boundary, r = void 0 === o ? "clippingParents" : o, a = i.rootBoundary, l = void 0 === a ? "viewport" : a, c = i.elementContext, h = void 0 === c ? "popper" : c, d = i.altBoundary, u = void 0 !== d && d, f = i.padding, p = void 0 === f ? 0 : f, m = St("number" != typeof p ? p : It(p, rt)), g = "popper" === h ? "reference" : "popper", _ = t.elements.reference, b = t.rects.popper, v = t.elements[u ? g : h], y = function (t, e, i) { var n = "clippingParents" === e ? function (t) { var e = Vt(At(t)), i = ["absolute", "fixed"].indexOf(yt(t).position) >= 0 && ft(t) ? Ot(t) : t; return ut(i) ? e.filter((function (t) { return ut(t) && vt(t, i) && "body" !== ht(t) })) : [] }(t) : [].concat(e), s = [].concat(n, [i]), o = s[0], r = s.reduce((function (e, i) { var n = Xt(t, i); return e.top = kt(n.top, e.top), e.right = Lt(n.right, e.right), e.bottom = Lt(n.bottom, e.bottom), e.left = kt(n.left, e.left), e }), Xt(t, o)); return r.width = r.right - r.left, r.height = r.bottom - r.top, r.x = r.left, r.y = r.top, r }(ut(v) ? v : v.contextElement || Et(t.elements.popper), r, l), w = _t(_), E = Qt({ reference: w, element: b, strategy: "absolute", placement: s }), A = Kt(Object.assign({}, b, E)), T = "popper" === h ? A : w, O = { top: y.top - T.top + m.top, bottom: T.bottom - y.bottom + m.bottom, left: y.left - T.left + m.left, right: T.right - y.right + m.right }, C = t.modifiersData.offset; if ("popper" === h && C) { var k = C[s]; Object.keys(O).forEach((function (t) { var e = [st, nt].indexOf(t) >= 0 ? 1 : -1, i = [it, nt].indexOf(t) >= 0 ? "y" : "x"; O[t] += k[i] * e })) } return O } function Zt(t, e) { void 0 === e && (e = {}); var i = e, n = i.placement, s = i.boundary, o = i.rootBoundary, r = i.padding, a = i.flipVariations, l = i.allowedAutoPlacements, c = void 0 === l ? lt : l, h = Yt(n), d = h ? a ? at : at.filter((function (t) { return Yt(t) === h })) : rt, u = d.filter((function (t) { return c.indexOf(t) >= 0 })); 0 === u.length && (u = d); var f = u.reduce((function (e, i) { return e[i] = Gt(t, { placement: i, boundary: s, rootBoundary: o, padding: r })[gt(i)], e }), {}); return Object.keys(f).sort((function (t, e) { return f[t] - f[e] })) } var Jt = { name: "flip", enabled: !0, phase: "main", fn: function (t) { var e = t.state, i = t.options, n = t.name; if (!e.modifiersData[n]._skip) { for (var s = i.mainAxis, o = void 0 === s || s, r = i.altAxis, a = void 0 === r || r, l = i.fallbackPlacements, c = i.padding, h = i.boundary, d = i.rootBoundary, u = i.altBoundary, f = i.flipVariations, p = void 0 === f || f, m = i.allowedAutoPlacements, g = e.options.placement, _ = gt(g), b = l || (_ !== g && p ? function (t) { if ("auto" === gt(t)) return []; var e = Wt(t); return [zt(t), e, zt(e)] }(g) : [Wt(g)]), v = [g].concat(b).reduce((function (t, i) { return t.concat("auto" === gt(i) ? Zt(e, { placement: i, boundary: h, rootBoundary: d, padding: c, flipVariations: p, allowedAutoPlacements: m }) : i) }), []), y = e.rects.reference, w = e.rects.popper, E = new Map, A = !0, T = v[0], O = 0; O < v.length; O++) { var C = v[O], k = gt(C), L = "start" === Yt(C), x = [it, nt].indexOf(k) >= 0, D = x ? "width" : "height", S = Gt(e, { placement: C, boundary: h, rootBoundary: d, altBoundary: u, padding: c }), I = x ? L ? st : ot : L ? nt : it; y[D] > w[D] && (I = Wt(I)); var N = Wt(I), j = []; if (o && j.push(S[k] <= 0), a && j.push(S[I] <= 0, S[N] <= 0), j.every((function (t) { return t }))) { T = C, A = !1; break } E.set(C, j) } if (A) for (var M = function (t) { var e = v.find((function (e) { var i = E.get(e); if (i) return i.slice(0, t).every((function (t) { return t })) })); if (e) return T = e, "break" }, P = p ? 3 : 1; P > 0 && "break" !== M(P); P--); e.placement !== T && (e.modifiersData[n]._skip = !0, e.placement = T, e.reset = !0) } }, requiresIfExists: ["offset"], data: { _skip: !1 } }; function te(t, e, i) { return void 0 === i && (i = { x: 0, y: 0 }), { top: t.top - e.height - i.y, right: t.right - e.width + i.x, bottom: t.bottom - e.height + i.y, left: t.left - e.width - i.x } } function ee(t) { return [it, st, nt, ot].some((function (e) { return t[e] >= 0 })) } var ie = { name: "hide", enabled: !0, phase: "main", requiresIfExists: ["preventOverflow"], fn: function (t) { var e = t.state, i = t.name, n = e.rects.reference, s = e.rects.popper, o = e.modifiersData.preventOverflow, r = Gt(e, { elementContext: "reference" }), a = Gt(e, { altBoundary: !0 }), l = te(r, n), c = te(a, s, o), h = ee(l), d = ee(c); e.modifiersData[i] = { referenceClippingOffsets: l, popperEscapeOffsets: c, isReferenceHidden: h, hasPopperEscaped: d }, e.attributes.popper = Object.assign({}, e.attributes.popper, { "data-popper-reference-hidden": h, "data-popper-escaped": d }) } }, ne = { name: "offset", enabled: !0, phase: "main", requires: ["popperOffsets"], fn: function (t) { var e = t.state, i = t.options, n = t.name, s = i.offset, o = void 0 === s ? [0, 0] : s, r = lt.reduce((function (t, i) { return t[i] = function (t, e, i) { var n = gt(t), s = [ot, it].indexOf(n) >= 0 ? -1 : 1, o = "function" == typeof i ? i(Object.assign({}, e, { placement: t })) : i, r = o[0], a = o[1]; return r = r || 0, a = (a || 0) * s, [ot, st].indexOf(n) >= 0 ? { x: a, y: r } : { x: r, y: a } }(i, e.rects, o), t }), {}), a = r[e.placement], l = a.x, c = a.y; null != e.modifiersData.popperOffsets && (e.modifiersData.popperOffsets.x += l, e.modifiersData.popperOffsets.y += c), e.modifiersData[n] = r } }, se = { name: "popperOffsets", enabled: !0, phase: "read", fn: function (t) { var e = t.state, i = t.name; e.modifiersData[i] = Qt({ reference: e.rects.reference, element: e.rects.popper, strategy: "absolute", placement: e.placement }) }, data: {} }, oe = { name: "preventOverflow", enabled: !0, phase: "main", fn: function (t) { var e = t.state, i = t.options, n = t.name, s = i.mainAxis, o = void 0 === s || s, r = i.altAxis, a = void 0 !== r && r, l = i.boundary, c = i.rootBoundary, h = i.altBoundary, d = i.padding, u = i.tether, f = void 0 === u || u, p = i.tetherOffset, m = void 0 === p ? 0 : p, g = Gt(e, { boundary: l, rootBoundary: c, padding: d, altBoundary: h }), _ = gt(e.placement), b = Yt(e.placement), v = !b, y = Ct(_), w = "x" === y ? "y" : "x", E = e.modifiersData.popperOffsets, A = e.rects.reference, T = e.rects.popper, O = "function" == typeof m ? m(Object.assign({}, e.rects, { placement: e.placement })) : m, C = { x: 0, y: 0 }; if (E) { if (o || a) { var k = "y" === y ? it : ot, L = "y" === y ? nt : st, x = "y" === y ? "height" : "width", D = E[y], S = E[y] + g[k], I = E[y] - g[L], N = f ? -T[x] / 2 : 0, j = "start" === b ? A[x] : T[x], M = "start" === b ? -T[x] : -A[x], P = e.elements.arrow, H = f && P ? bt(P) : { width: 0, height: 0 }, R = e.modifiersData["arrow#persistent"] ? e.modifiersData["arrow#persistent"].padding : { top: 0, right: 0, bottom: 0, left: 0 }, B = R[k], W = R[L], q = Dt(0, A[x], H[x]), z = v ? A[x] / 2 - N - q - B - O : j - q - B - O, $ = v ? -A[x] / 2 + N + q + W + O : M + q + W + O, U = e.elements.arrow && Ot(e.elements.arrow), F = U ? "y" === y ? U.clientTop || 0 : U.clientLeft || 0 : 0, V = e.modifiersData.offset ? e.modifiersData.offset[e.placement][y] : 0, K = E[y] + z - V - F, X = E[y] + $ - V; if (o) { var Y = Dt(f ? Lt(S, K) : S, D, f ? kt(I, X) : I); E[y] = Y, C[y] = Y - D } if (a) { var Q = "x" === y ? it : ot, G = "x" === y ? nt : st, Z = E[w], J = Z + g[Q], tt = Z - g[G], et = Dt(f ? Lt(J, K) : J, Z, f ? kt(tt, X) : tt); E[w] = et, C[w] = et - Z } } e.modifiersData[n] = C } }, requiresIfExists: ["offset"] }; function re(t, e, i) { void 0 === i && (i = !1); var n, s, o = Et(e), r = _t(t), a = ft(e), l = { scrollLeft: 0, scrollTop: 0 }, c = { x: 0, y: 0 }; return (a || !a && !i) && (("body" !== ht(e) || Ft(o)) && (l = (n = e) !== dt(n) && ft(n) ? { scrollLeft: (s = n).scrollLeft, scrollTop: s.scrollTop } : $t(n)), ft(e) ? ((c = _t(e)).x += e.clientLeft, c.y += e.clientTop) : o && (c.x = Ut(o))), { x: r.left + l.scrollLeft - c.x, y: r.top + l.scrollTop - c.y, width: r.width, height: r.height } } var ae = { placement: "bottom", modifiers: [], strategy: "absolute" }; function le() { for (var t = arguments.length, e = new Array(t), i = 0; i < t; i++)e[i] = arguments[i]; return !e.some((function (t) { return !(t && "function" == typeof t.getBoundingClientRect) })) } function ce(t) { void 0 === t && (t = {}); var e = t, i = e.defaultModifiers, n = void 0 === i ? [] : i, s = e.defaultOptions, o = void 0 === s ? ae : s; return function (t, e, i) { void 0 === i && (i = o); var s, r, a = { placement: "bottom", orderedModifiers: [], options: Object.assign({}, ae, o), modifiersData: {}, elements: { reference: t, popper: e }, attributes: {}, styles: {} }, l = [], c = !1, h = { state: a, setOptions: function (i) { d(), a.options = Object.assign({}, o, a.options, i), a.scrollParents = { reference: ut(t) ? Vt(t) : t.contextElement ? Vt(t.contextElement) : [], popper: Vt(e) }; var s, r, c = function (t) { var e = function (t) { var e = new Map, i = new Set, n = []; return t.forEach((function (t) { e.set(t.name, t) })), t.forEach((function (t) { i.has(t.name) || function t(s) { i.add(s.name), [].concat(s.requires || [], s.requiresIfExists || []).forEach((function (n) { if (!i.has(n)) { var s = e.get(n); s && t(s) } })), n.push(s) }(t) })), n }(t); return ct.reduce((function (t, i) { return t.concat(e.filter((function (t) { return t.phase === i }))) }), []) }((s = [].concat(n, a.options.modifiers), r = s.reduce((function (t, e) { var i = t[e.name]; return t[e.name] = i ? Object.assign({}, i, e, { options: Object.assign({}, i.options, e.options), data: Object.assign({}, i.data, e.data) }) : e, t }), {}), Object.keys(r).map((function (t) { return r[t] })))); return a.orderedModifiers = c.filter((function (t) { return t.enabled })), a.orderedModifiers.forEach((function (t) { var e = t.name, i = t.options, n = void 0 === i ? {} : i, s = t.effect; if ("function" == typeof s) { var o = s({ state: a, name: e, instance: h, options: n }); l.push(o || function () { }) } })), h.update() }, forceUpdate: function () { if (!c) { var t = a.elements, e = t.reference, i = t.popper; if (le(e, i)) { a.rects = { reference: re(e, Ot(i), "fixed" === a.options.strategy), popper: bt(i) }, a.reset = !1, a.placement = a.options.placement, a.orderedModifiers.forEach((function (t) { return a.modifiersData[t.name] = Object.assign({}, t.data) })); for (var n = 0; n < a.orderedModifiers.length; n++)if (!0 !== a.reset) { var s = a.orderedModifiers[n], o = s.fn, r = s.options, l = void 0 === r ? {} : r, d = s.name; "function" == typeof o && (a = o({ state: a, options: l, name: d, instance: h }) || a) } else a.reset = !1, n = -1 } } }, update: (s = function () { return new Promise((function (t) { h.forceUpdate(), t(a) })) }, function () { return r || (r = new Promise((function (t) { Promise.resolve().then((function () { r = void 0, t(s()) })) }))), r }), destroy: function () { d(), c = !0 } }; if (!le(t, e)) return h; function d() { l.forEach((function (t) { return t() })), l = [] } return h.setOptions(i).then((function (t) { !c && i.onFirstUpdate && i.onFirstUpdate(t) })), h } } var he = ce(), de = ce({ defaultModifiers: [Rt, se, Pt, mt] }), ue = ce({ defaultModifiers: [Rt, se, Pt, mt, ne, Jt, oe, Nt, ie] }), fe = Object.freeze({ __proto__: null, popperGenerator: ce, detectOverflow: Gt, createPopperBase: he, createPopper: ue, createPopperLite: de, top: it, bottom: nt, right: st, left: ot, auto: "auto", basePlacements: rt, start: "start", end: "end", clippingParents: "clippingParents", viewport: "viewport", popper: "popper", reference: "reference", variationPlacements: at, placements: lt, beforeRead: "beforeRead", read: "read", afterRead: "afterRead", beforeMain: "beforeMain", main: "main", afterMain: "afterMain", beforeWrite: "beforeWrite", write: "write", afterWrite: "afterWrite", modifierPhases: ct, applyStyles: mt, arrow: Nt, computeStyles: Pt, eventListeners: Rt, flip: Jt, hide: ie, offset: ne, popperOffsets: se, preventOverflow: oe }); const pe = new RegExp("ArrowUp|ArrowDown|Escape"), me = g() ? "top-end" : "top-start", ge = g() ? "top-start" : "top-end", _e = g() ? "bottom-end" : "bottom-start", be = g() ? "bottom-start" : "bottom-end", ve = g() ? "left-start" : "right-start", ye = g() ? "right-start" : "left-start", we = { offset: [0, 2], boundary: "clippingParents", reference: "toggle", display: "dynamic", popperConfig: null, autoClose: !0 }, Ee = { offset: "(array|string|function)", boundary: "(string|element)", reference: "(string|element|object)", display: "string", popperConfig: "(null|object|function)", autoClose: "(boolean|string)" }; class Ae extends B { constructor(t, e) { super(t), this._popper = null, this._config = this._getConfig(e), this._menu = this._getMenuElement(), this._inNavbar = this._detectNavbar(), this._addEventListeners() } static get Default() { return we } static get DefaultType() { return Ee } static get NAME() { return "dropdown" } toggle() { h(this._element) || (this._element.classList.contains("show") ? this.hide() : this.show()) } show() { if (h(this._element) || this._menu.classList.contains("show")) return; const t = Ae.getParentFromElement(this._element), e = { relatedTarget: this._element }; if (!P.trigger(this._element, "show.bs.dropdown", e).defaultPrevented) { if (this._inNavbar) U.setDataAttribute(this._menu, "popper", "none"); else { if (void 0 === fe) throw new TypeError("Bootstrap's dropdowns require Popper (https://popper.js.org)"); let e = this._element; "parent" === this._config.reference ? e = t : r(this._config.reference) ? e = a(this._config.reference) : "object" == typeof this._config.reference && (e = this._config.reference); const i = this._getPopperConfig(), n = i.modifiers.find(t => "applyStyles" === t.name && !1 === t.enabled); this._popper = ue(e, this._menu, i), n && U.setDataAttribute(this._menu, "popper", "static") } "ontouchstart" in document.documentElement && !t.closest(".navbar-nav") && [].concat(...document.body.children).forEach(t => P.on(t, "mouseover", u)), this._element.focus(), this._element.setAttribute("aria-expanded", !0), this._menu.classList.toggle("show"), this._element.classList.toggle("show"), P.trigger(this._element, "shown.bs.dropdown", e) } } hide() { if (h(this._element) || !this._menu.classList.contains("show")) return; const t = { relatedTarget: this._element }; this._completeHide(t) } dispose() { this._popper && this._popper.destroy(), super.dispose() } update() { this._inNavbar = this._detectNavbar(), this._popper && this._popper.update() } _addEventListeners() { P.on(this._element, "click.bs.dropdown", t => { t.preventDefault(), this.toggle() }) } _completeHide(t) { P.trigger(this._element, "hide.bs.dropdown", t).defaultPrevented || ("ontouchstart" in document.documentElement && [].concat(...document.body.children).forEach(t => P.off(t, "mouseover", u)), this._popper && this._popper.destroy(), this._menu.classList.remove("show"), this._element.classList.remove("show"), this._element.setAttribute("aria-expanded", "false"), U.removeDataAttribute(this._menu, "popper"), P.trigger(this._element, "hidden.bs.dropdown", t)) } _getConfig(t) { if (t = { ...this.constructor.Default, ...U.getDataAttributes(this._element), ...t }, l("dropdown", t, this.constructor.DefaultType), "object" == typeof t.reference && !r(t.reference) && "function" != typeof t.reference.getBoundingClientRect) throw new TypeError("dropdown".toUpperCase() + ': Option "reference" provided type "object" without a required "getBoundingClientRect" method.'); return t } _getMenuElement() { return t.next(this._element, ".dropdown-menu")[0] } _getPlacement() { const t = this._element.parentNode; if (t.classList.contains("dropend")) return ve; if (t.classList.contains("dropstart")) return ye; const e = "end" === getComputedStyle(this._menu).getPropertyValue("--bs-position").trim(); return t.classList.contains("dropup") ? e ? ge : me : e ? be : _e } _detectNavbar() { return null !== this._element.closest(".navbar") } _getOffset() { const { offset: t } = this._config; return "string" == typeof t ? t.split(",").map(t => Number.parseInt(t, 10)) : "function" == typeof t ? e => t(e, this._element) : t } _getPopperConfig() { const t = { placement: this._getPlacement(), modifiers: [{ name: "preventOverflow", options: { boundary: this._config.boundary } }, { name: "offset", options: { offset: this._getOffset() } }] }; return "static" === this._config.display && (t.modifiers = [{ name: "applyStyles", enabled: !1 }]), { ...t, ..."function" == typeof this._config.popperConfig ? this._config.popperConfig(t) : this._config.popperConfig } } _selectMenuItem({ key: e, target: i }) { const n = t.find(".dropdown-menu .dropdown-item:not(.disabled):not(:disabled)", this._menu).filter(c); n.length && y(n, i, "ArrowDown" === e, !n.includes(i)).focus() } static dropdownInterface(t, e) { const i = Ae.getOrCreateInstance(t, e); if ("string" == typeof e) { if (void 0 === i[e]) throw new TypeError(`No method named "${e}"`); i[e]() } } static jQueryInterface(t) { return this.each((function () { Ae.dropdownInterface(this, t) })) } static clearMenus(e) { if (e && (2 === e.button || "keyup" === e.type && "Tab" !== e.key)) return; const i = t.find('[data-bs-toggle="dropdown"]'); for (let t = 0, n = i.length; t < n; t++) { const n = Ae.getInstance(i[t]); if (!n || !1 === n._config.autoClose) continue; if (!n._element.classList.contains("show")) continue; const s = { relatedTarget: n._element }; if (e) { const t = e.composedPath(), i = t.includes(n._menu); if (t.includes(n._element) || "inside" === n._config.autoClose && !i || "outside" === n._config.autoClose && i) continue; if (n._menu.contains(e.target) && ("keyup" === e.type && "Tab" === e.key || /input|select|option|textarea|form/i.test(e.target.tagName))) continue; "click" === e.type && (s.clickEvent = e) } n._completeHide(s) } } static getParentFromElement(t) { return s(t) || t.parentNode } static dataApiKeydownHandler(e) { if (/input|textarea/i.test(e.target.tagName) ? "Space" === e.key || "Escape" !== e.key && ("ArrowDown" !== e.key && "ArrowUp" !== e.key || e.target.closest(".dropdown-menu")) : !pe.test(e.key)) return; const i = this.classList.contains("show"); if (!i && "Escape" === e.key) return; if (e.preventDefault(), e.stopPropagation(), h(this)) return; const n = () => this.matches('[data-bs-toggle="dropdown"]') ? this : t.prev(this, '[data-bs-toggle="dropdown"]')[0]; return "Escape" === e.key ? (n().focus(), void Ae.clearMenus()) : "ArrowUp" === e.key || "ArrowDown" === e.key ? (i || n().click(), void Ae.getInstance(n())._selectMenuItem(e)) : void (i && "Space" !== e.key || Ae.clearMenus()) } } P.on(document, "keydown.bs.dropdown.data-api", '[data-bs-toggle="dropdown"]', Ae.dataApiKeydownHandler), P.on(document, "keydown.bs.dropdown.data-api", ".dropdown-menu", Ae.dataApiKeydownHandler), P.on(document, "click.bs.dropdown.data-api", Ae.clearMenus), P.on(document, "keyup.bs.dropdown.data-api", Ae.clearMenus), P.on(document, "click.bs.dropdown.data-api", '[data-bs-toggle="dropdown"]', (function (t) { t.preventDefault(), Ae.dropdownInterface(this) })), _(Ae); class Te { constructor() { this._element = document.body } getWidth() { const t = document.documentElement.clientWidth; return Math.abs(window.innerWidth - t) } hide() { const t = this.getWidth(); this._disableOverFlow(), this._setElementAttributes(this._element, "paddingRight", e => e + t), this._setElementAttributes(".fixed-top, .fixed-bottom, .is-fixed, .sticky-top", "paddingRight", e => e + t), this._setElementAttributes(".sticky-top", "marginRight", e => e - t) } _disableOverFlow() { this._saveInitialAttribute(this._element, "overflow"), this._element.style.overflow = "hidden" } _setElementAttributes(t, e, i) { const n = this.getWidth(); this._applyManipulationCallback(t, t => { if (t !== this._element && window.innerWidth > t.clientWidth + n) return; this._saveInitialAttribute(t, e); const s = window.getComputedStyle(t)[e]; t.style[e] = i(Number.parseFloat(s)) + "px" }) } reset() { this._resetElementAttributes(this._element, "overflow"), this._resetElementAttributes(this._element, "paddingRight"), this._resetElementAttributes(".fixed-top, .fixed-bottom, .is-fixed, .sticky-top", "paddingRight"), this._resetElementAttributes(".sticky-top", "marginRight") } _saveInitialAttribute(t, e) { const i = t.style[e]; i && U.setDataAttribute(t, e, i) } _resetElementAttributes(t, e) { this._applyManipulationCallback(t, t => { const i = U.getDataAttribute(t, e); void 0 === i ? t.style.removeProperty(e) : (U.removeDataAttribute(t, e), t.style[e] = i) }) } _applyManipulationCallback(e, i) { r(e) ? i(e) : t.find(e, this._element).forEach(i) } isOverflowing() { return this.getWidth() > 0 } } const Oe = { isVisible: !0, isAnimated: !1, rootElement: "body", clickCallback: null }, Ce = { isVisible: "boolean", isAnimated: "boolean", rootElement: "(element|string)", clickCallback: "(function|null)" }; class ke { constructor(t) { this._config = this._getConfig(t), this._isAppended = !1, this._element = null } show(t) { this._config.isVisible ? (this._append(), this._config.isAnimated && f(this._getElement()), this._getElement().classList.add("show"), this._emulateAnimation(() => { b(t) })) : b(t) } hide(t) { this._config.isVisible ? (this._getElement().classList.remove("show"), this._emulateAnimation(() => { this.dispose(), b(t) })) : b(t) } _getElement() { if (!this._element) { const t = document.createElement("div"); t.className = "modal-backdrop", this._config.isAnimated && t.classList.add("fade"), this._element = t } return this._element } _getConfig(t) { return (t = { ...Oe, ..."object" == typeof t ? t : {} }).rootElement = a(t.rootElement), l("backdrop", t, Ce), t } _append() { this._isAppended || (this._config.rootElement.appendChild(this._getElement()), P.on(this._getElement(), "mousedown.bs.backdrop", () => { b(this._config.clickCallback) }), this._isAppended = !0) } dispose() { this._isAppended && (P.off(this._element, "mousedown.bs.backdrop"), this._element.remove(), this._isAppended = !1) } _emulateAnimation(t) { v(t, this._getElement(), this._config.isAnimated) } } const Le = { backdrop: !0, keyboard: !0, focus: !0 }, xe = { backdrop: "(boolean|string)", keyboard: "boolean", focus: "boolean" }; class De extends B { constructor(e, i) { super(e), this._config = this._getConfig(i), this._dialog = t.findOne(".modal-dialog", this._element), this._backdrop = this._initializeBackDrop(), this._isShown = !1, this._ignoreBackdropClick = !1, this._isTransitioning = !1, this._scrollBar = new Te } static get Default() { return Le } static get NAME() { return "modal" } toggle(t) { return this._isShown ? this.hide() : this.show(t) } show(t) { this._isShown || this._isTransitioning || P.trigger(this._element, "show.bs.modal", { relatedTarget: t }).defaultPrevented || (this._isShown = !0, this._isAnimated() && (this._isTransitioning = !0), this._scrollBar.hide(), document.body.classList.add("modal-open"), this._adjustDialog(), this._setEscapeEvent(), this._setResizeEvent(), P.on(this._element, "click.dismiss.bs.modal", '[data-bs-dismiss="modal"]', t => this.hide(t)), P.on(this._dialog, "mousedown.dismiss.bs.modal", () => { P.one(this._element, "mouseup.dismiss.bs.modal", t => { t.target === this._element && (this._ignoreBackdropClick = !0) }) }), this._showBackdrop(() => this._showElement(t))) } hide(t) { if (t && ["A", "AREA"].includes(t.target.tagName) && t.preventDefault(), !this._isShown || this._isTransitioning) return; if (P.trigger(this._element, "hide.bs.modal").defaultPrevented) return; this._isShown = !1; const e = this._isAnimated(); e && (this._isTransitioning = !0), this._setEscapeEvent(), this._setResizeEvent(), P.off(document, "focusin.bs.modal"), this._element.classList.remove("show"), P.off(this._element, "click.dismiss.bs.modal"), P.off(this._dialog, "mousedown.dismiss.bs.modal"), this._queueCallback(() => this._hideModal(), this._element, e) } dispose() { [window, this._dialog].forEach(t => P.off(t, ".bs.modal")), this._backdrop.dispose(), super.dispose(), P.off(document, "focusin.bs.modal") } handleUpdate() { this._adjustDialog() } _initializeBackDrop() { return new ke({ isVisible: Boolean(this._config.backdrop), isAnimated: this._isAnimated() }) } _getConfig(t) { return t = { ...Le, ...U.getDataAttributes(this._element), ..."object" == typeof t ? t : {} }, l("modal", t, xe), t } _showElement(e) { const i = this._isAnimated(), n = t.findOne(".modal-body", this._dialog); this._element.parentNode && this._element.parentNode.nodeType === Node.ELEMENT_NODE || document.body.appendChild(this._element), this._element.style.display = "block", this._element.removeAttribute("aria-hidden"), this._element.setAttribute("aria-modal", !0), this._element.setAttribute("role", "dialog"), this._element.scrollTop = 0, n && (n.scrollTop = 0), i && f(this._element), this._element.classList.add("show"), this._config.focus && this._enforceFocus(), this._queueCallback(() => { this._config.focus && this._element.focus(), this._isTransitioning = !1, P.trigger(this._element, "shown.bs.modal", { relatedTarget: e }) }, this._dialog, i) } _enforceFocus() { P.off(document, "focusin.bs.modal"), P.on(document, "focusin.bs.modal", t => { document === t.target || this._element === t.target || this._element.contains(t.target) || this._element.focus() }) } _setEscapeEvent() { this._isShown ? P.on(this._element, "keydown.dismiss.bs.modal", t => { this._config.keyboard && "Escape" === t.key ? (t.preventDefault(), this.hide()) : this._config.keyboard || "Escape" !== t.key || this._triggerBackdropTransition() }) : P.off(this._element, "keydown.dismiss.bs.modal") } _setResizeEvent() { this._isShown ? P.on(window, "resize.bs.modal", () => this._adjustDialog()) : P.off(window, "resize.bs.modal") } _hideModal() { this._element.style.display = "none", this._element.setAttribute("aria-hidden", !0), this._element.removeAttribute("aria-modal"), this._element.removeAttribute("role"), this._isTransitioning = !1, this._backdrop.hide(() => { document.body.classList.remove("modal-open"), this._resetAdjustments(), this._scrollBar.reset(), P.trigger(this._element, "hidden.bs.modal") }) } _showBackdrop(t) { P.on(this._element, "click.dismiss.bs.modal", t => { this._ignoreBackdropClick ? this._ignoreBackdropClick = !1 : t.target === t.currentTarget && (!0 === this._config.backdrop ? this.hide() : "static" === this._config.backdrop && this._triggerBackdropTransition()) }), this._backdrop.show(t) } _isAnimated() { return this._element.classList.contains("fade") } _triggerBackdropTransition() { if (P.trigger(this._element, "hidePrevented.bs.modal").defaultPrevented) return; const { classList: t, scrollHeight: e, style: i } = this._element, n = e > document.documentElement.clientHeight; !n && "hidden" === i.overflowY || t.contains("modal-static") || (n || (i.overflowY = "hidden"), t.add("modal-static"), this._queueCallback(() => { t.remove("modal-static"), n || this._queueCallback(() => { i.overflowY = "" }, this._dialog) }, this._dialog), this._element.focus()) } _adjustDialog() { const t = this._element.scrollHeight > document.documentElement.clientHeight, e = this._scrollBar.getWidth(), i = e > 0; (!i && t && !g() || i && !t && g()) && (this._element.style.paddingLeft = e + "px"), (i && !t && !g() || !i && t && g()) && (this._element.style.paddingRight = e + "px") } _resetAdjustments() { this._element.style.paddingLeft = "", this._element.style.paddingRight = "" } static jQueryInterface(t, e) { return this.each((function () { const i = De.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === i[t]) throw new TypeError(`No method named "${t}"`); i[t](e) } })) } } P.on(document, "click.bs.modal.data-api", '[data-bs-toggle="modal"]', (function (t) { const e = s(this);["A", "AREA"].includes(this.tagName) && t.preventDefault(), P.one(e, "show.bs.modal", t => { t.defaultPrevented || P.one(e, "hidden.bs.modal", () => { c(this) && this.focus() }) }), De.getOrCreateInstance(e).toggle(this) })), _(De); const Se = { backdrop: !0, keyboard: !0, scroll: !1 }, Ie = { backdrop: "boolean", keyboard: "boolean", scroll: "boolean" }; class Ne extends B { constructor(t, e) { super(t), this._config = this._getConfig(e), this._isShown = !1, this._backdrop = this._initializeBackDrop(), this._addEventListeners() } static get NAME() { return "offcanvas" } static get Default() { return Se } toggle(t) { return this._isShown ? this.hide() : this.show(t) } show(t) { this._isShown || P.trigger(this._element, "show.bs.offcanvas", { relatedTarget: t }).defaultPrevented || (this._isShown = !0, this._element.style.visibility = "visible", this._backdrop.show(), this._config.scroll || ((new Te).hide(), this._enforceFocusOnElement(this._element)), this._element.removeAttribute("aria-hidden"), this._element.setAttribute("aria-modal", !0), this._element.setAttribute("role", "dialog"), this._element.classList.add("show"), this._queueCallback(() => { P.trigger(this._element, "shown.bs.offcanvas", { relatedTarget: t }) }, this._element, !0)) } hide() { this._isShown && (P.trigger(this._element, "hide.bs.offcanvas").defaultPrevented || (P.off(document, "focusin.bs.offcanvas"), this._element.blur(), this._isShown = !1, this._element.classList.remove("show"), this._backdrop.hide(), this._queueCallback(() => { this._element.setAttribute("aria-hidden", !0), this._element.removeAttribute("aria-modal"), this._element.removeAttribute("role"), this._element.style.visibility = "hidden", this._config.scroll || (new Te).reset(), P.trigger(this._element, "hidden.bs.offcanvas") }, this._element, !0))) } dispose() { this._backdrop.dispose(), super.dispose(), P.off(document, "focusin.bs.offcanvas") } _getConfig(t) { return t = { ...Se, ...U.getDataAttributes(this._element), ..."object" == typeof t ? t : {} }, l("offcanvas", t, Ie), t } _initializeBackDrop() { return new ke({ isVisible: this._config.backdrop, isAnimated: !0, rootElement: this._element.parentNode, clickCallback: () => this.hide() }) } _enforceFocusOnElement(t) { P.off(document, "focusin.bs.offcanvas"), P.on(document, "focusin.bs.offcanvas", e => { document === e.target || t === e.target || t.contains(e.target) || t.focus() }), t.focus() } _addEventListeners() { P.on(this._element, "click.dismiss.bs.offcanvas", '[data-bs-dismiss="offcanvas"]', () => this.hide()), P.on(this._element, "keydown.dismiss.bs.offcanvas", t => { this._config.keyboard && "Escape" === t.key && this.hide() }) } static jQueryInterface(t) { return this.each((function () { const e = Ne.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === e[t] || t.startsWith("_") || "constructor" === t) throw new TypeError(`No method named "${t}"`); e[t](this) } })) } } P.on(document, "click.bs.offcanvas.data-api", '[data-bs-toggle="offcanvas"]', (function (e) { const i = s(this); if (["A", "AREA"].includes(this.tagName) && e.preventDefault(), h(this)) return; P.one(i, "hidden.bs.offcanvas", () => { c(this) && this.focus() }); const n = t.findOne(".offcanvas.show"); n && n !== i && Ne.getInstance(n).hide(), Ne.getOrCreateInstance(i).toggle(this) })), P.on(window, "load.bs.offcanvas.data-api", () => t.find(".offcanvas.show").forEach(t => Ne.getOrCreateInstance(t).show())), _(Ne); const je = new Set(["background", "cite", "href", "itemtype", "longdesc", "poster", "src", "xlink:href"]), Me = /^(?:(?:https?|mailto|ftp|tel|file):|[^#&/:?]*(?:[#/?]|$))/i, Pe = /^data:(?:image\/(?:bmp|gif|jpeg|jpg|png|tiff|webp)|video\/(?:mpeg|mp4|ogg|webm)|audio\/(?:mp3|oga|ogg|opus));base64,[\d+/a-z]+=*$/i, He = (t, e) => { const i = t.nodeName.toLowerCase(); if (e.includes(i)) return !je.has(i) || Boolean(Me.test(t.nodeValue) || Pe.test(t.nodeValue)); const n = e.filter(t => t instanceof RegExp); for (let t = 0, e = n.length; t < e; t++)if (n[t].test(i)) return !0; return !1 }; function Re(t, e, i) { if (!t.length) return t; if (i && "function" == typeof i) return i(t); const n = (new window.DOMParser).parseFromString(t, "text/html"), s = Object.keys(e), o = [].concat(...n.body.querySelectorAll("*")); for (let t = 0, i = o.length; t < i; t++) { const i = o[t], n = i.nodeName.toLowerCase(); if (!s.includes(n)) { i.remove(); continue } const r = [].concat(...i.attributes), a = [].concat(e["*"] || [], e[n] || []); r.forEach(t => { He(t, a) || i.removeAttribute(t.nodeName) }) } return n.body.innerHTML } const Be = new RegExp("(^|\\s)bs-tooltip\\S+", "g"), We = new Set(["sanitize", "allowList", "sanitizeFn"]), qe = { animation: "boolean", template: "string", title: "(string|element|function)", trigger: "string", delay: "(number|object)", html: "boolean", selector: "(string|boolean)", placement: "(string|function)", offset: "(array|string|function)", container: "(string|element|boolean)", fallbackPlacements: "array", boundary: "(string|element)", customClass: "(string|function)", sanitize: "boolean", sanitizeFn: "(null|function)", allowList: "object", popperConfig: "(null|object|function)" }, ze = { AUTO: "auto", TOP: "top", RIGHT: g() ? "left" : "right", BOTTOM: "bottom", LEFT: g() ? "right" : "left" }, $e = { animation: !0, template: '<div class="tooltip" role="tooltip"><div class="tooltip-arrow"></div><div class="tooltip-inner"></div></div>', trigger: "hover focus", title: "", delay: 0, html: !1, selector: !1, placement: "top", offset: [0, 0], container: !1, fallbackPlacements: ["top", "right", "bottom", "left"], boundary: "clippingParents", customClass: "", sanitize: !0, sanitizeFn: null, allowList: { "*": ["class", "dir", "id", "lang", "role", /^aria-[\w-]*$/i], a: ["target", "href", "title", "rel"], area: [], b: [], br: [], col: [], code: [], div: [], em: [], hr: [], h1: [], h2: [], h3: [], h4: [], h5: [], h6: [], i: [], img: ["src", "srcset", "alt", "title", "width", "height"], li: [], ol: [], p: [], pre: [], s: [], small: [], span: [], sub: [], sup: [], strong: [], u: [], ul: [] }, popperConfig: null }, Ue = { HIDE: "hide.bs.tooltip", HIDDEN: "hidden.bs.tooltip", SHOW: "show.bs.tooltip", SHOWN: "shown.bs.tooltip", INSERTED: "inserted.bs.tooltip", CLICK: "click.bs.tooltip", FOCUSIN: "focusin.bs.tooltip", FOCUSOUT: "focusout.bs.tooltip", MOUSEENTER: "mouseenter.bs.tooltip", MOUSELEAVE: "mouseleave.bs.tooltip" }; class Fe extends B { constructor(t, e) { if (void 0 === fe) throw new TypeError("Bootstrap's tooltips require Popper (https://popper.js.org)"); super(t), this._isEnabled = !0, this._timeout = 0, this._hoverState = "", this._activeTrigger = {}, this._popper = null, this._config = this._getConfig(e), this.tip = null, this._setListeners() } static get Default() { return $e } static get NAME() { return "tooltip" } static get Event() { return Ue } static get DefaultType() { return qe } enable() { this._isEnabled = !0 } disable() { this._isEnabled = !1 } toggleEnabled() { this._isEnabled = !this._isEnabled } toggle(t) { if (this._isEnabled) if (t) { const e = this._initializeOnDelegatedTarget(t); e._activeTrigger.click = !e._activeTrigger.click, e._isWithActiveTrigger() ? e._enter(null, e) : e._leave(null, e) } else { if (this.getTipElement().classList.contains("show")) return void this._leave(null, this); this._enter(null, this) } } dispose() { clearTimeout(this._timeout), P.off(this._element.closest(".modal"), "hide.bs.modal", this._hideModalHandler), this.tip && this.tip.remove(), this._popper && this._popper.destroy(), super.dispose() } show() { if ("none" === this._element.style.display) throw new Error("Please use show on visible elements"); if (!this.isWithContent() || !this._isEnabled) return; const t = P.trigger(this._element, this.constructor.Event.SHOW), i = d(this._element), n = null === i ? this._element.ownerDocument.documentElement.contains(this._element) : i.contains(this._element); if (t.defaultPrevented || !n) return; const s = this.getTipElement(), o = e(this.constructor.NAME); s.setAttribute("id", o), this._element.setAttribute("aria-describedby", o), this.setContent(), this._config.animation && s.classList.add("fade"); const r = "function" == typeof this._config.placement ? this._config.placement.call(this, s, this._element) : this._config.placement, a = this._getAttachment(r); this._addAttachmentClass(a); const { container: l } = this._config; R.set(s, this.constructor.DATA_KEY, this), this._element.ownerDocument.documentElement.contains(this.tip) || (l.appendChild(s), P.trigger(this._element, this.constructor.Event.INSERTED)), this._popper ? this._popper.update() : this._popper = ue(this._element, s, this._getPopperConfig(a)), s.classList.add("show"); const c = "function" == typeof this._config.customClass ? this._config.customClass() : this._config.customClass; c && s.classList.add(...c.split(" ")), "ontouchstart" in document.documentElement && [].concat(...document.body.children).forEach(t => { P.on(t, "mouseover", u) }); const h = this.tip.classList.contains("fade"); this._queueCallback(() => { const t = this._hoverState; this._hoverState = null, P.trigger(this._element, this.constructor.Event.SHOWN), "out" === t && this._leave(null, this) }, this.tip, h) } hide() { if (!this._popper) return; const t = this.getTipElement(); if (P.trigger(this._element, this.constructor.Event.HIDE).defaultPrevented) return; t.classList.remove("show"), "ontouchstart" in document.documentElement && [].concat(...document.body.children).forEach(t => P.off(t, "mouseover", u)), this._activeTrigger.click = !1, this._activeTrigger.focus = !1, this._activeTrigger.hover = !1; const e = this.tip.classList.contains("fade"); this._queueCallback(() => { this._isWithActiveTrigger() || ("show" !== this._hoverState && t.remove(), this._cleanTipClass(), this._element.removeAttribute("aria-describedby"), P.trigger(this._element, this.constructor.Event.HIDDEN), this._popper && (this._popper.destroy(), this._popper = null)) }, this.tip, e), this._hoverState = "" } update() { null !== this._popper && this._popper.update() } isWithContent() { return Boolean(this.getTitle()) } getTipElement() { if (this.tip) return this.tip; const t = document.createElement("div"); return t.innerHTML = this._config.template, this.tip = t.children[0], this.tip } setContent() { const e = this.getTipElement(); this.setElementContent(t.findOne(".tooltip-inner", e), this.getTitle()), e.classList.remove("fade", "show") } setElementContent(t, e) { if (null !== t) return r(e) ? (e = a(e), void (this._config.html ? e.parentNode !== t && (t.innerHTML = "", t.appendChild(e)) : t.textContent = e.textContent)) : void (this._config.html ? (this._config.sanitize && (e = Re(e, this._config.allowList, this._config.sanitizeFn)), t.innerHTML = e) : t.textContent = e) } getTitle() { let t = this._element.getAttribute("data-bs-original-title"); return t || (t = "function" == typeof this._config.title ? this._config.title.call(this._element) : this._config.title), t } updateAttachment(t) { return "right" === t ? "end" : "left" === t ? "start" : t } _initializeOnDelegatedTarget(t, e) { const i = this.constructor.DATA_KEY; return (e = e || R.get(t.delegateTarget, i)) || (e = new this.constructor(t.delegateTarget, this._getDelegateConfig()), R.set(t.delegateTarget, i, e)), e } _getOffset() { const { offset: t } = this._config; return "string" == typeof t ? t.split(",").map(t => Number.parseInt(t, 10)) : "function" == typeof t ? e => t(e, this._element) : t } _getPopperConfig(t) { const e = { placement: t, modifiers: [{ name: "flip", options: { fallbackPlacements: this._config.fallbackPlacements } }, { name: "offset", options: { offset: this._getOffset() } }, { name: "preventOverflow", options: { boundary: this._config.boundary } }, { name: "arrow", options: { element: `.${this.constructor.NAME}-arrow` } }, { name: "onChange", enabled: !0, phase: "afterWrite", fn: t => this._handlePopperPlacementChange(t) }], onFirstUpdate: t => { t.options.placement !== t.placement && this._handlePopperPlacementChange(t) } }; return { ...e, ..."function" == typeof this._config.popperConfig ? this._config.popperConfig(e) : this._config.popperConfig } } _addAttachmentClass(t) { this.getTipElement().classList.add("bs-tooltip-" + this.updateAttachment(t)) } _getAttachment(t) { return ze[t.toUpperCase()] } _setListeners() { this._config.trigger.split(" ").forEach(t => { if ("click" === t) P.on(this._element, this.constructor.Event.CLICK, this._config.selector, t => this.toggle(t)); else if ("manual" !== t) { const e = "hover" === t ? this.constructor.Event.MOUSEENTER : this.constructor.Event.FOCUSIN, i = "hover" === t ? this.constructor.Event.MOUSELEAVE : this.constructor.Event.FOCUSOUT; P.on(this._element, e, this._config.selector, t => this._enter(t)), P.on(this._element, i, this._config.selector, t => this._leave(t)) } }), this._hideModalHandler = () => { this._element && this.hide() }, P.on(this._element.closest(".modal"), "hide.bs.modal", this._hideModalHandler), this._config.selector ? this._config = { ...this._config, trigger: "manual", selector: "" } : this._fixTitle() } _fixTitle() { const t = this._element.getAttribute("title"), e = typeof this._element.getAttribute("data-bs-original-title"); (t || "string" !== e) && (this._element.setAttribute("data-bs-original-title", t || ""), !t || this._element.getAttribute("aria-label") || this._element.textContent || this._element.setAttribute("aria-label", t), this._element.setAttribute("title", "")) } _enter(t, e) { e = this._initializeOnDelegatedTarget(t, e), t && (e._activeTrigger["focusin" === t.type ? "focus" : "hover"] = !0), e.getTipElement().classList.contains("show") || "show" === e._hoverState ? e._hoverState = "show" : (clearTimeout(e._timeout), e._hoverState = "show", e._config.delay && e._config.delay.show ? e._timeout = setTimeout(() => { "show" === e._hoverState && e.show() }, e._config.delay.show) : e.show()) } _leave(t, e) { e = this._initializeOnDelegatedTarget(t, e), t && (e._activeTrigger["focusout" === t.type ? "focus" : "hover"] = e._element.contains(t.relatedTarget)), e._isWithActiveTrigger() || (clearTimeout(e._timeout), e._hoverState = "out", e._config.delay && e._config.delay.hide ? e._timeout = setTimeout(() => { "out" === e._hoverState && e.hide() }, e._config.delay.hide) : e.hide()) } _isWithActiveTrigger() { for (const t in this._activeTrigger) if (this._activeTrigger[t]) return !0; return !1 } _getConfig(t) { const e = U.getDataAttributes(this._element); return Object.keys(e).forEach(t => { We.has(t) && delete e[t] }), (t = { ...this.constructor.Default, ...e, ..."object" == typeof t && t ? t : {} }).container = !1 === t.container ? document.body : a(t.container), "number" == typeof t.delay && (t.delay = { show: t.delay, hide: t.delay }), "number" == typeof t.title && (t.title = t.title.toString()), "number" == typeof t.content && (t.content = t.content.toString()), l("tooltip", t, this.constructor.DefaultType), t.sanitize && (t.template = Re(t.template, t.allowList, t.sanitizeFn)), t } _getDelegateConfig() { const t = {}; if (this._config) for (const e in this._config) this.constructor.Default[e] !== this._config[e] && (t[e] = this._config[e]); return t } _cleanTipClass() { const t = this.getTipElement(), e = t.getAttribute("class").match(Be); null !== e && e.length > 0 && e.map(t => t.trim()).forEach(e => t.classList.remove(e)) } _handlePopperPlacementChange(t) { const { state: e } = t; e && (this.tip = e.elements.popper, this._cleanTipClass(), this._addAttachmentClass(this._getAttachment(e.placement))) } static jQueryInterface(t) { return this.each((function () { const e = Fe.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === e[t]) throw new TypeError(`No method named "${t}"`); e[t]() } })) } } _(Fe); const Ve = new RegExp("(^|\\s)bs-popover\\S+", "g"), Ke = { ...Fe.Default, placement: "right", offset: [0, 8], trigger: "click", content: "", template: '<div class="popover" role="tooltip"><div class="popover-arrow"></div><h3 class="popover-header"></h3><div class="popover-body"></div></div>' }, Xe = { ...Fe.DefaultType, content: "(string|element|function)" }, Ye = { HIDE: "hide.bs.popover", HIDDEN: "hidden.bs.popover", SHOW: "show.bs.popover", SHOWN: "shown.bs.popover", INSERTED: "inserted.bs.popover", CLICK: "click.bs.popover", FOCUSIN: "focusin.bs.popover", FOCUSOUT: "focusout.bs.popover", MOUSEENTER: "mouseenter.bs.popover", MOUSELEAVE: "mouseleave.bs.popover" }; class Qe extends Fe { static get Default() { return Ke } static get NAME() { return "popover" } static get Event() { return Ye } static get DefaultType() { return Xe } isWithContent() { return this.getTitle() || this._getContent() } getTipElement() { return this.tip || (this.tip = super.getTipElement(), this.getTitle() || t.findOne(".popover-header", this.tip).remove(), this._getContent() || t.findOne(".popover-body", this.tip).remove()), this.tip } setContent() { const e = this.getTipElement(); this.setElementContent(t.findOne(".popover-header", e), this.getTitle()); let i = this._getContent(); "function" == typeof i && (i = i.call(this._element)), this.setElementContent(t.findOne(".popover-body", e), i), e.classList.remove("fade", "show") } _addAttachmentClass(t) { this.getTipElement().classList.add("bs-popover-" + this.updateAttachment(t)) } _getContent() { return this._element.getAttribute("data-bs-content") || this._config.content } _cleanTipClass() { const t = this.getTipElement(), e = t.getAttribute("class").match(Ve); null !== e && e.length > 0 && e.map(t => t.trim()).forEach(e => t.classList.remove(e)) } static jQueryInterface(t) { return this.each((function () { const e = Qe.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === e[t]) throw new TypeError(`No method named "${t}"`); e[t]() } })) } } _(Qe); const Ge = { offset: 10, method: "auto", target: "" }, Ze = { offset: "number", method: "string", target: "(string|element)" }; class Je extends B { constructor(t, e) { super(t), this._scrollElement = "BODY" === this._element.tagName ? window : this._element, this._config = this._getConfig(e), this._selector = `${this._config.target} .nav-link, ${this._config.target} .list-group-item, ${this._config.target} .dropdown-item`, this._offsets = [], this._targets = [], this._activeTarget = null, this._scrollHeight = 0, P.on(this._scrollElement, "scroll.bs.scrollspy", () => this._process()), this.refresh(), this._process() } static get Default() { return Ge } static get NAME() { return "scrollspy" } refresh() { const e = this._scrollElement === this._scrollElement.window ? "offset" : "position", i = "auto" === this._config.method ? e : this._config.method, s = "position" === i ? this._getScrollTop() : 0; this._offsets = [], this._targets = [], this._scrollHeight = this._getScrollHeight(), t.find(this._selector).map(e => { const o = n(e), r = o ? t.findOne(o) : null; if (r) { const t = r.getBoundingClientRect(); if (t.width || t.height) return [U[i](r).top + s, o] } return null }).filter(t => t).sort((t, e) => t[0] - e[0]).forEach(t => { this._offsets.push(t[0]), this._targets.push(t[1]) }) } dispose() { P.off(this._scrollElement, ".bs.scrollspy"), super.dispose() } _getConfig(t) { if ("string" != typeof (t = { ...Ge, ...U.getDataAttributes(this._element), ..."object" == typeof t && t ? t : {} }).target && r(t.target)) { let { id: i } = t.target; i || (i = e("scrollspy"), t.target.id = i), t.target = "#" + i } return l("scrollspy", t, Ze), t } _getScrollTop() { return this._scrollElement === window ? this._scrollElement.pageYOffset : this._scrollElement.scrollTop } _getScrollHeight() { return this._scrollElement.scrollHeight || Math.max(document.body.scrollHeight, document.documentElement.scrollHeight) } _getOffsetHeight() { return this._scrollElement === window ? window.innerHeight : this._scrollElement.getBoundingClientRect().height } _process() { const t = this._getScrollTop() + this._config.offset, e = this._getScrollHeight(), i = this._config.offset + e - this._getOffsetHeight(); if (this._scrollHeight !== e && this.refresh(), t >= i) { const t = this._targets[this._targets.length - 1]; this._activeTarget !== t && this._activate(t) } else { if (this._activeTarget && t < this._offsets[0] && this._offsets[0] > 0) return this._activeTarget = null, void this._clear(); for (let e = this._offsets.length; e--;)this._activeTarget !== this._targets[e] && t >= this._offsets[e] && (void 0 === this._offsets[e + 1] || t < this._offsets[e + 1]) && this._activate(this._targets[e]) } } _activate(e) { this._activeTarget = e, this._clear(); const i = this._selector.split(",").map(t => `${t}[data-bs-target="${e}"],${t}[href="${e}"]`), n = t.findOne(i.join(",")); n.classList.contains("dropdown-item") ? (t.findOne(".dropdown-toggle", n.closest(".dropdown")).classList.add("active"), n.classList.add("active")) : (n.classList.add("active"), t.parents(n, ".nav, .list-group").forEach(e => { t.prev(e, ".nav-link, .list-group-item").forEach(t => t.classList.add("active")), t.prev(e, ".nav-item").forEach(e => { t.children(e, ".nav-link").forEach(t => t.classList.add("active")) }) })), P.trigger(this._scrollElement, "activate.bs.scrollspy", { relatedTarget: e }) } _clear() { t.find(this._selector).filter(t => t.classList.contains("active")).forEach(t => t.classList.remove("active")) } static jQueryInterface(t) { return this.each((function () { const e = Je.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === e[t]) throw new TypeError(`No method named "${t}"`); e[t]() } })) } } P.on(window, "load.bs.scrollspy.data-api", () => { t.find('[data-bs-spy="scroll"]').forEach(t => new Je(t)) }), _(Je); class ti extends B { static get NAME() { return "tab" } show() { if (this._element.parentNode && this._element.parentNode.nodeType === Node.ELEMENT_NODE && this._element.classList.contains("active")) return; let e; const i = s(this._element), n = this._element.closest(".nav, .list-group"); if (n) { const i = "UL" === n.nodeName || "OL" === n.nodeName ? ":scope > li > .active" : ".active"; e = t.find(i, n), e = e[e.length - 1] } const o = e ? P.trigger(e, "hide.bs.tab", { relatedTarget: this._element }) : null; if (P.trigger(this._element, "show.bs.tab", { relatedTarget: e }).defaultPrevented || null !== o && o.defaultPrevented) return; this._activate(this._element, n); const r = () => { P.trigger(e, "hidden.bs.tab", { relatedTarget: this._element }), P.trigger(this._element, "shown.bs.tab", { relatedTarget: e }) }; i ? this._activate(i, i.parentNode, r) : r() } _activate(e, i, n) { const s = (!i || "UL" !== i.nodeName && "OL" !== i.nodeName ? t.children(i, ".active") : t.find(":scope > li > .active", i))[0], o = n && s && s.classList.contains("fade"), r = () => this._transitionComplete(e, s, n); s && o ? (s.classList.remove("show"), this._queueCallback(r, e, !0)) : r() } _transitionComplete(e, i, n) { if (i) { i.classList.remove("active"); const e = t.findOne(":scope > .dropdown-menu .active", i.parentNode); e && e.classList.remove("active"), "tab" === i.getAttribute("role") && i.setAttribute("aria-selected", !1) } e.classList.add("active"), "tab" === e.getAttribute("role") && e.setAttribute("aria-selected", !0), f(e), e.classList.contains("fade") && e.classList.add("show"); let s = e.parentNode; if (s && "LI" === s.nodeName && (s = s.parentNode), s && s.classList.contains("dropdown-menu")) { const i = e.closest(".dropdown"); i && t.find(".dropdown-toggle", i).forEach(t => t.classList.add("active")), e.setAttribute("aria-expanded", !0) } n && n() } static jQueryInterface(t) { return this.each((function () { const e = ti.getOrCreateInstance(this); if ("string" == typeof t) { if (void 0 === e[t]) throw new TypeError(`No method named "${t}"`); e[t]() } })) } } P.on(document, "click.bs.tab.data-api", '[data-bs-toggle="tab"], [data-bs-toggle="pill"], [data-bs-toggle="list"]', (function (t) { ["A", "AREA"].includes(this.tagName) && t.preventDefault(), h(this) || ti.getOrCreateInstance(this).show() })), _(ti); const ei = { animation: "boolean", autohide: "boolean", delay: "number" }, ii = { animation: !0, autohide: !0, delay: 5e3 }; class ni extends B { constructor(t, e) { super(t), this._config = this._getConfig(e), this._timeout = null, this._hasMouseInteraction = !1, this._hasKeyboardInteraction = !1, this._setListeners() } static get DefaultType() { return ei } static get Default() { return ii } static get NAME() { return "toast" } show() { P.trigger(this._element, "show.bs.toast").defaultPrevented || (this._clearTimeout(), this._config.animation && this._element.classList.add("fade"), this._element.classList.remove("hide"), f(this._element), this._element.classList.add("showing"), this._queueCallback(() => { this._element.classList.remove("showing"), this._element.classList.add("show"), P.trigger(this._element, "shown.bs.toast"), this._maybeScheduleHide() }, this._element, this._config.animation)) } hide() { this._element.classList.contains("show") && (P.trigger(this._element, "hide.bs.toast").defaultPrevented || (this._element.classList.remove("show"), this._queueCallback(() => { this._element.classList.add("hide"), P.trigger(this._element, "hidden.bs.toast") }, this._element, this._config.animation))) } dispose() { this._clearTimeout(), this._element.classList.contains("show") && this._element.classList.remove("show"), super.dispose() } _getConfig(t) { return t = { ...ii, ...U.getDataAttributes(this._element), ..."object" == typeof t && t ? t : {} }, l("toast", t, this.constructor.DefaultType), t } _maybeScheduleHide() { this._config.autohide && (this._hasMouseInteraction || this._hasKeyboardInteraction || (this._timeout = setTimeout(() => { this.hide() }, this._config.delay))) } _onInteraction(t, e) { switch (t.type) { case "mouseover": case "mouseout": this._hasMouseInteraction = e; break; case "focusin": case "focusout": this._hasKeyboardInteraction = e }if (e) return void this._clearTimeout(); const i = t.relatedTarget; this._element === i || this._element.contains(i) || this._maybeScheduleHide() } _setListeners() { P.on(this._element, "click.dismiss.bs.toast", '[data-bs-dismiss="toast"]', () => this.hide()), P.on(this._element, "mouseover.bs.toast", t => this._onInteraction(t, !0)), P.on(this._element, "mouseout.bs.toast", t => this._onInteraction(t, !1)), P.on(this._element, "focusin.bs.toast", t => this._onInteraction(t, !0)), P.on(this._element, "focusout.bs.toast", t => this._onInteraction(t, !1)) } _clearTimeout() { clearTimeout(this._timeout), this._timeout = null } static jQueryInterface(t) { return this.each((function () { const e = ni.getOrCreateInstance(this, t); if ("string" == typeof t) { if (void 0 === e[t]) throw new TypeError(`No method named "${t}"`); e[t](this) } })) } } return _(ni), { Alert: W, Button: q, Carousel: Z, Collapse: et, Dropdown: Ae, Modal: De, Offcanvas: Ne, Popover: Qe, ScrollSpy: Je, Tab: ti, Toast: ni, Tooltip: Fe } }));
//# sourceMappingURL=bootstrap.bundle.min.js.map


$.fn.block = function (block = true, withText = false) {

	if (block === false) {
		return this.each(function () {
			$(this).children('.block-overlay').remove();
		});
	}

	return this.each(function () {
		var $element = $(this);

		// جلوگیری از اضافه شدن overlay تکراری
		if ($element.children('.block-overlay').length)
			return;

		var $overlay = withText
			? $('<div class="block-overlay"><span>لطفا منتظر بمانید... <span class="spinner-border spinner-border-sm text-primary"></span></span></div>')
			: $('<div class="block-overlay"><span class="spinner-border spinner-border-sm text-primary"></span></div>');

		$overlay.css({
			position: 'absolute',
			inset: 0,
			backgroundColor: 'rgba(0, 0, 0, 0.6)',
			color: '#fff',
			display: 'flex',
			justifyContent: 'center',
			alignItems: 'center',
			zIndex: 1000,
			pointerEvents: 'auto', // 👈 بلاک واقعی کلیک
			borderRadius: $element.css("border-radius"),
			fontSize: '1rem'
		});

		if ($element.css('position') === 'static') {
			$element.css('position', 'relative');
		}

		$element.append($overlay);
	});
};



/**
 * @popperjs/core v2.11.8 - MIT License
 */

(function (global, factory) {
	typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
		typeof define === 'function' && define.amd ? define(['exports'], factory) :
			(global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global.Popper = {}));
}(this, (function (exports) {
	'use strict';

	function getWindow(node) {
		if (node == null) {
			return window;
		}

		if (node.toString() !== '[object Window]') {
			var ownerDocument = node.ownerDocument;
			return ownerDocument ? ownerDocument.defaultView || window : window;
		}

		return node;
	}

	function isElement(node) {
		var OwnElement = getWindow(node).Element;
		return node instanceof OwnElement || node instanceof Element;
	}

	function isHTMLElement(node) {
		var OwnElement = getWindow(node).HTMLElement;
		return node instanceof OwnElement || node instanceof HTMLElement;
	}

	function isShadowRoot(node) {
		// IE 11 has no ShadowRoot
		if (typeof ShadowRoot === 'undefined') {
			return false;
		}

		var OwnElement = getWindow(node).ShadowRoot;
		return node instanceof OwnElement || node instanceof ShadowRoot;
	}

	var max = Math.max;
	var min = Math.min;
	var round = Math.round;

	function getUAString() {
		var uaData = navigator.userAgentData;

		if (uaData != null && uaData.brands && Array.isArray(uaData.brands)) {
			return uaData.brands.map(function (item) {
				return item.brand + "/" + item.version;
			}).join(' ');
		}

		return navigator.userAgent;
	}

	function isLayoutViewport() {
		return !/^((?!chrome|android).)*safari/i.test(getUAString());
	}

	function getBoundingClientRect(element, includeScale, isFixedStrategy) {
		if (includeScale === void 0) {
			includeScale = false;
		}

		if (isFixedStrategy === void 0) {
			isFixedStrategy = false;
		}

		var clientRect = element.getBoundingClientRect();
		var scaleX = 1;
		var scaleY = 1;

		if (includeScale && isHTMLElement(element)) {
			scaleX = element.offsetWidth > 0 ? round(clientRect.width) / element.offsetWidth || 1 : 1;
			scaleY = element.offsetHeight > 0 ? round(clientRect.height) / element.offsetHeight || 1 : 1;
		}

		var _ref = isElement(element) ? getWindow(element) : window,
			visualViewport = _ref.visualViewport;

		var addVisualOffsets = !isLayoutViewport() && isFixedStrategy;
		var x = (clientRect.left + (addVisualOffsets && visualViewport ? visualViewport.offsetLeft : 0)) / scaleX;
		var y = (clientRect.top + (addVisualOffsets && visualViewport ? visualViewport.offsetTop : 0)) / scaleY;
		var width = clientRect.width / scaleX;
		var height = clientRect.height / scaleY;
		return {
			width: width,
			height: height,
			top: y,
			right: x + width,
			bottom: y + height,
			left: x,
			x: x,
			y: y
		};
	}

	function getWindowScroll(node) {
		var win = getWindow(node);
		var scrollLeft = win.pageXOffset;
		var scrollTop = win.pageYOffset;
		return {
			scrollLeft: scrollLeft,
			scrollTop: scrollTop
		};
	}

	function getHTMLElementScroll(element) {
		return {
			scrollLeft: element.scrollLeft,
			scrollTop: element.scrollTop
		};
	}

	function getNodeScroll(node) {
		if (node === getWindow(node) || !isHTMLElement(node)) {
			return getWindowScroll(node);
		} else {
			return getHTMLElementScroll(node);
		}
	}

	function getNodeName(element) {
		return element ? (element.nodeName || '').toLowerCase() : null;
	}

	function getDocumentElement(element) {
		// $FlowFixMe[incompatible-return]: assume body is always available
		return ((isElement(element) ? element.ownerDocument : // $FlowFixMe[prop-missing]
			element.document) || window.document).documentElement;
	}

	function getWindowScrollBarX(element) {
		// If <html> has a CSS width greater than the viewport, then this will be
		// incorrect for RTL.
		// Popper 1 is broken in this case and never had a bug report so let's assume
		// it's not an issue. I don't think anyone ever specifies width on <html>
		// anyway.
		// Browsers where the left scrollbar doesn't cause an issue report `0` for
		// this (e.g. Edge 2019, IE11, Safari)
		return getBoundingClientRect(getDocumentElement(element)).left + getWindowScroll(element).scrollLeft;
	}

	function getComputedStyle(element) {
		return getWindow(element).getComputedStyle(element);
	}

	function isScrollParent(element) {
		// Firefox wants us to check `-x` and `-y` variations as well
		var _getComputedStyle = getComputedStyle(element),
			overflow = _getComputedStyle.overflow,
			overflowX = _getComputedStyle.overflowX,
			overflowY = _getComputedStyle.overflowY;

		return /auto|scroll|overlay|hidden/.test(overflow + overflowY + overflowX);
	}

	function isElementScaled(element) {
		var rect = element.getBoundingClientRect();
		var scaleX = round(rect.width) / element.offsetWidth || 1;
		var scaleY = round(rect.height) / element.offsetHeight || 1;
		return scaleX !== 1 || scaleY !== 1;
	} // Returns the composite rect of an element relative to its offsetParent.
	// Composite means it takes into account transforms as well as layout.


	function getCompositeRect(elementOrVirtualElement, offsetParent, isFixed) {
		if (isFixed === void 0) {
			isFixed = false;
		}

		var isOffsetParentAnElement = isHTMLElement(offsetParent);
		var offsetParentIsScaled = isHTMLElement(offsetParent) && isElementScaled(offsetParent);
		var documentElement = getDocumentElement(offsetParent);
		var rect = getBoundingClientRect(elementOrVirtualElement, offsetParentIsScaled, isFixed);
		var scroll = {
			scrollLeft: 0,
			scrollTop: 0
		};
		var offsets = {
			x: 0,
			y: 0
		};

		if (isOffsetParentAnElement || !isOffsetParentAnElement && !isFixed) {
			if (getNodeName(offsetParent) !== 'body' || // https://github.com/popperjs/popper-core/issues/1078
				isScrollParent(documentElement)) {
				scroll = getNodeScroll(offsetParent);
			}

			if (isHTMLElement(offsetParent)) {
				offsets = getBoundingClientRect(offsetParent, true);
				offsets.x += offsetParent.clientLeft;
				offsets.y += offsetParent.clientTop;
			} else if (documentElement) {
				offsets.x = getWindowScrollBarX(documentElement);
			}
		}

		return {
			x: rect.left + scroll.scrollLeft - offsets.x,
			y: rect.top + scroll.scrollTop - offsets.y,
			width: rect.width,
			height: rect.height
		};
	}

	// means it doesn't take into account transforms.

	function getLayoutRect(element) {
		var clientRect = getBoundingClientRect(element); // Use the clientRect sizes if it's not been transformed.
		// Fixes https://github.com/popperjs/popper-core/issues/1223

		var width = element.offsetWidth;
		var height = element.offsetHeight;

		if (Math.abs(clientRect.width - width) <= 1) {
			width = clientRect.width;
		}

		if (Math.abs(clientRect.height - height) <= 1) {
			height = clientRect.height;
		}

		return {
			x: element.offsetLeft,
			y: element.offsetTop,
			width: width,
			height: height
		};
	}

	function getParentNode(element) {
		if (getNodeName(element) === 'html') {
			return element;
		}

		return (// this is a quicker (but less type safe) way to save quite some bytes from the bundle
			// $FlowFixMe[incompatible-return]
			// $FlowFixMe[prop-missing]
			element.assignedSlot || // step into the shadow DOM of the parent of a slotted node
			element.parentNode || ( // DOM Element detected
				isShadowRoot(element) ? element.host : null) || // ShadowRoot detected
			// $FlowFixMe[incompatible-call]: HTMLElement is a Node
			getDocumentElement(element) // fallback

		);
	}

	function getScrollParent(node) {
		if (['html', 'body', '#document'].indexOf(getNodeName(node)) >= 0) {
			// $FlowFixMe[incompatible-return]: assume body is always available
			return node.ownerDocument.body;
		}

		if (isHTMLElement(node) && isScrollParent(node)) {
			return node;
		}

		return getScrollParent(getParentNode(node));
	}

	/*
	given a DOM element, return the list of all scroll parents, up the list of ancesors
	until we get to the top window object. This list is what we attach scroll listeners
	to, because if any of these parent elements scroll, we'll need to re-calculate the
	reference element's position.
	*/

	function listScrollParents(element, list) {
		var _element$ownerDocumen;

		if (list === void 0) {
			list = [];
		}

		var scrollParent = getScrollParent(element);
		var isBody = scrollParent === ((_element$ownerDocumen = element.ownerDocument) == null ? void 0 : _element$ownerDocumen.body);
		var win = getWindow(scrollParent);
		var target = isBody ? [win].concat(win.visualViewport || [], isScrollParent(scrollParent) ? scrollParent : []) : scrollParent;
		var updatedList = list.concat(target);
		return isBody ? updatedList : // $FlowFixMe[incompatible-call]: isBody tells us target will be an HTMLElement here
			updatedList.concat(listScrollParents(getParentNode(target)));
	}

	function isTableElement(element) {
		return ['table', 'td', 'th'].indexOf(getNodeName(element)) >= 0;
	}

	function getTrueOffsetParent(element) {
		if (!isHTMLElement(element) || // https://github.com/popperjs/popper-core/issues/837
			getComputedStyle(element).position === 'fixed') {
			return null;
		}

		return element.offsetParent;
	} // `.offsetParent` reports `null` for fixed elements, while absolute elements
	// return the containing block


	function getContainingBlock(element) {
		var isFirefox = /firefox/i.test(getUAString());
		var isIE = /Trident/i.test(getUAString());

		if (isIE && isHTMLElement(element)) {
			// In IE 9, 10 and 11 fixed elements containing block is always established by the viewport
			var elementCss = getComputedStyle(element);

			if (elementCss.position === 'fixed') {
				return null;
			}
		}

		var currentNode = getParentNode(element);

		if (isShadowRoot(currentNode)) {
			currentNode = currentNode.host;
		}

		while (isHTMLElement(currentNode) && ['html', 'body'].indexOf(getNodeName(currentNode)) < 0) {
			var css = getComputedStyle(currentNode); // This is non-exhaustive but covers the most common CSS properties that
			// create a containing block.
			// https://developer.mozilla.org/en-US/docs/Web/CSS/Containing_block#identifying_the_containing_block

			if (css.transform !== 'none' || css.perspective !== 'none' || css.contain === 'paint' || ['transform', 'perspective'].indexOf(css.willChange) !== -1 || isFirefox && css.willChange === 'filter' || isFirefox && css.filter && css.filter !== 'none') {
				return currentNode;
			} else {
				currentNode = currentNode.parentNode;
			}
		}

		return null;
	} // Gets the closest ancestor positioned element. Handles some edge cases,
	// such as table ancestors and cross browser bugs.


	function getOffsetParent(element) {
		var window = getWindow(element);
		var offsetParent = getTrueOffsetParent(element);

		while (offsetParent && isTableElement(offsetParent) && getComputedStyle(offsetParent).position === 'static') {
			offsetParent = getTrueOffsetParent(offsetParent);
		}

		if (offsetParent && (getNodeName(offsetParent) === 'html' || getNodeName(offsetParent) === 'body' && getComputedStyle(offsetParent).position === 'static')) {
			return window;
		}

		return offsetParent || getContainingBlock(element) || window;
	}

	var top = 'top';
	var bottom = 'bottom';
	var right = 'right';
	var left = 'left';
	var auto = 'auto';
	var basePlacements = [top, bottom, right, left];
	var start = 'start';
	var end = 'end';
	var clippingParents = 'clippingParents';
	var viewport = 'viewport';
	var popper = 'popper';
	var reference = 'reference';
	var variationPlacements = /*#__PURE__*/basePlacements.reduce(function (acc, placement) {
		return acc.concat([placement + "-" + start, placement + "-" + end]);
	}, []);
	var placements = /*#__PURE__*/[].concat(basePlacements, [auto]).reduce(function (acc, placement) {
		return acc.concat([placement, placement + "-" + start, placement + "-" + end]);
	}, []); // modifiers that need to read the DOM

	var beforeRead = 'beforeRead';
	var read = 'read';
	var afterRead = 'afterRead'; // pure-logic modifiers

	var beforeMain = 'beforeMain';
	var main = 'main';
	var afterMain = 'afterMain'; // modifier with the purpose to write to the DOM (or write into a framework state)

	var beforeWrite = 'beforeWrite';
	var write = 'write';
	var afterWrite = 'afterWrite';
	var modifierPhases = [beforeRead, read, afterRead, beforeMain, main, afterMain, beforeWrite, write, afterWrite];

	function order(modifiers) {
		var map = new Map();
		var visited = new Set();
		var result = [];
		modifiers.forEach(function (modifier) {
			map.set(modifier.name, modifier);
		}); // On visiting object, check for its dependencies and visit them recursively

		function sort(modifier) {
			visited.add(modifier.name);
			var requires = [].concat(modifier.requires || [], modifier.requiresIfExists || []);
			requires.forEach(function (dep) {
				if (!visited.has(dep)) {
					var depModifier = map.get(dep);

					if (depModifier) {
						sort(depModifier);
					}
				}
			});
			result.push(modifier);
		}

		modifiers.forEach(function (modifier) {
			if (!visited.has(modifier.name)) {
				// check for visited object
				sort(modifier);
			}
		});
		return result;
	}

	function orderModifiers(modifiers) {
		// order based on dependencies
		var orderedModifiers = order(modifiers); // order based on phase

		return modifierPhases.reduce(function (acc, phase) {
			return acc.concat(orderedModifiers.filter(function (modifier) {
				return modifier.phase === phase;
			}));
		}, []);
	}

	function debounce(fn) {
		var pending;
		return function () {
			if (!pending) {
				pending = new Promise(function (resolve) {
					Promise.resolve().then(function () {
						pending = undefined;
						resolve(fn());
					});
				});
			}

			return pending;
		};
	}

	function mergeByName(modifiers) {
		var merged = modifiers.reduce(function (merged, current) {
			var existing = merged[current.name];
			merged[current.name] = existing ? Object.assign({}, existing, current, {
				options: Object.assign({}, existing.options, current.options),
				data: Object.assign({}, existing.data, current.data)
			}) : current;
			return merged;
		}, {}); // IE11 does not support Object.values

		return Object.keys(merged).map(function (key) {
			return merged[key];
		});
	}

	function getViewportRect(element, strategy) {
		var win = getWindow(element);
		var html = getDocumentElement(element);
		var visualViewport = win.visualViewport;
		var width = html.clientWidth;
		var height = html.clientHeight;
		var x = 0;
		var y = 0;

		if (visualViewport) {
			width = visualViewport.width;
			height = visualViewport.height;
			var layoutViewport = isLayoutViewport();

			if (layoutViewport || !layoutViewport && strategy === 'fixed') {
				x = visualViewport.offsetLeft;
				y = visualViewport.offsetTop;
			}
		}

		return {
			width: width,
			height: height,
			x: x + getWindowScrollBarX(element),
			y: y
		};
	}

	// of the `<html>` and `<body>` rect bounds if horizontally scrollable

	function getDocumentRect(element) {
		var _element$ownerDocumen;

		var html = getDocumentElement(element);
		var winScroll = getWindowScroll(element);
		var body = (_element$ownerDocumen = element.ownerDocument) == null ? void 0 : _element$ownerDocumen.body;
		var width = max(html.scrollWidth, html.clientWidth, body ? body.scrollWidth : 0, body ? body.clientWidth : 0);
		var height = max(html.scrollHeight, html.clientHeight, body ? body.scrollHeight : 0, body ? body.clientHeight : 0);
		var x = -winScroll.scrollLeft + getWindowScrollBarX(element);
		var y = -winScroll.scrollTop;

		if (getComputedStyle(body || html).direction === 'rtl') {
			x += max(html.clientWidth, body ? body.clientWidth : 0) - width;
		}

		return {
			width: width,
			height: height,
			x: x,
			y: y
		};
	}

	function contains(parent, child) {
		var rootNode = child.getRootNode && child.getRootNode(); // First, attempt with faster native method

		if (parent.contains(child)) {
			return true;
		} // then fallback to custom implementation with Shadow DOM support
		else if (rootNode && isShadowRoot(rootNode)) {
			var next = child;

			do {
				if (next && parent.isSameNode(next)) {
					return true;
				} // $FlowFixMe[prop-missing]: need a better way to handle this...


				next = next.parentNode || next.host;
			} while (next);
		} // Give up, the result is false


		return false;
	}

	function rectToClientRect(rect) {
		return Object.assign({}, rect, {
			left: rect.x,
			top: rect.y,
			right: rect.x + rect.width,
			bottom: rect.y + rect.height
		});
	}

	function getInnerBoundingClientRect(element, strategy) {
		var rect = getBoundingClientRect(element, false, strategy === 'fixed');
		rect.top = rect.top + element.clientTop;
		rect.left = rect.left + element.clientLeft;
		rect.bottom = rect.top + element.clientHeight;
		rect.right = rect.left + element.clientWidth;
		rect.width = element.clientWidth;
		rect.height = element.clientHeight;
		rect.x = rect.left;
		rect.y = rect.top;
		return rect;
	}

	function getClientRectFromMixedType(element, clippingParent, strategy) {
		return clippingParent === viewport ? rectToClientRect(getViewportRect(element, strategy)) : isElement(clippingParent) ? getInnerBoundingClientRect(clippingParent, strategy) : rectToClientRect(getDocumentRect(getDocumentElement(element)));
	} // A "clipping parent" is an overflowable container with the characteristic of
	// clipping (or hiding) overflowing elements with a position different from
	// `initial`


	function getClippingParents(element) {
		var clippingParents = listScrollParents(getParentNode(element));
		var canEscapeClipping = ['absolute', 'fixed'].indexOf(getComputedStyle(element).position) >= 0;
		var clipperElement = canEscapeClipping && isHTMLElement(element) ? getOffsetParent(element) : element;

		if (!isElement(clipperElement)) {
			return [];
		} // $FlowFixMe[incompatible-return]: https://github.com/facebook/flow/issues/1414


		return clippingParents.filter(function (clippingParent) {
			return isElement(clippingParent) && contains(clippingParent, clipperElement) && getNodeName(clippingParent) !== 'body';
		});
	} // Gets the maximum area that the element is visible in due to any number of
	// clipping parents


	function getClippingRect(element, boundary, rootBoundary, strategy) {
		var mainClippingParents = boundary === 'clippingParents' ? getClippingParents(element) : [].concat(boundary);
		var clippingParents = [].concat(mainClippingParents, [rootBoundary]);
		var firstClippingParent = clippingParents[0];
		var clippingRect = clippingParents.reduce(function (accRect, clippingParent) {
			var rect = getClientRectFromMixedType(element, clippingParent, strategy);
			accRect.top = max(rect.top, accRect.top);
			accRect.right = min(rect.right, accRect.right);
			accRect.bottom = min(rect.bottom, accRect.bottom);
			accRect.left = max(rect.left, accRect.left);
			return accRect;
		}, getClientRectFromMixedType(element, firstClippingParent, strategy));
		clippingRect.width = clippingRect.right - clippingRect.left;
		clippingRect.height = clippingRect.bottom - clippingRect.top;
		clippingRect.x = clippingRect.left;
		clippingRect.y = clippingRect.top;
		return clippingRect;
	}

	function getBasePlacement(placement) {
		return placement.split('-')[0];
	}

	function getVariation(placement) {
		return placement.split('-')[1];
	}

	function getMainAxisFromPlacement(placement) {
		return ['top', 'bottom'].indexOf(placement) >= 0 ? 'x' : 'y';
	}

	function computeOffsets(_ref) {
		var reference = _ref.reference,
			element = _ref.element,
			placement = _ref.placement;
		var basePlacement = placement ? getBasePlacement(placement) : null;
		var variation = placement ? getVariation(placement) : null;
		var commonX = reference.x + reference.width / 2 - element.width / 2;
		var commonY = reference.y + reference.height / 2 - element.height / 2;
		var offsets;

		switch (basePlacement) {
			case top:
				offsets = {
					x: commonX,
					y: reference.y - element.height
				};
				break;

			case bottom:
				offsets = {
					x: commonX,
					y: reference.y + reference.height
				};
				break;

			case right:
				offsets = {
					x: reference.x + reference.width,
					y: commonY
				};
				break;

			case left:
				offsets = {
					x: reference.x - element.width,
					y: commonY
				};
				break;

			default:
				offsets = {
					x: reference.x,
					y: reference.y
				};
		}

		var mainAxis = basePlacement ? getMainAxisFromPlacement(basePlacement) : null;

		if (mainAxis != null) {
			var len = mainAxis === 'y' ? 'height' : 'width';

			switch (variation) {
				case start:
					offsets[mainAxis] = offsets[mainAxis] - (reference[len] / 2 - element[len] / 2);
					break;

				case end:
					offsets[mainAxis] = offsets[mainAxis] + (reference[len] / 2 - element[len] / 2);
					break;
			}
		}

		return offsets;
	}

	function getFreshSideObject() {
		return {
			top: 0,
			right: 0,
			bottom: 0,
			left: 0
		};
	}

	function mergePaddingObject(paddingObject) {
		return Object.assign({}, getFreshSideObject(), paddingObject);
	}

	function expandToHashMap(value, keys) {
		return keys.reduce(function (hashMap, key) {
			hashMap[key] = value;
			return hashMap;
		}, {});
	}

	function detectOverflow(state, options) {
		if (options === void 0) {
			options = {};
		}

		var _options = options,
			_options$placement = _options.placement,
			placement = _options$placement === void 0 ? state.placement : _options$placement,
			_options$strategy = _options.strategy,
			strategy = _options$strategy === void 0 ? state.strategy : _options$strategy,
			_options$boundary = _options.boundary,
			boundary = _options$boundary === void 0 ? clippingParents : _options$boundary,
			_options$rootBoundary = _options.rootBoundary,
			rootBoundary = _options$rootBoundary === void 0 ? viewport : _options$rootBoundary,
			_options$elementConte = _options.elementContext,
			elementContext = _options$elementConte === void 0 ? popper : _options$elementConte,
			_options$altBoundary = _options.altBoundary,
			altBoundary = _options$altBoundary === void 0 ? false : _options$altBoundary,
			_options$padding = _options.padding,
			padding = _options$padding === void 0 ? 0 : _options$padding;
		var paddingObject = mergePaddingObject(typeof padding !== 'number' ? padding : expandToHashMap(padding, basePlacements));
		var altContext = elementContext === popper ? reference : popper;
		var popperRect = state.rects.popper;
		var element = state.elements[altBoundary ? altContext : elementContext];
		var clippingClientRect = getClippingRect(isElement(element) ? element : element.contextElement || getDocumentElement(state.elements.popper), boundary, rootBoundary, strategy);
		var referenceClientRect = getBoundingClientRect(state.elements.reference);
		var popperOffsets = computeOffsets({
			reference: referenceClientRect,
			element: popperRect,
			strategy: 'absolute',
			placement: placement
		});
		var popperClientRect = rectToClientRect(Object.assign({}, popperRect, popperOffsets));
		var elementClientRect = elementContext === popper ? popperClientRect : referenceClientRect; // positive = overflowing the clipping rect
		// 0 or negative = within the clipping rect

		var overflowOffsets = {
			top: clippingClientRect.top - elementClientRect.top + paddingObject.top,
			bottom: elementClientRect.bottom - clippingClientRect.bottom + paddingObject.bottom,
			left: clippingClientRect.left - elementClientRect.left + paddingObject.left,
			right: elementClientRect.right - clippingClientRect.right + paddingObject.right
		};
		var offsetData = state.modifiersData.offset; // Offsets can be applied only to the popper element

		if (elementContext === popper && offsetData) {
			var offset = offsetData[placement];
			Object.keys(overflowOffsets).forEach(function (key) {
				var multiply = [right, bottom].indexOf(key) >= 0 ? 1 : -1;
				var axis = [top, bottom].indexOf(key) >= 0 ? 'y' : 'x';
				overflowOffsets[key] += offset[axis] * multiply;
			});
		}

		return overflowOffsets;
	}

	var DEFAULT_OPTIONS = {
		placement: 'bottom',
		modifiers: [],
		strategy: 'absolute'
	};

	function areValidElements() {
		for (var _len = arguments.length, args = new Array(_len), _key = 0; _key < _len; _key++) {
			args[_key] = arguments[_key];
		}

		return !args.some(function (element) {
			return !(element && typeof element.getBoundingClientRect === 'function');
		});
	}

	function popperGenerator(generatorOptions) {
		if (generatorOptions === void 0) {
			generatorOptions = {};
		}

		var _generatorOptions = generatorOptions,
			_generatorOptions$def = _generatorOptions.defaultModifiers,
			defaultModifiers = _generatorOptions$def === void 0 ? [] : _generatorOptions$def,
			_generatorOptions$def2 = _generatorOptions.defaultOptions,
			defaultOptions = _generatorOptions$def2 === void 0 ? DEFAULT_OPTIONS : _generatorOptions$def2;
		return function createPopper(reference, popper, options) {
			if (options === void 0) {
				options = defaultOptions;
			}

			var state = {
				placement: 'bottom',
				orderedModifiers: [],
				options: Object.assign({}, DEFAULT_OPTIONS, defaultOptions),
				modifiersData: {},
				elements: {
					reference: reference,
					popper: popper
				},
				attributes: {},
				styles: {}
			};
			var effectCleanupFns = [];
			var isDestroyed = false;
			var instance = {
				state: state,
				setOptions: function setOptions(setOptionsAction) {
					var options = typeof setOptionsAction === 'function' ? setOptionsAction(state.options) : setOptionsAction;
					cleanupModifierEffects();
					state.options = Object.assign({}, defaultOptions, state.options, options);
					state.scrollParents = {
						reference: isElement(reference) ? listScrollParents(reference) : reference.contextElement ? listScrollParents(reference.contextElement) : [],
						popper: listScrollParents(popper)
					}; // Orders the modifiers based on their dependencies and `phase`
					// properties

					var orderedModifiers = orderModifiers(mergeByName([].concat(defaultModifiers, state.options.modifiers))); // Strip out disabled modifiers

					state.orderedModifiers = orderedModifiers.filter(function (m) {
						return m.enabled;
					});
					runModifierEffects();
					return instance.update();
				},
				// Sync update – it will always be executed, even if not necessary. This
				// is useful for low frequency updates where sync behavior simplifies the
				// logic.
				// For high frequency updates (e.g. `resize` and `scroll` events), always
				// prefer the async Popper#update method
				forceUpdate: function forceUpdate() {
					if (isDestroyed) {
						return;
					}

					var _state$elements = state.elements,
						reference = _state$elements.reference,
						popper = _state$elements.popper; // Don't proceed if `reference` or `popper` are not valid elements
					// anymore

					if (!areValidElements(reference, popper)) {
						return;
					} // Store the reference and popper rects to be read by modifiers


					state.rects = {
						reference: getCompositeRect(reference, getOffsetParent(popper), state.options.strategy === 'fixed'),
						popper: getLayoutRect(popper)
					}; // Modifiers have the ability to reset the current update cycle. The
					// most common use case for this is the `flip` modifier changing the
					// placement, which then needs to re-run all the modifiers, because the
					// logic was previously ran for the previous placement and is therefore
					// stale/incorrect

					state.reset = false;
					state.placement = state.options.placement; // On each update cycle, the `modifiersData` property for each modifier
					// is filled with the initial data specified by the modifier. This means
					// it doesn't persist and is fresh on each update.
					// To ensure persistent data, use `${name}#persistent`

					state.orderedModifiers.forEach(function (modifier) {
						return state.modifiersData[modifier.name] = Object.assign({}, modifier.data);
					});

					for (var index = 0; index < state.orderedModifiers.length; index++) {
						if (state.reset === true) {
							state.reset = false;
							index = -1;
							continue;
						}

						var _state$orderedModifie = state.orderedModifiers[index],
							fn = _state$orderedModifie.fn,
							_state$orderedModifie2 = _state$orderedModifie.options,
							_options = _state$orderedModifie2 === void 0 ? {} : _state$orderedModifie2,
							name = _state$orderedModifie.name;

						if (typeof fn === 'function') {
							state = fn({
								state: state,
								options: _options,
								name: name,
								instance: instance
							}) || state;
						}
					}
				},
				// Async and optimistically optimized update – it will not be executed if
				// not necessary (debounced to run at most once-per-tick)
				update: debounce(function () {
					return new Promise(function (resolve) {
						instance.forceUpdate();
						resolve(state);
					});
				}),
				destroy: function destroy() {
					cleanupModifierEffects();
					isDestroyed = true;
				}
			};

			if (!areValidElements(reference, popper)) {
				return instance;
			}

			instance.setOptions(options).then(function (state) {
				if (!isDestroyed && options.onFirstUpdate) {
					options.onFirstUpdate(state);
				}
			}); // Modifiers have the ability to execute arbitrary code before the first
			// update cycle runs. They will be executed in the same order as the update
			// cycle. This is useful when a modifier adds some persistent data that
			// other modifiers need to use, but the modifier is run after the dependent
			// one.

			function runModifierEffects() {
				state.orderedModifiers.forEach(function (_ref) {
					var name = _ref.name,
						_ref$options = _ref.options,
						options = _ref$options === void 0 ? {} : _ref$options,
						effect = _ref.effect;

					if (typeof effect === 'function') {
						var cleanupFn = effect({
							state: state,
							name: name,
							instance: instance,
							options: options
						});

						var noopFn = function noopFn() { };

						effectCleanupFns.push(cleanupFn || noopFn);
					}
				});
			}

			function cleanupModifierEffects() {
				effectCleanupFns.forEach(function (fn) {
					return fn();
				});
				effectCleanupFns = [];
			}

			return instance;
		};
	}

	var passive = {
		passive: true
	};

	function effect$2(_ref) {
		var state = _ref.state,
			instance = _ref.instance,
			options = _ref.options;
		var _options$scroll = options.scroll,
			scroll = _options$scroll === void 0 ? true : _options$scroll,
			_options$resize = options.resize,
			resize = _options$resize === void 0 ? true : _options$resize;
		var window = getWindow(state.elements.popper);
		var scrollParents = [].concat(state.scrollParents.reference, state.scrollParents.popper);

		if (scroll) {
			scrollParents.forEach(function (scrollParent) {
				scrollParent.addEventListener('scroll', instance.update, passive);
			});
		}

		if (resize) {
			window.addEventListener('resize', instance.update, passive);
		}

		return function () {
			if (scroll) {
				scrollParents.forEach(function (scrollParent) {
					scrollParent.removeEventListener('scroll', instance.update, passive);
				});
			}

			if (resize) {
				window.removeEventListener('resize', instance.update, passive);
			}
		};
	} // eslint-disable-next-line import/no-unused-modules


	var eventListeners = {
		name: 'eventListeners',
		enabled: true,
		phase: 'write',
		fn: function fn() { },
		effect: effect$2,
		data: {}
	};

	function popperOffsets(_ref) {
		var state = _ref.state,
			name = _ref.name;
		// Offsets are the actual position the popper needs to have to be
		// properly positioned near its reference element
		// This is the most basic placement, and will be adjusted by
		// the modifiers in the next step
		state.modifiersData[name] = computeOffsets({
			reference: state.rects.reference,
			element: state.rects.popper,
			strategy: 'absolute',
			placement: state.placement
		});
	} // eslint-disable-next-line import/no-unused-modules


	var popperOffsets$1 = {
		name: 'popperOffsets',
		enabled: true,
		phase: 'read',
		fn: popperOffsets,
		data: {}
	};

	var unsetSides = {
		top: 'auto',
		right: 'auto',
		bottom: 'auto',
		left: 'auto'
	}; // Round the offsets to the nearest suitable subpixel based on the DPR.
	// Zooming can change the DPR, but it seems to report a value that will
	// cleanly divide the values into the appropriate subpixels.

	function roundOffsetsByDPR(_ref, win) {
		var x = _ref.x,
			y = _ref.y;
		var dpr = win.devicePixelRatio || 1;
		return {
			x: round(x * dpr) / dpr || 0,
			y: round(y * dpr) / dpr || 0
		};
	}

	function mapToStyles(_ref2) {
		var _Object$assign2;

		var popper = _ref2.popper,
			popperRect = _ref2.popperRect,
			placement = _ref2.placement,
			variation = _ref2.variation,
			offsets = _ref2.offsets,
			position = _ref2.position,
			gpuAcceleration = _ref2.gpuAcceleration,
			adaptive = _ref2.adaptive,
			roundOffsets = _ref2.roundOffsets,
			isFixed = _ref2.isFixed;
		var _offsets$x = offsets.x,
			x = _offsets$x === void 0 ? 0 : _offsets$x,
			_offsets$y = offsets.y,
			y = _offsets$y === void 0 ? 0 : _offsets$y;

		var _ref3 = typeof roundOffsets === 'function' ? roundOffsets({
			x: x,
			y: y
		}) : {
			x: x,
			y: y
		};

		x = _ref3.x;
		y = _ref3.y;
		var hasX = offsets.hasOwnProperty('x');
		var hasY = offsets.hasOwnProperty('y');
		var sideX = left;
		var sideY = top;
		var win = window;

		if (adaptive) {
			var offsetParent = getOffsetParent(popper);
			var heightProp = 'clientHeight';
			var widthProp = 'clientWidth';

			if (offsetParent === getWindow(popper)) {
				offsetParent = getDocumentElement(popper);

				if (getComputedStyle(offsetParent).position !== 'static' && position === 'absolute') {
					heightProp = 'scrollHeight';
					widthProp = 'scrollWidth';
				}
			} // $FlowFixMe[incompatible-cast]: force type refinement, we compare offsetParent with window above, but Flow doesn't detect it


			offsetParent = offsetParent;

			if (placement === top || (placement === left || placement === right) && variation === end) {
				sideY = bottom;
				var offsetY = isFixed && offsetParent === win && win.visualViewport ? win.visualViewport.height : // $FlowFixMe[prop-missing]
					offsetParent[heightProp];
				y -= offsetY - popperRect.height;
				y *= gpuAcceleration ? 1 : -1;
			}

			if (placement === left || (placement === top || placement === bottom) && variation === end) {
				sideX = right;
				var offsetX = isFixed && offsetParent === win && win.visualViewport ? win.visualViewport.width : // $FlowFixMe[prop-missing]
					offsetParent[widthProp];
				x -= offsetX - popperRect.width;
				x *= gpuAcceleration ? 1 : -1;
			}
		}

		var commonStyles = Object.assign({
			position: position
		}, adaptive && unsetSides);

		var _ref4 = roundOffsets === true ? roundOffsetsByDPR({
			x: x,
			y: y
		}, getWindow(popper)) : {
			x: x,
			y: y
		};

		x = _ref4.x;
		y = _ref4.y;

		if (gpuAcceleration) {
			var _Object$assign;

			return Object.assign({}, commonStyles, (_Object$assign = {}, _Object$assign[sideY] = hasY ? '0' : '', _Object$assign[sideX] = hasX ? '0' : '', _Object$assign.transform = (win.devicePixelRatio || 1) <= 1 ? "translate(" + x + "px, " + y + "px)" : "translate3d(" + x + "px, " + y + "px, 0)", _Object$assign));
		}

		return Object.assign({}, commonStyles, (_Object$assign2 = {}, _Object$assign2[sideY] = hasY ? y + "px" : '', _Object$assign2[sideX] = hasX ? x + "px" : '', _Object$assign2.transform = '', _Object$assign2));
	}

	function computeStyles(_ref5) {
		var state = _ref5.state,
			options = _ref5.options;
		var _options$gpuAccelerat = options.gpuAcceleration,
			gpuAcceleration = _options$gpuAccelerat === void 0 ? true : _options$gpuAccelerat,
			_options$adaptive = options.adaptive,
			adaptive = _options$adaptive === void 0 ? true : _options$adaptive,
			_options$roundOffsets = options.roundOffsets,
			roundOffsets = _options$roundOffsets === void 0 ? true : _options$roundOffsets;
		var commonStyles = {
			placement: getBasePlacement(state.placement),
			variation: getVariation(state.placement),
			popper: state.elements.popper,
			popperRect: state.rects.popper,
			gpuAcceleration: gpuAcceleration,
			isFixed: state.options.strategy === 'fixed'
		};

		if (state.modifiersData.popperOffsets != null) {
			state.styles.popper = Object.assign({}, state.styles.popper, mapToStyles(Object.assign({}, commonStyles, {
				offsets: state.modifiersData.popperOffsets,
				position: state.options.strategy,
				adaptive: adaptive,
				roundOffsets: roundOffsets
			})));
		}

		if (state.modifiersData.arrow != null) {
			state.styles.arrow = Object.assign({}, state.styles.arrow, mapToStyles(Object.assign({}, commonStyles, {
				offsets: state.modifiersData.arrow,
				position: 'absolute',
				adaptive: false,
				roundOffsets: roundOffsets
			})));
		}

		state.attributes.popper = Object.assign({}, state.attributes.popper, {
			'data-popper-placement': state.placement
		});
	} // eslint-disable-next-line import/no-unused-modules


	var computeStyles$1 = {
		name: 'computeStyles',
		enabled: true,
		phase: 'beforeWrite',
		fn: computeStyles,
		data: {}
	};

	// and applies them to the HTMLElements such as popper and arrow

	function applyStyles(_ref) {
		var state = _ref.state;
		Object.keys(state.elements).forEach(function (name) {
			var style = state.styles[name] || {};
			var attributes = state.attributes[name] || {};
			var element = state.elements[name]; // arrow is optional + virtual elements

			if (!isHTMLElement(element) || !getNodeName(element)) {
				return;
			} // Flow doesn't support to extend this property, but it's the most
			// effective way to apply styles to an HTMLElement
			// $FlowFixMe[cannot-write]


			Object.assign(element.style, style);
			Object.keys(attributes).forEach(function (name) {
				var value = attributes[name];

				if (value === false) {
					element.removeAttribute(name);
				} else {
					element.setAttribute(name, value === true ? '' : value);
				}
			});
		});
	}

	function effect$1(_ref2) {
		var state = _ref2.state;
		var initialStyles = {
			popper: {
				position: state.options.strategy,
				left: '0',
				top: '0',
				margin: '0'
			},
			arrow: {
				position: 'absolute'
			},
			reference: {}
		};
		Object.assign(state.elements.popper.style, initialStyles.popper);
		state.styles = initialStyles;

		if (state.elements.arrow) {
			Object.assign(state.elements.arrow.style, initialStyles.arrow);
		}

		return function () {
			Object.keys(state.elements).forEach(function (name) {
				var element = state.elements[name];
				var attributes = state.attributes[name] || {};
				var styleProperties = Object.keys(state.styles.hasOwnProperty(name) ? state.styles[name] : initialStyles[name]); // Set all values to an empty string to unset them

				var style = styleProperties.reduce(function (style, property) {
					style[property] = '';
					return style;
				}, {}); // arrow is optional + virtual elements

				if (!isHTMLElement(element) || !getNodeName(element)) {
					return;
				}

				Object.assign(element.style, style);
				Object.keys(attributes).forEach(function (attribute) {
					element.removeAttribute(attribute);
				});
			});
		};
	} // eslint-disable-next-line import/no-unused-modules


	var applyStyles$1 = {
		name: 'applyStyles',
		enabled: true,
		phase: 'write',
		fn: applyStyles,
		effect: effect$1,
		requires: ['computeStyles']
	};

	function distanceAndSkiddingToXY(placement, rects, offset) {
		var basePlacement = getBasePlacement(placement);
		var invertDistance = [left, top].indexOf(basePlacement) >= 0 ? -1 : 1;

		var _ref = typeof offset === 'function' ? offset(Object.assign({}, rects, {
			placement: placement
		})) : offset,
			skidding = _ref[0],
			distance = _ref[1];

		skidding = skidding || 0;
		distance = (distance || 0) * invertDistance;
		return [left, right].indexOf(basePlacement) >= 0 ? {
			x: distance,
			y: skidding
		} : {
			x: skidding,
			y: distance
		};
	}

	function offset(_ref2) {
		var state = _ref2.state,
			options = _ref2.options,
			name = _ref2.name;
		var _options$offset = options.offset,
			offset = _options$offset === void 0 ? [0, 0] : _options$offset;
		var data = placements.reduce(function (acc, placement) {
			acc[placement] = distanceAndSkiddingToXY(placement, state.rects, offset);
			return acc;
		}, {});
		var _data$state$placement = data[state.placement],
			x = _data$state$placement.x,
			y = _data$state$placement.y;

		if (state.modifiersData.popperOffsets != null) {
			state.modifiersData.popperOffsets.x += x;
			state.modifiersData.popperOffsets.y += y;
		}

		state.modifiersData[name] = data;
	} // eslint-disable-next-line import/no-unused-modules


	var offset$1 = {
		name: 'offset',
		enabled: true,
		phase: 'main',
		requires: ['popperOffsets'],
		fn: offset
	};

	var hash$1 = {
		left: 'right',
		right: 'left',
		bottom: 'top',
		top: 'bottom'
	};
	function getOppositePlacement(placement) {
		return placement.replace(/left|right|bottom|top/g, function (matched) {
			return hash$1[matched];
		});
	}

	var hash = {
		start: 'end',
		end: 'start'
	};
	function getOppositeVariationPlacement(placement) {
		return placement.replace(/start|end/g, function (matched) {
			return hash[matched];
		});
	}

	function computeAutoPlacement(state, options) {
		if (options === void 0) {
			options = {};
		}

		var _options = options,
			placement = _options.placement,
			boundary = _options.boundary,
			rootBoundary = _options.rootBoundary,
			padding = _options.padding,
			flipVariations = _options.flipVariations,
			_options$allowedAutoP = _options.allowedAutoPlacements,
			allowedAutoPlacements = _options$allowedAutoP === void 0 ? placements : _options$allowedAutoP;
		var variation = getVariation(placement);
		var placements$1 = variation ? flipVariations ? variationPlacements : variationPlacements.filter(function (placement) {
			return getVariation(placement) === variation;
		}) : basePlacements;
		var allowedPlacements = placements$1.filter(function (placement) {
			return allowedAutoPlacements.indexOf(placement) >= 0;
		});

		if (allowedPlacements.length === 0) {
			allowedPlacements = placements$1;
		} // $FlowFixMe[incompatible-type]: Flow seems to have problems with two array unions...


		var overflows = allowedPlacements.reduce(function (acc, placement) {
			acc[placement] = detectOverflow(state, {
				placement: placement,
				boundary: boundary,
				rootBoundary: rootBoundary,
				padding: padding
			})[getBasePlacement(placement)];
			return acc;
		}, {});
		return Object.keys(overflows).sort(function (a, b) {
			return overflows[a] - overflows[b];
		});
	}

	function getExpandedFallbackPlacements(placement) {
		if (getBasePlacement(placement) === auto) {
			return [];
		}

		var oppositePlacement = getOppositePlacement(placement);
		return [getOppositeVariationPlacement(placement), oppositePlacement, getOppositeVariationPlacement(oppositePlacement)];
	}

	function flip(_ref) {
		var state = _ref.state,
			options = _ref.options,
			name = _ref.name;

		if (state.modifiersData[name]._skip) {
			return;
		}

		var _options$mainAxis = options.mainAxis,
			checkMainAxis = _options$mainAxis === void 0 ? true : _options$mainAxis,
			_options$altAxis = options.altAxis,
			checkAltAxis = _options$altAxis === void 0 ? true : _options$altAxis,
			specifiedFallbackPlacements = options.fallbackPlacements,
			padding = options.padding,
			boundary = options.boundary,
			rootBoundary = options.rootBoundary,
			altBoundary = options.altBoundary,
			_options$flipVariatio = options.flipVariations,
			flipVariations = _options$flipVariatio === void 0 ? true : _options$flipVariatio,
			allowedAutoPlacements = options.allowedAutoPlacements;
		var preferredPlacement = state.options.placement;
		var basePlacement = getBasePlacement(preferredPlacement);
		var isBasePlacement = basePlacement === preferredPlacement;
		var fallbackPlacements = specifiedFallbackPlacements || (isBasePlacement || !flipVariations ? [getOppositePlacement(preferredPlacement)] : getExpandedFallbackPlacements(preferredPlacement));
		var placements = [preferredPlacement].concat(fallbackPlacements).reduce(function (acc, placement) {
			return acc.concat(getBasePlacement(placement) === auto ? computeAutoPlacement(state, {
				placement: placement,
				boundary: boundary,
				rootBoundary: rootBoundary,
				padding: padding,
				flipVariations: flipVariations,
				allowedAutoPlacements: allowedAutoPlacements
			}) : placement);
		}, []);
		var referenceRect = state.rects.reference;
		var popperRect = state.rects.popper;
		var checksMap = new Map();
		var makeFallbackChecks = true;
		var firstFittingPlacement = placements[0];

		for (var i = 0; i < placements.length; i++) {
			var placement = placements[i];

			var _basePlacement = getBasePlacement(placement);

			var isStartVariation = getVariation(placement) === start;
			var isVertical = [top, bottom].indexOf(_basePlacement) >= 0;
			var len = isVertical ? 'width' : 'height';
			var overflow = detectOverflow(state, {
				placement: placement,
				boundary: boundary,
				rootBoundary: rootBoundary,
				altBoundary: altBoundary,
				padding: padding
			});
			var mainVariationSide = isVertical ? isStartVariation ? right : left : isStartVariation ? bottom : top;

			if (referenceRect[len] > popperRect[len]) {
				mainVariationSide = getOppositePlacement(mainVariationSide);
			}

			var altVariationSide = getOppositePlacement(mainVariationSide);
			var checks = [];

			if (checkMainAxis) {
				checks.push(overflow[_basePlacement] <= 0);
			}

			if (checkAltAxis) {
				checks.push(overflow[mainVariationSide] <= 0, overflow[altVariationSide] <= 0);
			}

			if (checks.every(function (check) {
				return check;
			})) {
				firstFittingPlacement = placement;
				makeFallbackChecks = false;
				break;
			}

			checksMap.set(placement, checks);
		}

		if (makeFallbackChecks) {
			// `2` may be desired in some cases – research later
			var numberOfChecks = flipVariations ? 3 : 1;

			var _loop = function _loop(_i) {
				var fittingPlacement = placements.find(function (placement) {
					var checks = checksMap.get(placement);

					if (checks) {
						return checks.slice(0, _i).every(function (check) {
							return check;
						});
					}
				});

				if (fittingPlacement) {
					firstFittingPlacement = fittingPlacement;
					return "break";
				}
			};

			for (var _i = numberOfChecks; _i > 0; _i--) {
				var _ret = _loop(_i);

				if (_ret === "break") break;
			}
		}

		if (state.placement !== firstFittingPlacement) {
			state.modifiersData[name]._skip = true;
			state.placement = firstFittingPlacement;
			state.reset = true;
		}
	} // eslint-disable-next-line import/no-unused-modules


	var flip$1 = {
		name: 'flip',
		enabled: true,
		phase: 'main',
		fn: flip,
		requiresIfExists: ['offset'],
		data: {
			_skip: false
		}
	};

	function getAltAxis(axis) {
		return axis === 'x' ? 'y' : 'x';
	}

	function within(min$1, value, max$1) {
		return max(min$1, min(value, max$1));
	}
	function withinMaxClamp(min, value, max) {
		var v = within(min, value, max);
		return v > max ? max : v;
	}

	function preventOverflow(_ref) {
		var state = _ref.state,
			options = _ref.options,
			name = _ref.name;
		var _options$mainAxis = options.mainAxis,
			checkMainAxis = _options$mainAxis === void 0 ? true : _options$mainAxis,
			_options$altAxis = options.altAxis,
			checkAltAxis = _options$altAxis === void 0 ? false : _options$altAxis,
			boundary = options.boundary,
			rootBoundary = options.rootBoundary,
			altBoundary = options.altBoundary,
			padding = options.padding,
			_options$tether = options.tether,
			tether = _options$tether === void 0 ? true : _options$tether,
			_options$tetherOffset = options.tetherOffset,
			tetherOffset = _options$tetherOffset === void 0 ? 0 : _options$tetherOffset;
		var overflow = detectOverflow(state, {
			boundary: boundary,
			rootBoundary: rootBoundary,
			padding: padding,
			altBoundary: altBoundary
		});
		var basePlacement = getBasePlacement(state.placement);
		var variation = getVariation(state.placement);
		var isBasePlacement = !variation;
		var mainAxis = getMainAxisFromPlacement(basePlacement);
		var altAxis = getAltAxis(mainAxis);
		var popperOffsets = state.modifiersData.popperOffsets;
		var referenceRect = state.rects.reference;
		var popperRect = state.rects.popper;
		var tetherOffsetValue = typeof tetherOffset === 'function' ? tetherOffset(Object.assign({}, state.rects, {
			placement: state.placement
		})) : tetherOffset;
		var normalizedTetherOffsetValue = typeof tetherOffsetValue === 'number' ? {
			mainAxis: tetherOffsetValue,
			altAxis: tetherOffsetValue
		} : Object.assign({
			mainAxis: 0,
			altAxis: 0
		}, tetherOffsetValue);
		var offsetModifierState = state.modifiersData.offset ? state.modifiersData.offset[state.placement] : null;
		var data = {
			x: 0,
			y: 0
		};

		if (!popperOffsets) {
			return;
		}

		if (checkMainAxis) {
			var _offsetModifierState$;

			var mainSide = mainAxis === 'y' ? top : left;
			var altSide = mainAxis === 'y' ? bottom : right;
			var len = mainAxis === 'y' ? 'height' : 'width';
			var offset = popperOffsets[mainAxis];
			var min$1 = offset + overflow[mainSide];
			var max$1 = offset - overflow[altSide];
			var additive = tether ? -popperRect[len] / 2 : 0;
			var minLen = variation === start ? referenceRect[len] : popperRect[len];
			var maxLen = variation === start ? -popperRect[len] : -referenceRect[len]; // We need to include the arrow in the calculation so the arrow doesn't go
			// outside the reference bounds

			var arrowElement = state.elements.arrow;
			var arrowRect = tether && arrowElement ? getLayoutRect(arrowElement) : {
				width: 0,
				height: 0
			};
			var arrowPaddingObject = state.modifiersData['arrow#persistent'] ? state.modifiersData['arrow#persistent'].padding : getFreshSideObject();
			var arrowPaddingMin = arrowPaddingObject[mainSide];
			var arrowPaddingMax = arrowPaddingObject[altSide]; // If the reference length is smaller than the arrow length, we don't want
			// to include its full size in the calculation. If the reference is small
			// and near the edge of a boundary, the popper can overflow even if the
			// reference is not overflowing as well (e.g. virtual elements with no
			// width or height)

			var arrowLen = within(0, referenceRect[len], arrowRect[len]);
			var minOffset = isBasePlacement ? referenceRect[len] / 2 - additive - arrowLen - arrowPaddingMin - normalizedTetherOffsetValue.mainAxis : minLen - arrowLen - arrowPaddingMin - normalizedTetherOffsetValue.mainAxis;
			var maxOffset = isBasePlacement ? -referenceRect[len] / 2 + additive + arrowLen + arrowPaddingMax + normalizedTetherOffsetValue.mainAxis : maxLen + arrowLen + arrowPaddingMax + normalizedTetherOffsetValue.mainAxis;
			var arrowOffsetParent = state.elements.arrow && getOffsetParent(state.elements.arrow);
			var clientOffset = arrowOffsetParent ? mainAxis === 'y' ? arrowOffsetParent.clientTop || 0 : arrowOffsetParent.clientLeft || 0 : 0;
			var offsetModifierValue = (_offsetModifierState$ = offsetModifierState == null ? void 0 : offsetModifierState[mainAxis]) != null ? _offsetModifierState$ : 0;
			var tetherMin = offset + minOffset - offsetModifierValue - clientOffset;
			var tetherMax = offset + maxOffset - offsetModifierValue;
			var preventedOffset = within(tether ? min(min$1, tetherMin) : min$1, offset, tether ? max(max$1, tetherMax) : max$1);
			popperOffsets[mainAxis] = preventedOffset;
			data[mainAxis] = preventedOffset - offset;
		}

		if (checkAltAxis) {
			var _offsetModifierState$2;

			var _mainSide = mainAxis === 'x' ? top : left;

			var _altSide = mainAxis === 'x' ? bottom : right;

			var _offset = popperOffsets[altAxis];

			var _len = altAxis === 'y' ? 'height' : 'width';

			var _min = _offset + overflow[_mainSide];

			var _max = _offset - overflow[_altSide];

			var isOriginSide = [top, left].indexOf(basePlacement) !== -1;

			var _offsetModifierValue = (_offsetModifierState$2 = offsetModifierState == null ? void 0 : offsetModifierState[altAxis]) != null ? _offsetModifierState$2 : 0;

			var _tetherMin = isOriginSide ? _min : _offset - referenceRect[_len] - popperRect[_len] - _offsetModifierValue + normalizedTetherOffsetValue.altAxis;

			var _tetherMax = isOriginSide ? _offset + referenceRect[_len] + popperRect[_len] - _offsetModifierValue - normalizedTetherOffsetValue.altAxis : _max;

			var _preventedOffset = tether && isOriginSide ? withinMaxClamp(_tetherMin, _offset, _tetherMax) : within(tether ? _tetherMin : _min, _offset, tether ? _tetherMax : _max);

			popperOffsets[altAxis] = _preventedOffset;
			data[altAxis] = _preventedOffset - _offset;
		}

		state.modifiersData[name] = data;
	} // eslint-disable-next-line import/no-unused-modules


	var preventOverflow$1 = {
		name: 'preventOverflow',
		enabled: true,
		phase: 'main',
		fn: preventOverflow,
		requiresIfExists: ['offset']
	};

	var toPaddingObject = function toPaddingObject(padding, state) {
		padding = typeof padding === 'function' ? padding(Object.assign({}, state.rects, {
			placement: state.placement
		})) : padding;
		return mergePaddingObject(typeof padding !== 'number' ? padding : expandToHashMap(padding, basePlacements));
	};

	function arrow(_ref) {
		var _state$modifiersData$;

		var state = _ref.state,
			name = _ref.name,
			options = _ref.options;
		var arrowElement = state.elements.arrow;
		var popperOffsets = state.modifiersData.popperOffsets;
		var basePlacement = getBasePlacement(state.placement);
		var axis = getMainAxisFromPlacement(basePlacement);
		var isVertical = [left, right].indexOf(basePlacement) >= 0;
		var len = isVertical ? 'height' : 'width';

		if (!arrowElement || !popperOffsets) {
			return;
		}

		var paddingObject = toPaddingObject(options.padding, state);
		var arrowRect = getLayoutRect(arrowElement);
		var minProp = axis === 'y' ? top : left;
		var maxProp = axis === 'y' ? bottom : right;
		var endDiff = state.rects.reference[len] + state.rects.reference[axis] - popperOffsets[axis] - state.rects.popper[len];
		var startDiff = popperOffsets[axis] - state.rects.reference[axis];
		var arrowOffsetParent = getOffsetParent(arrowElement);
		var clientSize = arrowOffsetParent ? axis === 'y' ? arrowOffsetParent.clientHeight || 0 : arrowOffsetParent.clientWidth || 0 : 0;
		var centerToReference = endDiff / 2 - startDiff / 2; // Make sure the arrow doesn't overflow the popper if the center point is
		// outside of the popper bounds

		var min = paddingObject[minProp];
		var max = clientSize - arrowRect[len] - paddingObject[maxProp];
		var center = clientSize / 2 - arrowRect[len] / 2 + centerToReference;
		var offset = within(min, center, max); // Prevents breaking syntax highlighting...

		var axisProp = axis;
		state.modifiersData[name] = (_state$modifiersData$ = {}, _state$modifiersData$[axisProp] = offset, _state$modifiersData$.centerOffset = offset - center, _state$modifiersData$);
	}

	function effect(_ref2) {
		var state = _ref2.state,
			options = _ref2.options;
		var _options$element = options.element,
			arrowElement = _options$element === void 0 ? '[data-popper-arrow]' : _options$element;

		if (arrowElement == null) {
			return;
		} // CSS selector


		if (typeof arrowElement === 'string') {
			arrowElement = state.elements.popper.querySelector(arrowElement);

			if (!arrowElement) {
				return;
			}
		}

		if (!contains(state.elements.popper, arrowElement)) {
			return;
		}

		state.elements.arrow = arrowElement;
	} // eslint-disable-next-line import/no-unused-modules


	var arrow$1 = {
		name: 'arrow',
		enabled: true,
		phase: 'main',
		fn: arrow,
		effect: effect,
		requires: ['popperOffsets'],
		requiresIfExists: ['preventOverflow']
	};

	function getSideOffsets(overflow, rect, preventedOffsets) {
		if (preventedOffsets === void 0) {
			preventedOffsets = {
				x: 0,
				y: 0
			};
		}

		return {
			top: overflow.top - rect.height - preventedOffsets.y,
			right: overflow.right - rect.width + preventedOffsets.x,
			bottom: overflow.bottom - rect.height + preventedOffsets.y,
			left: overflow.left - rect.width - preventedOffsets.x
		};
	}

	function isAnySideFullyClipped(overflow) {
		return [top, right, bottom, left].some(function (side) {
			return overflow[side] >= 0;
		});
	}

	function hide(_ref) {
		var state = _ref.state,
			name = _ref.name;
		var referenceRect = state.rects.reference;
		var popperRect = state.rects.popper;
		var preventedOffsets = state.modifiersData.preventOverflow;
		var referenceOverflow = detectOverflow(state, {
			elementContext: 'reference'
		});
		var popperAltOverflow = detectOverflow(state, {
			altBoundary: true
		});
		var referenceClippingOffsets = getSideOffsets(referenceOverflow, referenceRect);
		var popperEscapeOffsets = getSideOffsets(popperAltOverflow, popperRect, preventedOffsets);
		var isReferenceHidden = isAnySideFullyClipped(referenceClippingOffsets);
		var hasPopperEscaped = isAnySideFullyClipped(popperEscapeOffsets);
		state.modifiersData[name] = {
			referenceClippingOffsets: referenceClippingOffsets,
			popperEscapeOffsets: popperEscapeOffsets,
			isReferenceHidden: isReferenceHidden,
			hasPopperEscaped: hasPopperEscaped
		};
		state.attributes.popper = Object.assign({}, state.attributes.popper, {
			'data-popper-reference-hidden': isReferenceHidden,
			'data-popper-escaped': hasPopperEscaped
		});
	} // eslint-disable-next-line import/no-unused-modules


	var hide$1 = {
		name: 'hide',
		enabled: true,
		phase: 'main',
		requiresIfExists: ['preventOverflow'],
		fn: hide
	};

	var defaultModifiers$1 = [eventListeners, popperOffsets$1, computeStyles$1, applyStyles$1];
	var createPopper$1 = /*#__PURE__*/popperGenerator({
		defaultModifiers: defaultModifiers$1
	}); // eslint-disable-next-line import/no-unused-modules

	var defaultModifiers = [eventListeners, popperOffsets$1, computeStyles$1, applyStyles$1, offset$1, flip$1, preventOverflow$1, arrow$1, hide$1];
	var createPopper = /*#__PURE__*/popperGenerator({
		defaultModifiers: defaultModifiers
	}); // eslint-disable-next-line import/no-unused-modules

	exports.applyStyles = applyStyles$1;
	exports.arrow = arrow$1;
	exports.computeStyles = computeStyles$1;
	exports.createPopper = createPopper;
	exports.createPopperLite = createPopper$1;
	exports.defaultModifiers = defaultModifiers;
	exports.detectOverflow = detectOverflow;
	exports.eventListeners = eventListeners;
	exports.flip = flip$1;
	exports.hide = hide$1;
	exports.offset = offset$1;
	exports.popperGenerator = popperGenerator;
	exports.popperOffsets = popperOffsets$1;
	exports.preventOverflow = preventOverflow$1;

	Object.defineProperty(exports, '__esModule', { value: true });

})));
//# sourceMappingURL=popper.js.map





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
				debugger
			api.columns.adjust();
		 
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
	columns.forEach(c => {
		c.width = "150px"
	})
	let fetchUrl = "/System/FetchDataProfile";
	let deleteUrl = "/System/Remove";
	let searchBuilderCollapseId = $el.parent().parent().find("[data-place=searchBuilderCollapse]").attr("id");
	let dataProfileSelector = $el.closest(".card").find(`[data-action=dataProfile]`);

	var newPath = dataProfileSelector.data("new-path")
	var editPath = dataProfileSelector.data("edit-path")
	var deletePath = dataProfileSelector.data("delete-path")
	var exportPath = dataProfileSelector.data("exportExcell-path")


	let canNew = (newPath && newPath.length > 0);
	let canEdit = (editPath && editPath.length > 0);
	let canExportExcell = (exportPath && exportPath.length > 0);
	let canDelete = (deletePath && deletePath.length > 0);
	let selectedRow = {};
	let $selectedRowEl;

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
		<button ${canExportExcell == false ? "disabled" : ""}   class='btn-sm btn btn-icon   btn-color-success btn-active-icon-dark p-2' data-action='exportExcell' data-bs-toggle="tooltip" data-bs-placement="top" title="خروجی اکسل">
		    <i  class='fs-2 fa-light fa-file-xls'></i>

		</button>
	 
		<button ${canNew == false ? "disabled" : ""} class='btn-sm btn btn-icon btn-active-icon-dark btn-color-primary' data-action='new' data-bs-toggle="tooltip" data-bs-placement="top" title="جدید">
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
		$(document).on('click', function (e) {
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
				columnFilterPopups.forEach(function (popoverInstance) {
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
		if (currentSearch && currentSearch.length > 0) {
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

				currentSearch.toArray().forEach(function (val) {
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
							} catch (e) {
								console.warn('Error parsing from date:', e);
							}
						} else if (val.includes('to ')) {
							const dateStr = val.replace('to ', '');
							try {
								const date = new Date(dateStr);
								if (!isNaN(date.getTime())) {
									if (typeLower.includes('datetime')) {
										toDateTimeValue = MJUtil.miladiToShamsi(date.toISOString());
									} else {
										toDateValue = MJUtil.miladiToShamsi(date.toISOString());
									}
								}
							} catch (e) {
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
			$popup.find('.filter-date-from, .filter-date-to, .filter-datetime-from, .filter-datetime-to').each(function () {
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
		setTimeout(function () {
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
			setTimeout(function () {
				columnsBeingCleared.delete(columnIndex);
			}, 200);
		}, 150);
	}

	let tableApi = null;
	// Store row render callbacks
	const rowRenderCallbacks = [];

	var table = new DataTable($el, {
		rowCallback: function (row, data) {
			// Call all registered row render callbacks
			const $row = $(row);
			rowRenderCallbacks.forEach(function (callback) {
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
			api.onRowRender = function (callback) {
				if (typeof callback === 'function') {
					rowRenderCallbacks.push(callback);

					// Apply callback to existing rows immediately
					// This ensures callback works on first load as well
					api.rows().every(function () {
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
				$filterIcon[0].addEventListener('click', function (e) {
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
							columnFilterPopups.forEach(function (otherPopover, otherIndex) {
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
					content: function () {
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
					setTimeout(function () {
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
									onSelect: function () { }
								});

								// Set date value if exists
								if (fromValue) {
									try {
										const date = new Date(fromValue);
										if (!isNaN(date.getTime())) {
											setTimeout(function () {
												const fromPicker = $fromInput.data('pDatepicker');
												if (fromPicker && typeof fromPicker.setDate === 'function') {
													fromPicker.setDate(date.getTime());
												}
											}, 50);
										}
									} catch (e) {
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
									onSelect: function () { }
								});

								// Set date value if exists
								if (toValue) {
									try {
										const date = new Date(toValue);
										if (!isNaN(date.getTime())) {
											setTimeout(function () {
												const toPicker = $toInput.data('pDatepicker');
												if (toPicker && typeof toPicker.setDate === 'function') {
													toPicker.setDate(date.getTime());
												}
											}, 50);
										}
									} catch (e) {
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
									onSelect: function () { }
								});

								// Set datetime value if exists
								if (fromValue) {
									try {
										const date = new Date(fromValue);
										if (!isNaN(date.getTime())) {
											setTimeout(function () {
												const fromPicker = $fromInput.data('pDatepicker');
												if (fromPicker && typeof fromPicker.setDate === 'function') {
													fromPicker.setDate(date.getTime());
												}
											}, 50);
										}
									} catch (e) {
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
									onSelect: function () { }
								});

								// Set datetime value if exists
								if (toValue) {
									try {
										const date = new Date(toValue);
										if (!isNaN(date.getTime())) {
											setTimeout(function () {
												const toPicker = $toInput.data('pDatepicker');
												if (toPicker && typeof toPicker.setDate === 'function') {
													toPicker.setDate(date.getTime());
												}
											}, 50);
										}
									} catch (e) {
										console.warn('Error setting to datetime:', e);
									}
								}
							}
						}

						// Prevent clicks inside popover from closing it and triggering sort
						$popoverContent.on('click', function (e) {
							e.stopPropagation();
							e.stopImmediatePropagation();
						});

						// Also prevent clicks on popover container
						$(popoverElement).on('click', function (e) {
							e.stopPropagation();
						});

						// Prevent clicks on datepicker inputs from closing popover
						$popoverContent.on('click', '.filter-date-from, .filter-date-to, .filter-datetime-from, .filter-datetime-to', function (e) {
							e.stopPropagation();
						});

						// Bind filter apply button (use event delegation)
						$popoverContent.off('click', '.filter-apply').on('click', '.filter-apply', function (e) {
							e.preventDefault();
							e.stopPropagation();
							applyColumnFilter(columnIndex, column, columnType);
							if (popover) {
								popover.hide();
							}
						});

						// Bind filter clear button
						$popoverContent.off('click', '.filter-clear').on('click', '.filter-clear', function (e) {
							e.preventDefault();
							e.stopPropagation();
							clearColumnFilter(columnIndex, column);
							if (popover) {
								popover.hide();
							}
						});

						// Bind Enter key for text inputs
						$popoverContent.off('keypress', '.filter-input').on('keypress', '.filter-input', function (e) {
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

			 
			setTimeout(function () {
				api.columns.adjust();
			}, 200);

			 


		},
		drawCallback: function (settings) {
			var api = this.api();
			var $tableContainer = $(api.table().container());

			// استفاده از requestAnimationFrame برای اجرا دقیقاً قبل از Repaint مرورگر
			requestAnimationFrame(function () {
				// 1. تنظیم مجدد ستون‌ها
				api.columns.adjust();

				// 2. هک برای رفع باگ اسکرول‌بار در دیتاتیبل
				var body = $tableContainer.find('.dataTables_scrollBody');
				var head = $tableContainer.find('.dataTables_scrollHead');
				var headInner = head.find('.dataTables_scrollHeadInner');

				// همگام‌سازی دستی عرض هدر با بدنه
				if (body.width() > 0) {
					headInner.css('width', body.children('table').width());
					head.find('table').css('width', body.children('table').width());
				}

				// 3. اگر ColResize دارید، وضعیت اسکرول را چک کنید
				if (settings._colResize) {
					// گاهی اوقات پلاگین تغییر سایز، استایل‌های اضافی اعمال می‌کند که باید ریست شوند
					$(window).trigger('resize');
				}
			});
		},
		layout: {
			top2start: searchBuilderOnButton === false ? null : top2startInit,
			top2: searchBuilderOnButton === false ? top2startInit : null,
			top1start: top1startBtns,
			topEnd: null,
			bottomStart: [{ pageLength: { text: "  تعداد در هر صفحه _MENU_  ", class: "mx-11" } }, 'info'],
			bottomEnd: 'paging',
			top1End: null,
			topStart: null

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
		fixedColumns: false,
		scrollCollapse: true,
		scrollX: true,
		autoWidth: false,
		scrollY: '50vh',

	 

	});
	table.on("draw", function () {
		// Apply row render callbacks to all visible rows after draw
		// This ensures callbacks are applied even on first load and after every draw
		if (tableApi && rowRenderCallbacks.length > 0) {
			tableApi.rows({ page: 'current' }).every(function () {
				const rowData = this.data();
				const rowNode = this.node();
				if (rowNode) {
					const $row = $(rowNode);
					rowRenderCallbacks.forEach(function (callback) {
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
			columnReferences.forEach(function (column, columnIndex) {
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
							action: function () { }
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
function getColFilters(table) {
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
	 * @param {string} apiUrl - آدرس API partial view (مثال: '/Panel/Edms/Document/DocumentCommentPartial')
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
		if (address === "#") return;
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

				if (window.UpdateCurrentPage) {

					window.UpdateCurrentPage(page.address,page.title)
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

		if (window.UpdateCurrentPage) {

			window.ClosePage(page.address)
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
					$(currentTable.context[0].nTableWrapper).parent().append(`<table id="itemsTable" class="table table-rounded table-striped border table-bordered nowrap table-hover" style="width: 100%"></table>`)
					currentTable.destroy(true);
					currentTable = undefined;
				}
				else {
					currentTable = page.find('#itemsTable').DataTable();
					$(currentTable.context[0].nTableWrapper).parent().append(`<table id="itemsTable" class="table table-rounded table-striped border table-bordered nowrap table-hover" style="width: 100%"></table>`)
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
	    'jpg': 'ki-outline ki-picture fs-2x text-primary',
	    'jpeg': 'ki-outline ki-picture fs-2x text-primary',
	    'png': 'ki-outline ki-picture fs-2x text-primary',
	    'gif': 'ki-outline ki-picture fs-2x text-primary',
	    'txt': 'fas fa-file fs-2x text-muted',
	    'csv': 'fas fa-file-csv fs-2x text-info',
	    'xml': 'fas fa-file fs-2x text-info',
	    'html': 'fas fa-file fs-2x text-warning',
	    'mp3': 'fas fa-file fs-2x text-primary',
	    'mp4': 'fas fa-file fs-2x text-primary',
	    'avi': 'fas fa-file fs-2x text-primary'
    };
    
	return iconMap[ext] || 'ki-outline ki-file fs-2x text-primary';
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
		 
		
		if (!isSameOrigin(href)) return;

		if (href.startsWith("/File/download")) {

			return;
		}
		e.preventDefault();
		if (!href || href.startsWith("javascript:") || href.startsWith("#") || $a.closest("#pageMenuBuilder").length > 0) return;


		if (e.ctrlKey || e.metaKey || e.shiftKey || $a.attr("target") === "_blank") return;

	
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



/*! Select2 4.1.0-rc.0 | https://github.com/select2/select2/blob/master/LICENSE.md */
!function (n) { "function" == typeof define && define.amd ? define(["jquery"], n) : "object" == typeof module && module.exports ? module.exports = function (e, t) { return void 0 === t && (t = "undefined" != typeof window ? require("jquery") : require("jquery")(e)), n(t), t } : n(jQuery) }(function (t) { var e, n, s, p, r, o, h, f, g, m, y, v, i, a, _, s = ((u = t && t.fn && t.fn.select2 && t.fn.select2.amd ? t.fn.select2.amd : u) && u.requirejs || (u ? n = u : u = {}, g = {}, m = {}, y = {}, v = {}, i = Object.prototype.hasOwnProperty, a = [].slice, _ = /\.js$/, h = function (e, t) { var n, s, i = c(e), r = i[0], t = t[1]; return e = i[1], r && (n = x(r = l(r, t))), r ? e = n && n.normalize ? n.normalize(e, (s = t, function (e) { return l(e, s) })) : l(e, t) : (r = (i = c(e = l(e, t)))[0], e = i[1], r && (n = x(r))), { f: r ? r + "!" + e : e, n: e, pr: r, p: n } }, f = { require: function (e) { return w(e) }, exports: function (e) { var t = g[e]; return void 0 !== t ? t : g[e] = {} }, module: function (e) { return { id: e, uri: "", exports: g[e], config: (t = e, function () { return y && y.config && y.config[t] || {} }) }; var t } }, r = function (e, t, n, s) { var i, r, o, a, l, c = [], u = typeof n, d = A(s = s || e); if ("undefined" == u || "function" == u) { for (t = !t.length && n.length ? ["require", "exports", "module"] : t, a = 0; a < t.length; a += 1)if ("require" === (r = (o = h(t[a], d)).f)) c[a] = f.require(e); else if ("exports" === r) c[a] = f.exports(e), l = !0; else if ("module" === r) i = c[a] = f.module(e); else if (b(g, r) || b(m, r) || b(v, r)) c[a] = x(r); else { if (!o.p) throw new Error(e + " missing " + r); o.p.load(o.n, w(s, !0), function (t) { return function (e) { g[t] = e } }(r), {}), c[a] = g[r] } u = n ? n.apply(g[e], c) : void 0, e && (i && i.exports !== p && i.exports !== g[e] ? g[e] = i.exports : u === p && l || (g[e] = u)) } else e && (g[e] = n) }, e = n = o = function (e, t, n, s, i) { if ("string" == typeof e) return f[e] ? f[e](t) : x(h(e, A(t)).f); if (!e.splice) { if ((y = e).deps && o(y.deps, y.callback), !t) return; t.splice ? (e = t, t = n, n = null) : e = p } return t = t || function () { }, "function" == typeof n && (n = s, s = i), s ? r(p, e, t, n) : setTimeout(function () { r(p, e, t, n) }, 4), o }, o.config = function (e) { return o(e) }, e._defined = g, (s = function (e, t, n) { if ("string" != typeof e) throw new Error("See almond README: incorrect module build, no module name"); t.splice || (n = t, t = []), b(g, e) || b(m, e) || (m[e] = [e, t, n]) }).amd = { jQuery: !0 }, u.requirejs = e, u.require = n, u.define = s), u.define("almond", function () { }), u.define("jquery", [], function () { var e = t || $; return null == e && console && console.error && console.error("Select2: An instance of jQuery or a jQuery-compatible library was not found. Make sure that you are including jQuery before Select2 on your web page."), e }), u.define("select2/utils", ["jquery"], function (r) { var s = {}; function c(e) { var t, n = e.prototype, s = []; for (t in n) "function" == typeof n[t] && "constructor" !== t && s.push(t); return s } s.Extend = function (e, t) { var n, s = {}.hasOwnProperty; function i() { this.constructor = e } for (n in t) s.call(t, n) && (e[n] = t[n]); return i.prototype = t.prototype, e.prototype = new i, e.__super__ = t.prototype, e }, s.Decorate = function (s, i) { var e = c(i), t = c(s); function r() { var e = Array.prototype.unshift, t = i.prototype.constructor.length, n = s.prototype.constructor; 0 < t && (e.call(arguments, s.prototype.constructor), n = i.prototype.constructor), n.apply(this, arguments) } i.displayName = s.displayName, r.prototype = new function () { this.constructor = r }; for (var n = 0; n < t.length; n++) { var o = t[n]; r.prototype[o] = s.prototype[o] } for (var a = 0; a < e.length; a++) { var l = e[a]; r.prototype[l] = function (e) { var t = function () { }; e in r.prototype && (t = r.prototype[e]); var n = i.prototype[e]; return function () { return Array.prototype.unshift.call(arguments, t), n.apply(this, arguments) } }(l) } return r }; function e() { this.listeners = {} } e.prototype.on = function (e, t) { this.listeners = this.listeners || {}, e in this.listeners ? this.listeners[e].push(t) : this.listeners[e] = [t] }, e.prototype.trigger = function (e) { var t = Array.prototype.slice, n = t.call(arguments, 1); this.listeners = this.listeners || {}, 0 === (n = null == n ? [] : n).length && n.push({}), (n[0]._type = e) in this.listeners && this.invoke(this.listeners[e], t.call(arguments, 1)), "*" in this.listeners && this.invoke(this.listeners["*"], arguments) }, e.prototype.invoke = function (e, t) { for (var n = 0, s = e.length; n < s; n++)e[n].apply(this, t) }, s.Observable = e, s.generateChars = function (e) { for (var t = "", n = 0; n < e; n++)t += Math.floor(36 * Math.random()).toString(36); return t }, s.bind = function (e, t) { return function () { e.apply(t, arguments) } }, s._convertData = function (e) { for (var t in e) { var n = t.split("-"), s = e; if (1 !== n.length) { for (var i = 0; i < n.length; i++) { var r = n[i]; (r = r.substring(0, 1).toLowerCase() + r.substring(1)) in s || (s[r] = {}), i == n.length - 1 && (s[r] = e[t]), s = s[r] } delete e[t] } } return e }, s.hasScroll = function (e, t) { var n = r(t), s = t.style.overflowX, i = t.style.overflowY; return (s !== i || "hidden" !== i && "visible" !== i) && ("scroll" === s || "scroll" === i || (n.innerHeight() < t.scrollHeight || n.innerWidth() < t.scrollWidth)) }, s.escapeMarkup = function (e) { var t = { "\\": "&#92;", "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;", "/": "&#47;" }; return "string" != typeof e ? e : String(e).replace(/[&<>"'\/\\]/g, function (e) { return t[e] }) }, s.__cache = {}; var n = 0; return s.GetUniqueElementId = function (e) { var t = e.getAttribute("data-select2-id"); return null != t || (t = e.id ? "select2-data-" + e.id : "select2-data-" + (++n).toString() + "-" + s.generateChars(4), e.setAttribute("data-select2-id", t)), t }, s.StoreData = function (e, t, n) { e = s.GetUniqueElementId(e); s.__cache[e] || (s.__cache[e] = {}), s.__cache[e][t] = n }, s.GetData = function (e, t) { var n = s.GetUniqueElementId(e); return t ? s.__cache[n] && null != s.__cache[n][t] ? s.__cache[n][t] : r(e).data(t) : s.__cache[n] }, s.RemoveData = function (e) { var t = s.GetUniqueElementId(e); null != s.__cache[t] && delete s.__cache[t], e.removeAttribute("data-select2-id") }, s.copyNonInternalCssClasses = function (e, t) { var n = (n = e.getAttribute("class").trim().split(/\s+/)).filter(function (e) { return 0 === e.indexOf("select2-") }), t = (t = t.getAttribute("class").trim().split(/\s+/)).filter(function (e) { return 0 !== e.indexOf("select2-") }), t = n.concat(t); e.setAttribute("class", t.join(" ")) }, s }), u.define("select2/results", ["jquery", "./utils"], function (d, p) { function s(e, t, n) { this.$element = e, this.data = n, this.options = t, s.__super__.constructor.call(this) } return p.Extend(s, p.Observable), s.prototype.render = function () { var e = d('<ul class="select2-results__options" role="listbox"></ul>'); return this.options.get("multiple") && e.attr("aria-multiselectable", "true"), this.$results = e }, s.prototype.clear = function () { this.$results.empty() }, s.prototype.displayMessage = function (e) { var t = this.options.get("escapeMarkup"); this.clear(), this.hideLoading(); var n = d('<li role="alert" aria-live="assertive" class="select2-results__option"></li>'), s = this.options.get("translations").get(e.message); n.append(t(s(e.args))), n[0].className += " select2-results__message", this.$results.append(n) }, s.prototype.hideMessages = function () { this.$results.find(".select2-results__message").remove() }, s.prototype.append = function (e) { this.hideLoading(); var t = []; if (null != e.results && 0 !== e.results.length) { e.results = this.sort(e.results); for (var n = 0; n < e.results.length; n++) { var s = e.results[n], s = this.option(s); t.push(s) } this.$results.append(t) } else 0 === this.$results.children().length && this.trigger("results:message", { message: "noResults" }) }, s.prototype.position = function (e, t) { t.find(".select2-results").append(e) }, s.prototype.sort = function (e) { return this.options.get("sorter")(e) }, s.prototype.highlightFirstItem = function () { var e = this.$results.find(".select2-results__option--selectable"), t = e.filter(".select2-results__option--selected"); (0 < t.length ? t : e).first().trigger("mouseenter"), this.ensureHighlightVisible() }, s.prototype.setClasses = function () { var t = this; this.data.current(function (e) { var s = e.map(function (e) { return e.id.toString() }); t.$results.find(".select2-results__option--selectable").each(function () { var e = d(this), t = p.GetData(this, "data"), n = "" + t.id; null != t.element && t.element.selected || null == t.element && -1 < s.indexOf(n) ? (this.classList.add("select2-results__option--selected"), e.attr("aria-selected", "true")) : (this.classList.remove("select2-results__option--selected"), e.attr("aria-selected", "false")) }) }) }, s.prototype.showLoading = function (e) { this.hideLoading(); e = { disabled: !0, loading: !0, text: this.options.get("translations").get("searching")(e) }, e = this.option(e); e.className += " loading-results", this.$results.prepend(e) }, s.prototype.hideLoading = function () { this.$results.find(".loading-results").remove() }, s.prototype.option = function (e) { var t = document.createElement("li"); t.classList.add("select2-results__option"), t.classList.add("select2-results__option--selectable"); var n, s = { role: "option" }, i = window.Element.prototype.matches || window.Element.prototype.msMatchesSelector || window.Element.prototype.webkitMatchesSelector; for (n in (null != e.element && i.call(e.element, ":disabled") || null == e.element && e.disabled) && (s["aria-disabled"] = "true", t.classList.remove("select2-results__option--selectable"), t.classList.add("select2-results__option--disabled")), null == e.id && t.classList.remove("select2-results__option--selectable"), null != e._resultId && (t.id = e._resultId), e.title && (t.title = e.title), e.children && (s.role = "group", s["aria-label"] = e.text, t.classList.remove("select2-results__option--selectable"), t.classList.add("select2-results__option--group")), s) { var r = s[n]; t.setAttribute(n, r) } if (e.children) { var o = d(t), a = document.createElement("strong"); a.className = "select2-results__group", this.template(e, a); for (var l = [], c = 0; c < e.children.length; c++) { var u = e.children[c], u = this.option(u); l.push(u) } i = d("<ul></ul>", { class: "select2-results__options select2-results__options--nested", role: "none" }); i.append(l), o.append(a), o.append(i) } else this.template(e, t); return p.StoreData(t, "data", e), t }, s.prototype.bind = function (t, e) { var i = this, n = t.id + "-results"; this.$results.attr("id", n), t.on("results:all", function (e) { i.clear(), i.append(e.data), t.isOpen() && (i.setClasses(), i.highlightFirstItem()) }), t.on("results:append", function (e) { i.append(e.data), t.isOpen() && i.setClasses() }), t.on("query", function (e) { i.hideMessages(), i.showLoading(e) }), t.on("select", function () { t.isOpen() && (i.setClasses(), i.options.get("scrollAfterSelect") && i.highlightFirstItem()) }), t.on("unselect", function () { t.isOpen() && (i.setClasses(), i.options.get("scrollAfterSelect") && i.highlightFirstItem()) }), t.on("open", function () { i.$results.attr("aria-expanded", "true"), i.$results.attr("aria-hidden", "false"), i.setClasses(), i.ensureHighlightVisible() }), t.on("close", function () { i.$results.attr("aria-expanded", "false"), i.$results.attr("aria-hidden", "true"), i.$results.removeAttr("aria-activedescendant") }), t.on("results:toggle", function () { var e = i.getHighlightedResults(); 0 !== e.length && e.trigger("mouseup") }), t.on("results:select", function () { var e, t = i.getHighlightedResults(); 0 !== t.length && (e = p.GetData(t[0], "data"), t.hasClass("select2-results__option--selected") ? i.trigger("close", {}) : i.trigger("select", { data: e })) }), t.on("results:previous", function () { var e, t = i.getHighlightedResults(), n = i.$results.find(".select2-results__option--selectable"), s = n.index(t); s <= 0 || (e = s - 1, 0 === t.length && (e = 0), (s = n.eq(e)).trigger("mouseenter"), t = i.$results.offset().top, n = s.offset().top, s = i.$results.scrollTop() + (n - t), 0 === e ? i.$results.scrollTop(0) : n - t < 0 && i.$results.scrollTop(s)) }), t.on("results:next", function () { var e, t = i.getHighlightedResults(), n = i.$results.find(".select2-results__option--selectable"), s = n.index(t) + 1; s >= n.length || ((e = n.eq(s)).trigger("mouseenter"), t = i.$results.offset().top + i.$results.outerHeight(!1), n = e.offset().top + e.outerHeight(!1), e = i.$results.scrollTop() + n - t, 0 === s ? i.$results.scrollTop(0) : t < n && i.$results.scrollTop(e)) }), t.on("results:focus", function (e) { e.element[0].classList.add("select2-results__option--highlighted"), e.element[0].setAttribute("aria-selected", "true") }), t.on("results:message", function (e) { i.displayMessage(e) }), d.fn.mousewheel && this.$results.on("mousewheel", function (e) { var t = i.$results.scrollTop(), n = i.$results.get(0).scrollHeight - t + e.deltaY, t = 0 < e.deltaY && t - e.deltaY <= 0, n = e.deltaY < 0 && n <= i.$results.height(); t ? (i.$results.scrollTop(0), e.preventDefault(), e.stopPropagation()) : n && (i.$results.scrollTop(i.$results.get(0).scrollHeight - i.$results.height()), e.preventDefault(), e.stopPropagation()) }), this.$results.on("mouseup", ".select2-results__option--selectable", function (e) { var t = d(this), n = p.GetData(this, "data"); t.hasClass("select2-results__option--selected") ? i.options.get("multiple") ? i.trigger("unselect", { originalEvent: e, data: n }) : i.trigger("close", {}) : i.trigger("select", { originalEvent: e, data: n }) }), this.$results.on("mouseenter", ".select2-results__option--selectable", function (e) { var t = p.GetData(this, "data"); i.getHighlightedResults().removeClass("select2-results__option--highlighted").attr("aria-selected", "false"), i.trigger("results:focus", { data: t, element: d(this) }) }) }, s.prototype.getHighlightedResults = function () { return this.$results.find(".select2-results__option--highlighted") }, s.prototype.destroy = function () { this.$results.remove() }, s.prototype.ensureHighlightVisible = function () { var e, t, n, s, i = this.getHighlightedResults(); 0 !== i.length && (e = this.$results.find(".select2-results__option--selectable").index(i), s = this.$results.offset().top, t = i.offset().top, n = this.$results.scrollTop() + (t - s), s = t - s, n -= 2 * i.outerHeight(!1), e <= 2 ? this.$results.scrollTop(0) : (s > this.$results.outerHeight() || s < 0) && this.$results.scrollTop(n)) }, s.prototype.template = function (e, t) { var n = this.options.get("templateResult"), s = this.options.get("escapeMarkup"), e = n(e, t); null == e ? t.style.display = "none" : "string" == typeof e ? t.innerHTML = s(e) : d(t).append(e) }, s }), u.define("select2/keys", [], function () { return { BACKSPACE: 8, TAB: 9, ENTER: 13, SHIFT: 16, CTRL: 17, ALT: 18, ESC: 27, SPACE: 32, PAGE_UP: 33, PAGE_DOWN: 34, END: 35, HOME: 36, LEFT: 37, UP: 38, RIGHT: 39, DOWN: 40, DELETE: 46 } }), u.define("select2/selection/base", ["jquery", "../utils", "../keys"], function (n, s, i) { function r(e, t) { this.$element = e, this.options = t, r.__super__.constructor.call(this) } return s.Extend(r, s.Observable), r.prototype.render = function () { var e = n('<span class="select2-selection" role="combobox"  aria-haspopup="true" aria-expanded="false"></span>'); return this._tabindex = 0, null != s.GetData(this.$element[0], "old-tabindex") ? this._tabindex = s.GetData(this.$element[0], "old-tabindex") : null != this.$element.attr("tabindex") && (this._tabindex = this.$element.attr("tabindex")), e.attr("title", this.$element.attr("title")), e.attr("tabindex", this._tabindex), e.attr("aria-disabled", "false"), this.$selection = e }, r.prototype.bind = function (e, t) { var n = this, s = e.id + "-results"; this.container = e, this.$selection.on("focus", function (e) { n.trigger("focus", e) }), this.$selection.on("blur", function (e) { n._handleBlur(e) }), this.$selection.on("keydown", function (e) { n.trigger("keypress", e), e.which === i.SPACE && e.preventDefault() }), e.on("results:focus", function (e) { n.$selection.attr("aria-activedescendant", e.data._resultId) }), e.on("selection:update", function (e) { n.update(e.data) }), e.on("open", function () { n.$selection.attr("aria-expanded", "true"), n.$selection.attr("aria-owns", s), n._attachCloseHandler(e) }), e.on("close", function () { n.$selection.attr("aria-expanded", "false"), n.$selection.removeAttr("aria-activedescendant"), n.$selection.removeAttr("aria-owns"), n.$selection.trigger("focus"), n._detachCloseHandler(e) }), e.on("enable", function () { n.$selection.attr("tabindex", n._tabindex), n.$selection.attr("aria-disabled", "false") }), e.on("disable", function () { n.$selection.attr("tabindex", "-1"), n.$selection.attr("aria-disabled", "true") }) }, r.prototype._handleBlur = function (e) { var t = this; window.setTimeout(function () { document.activeElement == t.$selection[0] || n.contains(t.$selection[0], document.activeElement) || t.trigger("blur", e) }, 1) }, r.prototype._attachCloseHandler = function (e) { n(document.body).on("mousedown.select2." + e.id, function (e) { var t = n(e.target).closest(".select2"); n(".select2.select2-container--open").each(function () { this != t[0] && s.GetData(this, "element").select2("close") }) }) }, r.prototype._detachCloseHandler = function (e) { n(document.body).off("mousedown.select2." + e.id) }, r.prototype.position = function (e, t) { t.find(".selection").append(e) }, r.prototype.destroy = function () { this._detachCloseHandler(this.container) }, r.prototype.update = function (e) { throw new Error("The `update` method must be defined in child classes.") }, r.prototype.isEnabled = function () { return !this.isDisabled() }, r.prototype.isDisabled = function () { return this.options.get("disabled") }, r }), u.define("select2/selection/single", ["jquery", "./base", "../utils", "../keys"], function (e, t, n, s) { function i() { i.__super__.constructor.apply(this, arguments) } return n.Extend(i, t), i.prototype.render = function () { var e = i.__super__.render.call(this); return e[0].classList.add("select2-selection--single"), e.html('<span class="select2-selection__rendered"></span><span class="select2-selection__arrow" role="presentation"><b role="presentation"></b></span>'), e }, i.prototype.bind = function (t, e) { var n = this; i.__super__.bind.apply(this, arguments); var s = t.id + "-container"; this.$selection.find(".select2-selection__rendered").attr("id", s).attr("role", "textbox").attr("aria-readonly", "true"), this.$selection.attr("aria-labelledby", s), this.$selection.attr("aria-controls", s), this.$selection.on("mousedown", function (e) { 1 === e.which && n.trigger("toggle", { originalEvent: e }) }), this.$selection.on("focus", function (e) { }), this.$selection.on("blur", function (e) { }), t.on("focus", function (e) { t.isOpen() || n.$selection.trigger("focus") }) }, i.prototype.clear = function () { var e = this.$selection.find(".select2-selection__rendered"); e.empty(), e.removeAttr("title") }, i.prototype.display = function (e, t) { var n = this.options.get("templateSelection"); return this.options.get("escapeMarkup")(n(e, t)) }, i.prototype.selectionContainer = function () { return e("<span></span>") }, i.prototype.update = function (e) { var t, n; 0 !== e.length ? (n = e[0], t = this.$selection.find(".select2-selection__rendered"), e = this.display(n, t), t.empty().append(e), (n = n.title || n.text) ? t.attr("title", n) : t.removeAttr("title")) : this.clear() }, i }), u.define("select2/selection/multiple", ["jquery", "./base", "../utils"], function (i, e, c) { function r(e, t) { r.__super__.constructor.apply(this, arguments) } return c.Extend(r, e), r.prototype.render = function () { var e = r.__super__.render.call(this); return e[0].classList.add("select2-selection--multiple"), e.html('<ul class="select2-selection__rendered"></ul>'), e }, r.prototype.bind = function (e, t) { var n = this; r.__super__.bind.apply(this, arguments); var s = e.id + "-container"; this.$selection.find(".select2-selection__rendered").attr("id", s), this.$selection.on("click", function (e) { n.trigger("toggle", { originalEvent: e }) }), this.$selection.on("click", ".select2-selection__choice__remove", function (e) { var t; n.isDisabled() || (t = i(this).parent(), t = c.GetData(t[0], "data"), n.trigger("unselect", { originalEvent: e, data: t })) }), this.$selection.on("keydown", ".select2-selection__choice__remove", function (e) { n.isDisabled() || e.stopPropagation() }) }, r.prototype.clear = function () { var e = this.$selection.find(".select2-selection__rendered"); e.empty(), e.removeAttr("title") }, r.prototype.display = function (e, t) { var n = this.options.get("templateSelection"); return this.options.get("escapeMarkup")(n(e, t)) }, r.prototype.selectionContainer = function () { return i('<li class="select2-selection__choice"><button type="button" class="select2-selection__choice__remove" tabindex="-1"><span aria-hidden="true">&times;</span></button><span class="select2-selection__choice__display"></span></li>') }, r.prototype.update = function (e) { if (this.clear(), 0 !== e.length) { for (var t = [], n = this.$selection.find(".select2-selection__rendered").attr("id") + "-choice-", s = 0; s < e.length; s++) { var i = e[s], r = this.selectionContainer(), o = this.display(i, r), a = n + c.generateChars(4) + "-"; i.id ? a += i.id : a += c.generateChars(4), r.find(".select2-selection__choice__display").append(o).attr("id", a); var l = i.title || i.text; l && r.attr("title", l); o = this.options.get("translations").get("removeItem"), l = r.find(".select2-selection__choice__remove"); l.attr("title", o()), l.attr("aria-label", o()), l.attr("aria-describedby", a), c.StoreData(r[0], "data", i), t.push(r) } this.$selection.find(".select2-selection__rendered").append(t) } }, r }), u.define("select2/selection/placeholder", [], function () { function e(e, t, n) { this.placeholder = this.normalizePlaceholder(n.get("placeholder")), e.call(this, t, n) } return e.prototype.normalizePlaceholder = function (e, t) { return t = "string" == typeof t ? { id: "", text: t } : t }, e.prototype.createPlaceholder = function (e, t) { var n = this.selectionContainer(); n.html(this.display(t)), n[0].classList.add("select2-selection__placeholder"), n[0].classList.remove("select2-selection__choice"); t = t.title || t.text || n.text(); return this.$selection.find(".select2-selection__rendered").attr("title", t), n }, e.prototype.update = function (e, t) { var n = 1 == t.length && t[0].id != this.placeholder.id; if (1 < t.length || n) return e.call(this, t); this.clear(); t = this.createPlaceholder(this.placeholder); this.$selection.find(".select2-selection__rendered").append(t) }, e }), u.define("select2/selection/allowClear", ["jquery", "../keys", "../utils"], function (i, s, a) { function e() { } return e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), null == this.placeholder && this.options.get("debug") && window.console && console.error && console.error("Select2: The `allowClear` option should be used in combination with the `placeholder` option."), this.$selection.on("mousedown", ".select2-selection__clear", function (e) { s._handleClear(e) }), t.on("keypress", function (e) { s._handleKeyboardClear(e, t) }) }, e.prototype._handleClear = function (e, t) { if (!this.isDisabled()) { var n = this.$selection.find(".select2-selection__clear"); if (0 !== n.length) { t.stopPropagation(); var s = a.GetData(n[0], "data"), i = this.$element.val(); this.$element.val(this.placeholder.id); var r = { data: s }; if (this.trigger("clear", r), r.prevented) this.$element.val(i); else { for (var o = 0; o < s.length; o++)if (r = { data: s[o] }, this.trigger("unselect", r), r.prevented) return void this.$element.val(i); this.$element.trigger("input").trigger("change"), this.trigger("toggle", {}) } } } }, e.prototype._handleKeyboardClear = function (e, t, n) { n.isOpen() || t.which != s.DELETE && t.which != s.BACKSPACE || this._handleClear(t) }, e.prototype.update = function (e, t) { var n, s; e.call(this, t), this.$selection.find(".select2-selection__clear").remove(), this.$selection[0].classList.remove("select2-selection--clearable"), 0 < this.$selection.find(".select2-selection__placeholder").length || 0 === t.length || (n = this.$selection.find(".select2-selection__rendered").attr("id"), s = this.options.get("translations").get("removeAllItems"), (e = i('<button type="button" class="select2-selection__clear" tabindex="-1"><span aria-hidden="true">&times;</span></button>')).attr("title", s()), e.attr("aria-label", s()), e.attr("aria-describedby", n), a.StoreData(e[0], "data", t), this.$selection.prepend(e), this.$selection[0].classList.add("select2-selection--clearable")) }, e }), u.define("select2/selection/search", ["jquery", "../utils", "../keys"], function (s, a, l) { function e(e, t, n) { e.call(this, t, n) } return e.prototype.render = function (e) { var t = this.options.get("translations").get("search"), n = s('<span class="select2-search select2-search--inline"><textarea class="select2-search__field" type="search" tabindex="-1" autocorrect="off" autocapitalize="none" spellcheck="false" role="searchbox" aria-autocomplete="list" ></textarea></span>'); this.$searchContainer = n, this.$search = n.find("textarea"), this.$search.prop("autocomplete", this.options.get("autocomplete")), this.$search.attr("aria-label", t()); e = e.call(this); return this._transferTabIndex(), e.append(this.$searchContainer), e }, e.prototype.bind = function (e, t, n) { var s = this, i = t.id + "-results", r = t.id + "-container"; e.call(this, t, n), s.$search.attr("aria-describedby", r), t.on("open", function () { s.$search.attr("aria-controls", i), s.$search.trigger("focus") }), t.on("close", function () { s.$search.val(""), s.resizeSearch(), s.$search.removeAttr("aria-controls"), s.$search.removeAttr("aria-activedescendant"), s.$search.trigger("focus") }), t.on("enable", function () { s.$search.prop("disabled", !1), s._transferTabIndex() }), t.on("disable", function () { s.$search.prop("disabled", !0) }), t.on("focus", function (e) { s.$search.trigger("focus") }), t.on("results:focus", function (e) { e.data._resultId ? s.$search.attr("aria-activedescendant", e.data._resultId) : s.$search.removeAttr("aria-activedescendant") }), this.$selection.on("focusin", ".select2-search--inline", function (e) { s.trigger("focus", e) }), this.$selection.on("focusout", ".select2-search--inline", function (e) { s._handleBlur(e) }), this.$selection.on("keydown", ".select2-search--inline", function (e) { var t; e.stopPropagation(), s.trigger("keypress", e), s._keyUpPrevented = e.isDefaultPrevented(), e.which !== l.BACKSPACE || "" !== s.$search.val() || 0 < (t = s.$selection.find(".select2-selection__choice").last()).length && (t = a.GetData(t[0], "data"), s.searchRemoveChoice(t), e.preventDefault()) }), this.$selection.on("click", ".select2-search--inline", function (e) { s.$search.val() && e.stopPropagation() }); var t = document.documentMode, o = t && t <= 11; this.$selection.on("input.searchcheck", ".select2-search--inline", function (e) { o ? s.$selection.off("input.search input.searchcheck") : s.$selection.off("keyup.search") }), this.$selection.on("keyup.search input.search", ".select2-search--inline", function (e) { var t; o && "input" === e.type ? s.$selection.off("input.search input.searchcheck") : (t = e.which) != l.SHIFT && t != l.CTRL && t != l.ALT && t != l.TAB && s.handleSearch(e) }) }, e.prototype._transferTabIndex = function (e) { this.$search.attr("tabindex", this.$selection.attr("tabindex")), this.$selection.attr("tabindex", "-1") }, e.prototype.createPlaceholder = function (e, t) { this.$search.attr("placeholder", t.text) }, e.prototype.update = function (e, t) { var n = this.$search[0] == document.activeElement; this.$search.attr("placeholder", ""), e.call(this, t), this.resizeSearch(), n && this.$search.trigger("focus") }, e.prototype.handleSearch = function () { var e; this.resizeSearch(), this._keyUpPrevented || (e = this.$search.val(), this.trigger("query", { term: e })), this._keyUpPrevented = !1 }, e.prototype.searchRemoveChoice = function (e, t) { this.trigger("unselect", { data: t }), this.$search.val(t.text), this.handleSearch() }, e.prototype.resizeSearch = function () { this.$search.css("width", "25px"); var e = "100%"; "" === this.$search.attr("placeholder") && (e = .75 * (this.$search.val().length + 1) + "em"), this.$search.css("width", e) }, e }), u.define("select2/selection/selectionCss", ["../utils"], function (n) { function e() { } return e.prototype.render = function (e) { var t = e.call(this), e = this.options.get("selectionCssClass") || ""; return -1 !== e.indexOf(":all:") && (e = e.replace(":all:", ""), n.copyNonInternalCssClasses(t[0], this.$element[0])), t.addClass(e), t }, e }), u.define("select2/selection/eventRelay", ["jquery"], function (o) { function e() { } return e.prototype.bind = function (e, t, n) { var s = this, i = ["open", "opening", "close", "closing", "select", "selecting", "unselect", "unselecting", "clear", "clearing"], r = ["opening", "closing", "selecting", "unselecting", "clearing"]; e.call(this, t, n), t.on("*", function (e, t) { var n; -1 !== i.indexOf(e) && (t = t || {}, n = o.Event("select2:" + e, { params: t }), s.$element.trigger(n), -1 !== r.indexOf(e) && (t.prevented = n.isDefaultPrevented())) }) }, e }), u.define("select2/translation", ["jquery", "require"], function (t, n) { function s(e) { this.dict = e || {} } return s.prototype.all = function () { return this.dict }, s.prototype.get = function (e) { return this.dict[e] }, s.prototype.extend = function (e) { this.dict = t.extend({}, e.all(), this.dict) }, s._cache = {}, s.loadPath = function (e) { var t; return e in s._cache || (t = n(e), s._cache[e] = t), new s(s._cache[e]) }, s }), u.define("select2/diacritics", [], function () { return { "Ⓐ": "A", "Ａ": "A", "À": "A", "Á": "A", "Â": "A", "Ầ": "A", "Ấ": "A", "Ẫ": "A", "Ẩ": "A", "Ã": "A", "Ā": "A", "Ă": "A", "Ằ": "A", "Ắ": "A", "Ẵ": "A", "Ẳ": "A", "Ȧ": "A", "Ǡ": "A", "Ä": "A", "Ǟ": "A", "Ả": "A", "Å": "A", "Ǻ": "A", "Ǎ": "A", "Ȁ": "A", "Ȃ": "A", "Ạ": "A", "Ậ": "A", "Ặ": "A", "Ḁ": "A", "Ą": "A", "Ⱥ": "A", "Ɐ": "A", "Ꜳ": "AA", "Æ": "AE", "Ǽ": "AE", "Ǣ": "AE", "Ꜵ": "AO", "Ꜷ": "AU", "Ꜹ": "AV", "Ꜻ": "AV", "Ꜽ": "AY", "Ⓑ": "B", "Ｂ": "B", "Ḃ": "B", "Ḅ": "B", "Ḇ": "B", "Ƀ": "B", "Ƃ": "B", "Ɓ": "B", "Ⓒ": "C", "Ｃ": "C", "Ć": "C", "Ĉ": "C", "Ċ": "C", "Č": "C", "Ç": "C", "Ḉ": "C", "Ƈ": "C", "Ȼ": "C", "Ꜿ": "C", "Ⓓ": "D", "Ｄ": "D", "Ḋ": "D", "Ď": "D", "Ḍ": "D", "Ḑ": "D", "Ḓ": "D", "Ḏ": "D", "Đ": "D", "Ƌ": "D", "Ɗ": "D", "Ɖ": "D", "Ꝺ": "D", "Ǳ": "DZ", "Ǆ": "DZ", "ǲ": "Dz", "ǅ": "Dz", "Ⓔ": "E", "Ｅ": "E", "È": "E", "É": "E", "Ê": "E", "Ề": "E", "Ế": "E", "Ễ": "E", "Ể": "E", "Ẽ": "E", "Ē": "E", "Ḕ": "E", "Ḗ": "E", "Ĕ": "E", "Ė": "E", "Ë": "E", "Ẻ": "E", "Ě": "E", "Ȅ": "E", "Ȇ": "E", "Ẹ": "E", "Ệ": "E", "Ȩ": "E", "Ḝ": "E", "Ę": "E", "Ḙ": "E", "Ḛ": "E", "Ɛ": "E", "Ǝ": "E", "Ⓕ": "F", "Ｆ": "F", "Ḟ": "F", "Ƒ": "F", "Ꝼ": "F", "Ⓖ": "G", "Ｇ": "G", "Ǵ": "G", "Ĝ": "G", "Ḡ": "G", "Ğ": "G", "Ġ": "G", "Ǧ": "G", "Ģ": "G", "Ǥ": "G", "Ɠ": "G", "Ꞡ": "G", "Ᵹ": "G", "Ꝿ": "G", "Ⓗ": "H", "Ｈ": "H", "Ĥ": "H", "Ḣ": "H", "Ḧ": "H", "Ȟ": "H", "Ḥ": "H", "Ḩ": "H", "Ḫ": "H", "Ħ": "H", "Ⱨ": "H", "Ⱶ": "H", "Ɥ": "H", "Ⓘ": "I", "Ｉ": "I", "Ì": "I", "Í": "I", "Î": "I", "Ĩ": "I", "Ī": "I", "Ĭ": "I", "İ": "I", "Ï": "I", "Ḯ": "I", "Ỉ": "I", "Ǐ": "I", "Ȉ": "I", "Ȋ": "I", "Ị": "I", "Į": "I", "Ḭ": "I", "Ɨ": "I", "Ⓙ": "J", "Ｊ": "J", "Ĵ": "J", "Ɉ": "J", "Ⓚ": "K", "Ｋ": "K", "Ḱ": "K", "Ǩ": "K", "Ḳ": "K", "Ķ": "K", "Ḵ": "K", "Ƙ": "K", "Ⱪ": "K", "Ꝁ": "K", "Ꝃ": "K", "Ꝅ": "K", "Ꞣ": "K", "Ⓛ": "L", "Ｌ": "L", "Ŀ": "L", "Ĺ": "L", "Ľ": "L", "Ḷ": "L", "Ḹ": "L", "Ļ": "L", "Ḽ": "L", "Ḻ": "L", "Ł": "L", "Ƚ": "L", "Ɫ": "L", "Ⱡ": "L", "Ꝉ": "L", "Ꝇ": "L", "Ꞁ": "L", "Ǉ": "LJ", "ǈ": "Lj", "Ⓜ": "M", "Ｍ": "M", "Ḿ": "M", "Ṁ": "M", "Ṃ": "M", "Ɱ": "M", "Ɯ": "M", "Ⓝ": "N", "Ｎ": "N", "Ǹ": "N", "Ń": "N", "Ñ": "N", "Ṅ": "N", "Ň": "N", "Ṇ": "N", "Ņ": "N", "Ṋ": "N", "Ṉ": "N", "Ƞ": "N", "Ɲ": "N", "Ꞑ": "N", "Ꞥ": "N", "Ǌ": "NJ", "ǋ": "Nj", "Ⓞ": "O", "Ｏ": "O", "Ò": "O", "Ó": "O", "Ô": "O", "Ồ": "O", "Ố": "O", "Ỗ": "O", "Ổ": "O", "Õ": "O", "Ṍ": "O", "Ȭ": "O", "Ṏ": "O", "Ō": "O", "Ṑ": "O", "Ṓ": "O", "Ŏ": "O", "Ȯ": "O", "Ȱ": "O", "Ö": "O", "Ȫ": "O", "Ỏ": "O", "Ő": "O", "Ǒ": "O", "Ȍ": "O", "Ȏ": "O", "Ơ": "O", "Ờ": "O", "Ớ": "O", "Ỡ": "O", "Ở": "O", "Ợ": "O", "Ọ": "O", "Ộ": "O", "Ǫ": "O", "Ǭ": "O", "Ø": "O", "Ǿ": "O", "Ɔ": "O", "Ɵ": "O", "Ꝋ": "O", "Ꝍ": "O", "Œ": "OE", "Ƣ": "OI", "Ꝏ": "OO", "Ȣ": "OU", "Ⓟ": "P", "Ｐ": "P", "Ṕ": "P", "Ṗ": "P", "Ƥ": "P", "Ᵽ": "P", "Ꝑ": "P", "Ꝓ": "P", "Ꝕ": "P", "Ⓠ": "Q", "Ｑ": "Q", "Ꝗ": "Q", "Ꝙ": "Q", "Ɋ": "Q", "Ⓡ": "R", "Ｒ": "R", "Ŕ": "R", "Ṙ": "R", "Ř": "R", "Ȑ": "R", "Ȓ": "R", "Ṛ": "R", "Ṝ": "R", "Ŗ": "R", "Ṟ": "R", "Ɍ": "R", "Ɽ": "R", "Ꝛ": "R", "Ꞧ": "R", "Ꞃ": "R", "Ⓢ": "S", "Ｓ": "S", "ẞ": "S", "Ś": "S", "Ṥ": "S", "Ŝ": "S", "Ṡ": "S", "Š": "S", "Ṧ": "S", "Ṣ": "S", "Ṩ": "S", "Ș": "S", "Ş": "S", "Ȿ": "S", "Ꞩ": "S", "Ꞅ": "S", "Ⓣ": "T", "Ｔ": "T", "Ṫ": "T", "Ť": "T", "Ṭ": "T", "Ț": "T", "Ţ": "T", "Ṱ": "T", "Ṯ": "T", "Ŧ": "T", "Ƭ": "T", "Ʈ": "T", "Ⱦ": "T", "Ꞇ": "T", "Ꜩ": "TZ", "Ⓤ": "U", "Ｕ": "U", "Ù": "U", "Ú": "U", "Û": "U", "Ũ": "U", "Ṹ": "U", "Ū": "U", "Ṻ": "U", "Ŭ": "U", "Ü": "U", "Ǜ": "U", "Ǘ": "U", "Ǖ": "U", "Ǚ": "U", "Ủ": "U", "Ů": "U", "Ű": "U", "Ǔ": "U", "Ȕ": "U", "Ȗ": "U", "Ư": "U", "Ừ": "U", "Ứ": "U", "Ữ": "U", "Ử": "U", "Ự": "U", "Ụ": "U", "Ṳ": "U", "Ų": "U", "Ṷ": "U", "Ṵ": "U", "Ʉ": "U", "Ⓥ": "V", "Ｖ": "V", "Ṽ": "V", "Ṿ": "V", "Ʋ": "V", "Ꝟ": "V", "Ʌ": "V", "Ꝡ": "VY", "Ⓦ": "W", "Ｗ": "W", "Ẁ": "W", "Ẃ": "W", "Ŵ": "W", "Ẇ": "W", "Ẅ": "W", "Ẉ": "W", "Ⱳ": "W", "Ⓧ": "X", "Ｘ": "X", "Ẋ": "X", "Ẍ": "X", "Ⓨ": "Y", "Ｙ": "Y", "Ỳ": "Y", "Ý": "Y", "Ŷ": "Y", "Ỹ": "Y", "Ȳ": "Y", "Ẏ": "Y", "Ÿ": "Y", "Ỷ": "Y", "Ỵ": "Y", "Ƴ": "Y", "Ɏ": "Y", "Ỿ": "Y", "Ⓩ": "Z", "Ｚ": "Z", "Ź": "Z", "Ẑ": "Z", "Ż": "Z", "Ž": "Z", "Ẓ": "Z", "Ẕ": "Z", "Ƶ": "Z", "Ȥ": "Z", "Ɀ": "Z", "Ⱬ": "Z", "Ꝣ": "Z", "ⓐ": "a", "ａ": "a", "ẚ": "a", "à": "a", "á": "a", "â": "a", "ầ": "a", "ấ": "a", "ẫ": "a", "ẩ": "a", "ã": "a", "ā": "a", "ă": "a", "ằ": "a", "ắ": "a", "ẵ": "a", "ẳ": "a", "ȧ": "a", "ǡ": "a", "ä": "a", "ǟ": "a", "ả": "a", "å": "a", "ǻ": "a", "ǎ": "a", "ȁ": "a", "ȃ": "a", "ạ": "a", "ậ": "a", "ặ": "a", "ḁ": "a", "ą": "a", "ⱥ": "a", "ɐ": "a", "ꜳ": "aa", "æ": "ae", "ǽ": "ae", "ǣ": "ae", "ꜵ": "ao", "ꜷ": "au", "ꜹ": "av", "ꜻ": "av", "ꜽ": "ay", "ⓑ": "b", "ｂ": "b", "ḃ": "b", "ḅ": "b", "ḇ": "b", "ƀ": "b", "ƃ": "b", "ɓ": "b", "ⓒ": "c", "ｃ": "c", "ć": "c", "ĉ": "c", "ċ": "c", "č": "c", "ç": "c", "ḉ": "c", "ƈ": "c", "ȼ": "c", "ꜿ": "c", "ↄ": "c", "ⓓ": "d", "ｄ": "d", "ḋ": "d", "ď": "d", "ḍ": "d", "ḑ": "d", "ḓ": "d", "ḏ": "d", "đ": "d", "ƌ": "d", "ɖ": "d", "ɗ": "d", "ꝺ": "d", "ǳ": "dz", "ǆ": "dz", "ⓔ": "e", "ｅ": "e", "è": "e", "é": "e", "ê": "e", "ề": "e", "ế": "e", "ễ": "e", "ể": "e", "ẽ": "e", "ē": "e", "ḕ": "e", "ḗ": "e", "ĕ": "e", "ė": "e", "ë": "e", "ẻ": "e", "ě": "e", "ȅ": "e", "ȇ": "e", "ẹ": "e", "ệ": "e", "ȩ": "e", "ḝ": "e", "ę": "e", "ḙ": "e", "ḛ": "e", "ɇ": "e", "ɛ": "e", "ǝ": "e", "ⓕ": "f", "ｆ": "f", "ḟ": "f", "ƒ": "f", "ꝼ": "f", "ⓖ": "g", "ｇ": "g", "ǵ": "g", "ĝ": "g", "ḡ": "g", "ğ": "g", "ġ": "g", "ǧ": "g", "ģ": "g", "ǥ": "g", "ɠ": "g", "ꞡ": "g", "ᵹ": "g", "ꝿ": "g", "ⓗ": "h", "ｈ": "h", "ĥ": "h", "ḣ": "h", "ḧ": "h", "ȟ": "h", "ḥ": "h", "ḩ": "h", "ḫ": "h", "ẖ": "h", "ħ": "h", "ⱨ": "h", "ⱶ": "h", "ɥ": "h", "ƕ": "hv", "ⓘ": "i", "ｉ": "i", "ì": "i", "í": "i", "î": "i", "ĩ": "i", "ī": "i", "ĭ": "i", "ï": "i", "ḯ": "i", "ỉ": "i", "ǐ": "i", "ȉ": "i", "ȋ": "i", "ị": "i", "į": "i", "ḭ": "i", "ɨ": "i", "ı": "i", "ⓙ": "j", "ｊ": "j", "ĵ": "j", "ǰ": "j", "ɉ": "j", "ⓚ": "k", "ｋ": "k", "ḱ": "k", "ǩ": "k", "ḳ": "k", "ķ": "k", "ḵ": "k", "ƙ": "k", "ⱪ": "k", "ꝁ": "k", "ꝃ": "k", "ꝅ": "k", "ꞣ": "k", "ⓛ": "l", "ｌ": "l", "ŀ": "l", "ĺ": "l", "ľ": "l", "ḷ": "l", "ḹ": "l", "ļ": "l", "ḽ": "l", "ḻ": "l", "ſ": "l", "ł": "l", "ƚ": "l", "ɫ": "l", "ⱡ": "l", "ꝉ": "l", "ꞁ": "l", "ꝇ": "l", "ǉ": "lj", "ⓜ": "m", "ｍ": "m", "ḿ": "m", "ṁ": "m", "ṃ": "m", "ɱ": "m", "ɯ": "m", "ⓝ": "n", "ｎ": "n", "ǹ": "n", "ń": "n", "ñ": "n", "ṅ": "n", "ň": "n", "ṇ": "n", "ņ": "n", "ṋ": "n", "ṉ": "n", "ƞ": "n", "ɲ": "n", "ŉ": "n", "ꞑ": "n", "ꞥ": "n", "ǌ": "nj", "ⓞ": "o", "ｏ": "o", "ò": "o", "ó": "o", "ô": "o", "ồ": "o", "ố": "o", "ỗ": "o", "ổ": "o", "õ": "o", "ṍ": "o", "ȭ": "o", "ṏ": "o", "ō": "o", "ṑ": "o", "ṓ": "o", "ŏ": "o", "ȯ": "o", "ȱ": "o", "ö": "o", "ȫ": "o", "ỏ": "o", "ő": "o", "ǒ": "o", "ȍ": "o", "ȏ": "o", "ơ": "o", "ờ": "o", "ớ": "o", "ỡ": "o", "ở": "o", "ợ": "o", "ọ": "o", "ộ": "o", "ǫ": "o", "ǭ": "o", "ø": "o", "ǿ": "o", "ɔ": "o", "ꝋ": "o", "ꝍ": "o", "ɵ": "o", "œ": "oe", "ƣ": "oi", "ȣ": "ou", "ꝏ": "oo", "ⓟ": "p", "ｐ": "p", "ṕ": "p", "ṗ": "p", "ƥ": "p", "ᵽ": "p", "ꝑ": "p", "ꝓ": "p", "ꝕ": "p", "ⓠ": "q", "ｑ": "q", "ɋ": "q", "ꝗ": "q", "ꝙ": "q", "ⓡ": "r", "ｒ": "r", "ŕ": "r", "ṙ": "r", "ř": "r", "ȑ": "r", "ȓ": "r", "ṛ": "r", "ṝ": "r", "ŗ": "r", "ṟ": "r", "ɍ": "r", "ɽ": "r", "ꝛ": "r", "ꞧ": "r", "ꞃ": "r", "ⓢ": "s", "ｓ": "s", "ß": "s", "ś": "s", "ṥ": "s", "ŝ": "s", "ṡ": "s", "š": "s", "ṧ": "s", "ṣ": "s", "ṩ": "s", "ș": "s", "ş": "s", "ȿ": "s", "ꞩ": "s", "ꞅ": "s", "ẛ": "s", "ⓣ": "t", "ｔ": "t", "ṫ": "t", "ẗ": "t", "ť": "t", "ṭ": "t", "ț": "t", "ţ": "t", "ṱ": "t", "ṯ": "t", "ŧ": "t", "ƭ": "t", "ʈ": "t", "ⱦ": "t", "ꞇ": "t", "ꜩ": "tz", "ⓤ": "u", "ｕ": "u", "ù": "u", "ú": "u", "û": "u", "ũ": "u", "ṹ": "u", "ū": "u", "ṻ": "u", "ŭ": "u", "ü": "u", "ǜ": "u", "ǘ": "u", "ǖ": "u", "ǚ": "u", "ủ": "u", "ů": "u", "ű": "u", "ǔ": "u", "ȕ": "u", "ȗ": "u", "ư": "u", "ừ": "u", "ứ": "u", "ữ": "u", "ử": "u", "ự": "u", "ụ": "u", "ṳ": "u", "ų": "u", "ṷ": "u", "ṵ": "u", "ʉ": "u", "ⓥ": "v", "ｖ": "v", "ṽ": "v", "ṿ": "v", "ʋ": "v", "ꝟ": "v", "ʌ": "v", "ꝡ": "vy", "ⓦ": "w", "ｗ": "w", "ẁ": "w", "ẃ": "w", "ŵ": "w", "ẇ": "w", "ẅ": "w", "ẘ": "w", "ẉ": "w", "ⱳ": "w", "ⓧ": "x", "ｘ": "x", "ẋ": "x", "ẍ": "x", "ⓨ": "y", "ｙ": "y", "ỳ": "y", "ý": "y", "ŷ": "y", "ỹ": "y", "ȳ": "y", "ẏ": "y", "ÿ": "y", "ỷ": "y", "ẙ": "y", "ỵ": "y", "ƴ": "y", "ɏ": "y", "ỿ": "y", "ⓩ": "z", "ｚ": "z", "ź": "z", "ẑ": "z", "ż": "z", "ž": "z", "ẓ": "z", "ẕ": "z", "ƶ": "z", "ȥ": "z", "ɀ": "z", "ⱬ": "z", "ꝣ": "z", "Ά": "Α", "Έ": "Ε", "Ή": "Η", "Ί": "Ι", "Ϊ": "Ι", "Ό": "Ο", "Ύ": "Υ", "Ϋ": "Υ", "Ώ": "Ω", "ά": "α", "έ": "ε", "ή": "η", "ί": "ι", "ϊ": "ι", "ΐ": "ι", "ό": "ο", "ύ": "υ", "ϋ": "υ", "ΰ": "υ", "ώ": "ω", "ς": "σ", "’": "'" } }), u.define("select2/data/base", ["../utils"], function (n) { function s(e, t) { s.__super__.constructor.call(this) } return n.Extend(s, n.Observable), s.prototype.current = function (e) { throw new Error("The `current` method must be defined in child classes.") }, s.prototype.query = function (e, t) { throw new Error("The `query` method must be defined in child classes.") }, s.prototype.bind = function (e, t) { }, s.prototype.destroy = function () { }, s.prototype.generateResultId = function (e, t) { e = e.id + "-result-"; return e += n.generateChars(4), null != t.id ? e += "-" + t.id.toString() : e += "-" + n.generateChars(4), e }, s }), u.define("select2/data/select", ["./base", "../utils", "jquery"], function (e, a, l) { function n(e, t) { this.$element = e, this.options = t, n.__super__.constructor.call(this) } return a.Extend(n, e), n.prototype.current = function (e) { var t = this; e(Array.prototype.map.call(this.$element[0].querySelectorAll(":checked"), function (e) { return t.item(l(e)) })) }, n.prototype.select = function (i) { var e, r = this; if (i.selected = !0, null != i.element && "option" === i.element.tagName.toLowerCase()) return i.element.selected = !0, void this.$element.trigger("input").trigger("change"); this.$element.prop("multiple") ? this.current(function (e) { var t = []; (i = [i]).push.apply(i, e); for (var n = 0; n < i.length; n++) { var s = i[n].id; -1 === t.indexOf(s) && t.push(s) } r.$element.val(t), r.$element.trigger("input").trigger("change") }) : (e = i.id, this.$element.val(e), this.$element.trigger("input").trigger("change")) }, n.prototype.unselect = function (i) { var r = this; if (this.$element.prop("multiple")) { if (i.selected = !1, null != i.element && "option" === i.element.tagName.toLowerCase()) return i.element.selected = !1, void this.$element.trigger("input").trigger("change"); this.current(function (e) { for (var t = [], n = 0; n < e.length; n++) { var s = e[n].id; s !== i.id && -1 === t.indexOf(s) && t.push(s) } r.$element.val(t), r.$element.trigger("input").trigger("change") }) } }, n.prototype.bind = function (e, t) { var n = this; (this.container = e).on("select", function (e) { n.select(e.data) }), e.on("unselect", function (e) { n.unselect(e.data) }) }, n.prototype.destroy = function () { this.$element.find("*").each(function () { a.RemoveData(this) }) }, n.prototype.query = function (t, e) { var n = [], s = this; this.$element.children().each(function () { var e; "option" !== this.tagName.toLowerCase() && "optgroup" !== this.tagName.toLowerCase() || (e = l(this), e = s.item(e), null !== (e = s.matches(t, e)) && n.push(e)) }), e({ results: n }) }, n.prototype.addOptions = function (e) { this.$element.append(e) }, n.prototype.option = function (e) { var t; e.children ? (t = document.createElement("optgroup")).label = e.text : void 0 !== (t = document.createElement("option")).textContent ? t.textContent = e.text : t.innerText = e.text, void 0 !== e.id && (t.value = e.id), e.disabled && (t.disabled = !0), e.selected && (t.selected = !0), e.title && (t.title = e.title); e = this._normalizeItem(e); return e.element = t, a.StoreData(t, "data", e), l(t) }, n.prototype.item = function (e) { var t = {}; if (null != (t = a.GetData(e[0], "data"))) return t; var n = e[0]; if ("option" === n.tagName.toLowerCase()) t = { id: e.val(), text: e.text(), disabled: e.prop("disabled"), selected: e.prop("selected"), title: e.prop("title") }; else if ("optgroup" === n.tagName.toLowerCase()) { t = { text: e.prop("label"), children: [], title: e.prop("title") }; for (var s = e.children("option"), i = [], r = 0; r < s.length; r++) { var o = l(s[r]), o = this.item(o); i.push(o) } t.children = i } return (t = this._normalizeItem(t)).element = e[0], a.StoreData(e[0], "data", t), t }, n.prototype._normalizeItem = function (e) { e !== Object(e) && (e = { id: e, text: e }); return null != (e = l.extend({}, { text: "" }, e)).id && (e.id = e.id.toString()), null != e.text && (e.text = e.text.toString()), null == e._resultId && e.id && null != this.container && (e._resultId = this.generateResultId(this.container, e)), l.extend({}, { selected: !1, disabled: !1 }, e) }, n.prototype.matches = function (e, t) { return this.options.get("matcher")(e, t) }, n }), u.define("select2/data/array", ["./select", "../utils", "jquery"], function (e, t, c) { function s(e, t) { this._dataToConvert = t.get("data") || [], s.__super__.constructor.call(this, e, t) } return t.Extend(s, e), s.prototype.bind = function (e, t) { s.__super__.bind.call(this, e, t), this.addOptions(this.convertToOptions(this._dataToConvert)) }, s.prototype.select = function (n) { var e = this.$element.find("option").filter(function (e, t) { return t.value == n.id.toString() }); 0 === e.length && (e = this.option(n), this.addOptions(e)), s.__super__.select.call(this, n) }, s.prototype.convertToOptions = function (e) { var t = this, n = this.$element.find("option"), s = n.map(function () { return t.item(c(this)).id }).get(), i = []; for (var r = 0; r < e.length; r++) { var o, a, l = this._normalizeItem(e[r]); 0 <= s.indexOf(l.id) ? (o = n.filter(function (e) { return function () { return c(this).val() == e.id } }(l)), a = this.item(o), a = c.extend(!0, {}, l, a), a = this.option(a), o.replaceWith(a)) : (a = this.option(l), l.children && (l = this.convertToOptions(l.children), a.append(l)), i.push(a)) } return i }, s }), u.define("select2/data/ajax", ["./array", "../utils", "jquery"], function (e, t, r) { function n(e, t) { this.ajaxOptions = this._applyDefaults(t.get("ajax")), null != this.ajaxOptions.processResults && (this.processResults = this.ajaxOptions.processResults), n.__super__.constructor.call(this, e, t) } return t.Extend(n, e), n.prototype._applyDefaults = function (e) { var t = { data: function (e) { return r.extend({}, e, { q: e.term }) }, transport: function (e, t, n) { e = r.ajax(e); return e.then(t), e.fail(n), e } }; return r.extend({}, t, e, !0) }, n.prototype.processResults = function (e) { return e }, n.prototype.query = function (t, n) { var s = this; null != this._request && ("function" == typeof this._request.abort && this._request.abort(), this._request = null); var i = r.extend({ type: "GET" }, this.ajaxOptions); function e() { var e = i.transport(i, function (e) { e = s.processResults(e, t); s.options.get("debug") && window.console && console.error && (e && e.results && Array.isArray(e.results) || console.error("Select2: The AJAX results did not return an array in the `results` key of the response.")), n(e) }, function () { "status" in e && (0 === e.status || "0" === e.status) || s.trigger("results:message", { message: "errorLoading" }) }); s._request = e } "function" == typeof i.url && (i.url = i.url.call(this.$element, t)), "function" == typeof i.data && (i.data = i.data.call(this.$element, t)), this.ajaxOptions.delay && null != t.term ? (this._queryTimeout && window.clearTimeout(this._queryTimeout), this._queryTimeout = window.setTimeout(e, this.ajaxOptions.delay)) : e() }, n }), u.define("select2/data/tags", ["jquery"], function (t) { function e(e, t, n) { var s = n.get("tags"), i = n.get("createTag"); void 0 !== i && (this.createTag = i); i = n.get("insertTag"); if (void 0 !== i && (this.insertTag = i), e.call(this, t, n), Array.isArray(s)) for (var r = 0; r < s.length; r++) { var o = s[r], o = this._normalizeItem(o), o = this.option(o); this.$element.append(o) } } return e.prototype.query = function (e, c, u) { var d = this; this._removeOldTags(), null != c.term && null == c.page ? e.call(this, c, function e(t, n) { for (var s = t.results, i = 0; i < s.length; i++) { var r = s[i], o = null != r.children && !e({ results: r.children }, !0); if ((r.text || "").toUpperCase() === (c.term || "").toUpperCase() || o) return !n && (t.data = s, void u(t)) } if (n) return !0; var a, l = d.createTag(c); null != l && ((a = d.option(l)).attr("data-select2-tag", "true"), d.addOptions([a]), d.insertTag(s, l)), t.results = s, u(t) }) : e.call(this, c, u) }, e.prototype.createTag = function (e, t) { if (null == t.term) return null; t = t.term.trim(); return "" === t ? null : { id: t, text: t } }, e.prototype.insertTag = function (e, t, n) { t.unshift(n) }, e.prototype._removeOldTags = function (e) { this.$element.find("option[data-select2-tag]").each(function () { this.selected || t(this).remove() }) }, e }), u.define("select2/data/tokenizer", ["jquery"], function (c) { function e(e, t, n) { var s = n.get("tokenizer"); void 0 !== s && (this.tokenizer = s), e.call(this, t, n) } return e.prototype.bind = function (e, t, n) { e.call(this, t, n), this.$search = t.dropdown.$search || t.selection.$search || n.find(".select2-search__field") }, e.prototype.query = function (e, t, n) { var s = this; t.term = t.term || ""; var i = this.tokenizer(t, this.options, function (e) { var t, n = s._normalizeItem(e); s.$element.find("option").filter(function () { return c(this).val() === n.id }).length || ((t = s.option(n)).attr("data-select2-tag", !0), s._removeOldTags(), s.addOptions([t])), t = n, s.trigger("select", { data: t }) }); i.term !== t.term && (this.$search.length && (this.$search.val(i.term), this.$search.trigger("focus")), t.term = i.term), e.call(this, t, n) }, e.prototype.tokenizer = function (e, t, n, s) { for (var i = n.get("tokenSeparators") || [], r = t.term, o = 0, a = this.createTag || function (e) { return { id: e.term, text: e.term } }; o < r.length;) { var l = r[o]; -1 !== i.indexOf(l) ? (l = r.substr(0, o), null != (l = a(c.extend({}, t, { term: l }))) ? (s(l), r = r.substr(o + 1) || "", o = 0) : o++) : o++ } return { term: r } }, e }), u.define("select2/data/minimumInputLength", [], function () { function e(e, t, n) { this.minimumInputLength = n.get("minimumInputLength"), e.call(this, t, n) } return e.prototype.query = function (e, t, n) { t.term = t.term || "", t.term.length < this.minimumInputLength ? this.trigger("results:message", { message: "inputTooShort", args: { minimum: this.minimumInputLength, input: t.term, params: t } }) : e.call(this, t, n) }, e }), u.define("select2/data/maximumInputLength", [], function () { function e(e, t, n) { this.maximumInputLength = n.get("maximumInputLength"), e.call(this, t, n) } return e.prototype.query = function (e, t, n) { t.term = t.term || "", 0 < this.maximumInputLength && t.term.length > this.maximumInputLength ? this.trigger("results:message", { message: "inputTooLong", args: { maximum: this.maximumInputLength, input: t.term, params: t } }) : e.call(this, t, n) }, e }), u.define("select2/data/maximumSelectionLength", [], function () { function e(e, t, n) { this.maximumSelectionLength = n.get("maximumSelectionLength"), e.call(this, t, n) } return e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), t.on("select", function () { s._checkIfMaximumSelected() }) }, e.prototype.query = function (e, t, n) { var s = this; this._checkIfMaximumSelected(function () { e.call(s, t, n) }) }, e.prototype._checkIfMaximumSelected = function (e, t) { var n = this; this.current(function (e) { e = null != e ? e.length : 0; 0 < n.maximumSelectionLength && e >= n.maximumSelectionLength ? n.trigger("results:message", { message: "maximumSelected", args: { maximum: n.maximumSelectionLength } }) : t && t() }) }, e }), u.define("select2/dropdown", ["jquery", "./utils"], function (t, e) { function n(e, t) { this.$element = e, this.options = t, n.__super__.constructor.call(this) } return e.Extend(n, e.Observable), n.prototype.render = function () { var e = t('<span class="select2-dropdown"><span class="select2-results"></span></span>'); return e.attr("dir", this.options.get("dir")), this.$dropdown = e }, n.prototype.bind = function () { }, n.prototype.position = function (e, t) { }, n.prototype.destroy = function () { this.$dropdown.remove() }, n }), u.define("select2/dropdown/search", ["jquery"], function (r) { function e() { } return e.prototype.render = function (e) { var t = e.call(this), n = this.options.get("translations").get("search"), e = r('<span class="select2-search select2-search--dropdown"><input class="select2-search__field" type="search" tabindex="-1" autocorrect="off" autocapitalize="none" spellcheck="false" role="searchbox" aria-autocomplete="list" /></span>'); return this.$searchContainer = e, this.$search = e.find("input"), this.$search.prop("autocomplete", this.options.get("autocomplete")), this.$search.attr("aria-label", n()), t.prepend(e), t }, e.prototype.bind = function (e, t, n) { var s = this, i = t.id + "-results"; e.call(this, t, n), this.$search.on("keydown", function (e) { s.trigger("keypress", e), s._keyUpPrevented = e.isDefaultPrevented() }), this.$search.on("input", function (e) { r(this).off("keyup") }), this.$search.on("keyup input", function (e) { s.handleSearch(e) }), t.on("open", function () { s.$search.attr("tabindex", 0), s.$search.attr("aria-controls", i), s.$search.trigger("focus"), window.setTimeout(function () { s.$search.trigger("focus") }, 0) }), t.on("close", function () { s.$search.attr("tabindex", -1), s.$search.removeAttr("aria-controls"), s.$search.removeAttr("aria-activedescendant"), s.$search.val(""), s.$search.trigger("blur") }), t.on("focus", function () { t.isOpen() || s.$search.trigger("focus") }), t.on("results:all", function (e) { null != e.query.term && "" !== e.query.term || (s.showSearch(e) ? s.$searchContainer[0].classList.remove("select2-search--hide") : s.$searchContainer[0].classList.add("select2-search--hide")) }), t.on("results:focus", function (e) { e.data._resultId ? s.$search.attr("aria-activedescendant", e.data._resultId) : s.$search.removeAttr("aria-activedescendant") }) }, e.prototype.handleSearch = function (e) { var t; this._keyUpPrevented || (t = this.$search.val(), this.trigger("query", { term: t })), this._keyUpPrevented = !1 }, e.prototype.showSearch = function (e, t) { return !0 }, e }), u.define("select2/dropdown/hidePlaceholder", [], function () { function e(e, t, n, s) { this.placeholder = this.normalizePlaceholder(n.get("placeholder")), e.call(this, t, n, s) } return e.prototype.append = function (e, t) { t.results = this.removePlaceholder(t.results), e.call(this, t) }, e.prototype.normalizePlaceholder = function (e, t) { return t = "string" == typeof t ? { id: "", text: t } : t }, e.prototype.removePlaceholder = function (e, t) { for (var n = t.slice(0), s = t.length - 1; 0 <= s; s--) { var i = t[s]; this.placeholder.id === i.id && n.splice(s, 1) } return n }, e }), u.define("select2/dropdown/infiniteScroll", ["jquery"], function (n) { function e(e, t, n, s) { this.lastParams = {}, e.call(this, t, n, s), this.$loadingMore = this.createLoadingMore(), this.loading = !1 } return e.prototype.append = function (e, t) { this.$loadingMore.remove(), this.loading = !1, e.call(this, t), this.showLoadingMore(t) && (this.$results.append(this.$loadingMore), this.loadMoreIfNeeded()) }, e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), t.on("query", function (e) { s.lastParams = e, s.loading = !0 }), t.on("query:append", function (e) { s.lastParams = e, s.loading = !0 }), this.$results.on("scroll", this.loadMoreIfNeeded.bind(this)) }, e.prototype.loadMoreIfNeeded = function () { var e = n.contains(document.documentElement, this.$loadingMore[0]); !this.loading && e && (e = this.$results.offset().top + this.$results.outerHeight(!1), this.$loadingMore.offset().top + this.$loadingMore.outerHeight(!1) <= e + 50 && this.loadMore()) }, e.prototype.loadMore = function () { this.loading = !0; var e = n.extend({}, { page: 1 }, this.lastParams); e.page++, this.trigger("query:append", e) }, e.prototype.showLoadingMore = function (e, t) { return t.pagination && t.pagination.more }, e.prototype.createLoadingMore = function () { var e = n('<li class="select2-results__option select2-results__option--load-more"role="option" aria-disabled="true"></li>'), t = this.options.get("translations").get("loadingMore"); return e.html(t(this.lastParams)), e }, e }), u.define("select2/dropdown/attachBody", ["jquery", "../utils"], function (u, o) { function e(e, t, n) { this.$dropdownParent = u(n.get("dropdownParent") || document.body), e.call(this, t, n) } return e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), t.on("open", function () { s._showDropdown(), s._attachPositioningHandler(t), s._bindContainerResultHandlers(t) }), t.on("close", function () { s._hideDropdown(), s._detachPositioningHandler(t) }), this.$dropdownContainer.on("mousedown", function (e) { e.stopPropagation() }) }, e.prototype.destroy = function (e) { e.call(this), this.$dropdownContainer.remove() }, e.prototype.position = function (e, t, n) { t.attr("class", n.attr("class")), t[0].classList.remove("select2"), t[0].classList.add("select2-container--open"), t.css({ position: "absolute", top: -999999 }), this.$container = n }, e.prototype.render = function (e) { var t = u("<span></span>"), e = e.call(this); return t.append(e), this.$dropdownContainer = t }, e.prototype._hideDropdown = function (e) { this.$dropdownContainer.detach() }, e.prototype._bindContainerResultHandlers = function (e, t) { var n; this._containerResultsHandlersBound || (n = this, t.on("results:all", function () { n._positionDropdown(), n._resizeDropdown() }), t.on("results:append", function () { n._positionDropdown(), n._resizeDropdown() }), t.on("results:message", function () { n._positionDropdown(), n._resizeDropdown() }), t.on("select", function () { n._positionDropdown(), n._resizeDropdown() }), t.on("unselect", function () { n._positionDropdown(), n._resizeDropdown() }), this._containerResultsHandlersBound = !0) }, e.prototype._attachPositioningHandler = function (e, t) { var n = this, s = "scroll.select2." + t.id, i = "resize.select2." + t.id, r = "orientationchange.select2." + t.id, t = this.$container.parents().filter(o.hasScroll); t.each(function () { o.StoreData(this, "select2-scroll-position", { x: u(this).scrollLeft(), y: u(this).scrollTop() }) }), t.on(s, function (e) { var t = o.GetData(this, "select2-scroll-position"); u(this).scrollTop(t.y) }), u(window).on(s + " " + i + " " + r, function (e) { n._positionDropdown(), n._resizeDropdown() }) }, e.prototype._detachPositioningHandler = function (e, t) { var n = "scroll.select2." + t.id, s = "resize.select2." + t.id, t = "orientationchange.select2." + t.id; this.$container.parents().filter(o.hasScroll).off(n), u(window).off(n + " " + s + " " + t) }, e.prototype._positionDropdown = function () { var e = u(window), t = this.$dropdown[0].classList.contains("select2-dropdown--above"), n = this.$dropdown[0].classList.contains("select2-dropdown--below"), s = null, i = this.$container.offset(); i.bottom = i.top + this.$container.outerHeight(!1); var r = { height: this.$container.outerHeight(!1) }; r.top = i.top, r.bottom = i.top + r.height; var o = this.$dropdown.outerHeight(!1), a = e.scrollTop(), l = e.scrollTop() + e.height(), c = a < i.top - o, e = l > i.bottom + o, a = { left: i.left, top: r.bottom }, l = this.$dropdownParent; "static" === l.css("position") && (l = l.offsetParent()); i = { top: 0, left: 0 }; (u.contains(document.body, l[0]) || l[0].isConnected) && (i = l.offset()), a.top -= i.top, a.left -= i.left, t || n || (s = "below"), e || !c || t ? !c && e && t && (s = "below") : s = "above", ("above" == s || t && "below" !== s) && (a.top = r.top - i.top - o), null != s && (this.$dropdown[0].classList.remove("select2-dropdown--below"), this.$dropdown[0].classList.remove("select2-dropdown--above"), this.$dropdown[0].classList.add("select2-dropdown--" + s), this.$container[0].classList.remove("select2-container--below"), this.$container[0].classList.remove("select2-container--above"), this.$container[0].classList.add("select2-container--" + s)), this.$dropdownContainer.css(a) }, e.prototype._resizeDropdown = function () { var e = { width: this.$container.outerWidth(!1) + "px" }; this.options.get("dropdownAutoWidth") && (e.minWidth = e.width, e.position = "relative", e.width = "auto"), this.$dropdown.css(e) }, e.prototype._showDropdown = function (e) { this.$dropdownContainer.appendTo(this.$dropdownParent), this._positionDropdown(), this._resizeDropdown() }, e }), u.define("select2/dropdown/minimumResultsForSearch", [], function () { function e(e, t, n, s) { this.minimumResultsForSearch = n.get("minimumResultsForSearch"), this.minimumResultsForSearch < 0 && (this.minimumResultsForSearch = 1 / 0), e.call(this, t, n, s) } return e.prototype.showSearch = function (e, t) { return !(function e(t) { for (var n = 0, s = 0; s < t.length; s++) { var i = t[s]; i.children ? n += e(i.children) : n++ } return n }(t.data.results) < this.minimumResultsForSearch) && e.call(this, t) }, e }), u.define("select2/dropdown/selectOnClose", ["../utils"], function (s) { function e() { } return e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), t.on("close", function (e) { s._handleSelectOnClose(e) }) }, e.prototype._handleSelectOnClose = function (e, t) { if (t && null != t.originalSelect2Event) { var n = t.originalSelect2Event; if ("select" === n._type || "unselect" === n._type) return } n = this.getHighlightedResults(); n.length < 1 || (null != (n = s.GetData(n[0], "data")).element && n.element.selected || null == n.element && n.selected || this.trigger("select", { data: n })) }, e }), u.define("select2/dropdown/closeOnSelect", [], function () { function e() { } return e.prototype.bind = function (e, t, n) { var s = this; e.call(this, t, n), t.on("select", function (e) { s._selectTriggered(e) }), t.on("unselect", function (e) { s._selectTriggered(e) }) }, e.prototype._selectTriggered = function (e, t) { var n = t.originalEvent; n && (n.ctrlKey || n.metaKey) || this.trigger("close", { originalEvent: n, originalSelect2Event: t }) }, e }), u.define("select2/dropdown/dropdownCss", ["../utils"], function (n) { function e() { } return e.prototype.render = function (e) { var t = e.call(this), e = this.options.get("dropdownCssClass") || ""; return -1 !== e.indexOf(":all:") && (e = e.replace(":all:", ""), n.copyNonInternalCssClasses(t[0], this.$element[0])), t.addClass(e), t }, e }), u.define("select2/dropdown/tagsSearchHighlight", ["../utils"], function (s) { function e() { } return e.prototype.highlightFirstItem = function (e) { var t = this.$results.find(".select2-results__option--selectable:not(.select2-results__option--selected)"); if (0 < t.length) { var n = t.first(), t = s.GetData(n[0], "data").element; if (t && t.getAttribute && "true" === t.getAttribute("data-select2-tag")) return void n.trigger("mouseenter") } e.call(this) }, e }), u.define("select2/i18n/en", [], function () { return { errorLoading: function () { return "The results could not be loaded." }, inputTooLong: function (e) { var t = e.input.length - e.maximum, e = "Please delete " + t + " character"; return 1 != t && (e += "s"), e }, inputTooShort: function (e) { return "Please enter " + (e.minimum - e.input.length) + " or more characters" }, loadingMore: function () { return "Loading more results…" }, maximumSelected: function (e) { var t = "You can only select " + e.maximum + " item"; return 1 != e.maximum && (t += "s"), t }, noResults: function () { return "No results found" }, searching: function () { return "Searching…" }, removeAllItems: function () { return "Remove all items" }, removeItem: function () { return "Remove item" }, search: function () { return "Search" } } }), u.define("select2/defaults", ["jquery", "./results", "./selection/single", "./selection/multiple", "./selection/placeholder", "./selection/allowClear", "./selection/search", "./selection/selectionCss", "./selection/eventRelay", "./utils", "./translation", "./diacritics", "./data/select", "./data/array", "./data/ajax", "./data/tags", "./data/tokenizer", "./data/minimumInputLength", "./data/maximumInputLength", "./data/maximumSelectionLength", "./dropdown", "./dropdown/search", "./dropdown/hidePlaceholder", "./dropdown/infiniteScroll", "./dropdown/attachBody", "./dropdown/minimumResultsForSearch", "./dropdown/selectOnClose", "./dropdown/closeOnSelect", "./dropdown/dropdownCss", "./dropdown/tagsSearchHighlight", "./i18n/en"], function (l, r, o, a, c, u, d, p, h, f, g, t, m, y, v, _, b, $, w, x, A, D, S, E, O, C, L, T, q, I, e) { function n() { this.reset() } return n.prototype.apply = function (e) { var t; null == (e = l.extend(!0, {}, this.defaults, e)).dataAdapter && (null != e.ajax ? e.dataAdapter = v : null != e.data ? e.dataAdapter = y : e.dataAdapter = m, 0 < e.minimumInputLength && (e.dataAdapter = f.Decorate(e.dataAdapter, $)), 0 < e.maximumInputLength && (e.dataAdapter = f.Decorate(e.dataAdapter, w)), 0 < e.maximumSelectionLength && (e.dataAdapter = f.Decorate(e.dataAdapter, x)), e.tags && (e.dataAdapter = f.Decorate(e.dataAdapter, _)), null == e.tokenSeparators && null == e.tokenizer || (e.dataAdapter = f.Decorate(e.dataAdapter, b))), null == e.resultsAdapter && (e.resultsAdapter = r, null != e.ajax && (e.resultsAdapter = f.Decorate(e.resultsAdapter, E)), null != e.placeholder && (e.resultsAdapter = f.Decorate(e.resultsAdapter, S)), e.selectOnClose && (e.resultsAdapter = f.Decorate(e.resultsAdapter, L)), e.tags && (e.resultsAdapter = f.Decorate(e.resultsAdapter, I))), null == e.dropdownAdapter && (e.multiple ? e.dropdownAdapter = A : (t = f.Decorate(A, D), e.dropdownAdapter = t), 0 !== e.minimumResultsForSearch && (e.dropdownAdapter = f.Decorate(e.dropdownAdapter, C)), e.closeOnSelect && (e.dropdownAdapter = f.Decorate(e.dropdownAdapter, T)), null != e.dropdownCssClass && (e.dropdownAdapter = f.Decorate(e.dropdownAdapter, q)), e.dropdownAdapter = f.Decorate(e.dropdownAdapter, O)), null == e.selectionAdapter && (e.multiple ? e.selectionAdapter = a : e.selectionAdapter = o, null != e.placeholder && (e.selectionAdapter = f.Decorate(e.selectionAdapter, c)), e.allowClear && (e.selectionAdapter = f.Decorate(e.selectionAdapter, u)), e.multiple && (e.selectionAdapter = f.Decorate(e.selectionAdapter, d)), null != e.selectionCssClass && (e.selectionAdapter = f.Decorate(e.selectionAdapter, p)), e.selectionAdapter = f.Decorate(e.selectionAdapter, h)), e.language = this._resolveLanguage(e.language), e.language.push("en"); for (var n = [], s = 0; s < e.language.length; s++) { var i = e.language[s]; -1 === n.indexOf(i) && n.push(i) } return e.language = n, e.translations = this._processTranslations(e.language, e.debug), e }, n.prototype.reset = function () { function a(e) { return e.replace(/[^\u0000-\u007E]/g, function (e) { return t[e] || e }) } this.defaults = { amdLanguageBase: "./i18n/", autocomplete: "off", closeOnSelect: !0, debug: !1, dropdownAutoWidth: !1, escapeMarkup: f.escapeMarkup, language: {}, matcher: function e(t, n) { if (null == t.term || "" === t.term.trim()) return n; if (n.children && 0 < n.children.length) { for (var s = l.extend(!0, {}, n), i = n.children.length - 1; 0 <= i; i--)null == e(t, n.children[i]) && s.children.splice(i, 1); return 0 < s.children.length ? s : e(t, s) } var r = a(n.text).toUpperCase(), o = a(t.term).toUpperCase(); return -1 < r.indexOf(o) ? n : null }, minimumInputLength: 0, maximumInputLength: 0, maximumSelectionLength: 0, minimumResultsForSearch: 0, selectOnClose: !1, scrollAfterSelect: !1, sorter: function (e) { return e }, templateResult: function (e) { return e.text }, templateSelection: function (e) { return e.text }, theme: "default", width: "resolve" } }, n.prototype.applyFromElement = function (e, t) { var n = e.language, s = this.defaults.language, i = t.prop("lang"), t = t.closest("[lang]").prop("lang"), t = Array.prototype.concat.call(this._resolveLanguage(i), this._resolveLanguage(n), this._resolveLanguage(s), this._resolveLanguage(t)); return e.language = t, e }, n.prototype._resolveLanguage = function (e) { if (!e) return []; if (l.isEmptyObject(e)) return []; if (l.isPlainObject(e)) return [e]; for (var t, n = Array.isArray(e) ? e : [e], s = [], i = 0; i < n.length; i++)s.push(n[i]), "string" == typeof n[i] && 0 < n[i].indexOf("-") && (t = n[i].split("-")[0], s.push(t)); return s }, n.prototype._processTranslations = function (e, t) { for (var n = new g, s = 0; s < e.length; s++) { var i = new g, r = e[s]; if ("string" == typeof r) try { i = g.loadPath(r) } catch (e) { try { r = this.defaults.amdLanguageBase + r, i = g.loadPath(r) } catch (e) { t && window.console && console.warn && console.warn('Select2: The language file for "' + r + '" could not be automatically loaded. A fallback will be used instead.') } } else i = l.isPlainObject(r) ? new g(r) : r; n.extend(i) } return n }, n.prototype.set = function (e, t) { var n = {}; n[l.camelCase(e)] = t; n = f._convertData(n); l.extend(!0, this.defaults, n) }, new n }), u.define("select2/options", ["jquery", "./defaults", "./utils"], function (c, n, u) { function e(e, t) { this.options = e, null != t && this.fromElement(t), null != t && (this.options = n.applyFromElement(this.options, t)), this.options = n.apply(this.options) } return e.prototype.fromElement = function (e) { var t = ["select2"]; null == this.options.multiple && (this.options.multiple = e.prop("multiple")), null == this.options.disabled && (this.options.disabled = e.prop("disabled")), null == this.options.autocomplete && e.prop("autocomplete") && (this.options.autocomplete = e.prop("autocomplete")), null == this.options.dir && (e.prop("dir") ? this.options.dir = e.prop("dir") : e.closest("[dir]").prop("dir") ? this.options.dir = e.closest("[dir]").prop("dir") : this.options.dir = "ltr"), e.prop("disabled", this.options.disabled), e.prop("multiple", this.options.multiple), u.GetData(e[0], "select2Tags") && (this.options.debug && window.console && console.warn && console.warn('Select2: The `data-select2-tags` attribute has been changed to use the `data-data` and `data-tags="true"` attributes and will be removed in future versions of Select2.'), u.StoreData(e[0], "data", u.GetData(e[0], "select2Tags")), u.StoreData(e[0], "tags", !0)), u.GetData(e[0], "ajaxUrl") && (this.options.debug && window.console && console.warn && console.warn("Select2: The `data-ajax-url` attribute has been changed to `data-ajax--url` and support for the old attribute will be removed in future versions of Select2."), e.attr("ajax--url", u.GetData(e[0], "ajaxUrl")), u.StoreData(e[0], "ajax-Url", u.GetData(e[0], "ajaxUrl"))); var n = {}; function s(e, t) { return t.toUpperCase() } for (var i = 0; i < e[0].attributes.length; i++) { var r = e[0].attributes[i].name, o = "data-"; r.substr(0, o.length) == o && (r = r.substring(o.length), o = u.GetData(e[0], r), n[r.replace(/-([a-z])/g, s)] = o) } c.fn.jquery && "1." == c.fn.jquery.substr(0, 2) && e[0].dataset && (n = c.extend(!0, {}, e[0].dataset, n)); var a, l = c.extend(!0, {}, u.GetData(e[0]), n); for (a in l = u._convertData(l)) -1 < t.indexOf(a) || (c.isPlainObject(this.options[a]) ? c.extend(this.options[a], l[a]) : this.options[a] = l[a]); return this }, e.prototype.get = function (e) { return this.options[e] }, e.prototype.set = function (e, t) { this.options[e] = t }, e }), u.define("select2/core", ["jquery", "./options", "./utils", "./keys"], function (t, i, r, s) { var o = function (e, t) { null != r.GetData(e[0], "select2") && r.GetData(e[0], "select2").destroy(), this.$element = e, this.id = this._generateId(e), t = t || {}, this.options = new i(t, e), o.__super__.constructor.call(this); var n = e.attr("tabindex") || 0; r.StoreData(e[0], "old-tabindex", n), e.attr("tabindex", "-1"); t = this.options.get("dataAdapter"); this.dataAdapter = new t(e, this.options); n = this.render(); this._placeContainer(n); t = this.options.get("selectionAdapter"); this.selection = new t(e, this.options), this.$selection = this.selection.render(), this.selection.position(this.$selection, n); t = this.options.get("dropdownAdapter"); this.dropdown = new t(e, this.options), this.$dropdown = this.dropdown.render(), this.dropdown.position(this.$dropdown, n); n = this.options.get("resultsAdapter"); this.results = new n(e, this.options, this.dataAdapter), this.$results = this.results.render(), this.results.position(this.$results, this.$dropdown); var s = this; this._bindAdapters(), this._registerDomEvents(), this._registerDataEvents(), this._registerSelectionEvents(), this._registerDropdownEvents(), this._registerResultsEvents(), this._registerEvents(), this.dataAdapter.current(function (e) { s.trigger("selection:update", { data: e }) }), e[0].classList.add("select2-hidden-accessible"), e.attr("aria-hidden", "true"), this._syncAttributes(), r.StoreData(e[0], "select2", this), e.data("select2", this) }; return r.Extend(o, r.Observable), o.prototype._generateId = function (e) { return "select2-" + (null != e.attr("id") ? e.attr("id") : null != e.attr("name") ? e.attr("name") + "-" + r.generateChars(2) : r.generateChars(4)).replace(/(:|\.|\[|\]|,)/g, "") }, o.prototype._placeContainer = function (e) { e.insertAfter(this.$element); var t = this._resolveWidth(this.$element, this.options.get("width")); null != t && e.css("width", t) }, o.prototype._resolveWidth = function (e, t) { var n = /^width:(([-+]?([0-9]*\.)?[0-9]+)(px|em|ex|%|in|cm|mm|pt|pc))/i; if ("resolve" == t) { var s = this._resolveWidth(e, "style"); return null != s ? s : this._resolveWidth(e, "element") } if ("element" == t) { s = e.outerWidth(!1); return s <= 0 ? "auto" : s + "px" } if ("style" != t) return "computedstyle" != t ? t : window.getComputedStyle(e[0]).width; e = e.attr("style"); if ("string" != typeof e) return null; for (var i = e.split(";"), r = 0, o = i.length; r < o; r += 1) { var a = i[r].replace(/\s/g, "").match(n); if (null !== a && 1 <= a.length) return a[1] } return null }, o.prototype._bindAdapters = function () { this.dataAdapter.bind(this, this.$container), this.selection.bind(this, this.$container), this.dropdown.bind(this, this.$container), this.results.bind(this, this.$container) }, o.prototype._registerDomEvents = function () { var t = this; this.$element.on("change.select2", function () { t.dataAdapter.current(function (e) { t.trigger("selection:update", { data: e }) }) }), this.$element.on("focus.select2", function (e) { t.trigger("focus", e) }), this._syncA = r.bind(this._syncAttributes, this), this._syncS = r.bind(this._syncSubtree, this), this._observer = new window.MutationObserver(function (e) { t._syncA(), t._syncS(e) }), this._observer.observe(this.$element[0], { attributes: !0, childList: !0, subtree: !1 }) }, o.prototype._registerDataEvents = function () { var n = this; this.dataAdapter.on("*", function (e, t) { n.trigger(e, t) }) }, o.prototype._registerSelectionEvents = function () { var n = this, s = ["toggle", "focus"]; this.selection.on("toggle", function () { n.toggleDropdown() }), this.selection.on("focus", function (e) { n.focus(e) }), this.selection.on("*", function (e, t) { -1 === s.indexOf(e) && n.trigger(e, t) }) }, o.prototype._registerDropdownEvents = function () { var n = this; this.dropdown.on("*", function (e, t) { n.trigger(e, t) }) }, o.prototype._registerResultsEvents = function () { var n = this; this.results.on("*", function (e, t) { n.trigger(e, t) }) }, o.prototype._registerEvents = function () { var n = this; this.on("open", function () { n.$container[0].classList.add("select2-container--open") }), this.on("close", function () { n.$container[0].classList.remove("select2-container--open") }), this.on("enable", function () { n.$container[0].classList.remove("select2-container--disabled") }), this.on("disable", function () { n.$container[0].classList.add("select2-container--disabled") }), this.on("blur", function () { n.$container[0].classList.remove("select2-container--focus") }), this.on("query", function (t) { n.isOpen() || n.trigger("open", {}), this.dataAdapter.query(t, function (e) { n.trigger("results:all", { data: e, query: t }) }) }), this.on("query:append", function (t) { this.dataAdapter.query(t, function (e) { n.trigger("results:append", { data: e, query: t }) }) }), this.on("keypress", function (e) { var t = e.which; n.isOpen() ? t === s.ESC || t === s.UP && e.altKey ? (n.close(e), e.preventDefault()) : t === s.ENTER || t === s.TAB ? (n.trigger("results:select", {}), e.preventDefault()) : t === s.SPACE && e.ctrlKey ? (n.trigger("results:toggle", {}), e.preventDefault()) : t === s.UP ? (n.trigger("results:previous", {}), e.preventDefault()) : t === s.DOWN && (n.trigger("results:next", {}), e.preventDefault()) : (t === s.ENTER || t === s.SPACE || t === s.DOWN && e.altKey) && (n.open(), e.preventDefault()) }) }, o.prototype._syncAttributes = function () { this.options.set("disabled", this.$element.prop("disabled")), this.isDisabled() ? (this.isOpen() && this.close(), this.trigger("disable", {})) : this.trigger("enable", {}) }, o.prototype._isChangeMutation = function (e) { var t = this; if (e.addedNodes && 0 < e.addedNodes.length) { for (var n = 0; n < e.addedNodes.length; n++)if (e.addedNodes[n].selected) return !0 } else { if (e.removedNodes && 0 < e.removedNodes.length) return !0; if (Array.isArray(e)) return e.some(function (e) { return t._isChangeMutation(e) }) } return !1 }, o.prototype._syncSubtree = function (e) { var e = this._isChangeMutation(e), t = this; e && this.dataAdapter.current(function (e) { t.trigger("selection:update", { data: e }) }) }, o.prototype.trigger = function (e, t) { var n = o.__super__.trigger, s = { open: "opening", close: "closing", select: "selecting", unselect: "unselecting", clear: "clearing" }; if (void 0 === t && (t = {}), e in s) { var i = s[e], s = { prevented: !1, name: e, args: t }; if (n.call(this, i, s), s.prevented) return void (t.prevented = !0) } n.call(this, e, t) }, o.prototype.toggleDropdown = function () { this.isDisabled() || (this.isOpen() ? this.close() : this.open()) }, o.prototype.open = function () { this.isOpen() || this.isDisabled() || this.trigger("query", {}) }, o.prototype.close = function (e) { this.isOpen() && this.trigger("close", { originalEvent: e }) }, o.prototype.isEnabled = function () { return !this.isDisabled() }, o.prototype.isDisabled = function () { return this.options.get("disabled") }, o.prototype.isOpen = function () { return this.$container[0].classList.contains("select2-container--open") }, o.prototype.hasFocus = function () { return this.$container[0].classList.contains("select2-container--focus") }, o.prototype.focus = function (e) { this.hasFocus() || (this.$container[0].classList.add("select2-container--focus"), this.trigger("focus", {})) }, o.prototype.enable = function (e) { this.options.get("debug") && window.console && console.warn && console.warn('Select2: The `select2("enable")` method has been deprecated and will be removed in later Select2 versions. Use $element.prop("disabled") instead.'); e = !(e = null == e || 0 === e.length ? [!0] : e)[0]; this.$element.prop("disabled", e) }, o.prototype.data = function () { this.options.get("debug") && 0 < arguments.length && window.console && console.warn && console.warn('Select2: Data can no longer be set using `select2("data")`. You should consider setting the value instead using `$element.val()`.'); var t = []; return this.dataAdapter.current(function (e) { t = e }), t }, o.prototype.val = function (e) { if (this.options.get("debug") && window.console && console.warn && console.warn('Select2: The `select2("val")` method has been deprecated and will be removed in later Select2 versions. Use $element.val() instead.'), null == e || 0 === e.length) return this.$element.val(); e = e[0]; Array.isArray(e) && (e = e.map(function (e) { return e.toString() })), this.$element.val(e).trigger("input").trigger("change") }, o.prototype.destroy = function () { r.RemoveData(this.$container[0]), this.$container.remove(), this._observer.disconnect(), this._observer = null, this._syncA = null, this._syncS = null, this.$element.off(".select2"), this.$element.attr("tabindex", r.GetData(this.$element[0], "old-tabindex")), this.$element[0].classList.remove("select2-hidden-accessible"), this.$element.attr("aria-hidden", "false"), r.RemoveData(this.$element[0]), this.$element.removeData("select2"), this.dataAdapter.destroy(), this.selection.destroy(), this.dropdown.destroy(), this.results.destroy(), this.dataAdapter = null, this.selection = null, this.dropdown = null, this.results = null }, o.prototype.render = function () { var e = t('<span class="select2 select2-container"><span class="selection"></span><span class="dropdown-wrapper" aria-hidden="true"></span></span>'); return e.attr("dir", this.options.get("dir")), this.$container = e, this.$container[0].classList.add("select2-container--" + this.options.get("theme")), r.StoreData(e[0], "element", this.$element), e }, o }), u.define("jquery-mousewheel", ["jquery"], function (e) { return e }), u.define("jquery.select2", ["jquery", "jquery-mousewheel", "./select2/core", "./select2/defaults", "./select2/utils"], function (i, e, r, t, o) { var a; return null == i.fn.select2 && (a = ["open", "close", "destroy"], i.fn.select2 = function (t) { if ("object" == typeof (t = t || {})) return this.each(function () { var e = i.extend(!0, {}, t); new r(i(this), e) }), this; if ("string" != typeof t) throw new Error("Invalid arguments for Select2: " + t); var n, s = Array.prototype.slice.call(arguments, 1); return this.each(function () { var e = o.GetData(this, "select2"); null == e && window.console && console.error && console.error("The select2('" + t + "') method was called on an element that is not using Select2."), n = e[t].apply(e, s) }), -1 < a.indexOf(t) ? this : n }), null == i.fn.select2.defaults && (i.fn.select2.defaults = t), r }), { define: u.define, require: u.require }); function b(e, t) { return i.call(e, t) } function l(e, t) { var n, s, i, r, o, a, l, c, u, d, p = t && t.split("/"), h = y.map, f = h && h["*"] || {}; if (e) { for (t = (e = e.split("/")).length - 1, y.nodeIdCompat && _.test(e[t]) && (e[t] = e[t].replace(_, "")), "." === e[0].charAt(0) && p && (e = p.slice(0, p.length - 1).concat(e)), c = 0; c < e.length; c++)"." === (d = e[c]) ? (e.splice(c, 1), --c) : ".." === d && (0 === c || 1 === c && ".." === e[2] || ".." === e[c - 1] || 0 < c && (e.splice(c - 1, 2), c -= 2)); e = e.join("/") } if ((p || f) && h) { for (c = (n = e.split("/")).length; 0 < c; --c) { if (s = n.slice(0, c).join("/"), p) for (u = p.length; 0 < u; --u)if (i = h[p.slice(0, u).join("/")], i = i && i[s]) { r = i, o = c; break } if (r) break; !a && f && f[s] && (a = f[s], l = c) } !r && a && (r = a, o = l), r && (n.splice(0, o, r), e = n.join("/")) } return e } function w(t, n) { return function () { var e = a.call(arguments, 0); return "string" != typeof e[0] && 1 === e.length && e.push(null), o.apply(p, e.concat([t, n])) } } function x(e) { var t; if (b(m, e) && (t = m[e], delete m[e], v[e] = !0, r.apply(p, t)), !b(g, e) && !b(v, e)) throw new Error("No " + e); return g[e] } function c(e) { var t, n = e ? e.indexOf("!") : -1; return -1 < n && (t = e.substring(0, n), e = e.substring(n + 1, e.length)), [t, e] } function A(e) { return e ? c(e) : [] } var u = s.require("jquery.select2"); return t.fn.select2.amd = s, u });
