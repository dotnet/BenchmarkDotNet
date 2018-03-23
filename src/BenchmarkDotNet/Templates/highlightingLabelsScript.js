$(document).ready(function () {
    var selectedIndex = 0;

    function highlight() {
        $('td.reference.highlighted').removeClass('highlighted');

        var selectedLabel = $(this).attr('data-label');
        if (selectedLabel) {
            $('td.reference[data-reference=' + selectedLabel + ']').addClass('highlighted');
        }
        selectedIndex = 0;
    }

    function selectNext() {
        var $highlighted = $('td.reference.highlighted');
        var selected = $highlighted[selectedIndex++ % $highlighted.length];

        if (selected) {
            var selectedReference = $(selected).attr('id');
            if (selectedReference) {
                window.location.hash = '#' + selectedReference;
            }
        }
    }

    $('td.label').on('click', highlight);

    $(document).on('keydown', function (event) {
        if (event.keyCode == 114) { // F3
            event.preventDefault();
            // Remap F3 to some other key that the browser doesn't care about
            selectNext();
        }
    });
});