$( document ).ready(function() {

    // Shift nav in mobile when clicking the menu.
    $(document).on('click', "[data-toggle='nav-top']", function() {
      $("[data-toggle='nav-shift']").toggleClass("shift");
    });

    // Close menu when you click a link.
    $(document).on('click', ".menu-vertical .current ul li a", function() {
      $("[data-toggle='nav-shift']").removeClass("shift");
    });

    // Make tables responsive
    $("table.docutils:not(.field-list)").wrap("<div class='table-responsive'></div>");
    $('table').addClass('docutils');
	
	var currentEntry = $('li.tocentry.current');
	if(!(currentEntry===undefined))
	{
		var offset = currentEntry.offset();
		if(!(offset===undefined))
		{
			if(offset.top + 40 > window.innerHeight)
			{
				// scroll current selected to top.
				$('.nav-side').scrollTop(currentEntry.offset().top - ($('.nav-side').offset().top + 80));
			}
		}
	}

    hljs.initHighlightingOnLoad();
});
