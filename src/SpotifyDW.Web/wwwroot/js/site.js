// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


// Artist autocomplete for all artistPattern/artist1/artist2 fields
document.addEventListener('DOMContentLoaded', function () {
	function setupAutocomplete(inputId) {
		var input = document.getElementById(inputId);
		if (!input) return;

		// Create suggestion box
		var box = document.createElement('div');
		box.className = 'autocomplete-suggestions';
		box.style.position = 'absolute';
		box.style.zIndex = 1000;
		box.style.background = '#fff';
		box.style.border = '1px solid #ccc';
		box.style.width = input.offsetWidth + 'px';
		box.style.maxHeight = '220px';
		box.style.overflowY = 'auto';
		box.style.display = 'none';
		input.parentNode.appendChild(box);

		input.addEventListener('input', function () {
			var val = input.value;
			if (val.length < 2) {
				box.style.display = 'none';
				return;
			}
			fetch(`/api/artists/suggest?term=${encodeURIComponent(val)}`)
				.then(r => r.json())
				.then(data => {
					box.innerHTML = '';
					if (data.length === 0) {
						box.style.display = 'none';
						return;
					}
					data.forEach(function (artist) {
						var item = document.createElement('div');
						item.className = 'autocomplete-item';
						item.textContent = artist;
						item.style.padding = '8px 12px';
						item.style.cursor = 'pointer';
						item.addEventListener('mousedown', function (e) {
							input.value = artist;
							box.style.display = 'none';
							input.dispatchEvent(new Event('input'));
						});
						box.appendChild(item);
					});
					var rect = input.getBoundingClientRect();
					box.style.width = input.offsetWidth + 'px';
					box.style.left = input.offsetLeft + 'px';
					box.style.top = (input.offsetTop + input.offsetHeight) + 'px';
					box.style.display = 'block';
				});
		});

		// Hide box on blur
		input.addEventListener('blur', function () {
			setTimeout(() => { box.style.display = 'none'; }, 150);
		});
	}

	// Attach to all relevant fields
	['artistPattern', 'artist1', 'artist2', 'artist', 'album'].forEach(setupAutocomplete);

	// Album autocomplete for Warehouse page
	function setupAlbumAutocomplete(inputId) {
		var input = document.getElementById(inputId);
		if (!input) return;
		var box = document.createElement('div');
		box.className = 'autocomplete-suggestions';
		box.style.position = 'absolute';
		box.style.zIndex = 1000;
		box.style.background = '#fff';
		box.style.border = '1px solid #ccc';
		box.style.width = input.offsetWidth + 'px';
		box.style.maxHeight = '220px';
		box.style.overflowY = 'auto';
		box.style.display = 'none';
		input.parentNode.appendChild(box);
		input.addEventListener('input', function () {
			var val = input.value;
			if (val.length < 2) {
				box.style.display = 'none';
				return;
			}
			fetch(`/api/albums/suggest?term=${encodeURIComponent(val)}`)
				.then(r => r.json())
				.then(data => {
					box.innerHTML = '';
					if (data.length === 0) {
						box.style.display = 'none';
						return;
					}
					data.forEach(function (album) {
						var item = document.createElement('div');
						item.className = 'autocomplete-item';
						item.textContent = album;
						item.style.padding = '8px 12px';
						item.style.cursor = 'pointer';
						item.addEventListener('mousedown', function (e) {
							input.value = album;
							box.style.display = 'none';
							input.dispatchEvent(new Event('input'));
						});
						box.appendChild(item);
					});
					var rect = input.getBoundingClientRect();
					box.style.width = input.offsetWidth + 'px';
					box.style.left = input.offsetLeft + 'px';
					box.style.top = (input.offsetTop + input.offsetHeight) + 'px';
					box.style.display = 'block';
				});
		});
		input.addEventListener('blur', function () {
			setTimeout(() => { box.style.display = 'none'; }, 150);
		});
	}
	setupAlbumAutocomplete('album');
});
