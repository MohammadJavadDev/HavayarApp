(function () {
	'use strict';

	var menu = null;
	var searchInput = null;
	var items = [];
	var pageIndex = [];
	var activePath = '';
	var searchTimer = null;
	var SEARCH_LIMIT = 40;

	function fold(value) {
		return String(value || '')
			.replace(/\u200c/g, '')
			.replace(/\u064a/g, '\u06cc')
			.replace(/\u0643/g, '\u06a9')
			.replace(/\u0629/g, '\u0647')
			.trim()
			.toLowerCase();
	}

	function foldIndex(source) {
		var folded = '';
		var map = [];
		var text = String(source || '');
		for (var i = 0; i < text.length; i++) {
			var ch = text.charAt(i);
			if (ch === '\u200c')
				continue;
			if (ch === '\u064a') ch = '\u06cc';
			else if (ch === '\u0643') ch = '\u06a9';
			else if (ch === '\u0629') ch = '\u0647';
			else ch = ch.toLowerCase();
			folded += ch;
			map.push(i);
		}
		return { folded: folded, map: map };
	}

	function normalizePath(path) {
		var value = String(path || '').trim().toLowerCase();
		var query = value.indexOf('?');
		if (query >= 0)
			value = value.slice(0, query);
		if (value.length > 1 && value.charAt(value.length - 1) === '/')
			value = value.slice(0, -1);
		return value;
	}

	function safeColor(color) {
		var value = String(color || '').trim();
		if (!value) return '';
		if (/^#(?:[0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$/i.test(value)) return value;
		if (/^(?:rgb|rgba|hsl|hsla)\([0-9.,%\s]+\)$/i.test(value)) return value;
		if (/^[a-z]{3,20}$/i.test(value)) return value;
		return '';
	}

	function parseIcon(icon, iconColor) {
		var result = { className: '', color: safeColor(iconColor) };
		var raw = String(icon || '').trim();
		if (!raw) return result;
		if (raw.charAt(0) !== '<') {
			result.className = raw;
			return result;
		}
		var classMatch = /class\s*=\s*['"]([^'"]+)['"]/i.exec(raw);
		var styleMatch = /color\s*:\s*([^;'"]+)/i.exec(raw);
		if (classMatch) result.className = classMatch[1];
		if (!result.color && styleMatch) result.color = safeColor(styleMatch[1]);
		return result;
	}

	function safeHref(path) {
		var value = String(path || '').trim();
		if (!value || value === '#') return '#';
		if (value.charAt(0) === '/' && value.charAt(1) !== '/') return value;
		if (/^https?:\/\//i.test(value)) return value;
		return '#';
	}

	function makeIcon(icon, iconColor, asBullet) {
		var parsed = parseIcon(icon, iconColor);
		if (!parsed.className) {
			if (!asBullet) {
				var empty = document.createElement('span');
				empty.className = 'menu-icon';
				return empty;
			}
			var bullet = document.createElement('span');
			bullet.className = 'menu-bullet';
			var dot = document.createElement('span');
			dot.className = 'bullet bullet-dot';
			bullet.appendChild(dot);
			return bullet;
		}
		var wrap = document.createElement('span');
		wrap.className = 'menu-icon';
		var iconEl = document.createElement('i');
		iconEl.className = parsed.className;
		if (parsed.color) iconEl.style.color = parsed.color;
		wrap.appendChild(iconEl);
		return wrap;
	}

	function setHighlighted(el, text, query) {
		var source = text || '';
		el.textContent = '';
		var q = fold(query);
		if (!q) {
			el.textContent = source;
			return;
		}
		var indexed = foldIndex(source);
		var from = 0;
		var guard = 0;
		while (from <= indexed.folded.length && guard < 20) {
			guard++;
			var at = indexed.folded.indexOf(q, from);
			if (at < 0) {
				if (from < indexed.map.length)
					el.appendChild(document.createTextNode(source.slice(indexed.map[from])));
				return;
			}
			if (at > from) {
				var start = indexed.map[from];
				var end = indexed.map[at];
				el.appendChild(document.createTextNode(source.slice(start, end)));
			}
			var mark = document.createElement('mark');
			mark.className = 'hv-mark';
			var markStart = indexed.map[at];
			var markEndIndex = at + q.length - 1;
			var markEnd = indexed.map[markEndIndex];
			mark.textContent = source.slice(markStart, markEnd + 1);
			el.appendChild(mark);
			from = at + q.length;
		}
	}

	function indexPages(nodes, crumbs) {
		for (var i = 0; i < nodes.length; i++) {
			var node = nodes[i];
			var title = node.text || '';
			if (node.children && node.children.length) {
				indexPages(node.children, crumbs.concat(title));
				continue;
			}
			if (!node.path || node.path === '#') continue;
			var parsed = parseIcon(node.icon, node.iconColor);
			var group = crumbs.length ? crumbs[crumbs.length - 1] : '';
			pageIndex.push({
				path: node.path,
				title: title,
				iconClass: parsed.className || 'fas fa-file-alt',
				iconColor: parsed.color || '',
				group: group,
				search: fold(title + ' ' + crumbs.join(' ') + ' ' + node.path)
			});
		}
	}

	function findChain(nodes, path, acc) {
		for (var i = 0; i < nodes.length; i++) {
			var node = nodes[i];
			var next = acc.concat(node);
			if (node.children && node.children.length) {
				var hit = findChain(node.children, path, next);
				if (hit) return hit;
			} else if (normalizePath(node.path) === path) {
				return next;
			}
		}
		return null;
	}

	function renderLink(node, query, group) {
		var link = document.createElement('a');
		link.className = 'menu-link';
		link.href = safeHref(node.path);
		var path = normalizePath(node.path);
		link.setAttribute('data-path', path);
		if (node.text) {
			link.title = node.text;
			link.setAttribute('data-menu-title', node.text);
		}
		if (path && path === activePath)
			link.classList.add('active');
		link.appendChild(makeIcon(node.icon, node.iconColor, true));
		var title = document.createElement('span');
		title.className = 'menu-title';
		setHighlighted(title, node.text || '', query || '');
		link.appendChild(title);
		if (group) {
			var crumb = document.createElement('span');
			crumb.className = 'hv-crumb';
			crumb.textContent = group;
			link.appendChild(crumb);
		}
		return link;
	}

	function renderNode(node) {
		var hasKids = !!(node.children && node.children.length);
		var item = document.createElement('div');
		item.className = 'menu-item' + (hasKids ? ' menu-accordion' : '');
		if (!hasKids) {
			item.appendChild(renderLink(node, '', ''));
			return item;
		}

		if (node.open)
			item.classList.add('show', 'here');

		var toggle = document.createElement('span');
		toggle.className = 'menu-link hv-toggle';
		toggle.setAttribute('role', 'button');
		toggle.tabIndex = 0;
		toggle.setAttribute('aria-expanded', node.open ? 'true' : 'false');
		if (node.text) toggle.title = node.text;
		toggle.appendChild(makeIcon(node.icon, node.iconColor, false));
		var title = document.createElement('span');
		title.className = 'menu-title';
		title.textContent = node.text || '';
		toggle.appendChild(title);
		var arrow = document.createElement('span');
		arrow.className = 'menu-arrow';
		toggle.appendChild(arrow);
		item.appendChild(toggle);

		var sub = document.createElement('div');
		sub.className = 'menu-sub menu-sub-accordion';
		if (node.open) {
			for (var i = 0; i < node.children.length; i++)
				sub.appendChild(renderNode(node.children[i]));
			sub.setAttribute('data-ready', '1');
		}
		item.appendChild(sub);
		item._hvNode = node;
		return item;
	}

	function fillSub(item) {
		var node = item._hvNode;
		var sub = item.querySelector(':scope > .menu-sub');
		if (!node || !sub || !node.children || sub.getAttribute('data-ready') === '1')
			return;
		var frag = document.createDocumentFragment();
		for (var i = 0; i < node.children.length; i++)
			frag.appendChild(renderNode(node.children[i]));
		sub.appendChild(frag);
		sub.setAttribute('data-ready', '1');
	}

	function renderTree() {
		if (!menu) return;
		menu.classList.remove('hv-searching');
		menu.replaceChildren();
		if (!items.length) {
			var empty = document.createElement('div');
			empty.className = 'hv-empty';
			empty.textContent = 'منویی برای نمایش نیست';
			menu.appendChild(empty);
			return;
		}
		var frag = document.createDocumentFragment();
		for (var i = 0; i < items.length; i++)
			frag.appendChild(renderNode(items[i]));
		menu.appendChild(frag);
		var active = menu.querySelector('a.menu-link.active');
		if (active && active.scrollIntoView)
			active.scrollIntoView({ block: 'nearest' });
	}

	function renderSearch(query) {
		if (!menu) return;
		var folded = fold(query);
		if (!folded) {
			renderTree();
			return;
		}
		menu.classList.add('hv-searching');
		menu.replaceChildren();
		var hits = [];
		var total = 0;
		for (var i = 0; i < pageIndex.length; i++) {
			if (pageIndex[i].search.indexOf(folded) === -1) continue;
			total++;
			if (hits.length < SEARCH_LIMIT)
				hits.push(pageIndex[i]);
		}
		if (!hits.length) {
			var empty = document.createElement('div');
			empty.className = 'hv-empty';
			empty.textContent = 'موردی پیدا نشد';
			menu.appendChild(empty);
			return;
		}
		var frag = document.createDocumentFragment();
		for (var h = 0; h < hits.length; h++) {
			var page = hits[h];
			var item = document.createElement('div');
			item.className = 'menu-item';
			item.appendChild(renderLink({
				text: page.title,
				path: page.path,
				icon: page.iconClass,
				iconColor: page.iconColor
			}, query, page.group));
			frag.appendChild(item);
		}
		if (total > hits.length) {
			var more = document.createElement('div');
			more.className = 'hv-empty';
			more.textContent = hits.length + ' نتیجه اول از ' + total + ' مورد. عبارت را دقیق‌تر بنویسید';
			frag.appendChild(more);
		}
		menu.appendChild(frag);
	}

	function toggleBranch(item) {
		if (!item || !item.classList.contains('menu-accordion')) return;
		var node = item._hvNode;
		var willOpen = !item.classList.contains('show');
		if (willOpen) fillSub(item);
		if (node) node.open = willOpen;
		item.classList.toggle('show', willOpen);
		if (!willOpen) item.classList.remove('here');
		var toggle = item.querySelector(':scope > .hv-toggle');
		if (toggle) toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
	}

	function expandChain(container, nodes) {
		if (!container || !nodes) return;
		var rows = container.children;
		var rowIndex = 0;
		for (var i = 0; i < nodes.length; i++) {
			var node = nodes[i];
			var row = rows[rowIndex];
			rowIndex++;
			if (!row || !node.children || !node.children.length || !node.open) continue;
			row.classList.add('show', 'here');
			fillSub(row);
			var toggle = row.querySelector(':scope > .hv-toggle');
			if (toggle) toggle.setAttribute('aria-expanded', 'true');
			expandChain(row.querySelector(':scope > .menu-sub'), node.children);
		}
	}

	function markActiveLink() {
		if (!menu) return null;
		var links = menu.querySelectorAll('a.menu-link');
		var found = null;
		for (var i = 0; i < links.length; i++) {
			var match = !!activePath && links[i].getAttribute('data-path') === activePath;
			links[i].classList.toggle('active', match);
			if (match) found = links[i];
		}
		return found;
	}

	function openActiveChain() {
		if (!activePath) return;
		var chain = findChain(items, activePath, []);
		if (!chain) return;
		for (var i = 0; i < chain.length - 1; i++)
			chain[i].open = true;
	}

	function highlight(address) {
		activePath = normalizePath(address);
		openActiveChain();
		if (!menu) return;
		if (menu.classList.contains('hv-searching')) {
			markActiveLink();
			return;
		}
		expandChain(menu, items);
		var found = markActiveLink();
		if (found && found.scrollIntoView)
			found.scrollIntoView({ block: 'nearest' });
	}

	function load(data) {
		items = Array.isArray(data) ? data : [];
		pageIndex = [];
		indexPages(items, []);
		openActiveChain();
		var typed = searchInput ? searchInput.value.trim() : '';
		if (typed) renderSearch(typed);
		else renderTree();
	}

	var flyout = null;
	var flyoutAnchor = null;
	var flyoutCloseTimer = null;

	function cancelFlyoutClose() {
		if (!flyoutCloseTimer) return;
		clearTimeout(flyoutCloseTimer);
		flyoutCloseTimer = null;
	}

	function scheduleFlyoutClose() {
		cancelFlyoutClose();
		flyoutCloseTimer = setTimeout(closeFlyout, 220);
	}

	function isMinimized() {
		return document.body.getAttribute('data-kt-app-sidebar-minimize') === 'on';
	}

	function rememberSidebar() {
		var value = isMinimized() ? 'on' : 'off';
		try { localStorage.setItem('hv-sidebar-minimize', value); } catch (e) {}
		document.cookie = 'sidebar_minimize_state=' + value + '; path=/; max-age=31536000; samesite=lax';
	}

	function closeFlyout() {
		if (flyout) flyout.remove();
		flyout = null;
		if (flyoutAnchor) flyoutAnchor.classList.remove('hv-flyout-source');
		flyoutAnchor = null;
	}

	function positionFlyout(panel, item) {
		var sidebar = document.getElementById('kt_app_sidebar');
		if (!sidebar) return;
		var sb = sidebar.getBoundingClientRect();
		var anchor = item.getBoundingClientRect();
		var gap = 10;
		var width = Math.min(260, Math.max(220, sb.left - gap - 12));
		panel.style.width = width + 'px';
		panel.style.right = (window.innerWidth - sb.left + gap) + 'px';
		panel.style.left = 'auto';
		var top = anchor.top;
		panel.style.top = top + 'px';
		var height = panel.offsetHeight;
		var maxTop = window.innerHeight - 8 - height;
		if (top > maxTop) top = Math.max(8, maxTop);
		panel.style.top = top + 'px';
	}

	function openFlyout(item) {
		var node = item._hvNode;
		if (!node || !node.children || !node.children.length) return;
		if (flyoutAnchor === item) return;
		cancelFlyoutClose();
		closeFlyout();
		var panel = document.createElement('div');
		panel.className = 'hv-flyout';
		panel.setAttribute('role', 'menu');
		var sidebar = document.getElementById('kt_app_sidebar');
		if (sidebar) {
			var paint = getComputedStyle(sidebar);
			panel.style.setProperty('background-color', paint.backgroundColor, 'important');
			var sample = sidebar.querySelector('.menu-title');
			var textColor = sample ? getComputedStyle(sample).color : '';
			if (!textColor || textColor === 'rgba(0, 0, 0, 0)')
				textColor = '#DBDFE9';
			panel.style.setProperty('--hv-flyout-color', textColor);
			panel.style.setProperty('color', textColor, 'important');
		}
		var head = document.createElement('div');
		head.className = 'hv-flyout__title';
		head.textContent = node.text || '';
		var icon = item.querySelector(':scope > .menu-link .menu-icon i');
		var iconColor = icon ? getComputedStyle(icon).color : '';
		if (iconColor && iconColor !== 'rgba(0, 0, 0, 0)')
			head.style.setProperty('color', iconColor, 'important');
		panel.appendChild(head);
		var list = document.createElement('div');
		list.className = 'menu menu-column menu-rounded menu-sub-indention hv-menu hv-flyout__menu';
		for (var i = 0; i < node.children.length; i++)
			list.appendChild(renderNode(node.children[i]));
		panel.appendChild(list);
		panel.addEventListener('mouseenter', cancelFlyoutClose);
		panel.addEventListener('mouseleave', function () {
			if (isMinimized()) scheduleFlyoutClose();
		});
		panel.addEventListener('click', function (e) {
			var toggle = e.target.closest('.hv-toggle');
			if (toggle && panel.contains(toggle)) {
				e.preventDefault();
				e.stopPropagation();
				toggleBranch(toggle.parentElement);
				positionFlyout(panel, item);
				return;
			}
			if (e.target.closest('a.menu-link')) closeFlyout();
		});
		panel.addEventListener('keydown', function (e) {
			var toggle = e.target.closest('.hv-toggle');
			if (!toggle || !panel.contains(toggle)) return;
			if (e.key !== 'Enter' && e.key !== ' ') return;
			e.preventDefault();
			toggleBranch(toggle.parentElement);
			positionFlyout(panel, item);
		});
		document.body.appendChild(panel);
		flyout = panel;
		flyoutAnchor = item;
		item.classList.add('hv-flyout-source');
		positionFlyout(panel, item);
	}

	function onMenuClick(e) {
		var toggle = e.target.closest('.hv-toggle');
		if (!toggle || !menu.contains(toggle)) return;
		e.preventDefault();
		e.stopPropagation();
		if (isMinimized()) {
			openFlyout(toggle.parentElement);
			return;
		}
		closeFlyout();
		toggleBranch(toggle.parentElement);
	}

	function onMenuHover(e) {
		if (!isMinimized()) return;
		var item = e.target.closest('.menu > .menu-item');
		if (!item || !menu.contains(item)) return;
		var node = item._hvNode;
		if (node && node.children && node.children.length) {
			cancelFlyoutClose();
			openFlyout(item);
			return;
		}
		scheduleFlyoutClose();
	}

	function onMenuKey(e) {
		var toggle = e.target.closest('.hv-toggle');
		if (!toggle || !menu.contains(toggle)) return;
		if (e.key !== 'Enter' && e.key !== ' ') return;
		e.preventDefault();
		if (isMinimized()) {
			openFlyout(toggle.parentElement);
			return;
		}
		closeFlyout();
		toggleBranch(toggle.parentElement);
	}

	function bind() {
		menu.addEventListener('click', onMenuClick);
		menu.addEventListener('keydown', onMenuKey);
		menu.addEventListener('mouseover', onMenuHover);
		menu.addEventListener('mouseleave', function () {
			if (isMinimized()) scheduleFlyoutClose();
		});
		document.addEventListener('click', function (e) {
			if (!flyout) return;
			if (flyout.contains(e.target)) return;
			if (flyoutAnchor && flyoutAnchor.contains(e.target)) return;
			closeFlyout();
		});
		document.addEventListener('keydown', function (e) {
			if (e.key === 'Escape') closeFlyout();
		});
		window.addEventListener('resize', function () {
			if (flyout && flyoutAnchor) positionFlyout(flyout, flyoutAnchor);
		});
		var scroll = document.getElementById('kt_app_sidebar_menu_scroll');
		if (scroll) scroll.addEventListener('scroll', closeFlyout, { passive: true });
		var sidebarToggle = document.getElementById('kt_app_sidebar_toggle');
		if (sidebarToggle) sidebarToggle.addEventListener('click', function () {
			closeFlyout();
			setTimeout(rememberSidebar, 0);
		});
		if (!searchInput) return;
		searchInput.addEventListener('input', function () {
			clearTimeout(searchTimer);
			searchTimer = setTimeout(function () {
				var typed = searchInput.value.trim();
				if (!typed) renderTree();
				else renderSearch(typed);
			}, 120);
		});
		searchInput.addEventListener('keydown', function (e) {
			if (e.key !== 'Escape') return;
			searchInput.value = '';
			renderTree();
			searchInput.blur();
		});
	}

	function pages() {
		var copy = [];
		for (var i = 0; i < pageIndex.length; i++) {
			var page = pageIndex[i];
			copy.push({
				path: page.path,
				title: page.title,
				iconClass: page.iconClass,
				iconColor: page.iconColor,
				group: page.group
			});
		}
		return copy;
	}

	function init() {
		menu = document.getElementById('kt_app_sidebar_menu');
		searchInput = document.getElementById('sidebar_menu_search');
		var dataEl = document.getElementById('hv-menu-data');
		if (!menu || !dataEl) return;
		activePath = normalizePath(menu.getAttribute('data-active-path') || '');
		var data = [];
		try {
			data = JSON.parse(dataEl.textContent || '[]');
		} catch (e) {
			data = [];
		}
		dataEl.remove();
		load(data);
		bind();
	}

	window.HvMenu = {
		load: load,
		pages: pages,
		highlight: highlight
	};

	if (document.readyState === 'loading')
		document.addEventListener('DOMContentLoaded', init);
	else
		init();
})();
