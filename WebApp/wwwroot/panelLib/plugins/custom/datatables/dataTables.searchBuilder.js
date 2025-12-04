/*! SearchBuilder 1.8.1
 * ©SpryMedia Ltd - datatables.net/license/mit
 */

(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD
        define(['jquery', 'datatables.net'], function ($) {
            return factory($, window, document);
        });
    }
    else if (typeof exports === 'object') {
        // CommonJS
        var jq = require('jquery');
        var cjsRequires = function (root, $) {
            if (!$.fn.dataTable) {
                require('datatables.net')(root, $);
            }
        };

        if (typeof window === 'undefined') {
            module.exports = function (root, $) {
                if (!root) {
                    // CommonJS environments without a window global must pass a
                    // root. This will give an error otherwise
                    root = window;
                }

                if (!$) {
                    $ = jq(root);
                }

                cjsRequires(root, $);
                return factory($, root, root.document);
            };
        }
        else {
            cjsRequires(window, jq);
            module.exports = factory(jq, window, window.document);
        }
    }
    else {
        // Browser
        factory(jQuery, window, document);
    }
}(function ($, window, document) {
    'use strict';
    var DataTable = $.fn.dataTable;


    (function () {
        'use strict';

        var $$3;
        var dataTable$2;
        /** Get a moment object. Attempt to get from DataTables for module loading first. */
        function moment() {
            var used = DataTable.use('moment');
            return used
                ? used
                : window.moment;
        }
        /** Get a luxon object. Attempt to get from DataTables for module loading first. */
        function luxon() {
            var used = DataTable.use('luxon');
            return used
                ? used
                : window.luxon;
        }
        /**
         * Sets the value of jQuery for use in the file
         *
         * @param jq the instance of jQuery to be set
         */
        function setJQuery$2(jq) {
            $$3 = jq;
            dataTable$2 = jq.fn.dataTable;
        }
        /**
         * The Criteria class is used within SearchBuilder to represent a search criteria
         */
        var Criteria = /** @class */ (function () {
            function Criteria(table, opts, topGroup, index, depth, serverData, liveSearch) {
                if (index === void 0) { index = 0; }
                if (depth === void 0) { depth = 1; }
                if (serverData === void 0) { serverData = undefined; }
                if (liveSearch === void 0) { liveSearch = false; }
                var _this = this;
                this.classes = $$3.extend(true, {}, Criteria.classes);
                // Get options from user and any extra conditions/column types defined by plug-ins
                this.c = $$3.extend(true, {}, Criteria.defaults, $$3.fn.dataTable.ext.searchBuilder, opts);
                var i18n = this.c.i18n;
                this.s = {
                    condition: undefined,
                    conditions: {},
                    data: undefined,
                    dataIdx: -1,
                    dataPoints: [],
                    dateFormat: false,
                    depth: depth,
                    dt: table,
                    filled: false,
                    index: index,
                    liveSearch: liveSearch,
                    origData: undefined,
                    preventRedraw: false,
                    serverData: serverData,
                    topGroup: topGroup,
                    type: '',
                    value: []
                };
                this.dom = {
                    buttons: $$3('<div/>')
                        .addClass(this.classes.buttonContainer),
                    condition: $$3('<select disabled/>')
                        .addClass(this.classes.condition)
                        .addClass(this.classes.dropDown)
                        .addClass(this.classes.italic)
                        .attr('autocomplete', 'hacking'),
                    conditionTitle: $$3('<option value="" disabled selected hidden/>')
                        .html(this.s.dt.i18n('searchBuilder.condition', i18n.condition)),
                    container: $$3('<div/>')
                        .addClass(this.classes.container),
                    data: $$3('<select/>')
                        .addClass(this.classes.data)
                        .addClass(this.classes.dropDown)
                        .addClass(this.classes.italic),
                    dataTitle: $$3('<option value="" disabled selected hidden/>')
                        .html(this.s.dt.i18n('searchBuilder.data', i18n.data)),
                    defaultValue: $$3('<select disabled/>')
                        .addClass(this.classes.value)
                        .addClass(this.classes.dropDown)
                        .addClass(this.classes.select)
                        .addClass(this.classes.italic),
                    "delete": $$3('<button/>')
                        .html(this.s.dt.i18n('searchBuilder.delete', i18n["delete"]))
                        .addClass(this.classes["delete"])
                        .addClass(this.classes.button)
                        .attr('title', this.s.dt.i18n('searchBuilder.deleteTitle', i18n.deleteTitle))
                        .attr('type', 'button'),
                    inputCont: $$3('<div/>')
                        .addClass(this.classes.inputCont),
                    // eslint-disable-next-line no-useless-escape
                    left: $$3('<button/>')
                        .html(this.s.dt.i18n('searchBuilder.left', i18n.left))
                        .addClass(this.classes.left)
                        .addClass(this.classes.button)
                        .attr('title', this.s.dt.i18n('searchBuilder.leftTitle', i18n.leftTitle))
                        .attr('type', 'button'),
                    // eslint-disable-next-line no-useless-escape
                    right: $$3('<button/>')
                        .html(this.s.dt.i18n('searchBuilder.right', i18n.right))
                        .addClass(this.classes.right)
                        .addClass(this.classes.button)
                        .attr('title', this.s.dt.i18n('searchBuilder.rightTitle', i18n.rightTitle))
                        .attr('type', 'button'),
                    value: [
                        $$3('<select disabled/>')
                            .addClass(this.classes.value)
                            .addClass(this.classes.dropDown)
                            .addClass(this.classes.italic)
                            .addClass(this.classes.select)
                    ],
                    valueTitle: $$3('<option value="--valueTitle--" disabled selected hidden/>')
                        .html(this.s.dt.i18n('searchBuilder.value', i18n.value))
                };
                // If the greyscale option is selected then add the class to add the grey colour to SearchBuilder
                if (this.c.greyscale) {
                    this.dom.data.addClass(this.classes.greyscale);
                    this.dom.condition.addClass(this.classes.greyscale);
                    this.dom.defaultValue.addClass(this.classes.greyscale);
                    for (var _i = 0, _a = this.dom.value; _i < _a.length; _i++) {
                        var val = _a[_i];
                        val.addClass(this.classes.greyscale);
                    }
                }
                $$3(window).on('resize.dtsb', dataTable$2.util.throttle(function () {
                    _this.s.topGroup.trigger('dtsb-redrawLogic');
                }));
                this._buildCriteria();
                return this;
            }
            /**
             * Escape html characters within a string
             *
             * @param txt the string to be escaped
             * @returns the escaped string
             */
            Criteria._escapeHTML = function (txt) {
                return txt
                    .toString()
                    .replace(/&lt;/g, '<')
                    .replace(/&gt;/g, '>')
                    .replace(/&quot;/g, '"')
                    .replace(/&amp;/g, '&');
            };
            /**
             * Redraw the DataTable with the current search parameters
             */
            Criteria.prototype.doSearch = function () {
                // Only do the search if live search is disabled, otherwise the search
                // is triggered by the button at the top level group.
                if (this.c.liveSearch) {
                    this.s.dt.draw();
                }
            };
            /**
             * Parses formatted numbers down to a form where they can be compared.
             * Note that this does not account for different decimal characters. Use
             * parseNumber instead on the instance.
             *
             * @param val the value to convert
             * @returns the converted value
             */
            Criteria.parseNumFmt = function (val) {
                return +val.replace(/(?!^-)[^0-9.]/g, '');
            };
            /**
             * Adds the left button to the criteria
             */
            Criteria.prototype.updateArrows = function (hasSiblings) {
                if (hasSiblings === void 0) { hasSiblings = false; }
                // Empty the container and append all of the elements in the correct order
                this.dom.container.children().detach();
                this.dom.container
                    .append(this.dom.data)
                    .append(this.dom.condition)
                    .append(this.dom.inputCont);
                this.setListeners();
                // Trigger the inserted events for the value elements as they are inserted
                if (this.dom.value[0] !== undefined) {
                    $$3(this.dom.value[0]).trigger('dtsb-inserted');
                }
                for (var i = 1; i < this.dom.value.length; i++) {
                    this.dom.inputCont.append(this.dom.value[i]);
                    $$3(this.dom.value[i]).trigger('dtsb-inserted');
                }
                // If this is a top level criteria then don't let it move left
                if (this.s.depth > 1) {
                    this.dom.buttons.append(this.dom.left);
                }
                // If the depthLimit of the query has been hit then don't add the right button
                if ((this.c.depthLimit === false || this.s.depth < this.c.depthLimit) && hasSiblings) {
                    this.dom.buttons.append(this.dom.right);
                }
                else {
                    this.dom.right.remove();
                }
                this.dom.buttons.append(this.dom["delete"]);
                this.dom.container.append(this.dom.buttons);
            };
            /**
             * Destroys the criteria, removing listeners and container from the dom
             */
            Criteria.prototype.destroy = function () {
                // Turn off listeners
                this.dom.data.off('.dtsb');
                this.dom.condition.off('.dtsb');
                this.dom["delete"].off('.dtsb');
                for (var _i = 0, _a = this.dom.value; _i < _a.length; _i++) {
                    var val = _a[_i];
                    val.off('.dtsb');
                }
                // Remove container from the dom
                this.dom.container.remove();
            };
            /**
             * Passes in the data for the row and compares it against this single criteria
             *
             * @param rowData The data for the row to be compared
             * @returns boolean Whether the criteria has passed
             */
            Criteria.prototype.search = function (rowData, rowIdx) {
                var settings = this.s.dt.settings()[0];
                var condition = this.s.conditions[this.s.condition];
                if (this.s.condition !== undefined && condition !== undefined) {
                    var filter = rowData[this.s.dataIdx];
                    // This check is in place for if a custom decimal character is in place
                    if (this.s.type &&
                        this.s.type.includes('num') &&
                        (settings.oLanguage.sDecimal !== '' ||
                            settings.oLanguage.sThousands !== '')) {
                        var splitRD = [rowData[this.s.dataIdx]];
                        if (settings.oLanguage.sDecimal !== '') {
                            splitRD = rowData[this.s.dataIdx].split(settings.oLanguage.sDecimal);
                        }
                        if (settings.oLanguage.sThousands !== '') {
                            for (var i = 0; i < splitRD.length; i++) {
                                splitRD[i] = splitRD[i].replace(settings.oLanguage.sThousands, ',');
                            }
                        }
                        filter = splitRD.join('.');
                    }
                    // If orthogonal data is in place we need to get it's values for searching
                    if (this.c.orthogonal.search !== 'filter') {
                        filter = settings.fastData(rowIdx, this.s.dataIdx, typeof this.c.orthogonal === 'string' ?
                            this.c.orthogonal :
                            this.c.orthogonal.search);
                    }
                    if (this.s.type === 'array') {
                        // Make sure we are working with an array
                        if (!Array.isArray(filter)) {
                            filter = [filter];
                        }
                        filter.sort();
                        for (var _i = 0, filter_1 = filter; _i < filter_1.length; _i++) {
                            var filt = filter_1[_i];
                            if (filt && typeof filt === 'string') {
                                filt = filt.replace(/[\r\n\u2028]/g, ' ');
                            }
                        }
                    }
                    else if (filter !== null && typeof filter === 'string') {
                        filter = filter.replace(/[\r\n\u2028]/g, ' ');
                    }
                    if (this.s.type.includes('html') && typeof filter === 'string') {
                        filter = filter.replace(/(<([^>]+)>)/ig, '');
                    }
                    // Not ideal, but jqueries .val() returns an empty string even
                    // when the value set is null, so we shall assume the two are equal
                    if (filter === null) {
                        filter = '';
                    }
                    return condition.search(filter, this.s.value, this);
                }
            };
            /**
             * Gets the details required to rebuild the criteria
             */
            Criteria.prototype.getDetails = function (deFormatDates) {
                if (deFormatDates === void 0) { deFormatDates = false; }
                var i;
                var settings = this.s.dt.settings()[0];
                // This check is in place for if a custom decimal character is in place
                if (this.s.type !== null &&
                    ["num", "num-fmt", "html-num", "html-num-fmt"].includes(this.s.type) &&
                    (settings.oLanguage.sDecimal !== '' || settings.oLanguage.sThousands !== '')) {
                    for (i = 0; i < this.s.value.length; i++) {
                        var splitRD = [this.s.value[i].toString()];
                        if (settings.oLanguage.sDecimal !== '') {
                            splitRD = this.s.value[i].split(settings.oLanguage.sDecimal);
                        }
                        if (settings.oLanguage.sThousands !== '') {
                            for (var j = 0; j < splitRD.length; j++) {
                                splitRD[j] = splitRD[j].replace(settings.oLanguage.sThousands, ',');
                            }
                        }
                        this.s.value[i] = splitRD.join('.');
                    }
                }
                else if (this.s.type !== null && deFormatDates) {
                    if (this.s.type.includes('date') ||
                        this.s.type.includes('time')) {
                        for (i = 0; i < this.s.value.length; i++) {
                            if (this.s.value[i].match(/^\d{4}-([0]\d|1[0-2])-([0-2]\d|3[01])$/g) === null) {
                                this.s.value[i] = '';
                            }
                        }
                    }
                    else if (this.s.type.includes('moment')) {
                        for (i = 0; i < this.s.value.length; i++) {
                            if (this.s.value[i] &&
                                this.s.value[i].length > 0 &&
                                moment()(this.s.value[i], this.s.dateFormat, true).isValid()) {
                                this.s.value[i] = moment()(this.s.value[i], this.s.dateFormat).format('YYYY-MM-DD HH:mm:ss');
                            }
                        }
                    }
                    else if (this.s.type.includes('luxon')) {
                        for (i = 0; i < this.s.value.length; i++) {
                            if (this.s.value[i] &&
                                this.s.value[i].length > 0 &&
                                luxon().DateTime.fromFormat(this.s.value[i], this.s.dateFormat).invalid === null) {
                                this.s.value[i] = luxon().DateTime.fromFormat(this.s.value[i], this.s.dateFormat).toFormat('yyyy-MM-dd HH:mm:ss');
                            }
                        }
                    }
                }
                if (this.s.type && this.s.type.includes('num') && this.s.dt.page.info().serverSide) {
                    for (i = 0; i < this.s.value.length; i++) {
                        this.s.value[i] = this.s.value[i].replace(/[^0-9.\-]/g, '');
                    }
                }
                return {
                    condition: this.s.condition,
                    data: this.s.data,
                    origData: this.s.origData,
                    type: this.s.type,
                    value: this.s.value.map(function (a) { return a !== null && a !== undefined ? a.toString() : a; })
                };
            };
            /**
             * Getter for the node for the container of the criteria
             *
             * @returns JQuery<HTMLElement> the node for the container
             */
            Criteria.prototype.getNode = function () {
                return this.dom.container;
            };
            /**
             * Parses formatted numbers down to a form where they can be compared
             *
             * @param val the value to convert
             * @returns the converted value
             */
            Criteria.prototype.parseNumber = function (val) {
                var decimal = this.s.dt.i18n('decimal');
                // Remove any periods and then replace the decimal with a period
                if (decimal && decimal !== '.') {
                    val = val.replace(/\./g, '').replace(decimal, '.');
                }
                return +val.replace(/(?!^-)[^0-9.]/g, '');
            };
            /**
             * Populates the criteria data, condition and value(s) as far as has been selected
             */
            Criteria.prototype.populate = function () {
                this._populateData();
                // If the column index has been found attempt to select a condition
                if (this.s.dataIdx !== -1) {
                    this._populateCondition();
                    // If the condittion has been found attempt to select the values
                    if (this.s.condition !== undefined) {
                        this._populateValue();
                    }
                }
            };
            /**
             * Rebuilds the criteria based upon the details passed in
             *
             * @param loadedCriteria the details required to rebuild the criteria
             */
            Criteria.prototype.rebuild = function (loadedCriteria) {
                // Check to see if the previously selected data exists, if so select it
                var foundData = false;
                var dataIdx, i;
                this._populateData();
                // If a data selection has previously been made attempt to find and select it
                if (loadedCriteria.data !== undefined) {
                    var italic_1 = this.classes.italic;
                    var data_1 = this.dom.data;
                    this.dom.data.children('option').each(function () {
                        if (!foundData &&
                            ($$3(this).text() === loadedCriteria.data ||
                                loadedCriteria.origData && $$3(this).prop('origData') === loadedCriteria.origData)) {
                            $$3(this).prop('selected', true);
                            data_1.removeClass(italic_1);
                            foundData = true;
                            dataIdx = parseInt($$3(this).val(), 10);
                        }
                        else {
                            $$3(this).removeProp('selected');
                        }
                    });
                }
                // If the data has been found and selected then the condition can be populated and searched
                if (foundData) {
                    this.s.data = loadedCriteria.data;
                    this.s.origData = loadedCriteria.origData;
                    this.s.dataIdx = dataIdx;
                    this.c.orthogonal = this._getOptions().orthogonal;
                    this.dom.dataTitle.remove();
                    this._populateCondition();
                    this.dom.conditionTitle.remove();
                    var condition = void 0;
                    // Check to see if the previously selected condition exists, if so select it
                    var options = this.dom.condition.children('option');
                    for (i = 0; i < options.length; i++) {
                        var option = $$3(options[i]);
                        if (loadedCriteria.condition !== undefined &&
                            option.val() === loadedCriteria.condition &&
                            typeof loadedCriteria.condition === 'string') {
                            option.prop('selected', true);
                            condition = option.val();
                        }
                        else {
                            option.removeProp('selected');
                        }
                    }
                    this.s.condition = condition;
                    // If the condition has been found and selected then the value can be populated and searched
                    if (this.s.condition !== undefined) {
                        this.dom.conditionTitle.removeProp('selected');
                        this.dom.conditionTitle.remove();
                        this.dom.condition.removeClass(this.classes.italic);
                        for (i = 0; i < options.length; i++) {
                            var opt = $$3(options[i]);
                            if (opt.val() !== this.s.condition) {
                                opt.removeProp('selected');
                            }
                        }
                        this._populateValue(loadedCriteria);
                    }
                    else {
                        this.dom.conditionTitle.prependTo(this.dom.condition).prop('selected', true);
                    }
                }
            };
            /**
             * Sets the listeners for the criteria
             */
            Criteria.prototype.setListeners = function () {
                var _this = this;
                this.dom.data
                    .unbind('change')
                    .on('change.dtsb', function () {
                        _this.dom.dataTitle.removeProp('selected');
                        // Need to go over every option to identify the correct selection
                        var options = _this.dom.data.children('option.' + _this.classes.option);
                        for (var i = 0; i < options.length; i++) {
                            var option = $$3(options[i]);
                            if (option.val() === _this.dom.data.val()) {
                                _this.dom.data.removeClass(_this.classes.italic);
                                option.prop('selected', true);
                                _this.s.dataIdx = +option.val();
                                _this.s.data = option.text();
                                _this.s.origData = option.prop('origData');
                                _this.c.orthogonal = _this._getOptions().orthogonal;
                                // When the data is changed, the values in condition and
                                // value may also change so need to renew them
                                _this._clearCondition();
                                _this._clearValue();
                                _this._populateCondition();
                                // If this criteria was previously active in the search then
                                // remove it from the search and trigger a new search
                                if (_this.s.filled) {
                                    _this.s.filled = false;
                                    _this.doSearch();
                                    _this.setListeners();
                                }
                                _this.s.dt.state.save();
                            }
                            else {
                                option.removeProp('selected');
                            }
                        }
                    });
                this.dom.condition
                    .unbind('change')
                    .on('change.dtsb', function () {
                        _this.dom.conditionTitle.removeProp('selected');
                        // Need to go over every option to identify the correct selection
                        var options = _this.dom.condition.children('option.' + _this.classes.option);
                        for (var i = 0; i < options.length; i++) {
                            var option = $$3(options[i]);
                            if (option.val() === _this.dom.condition.val()) {
                                _this.dom.condition.removeClass(_this.classes.italic);
                                option.prop('selected', true);
                                var condDisp = option.val();
                                // Find the condition that has been selected and store it internally
                                for (var _i = 0, _a = Object.keys(_this.s.conditions); _i < _a.length; _i++) {
                                    var cond = _a[_i];
                                    if (cond === condDisp) {
                                        _this.s.condition = condDisp;
                                        break;
                                    }
                                }
                                // When the condition is changed, the value selector may switch between
                                // a select element and an input element
                                _this._clearValue();
                                _this._populateValue();
                                for (var _b = 0, _c = _this.dom.value; _b < _c.length; _b++) {
                                    var val = _c[_b];
                                    // If this criteria was previously active in the search then remove
                                    // it from the search and trigger a new search
                                    if (_this.s.filled && val !== undefined && _this.dom.inputCont.has(val[0]).length !== 0) {
                                        _this.s.filled = false;
                                        _this.doSearch();
                                        _this.setListeners();
                                    }
                                }
                                if (_this.dom.value.length === 0 ||
                                    _this.dom.value.length === 1 && _this.dom.value[0] === undefined) {
                                    _this.doSearch();
                                }
                            }
                            else {
                                option.removeProp('selected');
                            }
                        }
                    });
            };
            Criteria.prototype.setupButtons = function () {
                if (window.innerWidth > 550) {
                    this.dom.container.removeClass(this.classes.vertical);
                    this.dom.buttons.css('left', null);
                    this.dom.buttons.css('top', null);
                    return;
                }
                this.dom.container.addClass(this.classes.vertical);
                this.dom.buttons.css('left', this.dom.data.innerWidth());
                this.dom.buttons.css('top', this.dom.data.position().top);
            };
            /**
             * Builds the elements of the dom together
             */
            Criteria.prototype._buildCriteria = function () {
                // Append Titles for select elements
                this.dom.data.append(this.dom.dataTitle);
                this.dom.condition.append(this.dom.conditionTitle);
                // Add elements to container
                this.dom.container
                    .append(this.dom.data)
                    .append(this.dom.condition);
                this.dom.inputCont.empty();
                for (var _i = 0, _a = this.dom.value; _i < _a.length; _i++) {
                    var val = _a[_i];
                    val.append(this.dom.valueTitle);
                    this.dom.inputCont.append(val);
                }
                // Add buttons to container
                this.dom.buttons
                    .append(this.dom["delete"])
                    .append(this.dom.right);
                this.dom.container.append(this.dom.inputCont).append(this.dom.buttons);
                this.setListeners();
            };
            /**
             * Clears the condition select element
             */
            Criteria.prototype._clearCondition = function () {
                this.dom.condition.empty();
                this.dom.conditionTitle.prop('selected', true).attr('disabled', 'true');
                this.dom.condition.prepend(this.dom.conditionTitle).prop('selectedIndex', 0);
                this.s.conditions = {};
                this.s.condition = undefined;
            };
            /**
             * Clears the value elements
             */
            Criteria.prototype._clearValue = function () {
                var val;
                if (this.s.condition !== undefined) {
                    if (this.dom.value.length > 0 && this.dom.value[0] !== undefined) {
                        // Remove all of the value elements
                        for (var _i = 0, _a = this.dom.value; _i < _a.length; _i++) {
                            val = _a[_i];
                            if (val !== undefined) {
                                // Timeout is annoying but because of IOS
                                setTimeout(function () {
                                    val.remove();
                                }, 50);
                            }
                        }
                    }
                    // Call the init function to get the value elements for this condition
                    this.dom.value = [].concat(this.s.conditions[this.s.condition].init(this, Criteria.updateListener));
                    if (this.dom.value.length > 0 && this.dom.value[0] !== undefined) {
                        this.dom.inputCont
                            .empty()
                            .append(this.dom.value[0])
                            .insertAfter(this.dom.condition);
                        $$3(this.dom.value[0]).trigger('dtsb-inserted');
                        // Insert all of the value elements
                        for (var i = 1; i < this.dom.value.length; i++) {
                            this.dom.inputCont.append(this.dom.value[i]);
                            $$3(this.dom.value[i]).trigger('dtsb-inserted');
                        }
                    }
                }
                else {
                    // Remove all of the value elements
                    for (var _b = 0, _c = this.dom.value; _b < _c.length; _b++) {
                        val = _c[_b];
                        if (val !== undefined) {
                            // Timeout is annoying but because of IOS
                            setTimeout(function () {
                                val.remove();
                            }, 50);
                        }
                    }
                    // Append the default valueTitle to the default select element
                    this.dom.valueTitle
                        .prop('selected', true);
                    this.dom.defaultValue
                        .append(this.dom.valueTitle)
                        .insertAfter(this.dom.condition);
                }
                this.s.value = [];
                this.dom.value = [
                    $$3('<select disabled/>')
                        .addClass(this.classes.value)
                        .addClass(this.classes.dropDown)
                        .addClass(this.classes.italic)
                        .addClass(this.classes.select)
                        .append(this.dom.valueTitle.clone())
                ];
            };
            /**
             * Gets the options for the column
             *
             * @returns {object} The options for the column
             */
            Criteria.prototype._getOptions = function () {
                var table = this.s.dt;
                return $$3.extend(true, {}, Criteria.defaults, table.settings()[0].aoColumns[this.s.dataIdx].searchBuilder);
            };
            /**
             * Populates the condition dropdown
             */
            Criteria.prototype._populateCondition = function () {
                var conditionOpts = [];
                var conditionsLength = Object.keys(this.s.conditions).length;
                var dt = this.s.dt;
                var colInits = dt.settings()[0].aoColumns;
                var column = +this.dom.data.children('option:selected').val();
                var condition, condName;
                // If there are no conditions stored then we need to get them from the appropriate type
                if (conditionsLength === 0) {
                    this.s.type = dt.column(column).type();
                    if (colInits !== undefined) {
                        var colInit = colInits[column];
                        if (colInit.searchBuilderType !== undefined && colInit.searchBuilderType !== null) {
                            this.s.type = colInit.searchBuilderType;
                        }
                        else if (this.s.type === undefined || this.s.type === null) {
                            this.s.type = colInit.sType;
                        }
                    }
                    // If the column type is still unknown use the internal API to detect type
                    if (this.s.type === null || this.s.type === undefined) {
                        // This can only happen in DT1 - DT2 will do the invalidation of the type itself
                        if ($$3.fn.dataTable.ext.oApi) {
                            $$3.fn.dataTable.ext.oApi._fnColumnTypes(dt.settings()[0]);
                        }
                        this.s.type = dt.column(column).type();
                    }
                    // Enable the condition element
                    this.dom.condition
                        .removeAttr('disabled')
                        .empty()
                        .append(this.dom.conditionTitle)
                        .addClass(this.classes.italic);
                    this.dom.conditionTitle
                        .prop('selected', true);
                    var decimal = dt.settings()[0].oLanguage.sDecimal;
                    // This check is in place for if a custom decimal character is in place
                    if (decimal !== '' && this.s.type && this.s.type.indexOf(decimal) === this.s.type.length - decimal.length) {
                        if (this.s.type.includes('num-fmt')) {
                            this.s.type = this.s.type.replace(decimal, '');
                        }
                        else if (this.s.type.includes('num')) {
                            this.s.type = this.s.type.replace(decimal, '');
                        }
                    }
                    // Select which conditions are going to be used based on the column type
                    var conditionObj = void 0;
                    if (this.c.conditions[this.s.type] !== undefined) {
                        conditionObj = this.c.conditions[this.s.type];
                    }
                    else if (this.s.type && this.s.type.includes('datetime-')) {
                        // Date / time data types in DataTables are driven by Luxon or
                        // Moment.js.
                        conditionObj = DataTable.use('moment')
                            ? this.c.conditions.moment
                            : this.c.conditions.luxon;
                        this.s.dateFormat = this.s.type.replace(/datetime-/g, '');
                    }
                    else if (this.s.type && this.s.type.includes('moment')) {
                        conditionObj = this.c.conditions.moment;
                        this.s.dateFormat = this.s.type.replace(/moment-/g, '');
                    }
                    else if (this.s.type && this.s.type.includes('luxon')) {
                        conditionObj = this.c.conditions.luxon;
                        this.s.dateFormat = this.s.type.replace(/luxon-/g, '');
                    }
                    else {
                        conditionObj = this.c.conditions.string;
                    }
                    // Add all of the conditions to the select element
                    for (var _i = 0, _a = Object.keys(conditionObj); _i < _a.length; _i++) {
                        condition = _a[_i];
                        if (conditionObj[condition] !== null) {
                            // Serverside processing does not supply the options for the select elements
                            // Instead input elements need to be used for these instead
                            if (dt.page.info().serverSide && conditionObj[condition].init === Criteria.initSelect) {
                                var col = colInits[column];
                                if (this.s.serverData && this.s.serverData[col.data]) {
                                    conditionObj[condition].init = Criteria.initSelectSSP;
                                    conditionObj[condition].inputValue = Criteria.inputValueSelect;
                                    conditionObj[condition].isInputValid = Criteria.isInputValidSelect;
                                }
                                else {
                                    conditionObj[condition].init = Criteria.initInput;
                                    conditionObj[condition].inputValue = Criteria.inputValueInput;
                                    conditionObj[condition].isInputValid = Criteria.isInputValidInput;
                                }
                            }
                            this.s.conditions[condition] = conditionObj[condition];
                            condName = conditionObj[condition].conditionName;
                            if (typeof condName === 'function') {
                                condName = condName(dt, this.c.i18n);
                            }
                            conditionOpts.push($$3('<option>', {
                                text: condName,
                                value: condition
                            })
                                .addClass(this.classes.option)
                                .addClass(this.classes.notItalic));
                        }
                    }
                }
                // Otherwise we can just load them in
                else if (conditionsLength > 0) {
                    this.dom.condition.empty().removeAttr('disabled').addClass(this.classes.italic);
                    for (var _b = 0, _c = Object.keys(this.s.conditions); _b < _c.length; _b++) {
                        condition = _c[_b];
                        var name_1 = this.s.conditions[condition].conditionName;
                        if (typeof name_1 === 'function') {
                            name_1 = name_1(dt, this.c.i18n);
                        }
                        var newOpt = $$3('<option>', {
                            text: name_1,
                            value: condition
                        })
                            .addClass(this.classes.option)
                            .addClass(this.classes.notItalic);
                        if (this.s.condition !== undefined && this.s.condition === name_1) {
                            newOpt.prop('selected', true);
                            this.dom.condition.removeClass(this.classes.italic);
                        }
                        conditionOpts.push(newOpt);
                    }
                }
                else {
                    this.dom.condition
                        .attr('disabled', 'true')
                        .addClass(this.classes.italic);
                    return;
                }
                for (var _d = 0, conditionOpts_1 = conditionOpts; _d < conditionOpts_1.length; _d++) {
                    var opt = conditionOpts_1[_d];
                    this.dom.condition.append(opt);
                }
                // Selecting a default condition if one is set
                if (colInits[column].searchBuilder && colInits[column].searchBuilder.defaultCondition) {
                    var defaultCondition = colInits[column].searchBuilder.defaultCondition;
                    // If it is a number just use it as an index
                    if (typeof defaultCondition === 'number') {
                        this.dom.condition.prop('selectedIndex', defaultCondition);
                        this.dom.condition.trigger('change');
                    }
                    // If it is a string then things get slightly more tricly
                    else if (typeof defaultCondition === 'string') {
                        // We need to check each condition option to see if any will match
                        for (var i = 0; i < conditionOpts.length; i++) {
                            // Need to check against the stored conditions so we can match the token "cond" to the option
                            for (var _e = 0, _f = Object.keys(this.s.conditions); _e < _f.length; _e++) {
                                var cond = _f[_e];
                                condName = this.s.conditions[cond].conditionName;
                                if (
                                    // If the conditionName matches the text of the option
                                    (typeof condName === 'string' ? condName : condName(dt, this.c.i18n)) ===
                                    conditionOpts[i].text() &&
                                    // and the tokens match
                                    cond === defaultCondition) {
                                    // Select that option
                                    this.dom.condition
                                        .prop('selectedIndex', this.dom.condition.children().toArray().indexOf(conditionOpts[i][0]))
                                        .removeClass(this.classes.italic);
                                    this.dom.condition.trigger('change');
                                    i = conditionOpts.length;
                                    break;
                                }
                            }
                        }
                    }
                }
                // If not default set then default to 0, the title
                else {
                    this.dom.condition.prop('selectedIndex', 0);
                }
            };
            /**
             * Populates the data / column select element
             */
            Criteria.prototype._populateData = function () {
                var columns = this.s.dt.settings()[0].aoColumns;
                var includeColumns = this.s.dt.columns(this.c.columns).indexes().toArray();
                this.dom.data.empty().append(this.dom.dataTitle);
                for (var index = 0; index < columns.length; index++) {
                    // Need to check that the column can be filtered on before adding it
                    if (this.c.columns === true || includeColumns.includes(index)) {
                        var col = columns[index];
                        var opt = {
                            index: index,
                            origData: col.data,
                            text: (col.searchBuilderTitle || col.sTitle)
                                .replace(/(<([^>]+)>)/ig, '')
                        };
                        this.dom.data.append($$3('<option>', {
                            text: opt.text,
                            value: opt.index
                        })
                            .addClass(this.classes.option)
                            .addClass(this.classes.notItalic)
                            .prop('origData', col.data)
                            .prop('selected', this.s.dataIdx === opt.index ? true : false));
                        if (this.s.dataIdx === opt.index) {
                            this.dom.dataTitle.removeProp('selected');
                        }
                    }
                }
            };
            /**
             * Populates the Value select element
             *
             * @param loadedCriteria optional, used to reload criteria from predefined filters
             */
            Criteria.prototype._populateValue = function (loadedCriteria) {
                var _this = this;
                var prevFilled = this.s.filled;
                var i;
                this.s.filled = false;
                // Remove any previous value elements
                // Timeout is annoying but because of IOS
                setTimeout(function () {
                    _this.dom.defaultValue.remove();
                }, 50);
                var _loop_1 = function (val) {
                    // Timeout is annoying but because of IOS
                    setTimeout(function () {
                        if (val !== undefined) {
                            val.remove();
                        }
                    }, 50);
                };
                for (var _i = 0, _a = this.dom.value; _i < _a.length; _i++) {
                    var val = _a[_i];
                    _loop_1(val);
                }
                var children = this.dom.inputCont.children();
                if (children.length > 1) {
                    for (i = 0; i < children.length; i++) {
                        $$3(children[i]).remove();
                    }
                }
                // Find the column with the title matching the data for the criteria and take note of the index
                if (loadedCriteria !== undefined) {
                    this.s.dt.columns().every(function (index) {
                        if (_this.s.dt.settings()[0].aoColumns[index].sTitle === loadedCriteria.data) {
                            _this.s.dataIdx = index;
                        }
                    });
                }
                // Initialise the value elements based on the condition
                this.dom.value = [].concat(this.s.conditions[this.s.condition].init(this, Criteria.updateListener, loadedCriteria !== undefined ? loadedCriteria.value : undefined));
                if (loadedCriteria !== undefined && loadedCriteria.value !== undefined) {
                    this.s.value = loadedCriteria.value;
                }
                this.dom.inputCont.empty();
                // Insert value elements and trigger the inserted event
                if (this.dom.value[0] !== undefined) {
                    $$3(this.dom.value[0])
                        .appendTo(this.dom.inputCont)
                        .trigger('dtsb-inserted');
                }
                for (i = 1; i < this.dom.value.length; i++) {
                    $$3(this.dom.value[i])
                        .insertAfter(this.dom.value[i - 1])
                        .trigger('dtsb-inserted');
                }
                // Check if the criteria can be used in a search
                this.s.filled = this.s.conditions[this.s.condition].isInputValid(this.dom.value, this);
                this.setListeners();
                // If it can and this is different to before then trigger a draw
                if (!this.s.preventRedraw && prevFilled !== this.s.filled) {
                    // If using SSP we want to restrict the amount of server calls that take place
                    //  and this will already have taken place
                    if (!this.s.dt.page.info().serverSide) {
                        this.doSearch();
                    }
                    this.setListeners();
                }
            };
            /**
             * Provides throttling capabilities to SearchBuilder without having to use dt's _fnThrottle function
             * This is because that function is not quite suitable for our needs as it runs initially rather than waiting
             *
             * @param args arguments supplied to the throttle function
             * @returns Function that is to be run that implements the throttling
             */
            Criteria.prototype._throttle = function (fn, frequency) {
                if (frequency === void 0) { frequency = 200; }
                var last = null;
                var timer = null;
                var that = this;
                if (frequency === null) {
                    frequency = 200;
                }
                return function () {
                    var args = [];
                    for (var _i = 0; _i < arguments.length; _i++) {
                        args[_i] = arguments[_i];
                    }
                    var now = +new Date();
                    if (last !== null && now < last + frequency) {
                        clearTimeout(timer);
                    }
                    else {
                        last = now;
                    }
                    timer = setTimeout(function () {
                        last = null;
                        fn.apply(that, args);
                    }, frequency);
                };
            };
            Criteria.version = '1.1.0';
            Criteria.classes = {
                button: 'dtsb-button',
                buttonContainer: 'dtsb-buttonContainer',
                condition: 'dtsb-condition',
                container: 'dtsb-criteria',
                data: 'dtsb-data',
                "delete": 'dtsb-delete',
                dropDown: 'dtsb-dropDown',
                greyscale: 'dtsb-greyscale',
                input: 'dtsb-input',
                inputCont: 'dtsb-inputCont',
                italic: 'dtsb-italic',
                joiner: 'dtsb-joiner',
                left: 'dtsb-left',
                notItalic: 'dtsb-notItalic',
                option: 'dtsb-option',
                right: 'dtsb-right',
                select: 'dtsb-select',
                value: 'dtsb-value',
                vertical: 'dtsb-vertical'
            };
            /**
             * Default initialisation function for select conditions
             */
            Criteria.initSelect = function (that, fn, preDefined, array) {
                if (preDefined === void 0) { preDefined = null; }
                if (array === void 0) { array = false; }
                var column = that.dom.data.children('option:selected').val();
                var indexArray = that.s.dt.rows().indexes().toArray();
                var fastData = that.s.dt.settings()[0].fastData;
                that.dom.valueTitle.prop('selected', true);
                // Declare select element to be used with all of the default classes and listeners.
                var el = $$3('<select/>')
                    .addClass(Criteria.classes.value)
                    .addClass(Criteria.classes.dropDown)
                    .addClass(Criteria.classes.italic)
                    .addClass(Criteria.classes.select)
                    .append(that.dom.valueTitle)
                    .on('change.dtsb', function () {
                        $$3(this).removeClass(Criteria.classes.italic);
                        fn(that, this);
                    });
                if (that.c.greyscale) {
                    el.addClass(Criteria.classes.greyscale);
                }
                var added = [];
                var options = [];
                // Add all of the options from the table to the select element.
                // Only add one option for each possible value
                for (var _i = 0, indexArray_1 = indexArray; _i < indexArray_1.length; _i++) {
                    var index = indexArray_1[_i];
                    var filter = fastData(index, column, typeof that.c.orthogonal === 'string' ?
                        that.c.orthogonal :
                        that.c.orthogonal.search);
                    var value = {
                        filter: typeof filter === 'string' ?
                            filter.replace(/[\r\n\u2028]/g, ' ') : // Need to replace certain characters to match search values
                            filter,
                        index: index,
                        text: fastData(index, column, typeof that.c.orthogonal === 'string' ?
                            that.c.orthogonal :
                            that.c.orthogonal.display)
                    };
                    // If we are dealing with an array type, either make sure we are working with arrays, or sort them
                    if (that.s.type === 'array') {
                        value.filter = !Array.isArray(value.filter) ? [value.filter] : value.filter;
                        value.text = !Array.isArray(value.text) ? [value.text] : value.text;
                    }
                    // Function to add an option to the select element
                    var addOption = function (filt, text) {
                        if (that.s.type.includes('html') && filt !== null && typeof filt === 'string') {
                            filt.replace(/(<([^>]+)>)/ig, '');
                        }
                        // Add text and value, stripping out any html if that is the column type
                        var opt = $$3('<option>', {
                            type: Array.isArray(filt) ? 'Array' : 'String',
                            value: filt
                        })
                            .data('sbv', filt)
                            .addClass(that.classes.option)
                            .addClass(that.classes.notItalic)
                            // Have to add the text this way so that special html characters are not escaped - &amp; etc.
                            .html(typeof text === 'string' ?
                                text.replace(/(<([^>]+)>)/ig, '') :
                                text);
                        var val = opt.val();
                        // Check that this value has not already been added
                        if (added.indexOf(val) === -1) {
                            added.push(val);
                            options.push(opt);
                            if (preDefined !== null && Array.isArray(preDefined[0])) {
                                preDefined[0] = preDefined[0].sort().join(',');
                            }
                            // If this value was previously selected as indicated by preDefined, then select it again
                            if (preDefined !== null && opt.val() === preDefined[0]) {
                                opt.prop('selected', true);
                                el.removeClass(Criteria.classes.italic);
                                that.dom.valueTitle.removeProp('selected');
                            }
                        }
                    };
                    // If this is to add the individual values within the array we need to loop over the array
                    if (array) {
                        for (var i = 0; i < value.filter.length; i++) {
                            addOption(value.filter[i], value.text[i]);
                        }
                    }
                    // Otherwise the value that is in the cell is to be added
                    else {
                        addOption(value.filter, Array.isArray(value.text) ? value.text.join(', ') : value.text);
                    }
                }
                options.sort(function (a, b) {
                    if (that.s.type === 'array' ||
                        that.s.type === 'string' ||
                        that.s.type === 'html') {
                        if (a.val() < b.val()) {
                            return -1;
                        }
                        else if (a.val() > b.val()) {
                            return 1;
                        }
                        else {
                            return 0;
                        }
                    }
                    else if (that.s.type === 'num' ||
                        that.s.type === 'html-num') {
                        if (+a.val().replace(/(<([^>]+)>)/ig, '') < +b.val().replace(/(<([^>]+)>)/ig, '')) {
                            return -1;
                        }
                        else if (+a.val().replace(/(<([^>]+)>)/ig, '') > +b.val().replace(/(<([^>]+)>)/ig, '')) {
                            return 1;
                        }
                        else {
                            return 0;
                        }
                    }
                    else if (that.s.type === 'num-fmt' || that.s.type === 'html-num-fmt') {
                        if (+a.val().replace(/[^0-9.]/g, '') < +b.val().replace(/[^0-9.]/g, '')) {
                            return -1;
                        }
                        else if (+a.val().replace(/[^0-9.]/g, '') > +b.val().replace(/[^0-9.]/g, '')) {
                            return 1;
                        }
                        else {
                            return 0;
                        }
                    }
                });
                for (var _a = 0, options_1 = options; _a < options_1.length; _a++) {
                    var opt = options_1[_a];
                    el.append(opt);
                }
                return el;
            };
            /**
             * Default initialisation function for select conditions
             */
            Criteria.initSelectSSP = function (that, fn, preDefined) {
                if (preDefined === void 0) { preDefined = null; }
                that.dom.valueTitle.prop('selected', true);
                // Declare select element to be used with all of the default classes and listeners.
                var el = $$3('<select/>')
                    .addClass(Criteria.classes.value)
                    .addClass(Criteria.classes.dropDown)
                    .addClass(Criteria.classes.italic)
                    .addClass(Criteria.classes.select)
                    .append(that.dom.valueTitle)
                    .on('change.dtsb', function () {
                        $$3(this).removeClass(Criteria.classes.italic);
                        fn(that, this);
                    });
                if (that.c.greyscale) {
                    el.addClass(Criteria.classes.greyscale);
                }
                var options = [];
                for (var _i = 0, _a = that.s.serverData[that.s.origData]; _i < _a.length; _i++) {
                    var option = _a[_i];
                    var value = option.value;
                    var label = option.label;
                    // Function to add an option to the select element
                    var addOption = function (filt, text) {
                        if (that.s.type.includes('html') && filt !== null && typeof filt === 'string') {
                            filt.replace(/(<([^>]+)>)/ig, '');
                        }
                        // Add text and value, stripping out any html if that is the column type
                        var opt = $$3('<option>', {
                            type: Array.isArray(filt) ? 'Array' : 'String',
                            value: filt
                        })
                            .data('sbv', filt)
                            .addClass(that.classes.option)
                            .addClass(that.classes.notItalic)
                            // Have to add the text this way so that special html characters are not escaped - &amp; etc.
                            .html(typeof text === 'string' ?
                                text.replace(/(<([^>]+)>)/ig, '') :
                                text);
                        options.push(opt);
                        // If this value was previously selected as indicated by preDefined, then select it again
                        if (preDefined !== null && opt.val() === preDefined[0]) {
                            opt.prop('selected', true);
                            el.removeClass(Criteria.classes.italic);
                            that.dom.valueTitle.removeProp('selected');
                        }
                    };
                    addOption(value, label);
                }
                for (var _b = 0, options_2 = options; _b < options_2.length; _b++) {
                    var opt = options_2[_b];
                    el.append(opt);
                }
                return el;
            };
            /**
             * Default initialisation function for select array conditions
             *
             * This exists because there needs to be different select functionality for contains/without and equals/not
             */
            Criteria.initSelectArray = function (that, fn, preDefined) {
                if (preDefined === void 0) { preDefined = null; }
                return Criteria.initSelect(that, fn, preDefined, true);
            };
            /**
             * Default initialisation function for input conditions
             */
            Criteria.initInput = function (that, fn, preDefined) {
                if (preDefined === void 0) { preDefined = null; }
                // Declare the input element
                var searchDelay = that.s.dt.settings()[0].searchDelay;
                var el = $$3('<input/>')
                    .addClass(Criteria.classes.value)
                    .addClass(Criteria.classes.input)
                    .on('input.dtsb keypress.dtsb', that._throttle(function (e) {
                        var code = e.keyCode || e.which;
                        return fn(that, this, code);
                    }, searchDelay === null ? 100 : searchDelay));
                if (that.c.greyscale) {
                    el.addClass(Criteria.classes.greyscale);
                }
                // If there is a preDefined value then add it
                if (preDefined !== null) {
                    el.val(preDefined[0]);
                }
                // This is add responsive functionality to the logic button without redrawing everything else
                that.s.dt.one('draw.dtsb', function () {
                    that.s.topGroup.trigger('dtsb-redrawLogic');
                });
                return el;
            };
            /**
             * Default initialisation function for conditions requiring 2 inputs
             */
            Criteria.init2Input = function (that, fn, preDefined) {
                if (preDefined === void 0) { preDefined = null; }
                // Declare all of the necessary jQuery elements
                var searchDelay = that.s.dt.settings()[0].searchDelay;
                var els = [
                    $$3('<input/>')
                        .addClass(Criteria.classes.value)
                        .addClass(Criteria.classes.input)
                        .on('input.dtsb keypress.dtsb', that._throttle(function (e) {
                            var code = e.keyCode || e.which;
                            return fn(that, this, code);
                        }, searchDelay === null ? 100 : searchDelay)),
                    $$3('<span>')
                        .addClass(that.classes.joiner)
                        .html(that.s.dt.i18n('searchBuilder.valueJoiner', that.c.i18n.valueJoiner)),
                    $$3('<input/>')
                        .addClass(Criteria.classes.value)
                        .addClass(Criteria.classes.input)
                        .on('input.dtsb keypress.dtsb', that._throttle(function (e) {
                            var code = e.keyCode || e.which;
                            return fn(that, this, code);
                        }, searchDelay === null ? 100 : searchDelay))
                ];
                if (that.c.greyscale) {
                    els[0].addClass(Criteria.classes.greyscale);
                    els[2].addClass(Criteria.classes.greyscale);
                }
                // If there is a preDefined value then add it
                if (preDefined !== null) {
                    els[0].val(preDefined[0]);
                    els[2].val(preDefined[1]);
                }
                // This is add responsive functionality to the logic button without redrawing everything else
                that.s.dt.one('draw.dtsb', function () {
                    that.s.topGroup.trigger('dtsb-redrawLogic');
                });
                return els;
            };
            /**
             * Default initialisation function for date conditions
             */
            Criteria.initDate = function (that, fn, preDefined) {
                if (preDefined === void 0) { preDefined = null; }
                var searchDelay = that.s.dt.settings()[0].searchDelay;
                var i18n = that.s.dt.i18n('datetime', {});
                // Declare date element using DataTables dateTime plugin
                var el = $$3('<input/>')
                    .addClass(Criteria.classes.value)
                    .addClass(Criteria.classes.input)
                    .dtDateTime({
                        attachTo: 'input',
                        format: that.s.dateFormat ? that.s.dateFormat : undefined,
                        i18n: i18n
                    })
                    .on('change.dtsb', that._throttle(function () {
                        return fn(that, this);
                    }, searchDelay === null ? 100 : searchDelay))
                    .on('input.dtsb keypress.dtsb', function (e) {
                        that._throttle(function () {
                            var code = e.keyCode || e.which;
                            return fn(that, this, code);
                        }, searchDelay === null ? 100 : searchDelay);
                    });
                if (that.c.greyscale) {
                    el.addClass(Criteria.classes.greyscale);
                }
                // If there is a preDefined value then add it
                if (preDefined !== null) {
                    el.val(preDefined[0]);
                }
                // This is add responsive functionality to the logic button without redrawing everything else
                that.s.dt.one('draw.dtsb', function () {
                    that.s.topGroup.trigger('dtsb-redrawLogic');
                });
                return el;
            };
            Criteria.initNoValue = function (that) {
                // This is add responsive functionality to the logic button without redrawing everything else
                that.s.dt.one('draw.dtsb', function () {
                    that.s.topGroup.trigger('dtsb-redrawLogic');
                });
                return [];
            };
            Criteria.init2Date = function (that, fn, preDefined) {
                var _this = this;
                if (preDefined === void 0) { preDefined = null; }
                var searchDelay = that.s.dt.settings()[0].searchDelay;
                var i18n = that.s.dt.i18n('datetime', {});
                // Declare all of the date elements that are required using DataTables dateTime plugin
                var els = [
                    $$3('<input/>')
                        .addClass(Criteria.classes.value)
                        .addClass(Criteria.classes.input)
                        .dtDateTime({
                            attachTo: 'input',
                            format: that.s.dateFormat ? that.s.dateFormat : undefined,
                            i18n: i18n
                        })
                        .on('change.dtsb', searchDelay !== null ?
                            DataTable.util.throttle(function () {
                                return fn(that, this);
                            }, searchDelay) :
                            function () {
                                fn(that, _this);
                            })
                        .on('input.dtsb keypress.dtsb', function (e) {
                            DataTable.util.throttle(function () {
                                var code = e.keyCode || e.which;
                                return fn(that, this, code);
                            }, searchDelay === null ? 0 : searchDelay);
                        }),
                    $$3('<span>')
                        .addClass(that.classes.joiner)
                        .html(that.s.dt.i18n('searchBuilder.valueJoiner', that.c.i18n.valueJoiner)),
                    $$3('<input/>')
                        .addClass(Criteria.classes.value)
                        .addClass(Criteria.classes.input)
                        .dtDateTime({
                            attachTo: 'input',
                            format: that.s.dateFormat ? that.s.dateFormat : undefined,
                            i18n: i18n
                        })
                        .on('change.dtsb', searchDelay !== null ?
                            DataTable.util.throttle(function () {
                                return fn(that, this);
                            }, searchDelay) :
                            function () {
                                fn(that, _this);
                            })
                        .on('input.dtsb keypress.dtsb', !that.c.enterSearch &&
                            !(that.s.dt.settings()[0].oInit.search !== undefined &&
                                that.s.dt.settings()[0].oInit.search["return"]) &&
                            searchDelay !== null ?
                            DataTable.util.throttle(function () {
                                return fn(that, this);
                            }, searchDelay) :
                            function (e) {
                                var code = e.keyCode || e.which;
                                fn(that, _this, code);
                            })
                ];
                if (that.c.greyscale) {
                    els[0].addClass(Criteria.classes.greyscale);
                    els[2].addClass(Criteria.classes.greyscale);
                }
                // If there are and preDefined values then add them
                if (preDefined !== null && preDefined.length > 0) {
                    els[0].val(preDefined[0]);
                    els[2].val(preDefined[1]);
                }
                // This is add responsive functionality to the logic button without redrawing everything else
                that.s.dt.one('draw.dtsb', function () {
                    that.s.topGroup.trigger('dtsb-redrawLogic');
                });
                return els;
            };
            /**
             * Default function for select elements to validate condition
             */
            Criteria.isInputValidSelect = function (el) {
                var allFilled = true;
                // Check each element to make sure that the selections are valid
                for (var _i = 0, el_1 = el; _i < el_1.length; _i++) {
                    var element = el_1[_i];
                    if (element.children('option:selected').length ===
                        element.children('option').length -
                        element.children('option.' + Criteria.classes.notItalic).length &&
                        element.children('option:selected').length === 1 &&
                        element.children('option:selected')[0] === element.children('option')[0]) {
                        allFilled = false;
                    }
                }
                return allFilled;
            };
            /**
             * Default function for input and date elements to validate condition
             */
            Criteria.isInputValidInput = function (el) {
                var allFilled = true;
                // Check each element to make sure that the inputs are valid
                for (var _i = 0, el_2 = el; _i < el_2.length; _i++) {
                    var element = el_2[_i];
                    if (element.is('input') && element.val().length === 0) {
                        allFilled = false;
                    }
                }
                return allFilled;
            };
            /**
             * Default function for getting select conditions
             */
            Criteria.inputValueSelect = function (el) {
                var values = [];
                // Go through the select elements and push each selected option to the return array
                for (var _i = 0, el_3 = el; _i < el_3.length; _i++) {
                    var element = el_3[_i];
                    if (element.is('select')) {
                        values.push(Criteria._escapeHTML(element.children('option:selected').data('sbv')));
                    }
                }
                return values;
            };
            /**
             * Default function for getting input conditions
             */
            Criteria.inputValueInput = function (el) {
                var values = [];
                // Go through the input elements and push each value to the return array
                for (var _i = 0, el_4 = el; _i < el_4.length; _i++) {
                    var element = el_4[_i];
                    if (element.is('input')) {
                        values.push(Criteria._escapeHTML(element.val()));
                    }
                }
                return values.map(dataTable$2.util.diacritics);
            };
            /**
             * Function that is run on each element as a call back when a search should be triggered
             */
            Criteria.updateListener = function (that, el, code) {
                // When the value is changed the criteria is now complete so can be included in searches
                // Get the condition from the map based on the key that has been selected for the condition
                var condition = that.s.conditions[that.s.condition];
                var i;
                that.s.filled = condition.isInputValid(that.dom.value, that);
                that.s.value = condition.inputValue(that.dom.value, that);
                if (!that.s.filled) {
                    if (!that.c.enterSearch &&
                        !(that.s.dt.settings()[0].oInit.search !== undefined &&
                            that.s.dt.settings()[0].oInit.search["return"]) ||
                        code === 13) {
                        that.doSearch();
                    }
                    return;
                }
                if (!Array.isArray(that.s.value)) {
                    that.s.value = [that.s.value];
                }
                for (i = 0; i < that.s.value.length; i++) {
                    // If the value is an array we need to sort it
                    if (Array.isArray(that.s.value[i])) {
                        that.s.value[i].sort();
                    }
                }
                // Take note of the cursor position so that we can refocus there later
                var idx = null;
                var cursorPos = null;
                for (i = 0; i < that.dom.value.length; i++) {
                    if (el === that.dom.value[i][0]) {
                        idx = i;
                        if (el.selectionStart !== undefined) {
                            cursorPos = el.selectionStart;
                        }
                    }
                }
                if (!that.c.enterSearch &&
                    !(that.s.dt.settings()[0].oInit.search !== undefined &&
                        that.s.dt.settings()[0].oInit.search["return"]) ||
                    code === 13) {
                    // Trigger a search
                    that.doSearch();
                }
                // Refocus the element and set the correct cursor position
                if (idx !== null) {
                    that.dom.value[idx].removeClass(that.classes.italic);
                    that.dom.value[idx].focus();
                    if (cursorPos !== null) {
                        that.dom.value[idx][0].setSelectionRange(cursorPos, cursorPos);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.dateConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.equals', i18n.conditions.date.equals);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        return value === comparison[0];
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.not', i18n.conditions.date.not);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        return value !== comparison[0];
                    }
                },
                '<': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.before', i18n.conditions.date.before);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        return value < comparison[0];
                    }
                },
                '>': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.after', i18n.conditions.date.after);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        return value > comparison[0];
                    }
                },
                'between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.between', i18n.conditions.date.between);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        if (comparison[0] < comparison[1]) {
                            return comparison[0] <= value && value <= comparison[1];
                        }
                        else {
                            return comparison[1] <= value && value <= comparison[0];
                        }
                    }
                },
                '!between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notBetween', i18n.conditions.date.notBetween);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        value = value.replace(/(\/|-|,)/g, '-');
                        if (comparison[0] < comparison[1]) {
                            return !(comparison[0] <= value && value <= comparison[1]);
                        }
                        else {
                            return !(comparison[1] <= value && value <= comparison[0]);
                        }
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.empty', i18n.conditions.date.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notEmpty', i18n.conditions.date.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.momentDateConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.equals', i18n.conditions.date.equals);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return moment()(value, that.s.dateFormat).valueOf() ===
                            moment()(comparison[0], that.s.dateFormat).valueOf();
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.not', i18n.conditions.date.not);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return moment()(value, that.s.dateFormat).valueOf() !==
                            moment()(comparison[0], that.s.dateFormat).valueOf();
                    }
                },
                '<': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.before', i18n.conditions.date.before);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return moment()(value, that.s.dateFormat).valueOf() < moment()(comparison[0], that.s.dateFormat).valueOf();
                    }
                },
                '>': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.after', i18n.conditions.date.after);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return moment()(value, that.s.dateFormat).valueOf() > moment()(comparison[0], that.s.dateFormat).valueOf();
                    }
                },
                'between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.between', i18n.conditions.date.between);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        var val = moment()(value, that.s.dateFormat).valueOf();
                        var comp0 = moment()(comparison[0], that.s.dateFormat).valueOf();
                        var comp1 = moment()(comparison[1], that.s.dateFormat).valueOf();
                        if (comp0 < comp1) {
                            return comp0 <= val && val <= comp1;
                        }
                        else {
                            return comp1 <= val && val <= comp0;
                        }
                    }
                },
                '!between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notBetween', i18n.conditions.date.notBetween);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        var val = moment()(value, that.s.dateFormat).valueOf();
                        var comp0 = moment()(comparison[0], that.s.dateFormat).valueOf();
                        var comp1 = moment()(comparison[1], that.s.dateFormat).valueOf();
                        if (comp0 < comp1) {
                            return !(+comp0 <= +val && +val <= +comp1);
                        }
                        else {
                            return !(+comp1 <= +val && +val <= +comp0);
                        }
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.empty', i18n.conditions.date.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notEmpty', i18n.conditions.date.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.luxonDateConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.equals', i18n.conditions.date.equals);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return luxon().DateTime.fromFormat(value, that.s.dateFormat).ts
                            === luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.not', i18n.conditions.date.not);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return luxon().DateTime.fromFormat(value, that.s.dateFormat).ts
                            !== luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                    }
                },
                '<': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.before', i18n.conditions.date.before);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return luxon().DateTime.fromFormat(value, that.s.dateFormat).ts
                            < luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                    }
                },
                '>': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.after', i18n.conditions.date.after);
                    },
                    init: Criteria.initDate,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        return luxon().DateTime.fromFormat(value, that.s.dateFormat).ts
                            > luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                    }
                },
                'between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.between', i18n.conditions.date.between);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        var val = luxon().DateTime.fromFormat(value, that.s.dateFormat).ts;
                        var comp0 = luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                        var comp1 = luxon().DateTime.fromFormat(comparison[1], that.s.dateFormat).ts;
                        if (comp0 < comp1) {
                            return comp0 <= val && val <= comp1;
                        }
                        else {
                            return comp1 <= val && val <= comp0;
                        }
                    }
                },
                '!between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notBetween', i18n.conditions.date.notBetween);
                    },
                    init: Criteria.init2Date,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, that) {
                        var val = luxon().DateTime.fromFormat(value, that.s.dateFormat).ts;
                        var comp0 = luxon().DateTime.fromFormat(comparison[0], that.s.dateFormat).ts;
                        var comp1 = luxon().DateTime.fromFormat(comparison[1], that.s.dateFormat).ts;
                        if (comp0 < comp1) {
                            return !(+comp0 <= +val && +val <= +comp1);
                        }
                        else {
                            return !(+comp1 <= +val && +val <= +comp0);
                        }
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.empty', i18n.conditions.date.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.date.notEmpty', i18n.conditions.date.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.numConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.equals', i18n.conditions.number.equals);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        return +value === +comparison[0];
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.not', i18n.conditions.number.not);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        return +value !== +comparison[0];
                    }
                },
                '<': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.lt', i18n.conditions.number.lt);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return +value < +comparison[0];
                    }
                },
                '<=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.lte', i18n.conditions.number.lte);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return +value <= +comparison[0];
                    }
                },
                '>=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.gte', i18n.conditions.number.gte);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return +value >= +comparison[0];
                    }
                },
                '>': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.gt', i18n.conditions.number.gt);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return +value > +comparison[0];
                    }
                },
                'between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.between', i18n.conditions.number.between);
                    },
                    init: Criteria.init2Input,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        if (+comparison[0] < +comparison[1]) {
                            return +comparison[0] <= +value && +value <= +comparison[1];
                        }
                        else {
                            return +comparison[1] <= +value && +value <= +comparison[0];
                        }
                    }
                },
                '!between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.notBetween', i18n.conditions.number.notBetween);
                    },
                    init: Criteria.init2Input,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        if (+comparison[0] < +comparison[1]) {
                            return !(+comparison[0] <= +value && +value <= +comparison[1]);
                        }
                        else {
                            return !(+comparison[1] <= +value && +value <= +comparison[0]);
                        }
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.empty', i18n.conditions.number.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.notEmpty', i18n.conditions.number.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.numFmtConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.equals', i18n.conditions.number.equals);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) === criteria.parseNumber(comparison[0]);
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.not', i18n.conditions.number.not);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) !== criteria.parseNumber(comparison[0]);
                    }
                },
                '<': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.lt', i18n.conditions.number.lt);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) < criteria.parseNumber(comparison[0]);
                    }
                },
                '<=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.lte', i18n.conditions.number.lte);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) <= criteria.parseNumber(comparison[0]);
                    }
                },
                '>=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.gte', i18n.conditions.number.gte);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) >= criteria.parseNumber(comparison[0]);
                    }
                },
                '>': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.gt', i18n.conditions.number.gt);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        return criteria.parseNumber(value) > criteria.parseNumber(comparison[0]);
                    }
                },
                'between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.between', i18n.conditions.number.between);
                    },
                    init: Criteria.init2Input,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        var val = criteria.parseNumber(value);
                        var comp0 = criteria.parseNumber(comparison[0]);
                        var comp1 = criteria.parseNumber(comparison[1]);
                        if (+comp0 < +comp1) {
                            return +comp0 <= +val && +val <= +comp1;
                        }
                        else {
                            return +comp1 <= +val && +val <= +comp0;
                        }
                    }
                },
                '!between': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.notBetween', i18n.conditions.number.notBetween);
                    },
                    init: Criteria.init2Input,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison, criteria) {
                        var val = criteria.parseNumber(value);
                        var comp0 = criteria.parseNumber(comparison[0]);
                        var comp1 = criteria.parseNumber(comparison[1]);
                        if (+comp0 < +comp1) {
                            return !(+comp0 <= +val && +val <= +comp1);
                        }
                        else {
                            return !(+comp1 <= +val && +val <= +comp0);
                        }
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.empty', i18n.conditions.number.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.number.notEmpty', i18n.conditions.number.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Has to be in this order so that they are displayed correctly in select elements
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.stringConditions = {
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.equals', i18n.conditions.string.equals);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        return value === comparison[0];
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.not', i18n.conditions.string.not);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return value !== comparison[0];
                    }
                },
                'starts': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.startsWith', i18n.conditions.string.startsWith);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return value.toLowerCase().indexOf(comparison[0].toLowerCase()) === 0;
                    }
                },
                '!starts': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.notStartsWith', i18n.conditions.string.notStartsWith);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return value.toLowerCase().indexOf(comparison[0].toLowerCase()) !== 0;
                    }
                },
                'contains': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.contains', i18n.conditions.string.contains);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return value.toLowerCase().includes(comparison[0].toLowerCase());
                    }
                },
                '!contains': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.notContains', i18n.conditions.string.notContains);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return !value.toLowerCase().includes(comparison[0].toLowerCase());
                    }
                },
                'ends': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.endsWith', i18n.conditions.string.endsWith);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return value.toLowerCase().endsWith(comparison[0].toLowerCase());
                    }
                },
                '!ends': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.notEndsWith', i18n.conditions.string.notEndsWith);
                    },
                    init: Criteria.initInput,
                    inputValue: Criteria.inputValueInput,
                    isInputValid: Criteria.isInputValidInput,
                    search: function (value, comparison) {
                        return !value.toLowerCase().endsWith(comparison[0].toLowerCase());
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.empty', i18n.conditions.string.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.string.notEmpty', i18n.conditions.string.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return !(value === null || value === undefined || value.length === 0);
                    }
                }
            };
            // The order of the conditions will make eslint sad :(
            // Also have to disable member ordering for this as the private methods used are not yet declared otherwise
            Criteria.arrayConditions = {
                'contains': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.contains', i18n.conditions.array.contains);
                    },
                    init: Criteria.initSelectArray,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        return value.includes(comparison[0]);
                    }
                },
                'without': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.without', i18n.conditions.array.without);
                    },
                    init: Criteria.initSelectArray,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        return value.indexOf(comparison[0]) === -1;
                    }
                },
                '=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.equals', i18n.conditions.array.equals);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        if (value.length === comparison[0].length) {
                            for (var i = 0; i < value.length; i++) {
                                if (value[i] !== comparison[0][i]) {
                                    return false;
                                }
                            }
                            return true;
                        }
                        return false;
                    }
                },
                '!=': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.not', i18n.conditions.array.not);
                    },
                    init: Criteria.initSelect,
                    inputValue: Criteria.inputValueSelect,
                    isInputValid: Criteria.isInputValidSelect,
                    search: function (value, comparison) {
                        if (value.length === comparison[0].length) {
                            for (var i = 0; i < value.length; i++) {
                                if (value[i] !== comparison[0][i]) {
                                    return true;
                                }
                            }
                            return false;
                        }
                        return true;
                    }
                },
                'null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.empty', i18n.conditions.array.empty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value === null || value === undefined || value.length === 0;
                    }
                },
                '!null': {
                    conditionName: function (dt, i18n) {
                        return dt.i18n('searchBuilder.conditions.array.notEmpty', i18n.conditions.array.notEmpty);
                    },
                    init: Criteria.initNoValue,
                    inputValue: function () {
                        return;
                    },
                    isInputValid: function () {
                        return true;
                    },
                    search: function (value) {
                        return value !== null && value !== undefined && value.length !== 0;
                    }
                }
            };
            // eslint will be sad because we have to disable member ordering for this as the
            // private static properties used are not yet declared otherwise
            Criteria.defaults = {
                columns: true,
                conditions: {
                    'array': Criteria.arrayConditions,
                    'date': Criteria.dateConditions,
                    'html': Criteria.stringConditions,
                    'html-num': Criteria.numConditions,
                    'html-num-fmt': Criteria.numFmtConditions,
                    'luxon': Criteria.luxonDateConditions,
                    'moment': Criteria.momentDateConditions,
                    'num': Criteria.numConditions,
                    'num-fmt': Criteria.numFmtConditions,
                    'string': Criteria.stringConditions
                },
                depthLimit: false,
                enterSearch: false,
                filterChanged: undefined,
                greyscale: false,
                i18n: {
                    add: 'Add Condition',
                    button: {
                        0: 'Search Builder',
                        _: 'Search Builder (%d)'
                    },
                    clearAll: 'Clear All',
                    condition: 'Condition',
                    data: 'Data',
                    "delete": '&times',
                    deleteTitle: 'Delete filtering rule',
                    left: '<',
                    leftTitle: 'Outdent criteria',
                    logicAnd: 'And',
                    logicOr: 'Or',
                    right: '>',
                    rightTitle: 'Indent criteria',
                    search: 'Search',
                    title: {
                        0: 'Custom Search Builder',
                        _: 'Custom Search Builder (%d)'
                    },
                    value: 'Value',
                    valueJoiner: 'and'
                },
                liveSearch: true,
                logic: 'AND',
                orthogonal: {
                    display: 'display',
                    search: 'filter'
                },
                preDefined: false
            };
            return Criteria;
        }());

        var $$2;
        /**
         * Sets the value of jQuery for use in the file
         *
         * @param jq the instance of jQuery to be set
         */
        function setJQuery$1(jq) {
            $$2 = jq;
            jq.fn.dataTable;
        }
        /**
         * The Group class is used within SearchBuilder to represent a group of criteria
         */
        var Group = /** @class */ (function () {
            function Group(table, opts, topGroup, index, isChild, depth, serverData) {
                if (index === void 0) { index = 0; }
                if (isChild === void 0) { isChild = false; }
                if (depth === void 0) { depth = 1; }
                if (serverData === void 0) { serverData = undefined; }
                this.classes = $$2.extend(true, {}, Group.classes);
                // Get options from user
                this.c = $$2.extend(true, {}, Group.defaults, opts);
                this.s = {
                    criteria: [],
                    depth: depth,
                    dt: table,
                    index: index,
                    isChild: isChild,
                    logic: undefined,
                    opts: opts,
                    preventRedraw: false,
                    serverData: serverData,
                    toDrop: undefined,
                    topGroup: topGroup
                };
                this.dom = {
                    add: $$2('<button/>')
                        .addClass(this.classes.add)
                        .addClass(this.classes.button)
                        .attr('type', 'button'),
                    clear: $$2('<button>&times</button>')
                        .addClass(this.classes.button)
                        .addClass(this.classes.clearGroup)
                        .attr('type', 'button'),
                    container: $$2('<div/>')
                        .addClass(this.classes.group),
                    logic: $$2('<button><div/></button>')
                        .addClass(this.classes.logic)
                        .addClass(this.classes.button)
                        .attr('type', 'button'),
                    logicContainer: $$2('<div/>')
                        .addClass(this.classes.logicContainer),
                    search: $$2('<button/>')
                        .addClass(this.classes.search)
                        .addClass(this.classes.button)
                        .attr('type', 'button')
                        .css('display', 'none')
                };
                // A reference to the top level group is maintained throughout any subgroups and criteria that may be created
                if (this.s.topGroup === undefined) {
                    this.s.topGroup = this.dom.container;
                }
                this._setup();
                return this;
            }
            /**
             * Destroys the groups buttons, clears the internal criteria and removes it from the dom
             */
            Group.prototype.destroy = function () {
                // Turn off listeners
                this.dom.add.off('.dtsb');
                this.dom.logic.off('.dtsb');
                this.dom.search.off('.dtsb');
                // Trigger event for groups at a higher level to pick up on
                this.dom.container
                    .trigger('dtsb-destroy')
                    .remove();
                this.s.criteria = [];
            };
            /**
             * Gets the details required to rebuild the group
             */
            // Eslint upset at empty object but needs to be done
            Group.prototype.getDetails = function (deFormatDates) {
                if (deFormatDates === void 0) { deFormatDates = false; }
                if (this.s.criteria.length === 0) {
                    return {};
                }
                var details = {
                    criteria: [],
                    logic: this.s.logic
                };
                // NOTE here crit could be either a subgroup or a criteria
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    details.criteria.push(crit.criteria.getDetails(deFormatDates));
                }
                return details;
            };
            /**
             * Getter for the node for the container of the group
             *
             * @returns Node for the container of the group
             */
            Group.prototype.getNode = function () {
                return this.dom.container;
            };
            /**
             * Rebuilds the group based upon the details passed in
             *
             * @param loadedDetails the details required to rebuild the group
             */
            Group.prototype.rebuild = function (loadedDetails) {
                var crit;
                // If no criteria are stored then just return
                if (loadedDetails.criteria === undefined ||
                    loadedDetails.criteria === null ||
                    Array.isArray(loadedDetails.criteria) && loadedDetails.criteria.length === 0) {
                    return;
                }
                this.s.logic = loadedDetails.logic;
                this.dom.logic.children().first().html(this.s.logic === 'OR'
                    ? this.s.dt.i18n('searchBuilder.logicOr', this.c.i18n.logicOr)
                    : this.s.dt.i18n('searchBuilder.logicAnd', this.c.i18n.logicAnd));
                // Add all of the criteria, be it a sub group or a criteria
                if (Array.isArray(loadedDetails.criteria)) {
                    for (var _i = 0, _a = loadedDetails.criteria; _i < _a.length; _i++) {
                        crit = _a[_i];
                        if (crit.logic !== undefined) {
                            this._addPrevGroup(crit);
                        }
                        else if (crit.logic === undefined) {
                            this._addPrevCriteria(crit);
                        }
                    }
                }
                // For all of the criteria children, update the arrows incase they require changing and set the listeners
                for (var _b = 0, _c = this.s.criteria; _b < _c.length; _b++) {
                    crit = _c[_b];
                    if (crit.criteria instanceof Criteria) {
                        crit.criteria.updateArrows(this.s.criteria.length > 1);
                        this._setCriteriaListeners(crit.criteria);
                    }
                }
            };
            /**
             * Redraws the Contents of the searchBuilder Groups and Criteria
             */
            Group.prototype.redrawContents = function () {
                if (this.s.preventRedraw) {
                    return;
                }
                // Clear the container out and add the basic elements
                this.dom.container.children().detach();
                this.dom.container
                    .append(this.dom.logicContainer)
                    .append(this.dom.add);
                if (!this.c.liveSearch) {
                    this.dom.container.append(this.dom.search);
                }
                // Sort the criteria by index so that they appear in the correct order
                this.s.criteria.sort(function (a, b) {
                    if (a.criteria.s.index < b.criteria.s.index) {
                        return -1;
                    }
                    else if (a.criteria.s.index > b.criteria.s.index) {
                        return 1;
                    }
                    return 0;
                });
                this.setListeners();
                for (var i = 0; i < this.s.criteria.length; i++) {
                    var crit = this.s.criteria[i].criteria;
                    if (crit instanceof Criteria) {
                        // Reset the index to the new value
                        this.s.criteria[i].index = i;
                        this.s.criteria[i].criteria.s.index = i;
                        // Add to the group
                        this.s.criteria[i].criteria.dom.container.insertBefore(this.dom.add);
                        // Set listeners for various points
                        this._setCriteriaListeners(crit);
                        this.s.criteria[i].criteria.s.preventRedraw = this.s.preventRedraw;
                        this.s.criteria[i].criteria.rebuild(this.s.criteria[i].criteria.getDetails());
                        this.s.criteria[i].criteria.s.preventRedraw = false;
                    }
                    else if (crit instanceof Group && crit.s.criteria.length > 0) {
                        // Reset the index to the new value
                        this.s.criteria[i].index = i;
                        this.s.criteria[i].criteria.s.index = i;
                        // Add the sub group to the group
                        this.s.criteria[i].criteria.dom.container.insertBefore(this.dom.add);
                        // Redraw the contents of the group
                        crit.s.preventRedraw = this.s.preventRedraw;
                        crit.redrawContents();
                        crit.s.preventRedraw = false;
                        this._setGroupListeners(crit);
                    }
                    else {
                        // The group is empty so remove it
                        this.s.criteria.splice(i, 1);
                        i--;
                    }
                }
                this.setupLogic();
            };
            /**
             * Resizes the logic button only rather than the entire dom.
             */
            Group.prototype.redrawLogic = function () {
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    if (crit.criteria instanceof Group) {
                        crit.criteria.redrawLogic();
                    }
                }
                this.setupLogic();
            };
            /**
             * Search method, checking the row data against the criteria in the group
             *
             * @param rowData The row data to be compared
             * @returns boolean The result of the search
             */
            Group.prototype.search = function (rowData, rowIdx) {
                if (this.s.logic === 'AND') {
                    return this._andSearch(rowData, rowIdx);
                }
                else if (this.s.logic === 'OR') {
                    return this._orSearch(rowData, rowIdx);
                }
                return true;
            };
            /**
             * Locates the groups logic button to the correct location on the page
             */
            Group.prototype.setupLogic = function () {
                // Remove logic button
                this.dom.logicContainer.remove();
                this.dom.clear.remove();
                // If there are no criteria in the group then keep the logic removed and return
                if (this.s.criteria.length < 1) {
                    if (!this.s.isChild) {
                        this.dom.container.trigger('dtsb-destroy');
                        // Set criteria left margin
                        this.dom.container.css('margin-left', 0);
                    }
                    this.dom.search.css('display', 'none');
                    return;
                }
                this.dom.clear.height('0px');
                this.dom.logicContainer.append(this.dom.clear);
                if (!this.s.isChild) {
                    this.dom.search.css('display', 'inline-block');
                }
                // Prepend logic button
                this.dom.container.prepend(this.dom.logicContainer);
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    if (crit.criteria instanceof Criteria) {
                        crit.criteria.setupButtons();
                    }
                }
                // Set width, take 2 for the border
                var height = this.dom.container.outerHeight() - 1;
                this.dom.logicContainer.width(height);
                this._setLogicListener();
                // Set criteria left margin
                this.dom.container.css('margin-left', this.dom.logicContainer.outerHeight(true));
                var logicOffset = this.dom.logicContainer.offset();
                // Set horizontal alignment
                var currentLeft = logicOffset.left;
                var groupLeft = this.dom.container.offset().left;
                var shuffleLeft = currentLeft - groupLeft;
                var newPos = currentLeft - shuffleLeft - this.dom.logicContainer.outerHeight(true);
                this.dom.logicContainer.offset({ left: newPos });
                // Set vertical alignment
                var firstCrit = this.dom.logicContainer.next();
                var currentTop = logicOffset.top;
                var firstTop = $$2(firstCrit).offset().top;
                var shuffleTop = currentTop - firstTop;
                var newTop = currentTop - shuffleTop;
                this.dom.logicContainer.offset({ top: newTop });
                this.dom.clear.outerHeight(this.dom.logicContainer.height());
                this._setClearListener();
            };
            /**
             * Sets listeners on the groups elements
             */
            Group.prototype.setListeners = function () {
                var _this = this;
                this.dom.add.unbind('click');
                this.dom.add.on('click.dtsb', function () {
                    // If this is the parent group then the logic button has not been added yet
                    if (!_this.s.isChild) {
                        _this.dom.container.prepend(_this.dom.logicContainer);
                    }
                    _this.addCriteria();
                    _this.dom.container.trigger('dtsb-add');
                    _this.s.dt.state.save();
                    return false;
                });
                this.dom.search
                    .off('click.dtsb')
                    .on('click.dtsb', function () {
                        _this.s.dt.draw();
                    });
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    crit.criteria.setListeners();
                }
                this._setClearListener();
                this._setLogicListener();
            };
            /**
             * Adds a criteria to the group
             *
             * @param crit Instance of Criteria to be added to the group
             */
            Group.prototype.addCriteria = function (crit) {
                if (crit === void 0) { crit = null; }
                var index = crit === null ? this.s.criteria.length : crit.s.index;
                var criteria = new Criteria(this.s.dt, this.s.opts, this.s.topGroup, index, this.s.depth, this.s.serverData, this.c.liveSearch);
                // If a Criteria has been passed in then set the values to continue that
                if (crit !== null) {
                    criteria.c = crit.c;
                    criteria.s = crit.s;
                    criteria.s.depth = this.s.depth;
                    criteria.classes = crit.classes;
                }
                criteria.populate();
                var inserted = false;
                for (var i = 0; i < this.s.criteria.length; i++) {
                    if (i === 0 && this.s.criteria[i].criteria.s.index > criteria.s.index) {
                        // Add the node for the criteria at the start of the group
                        criteria.getNode().insertBefore(this.s.criteria[i].criteria.dom.container);
                        inserted = true;
                    }
                    else if (i < this.s.criteria.length - 1 &&
                        this.s.criteria[i].criteria.s.index < criteria.s.index &&
                        this.s.criteria[i + 1].criteria.s.index > criteria.s.index) {
                        // Add the node for the criteria in the correct location
                        criteria.getNode().insertAfter(this.s.criteria[i].criteria.dom.container);
                        inserted = true;
                    }
                }
                if (!inserted) {
                    criteria.getNode().insertBefore(this.dom.add);
                }
                // Add the details for this criteria to the array
                this.s.criteria.push({
                    criteria: criteria,
                    index: index
                });
                this.s.criteria = this.s.criteria.sort(function (a, b) { return a.criteria.s.index - b.criteria.s.index; });
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var opt = _a[_i];
                    if (opt.criteria instanceof Criteria) {
                        opt.criteria.updateArrows(this.s.criteria.length > 1);
                    }
                }
                this._setCriteriaListeners(criteria);
                criteria.setListeners();
                this.setupLogic();
            };
            /**
             * Checks the group to see if it has any filled criteria
             */
            Group.prototype.checkFilled = function () {
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    if (crit.criteria instanceof Criteria && crit.criteria.s.filled ||
                        crit.criteria instanceof Group && crit.criteria.checkFilled()) {
                        return true;
                    }
                }
                return false;
            };
            /**
             * Gets the count for the number of criteria in this group and any sub groups
             */
            Group.prototype.count = function () {
                var count = 0;
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    if (crit.criteria instanceof Group) {
                        count += crit.criteria.count();
                    }
                    else {
                        count++;
                    }
                }
                return count;
            };
            /**
             * Rebuilds a sub group that previously existed
             *
             * @param loadedGroup The details of a group within this group
             */
            Group.prototype._addPrevGroup = function (loadedGroup) {
                var idx = this.s.criteria.length;
                var group = new Group(this.s.dt, this.c, this.s.topGroup, idx, true, this.s.depth + 1, this.s.serverData);
                // Add the new group to the criteria array
                this.s.criteria.push({
                    criteria: group,
                    index: idx,
                    logic: group.s.logic
                });
                // Rebuild it with the previous conditions for that group
                group.rebuild(loadedGroup);
                this.s.criteria[idx].criteria = group;
                this.s.topGroup.trigger('dtsb-redrawContents');
                this._setGroupListeners(group);
            };
            /**
             * Rebuilds a criteria of this group that previously existed
             *
             * @param loadedCriteria The details of a criteria within the group
             */
            Group.prototype._addPrevCriteria = function (loadedCriteria) {
                var idx = this.s.criteria.length;
                var criteria = new Criteria(this.s.dt, this.s.opts, this.s.topGroup, idx, this.s.depth, this.s.serverData);
                criteria.populate();
                // Add the new criteria to the criteria array
                this.s.criteria.push({
                    criteria: criteria,
                    index: idx
                });
                // Rebuild it with the previous conditions for that criteria
                criteria.s.preventRedraw = this.s.preventRedraw;
                criteria.rebuild(loadedCriteria);
                criteria.s.preventRedraw = false;
                this.s.criteria[idx].criteria = criteria;
                if (!this.s.preventRedraw) {
                    this.s.topGroup.trigger('dtsb-redrawContents');
                }
            };
            /**
             * Checks And the criteria using AND logic
             *
             * @param rowData The row data to be checked against the search criteria
             * @returns boolean The result of the AND search
             */
            Group.prototype._andSearch = function (rowData, rowIdx) {
                // If there are no criteria then return true for this group
                if (this.s.criteria.length === 0) {
                    return true;
                }
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    // If the criteria is not complete then skip it
                    if (crit.criteria instanceof Criteria && !crit.criteria.s.filled) {
                        continue;
                    }
                    // Otherwise if a single one fails return false
                    else if (!crit.criteria.search(rowData, rowIdx)) {
                        return false;
                    }
                }
                // If we get to here then everything has passed, so return true for the group
                return true;
            };
            /**
             * Checks And the criteria using OR logic
             *
             * @param rowData The row data to be checked against the search criteria
             * @returns boolean The result of the OR search
             */
            Group.prototype._orSearch = function (rowData, rowIdx) {
                // If there are no criteria in the group then return true
                if (this.s.criteria.length === 0) {
                    return true;
                }
                // This will check to make sure that at least one criteria in the group is complete
                var filledfound = false;
                for (var _i = 0, _a = this.s.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    if (crit.criteria instanceof Criteria && crit.criteria.s.filled) {
                        // A completed criteria has been found so set the flag
                        filledfound = true;
                        // If the search passes then return true
                        if (crit.criteria.search(rowData, rowIdx)) {
                            return true;
                        }
                    }
                    else if (crit.criteria instanceof Group && crit.criteria.checkFilled()) {
                        filledfound = true;
                        if (crit.criteria.search(rowData, rowIdx)) {
                            return true;
                        }
                    }
                }
                // If we get here we need to return the inverse of filledfound,
                //  as if any have been found and we are here then none have passed
                return !filledfound;
            };
            /**
             * Removes a criteria from the group
             *
             * @param criteria The criteria instance to be removed
             */
            Group.prototype._removeCriteria = function (criteria, group) {
                if (group === void 0) { group = false; }
                var i;
                // If removing a criteria and there is only then then just destroy the group
                if (this.s.criteria.length <= 1 && this.s.isChild) {
                    this.destroy();
                }
                else {
                    // Otherwise splice the given criteria out and redo the indexes
                    var last = void 0;
                    for (i = 0; i < this.s.criteria.length; i++) {
                        if (this.s.criteria[i].index === criteria.s.index &&
                            (!group || this.s.criteria[i].criteria instanceof Group)) {
                            last = i;
                        }
                    }
                    // We want to remove the last element with the desired index, as its replacement will be inserted before it
                    if (last !== undefined) {
                        this.s.criteria.splice(last, 1);
                    }
                    for (i = 0; i < this.s.criteria.length; i++) {
                        this.s.criteria[i].index = i;
                        this.s.criteria[i].criteria.s.index = i;
                    }
                }
            };
            /**
             * Sets the listeners in group for a criteria
             *
             * @param criteria The criteria for the listeners to be set on
             */
            Group.prototype._setCriteriaListeners = function (criteria) {
                var _this = this;
                criteria.dom["delete"]
                    .unbind('click')
                    .on('click.dtsb', function () {
                        _this._removeCriteria(criteria);
                        criteria.dom.container.remove();
                        for (var _i = 0, _a = _this.s.criteria; _i < _a.length; _i++) {
                            var crit = _a[_i];
                            if (crit.criteria instanceof Criteria) {
                                crit.criteria.updateArrows(_this.s.criteria.length > 1);
                            }
                        }
                        criteria.destroy();
                        _this.s.dt.draw();
                        _this.s.topGroup.trigger('dtsb-redrawContents');
                        return false;
                    });
                criteria.dom.right
                    .unbind('click')
                    .on('click.dtsb', function () {
                        var idx = criteria.s.index;
                        var group = new Group(_this.s.dt, _this.s.opts, _this.s.topGroup, criteria.s.index, true, _this.s.depth + 1, _this.s.serverData);
                        // Add the criteria that is to be moved to the new group
                        group.addCriteria(criteria);
                        // Update the details in the current groups criteria array
                        _this.s.criteria[idx].criteria = group;
                        _this.s.criteria[idx].logic = 'AND';
                        _this.s.topGroup.trigger('dtsb-redrawContents');
                        _this._setGroupListeners(group);
                        return false;
                    });
                criteria.dom.left
                    .unbind('click')
                    .on('click.dtsb', function () {
                        _this.s.toDrop = new Criteria(_this.s.dt, _this.s.opts, _this.s.topGroup, criteria.s.index, undefined, _this.s.serverData);
                        _this.s.toDrop.s = criteria.s;
                        _this.s.toDrop.c = criteria.c;
                        _this.s.toDrop.classes = criteria.classes;
                        _this.s.toDrop.populate();
                        // The dropCriteria event mutates the reference to the index so need to store it
                        var index = _this.s.toDrop.s.index;
                        _this.dom.container.trigger('dtsb-dropCriteria');
                        criteria.s.index = index;
                        _this._removeCriteria(criteria);
                        // By tracking the top level group we can directly trigger a redraw on it,
                        //  bubbling is also possible, but that is slow with deep levelled groups
                        _this.s.topGroup.trigger('dtsb-redrawContents');
                        _this.s.dt.draw();
                        return false;
                    });
            };
            /**
             * Set's the listeners for the group clear button
             */
            Group.prototype._setClearListener = function () {
                var _this = this;
                this.dom.clear
                    .unbind('click')
                    .on('click.dtsb', function () {
                        if (!_this.s.isChild) {
                            _this.dom.container.trigger('dtsb-clearContents');
                            return false;
                        }
                        _this.destroy();
                        _this.s.topGroup.trigger('dtsb-redrawContents');
                        return false;
                    });
            };
            /**
             * Sets listeners for sub groups of this group
             *
             * @param group The sub group that the listeners are to be set on
             */
            Group.prototype._setGroupListeners = function (group) {
                var _this = this;
                // Set listeners for the new group
                group.dom.add
                    .unbind('click')
                    .on('click.dtsb', function () {
                        _this.setupLogic();
                        _this.dom.container.trigger('dtsb-add');
                        return false;
                    });
                group.dom.container
                    .unbind('dtsb-add')
                    .on('dtsb-add.dtsb', function () {
                        _this.setupLogic();
                        _this.dom.container.trigger('dtsb-add');
                        return false;
                    });
                group.dom.container
                    .unbind('dtsb-destroy')
                    .on('dtsb-destroy.dtsb', function () {
                        _this._removeCriteria(group, true);
                        group.dom.container.remove();
                        _this.setupLogic();
                        return false;
                    });
                group.dom.container
                    .unbind('dtsb-dropCriteria')
                    .on('dtsb-dropCriteria.dtsb', function () {
                        var toDrop = group.s.toDrop;
                        toDrop.s.index = group.s.index;
                        toDrop.updateArrows(_this.s.criteria.length > 1);
                        _this.addCriteria(toDrop);
                        return false;
                    });
                group.setListeners();
            };
            /**
             * Sets up the Group instance, setting listeners and appending elements
             */
            Group.prototype._setup = function () {
                this.setListeners();
                this.dom.add.html(this.s.dt.i18n('searchBuilder.add', this.c.i18n.add));
                this.dom.search.html(this.s.dt.i18n('searchBuilder.search', this.c.i18n.search));
                this.dom.logic.children().first().html(this.c.logic === 'OR'
                    ? this.s.dt.i18n('searchBuilder.logicOr', this.c.i18n.logicOr)
                    : this.s.dt.i18n('searchBuilder.logicAnd', this.c.i18n.logicAnd));
                this.s.logic = this.c.logic === 'OR' ? 'OR' : 'AND';
                if (this.c.greyscale) {
                    this.dom.logic.addClass(this.classes.greyscale);
                }
                this.dom.logicContainer.append(this.dom.logic).append(this.dom.clear);
                // Only append the logic button immediately if this is a sub group,
                //  otherwise it will be prepended later when adding a criteria
                if (this.s.isChild) {
                    this.dom.container.append(this.dom.logicContainer);
                }
                this.dom.container.append(this.dom.add);
                if (!this.c.liveSearch) {
                    this.dom.container.append(this.dom.search);
                }
            };
            /**
             * Sets the listener for the logic button
             */
            Group.prototype._setLogicListener = function () {
                var _this = this;
                this.dom.logic
                    .unbind('click')
                    .on('click.dtsb', function () {
                        _this._toggleLogic();
                        _this.s.dt.draw();
                        for (var _i = 0, _a = _this.s.criteria; _i < _a.length; _i++) {
                            var crit = _a[_i];
                            crit.criteria.setListeners();
                        }
                    });
            };
            /**
             * Toggles the logic for the group
             */
            Group.prototype._toggleLogic = function () {
                if (this.s.logic === 'OR') {
                    this.s.logic = 'AND';
                    this.dom.logic.children().first().html(this.s.dt.i18n('searchBuilder.logicAnd', this.c.i18n.logicAnd));
                }
                else if (this.s.logic === 'AND') {
                    this.s.logic = 'OR';
                    this.dom.logic.children().first().html(this.s.dt.i18n('searchBuilder.logicOr', this.c.i18n.logicOr));
                }
            };
            Group.version = '1.1.0';
            Group.classes = {
                add: 'dtsb-add',
                button: 'dtsb-button',
                clearGroup: 'dtsb-clearGroup',
                greyscale: 'dtsb-greyscale',
                group: 'dtsb-group',
                inputButton: 'dtsb-iptbtn',
                logic: 'dtsb-logic',
                logicContainer: 'dtsb-logicContainer',
                search: 'dtsb-search'
            };
            Group.defaults = {
                columns: true,
                conditions: {
                    'date': Criteria.dateConditions,
                    'html': Criteria.stringConditions,
                    'html-num': Criteria.numConditions,
                    'html-num-fmt': Criteria.numFmtConditions,
                    'luxon': Criteria.luxonDateConditions,
                    'moment': Criteria.momentDateConditions,
                    'num': Criteria.numConditions,
                    'num-fmt': Criteria.numFmtConditions,
                    'string': Criteria.stringConditions
                },
                depthLimit: false,
                enterSearch: false,
                filterChanged: undefined,
                greyscale: false,
                liveSearch: true,
                i18n: {
                    add: 'Add Condition',
                    button: {
                        0: 'Search Builder',
                        _: 'Search Builder (%d)'
                    },
                    clearAll: 'Clear All',
                    condition: 'Condition',
                    data: 'Data',
                    "delete": '&times',
                    deleteTitle: 'Delete filtering rule',
                    left: '<',
                    leftTitle: 'Outdent criteria',
                    logicAnd: 'And',
                    logicOr: 'Or',
                    right: '>',
                    rightTitle: 'Indent criteria',
                    search: 'Search',
                    title: {
                        0: 'Custom Search Builder',
                        _: 'Custom Search Builder (%d)'
                    },
                    value: 'Value',
                    valueJoiner: 'and'
                },
                logic: 'AND',
                orthogonal: {
                    display: 'display',
                    search: 'filter'
                },
                preDefined: false
            };
            return Group;
        }());

        var $$1;
        var dataTable$1;
        /**
         * Sets the value of jQuery for use in the file
         *
         * @param jq the instance of jQuery to be set
         */
        function setJQuery(jq) {
            $$1 = jq;
            dataTable$1 = jq.fn.DataTable;
        }
        /**
         * SearchBuilder class for DataTables.
         * Allows for complex search queries to be constructed and implemented on a DataTable
         */
        var SearchBuilder = /** @class */ (function () {
            function SearchBuilder(builderSettings, opts) {
                var _this = this;
                // Check that the required version of DataTables is included
                if (!dataTable$1 || !dataTable$1.versionCheck || !dataTable$1.versionCheck('2')) {
                    throw new Error('SearchBuilder requires DataTables 2 or newer');
                }
                var table = new dataTable$1.Api(builderSettings);
                this.classes = $$1.extend(true, {}, SearchBuilder.classes);
                // Get options from user
                this.c = $$1.extend(true, {}, SearchBuilder.defaults, opts);
                this.dom = {
                    clearAll: $$1('<button type="button">' + table.i18n('searchBuilder.clearAll', this.c.i18n.clearAll) + '</button>')
                        .addClass(this.classes.clearAll)
                        .addClass(this.classes.button)
                        .attr('type', 'button'),
                    container: $$1('<div/>')
                        .addClass(this.classes.container),
                    title: $$1('<div/>')
                        .addClass(this.classes.title),
                    titleRow: $$1('<div/>')
                        .addClass(this.classes.titleRow),
                    topGroup: undefined
                };
                this.s = {
                    dt: table,
                    opts: opts,
                    search: undefined,
                    serverData: undefined,
                    topGroup: undefined
                };
                // If searchbuilder is already defined for this table then return
                if (table.settings()[0]._searchBuilder !== undefined) {
                    return;
                }
                table.settings()[0]._searchBuilder = this;
                // If using SSP we want to include the previous state in the very first server call
                if (this.s.dt.page.info().serverSide) {
                    this.s.dt.on('preXhr.dtsb', function (e, settings, data) {
                        var loadedState = _this.s.dt.state.loaded();
                        if (loadedState && loadedState.searchBuilder) {
                            data.searchBuilder = _this._collapseArray(loadedState.searchBuilder);
                        }
                    });
                    this.s.dt.on('xhr.dtsb', function (e, settings, json) {
                        if (json && json.searchBuilder && json.searchBuilder.options) {
                            _this.s.serverData = json.searchBuilder.options;
                        }
                    });
                }
                // Run the remaining setup when the table is initialised
                if (this.s.dt.settings()[0]._bInitComplete) {
                    this._setUp();
                }
                else {
                    table.one('init.dt', function () {
                        _this._setUp();
                    });
                }
                return this;
            }
            /**
             * Gets the details required to rebuild the SearchBuilder as it currently is
             */
            // eslint upset at empty object but that is what it is
            SearchBuilder.prototype.getDetails = function (deFormatDates) {
                if (deFormatDates === void 0) { deFormatDates = false; }
                return this.s.topGroup
                    ? this.s.topGroup.getDetails(deFormatDates)
                    : {};
            };
            /**
             * Getter for the node of the container for the searchBuilder
             *
             * @returns JQuery<HTMLElement> the node of the container
             */
            SearchBuilder.prototype.getNode = function () {
                return this.dom.container;
            };
            /**
             * Rebuilds the SearchBuilder to a state that is provided
             *
             * @param details The details required to perform a rebuild
             */
            SearchBuilder.prototype.rebuild = function (details) {
                this.dom.clearAll.click();
                // If there are no details to rebuild then return
                if (details === undefined || details === null) {
                    return this;
                }
                this.s.topGroup.s.preventRedraw = true;
                this.s.topGroup.rebuild(details);
                this.s.topGroup.s.preventRedraw = false;
                this._checkClear();
                this._updateTitle(this.s.topGroup.count());
                this.s.topGroup.redrawContents();
                this.s.dt.draw(false);
                this.s.topGroup.setListeners();
                return this;
            };
            /**
             * Applies the defaults to preDefined criteria
             *
             * @param preDef the array of criteria to be processed.
             */
            SearchBuilder.prototype._applyPreDefDefaults = function (preDef) {
                var _this = this;
                if (preDef.criteria !== undefined && preDef.logic === undefined) {
                    preDef.logic = 'AND';
                }
                var _loop_1 = function (crit) {
                    // Apply the defaults to any further criteria
                    if (crit.criteria !== undefined) {
                        crit = this_1._applyPreDefDefaults(crit);
                    }
                    else {
                        this_1.s.dt.columns().every(function (index) {
                            if (_this.s.dt.settings()[0].aoColumns[index].sTitle === crit.data) {
                                crit.dataIdx = index;
                            }
                        });
                    }
                };
                var this_1 = this;
                for (var _i = 0, _a = preDef.criteria; _i < _a.length; _i++) {
                    var crit = _a[_i];
                    _loop_1(crit);
                }
                return preDef;
            };
            /**
             * Set's up the SearchBuilder
             */
            SearchBuilder.prototype._setUp = function (loadState) {
                var _this = this;
                if (loadState === void 0) { loadState = true; }
                // Register an Api method for getting the column type. DataTables 2 has
                // this built in
                if (typeof this.s.dt.column().type !== 'function') {
                    DataTable.Api.registerPlural('columns().types()', 'column().type()', function () {
                        return this.iterator('column', function (settings, column) {
                            return settings.aoColumns[column].sType;
                        }, 1);
                    });
                }
                // Check that DateTime is included, If not need to check if it could be used
                // eslint-disable-next-line no-extra-parens
                if (!dataTable$1.DateTime) {
                    var types = this.s.dt.columns().types().toArray();
                    if (types === undefined || types.includes(undefined) || types.includes(null)) {
                        types = [];
                        for (var _i = 0, _a = this.s.dt.settings()[0].aoColumns; _i < _a.length; _i++) {
                            var colInit = _a[_i];
                            types.push(colInit.searchBuilderType !== undefined ? colInit.searchBuilderType : colInit.sType);
                        }
                    }
                    var columnIdxs = this.s.dt.columns().toArray();
                    // If the column type is still unknown use the internal API to detect type
                    if (types === undefined || types.includes(undefined) || types.includes(null)) {
                        // This can only happen in DT1 - DT2 will do the invalidation of the type itself
                        if ($$1.fn.dataTable.ext.oApi) {
                            $$1.fn.dataTable.ext.oApi._fnColumnTypes(this.s.dt.settings()[0]);
                        }
                        types = this.s.dt.columns().types().toArray();
                    }
                    for (var i = 0; i < columnIdxs[0].length; i++) {
                        var column = columnIdxs[0][i];
                        var type = types[column];
                        if (
                            // Check if this column can be filtered
                            (this.c.columns === true ||
                                Array.isArray(this.c.columns) &&
                                this.c.columns.includes(i)) &&
                            // Check if the type is one of the restricted types
                            (type.includes('date') ||
                                type.includes('moment') ||
                                type.includes('luxon'))) {
                            alert('SearchBuilder Requires DateTime when used with dates.');
                            throw new Error('SearchBuilder requires DateTime');
                        }
                    }
                }
                this.s.topGroup = new Group(this.s.dt, this.c, undefined, undefined, undefined, undefined, this.s.serverData);
                this._setClearListener();
                this.s.dt.on('stateSaveParams.dtsb', function (e, settings, data) {
                    data.searchBuilder = _this.getDetails();
                    if (!data.scroller) {
                        data.page = _this.s.dt.page();
                    }
                    else {
                        data.start = _this.s.dt.state().start;
                    }
                });
                this.s.dt.on('stateLoadParams.dtsb', function (e, settings, data) {
                    _this.rebuild(data.searchBuilder);
                });
                this._build();
                this.s.dt.on('preXhr.dtsb', function (e, settings, data) {
                    if (_this.s.dt.page.info().serverSide) {
                        data = JSON.parse(data);
                        data.searchBuilder = _this._collapseArray(_this.getDetails(true));
                    }
                });
                this.s.dt.on('columns-reordered', function () {
                    _this.rebuild(_this.getDetails());
                });
                if (loadState) {
                    var loadedState = this.s.dt.state.loaded();
                    // If the loaded State is not null rebuild based on it for statesave
                    if (loadedState !== null && loadedState.searchBuilder !== undefined) {
                        this.s.topGroup.rebuild(loadedState.searchBuilder);
                        this.s.topGroup.dom.container.trigger('dtsb-redrawContents');
                        // If using SSP we want to restrict the amount of server calls that take place
                        //  and this information will already have been processed
                        if (!this.s.dt.page.info().serverSide) {
                            if (loadedState.page) {
                                this.s.dt.page(loadedState.page).draw('page');
                            }
                            else if (this.s.dt.scroller && loadedState.scroller) {
                                this.s.dt.scroller().scrollToRow(loadedState.scroller.topRow);
                            }
                        }
                        this.s.topGroup.setListeners();
                    }
                    // Otherwise load any predefined options
                    else if (this.c.preDefined !== false) {
                        this.c.preDefined = this._applyPreDefDefaults(this.c.preDefined);
                        this.rebuild(this.c.preDefined);
                    }
                }
                this._setEmptyListener();
                this.s.dt.state.save();
            };
            SearchBuilder.prototype._collapseArray = function (criteria) {
                if (criteria.logic === undefined) {
                    if (criteria.value !== undefined) {
                        criteria.value.sort(function (a, b) {
                            if (!isNaN(+a)) {
                                a = +a;
                                b = +b;
                            }
                            if (a < b) {
                                return -1;
                            }
                            else if (b < a) {
                                return 1;
                            }
                            else {
                                return 0;
                            }
                        });
                        criteria.value1 = criteria.value[0];
                        criteria.value2 = criteria.value[1];
                    }
                }
                else {
                    for (var i = 0; i < criteria.criteria.length; i++) {
                        criteria.criteria[i] = this._collapseArray(criteria.criteria[i]);
                    }
                }
                return criteria;
            };
            /**
             * Updates the title of the SearchBuilder
             *
             * @param count the number of filters in the SearchBuilder
             */
            SearchBuilder.prototype._updateTitle = function (count) {
                this.dom.title.html(this.s.dt.i18n('searchBuilder.title', this.c.i18n.title, count));
            };
            /**
             * Builds all of the dom elements together
             */
            SearchBuilder.prototype._build = function () {
                var _this = this;
                // Empty and setup the container
                this.dom.clearAll.remove();
                this.dom.container.empty();
                var count = this.s.topGroup.count();
                this._updateTitle(count);
                this.dom.titleRow.append(this.dom.title);
                this.dom.container.append(this.dom.titleRow);
                this.dom.topGroup = this.s.topGroup.getNode();
                this.dom.container.append(this.dom.topGroup);
                this._setRedrawListener();
                var tableNode = this.s.dt.table(0).node();
                if (!$$1.fn.dataTable.ext.search.includes(this.s.search)) {
                    // Custom search function for SearchBuilder
                    this.s.search = function (settings, searchData, dataIndex) {
                        if (settings.nTable !== tableNode) {
                            return true;
                        }
                        return _this.s.topGroup.search(searchData, dataIndex);
                    };
                    // Add SearchBuilder search function to the dataTables search array
                    $$1.fn.dataTable.ext.search.push(this.s.search);
                }
                this.s.dt.on('destroy.dtsb', function () {
                    _this.dom.container.remove();
                    _this.dom.clearAll.remove();
                    var searchIdx = $$1.fn.dataTable.ext.search.indexOf(_this.s.search);
                    while (searchIdx !== -1) {
                        $$1.fn.dataTable.ext.search.splice(searchIdx, 1);
                        searchIdx = $$1.fn.dataTable.ext.search.indexOf(_this.s.search);
                    }
                    _this.s.dt.off('.dtsb');
                    $$1(_this.s.dt.table().node()).off('.dtsb');
                });
            };
            /**
             * Checks if the clearAll button should be added or not
             */
            SearchBuilder.prototype._checkClear = function () {
                if (this.s.topGroup.s.criteria.length > 0) {
                    this.dom.clearAll.insertAfter(this.dom.title);
                    this._setClearListener();
                }
                else {
                    this.dom.clearAll.remove();
                }
            };
            /**
             * Update the count in the title/button
             *
             * @param count Number of filters applied
             */
            SearchBuilder.prototype._filterChanged = function (count) {
                var fn = this.c.filterChanged;
                if (typeof fn === 'function') {
                    fn(count, this.s.dt.i18n('searchBuilder.button', this.c.i18n.button, count));
                }
            };
            /**
             * Set the listener for the clear button
             */
            SearchBuilder.prototype._setClearListener = function () {
                var _this = this;
                this.dom.clearAll.unbind('click');
                this.dom.clearAll.on('click.dtsb', function () {
                    _this.s.topGroup = new Group(_this.s.dt, _this.c, undefined, undefined, undefined, undefined, _this.s.serverData);
                    _this._build();
                    _this.s.dt.draw();
                    _this.s.topGroup.setListeners();
                    _this.dom.clearAll.remove();
                    _this._setEmptyListener();
                    _this._filterChanged(0);
                    return false;
                });
            };
            /**
             * Set the listener for the Redraw event
             */
            SearchBuilder.prototype._setRedrawListener = function () {
                var _this = this;
                this.s.topGroup.dom.container.unbind('dtsb-redrawContents');
                this.s.topGroup.dom.container.on('dtsb-redrawContents.dtsb', function () {
                    _this._checkClear();
                    _this.s.topGroup.redrawContents();
                    _this.s.topGroup.setupLogic();
                    _this._setEmptyListener();
                    var count = _this.s.topGroup.count();
                    _this._updateTitle(count);
                    _this._filterChanged(count);
                    // If using SSP we want to restrict the amount of server calls that take place
                    //  and this information will already have been processed
                    if (!_this.s.dt.page.info().serverSide) {
                        _this.s.dt.draw();
                    }
                    _this.s.dt.state.save();
                });
                this.s.topGroup.dom.container.unbind('dtsb-redrawContents-noDraw');
                this.s.topGroup.dom.container.on('dtsb-redrawContents-noDraw.dtsb', function () {
                    _this._checkClear();
                    _this.s.topGroup.s.preventRedraw = true;
                    _this.s.topGroup.redrawContents();
                    _this.s.topGroup.s.preventRedraw = false;
                    _this.s.topGroup.setupLogic();
                    _this._setEmptyListener();
                    var count = _this.s.topGroup.count();
                    _this._updateTitle(count);
                    _this._filterChanged(count);
                });
                this.s.topGroup.dom.container.unbind('dtsb-redrawLogic');
                this.s.topGroup.dom.container.on('dtsb-redrawLogic.dtsb', function () {
                    _this.s.topGroup.redrawLogic();
                    var count = _this.s.topGroup.count();
                    _this._updateTitle(count);
                    _this._filterChanged(count);
                });
                this.s.topGroup.dom.container.unbind('dtsb-add');
                this.s.topGroup.dom.container.on('dtsb-add.dtsb', function () {
                    var count = _this.s.topGroup.count();
                    _this._updateTitle(count);
                    _this._filterChanged(count);
                    _this._checkClear();
                });
                this.s.dt.on('postEdit.dtsb postCreate.dtsb postRemove.dtsb', function () {
                    _this.s.topGroup.redrawContents();
                });
                this.s.topGroup.dom.container.unbind('dtsb-clearContents');
                this.s.topGroup.dom.container.on('dtsb-clearContents.dtsb', function () {
                    _this._setUp(false);
                    _this._filterChanged(0);
                    _this.s.dt.draw();
                });
            };
            /**
             * Sets listeners to check whether clearAll should be added or removed
             */
            SearchBuilder.prototype._setEmptyListener = function () {
                var _this = this;
                this.s.topGroup.dom.add.on('click.dtsb', function () {
                    _this._checkClear();
                });
                this.s.topGroup.dom.container.on('dtsb-destroy.dtsb', function () {
                    _this.dom.clearAll.remove();
                });
            };
            SearchBuilder.version = '1.8.1';
            SearchBuilder.classes = {
                button: 'dtsb-button',
                clearAll: 'dtsb-clearAll',
                container: 'dtsb-searchBuilder',
                inputButton: 'dtsb-iptbtn',
                title: 'dtsb-title',
                titleRow: 'dtsb-titleRow'
            };
            SearchBuilder.defaults = {
                columns: true,
                conditions: {
                    'date': Criteria.dateConditions,
                    'html': Criteria.stringConditions,
                    'html-num': Criteria.numConditions,
                    'html-num-fmt': Criteria.numFmtConditions,
                    'luxon': Criteria.luxonDateConditions,
                    'moment': Criteria.momentDateConditions,
                    'num': Criteria.numConditions,
                    'num-fmt': Criteria.numFmtConditions,
                    'string': Criteria.stringConditions
                },
                depthLimit: false,
                enterSearch: false,
                filterChanged: undefined,
                greyscale: false,
                liveSearch: true,
                i18n: {
                    add: 'Add Condition',
                    button: {
                        0: 'Search Builder',
                        _: 'Search Builder (%d)'
                    },
                    clearAll: 'Clear All',
                    condition: 'Condition',
                    conditions: {
                        array: {
                            contains: 'Contains',
                            empty: 'Empty',
                            equals: 'Equals',
                            not: 'Not',
                            notEmpty: 'Not Empty',
                            without: 'Without'
                        },
                        date: {
                            after: 'After',
                            before: 'Before',
                            between: 'Between',
                            empty: 'Empty',
                            equals: 'Equals',
                            not: 'Not',
                            notBetween: 'Not Between',
                            notEmpty: 'Not Empty'
                        },
                        // eslint-disable-next-line id-blacklist
                        number: {
                            between: 'Between',
                            empty: 'Empty',
                            equals: 'Equals',
                            gt: 'Greater Than',
                            gte: 'Greater Than Equal To',
                            lt: 'Less Than',
                            lte: 'Less Than Equal To',
                            not: 'Not',
                            notBetween: 'Not Between',
                            notEmpty: 'Not Empty'
                        },
                        // eslint-disable-next-line id-blacklist
                        string: {
                            contains: 'Contains',
                            empty: 'Empty',
                            endsWith: 'Ends With',
                            equals: 'Equals',
                            not: 'Not',
                            notContains: 'Does Not Contain',
                            notEmpty: 'Not Empty',
                            notEndsWith: 'Does Not End With',
                            notStartsWith: 'Does Not Start With',
                            startsWith: 'Starts With'
                        }
                    },
                    data: 'Data',
                    "delete": '&times',
                    deleteTitle: 'Delete filtering rule',
                    left: '<',
                    leftTitle: 'Outdent criteria',
                    logicAnd: 'And',
                    logicOr: 'Or',
                    right: '>',
                    rightTitle: 'Indent criteria',
                    search: 'Search',
                    title: {
                        0: 'Custom Search Builder',
                        _: 'Custom Search Builder (%d)'
                    },
                    value: 'Value',
                    valueJoiner: 'and'
                },
                logic: 'AND',
                orthogonal: {
                    display: 'display',
                    search: 'filter'
                },
                preDefined: false
            };
            return SearchBuilder;
        }());

        /*! SearchBuilder 1.8.1
         * ©SpryMedia Ltd - datatables.net/license/mit
         */
        setJQuery($);
        setJQuery$1($);
        setJQuery$2($);
        var dataTable = $.fn.dataTable;
        // eslint-disable-next-line no-extra-parens
        DataTable.SearchBuilder = SearchBuilder;
        // eslint-disable-next-line no-extra-parens
        dataTable.SearchBuilder = SearchBuilder;
        // eslint-disable-next-line no-extra-parens
        DataTable.Group = Group;
        // eslint-disable-next-line no-extra-parens
        dataTable.Group = Group;
        // eslint-disable-next-line no-extra-parens
        DataTable.Criteria = Criteria;
        // eslint-disable-next-line no-extra-parens
        dataTable.Criteria = Criteria;
        // eslint-disable-next-line no-extra-parens
        var apiRegister = DataTable.Api.register;
        // Set up object for plugins
        DataTable.ext.searchBuilder = {
            conditions: {}
        };
        DataTable.ext.buttons.searchBuilder = {
            action: function (e, dt, node, config) {
                this.popover(config._searchBuilder.getNode(), {
                    align: 'container',
                    span: 'container'
                });
                var topGroup = config._searchBuilder.s.topGroup;
                // Need to redraw the contents to calculate the correct positions for the elements
                if (topGroup !== undefined) {
                    topGroup.dom.container.trigger('dtsb-redrawContents-noDraw');
                }
                if (topGroup.s.criteria.length === 0) {
                    $('.' + $.fn.dataTable.Group.classes.add.replace(/ /g, '.')).click();
                }
            },
            config: {},
            init: function (dt, node, config) {
                var that = this;
                var sb = new DataTable.SearchBuilder(dt, config.config);
                dt.on('draw', function () {
                    var count = sb.s.topGroup
                        ? sb.s.topGroup.count()
                        : 0;
                    that.text(dt.i18n('searchBuilder.button', sb.c.i18n.button, count));
                });
                that.text(config.text || dt.i18n('searchBuilder.button', sb.c.i18n.button, 0));
                config._searchBuilder = sb;
            },
            text: null
        };
        apiRegister('searchBuilder.getDetails()', function (deFormatDates) {
            if (deFormatDates === void 0) { deFormatDates = false; }
            var ctx = this.context[0];
            // If SearchBuilder has not been initialised on this instance then return
            return ctx._searchBuilder ?
                ctx._searchBuilder.getDetails(deFormatDates) :
                null;
        });
        apiRegister('searchBuilder.rebuild()', function (details) {
            var ctx = this.context[0];
            // If SearchBuilder has not been initialised on this instance then return
            if (ctx._searchBuilder === undefined) {
                return null;
            }
            ctx._searchBuilder.rebuild(details);
            return this;
        });
        apiRegister('searchBuilder.container()', function () {
            var ctx = this.context[0];
            // If SearchBuilder has not been initialised on this instance then return
            return ctx._searchBuilder ?
                ctx._searchBuilder.getNode() :
                null;
        });
        /**
         * Init function for SearchBuilder
         *
         * @param settings the settings to be applied
         * @param options the options for SearchBuilder
         * @returns JQUERY<HTMLElement> Returns the node of the SearchBuilder
         */
        function _init(settings, options) {
            var api = new DataTable.Api(settings);
            var opts = options
                ? options
                : api.init().searchBuilder || DataTable.defaults.searchBuilder;
            var searchBuilder = new SearchBuilder(api, opts);
            var node = searchBuilder.getNode();
            return node;
        }
        // Attach a listener to the document which listens for DataTables initialisation
        // events so we can automatically initialise
        $(document).on('preInit.dt.dtsp', function (e, settings) {
            if (e.namespace !== 'dt') {
                return;
            }
            if (settings.oInit.searchBuilder ||
                DataTable.defaults.searchBuilder) {
                if (!settings._searchBuilder) {
                    _init(settings);
                }
            }
        });
        // DataTables `dom` feature option
        DataTable.ext.feature.push({
            cFeature: 'Q',
            fnInit: _init
        });
        // DataTables 2 layout feature
        if (DataTable.feature) {
            DataTable.feature.register('searchBuilder', _init);
        }

    })();


    return DataTable;
}));

/*! Bootstrap 5 ui integration for DataTables' SearchBuilder
 * © SpryMedia Ltd - datatables.net/license
 */

(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD
        define(['jquery', 'datatables.net-bs5', 'datatables.net-searchbuilder'], function ($) {
            return factory($, window, document);
        });
    }
    else if (typeof exports === 'object') {
        // CommonJS
        var jq = require('jquery');
        var cjsRequires = function (root, $) {
            if (!$.fn.dataTable) {
                require('datatables.net-bs5')(root, $);
            }

            if (!$.fn.dataTable.SearchBuilder) {
                require('datatables.net-searchbuilder')(root, $);
            }
        };

        if (typeof window === 'undefined') {
            module.exports = function (root, $) {
                if (!root) {
                    // CommonJS environments without a window global must pass a
                    // root. This will give an error otherwise
                    root = window;
                }

                if (!$) {
                    $ = jq(root);
                }

                cjsRequires(root, $);
                return factory($, root, root.document);
            };
        }
        else {
            cjsRequires(window, jq);
            module.exports = factory(jq, window, window.document);
        }
    }
    else {
        // Browser
        factory(jQuery, window, document);
    }
}(function ($, window, document) {
    'use strict';
    var DataTable = $.fn.dataTable;


    $.extend(true, DataTable.SearchBuilder.classes, {
        clearAll: 'btn btn-secondary dtsb-clearAll'
    });
    $.extend(true, DataTable.Group.classes, {
        add: 'btn btn-secondary dtsb-add',
        clearGroup: 'btn btn-secondary dtsb-clearGroup',
        logic: 'btn btn-secondary dtsb-logic',
        search: 'btn btn-secondary dtsb-search'
    });
    $.extend(true, DataTable.Criteria.classes, {
        condition: 'form-select dtsb-condition',
        data: 'dtsb-data form-select',
        "delete": 'btn btn-secondary dtsb-delete',
        input: 'form-control dtsb-input',
        left: 'btn btn-secondary dtsb-left',
        right: 'btn btn-secondary dtsb-right',
        select: 'form-select',
        value: 'dtsb-value'
    });


    return DataTable;
}));


/*! DateTime picker for DataTables.net v1.5.4
*
* © SpryMedia Ltd, all rights reserved.
* License: MIT datatables.net/license/mit
*/
!function (s) { var i; "function" == typeof define && define.amd ? define(["jquery"], function (t) { return s(t, window, document) }) : "object" == typeof exports ? (i = require("jquery"), "undefined" == typeof window ? module.exports = function (t, e) { return t = t || window, e = e || i(t), s(e, t, t.document) } : module.exports = s(i, window, window.document)) : s(jQuery, window, document) }(function (g, o, i) { "use strict"; function n(t, e) { if (n.factory(t, e)) return n; if (void 0 === a && (a = o.moment || o.dayjs || o.luxon || null), this.c = g.extend(!0, {}, n.defaults, e), e = this.c.classPrefix, !a && "YYYY-MM-DD" !== this.c.format) throw "DateTime: Without momentjs, dayjs or luxon only the format 'YYYY-MM-DD' can be used"; "string" == typeof this.c.minDate && (this.c.minDate = new Date(this.c.minDate)), "string" == typeof this.c.maxDate && (this.c.maxDate = new Date(this.c.maxDate)); var s = g('<div class="' + e + '"><div class="' + e + '-date"><div class="' + e + '-title"><div class="' + e + '-iconLeft"><button type="button"></button></div><div class="' + e + '-iconRight"><button type="button"></button></div><div class="' + e + '-label"><span></span><select class="' + e + '-month"></select></div><div class="' + e + '-label"><span></span><select class="' + e + '-year"></select></div></div><div class="' + e + '-buttons"><a class="' + e + '-clear"></a><a class="' + e + '-today"></a></div><div class="' + e + '-calendar"></div></div><div class="' + e + '-time"><div class="' + e + '-hours"></div><div class="' + e + '-minutes"></div><div class="' + e + '-seconds"></div></div><div class="' + e + '-error"></div></div>'); this.dom = { container: s, date: s.find("." + e + "-date"), title: s.find("." + e + "-title"), calendar: s.find("." + e + "-calendar"), time: s.find("." + e + "-time"), error: s.find("." + e + "-error"), buttons: s.find("." + e + "-buttons"), clear: s.find("." + e + "-clear"), today: s.find("." + e + "-today"), previous: s.find("." + e + "-iconLeft"), next: s.find("." + e + "-iconRight"), input: g(t) }, this.s = { d: null, display: null, minutesRange: null, secondsRange: null, namespace: "dateime-" + n._instance++, parts: { date: null !== this.c.format.match(/[YMD]|L(?!T)|l/), time: null !== this.c.format.match(/[Hhm]|LT|LTS/), seconds: -1 !== this.c.format.indexOf("s"), hours12: null !== this.c.format.match(/[haA]/) } }, this.dom.container.append(this.dom.date).append(this.dom.time).append(this.dom.error), this.dom.date.append(this.dom.title).append(this.dom.buttons).append(this.dom.calendar), this.dom.input.addClass("dt-datetime"), this._constructor() } var a; return g.extend(n.prototype, { destroy: function () { this._hide(!0), this.dom.container.off().empty(), this.dom.input.removeClass("dt-datetime").removeAttr("autocomplete").off(".datetime") }, display: function (t, e) { return void 0 !== t && this.s.display.setUTCFullYear(t), void 0 !== e && this.s.display.setUTCMonth(e - 1), void 0 !== t || void 0 !== e ? (this._setTitle(), this._setCalander(), this) : { month: this.s.display.getUTCMonth() + 1, year: this.s.display.getUTCFullYear() } }, errorMsg: function (t) { var e = this.dom.error; return t ? e.html(t) : e.empty(), this }, hide: function () { return this._hide(), this }, max: function (t) { return this.c.maxDate = "string" == typeof t ? new Date(t) : t, this._optionsTitle(), this._setCalander(), this }, min: function (t) { return this.c.minDate = "string" == typeof t ? new Date(t) : t, this._optionsTitle(), this._setCalander(), this }, owns: function (t) { return 0 < g(t).parents().filter(this.dom.container).length }, val: function (t, e) { return void 0 === t ? this.s.d : (t instanceof Date ? this.s.d = this._dateToUtc(t) : null === t || "" === t ? this.s.d = null : "--now" === t ? this.s.d = this._dateToUtc(new Date) : "string" == typeof t && (this.s.d = this._dateToUtc(this._convert(t, this.c.format, null))), !e && void 0 !== e || (this.s.d ? this._writeOutput() : this.dom.input.val(t)), this.s.display = this.s.d ? new Date(this.s.d.toString()) : new Date, this.s.display.setUTCDate(1), this._setTitle(), this._setCalander(), this._setTime(), this) }, valFormat: function (t, e) { return e ? (this.val(this._convert(e, t, null)), this) : this._convert(this.val(), null, t) }, _constructor: function () { function a() { var t = o.dom.input.val(); t !== e && (o.c.onChange.call(o, t, o.s.d, o.dom.input), e = t) } var o = this, r = this.c.classPrefix, e = this.dom.input.val(); this.s.parts.date || this.dom.date.css("display", "none"), this.s.parts.time || this.dom.time.css("display", "none"), this.s.parts.seconds || (this.dom.time.children("div." + r + "-seconds").remove(), this.dom.time.children("span").eq(1).remove()), this.c.buttons.clear || this.dom.clear.css("display", "none"), this.c.buttons.today || this.dom.today.css("display", "none"), this._optionsTitle(), g(i).on("i18n.dt", function (t, e) { e.oLanguage.datetime && (g.extend(!0, o.c.i18n, e.oLanguage.datetime), o._optionsTitle()) }), "hidden" === this.dom.input.attr("type") && (this.dom.container.addClass("inline"), this.c.attachTo = "input", this.val(this.dom.input.val(), !1), this._show()), e && this.val(e, !1), this.dom.input.attr("autocomplete", "off").on("focus.datetime click.datetime", function () { o.dom.container.is(":visible") || o.dom.input.is(":disabled") || (o.val(o.dom.input.val(), !1), o._show()) }).on("keyup.datetime", function () { o.dom.container.is(":visible") && o.val(o.dom.input.val(), !1) }), this.dom.container[0].addEventListener("focusin", function (t) { t.stopPropagation() }), this.dom.container.on("change", "select", function () { var t, e, s = g(this), i = s.val(); s.hasClass(r + "-month") ? (o._correctMonth(o.s.display, i), o._setTitle(), o._setCalander()) : s.hasClass(r + "-year") ? (o.s.display.setUTCFullYear(i), o._setTitle(), o._setCalander()) : s.hasClass(r + "-hours") || s.hasClass(r + "-ampm") ? (o.s.parts.hours12 ? (t = +g(o.dom.container).find("." + r + "-hours").val(), e = "pm" === g(o.dom.container).find("." + r + "-ampm").val(), o.s.d.setUTCHours(12 != t || e ? e && 12 != t ? 12 + t : t : 0)) : o.s.d.setUTCHours(i), o._setTime(), o._writeOutput(!0), a()) : s.hasClass(r + "-minutes") ? (o.s.d.setUTCMinutes(i), o._setTime(), o._writeOutput(!0), a()) : s.hasClass(r + "-seconds") && (o.s.d.setSeconds(i), o._setTime(), o._writeOutput(!0), a()), o.dom.input.focus(), o._position() }).on("click", function (t) { var e = o.s.d, s = "span" === t.target.nodeName.toLowerCase() ? t.target.parentNode : t.target, i = s.nodeName.toLowerCase(); if ("select" !== i) if (t.stopPropagation(), "a" === i && (t.preventDefault(), g(s).hasClass(r + "-clear") ? (o.s.d = null, o.dom.input.val(""), o._writeOutput(), o._setCalander(), o._setTime(), a()) : g(s).hasClass(r + "-today") && (o.s.display = new Date, o._setTitle(), o._setCalander())), "button" === i) { t = g(s), i = t.parent(); if (i.hasClass("disabled") && !i.hasClass("range")) t.blur(); else if (i.hasClass(r + "-iconLeft")) o.s.display.setUTCMonth(o.s.display.getUTCMonth() - 1), o._setTitle(), o._setCalander(), o.dom.input.focus(); else if (i.hasClass(r + "-iconRight")) o._correctMonth(o.s.display, o.s.display.getUTCMonth() + 1), o._setTitle(), o._setCalander(), o.dom.input.focus(); else { if (t.parents("." + r + "-time").length) { var s = t.data("value"), n = t.data("unit"), e = o._needValue(); if ("minutes" === n) { if (i.hasClass("disabled") && i.hasClass("range")) return o.s.minutesRange = s, void o._setTime(); o.s.minutesRange = null } if ("seconds" === n) { if (i.hasClass("disabled") && i.hasClass("range")) return o.s.secondsRange = s, void o._setTime(); o.s.secondsRange = null } if ("am" === s) { if (!(12 <= e.getUTCHours())) return; s = e.getUTCHours() - 12 } else if ("pm" === s) { if (!(e.getUTCHours() < 12)) return; s = e.getUTCHours() + 12 } e["hours" === n ? "setUTCHours" : "minutes" === n ? "setUTCMinutes" : "setSeconds"](s), o._setCalander(), o._setTime(), o._writeOutput(!0) } else (e = o._needValue()).setUTCDate(1), e.setUTCFullYear(t.data("year")), e.setUTCMonth(t.data("month")), e.setUTCDate(t.data("day")), o._writeOutput(!0), o.s.parts.time ? (o._setCalander(), o._setTime()) : setTimeout(function () { o._hide() }, 10); a() } } else o.dom.input.focus() }) }, _compareDates: function (t, e) { return this._isLuxon() ? a.DateTime.fromJSDate(t).toUTC().toISODate() === a.DateTime.fromJSDate(e).toUTC().toISODate() : this._dateToUtcString(t) === this._dateToUtcString(e) }, _convert: function (t, e, s) { var i; return t && (a ? this._isLuxon() ? (i = t instanceof Date ? a.DateTime.fromJSDate(t).toUTC() : a.DateTime.fromFormat(t, e)).isValid ? s ? i.toFormat(s) : i.toJSDate() : null : (i = t instanceof Date ? a.utc(t, void 0, this.c.locale, this.c.strict) : a(t, e, this.c.locale, this.c.strict)).isValid() ? s ? i.format(s) : i.toDate() : null : !e && !s || e && s ? t : e ? (i = t.match(/(\d{4})\-(\d{2})\-(\d{2})/)) ? new Date(i[1], i[2] - 1, i[3]) : null : t.getUTCFullYear() + "-" + this._pad(t.getUTCMonth() + 1) + "-" + this._pad(t.getUTCDate())) }, _correctMonth: function (t, e) { var s = this._daysInMonth(t.getUTCFullYear(), e), i = t.getUTCDate() > s; t.setUTCMonth(e), i && (t.setUTCDate(s), t.setUTCMonth(e)) }, _daysInMonth: function (t, e) { return [31, t % 4 == 0 && (t % 100 != 0 || t % 400 == 0) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][e] }, _dateToUtc: function (t) { return t && new Date(Date.UTC(t.getFullYear(), t.getMonth(), t.getDate(), t.getHours(), t.getMinutes(), t.getSeconds())) }, _dateToUtcString: function (t) { return this._isLuxon() ? a.DateTime.fromJSDate(t).toUTC().toISODate() : t.getUTCFullYear() + "-" + this._pad(t.getUTCMonth() + 1) + "-" + this._pad(t.getUTCDate()) }, _hide: function (t) { !t && "hidden" === this.dom.input.attr("type") || (t = this.s.namespace, this.dom.container.detach(), g(o).off("." + t), g(i).off("keydown." + t), g("div.dataTables_scrollBody").off("scroll." + t), g("div.DTE_Body_Content").off("scroll." + t), g(i).off("click." + t), g(this.dom.input[0].offsetParent).off("." + t)) }, _hours24To12: function (t) { return 0 === t ? 12 : 12 < t ? t - 12 : t }, _htmlDay: function (t) { var e, s; return t.empty ? '<td class="empty"></td>' : (e = ["selectable"], s = this.c.classPrefix, t.disabled && e.push("disabled"), t.today && e.push("now"), t.selected && e.push("selected"), '<td data-day="' + t.day + '" class="' + e.join(" ") + '"><button class="' + s + "-button " + s + '-day" type="button" data-year="' + t.year + '" data-month="' + t.month + '" data-day="' + t.day + '"><span>' + t.day + "</span></button></td>") }, _htmlMonth: function (t, e) { for (var s = this._dateToUtc(new Date), i = this._daysInMonth(t, e), n = new Date(Date.UTC(t, e, 1)).getUTCDay(), a = [], o = [], r = (0 < this.c.firstDay && (n -= this.c.firstDay) < 0 && (n += 7), i + n), d = r; 7 < d;)d -= 7; r += 7 - d; var l = this.c.minDate, h = this.c.maxDate; l && (l.setUTCHours(0), l.setUTCMinutes(0), l.setSeconds(0)), h && (h.setUTCHours(23), h.setUTCMinutes(59), h.setSeconds(59)); for (var u = 0, c = 0; u < r; u++) { var m = new Date(Date.UTC(t, e, u - n + 1)), f = !!this.s.d && this._compareDates(m, this.s.d), p = this._compareDates(m, s), y = u < n || i + n <= u, T = l && m < l || h && h < m, v = this.c.disableDays, f = { day: u - n + 1, month: e, year: t, selected: f, today: p, disabled: T = Array.isArray(v) && -1 !== g.inArray(m.getUTCDay(), v) || "function" == typeof v && !0 === v(m) ? !0 : T, empty: y }; o.push(this._htmlDay(f)), 7 == ++c && (this.c.showWeekNumber && o.unshift(this._htmlWeekOfYear(u - n, e, t)), a.push("<tr>" + o.join("") + "</tr>"), o = [], c = 0) } var _, D = this.c.classPrefix, C = D + "-table"; return this.c.showWeekNumber && (C += " weekNumber"), l && (_ = l >= new Date(Date.UTC(t, e, 1, 0, 0, 0)), this.dom.title.find("div." + D + "-iconLeft").css("display", _ ? "none" : "block")), h && (_ = h < new Date(Date.UTC(t, e + 1, 1, 0, 0, 0)), this.dom.title.find("div." + D + "-iconRight").css("display", _ ? "none" : "block")), '<table class="' + C + '"><thead>' + this._htmlMonthHead() + "</thead><tbody>" + a.join("") + "</tbody></table>" }, _htmlMonthHead: function () { var t = [], e = this.c.firstDay, s = this.c.i18n; this.c.showWeekNumber && t.push("<th></th>"); for (var i = 0; i < 7; i++)t.push("<th>" + function (t) { for (t += e; 7 <= t;)t -= 7; return s.weekdays[t] }(i) + "</th>"); return t.join("") }, _htmlWeekOfYear: function (t, e, s) { e = new Date(s, e, t, 0, 0, 0, 0), e.setDate(e.getDate() + 4 - (e.getDay() || 7)), t = new Date(s, 0, 1), s = Math.ceil(((e - t) / 864e5 + 1) / 7); return '<td class="' + this.c.classPrefix + '-week">' + s + "</td>" }, _isLuxon: function () { return !!(a && a.DateTime && a.Duration && a.Settings) }, _needValue: function () { return this.s.d || (this.s.d = this._dateToUtc(new Date), this.s.parts.time) || (this.s.d.setUTCHours(0), this.s.d.setUTCMinutes(0), this.s.d.setSeconds(0), this.s.d.setMilliseconds(0)), this.s.d }, _options: function (t, e, s) { s = s || e; var i = this.dom.container.find("select." + this.c.classPrefix + "-" + t); i.empty(); for (var n = 0, a = e.length; n < a; n++)i.append('<option value="' + e[n] + '">' + s[n] + "</option>") }, _optionSet: function (t, e) { var t = this.dom.container.find("select." + this.c.classPrefix + "-" + t), s = t.parent().children("span"), e = (t.val(e), t.find("option:selected")); s.html(0 !== e.length ? e.text() : this.c.i18n.unknown) }, _optionsTime: function (n, a, o, r, t) { var e, d = this.c.classPrefix, s = this.dom.container.find("div." + d + "-" + n), i = 12 === a ? function (t) { return t } : this._pad, l = d + "-table", h = this.c.i18n; if (s.length) { var u = "", c = 10, m = function (t, e, s) { 12 === a && "number" == typeof t && (12 <= o && (t += 12), 12 == t ? t = 0 : 24 == t && (t = 12)); var i = o === t || "am" === t && o < 12 || "pm" === t && 12 <= o ? "selected" : ""; return "number" == typeof t && r && -1 === g.inArray(t, r) && (i += " disabled"), s && (i += " " + s), '<td class="selectable ' + i + '"><button class="' + d + "-button " + d + '-day" type="button" data-unit="' + n + '" data-value="' + t + '"><span>' + e + "</span></button></td>" }; if (12 === a) { for (u += "<tr>", e = 1; e <= 6; e++)u += m(e, i(e)); for (u = (u += m("am", h.amPm[0])) + "</tr>" + "<tr>", e = 7; e <= 12; e++)u += m(e, i(e)); u = u + m("pm", h.amPm[1]) + "</tr>", c = 7 } else { if (24 === a) for (var f = 0, p = 0; p < 4; p++) { for (u += "<tr>", e = 0; e < 6; e++)u += m(f, i(f)), f++; u += "</tr>" } else { for (u += "<tr>", p = 0; p < 60; p += 10)u += m(p, i(p), "range"); var u = u + "</tr>" + ('</tbody></thead><table class="' + l + " " + l + '-nospace"><tbody>'), y = null !== t ? t : -1 === o ? 0 : 10 * Math.floor(o / 10); for (u += "<tr>", p = y + 1; p < y + 10; p++)u += m(p, i(p)); u += "</tr>" } c = 6 } s.empty().append('<table class="' + l + '"><thead><tr><th colspan="' + c + '">' + h[n] + "</th></tr></thead><tbody>" + u + "</tbody></table>") } }, _optionsTitle: function () { var t = this.c.i18n, e = this.c.minDate, s = this.c.maxDate, e = e ? e.getFullYear() : null, s = s ? s.getFullYear() : null, e = null !== e ? e : (new Date).getFullYear() - this.c.yearRange, s = null !== s ? s : (new Date).getFullYear() + this.c.yearRange; this._options("month", this._range(0, 11), t.months), this._options("year", this._range(e, s)), this.dom.today.text(t.today).text(t.today), this.dom.clear.text(t.clear).text(t.clear), this.dom.previous.attr("title", t.previous).children("button").text(t.previous), this.dom.next.attr("title", t.next).children("button").text(t.next) }, _pad: function (t) { return t < 10 ? "0" + t : t }, _position: function () { var t, e, s, i = "input" === this.c.attachTo ? this.dom.input.position() : this.dom.input.offset(), n = this.dom.container, a = this.dom.input.outerHeight(); n.hasClass("inline") ? n.insertAfter(this.dom.input) : (this.s.parts.date && this.s.parts.time && 550 < g(o).width() ? n.addClass("horizontal") : n.removeClass("horizontal"), "input" === this.c.attachTo ? n.css({ top: i.top + a, left: i.left }).insertAfter(this.dom.input) : n.css({ top: i.top + a, left: i.left }).appendTo("body"), t = n.outerHeight(), e = n.outerWidth(), s = g(o).scrollTop(), i.top + a + t - s > g(o).height() && (a = i.top - t, n.css("top", a < 0 ? 0 : a)), e + i.left > g(o).width() && (s = g(o).width() - e - 5, "input" === this.c.attachTo && (s -= g(n).offsetParent().offset().left), n.css("left", s < 0 ? 0 : s))) }, _range: function (t, e, s) { var i = []; s = s || 1; for (var n = t; n <= e; n += s)i.push(n); return i }, _setCalander: function () { this.s.display && this.dom.calendar.empty().append(this._htmlMonth(this.s.display.getUTCFullYear(), this.s.display.getUTCMonth())) }, _setTitle: function () { this._optionSet("month", this.s.display.getUTCMonth()), this._optionSet("year", this.s.display.getUTCFullYear()) }, _setTime: function () { function t(t) { return e.c[t + "Available"] || e._range(0, 59, e.c[t + "Increment"]) } var e = this, s = this.s.d, i = null, n = null != (i = this._isLuxon() ? a.DateTime.fromJSDate(s).toUTC() : i) ? i.hour : s ? s.getUTCHours() : -1; this._optionsTime("hours", this.s.parts.hours12 ? 12 : 24, n, this.c.hoursAvailable), this._optionsTime("minutes", 60, null != i ? i.minute : s ? s.getUTCMinutes() : -1, t("minutes"), this.s.minutesRange), this._optionsTime("seconds", 60, null != i ? i.second : s ? s.getSeconds() : -1, t("seconds"), this.s.secondsRange) }, _show: function () { var e = this, t = this.s.namespace, s = (this._position(), g(o).on("scroll." + t + " resize." + t, function () { e._position() }), g("div.DTE_Body_Content").on("scroll." + t, function () { e._position() }), g("div.dataTables_scrollBody").on("scroll." + t, function () { e._position() }), this.dom.input[0].offsetParent); s !== i.body && g(s).on("scroll." + t, function () { e._position() }), g(i).on("keydown." + t, function (t) { 9 !== t.keyCode && 27 !== t.keyCode && 13 !== t.keyCode || e._hide() }), setTimeout(function () { g(i).on("click." + t, function (t) { g(t.target).parents().filter(e.dom.container).length || t.target === e.dom.input[0] || e._hide() }) }, 10) }, _writeOutput: function (t) { var e = this.s.d, s = "", i = this.dom.input, e = (e && (s = this._convert(e, null, this.c.format)), i.val(s), new Event("change", { bubbles: !0 })); i[0].dispatchEvent(e), "hidden" === i.attr("type") && this.val(s, !1), t && i.focus() } }), n.use = function (t) { a = t }, n._instance = 0, n.type = "DateTime", n.defaults = { attachTo: "body", buttons: { clear: !1, today: !1 }, classPrefix: "dt-datetime", disableDays: null, firstDay: 1, format: "YYYY-MM-DD", hoursAvailable: null, i18n: { clear: "Clear", previous: "Previous", next: "Next", months: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"], weekdays: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], amPm: ["am", "pm"], hours: "Hour", minutes: "Minute", seconds: "Second", unknown: "-", today: "Today" }, maxDate: null, minDate: null, minutesAvailable: null, minutesIncrement: 1, strict: !0, locale: "en", onChange: function () { }, secondsAvailable: null, secondsIncrement: 1, showWeekNumber: !1, yearRange: 25 }, n.version = "1.5.4", n.factory = function (t, e) { var s = !1; return t && t.document && (i = (o = t).document), e && e.fn && e.fn.jquery && (g = e, s = !0), s }, o.DateTime || (o.DateTime = n), o.DataTable && (o.DataTable.DateTime = n), g.fn.dtDateTime = function (t) { return this.each(function () { new n(this, t) }) }, g.fn.dataTable && (g.fn.dataTable.DateTime = n, g.fn.DataTable.DateTime = n, g.fn.dataTable.Editor) && (g.fn.dataTable.Editor.DateTime = n), n });