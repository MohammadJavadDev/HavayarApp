
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
		colResize: {
			isEnabled: true,
			resize: true,   
			reorder: true,
			dataProfileId: profileId ,
		}
		 
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
				$item.remove();
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

		if (href && href.startsWith("/File/download")) {

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
		$(".entity-selector-wrapper").each(function() {
			if (!$(this).data('readonly')) {
				$(this).entitySelector();
			}
		});
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
                 if (!$(node).data('readonly')) {
                     $(node).entitySelector();
                 }
                 continue;
            }

            // Check children using native API for better performance
            if (node.querySelectorAll) {
                const selectors = node.querySelectorAll('.entity-selector-wrapper');
                if (selectors.length > 0) {
                    $(selectors).each(function() {
                        if (!$(this).data('readonly')) {
                            $(this).entitySelector();
                        }
                    });
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



class SettingsManager {
	/**
	 * @param {string} storageName - نام کلید اصلی در لوکال استوریج (برای تداخل نداشتن با بقیه برنامه‌ها)
	 * @param {object} defaultSettings - (اختیاری) تنظیمات پیش‌فرض اولیه
	 */
	constructor(storageName = 'app_settings', defaultSettings = {}) {
		this.storageName = storageName;
		this.settings = { ...defaultSettings }; // کپی کردن پیش‌فرض‌ها
		this._load(); // لود کردن اطلاعات ذخیره شده قبلی روی پیش‌فرض‌ها
	}

	/**
	 * بارگذاری اطلاعات از مرورگر
	 * (متد داخلی)
	 */
	_load() {
		try {
			const savedData = localStorage.getItem(this.storageName);
			if (savedData) {
				// ترکیب داده‌های ذخیره شده با داده‌های موجود
				this.settings = { ...this.settings, ...JSON.parse(savedData) };
			}
		} catch (error) {
			console.error('Error loading settings:', error);
		}
	}

	/**
	 * ذخیره اطلاعات در مرورگر
	 * (متد داخلی)
	 */
	_save() {
		try {
			localStorage.setItem(this.storageName, JSON.stringify(this.settings));
		} catch (error) {
			console.error('Error saving settings:', error);
		}
	}

	/**
	 * دریافت یک مقدار
	 * @param {string} key - نام تنظیم
	 * @param {any} fallbackValue - (اختیاری) مقداری که اگر تنظیم وجود نداشت برگردانده شود
	 */
	get(key, fallbackValue = null) {
		// اگر کلید وجود داشت برگردان، وگرنه مقدار فال‌بک را بده
		return key in this.settings ? this.settings[key] : fallbackValue;
	}

	/**
	 * تنظیم یا آپدیت یک مقدار
	 * @param {string} key - نام تنظیم
	 * @param {any} value - مقدار (می‌تواند عدد، رشته، آرایه یا آبجکت باشد)
	 */
	set(key, value) {
		this.settings[key] = value;
		this._save(); // ذخیره آنی در مرورگر
		return this; // برای قابلیت زنجیره‌سازی (Chaining)
	}

	/**
	 * حذف یک تنظیم خاص
	 * @param {string} key 
	 */
	remove(key) {
		if (key in this.settings) {
			delete this.settings[key];
			this._save();
		}
	}

	/**
	 * پاک کردن تمام تنظیمات
	 */
	clear() {
		this.settings = {};
		localStorage.removeItem(this.storageName);
	}

	/**
	 * دریافت کل آبجکت تنظیمات (برای دیباگ یا ارسال به سرور)
	 */
	getAll() {
		return this.settings;
	}
}