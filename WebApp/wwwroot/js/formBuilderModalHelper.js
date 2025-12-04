/**
 * Modal Helper for managing z-index of stacked modals
 * Ensures nested modals appear correctly above parent modals
 */

var ModalHelper = (function () {
	'use strict';

	var baseZIndex = 1050;
	var modalStack = [];
	var $$ = window.$$ || window.jQuery; // Use global $$

	/**
	 * Show modal with proper z-index
	 */
	function showModal($modal, level) {
		level = level || 0;
		
		const modalZIndex = baseZIndex + 5 + (level * 10);
		const backdropZIndex = baseZIndex + 4 + (level * 10);

		// Set z-index for modal
		$modal.css('z-index', modalZIndex);

		// Track modal in stack
		modalStack.push({
			modal: $modal,
			level: level,
			zIndex: modalZIndex
		});

		// Show modal
		$modal.modal('show');

		// Adjust backdrop z-index after modal is shown
		setTimeout(function () {
			$('.modal-backdrop').each(function (idx) {
				$(this).css('z-index', baseZIndex + idx + (level * 10));
			});
		}, 100);
	}

	/**
	 * Hide modal and clean up stack
	 */
	function hideModal($modal) {
		// Remove from stack first
		modalStack = modalStack.filter(function (item) {
			return item.modal[0] !== $modal[0];
		});

		// Hide modal
		$modal.modal('hide');

		// Clean up backdrops after modal is hidden
		setTimeout(function () {
			// Remove extra backdrops (keep only one for each remaining modal)
			const remainingModals = modalStack.length;
			const $backdrops = $('.modal-backdrop');
			
			// If no modals remain, remove all backdrops
			if (remainingModals === 0) {
				$backdrops.remove();
				$('body').removeClass('modal-open');
				$('body').css('padding-right', '');
			} else {
				// Keep only the necessary number of backdrops
				if ($backdrops.length > remainingModals) {
					$backdrops.slice(remainingModals).remove();
				}
				
				// Re-adjust remaining backdrops z-index
				$('.modal-backdrop').each(function (idx) {
					$(this).css('z-index', baseZIndex + idx);
				});
			}
		}, 300);
	}

	/**
	 * Get current modal level
	 */
	function getCurrentLevel() {
		return modalStack.length;
	}

	/**
	 * Clear all modals
	 */
	function clearAll() {
		modalStack.forEach(function (item) {
			item.modal.modal('hide');
		});
		modalStack = [];
	}

	return {
		showModal: showModal,
		hideModal: hideModal,
		getCurrentLevel: getCurrentLevel,
		clearAll: clearAll
	};
})();

