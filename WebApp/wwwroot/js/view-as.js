(function () {
	var params = new URLSearchParams(window.location.search);
	var vas = params.get("viewAs");

	if (!vas) {
		if (window.top === window.self) {
			try { sessionStorage.removeItem("havayarViewAs"); } catch (e) { }
		}
		return;
	}

	try { sessionStorage.setItem("havayarViewAs", vas); } catch (e) { }

	document.addEventListener("DOMContentLoaded", function () {
		document.body.classList.add("havayar-view-as");
		if (document.title.indexOf("بررسی دسترسی") !== 0)
			document.title = "بررسی دسترسی | " + document.title;
	});

	function sameOrigin(url) {
		try {
			return new URL(url, window.location.origin).origin === window.location.origin;
		} catch (e) {
			return false;
		}
	}

	function withViewAs(url) {
		if (url == null || url === "") return url;
		try {
			var u = new URL(url, window.location.origin);
			if (u.origin !== window.location.origin) return url;
			u.searchParams.set("viewAs", vas);
			return u.pathname + u.search + u.hash;
		} catch (e) {
			return url;
		}
	}

	var pushState = history.pushState;
	var replaceState = history.replaceState;
	history.pushState = function (state, title, url) {
		if (typeof url === "string") url = withViewAs(url);
		return pushState.call(history, state, title, url);
	};
	history.replaceState = function (state, title, url) {
		if (typeof url === "string") url = withViewAs(url);
		return replaceState.call(history, state, title, url);
	};

	var origOpen = XMLHttpRequest.prototype.open;
	var origSend = XMLHttpRequest.prototype.send;
	XMLHttpRequest.prototype.open = function (method, url) {
		this.__havayarViewAsUrl = url;
		return origOpen.apply(this, arguments);
	};
	XMLHttpRequest.prototype.send = function () {
		try {
			if (this.__havayarViewAsUrl && sameOrigin(this.__havayarViewAsUrl))
				this.setRequestHeader("X-Havayar-ViewAs", vas);
		} catch (e) { }
		return origSend.apply(this, arguments);
	};

	var origFetch = window.fetch;
	if (typeof origFetch === "function") {
		window.fetch = function (input, init) {
			var url = typeof input === "string" ? input : (input && input.url);
			if (!url || !sameOrigin(url))
				return origFetch.apply(this, arguments);

			if (typeof Request !== "undefined" && input instanceof Request && !init) {
				var requestHeaders = new Headers(input.headers);
				requestHeaders.set("X-Havayar-ViewAs", vas);
				return origFetch.call(this, new Request(input, { headers: requestHeaders }));
			}

			var nextInit = init ? Object.assign({}, init) : {};
			var headers = new Headers(nextInit.headers || undefined);
			headers.set("X-Havayar-ViewAs", vas);
			nextInit.headers = headers;
			return origFetch.call(this, input, nextInit);
		};
	}

	var origWindowOpen = window.open;
	window.open = function (url, name, specs) {
		if (typeof url === "string" && sameOrigin(url))
			url = withViewAs(url);
		return origWindowOpen.call(window, url, name, specs);
	};

	document.addEventListener("click", function (e) {
		var closeBtn = e.target && e.target.closest ? e.target.closest("#havayarViewAsClose") : null;
		if (closeBtn) {
			e.preventDefault();
			try { sessionStorage.removeItem("havayarViewAs"); } catch (err) { }
			window.close();
			return;
		}

		var a = e.target && e.target.closest ? e.target.closest("a") : null;
		if (!a) return;

		var href = a.getAttribute("href") || "";
		if (href === "/Authenticate/Logout") {
			e.preventDefault();
			e.stopImmediatePropagation();
			try { sessionStorage.removeItem("havayarViewAs"); } catch (err) { }
			window.close();
			return;
		}

		var download = href.indexOf("/File/download") === 0 || href.indexOf("/panel/importData/downloadSample") === 0;
		var newWindow = e.ctrlKey || e.metaKey || e.shiftKey || a.getAttribute("target") === "_blank";
		if (!download && !newWindow) return;
		if (!href || href.charAt(0) === "#" || !sameOrigin(href)) return;

		a.setAttribute("href", withViewAs(href));
	}, true);

	document.addEventListener("submit", function (e) {
		var form = e.target;
		if (!form || !form.action || !sameOrigin(form.action)) return;
		form.action = withViewAs(form.action);
	}, true);
})();
