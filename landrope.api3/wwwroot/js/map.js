var mapopts = {
	preferCanvas: true,
	tap: true,
	touchZoom: true
	//      zoomSnap: 0.1
};
//pk.eyJ1Ijoiam9zZXJwb25nIiwiYSI6ImNqNWRjdTdnczBvcHIzM3I2em9oaHZpbmMifQ.cU78B8KCk2JcVp4WckrDDA

var map;

function addMap() {
	map = L.map('map', mapopts).setView([-6.027965, 106.624451], 15.5);
}

function showNormal() { }
function showStatus() { }
function showPayment() { }
//var OpenStreetMap_Mapnik = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
//	maxZoom: 24,
//	attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
//}).addTo(map);
//L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1Ijoiam9zZXJwb25nIiwiYSI6ImNqNWRjdTdnczBvcHIzM3I2em9oaHZpbmMifQ.cU78B8KCk2JcVp4WckrDDA', {
//	maxZoom: 18,
//	attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, ' +
//		'<a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
//		'Imagery © <a href="http://mapbox.com">Mapbox</a>',
//	id: 'mapbox.streets'
//}).addTo(map);

function mutant(type) {
	return L.gridLayer.googleMutant({ maxZoom: 24, type: type });
}

function initMap() {
	addMap();
	L.control.layers(
		{
			Gabungan: mutant('hybrid').addTo(map),
			Satelit: mutant('satellite'),
			Jalanan: mutant('roadmap'),
			Informasi: mutant('terrain')
		}, {},
		{
			collapsed: false
		}
	).addTo(map);

	L.control.scale().addTo(map);

	L.control.custom({
		position: 'topright',
		content:
			'<button class="btn btn-info fas fa-arrow-left" style="width:30;height:30;">' +
			//'    <i class="fas fa-arrow-left"></i>' +
			'</button>',
		classes: 'btn-group-vertical btn-group-sm',
		style:
		{
			margin: '10px',
			padding: '0px 0 0 0',
			cursor: 'pointer'
		},
		events:
		{
			click: function (data) {
				w3_open()
			}
		}
	}).addTo(map);

	const grid = L.gridLayer({
		attribution: 'Land Acquisition by Agung Sedayu Group',
		//      tileSize: L.point(150, 80),
		//      tileSize: tileSize
	});

	grid.createTile = function (coords) {
		var tile = L.DomUtil.create('div', 'tile-coords');
		tile.innerHTML = "";
		return tile;
	};

	map.addLayer(grid);
}

const labels = [];
const layers = [];

//function onEachFeature(feature, layer) {

//	var latlngs = feature.geometry.coordinates;
//	var style = Style(feature);
//	style.interactive = true;
//	var polygon = L.polygon(latlngs, style).addTo(map);

//	var content = '<table border="0" cellpadding="1" cellspacing="1">';
//	if (feature.properties) {
//		for (var key in feature.properties) {
//			if (key.substr(0, 1) != '-' || key.substr(0, 4) == '-idx')
//				continue;
//			content += '<tr><td><b>' + key.substring(1) + '</b></td><td><b>:</b> ' + feature.properties[key] + '</td></tr>';
//		}
//	}
//	if (activeInfo.t == "s") {
//		var idx = "'" + feature.properties["key"] + "'";
//		var name = "'" + feature.properties["-Desa"] + "'";
//		content += '<tr><td colspan="2">&nbsp;</td></tr><tr><td colspan="2" stye="align:right;"><button class="detail-link" onclick="detailClick(' + idx + ',' + name + ') ">Tampilkan bidang tanah</button></td></tr>';
//	}
//	content += '</table>';

//	if (feature.properties && feature.properties.popupContent)
//		content += feature.properties.popupContent;

//	layer.properties = feature.properties;

//	if (activeInfo.t == 's') {
//		var label = L.marker({ lat: feature.properties["-idx-lat"], lng: feature.properties["-idx-lon"] }, {
//			icon: L.divIcon({
//				className: 'label',
//				html: "<font style=\"font-family:\"Helvetica Neue\", Arial, Helvetica, sans-serif;font-size:9pt;color:black;background-color:rgba(255,255,255,0.2);white-space:nowrap;\">&nbsp;<b>" + feature.properties["label"] + "</b>&nbsp;</font>"
//				, iconSize: [80, 24]
//			})
//		});
//		label.addTo(map);
//		labels.push(label);
//	}
//	layers.push(layer);
//	layers.push(polygon);

//	const dblclickfunction = function (e) {
//		// do something here like display a popup
//		var reload = false;
//		switch (currType) {
//			case 'p':
//				currP = layer.properties['-idx'];
//				currType = 's';
//				reload = true;
//				; break;
//			case 's':
//				currS = layer.properties['-idx'];
//				currType = 'g';
//				reload = true;
//				; break;
//		}
//	};

//	const cmenufunction = function (e) {
//		// do something here like display a popup
//		e.originalEvent.preventDefault();
//		e.target.bindPopup(content).openPopup();
//		return false;
//	}

//	const clickfunction = function (e) {
//		e.originalEvent.preventDefault();
//		e.target.bindPopup(content).openPopup();
//		return false;
//	};

//	polygon.on("click", clickfunction);
//	if (label)
//		label.on("click", clickfunction);
//	polygon.on("dblclick", dblclickfunction);
//	if (label)
//		label.on("dblclick", dblclickfunction);

//	//layer.bindPopup(content);
//	//label.bindPopup(content);

//	polygon.on("mouseover", e => {
//		polygon.setStyle({ weight: 6 });
//	});
//	polygon.on("mouseout", e => {
//		polygon.setStyle({ weight: 1 });
//	});

//	if (label) {
//		label.on("mouseover", e => {
//			polygon.setStyle({ weight: 6 });
//		});
//		label.on("mouseout", e => {
//			polygon.setStyle({ weight: 1 });
//		});
//	}
//}

function getVillageKey() {
	$('#selvil')
}

var activeInfo = null;

/*
 * mapData
	{
		key: "<key>",
		Areas:{
			"<status>": <Area M2>,
			...
		}, 
		SKs : [<geoJson Data>],
		Lands:{
			"<status>": [<geoJson Data>],
			...
		}
	}
*/
const mapData = [];

function clearMap() {
	try {
		for (var x in labels)
			labels[x].removeFrom(map);
		for (x in layers)
			layers[x].removeFrom(map);
	}
	catch (e) {
		alert(e);
	}
}

function ShowMap(key) {
	map.spin(true);
	getMap(key);
}

function showNow(keys) {
	let checked = $('#showmode').jqxSwitchButton('checked');
	var SKtype = checked ? "SS" : "SO";

	clearMap();
	//geojson = L.geoJSON(jdata, { onEachFeature: onEachFeature }).addTo(map);

	var data = mapData.filter((v, i, a) => keys.indexOf(v.key) != -1);
	data.forEach(d => {
		for (var vk in d.SKs) {
			addFeature(d.SKs[vk], SKtype)
		}
		if (!checked) {
			for (var i = 1; i < 7; i++) {
				if (states[i] && d.Lands[i])
					d.Lands[i].forEach(l => {
						addFeature(l, '')
					});
			}
		}
	});
	if (!checked)
		showAreas(data);
	map.spin(false);
}

function showAreas(data) {
	areas = [0, 0, 0, 0, 0, 0, 0, 0, 0];
	data.forEach(d => {
		for (var x in d.Areas) {
			areas[x] += d.Areas[x];
		}
	});
	for (var i in states) {
		if (!states[i]) {
			areas[i] = 0;
			$("#a_" + i).text('');
		}
		else if (i != 0) {
			$("#a_" + i).text(formatHa(areas[i]));
		}
	}
	let total = areas.reduce((tot, num) => tot + num);
	$('#a_total').text(formatHa(total));
}

function formatHa(value) {
	if (value < 10)
		return "" + value;
	const ivaly = value % 10;
	const ivalx = Math.trunc(value / 10);
	return Intl.NumberFormat('en-US').format(ivalx) + ivaly;
}

function getMap(keys) {
	let mkeys = [];
	for (var x in mapData)
		mkeys.push(mapData[x].key);
	let rems = [];
	keys.forEach(k => {
		if (mkeys.indexOf(k) == -1)
			rems.push(k);
	});
	if (rems.length == 0)
		showNow(keys);
	else
		$.ajax({ url: $myroot + '/api/map?token=' + $const.token, method: 'POST', data: { key: rems.toString() }, dataType: "json" })
			//$.ajax({ url: $myroot + 'sample/features4.js', method: 'GET', dataType: "json" })
			.done(data => {
				data.forEach(d => mapData.push(d));
				showNow(keys);
			})
			.catch((a, b, c) => { alert(c); });
}

const $const = { token: "" };

function addFeature(feature, type) {
	var skOnly = (type == 'SS');
	var skAsLayer = (type == 'SO');
	var land = (type == '');

	var latlngs = feature.geometry.coordinates;
	var style = skOnly ? SKSkin : skAsLayer ? SKOver : Style(feature);
	var polygon = L.polygon(latlngs, style).addTo(map);
	if (land) {
		layers.push(polygon);
		polygon = L.polygon(latlngs, style).addTo(map);
		layers.push(polygon);
		polygon = L.polygon(latlngs, style).addTo(map);
		layers.push(polygon);
		polygon = L.polygon(latlngs, style).addTo(map);
		layers.push(polygon);
		//polygon = L.polygon(latlngs, style).addTo(map);
	}

	if (!skAsLayer) {
		var content = '<table border="0" cellpadding="1" cellspacing="1">';
		if (feature.properties) {
			for (var key in feature.properties) {
				if (key.substr(0, 1) != '-' || key.substr(0, 4) == '-idx')
					continue;
				content += '<tr><td><b>' + key.substring(1) + '</b></td><td><b>:</b> ' + feature.properties[key] + '</td></tr>';
			}
		}
		if (skOnly) {
			var idx = "'" + feature.properties["key"] + "'";
			var name = "'" + feature.properties["-Desa"] + "'";
			//content += '<tr><td colspan="2">&nbsp;</td></tr><tr><td colspan="2" stye="align:right;"><button class="detail-link" onclick="detailClick(' + idx + ',' + name + ') ">Tampilkan bidang tanah</button></td></tr>';
			content += '<tr><td colspan="2">&nbsp;</td></tr><tr><td colspan="2" stye="align:right;"></td></tr>';
		}
		content += '</table>';

		if (feature.properties && feature.properties.popupContent)
			content += feature.properties.popupContent;

		//layer.properties = feature.properties;
	}
	//if (skOnly) {
	//	var label = L.marker({ lat: feature.properties["-idx-lat"], lng: feature.properties["-idx-lon"] }, {
	//		icon: L.divIcon({
	//			className: 'label',
	//			html: "<font style=\"font-family:\"Helvetica Neue\", Arial, Helvetica, sans-serif;font-size:9pt;color:black;background-color:rgba(255,255,255,0.2);white-space:nowrap;\">&nbsp;<b>" + feature.properties["label"] + "</b>&nbsp;</font>"
	//			, iconSize: [80, 24]
	//		})
	//	});
	//	label.addTo(map);
	//	labels.push(label);
	//}
	//layers.push(layer);
	layers.push(polygon);

	//const dblclickfunction = function (e) {
	//	// do something here like display a popup
	//	var reload = false;
	//	switch (currType) {
	//		case 'p':
	//			currP = layer.properties['-idx'];
	//			currType = 's';
	//			reload = true;
	//			; break;
	//		case 's':
	//			currS = layer.properties['-idx'];
	//			currType = 'g';
	//			reload = true;
	//			; break;
	//	}
	//};

	//const cmenufunction = function (e) {
	//	// do something here like display a popup
	//	e.originalEvent.preventDefault();
	//	e.target.bindPopup(content).openPopup();
	//	return false;
	//}

	const clickfunction = function (e) {
		e.originalEvent.preventDefault();
		e.target.bindPopup(content).openPopup();
		return false;
	};

	polygon.on("click", clickfunction);
	//if (label)
	//	label.on("click", clickfunction);
	//polygon.on("dblclick", dblclickfunction);
	//if (label)
	//	label.on("dblclick", dblclickfunction);

	//layer.bindPopup(content);
	//label.bindPopup(content);

	polygon.on("mouseover", e => {
		polygon.setStyle({ weight: 6 });
	});
	polygon.on("mouseout", e => {
		polygon.setStyle({ weight: 1 });
	});

	//if (label) {
	//	label.on("mouseover", e => {
	//		polygon.setStyle({ weight: 6 });
	//	});
	//	label.on("mouseout", e => {
	//		polygon.setStyle({ weight: 1 });
	//	});
	//}
}
