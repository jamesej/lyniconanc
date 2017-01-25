(function($) {
	$('.l24-bimodal-autocomplete').each(function () {
		$(this).autocomplete({
				source: _autocompleteSources[$(this).attr('id')],
				delay: 0,
				minLength: 0
			}); 
		});
		
	$('body').delegate('.l24-bimodal-autocomplete-button', 'click', function () {
		var $ac = $(this).closest('.l24-bimodal-autocomplete-container').find('.l24-bimodal-autocomplete');
		$ac.autocomplete("search", "");
		$ac.focus();
	}).delegate('.l24-styled-dd select', 'change', function () {
		$(this).next().text($(this).find('option:selected').text());
	}).delegate('.l24-styled-dd select', 'mouseup', function () {
		var open = $(this).data('isopen');
		if (open) {
			$(this).next().text($(this).find('option:selected').text());
			var $this = $(this);
			setTimeout(function () {
				$this.trigger('choose');
				}, 100);
		}
		$(this).data('isopen', !open);
	}).delegate('.l24-styled-dd select', 'keyup', function (ev) {
		$(this).next().text($(this).find('option:selected').text());
		if (ev.keyCode == 13)
			$(this).trigger('choose');
	}).delegate('.l24-styled-dd select', 'blur', function () {
		$(this).data('isopen', false);
	});
	// set span on styled selects to show selected value if not already a default;
	$('.l24-styled-dd option:selected').each(function () {
		var $span = $(this).parent().next();
		var text = $(this).text();
		if ($span.text() == "&#160")
			$span.text(text)
	});
})(jQuery);